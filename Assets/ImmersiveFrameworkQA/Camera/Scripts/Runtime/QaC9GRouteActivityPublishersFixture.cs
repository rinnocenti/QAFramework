using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/C9G Route Activity Publishers Fixture")]
    public sealed class QaC9GRouteActivityPublishersFixture : MonoBehaviour
    {
        private const string LogPrefix = "[QA][C9G Route Activity Publishers]";

        [Header("Output")]
        [SerializeField] private UnityEngine.Camera outputCamera;
        [SerializeField] private CinemachineBrain outputBrain;

        [Header("Rigs")]
        [SerializeField] private CameraRigComposer routeComposer;
        [SerializeField] private CameraRigComposer activityComposer;
        [SerializeField] private CameraRigComposer invalidComposer;

        [Header("Target")]
        [SerializeField] private Transform explicitTargetSource;

        [Header("Execution")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        [Header("Debug")]
        [SerializeField] private string lastStatus = "NotRun";
        [SerializeField] private string lastFailure;
        [SerializeField] private int completedCaseCount;

        public string LastStatus => lastStatus ?? string.Empty;
        public string LastFailure => lastFailure ?? string.Empty;
        public int CompletedCaseCount => completedCaseCount;

        private void Start()
        {
            if (runOnStart)
            {
                Run();
            }
        }

        [ContextMenu("Run C9G Route Activity Publishers Proof")]
        public void Run()
        {
            var completed = new List<string>();
            lastStatus = "Running";
            lastFailure = string.Empty;
            completedCaseCount = 0;

            bool outputEnabledBefore = outputCamera != null && outputCamera.enabled;
            bool outputActiveBefore = outputCamera != null && outputCamera.gameObject.activeSelf;
            Vector3 outputPositionBefore = outputCamera != null
                ? outputCamera.transform.position
                : Vector3.zero;

            Debug.Log($"{LogPrefix} Smoke started.", this);

            try
            {
                ValidateFixture();
                RunRouteActivityLifecycle(completed);
                RunPublishReleaseIdempotency(completed);
                RunWrongOwnerBlocked(completed);
                RunWrongLifetimeBlocked(completed);
                RunForeignOutputBlocked(completed);
                RunFailedSessionDoesNotFlipPublisherState(completed);
                AssertUnityOutputUnchanged(
                    outputEnabledBefore,
                    outputActiveBefore,
                    outputPositionBefore,
                    completed);

                completedCaseCount = completed.Count;
                lastStatus = "Passed";

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.",
                    this);
            }
            catch (Exception exception)
            {
                completedCaseCount = completed.Count;
                lastStatus = "Failed";
                lastFailure = exception.Message ?? exception.GetType().Name;

                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(lastFailure)}' completed='{string.Join(",", completed)}'.",
                    this);

                if (throwOnFailure)
                {
                    throw;
                }
            }
        }

        private void ValidateFixture()
        {
            AssertNotNull(outputCamera, "Output Camera is missing.");
            AssertNotNull(outputBrain, "Output CinemachineBrain is missing.");
            AssertTrue(
                outputCamera.gameObject == outputBrain.gameObject,
                "Output Camera and CinemachineBrain must share one GameObject.");

            AssertValidComposer(routeComposer, "Route");
            AssertValidComposer(activityComposer, "Activity");

            AssertNotNull(invalidComposer, "Invalid composer is missing.");
            AssertTrue(
                invalidComposer.CinemachineCamera == null,
                "Invalid composer must not expose a CinemachineCamera.");

            AssertNotNull(explicitTargetSource, "Explicit target source is missing.");
        }

        private void RunRouteActivityLifecycle(List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest routeRequest = CreateRequest(
                "route-lifecycle",
                routeComposer,
                CameraOutputId.Main,
                CameraRequestOwnerKind.Route,
                CameraRequestLifetimeKind.Route,
                10,
                "route");

            CameraRequest activityRequest = CreateRequest(
                "activity-lifecycle",
                activityComposer,
                CameraOutputId.Main,
                CameraRequestOwnerKind.Activity,
                CameraRequestLifetimeKind.Activity,
                100,
                "activity");

            RouteCameraRequestPublisher routePublisher =
                RequirePublisher<RouteCameraRequestPublisher>(
                    RouteCameraRequestPublisher.Create(
                        session,
                        routeRequest),
                    "Route publisher creation failed.");

            ActivityCameraRequestPublisher activityPublisher =
                RequirePublisher<ActivityCameraRequestPublisher>(
                    ActivityCameraRequestPublisher.Create(
                        session,
                        activityRequest),
                    "Activity publisher creation failed.");

            CameraRequestPublisherResult routePublish =
                routePublisher.Publish();

            AssertPublisherSucceeded(
                routePublish,
                CameraRequestPublisherOperationKind.Published,
                "Route publish failed.");

            AssertTrue(routeComposer.CinemachineCamera.enabled,
                "Route rig was not applied.");
            AssertTrue(!activityComposer.CinemachineCamera.enabled,
                "Activity rig should remain disabled.");

            CameraRequestPublisherResult activityPublish =
                activityPublisher.Publish();

            AssertPublisherSucceeded(
                activityPublish,
                CameraRequestPublisherOperationKind.Published,
                "Activity publish failed.");

            AssertTrue(!routeComposer.CinemachineCamera.enabled,
                "Route rig was not disabled by Activity.");
            AssertTrue(activityComposer.CinemachineCamera.enabled,
                "Activity rig was not applied.");

            CameraRequestPublisherResult activityRelease =
                activityPublisher.Release();

            AssertPublisherSucceeded(
                activityRelease,
                CameraRequestPublisherOperationKind.Released,
                "Activity release failed.");

            AssertTrue(routeComposer.CinemachineCamera.enabled,
                "Route rig was not restored.");
            AssertTrue(!activityComposer.CinemachineCamera.enabled,
                "Activity rig remained enabled.");

            CameraRequestPublisherResult routeRelease =
                routePublisher.Release();

            AssertPublisherSucceeded(
                routeRelease,
                CameraRequestPublisherOperationKind.Released,
                "Route release failed.");

            AssertTrue(!routeComposer.CinemachineCamera.enabled,
                "Route rig was not cleared.");
            AssertTrue(!session.Applicator.HasAppliedRequest,
                "Output remained applied after final release.");

            LogStep(
                "route-activity-lifecycle",
                $"route='{routeRequest.RequestId}' activity='{activityRequest.RequestId}'");
            completed.Add("route-activity-lifecycle");
        }

        private void RunPublishReleaseIdempotency(List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest routeRequest = CreateRequest(
                "route-idempotent",
                routeComposer,
                CameraOutputId.Main,
                CameraRequestOwnerKind.Route,
                CameraRequestLifetimeKind.Route,
                10,
                "route-idempotent");

            RouteCameraRequestPublisher publisher =
                RequirePublisher<RouteCameraRequestPublisher>(
                    RouteCameraRequestPublisher.Create(
                        session,
                        routeRequest),
                    "Idempotent Route publisher creation failed.");

            CameraRequestPublisherResult firstPublish = publisher.Publish();
            CameraRequestPublisherResult secondPublish = publisher.Publish();

            AssertPublisherSucceeded(
                firstPublish,
                CameraRequestPublisherOperationKind.Published,
                "First publish failed.");

            AssertPublisherSucceeded(
                secondPublish,
                CameraRequestPublisherOperationKind.Preserved,
                "Second publish should preserve state.");

            AssertTrue(publisher.IsPublished,
                "Publisher lost published state after duplicate Publish.");

            CameraRequestPublisherResult firstRelease = publisher.Release();
            CameraRequestPublisherResult secondRelease = publisher.Release();

            AssertPublisherSucceeded(
                firstRelease,
                CameraRequestPublisherOperationKind.Released,
                "First release failed.");

            AssertPublisherSucceeded(
                secondRelease,
                CameraRequestPublisherOperationKind.Preserved,
                "Second release should preserve state.");

            AssertTrue(!publisher.IsPublished,
                "Publisher remained published after release.");

            LogStep(
                "publisher-idempotency",
                $"publish2='{secondPublish.OperationKind}' release2='{secondRelease.OperationKind}'");
            completed.Add("publisher-idempotency");
        }

        private void RunWrongOwnerBlocked(List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest wrongOwner = CreateRequest(
                "wrong-owner",
                routeComposer,
                CameraOutputId.Main,
                CameraRequestOwnerKind.Activity,
                CameraRequestLifetimeKind.Route,
                10,
                "wrong-owner");

            CameraRequestPublisherCreateResult result =
                RouteCameraRequestPublisher.Create(
                    session,
                    wrongOwner);

            AssertCreateBlocked(
                result,
                "camera.request-publisher.owner-kind.invalid",
                "Wrong owner kind was not blocked.");

            LogStep("wrong-owner-blocked", result.DiagnosticSummary);
            completed.Add("wrong-owner-blocked");
        }

        private void RunWrongLifetimeBlocked(List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest wrongLifetime = CreateRequest(
                "wrong-lifetime",
                activityComposer,
                CameraOutputId.Main,
                CameraRequestOwnerKind.Activity,
                CameraRequestLifetimeKind.Route,
                100,
                "wrong-lifetime");

            CameraRequestPublisherCreateResult result =
                ActivityCameraRequestPublisher.Create(
                    session,
                    wrongLifetime);

            AssertCreateBlocked(
                result,
                "camera.request-publisher.lifetime-kind.invalid",
                "Wrong lifetime kind was not blocked.");

            LogStep("wrong-lifetime-blocked", result.DiagnosticSummary);
            completed.Add("wrong-lifetime-blocked");
        }

        private void RunForeignOutputBlocked(List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest foreign = CreateRequest(
                "foreign-output",
                routeComposer,
                new CameraOutputId("camera.output.foreign"),
                CameraRequestOwnerKind.Route,
                CameraRequestLifetimeKind.Route,
                10,
                "foreign-output");

            CameraRequestPublisherCreateResult result =
                RouteCameraRequestPublisher.Create(
                    session,
                    foreign);

            AssertCreateBlocked(
                result,
                "camera.request-publisher.output-mismatch",
                "Foreign output was not blocked.");

            LogStep("foreign-output-blocked", result.DiagnosticSummary);
            completed.Add("foreign-output-blocked");
        }

        private void RunFailedSessionDoesNotFlipPublisherState(
            List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest invalidActivity = CreateRequest(
                "invalid-activity",
                invalidComposer,
                CameraOutputId.Main,
                CameraRequestOwnerKind.Activity,
                CameraRequestLifetimeKind.Activity,
                100,
                "invalid-activity");

            ActivityCameraRequestPublisher publisher =
                RequirePublisher<ActivityCameraRequestPublisher>(
                    ActivityCameraRequestPublisher.Create(
                        session,
                        invalidActivity),
                    "Invalid Activity publisher creation failed.");

            CameraRequestPublisherResult publish = publisher.Publish();

            AssertTrue(
                publish.IsRejected,
                "Failed session Publish should be rejected.");
            AssertTrue(
                !publisher.IsPublished,
                "Publisher flipped to published after rolled-back session.");
            AssertTrue(
                !session.Context.Contains(invalidActivity.RequestId),
                "Rolled-back request remained admitted.");

            LogStep(
                "failed-session-preserves-state",
                publish.DiagnosticSummary);
            completed.Add("failed-session-preserves-state");
        }

        private void AssertUnityOutputUnchanged(
            bool enabledBefore,
            bool activeBefore,
            Vector3 positionBefore,
            List<string> completed)
        {
            AssertEqual(
                enabledBefore,
                outputCamera.enabled,
                "Publishers changed Unity Camera.enabled.");

            AssertEqual(
                activeBefore,
                outputCamera.gameObject.activeSelf,
                "Publishers changed output active state.");

            AssertEqual(
                positionBefore,
                outputCamera.transform.position,
                "Publishers changed output transform.");

            LogStep(
                "unity-output-unchanged",
                $"enabled='{outputCamera.enabled}' active='{outputCamera.gameObject.activeSelf}' " +
                $"position='{outputCamera.transform.position}'");
            completed.Add("unity-output-unchanged");
        }

        private CameraOutputSession CreateSession(CameraOutputId outputId)
        {
            CameraOutputContext context =
                new CameraOutputContext(outputId);

            CameraOutputRigApplicator applicator =
                new CameraOutputRigApplicator(
                    new CameraOutputBinding(
                        outputId,
                        outputCamera,
                        outputBrain));

            return new CameraOutputSession(context, applicator);
        }

        private CameraRequest CreateRequest(
            string suffix,
            CameraRigComposer composer,
            CameraOutputId outputId,
            CameraRequestOwnerKind ownerKind,
            CameraRequestLifetimeKind lifetimeKind,
            int precedence,
            string tieBreaker)
        {
            CameraRequestCreateResult result =
                CameraRequestCreateResult.Create(
                    new CameraRequestId($"qa.camera.request.c9g.{suffix}"),
                    outputId,
                    new CameraRequestOwner(
                        ownerKind,
                        $"qa.owner.c9g.{suffix}"),
                    new CameraRequestLifetime(
                        lifetimeKind,
                        $"qa.lifetime.c9g.{suffix}"),
                    CameraRigReference.FromComposer(composer),
                    CameraTargetSourceDescriptor.ExplicitTransform(
                        explicitTargetSource,
                        $"QA C9G Target {suffix}"),
                    new CameraRequestPolicy(precedence, tieBreaker),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(QaC9GRouteActivityPublishersFixture),
                    $"QA C9G synthetic request '{suffix}'.");

            if (!result.IsSucceeded)
            {
                throw new InvalidOperationException(
                    $"Could not create request '{suffix}': {result.BlockingIssue}");
            }

            return result.Request;
        }

        private static TPublisher RequirePublisher<TPublisher>(
            CameraRequestPublisherCreateResult result,
            string message)
            where TPublisher : class, ICameraRequestPublisher
        {
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{message} {result.DiagnosticSummary}");
            }

            TPublisher publisher = result.Publisher as TPublisher;

            if (publisher == null)
            {
                throw new InvalidOperationException(
                    $"{message} Publisher type mismatch.");
            }

            return publisher;
        }

        private static void AssertCreateBlocked(
            CameraRequestPublisherCreateResult result,
            string expectedCode,
            string message)
        {
            AssertTrue(result.IsBlocked, message);
            AssertIssue(result.Issues, expectedCode, message);
        }

        private static void AssertPublisherSucceeded(
            CameraRequestPublisherResult result,
            CameraRequestPublisherOperationKind expectedKind,
            string message)
        {
            AssertTrue(result.Succeeded, message + " " + result.DiagnosticSummary);
            AssertEqual(expectedKind, result.OperationKind, message);
        }

        private static void AssertIssue(
            CameraIssue[] issues,
            string expectedCode,
            string message)
        {
            AssertTrue(
                issues != null && issues.Length == 1,
                message + " Expected one issue.");

            AssertEqual(
                expectedCode,
                issues[0].Code,
                message + " Wrong issue code.");

            AssertTrue(
                issues[0].IsBlocking,
                message + " Issue is not blocking.");
        }

        private void ResetRigState()
        {
            routeComposer.CinemachineCamera.enabled = false;
            activityComposer.CinemachineCamera.enabled = false;
        }

        private static void AssertValidComposer(
            CameraRigComposer composer,
            string label)
        {
            AssertNotNull(composer, $"{label} composer is missing.");
            AssertNotNull(
                composer.CinemachineCamera,
                $"{label} composer CinemachineCamera is missing.");
        }

        private void LogStep(string step, string evidence)
        {
            Debug.Log(
                $"{LogPrefix} step='{step}' evidence='{Escape(evidence)}'.",
                this);
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(UnityEngine.Object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
        }
    }
}

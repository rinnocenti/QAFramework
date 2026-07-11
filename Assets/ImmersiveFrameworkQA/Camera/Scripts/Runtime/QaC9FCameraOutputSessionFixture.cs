using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/C9F Camera Output Session Fixture")]
    public sealed class QaC9FCameraOutputSessionFixture : MonoBehaviour
    {
        private const string LogPrefix = "[QA][C9F Camera Output Session]";

        [Header("Output")]
        [SerializeField] private UnityEngine.Camera outputCamera;
        [SerializeField] private CinemachineBrain outputBrain;

        [Header("Rigs")]
        [SerializeField] private CameraRigComposer routeComposer;
        [SerializeField] private CameraRigComposer playerComposer;
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

        [ContextMenu("Run C9F Camera Output Session Proof")]
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
                RunAutomaticLifecycle(completed);
                RunRejectedMutationDoesNotApply(completed);
                RunAdmissionRollback(completed);
                RunReleaseRollback(completed);
                RunSynchronizePreexistingState(completed);
                RunMismatchedConstructorBlocked(completed);
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
            AssertValidComposer(playerComposer, "Player");

            AssertNotNull(invalidComposer, "Invalid composer is missing.");
            AssertTrue(
                invalidComposer.CinemachineCamera == null,
                "Invalid composer must not expose a CinemachineCamera.");

            AssertNotNull(explicitTargetSource, "Explicit target source is missing.");
        }

        private void RunAutomaticLifecycle(List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest route = CreateRequest(
                "lifecycle-route",
                routeComposer,
                CameraOutputId.Main,
                10,
                "route");

            CameraRequest player = CreateRequest(
                "lifecycle-player",
                playerComposer,
                CameraOutputId.Main,
                100,
                "player");

            CameraOutputSessionResult routeAdmit = session.Admit(route);
            AssertSessionSucceeded(routeAdmit, "Route session admission failed.");
            AssertTrue(routeComposer.CinemachineCamera.enabled, "Route rig was not applied automatically.");
            AssertTrue(!playerComposer.CinemachineCamera.enabled, "Player rig should remain disabled.");

            CameraOutputSessionResult playerAdmit = session.Admit(player);
            AssertSessionSucceeded(playerAdmit, "Player session admission failed.");
            AssertTrue(!routeComposer.CinemachineCamera.enabled, "Route rig was not disabled.");
            AssertTrue(playerComposer.CinemachineCamera.enabled, "Player rig was not applied.");

            CameraOutputSessionResult playerRelease =
                session.Release(player.RequestId);
            AssertSessionSucceeded(playerRelease, "Player session release failed.");
            AssertTrue(routeComposer.CinemachineCamera.enabled, "Route rig was not restored automatically.");
            AssertTrue(!playerComposer.CinemachineCamera.enabled, "Player rig remained enabled.");

            CameraOutputSessionResult routeRelease =
                session.Release(route.RequestId);
            AssertSessionSucceeded(routeRelease, "Route session release failed.");
            AssertTrue(!routeComposer.CinemachineCamera.enabled, "Final release did not clear Route rig.");
            AssertTrue(!session.Applicator.HasAppliedRequest, "Final release left applied state.");

            LogStep(
                "automatic-lifecycle",
                $"route='{route.RequestId}' player='{player.RequestId}'");
            completed.Add("automatic-lifecycle");
        }

        private void RunRejectedMutationDoesNotApply(List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest route = CreateRequest(
                "duplicate",
                routeComposer,
                CameraOutputId.Main,
                10,
                "duplicate");

            AssertSessionSucceeded(
                session.Admit(route),
                "Initial duplicate request admission failed.");

            CameraOutputSessionResult duplicate = session.Admit(route);

            AssertTrue(duplicate.WasRejected, "Duplicate session admission should be rejected.");
            AssertTrue(!duplicate.HasApplyResult, "Rejected context mutation should not apply output.");
            AssertTrue(routeComposer.CinemachineCamera.enabled, "Rejected mutation disturbed current rig.");
            AssertEqual(
                route.RequestId,
                session.Context.Winner.RequestId,
                "Rejected mutation changed the winner.");

            LogStep("rejected-mutation-no-apply", duplicate.DiagnosticSummary);
            completed.Add("rejected-mutation-no-apply");
        }

        private void RunAdmissionRollback(List<string> completed)
        {
            ResetRigState();

            CameraOutputSession session = CreateSession(CameraOutputId.Main);

            CameraRequest route = CreateRequest(
                "admit-rollback-route",
                routeComposer,
                CameraOutputId.Main,
                10,
                "route");

            CameraRequest invalid = CreateRequest(
                "admit-rollback-invalid",
                invalidComposer,
                CameraOutputId.Main,
                100,
                "invalid");

            AssertSessionSucceeded(
                session.Admit(route),
                "Rollback baseline Route admission failed.");

            CameraOutputSessionResult result = session.Admit(invalid);

            AssertTrue(result.WasRolledBack, "Failed admission was not rolled back.");
            AssertIssue(
                result.Issues,
                "camera.output-session.application-failed-rolled-back",
                "Admission rollback emitted wrong issue.");
            AssertTrue(!session.Context.Contains(invalid.RequestId), "Invalid request remained admitted.");
            AssertTrue(session.Context.Contains(route.RequestId), "Baseline Route request was lost.");
            AssertEqual(route.RequestId, session.Context.Winner.RequestId, "Route winner was not restored.");
            AssertTrue(routeComposer.CinemachineCamera.enabled, "Route presentation was not restored.");
            AssertTrue(!session.Applicator.HasAppliedRequest ||
                       session.Applicator.AppliedRequestId == route.RequestId,
                "Applicator did not restore Route request.");

            LogStep("admission-rollback", result.DiagnosticSummary);
            completed.Add("admission-rollback");
        }

        private void RunReleaseRollback(List<string> completed)
        {
            ResetRigState();

            CameraOutputContext context =
                new CameraOutputContext(CameraOutputId.Main);

            CameraOutputRigApplicator applicator =
                CreateApplicator(CameraOutputId.Main);

            CameraRequest invalid = CreateRequest(
                "release-rollback-invalid",
                invalidComposer,
                CameraOutputId.Main,
                10,
                "invalid");

            CameraRequest player = CreateRequest(
                "release-rollback-player",
                playerComposer,
                CameraOutputId.Main,
                100,
                "player");

            AssertTrue(context.Admit(invalid).Succeeded, "Invalid lower request admission failed.");
            AssertTrue(context.Admit(player).Succeeded, "Player higher request admission failed.");

            CameraOutputApplyResult initialApply = applicator.Apply(context);
            AssertTrue(initialApply.Succeeded, "Initial Player application failed.");
            AssertTrue(playerComposer.CinemachineCamera.enabled, "Player rig was not initially applied.");

            var session = new CameraOutputSession(context, applicator);

            CameraOutputSessionResult result =
                session.Release(player.RequestId);

            AssertTrue(result.WasRolledBack, "Failed release was not rolled back.");
            AssertIssue(
                result.Issues,
                "camera.output-session.application-failed-rolled-back",
                "Release rollback emitted wrong issue.");
            AssertTrue(context.Contains(player.RequestId), "Released Player request was not re-admitted.");
            AssertEqual(player.RequestId, context.Winner.RequestId, "Player winner was not restored.");
            AssertTrue(playerComposer.CinemachineCamera.enabled, "Player presentation was not restored.");

            LogStep("release-rollback", result.DiagnosticSummary);
            completed.Add("release-rollback");
        }

        private void RunSynchronizePreexistingState(List<string> completed)
        {
            ResetRigState();

            CameraOutputContext context =
                new CameraOutputContext(CameraOutputId.Main);

            CameraRequest route = CreateRequest(
                "synchronize",
                routeComposer,
                CameraOutputId.Main,
                10,
                "synchronize");

            AssertTrue(context.Admit(route).Succeeded, "Preexisting context admission failed.");

            CameraOutputRigApplicator applicator =
                CreateApplicator(CameraOutputId.Main);

            var session = new CameraOutputSession(context, applicator);

            CameraOutputSessionResult result = session.Synchronize();

            AssertSessionSucceeded(result, "Session synchronize failed.");
            AssertTrue(routeComposer.CinemachineCamera.enabled, "Synchronize did not apply preexisting winner.");
            AssertEqual(
                route.RequestId,
                applicator.AppliedRequestId,
                "Synchronize applied wrong request.");

            LogStep("synchronize-preexisting", result.DiagnosticSummary);
            completed.Add("synchronize-preexisting");
        }

        private void RunMismatchedConstructorBlocked(List<string> completed)
        {
            ResetRigState();

            bool threw = false;

            try
            {
                var context = new CameraOutputContext(CameraOutputId.Main);
                CameraOutputRigApplicator foreignApplicator =
                    CreateApplicator(new CameraOutputId("camera.output.foreign"));

                _ = new CameraOutputSession(context, foreignApplicator);
            }
            catch (ArgumentException)
            {
                threw = true;
            }

            AssertTrue(threw, "Mismatched output constructor did not throw.");

            LogStep(
                "mismatched-constructor-blocked",
                "ArgumentException");
            completed.Add("mismatched-constructor-blocked");
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
                "Session changed Unity Camera.enabled.");

            AssertEqual(
                activeBefore,
                outputCamera.gameObject.activeSelf,
                "Session changed output active state.");

            AssertEqual(
                positionBefore,
                outputCamera.transform.position,
                "Session changed output transform.");

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
                CreateApplicator(outputId);

            return new CameraOutputSession(context, applicator);
        }

        private CameraOutputRigApplicator CreateApplicator(
            CameraOutputId outputId)
        {
            return new CameraOutputRigApplicator(
                new CameraOutputBinding(
                    outputId,
                    outputCamera,
                    outputBrain));
        }

        private CameraRequest CreateRequest(
            string suffix,
            CameraRigComposer composer,
            CameraOutputId outputId,
            int precedence,
            string tieBreaker)
        {
            CameraRequestCreateResult result =
                CameraRequestCreateResult.Create(
                    new CameraRequestId($"qa.camera.request.c9f.{suffix}"),
                    outputId,
                    new CameraRequestOwner(
                        CameraRequestOwnerKind.Debug,
                        $"qa.owner.c9f.{suffix}"),
                    new CameraRequestLifetime(
                        CameraRequestLifetimeKind.ExplicitOperation,
                        $"qa.lifetime.c9f.{suffix}"),
                    CameraRigReference.FromComposer(composer),
                    CameraTargetSourceDescriptor.ExplicitTransform(
                        explicitTargetSource,
                        $"QA C9F Target {suffix}"),
                    new CameraRequestPolicy(precedence, tieBreaker),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(QaC9FCameraOutputSessionFixture),
                    $"QA C9F synthetic request '{suffix}'.");

            if (!result.IsSucceeded)
            {
                throw new InvalidOperationException(
                    $"Could not create request '{suffix}': {result.BlockingIssue}");
            }

            return result.Request;
        }

        private void ResetRigState()
        {
            routeComposer.CinemachineCamera.enabled = false;
            playerComposer.CinemachineCamera.enabled = false;
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

        private static void AssertSessionSucceeded(
            CameraOutputSessionResult result,
            string message)
        {
            AssertTrue(result.Succeeded, message + " " + result.DiagnosticSummary);
            AssertTrue(result.HasApplyResult, message + " Apply result is missing.");
            AssertTrue(result.ApplyResult.Succeeded, message + " Apply failed.");
        }

        private static void AssertIssue(
            CameraIssue[] issues,
            string expectedCode,
            string message)
        {
            AssertTrue(issues != null && issues.Length == 1, message);
            AssertEqual(expectedCode, issues[0].Code, message);
            AssertTrue(issues[0].IsBlocking, message + " Issue is not blocking.");
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

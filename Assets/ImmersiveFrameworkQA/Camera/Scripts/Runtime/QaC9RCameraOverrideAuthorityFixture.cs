using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaCameraOverrideAuthorityFixture :
        MonoBehaviour,
        ICameraOutputSessionConsumer,
        ISessionCameraOverrideConsumer
    {
        private const string LogPrefix =
            "[CAMERA_RUNTIME_HOST_INTEGRATION_REGRESSION]";
        private const string Adr004BLogPrefix =
            "[QA_CAMERA_ADR004B]";
        private const int MaxReadinessFrames = 600;
        private const int ExpectedCaseCount = 11;

        [SerializeField] private RouteCameraOverrideBinding routeBinding;
        [SerializeField]
        private QaLocalPlayerCameraRequestBinding playerBinding;
        [SerializeField] private ActivityCameraOverrideBinding activityBinding;
        [SerializeField] private CameraRigComposer routeComposer;
        [SerializeField] private CameraRigComposer playerComposer;
        [SerializeField] private CameraRigComposer activityComposer;
        [SerializeField] private ActivityRequestTrigger activityRequestTrigger;
        [SerializeField] private RouteRequestTrigger backToHubTrigger;
        [SerializeField] private bool throwOnFailure;
        [SerializeField] private string lastStatus = "NotRun";
        [SerializeField] private string lastFailure;
        [SerializeField] private int completedCaseCount;

        private CameraOutputSessionBinding outputSession;
        private SessionCameraOverrideBinding sessionOverride;
        private bool started;
        private bool awaitingRouteLifecycleCleanup;
        private string routeRequestId;
        private const string RouteLifecycleSurvivorRequestId =
            "qa.camera.adr004b.route-lifecycle-survivor";
        private ICameraRequestPublisher routeLifecycleSurvivorPublisher;
        private readonly List<string> completedCases = new List<string>();

        public static bool Adr004BActivityLifecycleExecuted { get; private set; }
        public static bool Adr004BActivityLifecyclePassed { get; private set; }
        public static bool Adr004BRouteLifecycleExecuted { get; private set; }
        public static bool Adr004BRouteLifecyclePassed { get; private set; }
        public static bool Adr004BOwnerLossExecuted { get; private set; }
        public static bool Adr004BOwnerLossInvariantPassed { get; private set; }
        public static string Adr004BOwnerLossDiagnostic { get; private set; } = string.Empty;

        public void RunFromContextMenu()
        {
            Begin();
        }

        internal void Begin()
        {
            if (started)
            {
                return;
            }

            started = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            lastStatus = "Running";
            lastFailure = string.Empty;
            completedCaseCount = 0;
            completedCases.Clear();
            ResetAdr004BEvidence();

            yield return WaitFor(Readiness, "persistent-output-readiness");
            if (HasFailed)
            {
                yield break;
            }

            if (!TryStep(() =>
                {
                    Winner(playerBinding.RequestIdText, playerComposer, "player-default");
                    Complete("player-default");

                    Require(activityBinding.RequestOverride().Succeeded,
                        "Activity request failed.");
                    Winner(activityBinding.RequestIdText, activityComposer, "activity-request");
                    Complete("activity-request");

                    Require(routeBinding.RequestOverride().Succeeded,
                        "Route request failed.");
                    Winner(routeBinding.RequestIdText, routeComposer, "route-request");
                    Complete("route-request");

                    Require(sessionOverride.RequestOverride().Succeeded,
                        "Session request failed.");
                    Winner(
                        sessionOverride.RequestIdText,
                        sessionOverride.RigComposer,
                        "session-request");
                    Complete("session-request");

                    Require(sessionOverride.ReleaseOverride().Succeeded,
                        "Session release failed.");
                    Winner(routeBinding.RequestIdText, routeComposer,
                        "session-release-restores-route");
                    Complete("session-release-restores-route");

                    Require(routeBinding.ReleaseOverride().Succeeded,
                        "Route release failed.");
                    Winner(activityBinding.RequestIdText, activityComposer,
                        "route-release-restores-activity");
                    Complete("route-release-restores-activity");

                    Require(activityBinding.ReleaseOverride().Succeeded,
                        "Activity release failed.");
                    Winner(playerBinding.RequestIdText, playerComposer,
                        "activity-release-restores-player");
                    Complete("activity-release-restores-player");

                    Require(activityBinding.RequestOverride().Succeeded,
                        "First duplicate request failed.");
                    Require(
                        activityBinding.RequestOverride().Operation ==
                        CameraOverrideOperationKind.Preserved,
                        "Duplicate request was not preserved.");
                    Winner(activityBinding.RequestIdText, activityComposer,
                        "duplicate-request");
                    Complete("duplicate-request");

                    Require(activityBinding.ReleaseOverride().Succeeded,
                        "First duplicate release failed.");
                    Require(
                        activityBinding.ReleaseOverride().Operation ==
                        CameraOverrideOperationKind.Preserved,
                        "Duplicate release was not preserved.");
                    Winner(playerBinding.RequestIdText, playerComposer,
                        "duplicate-release");
                    Complete("duplicate-release");

                    Require(activityBinding.RequestOverride().Succeeded,
                        "Activity lifecycle setup failed.");
                    Require(activityRequestTrigger != null,
                        "Activity request trigger is missing.");
                    activityRequestTrigger.ClearActivity();
                }))
            {
                yield break;
            }

            yield return WaitFor(
                () => !activityRequestTrigger.IsRequestInFlight &&
                    activityRequestTrigger.LastRequestSucceeded,
                "activity-clear-request");
            if (HasFailed)
            {
                yield break;
            }

            yield return WaitFor(
                () => !Context.Contains(
                        new CameraRequestId(activityBinding.RequestIdText)) &&
                    IsWinner(playerBinding.RequestIdText),
                "activity-lifecycle-cleanup");
            if (HasFailed)
            {
                yield break;
            }

            if (!TryStep(() =>
                {
                    Adr004BActivityLifecycleExecuted = true;
                    Adr004BActivityLifecyclePassed = true;
                    Complete("activity-lifecycle-cleanup");

                    RunAdr004BOwnerLossProbe();

                    Require(routeBinding.RequestOverride().Succeeded,
                        "Route lifecycle setup failed.");
                    Winner(routeBinding.RequestIdText, routeComposer,
                        "route-lifecycle-setup");

                    PublishRouteLifecycleSurvivor();
                    Winner(routeBinding.RequestIdText, routeComposer,
                        "route-lifecycle-survivor-admitted");

                    Require(
                        backToHubTrigger != null &&
                        backToHubTrigger.TargetRoute != null,
                        "Back-to-Hub trigger is missing.");
                    routeRequestId = routeBinding.RequestIdText;
                    awaitingRouteLifecycleCleanup = true;
                    lastStatus = "WaitingRouteLifecycleCleanup";
                    backToHubTrigger.RequestRoute();
                    StartCoroutine(WatchRouteExit());
                }))
            {
                yield break;
            }
        }

        private void RunAdr004BOwnerLossProbe()
        {
            Require(routeBinding != null,
                "ADR-004B owner-loss probe requires the canonical Route binding.");
            Require(Context != null,
                "ADR-004B owner-loss probe requires the canonical CameraOutputContext.");
            Require(routeBinding.RequestOverride().Succeeded,
                "ADR-004B owner-loss setup could not publish the Route request.");
            Winner(routeBinding.RequestIdText, routeComposer,
                "adr004b-owner-loss-setup");

            string requestId = routeBinding.RequestIdText;
            CameraRequestId typedRequestId = new CameraRequestId(requestId);
            int admittedBeforeDisable = Context.AdmittedRequestCount;

            routeBinding.enabled = false;

            bool orphaned = Context.Contains(typedRequestId);
            Adr004BOwnerLossExecuted = true;
            Adr004BOwnerLossInvariantPassed = !orphaned;
            Adr004BOwnerLossDiagnostic = orphaned
                ? "Disabling the active RouteCameraOverrideBinding left its admitted Camera request in CameraOutputContext."
                : "Disabling the active RouteCameraOverrideBinding released its admitted Camera request before ownership became invalid.";

            string evidence =
                $"case='16-abnormal-owner-loss' operation='DisableRouteOwner' " +
                $"request='{requestId}' owner='Route' lifetime='Route' " +
                $"output='{outputSession.OutputIdText}' admittedBefore='{admittedBeforeDisable}' " +
                $"admittedAfter='{Context.AdmittedRequestCount}' orphan='{orphaned}' " +
                $"diagnostic='{Escape(Adr004BOwnerLossDiagnostic)}'.";

            if (orphaned)
            {
                Debug.LogError(
                    $"{Adr004BLogPrefix} status='Failed' {evidence}",
                    this);
            }
            else
            {
                Debug.Log(
                    $"{Adr004BLogPrefix} status='Passed' {evidence}",
                    this);
            }

            CameraOverrideResult cleanup = routeBinding.ReleaseOverride();
            Require(cleanup.Succeeded,
                "ADR-004B owner-loss probe could not clean its owned Route request.");
            Require(!Context.Contains(typedRequestId),
                "ADR-004B owner-loss probe cleanup left the Route request admitted.");

            routeBinding.enabled = true;
            Winner(playerBinding.RequestIdText, playerComposer,
                "adr004b-owner-loss-cleanup");
        }


        private void PublishRouteLifecycleSurvivor()
        {
            Require(outputSession != null && outputSession.Session != null,
                "ADR-004B Route lifecycle survivor requires the canonical output session.");
            Require(sessionOverride != null &&
                sessionOverride.RigComposer != null &&
                sessionOverride.TargetSource != null,
                "ADR-004B Route lifecycle survivor requires the persistent Session rig and target.");

            CameraRequestCreateResult request = CameraRequestCreateResult.Create(
                new CameraRequestId(RouteLifecycleSurvivorRequestId),
                new CameraOutputId(outputSession.OutputIdText),
                new CameraRequestOwner(
                    CameraRequestOwnerKind.Session,
                    "qa.camera.adr004b.route-lifecycle-survivor-owner"),
                new CameraRequestLifetime(
                    CameraRequestLifetimeKind.Session,
                    "qa.camera.adr004b.route-lifecycle-survivor-scope"),
                CameraRigReference.FromComposer(sessionOverride.RigComposer),
                CameraTargetSourceDescriptor.ExplicitTransform(
                    sessionOverride.TargetSource,
                    "ADR004BRouteLifecycleSurvivor"),
                new CameraRequestPolicy(150, "adr004b-route-lifecycle-survivor"),
                CameraRequestReleaseCondition.ExplicitRelease,
                nameof(QaCameraOverrideAuthorityFixture),
                "ADR-004B persistent survivor for Route lifecycle cleanup isolation.");
            Require(request.IsSucceeded,
                $"ADR-004B Route lifecycle survivor request creation failed. {request.BlockingIssue}");

            CameraRequestPublisherCreateResult publisher =
                SessionCameraRequestPublisher.Create(outputSession.Session, request.Request);
            Require(publisher.Succeeded && publisher.Publisher != null,
                $"ADR-004B Route lifecycle survivor publisher creation failed. {publisher.DiagnosticSummary}");

            routeLifecycleSurvivorPublisher = publisher.Publisher;
            CameraRequestPublisherResult publication = routeLifecycleSurvivorPublisher.Publish();
            Require(publication.Succeeded,
                $"ADR-004B Route lifecycle survivor publication failed. {publication.DiagnosticSummary}");
            Require(Context.Contains(new CameraRequestId(RouteLifecycleSurvivorRequestId)),
                "ADR-004B Route lifecycle survivor was not admitted.");
        }

        private IEnumerator WatchRouteExit()
        {
            for (int frame = 0; frame < MaxReadinessFrames; frame++)
            {
                if (!backToHubTrigger.IsRequestInFlight &&
                    backToHubTrigger.LastRequestFailed)
                {
                    Fail(
                        "Route lifecycle cleanup transition failed: " +
                        backToHubTrigger.LastMessage);
                    yield break;
                }

                yield return null;
            }

            Fail(
                "Route lifecycle cleanup did not unload the Camera route before timeout. " +
                State());
        }

        private void OnDestroy()
        {
            if (!awaitingRouteLifecycleCleanup || outputSession == null)
            {
                return;
            }

            Adr004BRouteLifecycleExecuted = true;

            if (outputSession.Context == null ||
                outputSession.Context.Contains(
                    new CameraRequestId(routeRequestId)))
            {
                Adr004BRouteLifecyclePassed = false;
                Fail(
                    "route-lifecycle-cleanup did not release the Route request. " +
                    State());
                return;
            }

            CameraRequestId survivorId =
                new CameraRequestId(RouteLifecycleSurvivorRequestId);
            if (!survivorId.IsValid ||
                !outputSession.Context.Contains(survivorId) ||
                !outputSession.Context.HasWinner ||
                outputSession.Context.Winner.RequestId != survivorId)
            {
                Adr004BRouteLifecyclePassed = false;
                Fail(
                    "route-lifecycle-cleanup did not preserve the persistent Session request while releasing the Route owner. " +
                    State());
                return;
            }

            Adr004BRouteLifecyclePassed = true;

            CameraRequestPublisherResult survivorCleanup =
                routeLifecycleSurvivorPublisher != null
                    ? routeLifecycleSurvivorPublisher.Release()
                    : default;
            if (routeLifecycleSurvivorPublisher != null && !survivorCleanup.Succeeded)
            {
                Fail(
                    "route-lifecycle-cleanup could not release the ADR-004B Session survivor request.");
                return;
            }
            routeLifecycleSurvivorPublisher = null;

            Complete("route-lifecycle-cleanup");
            if (completedCaseCount != ExpectedCaseCount)
            {
                Fail(
                    $"Camera runtime host integration case count changed. " +
                    $"expected='{ExpectedCaseCount}' actual='{completedCaseCount}'.");
                return;
            }
            lastStatus = "Passed";
            Debug.Log(
                $"{LogPrefix} status='Passed' phase='canonical-override-fixture' " +
                $"cases='{completedCaseCount}' completed='{string.Join(",", completedCases)}'.",
                this);
        }

        private static void ResetAdr004BEvidence()
        {
            Adr004BActivityLifecycleExecuted = false;
            Adr004BActivityLifecyclePassed = false;
            Adr004BRouteLifecycleExecuted = false;
            Adr004BRouteLifecyclePassed = false;
            Adr004BOwnerLossExecuted = false;
            Adr004BOwnerLossInvariantPassed = false;
            Adr004BOwnerLossDiagnostic = string.Empty;
        }

        private bool Readiness()
        {
            return outputSession != null &&
                outputSession.IsInitialized &&
                sessionOverride != null &&
                sessionOverride.IsOwnerActive &&
                playerBinding != null &&
                playerBinding.IsLocallyEligible &&
                playerBinding.IsPublished &&
                routeBinding != null &&
                activityBinding != null &&
                routeBinding.OutputSession == outputSession &&
                activityBinding.OutputSession == outputSession &&
                Context != null &&
                Context.HasWinner &&
                IsWinner(playerBinding.RequestIdText);
        }

        private CameraOutputContext Context =>
            outputSession != null ? outputSession.Context : null;
        private bool HasFailed =>
            string.Equals(lastStatus, "Failed", StringComparison.Ordinal);
        private bool IsWinner(string requestId) =>
            Context != null &&
            Context.HasWinner &&
            Context.Winner.RequestId.Value == requestId;

        private IEnumerator WaitFor(Func<bool> condition, string label)
        {
            for (int frame = 0; frame < MaxReadinessFrames; frame++)
            {
                bool completed;
                try
                {
                    completed = condition();
                }
                catch (Exception exception)
                {
                    Fail(
                        $"Readiness check '{label}' threw: {exception.Message}");
                    yield break;
                }

                if (completed)
                {
                    yield break;
                }

                yield return null;
            }

            Fail($"Timed out waiting for '{label}'. {State()}");
        }

        private bool TryStep(Action step)
        {
            try
            {
                step();
                return true;
            }
            catch (Exception exception)
            {
                Fail(exception.Message);
                return false;
            }
        }

        private void Winner(
            string requestId,
            CameraRigComposer rig,
            string step)
        {
            Require(IsWinner(requestId),
                $"Unexpected winner at '{step}'. {State()}");
            Require(
                rig != null &&
                rig.CinemachineCamera != null &&
                rig.CinemachineCamera.enabled,
                $"Expected rig is disabled at '{step}'.");
        }

        private void Complete(string name)
        {
            completedCaseCount++;
            completedCases.Add(name);
            Debug.Log(
                $"{LogPrefix} phase='canonical-override-fixture' case='{name}' status='Passed'.",
                this);
        }

        private void Fail(string reason)
        {
            if (HasFailed)
            {
                return;
            }

            lastStatus = "Failed";
            lastFailure = reason;
            Debug.LogError(
                $"{LogPrefix} status='Failed' phase='canonical-override-fixture' " +
                $"reason='{reason}' completed='{string.Join(",", completedCases)}'.",
                this);
            if (throwOnFailure)
            {
                throw new InvalidOperationException(reason);
            }
        }

        private string State()
        {
            return
                $"output='{(outputSession == null ? "<missing>" : outputSession.OutputIdText)}' " +
                $"initialized='{(outputSession != null && outputSession.IsInitialized)}' " +
                $"playerStatus='{(playerBinding == null ? "<missing>" : playerBinding.LastStatus)}' " +
                $"playerEligibility='{(playerBinding != null && playerBinding.IsLocallyEligible)}' " +
                $"playerRequest='{(playerBinding == null ? "<missing>" : playerBinding.RequestIdText)}' " +
                $"playerScope='{(playerBinding == null ? "<missing>" : playerBinding.EligibilityScopeId)}' " +
                $"routeOutputAttached='{(routeBinding != null && routeBinding.OutputSession == outputSession)}' " +
                $"activityOutputAttached='{(activityBinding != null && activityBinding.OutputSession == outputSession)}' " +
                $"requestCount='{(Context == null ? -1 : Context.AdmittedRequestCount)}' " +
                $"winner='{(Context != null && Context.HasWinner ? Context.Winner.RequestId.Value : "<none>")}'.";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        void ICameraOutputSessionConsumer.AttachOutputSession(
            CameraOutputSessionBinding binding)
        {
            outputSession = binding;
        }

        void ICameraOutputSessionConsumer.DetachOutputSession(string reason)
        {
            outputSession = null;
        }

        void ISessionCameraOverrideConsumer.AttachSessionCameraOverride(
            SessionCameraOverrideBinding binding)
        {
            sessionOverride = binding;
        }
    }
}

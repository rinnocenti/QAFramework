using System;
using System.Collections;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaC9RCameraOverrideAuthorityFixture : MonoBehaviour, ICameraOutputSessionConsumer, ISessionCameraOverrideConsumer
    {
        private const string LogPrefix = "[QA][C9R Camera Override Authority]";
        private const int MaxReadinessFrames = 600;
        [SerializeField] private RouteCameraOverrideBinding routeBinding;
        [SerializeField] private LocalPlayerCameraRequestBinding playerBinding;
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

        [ContextMenu("Run C9R Camera Override Authority Proof")]
        public void RunFromContextMenu() => Begin();
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

            yield return WaitFor(Readiness, "persistent-output-readiness");
            if (HasFailed) yield break;

            if (!TryStep(() =>
                {
                    Winner(playerBinding.RequestIdText, playerComposer, "player-default"); Complete("player-default");
                    Require(activityBinding.RequestOverride().Succeeded, "Activity request failed."); Winner(activityBinding.RequestIdText, activityComposer, "activity-request"); Complete("activity-request");
                    Require(routeBinding.RequestOverride().Succeeded, "Route request failed."); Winner(routeBinding.RequestIdText, routeComposer, "route-request"); Complete("route-request");
                    Require(sessionOverride.RequestOverride().Succeeded, "Session request failed."); Winner(sessionOverride.RequestIdText, sessionOverride.RigComposer, "session-request"); Complete("session-request");
                    Require(sessionOverride.ReleaseOverride().Succeeded, "Session release failed."); Winner(routeBinding.RequestIdText, routeComposer, "session-release-restores-route"); Complete("session-release-restores-route");
                    Require(routeBinding.ReleaseOverride().Succeeded, "Route release failed."); Winner(activityBinding.RequestIdText, activityComposer, "route-release-restores-activity"); Complete("route-release-restores-activity");
                    Require(activityBinding.ReleaseOverride().Succeeded, "Activity release failed."); Winner(playerBinding.RequestIdText, playerComposer, "activity-release-restores-player"); Complete("activity-release-restores-player");
                    Require(activityBinding.RequestOverride().Succeeded, "First duplicate request failed."); Require(activityBinding.RequestOverride().Operation == CameraOverrideOperationKind.Preserved, "Duplicate request was not preserved."); Winner(activityBinding.RequestIdText, activityComposer, "duplicate-request"); Complete("duplicate-request");
                    Require(activityBinding.ReleaseOverride().Succeeded, "First duplicate release failed."); Require(activityBinding.ReleaseOverride().Operation == CameraOverrideOperationKind.Preserved, "Duplicate release was not preserved."); Winner(playerBinding.RequestIdText, playerComposer, "duplicate-release"); Complete("duplicate-release");
                    Require(activityBinding.RequestOverride().Succeeded, "Activity lifecycle setup failed.");
                    Require(activityRequestTrigger != null, "Activity request trigger is missing.");
                    activityRequestTrigger.ClearActivity();
                }))
            {
                yield break;
            }

            yield return WaitFor(() => !activityRequestTrigger.IsRequestInFlight && activityRequestTrigger.LastRequestSucceeded, "activity-clear-request");
            if (HasFailed) yield break;

            yield return WaitFor(() => !Context.Contains(new CameraRequestId(activityBinding.RequestIdText)) && IsWinner(playerBinding.RequestIdText), "activity-lifecycle-cleanup");
            if (HasFailed) yield break;

            if (!TryStep(() =>
                {
                    Complete("activity-lifecycle-cleanup");
                    Require(routeBinding.RequestOverride().Succeeded, "Route lifecycle setup failed.");
                    Winner(routeBinding.RequestIdText, routeComposer, "route-lifecycle-setup");
                    Require(backToHubTrigger != null && backToHubTrigger.TargetRoute != null, "Back-to-Hub trigger is missing.");
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

        private IEnumerator WatchRouteExit()
        {
            for (int frame = 0; frame < MaxReadinessFrames; frame++)
            {
                if (!backToHubTrigger.IsRequestInFlight && backToHubTrigger.LastRequestFailed)
                {
                    Fail("Route lifecycle cleanup transition failed: " + backToHubTrigger.LastMessage);
                    yield break;
                }
                yield return null;
            }
            Fail("Route lifecycle cleanup did not unload the C9R route before timeout. " + State());
        }

        private void OnDestroy()
        {
            if (!awaitingRouteLifecycleCleanup || outputSession == null) return;
            if (outputSession.Context == null || outputSession.Context.Contains(new CameraRequestId(routeRequestId))) { Fail("route-lifecycle-cleanup did not release the Route request. " + State()); return; }
            Complete("route-lifecycle-cleanup"); lastStatus = "Passed"; Debug.Log($"{LogPrefix} PASS. cases='{completedCaseCount}'.", this);
        }

        private bool Readiness() => outputSession != null && outputSession.IsInitialized && sessionOverride != null && sessionOverride.IsOwnerActive && playerBinding != null && playerBinding.IsLocallyEligible && playerBinding.IsPublished && routeBinding != null && activityBinding != null && routeBinding.OutputSession == outputSession && activityBinding.OutputSession == outputSession && Context != null && Context.HasWinner && IsWinner(playerBinding.RequestIdText);
        private CameraOutputContext Context => outputSession != null ? outputSession.Context : null;
        private bool HasFailed => string.Equals(lastStatus, "Failed", StringComparison.Ordinal);
        private bool IsWinner(string requestId) => Context != null && Context.HasWinner && Context.Winner.RequestId.Value == requestId;

        private IEnumerator WaitFor(Func<bool> condition, string label)
        {
            for (int frame = 0; frame < MaxReadinessFrames; frame++)
            {
                bool completed;
                try { completed = condition(); }
                catch (Exception exception) { Fail($"Readiness check '{label}' threw: {exception.Message}"); yield break; }
                if (completed) yield break;
                yield return null;
            }
            Fail($"Timed out waiting for '{label}'. {State()}");
        }

        private bool TryStep(Action step)
        {
            try { step(); return true; }
            catch (Exception exception) { Fail(exception.Message); return false; }
        }

        private void Winner(string requestId, CameraRigComposer rig, string step) { Require(IsWinner(requestId), $"Unexpected winner at '{step}'. {State()}"); Require(rig != null && rig.CinemachineCamera != null && rig.CinemachineCamera.enabled, $"Expected rig is disabled at '{step}'."); }
        private void Complete(string name) { completedCaseCount++; Debug.Log($"{LogPrefix} case='{name}' status='PASS'.", this); }
        private void Fail(string reason) { if (HasFailed) return; lastStatus = "Failed"; lastFailure = reason; Debug.LogError($"{LogPrefix} FAIL. reason='{reason}'.", this); if (throwOnFailure) throw new InvalidOperationException(reason); }
        private string State() => $"output='{(outputSession == null ? "<missing>" : outputSession.OutputIdText)}' initialized='{(outputSession != null && outputSession.IsInitialized)}' playerStatus='{(playerBinding == null ? "<missing>" : playerBinding.LastStatus)}' playerEligibility='{(playerBinding != null && playerBinding.IsLocallyEligible)}' playerRequest='{(playerBinding == null ? "<missing>" : playerBinding.RequestIdText)}' playerScope='{(playerBinding == null ? "<missing>" : playerBinding.EligibilityScopeId)}' routeOutputAttached='{(routeBinding != null && routeBinding.OutputSession == outputSession)}' activityOutputAttached='{(activityBinding != null && activityBinding.OutputSession == outputSession)}' requestCount='{(Context == null ? -1 : Context.AdmittedRequestCount)}' winner='{(Context != null && Context.HasWinner ? Context.Winner.RequestId.Value : "<none>")}'.";
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void ICameraOutputSessionConsumer.AttachOutputSession(CameraOutputSessionBinding binding) { outputSession = binding; }
        void ICameraOutputSessionConsumer.DetachOutputSession(string reason) { outputSession = null; }
        void ISessionCameraOverrideConsumer.AttachSessionCameraOverride(SessionCameraOverrideBinding binding) { sessionOverride = binding; }
    }
}

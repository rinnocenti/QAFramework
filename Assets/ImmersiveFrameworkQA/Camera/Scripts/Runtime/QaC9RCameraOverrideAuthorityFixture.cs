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
        private const string Adr004CLogPrefix =
            "[QA_CAMERA_ADR004C]";
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

        public static bool Adr004CActivityDisableExecuted { get; private set; }
        public static bool Adr004CActivityDisablePassed { get; private set; }
        public static bool Adr004CSessionDisableExecuted { get; private set; }
        public static bool Adr004CSessionDisablePassed { get; private set; }
        public static bool Adr004CNonWinnerDisableExecuted { get; private set; }
        public static bool Adr004CNonWinnerDisablePassed { get; private set; }
        public static bool Adr004CWinningRestoreExecuted { get; private set; }
        public static bool Adr004CWinningRestorePassed { get; private set; }
        public static bool Adr004CIdempotentCleanupExecuted { get; private set; }
        public static bool Adr004CIdempotentCleanupPassed { get; private set; }
        public static bool Adr004CActivityDestroyExecuted { get; private set; }
        public static bool Adr004CActivityDestroyPassed { get; private set; }
        public static bool Adr004CRouteReenableExecuted { get; private set; }
        public static bool Adr004CRouteReenablePassed { get; private set; }

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
            ResetAdr004CEvidence();

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

                    RunAdr004CActivityDisableProbe();
                    RunAdr004CNonWinnerDisableProbe();
                    RunAdr004CSessionDisableProbe();

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

                    activityRequestTrigger.RequestActivity();
                }))
            {
                yield break;
            }

            yield return WaitFor(
                () => !activityRequestTrigger.IsRequestInFlight &&
                    activityRequestTrigger.LastRequestSucceeded &&
                    !activityRequestTrigger.LastRequestClearedActivity,
                "adr004c-activity-reenter");
            if (HasFailed)
            {
                yield break;
            }

            yield return WaitFor(
                () => activityBinding != null &&
                    activityBinding.IsOwnerActive,
                "adr004c-activity-owner-reactivation");
            if (HasFailed)
            {
                yield break;
            }

            string destroyedActivityRequestId = string.Empty;
            if (!TryStep(() =>
                {
                    Require(activityBinding.RequestOverride().Succeeded,
                        "ADR-004C Activity destruction setup could not publish the Activity request.");
                    Winner(activityBinding.RequestIdText, activityComposer,
                        "adr004c-activity-destroy-setup");
                    destroyedActivityRequestId = activityBinding.RequestIdText;
                    Destroy(activityBinding);
                }))
            {
                yield break;
            }

            yield return null;

            if (!TryStep(() =>
                {
                    CameraRequestId requestId =
                        new CameraRequestId(destroyedActivityRequestId);
                    bool removed =
                        !Context.Contains(requestId);
                    bool restoredPlayer =
                        IsWinner(playerBinding.RequestIdText);

                    Adr004CActivityDestroyExecuted = true;
                    Adr004CActivityDestroyPassed =
                        removed && restoredPlayer;

                    Debug.Log(
                        $"{Adr004CLogPrefix} case='activity-destruction' " +
                        $"status='{(Adr004CActivityDestroyPassed ? "Passed" : "Failed")}' " +
                        $"request='{destroyedActivityRequestId}' removed='{removed}' " +
                        $"restoredPlayer='{restoredPlayer}'.",
                        this);

                    if (!removed)
                    {
                        CameraOutputSessionResult cleanup =
                            outputSession.Session.Release(requestId);
                        Require(cleanup.Succeeded,
                            "ADR-004C Activity destruction fallback cleanup failed.");
                    }

                    activityRequestTrigger.ClearActivity();
                }))
            {
                yield break;
            }

            yield return WaitFor(
                () => !activityRequestTrigger.IsRequestInFlight &&
                    activityRequestTrigger.LastRequestSucceeded &&
                    activityRequestTrigger.LastRequestClearedActivity,
                "adr004c-activity-clear-after-destroy");
            if (HasFailed)
            {
                yield break;
            }

            if (!TryStep(() =>
                {
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

            bool silentlyRepublished =
                Context.Contains(typedRequestId);
            Adr004CRouteReenableExecuted = true;
            Adr004CRouteReenablePassed =
                !silentlyRepublished && routeBinding.IsOwnerActive;

            Debug.Log(
                $"{Adr004CLogPrefix} case='route-disable-reenable' " +
                $"status='{(Adr004CRouteReenablePassed ? "Passed" : "Failed")}' " +
                $"request='{requestId}' orphan='{orphaned}' " +
                $"silentRepublish='{silentlyRepublished}' " +
                $"ownerActiveAfterReenable='{routeBinding.IsOwnerActive}'.",
                this);

            if (silentlyRepublished)
            {
                CameraOverrideResult silentCleanup =
                    routeBinding.ReleaseOverride();
                Require(silentCleanup.Succeeded,
                    "ADR-004C Route silent re-publication cleanup failed.");
            }

            Winner(playerBinding.RequestIdText, playerComposer,
                "adr004b-owner-loss-cleanup");
        }

        private void RunAdr004CActivityDisableProbe()
        {
            Require(activityBinding != null,
                "ADR-004C Activity disable probe requires the canonical Activity binding.");
            Require(activityBinding.RequestOverride().Succeeded,
                "ADR-004C Activity disable setup could not publish the Activity request.");
            Winner(activityBinding.RequestIdText, activityComposer,
                "adr004c-activity-disable-setup");

            CameraRequestId requestId =
                new CameraRequestId(activityBinding.RequestIdText);
            activityBinding.enabled = false;

            bool removed = !Context.Contains(requestId);
            bool restoredPlayer = IsWinner(playerBinding.RequestIdText);

            CameraOverrideResult repeatedCleanup =
                activityBinding.ReleaseOverride();
            bool idempotent =
                repeatedCleanup.Succeeded &&
                repeatedCleanup.Operation ==
                CameraOverrideOperationKind.Preserved;

            activityBinding.enabled = true;
            bool silentRepublish = Context.Contains(requestId);
            bool ownerStillActive = activityBinding.IsOwnerActive;

            Adr004CActivityDisableExecuted = true;
            Adr004CActivityDisablePassed =
                removed &&
                !silentRepublish &&
                ownerStillActive;
            Adr004CWinningRestoreExecuted = true;
            Adr004CWinningRestorePassed =
                removed && restoredPlayer;
            Adr004CIdempotentCleanupExecuted = true;
            Adr004CIdempotentCleanupPassed = idempotent;

            Debug.Log(
                $"{Adr004CLogPrefix} case='activity-disable' " +
                $"status='{(Adr004CActivityDisablePassed ? "Passed" : "Failed")}' " +
                $"request='{requestId.Value}' removed='{removed}' " +
                $"restoredPlayer='{restoredPlayer}' silentRepublish='{silentRepublish}' " +
                $"ownerActiveAfterReenable='{ownerStillActive}' " +
                $"repeatedCleanup='{repeatedCleanup.Operation}'.",
                this);

            if (!removed || silentRepublish)
            {
                CameraOverrideResult cleanup =
                    activityBinding.ReleaseOverride();
                Require(cleanup.Succeeded,
                    "ADR-004C Activity disable fallback cleanup failed.");
            }

            Require(activityBinding.RequestOverride().Succeeded,
                "ADR-004C Activity binding could not explicitly publish after re-enable while its logical Activity owner remained active.");
            Winner(activityBinding.RequestIdText, activityComposer,
                "adr004c-activity-disable-explicit-republish");
            Require(activityBinding.ReleaseOverride().Succeeded,
                "ADR-004C Activity disable probe could not release the explicit re-publication.");
            Winner(playerBinding.RequestIdText, playerComposer,
                "adr004c-activity-disable-cleanup");
        }

        private void RunAdr004CNonWinnerDisableProbe()
        {
            Require(activityBinding.RequestOverride().Succeeded,
                "ADR-004C non-winner setup could not publish the Activity request.");
            Require(routeBinding.RequestOverride().Succeeded,
                "ADR-004C non-winner setup could not publish the Route request.");
            Winner(routeBinding.RequestIdText, routeComposer,
                "adr004c-nonwinner-route-winner");

            CameraRequestId activityRequestId =
                new CameraRequestId(activityBinding.RequestIdText);
            CameraRequestId routeRequestId =
                new CameraRequestId(routeBinding.RequestIdText);

            activityBinding.enabled = false;

            bool activityRemoved =
                !Context.Contains(activityRequestId);
            bool routePreserved =
                Context.Contains(routeRequestId) &&
                IsWinner(routeBinding.RequestIdText);

            Adr004CNonWinnerDisableExecuted = true;
            Adr004CNonWinnerDisablePassed =
                activityRemoved && routePreserved;

            Debug.Log(
                $"{Adr004CLogPrefix} case='nonwinner-disable' " +
                $"status='{(Adr004CNonWinnerDisablePassed ? "Passed" : "Failed")}' " +
                $"removed='{activityRequestId.Value}' routePreserved='{routePreserved}' " +
                $"winner='{(Context.HasWinner ? Context.Winner.RequestId.Value : "<none>")}'.",
                this);

            if (!activityRemoved)
            {
                CameraOverrideResult cleanup =
                    activityBinding.ReleaseOverride();
                Require(cleanup.Succeeded,
                    "ADR-004C non-winner Activity fallback cleanup failed.");
            }

            activityBinding.enabled = true;
            Require(!Context.Contains(activityRequestId),
                "ADR-004C Activity request silently re-published after non-winner re-enable.");

            Require(routeBinding.ReleaseOverride().Succeeded,
                "ADR-004C non-winner Route cleanup failed.");
            Winner(playerBinding.RequestIdText, playerComposer,
                "adr004c-nonwinner-cleanup");
        }

        private void RunAdr004CSessionDisableProbe()
        {
            Require(sessionOverride != null,
                "ADR-004C Session disable probe requires the persistent Session binding.");
            Require(sessionOverride.RequestOverride().Succeeded,
                "ADR-004C Session disable setup could not publish the Session request.");
            Winner(
                sessionOverride.RequestIdText,
                sessionOverride.RigComposer,
                "adr004c-session-disable-setup");

            CameraRequestId requestId =
                new CameraRequestId(sessionOverride.RequestIdText);
            sessionOverride.enabled = false;

            bool removed = !Context.Contains(requestId);
            bool restoredPlayer = IsWinner(playerBinding.RequestIdText);

            CameraOverrideResult repeatedCleanup =
                sessionOverride.ReleaseOverride();
            bool idempotent =
                repeatedCleanup.Succeeded &&
                repeatedCleanup.Operation ==
                CameraOverrideOperationKind.Preserved;

            sessionOverride.enabled = true;
            bool silentRepublish =
                Context.Contains(requestId);
            bool ownerReactivated =
                sessionOverride.IsOwnerActive;

            Adr004CSessionDisableExecuted = true;
            Adr004CSessionDisablePassed =
                removed &&
                restoredPlayer &&
                !silentRepublish &&
                ownerReactivated;
            Adr004CIdempotentCleanupPassed &=
                idempotent;

            Debug.Log(
                $"{Adr004CLogPrefix} case='session-disable' " +
                $"status='{(Adr004CSessionDisablePassed ? "Passed" : "Failed")}' " +
                $"request='{requestId.Value}' removed='{removed}' restoredPlayer='{restoredPlayer}' " +
                $"silentRepublish='{silentRepublish}' ownerActiveAfterReenable='{ownerReactivated}' " +
                $"repeatedCleanup='{repeatedCleanup.Operation}'.",
                this);

            if (!removed || silentRepublish)
            {
                CameraOverrideResult cleanup =
                    sessionOverride.ReleaseOverride();
                Require(cleanup.Succeeded,
                    "ADR-004C Session disable fallback cleanup failed.");
            }

            Require(sessionOverride.RequestOverride().Succeeded,
                "ADR-004C Session binding could not explicitly publish after re-enable.");
            Winner(
                sessionOverride.RequestIdText,
                sessionOverride.RigComposer,
                "adr004c-session-disable-explicit-republish");
            Require(sessionOverride.ReleaseOverride().Succeeded,
                "ADR-004C Session disable probe could not release the explicit re-publication.");
            Winner(playerBinding.RequestIdText, playerComposer,
                "adr004c-session-disable-cleanup");
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
                !outputSession.Context.Contains(survivorId))
            {
                Adr004BRouteLifecyclePassed = false;
                Fail(
                    "route-lifecycle-cleanup did not preserve the persistent Session survivor while releasing the Route owner. " +
                    State());
                return;
            }

            // Route cleanup owns only the Route request. The transition boundary
            // may legitimately re-publish the canonical Session override at a
            // higher precedence, so survivor preservation must not require the
            // synthetic Session survivor to become the current winner.
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

        private static void ResetAdr004CEvidence()
        {
            Adr004CActivityDisableExecuted = false;
            Adr004CActivityDisablePassed = false;
            Adr004CSessionDisableExecuted = false;
            Adr004CSessionDisablePassed = false;
            Adr004CNonWinnerDisableExecuted = false;
            Adr004CNonWinnerDisablePassed = false;
            Adr004CWinningRestoreExecuted = false;
            Adr004CWinningRestorePassed = false;
            Adr004CIdempotentCleanupExecuted = false;
            Adr004CIdempotentCleanupPassed = true;
            Adr004CActivityDestroyExecuted = false;
            Adr004CActivityDestroyPassed = false;
            Adr004CRouteReenableExecuted = false;
            Adr004CRouteReenablePassed = false;
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

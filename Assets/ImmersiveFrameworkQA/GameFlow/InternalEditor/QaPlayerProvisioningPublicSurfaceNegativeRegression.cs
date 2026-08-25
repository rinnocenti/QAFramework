using System;
using ImmersiveFrameworkQA.Player;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.Lifecycle;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// QA-PLAYER-SURFACE-02 — Negative, stale-scope and lifecycle hardening for
    /// the public Manager-Provisioned Player consumer surface.
    ///
    /// Q1 remains the positive baseline. This runner certifies rejection
    /// semantics, scope lifetime, exit/reentry and late-observation isolation
    /// using only public typed evidence for the Player contract under test.
    /// </summary>
    public static class QaPlayerProvisioningPublicSurfaceNegativeRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Public Surface/" +
            "Run Negative Contract";
        private const string Prefix = "[QA_PLAYER_SURFACE_02]";
        private const string Source =
            nameof(QaPlayerProvisioningPublicSurfaceNegativeRegression);
        private const int FrameBudget = 360;
        private const int ExpectedCaseCount = 36;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "runtime-started",
            "consumer-binding-authored",
            "scoped-access-available",
            "fresh-session-confirmed",
            "join-rejected-joining-closed",
            "open-joining-succeeded",
            "open-joining-no-change",
            "supported-slot-universe-confirmed",
            "first-join-uses-first-supported-slot",
            "close-joining-succeeded",
            "close-joining-no-change",
            "missing-binding-command-unavailable",
            "wrong-scope-no-fallback",
            "activity-entry-waiting",
            "activity-scope-bound",
            "exit-while-waiting-for-join",
            "stale-activity-endpoint-after-exit",
            "reentry-after-waiting-exit",
            "join-select-for-lifecycle",
            "capture-occurrence-a",
            "exit-after-join-session-persists",
            "reentry-newer-occurrence",
            "old-occurrence-not-current",
            "no-duplicate-slot-actor",
            "stale-selection-revision-rejected",
            "repeated-default-selection-stable",
            "second-join-uses-next-supported-slot",
            "session-full-rejected-no-available-slot",
            "public-activity-trigger-unbound-fails",
            "public-navigation-disposition",
            "destroyed-binding-released",
            "stale-route-endpoint-after-destroy",
            "fixture-cleaned",
            "public-scan-clean"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            await RunAsync();
        }

        /// <summary>
        /// Entry point for the joint certification orchestrator.
        /// </summary>
        public static Task RunCertificationAsync() => RunAsync();

        private static async Task RunAsync()
        {
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            var failures = new QaFailureCollector();
            FrameworkRuntimeHost host = null;
            QaActivityEntryReadinessFixture fixture = null;
            QaPlayerSurfacePublicNavigationFixture publicNav = null;
            LocalPlayerProvisioningConsumerAccessBinding routeBinding = null;
            ILocalPlayerProvisioningConsumerAccess routeAccess = null;
            ILocalPlayerProvisioningConsumerAccess activityAccess = null;
            ILocalPlayerProvisioningConsumerAccess destroyedAccess = null;
            PlayerParticipationOperationResult staleDestroyedOpen = null;
            bool destroyedBindingReleasedAtDestruction = false;
            bool destroyedObservationUnavailableAtDestruction = false;
            LocalPlayerActorSelectionRequestAuthoring actorSelection = null;
            LocalPlayerJoinResult joined = null;
            LocalPlayerHostAuthoring joinedHost = null;
            PlayerSlotId joinedSlotId = default;
            int occurrenceA = 0;
            int sessionRevisionFloor = 0;
            int selectionRevisionAtCapture = 0;
            bool joiningOpen = false;
            string publicNavigationDisposition = "unresolved";
            var ownedWaiting =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-player-surface-02-waiting");
            var ownedReentryWaiting =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-player-surface-02-reentry-waiting");
            var ownedLifecycle =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-player-surface-02-lifecycle");
            var ownedLifecycleReentry =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-player-surface-02-lifecycle-reentry");

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "QA-PLAYER-SURFACE-02 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaPlayerSurfacePublicNavigationSetup.RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                await QaH2FrameworkReadiness.RequireStartedRouteAsync(
                    host,
                    FrameBudget);
                cases.Complete("runtime-started");

                Require(
                    QaPlayerSurfacePublicNavigationSupport.TryResolveGlobalUiFixture(
                        out QaPlayerSurfaceGlobalUiFixture globalUiFixture,
                        out string globalUiFixtureDiagnostic),
                    globalUiFixtureDiagnostic);
                Require(
                    QaPlayerSurfacePublicNavigationSupport.TryResolveAuthoredFixture(
                        out publicNav,
                        out string publicNavDiagnostic),
                    publicNavDiagnostic);
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireProvisioningRuntimeReadyAsync(
                        globalUiFixture,
                        FrameBudget);
                PlayerSlotProfile slotProfile = ResolveLocalPlayerSlot(
                    0,
                    "first");
                PlayerSlotProfile waitingSlotProfile = ResolveLocalPlayerSlot(
                    1,
                    "second");
                Require(
                    slotProfile != null &&
                    slotProfile.PlayerSlotId.IsValid &&
                    slotProfile.DefaultActorProfile != null &&
                    waitingSlotProfile != null &&
                    waitingSlotProfile.PlayerSlotId.IsValid &&
                    waitingSlotProfile.DefaultActorProfile != null &&
                    waitingSlotProfile.PlayerSlotId != slotProfile.PlayerSlotId,
                    "QA-PLAYER-SURFACE-02 requires two identity-distinct configured " +
                    "Local Player Slots with default Actors.");

                routeBinding = publicNav.RouteConsumerBinding;
                Require(
                    routeBinding != null &&
                    routeBinding.Scope ==
                        LocalPlayerProvisioningConsumerScope.Route,
                    "Prepared Player Surface fixture has no authored Route consumer binding.");
                cases.Complete("consumer-binding-authored");

                routeAccess = await AwaitScopedAccessAsync(
                    routeBinding,
                    FrameBudget);
                cases.Complete("scoped-access-available");

                LocalPlayerProvisioningConsumerObservationSnapshot initial =
                    RequireObservation(routeAccess, "initial");
                Require(
                    initial.Participation != null &&
                    initial.Participation.IsInitialized &&
                    initial.Participation.JoinedCount == 0,
                    "QA-PLAYER-SURFACE-02 is one-shot. Enter a fresh Play Mode with no joined Players. " +
                    DescribeObservation(initial));
                sessionRevisionFloor = initial.SessionRevision;
                cases.Complete("fresh-session-confirmed");

                // Capture the authored Route endpoint while Route composition is
                // still authoritative. Later temporary Activity compositions may
                // legitimately classify scene roots against Activity scope, so
                // they cannot establish the pre-destruction Route evidence.
                LocalPlayerProvisioningConsumerAccessBinding destroyBinding =
                    publicNav.DestroyProbeBinding;
                Require(
                    destroyBinding != null &&
                    destroyBinding.Scope ==
                        LocalPlayerProvisioningConsumerScope.Route,
                    "Prepared Player Surface fixture has no authored Route-scoped " +
                    "destroy probe binding.");
                destroyedAccess = await AwaitScopedAccessAsync(
                    destroyBinding,
                    FrameBudget);
                Require(
                    destroyedAccess.Snapshot.IsAvailable,
                    "Destroy-probe consumer access was not available before destruction.");
                UnityEngine.Object.Destroy(destroyBinding.gameObject);
                await AwaitFramesAsync(4);
                destroyedBindingReleasedAtDestruction =
                    destroyedAccess.Snapshot.IsDisposed ||
                    !destroyedAccess.Snapshot.IsAvailable;
                staleDestroyedOpen = destroyedAccess.OpenJoining(
                    Source,
                    "qa-player-surface-02-stale-open");
                destroyedObservationUnavailableAtDestruction =
                    !destroyedAccess.TryGetObservation(
                        out LocalPlayerProvisioningConsumerObservationSnapshot
                            destroyedObservation) ||
                    destroyedObservation == null ||
                    !destroyedObservation.IsAvailable;

                // --- Command negatives (closed / no-available-slot / invalid / no-change) ---

                // Normalize to closed so the closed-join rejection is deterministic
                // regardless of authored Initial Joining intent.
                if (initial.Participation.JoiningOpen)
                {
                    PlayerParticipationOperationResult normalizeClose =
                        routeAccess.CloseJoining(
                            Source,
                            "qa-player-surface-02-normalize-closed");
                    Require(
                        normalizeClose != null &&
                        normalizeClose.Completed &&
                        normalizeClose.Snapshot != null &&
                        !normalizeClose.Snapshot.JoiningOpen,
                        normalizeClose != null
                            ? normalizeClose.ToDiagnosticString()
                            : "Could not normalize Joining closed.");
                    joiningOpen = false;
                }

                LocalPlayerJoinResult closedJoin = routeAccess.RequestJoin(
                    new LocalPlayerJoinRequest(
                        Source,
                        "qa-player-surface-02-join-while-closed"));
                Require(
                    closedJoin != null &&
                    closedJoin.Status ==
                        LocalPlayerJoinStatus.RejectedJoiningClosed &&
                    !closedJoin.Succeeded,
                    closedJoin != null
                        ? closedJoin.ToDiagnosticString()
                        : "Join-while-closed returned no public result.");
                LocalPlayerProvisioningConsumerObservationSnapshot afterClosedJoin =
                    RequireObservation(routeAccess, "after-closed-join");
                Require(
                    afterClosedJoin.Participation.JoinedCount == 0 &&
                    afterClosedJoin.SessionRevision >= sessionRevisionFloor,
                    "Join-while-closed mutated Session occupancy. " +
                    DescribeObservation(afterClosedJoin));
                cases.Complete("join-rejected-joining-closed");

                PlayerParticipationOperationResult open =
                    routeAccess.OpenJoining(
                        Source,
                        "qa-player-surface-02-open-joining");
                Require(
                    open != null &&
                    open.Succeeded &&
                    open.Snapshot != null &&
                    open.Snapshot.JoiningOpen,
                    open != null ? open.ToDiagnosticString() : "OpenJoining failed.");
                joiningOpen = true;
                cases.Complete("open-joining-succeeded");

                PlayerParticipationOperationResult openAgain =
                    routeAccess.OpenJoining(
                        Source,
                        "qa-player-surface-02-open-joining-repeat");
                Require(
                    openAgain != null &&
                    openAgain.IgnoredNoChange &&
                    openAgain.Completed &&
                    !openAgain.StateChanged &&
                    openAgain.Snapshot != null &&
                    openAgain.Snapshot.JoiningOpen &&
                    openAgain.CurrentRevision == openAgain.PreviousRevision,
                    openAgain != null
                        ? openAgain.ToDiagnosticString()
                        : "Repeated OpenJoining returned no public result.");
                cases.Complete("open-joining-no-change");

                int configuredSlots = open.Snapshot.ConfiguredSlotCount;
                Require(
                    configuredSlots >= 2,
                    "Session must expose two Supported Slots for the full-Session proof.");
                Require(
                    open.Snapshot.AvailableCount == configuredSlots &&
                    open.Snapshot.JoinedCount == 0,
                    "Supported Slot universe was not fully Available before Join. " +
                    DescribeObservation(RequireObservation(
                        routeAccess,
                        "supported-slot-universe")));
                cases.Complete("supported-slot-universe-confirmed");

                joined = routeAccess.RequestJoin(
                    new LocalPlayerJoinRequest(
                        Source,
                        "qa-player-surface-02-first-join"));
                Require(
                    joined != null &&
                    joined.Succeeded &&
                    joined.Slot.IsJoined &&
                    joined.HasLocalPlayerHostEvidence,
                    joined != null
                        ? joined.ToDiagnosticString()
                        : "First public Join returned no result.");
                joinedHost = joined.LocalPlayerHost;
                joinedSlotId = joined.Slot.PlayerSlotId;
                sessionRevisionFloor = Math.Max(
                    sessionRevisionFloor,
                    RequireObservation(routeAccess, "after-first-join")
                        .SessionRevision);
                cases.Complete("first-join-uses-first-supported-slot");

                PlayerParticipationOperationResult close =
                    routeAccess.CloseJoining(
                        Source,
                        "qa-player-surface-02-close-joining");
                Require(
                    close != null &&
                    close.Succeeded &&
                    close.Snapshot != null &&
                    !close.Snapshot.JoiningOpen,
                    close != null
                        ? close.ToDiagnosticString()
                        : "CloseJoining returned no result.");
                joiningOpen = false;
                cases.Complete("close-joining-succeeded");

                PlayerParticipationOperationResult closeAgain =
                    routeAccess.CloseJoining(
                        Source,
                        "qa-player-surface-02-close-joining-repeat");
                Require(
                    closeAgain != null &&
                    closeAgain.IgnoredNoChange &&
                    closeAgain.Completed &&
                    !closeAgain.StateChanged &&
                    closeAgain.Snapshot != null &&
                    !closeAgain.Snapshot.JoiningOpen &&
                    closeAgain.CurrentRevision == closeAgain.PreviousRevision,
                    closeAgain != null
                        ? closeAgain.ToDiagnosticString()
                        : "Repeated CloseJoining returned no public result.");
                cases.Complete("close-joining-no-change");

                // --- Missing binding / wrong scope ---

                PlayerSessionCommandTrigger unboundTrigger =
                    new GameObject(
                            "QA_PLAYER_SURFACE_02_UnboundCommand")
                        .AddComponent<PlayerSessionCommandTrigger>();
                try
                {
                    Require(
                        !unboundTrigger.TryValidateConfiguration(
                            out string unboundIssue) &&
                        !string.IsNullOrWhiteSpace(unboundIssue),
                        "Missing consumer binding must fail public command validation.");
                    unboundTrigger.InvokeConfiguredOperation();
                    Require(
                        unboundTrigger.LastResultKind ==
                            PlayerProvisioningCommandResultKind
                                .ParticipationOperation &&
                        unboundTrigger.LastParticipationResult != null &&
                        unboundTrigger.LastParticipationResult.Rejected &&
                        unboundTrigger.LastParticipationResult.Status ==
                            PlayerParticipationOperationStatus
                                .RejectedInvalidState,
                        "Missing binding did not produce an explicit public unavailable/rejected command result. " +
                        unboundTrigger.LastDiagnostic);
                    cases.Complete("missing-binding-command-unavailable");
                }
                finally
                {
                    UnityEngine.Object.Destroy(unboundTrigger.gameObject);
                }

                LocalPlayerProvisioningConsumerAccessBinding wrongBinding =
                    publicNav.WrongScopeBinding;
                Require(
                    wrongBinding != null &&
                    wrongBinding.Scope ==
                        LocalPlayerProvisioningConsumerScope.Activity,
                    "Prepared Player Surface fixture has no authored wrong-scope binding.");
                await AwaitFramesAsync(8);
                ILocalPlayerProvisioningConsumerAccess wrongAccess = null;
                string wrongIssue = string.Empty;
                bool wrongAccessAvailable =
                    wrongBinding.IsBound &&
                    wrongBinding.TryGetAccess(
                        out wrongAccess,
                        out wrongIssue);
                Require(
                    wrongAccessAvailable &&
                    wrongAccess != null &&
                    wrongAccess.Snapshot.IsAvailable &&
                    wrongAccess.Snapshot.Scope ==
                        LocalPlayerProvisioningConsumerScope.Activity &&
                    wrongAccess.Snapshot.Owner.IsValid,
                    "Activity-scoped binding on Route content must bind only to the " +
                    "live Activity authority and never fall back to Route authority. " +
                    $"state='{wrongBinding.BindingState}' scope='{wrongAccess?.Snapshot.Scope}' " +
                    $"owner='{wrongAccess?.Snapshot.Owner.StableText}' issue='{wrongIssue}' " +
                    $"diagnostic='{wrongBinding.Diagnostic}'.");
                cases.Complete("wrong-scope-no-fallback");

                // --- Activity lifecycle: exit while WaitingForJoin ---

                fixture = await QaActivityEntryReadinessFixture.CreateAsync();
                fixture.ExpectParticipantPreparationCycles(4);
                ActivityAsset waitingActivity = fixture.CreateActivity(
                    "qa.player.surface.02.waiting",
                    "QA Player Surface 02 Waiting",
                    ActivityEntryReadinessPolicy.WaitCovered,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaPlayerSurfacePublicNavigationSetup.ContentScenePath);
                ConfigurePlayerParticipation(
                    waitingActivity,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    waitingSlotProfile);

                ownedWaiting.Attach(
                    fixture.Activities.RequestActivityAsync(
                        waitingActivity,
                        Source,
                        "qa-player-surface-02-waiting-entry"));
                LocalPlayerProvisioningConsumerObservationSnapshot waiting =
                    await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.HasCurrentActivityOccurrence &&
                            observation.Lifecycle.Status ==
                                ManagerProvisionedPlayerLifecycleStatus
                                    .WaitingForJoin &&
                            observation.Lifecycle.GateHeld &&
                            observation.Participation.JoinedCount == 1 &&
                            HasJoinedSlot(observation, joinedSlotId) &&
                            !HasJoinedSlot(
                                observation,
                                waitingSlotProfile.PlayerSlotId) &&
                            ProjectsOnly(
                                observation.Lifecycle,
                                waitingSlotProfile),
                        "WaitingForJoin was not publicly observed",
                        FrameBudget);
                occurrenceA = waiting.ActivityOccurrence;
                cases.Complete("activity-entry-waiting");

                LocalPlayerProvisioningConsumerAccessBinding activityBinding =
                    await ResolveAuthoredActivityBindingWhenContentLoadedAsync();
                activityAccess = await AwaitScopedAccessAsync(
                    activityBinding,
                    FrameBudget);
                LocalPlayerProvisioningConsumerObservationSnapshot activityObservation =
                    RequireObservation(activityAccess, "activity-scope");
                Require(
                    activityObservation.IsAvailable &&
                    activityObservation.ActivityOccurrence == occurrenceA,
                    "Activity-scoped observation did not correlate to the current occurrence. " +
                    DescribeObservation(activityObservation));
                cases.Complete("activity-scope-bound");

                FrameworkActivityRequestResult clearWaiting =
                    await fixture.Activities.ClearActivityAsync(
                        Source,
                        "qa-player-surface-02-exit-waiting");
                Require(
                    clearWaiting.Succeeded,
                    string.IsNullOrWhiteSpace(clearWaiting.Message)
                        ? "Exit while WaitingForJoin failed."
                        : clearWaiting.Message);

                LocalPlayerProvisioningConsumerObservationSnapshot releasedWaiting =
                    await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.Lifecycle.IsReleased &&
                            observation.Participation.JoinedCount == 1,
                        "Exit while WaitingForJoin did not release Activity evidence while preserving Session join",
                        FrameBudget);
                Require(
                    joinedHost != null &&
                    joinedHost.IsJoined &&
                    releasedWaiting.SessionRevision >= sessionRevisionFloor,
                    "Exit while WaitingForJoin did not preserve Session-owned join/Host. " +
                    DescribeObservation(releasedWaiting));
                cases.Complete("exit-while-waiting-for-join");

                await AwaitFramesAsync(4);
                Require(
                    !activityAccess.Snapshot.IsAvailable ||
                    activityAccess.Snapshot.IsDisposed,
                    "Activity-scoped endpoint remained available after Activity exit/replacement. " +
                    activityAccess.Snapshot.Diagnostic);
                LocalPlayerJoinResult staleActivityJoin =
                    activityAccess.RequestJoin(
                        new LocalPlayerJoinRequest(
                            Source,
                            "qa-player-surface-02-stale-activity-join"));
                Require(
                    staleActivityJoin != null &&
                    staleActivityJoin.Status ==
                        LocalPlayerJoinStatus.RejectedRuntimeUnavailable &&
                    !staleActivityJoin.Succeeded,
                    staleActivityJoin != null
                        ? staleActivityJoin.ToDiagnosticString()
                        : "Stale Activity endpoint Join returned no result.");
                Require(
                    !activityAccess.TryGetObservation(
                        out LocalPlayerProvisioningConsumerObservationSnapshot staleObs) ||
                    staleObs == null ||
                    !staleObs.IsAvailable,
                    "Stale Activity endpoint returned a valid current observation after scope exit.");
                cases.Complete("stale-activity-endpoint-after-exit");

                ownedReentryWaiting.Attach(
                    fixture.Activities.RequestActivityAsync(
                        waitingActivity,
                        Source,
                        "qa-player-surface-02-waiting-reentry"));
                LocalPlayerProvisioningConsumerObservationSnapshot reenteredWaiting =
                    await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.HasCurrentActivityOccurrence &&
                            observation.ActivityOccurrence > occurrenceA &&
                            observation.Lifecycle.Status ==
                                ManagerProvisionedPlayerLifecycleStatus
                                    .WaitingForJoin &&
                            observation.Lifecycle.GateHeld &&
                            observation.Participation.JoinedCount == 1 &&
                            HasJoinedSlot(observation, joinedSlotId) &&
                            ProjectsOnly(
                                observation.Lifecycle,
                                waitingSlotProfile),
                        "Reentry after WaitingForJoin exit did not create a newer occurrence with Session join",
                        FrameBudget);
                Require(
                    reenteredWaiting.ActivityOccurrence > occurrenceA &&
                    reenteredWaiting.SessionRevision >= sessionRevisionFloor,
                    "Reentry after waiting exit lost occurrence or Session revision floor. " +
                    DescribeObservation(reenteredWaiting));
                cases.Complete("reentry-after-waiting-exit");

                FrameworkActivityRequestResult clearReentryWaiting =
                    await fixture.Activities.ClearActivityAsync(
                        Source,
                        "qa-player-surface-02-clear-reentry-waiting");
                Require(
                    clearReentryWaiting.Succeeded,
                    clearReentryWaiting.Message);
                await AwaitObservationAsync(
                    routeAccess,
                    observation =>
                        observation.IsAvailable &&
                        observation.Lifecycle.IsReleased,
                    "Second waiting Activity did not release after clear",
                    FrameBudget);

                // --- Join/select, capture occurrence, exit, reentry isolation ---

                PlayerParticipationOperationResult reopen =
                    routeAccess.OpenJoining(
                        Source,
                        "qa-player-surface-02-reopen-for-lifecycle");
                Require(
                    reopen != null && reopen.Completed &&
                    reopen.Snapshot != null && reopen.Snapshot.JoiningOpen,
                    reopen != null
                        ? reopen.ToDiagnosticString()
                        : "Re-open joining failed.");
                joiningOpen = true;

                // Lifecycle path reuses the existing joined Slot and the single
                // fixture-owned Activity asset (CreateActivity is one-shot).
                // Reconfigure only while inactive, matching the canonical
                // participation projection: the waiting occurrences project unjoined P2,
                // while lifecycle/reentry project the joined P1.
                ActivityAsset lifecycleActivity = waitingActivity;
                ConfigurePlayerParticipation(
                    lifecycleActivity,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    slotProfile);

                actorSelection = await QaPlayerSurfacePublicNavigationSupport
                    .RequireActorSelectionRuntimeReadyAsync(
                        globalUiFixture,
                        FrameBudget);

                LocalPlayerProvisioningConsumerObservationSnapshot preSelect =
                    RequireObservation(routeAccess, "pre-select");
                int currentSelectionRevision =
                    FindSlot(preSelect.Participation, joinedSlotId)
                        .SelectionRevision;
                PlayerActorSelectionResult selected =
                    actorSelection.RequestDefaultActorSelection(
                        joinedSlotId,
                        currentSelectionRevision,
                        Source,
                        "qa-player-surface-02-default-selection");
                Require(
                    selected != null &&
                    selected.Succeeded &&
                    selected.Slot.HasSelectedActor,
                    selected != null
                        ? selected.ToDiagnosticString()
                        : "Default Actor selection returned no result.");
                cases.Complete("join-select-for-lifecycle");

                int lifecyclePrepExpected =
                    fixture.PreparationStartedCount + 1;
                ownedLifecycle.Attach(
                    fixture.Activities.RequestActivityAsync(
                        lifecycleActivity,
                        Source,
                        "qa-player-surface-02-lifecycle-entry"));
                LocalPlayerProvisioningConsumerObservationSnapshot lifecyclePending =
                    await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.HasCurrentActivityOccurrence &&
                            observation.Participation.JoinedCount == 1,
                        "Lifecycle Activity entry did not expose joined Session Slot",
                        FrameBudget);
                occurrenceA = lifecyclePending.ActivityOccurrence;

                await AwaitParticipantCycleAsync(
                    fixture,
                    ownedLifecycle,
                    lifecyclePrepExpected,
                    FrameBudget);
                if (fixture.Participant.State ==
                    ActivityReadinessParticipantState.Preparing)
                {
                    fixture.Participant.CompletePreparation();
                }

                LocalPlayerProvisioningConsumerObservationSnapshot occurrenceSnapshotA =
                    await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.ActivityOccurrence == occurrenceA &&
                            HasSelectedActor(
                                observation,
                                joinedSlotId,
                                slotProfile.DefaultActorProfile),
                        "Occurrence A selected Actor was not publicly observable",
                        FrameBudget);
                selectionRevisionAtCapture =
                    FindSlot(
                            occurrenceSnapshotA.Participation,
                            joinedSlotId)
                        .SelectionRevision;
                sessionRevisionFloor = Math.Max(
                    sessionRevisionFloor,
                    occurrenceSnapshotA.SessionRevision);
                int appliedRevisionFloor =
                    occurrenceSnapshotA.AppliedSessionRevision;
                cases.Complete("capture-occurrence-a");

                FrameworkActivityRequestResult lifecycleTerminal =
                    await ownedLifecycle.AwaitTerminalAsync();
                Require(
                    lifecycleTerminal.Succeeded,
                    string.IsNullOrWhiteSpace(lifecycleTerminal.Message)
                        ? "Lifecycle Activity entry did not succeed."
                        : lifecycleTerminal.Message);

                // Exit after the canonical Actor progression reaches its request
                // terminal while Activity-owned projection is still live.
                FrameworkActivityRequestResult clearLifecycle =
                    await fixture.Activities.ClearActivityAsync(
                        Source,
                        "qa-player-surface-02-exit-after-join");
                Require(
                    clearLifecycle.Succeeded,
                    string.IsNullOrWhiteSpace(clearLifecycle.Message)
                        ? "Exit after join/select failed."
                        : clearLifecycle.Message);

                LocalPlayerProvisioningConsumerObservationSnapshot releasedLifecycle =
                    await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.Lifecycle.IsReleased &&
                            observation.Lifecycle.SlotCount == 0 &&
                            observation.Participation.JoinedCount == 1 &&
                            observation.Lifecycle.HostCount >= 1,
                        "Exit after join did not release Activity-owned projection while preserving Session",
                        FrameBudget);
                Require(
                    joinedHost != null &&
                    joinedHost.IsJoined &&
                    CountActors(joinedHost) == 1 &&
                    releasedLifecycle.SessionRevision >= sessionRevisionFloor &&
                    releasedLifecycle.AppliedSessionRevision >= appliedRevisionFloor,
                    "Exit after join destroyed the Session-owned physical Actor, lost Session Host/join or regressed revisions. " +
                    DescribeObservation(releasedLifecycle));
                // Immutable capture must retain occurrence A facts; it is not the live view.
                Require(
                    occurrenceSnapshotA.ActivityOccurrence == occurrenceA,
                    "Captured occurrence A snapshot lost its immutable identity.");
                cases.Complete("exit-after-join-session-persists");

                int reentryPrepExpected =
                    fixture.PreparationStartedCount + 1;
                ownedLifecycleReentry.Attach(
                    fixture.Activities.RequestActivityAsync(
                        lifecycleActivity,
                        Source,
                        "qa-player-surface-02-lifecycle-reentry"));
                LocalPlayerProvisioningConsumerObservationSnapshot reentry =
                    await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.HasCurrentActivityOccurrence &&
                            observation.ActivityOccurrence > occurrenceA &&
                            observation.Participation.JoinedCount == 1 &&
                            HasJoinedSlot(observation, joinedSlotId),
                        "Lifecycle reentry did not expose a newer occurrence",
                        FrameBudget);
                Require(
                    reentry.ActivityOccurrence > occurrenceA &&
                    reentry.SessionRevision >= sessionRevisionFloor,
                    "Lifecycle reentry regressed occurrence or Session revision. " +
                    DescribeObservation(reentry));
                cases.Complete("reentry-newer-occurrence");

                Require(
                    occurrenceSnapshotA.ActivityOccurrence !=
                        reentry.ActivityOccurrence &&
                    reentry.Lifecycle.ActivityOccurrence ==
                        reentry.ActivityOccurrence,
                    "Current observation presented old occurrence as current. " +
                    $"old='{occurrenceSnapshotA.ActivityOccurrence}' " +
                    $"current='{reentry.ActivityOccurrence}'.");
                // Old immutable snapshot must not be treated as the live Activity state.
                Require(
                    !reentry.Lifecycle.IsReleased ||
                    reentry.HasCurrentActivityOccurrence,
                    "Current observation lost live Activity correlation after reentry.");
                cases.Complete("old-occurrence-not-current");

                await AwaitParticipantCycleAsync(
                    fixture,
                    ownedLifecycleReentry,
                    reentryPrepExpected,
                    FrameBudget);
                if (fixture.Participant.State ==
                    ActivityReadinessParticipantState.Preparing)
                {
                    fixture.Participant.CompletePreparation();
                }

                LocalPlayerProvisioningConsumerObservationSnapshot reentryReady =
                    await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.ActivityOccurrence ==
                                reentry.ActivityOccurrence &&
                            observation.Participation.JoinedCount == 1 &&
                            HasSelectedActor(
                                observation,
                                joinedSlotId,
                                slotProfile.DefaultActorProfile),
                        "Reentry did not restore selected Actor on the same Session Slot",
                        FrameBudget);
                Require(
                    reentryReady.Participation.JoinedCount == 1 &&
                    joinedHost != null &&
                    joinedHost.IsJoined &&
                    CountActors(joinedHost) <= 1,
                    "Reentry duplicated Slot/Host/Actor evidence. " +
                    DescribeObservation(reentryReady));
                cases.Complete("no-duplicate-slot-actor");

                FrameworkActivityRequestResult lifecycleReentryTerminal =
                    await ownedLifecycleReentry.AwaitTerminalAsync();
                Require(
                    lifecycleReentryTerminal.Succeeded,
                    string.IsNullOrWhiteSpace(lifecycleReentryTerminal.Message)
                        ? "Lifecycle reentry did not succeed."
                        : lifecycleReentryTerminal.Message);

                FrameworkActivityRequestResult releaseBeforeSelectionChecks =
                    await fixture.Activities.ClearActivityAsync(
                        Source,
                        "qa-player-surface-02-release-before-selection-checks");
                Require(
                    releaseBeforeSelectionChecks.Succeeded,
                    string.IsNullOrWhiteSpace(
                        releaseBeforeSelectionChecks.Message)
                        ? "Could not release lifecycle reentry before selection checks."
                        : releaseBeforeSelectionChecks.Message);
                LocalPlayerProvisioningConsumerObservationSnapshot
                    selectionCheckObservation = await AwaitObservationAsync(
                        routeAccess,
                        observation =>
                            observation.IsAvailable &&
                            observation.Lifecycle.IsReleased &&
                            observation.Lifecycle.SlotCount == 0 &&
                            observation.Participation.JoinedCount == 1 &&
                            HasSelectedActor(
                                observation,
                                joinedSlotId,
                                slotProfile.DefaultActorProfile) &&
                            CountActors(joinedHost) == 1,
                        "Lifecycle reentry destroyed its Session-owned physical Actor before selection checks",
                        FrameBudget);

                // Stale Actor selection revision against the current Slot.
                int liveSelectionRevision =
                    FindSlot(
                        selectionCheckObservation.Participation,
                        joinedSlotId)
                        .SelectionRevision;
                int staleRevision = Math.Max(0, liveSelectionRevision - 1);
                if (staleRevision == liveSelectionRevision)
                {
                    // Ensure a non-matching expected revision.
                    staleRevision = liveSelectionRevision + 17;
                }

                PlayerActorSelectionResult staleSelection =
                    actorSelection.RequestDefaultActorSelection(
                        joinedSlotId,
                        staleRevision,
                        Source,
                        "qa-player-surface-02-stale-selection");
                Require(
                    staleSelection != null &&
                    staleSelection.Status ==
                        PlayerActorSelectionStatus
                            .RejectedStaleSelectionRevision &&
                    !staleSelection.Succeeded &&
                    staleSelection.SelectionRevision == liveSelectionRevision,
                    staleSelection != null
                        ? staleSelection.ToDiagnosticString()
                        : "Stale Actor selection returned no public result.");
                LocalPlayerProvisioningConsumerObservationSnapshot afterStaleSelection =
                    RequireObservation(routeAccess, "after-stale-selection");
                Require(
                    FindSlot(afterStaleSelection.Participation, joinedSlotId)
                        .SelectionRevision == liveSelectionRevision &&
                    afterStaleSelection.SessionRevision >= sessionRevisionFloor,
                    "Stale Actor selection mutated selection revision or Session revision. " +
                    DescribeObservation(afterStaleSelection));
                cases.Complete("stale-selection-revision-rejected");

                PlayerActorSelectionResult repeatedSelection =
                    actorSelection.RequestDefaultActorSelection(
                        joinedSlotId,
                        liveSelectionRevision,
                        Source,
                        "qa-player-surface-02-repeat-selection");
                Require(
                    repeatedSelection != null &&
                    repeatedSelection.Succeeded &&
                    repeatedSelection.SelectionRevision == liveSelectionRevision &&
                    ReferenceEquals(
                        repeatedSelection.SelectedActorProfile,
                        slotProfile.DefaultActorProfile),
                    repeatedSelection != null
                        ? repeatedSelection.ToDiagnosticString()
                        : "Repeated default Actor selection returned no result.");
                cases.Complete("repeated-default-selection-stable");

                // The lifecycle proof requires P2 to remain available for its
                // WaitingForJoin projection. The public surface has no leave
                // operation, so consume P2 only after every one-Player
                // lifecycle observation has completed.
                InputDevice sharedDevice =
                    joined.PlayerInput != null &&
                    joined.PlayerInput.devices.Count > 0
                        ? joined.PlayerInput.devices[0]
                        : null;
                Require(
                    sharedDevice != null &&
                    sharedDevice.added,
                    "Second Supported Slot Join requires one explicit active InputDevice " +
                    "from the first PlayerInput.");

                LocalPlayerJoinResult secondJoin = routeAccess.RequestJoin(
                    new LocalPlayerJoinRequest(
                        Source,
                        "qa-player-surface-02-second-supported-slot",
                        sharedDevice));
                Require(
                    secondJoin != null &&
                    secondJoin.Succeeded &&
                    secondJoin.Slot.IsJoined &&
                    secondJoin.Slot.PlayerSlotId != joinedSlotId,
                    secondJoin != null
                        ? secondJoin.ToDiagnosticString()
                        : "Second Supported Slot Join returned no result.");
                cases.Complete("second-join-uses-next-supported-slot");

                LocalPlayerJoinResult noAvailableSlot = routeAccess.RequestJoin(
                    new LocalPlayerJoinRequest(
                        Source,
                        "qa-player-surface-02-no-available-slot"));
                Require(
                    noAvailableSlot != null &&
                    noAvailableSlot.Status ==
                        LocalPlayerJoinStatus.RejectedNoAvailableSlot &&
                    !noAvailableSlot.Succeeded,
                    noAvailableSlot != null
                        ? noAvailableSlot.ToDiagnosticString()
                        : "Full-Session Join returned no public result.");
                LocalPlayerProvisioningConsumerObservationSnapshot afterFull =
                    RequireObservation(routeAccess, "after-full-session-join");
                Require(
                    afterFull.Participation.JoinedCount == configuredSlots &&
                    afterFull.Participation.AvailableCount == 0 &&
                    afterFull.SessionRevision >= sessionRevisionFloor,
                    "Rejected full-Session Join changed occupancy unexpectedly. " +
                    DescribeObservation(afterFull));
                cases.Complete("session-full-rejected-no-available-slot");

                // --- Public navigation disposition ---

                ActivityRequestTrigger publicTrigger =
                    new GameObject("QA_PLAYER_SURFACE_02_ActivityTrigger")
                        .AddComponent<ActivityRequestTrigger>();
                try
                {
                    SceneManager.MoveGameObjectToScene(
                        publicTrigger.gameObject,
                        ResolvePrimaryScene(host.State.CurrentRoute));
                    publicTrigger.TargetActivity = lifecycleActivity;
                    Require(
                        !publicTrigger.HasActivityRuntimeBinding,
                        "Runtime-created ActivityRequestTrigger unexpectedly already had a runtime binding.");
                    publicTrigger.RequestActivity();
                    await AwaitFramesAsync(4);
                    Require(
                        publicTrigger.LastRequestFailed &&
                        !publicTrigger.LastRequestSucceeded &&
                        !string.IsNullOrWhiteSpace(publicTrigger.LastMessage),
                        "Unbound public ActivityRequestTrigger did not fail explicitly. " +
                        $"phase='{publicTrigger.LastEventPhase}' " +
                        $"outcome='{publicTrigger.LastOutcome}' " +
                        $"message='{publicTrigger.LastMessage}'.");
                    cases.Complete("public-activity-trigger-unbound-fails");
                    publicNavigationDisposition =
                        "gap-runtime-created-trigger-not-composition-bound";
                }
                finally
                {
                    UnityEngine.Object.Destroy(publicTrigger.gameObject);
                }

                // Product composition binds ActivityRequestTrigger only during
                // Route/Activity/GlobalUI composition. Runtime-authored triggers
                // without that binding cannot complete public navigation. This is
                // recorded as a product reachability disposition for Q2, not a QA
                // privileged bypass.
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireCompositionBoundAsync(
                        publicNav.EnterActivityTrigger,
                        FrameBudget);
                publicNavigationDisposition =
                    "canonical-authored-trigger-composition-bound";
                cases.Complete("public-navigation-disposition");

                // --- Destroyed binding / stale route endpoint ---

                Require(
                    destroyedBindingReleasedAtDestruction,
                    "Destroyed consumer binding did not release/dispose its endpoint. " +
                    destroyedAccess.Snapshot.Diagnostic);
                cases.Complete("destroyed-binding-released");

                Require(
                    staleDestroyedOpen != null &&
                    staleDestroyedOpen.Rejected &&
                    staleDestroyedOpen.Status ==
                        PlayerParticipationOperationStatus.RejectedInvalidState,
                    staleDestroyedOpen != null
                        ? staleDestroyedOpen.ToDiagnosticString()
                        : "Stale destroyed endpoint OpenJoining returned no result.");
                Require(
                    destroyedObservationUnavailableAtDestruction,
                    "Destroyed endpoint returned a valid live observation.");
                cases.Complete("stale-route-endpoint-after-destroy");

                if (joiningOpen)
                {
                    PlayerParticipationOperationResult finalClose =
                        routeAccess.CloseJoining(
                            Source,
                            "qa-player-surface-02-final-close");
                    Require(
                        finalClose != null && finalClose.Completed,
                        finalClose != null
                            ? finalClose.ToDiagnosticString()
                            : "Final CloseJoining returned no result.");
                    joiningOpen = false;
                }

                if (ownedLifecycleReentry.HasOperation &&
                    !ownedLifecycleReentry.IsCompleted)
                {
                    await fixture.Activities.ClearActivityAsync(
                        Source,
                        "qa-player-surface-02-final-clear");
                }

                await fixture.DisposeAsync();
                fixture = null;
                cases.Complete("fixture-cleaned");

                RequirePublicSurfaceScanClean();
                cases.Complete("public-scan-clean");
                cases.RequireComplete();

                Debug.Log(
                    $"{Prefix} status='Passed' verdict='Q2_IMPLEMENTED_STATIC_OK' " +
                    $"behavioral='PendingUnityPlayModeConfirmation' " +
                    $"cases='{cases.Count}' " +
                    $"sessionRevisionFloor='{sessionRevisionFloor}' " +
                    $"joinedSlot='{joinedSlotId.StableText}' " +
                    $"selectionRevision='{selectionRevisionAtCapture}' " +
                    $"publicNavigation='{publicNavigationDisposition}' " +
                    "proof='ClosedJoin,SupportedSlots,FirstAvailableOrder,NoAvailableSlot,NoChange,MissingBinding,WrongScope,ExitWaiting,StaleActivityEndpoint,Reentry,StaleSelection,DestroyedBinding' " +
                    $"completed='{cases.DescribeCompleted()}'.");
            }
            catch (Exception exception)
            {
                failures.Add("execution", exception);
            }
            finally
            {
                await SafeUnwindAsync(
                    ownedLifecycleReentry,
                    fixture,
                    failures,
                    "lifecycle-reentry-unwind");
                await SafeUnwindAsync(
                    ownedLifecycle,
                    fixture,
                    failures,
                    "lifecycle-unwind");
                await SafeUnwindAsync(
                    ownedReentryWaiting,
                    fixture,
                    failures,
                    "reentry-waiting-unwind");
                await SafeUnwindAsync(
                    ownedWaiting,
                    fixture,
                    failures,
                    "waiting-unwind");

                if (joiningOpen &&
                    routeAccess != null &&
                    routeAccess.Snapshot.IsAvailable)
                {
                    try
                    {
                        routeAccess.CloseJoining(
                            Source,
                            "qa-player-surface-02-finally-close");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("joining-cleanup", exception);
                    }
                }

                if (fixture != null)
                {
                    try
                    {
                        await fixture.DisposeAsync();
                    }
                    catch (Exception exception)
                    {
                        failures.Add("fixture-cleanup", exception);
                    }
                }

            }

            if (failures.HasFailures)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' verdict='Q2_FAIL' " +
                    $"cases='{cases.Count}/{cases.ExpectedCount}' " +
                    $"next='{cases.NextExpectedOrNone()}' " +
                    $"completed='{cases.DescribeCompleted()}' " +
                    $"missing='{cases.DescribeMissing()}' " +
                    $"execution='{Escape(failures.Describe("execution"))}' " +
                    $"publicNavigation='{publicNavigationDisposition}'.");
                throw failures.ToAggregate(
                    "QA-PLAYER-SURFACE-02 public Player surface negative regression failed.");
            }
        }

        private static async Task SafeUnwindAsync(
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
            QaActivityEntryReadinessFixture fixture,
            QaFailureCollector failures,
            string name)
        {
            if (owned == null || !owned.HasOperation || owned.ReachedTerminal)
            {
                return;
            }

            try
            {
                await owned.UnwindAsync(
                    async () =>
                    {
                        if (fixture != null)
                        {
                            await fixture.Activities.ClearActivityAsync(
                                Source,
                                name);
                        }
                    });
            }
            catch (Exception exception)
            {
                failures.Add(name, exception);
            }
        }

        private static async Task<LocalPlayerProvisioningConsumerAccessBinding>
            ResolveAuthoredActivityBindingWhenContentLoadedAsync()
        {
            Scene content = default;
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                content = SceneManager.GetSceneByPath(
                    QaPlayerSurfacePublicNavigationSetup.ContentScenePath);
                if (content.IsValid() && content.isLoaded)
                {
                    break;
                }

                await Awaitable.NextFrameAsync();
            }

            Require(
                content.IsValid() && content.isLoaded,
                "Activity content scene did not load for Activity-scoped consumer binding.");

            GameObject root = null;
            foreach (GameObject candidate in content.GetRootGameObjects())
            {
                if (candidate != null && string.Equals(
                        candidate.name,
                        QaPlayerSurfaceActivityConsumerFixture.RootObjectName,
                        StringComparison.Ordinal))
                {
                    Require(root == null,
                        "Loaded Activity content contains duplicate Player Surface fixture roots.");
                    root = candidate;
                }
            }

            Require(root != null,
                "Loaded Activity content is missing its authored Player Surface consumer fixture.");
            QaPlayerSurfaceActivityConsumerFixture fixture =
                root.GetComponent<QaPlayerSurfaceActivityConsumerFixture>();
            string issue = string.Empty;
            Require(
                fixture != null &&
                fixture.TryValidateAuthoredSurface(out issue),
                string.IsNullOrWhiteSpace(issue)
                    ? "Authored Player Surface Activity fixture is invalid."
                    : issue);
            return fixture.ConsumerBinding;
        }

        private static async Task<ILocalPlayerProvisioningConsumerAccess>
            AwaitScopedAccessAsync(
                LocalPlayerProvisioningConsumerAccessBinding binding,
                int frameBudget)
        {
            Require(binding != null, "Scoped access wait requires a consumer binding.");
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (binding.TryGetAccess(
                        out ILocalPlayerProvisioningConsumerAccess access,
                        out _) &&
                    access != null &&
                    access.Snapshot.IsAvailable)
                {
                    return access;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Consumer access binding did not become available. " +
                $"state='{binding.BindingState}' diagnostic='{binding.Diagnostic}'.");
        }

        private static LocalPlayerProvisioningConsumerObservationSnapshot
            RequireObservation(
                ILocalPlayerProvisioningConsumerAccess access,
                string phase)
        {
            Require(access != null,
                $"Public observation unavailable at '{phase}'. access=null");
            LocalPlayerProvisioningConsumerObservationSnapshot observation;
            bool available = access.TryGetObservation(out observation);
            Require(
                available &&
                observation != null &&
                observation.IsAvailable,
                $"Public observation unavailable at '{phase}'. " +
                access.Snapshot.Diagnostic);
            return observation;
        }

        private static async Task<LocalPlayerProvisioningConsumerObservationSnapshot>
            AwaitObservationAsync(
                ILocalPlayerProvisioningConsumerAccess access,
                Func<LocalPlayerProvisioningConsumerObservationSnapshot, bool> predicate,
                string failure,
                int frameBudget)
        {
            LocalPlayerProvisioningConsumerObservationSnapshot latest = null;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (access.TryGetObservation(out latest) &&
                    latest != null &&
                    latest.IsAvailable &&
                    predicate(latest))
                {
                    return latest;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"{failure}. latest='{DescribeObservation(latest)}'.");
        }

        private static async Task AwaitParticipantCycleAsync(
            QaActivityEntryReadinessFixture fixture,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
            int expectedPreparationCount,
            int frameBudget)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (fixture.PreparationStartedCount >= expectedPreparationCount)
                {
                    return;
                }

                if (owned.IsCompleted)
                {
                    FrameworkActivityRequestResult early =
                        await owned.AwaitTerminalAsync();
                    throw new InvalidOperationException(
                        "Activity request terminated before expected readiness preparation. " +
                        early.Message);
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Expected readiness participant preparation cycle did not start. " +
                $"expected='{expectedPreparationCount}' actual='{fixture.PreparationStartedCount}'.");
        }

        private static async Task AwaitFramesAsync(int frames)
        {
            for (int frame = 0; frame < frames; frame++)
            {
                await Awaitable.NextFrameAsync();
            }
        }

        private static void ConfigurePlayerParticipation(
            ActivityAsset activity,
            PlayerParticipationRequirementLevel requirement,
            PlayerSlotProfile slotProfile)
        {
            var serialized = new SerializedObject(activity);
            SetEnumName(
                RequireProperty(serialized, "playerParticipationProjectionMode"),
                ActivityParticipationProjectionMode.ExplicitSlots.ToString());
            SetEnumName(
                RequireProperty(serialized, "playerParticipationZeroParticipantPolicy"),
                ActivityParticipationZeroParticipantPolicy.Rejected.ToString());
            SetEnumName(
                RequireProperty(serialized, "playerParticipationRequirementLevel"),
                requirement.ToString());
            SerializedProperty explicitSlots = RequireProperty(
                serialized,
                "playerParticipationExplicitSlotProfiles");
            explicitSlots.arraySize = 1;
            explicitSlots.GetArrayElementAtIndex(0).objectReferenceValue = slotProfile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerSlotProfile ResolveLocalPlayerSlot(
            int index,
            string label)
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            GameApplicationAsset application =
                settings != null ? settings.ActiveGameApplication : null;
            PlayerSlotProfile slot = null;
            Require(
                application != null &&
                QaPlayerSessionQaSupport.TryGetSupportedSlot(application, index, out slot) &&
                slot != null,
                $"Could not resolve {label} Local Player Slot at index '{index}' " +
                "from active GameApplication.");
            return slot;
        }

        private static bool ProjectsOnly(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            PlayerSlotProfile expectedSlot)
        {
            return snapshot != null &&
                expectedSlot != null &&
                expectedSlot.PlayerSlotId.IsValid &&
                snapshot.SlotCount == 1 &&
                snapshot.Slots.Count == 1 &&
                string.Equals(
                    snapshot.Slots[0].PlayerSlotId,
                    expectedSlot.PlayerSlotId.StableText,
                    StringComparison.Ordinal);
        }


        private static Scene ResolvePrimaryScene(RouteAsset route)
        {
            Require(route != null, "Route is required to resolve primary scene.");
            return ResolveSceneByName(route.PrimarySceneName);
        }

        private static Scene ResolveSceneByName(string sceneName)
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene candidate = SceneManager.GetSceneAt(index);
                if (candidate.IsValid() &&
                    candidate.isLoaded &&
                    string.Equals(candidate.name, sceneName, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return default;
        }

        private static bool HasJoinedSlot(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            if (observation?.Participation == null || !slotId.IsValid)
            {
                return false;
            }

            return FindSlot(observation.Participation, slotId).IsJoined;
        }

        private static bool HasSelectedActor(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId,
            ActorProfile expected)
        {
            if (observation?.Participation == null || expected == null)
            {
                return false;
            }

            PlayerSlotRuntimeSnapshot slot =
                FindSlot(observation.Participation, slotId);
            return slot.IsJoined &&
                slot.HasSelectedActor &&
                ReferenceEquals(slot.SelectedActorProfile, expected);
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot participation,
            PlayerSlotId slotId)
        {
            Require(participation != null && slotId.IsValid,
                "Slot lookup requires participation and a valid Slot id.");
            for (int index = 0; index < participation.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = participation.Slots[index];
                if (slot.PlayerSlotId == slotId)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException(
                $"Slot '{slotId.StableText}' is not present in the public participation snapshot.");
        }

        private static int CountActors(LocalPlayerHostAuthoring host)
        {
            if (host == null || host.ActorMount == null)
            {
                return 0;
            }

            return host.ActorMount
                .GetComponentsInChildren<PlayerActorDeclaration>(true)
                .Length;
        }

        private static void RequirePublicSurfaceScanClean()
        {
            string path =
                "Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/" +
                "QaPlayerProvisioningPublicSurfaceNegativeRegression.cs";
            string source = System.IO.File.ReadAllText(path);
            string[] forbidden =
            {
                "System." + "Reflection",
                "FindObject" + "OfType<",
                "FindObjects" + "ByType<",
                "FindObjectsOfType" + "All<",
                "GetComponentsInChildren<LocalPlayerActor" +
                    "SelectionRequestAuthoring",
                "PrepareSelected" + "Actor(",
                "EnsureGameplay" + "Ready(",
                "TryReconcile" + "(",
                "GetComponent<PlayerActor" + "Preparation",
                "GetComponent<Player" + "Gameplay",
                "GetComponent<LocalPlayerProvisioning" + "RuntimeHostModule",
                "RuntimeScope" + "Context",
                "TryBindActivity" + "Runtime("
            };

            for (int index = 0; index < forbidden.Length; index++)
            {
                Require(
                    source.IndexOf(forbidden[index], StringComparison.Ordinal) < 0,
                    $"QA-PLAYER-SURFACE-02 source scan found forbidden token '{forbidden[index]}'.");
            }
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Serialized property '{propertyName}' was not found.");
            return property;
        }

        private static void SetEnumName(
            SerializedProperty property,
            string enumName)
        {
            int index = Array.IndexOf(property.enumNames, enumName);
            Require(index >= 0,
                $"Enum value '{enumName}' was not found for '{property.propertyPath}'.");
            property.enumValueIndex = index;
        }

        private static string DescribeObservation(
            LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            if (observation == null)
            {
                return "observation='null'";
            }

            return
                $"available='{observation.IsAvailable}' " +
                $"lifecycle='{observation.Lifecycle?.Status}' " +
                $"activity='{observation.Lifecycle?.ActivityName}' " +
                $"occurrence='{observation.ActivityOccurrence}' " +
                $"sessionRevision='{observation.SessionRevision}' " +
                $"appliedRevision='{observation.AppliedSessionRevision}' " +
                $"joined='{observation.Participation?.JoinedCount}' " +
                $"availableSlots='{observation.Participation?.AvailableCount}' " +
                $"joiningOpen='{observation.Participation?.JoiningOpen}' " +
                $"diagnostic='{observation.Diagnostic}'";
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

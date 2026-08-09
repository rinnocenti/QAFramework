using System;
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
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
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
            "Immersive Framework/QA/Regressions/Player/" +
            "Run QA-PLAYER-SURFACE-02 Public Surface Negative Regression";
        private const string Prefix = "[QA_PLAYER_SURFACE_02]";
        private const string Source =
            nameof(QaPlayerProvisioningPublicSurfaceNegativeRegression);
        private const string ConsumerRootName = "QA_PLAYER_SURFACE_02_Consumer";
        private const string WrongScopeRootName =
            "QA_PLAYER_SURFACE_02_WrongScope";
        private const string ActivityScopeRootName =
            "QA_PLAYER_SURFACE_02_ActivityScope";
        private const int FrameBudget = 360;
        private const int ExpectedCaseCount = 36;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "runtime-started",
            "consumer-binding-created",
            "scoped-access-available",
            "fresh-session-confirmed",
            "join-rejected-joining-closed",
            "open-joining-succeeded",
            "open-joining-no-change",
            "invalid-capacity-rejected",
            "capacity-set-for-exhaustion",
            "first-join-for-capacity",
            "second-join-capacity-exhausted",
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
        internal static Task RunCertificationAsync() => RunAsync();

        private static async Task RunAsync()
        {
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            var failures = new QaFailureCollector();
            FrameworkRuntimeHost host = null;
            QaActivityEntryReadinessFixture fixture = null;
            GameObject consumerRoot = null;
            GameObject wrongScopeRoot = null;
            GameObject activityScopeRoot = null;
            GameObject destroyProbeRoot = null;
            LocalPlayerProvisioningConsumerAccessBinding routeBinding = null;
            ILocalPlayerProvisioningConsumerAccess routeAccess = null;
            ILocalPlayerProvisioningConsumerAccess activityAccess = null;
            ILocalPlayerProvisioningConsumerAccess destroyedAccess = null;
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

                QaM07InternalReconcileSetup.RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(
                    host != null &&
                    host.State.GameFlowStarted &&
                    host.State.CurrentRoute != null,
                    "QA-PLAYER-SURFACE-02 requires a started Game Flow runtime.");
                cases.Complete("runtime-started");

                Require(
                    QaPlayerSurfacePublicNavigationSupport.TryResolveGlobalUiFixture(
                        out QaPlayerSurfaceGlobalUiFixture globalUiFixture,
                        out string globalUiFixtureDiagnostic),
                    globalUiFixtureDiagnostic);
                actorSelection = await QaPlayerSurfacePublicNavigationSupport
                    .RequireActorSelectionRuntimeReadyAsync(
                        globalUiFixture,
                        FrameBudget);

                PlayerSlotProfile slotProfile = ResolveFirstLocalPlayerSlot();
                Require(
                    slotProfile != null &&
                    slotProfile.PlayerSlotId.IsValid &&
                    slotProfile.DefaultActorProfile != null,
                    "QA-PLAYER-SURFACE-02 requires a configured first Local Player Slot.");

                consumerRoot = CreateScopedConsumerRoot(
                    host.State.CurrentRoute.PrimarySceneName,
                    ConsumerRootName,
                    LocalPlayerProvisioningConsumerScope.Route,
                    out routeBinding);
                cases.Complete("consumer-binding-created");

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

                // --- Command negatives (closed / capacity / invalid / no-change) ---

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

                int configuredSlots =
                    open.Snapshot.ConfiguredSlotCount;
                Require(
                    configuredSlots >= 1,
                    "Session must expose at least one configured Slot.");

                PlayerParticipationOperationResult invalidCapacity =
                    routeAccess.SetDynamicCapacity(
                        configuredSlots + 1,
                        Source,
                        "qa-player-surface-02-invalid-capacity");
                Require(
                    invalidCapacity != null &&
                    invalidCapacity.Status ==
                        PlayerParticipationOperationStatus
                            .RejectedInvalidRequest &&
                    invalidCapacity.Rejected &&
                    !invalidCapacity.StateChanged &&
                    invalidCapacity.CurrentRevision ==
                        invalidCapacity.PreviousRevision,
                    invalidCapacity != null
                        ? invalidCapacity.ToDiagnosticString()
                        : "Invalid SetDynamicCapacity returned no result.");
                LocalPlayerProvisioningConsumerObservationSnapshot afterInvalidCapacity =
                    RequireObservation(routeAccess, "after-invalid-capacity");
                Require(
                    afterInvalidCapacity.Participation.DynamicCapacity ==
                        open.Snapshot.DynamicCapacity,
                    "Invalid capacity request changed live capacity. " +
                    DescribeObservation(afterInvalidCapacity));
                cases.Complete("invalid-capacity-rejected");

                PlayerParticipationOperationResult setCapacityOne =
                    routeAccess.SetDynamicCapacity(
                        1,
                        Source,
                        "qa-player-surface-02-capacity-one");
                Require(
                    setCapacityOne != null &&
                    setCapacityOne.Completed &&
                    setCapacityOne.Snapshot != null &&
                    setCapacityOne.Snapshot.DynamicCapacity == 1,
                    setCapacityOne != null
                        ? setCapacityOne.ToDiagnosticString()
                        : "SetDynamicCapacity(1) returned no result.");
                cases.Complete("capacity-set-for-exhaustion");

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
                        : "First public Join for capacity case returned no result.");
                joinedHost = joined.LocalPlayerHost;
                joinedSlotId = joined.Slot.PlayerSlotId;
                sessionRevisionFloor = Math.Max(
                    sessionRevisionFloor,
                    RequireObservation(routeAccess, "after-first-join")
                        .SessionRevision);
                cases.Complete("first-join-for-capacity");

                LocalPlayerJoinResult capacityJoin = routeAccess.RequestJoin(
                    new LocalPlayerJoinRequest(
                        Source,
                        "qa-player-surface-02-capacity-exhausted"));
                Require(
                    capacityJoin != null &&
                    capacityJoin.Status ==
                        LocalPlayerJoinStatus.RejectedCapacityReached &&
                    !capacityJoin.Succeeded,
                    capacityJoin != null
                        ? capacityJoin.ToDiagnosticString()
                        : "Capacity-exhausted Join returned no public result.");
                LocalPlayerProvisioningConsumerObservationSnapshot afterCapacity =
                    RequireObservation(routeAccess, "after-capacity-join");
                Require(
                    afterCapacity.Participation.JoinedCount == 1 &&
                    afterCapacity.SessionRevision >= sessionRevisionFloor,
                    "Capacity-exhausted Join changed occupancy unexpectedly. " +
                    DescribeObservation(afterCapacity));
                cases.Complete("second-join-capacity-exhausted");

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

                PlayerProvisioningCommandTrigger unboundTrigger =
                    new GameObject(
                            "QA_PLAYER_SURFACE_02_UnboundCommand")
                        .AddComponent<PlayerProvisioningCommandTrigger>();
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

                wrongScopeRoot = CreateScopedConsumerRoot(
                    host.State.CurrentRoute.PrimarySceneName,
                    WrongScopeRootName,
                    LocalPlayerProvisioningConsumerScope.Activity,
                    out LocalPlayerProvisioningConsumerAccessBinding wrongBinding);
                await AwaitFramesAsync(8);
                Require(
                    !wrongBinding.IsBound &&
                    !wrongBinding.TryGetAccess(
                        out ILocalPlayerProvisioningConsumerAccess wrongAccess,
                        out string wrongIssue) &&
                    wrongAccess == null &&
                    !string.IsNullOrWhiteSpace(wrongIssue),
                    "Activity-scoped binding on Route content must not fall back to Route authority. " +
                    $"state='{wrongBinding.BindingState}' diagnostic='{wrongBinding.Diagnostic}'.");
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
                    QaM07InternalReconcileSetup.ContentScenePath);
                ConfigurePlayerParticipation(
                    waitingActivity,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    slotProfile);

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
                            observation.Lifecycle.GateHeld,
                        "WaitingForJoin was not publicly observed",
                        FrameBudget);
                occurrenceA = waiting.ActivityOccurrence;
                cases.Complete("activity-entry-waiting");

                var activityScopePair =
                    await CreateActivityScopedBindingWhenContentLoadedAsync();
                activityScopeRoot = activityScopePair.root;
                LocalPlayerProvisioningConsumerAccessBinding activityBinding =
                    activityScopePair.binding;
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

                if (activityScopeRoot != null)
                {
                    UnityEngine.Object.Destroy(activityScopeRoot);
                    activityScopeRoot = null;
                }

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
                            observation.Participation.JoinedCount == 1 &&
                            HasJoinedSlot(observation, joinedSlotId),
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
                ActivityAsset lifecycleActivity = waitingActivity;

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

                int lifecyclePrepExpected = fixture.PreparationStartedCount + 1;
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

                // Exit during/after Actor progression while Activity-owned projection is live.
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
                    CountActors(joinedHost) == 0 &&
                    releasedLifecycle.SessionRevision >= sessionRevisionFloor &&
                    releasedLifecycle.AppliedSessionRevision >= appliedRevisionFloor,
                    "Exit after join lost Session Host/join or regressed revisions. " +
                    DescribeObservation(releasedLifecycle));
                // Immutable capture must retain occurrence A facts; it is not the live view.
                Require(
                    occurrenceSnapshotA.ActivityOccurrence == occurrenceA,
                    "Captured occurrence A snapshot lost its immutable identity.");
                cases.Complete("exit-after-join-session-persists");

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

                int reentryPrepExpected = fixture.PreparationStartedCount + 1;
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

                // Stale Actor selection revision against the current Slot.
                int liveSelectionRevision =
                    FindSlot(reentryReady.Participation, joinedSlotId)
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
                cases.Complete("public-navigation-disposition");

                // --- Destroyed binding / stale route endpoint ---

                destroyProbeRoot = CreateScopedConsumerRoot(
                    host.State.CurrentRoute.PrimarySceneName,
                    "QA_PLAYER_SURFACE_02_DestroyProbe",
                    LocalPlayerProvisioningConsumerScope.Route,
                    out LocalPlayerProvisioningConsumerAccessBinding destroyBinding);
                destroyedAccess = await AwaitScopedAccessAsync(
                    destroyBinding,
                    FrameBudget);
                Require(
                    destroyedAccess.Snapshot.IsAvailable,
                    "Destroy-probe consumer access was not available before destruction.");
                UnityEngine.Object.Destroy(destroyProbeRoot);
                destroyProbeRoot = null;
                await AwaitFramesAsync(4);
                Require(
                    destroyedAccess.Snapshot.IsDisposed ||
                    !destroyedAccess.Snapshot.IsAvailable,
                    "Destroyed consumer binding did not release/dispose its endpoint. " +
                    destroyedAccess.Snapshot.Diagnostic);
                cases.Complete("destroyed-binding-released");

                PlayerParticipationOperationResult staleOpen =
                    destroyedAccess.OpenJoining(
                        Source,
                        "qa-player-surface-02-stale-open");
                Require(
                    staleOpen != null &&
                    staleOpen.Rejected &&
                    staleOpen.Status ==
                        PlayerParticipationOperationStatus.RejectedInvalidState,
                    staleOpen != null
                        ? staleOpen.ToDiagnosticString()
                        : "Stale destroyed endpoint OpenJoining returned no result.");
                Require(
                    !destroyedAccess.TryGetObservation(
                        out LocalPlayerProvisioningConsumerObservationSnapshot destroyedObs) ||
                    destroyedObs == null ||
                    !destroyedObs.IsAvailable,
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
                    "proof='ClosedJoin,CapacityExhausted,InvalidCapacity,NoChange,MissingBinding,WrongScope,ExitWaiting,StaleActivityEndpoint,Reentry,StaleSelection,DestroyedBinding' " +
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

                DestroyIfPresent(ref activityScopeRoot);
                DestroyIfPresent(ref wrongScopeRoot);
                DestroyIfPresent(ref destroyProbeRoot);
                DestroyIfPresent(ref consumerRoot);
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

        private static GameObject CreateScopedConsumerRoot(
            string primarySceneName,
            string rootName,
            LocalPlayerProvisioningConsumerScope scope,
            out LocalPlayerProvisioningConsumerAccessBinding binding)
        {
            Scene primary = ResolveSceneByName(primarySceneName);
            Require(
                primary.IsValid() && primary.isLoaded,
                $"Primary scene '{primarySceneName}' is not loaded for consumer binding.");

            var root = new GameObject(rootName);
            SceneManager.MoveGameObjectToScene(root, primary);
            binding = root.AddComponent<LocalPlayerProvisioningConsumerAccessBinding>();
            ApplyScope(binding, scope);
            return root;
        }

        private static async Task<(
            GameObject root,
            LocalPlayerProvisioningConsumerAccessBinding binding)>
            CreateActivityScopedBindingWhenContentLoadedAsync()
        {
            Scene content = default;
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                content = SceneManager.GetSceneByPath(
                    QaM07InternalReconcileSetup.ContentScenePath);
                if (content.IsValid() && content.isLoaded)
                {
                    break;
                }

                await Awaitable.NextFrameAsync();
            }

            Require(
                content.IsValid() && content.isLoaded,
                "Activity content scene did not load for Activity-scoped consumer binding.");

            var root = new GameObject(ActivityScopeRootName);
            SceneManager.MoveGameObjectToScene(root, content);
            LocalPlayerProvisioningConsumerAccessBinding binding =
                root.AddComponent<LocalPlayerProvisioningConsumerAccessBinding>();
            ApplyScope(binding, LocalPlayerProvisioningConsumerScope.Activity);
            return (root, binding);
        }

        private static void ApplyScope(
            LocalPlayerProvisioningConsumerAccessBinding binding,
            LocalPlayerProvisioningConsumerScope scope)
        {
            var serialized = new SerializedObject(binding);
            SerializedProperty scopeProperty = serialized.FindProperty("scope");
            Require(scopeProperty != null, "Consumer binding is missing serialized scope.");
            int index = Array.IndexOf(scopeProperty.enumNames, scope.ToString());
            Require(index >= 0, $"Consumer binding scope enum lacks '{scope}'.");
            scopeProperty.enumValueIndex = index;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(
                binding.Scope == scope,
                $"Consumer binding scope '{binding.Scope}' did not apply '{scope}'.");
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

        private static PlayerSlotProfile ResolveFirstLocalPlayerSlot()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            GameApplicationAsset application =
                settings != null ? settings.ActiveGameApplication : null;
            PlayerSlotProfile slot = null;
            Require(
                application != null &&
                application.TryGetLocalPlayerSlot(0, out slot) &&
                slot != null,
                "Could not resolve first Local Player Slot from active GameApplication.");
            return slot;
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

        private static void DestroyIfPresent(ref GameObject root)
        {
            if (root != null)
            {
                UnityEngine.Object.Destroy(root);
                root = null;
            }
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
                $"capacity='{observation.Participation?.DynamicCapacity}' " +
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

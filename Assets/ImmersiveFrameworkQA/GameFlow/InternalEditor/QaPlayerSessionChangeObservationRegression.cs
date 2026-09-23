using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.Lifecycle;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-PLAYER-SURFACE-08 — certifies committed, scoped Player Session change
    /// observation through the authored public navigation fixture only.
    /// </summary>
    public static class QaPlayerSessionChangeObservationRegression
    {
        private const string Prefix = "[QA_PLAYER_SESSION_CHANGE_01]";
        private const string Source = nameof(QaPlayerSessionChangeObservationRegression);
        private const int FrameBudget = 360;
        private const int ExpectedCaseCount = 18;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "route-observer-bound",
            "fresh-session-confirmed",
            "open-joining-committed",
            "open-joining-no-op",
            "close-joining-committed",
            "close-joining-no-op",
            "join-rejected-closed",
            "join-terminal-slot-observed",
            "actor-selection-observed",
            "actor-selection-no-op",
            "leave-terminal-slot-observed",
            "destroy-probe-bound",
            "destroy-probe-unsubscribed",
            "activity-observer-bound",
            "activity-observer-unsubscribed",
            "cleanup-restored"
        };

        public static Task RunCertificationAsync() => RunAsync();

        private static async Task RunAsync()
        {
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            var events = new List<ObservedChange>();
            PlayerSessionObserver routeObserver = null;
            PlayerSessionObserver destroyProbe = null;
            PlayerSessionObserver activityObserver = null;
            ILocalPlayerProvisioningConsumerAccess routeAccess = null;
            ILocalPlayerProvisioningConsumerAccess activityAccess = null;
            QaPlayerSurfacePublicNavigationFixture fixture = null;
            bool initialJoiningOpen = false;
            bool initialStateCaptured = false;
            bool regressionActivityRequested = false;
            bool regressionActivityReleased = false;
            bool cleanupRestored = false;
            int destroyedCallbackCount = 0;
            int activityCallbackCount = 0;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            Action<PlayerSessionChange> captureDestroyedChange = _ =>
                destroyedCallbackCount++;
            Action<PlayerSessionChange> captureActivityChange = _ =>
                activityCallbackCount++;

            void CaptureRouteChange(PlayerSessionChange change)
            {
                PlayerParticipationSnapshot snapshot = null;
                bool snapshotAvailable = routeObserver != null &&
                    routeObserver.TryGetSnapshot(out snapshot);
                events.Add(new ObservedChange(change, snapshotAvailable ? snapshot : null));
            }

            try
            {
                Require(EditorApplication.isPlaying,
                    "Player Session change observation requires Play Mode.");
                cases.Complete("play-mode-required");

                QaPlayerSurfacePublicNavigationSetup.RequirePreparedForCurrentPlayMode();
                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string hostIssue), hostIssue);
                await QaH2FrameworkReadiness.RequireStartedRouteAsync(host, FrameBudget);
                Require(QaPlayerSurfacePublicNavigationSupport.TryResolveAuthoredFixture(
                        out fixture,
                        out string fixtureIssue), fixtureIssue);
                Require(QaPlayerSurfacePublicNavigationSupport.TryResolveGlobalUiFixture(
                        out QaPlayerSurfaceGlobalUiFixture globalUi,
                        out string globalUiIssue), globalUiIssue);
                await QaPlayerSurfacePublicNavigationSupport.RequireProvisioningRuntimeReadyAsync(
                    globalUi, FrameBudget);
                cases.Complete("setup-confirmed");

                routeObserver = fixture.RouteConsumerBinding as PlayerSessionObserver;
                Require(routeObserver != null &&
                        routeObserver.Scope == LocalPlayerProvisioningConsumerScope.Route,
                    "Public navigation fixture requires a Route-scoped PlayerSessionObserver.");
                routeAccess = await AwaitScopedAccessAsync(routeObserver, FrameBudget);
                Require(routeObserver.IsAvailable,
                    "Route PlayerSessionObserver did not expose live scoped observation.");
                cases.Complete("route-observer-bound");

                PlayerParticipationSnapshot initial = RequireSnapshot(routeObserver, "initial");
                initialJoiningOpen = initial.JoiningOpen;
                initialStateCaptured = true;
                initial = EnsureJoiningState(
                    routeObserver,
                    routeAccess,
                    false,
                    "normalize-initial-joining-closed");

                PlayerSlotProfile primarySlot = fixture.PrimaryPlayerSlot;
                Require(primarySlot != null && primarySlot.PlayerSlotId.IsValid &&
                        primarySlot.DefaultActorProfile != null,
                    "Session change observation requires the authored primary Slot and Actor A.");
                PlayerSlotRuntimeSnapshot initialSlot = FindSlot(initial, primarySlot.PlayerSlotId);
                Require(initial.IsInitialized && !initial.JoiningOpen &&
                        initial.JoinedCount == 0 &&
                        initialSlot.AllocationState == PlayerSlotAllocationState.Available,
                    "Session change observation requires a fresh closed Session and an Available primary Slot. " +
                    Describe(initial));
                cases.Complete("fresh-session-confirmed");

                routeObserver.Changed += CaptureRouteChange;

                ChangeBaseline openBaseline = CaptureBaseline(events, routeObserver);
                PlayerParticipationOperationResult open = routeAccess.OpenJoining(
                    Source, "open-joining");
                Require(open != null && open.Succeeded && open.StateChanged &&
                        open.Snapshot != null && open.Snapshot.JoiningOpen,
                    Describe(open));
                RequireCommittedJoiningChange(
                    ChangesSince(events, openBaseline),
                    openBaseline,
                    false,
                    true,
                    "OpenJoining");
                cases.Complete("open-joining-committed");

                ChangeBaseline openNoOpBaseline = CaptureBaseline(events, routeObserver);
                PlayerParticipationOperationResult openAgain = routeAccess.OpenJoining(
                    Source, "open-joining-again");
                Require(openAgain != null && openAgain.IgnoredNoChange &&
                        !openAgain.StateChanged &&
                        openAgain.CurrentRevision == openAgain.PreviousRevision,
                    Describe(openAgain));
                Require(ChangesSince(events, openNoOpBaseline).Count == 0,
                    "Ignored OpenJoining published a change event.");
                cases.Complete("open-joining-no-op");

                ChangeBaseline closeBaseline = CaptureBaseline(events, routeObserver);
                PlayerParticipationOperationResult close = routeAccess.CloseJoining(
                    Source, "close-joining");
                Require(close != null && close.Succeeded && close.StateChanged &&
                        close.Snapshot != null && !close.Snapshot.JoiningOpen,
                    Describe(close));
                RequireCommittedJoiningChange(
                    ChangesSince(events, closeBaseline),
                    closeBaseline,
                    true,
                    false,
                    "CloseJoining");
                cases.Complete("close-joining-committed");

                ChangeBaseline closeNoOpBaseline = CaptureBaseline(events, routeObserver);
                PlayerParticipationOperationResult closeAgain = routeAccess.CloseJoining(
                    Source, "close-joining-again");
                Require(closeAgain != null && closeAgain.IgnoredNoChange &&
                        !closeAgain.StateChanged &&
                        closeAgain.CurrentRevision == closeAgain.PreviousRevision,
                    Describe(closeAgain));
                Require(ChangesSince(events, closeNoOpBaseline).Count == 0,
                    "Ignored CloseJoining published a change event.");
                cases.Complete("close-joining-no-op");

                ChangeBaseline rejectedJoinBaseline = CaptureBaseline(events, routeObserver);
                LocalPlayerJoinResult rejectedJoin = routeAccess.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "join-while-closed"));
                Require(rejectedJoin != null &&
                        rejectedJoin.Status == LocalPlayerJoinStatus.RejectedJoiningClosed,
                    Describe(rejectedJoin));
                Require(!ContainsKind(
                            ChangesSince(events, rejectedJoinBaseline),
                            PlayerSessionChangeKind.SlotAllocationChanged) &&
                        ChangesSince(events, rejectedJoinBaseline).Count == 0 &&
                        RequireSnapshot(routeObserver, "after-closed-join").Revision ==
                            rejectedJoinBaseline.Snapshot.Revision,
                    "Join rejected because Joining is closed published or committed a Slot change.");
                cases.Complete("join-rejected-closed");

                EnsureJoiningState(
                    routeObserver,
                    routeAccess,
                    true,
                    "ensure-joining-open-for-player-occurrence");
                ChangeBaseline joinBaseline = CaptureBaseline(events, routeObserver);
                LocalPlayerJoinResult join = routeAccess.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "join-primary"));
                Require(join != null && join.Succeeded && join.Slot.IsJoined &&
                        join.Slot.PlayerSlotId == primarySlot.PlayerSlotId,
                    Describe(join));
                IReadOnlyList<ObservedChange> joinChanges = ChangesForSlot(
                    ChangesSince(events, joinBaseline),
                    join.Slot.PlayerSlotId,
                    "Join");
                ObservedChange joinedChange = RequireJoinCommitChain(
                    joinChanges,
                    join.Slot.PlayerSlotId,
                    joinBaseline.Snapshot.Revision,
                    "Join");
                cases.Complete("join-terminal-slot-observed");

                Require(routeAccess.TryGetObservation(
                            out LocalPlayerProvisioningConsumerObservationSnapshot
                                actorResolutionObservation) &&
                        actorResolutionObservation != null &&
                        actorResolutionObservation.HasInitializationEvidence &&
                        actorResolutionObservation.InitializationConfiguration
                            .ActorResolutionPolicy ==
                            PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                    "Public fixture must use ResolveConfiguredDefault Actor Resolution.");
                PlayerSlotRuntimeSnapshot joinedSlotForActorSelection = FindSlot(
                    joinedChange.Snapshot,
                    join.Slot.PlayerSlotId);
                Require(joinedSlotForActorSelection.IsJoined &&
                        joinedSlotForActorSelection.SelectedActorProfile == null,
                    "Join must leave the Slot unresolved until the explicit Actor selection " +
                    "command. Actor Resolution does not authorize an implicit selection.");

                ChangeBaseline actorSelectionBaseline = CaptureBaseline(events, routeObserver);
                PlayerActorSelectionResult selectActor = routeAccess.RequestSelectActorProfile(
                    new PlayerActorSelectionRequest(
                        join.Slot.PlayerSlotId,
                        primarySlot.DefaultActorProfile,
                        Source,
                        "select-actor-a",
                        join.Slot.SelectionRevision));
                Require(selectActor != null && selectActor.Succeeded &&
                        selectActor.Status == PlayerActorSelectionStatus.SucceededSelected &&
                        selectActor.StateChanged,
                    Describe(selectActor));
                IReadOnlyList<ObservedChange> actorChanges = ChangesForSlot(
                    ChangesSince(events, actorSelectionBaseline),
                    join.Slot.PlayerSlotId,
                    "Actor A selection");
                ObservedChange actorChange = RequireSingleKind(
                    actorChanges,
                    PlayerSessionChangeKind.ActorSelectionChanged,
                    "Actor A selection");
                Require(actorChange.Change.PlayerSlotId == join.Slot.PlayerSlotId &&
                        actorChange.Change.PreviousSlot.AllocationState ==
                            PlayerSlotAllocationState.Joined &&
                        actorChange.Change.CurrentSlot.AllocationState ==
                            PlayerSlotAllocationState.Joined &&
                        actorChange.Change.PreviousSlot.SelectedActorProfile == null &&
                        ReferenceEquals(
                            actorChange.Change.CurrentSlot.SelectedActorProfile,
                            primarySlot.DefaultActorProfile),
                    "Actor A selection event has the wrong Slot or current Actor.");
                RequireSnapshotMatchesChange(
                    actorChange, join.Slot.PlayerSlotId, true, "Actor A selection");
                Require(ReferenceEquals(
                        FindSlot(actorChange.Snapshot, join.Slot.PlayerSlotId)
                            .SelectedActorProfile,
                        primarySlot.DefaultActorProfile),
                    "Snapshot read inside Actor A callback did not expose Actor A.");
                RequireCommittedRevisions(
                    actorChanges,
                    actorSelectionBaseline.Snapshot.Revision,
                    "Actor A selection");
                cases.Complete("actor-selection-observed");

                ChangeBaseline actorSelectionNoOpBaseline = CaptureBaseline(events, routeObserver);
                PlayerActorSelectionResult selectActorAgain = routeAccess.RequestSelectActorProfile(
                    new PlayerActorSelectionRequest(
                        join.Slot.PlayerSlotId,
                        primarySlot.DefaultActorProfile,
                        Source,
                        "select-actor-a-again",
                        selectActor.SelectionRevision));
                Require(selectActorAgain != null && selectActorAgain.Succeeded &&
                        !selectActorAgain.StateChanged,
                    Describe(selectActorAgain));
                IReadOnlyList<ObservedChange> actorNoOpChanges = ChangesForSlot(
                    ChangesSince(events, actorSelectionNoOpBaseline),
                    join.Slot.PlayerSlotId,
                    "Actor A no-op");
                Require(!ContainsKind(
                            actorNoOpChanges,
                            PlayerSessionChangeKind.ActorSelectionChanged) &&
                        actorNoOpChanges.Count == 0,
                    "Selecting Actor A again published an additional Slot or Actor change.");
                cases.Complete("actor-selection-no-op");

                ChangeBaseline leaveBaseline = CaptureBaseline(events, routeObserver);
                PlayerSlotRuntimeSnapshot joinedSlot = FindSlot(
                    leaveBaseline.Snapshot, join.Slot.PlayerSlotId);
                SessionPlayerLeaveResult leave = routeAccess.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        join.Slot.PlayerSlotId,
                        joinedSlot.Revision,
                        Source,
                        "leave-primary"));
                Require(leave != null && leave.Status == SessionPlayerLeaveStatus.SucceededLeft,
                    Describe(leave));
                IReadOnlyList<ObservedChange> leaveChanges = ChangesForSlot(
                    ChangesSince(events, leaveBaseline),
                    join.Slot.PlayerSlotId,
                    "Leave");
                RequireLeaveCommitChain(
                    leaveChanges,
                    join.Slot.PlayerSlotId,
                    primarySlot.DefaultActorProfile,
                    leaveBaseline.Snapshot.Revision,
                    "Leave");
                cases.Complete("leave-terminal-slot-observed");

                PlayerParticipationSnapshot beforeActivity = RequireSnapshot(
                    routeObserver, "before-activity-observer-lifetime");
                Require(beforeActivity.JoinedCount == 0,
                    "Activity observer lifetime proof requires no joined Player.");

                EnsureJoiningState(
                    routeObserver,
                    routeAccess,
                    false,
                    "normalize-joining-closed-for-destroy-probe");
                destroyProbe = fixture.DestroyProbeBinding as PlayerSessionObserver;
                Require(destroyProbe != null &&
                        destroyProbe.Scope == LocalPlayerProvisioningConsumerScope.Route,
                    "Public fixture DestroyProbeBinding must be a Route-scoped PlayerSessionObserver.");
                ILocalPlayerProvisioningConsumerAccess destroyAccess =
                    await AwaitScopedAccessAsync(destroyProbe, FrameBudget);
                Require(destroyAccess.Snapshot.IsAvailable,
                    "Destroy probe was not live before subscription.");
                cases.Complete("destroy-probe-bound");

                destroyProbe.Changed += captureDestroyedChange;
                UnityEngine.Object.Destroy(destroyProbe.gameObject);
                await AwaitScopedAccessReleasedAsync(destroyAccess, FrameBudget);
                ChangeBaseline destroyProbeBaseline = CaptureBaseline(events, routeObserver);
                PlayerParticipationOperationResult postDestroyOpen = routeAccess.OpenJoining(
                    Source, "post-destroy-route-change");
                Require(postDestroyOpen != null && postDestroyOpen.Succeeded &&
                        postDestroyOpen.StateChanged,
                    Describe(postDestroyOpen));
                RequireCommittedJoiningChange(
                    ChangesSince(events, destroyProbeBaseline),
                    destroyProbeBaseline,
                    false,
                    true,
                    "DestroyProbe post-destroy OpenJoining");
                Require(destroyedCallbackCount == 0,
                    "Destroyed Route observer received a callback from a later Route change.");
                cases.Complete("destroy-probe-unsubscribed");

                await QaPlayerSurfacePublicNavigationSupport.RequireCompositionBoundAsync(
                    fixture.EnterActivityTrigger, FrameBudget);
                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(
                    fixture.EnterActivityTrigger);
                regressionActivityRequested = true;
                PlayerSessionScopedAccessConsumer activityBinding =
                    await ResolveActivityBindingAsync();
                activityObserver = activityBinding as PlayerSessionObserver;
                Require(activityObserver != null &&
                        activityObserver.Scope == LocalPlayerProvisioningConsumerScope.Activity,
                    "Activity fixture must expose an Activity-scoped PlayerSessionObserver.");
                activityAccess = await AwaitScopedAccessAsync(activityObserver, FrameBudget);
                Require(activityAccess.Snapshot.IsAvailable,
                    "Activity PlayerSessionObserver was not live after Activity entry.");
                cases.Complete("activity-observer-bound");

                activityObserver.Changed += captureActivityChange;
                QaPlayerSurfacePublicNavigationSupport.ClearActivityPublic(
                    fixture.ClearActivityTrigger);
                await QaPlayerSurfacePublicNavigationSupport.AwaitTriggerTerminalSuccessAsync(
                    fixture.ClearActivityTrigger,
                    FrameBudget,
                    "Activity exit for observer lifetime proof failed.");
                await AwaitScopedAccessReleasedAsync(activityAccess, FrameBudget);
                regressionActivityReleased = true;
                PlayerParticipationSnapshot activityCloseStart = EnsureJoiningState(
                    routeObserver,
                    routeAccess,
                    true,
                    "normalize-joining-open-for-activity-unsubscribe");
                Require(activityCloseStart.JoiningOpen,
                    "Activity unsubscribe proof requires an open Joining state before CloseJoining.");
                ChangeBaseline activityUnsubscribeBaseline = CaptureBaseline(events, routeObserver);
                PlayerParticipationOperationResult postActivityClose = routeAccess.CloseJoining(
                    Source, "post-activity-exit-route-change");
                Require(postActivityClose != null && postActivityClose.Succeeded &&
                        postActivityClose.StateChanged,
                    Describe(postActivityClose));
                RequireCommittedJoiningChange(
                    ChangesSince(events, activityUnsubscribeBaseline),
                    activityUnsubscribeBaseline,
                    true,
                    false,
                    "Activity observer post-release CloseJoining");
                Require(activityCallbackCount == 0,
                    "Released Activity observer received a callback from a later Route change.");
                cases.Complete("activity-observer-unsubscribed");

                PlayerParticipationSnapshot noJoinedPlayers = EnsureNoPlayersJoined(
                    routeObserver,
                    routeAccess,
                    "cleanup-release-joined-players");
                PlayerParticipationSnapshot cleanupSnapshot = EnsureJoiningState(
                    routeObserver,
                    routeAccess,
                    initialJoiningOpen,
                    "restore-initial-joining-state");
                Require(noJoinedPlayers.JoinedCount == 0 && cleanupSnapshot.JoinedCount == 0 &&
                        regressionActivityReleased,
                    "Regression cleanup did not restore an empty Session or release its Activity.");
                cleanupRestored = true;
                cases.Complete("cleanup-restored");
                cases.RequireComplete();
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                if (destroyProbe != null)
                {
                    destroyProbe.Changed -= captureDestroyedChange;
                }

                if (activityObserver != null)
                {
                    activityObserver.Changed -= captureActivityChange;
                }

                if (routeObserver != null)
                {
                    routeObserver.Changed -= CaptureRouteChange;
                }

                try
                {
                    if (regressionActivityRequested && !regressionActivityReleased &&
                        fixture != null && routeAccess != null &&
                        routeAccess.Snapshot.IsAvailable)
                    {
                        bool hasCurrentActivity = routeAccess.TryGetObservation(
                            out LocalPlayerProvisioningConsumerObservationSnapshot observation) &&
                            observation != null && observation.IsAvailable &&
                            observation.HasCurrentActivityOccurrence;
                        if (hasCurrentActivity)
                        {
                            QaPlayerSurfacePublicNavigationSupport.ClearActivityPublic(
                                fixture.ClearActivityTrigger);
                            await QaPlayerSurfacePublicNavigationSupport
                                .AwaitTriggerTerminalSuccessAsync(
                                    fixture.ClearActivityTrigger,
                                    FrameBudget,
                                    "Final Activity clear did not settle.");
                        }

                        bool activityStillCurrent = routeAccess.TryGetObservation(
                            out LocalPlayerProvisioningConsumerObservationSnapshot afterClear) &&
                            afterClear != null && afterClear.IsAvailable &&
                            afterClear.HasCurrentActivityOccurrence;
                        Require(!activityStillCurrent,
                            "Regression-created Activity remained current during final cleanup.");
                        regressionActivityReleased = true;
                    }

                    if (initialStateCaptured && routeAccess != null &&
                        routeAccess.Snapshot.IsAvailable)
                    {
                        PlayerParticipationSnapshot noJoinedPlayers = EnsureNoPlayersJoined(
                            routeObserver,
                            routeAccess,
                            "finally-release-joined-players");
                        PlayerParticipationSnapshot restored = EnsureJoiningState(
                            routeObserver,
                            routeAccess,
                            initialJoiningOpen,
                            "finally-restore-initial-joining-state");
                        Require(noJoinedPlayers.JoinedCount == 0 && restored.JoinedCount == 0 &&
                                (!regressionActivityRequested || regressionActivityReleased),
                            "Final cleanup did not restore the empty Session state.");
                        cleanupRestored = true;
                    }
                }
                catch (Exception exception)
                {
                    cleanupFailure ??= exception;
                }

                string execution = executionFailure == null ? "passed" :
                    Escape(executionFailure.Message);
                string unwind = "not-required";
                string cleanup = cleanupFailure == null && cleanupRestored
                    ? "restored"
                    : Escape(cleanupFailure?.Message ?? "not-confirmed");
                if (executionFailure == null && cleanupFailure == null && cleanupRestored)
                {
                    Debug.Log(
                        $"{Prefix} status='Passed' verdict='CommittedScopedObservation' " +
                        $"cases='{cases.Count}/{cases.ExpectedCount}' next='{cases.NextExpectedOrNone()}' " +
                        $"completed='{cases.DescribeCompleted()}' missing='{cases.DescribeMissing()}' " +
                        $"execution='{execution}' unwind='{unwind}' cleanup='{cleanup}'.");
                }
                else
                {
                    Debug.LogError(
                        $"{Prefix} status='Failed' verdict='CommittedScopedObservationRejected' " +
                        $"cases='{cases.Count}/{cases.ExpectedCount}' next='{cases.NextExpectedOrNone()}' " +
                        $"completed='{cases.DescribeCompleted()}' missing='{cases.DescribeMissing()}' " +
                        $"execution='{execution}' unwind='{unwind}' cleanup='{cleanup}'.");
                }
            }

            if (executionFailure != null)
            {
                throw executionFailure;
            }

            if (cleanupFailure != null)
            {
                throw cleanupFailure;
            }
        }

        private static async Task<ILocalPlayerProvisioningConsumerAccess>
            AwaitScopedAccessAsync(
                PlayerSessionScopedAccessConsumer binding,
                int frameBudget)
        {
            Require(binding != null, "Scoped observer binding is required.");
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (binding.TryGetAccess(
                        out ILocalPlayerProvisioningConsumerAccess access,
                        out _) &&
                    access != null && access.Snapshot.IsAvailable)
                {
                    return access;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Scoped PlayerSessionObserver did not become available. " +
                $"state='{binding.BindingState}' diagnostic='{binding.Diagnostic}'.");
        }

        private static async Task AwaitScopedAccessReleasedAsync(
            ILocalPlayerProvisioningConsumerAccess access,
            int frameBudget)
        {
            Require(access != null, "Scoped observer access is required.");
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (!access.Snapshot.IsAvailable)
                {
                    return;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Scoped PlayerSessionObserver remained available after its scope was released. " +
                $"diagnostic='{access.Snapshot.Diagnostic}'.");
        }

        private static PlayerParticipationSnapshot EnsureJoiningState(
            PlayerSessionObserver observer,
            ILocalPlayerProvisioningConsumerAccess access,
            bool expectedOpen,
            string operation)
        {
            Require(access != null && access.Snapshot.IsAvailable,
                $"{operation} requires live Route-scoped access.");
            PlayerParticipationSnapshot before = RequireSnapshot(
                observer, operation + "-before");
            if (before.JoiningOpen == expectedOpen)
            {
                return before;
            }

            PlayerParticipationOperationResult change = expectedOpen
                ? access.OpenJoining(Source, operation)
                : access.CloseJoining(Source, operation);
            Require(change != null && change.Succeeded && change.StateChanged &&
                    change.Snapshot != null && change.Snapshot.JoiningOpen == expectedOpen,
                Describe(change));

            PlayerParticipationSnapshot after = RequireSnapshot(
                observer, operation + "-after");
            Require(after.JoiningOpen == expectedOpen &&
                    after.Revision > before.Revision &&
                    after.Revision == change.CurrentRevision,
                $"{operation} did not commit the requested Joining state. " +
                $"before='{Describe(before)}' after='{Describe(after)}' " +
                $"result='{Describe(change)}'.");
            return after;
        }

        private static PlayerParticipationSnapshot EnsureNoPlayersJoined(
            PlayerSessionObserver observer,
            ILocalPlayerProvisioningConsumerAccess access,
            string operation)
        {
            Require(access != null && access.Snapshot.IsAvailable,
                $"{operation} requires live Route-scoped access.");
            PlayerParticipationSnapshot snapshot = RequireSnapshot(
                observer, operation + "-before");
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (!slot.IsJoined)
                {
                    continue;
                }

                SessionPlayerLeaveResult leave = access.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        slot.PlayerSlotId,
                        slot.Revision,
                        Source,
                        operation));
                Require(leave != null &&
                        leave.Status == SessionPlayerLeaveStatus.SucceededLeft,
                    Describe(leave));
                snapshot = RequireSnapshot(observer, operation + "-after-leave");
            }

            Require(snapshot.JoinedCount == 0,
                $"{operation} left joined Players in the Session. {Describe(snapshot)}");
            return snapshot;
        }

        private static ChangeBaseline CaptureBaseline(
            List<ObservedChange> events,
            PlayerSessionObserver observer) => new ChangeBaseline(
            events.Count,
            RequireSnapshot(observer, "change-baseline"));

        private static List<ObservedChange> ChangesSince(
            List<ObservedChange> events,
            ChangeBaseline baseline)
        {
            Require(events != null && baseline.EventCount >= 0 &&
                    baseline.EventCount <= events.Count,
                "Change baseline is outside the observed event stream.");
            var changes = new List<ObservedChange>(
                events.Count - baseline.EventCount);
            for (int index = baseline.EventCount; index < events.Count; index++)
            {
                changes.Add(events[index]);
            }

            return changes;
        }

        private static void RequireCommittedJoiningChange(
            IReadOnlyList<ObservedChange> changes,
            ChangeBaseline baseline,
            bool expectedPreviousOpen,
            bool expectedCurrentOpen,
            string operation)
        {
            Require(changes != null && changes.Count == 1,
                $"{operation} expected exactly one committed Joining change; " +
                $"actual='{changes?.Count ?? 0}'.");
            ObservedChange observed = RequireSingleKind(
                changes, PlayerSessionChangeKind.JoiningChanged, operation);
            Require(observed.Change.PreviousJoiningOpen == expectedPreviousOpen &&
                    observed.Change.CurrentJoiningOpen == expectedCurrentOpen,
                $"{operation} published the wrong Joining transition. " +
                $"expected='{expectedPreviousOpen}->{expectedCurrentOpen}' " +
                $"actual='{observed.Change.PreviousJoiningOpen}->{observed.Change.CurrentJoiningOpen}'.");
            RequireSnapshotMatchesChange(observed, default, false, operation);
            RequireCommittedRevisions(changes, baseline.Snapshot.Revision, operation);
        }

        private static async Task<PlayerSessionScopedAccessConsumer>
            ResolveActivityBindingAsync()
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

            Require(content.IsValid() && content.isLoaded,
                "Activity content did not load for its authored PlayerSessionObserver.");
            QaPlayerSurfaceActivityConsumerFixture activityFixture = null;
            GameObject[] roots = content.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (roots[index] != null && string.Equals(
                        roots[index].name,
                        QaPlayerSurfaceActivityConsumerFixture.RootObjectName,
                        StringComparison.Ordinal))
                {
                    Require(activityFixture == null,
                        "Activity content contains duplicate Player Surface consumer roots.");
                    activityFixture = roots[index].GetComponent<
                        QaPlayerSurfaceActivityConsumerFixture>();
                }
            }

            string issue = string.Empty;
            bool validFixture = activityFixture != null &&
                activityFixture.TryValidateAuthoredSurface(out issue);
            Require(validFixture,
                string.IsNullOrWhiteSpace(issue)
                    ? "Activity Player Surface consumer fixture is invalid."
                    : issue);
            return activityFixture.ConsumerBinding;
        }

        private static PlayerParticipationSnapshot RequireSnapshot(
            PlayerSessionObserver observer,
            string phase)
        {
            PlayerParticipationSnapshot snapshot = null;
            Require(observer != null && observer.TryGetSnapshot(
                        out snapshot) &&
                    snapshot != null,
                $"PlayerSessionObserver snapshot is unavailable at '{phase}'. " +
                $"diagnostic='{observer?.Diagnostic}'.");
            return snapshot;
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId slotId)
        {
            Require(snapshot != null, "Player Session snapshot is required.");
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (slot.PlayerSlotId == slotId)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException(
                $"Player Session snapshot has no Slot '{slotId.StableText}'.");
        }

        private static ObservedChange RequireSingleKind(
            IReadOnlyList<ObservedChange> events,
            PlayerSessionChangeKind kind,
            string operation)
        {
            ObservedChange match = default;
            int count = 0;
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].Change != null && events[index].Change.Kind == kind)
                {
                    match = events[index];
                    count++;
                }
            }

            Require(events.Count == 1 && count == 1,
                $"{operation} expected exactly one '{kind}' event; " +
                $"actualEvents='{events.Count}' actualKind='{count}'.");
            return match;
        }

        private static IReadOnlyList<ObservedChange> ChangesForSlot(
            IReadOnlyList<ObservedChange> events,
            PlayerSlotId slotId,
            string operation)
        {
            Require(events != null && slotId.IsValid,
                $"{operation} requires a valid Slot-scoped event window.");
            var slotChanges = new List<ObservedChange>();
            for (int index = 0; index < events.Count; index++)
            {
                PlayerSessionChange change = events[index].Change;
                if (change != null && change.PlayerSlotId == slotId)
                {
                    slotChanges.Add(events[index]);
                }
            }

            return slotChanges;
        }

        private static ObservedChange RequireJoinCommitChain(
            IReadOnlyList<ObservedChange> changes,
            PlayerSlotId slotId,
            int baselineRevision,
            string operation)
        {
            Require(changes != null && changes.Count == 2,
                $"{operation} expected the authoritative Available -> Reserved -> Joined " +
                $"Slot chain; actual='{DescribeEvents(changes)}'.");
            ObservedChange reserved = RequireSlotAllocationTransition(
                changes[0],
                slotId,
                PlayerSlotAllocationState.Available,
                PlayerSlotAllocationState.Reserved,
                operation + " reserve");
            ObservedChange joined = RequireSlotAllocationTransition(
                changes[1],
                slotId,
                PlayerSlotAllocationState.Reserved,
                PlayerSlotAllocationState.Joined,
                operation + " commit");
            RequireSlotContinuation(reserved, joined, operation);
            RequireSnapshotMatchesChange(reserved, slotId, true, operation + " reserve");
            RequireSnapshotMatchesChange(joined, slotId, true, operation + " commit");
            RequireCommittedRevisions(changes, baselineRevision, operation);
            return joined;
        }

        private static ObservedChange RequireLeaveCommitChain(
            IReadOnlyList<ObservedChange> changes,
            PlayerSlotId slotId,
            ActorProfile actorProfile,
            int baselineRevision,
            string operation)
        {
            Require(actorProfile != null && changes != null && changes.Count == 3,
                $"{operation} expected the authoritative Joined -> Leaving -> Available " +
                $"chain with Actor clear; actual='{DescribeEvents(changes)}'.");
            ObservedChange leaving = RequireSlotAllocationTransition(
                changes[0],
                slotId,
                PlayerSlotAllocationState.Joined,
                PlayerSlotAllocationState.Leaving,
                operation + " begin");
            ObservedChange cleared = RequireActorSelectionTransition(
                changes[1],
                slotId,
                actorProfile,
                null,
                PlayerSlotAllocationState.Leaving,
                operation + " clear Actor");
            ObservedChange available = RequireSlotAllocationTransition(
                changes[2],
                slotId,
                PlayerSlotAllocationState.Leaving,
                PlayerSlotAllocationState.Available,
                operation + " terminal");
            RequireSlotContinuation(leaving, cleared, operation + " begin-to-clear");
            RequireSlotContinuation(cleared, available, operation + " clear-to-terminal");
            Require(available.Change.CurrentSlot.SelectedActorProfile == null,
                $"{operation} terminal Available Slot retained an Actor selection.");
            RequireSnapshotMatchesChange(leaving, slotId, true, operation + " begin");
            RequireSnapshotMatchesChange(cleared, slotId, true, operation + " clear Actor");
            RequireSnapshotMatchesChange(available, slotId, true, operation + " terminal");
            RequireCommittedRevisions(changes, baselineRevision, operation);
            return available;
        }

        private static ObservedChange RequireSlotAllocationTransition(
            ObservedChange observed,
            PlayerSlotId slotId,
            PlayerSlotAllocationState previousState,
            PlayerSlotAllocationState currentState,
            string operation)
        {
            PlayerSessionChange change = observed.Change;
            Require(change != null &&
                    change.Kind == PlayerSessionChangeKind.SlotAllocationChanged &&
                    change.PlayerSlotId == slotId &&
                    change.PreviousSlot.PlayerSlotId == slotId &&
                    change.CurrentSlot.PlayerSlotId == slotId &&
                    change.PreviousSlot.AllocationState == previousState &&
                    change.CurrentSlot.AllocationState == currentState,
                $"{operation} did not publish '{previousState} -> {currentState}' for " +
                $"'{slotId.StableText}'. observed='{DescribeChange(change)}'.");
            return observed;
        }

        private static ObservedChange RequireActorSelectionTransition(
            ObservedChange observed,
            PlayerSlotId slotId,
            ActorProfile previousActor,
            ActorProfile currentActor,
            PlayerSlotAllocationState expectedAllocationState,
            string operation)
        {
            PlayerSessionChange change = observed.Change;
            Require(change != null &&
                    change.Kind == PlayerSessionChangeKind.ActorSelectionChanged &&
                    change.PlayerSlotId == slotId &&
                    change.PreviousSlot.PlayerSlotId == slotId &&
                    change.CurrentSlot.PlayerSlotId == slotId &&
                    change.PreviousSlot.AllocationState == expectedAllocationState &&
                    change.CurrentSlot.AllocationState == expectedAllocationState &&
                    ReferenceEquals(change.PreviousSlot.SelectedActorProfile, previousActor) &&
                    ReferenceEquals(change.CurrentSlot.SelectedActorProfile, currentActor),
                $"{operation} did not publish the expected Actor selection transition for " +
                $"'{slotId.StableText}'. observed='{DescribeChange(change)}'.");
            return observed;
        }

        private static void RequireSlotContinuation(
            ObservedChange previous,
            ObservedChange current,
            string operation)
        {
            Require(previous.Change != null && current.Change != null &&
                    previous.Change.CurrentSlot.PlayerSlotId ==
                        current.Change.PreviousSlot.PlayerSlotId &&
                    previous.Change.CurrentSlot.AllocationState ==
                        current.Change.PreviousSlot.AllocationState &&
                    previous.Change.CurrentSlot.Revision ==
                        current.Change.PreviousSlot.Revision &&
                    previous.Change.CurrentSlot.SelectionRevision ==
                        current.Change.PreviousSlot.SelectionRevision &&
                    ReferenceEquals(
                        previous.Change.CurrentSlot.SelectedActorProfile,
                        current.Change.PreviousSlot.SelectedActorProfile),
                $"{operation} did not preserve the exact Slot state between commits. " +
                $"previous='{DescribeChange(previous.Change)}' " +
                $"current='{DescribeChange(current.Change)}'.");
        }

        private static void RequireSnapshotMatchesChange(
            ObservedChange observed,
            PlayerSlotId expectedSlotId,
            bool requireSlot,
            string operation)
        {
            Require(observed.Change != null && observed.Snapshot != null &&
                    observed.Snapshot.Revision == observed.Change.SessionRevision,
                $"{operation} callback snapshot was unavailable or stale. " +
                $"eventRevision='{observed.Change?.SessionRevision}' " +
                $"snapshotRevision='{observed.Snapshot?.Revision}'.");
            if (observed.Change.Kind == PlayerSessionChangeKind.JoiningChanged)
            {
                Require(observed.Snapshot.JoiningOpen ==
                        observed.Change.CurrentJoiningOpen,
                    $"{operation} callback snapshot did not expose committed Joining state.");
            }

            if (requireSlot)
            {
                PlayerSlotRuntimeSnapshot snapshotSlot = FindSlot(
                    observed.Snapshot, expectedSlotId);
                Require(snapshotSlot.PlayerSlotId == observed.Change.PlayerSlotId &&
                        snapshotSlot.AllocationState ==
                            observed.Change.CurrentSlot.AllocationState &&
                        snapshotSlot.Revision == observed.Change.CurrentSlot.Revision &&
                        snapshotSlot.SelectionRevision ==
                            observed.Change.CurrentSlot.SelectionRevision &&
                        ReferenceEquals(
                            snapshotSlot.SelectedActorProfile,
                            observed.Change.CurrentSlot.SelectedActorProfile),
                    $"{operation} callback snapshot did not expose committed Slot state.");
            }
        }

        private static void RequireCommittedRevisions(
            IReadOnlyList<ObservedChange> events,
            int baselineRevision,
            string operation)
        {
            int previousRevision = baselineRevision;
            for (int index = 0; index < events.Count; index++)
            {
                ObservedChange observed = events[index];
                Require(observed.Change != null &&
                        observed.Change.SessionRevision > previousRevision &&
                        observed.Snapshot != null &&
                        observed.Snapshot.Revision == observed.Change.SessionRevision,
                    $"{operation} emitted a non-monotonic or non-committed revision. " +
                    $"event='{observed.Change?.SessionRevision}' " +
                    $"previous='{previousRevision}' snapshot='{observed.Snapshot?.Revision}'.");
                previousRevision = observed.Change.SessionRevision;
            }
        }

        private static bool ContainsKind(
            IReadOnlyList<ObservedChange> events,
            PlayerSessionChangeKind kind)
        {
            for (int index = 0; index < events.Count; index++)
            {
                if (events[index].Change != null && events[index].Change.Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Describe(PlayerParticipationOperationResult result) =>
            result != null ? result.ToDiagnosticString() :
                "missing Player participation result.";

        private static string Describe(LocalPlayerJoinResult result) =>
            result != null ? result.ToDiagnosticString() : "missing Player join result.";

        private static string Describe(SessionPlayerLeaveResult result) =>
            result != null ? result.ToDiagnosticString() : "missing Player leave result.";

        private static string Describe(PlayerActorSelectionResult result) =>
            result != null ? result.ToDiagnosticString() :
                "missing Player Actor selection result.";

        private static string Describe(PlayerParticipationSnapshot snapshot) =>
            snapshot == null
                ? "snapshot='null'"
                : $"revision='{snapshot.Revision}' joiningOpen='{snapshot.JoiningOpen}' " +
                  $"joined='{snapshot.JoinedCount}' available='{snapshot.AvailableCount}'.";

        private static string DescribeEvents(IReadOnlyList<ObservedChange> events)
        {
            if (events == null)
            {
                return "null";
            }

            var descriptions = new List<string>();
            for (int index = 0; index < events.Count; index++)
            {
                PlayerSessionChange change = events[index].Change;
                descriptions.Add(change == null
                    ? "null"
                    : $"{change.Kind}:{change.PlayerSlotId.StableText}:" +
                      $"{change.CurrentSlot.AllocationState}:r{change.SessionRevision}");
            }

            return string.Join(",", descriptions);
        }

        private static string DescribeChange(PlayerSessionChange change) =>
            change == null
                ? "null"
                : $"{change.Kind}:{change.PlayerSlotId.StableText}:" +
                  $"{change.PreviousSlot.AllocationState}->{change.CurrentSlot.AllocationState}:" +
                  $"actor='{change.PreviousSlot.SelectedActorProfile}'->" +
                  $"'{change.CurrentSlot.SelectedActorProfile}':r{change.SessionRevision}";

        private static string Escape(string value) => (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\r", " ")
            .Replace("\n", " ");

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private readonly struct ObservedChange
        {
            internal ObservedChange(
                PlayerSessionChange change,
                PlayerParticipationSnapshot snapshot)
            {
                Change = change;
                Snapshot = snapshot;
            }

            internal PlayerSessionChange Change { get; }
            internal PlayerParticipationSnapshot Snapshot { get; }
        }

        private readonly struct ChangeBaseline
        {
            internal ChangeBaseline(
                int eventCount,
                PlayerParticipationSnapshot snapshot)
            {
                EventCount = eventCount;
                Snapshot = snapshot;
            }

            internal int EventCount { get; }
            internal PlayerParticipationSnapshot Snapshot { get; }
        }
    }
}

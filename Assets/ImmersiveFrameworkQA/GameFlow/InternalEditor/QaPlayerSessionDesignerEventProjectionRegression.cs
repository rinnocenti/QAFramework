using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-PLAYER-SURFACE-09 — certifies the designer-facing UnityEvent
    /// projection published by the public PlayerSessionObserver surface.
    /// </summary>
    public static class QaPlayerSessionDesignerEventProjectionRegression
    {
        private const string Source = nameof(QaPlayerSessionDesignerEventProjectionRegression);
        private const int FrameBudget = 360;

        public static Task RunCertificationAsync() => RunAsync();

        private static async Task RunAsync()
        {
            PlayerSessionObserver observer = null;
            ILocalPlayerProvisioningConsumerAccess access = null;
            bool initialJoiningOpen = false;
            var rawChanges = new List<PlayerSessionChange>();
            var designerEvents = new List<string>();
            Action<PlayerSessionChange> captureRaw = change => rawChanges.Add(change);
            UnityAction joiningOpened = () => designerEvents.Add("joining-opened");
            UnityAction joiningClosed = () => designerEvents.Add("joining-closed");
            UnityAction playerJoined = () => designerEvents.Add("player-joined");
            UnityAction playerLeft = () => designerEvents.Add("player-left");
            UnityAction actorSelected = () => designerEvents.Add("actor-selected");
            UnityAction actorChanged = () => designerEvents.Add("actor-changed");
            UnityAction actorCleared = () => designerEvents.Add("actor-cleared");

            try
            {
                Require(EditorApplication.isPlaying,
                    "Designer-facing Player Session event projection requires Play Mode.");
                QaPlayerSurfacePublicNavigationSetup.RequirePreparedForCurrentPlayMode();
                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string hostIssue), hostIssue);
                await QaH2FrameworkReadiness.RequireStartedRouteAsync(host, FrameBudget);
                Require(QaPlayerSurfacePublicNavigationSupport.TryResolveAuthoredFixture(
                        out QaPlayerSurfacePublicNavigationFixture fixture,
                        out string fixtureIssue), fixtureIssue);
                Require(QaPlayerSurfacePublicNavigationSupport.TryResolveGlobalUiFixture(
                        out QaPlayerSurfaceGlobalUiFixture globalUi,
                        out string globalUiIssue), globalUiIssue);
                await QaPlayerSurfacePublicNavigationSupport.RequireProvisioningRuntimeReadyAsync(
                    globalUi, FrameBudget);

                observer = fixture.RouteConsumerBinding as PlayerSessionObserver;
                Require(observer != null && observer.Scope ==
                        LocalPlayerProvisioningConsumerScope.Route,
                    "Designer event projection requires the Route-scoped PlayerSessionObserver.");
                access = await AwaitAccessAsync(observer);

                PlayerParticipationSnapshot initial = RequireSnapshot(observer, "initial");
                initialJoiningOpen = initial.JoiningOpen;
                Require(initial.JoinedCount == 0,
                    "Designer event projection requires a fresh Session without joined Players.");
                if (initial.JoiningOpen)
                {
                    PlayerParticipationOperationResult closeInitial = access.CloseJoining(
                        Source, "normalize-initial-joining-closed");
                    Require(closeInitial != null && closeInitial.Succeeded &&
                            closeInitial.StateChanged,
                        "Could not normalize the initial Joining state.");
                }

                PlayerSlotProfile slotProfile = fixture.PrimaryPlayerSlot;
                Require(slotProfile != null && slotProfile.PlayerSlotId.IsValid &&
                        slotProfile.DefaultActorProfile != null,
                    "Designer event projection requires the public primary Slot and Actor A.");
                PlayerSlotRuntimeSnapshot freshSlot = FindSlot(
                    RequireSnapshot(observer, "fresh-session"), slotProfile.PlayerSlotId);
                Require(!freshSlot.IsJoined && freshSlot.AllocationState ==
                        PlayerSlotAllocationState.Available &&
                        freshSlot.SelectedActorProfile == null,
                    "Designer event projection requires an Available primary Slot without Actor.");

                RegisterListeners(
                    observer,
                    captureRaw,
                    joiningOpened,
                    joiningClosed,
                    playerJoined,
                    playerLeft,
                    actorSelected,
                    actorChanged,
                    actorCleared);

                EventBaseline openBaseline = CaptureBaseline(rawChanges, designerEvents);
                PlayerParticipationOperationResult open = access.OpenJoining(
                    Source, "open-joining");
                Require(open != null && open.Succeeded && open.StateChanged,
                    "Open Joining did not commit.");
                RequireJoiningProjection(
                    rawChanges, designerEvents, openBaseline, false, true,
                    "joining-opened");

                EventBaseline openNoOpBaseline = CaptureBaseline(rawChanges, designerEvents);
                PlayerParticipationOperationResult openAgain = access.OpenJoining(
                    Source, "open-joining-no-op");
                Require(openAgain != null && openAgain.IgnoredNoChange &&
                        !openAgain.StateChanged,
                    "Open Joining no-op was not reported as no-op.");
                RequireNoProjection(rawChanges, designerEvents, openNoOpBaseline,
                    "Open Joining no-op");

                EventBaseline closeBaseline = CaptureBaseline(rawChanges, designerEvents);
                PlayerParticipationOperationResult close = access.CloseJoining(
                    Source, "close-joining");
                Require(close != null && close.Succeeded && close.StateChanged,
                    "Close Joining did not commit.");
                RequireJoiningProjection(
                    rawChanges, designerEvents, closeBaseline, true, false,
                    "joining-closed");

                EventBaseline rejectedJoinBaseline = CaptureBaseline(rawChanges, designerEvents);
                LocalPlayerJoinResult rejectedJoin = access.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "join-while-closed"));
                Require(rejectedJoin != null && rejectedJoin.Status ==
                        LocalPlayerJoinStatus.RejectedJoiningClosed,
                    "Join while Joining is closed was not rejected.");
                RequireNoProjection(rawChanges, designerEvents, rejectedJoinBaseline,
                    "Rejected Join");

                PlayerParticipationOperationResult reopen = access.OpenJoining(
                    Source, "reopen-joining-for-join");
                Require(reopen != null && reopen.Succeeded && reopen.StateChanged,
                    "Could not reopen Joining for the Join projection.");

                EventBaseline joinBaseline = CaptureBaseline(rawChanges, designerEvents);
                LocalPlayerJoinResult join = access.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "join-primary"));
                Require(join != null && join.Succeeded && join.Slot.IsJoined,
                    "Primary Player Join did not commit.");
                RequireJoinProjection(
                    rawChanges, designerEvents, joinBaseline, join.Slot.PlayerSlotId);

                EventBaseline selectedBaseline = CaptureBaseline(rawChanges, designerEvents);
                PlayerActorSelectionResult selectActor = access.RequestSelectActorProfile(
                    new PlayerActorSelectionRequest(
                        join.Slot.PlayerSlotId,
                        slotProfile.DefaultActorProfile,
                        Source,
                        "select-actor-a",
                        join.Slot.SelectionRevision));
                Require(selectActor != null && selectActor.Succeeded &&
                        selectActor.StateChanged,
                    "Actor A selection did not commit.");
                RequireActorProjection(
                    rawChanges, designerEvents, selectedBaseline,
                    null, slotProfile.DefaultActorProfile, "actor-selected");

                EventBaseline selectNoOpBaseline = CaptureBaseline(rawChanges, designerEvents);
                PlayerActorSelectionResult selectActorAgain = access.RequestSelectActorProfile(
                    new PlayerActorSelectionRequest(
                        join.Slot.PlayerSlotId,
                        slotProfile.DefaultActorProfile,
                        Source,
                        "select-actor-a-no-op",
                        selectActor.SelectionRevision));
                Require(selectActorAgain != null && selectActorAgain.Succeeded &&
                        !selectActorAgain.StateChanged,
                    "Repeated Actor A selection was not reported as no-op.");
                RequireNoProjection(rawChanges, designerEvents, selectNoOpBaseline,
                    "Repeated Actor A selection");

                EventBaseline changedBaseline = CaptureBaseline(rawChanges, designerEvents);
                fixture.ReplaceActorSelectionCommand.Invoke();
                PlayerActorSelectionResult replaceActor =
                    fixture.ReplaceActorSelectionCommand.LastActorSelectionResult;
                Require(replaceActor != null && replaceActor.Succeeded &&
                        replaceActor.StateChanged && replaceActor.SelectedActorProfile != null &&
                        !ReferenceEquals(
                            replaceActor.SelectedActorProfile,
                            slotProfile.DefaultActorProfile),
                    "Public Replace Actor command did not commit Actor B.");
                RequireActorProjection(
                    rawChanges, designerEvents, changedBaseline,
                    slotProfile.DefaultActorProfile,
                    replaceActor.SelectedActorProfile,
                    "actor-changed");

                EventBaseline clearBaseline = CaptureBaseline(rawChanges, designerEvents);
                fixture.ClearActorSelectionCommand.Invoke();
                PlayerActorSelectionResult clearActor =
                    fixture.ClearActorSelectionCommand.LastActorSelectionResult;
                Require(clearActor != null && clearActor.Succeeded && clearActor.StateChanged,
                    "Public Clear Actor command did not commit.");
                RequireActorProjection(
                    rawChanges, designerEvents, clearBaseline,
                    replaceActor.SelectedActorProfile, null, "actor-cleared");

                EventBaseline clearNoOpBaseline = CaptureBaseline(rawChanges, designerEvents);
                fixture.ClearActorSelectionCommand.Invoke();
                PlayerActorSelectionResult clearActorAgain =
                    fixture.ClearActorSelectionCommand.LastActorSelectionResult;
                Require(clearActorAgain != null && clearActorAgain.Succeeded &&
                        !clearActorAgain.StateChanged,
                    "Repeated Actor clear was not reported as no-op.");
                RequireNoProjection(rawChanges, designerEvents, clearNoOpBaseline,
                    "Repeated Actor clear");

                PlayerSlotRuntimeSnapshot unselectedSlot = FindSlot(
                    RequireSnapshot(observer, "before-leave-actor-selection"),
                    join.Slot.PlayerSlotId);
                PlayerActorSelectionResult prepareLeaveActor =
                    access.RequestSelectActorProfile(new PlayerActorSelectionRequest(
                        join.Slot.PlayerSlotId,
                        slotProfile.DefaultActorProfile,
                        Source,
                        "prepare-actor-for-leave",
                        unselectedSlot.SelectionRevision));
                Require(prepareLeaveActor != null && prepareLeaveActor.Succeeded &&
                        prepareLeaveActor.StateChanged,
                    "Could not prepare Actor A for Leave projection.");

                EventBaseline leaveBaseline = CaptureBaseline(rawChanges, designerEvents);
                PlayerSlotRuntimeSnapshot joinedSlot = FindSlot(
                    RequireSnapshot(observer, "leave-baseline"), join.Slot.PlayerSlotId);
                SessionPlayerLeaveResult leave = access.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        join.Slot.PlayerSlotId,
                        joinedSlot.Revision,
                        Source,
                        "leave-primary"));
                Require(leave != null && leave.Status ==
                        SessionPlayerLeaveStatus.SucceededLeft,
                    "Leave did not commit.");
                RequireLeaveProjection(rawChanges, designerEvents, leaveBaseline,
                    join.Slot.PlayerSlotId, slotProfile.DefaultActorProfile);
            }
            finally
            {
                UnregisterListeners(
                    observer,
                    captureRaw,
                    joiningOpened,
                    joiningClosed,
                    playerJoined,
                    playerLeft,
                    actorSelected,
                    actorChanged,
                    actorCleared);

                if (access != null && access.Snapshot.IsAvailable)
                {
                    PlayerParticipationSnapshot snapshot = RequireSnapshot(
                        observer, "cleanup");
                    if (snapshot.JoiningOpen != initialJoiningOpen)
                    {
                        PlayerParticipationOperationResult restore = initialJoiningOpen
                            ? access.OpenJoining(Source, "restore-initial-joining-state")
                            : access.CloseJoining(Source, "restore-initial-joining-state");
                        Require(restore != null && restore.Succeeded,
                            "Designer event projection cleanup could not restore Joining.");
                    }
                }
            }
        }

        private static async Task<ILocalPlayerProvisioningConsumerAccess> AwaitAccessAsync(
            PlayerSessionObserver observer)
        {
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                if (observer.TryGetAccess(
                        out ILocalPlayerProvisioningConsumerAccess access,
                        out _) && access.Snapshot.IsAvailable)
                {
                    return access;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Route PlayerSessionObserver did not expose live scoped access.");
        }

        private static void RegisterListeners(
            PlayerSessionObserver observer,
            Action<PlayerSessionChange> raw,
            UnityAction joiningOpened,
            UnityAction joiningClosed,
            UnityAction playerJoined,
            UnityAction playerLeft,
            UnityAction actorSelected,
            UnityAction actorChanged,
            UnityAction actorCleared)
        {
            Require(observer.OnJoiningOpened != null && observer.OnJoiningClosed != null &&
                    observer.OnPlayerJoined != null && observer.OnPlayerLeft != null &&
                    observer.OnActorSelected != null && observer.OnActorChanged != null &&
                    observer.OnActorCleared != null,
                "PlayerSessionObserver did not initialize its designer-facing UnityEvents.");
            observer.Changed += raw;
            observer.OnJoiningOpened.AddListener(joiningOpened);
            observer.OnJoiningClosed.AddListener(joiningClosed);
            observer.OnPlayerJoined.AddListener(playerJoined);
            observer.OnPlayerLeft.AddListener(playerLeft);
            observer.OnActorSelected.AddListener(actorSelected);
            observer.OnActorChanged.AddListener(actorChanged);
            observer.OnActorCleared.AddListener(actorCleared);
        }

        private static void UnregisterListeners(
            PlayerSessionObserver observer,
            Action<PlayerSessionChange> raw,
            UnityAction joiningOpened,
            UnityAction joiningClosed,
            UnityAction playerJoined,
            UnityAction playerLeft,
            UnityAction actorSelected,
            UnityAction actorChanged,
            UnityAction actorCleared)
        {
            if (observer == null)
            {
                return;
            }

            observer.Changed -= raw;
            observer.OnJoiningOpened?.RemoveListener(joiningOpened);
            observer.OnJoiningClosed?.RemoveListener(joiningClosed);
            observer.OnPlayerJoined?.RemoveListener(playerJoined);
            observer.OnPlayerLeft?.RemoveListener(playerLeft);
            observer.OnActorSelected?.RemoveListener(actorSelected);
            observer.OnActorChanged?.RemoveListener(actorChanged);
            observer.OnActorCleared?.RemoveListener(actorCleared);
        }

        private static EventBaseline CaptureBaseline(
            List<PlayerSessionChange> rawChanges,
            List<string> designerEvents) => new(rawChanges.Count, designerEvents.Count);

        private static void RequireJoiningProjection(
            List<PlayerSessionChange> rawChanges,
            List<string> designerEvents,
            EventBaseline baseline,
            bool previousOpen,
            bool currentOpen,
            string expectedDesignerEvent)
        {
            IReadOnlyList<PlayerSessionChange> changes = RawSince(rawChanges, baseline);
            Require(changes.Count == 1 && changes[0].Kind ==
                    PlayerSessionChangeKind.JoiningChanged &&
                    changes[0].PreviousJoiningOpen == previousOpen &&
                    changes[0].CurrentJoiningOpen == currentOpen,
                "Canonical Changed did not publish the expected Joining transition.");
            RequireDesignerEvents(designerEvents, baseline, expectedDesignerEvent);
        }

        private static void RequireJoinProjection(
            List<PlayerSessionChange> rawChanges,
            List<string> designerEvents,
            EventBaseline baseline,
            PlayerSlotId slotId)
        {
            IReadOnlyList<PlayerSessionChange> changes = RawSince(rawChanges, baseline);
            Require(changes.Count == 2 &&
                    IsSlotTransition(changes[0], slotId,
                        PlayerSlotAllocationState.Available,
                        PlayerSlotAllocationState.Reserved) &&
                    IsSlotTransition(changes[1], slotId,
                        PlayerSlotAllocationState.Reserved,
                        PlayerSlotAllocationState.Joined),
                "Join did not publish the canonical Available -> Reserved -> Joined chain.");
            RequireDesignerEvents(designerEvents, baseline, "player-joined");
        }

        private static void RequireActorProjection(
            List<PlayerSessionChange> rawChanges,
            List<string> designerEvents,
            EventBaseline baseline,
            ActorProfile previousActor,
            ActorProfile currentActor,
            string expectedDesignerEvent)
        {
            IReadOnlyList<PlayerSessionChange> changes = RawSince(rawChanges, baseline);
            Require(changes.Count == 1 && changes[0].Kind ==
                    PlayerSessionChangeKind.ActorSelectionChanged &&
                    ReferenceEquals(changes[0].PreviousSlot.SelectedActorProfile, previousActor) &&
                    ReferenceEquals(changes[0].CurrentSlot.SelectedActorProfile, currentActor),
                "Canonical Changed did not publish the expected Actor transition.");
            RequireDesignerEvents(designerEvents, baseline, expectedDesignerEvent);
        }

        private static void RequireLeaveProjection(
            List<PlayerSessionChange> rawChanges,
            List<string> designerEvents,
            EventBaseline baseline,
            PlayerSlotId slotId,
            ActorProfile actor)
        {
            IReadOnlyList<PlayerSessionChange> changes = RawSince(rawChanges, baseline);
            Require(changes.Count == 3 &&
                    IsSlotTransition(changes[0], slotId,
                        PlayerSlotAllocationState.Joined,
                        PlayerSlotAllocationState.Leaving) &&
                    changes[1].Kind == PlayerSessionChangeKind.ActorSelectionChanged &&
                    ReferenceEquals(changes[1].PreviousSlot.SelectedActorProfile, actor) &&
                    changes[1].CurrentSlot.SelectedActorProfile == null &&
                    IsSlotTransition(changes[2], slotId,
                        PlayerSlotAllocationState.Leaving,
                        PlayerSlotAllocationState.Available),
                "Leave did not publish the canonical Leaving, Actor clear, Available chain.");
            RequireDesignerEvents(designerEvents, baseline,
                "actor-cleared", "player-left");
        }

        private static void RequireNoProjection(
            List<PlayerSessionChange> rawChanges,
            List<string> designerEvents,
            EventBaseline baseline,
            string operation)
        {
            Require(RawSince(rawChanges, baseline).Count == 0 &&
                    DesignerEventsSince(designerEvents, baseline).Count == 0,
                $"{operation} published a canonical change or designer-facing UnityEvent.");
        }

        private static void RequireDesignerEvents(
            List<string> designerEvents,
            EventBaseline baseline,
            params string[] expected)
        {
            IReadOnlyList<string> actual = DesignerEventsSince(designerEvents, baseline);
            Require(actual.Count == expected.Length,
                $"Expected {expected.Length} designer-facing UnityEvents but received {actual.Count}.");
            for (int index = 0; index < expected.Length; index++)
            {
                Require(string.Equals(actual[index], expected[index], StringComparison.Ordinal),
                    $"Designer-facing UnityEvent order mismatch at index {index}. " +
                    $"expected='{expected[index]}' actual='{actual[index]}'.");
            }
        }

        private static IReadOnlyList<PlayerSessionChange> RawSince(
            List<PlayerSessionChange> changes,
            EventBaseline baseline) => changes.GetRange(
            baseline.RawChangeCount,
            changes.Count - baseline.RawChangeCount);

        private static IReadOnlyList<string> DesignerEventsSince(
            List<string> events,
            EventBaseline baseline) => events.GetRange(
            baseline.DesignerEventCount,
            events.Count - baseline.DesignerEventCount);

        private static bool IsSlotTransition(
            PlayerSessionChange change,
            PlayerSlotId slotId,
            PlayerSlotAllocationState previous,
            PlayerSlotAllocationState current) =>
            change != null && change.Kind == PlayerSessionChangeKind.SlotAllocationChanged &&
            change.PlayerSlotId == slotId &&
            change.PreviousSlot.AllocationState == previous &&
            change.CurrentSlot.AllocationState == current;

        private static PlayerParticipationSnapshot RequireSnapshot(
            PlayerSessionObserver observer,
            string phase)
        {
            PlayerParticipationSnapshot snapshot = null;
            Require(observer != null && observer.TryGetSnapshot(
                        out snapshot) &&
                    snapshot != null,
                $"PlayerSessionObserver snapshot is unavailable at '{phase}'.");
            return snapshot;
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId slotId)
        {
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private readonly struct EventBaseline
        {
            internal EventBaseline(int rawChangeCount, int designerEventCount)
            {
                RawChangeCount = rawChangeCount;
                DesignerEventCount = designerEventCount;
            }

            internal int RawChangeCount { get; }
            internal int DesignerEventCount { get; }
        }
    }
}

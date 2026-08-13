using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.Player;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// ADR020-H — public Manager-Provisioned Session Player Leave proof.
    ///
    /// The Player command path under test is public only:
    /// scoped P1 consumer access -> Join / Actor selection -> normal Activity lifecycle
    /// -> RequestLeave -> public P2 observation. QA-only helpers arrange the authored
    /// navigation fixture and resolve Framework startup state; they never mutate the
    /// Player Slot, prepare an Actor, release resources or invoke Leave internals.
    /// </summary>
    public static class QaSessionPlayerLeavePublicManagerRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/" +
            "Run ADR020-H Session Player Leave Public Manager Regression";
        private const string Prefix = "[QA_ADR020_H_LEAVE]";
        private const string Source = nameof(QaSessionPlayerLeavePublicManagerRegression);
        private const int FrameBudget = 360;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "runtime-started",
            "public-fixture-resolved",
            "consumer-access-ready",
            "fresh-session-confirmed",
            "joining-opened",
            "player-a-joined",
            "player-a-selected",
            "activity-entered-ready",
            "joining-closed-before-leave",
            "leave-a-succeeded",
            "leave-a-stage-evidence",
            "slot-a-available",
            "joining-remains-closed",
            "manager-host-authority-released",
            "activity-stale-ready-cleared",
            "join-blocked-while-closed",
            "joining-reopened",
            "player-b-rejoined-new-occurrence",
            "stale-leave-a-rejected",
            "player-b-survives-stale-leave",
            "activity-cleared",
            "leave-b-without-activity-succeeded",
            "slot-b-available",
            "public-scan-clean"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            await RunAsync();
        }

        public static Task RunCertificationAsync() => RunAsync();

        private static async Task RunAsync()
        {
            var completed = new List<string>();
            Exception executionFailure = null;
            ILocalPlayerProvisioningConsumerAccess access = null;
            ActivityRequestTrigger clearTrigger = null;
            bool joiningOpen = false;

            try
            {
                Require(EditorApplication.isPlaying,
                    "ADR020-H requires Play Mode.");
                Complete(completed, "play-mode-required");

                QaPlayerSurfacePublicNavigationSetup.RequirePreparedForCurrentPlayMode();
                Complete(completed, "setup-confirmed");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(
                    host != null && host.State.GameFlowStarted &&
                    host.State.CurrentRoute != null,
                    "ADR020-H requires a started Game Flow runtime with a current Route.");
                Complete(completed, "runtime-started");

                Require(
                    QaPlayerSurfacePublicNavigationSupport.TryResolveAuthoredFixture(
                        out QaPlayerSurfacePublicNavigationFixture publicNav,
                        out string fixtureDiagnostic),
                    fixtureDiagnostic);
                Require(publicNav != null && publicNav.TargetActivity != null,
                    "ADR020-H requires the authored Player Surface navigation fixture and Activity.");
                Require(publicNav.PrimaryPlayerSlot != null,
                    "ADR020-H fixture requires an explicit Primary Player Slot.");
                Complete(completed, "public-fixture-resolved");

                Require(
                    QaPlayerSurfacePublicNavigationSupport.TryResolveGlobalUiFixture(
                        out QaPlayerSurfaceGlobalUiFixture globalUiFixture,
                        out string globalUiDiagnostic),
                    globalUiDiagnostic);
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireProvisioningRuntimeReadyAsync(globalUiFixture, FrameBudget);

                ActivityRequestTrigger enterTrigger = publicNav.EnterActivityTrigger;
                clearTrigger = publicNav.ClearActivityTrigger;
                Require(enterTrigger != null && clearTrigger != null,
                    "ADR020-H requires authored enter and clear Activity triggers.");
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireCompositionBoundAsync(enterTrigger, FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireCompositionBoundAsync(clearTrigger, FrameBudget);

                LocalPlayerProvisioningConsumerAccessBinding consumerBinding =
                    publicNav.RouteConsumerBinding;
                Require(
                    consumerBinding != null &&
                    consumerBinding.Scope == LocalPlayerProvisioningConsumerScope.Route,
                    "ADR020-H requires the authored Route scoped Player consumer binding.");
                access = await AwaitScopedAccessAsync(consumerBinding, FrameBudget);
                Complete(completed, "consumer-access-ready");

                PlayerSlotProfile slotProfile = publicNav.PrimaryPlayerSlot;
                PlayerSlotId slotId = slotProfile.PlayerSlotId;
                Require(slotId.IsValid && slotProfile.DefaultActorProfile != null,
                    "ADR020-H requires a valid Player Slot with default Actor.");

                LocalPlayerProvisioningConsumerObservationSnapshot initial =
                    RequireObservation(access, "initial");
                Require(
                    initial.Participation != null &&
                    initial.Participation.IsInitialized,
                    "ADR020-H requires initialized Player Session participation. " +
                    DescribeObservation(initial));

                PlayerSlotRuntimeSnapshot initialSlot =
                    FindSlot(initial.Participation, slotId);
                Require(
                    initial.Participation.JoinedCount == 0 &&
                    initial.Participation.LeavingCount == 0 &&
                    initialSlot.AllocationState == PlayerSlotAllocationState.Available &&
                    !initialSlot.IsJoined &&
                    !initialSlot.HasSelectedActor,
                    "ADR020-H is one-shot and requires a fresh Player Session occurrence state: " +
                    "no Joined/Leaving Player and the target Slot must be Available with no current selection. " +
                    "GameFlow startup Activity and diagnostic preparation/gameplay evidence are allowed. " +
                    DescribeSlot(initialSlot) + " " +
                    DescribeObservation(initial));
                Complete(completed, "fresh-session-confirmed");

                PlayerParticipationOperationResult open =
                    access.OpenJoining(Source, "adr020-h-open-joining-a");
                Require(
                    open != null && open.Completed && open.Snapshot != null &&
                    open.Snapshot.JoiningOpen,
                    open != null ? open.ToDiagnosticString() :
                        "OpenJoining returned no result.");
                joiningOpen = true;
                Complete(completed, "joining-opened");

                LocalPlayerJoinResult joinA = access.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "adr020-h-join-a"));
                Require(
                    joinA != null && joinA.Succeeded &&
                    joinA.Slot.IsJoined &&
                    joinA.Slot.PlayerSlotId == slotId &&
                    joinA.HasLocalPlayerHostEvidence,
                    joinA != null ? joinA.ToDiagnosticString() :
                        "Player A Join returned no result.");
                LocalPlayerHostAuthoring hostA = joinA.LocalPlayerHost;
                Require(hostA != null && hostA.IsJoined,
                    "Player A Join did not return a live joined Local Player Host.");
                Complete(completed, "player-a-joined");

                LocalPlayerActorSelectionRequestAuthoring actorSelection =
                    await QaPlayerSurfacePublicNavigationSupport
                        .RequireActorSelectionRuntimeReadyAsync(
                            globalUiFixture,
                            FrameBudget);
                LocalPlayerProvisioningConsumerObservationSnapshot beforeSelection =
                    RequireObservation(access, "before-selection-a");
                PlayerSlotRuntimeSnapshot slotBeforeSelection =
                    FindSlot(beforeSelection.Participation, slotId);
                PlayerActorSelectionResult selection =
                    actorSelection.RequestDefaultActorSelection(
                        slotId,
                        slotBeforeSelection.SelectionRevision,
                        Source,
                        "adr020-h-select-a");
                Require(
                    selection != null && selection.Succeeded &&
                    selection.Slot.IsJoined &&
                    selection.Slot.HasSelectedActor &&
                    ReferenceEquals(
                        selection.SelectedActorProfile,
                        slotProfile.DefaultActorProfile),
                    selection != null ? selection.ToDiagnosticString() :
                        "Player A default Actor selection returned no result.");
                Complete(completed, "player-a-selected");

                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(enterTrigger);
                LocalPlayerProvisioningConsumerObservationSnapshot ready =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.HasCurrentActivityOccurrence &&
                            observation.Lifecycle != null &&
                            observation.Lifecycle.IsReady &&
                            SlotIsFullyReady(observation, slotId),
                        "Player A Activity representation did not reach Ready before Leave",
                        FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        enterTrigger,
                        FrameBudget,
                        "ADR020-H Activity entry did not complete after Player A became Ready.");
                int activityOccurrenceA = ready.ActivityOccurrence;
                Require(activityOccurrenceA > 0,
                    "ADR020-H did not capture a valid current Activity occurrence.");
                Complete(completed, "activity-entered-ready");

                PlayerParticipationOperationResult close =
                    access.CloseJoining(Source, "adr020-h-close-before-leave");
                Require(
                    close != null && close.Completed && close.Snapshot != null &&
                    !close.Snapshot.JoiningOpen,
                    close != null ? close.ToDiagnosticString() :
                        "CloseJoining returned no result.");
                joiningOpen = false;
                Complete(completed, "joining-closed-before-leave");

                LocalPlayerProvisioningConsumerObservationSnapshot preLeave =
                    RequireObservation(access, "pre-leave-a");
                PlayerSlotRuntimeSnapshot playerAOccurrence =
                    FindSlot(preLeave.Participation, slotId);
                Require(
                    playerAOccurrence.IsJoined &&
                    playerAOccurrence.Revision >= 0,
                    "Player A occurrence snapshot is not Joined before Leave.");

                var leaveARequest = new SessionPlayerLeaveRequest(
                    slotId,
                    playerAOccurrence.Revision,
                    Source,
                    "adr020-h-leave-a-while-joining-closed");
                SessionPlayerLeaveResult leaveA = access.RequestLeave(leaveARequest);
                Require(
                    leaveA != null && leaveA.Succeeded &&
                    leaveA.Status == SessionPlayerLeaveStatus.SucceededLeft &&
                    leaveA.ProvisioningMode ==
                        PlayerHostProvisioningMode.ManagerProvisioned,
                    leaveA != null ? leaveA.ToDiagnosticString() :
                        "Player A Leave returned no result.");
                Complete(completed, "leave-a-succeeded");

                Require(
                    leaveA.LeaveStarted &&
                    leaveA.ActivityRepresentationReleased &&
                    leaveA.ProvisioningReleased &&
                    leaveA.TerminalCommitted &&
                    !leaveA.PartialRelease &&
                    !string.IsNullOrEmpty(leaveA.LeaveCorrelation),
                    "Successful Player A Leave did not expose complete staged release evidence. " +
                    leaveA.ToDiagnosticString());
                Complete(completed, "leave-a-stage-evidence");

                LocalPlayerProvisioningConsumerObservationSnapshot afterLeaveA =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                        {
                            if (observation.Participation == null)
                            {
                                return false;
                            }

                            PlayerSlotRuntimeSnapshot slot =
                                FindSlot(observation.Participation, slotId);
                            return observation.Participation.JoinedCount == 0 &&
                                slot.AllocationState ==
                                    PlayerSlotAllocationState.Available;
                        },
                        "Player A Slot did not reach Available after terminal Leave commit",
                        FrameBudget);
                PlayerSlotRuntimeSnapshot availableA =
                    FindSlot(afterLeaveA.Participation, slotId);
                Require(
                    !availableA.IsJoined &&
                    !availableA.HasSelectedActor &&
                    availableA.Revision > playerAOccurrence.Revision,
                    "Terminal Leave did not clear Player A Session occurrence state. " +
                    DescribeSlot(availableA));
                Complete(completed, "slot-a-available");

                Require(
                    !afterLeaveA.Participation.JoiningOpen,
                    "Successful Leave reopened Joining. ADR-020 requires Joining policy to remain unchanged.");
                Complete(completed, "joining-remains-closed");

                LocalPlayerProvisioningConsumerObservationSnapshot
                    postLeaveAuthorityObservation = null;
                LocalPlayerProvisioningConsumerObservationSnapshot releasedAuthority;
                try
                {
                    releasedAuthority = await AwaitObservationAsync(
                        access,
                        observation =>
                        {
                            postLeaveAuthorityObservation = observation;
                            return !HasHostEvidence(observation, slotId) &&
                                !SlotHasActivityAuthority(observation, slotId) &&
                                hostA == null;
                        },
                        "Manager-Provisioned Leave did not settle Host destruction and " +
                        "authoritative Host/Activity evidence release",
                        FrameBudget);
                }
                catch (TimeoutException exception)
                {
                    bool hostDestroyed = hostA == null;
                    bool hostEvidenceReleased =
                        postLeaveAuthorityObservation != null &&
                        !HasHostEvidence(
                            postLeaveAuthorityObservation,
                            slotId);
                    bool activityAuthorityReleased =
                        postLeaveAuthorityObservation != null &&
                        !SlotHasActivityAuthority(
                            postLeaveAuthorityObservation,
                            slotId);
                    throw new TimeoutException(
                        $"{exception.Message} hostDestroyed='{hostDestroyed}' " +
                        $"hostEvidenceReleased='{hostEvidenceReleased}' " +
                        $"activityAuthorityReleased='{activityAuthorityReleased}'.",
                        exception);
                }
                Complete(completed, "manager-host-authority-released");

                LocalPlayerProvisioningConsumerObservationSnapshot readinessAfterLeave =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.HasCurrentActivityOccurrence &&
                            observation.ActivityOccurrence == activityOccurrenceA &&
                            observation.Lifecycle != null &&
                            !observation.Lifecycle.IsReady,
                        "Current required Activity retained stale Ready after its required Player left",
                        FrameBudget);
                Require(
                    !readinessAfterLeave.Lifecycle.IsReady &&
                    !SlotHasActivityAuthority(readinessAfterLeave, slotId),
                    "Required Activity retained stale Player readiness/representation evidence after Leave. " +
                    DescribeObservation(readinessAfterLeave));
                Complete(completed, "activity-stale-ready-cleared");

                LocalPlayerJoinResult blockedJoin = access.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "adr020-h-join-blocked-closed"));
                Require(
                    blockedJoin != null && !blockedJoin.Succeeded,
                    "RequestJoin succeeded while Joining remained Closed after Leave.");
                LocalPlayerProvisioningConsumerObservationSnapshot stillVacant =
                    RequireObservation(access, "join-blocked-closed");
                Require(
                    stillVacant.Participation.JoinedCount == 0 &&
                    FindSlot(stillVacant.Participation, slotId).AllocationState ==
                        PlayerSlotAllocationState.Available,
                    "Rejected Join while Joining Closed mutated the vacant Slot.");
                Complete(completed, "join-blocked-while-closed");

                PlayerParticipationOperationResult reopen =
                    access.OpenJoining(Source, "adr020-h-reopen-for-b");
                Require(
                    reopen != null && reopen.Completed && reopen.Snapshot != null &&
                    reopen.Snapshot.JoiningOpen,
                    reopen != null ? reopen.ToDiagnosticString() :
                        "Reopen Joining returned no result.");
                joiningOpen = true;
                Complete(completed, "joining-reopened");

                LocalPlayerJoinResult joinB = access.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "adr020-h-join-b"));
                Require(
                    joinB != null && joinB.Succeeded &&
                    joinB.Slot.IsJoined &&
                    joinB.Slot.PlayerSlotId == slotId,
                    joinB != null ? joinB.ToDiagnosticString() :
                        "Player B rejoin returned no result.");
                LocalPlayerProvisioningConsumerObservationSnapshot joinedB =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.Participation != null &&
                            observation.Participation.JoinedCount == 1 &&
                            FindSlot(observation.Participation, slotId).IsJoined,
                        "Player B was not observable after rejoin",
                        FrameBudget);
                PlayerSlotRuntimeSnapshot playerBOccurrence =
                    FindSlot(joinedB.Participation, slotId);
                Require(
                    playerBOccurrence.Revision > playerAOccurrence.Revision,
                    "Rejoin did not create a newer Slot occurrence revision. " +
                    $"A='{playerAOccurrence.Revision}' B='{playerBOccurrence.Revision}'.");
                Complete(completed, "player-b-rejoined-new-occurrence");

                SessionPlayerLeaveResult staleA = access.RequestLeave(leaveARequest);
                Require(
                    staleA != null &&
                    staleA.Status ==
                        SessionPlayerLeaveStatus.RejectedForeignOrStaleOccurrence &&
                    !staleA.Succeeded,
                    staleA != null ?
                        "Old Leave A was not rejected as stale after Slot reuse. " +
                        staleA.ToDiagnosticString() :
                        "Stale Leave A returned no result.");
                Complete(completed, "stale-leave-a-rejected");

                LocalPlayerProvisioningConsumerObservationSnapshot afterStaleA =
                    RequireObservation(access, "post-stale-a");
                PlayerSlotRuntimeSnapshot bAfterStale =
                    FindSlot(afterStaleA.Participation, slotId);
                Require(
                    afterStaleA.Participation.JoinedCount == 1 &&
                    bAfterStale.IsJoined &&
                    bAfterStale.Revision == playerBOccurrence.Revision,
                    "Stale Leave A affected the newer Player B occurrence. " +
                    DescribeSlot(bAfterStale));
                Complete(completed, "player-b-survives-stale-leave");

                QaPlayerSurfacePublicNavigationSupport.ClearActivityPublic(clearTrigger);
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        clearTrigger,
                        FrameBudget,
                        "ADR020-H current Activity clear did not complete before no-Activity Leave proof.");
                LocalPlayerProvisioningConsumerObservationSnapshot noActivity =
                    await AwaitObservationAsync(
                        access,
                        observation => !observation.HasCurrentActivityOccurrence,
                        "Current Activity observation did not clear",
                        FrameBudget);
                Require(!noActivity.HasCurrentActivityOccurrence,
                    "No-Activity Leave proof requires no current Activity occurrence.");
                Complete(completed, "activity-cleared");

                PlayerSlotRuntimeSnapshot bBeforeLeave =
                    FindSlot(noActivity.Participation, slotId);
                var leaveBRequest = new SessionPlayerLeaveRequest(
                    slotId,
                    bBeforeLeave.Revision,
                    Source,
                    "adr020-h-leave-b-no-activity");
                SessionPlayerLeaveResult leaveB = access.RequestLeave(leaveBRequest);
                Require(
                    leaveB != null && leaveB.Succeeded &&
                    leaveB.ActivityRepresentationReleased &&
                    leaveB.ProvisioningReleased &&
                    leaveB.TerminalCommitted,
                    leaveB != null ? leaveB.ToDiagnosticString() :
                        "Player B no-Activity Leave returned no result.");
                Complete(completed, "leave-b-without-activity-succeeded");

                LocalPlayerProvisioningConsumerObservationSnapshot afterLeaveB =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                        {
                            if (observation.Participation == null)
                            {
                                return false;
                            }

                            PlayerSlotRuntimeSnapshot slot =
                                FindSlot(observation.Participation, slotId);
                            return observation.Participation.JoinedCount == 0 &&
                                slot.AllocationState ==
                                    PlayerSlotAllocationState.Available;
                        },
                        "Player B Slot did not return to Available after no-Activity Leave",
                        FrameBudget);
                Require(
                    !FindSlot(afterLeaveB.Participation, slotId).HasSelectedActor,
                    "No-Activity Leave retained Session-scoped Actor selection.");
                Complete(completed, "slot-b-available");

                RequirePublicSurfaceScanClean();
                Complete(completed, "public-scan-clean");
                RequireComplete(completed);

                Debug.Log(
                    $"{Prefix} status='Passed' verdict='ADR020_H_PASS' " +
                    $"cases='{completed.Count}' " +
                    $"slot='{slotId.StableText}' " +
                    $"leaveAOccurrence='{playerAOccurrence.Revision}' " +
                    $"leaveBOccurrence='{playerBOccurrence.Revision}' " +
                    $"activityOccurrence='{activityOccurrenceA}' " +
                    "proof='PublicLeave,ManagerProvisioned,JoiningClosed,TerminalAvailable,ResourceRelease,ReadinessInvalidation,Rejoin,StaleOccurrence,NoActivityLeave' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                if (clearTrigger != null &&
                    clearTrigger.HasActivityRuntimeBinding &&
                    clearTrigger.IsRequestInFlight)
                {
                    try
                    {
                        QaPlayerSurfacePublicNavigationSupport.ClearActivityPublic(
                            clearTrigger);
                        await QaPlayerSurfacePublicNavigationSupport
                            .AwaitTriggerTerminalSuccessAsync(
                                clearTrigger,
                                FrameBudget,
                                "ADR020-H cleanup Activity clear did not settle.");
                    }
                    catch (Exception cleanupException)
                    {
                        executionFailure ??= cleanupException;
                    }
                }

                if (joiningOpen && access != null && access.Snapshot.IsAvailable)
                {
                    try
                    {
                        access.CloseJoining(Source, "adr020-h-finally-close-joining");
                    }
                    catch (Exception cleanupException)
                    {
                        executionFailure ??= cleanupException;
                    }
                }
            }

            if (executionFailure != null)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' verdict='ADR020_H_FAIL' " +
                    $"cases='{completed.Count}/{ExpectedCases.Length}' " +
                    $"next='{NextExpected(completed)}' " +
                    $"completed='{string.Join(",", completed)}' " +
                    $"error='{Escape(executionFailure.Message)}'.");
                throw new InvalidOperationException(
                    "ADR020-H Session Player Leave public Manager regression failed.",
                    executionFailure);
            }
        }

        private static async Task<ILocalPlayerProvisioningConsumerAccess>
            AwaitScopedAccessAsync(
                LocalPlayerProvisioningConsumerAccessBinding binding,
                int frameBudget)
        {
            Require(binding != null,
                "Scoped access wait requires a consumer binding.");
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
                "Player consumer binding did not become available. " +
                $"state='{binding.BindingState}' diagnostic='{binding.Diagnostic}'.");
        }

        private static LocalPlayerProvisioningConsumerObservationSnapshot
            RequireObservation(
                ILocalPlayerProvisioningConsumerAccess access,
                string phase)
        {
            Require(access != null,
                $"Observation requires public access at phase '{phase}'.");
            Require(
                access.TryGetObservation(
                    out LocalPlayerProvisioningConsumerObservationSnapshot observation) &&
                observation != null && observation.IsAvailable,
                $"Public observation unavailable at phase '{phase}'. " +
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
                    latest != null && latest.IsAvailable &&
                    predicate(latest))
                {
                    return latest;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"{failure}. latest='{DescribeObservation(latest)}'.");
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot participation,
            PlayerSlotId slotId)
        {
            Require(participation != null && slotId.IsValid,
                "Slot lookup requires initialized participation and valid Slot id.");
            for (int index = 0; index < participation.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = participation.Slots[index];
                if (slot.PlayerSlotId == slotId)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException(
                $"Slot '{slotId.StableText}' is absent from the public participation snapshot.");
        }

        private static bool HasHostEvidence(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            if (observation?.Slots == null)
            {
                return false;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                if (slot.Slot.PlayerSlotId == slotId)
                {
                    return slot.HasHostEvidence && slot.HostEvidence.IsRecorded;
                }
            }

            return false;
        }

        private static bool SlotIsFullyReady(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            if (observation?.Slots == null)
            {
                return false;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                if (slot.Slot.PlayerSlotId != slotId)
                {
                    continue;
                }

                return slot.IsJoined &&
                    slot.HasSelectedActor &&
                    slot.IsLogicalActorPrepared &&
                    slot.IsPhysicallyMaterialized &&
                    slot.IsGameplayAdmitted;
            }

            return false;
        }

        private static bool SlotHasActivityAuthority(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            if (observation?.Slots == null)
            {
                return false;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                if (slot.Slot.PlayerSlotId != slotId)
                {
                    continue;
                }

                return slot.IsLogicalActorPrepared ||
                    slot.IsPhysicallyMaterialized ||
                    slot.IsGameplayAdmitted ||
                    slot.HasCurrentActorEvidence;
            }

            return false;
        }

        private static string DescribeSlot(PlayerSlotRuntimeSnapshot slot)
        {
            return
                $"slot='{(slot.PlayerSlotId.IsValid ? slot.PlayerSlotId.StableText : "<invalid>")}' " +
                $"allocation='{slot.AllocationState}' revision='{slot.Revision}' " +
                $"selectionRevision='{slot.SelectionRevision}' selected='{slot.HasSelectedActor}'.";
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
                $"activityOccurrence='{observation.ActivityOccurrence}' " +
                $"hasActivity='{observation.HasCurrentActivityOccurrence}' " +
                $"lifecycle='{observation.Lifecycle?.Status}' " +
                $"ready='{observation.Lifecycle?.IsReady}' " +
                $"gateHeld='{observation.Lifecycle?.GateHeld}' " +
                $"sessionRevision='{observation.SessionRevision}' " +
                $"appliedRevision='{observation.AppliedSessionRevision}' " +
                $"joined='{observation.Participation?.JoinedCount}' " +
                $"availableSlots='{observation.Participation?.AvailableCount}' " +
                $"joiningOpen='{observation.Participation?.JoiningOpen}' " +
                $"diagnostic='{observation.Diagnostic}'";
        }

        private static void RequirePublicSurfaceScanClean()
        {
            const string path =
                "Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/" +
                "QaSessionPlayerLeavePublicManagerRegression.cs";
            string source = System.IO.File.ReadAllText(path);
            string[] forbidden =
            {
                "System." + "Reflection",
                "FindObject" + "OfType<",
                "FindObjects" + "ByType<",
                "FindObjectsOfType" + "All<",
                "GetComponent<SessionPlayerLeave" + "RuntimeHostModule",
                "TryBeginSessionPlayer" + "Leave(",
                "TryCommitSessionPlayer" + "Leave(",
                "TryFinalizeSessionPlayer" + "Leave(",
                "TryReleaseActivityRepresentationForSessionPlayer" + "Leave(",
                "TryReleaseManagerProvisionedPlayerForSession" + "Leave(",
                "ReleaseHost" + "Evidence(",
                "PrepareSelected" + "Actor(",
                "EnsureGameplay" + "Ready(",
                "TryReconcile" + "(",
                "Reject" + "Player("
            };

            for (int index = 0; index < forbidden.Length; index++)
            {
                Require(
                    source.IndexOf(forbidden[index], StringComparison.Ordinal) < 0,
                    $"ADR020-H public QA source contains forbidden privileged token '{forbidden[index]}'.");
            }
        }

        private static void Complete(List<string> completed, string name)
        {
            Require(
                completed.Count < ExpectedCases.Length &&
                string.Equals(
                    ExpectedCases[completed.Count],
                    name,
                    StringComparison.Ordinal),
                $"ADR020-H case order mismatch. expected='{NextExpected(completed)}' actual='{name}'.");
            completed.Add(name);
        }

        private static void RequireComplete(List<string> completed)
        {
            Require(
                completed.Count == ExpectedCases.Length,
                $"ADR020-H incomplete. completed='{completed.Count}' expected='{ExpectedCases.Length}' next='{NextExpected(completed)}'.");
        }

        private static string NextExpected(List<string> completed)
        {
            return completed.Count < ExpectedCases.Length
                ? ExpectedCases[completed.Count]
                : "<none>";
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

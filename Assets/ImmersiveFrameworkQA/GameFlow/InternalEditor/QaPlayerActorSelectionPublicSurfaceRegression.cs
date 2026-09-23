using System;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.Player;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// QA-PLAYER-ACTOR-COMMANDS-01 — integrated public Actor-selection proof.
    /// It invokes only the authored explicit commands and observes state through
    /// Route-scoped access; it never binds or calls an Actor-selection port.
    /// </summary>
    public static class QaPlayerActorSelectionPublicSurfaceRegression
    {
        private const string Source = "qa-player-actor-commands-01";
        private const int FrameBudget = 360;

        public static async Task RunCertificationAsync()
        {
            Require(EditorApplication.isPlaying,
                "QA-PLAYER-ACTOR-COMMANDS-01 requires Play Mode.");

            QaPlayerSurfacePublicNavigationSetup.RequirePreparedForCurrentPlayMode();
            Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                    out FrameworkRuntimeHost host,
                    out string hostIssue), hostIssue);
            await QaH2FrameworkReadiness.RequireStartedRouteAsync(host, FrameBudget);

            Require(QaPlayerSurfacePublicNavigationSupport.TryResolveAuthoredFixture(
                    out QaPlayerSurfacePublicNavigationFixture navigation,
                    out string navigationIssue), navigationIssue);
            Require(QaPlayerSurfacePublicNavigationSupport.TryResolveGlobalUiFixture(
                    out QaPlayerSurfaceGlobalUiFixture globalUi,
                    out string globalUiIssue), globalUiIssue);
            await QaPlayerSurfacePublicNavigationSupport.RequireProvisioningRuntimeReadyAsync(
                globalUi, FrameBudget);
            await QaPlayerSurfacePublicNavigationSupport.RequireActorSelectionRuntimeReadyAsync(
                navigation, FrameBudget);

            ILocalPlayerProvisioningConsumerAccess access = await AwaitScopedAccessAsync(
                navigation.RouteConsumerBinding, FrameBudget);
            PlayerSlotProfile firstSlot = navigation.PrimaryPlayerSlot;
            Require(firstSlot != null && firstSlot.DefaultActorProfile != null,
                "Actor command QA requires the authored primary Slot and default Actor.");
            ActorProfile actorA = firstSlot.DefaultActorProfile;
            ActorProfile actorB = AssetDatabase.LoadAssetAtPath<ActorProfile>(
                "Assets/ImmersiveFrameworkQA/Player/Profiles/QA_AlternateActor.asset");
            Require(actorB != null && !ReferenceEquals(actorA, actorB),
                "Actor command QA requires the existing distinct P3H4 alternate Actor.");

            PlayerSessionSelectActorCommandTrigger select = navigation.SelectActorCommand;
            PlayerSessionDefaultActorSelectionCommandTrigger selectDefault =
                navigation.DefaultActorSelectionCommand;
            PlayerSessionReplaceActorSelectionCommandTrigger replace =
                navigation.ReplaceActorSelectionCommand;
            PlayerSessionClearActorSelectionCommandTrigger clear =
                navigation.ClearActorSelectionCommand;
            Require(select != null && selectDefault != null && replace != null && clear != null,
                "Actor command QA fixture is missing one explicit command.");

            // Slot-not-joined: every mutating command rejects without a revision.
            Configure(select, firstSlot, actorA, PlayerActorSelectionRequest.NoExpectedRevision);
            select.Invoke();
            RequireStatus(select.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedSlotNotJoined, "select-not-joined");
            Configure(replace, firstSlot, actorB, PlayerActorSelectionRequest.NoExpectedRevision);
            replace.Invoke();
            RequireStatus(replace.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedSlotNotJoined, "replace-not-joined");
            Configure(clear, firstSlot, null, PlayerActorSelectionRequest.NoExpectedRevision);
            clear.Invoke();
            RequireStatus(clear.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedSlotNotJoined, "clear-not-joined");
            Require(GetSlot(access, firstSlot.PlayerSlotId).SelectionRevision == 0,
                "Not-joined Actor commands mutated the Slot selection revision.");

            PlayerParticipationOperationResult open = access.OpenJoining(
                Source, "open-joining");
            Require(open != null && open.Completed && open.Snapshot != null &&
                open.Snapshot.JoiningOpen,
                Describe(open));
            LocalPlayerJoinResult joinA = access.RequestJoin(
                new LocalPlayerJoinRequest(Source, "join-primary"));
            Require(joinA != null && joinA.Succeeded && joinA.Slot.IsJoined,
                Describe(joinA));
            PlayerSlotId firstSlotId = joinA.Slot.PlayerSlotId;

            int revision0 = GetSlot(access, firstSlotId).SelectionRevision;
            Configure(select, firstSlot, actorA, revision0);
            select.Invoke();
            RequireStatus(select.LastActorSelectionResult,
                PlayerActorSelectionStatus.SucceededSelected, "select-explicit");
            Require(ReferenceEquals(select.LastActorSelectionResult.SelectedActorProfile, actorA) &&
                select.LastActorSelectionResult.SelectionRevision == revision0 + 1,
                "Explicit Select did not select Actor A or advance revision exactly once.");

            Configure(select, firstSlot, actorA, revision0 + 1);
            select.Invoke();
            RequireStatus(select.LastActorSelectionResult,
                PlayerActorSelectionStatus.SucceededSelected, "select-idempotent");
            Require(!select.LastActorSelectionResult.StateChanged &&
                select.LastActorSelectionResult.SelectionRevision == revision0 + 1,
                "Idempotent Select changed the selection revision.");

            Configure(select, firstSlot, actorB, revision0 + 1);
            select.Invoke();
            RequireStatus(select.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedInvalidRequest, "select-conflict");
            Require(select.LastActorSelectionResult.Message.IndexOf("ReplaceActorSelection", StringComparison.Ordinal) >= 0 &&
                GetSlot(access, firstSlotId).SelectionRevision == revision0 + 1,
                "Select conflict did not preserve state and require Replace.");

            Configure(replace, firstSlot, actorB, revision0 + 1);
            replace.Invoke();
            RequireStatus(replace.LastActorSelectionResult,
                PlayerActorSelectionStatus.SucceededReplaced, "replace");
            Require(ReferenceEquals(replace.LastActorSelectionResult.SelectedActorProfile, actorB) &&
                replace.LastActorSelectionResult.SelectionRevision == revision0 + 2,
                "Replace did not select Actor B or advance revision exactly once.");

            Configure(clear, firstSlot, null, revision0 + 2);
            clear.Invoke();
            RequireStatus(clear.LastActorSelectionResult,
                PlayerActorSelectionStatus.SucceededCleared, "clear");
            Require(!clear.LastActorSelectionResult.Slot.HasSelectedActor &&
                clear.LastActorSelectionResult.SelectionRevision == revision0 + 3,
                "Clear did not remove the Actor or advance revision exactly once.");

            Configure(selectDefault, firstSlot, null, revision0 + 3);
            selectDefault.Invoke();
            RequireStatus(selectDefault.LastActorSelectionResult,
                PlayerActorSelectionStatus.SucceededSelected, "select-default");
            Require(ReferenceEquals(selectDefault.LastActorSelectionResult.SelectedActorProfile, actorA),
                "Default command did not resolve the configured default Actor.");

            int currentRevision = selectDefault.LastActorSelectionResult.SelectionRevision;
            Configure(clear, firstSlot, null, currentRevision + 9);
            clear.Invoke();
            RequireStatus(clear.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedStaleSelectionRevision, "stale-revision");
            Require(GetSlot(access, firstSlotId).SelectionRevision == currentRevision,
                "Stale Actor command mutated selection state.");

            InputDevice device = joinA.PlayerInput != null && joinA.PlayerInput.devices.Count > 0
                ? joinA.PlayerInput.devices[0]
                : null;
            Require(device != null && device.added,
                "Duplicate policy proof requires the joined Player input device.");
            LocalPlayerJoinResult joinB = access.RequestJoin(
                new LocalPlayerJoinRequest(Source, "join-second-slot", device));
            Require(joinB != null && joinB.Succeeded && joinB.Slot.IsJoined,
                Describe(joinB));
            PlayerSlotProfile secondSlot = joinB.Slot.Profile;
            Require(secondSlot != null, "Second joined Slot has no authored Slot Profile.");
            Configure(select, secondSlot, actorA, joinB.Slot.SelectionRevision);
            select.Invoke();
            RequireStatus(select.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedDuplicateActorSelection, "duplicate-policy");
            Require(GetSlot(access, joinB.Slot.PlayerSlotId).SelectionRevision ==
                    joinB.Slot.SelectionRevision,
                "Duplicate Actor selection mutated the second Slot.");
            Configure(select, secondSlot, actorB, joinB.Slot.SelectionRevision);
            select.Invoke();
            RequireStatus(select.LastActorSelectionResult,
                PlayerActorSelectionStatus.SucceededSelected, "second-slot-distinct-select");

            await QaPlayerSurfacePublicNavigationSupport.RequireCompositionBoundAsync(
                navigation.EnterActivityTrigger, FrameBudget);
            QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(
                navigation.EnterActivityTrigger);
            await AwaitPreparedAsync(access, firstSlotId, FrameBudget);
            int preparedRevision = GetSlot(access, firstSlotId).SelectionRevision;
            Configure(select, firstSlot, actorA, preparedRevision);
            select.Invoke();
            RequireStatus(select.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedLogicalActorAlreadyPrepared,
                "prepared-select-barrier");
            Configure(replace, firstSlot, actorB, preparedRevision);
            replace.Invoke();
            RequireStatus(replace.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedLogicalActorAlreadyPrepared,
                "prepared-replace-barrier");
            Configure(clear, firstSlot, null, preparedRevision);
            clear.Invoke();
            RequireStatus(clear.LastActorSelectionResult,
                PlayerActorSelectionStatus.RejectedLogicalActorAlreadyPrepared,
                "prepared-clear-barrier");

            PlayerSessionSelectActorCommandTrigger activityScopedSelect =
                navigation.UnavailableSelectActorCommand;
            ILocalPlayerProvisioningConsumerAccess activityScopedAccess = null;
            string activityScopedIssue = string.Empty;
            bool activityScopedAccessAvailable = activityScopedSelect != null &&
                activityScopedSelect.TryGetAccess(
                    out activityScopedAccess,
                    out activityScopedIssue);
            Require(
                activityScopedSelect != null &&
                activityScopedSelect.Scope ==
                    LocalPlayerProvisioningConsumerScope.Activity &&
                activityScopedSelect.BindingState ==
                    PlayerSessionScopedAccessState.Bound &&
                activityScopedAccessAvailable &&
                activityScopedAccess != null &&
                activityScopedAccess.Snapshot.IsAvailable &&
                activityScopedAccess.Snapshot.Scope ==
                    LocalPlayerProvisioningConsumerScope.Activity &&
                activityScopedAccess.Snapshot.Owner.IsValid,
                "Activity-scoped Select Actor command on Route content must bind only " +
                "to the live Activity authority and never fall back to Route authority. " +
                $"state='{activityScopedSelect?.BindingState}' " +
                $"scope='{activityScopedAccess?.Snapshot.Scope}' " +
                $"owner='{activityScopedAccess?.Snapshot.Owner.StableText}' " +
                $"issue='{activityScopedIssue}' " +
                $"diagnostic='{activityScopedSelect?.Diagnostic}'.");

            Require(select.LastActorSelectionResult != null &&
                selectDefault.LastActorSelectionResult != null &&
                replace.LastActorSelectionResult != null &&
                clear.LastActorSelectionResult != null,
                "Every explicit Actor command must retain typed LastActorSelectionResult evidence.");
            Debug.Log("[QA_PLAYER_ACTOR_COMMANDS_01] status='Passed' " +
                "cases='select,idempotent,conflict,default,replace,clear,notJoined,stale,duplicate,preparedBarrier,activityScopedAccess,typedEvidence' " +
                "note='Default-disabled and retained-failure cases require a dedicated deterministic public-command fixture.'");
        }

        private static void Configure(
            PlayerSessionCommandTriggerBase command,
            PlayerSlotProfile slot,
            ActorProfile actor,
            int expectedSelectionRevision)
        {
            var serialized = new SerializedObject(command);
            SerializedProperty slotProperty = serialized.FindProperty("playerSlot");
            SerializedProperty revisionProperty = serialized.FindProperty("expectedSelectionRevision");
            Require(slotProperty != null && revisionProperty != null,
                $"{command.GetType().Name} has an incomplete serialized Actor command contract.");
            slotProperty.objectReferenceValue = slot;
            revisionProperty.intValue = expectedSelectionRevision;
            SerializedProperty actorProperty = serialized.FindProperty("actorProfile");
            if (actorProperty != null)
            {
                actorProperty.objectReferenceValue = actor;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static async Task<ILocalPlayerProvisioningConsumerAccess> AwaitScopedAccessAsync(
            PlayerSessionScopedAccessConsumer binding,
            int frameBudget)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (binding != null && binding.TryGetAccess(
                        out ILocalPlayerProvisioningConsumerAccess access, out _) &&
                    access != null && access.Snapshot.IsAvailable)
                {
                    return access;
                }
                await Awaitable.NextFrameAsync();
            }
            throw new TimeoutException("Route-scoped Player access did not become available.");
        }

        private static PlayerSlotRuntimeSnapshot GetSlot(
            ILocalPlayerProvisioningConsumerAccess access,
            PlayerSlotId slotId)
        {
            LocalPlayerProvisioningConsumerObservationSnapshot observation = null;
            bool observationAvailable = access != null &&
                access.TryGetObservation(out observation);
            Require(observationAvailable &&
                observation != null && observation.IsAvailable,
                "Actor command QA has no public scoped observation.");
            foreach (PlayerSlotRuntimeSnapshot slot in observation.Participation.Slots)
            {
                if (slot.PlayerSlotId == slotId)
                {
                    return slot;
                }
            }
            throw new InvalidOperationException($"Public observation has no Slot '{slotId.StableText}'.");
        }

        private static async Task AwaitPreparedAsync(
            ILocalPlayerProvisioningConsumerAccess access,
            PlayerSlotId slotId,
            int frameBudget)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (access.TryGetObservation(
                        out LocalPlayerProvisioningConsumerObservationSnapshot observation) &&
                    observation != null && observation.IsAvailable &&
                    observation.Lifecycle != null && observation.Lifecycle.IsReady)
                {
                    foreach (LocalPlayerProvisioningConsumerSlotObservation slot in observation.Slots)
                    {
                        if (slot.Slot.PlayerSlotId == slotId &&
                            slot.IsLogicalActorPrepared)
                        {
                            return;
                        }
                    }
                }
                await Awaitable.NextFrameAsync();
            }
            throw new TimeoutException("Public Actor command QA did not reach prepared Actor evidence.");
        }

        private static void RequireStatus(
            PlayerActorSelectionResult result,
            PlayerActorSelectionStatus expected,
            string step)
        {
            Require(result != null && result.Status == expected,
                $"Actor command step '{step}' expected '{expected}', actual '{(result != null ? result.ToDiagnosticString() : "missing")}'.");
        }

        private static string Describe(PlayerParticipationOperationResult result) =>
            result != null ? result.ToDiagnosticString() : "missing Player participation result.";

        private static string Describe(LocalPlayerJoinResult result) =>
            result != null ? result.ToDiagnosticString() : "missing Player join result.";

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

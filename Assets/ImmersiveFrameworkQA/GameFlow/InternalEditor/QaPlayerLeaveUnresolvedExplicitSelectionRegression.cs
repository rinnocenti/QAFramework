using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// QA-PLAYER-LEAVE-UNRESOLVED-01 — public lifecycle proof that an unresolved
    /// Manager-Provisioned Slot waits for explicit Actor intent instead of asking
    /// the Session to select its configured default Actor.
    /// </summary>
    public static class QaPlayerLeaveUnresolvedExplicitSelectionRegression
    {
        private const string Prefix = "[QA_PLAYER_LEAVE_UNRESOLVED_01]";
        private const string Source = "qa-player-leave-unresolved-01";
        private const int ExpectedCaseCount = 22;
        private const int FrameBudget = 360;
        private const string AlternateActorPath =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/QA_AlternateActor.asset";

        public static Task RunForFullPlayerQaAsync() => RunAsync();

        private static async Task RunAsync()
        {
            var completed = new List<string>();
            try
            {
                Require(EditorApplication.isPlaying,
                    "LeaveUnresolved explicit-selection regression requires Play Mode.");
                completed.Add("play-mode-required");

                QaPlayerLeaveUnresolvedExplicitSelectionSetup
                    .RequirePreparedForCurrentPlayMode();
                completed.Add("leave-unresolved-session-prepared");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string hostIssue), hostIssue);
                await QaH2FrameworkReadiness.RequireStartedRouteAsync(host, FrameBudget);
                completed.Add("official-host-ready");

                Require(QaPlayerSurfacePublicNavigationSupport
                        .TryResolveAuthoredFixture(
                            out QaPlayerSurfacePublicNavigationFixture navigation,
                            out string navigationIssue), navigationIssue);
                Require(QaPlayerSurfacePublicNavigationSupport
                        .TryResolveGlobalUiFixture(
                            out QaPlayerSurfaceGlobalUiFixture globalUi,
                            out string globalUiIssue), globalUiIssue);
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireProvisioningRuntimeReadyAsync(globalUi, FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireActorSelectionRuntimeReadyAsync(navigation, FrameBudget);
                completed.Add("public-surface-bound");

                ILocalPlayerProvisioningConsumerAccess access =
                    await AwaitScopedAccessAsync(
                        navigation.RouteConsumerBinding, FrameBudget);
                PlayerSlotProfile slotProfile = navigation.PrimaryPlayerSlot;
                ActorProfile actorA = slotProfile != null
                    ? slotProfile.DefaultActorProfile
                    : null;
                ActorProfile actorB = AssetDatabase.LoadAssetAtPath<ActorProfile>(
                    AlternateActorPath);
                Require(slotProfile != null && actorA != null && actorB != null &&
                    !ReferenceEquals(actorA, actorB),
                    "LeaveUnresolved regression requires distinct authored Actor A and B profiles.");
                PlayerSessionSelectActorCommandTrigger selectActor =
                    navigation.SelectActorCommand;
                Require(selectActor != null,
                    "LeaveUnresolved regression requires the authored Select Actor command.");
                Require(access.TryGetObservation(
                        out LocalPlayerProvisioningConsumerObservationSnapshot
                            initializationObservation) &&
                    initializationObservation != null &&
                    initializationObservation.InitializationConfiguration != null &&
                    initializationObservation.InitializationConfiguration
                        .ActorResolutionPolicy ==
                        PlayerActorResolutionPolicy.LeaveUnresolved,
                    "Public Session initialization evidence did not retain LeaveUnresolved.");
                completed.Add("explicit-selection-command-resolved");

                await QaPlayerSurfacePublicNavigationSupport.RequireCompositionBoundAsync(
                    navigation.EnterActivityTrigger, FrameBudget);
                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(
                    navigation.EnterActivityTrigger);
                await QaPlayerSurfacePublicNavigationSupport.AwaitTriggerInFlightAsync(
                    navigation.EnterActivityTrigger,
                    FrameBudget,
                    "LeaveUnresolved Activity entry did not remain in-flight before Join.");
                completed.Add("gameplay-ready-activity-entered");

                await AwaitObservationAsync(
                    access,
                    observation => observation.Lifecycle != null &&
                        observation.Lifecycle.Status ==
                            ManagerProvisionedPlayerLifecycleStatus.WaitingForJoin &&
                        observation.Lifecycle.GateHeld,
                    "LeaveUnresolved Activity did not begin by waiting for Join.");
                completed.Add("waiting-for-join-observed");

                PlayerParticipationOperationResult open = access.OpenJoining(
                    Source, "open-joining");
                Require(open != null && open.Succeeded && open.Snapshot != null &&
                    open.Snapshot.JoiningOpen, Describe(open));
                completed.Add("joining-opened");

                LocalPlayerJoinResult firstJoin = access.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "join-first-unresolved"));
                Require(firstJoin != null && firstJoin.Succeeded &&
                    firstJoin.Slot.IsJoined &&
                    firstJoin.Slot.SelectionRevision == 0,
                    Describe(firstJoin));
                completed.Add("first-join-succeeded");

                LocalPlayerProvisioningConsumerObservationSnapshot firstWaiting =
                    await AwaitObservationAsync(
                        access,
                        observation => IsWaitingForExplicitSelection(
                            observation,
                            firstJoin.Slot.PlayerSlotId,
                            firstJoin.Slot.SelectionRevision),
                        "Joined LeaveUnresolved Slot did not remain Preparing / WaitingForActorSelection.");
                completed.Add("waiting-for-explicit-selection-observed");

                PlayerSlotRuntimeSnapshot firstWaitingSlot = FindSlot(
                    firstWaiting.Participation,
                    firstJoin.Slot.PlayerSlotId);
                ManagerProvisionedPlayerLifecycleSlotSnapshot firstWaitingLifecycle =
                    FindLifecycleSlot(firstWaiting.Lifecycle,
                        firstJoin.Slot.PlayerSlotId);
                Require(!firstWaitingSlot.HasSelectedActor &&
                    firstWaitingSlot.SelectionRevision ==
                        firstJoin.Slot.SelectionRevision &&
                    !firstWaitingLifecycle.LogicalActorPrepared &&
                    !firstWaitingLifecycle.PhysicalActorMaterialized &&
                    !firstWaitingLifecycle.GameplayAdmitted &&
                    !firstWaiting.Lifecycle.IsFailure,
                    "LeaveUnresolved Join committed default Actor selection or failed readiness. " +
                    Describe(firstWaiting));
                completed.Add("no-default-selection-or-failure");

                ConfigureSelectActorCommand(
                    selectActor,
                    slotProfile,
                    actorA,
                    firstWaitingSlot.SelectionRevision);
                selectActor.Invoke();
                Require(selectActor.LastActorSelectionResult != null &&
                    selectActor.LastActorSelectionResult.Succeeded &&
                    selectActor.LastActorSelectionResult.StateChanged &&
                    ReferenceEquals(
                        selectActor.LastActorSelectionResult.SelectedActorProfile,
                        actorA),
                    Describe(selectActor.LastActorSelectionResult));
                completed.Add("first-explicit-selection-succeeded");

                LocalPlayerProvisioningConsumerObservationSnapshot firstReady =
                    await AwaitReadyAsync(
                        access,
                        firstJoin.Slot.PlayerSlotId,
                        actorA,
                        "First explicit Actor selection did not complete the normal lifecycle.");
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        navigation.EnterActivityTrigger,
                        FrameBudget,
                        "GameplayReady Activity did not complete after explicit Actor A selection.");
                completed.Add("first-selection-completed");

                PlayerSlotRuntimeSnapshot firstReadySlot = FindSlot(
                    firstReady.Participation,
                    firstJoin.Slot.PlayerSlotId);
                SessionPlayerLeaveResult firstLeave = access.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        firstReadySlot.PlayerSlotId,
                        firstReadySlot.Revision,
                        Source,
                        "leave-first-ready-player"));
                Require(firstLeave != null && firstLeave.Succeeded, Describe(firstLeave));
                completed.Add("first-leave-succeeded");

                await AwaitObservationAsync(
                    access,
                    observation => observation.Lifecycle != null &&
                        observation.Lifecycle.Status ==
                            ManagerProvisionedPlayerLifecycleStatus.WaitingForJoin &&
                        observation.Lifecycle.GateHeld &&
                        !FindSlot(observation.Participation,
                            firstJoin.Slot.PlayerSlotId).IsJoined,
                    "Leave did not restore the active Activity Player lifecycle to WaitingForJoin.");
                completed.Add("leave-restored-waiting-for-join");

                LocalPlayerJoinResult secondJoin = access.RequestJoin(
                    new LocalPlayerJoinRequest(Source, "join-second-unresolved"));
                Require(secondJoin != null && secondJoin.Succeeded &&
                    secondJoin.Slot.IsJoined, Describe(secondJoin));
                completed.Add("second-join-succeeded");

                LocalPlayerProvisioningConsumerObservationSnapshot secondWaiting =
                    await AwaitObservationAsync(
                        access,
                        observation => IsWaitingForExplicitSelection(
                            observation,
                            secondJoin.Slot.PlayerSlotId,
                            secondJoin.Slot.SelectionRevision),
                        "Rejoined LeaveUnresolved Slot did not return to WaitingForActorSelection.");
                completed.Add("rejoin-waiting-for-explicit-selection-observed");

                PlayerSlotRuntimeSnapshot secondWaitingSlot = FindSlot(
                    secondWaiting.Participation,
                    secondJoin.Slot.PlayerSlotId);
                Require(!secondWaitingSlot.HasSelectedActor &&
                    secondWaitingSlot.SelectionRevision ==
                        secondJoin.Slot.SelectionRevision,
                    "Rejoined LeaveUnresolved Slot acquired an Actor without explicit intent. " +
                    Describe(secondWaiting));
                completed.Add("rejoin-no-default-selection");

                ConfigureSelectActorCommand(
                    selectActor,
                    slotProfile,
                    actorB,
                    secondWaitingSlot.SelectionRevision);
                selectActor.Invoke();
                Require(selectActor.LastActorSelectionResult != null &&
                    selectActor.LastActorSelectionResult.Succeeded &&
                    selectActor.LastActorSelectionResult.StateChanged &&
                    ReferenceEquals(
                        selectActor.LastActorSelectionResult.SelectedActorProfile,
                        actorB),
                    Describe(selectActor.LastActorSelectionResult));
                completed.Add("second-explicit-selection-succeeded");

                LocalPlayerProvisioningConsumerObservationSnapshot secondReady =
                    await AwaitReadyAsync(
                        access,
                        secondJoin.Slot.PlayerSlotId,
                        actorB,
                        "Second explicit Actor selection did not complete the normal lifecycle.");
                completed.Add("second-selection-completed");

                PlayerSlotRuntimeSnapshot secondReadySlot = FindSlot(
                    secondReady.Participation,
                    secondJoin.Slot.PlayerSlotId);
                SessionPlayerLeaveResult cleanupLeave = access.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        secondReadySlot.PlayerSlotId,
                        secondReadySlot.Revision,
                        Source,
                        "cleanup-second-ready-player"));
                Require(cleanupLeave != null && cleanupLeave.Succeeded,
                    Describe(cleanupLeave));
                completed.Add("second-leave-cleanup-succeeded");

                QaPlayerSurfacePublicNavigationSupport.ClearActivityPublic(
                    navigation.ClearActivityTrigger);
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        navigation.ClearActivityTrigger,
                        FrameBudget,
                        "LeaveUnresolved regression did not clear its Activity authority.");
                completed.Add("activity-cleaned-up");

                Require(completed.Count == ExpectedCaseCount,
                    "LeaveUnresolved explicit-selection case count changed unexpectedly.");
                Debug.Log(
                    $"{Prefix} status='Passed' cases='{completed.Count}' " +
                    "proof='WaitingForActorSelection,NoDefaultSelection,ExplicitRecovery,LeaveRejoin,ResolveConfiguredDefaultPreservedElsewhere' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static bool IsWaitingForExplicitSelection(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId,
            int expectedSelectionRevision)
        {
            if (observation == null || !observation.IsAvailable ||
                observation.Lifecycle == null || observation.Participation == null ||
                observation.Lifecycle.Status !=
                    ManagerProvisionedPlayerLifecycleStatus.WaitingForActorSelection ||
                !string.Equals(observation.Lifecycle.ReadinessStatus, "Preparing",
                    StringComparison.Ordinal) ||
                !string.Equals(observation.Lifecycle.ReadinessReason,
                    ActivityPlayerActorReadinessReason.WaitingForActorSelection.ToString(),
                    StringComparison.Ordinal) ||
                !observation.Lifecycle.HasGateEvidence ||
                !observation.Lifecycle.GateHeld ||
                observation.Lifecycle.IsFailure)
            {
                return false;
            }

            PlayerSlotRuntimeSnapshot slot = FindSlot(observation.Participation, slotId);
            return slot.IsJoined && !slot.HasSelectedActor &&
                slot.SelectionRevision == expectedSelectionRevision;
        }

        private static async Task<LocalPlayerProvisioningConsumerObservationSnapshot>
            AwaitReadyAsync(
                ILocalPlayerProvisioningConsumerAccess access,
                PlayerSlotId slotId,
                ActorProfile selectedActor,
                string failure)
        {
            return await AwaitObservationAsync(
                access,
                observation =>
                {
                    if (observation.Lifecycle == null ||
                        observation.Lifecycle.Status !=
                            ManagerProvisionedPlayerLifecycleStatus.Ready ||
                        observation.Lifecycle.GateHeld)
                    {
                        return false;
                    }

                    PlayerSlotRuntimeSnapshot slot = FindSlot(
                        observation.Participation, slotId);
                    ManagerProvisionedPlayerLifecycleSlotSnapshot lifecycleSlot =
                        FindLifecycleSlot(observation.Lifecycle, slotId);
                    return slot.IsJoined &&
                        ReferenceEquals(slot.SelectedActorProfile, selectedActor) &&
                        lifecycleSlot.LogicalActorPrepared &&
                        lifecycleSlot.PhysicalActorMaterialized &&
                        lifecycleSlot.GameplayAdmitted;
                },
                failure);
        }

        private static async Task<ILocalPlayerProvisioningConsumerAccess>
            AwaitScopedAccessAsync(
                PlayerSessionScopedAccessConsumer binding,
                int frameBudget)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (binding != null && binding.TryGetAccess(
                        out ILocalPlayerProvisioningConsumerAccess access,
                        out _) && access != null && access.Snapshot.IsAvailable)
                {
                    return access;
                }
                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "LeaveUnresolved regression did not acquire Route-scoped Player access.");
        }

        private static async Task<LocalPlayerProvisioningConsumerObservationSnapshot>
            AwaitObservationAsync(
                ILocalPlayerProvisioningConsumerAccess access,
                Func<LocalPlayerProvisioningConsumerObservationSnapshot, bool> predicate,
                string failure)
        {
            LocalPlayerProvisioningConsumerObservationSnapshot last = null;
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                if (access != null && access.TryGetObservation(out last) &&
                    last != null && last.IsAvailable && predicate(last))
                {
                    return last;
                }
                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                failure + " last='" + Describe(last) + "'.");
        }

        private static void ConfigureSelectActorCommand(
            PlayerSessionSelectActorCommandTrigger command,
            PlayerSlotProfile slot,
            ActorProfile actor,
            int expectedSelectionRevision)
        {
            var serialized = new SerializedObject(command);
            SerializedProperty slotProperty = serialized.FindProperty("playerSlot");
            SerializedProperty actorProperty = serialized.FindProperty("actorProfile");
            SerializedProperty revisionProperty = serialized.FindProperty(
                "expectedSelectionRevision");
            Require(slotProperty != null && actorProperty != null &&
                revisionProperty != null,
                "Select Actor command serialized contract is incomplete.");
            slotProperty.objectReferenceValue = slot;
            actorProperty.objectReferenceValue = actor;
            revisionProperty.intValue = expectedSelectionRevision;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot participation,
            PlayerSlotId slotId)
        {
            if (participation != null)
            {
                for (int index = 0; index < participation.Slots.Count; index++)
                {
                    PlayerSlotRuntimeSnapshot slot = participation.Slots[index];
                    if (slot.PlayerSlotId == slotId)
                    {
                        return slot;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Public Player observation has no Slot '{slotId.StableText}'.");
        }

        private static ManagerProvisionedPlayerLifecycleSlotSnapshot
            FindLifecycleSlot(
                ManagerProvisionedPlayerLifecycleSnapshot lifecycle,
                PlayerSlotId slotId)
        {
            if (lifecycle != null)
            {
                for (int index = 0; index < lifecycle.Slots.Count; index++)
                {
                    ManagerProvisionedPlayerLifecycleSlotSnapshot slot =
                        lifecycle.Slots[index];
                    if (string.Equals(slot.PlayerSlotId, slotId.StableText,
                        StringComparison.Ordinal))
                    {
                        return slot;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Public lifecycle projection has no Slot '{slotId.StableText}'.");
        }

        private static string Describe(
            LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            if (observation == null)
            {
                return "missing-observation";
            }

            return
                $"available='{observation.IsAvailable}' scope='{observation.Scope}' " +
                $"sessionRevision='{observation.SessionRevision}' " +
                $"activityOccurrence='{observation.ActivityOccurrence}' " +
                $"lifecycle=[{observation.Lifecycle?.ToDiagnosticString() ?? string.Empty}] " +
                $"diagnostic='{observation.Diagnostic}'";
        }

        private static string Describe(PlayerParticipationOperationResult result)
        {
            return result != null ? result.ToDiagnosticString() : "missing-result";
        }

        private static string Describe(LocalPlayerJoinResult result)
        {
            return result != null ? result.ToDiagnosticString() : "missing-join";
        }

        private static string Describe(PlayerActorSelectionResult result)
        {
            return result != null ? result.ToDiagnosticString() : "missing-selection";
        }

        private static string Describe(SessionPlayerLeaveResult result)
        {
            return result != null ? result.ToDiagnosticString() : "missing-leave";
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
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.CycleReset;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Reset;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Internal.Editor
{
    /// <summary>
    /// IF-ADR-021 Model B — Route Player Spatial Entry regression.
    /// Exercises RouteLifecycleRuntime publication into the Host-scoped spatial-entry
    /// gate used by PlayerActorPreparationRuntimeHostModule. It does not replace the
    /// historical Activity initial-placement suite.
    /// </summary>
    public static class QaAdr21RoutePlayerSpatialEntryRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run ADR-021 Route Spatial Entry QA";
        private const string Prefix = "[QA_ADR021_ROUTE_SPATIAL_ENTRY]";
        private const int ExpectedCaseCount = 18;
        private const string SlotP1Path =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/SlotsProfiles/PlayerSlotProfileP1.asset";
        private const string SlotP2Path =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/SlotsProfiles/PlayerSlotProfileP2.asset";
        private const string ActorProfilePath =
            "Assets/ImmersiveFrameworkQA/Player/P3H4/P3H4_DefaultActor.asset";
        private const string InputActionsPath =
            "Assets/ImmersiveFrameworkQA/Player/LocalPlayerRuntimeIntegration/LocalPlayerInputActions.asset";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            if (!Execute(out string error))
            {
                throw new InvalidOperationException(error);
            }
        }

        public static bool Execute(out string error)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                error = "ADR-021 Route Spatial Entry QA must run in Edit Mode.";
                Debug.LogError($"{Prefix} status='Failed' cases='0/{ExpectedCaseCount}' error='{Escape(error)}'.");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                error = "ADR-021 Route Spatial Entry QA was cancelled because the current Editor scenes were not saved.";
                Debug.LogError($"{Prefix} status='Failed' cases='0/{ExpectedCaseCount}' error='{Escape(error)}'.");
                return false;
            }

            var failures = new List<string>();
            int passed = 0;
            PlayerSlotProfile p1 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP1Path);
            PlayerSlotProfile p2 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP2Path);
            ActorProfile actorProfile = AssetDatabase.LoadAssetAtPath<ActorProfile>(ActorProfilePath);
            SceneSetup[] initialSetup = EditorSceneManager.GetSceneManagerSetup();
            World world = null;

            try
            {
                Require(p1 != null, $"Missing QA PlayerSlotProfile at '{SlotP1Path}'.");
                Require(p2 != null, $"Missing QA PlayerSlotProfile at '{SlotP2Path}'.");
                Require(actorProfile != null, $"Missing QA ActorProfile at '{ActorProfilePath}'.");
                world = World.Create();

                RunCase("RouteWithoutActivityPrimaryScenePlacement",
                    () => ProveRouteWithoutActivityPrimaryScenePlacement(world, p1, actorProfile), failures, ref passed);
                RunCase("ActivityContentProfileNullDoesNotBlockRouteEntry",
                    () => ProveActivityContentProfileNullDoesNotBlockRouteEntry(world, p1, actorProfile), failures, ref passed);
                RunCase("LateManagerJoinUsesCurrentRouteOccurrence",
                    () => ProveLateManagerJoinUsesCurrentRouteOccurrence(world, p1, actorProfile), failures, ref passed);
                RunCase("ExistingPlayerReceivesNewRouteOccurrence",
                    () => ProveExistingPlayerReceivesNewRouteOccurrence(world, p1, actorProfile), failures, ref passed);
                RunCase("ReturnToSameRouteCreatesNewOccurrence",
                    () => ProveReturnToSameRouteCreatesNewOccurrence(world, p1, actorProfile), failures, ref passed);
                RunCase("SameOccurrenceIsIdempotent",
                    () => ProveSameOccurrenceIsIdempotent(world, p1, actorProfile), failures, ref passed);
                RunCase("PreserveCurrentPoseRequiresNoAnchor",
                    () => ProvePreserveCurrentPoseRequiresNoAnchor(world, p1, actorProfile), failures, ref passed);
                RunCase("ApplyExplicitPlacementMissingBindingFails",
                    () => ProveApplyExplicitPlacementMissingBindingFails(world, p1, actorProfile), failures, ref passed);
                RunCase("ApplyExplicitPlacementDuplicateFails",
                    () => ProveApplyExplicitPlacementDuplicateFails(world, p1, actorProfile), failures, ref passed);
                RunCase("RouteContentAnchorIsEligible",
                    () => ProveRouteContentAnchorIsEligible(world, p1, actorProfile), failures, ref passed);
                RunCase("ActivityContentAnchorIsNotEligibleForRouteEntry",
                    () => ProveActivityContentAnchorIsNotEligible(world, p1, actorProfile), failures, ref passed);
                RunCase("UnrelatedLoadedSceneIgnored",
                    () => ProveUnrelatedLoadedSceneIgnored(world, p1, actorProfile), failures, ref passed);
                RunCase("SceneProvidedPreserveAuthoredPoseUsesRoutePolicy",
                    () => ProveSceneProvidedPreserveAuthoredPoseUsesRoutePolicy(world, p1, actorProfile), failures, ref passed);
                RunCase("SceneProvidedApplyUsesRoutePolicy",
                    () => ProveSceneProvidedApplyUsesRoutePolicy(world, p1, actorProfile), failures, ref passed);
                RunCase("ActivityEnterWithoutRelocationDoesNotRepeatRouteEntry",
                    () => ProveActivityEnterWithoutRelocationDoesNotRepeatRouteEntry(world, p1, actorProfile), failures, ref passed);
                RunCase("RouteExitClearsCurrentOccurrence",
                    () => ProveRouteExitClearsCurrentOccurrence(world, p1, actorProfile), failures, ref passed);
                RunCase("FailedRouteEntryIsNotMarkedSuccessfullyApplied",
                    () => ProveFailedRouteEntryIsNotMarkedSuccessfullyApplied(world, p1, actorProfile), failures, ref passed);
                RunCase("PhysicalRepresentationReplacementWithinSameRouteOccurrence",
                    () => ProvePhysicalRepresentationReplacementWithinSameRouteOccurrence(world, p1, actorProfile), failures, ref passed);
            }
            catch (Exception exception)
            {
                failures.Add($"harness: {exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                world?.Dispose();
                try
                {
                    if (initialSetup != null && initialSetup.Length > 0)
                    {
                        EditorSceneManager.RestoreSceneManagerSetup(initialSetup);
                    }
                }
                catch (Exception exception)
                {
                    Debug.LogError($"{Prefix} status='Cleanup Failed' error='{Escape(exception.Message)}'.");
                }
            }

            if (passed != ExpectedCaseCount || failures.Count > 0)
            {
                error = failures.Count > 0
                    ? string.Join(" | ", failures)
                    : $"Expected {ExpectedCaseCount} passing cases but observed {passed}.";
                Debug.LogError($"{Prefix} status='Failed' cases='{passed}/{ExpectedCaseCount}' error='{Escape(error)}'.");
                return false;
            }

            error = string.Empty;
            Debug.Log($"{Prefix} status='Passed' verdict='ADR-021 MODEL B ROUTE SPATIAL ENTRY VERIFIED' cases='{passed}/{ExpectedCaseCount}'.");
            return true;
        }

        private static void ProveRouteWithoutActivityPrimaryScenePlacement(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                Vector3 anchorPose = new Vector3(12f, 3f, -7f);
                Quaternion anchorRotation = Quaternion.Euler(0f, 137f, 0f);
                authoring = CreateRouteBinding(world.PrimaryA, p1, anchorPose, anchorRotation);
                route = CreateRoute(
                    "qa.adr021.route.primary-only",
                    "ADR021 Primary Only",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                Require(!route.HasStartupActivity, "CASE 1 Route must have Startup Activity = none.");
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case1", "qa.physical.case1");
                PoseSnapshot before = PoseSnapshot.Capture(player.Target);
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(
                    runtime.SetPlayerSpatialEntryParticipant(participant, out string attachIssue),
                    "CASE 1 could not attach the Route spatial-entry participant. " + attachIssue);
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(result.Started, "CASE 1 Route start failed. " + result.Message);
                Require(runtime.CurrentRoute != null && ReferenceEquals(runtime.CurrentRoute, route),
                    "CASE 1 did not publish the current Route.");
                Require(runtime.CurrentActivity == null, "CASE 1 required no Activity, but an Activity was current.");
                Require(participant.EnterCount == 1,
                    $"CASE 1 expected one Route spatial entry, observed '{participant.EnterCount}'.");
                Require(participant.LastContext.IsValid &&
                        participant.LastContext.OccurrenceSequence == 1 &&
                        ReferenceEquals(participant.LastContext.Route, route),
                    "CASE 1 did not publish a valid Route occurrence.");
                Require(participant.LastApplySucceeded, "CASE 1 Route spatial entry did not succeed. " + participant.LastIssue);
                Require(SamePose(player.Target, GetAnchor(authoring)),
                    "CASE 1 Player did not receive the Primary Scene anchor.");
                Require(player.Target.position != before.Position || !SameRotation(player.Target.rotation, before.Rotation),
                    "CASE 1 did not mutate the Player toward the Route anchor.");
                LogIdentity("RouteWithoutActivityPrimaryScenePlacement", participant, player, route, 1);
            }
            finally
            {
                CleanupCase(player, authoring, route);
            }
        }

        private static void ProveActivityContentProfileNullDoesNotBlockRouteEntry(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            ActivityAsset activity = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                Vector3 anchorPose = new Vector3(4f, 8f, 2f);
                authoring = CreateRouteBinding(world.PrimaryA, p1, anchorPose, Quaternion.identity);
                activity = CreateActivity("qa.adr021.activity.null-profile", "ADR021 Null Profile Activity", null);
                Require(activity.ActivityContentProfile == null, "CASE 2 ActivityContentProfile must be null.");
                route = CreateRoute(
                    "qa.adr021.route.null-profile",
                    "ADR021 Null Profile Route",
                    world.PrimaryA,
                    null,
                    activity,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case2", "qa.physical.case2");
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 2 could not attach the participant.");
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(result.Started, "CASE 2 Route start failed. " + result.Message);
                Require(participant.EnterCount == 1 && participant.LastApplySucceeded,
                    "CASE 2 Route spatial entry did not resolve with ActivityContentProfile = null. " + participant.LastIssue);
                Require(SamePose(player.Target, GetAnchor(authoring)),
                    "CASE 2 Player did not receive the Primary Scene anchor.");
                Require(participant.LastContext.DiscoveryScope.RouteOwnedScenes.Count >= 1,
                    "CASE 2 Route discovery unexpectedly required ActivityOwnedScenes.");
                for (int index = 0; index < participant.LastContext.DiscoveryScope.RouteOwnedScenes.Count; index++)
                {
                    Require(
                        !string.Equals(
                            participant.LastContext.DiscoveryScope.RouteOwnedScenes[index].ScenePath,
                            world.ActivityContent.path,
                            StringComparison.OrdinalIgnoreCase),
                        "CASE 2 Route discovery included Activity Content.");
                }

                LogIdentity("ActivityContentProfileNullDoesNotBlockRouteEntry", participant, player, route, 1);
            }
            finally
            {
                CleanupCase(player, authoring, route, activity);
            }
        }

        private static void ProveLateManagerJoinUsesCurrentRouteOccurrence(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                Vector3 anchorPose = new Vector3(-11f, 2f, 9f);
                Quaternion anchorRotation = Quaternion.Euler(0f, 45f, 0f);
                authoring = CreateRouteBinding(world.PrimaryA, p1, anchorPose, anchorRotation);
                route = CreateRoute(
                    "qa.adr021.route.late-join",
                    "ADR021 Late Join Route",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case3", "qa.physical.case3");
                PoseSnapshot unadmittedPose = PoseSnapshot.Capture(player.Target);
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: false);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 3 could not attach the participant.");
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(result.Started, "CASE 3 Route start failed. " + result.Message);
                Require(participant.EnterCount == 1 && participant.LastContext.IsValid,
                    "CASE 3 Route occurrence was not published before late admission.");
                Require(participant.ApplyInvocationCount == 0,
                    "CASE 3 applied spatial entry before any Player was admitted.");
                Require(SamePose(player.Target.position, unadmittedPose.Position) &&
                        SameRotation(player.Target.rotation, unadmittedPose.Rotation),
                    "CASE 3 moved the Player before Manager-Provisioned admission.");
                int occurrence = participant.LastContext.OccurrenceSequence;
                Require(participant.TryAdmitLate(out string admitIssue),
                    "CASE 3 late admission failed. " + admitIssue);
                Require(participant.EnterCount == 1,
                    "CASE 3 late admission required a new Route enter.");
                Require(participant.LastContext.OccurrenceSequence == occurrence,
                    "CASE 3 late admission did not reuse the current Route occurrence.");
                Require(SamePose(player.Target, GetAnchor(authoring)),
                    "CASE 3 late Player did not finish on the current Route anchor.");
                LogIdentity("LateManagerJoinUsesCurrentRouteOccurrence", participant, player, route, occurrence);
            }
            finally
            {
                CleanupCase(player, authoring, route);
            }
        }

        private static void ProveExistingPlayerReceivesNewRouteOccurrence(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset routeA = null;
            RouteAsset routeB = null;
            SessionPlayer player = null;
            GameObject authoringA = null;
            GameObject authoringB = null;
            try
            {
                authoringA = CreateRouteBinding(world.PrimaryA, p1, new Vector3(1f, 0f, 0f), Quaternion.identity);
                authoringB = CreateRouteBinding(world.PrimaryB, p1, new Vector3(30f, 1f, 5f), Quaternion.Euler(0f, 90f, 0f));
                routeA = CreateRoute(
                    "qa.adr021.route.a",
                    "ADR021 Route A",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                routeB = CreateRoute(
                    "qa.adr021.route.b",
                    "ADR021 Route B",
                    world.PrimaryB,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case4", "qa.physical.case4");
                string physicalId = player.PhysicalInstanceId;
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 4 could not attach the participant.");
                Require(StartRoute(runtime, routeA).Started, "CASE 4 Route A start failed.");
                Require(SamePose(player.Target, GetAnchor(authoringA)), "CASE 4 Player did not receive Route A.");
                int occurrenceA = participant.LastContext.OccurrenceSequence;
                Require(StartRoute(runtime, routeB).Started, "CASE 4 Route B start failed.");
                Require(player.PhysicalInstanceId == physicalId,
                    "CASE 4 replaced the physical Player occurrence. Route change must keep the Session Player.");
                Require(participant.LastContext.OccurrenceSequence != occurrenceA,
                    "CASE 4 did not publish a new Route occurrence for Route B.");
                Require(ReferenceEquals(participant.LastContext.Route, routeB),
                    "CASE 4 current occurrence was not Route B.");
                Require(participant.LastContext.Policy == RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement,
                    "CASE 4 did not evaluate Route B policy.");
                Require(SamePose(player.Target, GetAnchor(authoringB)),
                    "CASE 4 Player did not finish on Route B anchor.");
                LogIdentity("ExistingPlayerReceivesNewRouteOccurrence", participant, player, routeB, participant.LastContext.OccurrenceSequence);
            }
            finally
            {
                CleanupCase(player, authoringA, authoringB, routeA, routeB);
            }
        }

        private static void ProveReturnToSameRouteCreatesNewOccurrence(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset routeA = null;
            RouteAsset routeB = null;
            SessionPlayer player = null;
            GameObject authoringA = null;
            GameObject authoringB = null;
            try
            {
                authoringA = CreateRouteBinding(world.PrimaryA, p1, new Vector3(-20f, 0f, 2f), Quaternion.Euler(0f, 15f, 0f));
                authoringB = CreateRouteBinding(world.PrimaryB, p1, new Vector3(40f, 2f, -4f), Quaternion.Euler(0f, 200f, 0f));
                routeA = CreateRoute(
                    "qa.adr021.route.return-a",
                    "ADR021 Return A",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                routeB = CreateRoute(
                    "qa.adr021.route.return-b",
                    "ADR021 Return B",
                    world.PrimaryB,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case5", "qa.physical.case5");
                string physicalId = player.PhysicalInstanceId;
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 5 could not attach the participant.");
                Require(StartRoute(runtime, routeA).Started, "CASE 5 A#1 failed.");
                int occurrence1 = participant.LastContext.OccurrenceSequence;
                Require(SamePose(player.Target, GetAnchor(authoringA)), "CASE 5 A#1 was not applied.");
                Require(StartRoute(runtime, routeB).Started, "CASE 5 B#2 failed.");
                int occurrence2 = participant.LastContext.OccurrenceSequence;
                Require(occurrence2 > occurrence1, "CASE 5 B#2 reused A occurrence sequence.");
                Require(SamePose(player.Target, GetAnchor(authoringB)), "CASE 5 B#2 was not applied.");
                Require(StartRoute(runtime, routeA).Started, "CASE 5 A#3 failed.");
                int occurrence3 = participant.LastContext.OccurrenceSequence;
                Require(occurrence3 > occurrence2, "CASE 5 A#3 did not create a new Route occurrence sequence.");
                Require(player.PhysicalInstanceId == physicalId, "CASE 5 replaced the physical Player.");
                Require(ReferenceEquals(participant.LastContext.Route, routeA),
                    "CASE 5 A#3 current Route was not Route A.");
                Require(SamePose(player.Target, GetAnchor(authoringA)),
                    "CASE 5 A#3 did not re-apply Route A spatial entry. A RouteId-only gate would leave the Player on B.");
                LogIdentity("ReturnToSameRouteCreatesNewOccurrence", participant, player, routeA, occurrence3);
            }
            finally
            {
                CleanupCase(player, authoringA, authoringB, routeA, routeB);
            }
        }

        private static void ProveSameOccurrenceIsIdempotent(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                authoring = CreateRouteBinding(world.PrimaryA, p1, new Vector3(7f, 1f, 3f), Quaternion.Euler(0f, 80f, 0f));
                route = CreateRoute(
                    "qa.adr021.route.idempotent",
                    "ADR021 Idempotent",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case6", "qa.physical.case6");
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 6 could not attach the participant.");
                Require(StartRoute(runtime, route).Started, "CASE 6 Route start failed.");
                Require(SamePose(player.Target, GetAnchor(authoring)), "CASE 6 first apply missed the anchor.");
                Vector3 nudged = player.Target.position + new Vector3(0.5f, 0f, -0.25f);
                player.Target.position = nudged;
                Quaternion nudgedRotation = player.Target.rotation;
                int occurrence = participant.LastContext.OccurrenceSequence;
                Require(player.Binding.TryApplyBeforeActivation(player.Handle, out string repeatIssue),
                    "CASE 6 repeat delivery of the same occurrence failed. " + repeatIssue);
                Require(player.Target.position == nudged && SameRotation(player.Target.rotation, nudgedRotation),
                    "CASE 6 same Route occurrence mutated spatial pose a second time.");
                Require(participant.LastContext.OccurrenceSequence == occurrence,
                    "CASE 6 repeat delivery created a new Route occurrence.");
                LogIdentity("SameOccurrenceIsIdempotent", participant, player, route, occurrence);
            }
            finally
            {
                CleanupCase(player, authoring, route);
            }
        }

        private static void ProvePreserveCurrentPoseRequiresNoAnchor(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            try
            {
                route = CreateRoute(
                    "qa.adr021.route.preserve",
                    "ADR021 Preserve",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case7", "qa.physical.case7");
                Vector3 known = new Vector3(-3.5f, 6.25f, 1.5f);
                Quaternion knownRotation = Quaternion.Euler(10f, 20f, 30f);
                player.Target.SetPositionAndRotation(known, knownRotation);
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 7 could not attach the participant.");
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(result.Started, "CASE 7 PreserveCurrentPose failed. " + result.Message);
                Require(participant.LastApplySucceeded, "CASE 7 spatial entry did not succeed. " + participant.LastIssue);
                Require(participant.LastContext.Policy == RoutePlayerSpatialEntryPolicy.PreserveCurrentPose,
                    "CASE 7 did not evaluate PreserveCurrentPose as a positive Route policy.");
                Require(player.Target.position == known && SameRotation(player.Target.rotation, knownRotation),
                    "CASE 7 PreserveCurrentPose changed the known pose.");
                Require(
                    string.IsNullOrEmpty(participant.LastIssue) ||
                    !participant.LastIssue.Contains("exactly one binding", StringComparison.Ordinal),
                    "CASE 7 treated PreserveCurrentPose as a missing-binding fallback. " + participant.LastIssue);
                LogIdentity("PreserveCurrentPoseRequiresNoAnchor", participant, player, route, participant.LastContext.OccurrenceSequence);
            }
            finally
            {
                CleanupCase(player, null, route);
            }
        }

        private static void ProveApplyExplicitPlacementMissingBindingFails(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject otherSlot = null;
            try
            {
                Require(world.SlotP2 != null, "CASE 8 requires PlayerSlotProfile P2 so the authored binding cannot be consumed as a P1 fallback.");
                otherSlot = CreateRouteBinding(world.PrimaryA, world.SlotP2, new Vector3(9f, 0f, 9f), Quaternion.identity);
                route = CreateRoute(
                    "qa.adr021.route.missing",
                    "ADR021 Missing",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case8", "qa.physical.case8");
                Vector3 before = new Vector3(4f, 5f, 6f);
                player.Target.position = before;
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 8 could not attach the participant.");
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(!result.Started, "CASE 8 missing exact binding unexpectedly succeeded.");
                Require(!participant.LastApplySucceeded, "CASE 8 applied a fallback binding.");
                Require(
                    participant.LastIssue.Contains("exactly one binding", StringComparison.Ordinal) &&
                    participant.LastIssue.Contains("Found '0'", StringComparison.Ordinal),
                    "CASE 8 did not fail explicitly without fallback. " + participant.LastIssue);
                Require(player.Target.position == before, "CASE 8 moved the Player with no exact P1 binding.");
                LogIdentity("ApplyExplicitPlacementMissingBindingFails", participant, player, route, 0);
            }
            finally
            {
                CleanupCase(player, otherSlot, route);
            }
        }

        private static void ProveApplyExplicitPlacementDuplicateFails(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject first = null;
            GameObject second = null;
            try
            {
                first = CreateRouteBinding(world.PrimaryA, p1, Vector3.one, Quaternion.identity);
                second = CreateRouteBinding(world.PrimaryA, p1, Vector3.one * 2f, Quaternion.identity);
                route = CreateRoute(
                    "qa.adr021.route.duplicate",
                    "ADR021 Duplicate",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case9", "qa.physical.case9");
                Vector3 before = player.Target.position;
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 9 could not attach the participant.");
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(!result.Started, "CASE 9 duplicate exact bindings were not rejected.");
                Require(
                    !participant.LastApplySucceeded &&
                    participant.LastIssue.Contains("duplicate bindings", StringComparison.Ordinal),
                    "CASE 9 did not produce a duplicate deterministic failure. " + participant.LastIssue);
                Require(player.Target.position == before, "CASE 9 chose a first-found duplicate anchor.");
                LogIdentity("ApplyExplicitPlacementDuplicateFails", participant, player, route, 0);
            }
            finally
            {
                CleanupCase(player, first, second, route);
            }
        }

        private static void ProveRouteContentAnchorIsEligible(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            RouteContentProfileAsset profile = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                authoring = CreateRouteBinding(world.RouteContent, p1, new Vector3(14f, 2f, -6f), Quaternion.Euler(0f, 12f, 0f));
                profile = CreateRouteContentProfile("qa.adr021.route-content", world.RouteContent);
                route = CreateRoute(
                    "qa.adr021.route.content-anchor",
                    "ADR021 Route Content Anchor",
                    world.PrimaryA,
                    profile,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case10", "qa.physical.case10");
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 10 could not attach the participant.");
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(result.Started, "CASE 10 Route Content discovery failed. " + result.Message);
                Require(participant.LastApplySucceeded, "CASE 10 placement failed. " + participant.LastIssue);
                Require(ContainsScene(participant.LastContext.DiscoveryScope, world.RouteContent),
                    "CASE 10 discovery did not include Route Content.");
                Require(SamePose(player.Target, GetAnchor(authoring)),
                    "CASE 10 Player did not receive the Route Content anchor.");
                LogIdentity("RouteContentAnchorIsEligible", participant, player, route, participant.LastContext.OccurrenceSequence);
            }
            finally
            {
                CleanupCase(player, authoring, route, profile);
            }
        }

        private static void ProveActivityContentAnchorIsNotEligible(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                authoring = CreateRouteBinding(
                    world.ActivityContent,
                    p1,
                    new Vector3(50f, 0f, 50f),
                    Quaternion.identity);
                route = CreateRoute(
                    "qa.adr021.route.activity-ineligible",
                    "ADR021 Activity Ineligible",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case11", "qa.physical.case11");
                Vector3 before = player.Target.position;
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 11 could not attach the participant.");
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(!result.Started, "CASE 11 used an Activity Content binding for Route Spatial Entry.");
                Require(
                    participant.LastIssue.Contains("Found '0'", StringComparison.Ordinal),
                    "CASE 11 did not fail as missing exact Route binding. " + participant.LastIssue);
                Require(player.Target.position == before, "CASE 11 moved the Player from an ineligible Activity Content anchor.");
                LogIdentity("ActivityContentAnchorIsNotEligibleForRouteEntry", participant, player, route, 0);
            }
            finally
            {
                CleanupCase(player, authoring, route);
            }
        }

        private static void ProveUnrelatedLoadedSceneIgnored(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                authoring = CreateRouteBinding(
                    world.Unrelated,
                    p1,
                    new Vector3(80f, 0f, 0f),
                    Quaternion.identity);
                route = CreateRoute(
                    "qa.adr021.route.unrelated",
                    "ADR021 Unrelated",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case12", "qa.physical.case12");
                Vector3 before = player.Target.position;
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 12 could not attach the participant.");
                RouteLifecycleStartResult result = StartRoute(runtime, route);
                Require(!result.Started, "CASE 12 counted an unrelated loaded scene in Route discovery cardinality.");
                Require(
                    participant.LastIssue.Contains("Found '0'", StringComparison.Ordinal),
                    "CASE 12 did not ignore the unrelated scene. " + participant.LastIssue);
                Require(player.Target.position == before, "CASE 12 moved the Player from an unrelated scene binding.");
                LogIdentity("UnrelatedLoadedSceneIgnored", participant, player, route, 0);
            }
            finally
            {
                CleanupCase(player, authoring, route);
            }
        }

        private static void ProveSceneProvidedPreserveAuthoredPoseUsesRoutePolicy(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            try
            {
                route = CreateRoute(
                    "qa.adr021.route.scene-preserve",
                    "ADR021 Scene Preserve",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case13", "qa.physical.case13");
                Vector3 authored = new Vector3(-8f, 2f, 11f);
                Quaternion authoredRotation = Quaternion.Euler(12f, 34f, 56f);
                player.Target.SetPositionAndRotation(authored, authoredRotation);
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: false);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 13 could not attach the participant.");
                Require(StartRoute(runtime, route).Started, "CASE 13 Route start failed.");
                Require(participant.EnterCount == 1 && participant.ApplyInvocationCount == 0,
                    "CASE 13 applied Manager-Provisioned spatial entry instead of the Scene-Provided Route policy path.");
                Require(
                    participant.TryApplySceneProvided(out string issue),
                    "CASE 13 Scene-Provided Preserve failed. " + issue);
                Require(participant.LastContext.Policy == RoutePlayerSpatialEntryPolicy.PreserveCurrentPose,
                    "CASE 13 result did not derive from the Route policy.");
                Require(player.Target.position == authored && SameRotation(player.Target.rotation, authoredRotation),
                    "CASE 13 did not preserve the authored Scene-Provided pose.");
                LogIdentity("SceneProvidedPreserveAuthoredPoseUsesRoutePolicy", participant, player, route, participant.LastContext.OccurrenceSequence);
            }
            finally
            {
                CleanupCase(player, null, route);
            }
        }

        private static void ProveSceneProvidedApplyUsesRoutePolicy(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                authoring = CreateRouteBinding(world.PrimaryA, p1, new Vector3(-15f, 1.5f, 22f), Quaternion.Euler(0f, 270f, 0f));
                route = CreateRoute(
                    "qa.adr021.route.scene-apply",
                    "ADR021 Scene Apply",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case14", "qa.physical.case14");
                string physicalId = player.PhysicalInstanceId;
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: false);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 14 could not attach the participant.");
                Require(StartRoute(runtime, route).Started, "CASE 14 Route start failed.");
                Require(player.PhysicalInstanceId == physicalId, "CASE 14 replaced the Scene-Provided physical Actor.");
                Require(
                    participant.TryApplySceneProvided(out string issue),
                    "CASE 14 Scene-Provided Apply failed. " + issue);
                Require(SamePose(player.Target, GetAnchor(authoring)),
                    "CASE 14 Scene-Provided Actor did not receive Route placement.");
                LogIdentity("SceneProvidedApplyUsesRoutePolicy", participant, player, route, participant.LastContext.OccurrenceSequence);
            }
            finally
            {
                CleanupCase(player, authoring, route);
            }
        }

        private static void ProveActivityEnterWithoutRelocationDoesNotRepeatRouteEntry(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            ActivityAsset activity = null;
            ActivityContentProfileAsset activityProfile = null;
            SessionPlayer player = null;
            GameObject routeAuthoring = null;
            GameObject activityAuthoring = null;
            try
            {
                routeAuthoring = CreateRouteBinding(world.PrimaryA, p1, new Vector3(2f, 4f, 6f), Quaternion.Euler(0f, 40f, 0f));
                activityProfile = CreateActivityContentProfile("qa.adr021.activity-content", world.ActivityContent);
                activity = CreateActivity("qa.adr021.activity.no-relocation", "ADR021 No Relocation", activityProfile);
                activityAuthoring = CreateActivityRelocationAuthoring(
                    world.ActivityContent,
                    activity,
                    p1,
                    new Vector3(99f, 0f, 99f),
                    Quaternion.identity);
                route = CreateRoute(
                    "qa.adr021.route.then-activity",
                    "ADR021 Then Activity",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case15", "qa.physical.case15");
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 15 could not attach the participant.");
                Require(StartRoute(runtime, route).Started, "CASE 15 Route start failed.");
                Require(SamePose(player.Target, GetAnchor(routeAuthoring)), "CASE 15 Route placement was not applied.");
                int enterCount = participant.EnterCount;
                int occurrence = participant.LastContext.OccurrenceSequence;
                PoseSnapshot routePose = PoseSnapshot.Capture(player.Target);
                ActivityFlowStartResult activityResult = StartActivity(runtime, activity);
                Require(activityResult.Completed, "CASE 15 Activity enter failed. " + activityResult.Message);
                Require(participant.EnterCount == enterCount,
                    "CASE 15 Activity enter re-executed Route Spatial Entry.");
                Require(participant.LastContext.OccurrenceSequence == occurrence,
                    "CASE 15 Activity enter constituted a new Route occurrence.");
                Require(
                    player.Target.position == routePose.Position &&
                    SameRotation(player.Target.rotation, routePose.Rotation),
                    "CASE 15 Activity enter changed pose. Activity NoRelocation must not run as Route fallback.");
                Require(
                    !SamePose(player.Target, GetAnchor(activityAuthoring)),
                    "CASE 15 applied Activity relocation during Activity enter.");
                LogIdentity("ActivityEnterWithoutRelocationDoesNotRepeatRouteEntry", participant, player, route, occurrence);
            }
            finally
            {
                CleanupCase(player, routeAuthoring, activityAuthoring, route, activity, activityProfile);
            }
        }

        private static void ProveRouteExitClearsCurrentOccurrence(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            RouteAsset missing = null;
            SessionPlayer first = null;
            SessionPlayer late = null;
            GameObject authoring = null;
            try
            {
                authoring = CreateRouteBinding(world.PrimaryA, p1, new Vector3(5f, 1f, 1f), Quaternion.identity);
                route = CreateRoute(
                    "qa.adr021.route.exit",
                    "ADR021 Exit",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                missing = CreateRoute(
                    "qa.adr021.route.missing-scene",
                    "ADR021 Missing Scene",
                    "Assets/ImmersiveFrameworkQA/__DoesNotExistAdr021/Missing.unity",
                    "Missing",
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                first = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case16", "qa.physical.case16");
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(first, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 16 could not attach the participant.");
                Require(StartRoute(runtime, route).Started, "CASE 16 Route start failed.");
                Require(participant.LastContext.IsValid, "CASE 16 current occurrence was not published.");
                int exitBefore = participant.ExitCount;
                RouteLifecycleStartResult failed = StartRoute(runtime, missing);
                Require(!failed.Started, "CASE 16 expected Route composition failure after exit. " + failed.Message);
                Require(participant.ExitCount == exitBefore + 1,
                    "CASE 16 Route exit did not clear the published occurrence through the participant.");
                Require(!participant.LastContext.IsValid,
                    "CASE 16 participant still held a current Route occurrence after exit.");
                var lateParticipant = new SpatialEntryParticipant(null, admitImmediately: false);
                Require(
                    runtime.SetPlayerSpatialEntryParticipant(lateParticipant, out string lateIssue),
                    "CASE 16 late participant attach failed. " + lateIssue);
                Require(lateParticipant.EnterCount == 0 && !lateParticipant.LastContext.IsValid,
                    "CASE 16 late Player received a stale Route occurrence after exit.");
                late = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case16-late", "qa.physical.case16-late");
                lateParticipant.AttachPlayer(late);
                Require(
                    !lateParticipant.TryAdmitLate(out string admitIssue),
                    "CASE 16 late preparation after exit applied a stale Route scope. " + admitIssue);
                Require(
                    admitIssue.IndexOf("Route spatial-entry occurrence", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    admitIssue.IndexOf("valid Route occurrence", StringComparison.OrdinalIgnoreCase) >= 0,
                    "CASE 16 late failure did not identify missing current Route occurrence. " + admitIssue);
                LogIdentity("RouteExitClearsCurrentOccurrence", participant, first, route, 0);
            }
            finally
            {
                CleanupCase(first, late, authoring, route, missing);
            }
        }

        private static void ProveFailedRouteEntryIsNotMarkedSuccessfullyApplied(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                route = CreateRoute(
                    "qa.adr021.route.retry",
                    "ADR021 Retry",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case17", "qa.physical.case17");
                Vector3 before = player.Target.position;
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 17 could not attach the participant.");
                RouteLifecycleStartResult first = StartRoute(runtime, route);
                Require(!first.Started, "CASE 17 first attempt unexpectedly succeeded without a binding.");
                Require(!participant.LastApplySucceeded, "CASE 17 failure was treated as success.");
                Require(player.Target.position == before, "CASE 17 failure moved the Player.");
                authoring = CreateRouteBinding(world.PrimaryA, p1, new Vector3(8f, 3f, 1f), Quaternion.Euler(0f, 25f, 0f));
                RouteLifecycleStartResult second = StartRoute(runtime, route);
                Require(second.Started, "CASE 17 retry of the same Route sequence could not resolve after the binding was added. " + second.Message);
                Require(participant.LastApplySucceeded, "CASE 17 marked the failed attempt as idempotent completion. " + participant.LastIssue);
                Require(SamePose(player.Target, GetAnchor(authoring)),
                    "CASE 17 retry did not apply the corrected exact binding.");
                LogIdentity("FailedRouteEntryIsNotMarkedSuccessfullyApplied", participant, player, route, participant.LastContext.OccurrenceSequence);
            }
            finally
            {
                CleanupCase(player, authoring, route);
            }
        }

        private static void ProvePhysicalRepresentationReplacementWithinSameRouteOccurrence(
            World world,
            PlayerSlotProfile p1,
            ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteAsset route = null;
            SessionPlayer player = null;
            GameObject authoring = null;
            try
            {
                authoring = CreateRouteBinding(world.PrimaryA, p1, new Vector3(3f, 2f, 1f), Quaternion.Euler(0f, 60f, 0f));
                route = CreateRoute(
                    "qa.adr021.route.representation",
                    "ADR021 Representation",
                    world.PrimaryA,
                    null,
                    null,
                    RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
                player = SessionPlayer.Create(world.Session, p1, actorProfile, "qa.adr021.actor.case18", "qa.physical.case18-a");
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately: true);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out _), "CASE 18 could not attach the participant.");
                Require(StartRoute(runtime, route).Started, "CASE 18 Route start failed.");
                Require(SamePose(player.Target, GetAnchor(authoring)), "CASE 18 first representation was not placed.");
                player.Target.position = player.Target.position + Vector3.up;
                PlayerActorMaterializationHandle replacement = player.CreateReplacementHandle("qa.physical.case18-b");
                Require(
                    player.Binding.TryApplyBeforeActivation(replacement, out string issue),
                    "CASE 18 replacement representation was not processed. " + issue);
                Require(SamePose(player.Target, GetAnchor(authoring)),
                    "CASE 18 gate ignored physical representation identity inside the same Route occurrence.");
                LogIdentity("PhysicalRepresentationReplacementWithinSameRouteOccurrence", participant, player, route, participant.LastContext.OccurrenceSequence);
            }
            finally
            {
                CleanupCase(player, authoring, route);
            }
        }

        private static RouteLifecycleRuntime CreateRuntime()
        {
            return new RouteLifecycleRuntime(
                new RuntimeContentRuntime(),
                new QaAdr21RoutePort(),
                new QaAdr21ActivityPort(),
                new QaAdr21RouteCycleResetPort(),
                new QaAdr21ActivityCycleResetPort(),
                new QaAdr21ActivityRestartPort());
        }

        private static RouteLifecycleStartResult StartRoute(RouteLifecycleRuntime runtime, RouteAsset route)
        {
            Task<RouteLifecycleStartResult> task = runtime.StartRouteAsync(
                route,
                "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                "qa-route-spatial-entry");
            return Complete(task, "RouteLifecycleRuntime.StartRouteAsync");
        }

        private static ActivityFlowStartResult StartActivity(RouteLifecycleRuntime runtime, ActivityAsset activity)
        {
            Task<ActivityFlowStartResult> task = runtime.StartActivityAsync(
                activity,
                "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                "qa-activity-enter");
            return Complete(task, "RouteLifecycleRuntime.StartActivityAsync");
        }

        private static T Complete<T>(Task<T> task, string operation)
        {
            Require(task != null, operation + " returned no Task.");
            Require(
                task.IsCompleted,
                operation + " did not complete synchronously. Edit Mode Route Spatial Entry QA requires already-loaded scenes and no NextFrame waits.");
            if (task.IsFaulted)
            {
                Exception inner = task.Exception != null && task.Exception.InnerException != null
                    ? task.Exception.InnerException
                    : task.Exception;
                throw new InvalidOperationException(operation + " faulted. " + inner.Message, inner);
            }

            return task.GetAwaiter().GetResult();
        }

        private static RouteAsset CreateRoute(
            string routeId,
            string routeName,
            Scene primary,
            RouteContentProfileAsset content,
            ActivityAsset startup,
            RoutePlayerSpatialEntryPolicy policy)
        {
            return CreateRoute(routeId, routeName, primary.path, primary.name, content, startup, policy);
        }

        private static RouteAsset CreateRoute(
            string routeId,
            string routeName,
            string primaryPath,
            string primaryName,
            RouteContentProfileAsset content,
            ActivityAsset startup,
            RoutePlayerSpatialEntryPolicy policy)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = routeName;
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = routeId;
            serialized.FindProperty("routeName").stringValue = routeName;
            serialized.FindProperty("primaryScenePath").stringValue = primaryPath;
            serialized.FindProperty("primarySceneName").stringValue = primaryName;
            serialized.FindProperty("routeContentProfile").objectReferenceValue = content;
            serialized.FindProperty("startupActivity").objectReferenceValue = startup;
            serialized.FindProperty("playerSpatialEntryPolicy").intValue = (int)policy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(route.HasValidRouteId && route.HasPrimaryScene && route.HasDefinedPlayerSpatialEntryPolicy,
                "QA RouteAsset was not authored with a valid RouteId, Primary Scene and policy.");
            return route;
        }

        private static ActivityAsset CreateActivity(
            string activityId,
            string activityName,
            ActivityContentProfileAsset content)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = activityName;
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = activityId;
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("playerParticipationProjectionMode").intValue =
                (int)ActivityParticipationProjectionMode.NoSlots;
            serialized.FindProperty("playerParticipationZeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Allowed;
            serialized.FindProperty("playerParticipationRequirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.None;
            serialized.FindProperty("activityContentProfile").objectReferenceValue = content;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(activity.HasValidActivityId, "QA ActivityAsset was not authored with a valid ActivityId.");
            return activity;
        }

        private static RouteContentProfileAsset CreateRouteContentProfile(string profileId, Scene scene)
        {
            RouteContentProfileAsset profile = ScriptableObject.CreateInstance<RouteContentProfileAsset>();
            profile.name = profileId;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = profileId;
            SerializedProperty scenes = serialized.FindProperty("additionalScenes");
            Require(scenes != null, "RouteContentProfileAsset.additionalScenes was not found.");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("contentId").stringValue = profileId + ".scene";
            entry.FindPropertyRelative("scenePath").stringValue = scene.path;
            entry.FindPropertyRelative("sceneName").stringValue = scene.name;
            entry.FindPropertyRelative("requiredness").intValue = (int)FrameworkContentRequiredness.Required;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static ActivityContentProfileAsset CreateActivityContentProfile(string profileId, Scene scene)
        {
            ActivityContentProfileAsset profile = ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
            profile.name = profileId;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = profileId;
            SerializedProperty scenes = serialized.FindProperty("scenes");
            Require(scenes != null, "ActivityContentProfileAsset.scenes was not found.");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("contentId").stringValue = profileId + ".scene";
            entry.FindPropertyRelative("scenePath").stringValue = scene.path;
            entry.FindPropertyRelative("sceneName").stringValue = scene.name;
            entry.FindPropertyRelative("requiredness").intValue = (int)FrameworkContentRequiredness.Required;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static GameObject CreateRouteBinding(
            Scene scene,
            PlayerSlotProfile slot,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject root = CreateInScene(scene, "RoutePlayerSpatialEntry");
            RoutePlayerSpatialEntryAuthoring authoring = root.AddComponent<RoutePlayerSpatialEntryAuthoring>();
            GameObject anchor = CreateInScene(scene, "RouteAnchor");
            anchor.transform.SetParent(root.transform, true);
            anchor.transform.SetPositionAndRotation(position, rotation);
            SetRouteBindings(authoring, new BindingSpec(slot, anchor.transform));
            return root;
        }

        private static GameObject CreateActivityRelocationAuthoring(
            Scene scene,
            ActivityAsset activity,
            PlayerSlotProfile slot,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject root = CreateInScene(scene, "ActivityPlayerRelocation");
            ActivityPlayerRelocationAuthoring authoring = root.AddComponent<ActivityPlayerRelocationAuthoring>();
            GameObject anchor = CreateInScene(scene, "ActivityRelocationAnchor");
            anchor.transform.SetParent(root.transform, true);
            anchor.transform.SetPositionAndRotation(position, rotation);
            var serialized = new SerializedObject(authoring);
            SerializedProperty bindings = serialized.FindProperty("bindings");
            Require(bindings != null, "ActivityPlayerRelocationAuthoring.bindings was not found.");
            bindings.arraySize = 1;
            SerializedProperty element = bindings.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("activity").objectReferenceValue = activity;
            element.FindPropertyRelative("playerSlotProfile").objectReferenceValue = slot;
            element.FindPropertyRelative("relocationAnchor").objectReferenceValue = anchor.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject CreateInScene(Scene scene, string name)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return gameObject;
        }

        private static Transform GetAnchor(GameObject authoringRoot)
        {
            Require(
                authoringRoot != null && authoringRoot.transform.childCount > 0,
                "Placement authoring root has no anchor child.");
            return authoringRoot.transform.GetChild(0);
        }

        private readonly struct BindingSpec
        {
            internal BindingSpec(PlayerSlotProfile slotProfile, Transform anchor)
            {
                SlotProfile = slotProfile;
                Anchor = anchor;
            }

            internal PlayerSlotProfile SlotProfile { get; }
            internal Transform Anchor { get; }
        }

        private static void SetRouteBindings(
            RoutePlayerSpatialEntryAuthoring authoring,
            params BindingSpec[] specs)
        {
            var serialized = new SerializedObject(authoring);
            SerializedProperty bindings = serialized.FindProperty("bindings");
            Require(bindings != null, "Serialized Route spatial-entry field 'bindings' was not found.");
            bindings.arraySize = specs?.Length ?? 0;
            for (int index = 0; index < bindings.arraySize; index++)
            {
                SerializedProperty element = bindings.GetArrayElementAtIndex(index);
                SerializedProperty slot = element.FindPropertyRelative("playerSlotProfile");
                SerializedProperty anchor = element.FindPropertyRelative("placementAnchor");
                Require(slot != null && anchor != null,
                    $"Serialized Route spatial-entry binding fields were not found at index '{index}'.");
                slot.objectReferenceValue = specs[index].SlotProfile;
                anchor.objectReferenceValue = specs[index].Anchor;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
        }

        private static bool ContainsScene(RouteContentDiscoveryScope scope, Scene scene)
        {
            for (int index = 0; index < scope.RouteOwnedScenes.Count; index++)
            {
                if (string.Equals(scope.RouteOwnedScenes[index].ScenePath, scene.path, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool SamePose(Transform left, Transform right) =>
            Vector3.Distance(left.position, right.position) <= 0.0001f &&
            SameRotation(left.rotation, right.rotation);

        private static bool SamePose(Vector3 left, Vector3 right) =>
            Vector3.Distance(left, right) <= 0.0001f;

        private static bool SameRotation(Quaternion left, Quaternion right) =>
            Quaternion.Angle(left, right) <= 0.01f;

        private static void CleanupCase(params object[] objects)
        {
            if (objects == null)
            {
                return;
            }

            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] is SessionPlayer player)
                {
                    player.Dispose();
                    continue;
                }

                if (objects[index] is UnityEngine.Object unityObject && unityObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(unityObject);
                }
            }
        }

        private static void LogIdentity(
            string caseName,
            SpatialEntryParticipant participant,
            SessionPlayer player,
            RouteAsset route,
            int occurrence)
        {
            string routeId = route != null && route.HasValidRouteId ? route.RouteId.StableText : string.Empty;
            string slot = player != null ? player.SlotId.StableText : string.Empty;
            string actor = player != null ? player.ActorId.StableText : string.Empty;
            string physical = player != null ? player.RepresentationIdentity : string.Empty;
            string policy = participant != null && participant.LastContext.IsValid
                ? participant.LastContext.Policy.ToString()
                : (route != null ? route.PlayerSpatialEntryPolicy.ToString() : string.Empty);
            Debug.Log(
                $"{Prefix} identity case='{caseName}' routeId='{routeId}' occurrence='{occurrence}' " +
                $"slot='{slot}' actor='{actor}' physical='{physical}' physicalInstance='{(player != null ? player.PhysicalInstanceId : string.Empty)}' " +
                $"policy='{policy}' result='{(participant != null && participant.LastApplySucceeded ? "success" : "failure")}'.");
        }

        private static void RunCase(
            string caseName,
            Action proof,
            List<string> failures,
            ref int passed)
        {
            try
            {
                proof();
                passed++;
                Debug.Log($"{Prefix} case='{caseName}' status='PASS'.");
            }
            catch (Exception exception)
            {
                string failure = $"{caseName}: {exception.GetType().Name}: {exception.Message}";
                failures.Add(failure);
                Debug.LogError($"{Prefix} case='{caseName}' status='FAIL' error='{Escape(exception.Message)}'.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "''").Replace("\r", " ").Replace("\n", " ");

        private readonly struct PoseSnapshot
        {
            internal PoseSnapshot(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }

            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }

            internal static PoseSnapshot Capture(Transform target) =>
                new PoseSnapshot(target.position, target.rotation);
        }

        private sealed class World : IDisposable
        {
            private readonly string temporaryRoot;
            private bool disposed;

            private World(
                string temporaryRoot,
                Scene session,
                Scene primaryA,
                Scene primaryB,
                Scene routeContent,
                Scene activityContent,
                Scene unrelated,
                PlayerSlotProfile slotP2)
            {
                this.temporaryRoot = temporaryRoot;
                Session = session;
                PrimaryA = primaryA;
                PrimaryB = primaryB;
                RouteContent = routeContent;
                ActivityContent = activityContent;
                Unrelated = unrelated;
                SlotP2 = slotP2;
            }

            internal Scene Session { get; }
            internal Scene PrimaryA { get; }
            internal Scene PrimaryB { get; }
            internal Scene RouteContent { get; }
            internal Scene ActivityContent { get; }
            internal Scene Unrelated { get; }
            internal PlayerSlotProfile SlotP2 { get; }

            internal static World Create()
            {
                string temporaryId = Guid.NewGuid().ToString("N");
                string folderName = "__Adr021RouteSpatialEntry_" + temporaryId;
                string temporaryRoot = "Assets/ImmersiveFrameworkQA/" + folderName;
                Require(
                    AssetDatabase.CreateFolder("Assets/ImmersiveFrameworkQA", folderName).Length > 0,
                    "ADR-021 Route Spatial Entry could not create its temporary Editor scene root.");
                Scene session = CreateScene(temporaryRoot, "Session");
                Scene primaryA = CreateScene(temporaryRoot, "PrimaryA");
                Scene primaryB = CreateScene(temporaryRoot, "PrimaryB");
                Scene routeContent = CreateScene(temporaryRoot, "RouteContent");
                Scene activityContent = CreateScene(temporaryRoot, "ActivityContent");
                Scene unrelated = CreateScene(temporaryRoot, "Unrelated");
                PlayerSlotProfile slotP2 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP2Path);
                return new World(temporaryRoot, session, primaryA, primaryB, routeContent, activityContent, unrelated, slotP2);
            }

            internal void EnsureLoaded()
            {
                EnsureScene(Session);
                EnsureScene(PrimaryA);
                EnsureScene(PrimaryB);
                EnsureScene(RouteContent);
                EnsureScene(ActivityContent);
                EnsureScene(Unrelated);
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                Close(Unrelated);
                Close(ActivityContent);
                Close(RouteContent);
                Close(PrimaryB);
                Close(PrimaryA);
                Close(Session);
                if (!string.IsNullOrEmpty(temporaryRoot) && AssetDatabase.IsValidFolder(temporaryRoot))
                {
                    AssetDatabase.DeleteAsset(temporaryRoot);
                    AssetDatabase.Refresh();
                }
            }

            private static Scene CreateScene(string root, string name)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                string path = root + "/" + name + ".unity";
                Require(EditorSceneManager.SaveScene(scene, path), "Could not save temporary scene '" + path + "'.");
                return scene;
            }

            private static void EnsureScene(Scene scene)
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    return;
                }

                Require(
                    !string.IsNullOrEmpty(scene.path),
                    "Temporary Route Spatial Entry scene lost its path.");
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Additive);
            }

            private static void Close(Scene scene)
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private sealed class SessionPlayer : IDisposable
        {
            private SessionPlayer(
                GameObject root,
                LocalPlayerHostAuthoring host,
                PlayerActorDeclaration actor,
                RoutePlayerSpatialEntryRuntimeBinding binding,
                PlayerActorMaterializationHandle handle,
                PlayerSlotId slotId,
                ActorId actorId,
                string representationIdentity)
            {
                Root = root;
                Host = host;
                Actor = actor;
                Binding = binding;
                Handle = handle;
                SlotId = slotId;
                ActorId = actorId;
                RepresentationIdentity = representationIdentity;
            }

            internal GameObject Root { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorDeclaration Actor { get; }
            internal RoutePlayerSpatialEntryRuntimeBinding Binding { get; }
            internal PlayerActorMaterializationHandle Handle { get; private set; }
            internal PlayerSlotId SlotId { get; }
            internal ActorId ActorId { get; }
            internal string RepresentationIdentity { get; private set; }
            internal Transform Target => Actor.transform;
            internal string PhysicalInstanceId => Actor.gameObject.GetEntityId().ToString();
            internal SceneLocalPlayerAdmissionAuthoring SceneAdmission { get; private set; }

            internal static SessionPlayer Create(
                Scene session,
                PlayerSlotProfile slot,
                ActorProfile actorProfile,
                string actorIdText,
                string representationId)
            {
                GameObject root = CreateInScene(session, "QA_ADR021_SessionPlayer");
                PlayerInput playerInput = root.AddComponent<PlayerInput>();
                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
                if (actions != null)
                {
                    playerInput.actions = actions;
                }

                LocalPlayerHostAuthoring host = root.AddComponent<LocalPlayerHostAuthoring>();
                GameObject mount = new GameObject("ActorMount");
                SceneManager.MoveGameObjectToScene(mount, session);
                mount.transform.SetParent(root.transform, false);
                var hostSerialized = new SerializedObject(host);
                hostSerialized.FindProperty("playerInput").objectReferenceValue = playerInput;
                hostSerialized.FindProperty("actorMount").objectReferenceValue = mount.transform;
                hostSerialized.ApplyModifiedPropertiesWithoutUndo();

                GameObject actorObject = new GameObject("LogicalActor");
                SceneManager.MoveGameObjectToScene(actorObject, session);
                actorObject.transform.SetParent(mount.transform, false);
                PlayerActorDeclaration actor = actorObject.AddComponent<PlayerActorDeclaration>();
                var actorSerialized = new SerializedObject(actor);
                actorSerialized.FindProperty("actorId").stringValue = actorIdText;
                actorSerialized.ApplyModifiedPropertiesWithoutUndo();

                SceneLocalPlayerAdmissionAuthoring admission = root.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
                var admissionSerialized = new SerializedObject(admission);
                admissionSerialized.FindProperty("playerSlotProfile").objectReferenceValue = slot;
                admissionSerialized.FindProperty("actorProfile").objectReferenceValue = actorProfile;
                admissionSerialized.FindProperty("sceneLogicalPlayerActor").objectReferenceValue = actor;
                admissionSerialized.ApplyModifiedPropertiesWithoutUndo();

                RoutePlayerSpatialEntryRuntimeBinding binding =
                    root.AddComponent<RoutePlayerSpatialEntryRuntimeBinding>();
                Require(slot.TryGetPlayerSlotId(out PlayerSlotId slotId, out string slotIssue),
                    "QA PlayerSlotProfile did not resolve a SlotId. " + slotIssue);
                PlayerActorMaterializationHandle handle = CreateHandle(
                    slot,
                    slotId,
                    actorProfile,
                    host,
                    playerInput,
                    actor,
                    actorObject,
                    ActorId.From(actorIdText),
                    representationId);
                return new SessionPlayer(
                    root,
                    host,
                    actor,
                    binding,
                    handle,
                    slotId,
                    ActorId.From(actorIdText),
                    handle.Request.RuntimeContentIdentity.StableText)
                {
                    SceneAdmission = admission
                };
            }

            internal PlayerActorMaterializationHandle CreateReplacementHandle(string representationId)
            {
                PlayerActorMaterializationHandle replacement = CreateHandle(
                    Handle.Request.Slot.Profile,
                    SlotId,
                    Handle.Request.ActorProfile,
                    Host,
                    Host.PlayerInput,
                    Actor,
                    Actor.gameObject,
                    ActorId,
                    representationId);
                RepresentationIdentity = replacement.Request.RuntimeContentIdentity.StableText;
                return replacement;
            }

            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                }
            }

            private static PlayerActorMaterializationHandle CreateHandle(
                PlayerSlotProfile slotProfile,
                PlayerSlotId slotId,
                ActorProfile actorProfile,
                LocalPlayerHostAuthoring host,
                PlayerInput playerInput,
                PlayerActorDeclaration actor,
                GameObject logicalActorHost,
                ActorId actorId,
                string representationId)
            {
                RuntimeContentOwner owner = RuntimeContentOwner.Session("qa.adr021.session", "ADR021 Session");
                var scope = new RuntimeScopeContext(owner, "QA_ADR021_ROUTE_SPATIAL_ENTRY", "session-player");
                var snapshot = new PlayerSlotRuntimeSnapshot(
                    0,
                    slotProfile,
                    slotId,
                    PlayerSlotAllocationState.Joined,
                    new PlayerSlotReservationToken("qa.adr021.session", 1, slotId, 1),
                    1,
                    "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                    "joined",
                    actorProfile,
                    1,
                    "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                    "selected");
                Require(
                    PlayerActorMaterializationOperationId.TryCreate(
                        "qa.adr021.session",
                        owner,
                        slotId,
                        1,
                        out PlayerActorMaterializationOperationId operationId,
                        out string operationIssue),
                    "Could not create a Player Actor materialization operation id. " + operationIssue);
                RuntimeContentId contentId = RuntimeContentId.From(representationId);
                var request = new PlayerActorMaterializationRequest(
                    operationId,
                    "qa.adr021.session",
                    scope,
                    snapshot,
                    actorProfile,
                    host,
                    actorId,
                    contentId,
                    1,
                    "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                    "materialize");
                Require(request.IsValid, "QA PlayerActorMaterializationRequest is invalid. " + request.ToDiagnosticString());
                var resource = new RuntimeMaterializationResource(
                    "logical-actor",
                    representationId,
                    "ADR021 Logical Actor",
                    string.Empty);
                var cancellation = new RuntimeScopeCancellationToken(
                    owner,
                    1,
                    RuntimeScopeTransitionState.Active,
                    "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                    "active");
                var runtimeRequest = new RuntimeMaterializationRequest(
                    scope,
                    contentId,
                    resource,
                    cancellation,
                    "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                    "materialize");
                RuntimeContentHandle contentHandle = RuntimeContentHandle.Materialized(
                    runtimeRequest.Identity,
                    "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                    "materialize");
                Require(contentHandle.IsMaterialized, "QA RuntimeContentHandle is not materialized.");
                return new PlayerActorMaterializationHandle(
                    request,
                    runtimeRequest,
                    contentHandle,
                    host,
                    playerInput,
                    actor,
                    logicalActorHost,
                    "QA_ADR021_ROUTE_SPATIAL_ENTRY",
                    "materialize");
            }
        }

        /// <summary>
        /// Host-scoped participant registered on RouteLifecycleRuntime. It uses the same
        /// IRoutePlayerSpatialEntryLifecycleParticipant contract and the same
        /// RoutePlayerSpatialEntryRuntimeBinding gate as
        /// PlayerActorPreparationRuntimeHostModule.
        /// </summary>
        private sealed class SpatialEntryParticipant : IRoutePlayerSpatialEntryLifecycleParticipant
        {
            private SessionPlayer player;
            private bool admitted;

            internal SpatialEntryParticipant(SessionPlayer sessionPlayer, bool admitImmediately)
            {
                player = sessionPlayer;
                admitted = admitImmediately && sessionPlayer != null;
            }

            internal int EnterCount { get; private set; }
            internal int ExitCount { get; private set; }
            internal int ApplyInvocationCount { get; private set; }
            internal bool LastApplySucceeded { get; private set; }
            internal string LastIssue { get; private set; } = string.Empty;
            internal RoutePlayerSpatialEntryContext LastContext { get; private set; }

            internal void AttachPlayer(SessionPlayer sessionPlayer)
            {
                player = sessionPlayer;
            }

            public bool TryEnterRouteSpatialEntry(RoutePlayerSpatialEntryContext context, out string issue)
            {
                EnterCount++;
                LastContext = context;
                issue = string.Empty;
                if (player != null)
                {
                    player.Binding.Configure(context);
                }

                if (!admitted || player == null)
                {
                    LastApplySucceeded = true;
                    LastIssue = string.Empty;
                    return true;
                }

                return ApplyCurrent(out issue);
            }

            public void ExitRouteSpatialEntry(RoutePlayerSpatialEntryContext context)
            {
                ExitCount++;
                if (LastContext.Matches(context))
                {
                    LastContext = default;
                }
            }

            internal bool TryAdmitLate(out string issue)
            {
                Require(player != null, "Late admission requires a Session Player.");
                admitted = true;
                if (!LastContext.IsValid)
                {
                    issue = "Session Player cannot activate without current Route spatial-entry occurrence evidence.";
                    LastIssue = issue;
                    LastApplySucceeded = false;
                    return false;
                }

                player.Binding.Configure(LastContext);
                return ApplyCurrent(out issue);
            }

            internal bool TryApplySceneProvided(out string issue)
            {
                issue = string.Empty;
                Require(player != null && player.SceneAdmission != null,
                    "Scene-Provided apply requires SceneLocalPlayerAdmissionAuthoring.");
                if (!LastContext.IsValid ||
                    player.SceneAdmission.SceneLogicalPlayerActor == null ||
                    !player.SceneAdmission.TryGetPlayerSlotId(out PlayerSlotId playerSlotId, out issue))
                {
                    if (string.IsNullOrEmpty(issue))
                    {
                        issue =
                            "Scene-Provided spatial entry requires current Route occurrence context and complete authoring.";
                    }

                    LastApplySucceeded = false;
                    LastIssue = issue;
                    return false;
                }

                ApplyInvocationCount++;
                bool applied = RoutePlayerSpatialEntryRuntime.TryApply(
                    LastContext,
                    playerSlotId,
                    player.SceneAdmission.SceneLogicalPlayerActor.ActorId,
                    "scene-provided:" + playerSlotId.StableText + ":" +
                    player.SceneAdmission.SceneLogicalPlayerActor.ActorId.StableText,
                    player.SceneAdmission.SceneLogicalPlayerActor.transform,
                    out issue);
                LastApplySucceeded = applied;
                LastIssue = issue ?? string.Empty;
                return applied;
            }

            private bool ApplyCurrent(out string issue)
            {
                ApplyInvocationCount++;
                bool applied = player.Binding.TryApplyBeforeActivation(player.Handle, out issue);
                LastApplySucceeded = applied;
                LastIssue = issue ?? string.Empty;
                return applied;
            }
        }

        private sealed class QaAdr21RoutePort : IRouteRuntimePort
        {
            public Task<FrameworkRouteRequestResult> RequestRouteAsync(
                RouteAsset targetRoute,
                string source,
                string reason)
            {
                return Task.FromResult(
                    FrameworkRouteRequestResult.SucceededWith(targetRoute, source, reason, default));
            }
        }

        private sealed class QaAdr21ActivityPort : IActivityRuntimePort
        {
            public Task<FrameworkActivityRequestResult> RequestActivityAsync(
                ActivityAsset targetActivity,
                string source,
                string reason)
            {
                return Task.FromResult(
                    FrameworkActivityRequestResult.SucceededWith(targetActivity, source, reason, default));
            }

            public Task<FrameworkActivityRequestResult> ClearActivityAsync(string source, string reason)
            {
                return Task.FromResult(
                    FrameworkActivityRequestResult.SucceededWith(null, source, reason, default));
            }
        }

        private sealed class QaAdr21RouteCycleResetPort : IRouteCycleResetRuntimePort
        {
            public Task<CycleResetResult> RequestRouteCycleResetAsync(string source, string reason)
            {
                return Task.FromResult(default(CycleResetResult));
            }
        }

        private sealed class QaAdr21ActivityCycleResetPort : IActivityCycleResetRuntimePort
        {
            public Task<CycleResetResult> RequestActivityCycleResetAsync(string source, string reason)
            {
                return Task.FromResult(default(CycleResetResult));
            }
        }

        private sealed class QaAdr21ActivityRestartPort : IActivityRestartRuntimePort
        {
            public Task<ActivityRestartRuntimeResult> RequestActivityRestartAsync(
                ActivityAsset targetActivity,
                bool useCurrentActivityWhenTargetMissing,
                bool requireTargetActivityIsCurrent,
                ResetSelectionConfig resetSelection,
                string source,
                string reason)
            {
                return Task.FromResult(ActivityRestartRuntimeResult.From(null));
            }
        }
    }
}

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
    /// IF-ADR-021 Model B — Activity explicit Player relocation.
    /// Uses RouteLifecycleRuntime / ActivityFlow composition plus
    /// ActivityPlayerRelocationRuntime. Discovery and apply are the real owners.
    /// GameFlow.TryConfigureRelocationContext is the production publisher of
    /// ActivityTransitionPreparationContext; Edit Mode reconstructs that same
    /// context from the current ActivityFlow occurrence after StartActivity.
    /// </summary>
    public static class QaAdr21ActivityPlayerRelocationRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run ADR-021 Activity Relocation QA";
        private const string Prefix = "[QA_ADR021_ACTIVITY_RELOCATION]";
        private const int ExpectedCaseCount = 23;
        private const string SlotP1Path =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/SlotsProfiles/PlayerSlotProfileP1.asset";
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
                error = "ADR-021 Activity Relocation QA must run in Edit Mode.";
                Debug.LogError($"{Prefix} status='Failed' cases='0/{ExpectedCaseCount}' error='{Escape(error)}'.");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                error = "ADR-021 Activity Relocation QA was cancelled because the current Editor scenes were not saved.";
                Debug.LogError($"{Prefix} status='Failed' cases='0/{ExpectedCaseCount}' error='{Escape(error)}'.");
                return false;
            }

            var failures = new List<string>();
            int passed = 0;
            PlayerSlotProfile p1 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP1Path);
            ActorProfile actorProfile = AssetDatabase.LoadAssetAtPath<ActorProfile>(ActorProfilePath);
            SceneSetup[] initialSetup = EditorSceneManager.GetSceneManagerSetup();
            World world = null;
            try
            {
                Require(p1 != null, $"Missing QA PlayerSlotProfile at '{SlotP1Path}'.");
                Require(actorProfile != null, $"Missing QA ActorProfile at '{ActorProfilePath}'.");
                world = World.Create();
                RunCase("NoRelocationPreservesPose",
                    () => ProveNoRelocationPreservesPose(world, p1, actorProfile), failures, ref passed);
                RunCase("PrimarySceneRelocation",
                    () => ProvePrimarySceneRelocation(world, p1, actorProfile), failures, ref passed);
                RunCase("RouteContentRelocation",
                    () => ProveRouteContentRelocation(world, p1, actorProfile), failures, ref passed);
                RunCase("ActivityContentRelocation",
                    () => ProveActivityContentRelocation(world, p1, actorProfile), failures, ref passed);
                RunCase("NullActivityContentProfileDoesNotBlockRelocation",
                    () => ProveNullActivityContentProfileDoesNotBlockRelocation(world, p1, actorProfile), failures, ref passed);
                RunCase("SharedPrimaryUsesActivityIdentity",
                    () => ProveSharedPrimaryUsesActivityIdentity(world, p1, actorProfile), failures, ref passed);
                RunCase("MissingBindingFails",
                    () => ProveMissingBindingFails(world, p1, actorProfile), failures, ref passed);
                RunCase("DuplicateBindingFails",
                    () => ProveDuplicateBindingFails(world, p1, actorProfile), failures, ref passed);
                RunCase("UnrelatedSceneIgnored",
                    () => ProveUnrelatedSceneIgnored(world, p1, actorProfile), failures, ref passed);
                RunCase("PersistentContentIgnored",
                    () => ProvePersistentContentIgnored(world, p1, actorProfile), failures, ref passed);
                RunCase("OtherActivityContentIgnored",
                    () => ProveOtherActivityContentIgnored(world, p1, actorProfile), failures, ref passed);
                RunCase("ActivityChangeWithoutRelocationPreservesPose",
                    () => ProveActivityChangeWithoutRelocationPreservesPose(world, p1, actorProfile), failures, ref passed);
                RunCase("ActivityChangeWithRelocationMovesSamePlayer",
                    () => ProveActivityChangeWithRelocationMovesSamePlayer(world, p1, actorProfile), failures, ref passed);
                RunCase("ReturnToSameActivityCreatesNewOccurrence",
                    () => ProveReturnToSameActivityCreatesNewOccurrence(world, p1, actorProfile), failures, ref passed);
                RunCase("SameOccurrenceIsIdempotent",
                    () => ProveSameOccurrenceIsIdempotent(world, p1, actorProfile), failures, ref passed);
                RunCase("FailureDoesNotMarkSuccess",
                    () => ProveFailureDoesNotMarkSuccess(world, p1, actorProfile), failures, ref passed);
                RunCase("RelocationEvidenceOnlyWhenConfigured",
                    () => ProveRelocationEvidenceOnlyWhenConfigured(world, p1, actorProfile), failures, ref passed);
                RunCase("RelocationFailureDoesNotSatisfyEvidence",
                    () => ProveRelocationFailureDoesNotSatisfyEvidence(world, p1, actorProfile), failures, ref passed);
                RunCase("RelocationDoesNotReplayRouteEntry",
                    () => ProveRelocationDoesNotReplayRouteEntry(world, p1, actorProfile), failures, ref passed);
                RunCase("SceneProvidedCanBeRelocated",
                    () => ProveSceneProvidedCanBeRelocated(world, p1, actorProfile), failures, ref passed);
                RunCase("ManagerProvisionedCanBeRelocated",
                    () => ProveManagerProvisionedCanBeRelocated(world, p1, actorProfile), failures, ref passed);
                RunCase("RouteExitOrActivityClearDoesNotLeaveStaleRelocationContext",
                    () => ProveRouteExitOrActivityClearDoesNotLeaveStaleRelocationContext(world, p1, actorProfile), failures, ref passed);
                RunCase("PhysicalRepresentationReplacementWithinSameActivityOccurrence",
                    () => ProvePhysicalRepresentationReplacement(world, p1, actorProfile), failures, ref passed);
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
            Debug.Log($"{Prefix} status='Passed' verdict='ADR-021 MODEL B ACTIVITY RELOCATION VERIFIED' cases='{passed}/{ExpectedCaseCount}'.");
            return true;
        }

        private static void ProveNoRelocationPreservesPose(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case1", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.none", "No Relocation", null, ActivityPlayerRelocationPolicy.NoRelocation);
            GameObject unusedBinding = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(40f, 0f, 0f), Quaternion.identity);
            try
            {
                session.StartRoutePreserve();
                Vector3 known = new Vector3(-2f, 3f, 1f);
                Quaternion knownRotation = Quaternion.Euler(5f, 15f, 0f);
                session.Player.Target.SetPositionAndRotation(known, knownRotation);
                int routeEnters = session.Participant.EnterCount;
                int routeOccurrence = session.Participant.LastContext.OccurrenceSequence;
                Require(session.StartActivity(activity).Completed, "CASE 1 Activity enter failed.");
                Require(activity.PlayerRelocationPolicy == ActivityPlayerRelocationPolicy.NoRelocation,
                    "CASE 1 Activity policy was not NoRelocation.");
                Require(session.TryConfigureOnly(out string configureIssue),
                    "CASE 1 NoRelocation configure failed. " + configureIssue);
                Require(session.Participant.EnterCount == routeEnters,
                    "CASE 1 Route Spatial Entry repeated on Activity enter.");
                Require(session.Participant.LastContext.OccurrenceSequence == routeOccurrence,
                    "CASE 1 Activity enter created a new Route occurrence.");
                Require(session.Player.Target.position == known && SameRotation(session.Player.Target.rotation, knownRotation),
                    "CASE 1 NoRelocation changed pose.");
                Require(!session.Gate.HasEvidence(session.Player.SlotId),
                    "CASE 1 NoRelocation required relocation evidence.");
                LogCase("NoRelocationPreservesPose", activity, session, ActivityPlayerRelocationPolicy.NoRelocation, 0, true);
            }
            finally
            {
                Cleanup(unusedBinding, activity);
            }
        }

        private static void ProvePrimarySceneRelocation(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case2", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.primary", "Primary Relocation", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(11f, 2f, -4f), Quaternion.Euler(0f, 70f, 0f));
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 2 Activity enter failed.");
                Require(session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 2 Primary relocation failed. " + issue);
                Require(evidence.IsApplied && ReferenceEquals(evidence.Anchor, GetAnchor(authoring)),
                    "CASE 2 did not apply the Primary exact binding.");
                Require(SamePose(session.Player.Target, GetAnchor(authoring)), "CASE 2 Player did not receive the Primary anchor.");
                LogCase("PrimarySceneRelocation", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true, evidence);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private static void ProveRouteContentRelocation(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            RouteContentProfileAsset profile = CreateRouteContentProfile("qa.adr021.rel.route-content", world.RouteContent);
            using var session = Fixture.Create(world, p1, actorProfile, "case3", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose, profile);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.route-content-act", "Route Content Relocation", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.RouteContent, activity, p1, new Vector3(18f, 1f, 6f), Quaternion.Euler(0f, 20f, 0f));
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 3 Activity enter failed.");
                Require(session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 3 Route Content relocation failed. " + issue);
                Require(evidence.IsApplied && SamePose(session.Player.Target, GetAnchor(authoring)),
                    "CASE 3 Player did not receive the Route Content anchor.");
                LogCase("RouteContentRelocation", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true, evidence);
            }
            finally
            {
                Cleanup(authoring, activity, profile);
            }
        }

        private static void ProveActivityContentRelocation(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            ActivityContentProfileAsset content = CreateActivityContentProfile("qa.adr021.rel.act-content", world.ActivityContentA);
            using var session = Fixture.Create(world, p1, actorProfile, "case4", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.act-content-act", "Activity Content Relocation", content, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.ActivityContentA, activity, p1, new Vector3(-9f, 4f, 3f), Quaternion.Euler(0f, 180f, 0f));
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 4 Activity enter failed.");
                Require(session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 4 Activity Content relocation failed. " + issue);
                Require(evidence.IsApplied && SamePose(session.Player.Target, GetAnchor(authoring)),
                    "CASE 4 Player did not receive the Activity Content anchor.");
                LogCase("ActivityContentRelocation", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true, evidence);
            }
            finally
            {
                Cleanup(authoring, activity, content);
            }
        }

        private static void ProveNullActivityContentProfileDoesNotBlockRelocation(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case5", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.null-profile", "Null Profile Relocation", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(6f, 1f, 2f), Quaternion.identity);
            try
            {
                Require(activity.ActivityContentProfile == null, "CASE 5 ActivityContentProfile must be null.");
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 5 Activity enter failed.");
                Require(session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 5 null ActivityContentProfile blocked relocation. " + issue);
                Require(evidence.IsApplied && SamePose(session.Player.Target, GetAnchor(authoring)),
                    "CASE 5 did not apply the Primary/Route Content binding.");
                LogCase("NullActivityContentProfileDoesNotBlockRelocation", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true, evidence);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private static void ProveSharedPrimaryUsesActivityIdentity(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case6", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activityA = CreateActivity("qa.adr021.rel.shared-a", "Shared A", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            ActivityAsset activityB = CreateActivity("qa.adr021.rel.shared-b", "Shared B", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoringA = CreateRelocationBinding(world.Primary, activityA, p1, new Vector3(1f, 0f, 0f), Quaternion.Euler(0f, 10f, 0f));
            GameObject authoringB = CreateRelocationBinding(world.Primary, activityB, p1, new Vector3(25f, 0f, 0f), Quaternion.Euler(0f, 100f, 0f));
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activityA).Completed, "CASE 6 Activity A enter failed.");
                Require(session.TryRelocate(out _, out string issueA), "CASE 6 A failed. " + issueA);
                Require(SamePose(session.Player.Target, GetAnchor(authoringA)), "CASE 6 current A did not select AnchorA.");
                string physical = session.Player.PhysicalInstanceId;
                Require(session.StartActivity(activityB).Completed, "CASE 6 Activity B enter failed.");
                Require(session.TryRelocate(out _, out string issueB), "CASE 6 B failed. " + issueB);
                Require(session.Player.PhysicalInstanceId == physical, "CASE 6 replaced the physical Player.");
                Require(SamePose(session.Player.Target, GetAnchor(authoringB)), "CASE 6 current B did not select AnchorB.");
                Require(!SamePose(GetAnchor(authoringA), GetAnchor(authoringB)), "CASE 6 anchors must be distinct.");
                LogCase("SharedPrimaryUsesActivityIdentity", activityB, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true);
            }
            finally
            {
                Cleanup(authoringA, authoringB, activityA, activityB);
            }
        }

        private static void ProveMissingBindingFails(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case7", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.missing", "Missing", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            try
            {
                session.StartRoutePreserve();
                Vector3 before = new Vector3(4f, 5f, 6f);
                session.Player.Target.position = before;
                PoseSnapshot poseBefore = PoseSnapshot.Capture(session.Player.Target);
                Require(session.StartActivity(activity).Completed, "CASE 7 Activity enter failed.");
                Require(!session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 7 missing exact binding unexpectedly succeeded.");
                RequireFailedRelocation(evidence, issue, 0, "CASE 7 missing exact binding");
                Require(SamePose(session.Player.Target, poseBefore),
                    "CASE 7 moved the Player after a missing-binding failure.");
                LogCase("MissingBindingFails", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 0, false, evidence);
            }
            finally
            {
                Cleanup(activity);
            }
        }

        private static void ProveDuplicateBindingFails(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case8", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.duplicate", "Duplicate", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject first = CreateRelocationBinding(world.Primary, activity, p1, Vector3.one, Quaternion.identity);
            GameObject second = CreateRelocationBinding(world.Primary, activity, p1, Vector3.one * 2f, Quaternion.identity);
            try
            {
                session.StartRoutePreserve();
                PoseSnapshot poseBefore = PoseSnapshot.Capture(session.Player.Target);
                Require(session.StartActivity(activity).Completed, "CASE 8 Activity enter failed.");
                Require(!session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 8 duplicate exact bindings were not rejected.");
                RequireFailedRelocation(evidence, issue, 2, "CASE 8 duplicate exact bindings");
                Require(SamePose(session.Player.Target, poseBefore),
                    "CASE 8 chose a first-found duplicate anchor.");
                LogCase("DuplicateBindingFails", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 2, false, evidence);
            }
            finally
            {
                Cleanup(first, second, activity);
            }
        }

        private static void ProveUnrelatedSceneIgnored(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case9", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.unrelated", "Unrelated", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Unrelated, activity, p1, new Vector3(80f, 0f, 0f), Quaternion.identity);
            try
            {
                session.StartRoutePreserve();
                PoseSnapshot poseBefore = PoseSnapshot.Capture(session.Player.Target);
                Require(session.StartActivity(activity).Completed, "CASE 9 Activity enter failed.");
                Require(!session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 9 counted an unrelated loaded scene.");
                RequireFailedRelocation(evidence, issue, 0, "CASE 9 unrelated scene");
                Require(SamePose(session.Player.Target, poseBefore),
                    "CASE 9 moved the Player from an unrelated scene binding.");
                LogCase("UnrelatedSceneIgnored", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 0, false, evidence);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private static void ProvePersistentContentIgnored(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case10", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.persistent", "Persistent", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Persistent, activity, p1, new Vector3(70f, 1f, 0f), Quaternion.identity);
            try
            {
                session.StartRoutePreserve();
                PoseSnapshot poseBefore = PoseSnapshot.Capture(session.Player.Target);
                Require(session.StartActivity(activity).Completed, "CASE 10 Activity enter failed.");
                Require(!session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 10 used Persistent Content as a relocation source.");
                RequireFailedRelocation(evidence, issue, 0, "CASE 10 Persistent Content");
                Require(SamePose(session.Player.Target, poseBefore),
                    "CASE 10 moved the Player from a Persistent Content binding.");
                LogCase("PersistentContentIgnored", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 0, false, evidence);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private static void ProveOtherActivityContentIgnored(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case11", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityContentProfileAsset contentA = CreateActivityContentProfile("qa.adr021.rel.other-a", world.ActivityContentA);
            ActivityAsset activityA = CreateActivity("qa.adr021.rel.other-act-a", "Other A", contentA, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            ActivityAsset activityB = CreateActivity("qa.adr021.rel.other-act-b", "Other B", null, ActivityPlayerRelocationPolicy.NoRelocation);
            GameObject foreign = CreateRelocationBinding(world.ActivityContentB, activityA, p1, new Vector3(55f, 0f, 55f), Quaternion.identity);
            try
            {
                session.StartRoutePreserve();
                PoseSnapshot poseBefore = PoseSnapshot.Capture(session.Player.Target);
                Require(session.StartActivity(activityA).Completed, "CASE 11 Activity A enter failed.");
                Require(!session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 11 used another Activity's content scene as current discovery.");
                RequireFailedRelocation(evidence, issue, 0, "CASE 11 other Activity content");
                Require(SamePose(session.Player.Target, poseBefore),
                    "CASE 11 moved the Player from another Activity's content binding.");
                LogCase("OtherActivityContentIgnored", activityA, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 0, false, evidence);
            }
            finally
            {
                Cleanup(foreign, activityA, activityB, contentA);
            }
        }

        private static void ProveActivityChangeWithoutRelocationPreservesPose(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case12", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activityA = CreateActivity("qa.adr021.rel.change-none-a", "Change None A", null, ActivityPlayerRelocationPolicy.NoRelocation);
            ActivityAsset activityB = CreateActivity("qa.adr021.rel.change-none-b", "Change None B", null, ActivityPlayerRelocationPolicy.NoRelocation);
            try
            {
                session.StartRoutePreserve();
                Vector3 known = new Vector3(-4f, 2f, 8f);
                session.Player.Target.position = known;
                Require(session.StartActivity(activityA).Completed, "CASE 12 A failed.");
                string physical = session.Player.PhysicalInstanceId;
                Require(session.StartActivity(activityB).Completed, "CASE 12 B failed.");
                Require(session.Player.PhysicalInstanceId == physical, "CASE 12 replaced the physical Player.");
                Require(session.Player.Target.position == known, "CASE 12 A→B with NoRelocation changed pose.");
                LogCase("ActivityChangeWithoutRelocationPreservesPose", activityB, session, ActivityPlayerRelocationPolicy.NoRelocation, 0, true);
            }
            finally
            {
                Cleanup(activityA, activityB);
            }
        }

        private static void ProveActivityChangeWithRelocationMovesSamePlayer(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case13", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activityA = CreateActivity("qa.adr021.rel.change-a", "Change A", null, ActivityPlayerRelocationPolicy.NoRelocation);
            ActivityAsset activityB = CreateActivity("qa.adr021.rel.change-b", "Change B", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoringB = CreateRelocationBinding(world.Primary, activityB, p1, new Vector3(16f, 2f, -3f), Quaternion.Euler(0f, 45f, 0f));
            try
            {
                session.StartRoutePreserve();
                Vector3 before = new Vector3(0.5f, 0.5f, 0.5f);
                session.Player.Target.position = before;
                Require(session.StartActivity(activityA).Completed, "CASE 13 A failed.");
                string physical = session.Player.PhysicalInstanceId;
                int actors = session.Player.PhysicalInstanceId == physical ? 1 : 0;
                Require(session.StartActivity(activityB).Completed, "CASE 13 B failed.");
                Require(session.Player.PhysicalInstanceId == physical, "CASE 13 Join/Leave/Actor replacement occurred.");
                Require(session.TryRelocate(out _, out string issue), "CASE 13 B relocation failed. " + issue);
                Require(SamePose(session.Player.Target, GetAnchor(authoringB)), "CASE 13 same Player did not receive B pose.");
                Require(session.Player.Target.position != before, "CASE 13 B relocation did not move the Player.");
                Require(actors == 1, "CASE 13 lost the original physical identity.");
                LogCase("ActivityChangeWithRelocationMovesSamePlayer", activityB, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true);
            }
            finally
            {
                Cleanup(authoringB, activityA, activityB);
            }
        }

        private static void ProveReturnToSameActivityCreatesNewOccurrence(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case14", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activityA = CreateActivity("qa.adr021.rel.return-a", "Return A", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            ActivityAsset activityB = CreateActivity("qa.adr021.rel.return-b", "Return B", null, ActivityPlayerRelocationPolicy.NoRelocation);
            GameObject authoringA = CreateRelocationBinding(world.Primary, activityA, p1, new Vector3(-12f, 1f, 2f), Quaternion.Euler(0f, 30f, 0f));
            try
            {
                session.StartRoutePreserve();
                string physical = session.Player.PhysicalInstanceId;
                Require(session.StartActivity(activityA).Completed, "CASE 14 A#1 failed.");
                Require(session.TryRelocate(out _, out string issue1), "CASE 14 A#1 relocate failed. " + issue1);
                int sequence1 = session.OccurrenceSequence;
                Require(session.StartActivity(activityB).Completed, "CASE 14 B#2 failed.");
                int sequence2 = session.OccurrenceSequence;
                Require(sequence2 > sequence1, "CASE 14 B#2 reused A occurrence sequence.");
                session.Player.Target.position = session.Player.Target.position + Vector3.up;
                Require(session.StartActivity(activityA).Completed, "CASE 14 A#3 failed.");
                int sequence3 = session.OccurrenceSequence;
                Require(sequence3 > sequence2, "CASE 14 A#3 did not create a new Activity occurrence.");
                Require(session.Player.PhysicalInstanceId == physical, "CASE 14 replaced the physical Player.");
                Require(session.TryRelocate(out _, out string issue3), "CASE 14 A#3 relocate failed. " + issue3);
                Require(SamePose(session.Player.Target, GetAnchor(authoringA)),
                    "CASE 14 A#3 did not relocate again. An ActivityId-only gate would skip.");
                LogCase("ReturnToSameActivityCreatesNewOccurrence", activityA, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true);
            }
            finally
            {
                Cleanup(authoringA, activityA, activityB);
            }
        }

        private static void ProveSameOccurrenceIsIdempotent(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case15", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.idempotent", "Idempotent", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(7f, 2f, 1f), Quaternion.Euler(0f, 80f, 0f));
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 15 Activity enter failed.");
                Require(session.TryRelocate(out _, out string issue), "CASE 15 first relocate failed. " + issue);
                Vector3 nudged = session.Player.Target.position + new Vector3(0.4f, 0f, -0.2f);
                session.Player.Target.position = nudged;
                int sequence = session.OccurrenceSequence;
                Require(session.TryRelocate(out _, out string repeatIssue), "CASE 15 repeat failed. " + repeatIssue);
                Require(session.OccurrenceSequence == sequence, "CASE 15 repeat created a new occurrence.");
                Require(session.Player.Target.position == nudged,
                    "CASE 15 same Activity occurrence mutated pose a second time.");
                LogCase("SameOccurrenceIsIdempotent", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private static void ProveFailureDoesNotMarkSuccess(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case16", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.retry", "Retry", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = null;
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 16 Activity enter failed.");
                Require(!session.TryRelocate(out ActivityPlayerRelocationEvidence failed, out _),
                    "CASE 16 first attempt unexpectedly succeeded.");
                Require(!failed.IsApplied && !session.Gate.HasAppliedEvidence(session.Player.SlotId),
                    "CASE 16 stored failure as successful/idempotent completion.");
                authoring = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(8f, 3f, 1f), Quaternion.Euler(0f, 25f, 0f));
                Require(session.TryRelocate(out ActivityPlayerRelocationEvidence success, out string issue),
                    "CASE 16 retry of the same occurrence could not resolve. " + issue);
                Require(success.IsApplied && SamePose(session.Player.Target, GetAnchor(authoring)),
                    "CASE 16 retry did not apply the corrected binding.");
                LogCase("FailureDoesNotMarkSuccess", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true, success);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private static void ProveRelocationEvidenceOnlyWhenConfigured(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var none = Fixture.Create(world, p1, actorProfile, "case17-none", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset noRelocation = CreateActivity("qa.adr021.rel.evidence-none", "Evidence None", null, ActivityPlayerRelocationPolicy.NoRelocation);
            ActivityAsset apply = CreateActivity("qa.adr021.rel.evidence-apply", "Evidence Apply", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Primary, apply, p1, new Vector3(3f, 1f, 0f), Quaternion.identity);
            Fixture applySession = null;
            try
            {
                none.StartRoutePreserve();
                Require(none.StartActivity(noRelocation).Completed, "CASE 17 NoRelocation enter failed.");
                Require(none.TryConfigureOnly(out string noneIssue), "CASE 17 NoRelocation configure failed. " + noneIssue);
                Require(!none.Gate.HasEvidence(none.Player.SlotId),
                    "CASE 17 NoRelocation required evidence.");
                applySession = Fixture.Create(world, p1, actorProfile, "case17-apply", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
                applySession.StartRoutePreserve();
                Require(applySession.StartActivity(apply).Completed, "CASE 17 Apply enter failed.");
                Require(applySession.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue),
                    "CASE 17 ApplyExplicitRelocation failed. " + issue);
                Require(evidence.IsApplied, "CASE 17 ApplyExplicitRelocation did not require/produce Applied evidence.");
                LogCase("RelocationEvidenceOnlyWhenConfigured", apply, applySession, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true, evidence);
            }
            finally
            {
                applySession?.Dispose();
                Cleanup(authoring, noRelocation, apply);
            }
        }

        private static void ProveRelocationFailureDoesNotSatisfyEvidence(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case18", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.unsatisfied", "Unsatisfied", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 18 Activity enter failed.");
                Require(!session.TryRelocate(out ActivityPlayerRelocationEvidence evidence, out _),
                    "CASE 18 missing binding unexpectedly succeeded.");
                Require(evidence.Status == ActivityPlayerRelocationStatus.Failed && !evidence.IsApplied,
                    "CASE 18 failure evidence was treated as satisfied.");
                Require(!session.Gate.HasAppliedEvidence(session.Player.SlotId),
                    "CASE 18 gate stored satisfied evidence after failure.");
                LogCase("RelocationFailureDoesNotSatisfyEvidence", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 0, false, evidence);
            }
            finally
            {
                Cleanup(activity);
            }
        }

        private static void ProveRelocationDoesNotReplayRouteEntry(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case19", RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
            GameObject routeBinding = CreateRouteBinding(world.Primary, p1, new Vector3(2f, 4f, 6f), Quaternion.Euler(0f, 40f, 0f));
            ActivityAsset activity = CreateActivity("qa.adr021.rel.no-replay", "No Replay", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject relocation = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(21f, 0f, 3f), Quaternion.Euler(0f, 90f, 0f));
            try
            {
                session.StartRouteApply();
                Require(SamePose(session.Player.Target, GetAnchor(routeBinding)), "CASE 19 Route Spatial Entry was not applied.");
                int routeEnters = session.Participant.EnterCount;
                int routeOccurrence = session.Participant.LastContext.OccurrenceSequence;
                PoseSnapshot routePose = PoseSnapshot.Capture(session.Player.Target);
                Require(session.StartActivity(activity).Completed, "CASE 19 Activity enter failed.");
                Require(session.Participant.EnterCount == routeEnters &&
                        session.Participant.LastContext.OccurrenceSequence == routeOccurrence,
                    "CASE 19 Activity relocation replayed Route Spatial Entry.");
                Require(session.TryRelocate(out _, out string issue), "CASE 19 relocation failed. " + issue);
                Require(SamePose(session.Player.Target, GetAnchor(relocation)), "CASE 19 relocation did not apply.");
                Require(
                    session.Player.Target.position != routePose.Position ||
                    !SameRotation(session.Player.Target.rotation, routePose.Rotation),
                    "CASE 19 relocation left the Route pose unchanged.");
                LogCase("RelocationDoesNotReplayRouteEntry", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true);
            }
            finally
            {
                Cleanup(routeBinding, relocation, activity);
            }
        }

        private static void ProveSceneProvidedCanBeRelocated(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case20", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose, admitImmediately: false);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.scene", "Scene Provided", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(-6f, 2f, 4f), Quaternion.Euler(0f, 270f, 0f));
            try
            {
                session.StartRoutePreserve();
                string physical = session.Player.PhysicalInstanceId;
                Require(session.StartActivity(activity).Completed, "CASE 20 Activity enter failed.");
                Require(session.TryRelocate(out _, out string issue), "CASE 20 Scene-Provided relocation failed. " + issue);
                Require(session.Player.PhysicalInstanceId == physical, "CASE 20 replaced the Scene-Provided physical Actor.");
                Require(SamePose(session.Player.Target, GetAnchor(authoring)),
                    "CASE 20 Scene-Provided Actor did not receive Activity relocation.");
                LogCase("SceneProvidedCanBeRelocated", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private static void ProveManagerProvisionedCanBeRelocated(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case21", RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
            GameObject routeBinding = CreateRouteBinding(world.Primary, p1, new Vector3(1f, 1f, 1f), Quaternion.identity);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.manager", "Manager", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject relocation = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(13f, 0f, -2f), Quaternion.Euler(0f, 55f, 0f));
            try
            {
                session.StartRouteApply();
                Require(SamePose(session.Player.Target, GetAnchor(routeBinding)), "CASE 21 Route Entry was not applied.");
                string physical = session.Player.PhysicalInstanceId;
                int applyInvocations = session.Participant.ApplyInvocationCount;
                Require(session.StartActivity(activity).Completed, "CASE 21 Activity enter failed.");
                Require(session.TryRelocate(out _, out string issue), "CASE 21 Manager-Provisioned relocation failed. " + issue);
                Require(session.Player.PhysicalInstanceId == physical, "CASE 21 provisioning/physical replacement occurred.");
                Require(session.Participant.ApplyInvocationCount == applyInvocations,
                    "CASE 21 Route Spatial Entry / provisioning repeated.");
                Require(SamePose(session.Player.Target, GetAnchor(relocation)), "CASE 21 Player did not receive relocation.");
                LogCase("ManagerProvisionedCanBeRelocated", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true);
            }
            finally
            {
                Cleanup(routeBinding, relocation, activity);
            }
        }

        private static void ProveRouteExitOrActivityClearDoesNotLeaveStaleRelocationContext(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case22", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.stale", "Stale", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(5f, 1f, 1f), Quaternion.identity);
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 22 Activity enter failed.");
                Require(session.TryRelocate(out _, out string issue), "CASE 22 first relocate failed. " + issue);
                ActivityFlowStartResult cleared = session.ClearActivity();
                Require(cleared.Completed, "CASE 22 Activity clear failed. " + cleared.Message);
                Require(session.Runtime.CurrentActivity == null, "CASE 22 current Activity was not cleared.");
                Require(
                    session.Runtime.CurrentActivityFlowRuntime == null ||
                    !session.Runtime.CurrentActivityFlowRuntime.TryCreateCurrentActivityContentDiscoveryScope(
                        activity, out _),
                    "CASE 22 late preparation can still reconstruct a current Activity discovery scope.");
                session.Gate.Clear();
                Require(
                    !session.TryRelocate(out _, out string lateIssue),
                    "CASE 22 late preparation received a stale Activity relocation occurrence. " + lateIssue);
                LogCase("RouteExitOrActivityClearDoesNotLeaveStaleRelocationContext", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 0, false);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private static void ProvePhysicalRepresentationReplacement(World world, PlayerSlotProfile p1, ActorProfile actorProfile)
        {
            world.EnsureLoaded();
            using var session = Fixture.Create(world, p1, actorProfile, "case23", RoutePlayerSpatialEntryPolicy.PreserveCurrentPose);
            ActivityAsset activity = CreateActivity("qa.adr021.rel.representation", "Representation", null, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation);
            GameObject authoring = CreateRelocationBinding(world.Primary, activity, p1, new Vector3(3f, 2f, 1f), Quaternion.Euler(0f, 60f, 0f));
            try
            {
                session.StartRoutePreserve();
                Require(session.StartActivity(activity).Completed, "CASE 23 Activity enter failed.");
                Require(session.TryRelocate(out _, out string issue), "CASE 23 first representation failed. " + issue);
                session.Player.Target.position = session.Player.Target.position + Vector3.up;
                PlayerActorMaterializationHandle replacement = session.Player.CreateReplacementHandle("qa.physical.rel.case23-b");
                Require(
                    session.Gate.TryApply(session.CurrentContext, session.Player.SlotId, session.Player.ActorId, replacement, session.Player.Target, out _, out string replaceIssue),
                    "CASE 23 replacement representation was not processed. " + replaceIssue);
                Require(SamePose(session.Player.Target, GetAnchor(authoring)),
                    "CASE 23 gate ignored physical representation identity inside the same Activity occurrence.");
                LogCase("PhysicalRepresentationReplacementWithinSameActivityOccurrence", activity, session, ActivityPlayerRelocationPolicy.ApplyExplicitRelocation, 1, true);
            }
            finally
            {
                Cleanup(authoring, activity);
            }
        }

        private sealed class Fixture : IDisposable
        {
            private Fixture(
                World world,
                RouteAsset route,
                SessionPlayer player,
                SpatialEntryParticipant participant,
                RouteLifecycleRuntime runtime,
                RelocationGate gate,
                bool applyRoute)
            {
                World = world;
                Route = route;
                Player = player;
                Participant = participant;
                Runtime = runtime;
                Gate = gate;
                ApplyRoute = applyRoute;
            }

            internal World World { get; }
            internal RouteAsset Route { get; }
            internal SessionPlayer Player { get; }
            internal SpatialEntryParticipant Participant { get; }
            internal RouteLifecycleRuntime Runtime { get; }
            internal RelocationGate Gate { get; }
            internal bool ApplyRoute { get; }
            internal int OccurrenceSequence => Runtime.CurrentOccurrence.TransitionSequence;

            internal static Fixture Create(
                World world,
                PlayerSlotProfile p1,
                ActorProfile actorProfile,
                string id,
                RoutePlayerSpatialEntryPolicy routePolicy,
                RouteContentProfileAsset routeContent = null,
                bool admitImmediately = true)
            {
                RouteAsset route = CreateRoute(
                    "qa.adr021.rel.route." + id,
                    "ADR021 Rel " + id,
                    world.Primary,
                    routeContent,
                    routePolicy);
                SessionPlayer player = SessionPlayer.Create(
                    world.Session, p1, actorProfile, "qa.adr021.rel.actor." + id, "qa.physical.rel." + id);
                RouteLifecycleRuntime runtime = CreateRuntime();
                var participant = new SpatialEntryParticipant(player, admitImmediately);
                Require(runtime.SetPlayerSpatialEntryParticipant(participant, out string attachIssue),
                    "Could not attach Route spatial-entry participant. " + attachIssue);
                return new Fixture(world, route, player, participant, runtime, new RelocationGate(), routePolicy == RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement);
            }

            internal void StartRoutePreserve() => StartRouteCore();

            internal void StartRouteApply() => StartRouteCore();

            private void StartRouteCore()
            {
                RouteLifecycleStartResult result = Complete(
                    Runtime.StartRouteAsync(Route, "QA_ADR021_ACTIVITY_RELOCATION", "qa-activity-relocation"),
                    "RouteLifecycleRuntime.StartRouteAsync");
                Require(result.Started, "Route start failed. " + result.Message);
            }

            internal ActivityFlowStartResult StartActivity(ActivityAsset activity)
            {
                ActivityFlowStartResult result = Complete(
                    Runtime.StartActivityAsync(activity, "QA_ADR021_ACTIVITY_RELOCATION", "qa-activity-relocation"),
                    "RouteLifecycleRuntime.StartActivityAsync");
                Gate.Clear();
                return result;
            }

            internal ActivityFlowStartResult ClearActivity()
            {
                ActivityFlowStartResult result = Complete(
                    Runtime.ClearActivityAsync("QA_ADR021_ACTIVITY_RELOCATION", "qa-activity-clear"),
                    "RouteLifecycleRuntime.ClearActivityAsync");
                Gate.Clear();
                return result;
            }

            internal ActivityTransitionPreparationContext CurrentContext
            {
                get
                {
                    Require(TryCreateContext(out ActivityTransitionPreparationContext context, out string issue), issue);
                    return context;
                }
            }

            internal bool TryConfigureOnly(out string issue)
            {
                if (!TryCreateContext(out ActivityTransitionPreparationContext context, out issue))
                {
                    return false;
                }

                return Gate.TryConfigureAndApply(context, Player, out _, out issue);
            }

            internal bool TryRelocate(out ActivityPlayerRelocationEvidence evidence, out string issue)
            {
                evidence = default;
                if (!TryCreateContext(out ActivityTransitionPreparationContext context, out issue))
                {
                    return false;
                }

                if (Gate.IsCurrent(context))
                {
                    return Gate.TryApply(
                        context,
                        Player.SlotId,
                        Player.ActorId,
                        Player.Handle,
                        Player.Target,
                        out evidence,
                        out issue);
                }

                return Gate.TryConfigureAndApply(context, Player, out evidence, out issue);
            }

            internal bool TryCreateContext(out ActivityTransitionPreparationContext context, out string issue)
            {
                context = default;
                issue = string.Empty;
                ActivityFlowRuntime flow = Runtime.CurrentActivityFlowRuntime;
                ActivityAsset activity = Runtime.CurrentActivity;
                if (flow == null || activity == null || !Runtime.CurrentOccurrence.IsValid)
                {
                    issue = "No current Activity occurrence is published for relocation.";
                    return false;
                }

                if (!flow.TryCreateCurrentActivityContentDiscoveryScope(activity, out ActivityContentDiscoveryScope scope))
                {
                    issue = "Current Activity discovery scope is unavailable.";
                    return false;
                }

                RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                    activity.ActivityId.StableText,
                    activity.ActivityName,
                    RuntimeDefinitionToken.FromUnityObject(activity));
                context = new ActivityTransitionPreparationContext(activity, owner, Runtime.CurrentOccurrence, scope);
                if (!context.IsValid)
                {
                    issue = "Reconstructed ActivityTransitionPreparationContext is invalid.";
                    return false;
                }

                return true;
            }

            public void Dispose()
            {
                Player?.Dispose();
                if (Route != null)
                {
                    UnityEngine.Object.DestroyImmediate(Route);
                }
            }
        }

        /// <summary>
        /// Edit Mode stand-in for PlayerActorPreparationRuntimeHostModule.TryApplyCurrentActivityRelocation.
        /// Skip/store/remove-on-failure predicates match the Host module; apply/discovery stay on
        /// ActivityPlayerRelocationRuntime.
        /// </summary>
        private sealed class RelocationGate
        {
            private readonly Dictionary<PlayerSlotId, ActivityPlayerRelocationEvidence> evidenceBySlot =
                new Dictionary<PlayerSlotId, ActivityPlayerRelocationEvidence>();
            private ActivityTransitionPreparationContext current;

            internal bool TryConfigureAndApply(
                ActivityTransitionPreparationContext context,
                SessionPlayer player,
                out ActivityPlayerRelocationEvidence evidence,
                out string issue)
            {
                evidence = default;
                issue = string.Empty;
                if (!context.IsValid || !context.Activity.HasDefinedPlayerRelocationPolicy)
                {
                    issue = "Activity Player relocation requires a valid target occurrence and a defined policy.";
                    return false;
                }

                current = context;
                evidenceBySlot.Clear();
                if (context.Activity.PlayerRelocationPolicy == ActivityPlayerRelocationPolicy.NoRelocation)
                {
                    return true;
                }

                return TryApply(context, player.SlotId, player.ActorId, player.Handle, player.Target, out evidence, out issue);
            }

            internal bool TryApply(
                ActivityTransitionPreparationContext context,
                PlayerSlotId slotId,
                ActorId actorId,
                PlayerActorMaterializationHandle handle,
                Transform target,
                out ActivityPlayerRelocationEvidence evidence,
                out string issue)
            {
                evidence = default;
                issue = string.Empty;
                current = context;
                if (!current.IsValid ||
                    current.Activity.PlayerRelocationPolicy == ActivityPlayerRelocationPolicy.NoRelocation)
                {
                    return true;
                }

                if (current.Activity.PlayerRelocationPolicy != ActivityPlayerRelocationPolicy.ApplyExplicitRelocation ||
                    handle == null || target == null)
                {
                    issue = "Activity Player relocation requires ApplyExplicitRelocation and a prepared physical target.";
                    return false;
                }

                string representation = handle.Request.RuntimeContentIdentity.StableText;
                if (evidenceBySlot.TryGetValue(slotId, out ActivityPlayerRelocationEvidence previous) &&
                    previous.IsApplied &&
                    previous.Owner == current.Owner &&
                    previous.Occurrence.Matches(current.Activity, current.Occurrence.TransitionSequence) &&
                    previous.RepresentationIdentity == representation &&
                    ReferenceEquals(previous.Target, target))
                {
                    evidence = previous;
                    return true;
                }

                if (!ActivityPlayerRelocationRuntime.TryApply(
                        current, slotId, actorId, representation, target, out evidence, out issue))
                {
                    evidenceBySlot.Remove(slotId);
                    return false;
                }

                evidenceBySlot[slotId] = evidence;
                return true;
            }

            internal bool IsCurrent(ActivityTransitionPreparationContext context) =>
                current.IsValid && context.IsValid &&
                ReferenceEquals(current.Activity, context.Activity) &&
                current.Occurrence.Matches(context.Activity, context.Occurrence.TransitionSequence);

            internal bool HasEvidence(PlayerSlotId slotId) => evidenceBySlot.ContainsKey(slotId);

            internal bool HasAppliedEvidence(PlayerSlotId slotId) =>
                evidenceBySlot.TryGetValue(slotId, out ActivityPlayerRelocationEvidence evidence) && evidence.IsApplied;

            internal void Clear()
            {
                current = default;
                evidenceBySlot.Clear();
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

        private static T Complete<T>(Task<T> task, string operation)
        {
            Require(task != null, operation + " returned no Task.");
            Require(
                task.IsCompleted,
                operation + " did not complete synchronously. Edit Mode Activity Relocation QA requires already-loaded scenes.");
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
            RoutePlayerSpatialEntryPolicy policy)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = routeName;
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = routeId;
            serialized.FindProperty("routeName").stringValue = routeName;
            serialized.FindProperty("primaryScenePath").stringValue = primary.path;
            serialized.FindProperty("primarySceneName").stringValue = primary.name;
            serialized.FindProperty("routeContentProfile").objectReferenceValue = content;
            serialized.FindProperty("startupActivity").objectReferenceValue = null;
            serialized.FindProperty("playerSpatialEntryPolicy").intValue = (int)policy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(route.HasValidRouteId && route.HasPrimaryScene, "QA RouteAsset is invalid.");
            return route;
        }

        private static ActivityAsset CreateActivity(
            string activityId,
            string activityName,
            ActivityContentProfileAsset content,
            ActivityPlayerRelocationPolicy policy)
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
            serialized.FindProperty("playerRelocationPolicy").intValue = (int)policy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(activity.HasValidActivityId && activity.HasDefinedPlayerRelocationPolicy,
                "QA ActivityAsset is invalid.");
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
            Scene scene, PlayerSlotProfile slot, Vector3 position, Quaternion rotation)
        {
            GameObject root = CreateInScene(scene, "RoutePlayerSpatialEntry");
            RoutePlayerSpatialEntryAuthoring authoring = root.AddComponent<RoutePlayerSpatialEntryAuthoring>();
            GameObject anchor = CreateInScene(scene, "RouteAnchor");
            anchor.transform.SetParent(root.transform, true);
            anchor.transform.SetPositionAndRotation(position, rotation);
            var serialized = new SerializedObject(authoring);
            SerializedProperty bindings = serialized.FindProperty("bindings");
            bindings.arraySize = 1;
            SerializedProperty element = bindings.GetArrayElementAtIndex(0);
            element.FindPropertyRelative("playerSlotProfile").objectReferenceValue = slot;
            element.FindPropertyRelative("placementAnchor").objectReferenceValue = anchor.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject CreateRelocationBinding(
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
            Require(authoringRoot != null && authoringRoot.transform.childCount > 0, "Authoring root has no anchor child.");
            return authoringRoot.transform.GetChild(0);
        }

        private static bool SamePose(Transform left, Transform right) =>
            Vector3.Distance(left.position, right.position) <= 0.0001f &&
            SameRotation(left.rotation, right.rotation);

        private static bool SamePose(Transform target, PoseSnapshot snapshot) =>
            target != null &&
            Vector3.Distance(target.position, snapshot.Position) <= 0.0001f &&
            SameRotation(target.rotation, snapshot.Rotation);

        private static void RequireFailedRelocation(
            ActivityPlayerRelocationEvidence evidence,
            string issue,
            int expectedMatchingBindings,
            string caseName)
        {
            Require(
                evidence.Status == ActivityPlayerRelocationStatus.Failed &&
                !evidence.IsApplied,
                $"{caseName} did not publish failed, unapplied relocation evidence. " +
                $"status='{evidence.Status}' applied='{evidence.IsApplied}' " +
                $"issue='{issue}'.");
            Require(
                issue.Contains(
                    $"Matching bindings: '{expectedMatchingBindings}'",
                    StringComparison.Ordinal),
                $"{caseName} did not report the expected matching-binding cardinality " +
                $"'{expectedMatchingBindings}'. {issue}");
        }

        private static bool SameRotation(Quaternion left, Quaternion right) =>
            Quaternion.Angle(left, right) <= 0.01f;

        private static void Cleanup(params UnityEngine.Object[] objects)
        {
            if (objects == null)
            {
                return;
            }

            for (int index = 0; index < objects.Length; index++)
            {
                if (objects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(objects[index]);
                }
            }
        }

        private static void LogCase(
            string caseName,
            ActivityAsset activity,
            Fixture session,
            ActivityPlayerRelocationPolicy policy,
            int bindingCount,
            bool success,
            ActivityPlayerRelocationEvidence evidence = default)
        {
            string activityId = activity != null && activity.HasValidActivityId ? activity.ActivityId.StableText : string.Empty;
            string occurrence = session != null && session.Runtime.CurrentOccurrence.IsValid
                ? session.Runtime.CurrentOccurrence.TransitionSequence.ToString()
                : "none";
            string slot = session != null ? session.Player.SlotId.StableText : string.Empty;
            string physical = session != null ? session.Player.PhysicalInstanceId : string.Empty;
            string anchorScene = evidence.Anchor != null ? evidence.Anchor.gameObject.scene.path : string.Empty;
            Debug.Log(
                $"{Prefix} identity case='{caseName}' activityId='{activityId}' occurrence='{occurrence}' " +
                $"slot='{slot}' physical='{physical}' policy='{policy}' exactBindingCount='{bindingCount}' " +
                $"anchorScene='{anchorScene}' evidence='{evidence.Status}' result='{(success ? "success" : "failure")}'.");
        }

        private static void RunCase(string caseName, Action proof, List<string> failures, ref int passed)
        {
            try
            {
                proof();
                passed++;
                Debug.Log($"{Prefix} case='{caseName}' status='PASS'.");
            }
            catch (Exception exception)
            {
                failures.Add($"{caseName}: {exception.GetType().Name}: {exception.Message}");
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
                Scene primary,
                Scene routeContent,
                Scene activityContentA,
                Scene activityContentB,
                Scene unrelated,
                Scene persistent)
            {
                this.temporaryRoot = temporaryRoot;
                Session = session;
                Primary = primary;
                RouteContent = routeContent;
                ActivityContentA = activityContentA;
                ActivityContentB = activityContentB;
                Unrelated = unrelated;
                Persistent = persistent;
            }

            internal Scene Session { get; }
            internal Scene Primary { get; }
            internal Scene RouteContent { get; }
            internal Scene ActivityContentA { get; }
            internal Scene ActivityContentB { get; }
            internal Scene Unrelated { get; }
            internal Scene Persistent { get; }

            internal static World Create()
            {
                string temporaryId = Guid.NewGuid().ToString("N");
                string folderName = "__Adr021ActivityRelocation_" + temporaryId;
                string temporaryRoot = "Assets/ImmersiveFrameworkQA/" + folderName;
                Require(
                    AssetDatabase.CreateFolder("Assets/ImmersiveFrameworkQA", folderName).Length > 0,
                    "ADR-021 Activity Relocation could not create its temporary Editor scene root.");
                return new World(
                    temporaryRoot,
                    CreateScene(temporaryRoot, "Session"),
                    CreateScene(temporaryRoot, "Primary"),
                    CreateScene(temporaryRoot, "RouteContent"),
                    CreateScene(temporaryRoot, "ActivityContentA"),
                    CreateScene(temporaryRoot, "ActivityContentB"),
                    CreateScene(temporaryRoot, "Unrelated"),
                    CreateScene(temporaryRoot, "Persistent"));
            }

            internal void EnsureLoaded()
            {
                EnsureScene(Session);
                EnsureScene(Primary);
                EnsureScene(RouteContent);
                EnsureScene(ActivityContentA);
                EnsureScene(ActivityContentB);
                EnsureScene(Unrelated);
                EnsureScene(Persistent);
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                Close(Persistent);
                Close(Unrelated);
                Close(ActivityContentB);
                Close(ActivityContentA);
                Close(RouteContent);
                Close(Primary);
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
                GameObject root = CreateInScene(session, "QA_ADR021_Rel_SessionPlayer");
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

                RoutePlayerSpatialEntryRuntimeBinding binding = root.AddComponent<RoutePlayerSpatialEntryRuntimeBinding>();
                Require(slot.TryGetPlayerSlotId(out PlayerSlotId slotId, out string slotIssue),
                    "QA PlayerSlotProfile did not resolve a SlotId. " + slotIssue);
                PlayerActorMaterializationHandle handle = CreateHandle(
                    slot, slotId, actorProfile, host, playerInput, actor, actorObject, ActorId.From(actorIdText), representationId);
                return new SessionPlayer(root, host, actor, binding, handle, slotId, ActorId.From(actorIdText), handle.Request.RuntimeContentIdentity.StableText)
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
                Handle = replacement;
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
                RuntimeContentOwner owner = RuntimeContentOwner.Session("qa.adr021.rel.session", "ADR021 Relocation Session");
                var scope = new RuntimeScopeContext(owner, "QA_ADR021_ACTIVITY_RELOCATION", "session-player");
                var snapshot = new PlayerSlotRuntimeSnapshot(
                    0, slotProfile, slotId, PlayerSlotAllocationState.Joined,
                    new PlayerSlotReservationToken("qa.adr021.rel.session", 1, slotId, 1),
                    1, "QA_ADR021_ACTIVITY_RELOCATION", "joined", actorProfile, 1,
                    "QA_ADR021_ACTIVITY_RELOCATION", "selected");
                Require(
                    PlayerActorMaterializationOperationId.TryCreate(
                        "qa.adr021.rel.session", owner, slotId, 1,
                        out PlayerActorMaterializationOperationId operationId, out string operationIssue),
                    "Could not create a Player Actor materialization operation id. " + operationIssue);
                RuntimeContentId contentId = RuntimeContentId.From(representationId);
                var request = new PlayerActorMaterializationRequest(
                    operationId, "qa.adr021.rel.session", scope, snapshot, actorProfile, host, actorId, contentId, 1,
                    "QA_ADR021_ACTIVITY_RELOCATION", "materialize");
                Require(request.IsValid, "QA PlayerActorMaterializationRequest is invalid. " + request.ToDiagnosticString());
                var resource = new RuntimeMaterializationResource("logical-actor", representationId, "ADR021 Rel Logical Actor", string.Empty);
                var cancellation = new RuntimeScopeCancellationToken(owner, 1, RuntimeScopeTransitionState.Active, "QA_ADR021_ACTIVITY_RELOCATION", "active");
                var runtimeRequest = new RuntimeMaterializationRequest(scope, contentId, resource, cancellation, "QA_ADR021_ACTIVITY_RELOCATION", "materialize");
                RuntimeContentHandle contentHandle = RuntimeContentHandle.Materialized(runtimeRequest.Identity, "QA_ADR021_ACTIVITY_RELOCATION", "materialize");
                return new PlayerActorMaterializationHandle(
                    request, runtimeRequest, contentHandle, host, playerInput, actor, logicalActorHost,
                    "QA_ADR021_ACTIVITY_RELOCATION", "materialize");
            }
        }

        private sealed class SpatialEntryParticipant : IRoutePlayerSpatialEntryLifecycleParticipant
        {
            private readonly SessionPlayer player;
            private readonly bool admitted;

            internal SpatialEntryParticipant(SessionPlayer sessionPlayer, bool admitImmediately)
            {
                player = sessionPlayer;
                admitted = admitImmediately && sessionPlayer != null;
            }

            internal int EnterCount { get; private set; }
            internal int ApplyInvocationCount { get; private set; }
            internal RoutePlayerSpatialEntryContext LastContext { get; private set; }

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
                    return true;
                }

                ApplyInvocationCount++;
                return player.Binding.TryApplyBeforeActivation(player.Handle, out issue);
            }

            public void ExitRouteSpatialEntry(RoutePlayerSpatialEntryContext context)
            {
                if (LastContext.Matches(context))
                {
                    LastContext = default;
                }
            }
        }

        private sealed class QaAdr21RoutePort : IRouteRuntimePort
        {
            public Task<FrameworkRouteRequestResult> RequestRouteAsync(RouteAsset targetRoute, string source, string reason) =>
                Task.FromResult(FrameworkRouteRequestResult.SucceededWith(targetRoute, source, reason, default));
        }

        private sealed class QaAdr21ActivityPort : IActivityRuntimePort
        {
            public Task<FrameworkActivityRequestResult> RequestActivityAsync(ActivityAsset targetActivity, string source, string reason) =>
                Task.FromResult(FrameworkActivityRequestResult.SucceededWith(targetActivity, source, reason, default));

            public Task<FrameworkActivityRequestResult> ClearActivityAsync(string source, string reason) =>
                Task.FromResult(FrameworkActivityRequestResult.SucceededWith(null, source, reason, default));
        }

        private sealed class QaAdr21RouteCycleResetPort : IRouteCycleResetRuntimePort
        {
            public Task<CycleResetResult> RequestRouteCycleResetAsync(string source, string reason) =>
                Task.FromResult(default(CycleResetResult));
        }

        private sealed class QaAdr21ActivityCycleResetPort : IActivityCycleResetRuntimePort
        {
            public Task<CycleResetResult> RequestActivityCycleResetAsync(string source, string reason) =>
                Task.FromResult(default(CycleResetResult));
        }

        private sealed class QaAdr21ActivityRestartPort : IActivityRestartRuntimePort
        {
            public Task<ActivityRestartRuntimeResult> RequestActivityRestartAsync(
                ActivityAsset targetActivity,
                bool useCurrentActivityWhenTargetMissing,
                bool requireTargetActivityIsCurrent,
                ResetSelectionConfig resetSelection,
                string source,
                string reason) =>
                Task.FromResult(ActivityRestartRuntimeResult.From(null));
        }
    }
}

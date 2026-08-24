using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Internal.Editor
{
    /// <summary>
    /// IF-ADR-021 — Activity Player Actor Initial Placement regression.
    /// Preserves the historical Activity proof while asserting the Model B Route
    /// authoring policy surface. Route lifecycle certification remains a separate
    /// Play Mode proof; this runner does not duplicate the runtime lifecycle.
    /// </summary>
    public static class QaAdr21ActivityPlayerInitialPlacementRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run ADR-021 Initial Placement QA";
        private const string Prefix = "[QA_ADR021_INITIAL_PLACEMENT]";
        private const int ExpectedCaseCount = 9;
        private const string SlotP1Path =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/SlotsProfiles/PlayerSlotProfileP1.asset";
        private const string SlotP2Path =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/SlotsProfiles/PlayerSlotProfileP2.asset";
        private const string ActivityPath =
            "Assets/ImmersiveFrameworkQA/Player/P3J6/P3J6_PlayerActorLifecycleActivity.asset";
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
                error = "ADR-021 Initial Placement QA must run in Edit Mode.";
                Debug.LogError($"{Prefix} status='Failed' cases='0/{ExpectedCaseCount}' error='{Escape(error)}'.");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                error = "ADR-021 Initial Placement QA was cancelled because the current Editor scenes were not saved.";
                Debug.LogError($"{Prefix} status='Failed' cases='0/{ExpectedCaseCount}' error='{Escape(error)}'.");
                return false;
            }

            var failures = new List<string>();
            int passed = 0;
            PlayerSlotProfile p1 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP1Path);
            PlayerSlotProfile p2 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP2Path);
            ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);

            try
            {
                Require(p1 != null, $"Missing QA PlayerSlotProfile at '{SlotP1Path}'.");
                Require(p2 != null, $"Missing QA PlayerSlotProfile at '{SlotP2Path}'.");
                Require(activity != null && activity.HasValidActivityId,
                    $"Missing or invalid QA Activity at '{ActivityPath}'.");

                RunCase("AuthoringAndRoutePolicy", () => ProveAuthoringAndRoutePolicy(p1), failures, ref passed);
                RunCase("ManagerLogicalActorTarget", () => ProveManagerLogicalActorTarget(activity, p1), failures, ref passed);
                RunCase("MissingBindingNoFallback", () => ProveMissingBindingNoFallback(activity, p1, p2), failures, ref passed);
                RunCase("DuplicateExactSlotRejected", () => ProveDuplicateExactSlotRejected(activity, p1), failures, ref passed);
                RunCase("ForeignSceneIgnored", () => ProveForeignSceneIgnored(activity, p1), failures, ref passed);
                RunCase("AnchorOutsideOwnedSceneRejected", () => ProveAnchorOutsideOwnedSceneRejected(activity, p1), failures, ref passed);
                RunCase("SceneProvidedPreserveAuthoredPose", () => ProvePreserveAuthoredPose(activity, p1), failures, ref passed);
                RunCase("SceneProvidedApplyActivityPlacement", () => ProveSceneApplyPlacement(activity, p1), failures, ref passed);
                RunCase("FailedPlacementEvidence", () => ProveFailedPlacementEvidence(activity, p1), failures, ref passed);
            }
            catch (Exception exception)
            {
                failures.Add($"harness: {exception.GetType().Name}: {exception.Message}");
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
            Debug.Log($"{Prefix} status='Passed' verdict='ADR-021 INITIAL PLACEMENT VERIFIED' cases='{passed}/{ExpectedCaseCount}'.");
            return true;
        }

        private static void ProveAuthoringAndRoutePolicy(PlayerSlotProfile p1)
        {
            GameObject root = null;
            try
            {
                root = new GameObject("QA_ADR021_Authoring");
                ActivityPlayerInitialPlacementAuthoring authoring =
                    root.AddComponent<ActivityPlayerInitialPlacementAuthoring>();
                GameObject anchorObject = new GameObject("Anchor");
                anchorObject.transform.SetParent(root.transform, false);
                SetBindings(authoring, new BindingSpec(p1, anchorObject.transform));

                Require(authoring.Bindings.Count == 1, "Authoring did not retain exactly one explicit binding.");
                ActivityPlayerInitialPlacementAuthoring.Binding binding = authoring.Bindings[0];
                Require(binding != null && ReferenceEquals(binding.PlayerSlotProfile, p1),
                    "Binding did not retain the exact PlayerSlotProfile reference.");
                Require(ReferenceEquals(binding.PlacementAnchor, anchorObject.transform),
                    "Binding did not retain the exact Transform anchor reference.");
                Require(binding.TryGetPlayerSlotId(out PlayerSlotId slot, out string issue) && slot == PlayerSlotId.Player1,
                    "Binding did not resolve exact PlayerSlotId. " + issue);

                GameObject routeRoot = new GameObject("QA_ADR021_RouteSpatialEntry");
                routeRoot.transform.SetParent(root.transform, false);
                RoutePlayerSpatialEntryAuthoring routeAuthoring =
                    routeRoot.AddComponent<RoutePlayerSpatialEntryAuthoring>();
                SetRouteBindings(routeAuthoring, new BindingSpec(p1, anchorObject.transform));
                string routeIssue = string.Empty;
                bool hasExactRouteSlot = routeAuthoring.Bindings.Count == 1 &&
                    routeAuthoring.Bindings[0].TryGetPlayerSlotId(
                        out PlayerSlotId routeSlot,
                        out routeIssue) &&
                    routeSlot == PlayerSlotId.Player1;
                Require(hasExactRouteSlot,
                    "Route authoring did not retain the exact Slot binding. " + routeIssue);
                Require((int)RoutePlayerSpatialEntryPolicy.PreserveCurrentPose == 0 &&
                        (int)RoutePlayerSpatialEntryPolicy.ApplyExplicitPlacement == 1,
                    "Route spatial-entry policy serialized identities changed.");
            }
            finally
            {
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ProveManagerLogicalActorTarget(ActivityAsset activity, PlayerSlotProfile p1)
        {
            WithScenes(activity, (owned, foreign, context) =>
            {
                Transform anchor = CreatePlacementAuthoring(owned, p1, new Vector3(12f, 3f, -7f), Quaternion.Euler(0f, 137f, 0f));
                GameObject localPlayerHost = CreateInScene(foreign, "LocalPlayerHost");
                localPlayerHost.transform.SetPositionAndRotation(new Vector3(100f, 5f, 100f), Quaternion.Euler(0f, 20f, 0f));
                GameObject logicalActorHost = new GameObject("LogicalActorHost");
                SceneManager.MoveGameObjectToScene(logicalActorHost, foreign);
                logicalActorHost.transform.SetParent(localPlayerHost.transform, true);
                logicalActorHost.transform.localScale = new Vector3(2f, 3f, 4f);

                Vector3 hostPosition = localPlayerHost.transform.position;
                Quaternion hostRotation = localPlayerHost.transform.rotation;
                Transform parent = logicalActorHost.transform.parent;
                Vector3 scale = logicalActorHost.transform.localScale;

                bool applied = ActivityPlayerInitialPlacementRuntime.TryApplyRequiredPlacement(
                    context, PlayerSlotId.Player1, ActorId.From("qa.adr21.manager"),
                    "qa:manager:logical-actor", logicalActorHost.transform,
                    out ActivityPlayerInitialPlacementEvidence evidence, out string issue);

                Require(applied, "Manager placement failed. " + issue);
                Require(evidence.Status == ActivityPlayerInitialPlacementStatus.Applied,
                    "Manager placement did not publish Applied evidence.");
                Require(ReferenceEquals(evidence.Target, logicalActorHost.transform) && ReferenceEquals(evidence.Anchor, anchor),
                    "Placement evidence does not identify LogicalActorHost and exact anchor.");
                Require(SamePose(logicalActorHost.transform, anchor), "LogicalActorHost did not receive anchor world pose.");
                Require(ReferenceEquals(logicalActorHost.transform.parent, parent), "Placement reparented LogicalActorHost.");
                Require(logicalActorHost.transform.localScale == scale, "Placement changed LogicalActorHost scale.");
                Require(localPlayerHost.transform.position == hostPosition && SameRotation(localPlayerHost.transform.rotation, hostRotation),
                    "Placement moved or rotated the Local Player Host instead of the LogicalActorHost.");
            });
        }

        private static void ProveMissingBindingNoFallback(ActivityAsset activity, PlayerSlotProfile p1, PlayerSlotProfile p2)
        {
            WithScenes(activity, (owned, foreign, context) =>
            {
                CreatePlacementAuthoring(owned, p2, new Vector3(9f, 0f, 9f), Quaternion.identity);
                GameObject target = CreateInScene(foreign, "Target");
                target.transform.position = new Vector3(4f, 5f, 6f);
                Vector3 before = target.transform.position;

                bool applied = ActivityPlayerInitialPlacementRuntime.TryApplyRequiredPlacement(
                    context, PlayerSlotId.Player1, ActorId.From("qa.adr21.missing"), "qa:missing",
                    target.transform, out ActivityPlayerInitialPlacementEvidence evidence, out string issue);
                Require(!applied, "Missing P1 binding unexpectedly consumed another Slot binding.");
                Require(target.transform.position == before, "Failed placement changed target pose.");
                Require(evidence.Status == ActivityPlayerInitialPlacementStatus.Failed, "Missing binding did not produce Failed evidence.");
                Require(issue.Contains("exactly one binding") && issue.Contains("Found '0'"),
                    "Missing binding did not fail explicitly without fallback. " + issue);
            });
        }

        private static void ProveDuplicateExactSlotRejected(ActivityAsset activity, PlayerSlotProfile p1)
        {
            WithScenes(activity, (owned, foreign, context) =>
            {
                CreatePlacementAuthoring(owned, p1, Vector3.one, Quaternion.identity);
                CreatePlacementAuthoring(owned, p1, Vector3.one * 2f, Quaternion.identity);
                GameObject target = CreateInScene(foreign, "Target");
                bool applied = ActivityPlayerInitialPlacementRuntime.TryApplyRequiredPlacement(
                    context, PlayerSlotId.Player1, ActorId.From("qa.adr21.duplicate"), "qa:duplicate",
                    target.transform, out _, out string issue);
                Require(!applied && issue.Contains("duplicate bindings"),
                    "Duplicate exact-slot bindings were not rejected. " + issue);
            });
        }

        private static void ProveForeignSceneIgnored(ActivityAsset activity, PlayerSlotProfile p1)
        {
            WithScenes(activity, (owned, foreign, context) =>
            {
                CreatePlacementAuthoring(foreign, p1, new Vector3(40f, 0f, 0f), Quaternion.identity);
                GameObject target = CreateInScene(foreign, "Target");
                bool applied = ActivityPlayerInitialPlacementRuntime.TryApplyRequiredPlacement(
                    context, PlayerSlotId.Player1, ActorId.From("qa.adr21.foreign"), "qa:foreign",
                    target.transform, out _, out string issue);
                Require(!applied && issue.Contains("Found '0'"),
                    "Authoring outside ActivityOwnedScenes was discovered. " + issue);
            });
        }

        private static void ProveAnchorOutsideOwnedSceneRejected(ActivityAsset activity, PlayerSlotProfile p1)
        {
            WithScenes(activity, (owned, foreign, context) =>
            {
                GameObject authoringRoot = CreateInScene(owned, "PlacementAuthoring");
                ActivityPlayerInitialPlacementAuthoring authoring = authoringRoot.AddComponent<ActivityPlayerInitialPlacementAuthoring>();
                GameObject foreignAnchor = CreateInScene(foreign, "ForeignAnchor");
                SetBindings(authoring, new BindingSpec(p1, foreignAnchor.transform));
                GameObject target = CreateInScene(foreign, "Target");
                bool applied = ActivityPlayerInitialPlacementRuntime.TryApplyRequiredPlacement(
                    context, PlayerSlotId.Player1, ActorId.From("qa.adr21.anchor-scope"), "qa:anchor-scope",
                    target.transform, out _, out string issue);
                Require(!applied && issue.Contains("outside its canonical Activity-owned scene"),
                    "Cross-scene anchor was not rejected. " + issue);
            });
        }

        private static void ProvePreserveAuthoredPose(ActivityAsset activity, PlayerSlotProfile p1)
        {
            WithScenes(activity, (owned, foreign, context) =>
            {
                GameObject target = CreateInScene(foreign, "SceneProvidedActor");
                target.transform.SetPositionAndRotation(new Vector3(-8f, 2f, 11f), Quaternion.Euler(12f, 34f, 56f));
                Vector3 beforePosition = target.transform.position;
                Quaternion beforeRotation = target.transform.rotation;
                bool applied = ActivityPlayerInitialPlacementRuntime.TryApplyScenePolicy(
                    context, PlayerSlotId.Player1, ActorId.From("qa.adr21.preserve"), "qa:scene:preserve",
                    SceneProvidedPlayerInitialPlacementPolicy.PreserveAuthoredPose, target.transform,
                    out ActivityPlayerInitialPlacementEvidence evidence, out string issue);
                Require(applied, "PreserveAuthoredPose failed. " + issue);
                Require(evidence.Status == ActivityPlayerInitialPlacementStatus.Preserved && evidence.Anchor == null,
                    "PreserveAuthoredPose produced incorrect evidence.");
                Require(target.transform.position == beforePosition && SameRotation(target.transform.rotation, beforeRotation),
                    "PreserveAuthoredPose changed the authored pose.");
            });
        }

        private static void ProveSceneApplyPlacement(ActivityAsset activity, PlayerSlotProfile p1)
        {
            WithScenes(activity, (owned, foreign, context) =>
            {
                Transform anchor = CreatePlacementAuthoring(owned, p1, new Vector3(-15f, 1.5f, 22f), Quaternion.Euler(0f, 270f, 0f));
                GameObject parent = CreateInScene(foreign, "SceneHost");
                GameObject target = new GameObject("SceneProvidedActor");
                SceneManager.MoveGameObjectToScene(target, foreign);
                target.transform.SetParent(parent.transform, true);
                target.transform.localScale = new Vector3(0.8f, 1.2f, 1.4f);
                Vector3 scale = target.transform.localScale;
                Transform originalParent = target.transform.parent;

                bool applied = ActivityPlayerInitialPlacementRuntime.TryApplyScenePolicy(
                    context, PlayerSlotId.Player1, ActorId.From("qa.adr21.scene-apply"), "qa:scene:apply",
                    SceneProvidedPlayerInitialPlacementPolicy.ApplyActivityPlacement, target.transform,
                    out ActivityPlayerInitialPlacementEvidence evidence, out string issue);
                Require(applied, "ApplyActivityPlacement failed. " + issue);
                Require(evidence.Status == ActivityPlayerInitialPlacementStatus.Applied && ReferenceEquals(evidence.Anchor, anchor),
                    "Scene Apply did not publish exact Applied evidence.");
                Require(SamePose(target.transform, anchor), "Scene-Provided Actor did not receive placement pose.");
                Require(ReferenceEquals(target.transform.parent, originalParent) && target.transform.localScale == scale,
                    "Scene Apply changed hierarchy or scale.");
            });
        }

        private static void ProveFailedPlacementEvidence(ActivityAsset activity, PlayerSlotProfile p1)
        {
            WithScenes(activity, (owned, foreign, context) =>
            {
                GameObject target = CreateInScene(foreign, "Target");
                target.transform.position = new Vector3(8f, 8f, 8f);
                bool applied = ActivityPlayerInitialPlacementRuntime.TryApplyRequiredPlacement(
                    context, PlayerSlotId.Player1, ActorId.From("qa.adr21.failure"), "qa:failure",
                    target.transform, out ActivityPlayerInitialPlacementEvidence evidence, out string issue);
                Require(!applied, "Placement unexpectedly succeeded without authoring.");
                Require(evidence.Status == ActivityPlayerInitialPlacementStatus.Failed,
                    "Placement failure did not publish Failed evidence.");
                Require(evidence.Owner == context.Owner &&
                        evidence.Occurrence.Matches(activity, context.Occurrence.TransitionSequence) &&
                        evidence.PlayerSlotId == PlayerSlotId.Player1,
                    "Failure evidence lost owner/occurrence/Slot correlation.");
                Require(!string.IsNullOrEmpty(issue) && evidence.Diagnostic == issue,
                    "Failure evidence did not preserve the blocking diagnostic.");
            });
        }

        private static void WithScenes(
            ActivityAsset activity,
            Action<Scene, Scene, ActivityTransitionPreparationContext> proof,
            int transitionSequence = 77)
        {
            Scene owned = default;
            Scene foreign = default;
            Scene previousActiveScene = SceneManager.GetActiveScene();
            string temporaryRoot = null;
            Exception executionFailure = null;
            var cleanupFailures = new List<Exception>();
            try
            {
                string temporaryId = Guid.NewGuid().ToString("N");
                temporaryRoot = "Assets/ImmersiveFrameworkQA/__Adr021InitialPlacement_" + temporaryId;
                Require(
                    AssetDatabase.CreateFolder(
                        "Assets/ImmersiveFrameworkQA",
                        "__Adr021InitialPlacement_" + temporaryId).Length > 0,
                    "ADR-021 could not create its temporary Editor scene root.");

                owned = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                Require(
                    EditorSceneManager.SaveScene(owned, temporaryRoot + "/Owned.unity"),
                    "ADR-021 could not save its Activity-owned scene.");
                foreign = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                Require(
                    EditorSceneManager.SaveScene(foreign, temporaryRoot + "/Foreign.unity"),
                    "ADR-021 could not save its foreign scene.");
                ActivityTransitionPreparationContext context = CreateContext(activity, owned, transitionSequence);
                proof(owned, foreign, context);
            }
            catch (Exception exception)
            {
                executionFailure = exception;
                throw;
            }
            finally
            {
                try
                {
                    if (foreign.IsValid() && foreign.isLoaded) EditorSceneManager.CloseScene(foreign, true);
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }

                try
                {
                    if (owned.IsValid() && owned.isLoaded) EditorSceneManager.CloseScene(owned, true);
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }

                try
                {
                    if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                    {
                        SceneManager.SetActiveScene(previousActiveScene);
                    }
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }

                try
                {
                    if (!string.IsNullOrEmpty(temporaryRoot) && AssetDatabase.IsValidFolder(temporaryRoot))
                    {
                        AssetDatabase.DeleteAsset(temporaryRoot);
                        AssetDatabase.Refresh();
                    }
                }
                catch (Exception exception)
                {
                    cleanupFailures.Add(exception);
                }

                if (cleanupFailures.Count > 0)
                {
                    string cleanupIssue = string.Join(" | ", cleanupFailures.ConvertAll(
                        exception => exception.GetType().Name + ": " + exception.Message));
                    Debug.LogError($"{Prefix} status='Cleanup Failed' error='{Escape(cleanupIssue)}'.");
                    if (executionFailure == null)
                    {
                        throw new InvalidOperationException(cleanupIssue, cleanupFailures[0]);
                    }
                }
            }
        }

        private static ActivityTransitionPreparationContext CreateContext(
            ActivityAsset activity,
            Scene owned,
            int transitionSequence)
        {
            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                activity.ActivityId.StableText,
                activity.ActivityName,
                RuntimeDefinitionToken.FromUnityObject(activity));
            var occurrence = new ActivityReadinessOccurrence(activity, transitionSequence);
            var discoveryScene = new ActivityContentDiscoveryScene(activity, owned.path, owned.name);
            var discoveryScope = new ActivityContentDiscoveryScope(default, new[] { discoveryScene });
            var context = new ActivityTransitionPreparationContext(activity, owner, occurrence, discoveryScope);
            Require(context.IsValid, "Synthetic Activity transition preparation context is invalid.");
            return context;
        }

        private static Transform CreatePlacementAuthoring(
            Scene scene,
            PlayerSlotProfile slotProfile,
            Vector3 position,
            Quaternion rotation)
        {
            GameObject root = CreateInScene(scene, "ActivityPlayerInitialPlacement");
            ActivityPlayerInitialPlacementAuthoring authoring = root.AddComponent<ActivityPlayerInitialPlacementAuthoring>();
            GameObject anchorObject = new GameObject("PlacementAnchor");
            SceneManager.MoveGameObjectToScene(anchorObject, scene);
            anchorObject.transform.SetParent(root.transform, true);
            anchorObject.transform.SetPositionAndRotation(position, rotation);
            SetBindings(authoring, new BindingSpec(slotProfile, anchorObject.transform));
            return anchorObject.transform;
        }

        private static GameObject CreateInScene(Scene scene, string name)
        {
            GameObject gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            return gameObject;
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

        private static void SetBindings(
            ActivityPlayerInitialPlacementAuthoring authoring,
            params BindingSpec[] specs)
        {
            var serialized = new SerializedObject(authoring);
            SerializedProperty bindings = serialized.FindProperty("bindings");
            Require(bindings != null, "Serialized initial-placement field 'bindings' was not found.");
            bindings.arraySize = specs?.Length ?? 0;
            for (int index = 0; index < bindings.arraySize; index++)
            {
                SerializedProperty element = bindings.GetArrayElementAtIndex(index);
                SerializedProperty slot = element.FindPropertyRelative("playerSlotProfile");
                SerializedProperty anchor = element.FindPropertyRelative("placementAnchor");
                Require(slot != null && anchor != null,
                    $"Serialized placement binding fields were not found at index '{index}'.");
                slot.objectReferenceValue = specs[index].SlotProfile;
                anchor.objectReferenceValue = specs[index].Anchor;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();
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

        private static bool SamePose(Transform left, Transform right) =>
            Vector3.Distance(left.position, right.position) <= 0.0001f &&
            SameRotation(left.rotation, right.rotation);

        private static bool SameRotation(Quaternion left, Quaternion right) =>
            Quaternion.Angle(left, right) <= 0.01f;

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
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "''").Replace("\r", " ").Replace("\n", " ");
    }
}

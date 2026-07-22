using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using ImmersiveFrameworkQA.Hub;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace ImmersiveFrameworkQA.Camera.Scripts.Editor
{
    internal static class QaCameraOverrideAuthoritySceneInstaller
    {
        private const string Root = "Assets/ImmersiveFrameworkQA";
        private const string CameraRoot = Root + "/Camera";
        private const string ScenePath =
            CameraRoot + "/Scenes/QA_PlayerCameraArbitration.unity";
        private const string RoutePath =
            CameraRoot + "/Routes/QA_PlayerCameraArbitrationRoute.asset";
        private const string ActivityPath =
            CameraRoot + "/Activities/QA_PlayerCameraArbitrationActivity.asset";
        private const string HubScenePath =
            Root + "/Hub/Scenes/QA_Hub.unity";
        private const string HubRoutePath =
            Root + "/Hub/Routes/QA_HubRoute.asset";
        private const string HubLabel = " Camera Override Authority";
        private const string HubTriggerName =
            "RouteTrigger_Camera__Override_Authority";
        private const string CoordinatorName =
            "QA__RouteCompletionCoordinator";

        internal static void Install()
        {
            RepairAssets();
            RepairScene();
            EnsureBuildScene();
            RepairHub();
        }

        private static void RepairAssets()
        {
            ActivityAsset activity = LoadOrCreate<ActivityAsset>(ActivityPath);
            Set(activity, "activityName",
                "QA Camera Override Authority Activity");
            Set(activity, "activityId",
                "qa.camera.override.authority.activity");
            Set(activity, "description",
                "Explicit Activity override used by the  authority proof.");

            RouteAsset route = LoadOrCreate<RouteAsset>(RoutePath);
            Set(route, "routeName", "QA  Camera Override Authority");
            Set(route, "primaryScenePath", ScenePath);
            Set(route, "primarySceneName",
                Path.GetFileNameWithoutExtension(ScenePath));
            Set(route, "startupActivity", activity);
            Set(route, "description",
                " authority proof for synthetic LocalPlayer, Activity, Route and Session camera requests.");
        }

        private static void RepairScene()
        {
            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

            RemoveLocalOutputAndLegacyRoots(scene);

            RouteAsset route = Require<RouteAsset>(RoutePath);
            ActivityAsset activity = Require<ActivityAsset>(ActivityPath);
            RouteAsset hubRoute = Require<RouteAsset>(HubRoutePath);

            Transform routeTarget = Target(
                scene,
                "QA_RouteTarget",
                new Vector3(0f, 1f, 0f));
            Transform playerTarget = Target(
                scene,
                "QA_PlayerTarget",
                new Vector3(2f, 1f, 0f));
            Transform playerLookAt = Target(
                scene,
                "QA_PlayerLookAt",
                new Vector3(2f, 1.5f, 1f));
            Transform activityTarget = Target(
                scene,
                "QA_ActivityTarget",
                new Vector3(-2f, 1f, 0f));

            CameraRigComposer routeRig = Composer(
                scene,
                "QA_RouteRig",
                "Route Cinemachine Camera",
                routeTarget,
                routeTarget,
                "qa.route-target");
            CameraRigComposer playerRig = Composer(
                scene,
                "QA_PlayerRig",
                "Player Cinemachine Camera",
                playerTarget,
                playerLookAt,
                "qa.player-target");
            CameraRigComposer activityRig = Composer(
                scene,
                "QA_ActivityRig",
                "Activity Cinemachine Camera",
                activityTarget,
                activityTarget,
                "qa.activity-target");

            GameObject routeRoot = RootObject(
                scene,
                "QA_RouteContent");
            RouteContentBinding routeContent =
                Component<RouteContentBinding>(routeRoot);
            Set(routeContent, "route", route);
            Set(routeContent, "localContentId", "qa.route-content");

            RouteCameraOverrideBinding routeBinding =
                Component<RouteCameraOverrideBinding>(routeRoot);
            Configure(
                routeBinding,
                "assignedRoute",
                route,
                "qa.route",
                "qa.camera.request.route",
                routeRig,
                routeTarget,
                200,
                "route");

            GameObject playerRoot = RootObject(
                scene,
                "QA_LocalPlayer");
            RemoveLegacyPlayerComponents(playerRoot);
            QaLocalPlayerCameraRequestBinding playerBinding =
                Component<QaLocalPlayerCameraRequestBinding>(playerRoot);
            Set(playerBinding, "ownerId", "qa.player");
            Set(playerBinding, "eligibilityScopeId",
                "qa.player.eligibility");
            Set(playerBinding, "requestId",
                "qa.camera.request.player");
            Set(playerBinding, "outputSession", null);
            Set(playerBinding, "rigComposer", playerRig);
            Set(playerBinding, "precedence", 50);
            Set(playerBinding, "tieBreakerId", "player");
            Set(playerBinding, "eligibleOnEnable", true);
            Set(playerBinding, "releaseOnDisable", true);
            Set(playerBinding, "logDiagnostics", true);
            RemoveOtherPlayers(scene, playerBinding);

            GameObject activityRoot = Child(
                routeRoot.transform,
                "QA__ActivityContent");
            ActivityLocalVisibilityAdapter adapter =
                Component<ActivityLocalVisibilityAdapter>(activityRoot);
            Set(adapter, "activity", activity);
            Set(adapter, "localContentId", "qa.activity-content");

            ActivityCameraOverrideBinding activityBinding =
                Component<ActivityCameraOverrideBinding>(activityRoot);
            Configure(
                activityBinding,
                "assignedActivity",
                activity,
                "qa.activity",
                "qa.camera.request.activity",
                activityRig,
                activityTarget,
                100,
                "activity");

            GameObject controls = RootObject(scene, "QA__Controls");
            ActivityRequestTrigger activityTrigger =
                Component<ActivityRequestTrigger>(controls);
            activityTrigger.TargetActivity = activity;
            Set(activityTrigger, "reason",
                "qa.activity.lifecycle-cleanup");

            RouteRequestTrigger backTrigger =
                Component<RouteRequestTrigger>(controls);
            backTrigger.TargetRoute = hubRoute;
            Set(backTrigger, "reason",
                "qa.route.lifecycle-cleanup");

            QaCameraOverrideAuthorityFixture fixture =
                Component<QaCameraOverrideAuthorityFixture>(controls);
            Set(fixture, "routeBinding", routeBinding);
            Set(fixture, "playerBinding", playerBinding);
            Set(fixture, "activityBinding", activityBinding);
            Set(fixture, "routeComposer", routeRig);
            Set(fixture, "playerComposer", playerRig);
            Set(fixture, "activityComposer", activityRig);
            Set(fixture, "activityRequestTrigger", activityTrigger);
            Set(fixture, "backToHubTrigger", backTrigger);
            Set(fixture, "throwOnFailure", false);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static CameraRigComposer Composer(
            Scene scene,
            string rootName,
            string cameraName,
            Transform followTarget,
            Transform lookAtTarget,
            string logicalSourceId)
        {
            GameObject root = RootObject(scene, rootName);
            ExplicitCameraTargetSourceAuthoring source =
                Component<ExplicitCameraTargetSourceAuthoring>(root);
            Set(source, "logicalSourceId", logicalSourceId);
            Set(source, "followTarget", followTarget);
            Set(source, "lookAtTarget", lookAtTarget);

            CameraRigComposer composer = Component<CameraRigComposer>(root);
            CinemachineCamera camera = Component<CinemachineCamera>(
                Child(root.transform, cameraName));
            camera.enabled = false;

            Set(composer, "presentationIntent",
                (int)CameraRigPresentationIntent.Follow);
            Set(composer, "targetSource", source);
            Set(composer, "followRequirement",
                (int)CameraTargetRequirement.Required);
            Set(composer, "lookAtRequirement",
                (int)CameraTargetRequirement.Optional);
            Set(composer, "cinemachineCamera", camera);
            Set(composer, "createCinemachineCameraIfMissing", false);
            Set(composer, "logApplyRebuildDiagnostics", false);

            CameraRigComposerApplyRebuildResult result =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                    composer,
                    false,
                    false);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not materialize  rig '{rootName}'. {result.BlockingIssue}");
            }

            camera.enabled = false;
            return composer;
        }

        private static void Configure(
            ScopedCameraOverrideBinding binding,
            string ownerProperty,
            UnityEngine.Object owner,
            string scope,
            string request,
            CameraRigComposer rig,
            Transform target,
            int precedence,
            string tieBreaker)
        {
            Set(binding, ownerProperty, owner);
            Set(binding, "scopeId", scope);
            Set(binding, "requestId", request);
            Set(binding, "rigComposer", rig);
            Set(binding, "targetSource", target);
            Set(binding, "precedence", precedence);
            Set(binding, "tieBreakerId", tieBreaker);
            Set(binding, "logDiagnostics", true);
        }

        private static void RepairHub()
        {
            Scene scene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
            QaHubPanel panel = Single<QaHubPanel>(scene);
            GameObject triggerObject =
                Find(scene, HubTriggerName) ??
                Find(scene, "RouteTrigger_Camera_C9L_Player_Arbitration") ??
                new GameObject(HubTriggerName);
            triggerObject.name = HubTriggerName;
            triggerObject.transform.SetParent(panel.transform, false);

            RouteRequestTrigger trigger =
                Component<RouteRequestTrigger>(triggerObject);
            trigger.TargetRoute = Require<RouteAsset>(RoutePath);
            Set(trigger, "reason",
                "qa.hub.route.camera_override_authority");

            GameObject coordinatorObject =
                Find(scene, CoordinatorName) ??
                Find(scene, "QA_C9L_RouteCompletionCoordinator") ??
                new GameObject(CoordinatorName);
            coordinatorObject.name = CoordinatorName;
            coordinatorObject.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(coordinatorObject, scene);
            QaCameraOverrideAuthorityCompletionCoordinator coordinator =
                Component<QaCameraOverrideAuthorityCompletionCoordinator>(
                    coordinatorObject);
            Set(coordinator, "routeTrigger", trigger);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == coordinatorObject)
                {
                    continue;
                }

                if (root.name == "QA_C9L_RouteCompletionCoordinator" ||
                    root.name == CoordinatorName)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            var so = new SerializedObject(panel);
            so.Update();
            SerializedProperty entries = Property(so, "entries");
            int retained = -1;
            for (int index = entries.arraySize - 1; index >= 0; index--)
            {
                string label = entries.GetArrayElementAtIndex(index)
                    .FindPropertyRelative("label").stringValue;
                if (label != HubLabel &&
                    label != "Camera /  Override Authority" &&
                    label != "C9L Player Camera Arbitration")
                {
                    continue;
                }

                if (retained < 0)
                {
                    retained = index;
                    continue;
                }

                entries.DeleteArrayElementAtIndex(index);
                if (index < retained)
                {
                    retained--;
                }
            }

            if (retained < 0)
            {
                retained = entries.arraySize;
                entries.arraySize++;
            }

            SerializedProperty entry =
                entries.GetArrayElementAtIndex(retained);
            entry.FindPropertyRelative("label").stringValue = HubLabel;
            entry.FindPropertyRelative("routeRequestTrigger")
                .objectReferenceValue = trigger;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
            EditorSceneManager.SaveScene(scene, HubScenePath);
        }

        private static void RemoveLocalOutputAndLegacyRoots(Scene scene)
        {
            var remove = new HashSet<GameObject>();
            foreach (CameraOutputSessionBinding item in
                     All<CameraOutputSessionBinding>(scene))
            {
                remove.Add(item.gameObject);
            }
            foreach (CinemachineBrain item in All<CinemachineBrain>(scene))
            {
                remove.Add(item.gameObject);
            }
            foreach (UnityEngine.Camera item in All<UnityEngine.Camera>(scene))
            {
                remove.Add(item.gameObject);
            }
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("QA_C9L_", StringComparison.Ordinal))
                {
                    remove.Add(root);
                }
            }
            foreach (GameObject item in remove)
            {
                UnityEngine.Object.DestroyImmediate(item);
            }
        }

        private static void RemoveLegacyPlayerComponents(GameObject root)
        {
            const string LegacyCameraBinding =
                "Immersive.Framework.Camera.LocalPlayerCameraRequestBinding";
            const string PlayerInput = "UnityEngine.InputSystem.PlayerInput";

            RemoveComponentsByTypeName(root, LegacyCameraBinding);
            RemoveMissingScriptsRecursively(root);
            RemoveComponentsByTypeName(root, PlayerInput);

            RequireComponentAbsent(root, LegacyCameraBinding);
            RequireNoMissingScripts(root);
            RequireComponentAbsent(root, PlayerInput);
        }

        private static void RemoveMissingScriptsRecursively(GameObject root)
        {
            foreach (Transform item in
                     root.GetComponentsInChildren<Transform>(true))
            {
                GameObject target = item.gameObject;
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        target) <= 0)
                {
                    continue;
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(target);
                EditorUtility.SetDirty(target);
            }
        }

        private static void RequireNoMissingScripts(GameObject root)
        {
            foreach (Transform item in
                     root.GetComponentsInChildren<Transform>(true))
            {
                int count =
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        item.gameObject);
                if (count > 0)
                {
                    throw new InvalidOperationException(
                        $" scene repair left '{count}' Missing Script " +
                        $"component(s) on '{item.name}'.");
                }
            }
        }

        private static void RemoveComponentsByTypeName(
            GameObject root,
            string componentTypeName)
        {
            while (true)
            {
                Component selected = null;
                foreach (Component component in root.GetComponents<Component>())
                {
                    if (component == null)
                    {
                        continue;
                    }

                    string fullName =
                        component.GetType().FullName ?? string.Empty;
                    if (string.Equals(
                            fullName,
                            componentTypeName,
                            StringComparison.Ordinal))
                    {
                        selected = component;
                        break;
                    }
                }

                if (selected == null)
                {
                    return;
                }

                UnityEngine.Object.DestroyImmediate(selected);
            }
        }

        private static void RequireComponentAbsent(
            GameObject root,
            string componentTypeName)
        {
            foreach (Component component in root.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                string fullName = component.GetType().FullName ?? string.Empty;
                if (string.Equals(
                        fullName,
                        componentTypeName,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $" scene repair could not remove legacy component " +
                        $"'{componentTypeName}' from '{root.name}'.");
                }
            }
        }

        private static void RemoveOtherPlayers(
            Scene scene,
            QaLocalPlayerCameraRequestBinding selected)
        {
            foreach (QaLocalPlayerCameraRequestBinding item in
                     All<QaLocalPlayerCameraRequestBinding>(scene))
            {
                if (item != selected)
                {
                    UnityEngine.Object.DestroyImmediate(item);
                }
            }
        }

        private static Transform Target(
            Scene scene,
            string name,
            Vector3 position)
        {
            GameObject item = RootObject(scene, name);
            item.transform.position = position;
            return item.transform;
        }

        private static GameObject RootObject(Scene scene, string name)
        {
            GameObject item = Find(scene, name);
            if (item != null)
            {
                return item;
            }

            item = new GameObject(name);
            SceneManager.MoveGameObjectToScene(item, scene);
            return item;
        }

        private static GameObject Child(Transform parent, string name)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                if (parent.GetChild(index).name == name)
                {
                    return parent.GetChild(index).gameObject;
                }
            }

            var item = new GameObject(name);
            item.transform.SetParent(parent, false);
            return item;
        }

        private static T Component<T>(GameObject item)
            where T : Component
        {
            T component = item.GetComponent<T>();
            return component != null ? component : item.AddComponent<T>();
        }

        private static List<T> All<T>(Scene scene)
            where T : Component
        {
            var result = new List<T>();
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
            {
                if (item != null && item.gameObject.scene == scene)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        private static T Single<T>(Scene scene)
            where T : Component
        {
            List<T> all = All<T>(scene);
            if (all.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one {typeof(T).Name} in '{scene.name}', found '{all.Count}'.");
            }
            return all[0];
        }

        private static GameObject Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
                foreach (Transform child in
                         root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }
            return null;
        }

        private static void EnsureBuildScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value != null)
            {
                return value;
            }
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        private static T Require<T>(string path)
            where T : UnityEngine.Object
        {
            return AssetDatabase.LoadAssetAtPath<T>(path) ??
                throw new InvalidOperationException(
                    $"Required asset is missing at '{path}'.");
        }

        private static void Set(
            UnityEngine.Object target,
            string property,
            object value)
        {
            var so = new SerializedObject(target);
            so.Update();
            SerializedProperty item = Property(so, property);
            if (value == null)
            {
                item.objectReferenceValue = null;
            }
            else if (value is UnityEngine.Object reference)
            {
                item.objectReferenceValue = reference;
            }
            else if (value is string text)
            {
                item.stringValue = text;
            }
            else if (value is int number)
            {
                item.intValue = number;
            }
            else if (value is bool flag)
            {
                item.boolValue = flag;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported value for '{property}'.");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static SerializedProperty Property(
            SerializedObject serialized,
            string property)
        {
            return serialized.FindProperty(property) ??
                throw new InvalidOperationException(
                    $"Serialized property '{property}' was not found on '{serialized.targetObject.GetType().Name}'.");
        }
    }
}

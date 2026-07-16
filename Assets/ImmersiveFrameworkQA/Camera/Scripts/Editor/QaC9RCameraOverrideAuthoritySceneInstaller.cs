using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerAuthoring;
using Immersive.Framework.RouteLifecycle;
using ImmersiveFrameworkQA.Hub;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    internal static class QaC9RCameraOverrideAuthoritySceneInstaller
    {
        private const string Root = "Assets/ImmersiveFrameworkQA";
        private const string CameraRoot = Root + "/Camera";
        private const string ScenePath = CameraRoot + "/Scenes/QA_PlayerCameraArbitration.unity";
        private const string RoutePath = CameraRoot + "/Routes/QA_PlayerCameraArbitrationRoute.asset";
        private const string ActivityPath = CameraRoot + "/Activities/QA_PlayerCameraArbitrationActivity.asset";
        private const string HubScenePath = Root + "/Hub/Scenes/QA_Hub.unity";
        private const string HubRoutePath = Root + "/Hub/Routes/QA_HubRoute.asset";
        private const string HubLabel = "C9R Camera Override Authority";
        private const string HubTriggerName = "RouteTrigger_Camera_C9R_Override_Authority";
        private const string CoordinatorName = "QA_C9R_RouteCompletionCoordinator";

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
            Set(activity, "activityName", "QA C9R Camera Override Authority Activity");
            Set(activity, "description", "Explicit Activity override used by the C9R authority proof.");
            RouteAsset route = LoadOrCreate<RouteAsset>(RoutePath);
            Set(route, "routeName", "QA C9R Camera Override Authority");
            Set(route, "primaryScenePath", ScenePath);
            Set(route, "primarySceneName", Path.GetFileNameWithoutExtension(ScenePath));
            Set(route, "startupActivity", activity);
            Set(route, "description", "C9R authority proof for Player, Activity, Route and Session camera overrides.");
        }

        private static void RepairScene()
        {
            Scene scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RemoveLocalOutputAndLegacyRoots(scene);
            RouteAsset route = Require<RouteAsset>(RoutePath);
            ActivityAsset activity = Require<ActivityAsset>(ActivityPath);
            RouteAsset hubRoute = Require<RouteAsset>(HubRoutePath);
            Transform routeTarget = Target(scene, "QA_C9R_RouteTarget", new Vector3(0f, 1f, 0f));
            Transform playerTarget = Target(scene, "QA_C9R_PlayerTarget", new Vector3(2f, 1f, 0f));
            Transform playerLookAt = Target(scene, "QA_C9R_PlayerLookAt", new Vector3(2f, 1.5f, 1f));
            Transform activityTarget = Target(scene, "QA_C9R_ActivityTarget", new Vector3(-2f, 1f, 0f));
            CameraRigComposer routeRig = Composer(scene, "QA_C9R_RouteRig", "Route Cinemachine Camera");
            CameraRigComposer playerRig = Composer(scene, "QA_C9R_PlayerRig", "Player Cinemachine Camera");
            CameraRigComposer activityRig = Composer(scene, "QA_C9R_ActivityRig", "Activity Cinemachine Camera");

            GameObject routeRoot = RootObject(scene, "QA_C9R_RouteContent");
            RouteContentBinding routeContent = Component<RouteContentBinding>(routeRoot);
            Set(routeContent, "route", route); Set(routeContent, "localContentId", "qa.c9r.route-content");
            RouteCameraOverrideBinding routeBinding = Component<RouteCameraOverrideBinding>(routeRoot);
            Configure(routeBinding, "assignedRoute", route, "qa.c9r.route", "qa.camera.request.c9r.route", routeRig, routeTarget, 200, "route");

            GameObject playerRoot = RootObject(scene, "QA_C9R_LocalPlayer");
            PreAuthoredPlayerComposer player = Component<PreAuthoredPlayerComposer>(playerRoot);
            Set(player, "actorId", "qa.player.actor.c9r");
            Set(player, "playerInput", Component<PlayerInput>(playerRoot)); Set(player, "cameraBindingRequired", true);
            Set(player, "cameraTarget", playerTarget); Set(player, "lookAtTarget", playerLookAt);
            LocalPlayerCameraRequestBinding playerBinding = Component<LocalPlayerCameraRequestBinding>(playerRoot);
            Set(playerBinding, "playerComposer", player); Set(playerBinding, "eligibilityScopeId", "qa.c9r.player.eligibility");
            Set(playerBinding, "requestId", "qa.camera.request.c9r.player"); Set(playerBinding, "outputSession", null);
            Set(playerBinding, "rigComposer", playerRig); Set(playerBinding, "precedence", 50); Set(playerBinding, "tieBreakerId", "player");
            Set(playerBinding, "eligibleOnEnable", true); Set(playerBinding, "releaseOnDisable", true); Set(playerBinding, "logDiagnostics", true);
            RemoveOtherPlayers(scene, playerBinding);

            GameObject activityRoot = Child(routeRoot.transform, "QA_C9R_ActivityContent");
            ActivityLocalVisibilityAdapter adapter = Component<ActivityLocalVisibilityAdapter>(activityRoot);
            Set(adapter, "activity", activity); Set(adapter, "localContentId", "qa.c9r.activity-content");
            ActivityCameraOverrideBinding activityBinding = Component<ActivityCameraOverrideBinding>(activityRoot);
            Configure(activityBinding, "assignedActivity", activity, "qa.c9r.activity", "qa.camera.request.c9r.activity", activityRig, activityTarget, 100, "activity");

            GameObject controls = RootObject(scene, "QA_C9R_Controls");
            ActivityRequestTrigger activityTrigger = Component<ActivityRequestTrigger>(controls); activityTrigger.TargetActivity = activity; Set(activityTrigger, "reason", "qa.c9r.activity.lifecycle-cleanup");
            RouteRequestTrigger backTrigger = Component<RouteRequestTrigger>(controls); backTrigger.TargetRoute = hubRoute; Set(backTrigger, "reason", "qa.c9r.route.lifecycle-cleanup");
            QaC9RCameraOverrideAuthorityFixture fixture = Component<QaC9RCameraOverrideAuthorityFixture>(controls);
            Set(fixture, "routeBinding", routeBinding); Set(fixture, "playerBinding", playerBinding); Set(fixture, "activityBinding", activityBinding);
            Set(fixture, "routeComposer", routeRig); Set(fixture, "playerComposer", playerRig); Set(fixture, "activityComposer", activityRig);
            Set(fixture, "activityRequestTrigger", activityTrigger); Set(fixture, "backToHubTrigger", backTrigger); Set(fixture, "throwOnFailure", false);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void RepairHub()
        {
            Scene scene = EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
            QaHubPanel panel = Single<QaHubPanel>(scene);
            GameObject triggerObject = Find(scene, HubTriggerName) ?? Find(scene, "RouteTrigger_Camera_C9L_Player_Arbitration") ?? new GameObject(HubTriggerName);
            triggerObject.name = HubTriggerName; triggerObject.transform.SetParent(panel.transform, false);
            RouteRequestTrigger trigger = Component<RouteRequestTrigger>(triggerObject);
            trigger.TargetRoute = Require<RouteAsset>(RoutePath); Set(trigger, "reason", "qa.hub.route.camera_override_authority");

            GameObject coordinatorObject =
                Find(scene, CoordinatorName) ??
                Find(scene, "QA_C9L_RouteCompletionCoordinator") ??
                new GameObject(CoordinatorName);
            coordinatorObject.name = CoordinatorName;
            coordinatorObject.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(coordinatorObject, scene);
            QaC9RCameraOverrideAuthorityCompletionCoordinator coordinator =
                Component<QaC9RCameraOverrideAuthorityCompletionCoordinator>(coordinatorObject);
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

            var so = new SerializedObject(panel); so.Update(); SerializedProperty entries = Property(so, "entries"); int retained = -1;
            for (int i = entries.arraySize - 1; i >= 0; i--)
            {
                string label = entries.GetArrayElementAtIndex(i).FindPropertyRelative("label").stringValue;
                if (label != HubLabel && label != "Camera / C9R Override Authority" && label != "C9L Player Camera Arbitration") continue;
                if (retained < 0) { retained = i; continue; } entries.DeleteArrayElementAtIndex(i); if (i < retained) retained--;
            }
            if (retained < 0) { retained = entries.arraySize; entries.arraySize++; }
            SerializedProperty entry = entries.GetArrayElementAtIndex(retained); entry.FindPropertyRelative("label").stringValue = HubLabel; entry.FindPropertyRelative("routeRequestTrigger").objectReferenceValue = trigger;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(panel); EditorSceneManager.SaveScene(scene, HubScenePath);
        }

        private static void Configure(ScopedCameraOverrideBinding binding, string ownerProperty, UnityEngine.Object owner, string scope, string request, CameraRigComposer rig, Transform target, int precedence, string tieBreaker)
        {
            Set(binding, ownerProperty, owner); Set(binding, "scopeId", scope); Set(binding, "requestId", request); Set(binding, "rigComposer", rig); Set(binding, "targetSource", target); Set(binding, "precedence", precedence); Set(binding, "tieBreakerId", tieBreaker); Set(binding, "logDiagnostics", true);
        }

        private static void RemoveLocalOutputAndLegacyRoots(Scene scene)
        {
            var remove = new HashSet<GameObject>();
            foreach (CameraOutputSessionBinding item in All<CameraOutputSessionBinding>(scene)) remove.Add(item.gameObject);
            foreach (CinemachineBrain item in All<CinemachineBrain>(scene)) remove.Add(item.gameObject);
            foreach (UnityEngine.Camera item in All<UnityEngine.Camera>(scene)) remove.Add(item.gameObject);
            foreach (GameObject root in scene.GetRootGameObjects()) if (root.name.StartsWith("QA_C9L_", StringComparison.Ordinal)) remove.Add(root);
            foreach (GameObject item in remove) UnityEngine.Object.DestroyImmediate(item);
        }

        private static void RemoveOtherPlayers(Scene scene, LocalPlayerCameraRequestBinding selected) { foreach (LocalPlayerCameraRequestBinding item in All<LocalPlayerCameraRequestBinding>(scene)) if (item != selected) UnityEngine.Object.DestroyImmediate(item); }
        private static Transform Target(Scene scene, string name, Vector3 position) { GameObject item = RootObject(scene, name); item.transform.position = position; return item.transform; }
        private static CameraRigComposer Composer(Scene scene, string rootName, string cameraName) { GameObject root = RootObject(scene, rootName); CameraRigComposer composer = Component<CameraRigComposer>(root); CinemachineCamera camera = Component<CinemachineCamera>(Child(root.transform, cameraName)); camera.enabled = false; Set(composer, "cinemachineCamera", camera); Set(composer, "createCinemachineCameraIfMissing", false); Set(composer, "logApplyRebuildDiagnostics", false); return composer; }
        private static GameObject RootObject(Scene scene, string name) { GameObject item = Find(scene, name); if (item != null) return item; item = new GameObject(name); SceneManager.MoveGameObjectToScene(item, scene); return item; }
        private static GameObject Child(Transform parent, string name) { for (int i = 0; i < parent.childCount; i++) if (parent.GetChild(i).name == name) return parent.GetChild(i).gameObject; var item = new GameObject(name); item.transform.SetParent(parent, false); return item; }
        private static T Component<T>(GameObject item) where T : Component { T component = item.GetComponent<T>(); return component != null ? component : item.AddComponent<T>(); }
        private static List<T> All<T>(Scene scene) where T : Component { var result = new List<T>(); foreach (T item in Resources.FindObjectsOfTypeAll<T>()) if (item != null && item.gameObject.scene == scene) result.Add(item); return result; }
        private static T Single<T>(Scene scene) where T : Component { List<T> all = All<T>(scene); if (all.Count != 1) throw new InvalidOperationException($"Expected one {typeof(T).Name} in '{scene.name}', found '{all.Count}'."); return all[0]; }
        private static GameObject Find(Scene scene, string name) { foreach (GameObject root in scene.GetRootGameObjects()) { if (root.name == name) return root; foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) if (child.name == name) return child.gameObject; } return null; }
        private static void EnsureBuildScene() { var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes); if (!scenes.Exists(item => item.path == ScenePath)) { scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray(); } }
        private static T LoadOrCreate<T>(string path) where T : ScriptableObject { T value = AssetDatabase.LoadAssetAtPath<T>(path); if (value != null) return value; value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value; }
        private static T Require<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException($"Required asset is missing at '{path}'.");
        private static void Set(UnityEngine.Object target, string property, object value) { var so = new SerializedObject(target); so.Update(); SerializedProperty item = Property(so, property); if (value == null) item.objectReferenceValue = null; else if (value is UnityEngine.Object reference) item.objectReferenceValue = reference; else if (value is string text) item.stringValue = text; else if (value is int number) item.intValue = number; else if (value is bool flag) item.boolValue = flag; else throw new InvalidOperationException($"Unsupported value for '{property}'."); so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target); }
        private static SerializedProperty Property(SerializedObject serialized, string property) => serialized.FindProperty(property) ?? throw new InvalidOperationException($"Serialized property '{property}' was not found on '{serialized.targetObject.GetType().Name}'.");
    }
}

using System;
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
    public static class QaC9LPlayerCameraArbitrationInstaller
    {
        private const string Root = "Assets/ImmersiveFrameworkQA";
        private const string CameraRoot = Root + "/Camera";
        private const string HubRoot = Root + "/Hub";

        private const string ScenePath =
            CameraRoot + "/Scenes/QA_PlayerCameraArbitration.unity";
        private const string RoutePath =
            CameraRoot + "/Routes/QA_PlayerCameraArbitrationRoute.asset";
        private const string ActivityPath =
            CameraRoot + "/Activities/QA_PlayerCameraArbitrationActivity.asset";

        private const string HubScenePath =
            HubRoot + "/Scenes/QA_Hub.unity";
        private const string HubRoutePath =
            HubRoot + "/Routes/QA_HubRoute.asset";

        private const string HubLabel =
            "Camera / C9L Player Arbitration";
        private const string HubReason =
            "qa.hub.route.camera_player_arbitration";
        private const string HubTriggerName =
            "RouteTrigger_Camera_C9L_Player_Arbitration";
        private const string CoordinatorName =
            "QA_C9L_RouteCompletionCoordinator";

        [MenuItem("Immersive Framework QA/Camera/C9L Install Player Camera Arbitration QA")]
        public static void Install()
        {
            try
            {
                EnsureFolders();
                CreateOrRepairAssets();
                CreateScene();
                EnsureSceneInBuildSettings();
                CreateOrRepairHubEntry();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[C9L_PLAYER_CAMERA_ARBITRATION_SETUP] status='Succeeded' " +
                    $"scene='{ScenePath}' route='{RoutePath}' activity='{ActivityPath}' hubLabel='{HubLabel}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[C9L_PLAYER_CAMERA_ARBITRATION_SETUP] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static void CreateOrRepairAssets()
        {
            ActivityAsset activity = LoadOrCreate<ActivityAsset>(ActivityPath);
            SetString(activity, "activityName", "QA C9L Player Camera Arbitration Activity");
            SetString(activity, "description", "Activity override used to prove Player restoration.");

            RouteAsset route = LoadOrCreate<RouteAsset>(RoutePath);
            SetString(route, "routeName", "QA C9L Player Camera Arbitration");
            SetString(route, "primaryScenePath", ScenePath);
            SetString(route, "primarySceneName", Path.GetFileNameWithoutExtension(ScenePath));
            SetObject(route, "startupActivity", activity);
            SetString(route, "description", "QA route proving Route, Player and Activity camera arbitration.");
        }

        private static void CreateScene()
        {
            RouteAsset route = RequireAsset<RouteAsset>(RoutePath);
            ActivityAsset activity = RequireAsset<ActivityAsset>(ActivityPath);
            RouteAsset hubRoute = RequireAsset<RouteAsset>(HubRoutePath);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            GameObject outputObject = new GameObject("QA_C9L_Output");
            UnityEngine.Camera unityCamera =
                outputObject.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain =
                outputObject.AddComponent<CinemachineBrain>();
            outputObject.transform.position = new Vector3(0f, 0f, -10f);

            CameraOutputSessionBinding sessionBinding =
                outputObject.AddComponent<CameraOutputSessionBinding>();
            SetString(sessionBinding, "outputId", "camera.output.main");
            SetObject(sessionBinding, "unityCamera", unityCamera);
            SetObject(sessionBinding, "cinemachineBrain", brain);
            SetBool(sessionBinding, "initializeOnAwake", true);
            SetBool(sessionBinding, "logDiagnostics", true);

            GameObject routeTarget = CreateTarget("QA_C9L_RouteTarget", new Vector3(0f, 1f, 0f));
            GameObject playerTarget = CreateTarget("QA_C9L_PlayerTarget", new Vector3(2f, 1f, 0f));
            GameObject playerLookAt = CreateTarget("QA_C9L_PlayerLookAt", new Vector3(2f, 1.5f, 1f));
            GameObject activityTarget = CreateTarget("QA_C9L_ActivityTarget", new Vector3(-2f, 1f, 0f));

            CameraRigComposer routeComposer =
                CreateComposer("QA_C9L_RouteRig", "Route Cinemachine Camera");
            CameraRigComposer playerRigComposer =
                CreateComposer("QA_C9L_PlayerRig", "Player Cinemachine Camera");
            CameraRigComposer activityComposer =
                CreateComposer("QA_C9L_ActivityRig", "Activity Cinemachine Camera");

            routeComposer.CinemachineCamera.enabled = false;
            playerRigComposer.CinemachineCamera.enabled = false;
            activityComposer.CinemachineCamera.enabled = false;

            GameObject routeRoot = new GameObject("QA_C9L_RouteContent");
            RouteContentBinding routeContent =
                routeRoot.AddComponent<RouteContentBinding>();
            SetObject(routeContent, "route", route);
            SetString(routeContent, "localContentId", "qa.c9l.route-content");

            RouteCameraRequestBinding routeBinding =
                routeRoot.AddComponent<RouteCameraRequestBinding>();
            ConfigureRouteBinding(
                routeBinding,
                route,
                sessionBinding,
                routeComposer,
                routeTarget.transform);

            GameObject playerObject = new GameObject("QA_C9L_LocalPlayer");
            PlayerInput playerInput = playerObject.AddComponent<PlayerInput>();
            PlayerComposer authoredPlayer =
                playerObject.AddComponent<PlayerComposer>();

            SetString(authoredPlayer, "actorId", "qa.player.actor.c9l");
            SetString(authoredPlayer, "playerSlotId", "qa.player.slot.1");
            SetObject(authoredPlayer, "playerInput", playerInput);
            SetBool(authoredPlayer, "cameraBindingRequired", true);
            SetObject(authoredPlayer, "cameraTarget", playerTarget.transform);
            SetObject(authoredPlayer, "lookAtTarget", playerLookAt.transform);

            LocalPlayerCameraRequestBinding playerBinding =
                playerObject.AddComponent<LocalPlayerCameraRequestBinding>();
            ConfigurePlayerBinding(
                playerBinding,
                authoredPlayer,
                "qa.player.eligibility.c9l",
                "qa.camera.request.c9l.player",
                sessionBinding,
                playerRigComposer,
                50,
                "player");

            GameObject invalidPlayerObject =
                new GameObject("QA_C9L_InvalidPlayerBinding");
            invalidPlayerObject.transform.SetParent(playerObject.transform, false);

            LocalPlayerCameraRequestBinding invalidPlayerBinding =
                invalidPlayerObject.AddComponent<LocalPlayerCameraRequestBinding>();
            ConfigurePlayerBinding(
                invalidPlayerBinding,
                authoredPlayer,
                string.Empty,
                "qa.camera.request.c9l.invalid-player",
                sessionBinding,
                playerRigComposer,
                40,
                "invalid-player");

            GameObject activityRoot = new GameObject("QA_C9L_ActivityContent");
            activityRoot.transform.SetParent(routeRoot.transform, false);

            ActivityLocalVisibilityAdapter activityAdapter =
                activityRoot.AddComponent<ActivityLocalVisibilityAdapter>();
            SetObject(activityAdapter, "activity", activity);
            SetString(activityAdapter, "localContentId", "qa.c9l.activity-content");

            ActivityCameraRequestBinding activityBinding =
                activityRoot.AddComponent<ActivityCameraRequestBinding>();
            ConfigureActivityBinding(
                activityBinding,
                activity,
                sessionBinding,
                activityComposer,
                activityTarget.transform);

            GameObject controls = new GameObject("QA_C9L_Controls");

            ActivityRequestTrigger activityTrigger =
                controls.AddComponent<ActivityRequestTrigger>();
            activityTrigger.TargetActivity = activity;
            SetString(activityTrigger, "reason", "qa.c9l.activity-cycle");

            RouteRequestTrigger backTrigger =
                controls.AddComponent<RouteRequestTrigger>();
            backTrigger.TargetRoute = hubRoute;
            SetString(backTrigger, "reason", "qa.c9l.back-to-hub");

            QaC9LPlayerCameraArbitrationFixture fixture =
                controls.AddComponent<QaC9LPlayerCameraArbitrationFixture>();
            SetObject(fixture, "outputSession", sessionBinding);
            SetObject(fixture, "routeBinding", routeBinding);
            SetObject(fixture, "playerBinding", playerBinding);
            SetObject(fixture, "activityBinding", activityBinding);
            SetObject(fixture, "invalidPlayerBinding", invalidPlayerBinding);
            SetObject(fixture, "routeComposer", routeComposer);
            SetObject(fixture, "playerComposer", playerRigComposer);
            SetObject(fixture, "activityComposer", activityComposer);
            SetObject(fixture, "activityRequestTrigger", activityTrigger);
            SetObject(fixture, "backToHubTrigger", backTrigger);
            SetBool(fixture, "runOnStart", false);
            SetBool(fixture, "throwOnFailure", false);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static GameObject CreateTarget(string name, Vector3 position)
        {
            GameObject target = new GameObject(name);
            target.transform.position = position;
            return target;
        }

        private static CameraRigComposer CreateComposer(
            string rootName,
            string cameraName)
        {
            GameObject root = new GameObject(rootName);
            CameraRigComposer composer =
                root.AddComponent<CameraRigComposer>();

            GameObject cameraObject = new GameObject(cameraName);
            cameraObject.transform.SetParent(root.transform, false);

            CinemachineCamera camera =
                cameraObject.AddComponent<CinemachineCamera>();

            SetObject(composer, "cinemachineCamera", camera);
            SetBool(composer, "createUnityCameraIfMissing", false);
            SetBool(composer, "createCinemachineCameraIfMissing", false);
            SetBool(composer, "logApplyRebuildDiagnostics", false);

            return composer;
        }

        private static void ConfigureRouteBinding(
            RouteCameraRequestBinding binding,
            RouteAsset route,
            CameraOutputSessionBinding session,
            CameraRigComposer composer,
            Transform target)
        {
            SetObject(binding, "assignedRoute", route);
            SetString(binding, "scopeId", "qa.route.c9l");
            SetString(binding, "requestId", "qa.camera.request.c9l.route");
            SetObject(binding, "outputSession", session);
            SetObject(binding, "rigComposer", composer);
            SetObject(binding, "targetSource", target);
            SetInt(binding, "precedence", 10);
            SetString(binding, "tieBreakerId", "route");
            SetBool(binding, "logDiagnostics", true);
        }

        private static void ConfigurePlayerBinding(
            LocalPlayerCameraRequestBinding binding,
            PlayerComposer player,
            string scopeId,
            string requestId,
            CameraOutputSessionBinding session,
            CameraRigComposer composer,
            int precedence,
            string tieBreaker)
        {
            SetObject(binding, "playerComposer", player);
            SetString(binding, "eligibilityScopeId", scopeId);
            SetString(binding, "requestId", requestId);
            SetObject(binding, "outputSession", session);
            SetObject(binding, "rigComposer", composer);
            SetInt(binding, "precedence", precedence);
            SetString(binding, "tieBreakerId", tieBreaker);
            SetBool(binding, "eligibleOnEnable", true);
            SetBool(binding, "releaseOnDisable", true);
            SetBool(binding, "logDiagnostics", true);
        }

        private static void ConfigureActivityBinding(
            ActivityCameraRequestBinding binding,
            ActivityAsset activity,
            CameraOutputSessionBinding session,
            CameraRigComposer composer,
            Transform target)
        {
            SetObject(binding, "assignedActivity", activity);
            SetString(binding, "scopeId", "qa.activity.c9l");
            SetString(binding, "requestId", "qa.camera.request.c9l.activity");
            SetObject(binding, "outputSession", session);
            SetObject(binding, "rigComposer", composer);
            SetObject(binding, "targetSource", target);
            SetInt(binding, "precedence", 100);
            SetString(binding, "tieBreakerId", "activity");
            SetBool(binding, "logDiagnostics", true);
        }

        private static void CreateOrRepairHubEntry()
        {
            Scene hubScene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);

            RouteAsset route = RequireAsset<RouteAsset>(RoutePath);
            QaHubPanel panel = FindSingleInScene<QaHubPanel>(hubScene);

            GameObject triggerObject = FindByName(hubScene, HubTriggerName);
            if (triggerObject == null)
            {
                triggerObject = new GameObject(HubTriggerName);
                triggerObject.transform.SetParent(panel.transform, false);
            }

            RouteRequestTrigger trigger =
                triggerObject.GetComponent<RouteRequestTrigger>();

            if (trigger == null)
            {
                trigger = triggerObject.AddComponent<RouteRequestTrigger>();
            }

            trigger.TargetRoute = route;
            SetString(trigger, "reason", HubReason);

            GameObject coordinatorObject =
                FindByName(hubScene, CoordinatorName);

            if (coordinatorObject == null)
            {
                coordinatorObject = new GameObject(CoordinatorName);
            }

            coordinatorObject.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(coordinatorObject, hubScene);

            QaC9LRouteCompletionCoordinator coordinator =
                coordinatorObject.GetComponent<QaC9LRouteCompletionCoordinator>();

            if (coordinator == null)
            {
                coordinator =
                    coordinatorObject.AddComponent<QaC9LRouteCompletionCoordinator>();
            }

            SetObject(coordinator, "routeTrigger", trigger);

            var serialized = new SerializedObject(panel);
            serialized.Update();
            SerializedProperty entries = RequireProperty(serialized, "entries");

            int index = -1;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("label").stringValue == HubLabel)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                index = entries.arraySize;
                entries.arraySize++;
            }

            SerializedProperty targetEntry =
                entries.GetArrayElementAtIndex(index);
            targetEntry.FindPropertyRelative("label").stringValue = HubLabel;
            targetEntry.FindPropertyRelative("routeRequestTrigger")
                .objectReferenceValue = trigger;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(hubScene);
            EditorSceneManager.SaveScene(hubScene, HubScenePath);
        }

        private static T FindSingleInScene<T>(Scene scene)
            where T : Component
        {
            T[] candidates = Resources.FindObjectsOfTypeAll<T>();
            T result = null;
            int count = 0;

            foreach (T candidate in candidates)
            {
                if (candidate != null && candidate.gameObject.scene == scene)
                {
                    result = candidate;
                    count++;
                }
            }

            if (count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected one {typeof(T).Name} in '{scene.name}', found '{count}'.");
            }

            return result;
        }

        private static GameObject FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == name)
                    {
                        return child.gameObject;
                    }
                }
            }

            return null;
        }

        private static void EnsureSceneInBuildSettings()
        {
            var scenes =
                new System.Collections.Generic.List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);

            if (!scenes.Exists(x => x.path == ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder(Root, "Camera");
            EnsureFolder(CameraRoot, "Scenes");
            EnsureFolder(CameraRoot, "Routes");
            EnsureFolder(CameraRoot, "Activities");
            EnsureFolder(CameraRoot, "Scripts");
            EnsureFolder(CameraRoot + "/Scripts", "Runtime");
            EnsureFolder(CameraRoot + "/Scripts", "Editor");
            EnsureFolder(CameraRoot, "Documentation");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                return existing;
            }

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static T RequireAsset<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException(
                    $"Required asset is missing at '{path}'.");
            }

            return asset;
        }

        private static void SetObject(
            UnityEngine.Object target,
            string property,
            UnityEngine.Object value)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target),
                    $"Cannot assign serialized property '{property}' because target is null.");
            }

            var serialized = new SerializedObject(target);
            serialized.Update();
            RequireProperty(serialized, property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(
            UnityEngine.Object target,
            string property,
            string value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            RequireProperty(serialized, property).stringValue =
                value ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(
            UnityEngine.Object target,
            string property,
            bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            RequireProperty(serialized, property).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetInt(
            UnityEngine.Object target,
            string property,
            int value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            RequireProperty(serialized, property).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string property)
        {
            SerializedProperty found = serialized.FindProperty(property);
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"Serialized property '{property}' was not found on '{serialized.targetObject.GetType().Name}'.");
            }

            return found;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
        }
    }
}

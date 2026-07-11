using System;
using System.IO;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using ImmersiveFrameworkQA.Hub;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    public static class QaC9ICanonicalBindingsInstaller
    {
        private const string Root = "Assets/ImmersiveFrameworkQA";
        private const string CameraRoot = Root + "/Camera";
        private const string HubRoot = Root + "/Hub";

        private const string ScenePath =
            CameraRoot + "/Scenes/QA_CanonicalCameraBindings.unity";
        private const string RoutePath =
            CameraRoot + "/Routes/QA_CanonicalCameraBindingsRoute.asset";
        private const string ActivityPath =
            CameraRoot + "/Activities/QA_CanonicalCameraBindingsActivity.asset";
        private const string ForeignRoutePath =
            CameraRoot + "/Routes/QA_CanonicalCameraBindingsForeignRoute.asset";

        private const string HubScenePath =
            HubRoot + "/Scenes/QA_Hub.unity";
        private const string HubRoutePath =
            HubRoot + "/Routes/QA_HubRoute.asset";

        private const string HubLabel =
            "Camera / C9I Canonical Bindings";
        private const string HubReason =
            "qa.hub.route.camera_canonical_bindings";
        private const string HubTriggerName =
            "RouteTrigger_Camera_C9I_Canonical_Bindings";

        [MenuItem("Immersive Framework QA/Camera/C9I Install Canonical Camera Bindings QA")]
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
                    "[C9I_CANONICAL_CAMERA_BINDINGS_SETUP] status='Succeeded' " +
                    $"scene='{ScenePath}' route='{RoutePath}' foreignRoute='{ForeignRoutePath}' " +
                    $"activity='{ActivityPath}' hubLabel='{HubLabel}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[C9I_CANONICAL_CAMERA_BINDINGS_SETUP] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static void CreateOrRepairAssets()
        {
            ActivityAsset activity = LoadOrCreate<ActivityAsset>(ActivityPath);
            SetString(activity, "activityName", "QA C9I Canonical Camera Bindings Activity");
            SetString(activity, "description", "Startup Activity used to prove canonical camera binding callbacks.");

            RouteAsset route = LoadOrCreate<RouteAsset>(RoutePath);
            SetString(route, "routeName", "QA C9I Canonical Camera Bindings");
            SetString(route, "primaryScenePath", ScenePath);
            SetString(route, "primarySceneName", Path.GetFileNameWithoutExtension(ScenePath));
            SetObject(route, "startupActivity", activity);
            SetString(route, "description", "QA route proving Route/Activity camera bindings through canonical lifecycle.");

            RouteAsset foreignRoute = LoadOrCreate<RouteAsset>(ForeignRoutePath);
            SetString(foreignRoute, "routeName", "QA C9I Foreign Route");
            SetString(
                foreignRoute,
                "description",
                "Dedicated foreign RouteAsset used only to prove lifecycle asset mismatch.");
        }

        private static void CreateScene()
        {
            RouteAsset route = RequireAsset<RouteAsset>(RoutePath);
            RouteAsset foreignRoute = RequireAsset<RouteAsset>(ForeignRoutePath);
            ActivityAsset activity = RequireAsset<ActivityAsset>(ActivityPath);
            RouteAsset hubRoute = RequireAsset<RouteAsset>(HubRoutePath);

            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            GameObject outputObject = new GameObject("QA_C9I_Output");
            UnityEngine.Camera outputCamera =
                outputObject.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain =
                outputObject.AddComponent<CinemachineBrain>();
            outputObject.transform.position = new Vector3(0f, 0f, -10f);

            CameraOutputSessionBinding sessionBinding =
                outputObject.AddComponent<CameraOutputSessionBinding>();
            SetString(sessionBinding, "outputId", "camera.output.main");
            SetObject(sessionBinding, "unityCamera", outputCamera);
            SetObject(sessionBinding, "cinemachineBrain", brain);
            SetBool(sessionBinding, "initializeOnAwake", true);
            SetBool(sessionBinding, "logDiagnostics", true);

            GameObject target = new GameObject("QA_C9I_Target");
            target.transform.position = new Vector3(0f, 1.5f, 0f);

            CameraRigComposer routeComposer =
                CreateComposer("QA_C9I_RouteRig", "Route Cinemachine Camera");
            CameraRigComposer activityComposer =
                CreateComposer("QA_C9I_ActivityRig", "Activity Cinemachine Camera");

            routeComposer.CinemachineCamera.enabled = false;
            activityComposer.CinemachineCamera.enabled = false;

            GameObject routeRoot = new GameObject("QA_C9I_RouteContent");
            RouteContentBinding routeContent =
                routeRoot.AddComponent<RouteContentBinding>();
            SetObject(routeContent, "route", route);
            SetString(routeContent, "localContentId", "qa.c9i.route-content");

            QaC9IRouteExitProbe routeExitProbe =
                routeRoot.AddComponent<QaC9IRouteExitProbe>();

            RouteCameraRequestBinding routeBinding =
                routeRoot.AddComponent<RouteCameraRequestBinding>();
            ConfigureRouteBinding(
                routeBinding,
                route,
                "qa.route.c9i",
                "qa.camera.request.c9i.route",
                sessionBinding,
                routeComposer,
                target.transform,
                10,
                "route");

            GameObject foreignRouteObject =
                new GameObject("QA_C9I_ForeignRouteBinding");
            foreignRouteObject.transform.SetParent(routeRoot.transform, false);

            RouteCameraRequestBinding foreignRouteBinding =
                foreignRouteObject.AddComponent<RouteCameraRequestBinding>();
            ConfigureRouteBinding(
                foreignRouteBinding,
                foreignRoute,
                "qa.route.c9i.foreign",
                "qa.camera.request.c9i.foreign-route",
                sessionBinding,
                routeComposer,
                target.transform,
                5,
                "foreign-route");

            SetObject(routeExitProbe, "routeBinding", routeBinding);
            SetObject(routeExitProbe, "outputSession", sessionBinding);

            GameObject activityRoot = new GameObject("QA_C9I_ActivityContent");
            activityRoot.transform.SetParent(routeRoot.transform, false);

            ActivityLocalVisibilityAdapter activityAdapter =
                activityRoot.AddComponent<ActivityLocalVisibilityAdapter>();
            SetObject(activityAdapter, "activity", activity);
            SetString(activityAdapter, "localContentId", "qa.c9i.activity-content");

            ActivityCameraRequestBinding activityBinding =
                activityRoot.AddComponent<ActivityCameraRequestBinding>();
            ConfigureActivityBinding(
                activityBinding,
                activity,
                "qa.activity.c9i",
                "qa.camera.request.c9i.activity",
                sessionBinding,
                activityComposer,
                target.transform,
                100,
                "activity");

            GameObject missingScopeActivityObject =
                new GameObject("QA_C9I_MissingScopeActivityBinding");
            missingScopeActivityObject.transform.SetParent(activityRoot.transform, false);

            ActivityCameraRequestBinding missingScopeActivityBinding =
                missingScopeActivityObject.AddComponent<ActivityCameraRequestBinding>();
            ConfigureActivityBinding(
                missingScopeActivityBinding,
                activity,
                string.Empty,
                "qa.camera.request.c9i.missing-scope",
                sessionBinding,
                activityComposer,
                target.transform,
                90,
                "missing-scope");

            GameObject controls = new GameObject("QA_C9I_Controls");

            ActivityRequestTrigger activityTrigger =
                controls.AddComponent<ActivityRequestTrigger>();
            activityTrigger.TargetActivity = activity;
            SetString(activityTrigger, "reason", "qa.c9i.activity-cycle");

            RouteRequestTrigger backTrigger =
                controls.AddComponent<RouteRequestTrigger>();
            backTrigger.TargetRoute = hubRoute;
            SetString(backTrigger, "reason", "qa.c9i.back-to-hub");

            QaC9ICanonicalBindingsFixture fixture =
                controls.AddComponent<QaC9ICanonicalBindingsFixture>();
            SetObject(fixture, "outputSession", sessionBinding);
            SetObject(fixture, "routeBinding", routeBinding);
            SetObject(fixture, "activityBinding", activityBinding);
            SetObject(fixture, "foreignRouteBinding", foreignRouteBinding);
            SetObject(fixture, "missingScopeActivityBinding", missingScopeActivityBinding);
            SetObject(fixture, "routeComposer", routeComposer);
            SetObject(fixture, "activityComposer", activityComposer);
            SetObject(fixture, "activityRequestTrigger", activityTrigger);
            SetObject(fixture, "backToHubTrigger", backTrigger);
            SetBool(fixture, "runOnStart", false);
            SetBool(fixture, "throwOnFailure", false);

            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save C9I scene at '{ScenePath}'.");
            }
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
            string scopeId,
            string requestId,
            CameraOutputSessionBinding session,
            CameraRigComposer composer,
            Transform target,
            int precedence,
            string tieBreaker)
        {
            SetObject(binding, "assignedRoute", route);
            SetString(binding, "scopeId", scopeId);
            SetString(binding, "requestId", requestId);
            SetObject(binding, "outputSession", session);
            SetObject(binding, "rigComposer", composer);
            SetObject(binding, "targetSource", target);
            SetInt(binding, "precedence", precedence);
            SetString(binding, "tieBreakerId", tieBreaker);
            SetBool(binding, "logDiagnostics", true);
        }

        private static void ConfigureActivityBinding(
            ActivityCameraRequestBinding binding,
            ActivityAsset activity,
            string scopeId,
            string requestId,
            CameraOutputSessionBinding session,
            CameraRigComposer composer,
            Transform target,
            int precedence,
            string tieBreaker)
        {
            SetObject(binding, "assignedActivity", activity);
            SetString(binding, "scopeId", scopeId);
            SetString(binding, "requestId", requestId);
            SetObject(binding, "outputSession", session);
            SetObject(binding, "rigComposer", composer);
            SetObject(binding, "targetSource", target);
            SetInt(binding, "precedence", precedence);
            SetString(binding, "tieBreakerId", tieBreaker);
            SetBool(binding, "logDiagnostics", true);
        }

        private static void CreateOrRepairHubEntry()
        {
            Scene hubScene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);

            RouteAsset route = RequireAsset<RouteAsset>(RoutePath);
            QaHubPanel panel =
                FindSingleInScene<QaHubPanel>(hubScene);

            GameObject triggerObject = FindByName(
                hubScene,
                HubTriggerName);

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

            QaC9IRouteCompletionCoordinator legacyCoordinator =
                triggerObject.GetComponent<QaC9IRouteCompletionCoordinator>();

            if (legacyCoordinator != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyCoordinator);
            }

            const string coordinatorObjectName =
                "QA_C9I_RouteCompletionCoordinator";

            GameObject coordinatorObject =
                FindByName(hubScene, coordinatorObjectName);

            if (coordinatorObject == null)
            {
                coordinatorObject =
                    new GameObject(coordinatorObjectName);
            }

            coordinatorObject.transform.SetParent(null, false);
            SceneManager.MoveGameObjectToScene(
                coordinatorObject,
                hubScene);

            QaC9IRouteCompletionCoordinator coordinator =
                coordinatorObject.GetComponent<QaC9IRouteCompletionCoordinator>();

            if (coordinator == null)
            {
                coordinator =
                    coordinatorObject.AddComponent<QaC9IRouteCompletionCoordinator>();
            }

            SetObject(coordinator, "routeTrigger", trigger);

            var serialized = new SerializedObject(panel);
            serialized.Update();
            SerializedProperty entries =
                RequireProperty(serialized, "entries");

            int index = -1;

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry =
                    entries.GetArrayElementAtIndex(i);

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
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
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
                    $"Cannot assign serialized property '{property}' because the target component is null.");
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
            RequireProperty(serialized, property).stringValue = value ?? string.Empty;
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

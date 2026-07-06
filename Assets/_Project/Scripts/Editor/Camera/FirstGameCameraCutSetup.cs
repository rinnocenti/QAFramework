using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using Project._Project.Scripts.Runtime.GameCamera;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Editor.Camera
{
    /// <summary>
    /// Editor-only, idempotent FIRSTGAME camera setup for the first Cinemachine Route/Activity cut.
    /// It configures scene-authored bindings without using runtime global searches.
    /// </summary>
    public static class FirstGameCameraCutSetup
    {
        private const string MenuScenePath = "Assets/_Project/Scenes/Menu/FG_Menu.unity";
        private const string GameplayScenePath = "Assets/_Project/Scenes/Gameplay/FG_Gameplay.unity";

        private const string MenuRoutePath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/Routes/FG_MenuRoute.asset";
        private const string GameplayRoutePath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/Routes/FG_GameplayRoute.asset";

        private const string ActivityAPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/Activity/FG_Activity_A.asset";
        private const string ActivityBPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/Activity/FG_Activity_B.asset";
        private const string ActivityCPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/Activity/FG_Activity_C_RouteFallback.asset";
        private const string ActivityDPath = "Assets/_Project/ScriptableObjects/ImmersiveFramework/Activity/FG_Activity_D_StopBgm.asset";

        [MenuItem("Tools/FIRSTGAME/Camera/Configure Route-Activity Camera Cut")]
        public static void ConfigureCameraCut()
        {
            ConfigureMenuScene();
            ConfigureGameplayScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FIRSTGAME_CAMERA_SETUP] Route/Activity camera cut configured.");
        }

        private static void ConfigureMenuScene()
        {
            Scene scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            RouteAsset menuRoute = LoadAsset<RouteAsset>(MenuRoutePath);

            GameObject mainCamera = EnsureMainCamera(scene, new Vector3(0f, 1f, -10f), Quaternion.identity);
            UnityEngine.Camera operationalCamera = EnsureComponent<UnityEngine.Camera>(mainCamera);
            CinemachineBrain brain = EnsureComponent<CinemachineBrain>(mainCamera);

            GameObject root = EnsureRoot(scene, "FirstGameCameraRoot");
            RouteContentBinding routeContentBinding = EnsureComponent<RouteContentBinding>(root);
            FirstGameCameraDirector director = EnsureComponent<FirstGameCameraDirector>(root);
            FirstGameRouteCameraBinding routeCameraBinding = EnsureComponent<FirstGameRouteCameraBinding>(root);

            GameObject menuRig = EnsureCinemachineRig(scene, "MenuRoute_CameraRig", new Vector3(0f, 1f, -10f), Quaternion.identity);

            SetSerialized(routeContentBinding, "route", menuRoute);
            SetSerialized(routeContentBinding, "localContentId", "fg1-menu-route-camera");
            SetSerialized(routeContentBinding, "requiredness", 10);

            SetSerialized(director, "operationalCamera", operationalCamera);
            SetSerialized(director, "cinemachineBrain", brain);
            SetSerialized(director, "defaultCameraRig", menuRig);
            SetSerialized(director, "routePriority", 20);
            SetSerialized(director, "activityPriority", 100);
            SetSerialized(director, "logTransitions", true);

            SetSerialized(routeCameraBinding, "routeCameraRig", menuRig);
            SetSerialized(routeCameraBinding, "routeAnchors", (Object)null);
            SetSerialized(routeCameraBinding, "director", director);
            SetSerialized(routeCameraBinding, "startupActivityCameraBinding", (Object)null);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureGameplayScene()
        {
            Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            RouteAsset gameplayRoute = LoadAsset<RouteAsset>(GameplayRoutePath);
            ActivityAsset activityA = LoadAsset<ActivityAsset>(ActivityAPath);
            ActivityAsset activityB = LoadAsset<ActivityAsset>(ActivityBPath);
            ActivityAsset activityC = LoadAsset<ActivityAsset>(ActivityCPath);
            ActivityAsset activityD = LoadAsset<ActivityAsset>(ActivityDPath);

            GameObject mainCamera = EnsureMainCamera(scene, new Vector3(0f, 4f, -10f), Quaternion.Euler(20f, 0f, 0f));
            UnityEngine.Camera operationalCamera = EnsureComponent<UnityEngine.Camera>(mainCamera);
            CinemachineBrain brain = EnsureComponent<CinemachineBrain>(mainCamera);

            GameObject root = EnsureRoot(scene, "FirstGameCameraRoot");
            RouteContentBinding routeContentBinding = EnsureComponent<RouteContentBinding>(root);
            FirstGameCameraDirector director = EnsureComponent<FirstGameCameraDirector>(root);
            FirstGameRouteCameraBinding routeCameraBinding = EnsureComponent<FirstGameRouteCameraBinding>(root);

            GameObject player = FindInScene(scene, "PlayerPrototype");
            GameObject anchorsObject = EnsureRoot(scene, "FirstGameCameraAnchors");
            FirstGameCameraAnchorHost anchors = EnsureComponent<FirstGameCameraAnchorHost>(anchorsObject);
            Transform target = player != null ? player.transform : null;
            SetSerialized(anchors, "trackingTarget", target);
            SetSerialized(anchors, "lookAtTarget", target);

            GameObject routeRig = EnsureCinemachineRig(scene, "GameplayRoute_CameraRig", new Vector3(0f, 5f, -10f), Quaternion.Euler(25f, 0f, 0f));
            GameObject activityARig = EnsureCinemachineRig(scene, "ActivityA_CameraRig", new Vector3(0f, 3.5f, -7f), Quaternion.Euler(18f, 0f, 0f));

            SetSerialized(routeContentBinding, "route", gameplayRoute);
            SetSerialized(routeContentBinding, "localContentId", "fg1-gameplay-route-camera");
            SetSerialized(routeContentBinding, "requiredness", 10);

            SetSerialized(director, "operationalCamera", operationalCamera);
            SetSerialized(director, "cinemachineBrain", brain);
            SetSerialized(director, "defaultCameraRig", routeRig);
            SetSerialized(director, "routePriority", 20);
            SetSerialized(director, "activityPriority", 100);
            SetSerialized(director, "logTransitions", true);

            FirstGameActivityCameraBinding activityABinding = EnsureActivityBinding(scene, "ActivityA_ContentRoot", activityA, activityARig, FirstGameActivityCameraPolicy.UseOwnOrKeepPreviousActivity, anchors, director);
            EnsureActivityBinding(scene, "ActivityB_ContentRoot", activityB, null, FirstGameActivityCameraPolicy.UseOwnOrKeepPreviousActivity, anchors, director);
            EnsureActivityBinding(scene, "ActivityC_RouteFallback_ContentRoot", activityC, null, FirstGameActivityCameraPolicy.UseRoute, anchors, director);
            EnsureActivityBinding(scene, "ActivityD_StopBgm_ContentRoot", activityD, null, FirstGameActivityCameraPolicy.UseRoute, anchors, director);

            SetSerialized(routeCameraBinding, "routeCameraRig", routeRig);
            SetSerialized(routeCameraBinding, "routeAnchors", anchors);
            SetSerialized(routeCameraBinding, "director", director);
            SetSerialized(routeCameraBinding, "startupActivityCameraBinding", activityABinding);

            routeRig.SetActive(false);
            activityARig.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static FirstGameActivityCameraBinding EnsureActivityBinding(
            Scene scene,
            string rootName,
            ActivityAsset expectedActivity,
            GameObject rig,
            FirstGameActivityCameraPolicy policy,
            FirstGameCameraAnchorHost anchors,
            FirstGameCameraDirector director)
        {
            GameObject root = FindInScene(scene, rootName);
            if (root == null)
            {
                Debug.LogWarning($"[FIRSTGAME_CAMERA_SETUP] Activity root not found. root='{rootName}'.");
                return null;
            }

            ActivityLocalVisibilityAdapter adapter = root.GetComponent<ActivityLocalVisibilityAdapter>();
            if (adapter != null && expectedActivity != null)
            {
                SetSerialized(adapter, "activity", expectedActivity);
            }

            FirstGameActivityCameraBinding binding = EnsureComponent<FirstGameActivityCameraBinding>(root);
            SetSerialized(binding, "activityCameraRig", rig);
            SetSerialized(binding, "policy", (int)policy);
            SetSerialized(binding, "anchors", anchors);
            SetSerialized(binding, "director", director);
            return binding;
        }

        private static GameObject EnsureMainCamera(Scene scene, Vector3 position, Quaternion rotation)
        {
            GameObject cameraObject = FindInScene(scene, "Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetPositionAndRotation(position, rotation);
            }

            cameraObject.tag = "MainCamera";
            return cameraObject;
        }

        private static GameObject EnsureRoot(Scene scene, string name)
        {
            GameObject root = FindInScene(scene, name);
            if (root != null)
            {
                return root;
            }

            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return root;
        }

        private static GameObject EnsureCinemachineRig(Scene scene, string name, Vector3 position, Quaternion rotation)
        {
            GameObject rig = FindInScene(scene, name);
            if (rig == null)
            {
                rig = new GameObject(name);
                SceneManager.MoveGameObjectToScene(rig, scene);
            }

            rig.transform.SetPositionAndRotation(position, rotation);
            EnsureComponent<CinemachineCamera>(rig);
            if (rig.GetComponent<UnityEngine.Camera>() != null)
            {
                Object.DestroyImmediate(rig.GetComponent<UnityEngine.Camera>());
            }

            if (rig.GetComponent<CinemachineBrain>() != null)
            {
                Object.DestroyImmediate(rig.GetComponent<CinemachineBrain>());
            }

            return rig;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindInChildren(root.transform, name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInChildren(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindInChildren(root.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[FIRSTGAME_CAMERA_SETUP] Required asset not found. path='{path}'.");
            }

            return asset;
        }

        private static void SetSerialized(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FIRSTGAME_CAMERA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FIRSTGAME_CAMERA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FIRSTGAME_CAMERA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FIRSTGAME_CAMERA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}

using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using ImmersiveFrameworkQA.Lifecycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Lifecycle.Editor
{
    public static class QaLifecycleSceneBuilder
    {
        private const string RouteAScenePath = "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleRouteA.unity";
        private const string RouteBScenePath = "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleRouteB.unity";
        private const string AdditionalScenePath = "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleAdditional.unity";

        private const string RouteAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteA.asset";
        private const string RouteBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteB.asset";
        private const string NoActivityRoutePath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleNoActivityRoute.asset";
        private const string HubRoutePath = "Assets/ImmersiveFrameworkQA/Hub/Routes/QA_HubRoute.asset";

        private const string ActivityAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityA.asset";
        private const string ActivityBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityB.asset";
        private const string NoContentActivityPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleNoContentActivity.asset";

        [MenuItem("Immersive Framework QA/Lifecycle/Create or Refresh Lifecycle QA Scenes")]
        public static void CreateOrRefreshLifecycleQaScenes()
        {
            bool routeA = CreateRouteScene(
                RouteAScenePath,
                "A",
                RouteAPath,
                new Color(0.045f, 0.055f, 0.065f, 1f),
                new Vector3(-1.5f, 0.5f, 0f));

            bool routeB = CreateRouteScene(
                RouteBScenePath,
                "B",
                RouteBPath,
                new Color(0.055f, 0.045f, 0.065f, 1f),
                new Vector3(1.5f, 0.5f, 0f));

            bool additional = CreateAdditionalScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (routeA && routeB && additional)
            {
                Debug.Log("[QA_LIFECYCLE_SETUP] Lifecycle QA scenes refreshed under Assets/ImmersiveFrameworkQA/Lifecycle.");
                return;
            }

            Debug.LogError("[QA_LIFECYCLE_SETUP] Lifecycle QA scene refresh completed with setup errors. Fix the errors above before running smoke.");
        }

        private static bool CreateRouteScene(string scenePath, string label, string routePath, Color backgroundColor, Vector3 contentOffset)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            RouteAsset activeRoute = LoadAsset<RouteAsset>(routePath);
            RouteAsset routeA = LoadAsset<RouteAsset>(RouteAPath);
            RouteAsset routeB = LoadAsset<RouteAsset>(RouteBPath);
            RouteAsset noActivityRoute = LoadAsset<RouteAsset>(NoActivityRoutePath);
            RouteAsset hubRoute = LoadAsset<RouteAsset>(HubRoutePath);
            ActivityAsset activityA = LoadAsset<ActivityAsset>(ActivityAPath);
            ActivityAsset activityB = LoadAsset<ActivityAsset>(ActivityBPath);
            ActivityAsset noContentActivity = LoadAsset<ActivityAsset>(NoContentActivityPath);

            if (activeRoute == null
                || routeA == null
                || routeB == null
                || noActivityRoute == null
                || hubRoute == null
                || activityA == null
                || activityB == null
                || noContentActivity == null)
            {
                Debug.LogError($"[QA_LIFECYCLE_SETUP] Scene setup aborted because required assets are missing. scene='{scenePath}'.");
                return false;
            }

            CreateCamera(backgroundColor);
            CreateDirectionalLight($"QA_Lifecycle_Light_{label}");

            GameObject routeRoot = EnsureRoot(scene, $"QA_Lifecycle_RouteContent_{label}");
            RouteContentBinding routeBinding = EnsureComponent<RouteContentBinding>(routeRoot);
            SetSerialized(routeBinding, "route", activeRoute);
            SetSerialized(routeBinding, "localContentId", $"qa-lifecycle-route-{label.ToLowerInvariant()}");
            SetSerialized(routeBinding, "requiredness", 10);

            CreateMarker(routeRoot.transform, $"Route {label}", contentOffset, new Color(0.2f, 0.55f, 0.85f, 1f), PrimitiveType.Cube);

            CreateActivityContent(scene, $"QA_Lifecycle_ActivityA_{label}", activityA, contentOffset + new Vector3(-1.2f, 1.25f, 0f), Color.green, PrimitiveType.Sphere);
            CreateActivityContent(scene, $"QA_Lifecycle_ActivityB_{label}", activityB, contentOffset + new Vector3(0f, 1.25f, 0f), Color.yellow, PrimitiveType.Capsule);
            CreateActivityContent(scene, $"QA_Lifecycle_NoContentActivity_{label}", noContentActivity, contentOffset + new Vector3(1.2f, 1.25f, 0f), Color.gray, PrimitiveType.Cylinder);

            CreatePanel(
                scene,
                label,
                routeA,
                routeB,
                noActivityRoute,
                hubRoute,
                activityA,
                activityB,
                noContentActivity);

            EditorSceneManager.SaveScene(scene, scenePath);
            return true;
        }

        private static bool CreateAdditionalScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = System.IO.Path.GetFileNameWithoutExtension(AdditionalScenePath);

            CreateCamera(new Color(0.04f, 0.055f, 0.05f, 1f));
            CreateDirectionalLight("QA_Lifecycle_Additional_Light");
            GameObject root = EnsureRoot(scene, "QA_Lifecycle_AdditionalContent");
            CreateMarker(root.transform, "Additional Route Content", Vector3.zero, new Color(0.35f, 0.7f, 0.55f, 1f), PrimitiveType.Cylinder);

            EditorSceneManager.SaveScene(scene, AdditionalScenePath);
            return true;
        }

        private static void CreateActivityContent(
            Scene scene,
            string name,
            ActivityAsset activity,
            Vector3 position,
            Color color,
            PrimitiveType primitiveType)
        {
            GameObject root = EnsureRoot(scene, name);
            ActivityLocalVisibilityAdapter activityBinding = EnsureComponent<ActivityLocalVisibilityAdapter>(root);
            SetSerialized(activityBinding, "activity", activity);
            SetSerialized(activityBinding, "localContentId", name.ToLowerInvariant());
            SetSerialized(activityBinding, "requiredness", 10);

            CreateMarker(root.transform, activity.ActivityName, position, color, primitiveType);
        }

        private static void CreatePanel(
            Scene scene,
            string label,
            RouteAsset routeA,
            RouteAsset routeB,
            RouteAsset noActivityRoute,
            RouteAsset hubRoute,
            ActivityAsset activityA,
            ActivityAsset activityB,
            ActivityAsset noContentActivity)
        {
            GameObject panelObject = EnsureRoot(scene, $"QA_LifecyclePanel_{label}");
            QaLifecyclePanel panel = EnsureComponent<QaLifecyclePanel>(panelObject);

            RouteRequestTrigger routeATrigger = CreateRouteTrigger(scene, panelObject.transform, $"QA_Lifecycle_RouteRequest_A_{label}", routeA, $"qa.lifecycle.{label.ToLowerInvariant()}.route-a");
            RouteRequestTrigger routeBTrigger = CreateRouteTrigger(scene, panelObject.transform, $"QA_Lifecycle_RouteRequest_B_{label}", routeB, $"qa.lifecycle.{label.ToLowerInvariant()}.route-b");
            RouteRequestTrigger noActivityRouteTrigger = CreateRouteTrigger(scene, panelObject.transform, $"QA_Lifecycle_RouteRequest_NoActivity_{label}", noActivityRoute, $"qa.lifecycle.{label.ToLowerInvariant()}.route-no-activity");
            RouteRequestTrigger hubRouteTrigger = CreateRouteTrigger(scene, panelObject.transform, $"QA_Lifecycle_RouteRequest_Hub_{label}", hubRoute, $"qa.lifecycle.{label.ToLowerInvariant()}.hub");

            ActivityRequestTrigger activityATrigger = CreateActivityTrigger(scene, panelObject.transform, $"QA_Lifecycle_ActivityRequest_A_{label}", activityA, $"qa.lifecycle.{label.ToLowerInvariant()}.activity-a");
            ActivityRequestTrigger activityBTrigger = CreateActivityTrigger(scene, panelObject.transform, $"QA_Lifecycle_ActivityRequest_B_{label}", activityB, $"qa.lifecycle.{label.ToLowerInvariant()}.activity-b");
            ActivityRequestTrigger noContentActivityTrigger = CreateActivityTrigger(scene, panelObject.transform, $"QA_Lifecycle_ActivityRequest_NoContent_{label}", noContentActivity, $"qa.lifecycle.{label.ToLowerInvariant()}.activity-no-content");
            ActivityRequestTrigger clearActivityTrigger = CreateActivityTrigger(scene, panelObject.transform, $"QA_Lifecycle_ActivityRequest_Clear_{label}", null, $"qa.lifecycle.{label.ToLowerInvariant()}.activity-clear");

            panel.Configure(
                routeATrigger,
                routeBTrigger,
                noActivityRouteTrigger,
                hubRouteTrigger,
                activityATrigger,
                activityBTrigger,
                noContentActivityTrigger,
                clearActivityTrigger,
                $"QA Lifecycle Route {label}");

            EditorUtility.SetDirty(panel);
        }

        private static RouteRequestTrigger CreateRouteTrigger(Scene scene, Transform parent, string name, RouteAsset route, string reason)
        {
            GameObject triggerObject = EnsureChild(scene, parent, name);
            RouteRequestTrigger trigger = EnsureComponent<RouteRequestTrigger>(triggerObject);
            SetSerialized(trigger, "targetRoute", route);
            SetSerialized(trigger, "reason", reason);
            EditorUtility.SetDirty(trigger);
            return trigger;
        }

        private static ActivityRequestTrigger CreateActivityTrigger(Scene scene, Transform parent, string name, ActivityAsset activity, string reason)
        {
            GameObject triggerObject = EnsureChild(scene, parent, name);
            ActivityRequestTrigger trigger = EnsureComponent<ActivityRequestTrigger>(triggerObject);
            SetSerialized(trigger, "targetActivity", activity);
            SetSerialized(trigger, "reason", reason);
            EditorUtility.SetDirty(trigger);
            return trigger;
        }

        private static void CreateCamera(Color backgroundColor)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 3f, -8f), Quaternion.Euler(18f, 0f, 0f));
        }

        private static void CreateDirectionalLight(string name)
        {
            GameObject lightObject = new GameObject(name);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void CreateMarker(Transform parent, string name, Vector3 position, Color color, PrimitiveType primitiveType)
        {
            GameObject marker = GameObject.CreatePrimitive(primitiveType);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            Renderer renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader != null)
                {
                    Material material = new Material(shader);
                    material.color = color;
                    renderer.sharedMaterial = material;
                }
            }
        }

        private static GameObject EnsureRoot(Scene scene, string name)
        {
            GameObject root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return root;
        }

        private static GameObject EnsureChild(Scene scene, Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            SceneManager.MoveGameObjectToScene(child, scene);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[QA_LIFECYCLE_SETUP] Required asset not found. path='{path}'.");
            }

            return asset;
        }

        private static void SetSerialized(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[QA_LIFECYCLE_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[QA_LIFECYCLE_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[QA_LIFECYCLE_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}

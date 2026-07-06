using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Hub.Editor
{
    public static class QaHubSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA";
        private const string HubRoot = Root + "/Hub";
        private const string HubScenes = HubRoot + "/Scenes";
        private const string PlayerRoot = Root + "/Player";
        private const string PlayerScenes = PlayerRoot + "/Scenes";

        private const string HubScenePath = HubScenes + "/QA_Hub.unity";
        private const string HubRoutePath = HubRoot + "/Routes/QA_HubRoute.asset";
        private const string LifecycleRoutePath = Root + "/Lifecycle/Routes/QA_LifecycleRouteA.asset";
        private const string LifecycleSceneAPath = Root + "/Lifecycle/Scenes/QA_LifecycleRouteA.unity";
        private const string LifecycleSceneBPath = Root + "/Lifecycle/Scenes/QA_LifecycleRouteB.unity";
        private const string UnityBuildSurfaceRoutePath = Root + "/UnityBuildSurface/Routes/QA_UnityBuildSurfaceRoute.asset";
        private const string UnityBuildSurfaceScenePath = Root + "/UnityBuildSurface/Scenes/UnityBuildSurfaceQA.unity";
        private const string CameraRoutePath = Root + "/Camera/Routes/QA_CameraRoute.asset";
        private const string CameraScenePath = Root + "/Camera/Scenes/QA_Camera.unity";
        private const string CameraSceneBPath = Root + "/Camera/Scenes/QA_CameraRouteB.unity";
        private const string PoolingRoutePath = Root + "/Pooling/Routes/QA_PoolingRoute.asset";
        private const string PoolingScenePath = Root + "/Pooling/Scenes/QA_Pooling.unity";
        private const string AudioRoutePath = Root + "/Audio/Routes/QA_AudioRoute.asset";
        private const string AudioScenePath = Root + "/Audio/Scenes/QA_Audio.unity";
        private const string PlayerIdentityRoutePath = PlayerRoot + "/Routes/QA_PlayerIdentityRoute.asset";
        private const string PlayerIdentityScenePath = PlayerScenes + "/QA_PlayerIdentity.unity";

        private static readonly HubTarget[] Targets =
        {
            new HubTarget("Lifecycle / Core Flow QA", LifecycleRoutePath),
            new HubTarget("Unity Build Surface QA", UnityBuildSurfaceRoutePath),
            new HubTarget("Camera QA", CameraRoutePath),
            new HubTarget("Pooling QA", PoolingRoutePath),
            new HubTarget("Audio QA", AudioRoutePath),
            new HubTarget("Player Identity QA", PlayerIdentityRoutePath)
        };

        [MenuItem("Immersive Framework QA/Hub/Create or Refresh Hub and Player QA Scenes")]
        public static void CreateOrRefreshHubAndPlayerScenes()
        {
            EnsureFolders();
            CreateHubScene();
            CreatePlayerIdentityScene();
            ConfigureBackToHubPanelInScene(LifecycleSceneAPath, new Rect(16f, 620f, 360f, 92f));
            ConfigureBackToHubPanelInScene(LifecycleSceneBPath, new Rect(16f, 620f, 360f, 92f));
            ConfigureBackToHubPanelInScene(CameraScenePath, new Rect(16f, 620f, 360f, 92f));
            ConfigureBackToHubPanelInScene(CameraSceneBPath, new Rect(16f, 620f, 360f, 92f));
            ConfigureBackToHubPanelInScene(PoolingScenePath, new Rect(500f, 16f, 360f, 92f));
            ConfigureBackToHubPanelInScene(AudioScenePath, new Rect(590f, 16f, 360f, 92f));
            ConfigureBackToHubPanelInScene(UnityBuildSurfaceScenePath, new Rect(16f, 140f, 360f, 92f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QA_HUB] Hub and Player Identity QA scenes created or refreshed.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder(Root, "Hub");
            EnsureFolder(HubRoot, "Routes");
            EnsureFolder(HubRoot, "Activities");
            EnsureFolder(HubRoot, "Scenes");
            EnsureFolder(HubRoot, "Scripts");
            EnsureFolder(HubRoot + "/Scripts", "Runtime");
            EnsureFolder(HubRoot + "/Scripts", "Editor");
            EnsureFolder(Root, "Lifecycle");
            EnsureFolder(Root + "/Lifecycle", "Scenes");
            EnsureFolder(Root + "/Lifecycle", "Routes");
            EnsureFolder(Root + "/Lifecycle", "Activities");
            EnsureFolder(Root + "/Lifecycle", "Scripts");
            EnsureFolder(Root, "Camera");
            EnsureFolder(Root + "/Camera", "Scenes");
            EnsureFolder(Root + "/Camera", "Routes");
            EnsureFolder(Root + "/Camera", "Activities");
            EnsureFolder(Root, "Pooling");
            EnsureFolder(Root + "/Pooling", "Scenes");
            EnsureFolder(Root + "/Pooling", "Routes");
            EnsureFolder(Root + "/Pooling", "Activities");
            EnsureFolder(Root, "Audio");
            EnsureFolder(Root + "/Audio", "Scenes");
            EnsureFolder(Root + "/Audio", "Routes");
            EnsureFolder(Root + "/Audio", "Activities");
            EnsureFolder(Root, "UnityBuildSurface");
            EnsureFolder(Root + "/UnityBuildSurface", "Scenes");
            EnsureFolder(Root + "/UnityBuildSurface", "Routes");
            EnsureFolder(Root + "/UnityBuildSurface", "Activities");
            EnsureFolder(Root, "Player");
            EnsureFolder(PlayerRoot, "Scenes");
            EnsureFolder(PlayerRoot, "Routes");
            EnsureFolder(PlayerRoot, "Activities");
            EnsureFolder(PlayerRoot, "Scripts");
            EnsureFolder(PlayerRoot + "/Scripts", "Runtime");
            EnsureFolder(PlayerRoot + "/Scripts", "Editor");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void CreateHubScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_Hub";

            CreateCamera("QA_HubCamera", new Color(0.04f, 0.05f, 0.06f, 1f));

            Canvas canvas = CreateCanvas("QA_HubCanvas");
            QaHubPanel panel = canvas.gameObject.AddComponent<QaHubPanel>();

            QaHubPanel.QaHubEntry[] entries = new QaHubPanel.QaHubEntry[Targets.Length];
            for (int i = 0; i < Targets.Length; i++)
            {
                RouteRequestTrigger trigger = CreateRouteTrigger(canvas.transform, "RouteTrigger_" + SanitizeName(Targets[i].Label), Targets[i]);
                entries[i] = new QaHubPanel.QaHubEntry(Targets[i].Label, trigger);
            }

            panel.Configure(entries, "Immersive Framework QA Hub");

            EditorSceneManager.SaveScene(scene, HubScenePath);
        }

        private static void CreatePlayerIdentityScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_PlayerIdentity";

            CreateCamera("QA_PlayerIdentityCamera", new Color(0.06f, 0.055f, 0.045f, 1f));

            Canvas canvas = CreateCanvas("QA_PlayerIdentityCanvas");
            canvas.gameObject.AddComponent<QaPlayerIdentityPlaceholderPanel>();
            CreateBackToHubPanel(canvas.transform, new Rect(16f, 220f, 360f, 92f));

            EditorSceneManager.SaveScene(scene, PlayerIdentityScenePath);
        }

        private static void CreateCamera(string name, Color backgroundColor)
        {
            GameObject cameraObject = new GameObject(name);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new GameObject(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        private static RouteRequestTrigger CreateRouteTrigger(Transform parent, string name, HubTarget target)
        {
            GameObject triggerObject = new GameObject(name);
            triggerObject.transform.SetParent(parent, false);

            RouteRequestTrigger trigger = triggerObject.AddComponent<RouteRequestTrigger>();
            RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(target.RoutePath);
            if (route == null)
            {
                Debug.LogError($"[QA_HUB] Route asset not found. path='{target.RoutePath}'.", triggerObject);
            }

            SerializedObject serialized = new SerializedObject(trigger);
            serialized.FindProperty("targetRoute").objectReferenceValue = route;
            serialized.FindProperty("reason").stringValue = "qa.hub.route." + SanitizeName(target.Label).ToLowerInvariant();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);
            return trigger;
        }

        private static void CreateBackToHubPanel(Transform parent, Rect panelRect)
        {
            RouteRequestTrigger trigger = CreateRouteTrigger(parent, "RouteTrigger_BackToQaHub", new HubTarget("Back to QA Hub", HubRoutePath));
            GameObject panelObject = new GameObject("QA_BackToHubPanel");
            panelObject.transform.SetParent(parent, false);

            QaHubPanel panel = panelObject.AddComponent<QaHubPanel>();
            panel.Configure(
                new[] { new QaHubPanel.QaHubEntry("Back to QA Hub", trigger) },
                "QA Navigation");

            SerializedObject serialized = new SerializedObject(panel);
            serialized.FindProperty("panelRect").rectValue = panelRect;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        private static void ConfigureBackToHubPanelInScene(string scenePath, Rect panelRect)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[QA_HUB] Cannot configure Back to Hub panel. Scene not found. path='{scenePath}'.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameObject root = EnsureSceneRoot(scene, "QA_BackToHubNavigation");
            RouteRequestTrigger trigger = EnsureComponent<RouteRequestTrigger>(root);
            RouteAsset hubRoute = AssetDatabase.LoadAssetAtPath<RouteAsset>(HubRoutePath);

            SerializedObject triggerSerialized = new SerializedObject(trigger);
            triggerSerialized.FindProperty("targetRoute").objectReferenceValue = hubRoute;
            triggerSerialized.FindProperty("reason").stringValue = "qa.route.back-to-hub";
            triggerSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);

            QaHubPanel panel = EnsureComponent<QaHubPanel>(root);
            panel.Configure(
                new[] { new QaHubPanel.QaHubEntry("Back to QA Hub", trigger) },
                "QA Navigation");

            SerializedObject panelSerialized = new SerializedObject(panel);
            panelSerialized.FindProperty("panelRect").rectValue = panelRect;
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject EnsureSceneRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }
            }

            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static string SanitizeName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "Unnamed"
                : value.Replace(" / ", "_").Replace(" ", "_");
        }

        private readonly struct HubTarget
        {
            public HubTarget(string label, string routePath)
            {
                Label = label;
                RoutePath = routePath;
            }

            public string Label { get; }
            public string RoutePath { get; }
        }
    }
}

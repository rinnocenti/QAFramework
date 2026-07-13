using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Hub.Editor
{
    public static class QaHubSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA";
        private const string HubScenePath = Root + "/Hub/Scenes/QA_Hub.unity";

        private static readonly HubTarget[] Targets =
        {
            new("Lifecycle / Core Flow QA", Root + "/Lifecycle/Routes/QA_LifecycleRouteA.asset"),
            new("Pooling QA", Root + "/Pooling/Routes/QA_PoolingRoute.asset"),
            new("C9R Camera Override Authority", Root + "/Camera/Routes/QA_PlayerCameraArbitrationRoute.asset")
        };

        [MenuItem("Immersive Framework QA/Hub/Create or Refresh QA Hub")]
        public static void CreateOrRefreshHub()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_Hub";
            var cameraRoot = new GameObject("QA_HubCamera");
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.06f, 1f);
            cameraRoot.tag = "MainCamera";
            cameraRoot.transform.position = new Vector3(0f, 0f, -10f);

            var canvasRoot = new GameObject("QA_HubCanvas");
            canvasRoot.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            QaHubPanel panel = canvasRoot.AddComponent<QaHubPanel>();
            var entries = new QaHubPanel.QaHubEntry[Targets.Length];
            for (int index = 0; index < Targets.Length; index++)
            {
                HubTarget target = Targets[index];
                var triggerRoot = new GameObject("RouteTrigger_" + SanitizeName(target.Label));
                triggerRoot.transform.SetParent(canvasRoot.transform, false);
                RouteRequestTrigger trigger = triggerRoot.AddComponent<RouteRequestTrigger>();
                RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(target.RoutePath)
                    ?? throw new System.InvalidOperationException($"Required QA route is missing: '{target.RoutePath}'.");
                var serialized = new SerializedObject(trigger);
                serialized.FindProperty("targetRoute").objectReferenceValue = route;
                serialized.FindProperty("reason").stringValue = "qa.hub.route." + SanitizeName(target.Label).ToLowerInvariant();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                entries[index] = new QaHubPanel.QaHubEntry(target.Label, trigger);
            }

            panel.Configure(entries, "Immersive Framework QA");
            EditorSceneManager.SaveScene(scene, HubScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string SanitizeName(string value) => value.Replace(" / ", "_").Replace(" ", "_");

        private readonly struct HubTarget
        {
            public HubTarget(string label, string routePath) { Label = label; RoutePath = routePath; }
            public string Label { get; }
            public string RoutePath { get; }
        }
    }
}

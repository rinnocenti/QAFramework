using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.UnityBuildSurface.Editor
{
    internal static class TransitionQaRouteSwitchPanelInstaller
    {
        private const string RootPath = "Assets/ImmersiveFrameworkQA/UnityBuildSurface";
        private const string SceneAPath = RootPath + "/Scenes/TransitionRouteA.unity";
        private const string SceneBPath = RootPath + "/Scenes/TransitionRouteB.unity";
        private const string RouteAPath = RootPath + "/Routes/QA_TransitionRouteA.asset";
        private const string RouteBPath = RootPath + "/Routes/QA_TransitionRouteB.asset";

        [MenuItem("Immersive Framework/QA/Unity Build Surface/Install Transition QA Route Switch Panels")]
        internal static void InstallPanels()
        {
            var routeA = AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteAPath);
            var routeB = AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteBPath);

            if (routeA == null || routeB == null)
            {
                Debug.LogError("[Immersive Framework QA] Transition QA routes are missing. Run 'Create Transition QA Routes and Scenes' first.");
                return;
            }

            InstallPanel(SceneAPath, routeB, "Transition QA — Route A", "Request Transition Route B", "qa.transition.to-route-b");
            InstallPanel(SceneBPath, routeA, "Transition QA — Route B", "Request Transition Route A", "qa.transition.to-route-a");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Immersive Framework QA] Transition QA route switch panels installed.");
        }

        private static void InstallPanel(
            string scenePath,
            RouteAsset targetRoute,
            string panelTitle,
            string buttonLabel,
            string requestReason)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogError($"[Immersive Framework QA] Transition QA scene is missing: {scenePath}");
                return;
            }

            var previousScenePath = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            var root = FindOrCreateRoot("Unity Build Surface Transition QA");
            var trigger = FindOrCreateRouteRequestTrigger(root.transform, targetRoute, requestReason);
            var panel = FindOrCreatePanel(root.transform);
            panel.Configure(trigger, panelTitle, buttonLabel);

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(trigger);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!string.IsNullOrWhiteSpace(previousScenePath) && File.Exists(previousScenePath) && previousScenePath != scenePath)
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static GameObject FindOrCreateRoot(string rootName)
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name == rootName)
                {
                    return root;
                }
            }

            return new GameObject(rootName);
        }

        private static RouteRequestTrigger FindOrCreateRouteRequestTrigger(
            Transform root,
            RouteAsset targetRoute,
            string requestReason)
        {
            var triggers = root.GetComponentsInChildren<RouteRequestTrigger>(includeInactive: true);
            foreach (var existing in triggers)
            {
                if (existing.TargetRoute == targetRoute)
                {
                    SetReason(existing, requestReason);
                    return existing;
                }
            }

            var triggerObject = new GameObject("Route Request To Other Transition QA Route");
            triggerObject.transform.SetParent(root, false);
            var trigger = triggerObject.AddComponent<RouteRequestTrigger>();
            trigger.TargetRoute = targetRoute;
            SetReason(trigger, requestReason);
            return trigger;
        }

        private static TransitionQaRouteSwitchPanel FindOrCreatePanel(Transform root)
        {
            var panel = root.GetComponentInChildren<TransitionQaRouteSwitchPanel>(includeInactive: true);
            if (panel != null)
            {
                return panel;
            }

            var panelObject = new GameObject("Transition QA Route Switch Panel");
            panelObject.transform.SetParent(root, false);
            return panelObject.AddComponent<TransitionQaRouteSwitchPanel>();
        }

        private static void SetReason(RouteRequestTrigger trigger, string requestReason)
        {
            var serialized = new SerializedObject(trigger);
            serialized.FindProperty("reason").stringValue = requestReason;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

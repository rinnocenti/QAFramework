using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Editor
{
    internal static class ImmersiveFrameworkTransitionQaWorkspaceCreator
    {
        private const string RootPath = "Assets/ImmersiveFrameworkQA/UnityBuildSurface";
        private const string ScenesPath = RootPath + "/Scenes";
        private const string RoutesPath = RootPath + "/Routes";
        private const string ActivitiesPath = RootPath + "/Activities";

        private const string RouteAPath = RoutesPath + "/QA_TransitionRouteA.asset";
        private const string RouteBPath = RoutesPath + "/QA_TransitionRouteB.asset";
        private const string ActivityAPath = ActivitiesPath + "/QA_TransitionActivityA.asset";
        private const string ActivityBPath = ActivitiesPath + "/QA_TransitionActivityB.asset";
        private const string SceneAPath = ScenesPath + "/TransitionRouteA.unity";
        private const string SceneBPath = ScenesPath + "/TransitionRouteB.unity";

        [MenuItem("Immersive Framework/QA/Unity Build Surface/Create Transition QA Routes and Scenes")]
        internal static void CreateTransitionQaRoutesAndScenes()
        {
            EnsureFolders();

            var activityA = LoadOrCreateActivity(
                ActivityAPath,
                "QA_TransitionActivityA",
                "QA Transition Activity A",
                "Unity Build Surface QA activity for transition route A.");

            var activityB = LoadOrCreateActivity(
                ActivityBPath,
                "QA_TransitionActivityB",
                "QA Transition Activity B",
                "Unity Build Surface QA activity for transition route B.");

            var routeA = LoadOrCreateRoute(
                RouteAPath,
                "QA_TransitionRouteA",
                "QA Transition Route A",
                SceneAPath,
                "TransitionRouteA",
                activityA,
                "Unity Build Surface QA route A for isolated transition tests.");

            var routeB = LoadOrCreateRoute(
                RouteBPath,
                "QA_TransitionRouteB",
                "QA Transition Route B",
                SceneBPath,
                "TransitionRouteB",
                activityB,
                "Unity Build Surface QA route B for isolated transition tests.");

            CreateSceneIfMissing(SceneAPath, "QA Transition Route A", routeB, "qa.transition.to-route-b");
            CreateSceneIfMissing(SceneBPath, "QA Transition Route B", routeA, "qa.transition.to-route-a");
            EnsureScenesInBuildSettings(SceneAPath, SceneBPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = routeA;
            EditorGUIUtility.PingObject(routeA);

            Debug.Log("[Immersive Framework QA] Transition QA routes/scenes are ready under Assets/ImmersiveFrameworkQA/UnityBuildSurface.");
        }

        private static ActivityAsset LoadOrCreateActivity(
            string assetPath,
            string assetName,
            string activityName,
            string description)
        {
            var activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(assetPath);
            if (activity == null)
            {
                activity = ScriptableObject.CreateInstance<ActivityAsset>();
                activity.name = assetName;
                AssetDatabase.CreateAsset(activity, assetPath);
            }

            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("description").stringValue = description;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);
            return activity;
        }

        private static RouteAsset LoadOrCreateRoute(
            string assetPath,
            string assetName,
            string routeName,
            string primaryScenePath,
            string primarySceneName,
            ActivityAsset startupActivity,
            string description)
        {
            var route = AssetDatabase.LoadAssetAtPath<RouteAsset>(assetPath);
            if (route == null)
            {
                route = ScriptableObject.CreateInstance<RouteAsset>();
                route.name = assetName;
                AssetDatabase.CreateAsset(route, assetPath);
            }

            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeName").stringValue = routeName;
            serialized.FindProperty("primaryScenePath").stringValue = primaryScenePath;
            serialized.FindProperty("primarySceneName").stringValue = primarySceneName;
            serialized.FindProperty("startupActivity").objectReferenceValue = startupActivity;
            serialized.FindProperty("description").stringValue = description;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);
            return route;
        }

        private static void CreateSceneIfMissing(
            string scenePath,
            string sceneLabel,
            RouteAsset targetRoute,
            string routeRequestReason)
        {
            if (File.Exists(scenePath))
            {
                Debug.Log($"[Immersive Framework QA] Transition QA scene already exists: {scenePath}");
                return;
            }

            var previousScenePath = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = Path.GetFileNameWithoutExtension(scenePath);

            var root = new GameObject("Unity Build Surface Transition QA");
            var marker = new GameObject(sceneLabel);
            marker.transform.SetParent(root.transform, false);

            var requestObject = new GameObject("Route Request To Other Transition QA Route");
            requestObject.transform.SetParent(root.transform, false);
            var trigger = requestObject.AddComponent<RouteRequestTrigger>();
            trigger.TargetRoute = targetRoute;

            var serializedTrigger = new SerializedObject(trigger);
            serializedTrigger.FindProperty("reason").stringValue = routeRequestReason;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();

            var visualMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualMarker.name = "Transition QA Visual Marker";
            visualMarker.transform.SetParent(root.transform, false);
            visualMarker.transform.position = new Vector3(0f, 0.5f, 0f);
            visualMarker.transform.localScale = new Vector3(2f, 1f, 2f);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[Immersive Framework QA] Created Transition QA scene: {scenePath}");

            if (!string.IsNullOrWhiteSpace(previousScenePath) && File.Exists(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static void EnsureScenesInBuildSettings(params string[] scenePaths)
        {
            var existing = EditorBuildSettings.scenes;
            var changed = false;

            foreach (var scenePath in scenePaths)
            {
                var found = false;
                for (var i = 0; i < existing.Length; i++)
                {
                    if (existing[i].path == scenePath)
                    {
                        found = true;
                        if (!existing[i].enabled)
                        {
                            existing[i].enabled = true;
                            changed = true;
                        }

                        break;
                    }
                }

                if (found)
                {
                    continue;
                }

                var next = new EditorBuildSettingsScene[existing.Length + 1];
                for (var i = 0; i < existing.Length; i++)
                {
                    next[i] = existing[i];
                }

                next[next.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
                existing = next;
                changed = true;
            }

            if (changed)
            {
                EditorBuildSettings.scenes = existing;
                Debug.Log("[Immersive Framework QA] Transition QA scenes added/enabled in Build Settings.");
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "UnityBuildSurface");
            EnsureFolder(RootPath, "Scenes");
            EnsureFolder(RootPath, "Routes");
            EnsureFolder(RootPath, "Activities");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}

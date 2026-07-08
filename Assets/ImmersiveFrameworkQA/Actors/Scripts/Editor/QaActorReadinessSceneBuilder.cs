using System;
using System.IO;
using Immersive.Framework.Authoring;
using ImmersiveFrameworkQA.Actors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Actors.Editor
{
    public static class QaActorReadinessSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Actors";
        private const string ScenePath = Root + "/Scenes/QA_ActorReadiness.unity";
        private const string RoutePath = Root + "/Routes/QA_ActorReadinessRoute.asset";
        private const string ActivityPath = Root + "/Activities/QA_ActorReadinessActivity.asset";

        [MenuItem("Immersive Framework QA/Actors/Create or Refresh F49B Actor Readiness QA Scene")]
        public static void CreateOrRefreshActorReadinessScene()
        {
            EnsureFolders();

            ActivityAsset activity = CreateActivityAsset(
                ActivityPath,
                "QA Actor Readiness Activity",
                "F49B Actor readiness passive contract QA activity.");

            CreateRouteAsset(
                RoutePath,
                "QA Actor Readiness Route",
                ScenePath,
                "F49B Actor readiness passive contract QA route.",
                activity);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_ActorReadiness";

            CreateCamera("QA_ActorReadinessCamera", new Color(0.05f, 0.06f, 0.08f, 1f));

            GameObject root = new GameObject("QA_F49B_ActorReadiness");
            var fixture = root.AddComponent<QaActorReadinessContractFixture>();
            SetBool(fixture, "runOnStart", true);
            SetBool(fixture, "throwOnFailure", false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[F49B_ACTOR_READINESS_QA] Fixture scene ready: " + ScenePath);
        }

        [MenuItem("Immersive Framework QA/Actors/Open F49B Actor Readiness QA Scene")]
        public static void OpenActorReadinessScene()
        {
            if (!File.Exists(ToFullPath(ScenePath)))
            {
                CreateOrRefreshActorReadinessScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
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

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "Actors");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Routes");
            EnsureFolder(Root, "Activities");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root + "/Scripts", "Runtime");
            EnsureFolder(Root + "/Scripts", "Editor");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static RouteAsset CreateRouteAsset(string assetPath, string routeName, string scenePath, string description, ActivityAsset startupActivity)
        {
            RouteAsset asset = LoadOrCreate<RouteAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetSerialized(serialized, "routeName", routeName);
            SetSerialized(serialized, "primaryScenePath", scenePath);
            SetSerialized(serialized, "primarySceneName", Path.GetFileNameWithoutExtension(scenePath));
            SetSerialized(serialized, "startupActivity", startupActivity);
            SetSerialized(serialized, "description", description);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ActivityAsset CreateActivityAsset(string assetPath, string activityName, string description)
        {
            ActivityAsset asset = LoadOrCreate<ActivityAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetSerialized(serialized, "activityName", activityName);
            SetSerialized(serialized, "description", description);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void SetSerialized(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = FindProperty(serialized, propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetSerialized(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = FindProperty(serialized, propertyName);
            property.stringValue = value ?? string.Empty;
        }

        private static void SetBool(Component component, string propertyName, bool value)
        {
            SerializedProperty property = FindProperty(component, propertyName);
            property.boolValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(Component component, string propertyName)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {component.GetType().Name}.");
            }

            return property;
        }

        private static SerializedProperty FindProperty(SerializedObject serialized, string propertyName)
        {
            if (serialized == null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                string targetName = serialized.targetObject != null ? serialized.targetObject.GetType().Name : "<null>";
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {targetName}.");
            }

            return property;
        }

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve Unity project root.");
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}

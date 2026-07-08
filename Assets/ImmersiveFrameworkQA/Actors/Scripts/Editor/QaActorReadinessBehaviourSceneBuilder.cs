using System;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using ImmersiveFrameworkQA.Actors;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Actors.Editor
{
    public static class QaActorReadinessBehaviourSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Actors";
        private const string ScenePath = Root + "/Scenes/QA_ActorReadinessBehaviour.unity";
        private const string RoutePath = Root + "/Routes/QA_ActorReadinessBehaviourRoute.asset";
        private const string ActivityPath = Root + "/Activities/QA_ActorReadinessBehaviourActivity.asset";

        [MenuItem("Immersive Framework QA/Actors/Create or Refresh F49C Actor Readiness Behaviour QA Scene")]
        public static void CreateOrRefreshActorReadinessBehaviourScene()
        {
            EnsureFolders();

            ActivityAsset activity = CreateActivityAsset(
                ActivityPath,
                "QA Actor Readiness Behaviour Activity",
                "F49C Actor readiness Unity adapter QA activity.");

            CreateRouteAsset(
                RoutePath,
                "QA Actor Readiness Behaviour Route",
                ScenePath,
                "F49C Actor readiness Unity adapter QA route.",
                activity);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_ActorReadinessBehaviour";

            CreateCamera("QA_ActorReadinessBehaviourCamera", new Color(0.055f, 0.065f, 0.09f, 1f));

            GameObject root = new GameObject("QA_F49C_ActorReadinessBehaviour");
            var fixture = root.AddComponent<QaActorReadinessBehaviourFixture>();

            GameObject target = new GameObject("QA_ActorReadinessBehaviour_Target");
            target.transform.SetParent(root.transform, false);
            var readiness = target.AddComponent<ActorReadinessBehaviour>();

            SetObject(fixture, "actorReadiness", readiness);
            SetBool(fixture, "runOnStart", true);
            SetBool(fixture, "throwOnFailure", false);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[F49C_ACTOR_READINESS_BEHAVIOUR_QA] Fixture scene ready: " + ScenePath);
        }

        [MenuItem("Immersive Framework QA/Actors/Open F49C Actor Readiness Behaviour QA Scene")]
        public static void OpenActorReadinessBehaviourScene()
        {
            if (!File.Exists(ToFullPath(ScenePath)))
            {
                CreateOrRefreshActorReadinessBehaviourScene();
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

        private static void SetObject(Component component, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = FindProperty(component, propertyName);
            property.objectReferenceValue = value;
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

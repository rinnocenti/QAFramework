#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Editor
{
    internal static class ImmersiveFrameworkUnityBuildSurfaceQaSceneCreator
    {
        private const string WorkspaceFolder = "Assets/ImmersiveFrameworkQA/UnityBuildSurface";
        private const string SceneFolder = WorkspaceFolder + "/Scenes";
        private const string ScenePath = SceneFolder + "/UnityBuildSurfaceQA.unity";

        [MenuItem("Immersive Framework/QA/Unity Build Surface/Create QA Scene")]
        private static void CreateQaScene()
        {
            EnsureFolder(SceneFolder);

            if (File.Exists(ScenePath))
            {
                var existingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
                Selection.activeObject = existingScene;
                EditorGUIUtility.PingObject(existingScene);
                Debug.Log($"[Immersive Framework QA] Unity Build Surface QA scene already exists: {ScenePath}");
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.isDirty && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Immersive Framework QA] Scene creation cancelled because the current scene has unsaved changes.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "UnityBuildSurfaceQA";

            CreateSceneSkeleton();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[Immersive Framework QA] Failed to save Unity Build Surface QA scene at {ScenePath}.");
                return;
            }

            AssetDatabase.Refresh();
            var createdScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Selection.activeObject = createdScene;
            EditorGUIUtility.PingObject(createdScene);
            Debug.Log($"[Immersive Framework QA] Created Unity Build Surface QA scene: {ScenePath}");
        }

        private static void CreateSceneSkeleton()
        {
            var root = new GameObject("Unity Build Surface QA");

            CreateChild(root.transform, "Surfaces");
            CreateChild(root.transform, "Test Controls");
            CreateChild(root.transform, "Runtime Observations");
            CreateChild(root.transform, "Notes - add Transition, Loading, Pause, Save and Preferences QA objects here");

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.5f, -8f);
            cameraObject.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.04f, 0.04f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var lightObject = new GameObject("Directional Light");
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');

            if (path == "Assets" || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = Path.GetFileName(path);

            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName))
            {
                throw new IOException($"Invalid Unity asset folder path: {path}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif

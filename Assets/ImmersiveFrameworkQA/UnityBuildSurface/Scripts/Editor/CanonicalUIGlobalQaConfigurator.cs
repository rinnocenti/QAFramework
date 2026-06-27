using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface.Editor
{
    internal static class CanonicalUIGlobalQaConfigurator
    {
        private const string MenuPath = "Immersive Framework/QA/Unity Build Surface/Configure Canonical UIGlobal QA Scene";
        private const string GlobalUiScenePath = "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";
        private const string GameApplicationPath = "Assets/ImmersiveFrameworkQA/UnityBuildSurface/GameApplications/QA_TransitionGameApplication.asset";

        [MenuItem(MenuPath, priority = 260)]
        private static void Configure()
        {
            AddSceneToBuildSettings(GlobalUiScenePath);
            ConfigureGameApplication();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Immersive Framework QA] Canonical UIGlobal QA scene configured. scene='{GlobalUiScenePath}' gameApplication='{GameApplicationPath}'");
        }

        private static void ConfigureGameApplication()
        {
            var gameApplication = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(GameApplicationPath);
            if (gameApplication == null)
            {
                Debug.LogError($"[Immersive Framework QA] QA Game Application not found at '{GameApplicationPath}'.");
                return;
            }

            var serializedObject = new SerializedObject(gameApplication);
            serializedObject.FindProperty("globalUiScenePolicy").enumValueIndex = (int)GlobalUiScenePolicy.Required;
            serializedObject.FindProperty("globalUiScenePath").stringValue = GlobalUiScenePath;
            serializedObject.FindProperty("globalUiSceneName").stringValue = "QA_UIGlobal";
            serializedObject.FindProperty("transitionSurfacePolicy").enumValueIndex = (int)TransitionSurfacePolicy.Required;
            serializedObject.FindProperty("transitionSurfacePrefab").objectReferenceValue = null;
            serializedObject.FindProperty("loadingSurfacePolicy").enumValueIndex = 2; // LoadingSurfacePolicy.Required without adding another runtime using.
            serializedObject.FindProperty("loadingSurfacePrefab").objectReferenceValue = null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameApplication);
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"[Immersive Framework QA] UIGlobal scene not found at '{scenePath}'.");
                return;
            }

            var scenes = EditorBuildSettings.scenes;
            for (var i = 0; i < scenes.Length; i++)
            {
                if (scenes[i] != null && scenes[i].path == scenePath)
                {
                    if (!scenes[i].enabled)
                    {
                        scenes[i].enabled = true;
                        EditorBuildSettings.scenes = scenes;
                    }

                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            for (var i = 0; i < scenes.Length; i++)
            {
                updated[i] = scenes[i];
            }

            updated[updated.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}

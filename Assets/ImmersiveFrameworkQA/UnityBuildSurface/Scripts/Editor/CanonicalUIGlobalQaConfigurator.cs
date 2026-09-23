using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface.Editor
{
    internal static class CanonicalUIGlobalQaConfigurator
    {
        private const string MenuPath =
            "Immersive Framework/QA/Setup/Bootstrap/Configure Canonical UIGlobal QA Scene";

        private const string GlobalUiScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";

        private const string GameApplicationPath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/GameApplications/QA_TransitionGameApplication.asset";

        [MenuItem(MenuPath, priority = 260)]
        private static void Configure()
        {
            QaPersistentContentApplicationMigration
                .EnsureSceneInBuildSettings(
                    GlobalUiScenePath);

            if (!ConfigureGameApplication(
                    out string issue))
            {
                Debug.LogError(
                    "[Immersive Framework QA] Canonical Persistent Content QA configuration failed. " +
                    $"reason='{issue}'");
                return;
            }

            AssetDatabase.SaveAssets();

            Debug.Log(
                "[Immersive Framework QA] Canonical Persistent Content QA scene configured. " +
                $"scene='{GlobalUiScenePath}' " +
                $"gameApplication='{GameApplicationPath}'");
        }

        private static bool ConfigureGameApplication(
            out string issue)
        {
            GameApplicationAsset gameApplication =
                AssetDatabase.LoadAssetAtPath<
                    GameApplicationAsset>(
                    GameApplicationPath);
            if (gameApplication == null)
            {
                issue =
                    $"QA Game Application not found at '{GameApplicationPath}'.";
                return false;
            }

            SceneAsset persistentContentScene =
                AssetDatabase.LoadAssetAtPath<
                    SceneAsset>(
                    GlobalUiScenePath);
            if (persistentContentScene == null)
            {
                issue =
                    $"Persistent Content Scene not found at '{GlobalUiScenePath}'.";
                return false;
            }

            return QaPersistentContentApplicationMigration
                .TryConfigureApplication(
                    gameApplication,
                    persistentContentScene,
                    out issue);
        }
    }
}

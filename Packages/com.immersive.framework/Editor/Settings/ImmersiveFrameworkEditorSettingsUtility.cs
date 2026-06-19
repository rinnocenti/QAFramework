using System.IO;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Settings
{
    internal static class ImmersiveFrameworkEditorSettingsUtility
    {
        internal const string SettingsPath = "Assets/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset";
        internal const string GameApplicationDefaultPath = "Assets/ImmersiveFramework/GameApplication.asset";
        internal const string StartupRouteDefaultPath = "Assets/ImmersiveFramework/Routes/StartupRoute.asset";
        internal const string StartupActivityDefaultPath = "Assets/ImmersiveFramework/Activities/StartupActivity.asset";
        internal const string UsageGuidePath = "Packages/com.immersive.framework/Documentation~/Guides/Usage/index.html";

        internal static ImmersiveFrameworkSettingsAsset LoadOrCreateSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ImmersiveFrameworkSettingsAsset>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureDirectory("Assets/ImmersiveFramework/Resources");

            settings = ScriptableObject.CreateInstance<ImmersiveFrameworkSettingsAsset>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return settings;
        }

        internal static GameApplicationAsset CreateGameApplicationAsset()
        {
            EnsureDirectory("Assets/ImmersiveFramework");

            string path = AssetDatabase.GenerateUniqueAssetPath(GameApplicationDefaultPath);
            var gameApplication = ScriptableObject.CreateInstance<GameApplicationAsset>();
            AssetDatabase.CreateAsset(gameApplication, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return gameApplication;
        }

        internal static RouteAsset CreateStartupRouteAsset()
        {
            EnsureDirectory("Assets/ImmersiveFramework/Routes");

            string path = AssetDatabase.GenerateUniqueAssetPath(StartupRouteDefaultPath);
            var route = ScriptableObject.CreateInstance<RouteAsset>();
            AssetDatabase.CreateAsset(route, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return route;
        }

        internal static ActivityAsset CreateStartupActivityAsset()
        {
            EnsureDirectory("Assets/ImmersiveFramework/Activities");

            string path = AssetDatabase.GenerateUniqueAssetPath(StartupActivityDefaultPath);
            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            AssetDatabase.CreateAsset(activity, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return activity;
        }

        internal static GameApplicationAsset GetActiveGameApplication()
        {
            var settings = LoadOrCreateSettingsAsset();
            return settings != null ? settings.ActiveGameApplication : null;
        }

        internal static bool IsActiveGameApplication(GameApplicationAsset gameApplication)
        {
            return gameApplication != null && GetActiveGameApplication() == gameApplication;
        }

        internal static void AssignActiveGameApplication(GameApplicationAsset gameApplication)
        {
            if (gameApplication == null)
            {
                return;
            }

            var settings = LoadOrCreateSettingsAsset();
            if (settings == null)
            {
                return;
            }

            Undo.RecordObject(settings, "Set Active Game Application");

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("activeGameApplication").objectReferenceValue = gameApplication;
            serializedSettings.ApplyModifiedProperties();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        internal static void SelectSettingsAsset()
        {
            var settings = LoadOrCreateSettingsAsset();
            if (settings == null)
            {
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        internal static void OpenUsageGuide()
        {
            string absolutePath = Path.GetFullPath(UsageGuidePath).Replace("\\", "/");
            Application.OpenURL($"file:///{absolutePath}");
        }

        internal static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}

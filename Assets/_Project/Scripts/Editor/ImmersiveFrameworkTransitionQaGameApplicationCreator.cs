using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    internal static class ImmersiveFrameworkTransitionQaGameApplicationCreator
    {
        private const string SettingsPath = "Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset";
        private const string GameApplicationsFolder = "Assets/ImmersiveFrameworkQA/UnityBuildSurface/GameApplications";
        private const string TransitionGameApplicationPath = GameApplicationsFolder + "/QA_TransitionGameApplication.asset";
        private const string TransitionRouteAPath = "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Routes/QA_TransitionRouteA.asset";
        private const string TransitionRouteBPath = "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Routes/QA_TransitionRouteB.asset";
        private const string SettingsFolder = "Assets/_Project/Settings/ImmersiveFramework/Resources";

        [MenuItem("Immersive Framework/QA/Unity Build Surface/Create Transition QA Game Application")]
        private static void CreateTransitionQaGameApplication()
        {
            var gameApplication = CreateOrUpdateGameApplication();
            if (gameApplication == null)
            {
                return;
            }

            Selection.activeObject = gameApplication;
            EditorGUIUtility.PingObject(gameApplication);

            Debug.Log($"[Immersive Framework QA] Transition QA Game Application is ready: {TransitionGameApplicationPath}");
        }

        [MenuItem("Immersive Framework/QA/Unity Build Surface/Set Transition QA Game Application Active")]
        private static void SetTransitionQaGameApplicationActive()
        {
            var gameApplication = CreateOrUpdateGameApplication();
            if (gameApplication == null)
            {
                return;
            }

            var settings = LoadOrCreateSettingsAsset();
            if (settings == null)
            {
                Debug.LogError("[Immersive Framework QA] Could not load or create Immersive Framework settings asset.");
                return;
            }

            Undo.RecordObject(settings, "Set Transition QA Game Application Active");

            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("activeGameApplication").objectReferenceValue = gameApplication;
            serializedSettings.ApplyModifiedProperties();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);

            Debug.Log($"[Immersive Framework QA] Active Game Application set to Transition QA: {TransitionGameApplicationPath}");
        }

        private static GameApplicationAsset CreateOrUpdateGameApplication()
        {
            var transitionRouteA = AssetDatabase.LoadAssetAtPath<RouteAsset>(TransitionRouteAPath);
            if (transitionRouteA == null)
            {
                Debug.LogError($"[Immersive Framework QA] Missing Transition QA startup route: {TransitionRouteAPath}. Run 'Immersive Framework > QA > Unity Build Surface > Create Transition QA Routes and Scenes' first.");
                return null;
            }

            var transitionRouteB = AssetDatabase.LoadAssetAtPath<RouteAsset>(TransitionRouteBPath);
            if (transitionRouteB == null)
            {
                Debug.LogWarning($"[Immersive Framework QA] Transition QA alternate route is missing: {TransitionRouteBPath}. The Game Application can still boot Route A, but Route B transition validation is incomplete.");
            }

            if (!transitionRouteA.HasPrimaryScene)
            {
                Debug.LogError($"[Immersive Framework QA] Transition QA startup route has no Primary Scene: {TransitionRouteAPath}");
                return null;
            }

            EnsureDirectory(GameApplicationsFolder);

            var gameApplication = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(TransitionGameApplicationPath);
            if (gameApplication == null)
            {
                gameApplication = ScriptableObject.CreateInstance<GameApplicationAsset>();
                AssetDatabase.CreateAsset(gameApplication, TransitionGameApplicationPath);
            }

            Undo.RecordObject(gameApplication, "Configure Transition QA Game Application");

            var serializedGameApplication = new SerializedObject(gameApplication);
            serializedGameApplication.FindProperty("applicationName").stringValue = "QA Transition Game Application";
            serializedGameApplication.FindProperty("startupRoute").objectReferenceValue = transitionRouteA;
            serializedGameApplication.FindProperty("validationMode").enumValueIndex = (int)FrameworkValidationMode.Standard;
            serializedGameApplication.ApplyModifiedProperties();

            EditorUtility.SetDirty(gameApplication);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return gameApplication;
        }

        private static ImmersiveFrameworkSettingsAsset LoadOrCreateSettingsAsset()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ImmersiveFrameworkSettingsAsset>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            EnsureDirectory(SettingsFolder);

            settings = ScriptableObject.CreateInstance<ImmersiveFrameworkSettingsAsset>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return settings;
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parts = path.Split('/');
            var current = parts[0];

            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}

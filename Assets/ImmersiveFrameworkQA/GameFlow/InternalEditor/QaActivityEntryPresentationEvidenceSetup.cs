using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    internal static class QaActivityEntryPresentationEvidenceSetup
    {
        private const string Prefix = "[IF_READY_04_QA_SETUP]";
        private const string MenuRoot =
            "Immersive Framework/QA/Setup/Activity Entry Readiness/";
        private const string PrepareMenuPath =
            MenuRoot + "Prepare Presentation Evidence Regression";
        private const string PrepareDirectPoliciesMenuPath =
            MenuRoot + "Prepare Direct Readiness Policies Regression";
        private const string RestoreMenuPath = MenuRoot + "Restore Standard QA Hub";
        private const string ReportMenuPath = MenuRoot + "Report Current Startup Configuration";
        private const string PreparedKey =
            "ImmersiveFrameworkQA.IF_READY_04_QA_02.Prepared";
        private const string RestoreStandardAfterPlayKey =
            "ImmersiveFrameworkQA.IF_READY_04_QA_02.RestoreStandardAfterPlay";
        private const string DirectPoliciesPreparedKey =
            "ImmersiveFrameworkQA.IF_READY_04_QA_03.DirectPoliciesPrepared";
        private const string DirectPoliciesContentScenePath =
            "Assets/ImmersiveFrameworkQA/GameFlow/Scenes/QA_IF_READY_04_DirectPoliciesContent.unity";
        private const string DirectPoliciesContentSceneName =
            "QA_IF_READY_04_DirectPoliciesContent";
        private const string CanonicalApplicationName = "Game Application";
        private const string CanonicalRouteName = "QA Hub Route";
        private const string CanonicalPrimarySceneName = "QA_Hub";

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeRestoration()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(PrepareMenuPath, true)]
        private static bool ValidatePrepareMenu() => !EditorApplication.isPlaying;

        [MenuItem(PrepareMenuPath)]
        private static void PrepareFromMenu()
        {
            RequireEditMode("Preparation");
            try
            {
                QaActivityEntryPresentationEvidenceSetupResult result =
                    ApplyCanonicalStandard("prepare-presentation-evidence", false);
                SessionState.SetBool(PreparedKey, true);
                SessionState.EraseBool(DirectPoliciesPreparedKey);
                SessionState.SetBool(RestoreStandardAfterPlayKey, true);
                Debug.Log($"{Prefix} status='Prepared' " +
                    $"activeGameApplication='{result.CanonicalApplication.ApplicationName}' " +
                    $"startupRoute='{result.CanonicalApplication.StartupRoute.RouteName}' " +
                    $"primaryScene='{result.CanonicalApplication.StartupRoute.PrimarySceneName}' " +
                    $"editorPlayModeStartup='{FrameworkEditorPlayModeStartup.FrameworkStartup}' " +
                    "fixtureMode='RuntimeSynthetic' " +
                    "transitionExecutor='UnityFadeCurtainEffectAdapter' " +
                    "loadingExecutor='QaLoadingSurfaceVisibilityHoldAdapter' persistentAssets='None' " +
                    "next='Enter fresh Play Mode and run Activity Entry Presentation Evidence Regression'.");
            }
            catch (Exception exception)
            {
                LogFailure("Prepare", exception);
                throw;
            }
        }

        [MenuItem(PrepareDirectPoliciesMenuPath, true)]
        private static bool ValidatePrepareDirectPoliciesMenu() => !EditorApplication.isPlaying;

        [MenuItem(PrepareDirectPoliciesMenuPath)]
        private static void PrepareDirectPoliciesFromMenu()
        {
            RequireEditMode("Direct readiness policies preparation");
            try
            {
                QaActivityEntryPresentationEvidenceSetupResult result =
                    ApplyCanonicalStandard("prepare-direct-readiness-policies", false);
                SceneAsset contentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    DirectPoliciesContentScenePath);
                Require(contentScene != null,
                    $"Direct readiness policies requires Activity content scene '{DirectPoliciesContentScenePath}'.");
                Require(string.Equals(contentScene.name, DirectPoliciesContentSceneName,
                    StringComparison.Ordinal),
                    $"Direct readiness policies content scene asset must be '{DirectPoliciesContentSceneName}'.");
                Require(CountEnabledBuildSettingsScenes(DirectPoliciesContentScenePath) == 1,
                    $"Direct readiness policies requires exactly one enabled Build Settings scene '{DirectPoliciesContentScenePath}'. The setup does not modify Build Settings.");
                Scene loadedScene = SceneManager.GetSceneByPath(DirectPoliciesContentScenePath);
                Require(!loadedScene.IsValid() || !loadedScene.isLoaded,
                    $"Direct policies content scene is currently open. Close {DirectPoliciesContentSceneName} and prepare again.");
                SessionState.EraseBool(PreparedKey);
                SessionState.SetBool(DirectPoliciesPreparedKey, true);
                SessionState.SetBool(RestoreStandardAfterPlayKey, true);
                Debug.Log($"{Prefix} status='PreparedDirectPolicies' " +
                    $"activeGameApplication='{result.CanonicalApplication.ApplicationName}' " +
                    $"startupRoute='{result.CanonicalApplication.StartupRoute.RouteName}' " +
                    $"primaryScene='{result.CanonicalApplication.StartupRoute.PrimarySceneName}' " +
                    $"editorPlayModeStartup='{FrameworkEditorPlayModeStartup.FrameworkStartup}' " +
                    $"activityContentScene='{DirectPoliciesContentScenePath}' " +
                    "presentationSource='HostOwned' " +
                    "next='Enter fresh Play Mode and run Direct Activity Readiness Policies Regression'.");
            }
            catch (Exception exception)
            {
                LogFailure("PrepareDirectPolicies", exception);
                throw;
            }
        }

        [MenuItem(RestoreMenuPath, true)]
        private static bool ValidateRestoreMenu() => !EditorApplication.isPlaying;

        [MenuItem(RestoreMenuPath)]
        private static void RestoreFromMenu()
        {
            RequireEditMode("Standard QA Hub restore");
            try
            {
                QaActivityEntryPresentationEvidenceSetupResult result =
                    ApplyCanonicalStandard("restore-standard-qa-hub", false);
                ClearSessionMarkers();
                Debug.Log($"{Prefix} status='RestoredStandard' " +
                    $"activeGameApplication='{result.CanonicalApplication.ApplicationName}' " +
                    $"startupRoute='{result.CanonicalApplication.StartupRoute.RouteName}' " +
                    $"primaryScene='{result.CanonicalApplication.StartupRoute.PrimarySceneName}' " +
                    $"editorPlayModeStartup='{FrameworkEditorPlayModeStartup.FrameworkStartup}'.");
            }
            catch (Exception exception)
            {
                LogFailure("RestoreStandard", exception);
                throw;
            }
        }

        [MenuItem(ReportMenuPath)]
        private static void ReportFromMenu()
        {
            try
            {
                ImmersiveFrameworkSettingsAsset settings = ResolveSettingsAsset(out string settingsPath);
                GameApplicationAsset application = settings.ActiveGameApplication;
                RouteAsset route = application != null ? application.StartupRoute : null;
                Debug.Log($"{Prefix} status='Current' settings='{settingsPath}' " +
                    $"activeGameApplication='{DescribeApplication(application)}' " +
                    $"startupRoute='{DescribeRoute(route)}' " +
                    $"primaryScene='{DescribePrimaryScene(route)}' " +
                    $"editorPlayModeStartup='{settings.EditorPlayModeStartup}' " +
                    "fixtureMode='RuntimeSynthetic' " +
                    "transitionExecutor='UnityFadeCurtainEffectAdapter' " +
                    "loadingExecutor='QaLoadingSurfaceVisibilityHoldAdapter' persistentAssets='None' " +
                    $"prepared='{SessionState.GetBool(PreparedKey, false)}' " +
                    $"directPoliciesPrepared='{SessionState.GetBool(DirectPoliciesPreparedKey, false)}' " +
                    $"restoreAfterPlay='{SessionState.GetBool(RestoreStandardAfterPlayKey, false)}'.");
            }
            catch (Exception exception)
            {
                LogFailure("Report", exception);
                throw;
            }
        }

        internal static QaActivityEntryPresentationEvidenceSetupResult ApplyCanonicalStandard(
            string reason,
            bool markPreparedForNextPlay)
        {
            RequireEditMode("Canonical standard setup");
            ImmersiveFrameworkSettingsAsset settings = ResolveSettingsAsset(out string settingsPath);
            GameApplicationAsset canonicalApplication = ResolveCanonicalQaHubApplication();

            Undo.RecordObject(settings, $"Apply canonical QA Hub standard ({reason})");
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.Update();
            SerializedProperty activeApplication = serializedSettings.FindProperty("activeGameApplication");
            SerializedProperty editorStartup = serializedSettings.FindProperty("editorPlayModeStartup");
            Require(activeApplication != null && editorStartup != null,
                "Immersive Framework Settings serialized fields 'activeGameApplication' and " +
                "'editorPlayModeStartup' are required.");
            activeApplication.objectReferenceValue = canonicalApplication;
            editorStartup.enumValueIndex = (int)FrameworkEditorPlayModeStartup.FrameworkStartup;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ImmersiveFrameworkSettingsAsset verified = ResolveSettingsAsset(out string verifiedPath);
            Require(string.Equals(settingsPath, verifiedPath, StringComparison.Ordinal) &&
                ReferenceEquals(verified.ActiveGameApplication, canonicalApplication) &&
                verified.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.FrameworkStartup,
                "Canonical QA Hub settings were not persisted through the product authoring surface.");
            RequireCanonicalApplication(canonicalApplication);

            if (markPreparedForNextPlay)
            {
                SessionState.SetBool(PreparedKey, true);
            }

            return new QaActivityEntryPresentationEvidenceSetupResult(
                verified,
                verifiedPath,
                canonicalApplication);
        }

        internal static void RequirePreparedForCurrentPlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    $"Presentation evidence regression requires Play Mode. First run '{PrepareMenuPath}'.");
            }

            if (!SessionState.GetBool(PreparedKey, false))
            {
                throw new InvalidOperationException(
                    $"Presentation evidence regression is not prepared. Exit Play Mode, run '{PrepareMenuPath}', then enter a fresh Play Mode session.");
            }

            ImmersiveFrameworkSettingsAsset settings = ResolveSettingsAsset(out _);
            GameApplicationAsset canonicalApplication = ResolveCanonicalQaHubApplication();
            Require(ReferenceEquals(settings.ActiveGameApplication, canonicalApplication) &&
                settings.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.FrameworkStartup,
                $"Prepared presentation evidence regression requires canonical QA Hub settings. Exit Play Mode and run '{PrepareMenuPath}'.");
        }

        internal static void RequireDirectPoliciesPreparedForCurrentPlayMode()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    $"Direct readiness policies regression requires Play Mode. First run '{PrepareDirectPoliciesMenuPath}'.");
            }

            if (!SessionState.GetBool(DirectPoliciesPreparedKey, false))
            {
                throw new InvalidOperationException(
                    $"Direct readiness policies regression is not prepared. Exit Play Mode, run '{PrepareDirectPoliciesMenuPath}', then enter a fresh Play Mode session.");
            }

            ImmersiveFrameworkSettingsAsset settings = ResolveSettingsAsset(out _);
            GameApplicationAsset canonicalApplication = ResolveCanonicalQaHubApplication();
            Require(ReferenceEquals(settings.ActiveGameApplication, canonicalApplication) &&
                settings.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.FrameworkStartup &&
                canonicalApplication.StartupRoute != null &&
                string.Equals(canonicalApplication.StartupRoute.RouteName, CanonicalRouteName,
                    StringComparison.Ordinal) &&
                string.Equals(canonicalApplication.StartupRoute.PrimarySceneName,
                    CanonicalPrimarySceneName, StringComparison.Ordinal),
                $"Prepared direct readiness policies regression requires canonical QA Hub settings. Exit Play Mode and run '{PrepareDirectPoliciesMenuPath}'.");
        }

        internal static GameApplicationAsset ResolveCanonicalQaHubApplication()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameApplicationAsset");
            var matches = new List<GameApplicationAsset>();
            var candidates = new List<string>();
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                GameApplicationAsset candidate =
                    AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(path);
                if (candidate == null)
                {
                    candidates.Add($"path='{path}' application='<unloadable>' route='<none>' primaryScene='<none>'");
                    continue;
                }

                RouteAsset route = candidate.StartupRoute;
                candidates.Add($"path='{path}' application='{DescribeApplication(candidate)}' " +
                    $"route='{DescribeRoute(route)}' primaryScene='{DescribePrimaryScene(route)}'");
                if (IsCanonicalApplication(candidate))
                {
                    matches.Add(candidate);
                }
            }

            if (matches.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one canonical QA Hub Game Application, found '{matches.Count}'. " +
                    $"Candidates: {string.Join("; ", candidates)}.");
            }

            return matches[0];
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(RestoreStandardAfterPlayKey, false))
            {
                return;
            }

            try
            {
                QaActivityEntryPresentationEvidenceSetupResult result =
                    ApplyCanonicalStandard("automatic-post-play-restore", false);
                ClearSessionMarkers();
                Debug.Log($"{Prefix} status='RestoredAfterPlay' " +
                    $"activeGameApplication='{result.CanonicalApplication.ApplicationName}' " +
                    $"startupRoute='{result.CanonicalApplication.StartupRoute.RouteName}' " +
                    $"primaryScene='{result.CanonicalApplication.StartupRoute.PrimarySceneName}' " +
                    $"editorPlayModeStartup='{FrameworkEditorPlayModeStartup.FrameworkStartup}'.");
            }
            catch (Exception exception)
            {
                LogFailure("RestoreAfterPlay", exception);
            }
        }

        private static ImmersiveFrameworkSettingsAsset ResolveSettingsAsset(out string settingsPath)
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            Require(settings != null,
                $"Resources could not load ImmersiveFrameworkSettingsAsset at '{ImmersiveFrameworkSettingsAsset.ResourcesPath}'.");
            ImmersiveFrameworkSettingsAsset[] resourcesMatches =
                Resources.LoadAll<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            Require(resourcesMatches.Length == 1 && ReferenceEquals(resourcesMatches[0], settings),
                $"Expected exactly one Resources settings asset at '{ImmersiveFrameworkSettingsAsset.ResourcesPath}', " +
                $"found '{resourcesMatches.Length}'.");
            settingsPath = AssetDatabase.GetAssetPath(settings);
            Require(!string.IsNullOrWhiteSpace(settingsPath),
                "The Resources-loaded Immersive Framework Settings asset has no AssetDatabase path.");
            ImmersiveFrameworkSettingsAsset assetAtPath =
                AssetDatabase.LoadAssetAtPath<ImmersiveFrameworkSettingsAsset>(settingsPath);
            Require(ReferenceEquals(settings, assetAtPath),
                $"The Resources-loaded settings asset does not match the asset at '{settingsPath}'.");
            return settings;
        }

        private static bool IsCanonicalApplication(GameApplicationAsset candidate)
        {
            RouteAsset route = candidate != null ? candidate.StartupRoute : null;
            return candidate != null && route != null &&
                string.Equals(candidate.ApplicationName, CanonicalApplicationName,
                    StringComparison.Ordinal) &&
                string.Equals(route.RouteName, CanonicalRouteName,
                    StringComparison.Ordinal) &&
                string.Equals(route.PrimarySceneName, CanonicalPrimarySceneName,
                    StringComparison.Ordinal);
        }

        private static void RequireCanonicalApplication(GameApplicationAsset application)
        {
            Require(IsCanonicalApplication(application),
                $"Canonical Game Application must be '{CanonicalApplicationName}' with Route " +
                $"'{CanonicalRouteName}' and Primary Scene '{CanonicalPrimarySceneName}'.");
        }

        private static void RequireEditMode(string operation)
        {
            Require(!EditorApplication.isPlaying,
                $"{operation} must run outside Play Mode.");
        }

        private static void ClearSessionMarkers()
        {
            SessionState.EraseBool(PreparedKey);
            SessionState.EraseBool(DirectPoliciesPreparedKey);
            SessionState.EraseBool(RestoreStandardAfterPlayKey);
        }

        private static int CountEnabledBuildSettingsScenes(string scenePath)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            int count = 0;
            for (int index = 0; index < scenes.Length; index++)
            {
                if (scenes[index].enabled && string.Equals(scenes[index].path, scenePath,
                    StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static string DescribeApplication(GameApplicationAsset application) =>
            application == null ? "<none>" : application.ApplicationName;

        private static string DescribeRoute(RouteAsset route) =>
            route == null ? "<none>" : route.RouteName;

        private static string DescribePrimaryScene(RouteAsset route) =>
            route == null ? "<none>" : route.PrimarySceneName;

        private static void LogFailure(string operation, Exception exception)
        {
            Debug.LogError($"{Prefix} status='Failed' operation='{operation}' " +
                $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    internal readonly struct QaActivityEntryPresentationEvidenceSetupResult
    {
        public QaActivityEntryPresentationEvidenceSetupResult(
            ImmersiveFrameworkSettingsAsset settings,
            string settingsPath,
            GameApplicationAsset canonicalApplication)
        {
            Settings = settings;
            SettingsPath = settingsPath ?? string.Empty;
            CanonicalApplication = canonicalApplication;
        }

        public ImmersiveFrameworkSettingsAsset Settings { get; }
        public string SettingsPath { get; }
        public GameApplicationAsset CanonicalApplication { get; }
    }
}

using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Restores the P3M5A fixture entry point without owning a second Player
    /// Session model. The fixture consumes the active session's Supported Slots.
    /// </summary>
    public static class QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Scene Provided/Advanced/Prepare Legacy Fixture";
        private const string ScenePath =
            "Assets/ImmersiveFrameworkQA/Player/P3M5A/P3M5A_SceneLocalPlayerActivity.unity";

        [MenuItem(MenuPath, true)]
        private static bool ValidateApply()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_LIFECYCLE_FIXTURE] " +
                    "status='RejectedPlayMode' message='Exit Play Mode before applying the fixture.'.");
                return;
            }

            try
            {
                PlayerSlotProfile slot = RequireFirstSupportedSlot();
                RequireFixtureAsset(ScenePath);
                RequireFixtureAsset(
                    "Assets/ImmersiveFrameworkQA/Player/P3M5A/P3M5A_SceneLogicalPlayerActor.prefab");
                RequireFixtureAsset(
                    "Assets/ImmersiveFrameworkQA/Player/P3M5A/P3M5A_SceneActorProfile.asset");
                RequireFixtureAsset(
                    "Assets/ImmersiveFrameworkQA/Player/P3M5A/P3M5A_ActivityContent.asset");
                EnsureSceneInBuildSettings(ScenePath);
                AssetDatabase.SaveAssets();

                Debug.Log(
                    "[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_LIFECYCLE_FIXTURE] " +
                    "status='Applied' " +
                    $"slot='{slot.PlayerSlotId.StableText}' scene='{ScenePath}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_LIFECYCLE_FIXTURE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static PlayerSlotProfile RequireFirstSupportedSlot()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null || settings.ActiveGameApplication == null ||
                !ImmersiveFrameworkQA.Player.QaPlayerSessionQaSupport.TryGetSupportedSlot(
                    settings.ActiveGameApplication,
                    0,
                    out PlayerSlotProfile slot) ||
                slot == null || slot.DefaultActorProfile == null)
            {
                throw new InvalidOperationException(
                    "P3M5A requires the first Supported Slot of the active Player Session " +
                    "to define a default Actor Profile.");
            }

            return slot;
        }

        private static void RequireFixtureAsset(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) == null)
            {
                throw new InvalidOperationException(
                    $"P3M5A fixture asset is missing: '{path}'.");
            }
        }

        private static void EnsureSceneInBuildSettings(string path)
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int index = 0; index < existing.Length; index++)
            {
                if (string.Equals(existing[index].path, path, StringComparison.Ordinal))
                {
                    return;
                }
            }

            var updated = new EditorBuildSettingsScene[existing.Length + 1];
            Array.Copy(existing, updated, existing.Length);
            updated[updated.Length - 1] = new EditorBuildSettingsScene(path, true);
            EditorBuildSettings.scenes = updated;
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

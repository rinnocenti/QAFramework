using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public enum QaPlayerSessionBootProfile
    {
        ManagerProvisioned = 10,
        SceneProvided = 20
    }

    /// <summary>
    /// Selects the canonical persisted Player Session before a fresh QA Play Mode boot.
    /// It never mutates the Session policy after Framework bootstrap.
    /// </summary>
    public static class QaPlayerSessionBootProfileGuard
    {
        private const string Prefix = "[QA_PLAYER_BOOT_PROFILE]";

        public static void UseManagerProvisioned() =>
            Prepare(QaPlayerSessionBootProfile.ManagerProvisioned);

        public static void UseSceneProvided() =>
            Prepare(QaPlayerSessionBootProfile.SceneProvided);

        private static void Prepare(QaPlayerSessionBootProfile bootProfile)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player Session boot profile can only be selected in Edit Mode before Framework bootstrap.");
            }

            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            GameApplicationAsset application =
                settings != null ? settings.ActiveGameApplication : null;
            if (settings == null || application == null)
            {
                throw new InvalidOperationException(
                    "Player Session boot profile requires the active canonical Game Application.");
            }

            string profilePath = bootProfile == QaPlayerSessionBootProfile.SceneProvided
                ? PlayerQaPaths.SceneSessionPath
                : PlayerQaPaths.ManagerSessionPath;
            PlayerHostProvisioningMode expectedProvisioning =
                bootProfile == QaPlayerSessionBootProfile.SceneProvided
                    ? PlayerHostProvisioningMode.SceneProvided
                    : PlayerHostProvisioningMode.ManagerProvisioned;
            PlayerSessionProfile profile =
                AssetDatabase.LoadAssetAtPath<PlayerSessionProfile>(profilePath);
            if (profile == null)
            {
                throw new InvalidOperationException(
                    $"Canonical Player Session boot profile is missing at '{profilePath}'.");
            }

            string profileIssue = string.Empty;
            if (!profile.TryValidate(out profileIssue) ||
                profile.HostProvisioning != expectedProvisioning ||
                profile.ActorResolutionPolicy !=
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault)
            {
                throw new InvalidOperationException(
                    $"Canonical Player Session '{profile.name}' is invalid for boot '{bootProfile}'. " +
                    $"expectedProvisioning='{expectedProvisioning}' " +
                    $"actualProvisioning='{profile.HostProvisioning}' {profileIssue}");
            }

            var serialized = new SerializedObject(application);
            SerializedProperty enabled = serialized.FindProperty("playerSessionEnabled");
            SerializedProperty selectedProfile =
                serialized.FindProperty("defaultPlayerSessionProfile");
            if (enabled == null || selectedProfile == null)
            {
                throw new InvalidOperationException(
                    "Active Game Application does not expose the canonical Player Session configuration.");
            }

            enabled.boolValue = true;
            selectedProfile.objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            AssetDatabase.SaveAssetIfDirty(application);

            if (!application.PlayerSessionEnabled ||
                !ReferenceEquals(application.DefaultPlayerSessionProfile, profile))
            {
                throw new InvalidOperationException(
                    $"Player Session boot profile '{bootProfile}' was not persisted on the active Game Application.");
            }

            Debug.Log(
                $"{Prefix} status='Prepared' boot='{bootProfile}' " +
                $"session='{profile.name}' provisioning='{profile.HostProvisioning}'.");
        }
    }
}

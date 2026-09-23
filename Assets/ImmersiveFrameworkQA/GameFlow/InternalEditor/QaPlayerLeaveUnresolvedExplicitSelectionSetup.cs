using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Prepares the existing public Manager-Provisioned surface with the
    /// immutable Session policy that requires an explicit Actor selection.
    /// </summary>
public static class QaPlayerLeaveUnresolvedExplicitSelectionSetup
    {
        private const string Prefix = "[QA_PLAYER_LEAVE_UNRESOLVED_SETUP]";
        private const string PreparedKey =
            "ImmersiveFrameworkQA.QA_PLAYER_LEAVE_UNRESOLVED.Prepared";
        private const string RestoreAfterPlayKey =
            "ImmersiveFrameworkQA.QA_PLAYER_LEAVE_UNRESOLVED.RestoreAfterPlay";

        [InitializeOnLoadMethod]
        private static void RegisterRestoration()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

    public static void PrepareForFullPlayerQa()
        {
            Require(!EditorApplication.isPlaying,
                "LeaveUnresolved Player QA setup must run in Edit Mode.");

            SessionState.EraseBool(PreparedKey);
            QaPlayerSurfacePublicNavigationSetup.PrepareForCertification();

            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            GameApplicationAsset application = settings != null
                ? settings.ActiveGameApplication
                : null;
            PlayerSessionProfile session = application != null
                ? application.DefaultPlayerSessionProfile
                : null;
            Require(settings != null && application != null && session != null,
                "LeaveUnresolved Player QA requires the prepared active Player Session.");
            Require(session.HostProvisioning ==
                    PlayerHostProvisioningMode.ManagerProvisioned &&
                session.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                "LeaveUnresolved Player QA must start from the canonical Manager-Provisioned default-resolution fixture.");

            var serializedSession = new SerializedObject(session);
            SerializedProperty policy = serializedSession.FindProperty(
                "actorResolutionPolicy");
            Require(policy != null,
                "PlayerSessionProfile actorResolutionPolicy field is missing.");
            policy.intValue = (int)PlayerActorResolutionPolicy.LeaveUnresolved;
            serializedSession.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(session);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Require(session.TryValidate(out string sessionIssue), sessionIssue);
            Require(session.HostProvisioning ==
                    PlayerHostProvisioningMode.ManagerProvisioned &&
                session.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.LeaveUnresolved,
                "LeaveUnresolved Player QA did not persist the required effective Session policy.");

            SessionState.SetBool(PreparedKey, true);
            SessionState.SetBool(RestoreAfterPlayKey, true);
            Debug.Log(
                $"{Prefix} status='Prepared' session='{session.name}' " +
                $"hostProvisioning='{session.HostProvisioning}' " +
                $"actorResolution='{session.ActorResolutionPolicy}'.");
        }

        internal static void RequirePreparedForCurrentPlayMode()
        {
            Require(EditorApplication.isPlaying,
                "LeaveUnresolved Player QA requires Play Mode.");
            Require(SessionState.GetBool(PreparedKey, false),
                "LeaveUnresolved Player QA fixture was not prepared in Edit Mode.");

            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            PlayerSessionProfile session = settings != null &&
                settings.ActiveGameApplication != null
                ? settings.ActiveGameApplication.DefaultPlayerSessionProfile
                : null;
            Require(session != null &&
                session.HostProvisioning ==
                    PlayerHostProvisioningMode.ManagerProvisioned &&
                session.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.LeaveUnresolved,
                "LeaveUnresolved Player QA Play Mode did not retain its immutable Session policy.");
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(RestoreAfterPlayKey, false))
            {
                return;
            }

            try
            {
                QaPlayerSurfacePublicNavigationSetup.PrepareForCertification();
                SessionState.EraseBool(PreparedKey);
                SessionState.EraseBool(RestoreAfterPlayKey);
                Debug.Log($"{Prefix} status='Restored' actorResolution='ResolveConfiguredDefault'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='RestoreFailed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}

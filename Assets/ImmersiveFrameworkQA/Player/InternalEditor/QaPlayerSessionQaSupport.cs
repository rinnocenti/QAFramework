using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player
{
    public static class QaPlayerSessionQaSupport
    {
        public static bool TryGetSupportedSlot(
            GameApplicationAsset application,
            int configuredIndex,
            out PlayerSlotProfile playerSlot)
        {
            playerSlot = null;
            if (!TryResolveProfile(application, out PlayerSessionProfile profile, out _) ||
                configuredIndex < 0 ||
                configuredIndex >= profile.SupportedSlots.Count)
            {
                return false;
            }

            playerSlot = profile.SupportedSlots[configuredIndex];
            return playerSlot != null;
        }

        public static bool TryResolveProfile(
            GameApplicationAsset application,
            out PlayerSessionProfile profile,
            out string issue)
        {
            profile = application != null
                ? application.DefaultPlayerSessionProfile
                : null;
            if (profile == null)
            {
                issue = "Active Game Application has no PlayerSessionProfile.";
                return false;
            }

            if (!application.PlayerSessionEnabled)
            {
                issue = "Active Game Application has Player Session disabled.";
                profile = null;
                return false;
            }

            if (!profile.TryValidate(out issue))
            {
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public static bool TryValidateManagerBridge(
            PlayerSessionProfile profile,
            PlayerInputManager manager,
            out string issue)
        {
            if (profile == null)
            {
                issue = "Manager-Provisioned fixture has no PlayerSessionProfile.";
                return false;
            }

            if (!profile.TryValidate(out issue))
            {
                return false;
            }

            if (manager == null)
            {
                issue = "Manager-Provisioned fixture has no PlayerInputManager.";
                return false;
            }

            if (manager.maxPlayerCount != profile.SupportedSlotCount)
            {
                issue =
                    $"PlayerInputManager '{manager.name}' limit '{manager.maxPlayerCount}' " +
                    $"does not match PlayerSessionProfile Supported Slots '{profile.SupportedSlotCount}'.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        public static void ConfigureManagerBridge(
            PlayerSessionProfile profile,
            PlayerInputManager manager)
        {
            string profileIssue = string.Empty;
            if (profile == null || !profile.TryValidate(out profileIssue))
            {
                throw new System.InvalidOperationException(
                    "PlayerInputManager bridge requires a valid PlayerSessionProfile. " +
                    profileIssue);
            }

            if (manager == null)
            {
                throw new System.ArgumentNullException(nameof(manager));
            }

            var serializedManager = new SerializedObject(manager);
            SerializedProperty limit = serializedManager.FindProperty(
                "m_MaxPlayerCount");
            if (limit == null)
            {
                throw new System.InvalidOperationException(
                    "PlayerInputManager serialized max-player-count field was not found.");
            }

            limit.intValue = profile.SupportedSlotCount;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

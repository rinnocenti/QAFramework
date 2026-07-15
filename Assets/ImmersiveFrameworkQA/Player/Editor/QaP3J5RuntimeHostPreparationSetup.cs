using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent P3J.5 fixture. Reuses the canonical P3G.4 technical host and P3H.4
    /// Actor-selection assets; it does not create a second PlayerInputManager or GameApplication.
    /// </summary>
    internal static class QaP3J5RuntimeHostPreparationSetup
    {

        internal static void Apply()
        {
            try
            {
                QaP3H4RuntimeHostActorSelectionSetup.Apply();

                ImmersiveFrameworkSettingsAsset settings =
                    Resources.Load<ImmersiveFrameworkSettingsAsset>(
                        ImmersiveFrameworkSettingsAsset.ResourcesPath);
                if (settings == null || settings.ActiveGameApplication == null)
                {
                    throw new InvalidOperationException(
                        "Active Game Application was not resolved after P3H.4 fixture application.");
                }

                GameApplicationAsset gameApplication = settings.ActiveGameApplication;
                if (!gameApplication.TryGetLocalPlayerSlot(0, out PlayerSlotProfile firstSlot) ||
                    firstSlot == null || firstSlot.DefaultActorProfile == null)
                {
                    throw new InvalidOperationException(
                        "P3J.5 requires the first Local Player Slot to have an explicit default ActorProfile.");
                }

                if (firstSlot.DefaultActorProfile.LogicalActorHostPrefab == null)
                {
                    throw new InvalidOperationException(
                        "P3J.5 default ActorProfile has no Logical Actor Host prefab.");
                }

                Debug.Log(
                    "[P3J5_RUNTIME_HOST_PREPARATION_FIXTURE] status='Applied' " +
                    $"gameApplication='{gameApplication.name}' " +
                    $"slot='{firstSlot.PlayerSlotId.StableText}' " +
                    $"defaultActor='{firstSlot.DefaultActorProfile.ActorProfileId.StableText}' " +
                    $"logicalActorPrefab='{firstSlot.DefaultActorProfile.LogicalActorHostPrefab.name}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3J5_RUNTIME_HOST_PREPARATION_FIXTURE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3LocalPlayerProvisioningValidationSmoke
    {
        [MenuItem("Immersive Framework/QA/Player/P3 Local Player Provisioning Validation Smoke")]
        private static void Run()
        {
            var created = new List<UnityEngine.Object>();
            try
            {
                LocalPlayerProvisioningValidationResult missing =
                    LocalPlayerProvisioningConfigurationRules.Validate(
                        Array.Empty<LocalPlayerProvisioningAuthoring>(), true,
                        nameof(QaP3LocalPlayerProvisioningValidationSmoke), "missing-surface");
                RequireIssue(missing, LocalPlayerProvisioningIssueKind.MissingRequiredSurface);

                LocalPlayerProvisioningAuthoring valid = CreateSurface("Valid", false, created);
                LocalPlayerProvisioningValidationResult accepted =
                    LocalPlayerProvisioningConfigurationRules.Validate(
                        new[] { valid }, true,
                        nameof(QaP3LocalPlayerProvisioningValidationSmoke), "valid-surface");
                if (!accepted.Succeeded || !accepted.Available)
                    throw new InvalidOperationException(accepted.ToDiagnosticString());

                LocalPlayerProvisioningAuthoring duplicate = CreateSurface("Duplicate", false, created);
                LocalPlayerProvisioningValidationResult duplicates =
                    LocalPlayerProvisioningConfigurationRules.Validate(
                        new[] { valid, duplicate }, true,
                        nameof(QaP3LocalPlayerProvisioningValidationSmoke), "duplicate-surfaces");
                RequireIssue(duplicates, LocalPlayerProvisioningIssueKind.DuplicateSurface);

                LocalPlayerProvisioningAuthoring divergent = CreateSurface("Divergent", true, created);
                LocalPlayerProvisioningValidationResult divergence =
                    LocalPlayerProvisioningConfigurationRules.Validate(
                        new[] { divergent }, true,
                        nameof(QaP3LocalPlayerProvisioningValidationSmoke), "divergent-manager");
                RequireIssue(divergence, LocalPlayerProvisioningIssueKind.DivergentPlayerInputManager);

                Debug.Log("[P3_LOCAL_PLAYER_PROVISIONING_VALIDATION_SMOKE] status='Passed' cases='4'.");
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                    if (created[index] != null) UnityEngine.Object.DestroyImmediate(created[index]);
            }
        }

        private static LocalPlayerProvisioningAuthoring CreateSurface(
            string label,
            bool divergentManager,
            List<UnityEngine.Object> created)
        {
            GameObject surfaceObject = new GameObject($"P3 Provisioning {label}");
            created.Add(surfaceObject);
            GameObject managerObject = divergentManager
                ? new GameObject($"P3 Manager {label}")
                : surfaceObject;
            if (divergentManager) created.Add(managerObject);

            PlayerInputManager manager = managerObject.AddComponent<PlayerInputManager>();
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            var managerSerialized = new SerializedObject(manager);
            SerializedProperty maxPlayerCountProperty =
                managerSerialized.FindProperty("m_MaxPlayerCount");
            if (maxPlayerCountProperty == null)
            {
                throw new InvalidOperationException(
                    "PlayerInputManager serialized max-player-count property was not found.");
            }

            maxPlayerCountProperty.intValue = 2;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = new GameObject($"P3 Host Prefab {label}");
            created.Add(prefab);
            PlayerInput playerInput = prefab.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = prefab.AddComponent<LocalPlayerHostAuthoring>();
            GameObject mount = new GameObject("Actor Mount");
            mount.transform.SetParent(prefab.transform, false);
            created.Add(mount);
            var hostSerialized = new SerializedObject(host);
            hostSerialized.FindProperty("playerInput").objectReferenceValue = playerInput;
            hostSerialized.FindProperty("actorMount").objectReferenceValue = mount.transform;
            hostSerialized.ApplyModifiedPropertiesWithoutUndo();
            manager.playerPrefab = prefab;

            LocalPlayerProvisioningAuthoring surface =
                surfaceObject.AddComponent<LocalPlayerProvisioningAuthoring>();
            var serialized = new SerializedObject(surface);
            serialized.FindProperty("playerInputManager").objectReferenceValue = manager;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return surface;
        }

        private static void RequireIssue(
            LocalPlayerProvisioningValidationResult result,
            LocalPlayerProvisioningIssueKind expected)
        {
            for (int index = 0; index < result.Issues.Count; index++)
                if (result.Issues[index].Kind == expected) return;
            throw new InvalidOperationException(
                $"Expected provisioning issue '{expected}'. {result.ToDiagnosticString()}");
        }
    }
}

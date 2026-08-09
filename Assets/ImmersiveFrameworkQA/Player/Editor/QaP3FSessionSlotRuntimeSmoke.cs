using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Edit Mode proof of the authored Player Session product contract. Runtime
    /// Join behavior is covered through the public Q1/Q2 provisioning surfaces.
    /// </summary>
    internal static class QaPlayerSessionContractRegression
    {
        private const string Prefix = "[P3F_PLAYER_SESSION_CONTRACT]";
        private const int ExpectedCaseCount = 6;

        private static readonly string[] ExpectedCases =
        {
            "profile-resolves-four-authoring-concerns",
            "supported-slots-define-structural-universe",
            "manager-host-provisioning-is-uniform",
            "scene-host-provisioning-is-uniform",
            "actor-resolution-policies-remain-distinct",
            "effective-configuration-remains-frozen"
        };

        [MenuItem("Immersive Framework/QA/Player/Session/Run Session Contract")]
        internal static void Run()
        {
            var created = new List<UnityEngine.Object>();
            var completed = new List<string>();

            try
            {
                Require(
                    !EditorApplication.isPlayingOrWillChangePlaymode,
                    "Player Session contract regression must run in Edit Mode.");

                PlayerSlotProfile first = CreateSlot(created, "QA First Slot", "qa.r4.first");
                PlayerSlotProfile second = CreateSlot(created, "QA Second Slot", "qa.r4.second");
                PlayerSlotProfile third = CreateSlot(created, "QA Third Slot", "qa.r4.third");
                PlayerSessionProfile managerProfile = CreateSession(
                    created,
                    "QA Manager Player Session",
                    new[] { first, second, third },
                    true,
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault);
                PlayerSessionInitializationResult managerResolution =
                    PlayerSessionConfigurationResolver.Resolve(managerProfile);
                RequireSucceeded(managerResolution, "Manager-Provisioned profile");
                EffectivePlayerSessionConfiguration managerConfiguration =
                    managerResolution.Configuration;
                Require(
                    managerConfiguration.InitialJoiningOpen &&
                    managerConfiguration.HostProvisioning == PlayerHostProvisioningMode.ManagerProvisioned &&
                    managerConfiguration.ActorResolutionPolicy == PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                    "Effective configuration did not retain all Player Session authoring concerns.");
                completed.Add(ExpectedCases[0]);

                Require(
                    managerConfiguration.SupportedSlotCount == 3 &&
                    managerConfiguration.Slots.Count == 3 &&
                    managerConfiguration.Slots[0].PlayerSlotId == first.PlayerSlotId &&
                    managerConfiguration.Slots[1].PlayerSlotId == second.PlayerSlotId &&
                    managerConfiguration.Slots[2].PlayerSlotId == third.PlayerSlotId,
                    "Supported Slots did not define the canonical structural universe and order.");
                completed.Add(ExpectedCases[1]);

                RequireUniformHostProvisioning(managerConfiguration, PlayerHostProvisioningMode.ManagerProvisioned, "Manager-Provisioned");
                completed.Add(ExpectedCases[2]);

                PlayerSessionProfile sceneProfile = CreateSession(
                    created,
                    "QA Scene Player Session",
                    new[] { first, second },
                    false,
                    PlayerHostProvisioningMode.SceneProvided,
                    PlayerActorResolutionPolicy.LeaveUnresolved);
                PlayerSessionInitializationResult sceneResolution =
                    PlayerSessionConfigurationResolver.Resolve(sceneProfile);
                RequireSucceeded(sceneResolution, "Scene-Provided profile");
                RequireUniformHostProvisioning(sceneResolution.Configuration, PlayerHostProvisioningMode.SceneProvided, "Scene-Provided");
                completed.Add(ExpectedCases[3]);

                Require(
                    managerConfiguration.ActorResolutionPolicy != sceneResolution.Configuration.ActorResolutionPolicy &&
                    sceneResolution.Configuration.ActorResolutionPolicy == PlayerActorResolutionPolicy.LeaveUnresolved,
                    "Actor Resolution policies were not preserved as distinct Session configuration.");
                completed.Add(ExpectedCases[4]);

                ConfigureSession(
                    managerProfile,
                    new[] { third, second, first },
                    false,
                    PlayerHostProvisioningMode.SceneProvided,
                    PlayerActorResolutionPolicy.LeaveUnresolved);
                PlayerSessionInitializationResult reResolved =
                    PlayerSessionConfigurationResolver.Resolve(managerProfile);
                RequireSucceeded(reResolved, "Re-resolved edited profile");
                Require(
                    managerConfiguration.SupportedSlotCount == 3 &&
                    managerConfiguration.Slots[0].PlayerSlotId == first.PlayerSlotId &&
                    managerConfiguration.InitialJoiningOpen &&
                    managerConfiguration.HostProvisioning == PlayerHostProvisioningMode.ManagerProvisioned &&
                    managerConfiguration.ActorResolutionPolicy == PlayerActorResolutionPolicy.ResolveConfiguredDefault &&
                    reResolved.Configuration.Slots[0].PlayerSlotId == third.PlayerSlotId &&
                    !reResolved.Configuration.InitialJoiningOpen &&
                    reResolved.Configuration.HostProvisioning == PlayerHostProvisioningMode.SceneProvided &&
                    reResolved.Configuration.ActorResolutionPolicy == PlayerActorResolutionPolicy.LeaveUnresolved,
                    "Editing PlayerSessionProfile rewrote existing effective Session evidence.");
                completed.Add(ExpectedCases[5]);

                Require(completed.Count == ExpectedCaseCount, "Player Session contract case count changed unexpectedly.");
                Debug.Log($"{Prefix} status='Passed' verdict='StaticContractComplete' cases='{completed.Count}/{ExpectedCaseCount}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{Prefix} status='Failed' verdict='StaticContractFailed' cases='{completed.Count}/{ExpectedCaseCount}' next='{NextCase(completed)}' completed='{string.Join(",", completed)}' missing='{Escape(exception.Message)}'.");
                throw;
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private static PlayerSlotProfile CreateSlot(ICollection<UnityEngine.Object> created, string name, string playerSlotId)
        {
            var slot = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            slot.name = name;
            var serialized = new SerializedObject(slot);
            serialized.FindProperty("playerSlotId").stringValue = playerSlotId;
            serialized.FindProperty("displayName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(slot);
            return slot;
        }

        private static PlayerSessionProfile CreateSession(ICollection<UnityEngine.Object> created, string name, PlayerSlotProfile[] slots, bool initialJoiningOpen, PlayerHostProvisioningMode hostProvisioning, PlayerActorResolutionPolicy actorResolutionPolicy)
        {
            var session = ScriptableObject.CreateInstance<PlayerSessionProfile>();
            session.name = name;
            ConfigureSession(session, slots, initialJoiningOpen, hostProvisioning, actorResolutionPolicy);
            created.Add(session);
            return session;
        }

        private static void ConfigureSession(PlayerSessionProfile session, PlayerSlotProfile[] slots, bool initialJoiningOpen, PlayerHostProvisioningMode hostProvisioning, PlayerActorResolutionPolicy actorResolutionPolicy)
        {
            var serialized = new SerializedObject(session);
            SerializedProperty supportedSlots = serialized.FindProperty("supportedSlots");
            supportedSlots.arraySize = slots.Length;
            for (int index = 0; index < slots.Length; index++)
            {
                supportedSlots.GetArrayElementAtIndex(index).objectReferenceValue = slots[index];
            }

            serialized.FindProperty("initialJoiningOpen").boolValue = initialJoiningOpen;
            serialized.FindProperty("hostProvisioning").intValue = (int)hostProvisioning;
            serialized.FindProperty("actorResolutionPolicy").intValue = (int)actorResolutionPolicy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RequireUniformHostProvisioning(EffectivePlayerSessionConfiguration configuration, PlayerHostProvisioningMode expected, string label)
        {
            Require(configuration.HostProvisioning == expected, label + " effective Session Host Provisioning is invalid.");
            for (int index = 0; index < configuration.Slots.Count; index++)
            {
                Require(configuration.Slots[index].HostProvisioningMode == expected, label + " resolved a divergent per-Slot Host Provisioning value.");
            }
        }

        private static void RequireSucceeded(PlayerSessionInitializationResult result, string label)
        {
            Require(result != null && result.Succeeded && result.Configuration != null, label + " did not resolve. " + (result != null ? result.Message : string.Empty));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string NextCase(IReadOnlyList<string> completed)
        {
            return completed.Count < ExpectedCases.Length ? ExpectedCases[completed.Count] : string.Empty;
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

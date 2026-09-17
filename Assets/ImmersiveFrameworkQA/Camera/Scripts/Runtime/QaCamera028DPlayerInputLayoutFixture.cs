using System;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// Authored CAMERA-028-D references. The fixture describes the exact Session
    /// provisioning, Player Slots, Camera Outputs and PlayerInput layout owner; the
    /// regression remains responsible for commands and assertions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QaCamera028DPlayerInputLayoutFixture : MonoBehaviour
    {
        [SerializeField] private LocalPlayerProvisioningAuthoring provisioning;
        [SerializeField] private PlayerInputManager playerInputManager;
        [SerializeField] private PlayerCameraOutputPolicyAuthoring outputPolicy;
        [SerializeField] private CameraViewOutputPolicyAuthoring viewOutputPolicy;
        [SerializeField] private PlayerSlotProfile slotP1;
        [SerializeField] private PlayerSlotProfile slotP2;
        [SerializeField] private CameraOutputAuthoring outputA;
        [SerializeField] private CameraOutputAuthoring outputB;
        [SerializeField] private bool completeSlotCoverage;

        public LocalPlayerProvisioningAuthoring Provisioning => provisioning;
        public PlayerInputManager PlayerInputManager => playerInputManager;
        public PlayerCameraOutputPolicyAuthoring OutputPolicy => outputPolicy;
        public CameraViewOutputPolicyAuthoring ViewOutputPolicy => viewOutputPolicy;
        public PlayerSlotProfile SlotP1 => slotP1;
        public PlayerSlotProfile SlotP2 => slotP2;
        public CameraOutputAuthoring OutputA => outputA;
        public CameraOutputAuthoring OutputB => outputB;
        public bool CompleteSlotCoverage => completeSlotCoverage;

        public void Configure(
            LocalPlayerProvisioningAuthoring authoredProvisioning,
            PlayerInputManager authoredPlayerInputManager,
            PlayerCameraOutputPolicyAuthoring authoredOutputPolicy,
            CameraViewOutputPolicyAuthoring authoredViewOutputPolicy,
            PlayerSlotProfile authoredSlotP1,
            PlayerSlotProfile authoredSlotP2,
            CameraOutputAuthoring authoredOutputA,
            CameraOutputAuthoring authoredOutputB,
            bool hasCompleteSlotCoverage)
        {
            provisioning = authoredProvisioning;
            playerInputManager = authoredPlayerInputManager;
            outputPolicy = authoredOutputPolicy;
            viewOutputPolicy = authoredViewOutputPolicy;
            slotP1 = authoredSlotP1;
            slotP2 = authoredSlotP2;
            outputA = authoredOutputA;
            outputB = authoredOutputB;
            completeSlotCoverage = hasCompleteSlotCoverage;
        }

        public bool TryValidateAuthoredSurface(out string issue)
        {
            if (provisioning == null || playerInputManager == null ||
                outputPolicy == null || slotP1 == null || slotP2 == null ||
                viewOutputPolicy == null || outputA == null || outputB == null)
            {
                issue =
                    "CAMERA-028-D fixture requires provisioning, PlayerInputManager, policy, two Player Slots and two Camera Outputs.";
                return false;
            }

            if (!ReferenceEquals(provisioning.PlayerInputManager, playerInputManager))
            {
                issue =
                    "CAMERA-028-D fixture provisioning does not retain the exact authored PlayerInputManager.";
                return false;
            }

            if (!playerInputManager.splitScreen ||
                playerInputManager.maxPlayerCount < 2)
            {
                issue =
                    "CAMERA-028-D requires PlayerInputManager splitScreen enabled with capacity for at least two Players.";
                return false;
            }

            if (!slotP1.PlayerSlotId.IsValid || !slotP2.PlayerSlotId.IsValid ||
                slotP1.PlayerSlotId == slotP2.PlayerSlotId)
            {
                issue =
                    "CAMERA-028-D fixture requires two distinct typed Player Slot identities.";
                return false;
            }

            if (!outputA.OutputId.IsValid || !outputB.OutputId.IsValid ||
                outputA.OutputId == outputB.OutputId ||
                outputA.UnityCamera == null || outputB.UnityCamera == null ||
                ReferenceEquals(outputA.UnityCamera, outputB.UnityCamera))
            {
                issue =
                    "CAMERA-028-D fixture requires two distinct typed Camera Outputs with distinct Unity Cameras.";
                return false;
            }

            if (provisioning.gameObject.scene != gameObject.scene ||
                playerInputManager.gameObject.scene != gameObject.scene ||
                outputA.gameObject.scene != gameObject.scene ||
                outputB.gameObject.scene != gameObject.scene ||
                outputPolicy.gameObject.scene != gameObject.scene ||
                viewOutputPolicy.gameObject.scene != gameObject.scene)
            {
                issue =
                    "CAMERA-028-D fixture references must belong to the same authored persistent-content scene.";
                return false;
            }

            if (viewOutputPolicy.Bindings == null ||
                viewOutputPolicy.Bindings.Count != 1 ||
                viewOutputPolicy.Bindings[0] == null ||
                !ReferenceEquals(
                    viewOutputPolicy.Bindings[0].OutputDefinition,
                    outputB.OutputDefinition))
            {
                issue =
                    "CAMERA-028-D requires the exact authored View association to Output B.";
                return false;
            }

            int expectedBindingCount = completeSlotCoverage ? 2 : 1;
            if (outputPolicy.Bindings == null ||
                outputPolicy.Bindings.Count != expectedBindingCount)
            {
                issue =
                    $"CAMERA-028-D policy requires '{expectedBindingCount}' explicit binding(s); found '{outputPolicy.Bindings?.Count ?? 0}'.";
                return false;
            }

            if (!HasExactBinding(slotP1, outputB.OutputDefinition))
            {
                issue =
                    "CAMERA-028-D inverted policy must bind Slot P1 to exact Output B.";
                return false;
            }

            bool hasP2ToA = HasExactBinding(slotP2, outputA.OutputDefinition);
            if (hasP2ToA != completeSlotCoverage)
            {
                issue = completeSlotCoverage
                    ? "CAMERA-028-D complete policy must bind Slot P2 to exact Output A."
                    : "CAMERA-028-D incomplete policy must deliberately omit Slot P2.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool HasExactBinding(
            PlayerSlotProfile slot,
            Immersive.Framework.CameraAuthoring.CameraOutputDefinition output)
        {
            for (int index = 0; index < outputPolicy.Bindings.Count; index++)
            {
                PlayerCameraOutputBindingAuthoring binding =
                    outputPolicy.Bindings[index];
                if (binding != null &&
                    ReferenceEquals(binding.PlayerSlotProfile, slot) &&
                    ReferenceEquals(binding.OutputDefinition, output))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

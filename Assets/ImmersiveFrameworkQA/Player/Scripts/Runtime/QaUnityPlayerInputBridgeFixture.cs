using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerBinding;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerViews;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player
{
    public sealed class QaUnityPlayerInputBridgeFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F52B_UNITY_PLAYERINPUT_BRIDGE_QA]";

        [SerializeField] private GameObject validationRoot;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
        [SerializeField] private PlayerControlBehaviour controlBehaviour;
        [SerializeField] private PlayerControlBindingTargetBehaviour controlBindingTarget;
        [SerializeField] private UnityPlayerInputBridgeTargetBehaviour bridgeTarget;
        [SerializeField] private UnityPlayerInputBridgeTargetBehaviour slotMismatchBridgeTarget;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("Run F52B Unity PlayerInput Bridge Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            PrepareReadyAuthoringState();

            bool bridge = ValidateSuccessfulBridge();
            bool missingControlBindingTarget = ValidateMissingControlBindingTarget();
            bool missingControlBinding = ValidateMissingControlBinding();
            bool missingBridgeTarget = ValidateMissingBridgeTarget();
            bool missingPlayerInput = ValidateMissingPlayerInput();
            bool slotMismatch = ValidateSlotMismatch();
            bool clearNoOp = ValidateClearNoOp();
            bool clearAfterBridge = ValidateClearAfterBridge();
            bool passiveBoundary = ValidatePassiveBoundary();

            bool passed = component
                && bridge
                && missingControlBindingTarget
                && missingControlBinding
                && missingBridgeTarget
                && missingPlayerInput
                && slotMismatch
                && clearNoOp
                && clearAfterBridge
                && passiveBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' component='{component}' bridge='{bridge}' missingControlBindingTarget='{missingControlBindingTarget}' missingControlBinding='{missingControlBinding}' missingBridgeTarget='{missingBridgeTarget}' missingPlayerInput='{missingPlayerInput}' slotMismatch='{slotMismatch}' clearNoOp='{clearNoOp}' clearAfterBridge='{clearAfterBridge}' passiveBoundary='{passiveBoundary}'.";

            if (passed)
            {
                Debug.Log(message, this);
                return;
            }

            Debug.LogError(message, this);
            if (throwOnFailure)
            {
                throw new InvalidOperationException(message);
            }
        }

        private bool ValidateComponentReferences()
        {
            bool passed = validationRoot != null
                && actorReadinessBehaviour != null
                && entryBehaviour != null
                && viewBehaviour != null
                && controlBehaviour != null
                && controlBindingTarget != null
                && bridgeTarget != null
                && slotMismatchBridgeTarget != null
                && playerInput != null;

            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private void PrepareReadyAuthoringState()
        {
            actorReadinessBehaviour.BeginNewCycle("qa.unity-playerinput-bridge.prepare.begin-cycle");
            actorReadinessBehaviour.MarkReadyForControl("qa.unity-playerinput-bridge.prepare.ready-for-control");

            entryBehaviour.RebuildEntry("qa.unity-playerinput-bridge.prepare.entry-rebuild");
            entryBehaviour.SetState(PlayerEntryState.Active, "qa.unity-playerinput-bridge.prepare.entry-active");

            viewBehaviour.RebuildView("qa.unity-playerinput-bridge.prepare.view-rebuild");
            viewBehaviour.RefreshPlayerEntryEvidence("qa.unity-playerinput-bridge.prepare.view-refresh-entry");
            viewBehaviour.SetState(PlayerViewState.Active, "qa.unity-playerinput-bridge.prepare.view-active");

            controlBehaviour.RebuildControl("qa.unity-playerinput-bridge.prepare.control-rebuild");
            controlBehaviour.RefreshPlayerEntryFromBehaviour("qa.unity-playerinput-bridge.prepare.control-refresh-entry");
            controlBehaviour.SetState(PlayerControlState.Active, "qa.unity-playerinput-bridge.prepare.control-active");
        }

        private bool ValidateSuccessfulBridge()
        {
            ClearCurrentBindings("successful-bridge.before-clear");
            PlayerControlBindingResult controlResult = BindControl("successful-bridge.control");
            UnityPlayerInputBridgeResult result = UnityPlayerInputBridgeAdapter.Bridge(
                controlBindingTarget,
                bridgeTarget,
                "qa.unity-playerinput-bridge.success",
                "qa.unity-playerinput-bridge.success.reason");

            bool passed = controlResult.Succeeded
                && result.Succeeded
                && result.BindsControl
                && result.BridgesUnityPlayerInput
                && bridgeTarget.HasUnityPlayerInputBridge
                && bridgeTarget.CurrentUnityPlayerInputBridge.BridgesUnityPlayerInput
                && !result.BindsView
                && !result.ActivatesCamera
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Successful Unity PlayerInput bridge", passed, result);
            return passed;
        }

        private bool ValidateMissingControlBindingTarget()
        {
            UnityPlayerInputBridgeResult result = UnityPlayerInputBridgeAdapter.Bridge(
                null,
                bridgeTarget,
                "qa.unity-playerinput-bridge.missing-control-binding-target",
                "qa.unity-playerinput-bridge.missing-control-binding-target.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputBridgeFailureKind.MissingPlayerControlBindingTarget;
            LogResult("Missing PlayerControl binding target", passed, result);
            return passed;
        }

        private bool ValidateMissingControlBinding()
        {
            PlayerControlBindingTargetBehaviour emptyControlTarget = new GameObject("QA_F52B_EmptyControlBindingTarget").AddComponent<PlayerControlBindingTargetBehaviour>();
            UnityPlayerInputBridgeResult result = UnityPlayerInputBridgeAdapter.Bridge(
                emptyControlTarget,
                bridgeTarget,
                "qa.unity-playerinput-bridge.missing-control-binding",
                "qa.unity-playerinput-bridge.missing-control-binding.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputBridgeFailureKind.MissingPlayerControlBinding;
            LogResult("Missing PlayerControl binding", passed, result);
            Destroy(emptyControlTarget.gameObject);
            return passed;
        }

        private bool ValidateMissingBridgeTarget()
        {
            ClearCurrentBindings("missing-bridge-target.before-clear");
            BindControl("missing-bridge-target.control");
            UnityPlayerInputBridgeResult result = UnityPlayerInputBridgeAdapter.Bridge(
                controlBindingTarget,
                null,
                "qa.unity-playerinput-bridge.missing-bridge-target",
                "qa.unity-playerinput-bridge.missing-bridge-target.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputBridgeFailureKind.MissingUnityPlayerInputBridgeTarget;
            LogResult("Missing Unity PlayerInput bridge target", passed, result);
            return passed;
        }

        private bool ValidateMissingPlayerInput()
        {
            ClearCurrentBindings("missing-player-input.before-clear");
            BindControl("missing-player-input.control");
            UnityPlayerInputBridgeTargetBehaviour missingPlayerInputTarget = new GameObject("QA_F52B_MissingPlayerInputBridgeTarget").AddComponent<UnityPlayerInputBridgeTargetBehaviour>();
            UnityPlayerInputBridgeResult result = UnityPlayerInputBridgeAdapter.Bridge(
                controlBindingTarget,
                missingPlayerInputTarget,
                "qa.unity-playerinput-bridge.missing-player-input",
                "qa.unity-playerinput-bridge.missing-player-input.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputBridgeFailureKind.MissingUnityPlayerInput;
            LogResult("Missing Unity PlayerInput", passed, result);
            Destroy(missingPlayerInputTarget.gameObject);
            return passed;
        }

        private bool ValidateSlotMismatch()
        {
            ClearCurrentBindings("slot-mismatch.before-clear");
            BindControl("slot-mismatch.control");
            UnityPlayerInputBridgeResult result = UnityPlayerInputBridgeAdapter.Bridge(
                controlBindingTarget,
                slotMismatchBridgeTarget,
                "qa.unity-playerinput-bridge.slot-mismatch",
                "qa.unity-playerinput-bridge.slot-mismatch.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputBridgeFailureKind.PlayerSlotMismatch;
            LogResult("PlayerSlot mismatch", passed, result);
            return passed;
        }

        private bool ValidateClearNoOp()
        {
            UnityPlayerInputBridgeTargetBehaviour emptyTarget = new GameObject("QA_F52B_EmptyBridgeTarget").AddComponent<UnityPlayerInputBridgeTargetBehaviour>();
            UnityPlayerInputBridgeResult result = UnityPlayerInputBridgeAdapter.Clear(
                controlBehaviour.PlayerSlotId,
                emptyTarget,
                "qa.unity-playerinput-bridge.clear-noop",
                "qa.unity-playerinput-bridge.clear-noop.reason");

            bool passed = result.NoOp
                && result.FailureKind == UnityPlayerInputBridgeFailureKind.MissingExistingBridge
                && !emptyTarget.HasUnityPlayerInputBridge;

            LogResult("Clear no-op", passed, result);
            Destroy(emptyTarget.gameObject);
            return passed;
        }

        private bool ValidateClearAfterBridge()
        {
            ClearCurrentBindings("clear-after-bridge.before-clear");
            PlayerControlBindingResult controlResult = BindControl("clear-after-bridge.control");
            UnityPlayerInputBridgeResult bridgeResult = UnityPlayerInputBridgeAdapter.Bridge(
                controlBindingTarget,
                bridgeTarget,
                "qa.unity-playerinput-bridge.clear-after-bridge.bridge",
                "qa.unity-playerinput-bridge.clear-after-bridge.bridge.reason");
            UnityPlayerInputBridgeResult clearResult = UnityPlayerInputBridgeAdapter.Clear(
                controlBehaviour.PlayerSlotId,
                bridgeTarget,
                "qa.unity-playerinput-bridge.clear-after-bridge.clear",
                "qa.unity-playerinput-bridge.clear-after-bridge.clear.reason");

            bool passed = controlResult.Succeeded
                && bridgeResult.Succeeded
                && clearResult.Succeeded
                && !clearResult.BridgesUnityPlayerInput
                && !bridgeTarget.HasUnityPlayerInputBridge;

            LogResult("Clear after bridge", passed, clearResult);
            return passed;
        }

        private bool ValidatePassiveBoundary()
        {
            ClearCurrentBindings("passive-boundary.before-clear");
            PlayerControlBindingResult controlResult = BindControl("passive-boundary.control");
            UnityPlayerInputBridgeResult result = UnityPlayerInputBridgeAdapter.Bridge(
                controlBindingTarget,
                bridgeTarget,
                "qa.unity-playerinput-bridge.passive-boundary.bridge",
                "qa.unity-playerinput-bridge.passive-boundary.bridge.reason");

            bool passed = controlResult.Succeeded
                && result.Succeeded
                && result.BindsControl
                && result.BridgesUnityPlayerInput
                && !result.BindsView
                && !result.ActivatesCamera
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Passive boundary", passed, result);
            return passed;
        }

        private PlayerControlBindingResult BindControl(string reasonSuffix)
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot(reasonSuffix);
            return PlayerControlBindingAdapter.BindFirstReadyControl(
                report.ReadinessSummary,
                controlBindingTarget,
                "qa.unity-playerinput-bridge." + reasonSuffix,
                "qa.unity-playerinput-bridge." + reasonSuffix + ".reason");
        }

        private PlayerBindingAuthoringValidationReport ValidateCurrentRoot(string reasonSuffix)
        {
            return PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.unity-playerinput-bridge." + reasonSuffix,
                "qa.unity-playerinput-bridge." + reasonSuffix + ".reason");
        }

        private void ClearCurrentBindings(string reasonSuffix)
        {
            if (controlBindingTarget != null && controlBindingTarget.HasPlayerControlBinding)
            {
                PlayerControlBindingAdapter.Clear(
                    controlBehaviour.PlayerSlotId,
                    controlBindingTarget,
                    "qa.unity-playerinput-bridge." + reasonSuffix + ".clear-control",
                    "qa.unity-playerinput-bridge." + reasonSuffix + ".clear-control.reason");
            }

            if (bridgeTarget != null && bridgeTarget.HasUnityPlayerInputBridge)
            {
                UnityPlayerInputBridgeAdapter.Clear(
                    controlBehaviour.PlayerSlotId,
                    bridgeTarget,
                    "qa.unity-playerinput-bridge." + reasonSuffix + ".clear-bridge",
                    "qa.unity-playerinput-bridge." + reasonSuffix + ".clear-bridge.reason");
            }

            if (slotMismatchBridgeTarget != null && slotMismatchBridgeTarget.HasUnityPlayerInputBridge)
            {
                UnityPlayerInputBridgeAdapter.Clear(
                    controlBehaviour.PlayerSlotId,
                    slotMismatchBridgeTarget,
                    "qa.unity-playerinput-bridge." + reasonSuffix + ".clear-slot-mismatch-bridge",
                    "qa.unity-playerinput-bridge." + reasonSuffix + ".clear-slot-mismatch-bridge.reason");
            }
        }

        private void LogResult(string label, bool passed, UnityPlayerInputBridgeResult result)
        {
            string message = $"{LogPrefix} {label}. passed='{passed}' result=\"{result.ToDiagnosticString()}\".";
            if (passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }
    }
}

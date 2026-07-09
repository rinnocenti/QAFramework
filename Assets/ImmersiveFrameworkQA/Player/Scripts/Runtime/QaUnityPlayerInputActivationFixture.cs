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
    public sealed class QaUnityPlayerInputActivationFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F52C_UNITY_PLAYERINPUT_ACTIVATION_QA]";
        private const string InitialActionMap = "UI";
        private const string GameplayActionMap = "Gameplay";

        [SerializeField] private GameObject validationRoot;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
        [SerializeField] private PlayerControlBehaviour controlBehaviour;
        [SerializeField] private PlayerControlBindingTargetBehaviour controlBindingTarget;
        [SerializeField] private UnityPlayerInputBridgeTargetBehaviour bridgeTarget;
        [SerializeField] private UnityPlayerInputActivationTargetBehaviour activationTarget;
        [SerializeField] private UnityPlayerInputActivationTargetBehaviour missingActionMapNameTarget;
        [SerializeField] private UnityPlayerInputActivationTargetBehaviour invalidActionMapTarget;
        [SerializeField] private UnityPlayerInputActivationTargetBehaviour slotMismatchActivationTarget;
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

        [ContextMenu("Run F52C Unity PlayerInput Activation Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            PrepareReadyAuthoringState();

            bool activation = ValidateSuccessfulActivation();
            bool missingBridgeTarget = ValidateMissingBridgeTarget();
            bool missingBridge = ValidateMissingBridge();
            bool missingActivationTarget = ValidateMissingActivationTarget();
            bool missingPlayerInput = ValidateMissingPlayerInput();
            bool missingActionMapName = ValidateMissingActionMapName();
            bool missingActionMap = ValidateMissingActionMap();
            bool slotMismatch = ValidateSlotMismatch();
            bool clearNoOp = ValidateClearNoOp();
            bool clearAfterActivation = ValidateClearAfterActivation();
            bool actionMapBoundary = ValidateActionMapBoundary();

            bool passed = component
                && activation
                && missingBridgeTarget
                && missingBridge
                && missingActivationTarget
                && missingPlayerInput
                && missingActionMapName
                && missingActionMap
                && slotMismatch
                && clearNoOp
                && clearAfterActivation
                && actionMapBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' component='{component}' activation='{activation}' missingBridgeTarget='{missingBridgeTarget}' missingBridge='{missingBridge}' missingActivationTarget='{missingActivationTarget}' missingPlayerInput='{missingPlayerInput}' missingActionMapName='{missingActionMapName}' missingActionMap='{missingActionMap}' slotMismatch='{slotMismatch}' clearNoOp='{clearNoOp}' clearAfterActivation='{clearAfterActivation}' actionMapBoundary='{actionMapBoundary}'.";

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
                && activationTarget != null
                && missingActionMapNameTarget != null
                && invalidActionMapTarget != null
                && slotMismatchActivationTarget != null
                && playerInput != null;

            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private void PrepareReadyAuthoringState()
        {
            actorReadinessBehaviour.BeginNewCycle("qa.unity-playerinput-activation.prepare.begin-cycle");
            actorReadinessBehaviour.MarkReadyForControl("qa.unity-playerinput-activation.prepare.ready-for-control");

            entryBehaviour.RebuildEntry("qa.unity-playerinput-activation.prepare.entry-rebuild");
            entryBehaviour.SetState(PlayerEntryState.Active, "qa.unity-playerinput-activation.prepare.entry-active");

            viewBehaviour.RebuildView("qa.unity-playerinput-activation.prepare.view-rebuild");
            viewBehaviour.RefreshPlayerEntryEvidence("qa.unity-playerinput-activation.prepare.view-refresh-entry");
            viewBehaviour.SetState(PlayerViewState.Active, "qa.unity-playerinput-activation.prepare.view-active");

            controlBehaviour.RebuildControl("qa.unity-playerinput-activation.prepare.control-rebuild");
            controlBehaviour.RefreshPlayerEntryFromBehaviour("qa.unity-playerinput-activation.prepare.control-refresh-entry");
            controlBehaviour.SetState(PlayerControlState.Active, "qa.unity-playerinput-activation.prepare.control-active");

            ResetPlayerInputToInitialActionMap();
        }

        private bool ValidateSuccessfulActivation()
        {
            ClearCurrentBindings("successful-activation.before-clear");
            ResetPlayerInputToInitialActionMap();
            PlayerControlBindingResult controlResult = BindControl("successful-activation.control");
            UnityPlayerInputBridgeResult bridgeResult = BridgeInput("successful-activation.bridge");
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                bridgeTarget,
                activationTarget,
                "qa.unity-playerinput-activation.success",
                "qa.unity-playerinput-activation.success.reason");

            bool passed = controlResult.Succeeded
                && bridgeResult.Succeeded
                && result.Succeeded
                && result.BindsControl
                && result.BridgesUnityPlayerInput
                && result.ActivatesInput
                && activationTarget.HasUnityPlayerInputActivation
                && string.Equals(result.PreviousActionMapName, InitialActionMap, StringComparison.Ordinal)
                && string.Equals(result.CurrentActionMapName, GameplayActionMap, StringComparison.Ordinal)
                && string.Equals(CurrentPlayerInputActionMapName(), GameplayActionMap, StringComparison.Ordinal)
                && !result.BindsView
                && !result.ActivatesCamera
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Successful Unity PlayerInput activation", passed, result);
            return passed;
        }

        private bool ValidateMissingBridgeTarget()
        {
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                null,
                activationTarget,
                "qa.unity-playerinput-activation.missing-bridge-target",
                "qa.unity-playerinput-activation.missing-bridge-target.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputActivationFailureKind.MissingUnityPlayerInputBridgeTarget;
            LogResult("Missing Unity PlayerInput bridge target", passed, result);
            return passed;
        }

        private bool ValidateMissingBridge()
        {
            UnityPlayerInputBridgeTargetBehaviour emptyBridgeTarget = new GameObject("QA_F52C_EmptyBridgeTarget").AddComponent<UnityPlayerInputBridgeTargetBehaviour>();
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                emptyBridgeTarget,
                activationTarget,
                "qa.unity-playerinput-activation.missing-bridge",
                "qa.unity-playerinput-activation.missing-bridge.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputActivationFailureKind.MissingUnityPlayerInputBridge;
            LogResult("Missing Unity PlayerInput bridge", passed, result);
            Destroy(emptyBridgeTarget.gameObject);
            return passed;
        }

        private bool ValidateMissingActivationTarget()
        {
            ClearCurrentBindings("missing-activation-target.before-clear");
            BindControl("missing-activation-target.control");
            BridgeInput("missing-activation-target.bridge");
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                bridgeTarget,
                null,
                "qa.unity-playerinput-activation.missing-activation-target",
                "qa.unity-playerinput-activation.missing-activation-target.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputActivationFailureKind.MissingUnityPlayerInputActivationTarget;
            LogResult("Missing Unity PlayerInput activation target", passed, result);
            return passed;
        }

        private bool ValidateMissingPlayerInput()
        {
            ClearCurrentBindings("missing-player-input.before-clear");
            BindControl("missing-player-input.control");
            BridgeInput("missing-player-input.bridge");
            UnityPlayerInputActivationTargetBehaviour missingPlayerInputTarget = new GameObject("QA_F52C_MissingPlayerInputActivationTarget").AddComponent<UnityPlayerInputActivationTargetBehaviour>();
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                bridgeTarget,
                missingPlayerInputTarget,
                "qa.unity-playerinput-activation.missing-player-input",
                "qa.unity-playerinput-activation.missing-player-input.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputActivationFailureKind.MissingUnityPlayerInput;
            LogResult("Missing Unity PlayerInput", passed, result);
            Destroy(missingPlayerInputTarget.gameObject);
            return passed;
        }

        private bool ValidateMissingActionMapName()
        {
            ClearCurrentBindings("missing-action-map-name.before-clear");
            BindControl("missing-action-map-name.control");
            BridgeInput("missing-action-map-name.bridge");
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                bridgeTarget,
                missingActionMapNameTarget,
                "qa.unity-playerinput-activation.missing-action-map-name",
                "qa.unity-playerinput-activation.missing-action-map-name.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputActivationFailureKind.MissingActionMapName;
            LogResult("Missing action map name", passed, result);
            return passed;
        }

        private bool ValidateMissingActionMap()
        {
            ClearCurrentBindings("missing-action-map.before-clear");
            BindControl("missing-action-map.control");
            BridgeInput("missing-action-map.bridge");
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                bridgeTarget,
                invalidActionMapTarget,
                "qa.unity-playerinput-activation.missing-action-map",
                "qa.unity-playerinput-activation.missing-action-map.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputActivationFailureKind.MissingActionMap;
            LogResult("Missing configured action map", passed, result);
            return passed;
        }

        private bool ValidateSlotMismatch()
        {
            ClearCurrentBindings("slot-mismatch.before-clear");
            BindControl("slot-mismatch.control");
            BridgeInput("slot-mismatch.bridge");
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                bridgeTarget,
                slotMismatchActivationTarget,
                "qa.unity-playerinput-activation.slot-mismatch",
                "qa.unity-playerinput-activation.slot-mismatch.reason");

            bool passed = result.Failed && result.FailureKind == UnityPlayerInputActivationFailureKind.PlayerSlotMismatch;
            LogResult("PlayerSlot mismatch", passed, result);
            return passed;
        }

        private bool ValidateClearNoOp()
        {
            UnityPlayerInputActivationTargetBehaviour emptyTarget = new GameObject("QA_F52C_EmptyActivationTarget").AddComponent<UnityPlayerInputActivationTargetBehaviour>();
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Clear(
                controlBehaviour.PlayerSlotId,
                emptyTarget,
                "qa.unity-playerinput-activation.clear-noop",
                "qa.unity-playerinput-activation.clear-noop.reason");

            bool passed = result.NoOp
                && result.FailureKind == UnityPlayerInputActivationFailureKind.MissingExistingActivation
                && !emptyTarget.HasUnityPlayerInputActivation;

            LogResult("Clear no-op", passed, result);
            Destroy(emptyTarget.gameObject);
            return passed;
        }

        private bool ValidateClearAfterActivation()
        {
            ClearCurrentBindings("clear-after-activation.before-clear");
            ResetPlayerInputToInitialActionMap();
            PlayerControlBindingResult controlResult = BindControl("clear-after-activation.control");
            UnityPlayerInputBridgeResult bridgeResult = BridgeInput("clear-after-activation.bridge");
            UnityPlayerInputActivationResult activationResult = UnityPlayerInputActivationAdapter.Activate(
                bridgeTarget,
                activationTarget,
                "qa.unity-playerinput-activation.clear-after-activation.activate",
                "qa.unity-playerinput-activation.clear-after-activation.activate.reason");
            UnityPlayerInputActivationResult clearResult = UnityPlayerInputActivationAdapter.Clear(
                controlBehaviour.PlayerSlotId,
                activationTarget,
                "qa.unity-playerinput-activation.clear-after-activation.clear",
                "qa.unity-playerinput-activation.clear-after-activation.clear.reason");

            bool passed = controlResult.Succeeded
                && bridgeResult.Succeeded
                && activationResult.Succeeded
                && clearResult.Succeeded
                && clearResult.BindsControl
                && clearResult.BridgesUnityPlayerInput
                && !clearResult.ActivatesInput
                && !activationTarget.HasUnityPlayerInputActivation
                && bridgeTarget.HasUnityPlayerInputBridge
                && string.Equals(clearResult.CurrentActionMapName, InitialActionMap, StringComparison.Ordinal)
                && string.Equals(CurrentPlayerInputActionMapName(), InitialActionMap, StringComparison.Ordinal);

            LogResult("Clear after activation", passed, clearResult);
            return passed;
        }

        private bool ValidateActionMapBoundary()
        {
            ClearCurrentBindings("action-map-boundary.before-clear");
            ResetPlayerInputToInitialActionMap();
            PlayerControlBindingResult controlResult = BindControl("action-map-boundary.control");
            UnityPlayerInputBridgeResult bridgeResult = BridgeInput("action-map-boundary.bridge");
            UnityPlayerInputActivationResult result = UnityPlayerInputActivationAdapter.Activate(
                bridgeTarget,
                activationTarget,
                "qa.unity-playerinput-activation.action-map-boundary.activate",
                "qa.unity-playerinput-activation.action-map-boundary.activate.reason");

            bool passed = controlResult.Succeeded
                && bridgeResult.Succeeded
                && result.Succeeded
                && result.BindsControl
                && result.BridgesUnityPlayerInput
                && result.ActivatesInput
                && !result.BindsView
                && !result.ActivatesCamera
                && !result.EnablesMovement
                && !result.SpawnsActor
                && string.Equals(CurrentPlayerInputActionMapName(), GameplayActionMap, StringComparison.Ordinal);

            LogResult("Action-map boundary", passed, result);
            return passed;
        }

        private PlayerControlBindingResult BindControl(string reasonSuffix)
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot(reasonSuffix);
            return PlayerControlBindingAdapter.BindFirstReadyControl(
                report.ReadinessSummary,
                controlBindingTarget,
                "qa.unity-playerinput-activation." + reasonSuffix,
                "qa.unity-playerinput-activation." + reasonSuffix + ".reason");
        }

        private UnityPlayerInputBridgeResult BridgeInput(string reasonSuffix)
        {
            return UnityPlayerInputBridgeAdapter.Bridge(
                controlBindingTarget,
                bridgeTarget,
                "qa.unity-playerinput-activation." + reasonSuffix,
                "qa.unity-playerinput-activation." + reasonSuffix + ".reason");
        }

        private PlayerBindingAuthoringValidationReport ValidateCurrentRoot(string reasonSuffix)
        {
            return PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.unity-playerinput-activation." + reasonSuffix,
                "qa.unity-playerinput-activation." + reasonSuffix + ".reason");
        }

        private void ClearCurrentBindings(string reasonSuffix)
        {
            if (activationTarget != null && activationTarget.HasUnityPlayerInputActivation)
            {
                UnityPlayerInputActivationAdapter.Clear(
                    controlBehaviour.PlayerSlotId,
                    activationTarget,
                    "qa.unity-playerinput-activation." + reasonSuffix + ".clear-activation",
                    "qa.unity-playerinput-activation." + reasonSuffix + ".clear-activation.reason");
            }

            if (bridgeTarget != null && bridgeTarget.HasUnityPlayerInputBridge)
            {
                UnityPlayerInputBridgeAdapter.Clear(
                    controlBehaviour.PlayerSlotId,
                    bridgeTarget,
                    "qa.unity-playerinput-activation." + reasonSuffix + ".clear-bridge",
                    "qa.unity-playerinput-activation." + reasonSuffix + ".clear-bridge.reason");
            }

            if (controlBindingTarget != null && controlBindingTarget.HasPlayerControlBinding)
            {
                PlayerControlBindingAdapter.Clear(
                    controlBehaviour.PlayerSlotId,
                    controlBindingTarget,
                    "qa.unity-playerinput-activation." + reasonSuffix + ".clear-control",
                    "qa.unity-playerinput-activation." + reasonSuffix + ".clear-control.reason");
            }
        }

        private void ResetPlayerInputToInitialActionMap()
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return;
            }

            if (!playerInput.enabled)
            {
                playerInput.enabled = true;
            }

            if (playerInput.actions.FindActionMap(InitialActionMap, false) != null)
            {
                playerInput.SwitchCurrentActionMap(InitialActionMap);
            }
        }

        private string CurrentPlayerInputActionMapName()
        {
            return playerInput != null && playerInput.currentActionMap != null ? playerInput.currentActionMap.name : string.Empty;
        }

        private void LogResult(string label, bool passed, UnityPlayerInputActivationResult result)
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

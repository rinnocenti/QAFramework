using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerBinding;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerViews;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    public sealed class QaPlayerViewCameraActivationFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F51C_PLAYER_VIEW_CAMERA_ACTIVATION_QA]";

        [SerializeField] private GameObject validationRoot;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
        [SerializeField] private PlayerControlBehaviour controlBehaviour;
        [SerializeField] private PlayerViewBindingTargetBehaviour viewBindingTarget;
        [SerializeField] private PlayerViewCameraTargetBindingTargetBehaviour cameraTargetBindingTarget;
        [SerializeField] private PlayerViewCameraActivationTargetBehaviour cameraActivationTarget;
        [SerializeField] private UnityEngine.Camera activationCamera;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("F51C/Run PlayerView Camera Activation Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            PrepareReadyAuthoringState();

            bool successfulActivation = ValidateSuccessfulActivation();
            bool missingCameraTargetBinding = ValidateMissingCameraTargetBinding();
            bool missingCameraTargetBindingTarget = ValidateMissingCameraTargetBindingTarget();
            bool missingCameraActivationTarget = ValidateMissingCameraActivationTarget();
            bool missingCamera = ValidateMissingCamera();
            bool clearNoOp = ValidateClearNoOp();
            bool clearAfterActivation = ValidateClearAfterActivation();
            bool activationBoundary = ValidateActivationBoundary();

            bool passed = component
                && successfulActivation
                && missingCameraTargetBinding
                && missingCameraTargetBindingTarget
                && missingCameraActivationTarget
                && missingCamera
                && clearNoOp
                && clearAfterActivation
                && activationBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' activate='{successfulActivation}' missingCameraTargetBinding='{missingCameraTargetBinding}' " +
                $"missingCameraTargetBindingTarget='{missingCameraTargetBindingTarget}' missingCameraActivationTarget='{missingCameraActivationTarget}' " +
                $"missingCamera='{missingCamera}' clearNoOp='{clearNoOp}' clearAfterActivation='{clearAfterActivation}' " +
                $"activationBoundary='{activationBoundary}'.";

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
                && viewBindingTarget != null
                && cameraTargetBindingTarget != null
                && cameraActivationTarget != null
                && activationCamera != null;

            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private void PrepareReadyAuthoringState()
        {
            actorReadinessBehaviour.BeginNewCycle("qa.playerview-camera-activation.prepare.begin-cycle");
            actorReadinessBehaviour.MarkReadyForControl("qa.playerview-camera-activation.prepare.ready-for-control");

            entryBehaviour.RebuildEntry("qa.playerview-camera-activation.prepare.entry-rebuild");
            entryBehaviour.SetState(PlayerEntryState.Active, "qa.playerview-camera-activation.prepare.entry-active");

            viewBehaviour.RebuildView("qa.playerview-camera-activation.prepare.view-rebuild");
            viewBehaviour.RefreshPlayerEntryEvidence("qa.playerview-camera-activation.prepare.view-refresh-entry");
            viewBehaviour.SetState(PlayerViewState.Active, "qa.playerview-camera-activation.prepare.view-active");

            controlBehaviour.RebuildControl("qa.playerview-camera-activation.prepare.control-rebuild");
            controlBehaviour.RefreshPlayerEntryFromBehaviour("qa.playerview-camera-activation.prepare.control-refresh-entry");
            controlBehaviour.SetState(PlayerControlState.Active, "qa.playerview-camera-activation.prepare.control-active");
        }

        private bool ValidateSuccessfulActivation()
        {
            PlayerViewCameraTargetBindingResult targetBinding = BindReadyCameraTarget("success");
            activationCamera.enabled = false;
            PlayerViewCameraActivationResult result = PlayerViewCameraActivationAdapter.Activate(
                cameraTargetBindingTarget,
                cameraActivationTarget,
                "qa.playerview-camera-activation.success",
                "qa.playerview-camera-activation.success.reason");

            bool passed = targetBinding.Succeeded
                && result.Succeeded
                && result.BindsView
                && result.BindsCameraTarget
                && result.ActivatesCamera
                && cameraActivationTarget.HasCameraActivation
                && cameraActivationTarget.CurrentCameraActivation.ActivatesCamera
                && activationCamera.enabled
                && !result.BindsControl
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Successful camera activation", passed, result);
            return passed;
        }

        private bool ValidateMissingCameraTargetBinding()
        {
            PlayerViewCameraTargetBindingTargetBehaviour emptyTarget = new GameObject("QA_F51C_EmptyCameraTargetBindingTarget").AddComponent<PlayerViewCameraTargetBindingTargetBehaviour>();
            PlayerViewCameraActivationResult result = PlayerViewCameraActivationAdapter.Activate(
                emptyTarget,
                cameraActivationTarget,
                "qa.playerview-camera-activation.missing-camera-target-binding",
                "qa.playerview-camera-activation.missing-camera-target-binding.reason");

            bool passed = result.Failed
                && result.FailureKind == PlayerViewCameraActivationFailureKind.MissingCameraTargetBinding;

            LogResult("Missing camera-target binding", passed, result);
            Destroy(emptyTarget.gameObject);
            return passed;
        }

        private bool ValidateMissingCameraTargetBindingTarget()
        {
            PlayerViewCameraActivationResult result = PlayerViewCameraActivationAdapter.Activate(
                null,
                cameraActivationTarget,
                "qa.playerview-camera-activation.missing-camera-target-binding-target",
                "qa.playerview-camera-activation.missing-camera-target-binding-target.reason");

            bool passed = result.Failed
                && result.FailureKind == PlayerViewCameraActivationFailureKind.MissingCameraTargetBindingTarget;

            LogResult("Missing camera-target binding target", passed, result);
            return passed;
        }

        private bool ValidateMissingCameraActivationTarget()
        {
            BindReadyCameraTarget("missing-camera-activation-target");
            PlayerViewCameraActivationResult result = PlayerViewCameraActivationAdapter.Activate(
                cameraTargetBindingTarget,
                null,
                "qa.playerview-camera-activation.missing-camera-activation-target",
                "qa.playerview-camera-activation.missing-camera-activation-target.reason");

            bool passed = result.Failed
                && result.FailureKind == PlayerViewCameraActivationFailureKind.MissingCameraActivationTarget;

            LogResult("Missing camera activation target", passed, result);
            return passed;
        }

        private bool ValidateMissingCamera()
        {
            BindReadyCameraTarget("missing-camera");
            PlayerViewCameraActivationTargetBehaviour emptyActivationTarget = new GameObject("QA_F51C_EmptyCameraActivationTarget").AddComponent<PlayerViewCameraActivationTargetBehaviour>();
            PlayerViewCameraActivationResult result = PlayerViewCameraActivationAdapter.Activate(
                cameraTargetBindingTarget,
                emptyActivationTarget,
                "qa.playerview-camera-activation.missing-camera",
                "qa.playerview-camera-activation.missing-camera.reason");

            bool passed = result.Failed
                && result.FailureKind == PlayerViewCameraActivationFailureKind.MissingCamera;

            LogResult("Missing camera", passed, result);
            Destroy(emptyActivationTarget.gameObject);
            return passed;
        }

        private bool ValidateClearNoOp()
        {
            PlayerViewCameraActivationTargetBehaviour emptyTarget = new GameObject("QA_F51C_EmptyActivationTarget").AddComponent<PlayerViewCameraActivationTargetBehaviour>();
            PlayerViewCameraActivationResult result = PlayerViewCameraActivationAdapter.Clear(
                viewBehaviour.PlayerSlotId,
                emptyTarget,
                "qa.playerview-camera-activation.clear-noop",
                "qa.playerview-camera-activation.clear-noop.reason");

            bool passed = result.NoOp
                && result.FailureKind == PlayerViewCameraActivationFailureKind.MissingExistingActivation
                && !emptyTarget.HasCameraActivation;

            LogResult("Clear no-op", passed, result);
            Destroy(emptyTarget.gameObject);
            return passed;
        }

        private bool ValidateClearAfterActivation()
        {
            PlayerViewCameraTargetBindingResult targetBinding = BindReadyCameraTarget("clear-after-activation");
            activationCamera.enabled = false;
            PlayerViewCameraActivationResult activateResult = PlayerViewCameraActivationAdapter.Activate(
                cameraTargetBindingTarget,
                cameraActivationTarget,
                "qa.playerview-camera-activation.clear-after-activation.activate",
                "qa.playerview-camera-activation.clear-after-activation.activate.reason");
            PlayerViewCameraActivationResult clearResult = PlayerViewCameraActivationAdapter.Clear(
                viewBehaviour.PlayerSlotId,
                cameraActivationTarget,
                "qa.playerview-camera-activation.clear-after-activation.clear",
                "qa.playerview-camera-activation.clear-after-activation.clear.reason");

            bool passed = targetBinding.Succeeded
                && activateResult.Succeeded
                && clearResult.Succeeded
                && !cameraActivationTarget.HasCameraActivation
                && !clearResult.ActivatesCamera
                && !activationCamera.enabled;

            LogResult("Clear after activation", passed, clearResult);
            return passed;
        }

        private bool ValidateActivationBoundary()
        {
            PlayerViewCameraTargetBindingResult targetBinding = BindReadyCameraTarget("activation-boundary");
            activationCamera.enabled = false;
            PlayerViewCameraActivationResult result = PlayerViewCameraActivationAdapter.Activate(
                cameraTargetBindingTarget,
                cameraActivationTarget,
                "qa.playerview-camera-activation.activation-boundary",
                "qa.playerview-camera-activation.activation-boundary.reason");

            bool passed = targetBinding.Succeeded
                && result.Succeeded
                && result.BindsView
                && result.BindsCameraTarget
                && result.ActivatesCamera
                && !result.BindsControl
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Activation boundary", passed, result);
            return passed;
        }

        private PlayerViewCameraTargetBindingResult BindReadyCameraTarget(string suffix)
        {
            if (cameraActivationTarget.HasCameraActivation)
            {
                PlayerViewCameraActivationAdapter.Clear(
                    viewBehaviour.PlayerSlotId,
                    cameraActivationTarget,
                    "qa.playerview-camera-activation." + suffix + ".clear-activation-before-bind",
                    "qa.playerview-camera-activation." + suffix + ".clear-activation-before-bind.reason");
            }

            if (cameraTargetBindingTarget.HasCameraTargetBinding)
            {
                PlayerViewCameraTargetBindingAdapter.Clear(
                    viewBehaviour.PlayerSlotId,
                    cameraTargetBindingTarget,
                    "qa.playerview-camera-activation." + suffix + ".clear-camera-target-before-bind",
                    "qa.playerview-camera-activation." + suffix + ".clear-camera-target-before-bind.reason");
            }

            if (viewBindingTarget.HasPlayerViewBinding)
            {
                PlayerViewBindingAdapter.Clear(
                    viewBehaviour.PlayerSlotId,
                    viewBindingTarget,
                    "qa.playerview-camera-activation." + suffix + ".clear-view-binding-before-bind",
                    "qa.playerview-camera-activation." + suffix + ".clear-view-binding-before-bind.reason");
            }

            activationCamera.enabled = false;
            PrepareReadyAuthoringState();
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.playerview-camera-activation." + suffix,
                "qa.playerview-camera-activation." + suffix + ".reason");

            PlayerViewBindingResult viewBinding = PlayerViewBindingAdapter.BindFirstReadyView(
                report.ReadinessSummary,
                viewBindingTarget,
                "qa.playerview-camera-activation." + suffix + ".view-bind",
                "qa.playerview-camera-activation." + suffix + ".view-bind.reason");

            if (!viewBinding.Succeeded)
            {
                return PlayerViewCameraTargetBindingResult.Failure(
                    PlayerViewCameraTargetBindingFailureKind.MissingPlayerViewBinding,
                    string.Empty,
                    viewBindingTarget.BindingTargetName,
                    cameraTargetBindingTarget.CameraTargetBindingTargetName,
                    string.Empty,
                    "qa.playerview-camera-activation." + suffix,
                    "qa.playerview-camera-activation." + suffix + ".reason",
                    viewBinding.Message);
            }

            return PlayerViewCameraTargetBindingAdapter.Bind(
                viewBindingTarget,
                viewBehaviour,
                cameraTargetBindingTarget,
                "qa.playerview-camera-activation." + suffix + ".camera-target-bind",
                "qa.playerview-camera-activation." + suffix + ".camera-target-bind.reason");
        }

        private void LogResult(string label, bool passed, PlayerViewCameraActivationResult result)
        {
            string message = $"{LogPrefix} {label}. passed='{passed}' result='{result.ToDiagnosticString()}'.";
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

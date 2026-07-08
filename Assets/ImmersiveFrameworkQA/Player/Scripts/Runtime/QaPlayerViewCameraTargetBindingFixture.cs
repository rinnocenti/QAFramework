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
    public sealed class QaPlayerViewCameraTargetBindingFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F51B_PLAYER_VIEW_CAMERA_TARGET_BINDING_QA]";

        [SerializeField] private GameObject validationRoot;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
        [SerializeField] private PlayerViewBehaviour viewWithoutTargetBehaviour;
        [SerializeField] private PlayerControlBehaviour controlBehaviour;
        [SerializeField] private PlayerViewBindingTargetBehaviour viewBindingTarget;
        [SerializeField] private PlayerViewCameraTargetBindingTargetBehaviour cameraTargetBindingTarget;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("F51B/Run PlayerView Camera Target Binding Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            PrepareReadyAuthoringState();

            bool successfulBinding = ValidateSuccessfulBinding();
            bool missingViewBinding = ValidateMissingViewBinding();
            bool missingPlayerViewBehaviour = ValidateMissingPlayerViewBehaviour();
            bool missingViewTarget = ValidateMissingViewTarget();
            bool missingCameraTarget = ValidateMissingCameraTargetBindingTarget();
            bool clearNoOp = ValidateClearNoOp();
            bool clearAfterBind = ValidateClearAfterBind();
            bool passiveBoundary = ValidatePassiveBoundary();

            bool passed = component
                && successfulBinding
                && missingViewBinding
                && missingPlayerViewBehaviour
                && missingViewTarget
                && missingCameraTarget
                && clearNoOp
                && clearAfterBind
                && passiveBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' bind='{successfulBinding}' missingViewBinding='{missingViewBinding}' " +
                $"missingPlayerViewBehaviour='{missingPlayerViewBehaviour}' missingViewTarget='{missingViewTarget}' " +
                $"missingCameraTarget='{missingCameraTarget}' clearNoOp='{clearNoOp}' clearAfterBind='{clearAfterBind}' " +
                $"passiveBoundary='{passiveBoundary}'.";

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
                && viewWithoutTargetBehaviour != null
                && controlBehaviour != null
                && viewBindingTarget != null
                && cameraTargetBindingTarget != null;

            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private void PrepareReadyAuthoringState()
        {
            actorReadinessBehaviour.BeginNewCycle("qa.playerview-camera-target-binding.prepare.begin-cycle");
            actorReadinessBehaviour.MarkReadyForControl("qa.playerview-camera-target-binding.prepare.ready-for-control");

            entryBehaviour.RebuildEntry("qa.playerview-camera-target-binding.prepare.entry-rebuild");
            entryBehaviour.SetState(PlayerEntryState.Active, "qa.playerview-camera-target-binding.prepare.entry-active");

            viewBehaviour.RebuildView("qa.playerview-camera-target-binding.prepare.view-rebuild");
            viewBehaviour.RefreshPlayerEntryEvidence("qa.playerview-camera-target-binding.prepare.view-refresh-entry");
            viewBehaviour.SetState(PlayerViewState.Active, "qa.playerview-camera-target-binding.prepare.view-active");

            viewWithoutTargetBehaviour.RebuildView("qa.playerview-camera-target-binding.prepare.no-target-view-rebuild");
            viewWithoutTargetBehaviour.RefreshPlayerEntryEvidence("qa.playerview-camera-target-binding.prepare.no-target-view-refresh-entry");
            viewWithoutTargetBehaviour.SetState(PlayerViewState.Active, "qa.playerview-camera-target-binding.prepare.no-target-view-active");

            controlBehaviour.RebuildControl("qa.playerview-camera-target-binding.prepare.control-rebuild");
            controlBehaviour.RefreshPlayerEntryFromBehaviour("qa.playerview-camera-target-binding.prepare.control-refresh-entry");
            controlBehaviour.SetState(PlayerControlState.Active, "qa.playerview-camera-target-binding.prepare.control-active");
        }

        private bool ValidateSuccessfulBinding()
        {
            PlayerViewBindingResult viewBinding = BindReadyPlayerView("success");
            PlayerViewCameraTargetBindingResult result = PlayerViewCameraTargetBindingAdapter.Bind(
                viewBindingTarget,
                viewBehaviour,
                cameraTargetBindingTarget,
                "qa.playerview-camera-target-binding.success",
                "qa.playerview-camera-target-binding.success.reason");

            bool passed = viewBinding.Succeeded
                && result.Succeeded
                && result.BindsView
                && result.BindsCameraTarget
                && cameraTargetBindingTarget.HasCameraTargetBinding
                && cameraTargetBindingTarget.CurrentCameraTarget == viewBehaviour.ViewTarget
                && cameraTargetBindingTarget.CurrentCameraTargetBinding.BindsCameraTarget
                && !result.BindsControl
                && !result.ActivatesCamera
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Successful camera target binding", passed, result);
            return passed;
        }

        private bool ValidateMissingViewBinding()
        {
            PlayerViewBindingTargetBehaviour emptyViewBindingTarget = new GameObject("QA_F51B_EmptyViewBindingTarget").AddComponent<PlayerViewBindingTargetBehaviour>();
            PlayerViewCameraTargetBindingResult result = PlayerViewCameraTargetBindingAdapter.Bind(
                emptyViewBindingTarget,
                viewBehaviour,
                cameraTargetBindingTarget,
                "qa.playerview-camera-target-binding.missing-view-binding",
                "qa.playerview-camera-target-binding.missing-view-binding.reason");

            bool passed = result.Failed
                && result.FailureKind == PlayerViewCameraTargetBindingFailureKind.MissingPlayerViewBinding;

            LogResult("Missing PlayerView binding", passed, result);
            Destroy(emptyViewBindingTarget.gameObject);
            return passed;
        }

        private bool ValidateMissingPlayerViewBehaviour()
        {
            BindReadyPlayerView("missing-player-view-behaviour");
            PlayerViewCameraTargetBindingResult result = PlayerViewCameraTargetBindingAdapter.Bind(
                viewBindingTarget,
                null,
                cameraTargetBindingTarget,
                "qa.playerview-camera-target-binding.missing-player-view-behaviour",
                "qa.playerview-camera-target-binding.missing-player-view-behaviour.reason");

            bool passed = result.Failed
                && result.FailureKind == PlayerViewCameraTargetBindingFailureKind.MissingPlayerViewBehaviour;

            LogResult("Missing PlayerViewBehaviour", passed, result);
            return passed;
        }

        private bool ValidateMissingViewTarget()
        {
            PlayerViewBindingResult viewBinding = BindReadyPlayerView("missing-view-target");
            PlayerViewCameraTargetBindingResult result = PlayerViewCameraTargetBindingAdapter.Bind(
                viewBindingTarget.CurrentPlayerViewBinding,
                viewWithoutTargetBehaviour,
                cameraTargetBindingTarget,
                "qa.playerview-camera-target-binding.missing-view-target",
                "qa.playerview-camera-target-binding.missing-view-target.reason");

            bool passed = viewBinding.Succeeded
                && result.Failed
                && result.FailureKind == PlayerViewCameraTargetBindingFailureKind.MissingViewTarget;

            LogResult("Missing Unity view target", passed, result);
            return passed;
        }

        private bool ValidateMissingCameraTargetBindingTarget()
        {
            BindReadyPlayerView("missing-camera-target-binding-target");
            PlayerViewCameraTargetBindingResult result = PlayerViewCameraTargetBindingAdapter.Bind(
                viewBindingTarget,
                viewBehaviour,
                null,
                "qa.playerview-camera-target-binding.missing-camera-target-binding-target",
                "qa.playerview-camera-target-binding.missing-camera-target-binding-target.reason");

            bool passed = result.Failed
                && result.FailureKind == PlayerViewCameraTargetBindingFailureKind.MissingCameraTargetBindingTarget;

            LogResult("Missing camera target binding target", passed, result);
            return passed;
        }

        private bool ValidateClearNoOp()
        {
            PlayerViewCameraTargetBindingTargetBehaviour emptyTarget = new GameObject("QA_F51B_EmptyCameraTargetBindingTarget").AddComponent<PlayerViewCameraTargetBindingTargetBehaviour>();
            PlayerViewCameraTargetBindingResult result = PlayerViewCameraTargetBindingAdapter.Clear(
                viewBehaviour.PlayerSlotId,
                emptyTarget,
                "qa.playerview-camera-target-binding.clear-noop",
                "qa.playerview-camera-target-binding.clear-noop.reason");

            bool passed = result.NoOp
                && result.FailureKind == PlayerViewCameraTargetBindingFailureKind.MissingExistingBinding
                && !emptyTarget.HasCameraTargetBinding;

            LogResult("Clear no-op", passed, result);
            Destroy(emptyTarget.gameObject);
            return passed;
        }

        private bool ValidateClearAfterBind()
        {
            PlayerViewBindingResult viewBinding = BindReadyPlayerView("clear-after-bind");
            PlayerViewCameraTargetBindingResult bindResult = PlayerViewCameraTargetBindingAdapter.Bind(
                viewBindingTarget,
                viewBehaviour,
                cameraTargetBindingTarget,
                "qa.playerview-camera-target-binding.clear-after-bind.bind",
                "qa.playerview-camera-target-binding.clear-after-bind.bind.reason");
            PlayerViewCameraTargetBindingResult clearResult = PlayerViewCameraTargetBindingAdapter.Clear(
                viewBehaviour.PlayerSlotId,
                cameraTargetBindingTarget,
                "qa.playerview-camera-target-binding.clear-after-bind.clear",
                "qa.playerview-camera-target-binding.clear-after-bind.clear.reason");

            bool passed = viewBinding.Succeeded
                && bindResult.Succeeded
                && clearResult.Succeeded
                && !cameraTargetBindingTarget.HasCameraTargetBinding
                && !clearResult.BindsCameraTarget;

            LogResult("Clear after bind", passed, clearResult);
            return passed;
        }

        private bool ValidatePassiveBoundary()
        {
            PlayerViewBindingResult viewBinding = BindReadyPlayerView("passive-boundary");
            PlayerViewCameraTargetBindingResult result = PlayerViewCameraTargetBindingAdapter.Bind(
                viewBindingTarget,
                viewBehaviour,
                cameraTargetBindingTarget,
                "qa.playerview-camera-target-binding.passive-boundary",
                "qa.playerview-camera-target-binding.passive-boundary.reason");

            bool passed = viewBinding.Succeeded
                && result.Succeeded
                && result.BindsView
                && result.BindsCameraTarget
                && !result.BindsControl
                && !result.ActivatesCamera
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Passive boundary", passed, result);
            return passed;
        }

        private PlayerViewBindingResult BindReadyPlayerView(string suffix)
        {
            if (viewBindingTarget.HasPlayerViewBinding)
            {
                PlayerViewBindingAdapter.Clear(
                    viewBehaviour.PlayerSlotId,
                    viewBindingTarget,
                    "qa.playerview-camera-target-binding." + suffix + ".clear-view-binding-before-bind",
                    "qa.playerview-camera-target-binding." + suffix + ".clear-view-binding-before-bind.reason");
            }

            if (cameraTargetBindingTarget.HasCameraTargetBinding)
            {
                PlayerViewCameraTargetBindingAdapter.Clear(
                    viewBehaviour.PlayerSlotId,
                    cameraTargetBindingTarget,
                    "qa.playerview-camera-target-binding." + suffix + ".clear-camera-target-before-bind",
                    "qa.playerview-camera-target-binding." + suffix + ".clear-camera-target-before-bind.reason");
            }

            PrepareReadyAuthoringState();
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.playerview-camera-target-binding." + suffix,
                "qa.playerview-camera-target-binding." + suffix + ".reason");

            return PlayerViewBindingAdapter.BindFirstReadyView(
                report.ReadinessSummary,
                viewBindingTarget,
                "qa.playerview-camera-target-binding." + suffix + ".view-bind",
                "qa.playerview-camera-target-binding." + suffix + ".view-bind.reason");
        }

        private void LogResult(string label, bool passed, PlayerViewCameraTargetBindingResult result)
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

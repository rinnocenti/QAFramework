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
    public sealed class QaPlayerViewBindingAdapterFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F51A_PLAYERVIEW_BINDING_ADAPTER_QA]";

        [SerializeField] private GameObject validationRoot;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
        [SerializeField] private PlayerControlBehaviour controlBehaviour;
        [SerializeField] private PlayerViewBindingTargetBehaviour bindingTarget;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("F51A/Run PlayerView Binding Adapter Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            PrepareReadyAuthoringState();

            bool successfulBinding = ValidateSuccessfulBinding();
            bool missingReadiness = ValidateMissingReadiness();
            bool notReady = ValidateNotReady();
            bool inactiveViewRejected = ValidateInactiveViewRejected();
            bool missingTarget = ValidateMissingTarget();
            bool clearNoOp = ValidateClearNoOp();
            bool clearAfterBind = ValidateClearAfterBind();
            bool passiveBoundary = ValidatePassiveBoundary();

            bool passed = component
                && successfulBinding
                && missingReadiness
                && notReady
                && inactiveViewRejected
                && missingTarget
                && clearNoOp
                && clearAfterBind
                && passiveBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' bind='{successfulBinding}' missingReadiness='{missingReadiness}' " +
                $"notReady='{notReady}' inactiveView='{inactiveViewRejected}' missingTarget='{missingTarget}' " +
                $"clearNoOp='{clearNoOp}' clearAfterBind='{clearAfterBind}' passiveBoundary='{passiveBoundary}'.";

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
                && bindingTarget != null;

            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private void PrepareReadyAuthoringState()
        {
            actorReadinessBehaviour.BeginNewCycle("qa.playerview-binding-adapter.prepare.begin-cycle");
            actorReadinessBehaviour.MarkReadyForControl("qa.playerview-binding-adapter.prepare.ready-for-control");

            entryBehaviour.RebuildEntry("qa.playerview-binding-adapter.prepare.entry-rebuild");
            entryBehaviour.SetState(PlayerEntryState.Active, "qa.playerview-binding-adapter.prepare.entry-active");

            viewBehaviour.RebuildView("qa.playerview-binding-adapter.prepare.view-rebuild");
            viewBehaviour.RefreshPlayerEntryEvidence("qa.playerview-binding-adapter.prepare.view-refresh-entry");
            viewBehaviour.SetState(PlayerViewState.Active, "qa.playerview-binding-adapter.prepare.view-active");

            controlBehaviour.RebuildControl("qa.playerview-binding-adapter.prepare.control-rebuild");
            controlBehaviour.RefreshPlayerEntryFromBehaviour("qa.playerview-binding-adapter.prepare.control-refresh-entry");
            controlBehaviour.SetState(PlayerControlState.Active, "qa.playerview-binding-adapter.prepare.control-active");
        }

        private bool ValidateSuccessfulBinding()
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("successful-binding");
            PlayerViewBindingResult result = PlayerViewBindingAdapter.BindFirstReadyView(
                report.ReadinessSummary,
                bindingTarget,
                "qa.playerview-binding-adapter.success",
                "qa.playerview-binding-adapter.success.reason");

            bool passed = report.Succeeded
                && result.Succeeded
                && result.BindsView
                && bindingTarget.HasPlayerViewBinding
                && bindingTarget.CurrentPlayerViewBinding.BindsView
                && !result.BindsControl
                && !result.ActivatesCamera
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Successful binding", passed, result);
            return passed;
        }

        private bool ValidateMissingReadiness()
        {
            PlayerViewBindingResult result = PlayerViewBindingAdapter.BindFirstReadyView(
                null,
                bindingTarget,
                "qa.playerview-binding-adapter.missing-readiness",
                "qa.playerview-binding-adapter.missing-readiness.reason");

            bool passed = result.Failed && result.FailureKind == PlayerViewBindingFailureKind.MissingReadinessSummary;
            LogResult("Missing readiness", passed, result);
            return passed;
        }

        private bool ValidateNotReady()
        {
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateComponents(
                validationRoot.GetComponentsInChildren<PlayerSlotDeclaration>(true),
                validationRoot.GetComponentsInChildren<PlayerSlotOccupancy>(true),
                validationRoot.GetComponentsInChildren<ActorReadinessBehaviour>(true),
                validationRoot.GetComponentsInChildren<PlayerEntryBehaviour>(true),
                new PlayerViewBehaviour[0],
                validationRoot.GetComponentsInChildren<PlayerControlBehaviour>(true),
                "qa.playerview-binding-adapter.not-ready",
                "qa.playerview-binding-adapter.not-ready.reason");

            PlayerViewBindingResult result = PlayerViewBindingAdapter.BindFirstReadyView(
                report.ReadinessSummary,
                bindingTarget,
                "qa.playerview-binding-adapter.not-ready.bind",
                "qa.playerview-binding-adapter.not-ready.bind.reason");

            bool passed = report.Failed
                && !report.IsReadyForViewBinding
                && result.Failed
                && result.FailureKind == PlayerViewBindingFailureKind.ViewBindingNotReady;

            LogResult("Not ready", passed, result);
            return passed;
        }

        private bool ValidateInactiveViewRejected()
        {
            viewBehaviour.SetState(PlayerViewState.Declared, "qa.playerview-binding-adapter.inactive-view.declared");
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("inactive-view");
            PlayerViewBindingResult result = PlayerViewBindingAdapter.Bind(
                report.ReadinessSummary,
                viewBehaviour.CreateSnapshot(),
                bindingTarget,
                "qa.playerview-binding-adapter.inactive-view.bind",
                "qa.playerview-binding-adapter.inactive-view.bind.reason");

            PrepareReadyAuthoringState();

            bool passed = report.IsReadyForViewBinding
                && result.Failed
                && result.FailureKind == PlayerViewBindingFailureKind.PlayerViewNotActive;

            LogResult("Inactive view rejected", passed, result);
            return passed;
        }

        private bool ValidateMissingTarget()
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("missing-target");
            PlayerViewBindingResult result = PlayerViewBindingAdapter.BindFirstReadyView(
                report.ReadinessSummary,
                null,
                "qa.playerview-binding-adapter.missing-target",
                "qa.playerview-binding-adapter.missing-target.reason");

            bool passed = result.Failed && result.FailureKind == PlayerViewBindingFailureKind.MissingBindingTarget;
            LogResult("Missing target", passed, result);
            return passed;
        }

        private bool ValidateClearNoOp()
        {
            PlayerViewBindingTargetBehaviour emptyTarget = new GameObject("QA_F51A_EmptyBindingTarget").AddComponent<PlayerViewBindingTargetBehaviour>();
            PlayerViewBindingResult result = PlayerViewBindingAdapter.Clear(
                viewBehaviour.PlayerSlotId,
                emptyTarget,
                "qa.playerview-binding-adapter.clear-noop",
                "qa.playerview-binding-adapter.clear-noop.reason");

            bool passed = result.NoOp
                && result.FailureKind == PlayerViewBindingFailureKind.MissingExistingBinding
                && !emptyTarget.HasPlayerViewBinding;

            LogResult("Clear no-op", passed, result);
            Destroy(emptyTarget.gameObject);
            return passed;
        }

        private bool ValidateClearAfterBind()
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("clear-after-bind");
            PlayerViewBindingResult bindResult = PlayerViewBindingAdapter.BindFirstReadyView(
                report.ReadinessSummary,
                bindingTarget,
                "qa.playerview-binding-adapter.clear-after-bind.bind",
                "qa.playerview-binding-adapter.clear-after-bind.bind.reason");
            PlayerViewBindingResult clearResult = PlayerViewBindingAdapter.Clear(
                viewBehaviour.PlayerSlotId,
                bindingTarget,
                "qa.playerview-binding-adapter.clear-after-bind.clear",
                "qa.playerview-binding-adapter.clear-after-bind.clear.reason");

            bool passed = bindResult.Succeeded
                && clearResult.Succeeded
                && !bindingTarget.HasPlayerViewBinding
                && !clearResult.BindsView;

            LogResult("Clear after bind", passed, clearResult);
            return passed;
        }

        private bool ValidatePassiveBoundary()
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("passive-boundary");
            PlayerViewBindingResult result = PlayerViewBindingAdapter.BindFirstReadyView(
                report.ReadinessSummary,
                bindingTarget,
                "qa.playerview-binding-adapter.passive-boundary.bind",
                "qa.playerview-binding-adapter.passive-boundary.bind.reason");

            bool passed = result.Succeeded
                && result.BindsView
                && !result.BindsControl
                && !result.ActivatesCamera
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Passive boundary", passed, result);
            return passed;
        }

        private PlayerBindingAuthoringValidationReport ValidateCurrentRoot(string suffix)
        {
            return PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.playerview-binding-adapter." + suffix,
                "qa.playerview-binding-adapter." + suffix + ".reason");
        }

        private void LogResult(string label, bool passed, PlayerViewBindingResult result)
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

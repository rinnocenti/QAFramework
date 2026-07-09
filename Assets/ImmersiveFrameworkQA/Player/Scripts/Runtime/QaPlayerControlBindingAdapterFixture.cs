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
    public sealed class QaPlayerControlBindingAdapterFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F52A_PLAYERCONTROL_BINDING_ADAPTER_QA]";

        [SerializeField] private GameObject validationRoot;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
        [SerializeField] private PlayerControlBehaviour controlBehaviour;
        [SerializeField] private PlayerControlBindingTargetBehaviour bindingTarget;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("Run F52A PlayerControl Binding Adapter Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            PrepareReadyAuthoringState();

            bool bind = ValidateSuccessfulBinding();
            bool missingReadiness = ValidateMissingReadiness();
            bool notReady = ValidateNotReady();
            bool inactiveControl = ValidateInactiveControlRejected();
            bool missingTarget = ValidateMissingTarget();
            bool clearNoOp = ValidateClearNoOp();
            bool clearAfterBind = ValidateClearAfterBind();
            bool passiveBoundary = ValidatePassiveBoundary();

            bool passed = component
                && bind
                && missingReadiness
                && notReady
                && inactiveControl
                && missingTarget
                && clearNoOp
                && clearAfterBind
                && passiveBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' component='{component}' bind='{bind}' missingReadiness='{missingReadiness}' notReady='{notReady}' inactiveControl='{inactiveControl}' missingTarget='{missingTarget}' clearNoOp='{clearNoOp}' clearAfterBind='{clearAfterBind}' passiveBoundary='{passiveBoundary}'.";

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
            actorReadinessBehaviour.BeginNewCycle("qa.playercontrol-binding-adapter.prepare.begin-cycle");
            actorReadinessBehaviour.MarkReadyForControl("qa.playercontrol-binding-adapter.prepare.ready-for-control");

            entryBehaviour.RebuildEntry("qa.playercontrol-binding-adapter.prepare.entry-rebuild");
            entryBehaviour.SetState(PlayerEntryState.Active, "qa.playercontrol-binding-adapter.prepare.entry-active");

            viewBehaviour.RebuildView("qa.playercontrol-binding-adapter.prepare.view-rebuild");
            viewBehaviour.RefreshPlayerEntryEvidence("qa.playercontrol-binding-adapter.prepare.view-refresh-entry");
            viewBehaviour.SetState(PlayerViewState.Active, "qa.playercontrol-binding-adapter.prepare.view-active");

            controlBehaviour.RebuildControl("qa.playercontrol-binding-adapter.prepare.control-rebuild");
            controlBehaviour.RefreshPlayerEntryFromBehaviour("qa.playercontrol-binding-adapter.prepare.control-refresh-entry");
            controlBehaviour.SetState(PlayerControlState.Active, "qa.playercontrol-binding-adapter.prepare.control-active");
        }

        private bool ValidateSuccessfulBinding()
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("successful-binding");
            PlayerControlBindingResult result = PlayerControlBindingAdapter.BindFirstReadyControl(
                report.ReadinessSummary,
                bindingTarget,
                "qa.playercontrol-binding-adapter.success",
                "qa.playercontrol-binding-adapter.success.reason");

            bool passed = report.Succeeded
                && report.IsReadyForControlBinding
                && result.Succeeded
                && result.BindsControl
                && bindingTarget.HasPlayerControlBinding
                && bindingTarget.CurrentPlayerControlBinding.BindsControl
                && !result.BindsView
                && !result.ActivatesCamera
                && !result.ActivatesInput
                && !result.EnablesMovement
                && !result.SpawnsActor;

            LogResult("Successful binding", passed, result);
            return passed;
        }

        private bool ValidateMissingReadiness()
        {
            PlayerControlBindingResult result = PlayerControlBindingAdapter.BindFirstReadyControl(
                null,
                bindingTarget,
                "qa.playercontrol-binding-adapter.missing-readiness",
                "qa.playercontrol-binding-adapter.missing-readiness.reason");

            bool passed = result.Failed && result.FailureKind == PlayerControlBindingFailureKind.MissingReadinessSummary;
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
                validationRoot.GetComponentsInChildren<PlayerViewBehaviour>(true),
                new PlayerControlBehaviour[0],
                "qa.playercontrol-binding-adapter.not-ready",
                "qa.playercontrol-binding-adapter.not-ready.reason");

            PlayerControlBindingResult result = PlayerControlBindingAdapter.BindFirstReadyControl(
                report.ReadinessSummary,
                bindingTarget,
                "qa.playercontrol-binding-adapter.not-ready.bind",
                "qa.playercontrol-binding-adapter.not-ready.bind.reason");

            bool passed = report.Failed
                && !report.IsReadyForControlBinding
                && result.Failed
                && result.FailureKind == PlayerControlBindingFailureKind.ControlBindingNotReady;

            LogResult("Not ready", passed, result);
            return passed;
        }

        private bool ValidateInactiveControlRejected()
        {
            controlBehaviour.SetState(PlayerControlState.Bound, "qa.playercontrol-binding-adapter.inactive-control.bound");
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("inactive-control");
            PlayerControlBindingResult result = PlayerControlBindingAdapter.Bind(
                report.ReadinessSummary,
                controlBehaviour.CreateSnapshot(),
                bindingTarget,
                "qa.playercontrol-binding-adapter.inactive-control.bind",
                "qa.playercontrol-binding-adapter.inactive-control.bind.reason");

            PrepareReadyAuthoringState();

            bool passed = report.IsReadyForControlBinding
                && result.Failed
                && result.FailureKind == PlayerControlBindingFailureKind.PlayerControlNotActive;

            LogResult("Inactive control rejected", passed, result);
            return passed;
        }

        private bool ValidateMissingTarget()
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("missing-target");
            PlayerControlBindingResult result = PlayerControlBindingAdapter.BindFirstReadyControl(
                report.ReadinessSummary,
                null,
                "qa.playercontrol-binding-adapter.missing-target",
                "qa.playercontrol-binding-adapter.missing-target.reason");

            bool passed = result.Failed && result.FailureKind == PlayerControlBindingFailureKind.MissingBindingTarget;
            LogResult("Missing target", passed, result);
            return passed;
        }

        private bool ValidateClearNoOp()
        {
            PlayerControlBindingTargetBehaviour emptyTarget = new GameObject("QA_F52A_EmptyControlBindingTarget").AddComponent<PlayerControlBindingTargetBehaviour>();
            PlayerControlBindingResult result = PlayerControlBindingAdapter.Clear(
                controlBehaviour.PlayerSlotId,
                emptyTarget,
                "qa.playercontrol-binding-adapter.clear-noop",
                "qa.playercontrol-binding-adapter.clear-noop.reason");

            bool passed = result.NoOp
                && result.FailureKind == PlayerControlBindingFailureKind.MissingExistingBinding
                && !emptyTarget.HasPlayerControlBinding;

            LogResult("Clear no-op", passed, result);
            Destroy(emptyTarget.gameObject);
            return passed;
        }

        private bool ValidateClearAfterBind()
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("clear-after-bind");
            PlayerControlBindingResult bindResult = PlayerControlBindingAdapter.BindFirstReadyControl(
                report.ReadinessSummary,
                bindingTarget,
                "qa.playercontrol-binding-adapter.clear-after-bind.bind",
                "qa.playercontrol-binding-adapter.clear-after-bind.bind.reason");
            PlayerControlBindingResult clearResult = PlayerControlBindingAdapter.Clear(
                controlBehaviour.PlayerSlotId,
                bindingTarget,
                "qa.playercontrol-binding-adapter.clear-after-bind.clear",
                "qa.playercontrol-binding-adapter.clear-after-bind.clear.reason");

            bool passed = bindResult.Succeeded
                && clearResult.Succeeded
                && !bindingTarget.HasPlayerControlBinding
                && !clearResult.BindsControl;

            LogResult("Clear after bind", passed, clearResult);
            return passed;
        }

        private bool ValidatePassiveBoundary()
        {
            PlayerBindingAuthoringValidationReport report = ValidateCurrentRoot("passive-boundary");
            PlayerControlBindingResult result = PlayerControlBindingAdapter.BindFirstReadyControl(
                report.ReadinessSummary,
                bindingTarget,
                "qa.playercontrol-binding-adapter.passive-boundary.bind",
                "qa.playercontrol-binding-adapter.passive-boundary.bind.reason");

            bool passed = result.Succeeded
                && result.BindsControl
                && !result.BindsView
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
                "qa.playercontrol-binding-adapter." + suffix,
                "qa.playercontrol-binding-adapter." + suffix + ".reason");
        }

        private void LogResult(string label, bool passed, PlayerControlBindingResult result)
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

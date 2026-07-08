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
    public sealed class QaPlayerBindingAuthoringValidatorFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F50A_PLAYER_BINDING_AUTHORING_VALIDATOR_QA]";

        [SerializeField] private GameObject validationRoot;
        [SerializeField] private PlayerSlotDeclaration slotDeclaration;
        [SerializeField] private PlayerSlotOccupancy slotOccupancy;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
        [SerializeField] private PlayerControlBehaviour controlBehaviour;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("F50A/Run Player Binding Authoring Validator Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            PrepareReadyAuthoringState();

            bool valid = ValidateValidHierarchy();
            bool missingRoot = ValidateMissingRoot();
            bool missingSlot = ValidateMissingPlayerSlotDeclaration();
            bool missingOccupancy = ValidateMissingPlayerSlotOccupancy();
            bool missingReadiness = ValidateMissingActorReadinessBehaviour();
            bool missingEntry = ValidateMissingPlayerEntryBehaviour();
            bool missingView = ValidateMissingPlayerViewBehaviour();
            bool missingControl = ValidateMissingPlayerControlBehaviour();
            bool topologyPropagation = ValidateTopologyIssuePropagation();
            bool diagnostics = ValidateDiagnosticReport();
            bool passiveBoundary = ValidatePassiveBoundary();

            bool passed = component
                && valid
                && missingRoot
                && missingSlot
                && missingOccupancy
                && missingReadiness
                && missingEntry
                && missingView
                && missingControl
                && topologyPropagation
                && diagnostics
                && passiveBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' valid='{valid}' missingRoot='{missingRoot}' " +
                $"missingSlot='{missingSlot}' missingOccupancy='{missingOccupancy}' missingReadiness='{missingReadiness}' " +
                $"missingEntry='{missingEntry}' missingView='{missingView}' missingControl='{missingControl}' " +
                $"topologyPropagation='{topologyPropagation}' diagnostics='{diagnostics}' passiveBoundary='{passiveBoundary}'.";

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
                && slotDeclaration != null
                && slotOccupancy != null
                && actorReadinessBehaviour != null
                && entryBehaviour != null
                && viewBehaviour != null
                && controlBehaviour != null;

            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private void PrepareReadyAuthoringState()
        {
            actorReadinessBehaviour.BeginNewCycle("qa.player-binding-authoring-validator.prepare.begin-cycle");
            actorReadinessBehaviour.MarkReadyForControl("qa.player-binding-authoring-validator.prepare.ready-for-control");

            entryBehaviour.RebuildEntry("qa.player-binding-authoring-validator.prepare.entry-rebuild");
            entryBehaviour.SetState(PlayerEntryState.Active, "qa.player-binding-authoring-validator.prepare.entry-active");

            viewBehaviour.RebuildView("qa.player-binding-authoring-validator.prepare.view-rebuild");
            viewBehaviour.RefreshPlayerEntryEvidence("qa.player-binding-authoring-validator.prepare.view-refresh-entry");
            viewBehaviour.SetState(PlayerViewState.Active, "qa.player-binding-authoring-validator.prepare.view-active");

            controlBehaviour.RebuildControl("qa.player-binding-authoring-validator.prepare.control-rebuild");
            controlBehaviour.RefreshPlayerEntryFromBehaviour("qa.player-binding-authoring-validator.prepare.control-refresh-entry");
            controlBehaviour.SetState(PlayerControlState.Active, "qa.player-binding-authoring-validator.prepare.control-active");
        }

        private bool ValidateValidHierarchy()
        {
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.player-binding-authoring-validator.valid-hierarchy",
                "qa.player-binding-authoring-validator.valid-hierarchy.reason");

            bool passed = report.Succeeded
                && report.IsReadyForFullBinding
                && report.PlayerSlotDeclarationCount == 1
                && report.PlayerSlotOccupancyCount == 1
                && report.ActorReadinessBehaviourCount == 1
                && report.PlayerEntryBehaviourCount == 1
                && report.PlayerViewBehaviourCount == 1
                && report.PlayerControlBehaviourCount == 1
                && !report.BindsView
                && !report.BindsControl
                && !report.ActivatesCamera
                && !report.ActivatesInput
                && !report.EnablesMovement
                && !report.SpawnsActor;

            LogReport("Valid hierarchy", passed, report);
            return passed;
        }

        private bool ValidateMissingRoot()
        {
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateHierarchy(
                null,
                "qa.player-binding-authoring-validator.missing-root",
                "qa.player-binding-authoring-validator.missing-root.reason");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.MissingValidationRoot)
                && !report.IsReadyForFullBinding;

            LogReport("Missing root", passed, report);
            return passed;
        }

        private bool ValidateMissingPlayerSlotDeclaration()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new PlayerSlotDeclaration[0],
                new[] { slotOccupancy },
                new[] { actorReadinessBehaviour },
                new[] { entryBehaviour },
                new[] { viewBehaviour },
                new[] { controlBehaviour },
                "missing-slot");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.MissingPlayerSlotDeclaration)
                && !report.IsReadyForFullBinding;

            LogReport("Missing PlayerSlotDeclaration", passed, report);
            return passed;
        }

        private bool ValidateMissingPlayerSlotOccupancy()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new[] { slotDeclaration },
                new PlayerSlotOccupancy[0],
                new[] { actorReadinessBehaviour },
                new[] { entryBehaviour },
                new[] { viewBehaviour },
                new[] { controlBehaviour },
                "missing-occupancy");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.MissingPlayerSlotOccupancy)
                && !report.IsReadyForFullBinding;

            LogReport("Missing PlayerSlotOccupancy", passed, report);
            return passed;
        }

        private bool ValidateMissingActorReadinessBehaviour()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new[] { slotDeclaration },
                new[] { slotOccupancy },
                new ActorReadinessBehaviour[0],
                new[] { entryBehaviour },
                new[] { viewBehaviour },
                new[] { controlBehaviour },
                "missing-readiness");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.MissingActorReadinessBehaviour)
                && !report.Succeeded;

            LogReport("Missing ActorReadinessBehaviour", passed, report);
            return passed;
        }

        private bool ValidateMissingPlayerEntryBehaviour()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new[] { slotDeclaration },
                new[] { slotOccupancy },
                new[] { actorReadinessBehaviour },
                new PlayerEntryBehaviour[0],
                new[] { viewBehaviour },
                new[] { controlBehaviour },
                "missing-entry");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.MissingPlayerEntryBehaviour)
                && !report.IsReadyForFullBinding;

            LogReport("Missing PlayerEntryBehaviour", passed, report);
            return passed;
        }

        private bool ValidateMissingPlayerViewBehaviour()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new[] { slotDeclaration },
                new[] { slotOccupancy },
                new[] { actorReadinessBehaviour },
                new[] { entryBehaviour },
                new PlayerViewBehaviour[0],
                new[] { controlBehaviour },
                "missing-view");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.MissingPlayerViewBehaviour)
                && !report.IsReadyForViewBinding;

            LogReport("Missing PlayerViewBehaviour", passed, report);
            return passed;
        }

        private bool ValidateMissingPlayerControlBehaviour()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new[] { slotDeclaration },
                new[] { slotOccupancy },
                new[] { actorReadinessBehaviour },
                new[] { entryBehaviour },
                new[] { viewBehaviour },
                new PlayerControlBehaviour[0],
                "missing-control");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.MissingPlayerControlBehaviour)
                && !report.IsReadyForControlBinding;

            LogReport("Missing PlayerControlBehaviour", passed, report);
            return passed;
        }

        private bool ValidateTopologyIssuePropagation()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new[] { slotDeclaration },
                new[] { slotOccupancy },
                new[] { actorReadinessBehaviour },
                new[] { entryBehaviour },
                new[] { viewBehaviour, viewBehaviour },
                new[] { controlBehaviour },
                "topology-propagation");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.PlayerViewTopologyIssue)
                && report.HasIssue(PlayerBindingAuthoringIssueKind.BindingReadinessIssue)
                && !report.IsReadyForFullBinding;

            LogReport("Topology issue propagation", passed, report);
            return passed;
        }

        private bool ValidateDiagnosticReport()
        {
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.player-binding-authoring-validator.diagnostics",
                "qa.player-binding-authoring-validator.diagnostics.reason");

            bool passed = report.Succeeded
                && report.HasDiagnosticReport
                && report.DiagnosticReport.HasMessage(PlayerBindingDiagnosticMessageKind.ReadyForFullBinding)
                && report.DiagnosticReport.HasMessage(PlayerBindingDiagnosticMessageKind.PassiveBoundary);

            LogReport("Diagnostic report", passed, report);
            return passed;
        }

        private bool ValidatePassiveBoundary()
        {
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.player-binding-authoring-validator.passive-boundary",
                "qa.player-binding-authoring-validator.passive-boundary.reason");

            bool passed = !report.BindsView
                && !report.BindsControl
                && !report.ActivatesCamera
                && !report.ActivatesInput
                && !report.EnablesMovement
                && !report.SpawnsActor;

            LogReport("Passive boundary", passed, report);
            return passed;
        }

        private PlayerBindingAuthoringValidationReport ValidateWith(
            PlayerSlotDeclaration[] slots,
            PlayerSlotOccupancy[] occupancies,
            ActorReadinessBehaviour[] readiness,
            PlayerEntryBehaviour[] entries,
            PlayerViewBehaviour[] views,
            PlayerControlBehaviour[] controls,
            string reason)
        {
            return PlayerBindingAuthoringValidator.ValidateComponents(
                slots,
                occupancies,
                readiness,
                entries,
                views,
                controls,
                "qa.player-binding-authoring-validator." + reason,
                "qa.player-binding-authoring-validator." + reason + ".reason");
        }

        private void LogReport(string label, bool passed, PlayerBindingAuthoringValidationReport report)
        {
            string diagnostic = report != null ? report.ToDiagnosticString() : "<null>";
            Debug.Log($"{LogPrefix} {label}. passed='{passed}' report=\"{diagnostic}\".");
        }
    }
}

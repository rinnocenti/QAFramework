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
    public sealed class QaPlayerBindingAuthoringIssueCleanupFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F50C_PLAYER_BINDING_AUTHORING_ISSUE_CLEANUP_QA]";

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

        [ContextMenu("F50C/Run Player Binding Authoring Issue Cleanup Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            PrepareReadyAuthoringState();

            bool valid = ValidateValidReportHasNoIssueBuckets();
            bool missingSlot = ValidateMissingSlotRootCauseCleanup();
            bool missingOccupancy = ValidateMissingOccupancyRootCauseCleanup();
            bool topologyOnly = ValidateTopologyOnlyRootCauseRemainsVisible();
            bool defaultSummary = ValidateDefaultSummarySuppressesDerivedNoise();
            bool detailedSummary = ValidateDetailedSummaryPreservesDerivedIssues();
            bool passiveBoundary = ValidatePassiveBoundary();

            bool passed = component
                && valid
                && missingSlot
                && missingOccupancy
                && topologyOnly
                && defaultSummary
                && detailedSummary
                && passiveBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' valid='{valid}' missingSlot='{missingSlot}' " +
                $"missingOccupancy='{missingOccupancy}' topologyOnly='{topologyOnly}' " +
                $"defaultSummary='{defaultSummary}' detailedSummary='{detailedSummary}' passiveBoundary='{passiveBoundary}'.";

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
            actorReadinessBehaviour.BeginNewCycle("qa.player-binding-authoring-issue-cleanup.prepare.begin-cycle");
            actorReadinessBehaviour.MarkReadyForControl("qa.player-binding-authoring-issue-cleanup.prepare.ready-for-control");

            entryBehaviour.RebuildEntry("qa.player-binding-authoring-issue-cleanup.prepare.entry-rebuild");
            entryBehaviour.SetState(PlayerEntryState.Active, "qa.player-binding-authoring-issue-cleanup.prepare.entry-active");

            viewBehaviour.RebuildView("qa.player-binding-authoring-issue-cleanup.prepare.view-rebuild");
            viewBehaviour.RefreshPlayerEntryEvidence("qa.player-binding-authoring-issue-cleanup.prepare.view-refresh-entry");
            viewBehaviour.SetState(PlayerViewState.Active, "qa.player-binding-authoring-issue-cleanup.prepare.view-active");

            controlBehaviour.RebuildControl("qa.player-binding-authoring-issue-cleanup.prepare.control-rebuild");
            controlBehaviour.RefreshPlayerEntryFromBehaviour("qa.player-binding-authoring-issue-cleanup.prepare.control-refresh-entry");
            controlBehaviour.SetState(PlayerControlState.Active, "qa.player-binding-authoring-issue-cleanup.prepare.control-active");
        }

        private bool ValidateValidReportHasNoIssueBuckets()
        {
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.player-binding-authoring-issue-cleanup.valid",
                "qa.player-binding-authoring-issue-cleanup.valid.reason");

            bool passed = report.Succeeded
                && report.IssueCount == 0
                && report.RootCauseIssueCount == 0
                && report.DerivedIssueCount == 0
                && !report.HasRootCauseIssues
                && !report.HasDerivedIssues;

            LogReport("Valid report has no issue buckets", passed, report);
            return passed;
        }

        private bool ValidateMissingSlotRootCauseCleanup()
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
                && report.HasRootCauseIssue(PlayerBindingAuthoringIssueKind.MissingPlayerSlotDeclaration)
                && !report.HasDerivedIssue(PlayerBindingAuthoringIssueKind.MissingPlayerSlotDeclaration)
                && report.RootCauseIssueCount == 1
                && report.DerivedIssueCount > 0;

            LogReport("Missing PlayerSlotDeclaration root cause cleanup", passed, report);
            return passed;
        }

        private bool ValidateMissingOccupancyRootCauseCleanup()
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
                && report.HasRootCauseIssue(PlayerBindingAuthoringIssueKind.MissingPlayerSlotOccupancy)
                && report.RootCauseIssueCount == 1
                && report.DerivedIssueCount > 0;

            LogReport("Missing PlayerSlotOccupancy root cause cleanup", passed, report);
            return passed;
        }

        private bool ValidateTopologyOnlyRootCauseRemainsVisible()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new[] { slotDeclaration },
                new[] { slotOccupancy },
                new[] { actorReadinessBehaviour },
                new[] { entryBehaviour },
                new[] { viewBehaviour, viewBehaviour },
                new[] { controlBehaviour },
                "topology-only");

            bool passed = report.Failed
                && report.HasIssue(PlayerBindingAuthoringIssueKind.PlayerViewTopologyIssue)
                && report.HasRootCauseIssue(PlayerBindingAuthoringIssueKind.PlayerViewTopologyIssue)
                && report.RootCauseIssueCount > 0
                && report.DerivedIssueCount > 0;

            LogReport("Topology-only issue remains root cause", passed, report);
            return passed;
        }

        private bool ValidateDefaultSummarySuppressesDerivedNoise()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new PlayerSlotDeclaration[0],
                new[] { slotOccupancy },
                new[] { actorReadinessBehaviour },
                new[] { entryBehaviour },
                new[] { viewBehaviour },
                new[] { controlBehaviour },
                "default-summary");

            string diagnostic = report.ToDiagnosticString();
            bool passed = diagnostic.Contains("rootIssue[0]='MissingPlayerSlotDeclaration'")
                && diagnostic.Contains("derivedIssuesSuppressed='")
                && !diagnostic.Contains("derivedIssue[0]")
                && !diagnostic.Contains("issue[1]");

            LogReport("Default summary suppresses derived noise", passed, report);
            return passed;
        }

        private bool ValidateDetailedSummaryPreservesDerivedIssues()
        {
            PlayerBindingAuthoringValidationReport report = ValidateWith(
                new PlayerSlotDeclaration[0],
                new[] { slotOccupancy },
                new[] { actorReadinessBehaviour },
                new[] { entryBehaviour },
                new[] { viewBehaviour },
                new[] { controlBehaviour },
                "detailed-summary");

            string detailed = report.ToDetailedDiagnosticString();
            bool passed = detailed.Contains("rootIssue[0]='MissingPlayerSlotDeclaration'")
                && detailed.Contains("derivedIssue[0]")
                && detailed.Contains("BindingDiagnosticError")
                && detailed.Contains("issue[0]");

            LogDetailed("Detailed summary preserves derived issues", passed, report);
            return passed;
        }

        private bool ValidatePassiveBoundary()
        {
            PlayerBindingAuthoringValidationReport report = PlayerBindingAuthoringValidator.ValidateHierarchy(
                validationRoot,
                "qa.player-binding-authoring-issue-cleanup.passive-boundary",
                "qa.player-binding-authoring-issue-cleanup.passive-boundary.reason");

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
                "qa.player-binding-authoring-issue-cleanup." + reason,
                "qa.player-binding-authoring-issue-cleanup." + reason + ".reason");
        }

        private void LogReport(string label, bool passed, PlayerBindingAuthoringValidationReport report)
        {
            string diagnostic = report != null ? report.ToDiagnosticString() : "<null>";
            Debug.Log($"{LogPrefix} {label}. passed='{passed}' report=\"{diagnostic}\".");
        }

        private void LogDetailed(string label, bool passed, PlayerBindingAuthoringValidationReport report)
        {
            string diagnostic = report != null ? report.ToDetailedDiagnosticString() : "<null>";
            Debug.Log($"{LogPrefix} {label}. passed='{passed}' report=\"{diagnostic}\".");
        }
    }
}

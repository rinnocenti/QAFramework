using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerBinding;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerTopology;
using Immersive.Framework.PlayerViews;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F49L Player Binding Diagnostics Fixture")]
    public sealed class QaPlayerBindingDiagnosticsFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F49L_PLAYER_BINDING_DIAGNOSTICS_QA]";
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player-binding-diagnostics.actor";
        private const string ExpectedViewCameraName = "QA_PlayerBindingDiagnostics_ViewCamera";
        private const string ExpectedViewTargetName = "QA_PlayerBindingDiagnostics_ViewTarget";
        private const string ExpectedControlTargetName = "QA_PlayerBindingDiagnostics_ControlTarget";
        private const string ExpectedInputSourceId = "qa.input.binding-diagnostics.intent";

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

        [ContextMenu("F49L/Run Player Binding Diagnostics Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            bool ready = ValidateReadyReport();
            bool missingSummary = ValidateMissingSummaryReport();
            bool missingTopology = ValidateMissingTopologyReport();
            bool viewBlocked = ValidateViewBlockedReport();
            bool controlBlocked = ValidateControlBlockedReport();
            bool noParticipants = ValidateNoParticipantsWarningReport();
            bool issueMessages = ValidateReadinessIssueMessages();
            bool passiveBoundary = ValidatePassiveBoundary();

            bool passed = component
                && ready
                && missingSummary
                && missingTopology
                && viewBlocked
                && controlBlocked
                && noParticipants
                && issueMessages
                && passiveBoundary;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' ready='{ready}' missingSummary='{missingSummary}' " +
                $"missingTopology='{missingTopology}' viewBlocked='{viewBlocked}' controlBlocked='{controlBlocked}' " +
                $"noParticipants='{noParticipants}' issueMessages='{issueMessages}' passiveBoundary='{passiveBoundary}'.";

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
            bool passed = RequiredSlotDeclaration() != null
                && RequiredSlotOccupancy() != null
                && RequiredActorReadinessBehaviour() != null
                && RequiredEntryBehaviour() != null
                && RequiredViewBehaviour() != null
                && RequiredControlBehaviour() != null;
            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private bool ValidateReadyReport()
        {
            PlayerBindingDiagnosticReport report = PlayerBindingDiagnosticReporter.CreateReport(
                BuildReadySummary("qa.player-binding-diagnostics.ready"),
                "qa.player-binding-diagnostics.ready",
                "qa.player-binding-diagnostics.ready.reason");

            bool passed = report.Succeeded
                && report.IsReadyForViewBinding
                && report.IsReadyForControlBinding
                && report.IsReadyForFullBinding
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.ReadyForViewBinding)
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.ReadyForControlBinding)
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.ReadyForFullBinding)
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.PassiveBoundary)
                && report.ErrorCount == 0
                && !report.BindsView
                && !report.BindsControl
                && !report.ActivatesCamera
                && !report.ActivatesInput
                && !report.EnablesMovement
                && !report.SpawnsActor;

            LogResult("Ready report", passed, report);
            return passed;
        }

        private bool ValidateMissingSummaryReport()
        {
            PlayerBindingDiagnosticReport report = PlayerBindingDiagnosticReporter.CreateReport(
                null,
                "qa.player-binding-diagnostics.missing-summary",
                "qa.player-binding-diagnostics.missing-summary.reason");

            bool passed = report.Failed
                && !report.HasSummary
                && report.ErrorCount == 1
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.MissingReadinessSummary)
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.PassiveBoundary);

            LogResult("Missing summary report", passed, report);
            return passed;
        }

        private bool ValidateMissingTopologyReport()
        {
            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(
                null,
                null,
                null,
                "qa.player-binding-diagnostics.missing-topology",
                "qa.player-binding-diagnostics.missing-topology.reason");

            PlayerBindingDiagnosticReport report = PlayerBindingDiagnosticReporter.CreateReport(summary, "qa.player-binding-diagnostics.missing-topology", "qa.player-binding-diagnostics.missing-topology.report");
            bool passed = report.Failed
                && !report.IsReadyForViewBinding
                && !report.IsReadyForControlBinding
                && report.HasReadinessIssue(PlayerBindingReadinessIssueKind.MissingPlayerTopology)
                && report.HasReadinessIssue(PlayerBindingReadinessIssueKind.MissingPlayerViewTopology)
                && report.HasReadinessIssue(PlayerBindingReadinessIssueKind.MissingPlayerControlTopology);

            LogResult("Missing topology report", passed, report);
            return passed;
        }

        private bool ValidateViewBlockedReport()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-diagnostics.view-blocked.topology");
            PlayerViewSnapshot first = CreateViewSnapshot(PlayerViewState.Active, ExpectedSlotId, true, PlayerEntryState.Active, "qa.player-binding-diagnostics.view-blocked.first");
            PlayerViewSnapshot second = CreateViewSnapshot(PlayerViewState.Bound, ExpectedSlotId, true, PlayerEntryState.Active, "qa.player-binding-diagnostics.view-blocked.second");
            PlayerViewTopologyValidationResult viewTopology = PlayerViewTopologyValidator.Validate(topology, new[] { first, second }, "qa.player-binding-diagnostics.view-blocked.view", "qa.player-binding-diagnostics.view-blocked.view.reason");
            PlayerControlTopologyValidationResult controlTopology = BuildReadyControlTopology(topology, "qa.player-binding-diagnostics.view-blocked.control");
            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-diagnostics.view-blocked", "qa.player-binding-diagnostics.view-blocked.summary");
            PlayerBindingDiagnosticReport report = PlayerBindingDiagnosticReporter.CreateReport(summary, "qa.player-binding-diagnostics.view-blocked", "qa.player-binding-diagnostics.view-blocked.report");

            bool passed = report.Failed
                && !report.IsReadyForViewBinding
                && report.IsReadyForControlBinding
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.ViewBindingNotReady)
                && report.HasReadinessIssue(PlayerBindingReadinessIssueKind.PlayerViewTopologyIssue);

            LogResult("View blocked report", passed, report);
            return passed;
        }

        private bool ValidateControlBlockedReport()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-diagnostics.control-blocked.topology");
            PlayerViewTopologyValidationResult viewTopology = BuildReadyViewTopology(topology, "qa.player-binding-diagnostics.control-blocked.view");
            PlayerControlSnapshot first = CreateControlSnapshot(PlayerControlState.Active, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-binding-diagnostics.control-blocked.first");
            PlayerControlSnapshot second = CreateControlSnapshot(PlayerControlState.Bound, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-binding-diagnostics.control-blocked.second");
            PlayerControlTopologyValidationResult controlTopology = PlayerControlTopologyValidator.Validate(topology, new[] { first, second }, "qa.player-binding-diagnostics.control-blocked.control", "qa.player-binding-diagnostics.control-blocked.control.reason");
            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-diagnostics.control-blocked", "qa.player-binding-diagnostics.control-blocked.summary");
            PlayerBindingDiagnosticReport report = PlayerBindingDiagnosticReporter.CreateReport(summary, "qa.player-binding-diagnostics.control-blocked", "qa.player-binding-diagnostics.control-blocked.report");

            bool passed = report.Failed
                && report.IsReadyForViewBinding
                && !report.IsReadyForControlBinding
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.ControlBindingNotReady)
                && report.HasReadinessIssue(PlayerBindingReadinessIssueKind.PlayerControlTopologyIssue);

            LogResult("Control blocked report", passed, report);
            return passed;
        }

        private bool ValidateNoParticipantsWarningReport()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-diagnostics.no-participants.topology");
            PlayerViewTopologyValidationResult viewTopology = PlayerViewTopologyValidator.Validate(topology, Array.Empty<PlayerViewSnapshot>(), "qa.player-binding-diagnostics.no-participants.view", "qa.player-binding-diagnostics.no-participants.view.reason");
            PlayerControlTopologyValidationResult controlTopology = PlayerControlTopologyValidator.Validate(topology, Array.Empty<PlayerControlSnapshot>(), "qa.player-binding-diagnostics.no-participants.control", "qa.player-binding-diagnostics.no-participants.control.reason");
            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-diagnostics.no-participants", "qa.player-binding-diagnostics.no-participants.summary");
            PlayerBindingDiagnosticReport report = PlayerBindingDiagnosticReporter.CreateReport(summary, "qa.player-binding-diagnostics.no-participants", "qa.player-binding-diagnostics.no-participants.report");

            bool passed = report.Succeeded
                && !report.IsReadyForViewBinding
                && !report.IsReadyForControlBinding
                && report.WarningCount >= 2
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.NoParticipatingPlayerView)
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.NoParticipatingPlayerControl);

            LogResult("No participants warning report", passed, report);
            return passed;
        }

        private bool ValidateReadinessIssueMessages()
        {
            PlayerEntrySnapshot entry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-binding-diagnostics.issue-messages.entry-a");
            PlayerEntrySnapshot duplicate = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-binding-diagnostics.issue-messages.entry-b");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { entry, duplicate }, "qa.player-binding-diagnostics.issue-messages.topology");
            PlayerViewTopologyValidationResult viewTopology = BuildReadyViewTopology(topology, "qa.player-binding-diagnostics.issue-messages.view");
            PlayerControlTopologyValidationResult controlTopology = BuildReadyControlTopology(topology, "qa.player-binding-diagnostics.issue-messages.control");
            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-diagnostics.issue-messages", "qa.player-binding-diagnostics.issue-messages.summary");
            PlayerBindingDiagnosticReport report = PlayerBindingDiagnosticReporter.CreateReport(summary, "qa.player-binding-diagnostics.issue-messages", "qa.player-binding-diagnostics.issue-messages.report");

            bool passed = report.Failed
                && report.HasReadinessIssue(PlayerBindingReadinessIssueKind.PlayerTopologyIssue)
                && report.HasReadinessIssue(PlayerBindingReadinessIssueKind.PlayerViewTopologyIssue)
                && report.HasReadinessIssue(PlayerBindingReadinessIssueKind.PlayerControlTopologyIssue)
                && report.HasMessage(PlayerBindingDiagnosticMessageKind.ReadinessIssue);

            LogResult("Readiness issue messages", passed, report);
            return passed;
        }

        private bool ValidatePassiveBoundary()
        {
            PlayerBindingDiagnosticReport report = PlayerBindingDiagnosticReporter.CreateReport(
                BuildReadySummary("qa.player-binding-diagnostics.passive-boundary"),
                "qa.player-binding-diagnostics.passive-boundary",
                "qa.player-binding-diagnostics.passive-boundary.reason");

            bool passed = report.HasMessage(PlayerBindingDiagnosticMessageKind.PassiveBoundary)
                && !report.BindsView
                && !report.BindsControl
                && !report.ActivatesCamera
                && !report.ActivatesInput
                && !report.EnablesMovement
                && !report.SpawnsActor;

            LogResult("Passive boundary", passed, report);
            return passed;
        }

        private PlayerBindingReadinessSummary BuildReadySummary(string reason)
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology(reason + ".topology");
            PlayerViewTopologyValidationResult viewTopology = BuildReadyViewTopology(topology, reason + ".view");
            PlayerControlTopologyValidationResult controlTopology = BuildReadyControlTopology(topology, reason + ".control");
            return PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, reason, reason + ".summary");
        }

        private PlayerTopologyValidationResult BuildReadyPlayerTopology(string reason)
        {
            PlayerEntrySnapshot entry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, reason + ".entry");
            return BuildTopology(new[] { entry }, reason);
        }

        private PlayerViewTopologyValidationResult BuildReadyViewTopology(PlayerTopologyValidationResult topology, string reason)
        {
            PlayerViewSnapshot view = CreateViewSnapshot(PlayerViewState.Active, ExpectedSlotId, true, PlayerEntryState.Active, reason + ".view");
            return PlayerViewTopologyValidator.Validate(topology, new[] { view }, reason, reason + ".view-topology");
        }

        private PlayerControlTopologyValidationResult BuildReadyControlTopology(PlayerTopologyValidationResult topology, string reason)
        {
            PlayerControlSnapshot control = CreateControlSnapshot(PlayerControlState.Active, ExpectedSlotId, true, PlayerEntryState.Active, true, reason + ".control");
            return PlayerControlTopologyValidator.Validate(topology, new[] { control }, reason, reason + ".control-topology");
        }

        private PlayerTopologyValidationResult BuildTopology(PlayerEntrySnapshot[] entries, string reason)
        {
            PlayerSlotSet slotSet = BuildSlotSet(reason + ".slot-set");
            return PlayerTopologyValidator.Validate(slotSet, entries, "qa.player-binding-diagnostics.topology", reason);
        }

        private PlayerSlotSet BuildSlotSet(string reason)
        {
            if (!RequiredSlotDeclaration().TryCreateDescriptor("qa.player-binding-diagnostics.slot-declaration", out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue slotIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot descriptor for F49L QA. " + slotIssue.Message);
            }

            if (!RequiredSlotOccupancy().TryCreateDescriptor("qa.player-binding-diagnostics.slot-occupancy", out PlayerSlotOccupancyDescriptor occupancy, out PlayerSlotSetIssue occupancyIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot occupancy descriptor for F49L QA. " + occupancyIssue.Message);
            }

            return PlayerSlotSet.FromDescriptors(
                new[] { descriptor },
                new[] { occupancy },
                "qa.player-binding-diagnostics.slot-set",
                reason);
        }

        private static PlayerEntrySnapshot CreateEntrySnapshot(
            PlayerEntryState state,
            ActorReadinessState readinessState,
            string slotIdText,
            string actorIdText,
            string reason)
        {
            var readiness = new ActorReadinessSnapshot(readinessState, reason + ".readiness");
            return new PlayerEntry(
                new PlayerSlotId(slotIdText),
                ActorId.From(actorIdText),
                state,
                readiness,
                string.Empty,
                reason).CreateSnapshot();
        }

        private static PlayerViewSnapshot CreateViewSnapshot(
            PlayerViewState state,
            string slotIdText,
            bool hasPlayerEntryEvidence,
            PlayerEntryState playerEntryState,
            string reason)
        {
            return new PlayerViewSnapshot(
                new PlayerSlotId(slotIdText),
                state,
                true,
                true,
                hasPlayerEntryEvidence,
                playerEntryState,
                ExpectedViewCameraName,
                ExpectedViewTargetName,
                reason);
        }

        private static PlayerControlSnapshot CreateControlSnapshot(
            PlayerControlState state,
            string slotIdText,
            bool hasPlayerEntryEvidence,
            PlayerEntryState playerEntryState,
            bool readyForControl,
            string reason)
        {
            return new PlayerControlSnapshot(
                new PlayerSlotId(slotIdText),
                state,
                hasPlayerEntryEvidence,
                playerEntryState,
                readyForControl,
                true,
                ExpectedControlTargetName,
                ExpectedInputSourceId,
                string.Empty,
                reason);
        }

        private PlayerSlotDeclaration RequiredSlotDeclaration()
        {
            if (slotDeclaration == null)
            {
                slotDeclaration = GetComponentInChildren<PlayerSlotDeclaration>();
            }

            if (slotDeclaration == null)
            {
                throw new InvalidOperationException("F49L QA requires a PlayerSlotDeclaration reference.");
            }

            return slotDeclaration;
        }

        private PlayerSlotOccupancy RequiredSlotOccupancy()
        {
            if (slotOccupancy == null)
            {
                slotOccupancy = GetComponentInChildren<PlayerSlotOccupancy>();
            }

            if (slotOccupancy == null)
            {
                throw new InvalidOperationException("F49L QA requires a PlayerSlotOccupancy reference.");
            }

            return slotOccupancy;
        }

        private ActorReadinessBehaviour RequiredActorReadinessBehaviour()
        {
            if (actorReadinessBehaviour == null)
            {
                actorReadinessBehaviour = GetComponentInChildren<ActorReadinessBehaviour>();
            }

            if (actorReadinessBehaviour == null)
            {
                throw new InvalidOperationException("F49L QA requires an ActorReadinessBehaviour reference.");
            }

            return actorReadinessBehaviour;
        }

        private PlayerEntryBehaviour RequiredEntryBehaviour()
        {
            if (entryBehaviour == null)
            {
                entryBehaviour = GetComponentInChildren<PlayerEntryBehaviour>();
            }

            if (entryBehaviour == null)
            {
                throw new InvalidOperationException("F49L QA requires a PlayerEntryBehaviour reference.");
            }

            return entryBehaviour;
        }

        private PlayerViewBehaviour RequiredViewBehaviour()
        {
            if (viewBehaviour == null)
            {
                viewBehaviour = GetComponentInChildren<PlayerViewBehaviour>();
            }

            if (viewBehaviour == null)
            {
                throw new InvalidOperationException("F49L QA requires a PlayerViewBehaviour reference.");
            }

            return viewBehaviour;
        }

        private PlayerControlBehaviour RequiredControlBehaviour()
        {
            if (controlBehaviour == null)
            {
                controlBehaviour = GetComponentInChildren<PlayerControlBehaviour>();
            }

            if (controlBehaviour == null)
            {
                throw new InvalidOperationException("F49L QA requires a PlayerControlBehaviour reference.");
            }

            return controlBehaviour;
        }

        private static void LogResult(string step, bool passed, PlayerBindingDiagnosticReport report)
        {
            Debug.Log($"{LogPrefix} {step}. passed='{passed}' report='{report.ToDiagnosticString()}'.");
        }
    }
}

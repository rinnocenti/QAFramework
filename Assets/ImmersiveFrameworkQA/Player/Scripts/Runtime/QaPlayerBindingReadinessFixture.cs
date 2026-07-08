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
    [AddComponentMenu("Immersive Framework QA/Player/F49K Player Binding Readiness Fixture")]
    public sealed class QaPlayerBindingReadinessFixture : MonoBehaviour
    {
        private const string LogPrefix = "[F49K_PLAYER_BINDING_READINESS_QA]";
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player-binding-readiness.actor";
        private const string ExpectedViewCameraName = "QA_PlayerBindingReadiness_ViewCamera";
        private const string ExpectedViewTargetName = "QA_PlayerBindingReadiness_ViewTarget";
        private const string ExpectedControlTargetName = "QA_PlayerBindingReadiness_ControlTarget";
        private const string ExpectedInputSourceId = "qa.input.binding-readiness.intent";

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

        [ContextMenu("F49K/Run Player Binding Readiness Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            bool valid = ValidateValidUnityAuthoredReadiness();
            bool missingTopology = ValidateMissingTopologyEvidence();
            bool missingView = ValidateMissingViewTopology();
            bool missingControl = ValidateMissingControlTopology();
            bool viewIssue = ValidateViewTopologyIssueBlocksViewReadiness();
            bool controlIssue = ValidateControlTopologyIssueBlocksControlReadiness();
            bool mismatch = ValidateTopologyReferenceMismatch();
            bool noParticipants = ValidateNoParticipantsIsNonBlockingNotReady();
            bool topologyPropagation = ValidatePlayerTopologyIssuePropagation();

            bool passed = component
                && valid
                && missingTopology
                && missingView
                && missingControl
                && viewIssue
                && controlIssue
                && mismatch
                && noParticipants
                && topologyPropagation;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' valid='{valid}' missingTopology='{missingTopology}' " +
                $"missingView='{missingView}' missingControl='{missingControl}' viewIssue='{viewIssue}' " +
                $"controlIssue='{controlIssue}' mismatch='{mismatch}' noParticipants='{noParticipants}' " +
                $"topologyPropagation='{topologyPropagation}'.";

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

        private bool ValidateValidUnityAuthoredReadiness()
        {
            PlayerBindingReadinessSummary summary = BuildReadySummary("qa.player-binding-readiness.valid");
            bool passed = summary.Succeeded
                && summary.IsReadyForViewBinding
                && summary.IsReadyForControlBinding
                && summary.IsReadyForFullBinding
                && summary.PlayerSlotCount == 1
                && summary.PlayerEntryCount == 1
                && summary.ParticipatingPlayerViewCount == 1
                && summary.ParticipatingPlayerControlCount == 1
                && !summary.BindsView
                && !summary.BindsControl
                && !summary.ActivatesCamera
                && !summary.ActivatesInput
                && !summary.EnablesMovement;

            LogResult("Valid Unity-authored binding readiness", passed, summary);
            return passed;
        }

        private bool ValidateMissingTopologyEvidence()
        {
            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(
                null,
                null,
                null,
                "qa.player-binding-readiness.missing-topology",
                "qa.player-binding-readiness.missing-topology.reason");

            bool passed = summary.Failed
                && !summary.IsReadyForViewBinding
                && !summary.IsReadyForControlBinding
                && HasIssue(summary, PlayerBindingReadinessIssueKind.MissingPlayerTopology)
                && HasIssue(summary, PlayerBindingReadinessIssueKind.MissingPlayerViewTopology)
                && HasIssue(summary, PlayerBindingReadinessIssueKind.MissingPlayerControlTopology);

            LogResult("Missing topology evidence", passed, summary);
            return passed;
        }

        private bool ValidateMissingViewTopology()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-readiness.missing-view.topology");
            PlayerControlTopologyValidationResult controlTopology = BuildReadyControlTopology(topology, "qa.player-binding-readiness.missing-view.control");
            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(
                topology,
                null,
                controlTopology,
                "qa.player-binding-readiness.missing-view",
                "qa.player-binding-readiness.missing-view.reason");

            bool passed = summary.Failed
                && !summary.IsReadyForViewBinding
                && summary.IsReadyForControlBinding
                && HasIssue(summary, PlayerBindingReadinessIssueKind.MissingPlayerViewTopology);

            LogResult("Missing PlayerView topology", passed, summary);
            return passed;
        }

        private bool ValidateMissingControlTopology()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-readiness.missing-control.topology");
            PlayerViewTopologyValidationResult viewTopology = BuildReadyViewTopology(topology, "qa.player-binding-readiness.missing-control.view");
            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(
                topology,
                viewTopology,
                null,
                "qa.player-binding-readiness.missing-control",
                "qa.player-binding-readiness.missing-control.reason");

            bool passed = summary.Failed
                && summary.IsReadyForViewBinding
                && !summary.IsReadyForControlBinding
                && HasIssue(summary, PlayerBindingReadinessIssueKind.MissingPlayerControlTopology);

            LogResult("Missing PlayerControl topology", passed, summary);
            return passed;
        }

        private bool ValidateViewTopologyIssueBlocksViewReadiness()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-readiness.view-issue.topology");
            PlayerViewSnapshot first = CreateViewSnapshot(PlayerViewState.Active, ExpectedSlotId, true, PlayerEntryState.Active, "qa.player-binding-readiness.view-issue.first");
            PlayerViewSnapshot second = CreateViewSnapshot(PlayerViewState.Bound, ExpectedSlotId, true, PlayerEntryState.Active, "qa.player-binding-readiness.view-issue.second");
            PlayerViewTopologyValidationResult viewTopology = PlayerViewTopologyValidator.Validate(topology, new[] { first, second }, "qa.player-binding-readiness.view-issue.view", "qa.player-binding-readiness.view-issue.view.reason");
            PlayerControlTopologyValidationResult controlTopology = BuildReadyControlTopology(topology, "qa.player-binding-readiness.view-issue.control");

            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-readiness.view-issue", "qa.player-binding-readiness.view-issue.reason");
            bool passed = summary.Failed
                && !summary.IsReadyForViewBinding
                && summary.IsReadyForControlBinding
                && HasIssue(summary, PlayerBindingReadinessIssueKind.PlayerViewTopologyIssue);

            LogResult("PlayerView topology issue blocks view readiness", passed, summary);
            return passed;
        }

        private bool ValidateControlTopologyIssueBlocksControlReadiness()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-readiness.control-issue.topology");
            PlayerViewTopologyValidationResult viewTopology = BuildReadyViewTopology(topology, "qa.player-binding-readiness.control-issue.view");
            PlayerControlSnapshot first = CreateControlSnapshot(PlayerControlState.Active, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-binding-readiness.control-issue.first");
            PlayerControlSnapshot second = CreateControlSnapshot(PlayerControlState.Bound, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-binding-readiness.control-issue.second");
            PlayerControlTopologyValidationResult controlTopology = PlayerControlTopologyValidator.Validate(topology, new[] { first, second }, "qa.player-binding-readiness.control-issue.control", "qa.player-binding-readiness.control-issue.control.reason");

            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-readiness.control-issue", "qa.player-binding-readiness.control-issue.reason");
            bool passed = summary.Failed
                && summary.IsReadyForViewBinding
                && !summary.IsReadyForControlBinding
                && HasIssue(summary, PlayerBindingReadinessIssueKind.PlayerControlTopologyIssue);

            LogResult("PlayerControl topology issue blocks control readiness", passed, summary);
            return passed;
        }

        private bool ValidateTopologyReferenceMismatch()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-readiness.mismatch.topology-a");
            PlayerTopologyValidationResult otherTopology = BuildReadyPlayerTopology("qa.player-binding-readiness.mismatch.topology-b");
            PlayerViewTopologyValidationResult viewTopology = BuildReadyViewTopology(otherTopology, "qa.player-binding-readiness.mismatch.view");
            PlayerControlTopologyValidationResult controlTopology = BuildReadyControlTopology(topology, "qa.player-binding-readiness.mismatch.control");

            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-readiness.mismatch", "qa.player-binding-readiness.mismatch.reason");
            bool passed = summary.Failed
                && !summary.IsReadyForViewBinding
                && summary.IsReadyForControlBinding
                && HasIssue(summary, PlayerBindingReadinessIssueKind.PlayerViewTopologyPlayerTopologyMismatch);

            LogResult("Topology reference mismatch", passed, summary);
            return passed;
        }

        private bool ValidateNoParticipantsIsNonBlockingNotReady()
        {
            PlayerTopologyValidationResult topology = BuildReadyPlayerTopology("qa.player-binding-readiness.no-participants.topology");
            PlayerViewTopologyValidationResult viewTopology = PlayerViewTopologyValidator.Validate(topology, Array.Empty<PlayerViewSnapshot>(), "qa.player-binding-readiness.no-participants.view", "qa.player-binding-readiness.no-participants.view.reason");
            PlayerControlTopologyValidationResult controlTopology = PlayerControlTopologyValidator.Validate(topology, Array.Empty<PlayerControlSnapshot>(), "qa.player-binding-readiness.no-participants.control", "qa.player-binding-readiness.no-participants.control.reason");

            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-readiness.no-participants", "qa.player-binding-readiness.no-participants.reason");
            bool passed = summary.Succeeded
                && !summary.IsReadyForViewBinding
                && !summary.IsReadyForControlBinding
                && HasIssue(summary, PlayerBindingReadinessIssueKind.NoParticipatingPlayerView)
                && HasIssue(summary, PlayerBindingReadinessIssueKind.NoParticipatingPlayerControl)
                && summary.BlockingIssueCount == 0;

            LogResult("No participants is non-blocking but not ready", passed, summary);
            return passed;
        }

        private bool ValidatePlayerTopologyIssuePropagation()
        {
            PlayerEntrySnapshot entry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-binding-readiness.topology-propagation.entry-a");
            PlayerEntrySnapshot duplicate = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-binding-readiness.topology-propagation.entry-b");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { entry, duplicate }, "qa.player-binding-readiness.topology-propagation.topology");
            PlayerViewTopologyValidationResult viewTopology = BuildReadyViewTopology(topology, "qa.player-binding-readiness.topology-propagation.view");
            PlayerControlTopologyValidationResult controlTopology = BuildReadyControlTopology(topology, "qa.player-binding-readiness.topology-propagation.control");

            PlayerBindingReadinessSummary summary = PlayerBindingReadinessSummarizer.Summarize(topology, viewTopology, controlTopology, "qa.player-binding-readiness.topology-propagation", "qa.player-binding-readiness.topology-propagation.reason");
            bool passed = summary.Failed
                && !summary.IsReadyForViewBinding
                && !summary.IsReadyForControlBinding
                && HasIssue(summary, PlayerBindingReadinessIssueKind.PlayerTopologyIssue);

            LogResult("PlayerTopology issue propagation", passed, summary);
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
            return PlayerTopologyValidator.Validate(slotSet, entries, "qa.player-binding-readiness.topology", reason);
        }

        private PlayerSlotSet BuildSlotSet(string reason)
        {
            if (!RequiredSlotDeclaration().TryCreateDescriptor("qa.player-binding-readiness.slot-declaration", out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue slotIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot descriptor for F49K QA. " + slotIssue.Message);
            }

            if (!RequiredSlotOccupancy().TryCreateDescriptor("qa.player-binding-readiness.slot-occupancy", out PlayerSlotOccupancyDescriptor occupancy, out PlayerSlotSetIssue occupancyIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot occupancy descriptor for F49K QA. " + occupancyIssue.Message);
            }

            return PlayerSlotSet.FromDescriptors(
                new[] { descriptor },
                new[] { occupancy },
                "qa.player-binding-readiness.slot-set",
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
                throw new InvalidOperationException("F49K QA requires a PlayerSlotDeclaration reference.");
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
                throw new InvalidOperationException("F49K QA requires a PlayerSlotOccupancy reference.");
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
                throw new InvalidOperationException("F49K QA requires an ActorReadinessBehaviour reference.");
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
                throw new InvalidOperationException("F49K QA requires a PlayerEntryBehaviour reference.");
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
                throw new InvalidOperationException("F49K QA requires a PlayerViewBehaviour reference.");
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
                throw new InvalidOperationException("F49K QA requires a PlayerControlBehaviour reference.");
            }

            return controlBehaviour;
        }

        private static bool HasIssue(PlayerBindingReadinessSummary summary, PlayerBindingReadinessIssueKind kind)
        {
            return summary != null && summary.HasIssue(kind);
        }

        private static void LogResult(string step, bool passed, PlayerBindingReadinessSummary summary)
        {
            Debug.Log($"{LogPrefix} {step}. passed='{passed}' result='{summary.ToDiagnosticString()}'.");
        }
    }
}

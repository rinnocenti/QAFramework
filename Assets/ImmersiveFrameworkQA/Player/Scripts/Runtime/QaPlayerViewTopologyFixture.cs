using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerTopology;
using Immersive.Framework.PlayerViews;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F49H PlayerView Topology Fixture")]
    public sealed class QaPlayerViewTopologyFixture : MonoBehaviour
    {
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player-view-topology.actor";
        private const string ExpectedCameraName = "QA_PlayerViewTopology_Camera";
        private const string ExpectedTargetName = "QA_PlayerViewTopology_TargetAnchor";
        private const string LogPrefix = "[F49H_PLAYER_VIEW_TOPOLOGY_QA]";

        [SerializeField] private PlayerSlotDeclaration slotDeclaration;
        [SerializeField] private PlayerSlotOccupancy slotOccupancy;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("Immersive Framework QA/Player/Run F49H PlayerView Topology Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            bool valid = ValidateUnityAuthoredViewTopology();
            bool duplicateView = ValidateDuplicatePlayerViewSlot();
            bool missingSlot = ValidatePlayerViewWithoutSlotDeclaration();
            bool missingEntry = ValidatePlayerViewWithoutPlayerEntry();
            bool staleEntry = ValidatePlayerViewEntryStateMismatch();
            bool boundRequiresEntry = ValidateBoundRequiresTopologyEntry();
            bool releasedIgnored = ValidateReleasedViewDoesNotParticipate();
            bool topologyPropagation = ValidatePlayerTopologyIssuePropagation();

            bool passed = component
                && valid
                && duplicateView
                && missingSlot
                && missingEntry
                && staleEntry
                && boundRequiresEntry
                && releasedIgnored
                && topologyPropagation;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' valid='{valid}' duplicateView='{duplicateView}' " +
                $"missingSlot='{missingSlot}' missingEntry='{missingEntry}' staleEntry='{staleEntry}' " +
                $"boundRequiresEntry='{boundRequiresEntry}' releasedIgnored='{releasedIgnored}' topologyPropagation='{topologyPropagation}'.";

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
                && RequiredViewBehaviour() != null;
            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private bool ValidateUnityAuthoredViewTopology()
        {
            PlayerEntrySnapshot entry = BuildUnityAuthoredEntry(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, "qa.player-view-topology.valid.entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { entry }, "qa.player-view-topology.valid.topology");

            PlayerViewBehaviour view = RequiredViewBehaviour();
            view.RefreshPlayerEntryEvidence("qa.player-view-topology.valid.view-refresh");
            view.SetState(PlayerViewState.Active, "qa.player-view-topology.valid.view-active");

            PlayerViewTopologyValidationResult result = PlayerViewTopologyValidator.Validate(
                topology,
                new[] { view.CreateSnapshot() },
                "qa.player-view-topology.valid",
                "qa.player-view-topology.valid.reason");

            bool passed = result.Succeeded
                && result.PlayerViewCount == 1
                && result.ParticipatingPlayerViewCount == 1
                && !result.ActivatesCamera
                && !result.BindsView
                && !result.BindsControl;

            LogResult("Valid Unity-authored PlayerView topology", passed, result);
            return passed;
        }

        private bool ValidateDuplicatePlayerViewSlot()
        {
            PlayerEntrySnapshot entry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-view-topology.duplicate.entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { entry }, "qa.player-view-topology.duplicate.topology");
            PlayerViewSnapshot first = CreateViewSnapshot(PlayerViewState.Active, ExpectedSlotId, true, PlayerEntryState.Active, "qa.player-view-topology.duplicate.first");
            PlayerViewSnapshot second = CreateViewSnapshot(PlayerViewState.Declared, ExpectedSlotId, true, PlayerEntryState.Active, "qa.player-view-topology.duplicate.second");

            PlayerViewTopologyValidationResult result = PlayerViewTopologyValidator.Validate(
                topology,
                new[] { first, second },
                "qa.player-view-topology.duplicate",
                "qa.player-view-topology.duplicate.reason");

            bool passed = HasIssue(result, PlayerViewTopologyIssueKind.DuplicatePlayerViewSlot);
            LogResult("Duplicate PlayerView slot", passed, result);
            return passed;
        }

        private bool ValidatePlayerViewWithoutSlotDeclaration()
        {
            PlayerEntrySnapshot entry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-view-topology.missing-slot.entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { entry }, "qa.player-view-topology.missing-slot.topology");
            PlayerViewSnapshot view = CreateViewSnapshot(PlayerViewState.Declared, "player.2", false, PlayerEntryState.Configured, "qa.player-view-topology.missing-slot.view");

            PlayerViewTopologyValidationResult result = PlayerViewTopologyValidator.Validate(
                topology,
                new[] { view },
                "qa.player-view-topology.missing-slot",
                "qa.player-view-topology.missing-slot.reason");

            bool passed = HasIssue(result, PlayerViewTopologyIssueKind.PlayerViewWithoutPlayerSlotDeclaration);
            LogResult("PlayerView without PlayerSlot declaration", passed, result);
            return passed;
        }

        private bool ValidatePlayerViewWithoutPlayerEntry()
        {
            PlayerTopologyValidationResult topology = BuildSlotDeclarationOnlyTopology("qa.player-view-topology.missing-entry.topology");
            PlayerViewSnapshot view = CreateViewSnapshot(PlayerViewState.Declared, ExpectedSlotId, false, PlayerEntryState.Configured, "qa.player-view-topology.missing-entry.view");

            PlayerViewTopologyValidationResult result = PlayerViewTopologyValidator.Validate(
                topology,
                new[] { view },
                "qa.player-view-topology.missing-entry",
                "qa.player-view-topology.missing-entry.reason");

            bool passed = HasIssue(result, PlayerViewTopologyIssueKind.PlayerViewWithoutPlayerEntry);
            LogResult("PlayerView without PlayerEntry", passed, result);
            return passed;
        }

        private bool ValidatePlayerViewEntryStateMismatch()
        {
            PlayerEntrySnapshot topologyEntry = CreateEntrySnapshot(PlayerEntryState.Configured, ActorReadinessState.NotReady, ExpectedSlotId, ExpectedActorId, "qa.player-view-topology.stale-entry.topology-entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { topologyEntry }, "qa.player-view-topology.stale-entry.topology");
            PlayerViewSnapshot view = CreateViewSnapshot(PlayerViewState.Declared, ExpectedSlotId, true, PlayerEntryState.Active, "qa.player-view-topology.stale-entry.view");

            PlayerViewTopologyValidationResult result = PlayerViewTopologyValidator.Validate(
                topology,
                new[] { view },
                "qa.player-view-topology.stale-entry",
                "qa.player-view-topology.stale-entry.reason");

            bool passed = HasIssue(result, PlayerViewTopologyIssueKind.PlayerViewPlayerEntryStateMismatch);
            LogResult("PlayerView PlayerEntry state mismatch", passed, result);
            return passed;
        }

        private bool ValidateBoundRequiresTopologyEntry()
        {
            PlayerEntrySnapshot topologyEntry = CreateEntrySnapshot(PlayerEntryState.Configured, ActorReadinessState.NotReady, ExpectedSlotId, ExpectedActorId, "qa.player-view-topology.bound-requires-entry.topology-entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { topologyEntry }, "qa.player-view-topology.bound-requires-entry.topology");
            PlayerViewSnapshot view = CreateViewSnapshot(PlayerViewState.Bound, ExpectedSlotId, true, PlayerEntryState.ViewBound, "qa.player-view-topology.bound-requires-entry.view");

            PlayerViewTopologyValidationResult result = PlayerViewTopologyValidator.Validate(
                topology,
                new[] { view },
                "qa.player-view-topology.bound-requires-entry",
                "qa.player-view-topology.bound-requires-entry.reason");

            bool passed = HasIssue(result, PlayerViewTopologyIssueKind.BoundPlayerViewWithoutViewBoundOrActiveEntry);
            LogResult("Bound PlayerView requires topology entry ViewBound or Active", passed, result);
            return passed;
        }

        private bool ValidateReleasedViewDoesNotParticipate()
        {
            PlayerTopologyValidationResult topology = BuildSlotDeclarationOnlyTopology("qa.player-view-topology.released.topology");
            PlayerViewSnapshot released = CreateViewSnapshot(PlayerViewState.Released, ExpectedSlotId, false, PlayerEntryState.Released, "qa.player-view-topology.released.view");

            PlayerViewTopologyValidationResult result = PlayerViewTopologyValidator.Validate(
                topology,
                new[] { released },
                "qa.player-view-topology.released",
                "qa.player-view-topology.released.reason");

            bool passed = result.Succeeded
                && result.PlayerViewCount == 1
                && result.ReleasedPlayerViewCount == 1
                && result.ParticipatingPlayerViewCount == 0;

            LogResult("Released PlayerView does not participate", passed, result);
            return passed;
        }

        private bool ValidatePlayerTopologyIssuePropagation()
        {
            PlayerEntrySnapshot first = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-view-topology.propagation.first");
            PlayerEntrySnapshot second = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-view-topology.propagation.second");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { first, second }, "qa.player-view-topology.propagation.topology");
            PlayerViewSnapshot view = CreateViewSnapshot(PlayerViewState.Active, ExpectedSlotId, true, PlayerEntryState.Active, "qa.player-view-topology.propagation.view");

            PlayerViewTopologyValidationResult result = PlayerViewTopologyValidator.Validate(
                topology,
                new[] { view },
                "qa.player-view-topology.propagation",
                "qa.player-view-topology.propagation.reason");

            bool passed = topology.Failed && HasIssue(result, PlayerViewTopologyIssueKind.PlayerTopologyIssue);
            LogResult("PlayerTopology issue propagation", passed, result);
            return passed;
        }

        private PlayerEntrySnapshot BuildUnityAuthoredEntry(PlayerEntryState state, ActorReadinessState readinessState, string reason)
        {
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            readiness.BeginNewCycle(reason + ".readiness-new-cycle");

            switch (readinessState)
            {
                case ActorReadinessState.ReadyForView:
                    readiness.MarkReadyForView(reason + ".ready-for-view");
                    break;
                case ActorReadinessState.ReadyForControl:
                    readiness.MarkReadyForControl(reason + ".ready-for-control");
                    break;
                case ActorReadinessState.NotReady:
                    readiness.ClearReadiness(reason + ".not-ready");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(readinessState), readinessState, "Unsupported QA readiness state.");
            }

            entry.RebuildEntry(reason + ".entry-rebuild");
            entry.RefreshActorReadinessFromBehaviour(reason + ".entry-readiness");
            entry.SetState(state, reason + ".entry-state");
            return entry.CreateSnapshot();
        }

        private PlayerTopologyValidationResult BuildTopology(PlayerEntrySnapshot[] entries, string reason)
        {
            PlayerSlotSet slotSet = BuildSlotSet(reason + ".slot-set");
            return PlayerTopologyValidator.Validate(slotSet, entries, "qa.player-view-topology", reason);
        }

        private PlayerTopologyValidationResult BuildSlotDeclarationOnlyTopology(string reason)
        {
            if (!RequiredSlotDeclaration().TryCreateDescriptor("qa.player-view-topology.slot-declaration-only", out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue slotIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot descriptor for F49H QA. " + slotIssue.Message);
            }

            PlayerSlotSet slotSet = PlayerSlotSet.FromDescriptors(
                new[] { descriptor },
                Array.Empty<PlayerSlotOccupancyDescriptor>(),
                "qa.player-view-topology.slot-set",
                reason + ".slot-set");

            return PlayerTopologyValidator.Validate(
                slotSet,
                Array.Empty<PlayerEntrySnapshot>(),
                "qa.player-view-topology",
                reason);
        }

        private PlayerSlotSet BuildSlotSet(string reason)
        {
            if (!RequiredSlotDeclaration().TryCreateDescriptor("qa.player-view-topology.slot-declaration", out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue slotIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot descriptor for F49H QA. " + slotIssue.Message);
            }

            if (!RequiredSlotOccupancy().TryCreateDescriptor("qa.player-view-topology.slot-occupancy", out PlayerSlotOccupancyDescriptor occupancy, out PlayerSlotSetIssue occupancyIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot occupancy descriptor for F49H QA. " + occupancyIssue.Message);
            }

            return PlayerSlotSet.FromDescriptors(
                new[] { descriptor },
                new[] { occupancy },
                "qa.player-view-topology.slot-set",
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
                ExpectedCameraName,
                ExpectedTargetName,
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
                throw new InvalidOperationException("F49H QA requires a PlayerSlotDeclaration reference.");
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
                throw new InvalidOperationException("F49H QA requires a PlayerSlotOccupancy reference.");
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
                throw new InvalidOperationException("F49H QA requires an ActorReadinessBehaviour reference.");
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
                throw new InvalidOperationException("F49H QA requires a PlayerEntryBehaviour reference.");
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
                throw new InvalidOperationException("F49H QA requires a PlayerViewBehaviour reference.");
            }

            return viewBehaviour;
        }

        private static bool HasIssue(PlayerViewTopologyValidationResult result, PlayerViewTopologyIssueKind kind)
        {
            for (int i = 0; i < result.Issues.Count; i++)
            {
                if (result.Issues[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogResult(string step, bool passed, PlayerViewTopologyValidationResult result)
        {
            Debug.Log($"{LogPrefix} {step}. passed='{passed}' result=\"{result.ToDiagnosticString()}\".");
        }
    }
}

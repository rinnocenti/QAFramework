using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerTopology;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F49J PlayerControl Topology Fixture")]
    public sealed class QaPlayerControlTopologyFixture : MonoBehaviour
    {
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player-control-topology.actor";
        private const string ExpectedControlTargetName = "QA_PlayerControlTopology_TargetAnchor";
        private const string ExpectedInputSourceId = "qa.input.control-topology.intent";
        private const string LogPrefix = "[F49J_PLAYER_CONTROL_TOPOLOGY_QA]";

        [SerializeField] private PlayerSlotDeclaration slotDeclaration;
        [SerializeField] private PlayerSlotOccupancy slotOccupancy;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
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

        [ContextMenu("Immersive Framework QA/Player/Run F49J PlayerControl Topology Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            bool valid = ValidateUnityAuthoredControlTopology();
            bool duplicateControl = ValidateDuplicatePlayerControlSlot();
            bool missingSlot = ValidatePlayerControlWithoutSlotDeclaration();
            bool missingEntry = ValidatePlayerControlWithoutPlayerEntry();
            bool staleEntry = ValidatePlayerControlEntryEvidenceMismatch();
            bool boundRequiresEntry = ValidateBoundRequiresTopologyEntry();
            bool activeRequiresReadyEntry = ValidateActiveRequiresControlReadyTopologyEntry();
            bool releasedIgnored = ValidateReleasedControlDoesNotParticipate();
            bool topologyPropagation = ValidatePlayerTopologyIssuePropagation();

            bool passed = component
                && valid
                && duplicateControl
                && missingSlot
                && missingEntry
                && staleEntry
                && boundRequiresEntry
                && activeRequiresReadyEntry
                && releasedIgnored
                && topologyPropagation;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' valid='{valid}' duplicateControl='{duplicateControl}' " +
                $"missingSlot='{missingSlot}' missingEntry='{missingEntry}' staleEntry='{staleEntry}' " +
                $"boundRequiresEntry='{boundRequiresEntry}' activeRequiresReadyEntry='{activeRequiresReadyEntry}' " +
                $"releasedIgnored='{releasedIgnored}' topologyPropagation='{topologyPropagation}'.";

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
                && RequiredControlBehaviour() != null;
            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private bool ValidateUnityAuthoredControlTopology()
        {
            PlayerEntrySnapshot entry = BuildUnityAuthoredEntry(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, "qa.player-control-topology.valid.entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { entry }, "qa.player-control-topology.valid.topology");

            PlayerControlBehaviour control = RequiredControlBehaviour();
            control.RefreshPlayerEntryFromBehaviour("qa.player-control-topology.valid.control-refresh");
            control.SetState(PlayerControlState.Active, "qa.player-control-topology.valid.control-active");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { control.CreateSnapshot() },
                "qa.player-control-topology.valid",
                "qa.player-control-topology.valid.reason");

            bool passed = result.Succeeded
                && result.PlayerControlCount == 1
                && result.ParticipatingPlayerControlCount == 1
                && !result.ActivatesInput
                && !result.BindsControl
                && !result.EnablesMovement;

            LogResult("Valid Unity-authored PlayerControl topology", passed, result);
            return passed;
        }

        private bool ValidateDuplicatePlayerControlSlot()
        {
            PlayerEntrySnapshot entry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-control-topology.duplicate.entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { entry }, "qa.player-control-topology.duplicate.topology");
            PlayerControlSnapshot first = CreateControlSnapshot(PlayerControlState.Active, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-control-topology.duplicate.first");
            PlayerControlSnapshot second = CreateControlSnapshot(PlayerControlState.Bound, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-control-topology.duplicate.second");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { first, second },
                "qa.player-control-topology.duplicate",
                "qa.player-control-topology.duplicate.reason");

            bool passed = HasIssue(result, PlayerControlTopologyIssueKind.DuplicatePlayerControlSlot);
            LogResult("Duplicate PlayerControl slot", passed, result);
            return passed;
        }

        private bool ValidatePlayerControlWithoutSlotDeclaration()
        {
            PlayerEntrySnapshot entry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-control-topology.missing-slot.entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { entry }, "qa.player-control-topology.missing-slot.topology");
            PlayerControlSnapshot control = CreateControlSnapshot(PlayerControlState.Declared, "player.2", false, PlayerEntryState.Configured, false, "qa.player-control-topology.missing-slot.control");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { control },
                "qa.player-control-topology.missing-slot",
                "qa.player-control-topology.missing-slot.reason");

            bool passed = HasIssue(result, PlayerControlTopologyIssueKind.PlayerControlWithoutPlayerSlotDeclaration);
            LogResult("PlayerControl without PlayerSlot declaration", passed, result);
            return passed;
        }

        private bool ValidatePlayerControlWithoutPlayerEntry()
        {
            PlayerTopologyValidationResult topology = BuildSlotDeclarationOnlyTopology("qa.player-control-topology.missing-entry.topology");
            PlayerControlSnapshot control = CreateControlSnapshot(PlayerControlState.Declared, ExpectedSlotId, false, PlayerEntryState.Configured, false, "qa.player-control-topology.missing-entry.control");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { control },
                "qa.player-control-topology.missing-entry",
                "qa.player-control-topology.missing-entry.reason");

            bool passed = HasIssue(result, PlayerControlTopologyIssueKind.PlayerControlWithoutPlayerEntry);
            LogResult("PlayerControl without PlayerEntry", passed, result);
            return passed;
        }

        private bool ValidatePlayerControlEntryEvidenceMismatch()
        {
            PlayerEntrySnapshot topologyEntry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForView, ExpectedSlotId, ExpectedActorId, "qa.player-control-topology.stale-entry.topology-entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { topologyEntry }, "qa.player-control-topology.stale-entry.topology");
            PlayerControlSnapshot control = CreateControlSnapshot(PlayerControlState.Declared, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-control-topology.stale-entry.control");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { control },
                "qa.player-control-topology.stale-entry",
                "qa.player-control-topology.stale-entry.reason");

            bool passed = HasIssue(result, PlayerControlTopologyIssueKind.PlayerControlPlayerEntryEvidenceMismatch);
            LogResult("PlayerControl PlayerEntry evidence mismatch", passed, result);
            return passed;
        }

        private bool ValidateBoundRequiresTopologyEntry()
        {
            PlayerEntrySnapshot topologyEntry = CreateEntrySnapshot(PlayerEntryState.Configured, ActorReadinessState.NotReady, ExpectedSlotId, ExpectedActorId, "qa.player-control-topology.bound-requires-entry.topology-entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { topologyEntry }, "qa.player-control-topology.bound-requires-entry.topology");
            PlayerControlSnapshot control = CreateControlSnapshot(PlayerControlState.Bound, ExpectedSlotId, true, PlayerEntryState.Active, false, "qa.player-control-topology.bound-requires-entry.control");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { control },
                "qa.player-control-topology.bound-requires-entry",
                "qa.player-control-topology.bound-requires-entry.reason");

            bool passed = HasIssue(result, PlayerControlTopologyIssueKind.BoundPlayerControlWithoutActiveEntry);
            LogResult("Bound PlayerControl requires topology entry Active", passed, result);
            return passed;
        }

        private bool ValidateActiveRequiresControlReadyTopologyEntry()
        {
            PlayerEntrySnapshot topologyEntry = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForView, ExpectedSlotId, ExpectedActorId, "qa.player-control-topology.active-requires-ready.topology-entry");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { topologyEntry }, "qa.player-control-topology.active-requires-ready.topology");
            PlayerControlSnapshot control = CreateControlSnapshot(PlayerControlState.Active, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-control-topology.active-requires-ready.control");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { control },
                "qa.player-control-topology.active-requires-ready",
                "qa.player-control-topology.active-requires-ready.reason");

            bool passed = HasIssue(result, PlayerControlTopologyIssueKind.ActivePlayerControlWithoutActiveReadyEntry);
            LogResult("Active PlayerControl requires topology entry Active and ready for control", passed, result);
            return passed;
        }

        private bool ValidateReleasedControlDoesNotParticipate()
        {
            PlayerTopologyValidationResult topology = BuildSlotDeclarationOnlyTopology("qa.player-control-topology.released.topology");
            PlayerControlSnapshot released = CreateControlSnapshot(PlayerControlState.Released, ExpectedSlotId, false, PlayerEntryState.Released, false, "qa.player-control-topology.released.control");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { released },
                "qa.player-control-topology.released",
                "qa.player-control-topology.released.reason");

            bool passed = result.Succeeded
                && result.PlayerControlCount == 1
                && result.ReleasedPlayerControlCount == 1
                && result.ParticipatingPlayerControlCount == 0;

            LogResult("Released PlayerControl does not participate", passed, result);
            return passed;
        }

        private bool ValidatePlayerTopologyIssuePropagation()
        {
            PlayerEntrySnapshot first = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-control-topology.propagation.first");
            PlayerEntrySnapshot second = CreateEntrySnapshot(PlayerEntryState.Active, ActorReadinessState.ReadyForControl, ExpectedSlotId, ExpectedActorId, "qa.player-control-topology.propagation.second");
            PlayerTopologyValidationResult topology = BuildTopology(new[] { first, second }, "qa.player-control-topology.propagation.topology");
            PlayerControlSnapshot control = CreateControlSnapshot(PlayerControlState.Active, ExpectedSlotId, true, PlayerEntryState.Active, true, "qa.player-control-topology.propagation.control");

            PlayerControlTopologyValidationResult result = PlayerControlTopologyValidator.Validate(
                topology,
                new[] { control },
                "qa.player-control-topology.propagation",
                "qa.player-control-topology.propagation.reason");

            bool passed = topology.Failed && HasIssue(result, PlayerControlTopologyIssueKind.PlayerTopologyIssue);
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
            return PlayerTopologyValidator.Validate(slotSet, entries, "qa.player-control-topology", reason);
        }

        private PlayerTopologyValidationResult BuildSlotDeclarationOnlyTopology(string reason)
        {
            if (!RequiredSlotDeclaration().TryCreateDescriptor("qa.player-control-topology.slot-declaration-only", out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue slotIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot descriptor for F49J QA. " + slotIssue.Message);
            }

            PlayerSlotSet slotSet = PlayerSlotSet.FromDescriptors(
                new[] { descriptor },
                Array.Empty<PlayerSlotOccupancyDescriptor>(),
                "qa.player-control-topology.slot-set",
                reason + ".slot-set");

            return PlayerTopologyValidator.Validate(
                slotSet,
                Array.Empty<PlayerEntrySnapshot>(),
                "qa.player-control-topology",
                reason);
        }

        private PlayerSlotSet BuildSlotSet(string reason)
        {
            if (!RequiredSlotDeclaration().TryCreateDescriptor("qa.player-control-topology.slot-declaration", out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue slotIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot descriptor for F49J QA. " + slotIssue.Message);
            }

            if (!RequiredSlotOccupancy().TryCreateDescriptor("qa.player-control-topology.slot-occupancy", out PlayerSlotOccupancyDescriptor occupancy, out PlayerSlotSetIssue occupancyIssue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlot occupancy descriptor for F49J QA. " + occupancyIssue.Message);
            }

            return PlayerSlotSet.FromDescriptors(
                new[] { descriptor },
                new[] { occupancy },
                "qa.player-control-topology.slot-set",
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
                throw new InvalidOperationException("F49J QA requires a PlayerSlotDeclaration reference.");
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
                throw new InvalidOperationException("F49J QA requires a PlayerSlotOccupancy reference.");
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
                throw new InvalidOperationException("F49J QA requires an ActorReadinessBehaviour reference.");
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
                throw new InvalidOperationException("F49J QA requires a PlayerEntryBehaviour reference.");
            }

            return entryBehaviour;
        }

        private PlayerControlBehaviour RequiredControlBehaviour()
        {
            if (controlBehaviour == null)
            {
                controlBehaviour = GetComponentInChildren<PlayerControlBehaviour>();
            }

            if (controlBehaviour == null)
            {
                throw new InvalidOperationException("F49J QA requires a PlayerControlBehaviour reference.");
            }

            return controlBehaviour;
        }

        private static bool HasIssue(PlayerControlTopologyValidationResult result, PlayerControlTopologyIssueKind kind)
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

        private static void LogResult(string step, bool passed, PlayerControlTopologyValidationResult result)
        {
            Debug.Log($"{LogPrefix} {step}. passed='{passed}' result='{result.ToDiagnosticString()}'.");
        }
    }
}

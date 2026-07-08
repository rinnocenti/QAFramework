using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerTopology;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F49F PlayerTopology Passive Fixture")]
    public sealed class QaPlayerTopologyPassiveFixture : MonoBehaviour
    {
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player-topology.actor";
        private const string LogPrefix = "[F49F_PLAYER_TOPOLOGY_QA]";

        [SerializeField] private PlayerSlotDeclaration playerSlotDeclaration;
        [SerializeField] private ActorDeclaration actorDeclaration;
        [SerializeField] private PlayerSlotOccupancy playerSlotOccupancy;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private PlayerEntryBehaviour playerEntryBehaviour;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("Immersive Framework QA/Player/Run F49F PlayerTopology Passive Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            bool valid = ValidateUnityAuthoredTopology();
            bool duplicateEntrySlot = ValidateDuplicatePlayerEntrySlot();
            bool mismatch = ValidatePlayerEntryActorMismatch();
            bool missingOccupancy = ValidateMissingOccupancy();
            bool orphanOccupancy = ValidateOrphanOccupancy();
            bool duplicateActorOccupancy = ValidateDuplicateOccupiedActor();
            bool slotSetPropagation = ValidatePlayerSlotSetIssuePropagation();

            bool passed = component
                && valid
                && duplicateEntrySlot
                && mismatch
                && missingOccupancy
                && orphanOccupancy
                && duplicateActorOccupancy
                && slotSetPropagation;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' valid='{valid}' duplicateEntrySlot='{duplicateEntrySlot}' " +
                $"mismatch='{mismatch}' missingOccupancy='{missingOccupancy}' orphanOccupancy='{orphanOccupancy}' " +
                $"duplicateActorOccupancy='{duplicateActorOccupancy}' slotSetPropagation='{slotSetPropagation}'.";

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
                && RequiredActorDeclaration() != null
                && RequiredOccupancy() != null
                && RequiredActorReadinessBehaviour() != null
                && RequiredPlayerEntryBehaviour() != null;
            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private bool ValidateUnityAuthoredTopology()
        {
            PlayerSlotDescriptor slotDescriptor = CreateSlotDescriptorFromDeclaration();
            PlayerSlotOccupancyDescriptor occupancyDescriptor = CreateOccupancyDescriptorFromDeclaration();
            PlayerEntrySnapshot entrySnapshot = CreateReadyEntrySnapshotFromBehaviour();

            PlayerSlotSet slotSet = PlayerSlotSet.FromDescriptors(
                new[] { slotDescriptor },
                new[] { occupancyDescriptor },
                "qa.player-topology.valid.slot-set",
                "qa.player-topology.valid");

            PlayerTopologyValidationResult result = PlayerTopologyValidator.Validate(
                slotSet,
                new[] { entrySnapshot },
                "qa.player-topology.valid",
                "qa.player-topology.valid");

            bool passed = result.Succeeded
                && result.PlayerSlotCount == 1
                && result.OccupancyCount == 1
                && result.PlayerEntryCount == 1
                && result.BlockingIssueCount == 0;

            LogResult("Valid Unity-authored topology", passed, result);
            return passed;
        }

        private bool ValidateDuplicatePlayerEntrySlot()
        {
            PlayerSlotId slotId = PlayerSlotId.Player1;
            ActorId actorId = ActorId.From(ExpectedActorId);
            PlayerSlotSet slotSet = CreateValidSlotSet(slotId, actorId);
            PlayerEntrySnapshot first = CreateEntry(slotId, actorId, PlayerEntryState.ActorReady, ActorReadinessState.ReadyForView, "qa.player-topology.duplicate-entry.first");
            PlayerEntrySnapshot second = CreateEntry(slotId, actorId, PlayerEntryState.ActorReady, ActorReadinessState.ReadyForView, "qa.player-topology.duplicate-entry.second");

            PlayerTopologyValidationResult result = PlayerTopologyValidator.Validate(
                slotSet,
                new[] { first, second },
                "qa.player-topology.duplicate-entry-slot",
                "qa.player-topology.duplicate-entry-slot");

            bool passed = result.Failed && HasIssue(result, PlayerTopologyIssueKind.DuplicatePlayerEntrySlot);
            LogResult("Duplicate PlayerEntry slot", passed, result);
            return passed;
        }

        private bool ValidatePlayerEntryActorMismatch()
        {
            PlayerSlotId slotId = PlayerSlotId.Player1;
            ActorId occupiedActor = ActorId.From(ExpectedActorId);
            ActorId entryActor = ActorId.From("qa.player-topology.actor.mismatch");
            PlayerSlotSet slotSet = CreateValidSlotSet(slotId, occupiedActor);
            PlayerEntrySnapshot entry = CreateEntry(slotId, entryActor, PlayerEntryState.ActorReady, ActorReadinessState.ReadyForView, "qa.player-topology.mismatch-entry");

            PlayerTopologyValidationResult result = PlayerTopologyValidator.Validate(
                slotSet,
                new[] { entry },
                "qa.player-topology.actor-mismatch",
                "qa.player-topology.actor-mismatch");

            bool passed = result.Failed && HasIssue(result, PlayerTopologyIssueKind.PlayerEntryActorMismatch);
            LogResult("PlayerEntry Actor mismatch", passed, result);
            return passed;
        }

        private bool ValidateMissingOccupancy()
        {
            PlayerSlotId slotId = PlayerSlotId.Player1;
            ActorId actorId = ActorId.From(ExpectedActorId);
            PlayerSlotDescriptor descriptor = CreateSlotDescriptor(slotId, "qa.player-topology.missing-occupancy.slot");
            PlayerSlotSet slotSet = PlayerSlotSet.FromDescriptors(
                new[] { descriptor },
                Array.Empty<PlayerSlotOccupancyDescriptor>(),
                "qa.player-topology.missing-occupancy.slot-set",
                "qa.player-topology.missing-occupancy");
            PlayerEntrySnapshot entry = CreateEntry(slotId, actorId, PlayerEntryState.ActorReady, ActorReadinessState.ReadyForView, "qa.player-topology.missing-occupancy.entry");

            PlayerTopologyValidationResult result = PlayerTopologyValidator.Validate(
                slotSet,
                new[] { entry },
                "qa.player-topology.missing-occupancy",
                "qa.player-topology.missing-occupancy");

            bool passed = result.Failed && HasIssue(result, PlayerTopologyIssueKind.PlayerEntryWithoutPlayerSlotOccupancy);
            LogResult("Missing PlayerSlot occupancy", passed, result);
            return passed;
        }

        private bool ValidateOrphanOccupancy()
        {
            PlayerSlotId slotId = PlayerSlotId.Player1;
            ActorId actorId = ActorId.From(ExpectedActorId);
            PlayerSlotSet slotSet = CreateValidSlotSet(slotId, actorId);

            PlayerTopologyValidationResult result = PlayerTopologyValidator.Validate(
                slotSet,
                Array.Empty<PlayerEntrySnapshot>(),
                "qa.player-topology.orphan-occupancy",
                "qa.player-topology.orphan-occupancy");

            bool passed = result.Failed && HasIssue(result, PlayerTopologyIssueKind.PlayerSlotOccupancyWithoutPlayerEntry);
            LogResult("Occupancy without PlayerEntry", passed, result);
            return passed;
        }

        private bool ValidateDuplicateOccupiedActor()
        {
            PlayerSlotId firstSlot = PlayerSlotId.Player1;
            PlayerSlotId secondSlot = PlayerSlotId.Player2;
            ActorId actorId = ActorId.From(ExpectedActorId);

            PlayerSlotDescriptor firstDescriptor = CreateSlotDescriptor(firstSlot, "qa.player-topology.duplicate-actor.slot-1");
            PlayerSlotDescriptor secondDescriptor = CreateSlotDescriptor(secondSlot, "qa.player-topology.duplicate-actor.slot-2");
            PlayerSlotOccupancyDescriptor firstOccupancy = CreateOccupancy(firstSlot, actorId, "qa.player-topology.duplicate-actor.occupancy-1");
            PlayerSlotOccupancyDescriptor secondOccupancy = CreateOccupancy(secondSlot, actorId, "qa.player-topology.duplicate-actor.occupancy-2");
            PlayerEntrySnapshot firstEntry = CreateEntry(firstSlot, actorId, PlayerEntryState.ActorReady, ActorReadinessState.ReadyForView, "qa.player-topology.duplicate-actor.entry-1");
            PlayerEntrySnapshot secondEntry = CreateEntry(secondSlot, actorId, PlayerEntryState.ActorReady, ActorReadinessState.ReadyForView, "qa.player-topology.duplicate-actor.entry-2");

            PlayerSlotSet slotSet = PlayerSlotSet.FromDescriptors(
                new[] { firstDescriptor, secondDescriptor },
                new[] { firstOccupancy, secondOccupancy },
                "qa.player-topology.duplicate-actor.slot-set",
                "qa.player-topology.duplicate-actor");

            PlayerTopologyValidationResult result = PlayerTopologyValidator.Validate(
                slotSet,
                new[] { firstEntry, secondEntry },
                "qa.player-topology.duplicate-actor",
                "qa.player-topology.duplicate-actor");

            bool passed = result.Failed && HasIssue(result, PlayerTopologyIssueKind.DuplicateOccupiedActor);
            LogResult("Duplicate occupied Actor", passed, result);
            return passed;
        }

        private bool ValidatePlayerSlotSetIssuePropagation()
        {
            PlayerSlotId slotId = PlayerSlotId.Player1;
            ActorId actorId = ActorId.From(ExpectedActorId);
            PlayerSlotDescriptor first = CreateSlotDescriptor(slotId, "qa.player-topology.slot-set-propagation.slot-1");
            PlayerSlotDescriptor duplicate = CreateSlotDescriptor(slotId, "qa.player-topology.slot-set-propagation.slot-2");
            PlayerSlotOccupancyDescriptor occupancy = CreateOccupancy(slotId, actorId, "qa.player-topology.slot-set-propagation.occupancy");
            PlayerEntrySnapshot entry = CreateEntry(slotId, actorId, PlayerEntryState.ActorReady, ActorReadinessState.ReadyForView, "qa.player-topology.slot-set-propagation.entry");

            PlayerSlotSet slotSet = PlayerSlotSet.FromDescriptors(
                new[] { first, duplicate },
                new[] { occupancy },
                "qa.player-topology.slot-set-propagation.slot-set",
                "qa.player-topology.slot-set-propagation");

            PlayerTopologyValidationResult result = PlayerTopologyValidator.Validate(
                slotSet,
                new[] { entry },
                "qa.player-topology.slot-set-propagation",
                "qa.player-topology.slot-set-propagation");

            bool passed = result.Failed && HasIssue(result, PlayerTopologyIssueKind.PlayerSlotSetIssue);
            LogResult("PlayerSlotSet issue propagation", passed, result);
            return passed;
        }

        private PlayerSlotDescriptor CreateSlotDescriptorFromDeclaration()
        {
            PlayerSlotDeclaration declaration = RequiredSlotDeclaration();
            if (!declaration.TryCreateDescriptor("qa.player-topology.slot-declaration", out PlayerSlotDescriptor descriptor, out PlayerSlotSetIssue issue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlotDescriptor from declaration. " + issue.Message);
            }

            return descriptor;
        }

        private PlayerSlotOccupancyDescriptor CreateOccupancyDescriptorFromDeclaration()
        {
            PlayerSlotOccupancy occupancy = RequiredOccupancy();
            if (!occupancy.TryCreateDescriptor("qa.player-topology.occupancy", out PlayerSlotOccupancyDescriptor descriptor, out PlayerSlotSetIssue issue))
            {
                throw new InvalidOperationException("Unable to create PlayerSlotOccupancyDescriptor from declaration. " + issue.Message);
            }

            return descriptor;
        }

        private PlayerEntrySnapshot CreateReadyEntrySnapshotFromBehaviour()
        {
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredPlayerEntryBehaviour();
            readiness.BeginNewCycle("qa.player-topology.valid.actor-new-cycle");
            readiness.MarkReadyForView("qa.player-topology.valid.ready-for-view");
            entry.RebuildEntry("qa.player-topology.valid.entry-rebuild");
            entry.RefreshActorReadinessFromBehaviour("qa.player-topology.valid.refresh-readiness");
            entry.SetState(PlayerEntryState.ActorReady, "qa.player-topology.valid.actor-ready");
            return entry.CreateSnapshot();
        }

        private static PlayerSlotSet CreateValidSlotSet(PlayerSlotId slotId, ActorId actorId)
        {
            return PlayerSlotSet.FromDescriptors(
                new[] { CreateSlotDescriptor(slotId, "qa.player-topology.slot") },
                new[] { CreateOccupancy(slotId, actorId, "qa.player-topology.occupancy") },
                "qa.player-topology.slot-set",
                "qa.player-topology.valid");
        }

        private static PlayerSlotDescriptor CreateSlotDescriptor(PlayerSlotId slotId, string reason)
        {
            return new PlayerSlotDescriptor(
                slotId,
                false,
                slotId.StableText,
                "QA_PlayerTopologyPassive",
                "QA_PlayerTopologyPassive_Slot",
                "qa.player-topology.fixture",
                reason);
        }

        private static PlayerSlotOccupancyDescriptor CreateOccupancy(PlayerSlotId slotId, ActorId actorId, string reason)
        {
            return new PlayerSlotOccupancyDescriptor(
                slotId,
                actorId,
                true,
                slotId.StableText + " -> " + actorId.StableText,
                "QA_PlayerTopologyPassive",
                "QA_PlayerTopologyPassive_Occupancy",
                "qa.player-topology.fixture",
                reason);
        }

        private static PlayerEntrySnapshot CreateEntry(
            PlayerSlotId slotId,
            ActorId actorId,
            PlayerEntryState state,
            ActorReadinessState readinessState,
            string reason)
        {
            var readiness = new ActorReadinessSnapshot(readinessState, reason + ".readiness");
            return new PlayerEntry(
                slotId,
                actorId,
                state,
                readiness,
                string.Empty,
                reason).CreateSnapshot();
        }

        private static bool HasIssue(PlayerTopologyValidationResult result, PlayerTopologyIssueKind kind)
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

        private void LogResult(string step, bool passed, PlayerTopologyValidationResult result)
        {
            Debug.Log($"{LogPrefix} {step}. passed='{passed}' result=\"{result.ToDiagnosticString()}\".", this);
        }

        private PlayerSlotDeclaration RequiredSlotDeclaration()
        {
            if (playerSlotDeclaration == null)
            {
                playerSlotDeclaration = GetComponentInChildren<PlayerSlotDeclaration>();
            }

            if (playerSlotDeclaration == null)
            {
                throw new InvalidOperationException("F49F QA requires a PlayerSlotDeclaration reference.");
            }

            return playerSlotDeclaration;
        }

        private ActorDeclaration RequiredActorDeclaration()
        {
            if (actorDeclaration == null)
            {
                actorDeclaration = GetComponentInChildren<ActorDeclaration>();
            }

            if (actorDeclaration == null)
            {
                throw new InvalidOperationException("F49F QA requires an ActorDeclaration reference.");
            }

            return actorDeclaration;
        }

        private PlayerSlotOccupancy RequiredOccupancy()
        {
            if (playerSlotOccupancy == null)
            {
                playerSlotOccupancy = GetComponentInChildren<PlayerSlotOccupancy>();
            }

            if (playerSlotOccupancy == null)
            {
                throw new InvalidOperationException("F49F QA requires a PlayerSlotOccupancy reference.");
            }

            return playerSlotOccupancy;
        }

        private ActorReadinessBehaviour RequiredActorReadinessBehaviour()
        {
            if (actorReadinessBehaviour == null)
            {
                actorReadinessBehaviour = GetComponentInChildren<ActorReadinessBehaviour>();
            }

            if (actorReadinessBehaviour == null)
            {
                throw new InvalidOperationException("F49F QA requires an ActorReadinessBehaviour reference.");
            }

            return actorReadinessBehaviour;
        }

        private PlayerEntryBehaviour RequiredPlayerEntryBehaviour()
        {
            if (playerEntryBehaviour == null)
            {
                playerEntryBehaviour = GetComponentInChildren<PlayerEntryBehaviour>();
            }

            if (playerEntryBehaviour == null)
            {
                throw new InvalidOperationException("F49F QA requires a PlayerEntryBehaviour reference.");
            }

            return playerEntryBehaviour;
        }
    }
}

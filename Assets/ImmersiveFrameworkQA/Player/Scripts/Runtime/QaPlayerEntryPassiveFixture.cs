using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F49D PlayerEntry Passive Fixture")]
    public sealed class QaPlayerEntryPassiveFixture : MonoBehaviour
    {
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player-entry.actor";

        [SerializeField] private bool runOnStart = true;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("Immersive Framework QA/Player/Run F49D PlayerEntry Passive Smoke")]
        public void RunSmoke()
        {
            PlayerSlotId slotId = PlayerSlotId.Player1;
            ActorId actorId = ActorId.From(ExpectedActorId);

            bool configured = ValidateConfigured(slotId, actorId);
            bool actorReady = ValidateActorReady(slotId, actorId);
            bool active = ValidateActiveWithControlEvidence(slotId, actorId);
            bool immutableWithers = ValidateImmutableWithers(slotId, actorId);
            bool invalidActorReady = ValidateActorReadyRequiresReadiness(slotId, actorId);
            bool suspendedReason = ValidateSuspendedRequiresReason(slotId, actorId);
            bool released = ValidateReleasedSnapshot(slotId, actorId);
            bool interfaceSnapshot = ValidateInterfaceSnapshot(slotId, actorId);

            bool passed = configured
                && actorReady
                && active
                && immutableWithers
                && invalidActorReady
                && suspendedReason
                && released
                && interfaceSnapshot;

            Debug.Log(
                $"[F49D_PLAYER_ENTRY_QA] status='{(passed ? "Succeeded" : "Failed")}' " +
                $"configured='{configured}' actorReady='{actorReady}' active='{active}' " +
                $"immutableWithers='{immutableWithers}' invalidActorReady='{invalidActorReady}' " +
                $"suspendedReason='{suspendedReason}' released='{released}' interface='{interfaceSnapshot}'.",
                this);
        }

        private bool ValidateConfigured(PlayerSlotId slotId, ActorId actorId)
        {
            var entry = new PlayerEntry(slotId, actorId, "qa.player-entry.configured");
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = snapshot is { State: PlayerEntryState.Configured, PlayerSlotId: { Value: { Value: ExpectedSlotId } }, ActorId: { Value: { Value: ExpectedActorId } }, IsActorReadyForView: false, IsActorReadyForControl: false };

            LogStep("Configured", passed, snapshot);
            return passed;
        }

        private bool ValidateActorReady(PlayerSlotId slotId, ActorId actorId)
        {
            var readiness = new ActorReadinessSnapshot(ActorReadinessState.ReadyForView, "qa.player-entry.actor-ready");
            var entry = new PlayerEntry(
                slotId,
                actorId,
                PlayerEntryState.ActorReady,
                readiness,
                string.Empty,
                "qa.player-entry.actor-ready");
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = snapshot is { State: PlayerEntryState.ActorReady, IsActorReady: true, IsActorReadyForView: true, IsActorReadyForControl: false };

            LogStep("ActorReady", passed, snapshot);
            return passed;
        }

        private bool ValidateActiveWithControlEvidence(PlayerSlotId slotId, ActorId actorId)
        {
            var readiness = new ActorReadinessSnapshot(ActorReadinessState.ReadyForControl, "qa.player-entry.control-ready");
            var entry = new PlayerEntry(
                slotId,
                actorId,
                PlayerEntryState.Active,
                readiness,
                string.Empty,
                "qa.player-entry.active");
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = snapshot is { State: PlayerEntryState.Active, IsActive: true, IsActorReadyForView: true, IsActorReadyForControl: true };

            LogStep("Active with control evidence", passed, snapshot);
            return passed;
        }

        private bool ValidateImmutableWithers(PlayerSlotId slotId, ActorId actorId)
        {
            var entry = new PlayerEntry(slotId, actorId, "qa.player-entry.immutable.start");
            var readiness = new ActorReadinessSnapshot(ActorReadinessState.ReadyForView, "qa.player-entry.immutable.ready");

            PlayerEntry readyEntry = entry
                .WithActorReadiness(readiness, "qa.player-entry.immutable.readiness")
                .WithState(PlayerEntryState.ActorReady, "qa.player-entry.immutable.actor-ready");

            PlayerEntrySnapshot originalSnapshot = entry.CreateSnapshot();
            PlayerEntrySnapshot readySnapshot = readyEntry.CreateSnapshot();

            bool passed = originalSnapshot is { State: PlayerEntryState.Configured, IsActorReadyForView: false }
                && readySnapshot is { State: PlayerEntryState.ActorReady, IsActorReadyForView: true }
                && !ReferenceEquals(entry, readyEntry);

            LogStep("Immutable WithActorReadiness/WithState", passed, readySnapshot);
            return passed;
        }

        private bool ValidateActorReadyRequiresReadiness(PlayerSlotId slotId, ActorId actorId)
        {
            var notReady = new ActorReadinessSnapshot(ActorReadinessState.NotReady, string.Empty);
            bool passed = Throws<InvalidOperationException>(() =>
            {
                _ = new PlayerEntry(
                    slotId,
                    actorId,
                    PlayerEntryState.ActorReady,
                    notReady,
                    string.Empty,
                    "qa.player-entry.invalid-actor-ready");
            });

            Debug.Log($"[F49D_PLAYER_ENTRY_QA] ActorReady requires Actor readiness for view. passed='{passed}'.");
            return passed;
        }

        private bool ValidateSuspendedRequiresReason(PlayerSlotId slotId, ActorId actorId)
        {
            var readiness = new ActorReadinessSnapshot(ActorReadinessState.ReadyForView, "qa.player-entry.suspend-ready");
            bool rejectedMissingReason = Throws<ArgumentException>(() =>
            {
                _ = new PlayerEntry(
                    slotId,
                    actorId,
                    PlayerEntryState.Suspended,
                    readiness,
                    string.Empty,
                    "qa.player-entry.suspended-missing-reason");
            });

            PlayerEntry suspended = new PlayerEntry(slotId, actorId, "qa.player-entry.suspension-source")
                .WithActorReadiness(readiness, "qa.player-entry.suspension-readiness")
                .WithSuspension("qa.player-entry.suspended.explicit-reason", "qa.player-entry.suspended");

            PlayerEntrySnapshot snapshot = suspended.CreateSnapshot();
            bool passed = rejectedMissingReason
                && snapshot is { IsSuspended: true, SuspensionReason: "qa.player-entry.suspended.explicit-reason" };

            LogStep("Suspended requires reason", passed, snapshot);
            return passed;
        }

        private bool ValidateReleasedSnapshot(PlayerSlotId slotId, ActorId actorId)
        {
            PlayerEntry entry = new PlayerEntry(slotId, actorId, "qa.player-entry.release-source")
                .Released("qa.player-entry.released");
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = snapshot is { State: PlayerEntryState.Released, IsReleased: true, IsConfigured: false };

            LogStep("Released", passed, snapshot);
            return passed;
        }

        private bool ValidateInterfaceSnapshot(PlayerSlotId slotId, ActorId actorId)
        {
            var readiness = new ActorReadinessSnapshot(ActorReadinessState.ReadyForControl, "qa.player-entry.interface-readiness");
            IPlayerEntry entry = new PlayerEntry(
                slotId,
                actorId,
                PlayerEntryState.Active,
                readiness,
                string.Empty,
                "qa.player-entry.interface");
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = entry.PlayerSlotId.Value.Value == ExpectedSlotId
                && entry.ActorId.Value.Value == ExpectedActorId
                && entry.State == PlayerEntryState.Active
                && entry.IsActorReadyForView
                && entry.IsActorReadyForControl
                && snapshot.State == PlayerEntryState.Active;

            LogStep("IPlayerEntry snapshot", passed, snapshot);
            return passed;
        }

        private static void LogStep(string step, bool passed, PlayerEntrySnapshot snapshot)
        {
            Debug.Log($"[F49D_PLAYER_ENTRY_QA] {step}. passed='{passed}' snapshot=\"{snapshot}\".");
        }

        private static bool Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
                return false;
            }
            catch (TException)
            {
                return true;
            }
        }
    }
}

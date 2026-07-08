using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerEntry;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F49E PlayerEntry Behaviour Fixture")]
    public sealed class QaPlayerEntryBehaviourFixture : MonoBehaviour
    {
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player-entry.behaviour.actor";
        private const string LogPrefix = "[F49E_PLAYER_ENTRY_BEHAVIOUR_QA]";

        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("Immersive Framework QA/Player/Run F49E PlayerEntry Behaviour Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentExists();
            bool initial = ValidateInitialSnapshot();
            bool interfaceExposure = ValidateInterfaceExposure();
            bool readinessRefresh = ValidateReadinessRefreshFromBehaviour();
            bool active = ValidateActiveWithControlEvidence();
            bool invalidActorReady = ValidateActorReadyRequiresReadiness();
            bool suspension = ValidateSuspensionRequiresReason();
            bool release = ValidateReleaseAndRebuild();

            bool passed = component
                && initial
                && interfaceExposure
                && readinessRefresh
                && active
                && invalidActorReady
                && suspension
                && release;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' initial='{initial}' interface='{interfaceExposure}' " +
                $"readinessRefresh='{readinessRefresh}' active='{active}' invalidActorReady='{invalidActorReady}' " +
                $"suspension='{suspension}' release='{release}'.";

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

        private bool ValidateComponentExists()
        {
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            bool passed = entry != null && readiness != null;
            Debug.Log($"{LogPrefix} Component exists. passed='{passed}' entry='{entry.name}' readiness='{readiness.name}'.");
            return passed;
        }

        private bool ValidateInitialSnapshot()
        {
            ResetEntryEvidence("qa.player-entry-behaviour.initial-reset");
            PlayerEntrySnapshot snapshot = RequiredEntryBehaviour()
                .RebuildEntry("qa.player-entry-behaviour.initial")
                .CreateSnapshot();

            bool passed = snapshot.State == PlayerEntryState.Configured
                && snapshot.PlayerSlotId.Value.Value == ExpectedSlotId
                && snapshot.ActorId.Value.Value == ExpectedActorId
                && !snapshot.IsActorReadyForView
                && !snapshot.IsActorReadyForControl;

            LogStep("Initial snapshot", passed, snapshot);
            return passed;
        }

        private bool ValidateInterfaceExposure()
        {
            ResetEntryEvidence("qa.player-entry-behaviour.interface-reset");
            IPlayerEntry entry = RequiredEntryBehaviour();
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = entry.PlayerSlotId.Value.Value == ExpectedSlotId
                && entry.ActorId.Value.Value == ExpectedActorId
                && entry.State == PlayerEntryState.Configured
                && snapshot.State == PlayerEntryState.Configured;

            LogStep("IPlayerEntry exposure", passed, snapshot);
            return passed;
        }

        private bool ValidateReadinessRefreshFromBehaviour()
        {
            ResetEntryEvidence("qa.player-entry-behaviour.readiness-reset");
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();

            readiness.MarkReadyForView("qa.player-entry-behaviour.ready-for-view");
            entry.RefreshActorReadinessFromBehaviour("qa.player-entry-behaviour.refresh-ready-for-view");
            entry.SetState(PlayerEntryState.ActorReady, "qa.player-entry-behaviour.actor-ready");
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = snapshot.State == PlayerEntryState.ActorReady
                && snapshot.IsActorReadyForView
                && !snapshot.IsActorReadyForControl;

            LogStep("Refresh readiness from behaviour", passed, snapshot);
            return passed;
        }

        private bool ValidateActiveWithControlEvidence()
        {
            ResetEntryEvidence("qa.player-entry-behaviour.active-reset");
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();

            readiness.MarkReadyForControl("qa.player-entry-behaviour.ready-for-control");
            entry.RefreshActorReadinessFromBehaviour("qa.player-entry-behaviour.refresh-ready-for-control");
            entry.SetState(PlayerEntryState.Active, "qa.player-entry-behaviour.active");
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = snapshot.State == PlayerEntryState.Active
                && snapshot.IsActive
                && snapshot.IsActorReadyForView
                && snapshot.IsActorReadyForControl;

            LogStep("Active with control evidence", passed, snapshot);
            return passed;
        }

        private bool ValidateActorReadyRequiresReadiness()
        {
            ResetEntryEvidence("qa.player-entry-behaviour.invalid-actor-ready-reset");
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            bool rejected = Throws<InvalidOperationException>(() =>
            {
                entry.SetState(PlayerEntryState.ActorReady, "qa.player-entry-behaviour.invalid-actor-ready");
            });

            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();
            bool passed = rejected && snapshot.State == PlayerEntryState.Configured && !snapshot.IsActorReadyForView;
            LogStep("ActorReady requires readiness", passed, snapshot);
            return passed;
        }

        private bool ValidateSuspensionRequiresReason()
        {
            ResetEntryEvidence("qa.player-entry-behaviour.suspension-reset");
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();

            readiness.MarkReadyForView("qa.player-entry-behaviour.suspend-ready");
            entry.RefreshActorReadinessFromBehaviour("qa.player-entry-behaviour.suspend-readiness");

            bool rejectedMissingReason = Throws<ArgumentException>(() =>
            {
                entry.SetSuspension(string.Empty, "qa.player-entry-behaviour.suspended-missing-reason");
            });

            entry.SetSuspension(
                "qa.player-entry-behaviour.suspended.explicit-reason",
                "qa.player-entry-behaviour.suspended");
            PlayerEntrySnapshot snapshot = entry.CreateSnapshot();

            bool passed = rejectedMissingReason
                && snapshot.IsSuspended
                && snapshot.SuspensionReason == "qa.player-entry-behaviour.suspended.explicit-reason";

            LogStep("Suspended requires reason", passed, snapshot);
            return passed;
        }

        private bool ValidateReleaseAndRebuild()
        {
            ResetEntryEvidence("qa.player-entry-behaviour.release-reset");
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();

            entry.Release("qa.player-entry-behaviour.released");
            PlayerEntrySnapshot releasedSnapshot = entry.CreateSnapshot();
            entry.RebuildEntry("qa.player-entry-behaviour.rebuild-after-release");
            PlayerEntrySnapshot rebuiltSnapshot = entry.CreateSnapshot();

            bool passed = releasedSnapshot.IsReleased
                && rebuiltSnapshot.State == PlayerEntryState.Configured
                && !rebuiltSnapshot.IsReleased;

            LogStep("Released", releasedSnapshot.IsReleased, releasedSnapshot);
            LogStep("Rebuild after release", passed, rebuiltSnapshot);
            return passed;
        }

        private void ResetEntryEvidence(string reason)
        {
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            readiness.BeginNewCycle(reason + ".actor-new-cycle");
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            entry.RebuildEntry(reason + ".entry-rebuild");
        }

        private PlayerEntryBehaviour RequiredEntryBehaviour()
        {
            if (entryBehaviour == null)
            {
                entryBehaviour = GetComponentInChildren<PlayerEntryBehaviour>();
            }

            if (entryBehaviour == null)
            {
                throw new InvalidOperationException("F49E QA requires a PlayerEntryBehaviour reference.");
            }

            return entryBehaviour;
        }

        private ActorReadinessBehaviour RequiredActorReadinessBehaviour()
        {
            if (actorReadinessBehaviour == null)
            {
                actorReadinessBehaviour = GetComponentInChildren<ActorReadinessBehaviour>();
            }

            if (actorReadinessBehaviour == null)
            {
                throw new InvalidOperationException("F49E QA requires an ActorReadinessBehaviour reference.");
            }

            return actorReadinessBehaviour;
        }

        private static void LogStep(string step, bool passed, PlayerEntrySnapshot snapshot)
        {
            Debug.Log($"{LogPrefix} {step}. passed='{passed}' snapshot=\"{snapshot}\".");
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

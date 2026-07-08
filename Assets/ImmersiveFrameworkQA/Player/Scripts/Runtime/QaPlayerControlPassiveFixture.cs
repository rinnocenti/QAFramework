using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F49I PlayerControl Passive Fixture")]
    public sealed class QaPlayerControlPassiveFixture : MonoBehaviour
    {
        private const string ExpectedSlotId = "player.1";
        private const string LogPrefix = "[F49I_PLAYER_CONTROL_QA]";

        [SerializeField] private PlayerControlBehaviour controlBehaviour;
        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private ActorReadinessBehaviour actorReadinessBehaviour;
        [SerializeField] private UnityEngine.Transform controlTarget;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        [ContextMenu("Immersive Framework QA/Player/Run F49I PlayerControl Passive Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            bool declared = ValidateDeclaredSnapshot();
            bool interfaceExposure = ValidateInterfaceExposure();
            bool boundRequiresActiveEntry = ValidateBoundRequiresActiveEntry();
            bool bound = ValidateBoundWithActiveEntry();
            bool activeRequiresControlReady = ValidateActiveRequiresControlReadiness();
            bool active = ValidateActiveWithControlReadyEntry();
            bool suspended = ValidateSuspendedRequiresReason();
            bool optionalTargetInput = ValidateOptionalTargetInputEvidence();
            bool release = ValidateReleaseAndRebuild();

            bool passed = component
                && declared
                && interfaceExposure
                && boundRequiresActiveEntry
                && bound
                && activeRequiresControlReady
                && active
                && suspended
                && optionalTargetInput
                && release;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' declared='{declared}' interface='{interfaceExposure}' " +
                $"boundRequiresActiveEntry='{boundRequiresActiveEntry}' bound='{bound}' " +
                $"activeRequiresControlReady='{activeRequiresControlReady}' active='{active}' " +
                $"suspended='{suspended}' optionalTargetInput='{optionalTargetInput}' release='{release}'.";

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
            bool passed = RequiredControlBehaviour() != null
                && RequiredEntryBehaviour() != null
                && RequiredActorReadinessBehaviour() != null
                && RequiredControlTarget() != null;
            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private bool ValidateDeclaredSnapshot()
        {
            ResetControlEvidence("qa.player-control.declared-reset");
            PlayerControlSnapshot snapshot = RequiredControlBehaviour()
                .RebuildControl("qa.player-control.declared")
                .CreateSnapshot();

            bool passed = snapshot.State == PlayerControlState.Declared
                && snapshot.PlayerSlotId.Value.Value == ExpectedSlotId
                && snapshot.HasPlayerEntryEvidence
                && snapshot.PlayerEntryState == PlayerEntryState.Configured
                && snapshot.HasControlTarget
                && snapshot.HasInputSource
                && !snapshot.IsEligibleForBoundControl
                && !snapshot.IsEligibleForActiveControl;

            LogStep("Declared snapshot", passed, snapshot);
            return passed;
        }

        private bool ValidateInterfaceExposure()
        {
            ResetControlEvidence("qa.player-control.interface-reset");
            IPlayerControl control = RequiredControlBehaviour();
            PlayerControlSnapshot snapshot = control.CreateSnapshot();

            bool passed = control.PlayerSlotId.Value.Value == ExpectedSlotId
                && control.State == PlayerControlState.Declared
                && control.HasPlayerEntryEvidence
                && control.PlayerEntryState == PlayerEntryState.Configured
                && snapshot.State == PlayerControlState.Declared;

            LogStep("IPlayerControl exposure", passed, snapshot);
            return passed;
        }

        private bool ValidateBoundRequiresActiveEntry()
        {
            ResetControlEvidence("qa.player-control.invalid-bound-reset");
            PlayerControlBehaviour control = RequiredControlBehaviour();
            bool rejected = Throws<InvalidOperationException>(() =>
            {
                control.SetState(PlayerControlState.Bound, "qa.player-control.invalid-bound");
            });

            PlayerControlSnapshot snapshot = control.CreateSnapshot();
            bool passed = rejected && snapshot.State == PlayerControlState.Declared && snapshot.PlayerEntryState == PlayerEntryState.Configured;
            LogStep("Bound requires Active PlayerEntry", passed, snapshot);
            return passed;
        }

        private bool ValidateBoundWithActiveEntry()
        {
            ResetControlEvidence("qa.player-control.bound-reset");
            PrepareEntryActiveForView("qa.player-control.bound");

            PlayerControlBehaviour control = RequiredControlBehaviour();
            control.RefreshPlayerEntryFromBehaviour("qa.player-control.bound-refresh-entry");
            control.SetState(PlayerControlState.Bound, "qa.player-control.bound");
            PlayerControlSnapshot snapshot = control.CreateSnapshot();

            bool passed = snapshot.State == PlayerControlState.Bound
                && snapshot.IsEligibleForBoundControl
                && !snapshot.IsEligibleForActiveControl;

            LogStep("Bound with Active PlayerEntry", passed, snapshot);
            return passed;
        }

        private bool ValidateActiveRequiresControlReadiness()
        {
            ResetControlEvidence("qa.player-control.invalid-active-reset");
            PrepareEntryActiveForView("qa.player-control.invalid-active");

            PlayerControlBehaviour control = RequiredControlBehaviour();
            control.RefreshPlayerEntryFromBehaviour("qa.player-control.invalid-active-refresh-entry");
            bool rejected = Throws<InvalidOperationException>(() =>
            {
                control.SetState(PlayerControlState.Active, "qa.player-control.invalid-active");
            });

            PlayerControlSnapshot snapshot = control.CreateSnapshot();
            bool passed = rejected
                && snapshot.State == PlayerControlState.Declared
                && snapshot.IsEligibleForBoundControl
                && !snapshot.IsEligibleForActiveControl;

            LogStep("Active requires Actor readiness for control", passed, snapshot);
            return passed;
        }

        private bool ValidateActiveWithControlReadyEntry()
        {
            ResetControlEvidence("qa.player-control.active-reset");
            PrepareEntryActiveForControl("qa.player-control.active");

            PlayerControlBehaviour control = RequiredControlBehaviour();
            control.RefreshPlayerEntryFromBehaviour("qa.player-control.active-refresh-entry");
            control.SetState(PlayerControlState.Active, "qa.player-control.active");
            PlayerControlSnapshot snapshot = control.CreateSnapshot();

            bool passed = snapshot.State == PlayerControlState.Active
                && snapshot.IsActive
                && snapshot.IsEligibleForBoundControl
                && snapshot.IsEligibleForActiveControl
                && snapshot.IsPlayerEntryReadyForControl;

            LogStep("Active with control-ready PlayerEntry", passed, snapshot);
            return passed;
        }

        private bool ValidateSuspendedRequiresReason()
        {
            ResetControlEvidence("qa.player-control.suspension-reset");
            PlayerControlBehaviour control = RequiredControlBehaviour();

            bool rejectedMissingReason = Throws<ArgumentException>(() =>
            {
                control.SetSuspension(string.Empty, "qa.player-control.suspended-missing-reason");
            });

            control.SetSuspension(
                "qa.player-control.suspended.explicit-reason",
                "qa.player-control.suspended");
            PlayerControlSnapshot snapshot = control.CreateSnapshot();

            bool passed = rejectedMissingReason
                && snapshot.IsSuspended
                && snapshot.SuspensionReason == "qa.player-control.suspended.explicit-reason";

            LogStep("Suspended requires reason", passed, snapshot);
            return passed;
        }

        private bool ValidateOptionalTargetInputEvidence()
        {
            var control = new PlayerControl(
                new Immersive.Framework.PlayerSlots.PlayerSlotId(ExpectedSlotId),
                PlayerControlState.Declared,
                false,
                PlayerEntryState.Configured,
                false,
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                "qa.player-control.optional-no-target-input");
            PlayerControlSnapshot snapshot = control.CreateSnapshot();

            bool passed = snapshot.State == PlayerControlState.Declared
                && !snapshot.HasControlTarget
                && !snapshot.HasInputSource
                && !snapshot.HasPlayerEntryEvidence;

            LogStep("Optional target and input evidence", passed, snapshot);
            return passed;
        }

        private bool ValidateReleaseAndRebuild()
        {
            ResetControlEvidence("qa.player-control.release-reset");
            PlayerControlBehaviour control = RequiredControlBehaviour();

            control.Release("qa.player-control.released");
            PlayerControlSnapshot releasedSnapshot = control.CreateSnapshot();
            control.RebuildControl("qa.player-control.rebuild-after-release");
            PlayerControlSnapshot rebuiltSnapshot = control.CreateSnapshot();

            bool passed = releasedSnapshot.IsReleased
                && rebuiltSnapshot.State == PlayerControlState.Declared
                && !rebuiltSnapshot.IsReleased;

            LogStep("Released", releasedSnapshot.IsReleased, releasedSnapshot);
            LogStep("Rebuild after release", passed, rebuiltSnapshot);
            return passed;
        }

        private void PrepareEntryActiveForView(string reason)
        {
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            readiness.MarkReadyForView(reason + ".ready-for-view");
            entry.RefreshActorReadinessFromBehaviour(reason + ".entry-refresh-ready-for-view");
            entry.SetState(PlayerEntryState.Active, reason + ".entry-active");
        }

        private void PrepareEntryActiveForControl(string reason)
        {
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            readiness.MarkReadyForControl(reason + ".ready-for-control");
            entry.RefreshActorReadinessFromBehaviour(reason + ".entry-refresh-ready-for-control");
            entry.SetState(PlayerEntryState.Active, reason + ".entry-active");
        }

        private void ResetControlEvidence(string reason)
        {
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            readiness.BeginNewCycle(reason + ".actor-new-cycle");
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            entry.RebuildEntry(reason + ".entry-rebuild");
            PlayerControlBehaviour control = RequiredControlBehaviour();
            control.SetControlTarget(RequiredControlTarget(), reason + ".control-target");
            control.SetInputSource("qa.input.local.intent", reason + ".input-source");
            control.RebuildControl(reason + ".control-rebuild");
        }

        private PlayerControlBehaviour RequiredControlBehaviour()
        {
            if (controlBehaviour == null)
            {
                controlBehaviour = GetComponentInChildren<PlayerControlBehaviour>();
            }

            if (controlBehaviour == null)
            {
                throw new InvalidOperationException("F49I QA requires a PlayerControlBehaviour reference.");
            }

            return controlBehaviour;
        }

        private PlayerEntryBehaviour RequiredEntryBehaviour()
        {
            if (entryBehaviour == null)
            {
                entryBehaviour = GetComponentInChildren<PlayerEntryBehaviour>();
            }

            if (entryBehaviour == null)
            {
                throw new InvalidOperationException("F49I QA requires a PlayerEntryBehaviour reference.");
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
                throw new InvalidOperationException("F49I QA requires an ActorReadinessBehaviour reference.");
            }

            return actorReadinessBehaviour;
        }

        private UnityEngine.Transform RequiredControlTarget()
        {
            if (controlTarget == null)
            {
                controlTarget = transform.Find("QA_PlayerControl_TargetAnchor");
            }

            if (controlTarget == null)
            {
                throw new InvalidOperationException("F49I QA requires a control target reference.");
            }

            return controlTarget;
        }

        private static void LogStep(string step, bool passed, PlayerControlSnapshot snapshot)
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

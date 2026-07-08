using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerViews;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F49G PlayerView Passive Fixture")]
    public sealed class QaPlayerViewPassiveFixture : MonoBehaviour
    {
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player-view.actor";
        private const string LogPrefix = "[F49G_PLAYER_VIEW_QA]";

        [SerializeField] private PlayerEntryBehaviour entryBehaviour;
        [SerializeField] private PlayerViewBehaviour viewBehaviour;
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

        [ContextMenu("Immersive Framework QA/Player/Run F49G PlayerView Passive Smoke")]
        public void RunSmoke()
        {
            bool component = ValidateComponentReferences();
            bool declared = ValidateDeclaredSnapshot();
            bool interfaceExposure = ValidateInterfaceExposure();
            bool activeRequiresEntry = ValidateActiveRequiresViewBoundEntry();
            bool bound = ValidateBoundWithViewBoundEntry();
            bool active = ValidateActiveWithActiveEntry();
            bool suspended = ValidateSuspendedRequiresReason();
            bool optionalCameraTarget = ValidateOptionalCameraTargetEvidence();
            bool release = ValidateReleaseAndRebuild();

            bool passed = component
                && declared
                && interfaceExposure
                && activeRequiresEntry
                && bound
                && active
                && suspended
                && optionalCameraTarget
                && release;

            string message = $"{LogPrefix} status='{(passed ? "Succeeded" : "Failed")}' " +
                $"component='{component}' declared='{declared}' interface='{interfaceExposure}' " +
                $"activeRequiresEntry='{activeRequiresEntry}' bound='{bound}' active='{active}' " +
                $"suspended='{suspended}' optionalCameraTarget='{optionalCameraTarget}' release='{release}'.";

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
            bool passed = RequiredEntryBehaviour() != null
                && RequiredViewBehaviour() != null
                && RequiredActorReadinessBehaviour() != null;
            Debug.Log($"{LogPrefix} Component references. passed='{passed}'.");
            return passed;
        }

        private bool ValidateDeclaredSnapshot()
        {
            ResetEvidence("qa.player-view.declared-reset");
            PlayerViewSnapshot snapshot = RequiredViewBehaviour()
                .RebuildView("qa.player-view.declared")
                .CreateSnapshot();

            bool passed = snapshot.State == PlayerViewState.Declared
                && snapshot.PlayerSlotId.Value.Value == ExpectedSlotId
                && snapshot.HasCameraEvidence
                && snapshot.HasTargetEvidence
                && snapshot.HasPlayerEntryEvidence
                && snapshot.PlayerEntryState == PlayerEntryState.Configured
                && !snapshot.IsEligibleForActiveView;

            LogStep("Declared snapshot", passed, snapshot);
            return passed;
        }

        private bool ValidateInterfaceExposure()
        {
            ResetEvidence("qa.player-view.interface-reset");
            IPlayerView view = RequiredViewBehaviour();
            PlayerViewSnapshot snapshot = view.CreateSnapshot();

            bool passed = view.PlayerSlotId.Value.Value == ExpectedSlotId
                && view.State == PlayerViewState.Declared
                && view.HasCameraEvidence
                && view.HasTargetEvidence
                && view.HasPlayerEntryEvidence
                && !view.IsEligibleForActiveView
                && snapshot.State == PlayerViewState.Declared;

            LogStep("IPlayerView exposure", passed, snapshot);
            return passed;
        }

        private bool ValidateActiveRequiresViewBoundEntry()
        {
            ResetEvidence("qa.player-view.invalid-active-reset");
            PlayerViewBehaviour view = RequiredViewBehaviour();
            bool rejected = Throws<InvalidOperationException>(() =>
            {
                view.SetState(PlayerViewState.Active, "qa.player-view.invalid-active");
            });

            PlayerViewSnapshot snapshot = view.CreateSnapshot();
            bool passed = rejected && snapshot.State == PlayerViewState.Declared && !snapshot.IsEligibleForActiveView;
            LogStep("Active requires ViewBound or Active PlayerEntry", passed, snapshot);
            return passed;
        }

        private bool ValidateBoundWithViewBoundEntry()
        {
            ResetEvidence("qa.player-view.bound-reset");
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            PlayerViewBehaviour view = RequiredViewBehaviour();

            readiness.MarkReadyForView("qa.player-view.ready-for-view");
            entry.RefreshActorReadinessFromBehaviour("qa.player-view.entry-refresh-ready-for-view");
            entry.SetState(PlayerEntryState.ViewBound, "qa.player-view.entry-view-bound");
            view.RefreshPlayerEntryEvidence("qa.player-view.refresh-view-bound-entry");
            view.SetState(PlayerViewState.Bound, "qa.player-view.bound");
            PlayerViewSnapshot snapshot = view.CreateSnapshot();

            bool passed = snapshot.State == PlayerViewState.Bound
                && snapshot.HasPlayerEntryEvidence
                && snapshot.PlayerEntryState == PlayerEntryState.ViewBound
                && !snapshot.IsEligibleForActiveView;

            LogStep("Bound with ViewBound PlayerEntry", passed, snapshot);
            return passed;
        }

        private bool ValidateActiveWithActiveEntry()
        {
            ResetEvidence("qa.player-view.active-reset");
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            PlayerViewBehaviour view = RequiredViewBehaviour();

            readiness.MarkReadyForControl("qa.player-view.ready-for-control");
            entry.RefreshActorReadinessFromBehaviour("qa.player-view.entry-refresh-ready-for-control");
            entry.SetState(PlayerEntryState.Active, "qa.player-view.entry-active");
            view.RefreshPlayerEntryEvidence("qa.player-view.refresh-active-entry");
            view.SetState(PlayerViewState.Active, "qa.player-view.active");
            PlayerViewSnapshot snapshot = view.CreateSnapshot();

            bool passed = snapshot.State == PlayerViewState.Active
                && snapshot.HasPlayerEntryEvidence
                && snapshot.PlayerEntryState == PlayerEntryState.Active
                && snapshot.IsEligibleForActiveView;

            LogStep("Active with Active PlayerEntry", passed, snapshot);
            return passed;
        }

        private bool ValidateSuspendedRequiresReason()
        {
            ResetEvidence("qa.player-view.suspension-reset");
            PlayerViewBehaviour view = RequiredViewBehaviour();
            bool rejectedMissingReason = Throws<ArgumentException>(() =>
            {
                view.SetState(PlayerViewState.Suspended, string.Empty);
            });

            view.SetState(PlayerViewState.Suspended, "qa.player-view.suspended.explicit-reason");
            PlayerViewSnapshot snapshot = view.CreateSnapshot();

            bool passed = rejectedMissingReason
                && snapshot.State == PlayerViewState.Suspended
                && snapshot.Reason == "qa.player-view.suspended.explicit-reason";

            LogStep("Suspended requires reason", passed, snapshot);
            return passed;
        }

        private bool ValidateOptionalCameraTargetEvidence()
        {
            PlayerSlotId slotId = PlayerSlotId.Player1;
            var view = new PlayerView(slotId, "qa.player-view.optional-no-camera-target");
            PlayerViewSnapshot snapshot = view.CreateSnapshot();

            bool passed = snapshot.State == PlayerViewState.Declared
                && !snapshot.HasCameraEvidence
                && !snapshot.HasTargetEvidence
                && !snapshot.HasPlayerEntryEvidence;

            LogStep("Optional camera and target evidence", passed, snapshot);
            return passed;
        }

        private bool ValidateReleaseAndRebuild()
        {
            ResetEvidence("qa.player-view.release-reset");
            PlayerViewBehaviour view = RequiredViewBehaviour();

            view.Release("qa.player-view.released");
            PlayerViewSnapshot releasedSnapshot = view.CreateSnapshot();
            view.RebuildView("qa.player-view.rebuild-after-release");
            PlayerViewSnapshot rebuiltSnapshot = view.CreateSnapshot();

            bool passed = releasedSnapshot.IsReleased
                && rebuiltSnapshot.State == PlayerViewState.Declared
                && !rebuiltSnapshot.IsReleased;

            LogStep("Released", releasedSnapshot.IsReleased, releasedSnapshot);
            LogStep("Rebuild after release", passed, rebuiltSnapshot);
            return passed;
        }

        private void ResetEvidence(string reason)
        {
            ActorReadinessBehaviour readiness = RequiredActorReadinessBehaviour();
            readiness.BeginNewCycle(reason + ".actor-new-cycle");
            PlayerEntryBehaviour entry = RequiredEntryBehaviour();
            entry.RebuildEntry(reason + ".entry-rebuild");
            PlayerViewBehaviour view = RequiredViewBehaviour();
            view.RebuildView(reason + ".view-rebuild");
        }

        private PlayerEntryBehaviour RequiredEntryBehaviour()
        {
            if (entryBehaviour == null)
            {
                entryBehaviour = GetComponentInChildren<PlayerEntryBehaviour>();
            }

            if (entryBehaviour == null)
            {
                throw new InvalidOperationException("F49G QA requires a PlayerEntryBehaviour reference.");
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
                throw new InvalidOperationException("F49G QA requires a PlayerViewBehaviour reference.");
            }

            return viewBehaviour;
        }

        private ActorReadinessBehaviour RequiredActorReadinessBehaviour()
        {
            if (actorReadinessBehaviour == null)
            {
                actorReadinessBehaviour = GetComponentInChildren<ActorReadinessBehaviour>();
            }

            if (actorReadinessBehaviour == null)
            {
                throw new InvalidOperationException("F49G QA requires an ActorReadinessBehaviour reference.");
            }

            return actorReadinessBehaviour;
        }

        private static void LogStep(string step, bool passed, PlayerViewSnapshot snapshot)
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

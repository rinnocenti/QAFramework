using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace ImmersiveFrameworkQA.Hub
{
    /// <summary>
    /// Authored QA fixture root placed in the Hub primary scene before Play Mode.
    /// Runtime assembly intentionally: it must be addable to a scene GameObject and
    /// survive Play Mode. Framework composition binds the ActivityRequestTrigger;
    /// this component never binds ports itself.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework QA/Player Surface/Public Navigation Fixture")]
    public sealed class QaPlayerSurfacePublicNavigationFixture : MonoBehaviour
    {
        public const string RootObjectName = "QA_PlayerSurface_PublicNavigation";

        [SerializeField] private ActivityAsset targetActivity;
        [SerializeField] private ActivityAsset secondaryPlayerActivity;
        [SerializeField] private ActivityAsset playerExcludedActivity;
        [SerializeField] private ActivityRequestTrigger enterActivityTrigger;
        [SerializeField] private ActivityRequestTrigger enterSecondaryActivityTrigger;
        [SerializeField] private ActivityRequestTrigger enterPlayerExcludedActivityTrigger;
        [SerializeField] private ActivityRequestTrigger clearActivityTrigger;
        [SerializeField]
        private PlayerSessionScopedAccessConsumer routeConsumerBinding;
        [SerializeField]
        private PlayerSessionScopedAccessConsumer wrongScopeBinding;
        [SerializeField]
        private PlayerSessionScopedAccessConsumer destroyProbeBinding;
        [SerializeField] private PlayerSessionSelectActorCommandTrigger selectActorCommand;
        [SerializeField] private PlayerSessionDefaultActorSelectionCommandTrigger defaultActorSelectionCommand;
        [SerializeField] private PlayerSessionReplaceActorSelectionCommandTrigger replaceActorSelectionCommand;
        [SerializeField] private PlayerSessionClearActorSelectionCommandTrigger clearActorSelectionCommand;
        [SerializeField] private PlayerSessionSelectActorCommandTrigger unavailableSelectActorCommand;
        [SerializeField] private PlayerSlotProfile primaryPlayerSlot;

        public ActivityAsset TargetActivity => targetActivity;
        public ActivityAsset SecondaryPlayerActivity => secondaryPlayerActivity;
        public ActivityAsset PlayerExcludedActivity => playerExcludedActivity;
        public ActivityRequestTrigger EnterActivityTrigger => enterActivityTrigger;
        public ActivityRequestTrigger EnterSecondaryActivityTrigger =>
            enterSecondaryActivityTrigger;
        public ActivityRequestTrigger EnterPlayerExcludedActivityTrigger =>
            enterPlayerExcludedActivityTrigger;
        public ActivityRequestTrigger ClearActivityTrigger => clearActivityTrigger;
        public PlayerSessionScopedAccessConsumer RouteConsumerBinding =>
            routeConsumerBinding;
        public PlayerSessionScopedAccessConsumer WrongScopeBinding =>
            wrongScopeBinding;
        public PlayerSessionScopedAccessConsumer DestroyProbeBinding =>
            destroyProbeBinding;
        public PlayerSessionSelectActorCommandTrigger SelectActorCommand => selectActorCommand;
        public PlayerSessionDefaultActorSelectionCommandTrigger DefaultActorSelectionCommand => defaultActorSelectionCommand;
        public PlayerSessionReplaceActorSelectionCommandTrigger ReplaceActorSelectionCommand => replaceActorSelectionCommand;
        public PlayerSessionClearActorSelectionCommandTrigger ClearActorSelectionCommand => clearActorSelectionCommand;
        public PlayerSessionSelectActorCommandTrigger UnavailableSelectActorCommand => unavailableSelectActorCommand;
        public PlayerSlotProfile PrimaryPlayerSlot => primaryPlayerSlot;

        public void Configure(
            ActivityAsset activity,
            ActivityAsset secondaryActivity,
            ActivityAsset excludedActivity,
            ActivityRequestTrigger enterTrigger,
            ActivityRequestTrigger enterSecondaryTrigger,
            ActivityRequestTrigger enterExcludedTrigger,
            ActivityRequestTrigger clearTrigger,
            PlayerSessionScopedAccessConsumer consumerBinding,
            PlayerSessionScopedAccessConsumer authoredWrongScopeBinding,
            PlayerSessionScopedAccessConsumer authoredDestroyProbeBinding,
            PlayerSessionSelectActorCommandTrigger authoredSelectActorCommand,
            PlayerSessionDefaultActorSelectionCommandTrigger authoredDefaultActorSelectionCommand,
            PlayerSessionReplaceActorSelectionCommandTrigger authoredReplaceActorSelectionCommand,
            PlayerSessionClearActorSelectionCommandTrigger authoredClearActorSelectionCommand,
            PlayerSessionSelectActorCommandTrigger authoredUnavailableSelectActorCommand,
            PlayerSlotProfile playerSlot)
        {
            targetActivity = activity;
            secondaryPlayerActivity = secondaryActivity;
            playerExcludedActivity = excludedActivity;
            enterActivityTrigger = enterTrigger;
            enterSecondaryActivityTrigger = enterSecondaryTrigger;
            enterPlayerExcludedActivityTrigger = enterExcludedTrigger;
            clearActivityTrigger = clearTrigger;
            routeConsumerBinding = consumerBinding;
            wrongScopeBinding = authoredWrongScopeBinding;
            destroyProbeBinding = authoredDestroyProbeBinding;
            selectActorCommand = authoredSelectActorCommand;
            defaultActorSelectionCommand = authoredDefaultActorSelectionCommand;
            replaceActorSelectionCommand = authoredReplaceActorSelectionCommand;
            clearActorSelectionCommand = authoredClearActorSelectionCommand;
            unavailableSelectActorCommand = authoredUnavailableSelectActorCommand;
            primaryPlayerSlot = playerSlot;
        }

        public bool TryValidateAuthoredSurface(out string issue)
        {
            return TryValidateAuthoredSurface(true, out issue);
        }

        /// <summary>
        /// Validates the retained public surface after the negative regression has
        /// intentionally destroyed its one-shot Route stale-access probe.
        /// </summary>
        public bool TryValidateAuthoredSurface(
            bool requireDestroyProbe,
            out string issue)
        {
            if (targetActivity == null)
            {
                issue = "Public navigation fixture is missing Target Activity.";
                return false;
            }

            if (enterActivityTrigger == null)
            {
                issue =
                    "Public navigation fixture is missing enter ActivityRequestTrigger.";
                return false;
            }

            if (secondaryPlayerActivity == null ||
                enterSecondaryActivityTrigger == null ||
                enterSecondaryActivityTrigger.TargetActivity != secondaryPlayerActivity)
            {
                issue =
                    "Public navigation fixture is missing a distinct Player-representing Activity B trigger.";
                return false;
            }

            if (playerExcludedActivity == null ||
                enterPlayerExcludedActivityTrigger == null ||
                enterPlayerExcludedActivityTrigger.TargetActivity != playerExcludedActivity)
            {
                issue =
                    "Public navigation fixture is missing the Player-excluded Activity trigger.";
                return false;
            }

            if (clearActivityTrigger == null)
            {
                issue =
                    "Public navigation fixture is missing clear ActivityRequestTrigger.";
                return false;
            }

            if (enterActivityTrigger.TargetActivity != targetActivity)
            {
                issue =
                    "Enter ActivityRequestTrigger does not target the fixture Activity.";
                return false;
            }

            if (routeConsumerBinding == null)
            {
                issue =
                    "Public navigation fixture is missing Route consumer access binding.";
                return false;
            }

            if (routeConsumerBinding.Scope !=
                LocalPlayerProvisioningConsumerScope.Route)
            {
                issue =
                    "Public navigation scoped consumer must be Route-scoped.";
                return false;
            }

            if (wrongScopeBinding == null ||
                wrongScopeBinding.Scope !=
                    LocalPlayerProvisioningConsumerScope.Activity ||
                wrongScopeBinding.gameObject.scene != gameObject.scene)
            {
                issue =
                    "Public navigation fixture requires one authored Activity-scoped negative binding in the Route scene.";
                return false;
            }

            if (requireDestroyProbe &&
                (destroyProbeBinding == null ||
                 destroyProbeBinding.Scope !=
                    LocalPlayerProvisioningConsumerScope.Route ||
                 destroyProbeBinding.gameObject.scene != gameObject.scene))
            {
                issue =
                    "Public navigation fixture requires one authored Route-scoped destroy probe in the same scene.";
                return false;
            }

            if (!ValidateRouteCommand(selectActorCommand, "Select Actor", out issue) ||
                !ValidateRouteCommand(defaultActorSelectionCommand, "Default Actor Selection", out issue) ||
                !ValidateRouteCommand(replaceActorSelectionCommand, "Replace Actor Selection", out issue) ||
                !ValidateRouteCommand(clearActorSelectionCommand, "Clear Actor Selection", out issue))
            {
                return false;
            }

            if (unavailableSelectActorCommand == null ||
                unavailableSelectActorCommand.Scope !=
                    LocalPlayerProvisioningConsumerScope.Activity ||
                unavailableSelectActorCommand.gameObject.scene != gameObject.scene)
            {
                issue = "Public navigation fixture requires an Activity-scoped Select Actor command in the Route scene.";
                return false;
            }

            if (primaryPlayerSlot == null ||
                !primaryPlayerSlot.PlayerSlotId.IsValid)
            {
                issue =
                    "Public navigation fixture requires a valid primary Player Slot.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool ValidateRouteCommand(
            PlayerSessionCommandTriggerBase command,
            string label,
            out string issue)
        {
            if (command == null || command.gameObject != gameObject ||
                command.gameObject.scene != gameObject.scene ||
                command.Scope != LocalPlayerProvisioningConsumerScope.Route)
            {
                issue = $"Public navigation fixture requires its Route-scoped {label} command on the fixture root.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}

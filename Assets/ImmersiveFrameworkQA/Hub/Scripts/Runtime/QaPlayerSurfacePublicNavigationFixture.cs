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
        [SerializeField] private ActivityRequestTrigger enterActivityTrigger;
        [SerializeField] private ActivityRequestTrigger clearActivityTrigger;
        [SerializeField]
        private LocalPlayerProvisioningConsumerAccessBinding routeConsumerBinding;
        [SerializeField] private PlayerSlotProfile primaryPlayerSlot;
        [SerializeField]
        private LocalPlayerActorSelectionRequestAuthoring
            actorSelectionRequestAuthoring;

        public ActivityAsset TargetActivity => targetActivity;
        public ActivityRequestTrigger EnterActivityTrigger => enterActivityTrigger;
        public ActivityRequestTrigger ClearActivityTrigger => clearActivityTrigger;
        public LocalPlayerProvisioningConsumerAccessBinding RouteConsumerBinding =>
            routeConsumerBinding;
        public PlayerSlotProfile PrimaryPlayerSlot => primaryPlayerSlot;
        public LocalPlayerActorSelectionRequestAuthoring
            ActorSelectionRequestAuthoring => actorSelectionRequestAuthoring;

        public void Configure(
            ActivityAsset activity,
            ActivityRequestTrigger enterTrigger,
            ActivityRequestTrigger clearTrigger,
            LocalPlayerProvisioningConsumerAccessBinding consumerBinding,
            PlayerSlotProfile playerSlot,
            LocalPlayerActorSelectionRequestAuthoring actorSelectionAuthoring)
        {
            targetActivity = activity;
            enterActivityTrigger = enterTrigger;
            clearActivityTrigger = clearTrigger;
            routeConsumerBinding = consumerBinding;
            primaryPlayerSlot = playerSlot;
            actorSelectionRequestAuthoring = actorSelectionAuthoring;
        }

        public bool TryValidateAuthoredSurface(out string issue)
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
                    "Public navigation consumer binding must be Route-scoped.";
                return false;
            }

            if (primaryPlayerSlot == null ||
                !primaryPlayerSlot.PlayerSlotId.IsValid)
            {
                issue =
                    "Public navigation fixture requires a valid primary Player Slot.";
                return false;
            }

            if (actorSelectionRequestAuthoring == null)
            {
                issue =
                    "Public navigation fixture is missing the explicit Local Player Actor Selection Request authoring.";
                return false;
            }

            if (!actorSelectionRequestAuthoring.TryValidateConfiguration(
                    out string actorSelectionIssue))
            {
                issue =
                    "Public navigation fixture references an invalid Local Player Actor Selection Request authoring. " +
                    actorSelectionIssue;
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}

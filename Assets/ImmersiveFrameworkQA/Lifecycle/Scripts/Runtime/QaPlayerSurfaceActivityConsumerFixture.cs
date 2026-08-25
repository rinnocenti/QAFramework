using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace ImmersiveFrameworkQA.Lifecycle
{
    /// <summary>
    /// Scene-local authored evidence for the Player Surface Activity consumer.
    /// Framework composition owns its runtime binding.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework QA/Player Surface/Activity Consumer Fixture")]
    public sealed class QaPlayerSurfaceActivityConsumerFixture : MonoBehaviour
    {
        public const string RootObjectName =
            "QA_PlayerSurface_ActivityConsumer";

        [SerializeField]
        private PlayerSessionScopedAccessConsumer consumerBinding;

        public PlayerSessionScopedAccessConsumer ConsumerBinding =>
            consumerBinding;

        public void Configure(
            PlayerSessionScopedAccessConsumer binding)
        {
            consumerBinding = binding;
        }

        public bool TryValidateAuthoredSurface(out string issue)
        {
            if (consumerBinding == null)
            {
                issue =
                    "Player Surface Activity fixture is missing its consumer binding.";
                return false;
            }

            if (consumerBinding.Scope !=
                LocalPlayerProvisioningConsumerScope.Activity)
            {
                issue =
                    "Player Surface Activity consumer binding must be Activity-scoped.";
                return false;
            }

            if (consumerBinding.gameObject != gameObject ||
                consumerBinding.gameObject.scene != gameObject.scene)
            {
                issue =
                    "Player Surface Activity fixture and binding must share one scene-local GameObject.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}

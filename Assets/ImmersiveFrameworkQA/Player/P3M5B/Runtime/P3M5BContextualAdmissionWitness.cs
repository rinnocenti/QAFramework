using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.P3M5B
{
    /// <summary>
    /// Scene-owned QA locator for the exact admission surface authored by one Activity.
    /// It deliberately remains outside the LocalPlayerHost root, which can migrate to
    /// the Session physical lifetime during Scene-Provided Actor adoption.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class P3M5BContextualAdmissionWitness : MonoBehaviour
    {
        [SerializeField]
        private SceneLocalPlayerAdmissionAuthoring admissionAuthoring;

        [SerializeField]
        private LocalPlayerProvisioningConsumerAccessBinding activityConsumerBinding;

        public SceneLocalPlayerAdmissionAuthoring AdmissionAuthoring =>
            admissionAuthoring;

        public LocalPlayerProvisioningConsumerAccessBinding ActivityConsumerBinding =>
            activityConsumerBinding;

        public void EditorConfigure(
            SceneLocalPlayerAdmissionAuthoring admission,
            LocalPlayerProvisioningConsumerAccessBinding binding)
        {
            admissionAuthoring = admission;
            activityConsumerBinding = binding;
        }
    }

    /// <summary>
    /// QA-only Hub witness for a Route-scoped public provisioning observation.
    /// It is not an Activity representation, so it remains available while a
    /// SceneProvided Player has no contextual admission.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class P3M5BSessionProvisioningWitness : MonoBehaviour
    {
        public const string RootObjectName = "QA_P3M5B_SessionProvisioningWitness";

        [SerializeField]
        private LocalPlayerProvisioningConsumerAccessBinding routeConsumerBinding;
        [SerializeField]
        private RouteRequestTrigger enterRouteATrigger;

        public LocalPlayerProvisioningConsumerAccessBinding RouteConsumerBinding =>
            routeConsumerBinding;
        public RouteRequestTrigger EnterRouteATrigger => enterRouteATrigger;

        public void Configure(
            LocalPlayerProvisioningConsumerAccessBinding binding,
            RouteRequestTrigger routeATrigger)
        {
            routeConsumerBinding = binding;
            enterRouteATrigger = routeATrigger;
        }

        public bool TryValidate(out string issue)
        {
            if (routeConsumerBinding == null ||
                routeConsumerBinding.Scope != LocalPlayerProvisioningConsumerScope.Route ||
                routeConsumerBinding.gameObject != gameObject ||
                enterRouteATrigger == null ||
                enterRouteATrigger.gameObject != gameObject)
            {
                issue = "P3M5B Session witness requires a Route-scoped binding and Route A trigger on its Hub GameObject.";
                return false;
            }

            issue = string.Empty;
            return true;
        }
    }
}

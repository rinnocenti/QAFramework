using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// QA-only camera target provider for Route/Activity camera smoke fixtures.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/QA Camera Anchor Host")]
    public sealed class QaCameraAnchorHost : MonoBehaviour
    {
        [SerializeField] private Transform trackingTarget;
        [SerializeField] private Transform lookAtTarget;

        public Transform TrackingTarget => trackingTarget;

        public Transform LookAtTarget => lookAtTarget;

        public bool HasAnyTarget => trackingTarget != null || lookAtTarget != null;
    }
}

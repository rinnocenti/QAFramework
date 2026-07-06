using UnityEngine;

namespace Project._Project.Scripts.Runtime.GameCamera
{
    /// <summary>
    /// FIRSTGAME scene-authored camera target provider.
    /// Route and Activity camera bindings can reuse the same host or provide their own targets.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstGameCameraAnchorHost : MonoBehaviour
    {
        [SerializeField] private Transform trackingTarget;
        [SerializeField] private Transform lookAtTarget;

        public Transform TrackingTarget => trackingTarget;

        public Transform LookAtTarget => lookAtTarget;

        public bool HasAnyTarget => trackingTarget != null || lookAtTarget != null;
    }
}

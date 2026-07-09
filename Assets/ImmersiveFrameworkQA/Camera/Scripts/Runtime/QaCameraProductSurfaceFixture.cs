using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerAuthoring;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// Scene-authored references for the clean Camera Product Surface QA.
    /// This is a harness contract; it does not resolve product objects by name or path.
    /// </summary>
    public sealed class QaCameraProductSurfaceFixture : MonoBehaviour
    {
        [SerializeField] private PlayerComposer playerComposer;
        [SerializeField] private CameraComposer cameraComposer;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private GameObject cameraRig;
        [SerializeField] private CameraComposer negativeMissingPlayerComposer;

        public PlayerComposer PlayerComposer => playerComposer;
        public CameraComposer CameraComposer => cameraComposer;
        public Transform CameraTarget => cameraTarget;
        public Transform LookAtTarget => lookAtTarget;
        public GameObject CameraRig => cameraRig;
        public CameraComposer NegativeMissingPlayerComposer => negativeMissingPlayerComposer;
    }
}

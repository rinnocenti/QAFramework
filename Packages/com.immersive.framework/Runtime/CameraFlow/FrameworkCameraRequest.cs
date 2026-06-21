using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.CameraFlow
{
    /// <summary>
    /// Semantic camera request produced by a local camera adapter.
    /// The request does not own the output Unity Camera; it identifies a virtual camera candidate.
    /// </summary>
    public sealed class FrameworkCameraRequest
    {
        internal FrameworkCameraRequest(
            MonoBehaviour owner,
            IFrameworkCameraRequestDriver driver,
            CinemachineCamera cinemachineCamera,
            FrameworkCameraScope scope,
            int priorityOffset,
            int sequence,
            string source,
            string reason)
        {
            Owner = owner;
            Driver = driver;
            CinemachineCamera = cinemachineCamera;
            Scope = scope;
            PriorityOffset = priorityOffset;
            Sequence = sequence;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public MonoBehaviour Owner { get; }

        public CinemachineCamera CinemachineCamera { get; }

        public FrameworkCameraScope Scope { get; }

        public int PriorityOffset { get; }

        public int Sequence { get; }

        public string Source { get; }

        public string Reason { get; }

        public int EffectivePriority => (int)Scope + PriorityOffset;

        public string CameraName => CinemachineCamera != null ? CinemachineCamera.name : "<missing>";

        internal IFrameworkCameraRequestDriver Driver { get; }
    }
}

using Immersive.Foundation.Events;

namespace Immersive.Framework.CameraFlow
{
    /// <summary>
    /// Foundation event emitted when CameraFlow loses its active semantic camera request.
    /// </summary>
    public sealed class FrameworkCameraDeactivatedEvent : IEvent
    {
        public FrameworkCameraDeactivatedEvent(
            FrameworkCameraRequest deactivatedRequest,
            string reason)
        {
            DeactivatedRequest = deactivatedRequest;
            Reason = reason ?? string.Empty;
        }

        public FrameworkCameraRequest DeactivatedRequest { get; }

        public string Reason { get; }

        public string DeactivatedCameraName => DeactivatedRequest != null ? DeactivatedRequest.CameraName : "<none>";
    }
}

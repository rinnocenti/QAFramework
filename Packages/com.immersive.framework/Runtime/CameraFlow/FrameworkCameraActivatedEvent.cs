using Immersive.Foundation.Events;

namespace Immersive.Framework.CameraFlow
{
    /// <summary>
    /// Foundation event emitted when CameraFlow resolves a new active semantic camera request.
    /// </summary>
    public sealed class FrameworkCameraActivatedEvent : IEvent
    {
        public FrameworkCameraActivatedEvent(
            FrameworkCameraRequest activeRequest,
            FrameworkCameraRequest previousRequest,
            string reason)
        {
            ActiveRequest = activeRequest;
            PreviousRequest = previousRequest;
            Reason = reason ?? string.Empty;
        }

        public FrameworkCameraRequest ActiveRequest { get; }

        public FrameworkCameraRequest PreviousRequest { get; }

        public string Reason { get; }

        public string ActiveCameraName => ActiveRequest != null ? ActiveRequest.CameraName : "<none>";

        public string PreviousCameraName => PreviousRequest != null ? PreviousRequest.CameraName : "<none>";
    }
}

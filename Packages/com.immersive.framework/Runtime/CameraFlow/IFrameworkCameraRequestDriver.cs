namespace Immersive.Framework.CameraFlow
{
    internal interface IFrameworkCameraRequestDriver
    {
        string DriverName { get; }

        void ApplyCameraAuthorityState(FrameworkCameraRequest request, bool isActive);
    }
}

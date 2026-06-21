namespace Immersive.Framework.CameraFlow
{
    /// <summary>
    /// Semantic camera request scope. Higher values win when multiple camera requests are active.
    /// </summary>
    public enum FrameworkCameraScope
    {
        Auto = -1,
        Default = 0,
        Route = 100,
        Activity = 200,
        Presentation = 300,
        Pause = 400,
        Override = 900
    }
}

using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Policy that describes which scene should remain active after Route scene composition.
    /// F6B records policy only; execution starts in later F6 cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6B inert route scene composition planning vocabulary.")]
    internal enum RouteSceneActiveScenePolicy
    {
        Unknown = 0,
        PrimarySceneActive = 10
    }
}

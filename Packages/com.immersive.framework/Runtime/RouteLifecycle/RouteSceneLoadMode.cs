using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Intended Unity scene load mode recorded by Route scene composition planning.
    /// This does not execute scene loading by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6B inert route scene composition planning vocabulary.")]
    internal enum RouteSceneLoadMode
    {
        Unknown = 0,
        Single = 10,
        Additive = 20
    }
}

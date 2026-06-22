using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Per-scene status recorded by a Route scene composition result entry.
    /// This is evidence vocabulary, not a scene loading primitive.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6C inert route scene composition result vocabulary; additive execution is deferred.")]
    internal enum RouteSceneCompositionEntryStatus
    {
        Unknown = 0,
        NotExecuted = 10,
        Loaded = 20,
        AlreadyLoaded = 30,
        Skipped = 40,
        Failed = 50
    }
}

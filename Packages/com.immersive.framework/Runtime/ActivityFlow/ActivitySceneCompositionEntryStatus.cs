using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Per-scene status recorded by an Activity scene composition result entry.
    /// This is planning evidence only and does not represent a loaded scene handle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25B Activity scene composition entry vocabulary; execution and release are deferred.")]
    internal enum ActivitySceneCompositionEntryStatus
    {
        Unknown = 0,
        Planned = 10,
        NotExecutionReady = 20
    }
}

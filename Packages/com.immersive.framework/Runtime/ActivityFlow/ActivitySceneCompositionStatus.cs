using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Aggregate status for an Activity scene composition result.
    /// F25B records side-effect-free planning evidence only; execution starts in later F25 cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25B Activity scene composition result vocabulary; execution and release are deferred.")]
    internal enum ActivitySceneCompositionStatus
    {
        Unknown = 0,
        NotRequested = 10,
        Planned = 20,
        PlannedWithIssues = 30
    }
}

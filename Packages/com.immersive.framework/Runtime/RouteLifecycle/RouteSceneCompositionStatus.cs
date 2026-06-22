using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Aggregate status for a Route scene composition result.
    /// F6C records result vocabulary only; execution starts in later F6 cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6C inert route scene composition result vocabulary; additive execution is deferred.")]
    internal enum RouteSceneCompositionStatus
    {
        Unknown = 0,
        NotExecuted = 10,
        Succeeded = 20,
        SucceededWithIssues = 30,
        Failed = 40
    }
}

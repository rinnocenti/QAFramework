namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Minimal outcome classification for runtime route requests.
    /// This is intentionally small; richer transition policies are added only when needed.
    /// </summary>
    internal enum FrameworkRouteRequestKind
    {
        Succeeded = 0,
        FailedInvalidConfig = 1,
        IgnoredAlreadyActive = 2,
        IgnoredAlreadyInFlight = 3,
        FailedRuntimeUnavailable = 4
    }
}

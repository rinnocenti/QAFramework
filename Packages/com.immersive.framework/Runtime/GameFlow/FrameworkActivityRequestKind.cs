namespace Immersive.Framework.GameFlow
{
    internal enum FrameworkActivityRequestKind
    {
        Succeeded = 0,
        IgnoredAlreadyActive = 1,
        IgnoredAlreadyInFlight = 2,
        IgnoredNoActiveActivity = 3,
        FailedInvalidConfig = 4,
        FailedRuntimeUnavailable = 5
    }
}

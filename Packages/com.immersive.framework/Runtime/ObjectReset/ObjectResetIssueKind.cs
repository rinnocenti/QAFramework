using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Issue kinds emitted while validating or resolving Object Reset requests.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14B Object Reset issue kinds; no Unity object reset side effects.")]
    public enum ObjectResetIssueKind
    {
        Unknown = 0,
        InvalidRequest = 10,
        InvalidPolicy = 20,
        RuntimeContextUnavailable = 30,
        TargetNotFound = 40,
        TargetScopeMismatch = 50,
        TargetOwnerMissing = 60,
        TargetOwnerMismatch = 70,
        ForeignOrStaleTarget = 80,
        Exception = 90
    }
}

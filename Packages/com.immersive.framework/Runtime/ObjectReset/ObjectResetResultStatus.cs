using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Aggregate status for Object Reset foundation results.
    /// F14B uses only target-resolution statuses; participant execution is introduced in later cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14B Object Reset result statuses; target resolution now, participant execution later.")]
    public enum ObjectResetResultStatus
    {
        Unknown = 0,

        Succeeded = 10,
        SucceededNoParticipants = 20,
        CompletedWithWarnings = 30,

        RejectedInvalidRequest = 100,
        RejectedRuntimeContextUnavailable = 110,
        RejectedTargetNotFound = 120,
        RejectedForeignTarget = 130,

        Failed = 200
    }
}

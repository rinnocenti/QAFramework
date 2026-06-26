using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Severity for Object Reset structured diagnostics.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14B Object Reset issue severity primitive; logical target resolution only.")]
    public enum ObjectResetIssueSeverity
    {
        Unknown = 0,
        Info = 10,
        Warning = 20,
        Error = 30
    }
}

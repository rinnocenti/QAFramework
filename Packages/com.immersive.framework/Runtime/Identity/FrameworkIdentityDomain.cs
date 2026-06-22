using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Identity
{
    /// <summary>
    /// API status: Experimental. Canonical coarse-grained domains for framework identity values.
    /// The zero value is invalid for new functional identities and exists only as the default enum value.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Minimal typed identity domain primitive introduced by F1E.")]
    public enum FrameworkIdentityDomain
    {
        /// <summary>
        /// Invalid default value. Do not use for authored or runtime identity keys.
        /// </summary>
        Unspecified = 0,

        Application = 10,
        Session = 20,
        Route = 30,
        Activity = 40,
        Content = 50,
        Local = 60,
        ContentAnchor = 70,
        Runtime = 80,
        Diagnostics = 90,
        QA = 100
    }
}

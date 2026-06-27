using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Authoring
{
    /// <summary>
    /// API status: Experimental. Declares release intent for Activity-owned content once execution exists.
    /// F25A only defines the authoring contract; release execution is deferred.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Activity content release policies are declaration-only in F25A; release execution is deferred.")]
    public enum ActivityContentReleasePolicy
    {
        ReleaseOnActivityExit = 0,
        KeepUntilRouteExit = 1
    }
}

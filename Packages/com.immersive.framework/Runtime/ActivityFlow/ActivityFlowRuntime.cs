using System.Threading.Tasks;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal owner for Activity entry.
    /// It currently starts only the optional Startup Activity declared by a Route.
    /// </summary>
    internal sealed class ActivityFlowRuntime
    {
        private ActivityAsset currentActivity;

        internal ActivityAsset CurrentActivity => currentActivity;

        internal Task<ActivityFlowStartResult> StartStartupActivityAsync(RouteAsset route)
        {
            if (route == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Route is missing."));
            }

            if (!route.HasStartupActivity)
            {
                currentActivity = null;
                return Task.FromResult(ActivityFlowStartResult.SkippedNoStartupActivity());
            }

            currentActivity = route.StartupActivity;
            return Task.FromResult(ActivityFlowStartResult.StartedWith(currentActivity));
        }
    }
}

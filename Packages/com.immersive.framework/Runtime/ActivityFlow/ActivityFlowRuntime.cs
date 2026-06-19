using System.Threading.Tasks;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal owner for Activity entry.
    /// It owns the active Activity identity for the current application runtime.
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

            var previousActivity = currentActivity;
            if (!route.HasStartupActivity)
            {
                currentActivity = null;
                return Task.FromResult(ActivityFlowStartResult.SkippedNoStartupActivity(previousActivity));
            }

            var nextActivity = route.StartupActivity;
            if (ReferenceEquals(previousActivity, nextActivity))
            {
                return Task.FromResult(ActivityFlowStartResult.KeptCurrentActivity(nextActivity));
            }

            currentActivity = nextActivity;
            return Task.FromResult(ActivityFlowStartResult.StartedWith(nextActivity, previousActivity));
        }
    }
}

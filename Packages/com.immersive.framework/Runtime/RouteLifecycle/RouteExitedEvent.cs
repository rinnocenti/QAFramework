using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal sealed class RouteExitedEvent : IEvent
    {
        public RouteExitedEvent(RouteAsset route, RouteAsset nextRoute, string source, string reason)
        {
            Route = route;
            NextRoute = nextRoute;
            Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }

        public RouteAsset Route { get; }

        public RouteAsset NextRoute { get; }

        public string Source { get; }

        public string Reason { get; }
    }
}

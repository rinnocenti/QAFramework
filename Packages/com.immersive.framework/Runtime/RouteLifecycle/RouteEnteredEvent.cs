using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal sealed class RouteEnteredEvent : IEvent
    {
        public RouteEnteredEvent(RouteAsset route, RouteAsset previousRoute, string source, string reason)
        {
            Route = route;
            PreviousRoute = previousRoute;
            Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }

        public RouteAsset Route { get; }

        public RouteAsset PreviousRoute { get; }

        public string Source { get; }

        public string Reason { get; }
    }
}

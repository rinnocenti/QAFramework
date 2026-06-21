using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Route-owned content handles materialized for the active route.
    /// This baseline records the loaded primary scene as route content; additional scenes/prefabs come later.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct RouteContentSet
    {
        public RouteContentSet(
            RouteAsset route,
            FrameworkContentSet contentSet,
            RouteContentMaterializationPlan contentPlan)
        {
            Route = route;
            ContentSet = contentSet;
            ContentPlan = contentPlan;
        }

        public RouteAsset Route { get; }

        public FrameworkContentSet ContentSet { get; }

        public RouteContentMaterializationPlan ContentPlan { get; }

        public bool HasContent => ContentSet.HasContent;

        public int Count => ContentSet.Count;

        public string DiagnosticMessage
        {
            get
            {
                if (!HasContent)
                {
                    return Route != null
                        ? $"Route Content Set is empty for Route '{Route.RouteName}'."
                        : "Route Content Set is empty.";
                }

                var planMessage = ContentPlan.HasProfile ? $" {ContentPlan.ToDiagnosticString()}" : string.Empty;
                return $"Route Content Set registered {Count} handle(s). {ContentSet.ToDiagnosticString()}.{planMessage}";
            }
        }

        public static RouteContentSet Empty(RouteAsset route)
        {
            var ownerName = route != null ? route.RouteName : string.Empty;
            var ownerId = CreateRouteOwnerId(route);
            return new RouteContentSet(
                route,
                FrameworkContentSet.Empty(FrameworkContentScope.Route, ownerId, ownerName),
                RouteContentMaterializationPlan.FromRoute(route));
        }

        public static RouteContentSet FromPrimaryScene(
            RouteAsset route,
            RouteContentMaterializationPlan contentPlan,
            SceneLifecycleLoadResult sceneLifecycleResult,
            string source,
            string reason)
        {
            if (route == null || !sceneLifecycleResult.Loaded)
            {
                return Empty(route);
            }

            var ownerId = CreateRouteOwnerId(route);
            var handle = FrameworkContentHandle.RoutePrimaryScene(
                ownerId,
                route.RouteName,
                sceneLifecycleResult.SceneName,
                sceneLifecycleResult.ScenePath,
                true,
                source,
                reason,
                sceneLifecycleResult.Message);

            return new RouteContentSet(
                route,
                FrameworkContentSet.Single(FrameworkContentScope.Route, ownerId, route.RouteName, handle),
                contentPlan);
        }

        private static string CreateRouteOwnerId(RouteAsset route)
        {
            if (route == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(route.PrimaryScenePath))
            {
                return route.PrimaryScenePath.Trim();
            }

            return route.RouteName;
        }
    }
}

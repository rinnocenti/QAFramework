using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25H explicit Activity scene ledger entry.")]
    internal readonly struct ActivitySceneLedgerEntry
    {
        internal ActivitySceneLedgerEntry(
            string routeInstanceId,
            RouteAsset route,
            ActivityAsset activity,
            ActivitySceneCompositionPlanEntry planEntry,
            ActivitySceneLedgerOwnership ownership,
            ActivitySceneLedgerEntryStatus status)
        {
            RouteInstanceId = Normalize(routeInstanceId);
            Route = route;
            Activity = activity;
            PlanEntry = planEntry;
            Ownership = ownership;
            Status = status;
        }

        internal string RouteInstanceId { get; }

        internal RouteAsset Route { get; }

        internal ActivityAsset Activity { get; }

        internal ActivitySceneCompositionPlanEntry PlanEntry { get; }

        internal ActivitySceneLedgerOwnership Ownership { get; }

        internal ActivitySceneLedgerEntryStatus Status { get; }

        internal bool IsLoaded => Status == ActivitySceneLedgerEntryStatus.Loaded;

        internal bool IsReleased => Status == ActivitySceneLedgerEntryStatus.Released;

        internal bool IsStale => Status == ActivitySceneLedgerEntryStatus.Stale;

        internal string RouteName => Route != null && !string.IsNullOrWhiteSpace(Route.RouteName)
            ? Route.RouteName
            : string.Empty;

        internal string ActivityId => Activity != null && !string.IsNullOrWhiteSpace(Activity.ActivityName)
            ? Activity.ActivityName
            : string.Empty;

        internal string ContentId => PlanEntry.ContentId;

        internal string ContentIdentity => PlanEntry.ContentIdentity.IsValid
            ? PlanEntry.ContentIdentity.StableText
            : string.Empty;

        internal string SceneName => PlanEntry.SceneName;

        internal string ScenePath => PlanEntry.ScenePath;

        internal ActivityContentReleasePolicy ReleasePolicy => PlanEntry.ReleasePolicy;

        internal ActivitySceneLedgerEntry WithStatus(ActivitySceneLedgerEntryStatus status)
        {
            return new ActivitySceneLedgerEntry(
                RouteInstanceId,
                Route,
                Activity,
                PlanEntry,
                Ownership,
                status);
        }

        internal string ToDiagnosticString()
        {
            var routeInstanceId = !string.IsNullOrWhiteSpace(RouteInstanceId) ? RouteInstanceId : "<missing>";
            var route = !string.IsNullOrWhiteSpace(RouteName) ? RouteName : "<none>";
            var activity = !string.IsNullOrWhiteSpace(ActivityId) ? ActivityId : "<none>";
            var identity = !string.IsNullOrWhiteSpace(ContentIdentity) ? ContentIdentity : "<missing>";
            var contentId = !string.IsNullOrWhiteSpace(ContentId) ? ContentId : "<missing>";
            var scene = !string.IsNullOrWhiteSpace(SceneName) ? SceneName : ScenePath;
            if (string.IsNullOrWhiteSpace(scene))
            {
                scene = "<missing>";
            }

            return $"routeInstance='{routeInstanceId}' route='{route}' activity='{activity}' ownership='{Ownership}' status='{Status}' identity='{identity}' id='{contentId}' scene='{scene}' releasePolicy='{ReleasePolicy}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

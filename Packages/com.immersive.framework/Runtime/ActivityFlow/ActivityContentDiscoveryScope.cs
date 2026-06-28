using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26A Activity content discovery scope for Route primary and Activity-owned loaded scenes.")]
    internal readonly struct ActivityContentDiscoveryScope
    {
        private readonly ActivityContentDiscoveryScene[] _activityOwnedScenes;

        internal ActivityContentDiscoveryScope(
            RouteAsset route,
            string routeInstanceId,
            IReadOnlyList<ActivityContentDiscoveryScene> activityOwnedScenes)
        {
            Route = route;
            RouteInstanceId = Normalize(routeInstanceId);
            this._activityOwnedScenes = CopyScenes(activityOwnedScenes);
        }

        internal RouteAsset Route { get; }

        internal string RouteInstanceId { get; }

        internal IReadOnlyList<ActivityContentDiscoveryScene> ActivityOwnedScenes => _activityOwnedScenes ?? Array.Empty<ActivityContentDiscoveryScene>();

        internal int ActivityOwnedSceneCount => _activityOwnedScenes != null ? _activityOwnedScenes.Length : 0;

        internal static ActivityContentDiscoveryScope Empty(RouteAsset route, string routeInstanceId)
        {
            return new ActivityContentDiscoveryScope(route, routeInstanceId, Array.Empty<ActivityContentDiscoveryScene>());
        }

        private static ActivityContentDiscoveryScene[] CopyScenes(IReadOnlyList<ActivityContentDiscoveryScene> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<ActivityContentDiscoveryScene>();
            }

            var copy = new ActivityContentDiscoveryScene[scenes.Count];
            for (var i = 0; i < scenes.Count; i++)
            {
                copy[i] = scenes[i];
            }

            return copy;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

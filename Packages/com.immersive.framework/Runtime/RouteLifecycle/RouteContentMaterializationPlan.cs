using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Route content plan derived from Route authoring data.
    /// In this baseline it records additional scene declarations without executing additive loading.
    /// </summary>
    internal readonly struct RouteContentMaterializationPlan
    {
        private readonly RouteContentScenePlanEntry[] _additionalScenes;

        public RouteContentMaterializationPlan(
            RouteAsset route,
            RouteContentProfileAsset profile,
            IReadOnlyList<RouteContentScenePlanEntry> additionalScenes)
        {
            Route = route;
            Profile = profile;

            if (additionalScenes == null || additionalScenes.Count == 0)
            {
                _additionalScenes = Array.Empty<RouteContentScenePlanEntry>();
            }
            else
            {
                _additionalScenes = new RouteContentScenePlanEntry[additionalScenes.Count];
                for (var i = 0; i < additionalScenes.Count; i++)
                {
                    _additionalScenes[i] = additionalScenes[i];
                }
            }
        }

        public RouteAsset Route { get; }

        public RouteContentProfileAsset Profile { get; }

        public bool HasProfile => Profile != null;

        public string ProfileId => Profile != null ? Profile.ProfileId : string.Empty;

        public IReadOnlyList<RouteContentScenePlanEntry> AdditionalScenes => _additionalScenes ?? Array.Empty<RouteContentScenePlanEntry>();

        public int AdditionalSceneCount => _additionalScenes != null ? _additionalScenes.Length : 0;

        public bool HasAdditionalScenes => AdditionalSceneCount > 0;

        public static RouteContentMaterializationPlan Empty(RouteAsset route)
        {
            return new RouteContentMaterializationPlan(route, null, Array.Empty<RouteContentScenePlanEntry>());
        }

        public static RouteContentMaterializationPlan FromRoute(RouteAsset route)
        {
            if (route == null || !route.HasRouteContentProfile)
            {
                return Empty(route);
            }

            var profile = route.RouteContentProfile;
            var entries = profile.AdditionalScenes;
            if (entries == null || entries.Count == 0)
            {
                return new RouteContentMaterializationPlan(route, profile, Array.Empty<RouteContentScenePlanEntry>());
            }

            var plannedEntries = new List<RouteContentScenePlanEntry>(entries.Count);
            for (var i = 0; i < entries.Count; i++)
            {
                var planEntry = RouteContentScenePlanEntry.FromEntry(entries[i], i);
                if (planEntry.HasScene)
                {
                    plannedEntries.Add(planEntry);
                }
            }

            return new RouteContentMaterializationPlan(route, profile, plannedEntries);
        }

        public string ToDiagnosticString()
        {
            if (!HasProfile)
            {
                return "Route Content Plan has no profile.";
            }

            if (!HasAdditionalScenes)
            {
                return $"Route Content Plan profile='{ProfileId}' additionalScenes='0'.";
            }

            var builder = new StringBuilder();
            builder.Append($"Route Content Plan profile='{ProfileId}' additionalScenes='{AdditionalSceneCount}' plannedOnly='True' details=[");
            for (var i = 0; i < _additionalScenes.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(_additionalScenes[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }
    }
}

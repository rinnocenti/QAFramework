using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Route-owned content snapshot for the active Route scope.
    /// This baseline records the loaded primary scene with explicit ownership semantics; additional scenes/prefabs come later.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct RouteContentSet
    {
        private readonly RouteContentEntry[] entries;

        public RouteContentSet(
            RouteAsset route,
            FrameworkContentSet contentSet,
            RouteContentMaterializationPlan contentPlan,
            IReadOnlyList<RouteContentEntry> entries)
        {
            Route = route;
            ContentSet = contentSet;
            ContentPlan = contentPlan;

            if (entries == null || entries.Count == 0)
            {
                this.entries = Array.Empty<RouteContentEntry>();
                return;
            }

            this.entries = new RouteContentEntry[entries.Count];
            for (var i = 0; i < entries.Count; i++)
            {
                this.entries[i] = entries[i];
            }
        }

        public RouteAsset Route { get; }

        public FrameworkContentSet ContentSet { get; }

        public RouteContentMaterializationPlan ContentPlan { get; }

        public IReadOnlyList<RouteContentEntry> Entries => entries ?? Array.Empty<RouteContentEntry>();

        public bool HasContent => ContentSet.HasContent;

        public int Count => ContentSet.Count;

        public int RegisteredCount => CountOwnership(RouteContentOwnership.Registered);

        public int OwnedCount => CountOwnership(RouteContentOwnership.Owned);

        public int DiagnosticOnlyCount => CountOwnership(RouteContentOwnership.DiagnosticOnly);

        public bool HasOwnedContent => OwnedCount > 0;

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
                return $"Route Content Set registered {Count} handle(s). registered='{RegisteredCount}' owned='{OwnedCount}' diagnosticOnly='{DiagnosticOnlyCount}' {ContentSet.ToDiagnosticString()}.{planMessage}";
            }
        }

        public string OwnershipDiagnosticMessage
        {
            get
            {
                if (!HasContent)
                {
                    return "Route Content Set ownership is empty.";
                }

                var builder = new StringBuilder();
                builder.Append($"Route Content Set ownership registered='{RegisteredCount}' owned='{OwnedCount}' diagnosticOnly='{DiagnosticOnlyCount}' details=[");
                for (var i = 0; i < Entries.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(" | ");
                    }

                    builder.Append(Entries[i].ToDiagnosticString());
                }

                builder.Append("]");
                return builder.ToString();
            }
        }

        public static RouteContentSet Empty(RouteAsset route)
        {
            var ownerName = route != null ? route.RouteName : string.Empty;
            var ownerId = CreateRouteOwnerId(route);
            return new RouteContentSet(
                route,
                FrameworkContentSet.Empty(FrameworkContentScope.Route, ownerId, ownerName),
                RouteContentMaterializationPlan.FromRoute(route),
                Array.Empty<RouteContentEntry>());
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
            var entry = new RouteContentEntry(handle, RouteContentOwnership.Owned);

            return new RouteContentSet(
                route,
                FrameworkContentSet.Single(FrameworkContentScope.Route, ownerId, route.RouteName, handle),
                contentPlan,
                new[] { entry });
        }

        public static RouteContentSet FromSceneCompositionResult(
            RouteAsset route,
            RouteContentMaterializationPlan contentPlan,
            RouteSceneCompositionResult compositionResult,
            string source,
            string reason)
        {
            if (route == null || !compositionResult.Succeeded || compositionResult.LoadedCount == 0)
            {
                return Empty(route);
            }

            var ownerId = CreateRouteOwnerId(route);
            var handles = new List<FrameworkContentHandle>(compositionResult.LoadedCount);
            var entries = new List<RouteContentEntry>(compositionResult.LoadedCount);

            for (var i = 0; i < compositionResult.Entries.Count; i++)
            {
                var resultEntry = compositionResult.Entries[i];
                if (!resultEntry.Loaded || !resultEntry.ContentIdentity.IsValid)
                {
                    continue;
                }

                var handle = new FrameworkContentHandle(
                    resultEntry.ContentIdentity,
                    resultEntry.Requiredness,
                    route.RouteName,
                    resultEntry.SceneName,
                    resultEntry.ScenePath,
                    resultEntry.ActiveScene,
                    source,
                    reason,
                    resultEntry.Message);

                handles.Add(handle);
                entries.Add(new RouteContentEntry(handle, resultEntry.Ownership));
            }

            if (handles.Count == 0)
            {
                return Empty(route);
            }

            return new RouteContentSet(
                route,
                new FrameworkContentSet(FrameworkContentScope.Route, ownerId, route.RouteName, handles),
                contentPlan,
                entries);
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

        private int CountOwnership(RouteContentOwnership ownership)
        {
            if (entries == null || entries.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Ownership == ownership)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

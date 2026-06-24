using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Internal. Discovers scene-authored Route Content Anchors from already-loaded Route scenes.
    /// This is diagnostic discovery only; it does not validate required anchors, expose a registry or bind runtime content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Diagnostic Route Content Anchor discovery introduced by F7F.")]
    internal sealed class ContentAnchorDiscoveryRuntime
    {
        internal ContentAnchorDiscoveryResult DiscoverRouteAnchors(
            RouteAsset route,
            RouteSceneCompositionResult sceneCompositionResult,
            string source,
            string reason)
        {
            if (route == null)
            {
                return ContentAnchorDiscoveryResult.Empty(
                    route,
                    source,
                    reason,
                    "No Route is active; Content Anchor discovery was skipped.");
            }

            if (!sceneCompositionResult.Succeeded || sceneCompositionResult.LoadedCount == 0)
            {
                return ContentAnchorDiscoveryResult.Empty(
                    route,
                    source,
                    reason,
                    "Route scene composition did not provide loaded scenes; Content Anchor discovery was skipped.");
            }

            var declarations = new List<ContentAnchorDeclaration>();
            var scannedSceneKeys = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            var scannedSceneCount = 0;
            var candidateCount = 0;
            var skippedRouteMismatchCount = 0;
            var invalidAuthoringCount = 0;

            for (var i = 0; i < sceneCompositionResult.Entries.Count; i++)
            {
                var entry = sceneCompositionResult.Entries[i];
                if (!entry.Loaded)
                {
                    continue;
                }

                var sceneKey = CreateSceneKey(entry.ScenePath, entry.SceneName);
                if (!scannedSceneKeys.Add(sceneKey))
                {
                    continue;
                }

                scannedSceneCount++;
                var anchors = SceneScopedComponentQuery.GetComponentsInLoadedScene<RouteContentAnchor>(
                    entry.ScenePath,
                    entry.SceneName);
                if (anchors == null || anchors.Count == 0)
                {
                    continue;
                }

                for (var anchorIndex = 0; anchorIndex < anchors.Count; anchorIndex++)
                {
                    var anchor = anchors[anchorIndex];
                    if (anchor == null)
                    {
                        continue;
                    }

                    candidateCount++;
                    if (!anchor.MatchesRoute(route))
                    {
                        skippedRouteMismatchCount++;
                        continue;
                    }

                    if (!anchor.TryCreateDeclaration(out var declaration))
                    {
                        invalidAuthoringCount++;
                        continue;
                    }

                    declarations.Add(declaration);
                }
            }

            var anchorSet = ContentAnchorSet.FromDeclarations(declarations);
            return new ContentAnchorDiscoveryResult(
                route,
                anchorSet,
                scannedSceneCount,
                candidateCount,
                declarations.Count,
                skippedRouteMismatchCount,
                invalidAuthoringCount,
                source,
                reason,
                "Route Content Anchor discovery completed for loaded Route scenes.");
        }



        internal ActivityContentAnchorDiscoveryResult DiscoverActivityAnchors(
            ActivityAsset activity,
            RouteAsset route,
            string source,
            string reason)
        {
            if (activity == null)
            {
                return ActivityContentAnchorDiscoveryResult.Empty(
                    activity,
                    source,
                    reason,
                    "No Activity is active; Activity Content Anchor discovery was skipped.");
            }

            if (route == null)
            {
                return ActivityContentAnchorDiscoveryResult.Empty(
                    activity,
                    source,
                    reason,
                    "No Route is active; Activity Content Anchor discovery was skipped.");
            }

            var anchors = SceneScopedComponentQuery.GetComponentsInRoutePrimaryScene<ActivityContentAnchor>(route);
            if (anchors == null || anchors.Count == 0)
            {
                return new ActivityContentAnchorDiscoveryResult(
                    activity,
                    ContentAnchorSet.Empty(),
                    0,
                    0,
                    0,
                    0,
                    0,
                    source,
                    reason,
                    "No Activity Content Anchor components were found in the active Route primary scene.");
            }

            var declarations = new List<ContentAnchorDeclaration>();
            var scannedSceneCount = 1;
            var candidateCount = 0;
            var skippedActivityMismatchCount = 0;
            var invalidAuthoringCount = 0;

            for (var anchorIndex = 0; anchorIndex < anchors.Count; anchorIndex++)
            {
                var anchor = anchors[anchorIndex];
                if (anchor == null)
                {
                    continue;
                }

                candidateCount++;
                if (!anchor.MatchesActivity(activity))
                {
                    skippedActivityMismatchCount++;
                    continue;
                }

                if (!anchor.TryCreateDeclaration(out var declaration))
                {
                    invalidAuthoringCount++;
                    continue;
                }

                declarations.Add(declaration);
            }

            var anchorSet = ContentAnchorSet.FromDeclarations(declarations);
            return new ActivityContentAnchorDiscoveryResult(
                activity,
                anchorSet,
                scannedSceneCount,
                candidateCount,
                declarations.Count,
                skippedActivityMismatchCount,
                invalidAuthoringCount,
                source,
                reason,
                "Activity Content Anchor discovery completed for the active Activity in the Route primary scene.");
        }

        private static string CreateSceneKey(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return scenePath.Trim();
            }

            return string.IsNullOrWhiteSpace(sceneName) ? "<none>" : sceneName.Trim();
        }
    }
}

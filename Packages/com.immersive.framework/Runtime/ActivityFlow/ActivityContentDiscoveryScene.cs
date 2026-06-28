using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26A loaded Activity-owned scene reference for Activity content discovery.")]
    internal readonly struct ActivityContentDiscoveryScene
    {
        internal ActivityContentDiscoveryScene(
            ActivityAsset activity,
            string routeInstanceId,
            string scenePath,
            string sceneName)
        {
            Activity = activity;
            RouteInstanceId = Normalize(routeInstanceId);
            ScenePath = Normalize(scenePath);
            SceneName = Normalize(sceneName);
        }

        internal ActivityAsset Activity { get; }

        internal string RouteInstanceId { get; }

        internal string ScenePath { get; }

        internal string SceneName { get; }

        internal bool MatchesActivity(ActivityAsset activity)
        {
            return ReferenceEquals(Activity, activity);
        }

        internal string SceneKey => !string.IsNullOrWhiteSpace(ScenePath)
            ? ScenePath
            : !string.IsNullOrWhiteSpace(SceneName) ? SceneName : string.Empty;

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

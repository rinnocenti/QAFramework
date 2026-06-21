using Immersive.Framework.Authoring;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Scene-authored boundary for Route-scoped content.
    /// Route Lifecycle uses this component to notify local receivers; it does not own GameObject visibility.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Route Content Binding")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Route local content callbacks are active in the F3 Route baseline and may still change before stabilization.")]
    public sealed class RouteContentBinding : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Route asset that owns this scene. Use the Route whose Primary Scene is this scene.")]
        private RouteAsset route;

        public RouteAsset Route => route;

        internal bool IsSceneBinding => gameObject.scene.IsValid() && gameObject.scene.isLoaded;

        internal string ObjectName => gameObject != null ? gameObject.name : "<missing>";

        internal string SceneName => gameObject != null && gameObject.scene.IsValid()
            ? gameObject.scene.name
            : "<no-scene>";

        internal bool MatchesRoute(RouteAsset candidateRoute)
        {
            if (candidateRoute == null || !IsSceneBinding)
            {
                return false;
            }

            if (route != null)
            {
                return ReferenceEquals(route, candidateRoute);
            }

            var scene = gameObject.scene;
            if (!string.IsNullOrWhiteSpace(candidateRoute.PrimaryScenePath)
                && string.Equals(scene.path, candidateRoute.PrimaryScenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(candidateRoute.PrimarySceneName)
                && string.Equals(scene.name, candidateRoute.PrimarySceneName, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}

using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Simple scene-authored route request boundary.
    /// Designed for UnityEvents/UI Buttons without exposing pipeline/stage concepts.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Route Request Trigger")]
    public sealed class RouteRequestTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(RouteRequestTrigger);

        private FrameworkLogger logger;

        [Header("Route")]
        [SerializeField] private RouteAsset targetRoute;

        [Header("Request")]
        [SerializeField] private string reason;

        private bool requestInFlight;

        public RouteAsset TargetRoute
        {
            get => targetRoute;
            set => targetRoute = value;
        }

        private void Awake()
        {
            logger = FrameworkLogger.Create();
        }

        public async void RequestRoute()
        {
            if (logger == null)
            {
                logger = FrameworkLogger.Create();
            }

            if (requestInFlight)
            {
                logger.Warning("Route Request ignored. This Route Request Trigger already has a request in flight.");
                return;
            }

            if (targetRoute == null)
            {
                logger.Error("Route Request failed. Target Route is missing.");
                return;
            }

            var resolvedReason = string.IsNullOrWhiteSpace(reason) ? targetRoute.RouteName : reason.Trim();
            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                var unavailable = FrameworkRouteRequestResult.FailedRuntimeUnavailable(
                    "Route Request failed. Application Runtime is unavailable.",
                    targetRoute,
                    DefaultSource,
                    resolvedReason);
                logger.Error(unavailable.Message);
                return;
            }

            requestInFlight = true;
            try
            {
                await runtimeHost.RequestRouteAsync(targetRoute, DefaultSource, resolvedReason);
            }
            finally
            {
                requestInFlight = false;
            }
        }
    }
}

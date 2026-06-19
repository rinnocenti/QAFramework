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

        private FrameworkLogger _logger;

        [Header("Route")]
        [SerializeField] private RouteAsset targetRoute;

        [Header("Request")]
        [SerializeField] private string reason;

        private bool _requestInFlight;

        public RouteAsset TargetRoute
        {
            get => targetRoute;
            set => targetRoute = value;
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create();
        }

        public async void RequestRoute()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create();
            }

            if (_requestInFlight)
            {
                _logger.Warning("Route Request ignored. This Route Request Trigger already has a request in flight.");
                return;
            }

            if (targetRoute == null)
            {
                _logger.Error("Route Request failed. Target Route is missing.");
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
                _logger.Error(unavailable.Message);
                return;
            }

            _requestInFlight = true;
            try
            {
                await runtimeHost.RequestRouteAsync(targetRoute, DefaultSource, resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }
        }
    }
}

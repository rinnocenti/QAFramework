using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Simple scene-authored route request boundary.
    /// Designed for UnityEvents/UI Buttons to invoke requests, while publishing typed Foundation events for results.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Route Request Trigger")]
    public sealed class RouteRequestTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(RouteRequestTrigger);

        private readonly EventBus<RouteRequestTriggerEvent> _requestEvents = new EventBus<RouteRequestTriggerEvent>();
        private FrameworkLogger _logger;
        private bool _requestInFlight;

        [Header("Route")]
        [SerializeField] private RouteAsset targetRoute;

        [Header("Request")]
        [SerializeField] private string reason;

        public RouteAsset TargetRoute
        {
            get => targetRoute;
            set => targetRoute = value;
        }

        public IEventBinding SubscribeRequestEvents(Action<RouteRequestTriggerEvent> handler)
        {
            return _requestEvents.Subscribe(handler);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create();
        }

        public async void RequestRoute()
        {
            EnsureLogger();

            var resolvedReason = ResolveReason();

            if (_requestInFlight)
            {
                var message = "Route Request ignored. This Route Request Trigger already has a request in flight.";
                _logger.Warning(message);
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message);
                return;
            }

            if (targetRoute == null)
            {
                var message = "Route Request failed. Target Route is missing.";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message);
                return;
            }

            resolvedReason = ResolveReason();
            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                var unavailable = FrameworkRouteRequestResult.FailedRuntimeUnavailable(
                    "Route Request failed. Application Runtime is unavailable.",
                    targetRoute,
                    DefaultSource,
                    resolvedReason);
                _logger.Error(unavailable.Message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, unavailable.Message);
                return;
            }

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);

            FrameworkRouteRequestResult result;
            try
            {
                result = await runtimeHost.RequestRouteAsync(targetRoute, DefaultSource, resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }

            PublishCompleted(MapOutcome(result.Kind), resolvedReason, result.Message);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create();
            }
        }

        private string ResolveReason()
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                return reason.Trim();
            }

            return targetRoute != null ? targetRoute.RouteName : "Route Request";
        }

        private void PublishSubmitted(string resolvedReason)
        {
            _requestEvents.Publish(new RouteRequestTriggerEvent(
                this,
                targetRoute,
                FlowRequestEventPhase.Submitted,
                FlowRequestOutcome.Submitted,
                DefaultSource,
                resolvedReason,
                $"Route Request submitted. source='{DefaultSource}' reason='{resolvedReason}'."));
        }

        private void PublishCompleted(FlowRequestOutcome outcome, string resolvedReason, string message)
        {
            _requestEvents.Publish(new RouteRequestTriggerEvent(
                this,
                targetRoute,
                FlowRequestEventPhase.Completed,
                outcome,
                DefaultSource,
                resolvedReason,
                message));
        }

        private static FlowRequestOutcome MapOutcome(FrameworkRouteRequestKind resultKind)
        {
            switch (resultKind)
            {
                case FrameworkRouteRequestKind.Succeeded:
                    return FlowRequestOutcome.Succeeded;
                case FrameworkRouteRequestKind.IgnoredAlreadyActive:
                case FrameworkRouteRequestKind.IgnoredAlreadyInFlight:
                    return FlowRequestOutcome.Ignored;
                default:
                    return FlowRequestOutcome.Failed;
            }
        }
    }
}

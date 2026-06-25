using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Scene-authored request boundary for resetting the active Route cycle.
    /// It does not perform object, component, player, actor, pool, save or scene reload reset.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Cycle Reset/Route Cycle Reset Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11F public authored trigger for Route Cycle Reset requests.")]
    public sealed class RouteCycleResetTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(RouteCycleResetTrigger);
        private const string DefaultReason = "Route Cycle Reset";

        private readonly EventBus<CycleResetTriggerEvent> _requestEvents = new EventBus<CycleResetTriggerEvent>();
        private FrameworkLogger _logger;
        private bool _requestInFlight;
        private FlowRequestEventPhase _lastEventPhase = FlowRequestEventPhase.Completed;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private string _lastReason = string.Empty;
        private string _lastMessage = string.Empty;
        private CycleResetResult _lastResult;
        private bool _hasLastResult;

        [Header("Cycle Reset")]
        [SerializeField] private string reason;

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public CycleResetResult LastResult => _lastResult;

        public bool HasLastResult => _hasLastResult;

        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;

        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;

        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;

        public IEventBinding SubscribeRequestEvents(Action<CycleResetTriggerEvent> handler)
        {
            return _requestEvents.Subscribe(handler);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<RouteCycleResetTrigger>();
        }

        public async void RequestRouteCycleReset()
        {
            EnsureLogger();

            var resolvedReason = ResolveReason();

            if (_requestInFlight)
            {
                var message = "Route Cycle Reset ignored. This Route Cycle Reset Trigger already has a request in flight.";
                _logger.Warning(message);
                PublishCompleted(FlowRequestOutcome.Ignored, resolvedReason, message, default, false);
                return;
            }

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                var message = "Route Cycle Reset failed. Application Runtime is unavailable.";
                _logger.Error(message);
                PublishCompleted(FlowRequestOutcome.Failed, resolvedReason, message, default, false);
                return;
            }

            _requestInFlight = true;
            PublishSubmitted(resolvedReason);

            CycleResetResult result;
            try
            {
                result = await runtimeHost.RequestRouteCycleResetAsync(DefaultSource, resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }

            PublishCompleted(MapOutcome(result), resolvedReason, result.Message, result, true);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<RouteCycleResetTrigger>();
            }
        }

        private string ResolveReason()
        {
            return string.IsNullOrWhiteSpace(reason) ? DefaultReason : reason.Trim();
        }

        private void PublishSubmitted(string resolvedReason)
        {
            var message = $"Route Cycle Reset submitted. source='{DefaultSource}' reason='{resolvedReason}'.";
            SetRequestState(FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message, default, false);

            _requestEvents.Publish(new CycleResetTriggerEvent(
                this,
                CycleResetScope.Route,
                FlowRequestEventPhase.Submitted,
                FlowRequestOutcome.Submitted,
                DefaultSource,
                resolvedReason,
                message,
                default,
                false));
        }

        private void PublishCompleted(
            FlowRequestOutcome outcome,
            string resolvedReason,
            string message,
            CycleResetResult result,
            bool hasResult)
        {
            SetRequestState(FlowRequestEventPhase.Completed, outcome, resolvedReason, message, result, hasResult);

            _requestEvents.Publish(new CycleResetTriggerEvent(
                this,
                CycleResetScope.Route,
                FlowRequestEventPhase.Completed,
                outcome,
                DefaultSource,
                resolvedReason,
                message,
                result,
                hasResult));
        }

        private void SetRequestState(
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string resolvedReason,
            string message,
            CycleResetResult result,
            bool hasResult)
        {
            _lastEventPhase = phase;
            _lastOutcome = outcome;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
            _lastResult = result;
            _hasLastResult = hasResult;
        }

        private static FlowRequestOutcome MapOutcome(CycleResetResult result)
        {
            if (result.Succeeded || result.CompletedWithWarnings)
            {
                return FlowRequestOutcome.Succeeded;
            }

            return FlowRequestOutcome.Failed;
        }
    }
}

using System;
using Immersive.Foundation.Events;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// API status: Experimental. Public scene-authored request boundary kept for baseline smoke and development use.
    /// Simple scene-authored activity request boundary.
    /// Designed for UnityEvents/UI Buttons to invoke requests, while publishing typed Foundation events for results.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Request Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    public sealed class ActivityRequestTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(ActivityRequestTrigger);
        private const string DefaultClearReason = "Clear Activity";

        private readonly EventBus<ActivityRequestTriggerEvent> _requestEvents = new EventBus<ActivityRequestTriggerEvent>();
        private FrameworkLogger _logger;
        private bool _requestInFlight;
        private FlowRequestEventPhase _lastEventPhase = FlowRequestEventPhase.Completed;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private string _lastReason = string.Empty;
        private string _lastMessage = string.Empty;
        private bool _lastRequestClearedActivity;

        [Header("Activity")]
        [SerializeField] private ActivityAsset targetActivity;

        [Header("Request")]
        [SerializeField] private string reason;

        public ActivityAsset TargetActivity
        {
            get => targetActivity;
            set => targetActivity = value;
        }

        public bool IsRequestInFlight => _requestInFlight;

        public FlowRequestEventPhase LastEventPhase => _lastEventPhase;

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public bool LastRequestClearedActivity => _lastRequestClearedActivity;

        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;

        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;

        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;

        public IEventBinding SubscribeRequestEvents(Action<ActivityRequestTriggerEvent> handler)
        {
            return _requestEvents.Subscribe(handler);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<ActivityRequestTrigger>();
        }

        public async void RequestActivity()
        {
            EnsureLogger();

            var resolvedReason = ResolveRequestReason();

            if (_requestInFlight)
            {
                var message = "Activity Request ignored. This Activity Request Trigger already has a request in flight.";
                _logger.Warning(message);
                PublishCompleted(false, FlowRequestOutcome.Ignored, resolvedReason, message);
                return;
            }

            if (targetActivity == null)
            {
                var message = "Activity Request failed. Target Activity is missing.";
                _logger.Error(message);
                PublishCompleted(false, FlowRequestOutcome.Failed, resolvedReason, message);
                return;
            }

            resolvedReason = ResolveRequestReason();
            if (!TryGetRuntimeHost(targetActivity, resolvedReason, false, out var runtimeHost))
            {
                return;
            }

            _requestInFlight = true;
            PublishSubmitted(false, resolvedReason);

            FrameworkActivityRequestResult result;
            try
            {
                result = await runtimeHost.RequestActivityAsync(targetActivity, DefaultSource, resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }

            PublishCompleted(false, MapOutcome(result.Kind), resolvedReason, result.Message);
        }

        public async void ClearActivity()
        {
            EnsureLogger();

            var resolvedReason = ResolveClearReason();

            if (_requestInFlight)
            {
                var message = "Activity Request ignored. This Activity Request Trigger already has a request in flight.";
                _logger.Warning(message);
                PublishCompleted(true, FlowRequestOutcome.Ignored, resolvedReason, message);
                return;
            }

            if (!TryGetRuntimeHost(null, resolvedReason, true, out var runtimeHost))
            {
                return;
            }

            _requestInFlight = true;
            PublishSubmitted(true, resolvedReason);

            FrameworkActivityRequestResult result;
            try
            {
                result = await runtimeHost.ClearActivityAsync(DefaultSource, resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }

            PublishCompleted(true, MapOutcome(result.Kind), resolvedReason, result.Message);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<ActivityRequestTrigger>();
            }
        }

        private bool TryGetRuntimeHost(ActivityAsset activity, string resolvedReason, bool clearsActivity, out FrameworkRuntimeHost runtimeHost)
        {
            if (FrameworkRuntimeHost.TryGetCurrent(out runtimeHost))
            {
                return true;
            }

            var unavailable = FrameworkActivityRequestResult.FailedRuntimeUnavailable(
                "Activity Request failed. Application Runtime is unavailable.",
                activity,
                DefaultSource,
                resolvedReason);
            _logger.Error(unavailable.Message);
            PublishCompleted(clearsActivity, FlowRequestOutcome.Failed, resolvedReason, unavailable.Message);
            return false;
        }

        private string ResolveRequestReason()
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                return reason.Trim();
            }

            return targetActivity != null ? targetActivity.ActivityName : "Activity Request";
        }

        private string ResolveClearReason()
        {
            return string.IsNullOrWhiteSpace(reason) ? DefaultClearReason : reason.Trim();
        }

        private void PublishSubmitted(bool clearsActivity, string resolvedReason)
        {
            var message = $"Activity Request submitted. source='{DefaultSource}' reason='{resolvedReason}'.";
            SetRequestState(clearsActivity, FlowRequestEventPhase.Submitted, FlowRequestOutcome.Submitted, resolvedReason, message);

            _requestEvents.Publish(new ActivityRequestTriggerEvent(
                this,
                targetActivity,
                clearsActivity,
                FlowRequestEventPhase.Submitted,
                FlowRequestOutcome.Submitted,
                DefaultSource,
                resolvedReason,
                message));
        }

        private void PublishCompleted(bool clearsActivity, FlowRequestOutcome outcome, string resolvedReason, string message)
        {
            SetRequestState(clearsActivity, FlowRequestEventPhase.Completed, outcome, resolvedReason, message);

            _requestEvents.Publish(new ActivityRequestTriggerEvent(
                this,
                targetActivity,
                clearsActivity,
                FlowRequestEventPhase.Completed,
                outcome,
                DefaultSource,
                resolvedReason,
                message));
        }

        private void SetRequestState(
            bool clearsActivity,
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string resolvedReason,
            string message)
        {
            _lastRequestClearedActivity = clearsActivity;
            _lastEventPhase = phase;
            _lastOutcome = outcome;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
        }

        private static FlowRequestOutcome MapOutcome(FrameworkActivityRequestKind resultKind)
        {
            switch (resultKind)
            {
                case FrameworkActivityRequestKind.Succeeded:
                    return FlowRequestOutcome.Succeeded;
                case FrameworkActivityRequestKind.IgnoredAlreadyActive:
                case FrameworkActivityRequestKind.IgnoredAlreadyInFlight:
                case FrameworkActivityRequestKind.IgnoredNoActiveActivity:
                    return FlowRequestOutcome.Ignored;
                default:
                    return FlowRequestOutcome.Failed;
            }
        }
    }
}

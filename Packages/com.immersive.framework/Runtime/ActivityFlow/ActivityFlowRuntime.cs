using System;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal owner for Activity entry and exit identity.
    /// It owns the active Activity identity for the current application runtime and emits canonical lifecycle events.
    /// </summary>
    internal sealed class ActivityFlowRuntime
    {
        private readonly ActivityContentRuntime _activityContentRuntime = new ActivityContentRuntime();
        private readonly EventBus<ActivityEnteredEvent> _activityEnteredEvents = new EventBus<ActivityEnteredEvent>();
        private readonly EventBus<ActivityExitedEvent> _activityExitedEvents = new EventBus<ActivityExitedEvent>();
        private readonly IEventBinding _activityContentEnteredBinding;
        private readonly IEventBinding _activityContentExitedBinding;
        private ActivityAsset _currentActivity;

        internal ActivityFlowRuntime()
        {
            _activityContentEnteredBinding = _activityEnteredEvents.Subscribe(_activityContentRuntime.HandleActivityEntered);
            _activityContentExitedBinding = _activityExitedEvents.Subscribe(_activityContentRuntime.HandleActivityExited);
        }

        internal ActivityAsset CurrentActivity => _currentActivity;

        internal bool HasActiveActivity => _currentActivity != null;

        internal bool IsActivityActive(ActivityAsset activity)
        {
            return activity != null && ReferenceEquals(_currentActivity, activity);
        }

        internal IEventBinding SubscribeActivityEntered(Action<ActivityEnteredEvent> handler)
        {
            return _activityEnteredEvents.Subscribe(handler);
        }

        internal IEventBinding SubscribeActivityExited(Action<ActivityExitedEvent> handler)
        {
            return _activityExitedEvents.Subscribe(handler);
        }

        internal Task<ActivityFlowStartResult> StartStartupActivityAsync(RouteAsset route, string source, string reason)
        {
            var resolvedSource = NormalizeSource(source);
            var resolvedReason = NormalizeReason(reason);

            if (route == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Route is missing."));
            }

            var previousActivity = _currentActivity;
            if (!route.HasStartupActivity)
            {
                _currentActivity = null;
                var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, null, resolvedSource, resolvedReason);
                return Task.FromResult(ActivityFlowStartResult.SkippedNoStartupActivity(previousActivity, contentResult));
            }

            return StartActivityCoreAsync(route.StartupActivity, previousActivity, resolvedSource, resolvedReason);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(ActivityAsset activity, string source, string reason)
        {
            var resolvedSource = NormalizeSource(source);
            var resolvedReason = NormalizeReason(reason);

            if (activity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity is missing."));
            }

            return StartActivityCoreAsync(activity, _currentActivity, resolvedSource, resolvedReason);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(string source, string reason)
        {
            var resolvedSource = NormalizeSource(source);
            var resolvedReason = NormalizeReason(reason);

            var previousActivity = _currentActivity;
            if (previousActivity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity Flow cannot clear Activity because no Activity is active."));
            }

            _currentActivity = null;
            var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, null, resolvedSource, resolvedReason);
            return Task.FromResult(ActivityFlowStartResult.ClearedByRequest(previousActivity, contentResult));
        }

        private Task<ActivityFlowStartResult> StartActivityCoreAsync(ActivityAsset nextActivity, ActivityAsset previousActivity, string source, string reason)
        {
            var resolvedSource = NormalizeSource(source);
            var resolvedReason = NormalizeReason(reason);

            if (nextActivity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity is missing."));
            }

            if (ReferenceEquals(previousActivity, nextActivity))
            {
                return Task.FromResult(ActivityFlowStartResult.KeptCurrentActivity(nextActivity));
            }

            _currentActivity = nextActivity;
            var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, nextActivity, resolvedSource, resolvedReason);
            return Task.FromResult(ActivityFlowStartResult.StartedWith(nextActivity, previousActivity, contentResult));
        }

        private ActivityContentApplyResult ApplyActivityContentThroughLifecycleEvents(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            var resolvedSource = NormalizeSource(source);
            var resolvedReason = NormalizeReason(reason);

            _activityContentRuntime.ClearLastApplyResult();
            PublishActivityTransition(previousActivity, nextActivity, resolvedSource, resolvedReason);

            if (_activityContentRuntime.HasLastApplyResult)
            {
                return _activityContentRuntime.LastApplyResult;
            }

            // No lifecycle event is emitted when both previous and next Activity are absent.
            // The content owner still needs to enforce the scene-authored no-active-Activity state.
            return _activityContentRuntime.ApplyActiveActivity(nextActivity);
        }

        private void PublishActivityTransition(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            if (previousActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                _activityExitedEvents.Publish(new ActivityExitedEvent(previousActivity, nextActivity, source, reason));
            }

            if (nextActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                _activityEnteredEvents.Publish(new ActivityEnteredEvent(nextActivity, previousActivity, source, reason));
            }
        }

        private static string NormalizeSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        }

        private static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }
    }
}

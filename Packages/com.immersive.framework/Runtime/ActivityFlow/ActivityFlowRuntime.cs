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
        private readonly ActivityContentRuntime activityContentRuntime = new ActivityContentRuntime();
        private readonly EventBus<ActivityEnteredEvent> activityEnteredEvents = new EventBus<ActivityEnteredEvent>();
        private readonly EventBus<ActivityExitedEvent> activityExitedEvents = new EventBus<ActivityExitedEvent>();
        private readonly IEventBinding activityContentEnteredBinding;
        private readonly IEventBinding activityContentExitedBinding;
        private ActivityAsset currentActivity;

        internal ActivityFlowRuntime()
        {
            activityContentEnteredBinding = activityEnteredEvents.Subscribe(activityContentRuntime.HandleActivityEntered);
            activityContentExitedBinding = activityExitedEvents.Subscribe(activityContentRuntime.HandleActivityExited);
        }

        internal ActivityAsset CurrentActivity => currentActivity;

        internal bool HasActiveActivity => currentActivity != null;

        internal bool IsActivityActive(ActivityAsset activity)
        {
            return activity != null && ReferenceEquals(currentActivity, activity);
        }

        internal IEventBinding SubscribeActivityEntered(Action<ActivityEnteredEvent> handler)
        {
            return activityEnteredEvents.Subscribe(handler);
        }

        internal IEventBinding SubscribeActivityExited(Action<ActivityExitedEvent> handler)
        {
            return activityExitedEvents.Subscribe(handler);
        }

        internal Task<ActivityFlowStartResult> StartStartupActivityAsync(RouteAsset route)
        {
            if (route == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Route is missing."));
            }

            var previousActivity = currentActivity;
            if (!route.HasStartupActivity)
            {
                currentActivity = null;
                var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, null);
                return Task.FromResult(ActivityFlowStartResult.SkippedNoStartupActivity(previousActivity, contentResult));
            }

            return StartActivityCoreAsync(route.StartupActivity, previousActivity);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(ActivityAsset activity)
        {
            if (activity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity is missing."));
            }

            return StartActivityCoreAsync(activity, currentActivity);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync()
        {
            var previousActivity = currentActivity;
            if (previousActivity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity Flow cannot clear Activity because no Activity is active."));
            }

            currentActivity = null;
            var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, null);
            return Task.FromResult(ActivityFlowStartResult.ClearedByRequest(previousActivity, contentResult));
        }

        private Task<ActivityFlowStartResult> StartActivityCoreAsync(ActivityAsset nextActivity, ActivityAsset previousActivity)
        {
            if (nextActivity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity is missing."));
            }

            if (ReferenceEquals(previousActivity, nextActivity))
            {
                return Task.FromResult(ActivityFlowStartResult.KeptCurrentActivity(nextActivity));
            }

            currentActivity = nextActivity;
            var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, nextActivity);
            return Task.FromResult(ActivityFlowStartResult.StartedWith(nextActivity, previousActivity, contentResult));
        }

        private ActivityContentApplyResult ApplyActivityContentThroughLifecycleEvents(ActivityAsset previousActivity, ActivityAsset nextActivity)
        {
            activityContentRuntime.ClearLastApplyResult();
            PublishActivityTransition(previousActivity, nextActivity);

            if (activityContentRuntime.HasLastApplyResult)
            {
                return activityContentRuntime.LastApplyResult;
            }

            // No lifecycle event is emitted when both previous and next Activity are absent.
            // The content owner still needs to enforce the scene-authored no-active-Activity state.
            return activityContentRuntime.ApplyActiveActivity(nextActivity);
        }

        private void PublishActivityTransition(ActivityAsset previousActivity, ActivityAsset nextActivity)
        {
            if (previousActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                activityExitedEvents.Publish(new ActivityExitedEvent(previousActivity, nextActivity));
            }

            if (nextActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                activityEnteredEvents.Publish(new ActivityEnteredEvent(nextActivity, previousActivity));
            }
        }
    }
}

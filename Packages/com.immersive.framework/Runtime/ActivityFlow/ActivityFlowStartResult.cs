using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal immutable result for starting or clearing an Activity.
    /// This is diagnostics data, not an activity service.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct ActivityFlowStartResult
    {
        public ActivityFlowStartResult(
            bool started,
            bool skipped,
            bool keptActive,
            bool cleared,
            string message,
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult)
        {
            Started = started;
            Skipped = skipped;
            KeptActive = keptActive;
            Cleared = cleared;
            Message = message ?? string.Empty;
            ActivityState = activityState;
            PreviousActivity = previousActivity;
            ActivityContentResult = activityContentResult;
        }

        public bool Started { get; }

        public bool Skipped { get; }

        public bool KeptActive { get; }

        public bool Cleared { get; }

        public string Message { get; }

        public ActivityRuntimeState ActivityState { get; }

        public ActivityAsset Activity => ActivityState.Activity;

        public ActivityAsset PreviousActivity { get; }

        public ActivityContentApplyResult ActivityContentResult { get; }

        public bool ReplacedPreviousActivity => Started && PreviousActivity != null && !ReferenceEquals(PreviousActivity, Activity);

        public bool Completed => Started || Skipped || KeptActive || Cleared;

        public bool HasActivityState => ActivityState.IsActive || ActivityState.IsNone || ActivityState.IsTransitioning;

        public string ActivityIdentity => ActivityState.DiagnosticIdentity;

        public static ActivityFlowStartResult Failed(string message)
        {
            return new ActivityFlowStartResult(false, false, false, false, message, default, null, default);
        }

        public static ActivityFlowStartResult SkippedNoStartupActivity(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult)
        {
            if (previousActivity == null)
            {
                return new ActivityFlowStartResult(
                    false,
                    true,
                    false,
                    false,
                    AppendContentMessage(ActivityStateMessage(activityState), activityContentResult),
                    activityState,
                    null,
                    activityContentResult);
            }

            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage($"Activity Flow cleared Activity '{previousActivity.ActivityName}' because Route has no Startup Activity. {ActivityStateMessage(activityState)}", activityContentResult),
                activityState,
                previousActivity,
                activityContentResult);
        }

        public static ActivityFlowStartResult ClearedByRequest(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult)
        {
            if (previousActivity == null)
            {
                return Failed("Activity Flow cannot clear Activity because no Activity is active.");
            }

            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage($"Activity Flow cleared Activity '{previousActivity.ActivityName}' by request. {ActivityStateMessage(activityState)}", activityContentResult),
                activityState,
                previousActivity,
                activityContentResult);
        }

        public static ActivityFlowStartResult KeptCurrentActivity(ActivityRuntimeState activityState)
        {
            return new ActivityFlowStartResult(
                false,
                false,
                true,
                false,
                $"Activity Flow kept Activity '{activityState.ActivityName}' active. {ActivityStateMessage(activityState)}",
                activityState,
                activityState.Activity,
                default);
        }

        public static ActivityFlowStartResult StartedWith(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult)
        {
            var activity = activityState.Activity;
            string message = previousActivity != null && !ReferenceEquals(previousActivity, activity)
                ? $"Activity Flow switched from Activity '{previousActivity.ActivityName}' to Activity '{activity.ActivityName}'. {ActivityStateMessage(activityState)}"
                : $"Activity Flow started Activity '{activity.ActivityName}'. {ActivityStateMessage(activityState)}";

            return new ActivityFlowStartResult(
                true,
                false,
                false,
                false,
                AppendContentMessage(message, activityContentResult),
                activityState,
                previousActivity,
                activityContentResult);
        }

        private static string ActivityStateMessage(ActivityRuntimeState activityState)
        {
            if (activityState.IsActive)
            {
                return $"activityState='{activityState.DiagnosticStatus}' activityIdentity='{activityState.DiagnosticIdentity}'.";
            }

            return $"activityState='{activityState.DiagnosticStatus}'.";
        }

        private static string AppendContentMessage(string message, ActivityContentApplyResult activityContentResult)
        {
            if (!activityContentResult.HasBindings)
            {
                return message ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return activityContentResult.Message;
            }

            return $"{message} {activityContentResult.Message}";
        }
    }
}

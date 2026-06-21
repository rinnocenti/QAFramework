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
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult)
        {
            Started = started;
            Skipped = skipped;
            KeptActive = keptActive;
            Cleared = cleared;
            Message = message ?? string.Empty;
            Activity = activity;
            PreviousActivity = previousActivity;
            ActivityContentResult = activityContentResult;
        }

        public bool Started { get; }

        public bool Skipped { get; }

        public bool KeptActive { get; }

        public bool Cleared { get; }

        public string Message { get; }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityContentApplyResult ActivityContentResult { get; }

        public bool ReplacedPreviousActivity => Started && PreviousActivity != null && !ReferenceEquals(PreviousActivity, Activity);

        public bool Completed => Started || Skipped || KeptActive || Cleared;

        public static ActivityFlowStartResult Failed(string message)
        {
            return new ActivityFlowStartResult(false, false, false, false, message, null, null, default);
        }

        public static ActivityFlowStartResult SkippedNoStartupActivity(
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
                    AppendContentMessage(string.Empty, activityContentResult),
                    null,
                    null,
                    activityContentResult);
            }

            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage($"Activity Flow cleared Activity '{previousActivity.ActivityName}' because Route has no Startup Activity.", activityContentResult),
                null,
                previousActivity,
                activityContentResult);
        }

        public static ActivityFlowStartResult ClearedByRequest(
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
                AppendContentMessage($"Activity Flow cleared Activity '{previousActivity.ActivityName}' by request.", activityContentResult),
                null,
                previousActivity,
                activityContentResult);
        }

        public static ActivityFlowStartResult KeptCurrentActivity(ActivityAsset activity)
        {
            return new ActivityFlowStartResult(
                false,
                false,
                true,
                false,
                $"Activity Flow kept Activity '{activity.ActivityName}' active.",
                activity,
                activity,
                default);
        }

        public static ActivityFlowStartResult StartedWith(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult)
        {
            string message = previousActivity != null && !ReferenceEquals(previousActivity, activity)
                ? $"Activity Flow switched from Activity '{previousActivity.ActivityName}' to Activity '{activity.ActivityName}'."
                : $"Activity Flow started Activity '{activity.ActivityName}'.";

            return new ActivityFlowStartResult(
                true,
                false,
                false,
                false,
                AppendContentMessage(message, activityContentResult),
                activity,
                previousActivity,
                activityContentResult);
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

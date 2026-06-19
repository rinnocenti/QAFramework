using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal immutable result for starting an Activity from a Route.
    /// This is diagnostics data, not an activity service.
    /// </summary>
    internal readonly struct ActivityFlowStartResult
    {
        public ActivityFlowStartResult(
            bool started,
            bool skipped,
            bool keptActive,
            bool cleared,
            string message,
            ActivityAsset activity,
            ActivityAsset previousActivity)
        {
            Started = started;
            Skipped = skipped;
            KeptActive = keptActive;
            Cleared = cleared;
            Message = message ?? string.Empty;
            Activity = activity;
            PreviousActivity = previousActivity;
        }

        public bool Started { get; }

        public bool Skipped { get; }

        public bool KeptActive { get; }

        public bool Cleared { get; }

        public string Message { get; }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public bool ReplacedPreviousActivity => Started && PreviousActivity != null && !ReferenceEquals(PreviousActivity, Activity);

        public bool Completed => Started || Skipped || KeptActive || Cleared;

        public static ActivityFlowStartResult Failed(string message)
        {
            return new ActivityFlowStartResult(false, false, false, false, message, null, null);
        }

        public static ActivityFlowStartResult SkippedNoStartupActivity(ActivityAsset previousActivity)
        {
            if (previousActivity == null)
            {
                return new ActivityFlowStartResult(false, true, false, false, string.Empty, null, null);
            }

            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                $"Activity Flow cleared Activity '{previousActivity.ActivityName}' because Route has no Startup Activity.",
                null,
                previousActivity);
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
                activity);
        }

        public static ActivityFlowStartResult StartedWith(ActivityAsset activity, ActivityAsset previousActivity)
        {
            var message = previousActivity != null && !ReferenceEquals(previousActivity, activity)
                ? $"Activity Flow switched from Activity '{previousActivity.ActivityName}' to Activity '{activity.ActivityName}'."
                : $"Activity Flow started Activity '{activity.ActivityName}'.";

            return new ActivityFlowStartResult(
                true,
                false,
                false,
                false,
                message,
                activity,
                previousActivity);
        }
    }
}

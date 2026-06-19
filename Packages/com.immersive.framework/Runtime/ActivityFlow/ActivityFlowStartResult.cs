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
            string message,
            ActivityAsset activity)
        {
            Started = started;
            Skipped = skipped;
            Message = message ?? string.Empty;
            Activity = activity;
        }

        public bool Started { get; }

        public bool Skipped { get; }

        public string Message { get; }

        public ActivityAsset Activity { get; }

        public bool Completed => Started || Skipped;

        public static ActivityFlowStartResult Failed(string message)
        {
            return new ActivityFlowStartResult(false, false, message, null);
        }

        public static ActivityFlowStartResult SkippedNoStartupActivity()
        {
            return new ActivityFlowStartResult(false, true, string.Empty, null);
        }

        public static ActivityFlowStartResult StartedWith(ActivityAsset activity)
        {
            return new ActivityFlowStartResult(
                true,
                false,
                $"Activity Flow started Activity '{activity.ActivityName}'.",
                activity);
        }
    }
}

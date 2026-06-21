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
            ActivityContentApplyResult activityContentResult,
            ActivityReadinessState activityReadinessState)
        {
            Started = started;
            Skipped = skipped;
            KeptActive = keptActive;
            Cleared = cleared;
            Message = message ?? string.Empty;
            ActivityState = activityState;
            PreviousActivity = previousActivity;
            ActivityContentResult = activityContentResult;
            ActivityReadinessState = activityReadinessState;
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

        public ActivityContentSet ActivityContentSet => ActivityContentResult.ActivityContentSet;

        public ActivityContentLifecycleResult ActivityContentLifecycleResult => ActivityContentResult.LifecycleResult;

        public ActivityReadinessState ActivityReadinessState { get; }

        public bool HasActivityContent => ActivityContentSet.HasContent;

        public bool HasActivityContentLifecycle => ActivityContentLifecycleResult.Executed;

        public bool HasActivityReadiness => ActivityReadinessState.IsReady || ActivityReadinessState.IsNone || ActivityReadinessState.IsNotReady;

        public bool IsActivityReady => ActivityReadinessState.IsReady;

        public bool ReplacedPreviousActivity => Started && PreviousActivity != null && !ReferenceEquals(PreviousActivity, Activity);

        public bool Completed => Started || Skipped || KeptActive || Cleared;

        public bool HasActivityState => ActivityState.IsActive || ActivityState.IsNone || ActivityState.IsTransitioning;

        public string ActivityIdentity => ActivityState.DiagnosticIdentity;

        public static ActivityFlowStartResult Failed(string message)
        {
            return new ActivityFlowStartResult(false, false, false, false, message, default, null, default, default);
        }

        public static ActivityFlowStartResult SkippedNoStartupActivity(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult)
        {
            var readinessState = BuildReadinessState(activityState, activityContentResult);
            if (previousActivity == null)
            {
                return new ActivityFlowStartResult(
                    false,
                    true,
                    false,
                    false,
                    AppendContentMessage(CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState), activityContentResult),
                    activityState,
                    null,
                    activityContentResult,
                    readinessState);
            }

            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage($"Activity Flow cleared Activity '{previousActivity.ActivityName}' because Route has no Startup Activity. {CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState)}", activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState);
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

            var readinessState = BuildReadinessState(activityState, activityContentResult);
            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage($"Activity Flow cleared Activity '{previousActivity.ActivityName}' by request. {CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState)}", activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState);
        }

        public static ActivityFlowStartResult KeptCurrentActivity(ActivityRuntimeState activityState)
        {
            var activityContentResult = ActivityContentApplyResult.Empty(activityState.Activity);
            var readinessState = BuildReadinessState(activityState, activityContentResult);
            return new ActivityFlowStartResult(
                false,
                false,
                true,
                false,
                $"Activity Flow kept Activity '{activityState.ActivityName}' active. {CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState)}",
                activityState,
                activityState.Activity,
                activityContentResult,
                readinessState);
        }

        public static ActivityFlowStartResult StartedWith(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult)
        {
            var activity = activityState.Activity;
            var readinessState = BuildReadinessState(activityState, activityContentResult);
            string stateMessage = CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState);
            string message = previousActivity != null && !ReferenceEquals(previousActivity, activity)
                ? $"Activity Flow switched from Activity '{previousActivity.ActivityName}' to Activity '{activity.ActivityName}'. {stateMessage}"
                : $"Activity Flow started Activity '{activity.ActivityName}'. {stateMessage}";

            return new ActivityFlowStartResult(
                true,
                false,
                false,
                false,
                AppendContentMessage(message, activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState);
        }

        private static ActivityReadinessState BuildReadinessState(ActivityRuntimeState activityState, ActivityContentApplyResult activityContentResult)
        {
            return ActivityReadinessState.FromActivityResult(
                activityState,
                activityContentResult,
                activityState.Source,
                activityState.Reason);
        }

        private static string ActivityStateMessage(ActivityRuntimeState activityState)
        {
            if (activityState.IsActive)
            {
                return $"activityState='{activityState.DiagnosticStatus}' activityIdentity='{activityState.DiagnosticIdentity}'.";
            }

            return $"activityState='{activityState.DiagnosticStatus}'.";
        }

        private static string ActivityReadinessMessage(ActivityReadinessState activityReadinessState)
        {
            if (activityReadinessState.IsReady)
            {
                return $"activityReadiness='{activityReadinessState.DiagnosticStatus}' activityReadinessReason='{activityReadinessState.DiagnosticReason}' activityReadinessIssues='0'.";
            }

            if (activityReadinessState.IsNotReady)
            {
                return $"activityReadiness='{activityReadinessState.DiagnosticStatus}' activityReadinessReason='{activityReadinessState.DiagnosticReason}' activityReadinessIssues='{activityReadinessState.BlockingIssueCount}'.";
            }

            return $"activityReadiness='{activityReadinessState.DiagnosticStatus}' activityReadinessReason='{activityReadinessState.DiagnosticReason}' activityReadinessIssues='0'.";
        }

        private static string CombineStateAndReadinessMessage(string stateMessage, ActivityReadinessState readinessState)
        {
            if (string.IsNullOrWhiteSpace(stateMessage))
            {
                return ActivityReadinessMessage(readinessState);
            }

            return $"{stateMessage} {ActivityReadinessMessage(readinessState)}";
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

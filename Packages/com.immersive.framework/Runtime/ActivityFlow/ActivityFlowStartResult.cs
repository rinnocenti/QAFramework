using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

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
            ActivityReadinessState activityReadinessState,
            RuntimeScopeLifecycleResult runtimeActivityScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult))
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
            RuntimeActivityScopeResult = runtimeActivityScopeResult;
            ActivityContentAnchorBindingCleanupResult = activityContentAnchorBindingCleanupResult;
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

        public RuntimeScopeLifecycleResult RuntimeActivityScopeResult { get; }

        public ContentAnchorBindingLifecycleResult ActivityContentAnchorBindingCleanupResult { get; }

        public bool HasRuntimeActivityScope => RuntimeActivityScopeResult.Executed;

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
            ActivityContentApplyResult activityContentResult,
            RuntimeScopeLifecycleResult runtimeActivityScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult))
        {
            var readinessState = BuildReadinessState(activityState, activityContentResult);
            var runtimeScopeMessage = RuntimeScopeMessage(runtimeActivityScopeResult);
            var bindingCleanupMessage = BindingCleanupMessage(activityContentAnchorBindingCleanupResult);
            if (previousActivity == null)
            {
                return new ActivityFlowStartResult(
                    false,
                    true,
                    false,
                    false,
                    AppendContentMessage(CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState) + bindingCleanupMessage + runtimeScopeMessage, activityContentResult),
                    activityState,
                    null,
                    activityContentResult,
                    readinessState,
                    runtimeActivityScopeResult,
                    activityContentAnchorBindingCleanupResult);
            }

            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage($"Activity Flow cleared Activity '{previousActivity.ActivityName}' because Route has no Startup Activity. {CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState)}{bindingCleanupMessage}{runtimeScopeMessage}", activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState,
                runtimeActivityScopeResult,
                activityContentAnchorBindingCleanupResult);
        }

        public static ActivityFlowStartResult ClearedByRequest(
            ActivityRuntimeState activityState,
            ActivityAsset previousActivity,
            ActivityContentApplyResult activityContentResult,
            RuntimeScopeLifecycleResult runtimeActivityScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult))
        {
            if (previousActivity == null)
            {
                return Failed("Activity Flow cannot clear Activity because no Activity is active.");
            }

            var readinessState = BuildReadinessState(activityState, activityContentResult);
            var runtimeScopeMessage = RuntimeScopeMessage(runtimeActivityScopeResult);
            var bindingCleanupMessage = BindingCleanupMessage(activityContentAnchorBindingCleanupResult);
            return new ActivityFlowStartResult(
                false,
                false,
                false,
                true,
                AppendContentMessage($"Activity Flow cleared Activity '{previousActivity.ActivityName}' by request. {CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState)}{bindingCleanupMessage}{runtimeScopeMessage}", activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState,
                runtimeActivityScopeResult,
                activityContentAnchorBindingCleanupResult);
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
            ActivityContentApplyResult activityContentResult,
            RuntimeScopeLifecycleResult runtimeActivityScopeResult = default(RuntimeScopeLifecycleResult),
            ContentAnchorBindingLifecycleResult activityContentAnchorBindingCleanupResult = default(ContentAnchorBindingLifecycleResult))
        {
            var activity = activityState.Activity;
            var readinessState = BuildReadinessState(activityState, activityContentResult);
            string stateMessage = CombineStateAndReadinessMessage(ActivityStateMessage(activityState), readinessState);
            string runtimeScopeMessage = RuntimeScopeMessage(runtimeActivityScopeResult);
            string bindingCleanupMessage = BindingCleanupMessage(activityContentAnchorBindingCleanupResult);
            string message = previousActivity != null && !ReferenceEquals(previousActivity, activity)
                ? $"Activity Flow switched from Activity '{previousActivity.ActivityName}' to Activity '{activity.ActivityName}'. {stateMessage}{bindingCleanupMessage}{runtimeScopeMessage}"
                : $"Activity Flow started Activity '{activity.ActivityName}'. {stateMessage}{bindingCleanupMessage}{runtimeScopeMessage}";

            return new ActivityFlowStartResult(
                true,
                false,
                false,
                false,
                AppendContentMessage(message, activityContentResult),
                activityState,
                previousActivity,
                activityContentResult,
                readinessState,
                runtimeActivityScopeResult,
                activityContentAnchorBindingCleanupResult);
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

        private static string RuntimeScopeMessage(RuntimeScopeLifecycleResult runtimeActivityScopeResult)
        {
            if (!runtimeActivityScopeResult.Executed)
            {
                return string.Empty;
            }

            return $" runtimeActivityScope='{runtimeActivityScopeResult.DiagnosticStatus}' runtimeActivityRootEnter='{runtimeActivityScopeResult.EnterStatus}' runtimeActivityRootExit='{runtimeActivityScopeResult.ExitStatus}' runtimeActivityContext='{runtimeActivityScopeResult.ContextStatus}' runtimeRootCount='{runtimeActivityScopeResult.RootCount}'.";
        }

        private static string BindingCleanupMessage(ContentAnchorBindingLifecycleResult result)
        {
            if (!result.Executed)
            {
                return string.Empty;
            }

            return $" activityContentAnchorBindingCleanup='{result.DiagnosticStatus}' activityContentAnchorBindingCleanupRemoved='{result.RemovedCount}' activityContentAnchorBindingCleanupBefore='{result.BindingCountBefore}' activityContentAnchorBindingCleanupAfter='{result.BindingCountAfter}'.";
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

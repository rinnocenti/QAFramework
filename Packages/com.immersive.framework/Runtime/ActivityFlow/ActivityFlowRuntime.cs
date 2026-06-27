using System;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Minimal owner for Activity entry and exit identity.
    /// It owns the active Activity runtime state for the current application runtime and emits canonical lifecycle events.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class ActivityFlowRuntime
    {
        private readonly ActivityContentRuntime _activityContentRuntime = new ActivityContentRuntime();
        private readonly ContentAnchorDiscoveryRuntime _contentAnchorDiscoveryRuntime = new ContentAnchorDiscoveryRuntime();
        private readonly ActivityContentExecutionRuntime _activityContentExecutionRuntime = new ActivityContentExecutionRuntime();
        private IActivityContentExecutionParticipantSource _activityContentExecutionParticipantSource;
        private readonly RuntimeContentRuntime _runtimeContentRuntime;
        private readonly RuntimeContentAnchorBinding _contentAnchorBindingRuntime;
        private readonly EventBus<ActivityEnteredEvent> _activityEnteredEvents = new EventBus<ActivityEnteredEvent>();
        private readonly EventBus<ActivityExitedEvent> _activityExitedEvents = new EventBus<ActivityExitedEvent>();
        private readonly IEventBinding _activityContentEnteredBinding;
        private readonly IEventBinding _activityContentExitedBinding;
        private RouteAsset _currentRoute;
        private ActivityRuntimeState _currentActivityState;

        internal ActivityFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime)
            : this(runtimeContentRuntime, contentAnchorBindingRuntime, EmptyActivityContentExecutionParticipantSource.Instance)
        {
        }

        internal ActivityFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime,
            IActivityContentExecutionParticipantSource activityContentExecutionParticipantSource)
        {
            _runtimeContentRuntime = runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime));
            _contentAnchorBindingRuntime = contentAnchorBindingRuntime ?? throw new ArgumentNullException(nameof(contentAnchorBindingRuntime));
            _activityContentExecutionParticipantSource = activityContentExecutionParticipantSource ?? EmptyActivityContentExecutionParticipantSource.Instance;
            _currentActivityState = ActivityRuntimeState.Empty();
            _activityContentEnteredBinding = _activityEnteredEvents.Subscribe(_activityContentRuntime.HandleActivityEntered);
            _activityContentExitedBinding = _activityExitedEvents.Subscribe(_activityContentRuntime.HandleActivityExited);
        }

        internal ActivityRuntimeState CurrentActivityState => _currentActivityState;

        internal ActivityAsset CurrentActivity => _currentActivityState.Activity;

        internal bool HasActiveActivity => _currentActivityState.IsActive;

        internal bool IsActivityActive(ActivityAsset activity)
        {
            return activity != null && ReferenceEquals(_currentActivityState.Activity, activity);
        }

        internal void SetActivityContentExecutionParticipantSource(IActivityContentExecutionParticipantSource participantSource)
        {
            _activityContentExecutionParticipantSource = participantSource ?? EmptyActivityContentExecutionParticipantSource.Instance;
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
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Route is missing."));
            }

            _currentRoute = route;
            var previousActivity = _currentActivityState.Activity;
            if (!route.HasStartupActivity)
            {
                _currentActivityState = ActivityRuntimeState.None(previousActivity, resolvedSource, resolvedReason);
                var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, null, resolvedSource, resolvedReason);
                var executionResult = ExecuteActivityContentLifecycle(previousActivity, null, resolvedSource, resolvedReason);
                var bindingCleanupResult = CleanupPreviousActivityContentAnchorBindings(previousActivity, null, resolvedSource, resolvedReason);
                var runtimeScopeResult = RemovePreviousActivityScopeRoot(previousActivity, null, resolvedSource, resolvedReason);
                return Task.FromResult(ActivityFlowStartResult.SkippedNoStartupActivity(
                    _currentActivityState,
                    previousActivity,
                    contentResult,
                    runtimeScopeResult,
                    bindingCleanupResult,
                    ActivityContentAnchorDiscoveryResult.Empty(null, resolvedSource, resolvedReason, "No startup Activity is active; Activity Content Anchor discovery was skipped."),
                    executionResult));
            }

            return StartActivityCoreAsync(route.StartupActivity, previousActivity, resolvedSource, resolvedReason);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(ActivityAsset activity, string source, string reason)
        {
            return StartActivityAsync(activity, _currentRoute, source, reason);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(ActivityAsset activity, RouteAsset route, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (activity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity is missing."));
            }

            if (route != null)
            {
                _currentRoute = route;
            }

            return StartActivityCoreAsync(activity, _currentActivityState.Activity, resolvedSource, resolvedReason);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(string source, string reason)
        {
            return ClearActivityAsync(_currentRoute, source, reason);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(RouteAsset route, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (route != null)
            {
                _currentRoute = route;
            }

            var previousActivity = _currentActivityState.Activity;
            if (previousActivity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity Flow cannot clear Activity because no Activity is active."));
            }

            _currentActivityState = ActivityRuntimeState.None(previousActivity, resolvedSource, resolvedReason);
            var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, null, resolvedSource, resolvedReason);
            var executionResult = ExecuteActivityContentLifecycle(previousActivity, null, resolvedSource, resolvedReason);
            var sceneCompositionResult = CreateActivitySceneCompositionResult(null, resolvedSource, resolvedReason);
            var bindingCleanupResult = CleanupPreviousActivityContentAnchorBindings(previousActivity, null, resolvedSource, resolvedReason);
            var runtimeScopeResult = RemovePreviousActivityScopeRoot(previousActivity, null, resolvedSource, resolvedReason);
            return Task.FromResult(ActivityFlowStartResult.ClearedByRequest(
                _currentActivityState,
                previousActivity,
                contentResult,
                runtimeScopeResult,
                bindingCleanupResult,
                ActivityContentAnchorDiscoveryResult.Empty(null, resolvedSource, resolvedReason, "Activity was cleared; Activity Content Anchor discovery was skipped."),
                executionResult,
                sceneCompositionResult));
        }

        private Task<ActivityFlowStartResult> StartActivityCoreAsync(ActivityAsset nextActivity, ActivityAsset previousActivity, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (nextActivity == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("Activity is missing."));
            }

            if (ReferenceEquals(previousActivity, nextActivity))
            {
                if (!_currentActivityState.IsActive)
                {
                    _currentActivityState = ActivityRuntimeState.ActiveWith(nextActivity, previousActivity, resolvedSource, resolvedReason);
                }

                return Task.FromResult(ActivityFlowStartResult.KeptCurrentActivity(_currentActivityState));
            }

            var runtimeEnterResult = CreateActivityScopeRoot(nextActivity, resolvedSource, resolvedReason);
            _currentActivityState = ActivityRuntimeState.ActiveWith(nextActivity, previousActivity, resolvedSource, resolvedReason);
            var contentResult = ApplyActivityContentThroughLifecycleEvents(previousActivity, nextActivity, resolvedSource, resolvedReason);
            var activityContentAnchorDiscoveryResult = _contentAnchorDiscoveryRuntime.DiscoverActivityAnchors(
                nextActivity,
                _currentRoute,
                resolvedSource,
                resolvedReason);
            var executionResult = ExecuteActivityContentLifecycle(previousActivity, nextActivity, resolvedSource, resolvedReason);
            var sceneCompositionResult = CreateActivitySceneCompositionResult(nextActivity, resolvedSource, resolvedReason);
            var bindingCleanupResult = CleanupPreviousActivityContentAnchorBindings(previousActivity, nextActivity, resolvedSource, resolvedReason);
            var runtimeExitResult = RemovePreviousActivityScopeRoot(previousActivity, nextActivity, resolvedSource, resolvedReason);
            var runtimeScopeResult = MergeActivityScopeResults(runtimeEnterResult, runtimeExitResult, nextActivity, previousActivity, resolvedSource, resolvedReason);

            return Task.FromResult(ActivityFlowStartResult.StartedWith(
                _currentActivityState,
                previousActivity,
                contentResult,
                runtimeScopeResult,
                bindingCleanupResult,
                activityContentAnchorDiscoveryResult,
                executionResult,
                sceneCompositionResult));
        }

        private static ActivitySceneCompositionResult CreateActivitySceneCompositionResult(
            ActivityAsset activity,
            string source,
            string reason)
        {
            var plan = ActivitySceneCompositionPlan.FromActivity(activity, source, reason);
            return ActivitySceneCompositionResult.FromPlan(plan, source, reason);
        }

        private ActivityContentExecutionLifecycleResult ExecuteActivityContentLifecycle(
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            if (previousActivity == null && nextActivity == null)
            {
                return ActivityContentExecutionLifecycleResult.None(
                    resolvedSource,
                    resolvedReason,
                    "Activity content execution lifecycle skipped because there is no previous or next Activity.");
            }

            var participantSourceRequest = new ActivityContentExecutionParticipantSourceRequest(
                _currentRoute,
                previousActivity,
                nextActivity,
                resolvedSource,
                resolvedReason);
            var participantSourceResult = ResolveActivityContentExecutionParticipants(participantSourceRequest);
            var participants = participantSourceResult.Collection;
            var enterPlan = default(ActivityContentExecutionPhasePlan);
            var enterResult = default(ActivityContentExecutionAggregateResult);
            var exitPlan = default(ActivityContentExecutionPhasePlan);
            var exitResult = default(ActivityContentExecutionAggregateResult);

            if (previousActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                if (TryCreateActivityScopeContext(previousActivity, resolvedSource, resolvedReason, out var exitContext))
                {
                    exitPlan = ActivityContentExecutionRequestFactory.CreateExitPlan(
                        previousActivity,
                        nextActivity,
                        exitContext,
                        participants,
                        resolvedSource,
                        resolvedReason);
                    exitResult = _activityContentExecutionRuntime.ExecutePhasePlan(exitPlan, resolvedSource, resolvedReason);
                }
                else
                {
                    exitResult = ActivityContentExecutionAggregateResult.RejectedInvalidResults(
                        ActivityContentExecutionPhase.Exit,
                        previousActivity,
                        previousActivity,
                        nextActivity,
                        resolvedSource,
                        resolvedReason,
                        "Activity content execution exit phase rejected because previous Activity runtime scope context is not available.");
                }
            }

            if (nextActivity != null && !ReferenceEquals(previousActivity, nextActivity))
            {
                if (TryCreateActivityScopeContext(nextActivity, resolvedSource, resolvedReason, out var enterContext))
                {
                    enterPlan = ActivityContentExecutionRequestFactory.CreateEnterPlan(
                        nextActivity,
                        previousActivity,
                        enterContext,
                        participants,
                        resolvedSource,
                        resolvedReason);
                    enterResult = _activityContentExecutionRuntime.ExecutePhasePlan(enterPlan, resolvedSource, resolvedReason);
                }
                else
                {
                    enterResult = ActivityContentExecutionAggregateResult.RejectedInvalidResults(
                        ActivityContentExecutionPhase.Enter,
                        nextActivity,
                        previousActivity,
                        nextActivity,
                        resolvedSource,
                        resolvedReason,
                        "Activity content execution enter phase rejected because next Activity runtime scope context is not available.");
                }
            }

            var activity = nextActivity != null ? nextActivity : previousActivity;
            return ActivityContentExecutionLifecycleResult.FromResults(
                activity,
                previousActivity,
                nextActivity,
                participantSourceResult,
                participants,
                enterPlan,
                enterResult,
                exitPlan,
                exitResult,
                resolvedSource,
                resolvedReason,
                "Activity content execution lifecycle integrated with ActivityFlow using the currently resolved participant collection.");
        }

        private ActivityContentExecutionParticipantSourceResult ResolveActivityContentExecutionParticipants(
            ActivityContentExecutionParticipantSourceRequest request)
        {
            if (!request.IsValid)
            {
                return ActivityContentExecutionParticipantSourceResult.RejectedInvalidRequest(
                    request,
                    request.Source,
                    request.Reason,
                    "Activity content execution participant source request rejected because no Activity transition is available.");
            }

            try
            {
                var result = _activityContentExecutionParticipantSource.ResolveActivityContentExecutionParticipants(request);
                if (!result.Executed)
                {
                    return ActivityContentExecutionParticipantSourceResult.FailedResult(
                        request,
                        request.Source,
                        request.Reason,
                        "Activity content execution participant source returned a non-executed result for an executable lifecycle request.");
                }

                return result;
            }
            catch (Exception exception)
            {
                return ActivityContentExecutionParticipantSourceResult.FailedException(
                    request,
                    exception,
                    request.Source,
                    request.Reason);
            }
        }

        private bool TryCreateActivityScopeContext(
            ActivityAsset activity,
            string source,
            string reason,
            out RuntimeScopeContext context)
        {
            if (activity == null)
            {
                context = default(RuntimeScopeContext);
                return false;
            }

            return _runtimeContentRuntime.TryCreateScopeContext(CreateActivityOwner(activity), source, reason, out context);
        }

        private ActivityContentApplyResult ApplyActivityContentThroughLifecycleEvents(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            string resolvedSource = NormalizeSource(source);
            string resolvedReason = NormalizeReason(reason);

            _activityContentRuntime.SetRouteScope(_currentRoute);
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

        private RuntimeScopeLifecycleResult CreateActivityScopeRoot(ActivityAsset activity, string source, string reason)
        {
            if (activity == null)
            {
                return RuntimeScopeLifecycleResult.None(RuntimeContentScope.Activity, source, reason);
            }

            var owner = CreateActivityOwner(activity);
            var enterResult = _runtimeContentRuntime.CreateScopeRoot(owner, source, reason);
            _runtimeContentRuntime.TryCreateScopeContext(owner, source, reason, out var context);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Activity,
                owner,
                enterResult,
                null,
                context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private ContentAnchorBindingLifecycleResult CleanupPreviousActivityContentAnchorBindings(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            if (previousActivity == null || ReferenceEquals(previousActivity, nextActivity))
            {
                return default(ContentAnchorBindingLifecycleResult);
            }

            var owner = CreateActivityOwner(previousActivity);
            if (nextActivity != null && owner == CreateActivityOwner(nextActivity))
            {
                return default(ContentAnchorBindingLifecycleResult);
            }

            return _contentAnchorBindingRuntime.UnbindRuntimeOwner(owner, source, reason);
        }

        private RuntimeScopeLifecycleResult RemovePreviousActivityScopeRoot(ActivityAsset previousActivity, ActivityAsset nextActivity, string source, string reason)
        {
            if (previousActivity == null || ReferenceEquals(previousActivity, nextActivity))
            {
                return RuntimeScopeLifecycleResult.None(RuntimeContentScope.Activity, source, reason);
            }

            var owner = CreateActivityOwner(previousActivity);
            if (nextActivity != null && owner == CreateActivityOwner(nextActivity))
            {
                return RuntimeScopeLifecycleResult.None(RuntimeContentScope.Activity, source, reason);
            }

            var exitResult = _runtimeContentRuntime.RemoveScopeRoot(owner, source, reason);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Activity,
                owner,
                null,
                exitResult,
                default(RuntimeScopeContext),
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private RuntimeScopeLifecycleResult MergeActivityScopeResults(
            RuntimeScopeLifecycleResult enterResult,
            RuntimeScopeLifecycleResult exitResult,
            ActivityAsset nextActivity,
            ActivityAsset previousActivity,
            string source,
            string reason)
        {
            var owner = nextActivity != null
                ? CreateActivityOwner(nextActivity)
                : previousActivity != null ? CreateActivityOwner(previousActivity) : default(RuntimeContentOwner);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Activity,
                owner,
                enterResult.EnterRootResult,
                exitResult.ExitRootResult,
                enterResult.Context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private static RuntimeContentOwner CreateActivityOwner(ActivityAsset activity)
        {
            if (activity == null)
            {
                throw new ArgumentNullException(nameof(activity));
            }

            return RuntimeContentOwner.Activity(activity.ActivityName, activity.ActivityName);
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

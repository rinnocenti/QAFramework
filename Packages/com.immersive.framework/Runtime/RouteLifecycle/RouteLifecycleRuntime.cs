using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.CycleReset;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Minimal owner for starting and switching Routes.
    /// It owns the active Route identity and delegates scene loading to Scene Lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class RouteLifecycleRuntime
    {
        private readonly SceneLifecycleRuntime _sceneLifecycleRuntime = new SceneLifecycleRuntime();
        private readonly ActivityFlowRuntime _activityFlowRuntime;
        private readonly RouteContentRuntime _routeContentRuntime = new RouteContentRuntime();
        private readonly RouteSceneCompositionRuntime _routeSceneCompositionRuntime;
        private readonly ContentReleaseRuntime _contentReleaseRuntime;
        private readonly ContentAnchorDiscoveryRuntime _contentAnchorDiscoveryRuntime = new ContentAnchorDiscoveryRuntime();
        private readonly RuntimeContentRuntime _runtimeContentRuntime;
        private readonly RuntimeContentAnchorBinding _contentAnchorBindingRuntime;
        private readonly CycleResetRuntime _cycleResetRuntime = new CycleResetRuntime();
        private readonly EventBus<RouteEnteredEvent> _routeEnteredEvents = new EventBus<RouteEnteredEvent>();
        private readonly EventBus<RouteExitedEvent> _routeExitedEvents = new EventBus<RouteExitedEvent>();
        private RouteRuntimeState _currentRouteState;
        private ICycleResetParticipantSource _cycleResetParticipantSource = EmptyCycleResetParticipantSource.Instance;

        internal RouteLifecycleRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime)
        {
            _runtimeContentRuntime = runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime));
            _contentAnchorBindingRuntime = contentAnchorBindingRuntime ?? throw new ArgumentNullException(nameof(contentAnchorBindingRuntime));
            _activityFlowRuntime = new ActivityFlowRuntime(_runtimeContentRuntime, _contentAnchorBindingRuntime);
            _routeSceneCompositionRuntime = new RouteSceneCompositionRuntime(_sceneLifecycleRuntime);
            _contentReleaseRuntime = new ContentReleaseRuntime(_sceneLifecycleRuntime);
        }

        internal RouteRuntimeState CurrentRouteState => _currentRouteState;

        internal RouteAsset CurrentRoute => _currentRouteState.Route;

        internal RouteContentSet CurrentRouteContentSet => _currentRouteState.RouteContentSet;

        internal ActivityAsset CurrentActivity => _activityFlowRuntime.CurrentActivity;

        internal bool HasActiveRoute => CurrentRoute != null;

        internal bool HasActiveActivity => _activityFlowRuntime.HasActiveActivity;

        internal bool IsRouteActive(RouteAsset route)
        {
            return route != null && ReferenceEquals(CurrentRoute, route);
        }

        internal IEventBinding SubscribeRouteEntered(Action<RouteEnteredEvent> handler)
        {
            return _routeEnteredEvents.Subscribe(handler);
        }

        internal IEventBinding SubscribeRouteExited(Action<RouteExitedEvent> handler)
        {
            return _routeExitedEvents.Subscribe(handler);
        }

        internal bool IsActivityActive(ActivityAsset activity)
        {
            return _activityFlowRuntime.IsActivityActive(activity);
        }

        internal void SetActivityContentExecutionParticipantSource(IActivityContentExecutionParticipantSource participantSource)
        {
            _activityFlowRuntime.SetActivityContentExecutionParticipantSource(participantSource);
        }

        internal void SetCycleResetParticipantSource(ICycleResetParticipantSource participantSource)
        {
            _cycleResetParticipantSource = participantSource ?? EmptyCycleResetParticipantSource.Instance;
        }

        internal async Task<RouteLifecycleStartResult> StartRouteAsync(
            RouteAsset route,
            string source,
            string reason)
        {
            if (route == null)
            {
                return RouteLifecycleStartResult.Failed("Route is missing.");
            }

            if (!route.HasPrimaryScene)
            {
                return RouteLifecycleStartResult.Failed("Route Primary Scene is missing.");
            }

            var previousRouteState = _currentRouteState;
            var previousRoute = previousRouteState.Route;
            var routeContentExitResult = _routeContentRuntime.ExitRouteContent(previousRoute, route, source, reason);
            var releasePlan = previousRouteState.HasRouteContent
                ? previousRouteState.RouteContentSet.CreateReleasePlan(source, reason)
                : ContentReleasePlan.Empty(
                    FrameworkContentScope.Route,
                    string.Empty,
                    previousRoute != null ? previousRoute.RouteName : string.Empty,
                    source,
                    reason,
                    "No previous Route content is active; release plan is empty.");
            var releaseResult = await _contentReleaseRuntime.ExecuteAsync(releasePlan);
            if (releaseResult.Failed || releaseResult.HasBlockingIssues)
            {
                return RouteLifecycleStartResult.Failed(releaseResult.ToDiagnosticString());
            }

            var routeContentPlan = RouteContentMaterializationPlan.FromRoute(route);
            var routeSceneCompositionPlan = RouteSceneCompositionPlan.FromRoute(route, source, reason);
            var routeSceneCompositionResult = await _routeSceneCompositionRuntime.ExecuteAsync(routeSceneCompositionPlan);
            if (routeSceneCompositionResult.Failed || routeSceneCompositionResult.HasBlockingIssues)
            {
                return RouteLifecycleStartResult.Failed(routeSceneCompositionResult.ToDiagnosticString());
            }

            var runtimeRouteEnterResult = CreateRouteScopeRoot(route, source, reason);
            var sceneLifecycleResult = routeSceneCompositionResult.PrimarySceneLoadResult;
            var routeContentSet = RouteContentSet.FromSceneCompositionResult(
                route,
                routeContentPlan,
                routeSceneCompositionResult,
                source,
                reason);
            var contentAnchorDiscoveryResult = _contentAnchorDiscoveryRuntime.DiscoverRouteAnchors(
                route,
                routeSceneCompositionResult,
                source,
                reason);

            var routeContentEnterResult = _routeContentRuntime.EnterRouteContent(route, previousRoute, source, reason);

            var activityFlowResult = await _activityFlowRuntime.StartStartupActivityAsync(route, source, reason);
            if (!activityFlowResult.Completed)
            {
                return RouteLifecycleStartResult.Failed(activityFlowResult.Message);
            }

            var routeBindingCleanupResult = CleanupPreviousRouteContentAnchorBindings(previousRoute, route, source, reason);
            var runtimeRouteExitResult = RemovePreviousRouteScopeRoot(previousRoute, route, source, reason);
            var runtimeRouteScopeResult = MergeRouteScopeResults(
                runtimeRouteEnterResult,
                runtimeRouteExitResult,
                route,
                previousRoute,
                source,
                reason);

            var result = RouteLifecycleStartResult.StartedWith(
                route,
                previousRouteState,
                sceneLifecycleResult,
                routeSceneCompositionResult,
                routeContentSet,
                contentAnchorDiscoveryResult,
                routeContentEnterResult,
                routeContentExitResult,
                releaseResult,
                activityFlowResult,
                source,
                reason,
                runtimeRouteScopeResult,
                routeBindingCleanupResult);
            _currentRouteState = result.RouteState;
            PublishRouteTransition(previousRoute, route, source, reason);
            return result;
        }

        private void PublishRouteTransition(
            RouteAsset previousRoute,
            RouteAsset nextRoute,
            string source,
            string reason)
        {
            if (previousRoute != null && !ReferenceEquals(previousRoute, nextRoute))
            {
                _routeExitedEvents.Publish(new RouteExitedEvent(previousRoute, nextRoute, source, reason));
            }

            if (nextRoute != null && !ReferenceEquals(previousRoute, nextRoute))
            {
                _routeEnteredEvents.Publish(new RouteEnteredEvent(nextRoute, previousRoute, source, reason));
            }
        }

        internal Task<CycleResetResult> RequestRouteCycleResetAsync(
            CycleResetPolicy policy,
            string source,
            string reason)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(CreateRejectedCycleResetResult(
                    CycleResetScope.Route,
                    "Cycle Reset Request failed. No active Route is available.",
                    source,
                    reason));
            }

            var resolvedPolicy = policy.IsValid ? policy : CycleResetPolicy.RouteDefault();
            var request = CycleResetRequest.Route(CurrentRoute, CurrentActivity, resolvedPolicy, source, reason);
            return Task.FromResult(ExecuteCycleResetRequest(request, source, reason));
        }

        internal Task<CycleResetResult> RequestActivityCycleResetAsync(
            CycleResetPolicy policy,
            string source,
            string reason)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(CreateRejectedCycleResetResult(
                    CycleResetScope.Activity,
                    "Cycle Reset Request failed. No active Route is available.",
                    source,
                    reason));
            }

            if (CurrentActivity == null)
            {
                return Task.FromResult(CreateRejectedCycleResetResult(
                    CycleResetScope.Activity,
                    "Cycle Reset Request failed. No active Activity is available.",
                    source,
                    reason));
            }

            var resolvedPolicy = policy.IsValid ? policy : CycleResetPolicy.ActivityDefault();
            var request = CycleResetRequest.Activity(CurrentRoute, CurrentActivity, source, reason);
            if (resolvedPolicy != CycleResetPolicy.ActivityDefault())
            {
                request = new CycleResetRequest(
                    CycleResetScope.Activity,
                    CurrentRoute,
                    CurrentActivity,
                    resolvedPolicy,
                    source,
                    reason);
            }

            return Task.FromResult(ExecuteCycleResetRequest(request, source, reason));
        }

        private CycleResetResult ExecuteCycleResetRequest(CycleResetRequest request, string source, string reason)
        {
            IReadOnlyList<ICycleResetParticipant> participants;
            try
            {
                participants = _cycleResetParticipantSource.ResolveCycleResetParticipants(request);
            }
            catch (Exception exception)
            {
                return CycleResetResult.RejectedInvalidRequest(
                    request,
                    new[]
                    {
                        CycleResetIssue.BlockingIssue(
                            CycleResetIssueKind.ParticipantSourceException,
                            default,
                            request.Scope,
                            $"Cycle Reset participant source threw an exception: {exception.GetType().Name}.")
                    },
                    source,
                    reason,
                    "Cycle Reset Request failed because the participant source threw an exception.");
            }

            var plan = _cycleResetRuntime.CreatePlan(request, participants, source, reason);
            return _cycleResetRuntime.ExecutePlan(plan, source, reason);
        }

        private static CycleResetResult CreateRejectedCycleResetResult(
            CycleResetScope requestedScope,
            string message,
            string source,
            string reason)
        {
            return CycleResetResult.RejectedInvalidRequest(
                default,
                new[]
                {
                    CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.InvalidRequest,
                        default,
                        requestedScope,
                        message)
                },
                source,
                reason,
                message);
        }

        internal Task<ActivityFlowStartResult> StartActivityAsync(
            ActivityAsset activity,
            string source,
            string reason)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return _activityFlowRuntime.StartActivityAsync(activity, CurrentRoute, source, reason);
        }

        internal Task<ActivityFlowStartResult> ClearActivityAsync(string source, string reason)
        {
            if (CurrentRoute == null)
            {
                return Task.FromResult(ActivityFlowStartResult.Failed("No active Route is available."));
            }

            return _activityFlowRuntime.ClearActivityAsync(CurrentRoute, source, reason);
        }

        private RuntimeScopeLifecycleResult CreateRouteScopeRoot(RouteAsset route, string source, string reason)
        {
            if (route == null)
            {
                return RuntimeScopeLifecycleResult.None(RuntimeContentScope.Route, source, reason);
            }

            var owner = CreateRouteOwner(route);
            var enterResult = _runtimeContentRuntime.CreateScopeRoot(owner, source, reason);
            _runtimeContentRuntime.TryCreateScopeContext(owner, source, reason, out var context);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Route,
                owner,
                enterResult,
                null,
                context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private ContentAnchorBindingLifecycleResult CleanupPreviousRouteContentAnchorBindings(RouteAsset previousRoute, RouteAsset nextRoute, string source, string reason)
        {
            if (previousRoute == null || ReferenceEquals(previousRoute, nextRoute))
            {
                return default(ContentAnchorBindingLifecycleResult);
            }

            var owner = CreateRouteOwner(previousRoute);
            if (nextRoute != null && owner == CreateRouteOwner(nextRoute))
            {
                return default(ContentAnchorBindingLifecycleResult);
            }

            return _contentAnchorBindingRuntime.UnbindRuntimeOwner(owner, source, reason);
        }

        private RuntimeScopeLifecycleResult RemovePreviousRouteScopeRoot(RouteAsset previousRoute, RouteAsset nextRoute, string source, string reason)
        {
            if (previousRoute == null || ReferenceEquals(previousRoute, nextRoute))
            {
                return RuntimeScopeLifecycleResult.None(RuntimeContentScope.Route, source, reason);
            }

            var owner = CreateRouteOwner(previousRoute);
            if (nextRoute != null && owner == CreateRouteOwner(nextRoute))
            {
                return RuntimeScopeLifecycleResult.None(RuntimeContentScope.Route, source, reason);
            }

            var exitResult = _runtimeContentRuntime.RemoveScopeRoot(owner, source, reason);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Route,
                owner,
                null,
                exitResult,
                default(RuntimeScopeContext),
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private RuntimeScopeLifecycleResult MergeRouteScopeResults(
            RuntimeScopeLifecycleResult enterResult,
            RuntimeScopeLifecycleResult exitResult,
            RouteAsset nextRoute,
            RouteAsset previousRoute,
            string source,
            string reason)
        {
            var owner = nextRoute != null
                ? CreateRouteOwner(nextRoute)
                : previousRoute != null ? CreateRouteOwner(previousRoute) : default(RuntimeContentOwner);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Route,
                owner,
                enterResult.EnterRootResult,
                exitResult.ExitRootResult,
                enterResult.Context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
        }

        private static RuntimeContentOwner CreateRouteOwner(RouteAsset route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            return RuntimeContentOwner.Route(route.PrimaryScenePath, route.RouteName);
        }
    }
}

using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.ActivityFlow;
using System;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.CycleReset;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Minimal owner for the game-flow route handoff.
    /// It accepts route and activity requests and delegates route startup to Route Lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class GameFlowRuntime
    {
        private readonly RouteLifecycleRuntime _routeLifecycleRuntime;
        private bool _routeRequestInFlight;
        private bool _activityRequestInFlight;
        private bool _cycleResetRequestInFlight;

        internal GameFlowRuntime(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentAnchorBinding contentAnchorBindingRuntime)
        {
            _routeLifecycleRuntime = new RouteLifecycleRuntime(
                runtimeContentRuntime ?? throw new ArgumentNullException(nameof(runtimeContentRuntime)),
                contentAnchorBindingRuntime ?? throw new ArgumentNullException(nameof(contentAnchorBindingRuntime)));
        }

        internal void SetActivityContentExecutionParticipantSource(IActivityContentExecutionParticipantSource participantSource)
        {
            _routeLifecycleRuntime.SetActivityContentExecutionParticipantSource(participantSource);
        }

        internal void SetCycleResetParticipantSource(ICycleResetParticipantSource participantSource)
        {
            _routeLifecycleRuntime.SetCycleResetParticipantSource(participantSource);
        }

        internal async Task<FrameworkGameFlowStartResult> StartAsync(GameApplicationAsset gameApplication)
        {
            if (gameApplication == null)
            {
                return FrameworkGameFlowStartResult.Failed("Game Application is missing.");
            }

            var startupRoute = gameApplication.StartupRoute;
            if (startupRoute == null)
            {
                return FrameworkGameFlowStartResult.Failed("Startup Route is missing.");
            }

            if (!startupRoute.HasPrimaryScene)
            {
                return FrameworkGameFlowStartResult.Failed("Startup Route Primary Scene is missing.");
            }

            var routeLifecycleResult = await StartRouteCoreAsync(startupRoute, "GameApplication", "startup");
            if (!routeLifecycleResult.Started)
            {
                return FrameworkGameFlowStartResult.Failed(routeLifecycleResult.Message);
            }

            return FrameworkGameFlowStartResult.StartedWith(startupRoute, routeLifecycleResult);
        }

        internal async Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            string resolvedSource = FrameworkRouteRequestResult.NormalizeSource(source);
            string resolvedReason = FrameworkRouteRequestResult.NormalizeReason(reason);

            if (targetRoute == null)
            {
                return FrameworkRouteRequestResult.FailedInvalidConfig(
                    "Route Request failed. Target Route is missing.",
                    null,
                    resolvedSource,
                    resolvedReason);
            }

            if (!targetRoute.HasPrimaryScene)
            {
                return FrameworkRouteRequestResult.FailedInvalidConfig(
                    $"Route Request failed. Target Route '{targetRoute.RouteName}' has no Primary Scene.",
                    targetRoute,
                    resolvedSource,
                    resolvedReason);
            }

            if (_routeRequestInFlight)
            {
                return FrameworkRouteRequestResult.IgnoredAlreadyInFlight(targetRoute, resolvedSource, resolvedReason);
            }

            if (_activityRequestInFlight)
            {
                return FrameworkRouteRequestResult.IgnoredAlreadyInFlight(targetRoute, resolvedSource, resolvedReason);
            }

            if (_cycleResetRequestInFlight)
            {
                return FrameworkRouteRequestResult.IgnoredAlreadyInFlight(targetRoute, resolvedSource, resolvedReason);
            }

            if (_routeLifecycleRuntime.IsRouteActive(targetRoute))
            {
                return FrameworkRouteRequestResult.IgnoredAlreadyActive(targetRoute, resolvedSource, resolvedReason);
            }

            _routeRequestInFlight = true;
            try
            {
                var routeLifecycleResult = await StartRouteCoreAsync(targetRoute, resolvedSource, resolvedReason);
                if (!routeLifecycleResult.Started)
                {
                    return FrameworkRouteRequestResult.FailedInvalidConfig(
                        routeLifecycleResult.Message,
                        targetRoute,
                        resolvedSource,
                        resolvedReason);
                }

                return FrameworkRouteRequestResult.SucceededWith(
                    targetRoute,
                    resolvedSource,
                    resolvedReason,
                    routeLifecycleResult);
            }
            finally
            {
                _routeRequestInFlight = false;
            }
        }

        internal async Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            string resolvedSource = FrameworkActivityRequestResult.NormalizeSource(source);
            string resolvedReason = FrameworkActivityRequestResult.NormalizeReason(reason);

            if (targetActivity == null)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Request failed. Target Activity is missing.",
                    null,
                    resolvedSource,
                    resolvedReason);
            }

            if (!_routeLifecycleRuntime.HasActiveRoute)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Request failed. No active Route is available.",
                    targetActivity,
                    resolvedSource,
                    resolvedReason);
            }

            if (_routeRequestInFlight || _activityRequestInFlight || _cycleResetRequestInFlight)
            {
                return FrameworkActivityRequestResult.IgnoredAlreadyInFlight(targetActivity, resolvedSource, resolvedReason);
            }

            if (_routeLifecycleRuntime.IsActivityActive(targetActivity))
            {
                return FrameworkActivityRequestResult.IgnoredAlreadyActive(targetActivity, resolvedSource, resolvedReason);
            }

            _activityRequestInFlight = true;
            try
            {
                var activityFlowResult = await _routeLifecycleRuntime.StartActivityAsync(targetActivity, resolvedSource, resolvedReason);
                if (!activityFlowResult.Completed)
                {
                    return FrameworkActivityRequestResult.FailedInvalidConfig(
                        activityFlowResult.Message,
                        targetActivity,
                        resolvedSource,
                        resolvedReason);
                }

                return FrameworkActivityRequestResult.SucceededWith(
                    targetActivity,
                    resolvedSource,
                    resolvedReason,
                    activityFlowResult);
            }
            finally
            {
                _activityRequestInFlight = false;
            }
        }

        internal async Task<FrameworkActivityRequestResult> ClearActivityAsync(string source, string reason)
        {
            string resolvedSource = FrameworkActivityRequestResult.NormalizeSource(source);
            string resolvedReason = FrameworkActivityRequestResult.NormalizeReason(reason);

            if (!_routeLifecycleRuntime.HasActiveRoute)
            {
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "Activity Request failed. No active Route is available.",
                    null,
                    resolvedSource,
                    resolvedReason);
            }

            if (_routeRequestInFlight || _activityRequestInFlight || _cycleResetRequestInFlight)
            {
                return FrameworkActivityRequestResult.IgnoredAlreadyInFlight(null, resolvedSource, resolvedReason);
            }

            if (!_routeLifecycleRuntime.HasActiveActivity)
            {
                return FrameworkActivityRequestResult.IgnoredNoActiveActivity(resolvedSource, resolvedReason);
            }

            _activityRequestInFlight = true;
            try
            {
                var activityFlowResult = await _routeLifecycleRuntime.ClearActivityAsync(resolvedSource, resolvedReason);
                if (!activityFlowResult.Completed)
                {
                    return FrameworkActivityRequestResult.FailedInvalidConfig(
                        activityFlowResult.Message,
                        null,
                        resolvedSource,
                        resolvedReason);
                }

                return FrameworkActivityRequestResult.SucceededWith(
                    null,
                    resolvedSource,
                    resolvedReason,
                    activityFlowResult);
            }
            finally
            {
                _activityRequestInFlight = false;
            }
        }

        internal async Task<CycleResetResult> RequestRouteCycleResetAsync(string source, string reason)
        {
            return await RequestCycleResetAsync(CycleResetScope.Route, CycleResetPolicy.RouteDefault(), source, reason);
        }

        internal async Task<CycleResetResult> RequestActivityCycleResetAsync(string source, string reason)
        {
            return await RequestCycleResetAsync(CycleResetScope.Activity, CycleResetPolicy.ActivityDefault(), source, reason);
        }

        internal async Task<CycleResetResult> RequestCycleResetAsync(
            CycleResetScope scope,
            CycleResetPolicy policy,
            string source,
            string reason)
        {
            if (_routeRequestInFlight || _activityRequestInFlight || _cycleResetRequestInFlight)
            {
                return CycleResetResult.RejectedInvalidRequest(
                    default,
                    new[]
                    {
                        CycleResetIssue.BlockingIssue(
                            CycleResetIssueKind.RequestAlreadyInFlight,
                            default,
                            scope,
                            "Cycle Reset Request ignored because another framework lifecycle request is already in flight.")
                    },
                    source,
                    reason,
                    "Cycle Reset Request ignored because another framework lifecycle request is already in flight.");
            }

            _cycleResetRequestInFlight = true;
            try
            {
                if (scope == CycleResetScope.Route)
                {
                    return await _routeLifecycleRuntime.RequestRouteCycleResetAsync(policy, source, reason);
                }

                if (scope == CycleResetScope.Activity)
                {
                    return await _routeLifecycleRuntime.RequestActivityCycleResetAsync(policy, source, reason);
                }

                return CycleResetResult.RejectedInvalidRequest(
                    default,
                    new[]
                    {
                        CycleResetIssue.BlockingIssue(
                            CycleResetIssueKind.InvalidRequest,
                            default,
                            scope,
                            "Cycle Reset Request failed because scope is invalid.")
                    },
                    source,
                    reason,
                    "Cycle Reset Request failed because scope is invalid.");
            }
            finally
            {
                _cycleResetRequestInFlight = false;
            }
        }

        private Task<RouteLifecycleStartResult> StartRouteCoreAsync(RouteAsset route, string source, string reason)
        {
            return _routeLifecycleRuntime.StartRouteAsync(route, source, reason);
        }
    }
}

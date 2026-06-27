using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Immutable result for a completed runtime route request.
    /// This is diagnostics data and does not expose a service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct FrameworkRouteRequestResult
    {
        public FrameworkRouteRequestResult(
            FrameworkRouteRequestKind kind,
            string message,
            RouteAsset targetRoute,
            string source,
            string reason,
            RouteLifecycleStartResult routeLifecycleResult,
            FrameworkTransitionDiagnostics transitionDiagnostics = default)
        {
            Kind = kind;
            Message = message ?? string.Empty;
            TargetRoute = targetRoute;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            RouteLifecycleResult = routeLifecycleResult;
            TransitionDiagnostics = transitionDiagnostics;
        }

        public FrameworkRouteRequestKind Kind { get; }

        public string Message { get; }

        public RouteAsset TargetRoute { get; }

        public string Source { get; }

        public string Reason { get; }

        internal RouteLifecycleStartResult RouteLifecycleResult { get; }

        internal FrameworkTransitionDiagnostics TransitionDiagnostics { get; }

        public bool Succeeded => Kind == FrameworkRouteRequestKind.Succeeded;

        public static FrameworkRouteRequestResult FailedInvalidConfig(
            string message,
            RouteAsset targetRoute = null,
            string source = null,
            string reason = null)
        {
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.FailedInvalidConfig,
                message,
                targetRoute,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        public static FrameworkRouteRequestResult FailedRuntimeUnavailable(
            string message,
            RouteAsset targetRoute = null,
            string source = null,
            string reason = null)
        {
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.FailedRuntimeUnavailable,
                message,
                targetRoute,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        public static FrameworkRouteRequestResult IgnoredAlreadyActive(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.IgnoredAlreadyActive,
                $"Route Request ignored. {FormatRequestContext(source, reason)} Route '{targetRoute.RouteName}' is already active.",
                targetRoute,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        public static FrameworkRouteRequestResult IgnoredAlreadyInFlight(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            string routeName = targetRoute != null ? targetRoute.RouteName : "<missing>";
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.IgnoredAlreadyInFlight,
                $"Route Request ignored. {FormatRequestContext(source, reason)} Another route request is already in flight. targetRoute='{routeName}'.",
                targetRoute,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        internal static FrameworkRouteRequestResult IgnoredBlockedByGate(
            RouteAsset targetRoute,
            string source,
            string reason,
            GateEvaluationResult gateEvaluation)
        {
            string routeName = targetRoute != null ? targetRoute.RouteName : "<missing>";
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.IgnoredAlreadyInFlight,
                $"Route Request ignored. {FormatRequestContext(source, reason)} targetRoute='{routeName}'. {GateRequestAdmission.FormatBlockedMessage("Route Request", gateEvaluation)}",
                targetRoute,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        internal static FrameworkRouteRequestResult SucceededWith(
            RouteAsset targetRoute,
            string source,
            string reason,
            RouteLifecycleStartResult routeLifecycleResult,
            FrameworkTransitionDiagnostics transitionDiagnostics = default)
        {
            return new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.Succeeded,
                $"Route Request completed. {FormatRequestContext(source, reason)} {routeLifecycleResult.Message}",
                targetRoute,
                NormalizeSource(source),
                NormalizeReason(reason),
                routeLifecycleResult,
                transitionDiagnostics);
        }

        internal static string NormalizeSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
        }

        internal static string NormalizeReason(string reason)
        {
            return string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }

        private static string FormatRequestContext(string source, string reason)
        {
            return $"source='{NormalizeSource(source)}' reason='{NormalizeReason(reason)}'.";
        }
    }
}

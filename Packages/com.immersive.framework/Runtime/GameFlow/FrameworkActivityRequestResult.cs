using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Immutable result for a completed runtime activity request.
    /// This is diagnostics data and does not expose a service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct FrameworkActivityRequestResult
    {
        public FrameworkActivityRequestResult(
            FrameworkActivityRequestKind kind,
            string message,
            ActivityAsset targetActivity,
            string source,
            string reason,
            ActivityFlowStartResult activityFlowResult)
        {
            Kind = kind;
            Message = message ?? string.Empty;
            TargetActivity = targetActivity;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            ActivityFlowResult = activityFlowResult;
        }

        public FrameworkActivityRequestKind Kind { get; }

        public string Message { get; }

        public ActivityAsset TargetActivity { get; }

        public string Source { get; }

        public string Reason { get; }

        internal ActivityFlowStartResult ActivityFlowResult { get; }

        public bool Succeeded => Kind == FrameworkActivityRequestKind.Succeeded;

        public static FrameworkActivityRequestResult FailedInvalidConfig(
            string message,
            ActivityAsset targetActivity = null,
            string source = null,
            string reason = null)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.FailedInvalidConfig,
                message,
                targetActivity,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        public static FrameworkActivityRequestResult FailedRuntimeUnavailable(
            string message,
            ActivityAsset targetActivity = null,
            string source = null,
            string reason = null)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.FailedRuntimeUnavailable,
                message,
                targetActivity,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        public static FrameworkActivityRequestResult IgnoredAlreadyActive(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.IgnoredAlreadyActive,
                $"Activity Request ignored. {FormatRequestContext(source, reason)} Activity '{targetActivity.ActivityName}' is already active.",
                targetActivity,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        public static FrameworkActivityRequestResult IgnoredAlreadyInFlight(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            string activityName = targetActivity != null ? targetActivity.ActivityName : "<none>";
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.IgnoredAlreadyInFlight,
                $"Activity Request ignored. {FormatRequestContext(source, reason)} Another activity or route request is already in flight. targetActivity='{activityName}'.",
                targetActivity,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        public static FrameworkActivityRequestResult IgnoredNoActiveActivity(
            string source,
            string reason)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.IgnoredNoActiveActivity,
                $"Activity Request ignored. {FormatRequestContext(source, reason)} No Activity is active.",
                null,
                NormalizeSource(source),
                NormalizeReason(reason),
                default);
        }

        internal static FrameworkActivityRequestResult SucceededWith(
            ActivityAsset targetActivity,
            string source,
            string reason,
            ActivityFlowStartResult activityFlowResult)
        {
            return new FrameworkActivityRequestResult(
                FrameworkActivityRequestKind.Succeeded,
                $"Activity Request completed. {FormatRequestContext(source, reason)} {activityFlowResult.Message}",
                targetActivity,
                NormalizeSource(source),
                NormalizeReason(reason),
                activityFlowResult);
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

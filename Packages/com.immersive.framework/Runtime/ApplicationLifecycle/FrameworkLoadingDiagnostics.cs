using Immersive.Framework.ApiStatus;
using Immersive.Framework.Loading;

namespace Immersive.Framework.ApplicationLifecycle
{
    /// <summary>
    /// API status: Internal. Route/Activity request-level loading diagnostics for logging and smoke validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24D request-level Loading diagnostics.")]
    internal readonly struct FrameworkLoadingDiagnostics
    {
        private const string IndeterminateProgressText = "Indeterminate";
        private const string UnitySurfaceText = "UnitySurface";
        private const string NoneText = "None";
        private const string SkippedText = "Skipped";

        public FrameworkLoadingDiagnostics(
            string loadingText,
            string visualText,
            string beforeText,
            string afterText,
            string progressText,
            int blockingIssueCount,
            int adapterCount,
            bool progressSupported)
        {
            LoadingText = Normalize(loadingText);
            VisualText = Normalize(visualText);
            BeforeText = Normalize(beforeText);
            AfterText = Normalize(afterText);
            ProgressText = Normalize(progressText);
            BlockingIssueCount = blockingIssueCount < 0 ? 0 : blockingIssueCount;
            AdapterCount = adapterCount < 0 ? 0 : adapterCount;
            ProgressSupported = progressSupported;
        }

        public string LoadingText { get; }

        public string VisualText { get; }

        public string BeforeText { get; }

        public string AfterText { get; }

        public string ProgressText { get; }

        public int BlockingIssueCount { get; }

        public int AdapterCount { get; }

        public bool ProgressSupported { get; }

        public bool HasDiagnostics => !string.IsNullOrWhiteSpace(LoadingText);

        public static FrameworkLoadingDiagnostics SkippedAlreadyLoaded()
        {
            return new FrameworkLoadingDiagnostics(
                "SkippedAlreadyLoaded",
                NoneText,
                SkippedText,
                SkippedText,
                IndeterminateProgressText,
                0,
                0,
                false);
        }

        public static FrameworkLoadingDiagnostics SkippedNoSceneLoad()
        {
            return new FrameworkLoadingDiagnostics(
                "SkippedNoSceneLoad",
                NoneText,
                SkippedText,
                SkippedText,
                IndeterminateProgressText,
                0,
                0,
                false);
        }

        public static FrameworkLoadingDiagnostics SucceededWithNoOp()
        {
            return new FrameworkLoadingDiagnostics(
                "SucceededWithNoOp",
                NoneText,
                SkippedText,
                SkippedText,
                IndeterminateProgressText,
                0,
                0,
                false);
        }

        public static FrameworkLoadingDiagnostics FailedRequiredUnitySurfaceMissing()
        {
            return new FrameworkLoadingDiagnostics(
                "FailedRequiredUnitySurfaceMissing",
                NoneText,
                "Failed",
                "Failed",
                IndeterminateProgressText,
                1,
                0,
                false);
        }

        public static FrameworkLoadingDiagnostics FromUnitySurface(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult,
            int adapterCount,
            bool progressSupported)
        {
            var blockingIssueCount = beforeResult.BlockingIssueCount + afterResult.BlockingIssueCount;
            var beforeText = StatusText(beforeResult);
            var afterText = StatusText(afterResult);
            var loadingText = DetermineSurfaceLoadingText(beforeResult, afterResult);
            return new FrameworkLoadingDiagnostics(
                loadingText,
                UnitySurfaceText,
                beforeText,
                afterText,
                progressSupported ? "Supported" : IndeterminateProgressText,
                blockingIssueCount,
                adapterCount,
                progressSupported);
        }

        private static string DetermineSurfaceLoadingText(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult)
        {
            if (beforeResult.Failed || beforeResult.Rejected || afterResult.Failed || afterResult.Rejected)
            {
                return "FailedUnitySurface";
            }

            if (beforeResult.SucceededWithWarnings || afterResult.SucceededWithWarnings)
            {
                return "CompletedWithUnitySurfaceWarnings";
            }

            if (beforeResult.Skipped && afterResult.Skipped)
            {
                return "SucceededWithUnitySurface";
            }

            return "SucceededWithUnitySurface";
        }

        private static string StatusText(LoadingSurfaceResult result)
        {
            if (result.Succeeded)
            {
                return "Succeeded";
            }

            if (result.SucceededWithWarnings)
            {
                return "SucceededWithWarnings";
            }

            if (result.Skipped)
            {
                return "Skipped";
            }

            if (result.Rejected)
            {
                return "Rejected";
            }

            if (result.Failed)
            {
                return "Failed";
            }

            return "Unknown";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

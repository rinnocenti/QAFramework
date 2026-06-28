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
        private const string AlreadyLoadedPhase = "AlreadyLoaded";
        private const string NoSceneLoadPhase = "NoSceneLoad";
        private const string ActivityPolicyPhase = "ActivityPolicy";
        private const string NoOpPhase = "NoOp";
        private const string RequiredSurfacePhase = "RequiredSurface";
        private const string UnitySurfaceText = "UnitySurface";
        private const string NoneText = "None";
        private const string SkippedText = "Skipped";

        public FrameworkLoadingDiagnostics(
            string loadingText,
            string visualText,
            string beforeText,
            string afterText,
            FrameworkLoadingProgress progress,
            int blockingIssueCount,
            int adapterCount)
        {
            LoadingText = Normalize(loadingText);
            VisualText = Normalize(visualText);
            BeforeText = Normalize(beforeText);
            AfterText = Normalize(afterText);
            Progress = progress;
            BlockingIssueCount = blockingIssueCount < 0 ? 0 : blockingIssueCount;
            AdapterCount = adapterCount < 0 ? 0 : adapterCount;
        }

        public string LoadingText { get; }

        public string VisualText { get; }

        public string BeforeText { get; }

        public string AfterText { get; }

        public FrameworkLoadingProgress Progress { get; }

        public int BlockingIssueCount { get; }

        public int AdapterCount { get; }

        public bool ProgressSupported => Progress.Supported;

        public string ProgressModeText => Progress.ModeText;

        public string ProgressValueText => Progress.ValueText;

        public string ProgressPercentText => Progress.PercentText;

        public string ProgressPhaseText => Progress.PhaseText;

        public string ProgressMessageText => Progress.MessageText;

        public string ProgressText => ProgressModeText;

        public bool HasDiagnostics => !string.IsNullOrWhiteSpace(LoadingText);

        public static FrameworkLoadingDiagnostics SkippedAlreadyLoaded()
        {
            return new FrameworkLoadingDiagnostics(
                "SkippedAlreadyLoaded",
                NoneText,
                SkippedText,
                SkippedText,
                FrameworkLoadingProgress.Unsupported(
                    AlreadyLoadedPhase,
                    "Loading was skipped because the target is already active."),
                0,
                0);
        }

        public static FrameworkLoadingDiagnostics SkippedNoSceneLoad()
        {
            return new FrameworkLoadingDiagnostics(
                "SkippedNoSceneLoad",
                NoneText,
                SkippedText,
                SkippedText,
                FrameworkLoadingProgress.Unsupported(
                    NoSceneLoadPhase,
                    "No loading progress source was available because no scene load or release side-effect occurred."),
                0,
                0);
        }

        public static FrameworkLoadingDiagnostics SkippedByActivityPolicy()
        {
            return new FrameworkLoadingDiagnostics(
                "SkippedByActivityPolicy",
                NoneText,
                SkippedText,
                SkippedText,
                FrameworkLoadingProgress.Unsupported(
                    ActivityPolicyPhase,
                    "Activity loading presentation was skipped by the authored visual policy."),
                0,
                0);
        }

        public static FrameworkLoadingDiagnostics SucceededWithNoOp()
        {
            return new FrameworkLoadingDiagnostics(
                "SucceededWithNoOp",
                NoneText,
                SkippedText,
                SkippedText,
                FrameworkLoadingProgress.Unsupported(
                    NoOpPhase,
                    "Loading completed without an executable loading surface."),
                0,
                0);
        }

        public static FrameworkLoadingDiagnostics FailedRequiredUnitySurfaceMissing()
        {
            return new FrameworkLoadingDiagnostics(
                "FailedRequiredUnitySurfaceMissing",
                NoneText,
                "Failed",
                "Failed",
                FrameworkLoadingProgress.Unsupported(
                    RequiredSurfacePhase,
                    "Required loading surface configuration is missing."),
                1,
                0);
        }

        public static FrameworkLoadingDiagnostics FromUnitySurface(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult,
            int adapterCount,
            bool progressSupported)
        {
            return FromUnitySurface(
                beforeResult,
                afterResult,
                adapterCount,
                progressSupported,
                FrameworkLoadingProgress.Indeterminate(
                    progressSupported,
                    UnitySurfaceText,
                    "Loading progress support is available but no determinate source is wired for this operation."));
        }

        public static FrameworkLoadingDiagnostics FromUnitySurface(
            LoadingSurfaceResult beforeResult,
            LoadingSurfaceResult afterResult,
            int adapterCount,
            bool progressSupported,
            FrameworkLoadingProgress observedProgress)
        {
            var blockingIssueCount = beforeResult.BlockingIssueCount + afterResult.BlockingIssueCount;
            var beforeText = StatusText(beforeResult);
            var afterText = StatusText(afterResult);
            var loadingText = DetermineSurfaceLoadingText(beforeResult, afterResult);
            var progress = ResolveObservedProgress(progressSupported, observedProgress);
            return new FrameworkLoadingDiagnostics(
                loadingText,
                UnitySurfaceText,
                beforeText,
                afterText,
                progress,
                blockingIssueCount,
                adapterCount);
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

        private static FrameworkLoadingProgress ResolveObservedProgress(
            bool progressSupported,
            FrameworkLoadingProgress observedProgress)
        {
            if (!progressSupported)
            {
                return FrameworkLoadingProgress.Unsupported(
                    UnitySurfaceText,
                    "Loading surface adapters do not expose progress for this operation.");
            }

            if (observedProgress.Supported && observedProgress.IsDeterminate)
            {
                return observedProgress;
            }

            return FrameworkLoadingProgress.Indeterminate(
                true,
                UnitySurfaceText,
                "Loading progress support is available but no determinate source reported progress for this operation.");
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

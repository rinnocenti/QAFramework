using Immersive.Framework.ApiStatus;
using UnityEngine;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Internal. Runtime bridge used by lifecycle owners to publish determinate progress
    /// into the configured loading surface. It does not execute scene operations and does not own UI state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26D determinate loading progress reporter bridge.")]
    internal interface IFrameworkLoadingProgressReporter
    {
        bool IsEnabled { get; }

        bool HasReportedProgress { get; }

        FrameworkLoadingProgress LastProgress { get; }

        Awaitable ReportAsync(FrameworkLoadingProgress progress);
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26D no-op loading progress reporter.")]
    internal sealed class NoOpFrameworkLoadingProgressReporter : IFrameworkLoadingProgressReporter
    {
        internal static readonly NoOpFrameworkLoadingProgressReporter Instance = new NoOpFrameworkLoadingProgressReporter();

        private static readonly FrameworkLoadingProgress NoProgress = FrameworkLoadingProgress.Unsupported(
            "NoOp",
            "No loading progress reporter is active for this operation.");

        private NoOpFrameworkLoadingProgressReporter()
        {
        }

        public bool IsEnabled => false;

        public bool HasReportedProgress => false;

        public FrameworkLoadingProgress LastProgress => NoProgress;

        public async Awaitable ReportAsync(FrameworkLoadingProgress progress)
        {
            await Awaitable.NextFrameAsync();
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26D loading surface progress reporter.")]
    internal sealed class LoadingSurfaceProgressReporter : IFrameworkLoadingProgressReporter
    {
        private readonly LoadingSurfaceRuntime loadingSurfaceRuntime;
        private readonly string title;
        private readonly string detail;
        private readonly string source;
        private readonly string reason;
        private FrameworkLoadingProgress lastProgress;
        private bool hasReportedProgress;

        internal LoadingSurfaceProgressReporter(
            LoadingSurfaceRuntime loadingSurfaceRuntime,
            LoadingSurfaceRequest baseRequest)
        {
            this.loadingSurfaceRuntime = loadingSurfaceRuntime;
            title = baseRequest.Title;
            detail = baseRequest.Detail;
            source = baseRequest.Source;
            reason = baseRequest.Reason;
            lastProgress = FrameworkLoadingProgress.Indeterminate(
                supported: true,
                phase: "UnitySurface",
                message: "Loading surface progress reporter is ready and waiting for a determinate source.");
        }

        public bool IsEnabled => loadingSurfaceRuntime != null
            && loadingSurfaceRuntime.HasVisibleSurface
            && loadingSurfaceRuntime.ProgressSupported;

        public bool HasReportedProgress => hasReportedProgress;

        public FrameworkLoadingProgress LastProgress => lastProgress;

        public async Awaitable ReportAsync(FrameworkLoadingProgress progress)
        {
            if (!IsEnabled)
            {
                return;
            }

            lastProgress = progress;
            hasReportedProgress = true;

            var progressSupported = progress.Supported && progress.IsDeterminate;
            var surfaceProgress = progressSupported
                ? LoadingProgress.FromNormalized(progress.Value01)
                : LoadingProgress.Zero;

            var request = LoadingSurfaceRequest.Update(
                title,
                detail,
                source,
                reason,
                surfaceProgress,
                progressSupported);

            await loadingSurfaceRuntime.UpdateAsync(request);
        }
    }
}

using System;
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

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26E weighted loading progress reporter utilities.")]
    internal static class FrameworkLoadingProgressReporterUtility
    {
        internal static IFrameworkLoadingProgressReporter CreateWeightedStepReporter(
            IFrameworkLoadingProgressReporter parent,
            int stepIndex,
            int stepCount,
            string aggregatePhase,
            string aggregateMessage)
        {
            return CreateWeightedRangeReporter(
                parent,
                stepIndex,
                1,
                stepCount,
                aggregatePhase,
                aggregateMessage);
        }

        internal static IFrameworkLoadingProgressReporter CreateWeightedRangeReporter(
            IFrameworkLoadingProgressReporter parent,
            int startStepIndex,
            int rangeStepCount,
            int totalStepCount,
            string aggregatePhase,
            string aggregateMessage)
        {
            if (parent == null || !parent.IsEnabled || rangeStepCount <= 0 || totalStepCount <= 0)
            {
                return NoOpFrameworkLoadingProgressReporter.Instance;
            }

            if (rangeStepCount >= totalStepCount && startStepIndex <= 0)
            {
                return string.IsNullOrWhiteSpace(aggregatePhase)
                    ? parent
                    : new FrameworkLoadingProgressPhaseReporter(parent, aggregatePhase, aggregateMessage);
            }

            var start01 = Clamp01((float)Math.Max(0, startStepIndex) / totalStepCount);
            var weight01 = Clamp01((float)rangeStepCount / totalStepCount);
            if (weight01 <= 0f)
            {
                return NoOpFrameworkLoadingProgressReporter.Instance;
            }

            return new FrameworkLoadingProgressWeightedReporter(
                parent,
                start01,
                weight01,
                aggregatePhase,
                aggregateMessage);
        }

        internal static async Awaitable ReportCompletedIfAnyAsync(
            IFrameworkLoadingProgressReporter reporter,
            string phase,
            string message)
        {
            if (reporter == null || !reporter.IsEnabled || !reporter.HasReportedProgress)
            {
                return;
            }

            await reporter.ReportAsync(FrameworkLoadingProgress.Determinate(1f, phase, message));
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26E phase-remapping loading progress reporter.")]
    internal sealed class FrameworkLoadingProgressPhaseReporter : IFrameworkLoadingProgressReporter
    {
        private readonly IFrameworkLoadingProgressReporter parent;
        private readonly string phase;
        private readonly string messagePrefix;
        private FrameworkLoadingProgress lastProgress;
        private bool hasReportedProgress;

        internal FrameworkLoadingProgressPhaseReporter(
            IFrameworkLoadingProgressReporter parent,
            string phase,
            string messagePrefix)
        {
            this.parent = parent ?? NoOpFrameworkLoadingProgressReporter.Instance;
            this.phase = NormalizePhase(phase);
            this.messagePrefix = Normalize(messagePrefix);
            lastProgress = parent != null ? parent.LastProgress : FrameworkLoadingProgress.Unsupported("NoOp", "No loading progress reporter is active for this operation.");
        }

        public bool IsEnabled => parent.IsEnabled;

        public bool HasReportedProgress => hasReportedProgress || parent.HasReportedProgress;

        public FrameworkLoadingProgress LastProgress => hasReportedProgress ? lastProgress : parent.LastProgress;

        public async Awaitable ReportAsync(FrameworkLoadingProgress progress)
        {
            if (!IsEnabled)
            {
                return;
            }

            var remapped = progress.Supported && progress.IsDeterminate
                ? FrameworkLoadingProgress.Determinate(
                    progress.Value01,
                    phase,
                    BuildMessage(progress.Message))
                : FrameworkLoadingProgress.Indeterminate(
                    progress.Supported,
                    phase,
                    BuildMessage(progress.Message));

            lastProgress = remapped;
            hasReportedProgress = true;
            await parent.ReportAsync(remapped);
        }

        private string BuildMessage(string childMessage)
        {
            childMessage = Normalize(childMessage);
            if (string.IsNullOrWhiteSpace(messagePrefix))
            {
                return childMessage;
            }

            return string.IsNullOrWhiteSpace(childMessage)
                ? messagePrefix
                : $"{messagePrefix} {childMessage}";
        }

        private static string NormalizePhase(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Loading" : value.Trim();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F26E weighted loading progress reporter.")]
    internal sealed class FrameworkLoadingProgressWeightedReporter : IFrameworkLoadingProgressReporter
    {
        private readonly IFrameworkLoadingProgressReporter parent;
        private readonly float start01;
        private readonly float weight01;
        private readonly string aggregatePhase;
        private readonly string aggregateMessagePrefix;
        private FrameworkLoadingProgress lastProgress;
        private bool hasReportedProgress;

        internal FrameworkLoadingProgressWeightedReporter(
            IFrameworkLoadingProgressReporter parent,
            float start01,
            float weight01,
            string aggregatePhase,
            string aggregateMessagePrefix)
        {
            this.parent = parent ?? NoOpFrameworkLoadingProgressReporter.Instance;
            this.start01 = Clamp01(start01);
            this.weight01 = Clamp01(weight01);
            this.aggregatePhase = string.IsNullOrWhiteSpace(aggregatePhase) ? "Loading" : aggregatePhase.Trim();
            this.aggregateMessagePrefix = Normalize(aggregateMessagePrefix);
            lastProgress = parent != null ? parent.LastProgress : FrameworkLoadingProgress.Unsupported("NoOp", "No loading progress reporter is active for this operation.");
        }

        public bool IsEnabled => parent.IsEnabled && weight01 > 0f;

        public bool HasReportedProgress => hasReportedProgress || parent.HasReportedProgress;

        public FrameworkLoadingProgress LastProgress => hasReportedProgress ? lastProgress : parent.LastProgress;

        public async Awaitable ReportAsync(FrameworkLoadingProgress progress)
        {
            if (!IsEnabled)
            {
                return;
            }

            var mapped = MapProgress(progress);
            lastProgress = mapped;
            hasReportedProgress = true;
            await parent.ReportAsync(mapped);
        }

        private FrameworkLoadingProgress MapProgress(FrameworkLoadingProgress progress)
        {
            if (!progress.Supported || !progress.IsDeterminate)
            {
                return FrameworkLoadingProgress.Indeterminate(
                    progress.Supported,
                    aggregatePhase,
                    BuildMessage(progress.Message));
            }

            var mappedValue = start01 + Clamp01(progress.Value01) * weight01;
            return FrameworkLoadingProgress.Determinate(
                Clamp01(mappedValue),
                aggregatePhase,
                BuildMessage(progress.Message));
        }

        private string BuildMessage(string childMessage)
        {
            childMessage = Normalize(childMessage);
            if (string.IsNullOrWhiteSpace(aggregateMessagePrefix))
            {
                return childMessage;
            }

            return string.IsNullOrWhiteSpace(childMessage)
                ? aggregateMessagePrefix
                : $"{aggregateMessagePrefix} {childMessage}";
        }

        private static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

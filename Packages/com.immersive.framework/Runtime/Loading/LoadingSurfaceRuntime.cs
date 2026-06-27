using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using UnityEngine;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Internal. Loading surface runtime that executes explicit show/update/hide requests against
    /// adapters collected from the canonical UIGlobal scene.
    /// It does not own RouteLifecycle, SceneLifecycle or ActivityFlow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24E canonical UIGlobal loading surface runtime.")]
    internal sealed class LoadingSurfaceRuntime
    {
        private readonly ILoadingSurfaceAdapter[] _adapters;
        private readonly string _surfaceLabel;
        private readonly bool _hasBlockingConfigurationIssue;
        private readonly bool _hasWarningConfigurationIssue;
        private readonly string _blockingConfigurationMessage;
        private readonly string _warningConfigurationMessage;

        private LoadingSurfaceRuntime(
            LoadingSurfacePolicy policy,
            string surfaceLabel,
            IReadOnlyList<ILoadingSurfaceAdapter> adapters,
            bool hasBlockingConfigurationIssue,
            bool hasWarningConfigurationIssue,
            string blockingConfigurationMessage,
            string warningConfigurationMessage)
        {
            Policy = policy;
            _surfaceLabel = string.IsNullOrWhiteSpace(surfaceLabel) ? "Loading Surface" : surfaceLabel.Trim();
            _adapters = CopyAdapters(adapters);
            _hasBlockingConfigurationIssue = hasBlockingConfigurationIssue;
            _hasWarningConfigurationIssue = hasWarningConfigurationIssue;
            _blockingConfigurationMessage = Normalize(blockingConfigurationMessage);
            _warningConfigurationMessage = Normalize(warningConfigurationMessage);
        }

        public LoadingSurfacePolicy Policy { get; }

        public int AdapterCount => _adapters.Length;

        public bool HasVisibleSurface => AdapterCount > 0;

        public bool HasBlockingConfigurationIssue => _hasBlockingConfigurationIssue;

        public bool HasWarningConfigurationIssue => _hasWarningConfigurationIssue;

        public string BlockingConfigurationMessage => _blockingConfigurationMessage;

        public string WarningConfigurationMessage => _warningConfigurationMessage;

        public string SurfaceLabel => _surfaceLabel;

        public string VisualText => HasVisibleSurface ? "UnitySurface" : "None";

        public bool ProgressSupported => false;

        internal static LoadingSurfaceRuntime Create(
            FrameworkLogger logger,
            IReadOnlyList<ILoadingSurfaceAdapter> sceneAdapters,
            string sceneLabel)
        {
            logger ??= FrameworkLogger.Create<LoadingSurfaceRuntime>();
            var resolvedSceneLabel = string.IsNullOrWhiteSpace(sceneLabel) ? "UIGlobal Loading Surface" : sceneLabel.Trim();
            var hasSceneAdapters = sceneAdapters != null && sceneAdapters.Count > 0;

            if (!hasSceneAdapters)
            {
                const string infoMessage = "Loading surface is not configured. Loading will remain explicit NoOp.";
                logger.Info(infoMessage);
                return new LoadingSurfaceRuntime(
                    LoadingSurfacePolicy.NoneConfigured,
                    "Loading Surface",
                    Array.Empty<ILoadingSurfaceAdapter>(),
                    false,
                    false,
                    string.Empty,
                    infoMessage);
            }

            logger.Info($"Loading surface resolved from UIGlobal scene '{resolvedSceneLabel}' with adapterCount='{sceneAdapters.Count}'.");
            return new LoadingSurfaceRuntime(
                LoadingSurfacePolicy.NoneConfigured,
                resolvedSceneLabel,
                sceneAdapters,
                false,
                false,
                string.Empty,
                string.Empty);
        }

        public LoadingSurfaceResult Show(LoadingSurfaceRequest request)
        {
            return Execute(request, LoadingSurfaceAction.Show);
        }

        public LoadingSurfaceResult Update(LoadingSurfaceRequest request)
        {
            return Execute(request, LoadingSurfaceAction.Update);
        }

        public LoadingSurfaceResult Hide(LoadingSurfaceRequest request)
        {
            return Execute(request, LoadingSurfaceAction.Hide);
        }

        public Awaitable<LoadingSurfaceResult> ShowAsync(LoadingSurfaceRequest request)
        {
            return ExecuteAsync(request, LoadingSurfaceAction.Show);
        }

        public Awaitable<LoadingSurfaceResult> UpdateAsync(LoadingSurfaceRequest request)
        {
            return ExecuteAsync(request, LoadingSurfaceAction.Update);
        }

        public Awaitable<LoadingSurfaceResult> HideAsync(LoadingSurfaceRequest request)
        {
            return ExecuteAsync(request, LoadingSurfaceAction.Hide);
        }

        private LoadingSurfaceResult Execute(LoadingSurfaceRequest request, LoadingSurfaceAction expectedAction)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Loading surface runtime requires a valid request.", nameof(request));
            }

            if (request.Action != expectedAction)
            {
                return LoadingSurfaceResult.RejectedResult(
                    request,
                    _surfaceLabel,
                    $"Loading surface runtime expected action '{expectedAction}' but received '{request.Action}'.",
                    new[] { "loading-surface-request-action-mismatch" });
            }

            if (_hasBlockingConfigurationIssue)
            {
                return LoadingSurfaceResult.FailedResult(
                    request,
                    _surfaceLabel,
                    _blockingConfigurationMessage,
                    new[] { "loading-surface-required-configuration-missing" });
            }

            if (!HasVisibleSurface)
            {
                return ExecuteNoOp(request, expectedAction);
            }

            var matchingAdapters = CollectSupportingAdapters(request);
            if (matchingAdapters.Count == 0)
            {
                return LoadingSurfaceResult.SkippedResult(
                    request,
                    _surfaceLabel,
                    $"Loading surface '{_surfaceLabel}' has no adapters that can handle action '{request.Action}'.");
            }

            var issues = new List<string>();
            var blockingIssueCount = 0;
            var warningIssueCount = 0;

            for (var i = 0; i < matchingAdapters.Count; i++)
            {
                var adapter = matchingAdapters[i];
                var result = ExecuteAdapter(adapter, request, expectedAction);
                if (result.Failed || result.Rejected)
                {
                    blockingIssueCount++;
                }
                else if (result.SucceededWithWarnings)
                {
                    warningIssueCount++;
                }

                if (!result.HasIssues)
                {
                    continue;
                }

                for (var issueIndex = 0; issueIndex < result.Issues.Count; issueIndex++)
                {
                    var issueText = result.Issues[issueIndex];
                    if (!string.IsNullOrWhiteSpace(issueText))
                    {
                        issues.Add($"{adapter.AdapterName}: {issueText.Trim()}");
                    }
                }
            }

            if (blockingIssueCount > 0)
            {
                return LoadingSurfaceResult.FailedResult(
                    request,
                    _surfaceLabel,
                    $"Loading surface '{_surfaceLabel}' adapter execution failed.",
                    issues);
            }

            if (warningIssueCount > 0)
            {
                return LoadingSurfaceResult.SucceededWithWarningsResult(
                    request,
                    _surfaceLabel,
                    $"Loading surface '{_surfaceLabel}' completed with warnings.",
                    issues);
            }

            return LoadingSurfaceResult.SucceededResult(
                request,
                _surfaceLabel,
                $"Loading surface '{_surfaceLabel}' completed successfully.");
        }

        private async Awaitable<LoadingSurfaceResult> ExecuteAsync(LoadingSurfaceRequest request, LoadingSurfaceAction expectedAction)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Loading surface runtime requires a valid request.", nameof(request));
            }

            if (request.Action != expectedAction)
            {
                return LoadingSurfaceResult.RejectedResult(
                    request,
                    _surfaceLabel,
                    $"Loading surface runtime expected action '{expectedAction}' but received '{request.Action}'.",
                    new[] { "loading-surface-request-action-mismatch" });
            }

            if (_hasBlockingConfigurationIssue)
            {
                return LoadingSurfaceResult.FailedResult(
                    request,
                    _surfaceLabel,
                    _blockingConfigurationMessage,
                    new[] { "loading-surface-required-configuration-missing" });
            }

            if (!HasVisibleSurface)
            {
                return ExecuteNoOp(request, expectedAction);
            }

            var matchingAdapters = CollectSupportingAdapters(request);
            if (matchingAdapters.Count == 0)
            {
                return LoadingSurfaceResult.SkippedResult(
                    request,
                    _surfaceLabel,
                    $"Loading surface '{_surfaceLabel}' has no adapters that can handle action '{request.Action}'.");
            }

            var issues = new List<string>();
            var blockingIssueCount = 0;
            var warningIssueCount = 0;

            for (var i = 0; i < matchingAdapters.Count; i++)
            {
                var adapter = matchingAdapters[i];
                var result = await ExecuteAdapterAsync(adapter, request, expectedAction);
                if (result.Failed || result.Rejected)
                {
                    blockingIssueCount++;
                }
                else if (result.SucceededWithWarnings)
                {
                    warningIssueCount++;
                }

                if (!result.HasIssues)
                {
                    continue;
                }

                for (var issueIndex = 0; issueIndex < result.Issues.Count; issueIndex++)
                {
                    var issueText = result.Issues[issueIndex];
                    if (!string.IsNullOrWhiteSpace(issueText))
                    {
                        issues.Add($"{adapter.AdapterName}: {issueText.Trim()}");
                    }
                }
            }

            if (blockingIssueCount > 0)
            {
                return LoadingSurfaceResult.FailedResult(
                    request,
                    _surfaceLabel,
                    $"Loading surface '{_surfaceLabel}' adapter execution failed.",
                    issues);
            }

            if (warningIssueCount > 0)
            {
                return LoadingSurfaceResult.SucceededWithWarningsResult(
                    request,
                    _surfaceLabel,
                    $"Loading surface '{_surfaceLabel}' completed with warnings.",
                    issues);
            }

            return LoadingSurfaceResult.SucceededResult(
                request,
                _surfaceLabel,
                $"Loading surface '{_surfaceLabel}' completed successfully.");
        }

        private LoadingSurfaceResult ExecuteAdapter(
            ILoadingSurfaceAdapter adapter,
            LoadingSurfaceRequest request,
            LoadingSurfaceAction expectedAction)
        {
            switch (expectedAction)
            {
                case LoadingSurfaceAction.Show:
                    return adapter.Show(request);
                case LoadingSurfaceAction.Update:
                    return adapter.Update(request);
                case LoadingSurfaceAction.Hide:
                    return adapter.Hide(request);
                default:
                    return LoadingSurfaceResult.RejectedResult(
                        request,
                        adapter.AdapterName,
                        $"Unsupported loading surface action '{expectedAction}'.",
                        new[] { "loading-surface-action-unsupported" });
            }
        }

        private async Awaitable<LoadingSurfaceResult> ExecuteAdapterAsync(
            ILoadingSurfaceAdapter adapter,
            LoadingSurfaceRequest request,
            LoadingSurfaceAction expectedAction)
        {
            if (adapter is IAsyncLoadingSurfaceAdapter asyncAdapter)
            {
                switch (expectedAction)
                {
                    case LoadingSurfaceAction.Show:
                        return await asyncAdapter.ShowAsync(request);
                    case LoadingSurfaceAction.Update:
                        return await asyncAdapter.UpdateAsync(request);
                    case LoadingSurfaceAction.Hide:
                        return await asyncAdapter.HideAsync(request);
                    default:
                        return LoadingSurfaceResult.RejectedResult(
                            request,
                            adapter.AdapterName,
                            $"Unsupported loading surface action '{expectedAction}'.",
                            new[] { "loading-surface-action-unsupported" });
                }
            }

            return ExecuteAdapter(adapter, request, expectedAction);
        }

        private LoadingSurfaceResult ExecuteNoOp(LoadingSurfaceRequest request, LoadingSurfaceAction expectedAction)
        {
            switch (expectedAction)
            {
                case LoadingSurfaceAction.Show:
                    return NoOpLoadingSurfaceAdapter.Instance.Show(request);
                case LoadingSurfaceAction.Update:
                    return NoOpLoadingSurfaceAdapter.Instance.Update(request);
                case LoadingSurfaceAction.Hide:
                    return NoOpLoadingSurfaceAdapter.Instance.Hide(request);
                default:
                    return LoadingSurfaceResult.RejectedResult(
                        request,
                        "NoOp Loading Surface Adapter",
                        $"Unsupported loading surface action '{expectedAction}'.",
                        new[] { "loading-surface-action-unsupported" });
            }
        }

        private List<ILoadingSurfaceAdapter> CollectSupportingAdapters(LoadingSurfaceRequest request)
        {
            var supportingAdapters = new List<ILoadingSurfaceAdapter>();
            for (var i = 0; i < _adapters.Length; i++)
            {
                var adapter = _adapters[i];
                if (adapter != null && adapter.Supports(request))
                {
                    supportingAdapters.Add(adapter);
                }
            }

            return supportingAdapters;
        }

        private static ILoadingSurfaceAdapter[] CopyAdapters(IReadOnlyList<ILoadingSurfaceAdapter> adapters)
        {
            if (adapters == null || adapters.Count == 0)
            {
                return Array.Empty<ILoadingSurfaceAdapter>();
            }

            var copy = new ILoadingSurfaceAdapter[adapters.Count];
            for (var i = 0; i < adapters.Count; i++)
            {
                copy[i] = adapters[i];
            }

            return copy;
        }

        private static List<ILoadingSurfaceAdapter> CollectAdapters(GameObject surfaceInstance)
        {
            var adapters = new List<ILoadingSurfaceAdapter>();
            if (surfaceInstance == null)
            {
                return adapters;
            }

            var behaviours = surfaceInstance.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return adapters;
            }

            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is ILoadingSurfaceAdapter adapter)
                {
                    adapters.Add(adapter);
                }
            }

            return adapters;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

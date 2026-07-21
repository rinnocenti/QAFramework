using Immersive.Framework.Loading;
using UnityEngine;
using UnityEngine.UI;

namespace ImmersiveFrameworkQA.UnityBuildSurface
{
    /// <summary>
    /// QA-only adapter used to make fast loading transitions visually inspectable.
    /// Not a canonical production loading surface adapter.
    /// The hold is awaited by the loading surface runtime, so the route sequence remains:
    /// transition fade-in -> loading show/load/hide -> transition fade-out.
    /// It never delays SceneLifecycle directly and it does not own Route, Activity or GameFlow.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Unity Build Surface/QA Loading Surface Visibility Hold Adapter")]
    public sealed class QaLoadingSurfaceVisibilityHoldAdapter : MonoBehaviour, IAsyncLoadingSurfaceAdapter, ILoadingSurfaceProgressPresentationAdapter
    {
        private const string MissingCanvasGroupIssue = "qa-loading-surface-canvas-group-missing";
        private const string MissingImageIssue = "qa-loading-surface-image-missing";
        private const string UnsupportedActionIssue = "qa-loading-surface-action-unsupported";
        private const string InvalidRequestIssue = "qa-loading-surface-request-invalid";
        private const string ProgressSourcePendingMessage = "Loading progress source is not connected yet.";
        private const float MinimumProgressMotionEpsilon = 0.001f;

        [HideInInspector]
        [SerializeField] private string adapterName = "QA Loading Surface Visibility Hold Adapter";

        [Header("Surface")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject surfaceRoot;
        [HideInInspector]
        [SerializeField] private Image surfaceImage;

        [HideInInspector]
        [Range(0f, 1f)]
        [SerializeField] private float hiddenAlpha = 0f;
        [HideInInspector]
        [Range(0f, 1f)]
        [SerializeField] private float visibleAlpha = 1f;
        [HideInInspector]
        [SerializeField] private bool setSurfaceRootActive = true;
        [HideInInspector]
        [SerializeField] private bool blockRaycastsWhenVisible = true;
        [HideInInspector]
        [SerializeField] private bool interactableWhenVisible;
        [HideInInspector]
        [SerializeField] private bool applyHiddenStateOnAwake = true;

        [Header("QA Visibility Hold")]
        [Min(0f)]
        [SerializeField] private float hideHoldSeconds = 0.75f;
        [HideInInspector]
        [SerializeField] private bool holdHideForManualQa = true;
        [Tooltip("When enabled, the QA hold uses unscaled delta time through Awaitable.NextFrameAsync.")]
        [HideInInspector]
        [SerializeField] private bool useRealtimeSeconds = true;

        [Header("Progress Presentation")]
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private Image progressFillImage;
        [SerializeField] private Slider progressSlider;
        [Range(0f, 1f)]
        [SerializeField] private float initialProgressValue = 0f;
        [SerializeField] private bool showProgressRootWhenVisible = true;
        [SerializeField] private bool hideProgressRootWhenHidden = true;
        [SerializeField] private bool resetProgressOnShow = true;
        [SerializeField] private bool resetProgressOnHide = true;
        [Tooltip("Until the framework exposes a determinate loading source to the surface request, the QA bar stays reset and diagnostics report indeterminate progress.")]
        [SerializeField] private bool useIndeterminateProgressUntilSourceExists = true;

        [Header("QA Progress Motion")]
        [Tooltip("QA-only smoothing that makes very fast scene operations visually inspectable. The core progress value remains determinate and is not synthesized.")]
        [SerializeField] private bool smoothProgressMotionForManualQa = true;
        [Tooltip("Minimum visual motion duration for a determinate progress increase received by this QA adapter.")]
        [Min(0f)]
        [SerializeField] private float minimumProgressMotionSeconds = 0.35f;
        [Tooltip("Forces canvas refresh after progress value changes so Image.fillAmount/Slider values are visible during awaited QA motion.")]
        [SerializeField] private bool forceCanvasUpdateOnProgress = true;

        [HideInInspector]
        [SerializeField] private LoadingSurfaceResultStatus lastStatus = LoadingSurfaceResultStatus.Unknown;
        [HideInInspector]
        [SerializeField] private string lastMessage = string.Empty;
        [HideInInspector]
        [SerializeField] private bool lastVisibleState;
        [HideInInspector]
        [SerializeField] private bool hideHoldActive;
        [HideInInspector]
        [SerializeField] private bool lastProgressSupported;
        [HideInInspector]
        [SerializeField] private bool lastProgressDeterminate;
        [HideInInspector]
        [SerializeField] private float lastProgressValue01;
        [HideInInspector]
        [SerializeField] private int lastProgressPercent;
        [HideInInspector]
        [SerializeField] private string lastProgressMessage = string.Empty;

        private int hideHoldVersion;

        public string AdapterName => string.IsNullOrWhiteSpace(adapterName)
            ? nameof(QaLoadingSurfaceVisibilityHoldAdapter)
            : adapterName.Trim();

        public bool HasCanvasGroup => TryResolveCanvasGroup(out _);

        public bool HasSurfaceImage => TryResolveSurfaceImage(out _);

        public bool HasProgressPresentation => progressRoot != null || progressFillImage != null || progressSlider != null;

        public bool IsVisible => lastVisibleState;

        public bool HideHoldActive => hideHoldActive;

        public LoadingSurfaceResultStatus LastStatus => lastStatus;

        public string LastMessage => string.IsNullOrWhiteSpace(lastMessage) ? string.Empty : lastMessage;

        public bool LastProgressSupported => lastProgressSupported;

        public bool LastProgressDeterminate => lastProgressDeterminate;

        public float LastProgressValue01 => lastProgressValue01;

        public int LastProgressPercent => lastProgressPercent;

        public string LastProgressMessage => string.IsNullOrWhiteSpace(lastProgressMessage) ? string.Empty : lastProgressMessage;

        public float CurrentAlpha => TryResolveCanvasGroup(out var group) ? group.alpha : 0f;

        public bool Supports(LoadingSurfaceRequest request)
        {
            return request.IsValid;
        }

        public LoadingSurfaceResult Show(LoadingSurfaceRequest request)
        {
            return Apply(request, LoadingSurfaceAction.Show, true);
        }

        LoadingSurfaceResult ILoadingSurfaceAdapter.Update(LoadingSurfaceRequest request)
        {
            return Apply(request, LoadingSurfaceAction.Update, request.ShouldBeVisible);
        }

        public LoadingSurfaceResult Hide(LoadingSurfaceRequest request)
        {
            return Apply(request, LoadingSurfaceAction.Hide, false);
        }

        public Awaitable<LoadingSurfaceResult> ShowAsync(LoadingSurfaceRequest request)
        {
            return ApplyVisibleAndAwaitRenderAsync(request, LoadingSurfaceAction.Show);
        }

        Awaitable<LoadingSurfaceResult> IAsyncLoadingSurfaceAdapter.UpdateAsync(LoadingSurfaceRequest request)
        {
            return request.ShouldBeVisible
                ? ApplyVisibleAndAwaitRenderAsync(request, LoadingSurfaceAction.Update)
                : ApplyAsync(request, LoadingSurfaceAction.Update, false);
        }

        public Awaitable<LoadingSurfaceResult> HideAsync(LoadingSurfaceRequest request)
        {
            return ApplyAsync(request, LoadingSurfaceAction.Hide, false);
        }

        public void SetDeterminateProgress01(float value01)
        {
            SetDeterminateProgress01(value01, null);
        }

        public void SetDeterminateProgress01(float value01, string message)
        {
            value01 = NormalizeProgressValue(value01);
            ApplyProgressValue(value01);
            SetProgressRootVisible(showProgressRootWhenVisible && lastVisibleState);

            lastProgressSupported = true;
            lastProgressDeterminate = true;
            lastProgressValue01 = value01;
            lastProgressPercent = Mathf.RoundToInt(value01 * 100f);
            lastProgressMessage = string.IsNullOrWhiteSpace(message)
                ? $"Determinate loading progress applied at {lastProgressPercent}%."
                : message.Trim();
        }

        public void SetIndeterminateProgress()
        {
            SetIndeterminateProgress(null);
        }

        public void SetIndeterminateProgress(string message)
        {
            var value01 = NormalizeProgressValue(initialProgressValue);
            ApplyProgressValue(value01);
            SetProgressRootVisible(showProgressRootWhenVisible && lastVisibleState);

            lastProgressSupported = false;
            lastProgressDeterminate = false;
            lastProgressValue01 = value01;
            lastProgressPercent = Mathf.RoundToInt(value01 * 100f);
            lastProgressMessage = string.IsNullOrWhiteSpace(message)
                ? ProgressSourcePendingMessage
                : message.Trim();
        }

        private void QaApplyVisibleState()
        {
            if (TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                CancelPendingHide();
                ApplyVisibleState(resolvedCanvasGroup);
                lastStatus = LoadingSurfaceResultStatus.Succeeded;
                lastMessage = "QA visible loading state applied.";
            }
        }

        private void QaApplyHiddenState()
        {
            if (TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                CancelPendingHide();
                ApplyHiddenState(resolvedCanvasGroup);
                lastStatus = LoadingSurfaceResultStatus.Succeeded;
                lastMessage = "QA hidden loading state applied.";
            }
        }

        private void QaApplyHalfProgress()
        {
            SetDeterminateProgress01(0.5f, "QA loading progress manually set to 50%.");
        }

        private void QaApplyCompleteProgress()
        {
            SetDeterminateProgress01(1f, "QA loading progress manually set to 100%.");
        }

        private void QaApplyIndeterminateProgress()
        {
            SetIndeterminateProgress("QA loading progress manually set to indeterminate.");
        }

        private void Awake()
        {
            NormalizeConfiguration();

            if (!applyHiddenStateOnAwake || !TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                return;
            }

            ApplyHiddenState(resolvedCanvasGroup);
            lastStatus = LoadingSurfaceResultStatus.Skipped;
            lastMessage = "Initial hidden state applied on Awake.";
        }

        private void OnDisable()
        {
            CancelPendingHide();
        }

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            surfaceImage = GetComponent<Image>();
            surfaceRoot = gameObject;
            NormalizeConfiguration();
        }

        private void OnValidate()
        {
            NormalizeConfiguration();
        }

        private LoadingSurfaceResult Apply(
            LoadingSurfaceRequest request,
            LoadingSurfaceAction expectedAction,
            bool visibleState)
        {
            if (!TryValidate(request, expectedAction, out var failureResult, out var resolvedCanvasGroup))
            {
                return Record(failureResult);
            }

            if (visibleState)
            {
                CancelPendingHide();
                ApplyVisibleState(resolvedCanvasGroup, request);
                return Record(LoadingSurfaceResult.SucceededResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' applied visible loading state."));
            }

            CancelPendingHide();
            ApplyHiddenState(resolvedCanvasGroup);
            return Record(LoadingSurfaceResult.SucceededResult(
                request,
                AdapterName,
                $"Adapter '{AdapterName}' applied hidden loading state."));
        }

        private async Awaitable<LoadingSurfaceResult> ApplyVisibleAndAwaitRenderAsync(
            LoadingSurfaceRequest request,
            LoadingSurfaceAction expectedAction)
        {
            var shouldAnimateProgress = ShouldAnimateDeterminateProgressMotion(
                request,
                expectedAction,
                out var progressFrom01,
                out var progressTo01);

            var result = Apply(request, expectedAction, true);
            if (result.Succeeded && Application.isPlaying)
            {
                if (shouldAnimateProgress)
                {
                    await AnimateDeterminateProgressMotionAsync(progressFrom01, progressTo01);
                }

                await Awaitable.NextFrameAsync();
            }

            return result;
        }

        private async Awaitable<LoadingSurfaceResult> ApplyAsync(
            LoadingSurfaceRequest request,
            LoadingSurfaceAction expectedAction,
            bool visibleState)
        {
            if (!TryValidate(request, expectedAction, out var failureResult, out var resolvedCanvasGroup))
            {
                return Record(failureResult);
            }

            if (visibleState)
            {
                CancelPendingHide();
                ApplyVisibleState(resolvedCanvasGroup, request);
                return Record(LoadingSurfaceResult.SucceededResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' applied visible loading state."));
            }

            if (ShouldHoldHide())
            {
                var version = BeginAwaitableHideHold();
                await WaitForHideHoldAsync(version);
                if (version != hideHoldVersion)
                {
                    return Record(LoadingSurfaceResult.SkippedResult(
                        request,
                        AdapterName,
                        $"Adapter '{AdapterName}' skipped hidden loading state because a newer loading request cancelled the QA hold."));
                }

                ApplyHiddenState(resolvedCanvasGroup);
                hideHoldActive = false;
                return Record(LoadingSurfaceResult.SucceededResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' completed awaited hidden loading state after QA hold seconds='{hideHoldSeconds:0.###}'."));
            }

            CancelPendingHide();
            ApplyHiddenState(resolvedCanvasGroup);
            return Record(LoadingSurfaceResult.SucceededResult(
                request,
                AdapterName,
                $"Adapter '{AdapterName}' applied hidden loading state."));
        }

        private bool TryValidate(
            LoadingSurfaceRequest request,
            LoadingSurfaceAction expectedAction,
            out LoadingSurfaceResult failureResult,
            out CanvasGroup resolvedCanvasGroup)
        {
            failureResult = default;
            resolvedCanvasGroup = null;

            if (!request.IsValid)
            {
                failureResult = LoadingSurfaceResult.RejectedResult(
                    request,
                    AdapterName,
                    "QA loading surface adapter requires a valid request.",
                    new[] { InvalidRequestIssue });
                return false;
            }

            if (request.Action != expectedAction)
            {
                failureResult = LoadingSurfaceResult.RejectedResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' expected action '{expectedAction}' but received '{request.Action}'.",
                    new[] { UnsupportedActionIssue });
                return false;
            }

            if (!TryResolveCanvasGroup(out resolvedCanvasGroup))
            {
                failureResult = LoadingSurfaceResult.FailedResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' requires a CanvasGroup surface.",
                    new[] { MissingCanvasGroupIssue });
                return false;
            }

            if (!TryResolveSurfaceImage(out _))
            {
                failureResult = LoadingSurfaceResult.FailedResult(
                    request,
                    AdapterName,
                    $"Adapter '{AdapterName}' requires a surface Image for the loading panel.",
                    new[] { MissingImageIssue });
                return false;
            }

            return true;
        }

        private bool ShouldHoldHide()
        {
            return Application.isPlaying && holdHideForManualQa && hideHoldSeconds > 0f;
        }

        private int BeginAwaitableHideHold()
        {
            hideHoldVersion++;
            hideHoldActive = true;
            return hideHoldVersion;
        }

        private async Awaitable WaitForHideHoldAsync(int version)
        {
            if (hideHoldSeconds <= 0f)
            {
                return;
            }

            if (!useRealtimeSeconds)
            {
                await Awaitable.WaitForSecondsAsync(hideHoldSeconds);
                return;
            }

            var elapsed = 0f;
            while (version == hideHoldVersion && elapsed < hideHoldSeconds)
            {
                await Awaitable.NextFrameAsync();
                elapsed += Time.unscaledDeltaTime;
            }
        }

        private void CancelPendingHide()
        {
            hideHoldVersion++;
            hideHoldActive = false;
        }

        private void ApplyVisibleState(CanvasGroup resolvedCanvasGroup)
        {
            ApplyVisibleState(resolvedCanvasGroup, null);
        }

        private void ApplyVisibleState(CanvasGroup resolvedCanvasGroup, LoadingSurfaceRequest? request)
        {
            var root = ResolveSurfaceRoot();
            if (setSurfaceRootActive && root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            resolvedCanvasGroup.alpha = visibleAlpha;
            resolvedCanvasGroup.blocksRaycasts = blockRaycastsWhenVisible;
            resolvedCanvasGroup.interactable = interactableWhenVisible;
            lastVisibleState = true;
            ApplyProgressVisibleState(request);
            Canvas.ForceUpdateCanvases();
        }

        private void ApplyHiddenState(CanvasGroup resolvedCanvasGroup)
        {
            resolvedCanvasGroup.alpha = hiddenAlpha;
            resolvedCanvasGroup.blocksRaycasts = false;
            resolvedCanvasGroup.interactable = false;
            ApplyProgressHiddenState();

            var root = ResolveSurfaceRoot();
            if (setSurfaceRootActive && root != null && root.activeSelf)
            {
                root.SetActive(false);
            }

            Canvas.ForceUpdateCanvases();
            lastVisibleState = false;
        }

        private bool ShouldAnimateDeterminateProgressMotion(
            LoadingSurfaceRequest request,
            LoadingSurfaceAction expectedAction,
            out float progressFrom01,
            out float progressTo01)
        {
            progressFrom01 = NormalizeProgressValue(lastProgressDeterminate ? lastProgressValue01 : initialProgressValue);
            progressTo01 = request.ProgressSupported
                ? NormalizeProgressValue(request.Progress.NormalizedValue)
                : progressFrom01;

            if (!Application.isPlaying
                || !smoothProgressMotionForManualQa
                || !HasProgressPresentation
                || expectedAction != LoadingSurfaceAction.Update
                || !request.ProgressSupported
                || minimumProgressMotionSeconds <= 0f)
            {
                return false;
            }

            return progressTo01 > progressFrom01 + MinimumProgressMotionEpsilon;
        }

        private async Awaitable AnimateDeterminateProgressMotionAsync(float from01, float to01)
        {
            from01 = NormalizeProgressValue(from01);
            to01 = NormalizeProgressValue(to01);

            if (minimumProgressMotionSeconds <= 0f || to01 <= from01 + MinimumProgressMotionEpsilon)
            {
                ApplyProgressValue(to01);
                return;
            }

            var elapsed = 0f;
            ApplyProgressValue(from01);
            while (elapsed < minimumProgressMotionSeconds)
            {
                await Awaitable.NextFrameAsync();
                elapsed += Time.unscaledDeltaTime;
                var t = minimumProgressMotionSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / minimumProgressMotionSeconds);
                ApplyProgressValue(Mathf.Lerp(from01, to01, t));
            }

            ApplyProgressValue(to01);
        }

        private void ApplyProgressVisibleState(LoadingSurfaceRequest? request)
        {
            if (!HasProgressPresentation)
            {
                return;
            }

            SetProgressRootVisible(showProgressRootWhenVisible);

            if (request.HasValue && request.Value.ProgressSupported)
            {
                ApplyDeterminateProgressState(
                    request.Value.Progress.NormalizedValue,
                    BuildRequestProgressMessage(request.Value));
                return;
            }

            if (request.HasValue)
            {
                if (useIndeterminateProgressUntilSourceExists)
                {
                    ApplyIndeterminateProgressState(BuildUnsupportedProgressMessage(request.Value));
                    return;
                }

                ApplyDeterminateProgressState(initialProgressValue, "Initial loading progress value applied because the request has no supported progress source.");
                return;
            }

            if (resetProgressOnShow || !lastProgressSupported)
            {
                if (useIndeterminateProgressUntilSourceExists)
                {
                    ApplyIndeterminateProgressState(ProgressSourcePendingMessage);
                    return;
                }

                ApplyDeterminateProgressState(initialProgressValue, "Initial loading progress value applied.");
                return;
            }

            ApplyProgressValue(lastProgressValue01);
        }

        private void ApplyProgressHiddenState()
        {
            if (!HasProgressPresentation)
            {
                return;
            }

            if (resetProgressOnHide)
            {
                ApplyProgressValue(0f);
                lastProgressValue01 = 0f;
                lastProgressPercent = 0;
            }

            lastProgressSupported = false;
            lastProgressDeterminate = false;
            lastProgressMessage = "Loading progress hidden.";

            if (hideProgressRootWhenHidden)
            {
                SetProgressRootVisible(false);
            }
        }

        private void ApplyIndeterminateProgressState(string message)
        {
            var value01 = NormalizeProgressValue(initialProgressValue);
            ApplyProgressValue(value01);

            lastProgressSupported = false;
            lastProgressDeterminate = false;
            lastProgressValue01 = value01;
            lastProgressPercent = Mathf.RoundToInt(value01 * 100f);
            lastProgressMessage = string.IsNullOrWhiteSpace(message)
                ? ProgressSourcePendingMessage
                : message.Trim();
        }

        private static string BuildRequestProgressMessage(LoadingSurfaceRequest request)
        {
            var sourceText = request.HasSource ? request.Source : "LoadingSurfaceRequest";
            return $"Loading progress consumed from request source='{sourceText}' percent='{request.Progress.PercentRounded}'.";
        }

        private static string BuildUnsupportedProgressMessage(LoadingSurfaceRequest request)
        {
            if (request.HasDetail)
            {
                return $"{ProgressSourcePendingMessage} detail='{request.Detail}'.";
            }

            return ProgressSourcePendingMessage;
        }

        private void ApplyDeterminateProgressState(float value01, string message)
        {
            value01 = NormalizeProgressValue(value01);
            ApplyProgressValue(value01);

            lastProgressSupported = true;
            lastProgressDeterminate = true;
            lastProgressValue01 = value01;
            lastProgressPercent = Mathf.RoundToInt(value01 * 100f);
            lastProgressMessage = string.IsNullOrWhiteSpace(message)
                ? $"Determinate loading progress applied at {lastProgressPercent}%."
                : message.Trim();
        }

        private void ApplyProgressValue(float value01)
        {
            value01 = NormalizeProgressValue(value01);

            if (progressFillImage != null)
            {
                progressFillImage.fillAmount = value01;
            }

            if (progressSlider != null)
            {
                progressSlider.normalizedValue = value01;
            }

            if (forceCanvasUpdateOnProgress)
            {
                Canvas.ForceUpdateCanvases();
            }
        }

        private void SetProgressRootVisible(bool visible)
        {
            var resolvedProgressRoot = ResolveProgressRoot();
            if (resolvedProgressRoot != null && resolvedProgressRoot.activeSelf != visible)
            {
                resolvedProgressRoot.SetActive(visible);
            }
        }

        private float NormalizeProgressValue(float value01)
        {
            return float.IsNaN(value01) ? 0f : Mathf.Clamp01(value01);
        }

        private bool TryResolveCanvasGroup(out CanvasGroup resolvedCanvasGroup)
        {
            resolvedCanvasGroup = canvasGroup;
            if (resolvedCanvasGroup != null)
            {
                return true;
            }

            resolvedCanvasGroup = GetComponent<CanvasGroup>();
            if (resolvedCanvasGroup == null)
            {
                return false;
            }

            canvasGroup = resolvedCanvasGroup;
            return true;
        }

        private bool TryResolveSurfaceImage(out Image resolvedSurfaceImage)
        {
            resolvedSurfaceImage = surfaceImage;
            if (resolvedSurfaceImage != null)
            {
                return true;
            }

            resolvedSurfaceImage = GetComponent<Image>();
            if (resolvedSurfaceImage == null)
            {
                return false;
            }

            surfaceImage = resolvedSurfaceImage;
            return true;
        }

        private GameObject ResolveSurfaceRoot()
        {
            if (surfaceRoot != null)
            {
                return surfaceRoot;
            }

            surfaceRoot = gameObject;
            return surfaceRoot;
        }

        private GameObject ResolveProgressRoot()
        {
            if (progressRoot != null)
            {
                return progressRoot;
            }

            if (progressSlider != null)
            {
                progressRoot = progressSlider.gameObject;
                return progressRoot;
            }

            if (progressFillImage != null)
            {
                progressRoot = progressFillImage.gameObject;
                return progressRoot;
            }

            return null;
        }

        private void NormalizeConfiguration()
        {
            hiddenAlpha = Mathf.Clamp01(hiddenAlpha);
            visibleAlpha = Mathf.Clamp01(visibleAlpha);
            hideHoldSeconds = Mathf.Max(0f, hideHoldSeconds);
            minimumProgressMotionSeconds = Mathf.Max(0f, minimumProgressMotionSeconds);
            initialProgressValue = NormalizeProgressValue(initialProgressValue);
            lastProgressValue01 = NormalizeProgressValue(lastProgressValue01);
            lastProgressPercent = Mathf.Clamp(lastProgressPercent, 0, 100);

            if (surfaceRoot == null)
            {
                surfaceRoot = gameObject;
            }
        }

        private LoadingSurfaceResult Record(LoadingSurfaceResult result)
        {
            lastStatus = result.Status;
            lastMessage = result.Message;
            lastVisibleState = HasCanvasGroup && CurrentAlpha > 0.001f;
            return result;
        }
    }
}

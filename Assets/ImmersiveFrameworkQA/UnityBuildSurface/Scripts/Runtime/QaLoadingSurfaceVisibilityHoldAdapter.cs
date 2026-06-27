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
    public sealed class QaLoadingSurfaceVisibilityHoldAdapter : MonoBehaviour, IAsyncLoadingSurfaceAdapter
    {
        private const string MissingCanvasGroupIssue = "qa-loading-surface-canvas-group-missing";
        private const string MissingImageIssue = "qa-loading-surface-image-missing";
        private const string UnsupportedActionIssue = "qa-loading-surface-action-unsupported";
        private const string InvalidRequestIssue = "qa-loading-surface-request-invalid";

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

        [HideInInspector]
        [SerializeField] private LoadingSurfaceResultStatus lastStatus = LoadingSurfaceResultStatus.Unknown;
        [HideInInspector]
        [SerializeField] private string lastMessage = string.Empty;
        [HideInInspector]
        [SerializeField] private bool lastVisibleState;
        [HideInInspector]
        [SerializeField] private bool hideHoldActive;

        private int hideHoldVersion;

        public string AdapterName => string.IsNullOrWhiteSpace(adapterName)
            ? nameof(QaLoadingSurfaceVisibilityHoldAdapter)
            : adapterName.Trim();

        public bool HasCanvasGroup => TryResolveCanvasGroup(out _);

        public bool HasSurfaceImage => TryResolveSurfaceImage(out _);

        public bool IsVisible => lastVisibleState;

        public bool HideHoldActive => hideHoldActive;

        public LoadingSurfaceResultStatus LastStatus => lastStatus;

        public string LastMessage => string.IsNullOrWhiteSpace(lastMessage) ? string.Empty : lastMessage;

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

        [ContextMenu("Immersive Framework/QA Apply Visible Loading State")]
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

        [ContextMenu("Immersive Framework/QA Apply Hidden Loading State")]
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
                ApplyVisibleState(resolvedCanvasGroup);
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
            var result = Apply(request, expectedAction, true);
            if (result.Succeeded && Application.isPlaying)
            {
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
                ApplyVisibleState(resolvedCanvasGroup);
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
            var root = ResolveSurfaceRoot();
            if (setSurfaceRootActive && root != null && !root.activeSelf)
            {
                root.SetActive(true);
            }

            resolvedCanvasGroup.alpha = visibleAlpha;
            resolvedCanvasGroup.blocksRaycasts = blockRaycastsWhenVisible;
            resolvedCanvasGroup.interactable = interactableWhenVisible;
            Canvas.ForceUpdateCanvases();
            lastVisibleState = true;
        }

        private void ApplyHiddenState(CanvasGroup resolvedCanvasGroup)
        {
            resolvedCanvasGroup.alpha = hiddenAlpha;
            resolvedCanvasGroup.blocksRaycasts = false;
            resolvedCanvasGroup.interactable = false;

            var root = ResolveSurfaceRoot();
            if (setSurfaceRootActive && root != null && root.activeSelf)
            {
                root.SetActive(false);
            }

            Canvas.ForceUpdateCanvases();
            lastVisibleState = false;
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

        private void NormalizeConfiguration()
        {
            hiddenAlpha = Mathf.Clamp01(hiddenAlpha);
            visibleAlpha = Mathf.Clamp01(visibleAlpha);
            hideHoldSeconds = Mathf.Max(0f, hideHoldSeconds);

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

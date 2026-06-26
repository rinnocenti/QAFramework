using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;
using UnityEngine;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Minimal built-in Unity adapter for fade/curtain/blackout surfaces.
    /// It mutates only a configured CanvasGroup and optional surface root active state.
    /// It does not animate over time, use DOTween, own Transition lifecycle, register itself globally or discover other adapters.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Transition Effects/Unity Fade Curtain Effect Adapter")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19D minimal Unity fade/curtain adapter boundary; no registry, tweening or lifecycle ownership.")]
    public sealed class UnityFadeCurtainEffectAdapter : MonoBehaviour, ITransitionEffectAdapter
    {
        private const string MissingCanvasGroupIssue = "unity-fade-curtain-canvas-group-missing";
        private const string UnsupportedKindIssue = "unity-fade-curtain-kind-unsupported";
        private const string UnsupportedPhaseIssue = "unity-fade-curtain-phase-unsupported";

        [Header("Identity")]
        [SerializeField] private string adapterName = "Unity Fade Curtain Effect Adapter";
        [SerializeField] private TransitionEffectKind effectKind = TransitionEffectKind.Fade;

        [Header("Surface")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject surfaceRoot;
        [SerializeField] private bool setSurfaceRootActive = true;

        [Header("Visual State")]
        [Range(0f, 1f)]
        [SerializeField] private float hiddenAlpha = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float visibleAlpha = 1f;
        [SerializeField] private bool blockRaycastsWhenVisible = true;
        [SerializeField] private bool interactableWhenVisible;
        [SerializeField] private bool applyHiddenStateOnAwake = true;

        [Header("Diagnostics")]
        [SerializeField] private TransitionEffectStatus lastStatus = TransitionEffectStatus.Unknown;
        [SerializeField] private string lastMessage = string.Empty;
        [SerializeField] private bool lastVisibleState;

        public string AdapterName => string.IsNullOrWhiteSpace(adapterName)
            ? nameof(UnityFadeCurtainEffectAdapter)
            : adapterName.Trim();

        public TransitionEffectKind ConfiguredEffectKind => effectKind;

        public bool HasCanvasGroup => TryResolveCanvasGroup(out _);

        public bool IsVisible => lastVisibleState;

        public TransitionEffectStatus LastStatus => lastStatus;

        public string LastMessage => string.IsNullOrWhiteSpace(lastMessage) ? string.Empty : lastMessage;

        public float CurrentAlpha => TryResolveCanvasGroup(out var group) ? group.alpha : 0f;

        public bool Supports(TransitionEffectKind candidateKind)
        {
            return IsSupportedKind(effectKind) && candidateKind == effectKind;
        }

        public TransitionEffectResult Execute(TransitionEffectRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Unity fade/curtain adapter requires a valid Transition Effect request.", nameof(request));
            }

            if (!Supports(request.EffectKind))
            {
                return Record(TransitionEffectResult.RejectedResult(
                    request,
                    $"Adapter '{AdapterName}' does not support effect kind '{request.EffectKind}'.",
                    new[] { UnsupportedKindIssue }));
            }

            if (!TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                return Record(TransitionEffectResult.FailedResult(
                    request,
                    $"Adapter '{AdapterName}' requires a CanvasGroup surface.",
                    new[] { MissingCanvasGroupIssue }));
            }

            if (ShouldApplyVisibleState(request.Phase))
            {
                ApplyVisibleState(resolvedCanvasGroup);
                return Record(TransitionEffectResult.SucceededResult(
                    request,
                    $"Adapter '{AdapterName}' applied visible fade/curtain state."));
            }

            if (ShouldApplyHiddenState(request.Phase))
            {
                ApplyHiddenState(resolvedCanvasGroup);
                return Record(TransitionEffectResult.SucceededResult(
                    request,
                    $"Adapter '{AdapterName}' applied hidden fade/curtain state."));
            }

            return Record(TransitionEffectResult.RejectedResult(
                request,
                $"Adapter '{AdapterName}' does not handle transition phase '{request.Phase}'.",
                new[] { UnsupportedPhaseIssue }));
        }

        [ContextMenu("Immersive Framework/QA Apply Visible Curtain State")]
        private void QaApplyVisibleState()
        {
            if (TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                ApplyVisibleState(resolvedCanvasGroup);
                lastStatus = TransitionEffectStatus.Succeeded;
                lastMessage = "QA visible curtain state applied.";
            }
        }

        [ContextMenu("Immersive Framework/QA Apply Hidden Curtain State")]
        private void QaApplyHiddenState()
        {
            if (TryResolveCanvasGroup(out var resolvedCanvasGroup))
            {
                ApplyHiddenState(resolvedCanvasGroup);
                lastStatus = TransitionEffectStatus.Succeeded;
                lastMessage = "QA hidden curtain state applied.";
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
            lastStatus = TransitionEffectStatus.Skipped;
            lastMessage = "Initial hidden state applied on Awake.";
        }

        private void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            surfaceRoot = gameObject;
            NormalizeConfiguration();
        }

        private void OnValidate()
        {
            NormalizeConfiguration();
        }

        private TransitionEffectResult Record(TransitionEffectResult result)
        {
            lastStatus = result.Status;
            lastMessage = result.Message;
            lastVisibleState = HasCanvasGroup && CurrentAlpha > 0.001f;
            return result;
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
            if (!IsSupportedKind(effectKind))
            {
                effectKind = TransitionEffectKind.Fade;
            }

            hiddenAlpha = Mathf.Clamp01(hiddenAlpha);
            visibleAlpha = Mathf.Clamp01(visibleAlpha);

            if (surfaceRoot == null)
            {
                surfaceRoot = gameObject;
            }
        }

        private static bool IsSupportedKind(TransitionEffectKind candidateKind)
        {
            return candidateKind == TransitionEffectKind.Fade
                || candidateKind == TransitionEffectKind.Curtain
                || candidateKind == TransitionEffectKind.Blackout;
        }

        private static bool ShouldApplyVisibleState(TransitionPhase phase)
        {
            return phase == TransitionPhase.OperationOpened
                || phase == TransitionPhase.GateBlockApplied
                || phase == TransitionPhase.PreviousScopeExitObserved;
        }

        private static bool ShouldApplyHiddenState(TransitionPhase phase)
        {
            return phase == TransitionPhase.ReadinessObserved
                || phase == TransitionPhase.GateBlockReleased
                || phase == TransitionPhase.OperationClosed;
        }
    }
}

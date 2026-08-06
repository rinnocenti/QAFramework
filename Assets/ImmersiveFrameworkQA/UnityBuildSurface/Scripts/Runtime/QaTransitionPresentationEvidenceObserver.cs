using System;
using System.Collections.Generic;
using Immersive.Framework.TransitionEffects;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface
{
    public enum QaTransitionVisualState
    {
        Unknown = 0,
        Hidden = 1,
        Transitioning = 2,
        Visible = 3
    }

    public enum QaTransitionPresentationEvidenceKind
    {
        Unknown = 0,
        Baseline = 1,
        StateChanged = 2,
        Checkpoint = 3
    }

    public readonly struct QaTransitionPresentationEvidenceEntry
    {
        public QaTransitionPresentationEvidenceEntry(
            int sequence,
            int frame,
            double realtimeSinceStartup,
            QaTransitionPresentationEvidenceKind kind,
            QaTransitionVisualState visualState,
            bool adapterVisible,
            float alpha,
            TransitionEffectStatus status,
            string message,
            string label)
        {
            Sequence = sequence;
            Frame = frame;
            RealtimeSinceStartup = realtimeSinceStartup;
            Kind = kind;
            VisualState = visualState;
            AdapterVisible = adapterVisible;
            Alpha = alpha;
            Status = status;
            Message = message ?? string.Empty;
            Label = label ?? string.Empty;
        }

        public int Sequence { get; }
        public int Frame { get; }
        public double RealtimeSinceStartup { get; }
        public QaTransitionPresentationEvidenceKind Kind { get; }
        public QaTransitionVisualState VisualState { get; }
        public bool AdapterVisible { get; }
        public float Alpha { get; }
        public TransitionEffectStatus Status { get; }
        public string Message { get; }
        public string Label { get; }
    }

    [DisallowMultipleComponent]
    public sealed class QaTransitionPresentationEvidenceObserver : MonoBehaviour
    {
        private const int MaximumPresentationEvidenceEntries = 128;
        private const float HiddenAlphaThreshold = 0.001f;
        private const float VisibleAlphaThreshold = 0.999f;

        private readonly List<QaTransitionPresentationEvidenceEntry> presentationEvidence =
            new List<QaTransitionPresentationEvidenceEntry>();
        private UnityFadeCurtainEffectAdapter adapter;
        private CanvasGroup canvasGroup;
        private int presentationEvidenceSequence;
        private QaTransitionVisualState lastVisualState;
        private bool hasObservedState;

        public UnityFadeCurtainEffectAdapter Adapter => adapter;
        public bool IsBound => adapter != null && canvasGroup != null;
        public int PresentationEvidenceRevision { get; private set; }
        public int SettledVisibleCount { get; private set; }
        public int SettledHiddenCount { get; private set; }
        public int TransitioningCount { get; private set; }
        public IReadOnlyList<QaTransitionPresentationEvidenceEntry> PresentationEvidence =>
            presentationEvidence;
        public event Action<QaTransitionPresentationEvidenceEntry> PresentationEvidenceRecorded;

        public void Bind(UnityFadeCurtainEffectAdapter targetAdapter)
        {
            if (targetAdapter == null)
            {
                throw new ArgumentNullException(nameof(targetAdapter));
            }

            if (!targetAdapter.HasCanvasGroup)
            {
                throw new InvalidOperationException(
                    "Transition presentation observer requires an adapter CanvasGroup.");
            }

            CanvasGroup resolvedCanvasGroup = targetAdapter.GetComponent<CanvasGroup>();
            if (resolvedCanvasGroup == null)
            {
                throw new InvalidOperationException(
                    "Transition presentation observer could not resolve CanvasGroup from the adapter GameObject.");
            }

            adapter = targetAdapter;
            canvasGroup = resolvedCanvasGroup;
            ResetEvidence();
        }

        public void ResetEvidence()
        {
            RequireBound();
            presentationEvidence.Clear();
            presentationEvidenceSequence = 0;
            SettledVisibleCount = 0;
            SettledHiddenCount = 0;
            TransitioningCount = 0;
            PresentationEvidenceRevision++;
            lastVisualState = ClassifyVisualState();
            hasObservedState = true;
            Record(QaTransitionPresentationEvidenceKind.Baseline, lastVisualState,
                "baseline");
        }

        public void CaptureCheckpoint(string label)
        {
            RequireBound();
            Record(QaTransitionPresentationEvidenceKind.Checkpoint,
                ClassifyVisualState(), label);
        }

        private void LateUpdate()
        {
            if (!IsBound)
            {
                return;
            }

            QaTransitionVisualState current = ClassifyVisualState();
            if (!hasObservedState)
            {
                lastVisualState = current;
                hasObservedState = true;
                return;
            }

            if (current == lastVisualState)
            {
                return;
            }

            lastVisualState = current;
            if (current == QaTransitionVisualState.Visible)
            {
                SettledVisibleCount++;
            }
            else if (current == QaTransitionVisualState.Hidden)
            {
                SettledHiddenCount++;
            }
            else if (current == QaTransitionVisualState.Transitioning)
            {
                TransitioningCount++;
            }

            Record(QaTransitionPresentationEvidenceKind.StateChanged, current,
                string.Empty);
        }

        private QaTransitionVisualState ClassifyVisualState()
        {
            float alpha = canvasGroup.alpha;
            if (alpha <= HiddenAlphaThreshold)
            {
                return QaTransitionVisualState.Hidden;
            }

            if (alpha >= VisibleAlphaThreshold)
            {
                return QaTransitionVisualState.Visible;
            }

            return QaTransitionVisualState.Transitioning;
        }

        private void Record(
            QaTransitionPresentationEvidenceKind kind,
            QaTransitionVisualState visualState,
            string label)
        {
            if (presentationEvidence.Count == MaximumPresentationEvidenceEntries)
            {
                presentationEvidence.RemoveAt(0);
            }

            var entry = new QaTransitionPresentationEvidenceEntry(
                ++presentationEvidenceSequence,
                Time.frameCount,
                Time.realtimeSinceStartupAsDouble,
                kind,
                visualState,
                adapter.IsVisible,
                canvasGroup.alpha,
                adapter.LastStatus,
                adapter.LastMessage,
                label);
            presentationEvidence.Add(entry);
            PresentationEvidenceRecorded?.Invoke(entry);
        }

        private void RequireBound()
        {
            if (!IsBound)
            {
                throw new InvalidOperationException(
                    "Transition presentation observer must be bound before recording evidence.");
            }
        }
    }
}

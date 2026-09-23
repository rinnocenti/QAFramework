using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using UnityEngine;
using UnityEngine.Events;

namespace ImmersiveFrameworkQA.UnityBuildSurface
{
    /// <summary>
    /// QA-only scene-local driver for the Q2B startup parity fixture.
    /// It observes only participants under its own hierarchy and completes one
    /// deterministic occurrence without resolving any framework service.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework QA/Unity Build Surface/" +
        "QA Participant-Aware Startup Parity Driver")]
    public sealed class QaParticipantAwareStartupParityDriver : MonoBehaviour
    {
        private const int ExpectedRequiredCount = 4;
        private const int ExpectedOptionalCount = 1;

        private readonly List<ActivityReadinessParticipant> participants =
            new List<ActivityReadinessParticipant>();
        private readonly List<ActivityReadinessParticipant> required =
            new List<ActivityReadinessParticipant>();
        private readonly List<ListenerRegistration> listeners =
            new List<ListenerRegistration>();

        private ActivityReadinessParticipant optional;
        private bool completionScheduled;

        public int ParticipantCount => participants.Count;
        public int RequiredCount => required.Count;
        public int OptionalCount => optional != null ? 1 : 0;
        public int PreparationStartedCount { get; private set; }
        public int PreparationReleasedCount { get; private set; }
        public int RequiredCompletionCount { get; private set; }
        public bool OptionalFailureIssued { get; private set; }
        public bool CompletionSequenceFinished { get; private set; }
        public int CompletionFrame { get; private set; }
        public string Failure { get; private set; } = string.Empty;
        public bool HasFailure => !string.IsNullOrWhiteSpace(Failure);

        private void Awake()
        {
            try
            {
                ActivityReadinessParticipant[] discovered =
                    GetComponentsInChildren<ActivityReadinessParticipant>(true);
                for (int index = 0; index < discovered.Length; index++)
                {
                    ActivityReadinessParticipant participant = discovered[index];
                    if (participant == null)
                    {
                        continue;
                    }

                    participants.Add(participant);
                    if (participant.Requiredness ==
                        ActivityContentExecutionRequiredness.Required)
                    {
                        required.Add(participant);
                    }
                    else if (participant.Requiredness ==
                        ActivityContentExecutionRequiredness.Optional)
                    {
                        if (optional != null)
                        {
                            throw new InvalidOperationException(
                                "Q2B fixture contains more than one Optional participant.");
                        }

                        optional = participant;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Q2B participant '{participant.ParticipantId}' has invalid requiredness.");
                    }
                }

                Require(required.Count == ExpectedRequiredCount,
                    $"Q2B fixture requires {ExpectedRequiredCount} Required participants. " +
                    $"actual='{required.Count}'.");
                Require(optional != null,
                    $"Q2B fixture requires {ExpectedOptionalCount} Optional participant.");
                Require(participants.Count ==
                    ExpectedRequiredCount + ExpectedOptionalCount,
                    "Q2B fixture participant total diverged.");

                for (int index = 0; index < participants.Count; index++)
                {
                    Register(participants[index]);
                }
            }
            catch (Exception exception)
            {
                RecordFailure(exception);
            }
        }

        private void OnDestroy()
        {
            for (int index = 0; index < listeners.Count; index++)
            {
                listeners[index].Remove();
            }

            listeners.Clear();
        }

        private void Register(ActivityReadinessParticipant participant)
        {
            UnityAction started = HandlePreparationStarted;
            UnityAction released = () => PreparationReleasedCount++;
            participant.PreparationStarted.AddListener(started);
            participant.PreparationReleased.AddListener(released);
            listeners.Add(new ListenerRegistration(
                participant,
                started,
                released));
        }

        private void HandlePreparationStarted()
        {
            PreparationStartedCount++;
            if (PreparationStartedCount != participants.Count ||
                completionScheduled)
            {
                return;
            }

            completionScheduled = true;
            CompleteOccurrenceAsync();
        }

        private async void CompleteOccurrenceAsync()
        {
            try
            {
                // Fixed causal boundary: completion must not execute reentrantly inside
                // the final PreparationStarted UnityEvent stack.
                await Awaitable.NextFrameAsync();

                Require(optional != null &&
                    optional.State == ActivityReadinessParticipantState.Preparing,
                    "Q2B Optional participant was not Preparing.");
                optional.FailPreparation("QA_READY_PROGRESS_02B_OPTIONAL_NON_BLOCKING");
                OptionalFailureIssued = true;

                for (int index = 0; index < required.Count; index++)
                {
                    // Fixed visual sequencing, not polling. Each Required completion
                    // receives its own frame so the Loading adapter can retain evidence.
                    await Awaitable.NextFrameAsync();
                    ActivityReadinessParticipant participant = required[index];
                    Require(participant != null &&
                        participant.State ==
                        ActivityReadinessParticipantState.Preparing,
                        $"Q2B Required participant '{index}' was not Preparing.");
                    participant.CompletePreparation();
                    Require(participant.State ==
                        ActivityReadinessParticipantState.Completed,
                        $"Q2B Required participant '{index}' did not complete.");
                    RequiredCompletionCount++;
                }

                CompletionFrame = Time.frameCount;
                CompletionSequenceFinished = true;
            }
            catch (Exception exception)
            {
                RecordFailure(exception);
                FailPendingParticipants();
            }
        }

        private void FailPendingParticipants()
        {
            for (int index = 0; index < participants.Count; index++)
            {
                ActivityReadinessParticipant participant = participants[index];
                if (participant == null ||
                    participant.State !=
                        ActivityReadinessParticipantState.Preparing)
                {
                    continue;
                }

                try
                {
                    participant.FailPreparation(
                        "QA_READY_PROGRESS_02B_DRIVER_FAILURE");
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        "[QA_READY_PROGRESS_02B_DRIVER] " +
                        "status='TerminalFailurePropagationFailed' " +
                        $"participant='{participant.ParticipantId}' " +
                        $"failure='{exception.GetType().Name}: " +
                        $"{exception.Message}'.",
                        this);
                }
            }
        }

        private void RecordFailure(Exception exception)
        {
            Failure = exception == null
                ? "Unknown Q2B fixture failure."
                : $"{exception.GetType().Name}: {exception.Message}";
            Debug.LogError($"[QA_READY_PROGRESS_02B_DRIVER] status='Failed' " +
                $"failure='{Failure}'.", this);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private readonly struct ListenerRegistration
        {
            internal ListenerRegistration(
                ActivityReadinessParticipant participant,
                UnityAction started,
                UnityAction released)
            {
                Participant = participant;
                Started = started;
                Released = released;
            }

            private ActivityReadinessParticipant Participant { get; }
            private UnityAction Started { get; }
            private UnityAction Released { get; }

            internal void Remove()
            {
                if (Participant == null)
                {
                    return;
                }

                Participant.PreparationStarted.RemoveListener(Started);
                Participant.PreparationReleased.RemoveListener(Released);
            }
        }
    }
}

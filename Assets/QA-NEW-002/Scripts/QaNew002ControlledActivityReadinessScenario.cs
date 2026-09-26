using System;
using System.Collections;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Transition;
using UnityEngine;

namespace Immersive.QaFramework.New002
{
    [DisallowMultipleComponent]
    public sealed class QaNew002ControlledActivityReadinessScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-NEW-002";

        private enum QaVerdict
        {
            None,
            Pass,
            Fail,
            Blocked
        }

        [Header("Activity Definitions")]
        [SerializeField] private ActivityAsset activityA;
        [SerializeField] private ActivityAsset activityB;
        [SerializeField] private string expectedActivityAId = "qa-new-002.activity-a";
        [SerializeField] private string expectedActivityBId = "qa-new-002.activity-b";

        [Header("Scene-authored Boundaries")]
        [SerializeField] private ActivityContentContribution contributionA;
        [SerializeField] private ActivityContentContribution contributionB;
        [SerializeField] private QaNew002ActivityLifecycleProbe lifecycleA;
        [SerializeField] private QaNew002ActivityLifecycleProbe lifecycleB;
        [SerializeField] private ActivityRequestTrigger requestB;
        [SerializeField] private ActivityRequestTrigger requestA;

        [Header("Controlled Readiness")]
        [SerializeField] private ActivityReadinessParticipant readinessParticipantB;
        [SerializeField] private ActivityReadinessEvents readinessEventsB;
        [SerializeField] private string expectedParticipantId = "qa-new-002.activity-b.required";

        [Header("Local Missing-evidence Deadlines")]
        [SerializeField, Min(1f)] private float baselineTimeoutSeconds = 10f;
        [SerializeField, Min(1f)] private float pendingTimeoutSeconds = 10f;
        [SerializeField, Min(1f)] private float terminalTimeoutSeconds = 10f;
        [SerializeField, Min(1f)] private float cleanupTimeoutSeconds = 10f;

        private IEventBinding _requestBBinding;
        private IEventBinding _requestABinding;
        private QaVerdict _verdict;
        private string _firstCausalDivergence = string.Empty;
        private bool _scenarioRunning;
        private bool _cleanupFinished;
        private bool _freshBootRequired;
        private bool _requestBAccepted;
        private bool _pendingProved;
        private bool _releaseIssued;
        private bool _requestAInvoked;
        private bool _baselineAReadinessNormalized;
        private bool _activityBReleaseObserved;
        private int _requestBSubmitted;
        private int _requestBCompleted;
        private int _requestASubmitted;
        private int _requestACompleted;
        private ActivityRequestTriggerEvent _requestBTerminal;
        private ActivityRequestTriggerEvent _requestATerminal;
        private int _initialReadinessRevision;
        private int _pendingReadinessRevision;
        private int _initialParticipantOccurrence;
        private int _activityBOccurrence;
        private int _aEnterBaseline;
        private int _aExitBaseline;
        private int _bEnterBaseline;
        private int _bExitBaseline;
        private int _causalSequence;
        private int _submittedBSequence;
        private int _exitASequence;
        private int _enterBSequence;
        private int _pendingSequence;
        private int _releaseSequence;
        private int _completedBSequence;
        private int _submittedASequence;
        private int _releasedBSequence;
        private int _exitBSequence;
        private int _enterASequence;
        private int _completedASequence;
        private int _activityBReleaseCount;

        private IEnumerator Start()
        {
            _scenarioRunning = true;

            if (!TryValidateSerializedComposition(out string compositionIssue))
            {
                RecordBlocked(compositionIssue);
                yield return FinishAfterCleanup();
                yield break;
            }

            double baselineDeadline = Time.realtimeSinceStartupAsDouble + baselineTimeoutSeconds;
            string readinessIssue = string.Empty;
            while (Time.realtimeSinceStartupAsDouble < baselineDeadline)
            {
                if (TryValidateBaselineComposition(out readinessIssue))
                {
                    if (readinessParticipantB.State == ActivityReadinessParticipantState.Preparing)
                    {
                        readinessParticipantB.CompletePreparation();
                        _baselineAReadinessNormalized = true;
                    }

                    if (TryValidateBaseline(out readinessIssue))
                    {
                        break;
                    }
                }

                yield return null;
            }

            if (!TryValidateBaseline(out readinessIssue))
            {
                RecordBlocked($"Known baseline A was not established before the local deadline. {readinessIssue}");
                yield return FinishAfterCleanup();
                yield break;
            }

            CaptureBaselineEvidence();
            BindEvidence();
            Debug.Log(
                $"[{ScenarioId}] expectations declared: Submitted(B)=1, Completed(B)=0 before release, " +
                "A exit=1, B enter=1, Required Preparing=1, pending snapshot=1, then " +
                "Completed(B)=1/Succeeded, Submitted(A)=1, B exit=1, A enter=1, " +
                "B Released, Completed(A)=1/Succeeded and baseline A restored.",
                this);

            requestB.RequestActivity();

            double pendingDeadline = Time.realtimeSinceStartupAsDouble + pendingTimeoutSeconds;
            while (_requestBCompleted == 0 &&
                   Time.realtimeSinceStartupAsDouble < pendingDeadline)
            {
                TryCaptureActivityBOccurrence();
                if (HasCompletePendingEvidence())
                {
                    break;
                }

                yield return null;
            }

            TryCaptureActivityBOccurrence();
            if (!HasCompletePendingEvidence())
            {
                if (_requestBCompleted > 0)
                {
                    RecordFail("Activity B completed before the controlled pending state was fully proved.");
                }
                else if (!_requestBAccepted)
                {
                    RecordBlocked("Activity B did not emit the required Submitted evidence; the contract was not validly accepted.");
                }
                else
                {
                    RecordFail(BuildPendingEvidenceIssue());
                }

                yield return FinishAfterCleanup();
                yield break;
            }

            _pendingProved = true;
            _pendingSequence = NextSequence();
            _pendingReadinessRevision = readinessEventsB.LastSnapshot.Revision;
            ValidatePendingOrder();

            _releaseIssued = true;
            _releaseSequence = NextSequence();
            readinessParticipantB.CompletePreparation();

            double terminalDeadline = Time.realtimeSinceStartupAsDouble + terminalTimeoutSeconds;
            while (!HasTerminalBEvidence() &&
                   Time.realtimeSinceStartupAsDouble < terminalDeadline)
            {
                yield return null;
            }

            ValidateTerminalB();
            yield return FinishAfterCleanup();
        }

        private void OnDisable()
        {
            if (!_scenarioRunning || _cleanupFinished)
            {
                ReleaseEvidenceBindings();
                return;
            }

            if (readinessParticipantB != null &&
                readinessParticipantB.State == ActivityReadinessParticipantState.Preparing)
            {
                readinessParticipantB.CompletePreparation();
            }

            bool requiresFreshBoot =
                requestB == null || requestA == null ||
                requestB.IsRequestInFlight || requestA.IsRequestInFlight ||
                lifecycleA == null || lifecycleB == null ||
                !lifecycleA.IsActivityContentActive || lifecycleB.IsActivityContentActive ||
                !HasRestoredAReadiness();

            _freshBootRequired = requiresFreshBoot;
            if (_requestBAccepted)
            {
                RecordFail("Scenario interruption occurred after Activity B request acceptance.");
            }
            else
            {
                RecordBlocked("Scenario interruption occurred before the controlled contract could be exercised.");
            }

            ReleaseEvidenceBindings();
            PublishVerdict(false, requiresFreshBoot
                ? "Interruption left runtime state unverified; FreshBootRequired."
                : "Interruption containment completed, but terminal restoration was not observed.");
        }

        private bool TryValidateSerializedComposition(out string issue)
        {
            if (activityA == null || activityB == null || contributionA == null || contributionB == null ||
                lifecycleA == null || lifecycleB == null || requestB == null || requestA == null ||
                readinessParticipantB == null || readinessEventsB == null)
            {
                issue = "Required scene-authored references are missing.";
                return false;
            }

            if (ReferenceEquals(activityA, activityB) ||
                !activityA.HasValidActivityId || !activityB.HasValidActivityId ||
                !string.Equals(activityA.ActivityId.StableText, expectedActivityAId, StringComparison.Ordinal) ||
                !string.Equals(activityB.ActivityId.StableText, expectedActivityBId, StringComparison.Ordinal))
            {
                issue = "Activity A/B authored identities do not match the predeclared distinct definitions.";
                return false;
            }

            if (!ReferenceEquals(contributionA.Activity, activityA) ||
                !ReferenceEquals(contributionB.Activity, activityB) ||
                !ReferenceEquals(requestB.TargetActivity, activityB) ||
                !ReferenceEquals(requestA.TargetActivity, activityA))
            {
                issue = "Contribution or request targets do not reference the declared Activity definitions.";
                return false;
            }

            if (!IsInsideContribution(lifecycleA.transform, contributionA.transform) ||
                !IsInsideContribution(lifecycleB.transform, contributionB.transform))
            {
                issue = "Lifecycle probes are not inside their declared Activity contribution roots.";
                return false;
            }

            ActivityReadinessParticipant[] participants =
                contributionB.GetComponentsInChildren<ActivityReadinessParticipant>(true);
            if (participants.Length != 1 || !ReferenceEquals(participants[0], readinessParticipantB))
            {
                issue = $"The declared B content root must contain exactly one Route-scoped readiness participant, but found '{participants.Length}'.";
                return false;
            }

            ActivityReadinessEvents[] observers =
                contributionB.GetComponentsInChildren<ActivityReadinessEvents>(true);
            if (observers.Length != 1 || !ReferenceEquals(observers[0], readinessEventsB))
            {
                issue = $"The declared B content root must contain exactly one Route-scoped readiness observer, but found '{observers.Length}'.";
                return false;
            }

            if (!string.Equals(readinessParticipantB.ParticipantId, expectedParticipantId, StringComparison.Ordinal) ||
                readinessParticipantB.Requiredness != ActivityContentExecutionRequiredness.Required)
            {
                issue = "Activity B readiness participant identity or Requiredness differs from the expectation.";
                return false;
            }

            if (activityA.EntryReadinessPolicy != ActivityEntryReadinessPolicy.ObserveOnly ||
                activityB.EntryReadinessPolicy != ActivityEntryReadinessPolicy.WaitVisible ||
                activityB.TransitionGateMode != TransitionGateMode.InputInteractionAndGameplay)
            {
                issue = "Activity authoring must be A=ObserveOnly and B=WaitVisible with InputInteractionAndGameplay gate.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool IsInsideContribution(Transform candidate, Transform contributionRoot)
        {
            return candidate != null && contributionRoot != null &&
                (ReferenceEquals(candidate, contributionRoot) || candidate.IsChildOf(contributionRoot));
        }

        private bool TryValidateBaselineComposition(out string issue)
        {
            if (!requestB.HasActivityRuntimeBinding || !requestA.HasActivityRuntimeBinding)
            {
                issue = $"Activity trigger binding is missing. B='{requestB.ActivityRuntimeBindingDiagnostic}' A='{requestA.ActivityRuntimeBindingDiagnostic}'.";
                return false;
            }

            if (requestB.IsRequestInFlight || requestA.IsRequestInFlight)
            {
                issue = "A residual Activity request is already in flight.";
                return false;
            }

            if (!lifecycleA.IsActivityContentActive ||
                !ReferenceEquals(lifecycleA.ActiveActivity, activityA) ||
                lifecycleB.IsActivityContentActive)
            {
                issue = "Activity A is not the sole active local Activity content baseline.";
                return false;
            }

            if (readinessParticipantB.Occurrence <= 0 ||
                readinessParticipantB.State == ActivityReadinessParticipantState.Idle ||
                readinessParticipantB.State == ActivityReadinessParticipantState.Released)
            {
                issue = "The single Route-scoped readiness participant has not materialized its startup Activity A occurrence.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryValidateBaseline(out string issue)
        {
            if (!TryValidateBaselineComposition(out issue))
            {
                return false;
            }

            ActivityReadinessSnapshot snapshot = readinessEventsB.LastSnapshot;
            if (readinessParticipantB.State != ActivityReadinessParticipantState.Completed ||
                !ReferenceEquals(snapshot.Activity, activityA) ||
                !snapshot.HasOccurrence ||
                snapshot.Occurrence != readinessParticipantB.Occurrence ||
                !snapshot.IsReady || snapshot.IsPreparing ||
                snapshot.ParticipantCount != 1 || snapshot.RequiredCount != 1 ||
                snapshot.OptionalCount != 0 || snapshot.PendingCount != 0 ||
                snapshot.CompletedCount != 1 || snapshot.FailedCount != 0)
            {
                issue =
                    "Startup Activity A readiness baseline is not completed and observable. " +
                    $"state='{readinessParticipantB.State}' activity='{snapshot.Activity?.ActivityName}' " +
                    $"participantOccurrence='{readinessParticipantB.Occurrence}' snapshotOccurrence='{snapshot.Occurrence}' " +
                    $"snapshotHasOccurrence='{snapshot.HasOccurrence}' " +
                    $"participants='{snapshot.ParticipantCount}' required='{snapshot.RequiredCount}' " +
                    $"pending='{snapshot.PendingCount}' completed='{snapshot.CompletedCount}' " +
                    $"failed='{snapshot.FailedCount}' ready='{snapshot.IsReady}'.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private void CaptureBaselineEvidence()
        {
            _initialReadinessRevision = readinessEventsB.LastRevision;
            _initialParticipantOccurrence = readinessParticipantB.Occurrence;
            _aEnterBaseline = lifecycleA.EnterCount;
            _aExitBaseline = lifecycleA.ExitCount;
            _bEnterBaseline = lifecycleB.EnterCount;
            _bExitBaseline = lifecycleB.ExitCount;
        }

        private void BindEvidence()
        {
            _requestBBinding = requestB.SubscribeRequestEvents(OnRequestBEvent);
            _requestABinding = requestA.SubscribeRequestEvents(OnRequestAEvent);
            lifecycleA.Entered += OnLifecycleEntered;
            lifecycleA.Exited += OnLifecycleExited;
            lifecycleB.Entered += OnLifecycleEntered;
            lifecycleB.Exited += OnLifecycleExited;
            readinessParticipantB.PreparationReleased.AddListener(OnParticipantReleased);
        }

        private void ReleaseEvidenceBindings()
        {
            _requestBBinding?.Dispose();
            _requestBBinding = null;
            _requestABinding?.Dispose();
            _requestABinding = null;

            if (lifecycleA != null)
            {
                lifecycleA.Entered -= OnLifecycleEntered;
                lifecycleA.Exited -= OnLifecycleExited;
            }

            if (lifecycleB != null)
            {
                lifecycleB.Entered -= OnLifecycleEntered;
                lifecycleB.Exited -= OnLifecycleExited;
            }

            if (readinessParticipantB != null)
            {
                readinessParticipantB.PreparationReleased.RemoveListener(OnParticipantReleased);
            }
        }

        private void OnParticipantReleased()
        {
            if (_activityBOccurrence > 0 &&
                readinessParticipantB != null &&
                readinessParticipantB.Occurrence == _activityBOccurrence)
            {
                if (_activityBReleaseCount > 0)
                {
                    RecordFail("Activity B occurrence emitted PreparationReleased more than once.");
                }

                _activityBReleaseCount++;
                _activityBReleaseObserved = true;
                _releasedBSequence = NextSequence();
            }
        }

        private void OnRequestBEvent(ActivityRequestTriggerEvent evidence)
        {
            if (evidence == null || !ReferenceEquals(evidence.Trigger, requestB) ||
                !ReferenceEquals(evidence.TargetActivity, activityB) || evidence.ClearsActivity)
            {
                RecordRequestBDivergence("Received Activity B request evidence from an unexpected source or target.");
                return;
            }

            if (evidence.IsSubmitted)
            {
                if (_requestBSubmitted > 0 || _requestBCompleted > 0)
                {
                    RecordFail("Submitted(B) was duplicated or observed after Completed(B).");
                }

                _requestBSubmitted++;
                _requestBAccepted = true;
                _submittedBSequence = NextSequence();
                return;
            }

            if (evidence.IsCompleted)
            {
                if (_requestBSubmitted != 1 || _requestBCompleted > 0)
                {
                    RecordFail("Completed(B) was duplicated or did not follow exactly one Submitted(B).");
                }

                if (!_releaseIssued)
                {
                    RecordFail("Completed(B) occurred before QA released the controlled readiness condition.");
                }

                _requestBCompleted++;
                _requestBTerminal = evidence;
                _completedBSequence = NextSequence();
                return;
            }

            RecordRequestBDivergence($"Unexpected Activity B request phase '{evidence.Phase}'.");
        }

        private void OnRequestAEvent(ActivityRequestTriggerEvent evidence)
        {
            if (evidence == null || !ReferenceEquals(evidence.Trigger, requestA) ||
                !ReferenceEquals(evidence.TargetActivity, activityA) || evidence.ClearsActivity)
            {
                RecordFail("Received cleanup Activity A evidence from an unexpected source or target.");
                return;
            }

            if (evidence.IsSubmitted)
            {
                if (_requestASubmitted > 0 || _requestACompleted > 0)
                {
                    RecordFail("Submitted(A) was duplicated or observed after Completed(A).");
                }

                _requestASubmitted++;
                _submittedASequence = NextSequence();
                return;
            }

            if (evidence.IsCompleted)
            {
                if (_requestASubmitted != 1 || _requestACompleted > 0)
                {
                    RecordFail("Completed(A) was duplicated or did not follow exactly one Submitted(A).");
                }

                _requestACompleted++;
                _requestATerminal = evidence;
                _completedASequence = NextSequence();
                return;
            }

            RecordFail($"Unexpected cleanup Activity A request phase '{evidence.Phase}'.");
        }

        private void OnLifecycleEntered(
            QaNew002ActivityLifecycleProbe probe,
            ActivityContentLifecycleContext context)
        {
            if (ReferenceEquals(probe, lifecycleB) && ReferenceEquals(context.Activity, activityB))
            {
                _enterBSequence = NextSequence();
                return;
            }

            if (ReferenceEquals(probe, lifecycleA) && ReferenceEquals(context.Activity, activityA))
            {
                _enterASequence = NextSequence();
            }
        }

        private void OnLifecycleExited(
            QaNew002ActivityLifecycleProbe probe,
            ActivityContentLifecycleContext context)
        {
            if (ReferenceEquals(probe, lifecycleA) && ReferenceEquals(context.Activity, activityA))
            {
                _exitASequence = NextSequence();
                return;
            }

            if (ReferenceEquals(probe, lifecycleB) && ReferenceEquals(context.Activity, activityB))
            {
                _exitBSequence = NextSequence();
            }
        }

        private bool HasCompletePendingEvidence()
        {
            ActivityReadinessSnapshot snapshot = readinessEventsB.LastSnapshot;
            return _requestBSubmitted == 1 && _requestBCompleted == 0 &&
                requestB.IsRequestInFlight &&
                lifecycleA.ExitCount - _aExitBaseline == 1 &&
                lifecycleB.EnterCount - _bEnterBaseline == 1 &&
                !lifecycleA.IsActivityContentActive &&
                lifecycleB.IsActivityContentActive &&
                ReferenceEquals(lifecycleB.ActiveActivity, activityB) &&
                readinessParticipantB.State == ActivityReadinessParticipantState.Preparing &&
                readinessParticipantB.Occurrence > 0 &&
                readinessParticipantB.Occurrence > _initialParticipantOccurrence &&
                _activityBOccurrence == readinessParticipantB.Occurrence &&
                readinessEventsB.LastRevision > _initialReadinessRevision &&
                ReferenceEquals(snapshot.Activity, activityB) &&
                snapshot.HasOccurrence &&
                snapshot.Occurrence == _activityBOccurrence &&
                snapshot.ParticipantCount == 1 && snapshot.RequiredCount == 1 &&
                snapshot.OptionalCount == 0 && snapshot.PendingCount == 1 &&
                snapshot.CompletedCount == 0 && snapshot.FailedCount == 0 &&
                snapshot.IsPreparing && !snapshot.IsReady;
        }

        private void TryCaptureActivityBOccurrence()
        {
            if (_activityBOccurrence > 0 ||
                !_requestBAccepted ||
                readinessParticipantB == null ||
                lifecycleA == null ||
                lifecycleB == null ||
                lifecycleA.IsActivityContentActive ||
                !lifecycleB.IsActivityContentActive ||
                !ReferenceEquals(lifecycleB.ActiveActivity, activityB))
            {
                return;
            }

            int participantOccurrence = readinessParticipantB.Occurrence;
            if (participantOccurrence > _initialParticipantOccurrence)
            {
                _activityBOccurrence = participantOccurrence;
            }
        }

        private string BuildPendingEvidenceIssue()
        {
            ActivityReadinessSnapshot snapshot = readinessEventsB.LastSnapshot;
            return
                "Controlled pending evidence was incomplete before the local deadline. " +
                $"submittedB='{_requestBSubmitted}' completedB='{_requestBCompleted}' inFlight='{requestB.IsRequestInFlight}' " +
                $"exitA='{lifecycleA.ExitCount - _aExitBaseline}' enterB='{lifecycleB.EnterCount - _bEnterBaseline}' " +
                $"participantState='{readinessParticipantB.State}' occurrence='{readinessParticipantB.Occurrence}' " +
                $"capturedBOccurrence='{_activityBOccurrence}' snapshotActivity='{snapshot.Activity?.ActivityName}' " +
                $"snapshotOccurrence='{snapshot.Occurrence}' snapshotHasOccurrence='{snapshot.HasOccurrence}' " +
                $"baselineOccurrence='{_initialParticipantOccurrence}' revision='{readinessEventsB.LastRevision}' " +
                $"baselineRevision='{_initialReadinessRevision}' participants='{snapshot.ParticipantCount}' " +
                $"required='{snapshot.RequiredCount}' optional='{snapshot.OptionalCount}' pending='{snapshot.PendingCount}' " +
                $"completed='{snapshot.CompletedCount}' failed='{snapshot.FailedCount}' ready='{snapshot.IsReady}'.";
        }

        private void ValidatePendingOrder()
        {
            if (!(_submittedBSequence > 0 &&
                  _exitASequence > _submittedBSequence &&
                  _enterBSequence > _exitASequence &&
                  _pendingSequence > _enterBSequence))
            {
                RecordFail(
                    "Public causal order diverged before readiness release. " +
                    $"submittedB='{_submittedBSequence}' exitA='{_exitASequence}' " +
                    $"enterB='{_enterBSequence}' pending='{_pendingSequence}'.");
            }
        }

        private bool HasTerminalBEvidence()
        {
            ActivityReadinessSnapshot snapshot = readinessEventsB.LastSnapshot;
            return _requestBCompleted == 1 && !requestB.IsRequestInFlight &&
                readinessParticipantB.State == ActivityReadinessParticipantState.Completed &&
                readinessParticipantB.Occurrence == _activityBOccurrence &&
                readinessEventsB.LastRevision > _pendingReadinessRevision &&
                ReferenceEquals(snapshot.Activity, activityB) &&
                snapshot.HasOccurrence &&
                snapshot.Occurrence == _activityBOccurrence &&
                snapshot.IsReady && !snapshot.IsPreparing &&
                snapshot.ParticipantCount == 1 && snapshot.RequiredCount == 1 &&
                snapshot.OptionalCount == 0 && snapshot.PendingCount == 0 &&
                snapshot.CompletedCount == 1 && snapshot.FailedCount == 0;
        }

        private void ValidateTerminalB()
        {
            if (_requestBSubmitted != 1 || _requestBCompleted != 1 || _requestBTerminal == null)
            {
                RecordFail($"Expected exactly one Submitted(B) and one Completed(B). submitted='{_requestBSubmitted}' completed='{_requestBCompleted}'.");
                return;
            }

            if (!_pendingProved || !_releaseIssued ||
                _requestBTerminal.Outcome != FlowRequestOutcome.Succeeded ||
                requestB.IsRequestInFlight ||
                requestB.LastEventPhase != FlowRequestEventPhase.Completed ||
                requestB.LastOutcome != FlowRequestOutcome.Succeeded)
            {
                RecordFail(
                    "Activity B terminal evidence was not successful after explicit release. " +
                    $"pendingProved='{_pendingProved}' releaseIssued='{_releaseIssued}' outcome='{_requestBTerminal.Outcome}' " +
                    $"inFlight='{requestB.IsRequestInFlight}' phase='{requestB.LastEventPhase}' snapshotOutcome='{requestB.LastOutcome}'.");
                return;
            }

            if (!HasTerminalBEvidence())
            {
                ActivityReadinessSnapshot snapshot = readinessEventsB.LastSnapshot;
                RecordFail(
                    "Ready snapshot or participant terminal evidence for Activity B was incomplete. " +
                    $"state='{readinessParticipantB.State}' participantOccurrence='{readinessParticipantB.Occurrence}' " +
                    $"expectedOccurrence='{_activityBOccurrence}' snapshotOccurrence='{snapshot.Occurrence}' " +
                    $"snapshotHasOccurrence='{snapshot.HasOccurrence}' revision='{snapshot.Revision}' pending='{snapshot.PendingCount}' " +
                    $"completed='{snapshot.CompletedCount}' failed='{snapshot.FailedCount}' ready='{snapshot.IsReady}'.");
            }

            if (!(_completedBSequence > _releaseSequence && _releaseSequence > _pendingSequence))
            {
                RecordFail(
                    "Completed(B) did not follow pending proof and explicit release. " +
                    $"pending='{_pendingSequence}' release='{_releaseSequence}' completedB='{_completedBSequence}'.");
            }
        }

        private IEnumerator FinishAfterCleanup()
        {
            yield return CleanupAndRestore();

            bool baselineRestored = IsBaselineRestored(out string restoreIssue);
            if (!baselineRestored)
            {
                _freshBootRequired = true;
                if (_requestBAccepted)
                {
                    RecordFail($"Cleanup did not restore baseline A. {restoreIssue}");
                }
                else
                {
                    RecordBlocked($"Environment cleanup could not be proven. {restoreIssue}");
                }
            }

            ReleaseEvidenceBindings();

            if (_verdict == QaVerdict.None && baselineRestored && _pendingProved)
            {
                _verdict = QaVerdict.Pass;
            }

            _cleanupFinished = true;
            _scenarioRunning = false;
            PublishVerdict(baselineRestored, restoreIssue);
        }

        private IEnumerator CleanupAndRestore()
        {
            TryCaptureActivityBOccurrence();
            if (readinessParticipantB != null &&
                readinessParticipantB.State == ActivityReadinessParticipantState.Preparing)
            {
                _releaseIssued = true;
                readinessParticipantB.CompletePreparation();
            }

            if (requestB != null && requestB.IsRequestInFlight)
            {
                double bDeadline = Time.realtimeSinceStartupAsDouble + cleanupTimeoutSeconds;
                while (requestB.IsRequestInFlight && Time.realtimeSinceStartupAsDouble < bDeadline)
                {
                    yield return null;
                }
            }

            if (requestB != null && requestB.IsRequestInFlight)
            {
                _freshBootRequired = true;
                yield break;
            }

            bool bIsActive = lifecycleB != null && lifecycleB.IsActivityContentActive;
            bool aIsActive = lifecycleA != null && lifecycleA.IsActivityContentActive;
            if (bIsActive && !aIsActive && requestA != null && requestA.HasActivityRuntimeBinding &&
                !requestA.IsRequestInFlight)
            {
                _requestAInvoked = true;
                requestA.RequestActivity();

                double aDeadline = Time.realtimeSinceStartupAsDouble + cleanupTimeoutSeconds;
                while (requestA.IsRequestInFlight && Time.realtimeSinceStartupAsDouble < aDeadline)
                {
                    yield return null;
                }
            }

            if (requestA != null && requestA.IsRequestInFlight)
            {
                _freshBootRequired = true;
                yield break;
            }

            if (_requestAInvoked)
            {
                ValidateCleanupA();
            }

            if (lifecycleA != null && lifecycleA.IsActivityContentActive &&
                readinessParticipantB != null &&
                readinessParticipantB.State == ActivityReadinessParticipantState.Preparing)
            {
                readinessParticipantB.CompletePreparation();
            }

            double restoredReadinessDeadline =
                Time.realtimeSinceStartupAsDouble + cleanupTimeoutSeconds;
            while (lifecycleA != null && lifecycleA.IsActivityContentActive &&
                   !HasRestoredAReadiness() &&
                   Time.realtimeSinceStartupAsDouble < restoredReadinessDeadline)
            {
                yield return null;
            }
        }

        private void ValidateCleanupA()
        {
            if (_requestASubmitted != 1 || _requestACompleted != 1 || _requestATerminal == null ||
                _requestATerminal.Outcome != FlowRequestOutcome.Succeeded ||
                requestA.LastEventPhase != FlowRequestEventPhase.Completed ||
                requestA.LastOutcome != FlowRequestOutcome.Succeeded)
            {
                RecordFail(
                    "Activity A cleanup request did not produce one successful Submitted/Completed pair. " +
                    $"submitted='{_requestASubmitted}' completed='{_requestACompleted}' " +
                    $"outcome='{(_requestATerminal != null ? _requestATerminal.Outcome : FlowRequestOutcome.None)}'.");
            }

            if (!(_submittedASequence > _completedBSequence &&
                  _releasedBSequence > _submittedASequence &&
                  _exitBSequence > _releasedBSequence &&
                  _enterASequence > _exitBSequence &&
                  _completedASequence > _enterASequence))
            {
                RecordFail(
                    "Cleanup causal order diverged from the public runtime lifecycle. " +
                    $"completedB='{_completedBSequence}' submittedA='{_submittedASequence}' " +
                    $"releasedB='{_releasedBSequence}' exitB='{_exitBSequence}' " +
                    $"enterA='{_enterASequence}' completedA='{_completedASequence}'.");
            }
        }

        private bool IsBaselineRestored(out string issue)
        {
            if (requestB == null || requestA == null || requestB.IsRequestInFlight || requestA.IsRequestInFlight)
            {
                issue = "At least one Activity request remains in flight; FreshBootRequired.";
                return false;
            }

            if (lifecycleA == null || lifecycleB == null ||
                !lifecycleA.IsActivityContentActive ||
                !ReferenceEquals(lifecycleA.ActiveActivity, activityA) ||
                lifecycleB.IsActivityContentActive)
            {
                issue = "Activity A active/B inactive baseline is not publicly observable; FreshBootRequired.";
                return false;
            }

            if (_requestBAccepted)
            {
                if (lifecycleA.ExitCount - _aExitBaseline != 1 ||
                    lifecycleB.EnterCount - _bEnterBaseline != 1)
                {
                    issue = "Activity B transition lifecycle cardinality differs from one A exit and one B enter.";
                    return false;
                }

                if (lifecycleB.ExitCount - _bExitBaseline != 1 ||
                    lifecycleA.EnterCount - _aEnterBaseline != 1)
                {
                    issue = "Cleanup lifecycle cardinality differs from one B exit and one A reentry.";
                    return false;
                }

                if (!_activityBReleaseObserved || _activityBReleaseCount != 1)
                {
                    issue =
                        "Activity B occurrence did not emit exactly one public PreparationReleased callback. " +
                        $"count='{_activityBReleaseCount}'.";
                    return false;
                }

                if (!_requestAInvoked)
                {
                    issue = "Activity B was exercised, but the supported Activity A restoration request was not invoked.";
                    return false;
                }
                if (!HasRestoredAReadiness())
                {
                    issue = "Restored Activity A readiness did not return to the completed known baseline.";
                    return false;
                }
            }

            issue = string.Empty;
            return true;
        }

        private bool HasRestoredAReadiness()
        {
            if (readinessParticipantB == null || readinessEventsB == null ||
                readinessParticipantB.State != ActivityReadinessParticipantState.Completed)
            {
                return false;
            }

            ActivityReadinessSnapshot snapshot = readinessEventsB.LastSnapshot;
            return ReferenceEquals(snapshot.Activity, activityA) &&
                snapshot.HasOccurrence &&
                snapshot.Occurrence == readinessParticipantB.Occurrence &&
                snapshot.Occurrence > _activityBOccurrence &&
                snapshot.IsReady && !snapshot.IsPreparing &&
                snapshot.ParticipantCount == 1 && snapshot.RequiredCount == 1 &&
                snapshot.OptionalCount == 0 && snapshot.PendingCount == 0 &&
                snapshot.CompletedCount == 1 && snapshot.FailedCount == 0;
        }

        private void RecordRequestBDivergence(string issue)
        {
            if (_requestBAccepted)
            {
                RecordFail(issue);
            }
            else
            {
                RecordBlocked(issue);
            }
        }

        private void RecordFail(string issue) => RecordFirstDivergence(QaVerdict.Fail, issue);

        private void RecordBlocked(string issue) => RecordFirstDivergence(QaVerdict.Blocked, issue);

        private void RecordFirstDivergence(QaVerdict verdict, string issue)
        {
            if (_verdict != QaVerdict.None)
            {
                return;
            }

            _verdict = verdict;
            _firstCausalDivergence = issue ?? string.Empty;
        }

        private int NextSequence() => ++_causalSequence;

        private void PublishVerdict(bool baselineRestored, string cleanupIssue)
        {
            string status = _verdict switch
            {
                QaVerdict.Pass => "Passed",
                QaVerdict.Blocked => "Blocked",
                _ => "Failed"
            };
            ActivityReadinessSnapshot snapshot = readinessEventsB != null
                ? readinessEventsB.LastSnapshot
                : default;
            string message =
                $"[{ScenarioId}] status='{status}' verdict='{_verdict.ToString().ToUpperInvariant()}' " +
                $"submittedB='{_requestBSubmitted}' completedB='{_requestBCompleted}' " +
                $"submittedA='{_requestASubmitted}' completedA='{_requestACompleted}' " +
                $"baselineANormalized='{_baselineAReadinessNormalized}' pendingProved='{_pendingProved}' " +
                $"releaseIssued='{_releaseIssued}' activityBReleased='{_activityBReleaseObserved}' " +
                $"participantState='{(readinessParticipantB != null ? readinessParticipantB.State : ActivityReadinessParticipantState.Idle)}' " +
                $"participantOccurrence='{(readinessParticipantB != null ? readinessParticipantB.Occurrence : 0)}' " +
                $"activityBOccurrence='{_activityBOccurrence}' snapshotActivity='{snapshot.Activity?.ActivityName}' " +
                $"snapshotOccurrence='{snapshot.Occurrence}' snapshotHasOccurrence='{snapshot.HasOccurrence}' " +
                $"readinessRevision='{snapshot.Revision}' required='{snapshot.RequiredCount}' pending='{snapshot.PendingCount}' " +
                $"completed='{snapshot.CompletedCount}' failed='{snapshot.FailedCount}' ready='{snapshot.IsReady}' " +
                $"baselineRestored='{baselineRestored}' cleanup='{(_freshBootRequired ? "FreshBootRequired" : "BaselineRestored")}' " +
                $"firstDivergence='{SanitizeDiagnostic(_firstCausalDivergence)}' " +
                $"cleanupIssue='{SanitizeDiagnostic(cleanupIssue)}'.";

            switch (_verdict)
            {
                case QaVerdict.Pass:
                    Debug.Log(message, this);
                    break;
                case QaVerdict.Blocked:
                    Debug.LogWarning(message, this);
                    break;
                default:
                    Debug.LogError(message, this);
                    break;
            }
        }

        private static string SanitizeDiagnostic(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}

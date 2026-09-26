using System;
using System.Collections;
using Immersive.Foundation.Events;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEngine;

namespace Immersive.QaFramework.New001
{
    /// <summary>
    /// Concrete first vertical slice for ADR-001. This component deliberately owns
    /// only QA-NEW-001 and is not a reusable Scenario or Runner abstraction.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QaNew001ObjectResetScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-NEW-001";
        private const float PositionTolerance = 0.0001f;
        private const float RotationToleranceDegrees = 0.01f;
        private const float ScaleTolerance = 0.0001f;

        [Header("Scene-authored contract")]
        [SerializeField] private ObjectResetTrigger resetTrigger;
        [SerializeField] private UnityResetSubjectAdapter subjectAdapter;
        [SerializeField] private UnityTransformResetParticipant transformParticipant;
        [SerializeField] private Transform target;
        [SerializeField] private string expectedSubjectId = "qa-new-001.object-reset.subject";

        [Header("Known baseline B")]
        [SerializeField] private Vector3 expectedBaselineLocalPosition = new Vector3(1f, 2f, 3f);
        [SerializeField] private Vector3 expectedBaselineLocalEulerAngles = Vector3.zero;
        [SerializeField] private Vector3 expectedBaselineLocalScale = Vector3.one;

        [Header("QA-owned perturbation")]
        [SerializeField] private Vector3 perturbedLocalPosition = new Vector3(4f, -3f, 8f);
        [SerializeField] private Vector3 perturbedLocalEulerAngles = new Vector3(25f, 45f, 15f);
        [SerializeField] private Vector3 perturbedLocalScale = new Vector3(1.4f, 0.6f, 1.8f);

        [Header("Local Scenario timeout")]
        [SerializeField, Min(0.1f)] private float readinessTimeoutSeconds = 10f;
        [SerializeField, Min(0.1f)] private float terminalTimeoutSeconds = 5f;

        private IEventBinding _eventBinding;
        private ObjectResetTriggerEvent _terminalEvent;
        private Vector3 _baselineLocalPosition;
        private Quaternion _baselineLocalRotation;
        private Vector3 _baselineLocalScale;
        private string _firstCausalDivergence = string.Empty;
        private QaVerdict _verdict;
        private int _submittedCount;
        private int _completedCount;
        private bool _executionStarted;
        private bool _baselineCaptured;
        private bool _mutationApplied;
        private bool _requestInvoked;
        private bool _frameworkRestoredBaseline;
        private bool _cleanupFinished;

        private enum QaVerdict
        {
            None,
            Pass,
            Fail,
            Blocked
        }

        private void Start()
        {
            if (_executionStarted)
            {
                return;
            }

            _executionStarted = true;
            StartCoroutine(ExecuteScenario());
        }

        private void OnDisable()
        {
            if (!_executionStarted || _cleanupFinished)
            {
                return;
            }

            StopAllCoroutines();
            if (_requestInvoked)
            {
                RecordFail("Scenario execution was interrupted after the Object Reset request was invoked.");
            }
            else
            {
                RecordBlocked("Scenario execution was interrupted before the Object Reset contract could be exercised.");
            }

            FinishAfterCleanup();
        }

        private IEnumerator ExecuteScenario()
        {
            if (!TryValidateSerializedComposition(out string compositionIssue))
            {
                RecordBlocked(compositionIssue);
                FinishAfterCleanup();
                yield break;
            }

            double readinessDeadline = Time.realtimeSinceStartupAsDouble + readinessTimeoutSeconds;
            string readinessIssue;
            while (!IsReady(out readinessIssue) &&
                   Time.realtimeSinceStartupAsDouble < readinessDeadline)
            {
                yield return null;
            }

            if (!IsReady(out readinessIssue))
            {
                RecordBlocked($"Readiness was not reached before the local timeout. {readinessIssue}");
                FinishAfterCleanup();
                yield break;
            }

            if (!TryCaptureKnownBaseline(out string baselineIssue))
            {
                RecordBlocked(baselineIssue);
                FinishAfterCleanup();
                yield break;
            }

            transformParticipant.CaptureBaseline();
            _eventBinding = resetTrigger.SubscribeRequestEvents(OnResetRequestEvent);

            ApplyPerturbation();
            if (!HasExpectedPerturbation())
            {
                RecordFail("The QA-owned position, rotation and scale perturbation did not diverge from baseline B as declared.");
                FinishAfterCleanup();
                yield break;
            }

            _requestInvoked = true;
            resetTrigger.RequestObjectReset();

            double terminalDeadline = Time.realtimeSinceStartupAsDouble + terminalTimeoutSeconds;
            while (_completedCount == 0 &&
                   Time.realtimeSinceStartupAsDouble < terminalDeadline)
            {
                yield return null;
            }

            if (_submittedCount == 0)
            {
                RecordFail("The supported Object Reset action did not emit the expected Submitted evidence.");
            }
            else if (_completedCount == 0)
            {
                RecordFail("The accepted Object Reset request did not emit Completed before the local timeout.");
            }

            ValidateTerminalEvidence();

            _frameworkRestoredBaseline = PoseMatchesBaseline();
            if (!_frameworkRestoredBaseline)
            {
                // This failure is intentionally recorded before containment restoration.
                RecordFail("The framework completed Object Reset without restoring target pose to baseline B.");
            }

            FinishAfterCleanup();
        }

        private bool TryValidateSerializedComposition(out string issue)
        {
            if (resetTrigger == null || subjectAdapter == null ||
                transformParticipant == null || target == null)
            {
                issue = "Required scene-authored references are missing.";
                return false;
            }

            if (!ReferenceEquals(resetTrigger.TargetSubjectAdapter, subjectAdapter))
            {
                issue = "ObjectResetTrigger does not explicitly target the declared UnityResetSubjectAdapter.";
                return false;
            }

            if (!ReferenceEquals(transformParticipant.transform, target) ||
                !ReferenceEquals(subjectAdapter.gameObject, target.gameObject) ||
                !ReferenceEquals(resetTrigger.gameObject, target.gameObject))
            {
                issue = "QA-NEW-001 requires its trigger, adapter, single participant and target on the exact authored subject GameObject.";
                return false;
            }

            if (subjectAdapter.Scope != ResetSubjectScope.Route)
            {
                issue = $"Subject scope must be Route, but was '{subjectAdapter.Scope}'.";
                return false;
            }

            if (subjectAdapter.IdGeneration != UnityResetSubjectIdGenerationMode.AuthoredStableId)
            {
                issue = $"Subject identity must be AuthoredStableId, but was '{subjectAdapter.IdGeneration}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(expectedSubjectId) ||
                !string.Equals(resetTrigger.AuthoringResetSubjectId, expectedSubjectId, StringComparison.Ordinal))
            {
                issue = "The authored Object Reset subject identity does not match the Scenario expectation.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool IsReady(out string issue)
        {
            if (resetTrigger == null || subjectAdapter == null)
            {
                issue = "Scene-authored endpoints are missing.";
                return false;
            }

            if (!resetTrigger.HasResetExecutionRuntimeBinding)
            {
                issue = $"Object Reset execution binding is missing. diagnostic='{resetTrigger.ResetExecutionRuntimeBindingDiagnostic}'.";
                return false;
            }

            if (!subjectAdapter.HasResetRegistrationRuntimeBinding)
            {
                issue = $"Reset registration binding is missing. diagnostic='{subjectAdapter.ResetRegistrationRuntimeBindingDiagnostic}'.";
                return false;
            }

            if (!subjectAdapter.IsRegistered)
            {
                issue = "The scene-authored Reset subject is not registered for the current Route owner.";
                return false;
            }

            if (subjectAdapter.RegisteredParticipantCount != 1)
            {
                issue = $"Expected exactly one registered participant, but found '{subjectAdapter.RegisteredParticipantCount}'.";
                return false;
            }

            if (!subjectAdapter.SubjectId.IsValid ||
                !string.Equals(subjectAdapter.SubjectId.StableText, expectedSubjectId, StringComparison.Ordinal))
            {
                issue = "The registered Reset subject identity differs from the predeclared identity.";
                return false;
            }

            if (resetTrigger.IsRequestInFlight)
            {
                issue = "A residual Object Reset request is already in flight.";
                return false;
            }

            if (resetTrigger.HasLastResult)
            {
                issue = "Residual Object Reset result state is present before the Scenario action.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryCaptureKnownBaseline(out string issue)
        {
            if (!Approximately(target.localPosition, expectedBaselineLocalPosition, PositionTolerance) ||
                Quaternion.Angle(target.localRotation, Quaternion.Euler(expectedBaselineLocalEulerAngles)) > RotationToleranceDegrees ||
                !Approximately(target.localScale, expectedBaselineLocalScale, ScaleTolerance))
            {
                issue = $"Authored target pose does not match baseline B. actualPosition='{target.localPosition}' actualEuler='{target.localEulerAngles}' actualScale='{target.localScale}'.";
                return false;
            }

            _baselineLocalPosition = target.localPosition;
            _baselineLocalRotation = target.localRotation;
            _baselineLocalScale = target.localScale;
            _baselineCaptured = true;
            issue = string.Empty;
            return true;
        }

        private void ApplyPerturbation()
        {
            target.localPosition = perturbedLocalPosition;
            target.localEulerAngles = perturbedLocalEulerAngles;
            target.localScale = perturbedLocalScale;
            _mutationApplied = true;
        }

        private bool HasExpectedPerturbation()
        {
            bool valuesApplied =
                Approximately(target.localPosition, perturbedLocalPosition, PositionTolerance) &&
                Quaternion.Angle(target.localRotation, Quaternion.Euler(perturbedLocalEulerAngles)) <= RotationToleranceDegrees &&
                Approximately(target.localScale, perturbedLocalScale, ScaleTolerance);

            bool allDimensionsDiverged =
                !Approximately(target.localPosition, _baselineLocalPosition, PositionTolerance) &&
                Quaternion.Angle(target.localRotation, _baselineLocalRotation) > RotationToleranceDegrees &&
                !Approximately(target.localScale, _baselineLocalScale, ScaleTolerance);

            return valuesApplied && allDimensionsDiverged;
        }

        private void OnResetRequestEvent(ObjectResetTriggerEvent evidence)
        {
            if (evidence == null || !ReferenceEquals(evidence.Trigger, resetTrigger))
            {
                RecordFail("Received Object Reset evidence from an unexpected trigger.");
                return;
            }

            if (evidence.IsSubmitted)
            {
                if (_completedCount > 0 || _submittedCount > 0)
                {
                    RecordFail("Submitted evidence was duplicated or observed after Completed.");
                }

                _submittedCount++;
                return;
            }

            if (evidence.IsCompleted)
            {
                if (_submittedCount != 1 || _completedCount > 0)
                {
                    RecordFail("Completed evidence was duplicated or did not follow exactly one Submitted event.");
                }

                _completedCount++;
                _terminalEvent = evidence;
                return;
            }

            RecordFail($"Unexpected Object Reset evidence phase '{evidence.Phase}'.");
        }

        private void ValidateTerminalEvidence()
        {
            if (_submittedCount != 1 || _completedCount != 1 || _terminalEvent == null)
            {
                RecordFail($"Expected exactly one Submitted and one Completed event. submitted='{_submittedCount}' completed='{_completedCount}'.");
                return;
            }

            if (_terminalEvent.Outcome != FlowRequestOutcome.Succeeded ||
                !_terminalEvent.HasResult ||
                _terminalEvent.Result.Status != ResetExecutionStatus.Succeeded)
            {
                RecordFail($"Terminal Object Reset evidence was not successful. outcome='{_terminalEvent.Outcome}' hasResult='{_terminalEvent.HasResult}' status='{_terminalEvent.ResultStatus}'.");
                return;
            }

            if (resetTrigger.IsRequestInFlight ||
                resetTrigger.LastEventPhase != FlowRequestEventPhase.Completed ||
                resetTrigger.LastOutcome != FlowRequestOutcome.Succeeded ||
                !resetTrigger.HasLastResult ||
                resetTrigger.LastExecutionStatus != ResetExecutionStatus.Succeeded)
            {
                RecordFail(
                    $"Public trigger snapshot disagreed with terminal evidence. " +
                    $"inFlight='{resetTrigger.IsRequestInFlight}' phase='{resetTrigger.LastEventPhase}' " +
                    $"outcome='{resetTrigger.LastOutcome}' hasResult='{resetTrigger.HasLastResult}' " +
                    $"status='{resetTrigger.LastExecutionStatus}'.");
                return;
            }

            ResetExecutionResult result = _terminalEvent.Result;
            if (result.SubjectCount != 1 || result.SubjectSucceeded != 1 || result.SubjectFailed != 0 ||
                result.ParticipantCount != 1 || result.ParticipantSucceeded != 1 ||
                result.ParticipantSkipped != 0 || result.ParticipantFailed != 0 ||
                result.BlockingIssueCount != 0)
            {
                RecordFail($"Typed Object Reset result counts violated the predeclared expectation. {result}");
            }
        }

        private void FinishAfterCleanup()
        {
            bool cleanupSucceeded = CleanupAndVerify(out string cleanupIssue);
            if (!cleanupSucceeded)
            {
                if (_requestInvoked)
                {
                    RecordFail($"Cleanup did not restore the known baseline. {cleanupIssue}");
                }
                else
                {
                    RecordBlocked($"Environment cleanup could not be proven. {cleanupIssue}");
                }
            }

            if (_verdict == QaVerdict.None)
            {
                _verdict = QaVerdict.Pass;
            }

            _cleanupFinished = true;
            PublishVerdict(cleanupSucceeded, cleanupIssue);
        }

        private bool CleanupAndVerify(out string issue)
        {
            if (_mutationApplied && _baselineCaptured && target != null)
            {
                ContainQaOwnedState();
            }

            ReleaseEventBinding();

            if (_requestInvoked && resetTrigger != null && !resetTrigger.IsRequestInFlight)
            {
                resetTrigger.ClearLastResult();
            }

            if (_baselineCaptured && !PoseMatchesBaseline())
            {
                issue = "QA containment did not restore pose B.";
                return false;
            }

            if (resetTrigger != null && resetTrigger.IsRequestInFlight)
            {
                issue = "Object Reset remains in flight; a fresh runtime boot is required.";
                return false;
            }

            if (resetTrigger != null && resetTrigger.HasLastResult)
            {
                issue = "Object Reset result state remained after cleanup.";
                return false;
            }

            if (_requestInvoked &&
                (!resetTrigger.HasResetExecutionRuntimeBinding ||
                 !subjectAdapter.HasResetRegistrationRuntimeBinding ||
                 !subjectAdapter.IsRegistered ||
                 subjectAdapter.RegisteredParticipantCount != 1))
            {
                issue = "Framework binding or registration baseline was not preserved after execution.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private void ContainQaOwnedState()
        {
            if (!_baselineCaptured || target == null)
            {
                return;
            }

            target.localPosition = _baselineLocalPosition;
            target.localRotation = _baselineLocalRotation;
            target.localScale = _baselineLocalScale;
        }

        private void ReleaseEventBinding()
        {
            _eventBinding?.Dispose();
            _eventBinding = null;
        }

        private bool PoseMatchesBaseline()
        {
            return _baselineCaptured && target != null &&
                Approximately(target.localPosition, _baselineLocalPosition, PositionTolerance) &&
                Quaternion.Angle(target.localRotation, _baselineLocalRotation) <= RotationToleranceDegrees &&
                Approximately(target.localScale, _baselineLocalScale, ScaleTolerance);
        }

        private void RecordFail(string issue)
        {
            RecordFirstDivergence(QaVerdict.Fail, issue);
        }

        private void RecordBlocked(string issue)
        {
            RecordFirstDivergence(QaVerdict.Blocked, issue);
        }

        private void RecordFirstDivergence(QaVerdict verdict, string issue)
        {
            if (_verdict != QaVerdict.None)
            {
                return;
            }

            _verdict = verdict;
            _firstCausalDivergence = issue ?? string.Empty;
        }

        private void PublishVerdict(bool cleanupSucceeded, string cleanupIssue)
        {
            ResetExecutionResult result =
                _terminalEvent != null && _terminalEvent.HasResult
                    ? _terminalEvent.Result
                    : default;
            string status = _verdict switch
            {
                QaVerdict.Pass => "Passed",
                QaVerdict.Blocked => "Blocked",
                _ => "Failed"
            };
            string message =
                $"[{ScenarioId}] status='{status}' verdict='{_verdict.ToString().ToUpperInvariant()}' " +
                $"submitted='{_submittedCount}' completed='{_completedCount}' " +
                $"resultStatus='{(_terminalEvent != null ? _terminalEvent.ResultStatus : ResetExecutionStatus.Unknown)}' " +
                $"subjects='{result.SubjectCount}' subjectSucceeded='{result.SubjectSucceeded}' " +
                $"participants='{result.ParticipantCount}' participantSucceeded='{result.ParticipantSucceeded}' " +
                $"participantFailed='{result.ParticipantFailed}' blockingIssues='{result.BlockingIssueCount}' " +
                $"frameworkBaselineRestored='{_frameworkRestoredBaseline}' " +
                $"cleanup='{(cleanupSucceeded ? "BaselineRestored" : "FreshBootRequired")}' " +
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

        private static bool Approximately(Vector3 left, Vector3 right, float tolerance)
        {
            return (left - right).sqrMagnitude <= tolerance * tolerance;
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

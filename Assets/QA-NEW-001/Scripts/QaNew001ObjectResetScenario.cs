using System;
using System.Collections;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using Immersive.QaFramework.Certification;
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
        [SerializeField] private ResetRequestTrigger resetTrigger;
        [SerializeField] private Resettable resettable;
        [SerializeField] private UnityTransformResetParticipant transformParticipant;
        [SerializeField] private Transform target;

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

        private ResetExecutionResult _terminalResult;
        private ResetSelectionResolution _terminalResolution;
        private Vector3 _baselineLocalPosition;
        private Quaternion _baselineLocalRotation;
        private Vector3 _baselineLocalScale;
        private readonly QaCertificationRecorder _certification =
            new QaCertificationRecorder();
        private int _requestCompletedCount;
        private bool _executionStarted;
        private bool _baselineCaptured;
        private bool _mutationApplied;
        private bool _requestInvoked;
        private bool _frameworkRestoredBaseline;
        private bool _cleanupFinished;

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

            ApplyPerturbation();
            if (!HasExpectedPerturbation())
            {
                RecordFail("The QA-owned position, rotation and scale perturbation did not diverge from baseline B as declared.");
                FinishAfterCleanup();
                yield break;
            }

            _requestInvoked = true;
            resetTrigger.RequestReset();

            double terminalDeadline = Time.realtimeSinceStartupAsDouble + terminalTimeoutSeconds;
            while (resetTrigger.LastResult.Status == ResetExecutionStatus.Unknown &&
                   Time.realtimeSinceStartupAsDouble < terminalDeadline)
            {
                yield return null;
            }

            if (resetTrigger.LastResult.Status != ResetExecutionStatus.Unknown)
            {
                _terminalResult = resetTrigger.LastResult;
                _terminalResolution = resetTrigger.LastResolution;
                _requestCompletedCount = 1;
            }

            if (_requestCompletedCount == 0)
            {
                RecordFail("The accepted Reset request did not produce a terminal typed result before the local timeout.");
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
            if (resetTrigger == null || resettable == null ||
                transformParticipant == null || target == null)
            {
                issue = "Required scene-authored references are missing.";
                return false;
            }

            if (resetTrigger.Target.Kind != ResetTargetKind.Object ||
                resetTrigger.Target.ObjectTarget.ReferenceMode != ResetReferenceMode.Direct ||
                !ReferenceEquals(resetTrigger.Target.ObjectTarget.DirectResettable, resettable))
            {
                issue = "ResetRequestTrigger must target this QA's Resettable using Object/Direct.";
                return false;
            }

            if (!ReferenceEquals(transformParticipant.transform, target) ||
                !ReferenceEquals(resettable.gameObject, target.gameObject) ||
                !ReferenceEquals(resetTrigger.gameObject, target.gameObject))
            {
                issue = "QA-NEW-001 requires its trigger, Resettable, single participant and target on the exact authored subject GameObject.";
                return false;
            }

            if (resettable.Membership != ResetMembership.FollowOwner)
            {
                issue = $"QA-NEW-001 requires FollowOwner membership, but was '{resettable.Membership}'.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool IsReady(out string issue)
        {
            if (resetTrigger == null || resettable == null)
            {
                issue = "Scene-authored endpoints are missing.";
                return false;
            }

            if (!resettable.IsRegistered)
            {
                issue = "The scene-authored Resettable is not registered for the current Route owner.";
                return false;
            }

            if (!resettable.Owner.IsValid || resettable.Owner.Scope != RuntimeContentScope.Route)
            {
                issue = $"Expected a valid Route owner, but found '{resettable.Owner}'.";
                return false;
            }

            if (!resettable.RuntimeSubjectId.IsValid)
            {
                issue = "The registered Resettable has no valid runtime subject identity.";
                return false;
            }

            if (resettable.RegisteredCapabilityCount != 1)
            {
                issue = $"Expected exactly one registered Reset capability, but found '{resettable.RegisteredCapabilityCount}'.";
                return false;
            }

            if (resetTrigger.IsRequestInFlight)
            {
                issue = "A residual Object Reset request is already in flight.";
                return false;
            }

            if (resetTrigger.LastResult.Status != ResetExecutionStatus.Unknown ||
                resetTrigger.LastResolution.Status != ResetSelectionResolutionStatus.Unknown)
            {
                issue = "A residual Reset result is present before the Scenario action.";
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

        private void ValidateTerminalEvidence()
        {
            if (_requestCompletedCount != 1)
            {
                RecordFail($"Expected one terminal Reset result, but observed '{_requestCompletedCount}'.");
                return;
            }

            if (!_terminalResolution.Succeeded || _terminalResult.Status != ResetExecutionStatus.Succeeded)
            {
                RecordFail($"Terminal Reset request was not successful. resolution='{_terminalResolution.Status}' result='{_terminalResult.Status}'.");
                return;
            }

            if (resetTrigger.IsRequestInFlight ||
                resetTrigger.LastResolution.Status != _terminalResolution.Status ||
                resetTrigger.LastResult.Status != _terminalResult.Status)
            {
                RecordFail(
                    $"Public trigger snapshot disagreed with the awaited terminal result. " +
                    $"inFlight='{resetTrigger.IsRequestInFlight}' resolution='{resetTrigger.LastResolution.Status}' " +
                    $"result='{resetTrigger.LastResult.Status}'.");
                return;
            }

            ResetExecutionResult result = _terminalResult;
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

            _certification.RecordPass();
            _cleanupFinished = true;
            PublishVerdict(cleanupSucceeded, cleanupIssue);
        }

        private bool CleanupAndVerify(out string issue)
        {
            if (_mutationApplied && _baselineCaptured && target != null)
            {
                ContainQaOwnedState();
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

            if (_requestInvoked &&
                (!resettable.IsRegistered ||
                 !resettable.Owner.IsValid ||
                 resettable.RegisteredCapabilityCount != 1))
            {
                issue = "Resettable registration or owner evidence was not preserved after execution.";
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

        private bool PoseMatchesBaseline()
        {
            return _baselineCaptured && target != null &&
                Approximately(target.localPosition, _baselineLocalPosition, PositionTolerance) &&
                Quaternion.Angle(target.localRotation, _baselineLocalRotation) <= RotationToleranceDegrees &&
                Approximately(target.localScale, _baselineLocalScale, ScaleTolerance);
        }

        private void RecordFail(string issue)
        {
            _certification.RecordFirstCausalDivergence(
                QaCertificationVerdict.Fail,
                issue);
        }

        private void RecordBlocked(string issue)
        {
            _certification.RecordFirstCausalDivergence(
                QaCertificationVerdict.Blocked,
                issue);
        }

        private void PublishVerdict(bool cleanupSucceeded, string cleanupIssue)
        {
            QaCertificationResult certificationResult =
                _certification.CreateResult(
                    ScenarioId,
                    cleanupSucceeded
                        ? QaCleanupDisposition.BaselineRestored
                        : QaCleanupDisposition.FreshBootRequired,
                    cleanupIssue);
            ResetExecutionResult resetResult = _terminalResult;
            string status = certificationResult.Verdict switch
            {
                QaCertificationVerdict.Pass => "Passed",
                QaCertificationVerdict.Blocked => "Blocked",
                _ => "Failed"
            };
            string message =
                $"[{certificationResult.ScenarioId}] status='{status}' verdict='{certificationResult.Verdict.ToString().ToUpperInvariant()}' " +
                $"requestInvoked='{_requestInvoked}' requestCompleted='{_requestCompletedCount}' " +
                $"resolutionStatus='{_terminalResolution.Status}' resultStatus='{_terminalResult.Status}' " +
                $"subjects='{resetResult.SubjectCount}' subjectSucceeded='{resetResult.SubjectSucceeded}' " +
                $"participants='{resetResult.ParticipantCount}' participantSucceeded='{resetResult.ParticipantSucceeded}' " +
                $"participantFailed='{resetResult.ParticipantFailed}' blockingIssues='{resetResult.BlockingIssueCount}' " +
                $"frameworkBaselineRestored='{_frameworkRestoredBaseline}' " +
                $"cleanup='{certificationResult.CleanupDisposition}' " +
                $"firstDivergence='{SanitizeDiagnostic(certificationResult.FirstCausalDivergence)}' " +
                $"cleanupIssue='{SanitizeDiagnostic(certificationResult.CleanupIssue)}'.";

            switch (certificationResult.Verdict)
            {
                case QaCertificationVerdict.Pass:
                    Debug.Log(message, this);
                    break;
                case QaCertificationVerdict.Blocked:
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

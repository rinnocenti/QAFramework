using System.Collections;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using Immersive.QaFramework.Certification;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.New004
{
    [DisallowMultipleComponent]
    public sealed class QaNew004SessionCameraContinuityScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-NEW-004";

        [Header("Session Camera contract")]
        [SerializeField] private GameApplicationAsset gameApplication;
        [SerializeField] private QaNew004CameraContinuityEvidence cameraEvidence;
        [SerializeField] private string expectedAssignmentId = "94040000000000000000000000000005";

        [Header("Route continuity matrix")]
        [SerializeField] private RouteAsset routeA;
        [SerializeField] private RouteAsset routeB;
        [SerializeField] private RouteRequestTrigger requestRouteB;
        [SerializeField] private RouteRequestTrigger requestRouteA;
        [SerializeField] private RouteAsset routeC;
        [SerializeField] private RouteRequestTrigger requestRouteC;
        [SerializeField] private RouteAsset routeD;
        [SerializeField] private ActivityAsset activityDA;
        [SerializeField] private ActivityAsset activityDB;
        [SerializeField] private RouteRequestTrigger requestRouteD;
        [SerializeField] private ActivityRequestTrigger requestActivityDB;
        [SerializeField] private RouteAsset routeE;
        [SerializeField] private ActivityAsset activityEA;
        [SerializeField] private ActivityAsset activityEC;
        [SerializeField] private RouteRequestTrigger requestRouteE;
        [SerializeField] private ActivityRequestTrigger requestActivityEC;

        [Header("Local Scenario deadlines")]
        [SerializeField, Min(0.1f)] private float baselineTimeoutSeconds = 10f;
        [SerializeField, Min(0.1f)] private float transitionTimeoutSeconds = 15f;

        private readonly QaCertificationRecorder _certification = new QaCertificationRecorder();
        private QaNew004CameraOutputProbe _baselineOutput;
        private string _baselineOutputToken;
        private string _baselineOutputId;
        private int _completedCases;
        private bool _started;
        private bool _terminal;
        private string _firstIssue = string.Empty;
        private QaCleanupDisposition _cleanupDisposition = QaCleanupDisposition.FreshBootRequired;
        private string _cleanupIssue = "The Session Camera continuity run has not established a restored baseline.";

        private void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            StartCoroutine(ExecuteScenario());
        }

        private void OnDisable()
        {
            if (_started && !_terminal)
            {
                StopAllCoroutines();
                RecordBlocked("Scenario execution was interrupted before its Session Camera continuity matrix completed.");
                PublishTerminal();
            }
        }

        private IEnumerator ExecuteScenario()
        {
            if (!TryValidateAuthoredComposition(out string issue))
            {
                RecordBlocked(issue);
                PublishTerminal();
                yield break;
            }

            yield return WaitForCondition(
                () => cameraEvidence != null && cameraEvidence.CurrentOutput != null &&
                      cameraEvidence.CurrentOutput.IsReady,
                baselineTimeoutSeconds);
            if (!_waitSucceeded || !TryEstablishBaseline(out issue))
            {
                RecordBlocked(
                    $"Session Camera Output did not become ready before the baseline deadline. {issue} {cameraEvidence?.Diagnostic}");
                PublishTerminal();
                yield break;
            }
            _completedCases++;

            yield return RequestRouteAndVerify(routeB, requestRouteB, "Route A -> B", routeA);
            if (_terminal) yield break;
            yield return RequestRouteAndVerify(routeA, requestRouteA, "Route B -> A", routeB);
            if (_terminal) yield break;
            yield return RequestRouteAndVerify(routeC, requestRouteC, "Route A -> C", routeA);
            if (_terminal) yield break;
            yield return RequestRouteAndVerify(routeD, requestRouteD, "Route C -> D with startup Activity D-A", routeC);
            if (_terminal) yield break;
            yield return RequestActivityAndVerify(activityDB, requestActivityDB, "Activity D-A -> D-B");
            if (_terminal) yield break;
            yield return RequestRouteAndVerify(routeE, requestRouteE, "Route D -> E with startup Activity E-A", routeD);
            if (_terminal) yield break;
            yield return RequestActivityAndVerify(activityEC, requestActivityEC, "Activity E-A -> E-C");
            if (_terminal) yield break;
            yield return RequestRouteAndVerify(routeA, requestRouteA, "Route E -> A cleanup", routeE);
            if (_terminal) yield break;

            if (!IsSceneLoaded(routeA.PrimaryScenePath))
            {
                yield return FailAndRestoreBaseline(
                    "Cleanup completed without restoring the authored Route A Primary Scene.");
                yield break;
            }

            if (!IsRouteABaselineLoaded())
            {
                yield return FailAndRestoreBaseline(
                    "Cleanup completed with residual Route or Activity scenes still loaded.");
                yield break;
            }

            _certification.RecordPass();
            PublishTerminal();
        }

        private IEnumerator RequestRouteAndVerify(
            RouteAsset target,
            RouteRequestTrigger trigger,
            string phase,
            RouteAsset previous)
        {
            if (target == null || trigger == null || trigger.TargetRoute != target ||
                !trigger.HasRouteRuntimeBinding)
            {
                yield return FailAndRestoreBaseline(
                    $"{phase}: public Route Request Trigger is missing, mismatched, or unbound.");
                yield break;
            }

            trigger.RequestRoute();
            yield return WaitForCondition(
                () => !trigger.IsRequestInFlight &&
                      trigger.LastEventPhase == FlowRequestEventPhase.Completed,
                transitionTimeoutSeconds);
            if (!_waitSucceeded || !trigger.LastRequestSucceeded)
            {
                yield return FailAndRestoreBaseline(
                    $"{phase}: Route request did not complete successfully. outcome='{trigger.LastOutcome}' message='{trigger.LastMessage}'.");
                yield break;
            }

            yield return WaitForCondition(
                () => IsSceneLoaded(target.PrimaryScenePath),
                transitionTimeoutSeconds);
            if (!_waitSucceeded)
            {
                yield return FailAndRestoreBaseline(
                    $"{phase}: target Route Primary Scene did not become loaded.");
                yield break;
            }

            if (previous != null &&
                previous.PrimaryScenePath != target.PrimaryScenePath &&
                IsSceneLoaded(previous.PrimaryScenePath))
            {
                yield return FailAndRestoreBaseline(
                    $"{phase}: previous Route Primary Scene remained loaded after the transition.");
                yield break;
            }

            if (!TryValidateContinuity(phase, out string issue))
            {
                yield return FailAndRestoreBaseline(issue);
                yield break;
            }

            _completedCases++;
        }

        private IEnumerator RequestActivityAndVerify(
            ActivityAsset target,
            ActivityRequestTrigger trigger,
            string phase)
        {
            if (target == null || trigger == null || trigger.TargetActivity != target ||
                !trigger.HasActivityRuntimeBinding)
            {
                yield return FailAndRestoreBaseline(
                    $"{phase}: public Activity Request Trigger is missing, mismatched, or unbound.");
                yield break;
            }

            trigger.RequestActivity();
            yield return WaitForCondition(
                () => !trigger.IsRequestInFlight &&
                      trigger.LastEventPhase == FlowRequestEventPhase.Completed,
                transitionTimeoutSeconds);
            if (!_waitSucceeded || !trigger.LastRequestSucceeded)
            {
                yield return FailAndRestoreBaseline(
                    $"{phase}: Activity request did not complete successfully. outcome='{trigger.LastOutcome}' message='{trigger.LastMessage}'.");
                yield break;
            }

            if (target.HasActivityContentProfile && target.ActivityContentProfile.HasScenes)
            {
                string scenePath = target.ActivityContentProfile.Scenes[0].ScenePath;
                yield return WaitForCondition(
                    () => IsSceneLoaded(scenePath),
                    transitionTimeoutSeconds);
                if (!_waitSucceeded)
                {
                    yield return FailAndRestoreBaseline(
                        $"{phase}: target Activity content scene '{scenePath}' did not become loaded.");
                    yield break;
                }
            }

            if (!TryValidateContinuity(phase, out string issue))
            {
                yield return FailAndRestoreBaseline(issue);
                yield break;
            }

            _completedCases++;
        }

        private IEnumerator FailAndRestoreBaseline(string issue)
        {
            RecordFail(issue);

            yield return WaitForCondition(
                () => !HasRequestInFlight(),
                transitionTimeoutSeconds);

            if (HasRequestInFlight())
            {
                _cleanupDisposition = QaCleanupDisposition.FreshBootRequired;
                _cleanupIssue = "A Route/Activity request remained in flight after the cleanup deadline.";
                PublishTerminal();
                yield break;
            }

            if (requestRouteA != null && requestRouteA.HasRouteRuntimeBinding &&
                !IsRouteABaselineLoaded())
            {
                requestRouteA.RequestRoute();
                yield return WaitForCondition(
                    () => !requestRouteA.IsRequestInFlight &&
                          requestRouteA.LastEventPhase == FlowRequestEventPhase.Completed,
                    transitionTimeoutSeconds);
            }

            if (IsRouteABaselineLoaded())
            {
                _cleanupDisposition = QaCleanupDisposition.BaselineRestored;
                _cleanupIssue = string.Empty;
            }
            else
            {
                _cleanupDisposition = QaCleanupDisposition.FreshBootRequired;
                _cleanupIssue = "Cleanup could not restore Route A and release the other QA-NEW-004 scopes.";
            }

            PublishTerminal();
        }

        private bool HasRequestInFlight()
        {
            return (requestRouteA != null && requestRouteA.IsRequestInFlight) ||
                   (requestRouteB != null && requestRouteB.IsRequestInFlight) ||
                   (requestRouteC != null && requestRouteC.IsRequestInFlight) ||
                   (requestRouteD != null && requestRouteD.IsRequestInFlight) ||
                   (requestRouteE != null && requestRouteE.IsRequestInFlight) ||
                   (requestActivityDB != null && requestActivityDB.IsRequestInFlight) ||
                   (requestActivityEC != null && requestActivityEC.IsRequestInFlight);
        }

        private bool IsRouteABaselineLoaded()
        {
            if (routeA == null || !IsSceneLoaded(routeA.PrimaryScenePath))
            {
                return false;
            }

            return !IsSceneLoaded(routeB != null ? routeB.PrimaryScenePath : string.Empty) &&
                   !IsSceneLoaded(routeC != null ? routeC.PrimaryScenePath : string.Empty) &&
                   !IsSceneLoaded(routeD != null ? routeD.PrimaryScenePath : string.Empty) &&
                   !IsSceneLoaded(routeE != null ? routeE.PrimaryScenePath : string.Empty) &&
                   !IsActivitySceneLoaded(activityDA) &&
                   !IsActivitySceneLoaded(activityDB) &&
                   !IsActivitySceneLoaded(activityEA) &&
                   !IsActivitySceneLoaded(activityEC);
        }

        private static bool IsActivitySceneLoaded(ActivityAsset activity)
        {
            if (activity == null || !activity.HasActivityContentProfile ||
                !activity.ActivityContentProfile.HasScenes)
            {
                return false;
            }

            return IsSceneLoaded(activity.ActivityContentProfile.Scenes[0].ScenePath);
        }

        private bool TryValidateAuthoredComposition(out string issue)
        {
            issue = string.Empty;
            if (gameApplication == null || cameraEvidence == null)
            {
                issue = "Game Application and explicit Camera continuity evidence asset are required.";
                return false;
            }
            if (gameApplication.PlayerSessionEnabled || gameApplication.DefaultPlayerSessionProfile != null)
            {
                issue = "QA-NEW-004 does not use Player; Player Session must remain disabled without a profile.";
                return false;
            }
            if (gameApplication.CameraSession == null || gameApplication.CameraSession.OutputPrefabs.Count != 1)
            {
                issue = "QA-NEW-004 requires exactly one authored Session Camera Output prefab.";
                return false;
            }

            GameObject outputPrefab = gameApplication.CameraSession.OutputPrefabs[0];
            CameraOutputAuthoring[] outputs = outputPrefab != null
                ? outputPrefab.GetComponentsInChildren<CameraOutputAuthoring>(true)
                : System.Array.Empty<CameraOutputAuthoring>();
            if (outputs.Length != 1 || outputs[0] == null ||
                outputs[0].FallbackCameraRig == null ||
                !outputs[0].TryValidateDefinition(out issue) ||
                !outputs[0].FallbackCameraRig.TryValidateForApply(out issue))
            {
                if (string.IsNullOrEmpty(issue))
                {
                    issue = "QA-NEW-004 Output prefab must contain one valid CameraOutputAuthoring and explicit Fixed Fallback Camera.";
                }
                return false;
            }

            if (gameApplication.SessionCameraAssignments.Count != 1)
            {
                issue = "QA-NEW-004 requires one Session-scoped Camera Assignment for continuity coverage.";
                return false;
            }
            SessionCameraAssignmentAuthoring authoring = gameApplication.SessionCameraAssignments[0];
            if (authoring == null || authoring.AssignmentId.Value != expectedAssignmentId ||
                !authoring.TryBuild(out SessionCameraAssignment assignment, out issue))
            {
                if (string.IsNullOrEmpty(issue))
                {
                    issue = "QA-NEW-004 Session Camera Assignment identity or authoring is invalid.";
                }
                return false;
            }

            if (assignment.OccurrenceMode != CameraOccurrenceMode.SessionScoped ||
                assignment.MembershipPolicy != CameraMembershipPolicy.None ||
                assignment.TargetPolicy != CameraTargetPolicy.NoSubject ||
                assignment.Outputs.Count != 1 ||
                assignment.Outputs[0].OutputId != outputs[0].OutputDefinition.OutputId ||
                authoring.Definition == null ||
                !authoring.Definition.TryValidateSessionCamera(CameraTargetPolicy.NoSubject, out issue))
            {
                if (string.IsNullOrEmpty(issue))
                {
                    issue = "QA-NEW-004 assignment must be Session-scoped, targetless, Player-free and mapped to its exact Output.";
                }
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryEstablishBaseline(out string issue)
        {
            _baselineOutput = cameraEvidence != null ? cameraEvidence.CurrentOutput : null;
            if (_baselineOutput == null || !_baselineOutput.IsReady ||
                !IsSceneLoaded(routeA != null ? routeA.PrimaryScenePath : string.Empty))
            {
                issue = cameraEvidence != null ? cameraEvidence.Diagnostic : "Camera evidence is missing.";
                return false;
            }

            _baselineOutputToken = _baselineOutput.InstanceToken;
            _baselineOutputId = _baselineOutput.OutputId;
            if (string.IsNullOrWhiteSpace(_baselineOutputToken) ||
                string.IsNullOrWhiteSpace(_baselineOutputId))
            {
                issue = "Camera Output probe did not publish a runtime token and valid Output ID.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryValidateContinuity(string phase, out string issue)
        {
            QaNew004CameraOutputProbe current = cameraEvidence != null
                ? cameraEvidence.CurrentOutput
                : null;
            if (current == null || !current.IsReady ||
                !ReferenceEquals(current, _baselineOutput) ||
                current.InstanceToken != _baselineOutputToken ||
                current.OutputId != _baselineOutputId)
            {
                issue = $"{phase}: the same initialized Session Camera Output did not survive the scope transition. {cameraEvidence?.Diagnostic}";
                return false;
            }

            if (gameApplication.SessionCameraAssignments.Count != 1 ||
                gameApplication.SessionCameraAssignments[0].AssignmentId.Value != expectedAssignmentId)
            {
                issue = $"{phase}: the authored Session Camera Assignment changed during a Route/Activity transition.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private float _waitDeadline;
        private bool _waitSucceeded;

        private IEnumerator WaitForCondition(System.Func<bool> condition, float timeoutSeconds)
        {
            _waitSucceeded = false;
            _waitDeadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (Time.realtimeSinceStartup < _waitDeadline)
            {
                if (condition())
                {
                    _waitSucceeded = true;
                    yield break;
                }
                yield return null;
            }
            _waitSucceeded = condition();
        }

        private static bool IsSceneLoaded(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        private void RecordFail(string issue)
        {
            _firstIssue = string.IsNullOrWhiteSpace(_firstIssue) ? issue : _firstIssue;
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, _firstIssue);
        }

        private void RecordBlocked(string issue)
        {
            _firstIssue = string.IsNullOrWhiteSpace(_firstIssue) ? issue : _firstIssue;
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, _firstIssue);
        }

        private void PublishTerminal()
        {
            if (_terminal)
            {
                return;
            }
            _terminal = true;
            if (string.IsNullOrEmpty(_firstIssue))
            {
                _certification.RecordPass();
                _cleanupDisposition = QaCleanupDisposition.BaselineRestored;
                _cleanupIssue = string.Empty;
            }

            QaCertificationResult result = _certification.CreateResult(
                ScenarioId,
                _cleanupDisposition,
                _cleanupIssue);
            string status = result.Verdict switch
            {
                QaCertificationVerdict.Pass => "Passed",
                QaCertificationVerdict.Blocked => "Blocked",
                _ => "Failed"
            };
            string message =
                $"[{ScenarioId}] status='{status}' verdict='{result.Verdict.ToString().ToUpperInvariant()}' " +
                $"cases='{_completedCases}/9' outputId='{_baselineOutputId}' outputToken='{_baselineOutputToken}' " +
                $"assignment='{expectedAssignmentId}' cleanup='{result.CleanupDisposition}' " +
                $"firstDivergence='{Sanitize(result.FirstCausalDivergence)}' cleanupIssue='{Sanitize(result.CleanupIssue)}'.";

            if (result.Verdict == QaCertificationVerdict.Pass)
            {
                Debug.Log(message, this);
            }
            else if (result.Verdict == QaCertificationVerdict.Blocked)
            {
                Debug.LogWarning(message, this);
            }
            else
            {
                Debug.LogError(message, this);
            }
        }

        private static string Sanitize(string value)
        {
            return (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Replace("'", "\\'");
        }
    }
}

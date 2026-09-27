using System;
using System.Collections;
using Immersive.Foundation.Events;
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
    public sealed class QaNew004RouteCameraSelectionScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-NEW-004";

        [Header("Route contract")]
        [SerializeField] private RouteAsset routeA;
        [SerializeField] private RouteAsset routeB;
        [SerializeField] private RouteRequestTrigger requestRouteB;
        [SerializeField] private RouteRequestTrigger requestRouteA;

        [Header("Local Scenario deadlines")]
        [SerializeField, Min(0.1f)] private float baselineTimeoutSeconds = 10f;
        [SerializeField, Min(0.1f)] private float transitionTimeoutSeconds = 15f;
        [SerializeField, Min(0.1f)] private float cleanupTimeoutSeconds = 15f;

        private readonly QaCertificationRecorder _certification =
            new QaCertificationRecorder();
        private IEventBinding _requestBBinding;
        private IEventBinding _requestABinding;
        private QaNew004RouteLifecycleProbe _initialRouteAProbe;
        private QaNew004CameraOccurrenceProbe _initialOccurrence;
        private CameraOutputAuthoring _output;
        private CameraRequest _initialRequest;
        private string _initialOccurrenceToken;
        private int _submittedB;
        private int _completedB;
        private int _succeededB;
        private int _submittedA;
        private int _completedA;
        private int _succeededA;
        private int _routeAExited;
        private int _routeASceneUnloaded;
        private bool _waitSucceeded;
        private bool _contractProved;
        private bool _hasDivergence;
        private bool _terminalPublished;

        private void Awake()
        {
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void Start()
        {
            StartCoroutine(ExecuteScenario());
        }

        private void OnDisable()
        {
            if (_terminalPublished)
            {
                DisposeObservations();
                return;
            }

            StopAllCoroutines();
            RecordBlocked(
                "Scenario execution was interrupted before terminal evidence was published.");
            DisposeObservations();
            PublishTerminal(false, "A fresh runtime boot is required.");
        }

        private IEnumerator ExecuteScenario()
        {
            if (!TryValidateComposition(out string compositionIssue))
            {
                RecordBlocked(compositionIssue);
                yield return CleanupAndFinish();
                yield break;
            }

            _requestBBinding =
                requestRouteB.SubscribeRequestEvents(HandleRequestBEvent);
            _requestABinding =
                requestRouteA.SubscribeRequestEvents(HandleRequestAEvent);

            string baselineIssue = string.Empty;
            yield return WaitForCondition(
                () => TryCaptureBaseline(out baselineIssue),
                baselineTimeoutSeconds);
            if (!_waitSucceeded || !TryCaptureBaseline(out baselineIssue))
            {
                RecordBlocked(
                    "Route A Camera selection baseline was not established. " +
                    baselineIssue);
                yield return CleanupAndFinish();
                yield break;
            }

            _initialRouteAProbe.Exited += HandleRouteAExited;
            requestRouteB.RequestRoute();
            if (_submittedB != 1)
            {
                RecordBlocked(
                    $"Route B request was not accepted. submitted='{_submittedB}' inFlight='{requestRouteB.IsRequestInFlight}'.");
                yield return CleanupAndFinish();
                yield break;
            }

            yield return WaitForCondition(
                () => _completedB >= 1 &&
                      !requestRouteB.IsRequestInFlight &&
                      _routeASceneUnloaded >= 1,
                transitionTimeoutSeconds);
            if (!_waitSucceeded)
            {
                RecordFail(
                    $"A -> B did not reach terminal teardown evidence. completedB='{_completedB}' succeededB='{_succeededB}' exitA='{_routeAExited}' unloadA='{_routeASceneUnloaded}'.");
                yield return CleanupAndFinish();
                yield break;
            }

            if (!TryValidateContinuity(out string continuityIssue))
            {
                RecordFail(continuityIssue);
                yield return CleanupAndFinish();
                yield break;
            }

            _contractProved = true;
            yield return CleanupAndFinish();
        }

        private IEnumerator CleanupAndFinish()
        {
            yield return WaitForCondition(
                () => requestRouteB == null || !requestRouteB.IsRequestInFlight,
                cleanupTimeoutSeconds);

            if (requestRouteA != null &&
                requestRouteA.HasRouteRuntimeBinding &&
                !requestRouteA.IsRequestInFlight &&
                IsExactSceneActive(routeB))
            {
                requestRouteA.RequestRoute();
                yield return WaitForCondition(
                    () => _completedA >= 1 &&
                          !requestRouteA.IsRequestInFlight,
                    cleanupTimeoutSeconds);
            }

            bool baselineRestored =
                TryValidateRestoredBaseline(out string cleanupIssue);
            if (_contractProved && baselineRestored && !_hasDivergence)
            {
                _certification.RecordPass();
            }
            else if (!_hasDivergence)
            {
                RecordBlocked(
                    _contractProved
                        ? "The contract was observed, but Route A cleanup did not restore the baseline. " + cleanupIssue
                        : "The Scenario ended before the A -> B contract was proved.");
            }

            DisposeObservations();
            PublishTerminal(baselineRestored, cleanupIssue);
        }

        private bool TryValidateComposition(out string issue)
        {
            if (routeA == null || routeB == null ||
                requestRouteA == null || requestRouteB == null)
            {
                issue =
                    "QA-NEW-004 requires Route A, Route B and two persistent Route Request Triggers.";
                return false;
            }

            if (routeA.CameraPresentationSelections.Count != 1 ||
                routeB.HasCameraPresentationSelections)
            {
                issue =
                    "Route A must declare exactly one Camera Presentation Selection and Route B must declare zero selections.";
                return false;
            }

            if (routeA.HasCameraPresentations || routeB.HasCameraPresentations)
            {
                issue =
                    "QA-NEW-004 isolates persistent selection; neither Route may declare contextual Camera Presentations.";
                return false;
            }

            if (!ReferenceEquals(requestRouteB.TargetRoute, routeB) ||
                !ReferenceEquals(requestRouteA.TargetRoute, routeA))
            {
                issue = "Route Request Trigger targets do not match the authored Routes.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryCaptureBaseline(out string issue)
        {
            if (!requestRouteA.HasRouteRuntimeBinding ||
                !requestRouteB.HasRouteRuntimeBinding ||
                requestRouteA.IsRequestInFlight ||
                requestRouteB.IsRequestInFlight ||
                !IsExactSceneActive(routeA))
            {
                issue = "Route A or its public Route triggers are not ready.";
                return false;
            }

            if (!TryResolveRouteProbe(
                    routeA,
                    out QaNew004RouteLifecycleProbe routeProbe,
                    out issue) ||
                routeProbe.EnterCount != 1 ||
                routeProbe.ExitCount != 0 ||
                !routeProbe.IsRouteContentActive)
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? "Route A lifecycle probe is not at its entered baseline."
                    : issue;
                return false;
            }

            if (!TryResolveCameraEvidence(
                    out QaNew004CameraOccurrenceProbe occurrence,
                    out CameraOutputAuthoring output,
                    out CameraRequest request,
                    out issue))
            {
                return false;
            }

            _initialRouteAProbe = routeProbe;
            _initialOccurrence = occurrence;
            _initialOccurrenceToken = occurrence.OccurrenceToken;
            _output = output;
            _initialRequest = request;
            return true;
        }

        private bool TryValidateContinuity(out string issue)
        {
            if (_succeededB != 1 ||
                _routeAExited != 1 ||
                _routeASceneUnloaded != 1 ||
                !IsExactSceneActive(routeB) ||
                IsExactSceneLoaded(routeA))
            {
                issue =
                    $"Route lifecycle teardown is incomplete. succeededB='{_succeededB}' exitA='{_routeAExited}' unloadA='{_routeASceneUnloaded}' active='{SceneManager.GetActiveScene().path}'.";
                return false;
            }

            if (!TryResolveRouteProbe(
                    routeB,
                    out QaNew004RouteLifecycleProbe routeBProbe,
                    out issue) ||
                routeBProbe.EnterCount != 1 ||
                !routeBProbe.IsRouteContentActive)
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? "Route B did not become active through its public lifecycle."
                    : issue;
                return false;
            }

            if (!TryResolveCameraEvidence(
                    out QaNew004CameraOccurrenceProbe occurrence,
                    out CameraOutputAuthoring output,
                    out CameraRequest request,
                    out issue))
            {
                return false;
            }

            if (!ReferenceEquals(occurrence, _initialOccurrence) ||
                occurrence.OccurrenceToken != _initialOccurrenceToken ||
                !ReferenceEquals(output, _output) ||
                !RequestsMatch(_initialRequest, request))
            {
                issue =
                    "Route B preserved a visual result without preserving the exact Camera occurrence, Output and normal CameraRequest.";
                return false;
            }

            if (output.Applicator == null ||
                !output.Applicator.HasAppliedRequest ||
                output.Applicator.AppliedRequestId != request.RequestId)
            {
                issue =
                    "The preserved normal winner was not applied after the Route transition released force-default.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryValidateRestoredBaseline(out string issue)
        {
            if (!IsExactSceneActive(routeA) ||
                requestRouteA == null || requestRouteA.IsRequestInFlight)
            {
                issue = "Route A is not the restored active baseline.";
                return false;
            }

            if (!TryResolveCameraEvidence(
                    out QaNew004CameraOccurrenceProbe occurrence,
                    out CameraOutputAuthoring output,
                    out CameraRequest request,
                    out issue))
            {
                return false;
            }

            if (!ReferenceEquals(occurrence, _initialOccurrence) ||
                occurrence.OccurrenceToken != _initialOccurrenceToken ||
                !ReferenceEquals(output, _output) ||
                !RequestsMatch(_initialRequest, request))
            {
                issue =
                    "Cleanup recreated or replaced the persistent Camera selection occurrence.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryResolveCameraEvidence(
            out QaNew004CameraOccurrenceProbe occurrence,
            out CameraOutputAuthoring output,
            out CameraRequest request,
            out string issue)
        {
            occurrence = null;
            output = null;
            request = default;

            QaNew004CameraOccurrenceProbe[] occurrences =
                FindObjectsByType<QaNew004CameraOccurrenceProbe>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (occurrences.Length != 1 || occurrences[0] == null ||
                string.IsNullOrWhiteSpace(occurrences[0].OccurrenceToken) ||
                occurrences[0].Composer == null)
            {
                issue =
                    $"Expected exactly one live selected Camera occurrence. found='{occurrences.Length}'.";
                return false;
            }

            CameraPresentationDefinition selected =
                routeA.CameraPresentationSelections[0];
            CameraOutputAuthoring[] outputs =
                FindObjectsByType<CameraOutputAuthoring>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            for (int index = 0; index < outputs.Length; index++)
            {
                if (outputs[index] != null &&
                    ReferenceEquals(
                        outputs[index].OutputDefinition,
                        selected.OutputDefinition))
                {
                    if (output != null)
                    {
                        issue = "More than one physical Output matches the selected definition.";
                        return false;
                    }

                    output = outputs[index];
                }
            }

            if (output == null || output.Context == null ||
                !output.Context.HasWinner)
            {
                issue = "The selected Camera Output has no normal winner.";
                return false;
            }

            occurrence = occurrences[0];
            request = output.Context.Winner;
            if (!ReferenceEquals(request.Rig.Composer, occurrence.Composer))
            {
                issue =
                    "The normal winner does not reference the observed selected occurrence.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool RequestsMatch(
            CameraRequest left,
            CameraRequest right)
        {
            return left.RequestId == right.RequestId &&
                   left.OutputId == right.OutputId &&
                   left.Owner.Equals(right.Owner) &&
                   left.Lifetime.Equals(right.Lifetime) &&
                   ReferenceEquals(left.Rig.Composer, right.Rig.Composer) &&
                   left.Policy.Equals(right.Policy) &&
                   left.PresentationTransitionMode ==
                       right.PresentationTransitionMode &&
                   left.ReleaseCondition == right.ReleaseCondition &&
                   string.Equals(
                       left.DiagnosticSource,
                       right.DiagnosticSource,
                       StringComparison.Ordinal) &&
                   string.Equals(
                       left.DiagnosticReason,
                       right.DiagnosticReason,
                       StringComparison.Ordinal);
        }

        private static bool TryResolveRouteProbe(
            RouteAsset route,
            out QaNew004RouteLifecycleProbe probe,
            out string issue)
        {
            probe = null;
            Scene scene = SceneManager.GetSceneByPath(route.PrimaryScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                issue = $"Route scene is not loaded. route='{route.RouteName}'.";
                return false;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                QaNew004RouteLifecycleProbe[] candidates =
                    roots[index].GetComponentsInChildren<
                        QaNew004RouteLifecycleProbe>(true);
                for (int candidateIndex = 0;
                     candidateIndex < candidates.Length;
                     candidateIndex++)
                {
                    if (probe != null)
                    {
                        issue =
                            $"Route '{route.RouteName}' has more than one lifecycle probe.";
                        return false;
                    }

                    probe = candidates[candidateIndex];
                }
            }

            issue = probe == null
                ? $"Route '{route.RouteName}' has no lifecycle probe."
                : string.Empty;
            return probe != null;
        }

        private IEnumerator WaitForCondition(
            Func<bool> condition,
            float timeoutSeconds)
        {
            double deadline =
                Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (!condition() &&
                   Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            _waitSucceeded = condition();
        }

        private void HandleRequestBEvent(RouteRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null ||
                !ReferenceEquals(requestEvent.TargetRoute, routeB))
            {
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedB++;
            }
            else if (requestEvent.IsCompleted)
            {
                _completedB++;
                if (requestEvent.Succeeded)
                {
                    _succeededB++;
                }
                else
                {
                    RecordFail(
                        $"Route B request failed. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
            }
        }

        private void HandleRequestAEvent(RouteRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null ||
                !ReferenceEquals(requestEvent.TargetRoute, routeA))
            {
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedA++;
            }
            else if (requestEvent.IsCompleted)
            {
                _completedA++;
                if (requestEvent.Succeeded)
                {
                    _succeededA++;
                }
                else
                {
                    RecordFail(
                        $"Route A cleanup request failed. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
            }
        }

        private void HandleRouteAExited(
            QaNew004RouteLifecycleProbe probe,
            Immersive.Framework.RouteLifecycle.RouteContentLifecycleContext context)
        {
            _routeAExited++;
            if (!ReferenceEquals(context.Route, routeA) ||
                !ReferenceEquals(context.NextRoute, routeB))
            {
                RecordFail(
                    "Route A exit did not identify the exact A -> B transition.");
            }
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (routeA != null &&
                string.Equals(
                    scene.path,
                    routeA.PrimaryScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                _routeASceneUnloaded++;
            }
        }

        private static bool IsExactSceneActive(RouteAsset route)
        {
            return route != null &&
                   string.Equals(
                       SceneManager.GetActiveScene().path,
                       route.PrimaryScenePath,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExactSceneLoaded(RouteAsset route)
        {
            if (route == null)
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(route.PrimaryScenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        private void RecordFail(string issue)
        {
            _hasDivergence = true;
            _certification.RecordFirstCausalDivergence(
                QaCertificationVerdict.Fail,
                issue);
        }

        private void RecordBlocked(string issue)
        {
            _hasDivergence = true;
            _certification.RecordFirstCausalDivergence(
                QaCertificationVerdict.Blocked,
                issue);
        }

        private void DisposeObservations()
        {
            _requestBBinding?.Dispose();
            _requestBBinding = null;
            _requestABinding?.Dispose();
            _requestABinding = null;
            if (_initialRouteAProbe != null)
            {
                _initialRouteAProbe.Exited -= HandleRouteAExited;
            }

            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        private void PublishTerminal(
            bool baselineRestored,
            string cleanupIssue)
        {
            if (_terminalPublished)
            {
                return;
            }

            _terminalPublished = true;
            QaCertificationResult result = _certification.CreateResult(
                ScenarioId,
                baselineRestored
                    ? QaCleanupDisposition.BaselineRestored
                    : QaCleanupDisposition.FreshBootRequired,
                cleanupIssue);
            string status = result.Verdict switch
            {
                QaCertificationVerdict.Pass => "Passed",
                QaCertificationVerdict.Blocked => "Blocked",
                _ => "Failed"
            };
            string message =
                $"[{result.ScenarioId}] status='{status}' verdict='{result.Verdict.ToString().ToUpperInvariant()}' " +
                $"submittedB='{_submittedB}' completedB='{_completedB}' succeededB='{_succeededB}' exitA='{_routeAExited}' unloadA='{_routeASceneUnloaded}' " +
                $"occurrence='{_initialOccurrenceToken}' request='{_initialRequest.RequestId}' contractProved='{_contractProved}' " +
                $"submittedA='{_submittedA}' completedA='{_completedA}' succeededA='{_succeededA}' baselineRestored='{baselineRestored}' " +
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
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}

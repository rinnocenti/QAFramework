using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;
using Immersive.QaFramework.Certification;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.New003
{
    [DisallowMultipleComponent]
    public sealed class QaNew003RouteReplacementScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-NEW-003";

        [Header("Route contract")]
        [SerializeField] private RouteAsset routeA;
        [SerializeField] private RouteAsset routeB;
        [SerializeField] private RouteRequestTrigger requestRouteB;
        [SerializeField] private RouteRequestTrigger requestRouteA;

        [Header("Local Scenario deadlines")]
        [SerializeField, Min(0.1f)] private float baselineTimeoutSeconds = 10f;
        [SerializeField, Min(0.1f)] private float transitionTimeoutSeconds = 15f;
        [SerializeField, Min(0.1f)] private float cleanupTimeoutSeconds = 15f;

        private readonly QaCertificationRecorder _certification = new QaCertificationRecorder();
        private IEventBinding _requestBBinding;
        private IEventBinding _requestABinding;
        private QaNew003RouteLifecycleProbe _initialAProbe;
        private QaNew003RouteLifecycleProbe _routeBProbe;
        private QaNew003RouteLifecycleProbe _returnedAProbe;
        private SceneLifecycleEvents _initialASceneEvents;
        private SceneLifecycleEvents _routeBSceneEvents;
        private SceneLifecycleEvents _returnedASceneEvents;

        private int _ownerInstanceId;
        private int _ownerSceneHandle;
        private int _initialAProbeInstanceId;
        private int _returnedAProbeInstanceId;
        private int _sequence;
        private int _submittedB;
        private int _completedB;
        private int _succeededB;
        private int _submittedA;
        private int _completedA;
        private int _succeededA;
        private int _routeAExited;
        private int _routeBEntered;
        private int _routeBExited;
        private int _routeAReturned;
        private int _sceneAReleasing;
        private int _sceneBAvailable;
        private int _sceneBReleasing;
        private int _sceneAReturnAvailable;
        private int _sceneAUnloaded;
        private int _sceneBLoaded;
        private int _sceneBUnloaded;
        private int _sceneAReturnLoaded;

        private int _seqSubmittedB;
        private int _seqRouteAExited;
        private int _seqSceneAReleasing;
        private int _seqSceneAUnloaded;
        private int _seqSceneBLoaded;
        private int _seqSceneBAvailable;
        private int _seqRouteBEntered;
        private int _seqCompletedB;
        private int _seqSubmittedA;
        private int _seqRouteBExited;
        private int _seqSceneBReleasing;
        private int _seqSceneBUnloaded;
        private int _seqSceneAReturnLoaded;
        private int _seqSceneAReturnAvailable;
        private int _seqRouteAReturned;
        private int _seqCompletedA;

        private bool _executionStarted;
        private bool _requestBIssued;
        private bool _requestAIssued;
        private bool _contractProved;
        private bool _hasDivergence;
        private bool _terminalPublished;
        private bool _observingScenes;
        private bool _waitSucceeded;

        private void Awake()
        {
            _ownerInstanceId = GetInstanceID();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            _observingScenes = true;
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
            if (!_executionStarted || _terminalPublished)
            {
                DisposeObservations();
                return;
            }

            StopAllCoroutines();
            if (_requestBIssued)
            {
                RecordFail("Scenario execution was interrupted after the Route replacement contract was invoked.");
            }
            else
            {
                RecordBlocked("Scenario execution was interrupted before the Route replacement contract could be exercised.");
            }

            DisposeObservations();
            PublishTerminal(false, "Scenario interruption requires a fresh runtime boot.");
        }

        private IEnumerator ExecuteScenario()
        {
            if (!TryValidateSerializedComposition(out string compositionIssue))
            {
                RecordBlocked(compositionIssue);
                yield return CleanupAndFinish();
                yield break;
            }

            _requestBBinding = requestRouteB.SubscribeRequestEvents(HandleRequestBEvent);
            _requestABinding = requestRouteA.SubscribeRequestEvents(HandleRequestAEvent);

            string baselineIssue = string.Empty;
            yield return WaitForCondition(
                () => TryEstablishBaseline(out baselineIssue),
                baselineTimeoutSeconds);
            if (!_waitSucceeded || !TryEstablishBaseline(out baselineIssue))
            {
                RecordBlocked($"Known Route A baseline was not established before the local deadline. {baselineIssue}");
                yield return CleanupAndFinish();
                yield break;
            }

            AttachInitialAObservers();
            _requestBIssued = true;
            requestRouteB.RequestRoute();
            if (_submittedB != 1)
            {
                RecordBlocked(
                    $"Route B request was not publicly accepted. submittedB='{_submittedB}' completedB='{_completedB}' inFlight='{requestRouteB.IsRequestInFlight}'.");
                yield return CleanupAndFinish();
                yield break;
            }

            yield return WaitForCondition(
                () => _completedB >= 1 && !requestRouteB.IsRequestInFlight,
                transitionTimeoutSeconds);
            if (!_waitSucceeded)
            {
                RecordFail(BuildMissingBTerminalIssue());
                yield return CleanupAndFinish();
                yield break;
            }

            if (!TryValidateBPhase(out string routeBIssue))
            {
                RecordFail(routeBIssue);
                yield return CleanupAndFinish();
                yield break;
            }

            _requestAIssued = true;
            requestRouteA.RequestRoute();
            if (_submittedA != 1)
            {
                RecordFail(
                    $"Route A restoration request was not publicly accepted after Route B became authoritative. submittedA='{_submittedA}' completedA='{_completedA}' inFlight='{requestRouteA.IsRequestInFlight}'.");
                yield return CleanupAndFinish();
                yield break;
            }

            yield return WaitForCondition(
                () => _completedA >= 1 && !requestRouteA.IsRequestInFlight,
                transitionTimeoutSeconds);
            if (!_waitSucceeded)
            {
                RecordFail(BuildMissingATerminalIssue());
                yield return CleanupAndFinish();
                yield break;
            }

            if (!TryValidateCompleteCycle(out string cycleIssue))
            {
                RecordFail(cycleIssue);
                yield return CleanupAndFinish();
                yield break;
            }

            _contractProved = true;
            yield return CleanupAndFinish();
        }

        private IEnumerator CleanupAndFinish()
        {
            yield return WaitForCondition(
                () => !HasRequestInFlight(),
                cleanupTimeoutSeconds);

            if (!HasRequestInFlight() &&
                IsExactSceneActive(routeB) &&
                !_requestAIssued &&
                requestRouteA != null &&
                requestRouteA.HasRouteRuntimeBinding &&
                !requestRouteA.IsRequestInFlight)
            {
                _requestAIssued = true;
                requestRouteA.RequestRoute();
                yield return WaitForCondition(
                    () => _completedA >= 1 && !requestRouteA.IsRequestInFlight,
                    cleanupTimeoutSeconds);
            }

            bool baselineRestored = TryVerifyRestoredBaseline(out string cleanupIssue);
            if (_contractProved && !baselineRestored)
            {
                RecordFail("The complete Route replacement lifecycle was observed, but the Route A baseline was not restored. " + cleanupIssue);
            }

            if (!_hasDivergence)
            {
                if (_contractProved && baselineRestored)
                {
                    _certification.RecordPass();
                }
                else
                {
                    RecordBlocked("The Scenario reached cleanup without exercising the complete Route replacement contract.");
                }
            }

            DisposeObservations();
            PublishTerminal(baselineRestored, baselineRestored ? string.Empty : cleanupIssue);
        }

        private IEnumerator WaitForCondition(Func<bool> condition, float timeoutSeconds)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            _waitSucceeded = condition();
        }

        private bool TryValidateSerializedComposition(out string issue)
        {
            if (routeA == null || routeB == null || requestRouteA == null || requestRouteB == null)
            {
                issue = "QA-NEW-003 requires Route A, Route B and two persistent Route Request Triggers.";
                return false;
            }

            if (ReferenceEquals(routeA, routeB) || routeA.HasSameStableId(routeB))
            {
                issue = "Route A and Route B require distinct authored definitions and distinct stable Route IDs.";
                return false;
            }

            if (!routeA.HasValidRouteId || !routeB.HasValidRouteId ||
                !routeA.HasPrimaryScene || !routeB.HasPrimaryScene)
            {
                issue = "Both Routes require valid IDs and explicit Primary Scenes.";
                return false;
            }

            if (routeA.HasStartupActivity || routeB.HasStartupActivity)
            {
                issue = "QA-NEW-003 isolates Route Primary Scene replacement; neither Route may declare a Startup Activity.";
                return false;
            }

            if (string.Equals(routeA.PrimaryScenePath, routeB.PrimaryScenePath, StringComparison.OrdinalIgnoreCase))
            {
                issue = "Route A and Route B must not share a Primary Scene.";
                return false;
            }

            if (!ReferenceEquals(requestRouteB.TargetRoute, routeB) ||
                !ReferenceEquals(requestRouteA.TargetRoute, routeA))
            {
                issue = "Persistent Route Request Trigger targets do not match Route B and Route A respectively.";
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(routeA.PrimaryScenePath) ||
                !Application.CanStreamedLevelBeLoaded(routeB.PrimaryScenePath))
            {
                issue = "Both Route Primary Scenes must be enabled in the active Build Profile or Shared Scene List.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryEstablishBaseline(out string issue)
        {
            if (!requestRouteA.HasRouteRuntimeBinding || !requestRouteB.HasRouteRuntimeBinding)
            {
                issue = $"Persistent triggers are not bound. routeA='{requestRouteA.RouteRuntimeBindingStatus}' routeB='{requestRouteB.RouteRuntimeBindingStatus}'.";
                return false;
            }

            if (HasRequestInFlight())
            {
                issue = "A Route request is already in flight.";
                return false;
            }

            if (!IsExactSceneActive(routeA) || IsExactSceneLoaded(routeB))
            {
                issue = $"Expected active Primary Scene A and unloaded B. active='{SceneManager.GetActiveScene().path}' bLoaded='{IsExactSceneLoaded(routeB)}'.";
                return false;
            }

            Scene sceneA = SceneManager.GetSceneByPath(routeA.PrimaryScenePath);
            if (!TryResolveSceneEvidence(sceneA, routeA, out QaNew003RouteLifecycleProbe probe, out SceneLifecycleEvents sceneEvents, out issue))
            {
                return false;
            }

            if (probe.EnterCount != 1 || probe.ExitCount != 0 ||
                !probe.IsRouteContentActive || !probe.HasRouteContentContext ||
                !ReferenceEquals(probe.LastEnteredContext.Route, routeA))
            {
                issue = $"Route A lifecycle baseline is not ready. enter='{probe.EnterCount}' exit='{probe.ExitCount}' active='{probe.IsRouteContentActive}'.";
                return false;
            }

            if (sceneEvents.AvailableCount != 1 || sceneEvents.ReleasingCount != 0 ||
                !string.Equals(sceneEvents.LastEvent, "Available", StringComparison.Ordinal))
            {
                issue = $"Route A Scene Lifecycle baseline is not ready. available='{sceneEvents.AvailableCount}' releasing='{sceneEvents.ReleasingCount}' last='{sceneEvents.LastEvent}'.";
                return false;
            }

            Scene ownerScene = gameObject.scene;
            if (!ownerScene.IsValid() ||
                SceneMatches(ownerScene, routeA) ||
                SceneMatches(ownerScene, routeB))
            {
                issue = "Scenario owner was not retained outside both Route Primary Scenes.";
                return false;
            }

            _initialAProbe = probe;
            _initialASceneEvents = sceneEvents;
            _initialAProbeInstanceId = probe.GetInstanceID();
            _ownerSceneHandle = ownerScene.handle;
            issue = string.Empty;
            return true;
        }

        private void AttachInitialAObservers()
        {
            _initialAProbe.Exited += HandleInitialARouteExited;
            _initialASceneEvents.Releasing.AddListener(HandleInitialASceneReleasing);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_requestBIssued && !_requestAIssued && SceneMatches(scene, routeB))
            {
                _sceneBLoaded++;
                Mark(ref _seqSceneBLoaded);
                if (TryResolveSceneEvidence(scene, routeB, out _routeBProbe, out _routeBSceneEvents, out string issue))
                {
                    _routeBProbe.Entered += HandleRouteBEntered;
                    _routeBProbe.Exited += HandleRouteBExited;
                    _routeBSceneEvents.Available.AddListener(HandleRouteBSceneAvailable);
                    _routeBSceneEvents.Releasing.AddListener(HandleRouteBSceneReleasing);
                }
                else
                {
                    RecordFail("Route B loaded without its exact public lifecycle composition. " + issue);
                }
                return;
            }

            if (_requestAIssued && SceneMatches(scene, routeA))
            {
                _sceneAReturnLoaded++;
                Mark(ref _seqSceneAReturnLoaded);
                if (TryResolveSceneEvidence(scene, routeA, out _returnedAProbe, out _returnedASceneEvents, out string issue))
                {
                    _returnedAProbeInstanceId = _returnedAProbe.GetInstanceID();
                    _returnedAProbe.Entered += HandleReturnedARouteEntered;
                    _returnedASceneEvents.Available.AddListener(HandleReturnedASceneAvailable);
                }
                else
                {
                    RecordFail("Returned Route A loaded without its exact public lifecycle composition. " + issue);
                }
            }
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            if (_requestBIssued && SceneMatches(scene, routeA))
            {
                _sceneAUnloaded++;
                Mark(ref _seqSceneAUnloaded);
            }
            else if (_requestAIssued && SceneMatches(scene, routeB))
            {
                _sceneBUnloaded++;
                Mark(ref _seqSceneBUnloaded);
            }
        }

        private void HandleRequestBEvent(RouteRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null)
            {
                return;
            }

            if (!ReferenceEquals(requestEvent.TargetRoute, routeB))
            {
                RecordFail("Route B trigger published an event for the wrong target Route.");
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedB++;
                Mark(ref _seqSubmittedB);
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
                    RecordFail($"Route B request completed without success. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
                Mark(ref _seqCompletedB);
            }
        }

        private void HandleRequestAEvent(RouteRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null)
            {
                return;
            }

            if (!ReferenceEquals(requestEvent.TargetRoute, routeA))
            {
                RecordFail("Route A trigger published an event for the wrong target Route.");
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedA++;
                Mark(ref _seqSubmittedA);
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
                    RecordFail($"Route A request completed without success. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
                Mark(ref _seqCompletedA);
            }
        }

        private void HandleInitialARouteExited(QaNew003RouteLifecycleProbe probe, RouteContentLifecycleContext context)
        {
            _routeAExited++;
            Mark(ref _seqRouteAExited);
            if (!ReferenceEquals(context.Route, routeA) || !ReferenceEquals(context.NextRoute, routeB))
            {
                RecordFail("Route A Exited callback did not identify the exact A -> B transition.");
            }
        }

        private void HandleRouteBEntered(QaNew003RouteLifecycleProbe probe, RouteContentLifecycleContext context)
        {
            _routeBEntered++;
            Mark(ref _seqRouteBEntered);
            if (!ReferenceEquals(context.Route, routeB) || !ReferenceEquals(context.PreviousRoute, routeA))
            {
                RecordFail("Route B Entered callback did not identify the exact A -> B transition.");
            }
        }

        private void HandleRouteBExited(QaNew003RouteLifecycleProbe probe, RouteContentLifecycleContext context)
        {
            _routeBExited++;
            Mark(ref _seqRouteBExited);
            if (!ReferenceEquals(context.Route, routeB) || !ReferenceEquals(context.NextRoute, routeA))
            {
                RecordFail("Route B Exited callback did not identify the exact B -> A transition.");
            }
        }

        private void HandleReturnedARouteEntered(QaNew003RouteLifecycleProbe probe, RouteContentLifecycleContext context)
        {
            _routeAReturned++;
            Mark(ref _seqRouteAReturned);
            if (!ReferenceEquals(context.Route, routeA) || !ReferenceEquals(context.PreviousRoute, routeB))
            {
                RecordFail("Returned Route A Entered callback did not identify the exact B -> A transition.");
            }
        }

        private void HandleInitialASceneReleasing()
        {
            _sceneAReleasing++;
            Mark(ref _seqSceneAReleasing);
        }

        private void HandleRouteBSceneAvailable()
        {
            _sceneBAvailable++;
            Mark(ref _seqSceneBAvailable);
        }

        private void HandleRouteBSceneReleasing()
        {
            _sceneBReleasing++;
            Mark(ref _seqSceneBReleasing);
        }

        private void HandleReturnedASceneAvailable()
        {
            _sceneAReturnAvailable++;
            Mark(ref _seqSceneAReturnAvailable);
        }

        private bool TryValidateBPhase(out string issue)
        {
            if (_submittedB != 1 || _completedB != 1 || _succeededB != 1 ||
                _routeAExited != 1 || _sceneAReleasing != 1 ||
                _sceneAUnloaded != 1 || _sceneBLoaded != 1 ||
                _sceneBAvailable != 1 || _routeBEntered != 1)
            {
                issue = BuildMissingBTerminalIssue();
                return false;
            }

            if (!AreStrictlyIncreasing(
                    _seqSubmittedB,
                    _seqRouteAExited,
                    _seqSceneAReleasing,
                    _seqSceneAUnloaded,
                    _seqSceneBLoaded,
                    _seqSceneBAvailable,
                    _seqRouteBEntered,
                    _seqCompletedB))
            {
                issue = "Route A -> B public/physical evidence was observed out of causal order.";
                return false;
            }

            if (_initialAProbe != null || _routeBProbe == null ||
                _routeBProbe.EnterCount != 1 || _routeBProbe.ExitCount != 0 ||
                !_routeBProbe.IsRouteContentActive ||
                !IsExactSceneActive(routeB) || IsExactSceneLoaded(routeA) ||
                !OwnerSurvived())
            {
                issue = "Route B terminal state did not prove physical replacement of A with the same persistent execution owner.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryValidateCompleteCycle(out string issue)
        {
            if (_submittedA != 1 || _completedA != 1 || _succeededA != 1 ||
                _routeBExited != 1 || _sceneBReleasing != 1 ||
                _sceneBUnloaded != 1 || _sceneAReturnLoaded != 1 ||
                _sceneAReturnAvailable != 1 || _routeAReturned != 1)
            {
                issue = BuildMissingATerminalIssue();
                return false;
            }

            if (!AreStrictlyIncreasing(
                    _seqCompletedB,
                    _seqSubmittedA,
                    _seqRouteBExited,
                    _seqSceneBReleasing,
                    _seqSceneBUnloaded,
                    _seqSceneAReturnLoaded,
                    _seqSceneAReturnAvailable,
                    _seqRouteAReturned,
                    _seqCompletedA))
            {
                issue = "Route B -> A public/physical evidence was observed out of causal order.";
                return false;
            }

            if (_routeBProbe != null || _returnedAProbe == null ||
                _returnedAProbe.EnterCount != 1 || _returnedAProbe.ExitCount != 0 ||
                !_returnedAProbe.IsRouteContentActive ||
                _returnedAProbeInstanceId == _initialAProbeInstanceId ||
                !IsExactSceneActive(routeA) || IsExactSceneLoaded(routeB) ||
                !OwnerSurvived())
            {
                issue = "Route A terminal state did not prove a new A scene instance with the same persistent execution owner.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryVerifyRestoredBaseline(out string issue)
        {
            if (HasRequestInFlight() || !IsExactSceneActive(routeA) || IsExactSceneLoaded(routeB) || !OwnerSurvived())
            {
                issue = $"Restored baseline requires active A, unloaded B, no request in flight and the original execution owner. active='{SceneManager.GetActiveScene().path}' bLoaded='{IsExactSceneLoaded(routeB)}' inFlight='{HasRequestInFlight()}' ownerSurvived='{OwnerSurvived()}'.";
                return false;
            }

            Scene sceneA = SceneManager.GetSceneByPath(routeA.PrimaryScenePath);
            if (!TryResolveSceneEvidence(sceneA, routeA, out QaNew003RouteLifecycleProbe probe, out SceneLifecycleEvents sceneEvents, out issue))
            {
                return false;
            }

            if (!probe.IsRouteContentActive || probe.EnterCount != 1 || probe.ExitCount != 0 ||
                !ReferenceEquals(probe.LastEnteredContext.Route, routeA) ||
                sceneEvents.AvailableCount != 1 || sceneEvents.ReleasingCount != 0 ||
                !string.Equals(sceneEvents.LastEvent, "Available", StringComparison.Ordinal))
            {
                issue = "Active Route A scene does not expose the expected restored public lifecycle baseline.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private static bool TryResolveSceneEvidence(
            Scene scene,
            RouteAsset expectedRoute,
            out QaNew003RouteLifecycleProbe probe,
            out SceneLifecycleEvents sceneEvents,
            out string issue)
        {
            probe = null;
            sceneEvents = null;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                issue = "Expected Primary Scene is not loaded.";
                return false;
            }

            var probes = new List<QaNew003RouteLifecycleProbe>();
            var lifecycleEvents = new List<SceneLifecycleEvents>();
            var contributions = new List<RouteContentContribution>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                probes.AddRange(roots[rootIndex].GetComponentsInChildren<QaNew003RouteLifecycleProbe>(true));
                lifecycleEvents.AddRange(roots[rootIndex].GetComponentsInChildren<SceneLifecycleEvents>(true));
                contributions.AddRange(roots[rootIndex].GetComponentsInChildren<RouteContentContribution>(true));
            }

            if (probes.Count != 1 || lifecycleEvents.Count != 1 || contributions.Count != 1)
            {
                issue = $"Expected exactly one Route contribution, one Route lifecycle probe and one Scene lifecycle observer in '{scene.path}', found contributions='{contributions.Count}' probes='{probes.Count}' sceneEvents='{lifecycleEvents.Count}'.";
                return false;
            }

            probe = probes[0];
            sceneEvents = lifecycleEvents[0];
            RouteContentContribution contribution = contributions[0];
            bool probeOwned = probe.transform == contribution.transform ||
                probe.transform.IsChildOf(contribution.transform);
            bool sceneEventsOwned = sceneEvents.transform == contribution.transform ||
                sceneEvents.transform.IsChildOf(contribution.transform);
            if (!ReferenceEquals(contribution.Route, expectedRoute) ||
                !probeOwned || !sceneEventsOwned)
            {
                issue = "The exact expected Route Content Contribution must own both public lifecycle observers.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool HasRequestInFlight()
        {
            return (requestRouteA != null && requestRouteA.IsRequestInFlight) ||
                (requestRouteB != null && requestRouteB.IsRequestInFlight);
        }

        private bool OwnerSurvived()
        {
            return this != null && GetInstanceID() == _ownerInstanceId &&
                gameObject.scene.IsValid() && gameObject.scene.handle == _ownerSceneHandle;
        }

        private static bool IsExactSceneLoaded(RouteAsset route)
        {
            if (route == null || string.IsNullOrWhiteSpace(route.PrimaryScenePath))
            {
                return false;
            }

            Scene scene = SceneManager.GetSceneByPath(route.PrimaryScenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        private static bool IsExactSceneActive(RouteAsset route)
        {
            return route != null && SceneMatches(SceneManager.GetActiveScene(), route);
        }

        private static bool SceneMatches(Scene scene, RouteAsset route)
        {
            return scene.IsValid() && route != null &&
                string.Equals(scene.path, route.PrimaryScenePath, StringComparison.OrdinalIgnoreCase);
        }

        private void Mark(ref int sequenceField)
        {
            _sequence++;
            if (sequenceField == 0)
            {
                sequenceField = _sequence;
            }
        }

        private static bool AreStrictlyIncreasing(params int[] values)
        {
            if (values == null || values.Length == 0 || values[0] <= 0)
            {
                return false;
            }

            for (int index = 1; index < values.Length; index++)
            {
                if (values[index] <= values[index - 1])
                {
                    return false;
                }
            }

            return true;
        }

        private string BuildMissingBTerminalIssue()
        {
            return $"Route A -> B evidence was incomplete. submittedB='{_submittedB}' completedB='{_completedB}' succeededB='{_succeededB}' exitA='{_routeAExited}' releasingA='{_sceneAReleasing}' unloadA='{_sceneAUnloaded}' loadB='{_sceneBLoaded}' availableB='{_sceneBAvailable}' enterB='{_routeBEntered}'.";
        }

        private string BuildMissingATerminalIssue()
        {
            return $"Route B -> A evidence was incomplete. submittedA='{_submittedA}' completedA='{_completedA}' succeededA='{_succeededA}' exitB='{_routeBExited}' releasingB='{_sceneBReleasing}' unloadB='{_sceneBUnloaded}' loadA='{_sceneAReturnLoaded}' availableA='{_sceneAReturnAvailable}' enterA='{_routeAReturned}'.";
        }

        private void RecordFail(string issue)
        {
            if (!_hasDivergence)
            {
                _hasDivergence = true;
            }
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, issue);
        }

        private void RecordBlocked(string issue)
        {
            if (!_hasDivergence)
            {
                _hasDivergence = true;
            }
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, issue);
        }

        private void DisposeObservations()
        {
            _requestBBinding?.Dispose();
            _requestBBinding = null;
            _requestABinding?.Dispose();
            _requestABinding = null;

            if (_initialAProbe != null)
            {
                _initialAProbe.Exited -= HandleInitialARouteExited;
            }
            if (_routeBProbe != null)
            {
                _routeBProbe.Entered -= HandleRouteBEntered;
                _routeBProbe.Exited -= HandleRouteBExited;
            }
            if (_returnedAProbe != null)
            {
                _returnedAProbe.Entered -= HandleReturnedARouteEntered;
            }
            if (_initialASceneEvents != null)
            {
                _initialASceneEvents.Releasing.RemoveListener(HandleInitialASceneReleasing);
            }
            if (_routeBSceneEvents != null)
            {
                _routeBSceneEvents.Available.RemoveListener(HandleRouteBSceneAvailable);
                _routeBSceneEvents.Releasing.RemoveListener(HandleRouteBSceneReleasing);
            }
            if (_returnedASceneEvents != null)
            {
                _returnedASceneEvents.Available.RemoveListener(HandleReturnedASceneAvailable);
            }

            if (_observingScenes)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                SceneManager.sceneUnloaded -= HandleSceneUnloaded;
                _observingScenes = false;
            }
        }

        private void PublishTerminal(bool baselineRestored, string cleanupIssue)
        {
            if (_terminalPublished)
            {
                return;
            }

            _terminalPublished = true;
            QaCertificationResult result = _certification.CreateResult(
                ScenarioId,
                baselineRestored ? QaCleanupDisposition.BaselineRestored : QaCleanupDisposition.FreshBootRequired,
                cleanupIssue);
            string status = result.Verdict switch
            {
                QaCertificationVerdict.Pass => "Passed",
                QaCertificationVerdict.Blocked => "Blocked",
                _ => "Failed"
            };
            string message =
                $"[{result.ScenarioId}] status='{status}' verdict='{result.Verdict.ToString().ToUpperInvariant()}' " +
                $"submittedB='{_submittedB}' completedB='{_completedB}' succeededB='{_succeededB}' " +
                $"exitA='{_routeAExited}' releasingA='{_sceneAReleasing}' unloadA='{_sceneAUnloaded}' loadB='{_sceneBLoaded}' availableB='{_sceneBAvailable}' enterB='{_routeBEntered}' " +
                $"submittedA='{_submittedA}' completedA='{_completedA}' succeededA='{_succeededA}' " +
                $"exitB='{_routeBExited}' releasingB='{_sceneBReleasing}' unloadB='{_sceneBUnloaded}' loadA='{_sceneAReturnLoaded}' availableA='{_sceneAReturnAvailable}' enterA='{_routeAReturned}' " +
                $"initialAInstance='{_initialAProbeInstanceId}' returnedAInstance='{_returnedAProbeInstanceId}' ownerInstance='{_ownerInstanceId}' ownerSurvived='{OwnerSurvived()}' " +
                $"baselineRestored='{baselineRestored}' cleanup='{result.CleanupDisposition}' " +
                $"firstDivergence='{SanitizeDiagnostic(result.FirstCausalDivergence)}' cleanupIssue='{SanitizeDiagnostic(result.CleanupIssue)}'.";

            switch (result.Verdict)
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

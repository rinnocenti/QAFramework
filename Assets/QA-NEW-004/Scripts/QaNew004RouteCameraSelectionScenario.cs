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

        [Header("Route C contract (CAMERA-037-C same-Output replacement)")]
        [SerializeField] private RouteAsset routeC;
        [SerializeField] private RouteRequestTrigger requestRouteC;

        [Header("Activity D contract (CAMERA-037-D empty Activity preserves Camera)")]
        [SerializeField] private RouteAsset routeD;
        [SerializeField] private ActivityAsset activityDA;
        [SerializeField] private ActivityAsset activityDB;
        [SerializeField] private RouteRequestTrigger requestRouteD;
        [SerializeField] private ActivityRequestTrigger requestActivityDB;

        [Header("Activity E contract (CAMERA-037-E same-Output replacement)")]
        [SerializeField] private RouteAsset routeE;
        [SerializeField] private ActivityAsset activityEA;
        [SerializeField] private ActivityAsset activityEC;
        [SerializeField] private RouteRequestTrigger requestRouteE;
        [SerializeField] private ActivityRequestTrigger requestActivityEC;

        [Header("Local Scenario deadlines")]
        [SerializeField, Min(0.1f)] private float baselineTimeoutSeconds = 10f;
        [SerializeField, Min(0.1f)] private float transitionTimeoutSeconds = 15f;
        [SerializeField, Min(0.1f)] private float cleanupTimeoutSeconds = 15f;

        private readonly QaCertificationRecorder _certification =
            new QaCertificationRecorder();
        private IEventBinding _requestBBinding;
        private IEventBinding _requestABinding;
        private IEventBinding _requestCBinding;
        private IEventBinding _requestRouteDBinding;
        private IEventBinding _requestActivityDBBinding;
        private IEventBinding _requestRouteEBinding;
        private IEventBinding _requestActivityECBinding;
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
        private int _submittedC;
        private int _completedC;
        private int _succeededC;
        private int _submittedRouteD;
        private int _completedRouteD;
        private int _succeededRouteD;
        private int _submittedActivityDB;
        private int _completedActivityDB;
        private int _succeededActivityDB;
        private int _submittedRouteE;
        private int _completedRouteE;
        private int _succeededRouteE;
        private int _submittedActivityEC;
        private int _completedActivityEC;
        private int _succeededActivityEC;
        private int _routeAExited;
        private int _routeASceneUnloaded;
        private bool _waitSucceeded;
        private bool _baselineCaptured;
        private bool _contractProved;
        private bool _legCProved;
        private bool _legDProved;
        private bool _legEProved;
        private bool _observedWinnerDrop;
        private bool _hasDivergence;
        private bool _terminalPublished;
        private RouteAsset _expectedRouteAfterAExit;

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
            _requestCBinding =
                requestRouteC.SubscribeRequestEvents(HandleRequestCEvent);
            _requestRouteDBinding =
                requestRouteD.SubscribeRequestEvents(HandleRequestRouteDEvent);
            _requestActivityDBBinding =
                requestActivityDB.SubscribeRequestEvents(
                    HandleRequestActivityDBEvent);
            _requestRouteEBinding =
                requestRouteE.SubscribeRequestEvents(HandleRequestRouteEEvent);
            _requestActivityECBinding =
                requestActivityEC.SubscribeRequestEvents(
                    HandleRequestActivityECEvent);

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
            _expectedRouteAfterAExit = routeB;
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
                yield return RunRouteCReplacementLeg();

                if (_legCProved && !_hasDivergence)
                {
                    // Return to the Route A baseline so a subsequent Play
                    // session starts from the same composition state. This
                    // restoration is best-effort hygiene, not itself proof
                    // evidence for the A -> C contract.
                    yield return RestoreRouteAAfterRouteC();

                    if (!_hasDivergence)
                    {
                        yield return RunActivityContinuityLeg();

                        if (_legDProved && !_hasDivergence)
                        {
                            yield return RunActivityReplacementLeg();
                        }
                    }
                }
            }

            bool overallProved =
                _contractProved && baselineRestored && !_hasDivergence &&
                _legCProved && _legDProved && _legEProved;
            if (overallProved)
            {
                _certification.RecordPass();
            }
            else if (!_hasDivergence)
            {
                RecordBlocked(
                    !_contractProved
                        ? "The Scenario ended before the A -> B contract was proved."
                        : !baselineRestored
                            ? "The A -> B contract was observed, but Route A cleanup did not restore the baseline. " + cleanupIssue
                            : !_legCProved
                                ? "The A -> B contract was proved, but the A -> C replacement leg did not complete."
                                : !_legDProved
                                    ? "The A -> B and A -> C contracts were proved, but the Activity D-A -> D-B leg did not complete."
                                    : "The B/C/D contracts were proved, but the Activity E-A -> E-C replacement leg did not complete.");
            }

            DisposeObservations();
            PublishTerminal(baselineRestored, cleanupIssue);
        }

        private IEnumerator RunRouteCReplacementLeg()
        {
            if (!TryRebindRouteAProbe(out string rebindIssue))
            {
                RecordFail(
                    "Route C leg could not rebind the restored Route A lifecycle probe. " +
                    rebindIssue);
                yield break;
            }

            if (!TryResolveCurrentOccurrence(
                    out QaNew004CameraOccurrenceProbe beforeOccurrence,
                    out CameraRequest beforeRequest,
                    out string beforeIssue))
            {
                RecordFail(
                    "Route C leg could not resolve the restored Route A occurrence baseline. " +
                    beforeIssue);
                yield break;
            }

            int exitBefore = _routeAExited;
            int unloadBefore = _routeASceneUnloaded;

            _expectedRouteAfterAExit = routeC;
            requestRouteC.RequestRoute();
            if (_submittedC != 1)
            {
                RecordFail(
                    $"Route C request was not accepted. submitted='{_submittedC}' inFlight='{requestRouteC.IsRequestInFlight}'.");
                yield break;
            }

            yield return WaitForCondition(
                () => _completedC >= 1 &&
                      !requestRouteC.IsRequestInFlight &&
                      _routeASceneUnloaded > unloadBefore,
                transitionTimeoutSeconds);
            if (!_waitSucceeded)
            {
                RecordFail(
                    $"A -> C did not reach terminal teardown evidence. completedC='{_completedC}' succeededC='{_succeededC}' exitDelta='{_routeAExited - exitBefore}' unloadDelta='{_routeASceneUnloaded - unloadBefore}'.");
                yield break;
            }

            if (!TryValidateReplacementContinuity(
                    beforeOccurrence,
                    beforeRequest,
                    exitBefore,
                    unloadBefore,
                    out string continuityIssue))
            {
                RecordFail(continuityIssue);
                yield break;
            }

            _legCProved = true;
        }

        private IEnumerator RestoreRouteAAfterRouteC()
        {
            if (requestRouteA == null ||
                !requestRouteA.HasRouteRuntimeBinding ||
                requestRouteA.IsRequestInFlight ||
                !IsExactSceneActive(routeC))
            {
                yield break;
            }

            int completedBefore = _completedA;
            requestRouteA.RequestRoute();
            yield return WaitForCondition(
                () => _completedA > completedBefore &&
                      !requestRouteA.IsRequestInFlight,
                cleanupTimeoutSeconds);
        }

        // CAMERA-037-D: proves Activity A -> B empty-selection continuity
        // through the real ActivityFlowRuntime (Route D has no persistent
        // selection of its own; its Startup Activity D-A does). Runs from
        // the restored Route A baseline: entering Route D replaces A's
        // persistent selection with a fresh Activity D-A occurrence (same
        // shared Route/Activity mechanism as the A -> C leg); Activity D-B
        // then declares zero selections and must be a pure no-op for that
        // occurrence, while Activity D-A's own content lifecycle must report
        // a real exit.
        private IEnumerator RunActivityContinuityLeg()
        {
            _expectedRouteAfterAExit = routeD;
            requestRouteD.RequestRoute();
            if (_submittedRouteD != 1)
            {
                RecordFail(
                    $"Route D request was not accepted. submitted='{_submittedRouteD}' inFlight='{requestRouteD.IsRequestInFlight}'.");
                yield break;
            }

            yield return WaitForCondition(
                () => _completedRouteD >= 1 &&
                      !requestRouteD.IsRequestInFlight,
                transitionTimeoutSeconds);
            if (!_waitSucceeded || _succeededRouteD != 1)
            {
                RecordFail(
                    $"Route D (Startup Activity D-A) did not complete. completedRouteD='{_completedRouteD}' succeededRouteD='{_succeededRouteD}'.");
                yield break;
            }

            if (!IsExactSceneActive(routeD))
            {
                RecordFail(
                    $"Route D did not become the active scene. active='{SceneManager.GetActiveScene().path}'.");
                yield break;
            }

            if (!TryResolveActivityDAProbe(
                    out QaNew004ActivityLifecycleProbe activityDAProbe,
                    out string probeIssue) ||
                activityDAProbe.EnterCount != 1 ||
                !activityDAProbe.IsActivityContentActive)
            {
                RecordFail(
                    "Activity D-A did not become active through its public lifecycle. " +
                    probeIssue);
                yield break;
            }

            if (!TryResolveCurrentOccurrence(
                    out QaNew004CameraOccurrenceProbe beforeOccurrence,
                    out CameraRequest beforeRequest,
                    out string beforeIssue))
            {
                RecordFail(
                    "Activity D leg could not resolve the Activity D-A occurrence baseline. " +
                    beforeIssue);
                yield break;
            }

            if (beforeRequest.Owner.Kind != CameraRequestOwnerKind.Session ||
                beforeRequest.Lifetime.Kind != CameraRequestLifetimeKind.Session)
            {
                RecordFail(
                    "Activity D-A's normal winner is not a Session-owned persistent selection.");
                yield break;
            }

            bool activityDAExited = false;
            void HandleActivityDAExited(
                QaNew004ActivityLifecycleProbe probe,
                Immersive.Framework.ActivityFlow.ActivityContentLifecycleContext
                    context)
            {
                activityDAExited = true;
            }

            activityDAProbe.Exited += HandleActivityDAExited;

            requestActivityDB.RequestActivity();
            if (_submittedActivityDB != 1)
            {
                activityDAProbe.Exited -= HandleActivityDAExited;
                RecordFail(
                    $"Activity D-B request was not accepted. submitted='{_submittedActivityDB}' inFlight='{requestActivityDB.IsRequestInFlight}'.");
                yield break;
            }

            yield return WaitForCondition(
                () => _completedActivityDB >= 1 &&
                      !requestActivityDB.IsRequestInFlight,
                transitionTimeoutSeconds);
            activityDAProbe.Exited -= HandleActivityDAExited;
            if (!_waitSucceeded || _succeededActivityDB != 1)
            {
                RecordFail(
                    $"Activity D-B request did not complete. completed='{_completedActivityDB}' succeeded='{_succeededActivityDB}'.");
                yield break;
            }

            if (!activityDAExited)
            {
                RecordFail(
                    "Activity D-A's content lifecycle did not report exit; Activity D-A does not appear to have really ended.");
                yield break;
            }

            if (!TryResolveCurrentOccurrence(
                    out QaNew004CameraOccurrenceProbe afterOccurrence,
                    out CameraRequest afterRequest,
                    out string afterIssue))
            {
                RecordFail(
                    "Activity D leg could not resolve the occurrence after Activity D-B entry. " +
                    afterIssue);
                yield break;
            }

            if (!ReferenceEquals(afterOccurrence, beforeOccurrence) ||
                afterRequest.RequestId != beforeRequest.RequestId)
            {
                RecordFail(
                    "Activity D-B did not preserve the exact same Camera occurrence and normal CameraRequest as Activity D-A.");
                yield break;
            }

            if (_output.Applicator == null ||
                !_output.Applicator.HasAppliedRequest ||
                _output.Applicator.AppliedRequestId != afterRequest.RequestId)
            {
                RecordFail(
                    "The preserved normal winner was not applied while Activity D-B is active.");
                yield break;
            }

            if (_observedWinnerDrop)
            {
                RecordFail(
                    "The normal winner dropped (fell through to Default) at some point during observed transitions, including the Activity D-A -> D-B leg.");
                yield break;
            }

            _legDProved = true;
        }

        // CAMERA-037-E: proves same-Output replacement through the real
        // Activity lifecycle. Route E has no Camera selection of its own;
        // Startup Activity E-A establishes Camera A, then Activity E-C
        // declares Camera C. The old request must remain admitted until C is
        // ready, C must be the normal Session-owned winner when the public
        // request completes, and E-A must have exited through its content
        // lifecycle before the surviving occurrence is inspected.
        private IEnumerator RunActivityReplacementLeg()
        {
            requestRouteE.RequestRoute();
            if (_submittedRouteE != 1)
            {
                RecordFail(
                    $"Route E request was not accepted. submitted='{_submittedRouteE}' inFlight='{requestRouteE.IsRequestInFlight}'.");
                yield break;
            }

            yield return WaitForCondition(
                () => _completedRouteE >= 1 &&
                      !requestRouteE.IsRequestInFlight,
                transitionTimeoutSeconds);
            if (!_waitSucceeded || _succeededRouteE != 1 ||
                !IsExactSceneActive(routeE))
            {
                RecordFail(
                    $"Route E (Startup Activity E-A) did not complete. completedRouteE='{_completedRouteE}' succeededRouteE='{_succeededRouteE}' active='{SceneManager.GetActiveScene().path}'.");
                yield break;
            }

            if (!TryResolveActivityProbe(
                    activityEA,
                    out QaNew004ActivityLifecycleProbe activityEAProbe,
                    out string probeIssue) ||
                activityEAProbe.EnterCount != 1 ||
                !activityEAProbe.IsActivityContentActive)
            {
                RecordFail(
                    "Activity E-A did not become active through its public lifecycle. " +
                    probeIssue);
                yield break;
            }

            if (!TryResolveCurrentOccurrence(
                    out QaNew004CameraOccurrenceProbe beforeOccurrence,
                    out CameraRequest beforeRequest,
                    out string beforeIssue))
            {
                RecordFail(
                    "Activity E leg could not resolve the Activity E-A Camera A winner. " +
                    beforeIssue);
                yield break;
            }

            if (beforeRequest.Owner.Kind != CameraRequestOwnerKind.Session ||
                beforeRequest.Lifetime.Kind != CameraRequestLifetimeKind.Session)
            {
                RecordFail(
                    "Activity E-A's normal Camera A winner is not Session-owned.");
                yield break;
            }

            string beforeOccurrenceToken = beforeOccurrence.OccurrenceToken;
            bool activityEAExited = false;
            bool activityEAExitWasToC = false;
            void HandleActivityEAExited(
                QaNew004ActivityLifecycleProbe probe,
                Immersive.Framework.ActivityFlow.ActivityContentLifecycleContext
                    context)
            {
                probe.Exited -= HandleActivityEAExited;
                activityEAExited = true;
                activityEAExitWasToC =
                    ReferenceEquals(context.Activity, activityEA) &&
                    ReferenceEquals(context.NextActivity, activityEC);
            }

            activityEAProbe.Exited += HandleActivityEAExited;
            requestActivityEC.RequestActivity();
            if (_submittedActivityEC != 1)
            {
                activityEAProbe.Exited -= HandleActivityEAExited;
                RecordFail(
                    $"Activity E-C request was not accepted. submitted='{_submittedActivityEC}' inFlight='{requestActivityEC.IsRequestInFlight}'.");
                yield break;
            }

            yield return WaitForCondition(
                () => _completedActivityEC >= 1 &&
                      !requestActivityEC.IsRequestInFlight,
                transitionTimeoutSeconds);
            if (!_waitSucceeded || _succeededActivityEC != 1)
            {
                RecordFail(
                    $"Activity E-C request did not complete successfully. submitted='{_submittedActivityEC}' completed='{_completedActivityEC}' succeeded='{_succeededActivityEC}'.");
                yield break;
            }

            if (!activityEAExited || !activityEAExitWasToC)
            {
                RecordFail(
                    "Activity E-A did not report its exact public exit to Activity E-C.");
                yield break;
            }

            if (!TryResolveCurrentOccurrence(
                    out QaNew004CameraOccurrenceProbe afterOccurrence,
                    out CameraRequest afterRequest,
                    out string afterIssue))
            {
                RecordFail(
                    "Activity E leg could not resolve Camera C after Activity E-A teardown. " +
                    afterIssue);
                yield break;
            }

            if (ReferenceEquals(afterOccurrence, beforeOccurrence) ||
                afterOccurrence.OccurrenceToken == beforeOccurrenceToken ||
                afterRequest.RequestId == beforeRequest.RequestId)
            {
                RecordFail(
                    "Activity E-C did not materialize a fresh Camera occurrence and request.");
                yield break;
            }

            if (_output.Context.Contains(beforeRequest.RequestId))
            {
                RecordFail(
                    "Activity E-A's Camera A request remains admitted after the Activity E-C commit.");
                yield break;
            }

            if (afterRequest.Owner.Kind != CameraRequestOwnerKind.Session ||
                afterRequest.Lifetime.Kind != CameraRequestLifetimeKind.Session)
            {
                RecordFail(
                    "Activity E-C's surviving Camera C occurrence is not Session-owned.");
                yield break;
            }

            if (!_output.Context.HasWinner ||
                _output.Context.Winner.RequestId != afterRequest.RequestId ||
                _output.Applicator == null ||
                !_output.Applicator.HasAppliedRequest ||
                _output.Applicator.AppliedRequestId != afterRequest.RequestId)
            {
                RecordFail(
                    "Activity E-C is not the applied normal winner after Activity E-A teardown.");
                yield break;
            }

            if (_observedWinnerDrop)
            {
                RecordFail(
                    "The normal winner dropped (fell through to Default) during the Activity E-A -> E-C replacement.");
                yield break;
            }

            _legEProved = true;
        }

        private bool TryResolveActivityDAProbe(
            out QaNew004ActivityLifecycleProbe probe,
            out string issue)
        {
            return TryResolveActivityProbe(activityDA, out probe, out issue);
        }

        private static bool TryResolveActivityProbe(
            ActivityAsset expectedActivity,
            out QaNew004ActivityLifecycleProbe probe,
            out string issue)
        {
            QaNew004ActivityLifecycleProbe[] probes =
                FindObjectsByType<QaNew004ActivityLifecycleProbe>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (probes.Length != 1 || probes[0] == null ||
                !ReferenceEquals(
                    probes[0].LastEnteredContext.Activity,
                    expectedActivity))
            {
                probe = null;
                issue =
                    $"Expected exactly one live lifecycle probe for Activity '{(expectedActivity != null ? expectedActivity.ActivityName : "<null>")}'. found='{probes.Length}'.";
                return false;
            }

            probe = probes[0];
            issue = string.Empty;
            return true;
        }

        private bool TryValidateReplacementContinuity(
            QaNew004CameraOccurrenceProbe beforeOccurrence,
            CameraRequest beforeRequest,
            int exitBefore,
            int unloadBefore,
            out string issue)
        {
            if (_succeededC != 1 ||
                _routeAExited != exitBefore + 1 ||
                _routeASceneUnloaded != unloadBefore + 1 ||
                !IsExactSceneActive(routeC) ||
                IsExactSceneLoaded(routeA))
            {
                issue =
                    $"Route replacement teardown is incomplete. succeededC='{_succeededC}' exitDelta='{_routeAExited - exitBefore}' unloadDelta='{_routeASceneUnloaded - unloadBefore}' active='{SceneManager.GetActiveScene().path}'.";
                return false;
            }

            if (!TryResolveRouteProbe(
                    routeC,
                    out QaNew004RouteLifecycleProbe routeCProbe,
                    out issue) ||
                routeCProbe.EnterCount != 1 ||
                !routeCProbe.IsRouteContentActive)
            {
                issue = string.IsNullOrWhiteSpace(issue)
                    ? "Route C did not become active through its public lifecycle."
                    : issue;
                return false;
            }

            if (!TryResolveCurrentOccurrence(
                    out QaNew004CameraOccurrenceProbe afterOccurrence,
                    out CameraRequest afterRequest,
                    out issue))
            {
                return false;
            }

            // Direct identity/request proof that A did not survive the
            // commit as a persistent selected occurrence/request, preferred
            // over inferring it from a global live-occurrence count (which
            // can transiently include a just-released, deactivated rig
            // still pending Unity's deferred Object.Destroy in Play Mode).
            if (_output.Context.Contains(beforeRequest.RequestId))
            {
                issue =
                    "Route A's previous normal CameraRequest is still admitted on the Output after the Route C commit.";
                return false;
            }

            // A's Session-owned occurrence/request must not survive the
            // commit: exactly one live occurrence exists (enforced by
            // TryResolveCurrentOccurrence) and it must be a genuinely new
            // materialization, not the preserved Route A occurrence.
            if (ReferenceEquals(afterOccurrence, beforeOccurrence) ||
                afterRequest.RequestId == beforeRequest.RequestId)
            {
                issue =
                    "Route C did not replace the previous Camera occurrence and request; A appears to remain the normal winner.";
                return false;
            }

            if (afterRequest.Owner.Kind != CameraRequestOwnerKind.Session ||
                afterRequest.Lifetime.Kind != CameraRequestLifetimeKind.Session)
            {
                issue =
                    "Route C's normal winner is not a Session-owned persistent selection.";
                return false;
            }

            if (_output.Applicator == null ||
                !_output.Applicator.HasAppliedRequest ||
                _output.Applicator.AppliedRequestId != afterRequest.RequestId)
            {
                issue =
                    "Route C's normal winner was not applied after the Route transition released force-default.";
                return false;
            }

            if (_observedWinnerDrop)
            {
                issue =
                    "The normal winner dropped (fell through to Default) at some point during observed transitions, including the A -> C replacement.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryResolveCurrentOccurrence(
            out QaNew004CameraOccurrenceProbe occurrence,
            out CameraRequest request,
            out string issue)
        {
            occurrence = null;
            request = default;

            // Active-only by design: CameraPresentationMaterializationRuntime.
            // Release deactivates a released rig (RigRoot.SetActive(false))
            // synchronously, then schedules Object.Destroy, whose actual
            // destruction Unity defers to after the current Update loop (Play
            // Mode). A just-released occurrence can therefore still be found
            // by FindObjectsByType in the very same frame the commit ran,
            // even though it is no longer selected/admitted. Only an active
            // rig is a currently-selected occurrence.
            QaNew004CameraOccurrenceProbe[] occurrences =
                FindObjectsByType<QaNew004CameraOccurrenceProbe>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);
            if (occurrences.Length != 1 || occurrences[0] == null ||
                string.IsNullOrWhiteSpace(occurrences[0].OccurrenceToken) ||
                occurrences[0].Composer == null)
            {
                issue =
                    $"Expected exactly one live Camera occurrence. found='{occurrences.Length}'.";
                return false;
            }

            if (_output == null || _output.Context == null ||
                !_output.Context.HasWinner)
            {
                issue = "The Camera Output has no normal winner.";
                return false;
            }

            occurrence = occurrences[0];
            request = _output.Context.Winner;
            if (!ReferenceEquals(request.Rig.Composer, occurrence.Composer))
            {
                issue =
                    "The normal winner does not reference the observed occurrence.";
                return false;
            }

            issue = string.Empty;
            return true;
        }

        private bool TryRebindRouteAProbe(out string issue)
        {
            if (!TryResolveRouteProbe(
                    routeA,
                    out QaNew004RouteLifecycleProbe probe,
                    out issue))
            {
                return false;
            }

            // The original Route A lifecycle probe instance was destroyed
            // when its scene unloaded during A -> B; a fresh instance was
            // created when Route A was restored. Re-bind to that instance
            // so the A -> C exit event is observed too.
            if (_initialRouteAProbe != null)
            {
                _initialRouteAProbe.Exited -= HandleRouteAExited;
            }

            _initialRouteAProbe = probe;
            _initialRouteAProbe.Exited += HandleRouteAExited;
            issue = string.Empty;
            return true;
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

            if (routeC == null || requestRouteC == null)
            {
                issue =
                    "QA-NEW-004 requires Route C and its persistent Route Request Trigger for the CAMERA-037-C replacement leg.";
                return false;
            }

            if (!ReferenceEquals(requestRouteC.TargetRoute, routeC))
            {
                issue = "Route Request Trigger targets do not match the authored Routes.";
                return false;
            }

            if (routeC.CameraPresentationSelections.Count != 1)
            {
                issue =
                    "Route C must declare exactly one Camera Presentation Selection for the replacement leg.";
                return false;
            }

            if (routeC.HasCameraPresentations)
            {
                issue =
                    "QA-NEW-004 isolates persistent selection; Route C may not declare contextual Camera Presentations.";
                return false;
            }

            if (ReferenceEquals(
                    routeC.CameraPresentationSelections[0],
                    routeA.CameraPresentationSelections[0]))
            {
                issue =
                    "Route C must declare a distinct Camera Presentation from Route A to prove replacement rather than continuity.";
                return false;
            }

            if (!ReferenceEquals(
                    routeC.CameraPresentationSelections[0].OutputDefinition,
                    routeA.CameraPresentationSelections[0].OutputDefinition))
            {
                issue =
                    "Route C's selection must target the same Camera Output as Route A to prove same-Output replacement.";
                return false;
            }

            if (routeD == null || activityDA == null || activityDB == null ||
                requestRouteD == null || requestActivityDB == null)
            {
                issue =
                    "QA-NEW-004 requires Route D, Activity D-A, Activity D-B and their persistent Request Triggers for the CAMERA-037-D empty-Activity leg.";
                return false;
            }

            if (!ReferenceEquals(requestRouteD.TargetRoute, routeD) ||
                !ReferenceEquals(requestActivityDB.TargetActivity, activityDB))
            {
                issue =
                    "Route D / Activity D-B Request Trigger targets do not match the authored Route D / Activity D-B.";
                return false;
            }

            if (routeD.HasCameraPresentationSelections ||
                routeD.HasCameraPresentations)
            {
                issue =
                    "Route D must declare zero Camera Presentations/Selections; only its Startup Activity D-A may select persistently, to isolate the CAMERA-037-D contract from Route-level selection.";
                return false;
            }

            if (!ReferenceEquals(routeD.StartupActivity, activityDA))
            {
                issue = "Route D's Startup Activity must be Activity D-A.";
                return false;
            }

            if (activityDA.CameraPresentationSelections.Count != 1 ||
                activityDB.HasCameraPresentationSelections)
            {
                issue =
                    "Activity D-A must declare exactly one Camera Presentation Selection and Activity D-B must declare zero selections.";
                return false;
            }

            if (activityDA.HasCameraPresentations ||
                activityDB.HasCameraPresentations)
            {
                issue =
                    "QA-NEW-004 isolates persistent selection; neither Activity D-A nor Activity D-B may declare contextual Camera Presentations.";
                return false;
            }

            if (routeE == null || activityEA == null || activityEC == null ||
                requestRouteE == null || requestActivityEC == null)
            {
                issue =
                    "QA-NEW-004 requires Route E, Activity E-A, Activity E-C and their Request Triggers for CAMERA-037-E.";
                return false;
            }

            if (!ReferenceEquals(requestRouteE.TargetRoute, routeE) ||
                !ReferenceEquals(requestActivityEC.TargetActivity, activityEC) ||
                !ReferenceEquals(routeE.StartupActivity, activityEA))
            {
                issue =
                    "Route E / Activity E-C trigger targets or Route E Startup Activity do not match the authored CAMERA-037-E composition.";
                return false;
            }

            if (routeE.HasCameraPresentationSelections ||
                routeE.HasCameraPresentations ||
                activityEA.HasCameraPresentations ||
                activityEC.HasCameraPresentations ||
                activityEA.CameraPresentationSelections.Count != 1 ||
                activityEC.CameraPresentationSelections.Count != 1)
            {
                issue =
                    "Route E must declare no Camera intent; Activity E-A and E-C must each declare exactly one persistent selection and no contextual Presentation.";
                return false;
            }

            CameraPresentationDefinition activityEASelection =
                activityEA.CameraPresentationSelections[0];
            CameraPresentationDefinition activityECSelection =
                activityEC.CameraPresentationSelections[0];
            if (ReferenceEquals(activityEASelection, activityECSelection) ||
                !ReferenceEquals(
                    activityEASelection.OutputDefinition,
                    activityECSelection.OutputDefinition) ||
                !ReferenceEquals(
                    activityEASelection.OutputDefinition,
                    routeA.CameraPresentationSelections[0].OutputDefinition))
            {
                issue =
                    "Activity E-A and E-C must select distinct Presentations for the same QA-NEW-004 Camera Output.";
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
            _baselineCaptured = true;
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
            if (!_baselineCaptured)
            {
                // Nothing to restore or compare: the Scenario never
                // reached a captured Route A Camera selection baseline
                // (e.g. it was Blocked during composition preflight). The
                // real cause is already recorded as the first causal
                // divergence; leaving cleanupIssue empty here avoids
                // presenting this as a separate, misleading cleanup
                // failure when cleanup never actually ran into a problem.
                issue = string.Empty;
                return false;
            }

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
                // Continuous evidence that the normal winner never drops
                // through to Default once a baseline occurrence exists.
                if (_output != null && !_output.Context.HasWinner)
                {
                    _observedWinnerDrop = true;
                }

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
                !ReferenceEquals(context.NextRoute, _expectedRouteAfterAExit))
            {
                RecordFail(
                    $"Route A exit did not identify the expected transition. expected='{(_expectedRouteAfterAExit != null ? _expectedRouteAfterAExit.RouteName : "<none>")}' actual='{(context.NextRoute != null ? context.NextRoute.RouteName : "<none>")}'.");
            }
        }

        private void HandleRequestCEvent(RouteRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null ||
                !ReferenceEquals(requestEvent.TargetRoute, routeC))
            {
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedC++;
            }
            else if (requestEvent.IsCompleted)
            {
                _completedC++;
                if (requestEvent.Succeeded)
                {
                    _succeededC++;
                }
                else
                {
                    RecordFail(
                        $"Route C request failed. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
            }
        }

        private void HandleRequestRouteDEvent(RouteRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null ||
                !ReferenceEquals(requestEvent.TargetRoute, routeD))
            {
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedRouteD++;
            }
            else if (requestEvent.IsCompleted)
            {
                _completedRouteD++;
                if (requestEvent.Succeeded)
                {
                    _succeededRouteD++;
                }
                else
                {
                    RecordFail(
                        $"Route D request failed. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
            }
        }

        private void HandleRequestActivityDBEvent(
            ActivityRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null ||
                !ReferenceEquals(requestEvent.TargetActivity, activityDB))
            {
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedActivityDB++;
            }
            else if (requestEvent.IsCompleted)
            {
                _completedActivityDB++;
                if (requestEvent.Succeeded)
                {
                    _succeededActivityDB++;
                }
                else
                {
                    RecordFail(
                        $"Activity D-B request failed. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
            }
        }

        private void HandleRequestRouteEEvent(RouteRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null ||
                !ReferenceEquals(requestEvent.TargetRoute, routeE))
            {
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedRouteE++;
            }
            else if (requestEvent.IsCompleted)
            {
                _completedRouteE++;
                if (requestEvent.Succeeded)
                {
                    _succeededRouteE++;
                }
                else
                {
                    RecordFail(
                        $"Route E request failed. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
            }
        }

        private void HandleRequestActivityECEvent(
            ActivityRequestTriggerEvent requestEvent)
        {
            if (requestEvent == null ||
                !ReferenceEquals(requestEvent.TargetActivity, activityEC))
            {
                return;
            }

            if (requestEvent.IsSubmitted)
            {
                _submittedActivityEC++;
            }
            else if (requestEvent.IsCompleted)
            {
                _completedActivityEC++;
                if (requestEvent.Succeeded)
                {
                    _succeededActivityEC++;
                }
                else
                {
                    RecordFail(
                        $"Activity E-C request failed. outcome='{requestEvent.Outcome}' message='{requestEvent.Message}'.");
                }
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
            _requestCBinding?.Dispose();
            _requestCBinding = null;
            _requestRouteDBinding?.Dispose();
            _requestRouteDBinding = null;
            _requestActivityDBBinding?.Dispose();
            _requestActivityDBBinding = null;
            _requestRouteEBinding?.Dispose();
            _requestRouteEBinding = null;
            _requestActivityECBinding?.Dispose();
            _requestActivityECBinding = null;
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
                $"submittedC='{_submittedC}' completedC='{_completedC}' succeededC='{_succeededC}' legCProved='{_legCProved}' observedWinnerDrop='{_observedWinnerDrop}' " +
                $"submittedRouteD='{_submittedRouteD}' completedRouteD='{_completedRouteD}' succeededRouteD='{_succeededRouteD}' " +
                $"submittedActivityDB='{_submittedActivityDB}' completedActivityDB='{_completedActivityDB}' succeededActivityDB='{_succeededActivityDB}' legDProved='{_legDProved}' " +
                $"submittedRouteE='{_submittedRouteE}' completedRouteE='{_completedRouteE}' succeededRouteE='{_succeededRouteE}' " +
                $"submittedActivityEC='{_submittedActivityEC}' completedActivityEC='{_completedActivityEC}' succeededActivityEC='{_succeededActivityEC}' legEProved='{_legEProved}' " +
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

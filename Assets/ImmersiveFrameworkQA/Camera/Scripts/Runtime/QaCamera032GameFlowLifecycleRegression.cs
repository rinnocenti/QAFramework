using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaCamera032GameFlowLifecycleRegression : MonoBehaviour
    {
        private const string Prefix = "[CAMERA-032-GAMEFLOW-LIFECYCLE]";
        private const int FrameBudget = 1200;

        private static readonly string[] ExpectedCases =
        {
            "runtime-ready",
            "activity-winner-established",
            "activity-exit-restores-route",
            "route-force-default-preserves-normal-requests",
            "route-force-default-renders-default",
            "route-replacement-releases-outgoing-scene",
            "route-exit-restores-lower-scope-or-default",
            "route-force-default-release-restores-current-winner"
        };

        [SerializeField] private CameraOutputDefinition outputDefinition;
        [SerializeField] private RouteRequestTrigger routeRequestTrigger;
        [SerializeField] private ActivityRequestTrigger activityRequestTrigger;
        [SerializeField] private bool expectSessionFallback;
        [SerializeField] private string outgoingSceneName = "QA_Player";
        [SerializeField] private string replacementSceneName = "QA_Hub";

        private readonly List<string> completed = new();
        private CameraOutputAuthoring output;
        private CameraOutputSession session;
        private bool started;
        private bool captureTransition;
        private bool forceAppliedObserved;
        private bool forceReleasedObserved;
        private bool forceAppliedDefault;
        private bool forceReleasedDefault;
        private bool forceReleasedRequest;
        private CameraOutputContextSnapshot baselineSnapshot;
        private CameraOutputContextSnapshot forceAppliedSnapshot;
        private CameraOutputContextSnapshot forceReleasedSnapshot;

        public static bool Executed { get; private set; }
        public static bool Passed { get; private set; }
        public static int CompletedCaseCount { get; private set; }
        public static int ExpectedCaseCount => ExpectedCases.Length;
        public static string Diagnostic { get; private set; } = string.Empty;

        public void Configure(
            CameraOutputDefinition authoredOutputDefinition,
            RouteRequestTrigger authoredRouteRequestTrigger,
            ActivityRequestTrigger authoredActivityRequestTrigger,
            bool authoredExpectSessionFallback,
            string authoredOutgoingSceneName,
            string authoredReplacementSceneName)
        {
            outputDefinition = authoredOutputDefinition;
            routeRequestTrigger = authoredRouteRequestTrigger;
            activityRequestTrigger = authoredActivityRequestTrigger;
            expectSessionFallback = authoredExpectSessionFallback;
            outgoingSceneName = authoredOutgoingSceneName ?? string.Empty;
            replacementSceneName = authoredReplacementSceneName ?? string.Empty;
        }

        private void Awake()
        {
            Executed = false;
            Passed = false;
            CompletedCaseCount = 0;
            Diagnostic = string.Empty;
            completed.Clear();
            started = false;
            Application.logMessageReceived +=
                OnLogMessageReceived;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -=
                OnLogMessageReceived;
        }

        private void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            StartCoroutine(RunGuarded());
        }

        private IEnumerator RunGuarded()
        {
            IEnumerator proof = Run();
            while (true)
            {
                object current = null;
                bool hasNext;
                try
                {
                    hasNext = proof.MoveNext();
                    if (hasNext)
                    {
                        current = proof.Current;
                    }
                }
                catch (Exception exception)
                {
                    Fail(exception.GetBaseException().Message);
                    yield break;
                }

                if (!hasNext)
                {
                    yield break;
                }

                yield return current;
            }
        }

        private IEnumerator Run()
        {
            RequireFixture();

            yield return WaitFor(
                TryResolveOutputSession,
                "CAMERA-032 Game Flow QA could not resolve the exact Session Camera Output.");

            yield return WaitFor(
                () => routeRequestTrigger.HasRouteRuntimeBinding &&
                      activityRequestTrigger.HasActivityRuntimeBinding,
                "CAMERA-032 Game Flow QA request triggers did not bind to the persistent Game Flow runtime.");

            Complete("runtime-ready");

            int initialRequestCount = expectSessionFallback ? 3 : 2;
            yield return WaitFor(
                () => session.Context.AdmittedRequestCount == initialRequestCount &&
                      IsWinner(300, CameraRequestOwnerKind.Activity),
                "Startup Activity Presentation did not become the expected normal Camera winner.");

            Complete("activity-winner-established");

            activityRequestTrigger.ClearActivity();

            yield return WaitFor(
                () => !activityRequestTrigger.IsRequestInFlight &&
                      activityRequestTrigger.LastRequestSucceeded &&
                      activityRequestTrigger.LastRequestClearedActivity,
                "Activity clear request did not complete successfully.");

            yield return WaitFor(
                () => session.Context.AdmittedRequestCount ==
                          (expectSessionFallback ? 2 : 1) &&
                      IsWinner(200, CameraRequestOwnerKind.Route) &&
                      !session.IsDefaultForced &&
                      output.Applicator != null &&
                      output.Applicator.HasAppliedRequest,
                "Activity exit did not restore the surviving Route Camera Presentation.");

            Complete("activity-exit-restores-route");

            Require(
                !string.IsNullOrWhiteSpace(outgoingSceneName) &&
                SceneManager.GetSceneByName(outgoingSceneName).isLoaded,
                $"Outgoing Route scene '{outgoingSceneName}' is not loaded before replacement.");

            // Force-default continuity is certified on Route replacement. That
            // lifecycle owns a real primary-scene replacement while the visual
            // transition is covered, so the forced Default remains observable
            // independently from zero-content Activity timing.
            BeginTransitionCapture();

            routeRequestTrigger.RequestRoute();

            yield return WaitFor(
                TryCaptureForceApplied,
                "Route replacement did not expose Camera force-default application on the Session Output.");

            AssertForceAppliedPreservedBaseline();
            Complete("route-force-default-preserves-normal-requests");

            Require(
                forceAppliedDefault,
                "Route replacement force-default did not physically apply the Output Default.");
            Complete("route-force-default-renders-default");

            yield return WaitFor(
                () => !routeRequestTrigger.IsRequestInFlight &&
                      routeRequestTrigger.LastRequestSucceeded,
                "Route replacement request did not complete successfully.");

            yield return WaitFor(
                () => !SceneManager.GetSceneByName(outgoingSceneName).isLoaded &&
                      SceneManager.GetSceneByName(replacementSceneName).isLoaded,
                "Route replacement did not retire the outgoing primary scene and establish the replacement scene.");

            Complete("route-replacement-releases-outgoing-scene");

            if (expectSessionFallback)
            {
                yield return WaitFor(
                    () => session.Context.AdmittedRequestCount == 1 &&
                          IsWinner(100, CameraRequestOwnerKind.Session) &&
                          output.Applicator != null &&
                          output.Applicator.HasAppliedRequest,
                    "Route exit did not restore the surviving Session Camera Presentation.");
            }
            else
            {
                yield return WaitFor(
                    () => session.Context.AdmittedRequestCount == 0 &&
                          !session.Context.HasWinner &&
                          output.Applicator != null &&
                          output.Applicator.HasAppliedDefault,
                    "Route exit did not restore the Session Output Default.");
            }

            Complete("route-exit-restores-lower-scope-or-default");

            yield return WaitFor(
                TryCaptureForceReleased,
                "Route replacement did not release Camera force-default after restoring lower-scope arbitration.");

            AssertForceReleasedMatches(
                expectSessionFallback ? 100 : 0,
                expectSessionFallback
                    ? CameraRequestOwnerKind.Session
                    : CameraRequestOwnerKind.Undefined,
                !expectSessionFallback);
            Complete("route-force-default-release-restores-current-winner");

            EndTransitionCapture();

            Executed = true;
            Passed = completed.Count == ExpectedCases.Length;
            CompletedCaseCount = completed.Count;
            Diagnostic = Passed
                ? $"CAMERA-032 Game Flow lifecycle passed. fallback='{(expectSessionFallback ? "Session" : "Default")}'."
                : $"Case count diverged. actual='{completed.Count}' expected='{ExpectedCases.Length}'.";

            Debug.Log(
                $"{Prefix} status='{(Passed ? "Passed" : "Failed")}' " +
                $"fallback='{(expectSessionFallback ? "Session" : "Default")}' " +
                $"cases='{completed.Count}/{ExpectedCases.Length}' " +
                $"completed='{string.Join(",", completed)}' " +
                $"diagnostic='{Escape(Diagnostic)}'.",
                this);
        }

        private void RequireFixture()
        {
            Require(
                outputDefinition != null &&
                routeRequestTrigger != null &&
                activityRequestTrigger != null,
                "CAMERA-032 Game Flow lifecycle fixture is incomplete.");

            Require(
                outputDefinition.HasValidId,
                "CAMERA-032 Game Flow lifecycle fixture requires a valid Camera Output identity.");
        }

        private bool TryResolveOutputSession()
        {
            if (output != null && session != null)
            {
                return true;
            }

            CameraOutputAuthoring[] candidates =
                FindObjectsByType<CameraOutputAuthoring>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            CameraOutputAuthoring resolved = null;
            int matches = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                CameraOutputAuthoring candidate = candidates[index];
                if (candidate == null ||
                    candidate.OutputDefinition == null ||
                    candidate.OutputDefinition.OutputId !=
                        outputDefinition.OutputId)
                {
                    continue;
                }

                resolved = candidate;
                matches++;
            }

            if (matches != 1 ||
                resolved == null ||
                !resolved.TryGetSession(
                    out CameraOutputSession resolvedSession,
                    out _))
            {
                return false;
            }

            output = resolved;
            session = resolvedSession;
            return true;
        }

        private void BeginTransitionCapture()
        {
            Require(
                session != null &&
                output != null &&
                output.Applicator != null,
                "Camera Output Session/Application is unavailable.");

            Require(
                !session.IsDefaultForced,
                "Transition baseline unexpectedly starts with force-default already active.");

            baselineSnapshot = session.Context.CaptureSnapshot();
            Require(
                baselineSnapshot.HasWinner,
                "Transition baseline requires one normal Camera winner.");

            captureTransition = true;
            forceAppliedObserved = false;
            forceReleasedObserved = false;
            forceAppliedDefault = false;
            forceReleasedDefault = false;
            forceReleasedRequest = false;
            forceAppliedSnapshot = default;
            forceReleasedSnapshot = default;
        }

        private void EndTransitionCapture()
        {
            Require(
                !session.IsDefaultForced,
                "Transition completed while force-default remained active.");
            captureTransition = false;
        }

        private bool TryCaptureForceApplied()
        {
            if (!forceAppliedObserved)
            {
                CaptureForceAppliedIfCurrentStateMatches();
            }

            return forceAppliedObserved;
        }

        private bool TryCaptureForceReleased()
        {
            if (!forceReleasedObserved)
            {
                CaptureForceReleasedIfCurrentStateMatches();
            }

            return forceReleasedObserved;
        }

        private void CaptureForceAppliedIfCurrentStateMatches()
        {
            if (!captureTransition ||
                forceAppliedObserved ||
                session == null ||
                output == null ||
                output.Applicator == null ||
                !session.IsDefaultForced ||
                !output.Applicator.HasAppliedDefault)
            {
                return;
            }

            forceAppliedSnapshot =
                session.Context.CaptureSnapshot();
            forceAppliedDefault =
                output.Applicator.HasAppliedDefault;
            forceAppliedObserved = true;
        }

        private void CaptureForceReleasedIfCurrentStateMatches()
        {
            if (!captureTransition ||
                !forceAppliedObserved ||
                forceReleasedObserved ||
                session == null ||
                output == null ||
                output.Applicator == null ||
                session.IsDefaultForced)
            {
                return;
            }

            forceReleasedSnapshot =
                session.Context.CaptureSnapshot();
            forceReleasedDefault =
                output.Applicator.HasAppliedDefault;
            forceReleasedRequest =
                output.Applicator.HasAppliedRequest;
            forceReleasedObserved = true;
        }

        private void OnLogMessageReceived(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (!captureTransition ||
                string.IsNullOrEmpty(condition))
            {
                return;
            }

            // SessionCameraTransitionOrchestrator logs immediately after each
            // force-default mutation. Latch the public Output Session state in
            // that same synchronous callback so zero-frame transitions cannot
            // evade the per-frame polling fallback above.
            if (condition.IndexOf(
                    "Camera transition force-default applied.",
                    StringComparison.Ordinal) >= 0)
            {
                CaptureForceAppliedIfCurrentStateMatches();
                return;
            }

            if (condition.IndexOf(
                    "Camera transition force-default released.",
                    StringComparison.Ordinal) >= 0)
            {
                CaptureForceReleasedIfCurrentStateMatches();
            }
        }

        private void AssertForceAppliedPreservedBaseline()
        {
            Require(
                forceAppliedObserved,
                "Force-default application evidence was not captured.");
            Require(
                forceAppliedSnapshot.AdmittedRequestCount ==
                    baselineSnapshot.AdmittedRequestCount,
                $"Force-default changed normal request count. before='{baselineSnapshot.AdmittedRequestCount}' forced='{forceAppliedSnapshot.AdmittedRequestCount}'.");
            Require(
                forceAppliedSnapshot.HasWinner == baselineSnapshot.HasWinner,
                "Force-default changed normal winner availability.");
            Require(
                !baselineSnapshot.HasWinner ||
                forceAppliedSnapshot.Winner.RequestId ==
                    baselineSnapshot.Winner.RequestId,
                "Force-default replaced the logical normal Camera winner.");
        }

        private void AssertForceReleasedMatches(
            int expectedPrecedence,
            CameraRequestOwnerKind expectedOwnerKind,
            bool expectedDefault)
        {
            Require(
                forceReleasedObserved,
                "Force-default release evidence was not captured.");

            Require(
                !session.IsDefaultForced,
                "Force-default owner remained active after release.");

            if (expectedDefault)
            {
                Require(
                    !forceReleasedSnapshot.HasWinner &&
                    forceReleasedSnapshot.AdmittedRequestCount == 0 &&
                    forceReleasedDefault &&
                    !forceReleasedRequest,
                    "Force-default release did not restore the Output Default with no normal winner.");
                return;
            }

            Require(
                SnapshotWinnerIs(
                    forceReleasedSnapshot,
                    expectedPrecedence,
                    expectedOwnerKind) &&
                forceReleasedRequest &&
                !forceReleasedDefault,
                $"Force-default release did not restore expected winner owner='{expectedOwnerKind}' precedence='{expectedPrecedence}'.");
        }

        private bool IsWinner(
            int precedence,
            CameraRequestOwnerKind ownerKind)
        {
            return session != null &&
                SnapshotWinnerIs(
                    session.Context.CaptureSnapshot(),
                    precedence,
                    ownerKind);
        }

        private static bool SnapshotWinnerIs(
            CameraOutputContextSnapshot snapshot,
            int precedence,
            CameraRequestOwnerKind ownerKind)
        {
            return snapshot.HasWinner &&
                snapshot.Winner.Policy.Precedence == precedence &&
                snapshot.Winner.Owner.Kind == ownerKind;
        }

        private IEnumerator WaitFor(
            Func<bool> condition,
            string failure)
        {
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            throw new InvalidOperationException(
                $"{failure} {DescribeRuntimeState()}");
        }

        private string DescribeRuntimeState()
        {
            string cameraState = "unresolved";
            if (session != null)
            {
                CameraOutputContextSnapshot snapshot =
                    session.Context.CaptureSnapshot();
                string winner = snapshot.HasWinner
                    ? $"{snapshot.Winner.Owner.Kind}:{snapshot.Winner.Policy.Precedence}:{snapshot.Winner.RequestId}"
                    : "<none>";
                cameraState =
                    $"forced='{session.IsDefaultForced}' " +
                    $"forceOwners='{session.ForceDefaultOwnerCount}' " +
                    $"requests='{snapshot.AdmittedRequestCount}' " +
                    $"winner='{winner}'";
            }

            string applicationState =
                output != null && output.Applicator != null
                    ? $"appliedDefault='{output.Applicator.HasAppliedDefault}' appliedRequest='{output.Applicator.HasAppliedRequest}'"
                    : "applicator='<unresolved>'";

            string routeState =
                routeRequestTrigger != null
                    ? $"routeInFlight='{routeRequestTrigger.IsRequestInFlight}' routeSucceeded='{routeRequestTrigger.LastRequestSucceeded}'"
                    : "routeTrigger='<missing>'";

            string activityState =
                activityRequestTrigger != null
                    ? $"activityInFlight='{activityRequestTrigger.IsRequestInFlight}' activitySucceeded='{activityRequestTrigger.LastRequestSucceeded}' activityCleared='{activityRequestTrigger.LastRequestClearedActivity}'"
                    : "activityTrigger='<missing>'";

            string sceneState =
                $"outgoingLoaded='{SceneManager.GetSceneByName(outgoingSceneName).isLoaded}' " +
                $"replacementLoaded='{SceneManager.GetSceneByName(replacementSceneName).isLoaded}'";

            return
                $"camera=({cameraState}; {applicationState}) " +
                $"flow=({routeState}; {activityState}) " +
                $"scenes=({sceneState})";
        }

        private void Complete(string name)
        {
            int expectedIndex = completed.Count;
            Require(
                expectedIndex < ExpectedCases.Length &&
                string.Equals(
                    ExpectedCases[expectedIndex],
                    name,
                    StringComparison.Ordinal),
                $"Unexpected CAMERA-032 Game Flow case order. actual='{name}' index='{expectedIndex}'.");

            completed.Add(name);
            CompletedCaseCount = completed.Count;
            Debug.Log(
                $"{Prefix} case='{name}' status='Passed' fallback='{(expectSessionFallback ? "Session" : "Default")}'.",
                this);
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private void Fail(string reason)
        {
            Executed = true;
            Passed = false;
            CompletedCaseCount = completed.Count;
            Diagnostic = reason ?? string.Empty;
            Debug.LogError(
                $"{Prefix} status='Failed' " +
                $"fallback='{(expectSessionFallback ? "Session" : "Default")}' " +
                $"cases='{completed.Count}/{ExpectedCases.Length}' " +
                $"completed='{string.Join(",", completed)}' " +
                $"diagnostic='{Escape(Diagnostic)}'.",
                this);
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
    }
}

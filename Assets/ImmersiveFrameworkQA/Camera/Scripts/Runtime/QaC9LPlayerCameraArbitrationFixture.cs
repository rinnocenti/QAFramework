using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaC9LPlayerCameraArbitrationFixture : MonoBehaviour
    {
        private const string LogPrefix = "[QA][C9L Player Camera Arbitration]";

        [SerializeField] private CameraOutputSessionBinding outputSession;
        [SerializeField] private RouteCameraRequestBinding routeBinding;
        [SerializeField] private LocalPlayerCameraRequestBinding playerBinding;
        [SerializeField] private ActivityCameraRequestBinding activityBinding;
        [SerializeField] private LocalPlayerCameraRequestBinding invalidPlayerBinding;

        [SerializeField] private CameraRigComposer routeComposer;
        [SerializeField] private CameraRigComposer playerComposer;
        [SerializeField] private CameraRigComposer activityComposer;

        [SerializeField] private ActivityRequestTrigger activityRequestTrigger;
        [SerializeField] private RouteRequestTrigger backToHubTrigger;

        [SerializeField] private bool runOnStart;
        [SerializeField] private bool throwOnFailure;

        [Header("Debug")]
        [SerializeField] private string lastStatus = "NotRun";
        [SerializeField] private string lastFailure;
        [SerializeField] private int completedCaseCount;

        private readonly List<string> completed = new List<string>();
        private bool hasStarted;

        public string LastStatus => lastStatus ?? string.Empty;
        public string LastFailure => lastFailure ?? string.Empty;
        public int CompletedCaseCount => completedCaseCount;

        private void Start()
        {
            if (runOnStart)
            {
                Begin();
            }
        }

        public void Begin()
        {
            if (hasStarted)
            {
                return;
            }

            hasStarted = true;
            StartCoroutine(Run());
        }

        [ContextMenu("Run C9L Player Camera Arbitration Proof")]
        public void RunFromContextMenu()
        {
            Begin();
        }

        private IEnumerator Run()
        {
            lastStatus = "Running";
            lastFailure = string.Empty;
            completedCaseCount = 0;
            completed.Clear();

            Debug.Log($"{LogPrefix} Smoke started.", this);
            yield return null;

            try
            {
                ValidateFixture();
                AssertInitialActivityWinner();
                Complete(
                    "route-player-activity-enter",
                    $"route='{routeBinding.LastStatus}' player='{playerBinding.LastStatus}' activity='{activityBinding.LastStatus}'");

                AssertInvalidPlayerBindingBlocked();
                Complete(
                    "invalid-player-binding-blocked",
                    $"status='{invalidPlayerBinding.LastStatus}' diagnostic='{invalidPlayerBinding.LastDiagnostic}'");

                activityRequestTrigger.ClearActivity();
            }
            catch (Exception exception)
            {
                Fail(exception);
                yield break;
            }

            yield return WaitForActivityRequest("initial-clear");
            if (IsFailed) yield break;

            try
            {
                AssertPlayerWinner();
                Complete(
                    "activity-exit-restores-player",
                    $"winner='{outputSession.Context.Winner.RequestId}'");

                bool preserved = playerBinding.SetLocalPlayerEligible(true);
                AssertTrue(preserved, "Repeated Player eligibility true should be preserved.");
                AssertEqual("Preserved", playerBinding.LastStatus,
                    "Repeated Player eligibility did not report Preserved.");
                Complete(
                    "player-publish-idempotent",
                    $"status='{playerBinding.LastStatus}'");

                AssertTrue(playerBinding.SetLocalPlayerEligible(false),
                    "Player eligibility false failed.");
                AssertRouteWinner();
                Complete(
                    "player-release-restores-route",
                    $"winner='{outputSession.Context.Winner.RequestId}'");

                bool releasePreserved = playerBinding.SetLocalPlayerEligible(false);
                AssertTrue(releasePreserved,
                    "Repeated Player eligibility false should be preserved.");
                AssertEqual("Preserved", playerBinding.LastStatus,
                    "Repeated Player release did not report Preserved.");
                Complete(
                    "player-release-idempotent",
                    $"status='{playerBinding.LastStatus}'");

                AssertTrue(playerBinding.SetLocalPlayerEligible(true),
                    "Player re-eligibility failed.");
                AssertPlayerWinner();
                Complete(
                    "player-reeligible-overrides-route",
                    $"winner='{outputSession.Context.Winner.RequestId}'");

                activityRequestTrigger.RequestActivity();
            }
            catch (Exception exception)
            {
                Fail(exception);
                yield break;
            }

            yield return WaitForActivityRequest("activity-override");
            if (IsFailed) yield break;

            try
            {
                AssertActivityWinner();
                Complete(
                    "activity-overrides-player",
                    $"winner='{outputSession.Context.Winner.RequestId}'");

                activityRequestTrigger.ClearActivity();
            }
            catch (Exception exception)
            {
                Fail(exception);
                yield break;
            }

            yield return WaitForActivityRequest("activity-release");
            if (IsFailed) yield break;

            try
            {
                AssertPlayerWinner();
                Complete(
                    "activity-release-restores-player",
                    $"winner='{outputSession.Context.Winner.RequestId}'");

                AssertTrue(playerBinding.SetLocalPlayerEligible(false),
                    "Final Player release failed.");
                AssertRouteWinner();
                Complete(
                    "final-player-release-restores-route",
                    $"winner='{outputSession.Context.Winner.RequestId}'");

                lastStatus = "Passed";
                completedCaseCount = completed.Count;

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.",
                    this);

                backToHubTrigger.RequestRoute();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private bool IsFailed =>
            string.Equals(lastStatus, "Failed", StringComparison.Ordinal);

        private IEnumerator WaitForActivityRequest(string operation)
        {
            int frames = 0;

            while (activityRequestTrigger.IsRequestInFlight && frames < 600)
            {
                frames++;
                yield return null;
            }

            yield return null;
            yield return null;

            if (activityRequestTrigger.IsRequestInFlight)
            {
                Fail(new InvalidOperationException(
                    $"Activity request '{operation}' did not complete."));
                yield break;
            }

            if (!activityRequestTrigger.LastRequestSucceeded)
            {
                Fail(new InvalidOperationException(
                    $"Activity request '{operation}' failed. message='{activityRequestTrigger.LastMessage}'."));
            }
        }

        private void ValidateFixture()
        {
            AssertNotNull(outputSession, "Output session binding is missing.");
            AssertNotNull(routeBinding, "Route binding is missing.");
            AssertNotNull(playerBinding, "Player binding is missing.");
            AssertNotNull(activityBinding, "Activity binding is missing.");
            AssertNotNull(invalidPlayerBinding, "Invalid Player binding is missing.");
            AssertNotNull(routeComposer, "Route composer is missing.");
            AssertNotNull(playerComposer, "Player composer rig is missing.");
            AssertNotNull(activityComposer, "Activity composer is missing.");
            AssertNotNull(activityRequestTrigger, "Activity request trigger is missing.");
            AssertNotNull(backToHubTrigger, "Back-to-Hub trigger is missing.");
            AssertTrue(outputSession.IsInitialized, "Output session did not initialize.");
        }

        private void AssertInitialActivityWinner()
        {
            AssertEqual("Published", routeBinding.LastStatus,
                "Route request was not published.");
            AssertTrue(routeBinding.IsPublished,
                "Route binding does not report published state.");

            AssertEqual("Published", playerBinding.LastStatus,
                "Player request was not published.");
            AssertTrue(playerBinding.IsPublished,
                "Player binding does not report published state.");
            AssertTrue(playerBinding.IsLocallyEligible,
                "Player binding does not report local eligibility.");

            AssertEqual("Published", activityBinding.LastStatus,
                "Activity request was not published.");
            AssertTrue(activityBinding.IsPublished,
                "Activity binding does not report published state.");

            AssertActivityWinner();
        }

        private void AssertInvalidPlayerBindingBlocked()
        {
            AssertEqual("Blocked", invalidPlayerBinding.LastStatus,
                "Invalid Player binding was not blocked.");
            AssertTrue(!invalidPlayerBinding.IsPublished,
                "Invalid Player binding published unexpectedly.");
            AssertTrue(
                invalidPlayerBinding.LastDiagnostic.Contains("eligibility scope"),
                "Invalid Player binding returned unexpected diagnostic.");
        }

        private void AssertActivityWinner()
        {
            AssertEqual(
                activityBinding.RequestIdText,
                outputSession.Context.Winner.RequestId.Value,
                "Activity request is not winner.");
            AssertTrue(activityComposer.CinemachineCamera.enabled,
                "Activity rig is not enabled.");
            AssertTrue(!playerComposer.CinemachineCamera.enabled,
                "Player rig remained enabled while Activity owns output.");
            AssertTrue(!routeComposer.CinemachineCamera.enabled,
                "Route rig remained enabled while Activity owns output.");
        }

        private void AssertPlayerWinner()
        {
            AssertTrue(playerBinding.IsPublished,
                "Player request is not published.");
            AssertTrue(playerBinding.IsLocallyEligible,
                "Player is not locally eligible.");

            AssertEqual(
                playerBinding.RequestIdText,
                outputSession.Context.Winner.RequestId.Value,
                "Player request is not winner.");
            AssertTrue(playerComposer.CinemachineCamera.enabled,
                "Player rig is not enabled.");
            AssertTrue(!activityComposer.CinemachineCamera.enabled,
                "Activity rig remained enabled after Activity release.");
            AssertTrue(!routeComposer.CinemachineCamera.enabled,
                "Route rig remained enabled while Player owns output.");
        }

        private void AssertRouteWinner()
        {
            AssertTrue(routeBinding.IsPublished,
                "Route request is not published.");
            AssertTrue(!playerBinding.IsPublished,
                "Player request remained published after eligibility release.");
            AssertTrue(!playerBinding.IsLocallyEligible,
                "Player remained locally eligible after release.");

            AssertEqual(
                routeBinding.RequestIdText,
                outputSession.Context.Winner.RequestId.Value,
                "Route request is not winner.");
            AssertTrue(routeComposer.CinemachineCamera.enabled,
                "Route rig is not enabled.");
            AssertTrue(!playerComposer.CinemachineCamera.enabled,
                "Player rig remained enabled after release.");
            AssertTrue(!activityComposer.CinemachineCamera.enabled,
                "Activity rig remained enabled unexpectedly.");
        }

        private void Complete(string step, string evidence)
        {
            completed.Add(step);
            completedCaseCount = completed.Count;

            Debug.Log(
                $"{LogPrefix} step='{step}' evidence='{Escape(evidence)}'.",
                this);
        }

        private void Fail(Exception exception)
        {
            lastStatus = "Failed";
            lastFailure = exception.Message ?? exception.GetType().Name;
            completedCaseCount = completed.Count;

            Debug.LogError(
                $"{LogPrefix} FAIL. status='Failed' exception='{exception.GetType().Name}' " +
                $"message='{Escape(lastFailure)}' completed='{string.Join(",", completed)}'.",
                this);

            if (throwOnFailure)
            {
                throw exception;
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(UnityEngine.Object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
        }
    }
}

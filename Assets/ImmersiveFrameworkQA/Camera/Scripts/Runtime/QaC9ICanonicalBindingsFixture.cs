using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using Unity.Cinemachine;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaC9ICanonicalBindingsFixture : MonoBehaviour
    {
        private const string LogPrefix = "[QA][C9I Canonical Camera Bindings]";

        [SerializeField] private CameraOutputSessionBinding outputSession;
        [SerializeField] private RouteCameraRequestBinding routeBinding;
        [SerializeField] private ActivityCameraRequestBinding activityBinding;
        [SerializeField] private RouteCameraRequestBinding foreignRouteBinding;
        [SerializeField] private ActivityCameraRequestBinding missingScopeActivityBinding;

        [SerializeField] private CameraRigComposer routeComposer;
        [SerializeField] private CameraRigComposer activityComposer;

        [SerializeField] private ActivityRequestTrigger activityRequestTrigger;
        [SerializeField] private RouteRequestTrigger backToHubTrigger;

        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        [Header("Debug")]
        [SerializeField] private string lastStatus = "NotRun";
        [SerializeField] private string lastFailure;
        [SerializeField] private int completedCaseCount;

        private readonly List<string> completed = new List<string>();

        public string LastStatus => lastStatus ?? string.Empty;
        public string LastFailure => lastFailure ?? string.Empty;
        public int CompletedCaseCount => completedCaseCount;

        private bool hasStarted;

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

        [ContextMenu("Run C9I Canonical Camera Bindings Proof")]
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
            QaC9IRouteExitEvidence.Reset();

            Debug.Log($"{LogPrefix} Smoke started.", this);

            yield return null;

            try
            {
                ValidateFixture();
                AssertInitialCanonicalEnter();
                completed.Add("canonical-route-activity-enter");
                LogStep(
                    "canonical-route-activity-enter",
                    $"routeStatus='{routeBinding.LastStatus}' activityStatus='{activityBinding.LastStatus}'");

                AssertInvalidBindingsBlocked();
                completed.Add("invalid-bindings-blocked");
                LogStep(
                    "invalid-bindings-blocked",
                    $"foreignRoute='{foreignRouteBinding.LastStatus}' missingScopeActivity='{missingScopeActivityBinding.LastStatus}'");

                AssertInvalidSessionBindingBlocked();
                completed.Add("invalid-session-binding-blocked");
                LogStep(
                    "invalid-session-binding-blocked",
                    "missing Camera/Brain rejected");

                activityRequestTrigger.ClearActivity();
            }
            catch (Exception exception)
            {
                Fail(exception);
                yield break;
            }

            yield return WaitForActivityRequest("clear");

            if (string.Equals(lastStatus, "Failed", StringComparison.Ordinal))
            {
                yield break;
            }

            try
            {
                AssertActivityClearedAndRouteRestored();
                completed.Add("canonical-activity-exit-restores-route");
                LogStep(
                    "canonical-activity-exit-restores-route",
                    $"activityStatus='{activityBinding.LastStatus}' routePublished='{routeBinding.IsPublished}'");

                activityRequestTrigger.RequestActivity();
            }
            catch (Exception exception)
            {
                Fail(exception);
                yield break;
            }

            yield return WaitForActivityRequest("restore");

            if (string.Equals(lastStatus, "Failed", StringComparison.Ordinal))
            {
                yield break;
            }

            try
            {
                AssertActivityRestored();
                completed.Add("canonical-activity-reenter-overrides-route");
                LogStep(
                    "canonical-activity-reenter-overrides-route",
                    $"activityStatus='{activityBinding.LastStatus}'");

                activityRequestTrigger.ClearActivity();
            }
            catch (Exception exception)
            {
                Fail(exception);
                yield break;
            }

            yield return WaitForActivityRequest("final-clear");

            if (string.Equals(lastStatus, "Failed", StringComparison.Ordinal))
            {
                yield break;
            }

            try
            {
                AssertActivityClearedAndRouteRestored();
                completed.Add("pre-route-exit-route-only");
                LogStep(
                    "pre-route-exit-route-only",
                    $"winner='{outputSession.Context.Winner.RequestId}'");

                bool cameraEnabledBefore = outputSession.UnityCamera.enabled;
                bool cameraActiveBefore = outputSession.UnityCamera.gameObject.activeSelf;
                Vector3 cameraPositionBefore = outputSession.UnityCamera.transform.position;

                AssertEqual(true, cameraEnabledBefore, "Unity Camera should remain enabled.");
                AssertEqual(true, cameraActiveBefore, "Unity Camera GameObject should remain active.");

                completed.Add("unity-output-unchanged");
                LogStep(
                    "unity-output-unchanged",
                    $"enabled='{cameraEnabledBefore}' active='{cameraActiveBefore}' position='{cameraPositionBefore}'");

                var completionObject =
                    new GameObject("QA_C9I_PersistentCompletion");

                var completion =
                    completionObject.AddComponent<QaC9IPersistentCompletion>();

                completion.Configure(
                    completed.ToArray(),
                    cameraEnabledBefore,
                    cameraActiveBefore,
                    cameraPositionBefore,
                    throwOnFailure);

                DontDestroyOnLoad(completionObject);

                lastStatus = "AwaitingRouteExit";
                completedCaseCount = completed.Count;

                backToHubTrigger.RequestRoute();
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

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
            AssertNotNull(activityBinding, "Activity binding is missing.");
            AssertNotNull(foreignRouteBinding, "Foreign Route binding is missing.");
            AssertNotNull(missingScopeActivityBinding, "Missing-scope Activity binding is missing.");
            AssertNotNull(routeComposer, "Route composer is missing.");
            AssertNotNull(activityComposer, "Activity composer is missing.");
            AssertNotNull(activityRequestTrigger, "Activity request trigger is missing.");
            AssertNotNull(backToHubTrigger, "Back-to-Hub trigger is missing.");

            AssertTrue(outputSession.IsInitialized, "Output session did not initialize.");
        }

        private void AssertInitialCanonicalEnter()
        {
            AssertEqual("Published", routeBinding.LastStatus,
                "Canonical Route enter did not publish Route request.");
            AssertTrue(routeBinding.IsPublished,
                "Route binding does not report published state.");

            AssertEqual("Published", activityBinding.LastStatus,
                "Canonical Activity enter did not publish Activity request.");
            AssertTrue(activityBinding.IsPublished,
                "Activity binding does not report published state.");

            AssertTrue(!routeComposer.CinemachineCamera.enabled,
                "Route rig should be overridden by startup Activity.");
            AssertTrue(activityComposer.CinemachineCamera.enabled,
                "Activity rig was not applied by canonical Activity enter.");

            AssertEqual(
                activityBinding.RequestIdText,
                outputSession.Context.Winner.RequestId.Value,
                "Startup Activity request is not the winner.");
        }

        private void AssertInvalidBindingsBlocked()
        {
            AssertEqual("Blocked", foreignRouteBinding.LastStatus,
                "Foreign Route asset binding was not blocked.");
            AssertTrue(!foreignRouteBinding.IsPublished,
                "Foreign Route binding published unexpectedly.");

            AssertEqual("Blocked", missingScopeActivityBinding.LastStatus,
                "Missing Activity scope was not blocked.");
            AssertTrue(!missingScopeActivityBinding.IsPublished,
                "Missing-scope Activity binding published unexpectedly.");
        }

        private static void AssertInvalidSessionBindingBlocked()
        {
            var invalidObject = new GameObject("QA_C9I_InvalidSessionBinding");
            invalidObject.SetActive(false);

            try
            {
                CameraOutputSessionBinding invalid =
                    invalidObject.AddComponent<CameraOutputSessionBinding>();

                bool initialized =
                    invalid.TryInitialize(out string diagnostic);

                AssertTrue(!initialized,
                    "Invalid session binding initialized unexpectedly.");

                AssertTrue(
                    diagnostic.Contains("Unity Camera"),
                    "Invalid session binding returned unexpected diagnostic.");
            }
            finally
            {
                Destroy(invalidObject);
            }
        }

        private void AssertActivityClearedAndRouteRestored()
        {
            AssertEqual("Released", activityBinding.LastStatus,
                "Canonical Activity exit did not release Activity request.");
            AssertTrue(!activityBinding.IsPublished,
                "Activity binding remained published after clear.");

            AssertTrue(routeBinding.IsPublished,
                "Route request was lost after Activity clear.");
            AssertTrue(routeComposer.CinemachineCamera.enabled,
                "Route rig was not restored after Activity exit.");
            AssertTrue(!activityComposer.CinemachineCamera.enabled,
                "Activity rig remained enabled after Activity exit.");

            AssertEqual(
                routeBinding.RequestIdText,
                outputSession.Context.Winner.RequestId.Value,
                "Route request is not winner after Activity exit.");
        }

        private void AssertActivityRestored()
        {
            AssertEqual("Published", activityBinding.LastStatus,
                "Canonical Activity re-enter did not publish Activity request.");
            AssertTrue(activityBinding.IsPublished,
                "Activity binding is not published after restore.");

            AssertTrue(!routeComposer.CinemachineCamera.enabled,
                "Route rig was not overridden after Activity restore.");
            AssertTrue(activityComposer.CinemachineCamera.enabled,
                "Activity rig was not restored.");

            AssertEqual(
                activityBinding.RequestIdText,
                outputSession.Context.Winner.RequestId.Value,
                "Activity request is not winner after restore.");
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

        private void LogStep(string step, string evidence)
        {
            Debug.Log(
                $"{LogPrefix} step='{step}' evidence='{Escape(evidence)}'.",
                this);
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

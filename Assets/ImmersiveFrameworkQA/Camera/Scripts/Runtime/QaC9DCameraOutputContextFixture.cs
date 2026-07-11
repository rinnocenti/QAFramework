using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/C9D Camera Output Context Fixture")]
    public sealed class QaC9DCameraOutputContextFixture : MonoBehaviour
    {
        private const string LogPrefix = "[QA][C9D Camera Output Context]";

        [Header("Synthetic Inputs")]
        [SerializeField] private Transform explicitTargetSource;
        [SerializeField] private UnityEngine.Camera observedCamera;

        [Header("Execution")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        [Header("Debug")]
        [SerializeField] private string lastStatus = "NotRun";
        [SerializeField] private string lastFailure;
        [SerializeField] private int completedCaseCount;

        private CameraRigRecipe runtimeRecipe;

        public string LastStatus => lastStatus ?? string.Empty;
        public string LastFailure => lastFailure ?? string.Empty;
        public int CompletedCaseCount => completedCaseCount;

        private void Start()
        {
            if (runOnStart)
            {
                Run();
            }
        }

        private void OnDestroy()
        {
            ReleaseRuntimeRecipe();
        }

        [ContextMenu("Run C9D Camera Output Context Proof")]
        public void Run()
        {
            ReleaseRuntimeRecipe();

            runtimeRecipe = ScriptableObject.CreateInstance<CameraRigRecipe>();
            runtimeRecipe.name = "QA_C9D_RuntimeCameraRigRecipe";
            runtimeRecipe.hideFlags = HideFlags.DontSave;

            var completed = new List<string>();
            lastStatus = "Running";
            lastFailure = string.Empty;
            completedCaseCount = 0;

            bool cameraEnabledBefore = observedCamera != null && observedCamera.enabled;
            bool cameraActiveBefore = observedCamera != null && observedCamera.gameObject.activeSelf;
            Vector3 cameraPositionBefore = observedCamera != null
                ? observedCamera.transform.position
                : Vector3.zero;

            Debug.Log($"{LogPrefix} Smoke started.", this);

            try
            {
                ValidateFixture();
                RunWinnerLifecycle(completed);
                RunLowerPrecedencePreservesWinner(completed);
                RunDeterministicTieBreaker(completed);
                RunMissingTieBreakerBlocked(completed);
                RunDuplicateTieBreakerBlocked(completed);
                RunDuplicateRequestIdBlocked(completed);
                RunForeignOutputBlocked(completed);
                RunUnknownReleaseExplicit(completed);
                RunSnapshotOrdering(completed);
                AssertCameraStateUnchanged(
                    cameraEnabledBefore,
                    cameraActiveBefore,
                    cameraPositionBefore,
                    completed);

                completedCaseCount = completed.Count;
                lastStatus = "Passed";

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.",
                    this);
            }
            catch (Exception exception)
            {
                completedCaseCount = completed.Count;
                lastStatus = "Failed";
                lastFailure = exception.Message ?? exception.GetType().Name;

                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(lastFailure)}' completed='{string.Join(",", completed)}'.",
                    this);

                if (throwOnFailure)
                {
                    throw;
                }
            }
        }

        private void ValidateFixture()
        {
            AssertNotNull(explicitTargetSource, "Explicit target source is missing.");
            AssertNotNull(observedCamera, "Observed Camera is missing.");
            AssertNotNull(runtimeRecipe, "Runtime CameraRigRecipe was not created.");
        }

        private void RunWinnerLifecycle(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraRequest route = CreateRequest(
                "route",
                CameraOutputId.Main,
                10,
                "route");

            CameraRequest player = CreateRequest(
                "player",
                CameraOutputId.Main,
                100,
                "player");

            CameraRequest activity = CreateRequest(
                "activity",
                CameraOutputId.Main,
                200,
                "activity");

            CameraOutputContextResult routeAdmit = context.Admit(route);
            AssertTrue(routeAdmit.Succeeded, "Route request admission failed.");
            AssertEqual(
                CameraOutputContextChangeKind.WinnerEstablished,
                routeAdmit.ChangeKind,
                "Route admission did not establish the first winner.");
            AssertWinner(context, route.RequestId, "Route should be the initial winner.");

            CameraOutputContextResult playerAdmit = context.Admit(player);
            AssertEqual(
                CameraOutputContextChangeKind.WinnerChanged,
                playerAdmit.ChangeKind,
                "Player admission did not replace Route.");
            AssertWinner(context, player.RequestId, "Player should override Route.");

            CameraOutputContextResult activityAdmit = context.Admit(activity);
            AssertEqual(
                CameraOutputContextChangeKind.WinnerChanged,
                activityAdmit.ChangeKind,
                "Activity admission did not replace Player.");
            AssertWinner(context, activity.RequestId, "Activity should override Player.");

            CameraOutputContextResult activityRelease =
                context.Release(activity.RequestId);
            AssertEqual(
                CameraOutputContextChangeKind.WinnerChanged,
                activityRelease.ChangeKind,
                "Activity release did not restore Player.");
            AssertWinner(context, player.RequestId, "Player should be restored.");

            CameraOutputContextResult playerRelease =
                context.Release(player.RequestId);
            AssertWinner(context, route.RequestId, "Route should be restored.");

            CameraOutputContextResult routeRelease =
                context.Release(route.RequestId);
            AssertEqual(
                CameraOutputContextChangeKind.WinnerCleared,
                routeRelease.ChangeKind,
                "Final release did not clear the winner.");
            AssertTrue(!context.HasWinner, "Context should have no winner after final release.");
            AssertEqual(0, context.AdmittedRequestCount, "Context should be empty.");

            LogStep(
                "winner-lifecycle",
                $"route='{route.RequestId}' player='{player.RequestId}' activity='{activity.RequestId}'");
            completed.Add("winner-lifecycle");
        }

        private void RunLowerPrecedencePreservesWinner(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraRequest winner = CreateRequest(
                "winner-high",
                CameraOutputId.Main,
                100,
                "winner-high");

            CameraRequest lower = CreateRequest(
                "lower",
                CameraOutputId.Main,
                1,
                "lower");

            AssertTrue(context.Admit(winner).Succeeded, "High request admission failed.");

            CameraOutputContextResult lowerAdmit = context.Admit(lower);
            AssertEqual(
                CameraOutputContextChangeKind.WinnerPreserved,
                lowerAdmit.ChangeKind,
                "Lower precedence request should preserve the winner.");
            AssertWinner(context, winner.RequestId, "High request should remain winner.");

            LogStep("lower-precedence-preserved", $"winner='{winner.RequestId}'");
            completed.Add("lower-precedence-preserved");
        }

        private void RunDeterministicTieBreaker(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraRequest beta = CreateRequest(
                "tie-beta",
                CameraOutputId.Main,
                50,
                "beta");

            CameraRequest alpha = CreateRequest(
                "tie-alpha",
                CameraOutputId.Main,
                50,
                "alpha");

            AssertTrue(context.Admit(beta).Succeeded, "Beta admission failed.");
            CameraOutputContextResult alphaAdmit = context.Admit(alpha);

            AssertTrue(alphaAdmit.Succeeded, "Alpha admission failed.");
            AssertEqual(
                CameraOutputContextChangeKind.WinnerChanged,
                alphaAdmit.ChangeKind,
                "Ordinal tie-breaker did not change the winner.");
            AssertWinner(context, alpha.RequestId, "Alpha tie-breaker should win.");

            LogStep("deterministic-tie-breaker", $"winner='{alpha.RequestId}'");
            completed.Add("deterministic-tie-breaker");
        }

        private void RunMissingTieBreakerBlocked(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraRequest first = CreateRequest(
                "missing-tie-first",
                CameraOutputId.Main,
                30,
                string.Empty);

            CameraRequest second = CreateRequest(
                "missing-tie-second",
                CameraOutputId.Main,
                30,
                "second");

            AssertTrue(context.Admit(first).Succeeded, "First no-tie request admission failed.");

            CameraOutputContextResult result = context.Admit(second);
            AssertBlocked(
                result,
                "camera.output-context.tie-breaker.missing",
                "Missing tie-breaker ambiguity was not blocked.");

            LogStep("missing-tie-breaker-blocked", result.DiagnosticSummary);
            completed.Add("missing-tie-breaker-blocked");
        }

        private void RunDuplicateTieBreakerBlocked(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraRequest first = CreateRequest(
                "duplicate-tie-first",
                CameraOutputId.Main,
                40,
                "shared");

            CameraRequest second = CreateRequest(
                "duplicate-tie-second",
                CameraOutputId.Main,
                40,
                "shared");

            AssertTrue(context.Admit(first).Succeeded, "First shared-tie request admission failed.");

            CameraOutputContextResult result = context.Admit(second);
            AssertBlocked(
                result,
                "camera.output-context.tie-breaker.duplicate",
                "Duplicate tie-breaker ambiguity was not blocked.");

            LogStep("duplicate-tie-breaker-blocked", result.DiagnosticSummary);
            completed.Add("duplicate-tie-breaker-blocked");
        }

        private void RunDuplicateRequestIdBlocked(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraRequest request = CreateRequest(
                "duplicate-id",
                CameraOutputId.Main,
                10,
                "duplicate-id");

            AssertTrue(context.Admit(request).Succeeded, "Initial duplicate-id admission failed.");

            CameraOutputContextResult result = context.Admit(request);
            AssertBlocked(
                result,
                "camera.output-context.request-duplicate",
                "Duplicate request id was not blocked.");

            LogStep("duplicate-request-id-blocked", result.DiagnosticSummary);
            completed.Add("duplicate-request-id-blocked");
        }

        private void RunForeignOutputBlocked(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraRequest foreign = CreateRequest(
                "foreign-output",
                new CameraOutputId("camera.output.foreign"),
                100,
                "foreign");

            CameraOutputContextResult result = context.Admit(foreign);
            AssertBlocked(
                result,
                "camera.output-context.output-mismatch",
                "Foreign output request was not blocked.");

            AssertEqual(0, context.AdmittedRequestCount, "Foreign request was admitted.");

            LogStep("foreign-output-blocked", result.DiagnosticSummary);
            completed.Add("foreign-output-blocked");
        }

        private void RunUnknownReleaseExplicit(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraOutputContextResult result =
                context.Release(new CameraRequestId("qa.camera.request.c9d.unknown"));

            AssertTrue(result.IsNotFound, "Unknown release should return NotFound.");
            AssertTrue(result.Issues != null && result.Issues.Length == 1,
                "Unknown release should emit one warning.");
            AssertEqual(
                "camera.output-context.release-not-found",
                result.Issues[0].Code,
                "Unknown release emitted the wrong issue code.");

            LogStep("unknown-release-explicit", result.DiagnosticSummary);
            completed.Add("unknown-release-explicit");
        }

        private void RunSnapshotOrdering(List<string> completed)
        {
            var context = new CameraOutputContext(CameraOutputId.Main);

            CameraRequest zeta = CreateRequest(
                "snapshot-zeta",
                CameraOutputId.Main,
                1,
                "zeta");

            CameraRequest alpha = CreateRequest(
                "snapshot-alpha",
                CameraOutputId.Main,
                2,
                "alpha");

            CameraRequest middle = CreateRequest(
                "snapshot-middle",
                CameraOutputId.Main,
                3,
                "middle");

            AssertTrue(context.Admit(zeta).Succeeded, "Snapshot zeta admission failed.");
            AssertTrue(context.Admit(alpha).Succeeded, "Snapshot alpha admission failed.");
            AssertTrue(context.Admit(middle).Succeeded, "Snapshot middle admission failed.");

            CameraOutputContextSnapshot snapshot = context.CaptureSnapshot();

            AssertEqual(3, snapshot.AdmittedRequestCount, "Snapshot count is incorrect.");
            AssertTrue(snapshot.HasWinner, "Snapshot should expose a winner.");
            AssertEqual(3, snapshot.AdmittedRequestIds.Length, "Snapshot id count is incorrect.");

            string first = snapshot.AdmittedRequestIds[0].Value;
            string second = snapshot.AdmittedRequestIds[1].Value;
            string third = snapshot.AdmittedRequestIds[2].Value;

            AssertTrue(
                string.Compare(first, second, StringComparison.Ordinal) < 0 &&
                string.Compare(second, third, StringComparison.Ordinal) < 0,
                "Snapshot request ids are not ordinally sorted.");

            LogStep(
                "snapshot-ordering",
                $"ids='{first},{second},{third}' winner='{snapshot.Winner.RequestId}'");
            completed.Add("snapshot-ordering");
        }

        private void AssertCameraStateUnchanged(
            bool enabledBefore,
            bool activeBefore,
            Vector3 positionBefore,
            List<string> completed)
        {
            AssertNotNull(observedCamera, "Observed camera disappeared.");
            AssertEqual(enabledBefore, observedCamera.enabled, "Context changed Camera.enabled.");
            AssertEqual(activeBefore, observedCamera.gameObject.activeSelf, "Context changed camera active state.");
            AssertEqual(positionBefore, observedCamera.transform.position, "Context changed camera transform.");

            LogStep(
                "camera-state-unchanged",
                $"enabled='{observedCamera.enabled}' active='{observedCamera.gameObject.activeSelf}' " +
                $"position='{observedCamera.transform.position}'");
            completed.Add("camera-state-unchanged");
        }

        private CameraRequest CreateRequest(
            string suffix,
            CameraOutputId outputId,
            int precedence,
            string tieBreaker)
        {
            CameraRequestCreateResult result =
                CameraRequestCreateResult.Create(
                    new CameraRequestId($"qa.camera.request.c9d.{suffix}"),
                    outputId,
                    new CameraRequestOwner(
                        CameraRequestOwnerKind.Debug,
                        $"qa.owner.c9d.{suffix}"),
                    new CameraRequestLifetime(
                        CameraRequestLifetimeKind.ExplicitOperation,
                        $"qa.lifetime.c9d.{suffix}"),
                    CameraRigReference.FromRecipe(runtimeRecipe),
                    CameraTargetSourceDescriptor.ExplicitTransform(
                        explicitTargetSource,
                        $"QA C9D Target {suffix}"),
                    new CameraRequestPolicy(precedence, tieBreaker),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(QaC9DCameraOutputContextFixture),
                    $"QA C9D synthetic request '{suffix}'.");

            if (!result.IsSucceeded)
            {
                throw new InvalidOperationException(
                    $"Could not create request '{suffix}': {result.BlockingIssue}");
            }

            return result.Request;
        }

        private static void AssertWinner(
            CameraOutputContext context,
            CameraRequestId expected,
            string message)
        {
            AssertTrue(context.HasWinner, message + " Context has no winner.");
            AssertEqual(expected, context.Winner.RequestId, message);
        }

        private static void AssertBlocked(
            CameraOutputContextResult result,
            string expectedCode,
            string message)
        {
            AssertTrue(result.IsBlocked, message);
            AssertTrue(result.Issues != null && result.Issues.Length == 1,
                message + " Expected one issue.");
            AssertEqual(expectedCode, result.Issues[0].Code,
                message + " Wrong issue code.");
            AssertTrue(result.Issues[0].IsBlocking,
                message + " Issue is not blocking.");
        }

        private void LogStep(string step, string evidence)
        {
            Debug.Log(
                $"{LogPrefix} step='{step}' evidence='{Escape(evidence)}'.",
                this);
        }

        private void ReleaseRuntimeRecipe()
        {
            if (runtimeRecipe == null)
            {
                return;
            }

            Destroy(runtimeRecipe);
            runtimeRecipe = null;
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

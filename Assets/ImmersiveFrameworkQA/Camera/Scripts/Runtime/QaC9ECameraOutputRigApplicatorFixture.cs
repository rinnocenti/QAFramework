using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/C9E Camera Output Rig Applicator Fixture")]
    public sealed class QaC9ECameraOutputRigApplicatorFixture : MonoBehaviour
    {
        private const string LogPrefix = "[QA][C9E Camera Output Rig Applicator]";

        [Header("Output")]
        [SerializeField] private UnityEngine.Camera outputCamera;
        [SerializeField] private CinemachineBrain outputBrain;

        [Header("Materialized Rigs")]
        [SerializeField] private CameraRigComposer routeComposer;
        [SerializeField] private CameraRigComposer playerComposer;
        [SerializeField] private CameraRigComposer invalidComposer;

        [Header("Target")]
        [SerializeField] private Transform explicitTargetSource;

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

        [ContextMenu("Run C9E Camera Output Rig Applicator Proof")]
        public void Run()
        {
            ReleaseRuntimeRecipe();

            runtimeRecipe = ScriptableObject.CreateInstance<CameraRigRecipe>();
            runtimeRecipe.name = "QA_C9E_RuntimeCameraRigRecipe";
            runtimeRecipe.hideFlags = HideFlags.DontSave;

            var completed = new List<string>();
            lastStatus = "Running";
            lastFailure = string.Empty;
            completedCaseCount = 0;

            bool outputEnabledBefore = outputCamera != null && outputCamera.enabled;
            bool outputActiveBefore = outputCamera != null && outputCamera.gameObject.activeSelf;
            Vector3 outputPositionBefore = outputCamera != null
                ? outputCamera.transform.position
                : Vector3.zero;

            Debug.Log($"{LogPrefix} Smoke started.", this);

            try
            {
                ValidateFixture();
                RunWinnerApplicationLifecycle(completed);
                RunPreservedWinner(completed);
                RunRecipeOnlyBlocked(completed);
                RunMissingCinemachineCameraBlocked(completed);
                RunForeignContextBlocked(completed);
                RunClearAlreadyClear(completed);
                AssertUnityOutputUnchanged(
                    outputEnabledBefore,
                    outputActiveBefore,
                    outputPositionBefore,
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
            AssertNotNull(outputCamera, "Output Camera is missing.");
            AssertNotNull(outputBrain, "Output CinemachineBrain is missing.");
            AssertTrue(
                outputBrain.gameObject == outputCamera.gameObject,
                "Output Camera and CinemachineBrain must share one GameObject.");

            AssertValidComposer(routeComposer, "Route");
            AssertValidComposer(playerComposer, "Player");

            AssertNotNull(invalidComposer, "Invalid composer fixture is missing.");
            AssertTrue(
                invalidComposer.CinemachineCamera == null,
                "Invalid composer must not contain a CinemachineCamera.");

            AssertNotNull(explicitTargetSource, "Explicit target source is missing.");
            AssertNotNull(runtimeRecipe, "Runtime CameraRigRecipe was not created.");
        }

        private void RunWinnerApplicationLifecycle(List<string> completed)
        {
            ResetRigState();

            var context = new CameraOutputContext(CameraOutputId.Main);
            var applicator = CreateApplicator(CameraOutputId.Main);

            CameraRequest route = CreateComposerRequest(
                "route",
                routeComposer,
                10,
                "route");

            CameraRequest player = CreateComposerRequest(
                "player",
                playerComposer,
                100,
                "player");

            AssertTrue(context.Admit(route).Succeeded, "Route admission failed.");

            CameraOutputApplyResult routeApply = applicator.Apply(context);
            AssertApply(
                routeApply,
                CameraOutputApplyKind.Applied,
                routeComposer.CinemachineCamera,
                "Route rig was not applied.");
            AssertTrue(routeComposer.CinemachineCamera.enabled, "Route camera should be enabled.");
            AssertTrue(!playerComposer.CinemachineCamera.enabled, "Player camera should remain disabled.");

            AssertTrue(context.Admit(player).Succeeded, "Player admission failed.");

            CameraOutputApplyResult playerApply = applicator.Apply(context);
            AssertApply(
                playerApply,
                CameraOutputApplyKind.Applied,
                playerComposer.CinemachineCamera,
                "Player rig was not applied.");
            AssertTrue(!routeComposer.CinemachineCamera.enabled, "Route camera should be disabled after override.");
            AssertTrue(playerComposer.CinemachineCamera.enabled, "Player camera should be enabled.");

            CameraOutputContextResult playerRelease =
                context.Release(player.RequestId);
            AssertTrue(playerRelease.Succeeded, "Player release failed.");

            CameraOutputApplyResult routeRestore = applicator.Apply(context);
            AssertApply(
                routeRestore,
                CameraOutputApplyKind.Applied,
                routeComposer.CinemachineCamera,
                "Route rig was not restored.");
            AssertTrue(routeComposer.CinemachineCamera.enabled, "Route camera should be restored.");
            AssertTrue(!playerComposer.CinemachineCamera.enabled, "Player camera should be disabled after release.");

            CameraOutputContextResult routeRelease =
                context.Release(route.RequestId);
            AssertTrue(routeRelease.Succeeded, "Route release failed.");

            CameraOutputApplyResult clear = applicator.Apply(context);
            AssertEqual(
                CameraOutputApplyKind.Cleared,
                clear.Kind,
                "Empty context did not clear the output.");
            AssertTrue(!routeComposer.CinemachineCamera.enabled, "Route camera should be disabled after clear.");
            AssertTrue(!playerComposer.CinemachineCamera.enabled, "Player camera should be disabled after clear.");
            AssertTrue(!applicator.HasAppliedRequest, "Applicator should have no applied request after clear.");

            LogStep(
                "winner-application-lifecycle",
                $"route='{route.RequestId}' player='{player.RequestId}'");
            completed.Add("winner-application-lifecycle");
        }

        private void RunPreservedWinner(List<string> completed)
        {
            ResetRigState();

            var context = new CameraOutputContext(CameraOutputId.Main);
            var applicator = CreateApplicator(CameraOutputId.Main);

            CameraRequest route = CreateComposerRequest(
                "preserved",
                routeComposer,
                10,
                "preserved");

            AssertTrue(context.Admit(route).Succeeded, "Preserved request admission failed.");
            AssertEqual(
                CameraOutputApplyKind.Applied,
                applicator.Apply(context).Kind,
                "Initial preserved request application failed.");

            CameraOutputApplyResult second = applicator.Apply(context);

            AssertEqual(
                CameraOutputApplyKind.Preserved,
                second.Kind,
                "Unchanged winner was not preserved.");
            AssertTrue(routeComposer.CinemachineCamera.enabled, "Preserved camera should remain enabled.");

            LogStep("winner-preserved", second.DiagnosticSummary);
            completed.Add("winner-preserved");
        }

        private void RunRecipeOnlyBlocked(List<string> completed)
        {
            ResetRigState();

            var context = new CameraOutputContext(CameraOutputId.Main);
            var applicator = CreateApplicator(CameraOutputId.Main);

            CameraRequest recipeOnly = CreateRecipeRequest(
                "recipe-only",
                CameraOutputId.Main,
                50,
                "recipe-only");

            AssertTrue(context.Admit(recipeOnly).Succeeded, "Recipe-only request admission failed.");

            CameraOutputApplyResult result = applicator.Apply(context);

            AssertBlocked(
                result,
                "camera.output-apply.composer.missing",
                "Recipe-only winner was not blocked.");
            AssertTrue(!applicator.HasAppliedRequest, "Blocked recipe-only request should not be applied.");

            LogStep("recipe-only-blocked", result.DiagnosticSummary);
            completed.Add("recipe-only-blocked");
        }

        private void RunMissingCinemachineCameraBlocked(List<string> completed)
        {
            ResetRigState();

            var context = new CameraOutputContext(CameraOutputId.Main);
            var applicator = CreateApplicator(CameraOutputId.Main);

            CameraRequest invalid = CreateComposerRequest(
                "missing-cinemachine-camera",
                invalidComposer,
                50,
                "missing-cinemachine-camera");

            AssertTrue(context.Admit(invalid).Succeeded, "Invalid composer request admission failed.");

            CameraOutputApplyResult result = applicator.Apply(context);

            AssertBlocked(
                result,
                "camera.output-apply.cinemachine-camera.missing",
                "Composer without CinemachineCamera was not blocked.");
            AssertTrue(!applicator.HasAppliedRequest, "Invalid composer should not be applied.");

            LogStep("missing-cinemachine-camera-blocked", result.DiagnosticSummary);
            completed.Add("missing-cinemachine-camera-blocked");
        }

        private void RunForeignContextBlocked(List<string> completed)
        {
            ResetRigState();

            var context = new CameraOutputContext(
                new CameraOutputId("camera.output.foreign"));

            var applicator = CreateApplicator(CameraOutputId.Main);

            CameraOutputApplyResult result = applicator.Apply(context);

            AssertBlocked(
                result,
                "camera.output-apply.output-mismatch",
                "Foreign context was not blocked.");

            LogStep("foreign-context-blocked", result.DiagnosticSummary);
            completed.Add("foreign-context-blocked");
        }

        private void RunClearAlreadyClear(List<string> completed)
        {
            ResetRigState();

            var applicator = CreateApplicator(CameraOutputId.Main);
            CameraOutputApplyResult result = applicator.Clear();

            AssertEqual(
                CameraOutputApplyKind.Cleared,
                result.Kind,
                "Clear on an empty applicator should succeed.");
            AssertTrue(!applicator.HasAppliedRequest, "Empty clear created applied state.");

            LogStep("clear-already-clear", result.DiagnosticSummary);
            completed.Add("clear-already-clear");
        }

        private void AssertUnityOutputUnchanged(
            bool enabledBefore,
            bool activeBefore,
            Vector3 positionBefore,
            List<string> completed)
        {
            AssertNotNull(outputCamera, "Output Camera disappeared.");

            AssertEqual(
                enabledBefore,
                outputCamera.enabled,
                "Applicator changed Unity Camera.enabled.");

            AssertEqual(
                activeBefore,
                outputCamera.gameObject.activeSelf,
                "Applicator changed output GameObject active state.");

            AssertEqual(
                positionBefore,
                outputCamera.transform.position,
                "Applicator changed output Camera transform.");

            LogStep(
                "unity-output-unchanged",
                $"enabled='{outputCamera.enabled}' active='{outputCamera.gameObject.activeSelf}' " +
                $"position='{outputCamera.transform.position}'");
            completed.Add("unity-output-unchanged");
        }

        private CameraOutputRigApplicator CreateApplicator(CameraOutputId outputId)
        {
            return new CameraOutputRigApplicator(
                new CameraOutputBinding(
                    outputId,
                    outputCamera,
                    outputBrain));
        }

        private CameraRequest CreateComposerRequest(
            string suffix,
            CameraRigComposer composer,
            int precedence,
            string tieBreaker)
        {
            return CreateRequest(
                suffix,
                CameraOutputId.Main,
                CameraRigReference.FromComposer(composer),
                precedence,
                tieBreaker);
        }

        private CameraRequest CreateRecipeRequest(
            string suffix,
            CameraOutputId outputId,
            int precedence,
            string tieBreaker)
        {
            return CreateRequest(
                suffix,
                outputId,
                CameraRigReference.FromRecipe(runtimeRecipe),
                precedence,
                tieBreaker);
        }

        private CameraRequest CreateRequest(
            string suffix,
            CameraOutputId outputId,
            CameraRigReference rig,
            int precedence,
            string tieBreaker)
        {
            CameraRequestCreateResult result =
                CameraRequestCreateResult.Create(
                    new CameraRequestId($"qa.camera.request.c9e.{suffix}"),
                    outputId,
                    new CameraRequestOwner(
                        CameraRequestOwnerKind.Debug,
                        $"qa.owner.c9e.{suffix}"),
                    new CameraRequestLifetime(
                        CameraRequestLifetimeKind.ExplicitOperation,
                        $"qa.lifetime.c9e.{suffix}"),
                    rig,
                    CameraTargetSourceDescriptor.ExplicitTransform(
                        explicitTargetSource,
                        $"QA C9E Target {suffix}"),
                    new CameraRequestPolicy(precedence, tieBreaker),
                    CameraRequestReleaseCondition.ExplicitRelease,
                    nameof(QaC9ECameraOutputRigApplicatorFixture),
                    $"QA C9E synthetic request '{suffix}'.");

            if (!result.IsSucceeded)
            {
                throw new InvalidOperationException(
                    $"Could not create request '{suffix}': {result.BlockingIssue}");
            }

            return result.Request;
        }

        private void ResetRigState()
        {
            routeComposer.CinemachineCamera.enabled = false;
            playerComposer.CinemachineCamera.enabled = false;
        }

        private static void AssertValidComposer(
            CameraRigComposer composer,
            string label)
        {
            AssertNotNull(composer, $"{label} composer is missing.");
            AssertNotNull(
                composer.CinemachineCamera,
                $"{label} composer CinemachineCamera is missing.");
        }

        private static void AssertApply(
            CameraOutputApplyResult result,
            CameraOutputApplyKind expectedKind,
            CinemachineCamera expectedCamera,
            string message)
        {
            AssertTrue(result.Succeeded, message);
            AssertEqual(expectedKind, result.Kind, message);
            AssertEqual(expectedCamera, result.CurrentCamera, message);
        }

        private static void AssertBlocked(
            CameraOutputApplyResult result,
            string expectedCode,
            string message)
        {
            AssertTrue(result.IsBlocked, message);
            AssertTrue(
                result.Issues != null && result.Issues.Length == 1,
                message + " Expected one issue.");
            AssertEqual(
                expectedCode,
                result.Issues[0].Code,
                message + " Wrong issue code.");
            AssertTrue(
                result.Issues[0].IsBlocking,
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

using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/C9C Camera Request Contracts Fixture")]
    public sealed class QaC9CCameraRequestContractsFixture : MonoBehaviour
    {
        private const string LogPrefix = "[QA][C9C Camera Request Contracts]";

        [Header("Synthetic Contract Inputs")]
        [SerializeField] private CameraRigComposer rigComposer;
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

        [ContextMenu("Run C9C Camera Request Contract Proof")]
        public void Run()
        {
            ReleaseRuntimeRecipe();

            runtimeRecipe = ScriptableObject.CreateInstance<CameraRigRecipe>();
            runtimeRecipe.name = "QA_C9C_RuntimeCameraRigRecipe";
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
                RunValidComposerRequest(completed);
                RunValidRecipeRequest(completed);

                RunBlockedCase(
                    "missing-request-id",
                    "camera.request.id.missing",
                    CameraRequestCreateResult.Create(
                        default,
                        CameraOutputId.Main,
                        ValidOwner(),
                        ValidLifetime(),
                        ValidComposerRig(),
                        ValidTargetSource(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.OwnerLifetimeEnded,
                        nameof(QaC9CCameraRequestContractsFixture),
                        "QA request with missing request id."),
                    completed);

                RunBlockedCase(
                    "missing-output-id",
                    "camera.request.output.missing",
                    CameraRequestCreateResult.Create(
                        ValidRequestId("missing-output"),
                        default,
                        ValidOwner(),
                        ValidLifetime(),
                        ValidComposerRig(),
                        ValidTargetSource(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.OwnerLifetimeEnded,
                        nameof(QaC9CCameraRequestContractsFixture),
                        "QA request with missing output id."),
                    completed);

                RunBlockedCase(
                    "invalid-owner",
                    "camera.request.owner.invalid",
                    CameraRequestCreateResult.Create(
                        ValidRequestId("invalid-owner"),
                        CameraOutputId.Main,
                        default,
                        ValidLifetime(),
                        ValidComposerRig(),
                        ValidTargetSource(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.OwnerLifetimeEnded,
                        nameof(QaC9CCameraRequestContractsFixture),
                        "QA request with invalid owner."),
                    completed);

                RunBlockedCase(
                    "invalid-lifetime",
                    "camera.request.lifetime.invalid",
                    CameraRequestCreateResult.Create(
                        ValidRequestId("invalid-lifetime"),
                        CameraOutputId.Main,
                        ValidOwner(),
                        default,
                        ValidComposerRig(),
                        ValidTargetSource(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.OwnerLifetimeEnded,
                        nameof(QaC9CCameraRequestContractsFixture),
                        "QA request with invalid lifetime."),
                    completed);

                RunBlockedCase(
                    "missing-rig",
                    "camera.request.rig.missing",
                    CameraRequestCreateResult.Create(
                        ValidRequestId("missing-rig"),
                        CameraOutputId.Main,
                        ValidOwner(),
                        ValidLifetime(),
                        default,
                        ValidTargetSource(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.OwnerLifetimeEnded,
                        nameof(QaC9CCameraRequestContractsFixture),
                        "QA request with missing rig."),
                    completed);

                RunBlockedCase(
                    "missing-target-source",
                    "camera.request.target-source.missing",
                    CameraRequestCreateResult.Create(
                        ValidRequestId("missing-target-source"),
                        CameraOutputId.Main,
                        ValidOwner(),
                        ValidLifetime(),
                        ValidComposerRig(),
                        CameraTargetSourceDescriptor.None(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.OwnerLifetimeEnded,
                        nameof(QaC9CCameraRequestContractsFixture),
                        "QA request with missing target source."),
                    completed);

                RunBlockedCase(
                    "missing-release-condition",
                    "camera.request.release-condition.missing",
                    CameraRequestCreateResult.Create(
                        ValidRequestId("missing-release-condition"),
                        CameraOutputId.Main,
                        ValidOwner(),
                        ValidLifetime(),
                        ValidComposerRig(),
                        ValidTargetSource(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.Undefined,
                        nameof(QaC9CCameraRequestContractsFixture),
                        "QA request with missing release condition."),
                    completed);

                RunBlockedCase(
                    "missing-diagnostic-source",
                    "camera.request.diagnostic-source.missing",
                    CameraRequestCreateResult.Create(
                        ValidRequestId("missing-diagnostic-source"),
                        CameraOutputId.Main,
                        ValidOwner(),
                        ValidLifetime(),
                        ValidComposerRig(),
                        ValidTargetSource(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.OwnerLifetimeEnded,
                        string.Empty,
                        "QA request with missing diagnostic source."),
                    completed);

                RunBlockedCase(
                    "missing-diagnostic-reason",
                    "camera.request.diagnostic-reason.missing",
                    CameraRequestCreateResult.Create(
                        ValidRequestId("missing-diagnostic-reason"),
                        CameraOutputId.Main,
                        ValidOwner(),
                        ValidLifetime(),
                        ValidComposerRig(),
                        ValidTargetSource(),
                        ValidPolicy(),
                        CameraRequestReleaseCondition.OwnerLifetimeEnded,
                        nameof(QaC9CCameraRequestContractsFixture),
                        string.Empty),
                    completed);

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
            AssertNotNull(rigComposer, "CameraRigComposer is missing.");
            AssertNotNull(explicitTargetSource, "Explicit target source is missing.");
            AssertNotNull(observedCamera, "Observed Camera is missing.");
            AssertNotNull(runtimeRecipe, "Runtime CameraRigRecipe was not created.");
        }

        private void RunValidComposerRequest(List<string> completed)
        {
            CameraRequestCreateResult result = CameraRequestCreateResult.Create(
                ValidRequestId("composer"),
                CameraOutputId.Main,
                ValidOwner(),
                ValidLifetime(),
                ValidComposerRig(),
                ValidTargetSource(),
                ValidPolicy(),
                CameraRequestReleaseCondition.OwnerLifetimeEnded,
                nameof(QaC9CCameraRequestContractsFixture),
                "Prove a valid composer-backed request.");

            AssertTrue(result.IsSucceeded, $"Valid composer request failed: {result.BlockingIssue}");
            AssertTrue(result.Request.IsValid, "Valid composer request is invalid.");
            AssertTrue(result.Request.Rig.HasComposer, "Composer reference was lost.");

            Debug.Log(
                $"{LogPrefix} step='valid-composer-request-created' status='{result.Status}' " +
                $"requestId='{result.Request.RequestId}' outputId='{result.Request.OutputId}' " +
                $"owner='{result.Request.Owner}' lifetime='{result.Request.Lifetime}'.",
                this);

            completed.Add("valid-composer-request");
        }

        private void RunValidRecipeRequest(List<string> completed)
        {
            CameraRequestCreateResult result = CameraRequestCreateResult.Create(
                ValidRequestId("recipe"),
                new CameraOutputId("camera.output.qa.synthetic"),
                new CameraRequestOwner(CameraRequestOwnerKind.Activity, "qa.activity.c9c"),
                new CameraRequestLifetime(CameraRequestLifetimeKind.Activity, "qa.activity.c9c"),
                CameraRigReference.FromRecipe(runtimeRecipe),
                ValidTargetSource(),
                new CameraRequestPolicy(200, "qa.activity.c9c"),
                CameraRequestReleaseCondition.ScopeExited,
                nameof(QaC9CCameraRequestContractsFixture),
                "Prove a valid runtime recipe-backed request.");

            AssertTrue(result.IsSucceeded, $"Valid recipe request failed: {result.BlockingIssue}");
            AssertTrue(result.Request.IsValid, "Valid recipe request is invalid.");
            AssertTrue(result.Request.Rig.HasRecipe, "Recipe reference was lost.");

            Debug.Log(
                $"{LogPrefix} step='valid-recipe-request-created' status='{result.Status}' " +
                $"requestId='{result.Request.RequestId}' outputId='{result.Request.OutputId}' " +
                $"policy='{result.Request.Policy}'.",
                this);

            completed.Add("valid-recipe-request");
        }

        private void RunBlockedCase(
            string step,
            string expectedCode,
            CameraRequestCreateResult result,
            List<string> completed)
        {
            AssertTrue(result.IsBlocked, $"Case '{step}' unexpectedly succeeded.");
            AssertTrue(!string.IsNullOrWhiteSpace(result.BlockingIssue), $"Case '{step}' has no blocking issue.");
            AssertTrue(result.Issues != null && result.Issues.Length == 1, $"Case '{step}' did not emit one issue.");
            AssertEqual(expectedCode, result.Issues[0].Code, $"Case '{step}' emitted the wrong issue code.");
            AssertTrue(result.Issues[0].IsBlocking, $"Case '{step}' issue is not blocking.");

            Debug.Log(
                $"{LogPrefix} step='{step}-blocked' status='{result.Status}' " +
                $"code='{result.Issues[0].Code}' issue='{Escape(result.BlockingIssue)}'.",
                this);

            completed.Add(step);
        }

        private void AssertCameraStateUnchanged(
            bool enabledBefore,
            bool activeBefore,
            Vector3 positionBefore,
            List<string> completed)
        {
            AssertNotNull(observedCamera, "Observed camera disappeared.");
            AssertEqual(enabledBefore, observedCamera.enabled, "Request creation changed Camera.enabled.");
            AssertEqual(activeBefore, observedCamera.gameObject.activeSelf, "Request creation changed camera active state.");
            AssertEqual(positionBefore, observedCamera.transform.position, "Request creation changed camera transform.");

            Debug.Log(
                $"{LogPrefix} step='camera-state-unchanged' cameraEnabled='{observedCamera.enabled}' " +
                $"cameraActive='{observedCamera.gameObject.activeSelf}' position='{observedCamera.transform.position}'.",
                this);

            completed.Add("camera-state-unchanged");
        }

        private CameraRequestId ValidRequestId(string suffix)
        {
            return new CameraRequestId($"qa.camera.request.c9c.{suffix}");
        }

        private static CameraRequestOwner ValidOwner()
        {
            return new CameraRequestOwner(
                CameraRequestOwnerKind.Route,
                "qa.route.camera-request-contracts");
        }

        private static CameraRequestLifetime ValidLifetime()
        {
            return new CameraRequestLifetime(
                CameraRequestLifetimeKind.Route,
                "qa.route.camera-request-contracts");
        }

        private CameraRigReference ValidComposerRig()
        {
            return CameraRigReference.FromComposer(rigComposer);
        }

        private CameraTargetSourceDescriptor ValidTargetSource()
        {
            return CameraTargetSourceDescriptor.ExplicitTransform(
                explicitTargetSource,
                "QA C9C Explicit Target Source");
        }

        private static CameraRequestPolicy ValidPolicy()
        {
            return new CameraRequestPolicy(
                100,
                "qa.route.camera-request-contracts");
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

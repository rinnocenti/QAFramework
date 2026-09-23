using System;
using System.Linq;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Current IF-ADR-032 aggregate Camera certification.
    ///
    /// Historical ADR-004/026/028 rails are not certification authority here.
    /// The aggregate executes the focused current-architecture gates and verifies
    /// that deleted legacy Camera product types did not return.
    /// </summary>
    internal static class QaCameraFullCertificationOrchestrator
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run Full Camera QA";
        private const string Prefix =
            "[QA_CAMERA_FULL_032]";
        private const string PhaseKey =
            "ImmersiveFrameworkQA.QA_CAMERA_FULL_032.Phase";
        private const string FailureKey =
            "ImmersiveFrameworkQA.QA_CAMERA_FULL_032.Failure";

        private static readonly string[] RemovedLegacyTypeNames =
        {
            "CameraSharedComposition",
            "SessionCameraOverride",
            "RouteCameraOverride",
            "ActivityCameraOverride",
            "ScopedCameraOverride",
            "PlayerCameraOutputPolicyAuthoring",
            "PlayerCameraCompositionPolicyAuthoring",
            "CompositionCameraRequestPublisher",
            "CameraTargetSourceDescriptor"
        };

        private enum Phase
        {
            Idle = 0,
            RunningPlayerOutput = 10,
            RunningGameFlow = 20,
            Certified = 30,
            Failed = 40
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            CurrentPhase is Phase.Idle or Phase.Certified or Phase.Failed;

        [MenuItem(MenuPath, priority = 230)]
        private static void Run()
        {
            SessionState.EraseString(
                FailureKey);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Fail(
                    "preflight",
                    "Full CAMERA-032 certification must start in Edit Mode.");
                return;
            }

            try
            {
                QaCameraPersistentBaselineGuard
                    .PrepareAndVerify();

                VerifyLegacyProductTypesRemoved();
                VerifyPresentationStructuralContract();

                QaCamera032SubjectTransactionCertification
                    .RunForAggregate();
                Require(
                    QaCamera032SubjectTransactionCertification
                        .AggregatePassed,
                    "CAMERA-032 Subject + Transaction focused gate did not pass.");

                SetPhase(
                    Phase.RunningPlayerOutput);

                Debug.Log(
                    $"{Prefix} status='Running' " +
                    "phase='PlayerOutput' " +
                    "subjectTransaction='17/17 PASS'.");

                QaCamera032PlayerOutputLifecycleCertification
                    .RunForAggregate();
            }
            catch (Exception exception)
            {
                Fail(
                    "start",
                    exception.GetBaseException().Message);
            }
        }

        private static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                switch (CurrentPhase)
                {
                    case Phase.RunningPlayerOutput:
                        ContinueAfterPlayerOutput();
                        break;

                    case Phase.RunningGameFlow:
                        ContinueAfterGameFlow();
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(
                    "aggregate",
                    exception.GetBaseException().Message);
            }
        }

        private static void ContinueAfterPlayerOutput()
        {
            if (QaCamera032PlayerOutputLifecycleCertification
                    .AggregateFailed)
            {
                Fail(
                    "player-output",
                    "CAMERA-032 Player Output Lifecycle focused gate failed. See its focused diagnostic above.");
                return;
            }

            if (!QaCamera032PlayerOutputLifecycleCertification
                    .AggregateCompleted)
            {
                return;
            }

            VerifyLegacyProductTypesRemoved();

            SetPhase(
                Phase.RunningGameFlow);

            Debug.Log(
                $"{Prefix} status='Running' " +
                "phase='GameFlow' " +
                "subjectTransaction='17/17 PASS' " +
                "playerOutput='PASS'.");

            QaCamera032GameFlowLifecycleCertification
                .RunForAggregate();
        }

        private static void ContinueAfterGameFlow()
        {
            if (QaCamera032GameFlowLifecycleCertification
                    .AggregateFailed)
            {
                Fail(
                    "game-flow",
                    "CAMERA-032 Game Flow Lifecycle focused gate failed. See its focused diagnostic above.");
                return;
            }

            if (!QaCamera032GameFlowLifecycleCertification
                    .AggregateCompleted)
            {
                return;
            }

            FinishSuccess();
        }

        private static void FinishSuccess()
        {
            QaCameraPersistentBaselineGuard
                .RestoreCanonicalBaseline();

            VerifyLegacyProductTypesRemoved();
            VerifyPresentationStructuralContract();

            Require(
                QaCamera032SubjectTransactionCertification
                    .AggregatePassed,
                "Subject + Transaction focused evidence was lost before aggregate completion.");
            Require(
                QaCamera032PlayerOutputLifecycleCertification
                    .AggregateCompleted,
                "Player Output focused evidence was lost before aggregate completion.");
            Require(
                QaCamera032GameFlowLifecycleCertification
                    .AggregateCompleted,
                "Game Flow focused evidence was lost before aggregate completion.");

            SetPhase(
                Phase.Certified);

            Debug.Log(
                $"{Prefix} status='Passed' " +
                "verdict='CAMERA_032_FULL_CERTIFIED' " +
                "camera032A='structural PASS' " +
                "playerOutput='PASS' " +
                "gameFlow='16/16 PASS' " +
                "subjectTransaction='17/17 PASS' " +
                "legacyProductTypes='0' " +
                "presentationIntent='PASS' " +
                "canonicalRestore='PASS'.");
        }

        private static void VerifyLegacyProductTypesRemoved()
        {
            Type[] cameraAssemblyTypes =
                typeof(CameraOutputAuthoring)
                    .Assembly
                    .GetTypes();

            for (int index = 0;
                 index < RemovedLegacyTypeNames.Length;
                 index++)
            {
                string legacyName =
                    RemovedLegacyTypeNames[index];

                bool exists =
                    cameraAssemblyTypes.Any(
                        type =>
                            string.Equals(
                                type.Name,
                                legacyName,
                                StringComparison.Ordinal));

                Require(
                    !exists,
                    $"Legacy Camera product type '{legacyName}' is still present in the Framework runtime assembly.");
            }

            Require(
                !Enum.GetNames(
                        typeof(CameraRequestOwnerKind))
                    .Contains(
                        "Composition",
                        StringComparer.Ordinal),
                "CameraRequestOwnerKind still exposes the obsolete Composition owner.");

            Require(
                !Enum.GetNames(
                        typeof(CameraRequestLifetimeKind))
                    .Contains(
                        "Composition",
                        StringComparer.Ordinal),
                "CameraRequestLifetimeKind still exposes the obsolete Composition lifetime.");
        }

        private static void VerifyPresentationStructuralContract()
        {
            Type presentationRuntime = typeof(CameraOutputAuthoring)
                .Assembly
                .GetType(
                    "Immersive.Framework.Camera.CameraPresentationRuntime",
                    throwOnError: false);

            Require(
                presentationRuntime != null,
                "CAMERA-032-A requires CameraPresentationRuntime.");
            Require(
                !typeof(MonoBehaviour).IsAssignableFrom(presentationRuntime),
                "CameraPresentationRuntime must not be a scene MonoBehaviour.");
            Require(
                !typeof(ScriptableObject).IsAssignableFrom(presentationRuntime),
                "CameraPresentationRuntime must not store occurrence state in a ScriptableObject.");

            string[] serializedFields = typeof(CameraPresentationDefinition)
                .GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic)
                .Where(field =>
                    field.IsPublic ||
                    field.IsDefined(typeof(SerializeField), false))
                .Select(field => field.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            string[] allowedFields =
            {
                "description",
                "outputDefinition",
                "requestPrecedence",
                "rigPrefab",
                "stableId",
                "subjectPolicy",
                "transitionMode"
            };

            Require(
                serializedFields.SequenceEqual(allowedFields),
                "CameraPresentationDefinition serialized fields are not reusable intent only. " +
                $"found='{string.Join(",", serializedFields)}'.");

            Require(
                typeof(CameraRequest)
                    .GetProperties()
                    .All(property =>
                        property.Name.IndexOf(
                            "TargetSource",
                            StringComparison.Ordinal) < 0),
                "CameraRequest still exposes a target-source field.");
        }

        private static void Fail(
            string stage,
            string reason)
        {
            SetPhase(
                Phase.Failed);
            SessionState.SetString(
                FailureKey,
                reason ?? string.Empty);

            QaCameraPersistentBaselineGuard
                .RequestRestore(
                    $"camera-032-full-failed:{stage}");

            Debug.LogError(
                $"{Prefix} status='Failed' " +
                "verdict='CAMERA_032_FULL_NOT_CERTIFIED' " +
                $"stage='{Escape(stage)}' " +
                $"diagnostic='{Escape(reason)}' " +
                "cleanup='RequestedCanonicalRestore'.");
        }

        private static Phase CurrentPhase =>
            (Phase)SessionState.GetInt(
                PhaseKey,
                (int)Phase.Idle);

        private static void SetPhase(
            Phase phase) =>
            SessionState.SetInt(
                PhaseKey,
                (int)phase);

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static string Escape(
            string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
    }
}

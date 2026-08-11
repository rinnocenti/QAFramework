using System;
using System.Reflection;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Bootstrap;
using Immersive.Framework.Performance;
using ImmersiveFrameworkQA.Lifecycle;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaProjectFrameRateRegression
    {
        private const string PlayMenuPath =
            "Immersive Framework/QA/Regressions/Application/" +
            "Run Project Frame Rate Regression";
        private const string EditMenuPath =
            "Immersive Framework/QA/Regressions/Application/" +
            "Run Project Frame Rate Edit Validation";

        private const string TargetPrefix =
            "[ADR017_QA_TARGET]";
        private const string VSyncPrefix =
            "[ADR017_QA_VSYNC]";
        private const string DefaultsPrefix =
            "[ADR017_QA_DEFAULTS]";
        private const string EditPrefix =
            "[ADR017_QA_EDIT]";

        private const int PlayCaseCount = 13;
        private const int EditCaseCount = 13;

        [MenuItem(PlayMenuPath, true)]
        private static bool ValidatePlayRun() =>
            EditorApplication.isPlaying;

        [MenuItem(PlayMenuPath)]
        private static void RunPlayRegression()
        {
            QaProjectFrameRatePreparedMode mode =
                QaProjectFrameRateSetup
                    .RequirePreparedForCurrentPlayMode();

            var cases = new CaseCounter(PlayCaseCount);

            Require(
                EditorApplication.isPlaying,
                "ADR-017 play regression requires Play Mode.");
            cases.Complete();

            Require(
                PlayerPrefs.GetInt(
                    QaProjectFrameRatePreBootFixture.SeedAppliedKey,
                    0) == 1,
                "ADR-017 preboot seed was not applied.");
            cases.Complete();

            Require(
                PlayerPrefs.GetInt(
                    QaProjectFrameRatePreBootFixture.SeedTargetKey,
                    int.MinValue) ==
                    QaProjectFrameRatePreBootFixture
                        .SentinelTargetFrameRate &&
                PlayerPrefs.GetInt(
                    QaProjectFrameRatePreBootFixture.SeedVSyncKey,
                    int.MinValue) ==
                    QaProjectFrameRatePreBootFixture
                        .SentinelVSyncCount,
                "ADR-017 preboot seed evidence does not match the canonical sentinel.");
            cases.Complete();

            ImmersiveFrameworkSettingsAsset settings =
                QaProjectFrameRateSetup
                    .ResolveUniqueSettings();
            Require(
                settings.FrameRatePolicy != null &&
                settings.FrameRatePolicy.TryValidate(
                    out _),
                "ADR-017 Project Settings Frame Rate policy is missing or invalid.");
            cases.Complete();

            GameApplicationAsset gameApplication =
                settings.ActiveGameApplication;
            Require(
                gameApplication != null &&
                !HasGameApplicationFrameRateAuthority(
                    gameApplication),
                "ADR-017 GameApplication still exposes a Frame Rate authoring authority.");
            cases.Complete();

            Require(
                QaH2FrameworkReadiness
                    .TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string hostDiagnostic),
                hostDiagnostic);
            Require(
                host.State.GameFlowStarted,
                "ADR-017 requires a successfully started official FrameworkRuntimeHost.");
            cases.Complete();

            ApplicationFrameRateApplicationResult result =
                host.LastFrameRateApplicationResult;
            Require(
                result.Succeeded,
                $"ADR-017 runtime Frame Rate result failed. status='{result.Status}' message='{result.Message}'.");
            cases.Complete();

            ApplicationFrameRateMode expectedMode =
                ToApplicationMode(mode);
            Require(
                result.RequestedMode ==
                    expectedMode &&
                settings.FrameRatePolicy.Mode ==
                    expectedMode,
                $"ADR-017 requested mode mismatch. prepared='{mode}' settings='{settings.FrameRatePolicy.Mode}' runtime='{result.RequestedMode}'.");
            cases.Complete();

            Require(
                result.PreviousTargetFrameRate ==
                    QaProjectFrameRatePreBootFixture
                        .SentinelTargetFrameRate &&
                result.PreviousVSyncCount ==
                    QaProjectFrameRatePreBootFixture
                        .SentinelVSyncCount,
                $"ADR-017 runtime did not observe the preboot sentinel. previousTarget='{result.PreviousTargetFrameRate}' previousVSync='{result.PreviousVSyncCount}'.");
            cases.Complete();

            GetExpectedEffectiveValues(
                mode,
                out int expectedTarget,
                out int expectedVSync);

            Require(
                Application.targetFrameRate ==
                    expectedTarget,
                $"ADR-017 effective target frame rate mismatch. expected='{expectedTarget}' actual='{Application.targetFrameRate}'.");
            cases.Complete();

            Require(
                QualitySettings.vSyncCount ==
                    expectedVSync,
                $"ADR-017 effective VSync mismatch. expected='{expectedVSync}' actual='{QualitySettings.vSyncCount}'.");
            cases.Complete();

            Require(
                result.AppliedTargetFrameRate ==
                    Application.targetFrameRate &&
                result.AppliedVSyncCount ==
                    QualitySettings.vSyncCount,
                "ADR-017 typed runtime result does not match current Unity frame pacing values.");
            cases.Complete();

            RequireStatusSemantics(
                mode,
                result);
            cases.Complete();

            cases.RequireComplete();

            string prefix =
                mode switch
                {
                    QaProjectFrameRatePreparedMode
                        .TargetFrameRate => TargetPrefix,
                    QaProjectFrameRatePreparedMode
                        .VerticalSync => VSyncPrefix,
                    QaProjectFrameRatePreparedMode
                        .UseUnityDefaults => DefaultsPrefix,
                    _ => throw new ArgumentOutOfRangeException()
                };

            Debug.Log(
                $"{prefix} status='Passed' " +
                $"cases='{PlayCaseCount}' " +
                $"source='ProjectSettings' " +
                $"mode='{result.RequestedMode}' " +
                $"previousTargetFrameRate='{result.PreviousTargetFrameRate}' " +
                $"previousVSyncCount='{result.PreviousVSyncCount}' " +
                $"appliedTargetFrameRate='{result.AppliedTargetFrameRate}' " +
                $"appliedVSyncCount='{result.AppliedVSyncCount}' " +
                $"runtimeStatus='{result.Status}' " +
                $"platform='{result.Platform}' " +
                $"gameApplicationFrameRateAuthority='Absent'.");
        }

        [MenuItem(EditMenuPath, true)]
        private static bool ValidateEditRun() =>
            !EditorApplication.isPlaying;

        [MenuItem(EditMenuPath)]
        private static void RunEditValidation()
        {
            var cases = new CaseCounter(EditCaseCount);

            Require(
                !EditorApplication.isPlaying,
                "ADR-017 edit validation must run outside Play Mode.");
            cases.Complete();

            ImmersiveFrameworkSettingsAsset settings =
                QaProjectFrameRateSetup
                    .ResolveUniqueSettings();
            cases.Complete();

            var settingsSerialized =
                new SerializedObject(settings);
            settingsSerialized.Update();

            SerializedProperty policy =
                settingsSerialized.FindProperty(
                    "frameRatePolicy");
            Require(
                policy != null,
                "ADR-017 Project Settings serialized Frame Rate policy is missing.");
            cases.Complete();

            GameApplicationAsset gameApplication =
                settings.ActiveGameApplication;
            Require(
                gameApplication != null,
                "ADR-017 edit validation requires the active Game Application.");
            cases.Complete();

            SerializedProperty gameApplicationPolicy =
                new SerializedObject(gameApplication)
                    .FindProperty("frameRatePolicy");
            Require(
                gameApplicationPolicy == null,
                "ADR-017 GameApplication still serializes 'frameRatePolicy'.");
            cases.Complete();

            PropertyInfo gameApplicationPolicyProperty =
                typeof(GameApplicationAsset).GetProperty(
                    "FrameRatePolicy",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);
            Require(
                gameApplicationPolicyProperty == null,
                "ADR-017 GameApplication still exposes 'FrameRatePolicy'.");
            cases.Complete();

            SerializedProperty policyMode =
                policy.FindPropertyRelative("mode");
            SerializedProperty target =
                policy.FindPropertyRelative(
                    "targetFrameRate");
            SerializedProperty vSync =
                policy.FindPropertyRelative(
                    "vSyncCount");

            Require(
                policyMode != null &&
                target != null &&
                vSync != null,
                "ADR-017 Project Settings Frame Rate serialized policy shape is incomplete.");

            int originalMode = policyMode.intValue;
            int originalTarget = target.intValue;
            int originalVSync = vSync.intValue;
            int originalUnityTarget =
                Application.targetFrameRate;
            int originalUnityVSync =
                QualitySettings.vSyncCount;

            try
            {
                Application.targetFrameRate =
                    QaProjectFrameRatePreBootFixture
                        .SentinelTargetFrameRate;
                QualitySettings.vSyncCount =
                    QaProjectFrameRatePreBootFixture
                        .SentinelVSyncCount;

                policyMode.intValue = int.MaxValue;
                settingsSerialized.ApplyModifiedProperties();
                cases.Complete();

                FrameworkBootResult boot =
                    FrameworkBootValidator.Validate(
                        settings);
                Require(
                    !boot.Succeeded,
                    "ADR-017 invalid Project Frame Rate policy unexpectedly passed boot validation.");
                cases.Complete();

                Require(
                    boot.Message.Contains(
                        "Project Frame Rate policy is invalid",
                        StringComparison.Ordinal),
                    $"ADR-017 invalid boot diagnostic is not explicit. message='{boot.Message}'.");
                cases.Complete();

                Require(
                    Application.targetFrameRate ==
                        QaProjectFrameRatePreBootFixture
                            .SentinelTargetFrameRate &&
                    QualitySettings.vSyncCount ==
                        QaProjectFrameRatePreBootFixture
                            .SentinelVSyncCount,
                    "ADR-017 boot validation mutated Unity frame pacing values.");
                cases.Complete();

                ApplicationFrameRateApplicationResult apply =
                    ApplicationFrameRatePolicyApplier.Apply(
                        settings.FrameRatePolicy);
                Require(
                    !apply.Succeeded &&
                    apply.Status ==
                        ApplicationFrameRateApplicationStatus
                            .RejectedInvalidPolicy,
                    $"ADR-017 invalid policy applier result was not rejected. status='{apply.Status}'.");
                cases.Complete();

                Require(
                    Application.targetFrameRate ==
                        QaProjectFrameRatePreBootFixture
                            .SentinelTargetFrameRate &&
                    QualitySettings.vSyncCount ==
                        QaProjectFrameRatePreBootFixture
                            .SentinelVSyncCount,
                    "ADR-017 invalid policy application partially mutated Unity values.");
                cases.Complete();
            }
            finally
            {
                settingsSerialized.Update();
                policy =
                    settingsSerialized.FindProperty(
                        "frameRatePolicy");
                if (policy != null)
                {
                    policyMode =
                        policy.FindPropertyRelative(
                            "mode");
                    target =
                        policy.FindPropertyRelative(
                            "targetFrameRate");
                    vSync =
                        policy.FindPropertyRelative(
                            "vSyncCount");

                    if (policyMode != null)
                    {
                        policyMode.intValue =
                            originalMode;
                    }

                    if (target != null)
                    {
                        target.intValue =
                            originalTarget;
                    }

                    if (vSync != null)
                    {
                        vSync.intValue =
                            originalVSync;
                    }

                    settingsSerialized.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                }

                Application.targetFrameRate =
                    originalUnityTarget;
                QualitySettings.vSyncCount =
                    originalUnityVSync;
            }

            cases.Complete();
            cases.RequireComplete();

            Debug.Log(
                $"{EditPrefix} status='Passed' " +
                $"cases='{EditCaseCount}' " +
                $"invalidProjectPolicy='RejectedBeforeMutation' " +
                $"invalidApplier='RejectedWithoutPartialMutation' " +
                $"projectSettingsAuthority='Present' " +
                $"gameApplicationSerializedAuthority='Absent' " +
                $"gameApplicationApiAuthority='Absent' " +
                $"restored='True'.");
        }

        private static bool
            HasGameApplicationFrameRateAuthority(
                GameApplicationAsset gameApplication)
        {
            if (gameApplication == null)
            {
                return false;
            }

            var serialized =
                new SerializedObject(gameApplication);

            if (serialized.FindProperty(
                    "frameRatePolicy") != null)
            {
                return true;
            }

            return typeof(GameApplicationAsset)
                .GetProperty(
                    "FrameRatePolicy",
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic) != null;
        }

        private static ApplicationFrameRateMode
            ToApplicationMode(
                QaProjectFrameRatePreparedMode mode)
        {
            return mode switch
            {
                QaProjectFrameRatePreparedMode
                    .TargetFrameRate =>
                    ApplicationFrameRateMode.TargetFrameRate,
                QaProjectFrameRatePreparedMode
                    .VerticalSync =>
                    ApplicationFrameRateMode.VerticalSync,
                QaProjectFrameRatePreparedMode
                    .UseUnityDefaults =>
                    ApplicationFrameRateMode.UseUnityDefaults,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(mode),
                    mode,
                    "Unsupported ADR-017 QA mode.")
            };
        }

        private static void GetExpectedEffectiveValues(
            QaProjectFrameRatePreparedMode mode,
            out int targetFrameRate,
            out int vSyncCount)
        {
            switch (mode)
            {
                case QaProjectFrameRatePreparedMode
                    .TargetFrameRate:
                    targetFrameRate =
                        QaProjectFrameRateSetup
                            .TargetFrameRateValue;
                    vSyncCount = 0;
                    return;

                case QaProjectFrameRatePreparedMode
                    .VerticalSync:
                    targetFrameRate = -1;
                    vSyncCount =
                        QaProjectFrameRateSetup
                            .VerticalSyncCountValue;
                    return;

                case QaProjectFrameRatePreparedMode
                    .UseUnityDefaults:
                    targetFrameRate =
                        QaProjectFrameRatePreBootFixture
                            .SentinelTargetFrameRate;
                    vSyncCount =
                        QaProjectFrameRatePreBootFixture
                            .SentinelVSyncCount;
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mode),
                        mode,
                        "Unsupported ADR-017 QA mode.");
            }
        }

        private static void RequireStatusSemantics(
            QaProjectFrameRatePreparedMode mode,
            ApplicationFrameRateApplicationResult result)
        {
            switch (mode)
            {
                case QaProjectFrameRatePreparedMode
                    .UseUnityDefaults:
                    Require(
                        result.Status ==
                            ApplicationFrameRateApplicationStatus
                                .SkippedUnityDefaults,
                        $"UseUnityDefaults must report SkippedUnityDefaults. actual='{result.Status}'.");
                    return;

                case QaProjectFrameRatePreparedMode
                    .TargetFrameRate:
                    Require(
                        result.Status ==
                            ApplicationFrameRateApplicationStatus
                                .Applied,
                        $"TargetFrameRate must mutate the canonical sentinel and report Applied. actual='{result.Status}'.");
                    return;

                case QaProjectFrameRatePreparedMode
                    .VerticalSync:
                    Require(
                        result.Status ==
                            ApplicationFrameRateApplicationStatus
                                .Applied ||
                        result.Status ==
                            ApplicationFrameRateApplicationStatus
                                .AppliedPlatformLimited,
                        $"VerticalSync must report Applied or AppliedPlatformLimited. actual='{result.Status}'.");
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mode),
                        mode,
                        "Unsupported ADR-017 QA mode.");
            }
        }

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

        private sealed class CaseCounter
        {
            private readonly int _expected;
            private int _completed;

            internal CaseCounter(int expected)
            {
                _expected = expected;
            }

            internal void Complete()
            {
                _completed++;
            }

            internal void RequireComplete()
            {
                Require(
                    _completed == _expected,
                    $"ADR-017 QA case-count mismatch. completed='{_completed}' expected='{_expected}'.");
            }
        }
    }
}

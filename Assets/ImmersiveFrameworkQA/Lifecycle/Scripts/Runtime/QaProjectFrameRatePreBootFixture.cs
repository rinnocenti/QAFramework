using System;
using UnityEngine;

namespace ImmersiveFrameworkQA.Lifecycle
{
    public enum QaProjectFrameRatePreparedMode
    {
        None = 0,
        TargetFrameRate = 10,
        VerticalSync = 20,
        UseUnityDefaults = 30
    }

    /// <summary>
    /// One-shot QA fixture that seeds Unity frame pacing before the framework
    /// AfterSceneLoad bootstrap. It is inert unless explicitly armed by the
    /// ADR-017 Editor setup.
    /// </summary>
    public static class QaProjectFrameRatePreBootFixture
    {
        public const string ArmModeKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.ArmMode";
        public const string ObservedModeKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.ObservedMode";
        public const string SeedAppliedKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.SeedApplied";
        public const string SeedTargetKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.SeedTarget";
        public const string SeedVSyncKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.SeedVSync";

        public const int SentinelTargetFrameRate = 47;
        public const int SentinelVSyncCount = 2;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SeedBeforeFrameworkBootstrap()
        {
            int rawMode = PlayerPrefs.GetInt(
                ArmModeKey,
                (int)QaProjectFrameRatePreparedMode.None);

            if (!Enum.IsDefined(
                    typeof(QaProjectFrameRatePreparedMode),
                    rawMode) ||
                rawMode ==
                    (int)QaProjectFrameRatePreparedMode.None)
            {
                return;
            }

            var mode =
                (QaProjectFrameRatePreparedMode)rawMode;

            // Consume the arm immediately so a failed/aborted QA run cannot
            // silently seed a later unrelated Play Mode session.
            PlayerPrefs.DeleteKey(ArmModeKey);

            Application.targetFrameRate =
                SentinelTargetFrameRate;
            QualitySettings.vSyncCount =
                SentinelVSyncCount;

            PlayerPrefs.SetInt(
                ObservedModeKey,
                rawMode);
            PlayerPrefs.SetInt(
                SeedAppliedKey,
                1);
            PlayerPrefs.SetInt(
                SeedTargetKey,
                Application.targetFrameRate);
            PlayerPrefs.SetInt(
                SeedVSyncKey,
                QualitySettings.vSyncCount);
            PlayerPrefs.Save();

            Debug.Log(
                $"[ADR017_QA_PREBOOT] status='Seeded' " +
                $"mode='{mode}' " +
                $"targetFrameRate='{Application.targetFrameRate}' " +
                $"vSyncCount='{QualitySettings.vSyncCount}'.");
        }
    }
}

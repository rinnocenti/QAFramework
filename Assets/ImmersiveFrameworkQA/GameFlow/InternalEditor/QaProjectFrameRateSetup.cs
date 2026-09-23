using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.Performance;
using ImmersiveFrameworkQA.Lifecycle;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    [InitializeOnLoad]
    internal static class QaProjectFrameRateSetup
    {
        private const string Prefix =
            "[ADR017_QA_SETUP]";
        private const string MenuRoot =
            "Immersive Framework/QA/Setup/Application Frame Rate/";
        private const string PrepareTargetMenuPath =
            MenuRoot + "Prepare Target Frame Rate";
        private const string PrepareVSyncMenuPath =
            MenuRoot + "Prepare Vertical Sync";
        private const string PrepareDefaultsMenuPath =
            MenuRoot + "Prepare Use Unity Defaults";
        private const string RestoreMenuPath =
            MenuRoot + "Restore Project Frame Rate";
        private const string ReportMenuPath =
            MenuRoot + "Report Project Frame Rate Fixture";

        private const string PreparedKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.Prepared";
        private const string PreparedModeKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.PreparedMode";
        private const string RestoreAfterPlayKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.RestoreAfterPlay";
        private const string SettingsPathKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.SettingsPath";
        private const string OriginalModeKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.OriginalMode";
        private const string OriginalTargetKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.OriginalTarget";
        private const string OriginalVSyncKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.OriginalVSync";
        private const string OriginalUnityTargetKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.OriginalUnityTarget";
        private const string OriginalUnityVSyncKey =
            "ImmersiveFrameworkQA.ADR017.FrameRate.OriginalUnityVSync";

        internal const int TargetFrameRateValue = 73;
        internal const int VerticalSyncCountValue = 3;

        static QaProjectFrameRateSetup()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
        }

        [MenuItem(PrepareTargetMenuPath, true)]
        [MenuItem(PrepareVSyncMenuPath, true)]
        [MenuItem(PrepareDefaultsMenuPath, true)]
        private static bool ValidatePrepare() =>
            !EditorApplication.isPlaying;

        [MenuItem(PrepareTargetMenuPath)]
        private static void PrepareTarget()
        {
            Prepare(
                QaProjectFrameRatePreparedMode.TargetFrameRate);
        }

        [MenuItem(PrepareVSyncMenuPath)]
        private static void PrepareVerticalSync()
        {
            Prepare(
                QaProjectFrameRatePreparedMode.VerticalSync);
        }

        [MenuItem(PrepareDefaultsMenuPath)]
        private static void PrepareUseUnityDefaults()
        {
            Prepare(
                QaProjectFrameRatePreparedMode.UseUnityDefaults);
        }

        [MenuItem(RestoreMenuPath, true)]
        private static bool ValidateRestore() =>
            !EditorApplication.isPlaying;

        [MenuItem(RestoreMenuPath)]
        private static void RestoreFromMenu()
        {
            RestoreInternal(
                "manual-restore",
                logSuccess: true);
        }

        [MenuItem(ReportMenuPath)]
        private static void Report()
        {
            Debug.Log(
                $"{Prefix} status='Current' " +
                $"prepared='{SessionState.GetBool(PreparedKey, false)}' " +
                $"mode='{SessionState.GetString(PreparedModeKey, "None")}' " +
                $"settings='{SessionState.GetString(SettingsPathKey, string.Empty)}' " +
                $"armMode='{PlayerPrefs.GetInt(QaProjectFrameRatePreBootFixture.ArmModeKey, 0)}' " +
                $"observedMode='{PlayerPrefs.GetInt(QaProjectFrameRatePreBootFixture.ObservedModeKey, 0)}' " +
                $"seedApplied='{PlayerPrefs.GetInt(QaProjectFrameRatePreBootFixture.SeedAppliedKey, 0)}'.");
        }

        internal static QaProjectFrameRatePreparedMode
            RequirePreparedForCurrentPlayMode()
        {
            Require(
                EditorApplication.isPlaying,
                "ADR-017 Project Frame Rate regression requires Play Mode.");
            Require(
                SessionState.GetBool(PreparedKey, false),
                "ADR-017 Project Frame Rate is not prepared. Exit Play Mode and run one of the Application Frame Rate preparation menus.");

            Require(
                Enum.TryParse(
                    SessionState.GetString(
                        PreparedModeKey,
                        "None"),
                    out QaProjectFrameRatePreparedMode mode) &&
                mode != QaProjectFrameRatePreparedMode.None,
                "ADR-017 prepared mode is missing or invalid.");

            int observedMode = PlayerPrefs.GetInt(
                QaProjectFrameRatePreBootFixture.ObservedModeKey,
                0);
            Require(
                observedMode == (int)mode,
                $"ADR-017 preboot fixture mode mismatch. prepared='{mode}' observed='{observedMode}'.");

            return mode;
        }

        internal static ImmersiveFrameworkSettingsAsset
            ResolveUniqueSettings()
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:ImmersiveFrameworkSettingsAsset");

            Require(
                guids != null && guids.Length == 1,
                $"ADR-017 QA requires exactly one ImmersiveFrameworkSettingsAsset. found='{guids?.Length ?? 0}'.");

            string path =
                AssetDatabase.GUIDToAssetPath(guids[0]);
            var settings =
                AssetDatabase.LoadAssetAtPath<
                    ImmersiveFrameworkSettingsAsset>(path);

            Require(
                settings != null,
                $"ADR-017 QA could not load Framework Settings at '{path}'.");

            return settings;
        }

        private static void Prepare(
            QaProjectFrameRatePreparedMode mode)
        {
            Require(
                !EditorApplication.isPlaying,
                "ADR-017 Frame Rate preparation must run outside Play Mode.");
            Require(
                mode != QaProjectFrameRatePreparedMode.None,
                "ADR-017 Frame Rate preparation mode must be explicit.");
            Require(
                !SessionState.GetBool(PreparedKey, false),
                $"ADR-017 Frame Rate fixture is already prepared. Run '{RestoreMenuPath}' first.");

            ImmersiveFrameworkSettingsAsset settings =
                ResolveUniqueSettings();
            var serialized =
                new SerializedObject(settings);
            serialized.Update();

            SerializedProperty policy =
                serialized.FindProperty("frameRatePolicy");
            Require(
                policy != null,
                "Project Settings does not expose serialized 'frameRatePolicy'.");

            SerializedProperty policyMode =
                policy.FindPropertyRelative("mode");
            SerializedProperty target =
                policy.FindPropertyRelative("targetFrameRate");
            SerializedProperty vSync =
                policy.FindPropertyRelative("vSyncCount");

            Require(
                policyMode != null &&
                target != null &&
                vSync != null,
                "Project Frame Rate serialized policy shape is incomplete.");

            string settingsPath =
                AssetDatabase.GetAssetPath(settings);

            SessionState.SetInt(
                OriginalModeKey,
                policyMode.intValue);
            SessionState.SetInt(
                OriginalTargetKey,
                target.intValue);
            SessionState.SetInt(
                OriginalVSyncKey,
                vSync.intValue);
            SessionState.SetInt(
                OriginalUnityTargetKey,
                Application.targetFrameRate);
            SessionState.SetInt(
                OriginalUnityVSyncKey,
                QualitySettings.vSyncCount);
            SessionState.SetString(
                SettingsPathKey,
                settingsPath);

            ApplyPreparedPolicy(
                mode,
                policyMode,
                target,
                vSync);

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            ClearRuntimeEvidence();
            PlayerPrefs.SetInt(
                QaProjectFrameRatePreBootFixture.ArmModeKey,
                (int)mode);
            PlayerPrefs.Save();

            SessionState.SetBool(
                PreparedKey,
                true);
            SessionState.SetString(
                PreparedModeKey,
                mode.ToString());
            SessionState.SetBool(
                RestoreAfterPlayKey,
                true);

            Debug.Log(
                $"{Prefix} status='Prepared' " +
                $"mode='{mode}' " +
                $"settings='{settingsPath}' " +
                $"projectMode='{settings.FrameRatePolicy.Mode}' " +
                $"targetFrameRate='{settings.FrameRatePolicy.TargetFrameRate}' " +
                $"vSyncCount='{settings.FrameRatePolicy.VSyncCount}' " +
                $"prebootSentinelTarget='{QaProjectFrameRatePreBootFixture.SentinelTargetFrameRate}' " +
                $"prebootSentinelVSync='{QaProjectFrameRatePreBootFixture.SentinelVSyncCount}'.");
        }

        private static void ApplyPreparedPolicy(
            QaProjectFrameRatePreparedMode mode,
            SerializedProperty policyMode,
            SerializedProperty target,
            SerializedProperty vSync)
        {
            switch (mode)
            {
                case QaProjectFrameRatePreparedMode.TargetFrameRate:
                    policyMode.intValue =
                        (int)ApplicationFrameRateMode.TargetFrameRate;
                    target.intValue =
                        TargetFrameRateValue;
                    vSync.intValue = 1;
                    return;

                case QaProjectFrameRatePreparedMode.VerticalSync:
                    policyMode.intValue =
                        (int)ApplicationFrameRateMode.VerticalSync;
                    target.intValue = 60;
                    vSync.intValue =
                        VerticalSyncCountValue;
                    return;

                case QaProjectFrameRatePreparedMode.UseUnityDefaults:
                    policyMode.intValue =
                        (int)ApplicationFrameRateMode.UseUnityDefaults;
                    target.intValue = 60;
                    vSync.intValue = 1;
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(mode),
                        mode,
                        "Unsupported ADR-017 prepared mode.");
            }
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state !=
                    PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(
                    RestoreAfterPlayKey,
                    false) ||
                !SessionState.GetBool(
                    PreparedKey,
                    false))
            {
                return;
            }

            RestoreInternal(
                "post-play-auto-restore",
                logSuccess: true);
        }

        private static void RestoreInternal(
            string reason,
            bool logSuccess)
        {
            string path =
                SessionState.GetString(
                    SettingsPathKey,
                    string.Empty);

            if (!string.IsNullOrWhiteSpace(path))
            {
                var settings =
                    AssetDatabase.LoadAssetAtPath<
                        ImmersiveFrameworkSettingsAsset>(path);

                if (settings != null)
                {
                    var serialized =
                        new SerializedObject(settings);
                    serialized.Update();

                    SerializedProperty policy =
                        serialized.FindProperty(
                            "frameRatePolicy");
                    if (policy != null)
                    {
                        SerializedProperty policyMode =
                            policy.FindPropertyRelative(
                                "mode");
                        SerializedProperty target =
                            policy.FindPropertyRelative(
                                "targetFrameRate");
                        SerializedProperty vSync =
                            policy.FindPropertyRelative(
                                "vSyncCount");

                        if (policyMode != null)
                        {
                            policyMode.intValue =
                                SessionState.GetInt(
                                    OriginalModeKey,
                                    policyMode.intValue);
                        }

                        if (target != null)
                        {
                            target.intValue =
                                SessionState.GetInt(
                                    OriginalTargetKey,
                                    target.intValue);
                        }

                        if (vSync != null)
                        {
                            vSync.intValue =
                                SessionState.GetInt(
                                    OriginalVSyncKey,
                                    vSync.intValue);
                        }

                        serialized.ApplyModifiedProperties();
                        EditorUtility.SetDirty(settings);
                    }
                }
            }

            Application.targetFrameRate =
                SessionState.GetInt(
                    OriginalUnityTargetKey,
                    Application.targetFrameRate);
            QualitySettings.vSyncCount =
                SessionState.GetInt(
                    OriginalUnityVSyncKey,
                    QualitySettings.vSyncCount);

            AssetDatabase.SaveAssets();
            ClearRuntimeEvidence();

            SessionState.EraseBool(PreparedKey);
            SessionState.EraseString(PreparedModeKey);
            SessionState.EraseBool(RestoreAfterPlayKey);
            SessionState.EraseString(SettingsPathKey);
            SessionState.EraseInt(OriginalModeKey);
            SessionState.EraseInt(OriginalTargetKey);
            SessionState.EraseInt(OriginalVSyncKey);
            SessionState.EraseInt(OriginalUnityTargetKey);
            SessionState.EraseInt(OriginalUnityVSyncKey);

            if (logSuccess)
            {
                Debug.Log(
                    $"{Prefix} status='Restored' " +
                    $"reason='{reason}' " +
                    $"targetFrameRate='{Application.targetFrameRate}' " +
                    $"vSyncCount='{QualitySettings.vSyncCount}'.");
            }
        }

        private static void ClearRuntimeEvidence()
        {
            PlayerPrefs.DeleteKey(
                QaProjectFrameRatePreBootFixture.ArmModeKey);
            PlayerPrefs.DeleteKey(
                QaProjectFrameRatePreBootFixture.ObservedModeKey);
            PlayerPrefs.DeleteKey(
                QaProjectFrameRatePreBootFixture.SeedAppliedKey);
            PlayerPrefs.DeleteKey(
                QaProjectFrameRatePreBootFixture.SeedTargetKey);
            PlayerPrefs.DeleteKey(
                QaProjectFrameRatePreBootFixture.SeedVSyncKey);
            PlayerPrefs.Save();
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
    }
}

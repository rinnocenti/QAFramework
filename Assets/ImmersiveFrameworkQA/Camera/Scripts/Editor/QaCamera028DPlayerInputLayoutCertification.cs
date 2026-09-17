using System;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Two-boot CAMERA-028-D certification coordinator. Each runtime regression is
    /// independently valid; this class only authors the requested coverage, starts
    /// Play Mode, observes terminal evidence and restores the canonical baseline.
    /// </summary>
    internal static class QaCamera028DPlayerInputLayoutCertification
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run CAMERA-028-D PlayerInput Layout Certification";
        private const string Prefix = "[CAMERA-028-D-CERTIFICATION]";
        private const string PhaseKey =
            "ImmersiveFrameworkQA.CAMERA_028_D.Phase";
        private const string FailureKey =
            "ImmersiveFrameworkQA.CAMERA_028_D.Failure";
        private const string FailurePhaseKey =
            "ImmersiveFrameworkQA.CAMERA_028_D.FailurePhase";
        private const string PositiveCasesKey =
            "ImmersiveFrameworkQA.CAMERA_028_D.PositiveCases";
        private const string NegativeCasesKey =
            "ImmersiveFrameworkQA.CAMERA_028_D.NegativeCases";
        private const double TimeoutSeconds = 300d;

        private static double startedAt;
        private static bool watching;

        private enum Phase
        {
            Idle = 0,
            PositiveRunning = 10,
            PositivePassed = 20,
            NegativeRunning = 30,
            NegativePassed = 40,
            Failed = 50,
            Completed = 60
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            CurrentPhase is Phase.Idle or Phase.Failed or Phase.Completed;

        [MenuItem(MenuPath, priority = 233)]
        private static void Run()
        {
            StopWatching();
            SessionState.EraseString(FailureKey);
            SessionState.EraseString(FailurePhaseKey);
            SessionState.EraseInt(PositiveCasesKey);
            SessionState.EraseInt(NegativeCasesKey);

            try
            {
                Prepare(completeSlotCoverage: true);
                SetPhase(Phase.PositiveRunning);
                Debug.Log(
                    $"{Prefix} status='Running' phase='CompleteCoverage' " +
                    "mapping='player.1->OutputB,player.2->OutputA' cycle='0->1->2->1'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "prepare-complete-coverage",
                    exception.GetBaseException().Message);
                FinishFailure();
            }
        }

        private static void Prepare(bool completeSlotCoverage)
        {
            QaCameraPersistentBaselineGuard.PrepareAndVerify(
                QaCameraAdr026TopologyMode.Split);
            QaCameraPersistentTopologyBuilder
                .ConfigurePlayerInputLayoutIntegration(completeSlotCoverage);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode &&
                CurrentPhase is Phase.PositiveRunning or Phase.NegativeRunning)
            {
                BeginWatching();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            StopWatching();
            if (CurrentPhase == Phase.PositivePassed)
            {
                EditorApplication.delayCall -= BeginNegativePhase;
                EditorApplication.delayCall += BeginNegativePhase;
            }
            else if (CurrentPhase == Phase.NegativePassed)
            {
                EditorApplication.delayCall -= FinishSuccess;
                EditorApplication.delayCall += FinishSuccess;
            }
            else if (CurrentPhase is Phase.PositiveRunning or Phase.NegativeRunning)
            {
                RecordFailure(
                    "play-mode-interrupted",
                    "Play Mode exited before the current CAMERA-028-D phase completed.");
                EditorApplication.delayCall -= FinishFailure;
                EditorApplication.delayCall += FinishFailure;
            }
            else if (CurrentPhase == Phase.Failed)
            {
                EditorApplication.delayCall -= FinishFailure;
                EditorApplication.delayCall += FinishFailure;
            }
        }

        private static void BeginNegativePhase()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                CurrentPhase != Phase.PositivePassed)
            {
                return;
            }

            try
            {
                Prepare(completeSlotCoverage: false);
                SetPhase(Phase.NegativeRunning);
                Debug.Log(
                    $"{Prefix} status='Running' phase='IncompleteCoverage' " +
                    "mapping='player.1->OutputB,player.2->missing' expected='ExplicitBootRejection'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "prepare-incomplete-coverage",
                    exception.GetBaseException().Message);
                FinishFailure();
            }
        }

        private static void BeginWatching()
        {
            StopWatching();
            watching = true;
            startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!watching || !EditorApplication.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                FailCurrentPhase(
                    $"Runtime regression did not reach terminal evidence within '{TimeoutSeconds}' seconds.");
                return;
            }

            Phase phase = CurrentPhase;
            QaCamera028DPlayerInputLayoutMode expectedMode =
                phase == Phase.PositiveRunning
                    ? QaCamera028DPlayerInputLayoutMode.CompleteCoverage
                    : QaCamera028DPlayerInputLayoutMode.IncompleteCoverage;
            if (!QaCamera028DPlayerInputLayoutRegression.Executed ||
                QaCamera028DPlayerInputLayoutRegression.ExecutedMode != expectedMode)
            {
                return;
            }

            if (!QaCamera028DPlayerInputLayoutRegression.Passed)
            {
                FailCurrentPhase(
                    QaCamera028DPlayerInputLayoutRegression.Diagnostic);
                return;
            }

            if (phase == Phase.PositiveRunning)
            {
                SessionState.SetInt(
                    PositiveCasesKey,
                    QaCamera028DPlayerInputLayoutRegression.CompletedCaseCount);
                SetPhase(Phase.PositivePassed);
            }
            else if (phase == Phase.NegativeRunning)
            {
                SessionState.SetInt(
                    NegativeCasesKey,
                    QaCamera028DPlayerInputLayoutRegression.CompletedCaseCount);
                SetPhase(Phase.NegativePassed);
            }
            else
            {
                return;
            }

            StopWatching();
            EditorApplication.isPlaying = false;
        }

        private static void FailCurrentPhase(string reason)
        {
            string stage = CurrentPhase == Phase.PositiveRunning
                ? "complete-coverage-runtime"
                : "incomplete-coverage-runtime";
            RecordFailure(stage, reason);
            StopWatching();
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void FinishSuccess()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                CurrentPhase != Phase.NegativePassed)
            {
                return;
            }

            try
            {
                QaCameraPersistentBaselineGuard.RestoreCanonicalBaseline();
                int positiveCases = SessionState.GetInt(PositiveCasesKey, 0);
                int negativeCases = SessionState.GetInt(NegativeCasesKey, 0);
                SetPhase(Phase.Completed);
                Debug.Log(
                    $"{Prefix} status='Passed' verdict='READY_FOR_MANUAL_REVIEW' " +
                    $"cases='{positiveCases + negativeCases}/{positiveCases + negativeCases}' " +
                    "next='<none>' " +
                    "completed='CompleteCoverage,ZeroToOneToTwoToOne,IncompleteCoverage,CanonicalRestore' " +
                    "missing='' execution='' unwind='' cleanup='CanonicalSharedRestored'.");
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "canonical-restore",
                    exception.GetBaseException().Message);
                FinishFailure();
            }
        }

        private static void FinishFailure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            string failure = SessionState.GetString(FailureKey, string.Empty);
            string failurePhase =
                SessionState.GetString(FailurePhaseKey, "unknown");
            string cleanup = "CanonicalRestoreFailed";
            string cleanupFailure = string.Empty;
            try
            {
                QaCameraPersistentBaselineGuard.RestoreCanonicalBaseline();
                cleanup = "CanonicalSharedRestored";
            }
            catch (Exception restoreException)
            {
                cleanupFailure = restoreException.GetBaseException().Message;
                cleanup = "CanonicalRestoreFailed:" + cleanupFailure;
                if (string.IsNullOrEmpty(failure))
                {
                    failure = cleanupFailure;
                    failurePhase = "canonical-restore";
                }
            }

            SetPhase(Phase.Failed);
            Debug.LogError(
                $"{Prefix} status='Failed' verdict='CAMERA_028_D_FAIL' " +
                $"stage='{Escape(failurePhase)}' next='{Escape(failurePhase)}' " +
                $"missing='{Escape(failure)}' execution='{Escape(failure)}' " +
                $"unwind='' cleanup='{Escape(cleanup)}'.");
        }

        private static void RecordFailure(string stage, string reason)
        {
            SessionState.SetString(FailurePhaseKey, stage ?? string.Empty);
            SessionState.SetString(FailureKey, reason ?? string.Empty);
            SetPhase(Phase.Failed);
        }

        private static void StopWatching()
        {
            EditorApplication.update -= Tick;
            watching = false;
            startedAt = 0d;
        }

        private static Phase CurrentPhase =>
            (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);

        private static void SetPhase(Phase phase) =>
            SessionState.SetInt(PhaseKey, (int)phase);

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
    }
}

using System;
using System.Threading.Tasks;
using ImmersiveFrameworkQA.GameFlow.Internal.Editor;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Canonical one-button Player QA entry point. It coordinates existing
    /// isolated fixtures and regressions; it does not duplicate their proofs.
    /// </summary>
    public static class QaPlayerFullCertificationOrchestrator
    {
        private const string Prefix = "[QA_PLAYER_FULL]";
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run Full Player QA";
        private const string PhaseKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Phase";
        private const string SerializationKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Serialization";
        private const string SessionKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Session";
        private const string SceneProvidedKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.SceneProvided";
        private const string ManagerProvisionedKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.ManagerProvisioned";
        private const string ActorKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Actor";
        private const string PublicSurfaceKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.PublicSurface";
        private const string ParticipationKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Participation";
        private const string FailedPhaseKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.FailedPhase";
        private const string ErrorKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Error";
        private const string SummaryEmittedKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.SummaryEmitted";

        private enum Phase
        {
            Idle = 0,
            Preparing = 1,
            RunningSession = 2,
            PreparingSceneProvided = 3,
            RunningSceneProvided = 4,
            SceneProvidedCompleted = 5,
            PreparingManagerProvisioned = 6,
            RunningManagerProvisioned = 7,
            ManagerProvisionedCompleted = 8,
            PreparingActor = 9,
            RunningActor = 10,
            ActorSelectionCompleted = 11,
            RunningActorGameplay = 12,
            ActorCompleted = 13,
            PreparingPublicSurface = 14,
            RunningPublicSurfacePositive = 15,
            PublicSurfacePositiveCompleted = 16,
            RunningPublicSurfaceNegative = 17,
            PublicSurfaceCompleted = 18,
            PreparingParticipation = 19,
            RunningParticipation = 20,
            ParticipationCompleted = 21,
            Passed = 22,
            Failed = 23,
            RunningSerialization = 24
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeHook()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            StartFullPlayerQa();
        }

        /// <summary>
        /// Typed Edit Mode entry point for the complete Player QA sequence.
        /// </summary>
        public static void StartFullPlayerQa()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Full Player QA must start in Edit Mode.");
            }

            ResetCertificationState();
            try
            {
                SetPhase(Phase.Preparing);

                SetPhase(Phase.RunningSerialization);
                if (!QaPlayerSerializationIdentityRegression.Execute(out string serializationError))
                {
                    throw new InvalidOperationException(
                        $"Player serialization identity regression failed: {serializationError}");
                }
                MarkPassed(SerializationKey);

                SetPhase(Phase.RunningSession);
                QaPlayerParticipationAuthoringRegression.Run();
                MarkPassed(SessionKey);

                SetPhase(Phase.PreparingSceneProvided);
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();

                SetPhase(Phase.RunningSceneProvided);
                EnterFreshPlayMode();
            }
            catch (Exception exception)
            {
                Fail(CurrentPhaseLabel(), exception);
                EmitFailureSummary();
                throw;
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            Phase phase = CurrentPhase;
            if (state == PlayModeStateChange.EnteredPlayMode && IsPlayPhase(phase))
            {
                _ = RunPlayPhaseAsync(phase);
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            if (phase == Phase.Failed)
            {
                EmitFailureSummary();
                return;
            }

            if (phase == Phase.ParticipationCompleted)
            {
                CompleteSuccess();
                return;
            }

            if (IsPlayPhase(phase))
            {
                Fail(
                    PhaseLabel(phase),
                    new InvalidOperationException(
                        "Full Player QA Play Mode session ended before its phase completed."));
                EmitFailureSummary();
                return;
            }

            if (IsCompletedPlayPhase(phase))
            {
                EditorApplication.delayCall += AdvanceFromCompletedPlayPhase;
            }
        }

        private static async Task RunPlayPhaseAsync(Phase phase)
        {
            try
            {
                await Task.Yield();
                switch (phase)
                {
                    case Phase.RunningSceneProvided:
                        await QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync();
                        SetPhase(Phase.SceneProvidedCompleted);
                        MarkPassed(SceneProvidedKey);
                        break;

                    case Phase.RunningManagerProvisioned:
                        await QaManagerProvisionedLifecycleWaitingProjectionRegression
                            .RunForFullPlayerQaAsync();
                        SetPhase(Phase.ManagerProvisionedCompleted);
                        MarkPassed(ManagerProvisionedKey);
                        break;

                    case Phase.RunningActor:
                        await QaPlayerActorSelectionRuntimeBindingRegression
                            .RunRegressionAsync();
                        SetPhase(Phase.ActorSelectionCompleted);
                        break;

                    case Phase.RunningActorGameplay:
                        await QaPlayerGameplayAdmissionRegression
                            .RunRegressionAsync();
                        SetPhase(Phase.ActorCompleted);
                        MarkPassed(ActorKey);
                        break;

                    case Phase.RunningPublicSurfacePositive:
                        await QaPlayerProvisioningPublicSurfaceRegression
                            .RunCertificationAsync();
                        SetPhase(Phase.PublicSurfacePositiveCompleted);
                        break;

                    case Phase.RunningPublicSurfaceNegative:
                        await QaPlayerProvisioningPublicSurfaceNegativeRegression
                            .RunCertificationAsync();
                        SetPhase(Phase.PublicSurfaceCompleted);
                        MarkPassed(PublicSurfaceKey);
                        break;

                    case Phase.RunningParticipation:
                        await QaM07ActivitySessionLifecycleProjectionRegression
                            .RunForFullPlayerQaAsync();
                        SetPhase(Phase.ParticipationCompleted);
                        MarkPassed(ParticipationKey);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Full Player QA cannot execute phase '{phase}'.");
                }

                ExitPlayMode();
            }
            catch (Exception exception)
            {
                Fail(PhaseLabel(phase), exception);
                ExitPlayMode();
            }
        }

        private static void AdvanceFromCompletedPlayPhase()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            try
            {
                switch (CurrentPhase)
                {
                    case Phase.SceneProvidedCompleted:
                        SetPhase(Phase.PreparingManagerProvisioned);
                        QaM07InternalReconcileSetup.PrepareForFullPlayerQa();
                        QaManagerProvisionedLifecyclePublicContractRegression.Run();
                        SetPhase(Phase.RunningManagerProvisioned);
                        EnterFreshPlayMode();
                        break;

                    case Phase.ManagerProvisionedCompleted:
                        SetPhase(Phase.PreparingActor);
                        QaManagerProvisionedPlayerFixture.PrepareAndValidate();
                        SetPhase(Phase.RunningActor);
                        EnterFreshPlayMode();
                        break;

                    case Phase.ActorSelectionCompleted:
                        SetPhase(Phase.PreparingActor);
                        QaManagerProvisionedPlayerFixture.PrepareAndValidate();
                        SetPhase(Phase.RunningActorGameplay);
                        EnterFreshPlayMode();
                        break;

                    case Phase.ActorCompleted:
                        SetPhase(Phase.PreparingPublicSurface);
                        QaPlayerSurfaceCertificationOrchestrator.PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningPublicSurfacePositive);
                        EnterFreshPlayMode();
                        break;

                    case Phase.PublicSurfacePositiveCompleted:
                        SetPhase(Phase.PreparingPublicSurface);
                        QaPlayerSurfaceCertificationOrchestrator.PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningPublicSurfaceNegative);
                        EnterFreshPlayMode();
                        break;

                    case Phase.PublicSurfaceCompleted:
                        SetPhase(Phase.PreparingParticipation);
                        QaM07InternalReconcileSetup.PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningParticipation);
                        EnterFreshPlayMode();
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(CurrentPhaseLabel(), exception);
                EmitFailureSummary();
            }
        }

        private static void ResetCertificationState()
        {
            SetPhase(Phase.Idle);
            SessionState.SetString(SerializationKey, "NOT RUN");
            SessionState.SetString(SessionKey, "NOT RUN");
            SessionState.SetString(SceneProvidedKey, "NOT RUN");
            SessionState.SetString(ManagerProvisionedKey, "NOT RUN");
            SessionState.SetString(ActorKey, "NOT RUN");
            SessionState.SetString(PublicSurfaceKey, "NOT RUN");
            SessionState.SetString(ParticipationKey, "NOT RUN");
            SessionState.EraseString(FailedPhaseKey);
            SessionState.EraseString(ErrorKey);
            SessionState.EraseBool(SummaryEmittedKey);
        }

        private static void CompleteSuccess()
        {
            SetPhase(Phase.Passed);
            if (SessionState.GetBool(SummaryEmittedKey, false))
            {
                return;
            }

            SessionState.SetBool(SummaryEmittedKey, true);
            Debug.Log(
                $"{Prefix} status='Passed' verdict='PLAYER QA CERTIFIED' " +
                $"serialization='{Result(SerializationKey)}' " +
                $"session='{Result(SessionKey)}' " +
                $"sceneProvided='{Result(SceneProvidedKey)}' " +
                $"managerProvisioned='{Result(ManagerProvisionedKey)}' " +
                $"actor='{Result(ActorKey)}' " +
                $"publicSurface='{Result(PublicSurfaceKey)}' " +
                $"participation='{Result(ParticipationKey)}'.");
        }

        private static void Fail(string failedPhase, Exception exception)
        {
            SetPhase(Phase.Failed);
            SessionState.SetString(FailedPhaseKey, failedPhase);
            SessionState.SetString(
                ErrorKey,
                exception != null ? exception.Message : "Unknown failure.");
            MarkFailedForPhase(failedPhase);
        }

        private static void EmitFailureSummary()
        {
            if (SessionState.GetBool(SummaryEmittedKey, false))
            {
                return;
            }

            SessionState.SetBool(SummaryEmittedKey, true);
            Debug.LogError(
                $"{Prefix} status='Failed' verdict='PLAYER QA NOT CERTIFIED' " +
                $"failedPhase='{SessionState.GetString(FailedPhaseKey, "Unknown")}' " +
                $"message='{Escape(SessionState.GetString(ErrorKey, string.Empty))}' " +
                $"serialization='{Result(SerializationKey)}' " +
                $"session='{Result(SessionKey)}' " +
                $"sceneProvided='{Result(SceneProvidedKey)}' " +
                $"managerProvisioned='{Result(ManagerProvisionedKey)}' " +
                $"actor='{Result(ActorKey)}' " +
                $"publicSurface='{Result(PublicSurfaceKey)}' " +
                $"participation='{Result(ParticipationKey)}'.");
        }

        private static void MarkFailedForPhase(string phase)
        {
            switch (phase)
            {
                case "Serialization Identity":
                    SessionState.SetString(SerializationKey, "FAIL");
                    break;
                case "Session":
                    SessionState.SetString(SessionKey, "FAIL");
                    break;
                case "Scene Provided":
                    SessionState.SetString(SceneProvidedKey, "FAIL");
                    break;
                case "Manager Provisioned":
                    SessionState.SetString(ManagerProvisionedKey, "FAIL");
                    break;
                case "Actor Lifecycle":
                    SessionState.SetString(ActorKey, "FAIL");
                    break;
                case "Public Surface":
                    SessionState.SetString(PublicSurfaceKey, "FAIL");
                    break;
                case "Participation Integration":
                    SessionState.SetString(ParticipationKey, "FAIL");
                    break;
            }
        }

        private static void EnterFreshPlayMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Full Player QA cannot enter a fresh Play Mode session while Unity is changing Play Mode.");
            }

            EditorApplication.isPlaying = true;
        }

        private static void ExitPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static bool IsPlayPhase(Phase phase)
        {
            return phase is Phase.RunningSceneProvided or
                Phase.RunningManagerProvisioned or
                Phase.RunningActor or
                Phase.RunningActorGameplay or
                Phase.RunningPublicSurfacePositive or
                Phase.RunningPublicSurfaceNegative or
                Phase.RunningParticipation;
        }

        private static bool IsCompletedPlayPhase(Phase phase)
        {
            return phase is Phase.SceneProvidedCompleted or
                Phase.ManagerProvisionedCompleted or
                Phase.ActorSelectionCompleted or
                Phase.ActorCompleted or
                Phase.PublicSurfacePositiveCompleted or
                Phase.PublicSurfaceCompleted;
        }

        private static string CurrentPhaseLabel() => PhaseLabel(CurrentPhase);

        private static string PhaseLabel(Phase phase)
        {
            return phase switch
            {
                Phase.RunningSerialization => "Serialization Identity",
                Phase.RunningSession => "Session",
                Phase.PreparingSceneProvided or Phase.RunningSceneProvided or
                    Phase.SceneProvidedCompleted => "Scene Provided",
                Phase.PreparingManagerProvisioned or
                    Phase.RunningManagerProvisioned or
                    Phase.ManagerProvisionedCompleted => "Manager Provisioned",
                Phase.PreparingActor or Phase.RunningActor or
                    Phase.ActorSelectionCompleted or Phase.RunningActorGameplay or
                    Phase.ActorCompleted => "Actor Lifecycle",
                Phase.PreparingPublicSurface or
                    Phase.RunningPublicSurfacePositive or
                    Phase.PublicSurfacePositiveCompleted or
                    Phase.RunningPublicSurfaceNegative or
                    Phase.PublicSurfaceCompleted => "Public Surface",
                Phase.PreparingParticipation or Phase.RunningParticipation or
                    Phase.ParticipationCompleted => "Participation Integration",
                _ => "Preparation"
            };
        }

        private static Phase CurrentPhase =>
            (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);

        private static void SetPhase(Phase phase)
        {
            SessionState.SetInt(PhaseKey, (int)phase);
        }

        private static void MarkPassed(string key)
        {
            SessionState.SetString(key, "PASS");
        }

        private static string Result(string key) =>
            SessionState.GetString(key, "NOT RUN");

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}

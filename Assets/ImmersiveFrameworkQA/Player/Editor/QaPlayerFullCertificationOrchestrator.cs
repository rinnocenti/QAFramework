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
        private const string SceneProvidedLeaveKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.SceneProvidedLeave";
        private const string SceneProvidedNoActivityLeaveKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.SceneProvidedNoActivityLeave";
        private const string SceneProvidedNoActivityTerminationKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.SceneProvidedNoActivityTermination";
        private const string ManagerProvisionedKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.ManagerProvisioned";
        private const string ManagerNoActivityKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.ManagerNoActivity";
        private const string ManagerSessionTerminationKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.ManagerSessionTermination";
        private const string ActorKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Actor";
        private const string PublicSurfaceKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.PublicSurface";
        private const string SessionLifetimeKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.SessionLifetime";
        private const string PlacementKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Placement";
        private const string LeaveKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Leave";
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
            ActorCompleted = 11,
            PreparingManagerNoActivity = 12,
            RunningManagerNoActivity = 13,
            ManagerNoActivityCompleted = 14,
            PreparingPublicSurface = 15,
            RunningPublicSurfacePositive = 16,
            PublicSurfacePositiveCompleted = 17,
            RunningPublicSurfaceNegative = 18,
            PublicSurfaceCompleted = 19,
            PreparingLeave = 20,
            RunningLeave = 21,
            LeaveCompleted = 22,
            PreparingParticipation = 23,
            RunningParticipation = 24,
            ParticipationCompleted = 25,
            Passed = 26,
            Failed = 27,
            RunningSerialization = 28,
            PreparingSceneProvidedLeave = 29,
            RunningSceneProvidedLeave = 30,
            SceneProvidedLeaveCompleted = 31,
            PreparingSceneProvidedNoActivityLeave = 32,
            RunningSceneProvidedNoActivityLeave = 33,
            SceneProvidedNoActivityLeaveCompleted = 34,
            PreparingSceneProvidedNoActivityTermination = 35,
            RunningSceneProvidedNoActivityTermination = 36,
            SceneProvidedNoActivityTerminationCompleted = 37,
            PreparingManagerSessionTermination = 38,
            RunningManagerSessionTermination = 39,
            ManagerSessionTerminationCompleted = 40
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

                if (!QaAdr21ActivityPlayerInitialPlacementRegression.Execute(
                        out string placementError))
                {
                    throw new InvalidOperationException(
                        "ADR-021 initial placement regression failed: " + placementError);
                }
                MarkPassed(PlacementKey);

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

                    case Phase.RunningSceneProvidedLeave:
                        await QaP3M5BRouteTransitionAndNegativeMatrixSmoke
                            .RunSceneLeaveWithActivityAsync();
                        SetPhase(Phase.SceneProvidedLeaveCompleted);
                        MarkPassed(SceneProvidedLeaveKey);
                        break;

                    case Phase.RunningSceneProvidedNoActivityLeave:
                        await QaP3M5BRouteTransitionAndNegativeMatrixSmoke
                            .RunSceneLeaveWithoutActivityAsync();
                        SetPhase(Phase.SceneProvidedNoActivityLeaveCompleted);
                        MarkPassed(SceneProvidedNoActivityLeaveKey);
                        break;

                    case Phase.RunningSceneProvidedNoActivityTermination:
                        await QaP3M5BRouteTransitionAndNegativeMatrixSmoke
                            .RunSceneSessionTerminationWithoutActivityAsync();
                        SetPhase(Phase.SceneProvidedNoActivityTerminationCompleted);
                        MarkPassed(SceneProvidedNoActivityTerminationKey);
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
                        SetPhase(Phase.ActorCompleted);
                        MarkPassed(ActorKey);
                        break;

                    case Phase.RunningManagerNoActivity:
                        await QaPlayerProvisioningPublicSurfaceRegression
                            .RunNoActivityJoinAsync();
                        SetPhase(Phase.ManagerNoActivityCompleted);
                        MarkPassed(ManagerNoActivityKey);
                        break;

                    case Phase.RunningManagerSessionTermination:
                        await QaPlayerProvisioningPublicSurfaceRegression
                            .RunManagerSessionTerminationAsync();
                        SetPhase(Phase.ManagerSessionTerminationCompleted);
                        MarkPassed(ManagerSessionTerminationKey);
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

                    case Phase.RunningLeave:
                        await QaSessionPlayerLeavePublicManagerRegression
                            .RunCertificationAsync();
                        SetPhase(Phase.LeaveCompleted);
                        MarkPassed(LeaveKey);
                        break;

                    case Phase.RunningParticipation:
                        await QaP3M5BRouteTransitionAndNegativeMatrixSmoke
                            .RunSceneSessionTerminationWithoutActivityAsync();
                        SetPhase(Phase.ParticipationCompleted);
                        MarkPassed(SessionLifetimeKey);
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
                        SetPhase(Phase.PreparingSceneProvidedLeave);
                        QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();
                        SetPhase(Phase.RunningSceneProvidedLeave);
                        EnterFreshPlayMode();
                        break;

                    case Phase.SceneProvidedLeaveCompleted:
                        SetPhase(Phase.PreparingSceneProvidedNoActivityLeave);
                        QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();
                        SetPhase(Phase.RunningSceneProvidedNoActivityLeave);
                        EnterFreshPlayMode();
                        break;

                    case Phase.SceneProvidedNoActivityLeaveCompleted:
                        SetPhase(Phase.PreparingSceneProvidedNoActivityTermination);
                        QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();
                        SetPhase(Phase.RunningSceneProvidedNoActivityTermination);
                        EnterFreshPlayMode();
                        break;

                    case Phase.SceneProvidedNoActivityTerminationCompleted:
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

                    case Phase.ActorCompleted:
                        SetPhase(Phase.PreparingManagerNoActivity);
                        QaPlayerSurfaceCertificationOrchestrator.PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningManagerNoActivity);
                        EnterFreshPlayMode();
                        break;

                    case Phase.ManagerNoActivityCompleted:
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
                        SetPhase(Phase.PreparingLeave);
                        QaPlayerSurfaceCertificationOrchestrator.PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningLeave);
                        EnterFreshPlayMode();
                        break;

                    case Phase.LeaveCompleted:
                        SetPhase(Phase.PreparingManagerSessionTermination);
                        QaPlayerSurfaceCertificationOrchestrator.PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningManagerSessionTermination);
                        EnterFreshPlayMode();
                        break;

                    case Phase.ManagerSessionTerminationCompleted:
                        SetPhase(Phase.PreparingParticipation);
                        QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();
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
            SessionState.SetString(SceneProvidedLeaveKey, "NOT RUN");
            SessionState.SetString(SceneProvidedNoActivityLeaveKey, "NOT RUN");
            SessionState.SetString(SceneProvidedNoActivityTerminationKey, "NOT RUN");
            SessionState.SetString(ManagerProvisionedKey, "NOT RUN");
            SessionState.SetString(ManagerNoActivityKey, "NOT RUN");
            SessionState.SetString(ManagerSessionTerminationKey, "NOT RUN");
            SessionState.SetString(ActorKey, "NOT RUN");
            SessionState.SetString(PublicSurfaceKey, "NOT RUN");
            SessionState.SetString(LeaveKey, "NOT RUN");
            SessionState.SetString(SessionLifetimeKey, "NOT RUN");
            SessionState.SetString(PlacementKey, "NOT RUN");
            SessionState.EraseString(FailedPhaseKey);
            SessionState.EraseString(ErrorKey);
            SessionState.EraseBool(SummaryEmittedKey);
        }

        private static void CompleteSuccess()
        {
            if (!AllMandatoryPhasesPassed())
            {
                Fail(
                    "Certification Summary",
                    new InvalidOperationException(
                        "Full Player QA reached its terminal phase without every mandatory phase passing."));
                EmitFailureSummary();
                return;
            }

            SetPhase(Phase.Passed);
            if (SessionState.GetBool(SummaryEmittedKey, false))
            {
                return;
            }

            SessionState.SetBool(SummaryEmittedKey, true);
            Debug.Log(
                $"{Prefix} status='Completed' verdict='PLAYER QA CERTIFIED' " +
                $"serialization='{Result(SerializationKey)}' " +
                $"session='{Result(SessionKey)}' " +
                $"placement='{Result(PlacementKey)}' " +
                $"sceneProvided='{Result(SceneProvidedKey)}' " +
                $"sceneProvidedLeave='{Result(SceneProvidedLeaveKey)}' " +
                $"sceneProvidedNoActivityLeave='{Result(SceneProvidedNoActivityLeaveKey)}' " +
                $"sceneProvidedNoActivityTermination='{Result(SceneProvidedNoActivityTerminationKey)}' " +
                $"managerProvisioned='{Result(ManagerProvisionedKey)}' " +
                $"managerNoActivity='{Result(ManagerNoActivityKey)}' " +
                $"managerSessionTermination='{Result(ManagerSessionTerminationKey)}' " +
                $"actor='{Result(ActorKey)}' " +
                $"publicSurface='{Result(PublicSurfaceKey)}' " +
                $"leave='{Result(LeaveKey)}' " +
                $"sessionLifetime='{Result(SessionLifetimeKey)}'.");
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
                $"placement='{Result(PlacementKey)}' " +
                $"sceneProvided='{Result(SceneProvidedKey)}' " +
                $"sceneProvidedLeave='{Result(SceneProvidedLeaveKey)}' " +
                $"sceneProvidedNoActivityLeave='{Result(SceneProvidedNoActivityLeaveKey)}' " +
                $"sceneProvidedNoActivityTermination='{Result(SceneProvidedNoActivityTerminationKey)}' " +
                $"managerProvisioned='{Result(ManagerProvisionedKey)}' " +
                $"managerNoActivity='{Result(ManagerNoActivityKey)}' " +
                $"managerSessionTermination='{Result(ManagerSessionTerminationKey)}' " +
                $"actor='{Result(ActorKey)}' " +
                $"publicSurface='{Result(PublicSurfaceKey)}' " +
                $"leave='{Result(LeaveKey)}' " +
                $"sessionLifetime='{Result(SessionLifetimeKey)}'.");
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
                case "Scene Provided Leave":
                    SessionState.SetString(SceneProvidedLeaveKey, "FAIL");
                    break;
                case "Scene Provided Leave Without Activity":
                    SessionState.SetString(SceneProvidedNoActivityLeaveKey, "FAIL");
                    break;
                case "Scene Provided Session Termination Without Activity":
                    SessionState.SetString(SceneProvidedNoActivityTerminationKey, "FAIL");
                    break;
                case "Manager Provisioned":
                    SessionState.SetString(ManagerProvisionedKey, "FAIL");
                    break;
                case "Manager Join Without Activity":
                    SessionState.SetString(ManagerNoActivityKey, "FAIL");
                    break;
                case "Manager Session Termination":
                    SessionState.SetString(ManagerSessionTerminationKey, "FAIL");
                    break;
                case "Actor Lifecycle":
                    SessionState.SetString(ActorKey, "FAIL");
                    break;
                case "Public Surface":
                    SessionState.SetString(PublicSurfaceKey, "FAIL");
                    break;
                case "Session Player Leave":
                    SessionState.SetString(LeaveKey, "FAIL");
                    break;
                case "Participation Integration":
                case "Session Lifetime":
                    SessionState.SetString(SessionLifetimeKey, "FAIL");
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
                Phase.RunningSceneProvidedLeave or
                Phase.RunningSceneProvidedNoActivityLeave or
                Phase.RunningSceneProvidedNoActivityTermination or
                Phase.RunningManagerProvisioned or
                Phase.RunningActor or
                Phase.RunningManagerNoActivity or
                Phase.RunningManagerSessionTermination or
                Phase.RunningPublicSurfacePositive or
                Phase.RunningPublicSurfaceNegative or
                Phase.RunningLeave or
                Phase.RunningParticipation;
        }

        private static bool IsCompletedPlayPhase(Phase phase)
        {
            return phase is Phase.SceneProvidedCompleted or
                Phase.SceneProvidedLeaveCompleted or
                Phase.SceneProvidedNoActivityLeaveCompleted or
                Phase.SceneProvidedNoActivityTerminationCompleted or
                Phase.ManagerProvisionedCompleted or
                Phase.ActorCompleted or
                Phase.ManagerNoActivityCompleted or
                Phase.ManagerSessionTerminationCompleted or
                Phase.PublicSurfacePositiveCompleted or
                Phase.PublicSurfaceCompleted or
                Phase.LeaveCompleted;
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
                Phase.PreparingSceneProvidedLeave or Phase.RunningSceneProvidedLeave or
                    Phase.SceneProvidedLeaveCompleted => "Scene Provided Leave",
                Phase.PreparingSceneProvidedNoActivityLeave or
                    Phase.RunningSceneProvidedNoActivityLeave or
                    Phase.SceneProvidedNoActivityLeaveCompleted =>
                    "Scene Provided Leave Without Activity",
                Phase.PreparingSceneProvidedNoActivityTermination or
                    Phase.RunningSceneProvidedNoActivityTermination or
                    Phase.SceneProvidedNoActivityTerminationCompleted =>
                    "Scene Provided Session Termination Without Activity",
                Phase.PreparingManagerProvisioned or
                    Phase.RunningManagerProvisioned or
                    Phase.ManagerProvisionedCompleted => "Manager Provisioned",
                Phase.PreparingActor or Phase.RunningActor or Phase.ActorCompleted => "Actor Lifecycle",
                Phase.PreparingManagerNoActivity or Phase.RunningManagerNoActivity or
                    Phase.ManagerNoActivityCompleted => "Manager Join Without Activity",
                Phase.PreparingManagerSessionTermination or
                    Phase.RunningManagerSessionTermination or
                    Phase.ManagerSessionTerminationCompleted =>
                    "Manager Session Termination",
                Phase.PreparingPublicSurface or
                    Phase.RunningPublicSurfacePositive or
                    Phase.PublicSurfacePositiveCompleted or
                    Phase.RunningPublicSurfaceNegative or
                    Phase.PublicSurfaceCompleted => "Public Surface",
                Phase.PreparingLeave or Phase.RunningLeave or
                    Phase.LeaveCompleted => "Session Player Leave",
                Phase.PreparingParticipation or Phase.RunningParticipation or
                    Phase.ParticipationCompleted => "Session Lifetime",
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

        private static bool AllMandatoryPhasesPassed()
        {
            string[] keys =
            {
                SerializationKey,
                SessionKey,
                PlacementKey,
                SceneProvidedKey,
                SceneProvidedLeaveKey,
                SceneProvidedNoActivityLeaveKey,
                SceneProvidedNoActivityTerminationKey,
                ManagerProvisionedKey,
                ManagerNoActivityKey,
                ManagerSessionTerminationKey,
                ActorKey,
                PublicSurfaceKey,
                LeaveKey,
                SessionLifetimeKey
            };

            for (int index = 0; index < keys.Length; index++)
            {
                if (!string.Equals(Result(keys[index]), "PASS", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

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

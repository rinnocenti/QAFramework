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
        private const string LeaveUnresolvedKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.LeaveUnresolved";
        private const string ManagerNoActivityKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.ManagerNoActivity";
        private const string ManagerSessionTerminationKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.ManagerSessionTermination";
        private const string ActorKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Actor";
        private const string PublicSurfaceKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.PublicSurface";
        private const string SessionChangeObservationKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.SessionChangeObservation";
        private const string DesignerEventProjectionKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.DesignerEventProjection";
        private const string FailedFirstSceneAdoptionKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.FailedFirstSceneAdoption";
        private const string FailedContextualReprojectionKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.FailedContextualReprojection";
        private const string NoPhysicalHandoffKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.NoPhysicalHandoff";
        private const string PlacementKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.Placement";
        private const string RouteSpatialEntryKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.RouteSpatialEntry";
        private const string ActivityRelocationKey =
            "ImmersiveFrameworkQA.QA_PLAYER_FULL.ActivityRelocation";
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
            PreparingFailedFirstSceneAdoption = 23,
            RunningFailedFirstSceneAdoption = 24,
            FailedFirstSceneAdoptionCompleted = 25,
            PreparingFailedContextualReprojection = 26,
            RunningFailedContextualReprojection = 27,
            FailedContextualReprojectionCompleted = 28,
            PreparingNoPhysicalHandoff = 29,
            RunningNoPhysicalHandoff = 30,
            NoPhysicalHandoffCompleted = 31,
            Passed = 32,
            Failed = 33,
            RunningSerialization = 34,
            PreparingSceneProvidedLeave = 35,
            RunningSceneProvidedLeave = 36,
            SceneProvidedLeaveCompleted = 37,
            PreparingSceneProvidedNoActivityLeave = 38,
            RunningSceneProvidedNoActivityLeave = 39,
            SceneProvidedNoActivityLeaveCompleted = 40,
            PreparingSceneProvidedNoActivityTermination = 41,
            RunningSceneProvidedNoActivityTermination = 42,
            SceneProvidedNoActivityTerminationCompleted = 43,
            PreparingManagerSessionTermination = 44,
            RunningManagerSessionTermination = 45,
            ManagerSessionTerminationCompleted = 46,
            PreparingSessionChangeObservation = 47,
            RunningSessionChangeObservation = 48,
            SessionChangeObservationCompleted = 49,
            PreparingDesignerEventProjection = 50,
            RunningDesignerEventProjection = 51,
            DesignerEventProjectionCompleted = 52,
            PreparingLeaveUnresolved = 53,
            RunningLeaveUnresolved = 54,
            LeaveUnresolvedCompleted = 55
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
                if (!QaPlayerExplicitCommandSurfaceRegression.Execute(out string serializationError))
                {
                    throw new InvalidOperationException(
                        $"Player explicit command surface regression failed: {serializationError}");
                }
                MarkPassed(SerializationKey);

                SetPhase(Phase.RunningSession);
                QaPlayerParticipationAuthoringRegression.Run();
                MarkPassed(SessionKey);

                if (!QaAdr21RoutePlayerSpatialEntryRegression.Execute(
                        out string routeSpatialEntryError))
                {
                    throw new InvalidOperationException(
                        "ADR-021 Route Spatial Entry regression failed: " + routeSpatialEntryError);
                }
                MarkPassed(RouteSpatialEntryKey);

                if (!QaAdr21ActivityPlayerRelocationRegression.Execute(
                        out string relocationError))
                {
                    throw new InvalidOperationException(
                        "ADR-021 Activity Relocation regression failed: " + relocationError);
                }
                MarkPassed(ActivityRelocationKey);

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

            if (phase == Phase.NoPhysicalHandoffCompleted)
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

                    case Phase.RunningLeaveUnresolved:
                        await QaPlayerLeaveUnresolvedExplicitSelectionRegression
                            .RunForFullPlayerQaAsync();
                        SetPhase(Phase.LeaveUnresolvedCompleted);
                        MarkPassed(LeaveUnresolvedKey);
                        break;

                    case Phase.RunningActor:
                        await QaPlayerActorSelectionPublicSurfaceRegression
                            .RunCertificationAsync();
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

                    case Phase.RunningSessionChangeObservation:
                        await QaPlayerSessionChangeObservationRegression
                            .RunCertificationAsync();
                        SetPhase(Phase.SessionChangeObservationCompleted);
                        MarkPassed(SessionChangeObservationKey);
                        break;

                    case Phase.RunningDesignerEventProjection:
                        await QaPlayerSessionDesignerEventProjectionRegression
                            .RunCertificationAsync();
                        SetPhase(Phase.DesignerEventProjectionCompleted);
                        MarkPassed(DesignerEventProjectionKey);
                        break;

                    case Phase.RunningLeave:
                        await QaSessionPlayerLeavePublicManagerRegression
                            .RunCertificationAsync();
                        SetPhase(Phase.LeaveCompleted);
                        MarkPassed(LeaveKey);
                        break;

                    case Phase.RunningFailedFirstSceneAdoption:
                        await QaP3M5BRouteTransitionAndNegativeMatrixSmoke
                            .RunFailedFirstSceneAdoptionAsync();
                        SetPhase(Phase.FailedFirstSceneAdoptionCompleted);
                        MarkPassed(FailedFirstSceneAdoptionKey);
                        break;

                    case Phase.RunningFailedContextualReprojection:
                        await QaP3M5BRouteTransitionAndNegativeMatrixSmoke
                            .RunFailedContextualReprojectionAsync();
                        SetPhase(Phase.FailedContextualReprojectionCompleted);
                        MarkPassed(FailedContextualReprojectionKey);
                        break;

                    case Phase.RunningNoPhysicalHandoff:
                        await QaP3M5BRouteTransitionAndNegativeMatrixSmoke
                            .RunNoPhysicalHandoffOnActivityTransitionAsync();
                        SetPhase(Phase.NoPhysicalHandoffCompleted);
                        MarkPassed(NoPhysicalHandoffKey);
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
                        SetPhase(Phase.PreparingLeaveUnresolved);
                        QaPlayerLeaveUnresolvedExplicitSelectionSetup
                            .PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningLeaveUnresolved);
                        EnterFreshPlayMode();
                        break;

                    case Phase.LeaveUnresolvedCompleted:
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
                        SetPhase(Phase.PreparingSessionChangeObservation);
                        QaPlayerSurfaceCertificationOrchestrator.PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningSessionChangeObservation);
                        EnterFreshPlayMode();
                        break;

                    case Phase.SessionChangeObservationCompleted:
                        SetPhase(Phase.PreparingDesignerEventProjection);
                        QaPlayerSurfaceCertificationOrchestrator.PrepareForFullPlayerQa();
                        SetPhase(Phase.RunningDesignerEventProjection);
                        EnterFreshPlayMode();
                        break;

                    case Phase.DesignerEventProjectionCompleted:
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
                        SetPhase(Phase.PreparingFailedFirstSceneAdoption);
                        QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();
                        SetPhase(Phase.RunningFailedFirstSceneAdoption);
                        EnterFreshPlayMode();
                        break;

                    case Phase.FailedFirstSceneAdoptionCompleted:
                        SetPhase(Phase.PreparingFailedContextualReprojection);
                        QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();
                        SetPhase(Phase.RunningFailedContextualReprojection);
                        EnterFreshPlayMode();
                        break;

                    case Phase.FailedContextualReprojectionCompleted:
                        SetPhase(Phase.PreparingNoPhysicalHandoff);
                        QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();
                        SetPhase(Phase.RunningNoPhysicalHandoff);
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
            SessionState.SetString(LeaveUnresolvedKey, "NOT RUN");
            SessionState.SetString(ManagerNoActivityKey, "NOT RUN");
            SessionState.SetString(ManagerSessionTerminationKey, "NOT RUN");
            SessionState.SetString(ActorKey, "NOT RUN");
            SessionState.SetString(PublicSurfaceKey, "NOT RUN");
            SessionState.SetString(SessionChangeObservationKey, "NOT RUN");
            SessionState.SetString(DesignerEventProjectionKey, "NOT RUN");
            SessionState.SetString(LeaveKey, "NOT RUN");
            SessionState.SetString(FailedFirstSceneAdoptionKey, "NOT RUN");
            SessionState.SetString(FailedContextualReprojectionKey, "NOT RUN");
            SessionState.SetString(NoPhysicalHandoffKey, "NOT RUN");
            SessionState.SetString(PlacementKey, "NOT RUN");
            SessionState.SetString(RouteSpatialEntryKey, "NOT RUN");
            SessionState.SetString(ActivityRelocationKey, "NOT RUN");
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
                $"{Prefix} status='Completed' verdict='PLAYER CURRENT AGGREGATE COMPLETE' " +
                $"acceptedFullPlayer='{MandatoryContractCount}/{MandatoryContractCount}' " +
                $"serialization='{Result(SerializationKey)}' " +
                $"session='{Result(SessionKey)}' " +
                $"routeSpatialEntry='{Result(RouteSpatialEntryKey)}' " +
                $"activityRelocation='{Result(ActivityRelocationKey)}' " +
                $"historicalPlacement='{Result(PlacementKey)}' " +
                $"sceneProvided='{Result(SceneProvidedKey)}' " +
                $"sceneProvidedLeave='{Result(SceneProvidedLeaveKey)}' " +
                $"sceneProvidedNoActivityLeave='{Result(SceneProvidedNoActivityLeaveKey)}' " +
                $"sceneProvidedNoActivityTermination='{Result(SceneProvidedNoActivityTerminationKey)}' " +
                $"managerProvisioned='{Result(ManagerProvisionedKey)}' " +
                $"leaveUnresolved='{Result(LeaveUnresolvedKey)}' " +
                $"managerNoActivity='{Result(ManagerNoActivityKey)}' " +
                $"managerSessionTermination='{Result(ManagerSessionTerminationKey)}' " +
                $"actor='{Result(ActorKey)}' " +
                $"publicSurface='{Result(PublicSurfaceKey)}' " +
                $"sessionChangeObservation='{Result(SessionChangeObservationKey)}' " +
                $"designerEventProjection='{Result(DesignerEventProjectionKey)}' " +
                $"leave='{Result(LeaveKey)}' " +
                $"failedFirstSceneAdoption='{Result(FailedFirstSceneAdoptionKey)}' " +
                $"failedContextualReprojection='{Result(FailedContextualReprojectionKey)}' " +
                $"noPhysicalHandoff='{Result(NoPhysicalHandoffKey)}' " +
                $"mandatoryContracts='{MandatoryContractCount}' " +
                $"executedContracts='{ExecutedMandatoryContractCount}' " +
                $"passedContracts='{PassedMandatoryContractCount}'.");
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
                $"routeSpatialEntry='{Result(RouteSpatialEntryKey)}' " +
                $"activityRelocation='{Result(ActivityRelocationKey)}' " +
                $"historicalPlacement='{Result(PlacementKey)}' " +
                $"sceneProvided='{Result(SceneProvidedKey)}' " +
                $"sceneProvidedLeave='{Result(SceneProvidedLeaveKey)}' " +
                $"sceneProvidedNoActivityLeave='{Result(SceneProvidedNoActivityLeaveKey)}' " +
                $"sceneProvidedNoActivityTermination='{Result(SceneProvidedNoActivityTerminationKey)}' " +
                $"managerProvisioned='{Result(ManagerProvisionedKey)}' " +
                $"leaveUnresolved='{Result(LeaveUnresolvedKey)}' " +
                $"managerNoActivity='{Result(ManagerNoActivityKey)}' " +
                $"managerSessionTermination='{Result(ManagerSessionTerminationKey)}' " +
                $"actor='{Result(ActorKey)}' " +
                $"publicSurface='{Result(PublicSurfaceKey)}' " +
                $"sessionChangeObservation='{Result(SessionChangeObservationKey)}' " +
                $"designerEventProjection='{Result(DesignerEventProjectionKey)}' " +
                $"leave='{Result(LeaveKey)}' " +
                $"failedFirstSceneAdoption='{Result(FailedFirstSceneAdoptionKey)}' " +
                $"failedContextualReprojection='{Result(FailedContextualReprojectionKey)}' " +
                $"noPhysicalHandoff='{Result(NoPhysicalHandoffKey)}' " +
                $"mandatoryContracts='{MandatoryContractCount}' " +
                $"executedContracts='{ExecutedMandatoryContractCount}' " +
                $"passedContracts='{PassedMandatoryContractCount}'.");
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
                case "LeaveUnresolved Explicit Actor Selection":
                    SessionState.SetString(LeaveUnresolvedKey, "FAIL");
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
                case "Session Change Observation":
                    SessionState.SetString(SessionChangeObservationKey, "FAIL");
                    break;
                case "Designer-facing Player Session Event Projection":
                    SessionState.SetString(DesignerEventProjectionKey, "FAIL");
                    break;
                case "Session Player Leave":
                    SessionState.SetString(LeaveKey, "FAIL");
                    break;
                case "Failed First Scene Adoption":
                    SessionState.SetString(FailedFirstSceneAdoptionKey, "FAIL");
                    break;
                case "Failed Contextual Reprojection":
                    SessionState.SetString(FailedContextualReprojectionKey, "FAIL");
                    break;
                case "No Physical Handoff On Activity Transition":
                    SessionState.SetString(NoPhysicalHandoffKey, "FAIL");
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
                Phase.RunningLeaveUnresolved or
                Phase.RunningActor or
                Phase.RunningManagerNoActivity or
                Phase.RunningManagerSessionTermination or
                Phase.RunningPublicSurfacePositive or
                Phase.RunningPublicSurfaceNegative or
                Phase.RunningSessionChangeObservation or
                Phase.RunningDesignerEventProjection or
                Phase.RunningLeave or
                Phase.RunningFailedFirstSceneAdoption or
                Phase.RunningFailedContextualReprojection or
                Phase.RunningNoPhysicalHandoff;
        }

        private static bool IsCompletedPlayPhase(Phase phase)
        {
            return phase is Phase.SceneProvidedCompleted or
                Phase.SceneProvidedLeaveCompleted or
                Phase.SceneProvidedNoActivityLeaveCompleted or
                Phase.SceneProvidedNoActivityTerminationCompleted or
                Phase.ManagerProvisionedCompleted or
                Phase.LeaveUnresolvedCompleted or
                Phase.ActorCompleted or
                Phase.ManagerNoActivityCompleted or
                Phase.ManagerSessionTerminationCompleted or
                Phase.PublicSurfacePositiveCompleted or
                Phase.PublicSurfaceCompleted or
                Phase.SessionChangeObservationCompleted or
                Phase.DesignerEventProjectionCompleted or
                Phase.LeaveCompleted or
                Phase.FailedFirstSceneAdoptionCompleted or
                Phase.FailedContextualReprojectionCompleted;
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
                Phase.PreparingLeaveUnresolved or
                    Phase.RunningLeaveUnresolved or
                    Phase.LeaveUnresolvedCompleted =>
                    "LeaveUnresolved Explicit Actor Selection",
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
                Phase.PreparingSessionChangeObservation or
                Phase.RunningSessionChangeObservation or
                    Phase.SessionChangeObservationCompleted =>
                    "Session Change Observation",
                Phase.PreparingDesignerEventProjection or
                Phase.RunningDesignerEventProjection or
                    Phase.DesignerEventProjectionCompleted =>
                    "Designer-facing Player Session Event Projection",
                Phase.PreparingLeave or Phase.RunningLeave or
                    Phase.LeaveCompleted => "Session Player Leave",
                Phase.PreparingFailedFirstSceneAdoption or
                    Phase.RunningFailedFirstSceneAdoption or
                    Phase.FailedFirstSceneAdoptionCompleted =>
                    "Failed First Scene Adoption",
                Phase.PreparingFailedContextualReprojection or
                    Phase.RunningFailedContextualReprojection or
                    Phase.FailedContextualReprojectionCompleted =>
                    "Failed Contextual Reprojection",
                Phase.PreparingNoPhysicalHandoff or
                    Phase.RunningNoPhysicalHandoff or
                    Phase.NoPhysicalHandoffCompleted =>
                    "No Physical Handoff On Activity Transition",
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

        private readonly struct MandatoryContract
        {
            internal MandatoryContract(
                int id,
                string description,
                string owningQaCase,
                string resultKey)
            {
                Id = id;
                Description = description;
                OwningQaCase = owningQaCase;
                ResultKey = resultKey;
            }

            internal int Id { get; }
            internal string Description { get; }
            internal string OwningQaCase { get; }
            internal string ResultKey { get; }
            internal bool Mandatory => true;
        }

        // This is the certification authority, not a phase-count proxy. Multiple
        // contracts may be proved by one focused QA case, but every mandatory
        // contract has one explicit result source.
        private static readonly MandatoryContract[] MandatoryContracts =
        {
            new(1, "Manager Join without contextual assignment", "QaPlayerProvisioningPublicSurfaceRegression.RunNoActivityJoinAsync", ManagerNoActivityKey),
            new(2, "Manager first physical acquisition", "QaManagerProvisionedLifecycleWaitingProjectionRegression.RunForFullPlayerQaAsync", ManagerProvisionedKey),
            new(3, "Manager A -> B -> A physical continuity", "QaManagerProvisionedLifecycleWaitingProjectionRegression.RunForFullPlayerQaAsync", ManagerProvisionedKey),
            new(4, "SceneProvided exact original adoption", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(5, "SceneProvided A -> B -> A physical continuity", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(6, "Scene source unload retains physical", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(7, "Fresh contextual occurrence per Activity", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(8, "Previous contextual occurrence retired", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(9, "Activity -> none retains physical", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(10, "none -> Activity reuses physical", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(11, "Target Activity excludes Player without physical destruction", "QaPlayerProvisioningPublicSurfaceRegression.RunCertificationAsync", PublicSurfaceKey),
            new(12, "No implicit teleport across Activities", "QaAdr21ActivityPlayerRelocationRegression.Execute", ActivityRelocationKey),
            new(13, "Later SceneProvided candidate does not replace physical", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(14, "Failed first SceneProvided adoption does not steal candidate", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunFailedFirstSceneAdoptionAsync", FailedFirstSceneAdoptionKey),
            new(15, "Failed contextual reprojection retains committed physical", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunFailedContextualReprojectionAsync", FailedContextualReprojectionKey),
            new(16, "Manager Leave with Activity", "QaSessionPlayerLeavePublicManagerRegression.RunCertificationAsync", LeaveKey),
            new(17, "Manager Leave without Activity", "QaSessionPlayerLeavePublicManagerRegression.RunCertificationAsync", LeaveKey),
            new(18, "SceneProvided Leave with Activity", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunSceneLeaveWithActivityAsync", SceneProvidedLeaveKey),
            new(19, "SceneProvided Leave without Activity", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunSceneLeaveWithoutActivityAsync", SceneProvidedNoActivityLeaveKey),
            new(20, "Stale Leave rejected after rejoin", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunSceneLeaveWithoutActivityAsync", SceneProvidedNoActivityLeaveKey),
            new(21, "Manager Session termination", "QaPlayerProvisioningPublicSurfaceRegression.RunManagerSessionTerminationAsync", ManagerSessionTerminationKey),
            new(22, "SceneProvided Session termination", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunSceneSessionTerminationWithoutActivityAsync", SceneProvidedNoActivityTerminationKey),
            new(23, "Physical evidence stable between Activities", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(24, "Contextual input/gameplay evidence fresh", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunAsync", SceneProvidedKey),
            new(25, "Normal A -> B requires no physical candidate or handoff", "QaP3M5BRouteTransitionAndNegativeMatrixSmoke.RunNoPhysicalHandoffOnActivityTransitionAsync", NoPhysicalHandoffKey),
            new(26, "ADR-021 Model B Route Spatial Entry", "QaAdr21RoutePlayerSpatialEntryRegression.Execute", RouteSpatialEntryKey),
            new(27, "ADR-021 Model B Activity Explicit Relocation", "QaAdr21ActivityPlayerRelocationRegression.Execute", ActivityRelocationKey),
            new(28, "Session-scoped Player participation change observation", "QaPlayerSessionChangeObservationRegression.RunCertificationAsync", SessionChangeObservationKey),
            new(29, "Designer-facing Player Session event projection", "QaPlayerSessionDesignerEventProjectionRegression.RunCertificationAsync", DesignerEventProjectionKey),
            new(30, "LeaveUnresolved waits for explicit Actor selection", "QaPlayerLeaveUnresolvedExplicitSelectionRegression.RunForFullPlayerQaAsync", LeaveUnresolvedKey)
        };

        private static int MandatoryContractCount => MandatoryContracts.Length;

        private static int ExecutedMandatoryContractCount => CountMandatoryContracts(
            result => !string.Equals(result, "NOT RUN", StringComparison.Ordinal));

        private static int PassedMandatoryContractCount => CountMandatoryContracts(
            result => string.Equals(result, "PASS", StringComparison.Ordinal));

        private static bool AllMandatoryPhasesPassed() =>
            ExecutedMandatoryContractCount == MandatoryContractCount &&
            PassedMandatoryContractCount == MandatoryContractCount;

        private static int CountMandatoryContracts(Func<string, bool> predicate)
        {
            int count = 0;
            for (int index = 0; index < MandatoryContracts.Length; index++)
            {
                MandatoryContract contract = MandatoryContracts[index];
                if (contract.Mandatory && predicate(Result(contract.ResultKey)))
                {
                    count++;
                }
            }

            return count;
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

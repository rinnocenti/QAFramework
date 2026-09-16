using System;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.GameFlow.Internal.Editor;
using ImmersiveFrameworkQA.Player.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Coordinates the existing Camera rail across fresh Shared, Scene-Provided and Split Framework boots.
    /// Camera topology preparation is verified from disk and the shared QA baseline is
    /// restored after both successful and failed certification runs.
    /// </summary>
    internal static class QaCameraFullCertificationOrchestrator
    {
        private const string MenuPath = "Immersive Framework/QA/Camera/Run Full Camera QA";
        private const string Prefix = "[QA_CAMERA_FULL]";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string CanonicalScenePath =
            "Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_PlayerCameraArbitration.unity";
        private const string SceneProvidedScenePath =
            "Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerSceneProvided.unity";
        private const string CameraRouteTriggerName =
            "RouteTrigger_Camera__Override_Authority";
        private const string SceneProvidedRouteTriggerName =
            "RouteTrigger_Player_Scene-Provided";
        private const string PhaseKey = "ImmersiveFrameworkQA.QA_CAMERA_FULL.Phase";
        private const string FailureKey = "ImmersiveFrameworkQA.QA_CAMERA_FULL.Failure";
        private const double TimeoutSeconds = 180d;
        private const int EstablishedCaseCount = 11 + 18 + 10;

        private enum Phase
        {
            Idle = 0,
            RunningShared = 10,
            SharedPassed = 20,
            RunningSceneProvided = 21,
            SceneProvidedPassed = 22,
            RunningSplit = 30,
            Certified = 40,
            Failed = 50
        }

        private enum WatchStage
        {
            WaitingForHub,
            WaitingForFrameworkReady,
            WaitingForCameraSceneEnter,
            WaitingForCameraSceneExit,
            WaitingForSceneProvidedHub,
            WaitingForSceneProvidedFrameworkReady,
            WaitingForSceneProvidedEnter,
            WaitingForSceneProvidedExit
        }

        private static WatchStage watchStage;
        private static double startedAt;
        private static bool watching;
        private static RouteRequestTrigger pendingRouteTrigger;
        private static IDisposable pendingRouteRequestBinding;
        private static RouteRequestTriggerEvent pendingSceneProvidedRouteResult;
        private static string pendingReadinessStage = "framework-boot-unavailable";
        private static string pendingReadinessMessage =
            "Framework readiness has not been observed yet.";

        [InitializeOnLoadMethod]
        private static void RegisterHook()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying &&
            CurrentPhase is Phase.Idle or Phase.Certified or Phase.Failed;

        [MenuItem(MenuPath, priority = 230)]
        private static void Run()
        {
            SessionState.EraseString(FailureKey);
            try
            {
                // Shared topology preparation is also the canonical repository-wide
                // baseline repair. The guard reloads QA_UIGlobal from disk and refuses
                // to continue if Camera/Brain/DefaultRig references were not persisted.
                QaCameraPersistentBaselineGuard.PrepareAndVerify(
                    QaCameraAdr026TopologyMode.Shared);

                SetPhase(Phase.RunningShared);
                Debug.Log($"{Prefix} status='Running' phase='Shared' " +
                    $"expectedEstablishedCases='{EstablishedCaseCount}' adr026Phases='2'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail("prepare-shared", exception.GetBaseException().Message);
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            Phase phase = CurrentPhase;
            if (state == PlayModeStateChange.EnteredPlayMode &&
                phase is Phase.RunningShared or
                    Phase.RunningSceneProvided or
                    Phase.RunningSplit)
            {
                BeginWatching();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && phase == Phase.SharedPassed)
            {
                EditorApplication.delayCall += PrepareSceneProvidedPhase;
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode &&
                phase == Phase.SceneProvidedPassed)
            {
                EditorApplication.delayCall += PrepareSplitPhase;
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode &&
                phase is Phase.RunningShared or
                    Phase.RunningSceneProvided or
                    Phase.RunningSplit)
            {
                Fail(
                    "play-mode-interrupted",
                    $"Play Mode exited before Camera certification phase '{phase}' completed.");
            }
        }

        private static void BeginWatching()
        {
            StopWatching();
            watching = true;
            watchStage = CurrentPhase == Phase.RunningSceneProvided
                ? WatchStage.WaitingForSceneProvidedHub
                : WatchStage.WaitingForHub;
            startedAt = EditorApplication.timeSinceStartup;
            pendingRouteTrigger = null;
            pendingReadinessStage = "framework-boot-unavailable";
            pendingReadinessMessage =
                "QA Hub scene has not finished loading the canonical Camera Route trigger.";
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            try
            {
                TickCore();
            }
            catch (Exception exception)
            {
                Fail("runtime-orchestration", exception.GetBaseException().Message);
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                }
            }
        }

        private static void TickCore()
        {
            if (!watching || !EditorApplication.isPlaying)
            {
                StopWatching();
                return;
            }

            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                bool waitingOnReadiness = watchStage is
                    WatchStage.WaitingForHub or
                    WatchStage.WaitingForFrameworkReady or
                    WatchStage.WaitingForSceneProvidedHub or
                    WatchStage.WaitingForSceneProvidedFrameworkReady;
                string stage = waitingOnReadiness
                    ? pendingReadinessStage
                    : "runtime-timeout";
                string reason = waitingOnReadiness
                    ? pendingReadinessMessage
                    : DescribeEvidence();
                Fail(stage, reason);
                EditorApplication.isPlaying = false;
                return;
            }

            if (watchStage == WatchStage.WaitingForHub)
            {
                pendingReadinessStage = "framework-boot-unavailable";
                pendingReadinessMessage =
                    "QA Hub scene has not finished loading the canonical Camera Route trigger.";
                if (!TryResolveHubTrigger(
                        CameraRouteTriggerName,
                        "Camera",
                        out RouteRequestTrigger trigger))
                {
                    return;
                }

                pendingRouteTrigger = trigger;
                watchStage = WatchStage.WaitingForFrameworkReady;
                return;
            }

            if (watchStage == WatchStage.WaitingForFrameworkReady)
            {
                if (pendingRouteTrigger == null)
                {
                    watchStage = WatchStage.WaitingForHub;
                    return;
                }

                // The Camera Route can only be requested after the Framework has started
                // Game Flow with the Hub Activity ready and after the trigger has received
                // its canonical route-runtime port binding.
                if (!QaH2FrameworkReadiness.TryGetReady(out string frameworkDiagnostic))
                {
                    pendingReadinessStage = "framework-boot-unavailable";
                    pendingReadinessMessage =
                        "Framework has not finished starting Game Flow with a ready Hub Activity. " +
                        frameworkDiagnostic;
                    return;
                }

                if (!pendingRouteTrigger.HasRouteRuntimeBinding)
                {
                    pendingReadinessStage = "route-runtime-unavailable";
                    pendingReadinessMessage =
                        "Camera Route trigger has not been bound to the Game Flow route runtime port. " +
                        pendingRouteTrigger.RouteRuntimeBindingDiagnostic;
                    return;
                }

                Require(!pendingRouteTrigger.IsRequestInFlight,
                    "Camera Route trigger already has a request in flight.");
                pendingRouteTrigger.RequestRoute();
                watchStage = WatchStage.WaitingForCameraSceneEnter;
                return;
            }

            if (watchStage == WatchStage.WaitingForSceneProvidedHub)
            {
                pendingReadinessStage = "scene-provided-framework-boot-unavailable";
                pendingReadinessMessage =
                    "QA Hub scene has not finished loading the canonical Scene-Provided Route trigger.";
                if (!TryResolveHubTrigger(
                        SceneProvidedRouteTriggerName,
                        "Scene-Provided",
                        out RouteRequestTrigger trigger))
                {
                    return;
                }

                pendingRouteTrigger = trigger;
                watchStage = WatchStage.WaitingForSceneProvidedFrameworkReady;
                return;
            }

            if (watchStage == WatchStage.WaitingForSceneProvidedFrameworkReady)
            {
                if (pendingRouteTrigger == null)
                {
                    watchStage = WatchStage.WaitingForSceneProvidedHub;
                    return;
                }

                if (!QaH2FrameworkReadiness.TryGetReady(out string frameworkDiagnostic))
                {
                    pendingReadinessStage = "scene-provided-framework-boot-unavailable";
                    pendingReadinessMessage =
                        "Framework has not finished restoring the ready Hub Activity. " +
                        frameworkDiagnostic;
                    return;
                }

                if (!pendingRouteTrigger.HasRouteRuntimeBinding)
                {
                    pendingReadinessStage = "scene-provided-route-runtime-unavailable";
                    pendingReadinessMessage =
                        "Scene-Provided Route trigger has not been bound to the Game Flow route runtime port. " +
                        pendingRouteTrigger.RouteRuntimeBindingDiagnostic;
                    return;
                }

                Require(!pendingRouteTrigger.IsRequestInFlight,
                    "Scene-Provided Route trigger already has a request in flight.");
                pendingSceneProvidedRouteResult = null;
                pendingRouteRequestBinding?.Dispose();
                pendingRouteRequestBinding = pendingRouteTrigger.SubscribeRequestEvents(
                    HandleSceneProvidedRouteRequestEvent);
                pendingRouteTrigger.RequestRoute();
                watchStage = WatchStage.WaitingForSceneProvidedEnter;
                return;
            }

            if (watchStage is WatchStage.WaitingForSceneProvidedEnter or
                WatchStage.WaitingForSceneProvidedExit)
            {
                TickSceneProvidedPhase();
                return;
            }

            Scene cameraScene = SceneManager.GetSceneByPath(CanonicalScenePath);
            if (watchStage == WatchStage.WaitingForCameraSceneEnter)
            {
                if (cameraScene.IsValid() && cameraScene.isLoaded)
                {
                    watchStage = WatchStage.WaitingForCameraSceneExit;
                }
                return;
            }

            if (cameraScene.IsValid() && cameraScene.isLoaded)
            {
                if (CurrentPhase == Phase.RunningShared &&
                    QaCameraOverrideAuthorityFixture.Adr026SharedExecuted &&
                    !QaCameraOverrideAuthorityFixture.Adr026SharedPassed)
                {
                    throw new InvalidOperationException(
                        QaCameraOverrideAuthorityFixture.Adr026SharedDiagnostic);
                }

                if (CurrentPhase == Phase.RunningSplit &&
                    QaCameraOverrideAuthorityFixture.Adr026SplitExecuted &&
                    !QaCameraOverrideAuthorityFixture.Adr026SplitPassed)
                {
                    throw new InvalidOperationException(
                        QaCameraOverrideAuthorityFixture.Adr026SplitDiagnostic);
                }

                return;
            }

            try
            {
                if (CurrentPhase == Phase.RunningShared)
                {
                    CompleteSharedPhase();
                }
                else if (CurrentPhase == Phase.RunningSplit)
                {
                    CompleteSplitPhase();
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Unexpected Camera certification phase '{CurrentPhase}'.");
                }
            }
            catch (Exception exception)
            {
                Fail("runtime-evidence", exception.GetBaseException().Message);
                EditorApplication.isPlaying = false;
            }
        }

        private static void CompleteSharedPhase()
        {
            ValidateSharedPhaseEvidence();
            StopWatching();
            SetPhase(Phase.SharedPassed);
            Debug.Log($"{Prefix} status='SharedPassed' next='FreshSceneProvidedBoot'.");
            EditorApplication.isPlaying = false;
        }

        private static void TickSceneProvidedPhase()
        {
            Scene scene = SceneManager.GetSceneByPath(SceneProvidedScenePath);
            if (watchStage == WatchStage.WaitingForSceneProvidedEnter)
            {
                RouteRequestTriggerEvent routeResult =
                    pendingSceneProvidedRouteResult;
                if (routeResult == null)
                {
                    return;
                }

                pendingRouteRequestBinding?.Dispose();
                pendingRouteRequestBinding = null;

                if (!routeResult.Succeeded)
                {
                    Fail(
                        "scene-provided-route-request-failed",
                        routeResult.Message);
                    EditorApplication.isPlaying = false;
                    return;
                }

                if (!QaH2FrameworkReadiness.TryGetReady(
                        out string readinessDiagnostic))
                {
                    Fail(
                        "scene-provided-startup-not-ready",
                        routeResult.Message + " " + readinessDiagnostic);
                    EditorApplication.isPlaying = false;
                    return;
                }

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Fail(
                        "scene-provided-scene-unavailable",
                        routeResult.Message + " " + readinessDiagnostic);
                    EditorApplication.isPlaying = false;
                    return;
                }

                RequireSceneProvidedFixture(scene).Begin();
                watchStage = WatchStage.WaitingForSceneProvidedExit;
                return;
            }

            if (scene.IsValid() && scene.isLoaded)
            {
                if (QaCamera026ISceneProvidedFixture.Executed &&
                    !QaCamera026ISceneProvidedFixture.Passed)
                {
                    throw new InvalidOperationException(
                        QaCamera026ISceneProvidedFixture.Diagnostic);
                }
                return;
            }

            CompleteSceneProvidedPhase();
        }

        private static QaCamera026ISceneProvidedFixture RequireSceneProvidedFixture(
            Scene scene)
        {
            QaCamera026ISceneProvidedFixture resolved = null;
            int matches = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (QaCamera026ISceneProvidedFixture candidate in
                    root.GetComponentsInChildren<QaCamera026ISceneProvidedFixture>(true))
                {
                    if (candidate == null)
                    {
                        continue;
                    }

                    matches++;
                    resolved ??= candidate;
                }
            }

            Require(
                matches == 1,
                $"CAMERA-026-I requires exactly one Scene-Provided fixture, found '{matches}'.");
            return resolved;
        }

        private static void HandleSceneProvidedRouteRequestEvent(
            RouteRequestTriggerEvent routeEvent)
        {
            if (routeEvent == null ||
                !routeEvent.IsCompleted ||
                !ReferenceEquals(routeEvent.Trigger, pendingRouteTrigger))
            {
                return;
            }

            pendingSceneProvidedRouteResult = routeEvent;
        }

        private static void ValidateSharedPhaseEvidence()
        {
            Require(QaCameraOverrideAuthorityFixture.Adr026SharedExecuted &&
                    QaCameraOverrideAuthorityFixture.Adr026SharedPassed,
                "ADR-026 Shared Camera proof failed. " +
                QaCameraOverrideAuthorityFixture.Adr026SharedDiagnostic);
            Require(QaCameraOverrideAuthorityFixture.GenericArbitrationExecuted &&
                    QaCameraOverrideAuthorityFixture.GenericArbitrationPassed,
                "Generic Camera arbitration did not complete.");
            Require(AllLegacyEvidenceExecuted() && AllLegacyEvidencePassed(),
                "ADR-004 lifecycle evidence is incomplete. " + DescribeEvidence());
            Require(QaCameraAdr004BNegativeIntegrityRegression.RunCertification(),
                "ADR-004B Negative Integrity certification failed.");
            Require(QaCameraAdr004COwnerLifetimeIntegrityRegression.RunCertification(),
                "ADR-004C Owner Lifetime certification failed.");
        }

        private static void CompleteSceneProvidedPhase()
        {
            Require(QaCamera026ISceneProvidedFixture.Executed &&
                    QaCamera026ISceneProvidedFixture.Passed,
                "CAMERA-026-I Scene-Provided proof failed. " +
                QaCamera026ISceneProvidedFixture.Diagnostic);

            StopWatching();
            SetPhase(Phase.SceneProvidedPassed);
            Debug.Log("[CAMERA-026-I] status='Passed' " +
                "playerIndependence='PASS' subjectPublication='PASS' " +
                "authoredObservation='PASS' fallbackObservation='PASS' " +
                "replacement='PASS' staleReplacement='PASS' releaseLeave='PASS' " +
                "sceneProvided='PASS' idempotence='PASS' assignmentRegression='PASS' " +
                "implicitCameraRequests='0' packageInternalPrerequisites='PASS'.");
            Debug.Log($"{Prefix} status='SceneProvidedPassed' next='FreshSplitBoot'.");
            EditorApplication.isPlaying = false;
        }

        private static void PrepareSceneProvidedPhase()
        {
            if (EditorApplication.isPlaying || CurrentPhase != Phase.SharedPassed)
            {
                return;
            }

            try
            {
                QaCameraPersistentBaselineGuard.PrepareAndVerify(
                    QaCameraAdr026TopologyMode.Shared,
                    QaPlayerSessionBootProfile.SceneProvided);
                SetPhase(Phase.RunningSceneProvided);
                Debug.Log($"{Prefix} status='Running' phase='SceneProvided'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail("prepare-scene-provided", exception.GetBaseException().Message);
            }
        }

        private static void PrepareSplitPhase()
        {
            if (EditorApplication.isPlaying ||
                CurrentPhase != Phase.SceneProvidedPassed)
            {
                return;
            }

            try
            {
                QaCameraPersistentBaselineGuard.PrepareAndVerify(
                    QaCameraAdr026TopologyMode.Split);
                SetPhase(Phase.RunningSplit);
                Debug.Log($"{Prefix} status='Running' phase='Split'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail("prepare-split", exception.GetBaseException().Message);
            }
        }

        private static void CompleteSplitPhase()
        {
            Require(QaCameraOverrideAuthorityFixture.Adr026SplitExecuted &&
                    QaCameraOverrideAuthorityFixture.Adr026SplitPassed,
                "ADR-026 Split/Multi-Output proof failed. " +
                QaCameraOverrideAuthorityFixture.Adr026SplitDiagnostic);

            StopWatching();
            SetPhase(Phase.Certified);
            QaCameraPersistentBaselineGuard.RequestRestore("camera-full-certified");

            Debug.Log($"{Prefix} status='Completed' verdict='CAMERA QA CERTIFIED' " +
                "subjectsOccurrenceSafety='PASS' sharedCamera='PASS' playerCameraDecoupling='PASS' " +
                "multiOutput='PASS' outputIsolation='PASS' viewOutputAssociation='PASS' " +
                "genericArbitration='PASS' negativeValidation='PASS' " +
                $"mandatoryEstablishedCases='{EstablishedCaseCount}' " +
                $"executedEstablishedCases='{EstablishedCaseCount}' " +
                $"passedEstablishedCases='{EstablishedCaseCount}' adr026Phases='2/2' dimensions='8/8' " +
                "next='RestoreCanonicalBaseline' missing='<none>' cleanup='Requested'.");
            EditorApplication.isPlaying = false;
        }

        private static bool TryResolveHubTrigger(
            string triggerName,
            string label,
            out RouteRequestTrigger resolved)
        {
            resolved = null;
            Scene hub = SceneManager.GetSceneByPath(HubScenePath);
            if (!hub.IsValid() || !hub.isLoaded)
            {
                return false;
            }

            int matches = 0;
            foreach (GameObject root in hub.GetRootGameObjects())
            {
                foreach (RouteRequestTrigger candidate in
                    root.GetComponentsInChildren<RouteRequestTrigger>(true))
                {
                    if (candidate == null ||
                        candidate.gameObject.name != triggerName)
                    {
                        continue;
                    }

                    matches++;
                    resolved = candidate;
                }
            }

            Require(matches == 1 && resolved != null,
                $"Expected one authored {label} Route trigger, found '{matches}'.");
            return true;
        }

        private static bool AllLegacyEvidenceExecuted() =>
            QaCameraOverrideAuthorityFixture.Adr004BActivityLifecycleExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004BRouteLifecycleExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004BOwnerLossExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004CActivityDisableExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004CSessionDisableExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004CNonWinnerDisableExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004CWinningRestoreExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004CIdempotentCleanupExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004CActivityDestroyExecuted &&
            QaCameraOverrideAuthorityFixture.Adr004CRouteReenableExecuted;

        private static bool AllLegacyEvidencePassed() =>
            QaCameraOverrideAuthorityFixture.Adr004BActivityLifecyclePassed &&
            QaCameraOverrideAuthorityFixture.Adr004BRouteLifecyclePassed &&
            QaCameraOverrideAuthorityFixture.Adr004BOwnerLossInvariantPassed &&
            QaCameraOverrideAuthorityFixture.Adr004CActivityDisablePassed &&
            QaCameraOverrideAuthorityFixture.Adr004CSessionDisablePassed &&
            QaCameraOverrideAuthorityFixture.Adr004CNonWinnerDisablePassed &&
            QaCameraOverrideAuthorityFixture.Adr004CWinningRestorePassed &&
            QaCameraOverrideAuthorityFixture.Adr004CIdempotentCleanupPassed &&
            QaCameraOverrideAuthorityFixture.Adr004CActivityDestroyPassed &&
            QaCameraOverrideAuthorityFixture.Adr004CRouteReenablePassed;

        private static string DescribeEvidence() =>
            $"shared='{QaCameraOverrideAuthorityFixture.Adr026SharedExecuted}/" +
            $"{QaCameraOverrideAuthorityFixture.Adr026SharedPassed}' " +
            $"split='{QaCameraOverrideAuthorityFixture.Adr026SplitExecuted}/" +
            $"{QaCameraOverrideAuthorityFixture.Adr026SplitPassed}' " +
            $"generic='{QaCameraOverrideAuthorityFixture.GenericArbitrationExecuted}/" +
            $"{QaCameraOverrideAuthorityFixture.GenericArbitrationPassed}' " +
            $"activityExit='{QaCameraOverrideAuthorityFixture.Adr004BActivityLifecycleExecuted}/" +
            $"{QaCameraOverrideAuthorityFixture.Adr004BActivityLifecyclePassed}' " +
            $"routeExit='{QaCameraOverrideAuthorityFixture.Adr004BRouteLifecycleExecuted}/" +
            $"{QaCameraOverrideAuthorityFixture.Adr004BRouteLifecyclePassed}'.";

        private static Phase CurrentPhase =>
            (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);

        private static void SetPhase(Phase phase) =>
            SessionState.SetInt(PhaseKey, (int)phase);

        private static void Fail(string stage, string reason)
        {
            StopWatching();
            SetPhase(Phase.Failed);
            SessionState.SetString(FailureKey, reason ?? string.Empty);
            QaCameraPersistentBaselineGuard.RequestRestore(
                $"camera-full-failed:{stage}");

            Debug.LogError($"{Prefix} status='Failed' verdict='CAMERA QA NOT CERTIFIED' " +
                $"stage='{stage}' next='FixAndRerun' missing='{Escape(reason)}' " +
                "cleanup='RequestedCanonicalRestore'.");
        }

        private static void StopWatching()
        {
            EditorApplication.update -= Tick;
            pendingRouteRequestBinding?.Dispose();
            pendingRouteRequestBinding = null;
            pendingSceneProvidedRouteResult = null;
            watching = false;
            startedAt = 0d;
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

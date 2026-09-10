using System;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.GameFlow.Internal.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Runs the focused CAMERA-028-A Partial topology certification through a fresh
    /// Framework boot and delegates canonical baseline restoration to the shared guard.
    /// </summary>
    internal static class QaCameraPartialCertificationOrchestrator
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run CAMERA-028-A Partial Certification";
        private const string Prefix = "[CAMERA-028-A]";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string CameraScenePath =
            "Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_PlayerCameraArbitration.unity";
        private const string CameraRouteTriggerName =
            "RouteTrigger_Camera__Override_Authority";
        private const string PhaseKey =
            "ImmersiveFrameworkQA.CAMERA_028_A_PARTIAL.Phase";
        private const string FailureKey =
            "ImmersiveFrameworkQA.CAMERA_028_A_PARTIAL.Failure";
        private const double TimeoutSeconds = 180d;

        private enum Phase
        {
            Idle = 0,
            Running = 10,
            Passed = 20,
            Failed = 30
        }

        private enum WatchStage
        {
            WaitingForHub = 0,
            WaitingForFrameworkReady = 10,
            WaitingForCameraScene = 20,
            WaitingForFreshEvidence = 30,
            WaitingForPartialResult = 40
        }

        private static WatchStage watchStage;
        private static double startedAt;
        private static bool watching;
        private static RouteRequestTrigger pendingRouteTrigger;
        private static string pendingStage = "framework-boot-unavailable";
        private static string pendingDiagnostic =
            "Framework readiness has not been observed yet.";

        [InitializeOnLoadMethod]
        private static void RegisterHook()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlaying &&
            CurrentPhase is Phase.Idle or Phase.Passed or Phase.Failed;

        [MenuItem(MenuPath, priority = 231)]
        private static void Run()
        {
            SessionState.EraseString(FailureKey);
            try
            {
                QaCameraPersistentBaselineGuard.PrepareAndVerify(
                    QaCameraAdr026TopologyMode.Partial);
                SetPhase(Phase.Running);
                Debug.Log(
                    $"{Prefix} status='Running' availableOutputs='2' " +
                    "participatingBindings='1' expectedCases='8'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail("prepare-partial", exception.GetBaseException().Message);
            }
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode &&
                CurrentPhase == Phase.Running)
            {
                BeginWatching();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode &&
                CurrentPhase == Phase.Running)
            {
                Fail(
                    "play-mode-interrupted",
                    "Play Mode exited before CAMERA-028-A Partial certification completed.");
            }
        }

        private static void BeginWatching()
        {
            StopWatching();
            watching = true;
            watchStage = WatchStage.WaitingForHub;
            startedAt = EditorApplication.timeSinceStartup;
            pendingRouteTrigger = null;
            pendingStage = "framework-boot-unavailable";
            pendingDiagnostic =
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
                Fail("runtime-evidence", exception.GetBaseException().Message);
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
                Fail(pendingStage, pendingDiagnostic);
                EditorApplication.isPlaying = false;
                return;
            }

            if (watchStage == WatchStage.WaitingForHub)
            {
                pendingStage = "framework-boot-unavailable";
                pendingDiagnostic =
                    "QA Hub scene has not finished loading the canonical Camera Route trigger.";
                if (!TryResolveHubTrigger(out RouteRequestTrigger trigger))
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

                if (!QaH2FrameworkReadiness.TryGetReady(out string frameworkDiagnostic))
                {
                    pendingStage = "framework-boot-unavailable";
                    pendingDiagnostic =
                        "Framework has not finished starting Game Flow with a ready Hub Activity. " +
                        frameworkDiagnostic;
                    return;
                }

                if (!pendingRouteTrigger.HasRouteRuntimeBinding)
                {
                    pendingStage = "route-runtime-unavailable";
                    pendingDiagnostic =
                        "Camera Route trigger has not been bound to the Game Flow route runtime port. " +
                        pendingRouteTrigger.RouteRuntimeBindingDiagnostic;
                    return;
                }

                Require(!pendingRouteTrigger.IsRequestInFlight,
                    "Camera Route trigger already has a request in flight.");
                pendingRouteTrigger.RequestRoute();
                watchStage = WatchStage.WaitingForCameraScene;
                pendingStage = "camera-route-entry";
                pendingDiagnostic = "Canonical Camera QA scene has not loaded.";
                return;
            }

            if (watchStage == WatchStage.WaitingForCameraScene)
            {
                Scene cameraScene = SceneManager.GetSceneByPath(CameraScenePath);
                if (!cameraScene.IsValid() || !cameraScene.isLoaded)
                {
                    return;
                }

                watchStage = WatchStage.WaitingForFreshEvidence;
                pendingStage = "partial-evidence-reset";
                pendingDiagnostic =
                    "CAMERA-028-A fixture has not exposed a fresh evidence cycle.";
                return;
            }

            if (watchStage == WatchStage.WaitingForFreshEvidence)
            {
                if (QaCameraOverrideAuthorityFixture.Adr026PartialExecuted)
                {
                    return;
                }

                watchStage = WatchStage.WaitingForPartialResult;
                pendingStage = "partial-runtime-evidence";
                pendingDiagnostic =
                    "CAMERA-028-A runtime evidence did not reach a terminal result.";
                return;
            }

            if (!QaCameraOverrideAuthorityFixture.Adr026PartialExecuted)
            {
                return;
            }

            if (!QaCameraOverrideAuthorityFixture.Adr026PartialPassed)
            {
                Fail(
                    "partial-runtime-evidence",
                    QaCameraOverrideAuthorityFixture.Adr026PartialDiagnostic);
                EditorApplication.isPlaying = false;
                return;
            }

            CompletePassed();
        }

        private static void CompletePassed()
        {
            string diagnostic = QaCameraOverrideAuthorityFixture.Adr026PartialDiagnostic ??
                string.Empty;
            Require(diagnostic.Contains("availableOutputs='2'"),
                "CAMERA-028-A result omitted available Output cardinality.");
            Require(diagnostic.Contains("participatingBindings='1'"),
                "CAMERA-028-A result omitted participating binding cardinality.");
            Require(diagnostic.Contains("cases='8/8'"),
                "CAMERA-028-A result omitted complete runtime evidence cardinality.");

            StopWatching();
            SetPhase(Phase.Passed);
            QaCameraPersistentBaselineGuard.RequestRestore(
                "camera-028-a-partial-certified");

            Debug.Log(
                $"{Prefix} status='Passed' availableOutputs='2' " +
                "participatingBindings='1' cases='8/8' " +
                "cleanup='RequestedCanonicalRestore'.");
            EditorApplication.isPlaying = false;
        }

        private static bool TryResolveHubTrigger(out RouteRequestTrigger resolved)
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
                        candidate.gameObject.name != CameraRouteTriggerName)
                    {
                        continue;
                    }

                    matches++;
                    resolved = candidate;
                }
            }

            Require(matches == 1 && resolved != null,
                $"Expected one authored Camera Route trigger, found '{matches}'.");
            return true;
        }

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
                $"camera-028-a-partial-failed:{stage}");

            Debug.LogError(
                $"{Prefix} status='Failed' stage='{stage}' " +
                $"missing='{Escape(reason)}' cleanup='RequestedCanonicalRestore'.");
        }

        private static void StopWatching()
        {
            EditorApplication.update -= Tick;
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

using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.GameFlow.Internal.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Focused CAMERA-028-C certification. It authors a non-default external Camera.rect,
    /// boots the normal Shared Camera QA flow and proves that Framework Camera never
    /// mutates that physical-presentation state while requests and route transitions run.
    /// </summary>
    internal static class QaCamera028CPhysicalPresentationCertification
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run CAMERA-028-C Physical Presentation Certification";
        private const string Prefix = "[CAMERA-028-C]";
        private const string GlobalScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string CameraScenePath =
            "Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_PlayerCameraArbitration.unity";
        private const string CameraRouteTriggerName =
            "RouteTrigger_Camera__Override_Authority";
        private const string PhaseKey =
            "ImmersiveFrameworkQA.CAMERA_028_C_PHYSICAL_PRESENTATION.Phase";
        private const string FailureKey =
            "ImmersiveFrameworkQA.CAMERA_028_C_PHYSICAL_PRESENTATION.Failure";
        private const double TimeoutSeconds = 240d;

        private static readonly Rect ExternalRect =
            new Rect(0.11f, 0.17f, 0.63f, 0.71f);

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
            WaitingForProbe = 20,
            WaitingForCameraScene = 30,
            WaitingForFreshEvidence = 40,
            WaitingForTerminalResult = 50
        }

        private static WatchStage watchStage;
        private static double startedAt;
        private static bool watching;
        private static RouteRequestTrigger pendingRouteTrigger;
        private static int samplesAtRouteRequest;
        private static int samplesAtFreshEvidence;
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

        [MenuItem(MenuPath, priority = 232)]
        private static void Run()
        {
            SessionState.EraseString(FailureKey);
            try
            {
                QaCameraPersistentBaselineGuard.PrepareAndVerify(
                    QaCameraAdr026TopologyMode.Shared);
                InstallPersistedExternalRectProbe();
                SetPhase(Phase.Running);
                Debug.Log(
                    $"{Prefix} status='Running' expectedRect='{Describe(ExternalRect)}' " +
                    "contract='FrameworkPreservesExternalCameraRect'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                Fail("prepare-physical-presentation", exception.GetBaseException().Message);
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
                    "Play Mode exited before CAMERA-028-C certification completed.");
            }
        }

        private static void BeginWatching()
        {
            StopWatching();
            watching = true;
            watchStage = WatchStage.WaitingForHub;
            startedAt = EditorApplication.timeSinceStartup;
            pendingRouteTrigger = null;
            samplesAtRouteRequest = 0;
            samplesAtFreshEvidence = 0;
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

                watchStage = WatchStage.WaitingForProbe;
                pendingStage = "physical-presentation-probe";
                pendingDiagnostic =
                    "Persisted CAMERA-028-C physical-presentation probe has not started monitoring.";
                return;
            }

            if (watchStage == WatchStage.WaitingForProbe)
            {
                if (QaCamera028CPhysicalPresentationFixture.HasViolation)
                {
                    throw new InvalidOperationException(
                        QaCamera028CPhysicalPresentationFixture.Diagnostic);
                }

                if (!QaCamera028CPhysicalPresentationFixture.IsMonitoring)
                {
                    return;
                }

                RequireProbeHealthy("before-camera-route");
                Require(pendingRouteTrigger.HasRouteRuntimeBinding,
                    "Camera Route trigger has not been bound to the Game Flow route runtime port. " +
                    pendingRouteTrigger.RouteRuntimeBindingDiagnostic);
                Require(!pendingRouteTrigger.IsRequestInFlight,
                    "Camera Route trigger already has a request in flight.");

                samplesAtRouteRequest =
                    QaCamera028CPhysicalPresentationFixture.SampleCount;
                pendingRouteTrigger.RequestRoute();
                watchStage = WatchStage.WaitingForCameraScene;
                pendingStage = "camera-route-entry";
                pendingDiagnostic =
                    "Canonical Camera QA scene has not loaded while the external rect probe remained active.";
                return;
            }

            if (watchStage == WatchStage.WaitingForCameraScene)
            {
                RequireProbeHealthy("camera-route-entry");
                Scene cameraScene = SceneManager.GetSceneByPath(CameraScenePath);
                if (!cameraScene.IsValid() || !cameraScene.isLoaded)
                {
                    return;
                }

                watchStage = WatchStage.WaitingForFreshEvidence;
                pendingStage = "camera-evidence-reset";
                pendingDiagnostic =
                    "Canonical Shared Camera fixture has not exposed a fresh evidence cycle.";
                return;
            }

            if (watchStage == WatchStage.WaitingForFreshEvidence)
            {
                RequireProbeHealthy("camera-scene-loaded");
                if (QaCameraOverrideAuthorityFixture.Adr026SharedExecuted ||
                    QaCameraOverrideAuthorityFixture.GenericArbitrationExecuted)
                {
                    return;
                }

                samplesAtFreshEvidence =
                    QaCamera028CPhysicalPresentationFixture.SampleCount;
                Require(samplesAtFreshEvidence >= samplesAtRouteRequest,
                    "CAMERA-028-C probe sample count regressed during route entry.");
                watchStage = WatchStage.WaitingForTerminalResult;
                pendingStage = "physical-presentation-runtime-evidence";
                pendingDiagnostic =
                    "Shared Camera fixture did not complete its request/arbitration/route-exit lifecycle.";
                return;
            }

            RequireProbeHealthy("shared-camera-runtime");
            if (QaCameraOverrideAuthorityFixture.Adr026SharedExecuted &&
                !QaCameraOverrideAuthorityFixture.Adr026SharedPassed)
            {
                Fail(
                    "shared-camera-proof",
                    QaCameraOverrideAuthorityFixture.Adr026SharedDiagnostic);
                EditorApplication.isPlaying = false;
                return;
            }

            if (!QaCameraOverrideAuthorityFixture.GenericArbitrationExecuted)
            {
                return;
            }

            Require(QaCameraOverrideAuthorityFixture.Adr026SharedExecuted &&
                    QaCameraOverrideAuthorityFixture.Adr026SharedPassed,
                "CAMERA-028-C requires the canonical Shared ADR-026 proof to pass.");
            Require(QaCameraOverrideAuthorityFixture.GenericArbitrationPassed,
                "CAMERA-028-C requires the canonical request/arbitration lifecycle to pass.");
            Require(
                QaCamera028CPhysicalPresentationFixture.SampleCount > samplesAtFreshEvidence,
                "CAMERA-028-C probe did not observe the runtime after the Shared fixture began.");

            CompletePassed();
        }

        private static void CompletePassed()
        {
            RequireProbeHealthy("terminal");

            int sampleCount = QaCamera028CPhysicalPresentationFixture.SampleCount;
            StopWatching();
            SetPhase(Phase.Passed);
            QaCameraPersistentBaselineGuard.RequestRestore(
                "camera-028-c-physical-presentation-certified");

            Debug.Log(
                $"{Prefix} status='Passed' " +
                "physicalPresentationNonOwnership='PASS' " +
                "externalRectPreserved='PASS' " +
                "frameworkRectMutationObserved='False' " +
                $"expectedRect='{Describe(ExternalRect)}' samples='{sampleCount}' " +
                "cleanup='RequestedCanonicalRestore'.");
            EditorApplication.isPlaying = false;
        }

        private static void InstallPersistedExternalRectProbe()
        {
            Scene scene = EditorSceneManager.OpenScene(
                GlobalScenePath,
                OpenSceneMode.Single);
            CameraOutputAuthoring output = RequireOutputA(scene);
            UnityEngine.Camera camera = output.UnityCamera;
            Require(camera != null,
                "CAMERA-028-C Output A requires its explicit Unity Camera.");
            Require(output.GetComponents<QaCamera028CPhysicalPresentationFixture>().Length == 0,
                "CAMERA-028-C Output A already contains a physical-presentation probe.");

            camera.rect = ExternalRect;
            QaCamera028CPhysicalPresentationFixture probe =
                output.gameObject.AddComponent<QaCamera028CPhysicalPresentationFixture>();
            probe.Configure(camera, ExternalRect);

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(probe);
            EditorUtility.SetDirty(output.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, GlobalScenePath))
            {
                throw new InvalidOperationException(
                    "CAMERA-028-C could not persist its external Camera.rect probe.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Scene reloaded = EditorSceneManager.OpenScene(
                GlobalScenePath,
                OpenSceneMode.Single);
            CameraOutputAuthoring persistedOutput = RequireOutputA(reloaded);
            QaCamera028CPhysicalPresentationFixture persistedProbe =
                persistedOutput.GetComponent<QaCamera028CPhysicalPresentationFixture>();
            Require(persistedProbe != null,
                "CAMERA-028-C persisted Output A is missing its physical-presentation probe.");
            Require(RectMatches(persistedOutput.UnityCamera.rect, ExternalRect),
                "CAMERA-028-C external Camera.rect did not survive a persisted scene reload. " +
                $"expected='{Describe(ExternalRect)}' actual='{Describe(persistedOutput.UnityCamera.rect)}'.");

            EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
        }

        private static CameraOutputAuthoring RequireOutputA(Scene scene)
        {
            CameraOutputDefinition definition =
                QaCameraPersistentTopologyBuilder.RequireDefinition<CameraOutputDefinition>(
                    QaCameraPersistentTopologyBuilder.OutputAPath);
            CameraOutputAuthoring match = null;
            int matches = 0;

            foreach (CameraOutputAuthoring candidate in
                     FindInScene<CameraOutputAuthoring>(scene))
            {
                if (candidate == null ||
                    !ReferenceEquals(candidate.OutputDefinition, definition))
                {
                    continue;
                }

                match = candidate;
                matches++;
            }

            Require(matches == 1 && match != null,
                $"CAMERA-028-C expected exactly one persisted Output A, found '{matches}'.");
            return match;
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

        private static void RequireProbeHealthy(string stage)
        {
            Require(QaCamera028CPhysicalPresentationFixture.IsMonitoring,
                $"CAMERA-028-C probe stopped monitoring at '{stage}'.");
            Require(!QaCamera028CPhysicalPresentationFixture.HasViolation,
                string.IsNullOrWhiteSpace(
                    QaCamera028CPhysicalPresentationFixture.Diagnostic)
                    ? $"CAMERA-028-C observed a physical-presentation violation at '{stage}'."
                    : QaCamera028CPhysicalPresentationFixture.Diagnostic);
            Require(QaCamera028CPhysicalPresentationFixture.CurrentRectMatchesExpected,
                $"CAMERA-028-C current Camera.rect diverged at '{stage}'. " +
                $"expected='{Describe(QaCamera028CPhysicalPresentationFixture.ExpectedRect)}' " +
                $"actual='{Describe(QaCamera028CPhysicalPresentationFixture.LastObservedRect)}'.");
        }

        private static List<T> FindInScene<T>(Scene scene)
            where T : Component
        {
            var results = new List<T>();
            foreach (T item in Resources.FindObjectsOfTypeAll<T>())
            {
                if (item != null && item.gameObject.scene == scene)
                {
                    results.Add(item);
                }
            }

            return results;
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
                $"camera-028-c-physical-presentation-failed:{stage}");

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

        private static bool RectMatches(Rect left, Rect right)
        {
            const float tolerance = 0.000001f;
            return Mathf.Abs(left.x - right.x) <= tolerance &&
                   Mathf.Abs(left.y - right.y) <= tolerance &&
                   Mathf.Abs(left.width - right.width) <= tolerance &&
                   Mathf.Abs(left.height - right.height) <= tolerance;
        }

        private static string Describe(Rect value) =>
            $"x={value.x:F6},y={value.y:F6},w={value.width:F6},h={value.height:F6}";

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

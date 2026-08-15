using System;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Aggregates the already-existing Camera proofs into one canonical user flow.
    /// It does not duplicate Camera authority, materialization or lifecycle tests.
    /// </summary>
    internal static class QaCameraFullCertificationOrchestrator
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run Full Camera QA";
        private const string Prefix = "[QA_CAMERA_FULL]";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string CanonicalScenePath =
            "Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_PlayerCameraArbitration.unity";
        private const string CameraRouteTriggerName =
            "RouteTrigger_Camera__Override_Authority";
        private const string Adr022MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run ADR-022 Presentation Materialization Regression";
        private const string Adr022TerminalPrefix =
            "[QA][ADR022 Presentation Models] PASS.";
        private const int Adr022Cases = 14;
        private const int CanonicalCases = 11;
        private const int Adr004BCases = 18;
        private const int Adr004CCases = 10;
        private const int TotalCases =
            Adr022Cases + CanonicalCases + Adr004BCases + Adr004CCases;
        private const double TimeoutSeconds = 120d;

        private enum Stage
        {
            Idle,
            WaitingForCanonicalSceneEnter,
            WaitingForCanonicalSceneExit
        }

        private static Stage stage;
        private static double startedAt;
        private static bool running;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            EditorApplication.isPlaying && !running;

        [MenuItem(MenuPath, priority = 230)]
        private static void Run()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' reason='PlayModeRequired'.");
                return;
            }

            if (running)
            {
                Debug.LogWarning(
                    $"{Prefix} status='Ignored' reason='AlreadyRunning'.");
                return;
            }

            try
            {
                Require(
                    RunAdr022PresentationCertification(),
                    "ADR-022 Presentation Materialization did not reach PASS 14/14.");

                RouteRequestTrigger trigger =
                    ResolveHubCameraRouteTrigger();

                Require(trigger != null,
                    "Full Camera QA requires the explicitly authored Camera RouteRequestTrigger.");
                Require(!trigger.IsRequestInFlight,
                    "Camera RouteRequestTrigger already has a request in flight.");

                running = true;
                stage = Stage.WaitingForCanonicalSceneEnter;
                startedAt = EditorApplication.timeSinceStartup;
                EditorApplication.update += Tick;

                Debug.Log(
                    $"{Prefix} status='Running' phase='ADR022PassedStartingCanonicalC9R' " +
                    $"adr022='PASS' expectedCases='{TotalCases}'.");

                trigger.RequestRoute();
            }
            catch (Exception exception)
            {
                Fail(exception.GetBaseException().Message);
            }
        }

        private static void Tick()
        {
            if (!running)
            {
                StopWatching();
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                Fail("Play Mode ended before Full Camera QA completed.");
                return;
            }

            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                Fail(
                    "Timed out waiting for canonical C9R evidence. " +
                    DescribeCanonicalEvidence());
                return;
            }

            if (stage == Stage.WaitingForCanonicalSceneEnter)
            {
                Scene canonical =
                    SceneManager.GetSceneByPath(CanonicalScenePath);
                if (canonical.IsValid() && canonical.isLoaded)
                {
                    stage = Stage.WaitingForCanonicalSceneExit;
                }

                return;
            }

            if (stage != Stage.WaitingForCanonicalSceneExit)
            {
                return;
            }

            Scene currentCanonical =
                SceneManager.GetSceneByPath(CanonicalScenePath);
            if (currentCanonical.IsValid() && currentCanonical.isLoaded)
            {
                return;
            }

            try
            {
                Require(
                    AllEvidenceExecuted(),
                    "Canonical C9R scene exited without producing the complete certification evidence. " +
                    DescribeCanonicalEvidence());
                Require(
                    AllEvidencePassed(),
                    "Canonical C9R completed with failed evidence. " +
                    DescribeCanonicalEvidence());

                bool adr004b =
                    QaCameraAdr004BNegativeIntegrityRegression.RunCertification();
                bool adr004c =
                    QaCameraAdr004COwnerLifetimeIntegrityRegression.RunCertification();

                Require(adr004b,
                    "ADR-004B Negative Integrity certification failed or was blocked.");
                Require(adr004c,
                    "ADR-004C Owner Lifetime Integrity certification failed.");

                Succeed();
            }
            catch (Exception exception)
            {
                Fail(exception.GetBaseException().Message);
            }
        }

        private static bool RunAdr022PresentationCertification()
        {
            string terminal = string.Empty;

            void Capture(
                string condition,
                string stackTrace,
                LogType type)
            {
                if (!string.IsNullOrEmpty(condition) &&
                    condition.StartsWith(
                        "[QA][ADR022 Presentation Models]",
                        StringComparison.Ordinal))
                {
                    terminal = condition;
                }
            }

            Application.logMessageReceived += Capture;
            try
            {
                bool invoked =
                    EditorApplication.ExecuteMenuItem(
                        Adr022MenuPath);

                return invoked &&
                    terminal.StartsWith(
                        Adr022TerminalPrefix,
                        StringComparison.Ordinal) &&
                    terminal.Contains("cases='14/14'");
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }
        }

        private static RouteRequestTrigger
            ResolveHubCameraRouteTrigger()
        {
            Scene hub = SceneManager.GetSceneByPath(HubScenePath);
            Require(
                hub.IsValid() && hub.isLoaded,
                "Run Full Camera QA from the loaded QA Hub scene.");

            RouteRequestTrigger resolved = null;
            int matches = 0;

            foreach (GameObject root in hub.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                RouteRequestTrigger[] triggers =
                    root.GetComponentsInChildren<RouteRequestTrigger>(
                        true);

                for (int index = 0; index < triggers.Length; index++)
                {
                    RouteRequestTrigger candidate =
                        triggers[index];

                    if (candidate == null ||
                        candidate.gameObject.name !=
                        CameraRouteTriggerName)
                    {
                        continue;
                    }

                    matches++;
                    resolved = candidate;
                }
            }

            Require(
                matches == 1 && resolved != null,
                $"Expected exactly one authored Camera RouteRequestTrigger '{CameraRouteTriggerName}' in QA Hub. found='{matches}'.");

            return resolved;
        }

        private static bool AllEvidenceExecuted() =>
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

        private static bool AllEvidencePassed() =>
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

        private static string DescribeCanonicalEvidence() =>
            $"activityExit='{QaCameraOverrideAuthorityFixture.Adr004BActivityLifecycleExecuted}/{QaCameraOverrideAuthorityFixture.Adr004BActivityLifecyclePassed}' " +
            $"routeExit='{QaCameraOverrideAuthorityFixture.Adr004BRouteLifecycleExecuted}/{QaCameraOverrideAuthorityFixture.Adr004BRouteLifecyclePassed}' " +
            $"ownerLoss='{QaCameraOverrideAuthorityFixture.Adr004BOwnerLossExecuted}/{QaCameraOverrideAuthorityFixture.Adr004BOwnerLossInvariantPassed}' " +
            $"activityDisable='{QaCameraOverrideAuthorityFixture.Adr004CActivityDisableExecuted}/{QaCameraOverrideAuthorityFixture.Adr004CActivityDisablePassed}' " +
            $"sessionDisable='{QaCameraOverrideAuthorityFixture.Adr004CSessionDisableExecuted}/{QaCameraOverrideAuthorityFixture.Adr004CSessionDisablePassed}' " +
            $"nonWinner='{QaCameraOverrideAuthorityFixture.Adr004CNonWinnerDisableExecuted}/{QaCameraOverrideAuthorityFixture.Adr004CNonWinnerDisablePassed}' " +
            $"winnerRestore='{QaCameraOverrideAuthorityFixture.Adr004CWinningRestoreExecuted}/{QaCameraOverrideAuthorityFixture.Adr004CWinningRestorePassed}' " +
            $"idempotent='{QaCameraOverrideAuthorityFixture.Adr004CIdempotentCleanupExecuted}/{QaCameraOverrideAuthorityFixture.Adr004CIdempotentCleanupPassed}' " +
            $"activityDestroy='{QaCameraOverrideAuthorityFixture.Adr004CActivityDestroyExecuted}/{QaCameraOverrideAuthorityFixture.Adr004CActivityDestroyPassed}' " +
            $"routeReenable='{QaCameraOverrideAuthorityFixture.Adr004CRouteReenableExecuted}/{QaCameraOverrideAuthorityFixture.Adr004CRouteReenablePassed}'.";

        private static void Succeed()
        {
            StopWatching();
            Debug.Log(
                $"{Prefix} status='Completed' verdict='CAMERA QA CERTIFIED' " +
                $"adr022Presentation='PASS' canonicalAuthority='PASS' " +
                $"adr004NegativeIntegrity='PASS' adr004OwnerLifetime='PASS' " +
                $"mandatoryCases='{TotalCases}' executedCases='{TotalCases}' passedCases='{TotalCases}'.");
        }

        private static void Fail(string reason)
        {
            StopWatching();
            Debug.LogError(
                $"{Prefix} status='Failed' verdict='CAMERA QA NOT CERTIFIED' " +
                $"reason='{Escape(reason)}'.");
        }

        private static void StopWatching()
        {
            EditorApplication.update -= Tick;
            running = false;
            stage = Stage.Idle;
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

using System;
using System.Threading.Tasks;
using ImmersiveFrameworkQA.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Joint Edit/Play Mode orchestration for Player Surface public certification.
    /// Prepares the authored public Player fixture, then runs Q1 and Q2 in
    /// separate fresh Play Mode sessions.
    /// </summary>
    public static class QaPlayerSurfaceCertificationOrchestrator
    {
        private const string Prefix = "[QA_PLAYER_SURFACE_CERT]";
        private const string PrepareMenuPath =
            "Immersive Framework/QA/Player/Public Surface/Prepare Certification Fixture";
        private const string RunMenuPath =
            "Immersive Framework/QA/Player/Public Surface/Run Certification";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string PhaseKey =
            "ImmersiveFrameworkQA.QA_PLAYER_SURFACE.CertPhase";
        private const string Q1ResultKey =
            "ImmersiveFrameworkQA.QA_PLAYER_SURFACE.CertQ1";
        private const string Q2ResultKey =
            "ImmersiveFrameworkQA.QA_PLAYER_SURFACE.CertQ2";
        private const string NavResultKey =
            "ImmersiveFrameworkQA.QA_PLAYER_SURFACE.CertNav";
        private const string ErrorKey =
            "ImmersiveFrameworkQA.QA_PLAYER_SURFACE.CertError";

        private enum Phase
        {
            Unprepared = 0,
            Preparing = 1,
            Prepared = 2,
            RunningQ1 = 3,
            Q1Passed = 4,
            PreparingQ2 = 5,
            RunningQ2 = 6,
            Certified = 7,
            Failed = 8
        }

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeHook()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(PrepareMenuPath, true)]
        private static bool ValidatePrepare() => !EditorApplication.isPlaying;

        [MenuItem(PrepareMenuPath)]
        private static void PrepareFromMenu()
        {
            PrepareForCertification();
        }

        /// <summary>
        /// Prepares the public fixture. Never marks Prepared on failure.
        /// </summary>
        internal static void PrepareForCertification()
        {
            Require(!EditorApplication.isPlaying,
                "Player Surface Full Certification prepare must run in Edit Mode.");

            SessionState.SetInt(PhaseKey, (int)Phase.Preparing);
            SessionState.EraseString(Q1ResultKey);
            SessionState.EraseString(Q2ResultKey);
            SessionState.EraseString(NavResultKey);
            SessionState.EraseString(ErrorKey);

            try
            {
                PrepareArtifactsForPlaySession();

                SessionState.SetInt(PhaseKey, (int)Phase.Prepared);
                Debug.Log(
                    $"{Prefix} status='Prepared' " +
                    "publicPlayerFixture='Prepared' " +
                    "next='Run Player Surface Full Certification (Q1+Q2) or enter Play Mode after prepare'.");
            }
            catch (Exception exception)
            {
                SessionState.SetInt(PhaseKey, (int)Phase.Failed);
                SessionState.SetString(ErrorKey, exception.Message);
                SessionState.EraseString(Q1ResultKey);
                SessionState.EraseString(Q2ResultKey);
                SessionState.EraseString(NavResultKey);
                Debug.LogError(
                    $"{Prefix} status='PrepareFailed' " +
                    $"message='{Escape(exception.Message)}' " +
                    "q1='NotStarted' q2='NotStarted'.");
                throw;
            }
        }

        /// <summary>
        /// Prepares one isolated Public Surface Play Mode phase without starting
        /// the Public Surface orchestrator's own phase machine.
        /// </summary>
        public static void PrepareForFullPlayerQa()
        {
            Require(!EditorApplication.isPlaying,
                "Player Surface preparation for Full Player QA must run in Edit Mode.");
            PrepareArtifactsForPlaySession();
        }

        [MenuItem(RunMenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(RunMenuPath)]
        private static void RunFullCertification()
        {
            try
            {
                PrepareForCertification();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='Aborted' " +
                    "reason='prepare-failed' " +
                    $"message='{Escape(exception.Message)}' " +
                    "q1='NotStarted' q2='NotStarted'.");
                return;
            }

            if ((Phase)SessionState.GetInt(PhaseKey, (int)Phase.Unprepared) !=
                Phase.Prepared)
            {
                Debug.LogError(
                    $"{Prefix} status='Aborted' reason='not-prepared' " +
                    "q1='NotStarted' q2='NotStarted'.");
                return;
            }

            SessionState.SetInt(PhaseKey, (int)Phase.RunningQ1);
            EditorApplication.isPlaying = true;
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            Phase phase = (Phase)SessionState.GetInt(
                PhaseKey,
                (int)Phase.Unprepared);
            if (phase is Phase.Unprepared or Phase.Certified or Phase.Failed or
                Phase.Prepared)
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (phase == Phase.RunningQ1)
                {
                    _ = RunQ1PhaseAsync();
                    return;
                }

                if (phase == Phase.RunningQ2)
                {
                    _ = RunQ2PhaseAsync();
                }

                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode &&
                phase == Phase.Q1Passed)
            {
                EditorApplication.delayCall += PrepareAndRunQ2;
            }
        }

        private static async Task RunQ1PhaseAsync()
        {
            try
            {
                await Task.Yield();
                await QaPlayerProvisioningPublicSurfaceRegression
                    .RunCertificationAsync();
                SessionState.SetString(Q1ResultKey, "PASS");
                SessionState.SetString(NavResultKey, "PASS");
                SessionState.SetInt(PhaseKey, (int)Phase.Q1Passed);
                Debug.Log(
                    $"{Prefix} status='Q1_PASS' navigation='PASS' next='Exit Play Mode for Q2'.");
                EditorApplication.isPlaying = false;
            }
            catch (Exception exception)
            {
                SessionState.SetString(Q1ResultKey, "FAIL");
                SessionState.SetString(NavResultKey, "FAIL_OR_SKIPPED");
                SessionState.SetString(ErrorKey, exception.Message);
                SessionState.SetInt(PhaseKey, (int)Phase.Failed);
                Debug.LogError(
                    $"{Prefix} status='Q1_FAIL' message='{Escape(exception.Message)}'.");
                EditorApplication.isPlaying = false;
            }
        }

        private static async Task RunQ2PhaseAsync()
        {
            try
            {
                await Task.Yield();
                await QaPlayerProvisioningPublicSurfaceNegativeRegression
                    .RunCertificationAsync();
                SessionState.SetString(Q2ResultKey, "PASS");
                SessionState.SetInt(PhaseKey, (int)Phase.Certified);
                string q1 = SessionState.GetString(Q1ResultKey, "UNKNOWN");
                string nav = SessionState.GetString(NavResultKey, "UNKNOWN");
                Debug.Log(
                    $"{Prefix} status='Complete' " +
                    $"navigation='{nav}' q1='{q1}' q2='PASS' " +
                    (q1 == "PASS"
                        ? "verdict='PLAYER SURFACE QA CERTIFIED'"
                        : "verdict='PLAYER SURFACE QA PARTIAL'"));
                EditorApplication.isPlaying = false;
            }
            catch (Exception exception)
            {
                SessionState.SetString(Q2ResultKey, "FAIL");
                SessionState.SetString(ErrorKey, exception.Message);
                SessionState.SetInt(PhaseKey, (int)Phase.Failed);
                Debug.LogError(
                    $"{Prefix} status='Q2_FAIL' " +
                    $"q1='{SessionState.GetString(Q1ResultKey, "UNKNOWN")}' " +
                    $"message='{Escape(exception.Message)}'.");
                EditorApplication.isPlaying = false;
            }
        }

        private static void PrepareAndRunQ2()
        {
            if (EditorApplication.isPlaying ||
                (Phase)SessionState.GetInt(
                    PhaseKey,
                    (int)Phase.Unprepared) != Phase.Q1Passed)
            {
                return;
            }

            SessionState.SetInt(PhaseKey, (int)Phase.PreparingQ2);
            try
            {
                PrepareArtifactsForPlaySession();
                SessionState.SetInt(PhaseKey, (int)Phase.RunningQ2);
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                SessionState.SetString(Q2ResultKey, "NOT_STARTED");
                SessionState.SetString(ErrorKey, exception.Message);
                SessionState.SetInt(PhaseKey, (int)Phase.Failed);
                Debug.LogError(
                    $"{Prefix} status='Q2PrepareFailed' " +
                    $"q1='{SessionState.GetString(Q1ResultKey, "UNKNOWN")}' " +
                    $"message='{Escape(exception.Message)}'.");
            }
        }

        private static void PrepareArtifactsForPlaySession()
        {
            QaPlayerSurfacePublicNavigationSetup.PrepareForCertification();
            QaPlayerSurfacePublicNavigationSetup.RequirePrepared();
            RequireAuthoredHubFixturePresent();
        }

        private static void RequireAuthoredHubFixturePresent()
        {
            Scene hub = SceneManager.GetSceneByPath(HubScenePath);
            if (!hub.IsValid() || !hub.isLoaded)
            {
                hub = EditorSceneManager.OpenScene(
                    HubScenePath,
                    OpenSceneMode.Single);
            }

            Require(
                hub.IsValid() && hub.isLoaded,
                $"Hub scene '{HubScenePath}' could not be opened for fixture verification.");

            GameObject root = null;
            GameObject[] roots = hub.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (string.Equals(
                        roots[index].name,
                        QaPlayerSurfacePublicNavigationFixture.RootObjectName,
                        StringComparison.Ordinal))
                {
                    root = roots[index];
                    break;
                }
            }

            Require(
                root != null,
                $"Hub scene is missing root '{QaPlayerSurfacePublicNavigationFixture.RootObjectName}'.");

            QaPlayerSurfacePublicNavigationFixture fixture =
                root.GetComponent<QaPlayerSurfacePublicNavigationFixture>();
            Require(
                fixture != null,
                "Hub fixture root has no runtime QaPlayerSurfacePublicNavigationFixture component. " +
                "Editor-only assemblies cannot host scene MonoBehaviours.");
            Require(
                fixture.TryValidateAuthoredSurface(out string issue),
                issue);
            Require(
                fixture.EnterActivityTrigger != null &&
                fixture.EnterActivityTrigger.TargetActivity != null,
                "Authored enter ActivityRequestTrigger is missing or untargeted.");
            Require(
                fixture.ClearActivityTrigger != null,
                "Authored clear ActivityRequestTrigger is missing.");
            Require(
                fixture.RouteConsumerBinding != null &&
                fixture.RouteConsumerBinding.Scope ==
                    Immersive.Framework.PlayerParticipation
                        .LocalPlayerProvisioningConsumerScope.Route,
                "Authored Route consumer binding is missing or wrong scope.");
            Require(
                fixture.WrongScopeBinding != null &&
                fixture.WrongScopeBinding.Scope ==
                    Immersive.Framework.PlayerParticipation
                        .LocalPlayerProvisioningConsumerScope.Activity,
                "Authored wrong-scope negative binding is missing or wrong scope.");
            Require(
                fixture.DestroyProbeBinding != null &&
                fixture.DestroyProbeBinding.Scope ==
                    Immersive.Framework.PlayerParticipation
                        .LocalPlayerProvisioningConsumerScope.Route,
                "Authored destroy-probe binding is missing or wrong scope.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

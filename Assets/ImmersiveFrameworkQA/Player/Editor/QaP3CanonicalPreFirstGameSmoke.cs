using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    [InitializeOnLoad]
    internal static class QaP3CanonicalPreFirstGameSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3 Run Canonical Pre-FIRSTGAME Smoke";
        private const string Prefix = "ImmersiveFrameworkQA.P3Canonical.";
        private const string RunIdKey = Prefix + "RunId";
        private const string PhaseKey = Prefix + "Phase";
        private const string CurrentCaseKey = Prefix + "CurrentCase";
        private const string CompletedKey = Prefix + "Completed";
        private const string SceneSetupKey = Prefix + "SceneSetup";
        private const string StatusKey = Prefix + "Status";
        private const string ExceptionKey = Prefix + "Exception";
        private const string MessageKey = Prefix + "Message";
        private const string FailureSceneKey = Prefix + "FailureScene";
        private const string FailurePlayModeKey = Prefix + "FailurePlayMode";

        private static bool playModeRunnerStarted;

        static QaP3CanonicalPreFirstGameSmoke()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.delayCall += RecoverOrClearStaleRun;
        }

        [MenuItem(MenuPath)]
        private static void Run()
        {
            if (!string.IsNullOrEmpty(
                    SessionState.GetString(RunIdKey, string.Empty)))
            {
                RecordFailure(
                    SessionState.GetString(PhaseKey, "editor"),
                    new InvalidOperationException(
                        "A stale canonical P3 run was replaced by an explicit new run."));
                FinalizeRun();
            }

            ClearState();
            string runId = Guid.NewGuid().ToString("N");
            SessionState.SetString(RunIdKey, runId);
            SessionState.SetString(PhaseKey, "editor");
            SessionState.SetString(StatusKey, "Running");

            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,
                    "The canonical P3 smoke must start in Edit Mode.");
                Require(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(),
                    "The canonical P3 smoke was cancelled because open Scenes were not saved.");

                SceneSetupState previousScenes = CaptureSceneSetup();
                SessionState.SetString(SceneSetupKey, JsonUtility.ToJson(previousScenes));

                var completed = new List<string>();

                RunEditorGroup(
                    "slot-profile-authoring",
                    QaP3CPlayerProfileAuthoringSmoke.Run,
                    completed,
                    "player-slot-profiles-valid");
                RunEditorGroup(
                    "provisioning-configuration",
                    QaP3LocalPlayerProvisioningValidationSmoke.Run,
                    completed,
                    "provisioning-surface-valid",
                    "duplicate-provisioning-surface-rejected",
                    "divergent-manager-rejected");
                RunEditorGroup(
                    "activity-participation-authoring",
                    QaP3DActivityParticipationAuthoringSmoke.Run,
                    completed,
                    "activity-projection-valid");
                RunEditorGroup(
                    "session-slot-runtime",
                    QaP3FSessionSlotRuntimeSmoke.Run,
                    completed,
                    "ordered-slots-initialized",
                    "joining-closed-rejected",
                    "capacity-enforced");
                RunEditorGroup(
                    "join-contract-authoring",
                    QaP3G2JoinContractAuthoringSmoke.Run,
                    completed,
                    "join-contract-authoring-valid");
                RunEditorGroup(
                    "synthetic-provisioning-bridge",
                    QaP3G3ProvisioningBridgeSyntheticSmoke.Run,
                    completed,
                    "reservation-before-provisioning",
                    "technical-host-parented",
                    "joined-slot-committed",
                    "player-index-diagnostic-only",
                    "late-callback-confirmed",
                    "unexpected-callback-rejected",
                    "clr-null-rejected",
                    "unity-fake-null-rejected",
                    "rollback-restores-slot",
                    "single-flight-enforced");

                SessionState.SetString(CurrentCaseKey, "canonical-fixture-repair");
                RunWithoutIntermediateLogs(QaP3J6ActivityPlayerActorLifecycleSetup.Apply);
                ValidateCanonicalFixture();
                AddCompleted(completed, "canonical-fixture-repaired");

                SessionState.SetString(PhaseKey, "awaiting-play-mode");
                SessionState.SetString(CurrentCaseKey, "enter-play-mode");
                Debug.Log(
                    "[P3_CANONICAL_PREFIRSTGAME_SMOKE] status='Running' " +
                    $"runId='{runId}' phase='editor' cases='{completed.Count}' " +
                    "message='Editor phase completed; entering Play Mode.'.");
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                RecordFailure("editor", exception);
                FinalizeRun();
            }
        }

        private static void RunEditorGroup(
            string groupId,
            Action action,
            ICollection<string> completed,
            params string[] canonicalCases)
        {
            SessionState.SetString(CurrentCaseKey, groupId);
            RunWithoutIntermediateLogs(action);
            for (int index = 0; index < canonicalCases.Length; index++)
            {
                AddCompleted(completed, canonicalCases[index]);
            }
        }

        private static void RunWithoutIntermediateLogs(Action action)
        {
            bool previous = Debug.unityLogger.logEnabled;
            try
            {
                Debug.unityLogger.logEnabled = false;
                action();
            }
            finally
            {
                Debug.unityLogger.logEnabled = previous;
            }
        }

        private static void ValidateCanonicalFixture()
        {
            LocalPlayerProvisioningAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<LocalPlayerProvisioningAuthoring>(
                    FindObjectsInactive.Include);
            LocalPlayerProvisioningAuthoring authoring = candidates.SingleOrDefault(
                candidate => candidate != null &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.gameObject.scene.isLoaded);
            Require(authoring != null,
                "Fixture repair did not leave exactly one loaded LocalPlayerProvisioningAuthoring.");

            PlayerInputManager manager = authoring.PlayerInputManager;
            Require(manager != null,
                "Canonical provisioning surface has no PlayerInputManager.");
            Require(manager.gameObject == authoring.gameObject,
                "PlayerInputManager must share the provisioning surface GameObject.");
            Require(manager.joinBehavior == PlayerJoinBehavior.JoinPlayersManually,
                "Canonical PlayerInputManager is not configured for manual join.");
            Require(manager.notificationBehavior == PlayerNotifications.InvokeCSharpEvents,
                "Canonical PlayerInputManager is not configured for C# events.");
            Require(manager.maxPlayerCount > 0,
                "Canonical PlayerInputManager requires a positive technical capacity.");
            Require(manager.playerPrefab != null,
                "Canonical PlayerInputManager has no Player Prefab.");
            Require(manager.playerPrefab.GetComponent<PlayerInput>() != null,
                "Canonical Player Prefab has no PlayerInput.");
            LocalPlayerHostAuthoring host =
                manager.playerPrefab.GetComponent<LocalPlayerHostAuthoring>();
            Require(host != null && host.ActorMount != null,
                "Canonical Player Prefab has no valid LocalPlayerHostAuthoring/Actor Mount.");
            UnityPlayerInputGateAdapter gate =
                manager.playerPrefab.GetComponent<UnityPlayerInputGateAdapter>();
            Require(gate != null && gate.PlayerInput == host.PlayerInput,
                "Canonical Player Prefab has no explicit PlayerInput gate binding.");
            Require(!host.HasJoinedSlot,
                "Canonical Player Prefab pre-authors a joined Slot.");
            Require(manager.playerPrefab.GetComponents<Component>()
                    .All(component => component == null ||
                        component.GetType().Name != "PlayerSlotDeclaration"),
                "Canonical Player Prefab contains removed PlayerSlotDeclaration evidence.");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (string.IsNullOrEmpty(SessionState.GetString(RunIdKey, string.Empty)))
            {
                return;
            }

            SessionState.SetString(FailurePlayModeKey, state.ToString());
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RunPlayModePhaseAsync();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode &&
                SessionState.GetString(StatusKey, string.Empty) == "Running")
            {
                RecordFailure(
                    "play-mode",
                    new InvalidOperationException(
                        "Play Mode exited before the canonical P3 lane completed."));
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                FinalizeRun();
            }
        }

        private static async void RunPlayModePhaseAsync()
        {
            if (playModeRunnerStarted)
            {
                return;
            }

            playModeRunnerStarted = true;
            SessionState.SetString(PhaseKey, "play-mode");
            SessionState.SetString(CurrentCaseKey, "integrated-player-lane");
            try
            {
                IReadOnlyList<string> integrated =
                    await QaP3K7HRouteStartupActivityPlayerAdmissionSmoke.RunCanonicalAsync();
                Require(integrated.Contains("public-default-actor-selection"),
                    "Integrated Play Mode lane did not prove public default Actor selection.");

                var completed = ReadCompleted();
                AddCompleted(completed, "real-join-succeeded");
                AddCompleted(completed, "real-technical-host-admitted");
                AddCompleted(completed, "logical-actor-materialized");
                AddCompleted(completed, "actor-prepared");
                AddCompleted(completed, "restart-creates-new-owner-identity");
                AddCompleted(completed, "gameplay-occupancy-committed");
                AddCompleted(completed, "input-binding-committed");
                AddCompleted(completed, "camera-eligibility-committed");
                AddCompleted(completed, "gameplay-admission-committed");
                AddCompleted(completed, "activity-handoff-committed");
                AddCompleted(completed, "public-default-actor-selection");
                SessionState.SetString(StatusKey, "Passed");
                SessionState.SetString(CurrentCaseKey, string.Empty);
            }
            catch (Exception exception)
            {
                RecordFailure("play-mode", exception);
            }
            finally
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static void RecoverOrClearStaleRun()
        {
            if (string.IsNullOrEmpty(SessionState.GetString(RunIdKey, string.Empty)))
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                RunPlayModePhaseAsync();
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            string status = SessionState.GetString(StatusKey, string.Empty);
            if (status == "Passed" || status == "Failed")
            {
                FinalizeRun();
                return;
            }

            RecordFailure(
                SessionState.GetString(PhaseKey, "editor"),
                new InvalidOperationException(
                    "A stale canonical P3 run was recovered after an interrupted Editor transition."));
            FinalizeRun();
        }

        private static void RecordFailure(string phase, Exception exception)
        {
            Exception resolved = exception is System.Reflection.TargetInvocationException invocation &&
                invocation.InnerException != null
                    ? invocation.InnerException
                    : exception;
            SessionState.SetString(StatusKey, "Failed");
            SessionState.SetString(PhaseKey, phase);
            SessionState.SetString(ExceptionKey, resolved.GetType().Name);
            SessionState.SetString(MessageKey, resolved.Message ?? string.Empty);
            SessionState.SetString(
                FailureSceneKey,
                SceneManager.GetActiveScene().path ?? string.Empty);
        }

        private static void FinalizeRun()
        {
            string runId = SessionState.GetString(RunIdKey, string.Empty);
            if (string.IsNullOrEmpty(runId))
            {
                return;
            }

            string status = SessionState.GetString(StatusKey, "Failed");
            try
            {
                RestoreSceneSetup();
            }
            catch (Exception exception)
            {
                status = "Failed";
                SessionState.SetString(StatusKey, status);
                SessionState.SetString(PhaseKey, "editor");
                SessionState.SetString(CurrentCaseKey, "restore-previous-scenes");
                SessionState.SetString(ExceptionKey, exception.GetType().Name);
                SessionState.SetString(MessageKey, exception.Message ?? string.Empty);
            }

            List<string> completed = ReadCompleted();
            if (status == "Passed")
            {
                Debug.Log(
                    "[P3_CANONICAL_PREFIRSTGAME_SMOKE] " +
                    $"status='Passed' phases='2' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            else
            {
                Debug.LogError(
                    "[P3_CANONICAL_PREFIRSTGAME_SMOKE] " +
                    "status='Failed' " +
                    $"runId='{Escape(runId)}' " +
                    $"phase='{Escape(SessionState.GetString(PhaseKey, string.Empty))}' " +
                    $"case='{Escape(SessionState.GetString(CurrentCaseKey, string.Empty))}' " +
                    $"exception='{Escape(SessionState.GetString(ExceptionKey, string.Empty))}' " +
                    $"message='{Escape(SessionState.GetString(MessageKey, string.Empty))}' " +
                    $"completed='{Escape(string.Join(",", completed))}' " +
                    $"scene='{Escape(SessionState.GetString(FailureSceneKey, string.Empty))}' " +
                    $"playMode='{Escape(SessionState.GetString(FailurePlayModeKey, string.Empty))}'.");
            }

            ClearState();
            playModeRunnerStarted = false;
        }

        private static SceneSetupState CaptureSceneSetup()
        {
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            Require(setup.Length > 0, "No open Scene is available to restore after the smoke.");
            var state = new SceneSetupState
            {
                entries = new SceneSetupEntry[setup.Length]
            };
            for (int index = 0; index < setup.Length; index++)
            {
                Require(!string.IsNullOrWhiteSpace(setup[index].path),
                    "All open Scenes must be saved before running the canonical P3 smoke.");
                state.entries[index] = new SceneSetupEntry
                {
                    path = setup[index].path,
                    isLoaded = setup[index].isLoaded,
                    isActive = setup[index].isActive
                };
            }

            return state;
        }

        private static void RestoreSceneSetup()
        {
            string json = SessionState.GetString(SceneSetupKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            SceneSetupState state = JsonUtility.FromJson<SceneSetupState>(json);
            if (state?.entries == null || state.entries.Length == 0)
            {
                throw new InvalidOperationException(
                    "The previous Editor Scene setup could not be restored.");
            }

            var setup = new SceneSetup[state.entries.Length];
            for (int index = 0; index < setup.Length; index++)
            {
                setup[index] = new SceneSetup
                {
                    path = state.entries[index].path,
                    isLoaded = state.entries[index].isLoaded,
                    isActive = state.entries[index].isActive
                };
            }

            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        private static void AddCompleted(ICollection<string> completed, string caseId)
        {
            if (!completed.Contains(caseId))
            {
                completed.Add(caseId);
            }

            SessionState.SetString(CompletedKey, string.Join("|", completed));
        }

        private static List<string> ReadCompleted()
        {
            string serialized = SessionState.GetString(CompletedKey, string.Empty);
            return string.IsNullOrEmpty(serialized)
                ? new List<string>()
                : serialized.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
        }

        private static void ClearState()
        {
            string[] keys =
            {
                RunIdKey,
                PhaseKey,
                CurrentCaseKey,
                CompletedKey,
                SceneSetupKey,
                StatusKey,
                ExceptionKey,
                MessageKey,
                FailureSceneKey,
                FailurePlayModeKey
            };
            for (int index = 0; index < keys.Length; index++)
            {
                SessionState.EraseString(keys[index]);
            }
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
                : value.Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }

        [Serializable]
        private sealed class SceneSetupState
        {
            public SceneSetupEntry[] entries;
        }

        [Serializable]
        private sealed class SceneSetupEntry
        {
            public string path;
            public bool isLoaded;
            public bool isActive;
        }
    }
}

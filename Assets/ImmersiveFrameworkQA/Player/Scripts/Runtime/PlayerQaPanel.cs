using System.Collections;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    /// <summary>
    /// QA-only IMGUI panel for the canonical Player functional.
    /// Official Framework components own Player behaviour; this panel only
    /// coordinates already-authored consumers and records evidence.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/Player QA Panel")]
    public sealed class PlayerQaPanel : MonoBehaviour
    {
        public const string LogPrefix = "[QA_PLAYER_FULL]";

        [Header("Scoped Access")]
        [SerializeField] private PlayerQaScopedAccessProbe probe;
        [SerializeField] private PlayerSessionObserver observer;
        [SerializeField] private PlayerQaScopedAccessProbe activityScopeProbe;

        [Header("Commands")]
        [SerializeField] private PlayerSessionJoinCommandTrigger joinCommand;
        [SerializeField] private PlayerSessionLeaveCommandTrigger leaveCommand;
        [SerializeField] private PlayerSessionSelectActorCommandTrigger selectActorCommand;
        [SerializeField] private PlayerSessionDefaultActorSelectionCommandTrigger defaultActorCommand;
        [SerializeField] private PlayerSessionReplaceActorSelectionCommandTrigger replaceActorCommand;
        [SerializeField] private PlayerSessionClearActorSelectionCommandTrigger clearActorCommand;
        [SerializeField] private PlayerSessionOpenJoiningCommandTrigger openJoiningCommand;
        [SerializeField] private PlayerSessionCloseJoiningCommandTrigger closeJoiningCommand;

        [Header("Expected Fixtures")]
        [SerializeField] private LocalPlayerHostAuthoring managerHostTemplate;
        [SerializeField] private PlayerSlotProfile playerOneSlot;
        [SerializeField] private PlayerSlotProfile playerTwoSlot;
        [SerializeField] private ActorProfile defaultActor;
        [SerializeField] private ActorProfile alternateActor;
        [SerializeField] private RoutePlayerSpatialEntryAuthoring spatialEntry;
        [SerializeField] private ActivityPlayerRelocationAuthoring relocation;
        [SerializeField] private ActivityAsset startupActivity;
        [SerializeField] private ActivityAsset relocateActivity;

        [Header("Navigation")]
        [SerializeField] private ActivityRequestTrigger relocateActivityTrigger;
        [SerializeField] private RouteRequestTrigger hubRouteTrigger;

        [Header("Panel")]
        [SerializeField] private string title = "Player QA";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 560f, 640f);

        private string lastResult = "Player QA ready.";
        private bool lastPassed = true;
        private bool running;
        private Vector2 scroll;

        public PlayerQaScopedAccessProbe Probe => probe;
        public PlayerSessionObserver Observer => observer;
        public PlayerQaScopedAccessProbe ActivityScopeProbe => activityScopeProbe;
        public PlayerSessionJoinCommandTrigger JoinCommand => joinCommand;
        public PlayerSessionLeaveCommandTrigger LeaveCommand => leaveCommand;
        public PlayerSessionSelectActorCommandTrigger SelectActorCommand =>
            selectActorCommand;
        public PlayerSessionDefaultActorSelectionCommandTrigger DefaultActorCommand =>
            defaultActorCommand;
        public PlayerSessionReplaceActorSelectionCommandTrigger ReplaceActorCommand =>
            replaceActorCommand;
        public PlayerSessionClearActorSelectionCommandTrigger ClearActorCommand =>
            clearActorCommand;
        public PlayerSessionOpenJoiningCommandTrigger OpenJoiningCommand =>
            openJoiningCommand;
        public PlayerSessionCloseJoiningCommandTrigger CloseJoiningCommand =>
            closeJoiningCommand;
        public LocalPlayerHostAuthoring ManagerHostTemplate => managerHostTemplate;
        public PlayerSlotProfile PlayerOneSlot => playerOneSlot;
        public PlayerSlotProfile PlayerTwoSlot => playerTwoSlot;
        public ActorProfile DefaultActor => defaultActor;
        public ActorProfile AlternateActor => alternateActor;
        public RoutePlayerSpatialEntryAuthoring SpatialEntry => spatialEntry;
        public ActivityPlayerRelocationAuthoring Relocation => relocation;
        public ActivityAsset StartupActivity => startupActivity;
        public ActivityAsset RelocateActivity => relocateActivity;
        public ActivityRequestTrigger RelocateActivityTrigger => relocateActivityTrigger;

        public void Configure(
            PlayerQaScopedAccessProbe configuredProbe,
            PlayerSessionObserver configuredObserver,
            PlayerQaScopedAccessProbe configuredActivityScopeProbe,
            PlayerSessionJoinCommandTrigger configuredJoin,
            PlayerSessionLeaveCommandTrigger configuredLeave,
            PlayerSessionSelectActorCommandTrigger configuredSelectActor,
            PlayerSessionDefaultActorSelectionCommandTrigger configuredDefaultActor,
            PlayerSessionReplaceActorSelectionCommandTrigger configuredReplace,
            PlayerSessionClearActorSelectionCommandTrigger configuredClear,
            PlayerSessionOpenJoiningCommandTrigger configuredOpenJoining,
            PlayerSessionCloseJoiningCommandTrigger configuredCloseJoining,
            LocalPlayerHostAuthoring configuredManagerHost,
            PlayerSlotProfile configuredPlayerOne,
            PlayerSlotProfile configuredPlayerTwo,
            ActorProfile configuredDefaultActorProfile,
            ActorProfile configuredAlternateActor,
            RoutePlayerSpatialEntryAuthoring configuredSpatial,
            ActivityPlayerRelocationAuthoring configuredRelocation,
            ActivityAsset configuredStartupActivity,
            ActivityAsset configuredRelocateActivity,
            ActivityRequestTrigger configuredRelocateActivityTrigger,
            RouteRequestTrigger configuredHubTrigger)
        {
            probe = configuredProbe;
            observer = configuredObserver;
            activityScopeProbe = configuredActivityScopeProbe;
            joinCommand = configuredJoin;
            leaveCommand = configuredLeave;
            selectActorCommand = configuredSelectActor;
            defaultActorCommand = configuredDefaultActor;
            replaceActorCommand = configuredReplace;
            clearActorCommand = configuredClear;
            openJoiningCommand = configuredOpenJoining;
            closeJoiningCommand = configuredCloseJoining;
            managerHostTemplate = configuredManagerHost;
            playerOneSlot = configuredPlayerOne;
            playerTwoSlot = configuredPlayerTwo;
            defaultActor = configuredDefaultActorProfile;
            alternateActor = configuredAlternateActor;
            spatialEntry = configuredSpatial;
            relocation = configuredRelocation;
            startupActivity = configuredStartupActivity;
            relocateActivity = configuredRelocateActivity;
            relocateActivityTrigger = configuredRelocateActivityTrigger;
            hubRouteTrigger = configuredHubTrigger;
        }

        public void RunAllPlayerQa()
        {
            if (running)
            {
                SetResult(false, "Player QA is already running.");
                return;
            }

            StartCoroutine(RunAllRoutine());
        }

        public bool IsRunning => running;

        private IEnumerator RunAllRoutine()
        {
            running = true;
            lastPassed = false;
            lastResult = "Running Full Player QA.";
            PlayerQaSuite.Result suiteResult = null;
            yield return PlayerQaSuite.Run(this, result => suiteResult = result);

            bool ok = suiteResult != null && suiteResult.Ok;
            string completed = suiteResult != null
                ? string.Join(",", suiteResult.Completed)
                : string.Empty;
            string summary = suiteResult == null
                ? "Player QA suite did not complete."
                : ok
                    ? $"{LogPrefix} status='Passed' verdict='PLAYER QA CERTIFIED' " +
                      $"cases='{suiteResult.Passed}/{suiteResult.Passed}' " +
                      $"completed='{completed}'."
                    : $"{LogPrefix} status='Failed' verdict='PLAYER QA NOT CERTIFIED' " +
                      $"failedCase='{suiteResult.FailedCase}' " +
                      $"passed='{suiteResult.Passed}' failed='{suiteResult.Failed}' " +
                      $"completed='{completed}' " +
                      $"message='{Escape(suiteResult.FailureMessage)}'.";

            SetResult(ok, summary);
            if (ok)
            {
                Debug.Log(summary, this);
            }
            else
            {
                Debug.LogError(summary, this);
            }

            running = false;
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            panelRect = GUI.Window(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this),
                panelRect,
                DrawWindow,
                title);
        }

        private void DrawWindow(int windowId)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label($"Scene: {gameObject.scene.name}");
            GUILayout.Label(running ? "Status: Running" : "Status: Ready");
            GUILayout.Label(
                lastResult,
                new GUIStyle(GUI.skin.label) { wordWrap = true });
            GUILayout.Space(8f);

            GUI.enabled = !running;
            if (GUILayout.Button("Run All Player QA", GUILayout.Height(36f)))
            {
                RunAllPlayerQa();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Focused commands");
            if (GUILayout.Button("Join"))
            {
                Invoke(joinCommand);
            }

            if (GUILayout.Button("Select Actor"))
            {
                Invoke(selectActorCommand);
            }

            if (GUILayout.Button("Select Default Actor"))
            {
                Invoke(defaultActorCommand);
            }

            if (GUILayout.Button("Replace Actor"))
            {
                Invoke(replaceActorCommand);
            }

            if (GUILayout.Button("Clear Actor"))
            {
                Invoke(clearActorCommand);
            }

            if (GUILayout.Button("Leave"))
            {
                Invoke(leaveCommand);
            }

            if (GUILayout.Button("Open Joining"))
            {
                Invoke(openJoiningCommand);
            }

            if (GUILayout.Button("Close Joining"))
            {
                Invoke(closeJoiningCommand);
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("Back to Hub"))
            {
                if (hubRouteTrigger != null)
                {
                    hubRouteTrigger.RequestRoute();
                }
                else
                {
                    SetResult(false, "QA Hub route trigger is missing.");
                }
            }

            GUI.enabled = true;
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void Invoke(PlayerSessionCommandTriggerBase command)
        {
            if (command == null)
            {
                SetResult(false, "Command trigger is missing.");
                return;
            }

            command.Invoke();
            SetResult(true, $"{command.GetType().Name}: {command.LastOutcome} {command.LastDiagnostic}");
        }

        private void SetResult(bool passed, string message)
        {
            lastPassed = passed;
            lastResult = message ?? string.Empty;
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

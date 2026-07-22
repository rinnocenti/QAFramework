using System.Globalization;
using Immersive.Framework.Pause;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.PauseP1
{
    [DisallowMultipleComponent]
    public sealed class PauseRuntimeEvidencePanel : MonoBehaviour
    {
        private const string LogPrefix = "[PAUSE_RUNTIME_EVIDENCE]";

        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PausePlayerInputBinding pauseBinding;
        [SerializeField]
        private Rect panelRect = new Rect(500f, 330f, 500f, 300f);

        private bool hasObservedPosture;
        private ObservedPosture lastObservedPosture;

        public PlayerInput PlayerInput => playerInput;
        public PausePlayerInputBinding PauseBinding => pauseBinding;

        public void Configure(
            PlayerInput nextPlayerInput,
            PausePlayerInputBinding nextPauseBinding)
        {
            playerInput = nextPlayerInput;
            pauseBinding = nextPauseBinding;
        }

        private void Update()
        {
            EvidenceSnapshot snapshot = CaptureSnapshot();
            if (hasObservedPosture &&
                snapshot.Posture == lastObservedPosture)
            {
                return;
            }

            hasObservedPosture = true;
            lastObservedPosture = snapshot.Posture;
            Debug.Log(
                $"{LogPrefix} posture='{snapshot.Posture}' " +
                $"binding='{snapshot.BindingLogStatus}' " +
                $"activeBinding='{snapshot.HasActiveBinding}' " +
                $"timeScale='{FormatTimeScale(snapshot.TimeScale)}' " +
                $"global='{snapshot.GlobalEnabled}' " +
                $"gameplay='{snapshot.GameplayEnabled}' " +
                $"ui='{snapshot.UiEnabled}' " +
                $"pauseAction='{snapshot.PauseActionEnabled}'",
                this);
        }

        private EvidenceSnapshot CaptureSnapshot()
        {
            InputActionMap globalMap = ResolveMap(
                pauseBinding != null
                    ? pauseBinding.GlobalActionMapName
                    : string.Empty);
            InputActionMap gameplayMap = ResolveMap(
                pauseBinding != null
                    ? pauseBinding.GameplayActionMapName
                    : string.Empty);
            InputActionMap uiMap = ResolveMap(
                pauseBinding != null
                    ? pauseBinding.UiActionMapName
                    : string.Empty);
            InputAction runtimePauseAction = ResolvePauseAction();

            bool globalEnabled = globalMap != null && globalMap.enabled;
            bool gameplayEnabled = gameplayMap != null && gameplayMap.enabled;
            bool uiEnabled = uiMap != null && uiMap.enabled;
            float timeScale = Time.timeScale;
            ObservedPosture posture = ResolvePosture(
                globalMap,
                gameplayMap,
                uiMap,
                globalEnabled,
                gameplayEnabled,
                uiEnabled,
                timeScale);

            bool activeBinding =
                pauseBinding != null && pauseBinding.HasActiveBinding;
            string bindingStatus = pauseBinding != null
                ? pauseBinding.BindingStatus
                : "Unavailable";
            return new EvidenceSnapshot(
                posture,
                bindingStatus,
                ResolveBindingLogStatus(bindingStatus, activeBinding),
                pauseBinding != null
                    ? pauseBinding.BindingDiagnostic
                    : "Pause binding reference is missing.",
                activeBinding,
                playerInput != null && playerInput.currentActionMap != null
                    ? playerInput.currentActionMap.name
                    : "None",
                globalEnabled,
                gameplayEnabled,
                uiEnabled,
                runtimePauseAction != null && runtimePauseAction.enabled,
                timeScale);
        }

        private InputActionMap ResolveMap(string mapName)
        {
            if (playerInput == null ||
                playerInput.actions == null ||
                string.IsNullOrWhiteSpace(mapName))
            {
                return null;
            }

            return playerInput.actions.FindActionMap(mapName, false);
        }

        private InputAction ResolvePauseAction()
        {
            if (playerInput == null ||
                playerInput.actions == null ||
                pauseBinding == null ||
                pauseBinding.PauseAction == null ||
                pauseBinding.PauseAction.action == null)
            {
                return null;
            }

            return playerInput.actions.FindAction(
                pauseBinding.PauseAction.action.id.ToString(),
                false);
        }

        private static ObservedPosture ResolvePosture(
            InputActionMap globalMap,
            InputActionMap gameplayMap,
            InputActionMap uiMap,
            bool globalEnabled,
            bool gameplayEnabled,
            bool uiEnabled,
            float timeScale)
        {
            if (globalMap == null || gameplayMap == null || uiMap == null)
            {
                return ObservedPosture.Unknown;
            }

            if (globalEnabled &&
                gameplayEnabled &&
                !uiEnabled &&
                timeScale > 0f)
            {
                return ObservedPosture.Running;
            }

            if (globalEnabled &&
                !gameplayEnabled &&
                uiEnabled &&
                timeScale == 0f)
            {
                return ObservedPosture.Paused;
            }

            return ObservedPosture.Unknown;
        }

        private static string ResolveBindingLogStatus(
            string bindingStatus,
            bool activeBinding)
        {
            if (activeBinding)
            {
                return "Bound";
            }

            return string.Equals(
                    bindingStatus,
                    "Failed",
                    System.StringComparison.Ordinal)
                ? "Failed"
                : "Unbound";
        }

        private void OnGUI()
        {
            panelRect = GUI.Window(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this),
                panelRect,
                DrawWindow,
                "Pause Runtime Evidence");
        }

        private void DrawWindow(int windowId)
        {
            EvidenceSnapshot snapshot = CaptureSnapshot();
            var wrapped = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };

            GUILayout.Space(8f);
            GUILayout.Label($"Binding Status: {snapshot.BindingStatus}");
            GUILayout.Label(
                $"Binding Diagnostic: {snapshot.BindingDiagnostic}",
                wrapped);
            GUILayout.Label(
                $"Has Active Binding: {snapshot.HasActiveBinding}");
            GUILayout.Label(
                $"Current Action Map: {snapshot.CurrentActionMap}");
            GUILayout.Label($"Global Map Enabled: {snapshot.GlobalEnabled}");
            GUILayout.Label(
                $"Gameplay Map Enabled: {snapshot.GameplayEnabled}");
            GUILayout.Label($"UI Map Enabled: {snapshot.UiEnabled}");
            GUILayout.Label(
                $"Pause Action Enabled: {snapshot.PauseActionEnabled}");
            GUILayout.Label(
                $"Time.timeScale: {FormatTimeScale(snapshot.TimeScale)}");
            GUILayout.Label($"Observed Posture: {snapshot.Posture}");
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static string FormatTimeScale(float value) =>
            value.ToString("0.###", CultureInfo.InvariantCulture);

        private enum ObservedPosture
        {
            Unknown = 0,
            Running = 10,
            Paused = 20
        }

        private readonly struct EvidenceSnapshot
        {
            internal EvidenceSnapshot(
                ObservedPosture posture,
                string bindingStatus,
                string bindingLogStatus,
                string bindingDiagnostic,
                bool hasActiveBinding,
                string currentActionMap,
                bool globalEnabled,
                bool gameplayEnabled,
                bool uiEnabled,
                bool pauseActionEnabled,
                float timeScale)
            {
                Posture = posture;
                BindingStatus = bindingStatus ?? string.Empty;
                BindingLogStatus = bindingLogStatus ?? string.Empty;
                BindingDiagnostic = bindingDiagnostic ?? string.Empty;
                HasActiveBinding = hasActiveBinding;
                CurrentActionMap = currentActionMap ?? string.Empty;
                GlobalEnabled = globalEnabled;
                GameplayEnabled = gameplayEnabled;
                UiEnabled = uiEnabled;
                PauseActionEnabled = pauseActionEnabled;
                TimeScale = timeScale;
            }

            internal ObservedPosture Posture { get; }
            internal string BindingStatus { get; }
            internal string BindingLogStatus { get; }
            internal string BindingDiagnostic { get; }
            internal bool HasActiveBinding { get; }
            internal string CurrentActionMap { get; }
            internal bool GlobalEnabled { get; }
            internal bool GameplayEnabled { get; }
            internal bool UiEnabled { get; }
            internal bool PauseActionEnabled { get; }
            internal float TimeScale { get; }
        }
    }
}

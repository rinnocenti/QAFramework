using Immersive.Framework.Pause;
using UnityEngine;

namespace ImmersiveFrameworkQA.UnityBuildSurface
{
    /// <summary>
    /// QA-only Pause surface adapter for the canonical UIGlobal scene.
    /// It presents logical Pause state and offers IMGUI buttons for manual validation.
    /// It does not own PauseRuntime, input binding, Time.timeScale, Gate, Route or Activity lifecycle.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PauseRequestTrigger))]
    [AddComponentMenu("Immersive Framework QA/Unity Build Surface/QA Pause Surface Adapter")]
    public sealed class QaPauseSurfaceAdapter : MonoBehaviour, IPauseSurfaceAdapter
    {
        [Header("Surface")]
        [SerializeField] private string adapterName = "QA Pause Surface Adapter";
        [SerializeField] private bool showOverlayWhenPaused = true;
        [SerializeField] private bool showManualQaControls = true;
        [SerializeField] private Rect overlayRect = new Rect(16f, 360f, 420f, 160f);
        [SerializeField] private string title = "Pause Surface QA";
        [SerializeField] private string pausedLabel = "PAUSED";
        [SerializeField] private string runningLabel = "RUNNING";

        [Header("Runtime Diagnostics")]
        [SerializeField] private PauseState lastState = PauseState.Unknown;
        [SerializeField] private bool lastPaused;
        [SerializeField] private string lastReason = string.Empty;

        private PauseRequestTrigger requestTrigger;

        public string AdapterName => string.IsNullOrWhiteSpace(adapterName)
            ? nameof(QaPauseSurfaceAdapter)
            : adapterName.Trim();

        public PauseState LastState => lastState;

        public bool LastPaused => lastPaused;

        public string LastReason => string.IsNullOrWhiteSpace(lastReason) ? string.Empty : lastReason;

        public bool Supports(PauseSnapshot snapshot)
        {
            return snapshot.IsValid;
        }

        public void Apply(PauseSnapshot snapshot)
        {
            if (!snapshot.IsValid)
            {
                return;
            }

            lastState = snapshot.State;
            lastPaused = snapshot.IsPaused;
            lastReason = snapshot.Reason;
        }

        private void Awake()
        {
            requestTrigger = GetComponent<PauseRequestTrigger>();
        }

        private void Reset()
        {
            requestTrigger = GetComponent<PauseRequestTrigger>();
        }

        private void OnGUI()
        {
            if (!showManualQaControls && (!showOverlayWhenPaused || !lastPaused))
            {
                return;
            }

            if (!showOverlayWhenPaused && !showManualQaControls)
            {
                return;
            }

            if (!lastPaused && !showManualQaControls)
            {
                return;
            }

            GUILayout.BeginArea(overlayRect, GUI.skin.box);
            GUILayout.Label(title, GUI.skin.label);
            GUILayout.Label(lastPaused ? pausedLabel : runningLabel, GUI.skin.label);

            if (!string.IsNullOrWhiteSpace(lastReason))
            {
                GUILayout.Label($"Reason: {lastReason}", GUI.skin.label);
            }

            if (showManualQaControls)
            {
                DrawControls();
            }

            GUILayout.EndArea();
        }

        private void DrawControls()
        {
            ResolveTrigger();
            if (requestTrigger == null)
            {
                GUILayout.Label("PauseRequestTrigger missing.", GUI.skin.label);
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause", GUILayout.Height(28f)))
            {
                requestTrigger.RequestPause();
            }

            if (GUILayout.Button("Resume", GUILayout.Height(28f)))
            {
                requestTrigger.RequestResume();
            }

            if (GUILayout.Button("Toggle", GUILayout.Height(28f)))
            {
                requestTrigger.TogglePause();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Last: {requestTrigger.LastOutcome} / {requestTrigger.LastStatus}", GUI.skin.label);
        }

        private void ResolveTrigger()
        {
            if (requestTrigger == null)
            {
                requestTrigger = GetComponent<PauseRequestTrigger>();
            }
        }
    }
}

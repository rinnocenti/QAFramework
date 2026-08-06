using Immersive.Framework.Pause;
using UnityEngine;

namespace ImmersiveFrameworkQA.PauseP1
{
    [DisallowMultipleComponent]
    [AddComponentMenu(
        "Immersive Framework QA/Pause/Scene Request Binding Probe")]
    public sealed class PauseSceneRequestBindingProbe : MonoBehaviour
    {
        [SerializeField]
        private PauseRequestTrigger pauseRequestTrigger;

        [SerializeField]
        private string scopeLabel = "Scene";

        [SerializeField]
        private Rect panelRect =
            new Rect(16f, 220f, 440f, 250f);

        [SerializeField]
        private bool showPanel = true;

        public PauseRequestTrigger PauseRequestTrigger =>
            pauseRequestTrigger;

        public string ScopeLabel =>
            string.IsNullOrWhiteSpace(scopeLabel)
                ? "Scene"
                : scopeLabel.Trim();

        public void Configure(
            PauseRequestTrigger trigger,
            string label,
            Rect rect)
        {
            pauseRequestTrigger = trigger;
            scopeLabel = string.IsNullOrWhiteSpace(label)
                ? "Scene"
                : label.Trim();
            panelRect = rect;
            showPanel = true;
        }

        public void RequestPause()
        {
            pauseRequestTrigger?.RequestPause();
        }

        public void RequestResume()
        {
            pauseRequestTrigger?.RequestResume();
        }

        public void TogglePause()
        {
            pauseRequestTrigger?.TogglePause();
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            panelRect = GUI.Window(
                System.Runtime.CompilerServices.RuntimeHelpers
                    .GetHashCode(this),
                panelRect,
                DrawWindow,
                $"{ScopeLabel} Pause Request");
        }

        private void DrawWindow(int windowId)
        {
            var wrapped = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };

            GUILayout.Space(8f);
            GUILayout.Label(
                $"Request Port: " +
                $"{ResolveBindingStatus()}");
            GUILayout.Label(
                ResolveBindingDiagnostic(),
                wrapped);
            GUILayout.Label(
                $"Logical Pause: {ResolvePauseState()}");

            GUILayout.Space(8f);
            if (GUILayout.Button("Pause"))
            {
                RequestPause();
            }

            if (GUILayout.Button("Resume"))
            {
                RequestResume();
            }

            if (GUILayout.Button("Toggle"))
            {
                TogglePause();
            }

            GUI.DragWindow(
                new Rect(0f, 0f, 10000f, 24f));
        }

        private string ResolveBindingStatus() =>
            pauseRequestTrigger != null
                ? pauseRequestTrigger.ProductRequestBindingStatus
                : "Missing Trigger";

        private string ResolveBindingDiagnostic() =>
            pauseRequestTrigger != null
                ? pauseRequestTrigger.ProductRequestBindingDiagnostic
                : "No PauseRequestTrigger reference.";

        private string ResolvePauseState()
        {
            if (pauseRequestTrigger == null ||
                !pauseRequestTrigger.TryGetPauseSnapshot(
                    out PauseSnapshot snapshot))
            {
                return "Unavailable";
            }

            return snapshot.State.ToString();
        }
    }
}

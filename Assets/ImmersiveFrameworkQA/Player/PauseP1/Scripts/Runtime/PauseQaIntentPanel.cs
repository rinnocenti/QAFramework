using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.PauseP1
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Pause/Pause Intent Panel")]
    public sealed class PauseQaIntentPanel : MonoBehaviour
    {
        [SerializeField] private string title = "Pause Activity Intent";
        [SerializeField] private string state = "Required";
        [SerializeField] private string instructions =
            "Uses the framework-provisioned Local Player.";
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 460f, 170f);

        public void Configure(
            string nextTitle,
            string nextState,
            string nextInstructions)
        {
            title = string.IsNullOrWhiteSpace(nextTitle)
                ? "Pause Activity Intent"
                : nextTitle;
            state = string.IsNullOrWhiteSpace(nextState)
                ? "Required"
                : nextState;
            instructions = nextInstructions ?? string.Empty;
        }

        private void OnGUI()
        {
            panelRect = GUI.Window(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this),
                panelRect,
                DrawWindow,
                title);
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(8f);
            GUILayout.Label($"Pause binding: {state}");
            var wrapped = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label(instructions, wrapped);
            GUILayout.Space(8f);
            GUILayout.Label("PlayerInput owner: P3G4_LocalPlayerHost", wrapped);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }
    }
}

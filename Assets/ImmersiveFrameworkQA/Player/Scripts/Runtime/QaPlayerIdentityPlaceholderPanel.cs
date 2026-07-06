using UnityEngine;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/Player Identity Placeholder Panel")]
    public sealed class QaPlayerIdentityPlaceholderPanel : MonoBehaviour
    {
        [SerializeField] private string title = "Player Identity QA";
        [SerializeField] private string message = "Placeholder only. Future Player identity QA will live here. No gameplay or project-specific integration is present.";
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 760f, 180f);

        private void OnGUI()
        {
            panelRect = ClampToScreen(GUI.Window(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), panelRect, DrawWindow, title));
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(8f);
            GUILayout.Label(message, GUI.skin.label);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(280f, rect.width);
            float height = Mathf.Max(100f, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(Mathf.Clamp(rect.x, 0f, maxX), Mathf.Clamp(rect.y, 0f, maxY), width, height);
        }
    }
}

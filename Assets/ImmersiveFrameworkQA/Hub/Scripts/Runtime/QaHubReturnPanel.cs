using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Hub
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Hub/QA Hub Return Panel")]
    public sealed class QaHubReturnPanel : MonoBehaviour
    {
        [SerializeField] private RouteRequestTrigger hubRouteTrigger;
        [SerializeField] private string title = "QA Regression";
        [SerializeField] private string runSurface = "Use the public regression menu.";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool restrictToActiveScene = true;
        [SerializeField] private Rect panelRect = new Rect(500f, 16f, 420f, 180f);

        private string lastAction = "Scene ready.";

        public void Configure(
            RouteRequestTrigger nextHubRouteTrigger,
            string nextTitle,
            string nextRunSurface)
        {
            hubRouteTrigger = nextHubRouteTrigger;
            title = string.IsNullOrWhiteSpace(nextTitle) ? "QA Regression" : nextTitle;
            runSurface = string.IsNullOrWhiteSpace(nextRunSurface)
                ? "Use the public regression menu."
                : nextRunSurface;
            lastAction = "Scene ready.";
        }

        public void BackToHub()
        {
            if (hubRouteTrigger == null || hubRouteTrigger.TargetRoute == null)
            {
                lastAction = "QA Hub route is missing.";
                Debug.LogError($"[QA_HUB_RETURN] {lastAction}", this);
                return;
            }

            lastAction = $"Requested route: {hubRouteTrigger.TargetRoute.RouteName}";
            hubRouteTrigger.RequestRoute();
        }

        private void OnGUI()
        {
            if (!showPanel || !ShouldDrawForCurrentScene())
            {
                return;
            }

            panelRect = ClampToScreen(
                GUI.Window(
                    System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this),
                    panelRect,
                    DrawWindow,
                    title));
        }

        private bool ShouldDrawForCurrentScene()
        {
            if (!restrictToActiveScene)
            {
                return true;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid() && activeScene == gameObject.scene;
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(8f);
            GUILayout.Label("Run Regression", GUI.skin.label);
            var wrappedLabel = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
            GUILayout.Label(runSurface, wrappedLabel);
            GUILayout.Space(8f);

            bool canReturn = hubRouteTrigger != null &&
                hubRouteTrigger.TargetRoute != null &&
                !hubRouteTrigger.IsRequestInFlight;
            GUI.enabled = canReturn;
            if (GUILayout.Button("Back to QA Hub", GUILayout.Height(32f)))
            {
                BackToHub();
            }

            GUI.enabled = true;
            GUILayout.Label(lastAction, GUI.skin.label);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(320f, rect.width);
            float height = Mathf.Max(160f, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(
                Mathf.Clamp(rect.x, 0f, maxX),
                Mathf.Clamp(rect.y, 0f, maxY),
                width,
                height);
        }
    }
}

using System;
using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Hub
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Hub/QA Hub Panel")]
    public sealed class QaHubPanel : MonoBehaviour
    {
        [Serializable]
        public struct QaHubEntry
        {
            [SerializeField] private string label;
            [SerializeField] private RouteRequestTrigger routeRequestTrigger;

            public QaHubEntry(string label, RouteRequestTrigger routeRequestTrigger)
            {
                this.label = label;
                this.routeRequestTrigger = routeRequestTrigger;
            }

            public string Label => label;
            public RouteRequestTrigger RouteRequestTrigger => routeRequestTrigger;
        }

        private const float EntryButtonHeight = 34f;
        private const float MinimumPanelWidth = 240f;
        private const float MinimumPanelHeight = 120f;

        [SerializeField] private QaHubEntry[] entries = Array.Empty<QaHubEntry>();
        [SerializeField] private string title = "Immersive Framework QA Hub";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool restrictToActiveScene = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 520f, 360f);

        private string lastResult = "Select a QA route.";
        private bool lastResultIsError;
        private Vector2 scrollPosition;

        public void Configure(QaHubEntry[] nextEntries, string nextTitle)
        {
            entries = nextEntries ?? Array.Empty<QaHubEntry>();
            title = string.IsNullOrWhiteSpace(nextTitle) ? "Immersive Framework QA Hub" : nextTitle;
            lastResult = "Select a QA route.";
            lastResultIsError = false;
            scrollPosition = Vector2.zero;
        }

        public void RequestRoute(RouteRequestTrigger trigger)
        {
            if (trigger == null)
            {
                SetResult("Route trigger is missing.", true);
                return;
            }

            if (trigger.TargetRoute == null)
            {
                SetResult("Target route is missing.", true);
                return;
            }

            SetResult($"Requested route: {trigger.TargetRoute.RouteName}", false);
            trigger.RequestRoute();
        }

        private void SetResult(string message, bool isError)
        {
            lastResult = message ?? string.Empty;
            lastResultIsError = isError;

            if (isError)
            {
                Debug.LogError($"[QA_HUB] {lastResult}", this);
                return;
            }

            Debug.Log($"[QA_HUB] {lastResult}", this);
        }

        private void OnGUI()
        {
            if (!showPanel || !ShouldDrawForCurrentScene())
            {
                return;
            }

            panelRect = ClampToScreen(GUI.Window(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), panelRect, DrawWindow, title));
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

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            for (int i = 0; i < entries.Length; i++)
            {
                QaHubEntry entry = entries[i];
                RouteRequestTrigger trigger = entry.RouteRequestTrigger;
                string fallbackLabel = trigger != null && trigger.TargetRoute != null
                    ? trigger.TargetRoute.RouteName
                    : "Missing Route";
                string label = string.IsNullOrWhiteSpace(entry.Label) ? fallbackLabel : entry.Label;
                GUI.enabled = trigger != null && trigger.TargetRoute != null && !trigger.IsRequestInFlight;
                if (GUILayout.Button(label, GUILayout.Height(EntryButtonHeight)))
                {
                    RequestRoute(trigger);
                }
            }

            GUILayout.EndScrollView();

            GUI.enabled = true;
            GUILayout.Space(8f);
            Color previousColor = GUI.color;
            GUI.color = lastResultIsError ? Color.red : previousColor;
            GUILayout.Label(lastResult, GUI.skin.label);
            GUI.color = previousColor;
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(MinimumPanelWidth, rect.width);
            float height = Mathf.Max(MinimumPanelHeight, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(Mathf.Clamp(rect.x, 0f, maxX), Mathf.Clamp(rect.y, 0f, maxY), width, height);
        }
    }
}

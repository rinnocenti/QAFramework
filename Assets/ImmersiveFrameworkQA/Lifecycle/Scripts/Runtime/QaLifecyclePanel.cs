using System;
using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Lifecycle
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Lifecycle/QA Lifecycle Panel")]
    public sealed class QaLifecyclePanel : MonoBehaviour
    {
        [Header("Routes")]
        [SerializeField] private RouteRequestTrigger routeATrigger;
        [SerializeField] private RouteRequestTrigger routeBTrigger;
        [SerializeField] private RouteRequestTrigger noActivityRouteTrigger;
        [SerializeField] private RouteRequestTrigger hubRouteTrigger;

        [Header("Activities")]
        [SerializeField] private ActivityRequestTrigger activityATrigger;
        [SerializeField] private ActivityRequestTrigger activityBTrigger;
        [SerializeField] private ActivityRequestTrigger noContentActivityTrigger;
        [SerializeField] private ActivityRequestTrigger clearActivityTrigger;

        [Header("Panel")]
        [SerializeField] private string title = "QA Lifecycle";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool restrictToActiveLifecycleScene = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 520f, 500f);

        private string lastAction = "Lifecycle QA ready.";
        private Vector2 scroll;

        public void Configure(
            RouteRequestTrigger nextRouteATrigger,
            RouteRequestTrigger nextRouteBTrigger,
            RouteRequestTrigger nextNoActivityRouteTrigger,
            RouteRequestTrigger nextHubRouteTrigger,
            ActivityRequestTrigger nextActivityATrigger,
            ActivityRequestTrigger nextActivityBTrigger,
            ActivityRequestTrigger nextNoContentActivityTrigger,
            ActivityRequestTrigger nextClearActivityTrigger,
            string nextTitle)
        {
            routeATrigger = nextRouteATrigger;
            routeBTrigger = nextRouteBTrigger;
            noActivityRouteTrigger = nextNoActivityRouteTrigger;
            hubRouteTrigger = nextHubRouteTrigger;
            activityATrigger = nextActivityATrigger;
            activityBTrigger = nextActivityBTrigger;
            noContentActivityTrigger = nextNoContentActivityTrigger;
            clearActivityTrigger = nextClearActivityTrigger;
            title = string.IsNullOrWhiteSpace(nextTitle) ? "QA Lifecycle" : nextTitle;
        }

        private void OnGUI()
        {
            if (!showPanel || !ShouldDrawForCurrentScene())
            {
                return;
            }

            panelRect = ClampToScreen(GUI.Window(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), panelRect, DrawWindow, title));
            GUI.enabled = true;
        }

        private bool ShouldDrawForCurrentScene()
        {
            if (!restrictToActiveLifecycleScene)
            {
                return true;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            return activeScene.IsValid()
                && activeScene == gameObject.scene
                && activeScene.path.StartsWith("Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/", StringComparison.Ordinal);
        }

        private void DrawWindow(int windowId)
        {
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(panelRect.width - 12f), GUILayout.Height(panelRect.height - 46f));

            GUILayout.Label($"Scene: {gameObject.scene.name}");
            GUILayout.Label($"Last Action: {lastAction}");
            GUILayout.Space(8f);

            var wrappedLabel = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
            GUILayout.Label("Scene Regressions", GUI.skin.label);
            GUILayout.Label(
                "Run from Unity Editor: Immersive Framework/QA/Regressions/" +
                "Player/Run Player Gameplay Admission Regression",
                wrappedLabel);
            GUILayout.Label(
                "Run from Unity Editor: Immersive Framework/QA/Regressions/" +
                "Camera/Run Camera Runtime Host Integration Regression",
                wrappedLabel);

            GUILayout.Space(8f);
            GUILayout.Label("Routes", GUI.skin.label);
            DrawRouteButton("Load Lifecycle Route A", routeATrigger);
            DrawRouteButton("Load Lifecycle Route B", routeBTrigger);
            DrawRouteButton("Load Lifecycle No Activity Route", noActivityRouteTrigger);
            DrawRouteButton("Back to QA Hub", hubRouteTrigger);

            GUILayout.Space(8f);
            GUILayout.Label("Activities", GUI.skin.label);
            DrawActivityButton("Request Lifecycle Activity A", activityATrigger);
            DrawActivityButton("Request Lifecycle Activity B", activityBTrigger);
            DrawActivityButton("Request Route Fallback Activity", noContentActivityTrigger);
            DrawClearActivityButton("Clear Activity", clearActivityTrigger);

            GUILayout.Space(8f);
            GUILayout.Label("Expected Flow", GUI.skin.label);
            GUILayout.Label("- Route content enters when route changes.");
            GUILayout.Label("- Startup Activity content enters with its route.");
            GUILayout.Label("- Activity content switches without Camera QA behavior.");
            GUILayout.Label("- Clear Activity returns to route-only content.");

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawRouteButton(string label, RouteRequestTrigger trigger)
        {
            bool canRequest = trigger != null && trigger.TargetRoute != null && !trigger.IsRequestInFlight;
            GUI.enabled = canRequest;
            string resolvedLabel = trigger != null && trigger.TargetRoute != null ? label : $"{label} [MISSING ROUTE]";
            if (GUILayout.Button(resolvedLabel, GUILayout.Height(30f)))
            {
                lastAction = $"Requested route: {trigger.TargetRoute.RouteName}";
                trigger.RequestRoute();
            }

            GUI.enabled = true;
        }

        private void DrawActivityButton(string label, ActivityRequestTrigger trigger)
        {
            bool canRequest = trigger != null && trigger.TargetActivity != null && !trigger.IsRequestInFlight;
            GUI.enabled = canRequest;
            string resolvedLabel = trigger != null && trigger.TargetActivity != null ? label : $"{label} [MISSING ACTIVITY]";
            if (GUILayout.Button(resolvedLabel, GUILayout.Height(30f)))
            {
                lastAction = $"Requested activity: {trigger.TargetActivity.ActivityName}";
                trigger.RequestActivity();
            }

            GUI.enabled = true;
        }

        private void DrawClearActivityButton(string label, ActivityRequestTrigger trigger)
        {
            GUI.enabled = trigger != null && !trigger.IsRequestInFlight;
            if (GUILayout.Button(label, GUILayout.Height(30f)))
            {
                lastAction = "Requested activity clear.";
                trigger.ClearActivity();
            }

            GUI.enabled = true;
        }

        private static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(320f, rect.width);
            float height = Mathf.Max(260f, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(Mathf.Clamp(rect.x, 0f, maxX), Mathf.Clamp(rect.y, 0f, maxY), width, height);
        }
    }
}

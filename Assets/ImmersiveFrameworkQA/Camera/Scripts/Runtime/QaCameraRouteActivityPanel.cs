using System;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// QA-only IMGUI panel for manually exercising camera Route/Activity precedence.
    /// Keeps all camera-smoke controls and diagnostics in one scrollable panel.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/QA Camera Route Activity Panel")]
    public sealed class QaCameraRouteActivityPanel : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private QaCameraDirector director;

        [Header("Route")]
        [SerializeField] private RouteRequestTrigger otherRouteTrigger;

        [Header("Activities")]
        [SerializeField] private ActivityRequestTrigger primaryActivityTrigger;
        [SerializeField] private ActivityRequestTrigger secondaryActivityTrigger;
        [SerializeField] private ActivityRequestTrigger routeFallbackActivityTrigger;
        [SerializeField] private ActivityRequestTrigger clearActivityTrigger;

        [Header("Related Panels")]
        [SerializeField] private GameObject[] relatedPanelObjects = Array.Empty<GameObject>();
        [SerializeField] private bool closeRelatedPanelsOnStart = true;

        [Header("Panel")]
        [SerializeField] private string title = "QA Camera Route/Activity";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 640f, 700f);
        [SerializeField] private Vector2 minimumPanelSize = new Vector2(420f, 360f);
        [SerializeField] private float scrollContentWidth = 590f;

        private Vector2 scroll;
        private bool showSmokeNavigation = true;
        private bool showExpectedFlow = true;
        private bool showDirectorDiagnostics = true;
        private bool showTriggerDiagnostics = true;
        private bool showRelatedPanels;
        private string lastManualAction = "Camera QA panel ready.";

        public void Configure(
            RouteRequestTrigger nextOtherRouteTrigger,
            ActivityRequestTrigger nextPrimaryActivityTrigger,
            ActivityRequestTrigger nextSecondaryActivityTrigger,
            ActivityRequestTrigger nextRouteFallbackActivityTrigger,
            ActivityRequestTrigger nextClearActivityTrigger,
            string nextTitle)
        {
            Configure(
                nextOtherRouteTrigger,
                nextPrimaryActivityTrigger,
                nextSecondaryActivityTrigger,
                nextRouteFallbackActivityTrigger,
                nextClearActivityTrigger,
                nextTitle,
                null,
                Array.Empty<GameObject>());
        }

        public void Configure(
            RouteRequestTrigger nextOtherRouteTrigger,
            ActivityRequestTrigger nextPrimaryActivityTrigger,
            ActivityRequestTrigger nextSecondaryActivityTrigger,
            ActivityRequestTrigger nextRouteFallbackActivityTrigger,
            ActivityRequestTrigger nextClearActivityTrigger,
            string nextTitle,
            QaCameraDirector nextDirector,
            GameObject[] nextRelatedPanelObjects)
        {
            otherRouteTrigger = nextOtherRouteTrigger;
            primaryActivityTrigger = nextPrimaryActivityTrigger;
            secondaryActivityTrigger = nextSecondaryActivityTrigger;
            routeFallbackActivityTrigger = nextRouteFallbackActivityTrigger;
            clearActivityTrigger = nextClearActivityTrigger;
            title = string.IsNullOrWhiteSpace(nextTitle) ? "QA Camera Route/Activity" : nextTitle;
            director = nextDirector;
            relatedPanelObjects = nextRelatedPanelObjects ?? Array.Empty<GameObject>();
        }


        public void ConfigureLayout(Rect nextPanelRect, Vector2 nextMinimumPanelSize, float nextScrollContentWidth)
        {
            panelRect = nextPanelRect;
            minimumPanelSize = nextMinimumPanelSize;
            scrollContentWidth = Mathf.Max(320f, nextScrollContentWidth);
        }

        public void CloseRelatedPanels()
        {
            if (relatedPanelObjects == null)
            {
                return;
            }

            for (int i = 0; i < relatedPanelObjects.Length; i++)
            {
                GameObject panel = relatedPanelObjects[i];
                if (panel == null || ReferenceEquals(panel, gameObject))
                {
                    continue;
                }

                panel.SetActive(false);
            }
        }

        [ContextMenu("Request Other Route")]
        public void RequestOtherRoute()
        {
            if (otherRouteTrigger == null)
            {
                SetManualAction("Route request blocked. RouteRequestTrigger is missing.");
                Debug.LogWarning("[QA_CAMERA] Route request blocked. RouteRequestTrigger is missing.", this);
                return;
            }

            if (otherRouteTrigger.TargetRoute == null)
            {
                SetManualAction("Route request blocked. Target Route is missing. Run Immersive Framework QA > Camera > Configure Route-Activity Camera QA.");
                Debug.LogWarning("[QA_CAMERA] Route request blocked. Target Route is missing. Run the Camera QA configurator.", this);
                return;
            }

            SetManualAction("Requested other route. Expected: route camera set, then startup Activity camera if the route has one.");
            otherRouteTrigger.RequestRoute();
        }

        [ContextMenu("Request Primary Activity")]
        public void RequestPrimaryActivity()
        {
            RequestActivity(primaryActivityTrigger, "primary", "Expected: own Activity camera wins over Route camera.");
        }

        [ContextMenu("Request Secondary Activity")]
        public void RequestSecondaryActivity()
        {
            RequestActivity(secondaryActivityTrigger, "secondary", "Expected: keeps previous Activity camera when no own rig is assigned.");
        }

        [ContextMenu("Request Route Fallback Activity")]
        public void RequestRouteFallbackActivity()
        {
            RequestActivity(routeFallbackActivityTrigger, "route-fallback", "Expected: falls back to Route camera rig.");
        }

        [ContextMenu("Clear Activity")]
        public void ClearActivity()
        {
            if (clearActivityTrigger == null)
            {
                SetManualAction("Activity clear failed. ActivityRequestTrigger is missing.");
                Debug.LogWarning("[QA_CAMERA] Activity clear failed. ActivityRequestTrigger is missing.", this);
                return;
            }

            SetManualAction("Requested clear activity. Expected: Route/default camera becomes effective.");
            clearActivityTrigger.ClearActivity();
        }

        private void Start()
        {
            if (closeRelatedPanelsOnStart)
            {
                CloseRelatedPanels();
            }
        }

        private void RequestActivity(ActivityRequestTrigger trigger, string label, string expectation)
        {
            if (trigger == null)
            {
                SetManualAction($"Activity request blocked. label='{label}' ActivityRequestTrigger is missing.");
                Debug.LogWarning($"[QA_CAMERA] Activity request blocked. label='{label}' ActivityRequestTrigger is missing.", this);
                return;
            }

            if (trigger.TargetActivity == null)
            {
                SetManualAction($"Activity request blocked. label='{label}' Target Activity is missing. Run Immersive Framework QA > Camera > Configure Route-Activity Camera QA.");
                Debug.LogWarning($"[QA_CAMERA] Activity request blocked. label='{label}' Target Activity is missing. Run the Camera QA configurator.", this);
                return;
            }

            SetManualAction($"Requested activity '{label}'. {expectation}");
            trigger.RequestActivity();
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            panelRect.width = Mathf.Max(panelRect.width, minimumPanelSize.x);
            panelRect.height = Mathf.Max(panelRect.height, minimumPanelSize.y);

            GUILayout.BeginArea(panelRect, GUI.skin.window);
            DrawHeader();

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(panelRect.width - 12f), GUILayout.Height(panelRect.height - 64f));
            GUILayout.BeginVertical(GUILayout.Width(scrollContentWidth));

            DrawFoldout("Smoke Navigation", ref showSmokeNavigation, DrawSmokeNavigation);
            DrawFoldout("Expected Flow", ref showExpectedFlow, DrawExpectedFlow);
            DrawFoldout("Director Diagnostics", ref showDirectorDiagnostics, DrawDirectorDiagnostics);
            DrawFoldout("Trigger Diagnostics", ref showTriggerDiagnostics, DrawTriggerDiagnostics);
            DrawFoldout("Other QA Panels", ref showRelatedPanels, DrawRelatedPanels);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.enabled = true;
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, GUI.skin.label, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Close Others", GUILayout.Width(96f)))
            {
                CloseRelatedPanels();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Last Action: {lastManualAction}");
            GUILayout.Label($"Effective Camera: {FormatEffectiveCamera()} | Source: {FormatEffectiveSource()}");
        }

        private void DrawSmokeNavigation()
        {
            GUILayout.Label("Run this sequence from top to bottom. Watch [QA_CAMERA] logs after each request.");
            GUILayout.Space(4f);

            DrawRouteButton(otherRouteTrigger, "1. Request Other Route", RequestOtherRoute);
            GUILayout.Label("   Validates Route camera + Startup Activity camera pre-apply on the target route.");
            GUILayout.Space(6f);

            DrawActivityButton(primaryActivityTrigger, "2. Primary Activity: Own Camera", RequestPrimaryActivity);
            GUILayout.Label("   Expected: Activity rig overrides Route rig.");

            DrawActivityButton(secondaryActivityTrigger, "3. Secondary Activity: Keep Previous", RequestSecondaryActivity);
            GUILayout.Label("   Expected: no new own rig; retained Activity rig remains effective.");

            DrawActivityButton(routeFallbackActivityTrigger, "4. Route Fallback Activity", RequestRouteFallbackActivity);
            GUILayout.Label("   Expected: Route rig becomes effective.");

            DrawActivityButton(primaryActivityTrigger, "5. Restore Primary Activity", RequestPrimaryActivity);
            GUILayout.Label("   Expected: Activity rig becomes effective again.");

            DrawClearButton(clearActivityTrigger, "6. Clear Activity", ClearActivity);
            GUILayout.Label("   Expected: Route/default rig becomes effective.");
        }

        private void DrawExpectedFlow()
        {
            GUILayout.Label("Priority under test:");
            GUILayout.Label("Activity Camera > Retained Activity Camera > Route Camera > Default Camera");
            GUILayout.Space(6f);
            GUILayout.Label("Expected logs:");
            GUILayout.Label("- Route camera set");
            GUILayout.Label("- Startup Activity Camera pre-applied, when route has Startup Activity");
            GUILayout.Label("- Activity camera set");
            GUILayout.Label("- Camera applied when effective rig changes");
            GUILayout.Label("- Camera refresh skipped when effective rig is unchanged");
        }

        private void DrawDirectorDiagnostics()
        {
            if (director == null)
            {
                GUILayout.Label("Director: Missing");
                return;
            }

            GUILayout.Label($"Effective Rig: {director.CurrentEffectiveRigName}");
            GUILayout.Label($"Effective Source: {director.CurrentEffectiveSource}");
            GUILayout.Label($"Effective Priority: {director.CurrentEffectivePriority}");
            GUILayout.Label($"Route Rig: {director.CurrentRouteRigName}");
            GUILayout.Label($"Activity Rig: {director.CurrentActivityRigName}");
            GUILayout.Label($"Retained Activity Rig: {director.RetainedActivityRigName}");
            GUILayout.Label($"Activity Binding Active: {director.HasActiveActivityCameraBinding}");
            GUILayout.Label($"Activity Policy: {director.CurrentActivityPolicy}");
        }

        private void DrawTriggerDiagnostics()
        {
            DrawRouteStatus("Other Route", otherRouteTrigger);
            DrawActivityStatus("Primary", primaryActivityTrigger);
            DrawActivityStatus("Secondary", secondaryActivityTrigger);
            DrawActivityStatus("Route Fallback", routeFallbackActivityTrigger);
            DrawActivityStatus("Clear", clearActivityTrigger);
        }

        private void DrawRelatedPanels()
        {
            if (relatedPanelObjects == null || relatedPanelObjects.Length == 0)
            {
                GUILayout.Label("No related QA panels were linked by the configurator.");
                GUILayout.Label("This is valid for isolated Camera QA scenes.");
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close All Related Panels", GUILayout.Height(28f)))
            {
                CloseRelatedPanels();
            }
            if (GUILayout.Button("Open All Related Panels", GUILayout.Height(28f)))
            {
                SetRelatedPanelsActive(true);
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < relatedPanelObjects.Length; i++)
            {
                GameObject relatedPanel = relatedPanelObjects[i];
                if (relatedPanel == null || ReferenceEquals(relatedPanel, gameObject))
                {
                    continue;
                }

                GUILayout.BeginHorizontal();
                GUILayout.Label($"{relatedPanel.name}: {(relatedPanel.activeSelf ? "Open" : "Closed")}", GUILayout.ExpandWidth(true));
                if (GUILayout.Button(relatedPanel.activeSelf ? "Close" : "Open", GUILayout.Width(80f)))
                {
                    relatedPanel.SetActive(!relatedPanel.activeSelf);
                }
                GUILayout.EndHorizontal();
            }
        }

        private static void DrawFoldout(string label, ref bool expanded, Action draw)
        {
            GUILayout.Space(8f);
            if (GUILayout.Button($"{(expanded ? "▼" : "▶")} {label}", GUILayout.Height(26f)))
            {
                expanded = !expanded;
            }

            if (!expanded)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            draw?.Invoke();
            GUILayout.EndVertical();
        }

        private static void DrawRouteButton(RouteRequestTrigger trigger, string label, Action action)
        {
            bool hasTarget = trigger != null && trigger.TargetRoute != null;
            GUI.enabled = trigger != null && hasTarget && !trigger.IsRequestInFlight;
            string resolvedLabel = hasTarget ? label : $"{label}  [MISSING TARGET ROUTE]";
            if (GUILayout.Button(resolvedLabel, GUILayout.Height(30f)))
            {
                action?.Invoke();
            }

            GUI.enabled = true;
        }

        private static void DrawActivityButton(ActivityRequestTrigger trigger, string label, Action action)
        {
            bool hasTarget = trigger != null && trigger.TargetActivity != null;
            GUI.enabled = trigger != null && hasTarget && !trigger.IsRequestInFlight;
            string resolvedLabel = hasTarget ? label : $"{label}  [MISSING TARGET ACTIVITY]";
            if (GUILayout.Button(resolvedLabel, GUILayout.Height(30f)))
            {
                action?.Invoke();
            }

            GUI.enabled = true;
        }

        private static void DrawClearButton(ActivityRequestTrigger trigger, string label, Action action)
        {
            GUI.enabled = trigger != null && !trigger.IsRequestInFlight;
            if (GUILayout.Button(label, GUILayout.Height(30f)))
            {
                action?.Invoke();
            }

            GUI.enabled = true;
        }

        private static void DrawRouteStatus(string label, RouteRequestTrigger trigger)
        {
            if (trigger == null)
            {
                GUILayout.Label($"{label}: Missing route trigger");
                return;
            }

            GUILayout.Label($"{label}: target='{FormatRouteTarget(trigger)}' outcome='{trigger.LastOutcome}' inFlight='{trigger.IsRequestInFlight}'");
            if (!string.IsNullOrWhiteSpace(trigger.LastMessage))
            {
                GUILayout.Label($"  message='{trigger.LastMessage}'");
            }
        }

        private static void DrawActivityStatus(string label, ActivityRequestTrigger trigger)
        {
            if (trigger == null)
            {
                GUILayout.Label($"{label}: Missing activity trigger");
                return;
            }

            string suffix = trigger.LastRequestClearedActivity ? " clear" : string.Empty;
            GUILayout.Label($"{label}: target='{FormatActivityTarget(trigger)}' outcome='{trigger.LastOutcome}{suffix}' inFlight='{trigger.IsRequestInFlight}'");
        }

        private static string FormatRouteTarget(RouteRequestTrigger trigger)
        {
            return trigger != null && trigger.TargetRoute != null ? trigger.TargetRoute.RouteName : "<missing>";
        }

        private static string FormatActivityTarget(ActivityRequestTrigger trigger)
        {
            return trigger != null && trigger.TargetActivity != null ? trigger.TargetActivity.ActivityName : "<missing>";
        }

        private void SetRelatedPanelsActive(bool isActive)
        {
            if (relatedPanelObjects == null)
            {
                return;
            }

            for (int i = 0; i < relatedPanelObjects.Length; i++)
            {
                GameObject relatedPanel = relatedPanelObjects[i];
                if (relatedPanel == null || ReferenceEquals(relatedPanel, gameObject))
                {
                    continue;
                }

                relatedPanel.SetActive(isActive);
            }
        }

        private string FormatEffectiveCamera()
        {
            return director != null ? director.CurrentEffectiveRigName : "<director-missing>";
        }

        private string FormatEffectiveSource()
        {
            return director != null ? director.CurrentEffectiveSource : "<none>";
        }

        private void SetManualAction(string message)
        {
            lastManualAction = string.IsNullOrWhiteSpace(message) ? "<none>" : message;
        }
    }
}

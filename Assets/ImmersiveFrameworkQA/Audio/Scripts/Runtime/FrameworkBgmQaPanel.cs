using System;
using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Framework.Audio;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Audio
{
    /// <summary>
    /// QA-only IMGUI panel for manually exercising official FrameworkBgm Route/Activity precedence.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Audio/Framework BGM QA Panel")]
    public sealed class FrameworkBgmQaPanel : MonoBehaviour
    {
        [Header("Runtime")]
        [SerializeField] private FrameworkBgmDirector director;

        [Header("Route")]
        [SerializeField] private RouteRequestTrigger otherRouteTrigger;

        [Header("Activities")]
        [SerializeField] private ActivityRequestTrigger startupActivityTrigger;
        [SerializeField] private ActivityRequestTrigger ownActivityTrigger;
        [SerializeField] private ActivityRequestTrigger retainPreviousActivityTrigger;
        [SerializeField] private ActivityRequestTrigger routeFallbackActivityTrigger;
        [SerializeField] private ActivityRequestTrigger silenceActivityTrigger;
        [SerializeField] private ActivityRequestTrigger clearActivityTrigger;

        [Header("Expected Cues")]
        [SerializeField] private AudioBgmCueAsset expectedRouteBgm;
        [SerializeField] private AudioBgmCueAsset expectedStartupActivityBgm;
        [SerializeField] private AudioBgmCueAsset expectedOwnActivityBgm;

        [Header("Panel")]
        [SerializeField] private string title = "QA Framework BGM";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private bool restrictToAudioScene = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 680f, 720f);
        [SerializeField] private Vector2 minimumPanelSize = new Vector2(460f, 420f);
        [SerializeField] private float scrollContentWidth = 620f;

        private Vector2 scroll;
        private bool showSmokeNavigation = true;
        private bool showExpectedFlow = true;
        private bool showDirectorDiagnostics = true;
        private bool showTriggerDiagnostics = true;
        private string lastManualAction = "Framework BGM QA panel ready.";

        public void Configure(
            RouteRequestTrigger nextOtherRouteTrigger,
            ActivityRequestTrigger nextStartupActivityTrigger,
            ActivityRequestTrigger nextOwnActivityTrigger,
            ActivityRequestTrigger nextRetainPreviousActivityTrigger,
            ActivityRequestTrigger nextRouteFallbackActivityTrigger,
            ActivityRequestTrigger nextSilenceActivityTrigger,
            ActivityRequestTrigger nextClearActivityTrigger,
            string nextTitle,
            FrameworkBgmDirector nextDirector,
            AudioBgmCueAsset nextExpectedRouteBgm,
            AudioBgmCueAsset nextExpectedStartupActivityBgm,
            AudioBgmCueAsset nextExpectedOwnActivityBgm)
        {
            otherRouteTrigger = nextOtherRouteTrigger;
            startupActivityTrigger = nextStartupActivityTrigger;
            ownActivityTrigger = nextOwnActivityTrigger;
            retainPreviousActivityTrigger = nextRetainPreviousActivityTrigger;
            routeFallbackActivityTrigger = nextRouteFallbackActivityTrigger;
            silenceActivityTrigger = nextSilenceActivityTrigger;
            clearActivityTrigger = nextClearActivityTrigger;
            title = string.IsNullOrWhiteSpace(nextTitle) ? "QA Framework BGM" : nextTitle;
            director = nextDirector;
            expectedRouteBgm = nextExpectedRouteBgm;
            expectedStartupActivityBgm = nextExpectedStartupActivityBgm;
            expectedOwnActivityBgm = nextExpectedOwnActivityBgm;
        }

        public void ConfigureLayout(Rect nextPanelRect, Vector2 nextMinimumPanelSize, float nextScrollContentWidth)
        {
            panelRect = nextPanelRect;
            minimumPanelSize = nextMinimumPanelSize;
            scrollContentWidth = Mathf.Max(360f, nextScrollContentWidth);
        }

        public void RequestOtherRoute()
        {
            if (otherRouteTrigger == null)
            {
                SetManualAction("Route request blocked. RouteRequestTrigger is missing.");
                Debug.LogWarning("[FRAMEWORK_BGM_QA] Route request blocked. RouteRequestTrigger is missing.", this);
                return;
            }

            if (otherRouteTrigger.TargetRoute == null)
            {
                SetManualAction("Route request blocked. Target Route is missing. Run the Framework BGM QA configurator.");
                Debug.LogWarning("[FRAMEWORK_BGM_QA] Route request blocked. Target Route is missing.", this);
                return;
            }

            SetManualAction("Requested other route. Expected: Route BGM applies and retained Activity BGM is cleared.");
            otherRouteTrigger.RequestRoute();
        }

        public void RequestStartupActivity()
        {
            RequestActivity(startupActivityTrigger, "startup", "Expected: Startup Activity BGM applies.");
        }

        public void RequestOwnActivity()
        {
            RequestActivity(ownActivityTrigger, "own", "Expected: own Activity BGM applies and can be retained.");
        }

        public void RequestRetainPreviousActivity()
        {
            RequestActivity(retainPreviousActivityTrigger, "retain-previous", "Expected: no own BGM, retained Activity BGM remains effective.");
        }

        public void RequestRouteFallbackActivity()
        {
            RequestActivity(routeFallbackActivityTrigger, "route-fallback", "Expected: Route BGM becomes effective.");
        }

        public void RequestSilenceActivity()
        {
            RequestActivity(silenceActivityTrigger, "silence", "Expected: BGM stops and retained Activity BGM is cleared.");
        }

        public void ClearActivity()
        {
            if (clearActivityTrigger == null)
            {
                SetManualAction("Activity clear failed. ActivityRequestTrigger is missing.");
                Debug.LogWarning("[FRAMEWORK_BGM_QA] Activity clear failed. ActivityRequestTrigger is missing.", this);
                return;
            }

            SetManualAction("Requested clear activity. Expected: Route BGM or retained Activity policy result becomes effective.");
            clearActivityTrigger.ClearActivity();
        }

        private void RequestActivity(ActivityRequestTrigger trigger, string label, string expectation)
        {
            if (trigger == null)
            {
                SetManualAction($"Activity request blocked. label='{label}' ActivityRequestTrigger is missing.");
                Debug.LogWarning($"[FRAMEWORK_BGM_QA] Activity request blocked. label='{label}' ActivityRequestTrigger is missing.", this);
                return;
            }

            if (trigger.TargetActivity == null)
            {
                SetManualAction($"Activity request blocked. label='{label}' Target Activity is missing. Run the Framework BGM QA configurator.");
                Debug.LogWarning($"[FRAMEWORK_BGM_QA] Activity request blocked. label='{label}' Target Activity is missing.", this);
                return;
            }

            SetManualAction($"Requested activity '{label}'. {expectation}");
            trigger.RequestActivity();
        }

        private void OnGUI()
        {
            if (!showPanel || !ShouldDrawForCurrentScene())
            {
                return;
            }

            panelRect = ClampToScreen(panelRect);
            panelRect = ClampToScreen(GUI.Window(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), panelRect, DrawWindow, title));
            GUI.enabled = true;
        }

        private bool ShouldDrawForCurrentScene()
        {
            return !restrictToAudioScene
                || gameObject.scene.path.StartsWith("Assets/ImmersiveFrameworkQA/Audio/Scenes/", StringComparison.Ordinal);
        }

        private void DrawWindow(int windowId)
        {
            DrawHeader();

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(panelRect.width - 12f), GUILayout.Height(panelRect.height - 76f));
            GUILayout.BeginVertical(GUILayout.Width(scrollContentWidth));

            DrawFoldout("Smoke Navigation", ref showSmokeNavigation, DrawSmokeNavigation);
            DrawFoldout("Expected Flow", ref showExpectedFlow, DrawExpectedFlow);
            DrawFoldout("Director Diagnostics", ref showDirectorDiagnostics, DrawDirectorDiagnostics);
            DrawFoldout("Trigger Diagnostics", ref showTriggerDiagnostics, DrawTriggerDiagnostics);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(title, GUI.skin.label, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            GUILayout.Label($"Last Action: {lastManualAction}");
            GUILayout.Label($"Effective BGM: {FormatCue(director != null ? director.CurrentEffectiveBgm : null)} | Silence: {FormatSilence()} | Last Playback: {FormatPlayback()}");
        }

        private void DrawSmokeNavigation()
        {
            GUILayout.Label("Run this sequence from top to bottom. Watch [FRAMEWORK_BGM] logs after each request.");
            GUILayout.Space(4f);

            DrawActivityButton(startupActivityTrigger, "1. Startup Activity BGM", RequestStartupActivity);
            GUILayout.Label("   Expected cue: " + FormatCue(expectedStartupActivityBgm));

            DrawClearButton(clearActivityTrigger, "2. Clear Activity: Route BGM", ClearActivity);
            GUILayout.Label("   Expected cue: " + FormatCue(expectedRouteBgm));

            DrawActivityButton(ownActivityTrigger, "3. Activity Own BGM", RequestOwnActivity);
            GUILayout.Label("   Expected cue: " + FormatCue(expectedOwnActivityBgm));

            DrawActivityButton(retainPreviousActivityTrigger, "4. Activity Without BGM: Retain Previous", RequestRetainPreviousActivity);
            GUILayout.Label("   Expected cue: " + FormatCue(expectedOwnActivityBgm));

            DrawActivityButton(routeFallbackActivityTrigger, "5. Activity UseRoute: Route Fallback", RequestRouteFallbackActivity);
            GUILayout.Label("   Expected cue: " + FormatCue(expectedRouteBgm));

            DrawActivityButton(silenceActivityTrigger, "6. Activity Silence", RequestSilenceActivity);
            GUILayout.Label("   Expected: StopBgm and explicit silence.");

            DrawClearButton(clearActivityTrigger, "7. Clear Activity After Silence", ClearActivity);
            GUILayout.Label("   Expected cue: " + FormatCue(expectedRouteBgm));

            DrawActivityButton(ownActivityTrigger, "8. Restore Retained Activity BGM", RequestOwnActivity);
            DrawRouteButton(otherRouteTrigger, "9. Route Switch Clears Retained Activity BGM", RequestOtherRoute);
            GUILayout.Label("   Expected: retained Activity BGM becomes <silence> or route-local only after route switch.");
        }

        private void DrawExpectedFlow()
        {
            GUILayout.Label("Priority under test:");
            GUILayout.Label("Activity BGM > Retained Activity BGM > Route BGM > Silence / Stop");
            GUILayout.Space(6f);
            GUILayout.Label("Expected logs:");
            GUILayout.Label("- [FRAMEWORK_BGM] Route BGM set");
            GUILayout.Label("- [FRAMEWORK_BGM] Startup Activity BGM pre-applied");
            GUILayout.Label("- [FRAMEWORK_BGM] Activity BGM set");
            GUILayout.Label("- [FRAMEWORK_BGM] BGM applied");
            GUILayout.Label("- [FRAMEWORK_BGM] Activity BGM policy Silence applied");
            GUILayout.Label("- AudioPlaybackResult status is explicit");
        }

        private void DrawDirectorDiagnostics()
        {
            if (director == null)
            {
                GUILayout.Label("Director: Missing");
                return;
            }

            GUILayout.Label($"Route BGM: {FormatCue(director.CurrentRouteBgm)}");
            GUILayout.Label($"Activity BGM: {FormatCue(director.CurrentActivityBgm)}");
            GUILayout.Label($"Retained Activity BGM: {FormatCue(director.RetainedActivityBgmForCurrentRoute)}");
            GUILayout.Label($"Effective BGM: {FormatCue(director.CurrentEffectiveBgm)}");
            GUILayout.Label($"Activity Binding Active: {director.HasActiveActivityBgmBinding}");
            GUILayout.Label($"Activity Policy: {director.CurrentActivityPolicy}");
            GUILayout.Label($"Explicit Silence: {director.CurrentEffectiveIsExplicitSilence}");
            GUILayout.Label($"Last Playback Result: {director.LastPlaybackResult.Status}");
        }

        private void DrawTriggerDiagnostics()
        {
            DrawRouteStatus("Other Route", otherRouteTrigger);
            DrawActivityStatus("Startup", startupActivityTrigger);
            DrawActivityStatus("Own", ownActivityTrigger);
            DrawActivityStatus("Retain Previous", retainPreviousActivityTrigger);
            DrawActivityStatus("Route Fallback", routeFallbackActivityTrigger);
            DrawActivityStatus("Silence", silenceActivityTrigger);
            DrawActivityStatus("Clear", clearActivityTrigger);
        }

        private static void DrawFoldout(string label, ref bool expanded, Action draw)
        {
            GUILayout.Space(8f);
            if (GUILayout.Button($"{(expanded ? "v" : ">")} {label}", GUILayout.Height(26f)))
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
            string resolvedLabel = hasTarget ? label : $"{label} [MISSING TARGET ROUTE]";
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
            string resolvedLabel = hasTarget ? label : $"{label} [MISSING TARGET ACTIVITY]";
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

        private Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(rect.width, minimumPanelSize.x);
            float height = Mathf.Max(rect.height, minimumPanelSize.y);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(Mathf.Clamp(rect.x, 0f, maxX), Mathf.Clamp(rect.y, 0f, maxY), width, height);
        }

        private void SetManualAction(string message)
        {
            lastManualAction = string.IsNullOrWhiteSpace(message) ? "<none>" : message;
        }

        private string FormatSilence()
        {
            return director != null ? director.CurrentEffectiveIsExplicitSilence.ToString() : "<director-missing>";
        }

        private string FormatPlayback()
        {
            return director != null ? director.LastPlaybackResult.Status.ToString() : "<director-missing>";
        }

        private static string FormatCue(AudioBgmCueAsset cue)
        {
            return cue != null ? cue.name : "<silence>";
        }

        private static string FormatRouteTarget(RouteRequestTrigger trigger)
        {
            return trigger != null && trigger.TargetRoute != null ? trigger.TargetRoute.RouteName : "<missing>";
        }

        private static string FormatActivityTarget(ActivityRequestTrigger trigger)
        {
            return trigger != null && trigger.TargetActivity != null ? trigger.TargetActivity.ActivityName : "<missing>";
        }
    }
}

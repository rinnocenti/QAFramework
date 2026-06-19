using System;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// Minimal development QA surface for exercising framework runtime requests.
    /// Uses IMGUI to avoid adding a UGUI/package dependency to the framework runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/QA/Framework QA Canvas")]
    public sealed class FrameworkQaCanvas : MonoBehaviour
    {
        private const string QaSource = nameof(FrameworkQaCanvas);

        private static FrameworkQaCanvas current;
        private static int nextWindowId = 36000;

        private FrameworkLogger logger;
        private int windowId;
        private bool requestInFlight;
        private Vector2 scroll;

        [Header("Display")]
        [SerializeField] private bool showCanvas = true;
        [SerializeField] private bool persistAcrossSceneLoads = true;
        [SerializeField] private bool allowInPlayerBuild;
        [SerializeField] private Rect windowRect = new Rect(16f, 16f, 360f, 520f);

        [Header("Routes")]
        [SerializeField] private RouteAsset[] routeTargets = Array.Empty<RouteAsset>();
        [SerializeField] private string routeReason = "qa.route";

        [Header("Activities")]
        [SerializeField] private ActivityAsset[] activityTargets = Array.Empty<ActivityAsset>();
        [SerializeField] private string activityReason = "qa.activity";
        [SerializeField] private string clearActivityReason = "qa.activity.clear";

        private void Awake()
        {
            logger = FrameworkLogger.Create();
            EnsureWindowId();

            if (!persistAcrossSceneLoads)
            {
                return;
            }

            if (current != null && current != this)
            {
                Destroy(gameObject);
                return;
            }

            current = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnGUI()
        {
            if (!CanRenderInCurrentBuild())
            {
                return;
            }

            if (!showCanvas)
            {
                if (GUI.Button(new Rect(16f, 16f, 160f, 28f), "Immersive QA"))
                {
                    showCanvas = true;
                }

                return;
            }

            EnsureWindowId();
            windowRect = GUILayout.Window(windowId, windowRect, DrawWindow, "Immersive Framework QA");
        }


        private void EnsureWindowId()
        {
            if (windowId != 0)
            {
                return;
            }

            windowId = nextWindowId++;
        }

        private bool CanRenderInCurrentBuild()
        {
#if UNITY_EDITOR
            return true;
#else
            return allowInPlayerBuild || Debug.isDebugBuild;
#endif
        }

        private void DrawWindow(int windowId)
        {
            using (new GUILayout.VerticalScope())
            {
                DrawToolbar();

                scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
                DrawRuntimeStatus();
                DrawRouteRequests();
                DrawActivityRequests();
                GUILayout.EndScrollView();
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("QA runtime requests", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Hide", GUILayout.Width(64f)))
                {
                    showCanvas = false;
                }
            }
        }

        private void DrawRuntimeStatus()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Runtime", GUI.skin.box);

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                GUILayout.Label("Application Runtime: unavailable");
                return;
            }

            var state = runtimeHost.State;
            GUILayout.Label("Application Runtime: available");
            GUILayout.Label($"Game Application: {GetName(state.GameApplication != null ? state.GameApplication.ApplicationName : string.Empty, "<none>")}");
            GUILayout.Label($"Active Route: {GetName(state.CurrentRouteName, "<none>")}");
            GUILayout.Label($"Active Activity: {GetName(state.CurrentActivityName, "<none>")}");
        }

        private void DrawRouteRequests()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Route Requests", GUI.skin.box);
            GUILayout.Label($"Reason: {ResolveReason(routeReason, "qa.route")}");

            if (routeTargets == null || routeTargets.Length == 0)
            {
                GUILayout.Label("No Route targets configured.");
                return;
            }

            foreach (var route in routeTargets)
            {
                if (route == null)
                {
                    GUILayout.Label("Missing Route target.");
                    continue;
                }

                using (new EditorDisabledScope(requestInFlight))
                {
                    if (GUILayout.Button($"Request Route: {route.RouteName}"))
                    {
                        RequestRoute(route);
                    }
                }
            }
        }

        private void DrawActivityRequests()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Activity Requests", GUI.skin.box);
            GUILayout.Label($"Reason: {ResolveReason(activityReason, "qa.activity")}");

            if (activityTargets == null || activityTargets.Length == 0)
            {
                GUILayout.Label("No Activity targets configured.");
            }
            else
            {
                foreach (var activity in activityTargets)
                {
                    if (activity == null)
                    {
                        GUILayout.Label("Missing Activity target.");
                        continue;
                    }

                    using (new EditorDisabledScope(requestInFlight))
                    {
                        if (GUILayout.Button($"Request Activity: {activity.ActivityName}"))
                        {
                            RequestActivity(activity);
                        }
                    }
                }
            }

            GUILayout.Space(4f);
            GUILayout.Label($"Clear Reason: {ResolveReason(clearActivityReason, "qa.activity.clear")}");
            using (new EditorDisabledScope(requestInFlight))
            {
                if (GUILayout.Button("Clear Active Activity"))
                {
                    ClearActivity();
                }
            }
        }

        private async void RequestRoute(RouteAsset route)
        {
            EnsureLogger();

            if (requestInFlight)
            {
                logger.Warning("QA Route Request ignored. Framework QA Canvas already has a request in flight.");
                return;
            }

            if (route == null)
            {
                logger.Error("QA Route Request failed. Target Route is missing.");
                return;
            }

            if (!TryGetRuntimeHost("QA Route Request failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                return;
            }

            requestInFlight = true;
            try
            {
                await runtimeHost.RequestRouteAsync(route, QaSource, ResolveReason(routeReason, route.RouteName));
            }
            finally
            {
                requestInFlight = false;
            }
        }

        private async void RequestActivity(ActivityAsset activity)
        {
            EnsureLogger();

            if (requestInFlight)
            {
                logger.Warning("QA Activity Request ignored. Framework QA Canvas already has a request in flight.");
                return;
            }

            if (activity == null)
            {
                logger.Error("QA Activity Request failed. Target Activity is missing.");
                return;
            }

            if (!TryGetRuntimeHost("QA Activity Request failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                return;
            }

            requestInFlight = true;
            try
            {
                await runtimeHost.RequestActivityAsync(activity, QaSource, ResolveReason(activityReason, activity.ActivityName));
            }
            finally
            {
                requestInFlight = false;
            }
        }

        private async void ClearActivity()
        {
            EnsureLogger();

            if (requestInFlight)
            {
                logger.Warning("QA Activity Clear ignored. Framework QA Canvas already has a request in flight.");
                return;
            }

            if (!TryGetRuntimeHost("QA Activity Clear failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                return;
            }

            requestInFlight = true;
            try
            {
                await runtimeHost.ClearActivityAsync(QaSource, ResolveReason(clearActivityReason, "qa.activity.clear"));
            }
            finally
            {
                requestInFlight = false;
            }
        }

        private bool TryGetRuntimeHost(string message, out FrameworkRuntimeHost runtimeHost)
        {
            if (FrameworkRuntimeHost.TryGetCurrent(out runtimeHost))
            {
                return true;
            }

            logger.Error(message);
            return false;
        }

        private void EnsureLogger()
        {
            if (logger == null)
            {
                logger = FrameworkLogger.Create();
            }
        }

        private static string ResolveReason(string configuredReason, string fallback)
        {
            return string.IsNullOrWhiteSpace(configuredReason) ? fallback : configuredReason.Trim();
        }

        private static string GetName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }
        }

        private readonly struct EditorDisabledScope : IDisposable
        {
            private readonly bool previousEnabled;

            public EditorDisabledScope(bool disabled)
            {
                previousEnabled = GUI.enabled;
                GUI.enabled = !disabled && previousEnabled;
            }

            public void Dispose()
            {
                GUI.enabled = previousEnabled;
            }
        }
    }
}

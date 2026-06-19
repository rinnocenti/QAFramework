using System;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

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

        private static FrameworkQaCanvas _current;
        private static int _nextWindowId = 36000;

        private FrameworkLogger _logger;
        private int _windowId;
        private bool _requestInFlight;
        private Vector2 _scroll;

        [Header("Display")]
        [SerializeField] private bool showCanvas = true;
        [SerializeField] private bool persistAcrossSceneLoads = true;
        [SerializeField] private bool allowInPlayerBuild;
        [SerializeField] private Rect windowRect = new Rect(16f, 16f, 400f, 640f);

        [Header("Routes")]
        [SerializeField] private RouteAsset[] routeTargets = Array.Empty<RouteAsset>();
        [SerializeField] private string routeReason = "qa.route";

        [Header("Smoke Scenario Routes")]
        [FormerlySerializedAs("startupRoute")]
        [SerializeField] private RouteAsset canonicalRoute;
        [FormerlySerializedAs("secondaryRoute")]
        [SerializeField] private RouteAsset alternateRoute;
        [SerializeField] private RouteAsset noActivityRoute;

        [Header("Smoke Scenario Activities")]
        [FormerlySerializedAs("activity01")]
        [SerializeField] private ActivityAsset primaryActivity;
        [FormerlySerializedAs("activity02")]
        [SerializeField] private ActivityAsset secondaryActivity;
        [SerializeField] private ActivityAsset noContentActivity;

        [Header("Baseline Reset")]
        [SerializeField] private RouteAsset resetRoute;
        [SerializeField] private ActivityAsset resetActivity;
        [SerializeField] private string resetReason = "qa.reset";

        [Header("Activities")]
        [SerializeField] private ActivityAsset[] activityTargets = Array.Empty<ActivityAsset>();
        [SerializeField] private string activityReason = "qa.activity";
        [SerializeField] private string clearActivityReason = "qa.activity.clear";

        private void Awake()
        {
            _logger = FrameworkLogger.Create();
            EnsureWindowId();

            if (!persistAcrossSceneLoads)
            {
                return;
            }

            if (_current != null && _current != this)
            {
                Destroy(gameObject);
                return;
            }

            _current = this;
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
            windowRect = GUILayout.Window(_windowId, windowRect, DrawWindow, "Immersive Framework QA");
        }

        private void EnsureWindowId()
        {
            if (_windowId != 0)
            {
                return;
            }

            _windowId = _nextWindowId++;
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

                _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
                DrawRuntimeStatus();
                DrawBaselineReset();
                DrawSmokePresets();
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

        private void DrawBaselineReset()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Baseline Reset", GUI.skin.box);

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                GUILayout.Label("Reset unavailable: Application Runtime is unavailable.");
                return;
            }

            var effectiveRoute = ResolveResetRoute(runtimeHost);
            var effectiveActivity = ResolveResetActivity(effectiveRoute);
            GUILayout.Label($"Reset Route: {GetAssetName(effectiveRoute, "<none>")}");
            GUILayout.Label($"Reset Activity: {GetAssetName(effectiveActivity, "<none>")}");
            GUILayout.Label($"Reset Reason: {ResolveReason(resetReason, "qa.reset")}");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Reset QA Scenario"))
                {
                    ResetQaScenario();
                }
            }
        }

        private void DrawSmokePresets()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Smoke Scenario Targets", GUI.skin.box);
            GUILayout.Label($"Canonical Route: {GetAssetName(canonicalRoute, "<none>")}");
            GUILayout.Label($"Alternate Route: {GetAssetName(alternateRoute, "<none>")}");
            GUILayout.Label($"No-Activity Route: {GetAssetName(noActivityRoute, "<none>")}");
            GUILayout.Label($"Primary Activity: {GetAssetName(primaryActivity, "<none>")}");
            GUILayout.Label($"Secondary Activity: {GetAssetName(secondaryActivity, "<none>")}");
            GUILayout.Label($"No-Content Activity: {GetAssetName(noContentActivity, "<none>")}");
            GUILayout.Label(GetQaAssetsStatus());

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Activity Smoke"))
                {
                    RunActivitySmoke();
                }

                if (GUILayout.Button("Run Route Smoke"))
                {
                    RunRouteSmoke();
                }

                if (GUILayout.Button("Run Clear Activity Smoke"))
                {
                    RunClearActivitySmoke();
                }

                if (GUILayout.Button("Run No-Activity Route Smoke"))
                {
                    RunNoActivityRouteSmoke();
                }

                if (GUILayout.Button("Run No-Content Activity Smoke"))
                {
                    RunNoContentActivitySmoke();
                }

                if (GUILayout.Button("Run Negative Smoke"))
                {
                    RunNegativeSmoke();
                }
            }
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

                using (new EditorDisabledScope(_requestInFlight))
                {
                    if (GUILayout.Button($"Request Route: {GetAssetName(route, "<unnamed>")} "))
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

                    using (new EditorDisabledScope(_requestInFlight))
                    {
                        if (GUILayout.Button($"Request Activity: {GetAssetName(activity, "<unnamed>")} "))
                        {
                            RequestActivity(activity);
                        }
                    }
                }
            }

            GUILayout.Space(4f);
            GUILayout.Label($"Clear Reason: {ResolveReason(clearActivityReason, "qa.activity.clear")}");
            using (new EditorDisabledScope(_requestInFlight))
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

            if (!TryBeginOperation("QA Route Request ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (route == null)
            {
                EndOperation();
                _logger.Error("QA Route Request failed. Target Route is missing.");
                return;
            }

            if (!TryGetRuntimeHost("QA Route Request failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            try
            {
                await RequestRouteCoreAsync(runtimeHost, route, ResolveReason(routeReason, route.RouteName));
            }
            finally
            {
                EndOperation();
            }
        }

        private async void RequestActivity(ActivityAsset activity)
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Activity Request ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (activity == null)
            {
                EndOperation();
                _logger.Error("QA Activity Request failed. Target Activity is missing.");
                return;
            }

            if (!TryGetRuntimeHost("QA Activity Request failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            try
            {
                await RequestActivityCoreAsync(runtimeHost, activity, ResolveReason(activityReason, activity.ActivityName));
            }
            finally
            {
                EndOperation();
            }
        }

        private async void ClearActivity()
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Activity Clear ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (!TryGetRuntimeHost("QA Activity Clear failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            try
            {
                await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"));
            }
            finally
            {
                EndOperation();
            }
        }

        private async void ResetQaScenario()
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Scenario reset ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (!TryGetRuntimeHost("QA Scenario reset failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            try
            {
                var targetRoute = ResolveResetRoute(runtimeHost);
                if (targetRoute == null)
                {
                    _logger.Warning("QA Scenario reset aborted. reason='Missing Reset Route'.");
                    return;
                }

                var targetActivity = ResolveResetActivity(targetRoute);
                string routeName = GetAssetName(targetRoute, "<none>");
                string activityName = GetAssetName(targetActivity, "<none>");
                _logger.Info($"QA Scenario reset started. route='{FormatValue(routeName)}' activity='{FormatValue(activityName)}'.");

                if (!await RequestRouteCoreAsync(runtimeHost, targetRoute, ResolveResetStepReason("route"), allowAlreadyActive: true))
                {
                    _logger.Warning($"QA Scenario reset aborted. route='{FormatValue(routeName)}' activity='{FormatValue(activityName)}' reason='Route request failed'.");
                    return;
                }

                await Task.Yield();
                if (targetActivity != null)
                {
                    if (!await RequestActivityCoreAsync(runtimeHost, targetActivity, ResolveResetStepReason("activity"), allowAlreadyActive: true))
                    {
                        _logger.Warning($"QA Scenario reset aborted. route='{FormatValue(routeName)}' activity='{FormatValue(activityName)}' reason='Activity request failed'.");
                        return;
                    }
                }
                else if (!await ClearActivityCoreAsync(runtimeHost, ResolveResetStepReason("clear"), allowNoActiveActivity: true))
                {
                    _logger.Warning($"QA Scenario reset aborted. route='{FormatValue(routeName)}' activity='<none>' reason='Activity clear failed'.");
                    return;
                }

                _logger.Info($"QA Scenario reset completed. route='{FormatValue(routeName)}' activity='{FormatValue(activityName)}'.");
            }
            catch (Exception exception)
            {
                _logger.Error($"QA Scenario reset failed. reason='{FormatValue(exception.Message)}'.");
            }
            finally
            {
                EndOperation();
            }
        }

        private async void RunActivitySmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, secondaryActivity == null, "Secondary Activity");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Activity Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Smoke", async runtimeHost =>
            {
                if (!await RequestActivityCoreAsync(runtimeHost, secondaryActivity, ResolveReason(activityReason, GetAssetName(secondaryActivity, "QA Secondary Content Activity"))))
                {
                    return false;
                }

                await Task.Yield();
                if (!await RequestActivityCoreAsync(runtimeHost, primaryActivity, ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity"))))
                {
                    return false;
                }

                await Task.Yield();
                if (!await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear")))
                {
                    return false;
                }

                await Task.Yield();
                return await RequestActivityCoreAsync(runtimeHost, primaryActivity, ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")));
            });
        }

        private async void RunRouteSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route");
            AppendMissingQaAsset(ref missingTargets, canonicalRoute == null, "Canonical Route");
            if (!TryValidateSmokeTargets("Route Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Route Smoke", async runtimeHost =>
            {
                if (!await RequestRouteCoreAsync(runtimeHost, alternateRoute, ResolveReason(routeReason, GetAssetName(alternateRoute, "QA Alternate Route"))))
                {
                    return false;
                }

                await Task.Yield();
                return await RequestRouteCoreAsync(runtimeHost, canonicalRoute, ResolveReason(routeReason, GetAssetName(canonicalRoute, "QA Canonical Route")));
            });
        }

        private async void RunClearActivitySmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Clear Activity Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Clear Activity Smoke", async runtimeHost =>
            {
                if (!await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"), allowNoActiveActivity: true))
                {
                    return false;
                }

                await Task.Yield();
                if (!await RequestActivityCoreAsync(runtimeHost, primaryActivity, ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity"))))
                {
                    return false;
                }

                await Task.Yield();
                return await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"));
            });
        }

        private async void RunNoActivityRouteSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, noActivityRoute == null, "No-Activity Route");
            if (!TryValidateSmokeTargets("No-Activity Route Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("No-Activity Route Smoke", runtimeHost =>
                RequestRouteCoreAsync(runtimeHost, noActivityRoute, ResolveReason(routeReason, GetAssetName(noActivityRoute, "QA No Activity Route"))));
        }

        private async void RunNoContentActivitySmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, noContentActivity == null, "No-Content Activity");
            if (!TryValidateSmokeTargets("No-Content Activity Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("No-Content Activity Smoke", runtimeHost =>
                RequestActivityCoreAsync(runtimeHost, noContentActivity, ResolveReason(activityReason, GetAssetName(noContentActivity, "QA No Content Activity"))));
        }

        private async void RunNegativeSmoke()
        {
            await RunSmokeAsync("Negative Smoke", async runtimeHost =>
            {
                if (!await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"), allowNoActiveActivity: true))
                {
                    return false;
                }

                await Task.Yield();
                return await ClearActivityCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"), requireNoActiveActivity: true);
            });
        }

        private async Task RunSmokeAsync(string smokeName, Func<FrameworkRuntimeHost, Task<bool>> sequence)
        {
            EnsureLogger();

            if (!TryBeginOperation("QA Smoke ignored. Framework QA Canvas already has a request in flight."))
            {
                return;
            }

            if (!TryGetRuntimeHost("QA Smoke failed. Application Runtime is unavailable.", out var runtimeHost))
            {
                EndOperation();
                return;
            }

            _logger.Info($"QA Smoke started. name='{smokeName}'.");
            try
            {
                bool completed = await sequence(runtimeHost);
                if (completed)
                {
                    _logger.Info($"QA Smoke completed. name='{smokeName}'.");
                }
                else
                {
                    _logger.Warning($"QA Smoke aborted. name='{smokeName}'. reason='Step failed'.");
                }
            }
            catch (Exception exception)
            {
                _logger.Error($"QA Smoke failed. name='{smokeName}'. reason='{FormatValue(exception.Message)}'.");
            }
            finally
            {
                EndOperation();
            }
        }

        private async Task<bool> RequestRouteCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            RouteAsset route,
            string reason,
            bool allowAlreadyActive = false)
        {
            if (route == null)
            {
                _logger.Error("QA Route Request failed. Target Route is missing.");
                return false;
            }

            var result = await runtimeHost.RequestRouteAsync(route, QaSource, reason);
            return result.Succeeded || (allowAlreadyActive && result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive);
        }

        private async Task<bool> RequestActivityCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            ActivityAsset activity,
            string reason,
            bool allowAlreadyActive = false)
        {
            if (activity == null)
            {
                _logger.Error("QA Activity Request failed. Target Activity is missing.");
                return false;
            }

            var result = await runtimeHost.RequestActivityAsync(activity, QaSource, reason);
            return result.Succeeded || (allowAlreadyActive && result.Kind == FrameworkActivityRequestKind.IgnoredAlreadyActive);
        }

        private async Task<bool> ClearActivityCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            string reason,
            bool allowNoActiveActivity = false,
            bool requireNoActiveActivity = false)
        {
            var result = await runtimeHost.ClearActivityAsync(QaSource, reason);
            if (requireNoActiveActivity)
            {
                return result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity;
            }

            return result.Succeeded || (allowNoActiveActivity && result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity);
        }

        private bool TryGetRuntimeHost(string message, out FrameworkRuntimeHost runtimeHost)
        {
            if (FrameworkRuntimeHost.TryGetCurrent(out runtimeHost))
            {
                return true;
            }

            _logger.Error(message);
            return false;
        }

        private bool TryBeginOperation(string busyMessage)
        {
            if (_requestInFlight)
            {
                _logger.Warning(busyMessage);
                return false;
            }

            _requestInFlight = true;
            return true;
        }

        private void EndOperation()
        {
            _requestInFlight = false;
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create();
            }
        }

        private bool TryValidateSmokeTargets(string smokeName, string missingTargets)
        {
            EnsureLogger();
            if (string.IsNullOrEmpty(missingTargets))
            {
                return true;
            }

            _logger.Warning($"QA Smoke skipped. name='{smokeName}'. reason='Missing QA target(s): {FormatValue(missingTargets)}'.");
            return false;
        }

        private RouteAsset ResolveResetRoute(FrameworkRuntimeHost runtimeHost)
        {
            if (resetRoute != null)
            {
                return resetRoute;
            }

            var state = runtimeHost.State;
            return state.GameApplication != null ? state.GameApplication.StartupRoute : null;
        }

        private ActivityAsset ResolveResetActivity(RouteAsset effectiveRoute)
        {
            if (resetActivity != null)
            {
                return resetActivity;
            }

            return effectiveRoute != null ? effectiveRoute.StartupActivity : null;
        }

        private string ResolveResetStepReason(string suffix)
        {
            string baseReason = ResolveReason(resetReason, "qa.reset");
            return $"{baseReason}.{suffix}";
        }

        private static string ResolveReason(string configuredReason, string fallback)
        {
            return string.IsNullOrWhiteSpace(configuredReason) ? fallback : configuredReason.Trim();
        }

        private static string GetAssetName(Object asset, string fallback)
        {
            if (asset is RouteAsset route)
            {
                return GetName(route.RouteName, fallback);
            }

            if (asset is ActivityAsset activity)
            {
                return GetName(activity.ActivityName, fallback);
            }

            return fallback;
        }

        private string GetQaAssetsStatus()
        {
            string missingCore = string.Empty;
            AppendMissingQaAsset(ref missingCore, canonicalRoute == null, "Canonical Route");
            AppendMissingQaAsset(ref missingCore, alternateRoute == null, "Alternate Route");
            AppendMissingQaAsset(ref missingCore, primaryActivity == null, "Primary Activity");
            AppendMissingQaAsset(ref missingCore, secondaryActivity == null, "Secondary Activity");

            string missingOptional = string.Empty;
            AppendMissingQaAsset(ref missingOptional, noActivityRoute == null, "No-Activity Route");
            AppendMissingQaAsset(ref missingOptional, noContentActivity == null, "No-Content Activity");

            if (string.IsNullOrEmpty(missingCore) && string.IsNullOrEmpty(missingOptional))
            {
                return "QA scenario targets configured.";
            }

            if (string.IsNullOrEmpty(missingCore))
            {
                return $"QA core targets configured. Optional targets missing: {missingOptional}";
            }

            if (string.IsNullOrEmpty(missingOptional))
            {
                return $"QA core targets missing: {missingCore}";
            }

            return $"QA core targets missing: {missingCore}. Optional targets missing: {missingOptional}";
        }

        private static void AppendMissingQaAsset(ref string missing, bool isMissing, string label)
        {
            if (!isMissing)
            {
                return;
            }

            missing = string.IsNullOrEmpty(missing) ? label : $"{missing}, {label}";
        }

        private static string GetName(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }

        private void OnDestroy()
        {
            if (_current == this)
            {
                _current = null;
            }
        }

        private readonly struct EditorDisabledScope : IDisposable
        {
            private readonly bool _previousEnabled;

            public EditorDisabledScope(bool disabled)
            {
                _previousEnabled = GUI.enabled;
                GUI.enabled = !disabled && _previousEnabled;
            }

            public void Dispose()
            {
                GUI.enabled = _previousEnabled;
            }
        }
    }
}

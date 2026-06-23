#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.LocalContribution;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Immersive.Framework.ApiStatus;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// Minimal development QA surface for exercising framework runtime requests.
    /// API status: Development Tooling. Compiled only in the Unity Editor or development builds.
    /// Uses IMGUI to avoid adding a UGUI/package dependency to the framework runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/QA/Framework QA Canvas")]
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "QA smoke UI for Editor/development builds; not product API.")]
    public sealed class FrameworkQaCanvas : MonoBehaviour
    {
        private const string QaSource = nameof(FrameworkQaCanvas);

        private static FrameworkQaCanvas _current;
        private static int _nextWindowId = 36000;

        private FrameworkLogger _logger;
        private int _windowId;
        private bool _requestInFlight;
        private bool _showAdvancedControls;
        private Vector2 _scroll;

        [Header("Display")]
        [SerializeField] private bool showCanvas = true;
        [SerializeField] private bool persistAcrossSceneLoads = true;
        [SerializeField] private bool allowInPlayerBuild;
        [SerializeField] private Rect windowRect = new Rect(16f, 16f, 420f, 500f);

        [Header("Routes")]
        [SerializeField] private RouteAsset[] routeTargets = Array.Empty<RouteAsset>();
        [SerializeField] private string routeReason = "qa.route";

        [Header("Smoke Scenario Routes")]
        [FormerlySerializedAs("startupRoute")]
        [SerializeField] private RouteAsset canonicalRoute;
        [FormerlySerializedAs("secondaryRoute")]
        [SerializeField] private RouteAsset alternateRoute;
        [SerializeField] private RouteAsset noActivityRoute;

        [Header("Route Scene Composition Smoke")]
        [SerializeField] private RouteAsset routeSceneCompositionRoute;
        [SerializeField] private int expectedRouteSceneLoadedCount = 2;
        [SerializeField] private int expectedRouteSceneOwnedLoadedCount = 2;

        [Header("Route Release Smoke")]
        [SerializeField] private int expectedRouteReleaseReleasedCount = 1;

        [Header("Content Anchor Diagnostics Smoke")]
        [SerializeField] private int expectedContentAnchorCount = 1;
        [SerializeField] private int expectedContentAnchorCandidateCount = 1;
        [SerializeField] private int expectedContentAnchorIssueCount = 0;
        [SerializeField] private int expectedContentAnchorInvalidCount = 0;
        [SerializeField] private int expectedContentAnchorRouteMismatchCount = 0;
        [SerializeField] private int expectedContentAnchorRequiredCount = -1;
        [SerializeField] private int expectedContentAnchorOptionalCount = -1;
        [SerializeField] private int expectedContentAnchorRootCount = -1;
        [SerializeField] private int expectedContentAnchorSlotCount = -1;
        [SerializeField] private int expectedContentAnchorPointCount = -1;

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
            _logger = FrameworkLogger.Create<FrameworkQaCanvas>();
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
                DrawQaScenarioSummary();
                DrawCoreSmokeControls();
                DrawRouteContentSmokeControls();
                GUILayout.EndScrollView();
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("QA smoke control", GUILayout.ExpandWidth(true));
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

        private void DrawQaScenarioSummary()
        {
            GUILayout.Space(8f);
            GUILayout.Label("QA Scenario", GUI.skin.box);
            GUILayout.Label($"Canonical Route: {GetAssetName(canonicalRoute, "<missing>")}");
            GUILayout.Label($"Alternate Route: {GetAssetName(alternateRoute, "<missing>")}");
            GUILayout.Label($"Composition Route: {GetAssetName(routeSceneCompositionRoute, "<missing>")}");
            GUILayout.Label($"Expected Route Scenes: {Math.Max(1, expectedRouteSceneLoadedCount)} loaded / {Math.Max(1, expectedRouteSceneOwnedLoadedCount)} owned");
            GUILayout.Label($"Expected Route Release: {Math.Max(0, expectedRouteReleaseReleasedCount)} released");
            GUILayout.Label($"Expected Content Anchors: {Math.Max(0, expectedContentAnchorCount)} anchors / {Math.Max(0, expectedContentAnchorCandidateCount)} candidates");
            GUILayout.Label($"Primary Activity: {GetAssetName(primaryActivity, "<missing>")}");
            GUILayout.Label($"Secondary Activity: {GetAssetName(secondaryActivity, "<missing>")}");
            GUILayout.Label(GetCoreQaAssetsStatus());
        }

        private void DrawCoreSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Core Smokes", GUI.skin.box);
            GUILayout.Label("Use these buttons for the normal framework validation path.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Standard Smoke"))
                {
                    RunStandardSmoke();
                }

                if (GUILayout.Button("Run Activity Baseline Smoke"))
                {
                    RunActivityBaselineSmoke();
                }

                if (GUILayout.Button("Run Local Contribution Smoke"))
                {
                    RunLocalContributionSmoke();
                }

                if (GUILayout.Button("Run Runtime Content Smoke"))
                {
                    RunRuntimeContentSmoke();
                }

                if (GUILayout.Button("Validate Loaded Authoring"))
                {
                    ValidateLoadedLocalContributionsAuthoring();
                }

                if (GUILayout.Button("Reset QA Scenario"))
                {
                    ResetQaScenario();
                }
            }
        }

        private void DrawRouteContentSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Route Content Smokes", GUI.skin.box);
            GUILayout.Label("Current route-content validation path: composition, release and Content Anchor diagnostics.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Route Scene Composition Smoke"))
                {
                    RunRouteSceneCompositionSmoke();
                }

                if (GUILayout.Button("Run Route Release Smoke"))
                {
                    RunRouteReleaseSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Diagnostics Smoke"))
                {
                    RunContentAnchorDiagnosticsSmoke();
                }
            }
        }

        private void DrawAdvancedControls()
        {
            GUILayout.Space(8f);
            _showAdvancedControls = GUILayout.Toggle(_showAdvancedControls, "Show advanced/manual controls");
            if (!_showAdvancedControls)
            {
                return;
            }

            DrawBaselineResetDiagnostics();
            DrawOptionalSmokeControls();
            DrawRouteRequests();
            DrawActivityRequests();
        }

        private void DrawBaselineResetDiagnostics()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Baseline Reset Diagnostics", GUI.skin.box);

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
        }

        private void DrawOptionalSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Optional / Edge Smokes", GUI.skin.box);
            GUILayout.Label($"No-Activity Route: {GetAssetName(noActivityRoute, "<none>")}");
            GUILayout.Label($"No-Content Activity: {GetAssetName(noContentActivity, "<none>")}");

            using (new EditorDisabledScope(_requestInFlight))
            {
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

                if (GUILayout.Button("Run Route Callback Smoke"))
                {
                    RunRouteCallbackSmoke();
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

        private async void RunStandardSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route");
            AppendMissingQaAsset(ref missingTargets, canonicalRoute == null, "Canonical Route");
            AppendMissingQaAsset(ref missingTargets, secondaryActivity == null, "Secondary Activity");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Standard Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Standard Smoke", async runtimeHost =>
            {
                if (!await RequestRouteCoreAsync(runtimeHost, alternateRoute, ResolveReason(routeReason, GetAssetName(alternateRoute, "QA Alternate Route"))))
                {
                    return false;
                }

                await Task.Yield();
                if (!await RequestRouteCoreAsync(runtimeHost, canonicalRoute, ResolveReason(routeReason, GetAssetName(canonicalRoute, "QA Canonical Route"))))
                {
                    return false;
                }

                await Task.Yield();
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

        private async void RunActivityBaselineSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, secondaryActivity == null, "Secondary Activity");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Activity Baseline Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Baseline Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    if (!await RequestActivityCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity"))))
                    {
                        return false;
                    }

                    await Task.Yield();
                }


                var secondaryResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    secondaryActivity,
                    ResolveReason(activityReason, GetAssetName(secondaryActivity, "QA Secondary Content Activity")));
                if (!ValidateActivityBaselineStartedStep(secondaryResult, "secondary", secondaryActivity, requireActivityContent: true))
                {
                    return false;
                }

                await Task.Yield();

                var primaryResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    primaryActivity,
                    ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")));
                if (!ValidateActivityBaselineStartedStep(primaryResult, "primary", primaryActivity, requireActivityContent: false))
                {
                    return false;
                }

                await Task.Yield();

                var clearResult = await ClearActivityWithResultCoreAsync(runtimeHost, ResolveReason(clearActivityReason, "qa.activity.clear"));
                if (!ValidateActivityBaselineClearedStep(clearResult, "clear"))
                {
                    return false;
                }

                await Task.Yield();

                var restoreResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    primaryActivity,
                    ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")));
                return ValidateActivityBaselineStartedStep(restoreResult, "restore-primary", primaryActivity, requireActivityContent: false);
            });
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

        private async void RunLocalContributionSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, secondaryActivity == null, "Secondary Activity");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Local Contribution Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Local Contribution Smoke", async runtimeHost =>
            {
                if (!ValidateLocalContributionSmokeStep("loaded"))
                {
                    return false;
                }

                var secondaryResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    secondaryActivity,
                    ResolveReason(activityReason, GetAssetName(secondaryActivity, "QA Secondary Content Activity")));
                if (!secondaryResult.Succeeded)
                {
                    _logger.Warning($"QA Local Contribution Smoke step failed. step='secondary' reason='Activity request did not succeed' status='{FormatValue(secondaryResult.Kind.ToString())}'.");
                    return false;
                }

                await Task.Yield();
                if (!ValidateLocalContributionSmokeStep("secondary"))
                {
                    return false;
                }

                var primaryResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    primaryActivity,
                    ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")));
                if (!primaryResult.Succeeded)
                {
                    _logger.Warning($"QA Local Contribution Smoke step failed. step='primary' reason='Activity restore request did not succeed' status='{FormatValue(primaryResult.Kind.ToString())}'.");
                    return false;
                }

                await Task.Yield();
                return ValidateLocalContributionSmokeStep("primary");
            });
        }

        private bool ValidateLocalContributionSmokeStep(string stepName)
        {
            EnsureLogger();

            string normalizedStepName = string.IsNullOrWhiteSpace(stepName) ? "<unknown>" : stepName.Trim();
            RouteContentBinding[] routeBindings = FindObjectsByType<RouteContentBinding>(
                FindObjectsInactive.Include);
            ActivityLocalVisibilityAdapter[] activityAdapters = FindObjectsByType<ActivityLocalVisibilityAdapter>(
                FindObjectsInactive.Include);

            int routeBindingCount = routeBindings != null ? routeBindings.Length : 0;
            int activityAdapterCount = activityAdapters != null ? activityAdapters.Length : 0;
            if (routeBindingCount == 0)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='No RouteContentBinding found in loaded scenes' routeBindings='0' activityAdapters='{activityAdapterCount}'.");
                return false;
            }

            if (activityAdapterCount == 0)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='No ActivityLocalVisibilityAdapter found in loaded scenes' routeBindings='{routeBindingCount}' activityAdapters='0'.");
                return false;
            }

            var validationResult = LocalContributionValidator.ValidateLoadedSceneAuthored();
            var contributionSet = validationResult.ContributionSet;

            if (!validationResult.Succeeded)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Local contribution validation has blocking issues'. {validationResult.ToDiagnosticString()}");
                return false;
            }

            if (contributionSet.Count == 0)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='No valid local contribution handles were discovered'. routeBindings='{routeBindingCount}' activityAdapters='{activityAdapterCount}' {validationResult.ToDiagnosticString()}");
                return false;
            }

            if (contributionSet.RouteContentBindingCount != routeBindingCount
                || contributionSet.ActivityLocalVisibilityAdapterCount != activityAdapterCount)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Discovered contribution count does not match loaded authoring component count'. routeBindings='{routeBindingCount}' activityAdapters='{activityAdapterCount}' {validationResult.ToDiagnosticString()}");
                return false;
            }

            if (contributionSet.RequiredCount + contributionSet.OptionalCount != contributionSet.Count)
            {
                _logger.Warning($"QA Local Contribution Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Requiredness count does not match contribution count'. {validationResult.ToDiagnosticString()}");
                return false;
            }

            _logger.Info(
                "QA Local Contribution Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("routeBindings", routeBindingCount),
                    LogFields.Field("activityAdapters", activityAdapterCount),
                    LogFields.Field("localContributions", validationResult.ContributionCount),
                    LogFields.Field("validationIssues", validationResult.IssueCount),
                    LogFields.Field("blockingIssues", validationResult.BlockingIssueCount),
                    LogFields.Field("optionalSkips", validationResult.OptionalSkipCount),
                    LogFields.Field("handles", contributionSet.Count),
                    LogFields.Field("route", contributionSet.RouteCount),
                    LogFields.Field("activity", contributionSet.ActivityCount),
                    LogFields.Field("required", contributionSet.RequiredCount),
                    LogFields.Field("optional", contributionSet.OptionalCount)));
            _logger.Debug(
                "QA Local Contribution Smoke diagnostics.",
                LogFields.Field("details", validationResult.ToDiagnosticString()));
            return true;
        }

        private async void RunRuntimeContentSmoke()
        {
            await RunSmokeAsync("Runtime Content Smoke", runtimeHost =>
                Task.FromResult(RunRuntimeContentSmokeCore(runtimeHost)));
        }

        private bool RunRuntimeContentSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Runtime Content Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            string ownerId = "qa.runtime-content.smoke." + Guid.NewGuid().ToString("N");
            var owner = RuntimeContentOwner.Transient(ownerId, "QA Runtime Content Smoke");
            var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(owner, QaSource, "qa.runtime-content.root.create");
            if (!rootCreateResult.Applied && rootCreateResult.Status != RuntimeRootRegistryOperationStatus.RootAlreadyExists)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                return false;
            }

            if (!runtimeContentRuntime.TryCreateScopeContext(owner, QaSource, "qa.runtime-content.context", out var context))
            {
                _logger.Warning("QA Runtime Content Smoke failed. step='context' reason='Runtime scope context was not created'.");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.missing-context");
                return false;
            }

            var resource = RuntimeMaterializationResource.From(
                "QA",
                "qa.runtime-content.resource",
                "QA Runtime Content Smoke Resource",
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    context,
                    "qa.runtime-content.handle",
                    resource,
                    QaSource,
                    "qa.runtime-content.materialization.request",
                    out var request,
                    out var requestGuardResult))
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='materialization-request' guard='{requestGuardResult.Status}' message='{FormatValue(requestGuardResult.Message)}'.");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.request-failed");
                return false;
            }

            var handle = RuntimeContentHandle.Declared(
                request.Identity,
                QaSource,
                "qa.runtime-content.handle.declared");
            var materializationResult = RuntimeMaterializationResult.Success(
                request,
                handle,
                QaSource,
                "qa.runtime-content.materialization.synthetic-result",
                "Synthetic runtime content smoke materialization result.");
            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                QaSource,
                "qa.runtime-content.materialization.apply");

            if (!appliedMaterializationResult.Succeeded)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='materialization-apply' status='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.materialization-failed");
                return false;
            }

            if (!runtimeContentRuntime.TryGetHandle(context, request.Identity, out var registeredHandle) || registeredHandle == null || !registeredHandle.IsMaterialized)
            {
                _logger.Warning("QA Runtime Content Smoke failed. step='registered-handle' reason='Materialized handle was not registered in the runtime scope root'.");
                runtimeContentRuntime.ReleaseScopeLogically(context, RuntimeReleasePolicy.MarkReleasedAndUnregister, QaSource, "qa.runtime-content.cleanup.unregistered-handle");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.unregistered-handle");
                return false;
            }

            if (!runtimeContentRuntime.IsMaterializationRequestCurrent(
                    request,
                    QaSource,
                    "qa.runtime-content.guard.active-check",
                    out var activeGuardResult))
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='guard-active' guard='{activeGuardResult.Status}' message='{FormatValue(activeGuardResult.Message)}'.");
                runtimeContentRuntime.ReleaseScopeLogically(context, RuntimeReleasePolicy.MarkReleasedAndUnregister, QaSource, "qa.runtime-content.cleanup.guard-active-failed");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.guard-active-failed");
                return false;
            }

            var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                context,
                request.Identity,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                QaSource,
                "qa.runtime-content.release.logical");
            if (!releaseResult.Succeeded || !releaseResult.HandleUnregistered)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='release' status='{releaseResult.Status}' unregistered='{releaseResult.HandleUnregistered}' message='{FormatValue(releaseResult.Message)}'.");
                runtimeContentRuntime.ReleaseScopeLogically(context, RuntimeReleasePolicy.MarkReleasedAndUnregister, QaSource, "qa.runtime-content.cleanup.release-failed");
                runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.cleanup.release-failed");
                return false;
            }

            var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(owner, QaSource, "qa.runtime-content.root.remove");
            if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                return false;
            }

            bool staleRequestAllowed = runtimeContentRuntime.IsMaterializationRequestCurrent(
                request,
                QaSource,
                "qa.runtime-content.guard.stale-check",
                out var staleGuardResult);
            if (staleRequestAllowed || !staleGuardResult.Rejected)
            {
                _logger.Warning($"QA Runtime Content Smoke failed. step='guard-stale' allowed='{staleRequestAllowed}' guard='{staleGuardResult.Status}' message='{FormatValue(staleGuardResult.Message)}'.");
                return false;
            }

            _logger.Info($"QA Runtime Content Smoke step completed. rootCreate='{rootCreateResult.Status}' materialization='{appliedMaterializationResult.Status}' release='{releaseResult.Status}' releaseUnregistered='{releaseResult.HandleUnregistered}' rootRemove='{rootRemoveResult.Status}' staleGuard='{staleGuardResult.Status}' runtimeRootCount='{runtimeContentRuntime.RootCount}'.");
            return true;
        }

        private async void RunRouteCallbackSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route");
            AppendMissingQaAsset(ref missingTargets, canonicalRoute == null, "Canonical Route");
            if (!TryValidateSmokeTargets("Route Callback Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Route Callback Smoke", async runtimeHost =>
            {
                var firstResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    alternateRoute,
                    ResolveReason(routeReason, GetAssetName(alternateRoute, "QA Alternate Route")));
                if (!ValidateRouteCallbackSmokeStep(firstResult, "alternate"))
                {
                    return false;
                }

                await Task.Yield();

                var secondResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    canonicalRoute,
                    ResolveReason(routeReason, GetAssetName(canonicalRoute, "QA Canonical Route")));
                return ValidateRouteCallbackSmokeStep(secondResult, "canonical");
            });
        }

        private async void RunRouteSceneCompositionSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Route Scene Composition Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");

            if (routeSceneCompositionRoute != null)
            {
                AppendMissingQaAsset(ref missingTargets, !routeSceneCompositionRoute.HasRouteContentProfile, "Route Content Profile on Composition Route");
                if (routeSceneCompositionRoute.HasRouteContentProfile)
                {
                    AppendMissingQaAsset(
                        ref missingTargets,
                        !routeSceneCompositionRoute.RouteContentProfile.HasAdditionalScenes,
                        "Additional Scene in Route Content Profile");
                }
            }

            if (!TryValidateSmokeTargets("Route Scene Composition Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Route Scene Composition Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Route Scene Composition Smoke step failed. step='prepare' reason='Composition Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.route.composition.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Route Scene Composition Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var compositionResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.route.composition")));
                return ValidateRouteSceneCompositionSmokeStep(compositionResult, "composition");
            });
        }

        private async void RunRouteReleaseSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Route Scene Composition Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Release Target Route");

            if (routeSceneCompositionRoute != null)
            {
                AppendMissingQaAsset(ref missingTargets, !routeSceneCompositionRoute.HasRouteContentProfile, "Route Content Profile on Composition Route");
                if (routeSceneCompositionRoute.HasRouteContentProfile)
                {
                    AppendMissingQaAsset(
                        ref missingTargets,
                        !routeSceneCompositionRoute.RouteContentProfile.HasAdditionalScenes,
                        "Additional Scene in Route Content Profile");
                }
            }

            if (!TryValidateSmokeTargets("Route Release Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Route Release Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    var prepareCompositionResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        routeSceneCompositionRoute,
                        ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.route.release.prepare")));
                    if (!ValidateRouteSceneCompositionSmokeStep(prepareCompositionResult, "prepare-composition"))
                    {
                        return false;
                    }

                    await Task.Yield();
                }

                if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                {
                    _logger.Warning("QA Route Release Smoke step failed. step='release' reason='Alternate Route points to the same Route as the release source'.");
                    return false;
                }

                var releaseResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    alternateRoute,
                    ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.route.release")));
                if (!ValidateRouteReleaseSmokeStep(releaseResult, "release"))
                {
                    return false;
                }

                await Task.Yield();

                var restoreCompositionResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.route.release.restore")));
                return ValidateRouteSceneCompositionSmokeStep(restoreCompositionResult, "restore-composition");
            });
        }

        private async void RunContentAnchorDiagnosticsSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Content Anchor Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");

            if (!TryValidateSmokeTargets("Content Anchor Diagnostics Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Content Anchor Diagnostics Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Content Anchor Diagnostics Smoke step failed. step='prepare' reason='Content Anchor Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.content-anchor.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var anchorResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.content-anchor.diagnostics")));
                return ValidateContentAnchorDiagnosticsSmokeStep(anchorResult, "anchors");
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
            var result = await RequestRouteWithResultCoreAsync(runtimeHost, route, reason);
            return result.Succeeded || (allowAlreadyActive && result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive);
        }

        private async Task<FrameworkRouteRequestResult> RequestRouteWithResultCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            RouteAsset route,
            string reason)
        {
            if (route == null)
            {
                _logger.Error("QA Route Request failed. Target Route is missing.");
                return FrameworkRouteRequestResult.FailedInvalidConfig(
                    "QA Route Request failed. Target Route is missing.",
                    route,
                    QaSource,
                    reason);
            }

            return await runtimeHost.RequestRouteAsync(route, QaSource, reason);
        }

        private bool ValidateRouteCallbackSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = string.IsNullOrWhiteSpace(stepName) ? "<unknown>" : stepName.Trim();
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Route Callback Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Route Callback Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var enterResult = lifecycleResult.RouteContentEnterResult;
            var exitResult = lifecycleResult.RouteContentExitResult;
            string enterRouteName = GetAssetName(enterResult.Route, routeName);
            string exitRouteName = GetAssetName(exitResult.Route, "<none>");

            if (!IsValidRouteCallbackDispatch(enterResult))
            {
                _logger.Warning($"QA Route Callback Smoke step failed. step='{FormatValue(normalizedStepName)}' requestedRoute='{FormatValue(routeName)}' phase='Enter' callbackRoute='{FormatValue(enterRouteName)}' executed='{enterResult.Executed}' bindings='{enterResult.BindingCount}' receivers='{enterResult.ReceiverCount}' failed='{enterResult.FailedReceiverCount}'.");
                return false;
            }

            if (!IsValidRouteCallbackDispatch(exitResult))
            {
                _logger.Warning($"QA Route Callback Smoke step failed. step='{FormatValue(normalizedStepName)}' requestedRoute='{FormatValue(routeName)}' phase='Exit' callbackRoute='{FormatValue(exitRouteName)}' executed='{exitResult.Executed}' bindings='{exitResult.BindingCount}' receivers='{exitResult.ReceiverCount}' failed='{exitResult.FailedReceiverCount}'.");
                return false;
            }

            _logger.Info($"QA Route Callback Smoke step completed. step='{FormatValue(normalizedStepName)}' requestedRoute='{FormatValue(routeName)}' routeContentEnterRoute='{FormatValue(enterRouteName)}' routeContentEnterReceivers='{enterResult.ReceiverCount}' routeContentExitRoute='{FormatValue(exitRouteName)}' routeContentExitReceivers='{exitResult.ReceiverCount}'.");
            return true;
        }

        private bool ValidateActivityBaselineStartedStep(
            FrameworkActivityRequestResult result,
            string stepName,
            ActivityAsset expectedActivity,
            bool requireActivityContent)
        {
            string normalizedStepName = string.IsNullOrWhiteSpace(stepName) ? "<unknown>" : stepName.Trim();
            string expectedActivityName = GetAssetName(expectedActivity, "<missing>");

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' reason='Activity request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlowResult = result.ActivityFlowResult;
            if (!activityFlowResult.Started)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' reason='Activity flow did not start'.");
                return false;
            }

            if (!ReferenceEquals(activityFlowResult.Activity, expectedActivity))
            {
                string actualActivityName = activityFlowResult.Activity != null
                    ? GetAssetName(activityFlowResult.Activity, "<unnamed>")
                    : "<none>";
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' expectedActivity='{FormatValue(expectedActivityName)}' actualActivity='{FormatValue(actualActivityName)}' reason='Unexpected active Activity'.");
                return false;
            }

            var readinessState = activityFlowResult.ActivityReadinessState;
            if (!readinessState.IsReady || readinessState.HasBlockingIssues)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' activityReadiness='{FormatValue(readinessState.DiagnosticStatus)}' activityReadinessReason='{FormatValue(readinessState.DiagnosticReason)}' activityReadinessIssues='{readinessState.BlockingIssueCount}'.");
                return false;
            }

            var activityContentResult = activityFlowResult.ActivityContentResult;
            if (activityContentResult.HasLifecycleFailures)
            {
                var lifecycleResult = activityContentResult.LifecycleResult;
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' reason='Activity Content lifecycle failure' activityContentEnterFailed='{lifecycleResult.EnterFailedReceiverCount}' activityContentExitFailed='{lifecycleResult.ExitFailedReceiverCount}'.");
                return false;
            }

            if (requireActivityContent && !activityFlowResult.HasActivityContent)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' reason='Expected Activity Content Set handle was not registered' activityContentHandles='{activityFlowResult.ActivityContentSet.Count}'.");
                return false;
            }

            var lifecycle = activityContentResult.LifecycleResult;
            _logger.Info($"QA Activity Baseline Smoke step completed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(expectedActivityName)}' activityReadiness='{FormatValue(readinessState.DiagnosticStatus)}' activityReadinessIssues='{readinessState.BlockingIssueCount}' activityContentHandles='{activityFlowResult.ActivityContentSet.Count}' activityContentLifecycle='{FormatValue(lifecycle.DiagnosticStatus)}' activityContentEnterFailed='{lifecycle.EnterFailedReceiverCount}' activityContentExitFailed='{lifecycle.ExitFailedReceiverCount}'.");
            return true;
        }

        private bool ValidateActivityBaselineClearedStep(FrameworkActivityRequestResult result, string stepName)
        {
            string normalizedStepName = string.IsNullOrWhiteSpace(stepName) ? "<unknown>" : stepName.Trim();

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Clear Activity request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlowResult = result.ActivityFlowResult;
            if (!activityFlowResult.Cleared || !activityFlowResult.ActivityState.IsNone)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' reason='Activity flow did not clear to None' activityState='{FormatValue(activityFlowResult.ActivityState.DiagnosticStatus)}'.");
                return false;
            }

            var readinessState = activityFlowResult.ActivityReadinessState;
            if (!readinessState.IsNone || readinessState.HasBlockingIssues)
            {
                _logger.Warning($"QA Activity Baseline Smoke step failed. step='{FormatValue(normalizedStepName)}' activityReadiness='{FormatValue(readinessState.DiagnosticStatus)}' activityReadinessReason='{FormatValue(readinessState.DiagnosticReason)}' activityReadinessIssues='{readinessState.BlockingIssueCount}' reason='Expected no active Activity readiness'.");
                return false;
            }

            _logger.Info($"QA Activity Baseline Smoke step completed. step='{FormatValue(normalizedStepName)}' activityState='{FormatValue(activityFlowResult.ActivityState.DiagnosticStatus)}' activityReadiness='{FormatValue(readinessState.DiagnosticStatus)}' activityReadinessReason='{FormatValue(readinessState.DiagnosticReason)}' activityReadinessIssues='{readinessState.BlockingIssueCount}' activityContentHandles='{activityFlowResult.ActivityContentSet.Count}'.");
            return true;
        }

        private bool ValidateRouteSceneCompositionSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = string.IsNullOrWhiteSpace(stepName) ? "<unknown>" : stepName.Trim();
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var composition = lifecycleResult.RouteSceneCompositionResult;
            if (!composition.Succeeded || composition.HasBlockingIssues)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route scene composition did not succeed' status='{FormatValue(composition.Status.ToString())}' loaded='{composition.LoadedCount}' failed='{composition.FailedCount}' blockingIssues='{composition.BlockingIssueCount}'.");
                return false;
            }

            int expectedLoaded = Math.Max(1, expectedRouteSceneLoadedCount);
            if (composition.LoadedCount < expectedLoaded)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Loaded scene count below expectation' expectedLoaded='{expectedLoaded}' actualLoaded='{composition.LoadedCount}' failed='{composition.FailedCount}' blockingIssues='{composition.BlockingIssueCount}'.");
                return false;
            }

            int expectedOwnedLoaded = Math.Max(1, expectedRouteSceneOwnedLoadedCount);
            if (composition.OwnedLoadedCount < expectedOwnedLoaded)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Owned loaded scene count below expectation' expectedOwnedLoaded='{expectedOwnedLoaded}' actualOwnedLoaded='{composition.OwnedLoadedCount}' loaded='{composition.LoadedCount}'.");
                return false;
            }

            var contentSet = lifecycleResult.RouteContentSet;
            if (contentSet.Count < expectedOwnedLoaded)
            {
                _logger.Warning($"QA Route Scene Composition Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route Content Set handle count below expectation' expectedHandles='{expectedOwnedLoaded}' actualHandles='{contentSet.Count}' loaded='{composition.LoadedCount}' ownedLoaded='{composition.OwnedLoadedCount}'.");
                return false;
            }

            _logger.Info(
                "QA Route Scene Composition Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("routeSceneComposition", composition.Status.ToString()),
                    LogFields.Field("routeSceneEntries", composition.EntryCount),
                    LogFields.Field("routeSceneLoaded", composition.LoadedCount),
                    LogFields.Field("routeSceneOwnedLoaded", composition.OwnedLoadedCount),
                    LogFields.Field("routeSceneAlreadyLoaded", composition.AlreadyLoadedCount),
                    LogFields.Field("routeSceneFailed", composition.FailedCount),
                    LogFields.Field("routeSceneIssues", composition.IssueCount),
                    LogFields.Field("routeSceneBlockingIssues", composition.BlockingIssueCount),
                    LogFields.Field("routeContentHandles", contentSet.Count),
                    LogFields.Field("routeContentOwned", contentSet.OwnedCount),
                    LogFields.Field("routeContentDiagnosticOnly", contentSet.DiagnosticOnlyCount)));
            _logger.Debug(
                "QA Route Scene Composition Smoke diagnostics.",
                LogFields.Field("details", composition.ToDiagnosticString()));
            return true;
        }

        private bool ValidateRouteReleaseSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = string.IsNullOrWhiteSpace(stepName) ? "<unknown>" : stepName.Trim();
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Route Release Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Route Release Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var release = lifecycleResult.ContentReleaseResult;
            if (release.Failed || release.HasBlockingIssues)
            {
                _logger.Warning($"QA Route Release Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route content release failed' status='{FormatValue(release.Status.ToString())}' released='{release.ReleasedCount}' skipped='{release.SkippedCount}' failed='{release.FailedCount}' blockingIssues='{release.BlockingIssueCount}'.");
                return false;
            }

            int expectedReleased = Math.Max(0, expectedRouteReleaseReleasedCount);
            if (release.ReleasedCount < expectedReleased)
            {
                _logger.Warning($"QA Route Release Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Released count below expectation' expectedReleased='{expectedReleased}' actualReleased='{release.ReleasedCount}' skipped='{release.SkippedCount}' failed='{release.FailedCount}'.");
                return false;
            }

            _logger.Info(
                "QA Route Release Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("routeRelease", release.Status.ToString()),
                    LogFields.Field("routeReleasePlanned", release.PlannedCount),
                    LogFields.Field("routeReleaseReleased", release.ReleasedCount),
                    LogFields.Field("routeReleaseSkipped", release.SkippedCount),
                    LogFields.Field("routeReleaseFailed", release.FailedCount),
                    LogFields.Field("routeReleaseIssues", release.IssueCount),
                    LogFields.Field("routeReleaseBlockingIssues", release.BlockingIssueCount),
                    LogFields.Field("routeSceneComposition", lifecycleResult.RouteSceneCompositionResult.Status.ToString()),
                    LogFields.Field("routeSceneLoaded", lifecycleResult.RouteSceneCompositionResult.LoadedCount),
                    LogFields.Field("routeContentHandles", lifecycleResult.RouteContentSet.Count)));
            _logger.Debug(
                "QA Route Release Smoke diagnostics.",
                LogFields.Field("details", release.ToDiagnosticString()));
            return true;
        }

        private bool ValidateContentAnchorDiagnosticsSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = string.IsNullOrWhiteSpace(stepName) ? "<unknown>" : stepName.Trim();
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var discovery = lifecycleResult.ContentAnchorDiscoveryResult;
            var anchorSet = lifecycleResult.ContentAnchorSet;
            if (!ValidateMinimum("contentAnchors", Math.Max(0, expectedContentAnchorCount), discovery.AnchorCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateMinimum("contentAnchorCandidates", Math.Max(0, expectedContentAnchorCandidateCount), discovery.CandidateCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("contentAnchorIssues", Math.Max(0, expectedContentAnchorIssueCount), discovery.IssueCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("contentAnchorInvalid", Math.Max(0, expectedContentAnchorInvalidCount), discovery.InvalidAuthoringCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("contentAnchorRouteMismatch", Math.Max(0, expectedContentAnchorRouteMismatchCount), discovery.SkippedRouteMismatchCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorRequired", expectedContentAnchorRequiredCount, anchorSet.RequiredCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorOptional", expectedContentAnchorOptionalCount, anchorSet.OptionalCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorRoot", expectedContentAnchorRootCount, anchorSet.RootCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorSlot", expectedContentAnchorSlotCount, anchorSet.SlotCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateOptionalExact("contentAnchorPoint", expectedContentAnchorPointCount, anchorSet.PointCount, normalizedStepName, routeName))
            {
                return false;
            }

            _logger.Info(
                "QA Content Anchor Diagnostics Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("contentAnchors", discovery.AnchorCount),
                    LogFields.Field("contentAnchorCandidates", discovery.CandidateCount),
                    LogFields.Field("contentAnchorAccepted", discovery.AcceptedCount),
                    LogFields.Field("contentAnchorRoute", anchorSet.RouteCount),
                    LogFields.Field("contentAnchorActivity", anchorSet.ActivityCount),
                    LogFields.Field("contentAnchorLocal", anchorSet.LocalCount),
                    LogFields.Field("contentAnchorRequired", anchorSet.RequiredCount),
                    LogFields.Field("contentAnchorOptional", anchorSet.OptionalCount),
                    LogFields.Field("contentAnchorRoot", anchorSet.RootCount),
                    LogFields.Field("contentAnchorSlot", anchorSet.SlotCount),
                    LogFields.Field("contentAnchorPoint", anchorSet.PointCount),
                    LogFields.Field("contentAnchorIssues", discovery.IssueCount),
                    LogFields.Field("contentAnchorInvalid", discovery.InvalidAuthoringCount),
                    LogFields.Field("contentAnchorRouteMismatch", discovery.SkippedRouteMismatchCount),
                    LogFields.Field("contentAnchorDuplicateIdentity", anchorSet.DuplicateIdentityIssueCount),
                    LogFields.Field("contentAnchorDuplicateId", anchorSet.DuplicateAnchorIdIssueCount)));
            _logger.Debug(
                "QA Content Anchor Diagnostics Smoke diagnostics.",
                LogFields.Field("details", discovery.ToDiagnosticString()));
            return true;
        }

        private bool ValidateMinimum(string metricName, int expectedMinimum, int actual, string stepName, string routeName)
        {
            if (actual >= expectedMinimum)
            {
                return true;
            }

            _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='{FormatValue(stepName)}' route='{FormatValue(routeName)}' metric='{FormatValue(metricName)}' reason='Metric below expectation' expectedMinimum='{expectedMinimum}' actual='{actual}'.");
            return false;
        }

        private bool ValidateExact(string metricName, int expected, int actual, string stepName, string routeName)
        {
            if (actual == expected)
            {
                return true;
            }

            _logger.Warning($"QA Content Anchor Diagnostics Smoke step failed. step='{FormatValue(stepName)}' route='{FormatValue(routeName)}' metric='{FormatValue(metricName)}' reason='Metric did not match expectation' expected='{expected}' actual='{actual}'.");
            return false;
        }

        private bool ValidateOptionalExact(string metricName, int expected, int actual, string stepName, string routeName)
        {
            return expected < 0 || ValidateExact(metricName, expected, actual, stepName, routeName);
        }

        private static bool IsValidRouteCallbackDispatch(RouteContentLifecycleDispatchResult result)
        {
            return result is { Executed: true, BindingCount: > 0, ReceiverCount: > 0, FailedReceiverCount: 0 };
        }


        private void ValidateLoadedRouteContentAuthoring()
        {
            ValidateLoadedLocalContributionsAuthoring();
        }

        private void ValidateLoadedLocalContributionsAuthoring()
        {
            EnsureLogger();

            RouteContentBinding[] routeBindings = FindObjectsByType<RouteContentBinding>(
                FindObjectsInactive.Include);
            ActivityLocalVisibilityAdapter[] activityAdapters = FindObjectsByType<ActivityLocalVisibilityAdapter>(
                FindObjectsInactive.Include);
            RouteContentAnchor[] routeContentAnchors = FindObjectsByType<RouteContentAnchor>(
                FindObjectsInactive.Include);

            int routeBindingCount = routeBindings != null ? routeBindings.Length : 0;
            int activityAdapterCount = activityAdapters != null ? activityAdapters.Length : 0;
            int routeContentAnchorCount = routeContentAnchors != null ? routeContentAnchors.Length : 0;

            if (routeBindingCount == 0 && activityAdapterCount == 0 && routeContentAnchorCount == 0)
            {
                _logger.Warning("QA Authoring Validation completed. scope='Loaded Authoring' routeBindings='0' activityAdapters='0' routeContentAnchors='0' localContributions='0' contentAnchors='0' issues='1' reason='No local contribution or Content Anchor authoring components found in loaded scenes'.");
                return;
            }

            int issueCount = 0;
            int errorCount = 0;
            int warningCount = 0;
            var contentAnchorDeclarations = new List<ContentAnchorDeclaration>(Math.Max(0, routeContentAnchorCount));

            if (routeBindings != null)
            {
                for (int i = 0; i < routeBindings.Length; i++)
                {
                    ValidateLoadedRouteContentBinding(routeBindings[i], ref issueCount, ref errorCount, ref warningCount);
                }
            }

            if (routeContentAnchors != null)
            {
                for (int i = 0; i < routeContentAnchors.Length; i++)
                {
                    ValidateLoadedRouteContentAnchor(
                        routeContentAnchors[i],
                        contentAnchorDeclarations,
                        ref issueCount,
                        ref errorCount,
                        ref warningCount);
                }
            }

            var contentAnchorSet = ContentAnchorSet.FromDeclarations(contentAnchorDeclarations);
            AddLoadedRouteContentAnchorSetIssues(contentAnchorSet, ref issueCount, ref errorCount, ref warningCount);

            var validationResult = LocalContributionValidator.ValidateLoadedSceneAuthored();
            AddLocalContributionValidationIssues(validationResult, ref issueCount, ref errorCount, ref warningCount);

            if (issueCount == 0)
            {
                _logger.Info(
                    "QA Authoring Validation completed.",
                    LogFields.Of(
                        LogFields.Field("scope", "Loaded Authoring"),
                        LogFields.Field("routeBindings", routeBindingCount),
                        LogFields.Field("activityAdapters", activityAdapterCount),
                        LogFields.Field("routeContentAnchors", routeContentAnchorCount),
                        LogFields.Field("issues", 0),
                        LogFields.Field("localContributions", validationResult.ContributionCount),
                        LogFields.Field("blockingIssues", validationResult.BlockingIssueCount),
                        LogFields.Field("optionalSkips", validationResult.OptionalSkipCount),
                        LogFields.Field("contentAnchors", contentAnchorSet.Count),
                        LogFields.Field("contentAnchorIssues", contentAnchorSet.IssueCount),
                        LogFields.Field("contentAnchorDuplicateIdentity", contentAnchorSet.DuplicateIdentityIssueCount),
                        LogFields.Field("contentAnchorDuplicateId", contentAnchorSet.DuplicateAnchorIdIssueCount)));
                _logger.Debug(
                    "QA Authoring Validation diagnostics.",
                    LogFields.Of(
                        LogFields.Field("localContributions", validationResult.ToDiagnosticString()),
                        LogFields.Field("contentAnchors", contentAnchorSet.ToDiagnosticString())));
                return;
            }

            _logger.Warning(
                "QA Authoring Validation completed with issues.",
                LogFields.Of(
                    LogFields.Field("scope", "Loaded Authoring"),
                    LogFields.Field("routeBindings", routeBindingCount),
                    LogFields.Field("activityAdapters", activityAdapterCount),
                    LogFields.Field("routeContentAnchors", routeContentAnchorCount),
                    LogFields.Field("issues", issueCount),
                    LogFields.Field("errors", errorCount),
                    LogFields.Field("warnings", warningCount),
                    LogFields.Field("localContributions", validationResult.ContributionCount),
                    LogFields.Field("blockingIssues", validationResult.BlockingIssueCount),
                    LogFields.Field("optionalSkips", validationResult.OptionalSkipCount),
                    LogFields.Field("contentAnchors", contentAnchorSet.Count),
                    LogFields.Field("contentAnchorIssues", contentAnchorSet.IssueCount),
                    LogFields.Field("contentAnchorDuplicateIdentity", contentAnchorSet.DuplicateIdentityIssueCount),
                    LogFields.Field("contentAnchorDuplicateId", contentAnchorSet.DuplicateAnchorIdIssueCount),
                    LogFields.Field("localContributionDetails", validationResult.ToDiagnosticString()),
                    LogFields.Field("contentAnchorDetails", contentAnchorSet.ToDiagnosticString())));
        }

        private void AddLocalContributionValidationIssues(
            LocalContributionValidationResult validationResult,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (!validationResult.HasIssues)
            {
                return;
            }

            var issues = validationResult.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Blocking)
                {
                    AddQaAuthoringIssue(
                        ref issueCount,
                        ref errorCount,
                        $"LocalContributionValidation {issues[i].ToDiagnosticString()}.");
                    continue;
                }

                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"LocalContributionValidation {issues[i].ToDiagnosticString()}.");
            }
        }

        private void ValidateLoadedRouteContentAnchor(
            RouteContentAnchor anchor,
            List<ContentAnchorDeclaration> declarations,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (anchor == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    "RouteContentAnchor is missing.");
                return;
            }

            string objectName = anchor.gameObject != null ? anchor.gameObject.name : "<missing>";
            string sceneName = anchor.gameObject != null && anchor.gameObject.scene.IsValid()
                ? anchor.gameObject.scene.name
                : "<no-scene>";
            bool hasBlockingAuthoringIssue = false;

            if (anchor.Route == null)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Route'.");
            }

            if (!anchor.HasExplicitAnchorId)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Anchor Id'.");
            }

            if (!anchor.HasExplicitKind)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Unknown Kind'.");
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), anchor.Requiredness))
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Invalid Requiredness'.");
            }

            if (anchor.Route != null && !DoesAnchorSceneMatchRoute(anchor))
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' route='{FormatValue(GetAssetName(anchor.Route, "<unnamed>"))}' issue='Route does not declare this scene'.");
            }

            if (hasBlockingAuthoringIssue)
            {
                return;
            }

            try
            {
                if (anchor.TryCreateDeclaration(out var declaration))
                {
                    declarations.Add(declaration);
                }
            }
            catch (Exception exception)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Declaration creation failed' message='{FormatValue(exception.Message)}'.");
            }
        }

        private void AddLoadedRouteContentAnchorSetIssues(
            ContentAnchorSet contentAnchorSet,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (!contentAnchorSet.HasIssues)
            {
                return;
            }

            var issues = contentAnchorSet.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                switch (issue.Kind)
                {
                    case ContentAnchorSetIssueKind.DuplicateIdentity:
                    case ContentAnchorSetIssueKind.DuplicateAnchorId:
                    case ContentAnchorSetIssueKind.InvalidDeclaration:
                        AddQaAuthoringIssue(
                            ref issueCount,
                            ref errorCount,
                            $"ContentAnchorSet {issue.ToDiagnosticString()}.");
                        break;
                    default:
                        AddQaAuthoringWarning(
                            ref issueCount,
                            ref warningCount,
                            $"ContentAnchorSet {issue.ToDiagnosticString()}.");
                        break;
                }
            }
        }

        private void ValidateLoadedRouteContentBinding(
            RouteContentBinding binding,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (binding == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    "RouteContentBinding is missing.");
                return;
            }

            string objectName = binding.gameObject != null ? binding.gameObject.name : "<missing>";
            string sceneName = binding.gameObject != null && binding.gameObject.scene.IsValid()
                ? binding.gameObject.scene.name
                : "<no-scene>";

            if (binding.Route == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"RouteContentBinding object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Route'.");
                return;
            }

            if (!DoesBindingSceneMatchRoute(binding))
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"RouteContentBinding object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' route='{FormatValue(GetAssetName(binding.Route, "<unnamed>"))}' issue='Route does not own this scene'. expectedPrimaryScene='{FormatValue(binding.Route.PrimaryScenePath)}'.");
            }

            int receiverCount = CountRouteContentLifecycleReceivers(binding);
            if (receiverCount == 0)
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"RouteContentBinding object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' route='{FormatValue(GetAssetName(binding.Route, "<unnamed>"))}' issue='No IRouteContentLifecycleReceiver under binding'.");
            }
        }

        private void AddQaAuthoringIssue(
            ref int issueCount,
            ref int errorCount,
            string message)
        {
            issueCount++;
            errorCount++;
            _logger.Error($"QA Authoring Validation issue. severity='Error' {message}");
        }

        private void AddQaAuthoringWarning(
            ref int issueCount,
            ref int warningCount,
            string message)
        {
            issueCount++;
            warningCount++;
            _logger.Warning($"QA Authoring Validation issue. severity='Warning' {message}");
        }

        private static bool DoesAnchorSceneMatchRoute(RouteContentAnchor anchor)
        {
            if (anchor == null || anchor.Route == null || anchor.gameObject == null)
            {
                return false;
            }

            var scene = anchor.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return true;
            }

            return DoesRouteDeclareScene(anchor.Route, scene.path, scene.name);
        }

        private static bool DoesRouteDeclareScene(RouteAsset route, string scenePath, string sceneName)
        {
            if (route == null)
            {
                return false;
            }

            if (MatchesScene(route.PrimaryScenePath, route.PrimarySceneName, scenePath, sceneName))
            {
                return true;
            }

            var profile = route.RouteContentProfile;
            if (profile == null || profile.AdditionalScenes == null)
            {
                return false;
            }

            for (int i = 0; i < profile.AdditionalScenes.Count; i++)
            {
                var entry = profile.AdditionalScenes[i];
                if (entry != null && MatchesScene(entry.ScenePath, entry.SceneName, scenePath, sceneName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesScene(string declaredPath, string declaredName, string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(declaredPath)
                && !string.IsNullOrWhiteSpace(scenePath)
                && string.Equals(declaredPath.Trim(), scenePath.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(declaredName)
                && !string.IsNullOrWhiteSpace(sceneName)
                && string.Equals(declaredName.Trim(), sceneName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool DoesBindingSceneMatchRoute(RouteContentBinding binding)
        {
            if (binding == null || binding.Route == null || binding.gameObject == null)
            {
                return false;
            }

            var scene = binding.gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(binding.Route.PrimaryScenePath)
                && !string.IsNullOrWhiteSpace(scene.path))
            {
                return string.Equals(scene.path, binding.Route.PrimaryScenePath, StringComparison.OrdinalIgnoreCase);
            }

            return !string.IsNullOrWhiteSpace(binding.Route.PrimarySceneName)
                && string.Equals(scene.name, binding.Route.PrimarySceneName, StringComparison.OrdinalIgnoreCase);
        }

        private static int CountRouteContentLifecycleReceivers(RouteContentBinding binding)
        {
            if (binding == null)
            {
                return 0;
            }

            MonoBehaviour[] behaviours = binding.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IRouteContentLifecycleReceiver)
                {
                    count++;
                }
            }

            return count;
        }

        private async Task<bool> RequestActivityCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            ActivityAsset activity,
            string reason,
            bool allowAlreadyActive = false)
        {
            var result = await RequestActivityWithResultCoreAsync(runtimeHost, activity, reason);
            return result.Succeeded || (allowAlreadyActive && result.Kind == FrameworkActivityRequestKind.IgnoredAlreadyActive);
        }

        private async Task<FrameworkActivityRequestResult> RequestActivityWithResultCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            ActivityAsset activity,
            string reason)
        {
            if (activity == null)
            {
                _logger.Error("QA Activity Request failed. Target Activity is missing.");
                return FrameworkActivityRequestResult.FailedInvalidConfig(
                    "QA Activity Request failed. Target Activity is missing.",
                    activity,
                    QaSource,
                    reason);
            }

            return await runtimeHost.RequestActivityAsync(activity, QaSource, reason);
        }

        private async Task<bool> ClearActivityCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            string reason,
            bool allowNoActiveActivity = false,
            bool requireNoActiveActivity = false)
        {
            var result = await ClearActivityWithResultCoreAsync(runtimeHost, reason);
            if (requireNoActiveActivity)
            {
                return result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity;
            }

            return result.Succeeded || (allowNoActiveActivity && result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity);
        }

        private async Task<FrameworkActivityRequestResult> ClearActivityWithResultCoreAsync(
            FrameworkRuntimeHost runtimeHost,
            string reason)
        {
            return await runtimeHost.ClearActivityAsync(QaSource, reason);
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
            _logger ??= FrameworkLogger.Create<FrameworkQaCanvas>();
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

        private string GetCoreQaAssetsStatus()
        {
            string missingCore = string.Empty;
            AppendMissingQaAsset(ref missingCore, canonicalRoute == null, "Canonical Route");
            AppendMissingQaAsset(ref missingCore, alternateRoute == null, "Alternate Route");
            AppendMissingQaAsset(ref missingCore, primaryActivity == null, "Primary Activity");
            AppendMissingQaAsset(ref missingCore, secondaryActivity == null, "Secondary Activity");

            return string.IsNullOrEmpty(missingCore)
                ? "Core QA targets configured."
                : $"Core QA targets missing: {missingCore}";
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
            AppendMissingQaAsset(ref missingOptional, routeSceneCompositionRoute == null, "Route Scene Composition Route");

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
#endif

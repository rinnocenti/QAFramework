using Immersive.Framework.Common;
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
using Immersive.Framework.CycleReset;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.InputMode;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        private bool _showTransitionEffectDiagnostics;
        private bool _showPauseDiagnostics;
        private bool _showSnapshotDiagnostics;
        private bool _showLoadingDiagnostics;
        private bool _showUnityInputDiagnostics;
        private bool _showRouteContentDiagnostics;
        private bool _showFoundationDiagnostics;
        private bool _showResetObjectDiagnostics;
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

        [Header("Activity Content Anchor Diagnostics Smoke")]
        [SerializeField] private int expectedActivityContentAnchorCount = 0;
        [SerializeField] private int expectedActivityContentAnchorCandidateCount = 0;
        [SerializeField] private int expectedActivityContentAnchorIssueCount = 0;
        [SerializeField] private int expectedActivityContentAnchorInvalidCount = 0;
        [SerializeField] private int expectedActivityContentAnchorActivityMismatchCount = 0;

        [Header("Activity Content Anchor Positive Smoke")]
        [SerializeField] private string positiveActivityContentAnchorId = "qa.activity.anchor.positive";
        [SerializeField] private ContentAnchorKind positiveActivityContentAnchorKind = ContentAnchorKind.Slot;
        [SerializeField] private ContentAnchorRequiredness positiveActivityContentAnchorRequiredness = ContentAnchorRequiredness.Optional;

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
                DrawDiagnosticSmokeControls();
                DrawAdvancedControls();
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
            GUILayout.Label($"Content Anchor Bindings: {runtimeHost.ContentAnchorBindingCount}");
            if (runtimeHost.TryGetPauseSnapshot(out var pauseSnapshot))
            {
                GUILayout.Label($"Pause State: {pauseSnapshot.State} / Gate blockers: {runtimeHost.PauseGateSnapshot.BlockerCount}");
            }
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
            GUILayout.Label($"Expected Activity Content Anchors: {Math.Max(0, expectedActivityContentAnchorCount)} anchors / {Math.Max(0, expectedActivityContentAnchorCandidateCount)} candidates");
            GUILayout.Label($"Primary Activity: {GetAssetName(primaryActivity, "<missing>")}");
            GUILayout.Label($"Secondary Activity: {GetAssetName(secondaryActivity, "<missing>")}");
            GUILayout.Label(GetCoreQaAssetsStatus());
        }

        private void DrawCoreSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Core Smokes", GUI.skin.box);
            GUILayout.Label("Default QA path. Phase diagnostics are collapsed below to keep the panel compact.");

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

        private void DrawDiagnosticSmokeControls()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Phase Diagnostics", GUI.skin.box);
            GUILayout.Label("Open only the diagnostic family needed for the current cut.");

            _showTransitionEffectDiagnostics = GUILayout.Toggle(_showTransitionEffectDiagnostics, "Show Gate / Transition / Effect diagnostics");
            if (_showTransitionEffectDiagnostics)
            {
                DrawTransitionEffectDiagnosticSmokeControls();
            }

            _showPauseDiagnostics = GUILayout.Toggle(_showPauseDiagnostics, "Show Pause diagnostics");
            if (_showPauseDiagnostics)
            {
                DrawPauseDiagnosticSmokeControls();
            }

            _showUnityInputDiagnostics = GUILayout.Toggle(_showUnityInputDiagnostics, "Show Unity Input diagnostics");
            if (_showUnityInputDiagnostics)
            {
                DrawUnityInputDiagnosticSmokeControls();
            }

            _showSnapshotDiagnostics = GUILayout.Toggle(_showSnapshotDiagnostics, "Show Save / Snapshot diagnostics");
            if (_showSnapshotDiagnostics)
            {
                DrawSnapshotDiagnosticSmokeControls();
            }

            _showLoadingDiagnostics = GUILayout.Toggle(_showLoadingDiagnostics, "Show Loading diagnostics");
            if (_showLoadingDiagnostics)
            {
                DrawLoadingDiagnosticSmokeControls();
            }

            _showRouteContentDiagnostics = GUILayout.Toggle(_showRouteContentDiagnostics, "Show Route / Content diagnostics");
            if (_showRouteContentDiagnostics)
            {
                DrawRouteContentSmokeControls();
            }

            _showFoundationDiagnostics = GUILayout.Toggle(_showFoundationDiagnostics, "Show Foundation diagnostics");
            if (_showFoundationDiagnostics)
            {
                DrawFoundationDiagnosticSmokeControls();
            }

            _showResetObjectDiagnostics = GUILayout.Toggle(_showResetObjectDiagnostics, "Show Reset / Object diagnostics");
            if (_showResetObjectDiagnostics)
            {
                DrawResetObjectDiagnosticSmokeControls();
            }
        }

        private void DrawTransitionEffectDiagnosticSmokeControls()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Gate / Transition / Effect Diagnostics", GUI.skin.box);

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Gate Admission Diagnostics Smoke"))
                {
                    RunGateAdmissionDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Transition Diagnostics Smoke"))
                {
                    RunTransitionDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Transition Gate Blocker Smoke"))
                {
                    RunTransitionGateBlockerSmoke();
                }

                if (GUILayout.Button("Run Transition Orchestration Observation Smoke"))
                {
                    RunTransitionOrchestrationObservationSmoke();
                }

                if (GUILayout.Button("Run Transition Effect Diagnostics Smoke"))
                {
                    RunTransitionEffectDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Unity Fade Curtain Effect Adapter Smoke"))
                {
                    RunUnityFadeCurtainEffectAdapterSmoke();
                }

                if (GUILayout.Button("Run Transition Effect Policy Guardrails Smoke"))
                {
                    RunTransitionEffectPolicyGuardrailsSmoke();
                }
            }
        }

        private void DrawPauseDiagnosticSmokeControls()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Pause Diagnostics", GUI.skin.box);
            GUILayout.Label("F20 diagnostics plus F23 intent-only boundaries. No input adapter, overlay adapter, UI, Time.timeScale changes or Unity Build materialization.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Pause Diagnostics Smoke"))
                {
                    RunPauseDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Pause Gate Blocker Smoke"))
                {
                    RunPauseGateBlockerSmoke();
                }

                if (GUILayout.Button("Run Pause Runtime Request Smoke"))
                {
                    RunPauseRuntimeRequestSmoke();
                }

                if (GUILayout.Button("Run Pause Boundary Intent Smoke"))
                {
                    RunPauseBoundaryIntentSmoke();
                }
            }
        }

        private void DrawUnityInputDiagnosticSmokeControls()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Unity Input Diagnostics", GUI.skin.box);
            GUILayout.Label("F29 target ownership proof, F30 passive InputMode/Pause request mapping, F31 PlayerActor/Session references and F32 PlayerInput application plus F33 opt-in Pause runtime bridge. Unity PlayerInput/PlayerInputManager remain the official input components; no custom input manager, PlayerInputManager join, movement or actor spawning.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Unity Input Target Ownership Smoke"))
                {
                    RunUnityInputTargetOwnershipSmoke();
                }

                if (GUILayout.Button("Run InputMode Contract Smoke"))
                {
                    RunInputModeContractSmoke();
                }

                if (GUILayout.Button("Run Unity Input Official Component Evidence Smoke"))
                {
                    RunUnityInputOfficialComponentEvidenceSmoke();
                }

                if (GUILayout.Button("Run Session PlayerInputManager Boundary Smoke"))
                {
                    RunSessionPlayerInputManagerBoundarySmoke();
                }

                if (GUILayout.Button("Run Pause InputMode Request Boundary Smoke"))
                {
                    RunPauseInputModeRequestBoundarySmoke();
                }

                if (GUILayout.Button("Run PlayerActor Identity Smoke"))
                {
                    RunPlayerActorIdentitySmoke();
                }

                if (GUILayout.Button("Run InputMode Unity Application Preview Smoke"))
                {
                    RunInputModeUnityApplicationPreviewSmoke();
                }

                if (GUILayout.Button("Run InputMode Unity Action Map Preview Smoke"))
                {
                    RunInputModeUnityActionMapPreviewSmoke();
                }

                if (GUILayout.Button("Run InputMode Unity Application Plan Smoke"))
                {
                    RunInputModeUnityApplicationPlanSmoke();
                }

                if (GUILayout.Button("Run InputMode Unity PlayerInput Adapter Smoke"))
                {
                    RunInputModeUnityPlayerInputAdapterSmoke();
                }

                if (GUILayout.Button("Run InputMode Unity PlayerInput Application Smoke"))
                {
                    RunInputModeUnityPlayerInputApplicationSmoke();
                }

                if (GUILayout.Button("Run InputMode Unity PlayerInput Request Application Smoke"))
                {
                    RunInputModeUnityPlayerInputRequestApplicationSmoke();
                }


                if (GUILayout.Button("Run Pause InputMode Unity PlayerInput Application Smoke"))
                {
                    RunPauseInputModeUnityPlayerInputApplicationSmoke();
                }

                if (GUILayout.Button("Run Pause Runtime PlayerInput Bridge Smoke"))
                {
                    RunPauseInputModeUnityPlayerInputRuntimeBridgeSmoke();
                }

                if (GUILayout.Button("Run Pause InputAction Runtime Bridge Trigger Smoke"))
                {
                    RunPauseInputActionRuntimeBridgeTriggerSmoke();
                }
            }
        }

        private void DrawSnapshotDiagnosticSmokeControls()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Save / Snapshot Diagnostics", GUI.skin.box);
            GUILayout.Label("F21 diagnostics. Snapshot has no backend. Preferences may use PlayerPrefs only through the Preferences adapter. Progression Save uses an explicit request runtime over IProgressionSaveStore; no UI or autosave scheduler.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Snapshot Participant Diagnostics Smoke"))
                {
                    RunSnapshotParticipantDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Preferences Store Diagnostics Smoke"))
                {
                    RunPreferencesStoreDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Progression Save JSON Backend Smoke"))
                {
                    RunProgressionSaveJsonBackendSmoke();
                }

                if (GUILayout.Button("Run Progression Save Runtime Request Smoke"))
                {
                    RunProgressionSaveRuntimeRequestSmoke();
                }
            }
        }

        private void DrawLoadingDiagnosticSmokeControls()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Loading Diagnostics", GUI.skin.box);
            GUILayout.Label("F22 diagnostics. Aggregates passive LoadingStep progress, observes SceneLifecycle/Transition results, validates readiness observations, validates result/issue summaries and validates loading screen adapter contracts. No SceneLifecycle execution, Transition execution, UI prefab, fade, curtain or readiness mutation.");

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Loading Progress Aggregation Smoke"))
                {
                    RunLoadingProgressAggregationSmoke();
                }

                if (GUILayout.Button("Run Loading Observation Adapter Smoke"))
                {
                    RunLoadingObservationAdapterSmoke();
                }

                if (GUILayout.Button("Run Loading Readiness Observation Smoke"))
                {
                    RunLoadingReadinessObservationSmoke();
                }

                if (GUILayout.Button("Run Loading Result and Issue Smoke"))
                {
                    RunLoadingResultAndIssueSmoke();
                }

                if (GUILayout.Button("Run Loading Screen Adapter Boundary Smoke"))
                {
                    RunLoadingScreenAdapterBoundarySmoke();
                }
            }
        }

        private void DrawFoundationDiagnosticSmokeControls()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Foundation Diagnostics", GUI.skin.box);

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Local Contribution Smoke"))
                {
                    RunLocalContributionSmoke();
                }

                if (GUILayout.Button("Run Runtime Content Smoke"))
                {
                    RunRuntimeContentSmoke();
                }

                if (GUILayout.Button("Run Runtime Prefab Materialization Smoke"))
                {
                    RunRuntimePrefabMaterializationSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Physical Placement Smoke"))
                {
                    RunContentAnchorPhysicalPlacementSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Materialization Pipeline Smoke"))
                {
                    RunContentAnchorMaterializationPipelineSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Materialization Scope Release Smoke"))
                {
                    RunContentAnchorMaterializationScopeReleaseSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Materialization Bridge Smoke"))
                {
                    RunContentAnchorMaterializationBridgeSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Materialization Bridge Set Smoke"))
                {
                    RunContentAnchorMaterializationBridgeSetSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Materialization Bridge Set Preflight Smoke"))
                {
                    RunContentAnchorMaterializationBridgeSetPreflightSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Materialization Authoring Validation Smoke"))
                {
                    RunContentAnchorMaterializationAuthoringValidationSmoke();
                }
            }
        }

        private void DrawResetObjectDiagnosticSmokeControls()
        {
            GUILayout.Space(4f);
            GUILayout.Label("Reset / Object Diagnostics", GUI.skin.box);

            using (new EditorDisabledScope(_requestInFlight))
            {
                if (GUILayout.Button("Run Cycle Reset Runtime Host Smoke"))
                {
                    RunCycleResetRuntimeHostSmoke();
                }

                if (GUILayout.Button("Run Cycle Reset Trigger Smoke"))
                {
                    RunCycleResetTriggerSmoke();
                }

                if (GUILayout.Button("Run Cycle Reset Bridge Smoke"))
                {
                    RunCycleResetBridgeSmoke();
                }

                if (GUILayout.Button("Run Object Entry Foundation Closure Smoke"))
                {
                    RunObjectEntryFoundationClosureSmoke();
                }

                if (GUILayout.Button("Run Object Reset Foundation Closure Smoke"))
                {
                    RunObjectResetFoundationClosureSmoke();
                }

                if (GUILayout.Button("Run Object Reset Unity Adapters Closure Smoke"))
                {
                    RunObjectResetUnityAdaptersClosureSmoke();
                }

                if (GUILayout.Button("Run Object Reset GameObject Active Closure Smoke"))
                {
                    RunObjectResetGameObjectActiveClosureSmoke();
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

                if (GUILayout.Button("Run Activity Content Anchor Diagnostics Smoke"))
                {
                    RunActivityContentAnchorDiagnosticsSmoke();
                }

                if (GUILayout.Button("Run Activity Content Anchor Positive Smoke"))
                {
                    RunActivityContentAnchorPositiveSmoke();
                }

                if (GUILayout.Button("Run Activity Content Anchor Binding Smoke"))
                {
                    RunActivityContentAnchorBindingSmoke();
                }

                if (GUILayout.Button("Run Activity Content Execution Runtime Smoke"))
                {
                    RunActivityContentExecutionRuntimeSmoke();
                }

                if (GUILayout.Button("Run Activity Content Execution Lifecycle Transition Smoke"))
                {
                    RunActivityContentExecutionLifecycleTransitionSmoke();
                }

                if (GUILayout.Button("Run Activity Content Execution Participant Source Smoke"))
                {
                    RunActivityContentExecutionParticipantSourceSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Binding Smoke"))
                {
                    RunContentAnchorBindingSmoke();
                }

                if (GUILayout.Button("Run Content Anchor Binding Cleanup Smoke"))
                {
                    RunContentAnchorBindingCleanupSmoke();
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

        private async void RunGateAdmissionDiagnosticsSmoke()
        {
            await RunSmokeAsync(GateAdmissionQaSmokeRunner.SmokeName, runtimeHost =>
                GateAdmissionQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionDiagnosticsSmoke()
        {
            await RunSmokeAsync(TransitionQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionGateBlockerSmoke()
        {
            await RunSmokeAsync(TransitionGateBlockerQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionGateBlockerQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionOrchestrationObservationSmoke()
        {
            await RunSmokeAsync(TransitionOrchestrationObservationQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionOrchestrationObservationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionEffectDiagnosticsSmoke()
        {
            await RunSmokeAsync(TransitionEffectQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionEffectQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunUnityFadeCurtainEffectAdapterSmoke()
        {
            await RunSmokeAsync(TransitionEffectUnityFadeCurtainQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionEffectUnityFadeCurtainQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunTransitionEffectPolicyGuardrailsSmoke()
        {
            await RunSmokeAsync(TransitionEffectPolicyQaSmokeRunner.SmokeName, runtimeHost =>
                TransitionEffectPolicyQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseDiagnosticsSmoke()
        {
            await RunSmokeAsync(PauseQaSmokeRunner.SmokeName, runtimeHost =>
                PauseQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseGateBlockerSmoke()
        {
            await RunSmokeAsync(PauseGateBlockerQaSmokeRunner.SmokeName, runtimeHost =>
                PauseGateBlockerQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseRuntimeRequestSmoke()
        {
            await RunSmokeAsync(PauseRuntimeRequestQaSmokeRunner.SmokeName, runtimeHost =>
                PauseRuntimeRequestQaSmokeRunner.RunRuntimeRequestSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunPauseBoundaryIntentSmoke()
        {
            await RunSmokeAsync(PauseBoundaryIntentQaSmokeRunner.SmokeName, runtimeHost =>
                PauseBoundaryIntentQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunUnityInputTargetOwnershipSmoke()
        {
            await RunSmokeAsync(UnityInputTargetOwnershipQaSmokeRunner.SmokeName, runtimeHost =>
                UnityInputTargetOwnershipQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeContractSmoke()
        {
            await RunSmokeAsync(InputModeContractQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeContractQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }


        private async void RunUnityInputOfficialComponentEvidenceSmoke()
        {
            await RunSmokeAsync(UnityInputOfficialComponentEvidenceQaSmokeRunner.SmokeName, runtimeHost =>
                UnityInputOfficialComponentEvidenceQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseInputModeRequestBoundarySmoke()
        {
            await RunSmokeAsync(PauseInputModeRequestBoundaryQaSmokeRunner.SmokeName, runtimeHost =>
                PauseInputModeRequestBoundaryQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunSessionPlayerInputManagerBoundarySmoke()
        {
            await RunSmokeAsync(SessionPlayerInputManagerBoundaryQaSmokeRunner.SmokeName, runtimeHost =>
                SessionPlayerInputManagerBoundaryQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPlayerActorIdentitySmoke()
        {
            await RunSmokeAsync(PlayerActorIdentityQaSmokeRunner.SmokeName, runtimeHost =>
                PlayerActorIdentityQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityApplicationPreviewSmoke()
        {
            await RunSmokeAsync(InputModeUnityApplicationPreviewQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityApplicationPreviewQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityActionMapPreviewSmoke()
        {
            await RunSmokeAsync(InputModeUnityActionMapPreviewQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityActionMapPreviewQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityApplicationPlanSmoke()
        {
            await RunSmokeAsync(InputModeUnityApplicationPlanQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityApplicationPlanQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityPlayerInputAdapterSmoke()
        {
            await RunSmokeAsync(InputModeUnityPlayerInputAdapterQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityPlayerInputAdapterQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityPlayerInputApplicationSmoke()
        {
            await RunSmokeAsync(InputModeUnityPlayerInputApplicationQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityPlayerInputApplicationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunInputModeUnityPlayerInputRequestApplicationSmoke()
        {
            await RunSmokeAsync(InputModeUnityPlayerInputRequestApplicationQaSmokeRunner.SmokeName, runtimeHost =>
                InputModeUnityPlayerInputRequestApplicationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }


        private async void RunPauseInputModeUnityPlayerInputApplicationSmoke()
        {
            await RunSmokeAsync(PauseInputModeUnityPlayerInputApplicationQaSmokeRunner.SmokeName, runtimeHost =>
                PauseInputModeUnityPlayerInputApplicationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPauseInputModeUnityPlayerInputRuntimeBridgeSmoke()
        {
            await RunSmokeAsync(PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner.SmokeName, runtimeHost =>
                PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner.RunRuntimeBridgeSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunPauseInputActionRuntimeBridgeTriggerSmoke()
        {
            await RunSmokeAsync(PauseInputActionRuntimeBridgeTriggerQaSmokeRunner.SmokeName, runtimeHost =>
                PauseInputActionRuntimeBridgeTriggerQaSmokeRunner.RunTriggerSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunSnapshotParticipantDiagnosticsSmoke()
        {
            await RunSmokeAsync(SnapshotParticipantQaSmokeRunner.SmokeName, runtimeHost =>
                SnapshotParticipantQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunPreferencesStoreDiagnosticsSmoke()
        {
            await RunSmokeAsync(PreferencesQaSmokeRunner.SmokeName, runtimeHost =>
                PreferencesQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunProgressionSaveJsonBackendSmoke()
        {
            await RunSmokeAsync(ProgressionSaveQaSmokeRunner.SmokeName, runtimeHost =>
                ProgressionSaveQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunProgressionSaveRuntimeRequestSmoke()
        {
            await RunSmokeAsync(ProgressionSaveRuntimeQaSmokeRunner.SmokeName, runtimeHost =>
                ProgressionSaveRuntimeQaSmokeRunner.RunRuntimeRequestSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingProgressAggregationSmoke()
        {
            await RunSmokeAsync(LoadingProgressQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingProgressQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingObservationAdapterSmoke()
        {
            await RunSmokeAsync(LoadingObservationQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingObservationQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingReadinessObservationSmoke()
        {
            await RunSmokeAsync(LoadingReadinessQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingReadinessQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingResultAndIssueSmoke()
        {
            await RunSmokeAsync(LoadingResultQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingResultQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunLoadingScreenAdapterBoundarySmoke()
        {
            await RunSmokeAsync(LoadingScreenAdapterQaSmokeRunner.SmokeName, runtimeHost =>
                LoadingScreenAdapterQaSmokeRunner.RunDiagnosticsSmokeAsync(_logger, QaSource));
        }

        private async void RunCycleResetRuntimeHostSmoke()
        {
            await RunSmokeAsync(CycleResetQaSmokeRunner.SmokeName, runtimeHost =>
                CycleResetQaSmokeRunner.RunRuntimeHostSmokeAsync(
                    runtimeHost,
                    _logger,
                    QaSource,
                    runRouteCycleReset: true,
                    runActivityCycleReset: true,
                    logParticipantDetails: false,
                    emitSmokeEnvelope: false));
        }

        private async void RunCycleResetTriggerSmoke()
        {
            await RunSmokeAsync(CycleResetQaSmokeRunner.TriggerSmokeName, runtimeHost =>
                CycleResetQaSmokeRunner.RunTriggerSmokeAsync(
                    runtimeHost,
                    _logger,
                    QaSource,
                    runRouteCycleReset: true,
                    runActivityCycleReset: true,
                    emitSmokeEnvelope: false));
        }

        private async void RunCycleResetBridgeSmoke()
        {
            await RunSmokeAsync(CycleResetQaSmokeRunner.BridgeSmokeName, runtimeHost =>
                CycleResetQaSmokeRunner.RunBridgeSmokeAsync(
                    runtimeHost,
                    _logger,
                    QaSource,
                    runRouteCycleReset: true,
                    runActivityCycleReset: true,
                    emitSmokeEnvelope: false));
        }

        private async void RunObjectEntryFoundationClosureSmoke()
        {
            await RunSmokeAsync(ObjectEntryQaSmokeRunner.FoundationClosureSmokeName, runtimeHost =>
                ObjectEntryQaSmokeRunner.RunFoundationClosureSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunObjectResetFoundationClosureSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.FoundationClosureSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunFoundationClosureSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunObjectResetUnityAdaptersClosureSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.UnityAdaptersClosureSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunUnityAdaptersClosureSmokeAsync(runtimeHost, _logger, QaSource));
        }

        private async void RunObjectResetGameObjectActiveClosureSmoke()
        {
            await RunSmokeAsync(ObjectResetQaSmokeRunner.GameObjectActiveClosureSmokeName, runtimeHost =>
                ObjectResetQaSmokeRunner.RunGameObjectActiveClosureSmokeAsync(runtimeHost, _logger, QaSource));
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

            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            RouteContentBinding[] routeBindings = FindObjectsByType<RouteContentBinding>(
                FindObjectsInactive.Include);
            ActivityLocalVisibilityAdapter[] activityAdapters = FindObjectsByType<ActivityLocalVisibilityAdapter>(
                FindObjectsInactive.Include);

            int routeBindingCount = routeBindings?.Length ?? 0;
            int activityAdapterCount = activityAdapters?.Length ?? 0;
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


        private async void RunRuntimePrefabMaterializationSmoke()
        {
            await RunSmokeAsync("Runtime Prefab Materialization Smoke", runtimeHost =>
                Task.FromResult(RunRuntimePrefabMaterializationSmokeCore(runtimeHost)));
        }

        private bool RunRuntimePrefabMaterializationSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Runtime Prefab Materialization Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject template = null;
            var registry = new UnityRuntimeMaterializedObjectRegistry();
            RuntimeScopeContext context = default(RuntimeScopeContext);
            RuntimeMaterializationRequest request = default(RuntimeMaterializationRequest);
            bool rootCreated = false;

            string ownerId = "qa.runtime-prefab-materialization.smoke." + Guid.NewGuid().ToString("N");
            var owner = RuntimeContentOwner.Transient(ownerId, "QA Runtime Prefab Materialization Smoke");

            try
            {
                var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.runtime-prefab-materialization.root.create");
                rootCreated = rootCreateResult.Applied || rootCreateResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                if (!rootCreated)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(
                        owner,
                        QaSource,
                        "qa.runtime-prefab-materialization.context",
                        out context))
                {
                    _logger.Warning("QA Runtime Prefab Materialization Smoke step failed. step='context' reason='Runtime scope context was not created'.");
                    return false;
                }

                var resource = RuntimeMaterializationResource.From(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.runtime-prefab-materialization.prefab",
                    "QA Runtime Prefab Materialization Template",
                    string.Empty);

                if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                        context,
                        "qa.runtime-prefab-materialization.handle",
                        resource,
                        QaSource,
                        "qa.runtime-prefab-materialization.request",
                        out request,
                        out var requestGuardResult))
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='materialization-request' guard='{requestGuardResult.Status}' message='{FormatValue(requestGuardResult.Message)}'.");
                    return false;
                }

                var missingPrefabAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    null,
                    registry,
                    null,
                    QaSource);
                var missingPrefabResult = missingPrefabAdapter.Materialize(request);
                bool missingPrefabBlocked = missingPrefabResult.Failed
                    && missingPrefabResult.Status == RuntimeMaterializationStatus.FailedMaterializer
                    && registry.Count == 0;
                if (!missingPrefabBlocked)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='missing-prefab' status='{missingPrefabResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(missingPrefabResult.Message)}'.");
                    return false;
                }

                template = new GameObject("QA Runtime Prefab Materialization Template");
                template.hideFlags = HideFlags.DontSave;

                var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    template,
                    registry,
                    null,
                    QaSource);
                var materializationResult = materializationAdapter.Materialize(request);
                if (!materializationResult.Succeeded || !materializationResult.HasHandle || registry.ActiveCount != 1)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='materialize' status='{materializationResult.Status}' hasHandle='{materializationResult.HasHandle}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(materializationResult.Message)}'.");
                    return false;
                }

                if (!registry.TryGet(request.Identity, out var evidence) || evidence == null || !evidence.HasLiveInstance)
                {
                    _logger.Warning("QA Runtime Prefab Materialization Smoke step failed. step='physical-evidence' reason='Unity physical evidence was not registered'.");
                    return false;
                }

                var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                    materializationResult,
                    QaSource,
                    "qa.runtime-prefab-materialization.apply");
                if (!appliedMaterializationResult.Succeeded)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='apply-materialization' status='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryGetHandle(context, request.Identity, out var registeredHandle)
                    || registeredHandle == null
                    || !registeredHandle.IsMaterialized)
                {
                    _logger.Warning("QA Runtime Prefab Materialization Smoke step failed. step='registered-handle' reason='Materialized handle was not registered in the logical runtime scope root'.");
                    return false;
                }

                var duplicateMaterializationResult = materializationAdapter.Materialize(request);
                bool duplicateBlocked = duplicateMaterializationResult.Failed
                    && registry.Count == 1
                    && registry.ActiveCount == 1;
                if (!duplicateBlocked)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='duplicate-materialization' status='{duplicateMaterializationResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(duplicateMaterializationResult.Message)}'.");
                    return false;
                }

                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(registry, QaSource);
                var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                    context,
                    request.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.runtime-prefab-materialization.release.request");
                var physicalReleaseResult = releaseAdapter.Release(releaseRequest);
                if (!physicalReleaseResult.Succeeded || registry.PhysicalReleaseRequestedCount != 1)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='physical-release' status='{physicalReleaseResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(physicalReleaseResult.Message)}'.");
                    return false;
                }

                var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(
                    physicalReleaseResult,
                    QaSource,
                    "qa.runtime-prefab-materialization.release.logical");
                if (!logicalReleaseResult.Succeeded || !logicalReleaseResult.HandleUnregistered)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='logical-release' status='{logicalReleaseResult.Status}' unregistered='{logicalReleaseResult.HandleUnregistered}' message='{FormatValue(logicalReleaseResult.Message)}'.");
                    return false;
                }

                var doubleReleaseResult = releaseAdapter.Release(releaseRequest);
                bool doubleReleaseBlocked = doubleReleaseResult.Failed
                    && doubleReleaseResult.Status == RuntimeReleaseStatus.FailedReleaseAdapter;
                if (!doubleReleaseBlocked)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='double-release' status='{doubleReleaseResult.Status}' message='{FormatValue(doubleReleaseResult.Message)}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.runtime-prefab-materialization.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Runtime Prefab Materialization Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Runtime Prefab Materialization Smoke step completed. "
                    + $"step='unity-prefab-materialization' passed='True' missingPrefabBlocked='{missingPrefabBlocked}' materialization='{appliedMaterializationResult.Status}' duplicateBlocked='{duplicateBlocked}' physicalRelease='{physicalReleaseResult.Status}' logicalRelease='{logicalReleaseResult.Status}' doubleReleaseBlocked='{doubleReleaseBlocked}' registry='{registry.ToDiagnosticString()}' contentAnchorPlacement='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                foreach (var entry in registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (rootCreated)
                {
                    if (context.IsValid)
                    {
                        runtimeContentRuntime.ReleaseScopeLogically(
                            context,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.runtime-prefab-materialization.cleanup.scope-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.runtime-prefab-materialization.cleanup.root-remove");
                }
            }
        }


        private async void RunContentAnchorPhysicalPlacementSmoke()
        {
            await RunSmokeAsync("Content Anchor Physical Placement Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorPhysicalPlacementSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorPhysicalPlacementSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Physical Placement Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var registry = new UnityRuntimeMaterializedObjectRegistry();
            RuntimeScopeContext context = default;
            RuntimeContentOwner owner = default;
            RuntimeMaterializationRequest materializationRequest = default;
            ContentAnchorBindingRequest bindingRequest = default;
            GameObject template = null;
            GameObject anchorObject = null;
            bool rootCreated = false;

            try
            {
                owner = RuntimeContentOwner.Transient(
                    "qa.content-anchor-placement.owner",
                    "QA Content Anchor Physical Placement Smoke");
                var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-placement.root.create");
                rootCreated = rootCreateResult.Applied || rootCreateResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                if (!rootCreated)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(
                        owner,
                        QaSource,
                        "qa.content-anchor-placement.context",
                        out context))
                {
                    _logger.Warning("QA Content Anchor Physical Placement Smoke step failed. step='context' reason='Runtime scope context was not created'.");
                    return false;
                }

                var anchorOwner = ContentAnchorDeclaration.CreateOwnerKey(
                    ContentAnchorScope.Local,
                    "qa.content-anchor-placement.anchor-owner");
                var anchor = ContentAnchorDeclaration.Slot(
                    anchorOwner,
                    ContentAnchorScope.Local,
                    "qa.content-anchor-placement.slot",
                    ContentAnchorRequiredness.Optional,
                    "QA Content Anchor Physical Placement Slot",
                    "QA smoke anchor for explicit physical placement adapter proof.",
                    "QA Content Anchor Physical Placement Anchor",
                    "qa.content-anchor-placement.anchor");
                var anchorSet = ContentAnchorSet.FromDeclarations(new[] { anchor });
                if (!anchorSet.HasAnchors || anchorSet.HasIssues)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='anchor-set' diagnostics='{anchorSet.ToDiagnosticString()}'.");
                    return false;
                }

                var resource = RuntimeMaterializationResource.From(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.content-anchor-placement.prefab",
                    "QA Content Anchor Physical Placement Template",
                    string.Empty);

                if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                        context,
                        "qa.content-anchor-placement.content",
                        resource,
                        QaSource,
                        "qa.content-anchor-placement.materialization.request",
                        out materializationRequest,
                        out var requestGuardResult))
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='materialization-request' guard='{requestGuardResult.Status}' message='{FormatValue(requestGuardResult.Message)}'.");
                    return false;
                }

                template = new GameObject("QA Content Anchor Physical Placement Template");
                template.hideFlags = HideFlags.DontSave;
                anchorObject = new GameObject("QA Content Anchor Physical Placement Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    template,
                    registry,
                    null,
                    QaSource);
                var materializationResult = materializationAdapter.Materialize(materializationRequest);
                if (!materializationResult.Succeeded || !materializationResult.HasHandle || registry.ActiveCount != 1)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='materialize' status='{materializationResult.Status}' hasHandle='{materializationResult.HasHandle}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(materializationResult.Message)}'.");
                    return false;
                }

                var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                    materializationResult,
                    QaSource,
                    "qa.content-anchor-placement.materialization.apply");
                if (!appliedMaterializationResult.Succeeded)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='apply-materialization' status='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                    return false;
                }

                if (!registry.TryGet(materializationRequest.Identity, out var evidence)
                    || evidence == null
                    || !evidence.HasLiveInstance)
                {
                    _logger.Warning("QA Content Anchor Physical Placement Smoke step failed. step='physical-evidence' reason='Unity physical evidence was not registered'.");
                    return false;
                }

                bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                    context,
                    anchor,
                    materializationRequest.ContentId,
                    resource,
                    QaSource,
                    "qa.content-anchor-placement.binding.request");
                var bindingResult = runtimeHost.BindContentAnchor(
                    anchorSet,
                    bindingRequest,
                    QaSource,
                    "qa.content-anchor-placement.binding.logical");
                if (!bindingResult.Succeeded || !bindingResult.HasHandle)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='logical-binding' status='{bindingResult.Status}' message='{FormatValue(bindingResult.Message)}'.");
                    return false;
                }

                var placementAdapter = new UnityContentAnchorPlacementAdapter(QaSource);
                var missingAnchorTransformResult = placementAdapter.Place(
                    bindingResult,
                    evidence,
                    null,
                    true,
                    "qa.content-anchor-placement.missing-anchor-transform");
                bool missingAnchorTransformBlocked = missingAnchorTransformResult.Failed
                    && missingAnchorTransformResult.Status == UnityContentAnchorPlacementStatus.FailedMissingAnchorTransform
                    && evidence.Instance.transform.parent == null;
                if (!missingAnchorTransformBlocked)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='missing-anchor-transform' status='{missingAnchorTransformResult.Status}' parent='{FormatValue(evidence.Instance.transform.parent != null ? evidence.Instance.transform.parent.name : string.Empty)}' message='{FormatValue(missingAnchorTransformResult.Message)}'.");
                    return false;
                }

                var placementResult = placementAdapter.Place(
                    bindingResult,
                    evidence,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-placement.place");
                bool placed = placementResult.Succeeded
                    && placementResult.Status == UnityContentAnchorPlacementStatus.Succeeded
                    && placementResult.ParentApplied
                    && evidence.Instance.transform.parent == anchorObject.transform
                    && evidence.Instance.transform.localPosition == Vector3.zero
                    && evidence.Instance.transform.localRotation == Quaternion.identity
                    && evidence.Instance.transform.localScale == Vector3.one;
                if (!placed)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='place' status='{placementResult.Status}' diagnostics='{placementResult.ToDiagnosticString()}'.");
                    return false;
                }

                var alreadyPlacedResult = placementAdapter.Place(
                    bindingResult,
                    evidence,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-placement.already-placed");
                bool alreadyPlaced = alreadyPlacedResult.Succeeded
                    && alreadyPlacedResult.Status == UnityContentAnchorPlacementStatus.SucceededAlreadyPlaced
                    && !alreadyPlacedResult.ParentApplied;
                if (!alreadyPlaced)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='already-placed' status='{alreadyPlacedResult.Status}' diagnostics='{alreadyPlacedResult.ToDiagnosticString()}'.");
                    return false;
                }

                bool logicalUnbind = runtimeHost.UnbindContentAnchor(bindingResult.Handle);
                if (!logicalUnbind)
                {
                    _logger.Warning("QA Content Anchor Physical Placement Smoke step failed. step='logical-unbind' reason='Logical Content Anchor binding was not removed'.");
                    return false;
                }

                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(registry, QaSource);
                var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-placement.release.request");
                var physicalReleaseResult = releaseAdapter.Release(releaseRequest);
                if (!physicalReleaseResult.Succeeded || registry.PhysicalReleaseRequestedCount != 1)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='physical-release' status='{physicalReleaseResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(physicalReleaseResult.Message)}'.");
                    return false;
                }

                var placementAfterReleaseResult = placementAdapter.Place(
                    bindingResult,
                    evidence,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-placement.place-after-release");
                bool placementAfterReleaseBlocked = placementAfterReleaseResult.Failed
                    && placementAfterReleaseResult.Status == UnityContentAnchorPlacementStatus.RejectedReleasedPhysicalEvidence;
                if (!placementAfterReleaseBlocked)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='place-after-release' status='{placementAfterReleaseResult.Status}' diagnostics='{placementAfterReleaseResult.ToDiagnosticString()}'.");
                    return false;
                }

                var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(
                    physicalReleaseResult,
                    QaSource,
                    "qa.content-anchor-placement.release.logical");
                if (!logicalReleaseResult.Succeeded || !logicalReleaseResult.HandleUnregistered)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='logical-release' status='{logicalReleaseResult.Status}' unregistered='{logicalReleaseResult.HandleUnregistered}' message='{FormatValue(logicalReleaseResult.Message)}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-placement.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Physical Placement Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Content Anchor Physical Placement Smoke step completed. "
                    + $"step='unity-content-anchor-placement' passed='True' missingAnchorTransformBlocked='{missingAnchorTransformBlocked}' logicalBinding='{bindingResult.Status}' placement='{placementResult.Status}' alreadyPlaced='{alreadyPlaced}' logicalUnbind='{logicalUnbind}' physicalRelease='{physicalReleaseResult.Status}' logicalRelease='{logicalReleaseResult.Status}' placementAfterReleaseBlocked='{placementAfterReleaseBlocked}' registry='{registry.ToDiagnosticString()}' materialization='{appliedMaterializationResult.Status}' contentAnchorPhysicalPlacement='True' contentAnchorCreatesObject='False' contentAnchorDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                foreach (var entry in registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (rootCreated)
                {
                    if (context.IsValid)
                    {
                        if (bindingRequest.IsValid)
                        {
                            runtimeHost.UnbindContentAnchor(bindingRequest);
                        }

                        runtimeContentRuntime.ReleaseScopeLogically(
                            context,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.content-anchor-placement.cleanup.scope-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.content-anchor-placement.cleanup.root-remove");
                }
            }
        }

        private async void RunContentAnchorMaterializationPipelineSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Pipeline Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationPipelineSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationPipelineSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Pipeline Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var registry = new UnityRuntimeMaterializedObjectRegistry();
            RuntimeScopeContext context = default;
            RuntimeContentOwner owner = default;
            ContentAnchorBindingRequest bindingRequest = default;
            GameObject template = null;
            GameObject anchorObject = null;
            bool rootCreated = false;

            try
            {
                owner = RuntimeContentOwner.Transient(
                    "qa.content-anchor-pipeline.owner",
                    "QA Content Anchor Materialization Pipeline Smoke");
                var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-pipeline.root.create");
                rootCreated = rootCreateResult.Applied || rootCreateResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                if (!rootCreated)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(
                        owner,
                        QaSource,
                        "qa.content-anchor-pipeline.context",
                        out context))
                {
                    _logger.Warning("QA Content Anchor Materialization Pipeline Smoke step failed. step='context' reason='Runtime scope context was not created'.");
                    return false;
                }

                var anchorOwner = ContentAnchorDeclaration.CreateOwnerKey(
                    ContentAnchorScope.Route,
                    "qa.content-anchor-pipeline.route");
                var anchor = ContentAnchorDeclaration.Slot(
                    anchorOwner,
                    ContentAnchorScope.Route,
                    "qa.content-anchor-pipeline.anchor",
                    ContentAnchorRequiredness.Required,
                    "QA Content Anchor Materialization Pipeline Anchor",
                    "qa.content-anchor-pipeline.anchor",
                    string.Empty,
                    string.Empty);
                var anchorSet = ContentAnchorSet.FromDeclarations(new[] { anchor });
                if (!anchorSet.HasAnchors || anchorSet.Count != 1 || anchorSet.HasIssues)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='anchor-set' diagnostics='{anchorSet.ToDiagnosticString()}'.");
                    return false;
                }

                var resource = new RuntimeMaterializationResource(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.content-anchor-pipeline.prefab",
                    "QA Content Anchor Materialization Pipeline Template",
                    string.Empty);
                bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                    context,
                    anchor,
                    "qa.content-anchor-pipeline.content",
                    resource,
                    QaSource,
                    "qa.content-anchor-pipeline.binding.request");

                template = new GameObject("QA Content Anchor Materialization Pipeline Template");
                template.hideFlags = HideFlags.DontSave;
                anchorObject = new GameObject("QA Content Anchor Materialization Pipeline Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    template,
                    registry,
                    null,
                    QaSource);
                var placementAdapter = new UnityContentAnchorPlacementAdapter(QaSource);
                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(registry, QaSource);
                var pipeline = new UnityContentAnchorMaterializationPipeline(
                    materializationAdapter,
                    placementAdapter,
                    releaseAdapter,
                    QaSource);

                var missingAnchorTransformResult = pipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    bindingRequest,
                    null,
                    true,
                    "qa.content-anchor-pipeline.missing-anchor-transform");
                bool missingAnchorTransformBlocked = missingAnchorTransformResult.Failed
                    && missingAnchorTransformResult.Status == UnityContentAnchorMaterializationPipelineStatus.FailedMissingAnchorTransform
                    && registry.Count == 0
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!missingAnchorTransformBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='missing-anchor-transform' diagnostics='{missingAnchorTransformResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var pipelineResult = pipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    bindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-pipeline.execute");
                bool pipelineSucceeded = pipelineResult.Succeeded
                    && pipelineResult.MaterializationResult.Succeeded
                    && pipelineResult.AppliedMaterializationResult.Succeeded
                    && pipelineResult.BindingResult.Succeeded
                    && pipelineResult.PlacementResult.Succeeded
                    && pipelineResult.PlacementResult.ParentApplied;
                if (!pipelineSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='pipeline' diagnostics='{pipelineResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                if (!registry.TryGet(bindingRequest.RuntimeIdentity, out var evidence)
                    || evidence == null
                    || !evidence.HasLiveInstance
                    || evidence.Instance.transform.parent != anchorObject.transform)
                {
                    _logger.Warning("QA Content Anchor Materialization Pipeline Smoke step failed. step='physical-evidence' reason='Materialized evidence was not parented under the explicit anchor Transform'.");
                    return false;
                }

                var duplicateResult = pipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    bindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-pipeline.duplicate");
                bool duplicateBlocked = duplicateResult.Failed
                    && duplicateResult.Status == UnityContentAnchorMaterializationPipelineStatus.FailedMaterialization
                    && registry.Count == 1
                    && registry.ActiveCount == 1;
                if (!duplicateBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='duplicate' diagnostics='{duplicateResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                bool logicalUnbind = runtimeHost.UnbindContentAnchor(pipelineResult.BindingResult.Handle);
                if (!logicalUnbind)
                {
                    _logger.Warning("QA Content Anchor Materialization Pipeline Smoke step failed. step='logical-unbind' reason='Logical Content Anchor binding was not removed'.");
                    return false;
                }

                var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                    context,
                    bindingRequest.RuntimeIdentity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-pipeline.release.request");
                var physicalReleaseResult = releaseAdapter.Release(releaseRequest);
                if (!physicalReleaseResult.Succeeded || registry.PhysicalReleaseRequestedCount != 1)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='physical-release' status='{physicalReleaseResult.Status}' registry='{registry.ToDiagnosticString()}' message='{FormatValue(physicalReleaseResult.Message)}'.");
                    return false;
                }

                var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(
                    physicalReleaseResult,
                    QaSource,
                    "qa.content-anchor-pipeline.release.logical");
                if (!logicalReleaseResult.Succeeded || !logicalReleaseResult.HandleUnregistered)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='logical-release' status='{logicalReleaseResult.Status}' unregistered='{logicalReleaseResult.HandleUnregistered}' message='{FormatValue(logicalReleaseResult.Message)}'.");
                    return false;
                }

                var afterReleaseResult = pipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    bindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-pipeline.after-release");
                bool afterReleaseBlocked = afterReleaseResult.Failed
                    && afterReleaseResult.Status == UnityContentAnchorMaterializationPipelineStatus.FailedMaterialization
                    && registry.Count == 1
                    && registry.ActiveCount == 0;
                if (!afterReleaseBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='after-release' diagnostics='{afterReleaseResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-pipeline.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Pipeline Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Pipeline Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-pipeline' passed='True' missingAnchorTransformBlocked='{missingAnchorTransformBlocked}' pipeline='{pipelineResult.Status}' materialization='{pipelineResult.AppliedMaterializationResult.Status}' logicalBinding='{pipelineResult.BindingResult.Status}' placement='{pipelineResult.PlacementResult.Status}' duplicateBlocked='{duplicateBlocked}' logicalUnbind='{logicalUnbind}' physicalRelease='{physicalReleaseResult.Status}' logicalRelease='{logicalReleaseResult.Status}' afterReleaseBlocked='{afterReleaseBlocked}' registry='{registry.ToDiagnosticString()}' pipelineMaterializesObject='True' contentAnchorCreatesObject='False' contentAnchorDestroysObject='False' contentAnchorPhysicalPlacement='True' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                foreach (var entry in registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (rootCreated)
                {
                    if (context.IsValid)
                    {
                        if (bindingRequest.IsValid)
                        {
                            runtimeHost.UnbindContentAnchor(bindingRequest);
                        }

                        runtimeContentRuntime.ReleaseScopeLogically(
                            context,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.content-anchor-pipeline.cleanup.scope-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.content-anchor-pipeline.cleanup.root-remove");
                }
            }
        }


        private async void RunContentAnchorMaterializationScopeReleaseSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Scope Release Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationScopeReleaseSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationScopeReleaseSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Scope Release Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            var registry = new UnityRuntimeMaterializedObjectRegistry();
            RuntimeScopeContext context = default;
            RuntimeContentOwner owner = default;
            ContentAnchorBindingRequest firstBindingRequest = default;
            ContentAnchorBindingRequest secondBindingRequest = default;
            GameObject template = null;
            GameObject anchorObject = null;
            bool rootCreated = false;

            try
            {
                owner = RuntimeContentOwner.Transient(
                    "qa.content-anchor-scope-release.owner",
                    "QA Content Anchor Materialization Scope Release Smoke");
                var rootCreateResult = runtimeContentRuntime.CreateScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-scope-release.root.create");
                rootCreated = rootCreateResult.Applied || rootCreateResult.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists;
                if (!rootCreated)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='root-create' status='{rootCreateResult.Status}' message='{FormatValue(rootCreateResult.Message)}'.");
                    return false;
                }

                if (!runtimeContentRuntime.TryCreateScopeContext(
                        owner,
                        QaSource,
                        "qa.content-anchor-scope-release.context",
                        out context))
                {
                    _logger.Warning("QA Content Anchor Materialization Scope Release Smoke step failed. step='context' reason='Runtime scope context was not created'.");
                    return false;
                }

                var anchorOwner = ContentAnchorDeclaration.CreateOwnerKey(
                    ContentAnchorScope.Route,
                    "qa.content-anchor-scope-release.route");
                var anchor = ContentAnchorDeclaration.Slot(
                    anchorOwner,
                    ContentAnchorScope.Route,
                    "qa.content-anchor-scope-release.anchor",
                    ContentAnchorRequiredness.Required,
                    "QA Content Anchor Materialization Scope Release Anchor",
                    "qa.content-anchor-scope-release.anchor",
                    string.Empty,
                    string.Empty);
                var anchorSet = ContentAnchorSet.FromDeclarations(new[] { anchor });
                if (!anchorSet.HasAnchors || anchorSet.Count != 1 || anchorSet.HasIssues)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='anchor-set' diagnostics='{anchorSet.ToDiagnosticString()}'.");
                    return false;
                }

                var firstResource = new RuntimeMaterializationResource(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.content-anchor-scope-release.prefab.1",
                    "QA Content Anchor Scope Release Template 1",
                    string.Empty);
                firstBindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                    context,
                    anchor,
                    "qa.content-anchor-scope-release.content.1",
                    firstResource,
                    QaSource,
                    "qa.content-anchor-scope-release.binding.request.1");

                var secondResource = new RuntimeMaterializationResource(
                    UnityPrefabRuntimeMaterializationAdapter.ResourceType,
                    "qa.content-anchor-scope-release.prefab.2",
                    "QA Content Anchor Scope Release Template 2",
                    string.Empty);
                secondBindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                    context,
                    anchor,
                    "qa.content-anchor-scope-release.content.2",
                    secondResource,
                    QaSource,
                    "qa.content-anchor-scope-release.binding.request.2");

                template = new GameObject("QA Content Anchor Scope Release Template");
                template.hideFlags = HideFlags.DontSave;
                anchorObject = new GameObject("QA Content Anchor Scope Release Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                var materializationAdapter = new UnityPrefabRuntimeMaterializationAdapter(
                    template,
                    registry,
                    null,
                    QaSource);
                var placementAdapter = new UnityContentAnchorPlacementAdapter(QaSource);
                var releaseAdapter = new UnityObjectRuntimeReleaseAdapter(registry, QaSource);
                var materializationPipeline = new UnityContentAnchorMaterializationPipeline(
                    materializationAdapter,
                    placementAdapter,
                    releaseAdapter,
                    QaSource);
                var scopeReleasePipeline = new UnityContentAnchorMaterializationScopeReleasePipeline(
                    releaseAdapter,
                    QaSource);

                var missingRuntimeHostResult = scopeReleasePipeline.ReleaseScope(
                    null,
                    context,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    "qa.content-anchor-scope-release.missing-host");
                bool missingRuntimeHostBlocked = missingRuntimeHostResult.Failed
                    && missingRuntimeHostResult.Status == UnityContentAnchorMaterializationScopeReleasePipelineStatus.FailedMissingRuntimeHost
                    && registry.Count == 0;
                if (!missingRuntimeHostBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='missing-host' diagnostics='{missingRuntimeHostResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var firstPipelineResult = materializationPipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    firstBindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-scope-release.materialize.1");
                var secondPipelineResult = materializationPipeline.MaterializeBindPlace(
                    runtimeHost,
                    anchorSet,
                    secondBindingRequest,
                    anchorObject.transform,
                    true,
                    "qa.content-anchor-scope-release.materialize.2");
                bool materializedScope = firstPipelineResult.Succeeded
                    && secondPipelineResult.Succeeded
                    && registry.Count == 2
                    && registry.ActiveCount == 2
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 2;
                if (!materializedScope)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='materialize-scope' first='{firstPipelineResult.ToDiagnosticString()}' second='{secondPipelineResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var scopeReleaseResult = scopeReleasePipeline.ReleaseScope(
                    runtimeHost,
                    context,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    "qa.content-anchor-scope-release.release");
                bool scopeReleaseSucceeded = scopeReleaseResult.Succeeded
                    && scopeReleaseResult.Status == UnityContentAnchorMaterializationScopeReleasePipelineStatus.Succeeded
                    && scopeReleaseResult.MatchedPhysicalEntries == 2
                    && scopeReleaseResult.PhysicalReleaseRequests == 2
                    && scopeReleaseResult.LogicalReleaseResults == 2
                    && scopeReleaseResult.BindingRemovedCount == 2
                    && registry.Count == 2
                    && registry.ActiveCount == 0
                    && registry.PhysicalReleaseRequestedCount == 2
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!scopeReleaseSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='scope-release' diagnostics='{scopeReleaseResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var secondScopeReleaseResult = scopeReleasePipeline.ReleaseScope(
                    runtimeHost,
                    context,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    "qa.content-anchor-scope-release.release-again");
                bool secondScopeReleaseIdempotent = secondScopeReleaseResult.Succeeded
                    && secondScopeReleaseResult.Status == UnityContentAnchorMaterializationScopeReleasePipelineStatus.SucceededNoContent
                    && secondScopeReleaseResult.MatchedPhysicalEntries == 2
                    && secondScopeReleaseResult.PhysicalReleaseRequests == 0
                    && secondScopeReleaseResult.LogicalReleaseResults == 0
                    && secondScopeReleaseResult.BindingRemovedCount == 0
                    && registry.ActiveCount == 0
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!secondScopeReleaseIdempotent)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='release-again' diagnostics='{secondScopeReleaseResult.ToDiagnosticString()}' registry='{registry.ToDiagnosticString()}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-scope-release.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Scope Release Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Scope Release Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-scope-release' passed='True' missingRuntimeHostBlocked='{missingRuntimeHostBlocked}' materializedScope='{materializedScope}' scopeRelease='{scopeReleaseResult.Status}' physicalReleaseRequests='{scopeReleaseResult.PhysicalReleaseRequests}' logicalReleaseResults='{scopeReleaseResult.LogicalReleaseResults}' bindingRemoved='{scopeReleaseResult.BindingRemovedCount}' releaseAgain='{secondScopeReleaseResult.Status}' releaseAgainIdempotent='{secondScopeReleaseIdempotent}' registry='{registry.ToDiagnosticString()}' contentHandles='{runtimeContentRuntime.SnapshotHandles(context).Length}' contentAnchorPhysicalPlacement='True' pipelineMaterializesObject='True' scopeReleasePhysical='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorCreatesObject='False' contentAnchorDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                foreach (var entry in registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (rootCreated)
                {
                    if (context.IsValid)
                    {
                        if (firstBindingRequest.IsValid)
                        {
                            runtimeHost.UnbindContentAnchor(firstBindingRequest);
                        }

                        if (secondBindingRequest.IsValid)
                        {
                            runtimeHost.UnbindContentAnchor(secondBindingRequest);
                        }

                        runtimeContentRuntime.ReleaseScopeLogically(
                            context,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.content-anchor-scope-release.cleanup.scope-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.content-anchor-scope-release.cleanup.root-remove");
                }
            }
        }


        private async void RunContentAnchorMaterializationBridgeSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Bridge Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationBridgeSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationBridgeSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Bridge Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject bridgeObject = null;
            GameObject template = null;
            GameObject anchorObject = null;
            bool rootCreated = false;
            var owner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge.owner",
                "QA Content Anchor Materialization Bridge Smoke");

            try
            {
                bridgeObject = new GameObject("QA Content Anchor Materialization Bridge");
                bridgeObject.hideFlags = HideFlags.DontSave;
                var bridge = bridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                anchorObject = new GameObject("QA Content Anchor Materialization Bridge Anchor");
                anchorObject.hideFlags = HideFlags.DontSave;

                bridge.ConfigureForDiagnostics(
                    null,
                    anchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge.owner",
                    "QA Content Anchor Materialization Bridge Smoke",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge.route",
                    "qa.content-anchor-materialization-bridge.anchor",
                    "qa.content-anchor-materialization-bridge.content",
                    "qa.content-anchor-materialization-bridge.prefab",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var missingPrefabResult = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.missing-prefab");
                bool missingPrefabBlocked = missingPrefabResult.Failed
                    && missingPrefabResult.Status == UnityContentAnchorMaterializationBridgeStatus.FailedConfiguration
                    && bridge.Registry.Count == 0;
                if (!missingPrefabBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='missing-prefab' diagnostics='{missingPrefabResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                template = new GameObject("QA Content Anchor Materialization Bridge Template");
                template.hideFlags = HideFlags.DontSave;
                bridge.ConfigureForDiagnostics(
                    template,
                    anchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge.owner",
                    "QA Content Anchor Materialization Bridge Smoke",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge.route",
                    "qa.content-anchor-materialization-bridge.anchor",
                    "qa.content-anchor-materialization-bridge.content",
                    "qa.content-anchor-materialization-bridge.prefab",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var materializeResult = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.materialize");
                rootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    owner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge.root-check",
                    out var context);
                bool physicalParentApplied = false;
                foreach (var evidence in bridge.Registry.Snapshot())
                {
                    if (evidence != null && evidence.HasLiveInstance && evidence.Instance.transform.parent == anchorObject.transform)
                    {
                        physicalParentApplied = true;
                        break;
                    }
                }

                bool materializeSucceeded = materializeResult.Succeeded
                    && materializeResult.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededMaterialized
                    && materializeResult.PhysicalPlacementApplied
                    && physicalParentApplied
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 1
                    && context.IsValid
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 1;
                if (!materializeSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='materialize' diagnostics='{materializeResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var duplicateResult = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.duplicate");
                bool duplicateBlocked = duplicateResult.Failed
                    && duplicateResult.Status == UnityContentAnchorMaterializationBridgeStatus.FailedMaterializationPipeline
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 1
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 1;
                if (!duplicateBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='duplicate' diagnostics='{duplicateResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseResult = bridge.SubmitScopeReleaseForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.release");
                bool releaseSucceeded = releaseResult.Succeeded
                    && releaseResult.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleased
                    && releaseResult.PhysicalReleaseRequests == 1
                    && releaseResult.LogicalReleaseResults == 1
                    && releaseResult.BindingRemovedCount == 1
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 0
                    && bridge.Registry.PhysicalReleaseRequestedCount == 1
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!releaseSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='release' diagnostics='{releaseResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAgainResult = bridge.SubmitScopeReleaseForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.release-again");
                bool releaseAgainIdempotent = releaseAgainResult.Succeeded
                    && releaseAgainResult.Status == UnityContentAnchorMaterializationBridgeStatus.SucceededReleaseNoContent
                    && releaseAgainResult.PhysicalReleaseRequests == 0
                    && releaseAgainResult.LogicalReleaseResults == 0
                    && releaseAgainResult.BindingRemovedCount == 0
                    && bridge.Registry.ActiveCount == 0
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!releaseAgainIdempotent)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='release-again' diagnostics='{releaseAgainResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var afterReleaseResult = bridge.SubmitMaterializationForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge.after-release");
                bool afterReleaseBlocked = afterReleaseResult.Failed
                    && afterReleaseResult.Status == UnityContentAnchorMaterializationBridgeStatus.FailedMaterializationPipeline
                    && bridge.Registry.Count == 1
                    && bridge.Registry.ActiveCount == 0
                    && runtimeContentRuntime.SnapshotHandles(context).Length == 0;
                if (!afterReleaseBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='after-release' diagnostics='{afterReleaseResult.ToDiagnosticString()}' registry='{bridge.Registry.ToDiagnosticString()}'.");
                    return false;
                }

                var rootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    owner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge.root.remove");
                if (!rootRemoveResult.Applied && rootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Smoke step failed. step='root-remove' status='{rootRemoveResult.Status}' message='{FormatValue(rootRemoveResult.Message)}'.");
                    return false;
                }

                rootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Bridge Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-bridge' passed='True' missingPrefabBlocked='{missingPrefabBlocked}' materialize='{materializeResult.Status}' duplicateBlocked='{duplicateBlocked}' release='{releaseResult.Status}' releaseAgain='{releaseAgainResult.Status}' releaseAgainIdempotent='{releaseAgainIdempotent}' afterReleaseBlocked='{afterReleaseBlocked}' registry='{bridge.Registry.ToDiagnosticString()}' contentHandles='{runtimeContentRuntime.SnapshotHandles(context).Length}' authoredBridge='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' pipelineMaterializesObject='True' contentAnchorPhysicalPlacement='True' contentAnchorCreatesObject='False' contentAnchorDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                if (bridgeObject != null)
                {
                    var bridge = bridgeObject.GetComponent<UnityContentAnchorMaterializationBridge>();
                    if (bridge != null)
                    {
                        foreach (var entry in bridge.Registry.Snapshot())
                        {
                            if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                            {
                                Object.Destroy(entry.Instance);
                            }
                        }
                    }
                }

                if (template != null)
                {
                    Object.Destroy(template);
                }

                if (anchorObject != null)
                {
                    Object.Destroy(anchorObject);
                }

                if (bridgeObject != null)
                {
                    Object.Destroy(bridgeObject);
                }

                if (rootCreated)
                {
                    if (runtimeContentRuntime.TryCreateScopeContext(
                            owner,
                            QaSource,
                            "qa.content-anchor-materialization-bridge.cleanup.context",
                            out var cleanupContext))
                    {
                        runtimeHost.UnbindContentAnchorRuntimeOwner(
                            owner,
                            QaSource,
                            "qa.content-anchor-materialization-bridge.cleanup.unbind-owner");
                        runtimeContentRuntime.ReleaseScopeLogically(
                            cleanupContext,
                            RuntimeReleasePolicy.MarkReleasedAndUnregister,
                            QaSource,
                            "qa.content-anchor-materialization-bridge.cleanup.logical-release");
                    }

                    runtimeContentRuntime.RemoveScopeRoot(
                        owner,
                        QaSource,
                        "qa.content-anchor-materialization-bridge.cleanup.root-remove");
                }
            }
        }


        private async void RunContentAnchorMaterializationBridgeSetSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Bridge Set Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationBridgeSetSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationBridgeSetSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Bridge Set Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;
            bool firstRootCreated = false;
            bool secondRootCreated = false;
            var firstOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set.owner.1",
                "QA Content Anchor Materialization Bridge Set Smoke 1");
            var secondOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set.owner.2",
                "QA Content Anchor Materialization Bridge Set Smoke 2");

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Bridge Set");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();

                var missingBridgesResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.missing-bridges");
                bool missingBridgesBlocked = missingBridgesResult.Failed
                    && missingBridgesResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedNoBridges
                    && missingBridgesResult.RegistryEntries == 0
                    && missingBridgesResult.RegistryActive == 0;
                if (!missingBridgesBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='missing-bridges' diagnostics='{missingBridgesResult.ToDiagnosticString()}'.");
                    return false;
                }

                firstBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Item 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                secondBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Item 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                firstTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set.owner.1",
                    "QA Content Anchor Materialization Bridge Set Smoke 1",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set.route.1",
                    "qa.content-anchor-materialization-bridge-set.anchor.1",
                    "qa.content-anchor-materialization-bridge-set.content.1",
                    "qa.content-anchor-materialization-bridge-set.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set.owner.2",
                    "QA Content Anchor Materialization Bridge Set Smoke 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set.route.2",
                    "qa.content-anchor-materialization-bridge-set.anchor.2",
                    "qa.content-anchor-materialization-bridge-set.content.2",
                    "qa.content-anchor-materialization-bridge-set.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var materializeAllResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.materialize-all");
                firstRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    firstOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.root-check.1",
                    out var firstContext);
                secondRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    secondOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.root-check.2",
                    out var secondContext);

                bool firstParentApplied = HasParentedLiveInstance(firstBridge, firstAnchorObject.transform);
                bool secondParentApplied = HasParentedLiveInstance(secondBridge, secondAnchorObject.transform);
                bool materializedAll = materializeAllResult.Succeeded
                    && materializeAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
                    && materializeAllResult.BridgeCount == 2
                    && materializeAllResult.MaterializedCount == 2
                    && materializeAllResult.RegistryEntries == 2
                    && materializeAllResult.RegistryActive == 2
                    && firstParentApplied
                    && secondParentApplied
                    && firstContext.IsValid
                    && secondContext.IsValid
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 1
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 1;
                if (!materializedAll)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='materialize-all' diagnostics='{materializeAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                var duplicateResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.duplicate");
                bool duplicateBlocked = duplicateResult.Failed
                    && duplicateResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterialization
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 2
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 1
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 1;
                if (!duplicateBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='duplicate' diagnostics='{duplicateResult.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAllResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.release-all");
                bool releaseAllSucceeded = releaseAllResult.Succeeded
                    && releaseAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releaseAllResult.ReleasedCount == 2
                    && releaseAllResult.PhysicalReleaseRequests == 2
                    && releaseAllResult.LogicalReleaseResults == 2
                    && releaseAllResult.BindingRemovedCount == 2
                    && releaseAllResult.RegistryEntries == 2
                    && releaseAllResult.RegistryActive == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!releaseAllSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='release-all' diagnostics='{releaseAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAgainResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.release-again");
                bool releaseAgainIdempotent = releaseAgainResult.Succeeded
                    && releaseAgainResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleaseNoContent
                    && releaseAgainResult.ReleasedCount == 0
                    && releaseAgainResult.RegistryActive == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!releaseAgainIdempotent)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='release-again' diagnostics='{releaseAgainResult.ToDiagnosticString()}'.");
                    return false;
                }

                var afterReleaseResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.after-release");
                bool afterReleaseBlocked = afterReleaseResult.Failed
                    && afterReleaseResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterialization
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!afterReleaseBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='after-release' diagnostics='{afterReleaseResult.ToDiagnosticString()}'.");
                    return false;
                }

                var firstRootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    firstOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.root.remove.1");
                if (!firstRootRemoveResult.Applied && firstRootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='root-remove-1' status='{firstRootRemoveResult.Status}' message='{FormatValue(firstRootRemoveResult.Message)}'.");
                    return false;
                }

                var secondRootRemoveResult = runtimeContentRuntime.RemoveScopeRoot(
                    secondOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.root.remove.2");
                if (!secondRootRemoveResult.Applied && secondRootRemoveResult.Status != RuntimeRootRegistryOperationStatus.RootMissing)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Smoke step failed. step='root-remove-2' status='{secondRootRemoveResult.Status}' message='{FormatValue(secondRootRemoveResult.Message)}'.");
                    return false;
                }

                firstRootCreated = false;
                secondRootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Bridge Set Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-bridge-set' passed='True' missingBridgesBlocked='{missingBridgesBlocked}' materializeAll='{materializeAllResult.Status}' bridgeCount='{materializeAllResult.BridgeCount}' materialized='{materializeAllResult.MaterializedCount}' duplicateBlocked='{duplicateBlocked}' releaseAll='{releaseAllResult.Status}' released='{releaseAllResult.ReleasedCount}' releaseAgain='{releaseAgainResult.Status}' releaseAgainIdempotent='{releaseAgainIdempotent}' afterReleaseBlocked='{afterReleaseBlocked}' registryEntries='{bridgeSet.RegistryEntries}' registryActive='{bridgeSet.RegistryActive}' physicalReleaseRequests='{bridgeSet.PhysicalReleaseRequestedCount}' contentHandles='{runtimeContentRuntime.SnapshotHandles(firstContext).Length + runtimeContentRuntime.SnapshotHandles(secondContext).Length}' authoredBridgeSet='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }

                if (firstRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "1");
                }

                if (secondRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "2");
                }
            }
        }

        private async void RunContentAnchorMaterializationBridgeSetPreflightSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Bridge Set Preflight Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationBridgeSetPreflightSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationBridgeSetPreflightSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                _logger.Warning("QA Content Anchor Materialization Bridge Set Preflight Smoke failed. reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;
            bool firstRootCreated = false;
            bool secondRootCreated = false;
            var firstOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set-preflight.owner.1",
                "QA Content Anchor Materialization Bridge Set Preflight Smoke 1");
            var secondOwner = RuntimeContentOwner.Transient(
                "qa.content-anchor-materialization-bridge-set-preflight.owner.2",
                "QA Content Anchor Materialization Bridge Set Preflight Smoke 2");

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();
                firstBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Item 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                secondBridgeObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Item 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();
                firstTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Bridge Set Preflight Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-preflight.owner.1",
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke 1",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-preflight.route.1",
                    "qa.content-anchor-materialization-bridge-set-preflight.anchor.1",
                    "qa.content-anchor-materialization-bridge-set-preflight.content.1",
                    "qa.content-anchor-materialization-bridge-set-preflight.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    null,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-preflight.owner.2",
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-preflight.route.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.anchor.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.content.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var missingPrefabPreflightResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.missing-prefab");
                bool missingPrefabBlockedBeforeSideEffects = missingPrefabPreflightResult.Failed
                    && missingPrefabPreflightResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedBridgeMaterializationPreflight
                    && missingPrefabPreflightResult.MaterializedCount == 0
                    && bridgeSet.RegistryEntries == 0
                    && bridgeSet.RegistryActive == 0;
                if (!missingPrefabBlockedBeforeSideEffects)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Preflight Smoke step failed. step='missing-prefab-preflight' diagnostics='{missingPrefabPreflightResult.ToDiagnosticString()}'.");
                    return false;
                }

                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-preflight.owner.1",
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke Duplicate",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-preflight.route.duplicate",
                    "qa.content-anchor-materialization-bridge-set-preflight.anchor.duplicate",
                    "qa.content-anchor-materialization-bridge-set-preflight.content.1",
                    "qa.content-anchor-materialization-bridge-set-preflight.prefab.duplicate",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                var duplicateKeyPreflightResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.duplicate-key");
                bool duplicateKeyBlockedBeforeSideEffects = duplicateKeyPreflightResult.Failed
                    && duplicateKeyPreflightResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.FailedDuplicateMaterializationKey
                    && duplicateKeyPreflightResult.MaterializedCount == 0
                    && bridgeSet.RegistryEntries == 0
                    && bridgeSet.RegistryActive == 0;
                if (!duplicateKeyBlockedBeforeSideEffects)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Preflight Smoke step failed. step='duplicate-key-preflight' diagnostics='{duplicateKeyPreflightResult.ToDiagnosticString()}'.");
                    return false;
                }

                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-materialization-bridge-set-preflight.owner.2",
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-materialization-bridge-set-preflight.route.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.anchor.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.content.2",
                    "qa.content-anchor-materialization-bridge-set-preflight.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                var materializeAllResult = bridgeSet.SubmitMaterializeAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.materialize-all");
                firstRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    firstOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.root-check.1",
                    out var firstContext);
                secondRootCreated = runtimeContentRuntime.TryCreateScopeContext(
                    secondOwner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.root-check.2",
                    out var secondContext);
                bool materializedAllAfterPreflight = materializeAllResult.Succeeded
                    && materializeAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededMaterializedAll
                    && materializeAllResult.MaterializedCount == 2
                    && bridgeSet.RegistryEntries == 2
                    && bridgeSet.RegistryActive == 2
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 1
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 1;
                if (!materializedAllAfterPreflight)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Preflight Smoke step failed. step='materialize-all-after-preflight' diagnostics='{materializeAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                var releaseAllResult = bridgeSet.SubmitReleaseAllForDiagnostics(
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set-preflight.release-all");
                bool releaseAllSucceeded = releaseAllResult.Succeeded
                    && releaseAllResult.Status == UnityContentAnchorMaterializationBridgeSetStatus.SucceededReleasedAll
                    && releaseAllResult.ReleasedCount == 2
                    && releaseAllResult.PhysicalReleaseRequests == 2
                    && releaseAllResult.LogicalReleaseResults == 2
                    && releaseAllResult.BindingRemovedCount == 2
                    && bridgeSet.RegistryActive == 0
                    && runtimeContentRuntime.SnapshotHandles(firstContext).Length == 0
                    && runtimeContentRuntime.SnapshotHandles(secondContext).Length == 0;
                if (!releaseAllSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Bridge Set Preflight Smoke step failed. step='release-all' diagnostics='{releaseAllResult.ToDiagnosticString()}'.");
                    return false;
                }

                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "preflight.1");
                CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "preflight.2");
                firstRootCreated = false;
                secondRootCreated = false;

                _logger.Info(
                    "QA Content Anchor Materialization Bridge Set Preflight Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-bridge-set-preflight' passed='True' missingPrefabBlockedBeforeSideEffects='{missingPrefabBlockedBeforeSideEffects}' duplicateKeyBlockedBeforeSideEffects='{duplicateKeyBlockedBeforeSideEffects}' materializeAll='{materializeAllResult.Status}' materialized='{materializeAllResult.MaterializedCount}' releaseAll='{releaseAllResult.Status}' released='{releaseAllResult.ReleasedCount}' registryEntries='{bridgeSet.RegistryEntries}' registryActive='{bridgeSet.RegistryActive}' physicalReleaseRequests='{bridgeSet.PhysicalReleaseRequestedCount}' contentHandles='{runtimeContentRuntime.SnapshotHandles(firstContext).Length + runtimeContentRuntime.SnapshotHandles(secondContext).Length}' batchPreflight='True' partialMaterializationBlocked='True' authoredBridgeSet='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }

                if (firstRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, firstOwner, "preflight.1");
                }

                if (secondRootCreated)
                {
                    CleanupBridgeSetSmokeOwner(runtimeHost, runtimeContentRuntime, secondOwner, "preflight.2");
                }
            }
        }

        private async void RunContentAnchorMaterializationAuthoringValidationSmoke()
        {
            await RunSmokeAsync("Content Anchor Materialization Authoring Validation Smoke", runtimeHost =>
                Task.FromResult(RunContentAnchorMaterializationAuthoringValidationSmokeCore(runtimeHost)));
        }

        private bool RunContentAnchorMaterializationAuthoringValidationSmokeCore(FrameworkRuntimeHost runtimeHost)
        {
            GameObject setObject = null;
            GameObject firstBridgeObject = null;
            GameObject secondBridgeObject = null;
            GameObject firstTemplate = null;
            GameObject secondTemplate = null;
            GameObject firstAnchorObject = null;
            GameObject secondAnchorObject = null;

            try
            {
                setObject = new GameObject("QA Content Anchor Materialization Authoring Validation Set");
                setObject.hideFlags = HideFlags.DontSave;
                var bridgeSet = setObject.AddComponent<UnityContentAnchorMaterializationBridgeSet>();

                firstBridgeObject = new GameObject("QA Content Anchor Materialization Authoring Validation Bridge 1");
                firstBridgeObject.hideFlags = HideFlags.DontSave;
                var firstBridge = firstBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                secondBridgeObject = new GameObject("QA Content Anchor Materialization Authoring Validation Bridge 2");
                secondBridgeObject.hideFlags = HideFlags.DontSave;
                var secondBridge = secondBridgeObject.AddComponent<UnityContentAnchorMaterializationBridge>();

                firstTemplate = new GameObject("QA Content Anchor Materialization Authoring Validation Template 1");
                firstTemplate.hideFlags = HideFlags.DontSave;
                secondTemplate = new GameObject("QA Content Anchor Materialization Authoring Validation Template 2");
                secondTemplate.hideFlags = HideFlags.DontSave;
                firstAnchorObject = new GameObject("QA Content Anchor Materialization Authoring Validation Anchor 1");
                firstAnchorObject.hideFlags = HideFlags.DontSave;
                secondAnchorObject = new GameObject("QA Content Anchor Materialization Authoring Validation Anchor 2");
                secondAnchorObject.hideFlags = HideFlags.DontSave;

                var invalidBridgeResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridge(
                    firstBridge,
                    "qa.content-anchor-authoring.invalid-bridge");
                bool invalidBridgeBlocked = invalidBridgeResult.Failed
                    && invalidBridgeResult.Status == UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeConfiguration
                    && invalidBridgeResult.MissingPrefabCount == 1
                    && invalidBridgeResult.MissingAnchorTransformCount == 1
                    && firstBridge.RegistryCount == 0;
                if (!invalidBridgeBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Authoring Validation Smoke step failed. step='invalid-bridge' diagnostics='{invalidBridgeResult.ToDiagnosticString()}'.");
                    return false;
                }

                firstBridge.ConfigureForDiagnostics(
                    firstTemplate,
                    firstAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-authoring.owner.duplicate",
                    "QA Content Anchor Materialization Authoring Validation Duplicate Owner",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-authoring.route.1",
                    "qa.content-anchor-authoring.anchor.1",
                    "qa.content-anchor-authoring.content.duplicate",
                    "qa.content-anchor-authoring.prefab.1",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-authoring.owner.duplicate",
                    "QA Content Anchor Materialization Authoring Validation Duplicate Owner",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-authoring.route.2",
                    "qa.content-anchor-authoring.anchor.2",
                    "qa.content-anchor-authoring.content.duplicate",
                    "qa.content-anchor-authoring.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);
                bridgeSet.ConfigureForDiagnostics(new[] { firstBridge, secondBridge }, false);

                var duplicateKeyResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridgeSet(
                    bridgeSet,
                    "qa.content-anchor-authoring.duplicate-key");
                bool duplicateKeyBlocked = duplicateKeyResult.Failed
                    && duplicateKeyResult.Status == UnityContentAnchorMaterializationAuthoringValidationStatus.FailedBridgeSetConfiguration
                    && duplicateKeyResult.DuplicateMaterializationKeyCount == 1
                    && bridgeSet.RegistryEntries == 0
                    && bridgeSet.RegistryActive == 0;
                if (!duplicateKeyBlocked)
                {
                    _logger.Warning($"QA Content Anchor Materialization Authoring Validation Smoke step failed. step='duplicate-key' diagnostics='{duplicateKeyResult.ToDiagnosticString()}'.");
                    return false;
                }

                secondBridge.ConfigureForDiagnostics(
                    secondTemplate,
                    secondAnchorObject.transform,
                    RuntimeContentScope.Transient,
                    "qa.content-anchor-authoring.owner.2",
                    "QA Content Anchor Materialization Authoring Validation Owner 2",
                    true,
                    ContentAnchorScope.Route,
                    ContentAnchorKind.Slot,
                    ContentAnchorRequiredness.Required,
                    "qa.content-anchor-authoring.route.2",
                    "qa.content-anchor-authoring.anchor.2",
                    "qa.content-anchor-authoring.content.2",
                    "qa.content-anchor-authoring.prefab.2",
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    true,
                    false);

                var validFirstBridgeResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridge(
                    firstBridge,
                    "qa.content-anchor-authoring.valid-bridge.1");
                var validSetResult = UnityContentAnchorMaterializationAuthoringValidator.ValidateBridgeSet(
                    bridgeSet,
                    "qa.content-anchor-authoring.valid-set");
                bool validSetSucceeded = validFirstBridgeResult.Succeeded
                    && validSetResult.Succeeded
                    && validSetResult.BridgeCount == 2
                    && validSetResult.BlockingIssueCount == 0
                    && bridgeSet.RegistryEntries == 0
                    && bridgeSet.RegistryActive == 0
                    && bridgeSet.PhysicalReleaseRequestedCount == 0;
                if (!validSetSucceeded)
                {
                    _logger.Warning($"QA Content Anchor Materialization Authoring Validation Smoke step failed. step='valid-set' bridge='{validFirstBridgeResult.ToDiagnosticString()}' set='{validSetResult.ToDiagnosticString()}'.");
                    return false;
                }

                _logger.Info(
                    "QA Content Anchor Materialization Authoring Validation Smoke step completed. "
                    + $"step='unity-content-anchor-materialization-authoring-validation' passed='True' invalidBridgeBlocked='{invalidBridgeBlocked}' duplicateKeyBlocked='{duplicateKeyBlocked}' validBridge='{validFirstBridgeResult.Status}' validSet='{validSetResult.Status}' bridgeCount='{validSetResult.BridgeCount}' blockingIssues='{validSetResult.BlockingIssueCount}' registryEntries='{bridgeSet.RegistryEntries}' registryActive='{bridgeSet.RegistryActive}' physicalReleaseRequests='{bridgeSet.PhysicalReleaseRequestedCount}' authoringValidation='True' batchPreflight='True' noRuntimeSideEffects='True' authoredBridgeSet='True' explicitSubmit='True' automaticLifecycleWiring='False' routeActivityAutoMaterialization='False' contentAnchorPhysicalPlacement='True' bridgeSetCreatesObject='False' bridgeSetDestroysObject='False' addressables='False' pooling='False' actorSpawn='False' playerJoin='False' gameplayConsumer='False' cameraConsumer='False' audioConsumer='False' saveConsumer='False'.");
                return true;
            }
            finally
            {
                DestroyBridgeSetSmokeObject(firstBridgeObject);
                DestroyBridgeSetSmokeObject(secondBridgeObject);

                if (firstTemplate != null)
                {
                    Object.Destroy(firstTemplate);
                }

                if (secondTemplate != null)
                {
                    Object.Destroy(secondTemplate);
                }

                if (firstAnchorObject != null)
                {
                    Object.Destroy(firstAnchorObject);
                }

                if (secondAnchorObject != null)
                {
                    Object.Destroy(secondAnchorObject);
                }

                if (setObject != null)
                {
                    Object.Destroy(setObject);
                }
            }
        }

        private static bool HasParentedLiveInstance(UnityContentAnchorMaterializationBridge bridge, Transform expectedParent)
        {
            if (bridge == null || expectedParent == null)
            {
                return false;
            }

            foreach (var evidence in bridge.Registry.Snapshot())
            {
                if (evidence != null && evidence.HasLiveInstance && evidence.Instance.transform.parent == expectedParent)
                {
                    return true;
                }
            }

            return false;
        }

        private static void DestroyBridgeSetSmokeObject(GameObject bridgeObject)
        {
            if (bridgeObject == null)
            {
                return;
            }

            var bridge = bridgeObject.GetComponent<UnityContentAnchorMaterializationBridge>();
            if (bridge != null)
            {
                foreach (var entry in bridge.Registry.Snapshot())
                {
                    if (entry != null && entry.HasLiveInstance && !entry.PhysicalReleaseRequested)
                    {
                        Object.Destroy(entry.Instance);
                    }
                }
            }

            Object.Destroy(bridgeObject);
        }

        private static void CleanupBridgeSetSmokeOwner(
            FrameworkRuntimeHost runtimeHost,
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeContentOwner owner,
            string suffix)
        {
            if (runtimeHost == null || runtimeContentRuntime == null)
            {
                return;
            }

            if (runtimeContentRuntime.TryCreateScopeContext(
                    owner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.cleanup.context." + suffix,
                    out var cleanupContext))
            {
                runtimeHost.UnbindContentAnchorRuntimeOwner(
                    owner,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.cleanup.unbind-owner." + suffix);
                runtimeContentRuntime.ReleaseScopeLogically(
                    cleanupContext,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-materialization-bridge-set.cleanup.logical-release." + suffix);
            }

            runtimeContentRuntime.RemoveScopeRoot(
                owner,
                QaSource,
                "qa.content-anchor-materialization-bridge-set.cleanup.root-remove." + suffix);
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

        private async void RunActivityContentAnchorDiagnosticsSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Activity Content Anchor Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");

            if (!TryValidateSmokeTargets("Activity Content Anchor Diagnostics Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Anchor Diagnostics Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Activity Content Anchor Diagnostics Smoke step failed. step='prepare' reason='Activity Content Anchor Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.activity-content-anchor.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Diagnostics Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var anchorResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.activity-content-anchor.diagnostics")));
                return ValidateActivityContentAnchorDiagnosticsSmokeStep(anchorResult, "activity-anchors");
            });
        }

        private async void RunActivityContentAnchorPositiveSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Activity Content Anchor Positive Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity / Positive Activity Anchor Owner");

            if (!TryValidateSmokeTargets("Activity Content Anchor Positive Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Anchor Positive Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Activity Content Anchor Positive Smoke step failed. step='prepare' reason='Positive Activity Anchor Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.activity-content-anchor-positive.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var routeResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.activity-content-anchor-positive.route")));
                if (!routeResult.Succeeded)
                {
                    _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='route' reason='Route request did not succeed' status='{FormatValue(routeResult.Kind.ToString())}'.");
                    return false;
                }

                GameObject fixtureObject = null;
                try
                {
                    if (!TryCreatePositiveActivityContentAnchorFixture(primaryActivity, out fixtureObject, out var fixtureAnchor))
                    {
                        return false;
                    }

                    var clearResult = await ClearActivityWithResultCoreAsync(
                        runtimeHost,
                        ResolveReason(clearActivityReason, "qa.activity-content-anchor-positive.clear"));
                    if (!clearResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='clear' reason='Activity clear did not succeed before positive rediscovery' status='{FormatValue(clearResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();

                    var activityResult = await RequestActivityWithResultCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-anchor-positive.activity")));
                    return ValidateActivityContentAnchorPositiveSmokeStep(
                        activityResult,
                        "activity-anchor-positive",
                        fixtureAnchor);
                }
                finally
                {
                    if (fixtureObject != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(fixtureObject);
#else
                        fixtureObject.SetActive(false);
                        Destroy(fixtureObject);
#endif
                    }
                }
            });
        }


        private async void RunActivityContentExecutionRuntimeSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");
            if (!TryValidateSmokeTargets("Activity Content Execution Runtime Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Execution Runtime Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    if (!await RequestActivityCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "QA Primary Content Activity")),
                        allowAlreadyActive: true))
                    {
                        return false;
                    }

                    await Task.Yield();
                }

                var activityFlow = runtimeHost.State.ActivityFlowResult;
                var activity = runtimeHost.State.CurrentActivity;
                if (activity == null)
                {
                    _logger.Warning("QA Activity Content Execution Runtime Smoke step failed. step='activity' reason='No active Activity is available'.");
                    return false;
                }

                var runtimeScope = activityFlow.RuntimeActivityScopeResult;
                if (!runtimeScope.HasContext)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='context' reason='Activity runtime scope context unavailable' runtimeActivityScope='{FormatValue(runtimeScope.DiagnosticStatus)}' runtimeActivityContext='{FormatValue(runtimeScope.ContextStatus)}'.");
                    return false;
                }

                var enterRequiredParticipant = new SyntheticActivityContentExecutionParticipant(
                    ActivityContentExecutionParticipantDescriptor.Required(
                        RuntimeContentId.From("qa.activity.execution.required"),
                        supportsEnter: true,
                        supportsExit: true,
                        order: 10,
                        displayName: "QA Required Execution Participant",
                        source: QaSource,
                        reason: "qa.activity-content-execution.required"),
                    SyntheticActivityContentExecutionParticipantMode.Success);

                var enterOptionalParticipant = new SyntheticActivityContentExecutionParticipant(
                    ActivityContentExecutionParticipantDescriptor.Optional(
                        RuntimeContentId.From("qa.activity.execution.optional.enter"),
                        supportsEnter: true,
                        supportsExit: false,
                        order: 20,
                        displayName: "QA Optional Enter Execution Participant",
                        source: QaSource,
                        reason: "qa.activity-content-execution.optional-enter"),
                    SyntheticActivityContentExecutionParticipantMode.NoOp);

                var participants = ActivityContentExecutionParticipantCollection.FromParticipants(
                    new IActivityContentExecutionParticipant[]
                    {
                        enterRequiredParticipant,
                        enterOptionalParticipant
                    });

                if (participants.HasIssues || participants.Count != 2 || participants.EnterCount != 2 || participants.ExitCount != 1)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='collection' count='{participants.Count}' enter='{participants.EnterCount}' exit='{participants.ExitCount}' issues='{participants.IssueCount}' details='{FormatValue(participants.ToDiagnosticString())}'.");
                    return false;
                }

                var runtime = new ActivityContentExecutionRuntime();
                var enterPlan = ActivityContentExecutionRequestFactory.CreateEnterPlan(
                    activity,
                    activityFlow.PreviousActivity,
                    runtimeScope.Context,
                    participants,
                    QaSource,
                    "qa.activity-content-execution.enter");

                if (!enterPlan.Planned || enterPlan.RequestCount != 2 || enterPlan.RequiredCount != 1 || enterPlan.OptionalCount != 1)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='enter-plan' status='{enterPlan.Status}' requests='{enterPlan.RequestCount}' required='{enterPlan.RequiredCount}' optional='{enterPlan.OptionalCount}' message='{FormatValue(enterPlan.Message)}'.");
                    return false;
                }

                var enterResult = runtime.ExecutePhasePlan(
                    enterPlan,
                    QaSource,
                    "qa.activity-content-execution.enter");

                if (!enterResult.Succeeded || enterResult.Status != ActivityContentExecutionAggregateStatus.Succeeded || enterResult.ResultCount != 2 || enterResult.BlocksReadiness || enterResult.BlockingIssueCount != 0)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='enter-execute' status='{enterResult.Status}' results='{enterResult.ResultCount}' blockingIssues='{enterResult.BlockingIssueCount}' nonBlockingIssues='{enterResult.NonBlockingIssueCount}' blocksReadiness='{enterResult.BlocksReadiness}' details='{FormatValue(enterResult.ToDiagnosticString())}'.");
                    return false;
                }

                var exitPlan = ActivityContentExecutionRequestFactory.CreateExitPlan(
                    activity,
                    null,
                    runtimeScope.Context,
                    participants,
                    QaSource,
                    "qa.activity-content-execution.exit");

                if (!exitPlan.Planned || exitPlan.RequestCount != 1 || exitPlan.RequiredCount != 1 || exitPlan.OptionalCount != 0)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='exit-plan' status='{exitPlan.Status}' requests='{exitPlan.RequestCount}' required='{exitPlan.RequiredCount}' optional='{exitPlan.OptionalCount}' message='{FormatValue(exitPlan.Message)}'.");
                    return false;
                }

                var exitResult = runtime.ExecutePhasePlan(
                    exitPlan,
                    QaSource,
                    "qa.activity-content-execution.exit");

                if (!exitResult.Succeeded || exitResult.Status != ActivityContentExecutionAggregateStatus.Succeeded || exitResult.ResultCount != 1 || exitResult.BlocksReadiness || exitResult.BlockingIssueCount != 0)
                {
                    _logger.Warning($"QA Activity Content Execution Runtime Smoke step failed. step='exit-execute' status='{exitResult.Status}' results='{exitResult.ResultCount}' blockingIssues='{exitResult.BlockingIssueCount}' nonBlockingIssues='{exitResult.NonBlockingIssueCount}' blocksReadiness='{exitResult.BlocksReadiness}' details='{FormatValue(exitResult.ToDiagnosticString())}'.");
                    return false;
                }

                _logger.Info(
                    "QA Activity Content Execution Runtime Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "synthetic-execution"),
                        LogFields.Field("activity", GetAssetName(activity, "<none>")),
                        LogFields.Field("participants", participants.Count),
                        LogFields.Field("collectionIssues", participants.IssueCount),
                        LogFields.Field("enterPlan", enterPlan.Status.ToString()),
                        LogFields.Field("enterRequests", enterPlan.RequestCount),
                        LogFields.Field("enterStatus", enterResult.Status.ToString()),
                        LogFields.Field("enterResults", enterResult.ResultCount),
                        LogFields.Field("enterRequired", enterResult.RequiredCount),
                        LogFields.Field("enterOptional", enterResult.OptionalCount),
                        LogFields.Field("enterBlockingIssues", enterResult.BlockingIssueCount),
                        LogFields.Field("enterNonBlockingIssues", enterResult.NonBlockingIssueCount),
                        LogFields.Field("enterBlocksReadiness", enterResult.BlocksReadiness),
                        LogFields.Field("exitPlan", exitPlan.Status.ToString()),
                        LogFields.Field("exitRequests", exitPlan.RequestCount),
                        LogFields.Field("exitStatus", exitResult.Status.ToString()),
                        LogFields.Field("exitResults", exitResult.ResultCount),
                        LogFields.Field("exitRequired", exitResult.RequiredCount),
                        LogFields.Field("exitOptional", exitResult.OptionalCount),
                        LogFields.Field("exitBlockingIssues", exitResult.BlockingIssueCount),
                        LogFields.Field("exitNonBlockingIssues", exitResult.NonBlockingIssueCount),
                        LogFields.Field("exitBlocksReadiness", exitResult.BlocksReadiness)));

                _logger.Debug(
                    "QA Activity Content Execution Runtime Smoke diagnostics.",
                    LogFields.Of(
                        LogFields.Field("collection", participants.ToDiagnosticString()),
                        LogFields.Field("enter", enterResult.ToDiagnosticString()),
                        LogFields.Field("exit", exitResult.ToDiagnosticString())));

                return true;
            });
        }

        private async void RunActivityContentExecutionLifecycleTransitionSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");

            if (!TryValidateSmokeTargets("Activity Content Execution Lifecycle Transition Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Execution Lifecycle Transition Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    if (!await RequestActivityCoreAsync(
                            runtimeHost,
                            primaryActivity,
                            ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-lifecycle.prepare")),
                            allowAlreadyActive: true))
                    {
                        return false;
                    }

                    await Task.Yield();
                }

                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    _logger.Warning("QA Activity Content Execution Lifecycle Transition Smoke step failed. step='prepare' reason='Primary Activity is not active before clear'.");
                    return false;
                }

                var clearResult = await ClearActivityWithResultCoreAsync(
                    runtimeHost,
                    ResolveReason(clearActivityReason, "qa.activity-content-execution-lifecycle.clear"));
                if (!clearResult.Succeeded)
                {
                    _logger.Warning($"QA Activity Content Execution Lifecycle Transition Smoke step failed. step='clear' reason='Activity clear did not succeed' status='{FormatValue(clearResult.Kind.ToString())}'.");
                    return false;
                }

                var clearExecution = clearResult.ActivityFlowResult.ActivityContentExecutionResult;
                if (!clearExecution.Executed
                    || !clearExecution.Succeeded
                    || clearExecution.Status != ActivityContentExecutionLifecycleStatus.SucceededNoContent
                    || clearExecution.ExitResult.Status != ActivityContentExecutionAggregateStatus.SkippedNoContent
                    || clearExecution.ExitRequestCount != 0
                    || clearExecution.ExitResultCount != 0
                    || clearExecution.BlocksReadiness)
                {
                    _logger.Warning(
                        $"QA Activity Content Execution Lifecycle Transition Smoke step failed. step='clear-execution' reason='Activity clear execution diagnostics did not match empty exit expectation' execution='{FormatValue(clearExecution.DiagnosticStatus)}' exit='{FormatValue(clearExecution.ExitResult.Status.ToString())}' exitRequests='{clearExecution.ExitRequestCount}' exitResults='{clearExecution.ExitResultCount}' blocksReadiness='{clearExecution.BlocksReadiness}' details='{FormatValue(clearExecution.ToDiagnosticString())}'.");
                    return false;
                }

                await Task.Yield();

                var restoreResult = await RequestActivityWithResultCoreAsync(
                    runtimeHost,
                    primaryActivity,
                    ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-lifecycle.restore")));
                if (!restoreResult.Succeeded)
                {
                    _logger.Warning($"QA Activity Content Execution Lifecycle Transition Smoke step failed. step='restore' reason='Primary Activity restore did not succeed' status='{FormatValue(restoreResult.Kind.ToString())}'.");
                    return false;
                }

                var restoreExecution = restoreResult.ActivityFlowResult.ActivityContentExecutionResult;
                if (!restoreExecution.Executed
                    || !restoreExecution.Succeeded
                    || restoreExecution.Status != ActivityContentExecutionLifecycleStatus.SucceededNoContent
                    || restoreExecution.EnterResult.Status != ActivityContentExecutionAggregateStatus.SkippedNoContent
                    || restoreExecution.EnterRequestCount != 0
                    || restoreExecution.EnterResultCount != 0
                    || restoreExecution.BlocksReadiness)
                {
                    _logger.Warning(
                        $"QA Activity Content Execution Lifecycle Transition Smoke step failed. step='restore-execution' reason='Activity restore execution diagnostics did not match empty enter expectation' execution='{FormatValue(restoreExecution.DiagnosticStatus)}' enter='{FormatValue(restoreExecution.EnterResult.Status.ToString())}' enterRequests='{restoreExecution.EnterRequestCount}' enterResults='{restoreExecution.EnterResultCount}' blocksReadiness='{restoreExecution.BlocksReadiness}' details='{FormatValue(restoreExecution.ToDiagnosticString())}'.");
                    return false;
                }

                _logger.Info(
                    "QA Activity Content Execution Lifecycle Transition Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "activity-clear-restore"),
                        LogFields.Field("activity", GetAssetName(primaryActivity, "<none>")),
                        LogFields.Field("clearExecution", clearExecution.Status.ToString()),
                        LogFields.Field("clearEnter", clearExecution.EnterResult.Status.ToString()),
                        LogFields.Field("clearEnterRequests", clearExecution.EnterRequestCount),
                        LogFields.Field("clearExit", clearExecution.ExitResult.Status.ToString()),
                        LogFields.Field("clearExitRequests", clearExecution.ExitRequestCount),
                        LogFields.Field("clearExitResults", clearExecution.ExitResultCount),
                        LogFields.Field("clearBlocksReadiness", clearExecution.BlocksReadiness),
                        LogFields.Field("restoreExecution", restoreExecution.Status.ToString()),
                        LogFields.Field("restoreEnter", restoreExecution.EnterResult.Status.ToString()),
                        LogFields.Field("restoreEnterRequests", restoreExecution.EnterRequestCount),
                        LogFields.Field("restoreEnterResults", restoreExecution.EnterResultCount),
                        LogFields.Field("restoreExit", restoreExecution.ExitResult.Status.ToString()),
                        LogFields.Field("restoreExitRequests", restoreExecution.ExitRequestCount),
                        LogFields.Field("restoreBlocksReadiness", restoreExecution.BlocksReadiness),
                        LogFields.Field("runtimeRootCount", runtimeHost.RuntimeContentRuntime.RootCount)));

                _logger.Debug(
                    "QA Activity Content Execution Lifecycle Transition Smoke diagnostics.",
                    LogFields.Of(
                        LogFields.Field("clearExecution", clearExecution.ToDiagnosticString()),
                        LogFields.Field("restoreExecution", restoreExecution.ToDiagnosticString())));

                return true;
            });
        }

        private async void RunActivityContentExecutionParticipantSourceSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity");

            if (!TryValidateSmokeTargets("Activity Content Execution Participant Source Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Execution Participant Source Smoke", async runtimeHost =>
            {
                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    if (!await RequestActivityCoreAsync(
                            runtimeHost,
                            primaryActivity,
                            ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-source.prepare")),
                            allowAlreadyActive: true))
                    {
                        return false;
                    }

                    await Task.Yield();
                }

                if (!ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                {
                    _logger.Warning("QA Activity Content Execution Participant Source Smoke step failed. step='prepare' reason='Primary Activity is not active before source injection'.");
                    return false;
                }

                var source = new SyntheticActivityContentExecutionParticipantSource(
                    new IActivityContentExecutionParticipant[]
                    {
                        new SyntheticActivityContentExecutionParticipant(
                            ActivityContentExecutionParticipantDescriptor.Required(
                                RuntimeContentId.From("qa.activity.execution.source.required"),
                                supportsEnter: true,
                                supportsExit: true,
                                order: 10,
                                displayName: "QA Source Required Execution Participant",
                                source: QaSource,
                                reason: "qa.activity-content-execution-source.required"),
                            SyntheticActivityContentExecutionParticipantMode.Success),
                        new SyntheticActivityContentExecutionParticipant(
                            ActivityContentExecutionParticipantDescriptor.Optional(
                                RuntimeContentId.From("qa.activity.execution.source.optional.enter"),
                                supportsEnter: true,
                                supportsExit: false,
                                order: 20,
                                displayName: "QA Source Optional Enter Execution Participant",
                                source: QaSource,
                                reason: "qa.activity-content-execution-source.optional-enter"),
                            SyntheticActivityContentExecutionParticipantMode.NoOp)
                    });

                FrameworkActivityRequestResult clearResult = default;
                FrameworkActivityRequestResult restoreResult = default;
                bool shouldRestore = false;

                runtimeHost.SetActivityContentExecutionParticipantSource(source);
                try
                {
                    clearResult = await ClearActivityWithResultCoreAsync(
                        runtimeHost,
                        ResolveReason(clearActivityReason, "qa.activity-content-execution-source.clear"));
                    if (!clearResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Execution Participant Source Smoke step failed. step='clear' reason='Activity clear did not succeed' status='{FormatValue(clearResult.Kind.ToString())}'.");
                        return false;
                    }

                    shouldRestore = true;
                    var clearExecution = clearResult.ActivityFlowResult.ActivityContentExecutionResult;
                    bool clearValid = clearExecution is { Executed: true, Succeeded: true, Status: ActivityContentExecutionLifecycleStatus.Succeeded }
                        && clearExecution.ParticipantSourceStatus == ActivityContentExecutionParticipantSourceStatus.Succeeded.ToString()
                        && clearExecution is { ParticipantCount: 2, ExitResult: { Status: ActivityContentExecutionAggregateStatus.Succeeded, RequiredCount: 1, OptionalCount: 0, BlockingIssueCount: 0 }, ExitRequestCount: 1, ExitResultCount: 1, BlocksReadiness: false };

                    if (!clearValid)
                    {
                        _logger.Warning(
                            $"QA Activity Content Execution Participant Source Smoke step failed. step='clear-execution' reason='Activity clear did not execute sourced exit participant as expected' execution='{FormatValue(clearExecution.DiagnosticStatus)}' source='{FormatValue(clearExecution.ParticipantSourceStatus)}' participants='{clearExecution.ParticipantCount}' exit='{FormatValue(clearExecution.ExitResult.Status.ToString())}' exitRequests='{clearExecution.ExitRequestCount}' exitResults='{clearExecution.ExitResultCount}' blockingIssues='{clearExecution.ExitResult.BlockingIssueCount}' blocksReadiness='{clearExecution.BlocksReadiness}' details='{FormatValue(clearExecution.ToDiagnosticString())}'.");
                    }

                    await Task.Yield();

                    restoreResult = await RequestActivityWithResultCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-source.restore")));
                    shouldRestore = false;
                    if (!restoreResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Execution Participant Source Smoke step failed. step='restore' reason='Primary Activity restore did not succeed' status='{FormatValue(restoreResult.Kind.ToString())}'.");
                        return false;
                    }

                    var restoreExecution = restoreResult.ActivityFlowResult.ActivityContentExecutionResult;
                    bool restoreValid = restoreExecution is { Executed: true, Succeeded: true, Status: ActivityContentExecutionLifecycleStatus.Succeeded }
                        && restoreExecution.ParticipantSourceStatus == ActivityContentExecutionParticipantSourceStatus.Succeeded.ToString()
                        && restoreExecution is { ParticipantCount: 2, EnterResult: { Status: ActivityContentExecutionAggregateStatus.Succeeded, RequiredCount: 1, OptionalCount: 1, BlockingIssueCount: 0 }, EnterRequestCount: 2, EnterResultCount: 2, BlocksReadiness: false };

                    if (!restoreValid)
                    {
                        _logger.Warning(
                            $"QA Activity Content Execution Participant Source Smoke step failed. step='restore-execution' reason='Activity restore did not execute sourced enter participants as expected' execution='{FormatValue(restoreExecution.DiagnosticStatus)}' source='{FormatValue(restoreExecution.ParticipantSourceStatus)}' participants='{restoreExecution.ParticipantCount}' enter='{FormatValue(restoreExecution.EnterResult.Status.ToString())}' enterRequests='{restoreExecution.EnterRequestCount}' enterResults='{restoreExecution.EnterResultCount}' blockingIssues='{restoreExecution.EnterResult.BlockingIssueCount}' blocksReadiness='{restoreExecution.BlocksReadiness}' details='{FormatValue(restoreExecution.ToDiagnosticString())}'.");
                    }

                    if (!clearValid || !restoreValid)
                    {
                        return false;
                    }

                    _logger.Info(
                        "QA Activity Content Execution Participant Source Smoke step completed.",
                        LogFields.Of(
                            LogFields.Field("step", "explicit-source-lifecycle"),
                            LogFields.Field("activity", GetAssetName(primaryActivity, "<none>")),
                            LogFields.Field("sourceResolutions", source.ResolveCount),
                            LogFields.Field("clearExecution", clearExecution.Status.ToString()),
                            LogFields.Field("clearSource", clearExecution.ParticipantSourceStatus),
                            LogFields.Field("clearParticipants", clearExecution.ParticipantCount),
                            LogFields.Field("clearExit", clearExecution.ExitResult.Status.ToString()),
                            LogFields.Field("clearExitRequests", clearExecution.ExitRequestCount),
                            LogFields.Field("clearExitResults", clearExecution.ExitResultCount),
                            LogFields.Field("clearExitRequired", clearExecution.ExitResult.RequiredCount),
                            LogFields.Field("clearBlockingIssues", clearExecution.ExitResult.BlockingIssueCount),
                            LogFields.Field("clearBlocksReadiness", clearExecution.BlocksReadiness),
                            LogFields.Field("restoreExecution", restoreExecution.Status.ToString()),
                            LogFields.Field("restoreSource", restoreExecution.ParticipantSourceStatus),
                            LogFields.Field("restoreParticipants", restoreExecution.ParticipantCount),
                            LogFields.Field("restoreEnter", restoreExecution.EnterResult.Status.ToString()),
                            LogFields.Field("restoreEnterRequests", restoreExecution.EnterRequestCount),
                            LogFields.Field("restoreEnterResults", restoreExecution.EnterResultCount),
                            LogFields.Field("restoreEnterRequired", restoreExecution.EnterResult.RequiredCount),
                            LogFields.Field("restoreEnterOptional", restoreExecution.EnterResult.OptionalCount),
                            LogFields.Field("restoreBlockingIssues", restoreExecution.EnterResult.BlockingIssueCount),
                            LogFields.Field("restoreBlocksReadiness", restoreExecution.BlocksReadiness),
                            LogFields.Field("runtimeRootCount", runtimeHost.RuntimeContentRuntime.RootCount)));

                    _logger.Debug(
                        "QA Activity Content Execution Participant Source Smoke diagnostics.",
                        LogFields.Of(
                            LogFields.Field("clearExecution", clearExecution.ToDiagnosticString()),
                            LogFields.Field("restoreExecution", restoreExecution.ToDiagnosticString())));

                    return true;
                }
                finally
                {
                    runtimeHost.SetActivityContentExecutionParticipantSource(EmptyActivityContentExecutionParticipantSource.Instance);
                    if (shouldRestore && !ReferenceEquals(runtimeHost.State.CurrentActivity, primaryActivity))
                    {
                        await RequestActivityWithResultCoreAsync(
                            runtimeHost,
                            primaryActivity,
                            ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-execution-source.restore-after-failure")));
                    }
                }
            });
        }

        private async void RunActivityContentAnchorBindingSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Activity Content Anchor Binding Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");
            AppendMissingQaAsset(ref missingTargets, primaryActivity == null, "Primary Activity / Activity Anchor Binding Owner");

            if (!TryValidateSmokeTargets("Activity Content Anchor Binding Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Activity Content Anchor Binding Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Activity Content Anchor Binding Smoke step failed. step='prepare' reason='Activity Anchor Binding Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.activity-content-anchor-binding.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var routeResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.activity-content-anchor-binding.route")));
                if (!routeResult.Succeeded)
                {
                    _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='route' reason='Route request did not succeed' status='{FormatValue(routeResult.Kind.ToString())}'.");
                    return false;
                }

                GameObject fixtureObject = null;
                try
                {
                    if (!TryCreatePositiveActivityContentAnchorFixture(primaryActivity, out fixtureObject, out _))
                    {
                        return false;
                    }

                    var clearResult = await ClearActivityWithResultCoreAsync(
                        runtimeHost,
                        ResolveReason(clearActivityReason, "qa.activity-content-anchor-binding.clear-before-discovery"));
                    if (!clearResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='clear-before-discovery' reason='Activity clear did not succeed before binding discovery' status='{FormatValue(clearResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();

                    var activityResult = await RequestActivityWithResultCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-anchor-binding.activity")));

                    if (!TryCreateActivityContentAnchorBindingForCleanupSmoke(
                            activityResult,
                            runtimeHost,
                            out var context,
                            out var materializationIdentity,
                            out var bindingResult,
                            out var idempotentBindingResult,
                            out int bindingCountBefore))
                    {
                        return false;
                    }

                    var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
                    var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                        context,
                        materializationIdentity,
                        RuntimeReleasePolicy.MarkReleasedAndUnregister,
                        QaSource,
                        "qa.activity-content-anchor-binding.release-before-activity-exit");
                    if (!releaseResult.Succeeded || !releaseResult.HandleUnregistered)
                    {
                        runtimeHost.UnbindContentAnchor(bindingResult.Request);
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='release' reason='Runtime handle release before Activity exit failed' release='{releaseResult.Status}' releaseUnregistered='{releaseResult.HandleUnregistered}' message='{FormatValue(releaseResult.Message)}'.");
                        return false;
                    }

                    int boundCount = runtimeHost.ContentAnchorBindingCount;
                    if (boundCount <= bindingCountBefore)
                    {
                        runtimeHost.UnbindContentAnchor(bindingResult.Request);
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bound' reason='Binding count did not increase before Activity exit' bindingCountBefore='{bindingCountBefore}' bindingCount='{boundCount}'.");
                        return false;
                    }

                    var exitResult = await ClearActivityWithResultCoreAsync(
                        runtimeHost,
                        ResolveReason(clearActivityReason, "qa.activity-content-anchor-binding.exit"));
                    if (!exitResult.Succeeded)
                    {
                        runtimeHost.UnbindContentAnchor(bindingResult.Request);
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='activity-exit' reason='Activity clear did not succeed' status='{FormatValue(exitResult.Kind.ToString())}'.");
                        return false;
                    }

                    var cleanupResult = exitResult.ActivityFlowResult.ActivityContentAnchorBindingCleanupResult;
                    if (!cleanupResult.Executed || !cleanupResult.Succeeded || cleanupResult.RemovedCount < 1)
                    {
                        runtimeHost.UnbindContentAnchor(bindingResult.Request);
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='cleanup' reason='Activity binding cleanup did not remove the test binding' cleanup='{cleanupResult.DiagnosticStatus}' removed='{cleanupResult.RemovedCount}' before='{cleanupResult.BindingCountBefore}' after='{cleanupResult.BindingCountAfter}'.");
                        return false;
                    }

                    if (runtimeHost.ContentAnchorBindingCount != bindingCountBefore)
                    {
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='cleanup' reason='Binding count did not return to baseline after Activity exit' bindingCountBefore='{bindingCountBefore}' bindingCount='{runtimeHost.ContentAnchorBindingCount}'.");
                        return false;
                    }

                    if (fixtureObject != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(fixtureObject);
#else
                        fixtureObject.SetActive(false);
                        Destroy(fixtureObject);
#endif
                        fixtureObject = null;
                    }

                    var restoreResult = await RequestActivityWithResultCoreAsync(
                        runtimeHost,
                        primaryActivity,
                        ResolveReason(activityReason, GetAssetName(primaryActivity, "qa.activity-content-anchor-binding.restore")));
                    if (!restoreResult.Succeeded)
                    {
                        _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='restore' reason='Primary Activity restore did not succeed' status='{FormatValue(restoreResult.Kind.ToString())}'.");
                        return false;
                    }

                    _logger.Info(
                        "QA Activity Content Anchor Binding Smoke step completed.",
                        LogFields.Of(
                            LogFields.Field("step", "activity-anchor-binding"),
                            LogFields.Field("activity", GetAssetName(primaryActivity, "<unnamed>")),
                            LogFields.Field("anchor", bindingResult.Anchor.StableText),
                            LogFields.Field("binding", bindingResult.Status.ToString()),
                            LogFields.Field("idempotentBinding", idempotentBindingResult.Status.ToString()),
                            LogFields.Field("release", releaseResult.Status.ToString()),
                            LogFields.Field("releaseUnregistered", releaseResult.HandleUnregistered),
                            LogFields.Field("activityCleanup", cleanupResult.DiagnosticStatus),
                            LogFields.Field("activityCleanupRemoved", cleanupResult.RemovedCount),
                            LogFields.Field("activityCleanupBefore", cleanupResult.BindingCountBefore),
                            LogFields.Field("activityCleanupAfter", cleanupResult.BindingCountAfter),
                            LogFields.Field("bindingCountBefore", bindingCountBefore),
                            LogFields.Field("bindingCountBound", boundCount),
                            LogFields.Field("bindingCount", runtimeHost.ContentAnchorBindingCount)));
                    _logger.Debug(
                        "QA Activity Content Anchor Binding Smoke diagnostics.",
                        LogFields.Field("details", bindingResult.ToDiagnosticString()));
                    return true;
                }
                finally
                {
                    if (fixtureObject != null)
                    {
#if UNITY_EDITOR
                        DestroyImmediate(fixtureObject);
#else
                        fixtureObject.SetActive(false);
                        Destroy(fixtureObject);
#endif
                    }
                }
            });
        }

        private async void RunContentAnchorBindingSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Content Anchor Binding Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Preparation Route");

            if (!TryValidateSmokeTargets("Content Anchor Binding Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Content Anchor Binding Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Content Anchor Binding Smoke step failed. step='prepare' reason='Content Anchor Binding Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.content-anchor-binding.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var anchorResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.content-anchor-binding")));
                return ValidateContentAnchorBindingSmokeStep(anchorResult, "binding", runtimeHost);
            });
        }

        private async void RunContentAnchorBindingCleanupSmoke()
        {
            string missingTargets = string.Empty;
            AppendMissingQaAsset(ref missingTargets, routeSceneCompositionRoute == null, "Content Anchor Binding Route");
            AppendMissingQaAsset(ref missingTargets, alternateRoute == null, "Alternate Route / Cleanup Exit Route");

            if (!TryValidateSmokeTargets("Content Anchor Binding Cleanup Smoke", missingTargets))
            {
                return;
            }

            await RunSmokeAsync("Content Anchor Binding Cleanup Smoke", async runtimeHost =>
            {
                if (ReferenceEquals(runtimeHost.State.CurrentRoute, routeSceneCompositionRoute))
                {
                    if (ReferenceEquals(alternateRoute, routeSceneCompositionRoute))
                    {
                        _logger.Warning("QA Content Anchor Binding Cleanup Smoke step failed. step='prepare' reason='Content Anchor Binding Route is already active and Alternate Route points to the same Route'.");
                        return false;
                    }

                    var prepareResult = await RequestRouteWithResultCoreAsync(
                        runtimeHost,
                        alternateRoute,
                        ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.content-anchor-binding-cleanup.prepare")));
                    if (!prepareResult.Succeeded)
                    {
                        _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='prepare' reason='Preparation Route request did not succeed' status='{FormatValue(prepareResult.Kind.ToString())}'.");
                        return false;
                    }

                    await Task.Yield();
                }

                var anchorResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    routeSceneCompositionRoute,
                    ResolveReason(routeReason, GetAssetName(routeSceneCompositionRoute, "qa.content-anchor-binding-cleanup.bind")));

                if (!TryCreatePersistentContentAnchorBindingForCleanupSmoke(
                        anchorResult,
                        runtimeHost,
                        out var context,
                        out var materializationIdentity,
                        out var bindingResult,
                        out int bindingCountBefore))
                {
                    return false;
                }

                var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
                var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationIdentity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding-cleanup.release-before-route-exit");
                if (!releaseResult.Succeeded || !releaseResult.HandleUnregistered)
                {
                    runtimeHost.UnbindContentAnchor(bindingResult.Request);
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='release' reason='Runtime handle release before route exit failed' release='{releaseResult.Status}' releaseUnregistered='{releaseResult.HandleUnregistered}' message='{FormatValue(releaseResult.Message)}'.");
                    return false;
                }

                int boundCount = runtimeHost.ContentAnchorBindingCount;
                if (boundCount <= bindingCountBefore)
                {
                    runtimeHost.UnbindContentAnchor(bindingResult.Request);
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bound' reason='Binding count did not increase before Route exit' bindingCountBefore='{bindingCountBefore}' bindingCount='{boundCount}'.");
                    return false;
                }

                var exitResult = await RequestRouteWithResultCoreAsync(
                    runtimeHost,
                    alternateRoute,
                    ResolveReason(routeReason, GetAssetName(alternateRoute, "qa.content-anchor-binding-cleanup.exit")));
                if (!exitResult.Succeeded)
                {
                    runtimeHost.UnbindContentAnchor(bindingResult.Request);
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='route-exit' reason='Exit Route request did not succeed' status='{FormatValue(exitResult.Kind.ToString())}'.");
                    return false;
                }

                var cleanupResult = exitResult.RouteLifecycleResult.RouteContentAnchorBindingCleanupResult;
                if (!cleanupResult.Executed || !cleanupResult.Succeeded || cleanupResult.RemovedCount < 1)
                {
                    runtimeHost.UnbindContentAnchor(bindingResult.Request);
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='cleanup' reason='Route binding cleanup did not remove the test binding' cleanup='{cleanupResult.DiagnosticStatus}' removed='{cleanupResult.RemovedCount}' before='{cleanupResult.BindingCountBefore}' after='{cleanupResult.BindingCountAfter}'.");
                    return false;
                }

                if (runtimeHost.ContentAnchorBindingCount != bindingCountBefore)
                {
                    _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='cleanup' reason='Binding count did not return to baseline after Route exit' bindingCountBefore='{bindingCountBefore}' bindingCount='{runtimeHost.ContentAnchorBindingCount}'.");
                    return false;
                }

                _logger.Info(
                    "QA Content Anchor Binding Cleanup Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "route-exit-cleanup"),
                        LogFields.Field("binding", bindingResult.Status.ToString()),
                        LogFields.Field("release", releaseResult.Status.ToString()),
                        LogFields.Field("releaseUnregistered", releaseResult.HandleUnregistered),
                        LogFields.Field("routeCleanup", cleanupResult.DiagnosticStatus),
                        LogFields.Field("routeCleanupRemoved", cleanupResult.RemovedCount),
                        LogFields.Field("routeCleanupBefore", cleanupResult.BindingCountBefore),
                        LogFields.Field("routeCleanupAfter", cleanupResult.BindingCountAfter),
                        LogFields.Field("bindingCountBefore", bindingCountBefore),
                        LogFields.Field("bindingCountBound", boundCount),
                        LogFields.Field("bindingCount", runtimeHost.ContentAnchorBindingCount)));
                return true;
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
            return result.Succeeded || allowAlreadyActive && result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive;
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
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
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
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
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
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");

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
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
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
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
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

        private bool TryCreatePersistentContentAnchorBindingForCleanupSmoke(
            FrameworkRouteRequestResult result,
            FrameworkRuntimeHost runtimeHost,
            out RuntimeScopeContext context,
            out RuntimeContentIdentity materializationIdentity,
            out ContentAnchorBindingResult bindingResult,
            out int bindingCountBefore)
        {
            context = default(RuntimeScopeContext);
            materializationIdentity = default(RuntimeContentIdentity);
            bindingResult = default(ContentAnchorBindingResult);
            bindingCountBefore = 0;

            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            if (!result.RouteLifecycleResult.Started)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var anchorSet = result.RouteLifecycleResult.ContentAnchorSet;
            if (!anchorSet.HasAnchors)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Content Anchor Set has no anchors'.");
                return false;
            }

            var runtimeScopeResult = result.RouteLifecycleResult.RuntimeRouteScopeResult;
            if (!runtimeScopeResult.HasContext)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Route runtime scope context unavailable' runtimeRouteScope='{FormatValue(runtimeScopeResult.DiagnosticStatus)}' runtimeRouteContext='{FormatValue(runtimeScopeResult.ContextStatus)}'.");
                return false;
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            context = runtimeScopeResult.Context;
            var anchor = anchorSet.Declarations[0];
            var contentId = RuntimeContentId.From("qa.content-anchor.binding-cleanup." + Guid.NewGuid().ToString("N"));
            var resource = RuntimeMaterializationResource.From(
                "QA.ContentAnchorBindingCleanup",
                "qa.content-anchor.binding-cleanup.resource",
                "QA Content Anchor Binding Cleanup Smoke Resource",
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    context,
                    contentId,
                    resource,
                    QaSource,
                    "qa.content-anchor-binding-cleanup.materialization.request",
                    out var materializationRequest,
                    out var materializationGuardResult))
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Materialization request rejected' guard='{materializationGuardResult.Status}' message='{FormatValue(materializationGuardResult.Message)}'.");
                return false;
            }

            var runtimeHandle = RuntimeContentHandle.Declared(
                materializationRequest.Identity,
                QaSource,
                "qa.content-anchor-binding-cleanup.handle.declared");
            var materializationResult = RuntimeMaterializationResult.Success(
                materializationRequest,
                runtimeHandle,
                QaSource,
                "qa.content-anchor-binding-cleanup.materialization.synthetic-result",
                "Synthetic Content Anchor binding cleanup smoke materialization result.");
            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                QaSource,
                "qa.content-anchor-binding-cleanup.materialization.apply");

            if (!appliedMaterializationResult.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Materialization result was not applied' materialization='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                return false;
            }

            var bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                context,
                anchor,
                contentId,
                resource,
                QaSource,
                "qa.content-anchor-binding-cleanup.request");
            bindingCountBefore = runtimeHost.ContentAnchorBindingCount;
            bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.content-anchor-binding-cleanup.bind");

            if (!bindingResult.Succeeded)
            {
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding-cleanup.cleanup.binding-failed");
                _logger.Warning($"QA Content Anchor Binding Cleanup Smoke step failed. step='bind' route='{FormatValue(routeName)}' reason='Logical binding failed' binding='{bindingResult.Status}' message='{FormatValue(bindingResult.Message)}'.");
                return false;
            }

            materializationIdentity = materializationRequest.Identity;
            return true;
        }

        private bool ValidateContentAnchorBindingSmokeStep(
            FrameworkRouteRequestResult result,
            string stepName,
            FrameworkRuntimeHost runtimeHost)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (runtimeHost == null || runtimeHost.RuntimeContentRuntime == null)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var lifecycleResult = result.RouteLifecycleResult;
            if (!lifecycleResult.Started)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route lifecycle did not start'.");
                return false;
            }

            var anchorSet = lifecycleResult.ContentAnchorSet;
            if (!anchorSet.HasAnchors || anchorSet.Declarations.Count == 0)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Content Anchor Set has no anchors'.");
                return false;
            }

            var runtimeScopeResult = lifecycleResult.RuntimeRouteScopeResult;
            if (!runtimeScopeResult.HasContext)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route runtime scope context unavailable' runtimeRouteScope='{FormatValue(runtimeScopeResult.DiagnosticStatus)}' runtimeRouteContext='{FormatValue(runtimeScopeResult.ContextStatus)}'.");
                return false;
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            var context = runtimeScopeResult.Context;
            var anchor = anchorSet.Declarations[0];
            var contentId = RuntimeContentId.From("qa.content-anchor.binding." + Guid.NewGuid().ToString("N"));
            var resource = RuntimeMaterializationResource.From(
                "QA.ContentAnchorBinding",
                "qa.content-anchor.binding.resource",
                "QA Content Anchor Binding Smoke Resource",
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    context,
                    contentId,
                    resource,
                    QaSource,
                    "qa.content-anchor-binding.materialization.request",
                    out var materializationRequest,
                    out var materializationGuardResult))
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Materialization request rejected' guard='{materializationGuardResult.Status}' message='{FormatValue(materializationGuardResult.Message)}'.");
                return false;
            }

            var runtimeHandle = RuntimeContentHandle.Declared(
                materializationRequest.Identity,
                QaSource,
                "qa.content-anchor-binding.handle.declared");
            var materializationResult = RuntimeMaterializationResult.Success(
                materializationRequest,
                runtimeHandle,
                QaSource,
                "qa.content-anchor-binding.materialization.synthetic-result",
                "Synthetic Content Anchor binding smoke materialization result.");
            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                QaSource,
                "qa.content-anchor-binding.materialization.apply");

            if (!appliedMaterializationResult.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Materialization result was not applied' materialization='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                return false;
            }

            var bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                context,
                anchor,
                contentId,
                resource,
                QaSource,
                "qa.content-anchor-binding.request");
            int bindingCountBefore = runtimeHost.ContentAnchorBindingCount;
            var bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.content-anchor-binding.bind");

            if (!bindingResult.Succeeded)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Logical binding failed' binding='{bindingResult.Status}' message='{FormatValue(bindingResult.Message)}'.");
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding.cleanup.binding-failed");
                return false;
            }

            var idempotentBindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.content-anchor-binding.bind.idempotent");

            if (idempotentBindingResult.Status != ContentAnchorBindingStatus.SucceededAlreadyBound)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Idempotent binding did not return already-bound' binding='{idempotentBindingResult.Status}' message='{FormatValue(idempotentBindingResult.Message)}'.");
                runtimeHost.UnbindContentAnchor(bindingRequest);
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding.cleanup.idempotent-failed");
                return false;
            }

            bool unbound = runtimeHost.UnbindContentAnchor(bindingRequest);
            if (!unbound || runtimeHost.ContentAnchorBindingCount != bindingCountBefore)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Logical binding was not removed from host runtime' unbound='{unbound}' bindingCountBefore='{bindingCountBefore}' bindingCount='{runtimeHost.ContentAnchorBindingCount}'.");
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.content-anchor-binding.cleanup.unbind-failed");
                return false;
            }

            var releaseResult = runtimeContentRuntime.ReleaseHandleLogically(
                context,
                materializationRequest.Identity,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                QaSource,
                "qa.content-anchor-binding.release.logical");

            if (!releaseResult.Succeeded || !releaseResult.HandleUnregistered)
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Runtime handle release failed' release='{releaseResult.Status}' releaseUnregistered='{releaseResult.HandleUnregistered}' message='{FormatValue(releaseResult.Message)}'.");
                return false;
            }

            if (runtimeContentRuntime.TryGetHandle(context, materializationRequest.Identity, out _))
            {
                _logger.Warning($"QA Content Anchor Binding Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Runtime handle still registered after release/unregister'.");
                return false;
            }

            _logger.Info(
                "QA Content Anchor Binding Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("anchor", anchor.StableText),
                    LogFields.Field("binding", bindingResult.Status.ToString()),
                    LogFields.Field("idempotentBinding", idempotentBindingResult.Status.ToString()),
                    LogFields.Field("unbound", unbound),
                    LogFields.Field("release", releaseResult.Status.ToString()),
                    LogFields.Field("releaseUnregistered", releaseResult.HandleUnregistered),
                    LogFields.Field("runtimeRootCount", runtimeContentRuntime.RootCount),
                    LogFields.Field("bindingCountBefore", bindingCountBefore),
                    LogFields.Field("bindingCount", runtimeHost.ContentAnchorBindingCount)));
            _logger.Debug(
                "QA Content Anchor Binding Smoke diagnostics.",
                LogFields.Field("details", bindingResult.ToDiagnosticString()));
            return true;
        }

        private bool TryCreateActivityContentAnchorBindingForCleanupSmoke(
            FrameworkActivityRequestResult result,
            FrameworkRuntimeHost runtimeHost,
            out RuntimeScopeContext context,
            out RuntimeContentIdentity materializationIdentity,
            out ContentAnchorBindingResult bindingResult,
            out ContentAnchorBindingResult idempotentBindingResult,
            out int bindingCountBefore)
        {
            context = default(RuntimeScopeContext);
            materializationIdentity = default(RuntimeContentIdentity);
            bindingResult = default(ContentAnchorBindingResult);
            idempotentBindingResult = default(ContentAnchorBindingResult);
            bindingCountBefore = 0;

            string activityName = result.TargetActivity != null ? GetAssetName(result.TargetActivity, "<unnamed>") : "<missing>";

            if (runtimeHost == null || runtimeHost.RuntimeContentRuntime == null)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='RuntimeContentRuntime unavailable'.");
                return false;
            }

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlow = result.ActivityFlowResult;
            if (!activityFlow.Completed)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity flow did not complete'.");
                return false;
            }

            var anchorSet = activityFlow.ActivityContentAnchorSet;
            if (!anchorSet.HasAnchors || anchorSet.Declarations.Count == 0)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity Content Anchor Set has no anchors'.");
                return false;
            }

            var runtimeScopeResult = activityFlow.RuntimeActivityScopeResult;
            if (!runtimeScopeResult.HasContext)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity runtime scope context unavailable' runtimeActivityScope='{FormatValue(runtimeScopeResult.DiagnosticStatus)}' runtimeActivityContext='{FormatValue(runtimeScopeResult.ContextStatus)}'.");
                return false;
            }

            var discovery = activityFlow.ActivityContentAnchorDiscoveryResult;
            if (discovery.IssueCount != 0 || discovery.InvalidAuthoringCount != 0 || discovery.SkippedActivityMismatchCount != 0)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Activity Content Anchor discovery produced issues' issues='{discovery.IssueCount}' invalid='{discovery.InvalidAuthoringCount}' mismatch='{discovery.SkippedActivityMismatchCount}'.");
                return false;
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            context = runtimeScopeResult.Context;
            var anchor = anchorSet.Declarations[0];
            var contentId = RuntimeContentId.From("qa.activity-content-anchor.binding." + Guid.NewGuid().ToString("N"));
            var resource = RuntimeMaterializationResource.From(
                "QA.ActivityContentAnchorBinding",
                "qa.activity-content-anchor.binding.resource",
                "QA Activity Content Anchor Binding Smoke Resource",
                string.Empty);

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    context,
                    contentId,
                    resource,
                    QaSource,
                    "qa.activity-content-anchor-binding.materialization.request",
                    out var materializationRequest,
                    out var materializationGuardResult))
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Materialization request rejected' guard='{materializationGuardResult.Status}' message='{FormatValue(materializationGuardResult.Message)}'.");
                return false;
            }

            var runtimeHandle = RuntimeContentHandle.Declared(
                materializationRequest.Identity,
                QaSource,
                "qa.activity-content-anchor-binding.handle.declared");
            var materializationResult = RuntimeMaterializationResult.Success(
                materializationRequest,
                runtimeHandle,
                QaSource,
                "qa.activity-content-anchor-binding.materialization.synthetic-result",
                "Synthetic Activity Content Anchor binding smoke materialization result.");
            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                QaSource,
                "qa.activity-content-anchor-binding.materialization.apply");

            if (!appliedMaterializationResult.Succeeded)
            {
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Materialization result was not applied' materialization='{appliedMaterializationResult.Status}' message='{FormatValue(appliedMaterializationResult.Message)}'.");
                return false;
            }

            var bindingRequest = ContentAnchorBindingRequest.FromDeclaration(
                context,
                anchor,
                contentId,
                resource,
                QaSource,
                "qa.activity-content-anchor-binding.request");
            bindingCountBefore = runtimeHost.ContentAnchorBindingCount;
            bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.activity-content-anchor-binding.bind");

            if (!bindingResult.Succeeded)
            {
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.activity-content-anchor-binding.cleanup.binding-failed");
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Logical binding failed' binding='{bindingResult.Status}' message='{FormatValue(bindingResult.Message)}'.");
                return false;
            }

            idempotentBindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                QaSource,
                "qa.activity-content-anchor-binding.bind.idempotent");

            if (idempotentBindingResult.Status != ContentAnchorBindingStatus.SucceededAlreadyBound)
            {
                runtimeHost.UnbindContentAnchor(bindingRequest);
                runtimeContentRuntime.ReleaseHandleLogically(
                    context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    QaSource,
                    "qa.activity-content-anchor-binding.cleanup.idempotent-failed");
                _logger.Warning($"QA Activity Content Anchor Binding Smoke step failed. step='bind' activity='{FormatValue(activityName)}' reason='Idempotent binding did not return already-bound' binding='{idempotentBindingResult.Status}' message='{FormatValue(idempotentBindingResult.Message)}'.");
                return false;
            }

            materializationIdentity = materializationRequest.Identity;
            return true;
        }

        private bool TryCreatePositiveActivityContentAnchorFixture(
            ActivityAsset activity,
            out GameObject fixtureObject,
            out ActivityContentAnchor fixtureAnchor)
        {
            fixtureObject = null;
            fixtureAnchor = null;

            if (activity == null)
            {
                _logger.Warning("QA Activity Content Anchor Positive Smoke step failed. step='fixture' reason='Primary Activity is missing'.");
                return false;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                _logger.Warning("QA Activity Content Anchor Positive Smoke step failed. step='fixture' reason='Active Scene is invalid or not loaded'.");
                return false;
            }

            string anchorId = ResolveReason(positiveActivityContentAnchorId, "qa.activity.anchor.positive");
            var anchorKind = positiveActivityContentAnchorKind == ContentAnchorKind.Unknown
                ? ContentAnchorKind.Slot
                : positiveActivityContentAnchorKind;
            var requiredness = Enum.IsDefined(typeof(ContentAnchorRequiredness), positiveActivityContentAnchorRequiredness)
                ? positiveActivityContentAnchorRequiredness
                : ContentAnchorRequiredness.Optional;

            fixtureObject = new GameObject("QA Activity Content Anchor Positive Fixture");
            SceneManager.MoveGameObjectToScene(fixtureObject, activeScene);
            fixtureAnchor = fixtureObject.AddComponent<ActivityContentAnchor>();
            fixtureAnchor.ConfigureForDiagnostics(
                activity,
                anchorId,
                anchorKind,
                requiredness,
                "QA Activity Content Anchor Positive Fixture",
                "Temporary QA fixture created by FrameworkQaCanvas for F9H positive-path discovery smoke.");
            return true;
        }

        private bool ValidateActivityContentAnchorPositiveSmokeStep(
            FrameworkActivityRequestResult result,
            string stepName,
            ActivityContentAnchor fixtureAnchor)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string activityName = result.TargetActivity != null ? GetAssetName(result.TargetActivity, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Activity request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlow = result.ActivityFlowResult;
            if (!activityFlow.Completed)
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Activity flow did not complete'.");
                return false;
            }

            if (fixtureAnchor == null || !fixtureAnchor.TryCreateDeclaration(out var declaration))
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Positive fixture declaration is invalid'.");
                return false;
            }

            var discovery = activityFlow.ActivityContentAnchorDiscoveryResult;
            var anchorSet = activityFlow.ActivityContentAnchorSet;
            if (discovery.AnchorCount < 1 || discovery.CandidateCount < 1 || discovery.AcceptedCount < 1)
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Positive Activity Content Anchor was not discovered' anchors='{discovery.AnchorCount}' candidates='{discovery.CandidateCount}' accepted='{discovery.AcceptedCount}'.");
                return false;
            }

            if (!anchorSet.Contains(declaration))
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Discovered Activity Content Anchor Set does not contain the positive fixture declaration' anchor='{FormatValue(declaration.StableText)}'.");
                return false;
            }

            if (discovery.IssueCount != 0 || discovery.InvalidAuthoringCount != 0 || discovery.SkippedActivityMismatchCount != 0)
            {
                _logger.Warning($"QA Activity Content Anchor Positive Smoke step failed. step='{FormatValue(normalizedStepName)}' activity='{FormatValue(activityName)}' reason='Positive discovery produced issues' issues='{discovery.IssueCount}' invalid='{discovery.InvalidAuthoringCount}' mismatch='{discovery.SkippedActivityMismatchCount}'.");
                return false;
            }

            _logger.Info(
                "QA Activity Content Anchor Positive Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("activity", activityName),
                    LogFields.Field("anchor", declaration.StableText),
                    LogFields.Field("activityContentAnchors", discovery.AnchorCount),
                    LogFields.Field("activityContentAnchorCandidates", discovery.CandidateCount),
                    LogFields.Field("activityContentAnchorAccepted", discovery.AcceptedCount),
                    LogFields.Field("activityContentAnchorRequired", anchorSet.RequiredCount),
                    LogFields.Field("activityContentAnchorOptional", anchorSet.OptionalCount),
                    LogFields.Field("activityContentAnchorRoot", anchorSet.RootCount),
                    LogFields.Field("activityContentAnchorSlot", anchorSet.SlotCount),
                    LogFields.Field("activityContentAnchorPoint", anchorSet.PointCount),
                    LogFields.Field("activityContentAnchorIssues", discovery.IssueCount),
                    LogFields.Field("activityContentAnchorInvalid", discovery.InvalidAuthoringCount),
                    LogFields.Field("activityContentAnchorActivityMismatch", discovery.SkippedActivityMismatchCount)));
            _logger.Debug(
                "QA Activity Content Anchor Positive Smoke diagnostics.",
                LogFields.Field("details", discovery.ToDiagnosticString()));
            return true;
        }

        private bool ValidateActivityContentAnchorDiagnosticsSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
            string routeName = result.TargetRoute != null ? GetAssetName(result.TargetRoute, "<unnamed>") : "<missing>";

            if (!result.Succeeded)
            {
                _logger.Warning($"QA Activity Content Anchor Diagnostics Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Route request did not succeed' status='{FormatValue(result.Kind.ToString())}'.");
                return false;
            }

            var activityFlow = result.RouteLifecycleResult.ActivityFlowResult;
            if (!activityFlow.Completed)
            {
                _logger.Warning($"QA Activity Content Anchor Diagnostics Smoke step failed. step='{FormatValue(normalizedStepName)}' route='{FormatValue(routeName)}' reason='Activity flow did not complete'.");
                return false;
            }

            var discovery = activityFlow.ActivityContentAnchorDiscoveryResult;
            var anchorSet = activityFlow.ActivityContentAnchorSet;
            if (!ValidateMinimum("activityContentAnchors", Math.Max(0, expectedActivityContentAnchorCount), discovery.AnchorCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateMinimum("activityContentAnchorCandidates", Math.Max(0, expectedActivityContentAnchorCandidateCount), discovery.CandidateCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("activityContentAnchorIssues", Math.Max(0, expectedActivityContentAnchorIssueCount), discovery.IssueCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("activityContentAnchorInvalid", Math.Max(0, expectedActivityContentAnchorInvalidCount), discovery.InvalidAuthoringCount, normalizedStepName, routeName))
            {
                return false;
            }

            if (!ValidateExact("activityContentAnchorActivityMismatch", Math.Max(0, expectedActivityContentAnchorActivityMismatchCount), discovery.SkippedActivityMismatchCount, normalizedStepName, routeName))
            {
                return false;
            }

            _logger.Info(
                "QA Activity Content Anchor Diagnostics Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", normalizedStepName),
                    LogFields.Field("route", routeName),
                    LogFields.Field("activity", GetAssetName(activityFlow.Activity, "<none>")),
                    LogFields.Field("activityContentAnchors", discovery.AnchorCount),
                    LogFields.Field("activityContentAnchorCandidates", discovery.CandidateCount),
                    LogFields.Field("activityContentAnchorAccepted", discovery.AcceptedCount),
                    LogFields.Field("activityContentAnchorRequired", anchorSet.RequiredCount),
                    LogFields.Field("activityContentAnchorOptional", anchorSet.OptionalCount),
                    LogFields.Field("activityContentAnchorRoot", anchorSet.RootCount),
                    LogFields.Field("activityContentAnchorSlot", anchorSet.SlotCount),
                    LogFields.Field("activityContentAnchorPoint", anchorSet.PointCount),
                    LogFields.Field("activityContentAnchorIssues", discovery.IssueCount),
                    LogFields.Field("activityContentAnchorInvalid", discovery.InvalidAuthoringCount),
                    LogFields.Field("activityContentAnchorActivityMismatch", discovery.SkippedActivityMismatchCount),
                    LogFields.Field("activityContentAnchorDuplicateIdentity", anchorSet.DuplicateIdentityIssueCount),
                    LogFields.Field("activityContentAnchorDuplicateId", anchorSet.DuplicateAnchorIdIssueCount)));
            _logger.Debug(
                "QA Activity Content Anchor Diagnostics Smoke diagnostics.",
                LogFields.Field("details", discovery.ToDiagnosticString()));
            return true;
        }

        private bool ValidateContentAnchorDiagnosticsSmokeStep(FrameworkRouteRequestResult result, string stepName)
        {
            string normalizedStepName = stepName.NormalizeTextOrFallback("<unknown>");
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
            ActivityContentAnchor[] activityContentAnchors = FindObjectsByType<ActivityContentAnchor>(
                FindObjectsInactive.Include);
            RouteCycleResetTrigger[] routeCycleResetTriggers = FindObjectsByType<RouteCycleResetTrigger>(
                FindObjectsInactive.Include);
            ActivityCycleResetTrigger[] activityCycleResetTriggers = FindObjectsByType<ActivityCycleResetTrigger>(
                FindObjectsInactive.Include);
            ObjectEntryDeclaration[] objectEntryDeclarations = FindObjectsByType<ObjectEntryDeclaration>(
                FindObjectsInactive.Include);
            ObjectResetTrigger[] objectResetTriggers = FindObjectsByType<ObjectResetTrigger>(
                FindObjectsInactive.Include);

            int routeBindingCount = routeBindings?.Length ?? 0;
            int activityAdapterCount = activityAdapters?.Length ?? 0;
            int routeContentAnchorCount = routeContentAnchors?.Length ?? 0;
            int activityContentAnchorCount = activityContentAnchors?.Length ?? 0;
            int routeCycleResetTriggerCount = routeCycleResetTriggers?.Length ?? 0;
            int activityCycleResetTriggerCount = activityCycleResetTriggers?.Length ?? 0;
            int objectEntryDeclarationCount = objectEntryDeclarations?.Length ?? 0;
            int objectResetTriggerCount = objectResetTriggers?.Length ?? 0;

            if (routeBindingCount == 0
                && activityAdapterCount == 0
                && routeContentAnchorCount == 0
                && activityContentAnchorCount == 0
                && routeCycleResetTriggerCount == 0
                && activityCycleResetTriggerCount == 0
                && objectEntryDeclarationCount == 0
                && objectResetTriggerCount == 0)
            {
                _logger.Warning("QA Authoring Validation completed. scope='Loaded Authoring' routeBindings='0' activityAdapters='0' routeContentAnchors='0' activityContentAnchors='0' routeCycleResetTriggers='0' activityCycleResetTriggers='0' objectEntryDeclarations='0' objectResetTriggers='0' localContributions='0' contentAnchors='0' objectEntries='0' issues='1' reason='No framework authoring components found in loaded scenes'.");
                return;
            }

            int issueCount = 0;
            int errorCount = 0;
            int warningCount = 0;
            var contentAnchorDeclarations = new List<ContentAnchorDeclaration>(Math.Max(0, routeContentAnchorCount + activityContentAnchorCount));
            var objectEntryDescriptors = new List<ObjectEntryDescriptor>(Math.Max(0, objectEntryDeclarationCount));

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

            if (activityContentAnchors != null)
            {
                for (int i = 0; i < activityContentAnchors.Length; i++)
                {
                    ValidateLoadedActivityContentAnchor(
                        activityContentAnchors[i],
                        contentAnchorDeclarations,
                        ref issueCount,
                        ref errorCount,
                        ref warningCount);
                }
            }

            ValidateLoadedCycleResetTriggers(
                routeCycleResetTriggers,
                activityCycleResetTriggers,
                ref issueCount,
                ref errorCount,
                ref warningCount);

            if (objectEntryDeclarations != null)
            {
                for (int i = 0; i < objectEntryDeclarations.Length; i++)
                {
                    ValidateLoadedObjectEntryDeclaration(
                        objectEntryDeclarations[i],
                        objectEntryDescriptors,
                        ref issueCount,
                        ref errorCount,
                        ref warningCount);
                }
            }

            ValidateLoadedObjectResetTriggers(
                objectResetTriggers,
                objectEntryDescriptors,
                ref issueCount,
                ref errorCount,
                ref warningCount);

            var objectEntrySet = CreateLoadedObjectEntrySet(
                objectEntryDescriptors,
                ref issueCount,
                ref errorCount);

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
                        LogFields.Field("activityContentAnchors", activityContentAnchorCount),
                        LogFields.Field("routeCycleResetTriggers", routeCycleResetTriggerCount),
                        LogFields.Field("activityCycleResetTriggers", activityCycleResetTriggerCount),
                        LogFields.Field("objectEntryDeclarations", objectEntryDeclarationCount),
                        LogFields.Field("objectResetTriggers", objectResetTriggerCount),
                        LogFields.Field("issues", 0),
                        LogFields.Field("localContributions", validationResult.ContributionCount),
                        LogFields.Field("blockingIssues", validationResult.BlockingIssueCount),
                        LogFields.Field("optionalSkips", validationResult.OptionalSkipCount),
                        LogFields.Field("contentAnchors", contentAnchorSet.Count),
                        LogFields.Field("contentAnchorIssues", contentAnchorSet.IssueCount),
                        LogFields.Field("contentAnchorDuplicateIdentity", contentAnchorSet.DuplicateIdentityIssueCount),
                        LogFields.Field("contentAnchorDuplicateId", contentAnchorSet.DuplicateAnchorIdIssueCount),
                        LogFields.Field("objectEntries", objectEntrySet.Count),
                        LogFields.Field("objectEntryRequired", objectEntrySet.RequiredCount),
                        LogFields.Field("objectEntryOptional", objectEntrySet.OptionalCount),
                        LogFields.Field("objectEntryRoute", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Route)),
                        LogFields.Field("objectEntryActivity", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Activity)),
                        LogFields.Field("objectEntrySession", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Session))));
                _logger.Debug(
                    "QA Authoring Validation diagnostics.",
                    LogFields.Of(
                        LogFields.Field("localContributions", validationResult.ToDiagnosticString()),
                        LogFields.Field("contentAnchors", contentAnchorSet.ToDiagnosticString()),
                        LogFields.Field("objectEntries", BuildObjectEntryDiagnostics(objectEntrySet))));
                return;
            }

            _logger.Warning(
                "QA Authoring Validation completed with issues.",
                LogFields.Of(
                    LogFields.Field("scope", "Loaded Authoring"),
                    LogFields.Field("routeBindings", routeBindingCount),
                    LogFields.Field("activityAdapters", activityAdapterCount),
                    LogFields.Field("routeContentAnchors", routeContentAnchorCount),
                    LogFields.Field("activityContentAnchors", activityContentAnchorCount),
                    LogFields.Field("routeCycleResetTriggers", routeCycleResetTriggerCount),
                    LogFields.Field("activityCycleResetTriggers", activityCycleResetTriggerCount),
                    LogFields.Field("objectEntryDeclarations", objectEntryDeclarationCount),
                    LogFields.Field("objectResetTriggers", objectResetTriggerCount),
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
                    LogFields.Field("objectEntries", objectEntrySet.Count),
                    LogFields.Field("objectEntryRequired", objectEntrySet.RequiredCount),
                    LogFields.Field("objectEntryOptional", objectEntrySet.OptionalCount),
                    LogFields.Field("objectEntryRoute", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Route)),
                    LogFields.Field("objectEntryActivity", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Activity)),
                    LogFields.Field("objectEntrySession", CountObjectEntriesByScope(objectEntrySet, ObjectEntryScope.Session)),
                    LogFields.Field("localContributionDetails", validationResult.ToDiagnosticString()),
                    LogFields.Field("contentAnchorDetails", contentAnchorSet.ToDiagnosticString()),
                    LogFields.Field("objectEntryDetails", BuildObjectEntryDiagnostics(objectEntrySet))));
        }

        private void ValidateLoadedCycleResetTriggers(
            RouteCycleResetTrigger[] routeTriggers,
            ActivityCycleResetTrigger[] activityTriggers,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            var sharedObjects = new HashSet<GameObject>();

            if (routeTriggers != null)
            {
                for (int i = 0; i < routeTriggers.Length; i++)
                {
                    ValidateLoadedCycleResetTriggerCommon(routeTriggers[i], nameof(RouteCycleResetTrigger), ref issueCount, ref warningCount);

                    var trigger = routeTriggers[i];
                    if (trigger != null && trigger.gameObject != null && trigger.GetComponent<ActivityCycleResetTrigger>() != null)
                    {
                        if (sharedObjects.Add(trigger.gameObject))
                        {
                            string objectName = trigger.gameObject.name;
                            string sceneName = trigger.gameObject.scene.IsValid() ? trigger.gameObject.scene.name : "<no-scene>";
                            AddQaAuthoringWarning(
                                ref issueCount,
                                ref warningCount,
                                $"CycleResetTrigger object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Route and Activity Cycle Reset Triggers share the same GameObject'.");
                        }
                    }
                }
            }

            if (activityTriggers != null)
            {
                for (int i = 0; i < activityTriggers.Length; i++)
                {
                    ValidateLoadedCycleResetTriggerCommon(activityTriggers[i], nameof(ActivityCycleResetTrigger), ref issueCount, ref warningCount);
                }
            }
        }

        private void ValidateLoadedCycleResetTriggerCommon(
            MonoBehaviour trigger,
            string triggerType,
            ref int issueCount,
            ref int warningCount)
        {
            if (trigger == null)
            {
                AddQaAuthoringWarning(ref issueCount, ref warningCount, $"{triggerType} is missing.");
                return;
            }

            string objectName = trigger.gameObject != null ? trigger.gameObject.name : "<missing>";
            string sceneName = trigger.gameObject != null && trigger.gameObject.scene.IsValid()
                ? trigger.gameObject.scene.name
                : "<no-scene>";

            if (trigger.gameObject != null && !trigger.gameObject.activeInHierarchy)
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"{triggerType} object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Trigger GameObject is inactive in hierarchy'.");
            }

            string reason = string.Empty;
            if (trigger is RouteCycleResetTrigger { HasCustomReason: true } routeTrigger)
            {
                reason = routeTrigger.AuthoringReason;
            }
            else if (trigger is ActivityCycleResetTrigger { HasCustomReason: true } activityTrigger)
            {
                reason = activityTrigger.AuthoringReason;
            }

            if (!string.IsNullOrWhiteSpace(reason) && ContainsFutureResetVocabulary(reason))
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"{triggerType} object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' reason='{FormatValue(reason)}' issue='Reason uses object/component/player/actor/pool/save/reload vocabulary reserved for later reset phases'.");
            }
        }

        private void ValidateLoadedObjectResetTriggers(
            ObjectResetTrigger[] objectResetTriggers,
            List<ObjectEntryDescriptor> objectEntryDescriptors,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (objectResetTriggers == null)
            {
                return;
            }

            for (int i = 0; i < objectResetTriggers.Length; i++)
            {
                var trigger = objectResetTriggers[i];
                if (trigger == null)
                {
                    AddQaAuthoringIssue(
                        ref issueCount,
                        ref errorCount,
                        "ObjectResetTrigger is missing.");
                    continue;
                }

                string objectName = trigger.gameObject != null ? trigger.gameObject.name : "<missing>";
                string sceneName = trigger.gameObject != null && trigger.gameObject.scene.IsValid()
                    ? trigger.gameObject.scene.name
                    : "<no-scene>";

                if (trigger.gameObject != null && !trigger.gameObject.activeInHierarchy)
                {
                    AddQaAuthoringWarning(
                        ref issueCount,
                        ref warningCount,
                        $"ObjectResetTrigger object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Trigger GameObject is inactive in hierarchy'.");
                }

                string targetId = trigger.ResolvedAuthoringObjectEntryId;
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    AddQaAuthoringIssue(
                        ref issueCount,
                        ref errorCount,
                        $"ObjectResetTrigger object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Target Object Entry Id is missing'.");
                    continue;
                }

                bool targetDeclared = false;
                if (objectEntryDescriptors != null)
                {
                    for (int descriptorIndex = 0; descriptorIndex < objectEntryDescriptors.Count; descriptorIndex++)
                    {
                        var descriptor = objectEntryDescriptors[descriptorIndex];
                        if (descriptor.Id.IsValid
                            && string.Equals(descriptor.Id.Value.Value, targetId.Trim(), StringComparison.Ordinal))
                        {
                            targetDeclared = true;
                            break;
                        }
                    }
                }

                if (!targetDeclared)
                {
                    AddQaAuthoringWarning(
                        ref issueCount,
                        ref warningCount,
                        $"ObjectResetTrigger object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' objectEntry='{FormatValue(targetId)}' issue='Target Object Entry Id is not declared in loaded Object Entry authoring'.");
                }
            }
        }

        private void ValidateLoadedObjectEntryDeclaration(
            ObjectEntryDeclaration declaration,
            List<ObjectEntryDescriptor> descriptors,
            ref int issueCount,
            ref int errorCount,
            ref int warningCount)
        {
            if (declaration == null)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    "ObjectEntryDeclaration is missing.");
                return;
            }

            string objectName = declaration.gameObject != null ? declaration.gameObject.name : "<missing>";
            string sceneName = declaration.gameObject != null && declaration.gameObject.scene.IsValid()
                ? declaration.gameObject.scene.name
                : "<no-scene>";

            if (declaration.gameObject != null && !declaration.gameObject.activeInHierarchy)
            {
                AddQaAuthoringWarning(
                    ref issueCount,
                    ref warningCount,
                    $"ObjectEntryDeclaration object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Declaration GameObject is inactive in hierarchy'.");
            }

            if (!declaration.HasRequiredAuthoredOwner)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ObjectEntryDeclaration object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' scope='{declaration.Scope}' issue='Explicit owner is required for Route/Activity Object Entry'.");
                return;
            }

            if (!declaration.TryCreateDescriptor(out var descriptor, out string issue))
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ObjectEntryDeclaration object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='{FormatValue(issue)}'.");
                return;
            }

            descriptors.Add(descriptor);
        }

        private ObjectEntrySet CreateLoadedObjectEntrySet(
            List<ObjectEntryDescriptor> descriptors,
            ref int issueCount,
            ref int errorCount)
        {
            try
            {
                return new ObjectEntrySet(descriptors);
            }
            catch (ArgumentException exception)
            {
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ObjectEntrySet issue='Duplicate Object Entry Id' message='{FormatValue(exception.Message)}'.");
                return ObjectEntrySet.Empty();
            }
        }

        private static int CountObjectEntriesByScope(ObjectEntrySet set, ObjectEntryScope scope)
        {
            if (set == null || set.IsEmpty)
            {
                return 0;
            }

            IReadOnlyList<ObjectEntryDescriptor> entries = set.Entries;
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Scope == scope)
                {
                    count++;
                }
            }

            return count;
        }

        private static string BuildObjectEntryDiagnostics(ObjectEntrySet set)
        {
            if (set == null || set.IsEmpty)
            {
                return "objectEntries='0'";
            }

            IReadOnlyList<ObjectEntryDescriptor> entries = set.Entries;
            var details = new List<string>(entries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                details.Add($"id='{entry.Id.StableText}' scope='{entry.Scope}' sourceKind='{entry.SourceKind}' requiredness='{entry.Requiredness}' displayName='{entry.DisplayName}'");
            }

            return $"{set.Summary} details=[{string.Join(" | ", details)}]";
        }

        private static bool ContainsFutureResetVocabulary(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string[] reservedTerms =
            {
                "object",
                "component",
                "player",
                "actor",
                "transform",
                "rigidbody",
                "animator",
                "pool",
                "snapshot",
                "save",
                "reload",
                "checkpoint"
            };

            for (int i = 0; i < reservedTerms.Length; i++)
            {
                if (value.IndexOf(reservedTerms[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
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

            IReadOnlyList<LocalContributionValidationIssue> issues = validationResult.Issues;
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

        private void ValidateLoadedActivityContentAnchor(
            ActivityContentAnchor anchor,
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
                    "ActivityContentAnchor is missing.");
                return;
            }

            string objectName = anchor.gameObject != null ? anchor.gameObject.name : "<missing>";
            string sceneName = anchor.gameObject != null && anchor.gameObject.scene.IsValid()
                ? anchor.gameObject.scene.name
                : "<no-scene>";
            bool hasBlockingAuthoringIssue = false;

            if (anchor.Activity == null)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Activity'.");
            }

            if (!anchor.HasExplicitAnchorId)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Missing Anchor Id'.");
            }

            if (!anchor.HasExplicitKind)
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Unknown Kind'.");
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), anchor.Requiredness))
            {
                hasBlockingAuthoringIssue = true;
                AddQaAuthoringIssue(
                    ref issueCount,
                    ref errorCount,
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Invalid Requiredness'.");
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
                    $"ActivityContentAnchor object='{FormatValue(objectName)}' scene='{FormatValue(sceneName)}' issue='Declaration creation failed' message='{FormatValue(exception.Message)}'.");
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

            IReadOnlyList<ContentAnchorSetIssue> issues = contentAnchorSet.Issues;
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
            return result.Succeeded || allowAlreadyActive && result.Kind == FrameworkActivityRequestKind.IgnoredAlreadyActive;
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

            return result.Succeeded || allowNoActiveActivity && result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity;
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
            return configuredReason.NormalizeTextOrFallback(fallback);
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
            return value.NormalizeTextOrFallback(fallback);
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


        private enum SyntheticActivityContentExecutionParticipantMode
        {
            Success,
            NoOp
        }

        private sealed class SyntheticActivityContentExecutionParticipantSource : IActivityContentExecutionParticipantSource
        {
            private readonly ActivityContentExecutionParticipantCollection _collection;

            public SyntheticActivityContentExecutionParticipantSource(IReadOnlyList<IActivityContentExecutionParticipant> participants)
            {
                _collection = ActivityContentExecutionParticipantCollection.FromParticipants(participants);
            }

            public int ResolveCount { get; private set; }

            public ActivityContentExecutionParticipantSourceResult ResolveActivityContentExecutionParticipants(ActivityContentExecutionParticipantSourceRequest request)
            {
                ResolveCount++;
                if (!request.IsValid)
                {
                    return ActivityContentExecutionParticipantSourceResult.RejectedInvalidRequest(
                        request,
                        QaSource,
                        "qa.activity-content-execution-source.synthetic",
                        "Synthetic Activity content execution participant source rejected an invalid request.");
                }

                return ActivityContentExecutionParticipantSourceResult.FromCollection(
                    request,
                    _collection,
                    QaSource,
                    "qa.activity-content-execution-source.synthetic",
                    "Synthetic Activity content execution participant source returned explicit QA participants.");
            }
        }

        private sealed class SyntheticActivityContentExecutionParticipant : IActivityContentExecutionParticipant
        {
            private readonly ActivityContentExecutionParticipantDescriptor _descriptor;
            private readonly SyntheticActivityContentExecutionParticipantMode _mode;

            public SyntheticActivityContentExecutionParticipant(
                ActivityContentExecutionParticipantDescriptor descriptor,
                SyntheticActivityContentExecutionParticipantMode mode)
            {
                _descriptor = descriptor;
                _mode = mode;
            }

            public ActivityContentExecutionParticipantDescriptor GetActivityContentExecutionDescriptor()
            {
                return _descriptor;
            }

            public ActivityContentExecutionResult ExecuteActivityContent(ActivityContentExecutionRequest request)
            {
                if (!request.ContentId.Equals(_descriptor.ContentId))
                {
                    return request.IsRequired
                        ? ActivityContentExecutionResult.BlockingFailure(
                            request,
                            1,
                            QaSource,
                            "qa.activity-content-execution.synthetic",
                            "Synthetic Activity content execution participant received a request for a different content id.")
                        : ActivityContentExecutionResult.NonBlockingFailure(
                            request,
                            1,
                            QaSource,
                            "qa.activity-content-execution.synthetic",
                            "Synthetic Activity content execution participant received a request for a different content id.");
                }

                if (!_descriptor.SupportsPhase(request.Phase))
                {
                    return ActivityContentExecutionResult.SkippedResult(
                        request,
                        ActivityContentExecutionStatus.SkippedNotApplicable,
                        QaSource,
                        "qa.activity-content-execution.synthetic",
                        "Synthetic Activity content execution participant does not support the requested phase.");
                }

                return _mode == SyntheticActivityContentExecutionParticipantMode.NoOp
                    ? ActivityContentExecutionResult.SucceededNoOp(
                        request,
                        QaSource,
                        "qa.activity-content-execution.synthetic",
                        "Synthetic Activity content execution participant completed with no operation.")
                    : ActivityContentExecutionResult.Success(
                        request,
                        QaSource,
                        "qa.activity-content-execution.synthetic",
                        "Synthetic Activity content execution participant succeeded.");
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

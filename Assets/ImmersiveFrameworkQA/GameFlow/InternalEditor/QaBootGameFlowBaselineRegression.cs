using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Identity;
using Immersive.Framework.Loading;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.TransitionEffects;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaBootGameFlowBaselineRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run Boot and Game Flow Baseline Regression";
        private const string LogPrefix = "[BOOT_GAME_FLOW_BASELINE_REGRESSION]";
        private const string RouteAPath =
            "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteA.asset";
        private const string RouteBPath =
            "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteB.asset";
        private const string ActivityAPath =
            "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityA.asset";
        private const string ActivityBPath =
            "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityB.asset";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            var completed = new List<string>();
            try
            {
                Require(EditorApplication.isPlaying,
                    "Boot and Game Flow Baseline Regression requires a fresh Play Mode. It is one-shot because it performs Route and Activity requests.");
                completed.Add("fresh-play-mode-required");
                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                    out FrameworkRuntimeHost host, out string hostDiagnostic), hostDiagnostic);
                completed.Add("single-session-host-resolved");

                PlayerParticipationRuntimeHostModule participation =
                    host.GetComponent<PlayerParticipationRuntimeHostModule>();
                Require(participation != null && participation.IsInitialized,
                    "Player participation Session runtime is not initialized.");
                completed.Add("player-participation-session-initialized");

                FrameworkRuntimeState initialState = host.State;
                Require(initialState.PrimarySceneResult.Loaded && initialState.CurrentRoute != null,
                    "Startup Primary Scene was not prepared.");
                completed.Add("startup-primary-scene-prepared");
                Require(initialState.GameFlowStarted, "Game Flow did not report a successful boot.");
                completed.Add("boot-succeeded");
                Require(initialState.CurrentRoute.HasValidRouteId,
                    "Startup Route has no valid RouteId.");
                completed.Add("startup-route-id-valid");
                Require(initialState.CurrentActivity != null && initialState.CurrentActivity.HasValidActivityId,
                    "Startup Activity has no valid ActivityId.");
                completed.Add("startup-activity-id-valid");
                Require(initialState.IsActivityReady &&
                        initialState.RouteSceneCompositionResult.BlockingIssueCount == 0 &&
                        initialState.ActivityReadinessState.BlockingIssueCount == 0,
                    "Activity readiness is not Ready or boot retains blocking issues.");
                completed.Add("activity-ready-with-zero-blocking-issues");

                Require(CountLoadedAdapters<ILoadingSurfaceAdapter>() == 1,
                    "UIGlobal Loading surface was not resolved exactly once.");
                completed.Add("ui-global-loading-surface-resolved");
                Require(CountLoadedAdapters<IPauseSurfaceAdapter>() == 1,
                    "UIGlobal Pause surface was not resolved exactly once.");
                completed.Add("ui-global-pause-surface-resolved");
                Require(CountLoadedAdapters<ITransitionEffectAdapter>() == 1,
                    "UIGlobal Transition surface was not resolved exactly once.");
                completed.Add("ui-global-transition-surface-resolved");

                Require(host.TryResolveLocalPlayerProvisioningAuthoring(
                        out LocalPlayerProvisioningAuthoring provisioning,
                        out bool provisioningConfigured,
                        out string provisioningDiagnostic) &&
                        provisioningConfigured && provisioning != null && provisioning.RuntimeReady,
                    "Local Player provisioning is not Ready. " + provisioningDiagnostic);
                completed.Add("local-player-provisioning-ready");
                LocalPlayerActorSelectionRequestAuthoring selection =
                    provisioning.GetComponent<LocalPlayerActorSelectionRequestAuthoring>();
                Require(selection != null && selection.HasPlayerActorSelectionRuntimeBinding,
                    "Configured Actor Selection endpoint is not bound.");
                completed.Add("actor-selection-binding-complete");

                RouteAsset routeA = LoadRequired<RouteAsset>(RouteAPath);
                RouteAsset routeB = LoadRequired<RouteAsset>(RouteBPath);
                ActivityAsset activityA = LoadRequired<ActivityAsset>(ActivityAPath);
                ActivityAsset activityB = LoadRequired<ActivityAsset>(ActivityBPath);
                RouteAsset targetRoute = initialState.CurrentRoute.HasSameIdentity(routeA) ? routeB : routeA;
                RouteAsset restoreRoute = initialState.CurrentRoute;
                ActivityAsset targetActivity = targetRoute.HasSameIdentity(routeB) ? activityA : activityB;

                FrameworkRouteRequestResult routeResult = await ((IRouteRuntimePort)host)
                    .RequestRouteAsync(targetRoute, nameof(QaBootGameFlowBaselineRegression), "baseline-route-switch");
                Require(routeResult.Succeeded &&
                        routeResult.RouteLifecycleResult.PreviousRoute != null &&
                        routeResult.RouteLifecycleResult.PreviousRoute.HasSameIdentity(restoreRoute),
                    "Baseline Route request failed or lost previous Route release evidence. " + routeResult.Message);
                completed.Add("route-request-succeeded");
                Require(host.State.CurrentRoute.HasSameIdentity(targetRoute) &&
                        host.State.CurrentRouteIdentity ==
                            FrameworkIdentityKey.From(targetRoute.RouteId).StableText,
                    "Route request did not publish the target RouteId.");
                completed.Add("route-state-reflects-target-id");

                FrameworkActivityRequestResult activityResult = await ((IActivityRuntimePort)host)
                    .RequestActivityAsync(targetActivity, nameof(QaBootGameFlowBaselineRegression), "baseline-activity-switch");
                Require(activityResult.Succeeded &&
                        activityResult.ActivityFlowResult.PreviousActivity != null,
                    "Baseline Activity request failed or lost previous Activity release evidence. " + activityResult.Message);
                completed.Add("activity-request-succeeded");
                Require(host.State.CurrentActivity.HasSameIdentity(targetActivity) &&
                        host.State.CurrentActivityIdentity ==
                            FrameworkIdentityKey.From(targetActivity.ActivityId).StableText,
                    "Activity request did not publish the target ActivityId.");
                completed.Add("activity-state-reflects-target-id");

                FrameworkRouteRequestResult restoreResult = await ((IRouteRuntimePort)host)
                    .RequestRouteAsync(restoreRoute, nameof(QaBootGameFlowBaselineRegression), "baseline-route-restore");
                Require(restoreResult.Succeeded && host.State.CurrentRoute.HasSameIdentity(restoreRoute) &&
                        restoreResult.RouteLifecycleResult.PreviousRoute != null &&
                        restoreResult.RouteLifecycleResult.PreviousRoute.HasSameIdentity(targetRoute),
                    "Baseline cleanup could not restore the startup Route. " + restoreResult.Message);
                completed.Add("previous-route-and-activity-released-and-restored");

                Require(completed.Count == 18,
                    $"Boot and Game Flow baseline case count changed. actual='{completed.Count}'.");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' " +
                    $"routeId='{host.State.CurrentRoute.RouteId}' activityId='{host.State.CurrentActivity.ActivityId}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, $"Required canonical QA asset is missing: '{path}'.");
            return asset;
        }

        private static int CountLoadedAdapters<T>() where T : class
        {
            MonoBehaviour[] candidates = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            var seen = new HashSet<T>();
            for (int index = 0; index < candidates.Length; index++)
            {
                MonoBehaviour candidate = candidates[index];
                if (candidate != null && candidate.gameObject.scene.IsValid() &&
                    candidate.gameObject.scene.isLoaded && candidate is T adapter)
                {
                    seen.Add(adapter);
                }
            }
            return seen.Count;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

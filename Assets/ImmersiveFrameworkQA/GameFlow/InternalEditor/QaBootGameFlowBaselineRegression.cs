using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using ImmersiveFrameworkQA.Lifecycle;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaBootGameFlowBaselineRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Game Flow/Run Boot and Game Flow Baseline Regression";
        private const string Prefix = "[BOOT_GAME_FLOW_BASELINE_REGRESSION]";
        private const string RouteAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteA.asset";
        private const string RouteBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteB.asset";
        private const string ActivityAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityA.asset";
        private const string ActivityBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityB.asset";
        private const string RouteAScene = "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleRouteA.unity";
        private const string RouteBScene = "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleRouteB.unity";
        private const string AdditionalScene = "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleAdditional.unity";

        [MenuItem(MenuPath, true)] private static bool ValidateRun() => EditorApplication.isPlaying;
        [MenuItem(MenuPath)] private static async void Run()
        {
            var done = new List<string>(); FrameworkRuntimeHost host = null; RouteAsset initialRoute = null; ActivityAsset initialActivity = null; Exception failure = null; Exception restoreFailure = null;
            try
            {
                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(out host, out string diagnostic), diagnostic);
                Require(host.State.GameFlowStarted && host.State.CurrentRoute != null, "Game Flow is not ready.");
                RouteAsset routeA = Load<RouteAsset>(RouteAPath); RouteAsset routeB = Load<RouteAsset>(RouteBPath); ActivityAsset activityA = Load<ActivityAsset>(ActivityAPath); ActivityAsset activityB = Load<ActivityAsset>(ActivityBPath);
                initialRoute = host.State.CurrentRoute; initialActivity = host.State.CurrentActivity;
                IRouteRuntimePort routes = (IRouteRuntimePort)host; IActivityRuntimePort activities = (IActivityRuntimePort)host;
                if (host.State.CurrentRoute.HasSameIdentity(routeA)) Require((await routes.RequestRouteAsync(routeB, nameof(QaBootGameFlowBaselineRegression), "route-a-entry-precondition")).Succeeded, "Could not create a real Route A entry.");
                FrameworkRouteRequestResult enterA = await routes.RequestRouteAsync(routeA, nameof(QaBootGameFlowBaselineRegression), "route-a-entry"); Require(enterA.Succeeded, enterA.Message);
                Evidence primary = ResolveBinding(RouteAScene, routeA); Evidence additional = ResolveBinding(AdditionalScene, routeA); ValidateEnter(enterA.RouteLifecycleResult, routeA, primary, additional); done.Add("route-a-primary-additional-enter");
                ActivityVisibilityRule adapterA = ResolveAdapter(AdditionalScene, activityA); ActivityVisibilityRule adapterB = ResolveAdapter(AdditionalScene, activityB); Require(!adapterB.gameObject.activeSelf, "Activity B Additional adapter must start inactive."); Require(host.State.CurrentActivity != null && host.State.CurrentActivity.HasSameIdentity(activityA), "Route A startup Activity is not A."); Require(adapterA.gameObject.activeSelf && !adapterB.gameObject.activeSelf, "Startup Activity visibility diverged."); done.Add("startup-activity-a-and-inactive-b");
                FrameworkActivityRequestResult toB = await activities.RequestActivityAsync(activityB, nameof(QaBootGameFlowBaselineRegression), "activity-b"); Require(toB.Succeeded && toB.ActivityFlowResult.PreviousActivity.HasSameIdentity(activityA) && host.State.CurrentActivity.HasSameIdentity(activityB), toB.Message); ValidateActivity(toB.ActivityFlowResult); Require(!adapterA.gameObject.activeSelf && adapterB.gameObject.activeSelf && Loaded(AdditionalScene), "Activity B visibility or Additional lifetime diverged."); done.Add("activity-b");
                FrameworkActivityRequestResult clear = await activities.ClearActivityAsync(nameof(QaBootGameFlowBaselineRegression), "activity-clear"); Require(clear.Succeeded && clear.ActivityFlowResult.Cleared && host.State.CurrentActivity == null, clear.Message); ValidateActivity(clear.ActivityFlowResult); Require(!adapterA.gameObject.activeSelf && !adapterB.gameObject.activeSelf && Loaded(AdditionalScene), "Activity clear diverged."); done.Add("activity-clear");
                FrameworkRouteRequestResult enterB = await routes.RequestRouteAsync(routeB, nameof(QaBootGameFlowBaselineRegression), "route-a-to-b"); Require(enterB.Succeeded && enterB.RouteLifecycleResult.PreviousRoute.HasSameIdentity(routeA), enterB.Message); ValidateExit(enterB.RouteLifecycleResult); Require(!Loaded(AdditionalScene) && !Loaded(RouteAScene) && Loaded(RouteBScene), "Route B isolation diverged."); done.Add("route-a-to-b");
                FrameworkRouteRequestResult reenterA = await routes.RequestRouteAsync(routeA, nameof(QaBootGameFlowBaselineRegression), "route-b-to-a"); Require(reenterA.Succeeded && reenterA.RouteLifecycleResult.PreviousRoute.HasSameIdentity(routeB), reenterA.Message); Evidence newPrimary = ResolveBinding(RouteAScene, routeA); Evidence newAdditional = ResolveBinding(AdditionalScene, routeA); ValidateEnter(reenterA.RouteLifecycleResult, routeA, newPrimary, newAdditional); Require(primary.Binding == null && additional.Binding == null, "Released Route A occurrence was reused."); done.Add("route-b-to-a-new-scope");
            }
            catch (Exception exception) { failure = exception; Debug.LogError($"{Prefix} status='Failed' message='{Escape(exception.Message)}' completed='{string.Join(",", done)}'."); }
            finally
            {
                if (host != null && initialRoute != null) try { IRouteRuntimePort routes = (IRouteRuntimePort)host; IActivityRuntimePort activities = (IActivityRuntimePort)host; if (host.State.CurrentRoute == null || !host.State.CurrentRoute.HasSameIdentity(initialRoute)) Require((await routes.RequestRouteAsync(initialRoute, nameof(QaBootGameFlowBaselineRegression), "restore-route")).Succeeded, "Route restore failed."); if (initialActivity == null && host.State.CurrentActivity != null) Require((await activities.ClearActivityAsync(nameof(QaBootGameFlowBaselineRegression), "restore-empty-activity")).Succeeded, "Activity clear restore failed."); else if (initialActivity != null && (host.State.CurrentActivity == null || !host.State.CurrentActivity.HasSameIdentity(initialActivity))) Require((await activities.RequestActivityAsync(initialActivity, nameof(QaBootGameFlowBaselineRegression), "restore-activity")).Succeeded, "Activity restore failed."); done.Add("restored"); } catch (Exception exception) { restoreFailure = exception; Debug.LogError($"{Prefix} status='Restoration Failed' message='{Escape(exception.Message)}'."); }
            }
            if (failure != null) throw failure; if (restoreFailure != null) throw restoreFailure; Debug.Log($"{Prefix} status='Passed' cases='{done.Count}' completed='{string.Join(",", done)}'.");
        }
        private static void ValidateEnter(RouteLifecycleStartResult result, RouteAsset route, Evidence primary, Evidence additional) { Require(result.RouteSceneCompositionResult.BlockingIssueCount == 0 && Loaded(RouteAScene) && Loaded(AdditionalScene), "Route A composition diverged."); Require(result.RouteContentEnterResult.Executed && result.RouteContentEnterResult.BindingCount == 2 && result.RouteContentEnterResult.ReceiverCount == 2 && result.RouteContentEnterResult.FailedReceiverCount == 0, "Route Content Enter evidence diverged."); Require(primary.Probe.EnterCount == 1 && primary.Probe.ExitCount == 0 && primary.Probe.LastRoute.HasSameIdentity(route), "Primary probe evidence diverged."); Require(additional.Probe.EnterCount == 1 && additional.Probe.ExitCount == 0 && additional.Probe.LastRoute.HasSameIdentity(route), "Additional probe evidence diverged."); ValidateActivity(result.ActivityFlowResult); }
        private static void ValidateExit(RouteLifecycleStartResult result) { Require(result.RouteContentExitResult.Executed && result.RouteContentExitResult.BindingCount == 2 && result.RouteContentExitResult.ReceiverCount == 2 && result.RouteContentExitResult.FailedReceiverCount == 0, "Route Content Exit evidence diverged."); }
        private static void ValidateActivity(ActivityFlowStartResult result) { Require(result.ActivityContentResult.BindingCount >= 2 && result.ActivityContentResult.MissingActivityCount == 0 && !result.ActivityContentLifecycleResult.HasFailures, "Activity evidence diverged."); }
        private static Evidence ResolveBinding(string path, RouteAsset route) { Scene scene = Scene(path); var matches = new List<RouteContentContribution>(); foreach (GameObject root in scene.GetRootGameObjects()) foreach (RouteContentContribution binding in root.GetComponentsInChildren<RouteContentContribution>(true)) if (binding != null && binding.Route != null && binding.Route.HasSameIdentity(route) && binding.MatchesRoute(route)) matches.Add(binding); Require(matches.Count == 1, $"Expected one Route binding in '{path}'."); QaRouteContentLifecycleProbe[] probes = matches[0].GetComponentsInChildren<QaRouteContentLifecycleProbe>(true); Require(probes.Length == 1, $"Expected one probe in '{path}'."); return new Evidence(matches[0], probes[0]); }
        private static ActivityVisibilityRule ResolveAdapter(string path, ActivityAsset activity) { Scene scene = Scene(path); var matches = new List<ActivityVisibilityRule>(); foreach (GameObject root in scene.GetRootGameObjects()) foreach (ActivityVisibilityRule rule in root.GetComponentsInChildren<ActivityVisibilityRule>(true)) if (rule != null && rule.Activities.Count == 1 && rule.Activities[0] != null && rule.Activities[0].HasSameIdentity(activity)) matches.Add(rule); Require(matches.Count == 1, $"Expected one Activity visibility rule in '{path}'."); return matches[0]; }
        private static Scene Scene(string path) { Scene scene = SceneManager.GetSceneByPath(path); Require(scene.IsValid() && scene.isLoaded, $"Expected loaded scene '{path}'."); return scene; }
        private static bool Loaded(string path) { Scene scene = SceneManager.GetSceneByPath(path); return scene.IsValid() && scene.isLoaded; }
        private static T Load<T>(string path) where T : UnityEngine.Object { T asset = AssetDatabase.LoadAssetAtPath<T>(path); Require(asset != null, $"Missing QA asset '{path}'."); return asset; }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static string Escape(string value) => string.IsNullOrEmpty(value) ? string.Empty : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        private readonly struct Evidence { internal Evidence(RouteContentContribution binding, QaRouteContentLifecycleProbe probe) { Binding = binding; Probe = probe; } internal RouteContentContribution Binding { get; } internal QaRouteContentLifecycleProbe Probe { get; } }
    }
}

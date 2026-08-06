using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaActivityReadinessPostTransitionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run Activity Readiness Post-Transition Smoke";
        private const string Prefix = "[QA_ACTIVITY_READINESS_POST_TRANSITION]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var passed = new List<string>();
            try
            {
                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                    out FrameworkRuntimeHost host, out string diagnostic), diagnostic);
                GameFlowRuntime gameFlow = host.CurrentGameFlowRuntime;
                Require(gameFlow != null && gameFlow.CurrentRouteLifecycleRuntime != null,
                    "Current GameFlow runtime is unavailable.");

                RouteLifecycleRuntime routeLifecycle = gameFlow.CurrentRouteLifecycleRuntime;
                ActivityFlowRuntime activityFlow = routeLifecycle.CurrentActivityFlowRuntime;
                Require(activityFlow != null && activityFlow.HasCurrentActivityContext,
                    "Current ActivityFlow context is unavailable.");
                Require(activityFlow.TryGetCurrentActivityResult(out ActivityFlowStartResult before),
                    "Current ActivityFlow result is unavailable.");

                ActivityReadinessOccurrence occurrence = activityFlow.CurrentOccurrence;
                Require(occurrence.IsValid && before.ActivityReadinessState.IsReady,
                    "Smoke requires a ready active Activity.");
                var routeBefore = gameFlow.CurrentRoute;
                var activityBefore = gameFlow.CurrentActivity;

                ActivityReadinessState notReady = CreateReadiness(before, 1, "QA post-transition NotReady");
                Require(activityFlow.TryPublishPostTransitionReadiness(
                    occurrence, notReady, "qa-ready-to-not-ready", out _),
                    "Ready-to-NotReady update was rejected.");
                RequireConsistent(host, gameFlow, routeLifecycle, activityFlow, occurrence, false);
                Require(ReferenceEquals(routeBefore, gameFlow.CurrentRoute) &&
                    ReferenceEquals(activityBefore, gameFlow.CurrentActivity),
                    "Readiness update executed a Route or Activity request.");
                passed.Add("ReadyToNotReady");

                Require(activityFlow.TryGetCurrentActivityResult(out ActivityFlowStartResult notReadyResult),
                    "Current result disappeared after readiness update.");
                ActivityReadinessState ready = CreateReadiness(notReadyResult, 0, "QA post-transition Ready");
                Require(activityFlow.TryPublishPostTransitionReadiness(
                    occurrence, ready, "qa-not-ready-to-ready", out _),
                    "NotReady-to-Ready update was rejected.");
                RequireConsistent(host, gameFlow, routeLifecycle, activityFlow, occurrence, true);
                passed.Add("NotReadyToReady");

                FrameworkRuntimeState stateBeforeNoOp = host.State;
                Require(!activityFlow.TryPublishPostTransitionReadiness(
                    occurrence, ready, "qa-identical-value", out _),
                    "Identical readiness value was accepted.");
                Require(host.State.ActivityReadinessState.Equals(stateBeforeNoOp.ActivityReadinessState),
                    "Identical readiness value changed host state.");
                passed.Add("IdenticalValueIgnored");

                Debug.Log($"{Prefix} status='Passed' passed='{string.Join(",", passed)}' " +
                    $"route='{routeBefore.RouteName}' activity='{activityBefore.ActivityName}' " +
                    $"occurrence='{occurrence.TransitionSequence}' newRequest='False'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{Prefix} status='Failed' passed='{string.Join(",", passed)}' " +
                    $"exception='{exception.GetType().Name}' message='{exception.Message}'.");
                throw;
            }
        }

        private static ActivityReadinessState CreateReadiness(
            ActivityFlowStartResult result, int blockingIssues, string reason)
        {
            return new ActivityReadinessState(
                blockingIssues == 0 ? ActivityReadinessStatus.Ready : ActivityReadinessStatus.NotReady,
                result.Activity, result.ActivityContentSet, result.ActivityContentLifecycleResult,
                result.ActivityContentExecutionResult.Executed,
                blockingIssues > 0, blockingIssues, blockingIssues,
                nameof(QaActivityReadinessPostTransitionSmoke), reason, reason);
        }

        private static void RequireConsistent(
            FrameworkRuntimeHost host, GameFlowRuntime gameFlow,
            RouteLifecycleRuntime routeLifecycle, ActivityFlowRuntime activityFlow,
            ActivityReadinessOccurrence occurrence, bool ready)
        {
            Require(activityFlow.TryGetCurrentActivityResult(out ActivityFlowStartResult activityResult),
                "ActivityFlow snapshot is unavailable.");
            Require(routeLifecycle.TryGetCurrentRouteResult(out RouteLifecycleStartResult routeResult),
                "RouteLifecycle snapshot is unavailable.");
            Require(gameFlow.TryGetCurrentRouteLifecycleResult(out RouteLifecycleStartResult gameResult),
                "GameFlow snapshot is unavailable.");
            Require(activityFlow.CurrentOccurrence.Matches(occurrence.Activity, occurrence.TransitionSequence) &&
                routeLifecycle.CurrentOccurrence.Matches(occurrence.Activity, occurrence.TransitionSequence) &&
                gameFlow.CurrentOccurrence.Matches(occurrence.Activity, occurrence.TransitionSequence),
                "Current occurrence diverged across runtime layers.");
            Require(activityResult.IsActivityReady == ready && routeResult.ActivityFlowResult.IsActivityReady == ready &&
                gameResult.ActivityFlowResult.IsActivityReady == ready && host.State.IsActivityReady == ready &&
                host.SessionState.IsActivityReady == ready,
                "Activity readiness is inconsistent across runtime layers.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}

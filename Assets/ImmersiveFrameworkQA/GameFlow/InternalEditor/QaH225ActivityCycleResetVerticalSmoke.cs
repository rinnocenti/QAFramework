using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.CycleReset;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH225ActivityCycleResetVerticalSmoke
    {
        private const string LogPrefix = "[H225_ACTIVITY_CYCLE_RESET_VERTICAL_SMOKE]";

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.5 Run Activity Cycle Reset Vertical Smoke", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.5 Run Activity Cycle Reset Vertical Smoke")]
        public static async void Run()
        {
            var completed = new List<string>();
            GameObject root = null;
            try
            {
                Require(FrameworkRuntimeHost.TryGetCurrent(out FrameworkRuntimeHost host) && host != null, "H2.2.5 vertical smoke requires FrameworkRuntimeHost.");
                Require(host.State.CurrentRoute != null, "H2.2.5 vertical smoke requires active Route.");
                Require(host.State.CurrentActivity != null, "H2.2.5 vertical smoke requires active Activity.");
                await VerifyNoActiveRouteAsync(completed);
                await VerifyNoActiveActivityAsync(host, completed);

                IActivityCycleResetRuntimePort port = host;
                root = new GameObject("H225 Vertical Trigger");
                ActivityCycleResetTrigger trigger = root.AddComponent<ActivityCycleResetTrigger>();
                Require(trigger.TryBindActivityCycleResetRuntime(port, out string issue), issue);

                await VerifyNominalActivityOnlyAsync(host, trigger, completed);
                await VerifyMixedRouteOnlyAsync(host, port, completed);
                await VerifyNoParticipantsAsync(host, port, completed);
                await VerifyOptionalFailureAsync(host, port, completed);
                await VerifyRequiredFailureAsync(host, port, completed);
                Require(!trigger.IsRequestInFlight, "Trigger retained partial request-in-flight state.");
                completed.Add("no-request-in-flight-state-remains-after-completion");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                if (FrameworkRuntimeHost.TryGetCurrent(out FrameworkRuntimeHost host)) host.SetCycleResetParticipantSource(null);
                if (root != null) UnityEngine.Object.Destroy(root);
            }
        }

        private static async Task VerifyNoActiveRouteAsync(ICollection<string> completed)
        {
            var runtime = new RouteLifecycleRuntime(new RuntimeContentRuntime(), new RuntimeContentAnchorBinding(), new QaFakeRouteRuntimePort(), new QaFakeActivityRuntimePort(), new QaFakeRouteCycleResetRuntimePort(), new QaFakeActivityCycleResetRuntimePort());
            CycleResetResult result = await runtime.RequestActivityCycleResetAsync(CycleResetPolicy.ActivityDefault(), "H225", "no-active-route");
            Require(result.Status == CycleResetStatus.RejectedInvalidRequest && result.Message.Contains("No active Route"), result.ToDiagnosticString());
            completed.Add("no-active-route-explicit-structured-failure");
        }

        private static async Task VerifyNoActiveActivityAsync(
            FrameworkRuntimeHost host,
            ICollection<string> completed)
        {
            RouteAsset route = CreateRouteWithoutStartupActivity(host.State.CurrentRoute);
            try
            {
                IRouteRuntimePort routeRuntimePort = host;
                IActivityRuntimePort activityRuntimePort = host;
                IRouteCycleResetRuntimePort routeCycleResetRuntimePort = host;
                IActivityCycleResetRuntimePort activityCycleResetRuntimePort = host;
                var runtime = new RouteLifecycleRuntime(
                    new RuntimeContentRuntime(),
                    new RuntimeContentAnchorBinding(),
                    routeRuntimePort,
                    activityRuntimePort,
                    routeCycleResetRuntimePort,
                    activityCycleResetRuntimePort);
                var participantSource = new H225ParticipantSource(
                    H225Scenario.NominalActivityOnly);
                runtime.SetCycleResetParticipantSource(participantSource);

                RouteLifecycleStartResult startResult = await runtime.StartRouteAsync(
                    route,
                    "H225",
                    "establish-route-without-activity");
                Require(startResult.Started, startResult.Message);
                Require(
                    runtime.CurrentRoute == route && runtime.CurrentActivity == null,
                    "Isolated Route runtime did not retain the Route with no active Activity.");

                CycleResetResult result = await runtime.RequestActivityCycleResetAsync(
                    CycleResetPolicy.ActivityDefault(),
                    "H225",
                    "no-active-activity");
                Require(
                    result.Status == CycleResetStatus.RejectedInvalidRequest &&
                    result.Message.Contains("No active Activity") &&
                    result.ParticipantCount == 0 &&
                    participantSource.ActivityRequired.ExecutionCount == 0 &&
                    participantSource.ActivityOptional.ExecutionCount == 0 &&
                    runtime.CurrentRoute == route && runtime.CurrentActivity == null,
                    result.ToDiagnosticString());
                CycleResetResult retryResult = await runtime.RequestActivityCycleResetAsync(
                    CycleResetPolicy.ActivityDefault(),
                    "H225",
                    "no-active-activity-retry");
                Require(
                    retryResult.Status == CycleResetStatus.RejectedInvalidRequest &&
                    retryResult.Message.Contains("No active Activity") &&
                    participantSource.ActivityRequired.ExecutionCount == 0 &&
                    participantSource.ActivityOptional.ExecutionCount == 0 &&
                    runtime.CurrentRoute == route && runtime.CurrentActivity == null,
                    retryResult.ToDiagnosticString());
                completed.Add("no-active-activity-explicit-structured-failure");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(route);
            }
        }

        private static RouteAsset CreateRouteWithoutStartupActivity(RouteAsset sourceRoute)
        {
            Require(
                sourceRoute != null && sourceRoute.HasPrimaryScene,
                "Isolated no-active-Activity fixture requires the current Route primary scene.");
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = "H225 Route Without Activity";
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeName").stringValue = route.name;
            serialized.FindProperty("primaryScenePath").stringValue = sourceRoute.PrimaryScenePath;
            serialized.FindProperty("primarySceneName").stringValue = sourceRoute.PrimarySceneName;
            serialized.FindProperty("startupActivity").objectReferenceValue = null;
            serialized.FindProperty("transitionGateMode").intValue =
                (int)sourceRoute.TransitionGateMode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return route;
        }

        private static async Task VerifyNominalActivityOnlyAsync(FrameworkRuntimeHost host, ActivityCycleResetTrigger trigger, ICollection<string> completed)
        {
            var source = new H225ParticipantSource(H225Scenario.NominalActivityOnly);
            host.SetCycleResetParticipantSource(source);
            trigger.RequestActivityCycleReset();
            await Wait(trigger);
            CycleResetResult result = trigger.LastResult;
            Require(
                trigger.LastRequestSucceeded && result.Status == CycleResetStatus.Succeeded &&
                result.IssueCount == 0 && result.NonBlockingIssueCount == 0 &&
                result.ParticipantCount == 2 && result.SucceededCount == 2 &&
                result.Request.Scope == CycleResetScope.Activity &&
                result.Request.ActiveRoute == host.State.CurrentRoute &&
                result.Request.ActiveActivity == host.State.CurrentActivity &&
                source.ActivityRequired.ExecutionCount == 1 && source.ActivityOptional.ExecutionCount == 1 &&
                source.RouteOnly == null,
                result.ToDiagnosticString());
            completed.Add("bound-trigger-nominal-activity-only-succeeded-with-zero-issues");
        }

        private static async Task VerifyMixedRouteOnlyAsync(FrameworkRuntimeHost host, IActivityCycleResetRuntimePort port, ICollection<string> completed)
        {
            var source = new H225ParticipantSource(H225Scenario.MixedWithRouteOnly);
            host.SetCycleResetParticipantSource(source);
            CycleResetResult result = await port.RequestActivityCycleResetAsync("H225", "mixed-route-only");
            Require(
                result.Status == CycleResetStatus.CompletedWithWarnings &&
                CountIssues(result, CycleResetIssueKind.UnsupportedScope) == 1 && result.IssueCount == 1 &&
                result.ParticipantCount == 2 && result.SucceededCount == 2 &&
                source.ActivityRequired.ExecutionCount == 1 && source.ActivityOptional.ExecutionCount == 1 &&
                source.RouteOnly != null && source.RouteOnly.ExecutionCount == 0,
                result.ToDiagnosticString());
            completed.Add("mixed-route-only-produces-unsupported-scope-without-route-execution");
        }

        private static async Task VerifyNoParticipantsAsync(FrameworkRuntimeHost host, IActivityCycleResetRuntimePort port, ICollection<string> completed)
        {
            host.SetCycleResetParticipantSource(null);
            CycleResetResult result = await port.RequestActivityCycleResetAsync("H225", "no-participants");
            Require(result.Status == CycleResetStatus.SucceededNoParticipants, result.ToDiagnosticString());
            completed.Add("no-participants-succeeded-no-participants");
        }

        private static async Task VerifyOptionalFailureAsync(FrameworkRuntimeHost host, IActivityCycleResetRuntimePort port, ICollection<string> completed)
        {
            var source = new H225ParticipantSource(H225Scenario.OptionalFailure);
            host.SetCycleResetParticipantSource(source);
            CycleResetResult result = await port.RequestActivityCycleResetAsync("H225", "optional-failure");
            Require(
                result.Status == CycleResetStatus.CompletedWithWarnings &&
                result.NonBlockingFailureCount == 1 && result.BlockingFailureCount == 0 &&
                result.SucceededCount == 1 && source.ActivityRequired.ExecutionCount == 1 && source.ActivityOptional.ExecutionCount == 1,
                result.ToDiagnosticString());
            completed.Add("optional-failure-warnings-from-optional-participant");
        }

        private static async Task VerifyRequiredFailureAsync(FrameworkRuntimeHost host, IActivityCycleResetRuntimePort port, ICollection<string> completed)
        {
            var source = new H225ParticipantSource(H225Scenario.RequiredFailure);
            host.SetCycleResetParticipantSource(source);
            CycleResetResult result = await port.RequestActivityCycleResetAsync("H225", "required-failure");
            Require(result.Status == CycleResetStatus.Failed && result.BlockingFailureCount == 1 && source.ActivityRequired.ExecutionCount == 1, result.ToDiagnosticString());
            completed.Add("required-failure-blocking-failure");
        }

        private static int CountIssues(CycleResetResult result, CycleResetIssueKind kind)
        {
            int count = 0;
            for (int index = 0; index < result.IssueCount; index++) if (result.Issues[index].Kind == kind) count++;
            return count;
        }

        private static async Task Wait(ActivityCycleResetTrigger trigger)
        {
            for (int index = 0; index < 256; index++)
            {
                await Task.Yield();
                if (!trigger.IsRequestInFlight && trigger.HasLastResult) return;
            }
            throw new InvalidOperationException("Activity Cycle Reset Trigger did not complete.");
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private enum H225Scenario { NominalActivityOnly, MixedWithRouteOnly, OptionalFailure, RequiredFailure }

        private sealed class H225ParticipantSource : ICycleResetParticipantSource
        {
            internal H225ParticipantSource(H225Scenario scenario)
            {
                ActivityRequired = Create("qa.h225.activity.required", CycleResetScope.Activity, CycleResetParticipantRequiredness.Required, 10, scenario == H225Scenario.RequiredFailure);
                ActivityOptional = Create("qa.h225.activity.optional", CycleResetScope.Activity, CycleResetParticipantRequiredness.Optional, 20, scenario == H225Scenario.OptionalFailure);
                if (scenario == H225Scenario.MixedWithRouteOnly) RouteOnly = Create("qa.h225.route.only", CycleResetScope.Route, CycleResetParticipantRequiredness.Optional, 30, false);
            }

            internal H225Participant ActivityRequired { get; }
            internal H225Participant ActivityOptional { get; }
            internal H225Participant RouteOnly { get; }

            public IReadOnlyList<ICycleResetParticipant> ResolveCycleResetParticipants(CycleResetRequest request) => RouteOnly == null
                ? new ICycleResetParticipant[] { ActivityRequired, ActivityOptional }
                : new ICycleResetParticipant[] { ActivityRequired, ActivityOptional, RouteOnly };

            private static H225Participant Create(string id, CycleResetScope scope, CycleResetParticipantRequiredness requiredness, int order, bool fails)
            {
                CycleResetParticipantId participantId = CycleResetParticipantId.From(id);
                CycleResetParticipantDescriptor descriptor = requiredness == CycleResetParticipantRequiredness.Required
                    ? CycleResetParticipantDescriptor.Required(participantId, scope, order, id, "H225", "vertical")
                    : CycleResetParticipantDescriptor.Optional(participantId, scope, order, id, "H225", "vertical");
                return new H225Participant(descriptor, fails);
            }
        }

        private sealed class H225Participant : ICycleResetParticipant
        {
            private readonly CycleResetParticipantDescriptor _descriptor;
            private readonly bool _fails;
            internal H225Participant(CycleResetParticipantDescriptor descriptor, bool fails) { _descriptor = descriptor; _fails = fails; }
            internal int ExecutionCount { get; private set; }
            public CycleResetParticipantDescriptor GetCycleResetDescriptor() => _descriptor;
            public CycleResetParticipantResult ResetCycle(CycleResetContext context)
            {
                ExecutionCount++;
                return _fails
                    ? CycleResetParticipantResult.Failure(context, 1, "H225", "vertical", "Synthetic failure.")
                    : CycleResetParticipantResult.Success(context, "H225", "vertical", "Synthetic success.");
            }
        }
    }
}

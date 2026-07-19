using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.CycleReset;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH224RouteCycleResetVerticalSmoke
    {
        private const string LogPrefix = "[H224_ROUTE_CYCLE_RESET_VERTICAL_SMOKE]";
        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.4 Run Route Cycle Reset Vertical Smoke", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.4 Run Route Cycle Reset Vertical Smoke")]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        public static async Task RunInternalAsync()
        {
            var completed = new List<string>(); GameObject root = null;
            try
            {
                Require(FrameworkRuntimeHost.TryGetCurrent(out FrameworkRuntimeHost host) && host != null, "H2.2.4 vertical smoke requires FrameworkRuntimeHost.");
                Require(host.State.CurrentRoute != null, "H2.2.4 vertical smoke requires an active Route; no-active-Route structured rejection remains an explicit runtime precondition.");
                Require(host.State.CurrentActivity != null, "H2.2.4 vertical smoke requires an active Activity to prove RouteDefault activity inclusion.");
                IRouteCycleResetRuntimePort port = host;
                var noRouteRuntime = new RouteLifecycleRuntime(
                    new RuntimeContentRuntime(),
                    new RuntimeContentAnchorBinding(),
                    new QaFakeRouteRuntimePort(),
                    new QaFakeActivityRuntimePort(),
                    new QaFakeRouteCycleResetRuntimePort(),
                    new QaFakeActivityCycleResetRuntimePort(),
                    new QaFakeActivityRestartRuntimePort());
                CycleResetResult noRoute = await noRouteRuntime.RequestRouteCycleResetAsync(
                    CycleResetPolicy.RouteDefault(),
                    "H224",
                    "no-active-route");
                Require(noRoute.Status == CycleResetStatus.RejectedInvalidRequest &&
                    noRoute.Message.Contains("No active Route"), noRoute.ToDiagnosticString());
                completed.Add("no-active-route-explicit-structured-failure");
                root = new GameObject("H224 Vertical Trigger");
                RouteCycleResetTrigger trigger = root.AddComponent<RouteCycleResetTrigger>();
                Require(trigger.TryBindRouteCycleResetRuntime(port, out string bindingIssue), bindingIssue);

                host.SetCycleResetParticipantSource(new H224ParticipantSource(H224ParticipantMode.Success));
                trigger.RequestRouteCycleReset();
                await Wait(trigger);
                CycleResetResult success = trigger.LastResult;
                Require(trigger.LastRequestSucceeded && success.Request.ActiveRoute == host.State.CurrentRoute && success.Request.IncludesActiveActivity && success.ParticipantCount == 2, success.ToDiagnosticString());
                completed.Add("trigger-bound-reaches-game-flow-active-route-retained-active-activity-included-route-and-activity-participants-execute");

                host.SetCycleResetParticipantSource(null);
                CycleResetResult noParticipants = await port.RequestRouteCycleResetAsync("H224", "no-participants");
                Require(noParticipants.Status == CycleResetStatus.SucceededNoParticipants, noParticipants.ToDiagnosticString());
                completed.Add("no-participants-succeeded-no-participants");

                host.SetCycleResetParticipantSource(new H224ParticipantSource(H224ParticipantMode.OptionalFailure));
                CycleResetResult optionalFailure = await port.RequestRouteCycleResetAsync("H224", "optional-failure");
                Require(optionalFailure.Status == CycleResetStatus.CompletedWithWarnings, optionalFailure.ToDiagnosticString());
                completed.Add("optional-failure-completed-with-warnings");

                host.SetCycleResetParticipantSource(new H224ParticipantSource(H224ParticipantMode.RequiredFailure));
                CycleResetResult requiredFailure = await port.RequestRouteCycleResetAsync("H224", "required-failure");
                Require(requiredFailure.Failed && requiredFailure.BlockingFailureCount == 1, requiredFailure.ToDiagnosticString());
                completed.Add("required-failure-blocking-failure");
                Require(!trigger.IsRequestInFlight, "Trigger retained a partial request-in-flight state.");
                completed.Add("no-partial-request-in-flight-state-remains");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception) { Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'."); throw; }
            finally { if (FrameworkRuntimeHost.TryGetCurrent(out FrameworkRuntimeHost host)) host.SetCycleResetParticipantSource(null); if (root != null) UnityEngine.Object.Destroy(root); }
        }

        private static async Task Wait(RouteCycleResetTrigger trigger)
        {
            for (int i = 0; i < 256; i++) { await Task.Yield(); if (!trigger.IsRequestInFlight && trigger.HasLastResult) return; }
            throw new InvalidOperationException("Route Cycle Reset Trigger did not complete.");
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

        private enum H224ParticipantMode { Success, OptionalFailure, RequiredFailure }
        private sealed class H224ParticipantSource : ICycleResetParticipantSource
        {
            private readonly H224ParticipantMode _mode;
            internal H224ParticipantSource(H224ParticipantMode mode) { _mode = mode; }
            public IReadOnlyList<ICycleResetParticipant> ResolveCycleResetParticipants(CycleResetRequest request)
            {
                return new ICycleResetParticipant[]
                {
                    new H224Participant(CycleResetParticipantDescriptor.Required(CycleResetParticipantId.From("qa.h224.route"), CycleResetScope.Route, 10, "H224 Route", "H224", "vertical"), _mode == H224ParticipantMode.RequiredFailure),
                    new H224Participant(CycleResetParticipantDescriptor.Optional(CycleResetParticipantId.From("qa.h224.activity"), CycleResetScope.Activity, 20, "H224 Activity", "H224", "vertical"), _mode == H224ParticipantMode.OptionalFailure)
                };
            }
        }
        private sealed class H224Participant : ICycleResetParticipant
        {
            private readonly CycleResetParticipantDescriptor _descriptor; private readonly bool _fails;
            internal H224Participant(CycleResetParticipantDescriptor descriptor, bool fails) { _descriptor = descriptor; _fails = fails; }
            public CycleResetParticipantDescriptor GetCycleResetDescriptor() => _descriptor;
            public CycleResetParticipantResult ResetCycle(CycleResetContext context) => _fails
                ? CycleResetParticipantResult.Failure(context, 1, "H224", "vertical", "Synthetic failure.")
                : CycleResetParticipantResult.Success(context, "H224", "vertical", "Synthetic success.");
        }
    }
}

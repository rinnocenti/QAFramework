using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH222RouteRequestTriggerBindingSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/H2.2.2 Run Route Request Trigger Binding Smoke";
        private const string LogPrefix = "[H222_ROUTE_REQUEST_TRIGGER_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();

            try
            {
                Require(EditorApplication.isPlaying,
                    "H2.2.2 Route request trigger binding smoke requires Play Mode.");
                completed.Add("play-mode-required");
                Require(
                    FrameworkRuntimeHost.TryGetCurrent(
                        out FrameworkRuntimeHost host) && host != null,
                    "H2.2.2 Route request trigger binding smoke requires an initialized global FrameworkRuntimeHost.");
                IRouteRuntimePort hostRouteRuntime = host;
                Require(hostRouteRuntime != null,
                    "Global FrameworkRuntimeHost did not expose the Route runtime port.");
                completed.Add("runtime-host-available");

                RouteAsset target = CreateTarget("H222 Target", objects);
                GameObject boundRoot = CreateRoot("H222 Bound Trigger", objects);
                RouteRequestTrigger boundTrigger =
                    boundRoot.AddComponent<RouteRequestTrigger>();
                boundTrigger.TargetRoute = target;
                var fakeA = new QaFakeRouteRuntimePort();
                Require(boundTrigger.TryBindRouteRuntime(fakeA, out string firstIssue) &&
                    boundTrigger.HasRouteRuntimeBinding,
                    "Initial Route runtime binding was not accepted. " + firstIssue);
                completed.Add("initial-binding-accepted");
                Require(boundTrigger.TryBindRouteRuntime(fakeA, out string sameIssue) &&
                    boundTrigger.RouteRuntimeBindingDiagnostic.Contains("idempotent"),
                    "Same Route runtime rebinding was not idempotent. " + sameIssue);
                completed.Add("same-port-rebind-idempotent");

                var fakeB = new QaFakeRouteRuntimePort();
                Require(!boundTrigger.TryBindRouteRuntime(fakeB, out string differentIssue) &&
                    boundTrigger.HasRouteRuntimeBinding &&
                    differentIssue.Contains("different port"),
                    "Different Route runtime rebinding was not rejected. " + differentIssue);
                completed.Add("different-port-rebind-rejected");

                int submitted = 0;
                int completedEvents = 0;
                using (boundTrigger.SubscribeRequestEvents(requestEvent =>
                       {
                           if (requestEvent.Phase == FlowRequestEventPhase.Submitted) submitted++;
                           if (requestEvent.Phase == FlowRequestEventPhase.Completed) completedEvents++;
                       }))
                {
                    boundTrigger.RequestRoute();
                }
                Require(fakeA.RequestCallCount == 1 &&
                    fakeA.LastTargetRoute == target &&
                    fakeA.LastSource == "RouteRequestTrigger" &&
                    fakeA.LastReason == target.RouteName &&
                    fakeB.RequestCallCount == 0 &&
                    submitted == 1 && completedEvents == 1 &&
                    boundTrigger.LastRequestSucceeded &&
                    !boundTrigger.IsRequestInFlight,
                    "Bound Route trigger did not forward the request only to fake A.");
                completed.Add("request-forwarded-to-bound-port");

                GameObject unboundRoot = CreateRoot("H222 Unbound Trigger", objects);
                RouteRequestTrigger unboundTrigger =
                    unboundRoot.AddComponent<RouteRequestTrigger>();
                unboundTrigger.TargetRoute = target;
                int unboundSubmitted = 0;
                int unboundCompleted = 0;
                using (unboundTrigger.SubscribeRequestEvents(requestEvent =>
                       {
                           if (requestEvent.Phase == FlowRequestEventPhase.Submitted) unboundSubmitted++;
                           if (requestEvent.Phase == FlowRequestEventPhase.Completed) unboundCompleted++;
                       }))
                {
                    unboundTrigger.RequestRoute();
                }
                Require(unboundSubmitted == 0 && unboundCompleted == 1 &&
                    !unboundTrigger.IsRequestInFlight &&
                    unboundTrigger.LastRequestFailed &&
                    unboundTrigger.LastMessage.Contains("Route runtime port is not bound") &&
                    unboundTrigger.RouteRuntimeBindingDiagnostic.Contains("Route runtime port is not bound"),
                    "Unbound Route trigger submitted or fell back to the global host.");
                completed.Add("unbound-trigger-does-not-fallback-to-current-host");

                GameObject missingTargetRoot = CreateRoot("H222 Missing Target Trigger", objects);
                RouteRequestTrigger missingTargetTrigger =
                    missingTargetRoot.AddComponent<RouteRequestTrigger>();
                var missingTargetFake = new QaFakeRouteRuntimePort();
                Require(missingTargetTrigger.TryBindRouteRuntime(
                    missingTargetFake,
                    out string missingTargetBindingIssue),
                    "Could not bind the missing-target Route trigger. " + missingTargetBindingIssue);
                missingTargetTrigger.RequestRoute();
                Require(missingTargetFake.RequestCallCount == 0 &&
                    missingTargetTrigger.LastRequestFailed &&
                    !missingTargetTrigger.IsRequestInFlight,
                    "Missing Route target called the Route runtime port.");
                completed.Add("target-missing-does-not-call-port");

                Require(completed.Count == 8,
                    "H2.2.2 Route request trigger binding smoke case count changed unexpectedly.");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = objects.Count - 1; index >= 0; index--)
                {
                    if (objects[index] != null) UnityEngine.Object.DestroyImmediate(objects[index]);
                }
            }
        }

        private static GameObject CreateRoot(string name, ICollection<UnityEngine.Object> objects)
        {
            var root = new GameObject(name);
            objects.Add(root);
            return root;
        }

        private static RouteAsset CreateTarget(string name, ICollection<UnityEngine.Object> objects)
        {
            RouteAsset target = ScriptableObject.CreateInstance<RouteAsset>();
            target.name = name;
            objects.Add(target);
            return target;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class RouteRequestRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Requests/Run Route Request Regression";
        private const string LogPrefix = "[ROUTE_REQUEST_REGRESSION]";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();

            try
            {
                Require(EditorApplication.isPlaying,
                    "Route Request Regression requires Play Mode.");
                completed.Add("play-mode-required");
                Require(
                    global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host) && host != null,
                    "Route Request Regression requires an initialized global FrameworkRuntimeHost.");
                IRouteRuntimePort hostRouteRuntime = host;
                Require(hostRouteRuntime != null,
                    "Global FrameworkRuntimeHost did not expose the Route runtime port.");
                completed.Add("runtime-host-available");

                RunCompositionCases(completed, objects);

                RouteAsset target = CreateTarget("Route Regression Target", objects);
                GameObject boundRoot = CreateRoot("Route Regression Bound Trigger", objects);
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

                GameObject unboundRoot = CreateRoot("Route Regression Unbound Trigger", objects);
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

                GameObject missingTargetRoot = CreateRoot("Route Regression Missing Target Trigger", objects);
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

                Require(completed.Count == 13,
                    "Route Request Regression case count changed unexpectedly.");
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

        private static void RunCompositionCases(
            ICollection<string> completed,
            ICollection<UnityEngine.Object> objects)
        {
            GameObject emptyRoot = CreateRoot(
                "Route Regression Empty Root",
                objects);
            RouteRequestTriggerBindingResult absent =
                RouteRequestTriggerBinding.TryBind(
                    new[] { emptyRoot },
                    new QaFakeRouteRuntimePort());
            Require(
                absent.Succeeded &&
                absent.Status == "OptionalAbsent" &&
                absent.RootCount == 1 &&
                absent.TriggerCount == 0,
                "Zero Route trigger composition did not remain optional. " +
                absent.Message);
            completed.Add("zero-triggers-optional");

            GameObject oneRoot = CreateRoot(
                "Route Regression One Trigger Root",
                objects);
            RouteRequestTrigger oneTrigger =
                oneRoot.AddComponent<RouteRequestTrigger>();
            var oneRuntime = new QaFakeRouteRuntimePort();
            RouteRequestTriggerBindingResult one =
                RouteRequestTriggerBinding.TryBind(
                    new[] { oneRoot },
                    oneRuntime);
            Require(
                one.Succeeded &&
                one.TriggerCount == 1 &&
                one.BoundCount == 1 &&
                one.IdempotentCount == 0 &&
                one.RejectedCount == 0 &&
                oneTrigger.HasRouteRuntimeBinding,
                "One Route trigger composition was not bound. " +
                one.Message);
            completed.Add("one-trigger-bound");

            GameObject multipleRoot = CreateRoot(
                "Route Regression Multiple Trigger Root",
                objects);
            RouteRequestTrigger firstMultiple =
                multipleRoot.AddComponent<RouteRequestTrigger>();
            GameObject multipleChild =
                new GameObject("Route Regression Multiple Trigger Child");
            multipleChild.transform.SetParent(
                multipleRoot.transform,
                false);
            objects.Add(multipleChild);
            RouteRequestTrigger secondMultiple =
                multipleChild.AddComponent<RouteRequestTrigger>();
            var multipleRuntime = new QaFakeRouteRuntimePort();
            RouteRequestTriggerBindingResult multiple =
                RouteRequestTriggerBinding.TryBind(
                    new[] { multipleRoot },
                    multipleRuntime);
            Require(
                multiple.Succeeded &&
                multiple.TriggerCount == 2 &&
                multiple.BoundCount == 2 &&
                multiple.RejectedCount == 0 &&
                firstMultiple.HasRouteRuntimeBinding &&
                secondMultiple.HasRouteRuntimeBinding,
                "Multiple Route trigger composition did not bind every trigger. " +
                multiple.Message);
            completed.Add("multiple-triggers-all-bound");

            GameObject idempotentRoot = CreateRoot(
                "Route Regression Idempotent Root",
                objects);
            RouteRequestTrigger idempotentTrigger =
                idempotentRoot.AddComponent<RouteRequestTrigger>();
            var idempotentRuntime = new QaFakeRouteRuntimePort();
            Require(
                idempotentTrigger.TryBindRouteRuntime(
                    idempotentRuntime,
                    out string idempotentIssue),
                "Could not prebind the idempotent Route trigger. " +
                idempotentIssue);
            RouteRequestTriggerBindingResult idempotent =
                RouteRequestTriggerBinding.TryBind(
                    new[] { idempotentRoot },
                    idempotentRuntime);
            Require(
                idempotent.Succeeded &&
                idempotent.BoundCount == 0 &&
                idempotent.IdempotentCount == 1 &&
                idempotent.RejectedCount == 0,
                "Same Route runtime composition was not idempotent. " +
                idempotent.Message);
            completed.Add("composition-same-port-idempotent");

            RouteAsset incompatibleTarget = CreateTarget(
                "Route Regression Incompatible Target",
                objects);
            GameObject incompatibleRoot = CreateRoot(
                "Route Regression Incompatible Root",
                objects);
            RouteRequestTrigger compatibleTrigger =
                incompatibleRoot.AddComponent<RouteRequestTrigger>();
            compatibleTrigger.TargetRoute = incompatibleTarget;
            GameObject incompatibleChild =
                new GameObject("Route Regression Incompatible Child");
            incompatibleChild.transform.SetParent(
                incompatibleRoot.transform,
                false);
            objects.Add(incompatibleChild);
            RouteRequestTrigger incompatibleTrigger =
                incompatibleChild.AddComponent<RouteRequestTrigger>();
            incompatibleTrigger.TargetRoute = incompatibleTarget;
            var compositionRuntime = new QaFakeRouteRuntimePort();
            var originalRuntime = new QaFakeRouteRuntimePort();
            Require(
                incompatibleTrigger.TryBindRouteRuntime(
                    originalRuntime,
                    out string incompatibleIssue),
                "Could not prebind the incompatible Route trigger. " +
                incompatibleIssue);
            RouteRequestTriggerBindingResult incompatible =
                RouteRequestTriggerBinding.TryBind(
                    new[] { incompatibleRoot },
                    compositionRuntime);
            Require(
                !incompatible.Succeeded &&
                incompatible.Status == "RejectedTriggerBinding" &&
                incompatible.RootCount == 1 &&
                incompatible.TriggerCount == 2 &&
                incompatible.BoundCount == 1 &&
                incompatible.IdempotentCount == 0 &&
                incompatible.RejectedCount == 1 &&
                incompatible.Message.Contains(
                    "Route Regression Incompatible Child") &&
                incompatible.Message.Contains("different port") &&
                incompatible.Message.Contains("current lifetime"),
                "Incompatible Route trigger was not rejected structurally. " +
                incompatible.Message);
            compatibleTrigger.RequestRoute();
            Require(
                compositionRuntime.RequestCallCount == 1 &&
                originalRuntime.RequestCallCount == 0,
                "Compatible Route trigger did not preserve the composition authority.");
            incompatibleTrigger.RequestRoute();
            Require(
                compositionRuntime.RequestCallCount == 1 &&
                originalRuntime.RequestCallCount == 1,
                "Incompatible Route trigger did not preserve its original authority.");
            completed.Add("composition-different-port-rejected");
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

using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH222RouteRequestTriggerCompositionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/H2.2.2 Run Route Request Trigger Composition Smoke";
        private const string LogPrefix =
            "[H222_ROUTE_REQUEST_TRIGGER_COMPOSITION_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();

            try
            {
                GameObject emptyRoot = CreateRoot("H222 Empty Root", objects);
                RouteRequestTriggerBindingResult absent =
                    RouteRequestTriggerBinding.TryBind(
                        new[] { emptyRoot }, new QaFakeRouteRuntimePort());
                Require(absent.Succeeded && absent.Status == "OptionalAbsent" &&
                    absent.RootCount == 1 && absent.TriggerCount == 0,
                    "Zero Route trigger composition did not remain optional. " + absent.Message);
                completed.Add("zero-triggers-optional");

                GameObject oneRoot = CreateRoot("H222 One Trigger Root", objects);
                RouteRequestTrigger oneTrigger = oneRoot.AddComponent<RouteRequestTrigger>();
                var oneRuntime = new QaFakeRouteRuntimePort();
                RouteRequestTriggerBindingResult one =
                    RouteRequestTriggerBinding.TryBind(new[] { oneRoot }, oneRuntime);
                Require(one.Succeeded && one.TriggerCount == 1 &&
                    one.BoundCount == 1 && one.IdempotentCount == 0 &&
                    one.RejectedCount == 0 && oneTrigger.HasRouteRuntimeBinding,
                    "One Route trigger composition was not bound. " + one.Message);
                completed.Add("one-trigger-bound");

                GameObject multipleRoot = CreateRoot("H222 Multiple Trigger Root", objects);
                RouteRequestTrigger firstMultiple =
                    multipleRoot.AddComponent<RouteRequestTrigger>();
                GameObject multipleChild = new GameObject("H222 Multiple Child");
                multipleChild.transform.SetParent(multipleRoot.transform, false);
                objects.Add(multipleChild);
                RouteRequestTrigger secondMultiple =
                    multipleChild.AddComponent<RouteRequestTrigger>();
                var multipleRuntime = new QaFakeRouteRuntimePort();
                RouteRequestTriggerBindingResult multiple =
                    RouteRequestTriggerBinding.TryBind(new[] { multipleRoot }, multipleRuntime);
                Require(multiple.Succeeded && multiple.TriggerCount == 2 &&
                    multiple.BoundCount == 2 && multiple.RejectedCount == 0 &&
                    firstMultiple.HasRouteRuntimeBinding &&
                    secondMultiple.HasRouteRuntimeBinding,
                    "Multiple Route trigger composition did not bind every trigger. " + multiple.Message);
                completed.Add("multiple-triggers-all-bound");

                GameObject idempotentRoot = CreateRoot("H222 Idempotent Root", objects);
                RouteRequestTrigger idempotentTrigger =
                    idempotentRoot.AddComponent<RouteRequestTrigger>();
                var idempotentRuntime = new QaFakeRouteRuntimePort();
                Require(idempotentTrigger.TryBindRouteRuntime(
                    idempotentRuntime,
                    out string idempotentIssue),
                    "Could not prebind the idempotent Route trigger. " + idempotentIssue);
                RouteRequestTriggerBindingResult idempotent =
                    RouteRequestTriggerBinding.TryBind(
                        new[] { idempotentRoot }, idempotentRuntime);
                Require(idempotent.Succeeded && idempotent.BoundCount == 0 &&
                    idempotent.IdempotentCount == 1 && idempotent.RejectedCount == 0,
                    "Same Route runtime composition was not idempotent. " + idempotent.Message);
                completed.Add("same-port-idempotent");

                RouteAsset target = CreateTarget("H222 Incompatible Target", objects);
                GameObject incompatibleRoot = CreateRoot("H222 Incompatible Root", objects);
                RouteRequestTrigger compatibleTrigger =
                    incompatibleRoot.AddComponent<RouteRequestTrigger>();
                compatibleTrigger.TargetRoute = target;
                GameObject incompatibleChild = new GameObject("H222 Incompatible Child");
                incompatibleChild.transform.SetParent(incompatibleRoot.transform, false);
                objects.Add(incompatibleChild);
                RouteRequestTrigger incompatibleTrigger =
                    incompatibleChild.AddComponent<RouteRequestTrigger>();
                incompatibleTrigger.TargetRoute = target;
                var compositionRuntime = new QaFakeRouteRuntimePort();
                var originalRuntime = new QaFakeRouteRuntimePort();
                Require(incompatibleTrigger.TryBindRouteRuntime(
                    originalRuntime,
                    out string incompatibleIssue),
                    "Could not prebind the incompatible Route trigger. " + incompatibleIssue);
                RouteRequestTriggerBindingResult incompatible =
                    RouteRequestTriggerBinding.TryBind(
                        new[] { incompatibleRoot }, compositionRuntime);
                Require(!incompatible.Succeeded &&
                    incompatible.Status == "RejectedTriggerBinding" &&
                    incompatible.RootCount == 1 && incompatible.TriggerCount == 2 &&
                    incompatible.BoundCount == 1 && incompatible.IdempotentCount == 0 &&
                    incompatible.RejectedCount == 1 &&
                    incompatible.Message.Contains("H222 Incompatible Child") &&
                    incompatible.Message.Contains("different port") &&
                    incompatible.Message.Contains("current lifetime"),
                    "Incompatible Route trigger was not rejected structurally. " + incompatible.Message);
                compatibleTrigger.RequestRoute();
                Require(compositionRuntime.RequestCallCount == 1 &&
                    originalRuntime.RequestCallCount == 0,
                    "Compatible Route trigger did not preserve the composition authority.");
                incompatibleTrigger.RequestRoute();
                Require(compositionRuntime.RequestCallCount == 1 &&
                    originalRuntime.RequestCallCount == 1,
                    "Incompatible Route trigger did not preserve its original authority.");
                completed.Add("incompatible-trigger-rejected");

                Require(completed.Count == 5,
                    "H2.2.2 Route request trigger composition smoke case count changed unexpectedly.");
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

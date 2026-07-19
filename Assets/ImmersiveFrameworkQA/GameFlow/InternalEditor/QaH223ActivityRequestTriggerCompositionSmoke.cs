using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH223ActivityRequestTriggerCompositionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/H2.2.3 Run Activity Request Trigger Composition Smoke";
        private const string LogPrefix =
            "[H223_ACTIVITY_REQUEST_TRIGGER_COMPOSITION_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();

            try
            {
                GameObject emptyRoot = CreateRoot("H223 Empty Root", objects);
                ActivityRequestTriggerBindingResult absent =
                    ActivityRequestTriggerBinding.TryBind(
                        new[] { emptyRoot },
                        new QaFakeActivityRuntimePort());
                Require(
                    absent.Succeeded &&
                    absent.Status == "OptionalAbsent" &&
                    absent.RootCount == 1 &&
                    absent.TriggerCount == 0,
                    "Zero Activity trigger composition did not remain optional. " +
                    absent.Message);
                completed.Add("zero-triggers-optional");

                GameObject oneRoot = CreateRoot("H223 One Trigger Root", objects);
                ActivityRequestTrigger oneTrigger =
                    oneRoot.AddComponent<ActivityRequestTrigger>();
                var oneRuntime = new QaFakeActivityRuntimePort();
                ActivityRequestTriggerBindingResult one =
                    ActivityRequestTriggerBinding.TryBind(
                        new[] { oneRoot },
                        oneRuntime);
                Require(
                    one.Succeeded &&
                    one.TriggerCount == 1 &&
                    one.BoundCount == 1 &&
                    one.IdempotentCount == 0 &&
                    one.RejectedCount == 0 &&
                    oneTrigger.HasActivityRuntimeBinding,
                    "One Activity trigger composition was not bound. " +
                    one.Message);
                completed.Add("one-trigger-bound");

                GameObject multipleRoot =
                    CreateRoot("H223 Multiple Trigger Root", objects);
                ActivityRequestTrigger firstMultiple =
                    multipleRoot.AddComponent<ActivityRequestTrigger>();
                GameObject multipleChild = new GameObject("H223 Multiple Child");
                multipleChild.transform.SetParent(multipleRoot.transform, false);
                objects.Add(multipleChild);
                ActivityRequestTrigger secondMultiple =
                    multipleChild.AddComponent<ActivityRequestTrigger>();
                var multipleRuntime = new QaFakeActivityRuntimePort();
                ActivityRequestTriggerBindingResult multiple =
                    ActivityRequestTriggerBinding.TryBind(
                        new[] { multipleRoot },
                        multipleRuntime);
                Require(
                    multiple.Succeeded &&
                    multiple.TriggerCount == 2 &&
                    multiple.BoundCount == 2 &&
                    multiple.RejectedCount == 0 &&
                    firstMultiple.HasActivityRuntimeBinding &&
                    secondMultiple.HasActivityRuntimeBinding,
                    "Multiple Activity trigger composition did not bind every trigger. " +
                    multiple.Message);
                completed.Add("multiple-triggers-all-bound");

                GameObject idempotentRoot =
                    CreateRoot("H223 Idempotent Root", objects);
                ActivityRequestTrigger idempotentTrigger =
                    idempotentRoot.AddComponent<ActivityRequestTrigger>();
                var idempotentRuntime = new QaFakeActivityRuntimePort();
                Require(
                    idempotentTrigger.TryBindActivityRuntime(
                        idempotentRuntime,
                        out string idempotentIssue),
                    "Could not prebind the idempotent Activity trigger. " +
                    idempotentIssue);
                ActivityRequestTriggerBindingResult idempotent =
                    ActivityRequestTriggerBinding.TryBind(
                        new[] { idempotentRoot },
                        idempotentRuntime);
                Require(
                    idempotent.Succeeded &&
                    idempotent.BoundCount == 0 &&
                    idempotent.IdempotentCount == 1 &&
                    idempotent.RejectedCount == 0,
                    "Same Activity runtime composition was not idempotent. " +
                    idempotent.Message);
                completed.Add("same-port-idempotent");

                ActivityAsset target =
                    CreateTarget("H223 Incompatible Target", objects);
                GameObject incompatibleRoot =
                    CreateRoot("H223 Incompatible Root", objects);
                ActivityRequestTrigger compatibleTrigger =
                    incompatibleRoot.AddComponent<ActivityRequestTrigger>();
                compatibleTrigger.TargetActivity = target;
                GameObject incompatibleChild =
                    new GameObject("H223 Incompatible Child");
                incompatibleChild.transform.SetParent(
                    incompatibleRoot.transform,
                    false);
                objects.Add(incompatibleChild);
                ActivityRequestTrigger incompatibleTrigger =
                    incompatibleChild.AddComponent<ActivityRequestTrigger>();
                incompatibleTrigger.TargetActivity = target;
                var compositionRuntime = new QaFakeActivityRuntimePort();
                var originalRuntime = new QaFakeActivityRuntimePort();
                Require(
                    incompatibleTrigger.TryBindActivityRuntime(
                        originalRuntime,
                        out string incompatibleIssue),
                    "Could not prebind the incompatible Activity trigger. " +
                    incompatibleIssue);
                ActivityRequestTriggerBindingResult incompatible =
                    ActivityRequestTriggerBinding.TryBind(
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
                    incompatible.Message.Contains("H223 Incompatible Child") &&
                    incompatible.Message.Contains("different port") &&
                    incompatible.Message.Contains("current lifetime"),
                    "Incompatible Activity trigger was not rejected structurally. " +
                    incompatible.Message);
                compatibleTrigger.RequestActivity();
                Require(
                    compositionRuntime.RequestActivityCallCount == 1 &&
                    originalRuntime.RequestActivityCallCount == 0,
                    "Compatible Activity trigger did not preserve the composition authority.");
                incompatibleTrigger.RequestActivity();
                Require(
                    compositionRuntime.RequestActivityCallCount == 1 &&
                    originalRuntime.RequestActivityCallCount == 1,
                    "Incompatible Activity trigger did not preserve its original authority.");
                completed.Add("incompatible-trigger-rejected");

                Require(
                    completed.Count == 5,
                    "H2.2.3 Activity request trigger composition smoke case count changed unexpectedly.");
                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = objects.Count - 1; index >= 0; index--)
                {
                    if (objects[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(objects[index]);
                    }
                }
            }
        }

        private static GameObject CreateRoot(
            string name,
            ICollection<UnityEngine.Object> objects)
        {
            var root = new GameObject(name);
            objects.Add(root);
            return root;
        }

        private static ActivityAsset CreateTarget(
            string name,
            ICollection<UnityEngine.Object> objects)
        {
            ActivityAsset target = ScriptableObject.CreateInstance<ActivityAsset>();
            target.name = name;
            objects.Add(target);
            return target;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

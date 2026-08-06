using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class ActivityRequestRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Requests/Run Activity Request Regression";
        private const string LogPrefix = "[ACTIVITY_REQUEST_REGRESSION]";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "Activity Request Regression requires Play Mode.");
                completed.Add("play-mode-required");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host) && host != null,
                    "Activity Request Regression requires an initialized global FrameworkRuntimeHost.");
                IActivityRuntimePort hostActivityRuntime = host;
                Require(
                    hostActivityRuntime != null,
                    "Global FrameworkRuntimeHost did not expose the Activity runtime port.");
                completed.Add("runtime-host-available");

                RunCompositionCases(completed, objects);

                ActivityAsset target = CreateTarget(
                    "Activity Regression Target",
                    objects);
                GameObject boundRoot = CreateRoot(
                    "Activity Regression Bound Trigger",
                    objects);
                ActivityRequestTrigger boundTrigger =
                    boundRoot.AddComponent<ActivityRequestTrigger>();
                boundTrigger.TargetActivity = target;
                var fakeA = new QaFakeActivityRuntimePort();
                Require(
                    boundTrigger.TryBindActivityRuntime(fakeA, out string firstIssue) &&
                    boundTrigger.HasActivityRuntimeBinding,
                    "Initial Activity runtime binding was not accepted. " + firstIssue);
                completed.Add("initial-binding-accepted");

                Require(
                    boundTrigger.TryBindActivityRuntime(fakeA, out string sameIssue) &&
                    boundTrigger.ActivityRuntimeBindingDiagnostic.Contains("idempotent"),
                    "Same Activity runtime rebinding was not idempotent. " + sameIssue);
                completed.Add("same-port-rebind-idempotent");

                var fakeB = new QaFakeActivityRuntimePort();
                Require(
                    !boundTrigger.TryBindActivityRuntime(fakeB, out string differentIssue) &&
                    boundTrigger.HasActivityRuntimeBinding &&
                    differentIssue.Contains("different port"),
                    "Different Activity runtime rebinding was not rejected. " +
                    differentIssue);
                completed.Add("different-port-rebind-rejected");

                int submitted = 0;
                int completedEvents = 0;
                using (boundTrigger.SubscribeRequestEvents(requestEvent =>
                       {
                           if (requestEvent.Phase == FlowRequestEventPhase.Submitted)
                           {
                               submitted++;
                           }

                           if (requestEvent.Phase == FlowRequestEventPhase.Completed)
                           {
                               completedEvents++;
                           }
                       }))
                {
                    boundTrigger.RequestActivity();
                }

                Require(
                    fakeA.RequestActivityCallCount == 1 &&
                    fakeA.LastTargetActivity == target &&
                    fakeA.LastSource == "ActivityRequestTrigger" &&
                    fakeA.LastReason == target.ActivityName &&
                    fakeB.RequestActivityCallCount == 0 &&
                    submitted == 1 &&
                    completedEvents == 1 &&
                    boundTrigger.LastRequestSucceeded &&
                    !boundTrigger.IsRequestInFlight,
                    "Bound Activity trigger did not forward the request only to fake A.");
                completed.Add("request-forwarded-to-bound-port");

                int clearSubmitted = 0;
                int clearCompleted = 0;
                using (boundTrigger.SubscribeRequestEvents(requestEvent =>
                       {
                           if (requestEvent.ClearsActivity &&
                               requestEvent.Phase == FlowRequestEventPhase.Submitted)
                           {
                               clearSubmitted++;
                           }

                           if (requestEvent.ClearsActivity &&
                               requestEvent.Phase == FlowRequestEventPhase.Completed)
                           {
                               clearCompleted++;
                           }
                       }))
                {
                    boundTrigger.ClearActivity();
                }

                Require(
                    fakeA.ClearActivityCallCount == 1 &&
                    fakeA.LastTargetActivity == null &&
                    fakeA.LastSource == "ActivityRequestTrigger" &&
                    fakeA.LastReason == "Clear Activity" &&
                    fakeB.ClearActivityCallCount == 0 &&
                    clearSubmitted == 1 &&
                    clearCompleted == 1 &&
                    boundTrigger.LastRequestSucceeded &&
                    boundTrigger.LastRequestClearedActivity &&
                    !boundTrigger.IsRequestInFlight,
                    "Bound Activity trigger did not forward clear only to fake A.");
                completed.Add("clear-forwarded-to-bound-port");

                GameObject unboundRoot = CreateRoot(
                    "Activity Regression Unbound Trigger",
                    objects);
                ActivityRequestTrigger unboundTrigger =
                    unboundRoot.AddComponent<ActivityRequestTrigger>();
                unboundTrigger.TargetActivity = target;
                int unboundSubmitted = 0;
                int unboundCompleted = 0;
                using (unboundTrigger.SubscribeRequestEvents(requestEvent =>
                       {
                           if (requestEvent.Phase == FlowRequestEventPhase.Submitted)
                           {
                               unboundSubmitted++;
                           }

                           if (requestEvent.Phase == FlowRequestEventPhase.Completed)
                           {
                               unboundCompleted++;
                           }
                       }))
                {
                    unboundTrigger.RequestActivity();
                }

                Require(
                    unboundSubmitted == 0 &&
                    unboundCompleted == 1 &&
                    !unboundTrigger.IsRequestInFlight &&
                    unboundTrigger.LastRequestFailed &&
                    unboundTrigger.LastMessage.Contains("Activity runtime port is not bound") &&
                    unboundTrigger.ActivityRuntimeBindingDiagnostic.Contains(
                        "Activity runtime port is not bound"),
                    "Unbound Activity trigger submitted or fell back to the global host.");
                completed.Add("unbound-trigger-does-not-fallback-to-current-host");

                GameObject missingTargetRoot =
                    CreateRoot(
                        "Activity Regression Missing Target Trigger",
                        objects);
                ActivityRequestTrigger missingTargetTrigger =
                    missingTargetRoot.AddComponent<ActivityRequestTrigger>();
                var missingTargetFake = new QaFakeActivityRuntimePort();
                Require(
                    missingTargetTrigger.TryBindActivityRuntime(
                        missingTargetFake,
                        out string missingTargetBindingIssue),
                    "Could not bind the missing-target Activity trigger. " +
                    missingTargetBindingIssue);
                missingTargetTrigger.RequestActivity();
                Require(
                    missingTargetFake.RequestActivityCallCount == 0 &&
                    missingTargetTrigger.LastRequestFailed &&
                    !missingTargetTrigger.IsRequestInFlight,
                    "Missing Activity target called the Activity runtime port.");
                completed.Add("target-missing-does-not-call-port");

                Require(
                    completed.Count == 14,
                    "Activity Request Regression case count changed unexpectedly.");
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

        private static void RunCompositionCases(
            ICollection<string> completed,
            ICollection<UnityEngine.Object> objects)
        {
            GameObject emptyRoot = CreateRoot(
                "Activity Regression Empty Root",
                objects);
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

            GameObject oneRoot = CreateRoot(
                "Activity Regression One Trigger Root",
                objects);
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

            GameObject multipleRoot = CreateRoot(
                "Activity Regression Multiple Trigger Root",
                objects);
            ActivityRequestTrigger firstMultiple =
                multipleRoot.AddComponent<ActivityRequestTrigger>();
            GameObject multipleChild =
                new GameObject("Activity Regression Multiple Trigger Child");
            multipleChild.transform.SetParent(
                multipleRoot.transform,
                false);
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

            GameObject idempotentRoot = CreateRoot(
                "Activity Regression Idempotent Root",
                objects);
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
            completed.Add("composition-same-port-idempotent");

            ActivityAsset incompatibleTarget = CreateTarget(
                "Activity Regression Incompatible Target",
                objects);
            GameObject incompatibleRoot = CreateRoot(
                "Activity Regression Incompatible Root",
                objects);
            ActivityRequestTrigger compatibleTrigger =
                incompatibleRoot.AddComponent<ActivityRequestTrigger>();
            compatibleTrigger.TargetActivity = incompatibleTarget;
            GameObject incompatibleChild =
                new GameObject("Activity Regression Incompatible Child");
            incompatibleChild.transform.SetParent(
                incompatibleRoot.transform,
                false);
            objects.Add(incompatibleChild);
            ActivityRequestTrigger incompatibleTrigger =
                incompatibleChild.AddComponent<ActivityRequestTrigger>();
            incompatibleTrigger.TargetActivity = incompatibleTarget;
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
                incompatible.Message.Contains(
                    "Activity Regression Incompatible Child") &&
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
            completed.Add("composition-different-port-rejected");
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

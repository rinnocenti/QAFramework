using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH223ActivityRequestTriggerBindingSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/H2.2.3 Run Activity Request Trigger Binding Smoke";
        private const string LogPrefix = "[H223_ACTIVITY_REQUEST_TRIGGER_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "H2.2.3 Activity request trigger binding smoke requires Play Mode.");
                completed.Add("play-mode-required");

                Require(
                    FrameworkRuntimeHost.TryGetCurrent(
                        out FrameworkRuntimeHost host) && host != null,
                    "H2.2.3 Activity request trigger binding smoke requires an initialized global FrameworkRuntimeHost.");
                IActivityRuntimePort hostActivityRuntime = host;
                Require(
                    hostActivityRuntime != null,
                    "Global FrameworkRuntimeHost did not expose the Activity runtime port.");
                completed.Add("runtime-host-available");

                ActivityAsset target = CreateTarget("H223 Target", objects);
                GameObject boundRoot = CreateRoot("H223 Bound Trigger", objects);
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

                GameObject unboundRoot = CreateRoot("H223 Unbound Trigger", objects);
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
                    CreateRoot("H223 Missing Target Trigger", objects);
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
                    completed.Count == 9,
                    "H2.2.3 Activity request trigger binding smoke case count changed unexpectedly.");
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

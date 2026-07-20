using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.CycleReset;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH224RouteCycleResetTriggerBindingSmoke
    {
        private const string LogPrefix = "[H224_ROUTE_CYCLE_RESET_TRIGGER_SMOKE]";
        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.4 Run Route Cycle Reset Trigger Binding Smoke", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.4 Run Route Cycle Reset Trigger Binding Smoke")]
        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();
            try
            {
                Require(EditorApplication.isPlaying, "H2.2.4 binding smoke requires Play Mode.");
                completed.Add("play-mode-required");
                Require(global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(out FrameworkRuntimeHost host) && host != null, "H2.2.4 binding smoke requires FrameworkRuntimeHost.");
                IRouteCycleResetRuntimePort hostPort = host;
                Require(hostPort != null, "FrameworkRuntimeHost did not expose the Route Cycle Reset runtime port.");
                completed.Add("runtime-host-available");

                RouteCycleResetTrigger trigger = CreateTrigger("H224 Bound", objects);
                var fakeA = new QaFakeRouteCycleResetRuntimePort { Result = Result(CycleResetStatus.Succeeded) };
                Require(trigger.TryBindRouteCycleResetRuntime(fakeA, out string issue) && trigger.HasRouteCycleResetRuntimeBinding, "Initial binding failed. " + issue);
                completed.Add("initial-binding-accepted");
                Require(trigger.TryBindRouteCycleResetRuntime(fakeA, out issue) && trigger.RouteCycleResetRuntimeBindingDiagnostic.Contains("idempotent"), "Idempotent binding failed. " + issue);
                completed.Add("same-port-rebind-idempotent");
                Require(!trigger.TryBindRouteCycleResetRuntime(new QaFakeRouteCycleResetRuntimePort(), out issue) && issue.Contains("different port"), "Different binding was not rejected. " + issue);
                completed.Add("different-port-rebind-rejected");

                int submitted = 0; int finished = 0;
                using (trigger.SubscribeRequestEvents(e => { if (e.Phase == FlowRequestEventPhase.Submitted) submitted++; if (e.Phase == FlowRequestEventPhase.Completed) finished++; }))
                {
                    trigger.RequestRouteCycleReset();
                }
                Require(fakeA.CallCount == 1 && fakeA.LastSource == "RouteCycleResetTrigger" && fakeA.LastReason == "Route Cycle Reset" && submitted == 1 && finished == 1, "Bound trigger did not forward exact source/reason.");
                completed.Add("request-forwarded-with-exact-source-and-reason");
                Require(trigger.LastRequestSucceeded, "Succeeded result was not mapped to succeeded.");
                completed.Add("succeeded-result-mapped-to-succeeded");

                RouteCycleResetTrigger warnings = CreateTrigger("H224 Warnings", objects);
                Require(warnings.TryBindRouteCycleResetRuntime(new QaFakeRouteCycleResetRuntimePort { Result = Result(CycleResetStatus.CompletedWithWarnings) }, out issue), issue);
                warnings.RequestRouteCycleReset();
                Require(warnings.LastRequestSucceeded, "Warnings result was not mapped to succeeded.");
                completed.Add("warnings-result-mapped-to-succeeded");
                RouteCycleResetTrigger failed = CreateTrigger("H224 Failed", objects);
                Require(failed.TryBindRouteCycleResetRuntime(new QaFakeRouteCycleResetRuntimePort { Result = Result(CycleResetStatus.Failed) }, out issue), issue);
                failed.RequestRouteCycleReset();
                Require(failed.LastRequestFailed, "Failed result was not mapped to failed.");
                completed.Add("failed-result-mapped-to-failed");

                RouteCycleResetTrigger unbound = CreateTrigger("H224 Unbound", objects);
                int unboundSubmitted = 0;
                using (unbound.SubscribeRequestEvents(e => { if (e.Phase == FlowRequestEventPhase.Submitted) unboundSubmitted++; })) { unbound.RequestRouteCycleReset(); }
                Require(unboundSubmitted == 0 && unbound.LastRequestFailed && unbound.RouteCycleResetRuntimeBindingDiagnostic.Contains("not bound"), "Unbound trigger submitted or fell back to the current host.");
                completed.Add("unbound-trigger-does-not-fallback-to-current-host");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception) { Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'."); throw; }
            finally { foreach (UnityEngine.Object item in objects) if (item != null) UnityEngine.Object.DestroyImmediate(item); }
        }

        private static RouteCycleResetTrigger CreateTrigger(string name, ICollection<UnityEngine.Object> objects) { var root = new GameObject(name); objects.Add(root); return root.AddComponent<RouteCycleResetTrigger>(); }
        private static CycleResetResult Result(CycleResetStatus status) => new CycleResetResult(default, status, Array.Empty<CycleResetParticipantResult>(), Array.Empty<CycleResetIssue>(), "qa", "qa", "qa");
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}

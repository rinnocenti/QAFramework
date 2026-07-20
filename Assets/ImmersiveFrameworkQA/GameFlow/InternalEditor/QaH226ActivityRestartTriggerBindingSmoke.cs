using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Reset;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH226ActivityRestartTriggerBindingSmoke
    {
        private const string LogPrefix = "[H226_ACTIVITY_RESTART_TRIGGER_BINDING_SMOKE]";

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.6 Run Activity Restart Trigger Binding Smoke", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.6 Run Activity Restart Trigger Binding Smoke")]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        public static async Task RunInternalAsync()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();
            try
            {
                Require(EditorApplication.isPlaying, "H2.2.6 binding smoke requires Play Mode.");
                completed.Add("play-mode-required");
                Require(global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(out FrameworkRuntimeHost host) && host != null, "H2.2.6 binding smoke requires FrameworkRuntimeHost.");
                IActivityRestartRuntimePort hostPort = host;
                Require(hostPort != null, "FrameworkRuntimeHost did not expose Activity Restart runtime port.");
                completed.Add("runtime-host-available");

                ActivityAsset target = CreateTarget("H226 Target", objects);
                ActivityRestartTrigger trigger = CreateTrigger("H226 Bound", objects);
                trigger.ConfigureForQa(target, false, true, "h226-authoring", ResetSelectionMode.ExplicitSubjects, Array.Empty<ResetSubjectReference>(), true, true, false, true);
                var successPort = new QaFakeActivityRestartRuntimePort { Result = Result(ActivityRestartResultStatus.Succeeded, target) };
                Require(trigger.TryBindActivityRestartRuntime(successPort, out string issue) && trigger.HasActivityRestartRuntimeBinding, "Initial binding failed. " + issue);
                completed.Add("initial-binding-accepted");
                Require(trigger.TryBindActivityRestartRuntime(successPort, out issue) && trigger.ActivityRestartRuntimeBindingDiagnostic.Contains("idempotent"), "Idempotent binding failed. " + issue);
                completed.Add("same-port-rebind-idempotent");
                Require(!trigger.TryBindActivityRestartRuntime(new QaFakeActivityRestartRuntimePort(), out issue) && issue.Contains("different port"), "Different binding was not rejected. " + issue);
                completed.Add("different-port-rebind-rejected");

                await trigger.RequestActivityRestartAsync();
                Require(successPort.CallCount == 1 && successPort.LastTargetActivity == target && !successPort.LastUseCurrentActivityWhenTargetMissing && successPort.LastRequireTargetActivityIsCurrent && ReferenceEquals(successPort.LastResetSelection, trigger.ResetSelection) && successPort.LastSource == "ActivityRestartTrigger" && successPort.LastReason == "h226-authoring", "Trigger did not forward exact authored fields and ResetSelection.");
                completed.Add("authored-fields-and-reset-selection-forwarded-exactly");
                Require(trigger.LastRequestSucceeded && trigger.LastResultStatus == ActivityRestartResultStatus.Succeeded, "Succeeded result mapping failed.");
                completed.Add("succeeded-result-mapped-to-succeeded");

                ActivityRestartTrigger warnings = CreateTrigger("H226 Warnings", objects);
                Require(warnings.TryBindActivityRestartRuntime(new QaFakeActivityRestartRuntimePort { Result = Result(ActivityRestartResultStatus.CompletedWithWarnings, target) }, out issue), issue);
                await warnings.RequestActivityRestartAsync();
                Require(warnings.LastRequestSucceeded, "Warnings result mapping failed.");
                completed.Add("warnings-result-mapped-to-succeeded");

                ActivityRestartTrigger failed = CreateTrigger("H226 Failed", objects);
                Require(failed.TryBindActivityRestartRuntime(new QaFakeActivityRestartRuntimePort { Result = Result(ActivityRestartResultStatus.ResetExecutionFailed, target) }, out issue), issue);
                await failed.RequestActivityRestartAsync();
                Require(failed.LastRequestFailed, "Failure result mapping failed.");
                completed.Add("failure-result-mapped-to-failed");

                ActivityRestartTrigger unbound = CreateTrigger("H226 Unbound", objects);
                int submitted = 0;
                using (unbound.SubscribeRequestEvents(e => { if (e.Phase == FlowRequestEventPhase.Submitted) submitted++; }))
                {
                    await unbound.RequestActivityRestartAsync();
                }
                Require(submitted == 0 && unbound.LastRequestFailed && unbound.LastResultStatus == ActivityRestartResultStatus.RejectedRuntimeUnavailable && unbound.ActivityRestartRuntimeBindingDiagnostic.Contains("not bound"), "Unbound trigger submitted or fell back to current host.");
                completed.Add("unbound-trigger-does-not-fallback-to-current-host");

                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                foreach (UnityEngine.Object item in objects)
                {
                    if (item != null) UnityEngine.Object.Destroy(item);
                }
            }
        }

        private static ActivityRestartTrigger CreateTrigger(string name, ICollection<UnityEngine.Object> objects)
        {
            var root = new GameObject(name);
            objects.Add(root);
            return root.AddComponent<ActivityRestartTrigger>();
        }

        private static ActivityAsset CreateTarget(string name, ICollection<UnityEngine.Object> objects)
        {
            ActivityAsset target = ScriptableObject.CreateInstance<ActivityAsset>();
            target.name = name;
            objects.Add(target);
            return target;
        }

        private static ActivityRestartResult Result(ActivityRestartResultStatus status, ActivityAsset activity) =>
            new ActivityRestartResult(status, activity, activity != null ? activity.name : string.Empty, "qa", "qa", default, string.Empty, string.Empty, string.Empty, string.Empty, "qa");

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}

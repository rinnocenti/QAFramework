using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.CycleReset;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH225ActivityCycleResetTriggerBindingSmoke
    {
        private const string LogPrefix="[H225_ACTIVITY_CYCLE_RESET_TRIGGER_SMOKE]";
        private static bool ValidateRun()=>EditorApplication.isPlaying;
        public static void Run()
        {
            var completed=new List<string>();var objects=new List<UnityEngine.Object>();
            try
            {
                Require(EditorApplication.isPlaying,"H2.2.5 binding smoke requires Play Mode.");completed.Add("play-mode-required");
                Require(global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(out FrameworkRuntimeHost host)&&host!=null,"H2.2.5 binding smoke requires FrameworkRuntimeHost.");IActivityCycleResetRuntimePort hostPort=host;Require(hostPort!=null,"FrameworkRuntimeHost did not expose Activity Cycle Reset runtime port.");completed.Add("runtime-host-available");
                ActivityCycleResetTrigger trigger=Create("H225 Bound",objects);var fakeA=new QaFakeActivityCycleResetRuntimePort{Result=Result(CycleResetStatus.Succeeded)};
                Require(trigger.TryBindActivityCycleResetRuntime(fakeA,out string issue)&&trigger.HasActivityCycleResetRuntimeBinding,"Initial binding failed. "+issue);completed.Add("initial-binding-accepted");
                Require(trigger.TryBindActivityCycleResetRuntime(fakeA,out issue)&&trigger.ActivityCycleResetRuntimeBindingDiagnostic.Contains("idempotent"),"Idempotent binding failed. "+issue);completed.Add("same-port-rebind-idempotent");
                Require(!trigger.TryBindActivityCycleResetRuntime(new QaFakeActivityCycleResetRuntimePort(),out issue)&&issue.Contains("different port"),"Different binding was not rejected. "+issue);completed.Add("different-port-rebind-rejected");
                int submitted=0,finished=0;using(trigger.SubscribeRequestEvents(e=>{if(e.Phase==FlowRequestEventPhase.Submitted)submitted++;if(e.Phase==FlowRequestEventPhase.Completed)finished++;})){trigger.RequestActivityCycleReset();}
                Require(fakeA.CallCount==1&&fakeA.LastSource=="ActivityCycleResetTrigger"&&fakeA.LastReason=="Activity Cycle Reset"&&submitted==1&&finished==1,"Bound trigger did not forward exact source/reason.");completed.Add("request-forwarded-with-exact-source-and-reason");Require(trigger.LastRequestSucceeded,"Succeeded result mapping failed.");completed.Add("succeeded-result-mapped-to-succeeded");
                ActivityCycleResetTrigger warnings=Create("H225 Warnings",objects);Require(warnings.TryBindActivityCycleResetRuntime(new QaFakeActivityCycleResetRuntimePort{Result=Result(CycleResetStatus.CompletedWithWarnings)},out issue),issue);warnings.RequestActivityCycleReset();Require(warnings.LastRequestSucceeded,"Warnings result mapping failed.");completed.Add("warnings-result-mapped-to-succeeded");
                ActivityCycleResetTrigger failed=Create("H225 Failed",objects);Require(failed.TryBindActivityCycleResetRuntime(new QaFakeActivityCycleResetRuntimePort{Result=Result(CycleResetStatus.Failed)},out issue),issue);failed.RequestActivityCycleReset();Require(failed.LastRequestFailed,"Failed result mapping failed.");completed.Add("failed-result-mapped-to-failed");
                ActivityCycleResetTrigger unbound=Create("H225 Unbound",objects);int unboundSubmitted=0;using(unbound.SubscribeRequestEvents(e=>{if(e.Phase==FlowRequestEventPhase.Submitted)unboundSubmitted++;})){unbound.RequestActivityCycleReset();}Require(unboundSubmitted==0&&unbound.LastRequestFailed&&unbound.ActivityCycleResetRuntimeBindingDiagnostic.Contains("not bound"),"Unbound trigger submitted or fell back to current host.");completed.Add("unbound-trigger-does-not-fallback-to-current-host");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",",completed)}'.");
            } catch(Exception exception){Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'.");throw;} finally{foreach(UnityEngine.Object item in objects)if(item!=null)UnityEngine.Object.DestroyImmediate(item);}
        }
        private static ActivityCycleResetTrigger Create(string name,ICollection<UnityEngine.Object> objects){var root=new GameObject(name);objects.Add(root);return root.AddComponent<ActivityCycleResetTrigger>();}
        private static CycleResetResult Result(CycleResetStatus status)=>new CycleResetResult(default,status,Array.Empty<CycleResetParticipantResult>(),Array.Empty<CycleResetIssue>(),"qa","qa","qa");
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    }
}

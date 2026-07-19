using System;
using System.Collections.Generic;
using Immersive.Framework.CycleReset;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH224RouteCycleResetTriggerCompositionSmoke
    {
        private const string LogPrefix = "[H224_ROUTE_CYCLE_RESET_TRIGGER_COMPOSITION_SMOKE]";
        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.4 Run Route Cycle Reset Trigger Composition Smoke", true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;
        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.4 Run Route Cycle Reset Trigger Composition Smoke")]
        public static void Run()
        {
            var objects = new List<UnityEngine.Object>(); var completed = new List<string>();
            try
            {
                GameObject empty = Root("H224 Empty", objects);
                RouteCycleResetTriggerBindingResult absent = RouteCycleResetTriggerBinding.TryBind(new[] { empty, empty }, new QaFakeRouteCycleResetRuntimePort());
                Require(absent.Succeeded && absent.Status == "OptionalAbsent" && absent.RootCount == 1 && absent.TriggerCount == 0, absent.Message); completed.Add("zero-triggers-optional");
                GameObject one = Root("H224 One", objects); RouteCycleResetTrigger oneTrigger = one.AddComponent<RouteCycleResetTrigger>(); var port = new QaFakeRouteCycleResetRuntimePort();
                RouteCycleResetTriggerBindingResult bound = RouteCycleResetTriggerBinding.TryBind(new[] { one, one }, port);
                Require(bound.Succeeded && bound.RootCount == 1 && bound.TriggerCount == 1 && bound.BoundCount == 1 && oneTrigger.HasRouteCycleResetRuntimeBinding, bound.Message); completed.Add("one-trigger-bound");
                GameObject multiple = Root("H224 Multiple", objects); multiple.AddComponent<RouteCycleResetTrigger>(); GameObject child = new GameObject("H224 Child"); child.transform.SetParent(multiple.transform, false); objects.Add(child); child.AddComponent<RouteCycleResetTrigger>();
                RouteCycleResetTriggerBindingResult all = RouteCycleResetTriggerBinding.TryBind(new[] { multiple }, new QaFakeRouteCycleResetRuntimePort());
                Require(all.Succeeded && all.TriggerCount == 2 && all.BoundCount == 2, all.Message); completed.Add("multiple-triggers-all-bound");
                RouteCycleResetTriggerBindingResult same = RouteCycleResetTriggerBinding.TryBind(new[] { one }, port);
                Require(same.Succeeded && same.IdempotentCount == 1 && same.BoundCount == 0, same.Message); completed.Add("same-port-idempotent");
                GameObject incompatible = Root("H224 Incompatible", objects); incompatible.AddComponent<RouteCycleResetTrigger>(); GameObject incompatibleChild = new GameObject("H224 Incompatible Child"); incompatibleChild.transform.SetParent(incompatible.transform, false); objects.Add(incompatibleChild); RouteCycleResetTrigger incompatibleTrigger = incompatibleChild.AddComponent<RouteCycleResetTrigger>(); incompatibleTrigger.TryBindRouteCycleResetRuntime(new QaFakeRouteCycleResetRuntimePort(), out _);
                RouteCycleResetTriggerBindingResult rejected = RouteCycleResetTriggerBinding.TryBind(new[] { incompatible }, new QaFakeRouteCycleResetRuntimePort());
                Require(!rejected.Succeeded && rejected.Status == "RejectedTriggerBinding" && rejected.BoundCount == 1 && rejected.RejectedCount == 1, rejected.Message); completed.Add("incompatible-trigger-rejected");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception) { Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'."); throw; }
            finally { foreach (UnityEngine.Object item in objects) if (item != null) UnityEngine.Object.DestroyImmediate(item); }
        }
        private static GameObject Root(string name, ICollection<UnityEngine.Object> objects) { var root = new GameObject(name); objects.Add(root); return root; }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}

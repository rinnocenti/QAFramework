using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityRestart;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH226ActivityRestartTriggerCompositionSmoke
    {
        private const string LogPrefix = "[H226_ACTIVITY_RESTART_TRIGGER_COMPOSITION_SMOKE]";

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.6 Run Activity Restart Trigger Composition Smoke", true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.6 Run Activity Restart Trigger Composition Smoke")]
        public static void Run()
        {
            var objects = new List<UnityEngine.Object>();
            var completed = new List<string>();
            try
            {
                GameObject empty = Root("H226 Empty", objects);
                ActivityRestartTriggerBindingResult absent = ActivityRestartTriggerBinding.TryBind(new[] { empty, empty }, new QaFakeActivityRestartRuntimePort());
                Require(absent.Succeeded && absent.Status == "OptionalAbsent" && absent.RootCount == 1 && absent.TriggerCount == 0, absent.Message);
                completed.Add("zero-triggers-optional");

                GameObject one = Root("H226 One", objects);
                ActivityRestartTrigger oneTrigger = one.AddComponent<ActivityRestartTrigger>();
                var port = new QaFakeActivityRestartRuntimePort();
                ActivityRestartTriggerBindingResult bound = ActivityRestartTriggerBinding.TryBind(new[] { one, one }, port);
                Require(bound.Succeeded && bound.RootCount == 1 && bound.TriggerCount == 1 && bound.BoundCount == 1 && oneTrigger.HasActivityRestartRuntimeBinding, bound.Message);
                completed.Add("one-trigger-bound");

                GameObject multiple = Root("H226 Multiple", objects);
                multiple.AddComponent<ActivityRestartTrigger>();
                GameObject child = new GameObject("H226 Child");
                child.transform.SetParent(multiple.transform, false);
                objects.Add(child);
                child.AddComponent<ActivityRestartTrigger>();
                ActivityRestartTriggerBindingResult all = ActivityRestartTriggerBinding.TryBind(new[] { multiple }, new QaFakeActivityRestartRuntimePort());
                Require(all.Succeeded && all.TriggerCount == 2 && all.BoundCount == 2, all.Message);
                completed.Add("multiple-triggers-all-bound");

                ActivityRestartTriggerBindingResult same = ActivityRestartTriggerBinding.TryBind(new[] { one }, port);
                Require(same.Succeeded && same.IdempotentCount == 1 && same.BoundCount == 0, same.Message);
                completed.Add("same-port-idempotent");

                GameObject incompatible = Root("H226 Incompatible", objects);
                incompatible.AddComponent<ActivityRestartTrigger>();
                GameObject badChild = new GameObject("H226 Incompatible Child");
                badChild.transform.SetParent(incompatible.transform, false);
                objects.Add(badChild);
                ActivityRestartTrigger bad = badChild.AddComponent<ActivityRestartTrigger>();
                bad.TryBindActivityRestartRuntime(new QaFakeActivityRestartRuntimePort(), out _);
                ActivityRestartTriggerBindingResult rejected = ActivityRestartTriggerBinding.TryBind(new[] { incompatible }, new QaFakeActivityRestartRuntimePort());
                Require(!rejected.Succeeded && rejected.Status == "RejectedTriggerBinding" && rejected.BoundCount == 1 && rejected.RejectedCount == 1, rejected.Message);
                completed.Add("incompatible-trigger-rejected");

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
                    if (item != null) UnityEngine.Object.DestroyImmediate(item);
                }
            }
        }

        private static GameObject Root(string name, ICollection<UnityEngine.Object> objects)
        {
            var root = new GameObject(name);
            objects.Add(root);
            return root;
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}

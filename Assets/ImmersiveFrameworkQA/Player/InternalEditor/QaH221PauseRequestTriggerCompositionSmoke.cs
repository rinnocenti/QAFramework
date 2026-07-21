using System;
using System.Collections.Generic;
using Immersive.Framework.GlobalUi;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    public static class QaH221PauseRequestTriggerCompositionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Pause/H2.2.1 Run Pause Request Trigger Composition Smoke";
        private const string LogPrefix =
            "[H221_PAUSE_REQUEST_TRIGGER_COMPOSITION_SMOKE]";

        private static bool ValidateRun() => !EditorApplication.isPlaying;

        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();

            try
            {
                GameObject emptyRoot = CreateRoot("H221 Empty Root", objects);
                GlobalUiPauseRequestTriggerBindingResult absent =
                    GlobalUiSceneRuntime.TryBindPauseRequestTriggers(
                        new[] { emptyRoot },
                        new QaFakePauseProductRequestPort());
                Require(
                    absent.Succeeded && absent.Status == "OptionalAbsent" &&
                    absent.RootCount == 1 && absent.TriggerCount == 0,
                    "Zero trigger composition did not remain optional. " +
                    absent.Message);
                completed.Add("zero-triggers-optional");

                GameObject oneRoot = CreateRoot("H221 One Trigger Root", objects);
                PauseRequestTrigger oneTrigger =
                    oneRoot.AddComponent<PauseRequestTrigger>();
                var oneFake = new QaFakePauseProductRequestPort();
                GlobalUiPauseRequestTriggerBindingResult one =
                    GlobalUiSceneRuntime.TryBindPauseRequestTriggers(
                        new[] { oneRoot }, oneFake);
                Require(
                    one.Succeeded && one.Status == "Bound" &&
                    one.TriggerCount == 1 && one.BoundCount == 1 &&
                    one.IdempotentCount == 0 && one.RejectedCount == 0 &&
                    oneTrigger.HasPauseProductRequestBinding,
                    "One trigger composition did not bind explicitly. " + one.Message);
                completed.Add("one-trigger-bound");

                GameObject multipleRoot =
                    CreateRoot("H221 Multiple Trigger Root", objects);
                PauseRequestTrigger firstMultiple =
                    multipleRoot.AddComponent<PauseRequestTrigger>();
                GameObject multipleChild = new GameObject("H221 Multiple Child");
                multipleChild.transform.SetParent(multipleRoot.transform, false);
                objects.Add(multipleChild);
                PauseRequestTrigger secondMultiple =
                    multipleChild.AddComponent<PauseRequestTrigger>();
                var multipleFake = new QaFakePauseProductRequestPort();
                GlobalUiPauseRequestTriggerBindingResult multiple =
                    GlobalUiSceneRuntime.TryBindPauseRequestTriggers(
                        new[] { multipleRoot }, multipleFake);
                Require(
                    multiple.Succeeded && multiple.TriggerCount == 2 &&
                    multiple.BoundCount == 2 && multiple.RejectedCount == 0 &&
                    firstMultiple.HasPauseProductRequestBinding &&
                    secondMultiple.HasPauseProductRequestBinding,
                    "Multiple trigger composition did not bind every trigger. " +
                    multiple.Message);
                completed.Add("multiple-triggers-all-bound");

                GameObject idempotentRoot =
                    CreateRoot("H221 Idempotent Trigger Root", objects);
                PauseRequestTrigger idempotentTrigger =
                    idempotentRoot.AddComponent<PauseRequestTrigger>();
                var idempotentFake = new QaFakePauseProductRequestPort();
                Require(
                    idempotentTrigger.TryBindPauseProductRequest(
                        idempotentFake,
                        out string idempotentIssue),
                    "Could not prebind the idempotent trigger. " + idempotentIssue);
                GlobalUiPauseRequestTriggerBindingResult idempotent =
                    GlobalUiSceneRuntime.TryBindPauseRequestTriggers(
                        new[] { idempotentRoot }, idempotentFake);
                Require(
                    idempotent.Succeeded && idempotent.BoundCount == 0 &&
                    idempotent.IdempotentCount == 1 &&
                    idempotent.RejectedCount == 0,
                    "Existing same-port trigger binding was not idempotent. " +
                    idempotent.Message);
                completed.Add("same-port-existing-binding-idempotent");

                GameObject incompatibleRoot =
                    CreateRoot("H221 Incompatible Trigger Root", objects);
                PauseRequestTrigger compatibleTrigger =
                    incompatibleRoot.AddComponent<PauseRequestTrigger>();
                GameObject incompatibleChild =
                    new GameObject("H221 Incompatible Child");
                incompatibleChild.transform.SetParent(
                    incompatibleRoot.transform,
                    false);
                objects.Add(incompatibleChild);
                PauseRequestTrigger incompatibleTrigger =
                    incompatibleChild.AddComponent<PauseRequestTrigger>();
                var expectedRuntime = new QaFakePauseProductRequestPort();
                var incompatibleRuntime = new QaFakePauseProductRequestPort();
                Require(
                    incompatibleTrigger.TryBindPauseProductRequest(
                        incompatibleRuntime,
                        out string incompatibleIssue),
                    "Could not prebind the incompatible trigger. " +
                    incompatibleIssue);
                GlobalUiPauseRequestTriggerBindingResult incompatible =
                    GlobalUiSceneRuntime.TryBindPauseRequestTriggers(
                        new[] { incompatibleRoot }, expectedRuntime);
                Require(
                    !incompatible.Succeeded &&
                    incompatible.Status == "RejectedTriggerBinding" &&
                    incompatible.RootCount == 1 &&
                    incompatible.TriggerCount == 2 &&
                    incompatible.BoundCount == 1 &&
                    incompatible.IdempotentCount == 0 &&
                    incompatible.RejectedCount == 1 &&
                    compatibleTrigger.HasPauseProductRequestBinding &&
                    incompatibleTrigger.HasPauseProductRequestBinding &&
                    incompatible.Message.Contains("different port") &&
                    incompatible.Message.Contains("current lifetime") &&
                    incompatible.Message.Contains("H221 Incompatible Child") &&
                    incompatible.Message.Contains("bound='1'") &&
                    incompatible.Message.Contains("rejected='1'"),
                    "Incompatible trigger composition did not report the structured binding conflict. " +
                    incompatible.Message);
                compatibleTrigger.RequestPause();
                Require(
                    expectedRuntime.RequestCallCount == 1 &&
                    incompatibleRuntime.RequestCallCount == 0 &&
                    compatibleTrigger.LastRequestSucceeded,
                    "Compatible trigger did not preserve the composition product request authority.");
                incompatibleTrigger.RequestPause();
                Require(
                    expectedRuntime.RequestCallCount == 1 &&
                    incompatibleRuntime.RequestCallCount == 1 &&
                    incompatibleTrigger.LastRequestSucceeded,
                    "Incompatible trigger did not preserve its original product request authority.");
                completed.Add("incompatible-trigger-binding-rejected");

                Require(
                    completed.Count == 5,
                    "H2.2.1 Pause request trigger composition smoke case count changed unexpectedly.");
                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

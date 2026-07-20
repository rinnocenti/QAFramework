using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;
using PauseState = Immersive.Framework.Pause.PauseState;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    public static class QaH221PauseRequestTriggerBindingSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Pause/H2.2.1 Run Pause Request Trigger Binding Smoke";
        private const string LogPrefix = "[H221_PAUSE_REQUEST_TRIGGER_SMOKE]";

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
                    "H2.2.1 Pause request trigger binding smoke requires Play Mode.");
                completed.Add("play-mode-required");

                Require(
                    global::ImmersiveFrameworkQA.InputMode.Internal.Editor.QaInputModeFrameworkRuntimeHostResolver.TryResolveUniqueHost(
                        out FrameworkRuntimeHost currentHost) &&
                    currentHost != null,
                    "H2.2.1 Pause request trigger binding smoke requires an initialized global FrameworkRuntimeHost.");
                Require(
                    currentHost.TryGetPauseSnapshot(
                        out PauseSnapshot hostBefore),
                    "Global FrameworkRuntimeHost had no Pause snapshot for the unbound trigger proof.");
                completed.Add("runtime-host-available");

                GameObject boundRoot = CreateRoot("H221 Bound Trigger", objects);
                PauseRequestTrigger boundTrigger =
                    boundRoot.AddComponent<PauseRequestTrigger>();
                var fakeA = new QaFakePauseProductRequestPort();
                Require(
                    boundTrigger.TryBindPauseProductRequest(fakeA, out string firstIssue) &&
                    boundTrigger.HasPauseProductRequestBinding &&
                    boundTrigger.ProductRequestBindingStatus == "Bound",
                    "Initial Pause request trigger binding was not accepted. " +
                    firstIssue);
                completed.Add("initial-binding-accepted");

                Require(
                    boundTrigger.TryBindPauseProductRequest(fakeA, out string sameIssue) &&
                    boundTrigger.ProductRequestBindingDiagnostic.Contains("idempotent"),
                    "Same Pause product request port rebinding was not idempotent. " +
                    sameIssue);
                completed.Add("same-port-rebind-idempotent");

                var fakeB = new QaFakePauseProductRequestPort();
                Require(
                    !boundTrigger.TryBindPauseProductRequest(fakeB, out string differentIssue) &&
                    boundTrigger.HasPauseProductRequestBinding &&
                    differentIssue.Contains("different port"),
                    "Different Pause product request port rebinding was not rejected. " +
                    differentIssue);
                completed.Add("different-port-rebind-rejected");

                int snapshotsBefore = fakeA.SnapshotCallCount;
                Require(
                    boundTrigger.TryGetPauseSnapshot(out PauseSnapshot snapshot) &&
                    snapshot.State == PauseState.Running &&
                    fakeA.SnapshotCallCount == snapshotsBefore + 1 &&
                    fakeB.SnapshotCallCount == 0,
                    "Bound trigger did not forward the Pause snapshot only to fake A.");
                completed.Add("snapshot-forwarded-to-bound-port");

                int requestsBefore = fakeA.RequestCallCount;
                boundTrigger.RequestPause();
                Require(
                    fakeA.RequestCallCount == requestsBefore + 1 &&
                    fakeA.LastPauseRequest.Kind == PauseRequestKind.Pause &&
                    fakeB.RequestCallCount == 0 &&
                    boundTrigger.LastRequestSucceeded,
                    "Bound trigger did not forward the Pause request only to fake A.");
                completed.Add("request-forwarded-to-bound-port");

                GameObject unboundRoot = CreateRoot("H221 Unbound Trigger", objects);
                PauseRequestTrigger unboundTrigger =
                    unboundRoot.AddComponent<PauseRequestTrigger>();
                Require(
                    !unboundTrigger.TryGetPauseSnapshot(out _) &&
                    !unboundTrigger.HasPauseProductRequestBinding,
                    "Unbound trigger resolved a Pause snapshot without an explicit port.");
                unboundTrigger.TogglePause();
                Require(
                    currentHost.TryGetPauseSnapshot(
                        out PauseSnapshot hostAfter) &&
                    hostAfter.State == hostBefore.State &&
                    unboundTrigger.LastRequestFailed &&
                    unboundTrigger.LastMessage.Contains("Pause product request port is not bound") &&
                    unboundTrigger.ProductRequestBindingDiagnostic.Contains(
                        "Pause product request port is not bound"),
                    "Unbound trigger fell back to the global host or did not fail explicitly.");
                completed.Add("unbound-trigger-does-not-fallback-to-current-host");

                Require(
                    completed.Count == 8,
                    "H2.2.1 Pause request trigger binding smoke case count changed unexpectedly.");
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

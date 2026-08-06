using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Composition;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    // This smoke validates Scene Lifecycle binding and composition only.
    // It intentionally does not validate ResetRegistry or ResetExecutor execution.
    internal static class QaObjectResetSceneLifecycleBindingSmoke
    {
        [MenuItem("Immersive Framework/QA/Game Flow/Run Object Reset Scene Lifecycle Binding Smoke")]
        private static async void Run()
        {
            Scene scene = SceneManager.CreateScene("QA Object Reset Lifecycle");
            var root = new GameObject("QA Object Reset Root");
            SceneManager.MoveGameObjectToScene(root, scene);
            var trigger = root.AddComponent<ObjectResetTrigger>();
            var groupTrigger = root.AddComponent<ObjectResetGroupTrigger>();
            var subjectAdapter = root.AddComponent<UnityResetSubjectAdapter>();
            var executionPort = new RecordingExecutionPort();
            var registrationPort = new RecordingRegistrationPort();
            var selectionPort = new RecordingSelectionPort();
            subjectAdapter.ConfigureForQa(
                true,
                true,
                false,
                UnityResetSubjectIdGenerationMode.AuthoredStableId,
                "qa-reset-subject",
                string.Empty,
                ResetSubjectScope.Activity,
                "QA Reset Subject",
                "qa-reset-binding",
                UnityResetParticipantDiscoveryMode.SameGameObject,
                true);
            trigger.ConfigureForQa(subjectAdapter, string.Empty, "qa-object-reset", true, true);
            var participant = new ResetProductBindingSceneLifecycleParticipant(
                registrationPort,
                executionPort,
                selectionPort);
            try
            {
                Require(participant.OnSceneAvailable(scene, scene.GetRootGameObjects(), out string first), first);
                Require(trigger.HasResetExecutionRuntimeBinding && trigger.ResetExecutionRuntimeBindingStatus == "Bound", "Object Reset trigger was not bound from explicit roots.");
                Require(subjectAdapter.HasResetRegistrationRuntimeBinding && subjectAdapter.ResetRegistrationRuntimeBindingStatus == "Bound", "Unity Reset Subject Adapter was not bound from explicit roots.");
                Require(groupTrigger.HasResetSelectionExecutionRuntimeBinding && groupTrigger.ResetSelectionExecutionRuntimeBindingStatus == "Bound", "Object Reset Group Trigger was not bound from explicit roots.");
                Require(subjectAdapter.IsRegistered && registrationPort.Registry.SubjectCount == 1, "Unity Reset Subject Adapter was not registered through the lifecycle-owned registration port.");
                trigger.RequestObjectReset();
                Require(trigger.LastResult.Succeeded && executionPort.RequestCount == 1 && executionPort.LastRequest.SubjectIds.Count == 1, "Object Reset Trigger did not submit the registered subject through the execution port.");
                ResetExecutionResult groupResetResult = await groupTrigger.RequestObjectResetGroupAsync();
                Require(groupResetResult.Succeeded && selectionPort.RequestCount == 1, "Object Reset Group Trigger did not submit selection through the selection execution port.");
                Require(participant.OnSceneAvailable(scene, scene.GetRootGameObjects(), out string second), second);
                Require(trigger.TryBindResetExecutionRuntime(executionPort, out string idempotentIssue), idempotentIssue);
                Require(!trigger.TryBindResetExecutionRuntime(new RecordingExecutionPort(), out string rejectedIssue) && rejectedIssue.Contains("different port", StringComparison.OrdinalIgnoreCase), "Different reset execution port was not rejected.");
                Require(!subjectAdapter.TryBindResetRegistrationRuntime(new RecordingRegistrationPort(), out string registrationRejectedIssue) && registrationRejectedIssue.Contains("different port", StringComparison.OrdinalIgnoreCase), "Different reset registration port was not rejected.");
                Require(!groupTrigger.TryBindResetSelectionExecutionRuntime(new RecordingSelectionPort(), out string selectionRejectedIssue) && selectionRejectedIssue.Contains("different port", StringComparison.OrdinalIgnoreCase), "Different reset selection execution port was not rejected.");
                Require(participant.OnSceneReleasing(scene, scene.GetRootGameObjects(), "qa-reset-binding-release", out string release), release);
                Require(!subjectAdapter.IsRegistered && registrationPort.Registry.SubjectCount == 0, "Scene release did not unregister the Reset subject.");
                Debug.Log("[QA][RESET][BINDING] status='Passed' cases='subject-adapter-binding,object-trigger-binding,group-trigger-binding,idempotence,different-port-rejection,release'.");
            }
            finally
            {
                participant.OnSceneReleasing(
                    scene,
                    scene.GetRootGameObjects(),
                    "qa-reset-binding-finally-cleanup",
                    out _);
                UnityEngine.Object.DestroyImmediate(root);
                if (scene.IsValid() && scene.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                    while (unload != null && !unload.isDone) await Task.Yield();
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class RecordingExecutionPort : IResetExecutionRuntimePort
        {
            public int RequestCount { get; private set; }

            public ResetExecutionRequest LastRequest { get; private set; }

            public Task<ResetExecutionResult> ExecuteResetAsync(ResetExecutionRequest request)
            {
                RequestCount++;
                LastRequest = request;
                return Task.FromResult(ResetExecutionResult.SucceededNoSubjects(
                    ResetIssue.Info(ResetIssueKind.NoSubjects, "QA recording execution port accepted the request."),
                    nameof(RecordingExecutionPort),
                    "qa"));
            }
        }

        private sealed class RecordingRegistrationPort : IResetRegistrationRuntimePort
        {
            public ResetRegistry Registry { get; } = new ResetRegistry();

            public bool TryResolveCurrentResetOwner(ResetSubjectScope scope, out RuntimeContentOwner owner, out string issue)
            {
                owner = scope switch
                {
                    ResetSubjectScope.Route => RuntimeContentOwner.Route("qa-route", "QA Route"),
                    ResetSubjectScope.Activity => RuntimeContentOwner.Activity("qa-activity", "QA Activity"),
                    ResetSubjectScope.Runtime => RuntimeContentOwner.Activity("qa-activity", "QA Activity"),
                    _ => default
                };
                issue = owner.IsValid ? string.Empty : "QA binding smoke received an unsupported reset scope.";
                return owner.IsValid;
            }

            public ResetRegistryOperationResult RegisterResetSubject(ResetSubject subject, UnityEngine.Object owner, string source, string reason) => Registry.RegisterSubject(subject, owner, source, reason);

            public ResetRegistryOperationResult RegisterRuntimeResetSubject(string authoredPrefix, ResetSubjectScope scope, RuntimeContentOwner owner, UnityEngine.Object ownerObject, string displayName, string diagnosticTag, string source, string reason) => Registry.RegisterRuntimeSubject(authoredPrefix, scope, owner, ownerObject, displayName, diagnosticTag, source, reason);

            public ResetRegistryOperationResult RegisterResetParticipant(ResetRegistrationHandle subjectHandle, IResetParticipant participant, UnityEngine.Object owner, string source, string reason) => Registry.RegisterParticipant(subjectHandle, participant, owner, source, reason);

            public ResetRegistryOperationResult UnregisterResetRegistration(ResetRegistrationHandle handle, UnityEngine.Object owner, string source, string reason) => Registry.Unregister(handle, owner, source, reason);
        }

        private sealed class RecordingSelectionPort : IResetSelectionExecutionRuntimePort
        {
            public int RequestCount { get; private set; }

            public Task<ResetSelectionExecutionRuntimeResult> ExecuteResetSelectionAsync(ResetSelectionConfig selection, string source, string reason)
            {
                RequestCount++;
                var resolution = ResetSelectionResolution.SucceededResult(
                    ResetSelectionMode.ExplicitSubjects,
                    Array.Empty<ResetSubjectId>(),
                    Array.Empty<ResetIssue>(),
                    source,
                    reason,
                    "QA recording selection port accepted the request.");
                var execution = ResetExecutionResult.SucceededNoSubjects(
                    ResetIssue.Info(ResetIssueKind.NoSubjects, "QA recording selection port selected no subjects."),
                    source,
                    reason);
                return Task.FromResult(new ResetSelectionExecutionRuntimeResult(resolution, execution));
            }
        }
    }
}

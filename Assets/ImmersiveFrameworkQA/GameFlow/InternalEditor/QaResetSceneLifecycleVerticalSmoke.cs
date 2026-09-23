using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Actors;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Composition;
using Immersive.Framework.Reset.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaResetSceneLifecycleVerticalSmoke
    {
        // This smoke validates the canonical Reset pipeline end to end using the active FrameworkRuntimeHost, ResetRegistry and ResetExecutor.
        private const string LogPrefix = "[QA][RESET][VERTICAL]";

        [MenuItem("Immersive Framework/QA/Regressions/Game Flow/Run Reset Scene Lifecycle Vertical Smoke", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Regressions/Game Flow/Run Reset Scene Lifecycle Vertical Smoke")]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        internal static async Task RunInternalAsync()
        {
            var objects = new List<UnityEngine.Object>();
            FrameworkRuntimeHost host = null;
            ResetProductBindingSceneLifecycleParticipant lifecycleParticipant = null;
            GameObject[] roots = null;
            int baselineSubjects = 0;
            int baselineParticipants = 0;
            try
            {
                Require(EditorApplication.isPlaying, "Reset vertical smoke requires Play Mode.");
                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(out host), "Reset vertical smoke requires one FrameworkRuntimeHost.");
                Require(host.State.CurrentActivity != null && host.State.IsActivityReady, "Reset vertical smoke requires a ready current Activity owner.");

                baselineSubjects = host.ResetRegistrySubjectCount;
                baselineParticipants = host.ResetRegistryParticipantCount;
                var objectFixture = CreateFixture("object", objects);
                var groupFixture = CreateFixture("group", objects);
                var groupRoot = new GameObject("QA Reset Vertical Group Trigger");
                objects.Add(groupRoot);
                var groupTrigger = groupRoot.AddComponent<ObjectResetGroupTrigger>();
                groupTrigger.ConfigureForQa(
                    "qa-reset-vertical-group",
                    "qa-group-reset",
                    ResetSelectionMode.ExplicitSubjects,
                    new[] { Reference(objectFixture.Adapter), Reference(groupFixture.Adapter) },
                    false,
                    false,
                    true,
                    false);

                lifecycleParticipant = new ResetProductBindingSceneLifecycleParticipant(
                    (IResetRegistrationRuntimePort)host,
                    (IResetExecutionRuntimePort)host,
                    (IResetSelectionExecutionRuntimePort)host);
                Scene scene = objectFixture.Root.scene;
                roots = new[] { objectFixture.Root, groupFixture.Root, groupRoot };
                Require(lifecycleParticipant.OnSceneAvailable(scene, roots, out string available), available);
                Require(objectFixture.Adapter.IsRegistered && groupFixture.Adapter.IsRegistered, "Scene Lifecycle did not register both authored subjects.");
                Require(objectFixture.Adapter.RegisteredParticipantCount == 1 && groupFixture.Adapter.RegisteredParticipantCount == 1, "Scene Lifecycle did not register both participants.");
                Require(host.ResetRegistrySubjectCount == baselineSubjects + 2 && host.ResetRegistryParticipantCount == baselineParticipants + 2, "Registry counts did not reflect composed fixtures.");
                Require(objectFixture.Trigger.HasResetExecutionRuntimeBinding && groupTrigger.HasResetSelectionExecutionRuntimeBinding, "Reset triggers were not bound to FrameworkRuntimeHost ports.");

                ResetExecutionResult objectResult = await RequestObjectAsync(objectFixture.Trigger);
                RequireSucceeded(objectResult, 1, 1, "object");
                Require(objectFixture.Participant.ExecutionCount == 1 && objectFixture.Participant.LastSubjectId == objectFixture.Adapter.SubjectId, "Object participant was not invoked by ResetExecutor.");

                ResetExecutionResult groupResult = await groupTrigger.RequestObjectResetGroupAsync();
                RequireSucceeded(groupResult, 2, 2, "group");
                Require(groupTrigger.LastSelectionResolution.SubjectCount == 2, "Group selection did not resolve two real registry subjects.");
                Require(objectFixture.Participant.ExecutionCount == 2 && groupFixture.Participant.ExecutionCount == 1, "Group execution did not reach both participants.");

                Require(lifecycleParticipant.OnSceneAvailable(scene, roots, out string idempotent), idempotent);
                Require(host.ResetRegistrySubjectCount == baselineSubjects + 2 && host.ResetRegistryParticipantCount == baselineParticipants + 2, "Repeated SceneAvailable duplicated Reset registrations.");

                Require(lifecycleParticipant.OnSceneReleasing(scene, roots, "qa-vertical-release", out string release), release);
                Require(!objectFixture.Adapter.IsRegistered && !groupFixture.Adapter.IsRegistered, "Scene release retained adapter registration state.");
                Require(host.ResetRegistrySubjectCount == baselineSubjects && host.ResetRegistryParticipantCount == baselineParticipants, "Scene release retained registry registrations.");
                Require(lifecycleParticipant.OnSceneReleasing(scene, roots, "qa-vertical-release-repeat", out string repeatedRelease), repeatedRelease);

                Require(lifecycleParticipant.OnSceneAvailable(scene, roots, out string reentry), reentry);
                ResetExecutionResult reentryResult = await RequestObjectAsync(objectFixture.Trigger);
                RequireSucceeded(reentryResult, 1, 1, "reentry");
                Require(objectFixture.Participant.ExecutionCount == 3, "Reentry did not execute the participant through a new registration.");
                Require(lifecycleParticipant.OnSceneReleasing(scene, roots, "qa-vertical-cleanup", out string cleanup), cleanup);

                Debug.Log($"{LogPrefix} status='Passed' objectSubjects='1' objectParticipants='1' groupSubjects='2' groupParticipants='2' idempotence='Passed' release='Passed' reentry='Passed'.");
            }
            finally
            {
                if (lifecycleParticipant != null && roots != null)
                {
                    lifecycleParticipant.OnSceneReleasing(
                        roots[0].scene,
                        roots,
                        "qa-vertical-finally-cleanup",
                        out _);
                }

                foreach (UnityEngine.Object item in objects)
                {
                    if (item != null) UnityEngine.Object.Destroy(item);
                }

                if (host != null)
                {
                    Require(
                        host.ResetRegistrySubjectCount == baselineSubjects
                        && host.ResetRegistryParticipantCount == baselineParticipants,
                        "Reset vertical smoke cleanup did not restore ResetRegistry baseline.");
                }
            }
        }

        private static Fixture CreateFixture(string suffix, ICollection<UnityEngine.Object> objects)
        {
            var root = new GameObject("QA Reset Vertical " + suffix);
            objects.Add(root);
            var participant = root.AddComponent<QaResetVerticalParticipant>();
            participant.ConfigureForQa("qa-reset-vertical." + suffix + ".participant", ResetParticipantRequiredness.Required, 10, "QA " + suffix + " participant", "QaResetSceneLifecycleVerticalSmoke", suffix);
            var actor = root.AddComponent<PlayerActorDeclaration>();
            var actorSerialized = new SerializedObject(actor);
            actorSerialized.FindProperty("actorId").stringValue = "qa-reset-vertical.actor." + suffix;
            actorSerialized.ApplyModifiedPropertiesWithoutUndo();
            var adapter = root.AddComponent<UnityResetSubjectAdapter>();
            adapter.ConfigureForQa(true, true, false, UnityResetSubjectIdGenerationMode.AuthoredStableId, string.Empty, string.Empty, ResetSubjectScope.Activity, "QA " + suffix + " subject", "qa-vertical", UnityResetParticipantDiscoveryMode.SameGameObject, true, false, null, actor);
            var trigger = root.AddComponent<ObjectResetTrigger>();
            trigger.ConfigureForQa(adapter, string.Empty, "qa-object-reset-" + suffix, false, true);
            return new Fixture(root, adapter, participant, trigger);
        }

        private static ResetSubjectReference Reference(UnityResetSubjectAdapter adapter)
        {
            var reference = new ResetSubjectReference();
            reference.ConfigureForQa(adapter, string.Empty);
            return reference;
        }

        private static async Task<ResetExecutionResult> RequestObjectAsync(ObjectResetTrigger trigger)
        {
            var completion = new TaskCompletionSource<ResetExecutionResult>();
            using (trigger.SubscribeRequestEvents(resetEvent =>
            {
                if (resetEvent.Phase == Immersive.Framework.GameFlow.FlowRequestEventPhase.Completed && resetEvent.HasResult)
                {
                    completion.TrySetResult(resetEvent.Result);
                }
            }))
            {
                trigger.RequestObjectReset();
                return await completion.Task;
            }
        }

        private static void RequireSucceeded(ResetExecutionResult result, int subjects, int participants, string label)
        {
            Require(result.Status == ResetExecutionStatus.Succeeded, label + " result was " + result.Status + ". " + result);
            Require(result.SubjectCount == subjects && result.SubjectSucceeded == subjects && result.SubjectFailed == 0, label + " subject aggregation was invalid. " + result);
            Require(result.ParticipantCount == participants && result.ParticipantSucceeded == participants && result.ParticipantFailed == 0 && result.BlockingIssueCount == 0, label + " participant aggregation was invalid. " + result);
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private readonly struct Fixture
        {
            internal Fixture(GameObject root, UnityResetSubjectAdapter adapter, QaResetVerticalParticipant participant, ObjectResetTrigger trigger)
            {
                Root = root;
                Adapter = adapter;
                Participant = participant;
                Trigger = trigger;
            }

            internal GameObject Root { get; }
            internal UnityResetSubjectAdapter Adapter { get; }
            internal QaResetVerticalParticipant Participant { get; }
            internal ObjectResetTrigger Trigger { get; }
        }
    }

    internal sealed class QaResetVerticalParticipant : UnityResetParticipantBehaviour
    {
        internal int ExecutionCount { get; private set; }
        internal ResetSubjectId LastSubjectId { get; private set; }
        internal string LastSource { get; private set; }
        internal string LastReason { get; private set; }

        public override ResetParticipantResult Reset(ResetContext context)
        {
            ExecutionCount++;
            LastSubjectId = context.Subject.SubjectId;
            LastSource = context.Source;
            LastReason = context.Reason;
            return ResetParticipantResult.CreateSucceeded(context.Participant, nameof(QaResetVerticalParticipant), context.Reason, "QA vertical participant completed.");
        }
    }
}

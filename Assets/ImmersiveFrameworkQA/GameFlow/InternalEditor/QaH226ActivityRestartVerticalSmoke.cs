using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Reset;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH226ActivityRestartVerticalSmoke
    {
        private const string LogPrefix = "[H226_ACTIVITY_RESTART_VERTICAL_SMOKE]";

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.6 Run Activity Restart Vertical Smoke", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.6 Run Activity Restart Vertical Smoke")]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        public static async Task RunInternalAsync()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();
            NominalResetFixture nominalFixture = null;
            try
            {
                Require(FrameworkRuntimeHost.TryGetCurrent(out FrameworkRuntimeHost host) && host != null, "H2.2.6 vertical smoke requires FrameworkRuntimeHost.");
                Require(host.State.CurrentRoute != null && host.State.CurrentActivity != null, "H2.2.6 vertical smoke requires active Route and Activity.");
                IActivityRestartRuntimePort runtime = host;

                ActivityRestartResult noActivity = (await runtime.RequestActivityRestartAsync(null, false, true, new ResetSelectionConfig(), "H226", "no-active-activity")).Result;
                Require(noActivity.Status == ActivityRestartResultStatus.RejectedNoActiveActivity && noActivity.Message.Contains("No target Activity"), noActivity.ToDiagnosticString());
                completed.Add("no-active-activity-explicit-structured-failure");

                ActivityAsset mismatch = ScriptableObject.CreateInstance<ActivityAsset>();
                mismatch.name = "H226 Mismatch";
                objects.Add(mismatch);
                ActivityRestartResult mismatchResult = (await runtime.RequestActivityRestartAsync(mismatch, false, true, new ResetSelectionConfig(), "H226", "target-mismatch")).Result;
                Require(mismatchResult.Status == ActivityRestartResultStatus.RejectedTargetMismatch && mismatchResult.Activity == mismatch, mismatchResult.ToDiagnosticString());
                completed.Add("target-mismatch-explicit-structured-failure");

                ActivityRestartTrigger invalidSelection = CreateTrigger("H226 Invalid Selection", objects);
                Require(invalidSelection.TryBindActivityRestartRuntime(runtime, out string issue), issue);
                invalidSelection.ConfigureForQa(null, true, true, "selection-invalid", ResetSelectionMode.ExplicitSubjects, Array.Empty<ResetSubjectReference>(), false, true, true, false);
                ActivityRestartResult invalidResult = await invalidSelection.RequestActivityRestartAsync();
                Require(invalidResult.Status == ActivityRestartResultStatus.ResetExecutionFailed && invalidSelection.LastSelectionResolution.Status != ResetSelectionResolutionStatus.Unknown && invalidSelection.LastResetExecutionResult.Failed && host.State.CurrentActivity != null && string.IsNullOrEmpty(invalidResult.ClearStatus), invalidResult.ToDiagnosticString());
                completed.Add("selection-invalid-before-flow");

                nominalFixture = NominalResetFixture.Create(host);
                ActivityAsset activityBeforeRestart = host.State.CurrentActivity;
                ActivityRestartTrigger nominal = CreateTrigger("H226 Nominal", objects);
                Require(nominal.TryBindActivityRestartRuntime(runtime, out issue), issue);
                nominal.ConfigureForQa(null, true, true, "nominal", ResetSelectionMode.ExplicitSubjects, new[] { nominalFixture.Reference }, false, false, true, false);
                ActivityRestartResult nominalResult = await nominal.RequestActivityRestartAsync();
                Require(
                    nominalResult.Status == ActivityRestartResultStatus.Succeeded &&
                    nominalResult.ResetExecutionResult.Status == ResetExecutionStatus.Succeeded &&
                    nominalResult.ResetSubjectCount == 1 &&
                    nominalResult.ResetSubjectSucceededCount == 1 &&
                    nominalResult.ResetParticipantCount == 1 &&
                    nominalResult.ResetParticipantSucceededCount == 1 &&
                    nominalResult.ResetBlockingIssueCount == 0 &&
                    nominalResult.ResetNonBlockingIssueCount == 0 &&
                    nominalFixture.Participant.ExecutionCount == 1 &&
                    nominalResult.ClearStatus == "Succeeded" &&
                    nominalResult.ReenterStatus == "Succeeded" &&
                    host.State.CurrentActivity == activityBeforeRestart,
                    nominalResult.ToDiagnosticString());
                completed.Add("nominal-reset-clear-reentry-order-completed");

                ActivityRestartTrigger delayed = CreateTrigger("H226 Delayed", objects);
                var delayedPort = new DelayedPort(Result(ActivityRestartResultStatus.CompletedWithWarnings));
                Require(delayed.TryBindActivityRestartRuntime(delayedPort, out issue), issue);
                var first = delayed.RequestActivityRestartAsync();
                ActivityRestartResult second = await delayed.RequestActivityRestartAsync();
                Require(second.Status == ActivityRestartResultStatus.RejectedAlreadyInFlight && delayed.IsRequestInFlight, "Second request did not remain single-flight.");
                completed.Add("single-flight-rejects-concurrent-request");
                delayedPort.Complete();
                ActivityRestartResult warnings = await first;
                Require(warnings.Status == ActivityRestartResultStatus.CompletedWithWarnings && delayed.LastRequestSucceeded && !delayed.IsRequestInFlight, "Warnings did not continue to completion.");
                completed.Add("warnings-continue-until-completion");

                ActivityRestartTrigger blocking = CreateTrigger("H226 Blocking", objects);
                Require(blocking.TryBindActivityRestartRuntime(runtime, out issue), issue);
                blocking.ConfigureForQa(null, true, true, "blocking-reset", ResetSelectionMode.ExplicitSubjects, Array.Empty<ResetSubjectReference>(), false, true, true, false);
                ActivityRestartResult blockingResult = await blocking.RequestActivityRestartAsync();
                Require(blockingResult.Status == ActivityRestartResultStatus.ResetExecutionFailed && string.IsNullOrEmpty(blockingResult.ClearStatus) && host.State.CurrentActivity != null, blockingResult.ToDiagnosticString());
                completed.Add("blocking-reset-prevents-clear");

                Require(!invalidSelection.IsRequestInFlight && !nominal.IsRequestInFlight && !delayed.IsRequestInFlight && !blocking.IsRequestInFlight, "A trigger retained in-flight state after completion.");
                completed.Add("no-in-flight-state-remains-after-completion");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                nominalFixture?.Dispose();
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

        private static ActivityRestartResult Result(ActivityRestartResultStatus status) =>
            new ActivityRestartResult(status, null, string.Empty, "H226", "delayed", default, string.Empty, string.Empty, string.Empty, string.Empty, "qa");

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private sealed class NominalResetFixture : IDisposable
        {
            private readonly FrameworkRuntimeHost _host;
            private readonly GameObject _owner;
            private readonly List<ResetRegistrationHandle> _handles = new List<ResetRegistrationHandle>();

            private NominalResetFixture(FrameworkRuntimeHost host, GameObject owner, ResetSubjectReference reference, H226RequiredSuccessParticipant participant)
            {
                _host = host;
                _owner = owner;
                Reference = reference;
                Participant = participant;
            }

            internal ResetSubjectReference Reference { get; }

            internal H226RequiredSuccessParticipant Participant { get; }

            internal static NominalResetFixture Create(FrameworkRuntimeHost host)
            {
                var owner = new GameObject("H226 Nominal Reset Subject");
                var participant = new H226RequiredSuccessParticipant();
                var reference = new ResetSubjectReference();
                const string subjectId = "qa.h226.activity-restart.nominal";
                reference.ConfigureForQa(null, subjectId);
                var fixture = new NominalResetFixture(host, owner, reference, participant);
                var subject = new ResetSubject(
                    ResetSubjectId.From(subjectId),
                    ResetSubjectScope.Runtime,
                    ResetSubjectOrigin.RuntimeRegistered,
                    owner.name,
                    "H226:Nominal");
                ResetRegistryOperationResult subjectResult = host.RegisterResetSubject(subject, owner, "H226", "nominal-register-subject");
                if (!subjectResult.Succeeded)
                {
                    fixture.Dispose();
                    throw new InvalidOperationException(subjectResult.ToString());
                }

                fixture._handles.Add(subjectResult.Handle);
                ResetRegistryOperationResult participantResult = host.RegisterResetParticipant(subjectResult.Handle, participant, owner, "H226", "nominal-register-participant");
                if (!participantResult.Succeeded)
                {
                    fixture.Dispose();
                    throw new InvalidOperationException(participantResult.ToString());
                }

                fixture._handles.Add(participantResult.Handle);
                return fixture;
            }

            public void Dispose()
            {
                for (int index = _handles.Count - 1; index >= 0; index--)
                {
                    _host.UnregisterResetRegistration(_handles[index], _owner, "H226", "nominal-cleanup");
                }

                if (_owner != null)
                {
                    UnityEngine.Object.Destroy(_owner);
                }
            }
        }

        private sealed class H226RequiredSuccessParticipant : IResetParticipant
        {
            internal int ExecutionCount { get; private set; }

            public bool TryCreateResetParticipantDescriptor(ResetSubject subject, out ResetParticipantDescriptor descriptor, out ResetIssue issue)
            {
                descriptor = new ResetParticipantDescriptor(
                    ResetParticipantId.From("qa.h226.activity-restart.nominal.required"),
                    subject.SubjectId,
                    ResetParticipantRequiredness.Required,
                    0,
                    "H226 Nominal Required",
                    "H226",
                    "nominal");
                issue = default;
                return true;
            }

            public ResetParticipantResult Reset(ResetContext context)
            {
                ExecutionCount++;
                return ResetParticipantResult.CreateSucceeded(context.Participant, "H226", "nominal", "Required participant succeeded.");
            }
        }

        private sealed class DelayedPort : IActivityRestartRuntimePort
        {
            private readonly TaskCompletionSource<ActivityRestartRuntimeResult> _completion = new TaskCompletionSource<ActivityRestartRuntimeResult>();
            private readonly ActivityRestartResult _result;

            internal DelayedPort(ActivityRestartResult result)
            {
                _result = result;
            }

            public Task<ActivityRestartRuntimeResult> RequestActivityRestartAsync(ActivityAsset targetActivity, bool useCurrentActivityWhenTargetMissing, bool requireTargetActivityIsCurrent, ResetSelectionConfig resetSelection, string source, string reason) => _completion.Task;

            internal void Complete() => _completion.TrySetResult(ActivityRestartRuntimeResult.From(_result));
        }
    }
}

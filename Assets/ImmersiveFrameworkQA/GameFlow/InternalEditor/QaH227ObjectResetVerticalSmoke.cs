using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.Reset;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH227ObjectResetVerticalSmoke
    {
        private const string LogPrefix = "[H227_OBJECT_RESET_VERTICAL_SMOKE]";
        private const string RuntimeSource = nameof(QaH227ObjectResetVerticalSmoke);
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(10d);

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.7 Run Object Reset Vertical Smoke", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Game Flow/H2.2.7 Run Object Reset Vertical Smoke")]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        public static async Task RunInternalAsync()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();
            var fixtures = new List<ResetSubjectFixture>();
            var triggers = new List<ObjectResetTrigger>();

            try
            {
                Require(EditorApplication.isPlaying, "H2.2.7 vertical smoke requires Play Mode.");
                Require(
                    FrameworkRuntimeHost.TryGetCurrent(out FrameworkRuntimeHost host) && host != null,
                    "H2.2.7 vertical smoke requires FrameworkRuntimeHost.");

                IResetExecutionRuntimePort runtime = host;
                Require(runtime != null, "FrameworkRuntimeHost did not expose Reset execution runtime port.");
                completed.Add("runtime-port-available");

                ResetExecutionResult noSubjects = await runtime.ExecuteResetAsync(
                    ResetExecutionRequest.Empty(
                        allowNoSubjects: true,
                        allowNoParticipants: true,
                        stopOnFailure: true,
                        source: RuntimeSource,
                        reason: "no-subjects-allowed"));
                Require(
                    noSubjects.Status == ResetExecutionStatus.SucceededNoSubjects
                    && noSubjects.SubjectCount == 0
                    && noSubjects.Issues.Count == 1
                    && noSubjects.Issues[0].Kind == ResetIssueKind.NoSubjects
                    && noSubjects.BlockingIssueCount == 0
                    && noSubjects.NonBlockingIssueCount == 1
                    && noSubjects.Source == RuntimeSource
                    && noSubjects.Reason == "no-subjects-allowed",
                    noSubjects.ToString());
                completed.Add("no-subjects-allowed-through-runtime-port");

                ResetSubjectFixture nominalFixture = ResetSubjectFixture.Create(
                    host,
                    "qa.h227.object-reset.nominal",
                    "H227 Nominal Subject");
                fixtures.Add(nominalFixture);
                var nominalParticipant = new QaResetParticipant(
                    nominalFixture.SubjectId,
                    "qa.h227.object-reset.nominal.required",
                    ResetParticipantRequiredness.Required,
                    ParticipantBehavior.Succeed);
                nominalFixture.RegisterParticipant(nominalParticipant);

                ObjectResetTrigger nominalTrigger = CreateTrigger(
                    "H227 Nominal Trigger",
                    nominalFixture.SubjectId.StableText,
                    "nominal",
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    runtime,
                    objects,
                    triggers);
                ObjectResetTriggerEvent nominalEvent = await RequestAndAwaitAsync(nominalTrigger, "nominal");
                ResetExecutionResult nominal = nominalEvent.Result;
                Require(
                    nominalEvent.Succeeded
                    && nominal.Status == ResetExecutionStatus.Succeeded
                    && nominal.SubjectCount == 1
                    && nominal.SubjectSucceeded == 1
                    && nominal.SubjectFailed == 0
                    && nominal.ParticipantCount == 1
                    && nominal.ParticipantSucceeded == 1
                    && nominal.ParticipantFailed == 0
                    && nominal.BlockingIssueCount == 0
                    && nominal.NonBlockingIssueCount == 0
                    && nominal.Source == nameof(ObjectResetTrigger)
                    && nominal.Reason == "nominal"
                    && nominalParticipant.ExecutionCount == 1
                    && nominalParticipant.LastSource == nameof(ObjectResetTrigger)
                    && nominalParticipant.LastReason == "nominal"
                    && nominalTrigger.LastRequestSucceeded
                    && nominalTrigger.HasLastResult,
                    nominal.ToString());
                completed.Add("nominal-subject-and-required-participant-succeeded");

                ObjectResetTrigger missingTrigger = CreateTrigger(
                    "H227 Missing Subject Trigger",
                    "qa.h227.object-reset.missing",
                    "subject-missing",
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    runtime,
                    objects,
                    triggers);
                ObjectResetTriggerEvent missingEvent = await RequestAndAwaitAsync(missingTrigger, "subject-missing");
                ResetExecutionResult missing = missingEvent.Result;
                Require(
                    missingEvent.Failed
                    && missing.Status == ResetExecutionStatus.Failed
                    && missing.SubjectCount == 1
                    && missing.Subjects[0].Status == ResetSubjectResultStatus.FailedSubjectNotFound
                    && missing.Subjects[0].Issues.Count == 1
                    && missing.Subjects[0].Issues[0].Kind == ResetIssueKind.SubjectNotFound
                    && missing.BlockingIssueCount == 1
                    && missing.Source == nameof(ObjectResetTrigger)
                    && missing.Reason == "subject-missing"
                    && missingTrigger.LastRequestFailed,
                    missing.ToString());
                completed.Add("missing-subject-failed-explicitly");

                ResetSubjectFixture emptyFixture = ResetSubjectFixture.Create(
                    host,
                    "qa.h227.object-reset.no-participants",
                    "H227 No Participants Subject");
                fixtures.Add(emptyFixture);
                ObjectResetTrigger emptyTrigger = CreateTrigger(
                    "H227 No Participants Trigger",
                    emptyFixture.SubjectId.StableText,
                    "no-participants-allowed",
                    allowNoParticipants: true,
                    stopOnFailure: true,
                    runtime,
                    objects,
                    triggers);
                ObjectResetTriggerEvent emptyEvent = await RequestAndAwaitAsync(emptyTrigger, "no-participants-allowed");
                ResetExecutionResult empty = emptyEvent.Result;
                Require(
                    emptyEvent.Succeeded
                    && empty.Status == ResetExecutionStatus.Succeeded
                    && empty.SubjectCount == 1
                    && empty.Subjects[0].Status == ResetSubjectResultStatus.SkippedNoParticipants
                    && empty.Subjects[0].Issues.Count == 1
                    && empty.Subjects[0].Issues[0].Kind == ResetIssueKind.NoParticipants
                    && empty.ParticipantCount == 0
                    && empty.BlockingIssueCount == 0
                    && empty.NonBlockingIssueCount == 1
                    && emptyTrigger.LastResultSucceededNoParticipants,
                    empty.ToString());
                completed.Add("no-participants-allowed-succeeded-with-info");

                ResetSubjectFixture optionalFixture = ResetSubjectFixture.Create(
                    host,
                    "qa.h227.object-reset.optional-failure",
                    "H227 Optional Failure Subject");
                fixtures.Add(optionalFixture);
                var optionalParticipant = new QaResetParticipant(
                    optionalFixture.SubjectId,
                    "qa.h227.object-reset.optional.failure",
                    ResetParticipantRequiredness.Optional,
                    ParticipantBehavior.Fail);
                optionalFixture.RegisterParticipant(optionalParticipant);
                ObjectResetTrigger optionalTrigger = CreateTrigger(
                    "H227 Optional Failure Trigger",
                    optionalFixture.SubjectId.StableText,
                    "optional-failure",
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    runtime,
                    objects,
                    triggers);
                ObjectResetTriggerEvent optionalEvent = await RequestAndAwaitAsync(optionalTrigger, "optional-failure");
                ResetExecutionResult optional = optionalEvent.Result;
                Require(
                    optionalEvent.Succeeded
                    && optional.Status == ResetExecutionStatus.Succeeded
                    && optional.SubjectCount == 1
                    && optional.Subjects[0].Status == ResetSubjectResultStatus.Succeeded
                    && optional.Subjects[0].Issues.Count == 1
                    && optional.Subjects[0].Issues[0].Kind == ResetIssueKind.InvalidParticipant
                    && optional.ParticipantCount == 1
                    && optional.ParticipantFailed == 1
                    && optional.BlockingIssueCount == 0
                    && optional.NonBlockingIssueCount == 2
                    && optional.Source == nameof(ObjectResetTrigger)
                    && optional.Reason == "optional-failure"
                    && optionalParticipant.ExecutionCount == 1
                    && optionalTrigger.LastRequestSucceeded
                    && optionalTrigger.LastResultCompletedWithWarnings,
                    optional.ToString());
                completed.Add("optional-failure-produced-non-blocking-warning");

                ResetSubjectFixture requiredFixture = ResetSubjectFixture.Create(
                    host,
                    "qa.h227.object-reset.required-failure",
                    "H227 Required Failure Subject");
                fixtures.Add(requiredFixture);
                var requiredParticipant = new QaResetParticipant(
                    requiredFixture.SubjectId,
                    "qa.h227.object-reset.required.failure",
                    ResetParticipantRequiredness.Required,
                    ParticipantBehavior.Fail);
                requiredFixture.RegisterParticipant(requiredParticipant);
                ObjectResetTrigger requiredTrigger = CreateTrigger(
                    "H227 Required Failure Trigger",
                    requiredFixture.SubjectId.StableText,
                    "required-failure",
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    runtime,
                    objects,
                    triggers);
                ObjectResetTriggerEvent requiredEvent = await RequestAndAwaitAsync(requiredTrigger, "required-failure");
                ResetExecutionResult required = requiredEvent.Result;
                Require(
                    requiredEvent.Failed
                    && required.Status == ResetExecutionStatus.Failed
                    && required.SubjectCount == 1
                    && required.Subjects[0].Status == ResetSubjectResultStatus.Failed
                    && required.ParticipantCount == 1
                    && required.ParticipantFailed == 1
                    && required.BlockingIssueCount == 1
                    && required.NonBlockingIssueCount == 0
                    && required.Source == nameof(ObjectResetTrigger)
                    && required.Reason == "required-failure"
                    && requiredParticipant.ExecutionCount == 1
                    && requiredTrigger.LastRequestFailed,
                    required.ToString());
                completed.Add("required-failure-blocked-reset");

                ObjectResetTrigger delayedTrigger = CreateUnboundTrigger(
                    "H227 Single Flight Trigger",
                    "qa.h227.object-reset.single-flight",
                    "single-flight",
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    objects,
                    triggers);
                var delayedPort = new DelayedResetExecutionRuntimePort();
                Require(
                    delayedTrigger.TryBindResetExecutionRuntime(delayedPort, out string delayedBindingIssue),
                    delayedBindingIssue);

                int submittedCount = 0;
                int ignoredCount = 0;
                int succeededCount = 0;
                var ignoredCompletion = new TaskCompletionSource<ObjectResetTriggerEvent>();
                var succeededCompletion = new TaskCompletionSource<ObjectResetTriggerEvent>();
                using (delayedTrigger.SubscribeRequestEvents(resetEvent =>
                {
                    if (resetEvent.IsSubmitted)
                    {
                        submittedCount++;
                    }
                    else if (resetEvent.IsCompleted && resetEvent.Ignored)
                    {
                        ignoredCount++;
                        ignoredCompletion.TrySetResult(resetEvent);
                    }
                    else if (resetEvent.IsCompleted && resetEvent.Succeeded)
                    {
                        succeededCount++;
                        succeededCompletion.TrySetResult(resetEvent);
                    }
                }))
                {
                    delayedTrigger.RequestObjectReset();
                    Require(
                        delayedPort.CallCount == 1 && delayedTrigger.IsRequestInFlight,
                        "First Object Reset request did not remain in flight through the delayed port.");

                    delayedTrigger.RequestObjectReset();
                    ObjectResetTriggerEvent ignoredEvent = await AwaitWithTimeoutAsync(
                        ignoredCompletion.Task,
                        "single-flight ignored completion");
                    Require(
                        ignoredEvent.Ignored
                        && ignoredEvent.ResultStatus == ResetExecutionStatus.RejectedInvalidRequest
                        && delayedPort.CallCount == 1
                        && delayedTrigger.IsRequestInFlight,
                        ignoredEvent.Result.ToString());

                    delayedPort.Complete();
                    ObjectResetTriggerEvent completedEvent = await AwaitWithTimeoutAsync(
                        succeededCompletion.Task,
                        "single-flight final completion");
                    Require(
                        completedEvent.Succeeded
                        && completedEvent.Result.Source == nameof(ObjectResetTrigger)
                        && completedEvent.Result.Reason == "single-flight"
                        && submittedCount == 1
                        && ignoredCount == 1
                        && succeededCount == 1
                        && delayedPort.CallCount == 1
                        && !delayedTrigger.IsRequestInFlight
                        && delayedTrigger.LastRequestSucceeded,
                        completedEvent.Result.ToString());
                }
                completed.Add("single-flight-rejected-concurrent-request-and-cleaned-state");

                for (int index = 0; index < triggers.Count; index++)
                {
                    Require(
                        !triggers[index].IsRequestInFlight,
                        $"Trigger '{triggers[index].name}' retained in-flight state after completion.");
                }
                completed.Add("no-in-flight-state-remains");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                for (int index = fixtures.Count - 1; index >= 0; index--)
                {
                    fixtures[index]?.Dispose();
                }

                for (int index = objects.Count - 1; index >= 0; index--)
                {
                    if (objects[index] != null)
                    {
                        UnityEngine.Object.Destroy(objects[index]);
                    }
                }
            }
        }

        private static ObjectResetTrigger CreateTrigger(
            string name,
            string subjectId,
            string reason,
            bool allowNoParticipants,
            bool stopOnFailure,
            IResetExecutionRuntimePort runtime,
            ICollection<UnityEngine.Object> objects,
            ICollection<ObjectResetTrigger> triggers)
        {
            ObjectResetTrigger trigger = CreateUnboundTrigger(
                name,
                subjectId,
                reason,
                allowNoParticipants,
                stopOnFailure,
                objects,
                triggers);
            Require(trigger.TryBindResetExecutionRuntime(runtime, out string issue), issue);
            return trigger;
        }

        private static ObjectResetTrigger CreateUnboundTrigger(
            string name,
            string subjectId,
            string reason,
            bool allowNoParticipants,
            bool stopOnFailure,
            ICollection<UnityEngine.Object> objects,
            ICollection<ObjectResetTrigger> triggers)
        {
            var root = new GameObject(name);
            objects.Add(root);
            ObjectResetTrigger trigger = root.AddComponent<ObjectResetTrigger>();
            trigger.ConfigureForQa(
                null,
                subjectId,
                reason,
                allowNoParticipants,
                stopOnFailure);
            triggers.Add(trigger);
            return trigger;
        }

        private static async Task<ObjectResetTriggerEvent> RequestAndAwaitAsync(
            ObjectResetTrigger trigger,
            string label)
        {
            var completion = new TaskCompletionSource<ObjectResetTriggerEvent>();
            using (trigger.SubscribeRequestEvents(resetEvent =>
            {
                if (resetEvent.IsCompleted)
                {
                    completion.TrySetResult(resetEvent);
                }
            }))
            {
                trigger.RequestObjectReset();
                return await AwaitWithTimeoutAsync(completion.Task, label);
            }
        }

        private static async Task<T> AwaitWithTimeoutAsync<T>(Task<T> task, string label)
        {
            Task winner = await Task.WhenAny(task, Task.Delay(RequestTimeout));
            if (!ReferenceEquals(winner, task))
            {
                throw new TimeoutException(
                    $"H2.2.7 timed out while awaiting '{label}' after '{RequestTimeout.TotalSeconds:0}' seconds.");
            }

            return await task;
        }

        private static void Require(bool value, string message)
        {
            if (!value)
            {
                throw new InvalidOperationException(message);
            }
        }

        private enum ParticipantBehavior
        {
            Succeed = 0,
            Fail = 1
        }

        private sealed class QaResetParticipant : IResetParticipant
        {
            private readonly ResetSubjectId _subjectId;
            private readonly ResetParticipantId _participantId;
            private readonly ResetParticipantRequiredness _requiredness;
            private readonly ParticipantBehavior _behavior;

            internal QaResetParticipant(
                ResetSubjectId subjectId,
                string participantId,
                ResetParticipantRequiredness requiredness,
                ParticipantBehavior behavior)
            {
                _subjectId = subjectId;
                _participantId = ResetParticipantId.From(participantId);
                _requiredness = requiredness;
                _behavior = behavior;
            }

            internal int ExecutionCount { get; private set; }

            internal string LastSource { get; private set; } = string.Empty;

            internal string LastReason { get; private set; } = string.Empty;

            public bool TryCreateResetParticipantDescriptor(
                ResetSubject subject,
                out ResetParticipantDescriptor descriptor,
                out ResetIssue issue)
            {
                descriptor = new ResetParticipantDescriptor(
                    _participantId,
                    _subjectId,
                    _requiredness,
                    0,
                    _participantId.StableText,
                    RuntimeSource,
                    "register-participant");
                issue = default;
                return subject.SubjectId == _subjectId;
            }

            public ResetParticipantResult Reset(ResetContext context)
            {
                ExecutionCount++;
                LastSource = context.Source;
                LastReason = context.Reason;

                return _behavior == ParticipantBehavior.Succeed
                    ? ResetParticipantResult.CreateSucceeded(
                        context.Participant,
                        context.Source,
                        context.Reason,
                        "QA reset participant succeeded.")
                    : ResetParticipantResult.CreateFailed(
                        context.Participant,
                        1,
                        context.Source,
                        context.Reason,
                        "QA reset participant failed.");
            }
        }

        private sealed class ResetSubjectFixture : IDisposable
        {
            private readonly FrameworkRuntimeHost _host;
            private readonly GameObject _owner;
            private readonly List<ResetRegistrationHandle> _handles = new List<ResetRegistrationHandle>();
            private readonly ResetRegistrationHandle _subjectHandle;

            private ResetSubjectFixture(
                FrameworkRuntimeHost host,
                GameObject owner,
                ResetSubjectId subjectId,
                ResetRegistrationHandle subjectHandle)
            {
                _host = host;
                _owner = owner;
                SubjectId = subjectId;
                _subjectHandle = subjectHandle;
                _handles.Add(subjectHandle);
            }

            internal ResetSubjectId SubjectId { get; }

            internal static ResetSubjectFixture Create(
                FrameworkRuntimeHost host,
                string subjectIdText,
                string displayName)
            {
                var owner = new GameObject(displayName);
                ResetSubjectId subjectId = ResetSubjectId.From(subjectIdText);
                var subject = new ResetSubject(
                    subjectId,
                    ResetSubjectScope.Runtime,
                    ResetSubjectOrigin.RuntimeRegistered,
                    displayName,
                    RuntimeSource);
                ResetRegistryOperationResult registration = host.RegisterResetSubject(
                    subject,
                    owner,
                    RuntimeSource,
                    "register-subject");
                if (!registration.Succeeded)
                {
                    UnityEngine.Object.Destroy(owner);
                    throw new InvalidOperationException(registration.ToString());
                }

                return new ResetSubjectFixture(host, owner, subjectId, registration.Handle);
            }

            internal void RegisterParticipant(IResetParticipant participant)
            {
                ResetRegistryOperationResult registration = _host.RegisterResetParticipant(
                    _subjectHandle,
                    participant,
                    _owner,
                    RuntimeSource,
                    "register-participant");
                if (!registration.Succeeded)
                {
                    throw new InvalidOperationException(registration.ToString());
                }

                _handles.Add(registration.Handle);
            }

            public void Dispose()
            {
                for (int index = _handles.Count - 1; index >= 0; index--)
                {
                    _host.UnregisterResetRegistration(
                        _handles[index],
                        _owner,
                        RuntimeSource,
                        "cleanup");
                }

                if (_owner != null)
                {
                    UnityEngine.Object.Destroy(_owner);
                }
            }
        }

        private sealed class DelayedResetExecutionRuntimePort : IResetExecutionRuntimePort
        {
            private readonly TaskCompletionSource<ResetExecutionResult> _completion =
                new TaskCompletionSource<ResetExecutionResult>();
            private ResetExecutionRequest _request;

            internal int CallCount { get; private set; }

            public Task<ResetExecutionResult> ExecuteResetAsync(ResetExecutionRequest request)
            {
                CallCount++;
                _request = request;
                return _completion.Task;
            }

            internal void Complete()
            {
                _completion.TrySetResult(new ResetExecutionResult(
                    ResetExecutionStatus.Succeeded,
                    Array.Empty<ResetSubjectResult>(),
                    Array.Empty<ResetIssue>(),
                    _request.Source,
                    _request.Reason,
                    "Delayed QA reset completed."));
            }
        }
    }
}

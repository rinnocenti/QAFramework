using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.Reset;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH228ObjectResetGroupVerticalSmoke
    {
        private const string LogPrefix = "[H228_OBJECT_RESET_GROUP_VERTICAL_SMOKE]";
        private const string RuntimeSource = nameof(QaH228ObjectResetGroupVerticalSmoke);

        [MenuItem("Immersive Framework/QA/Regressions/Game Flow/Run Object Reset Regression", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Regressions/Game Flow/Run Object Reset Regression")]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        public static async Task RunInternalAsync()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();
            var fixtures = new List<ResetSubjectFixture>();
            var triggers = new List<ObjectResetGroupTrigger>();

            try
            {
                Require(EditorApplication.isPlaying, "H2.2.8 vertical smoke requires Play Mode.");
                Require(
                    global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(out FrameworkRuntimeHost host) && host != null,
                    "H2.2.8 vertical smoke requires FrameworkRuntimeHost.");

                IResetSelectionExecutionRuntimePort runtime = host;
                Require(runtime != null, "FrameworkRuntimeHost did not expose Reset selection execution runtime port.");
                completed.Add("runtime-port-available");

                RunBindingCompositionCases(objects, completed);

                ObjectResetGroupTrigger unbound = CreateUnboundTrigger(
                    "H228 Unbound Group Trigger",
                    "h228-unbound",
                    "unbound",
                    ResetSelectionMode.ExplicitSubjects,
                    Array.Empty<ResetSubjectReference>(),
                    allowNoSubjects: true,
                    allowNoParticipants: true,
                    stopOnFailure: true,
                    yieldBetweenSubjects: false,
                    objects,
                    triggers);
                int unboundSubmitted = 0;
                using (unbound.SubscribeRequestEvents(resetEvent =>
                {
                    if (resetEvent.Phase == FlowRequestEventPhase.Submitted)
                    {
                        unboundSubmitted++;
                    }
                }))
                {
                    ResetExecutionResult unboundResult = await unbound.RequestObjectResetGroupAsync();
                    Require(
                        unboundSubmitted == 0
                        && unboundResult.Status == ResetExecutionStatus.RejectedInvalidRequest
                        && unbound.LastRequestFailed
                        && unbound.ResetSelectionExecutionRuntimeBindingDiagnostic.Contains("not bound"),
                        unboundResult.ToString());
                }
                completed.Add("unbound-trigger-does-not-fallback-to-current-host");

                var recordingPort = new RecordingSelectionExecutionPort();
                ObjectResetGroupTrigger forwarding = CreateUnboundTrigger(
                    "H228 Forwarding Group Trigger",
                    "h228-forwarding",
                    "forward-exactly",
                    ResetSelectionMode.ExplicitSubjects,
                    Array.Empty<ResetSubjectReference>(),
                    allowNoSubjects: true,
                    allowNoParticipants: true,
                    stopOnFailure: false,
                    yieldBetweenSubjects: true,
                    objects,
                    triggers);
                Require(
                    forwarding.TryBindResetSelectionExecutionRuntime(recordingPort, out string forwardingIssue),
                    forwardingIssue);
                ResetExecutionResult forwarded = await forwarding.RequestObjectResetGroupAsync();
                Require(
                    recordingPort.CallCount == 1
                    && ReferenceEquals(recordingPort.LastSelection, forwarding.Selection)
                    && recordingPort.LastSource == nameof(ObjectResetGroupTrigger)
                    && recordingPort.LastReason == "forward-exactly"
                    && forwarding.ResolvedGroupId == "h228-forwarding"
                    && forwarded.Status == ResetExecutionStatus.SucceededNoSubjects
                    && forwarding.LastSelectionResolution.Status == ResetSelectionResolutionStatus.SucceededNoSubjects
                    && forwarding.LastRequestSucceeded,
                    forwarded.ToString());
                completed.Add("authored-selection-source-and-reason-forwarded-exactly");

                ResetSubjectFixture nominalA = ResetSubjectFixture.Create(
                    host,
                    "qa.h228.object-reset-group.nominal.a",
                    "H228 Nominal Subject A");
                ResetSubjectFixture nominalB = ResetSubjectFixture.Create(
                    host,
                    "qa.h228.object-reset-group.nominal.b",
                    "H228 Nominal Subject B");
                fixtures.Add(nominalA);
                fixtures.Add(nominalB);
                var nominalParticipantA = new QaResetParticipant(
                    nominalA.SubjectId,
                    "qa.h228.object-reset-group.nominal.a.required",
                    ResetParticipantRequiredness.Required,
                    ParticipantBehavior.Succeed);
                var nominalParticipantB = new QaResetParticipant(
                    nominalB.SubjectId,
                    "qa.h228.object-reset-group.nominal.b.required",
                    ResetParticipantRequiredness.Required,
                    ParticipantBehavior.Succeed);
                nominalA.RegisterParticipant(nominalParticipantA);
                nominalB.RegisterParticipant(nominalParticipantB);

                ObjectResetGroupTrigger nominal = CreateBoundTrigger(
                    "H228 Nominal Group Trigger",
                    "h228-nominal",
                    "nominal-group",
                    ResetSelectionMode.ExplicitSubjects,
                    new[]
                    {
                        Reference(nominalA.SubjectId),
                        Reference(nominalB.SubjectId),
                        Reference(nominalA.SubjectId)
                    },
                    allowNoSubjects: false,
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    yieldBetweenSubjects: false,
                    runtime,
                    objects,
                    triggers);
                ResetExecutionResult nominalResult = await nominal.RequestObjectResetGroupAsync();
                Require(
                    nominalResult.Status == ResetExecutionStatus.Succeeded
                    && nominal.LastSelectionResolution.Status == ResetSelectionResolutionStatus.Succeeded
                    && nominal.LastSelectionResolution.SubjectCount == 2
                    && nominalResult.SubjectCount == 2
                    && nominalResult.SubjectSucceeded == 2
                    && nominalResult.SubjectFailed == 0
                    && nominalResult.ParticipantCount == 2
                    && nominalResult.ParticipantSucceeded == 2
                    && nominalResult.ParticipantFailed == 0
                    && nominalResult.BlockingIssueCount == 0
                    && nominalResult.NonBlockingIssueCount == 0
                    && nominalResult.Source == nameof(ObjectResetGroupTrigger)
                    && nominalResult.Reason == "nominal-group"
                    && nominalParticipantA.ExecutionCount == 1
                    && nominalParticipantB.ExecutionCount == 1
                    && nominal.LastTargetCount == 2
                    && nominal.LastSucceededTargetCount == 2
                    && nominal.LastParticipantCount == 2
                    && nominal.LastRequestSucceeded,
                    nominalResult.ToString());
                completed.Add("explicit-multi-subject-selection-normalized-and-executed");

                ResetSubjectReference invalidReference = new ResetSubjectReference();
                invalidReference.ConfigureForQa(null, string.Empty);
                ObjectResetGroupTrigger invalidSelection = CreateBoundTrigger(
                    "H228 Invalid Selection Trigger",
                    "h228-invalid-selection",
                    "invalid-selection",
                    ResetSelectionMode.ExplicitSubjects,
                    new[] { invalidReference },
                    allowNoSubjects: false,
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    yieldBetweenSubjects: false,
                    runtime,
                    objects,
                    triggers);
                ResetExecutionResult invalidSelectionResult = await invalidSelection.RequestObjectResetGroupAsync();
                Require(
                    invalidSelectionResult.Status == ResetExecutionStatus.RejectedInvalidRequest
                    && invalidSelection.LastSelectionResolution.Failed
                    && invalidSelection.LastSelectionResolution.BlockingIssueCount > 0
                    && invalidSelection.LastSelectionResolution.SubjectCount == 0
                    && invalidSelection.LastRequestFailed,
                    invalidSelectionResult.ToString());
                completed.Add("invalid-selection-failed-before-execution");

                ObjectResetGroupTrigger missing = CreateBoundTrigger(
                    "H228 Missing Subject Group Trigger",
                    "h228-missing",
                    "missing-subject",
                    ResetSelectionMode.ExplicitSubjects,
                    new[] { Reference(ResetSubjectId.From("qa.h228.object-reset-group.missing")) },
                    allowNoSubjects: false,
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    yieldBetweenSubjects: false,
                    runtime,
                    objects,
                    triggers);
                ResetExecutionResult missingResult = await missing.RequestObjectResetGroupAsync();
                Require(
                    missing.LastSelectionResolution.Succeeded
                    && missing.LastSelectionResolution.SubjectCount == 1
                    && missingResult.Status == ResetExecutionStatus.Failed
                    && missingResult.SubjectCount == 1
                    && missingResult.Subjects[0].Status == ResetSubjectResultStatus.FailedSubjectNotFound
                    && missingResult.BlockingIssueCount == 1
                    && missing.LastRequestFailed,
                    missingResult.ToString());
                completed.Add("resolved-but-unregistered-subject-failed-explicitly");

                ObjectResetGroupTrigger emptyAllowed = CreateBoundTrigger(
                    "H228 Empty Allowed Group Trigger",
                    "h228-empty-allowed",
                    "empty-allowed",
                    ResetSelectionMode.ExplicitSubjects,
                    Array.Empty<ResetSubjectReference>(),
                    allowNoSubjects: true,
                    allowNoParticipants: true,
                    stopOnFailure: true,
                    yieldBetweenSubjects: false,
                    runtime,
                    objects,
                    triggers);
                ResetExecutionResult emptyAllowedResult = await emptyAllowed.RequestObjectResetGroupAsync();
                Require(
                    emptyAllowed.LastSelectionResolution.Status == ResetSelectionResolutionStatus.SucceededNoSubjects
                    && emptyAllowedResult.Status == ResetExecutionStatus.SucceededNoSubjects
                    && emptyAllowedResult.SubjectCount == 0
                    && emptyAllowedResult.Issues.Count == 1
                    && emptyAllowedResult.Issues[0].Kind == ResetIssueKind.NoSubjects
                    && emptyAllowedResult.BlockingIssueCount == 0
                    && emptyAllowedResult.NonBlockingIssueCount == 1
                    && emptyAllowed.LastRequestSucceeded,
                    emptyAllowedResult.ToString());
                completed.Add("empty-selection-allowed-with-structured-info");

                ResetSubjectFixture optionalFixture = ResetSubjectFixture.Create(
                    host,
                    "qa.h228.object-reset-group.optional-failure",
                    "H228 Optional Failure Subject");
                fixtures.Add(optionalFixture);
                var optionalParticipant = new QaResetParticipant(
                    optionalFixture.SubjectId,
                    "qa.h228.object-reset-group.optional-failure.optional",
                    ResetParticipantRequiredness.Optional,
                    ParticipantBehavior.Fail);
                optionalFixture.RegisterParticipant(optionalParticipant);
                ObjectResetGroupTrigger optionalFailure = CreateBoundTrigger(
                    "H228 Optional Failure Group Trigger",
                    "h228-optional-failure",
                    "optional-failure",
                    ResetSelectionMode.ExplicitSubjects,
                    new[] { Reference(optionalFixture.SubjectId) },
                    allowNoSubjects: false,
                    allowNoParticipants: false,
                    stopOnFailure: true,
                    yieldBetweenSubjects: false,
                    runtime,
                    objects,
                    triggers);
                ResetExecutionResult optionalFailureResult = await optionalFailure.RequestObjectResetGroupAsync();
                Require(
                    optionalFailureResult.Status == ResetExecutionStatus.Succeeded
                    && optionalFailureResult.SubjectCount == 1
                    && optionalFailureResult.SubjectSucceeded == 1
                    && optionalFailureResult.ParticipantCount == 1
                    && optionalFailureResult.ParticipantFailed == 1
                    && optionalFailureResult.BlockingIssueCount == 0
                    && optionalFailureResult.NonBlockingIssueCount > 0
                    && optionalFailure.LastWarningTargetCount == 1
                    && optionalFailure.LastRequestSucceeded,
                    optionalFailureResult.ToString());
                completed.Add("optional-participant-failure-remained-non-blocking");

                ResetSubjectFixture requiredFailureFixture = ResetSubjectFixture.Create(
                    host,
                    "qa.h228.object-reset-group.required-failure",
                    "H228 Required Failure Subject");
                ResetSubjectFixture continuesFixture = ResetSubjectFixture.Create(
                    host,
                    "qa.h228.object-reset-group.continues",
                    "H228 Continue Subject");
                fixtures.Add(requiredFailureFixture);
                fixtures.Add(continuesFixture);
                var requiredFailureParticipant = new QaResetParticipant(
                    requiredFailureFixture.SubjectId,
                    "qa.h228.object-reset-group.required-failure.required",
                    ResetParticipantRequiredness.Required,
                    ParticipantBehavior.Fail);
                var continuesParticipant = new QaResetParticipant(
                    continuesFixture.SubjectId,
                    "qa.h228.object-reset-group.continues.required",
                    ResetParticipantRequiredness.Required,
                    ParticipantBehavior.Succeed);
                requiredFailureFixture.RegisterParticipant(requiredFailureParticipant);
                continuesFixture.RegisterParticipant(continuesParticipant);
                ObjectResetGroupTrigger requiredFailure = CreateBoundTrigger(
                    "H228 Required Failure Group Trigger",
                    "h228-required-failure",
                    "required-failure-continue",
                    ResetSelectionMode.ExplicitSubjects,
                    new[]
                    {
                        Reference(requiredFailureFixture.SubjectId),
                        Reference(continuesFixture.SubjectId)
                    },
                    allowNoSubjects: false,
                    allowNoParticipants: false,
                    stopOnFailure: false,
                    yieldBetweenSubjects: false,
                    runtime,
                    objects,
                    triggers);
                ResetExecutionResult requiredFailureResult = await requiredFailure.RequestObjectResetGroupAsync();
                Require(
                    requiredFailureResult.Status == ResetExecutionStatus.Failed
                    && requiredFailureResult.SubjectCount == 2
                    && requiredFailureResult.SubjectFailed == 1
                    && requiredFailureResult.SubjectSucceeded == 1
                    && requiredFailureResult.ParticipantCount == 2
                    && requiredFailureResult.ParticipantFailed == 1
                    && requiredFailureResult.ParticipantSucceeded == 1
                    && requiredFailureResult.BlockingIssueCount > 0
                    && requiredFailureParticipant.ExecutionCount == 1
                    && continuesParticipant.ExecutionCount == 1
                    && requiredFailure.LastFailedTargetCount == 1
                    && requiredFailure.LastRequestFailed,
                    requiredFailureResult.ToString());
                completed.Add("required-failure-blocked-result-with-stop-on-failure-disabled");

                var delayedPort = new DelayedSelectionExecutionPort();
                ObjectResetGroupTrigger delayed = CreateUnboundTrigger(
                    "H228 Delayed Group Trigger",
                    "h228-delayed",
                    "single-flight",
                    ResetSelectionMode.ExplicitSubjects,
                    Array.Empty<ResetSubjectReference>(),
                    allowNoSubjects: true,
                    allowNoParticipants: true,
                    stopOnFailure: true,
                    yieldBetweenSubjects: false,
                    objects,
                    triggers);
                Require(delayed.TryBindResetSelectionExecutionRuntime(delayedPort, out string delayedIssue), delayedIssue);
                var firstRequest = delayed.RequestObjectResetGroupAsync();
                Require(
                    delayedPort.CallCount == 1 && delayed.IsRequestInFlight,
                    "First Object Reset Group request did not remain in flight through the delayed port.");
                ResetExecutionResult concurrent = await delayed.RequestObjectResetGroupAsync();
                Require(
                    concurrent.Status == ResetExecutionStatus.RejectedInvalidRequest
                    && delayedPort.CallCount == 1
                    && delayed.IsRequestInFlight
                    && delayed.LastRequestIgnored,
                    concurrent.ToString());
                delayedPort.Complete();
                ResetExecutionResult delayedResult = await firstRequest;
                Require(
                    delayedResult.Status == ResetExecutionStatus.SucceededNoSubjects
                    && delayedResult.Source == nameof(ObjectResetGroupTrigger)
                    && delayedResult.Reason == "single-flight"
                    && delayedPort.CallCount == 1
                    && !delayed.IsRequestInFlight
                    && delayed.LastRequestSucceeded,
                    delayedResult.ToString());
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

        private static void RunBindingCompositionCases(
            ICollection<UnityEngine.Object> objects,
            ICollection<string> completed)
        {
            GameObject empty = CreateRoot("H228 Binding Empty", objects);
            ObjectResetGroupTriggerBindingResult absent = ObjectResetGroupTriggerBinding.TryBind(
                new[] { empty, empty },
                new RecordingSelectionExecutionPort());
            Require(
                absent.Succeeded
                && absent.Status == "OptionalAbsent"
                && absent.RootCount == 1
                && absent.TriggerCount == 0,
                absent.Message);

            GameObject root = CreateRoot("H228 Binding Root", objects);
            ObjectResetGroupTrigger trigger = root.AddComponent<ObjectResetGroupTrigger>();
            var port = new RecordingSelectionExecutionPort();
            ObjectResetGroupTriggerBindingResult bound = ObjectResetGroupTriggerBinding.TryBind(
                new[] { root, root },
                port);
            Require(
                bound.Succeeded
                && bound.RootCount == 1
                && bound.TriggerCount == 1
                && bound.BoundCount == 1
                && trigger.HasResetSelectionExecutionRuntimeBinding,
                bound.Message);

            ObjectResetGroupTriggerBindingResult idempotent = ObjectResetGroupTriggerBinding.TryBind(
                new[] { root },
                port);
            Require(
                idempotent.Succeeded
                && idempotent.BoundCount == 0
                && idempotent.IdempotentCount == 1,
                idempotent.Message);

            GameObject incompatibleRoot = CreateRoot("H228 Binding Incompatible", objects);
            ObjectResetGroupTrigger incompatible = incompatibleRoot.AddComponent<ObjectResetGroupTrigger>();
            Require(
                incompatible.TryBindResetSelectionExecutionRuntime(
                    new RecordingSelectionExecutionPort(),
                    out string initialIssue),
                initialIssue);
            ObjectResetGroupTriggerBindingResult rejected = ObjectResetGroupTriggerBinding.TryBind(
                new[] { incompatibleRoot },
                new RecordingSelectionExecutionPort());
            Require(
                !rejected.Succeeded
                && rejected.Status == "RejectedTriggerBinding"
                && rejected.RejectedCount == 1,
                rejected.Message);

            completed.Add("explicit-root-binding-optional-idempotent-and-divergent-cases");
        }

        private static ObjectResetGroupTrigger CreateBoundTrigger(
            string name,
            string groupId,
            string reason,
            ResetSelectionMode selectionMode,
            IReadOnlyList<ResetSubjectReference> explicitSubjects,
            bool allowNoSubjects,
            bool allowNoParticipants,
            bool stopOnFailure,
            bool yieldBetweenSubjects,
            IResetSelectionExecutionRuntimePort runtime,
            ICollection<UnityEngine.Object> objects,
            ICollection<ObjectResetGroupTrigger> triggers)
        {
            ObjectResetGroupTrigger trigger = CreateUnboundTrigger(
                name,
                groupId,
                reason,
                selectionMode,
                explicitSubjects,
                allowNoSubjects,
                allowNoParticipants,
                stopOnFailure,
                yieldBetweenSubjects,
                objects,
                triggers);
            Require(trigger.TryBindResetSelectionExecutionRuntime(runtime, out string issue), issue);
            return trigger;
        }

        private static ObjectResetGroupTrigger CreateUnboundTrigger(
            string name,
            string groupId,
            string reason,
            ResetSelectionMode selectionMode,
            IReadOnlyList<ResetSubjectReference> explicitSubjects,
            bool allowNoSubjects,
            bool allowNoParticipants,
            bool stopOnFailure,
            bool yieldBetweenSubjects,
            ICollection<UnityEngine.Object> objects,
            ICollection<ObjectResetGroupTrigger> triggers)
        {
            GameObject root = CreateRoot(name, objects);
            ObjectResetGroupTrigger trigger = root.AddComponent<ObjectResetGroupTrigger>();
            trigger.ConfigureForQa(
                groupId,
                reason,
                selectionMode,
                explicitSubjects,
                allowNoSubjects,
                allowNoParticipants,
                stopOnFailure,
                yieldBetweenSubjects);
            triggers.Add(trigger);
            return trigger;
        }

        private static GameObject CreateRoot(
            string name,
            ICollection<UnityEngine.Object> objects)
        {
            var root = new GameObject(name);
            objects.Add(root);
            return root;
        }

        private static ResetSubjectReference Reference(ResetSubjectId subjectId)
        {
            var reference = new ResetSubjectReference();
            reference.ConfigureForQa(null, subjectId.StableText);
            return reference;
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

        private sealed class RecordingSelectionExecutionPort : IResetSelectionExecutionRuntimePort
        {
            internal int CallCount { get; private set; }

            internal ResetSelectionConfig LastSelection { get; private set; }

            internal string LastSource { get; private set; } = string.Empty;

            internal string LastReason { get; private set; } = string.Empty;

            public Task<ResetSelectionExecutionRuntimeResult> ExecuteResetSelectionAsync(
                ResetSelectionConfig selection,
                string source,
                string reason)
            {
                CallCount++;
                LastSelection = selection;
                LastSource = source;
                LastReason = reason;

                ResetSelectionResolution resolution = ResetSelectionResolution.SucceededResult(
                    selection != null ? selection.Mode : ResetSelectionMode.ExplicitSubjects,
                    Array.Empty<ResetSubjectId>(),
                    Array.Empty<ResetIssue>(),
                    source,
                    reason,
                    "QA selection resolved no subjects.");
                ResetExecutionResult execution = ResetExecutionResult.SucceededNoSubjects(
                    ResetIssue.Info(
                        ResetIssueKind.NoSubjects,
                        "QA selection execution completed with no subjects."),
                    source,
                    reason);
                return Task.FromResult(
                    new ResetSelectionExecutionRuntimeResult(resolution, execution));
            }
        }

        private sealed class DelayedSelectionExecutionPort : IResetSelectionExecutionRuntimePort
        {
            private readonly TaskCompletionSource<ResetSelectionExecutionRuntimeResult> _completion =
                new TaskCompletionSource<ResetSelectionExecutionRuntimeResult>();
            private ResetSelectionConfig _selection;
            private string _source = string.Empty;
            private string _reason = string.Empty;

            internal int CallCount { get; private set; }

            public Task<ResetSelectionExecutionRuntimeResult> ExecuteResetSelectionAsync(
                ResetSelectionConfig selection,
                string source,
                string reason)
            {
                CallCount++;
                _selection = selection;
                _source = source;
                _reason = reason;
                return _completion.Task;
            }

            internal void Complete()
            {
                ResetSelectionResolution resolution = ResetSelectionResolution.SucceededResult(
                    _selection != null ? _selection.Mode : ResetSelectionMode.ExplicitSubjects,
                    Array.Empty<ResetSubjectId>(),
                    Array.Empty<ResetIssue>(),
                    _source,
                    _reason,
                    "Delayed QA selection resolved no subjects.");
                ResetExecutionResult execution = ResetExecutionResult.SucceededNoSubjects(
                    ResetIssue.Info(
                        ResetIssueKind.NoSubjects,
                        "Delayed QA selection execution completed with no subjects."),
                    _source,
                    _reason);
                _completion.TrySetResult(
                    new ResetSelectionExecutionRuntimeResult(resolution, execution));
            }
        }
    }
}

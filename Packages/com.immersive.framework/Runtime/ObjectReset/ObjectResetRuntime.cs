using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Deterministic executor for the Object Reset foundation.
    /// It resolves logical targets, builds ordered participant plans and aggregates results; it does not discover Unity objects or perform any reset itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14H Object Reset plan builder/executor used by Runtime Host; no Unity baseline adapter.")]
    public sealed class ObjectResetRuntime
    {
        public ObjectResetPlan BuildPlan(
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectResetRequest request,
            IObjectResetParticipantSource participantSource)
        {
            var targetResolution = ObjectResetTargetResolver.ResolveTarget(snapshot, request);
            if (!targetResolution.Succeeded || !targetResolution.HasResolvedTarget)
            {
                return new ObjectResetPlan(
                    request,
                    default,
                    Array.Empty<ObjectResetParticipantEntry>(),
                    targetResolution.SnapshotIssues());
            }

            return BuildPlan(request, targetResolution.ResolvedTarget, participantSource);
        }

        public ObjectResetResult Execute(
            ObjectEntryRuntimeContextSnapshot snapshot,
            ObjectResetRequest request,
            IObjectResetParticipantSource participantSource)
        {
            var targetResolution = ObjectResetTargetResolver.ResolveTarget(snapshot, request);
            if (!targetResolution.Succeeded || !targetResolution.HasResolvedTarget)
            {
                return targetResolution;
            }

            var plan = BuildPlan(request, targetResolution.ResolvedTarget, participantSource);
            if (plan.HasBlockingIssues)
            {
                return ObjectResetResult.FromExecution(
                    request,
                    ObjectResetResultStatus.Failed,
                    targetResolution.ResolvedTarget,
                    true,
                    plan,
                    Array.Empty<ObjectResetParticipantResult>(),
                    plan.SnapshotIssues(),
                    "Object Reset plan failed because it contains blocking issues.");
            }

            if (!plan.HasParticipants)
            {
                return ObjectResetResult.FromExecution(
                    request,
                    ObjectResetResultStatus.SucceededNoParticipants,
                    targetResolution.ResolvedTarget,
                    true,
                    plan,
                    Array.Empty<ObjectResetParticipantResult>(),
                    plan.SnapshotIssues(),
                    "Object Reset completed with no participants.");
            }

            var issues = new List<ObjectResetIssue>(plan.SnapshotIssues());
            var participantResults = new List<ObjectResetParticipantResult>();

            foreach (var entry in plan.Participants)
            {
                var context = new ObjectResetContext(request, targetResolution.ResolvedTarget, entry.Descriptor);
                ObjectResetParticipantResult participantResult;
                try
                {
                    participantResult = entry.Participant.ResetObject(context);
                }
                catch (Exception exception)
                {
                    participantResult = entry.IsRequired
                        ? ObjectResetParticipantResult.BlockingFailure(
                            context,
                            1,
                            entry.Descriptor.Source,
                            entry.Descriptor.Reason,
                            $"Required Object Reset participant threw: {exception.Message}")
                        : ObjectResetParticipantResult.NonBlockingFailure(
                            context,
                            1,
                            entry.Descriptor.Source,
                            entry.Descriptor.Reason,
                            $"Optional Object Reset participant threw: {exception.Message}");
                }

                participantResults.Add(participantResult);

                if (participantResult.BlocksReset)
                {
                    issues.Add(ObjectResetIssue.Error(
                        ObjectResetIssueKind.RequiredParticipantFailed,
                        $"Required Object Reset participant '{participantResult.ParticipantId.StableText}' blocked reset with status '{participantResult.Status}'."));
                }
                else if (participantResult.Failed)
                {
                    issues.Add(ObjectResetIssue.Warning(
                        ObjectResetIssueKind.OptionalParticipantFailed,
                        $"Optional Object Reset participant '{participantResult.ParticipantId.StableText}' failed without blocking reset with status '{participantResult.Status}'."));
                }
            }

            var blockingIssues = issues.Count(issue => issue.IsBlocking);
            var nonBlockingIssues = issues.Count - blockingIssues;
            var status = blockingIssues > 0
                ? ObjectResetResultStatus.Failed
                : nonBlockingIssues > 0
                    ? ObjectResetResultStatus.CompletedWithWarnings
                    : ObjectResetResultStatus.Succeeded;
            var message = status == ObjectResetResultStatus.Succeeded
                ? "Object Reset completed successfully."
                : status == ObjectResetResultStatus.CompletedWithWarnings
                    ? "Object Reset completed with non-blocking participant warnings."
                    : "Object Reset failed because at least one required participant blocked reset.";

            return ObjectResetResult.FromExecution(
                request,
                status,
                targetResolution.ResolvedTarget,
                true,
                plan,
                participantResults,
                issues,
                message);
        }

        private ObjectResetPlan BuildPlan(
            ObjectResetRequest request,
            ObjectEntryDescriptor resolvedTarget,
            IObjectResetParticipantSource participantSource)
        {
            var issues = new List<ObjectResetIssue>();
            var entries = new List<ObjectResetParticipantEntry>();
            IReadOnlyList<IObjectResetParticipant> participants;

            try
            {
                participants = participantSource == null
                    ? Array.Empty<IObjectResetParticipant>()
                    : participantSource.ResolveObjectResetParticipants(request, resolvedTarget) ?? Array.Empty<IObjectResetParticipant>();
            }
            catch (Exception exception)
            {
                issues.Add(ObjectResetIssue.Error(
                    ObjectResetIssueKind.ParticipantSourceException,
                    $"Object Reset participant source threw while resolving participants: {exception.Message}"));
                participants = Array.Empty<IObjectResetParticipant>();
            }

            var participantIds = new HashSet<ObjectResetParticipantId>();
            for (var index = 0; index < participants.Count; index++)
            {
                var participant = participants[index];
                if (participant == null)
                {
                    issues.Add(ObjectResetIssue.Warning(
                        ObjectResetIssueKind.InvalidParticipant,
                        $"Object Reset participant source returned a null participant at index '{index}'."));
                    continue;
                }

                ObjectResetParticipantDescriptor descriptor;
                try
                {
                    descriptor = participant.GetObjectResetDescriptor();
                }
                catch (Exception exception)
                {
                    issues.Add(ObjectResetIssue.Error(
                        ObjectResetIssueKind.InvalidParticipant,
                        $"Object Reset participant descriptor failed at index '{index}': {exception.Message}"));
                    continue;
                }

                if (!descriptor.IsValid)
                {
                    issues.Add(ObjectResetIssue.Error(
                        ObjectResetIssueKind.InvalidParticipant,
                        $"Object Reset participant descriptor at index '{index}' is invalid."));
                    continue;
                }

                if (!descriptor.SupportsResolvedTarget(request, resolvedTarget))
                {
                    issues.Add(ObjectResetIssue.Warning(
                        ObjectResetIssueKind.ForeignOrStaleParticipant,
                        $"Object Reset participant '{descriptor.ParticipantId.StableText}' does not support the resolved request target and was ignored."));
                    continue;
                }

                if (!participantIds.Add(descriptor.ParticipantId))
                {
                    issues.Add(ObjectResetIssue.Error(
                        ObjectResetIssueKind.DuplicateParticipant,
                        $"Object Reset participant id '{descriptor.ParticipantId.StableText}' was provided more than once."));
                    continue;
                }

                entries.Add(new ObjectResetParticipantEntry(participant, descriptor, index));
            }

            if (entries.Count == 0 && !request.AllowsNoParticipants)
            {
                issues.Add(ObjectResetIssue.Error(
                    ObjectResetIssueKind.NoParticipants,
                    "Object Reset request rejected because no participants were available and policy disallows no-participant success."));
            }

            return new ObjectResetPlan(
                request,
                resolvedTarget,
                entries,
                issues);
        }
    }
}

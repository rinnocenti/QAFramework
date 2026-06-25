using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Executes an already-built Cycle Reset plan and aggregates participant results.
    /// It does not discover scene objects, reset Unity components, release content, reload scenes, restore snapshots or call gameplay systems directly.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11A Cycle Reset executor; executes supplied participants only and has no Unity side effects.")]
    public sealed class CycleResetRuntime
    {
        public CycleResetPlan CreatePlan(
            CycleResetRequest request,
            IReadOnlyList<ICycleResetParticipant> participants,
            string source,
            string reason)
        {
            return CycleResetPlan.FromParticipants(request, participants, source, reason);
        }

        public CycleResetResult ExecutePlan(
            CycleResetPlan plan,
            string source,
            string reason)
        {
            var sourceText = Normalize(source);
            var reasonText = Normalize(reason);

            if (!plan.Request.IsValid)
            {
                return CycleResetResult.RejectedInvalidPlan(
                    plan.Request,
                    plan.SnapshotIssues(),
                    sourceText,
                    reasonText,
                    "Cycle Reset runtime rejected the plan because the request is invalid.");
            }

            if (plan.Rejected)
            {
                return CycleResetResult.RejectedInvalidPlan(
                    plan.Request,
                    plan.SnapshotIssues(),
                    sourceText,
                    reasonText,
                    "Cycle Reset runtime rejected the plan because the plan was already rejected.");
            }

            if (plan.Skipped || !plan.HasEntries)
            {
                return CycleResetResult.FromResults(
                    plan.Request,
                    Array.Empty<CycleResetParticipantResult>(),
                    plan.SnapshotIssues(),
                    sourceText,
                    reasonText,
                    "Cycle Reset runtime completed with no participants to execute.");
            }

            if (!plan.Planned)
            {
                return CycleResetResult.RejectedInvalidPlan(
                    plan.Request,
                    plan.SnapshotIssues(),
                    sourceText,
                    reasonText,
                    "Cycle Reset runtime rejected the plan because it is not executable.");
            }

            var results = new List<CycleResetParticipantResult>(plan.EntryCount);
            var executionIssues = new List<CycleResetIssue>(plan.SnapshotIssues());
            var entries = plan.Entries;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (!entry.IsValid)
                {
                    executionIssues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.InvalidDescriptor,
                        entry.ParticipantId,
                        entry.ParticipantScope,
                        "Cycle Reset runtime found an invalid participant entry."));
                    continue;
                }

                if (!entry.Descriptor.SupportsRequest(plan.Request))
                {
                    executionIssues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.UnsupportedScope,
                        entry.ParticipantId,
                        entry.ParticipantScope,
                        "Cycle Reset runtime found a participant entry that does not support the request scope."));
                    continue;
                }

                try
                {
                    var context = new CycleResetContext(plan.Request, entry.Descriptor);
                    var result = entry.Participant.ResetCycle(context);
                    if (!IsValidResultForEntry(result, entry, plan.Request, out var invalidResultMessage))
                    {
                        results.Add(CreateFailure(entry, plan.Request, sourceText, reasonText, invalidResultMessage));
                        executionIssues.Add(CreateResultIssue(entry, invalidResultMessage));
                        continue;
                    }

                    results.Add(result);
                    if (result.Failed)
                    {
                        executionIssues.Add(CreateFailureIssue(result));
                    }
                }
                catch (Exception exception)
                {
                    var message = $"Cycle Reset participant threw an exception: {exception.GetType().Name}.";
                    results.Add(CreateFailure(entry, plan.Request, sourceText, reasonText, message));
                    executionIssues.Add(CycleResetIssue.BlockingIssue(
                        CycleResetIssueKind.ParticipantException,
                        entry.ParticipantId,
                        entry.ParticipantScope,
                        message));
                }
            }

            return CycleResetResult.FromResults(
                plan.Request,
                results,
                executionIssues,
                sourceText,
                reasonText,
                "Cycle Reset runtime executed the supplied plan.");
        }

        private static CycleResetParticipantResult CreateFailure(
            CycleResetParticipantEntry entry,
            CycleResetRequest request,
            string source,
            string reason,
            string message)
        {
            var context = new CycleResetContext(request, entry.Descriptor);
            return CycleResetParticipantResult.Failure(context, 1, source, reason, message);
        }

        private static CycleResetIssue CreateResultIssue(CycleResetParticipantEntry entry, string message)
        {
            return entry.IsRequired
                ? CycleResetIssue.BlockingIssue(
                    CycleResetIssueKind.InvalidParticipantResult,
                    entry.ParticipantId,
                    entry.ParticipantScope,
                    message)
                : CycleResetIssue.NonBlockingIssue(
                    CycleResetIssueKind.InvalidParticipantResult,
                    entry.ParticipantId,
                    entry.ParticipantScope,
                    message);
        }

        private static CycleResetIssue CreateFailureIssue(CycleResetParticipantResult result)
        {
            return result.BlocksReset
                ? CycleResetIssue.BlockingIssue(
                    CycleResetIssueKind.RequiredParticipantFailed,
                    result.ParticipantId,
                    result.ParticipantScope,
                    result.Message)
                : CycleResetIssue.NonBlockingIssue(
                    CycleResetIssueKind.OptionalParticipantFailed,
                    result.ParticipantId,
                    result.ParticipantScope,
                    result.Message);
        }

        private static bool IsValidResultForEntry(
            CycleResetParticipantResult result,
            CycleResetParticipantEntry entry,
            CycleResetRequest request,
            out string message)
        {
            if (!result.IsValid)
            {
                message = "Cycle Reset participant returned an invalid result.";
                return false;
            }

            if (!result.Request.Equals(request))
            {
                message = "Cycle Reset participant returned a result for a different request.";
                return false;
            }

            if (!result.ParticipantId.Equals(entry.ParticipantId))
            {
                message = "Cycle Reset participant returned a result for a different participant id.";
                return false;
            }

            if (result.ParticipantScope != entry.ParticipantScope)
            {
                message = "Cycle Reset participant returned a result for a different participant scope.";
                return false;
            }

            if (result.Requiredness != entry.Requiredness)
            {
                message = "Cycle Reset participant returned a result with different requiredness.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

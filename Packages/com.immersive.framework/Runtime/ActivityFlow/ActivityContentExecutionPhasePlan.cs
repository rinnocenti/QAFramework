using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive phase plan for Activity Content Execution.
    /// It pairs ordered participants with generated execution requests for one phase only; it does not execute participants or mutate Unity objects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10F Activity Content Execution phase plan; no execution runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionPhasePlan : IEquatable<ActivityContentExecutionPhasePlan>
    {
        private readonly ActivityContentExecutionParticipantEntry[] entries;
        private readonly ActivityContentExecutionRequest[] requests;
        private readonly ActivityContentExecutionParticipantCollectionIssue[] collectionIssues;

        public ActivityContentExecutionPhasePlan(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            IReadOnlyList<ActivityContentExecutionParticipantEntry> entries,
            IReadOnlyList<ActivityContentExecutionRequest> requests,
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> collectionIssues,
            ActivityContentExecutionPhasePlanStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhase), phase)
                || phase == ActivityContentExecutionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Activity content execution phase plan phase must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhasePlanStatus), status)
                || status == ActivityContentExecutionPhasePlanStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution phase plan status must be explicit.");
            }

            Phase = phase;
            Activity = activity;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Context = context;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);

            this.entries = CopyEntries(entries);
            this.requests = CopyRequests(requests, phase);
            this.collectionIssues = CopyIssues(collectionIssues);
        }

        public ActivityContentExecutionPhase Phase { get; }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset NextActivity { get; }

        public RuntimeScopeContext Context { get; }

        public RuntimeContentOwner Owner => Context.Owner;

        public RuntimeContentScope Scope => Context.Scope;

        public ActivityContentExecutionPhasePlanStatus Status { get; }

        public IReadOnlyList<ActivityContentExecutionParticipantEntry> Entries => entries ?? Array.Empty<ActivityContentExecutionParticipantEntry>();

        public IReadOnlyList<ActivityContentExecutionRequest> Requests => requests ?? Array.Empty<ActivityContentExecutionRequest>();

        public IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> CollectionIssues => collectionIssues ?? Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool IsEnter => Phase == ActivityContentExecutionPhase.Enter;

        public bool IsExit => Phase == ActivityContentExecutionPhase.Exit;

        public bool HasActivity => Activity != null;

        public bool HasContext => Context.IsValid;

        public bool HasEntries => EntryCount > 0;

        public bool HasRequests => RequestCount > 0;

        public bool HasCollectionIssues => CollectionIssueCount > 0;

        public bool Planned => Status == ActivityContentExecutionPhasePlanStatus.Planned;

        public bool Skipped => Status == ActivityContentExecutionPhasePlanStatus.SkippedNoParticipants;

        public bool Rejected => Status == ActivityContentExecutionPhasePlanStatus.RejectedInvalidPhase
            || Status == ActivityContentExecutionPhasePlanStatus.RejectedMissingActivity
            || Status == ActivityContentExecutionPhasePlanStatus.RejectedInvalidContext
            || Status == ActivityContentExecutionPhasePlanStatus.RejectedInvalidParticipants;

        public string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        public string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        public string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public int EntryCount => entries != null ? entries.Length : 0;

        public int RequestCount => requests != null ? requests.Length : 0;

        public int CollectionIssueCount => collectionIssues != null ? collectionIssues.Length : 0;

        public int RequiredCount => CountByRequiredness(ActivityContentExecutionRequiredness.Required);

        public int OptionalCount => CountByRequiredness(ActivityContentExecutionRequiredness.Optional);

        public int NullParticipantIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.NullParticipant);

        public int InvalidDescriptorIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.InvalidDescriptor);

        public int DuplicateContentIdIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.DuplicateContentId);

        public ActivityContentExecutionParticipantEntry[] SnapshotEntries()
        {
            if (!HasEntries)
            {
                return Array.Empty<ActivityContentExecutionParticipantEntry>();
            }

            var snapshot = new ActivityContentExecutionParticipantEntry[EntryCount];
            for (var i = 0; i < EntryCount; i++)
            {
                snapshot[i] = Entries[i];
            }

            return snapshot;
        }

        public ActivityContentExecutionRequest[] SnapshotRequests()
        {
            if (!HasRequests)
            {
                return Array.Empty<ActivityContentExecutionRequest>();
            }

            var snapshot = new ActivityContentExecutionRequest[RequestCount];
            for (var i = 0; i < RequestCount; i++)
            {
                snapshot[i] = Requests[i];
            }

            return snapshot;
        }

        public ActivityContentExecutionParticipantCollectionIssue[] SnapshotCollectionIssues()
        {
            if (!HasCollectionIssues)
            {
                return Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();
            }

            var snapshot = new ActivityContentExecutionParticipantCollectionIssue[CollectionIssueCount];
            for (var i = 0; i < CollectionIssueCount; i++)
            {
                snapshot[i] = CollectionIssues[i];
            }

            return snapshot;
        }

        public bool Equals(ActivityContentExecutionPhasePlan other)
        {
            if (Phase != other.Phase
                || !ReferenceEquals(Activity, other.Activity)
                || !ReferenceEquals(PreviousActivity, other.PreviousActivity)
                || !ReferenceEquals(NextActivity, other.NextActivity)
                || !Context.Equals(other.Context)
                || Status != other.Status
                || !string.Equals(Source, other.Source, StringComparison.Ordinal)
                || !string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || EntryCount != other.EntryCount
                || RequestCount != other.RequestCount
                || CollectionIssueCount != other.CollectionIssueCount)
            {
                return false;
            }

            for (var i = 0; i < EntryCount; i++)
            {
                if (!Entries[i].Equals(other.Entries[i]))
                {
                    return false;
                }
            }

            for (var i = 0; i < RequestCount; i++)
            {
                if (!Requests[i].Equals(other.Requests[i]))
                {
                    return false;
                }
            }

            for (var i = 0; i < CollectionIssueCount; i++)
            {
                if (!CollectionIssues[i].Equals(other.CollectionIssues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionPhasePlan other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Phase;
                hashCode = (hashCode * 397) ^ (Activity != null ? Activity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PreviousActivity != null ? PreviousActivity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NextActivity != null ? NextActivity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Context.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                for (var i = 0; i < EntryCount; i++)
                {
                    hashCode = (hashCode * 397) ^ Entries[i].GetHashCode();
                }

                for (var i = 0; i < RequestCount; i++)
                {
                    hashCode = (hashCode * 397) ^ Requests[i].GetHashCode();
                }

                for (var i = 0; i < CollectionIssueCount; i++)
                {
                    hashCode = (hashCode * 397) ^ CollectionIssues[i].GetHashCode();
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var activityText = !string.IsNullOrWhiteSpace(ActivityName) ? ActivityName : "<none>";
            var previousText = !string.IsNullOrWhiteSpace(PreviousActivityName) ? PreviousActivityName : "<none>";
            var nextText = !string.IsNullOrWhiteSpace(NextActivityName) ? NextActivityName : "<none>";
            var sourceText = !string.IsNullOrWhiteSpace(Source) ? Source : "<none>";
            var reasonText = !string.IsNullOrWhiteSpace(Reason) ? Reason : "<none>";
            var messageText = !string.IsNullOrWhiteSpace(Message) ? Message : "<none>";
            var builder = new StringBuilder();
            builder.Append($"Activity Content Execution Phase Plan phase='{Phase}' activity='{activityText}' previousActivity='{previousText}' nextActivity='{nextText}' status='{Status}' requests='{RequestCount}' entries='{EntryCount}' required='{RequiredCount}' optional='{OptionalCount}' collectionIssues='{CollectionIssueCount}' nullParticipants='{NullParticipantIssueCount}' invalidDescriptors='{InvalidDescriptorIssueCount}' duplicateContentIds='{DuplicateContentIdIssueCount}' owner='{Owner.StableText}' source='{sourceText}' reason='{reasonText}' message='{messageText}' entries=[");
            for (var i = 0; i < Entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Entries[i].ToDiagnosticString());
            }

            builder.Append("] requests=[");
            for (var i = 0; i < Requests.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Requests[i].ToDiagnosticString());
            }

            builder.Append("] collectionIssues=[");
            for (var i = 0; i < CollectionIssues.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(CollectionIssues[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        public static ActivityContentExecutionPhasePlan PlannedResult(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            IReadOnlyList<ActivityContentExecutionParticipantEntry> entries,
            IReadOnlyList<ActivityContentExecutionRequest> requests,
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> collectionIssues,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionPhasePlan(
                phase,
                activity,
                previousActivity,
                nextActivity,
                context,
                entries,
                requests,
                collectionIssues,
                ActivityContentExecutionPhasePlanStatus.Planned,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution phase plan was created." : message);
        }

        public static ActivityContentExecutionPhasePlan SkippedNoParticipants(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> collectionIssues,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionPhasePlan(
                phase,
                activity,
                previousActivity,
                nextActivity,
                context,
                Array.Empty<ActivityContentExecutionParticipantEntry>(),
                Array.Empty<ActivityContentExecutionRequest>(),
                collectionIssues,
                ActivityContentExecutionPhasePlanStatus.SkippedNoParticipants,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution phase plan skipped because no participants support the phase." : message);
        }

        public static ActivityContentExecutionPhasePlan RejectedResult(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            RuntimeScopeContext context,
            ActivityContentExecutionPhasePlanStatus status,
            IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> collectionIssues,
            string source,
            string reason,
            string message)
        {
            if (status == ActivityContentExecutionPhasePlanStatus.Planned
                || status == ActivityContentExecutionPhasePlanStatus.SkippedNoParticipants
                || status == ActivityContentExecutionPhasePlanStatus.Unknown)
            {
                throw new ArgumentException("Rejected Activity content execution phase plan requires a rejected status.", nameof(status));
            }

            return new ActivityContentExecutionPhasePlan(
                phase,
                activity,
                previousActivity,
                nextActivity,
                context,
                Array.Empty<ActivityContentExecutionParticipantEntry>(),
                Array.Empty<ActivityContentExecutionRequest>(),
                collectionIssues,
                status,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution phase plan was rejected." : message);
        }

        public static bool operator ==(ActivityContentExecutionPhasePlan left, ActivityContentExecutionPhasePlan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActivityContentExecutionPhasePlan left, ActivityContentExecutionPhasePlan right)
        {
            return !left.Equals(right);
        }

        private int CountByRequiredness(ActivityContentExecutionRequiredness requiredness)
        {
            if (!HasRequests)
            {
                return 0;
            }

            var count = 0;
            var items = Requests;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Requiredness == requiredness)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind kind)
        {
            if (!HasCollectionIssues)
            {
                return 0;
            }

            var count = 0;
            var items = CollectionIssues;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static ActivityContentExecutionParticipantEntry[] CopyEntries(IReadOnlyList<ActivityContentExecutionParticipantEntry> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<ActivityContentExecutionParticipantEntry>();
            }

            var copy = new ActivityContentExecutionParticipantEntry[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Activity content execution phase plan entries must be valid.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static ActivityContentExecutionRequest[] CopyRequests(IReadOnlyList<ActivityContentExecutionRequest> source, ActivityContentExecutionPhase phase)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<ActivityContentExecutionRequest>();
            }

            var copy = new ActivityContentExecutionRequest[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                var request = source[i];
                if (!request.IsValid)
                {
                    throw new ArgumentException("Activity content execution phase plan requests must be valid.", nameof(source));
                }

                if (request.Phase != phase)
                {
                    throw new ArgumentException("Activity content execution phase plan requests must match the plan phase.", nameof(source));
                }

                copy[i] = request;
            }

            return copy;
        }

        private static ActivityContentExecutionParticipantCollectionIssue[] CopyIssues(IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();
            }

            var copy = new ActivityContentExecutionParticipantCollectionIssue[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = source[i];
            }

            return copy;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

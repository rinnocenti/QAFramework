using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Immutable passive collection of Activity Content Execution participants.
    /// It validates descriptors, removes invalid/duplicate entries and exposes deterministic ordering only; it does not discover participants, execute requests or integrate with Activity lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Activity Content Execution participant collection and ordering model; no discovery runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionParticipantCollection
    {
        private readonly ActivityContentExecutionParticipantEntry[] _entries;
        private readonly ActivityContentExecutionParticipantCollectionIssue[] _issues;

        public ActivityContentExecutionParticipantCollection(IReadOnlyList<IActivityContentExecutionParticipant> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                _entries = Array.Empty<ActivityContentExecutionParticipantEntry>();
                _issues = Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();
                return;
            }

            var acceptedEntries = new List<ActivityContentExecutionParticipantEntry>(participants.Count);
            var detectedIssues = new List<ActivityContentExecutionParticipantCollectionIssue>();
            var contentIdKeys = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < participants.Count; i++)
            {
                var participant = participants[i];
                if (participant == null)
                {
                    detectedIssues.Add(new ActivityContentExecutionParticipantCollectionIssue(
                        ActivityContentExecutionParticipantCollectionIssueKind.NullParticipant,
                        i,
                        $"index:{i}",
                        default,
                        "Activity content execution participant is null and was not added to the collection."));
                    continue;
                }

                var descriptor = participant.GetActivityContentExecutionDescriptor();
                if (!descriptor.IsValid)
                {
                    detectedIssues.Add(new ActivityContentExecutionParticipantCollectionIssue(
                        ActivityContentExecutionParticipantCollectionIssueKind.InvalidDescriptor,
                        i,
                        CreateIssueKey(descriptor, i),
                        descriptor,
                        "Activity content execution participant descriptor is invalid and was not added to the collection."));
                    continue;
                }

                var contentIdKey = descriptor.ContentId.StableText;
                if (contentIdKeys.Contains(contentIdKey))
                {
                    detectedIssues.Add(new ActivityContentExecutionParticipantCollectionIssue(
                        ActivityContentExecutionParticipantCollectionIssueKind.DuplicateContentId,
                        i,
                        contentIdKey,
                        descriptor,
                        "Activity content execution participant content id already exists in the collection; duplicate participant was not added."));
                    continue;
                }

                contentIdKeys.Add(contentIdKey);
                acceptedEntries.Add(new ActivityContentExecutionParticipantEntry(participant, descriptor, i));
            }

            if (acceptedEntries.Count == 0)
            {
                _entries = Array.Empty<ActivityContentExecutionParticipantEntry>();
            }
            else
            {
                var orderedEntries = acceptedEntries.ToArray();
                Array.Sort(orderedEntries, CompareEntries);
                _entries = orderedEntries;
            }

            _issues = detectedIssues.Count == 0
                ? Array.Empty<ActivityContentExecutionParticipantCollectionIssue>()
                : detectedIssues.ToArray();
        }

        public IReadOnlyList<ActivityContentExecutionParticipantEntry> Entries => _entries ?? Array.Empty<ActivityContentExecutionParticipantEntry>();

        public IReadOnlyList<ActivityContentExecutionParticipantCollectionIssue> Issues => _issues ?? Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();

        public int Count => Entries.Count;

        public int IssueCount => Issues.Count;

        public bool HasParticipants => Count > 0;

        public bool IsEmpty => Count == 0;

        public bool HasIssues => IssueCount > 0;

        public int RequiredCount => CountByRequiredness(ActivityContentExecutionRequiredness.Required);

        public int OptionalCount => CountByRequiredness(ActivityContentExecutionRequiredness.Optional);

        public int EnterCount => CountByPhase(ActivityContentExecutionPhase.Enter);

        public int ExitCount => CountByPhase(ActivityContentExecutionPhase.Exit);

        public int NullParticipantIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.NullParticipant);

        public int InvalidDescriptorIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.InvalidDescriptor);

        public int DuplicateContentIdIssueCount => CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind.DuplicateContentId);

        public bool Contains(RuntimeContentId contentId)
        {
            return TryGetEntry(contentId, out _);
        }

        public bool TryGetEntry(RuntimeContentId contentId, out ActivityContentExecutionParticipantEntry entry)
        {
            if (!contentId.IsValid || !HasParticipants)
            {
                entry = default;
                return false;
            }

            var items = Entries;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].ContentId.Equals(contentId))
                {
                    entry = items[i];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        public ActivityContentExecutionParticipantEntry[] SnapshotEntries()
        {
            if (!HasParticipants)
            {
                return Array.Empty<ActivityContentExecutionParticipantEntry>();
            }

            var snapshot = new ActivityContentExecutionParticipantEntry[Count];
            for (var i = 0; i < Count; i++)
            {
                snapshot[i] = Entries[i];
            }

            return snapshot;
        }

        public ActivityContentExecutionParticipantEntry[] SnapshotEntriesForPhase(ActivityContentExecutionPhase phase)
        {
            ValidatePhase(phase);

            if (!HasParticipants)
            {
                return Array.Empty<ActivityContentExecutionParticipantEntry>();
            }

            var selected = new List<ActivityContentExecutionParticipantEntry>();
            var items = Entries;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].SupportsPhase(phase))
                {
                    selected.Add(items[i]);
                }
            }

            return selected.Count == 0 ? Array.Empty<ActivityContentExecutionParticipantEntry>() : selected.ToArray();
        }

        public ActivityContentExecutionParticipantDescriptor[] SnapshotDescriptors()
        {
            if (!HasParticipants)
            {
                return Array.Empty<ActivityContentExecutionParticipantDescriptor>();
            }

            var snapshot = new ActivityContentExecutionParticipantDescriptor[Count];
            for (var i = 0; i < Count; i++)
            {
                snapshot[i] = Entries[i].Descriptor;
            }

            return snapshot;
        }

        public ActivityContentExecutionParticipantDescriptor[] SnapshotDescriptorsForPhase(ActivityContentExecutionPhase phase)
        {
            var phaseEntries = SnapshotEntriesForPhase(phase);
            if (phaseEntries.Length == 0)
            {
                return Array.Empty<ActivityContentExecutionParticipantDescriptor>();
            }

            var descriptors = new ActivityContentExecutionParticipantDescriptor[phaseEntries.Length];
            for (var i = 0; i < phaseEntries.Length; i++)
            {
                descriptors[i] = phaseEntries[i].Descriptor;
            }

            return descriptors;
        }

        public ActivityContentExecutionParticipantCollectionIssue[] SnapshotIssues()
        {
            if (!HasIssues)
            {
                return Array.Empty<ActivityContentExecutionParticipantCollectionIssue>();
            }

            var snapshot = new ActivityContentExecutionParticipantCollectionIssue[IssueCount];
            for (var i = 0; i < IssueCount; i++)
            {
                snapshot[i] = Issues[i];
            }

            return snapshot;
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append($"Activity Content Execution Participant Collection count='{Count}' required='{RequiredCount}' optional='{OptionalCount}' enter='{EnterCount}' exit='{ExitCount}' issues='{IssueCount}' nullParticipants='{NullParticipantIssueCount}' invalidDescriptors='{InvalidDescriptorIssueCount}' duplicateContentIds='{DuplicateContentIdIssueCount}' entries=[");
            var items = Entries;
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(items[i].ToDiagnosticString());
            }

            builder.Append("] issues=[");
            var issueItems = Issues;
            for (var i = 0; i < issueItems.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(issueItems[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        public static ActivityContentExecutionParticipantCollection Empty()
        {
            return new ActivityContentExecutionParticipantCollection(Array.Empty<IActivityContentExecutionParticipant>());
        }

        public static ActivityContentExecutionParticipantCollection FromParticipants(IReadOnlyList<IActivityContentExecutionParticipant> participants)
        {
            return new ActivityContentExecutionParticipantCollection(participants);
        }

        private int CountByRequiredness(ActivityContentExecutionRequiredness requiredness)
        {
            if (!HasParticipants)
            {
                return 0;
            }

            var count = 0;
            var items = Entries;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Requiredness == requiredness)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountByPhase(ActivityContentExecutionPhase phase)
        {
            if (!HasParticipants)
            {
                return 0;
            }

            var count = 0;
            var items = Entries;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].SupportsPhase(phase))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountIssuesByKind(ActivityContentExecutionParticipantCollectionIssueKind kind)
        {
            if (!HasIssues)
            {
                return 0;
            }

            var count = 0;
            var items = Issues;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CompareEntries(ActivityContentExecutionParticipantEntry left, ActivityContentExecutionParticipantEntry right)
        {
            var orderCompare = left.Order.CompareTo(right.Order);
            if (orderCompare != 0)
            {
                return orderCompare;
            }

            var indexCompare = left.SourceIndex.CompareTo(right.SourceIndex);
            if (indexCompare != 0)
            {
                return indexCompare;
            }

            return string.Compare(left.ContentId.StableText, right.ContentId.StableText, StringComparison.Ordinal);
        }

        private static string CreateIssueKey(ActivityContentExecutionParticipantDescriptor descriptor, int index)
        {
            if (descriptor.ContentId.IsValid)
            {
                return descriptor.ContentId.StableText;
            }

            return $"index:{index}";
        }

        private static void ValidatePhase(ActivityContentExecutionPhase phase)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhase), phase)
                || phase == ActivityContentExecutionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Activity content execution phase must be explicit.");
            }
        }
    }
}

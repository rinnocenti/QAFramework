using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Side-effect-free result produced from an Activity scene composition plan.
    /// F25B intentionally does not load scenes or release content.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25B Activity scene composition result; plan evidence only, execution deferred.")]
    internal readonly struct ActivitySceneCompositionResult
    {
        private readonly ActivitySceneCompositionResultEntry[] _entries;

        public ActivitySceneCompositionResult(
            ActivitySceneCompositionPlan plan,
            IReadOnlyList<ActivitySceneCompositionResultEntry> entries,
            ActivitySceneCompositionStatus status,
            string source,
            string reason,
            string message)
        {
            Plan = plan;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);

            if (entries == null || entries.Count == 0)
            {
                _entries = Array.Empty<ActivitySceneCompositionResultEntry>();
            }
            else
            {
                _entries = new ActivitySceneCompositionResultEntry[entries.Count];
                for (var i = 0; i < entries.Count; i++)
                {
                    _entries[i] = entries[i];
                }
            }
        }

        public ActivitySceneCompositionPlan Plan { get; }

        public ActivityAsset Activity => Plan.Activity;

        public ActivityContentProfileAsset Profile => Plan.Profile;

        public string ProfileId => Plan.ProfileId;

        public string ActivityOwnerId => Plan.ActivityOwnerId;

        public string ActivityOwnerName => Plan.ActivityOwnerName;

        public ActivitySceneCompositionStatus Status { get; }

        public IReadOnlyList<ActivitySceneCompositionResultEntry> Entries => _entries ?? Array.Empty<ActivitySceneCompositionResultEntry>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Executed => Status != ActivitySceneCompositionStatus.Unknown;

        public bool HasProfile => Plan.HasProfile;

        public bool HasScenes => SceneCount > 0;

        public int SceneCount => Entries.Count;

        public int RequiredSceneCount => Plan.RequiredSceneCount;

        public int OptionalSceneCount => Plan.OptionalSceneCount;

        public int ExecutionReadySceneCount => Plan.ExecutionReadySceneCount;

        public int BlockingIssueCount => CountBlockingIssues();

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public string DiagnosticStatus => Status.ToString();

        public static ActivitySceneCompositionResult FromPlan(ActivitySceneCompositionPlan plan, string source, string reason)
        {
            if (!plan.HasActivity)
            {
                return new ActivitySceneCompositionResult(
                    plan,
                    Array.Empty<ActivitySceneCompositionResultEntry>(),
                    ActivitySceneCompositionStatus.NotRequested,
                    source,
                    reason,
                    "Activity scene composition planning was not requested because Activity is missing.");
            }

            if (!plan.HasProfile)
            {
                return new ActivitySceneCompositionResult(
                    plan,
                    Array.Empty<ActivitySceneCompositionResultEntry>(),
                    ActivitySceneCompositionStatus.NotRequested,
                    source,
                    reason,
                    "Activity scene composition planning skipped because Activity has no Activity Content Profile.");
            }

            if (!plan.HasScenes)
            {
                return new ActivitySceneCompositionResult(
                    plan,
                    Array.Empty<ActivitySceneCompositionResultEntry>(),
                    ActivitySceneCompositionStatus.NotRequested,
                    source,
                    reason,
                    "Activity scene composition planning skipped because Activity Content Profile has no scenes.");
            }

            var entries = new List<ActivitySceneCompositionResultEntry>(plan.SceneCount);
            for (var i = 0; i < plan.Scenes.Count; i++)
            {
                var entry = plan.Scenes[i];
                entries.Add(entry.IsExecutionReady
                    ? ActivitySceneCompositionResultEntry.PlannedEntry(entry)
                    : ActivitySceneCompositionResultEntry.NotExecutionReadyEntry(entry));
            }

            var status = EntriesHaveBlockingIssues(entries)
                ? ActivitySceneCompositionStatus.PlannedWithIssues
                : ActivitySceneCompositionStatus.Planned;
            var message = status == ActivitySceneCompositionStatus.Planned
                ? "Activity scene composition plan recorded. F25B is side-effect-free; scene loading is deferred."
                : "Activity scene composition plan recorded with blocking declaration issues. F25B is side-effect-free; scene loading is deferred.";

            return new ActivitySceneCompositionResult(
                plan,
                entries,
                status,
                source,
                reason,
                message);
        }

        public string ToDiagnosticString()
        {
            var activityName = !string.IsNullOrWhiteSpace(ActivityOwnerName) ? ActivityOwnerName : "<missing>";
            var builder = new StringBuilder();
            builder.Append($"Activity Scene Composition Result activity='{activityName}' profile='{ProfileId}' status='{Status}' scenes='{SceneCount}' required='{RequiredSceneCount}' optional='{OptionalSceneCount}' executionReady='{ExecutionReadySceneCount}' blockingIssues='{BlockingIssueCount}' sideEffects='False' message='{Message}' details=[");

            for (var i = 0; i < Entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Entries[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static bool EntriesHaveBlockingIssues(IReadOnlyList<ActivitySceneCompositionResultEntry> entries)
        {
            if (entries == null)
            {
                return false;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                if (entries[i].BlocksComposition)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountBlockingIssues()
        {
            var count = 0;
            for (var i = 0; i < Entries.Count; i++)
            {
                if (Entries[i].BlocksComposition)
                {
                    count++;
                }
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

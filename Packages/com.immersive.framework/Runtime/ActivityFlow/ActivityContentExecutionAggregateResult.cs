using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive aggregate result for a logical Activity Content Execution phase.
    /// It combines per-content execution results for readiness and diagnostics only; it does not discover participants, execute adapters, mutate GameObjects, reset objects or release physical resources.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10C Activity Content Execution aggregate result contract; no execution runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionAggregateResult : IEquatable<ActivityContentExecutionAggregateResult>
    {
        private readonly ActivityContentExecutionResult[] _results;

        public ActivityContentExecutionAggregateResult(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            IReadOnlyList<ActivityContentExecutionResult> results,
            ActivityContentExecutionAggregateStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionPhase), phase)
                || phase == ActivityContentExecutionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "Activity content execution aggregate phase must be explicit.");
            }

            if (!Enum.IsDefined(typeof(ActivityContentExecutionAggregateStatus), status)
                || status == ActivityContentExecutionAggregateStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution aggregate status must be explicit.");
            }

            Phase = phase;
            Activity = activity;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);

            if (results == null || results.Count == 0)
            {
                this._results = Array.Empty<ActivityContentExecutionResult>();
            }
            else
            {
                this._results = new ActivityContentExecutionResult[results.Count];
                for (var i = 0; i < results.Count; i++)
                {
                    var result = results[i];
                    if (result.Phase != phase)
                    {
                        throw new ArgumentException("All Activity content execution results in an aggregate must use the aggregate phase.", nameof(results));
                    }

                    this._results[i] = result;
                }
            }
        }

        public ActivityContentExecutionPhase Phase { get; }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset NextActivity { get; }

        public ActivityContentExecutionAggregateStatus Status { get; }

        public IReadOnlyList<ActivityContentExecutionResult> Results => _results ?? Array.Empty<ActivityContentExecutionResult>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool IsEnter => Phase == ActivityContentExecutionPhase.Enter;

        public bool IsExit => Phase == ActivityContentExecutionPhase.Exit;

        public bool HasActivity => Activity != null;

        public string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        public string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        public string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public int ResultCount => _results != null ? _results.Length : 0;

        public int RequiredCount => CountByRequiredness(ActivityContentExecutionRequiredness.Required);

        public int OptionalCount => CountByRequiredness(ActivityContentExecutionRequiredness.Optional);

        public int SucceededCount => CountSucceeded();

        public int SkippedCount => CountSkipped();

        public int FailedCount => CountFailed();

        public int BlockingIssueCount => SumBlockingIssues();

        public int NonBlockingIssueCount => SumNonBlockingIssues();

        public int IssueCount => BlockingIssueCount + NonBlockingIssueCount;

        public bool HasResults => ResultCount > 0;

        public bool HasIssues => IssueCount > 0;

        public bool HasBlockingIssues => BlockingIssueCount > 0
            || Status == ActivityContentExecutionAggregateStatus.FailedBlocking;

        public bool HasNonBlockingIssues => NonBlockingIssueCount > 0
            || Status == ActivityContentExecutionAggregateStatus.FailedNonBlocking
            || Status == ActivityContentExecutionAggregateStatus.SucceededWithNonBlockingIssues;

        public bool BlocksReadiness => HasBlockingIssues;

        public bool Succeeded => Status == ActivityContentExecutionAggregateStatus.Succeeded
            || Status == ActivityContentExecutionAggregateStatus.SucceededNoOp
            || Status == ActivityContentExecutionAggregateStatus.SucceededWithNonBlockingIssues;

        public bool Skipped => Status == ActivityContentExecutionAggregateStatus.SkippedNoContent;

        public bool Failed => Status == ActivityContentExecutionAggregateStatus.FailedNonBlocking
            || Status == ActivityContentExecutionAggregateStatus.FailedBlocking
            || Status == ActivityContentExecutionAggregateStatus.RejectedInvalidResults;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(ActivityContentExecutionAggregateResult other)
        {
            if (Phase != other.Phase
                || !ReferenceEquals(Activity, other.Activity)
                || !ReferenceEquals(PreviousActivity, other.PreviousActivity)
                || !ReferenceEquals(NextActivity, other.NextActivity)
                || Status != other.Status
                || !string.Equals(Source, other.Source, StringComparison.Ordinal)
                || !string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || ResultCount != other.ResultCount)
            {
                return false;
            }

            for (var i = 0; i < ResultCount; i++)
            {
                if (!Results[i].Equals(other.Results[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionAggregateResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Phase;
                hashCode = (hashCode * 397) ^ (Activity != null ? Activity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (PreviousActivity != null ? PreviousActivity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NextActivity != null ? NextActivity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                for (var i = 0; i < ResultCount; i++)
                {
                    hashCode = (hashCode * 397) ^ Results[i].GetHashCode();
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
            builder.Append($"Activity Content Participant Execution Aggregate phase='{Phase}' activity='{activityText}' previousActivity='{previousText}' nextActivity='{nextText}' status='{Status}' results='{ResultCount}' required='{RequiredCount}' optional='{OptionalCount}' succeeded='{SucceededCount}' skipped='{SkippedCount}' failed='{FailedCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' blocksReadiness='{BlocksReadiness}' source='{sourceText}' reason='{reasonText}' message='{messageText}' details=[");
            for (var i = 0; i < Results.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Results[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        public static ActivityContentExecutionAggregateResult FromResults(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            IReadOnlyList<ActivityContentExecutionResult> results,
            string source,
            string reason,
            string message)
        {
            var status = DetermineStatus(results);
            return new ActivityContentExecutionAggregateResult(
                phase,
                activity,
                previousActivity,
                nextActivity,
                results,
                status,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? BuildDefaultMessage(status) : message);
        }

        public static ActivityContentExecutionAggregateResult SkippedNoContent(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionAggregateResult(
                phase,
                activity,
                previousActivity,
                nextActivity,
                Array.Empty<ActivityContentExecutionResult>(),
                ActivityContentExecutionAggregateStatus.SkippedNoContent,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content participant execution aggregate skipped because no participant results were provided." : message);
        }

        public static ActivityContentExecutionAggregateResult RejectedInvalidResults(
            ActivityContentExecutionPhase phase,
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionAggregateResult(
                phase,
                activity,
                previousActivity,
                nextActivity,
                Array.Empty<ActivityContentExecutionResult>(),
                ActivityContentExecutionAggregateStatus.RejectedInvalidResults,
                source,
                reason,
                string.IsNullOrWhiteSpace(message) ? "Activity content execution aggregate rejected invalid results." : message);
        }

        private static ActivityContentExecutionAggregateStatus DetermineStatus(IReadOnlyList<ActivityContentExecutionResult> candidateResults)
        {
            if (candidateResults == null || candidateResults.Count == 0)
            {
                return ActivityContentExecutionAggregateStatus.SkippedNoContent;
            }

            var succeeded = 0;
            var skipped = 0;
            var failed = 0;
            var blockingIssues = 0;
            var nonBlockingIssues = 0;

            for (var i = 0; i < candidateResults.Count; i++)
            {
                var result = candidateResults[i];
                if (result.HasBlockingIssues)
                {
                    blockingIssues += result.BlockingIssueCount > 0 ? result.BlockingIssueCount : 1;
                }

                if (result.HasNonBlockingIssues)
                {
                    nonBlockingIssues += result.NonBlockingIssueCount > 0 ? result.NonBlockingIssueCount : 1;
                }

                if (result.Failed)
                {
                    failed++;
                }

                if (result.Skipped)
                {
                    skipped++;
                }

                if (result.Succeeded)
                {
                    succeeded++;
                }
            }

            if (blockingIssues > 0)
            {
                return ActivityContentExecutionAggregateStatus.FailedBlocking;
            }

            if (failed > 0)
            {
                return ActivityContentExecutionAggregateStatus.FailedNonBlocking;
            }

            if (nonBlockingIssues > 0)
            {
                return ActivityContentExecutionAggregateStatus.SucceededWithNonBlockingIssues;
            }

            if (succeeded == 0 && skipped == candidateResults.Count)
            {
                return ActivityContentExecutionAggregateStatus.SkippedNoContent;
            }

            if (succeeded == 0)
            {
                return ActivityContentExecutionAggregateStatus.SucceededNoOp;
            }

            return ActivityContentExecutionAggregateStatus.Succeeded;
        }

        private static string BuildDefaultMessage(ActivityContentExecutionAggregateStatus status)
        {
            switch (status)
            {
                case ActivityContentExecutionAggregateStatus.Succeeded:
                    return "Activity content participant execution aggregate succeeded.";
                case ActivityContentExecutionAggregateStatus.SucceededNoOp:
                    return "Activity content participant execution aggregate completed with no operation.";
                case ActivityContentExecutionAggregateStatus.SucceededWithNonBlockingIssues:
                    return "Activity content participant execution aggregate succeeded with non-blocking issues.";
                case ActivityContentExecutionAggregateStatus.SkippedNoContent:
                    return "Activity content participant execution aggregate skipped because no participant results were provided.";
                case ActivityContentExecutionAggregateStatus.FailedNonBlocking:
                    return "Activity content participant execution aggregate failed without blocking readiness.";
                case ActivityContentExecutionAggregateStatus.FailedBlocking:
                    return "Activity content participant execution aggregate failed and blocks readiness.";
                case ActivityContentExecutionAggregateStatus.RejectedInvalidResults:
                    return "Activity content participant execution aggregate rejected invalid results.";
                default:
                    return "Activity content participant execution aggregate completed.";
            }
        }

        private int CountByRequiredness(ActivityContentExecutionRequiredness requiredness)
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < _results.Length; i++)
            {
                if (_results[i].Requiredness == requiredness)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountSucceeded()
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < _results.Length; i++)
            {
                if (_results[i].Succeeded)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountSkipped()
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < _results.Length; i++)
            {
                if (_results[i].Skipped)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountFailed()
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < _results.Length; i++)
            {
                if (_results[i].Failed)
                {
                    count++;
                }
            }

            return count;
        }

        private int SumBlockingIssues()
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < _results.Length; i++)
            {
                count += _results[i].BlockingIssueCount;
            }

            return count;
        }

        private int SumNonBlockingIssues()
        {
            if (_results == null || _results.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < _results.Length; i++)
            {
                count += _results[i].NonBlockingIssueCount;
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive lifecycle-level diagnostic result for Activity Content Execution.
    /// It groups enter/exit plans and aggregate results for ActivityFlow integration only; it does not discover participants, mutate Unity objects, materialize content or execute gameplay systems by itself.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10I Activity Content Execution lifecycle integration result; diagnostics only and empty by default until participant discovery exists.")]
    public readonly struct ActivityContentExecutionLifecycleResult : IEquatable<ActivityContentExecutionLifecycleResult>
    {
        public ActivityContentExecutionLifecycleResult(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            ActivityContentExecutionParticipantCollection participants,
            ActivityContentExecutionPhasePlan enterPlan,
            ActivityContentExecutionAggregateResult enterResult,
            ActivityContentExecutionPhasePlan exitPlan,
            ActivityContentExecutionAggregateResult exitResult,
            ActivityContentExecutionLifecycleStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionLifecycleStatus), status)
                || status == ActivityContentExecutionLifecycleStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution lifecycle status must be explicit.");
            }

            Activity = activity;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Participants = participants;
            EnterPlan = enterPlan;
            EnterResult = enterResult;
            ExitPlan = exitPlan;
            ExitResult = exitResult;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset NextActivity { get; }

        public ActivityContentExecutionParticipantCollection Participants { get; }

        public ActivityContentExecutionPhasePlan EnterPlan { get; }

        public ActivityContentExecutionAggregateResult EnterResult { get; }

        public ActivityContentExecutionPhasePlan ExitPlan { get; }

        public ActivityContentExecutionAggregateResult ExitResult { get; }

        public ActivityContentExecutionLifecycleStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Executed => Status != ActivityContentExecutionLifecycleStatus.None
            && Status != ActivityContentExecutionLifecycleStatus.Unknown;

        public bool Succeeded => Status == ActivityContentExecutionLifecycleStatus.Succeeded
            || Status == ActivityContentExecutionLifecycleStatus.SucceededNoContent
            || Status == ActivityContentExecutionLifecycleStatus.SucceededWithNonBlockingIssues;

        public bool Failed => Status == ActivityContentExecutionLifecycleStatus.FailedBlocking
            || Status == ActivityContentExecutionLifecycleStatus.FailedNonBlocking
            || Status == ActivityContentExecutionLifecycleStatus.RejectedInvalidContext;

        public bool HasEnterPlan => EnterPlan.Status != ActivityContentExecutionPhasePlanStatus.Unknown;

        public bool HasExitPlan => ExitPlan.Status != ActivityContentExecutionPhasePlanStatus.Unknown;

        public bool HasEnterResult => EnterResult.Status != ActivityContentExecutionAggregateStatus.Unknown;

        public bool HasExitResult => ExitResult.Status != ActivityContentExecutionAggregateStatus.Unknown;

        public bool HasParticipants => Participants.HasParticipants;

        public bool HasParticipantIssues => Participants.HasIssues;

        public bool HasIssues => BlockingIssueCount > 0 || NonBlockingIssueCount > 0 || HasParticipantIssues;

        public bool BlocksReadiness => EnterBlocksReadiness || ExitBlocksReadiness || Status == ActivityContentExecutionLifecycleStatus.FailedBlocking;

        public bool EnterBlocksReadiness => HasEnterResult && EnterResult.BlocksReadiness;

        public bool ExitBlocksReadiness => HasExitResult && ExitResult.BlocksReadiness;

        public int ParticipantCount => Participants.Count;

        public int ParticipantIssueCount => Participants.IssueCount;

        public int EnterRequestCount => HasEnterPlan ? EnterPlan.RequestCount : 0;

        public int ExitRequestCount => HasExitPlan ? ExitPlan.RequestCount : 0;

        public int EnterResultCount => HasEnterResult ? EnterResult.ResultCount : 0;

        public int ExitResultCount => HasExitResult ? ExitResult.ResultCount : 0;

        public int RequiredCount => (HasEnterResult ? EnterResult.RequiredCount : 0) + (HasExitResult ? ExitResult.RequiredCount : 0);

        public int OptionalCount => (HasEnterResult ? EnterResult.OptionalCount : 0) + (HasExitResult ? ExitResult.OptionalCount : 0);

        public int BlockingIssueCount => (HasEnterResult ? EnterResult.BlockingIssueCount : 0) + (HasExitResult ? ExitResult.BlockingIssueCount : 0);

        public int NonBlockingIssueCount => (HasEnterResult ? EnterResult.NonBlockingIssueCount : 0) + (HasExitResult ? ExitResult.NonBlockingIssueCount : 0);

        public string DiagnosticStatus => Status.ToString();

        public string ActivityName => Activity != null ? Activity.ActivityName : string.Empty;

        public string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        public string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public bool Equals(ActivityContentExecutionLifecycleResult other)
        {
            return ReferenceEquals(Activity, other.Activity)
                && ReferenceEquals(PreviousActivity, other.PreviousActivity)
                && ReferenceEquals(NextActivity, other.NextActivity)
                && Participants.Count == other.Participants.Count
                && Participants.IssueCount == other.Participants.IssueCount
                && EnterPlan.Equals(other.EnterPlan)
                && EnterResult.Equals(other.EnterResult)
                && ExitPlan.Equals(other.ExitPlan)
                && ExitResult.Equals(other.ExitResult)
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionLifecycleResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Activity != null ? Activity.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (PreviousActivity != null ? PreviousActivity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NextActivity != null ? NextActivity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Participants.Count;
                hashCode = (hashCode * 397) ^ Participants.IssueCount;
                hashCode = (hashCode * 397) ^ EnterPlan.GetHashCode();
                hashCode = (hashCode * 397) ^ EnterResult.GetHashCode();
                hashCode = (hashCode * 397) ^ ExitPlan.GetHashCode();
                hashCode = (hashCode * 397) ^ ExitResult.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
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
            builder.Append($"Activity Content Execution Lifecycle status='{Status}' activity='{activityText}' previousActivity='{previousText}' nextActivity='{nextText}' participants='{ParticipantCount}' participantIssues='{ParticipantIssueCount}' enterPlan='{EnterPlan.Status}' enterRequests='{EnterRequestCount}' enterStatus='{EnterResult.Status}' enterResults='{EnterResultCount}' exitPlan='{ExitPlan.Status}' exitRequests='{ExitRequestCount}' exitStatus='{ExitResult.Status}' exitResults='{ExitResultCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' blocksReadiness='{BlocksReadiness}' source='{sourceText}' reason='{reasonText}' message='{messageText}'");
            return builder.ToString();
        }

        public static ActivityContentExecutionLifecycleResult None(string source, string reason, string message)
        {
            return new ActivityContentExecutionLifecycleResult(
                null,
                null,
                null,
                ActivityContentExecutionParticipantCollection.Empty(),
                default,
                default,
                default,
                default,
                ActivityContentExecutionLifecycleStatus.None,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionLifecycleResult FromResults(
            ActivityAsset activity,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            ActivityContentExecutionParticipantCollection participants,
            ActivityContentExecutionPhasePlan enterPlan,
            ActivityContentExecutionAggregateResult enterResult,
            ActivityContentExecutionPhasePlan exitPlan,
            ActivityContentExecutionAggregateResult exitResult,
            string source,
            string reason,
            string message)
        {
            var status = ResolveStatus(participants, enterResult, exitResult);
            return new ActivityContentExecutionLifecycleResult(
                activity,
                previousActivity,
                nextActivity,
                participants,
                enterPlan,
                enterResult,
                exitPlan,
                exitResult,
                status,
                source,
                reason,
                message);
        }

        private static ActivityContentExecutionLifecycleStatus ResolveStatus(
            ActivityContentExecutionParticipantCollection participants,
            ActivityContentExecutionAggregateResult enterResult,
            ActivityContentExecutionAggregateResult exitResult)
        {
            var hasEnter = enterResult.Status != ActivityContentExecutionAggregateStatus.Unknown;
            var hasExit = exitResult.Status != ActivityContentExecutionAggregateStatus.Unknown;
            if (!hasEnter && !hasExit)
            {
                return ActivityContentExecutionLifecycleStatus.None;
            }

            if ((hasEnter && enterResult.Status == ActivityContentExecutionAggregateStatus.RejectedInvalidResults)
                || (hasExit && exitResult.Status == ActivityContentExecutionAggregateStatus.RejectedInvalidResults))
            {
                return ActivityContentExecutionLifecycleStatus.RejectedInvalidContext;
            }

            if ((hasEnter && enterResult.BlocksReadiness) || (hasExit && exitResult.BlocksReadiness))
            {
                return ActivityContentExecutionLifecycleStatus.FailedBlocking;
            }

            if ((hasEnter && enterResult.Failed) || (hasExit && exitResult.Failed))
            {
                return ActivityContentExecutionLifecycleStatus.FailedNonBlocking;
            }

            if ((hasEnter && enterResult.HasNonBlockingIssues) || (hasExit && exitResult.HasNonBlockingIssues) || participants.HasIssues)
            {
                return ActivityContentExecutionLifecycleStatus.SucceededWithNonBlockingIssues;
            }

            var noEnterContent = !hasEnter || enterResult.Skipped;
            var noExitContent = !hasExit || exitResult.Skipped;
            if (!participants.HasParticipants && noEnterContent && noExitContent)
            {
                return ActivityContentExecutionLifecycleStatus.SucceededNoContent;
            }

            return ActivityContentExecutionLifecycleStatus.Succeeded;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

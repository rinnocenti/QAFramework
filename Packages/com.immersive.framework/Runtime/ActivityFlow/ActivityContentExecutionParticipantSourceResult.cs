using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive diagnostic result produced by an Activity Content Execution participant source.
    /// It carries a participant collection and source-level diagnostics only; it does not execute participants.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10K Activity Content Execution participant source result; source diagnostics only.")]
    public readonly struct ActivityContentExecutionParticipantSourceResult : IEquatable<ActivityContentExecutionParticipantSourceResult>
    {
        public ActivityContentExecutionParticipantSourceResult(
            ActivityContentExecutionParticipantSourceRequest request,
            ActivityContentExecutionParticipantCollection collection,
            ActivityContentExecutionParticipantSourceStatus status,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionParticipantSourceStatus), status)
                || status == ActivityContentExecutionParticipantSourceStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Activity content execution participant source status must be explicit.");
            }

            Request = request;
            Collection = collection;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
        }

        public ActivityContentExecutionParticipantSourceRequest Request { get; }

        public ActivityContentExecutionParticipantCollection Collection { get; }

        public ActivityContentExecutionParticipantSourceStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Executed => Status != ActivityContentExecutionParticipantSourceStatus.None
            && Status != ActivityContentExecutionParticipantSourceStatus.Unknown;

        public bool Succeeded => Status == ActivityContentExecutionParticipantSourceStatus.Succeeded
            || Status == ActivityContentExecutionParticipantSourceStatus.SucceededNoParticipants
            || Status == ActivityContentExecutionParticipantSourceStatus.SucceededWithIssues;

        public bool Failed => Status == ActivityContentExecutionParticipantSourceStatus.Failed
            || Status == ActivityContentExecutionParticipantSourceStatus.FailedException
            || Status == ActivityContentExecutionParticipantSourceStatus.RejectedInvalidRequest;

        public bool HasParticipants => Collection.HasParticipants;

        public bool HasIssues => Collection.HasIssues || Failed;

        public int ParticipantCount => Collection.Count;

        public int IssueCount => Collection.IssueCount + (Failed ? 1 : 0);

        public string DiagnosticStatus => Status.ToString();

        public bool Equals(ActivityContentExecutionParticipantSourceResult other)
        {
            return Request.Equals(other.Request)
                && Collection.Count == other.Collection.Count
                && Collection.IssueCount == other.Collection.IssueCount
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionParticipantSourceResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Request.GetHashCode();
                hashCode = (hashCode * 397) ^ Collection.Count;
                hashCode = (hashCode * 397) ^ Collection.IssueCount;
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
            var sourceText = !string.IsNullOrWhiteSpace(Source) ? Source : "<none>";
            var reasonText = !string.IsNullOrWhiteSpace(Reason) ? Reason : "<none>";
            var messageText = !string.IsNullOrWhiteSpace(Message) ? Message : "<none>";
            return $"status='{Status}' participants='{ParticipantCount}' issues='{IssueCount}' request=({Request.ToDiagnosticString()}) source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static ActivityContentExecutionParticipantSourceResult None(string source, string reason, string message)
        {
            return new ActivityContentExecutionParticipantSourceResult(
                default,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.None,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionParticipantSourceResult FromCollection(
            ActivityContentExecutionParticipantSourceRequest request,
            ActivityContentExecutionParticipantCollection collection,
            string source,
            string reason,
            string message)
        {
            var status = ResolveSuccessStatus(collection);
            return new ActivityContentExecutionParticipantSourceResult(request, collection, status, source, reason, message);
        }

        public static ActivityContentExecutionParticipantSourceResult SucceededNoParticipants(
            ActivityContentExecutionParticipantSourceRequest request,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionParticipantSourceResult(
                request,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.SucceededNoParticipants,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionParticipantSourceResult RejectedInvalidRequest(
            ActivityContentExecutionParticipantSourceRequest request,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionParticipantSourceResult(
                request,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.RejectedInvalidRequest,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionParticipantSourceResult FailedResult(
            ActivityContentExecutionParticipantSourceRequest request,
            string source,
            string reason,
            string message)
        {
            return new ActivityContentExecutionParticipantSourceResult(
                request,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.Failed,
                source,
                reason,
                message);
        }

        public static ActivityContentExecutionParticipantSourceResult FailedException(
            ActivityContentExecutionParticipantSourceRequest request,
            Exception exception,
            string source,
            string reason)
        {
            var message = exception == null
                ? "Activity content execution participant source failed with an unknown exception."
                : $"Activity content execution participant source threw '{exception.GetType().Name}': {exception.Message}";

            return new ActivityContentExecutionParticipantSourceResult(
                request,
                ActivityContentExecutionParticipantCollection.Empty(),
                ActivityContentExecutionParticipantSourceStatus.FailedException,
                source,
                reason,
                message);
        }

        private static ActivityContentExecutionParticipantSourceStatus ResolveSuccessStatus(ActivityContentExecutionParticipantCollection collection)
        {
            if (collection.HasParticipants && collection.HasIssues)
            {
                return ActivityContentExecutionParticipantSourceStatus.SucceededWithIssues;
            }

            return collection.HasParticipants
                ? ActivityContentExecutionParticipantSourceStatus.Succeeded
                : ActivityContentExecutionParticipantSourceStatus.SucceededNoParticipants;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

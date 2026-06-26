using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Result of preparing a Pause Content Anchor consumer contract.
    /// Prepared results carry a canonical ContentAnchorBindingRequest for a future materialization/binding adapter.
    /// The result itself does not show UI, create prefabs, bind input, change Pause state or execute Content Anchor binding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23B Pause Content Anchor consumer result; contract preparation only.")]
    public readonly struct PauseContentAnchorConsumerResult : IEquatable<PauseContentAnchorConsumerResult>
    {
        private readonly string[] issues;

        public PauseContentAnchorConsumerResult(
            PauseContentAnchorRequest request,
            PauseContentAnchorConsumerStatus status,
            ContentAnchorDeclaration anchor,
            ContentAnchorBindingRequest bindingRequest,
            string consumerName,
            string message,
            IReadOnlyList<string> issues)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor consumer result requires a valid request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(PauseContentAnchorConsumerStatus), status) || status == PauseContentAnchorConsumerStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Pause Content Anchor consumer status must be explicit.");
            }

            if (status == PauseContentAnchorConsumerStatus.Prepared)
            {
                if (!anchor.IsValid)
                {
                    throw new ArgumentException("Prepared Pause Content Anchor consumer result requires a valid anchor.", nameof(anchor));
                }

                if (!bindingRequest.IsValid)
                {
                    throw new ArgumentException("Prepared Pause Content Anchor consumer result requires a valid Content Anchor binding request.", nameof(bindingRequest));
                }

                if (!request.Matches(anchor) || !bindingRequest.Matches(anchor))
                {
                    throw new ArgumentException("Prepared Pause Content Anchor consumer result anchor must match request and binding request.", nameof(anchor));
                }
            }

            Request = request;
            Status = status;
            Anchor = anchor;
            BindingRequest = bindingRequest;
            ConsumerName = Normalize(consumerName);
            Message = Normalize(message);
            this.issues = CopyIssues(issues);
        }

        public PauseContentAnchorRequest Request { get; }

        public PauseContentAnchorRequestId RequestId => Request.RequestId;

        public PauseContentAnchorPurpose Purpose => Request.Purpose;

        public PauseState PauseState => Request.PauseState;

        public PauseContentAnchorConsumerStatus Status { get; }

        public ContentAnchorDeclaration Anchor { get; }

        public ContentAnchorBindingRequest BindingRequest { get; }

        public string ConsumerName { get; }

        public string Message { get; }

        public IReadOnlyList<string> Issues => issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public bool HasIssues => IssueCount > 0;

        public bool HasAnchor => Anchor.IsValid;

        public bool HasBindingRequest => BindingRequest.IsValid;

        public bool Prepared => Status == PauseContentAnchorConsumerStatus.Prepared;

        public bool SkippedOptionalMissing => Status == PauseContentAnchorConsumerStatus.SkippedOptionalMissing;

        public bool Rejected => Status == PauseContentAnchorConsumerStatus.RejectedMissingRequired
            || Status == PauseContentAnchorConsumerStatus.RejectedMismatchedAnchor;

        public bool Failed => Status == PauseContentAnchorConsumerStatus.Failed;

        public bool Completed => Prepared || SkippedOptionalMissing;

        public bool BlocksPauseContent => Rejected || Failed;

        public bool IsValid => Request.IsValid
            && Status != PauseContentAnchorConsumerStatus.Unknown
            && (!Prepared || (HasAnchor && HasBindingRequest));

        public bool Equals(PauseContentAnchorConsumerResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && Anchor.Equals(other.Anchor)
                && BindingRequest.Equals(other.BindingRequest)
                && string.Equals(ConsumerName, other.ConsumerName, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseContentAnchorConsumerResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Request.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ Anchor.GetHashCode();
                hashCode = (hashCode * 397) ^ BindingRequest.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ConsumerName ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                for (var i = 0; i < Issues.Count; i++)
                {
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Issues[i] ?? string.Empty);
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
            var consumerText = string.IsNullOrWhiteSpace(ConsumerName) ? "<none>" : ConsumerName;
            var messageText = string.IsNullOrWhiteSpace(Message) ? "<none>" : Message;
            var anchorText = HasAnchor ? Anchor.StableText : "<none>";
            var bindingText = HasBindingRequest ? BindingRequest.AnchorStableText : "<none>";
            var builder = new StringBuilder();
            builder.Append($"consumer='{consumerText}' request='{RequestId.StableText}' purpose='{Purpose}' pauseState='{PauseState}' status='{Status}' completed='{Completed}' blocksPauseContent='{BlocksPauseContent}' hasAnchor='{HasAnchor}' hasBindingRequest='{HasBindingRequest}' anchor='{anchorText}' binding='{bindingText}' issues='{IssueCount}' message='{messageText}'");
            if (HasIssues)
            {
                builder.Append(" issues=[");
                for (var i = 0; i < Issues.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(Issues[i]);
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static PauseContentAnchorConsumerResult PreparedResult(
            PauseContentAnchorRequest request,
            ContentAnchorDeclaration anchor,
            string consumerName,
            string message)
        {
            return new PauseContentAnchorConsumerResult(
                request,
                PauseContentAnchorConsumerStatus.Prepared,
                anchor,
                request.ToBindingRequest(),
                consumerName,
                message,
                Array.Empty<string>());
        }

        public static PauseContentAnchorConsumerResult SkippedOptionalMissingResult(
            PauseContentAnchorRequest request,
            string consumerName,
            string message)
        {
            return new PauseContentAnchorConsumerResult(
                request,
                PauseContentAnchorConsumerStatus.SkippedOptionalMissing,
                default,
                default,
                consumerName,
                message,
                Array.Empty<string>());
        }

        public static PauseContentAnchorConsumerResult RejectedMissingRequiredResult(
            PauseContentAnchorRequest request,
            string consumerName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new PauseContentAnchorConsumerResult(
                request,
                PauseContentAnchorConsumerStatus.RejectedMissingRequired,
                default,
                default,
                consumerName,
                message,
                issues);
        }

        public static PauseContentAnchorConsumerResult RejectedMismatchedAnchorResult(
            PauseContentAnchorRequest request,
            ContentAnchorDeclaration anchor,
            string consumerName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new PauseContentAnchorConsumerResult(
                request,
                PauseContentAnchorConsumerStatus.RejectedMismatchedAnchor,
                anchor,
                default,
                consumerName,
                message,
                issues);
        }

        public static PauseContentAnchorConsumerResult FailedResult(
            PauseContentAnchorRequest request,
            string consumerName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new PauseContentAnchorConsumerResult(
                request,
                PauseContentAnchorConsumerStatus.Failed,
                default,
                default,
                consumerName,
                message,
                issues);
        }

        public static bool operator ==(PauseContentAnchorConsumerResult left, PauseContentAnchorConsumerResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseContentAnchorConsumerResult left, PauseContentAnchorConsumerResult right)
        {
            return !left.Equals(right);
        }

        private static string[] CopyIssues(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = Normalize(source[i]);
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

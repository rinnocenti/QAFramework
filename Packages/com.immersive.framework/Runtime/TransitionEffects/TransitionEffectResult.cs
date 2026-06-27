using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Passive result for one Transition Effect request.
    /// The result records adapter-facing diagnostics and does not execute, release or fallback any visual effect.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B passive Transition Effect result; diagnostics only.")]
    public readonly struct TransitionEffectResult : IEquatable<TransitionEffectResult>
    {
        private readonly string[] issues;

        public TransitionEffectResult(
            TransitionEffectRequest request,
            TransitionEffectStatus status,
            string message,
            IReadOnlyList<string> issues)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Transition Effect result requires a valid request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(TransitionEffectStatus), status) || status == TransitionEffectStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Transition Effect result status must be explicit.");
            }

            Request = request;
            Status = status;
            Message = Normalize(message);
            this.issues = CopyIssues(issues);
        }

        public TransitionEffectRequest Request { get; }

        public TransitionEffectId EffectId => Request.EffectId;

        public TransitionEffectKind EffectKind => Request.EffectKind;

        public TransitionEffectRequiredness Requiredness => Request.Requiredness;

        public TransitionOperationId OperationId => Request.OperationId;

        public TransitionKind TransitionKind => Request.TransitionKind;

        public TransitionPhase Phase => Request.Phase;

        public TransitionEffectStatus Status { get; }

        public string Message { get; }

        public IReadOnlyList<string> Issues => issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public bool HasIssues => IssueCount > 0;

        public bool IsRequired => Request.IsRequired;

        public bool IsOptional => Request.IsOptional;

        public bool Succeeded => Status == TransitionEffectStatus.Succeeded;

        public bool CompletedWithWarnings => Status == TransitionEffectStatus.CompletedWithWarnings;

        public bool Skipped => Status == TransitionEffectStatus.Skipped;

        public bool Failed => Status == TransitionEffectStatus.Failed;

        public bool Rejected => Status == TransitionEffectStatus.Rejected;

        public bool MissingAdapter => Status == TransitionEffectStatus.MissingAdapter;

        public bool Completed => Succeeded || CompletedWithWarnings || Skipped;

        public bool BlocksTransition => IsRequired && (Failed || Rejected || MissingAdapter);

        public bool IsValid => Request.IsValid && Status != TransitionEffectStatus.Unknown;

        public bool Equals(TransitionEffectResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Request.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                var issueItems = Issues;
                for (var i = 0; i < issueItems.Count; i++)
                {
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(issueItems[i] ?? string.Empty);
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
            var builder = new StringBuilder();
            var messageText = string.IsNullOrWhiteSpace(Message) ? "<none>" : Message;
            builder.Append($"effect='{EffectId.StableText}' effectKind='{EffectKind}' requiredness='{Requiredness}' operation='{OperationId.StableText}' transitionKind='{TransitionKind}' phase='{Phase}' status='{Status}' completed='{Completed}' blocksTransition='{BlocksTransition}' issues='{IssueCount}' message='{messageText}'");

            if (HasIssues)
            {
                builder.Append(" issues=[");
                var issueItems = Issues;
                for (var i = 0; i < issueItems.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(issueItems[i]);
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static TransitionEffectResult SucceededResult(TransitionEffectRequest request, string message)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.Succeeded, message, Array.Empty<string>());
        }

        public static TransitionEffectResult CompletedWithWarningsResult(
            TransitionEffectRequest request,
            string message,
            IReadOnlyList<string> issues)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.CompletedWithWarnings, message, issues);
        }

        public static TransitionEffectResult SkippedResult(TransitionEffectRequest request, string message)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.Skipped, message, Array.Empty<string>());
        }

        public static TransitionEffectResult FailedResult(
            TransitionEffectRequest request,
            string message,
            IReadOnlyList<string> issues)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.Failed, message, issues);
        }

        public static TransitionEffectResult RejectedResult(
            TransitionEffectRequest request,
            string message,
            IReadOnlyList<string> issues)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.Rejected, message, issues);
        }

        public static TransitionEffectResult MissingAdapterResult(
            TransitionEffectRequest request,
            string message,
            IReadOnlyList<string> issues)
        {
            return new TransitionEffectResult(request, TransitionEffectStatus.MissingAdapter, message, issues);
        }

        public static bool operator ==(TransitionEffectResult left, TransitionEffectResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionEffectResult left, TransitionEffectResult right)
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

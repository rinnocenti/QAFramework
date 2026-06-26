using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Result of resolving one normalized Pause input signal.
    /// It may carry a PauseRequest for state-changing commands, but it does not execute the request or mutate Pause state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23D Pause Input resolution result; no Pause execution.")]
    public readonly struct PauseInputResolutionResult : IEquatable<PauseInputResolutionResult>
    {
        private readonly string[] issues;

        public PauseInputResolutionResult(
            PauseInputSignal signal,
            PauseInputResolutionStatus status,
            PauseRequest pauseRequest,
            string resolverName,
            string message,
            IReadOnlyList<string> issues)
        {
            if (!signal.IsValid)
            {
                throw new ArgumentException("Pause input resolution requires a valid signal.", nameof(signal));
            }

            if (!Enum.IsDefined(typeof(PauseInputResolutionStatus), status) || status == PauseInputResolutionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Pause input resolution status must be explicit.");
            }

            if (status == PauseInputResolutionStatus.ResolvedPauseRequest && !pauseRequest.IsValid)
            {
                throw new ArgumentException("Resolved Pause request input result requires a valid PauseRequest.", nameof(pauseRequest));
            }

            Signal = signal;
            Status = status;
            PauseRequest = pauseRequest;
            ResolverName = Normalize(resolverName);
            Message = Normalize(message);
            this.issues = CopyIssues(issues);
        }

        public PauseInputSignal Signal { get; }

        public PauseInputActionId ActionId => Signal.ActionId;

        public PauseInputCommandKind CommandKind => Signal.CommandKind;

        public PauseInputSourceKind SourceKind => Signal.SourceKind;

        public PauseInputResolutionStatus Status { get; }

        public PauseRequest PauseRequest { get; }

        public string ResolverName { get; }

        public string Message { get; }

        public IReadOnlyList<string> Issues => issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public bool HasIssues => IssueCount > 0;

        public bool HasPauseRequest => PauseRequest.IsValid;

        public bool ResolvedPauseRequest => Status == PauseInputResolutionStatus.ResolvedPauseRequest;

        public bool ResolvedMenuCommand => Status == PauseInputResolutionStatus.ResolvedMenuCommand;

        public bool Ignored => Status == PauseInputResolutionStatus.Ignored;

        public bool Rejected => Status == PauseInputResolutionStatus.RejectedUnsupported;

        public bool Failed => Status == PauseInputResolutionStatus.Failed;

        public bool Completed => ResolvedPauseRequest || ResolvedMenuCommand || Ignored;

        public bool IsValid => Signal.IsValid
            && Status != PauseInputResolutionStatus.Unknown
            && (!ResolvedPauseRequest || HasPauseRequest);

        public bool Equals(PauseInputResolutionResult other)
        {
            return Signal.Equals(other.Signal)
                && Status == other.Status
                && PauseRequest.Equals(other.PauseRequest)
                && string.Equals(ResolverName, other.ResolverName, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseInputResolutionResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Signal.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ PauseRequest.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(ResolverName ?? string.Empty);
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
            var resolverText = string.IsNullOrWhiteSpace(ResolverName) ? "<none>" : ResolverName;
            var messageText = string.IsNullOrWhiteSpace(Message) ? "<none>" : Message;
            var requestText = HasPauseRequest ? PauseRequest.RequestId.StableText : "<none>";
            var builder = new StringBuilder();
            builder.Append($"resolver='{resolverText}' status='{Status}' completed='{Completed}' hasPauseRequest='{HasPauseRequest}' pauseRequest='{requestText}' command='{CommandKind}' sourceKind='{SourceKind}' issues='{IssueCount}' message='{messageText}' signal=({Signal.ToDiagnosticString()})");
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

        public static PauseInputResolutionResult ResolvedPauseRequestResult(
            PauseInputSignal signal,
            PauseRequest pauseRequest,
            string resolverName,
            string message)
        {
            return new PauseInputResolutionResult(signal, PauseInputResolutionStatus.ResolvedPauseRequest, pauseRequest, resolverName, message, Array.Empty<string>());
        }

        public static PauseInputResolutionResult ResolvedMenuCommandResult(
            PauseInputSignal signal,
            string resolverName,
            string message)
        {
            return new PauseInputResolutionResult(signal, PauseInputResolutionStatus.ResolvedMenuCommand, default, resolverName, message, Array.Empty<string>());
        }

        public static PauseInputResolutionResult IgnoredResult(PauseInputSignal signal, string resolverName, string message)
        {
            return new PauseInputResolutionResult(signal, PauseInputResolutionStatus.Ignored, default, resolverName, message, Array.Empty<string>());
        }

        public static PauseInputResolutionResult RejectedUnsupportedResult(
            PauseInputSignal signal,
            string resolverName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new PauseInputResolutionResult(signal, PauseInputResolutionStatus.RejectedUnsupported, default, resolverName, message, issues);
        }

        public static PauseInputResolutionResult FailedResult(
            PauseInputSignal signal,
            string resolverName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new PauseInputResolutionResult(signal, PauseInputResolutionStatus.Failed, default, resolverName, message, issues);
        }

        public static bool operator ==(PauseInputResolutionResult left, PauseInputResolutionResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseInputResolutionResult left, PauseInputResolutionResult right)
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

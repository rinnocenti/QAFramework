using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive result for one logical Pause request.
    /// The result records state movement and issues; it does not change input, UI, Gate or Time.timeScale.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20B passive Pause result primitive; diagnostics only.")]
    public readonly struct PauseResult : IEquatable<PauseResult>
    {
        private readonly PauseIssue[] issues;

        public PauseResult(
            PauseRequest request,
            PauseState previousState,
            PauseState currentState,
            PauseRequestStatus status,
            string message,
            IReadOnlyList<PauseIssue> issues)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Pause result requires a valid request.", nameof(request));
            }

            ValidateState(previousState, nameof(previousState));
            ValidateState(currentState, nameof(currentState));

            if (!Enum.IsDefined(typeof(PauseRequestStatus), status) || status == PauseRequestStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Pause result status must be explicit.");
            }

            Request = request;
            PreviousState = previousState;
            CurrentState = currentState;
            Status = status;
            Message = Normalize(message);
            this.issues = CopyIssues(issues);
        }

        public PauseRequest Request { get; }

        public PauseRequestId RequestId => Request.RequestId;

        public PauseRequestKind Kind => Request.Kind;

        public PauseState PreviousState { get; }

        public PauseState CurrentState { get; }

        public PauseRequestStatus Status { get; }

        public string Message { get; }

        public IReadOnlyList<PauseIssue> Issues => issues ?? Array.Empty<PauseIssue>();

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount
        {
            get
            {
                var count = 0;
                var items = Issues;
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i].BlocksRequest)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasIssues => IssueCount > 0;

        public bool HasBlockingIssues => BlockingIssueCount > 0;

        public bool Applied => Status == PauseRequestStatus.Applied;

        public bool Rejected => Status == PauseRequestStatus.Rejected;

        public bool IgnoredNoChange => Status == PauseRequestStatus.IgnoredNoChange;

        public bool Failed => Status == PauseRequestStatus.Failed;

        public bool Completed => Applied || IgnoredNoChange;

        public bool StateChanged => PreviousState != CurrentState;

        public bool IsPaused => CurrentState == PauseState.Paused;

        public bool IsRunning => CurrentState == PauseState.Running;

        public bool IsValid => Request.IsValid
            && PreviousState != PauseState.Unknown
            && CurrentState != PauseState.Unknown
            && Status != PauseRequestStatus.Unknown;

        public bool Equals(PauseResult other)
        {
            return Request.Equals(other.Request)
                && PreviousState == other.PreviousState
                && CurrentState == other.CurrentState
                && Status == other.Status
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Request.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)PreviousState;
                hashCode = (hashCode * 397) ^ (int)CurrentState;
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);

                var issueItems = Issues;
                for (var i = 0; i < issueItems.Count; i++)
                {
                    hashCode = (hashCode * 397) ^ issueItems[i].GetHashCode();
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
            builder.Append($"request='{RequestId.StableText}' kind='{Kind}' previousState='{PreviousState}' currentState='{CurrentState}' status='{Status}' completed='{Completed}' stateChanged='{StateChanged}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' message='{messageText}'");

            if (HasIssues)
            {
                builder.Append(" issues=[");
                var items = Issues;
                for (var i = 0; i < items.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(items[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static PauseResult AppliedResult(PauseRequest request, PauseState previousState, PauseState currentState, string message)
        {
            return new PauseResult(request, previousState, currentState, PauseRequestStatus.Applied, message, Array.Empty<PauseIssue>());
        }

        public static PauseResult IgnoredNoChangeResult(PauseRequest request, PauseState state, string message)
        {
            return new PauseResult(request, state, state, PauseRequestStatus.IgnoredNoChange, message, Array.Empty<PauseIssue>());
        }

        public static PauseResult RejectedResult(PauseRequest request, PauseState state, string message, IReadOnlyList<PauseIssue> issues)
        {
            return new PauseResult(request, state, state, PauseRequestStatus.Rejected, message, issues);
        }

        public static PauseResult FailedResult(PauseRequest request, PauseState state, string message, IReadOnlyList<PauseIssue> issues)
        {
            return new PauseResult(request, state, state, PauseRequestStatus.Failed, message, issues);
        }

        public static bool operator ==(PauseResult left, PauseResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseResult left, PauseResult right)
        {
            return !left.Equals(right);
        }

        private static PauseIssue[] CopyIssues(IReadOnlyList<PauseIssue> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<PauseIssue>();
            }

            var copy = new PauseIssue[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Pause result cannot contain invalid issues.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<PauseIssue> left, IReadOnlyList<PauseIssue> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!left[i].Equals(right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static void ValidateState(PauseState state, string parameterName)
        {
            if (!Enum.IsDefined(typeof(PauseState), state) || state == PauseState.Unknown)
            {
                throw new ArgumentOutOfRangeException(parameterName, state, "Pause state must be explicit.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

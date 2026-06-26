using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Explicit result for one Pause overlay adapter action.
    /// It reports visual adapter outcome only; it does not block or own Pause state, input, Time.timeScale, Transition Effects or lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23C Pause Overlay adapter result boundary; no lifecycle ownership.")]
    public readonly struct PauseOverlayAdapterResult : IEquatable<PauseOverlayAdapterResult>
    {
        private readonly string[] issues;

        public PauseOverlayAdapterResult(
            PauseOverlayPresentation presentation,
            PauseOverlayAdapterAction action,
            PauseOverlayAdapterStatus status,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            if (!presentation.IsValid)
            {
                throw new ArgumentException("Pause overlay adapter result requires a valid presentation.", nameof(presentation));
            }

            if (!Enum.IsDefined(typeof(PauseOverlayAdapterAction), action) || action == PauseOverlayAdapterAction.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(action), action, "Pause overlay adapter action must be explicit.");
            }

            if (!Enum.IsDefined(typeof(PauseOverlayAdapterStatus), status) || status == PauseOverlayAdapterStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Pause overlay adapter status must be explicit.");
            }

            Presentation = presentation;
            Action = action;
            Status = status;
            AdapterName = Normalize(adapterName);
            Message = Normalize(message);
            this.issues = CopyIssues(issues);
        }

        public PauseOverlayPresentation Presentation { get; }

        public PauseState State => Presentation.State;

        public PauseOverlayAdapterAction Action { get; }

        public PauseOverlayAdapterStatus Status { get; }

        public string AdapterName { get; }

        public string Message { get; }

        public IReadOnlyList<string> Issues => issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public bool HasIssues => IssueCount > 0;

        public bool Succeeded => Status == PauseOverlayAdapterStatus.Succeeded;

        public bool Skipped => Status == PauseOverlayAdapterStatus.Skipped;

        public bool Failed => Status == PauseOverlayAdapterStatus.Failed;

        public bool Rejected => Status == PauseOverlayAdapterStatus.Rejected;

        public bool Completed => Succeeded || Skipped;

        public bool IsValid => Presentation.IsValid
            && Action != PauseOverlayAdapterAction.Unknown
            && Status != PauseOverlayAdapterStatus.Unknown;

        public bool Equals(PauseOverlayAdapterResult other)
        {
            return Presentation.Equals(other.Presentation)
                && Action == other.Action
                && Status == other.Status
                && string.Equals(AdapterName, other.AdapterName, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseOverlayAdapterResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Presentation.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Action;
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(AdapterName ?? string.Empty);
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
            var adapterText = string.IsNullOrWhiteSpace(AdapterName) ? "<none>" : AdapterName;
            var messageText = string.IsNullOrWhiteSpace(Message) ? "<none>" : Message;
            var builder = new StringBuilder();
            builder.Append($"adapter='{adapterText}' state='{State}' action='{Action}' status='{Status}' completed='{Completed}' issues='{IssueCount}' message='{messageText}' presentation=({Presentation.ToDiagnosticString()})");
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

        public static PauseOverlayAdapterResult SucceededResult(
            PauseOverlayPresentation presentation,
            PauseOverlayAdapterAction action,
            string adapterName,
            string message)
        {
            return new PauseOverlayAdapterResult(presentation, action, PauseOverlayAdapterStatus.Succeeded, adapterName, message, Array.Empty<string>());
        }

        public static PauseOverlayAdapterResult SkippedResult(
            PauseOverlayPresentation presentation,
            PauseOverlayAdapterAction action,
            string adapterName,
            string message)
        {
            return new PauseOverlayAdapterResult(presentation, action, PauseOverlayAdapterStatus.Skipped, adapterName, message, Array.Empty<string>());
        }

        public static PauseOverlayAdapterResult FailedResult(
            PauseOverlayPresentation presentation,
            PauseOverlayAdapterAction action,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new PauseOverlayAdapterResult(presentation, action, PauseOverlayAdapterStatus.Failed, adapterName, message, issues);
        }

        public static PauseOverlayAdapterResult RejectedResult(
            PauseOverlayPresentation presentation,
            PauseOverlayAdapterAction action,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new PauseOverlayAdapterResult(presentation, action, PauseOverlayAdapterStatus.Rejected, adapterName, message, issues);
        }

        public static bool operator ==(PauseOverlayAdapterResult left, PauseOverlayAdapterResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseOverlayAdapterResult left, PauseOverlayAdapterResult right)
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

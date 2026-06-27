using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.TransitionEffects;

namespace Immersive.Framework.Transition
{
    /// <summary>
    /// API status: Experimental. Passive immutable result for a transition operation.
    /// The result aggregates observed steps and issues; it does not mutate lifecycle or release Gate blockers.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F18B passive Transition result primitive; diagnostics only.")]
    public readonly struct TransitionResult : IEquatable<TransitionResult>
    {
        private readonly TransitionStep[] observedSteps;
        private readonly string[] issues;
        private readonly TransitionEffectKind effectKind;
        private readonly TransitionEffectStatus effectStatus;
        private readonly int effectAdapterCount;
        private readonly string visualText;
        private readonly int effectBlockingIssueCount;

        public TransitionResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            TransitionStatus status,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition result requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), kind) || kind == TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Transition result kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionStatus), status) || status == TransitionStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Transition result status must be explicit.");
            }

            OperationId = operationId;
            Kind = kind;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);
            this.observedSteps = CopySteps(observedSteps);
            this.issues = CopyIssues(issues);
            this.effectKind = effectKind;
            this.effectStatus = effectStatus;
            this.effectAdapterCount = Math.Max(0, effectAdapterCount);
            this.visualText = NormalizeVisual(visualText);
            this.effectBlockingIssueCount = Math.Max(0, effectBlockingIssueCount);
        }

        public TransitionOperationId OperationId { get; }

        public TransitionKind Kind { get; }

        public TransitionStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public IReadOnlyList<TransitionStep> ObservedSteps => observedSteps ?? Array.Empty<TransitionStep>();

        public IReadOnlyList<string> Issues => issues ?? Array.Empty<string>();

        public int ObservedStepCount => ObservedSteps.Count;

        public int IssueCount => Issues.Count;

        public TransitionEffectKind EffectKind => effectKind;

        public TransitionEffectStatus EffectStatus => effectStatus;

        public int EffectAdapterCount => effectAdapterCount;

        public string VisualText => visualText;

        public int EffectBlockingIssueCount => effectBlockingIssueCount;

        public int BlockingIssueCount
        {
            get
            {
                var count = 0;
                var items = ObservedSteps;
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i].BlockingIssue)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasObservedSteps => ObservedStepCount > 0;

        public bool HasIssues => IssueCount > 0;

        public bool HasEffectDiagnostics => EffectKind != TransitionEffectKind.Unknown
            || EffectStatus != TransitionEffectStatus.Skipped
            || EffectAdapterCount > 0
            || EffectBlockingIssueCount > 0
            || !string.Equals(VisualText, "NoneConfigured", StringComparison.Ordinal);

        public bool IsValid => OperationId.IsValid && Kind != TransitionKind.Unknown && Status != TransitionStatus.Unknown;

        public bool Succeeded => Status == TransitionStatus.Succeeded;

        public bool CompletedWithWarnings => Status == TransitionStatus.CompletedWithWarnings;

        public bool Failed => Status == TransitionStatus.Failed;

        public bool Rejected => Status == TransitionStatus.Rejected;

        public bool Cancelled => Status == TransitionStatus.Cancelled;

        public bool Completed => Succeeded || CompletedWithWarnings;

        public bool Equals(TransitionResult other)
        {
            return OperationId.Equals(other.OperationId)
                && Kind == other.Kind
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(ObservedSteps, other.ObservedSteps)
                && SequenceEquals(Issues, other.Issues)
                && EffectKind == other.EffectKind
                && EffectStatus == other.EffectStatus
                && EffectAdapterCount == other.EffectAdapterCount
                && string.Equals(VisualText, other.VisualText, StringComparison.Ordinal)
                && EffectBlockingIssueCount == other.EffectBlockingIssueCount;
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OperationId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Kind;
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                hashCode = (hashCode * 397) ^ (int)EffectKind;
                hashCode = (hashCode * 397) ^ (int)EffectStatus;
                hashCode = (hashCode * 397) ^ EffectAdapterCount;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(VisualText ?? string.Empty);
                hashCode = (hashCode * 397) ^ EffectBlockingIssueCount;

                var steps = ObservedSteps;
                for (var i = 0; i < steps.Count; i++)
                {
                    hashCode = (hashCode * 397) ^ steps[i].GetHashCode();
                }

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
            var sourceText = string.IsNullOrWhiteSpace(Source) ? "<none>" : Source;
            var reasonText = string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason;
            var messageText = string.IsNullOrWhiteSpace(Message) ? "<none>" : Message;
            var effectText = EffectKind != TransitionEffectKind.Unknown ? EffectKind.ToString() : VisualText;
            builder.Append(
                $"operation='{OperationId.StableText}' kind='{Kind}' status='{Status}' source='{sourceText}' reason='{reasonText}' observedSteps='{ObservedStepCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' message='{messageText}' effectKind='{effectText}' effectStatus='{EffectStatus}' effectAdapters='{EffectAdapterCount}' visual='{VisualText}' effectBlockingIssues='{EffectBlockingIssueCount}'");

            if (HasObservedSteps)
            {
                builder.Append(" observed=[");
                var steps = ObservedSteps;
                for (var i = 0; i < steps.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(steps[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

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

        public static TransitionResult SucceededResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.Succeeded,
                source,
                reason,
                message,
                observedSteps,
                Array.Empty<string>(),
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }

        public static TransitionResult CompletedWithWarningsResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.CompletedWithWarnings,
                source,
                reason,
                message,
                observedSteps,
                issues,
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }

        public static TransitionResult FailedResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<TransitionStep> observedSteps,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.Failed,
                source,
                reason,
                message,
                observedSteps,
                issues,
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }

        public static TransitionResult RejectedResult(
            TransitionOperationId operationId,
            TransitionKind kind,
            string source,
            string reason,
            string message,
            IReadOnlyList<string> issues,
            TransitionEffectKind effectKind = TransitionEffectKind.Unknown,
            TransitionEffectStatus effectStatus = TransitionEffectStatus.Skipped,
            int effectAdapterCount = 0,
            string visualText = "NoneConfigured",
            int effectBlockingIssueCount = 0)
        {
            return new TransitionResult(
                operationId,
                kind,
                TransitionStatus.Rejected,
                source,
                reason,
                message,
                Array.Empty<TransitionStep>(),
                issues,
                effectKind,
                effectStatus,
                effectAdapterCount,
                visualText,
                effectBlockingIssueCount);
        }

        public static bool operator ==(TransitionResult left, TransitionResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TransitionResult left, TransitionResult right)
        {
            return !left.Equals(right);
        }

        private static TransitionStep[] CopySteps(IReadOnlyList<TransitionStep> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionStep>();
            }

            var copy = new TransitionStep[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition result cannot contain invalid observed steps.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static string[] CopyIssues(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new List<string>(source.Count);
            for (var i = 0; i < source.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(source[i]))
                {
                    continue;
                }

                copy.Add(source[i].Trim());
            }

            return copy.Count == 0 ? Array.Empty<string>() : copy.ToArray();
        }

        private static bool SequenceEquals<T>(IReadOnlyList<T> left, IReadOnlyList<T> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            var comparer = EqualityComparer<T>.Default;
            for (var i = 0; i < left.Count; i++)
            {
                if (!comparer.Equals(left[i], right[i]))
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

        private static string NormalizeVisual(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "NoneConfigured" : value.Trim();
        }
    }
}

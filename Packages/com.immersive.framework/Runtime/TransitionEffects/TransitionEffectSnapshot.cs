using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;

namespace Immersive.Framework.TransitionEffects
{
    /// <summary>
    /// API status: Experimental. Passive diagnostics snapshot for Transition Effect planning/results.
    /// This is not an effect registry, loading progress model, visual surface or adapter owner.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F19B passive Transition Effect snapshot; diagnostics only.")]
    public readonly struct TransitionEffectSnapshot : IEquatable<TransitionEffectSnapshot>
    {
        private readonly TransitionEffectRequest[] _plannedRequests;
        private readonly TransitionEffectResult[] _observedResults;
        private readonly string[] _facts;

        public TransitionEffectSnapshot(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            TransitionPhase currentPhase,
            TransitionEffectStatus status,
            string source,
            string reason,
            IReadOnlyList<TransitionEffectRequest> plannedRequests,
            IReadOnlyList<TransitionEffectResult> observedResults,
            IReadOnlyList<string> facts)
        {
            if (!operationId.IsValid)
            {
                throw new ArgumentException("Transition Effect snapshot requires a valid operation id.", nameof(operationId));
            }

            if (!Enum.IsDefined(typeof(TransitionKind), transitionKind) || transitionKind == Immersive.Framework.Transition.TransitionKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(transitionKind), transitionKind, "Transition Effect snapshot transition kind must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionPhase), currentPhase) || currentPhase == TransitionPhase.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(currentPhase), currentPhase, "Transition Effect snapshot phase must be explicit.");
            }

            if (!Enum.IsDefined(typeof(TransitionEffectStatus), status) || status == TransitionEffectStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Transition Effect snapshot status must be explicit.");
            }

            OperationId = operationId;
            TransitionKind = transitionKind;
            CurrentPhase = currentPhase;
            Status = status;
            Source = Normalize(source);
            Reason = Normalize(reason);
            this._plannedRequests = CopyRequests(operationId, transitionKind, plannedRequests);
            this._observedResults = CopyResults(operationId, transitionKind, observedResults);
            this._facts = CopyFacts(facts);
        }

        public TransitionOperationId OperationId { get; }

        public TransitionKind TransitionKind { get; }

        public TransitionPhase CurrentPhase { get; }

        public TransitionEffectStatus Status { get; }

        public string Source { get; }

        public string Reason { get; }

        public IReadOnlyList<TransitionEffectRequest> PlannedRequests => _plannedRequests ?? Array.Empty<TransitionEffectRequest>();

        public IReadOnlyList<TransitionEffectResult> ObservedResults => _observedResults ?? Array.Empty<TransitionEffectResult>();

        public IReadOnlyList<string> Facts => _facts ?? Array.Empty<string>();

        public int PlannedRequestCount => PlannedRequests.Count;

        public int ObservedResultCount => ObservedResults.Count;

        public int FactCount => Facts.Count;

        public int BlockingIssueCount
        {
            get
            {
                var count = 0;
                var items = ObservedResults;
                for (var i = 0; i < items.Count; i++)
                {
                    if (items[i].BlocksTransition)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public bool HasPlannedRequests => PlannedRequestCount > 0;

        public bool HasObservedResults => ObservedResultCount > 0;

        public bool HasFacts => FactCount > 0;

        public bool IsValid => OperationId.IsValid
            && TransitionKind != Immersive.Framework.Transition.TransitionKind.Unknown
            && CurrentPhase != TransitionPhase.Unknown
            && Status != TransitionEffectStatus.Unknown;

        public bool Equals(TransitionEffectSnapshot other)
        {
            return OperationId.Equals(other.OperationId)
                && TransitionKind == other.TransitionKind
                && CurrentPhase == other.CurrentPhase
                && Status == other.Status
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal)
                && SequenceEquals(PlannedRequests, other.PlannedRequests)
                && SequenceEquals(ObservedResults, other.ObservedResults)
                && SequenceEquals(Facts, other.Facts);
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionEffectSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OperationId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)TransitionKind;
                hashCode = (hashCode * 397) ^ (int)CurrentPhase;
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);

                var requests = PlannedRequests;
                for (var i = 0; i < requests.Count; i++)
                {
                    hashCode = (hashCode * 397) ^ requests[i].GetHashCode();
                }

                var results = ObservedResults;
                for (var i = 0; i < results.Count; i++)
                {
                    hashCode = (hashCode * 397) ^ results[i].GetHashCode();
                }

                var factItems = Facts;
                for (var i = 0; i < factItems.Count; i++)
                {
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(factItems[i] ?? string.Empty);
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
            builder.Append($"operation='{OperationId.StableText}' transitionKind='{TransitionKind}' phase='{CurrentPhase}' status='{Status}' source='{sourceText}' reason='{reasonText}' planned='{PlannedRequestCount}' observed='{ObservedResultCount}' facts='{FactCount}' blockingIssues='{BlockingIssueCount}'");

            if (HasPlannedRequests)
            {
                builder.Append(" plannedRequests=[");
                var requests = PlannedRequests;
                for (var i = 0; i < requests.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(requests[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

            if (HasObservedResults)
            {
                builder.Append(" observedResults=[");
                var results = ObservedResults;
                for (var i = 0; i < results.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(results[i].ToDiagnosticString());
                }

                builder.Append(']');
            }

            if (HasFacts)
            {
                builder.Append(" facts=[");
                var factItems = Facts;
                for (var i = 0; i < factItems.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(factItems[i]);
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static TransitionEffectSnapshot FromPlan(
            TransitionEffectPlan plan,
            TransitionPhase currentPhase,
            TransitionEffectStatus status,
            IReadOnlyList<string> facts)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("Transition Effect snapshot requires a valid plan.", nameof(plan));
            }

            return new TransitionEffectSnapshot(
                plan.OperationId,
                plan.TransitionKind,
                currentPhase,
                status,
                plan.Source,
                plan.Reason,
                plan.Requests,
                Array.Empty<TransitionEffectResult>(),
                facts);
        }

        public static TransitionEffectSnapshot FromResults(
            TransitionEffectPlan plan,
            TransitionPhase currentPhase,
            TransitionEffectStatus status,
            IReadOnlyList<TransitionEffectResult> results,
            IReadOnlyList<string> facts)
        {
            if (!plan.IsValid)
            {
                throw new ArgumentException("Transition Effect snapshot requires a valid plan.", nameof(plan));
            }

            return new TransitionEffectSnapshot(
                plan.OperationId,
                plan.TransitionKind,
                currentPhase,
                status,
                plan.Source,
                plan.Reason,
                plan.Requests,
                results,
                facts);
        }

        private static TransitionEffectRequest[] CopyRequests(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            IReadOnlyList<TransitionEffectRequest> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionEffectRequest>();
            }

            var copy = new TransitionEffectRequest[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition Effect snapshot cannot contain invalid planned requests.", nameof(source));
                }

                if (source[i].OperationId != operationId || source[i].TransitionKind != transitionKind)
                {
                    throw new ArgumentException("Transition Effect snapshot cannot contain planned requests from another Transition.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static TransitionEffectResult[] CopyResults(
            TransitionOperationId operationId,
            TransitionKind transitionKind,
            IReadOnlyList<TransitionEffectResult> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<TransitionEffectResult>();
            }

            var copy = new TransitionEffectResult[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                if (!source[i].IsValid)
                {
                    throw new ArgumentException("Transition Effect snapshot cannot contain invalid observed results.", nameof(source));
                }

                if (source[i].OperationId != operationId || source[i].TransitionKind != transitionKind)
                {
                    throw new ArgumentException("Transition Effect snapshot cannot contain observed results from another Transition.", nameof(source));
                }

                copy[i] = source[i];
            }

            return copy;
        }

        private static string[] CopyFacts(IReadOnlyList<string> source)
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

        private static bool SequenceEquals(IReadOnlyList<TransitionEffectRequest> left, IReadOnlyList<TransitionEffectRequest> right)
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

        private static bool SequenceEquals(IReadOnlyList<TransitionEffectResult> left, IReadOnlyList<TransitionEffectResult> right)
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

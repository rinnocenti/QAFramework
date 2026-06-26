using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ObjectEntry;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// API status: Experimental. Aggregate result for Object Reset foundation operations.
    /// F14B resolves the logical ObjectEntry target only; participant results are added later.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F14B Object Reset aggregate result; target resolution only.")]
    public readonly struct ObjectResetResult : IEquatable<ObjectResetResult>
    {
        private readonly ObjectResetIssue[] issues;

        public ObjectResetResult(
            ObjectResetRequest request,
            ObjectResetResultStatus status,
            ObjectEntryDescriptor resolvedTarget,
            bool hasResolvedTarget,
            IReadOnlyList<ObjectResetIssue> issues,
            string message)
        {
            if (!Enum.IsDefined(typeof(ObjectResetResultStatus), status) || status == ObjectResetResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Object Reset result status must be explicit.");
            }

            Request = request;
            Status = status;
            ResolvedTarget = resolvedTarget;
            HasResolvedTarget = hasResolvedTarget;
            this.issues = issues == null ? Array.Empty<ObjectResetIssue>() : issues.ToArray();
            Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message.Trim();
        }

        public ObjectResetRequest Request { get; }

        public ObjectResetResultStatus Status { get; }

        public ObjectEntryDescriptor ResolvedTarget { get; }

        public bool HasResolvedTarget { get; }

        public IReadOnlyList<ObjectResetIssue> Issues => issues ?? Array.Empty<ObjectResetIssue>();

        public string Message { get; }

        public bool Succeeded => Status == ObjectResetResultStatus.Succeeded
            || Status == ObjectResetResultStatus.SucceededNoParticipants;

        public bool CompletedWithWarnings => Status == ObjectResetResultStatus.CompletedWithWarnings;

        public bool Failed => Status == ObjectResetResultStatus.Failed
            || Status == ObjectResetResultStatus.RejectedInvalidRequest
            || Status == ObjectResetResultStatus.RejectedRuntimeContextUnavailable
            || Status == ObjectResetResultStatus.RejectedTargetNotFound
            || Status == ObjectResetResultStatus.RejectedForeignTarget;

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public ObjectResetIssue[] SnapshotIssues()
        {
            if (IssueCount == 0)
            {
                return Array.Empty<ObjectResetIssue>();
            }

            var snapshot = new ObjectResetIssue[IssueCount];
            for (var i = 0; i < IssueCount; i++)
            {
                snapshot[i] = Issues[i];
            }

            return snapshot;
        }

        public bool Equals(ObjectResetResult other)
        {
            if (!Request.Equals(other.Request)
                || Status != other.Status
                || HasResolvedTarget != other.HasResolvedTarget
                || !ResolvedTarget.Equals(other.ResolvedTarget)
                || !string.Equals(Message, other.Message, StringComparison.Ordinal)
                || IssueCount != other.IssueCount)
            {
                return false;
            }

            for (var i = 0; i < IssueCount; i++)
            {
                if (!Issues[i].Equals(other.Issues[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectResetResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Request.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ (HasResolvedTarget ? 1 : 0);
                hashCode = (hashCode * 397) ^ ResolvedTarget.GetHashCode();
                hashCode = (hashCode * 397) ^ IssueCount;
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
            return $"status='{Status}' succeeded='{Succeeded}' failed='{Failed}' hasResolvedTarget='{HasResolvedTarget}' {Request.ToDiagnosticString()} issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}' message='{Message}'";
        }

        public static ObjectResetResult TargetResolved(ObjectResetRequest request, ObjectEntryDescriptor resolvedTarget)
        {
            return new ObjectResetResult(
                request,
                ObjectResetResultStatus.Succeeded,
                resolvedTarget,
                hasResolvedTarget: true,
                Array.Empty<ObjectResetIssue>(),
                "Object Reset target resolved from current Object Entry snapshot.");
        }

        public static ObjectResetResult Rejected(
            ObjectResetRequest request,
            ObjectResetResultStatus status,
            ObjectResetIssue issue,
            string message)
        {
            return new ObjectResetResult(
                request,
                status,
                default,
                hasResolvedTarget: false,
                new[] { issue },
                message);
        }
    }
}

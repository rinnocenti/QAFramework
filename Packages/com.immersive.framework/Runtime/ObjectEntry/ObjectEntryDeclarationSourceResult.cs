using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ObjectEntry
{
    /// <summary>
    /// API status: Experimental. Result produced by collecting passive ObjectEntryDeclaration components.
    /// It exposes descriptors and diagnostics only; it is not a runtime registry, binding table or reset inventory.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Object Entry declaration source result introduced by F13D.")]
    public sealed class ObjectEntryDeclarationSourceResult
    {
        private static readonly IReadOnlyList<ObjectEntryIssue> EmptyIssues = Array.Empty<ObjectEntryIssue>();

        public ObjectEntryDeclarationSourceResult(
            ObjectEntrySet objectEntries,
            int declarationCount,
            int acceptedDeclarationCount,
            int rejectedDeclarationCount,
            IEnumerable<ObjectEntryIssue> issues = null)
        {
            if (declarationCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(declarationCount), declarationCount, "Declaration count cannot be negative.");
            }

            if (acceptedDeclarationCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(acceptedDeclarationCount), acceptedDeclarationCount, "Accepted declaration count cannot be negative.");
            }

            if (rejectedDeclarationCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rejectedDeclarationCount), rejectedDeclarationCount, "Rejected declaration count cannot be negative.");
            }

            ObjectEntries = objectEntries ?? ObjectEntrySet.Empty();
            DeclarationCount = declarationCount;
            AcceptedDeclarationCount = acceptedDeclarationCount;
            RejectedDeclarationCount = rejectedDeclarationCount;
            Issues = issues == null ? EmptyIssues : issues.ToArray();
        }

        public ObjectEntrySet ObjectEntries { get; }

        public int DeclarationCount { get; }

        public int AcceptedDeclarationCount { get; }

        public int RejectedDeclarationCount { get; }

        public IReadOnlyList<ObjectEntryIssue> Issues { get; }

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Issues.Count(issue => issue.IsBlocking);

        public int NonBlockingIssueCount => IssueCount - BlockingIssueCount;

        public bool Succeeded => BlockingIssueCount == 0;

        public bool Failed => !Succeeded;

        public string Summary => $"declarations='{DeclarationCount}' acceptedDeclarations='{AcceptedDeclarationCount}' rejectedDeclarations='{RejectedDeclarationCount}' objectEntries='{ObjectEntries.Count}' required='{ObjectEntries.RequiredCount}' optional='{ObjectEntries.OptionalCount}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' nonBlockingIssues='{NonBlockingIssueCount}'";

        public string ToDiagnosticString()
        {
            if (Issues.Count == 0)
            {
                return Summary;
            }

            return $"{Summary} issueDetails=[{string.Join(" | ", Issues.Select(issue => issue.ToString()))}]";
        }
    }
}

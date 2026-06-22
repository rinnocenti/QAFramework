using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Immutable release plan for content known by one framework lifecycle scope.
    /// F6F builds release intent only; physical release execution is intentionally deferred.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6F release plan model; execution starts in a later cut.")]
    internal readonly struct ContentReleasePlan
    {
        private readonly ContentReleasePlanEntry[] entries;

        public ContentReleasePlan(
            FrameworkContentScope scope,
            string ownerId,
            string ownerName,
            IReadOnlyList<ContentReleasePlanEntry> entries,
            string source,
            string reason,
            string message)
        {
            if (!Enum.IsDefined(typeof(FrameworkContentScope), scope))
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Release plan scope must be explicit.");
            }

            Scope = scope;
            OwnerId = Normalize(ownerId);
            OwnerName = Normalize(ownerName);
            Source = Normalize(source);
            Reason = Normalize(reason);
            Message = Normalize(message);

            if (entries == null || entries.Count == 0)
            {
                this.entries = Array.Empty<ContentReleasePlanEntry>();
            }
            else
            {
                this.entries = new ContentReleasePlanEntry[entries.Count];
                for (var i = 0; i < entries.Count; i++)
                {
                    this.entries[i] = entries[i];
                }
            }
        }

        public FrameworkContentScope Scope { get; }

        public string OwnerId { get; }

        public string OwnerName { get; }

        public IReadOnlyList<ContentReleasePlanEntry> Entries => entries ?? Array.Empty<ContentReleasePlanEntry>();

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public int EntryCount => entries != null ? entries.Length : 0;

        public bool IsEmpty => EntryCount == 0;

        public bool HasEntries => !IsEmpty;

        public int OwnedCount => CountByOwnership(ContentReleaseOwnership.Owned);

        public int RegisteredCount => CountByOwnership(ContentReleaseOwnership.Registered);

        public int DiagnosticOnlyCount => CountByOwnership(ContentReleaseOwnership.DiagnosticOnly);

        public int ReleasableCount => CountReleasable();

        public int NoActionCount => CountByAction(ContentReleaseAction.None);

        public int UnloadSceneCount => CountByAction(ContentReleaseAction.UnloadScene);

        public static ContentReleasePlan Empty(
            FrameworkContentScope scope,
            string ownerId,
            string ownerName,
            string source,
            string reason,
            string message)
        {
            return new ContentReleasePlan(
                scope,
                ownerId,
                ownerName,
                Array.Empty<ContentReleasePlanEntry>(),
                source,
                reason,
                message);
        }

        public string ToDiagnosticString()
        {
            var owner = !string.IsNullOrWhiteSpace(OwnerName) ? OwnerName : OwnerId;
            if (string.IsNullOrWhiteSpace(owner))
            {
                owner = "<missing>";
            }

            var message = !string.IsNullOrWhiteSpace(Message) ? Message : "<none>";
            var builder = new StringBuilder();
            builder.Append($"Content Release Plan scope='{Scope}' owner='{owner}' ownerId='{OwnerId}' entries='{EntryCount}' owned='{OwnedCount}' registered='{RegisteredCount}' diagnosticOnly='{DiagnosticOnlyCount}' releasable='{ReleasableCount}' unloadScene='{UnloadSceneCount}' noAction='{NoActionCount}' source='{Source}' reason='{Reason}' message='{message}' details=[");
            for (var i = 0; i < Entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" | ");
                }

                builder.Append(Entries[i].ToDiagnosticString());
            }

            builder.Append("]");
            return builder.ToString();
        }

        private int CountByOwnership(ContentReleaseOwnership ownership)
        {
            if (entries == null || entries.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Ownership == ownership)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountByAction(ContentReleaseAction action)
        {
            if (entries == null || entries.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Action == action)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountReleasable()
        {
            if (entries == null || entries.Length == 0)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].IsReleasable)
                {
                    count++;
                }
            }

            return count;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

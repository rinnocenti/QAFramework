using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25H explicit ledger for Activity-owned Unity scenes.")]
    internal sealed class ActivitySceneLedger
    {
        private readonly List<ActivitySceneLedgerEntry> _entries = new List<ActivitySceneLedgerEntry>();

        internal int EntryCount => _entries.Count;

        internal int LoadedCount => CountByStatus(ActivitySceneLedgerEntryStatus.Loaded);

        internal int ReleasedCount => CountByStatus(ActivitySceneLedgerEntryStatus.Released);

        internal int StaleCount => CountByStatus(ActivitySceneLedgerEntryStatus.Stale);

        internal void RegisterLoaded(
            string routeInstanceId,
            RouteAsset route,
            ActivityAsset activity,
            ActivitySceneCompositionResultEntry entry)
        {
            if (activity == null || !entry.Loaded && !entry.AlreadyLoaded)
            {
                return;
            }

            var ledgerEntry = new ActivitySceneLedgerEntry(
                routeInstanceId,
                route,
                activity,
                entry.PlanEntry,
                ActivitySceneLedgerOwnership.Activity,
                ActivitySceneLedgerEntryStatus.Loaded);

            Upsert(ledgerEntry);
        }

        internal bool TryGetLoaded(
            ActivityAsset activity,
            ActivitySceneCompositionPlanEntry planEntry,
            out ActivitySceneLedgerEntry entry)
        {
            entry = default;
            if (activity == null || planEntry.ContentIdentity.IsValid == false)
            {
                return false;
            }

            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var existing = _entries[i];
                if (!existing.IsLoaded)
                {
                    continue;
                }

                if (ReferenceEquals(existing.Activity, activity)
                    && string.Equals(existing.ContentIdentity, planEntry.ContentIdentity.StableText, StringComparison.Ordinal))
                {
                    entry = existing;
                    return true;
                }
            }

            return false;
        }

        internal List<ActivitySceneLedgerEntry> CollectLoadedForActivity(ActivityAsset activity)
        {
            var entries = new List<ActivitySceneLedgerEntry>();
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.IsLoaded && ReferenceEquals(entry.Activity, activity))
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        internal List<ActivitySceneLedgerEntry> CollectLoadedForActivityRouteInstance(ActivityAsset activity, string routeInstanceId)
        {
            var entries = new List<ActivitySceneLedgerEntry>();
            var normalizedRouteInstanceId = Normalize(routeInstanceId);
            if (activity == null || string.IsNullOrWhiteSpace(normalizedRouteInstanceId))
            {
                return entries;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.IsLoaded
                    && entry.Ownership == ActivitySceneLedgerOwnership.Activity
                    && ReferenceEquals(entry.Activity, activity)
                    && string.Equals(entry.RouteInstanceId, normalizedRouteInstanceId, StringComparison.Ordinal))
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        internal List<ActivitySceneLedgerEntry> CollectLoaded()
        {
            var entries = new List<ActivitySceneLedgerEntry>();
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.IsLoaded)
                {
                    entries.Add(entry);
                }
            }

            return entries;
        }

        internal void MarkReleased(ActivitySceneLedgerEntry entry)
        {
            UpdateStatus(entry, ActivitySceneLedgerEntryStatus.Released);
        }

        internal void MarkStale(ActivitySceneLedgerEntry entry)
        {
            UpdateStatus(entry, ActivitySceneLedgerEntryStatus.Stale);
        }

        private void Upsert(ActivitySceneLedgerEntry entry)
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var existing = _entries[i];
                if (ReferenceEquals(existing.Activity, entry.Activity)
                    && string.Equals(existing.ContentIdentity, entry.ContentIdentity, StringComparison.Ordinal))
                {
                    _entries[i] = entry;
                    return;
                }
            }

            _entries.Add(entry);
        }

        private void UpdateStatus(ActivitySceneLedgerEntry entry, ActivitySceneLedgerEntryStatus status)
        {
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var existing = _entries[i];
                if (ReferenceEquals(existing.Activity, entry.Activity)
                    && string.Equals(existing.ContentIdentity, entry.ContentIdentity, StringComparison.Ordinal))
                {
                    _entries[i] = existing.WithStatus(status);
                    return;
                }
            }
        }

        private int CountByStatus(ActivitySceneLedgerEntryStatus status)
        {
            var count = 0;
            for (var i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Status == status)
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

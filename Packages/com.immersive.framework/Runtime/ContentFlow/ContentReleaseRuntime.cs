using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.ContentFlow
{
    /// <summary>
    /// Executes release actions from an explicit content release plan.
    /// F6G only supports owned additive scene unloads; all other actions remain skipped.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6G release execution for route-owned additive scenes only.")]
    internal sealed class ContentReleaseRuntime
    {
        private readonly SceneLifecycleRuntime sceneLifecycleRuntime;

        internal ContentReleaseRuntime(SceneLifecycleRuntime sceneLifecycleRuntime)
        {
            this.sceneLifecycleRuntime = sceneLifecycleRuntime ?? throw new System.ArgumentNullException(nameof(sceneLifecycleRuntime));
        }

        internal async Task<ContentReleaseResult> ExecuteAsync(ContentReleasePlan plan)
        {
            if (plan.IsEmpty)
            {
                return new ContentReleaseResult(
                    plan,
                    System.Array.Empty<ContentReleaseResultEntry>(),
                    ContentReleaseStatus.Succeeded,
                    true,
                    plan.Source,
                    plan.Reason,
                    "Content release completed with an empty plan.");
            }

            var results = new List<ContentReleaseResultEntry>(plan.EntryCount);
            for (var i = 0; i < plan.Entries.Count; i++)
            {
                results.Add(await ExecuteEntryAsync(plan.Entries[i]));
            }

            var status = ResolveStatus(results);
            return new ContentReleaseResult(
                plan,
                results,
                status,
                status != ContentReleaseStatus.Failed,
                plan.Source,
                plan.Reason,
                BuildMessage(status, results));
        }

        private async Task<ContentReleaseResultEntry> ExecuteEntryAsync(ContentReleasePlanEntry entry)
        {
            if (!entry.IsReleasable)
            {
                return ContentReleaseResultEntry.SkippedEntry(
                    entry,
                    "Content release skipped because the plan entry has no release action.");
            }

            switch (entry.Action)
            {
                case ContentReleaseAction.UnloadScene:
                    return await ExecuteUnloadSceneAsync(entry);
                default:
                    return ContentReleaseResultEntry.SkippedEntry(
                        entry,
                        $"Content release action '{entry.Action}' is not executable in F6G.");
            }
        }

        private async Task<ContentReleaseResultEntry> ExecuteUnloadSceneAsync(ContentReleasePlanEntry entry)
        {
            var unloadResult = await sceneLifecycleRuntime.UnloadSceneAsync(entry.ResourceName, entry.ResourcePath);
            if (unloadResult.Unloaded)
            {
                return ContentReleaseResultEntry.ReleasedEntry(entry, unloadResult.Message);
            }

            if (unloadResult.Skipped)
            {
                return ContentReleaseResultEntry.SkippedEntry(entry, unloadResult.Message);
            }

            return ContentReleaseResultEntry.FailedEntry(
                entry,
                ShouldBlockRelease(entry),
                unloadResult.Message);
        }

        private static ContentReleaseStatus ResolveStatus(IReadOnlyList<ContentReleaseResultEntry> results)
        {
            var failed = 0;
            var issues = 0;
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].Failed)
                {
                    failed++;
                }

                if (results[i].HasIssue)
                {
                    issues++;
                }
            }

            if (failed > 0)
            {
                return ContentReleaseStatus.Failed;
            }

            return issues > 0
                ? ContentReleaseStatus.SucceededWithIssues
                : ContentReleaseStatus.Succeeded;
        }

        private static bool ShouldBlockRelease(ContentReleasePlanEntry entry)
        {
            return entry.IsOwned && entry.Requiredness == FrameworkContentRequiredness.Required;
        }

        private static string BuildMessage(
            ContentReleaseStatus status,
            IReadOnlyList<ContentReleaseResultEntry> results)
        {
            var released = 0;
            var skipped = 0;
            var failed = 0;
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].Released)
                {
                    released++;
                }

                if (results[i].Skipped)
                {
                    skipped++;
                }

                if (results[i].Failed)
                {
                    failed++;
                }
            }

            return $"Content release executed. status='{status}' released='{released}' skipped='{skipped}' failed='{failed}'.";
        }
    }
}

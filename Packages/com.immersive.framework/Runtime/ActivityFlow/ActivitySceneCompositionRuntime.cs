using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Executes Activity-owned additive scene composition plans and releases Activity-owned scenes on Activity change.
    /// F25C added additive load. F25D adds ReleaseOnActivityChange unload.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25D Activity scene composition execution and Activity-change release.")]
    internal sealed class ActivitySceneCompositionRuntime
    {
        private readonly SceneLifecycleRuntime _sceneLifecycleRuntime;
        private readonly List<LoadedActivitySceneRecord> _loadedScenes = new List<LoadedActivitySceneRecord>();

        internal ActivitySceneCompositionRuntime(SceneLifecycleRuntime sceneLifecycleRuntime)
        {
            _sceneLifecycleRuntime = sceneLifecycleRuntime ?? throw new ArgumentNullException(nameof(sceneLifecycleRuntime));
        }

        internal async Task<ActivitySceneCompositionResult> ExecuteAsync(ActivitySceneCompositionPlan plan)
        {
            if (!plan.HasActivity || !plan.HasProfile || !plan.HasScenes)
            {
                return ActivitySceneCompositionResult.FromPlan(plan, plan.Source, plan.Reason);
            }

            var entries = new List<ActivitySceneCompositionResultEntry>(plan.SceneCount);
            for (var i = 0; i < plan.Scenes.Count; i++)
            {
                var scene = plan.Scenes[i];
                if (!scene.IsExecutionReady)
                {
                    entries.Add(ActivitySceneCompositionResultEntry.NotExecutionReadyEntry(scene));
                    continue;
                }

                var loadResult = await _sceneLifecycleRuntime.LoadAdditiveSceneAsync(scene.SceneName, scene.ScenePath);
                if (loadResult.Loaded)
                {
                    var loadedEntry = ActivitySceneCompositionResultEntry.LoadedEntry(scene, loadResult);
                    entries.Add(loadedEntry);
                    TrackLoadedScene(plan.Activity, loadedEntry);
                    continue;
                }

                entries.Add(ActivitySceneCompositionResultEntry.FailedEntry(scene, loadResult));
            }

            return ActivitySceneCompositionResult.ExecutedResult(plan, entries, plan.Source, plan.Reason);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseOnActivityChangeAsync(
            ActivityAsset activity,
            string source,
            string reason)
        {
            return ReleaseTrackedScenesAsync(activity, forceReleaseAll: false, source, reason);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseForRouteChangeAsync(
            ActivityAsset activity,
            string source,
            string reason)
        {
            return ReleaseTrackedScenesAsync(activity, forceReleaseAll: true, source, reason);
        }

        internal bool HasAnyTrackedScenes(ActivityAsset activity)
        {
            if (activity == null)
            {
                return false;
            }

            for (var i = 0; i < _loadedScenes.Count; i++)
            {
                if (ReferenceEquals(_loadedScenes[i].Activity, activity))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool HasReleaseOnActivityChangeScenes(ActivityAsset activity)
        {
            if (activity == null)
            {
                return false;
            }

            for (var i = 0; i < _loadedScenes.Count; i++)
            {
                var record = _loadedScenes[i];
                if (ReferenceEquals(record.Activity, activity)
                    && record.Entry.ReleasePolicy == ActivityContentReleasePolicy.ReleaseOnActivityChange)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<ActivitySceneReleaseResult> ReleaseTrackedScenesAsync(
            ActivityAsset activity,
            bool forceReleaseAll,
            string source,
            string reason)
        {
            if (activity == null)
            {
                return ActivitySceneReleaseResult.NotRequested(
                    null,
                    source,
                    reason,
                    "Activity scene release was not requested because Activity is missing.");
            }

            var records = CollectRecordsForActivity(activity);
            if (records.Count == 0)
            {
                return ActivitySceneReleaseResult.NotRequested(
                    activity,
                    source,
                    reason,
                    "Activity scene release skipped because no Activity-owned scenes are tracked for this Activity.");
            }

            var entries = new List<ActivitySceneReleaseResultEntry>(records.Count);
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var planEntry = record.Entry.PlanEntry;
                if (!forceReleaseAll && planEntry.ReleasePolicy == ActivityContentReleasePolicy.KeepOnActivityChange)
                {
                    entries.Add(ActivitySceneReleaseResultEntry.SkippedKeepOnActivityChangeEntry(planEntry));
                    continue;
                }

                var unloadResult = await _sceneLifecycleRuntime.UnloadSceneAsync(planEntry.SceneName, planEntry.ScenePath);
                if (unloadResult.Completed && unloadResult.Unloaded)
                {
                    entries.Add(ActivitySceneReleaseResultEntry.UnloadedEntry(planEntry, unloadResult));
                    RemoveTrackedScene(record);
                    continue;
                }

                if (unloadResult.Completed && unloadResult.Skipped)
                {
                    entries.Add(ActivitySceneReleaseResultEntry.SkippedNotLoadedEntry(planEntry, unloadResult));
                    RemoveTrackedScene(record);
                    continue;
                }

                entries.Add(ActivitySceneReleaseResultEntry.FailedEntry(planEntry, unloadResult));
            }

            return ActivitySceneReleaseResult.FromEntries(activity, entries, source, reason);
        }

        internal static bool ShouldExecute(ActivitySceneCompositionPlan plan)
        {
            return plan.HasActivity
                && plan.HasProfile
                && plan.HasScenes
                && plan.ExecutionReadySceneCount > 0;
        }

        private void TrackLoadedScene(ActivityAsset activity, ActivitySceneCompositionResultEntry entry)
        {
            if (activity == null || !entry.Loaded && !entry.AlreadyLoaded)
            {
                return;
            }

            var identity = entry.ContentIdentity.StableText;
            for (var i = 0; i < _loadedScenes.Count; i++)
            {
                var existing = _loadedScenes[i];
                if (ReferenceEquals(existing.Activity, activity)
                    && string.Equals(existing.Entry.ContentIdentity.StableText, identity, StringComparison.Ordinal))
                {
                    _loadedScenes[i] = new LoadedActivitySceneRecord(activity, entry);
                    return;
                }
            }

            _loadedScenes.Add(new LoadedActivitySceneRecord(activity, entry));
        }

        private List<LoadedActivitySceneRecord> CollectRecordsForActivity(ActivityAsset activity)
        {
            var records = new List<LoadedActivitySceneRecord>();
            for (var i = 0; i < _loadedScenes.Count; i++)
            {
                var record = _loadedScenes[i];
                if (ReferenceEquals(record.Activity, activity))
                {
                    records.Add(record);
                }
            }

            return records;
        }

        private void RemoveTrackedScene(LoadedActivitySceneRecord record)
        {
            for (var i = _loadedScenes.Count - 1; i >= 0; i--)
            {
                var existing = _loadedScenes[i];
                if (ReferenceEquals(existing.Activity, record.Activity)
                    && string.Equals(existing.Entry.ContentIdentity.StableText, record.Entry.ContentIdentity.StableText, StringComparison.Ordinal))
                {
                    _loadedScenes.RemoveAt(i);
                    return;
                }
            }
        }

        private readonly struct LoadedActivitySceneRecord
        {
            internal LoadedActivitySceneRecord(ActivityAsset activity, ActivitySceneCompositionResultEntry entry)
            {
                Activity = activity;
                Entry = entry;
            }

            internal ActivityAsset Activity { get; }

            internal ActivitySceneCompositionResultEntry Entry { get; }
        }
    }
}

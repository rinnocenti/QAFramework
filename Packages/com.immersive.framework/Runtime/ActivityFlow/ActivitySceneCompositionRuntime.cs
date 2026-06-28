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

        internal ActivityOperationPlan CreateActivityOperationPlan(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            ActivityVisualTransitionMode visualMode,
            string source,
            string reason)
        {
            var scenes = new List<ActivityOperationPlanSceneEntry>();
            var issues = new List<ActivityOperationIssue>();

            AddReleaseEntries(operationKind, previousActivity, targetActivity, scenes, issues);
            AddLoadEntries(operationKind, targetActivity, scenes);

            return new ActivityOperationPlan(
                operationKind,
                previousActivity,
                targetActivity,
                visualMode,
                scenes,
                issues,
                source,
                reason);
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

                if (TryGetTrackedScene(plan.Activity, scene, out var trackedRecord))
                {
                    if (_sceneLifecycleRuntime.IsSceneLoaded(scene.SceneName, scene.ScenePath))
                    {
                        entries.Add(ActivitySceneCompositionResultEntry.AlreadyLoadedTrackedEntry(scene));
                        continue;
                    }

                    RemoveTrackedScene(trackedRecord);
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
            string source,
            string reason)
        {
            return ReleaseTrackedScenesAsync(activity: null, forceReleaseAll: true, source, reason);
        }

        internal bool HasAnyTrackedScenes()
        {
            return _loadedScenes.Count > 0;
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

        internal bool HasSceneLoadOnActivityChange(ActivityAsset activity, string source, string reason)
        {
            if (activity == null)
            {
                return false;
            }

            var plan = ActivitySceneCompositionPlan.FromActivity(activity, source, reason);
            if (!ShouldExecute(plan))
            {
                return false;
            }

            for (var i = 0; i < plan.Scenes.Count; i++)
            {
                var scene = plan.Scenes[i];
                if (!scene.IsExecutionReady)
                {
                    continue;
                }

                if (!TryGetTrackedScene(activity, scene, out _))
                {
                    return true;
                }

                if (!_sceneLifecycleRuntime.IsSceneLoaded(scene.SceneName, scene.ScenePath))
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
            if (activity == null && !forceReleaseAll)
            {
                return ActivitySceneReleaseResult.NotRequested(
                    null,
                    source,
                    reason,
                    "Activity scene release was not requested because Activity is missing.");
            }

            var records = forceReleaseAll && activity == null
                ? CollectAllRecords()
                : CollectRecordsForActivity(activity);
            if (records.Count == 0)
            {
                return ActivitySceneReleaseResult.NotRequested(
                    activity,
                    source,
                    reason,
                    forceReleaseAll
                        ? "Activity scene release skipped because no Activity-owned scenes are tracked for route change."
                        : "Activity scene release skipped because no Activity-owned scenes are tracked for this Activity.");
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

        private void AddLoadEntries(
            ActivityOperationKind operationKind,
            ActivityAsset targetActivity,
            List<ActivityOperationPlanSceneEntry> scenes)
        {
            if (!ShouldPlanTargetLoad(operationKind, targetActivity))
            {
                return;
            }

            var compositionPlan = ActivitySceneCompositionPlan.FromActivity(targetActivity, source: string.Empty, reason: string.Empty);
            if (!ShouldExecute(compositionPlan))
            {
                return;
            }

            for (var i = 0; i < compositionPlan.Scenes.Count; i++)
            {
                var scene = compositionPlan.Scenes[i];
                if (!scene.IsExecutionReady)
                {
                    continue;
                }

                var isAlreadyLoaded = _sceneLifecycleRuntime.IsSceneLoaded(scene.SceneName, scene.ScenePath);
                scenes.Add(ActivityOperationPlanSceneEntry.FromCompositionPlanEntry(scene, isAlreadyLoaded));
            }
        }

        private void AddReleaseEntries(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            List<ActivityOperationPlanSceneEntry> scenes,
            List<ActivityOperationIssue> issues)
        {
            if (!ShouldPlanRelease(operationKind, previousActivity, targetActivity))
            {
                return;
            }

            var records = operationKind == ActivityOperationKind.RouteExitCleanup
                ? CollectAllRecords()
                : CollectRecordsForActivity(previousActivity);

            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var planEntry = record.Entry.PlanEntry;
                if (operationKind != ActivityOperationKind.RouteExitCleanup
                    && planEntry.ReleasePolicy == ActivityContentReleasePolicy.KeepOnActivityChange)
                {
                    continue;
                }

                if (!_sceneLifecycleRuntime.IsSceneLoaded(planEntry.SceneName, planEntry.ScenePath))
                {
                    issues.Add(ActivityOperationIssue.Warning(
                        ActivityOperationIssueKind.StaleTrackedScene,
                        $"Tracked Activity scene '{ResolveSceneLabel(planEntry)}' is not loaded in Unity; release side-effect is not planned."));
                    continue;
                }

                scenes.Add(ActivityOperationPlanSceneEntry.ForRelease(
                    planEntry.ContentIdentity,
                    planEntry.ContentId,
                    planEntry.SceneName,
                    planEntry.ScenePath,
                    planEntry.Requiredness,
                    planEntry.LoadMode,
                    planEntry.ReleasePolicy,
                    planEntry.ExecutionOrder));
            }
        }

        private static bool ShouldPlanTargetLoad(ActivityOperationKind operationKind, ActivityAsset targetActivity)
        {
            return targetActivity != null
                && (operationKind == ActivityOperationKind.Start
                    || operationKind == ActivityOperationKind.Switch
                    || operationKind == ActivityOperationKind.RouteStartup);
        }

        private static bool ShouldPlanRelease(
            ActivityOperationKind operationKind,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity)
        {
            if (operationKind == ActivityOperationKind.RouteExitCleanup)
            {
                return true;
            }

            if (previousActivity == null || ReferenceEquals(previousActivity, targetActivity))
            {
                return false;
            }

            return operationKind == ActivityOperationKind.Switch
                || operationKind == ActivityOperationKind.Clear;
        }

        private static string ResolveSceneLabel(ActivitySceneCompositionPlanEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.SceneName))
            {
                return entry.SceneName;
            }

            if (!string.IsNullOrWhiteSpace(entry.ScenePath))
            {
                return entry.ScenePath;
            }

            return "<missing>";
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

        private bool TryGetTrackedScene(
            ActivityAsset activity,
            ActivitySceneCompositionPlanEntry entry,
            out LoadedActivitySceneRecord record)
        {
            record = default;
            if (activity == null || entry.ContentIdentity.IsValid == false)
            {
                return false;
            }

            var identity = entry.ContentIdentity.StableText;
            for (var i = 0; i < _loadedScenes.Count; i++)
            {
                var existing = _loadedScenes[i];
                if (ReferenceEquals(existing.Activity, activity)
                    && string.Equals(existing.Entry.ContentIdentity.StableText, identity, StringComparison.Ordinal))
                {
                    record = existing;
                    return true;
                }
            }

            return false;
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

        private List<LoadedActivitySceneRecord> CollectAllRecords()
        {
            return new List<LoadedActivitySceneRecord>(_loadedScenes);
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

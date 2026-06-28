using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Loading;

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
        private readonly ActivitySceneLedger _ledger = new ActivitySceneLedger();
        private RouteAsset _currentRoute;
        private string _currentRouteInstanceId = string.Empty;

        internal ActivitySceneCompositionRuntime(SceneLifecycleRuntime sceneLifecycleRuntime)
        {
            _sceneLifecycleRuntime = sceneLifecycleRuntime ?? throw new ArgumentNullException(nameof(sceneLifecycleRuntime));
        }

        internal void SetRouteContext(RouteAsset route, string routeInstanceId)
        {
            _currentRoute = route;
            _currentRouteInstanceId = Normalize(routeInstanceId);
        }

        internal int LedgerEntryCount => _ledger.EntryCount;

        internal int LedgerLoadedCount => _ledger.LoadedCount;

        internal int LedgerReleasedCount => _ledger.ReleasedCount;

        internal int LedgerStaleCount => _ledger.StaleCount;

        internal ActivityContentDiscoveryScope CreateActivityContentDiscoveryScope(ActivityAsset activity)
        {
            return CreateActivityContentDiscoveryScope(activity, null);
        }

        internal ActivityContentDiscoveryScope CreateActivityContentDiscoveryScope(ActivityAsset firstActivity, ActivityAsset secondActivity)
        {
            var scenes = new List<ActivityContentDiscoveryScene>();
            var sceneKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AddActivityOwnedDiscoveryScenes(firstActivity, scenes, sceneKeys);
            if (!ReferenceEquals(firstActivity, secondActivity))
            {
                AddActivityOwnedDiscoveryScenes(secondActivity, scenes, sceneKeys);
            }

            return new ActivityContentDiscoveryScope(_currentRoute, _currentRouteInstanceId, scenes);
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

        internal Task<ActivitySceneCompositionResult> ExecuteAsync(ActivitySceneCompositionPlan plan)
        {
            return ExecuteAsync(plan, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<ActivitySceneCompositionResult> ExecuteAsync(
            ActivitySceneCompositionPlan plan,
            IFrameworkLoadingProgressReporter progressReporter)
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

                if (_ledger.TryGetLoaded(plan.Activity, scene, _currentRouteInstanceId, out var ledgerEntry))
                {
                    if (_sceneLifecycleRuntime.IsSceneLoaded(scene.SceneName, scene.ScenePath))
                    {
                        entries.Add(ActivitySceneCompositionResultEntry.AlreadyLoadedTrackedEntry(scene));
                        continue;
                    }

                    _ledger.MarkStale(ledgerEntry);
                }

                var loadResult = await _sceneLifecycleRuntime.LoadAdditiveSceneAsync(
                    scene.SceneName,
                    scene.ScenePath,
                    progressReporter);
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
            return ReleaseOnActivityChangeAsync(activity, source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseOnActivityChangeAsync(
            ActivityAsset activity,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return ReleaseTrackedScenesAsync(activity, forceReleaseAll: false, source, reason, progressReporter);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseForRouteChangeAsync(
            string source,
            string reason)
        {
            return ReleaseForRouteChangeAsync(source, reason, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal Task<ActivitySceneReleaseResult> ReleaseForRouteChangeAsync(
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            return ReleaseTrackedScenesAsync(activity: null, forceReleaseAll: true, source, reason, progressReporter);
        }

        private async Task<ActivitySceneReleaseResult> ReleaseTrackedScenesAsync(
            ActivityAsset activity,
            bool forceReleaseAll,
            string source,
            string reason,
            IFrameworkLoadingProgressReporter progressReporter)
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
                ? _ledger.CollectLoadedForRouteInstance(_currentRouteInstanceId)
                : _ledger.CollectLoadedForActivityRouteInstance(activity, _currentRouteInstanceId);
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
                var planEntry = record.PlanEntry;
                if (!forceReleaseAll && planEntry.ReleasePolicy == ActivityContentReleasePolicy.KeepOnActivityChange)
                {
                    entries.Add(ActivitySceneReleaseResultEntry.SkippedKeepOnActivityChangeEntry(planEntry));
                    continue;
                }

                var unloadResult = await _sceneLifecycleRuntime.UnloadSceneAsync(
                    planEntry.SceneName,
                    planEntry.ScenePath,
                    progressReporter);
                if (unloadResult.Completed && unloadResult.Unloaded)
                {
                    entries.Add(ActivitySceneReleaseResultEntry.UnloadedEntry(planEntry, unloadResult));
                    _ledger.MarkReleased(record);
                    continue;
                }

                if (unloadResult.Completed && unloadResult.Skipped)
                {
                    entries.Add(ActivitySceneReleaseResultEntry.SkippedNotLoadedEntry(planEntry, unloadResult));
                    _ledger.MarkStale(record);
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
                ? _ledger.CollectLoadedForRouteInstance(_currentRouteInstanceId)
                : _ledger.CollectLoadedForActivityRouteInstance(previousActivity, _currentRouteInstanceId);

            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                var planEntry = record.PlanEntry;
                if (operationKind != ActivityOperationKind.RouteExitCleanup
                    && planEntry.ReleasePolicy == ActivityContentReleasePolicy.KeepOnActivityChange)
                {
                    continue;
                }

                if (!_sceneLifecycleRuntime.IsSceneLoaded(planEntry.SceneName, planEntry.ScenePath))
                {
                    _ledger.MarkStale(record);
                    issues.Add(ActivityOperationIssue.Warning(
                        ActivityOperationIssueKind.StaleTrackedScene,
                        $"Ledger Activity scene '{ResolveSceneLabel(planEntry)}' is marked stale because Unity no longer reports it as loaded; release side-effect is not planned."));
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
            _ledger.RegisterLoaded(
                _currentRouteInstanceId,
                _currentRoute,
                activity,
                entry);
        }

        private void AddActivityOwnedDiscoveryScenes(
            ActivityAsset activity,
            List<ActivityContentDiscoveryScene> scenes,
            HashSet<string> sceneKeys)
        {
            if (activity == null || scenes == null || sceneKeys == null)
            {
                return;
            }

            var records = _ledger.CollectLoadedForActivityRouteInstance(activity, _currentRouteInstanceId);
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (!_sceneLifecycleRuntime.IsSceneLoaded(record.SceneName, record.ScenePath))
                {
                    continue;
                }

                var sceneKey = CreateSceneKey(record.ScenePath, record.SceneName);
                if (string.IsNullOrWhiteSpace(sceneKey) || !sceneKeys.Add(sceneKey))
                {
                    continue;
                }

                scenes.Add(new ActivityContentDiscoveryScene(
                    record.Activity,
                    record.RouteInstanceId,
                    record.ScenePath,
                    record.SceneName));
            }
        }

        private static string CreateSceneKey(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return scenePath.Trim();
            }

            return string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

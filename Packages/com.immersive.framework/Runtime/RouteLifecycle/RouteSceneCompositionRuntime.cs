using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Loading;

namespace Immersive.Framework.RouteLifecycle
{
    /// <summary>
    /// Executes the Route scene composition plan against Scene Lifecycle.
    /// This cut loads declared additional scenes additively and records composition evidence.
    /// Release/unload remains outside this runtime.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F6E route scene composition execution; additive load only, no release/unload side effects.")]
    internal sealed class RouteSceneCompositionRuntime
    {
        private readonly SceneLifecycleRuntime _sceneLifecycleRuntime;

        internal RouteSceneCompositionRuntime(SceneLifecycleRuntime sceneLifecycleRuntime)
        {
            _sceneLifecycleRuntime = sceneLifecycleRuntime ?? throw new ArgumentNullException(nameof(sceneLifecycleRuntime));
        }

        internal Task<RouteSceneCompositionResult> ExecuteAsync(RouteSceneCompositionPlan plan)
        {
            return ExecuteAsync(plan, NoOpFrameworkLoadingProgressReporter.Instance);
        }

        internal async Task<RouteSceneCompositionResult> ExecuteAsync(
            RouteSceneCompositionPlan plan,
            IFrameworkLoadingProgressReporter progressReporter)
        {
            var entries = new List<RouteSceneCompositionResultEntry>(Math.Max(1, plan.EntryCount));
            if (!plan.HasRoute)
            {
                entries.Add(RouteSceneCompositionResultEntry.FailedEntry(
                    plan.PrimaryScene,
                    true,
                    "Route scene composition failed because Route is missing."));

                return RouteSceneCompositionResult.ExecutedResult(
                    plan,
                    entries,
                    SceneLifecycleLoadResult.Failed("Route scene composition failed because Route is missing."),
                    plan.Source,
                    plan.Reason);
            }

            var primarySceneLoadResult = await _sceneLifecycleRuntime.LoadPrimarySceneAsync(plan.Route, progressReporter);
            if (!primarySceneLoadResult.Loaded)
            {
                entries.Add(RouteSceneCompositionResultEntry.FailedEntry(
                    plan.PrimaryScene,
                    true,
                    primarySceneLoadResult.Message));
                AddNotExecutedAdditionalScenes(
                    plan,
                    entries,
                    "Additive scene composition skipped because Primary Scene failed.");

                return RouteSceneCompositionResult.ExecutedResult(
                    plan,
                    entries,
                    primarySceneLoadResult,
                    plan.Source,
                    plan.Reason);
            }

            entries.Add(RouteSceneCompositionResultEntry.LoadedEntry(
                plan.PrimaryScene,
                primarySceneLoadResult.AlreadyLoaded,
                plan.ActiveScenePolicy == RouteSceneActiveScenePolicy.PrimarySceneActive,
                primarySceneLoadResult.Message));

            for (var i = 0; i < plan.AdditionalScenes.Count; i++)
            {
                var additionalScene = plan.AdditionalScenes[i];
                if (!additionalScene.IsExecutionReady)
                {
                    entries.Add(CreateNotReadyEntry(additionalScene));
                    continue;
                }

                var additiveLoadResult = await _sceneLifecycleRuntime.LoadAdditiveSceneAsync(
                    additionalScene.SceneName,
                    additionalScene.ScenePath,
                    progressReporter);

                if (additiveLoadResult.Loaded)
                {
                    entries.Add(RouteSceneCompositionResultEntry.LoadedEntry(
                        additionalScene,
                        additiveLoadResult.AlreadyLoaded,
                        false,
                        additiveLoadResult.Message));
                    continue;
                }

                entries.Add(RouteSceneCompositionResultEntry.FailedEntry(
                    additionalScene,
                    additionalScene.Requiredness == FrameworkContentRequiredness.Required,
                    additiveLoadResult.Message));
            }

            return RouteSceneCompositionResult.ExecutedResult(
                plan,
                entries,
                primarySceneLoadResult,
                plan.Source,
                plan.Reason);
        }

        private static void AddNotExecutedAdditionalScenes(
            RouteSceneCompositionPlan plan,
            ICollection<RouteSceneCompositionResultEntry> entries,
            string message)
        {
            for (var i = 0; i < plan.AdditionalScenes.Count; i++)
            {
                entries.Add(RouteSceneCompositionResultEntry.NotExecutedEntry(plan.AdditionalScenes[i], message));
            }
        }

        private static RouteSceneCompositionResultEntry CreateNotReadyEntry(RouteSceneCompositionPlanEntry additionalScene)
        {
            var blocksComposition = additionalScene.Requiredness == FrameworkContentRequiredness.Required;
            var message = blocksComposition
                ? "Required additive scene declaration is not execution-ready. Scene, explicit content id and content identity are required."
                : "Optional additive scene declaration skipped because it is not execution-ready.";

            if (blocksComposition)
            {
                return RouteSceneCompositionResultEntry.FailedEntry(additionalScene, true, message);
            }

            return RouteSceneCompositionResultEntry.SkippedEntry(additionalScene, false, message);
        }
    }
}

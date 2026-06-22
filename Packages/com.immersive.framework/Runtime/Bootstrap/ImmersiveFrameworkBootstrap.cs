using System;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Logging.Records;

namespace Immersive.Framework.Bootstrap
{
    /// <summary>
    /// Internal runtime bootstrap for the Immersive Framework.
    /// It resolves and validates the active Game Application, then hands off to the first lifecycle owner.
    /// Activity, Actor, Input, Camera, Save and Pooling lifecycles are not owned here.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal static class ImmersiveFrameworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async void BootAfterSceneLoad()
        {
            var logger = FrameworkLogger.Create(typeof(ImmersiveFrameworkBootstrap));

            try
            {
                var settings = LoadSettings();

#if UNITY_EDITOR
                if (ShouldSkipFrameworkStartupInEditor(settings))
                {
                    logger.Info("Boot skipped.", LogFields.Field("editorPlayModeStartup", "CurrentSceneOnly"));
                    return;
                }
#endif

                var result = FrameworkBootValidator.Validate(settings);

                if (!result.Succeeded)
                {
                    logger.Error("Boot failed.", LogFields.Field("reason", result.Message));
                    return;
                }

                var runtimeHost = FrameworkRuntimeHost.Create(result.GameApplication);
                var gameFlowResult = await runtimeHost.StartAsync();
                if (!gameFlowResult.Started)
                {
                    logger.Error("Game Flow failed.", LogFields.Field("reason", gameFlowResult.Message));
                    return;
                }

                logger.Info(
                    "Boot succeeded. Application Runtime started.",
                    BuildBootFields(result, gameFlowResult));
                logger.Debug("Boot diagnostics. " + gameFlowResult.Message);
                LogActivityContentObservability(logger, gameFlowResult.RouteLifecycleResult.ActivityFlowResult.ActivityContentResult);
            }
            catch (Exception exception)
            {
                logger.Error("Boot failed.", exception);
            }
        }

        internal static FrameworkBootResult Boot()
        {
            return FrameworkBootValidator.Validate(LoadSettings());
        }

        private static LogField[] BuildBootFields(
            FrameworkBootResult result,
            FrameworkGameFlowStartResult gameFlowResult)
        {
            RouteLifecycleStartResult routeLifecycleResult = gameFlowResult.RouteLifecycleResult;
            ActivityFlowStartResult activityFlowResult = routeLifecycleResult.ActivityFlowResult;
            ActivityContentApplyResult activityContentResult = activityFlowResult.ActivityContentResult;

            return LogFields.Of(
                LogFields.Field("gameApplication", result.GameApplication != null ? result.GameApplication.ApplicationName : null),
                LogFields.Field("startupRoute", result.StartupRoute != null ? result.StartupRoute.RouteName : null),
                LogFields.Field("primaryScene", result.StartupRoute != null ? result.StartupRoute.PrimarySceneName : null),
                LogFields.Field("validationMode", result.ValidationMode),
                LogFields.Field("alreadyLoaded", routeLifecycleResult.SceneLifecycleResult.AlreadyLoaded),
                LogFields.Field("loadMode", routeLifecycleResult.SceneLifecycleResult.LoadMode),
                LogFields.Field("routeSceneComposition", routeLifecycleResult.RouteSceneCompositionResult.Status),
                LogFields.Field("routeSceneLoaded", routeLifecycleResult.RouteSceneCompositionResult.LoadedCount),
                LogFields.Field("routeSceneFailed", routeLifecycleResult.RouteSceneCompositionResult.FailedCount),
                LogFields.Field("routeSceneBlockingIssues", routeLifecycleResult.RouteSceneCompositionResult.BlockingIssueCount),
                LogFields.Field("routeContentHandles", routeLifecycleResult.RouteContentSet.Count),
                LogFields.Field("routeContentEnterReceivers", routeLifecycleResult.RouteContentEnterResult.ReceiverCount),
                LogFields.Field("activity", FormatDiagnosticValue(activityFlowResult.ActivityState.ActivityName)),
                LogFields.Field("activityState", activityFlowResult.ActivityState.DiagnosticStatus),
                LogFields.Field("activityReadiness", activityFlowResult.ActivityReadinessState.DiagnosticStatus),
                LogFields.Field("activityContentHandles", activityContentResult.ActivityContentCount));
        }

        private static string FormatDiagnosticValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }

        private static void LogActivityContentObservability(FrameworkLogger logger, ActivityContentApplyResult activityContentResult)
        {
            if (activityContentResult.HasDetailMessage)
            {
                logger.Debug(activityContentResult.DetailMessage);
            }

            if (activityContentResult.HasWarningMessage)
            {
                logger.Warning(activityContentResult.WarningMessage);
            }
        }

        private static ImmersiveFrameworkSettingsAsset LoadSettings()
        {
            return Resources.Load<ImmersiveFrameworkSettingsAsset>(ImmersiveFrameworkSettingsAsset.ResourcesPath);
        }

#if UNITY_EDITOR
        private static bool ShouldSkipFrameworkStartupInEditor(ImmersiveFrameworkSettingsAsset settings)
        {
            return settings != null &&
                   settings.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.CurrentSceneOnly;
        }
#endif
    }
}

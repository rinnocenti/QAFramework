using System;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using UnityEngine;

namespace Immersive.Framework.Bootstrap
{
    /// <summary>
    /// Internal runtime bootstrap for the Immersive Framework.
    /// It resolves and validates the active Game Application, then hands off to the first lifecycle owner.
    /// Activity, Actor, Input, Camera, Save and Pooling lifecycles are not owned here.
    /// </summary>
    internal static class ImmersiveFrameworkBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static async void BootAfterSceneLoad()
        {
            var logger = FrameworkLogger.Create();

            try
            {
                var settings = LoadSettings();

#if UNITY_EDITOR
                if (ShouldSkipFrameworkStartupInEditor(settings))
                {
                    logger.Info("Boot skipped. Editor Play Mode Startup is set to Current Scene Only.");
                    return;
                }
#endif

                var result = FrameworkBootValidator.Validate(settings);

                if (!result.Succeeded)
                {
                    logger.Error($"Boot failed. {result.Message}");
                    return;
                }

                var runtimeHost = FrameworkRuntimeHost.Create(result.GameApplication);
                var gameFlowResult = await runtimeHost.StartAsync();
                if (!gameFlowResult.Started)
                {
                    logger.Error($"Game Flow failed. {gameFlowResult.Message}");
                    return;
                }

                logger.Info($"Boot succeeded. Application Runtime started. {result.Message} {gameFlowResult.Message} Validation Mode: {result.ValidationMode}.");
            }
            catch (Exception exception)
            {
                logger.Error($"Boot failed. {exception.GetType().Name}: {exception.Message}");
            }
        }

        internal static FrameworkBootResult Boot()
        {
            return FrameworkBootValidator.Validate(LoadSettings());
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

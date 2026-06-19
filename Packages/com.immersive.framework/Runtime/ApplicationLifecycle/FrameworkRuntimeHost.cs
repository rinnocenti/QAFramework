using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace Immersive.Framework.ApplicationLifecycle
{
    /// <summary>
    /// Minimal persistent application-scope runtime owner for the framework.
    /// It owns the Game Flow instance for this boot, but does not expose a global service locator.
    /// </summary>
    internal sealed class FrameworkRuntimeHost : MonoBehaviour
    {
        private const string RuntimeHostName = "Immersive Framework Runtime";

        private static FrameworkRuntimeHost current;

        private GameApplicationAsset gameApplication;
        private GameFlowRuntime gameFlowRuntime;
        private FrameworkRuntimeState state;
        private FrameworkLogger logger;

        public FrameworkRuntimeState State => state;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            current = null;
        }

        internal static FrameworkRuntimeHost Create(GameApplicationAsset gameApplication)
        {
            if (current != null)
            {
                UnityEngine.Object.Destroy(current.gameObject);
                current = null;
            }

            var runtimeObject = new GameObject(RuntimeHostName);
            UnityEngine.Object.DontDestroyOnLoad(runtimeObject);

            var host = runtimeObject.AddComponent<FrameworkRuntimeHost>();
            host.Initialize(gameApplication);
            current = host;
            return host;
        }

        internal static bool TryGetCurrent(out FrameworkRuntimeHost runtimeHost)
        {
            runtimeHost = current;
            return runtimeHost != null;
        }

        internal async Task<FrameworkGameFlowStartResult> StartAsync()
        {
            var result = await gameFlowRuntime.StartAsync(gameApplication);
            state = FrameworkRuntimeState.FromGameFlowResult(gameApplication, result);
            return result;
        }

        internal async Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            var result = await gameFlowRuntime.RequestRouteAsync(targetRoute, source, reason);
            if (result.Succeeded)
            {
                state = FrameworkRuntimeState.FromRouteRequestResult(gameApplication, result, true);
            }

            LogRouteRequestResult(result);
            return result;
        }

        private void Initialize(GameApplicationAsset application)
        {
            gameApplication = application;
            gameFlowRuntime = new GameFlowRuntime();
            logger = FrameworkLogger.Create();
            state = FrameworkRuntimeState.Empty(application);
        }

        private void LogRouteRequestResult(FrameworkRouteRequestResult result)
        {
            if (result.Succeeded)
            {
                logger.Info(result.Message);
                return;
            }

            if (result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive ||
                result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyInFlight)
            {
                logger.Warning(result.Message);
                return;
            }

            logger.Error(result.Message);
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }
        }
    }
}

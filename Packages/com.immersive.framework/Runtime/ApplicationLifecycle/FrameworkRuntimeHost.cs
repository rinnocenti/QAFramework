using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.SessionLifecycle;
using UnityEngine;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ApplicationLifecycle
{
    /// <summary>
    /// Minimal persistent application-scope runtime owner for the framework.
    /// It owns the Game Flow instance for this boot, but does not expose a global service locator.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class FrameworkRuntimeHost : MonoBehaviour
    {
        private const string RuntimeHostName = "Immersive Framework Runtime";

        private static FrameworkRuntimeHost _current;

        private GameApplicationAsset _gameApplication;
        private GameFlowRuntime _gameFlowRuntime;
        private FrameworkRuntimeState _state;
        private FrameworkLogger _logger;

        public FrameworkRuntimeState State => _state;

        public SessionRuntimeState SessionState => _state.SessionState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _current = null;
        }

        internal static FrameworkRuntimeHost Create(GameApplicationAsset gameApplication)
        {
            if (_current != null)
            {
                UnityEngine.Object.Destroy(_current.gameObject);
                _current = null;
            }

            var runtimeObject = new GameObject(RuntimeHostName);
            UnityEngine.Object.DontDestroyOnLoad(runtimeObject);

            var host = runtimeObject.AddComponent<FrameworkRuntimeHost>();
            host.Initialize(gameApplication);
            _current = host;
            return host;
        }

        internal static bool TryGetCurrent(out FrameworkRuntimeHost runtimeHost)
        {
            runtimeHost = _current;
            return runtimeHost != null;
        }

        internal async Task<FrameworkGameFlowStartResult> StartAsync()
        {
            var result = await _gameFlowRuntime.StartAsync(_gameApplication);
            _state = FrameworkRuntimeState.FromGameFlowResult(_gameApplication, result);
            return result;
        }

        internal async Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            var result = await _gameFlowRuntime.RequestRouteAsync(targetRoute, source, reason);
            if (result.Succeeded)
            {
                _state = FrameworkRuntimeState.FromRouteRequestResult(_gameApplication, result, true);
            }

            LogRouteRequestResult(result);
            return result;
        }

        internal async Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            var result = await _gameFlowRuntime.RequestActivityAsync(targetActivity, source, reason);
            if (result.Succeeded)
            {
                _state = FrameworkRuntimeState.FromActivityRequestResult(_state, result);
            }

            LogActivityRequestResult(result);
            return result;
        }

        internal async Task<FrameworkActivityRequestResult> ClearActivityAsync(string source, string reason)
        {
            var result = await _gameFlowRuntime.ClearActivityAsync(source, reason);
            if (result.Succeeded)
            {
                _state = FrameworkRuntimeState.FromActivityRequestResult(_state, result);
            }

            LogActivityRequestResult(result);
            return result;
        }

        private void Initialize(GameApplicationAsset application)
        {
            _gameApplication = application;
            _gameFlowRuntime = new GameFlowRuntime();
            _logger = FrameworkLogger.Create();
            _state = FrameworkRuntimeState.Empty(application);
        }

        private void LogRouteRequestResult(FrameworkRouteRequestResult result)
        {
            if (result.Succeeded)
            {
                _logger.Info(result.Message);
                LogActivityContentObservability(result.RouteLifecycleResult.ActivityFlowResult.ActivityContentResult);
                return;
            }

            if (result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive ||
                result.Kind == FrameworkRouteRequestKind.IgnoredAlreadyInFlight)
            {
                _logger.Warning(result.Message);
                return;
            }

            _logger.Error(result.Message);
        }

        private void LogActivityRequestResult(FrameworkActivityRequestResult result)
        {
            if (result.Succeeded)
            {
                _logger.Info(result.Message);
                LogActivityContentObservability(result.ActivityFlowResult.ActivityContentResult);
                return;
            }

            if (result.Kind == FrameworkActivityRequestKind.IgnoredAlreadyActive ||
                result.Kind == FrameworkActivityRequestKind.IgnoredAlreadyInFlight ||
                result.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity)
            {
                _logger.Warning(result.Message);
                return;
            }

            _logger.Error(result.Message);
        }

        private void LogActivityContentObservability(ActivityContentApplyResult activityContentResult)
        {
            if (activityContentResult.HasDetailMessage)
            {
                _logger.Info(activityContentResult.DetailMessage);
            }

            if (activityContentResult.HasWarningMessage)
            {
                _logger.Warning(activityContentResult.WarningMessage);
            }
        }

        private void OnDestroy()
        {
            if (_current == this)
            {
                _current = null;
            }
        }
    }
}

using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SessionLifecycle;
using UnityEngine;
using Immersive.Framework.ApiStatus;
using Immersive.Logging.Records;

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
                Destroy(_current.gameObject);
                _current = null;
            }

            var runtimeObject = new GameObject(RuntimeHostName);
            DontDestroyOnLoad(runtimeObject);

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
                _state = FrameworkRuntimeState.FromRouteRequestResult(_state, result, true);
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
            _logger = FrameworkLogger.Create<FrameworkRuntimeHost>();
            _state = FrameworkRuntimeState.Empty(application);
        }

        private void LogRouteRequestResult(FrameworkRouteRequestResult result)
        {
            if (result.Succeeded)
            {
                _logger.Info("Route Request completed.", BuildRouteRequestFields(result));
                _logger.Debug("Route Request diagnostics. " + result.Message);
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
                _logger.Info("Activity Request completed.", BuildActivityRequestFields(result));
                _logger.Debug("Activity Request diagnostics. " + result.Message);
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

        private static LogField[] BuildRouteRequestFields(FrameworkRouteRequestResult result)
        {
            RouteLifecycleStartResult routeLifecycle = result.RouteLifecycleResult;
            ActivityFlowStartResult activityFlow = routeLifecycle.ActivityFlowResult;
            ActivityContentApplyResult activityContent = activityFlow.ActivityContentResult;

            return LogFields.Of(
                LogFields.Field("kind", result.Kind),
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("previousRoute", GetRouteName(routeLifecycle.PreviousRoute)),
                LogFields.Field("targetRoute", GetRouteName(result.TargetRoute)),
                LogFields.Field("scene", routeLifecycle.SceneLifecycleResult.SceneName),
                LogFields.Field("alreadyLoaded", routeLifecycle.SceneLifecycleResult.AlreadyLoaded),
                LogFields.Field("loadMode", routeLifecycle.SceneLifecycleResult.LoadMode),
                LogFields.Field("routeSceneComposition", routeLifecycle.RouteSceneCompositionResult.Status),
                LogFields.Field("routeSceneLoaded", routeLifecycle.RouteSceneCompositionResult.LoadedCount),
                LogFields.Field("routeSceneFailed", routeLifecycle.RouteSceneCompositionResult.FailedCount),
                LogFields.Field("routeSceneBlockingIssues", routeLifecycle.RouteSceneCompositionResult.BlockingIssueCount),
                LogFields.Field("routeRelease", routeLifecycle.ContentReleaseResult.Status),
                LogFields.Field("routeReleaseReleased", routeLifecycle.ContentReleaseResult.ReleasedCount),
                LogFields.Field("routeReleaseSkipped", routeLifecycle.ContentReleaseResult.SkippedCount),
                LogFields.Field("routeReleaseFailed", routeLifecycle.ContentReleaseResult.FailedCount),
                LogFields.Field("routeReleaseBlockingIssues", routeLifecycle.ContentReleaseResult.BlockingIssueCount),
                LogFields.Field("routeExit", routeLifecycle.RouteExitResult.DiagnosticStatus),
                LogFields.Field("routeContentHandles", routeLifecycle.RouteContentSet.Count),
                LogFields.Field("routeContentEnterReceivers", routeLifecycle.RouteContentEnterResult.ReceiverCount),
                LogFields.Field("routeContentExitReceivers", routeLifecycle.RouteContentExitResult.ReceiverCount),
                LogFields.Field("activity", FormatDiagnosticValue(activityFlow.ActivityState.ActivityName)),
                LogFields.Field("activityState", activityFlow.ActivityState.DiagnosticStatus),
                LogFields.Field("activityReadiness", activityFlow.ActivityReadinessState.DiagnosticStatus),
                LogFields.Field("activityContentHandles", activityContent.ActivityContentCount));
        }

        private static LogField[] BuildActivityRequestFields(FrameworkActivityRequestResult result)
        {
            ActivityFlowStartResult activityFlow = result.ActivityFlowResult;
            ActivityContentApplyResult activityContent = activityFlow.ActivityContentResult;
            ActivityContentLifecycleResult lifecycle = activityContent.LifecycleResult;

            return LogFields.Of(
                LogFields.Field("kind", result.Kind),
                LogFields.Field("source", result.Source),
                LogFields.Field("reason", result.Reason),
                LogFields.Field("targetActivity", GetActivityName(result.TargetActivity)),
                LogFields.Field("previousActivity", GetActivityName(activityFlow.PreviousActivity)),
                LogFields.Field("activity", FormatDiagnosticValue(activityFlow.ActivityState.ActivityName)),
                LogFields.Field("activityState", activityFlow.ActivityState.DiagnosticStatus),
                LogFields.Field("activityReadiness", activityFlow.ActivityReadinessState.DiagnosticStatus),
                LogFields.Field("activityReadinessReason", activityFlow.ActivityReadinessState.DiagnosticReason),
                LogFields.Field("activityReadinessIssues", activityFlow.ActivityReadinessState.BlockingIssueCount),
                LogFields.Field("activityContentBindings", activityContent.BindingCount),
                LogFields.Field("activityContentHandles", activityContent.ActivityContentCount),
                LogFields.Field("activityContentLifecycle", lifecycle.DiagnosticStatus),
                LogFields.Field("activityContentEnterFailed", lifecycle.EnterFailedReceiverCount),
                LogFields.Field("activityContentExitFailed", lifecycle.ExitFailedReceiverCount));
        }

        private void LogActivityContentObservability(ActivityContentApplyResult activityContentResult)
        {
            if (activityContentResult.HasDetailMessage)
            {
                _logger.Debug(activityContentResult.DetailMessage);
            }

            if (activityContentResult.HasWarningMessage)
            {
                _logger.Warning(activityContentResult.WarningMessage);
            }
        }

        private static string GetRouteName(RouteAsset route)
        {
            return route != null ? FormatDiagnosticValue(route.RouteName) : "<none>";
        }

        private static string GetActivityName(ActivityAsset activity)
        {
            return activity != null ? FormatDiagnosticValue(activity.ActivityName) : "<none>";
        }

        private static string FormatDiagnosticValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
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

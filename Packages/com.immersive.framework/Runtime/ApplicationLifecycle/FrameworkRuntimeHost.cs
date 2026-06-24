using System;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SessionLifecycle;
using Immersive.Framework.RuntimeContent;
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
        private RuntimeContentRuntime _runtimeContentRuntime;
        private RuntimeContentAnchorBinding _contentAnchorBindingRuntime;
        private RuntimeScopeLifecycleResult _runtimeSessionScopeResult;
        private FrameworkRuntimeState _state;
        private FrameworkLogger _logger;

        public FrameworkRuntimeState State => _state;

        public SessionRuntimeState SessionState => _state.SessionState;

        internal RuntimeContentRuntime RuntimeContentRuntime => _runtimeContentRuntime;

        internal void SetActivityContentExecutionParticipantSource(IActivityContentExecutionParticipantSource participantSource)
        {
            _gameFlowRuntime?.SetActivityContentExecutionParticipantSource(participantSource);
        }

        internal int ContentAnchorBindingCount => _contentAnchorBindingRuntime != null ? _contentAnchorBindingRuntime.BindingCount : 0;


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
            _runtimeContentRuntime = new RuntimeContentRuntime();
            _contentAnchorBindingRuntime = new RuntimeContentAnchorBinding();
            _runtimeSessionScopeResult = CreateSessionScopeRoot(application, "FrameworkRuntimeHost", "session-start");
            _gameFlowRuntime = new GameFlowRuntime(_runtimeContentRuntime, _contentAnchorBindingRuntime);
            _logger = FrameworkLogger.Create<FrameworkRuntimeHost>();
            _state = FrameworkRuntimeState.Empty(application);
        }


        internal ContentAnchorBindingResult BindContentAnchor(
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest request,
            string source,
            string reason)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            if (_runtimeContentRuntime == null)
            {
                throw new InvalidOperationException("Runtime content runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.Bind(
                anchorSet,
                _runtimeContentRuntime,
                request,
                source,
                reason);
        }

        internal bool UnbindContentAnchor(ContentAnchorBindingRequest request)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.Unbind(request);
        }

        internal bool UnbindContentAnchor(ContentAnchorContentHandle handle)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.Unbind(handle);
        }

        internal ContentAnchorBindingLifecycleResult UnbindContentAnchorRuntimeContent(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.UnbindRuntimeContent(identity, source, reason);
        }

        internal ContentAnchorBindingLifecycleResult UnbindContentAnchorRuntimeOwner(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.UnbindRuntimeOwner(owner, source, reason);
        }

        internal ContentAnchorBindingLifecycleResult UnbindContentAnchorRuntimeScope(
            RuntimeContentScope scope,
            string source,
            string reason)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                throw new InvalidOperationException("Content Anchor binding runtime is not initialized.");
            }

            return _contentAnchorBindingRuntime.UnbindRuntimeScope(scope, source, reason);
        }

        internal ContentAnchorContentHandle[] SnapshotContentAnchorBindings()
        {
            if (_contentAnchorBindingRuntime == null)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            return _contentAnchorBindingRuntime.SnapshotBindings();
        }

        internal ContentAnchorContentHandle[] SnapshotContentAnchorBindingsForRuntimeContent(RuntimeContentIdentity identity)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            return _contentAnchorBindingRuntime.SnapshotBindingsForRuntimeContent(identity);
        }

        internal ContentAnchorContentHandle[] SnapshotContentAnchorBindingsForRuntimeOwner(RuntimeContentOwner owner)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            return _contentAnchorBindingRuntime.SnapshotBindingsForRuntimeOwner(owner);
        }

        internal ContentAnchorContentHandle[] SnapshotContentAnchorBindingsForRuntimeScope(RuntimeContentScope scope)
        {
            if (_contentAnchorBindingRuntime == null)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            return _contentAnchorBindingRuntime.SnapshotBindingsForRuntimeScope(scope);
        }


        private RuntimeScopeLifecycleResult CreateSessionScopeRoot(GameApplicationAsset application, string source, string reason)
        {
            var owner = RuntimeContentOwner.Session(application.ApplicationName, application.ApplicationName);
            var enterResult = _runtimeContentRuntime.CreateScopeRoot(owner, source, reason);
            _runtimeContentRuntime.TryCreateScopeContext(owner, source, reason, out var context);

            return new RuntimeScopeLifecycleResult(
                RuntimeContentScope.Session,
                owner,
                enterResult,
                null,
                context,
                _runtimeContentRuntime.RootCount,
                source,
                reason);
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

        private LogField[] BuildRouteRequestFields(FrameworkRouteRequestResult result)
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
                LogFields.Field("runtimeRouteScope", routeLifecycle.RuntimeRouteScopeResult.DiagnosticStatus),
                LogFields.Field("runtimeRouteRootEnter", routeLifecycle.RuntimeRouteScopeResult.EnterStatus),
                LogFields.Field("runtimeRouteRootExit", routeLifecycle.RuntimeRouteScopeResult.ExitStatus),
                LogFields.Field("runtimeRouteContext", routeLifecycle.RuntimeRouteScopeResult.ContextStatus),
                LogFields.Field("runtimeRootCount", routeLifecycle.RuntimeRouteScopeResult.RootCount),
                LogFields.Field("routeContentHandles", routeLifecycle.RouteContentSet.Count),
                LogFields.Field("contentAnchors", routeLifecycle.ContentAnchorDiscoveryResult.AnchorCount),
                LogFields.Field("contentAnchorCandidates", routeLifecycle.ContentAnchorDiscoveryResult.CandidateCount),
                LogFields.Field("contentAnchorIssues", routeLifecycle.ContentAnchorDiscoveryResult.IssueCount),
                LogFields.Field("contentAnchorInvalid", routeLifecycle.ContentAnchorDiscoveryResult.InvalidAuthoringCount),
                LogFields.Field("contentAnchorRouteMismatch", routeLifecycle.ContentAnchorDiscoveryResult.SkippedRouteMismatchCount),
                LogFields.Field("contentAnchorBindings", ContentAnchorBindingCount),
                LogFields.Field("routeContentAnchorBindingCleanup", routeLifecycle.RouteContentAnchorBindingCleanupResult.DiagnosticStatus),
                LogFields.Field("routeContentAnchorBindingCleanupRemoved", routeLifecycle.RouteContentAnchorBindingCleanupResult.RemovedCount),
                LogFields.Field("activityContentAnchorBindingCleanup", activityFlow.ActivityContentAnchorBindingCleanupResult.DiagnosticStatus),
                LogFields.Field("activityContentAnchorBindingCleanupRemoved", activityFlow.ActivityContentAnchorBindingCleanupResult.RemovedCount),
                LogFields.Field("activityContentExecution", activityFlow.ActivityContentExecutionResult.DiagnosticStatus),
                LogFields.Field("activityContentExecutionParticipantSource", activityFlow.ActivityContentExecutionResult.ParticipantSourceStatus),
                LogFields.Field("activityContentExecutionParticipantSourceIssues", activityFlow.ActivityContentExecutionResult.ParticipantSourceIssueCount),
                LogFields.Field("activityContentExecutionParticipants", activityFlow.ActivityContentExecutionResult.ParticipantCount),
                LogFields.Field("activityContentExecutionEnter", activityFlow.ActivityContentExecutionResult.EnterResult.Status),
                LogFields.Field("activityContentExecutionEnterRequests", activityFlow.ActivityContentExecutionResult.EnterRequestCount),
                LogFields.Field("activityContentExecutionExit", activityFlow.ActivityContentExecutionResult.ExitResult.Status),
                LogFields.Field("activityContentExecutionExitRequests", activityFlow.ActivityContentExecutionResult.ExitRequestCount),
                LogFields.Field("activityContentExecutionBlockingIssues", activityFlow.ActivityContentExecutionResult.BlockingIssueCount),
                LogFields.Field("activityContentExecutionBlocksReadiness", activityFlow.ActivityContentExecutionResult.BlocksReadiness),
                LogFields.Field("activityContentAnchors", activityFlow.ActivityContentAnchorDiscoveryResult.AnchorCount),
                LogFields.Field("activityContentAnchorCandidates", activityFlow.ActivityContentAnchorDiscoveryResult.CandidateCount),
                LogFields.Field("activityContentAnchorIssues", activityFlow.ActivityContentAnchorDiscoveryResult.IssueCount),
                LogFields.Field("activityContentAnchorInvalid", activityFlow.ActivityContentAnchorDiscoveryResult.InvalidAuthoringCount),
                LogFields.Field("activityContentAnchorActivityMismatch", activityFlow.ActivityContentAnchorDiscoveryResult.SkippedActivityMismatchCount),
                LogFields.Field("routeContentEnterReceivers", routeLifecycle.RouteContentEnterResult.ReceiverCount),
                LogFields.Field("routeContentExitReceivers", routeLifecycle.RouteContentExitResult.ReceiverCount),
                LogFields.Field("activity", FormatDiagnosticValue(activityFlow.ActivityState.ActivityName)),
                LogFields.Field("activityState", activityFlow.ActivityState.DiagnosticStatus),
                LogFields.Field("activityReadiness", activityFlow.ActivityReadinessState.DiagnosticStatus),
                LogFields.Field("runtimeActivityScope", activityFlow.RuntimeActivityScopeResult.DiagnosticStatus),
                LogFields.Field("runtimeActivityRootEnter", activityFlow.RuntimeActivityScopeResult.EnterStatus),
                LogFields.Field("runtimeActivityRootExit", activityFlow.RuntimeActivityScopeResult.ExitStatus),
                LogFields.Field("runtimeActivityContext", activityFlow.RuntimeActivityScopeResult.ContextStatus),
                LogFields.Field("activityContentHandles", activityContent.ActivityContentCount));
        }

        private LogField[] BuildActivityRequestFields(FrameworkActivityRequestResult result)
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
                LogFields.Field("runtimeActivityScope", activityFlow.RuntimeActivityScopeResult.DiagnosticStatus),
                LogFields.Field("runtimeActivityRootEnter", activityFlow.RuntimeActivityScopeResult.EnterStatus),
                LogFields.Field("runtimeActivityRootExit", activityFlow.RuntimeActivityScopeResult.ExitStatus),
                LogFields.Field("runtimeActivityContext", activityFlow.RuntimeActivityScopeResult.ContextStatus),
                LogFields.Field("runtimeRootCount", activityFlow.RuntimeActivityScopeResult.RootCount),
                LogFields.Field("activityReadinessIssues", activityFlow.ActivityReadinessState.BlockingIssueCount),
                LogFields.Field("activityContentBindings", activityContent.BindingCount),
                LogFields.Field("contentAnchorBindings", ContentAnchorBindingCount),
                LogFields.Field("activityContentAnchors", activityFlow.ActivityContentAnchorDiscoveryResult.AnchorCount),
                LogFields.Field("activityContentAnchorCandidates", activityFlow.ActivityContentAnchorDiscoveryResult.CandidateCount),
                LogFields.Field("activityContentAnchorIssues", activityFlow.ActivityContentAnchorDiscoveryResult.IssueCount),
                LogFields.Field("activityContentAnchorInvalid", activityFlow.ActivityContentAnchorDiscoveryResult.InvalidAuthoringCount),
                LogFields.Field("activityContentAnchorActivityMismatch", activityFlow.ActivityContentAnchorDiscoveryResult.SkippedActivityMismatchCount),
                LogFields.Field("activityContentAnchorBindingCleanup", activityFlow.ActivityContentAnchorBindingCleanupResult.DiagnosticStatus),
                LogFields.Field("activityContentAnchorBindingCleanupRemoved", activityFlow.ActivityContentAnchorBindingCleanupResult.RemovedCount),
                LogFields.Field("activityContentExecution", activityFlow.ActivityContentExecutionResult.DiagnosticStatus),
                LogFields.Field("activityContentExecutionParticipantSource", activityFlow.ActivityContentExecutionResult.ParticipantSourceStatus),
                LogFields.Field("activityContentExecutionParticipantSourceIssues", activityFlow.ActivityContentExecutionResult.ParticipantSourceIssueCount),
                LogFields.Field("activityContentExecutionParticipants", activityFlow.ActivityContentExecutionResult.ParticipantCount),
                LogFields.Field("activityContentExecutionEnter", activityFlow.ActivityContentExecutionResult.EnterResult.Status),
                LogFields.Field("activityContentExecutionEnterRequests", activityFlow.ActivityContentExecutionResult.EnterRequestCount),
                LogFields.Field("activityContentExecutionExit", activityFlow.ActivityContentExecutionResult.ExitResult.Status),
                LogFields.Field("activityContentExecutionExitRequests", activityFlow.ActivityContentExecutionResult.ExitRequestCount),
                LogFields.Field("activityContentExecutionBlockingIssues", activityFlow.ActivityContentExecutionResult.BlockingIssueCount),
                LogFields.Field("activityContentExecutionBlocksReadiness", activityFlow.ActivityContentExecutionResult.BlocksReadiness),
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

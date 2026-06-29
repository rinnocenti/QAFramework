using System;
using Immersive.Framework.Actors;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Scene-authored, opt-in bridge that submits a logical Pause request to the framework runtime
    /// and, only after a safe preflight, applies the resulting InputMode to one explicit Unity PlayerInput.
    /// It does not register itself in FrameworkRuntimeHost, own PauseRuntime, own PlayerInputManager, call JoinPlayer, spawn players,
    /// read gameplay commands or create a custom input manager.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Input Mode/Pause PlayerInput Runtime Bridge")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F33A opt-in Pause runtime to Unity PlayerInput wiring bridge.")]
    public sealed class PauseInputModeUnityPlayerInputRuntimeBridge : MonoBehaviour
    {
        private const string DefaultSource = nameof(PauseInputModeUnityPlayerInputRuntimeBridge);

        private FrameworkLogger _logger;
        private int _requestSequence;
        private PauseInputModeUnityPlayerInputRuntimeBridgeResult _lastResult;

        [Header("Unity PlayerInput")]
        [SerializeField] private PlayerInput playerInput;

        [Header("Framework References")]
        [SerializeField] private UnityInputTargetDeclaration[] unityInputTargets;
        [SerializeField] private PlayerActorDeclaration[] playerActors;
        [SerializeField] private SessionPlayerInputManagerDeclaration[] sessionPlayerInputManagers;
        [SerializeField] private bool autoDiscoverMissingReferences = true;
        [SerializeField] private bool requireSessionPlayerInputManagerEvidence = true;

        [Header("Action Maps")]
        [SerializeField] private string gameplayActionMapName = "Player";
        [SerializeField] private string uiActionMapName = "UI";

        [Header("Request")]
        [SerializeField] private string reason = "pause.inputmode.playerinput.runtime.bridge";
        [SerializeField] private bool logResults = true;

        public PlayerInput PlayerInput => playerInput;

        public bool AutoDiscoverMissingReferences => autoDiscoverMissingReferences;

        public bool RequireSessionPlayerInputManagerEvidence => requireSessionPlayerInputManagerEvidence;

        public string GameplayActionMapName => gameplayActionMapName.NormalizeTextOrFallback("Player");

        public string UiActionMapName => uiActionMapName.NormalizeTextOrFallback("UI");

        public PauseInputModeUnityPlayerInputRuntimeBridgeResult LastResult => _lastResult;

        public bool HasLastResult => _lastResult != null;

        public bool LastSucceeded => _lastResult != null && _lastResult.Succeeded;

        public bool LastIgnored => _lastResult != null && _lastResult.Ignored;

        public bool LastFailed => _lastResult != null && _lastResult.Failed;

        private void Awake()
        {
            _logger = FrameworkLogger.Create<PauseInputModeUnityPlayerInputRuntimeBridge>();
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Pause")]
        public void RequestPause()
        {
            Submit(PauseRequestKind.Pause, "pause.inputmode.playerinput.runtime.bridge.pause");
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Resume")]
        public void RequestResume()
        {
            Submit(PauseRequestKind.Resume, "pause.inputmode.playerinput.runtime.bridge.resume");
        }

        [ContextMenu("Immersive Framework/Pause/InputMode PlayerInput Bridge/Toggle")]
        public void TogglePause()
        {
            Submit(PauseRequestKind.Toggle, "pause.inputmode.playerinput.runtime.bridge.toggle");
        }

        private void Submit(PauseRequestKind kind, string fallbackReason)
        {
            _lastResult = SubmitInternal(kind, DefaultSource, ResolveReason(fallbackReason));
            if (logResults && _lastResult != null)
            {
                EnsureLogger();
                _logger.Info(
                    "Pause InputMode PlayerInput Runtime Bridge completed.",
                    Logging.Records.LogFields.Of(
                        Logging.Records.LogFields.Field("status", _lastResult.Status.ToString()),
                        Logging.Records.LogFields.Field("pauseStatus", _lastResult.PauseStatus.ToString()),
                        Logging.Records.LogFields.Field("requestedMode", _lastResult.RequestedMode.ToString()),
                        Logging.Records.LogFields.Field("operation", _lastResult.Operation.ToString()),
                        Logging.Records.LogFields.Field("actionMapSwitching", _lastResult.SwitchesActionMaps),
                        Logging.Records.LogFields.Field("inputBehavior", _lastResult.AppliesInputBehavior),
                        Logging.Records.LogFields.Field("pauseRuntimeWiring", _lastResult.PauseRuntimeWiring),
                        Logging.Records.LogFields.Field("playerJoin", _lastResult.CallsPlayerJoin),
                        Logging.Records.LogFields.Field("actorSpawning", _lastResult.SpawnsActor),
                        Logging.Records.LogFields.Field("diagnostics", _lastResult.ToDiagnosticString())));
            }
        }

        internal PauseInputModeUnityPlayerInputRuntimeBridgeResult SubmitForDiagnostics(
            PauseRequestKind kind,
            string source,
            string requestReason)
        {
            _lastResult = SubmitInternal(kind, source, requestReason);
            return _lastResult;
        }

        internal void ConfigureForDiagnostics(
            PlayerInput input,
            UnityInputTargetDeclaration[] targets,
            PlayerActorDeclaration[] actors,
            SessionPlayerInputManagerDeclaration[] sessionManagers,
            string playerMap,
            string uiMap,
            bool autoDiscover,
            bool requireSessionManager)
        {
            playerInput = input;
            unityInputTargets = targets;
            playerActors = actors;
            sessionPlayerInputManagers = sessionManagers;
            gameplayActionMapName = playerMap.NormalizeTextOrFallback("Player");
            uiActionMapName = uiMap.NormalizeTextOrFallback("UI");
            autoDiscoverMissingReferences = autoDiscover;
            requireSessionPlayerInputManagerEvidence = requireSessionManager;
        }

        private PauseInputModeUnityPlayerInputRuntimeBridgeResult SubmitInternal(
            PauseRequestKind kind,
            string source,
            string requestReason)
        {
            string normalizedSource = source.NormalizeTextOrFallback(DefaultSource);
            string normalizedReason = requestReason.NormalizeTextOrFallback(ResolveReason("pause.inputmode.playerinput.runtime.bridge"));

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedRuntimeUnavailable,
                    kind,
                    PauseState.Unknown,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    normalizedSource,
                    normalizedReason,
                    "FrameworkRuntimeHost is unavailable.");
            }

            if (!runtimeHost.TryGetPauseSnapshot(out PauseSnapshot pauseSnapshot) || pauseSnapshot.State == PauseState.Unknown)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedRuntimeUnavailable,
                    kind,
                    PauseState.Unknown,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    normalizedSource,
                    normalizedReason,
                    "Pause runtime snapshot is unavailable.");
            }

            if (!TryResolveReferences(normalizedSource, normalizedReason, out ResolvedReferences references, out string configurationMessage))
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedConfiguration,
                    kind,
                    pauseSnapshot.State,
                    PauseState.Unknown,
                    default,
                    null,
                    null,
                    normalizedSource,
                    normalizedReason,
                    configurationMessage);
            }

            _requestSequence++;
            string requestId = $"pause.inputmode.playerinput.runtime.bridge.{_requestSequence}";
            PauseRequest request = CreatePauseRequest(kind, requestId, normalizedSource, normalizedReason);
            PauseState targetPauseState = PauseRequest.ResolveTargetState(kind, pauseSnapshot.State);
            PauseResult anticipatedPauseResult = targetPauseState == pauseSnapshot.State
                ? PauseResult.IgnoredNoChangeResult(request, pauseSnapshot.State, "Pause runtime PlayerInput bridge preflight detected no Pause state change.")
                : PauseResult.AppliedResult(request, pauseSnapshot.State, targetPauseState, "Pause runtime PlayerInput bridge preflight detected a Pause state change.");

            InputModeState currentInputModeState = CreateInputModeStateForPauseState(pauseSnapshot.State, normalizedSource, normalizedReason);
            InputModeUnityApplicationPlanResult preflightPlan = BuildPreflightPlan(
                anticipatedPauseResult,
                currentInputModeState,
                references,
                normalizedSource,
                normalizedReason);

            if (preflightPlan != null && preflightPlan.Failed)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPreflight,
                    kind,
                    pauseSnapshot.State,
                    targetPauseState,
                    default,
                    preflightPlan,
                    null,
                    normalizedSource,
                    normalizedReason,
                    "Pause runtime PlayerInput bridge preflight failed before submitting the Pause request.");
            }

            PauseResult pauseResult;
            try
            {
                pauseResult = runtimeHost.RequestPause(request);
            }
            catch (Exception exception)
            {
                return CreateResult(
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPauseRequest,
                    kind,
                    pauseSnapshot.State,
                    targetPauseState,
                    default,
                    preflightPlan,
                    null,
                    normalizedSource,
                    normalizedReason,
                    exception.Message);
            }

            PauseInputModeUnityPlayerInputApplicationResult applicationResult = PauseInputModeUnityPlayerInputApplication.Apply(
                pauseResult,
                currentInputModeState,
                references.TargetSet,
                references.PlayerActorSet,
                references.SessionPlayerInputManagerEvidence,
                references.ActionMapEvidence,
                references.ActionMapBindings,
                references.PlayerInput,
                normalizedSource,
                normalizedReason);

            PauseInputModeUnityPlayerInputRuntimeBridgeStatus status = applicationResult.Succeeded
                ? PauseInputModeUnityPlayerInputRuntimeBridgeStatus.Succeeded
                : applicationResult.Ignored
                    ? PauseInputModeUnityPlayerInputRuntimeBridgeStatus.IgnoredInputModeRequest
                    : PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedInputModePlayerInputApplication;

            return CreateResult(
                status,
                kind,
                pauseSnapshot.State,
                targetPauseState,
                pauseResult,
                preflightPlan,
                applicationResult,
                normalizedSource,
                normalizedReason,
                $"InputMode PlayerInput application {applicationResult.Status}.");
        }

        private InputModeUnityApplicationPlanResult BuildPreflightPlan(
            PauseResult anticipatedPauseResult,
            InputModeState currentInputModeState,
            ResolvedReferences references,
            string source,
            string requestReason)
        {
            InputModeRequest inputModeRequest = PauseInputModeRequestMapper.CreateRequest(
                anticipatedPauseResult,
                source,
                requestReason.NormalizeTextOrFallback("pause-inputmode-playerinput-runtime-bridge-preflight"));

            InputModeRequestResult requestPreview = InputModeRequestEvaluator.Preview(currentInputModeState, inputModeRequest, source);
            if (requestPreview.Ignored)
            {
                return null;
            }

            if (!requestPreview.Succeeded)
            {
                return new InputModeUnityApplicationPlanResult(
                    InputModeUnityApplicationPlanStatus.FailedPreviewMismatch,
                    inputModeRequest.TargetMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    UnityInputTargetRole.Unknown,
                    UnityInputActionMapName.From(string.Empty),
                    false,
                    false,
                    false,
                    false,
                    new[]
                    {
                        InputModeUnityApplicationPlanIssue.BlockingIssue(
                            InputModeUnityApplicationPlanIssueKind.PreviewModeMismatch,
                            inputModeRequest.TargetMode,
                            UnityInputActionMapName.From(string.Empty),
                            source,
                            "InputMode request preflight did not succeed.")
                    },
                    source,
                    requestReason);
            }

            InputModeUnityApplicationPreviewResult applicationPreview = InputModeUnityApplicationPreviewEvaluator.Preview(
                requestPreview,
                references.TargetSet,
                references.PlayerActorSet,
                references.SessionPlayerInputManagerEvidence,
                source,
                requestReason);
            if (!applicationPreview.Succeeded)
            {
                return new InputModeUnityApplicationPlanResult(
                    InputModeUnityApplicationPlanStatus.FailedApplicationPreview,
                    inputModeRequest.TargetMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    applicationPreview.TargetRole,
                    UnityInputActionMapName.From(string.Empty),
                    false,
                    false,
                    applicationPreview.PlayerActorRequired,
                    applicationPreview.SessionPlayerInputManagerRequired,
                    new[]
                    {
                        InputModeUnityApplicationPlanIssue.BlockingIssue(
                            InputModeUnityApplicationPlanIssueKind.ApplicationPreviewNotSucceeded,
                            inputModeRequest.TargetMode,
                            UnityInputActionMapName.From(string.Empty),
                            source,
                            "InputMode Unity application preflight did not succeed.")
                    },
                    source,
                    requestReason);
            }

            InputModeUnityActionMapPreviewResult actionMapPreview = InputModeUnityActionMapPreviewEvaluator.Preview(
                applicationPreview,
                references.ActionMapEvidence,
                references.ActionMapBindings,
                source,
                requestReason);
            if (!actionMapPreview.Succeeded)
            {
                return new InputModeUnityApplicationPlanResult(
                    InputModeUnityApplicationPlanStatus.FailedActionMapPreview,
                    inputModeRequest.TargetMode,
                    InputModeUnityApplicationPlanOperation.NoOperation,
                    applicationPreview.TargetRole,
                    actionMapPreview.ActionMapName,
                    actionMapPreview.ActionMapRequired,
                    actionMapPreview.ActionMapAvailable,
                    applicationPreview.PlayerActorRequired,
                    applicationPreview.SessionPlayerInputManagerRequired,
                    new[]
                    {
                        InputModeUnityApplicationPlanIssue.BlockingIssue(
                            InputModeUnityApplicationPlanIssueKind.ActionMapPreviewNotSucceeded,
                            inputModeRequest.TargetMode,
                            actionMapPreview.ActionMapName,
                            source,
                            "InputMode Unity action map preflight did not succeed.")
                    },
                    source,
                    requestReason);
            }

            return InputModeUnityApplicationPlanEvaluator.BuildPlan(applicationPreview, actionMapPreview, source, requestReason);
        }

        private bool TryResolveReferences(string source, string requestReason, out ResolvedReferences references, out string message)
        {
            references = default;
            message = string.Empty;

            PlayerInput resolvedPlayerInput = ResolvePlayerInput();
            if (resolvedPlayerInput == null)
            {
                message = "Pause InputMode PlayerInput runtime bridge requires an explicit PlayerInput or a PlayerActor with PlayerInput evidence.";
                return false;
            }

            if (resolvedPlayerInput.actions == null)
            {
                message = "Pause InputMode PlayerInput runtime bridge requires PlayerInput.actions before applying InputMode.";
                return false;
            }

            UnityInputTargetSet targetSet = ResolveTargetSet(source, requestReason);
            PlayerActorSet playerActorSet = ResolvePlayerActorSet(source, requestReason);
            UnityInputPlayerInputManagerEvidence sessionEvidence = ResolveSessionPlayerInputManagerEvidence(source, requestReason);

            if (requireSessionPlayerInputManagerEvidence && !sessionEvidence.Succeeded)
            {
                message = sessionEvidence.ToDiagnosticString();
                return false;
            }

            references = new ResolvedReferences(
                resolvedPlayerInput,
                targetSet,
                playerActorSet,
                sessionEvidence,
                UnityInputActionMapEvidence.FromInputActionAsset(resolvedPlayerInput.actions, source, requestReason),
                CreateActionMapBindings(source, requestReason));
            return true;
        }

        private UnityInputTargetSet ResolveTargetSet(string source, string requestReason)
        {
            if (unityInputTargets != null && unityInputTargets.Length > 0)
            {
                return UnityInputTargetValidator.ValidateDeclarations(unityInputTargets, source, requestReason);
            }

            return autoDiscoverMissingReferences
                ? UnityInputTargetValidator.ValidateLoadedSceneDeclarations(source, requestReason)
                : UnityInputTargetValidator.ValidateDeclarations(Array.Empty<UnityInputTargetDeclaration>(), source, requestReason);
        }

        private PlayerActorSet ResolvePlayerActorSet(string source, string requestReason)
        {
            if (playerActors != null && playerActors.Length > 0)
            {
                return PlayerActorValidator.ValidateDeclarations(playerActors, source, requestReason);
            }

            return autoDiscoverMissingReferences
                ? PlayerActorValidator.ValidateLoadedSceneDeclarations(source, requestReason)
                : PlayerActorValidator.ValidateDeclarations(Array.Empty<PlayerActorDeclaration>(), source, requestReason);
        }

        private UnityInputPlayerInputManagerEvidence ResolveSessionPlayerInputManagerEvidence(string source, string requestReason)
        {
            if (!requireSessionPlayerInputManagerEvidence)
            {
                return UnityInputPlayerInputManagerEvidence.FromRequiredSessionManagerCount(1, source, requestReason);
            }

            if (sessionPlayerInputManagers != null && sessionPlayerInputManagers.Length > 0)
            {
                return UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerDeclarations(sessionPlayerInputManagers, source, requestReason);
            }

            return autoDiscoverMissingReferences
                ? UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerEvidence(source, requestReason)
                : UnityInputTargetValidator.ValidateRequiredSessionPlayerInputManagerDeclarations(Array.Empty<SessionPlayerInputManagerDeclaration>(), source, requestReason);
        }

        private PlayerInput ResolvePlayerInput()
        {
            if (playerInput != null)
            {
                return playerInput;
            }

            if (playerActors != null)
            {
                for (int i = 0; i < playerActors.Length; i++)
                {
                    if (playerActors[i] != null && playerActors[i].PlayerInput != null)
                    {
                        return playerActors[i].PlayerInput;
                    }
                }
            }

            if (!autoDiscoverMissingReferences)
            {
                return null;
            }

            PlayerActorDeclaration[] declarations = FindObjectsByType<PlayerActorDeclaration>(FindObjectsInactive.Include);
            for (int i = 0; i < declarations.Length; i++)
            {
                if (declarations[i] != null && declarations[i].PlayerInput != null)
                {
                    return declarations[i].PlayerInput;
                }
            }

            return null;
        }

        private InputModeUnityActionMapBinding[] CreateActionMapBindings(string source, string requestReason)
        {
            return new[]
            {
                new InputModeUnityActionMapBinding(InputModeKind.Gameplay, UnityInputActionMapName.From(GameplayActionMapName), true),
                new InputModeUnityActionMapBinding(InputModeKind.PauseOverlay, UnityInputActionMapName.From(UiActionMapName), true),
                new InputModeUnityActionMapBinding(InputModeKind.FrontendMenu, UnityInputActionMapName.From(UiActionMapName), true),
                new InputModeUnityActionMapBinding(InputModeKind.InputLocked, UnityInputActionMapName.From(string.Empty), false)
            };
        }

        private static PauseRequest CreatePauseRequest(PauseRequestKind kind, string requestId, string source, string requestReason)
        {
            switch (kind)
            {
                case PauseRequestKind.Pause:
                    return PauseRequest.Pause(requestId, source, requestReason);
                case PauseRequestKind.Resume:
                    return PauseRequest.Resume(requestId, source, requestReason);
                case PauseRequestKind.Toggle:
                    return PauseRequest.Toggle(requestId, source, requestReason);
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Pause runtime PlayerInput bridge request kind must be explicit.");
            }
        }

        private static InputModeState CreateInputModeStateForPauseState(PauseState pauseState, string source, string requestReason)
        {
            InputModeKind mode = MapPauseStateToInputMode(pauseState);
            return new InputModeState(
                InputModeDefinitions.FromKind(mode, source, requestReason),
                mode == InputModeKind.Gameplay ? 0 : 1,
                source,
                requestReason);
        }

        private static InputModeKind MapPauseStateToInputMode(PauseState state)
        {
            switch (state)
            {
                case PauseState.Running:
                    return InputModeKind.Gameplay;
                case PauseState.Paused:
                    return InputModeKind.PauseOverlay;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, "Pause runtime PlayerInput bridge requires an explicit Pause state.");
            }
        }

        private PauseInputModeUnityPlayerInputRuntimeBridgeResult CreateResult(
            PauseInputModeUnityPlayerInputRuntimeBridgeStatus status,
            PauseRequestKind kind,
            PauseState previousPauseState,
            PauseState targetPauseState,
            PauseResult pauseResult,
            InputModeUnityApplicationPlanResult preflightPlanResult,
            PauseInputModeUnityPlayerInputApplicationResult applicationResult,
            string source,
            string requestReason,
            string message)
        {
            return new PauseInputModeUnityPlayerInputRuntimeBridgeResult(
                status,
                kind,
                previousPauseState,
                targetPauseState,
                pauseResult,
                preflightPlanResult,
                applicationResult,
                source,
                requestReason,
                message);
        }

        private string ResolveReason(string fallbackReason)
        {
            return reason.NormalizeTextOrFallback(fallbackReason);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<PauseInputModeUnityPlayerInputRuntimeBridge>();
            }
        }

        private readonly struct ResolvedReferences
        {
            internal ResolvedReferences(
                PlayerInput playerInput,
                UnityInputTargetSet targetSet,
                PlayerActorSet playerActorSet,
                UnityInputPlayerInputManagerEvidence sessionPlayerInputManagerEvidence,
                UnityInputActionMapEvidence actionMapEvidence,
                InputModeUnityActionMapBinding[] actionMapBindings)
            {
                PlayerInput = playerInput;
                TargetSet = targetSet;
                PlayerActorSet = playerActorSet;
                SessionPlayerInputManagerEvidence = sessionPlayerInputManagerEvidence;
                ActionMapEvidence = actionMapEvidence;
                ActionMapBindings = actionMapBindings ?? Array.Empty<InputModeUnityActionMapBinding>();
            }

            internal PlayerInput PlayerInput { get; }

            internal UnityInputTargetSet TargetSet { get; }

            internal PlayerActorSet PlayerActorSet { get; }

            internal UnityInputPlayerInputManagerEvidence SessionPlayerInputManagerEvidence { get; }

            internal UnityInputActionMapEvidence ActionMapEvidence { get; }

            internal InputModeUnityActionMapBinding[] ActionMapBindings { get; }
        }
    }
}

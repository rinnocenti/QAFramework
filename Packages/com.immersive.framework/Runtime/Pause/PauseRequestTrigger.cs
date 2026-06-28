using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Public scene-authored request boundary for logical Pause requests.
    /// Designed for UnityEvents/UI Buttons/QA panels to invoke Pause, Resume and Toggle without owning Pause state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Pause/Pause Request Trigger")]
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F27A authored Pause request trigger for Unity-facing Pause surface validation.")]
    public sealed class PauseRequestTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(PauseRequestTrigger);

        private FrameworkLogger _logger;
        private FlowRequestOutcome _lastOutcome = FlowRequestOutcome.None;
        private PauseRequestStatus _lastStatus = PauseRequestStatus.Unknown;
        private PauseState _lastPreviousState = PauseState.Unknown;
        private PauseState _lastCurrentState = PauseState.Unknown;
        private string _lastReason = string.Empty;
        private string _lastMessage = string.Empty;

        [Header("Request")]
        [SerializeField] private string reason = "qa.pause.toggle";

        public FlowRequestOutcome LastOutcome => _lastOutcome;

        public PauseRequestStatus LastStatus => _lastStatus;

        public PauseState LastPreviousState => _lastPreviousState;

        public PauseState LastCurrentState => _lastCurrentState;

        public string LastReason => _lastReason;

        public string LastMessage => _lastMessage;

        public bool LastRequestSucceeded => _lastOutcome == FlowRequestOutcome.Succeeded;

        public bool LastRequestIgnored => _lastOutcome == FlowRequestOutcome.Ignored;

        public bool LastRequestFailed => _lastOutcome == FlowRequestOutcome.Failed;

        public bool IsPaused => TryGetPauseSnapshot(out var snapshot) && snapshot.IsPaused;

        public bool TryGetPauseSnapshot(out PauseSnapshot snapshot)
        {
            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                snapshot = default;
                return false;
            }

            return runtimeHost.TryGetPauseSnapshot(out snapshot);
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create<PauseRequestTrigger>();
        }

        [ContextMenu("Immersive Framework/Pause")]
        public void RequestPause()
        {
            Submit(PauseRequestKind.Pause, "qa.pause.pause");
        }

        [ContextMenu("Immersive Framework/Resume")]
        public void RequestResume()
        {
            Submit(PauseRequestKind.Resume, "qa.pause.resume");
        }

        [ContextMenu("Immersive Framework/Toggle")]
        public void TogglePause()
        {
            Submit(PauseRequestKind.Toggle, "qa.pause.toggle");
        }

        private void Submit(PauseRequestKind kind, string fallbackReason)
        {
            EnsureLogger();
            var resolvedReason = ResolveReason(fallbackReason);

            if (!FrameworkRuntimeHost.TryGetCurrent(out var runtimeHost))
            {
                var message = "Pause Request failed. Application Runtime is unavailable.";
                _logger.Error(message);
                SetLast(FlowRequestOutcome.Failed, PauseRequestStatus.Failed, PauseState.Unknown, PauseState.Unknown, resolvedReason, message);
                return;
            }

            PauseResult result;
            try
            {
                result = runtimeHost.RequestPause(kind, DefaultSource, resolvedReason);
            }
            catch (System.Exception exception)
            {
                var message = $"Pause Request failed. {exception.Message}";
                _logger.Error(message, exception);
                SetLast(FlowRequestOutcome.Failed, PauseRequestStatus.Failed, PauseState.Unknown, PauseState.Unknown, resolvedReason, message);
                return;
            }

            SetLast(MapOutcome(result), result.Status, result.PreviousState, result.CurrentState, resolvedReason, result.Message);
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create<PauseRequestTrigger>();
            }
        }

        private string ResolveReason(string fallbackReason)
        {
            return string.IsNullOrWhiteSpace(reason) ? fallbackReason : reason.Trim();
        }

        private void SetLast(
            FlowRequestOutcome outcome,
            PauseRequestStatus status,
            PauseState previousState,
            PauseState currentState,
            string resolvedReason,
            string message)
        {
            _lastOutcome = outcome;
            _lastStatus = status;
            _lastPreviousState = previousState;
            _lastCurrentState = currentState;
            _lastReason = resolvedReason ?? string.Empty;
            _lastMessage = message ?? string.Empty;
        }

        private static FlowRequestOutcome MapOutcome(PauseResult result)
        {
            if (result.Applied)
            {
                return FlowRequestOutcome.Succeeded;
            }

            if (result.IgnoredNoChange)
            {
                return FlowRequestOutcome.Ignored;
            }

            return FlowRequestOutcome.Failed;
        }
    }
}

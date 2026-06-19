using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using UnityEngine;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// Simple scene-authored activity request boundary.
    /// Designed for UnityEvents/UI Buttons without exposing pipeline/stage concepts.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Activity Request Trigger")]
    public sealed class ActivityRequestTrigger : MonoBehaviour
    {
        private const string DefaultSource = nameof(ActivityRequestTrigger);
        private const string DefaultClearReason = "Clear Activity";

        private FrameworkLogger _logger;

        [Header("Activity")]
        [SerializeField] private ActivityAsset targetActivity;

        [Header("Request")]
        [SerializeField] private string reason;

        private bool _requestInFlight;

        public ActivityAsset TargetActivity
        {
            get => targetActivity;
            set => targetActivity = value;
        }

        private void Awake()
        {
            _logger = FrameworkLogger.Create();
        }

        public async void RequestActivity()
        {
            EnsureLogger();

            if (_requestInFlight)
            {
                _logger.Warning("Activity Request ignored. This Activity Request Trigger already has a request in flight.");
                return;
            }

            if (targetActivity == null)
            {
                _logger.Error("Activity Request failed. Target Activity is missing.");
                return;
            }

            var resolvedReason = string.IsNullOrWhiteSpace(reason) ? targetActivity.ActivityName : reason.Trim();
            if (!TryGetRuntimeHost(targetActivity, resolvedReason, out var runtimeHost))
            {
                return;
            }

            _requestInFlight = true;
            try
            {
                await runtimeHost.RequestActivityAsync(targetActivity, DefaultSource, resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }
        }

        public async void ClearActivity()
        {
            EnsureLogger();

            if (_requestInFlight)
            {
                _logger.Warning("Activity Request ignored. This Activity Request Trigger already has a request in flight.");
                return;
            }

            var resolvedReason = string.IsNullOrWhiteSpace(reason) ? DefaultClearReason : reason.Trim();
            if (!TryGetRuntimeHost(null, resolvedReason, out var runtimeHost))
            {
                return;
            }

            _requestInFlight = true;
            try
            {
                await runtimeHost.ClearActivityAsync(DefaultSource, resolvedReason);
            }
            finally
            {
                _requestInFlight = false;
            }
        }

        private void EnsureLogger()
        {
            if (_logger == null)
            {
                _logger = FrameworkLogger.Create();
            }
        }

        private bool TryGetRuntimeHost(ActivityAsset activity, string resolvedReason, out FrameworkRuntimeHost runtimeHost)
        {
            if (FrameworkRuntimeHost.TryGetCurrent(out runtimeHost))
            {
                return true;
            }

            var unavailable = FrameworkActivityRequestResult.FailedRuntimeUnavailable(
                "Activity Request failed. Application Runtime is unavailable.",
                activity,
                DefaultSource,
                resolvedReason);
            _logger.Error(unavailable.Message);
            return false;
        }
    }
}

using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.RouteLifecycle;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.CameraFlow
{
    /// <summary>
    /// Scene-authored Cinemachine request adapter for Route, Activity, Pause, Presentation and Default camera scopes.
    /// It never owns a physical Unity Camera and never controls AudioListener.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Cinemachine Camera Binding")]
    public sealed class FrameworkCinemachineCameraBinding : MonoBehaviour, IActivityContentLifecycleReceiver, IRouteContentLifecycleReceiver, IFrameworkCameraRequestDriver
    {
        private const string DefaultSource = nameof(FrameworkCinemachineCameraBinding);
        private const int ActivePriorityBoost = 10000;

        private FrameworkLogger _logger;
        private bool _requestActive;
        private bool _warnedMissingOutputRig;
        private ActivityContentBinding _activityContentBinding;
        private RouteContentBinding _routeContentBinding;

        [Header("Cinemachine")]
        [SerializeField] private CinemachineCamera cinemachineCamera;

        [Header("Request")]
        [SerializeField] private FrameworkCameraScope cameraScope = FrameworkCameraScope.Auto;
        [SerializeField] private int priorityOffset;

        [Header("Validation")]
        [SerializeField] private bool requireOutputRig = true;
        [SerializeField] private bool validateNoPhysicalCameraInRig = true;
        [SerializeField] private bool validateNoCinemachineBrainInRig = true;

        public CinemachineCamera CinemachineCamera => cinemachineCamera;

        public FrameworkCameraScope CameraScope
        {
            get => cameraScope;
            set => cameraScope = value;
        }

        public int PriorityOffset
        {
            get => priorityOffset;
            set => priorityOffset = value;
        }

        string IFrameworkCameraRequestDriver.DriverName => name;

        private void Awake()
        {
            EnsureLogger();
            ResolveReferences();
        }

        private void OnEnable()
        {
            EnsureLogger();
            ResolveReferences();

            if (ShouldRegisterFromEnable())
            {
                RequestCamera(ResolveScopeForEnable(), DefaultSource, "camera-binding-enabled");
            }
        }

        private void OnDisable()
        {
            ReleaseCamera(DefaultSource, "camera-binding-disabled");
        }

        private void OnDestroy()
        {
            ReleaseCamera(DefaultSource, "camera-binding-destroyed");
        }

        private void Reset()
        {
            ResolveReferences();
        }

        void IActivityContentLifecycleReceiver.OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            RequestCamera(ResolveScopeForActivity(), ResolveSource(context.Source), ResolveReason(context.Reason, "activity-content-entered"));
        }

        void IActivityContentLifecycleReceiver.OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            ReleaseCamera(ResolveSource(context.Source), ResolveReason(context.Reason, "activity-content-exited"));
        }

        void IRouteContentLifecycleReceiver.OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            if (IsUnderActivityContent())
            {
                return;
            }

            RequestCamera(ResolveScopeForRoute(), ResolveSource(context.Source), ResolveReason(context.Reason, "route-content-entered"));
        }

        void IRouteContentLifecycleReceiver.OnRouteContentExited(RouteContentLifecycleContext context)
        {
            if (IsUnderActivityContent())
            {
                return;
            }

            ReleaseCamera(ResolveSource(context.Source), ResolveReason(context.Reason, "route-content-exited"));
        }

        void IFrameworkCameraRequestDriver.ApplyCameraAuthorityState(FrameworkCameraRequest request, bool isActive)
        {
            if (request == null || cinemachineCamera == null)
            {
                return;
            }

            int appliedPriority = isActive
                ? ActivePriorityBoost + request.EffectivePriority
                : request.EffectivePriority;

            if (cinemachineCamera.Priority != appliedPriority)
            {
                cinemachineCamera.Priority = appliedPriority;
            }
        }

        private void RequestCamera(FrameworkCameraScope resolvedScope, string source, string reason)
        {
            EnsureLogger();
            ResolveReferences();

            if (!ValidateBinding(resolvedScope))
            {
                return;
            }

            _requestActive = true;
            FrameworkCameraAuthority.RegisterOrUpdate(
                this,
                this,
                cinemachineCamera,
                resolvedScope,
                priorityOffset,
                source,
                reason);
        }

        private void ReleaseCamera(string source, string reason)
        {
            if (!_requestActive)
            {
                return;
            }

            _requestActive = false;
            FrameworkCameraAuthority.Release(this, source, reason);
        }

        private bool ValidateBinding(FrameworkCameraScope resolvedScope)
        {
            if (resolvedScope == FrameworkCameraScope.Auto)
            {
                _logger.Error($"Cinemachine Camera Binding invalid. object='{name}' reason='scope_auto_unresolved'.");
                return false;
            }

            if (cinemachineCamera == null)
            {
                _logger.Error($"Cinemachine Camera Binding invalid. object='{name}' reason='cinemachine_camera_missing'.");
                return false;
            }

            if (requireOutputRig && FrameworkCameraOutputRig.Current == null)
            {
                if (!_warnedMissingOutputRig)
                {
                    _logger.Error($"Cinemachine Camera Binding rejected. object='{name}' reason='camera_output_rig_missing'.");
                    _warnedMissingOutputRig = true;
                }

                return false;
            }

            if (validateNoPhysicalCameraInRig)
            {
                Camera[] cameras = GetComponentsInChildren<Camera>(true);
                if (cameras is { Length: > 0 })
                {
                    _logger.Error($"Cinemachine Camera Binding invalid. object='{name}' reason='presentation_rig_must_not_contain_unity_camera'.");
                    return false;
                }
            }

            if (validateNoCinemachineBrainInRig)
            {
                CinemachineBrain[] brains = GetComponentsInChildren<CinemachineBrain>(true);
                if (brains is { Length: > 0 })
                {
                    _logger.Error($"Cinemachine Camera Binding invalid. object='{name}' reason='presentation_rig_must_not_contain_cinemachine_brain'.");
                    return false;
                }
            }

            return true;
        }

        private void ResolveReferences()
        {
            if (cinemachineCamera == null)
            {
                CinemachineCamera[] cameras = GetComponentsInChildren<CinemachineCamera>(true);
                if (cameras != null && cameras.Length == 1)
                {
                    cinemachineCamera = cameras[0];
                }
            }

            if (_activityContentBinding == null)
            {
                _activityContentBinding = GetComponentInParent<ActivityContentBinding>(true);
            }

            if (_routeContentBinding == null)
            {
                _routeContentBinding = GetComponentInParent<RouteContentBinding>(true);
            }
        }

        private bool ShouldRegisterFromEnable()
        {
            if (cameraScope == FrameworkCameraScope.Auto)
            {
                return false;
            }

            return !IsUnderActivityContent() && !IsUnderRouteContent();
        }

        private FrameworkCameraScope ResolveScopeForEnable()
        {
            return cameraScope == FrameworkCameraScope.Auto ? FrameworkCameraScope.Default : cameraScope;
        }

        private FrameworkCameraScope ResolveScopeForActivity()
        {
            return cameraScope == FrameworkCameraScope.Auto ? FrameworkCameraScope.Activity : cameraScope;
        }

        private FrameworkCameraScope ResolveScopeForRoute()
        {
            return cameraScope == FrameworkCameraScope.Auto ? FrameworkCameraScope.Route : cameraScope;
        }

        private bool IsUnderActivityContent()
        {
            ResolveReferences();
            return _activityContentBinding != null;
        }

        private bool IsUnderRouteContent()
        {
            ResolveReferences();
            return _routeContentBinding != null;
        }

        private static string ResolveSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? DefaultSource : source.Trim();
        }

        private static string ResolveReason(string reason, string fallback)
        {
            return string.IsNullOrWhiteSpace(reason) ? fallback : reason.Trim();
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create();
        }
    }
}

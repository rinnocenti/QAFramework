using Immersive.Framework.Diagnostics;
using Unity.Cinemachine;
using UnityEngine;

namespace Immersive.Framework.CameraFlow
{
    /// <summary>
    /// Persistent physical output camera for the framework session.
    /// Route, Activity, Pause and Presentation content must provide CinemachineCamera requests, not physical render cameras.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework/Camera Output Rig")]
    public sealed class FrameworkCameraOutputRig : MonoBehaviour
    {
        private static FrameworkCameraOutputRig _current;

        private FrameworkLogger _logger;
        private bool _isCurrent;

        [Header("Output")]
        [SerializeField] private Camera outputCamera;
        [SerializeField] private CinemachineBrain cinemachineBrain;

        [Header("Session")]
        [SerializeField] private bool persistAcrossScenes = true;

        public static FrameworkCameraOutputRig Current => _current;

        public Camera OutputCamera => outputCamera;

        public CinemachineBrain CinemachineBrain => cinemachineBrain;

        public bool IsCurrent => _isCurrent && _current == this;

        private void Awake()
        {
            EnsureLogger();
            ResolveReferences();

            if (!ValidateRig())
            {
                return;
            }

            if (persistAcrossScenes)
            {
                if (transform.parent != null)
                {
                    _logger.Error(
                        $"Camera Output Rig cannot persist because it is not a scene root. object='{name}' parent='{transform.parent.name}'.");
                    return;
                }

                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            EnsureLogger();
            ResolveReferences();

            if (!ValidateRig())
            {
                return;
            }

            if (_current != null && _current != this)
            {
                _logger.Error(
                    $"Camera Output Rig rejected. Another output rig is already current. current='{_current.name}' rejected='{name}'.");
                return;
            }

            _current = this;
            _isCurrent = true;
            _logger.Info(
                $"Camera Output Rig ready. outputCamera='{outputCamera.name}' cinemachineBrain='{cinemachineBrain.name}' persistAcrossScenes='{persistAcrossScenes}'.");
        }

        private void OnDisable()
        {
            if (_current == this)
            {
                _current = null;
            }

            _isCurrent = false;
        }

        private void Reset()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (outputCamera == null)
            {
                outputCamera = GetComponent<Camera>();
            }

            if (cinemachineBrain == null)
            {
                cinemachineBrain = GetComponent<CinemachineBrain>();
            }
        }

        private bool ValidateRig()
        {
            if (outputCamera == null)
            {
                _logger.Error($"Camera Output Rig invalid. object='{name}' reason='output_camera_missing'.");
                return false;
            }

            if (cinemachineBrain == null)
            {
                _logger.Error($"Camera Output Rig invalid. object='{name}' reason='cinemachine_brain_missing'.");
                return false;
            }

            if (TryGetComponent<AudioListener>(out var listener) && listener != null)
            {
                _logger.Warning(
                    $"Camera Output Rig has an AudioListener, but CameraFlow does not control audio listeners. object='{name}' listener='{listener.name}'.");
            }

            return true;
        }

        private void EnsureLogger()
        {
            _logger ??= FrameworkLogger.Create();
        }
    }
}

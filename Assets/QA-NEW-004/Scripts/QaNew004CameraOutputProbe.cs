using System;
using System.Collections;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.QaFramework.New004
{
    [DisallowMultipleComponent]
    public sealed class QaNew004CameraOutputProbe : MonoBehaviour
    {
        [SerializeField] private QaNew004CameraContinuityEvidence evidence;

        private CameraOutputAuthoring _output;

        public string InstanceToken { get; private set; }
        public string OutputId { get; private set; }
        public bool IsReady => _output != null && _output.IsInitialized &&
                               _output.OutputDefinition != null &&
                               _output.OutputDefinition.HasValidId &&
                               _output.UnityCamera != null &&
                               _output.CinemachineBrain != null &&
                               _output.FallbackCameraRig != null;

        private void Awake()
        {
            InstanceToken = Guid.NewGuid().ToString("N");
            _output = GetComponent<CameraOutputAuthoring>();
        }

        private void Start()
        {
            StartCoroutine(RegisterAfterOutputInitialization());
        }

        private IEnumerator RegisterAfterOutputInitialization()
        {
            const int frameBudget = 180;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (evidence == null)
                {
                    yield break;
                }

                if (_output != null && _output.TryInitialize(out _) && IsReady)
                {
                    OutputId = _output.OutputIdText;
                    if (!evidence.Register(this))
                    {
                        evidence.ReportFailure(evidence.Diagnostic);
                    }
                    yield break;
                }

                yield return null;
            }

            evidence?.ReportFailure(
                _output == null
                    ? "The materialized Camera Output has no CameraOutputAuthoring on its Output GameObject."
                    : _output.LastDiagnostic);
        }

        private void OnDestroy()
        {
            evidence?.Unregister(this);
        }
    }
}

using System.Collections;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.QaFramework.IfAdr042
{
    [DisallowMultipleComponent]
    public sealed class QaIfAdr042OutputProbe : MonoBehaviour
    {
        [SerializeField] private QaIfAdr042SharedGroupEvidence evidence;
        private CameraOutputAuthoring _output;

        public CameraOutputAuthoring Output => _output;
        public bool IsReady => _output != null && _output.IsInitialized &&
            _output.OutputDefinition != null && _output.OutputDefinition.HasValidId &&
            _output.UnityCamera != null && _output.CinemachineBrain != null &&
            _output.FallbackCameraRig != null;

        private void Awake() => _output = GetComponent<CameraOutputAuthoring>();

        private void Start() => StartCoroutine(RegisterWhenReady());

        private IEnumerator RegisterWhenReady()
        {
            const int frameBudget = 180;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (evidence == null) yield break;
                if (_output != null && _output.TryInitialize(out _) && IsReady)
                {
                    if (!evidence.Register(this)) evidence.ReportFailure(evidence.Diagnostic);
                    yield break;
                }
                yield return null;
            }

            evidence?.ReportFailure(_output != null
                ? _output.LastDiagnostic
                : "The materialized Output prefab has no CameraOutputAuthoring component.");
        }

        private void OnDestroy() => evidence?.Unregister(this);
    }
}

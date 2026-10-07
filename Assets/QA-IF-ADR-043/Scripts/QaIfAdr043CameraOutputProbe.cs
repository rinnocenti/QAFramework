using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace Immersive.QaFramework.IfAdr043
{
    [DisallowMultipleComponent]
    public sealed class QaIfAdr043CameraOutputProbe : MonoBehaviour
    {
        [SerializeField] private QaIfAdr043CameraOutputEvidence evidence;
        [SerializeField] private PlayerSlotProfile mappedSlot;

        private CameraOutputAuthoring _output;

        public CameraOutputAuthoring Output => _output;
        public PlayerSlotProfile MappedSlot => mappedSlot;
        public bool IsReady => _output != null && _output.IsInitialized &&
                               _output.UnityCamera != null && _output.Session != null;

        private void Awake()
        {
            _output = GetComponent<CameraOutputAuthoring>();
            if (evidence != null && !evidence.Register(this))
                evidence.ReportFailure(evidence.Diagnostic);
        }

        private void OnDestroy()
        {
            evidence?.Unregister(this);
        }
    }
}

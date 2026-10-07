using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.QaFramework.IfAdr042
{
    [DisallowMultipleComponent]
    public sealed class QaIfAdr042OccurrenceProbe : MonoBehaviour
    {
        [SerializeField] private QaIfAdr042SharedGroupEvidence evidence;
        private CameraRigComposer _composer;

        public CameraRigComposer Composer => _composer;

        private void Awake()
        {
            _composer = GetComponent<CameraRigComposer>();
            if (evidence != null && !evidence.Register(this))
                evidence.ReportFailure(evidence.Diagnostic);
        }

        private void OnDestroy() => evidence?.Unregister(this);
    }
}

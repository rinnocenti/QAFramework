using Immersive.Framework.Audio;
using Immersive.Framework.ActivityFlow;
using UnityEngine;

namespace Immersive.QaFramework.New005
{
    public sealed class QaNew005ActivityBgmProbe : ActivityContentBehaviour
    {
        [SerializeField] private ActivityBgmAuthoring authoring;
        [SerializeField] private QaNew005AudioEvidence evidence;
        private bool _countedBound;
        private bool _detachmentRecorded;

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            _countedBound = evidence != null && evidence.RecordActivityEnter(authoring);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            evidence?.RecordActivityExit();
        }

        private void OnDisable() => RecordDetachIfReleased();

        private void OnDestroy()
        {
            if (!Application.isPlaying || _detachmentRecorded) return;
            if (authoring == null || authoring.Director != null)
            {
                _detachmentRecorded = true;
                evidence?.RecordActivityDestroyedWhileBound(_countedBound);
                _countedBound = false;
                return;
            }

            RecordDetachIfReleased();
        }

        private void RecordDetachIfReleased()
        {
            if (!Application.isPlaying || _detachmentRecorded || authoring == null || authoring.Director != null) return;
            _detachmentRecorded = true;
            evidence?.RecordActivityDetached(_countedBound);
            _countedBound = false;
        }
    }
}

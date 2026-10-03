using Immersive.Framework.Audio;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Immersive.QaFramework.New005
{
    public sealed class QaNew005RouteBgmProbe : RouteContentBehaviour
    {
        [SerializeField] private RouteBgmAuthoring authoring;
        [SerializeField] private QaNew005AudioEvidence evidence;
        private bool _countedBound;
        private bool _detachmentRecorded;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            _countedBound = evidence != null && evidence.RecordRouteEnter(authoring);
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            evidence?.RecordRouteExit();
        }

        private void OnDisable() => RecordDetachIfReleased();

        private void OnDestroy()
        {
            if (!Application.isPlaying || _detachmentRecorded) return;
            if (authoring == null || authoring.Director != null)
            {
                _detachmentRecorded = true;
                evidence?.RecordRouteDestroyedWhileBound(_countedBound);
                _countedBound = false;
                return;
            }

            RecordDetachIfReleased();
        }

        private void RecordDetachIfReleased()
        {
            if (!Application.isPlaying || _detachmentRecorded || authoring == null || authoring.Director != null) return;
            _detachmentRecorded = true;
            evidence?.RecordRouteDetached(_countedBound);
            _countedBound = false;
        }
    }
}

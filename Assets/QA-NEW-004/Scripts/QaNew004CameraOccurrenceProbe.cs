using System;
using Immersive.Framework.CameraAuthoring;
using UnityEngine;

namespace Immersive.QaFramework.New004
{
    [DisallowMultipleComponent]
    public sealed class QaNew004CameraOccurrenceProbe : MonoBehaviour
    {
        private string _occurrenceToken;

        public string OccurrenceToken => _occurrenceToken ?? string.Empty;

        public CameraRigComposer Composer { get; private set; }

        private void Awake()
        {
            _occurrenceToken = Guid.NewGuid().ToString("N");
            Composer = GetComponentInChildren<CameraRigComposer>(true);
        }
    }
}

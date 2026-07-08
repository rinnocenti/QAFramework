using Immersive.Pooling.Unity.Instances;
using UnityEngine;

namespace ImmersiveFrameworkQA.Pooling
{
    /// <summary>
    /// QA-only pooled object probe used by the Pooling QA harness.
    /// It records lifecycle callbacks without adding gameplay behavior.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Pooling/Pooling QA Callback Probe")]
    public sealed class PoolingQaCallbackProbe : PoolableBehaviour
    {
        [SerializeField] private string probeId = "pooling.qa.probe";

        public string ProbeId => string.IsNullOrWhiteSpace(probeId) ? name : probeId;

        public int CreatedCallbacks { get; private set; }

        public int TakenCallbacks { get; private set; }

        public int ReturnedCallbacks { get; private set; }

        public int DestroyedCallbacks { get; private set; }

        public void Configure(string id)
        {
            probeId = string.IsNullOrWhiteSpace(id) ? "pooling.qa.probe" : id.Trim();
        }

        protected override void HandleCreatedByPool()
        {
            CreatedCallbacks++;
        }

        protected override void HandleTakenFromPool()
        {
            TakenCallbacks++;
        }

        protected override void HandleReturnedToPool()
        {
            ReturnedCallbacks++;
        }

        protected override void HandleDestroyedByPool()
        {
            DestroyedCallbacks++;
        }
    }
}

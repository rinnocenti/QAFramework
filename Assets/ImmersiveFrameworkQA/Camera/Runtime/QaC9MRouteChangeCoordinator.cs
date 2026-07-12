using System.Collections;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaC9MRouteChangeCoordinator : MonoBehaviour
    {
        [SerializeField] private QaC9MActivityRouteTeardownProbe teardownProbe;
        [SerializeField] private RouteRequestTrigger backToHubTrigger;
        [SerializeField] private int settleFrames = 2;
        [SerializeField] private bool runOnStart = true;

        [SerializeField, HideInInspector] private string lastStatus = "NotRun";
        [SerializeField, HideInInspector] private string lastDiagnostic = string.Empty;

        private bool started;

        private void Start()
        {
            if (runOnStart)
            {
                Begin();
            }
        }

        [ContextMenu("Run C9M Route Change")]
        public void Begin()
        {
            if (started)
            {
                return;
            }

            started = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (teardownProbe == null)
            {
                Fail("C9M coordinator requires a teardown probe.");
                yield break;
            }

            if (backToHubTrigger == null)
            {
                Fail("C9M coordinator requires the Back to Hub RouteRequestTrigger.");
                yield break;
            }

            int guard = 120;
            while (!teardownProbe.Entered && guard-- > 0)
            {
                yield return null;
            }

            if (!teardownProbe.Entered)
            {
                Fail("C9M startup Activity did not enter before the timeout.");
                yield break;
            }

            int frames = Mathf.Max(0, settleFrames);
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }

            lastStatus = "RouteChangeRequested";
            lastDiagnostic =
                "Requesting Back to Hub while the startup Activity is still active.";

            Debug.Log(
                $"[QA][C9M Activity Route Teardown] {lastDiagnostic}",
                this);

            backToHubTrigger.RequestRoute();
        }

        private void Fail(string diagnostic)
        {
            lastStatus = "Failed";
            lastDiagnostic = diagnostic;

            Debug.LogError(
                $"[QA][C9M Activity Route Teardown] {diagnostic}",
                this);
        }
    }
}

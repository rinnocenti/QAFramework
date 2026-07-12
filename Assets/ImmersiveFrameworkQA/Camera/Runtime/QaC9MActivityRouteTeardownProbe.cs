using Immersive.Framework.ActivityFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaC9MActivityRouteTeardownProbe :
        MonoBehaviour,
        IActivityContentLifecycleReceiver
    {
        [SerializeField] private bool logDiagnostics = true;

        [SerializeField, HideInInspector] private bool entered;
        [SerializeField, HideInInspector] private bool exitedBeforeDisable;
        [SerializeField, HideInInspector] private string lastStatus = "NotRun";
        [SerializeField, HideInInspector] private string lastDiagnostic = string.Empty;

        public bool Entered => entered;

        public bool ExitedBeforeDisable => exitedBeforeDisable;

        public string LastStatus => lastStatus;

        public string LastDiagnostic => lastDiagnostic;

        public void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            entered = true;
            exitedBeforeDisable = false;

            SetDiagnostic(
                "Entered",
                $"Activity teardown probe entered. activity='{ResolveActivityName(context)}'.",
                false);
        }

        public void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            if (!entered)
            {
                SetDiagnostic(
                    "Failed",
                    $"Activity teardown probe received exit without prior enter. activity='{ResolveActivityName(context)}'.",
                    true);
                return;
            }

            exitedBeforeDisable = true;

            SetDiagnostic(
                "ExitObserved",
                $"Activity teardown probe observed lifecycle exit before disable. activity='{ResolveActivityName(context)}'.",
                false);
        }

        private void OnDisable()
        {
            if (!entered)
            {
                return;
            }

            if (!exitedBeforeDisable)
            {
                SetDiagnostic(
                    "Failed",
                    "Activity teardown ordering failed. Scene-authored Activity content was disabled before OnActivityContentExited.",
                    true);
                return;
            }

            SetDiagnostic(
                "Passed",
                "PASS. Route change dispatched Activity exit before scene-authored content was disabled.",
                false);

            entered = false;
        }

        private static string ResolveActivityName(ActivityContentLifecycleContext context)
        {
            if (context.Activity != null)
            {
                return context.Activity.ActivityName;
            }

            if (context.PreviousActivity != null)
            {
                return context.PreviousActivity.ActivityName;
            }

            return "<none>";
        }

        private void SetDiagnostic(string status, string diagnostic, bool isError)
        {
            lastStatus = status;
            lastDiagnostic = diagnostic ?? string.Empty;

            if (!logDiagnostics)
            {
                return;
            }

            string message =
                $"[QA][C9M Activity Route Teardown] {lastDiagnostic}";

            if (isError)
            {
                Debug.LogError(message, this);
                return;
            }

            Debug.Log(message, this);
        }
    }
}

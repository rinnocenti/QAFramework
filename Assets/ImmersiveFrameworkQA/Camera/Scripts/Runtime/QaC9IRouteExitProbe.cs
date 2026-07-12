using Immersive.Framework.Camera;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    public static class QaC9IRouteExitEvidence
    {
        public static bool Recorded { get; private set; }
        public static bool BindingReleased { get; private set; }
        public static bool OutputCleared { get; private set; }
        public static string Diagnostic { get; private set; }

        public static void Reset()
        {
            Recorded = false;
            BindingReleased = false;
            OutputCleared = false;
            Diagnostic = string.Empty;
        }

        public static void Record(
            bool bindingReleased,
            bool outputCleared,
            string diagnostic)
        {
            Recorded = true;
            BindingReleased = bindingReleased;
            OutputCleared = outputCleared;
            Diagnostic = diagnostic ?? string.Empty;
        }
    }

    [DisallowMultipleComponent]
    public sealed class QaC9IRouteExitProbe : RouteContentBehaviour
    {
        [SerializeField] private RouteCameraOverrideBinding routeBinding;
        [SerializeField] private CameraOutputSessionBinding outputSession;

        protected override void OnRouteContentExited(
            RouteContentLifecycleContext context)
        {
            bool released =
                routeBinding != null &&
                !routeBinding.IsPublished &&
                string.Equals(
                    routeBinding.LastStatus,
                    "Released",
                    System.StringComparison.Ordinal);

            bool cleared =
                outputSession != null &&
                outputSession.IsInitialized &&
                !outputSession.Context.HasWinner &&
                !outputSession.Applicator.HasAppliedRequest;

            string diagnostic =
                $"route='{context.RouteName}' bindingStatus='{routeBinding?.LastStatus}' " +
                $"bindingPublished='{routeBinding?.IsPublished}' hasWinner='{outputSession?.Context?.HasWinner}' " +
                $"hasApplied='{outputSession?.Applicator?.HasAppliedRequest}'.";

            QaC9IRouteExitEvidence.Record(
                released,
                cleared,
                diagnostic);

            Debug.Log(
                $"[QA][C9I Canonical Camera Bindings] step='canonical-route-exit-clears-output' " +
                $"evidence='{diagnostic}'.",
                this);
        }
    }
}

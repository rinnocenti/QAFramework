using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// QA-only Route lifecycle adapter for camera rig precedence validation.
    /// Startup Activity Camera is resolved through an explicit scene-authored reference.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/QA Route Camera Binding")]
    public sealed class QaRouteCameraBinding : RouteContentBehaviour
    {
        [SerializeField] private GameObject routeCameraRig;
        [SerializeField] private QaCameraAnchorHost routeAnchors;
        [SerializeField] private QaCameraDirector director;
        [SerializeField] private QaActivityCameraBinding startupActivityCameraBinding;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError("[QA_CAMERA] Route Camera binding requires a QaCameraDirector.", this);
                return;
            }

            ActivityAsset startupActivity = context.Route != null && context.Route.HasStartupActivity
                ? context.Route.StartupActivity
                : null;

            bool hasStartupActivity = startupActivity != null;
            director.SetRouteCamera(routeCameraRig, routeAnchors, hasStartupActivity);

            if (!hasStartupActivity)
            {
                return;
            }

            if (startupActivityCameraBinding != null
                && startupActivityCameraBinding.TryApplyStartupActivityCamera(director, startupActivity, context.RouteName))
            {
                return;
            }

            Debug.LogWarning(
                $"[QA_CAMERA] Route has Startup Activity but no valid explicit Startup Activity Camera binding was assigned. route='{context.RouteName}' startupActivity='{FormatActivity(startupActivity)}'. Route camera fallback will be applied.",
                this);
            director.Refresh();
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError("[QA_CAMERA] Route Camera binding requires a QaCameraDirector.", this);
                return;
            }

            director.ClearRouteCamera(routeCameraRig);
        }

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<none>";
        }
    }
}

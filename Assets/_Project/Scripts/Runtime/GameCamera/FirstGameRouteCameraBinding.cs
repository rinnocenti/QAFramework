using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace Project._Project.Scripts.Runtime.GameCamera
{
    /// <summary>
    /// FIRSTGAME scene-authored Route lifecycle adapter for camera rigs.
    /// Startup Activity Camera is resolved through an explicit scene-authored reference, not global scene search.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstGameRouteCameraBinding : RouteContentBehaviour
    {
        [SerializeField] private GameObject routeCameraRig;
        [SerializeField] private FirstGameCameraAnchorHost routeAnchors;
        [SerializeField] private FirstGameCameraDirector director;
        [SerializeField] private FirstGameActivityCameraBinding startupActivityCameraBinding;

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError("[FIRSTGAME_CAMERA] Route Camera binding requires a FirstGameCameraDirector.", this);
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
                $"[FIRSTGAME_CAMERA] Route has Startup Activity but no valid explicit Startup Activity Camera binding was assigned. route='{context.RouteName}' startupActivity='{FormatActivity(startupActivity)}'. Route camera fallback will be applied.",
                this);
            director.Refresh();
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError("[FIRSTGAME_CAMERA] Route Camera binding requires a FirstGameCameraDirector.", this);
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

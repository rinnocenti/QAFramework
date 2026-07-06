using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// QA-only Activity lifecycle adapter for camera rig precedence validation.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Camera/QA Activity Camera Binding")]
    public sealed class QaActivityCameraBinding : ActivityContentBehaviour
    {
        [SerializeField] private ActivityAsset assignedActivity;
        [SerializeField] private GameObject activityCameraRig;
        [SerializeField] private QaActivityCameraPolicy policy = QaActivityCameraPolicy.UseOwnOrRoute;
        [SerializeField] private QaCameraAnchorHost anchors;
        [SerializeField] private QaCameraDirector director;

        internal bool TryApplyStartupActivityCamera(
            QaCameraDirector expectedDirector,
            ActivityAsset expectedActivity,
            string routeName)
        {
            if (director == null)
            {
                Debug.LogError("[QA_CAMERA] Activity Camera binding requires a QaCameraDirector.", this);
                return false;
            }

            if (expectedDirector != null && !ReferenceEquals(director, expectedDirector))
            {
                Debug.LogWarning(
                    $"[QA_CAMERA] Startup Activity Camera binding ignored because it targets a different director. route='{routeName}' activityRig='{FormatRig(activityCameraRig)}'.",
                    this);
                return false;
            }

            if (expectedActivity != null && !ReferenceEquals(assignedActivity, expectedActivity))
            {
                Debug.LogWarning(
                    $"[QA_CAMERA] Startup Activity Camera binding ignored because it does not match the Route Startup Activity. route='{routeName}' expectedActivity='{FormatActivity(expectedActivity)}' bindingActivity='{FormatActivity(assignedActivity)}' activityRig='{FormatRig(activityCameraRig)}'.",
                    this);
                return false;
            }

            Debug.Log(
                $"[QA_CAMERA] Startup Activity Camera pre-applied from explicit Route binding. route='{routeName}' activity='{FormatActivity(expectedActivity)}' activityRig='{FormatRig(activityCameraRig)}' policy='{policy}'.",
                this);
            director.SetActivityCamera(activityCameraRig, policy, anchors);
            return true;
        }

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError("[QA_CAMERA] Activity Camera binding requires a QaCameraDirector.", this);
                return;
            }

            director.SetActivityCamera(activityCameraRig, policy, anchors);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError("[QA_CAMERA] Activity Camera binding requires a QaCameraDirector.", this);
                return;
            }

            bool deferRefreshForActivityTransition = context.NextActivity != null
                && !ReferenceEquals(context.NextActivity, context.Activity);

            director.ClearActivityCamera(activityCameraRig, deferRefreshForActivityTransition);
        }

        private static string FormatRig(GameObject rig)
        {
            return rig != null ? rig.name : "<none>";
        }

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<none>";
        }
    }
}

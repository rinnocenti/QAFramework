using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using UnityEngine;

namespace Project._Project.Scripts.Runtime.GameCamera
{
    /// <summary>
    /// FIRSTGAME scene-authored Activity lifecycle adapter for camera rigs.
    /// This consumes framework ActivityContent callbacks without making the framework depend on Cinemachine.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstGameActivityCameraBinding : ActivityContentBehaviour
    {
        [SerializeField] private GameObject activityCameraRig;
        [SerializeField] private FirstGameActivityCameraPolicy policy = FirstGameActivityCameraPolicy.UseOwnOrRoute;
        [SerializeField] private FirstGameCameraAnchorHost anchors;
        [SerializeField] private FirstGameCameraDirector director;

        internal bool TryApplyStartupActivityCamera(
            FirstGameCameraDirector expectedDirector,
            ActivityAsset expectedActivity,
            string routeName)
        {
            if (director == null)
            {
                Debug.LogError("[FIRSTGAME_CAMERA] Activity Camera binding requires a FirstGameCameraDirector.", this);
                return false;
            }

            if (expectedDirector != null && !ReferenceEquals(director, expectedDirector))
            {
                Debug.LogWarning(
                    $"[FIRSTGAME_CAMERA] Startup Activity Camera binding ignored because it targets a different director. route='{routeName}' activityRig='{FormatRig(activityCameraRig)}'.",
                    this);
                return false;
            }

            if (!MatchesExpectedActivity(expectedActivity, out string assignedActivityName))
            {
                Debug.LogWarning(
                    $"[FIRSTGAME_CAMERA] Startup Activity Camera binding ignored because it does not belong to the expected Activity. route='{routeName}' expectedActivity='{FormatActivity(expectedActivity)}' assignedActivity='{assignedActivityName}' activityRig='{FormatRig(activityCameraRig)}'.",
                    this);
                return false;
            }

            Debug.Log(
                $"[FIRSTGAME_CAMERA] Startup Activity Camera pre-applied from explicit Route binding. route='{routeName}' activity='{FormatActivity(expectedActivity)}' activityRig='{FormatRig(activityCameraRig)}' policy='{policy}'.",
                this);
            director.SetActivityCamera(activityCameraRig, policy, anchors);
            return true;
        }

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError("[FIRSTGAME_CAMERA] Activity Camera binding requires a FirstGameCameraDirector.", this);
                return;
            }

            director.SetActivityCamera(activityCameraRig, policy, anchors);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            if (director == null)
            {
                Debug.LogError("[FIRSTGAME_CAMERA] Activity Camera binding requires a FirstGameCameraDirector.", this);
                return;
            }

            bool deferRefreshForActivityTransition = context.NextActivity != null
                && !ReferenceEquals(context.NextActivity, context.Activity);

            director.ClearActivityCamera(activityCameraRig, deferRefreshForActivityTransition);
        }

        private bool MatchesExpectedActivity(ActivityAsset expectedActivity, out string assignedActivityName)
        {
            ActivityLocalVisibilityAdapter adapter = GetComponent<ActivityLocalVisibilityAdapter>();
            if (adapter == null)
            {
                adapter = GetComponentInParent<ActivityLocalVisibilityAdapter>();
            }

            ActivityAsset assignedActivity = adapter != null ? adapter.Activity : null;
            assignedActivityName = FormatActivity(assignedActivity);

            return expectedActivity == null || ReferenceEquals(assignedActivity, expectedActivity);
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

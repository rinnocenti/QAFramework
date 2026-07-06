using Unity.Cinemachine;
using UnityEngine;

namespace Project._Project.Scripts.Runtime.GameCamera
{
    /// <summary>
    /// FIRSTGAME camera director that resolves scene-authored Route/Activity Cinemachine rigs.
    ///
    /// Priority:
    /// Startup Activity Camera > Activity Camera > retained Activity Camera for current Route > Route Camera > Default Camera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstGameCameraDirector : MonoBehaviour
    {
        [Header("Operational Camera")]
        [SerializeField] private UnityEngine.Camera operationalCamera;
        [SerializeField] private CinemachineBrain cinemachineBrain;

        [Header("Fallback")]
        [SerializeField] private GameObject defaultCameraRig;

        [Header("Priority")]
        [SerializeField] private int routePriority = 20;
        [SerializeField] private int activityPriority = 100;

        [Header("Diagnostics")]
        [SerializeField] private bool logTransitions = true;

        private GameObject currentRouteRig;
        private FirstGameCameraAnchorHost currentRouteAnchors;
        private GameObject currentActivityRig;
        private FirstGameCameraAnchorHost currentActivityAnchors;
        private GameObject retainedActivityRigForCurrentRoute;
        private FirstGameCameraAnchorHost retainedActivityAnchorsForCurrentRoute;
        private GameObject currentEffectiveRig;
        private FirstGameActivityCameraPolicy currentActivityPolicy = FirstGameActivityCameraPolicy.UseOwnOrRoute;
        private bool hasActiveActivityCameraBinding;

        public GameObject CurrentRouteRig => currentRouteRig;

        public GameObject CurrentActivityRig => currentActivityRig;

        public GameObject RetainedActivityRigForCurrentRoute => retainedActivityRigForCurrentRoute;

        public GameObject CurrentEffectiveRig => currentEffectiveRig;

        public void SetRouteCamera(GameObject cameraRig, FirstGameCameraAnchorHost anchors)
        {
            SetRouteCamera(cameraRig, anchors, false);
        }

        public void SetRouteCamera(GameObject cameraRig, FirstGameCameraAnchorHost anchors, bool deferRefreshForStartupActivity)
        {
            currentRouteRig = cameraRig;
            currentRouteAnchors = anchors;
            currentActivityRig = null;
            currentActivityAnchors = null;
            retainedActivityRigForCurrentRoute = null;
            retainedActivityAnchorsForCurrentRoute = null;
            hasActiveActivityCameraBinding = false;
            currentActivityPolicy = FirstGameActivityCameraPolicy.UseOwnOrRoute;

            Log($"Route camera set. routeRig='{FormatRig(cameraRig)}' retainedActivityRig='<cleared>' deferRefreshForStartupActivity='{deferRefreshForStartupActivity}'.");

            if (!deferRefreshForStartupActivity)
            {
                Refresh();
            }
        }

        public void ClearRouteCamera(GameObject cameraRig)
        {
            if (cameraRig != null && !ReferenceEquals(currentRouteRig, cameraRig))
            {
                Log($"Route camera clear ignored as stale. requested='{FormatRig(cameraRig)}' currentRouteRig='{FormatRig(currentRouteRig)}'.");
                return;
            }

            currentRouteRig = null;
            currentRouteAnchors = null;
            currentActivityRig = null;
            currentActivityAnchors = null;
            retainedActivityRigForCurrentRoute = null;
            retainedActivityAnchorsForCurrentRoute = null;
            hasActiveActivityCameraBinding = false;
            currentActivityPolicy = FirstGameActivityCameraPolicy.UseOwnOrRoute;

            Log("Route camera cleared. Activity camera retention cleared with Route scope.");
            Refresh();
        }

        public void SetActivityCamera(GameObject cameraRig, FirstGameActivityCameraPolicy policy, FirstGameCameraAnchorHost anchors)
        {
            hasActiveActivityCameraBinding = true;
            currentActivityPolicy = policy;

            if (policy == FirstGameActivityCameraPolicy.UseRoute)
            {
                currentActivityRig = null;
                currentActivityAnchors = null;
                Log($"Activity camera policy UseRoute applied. retainedActivityRig='{FormatRig(retainedActivityRigForCurrentRoute)}'.");
                Refresh();
                return;
            }

            currentActivityRig = cameraRig;
            currentActivityAnchors = anchors;

            if (policy == FirstGameActivityCameraPolicy.UseOwnOrKeepPreviousActivity && cameraRig != null)
            {
                retainedActivityRigForCurrentRoute = cameraRig;
                retainedActivityAnchorsForCurrentRoute = anchors;
            }

            Log($"Activity camera set. activityRig='{FormatRig(cameraRig)}' policy='{policy}' retainedActivityRig='{FormatRig(retainedActivityRigForCurrentRoute)}'.");
            Refresh();
        }

        public void ClearActivityCamera(GameObject cameraRig)
        {
            ClearActivityCamera(cameraRig, false);
        }

        public void ClearActivityCamera(GameObject cameraRig, bool deferRefreshForActivityTransition)
        {
            if (cameraRig != null && !ReferenceEquals(currentActivityRig, cameraRig))
            {
                Log($"Activity camera clear ignored as stale. requested='{FormatRig(cameraRig)}' currentActivityRig='{FormatRig(currentActivityRig)}'.");
                return;
            }

            currentActivityRig = null;
            currentActivityAnchors = null;
            hasActiveActivityCameraBinding = false;
            currentActivityPolicy = FirstGameActivityCameraPolicy.UseOwnOrRoute;

            Log($"Activity camera cleared. retainedActivityRig='{FormatRig(retainedActivityRigForCurrentRoute)}' deferRefresh='{deferRefreshForActivityTransition}'.");

            if (!deferRefreshForActivityTransition)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (!ValidateOperationalCamera())
            {
                return;
            }

            GameObject next = ResolveEffectiveRig(out FirstGameCameraAnchorHost anchors, out int priority, out string source);

            if (next == null)
            {
                Debug.LogError("[FIRSTGAME_CAMERA] No effective camera rig is available. Configure a default, route, or activity camera rig.", this);
                return;
            }

            if (ReferenceEquals(currentEffectiveRig, next))
            {
                ApplyCinemachineRig(next, anchors, priority, source);
                Log($"Camera refresh skipped. effectiveRig='{FormatRig(next)}' source='{source}'.");
                return;
            }

            SetRigActive(currentEffectiveRig, false);
            currentEffectiveRig = next;
            SetRigActive(currentEffectiveRig, true);
            ApplyCinemachineRig(currentEffectiveRig, anchors, priority, source);

            Log($"Camera applied. effectiveRig='{FormatRig(currentEffectiveRig)}' source='{source}' priority='{priority}'.");
        }

        private GameObject ResolveEffectiveRig(out FirstGameCameraAnchorHost anchors, out int priority, out string source)
        {
            if (!hasActiveActivityCameraBinding)
            {
                anchors = currentRouteAnchors;
                priority = routePriority;
                source = currentRouteRig != null ? "Route" : "Default";
                return currentRouteRig != null ? currentRouteRig : defaultCameraRig;
            }

            switch (currentActivityPolicy)
            {
                case FirstGameActivityCameraPolicy.UseRoute:
                    anchors = currentRouteAnchors;
                    priority = routePriority;
                    source = currentRouteRig != null ? "Route" : "Default";
                    return currentRouteRig != null ? currentRouteRig : defaultCameraRig;

                case FirstGameActivityCameraPolicy.UseOwnOrKeepPreviousActivity:
                    if (currentActivityRig != null)
                    {
                        anchors = currentActivityAnchors;
                        priority = activityPriority;
                        source = "Activity";
                        return currentActivityRig;
                    }

                    if (retainedActivityRigForCurrentRoute != null)
                    {
                        anchors = retainedActivityAnchorsForCurrentRoute;
                        priority = activityPriority;
                        source = "RetainedActivity";
                        return retainedActivityRigForCurrentRoute;
                    }

                    anchors = currentRouteAnchors;
                    priority = routePriority;
                    source = currentRouteRig != null ? "Route" : "Default";
                    return currentRouteRig != null ? currentRouteRig : defaultCameraRig;

                default:
                    if (currentActivityRig != null)
                    {
                        anchors = currentActivityAnchors;
                        priority = activityPriority;
                        source = "Activity";
                        return currentActivityRig;
                    }

                    anchors = currentRouteAnchors;
                    priority = routePriority;
                    source = currentRouteRig != null ? "Route" : "Default";
                    return currentRouteRig != null ? currentRouteRig : defaultCameraRig;
            }
        }

        private bool ValidateOperationalCamera()
        {
            if (operationalCamera == null)
            {
                Debug.LogError("[FIRSTGAME_CAMERA] Operational Unity Camera is missing.", this);
                return false;
            }

            if (cinemachineBrain == null)
            {
                cinemachineBrain = operationalCamera.GetComponent<CinemachineBrain>();
            }

            if (cinemachineBrain == null)
            {
                Debug.LogError("[FIRSTGAME_CAMERA] Operational Unity Camera requires a CinemachineBrain.", operationalCamera);
                return false;
            }

            return true;
        }

        private static void ApplyCinemachineRig(GameObject rig, FirstGameCameraAnchorHost anchors, int priority, string source)
        {
            if (rig == null)
            {
                return;
            }

            CinemachineCamera cinemachineCamera = rig.GetComponentInChildren<CinemachineCamera>(true);
            if (cinemachineCamera == null)
            {
                Debug.LogError($"[FIRSTGAME_CAMERA] Camera rig requires one CinemachineCamera. rig='{FormatRig(rig)}' source='{source}'.", rig);
                return;
            }

            if (rig.GetComponentInChildren<UnityEngine.Camera>(true) != null)
            {
                Debug.LogError($"[FIRSTGAME_CAMERA] Camera rig must not contain a real Unity Camera. rig='{FormatRig(rig)}'.", rig);
                return;
            }

            if (rig.GetComponentInChildren<CinemachineBrain>(true) != null)
            {
                Debug.LogError($"[FIRSTGAME_CAMERA] Camera rig must not contain a CinemachineBrain. rig='{FormatRig(rig)}'.", rig);
                return;
            }

            cinemachineCamera.Priority = priority;

            if (anchors != null && anchors.HasAnyTarget)
            {
                cinemachineCamera.Target.TrackingTarget = anchors.TrackingTarget;
                cinemachineCamera.Target.LookAtTarget = anchors.LookAtTarget;
            }
        }

        private static void SetRigActive(GameObject rig, bool isActive)
        {
            if (rig != null && rig.activeSelf != isActive)
            {
                rig.SetActive(isActive);
            }
        }

        private void Log(string message)
        {
            if (!logTransitions)
            {
                return;
            }

            Debug.Log($"[FIRSTGAME_CAMERA] {message}", this);
        }

        private static string FormatRig(GameObject rig)
        {
            return rig != null ? rig.name : "<none>";
        }
    }
}

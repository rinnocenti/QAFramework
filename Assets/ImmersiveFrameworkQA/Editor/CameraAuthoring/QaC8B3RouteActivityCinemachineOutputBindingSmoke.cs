using Immersive.Framework.Camera.Cinemachine;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    public static class QaC8B3RouteActivityCinemachineOutputBindingSmoke
    {
        private const string LogPrefix = "[QA][C8B3 RouteActivity Cinemachine Output]";
        private const int ClearedPriority = 0;
        private const int RoutePriority = 10;
        private const int RetainedActivityPriority = 80;
        private const int ActivityPriority = 100;

        [MenuItem("Immersive Framework/QA/Camera/C8B3 Route Activity Cinemachine Output Binding Smoke")]
        public static void Run()
        {
            bool succeeded = RunForRegression();
            if (succeeded)
            {
                Debug.Log($"{LogPrefix} PASS. Route/Activity Cinemachine output binding applies Route, Activity override, Activity clear, UseRoute, retained Activity, and missing-output diagnostics without mutating legacy camera state.");
            }
        }

        public static bool RunForRegression()
        {
            Debug.Log($"{LogPrefix} Smoke started.");

            GameObject root = null;
            try
            {
                root = CreateHiddenRoot("QA_C8B3_RouteActivityCinemachineOutput_Root");
                SmokeFixture fixture = CreateFixture(root.transform);

                if (!RunRouteEnterCase(fixture))
                {
                    return false;
                }

                if (!RunActivityOverrideCase(fixture))
                {
                    return false;
                }

                if (!RunActivityExitRestoresRouteCase(fixture))
                {
                    return false;
                }

                if (!RunUseRouteCase(fixture))
                {
                    return false;
                }

                if (!RunRetainedActivityCase(fixture))
                {
                    return false;
                }

                if (!RunMissingRequiredActivityOutputCase(fixture))
                {
                    return false;
                }

                if (!RunMissingOptionalActivityOutputCase())
                {
                    return false;
                }

                if (!RunWrongBrainScopeBlocksCase(fixture))
                {
                    return false;
                }

                if (!AssertLegacyStateUntouched(fixture, "final-legacy-state"))
                {
                    return false;
                }

                return true;
            }
            catch (System.Exception exception)
            {
                return Fail("unexpected-exception", exception.ToString());
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static bool RunRouteEnterCase(SmokeFixture fixture)
        {
            CinemachineCameraOutputDiagnostic diagnostic = ApplyRoute(fixture);
            if (!diagnostic.IsSucceeded)
            {
                return Fail("route-enter-not-applied", FormatDiagnostic(diagnostic));
            }

            if (!AssertPriority(fixture.RouteCamera, RoutePriority, "route-priority-not-applied"))
            {
                return false;
            }

            if (!AssertTargets(fixture.RouteCamera, fixture.RouteFollow, fixture.RouteLookAt, "route-targets-not-applied"))
            {
                return false;
            }

            if (!AssertLegacyStateUntouched(fixture, "route-enter-legacy-state"))
            {
                return false;
            }

            Debug.Log($"{LogPrefix} step='route-enter-applied' status='{diagnostic.Status}' code='{diagnostic.Code}' routePriority='{RoutePriority}' follow='{fixture.RouteFollow.name}' lookAt='{fixture.RouteLookAt.name}'");
            return true;
        }

        private static bool RunActivityOverrideCase(SmokeFixture fixture)
        {
            CinemachineCameraOutputDiagnostic routeDiagnostic = ApplyRoute(fixture);
            if (!routeDiagnostic.IsSucceeded)
            {
                return Fail("activity-override-route-precondition-failed", FormatDiagnostic(routeDiagnostic));
            }

            CinemachineCameraOutputDiagnostic activityDiagnostic = ApplyActivity(fixture);
            if (!activityDiagnostic.IsSucceeded)
            {
                return Fail("activity-override-not-applied", FormatDiagnostic(activityDiagnostic));
            }

            if (!AssertPriority(fixture.ActivityCamera, ActivityPriority, "activity-priority-not-applied"))
            {
                return false;
            }

            if (!AssertTargets(fixture.ActivityCamera, fixture.ActivityFollow, fixture.ActivityLookAt, "activity-targets-not-applied"))
            {
                return false;
            }

            if (!IsPriorityGreaterThan(fixture.ActivityCamera, fixture.RouteCamera))
            {
                return Fail("activity-priority-does-not-override-route", $"routePriority='{RoutePriority}' activityPriority='{ActivityPriority}'");
            }

            if (!AssertLegacyStateUntouched(fixture, "activity-enter-legacy-state"))
            {
                return false;
            }

            Debug.Log($"{LogPrefix} step='activity-override-applied' status='{activityDiagnostic.Status}' code='{activityDiagnostic.Code}' routePriority='{RoutePriority}' activityPriority='{ActivityPriority}'");
            return true;
        }

        private static bool RunActivityExitRestoresRouteCase(SmokeFixture fixture)
        {
            CinemachineCameraOutputDiagnostic activityDiagnostic = ApplyActivity(fixture);
            if (!activityDiagnostic.IsSucceeded)
            {
                return Fail("activity-exit-precondition-activity-failed", FormatDiagnostic(activityDiagnostic));
            }

            CinemachineCameraOutputDiagnostic clearDiagnostic = ApplyActivityClear(fixture);
            if (!clearDiagnostic.IsSucceeded)
            {
                return Fail("activity-clear-not-applied", FormatDiagnostic(clearDiagnostic));
            }

            CinemachineCameraOutputDiagnostic routeDiagnostic = ApplyRoute(fixture);
            if (!routeDiagnostic.IsSucceeded)
            {
                return Fail("route-restore-not-applied", FormatDiagnostic(routeDiagnostic));
            }

            if (!AssertPriority(fixture.ActivityCamera, ClearedPriority, "activity-priority-not-cleared"))
            {
                return false;
            }

            if (!AssertPriority(fixture.RouteCamera, RoutePriority, "route-priority-not-restored"))
            {
                return false;
            }

            if (!IsPriorityGreaterThan(fixture.RouteCamera, fixture.ActivityCamera))
            {
                return Fail("route-priority-does-not-override-cleared-activity", $"routePriority='{RoutePriority}' activityPriority='{ClearedPriority}'");
            }

            if (!AssertLegacyStateUntouched(fixture, "activity-exit-legacy-state"))
            {
                return false;
            }

            Debug.Log($"{LogPrefix} step='activity-exit-restored-route' clearStatus='{clearDiagnostic.Status}' routeStatus='{routeDiagnostic.Status}' routePriority='{RoutePriority}' activityPriority='{ClearedPriority}'");
            return true;
        }

        private static bool RunUseRouteCase(SmokeFixture fixture)
        {
            CinemachineCameraOutputDiagnostic clearDiagnostic = ApplyActivityClear(fixture);
            if (!clearDiagnostic.IsSucceeded)
            {
                return Fail("use-route-clear-precondition-failed", FormatDiagnostic(clearDiagnostic));
            }

            CinemachineCameraOutputDiagnostic routeDiagnostic = ApplyRoute(fixture);
            if (!routeDiagnostic.IsSucceeded)
            {
                return Fail("use-route-route-apply-failed", FormatDiagnostic(routeDiagnostic));
            }

            if (!AssertPriority(fixture.RouteCamera, RoutePriority, "use-route-route-priority-invalid"))
            {
                return false;
            }

            if (!AssertPriority(fixture.ActivityCamera, ClearedPriority, "use-route-activity-priority-mutated"))
            {
                return false;
            }

            Debug.Log($"{LogPrefix} step='activity-use-route' status='{routeDiagnostic.Status}' routePriority='{RoutePriority}' activityPriority='{ClearedPriority}'");
            return true;
        }

        private static bool RunRetainedActivityCase(SmokeFixture fixture)
        {
            CinemachineCameraOutputDiagnostic routeDiagnostic = ApplyRoute(fixture);
            if (!routeDiagnostic.IsSucceeded)
            {
                return Fail("retained-activity-route-precondition-failed", FormatDiagnostic(routeDiagnostic));
            }

            CinemachineCameraOutputDiagnostic retainedDiagnostic = ApplyRetainedActivity(fixture);
            if (!retainedDiagnostic.IsSucceeded)
            {
                return Fail("retained-activity-not-applied", FormatDiagnostic(retainedDiagnostic));
            }

            if (!AssertPriority(fixture.RetainedActivityCamera, RetainedActivityPriority, "retained-activity-priority-not-applied"))
            {
                return false;
            }

            if (!AssertTargets(fixture.RetainedActivityCamera, fixture.RetainedFollow, fixture.RetainedLookAt, "retained-activity-targets-not-applied"))
            {
                return false;
            }

            if (!IsPriorityGreaterThan(fixture.RetainedActivityCamera, fixture.RouteCamera))
            {
                return Fail("retained-activity-priority-does-not-override-route", $"routePriority='{RoutePriority}' retainedPriority='{RetainedActivityPriority}'");
            }

            Debug.Log($"{LogPrefix} step='retained-activity-applied' status='{retainedDiagnostic.Status}' routePriority='{RoutePriority}' retainedPriority='{RetainedActivityPriority}'");
            return true;
        }

        private static bool RunMissingRequiredActivityOutputCase(SmokeFixture fixture)
        {
            CinemachineCameraOutputDiagnostic routeDiagnostic = ApplyRoute(fixture);
            if (!routeDiagnostic.IsSucceeded)
            {
                return Fail("missing-required-activity-route-precondition-failed", FormatDiagnostic(routeDiagnostic));
            }

            CinemachineCameraOutput missingActivityOutput = CinemachineCameraOutput.Missing(
                "qa.c8b3.activity.required-missing",
                "QA C8B3 Required Activity Missing",
                required: true,
                priority: ActivityPriority);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Apply(
                missingActivityOutput,
                requireFollowTarget: true,
                requireLookAtTarget: false,
                explicitBrainScope: new[] { fixture.Brain });

            if (!diagnostic.IsBlocked || diagnostic.Code != CinemachineCameraOutputDiagnostic.CameraOutputMissing)
            {
                return Fail("missing-required-activity-not-blocked", FormatDiagnostic(diagnostic));
            }

            if (!AssertPriority(fixture.RouteCamera, RoutePriority, "missing-required-activity-mutated-route"))
            {
                return false;
            }

            Debug.Log($"{LogPrefix} step='missing-required-activity-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}' routePriority='{RoutePriority}'");
            return true;
        }

        private static bool RunMissingOptionalActivityOutputCase()
        {
            CinemachineCameraOutput missingActivityOutput = CinemachineCameraOutput.Missing(
                "qa.c8b3.activity.optional-missing",
                "QA C8B3 Optional Activity Missing",
                required: false,
                priority: ActivityPriority);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Apply(
                missingActivityOutput,
                requireFollowTarget: false,
                requireLookAtTarget: false);

            if (!diagnostic.IsSkipped || diagnostic.Code != CinemachineCameraOutputDiagnostic.OptionalOutputSkipped)
            {
                return Fail("missing-optional-activity-not-skipped", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='missing-optional-activity-skipped' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static bool RunWrongBrainScopeBlocksCase(SmokeFixture fixture)
        {
            BrainFixture wrongBrain = CreateUnityCameraWithBrain(fixture.Root.transform, "WrongScope_UnityCamera");
            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Apply(
                CreateActivityOutput(fixture, ActivityPriority),
                requireFollowTarget: true,
                requireLookAtTarget: true,
                explicitBrainScope: new[] { wrongBrain.Brain });

            if (!diagnostic.IsBlocked || diagnostic.Code != CinemachineCameraOutputDiagnostic.BrainScopeMismatch)
            {
                return Fail("wrong-brain-scope-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='wrong-brain-scope-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static CinemachineCameraOutputDiagnostic ApplyRoute(SmokeFixture fixture)
        {
            return FrameworkCinemachineOutputApplier.Apply(
                CreateRouteOutput(fixture, RoutePriority),
                requireFollowTarget: true,
                requireLookAtTarget: true,
                explicitBrainScope: new[] { fixture.Brain });
        }

        private static CinemachineCameraOutputDiagnostic ApplyActivity(SmokeFixture fixture)
        {
            return FrameworkCinemachineOutputApplier.Apply(
                CreateActivityOutput(fixture, ActivityPriority),
                requireFollowTarget: true,
                requireLookAtTarget: true,
                explicitBrainScope: new[] { fixture.Brain });
        }

        private static CinemachineCameraOutputDiagnostic ApplyActivityClear(SmokeFixture fixture)
        {
            return FrameworkCinemachineOutputApplier.Apply(
                CreateActivityOutput(fixture, ClearedPriority),
                requireFollowTarget: true,
                requireLookAtTarget: true,
                explicitBrainScope: new[] { fixture.Brain });
        }

        private static CinemachineCameraOutputDiagnostic ApplyRetainedActivity(SmokeFixture fixture)
        {
            return FrameworkCinemachineOutputApplier.Apply(
                CreateRetainedActivityOutput(fixture, RetainedActivityPriority),
                requireFollowTarget: true,
                requireLookAtTarget: true,
                explicitBrainScope: new[] { fixture.Brain });
        }

        private static CinemachineCameraOutput CreateRouteOutput(SmokeFixture fixture, int priority)
        {
            return new CinemachineCameraOutput(
                "qa.c8b3.route",
                "QA C8B3 Route Output",
                fixture.RouteCamera,
                fixture.Brain,
                fixture.RouteFollow,
                fixture.RouteLookAt,
                priority,
                true);
        }

        private static CinemachineCameraOutput CreateActivityOutput(SmokeFixture fixture, int priority)
        {
            return new CinemachineCameraOutput(
                "qa.c8b3.activity",
                "QA C8B3 Activity Output",
                fixture.ActivityCamera,
                fixture.Brain,
                fixture.ActivityFollow,
                fixture.ActivityLookAt,
                priority,
                true);
        }

        private static CinemachineCameraOutput CreateRetainedActivityOutput(SmokeFixture fixture, int priority)
        {
            return new CinemachineCameraOutput(
                "qa.c8b3.retained-activity",
                "QA C8B3 Retained Activity Output",
                fixture.RetainedActivityCamera,
                fixture.Brain,
                fixture.RetainedFollow,
                fixture.RetainedLookAt,
                priority,
                true);
        }

        private static bool AssertTargets(CinemachineCamera camera, Transform expectedFollow, Transform expectedLookAt, string code)
        {
            if (camera.Target.TrackingTarget != expectedFollow)
            {
                return Fail(code, $"TrackingTarget mismatch. expected='{expectedFollow.name}' actual='{FormatName(camera.Target.TrackingTarget)}'");
            }

            if (camera.Target.LookAtTarget != expectedLookAt)
            {
                return Fail(code, $"LookAtTarget mismatch. expected='{expectedLookAt.name}' actual='{FormatName(camera.Target.LookAtTarget)}'");
            }

            return true;
        }

        private static bool AssertPriority(CinemachineCamera camera, int expectedPriority, string code)
        {
            if (camera.Priority != expectedPriority)
            {
                return Fail(code, $"expected='{expectedPriority}' actual='PrioritySettings did not match expected value' camera='{camera.name}'");
            }

            return true;
        }

        private static bool IsPriorityGreaterThan(CinemachineCamera left, CinemachineCamera right)
        {
            return left.Priority > right.Priority;
        }

        private static bool AssertLegacyStateUntouched(SmokeFixture fixture, string code)
        {
            if (fixture.UnityCamera.enabled)
            {
                return Fail(code, "Unity Camera.enabled was mutated by the Cinemachine output binding smoke.");
            }

            if (fixture.Root.activeSelf != fixture.RootActiveBefore ||
                fixture.RouteCamera.gameObject.activeSelf != fixture.RouteObjectActiveBefore ||
                fixture.ActivityCamera.gameObject.activeSelf != fixture.ActivityObjectActiveBefore ||
                fixture.RetainedActivityCamera.gameObject.activeSelf != fixture.RetainedObjectActiveBefore)
            {
                return Fail(code, "GameObject active state was mutated by the Cinemachine output binding smoke.");
            }

            return true;
        }

        private static SmokeFixture CreateFixture(Transform parent)
        {
            GameObject root = CreateHiddenChild(parent, "QA_C8B3_SharedCameraScope");
            BrainFixture brainFixture = CreateUnityCameraWithBrain(root.transform, "Shared_UnityCamera");
            brainFixture.UnityCamera.enabled = false;

            CameraRigFixture route = CreateCinemachineRig(root.transform, "Route", RoutePriority);
            CameraRigFixture activity = CreateCinemachineRig(root.transform, "Activity", ClearedPriority);
            CameraRigFixture retained = CreateCinemachineRig(root.transform, "RetainedActivity", ClearedPriority);

            return new SmokeFixture(
                root,
                brainFixture.UnityCamera,
                brainFixture.Brain,
                route.Camera,
                route.FollowTarget,
                route.LookAtTarget,
                activity.Camera,
                activity.FollowTarget,
                activity.LookAtTarget,
                retained.Camera,
                retained.FollowTarget,
                retained.LookAtTarget);
        }

        private static CameraRigFixture CreateCinemachineRig(Transform parent, string label, int initialPriority)
        {
            GameObject cameraObject = CreateHiddenChild(parent, $"{label}_CinemachineCamera");
            CinemachineCamera cinemachineCamera = cameraObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Priority = initialPriority;

            GameObject follow = CreateHiddenChild(parent, $"{label}_FollowTarget");
            GameObject lookAt = CreateHiddenChild(parent, $"{label}_LookAtTarget");

            return new CameraRigFixture(cinemachineCamera, follow.transform, lookAt.transform);
        }

        private static BrainFixture CreateUnityCameraWithBrain(Transform parent, string label)
        {
            GameObject cameraObject = CreateHiddenChild(parent, label);
            UnityEngine.Camera unityCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();
            return new BrainFixture(unityCamera, brain);
        }

        private static GameObject CreateHiddenRoot(string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            return gameObject;
        }

        private static GameObject CreateHiddenChild(Transform parent, string name)
        {
            GameObject gameObject = CreateHiddenRoot(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static bool Fail(string code, string detail)
        {
            Debug.LogError($"{LogPrefix} FAIL. code='{code}' detail='{detail}'");
            return false;
        }

        private static string FormatDiagnostic(CinemachineCameraOutputDiagnostic diagnostic)
        {
            return $"status='{diagnostic.Status}' code='{diagnostic.Code}' outputId='{diagnostic.OutputId}' message='{diagnostic.Message}'";
        }

        private static string FormatName(Object value)
        {
            return value != null ? value.name : "<null>";
        }

        private readonly struct SmokeFixture
        {
            public SmokeFixture(
                GameObject root,
                UnityEngine.Camera unityCamera,
                CinemachineBrain brain,
                CinemachineCamera routeCamera,
                Transform routeFollow,
                Transform routeLookAt,
                CinemachineCamera activityCamera,
                Transform activityFollow,
                Transform activityLookAt,
                CinemachineCamera retainedActivityCamera,
                Transform retainedFollow,
                Transform retainedLookAt)
            {
                Root = root;
                UnityCamera = unityCamera;
                Brain = brain;
                RouteCamera = routeCamera;
                RouteFollow = routeFollow;
                RouteLookAt = routeLookAt;
                ActivityCamera = activityCamera;
                ActivityFollow = activityFollow;
                ActivityLookAt = activityLookAt;
                RetainedActivityCamera = retainedActivityCamera;
                RetainedFollow = retainedFollow;
                RetainedLookAt = retainedLookAt;
                RootActiveBefore = root.activeSelf;
                RouteObjectActiveBefore = routeCamera.gameObject.activeSelf;
                ActivityObjectActiveBefore = activityCamera.gameObject.activeSelf;
                RetainedObjectActiveBefore = retainedActivityCamera.gameObject.activeSelf;
            }

            public GameObject Root { get; }

            public UnityEngine.Camera UnityCamera { get; }

            public CinemachineBrain Brain { get; }

            public CinemachineCamera RouteCamera { get; }

            public Transform RouteFollow { get; }

            public Transform RouteLookAt { get; }

            public CinemachineCamera ActivityCamera { get; }

            public Transform ActivityFollow { get; }

            public Transform ActivityLookAt { get; }

            public CinemachineCamera RetainedActivityCamera { get; }

            public Transform RetainedFollow { get; }

            public Transform RetainedLookAt { get; }

            public bool RootActiveBefore { get; }

            public bool RouteObjectActiveBefore { get; }

            public bool ActivityObjectActiveBefore { get; }

            public bool RetainedObjectActiveBefore { get; }
        }

        private readonly struct CameraRigFixture
        {
            public CameraRigFixture(CinemachineCamera camera, Transform followTarget, Transform lookAtTarget)
            {
                Camera = camera;
                FollowTarget = followTarget;
                LookAtTarget = lookAtTarget;
            }

            public CinemachineCamera Camera { get; }

            public Transform FollowTarget { get; }

            public Transform LookAtTarget { get; }
        }

        private readonly struct BrainFixture
        {
            public BrainFixture(UnityEngine.Camera unityCamera, CinemachineBrain brain)
            {
                UnityCamera = unityCamera;
                Brain = brain;
            }

            public UnityEngine.Camera UnityCamera { get; }

            public CinemachineBrain Brain { get; }
        }
    }
}

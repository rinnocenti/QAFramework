using System;
using System.Reflection;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Camera;
using Immersive.Framework.Camera.Cinemachine;
using Immersive.Framework.RouteLifecycle;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    public static class QaC8B4CCanonicalRouteActivityCinemachineSmoke
    {
        private const string LogPrefix = "[QA][C8B4C Canonical RouteActivity Cinemachine]";
        private const int InitialPriority = 1;
        private const int RoutePriority = 10;
        private const int ActivityPriority = 100;

        [MenuItem("Immersive Framework/QA/Camera/C8B4C Canonical Route Activity Cinemachine Smoke")]
        public static void Run()
        {
            if (RunForRegression())
            {
                Debug.Log($"{LogPrefix} PASS. Canonical Route/Activity bindings apply only explicit Cinemachine outputs without legacy camera authority.");
            }
        }

        public static bool RunForRegression()
        {
            Debug.Log($"{LogPrefix} Smoke started.");
            GameObject root = null;
            try
            {
                root = CreateHiddenRoot("QA_C8B4C_CanonicalRouteActivityCinemachine_Root");
                SmokeFixture fixture = CreateFixture(root.transform);

                return RunRouteCase(fixture)
                    && RunActivityCase(fixture)
                    && RunUseRouteCase(fixture)
                    && RunRequiredRouteMissingCase(fixture)
                    && RunRequiredActivityMissingCase(fixture)
                    && RunOptionalMissingCase(fixture)
                    && AssertCanonicalState(fixture, "final-canonical-state");
            }
            catch (Exception exception)
            {
                return Fail("unexpected-exception", exception.ToString());
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static bool RunRouteCase(SmokeFixture fixture)
        {
            ConfigureSource(fixture.RouteSource, fixture.Route.Camera, fixture.Brain, fixture.Route.Follow, fixture.Route.LookAt, RoutePriority, true, "qa.c8b4c.route");
            InvokeRoute(fixture.RouteBinding);

            if (fixture.Route.Camera.Priority != RoutePriority ||
                fixture.Route.Camera.Target.TrackingTarget != fixture.Route.Follow ||
                fixture.Route.Camera.Target.LookAtTarget != fixture.Route.LookAt)
            {
                return Fail("route-output-not-applied", "Route binding did not apply the explicit Cinemachine output.");
            }

            if (!AssertCanonicalState(fixture, "route-canonical-state"))
            {
                return false;
            }

            Debug.Log($"{LogPrefix} step='route-binding-output-applied' priority='{RoutePriority}'");
            return true;
        }

        private static bool RunActivityCase(SmokeFixture fixture)
        {
            ConfigureSource(fixture.ActivitySource, fixture.Activity.Camera, fixture.Brain, fixture.Activity.Follow, fixture.Activity.LookAt, ActivityPriority, true, "qa.c8b4c.activity");
            SetEnum(fixture.ActivityBinding, "policy", FrameworkCameraActivityPolicy.UseOwn);
            InvokeActivity(fixture.ActivityBinding);

            if (fixture.Activity.Camera.Priority != ActivityPriority ||
                fixture.Activity.Camera.Target.TrackingTarget != fixture.Activity.Follow ||
                fixture.Activity.Camera.Target.LookAtTarget != fixture.Activity.LookAt)
            {
                return Fail("activity-output-not-applied", "Activity binding did not apply the explicit Cinemachine output.");
            }

            Debug.Log($"{LogPrefix} step='activity-binding-output-applied' priority='{ActivityPriority}'");
            return true;
        }

        private static bool RunUseRouteCase(SmokeFixture fixture)
        {
            fixture.Activity.Camera.Priority = InitialPriority;
            SetEnum(fixture.ActivityBinding, "policy", FrameworkCameraActivityPolicy.UseRoute);
            InvokeActivity(fixture.ActivityBinding);

            if (fixture.Activity.Camera.Priority != InitialPriority)
            {
                return Fail("activity-use-route-applied-override", "UseRoute changed the Activity Cinemachine output.");
            }

            Debug.Log($"{LogPrefix} step='activity-use-route-preserved' activityPriority='{fixture.Activity.Camera.Priority}'");
            return true;
        }

        private static bool RunRequiredRouteMissingCase(SmokeFixture fixture)
        {
            fixture.Route.Camera.Priority = InitialPriority;
            ConfigureSource(fixture.RouteSource, null, fixture.Brain, fixture.Route.Follow, fixture.Route.LookAt, RoutePriority, true, "qa.c8b4c.route-required-missing");
            InvokeRoute(fixture.RouteBinding);

            fixture.RouteSource.TryCreateOutput(out _, out CinemachineCameraOutputDiagnostic diagnostic);
            if (!diagnostic.IsBlocked || fixture.Route.Camera.Priority != InitialPriority)
            {
                return Fail("required-route-output-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='required-route-output-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static bool RunRequiredActivityMissingCase(SmokeFixture fixture)
        {
            SetEnum(fixture.ActivityBinding, "policy", FrameworkCameraActivityPolicy.UseOwn);
            fixture.Activity.Camera.Priority = InitialPriority;
            ConfigureSource(fixture.ActivitySource, null, fixture.Brain, fixture.Activity.Follow, fixture.Activity.LookAt, ActivityPriority, true, "qa.c8b4c.activity-required-missing");
            InvokeActivity(fixture.ActivityBinding);

            fixture.ActivitySource.TryCreateOutput(out _, out CinemachineCameraOutputDiagnostic diagnostic);
            if (!diagnostic.IsBlocked || fixture.Activity.Camera.Priority != InitialPriority)
            {
                return Fail("required-activity-output-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='required-activity-output-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static bool RunOptionalMissingCase(SmokeFixture fixture)
        {
            ConfigureSource(fixture.RouteSource, null, fixture.Brain, fixture.Route.Follow, fixture.Route.LookAt, RoutePriority, false, "qa.c8b4c.route-optional-missing");
            InvokeRoute(fixture.RouteBinding);

            fixture.RouteSource.TryCreateOutput(out _, out CinemachineCameraOutputDiagnostic diagnostic);
            if (!diagnostic.IsSkipped)
            {
                return Fail("optional-output-not-skipped", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='optional-output-skipped' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static void InvokeRoute(FrameworkRouteCameraBinding binding)
        {
            ((IRouteContentLifecycleReceiver)binding).OnRouteContentEntered(
                CreateContext<RouteContentLifecycleContext>(typeof(RouteContentLifecycleContext), typeof(RouteContentLifecyclePhase)));
        }

        private static void InvokeActivity(FrameworkActivityCameraBinding binding)
        {
            ((IActivityContentLifecycleReceiver)binding).OnActivityContentEntered(
                CreateContext<ActivityContentLifecycleContext>(typeof(ActivityContentLifecycleContext), typeof(ActivityContentLifecyclePhase)));
        }

        private static T CreateContext<T>(Type contextType, Type phaseType)
        {
            ConstructorInfo constructor = null;
            foreach (ConstructorInfo candidate in contextType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length == 8 && parameters[0].ParameterType == phaseType)
                {
                    constructor = candidate;
                    break;
                }
            }

            if (constructor == null)
            {
                throw new MissingMethodException(contextType.FullName, ".ctor");
            }

            object phase = Enum.Parse(phaseType, "Entered", false);
            return (T)constructor.Invoke(new object[] { phase, null, null, null, null, null, "QA", "C8B4C" });
        }

        private static SmokeFixture CreateFixture(Transform parent)
        {
            GameObject cameraObject = CreateHiddenChild(parent, "UnityCamera");
            UnityEngine.Camera unityCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            unityCamera.enabled = false;
            CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();

            CameraRigFixture route = CreateRig(parent, "Route");
            CameraRigFixture activity = CreateRig(parent, "Activity");
            FrameworkRouteCameraBinding routeBinding = parent.gameObject.AddComponent<FrameworkRouteCameraBinding>();
            FrameworkActivityCameraBinding activityBinding = parent.gameObject.AddComponent<FrameworkActivityCameraBinding>();
            FrameworkCinemachineCameraOutputSource routeSource = route.Rig.AddComponent<FrameworkCinemachineCameraOutputSource>();
            FrameworkCinemachineCameraOutputSource activitySource = activity.Rig.AddComponent<FrameworkCinemachineCameraOutputSource>();

            SetReference(routeBinding, "cinemachineOutputSource", routeSource);
            SetReference(activityBinding, "cinemachineOutputSource", activitySource);
            return new SmokeFixture(parent.gameObject, unityCamera, brain, route, activity, routeBinding, activityBinding, routeSource, activitySource);
        }

        private static CameraRigFixture CreateRig(Transform parent, string label)
        {
            GameObject rig = CreateHiddenChild(parent, $"{label}Rig");
            CinemachineCamera camera = CreateHiddenChild(rig.transform, $"{label}Camera").AddComponent<CinemachineCamera>();
            camera.Priority = InitialPriority;
            Transform follow = CreateHiddenChild(rig.transform, $"{label}Follow").transform;
            Transform lookAt = CreateHiddenChild(rig.transform, $"{label}LookAt").transform;
            return new CameraRigFixture(rig, camera, follow, lookAt);
        }

        private static void ConfigureSource(FrameworkCinemachineCameraOutputSource source, CinemachineCamera camera, CinemachineBrain brain, Transform follow, Transform lookAt, int priority, bool required, string outputId)
        {
            SerializedObject serialized = new SerializedObject(source);
            serialized.FindProperty("cinemachineCamera").objectReferenceValue = camera;
            serialized.FindProperty("cinemachineBrain").objectReferenceValue = brain;
            serialized.FindProperty("followTarget").objectReferenceValue = follow;
            serialized.FindProperty("lookAtTarget").objectReferenceValue = lookAt;
            serialized.FindProperty("priority").intValue = priority;
            serialized.FindProperty("required").boolValue = required;
            serialized.FindProperty("outputId").stringValue = outputId;
            serialized.FindProperty("displayName").stringValue = outputId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(UnityEngine.Object target, string propertyName, Enum value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.enumValueIndex = Array.IndexOf(property.enumNames, value.ToString());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool AssertCanonicalState(SmokeFixture fixture, string code)
        {
            if (fixture.UnityCamera.enabled != fixture.InitialCameraEnabled ||
                fixture.Root.activeSelf != fixture.InitialRootActive ||
                fixture.Route.Rig.activeSelf != fixture.InitialRouteActive ||
                fixture.Activity.Rig.activeSelf != fixture.InitialActivityActive)
            {
                return Fail(code, "Canonical binding mutated Camera.enabled or GameObject.activeSelf.");
            }

            return true;
        }

        private static GameObject CreateHiddenRoot(string name)
        {
            return new GameObject(name) { hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild };
        }

        private static GameObject CreateHiddenChild(Transform parent, string name)
        {
            GameObject child = CreateHiddenRoot(name);
            child.transform.SetParent(parent, false);
            return child;
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

        private readonly struct CameraRigFixture
        {
            public CameraRigFixture(GameObject rig, CinemachineCamera camera, Transform follow, Transform lookAt)
            {
                Rig = rig;
                Camera = camera;
                Follow = follow;
                LookAt = lookAt;
            }

            public GameObject Rig { get; }
            public CinemachineCamera Camera { get; }
            public Transform Follow { get; }
            public Transform LookAt { get; }
        }

        private readonly struct SmokeFixture
        {
            public SmokeFixture(GameObject root, UnityEngine.Camera unityCamera, CinemachineBrain brain, CameraRigFixture route, CameraRigFixture activity, FrameworkRouteCameraBinding routeBinding, FrameworkActivityCameraBinding activityBinding, FrameworkCinemachineCameraOutputSource routeSource, FrameworkCinemachineCameraOutputSource activitySource)
            {
                Root = root;
                UnityCamera = unityCamera;
                Brain = brain;
                Route = route;
                Activity = activity;
                RouteBinding = routeBinding;
                ActivityBinding = activityBinding;
                RouteSource = routeSource;
                ActivitySource = activitySource;
                InitialCameraEnabled = unityCamera.enabled;
                InitialRootActive = root.activeSelf;
                InitialRouteActive = route.Rig.activeSelf;
                InitialActivityActive = activity.Rig.activeSelf;
            }

            public GameObject Root { get; }
            public UnityEngine.Camera UnityCamera { get; }
            public CinemachineBrain Brain { get; }
            public CameraRigFixture Route { get; }
            public CameraRigFixture Activity { get; }
            public FrameworkRouteCameraBinding RouteBinding { get; }
            public FrameworkActivityCameraBinding ActivityBinding { get; }
            public FrameworkCinemachineCameraOutputSource RouteSource { get; }
            public FrameworkCinemachineCameraOutputSource ActivitySource { get; }
            public bool InitialCameraEnabled { get; }
            public bool InitialRootActive { get; }
            public bool InitialRouteActive { get; }
            public bool InitialActivityActive { get; }
        }
    }
}

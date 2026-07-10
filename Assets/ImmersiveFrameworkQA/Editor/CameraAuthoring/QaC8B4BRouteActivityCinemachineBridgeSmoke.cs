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
    public static class QaC8B4BRouteActivityCinemachineBridgeSmoke
    {
        private const string LogPrefix = "[QA][C8B4B RouteActivity Cinemachine Bridge]";
        private const int InitialPriority = 1;
        private const int RoutePriority = 10;
        private const int ActivityPriority = 100;

        [MenuItem("Immersive Framework/QA/Camera/C8B4B Route Activity Cinemachine Bridge Smoke")]
        public static void Run()
        {
            if (RunForRegression())
            {
                Debug.Log($"{LogPrefix} PASS. Real Route/Activity bindings consume explicit Cinemachine outputs while preserving the legacy camera director path.");
            }
        }

        public static bool RunForRegression()
        {
            Debug.Log($"{LogPrefix} Smoke started.");

            GameObject root = null;
            try
            {
                root = CreateHiddenRoot("QA_C8B4B_RouteActivityCinemachineBridge_Root");
                SmokeFixture fixture = CreateFixture(root.transform);

                if (!RunRouteBindingCase(fixture) ||
                    !RunActivityBindingCase(fixture) ||
                    !RunActivityUseRouteCase(fixture) ||
                    !RunRequiredMissingOutputCase(fixture) ||
                    !RunOptionalMissingOutputCase(fixture) ||
                    !RunLegacyPathCase(fixture))
                {
                    return false;
                }

                return true;
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

        private static bool RunRouteBindingCase(SmokeFixture fixture)
        {
            ConfigureOutputSource(fixture.RouteSource, fixture.RouteCamera, fixture.Brain, fixture.RouteFollow, fixture.RouteLookAt, RoutePriority, true, "qa.c8b4b.route");
            InvokeRoute(fixture.RouteBinding);

            if (fixture.RouteCamera.Priority != RoutePriority ||
                fixture.RouteCamera.Target.TrackingTarget != fixture.RouteFollow ||
                fixture.RouteCamera.Target.LookAtTarget != fixture.RouteLookAt ||
                fixture.Director.CurrentRouteRig != fixture.RouteRig)
            {
                return Fail("route-binding-output-not-applied", "The real Route binding did not apply its explicit Cinemachine output and legacy route state.");
            }

            if (!AssertLegacyStateUntouched(fixture, "route-binding-legacy-state-mutated"))
            {
                return false;
            }

            Debug.Log($"{LogPrefix} step='route-binding-output-applied' priority='{RoutePriority}' follow='{fixture.RouteFollow.name}' lookAt='{fixture.RouteLookAt.name}'");
            return true;
        }

        private static bool RunActivityBindingCase(SmokeFixture fixture)
        {
            ConfigureOutputSource(fixture.ActivitySource, fixture.ActivityCamera, fixture.Brain, fixture.ActivityFollow, fixture.ActivityLookAt, ActivityPriority, true, "qa.c8b4b.activity");
            SetSerializedEnum(fixture.ActivityBinding, "policy", FrameworkCameraActivityPolicy.UseOwnOrRoute);
            InvokeActivity(fixture.ActivityBinding);

            if (fixture.ActivityCamera.Priority != ActivityPriority ||
                fixture.ActivityCamera.Target.TrackingTarget != fixture.ActivityFollow ||
                fixture.ActivityCamera.Target.LookAtTarget != fixture.ActivityLookAt ||
                fixture.Director.CurrentActivityRig != fixture.ActivityRig ||
                fixture.ActivityCamera.Priority <= fixture.RouteCamera.Priority)
            {
                return Fail("activity-binding-output-not-applied", "The real Activity binding did not apply an explicit override above Route.");
            }

            Debug.Log($"{LogPrefix} step='activity-binding-output-applied' priority='{ActivityPriority}' routePriority='{RoutePriority}'");
            return true;
        }

        private static bool RunActivityUseRouteCase(SmokeFixture fixture)
        {
            fixture.ActivityCamera.Priority = InitialPriority;
            SetSerializedEnum(fixture.ActivityBinding, "policy", FrameworkCameraActivityPolicy.UseRoute);
            InvokeActivity(fixture.ActivityBinding);

            if (fixture.ActivityCamera.Priority != InitialPriority ||
                fixture.RouteCamera.Priority != RoutePriority ||
                fixture.Director.CurrentActivityRig != null)
            {
                return Fail("activity-use-route-mutated-override", "UseRoute applied an Activity override or changed the Route result.");
            }

            Debug.Log($"{LogPrefix} step='activity-use-route-preserved' routePriority='{RoutePriority}' activityPriority='{InitialPriority}'");
            return true;
        }

        private static bool RunRequiredMissingOutputCase(SmokeFixture fixture)
        {
            fixture.RouteCamera.Priority = InitialPriority;
            ConfigureOutputSource(fixture.RouteSource, null, fixture.Brain, fixture.RouteFollow, fixture.RouteLookAt, RoutePriority, true, "qa.c8b4b.required-missing");
            InvokeRoute(fixture.RouteBinding);

            if (!TryGetSourceDiagnostic(fixture.RouteSource, out CinemachineCameraOutputDiagnostic diagnostic) ||
                !diagnostic.IsBlocked)
            {
                return Fail("required-output-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='required-output-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}' outputId='{diagnostic.OutputId}' routePriority='{fixture.RouteCamera.Priority}'");
            return true;
        }

        private static bool RunOptionalMissingOutputCase(SmokeFixture fixture)
        {
            fixture.RouteCamera.Priority = InitialPriority;
            ConfigureOutputSource(fixture.RouteSource, null, fixture.Brain, fixture.RouteFollow, fixture.RouteLookAt, RoutePriority, false, "qa.c8b4b.optional-missing");
            InvokeRoute(fixture.RouteBinding);

            if (!TryGetSourceDiagnostic(fixture.RouteSource, out CinemachineCameraOutputDiagnostic diagnostic) ||
                !diagnostic.IsSkipped ||
                diagnostic.Code != CinemachineCameraOutputDiagnostic.OptionalOutputSkipped ||
                fixture.Director.CurrentRouteRig != fixture.RouteRig)
            {
                return Fail("optional-output-not-skipped", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='optional-output-skipped-legacy-preserved' status='{diagnostic.Status}' code='{diagnostic.Code}' outputId='{diagnostic.OutputId}'");
            return true;
        }

        private static bool RunLegacyPathCase(SmokeFixture fixture)
        {
            SetSerializedObjectReference(fixture.RouteBinding, "cinemachineOutputSource", null);
            InvokeRoute(fixture.RouteBinding);
            SetSerializedObjectReference(fixture.ActivityBinding, "cinemachineOutputSource", null);
            SetSerializedEnum(fixture.ActivityBinding, "policy", FrameworkCameraActivityPolicy.UseOwnOrRoute);
            InvokeActivity(fixture.ActivityBinding);

            if (fixture.Director.CurrentRouteRig != fixture.RouteRig || fixture.Director.CurrentActivityRig != fixture.ActivityRig)
            {
                return Fail("legacy-path-not-preserved", "The real bindings no longer delivered their legacy rig state to FrameworkCameraDirector.");
            }

            Debug.Log($"{LogPrefix} step='legacy-path-preserved' routeRig='{fixture.Director.CurrentRouteRig.name}' activityRig='{fixture.Director.CurrentActivityRig.name}'");
            return true;
        }

        private static void InvokeRoute(FrameworkRouteCameraBinding binding)
        {
            ((IRouteContentLifecycleReceiver)binding).OnRouteContentEntered(CreateLifecycleContext<RouteContentLifecycleContext>(
                typeof(RouteContentLifecycleContext), "Entered", null, null, null, null, "QA", "C8B4B"));
        }

        private static void InvokeActivity(FrameworkActivityCameraBinding binding)
        {
            ((IActivityContentLifecycleReceiver)binding).OnActivityContentEntered(CreateLifecycleContext<ActivityContentLifecycleContext>(
                typeof(ActivityContentLifecycleContext), "Entered", null, null, null, null, "QA", "C8B4B"));
        }

        private static T CreateLifecycleContext<T>(Type contextType, string phaseName, object first, object second, object third, object binding, string source, string reason)
        {
            Type phaseType = contextType == typeof(RouteContentLifecycleContext)
                ? typeof(RouteContentLifecyclePhase)
                : typeof(ActivityContentLifecyclePhase);
            object phase = Enum.Parse(phaseType, phaseName, false);
            ConstructorInfo constructor = FindContextConstructor(contextType, phaseType);

            if (constructor == null)
            {
                constructor = FindContextConstructor(contextType, phaseType);
            }

            object[] args = contextType == typeof(RouteContentLifecycleContext)
                ? new object[] { phase, null, null, null, null, null, source, reason }
                : new object[] { phase, null, null, null, null, null, source, reason };
            return (T)constructor.Invoke(args);
        }

        private static ConstructorInfo FindContextConstructor(Type contextType, Type phaseType)
        {
            foreach (ConstructorInfo constructor in contextType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                if (parameters.Length == 8 && parameters[0].ParameterType == phaseType)
                {
                    return constructor;
                }
            }

            throw new MissingMethodException(contextType.FullName, ".ctor");
        }

        private static void ConfigureOutputSource(FrameworkCinemachineCameraOutputSource source, CinemachineCamera camera, CinemachineBrain brain, Transform follow, Transform lookAt, int priority, bool required, string outputId)
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

        private static void SetSerializedObjectReference(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingMemberException(target.GetType().FullName, propertyName);
            }

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = value;
            }
            else if (property.propertyType == SerializedPropertyType.Enum)
            {
                property.enumValueIndex = Array.IndexOf(property.enumNames, value.ToString());
            }
            else
            {
                throw new InvalidOperationException($"Unsupported serialized property type '{property.propertyType}'.");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedEnum(UnityEngine.Object target, string propertyName, Enum value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.Enum)
            {
                throw new MissingMemberException(target.GetType().FullName, propertyName);
            }

            property.enumValueIndex = Array.IndexOf(property.enumNames, value.ToString());
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool TryGetSourceDiagnostic(FrameworkCinemachineCameraOutputSource source, out CinemachineCameraOutputDiagnostic diagnostic)
        {
            return source.TryCreateOutput(out _, out diagnostic);
        }

        private static SmokeFixture CreateFixture(Transform parent)
        {
            GameObject directorObject = CreateHiddenChild(parent, "Director");
            FrameworkCameraDirector director = directorObject.AddComponent<FrameworkCameraDirector>();
            SetSerializedBool(director, "setRigActiveState", false);

            GameObject unityCameraObject = CreateHiddenChild(parent, "UnityCamera");
            UnityEngine.Camera unityCamera = unityCameraObject.AddComponent<UnityEngine.Camera>();
            unityCamera.enabled = false;
            CinemachineBrain brain = unityCameraObject.AddComponent<CinemachineBrain>();

            CameraRigFixture route = CreateCameraRig(parent, "Route");
            CameraRigFixture activity = CreateCameraRig(parent, "Activity");
            FrameworkRouteCameraBinding routeBinding = parent.gameObject.AddComponent<FrameworkRouteCameraBinding>();
            FrameworkActivityCameraBinding activityBinding = parent.gameObject.AddComponent<FrameworkActivityCameraBinding>();
            FrameworkCinemachineCameraOutputSource routeSource = route.Rig.AddComponent<FrameworkCinemachineCameraOutputSource>();
            FrameworkCinemachineCameraOutputSource activitySource = activity.Rig.AddComponent<FrameworkCinemachineCameraOutputSource>();

            SetSerializedObjectReference(routeBinding, "routeCameraRig", route.Rig);
            SetSerializedObjectReference(routeBinding, "director", director);
            SetSerializedObjectReference(routeBinding, "cinemachineOutputSource", routeSource);
            SetSerializedObjectReference(activityBinding, "activityCameraRig", activity.Rig);
            SetSerializedObjectReference(activityBinding, "director", director);
            SetSerializedObjectReference(activityBinding, "cinemachineOutputSource", activitySource);

            return new SmokeFixture(parent.gameObject, director, unityCamera, brain, route.Rig, route.Camera, route.Follow, route.LookAt, activity.Rig, activity.Camera, activity.Follow, activity.LookAt, routeBinding, activityBinding, routeSource, activitySource);
        }

        private static CameraRigFixture CreateCameraRig(Transform parent, string label)
        {
            GameObject rig = CreateHiddenChild(parent, $"{label}Rig");
            GameObject cameraObject = CreateHiddenChild(rig.transform, $"{label}Camera");
            CinemachineCamera camera = cameraObject.AddComponent<CinemachineCamera>();
            camera.Priority = InitialPriority;
            GameObject follow = CreateHiddenChild(rig.transform, $"{label}Follow");
            GameObject lookAt = CreateHiddenChild(rig.transform, $"{label}LookAt");
            return new CameraRigFixture(rig, camera, follow.transform, lookAt.transform);
        }

        private static GameObject CreateHiddenRoot(string name)
        {
            GameObject gameObject = new GameObject(name) { hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild };
            return gameObject;
        }

        private static GameObject CreateHiddenChild(Transform parent, string name)
        {
            GameObject child = CreateHiddenRoot(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void SetSerializedBool(UnityEngine.Object target, string propertyName, bool value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool AssertLegacyStateUntouched(SmokeFixture fixture, string code)
        {
            if (fixture.UnityCamera.enabled != fixture.InitialUnityCameraEnabled ||
                fixture.Root.activeSelf != fixture.RootActive ||
                fixture.RouteRig.activeSelf != fixture.RouteRigActive ||
                fixture.ActivityRig.activeSelf != fixture.ActivityRigActive)
            {
                return Fail(code, "The real binding path mutated Camera.enabled or GameObject.activeSelf.");
            }

            return true;
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
            public SmokeFixture(GameObject root, FrameworkCameraDirector director, UnityEngine.Camera unityCamera, CinemachineBrain brain, GameObject routeRig, CinemachineCamera routeCamera, Transform routeFollow, Transform routeLookAt, GameObject activityRig, CinemachineCamera activityCamera, Transform activityFollow, Transform activityLookAt, FrameworkRouteCameraBinding routeBinding, FrameworkActivityCameraBinding activityBinding, FrameworkCinemachineCameraOutputSource routeSource, FrameworkCinemachineCameraOutputSource activitySource)
            {
                Root = root;
                Director = director;
                UnityCamera = unityCamera;
                Brain = brain;
                RouteRig = routeRig;
                RouteCamera = routeCamera;
                RouteFollow = routeFollow;
                RouteLookAt = routeLookAt;
                ActivityRig = activityRig;
                ActivityCamera = activityCamera;
                ActivityFollow = activityFollow;
                ActivityLookAt = activityLookAt;
                RouteBinding = routeBinding;
                ActivityBinding = activityBinding;
                RouteSource = routeSource;
                ActivitySource = activitySource;
                InitialUnityCameraEnabled = unityCamera.enabled;
                RootActive = root.activeSelf;
                RouteRigActive = routeRig.activeSelf;
                ActivityRigActive = activityRig.activeSelf;
            }

            public GameObject Root { get; }
            public FrameworkCameraDirector Director { get; }
            public UnityEngine.Camera UnityCamera { get; }
            public CinemachineBrain Brain { get; }
            public GameObject RouteRig { get; }
            public CinemachineCamera RouteCamera { get; }
            public Transform RouteFollow { get; }
            public Transform RouteLookAt { get; }
            public GameObject ActivityRig { get; }
            public CinemachineCamera ActivityCamera { get; }
            public Transform ActivityFollow { get; }
            public Transform ActivityLookAt { get; }
            public FrameworkRouteCameraBinding RouteBinding { get; }
            public FrameworkActivityCameraBinding ActivityBinding { get; }
            public FrameworkCinemachineCameraOutputSource RouteSource { get; }
            public FrameworkCinemachineCameraOutputSource ActivitySource { get; }
            public bool InitialUnityCameraEnabled { get; }
            public bool RootActive { get; }
            public bool RouteRigActive { get; }
            public bool ActivityRigActive { get; }
        }
    }
}

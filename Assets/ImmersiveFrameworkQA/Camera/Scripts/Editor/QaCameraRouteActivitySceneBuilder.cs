using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Editor-only, idempotent setup for the QA Route/Activity camera cut.
    /// This configures QA fixtures under Assets/ImmersiveFrameworkQA, not FIRSTGAME/_Project assets.
    /// </summary>
    public static class QaCameraRouteActivitySceneBuilder
    {
        private const string StartupScenePath = "Assets/ImmersiveFrameworkQA/Scenes/StartupScene.unity";
        private const string AlternateScenePath = "Assets/ImmersiveFrameworkQA/Scenes/SecondScene.unity";

        private const string CanonicalRoutePath = "Assets/ImmersiveFrameworkQA/Routes/QA_CanonicalRoute.asset";
        private const string AlternateRoutePath = "Assets/ImmersiveFrameworkQA/Routes/QA_AlternateRoute.asset";

        private const string PrimaryActivityPath = "Assets/ImmersiveFrameworkQA/Activities/QA_PrimaryContentActivity.asset";
        private const string SecondaryActivityPath = "Assets/ImmersiveFrameworkQA/Activities/QA_SecondaryContentActivity.asset";
        private const string FallbackActivityPath = "Assets/ImmersiveFrameworkQA/Activities/QA_NoContentActivity.asset";

        [MenuItem("Immersive Framework QA/Camera/Configure Route-Activity Camera QA")]
        public static void ConfigureRouteActivityCameraQa()
        {
            bool canonicalConfigured = ConfigureScene(
                StartupScenePath,
                "Canonical",
                CanonicalRoutePath,
                AlternateRoutePath,
                PrimaryActivityPath,
                SecondaryActivityPath,
                FallbackActivityPath,
                startupActivityPath: PrimaryActivityPath,
                startupActivityOwnsCamera: true,
                mainCameraPosition: new Vector3(0f, 4f, -10f),
                mainCameraRotation: Quaternion.Euler(20f, 0f, 0f),
                routeRigPosition: new Vector3(0f, 5f, -10f),
                routeRigRotation: Quaternion.Euler(25f, 0f, 0f),
                activityRigPosition: new Vector3(0f, 3.5f, -7f),
                activityRigRotation: Quaternion.Euler(18f, 0f, 0f));

            bool alternateConfigured = ConfigureScene(
                AlternateScenePath,
                "Alternate",
                AlternateRoutePath,
                CanonicalRoutePath,
                PrimaryActivityPath,
                SecondaryActivityPath,
                FallbackActivityPath,
                startupActivityPath: SecondaryActivityPath,
                startupActivityOwnsCamera: true,
                mainCameraPosition: new Vector3(6f, 4f, -10f),
                mainCameraRotation: Quaternion.Euler(20f, -20f, 0f),
                routeRigPosition: new Vector3(6f, 5f, -10f),
                routeRigRotation: Quaternion.Euler(25f, -20f, 0f),
                activityRigPosition: new Vector3(6f, 3.5f, -7f),
                activityRigRotation: Quaternion.Euler(18f, -20f, 0f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (canonicalConfigured && alternateConfigured)
            {
                Debug.Log("[QA_CAMERA_SETUP] Route/Activity camera QA configured under Assets/ImmersiveFrameworkQA.");
                return;
            }

            Debug.LogError("[QA_CAMERA_SETUP] Route/Activity camera QA configuration completed with blocking setup errors. Fix the errors above before running the smoke.");
        }

        private static bool ConfigureScene(
            string scenePath,
            string label,
            string activeRoutePath,
            string otherRoutePath,
            string primaryActivityPath,
            string secondaryActivityPath,
            string fallbackActivityPath,
            string startupActivityPath,
            bool startupActivityOwnsCamera,
            Vector3 mainCameraPosition,
            Quaternion mainCameraRotation,
            Vector3 routeRigPosition,
            Quaternion routeRigRotation,
            Vector3 activityRigPosition,
            Quaternion activityRigRotation)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Reload ScriptableObject references after each scene open. This avoids stale UnityEngine.Object
            // wrappers when the builder configures multiple scenes in one editor command.
            RouteAsset activeRoute = LoadAsset<RouteAsset>(activeRoutePath);
            RouteAsset otherRoute = LoadAsset<RouteAsset>(otherRoutePath);
            ActivityAsset primaryActivity = LoadAsset<ActivityAsset>(primaryActivityPath);
            ActivityAsset secondaryActivity = LoadAsset<ActivityAsset>(secondaryActivityPath);
            ActivityAsset fallbackActivity = LoadAsset<ActivityAsset>(fallbackActivityPath);
            ActivityAsset startupActivity = LoadAsset<ActivityAsset>(startupActivityPath);

            if (activeRoute == null
                || otherRoute == null
                || primaryActivity == null
                || secondaryActivity == null
                || fallbackActivity == null
                || startupActivity == null)
            {
                Debug.LogError($"[QA_CAMERA_SETUP] Camera QA setup aborted for scene '{scenePath}' because required QA route/activity assets are missing.");
                return false;
            }

            GameObject mainCamera = EnsureMainCamera(scene, mainCameraPosition, mainCameraRotation);
            UnityEngine.Camera operationalCamera = EnsureComponent<UnityEngine.Camera>(mainCamera);
            CinemachineBrain brain = EnsureComponent<CinemachineBrain>(mainCamera);

            GameObject target = EnsureTarget(scene, $"QA_Camera_Target_{label}", new Vector3(label == "Alternate" ? 6f : 0f, 1f, 0f));
            GameObject anchorsObject = EnsureRoot(scene, $"QA_Camera_Anchors_{label}");
            QaCameraAnchorHost anchors = EnsureComponent<QaCameraAnchorHost>(anchorsObject);
            SetSerialized(anchors, "trackingTarget", target.transform);
            SetSerialized(anchors, "lookAtTarget", target.transform);

            GameObject root = EnsureRoot(scene, $"QA_CameraRoot_{label}");
            RouteContentBinding routeContentBinding = EnsureComponent<RouteContentBinding>(root);
            QaCameraDirector director = EnsureComponent<QaCameraDirector>(root);
            QaRouteCameraBinding routeCameraBinding = EnsureComponent<QaRouteCameraBinding>(root);

            GameObject routeRig = EnsureCinemachineRig(scene, $"QA_Camera_RouteRig_{label}", routeRigPosition, routeRigRotation);
            GameObject startupActivityRig = EnsureCinemachineRig(scene, $"QA_Camera_StartupActivityRig_{label}", activityRigPosition, activityRigRotation);

            SetSerialized(routeContentBinding, "route", activeRoute);
            SetSerialized(routeContentBinding, "localContentId", $"qa-camera-route-{label.ToLowerInvariant()}");
            SetSerialized(routeContentBinding, "requiredness", 10);

            SetSerialized(director, "operationalCamera", operationalCamera);
            SetSerialized(director, "cinemachineBrain", brain);
            SetSerialized(director, "defaultCameraRig", routeRig);
            SetSerialized(director, "routePriority", 20);
            SetSerialized(director, "activityPriority", 100);
            SetSerialized(director, "logTransitions", true);

            QaActivityCameraBinding startupBinding;
            if (ReferenceEquals(startupActivity, primaryActivity))
            {
                startupBinding = EnsureActivityBinding(
                    scene,
                    $"QA_Camera_Activity_Primary_{label}",
                    primaryActivity,
                    startupActivityOwnsCamera ? startupActivityRig : null,
                    QaActivityCameraPolicy.UseOwnOrKeepPreviousActivity,
                    anchors,
                    director,
                    "primary");

                EnsureActivityBinding(
                    scene,
                    $"QA_Camera_Activity_Secondary_{label}",
                    secondaryActivity,
                    null,
                    QaActivityCameraPolicy.UseOwnOrKeepPreviousActivity,
                    anchors,
                    director,
                    "secondary");
            }
            else
            {
                EnsureActivityBinding(
                    scene,
                    $"QA_Camera_Activity_Primary_{label}",
                    primaryActivity,
                    null,
                    QaActivityCameraPolicy.UseOwnOrKeepPreviousActivity,
                    anchors,
                    director,
                    "primary");

                startupBinding = EnsureActivityBinding(
                    scene,
                    $"QA_Camera_Activity_Secondary_{label}",
                    secondaryActivity,
                    startupActivityOwnsCamera ? startupActivityRig : null,
                    QaActivityCameraPolicy.UseOwnOrKeepPreviousActivity,
                    anchors,
                    director,
                    "secondary");
            }

            EnsureActivityBinding(
                scene,
                $"QA_Camera_Activity_RouteFallback_{label}",
                fallbackActivity,
                null,
                QaActivityCameraPolicy.UseRoute,
                anchors,
                director,
                "route-fallback");

            SetSerialized(routeCameraBinding, "routeCameraRig", routeRig);
            SetSerialized(routeCameraBinding, "routeAnchors", anchors);
            SetSerialized(routeCameraBinding, "director", director);
            SetSerialized(routeCameraBinding, "startupActivityCameraBinding", startupBinding);

            routeRig.SetActive(false);
            startupActivityRig.SetActive(false);

            ConfigurePanel(
                scene,
                label,
                otherRoute,
                primaryActivity,
                secondaryActivity,
                fallbackActivity,
                director);

            bool valid = ValidateObjectReference(routeContentBinding, "route", activeRoute, $"RouteContentBinding route on 'QA_CameraRoot_{label}'")
                & ValidateObjectReference(routeCameraBinding, "routeCameraRig", routeRig, $"QaRouteCameraBinding routeCameraRig on 'QA_CameraRoot_{label}'")
                & ValidateObjectReference(routeCameraBinding, "startupActivityCameraBinding", startupBinding, $"QaRouteCameraBinding startupActivityCameraBinding on 'QA_CameraRoot_{label}'");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return valid;
        }

        private static QaActivityCameraBinding EnsureActivityBinding(
            Scene scene,
            string rootName,
            ActivityAsset activity,
            GameObject rig,
            QaActivityCameraPolicy policy,
            QaCameraAnchorHost anchors,
            QaCameraDirector director,
            string contentIdSuffix)
        {
            GameObject root = EnsureRoot(scene, rootName);
            ActivityLocalVisibilityAdapter contentBinding = EnsureComponent<ActivityLocalVisibilityAdapter>(root);
            QaActivityCameraBinding cameraBinding = EnsureComponent<QaActivityCameraBinding>(root);

            SetSerialized(contentBinding, "activity", activity);
            SetSerialized(contentBinding, "localContentId", $"qa-camera-activity-{contentIdSuffix}-{scene.name.ToLowerInvariant()}");
            SetSerialized(contentBinding, "requiredness", 10);

            SetSerialized(cameraBinding, "assignedActivity", activity);
            SetSerialized(cameraBinding, "activityCameraRig", rig);
            SetSerialized(cameraBinding, "policy", (int)policy);
            SetSerialized(cameraBinding, "anchors", anchors);
            SetSerialized(cameraBinding, "director", director);

            ValidateObjectReference(
                contentBinding,
                "activity",
                activity,
                $"ActivityLocalVisibilityAdapter activity on '{rootName}'");
            ValidateObjectReference(
                cameraBinding,
                "assignedActivity",
                activity,
                $"QaActivityCameraBinding assignedActivity on '{rootName}'");
            return cameraBinding;
        }

        private static void ConfigurePanel(
            Scene scene,
            string label,
            RouteAsset otherRoute,
            ActivityAsset primaryActivity,
            ActivityAsset secondaryActivity,
            ActivityAsset fallbackActivity,
            QaCameraDirector director)
        {
            GameObject panelObject = EnsureRoot(scene, $"QA_CameraPanel_{label}");
            QaCameraRouteActivityPanel panel = EnsureComponent<QaCameraRouteActivityPanel>(panelObject);

            RouteRequestTrigger routeTrigger = EnsureRouteTrigger(
                scene,
                panelObject.transform,
                $"QA_Camera_RouteRequest_{label}",
                otherRoute,
                $"qa.camera.route.{label.ToLowerInvariant()}.other");

            ActivityRequestTrigger primaryTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_Camera_ActivityRequest_Primary_{label}",
                primaryActivity,
                $"qa.camera.activity.{label.ToLowerInvariant()}.primary");

            ActivityRequestTrigger secondaryTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_Camera_ActivityRequest_Secondary_{label}",
                secondaryActivity,
                $"qa.camera.activity.{label.ToLowerInvariant()}.secondary");

            ActivityRequestTrigger fallbackTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_Camera_ActivityRequest_RouteFallback_{label}",
                fallbackActivity,
                $"qa.camera.activity.{label.ToLowerInvariant()}.route-fallback");

            ActivityRequestTrigger clearTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_Camera_ActivityRequest_Clear_{label}",
                null,
                $"qa.camera.activity.{label.ToLowerInvariant()}.clear");

            GameObject[] relatedPanels = FindRelatedPanelObjects(scene, panelObject);

            panel.Configure(
                routeTrigger,
                primaryTrigger,
                secondaryTrigger,
                fallbackTrigger,
                clearTrigger,
                $"QA Camera {label}",
                director,
                relatedPanels);
            panel.ConfigureLayout(new Rect(16f, 16f, 660f, 720f), new Vector2(460f, 420f), 610f);
            panel.CloseRelatedPanels();

            EditorUtility.SetDirty(panel);
        }

        private static GameObject[] FindRelatedPanelObjects(Scene scene, GameObject cameraPanelObject)
        {
            List<GameObject> panels = new List<GameObject>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                CollectRelatedPanelObjects(root.transform, cameraPanelObject, panels);
            }

            return panels.ToArray();
        }

        private static void CollectRelatedPanelObjects(Transform current, GameObject cameraPanelObject, List<GameObject> panels)
        {
            if (current == null)
            {
                return;
            }

            GameObject currentObject = current.gameObject;
            if (!ReferenceEquals(currentObject, cameraPanelObject)
                && currentObject.name.IndexOf("Panel", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                panels.Add(currentObject);
            }

            for (int i = 0; i < current.childCount; i++)
            {
                CollectRelatedPanelObjects(current.GetChild(i), cameraPanelObject, panels);
            }
        }

        private static RouteRequestTrigger EnsureRouteTrigger(Scene scene, Transform parent, string name, RouteAsset targetRoute, string reason)
        {
            GameObject go = EnsureChild(scene, parent, name);
            RouteRequestTrigger trigger = EnsureComponent<RouteRequestTrigger>(go);
            trigger.TargetRoute = targetRoute;
            SetSerialized(trigger, "targetRoute", targetRoute);
            SetSerialized(trigger, "reason", reason);
            EditorUtility.SetDirty(trigger);
            ValidateRouteTriggerTarget(trigger, targetRoute, name);
            return trigger;
        }

        private static ActivityRequestTrigger EnsureActivityTrigger(Scene scene, Transform parent, string name, ActivityAsset targetActivity, string reason)
        {
            GameObject go = EnsureChild(scene, parent, name);
            ActivityRequestTrigger trigger = EnsureComponent<ActivityRequestTrigger>(go);
            trigger.TargetActivity = targetActivity;
            SetSerialized(trigger, "targetActivity", targetActivity);
            SetSerialized(trigger, "reason", reason);
            EditorUtility.SetDirty(trigger);

            if (targetActivity != null)
            {
                ValidateActivityTriggerTarget(trigger, targetActivity, name);
            }

            return trigger;
        }

        private static GameObject EnsureMainCamera(Scene scene, Vector3 position, Quaternion rotation)
        {
            GameObject cameraObject = FindInScene(scene, "Main Camera");
            if (cameraObject == null)
            {
                cameraObject = new GameObject("Main Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, scene);
            }

            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(position, rotation);
            return cameraObject;
        }

        private static GameObject EnsureTarget(Scene scene, string name, Vector3 position)
        {
            GameObject target = FindInScene(scene, name);
            if (target == null)
            {
                target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.name = name;
                SceneManager.MoveGameObjectToScene(target, scene);
            }

            target.transform.position = position;
            target.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
            return target;
        }

        private static GameObject EnsureRoot(Scene scene, string name)
        {
            GameObject root = FindInScene(scene, name);
            if (root != null)
            {
                return root;
            }

            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return root;
        }

        private static GameObject EnsureChild(Scene scene, Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject EnsureCinemachineRig(Scene scene, string name, Vector3 position, Quaternion rotation)
        {
            GameObject rig = FindInScene(scene, name);
            if (rig == null)
            {
                rig = new GameObject(name);
                SceneManager.MoveGameObjectToScene(rig, scene);
            }

            rig.transform.SetPositionAndRotation(position, rotation);
            EnsureComponent<CinemachineCamera>(rig);

            UnityEngine.Camera camera = rig.GetComponent<UnityEngine.Camera>();
            if (camera != null)
            {
                Object.DestroyImmediate(camera);
            }

            CinemachineBrain brain = rig.GetComponent<CinemachineBrain>();
            if (brain != null)
            {
                Object.DestroyImmediate(brain);
            }

            return rig;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindInChildren(root.transform, name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInChildren(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindInChildren(root.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }


        private static bool ValidateRouteTriggerTarget(RouteRequestTrigger trigger, RouteAsset expectedRoute, string context)
        {
            if (!ReferencesMatch(trigger.TargetRoute, expectedRoute))
            {
                Debug.LogError(
                    $"[QA_CAMERA_SETUP] Route trigger target was not assigned. context='{context}' expected='{FormatRoute(expectedRoute)}' actual='{FormatRoute(trigger.TargetRoute)}'.",
                    trigger);
                return false;
            }

            return true;
        }

        private static bool ValidateActivityTriggerTarget(ActivityRequestTrigger trigger, ActivityAsset expectedActivity, string context)
        {
            if (!ReferencesMatch(trigger.TargetActivity, expectedActivity))
            {
                Debug.LogError(
                    $"[QA_CAMERA_SETUP] Activity trigger target was not assigned. context='{context}' expected='{FormatActivity(expectedActivity)}' actual='{FormatActivity(trigger.TargetActivity)}'.",
                    trigger);
                return false;
            }

            return true;
        }

        private static bool ValidateObjectReference(Object target, string propertyName, Object expectedValue, string context)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[QA_CAMERA_SETUP] Serialized validation property not found. context='{context}' property='{propertyName}'.", target);
                return false;
            }

            if (!ReferencesMatch(property.objectReferenceValue, expectedValue))
            {
                Debug.LogError(
                    $"[QA_CAMERA_SETUP] Object reference was not assigned. context='{context}' expected='{FormatObject(expectedValue)}' actual='{FormatObject(property.objectReferenceValue)}'.",
                    target);
                return false;
            }

            return true;
        }

        private static bool ReferencesMatch(Object actual, Object expected)
        {
            if (actual == null || expected == null)
            {
                return actual == expected;
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual, out string actualGuid, out long actualLocalId)
                && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(expected, out string expectedGuid, out long expectedLocalId))
            {
                return actualGuid == expectedGuid && actualLocalId == expectedLocalId;
            }

            return actual == expected;
        }

        private static string FormatRoute(RouteAsset route)
        {
            return route != null ? route.RouteName : "<missing>";
        }

        private static string FormatActivity(ActivityAsset activity)
        {
            return activity != null ? activity.ActivityName : "<missing>";
        }

        private static string FormatObject(Object value)
        {
            if (value == null)
            {
                return "<missing>";
            }

            string path = AssetDatabase.GetAssetPath(value);
            return string.IsNullOrWhiteSpace(path) ? value.name : $"{value.name} ({path})";
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError($"[QA_CAMERA_SETUP] Required asset not found. path='{path}'.");
            }

            return asset;
        }

        private static void SetSerialized(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[QA_CAMERA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[QA_CAMERA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[QA_CAMERA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[QA_CAMERA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}

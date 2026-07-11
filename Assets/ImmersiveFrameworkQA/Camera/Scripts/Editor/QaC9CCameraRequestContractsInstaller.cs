using System;
using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    public static class QaC9CCameraRequestContractsInstaller
    {
        private const string Root = "Assets/ImmersiveFrameworkQA";
        private const string CameraRoot = Root + "/Camera";
        private const string HubRoot = Root + "/Hub";

        private const string ScenePath =
            CameraRoot + "/Scenes/QA_CameraRequestContracts.unity";
        private const string RoutePath =
            CameraRoot + "/Routes/QA_CameraRequestContractsRoute.asset";
        private const string ActivityPath =
            CameraRoot + "/Activities/QA_CameraRequestContractsActivity.asset";

        private const string HubScenePath =
            HubRoot + "/Scenes/QA_Hub.unity";
        private const string HubRoutePath =
            HubRoot + "/Routes/QA_HubRoute.asset";

        private const string HubLabel =
            "Camera / C9C Request Contracts";
        private const string HubReason =
            "qa.hub.route.camera_request_contracts";
        private const string HubTriggerName =
            "RouteTrigger_Camera_C9C_Request_Contracts";

        [MenuItem("Immersive Framework QA/Camera/C9C Install Camera Request Contracts QA")]
        public static void Install()
        {
            try
            {
                EnsureFolders();
                CreateOrRepairRouteAssets();
                CreateOrRepairSyntheticScene();
                EnsureSceneInBuildSettings();
                CreateOrRepairHubEntry();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                ValidateRouteAssets();
                ValidateSyntheticScene();
                ValidateHubEntry();

                Debug.Log(
                    "[C9C_CAMERA_REQUEST_CONTRACTS_SETUP] status='Succeeded' " +
                    $"scene='{ScenePath}' route='{RoutePath}' activity='{ActivityPath}' " +
                    $"hubLabel='{HubLabel}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[C9C_CAMERA_REQUEST_CONTRACTS_SETUP] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        [MenuItem("Immersive Framework QA/Camera/C9C Open Camera Request Contracts Scene")]
        public static void OpenScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Install();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void CreateOrRepairRouteAssets()
        {
            ActivityAsset activity = LoadOrCreate<ActivityAsset>(ActivityPath);
            var activitySerialized = new SerializedObject(activity);
            activitySerialized.Update();
            SetString(
                activitySerialized,
                "activityName",
                "QA C9C Camera Request Contracts Activity");
            SetString(
                activitySerialized,
                "description",
                "Synthetic runtime activity proving CameraRequest contract creation and explicit failures.");
            activitySerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);

            RouteAsset route = LoadOrCreate<RouteAsset>(RoutePath);
            var routeSerialized = new SerializedObject(route);
            routeSerialized.Update();
            SetString(
                routeSerialized,
                "routeName",
                "QA C9C Camera Request Contracts");
            SetString(
                routeSerialized,
                "primaryScenePath",
                ScenePath);
            SetString(
                routeSerialized,
                "primarySceneName",
                Path.GetFileNameWithoutExtension(ScenePath));
            SetObject(
                routeSerialized,
                "startupActivity",
                activity);
            SetString(
                routeSerialized,
                "description",
                "QA route for C9C typed CameraRequest contracts. No output authority or Cinemachine application.");
            routeSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RequireAsset<ActivityAsset>(ActivityPath);
            RequireAsset<RouteAsset>(RoutePath);
        }

        private static void CreateOrRepairSyntheticScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            scene.name = "QA_CameraRequestContracts";

            GameObject cameraObject = new GameObject("QA_C9C_ObservedCamera");
            UnityEngine.Camera observedCamera =
                cameraObject.AddComponent<UnityEngine.Camera>();
            observedCamera.clearFlags = CameraClearFlags.SolidColor;
            observedCamera.backgroundColor =
                new Color(0.025f, 0.035f, 0.06f, 1f);
            cameraObject.transform.position =
                new Vector3(0f, 0f, -10f);

            GameObject root =
                new GameObject("QA_C9C_CameraRequestContracts");

            GameObject followObject =
                CreateChild(root.transform, "ExplicitFollowTarget");
            followObject.transform.localPosition =
                new Vector3(0f, 1.5f, 0f);

            GameObject lookAtObject =
                CreateChild(root.transform, "ExplicitLookAtTarget");
            lookAtObject.transform.localPosition =
                new Vector3(0f, 1.8f, 1f);

            GameObject rigObject =
                CreateChild(root.transform, "QA_C9C_CameraRig");
            CameraRigComposer composer =
                rigObject.AddComponent<CameraRigComposer>();
            ConfigureComposer(
                composer,
                followObject.transform,
                lookAtObject.transform);

            QaC9CCameraRequestContractsFixture fixture =
                root.AddComponent<QaC9CCameraRequestContractsFixture>();
            ConfigureFixture(
                fixture,
                composer,
                followObject.transform,
                observedCamera);

            CreateBackToHubNavigation(scene);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save C9C scene at '{ScenePath}'.");
            }
        }

        private static void CreateBackToHubNavigation(Scene scene)
        {
            RouteAsset hubRoute = RequireAsset<RouteAsset>(HubRoutePath);

            GameObject navigation =
                new GameObject("QA_BackToHubNavigation");
            SceneManager.MoveGameObjectToScene(navigation, scene);

            RouteRequestTrigger trigger =
                navigation.AddComponent<RouteRequestTrigger>();
            AssignRouteTrigger(
                trigger,
                hubRoute,
                "qa.route.back-to-hub");

            QaHubPanel panel = navigation.AddComponent<QaHubPanel>();
            panel.Configure(
                new[]
                {
                    new QaHubPanel.QaHubEntry("Back to QA Hub", trigger)
                },
                "QA Navigation");

            var serialized = new SerializedObject(panel);
            serialized.Update();
            SetRect(
                serialized,
                "panelRect",
                new Rect(16f, 16f, 360f, 92f));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        private static void CreateOrRepairHubEntry()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HubScenePath) == null)
            {
                throw new InvalidOperationException(
                    $"QA Hub scene is missing at '{HubScenePath}'.");
            }

            Scene hubScene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);

            // Load after opening the scene. No asset reference is carried across scene changes.
            RouteAsset route = RequireAsset<RouteAsset>(RoutePath);

            QaHubPanel panel =
                FindSingleInScene<QaHubPanel>(hubScene, nameof(QaHubPanel));

            GameObject triggerObject =
                FindByName(hubScene, HubTriggerName);
            if (triggerObject == null)
            {
                triggerObject = new GameObject(HubTriggerName);
                triggerObject.transform.SetParent(panel.transform, false);
            }

            RouteRequestTrigger trigger =
                triggerObject.GetComponent<RouteRequestTrigger>();
            if (trigger == null)
            {
                trigger = triggerObject.AddComponent<RouteRequestTrigger>();
            }

            AssignRouteTrigger(trigger, route, HubReason);
            AppendOrRepairHubEntry(panel, trigger);

            EditorUtility.SetDirty(triggerObject);
            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(panel.gameObject);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(hubScene);

            if (!EditorSceneManager.SaveScene(hubScene, HubScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save QA Hub scene at '{HubScenePath}'.");
            }
        }

        private static void AssignRouteTrigger(
            RouteRequestTrigger trigger,
            RouteAsset route,
            string reason)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            trigger.TargetRoute = route;

            var serialized = new SerializedObject(trigger);
            serialized.Update();
            SetString(serialized, "reason", reason);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);

            if (trigger.TargetRoute == null)
            {
                throw new InvalidOperationException(
                    $"RouteRequestTrigger did not retain Route '{route.name}' before save.");
            }
        }

        private static void AppendOrRepairHubEntry(
            QaHubPanel panel,
            RouteRequestTrigger trigger)
        {
            var serialized = new SerializedObject(panel);
            serialized.Update();
            SerializedProperty entries = FindProperty(serialized, "entries");

            int targetIndex = -1;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry =
                    entries.GetArrayElementAtIndex(i);
                SerializedProperty label =
                    entry.FindPropertyRelative("label");
                SerializedProperty entryTrigger =
                    entry.FindPropertyRelative("routeRequestTrigger");

                if ((label != null &&
                     string.Equals(
                         label.stringValue,
                         HubLabel,
                         StringComparison.Ordinal)) ||
                    (entryTrigger != null &&
                     entryTrigger.objectReferenceValue == trigger))
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex < 0)
            {
                targetIndex = entries.arraySize;
                entries.arraySize++;
            }

            SerializedProperty targetEntry =
                entries.GetArrayElementAtIndex(targetIndex);
            SerializedProperty targetLabel =
                targetEntry.FindPropertyRelative("label");
            SerializedProperty targetTrigger =
                targetEntry.FindPropertyRelative("routeRequestTrigger");

            if (targetLabel == null || targetTrigger == null)
            {
                throw new InvalidOperationException(
                    "QA Hub entry serialization shape is invalid.");
            }

            targetLabel.stringValue = HubLabel;
            targetTrigger.objectReferenceValue = trigger;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureComposer(
            CameraRigComposer composer,
            Transform follow,
            Transform lookAt)
        {
            var serialized = new SerializedObject(composer);
            serialized.Update();
            SetEnum(
                serialized,
                "presentationIntent",
                CameraRigPresentationIntent.Follow);
            SetEnum(
                serialized,
                "targetSourceKind",
                CameraTargetSourceKind.ExplicitTransform);
            SetObject(serialized, "explicitFollowTarget", follow);
            SetObject(serialized, "explicitLookAtTarget", lookAt);
            SetEnum(
                serialized,
                "followRequirement",
                CameraTargetRequirement.Required);
            SetEnum(
                serialized,
                "lookAtRequirement",
                CameraTargetRequirement.Optional);
            SetBool(
                serialized,
                "createUnityCameraIfMissing",
                false);
            SetBool(
                serialized,
                "createCinemachineCameraIfMissing",
                false);
            SetBool(
                serialized,
                "logApplyRebuildDiagnostics",
                false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(composer);
        }

        private static void ConfigureFixture(
            QaC9CCameraRequestContractsFixture fixture,
            CameraRigComposer composer,
            Transform explicitTarget,
            UnityEngine.Camera observedCamera)
        {
            var serialized = new SerializedObject(fixture);
            serialized.Update();
            SetObject(serialized, "rigComposer", composer);
            SetObject(
                serialized,
                "explicitTargetSource",
                explicitTarget);
            SetObject(serialized, "observedCamera", observedCamera);
            SetBool(serialized, "runOnStart", true);
            SetBool(serialized, "throwOnFailure", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fixture);
        }

        private static void ValidateRouteAssets()
        {
            ActivityAsset activity = RequireAsset<ActivityAsset>(ActivityPath);
            RouteAsset route = RequireAsset<RouteAsset>(RoutePath);

            var serialized = new SerializedObject(route);
            serialized.Update();
            AssertString(
                serialized,
                "primaryScenePath",
                ScenePath);
            AssertReference(
                serialized,
                "startupActivity",
                activity);
        }

        private static void ValidateSyntheticScene()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);

            QaC9CCameraRequestContractsFixture fixture =
                FindSingleInScene<QaC9CCameraRequestContractsFixture>(
                    scene,
                    nameof(QaC9CCameraRequestContractsFixture));
            CameraRigComposer composer =
                FindSingleInScene<CameraRigComposer>(
                    scene,
                    nameof(CameraRigComposer));
            UnityEngine.Camera observedCamera =
                FindSingleInScene<UnityEngine.Camera>(
                    scene,
                    nameof(UnityEngine.Camera));

            var fixtureSerialized = new SerializedObject(fixture);
            fixtureSerialized.Update();
            AssertReference(
                fixtureSerialized,
                "rigComposer",
                composer);
            AssertReference(
                fixtureSerialized,
                "observedCamera",
                observedCamera);

            SerializedProperty explicitTarget =
                FindProperty(
                    fixtureSerialized,
                    "explicitTargetSource");
            if (explicitTarget.objectReferenceValue == null)
            {
                throw new InvalidOperationException(
                    "Saved C9C fixture has no explicit target source.");
            }

            var composerSerialized = new SerializedObject(composer);
            composerSerialized.Update();
            AssertEnum(
                composerSerialized,
                "presentationIntent",
                CameraRigPresentationIntent.Follow);
            AssertEnum(
                composerSerialized,
                "targetSourceKind",
                CameraTargetSourceKind.ExplicitTransform);
            AssertReference(
                composerSerialized,
                "explicitFollowTarget",
                explicitTarget.objectReferenceValue);
        }

        private static void ValidateHubEntry()
        {
            Scene hubScene = EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);

            // Reload after opening the Hub. Do not compare against an object from another scene phase.
            RouteAsset expectedRoute =
                RequireAsset<RouteAsset>(RoutePath);

            QaHubPanel panel =
                FindSingleInScene<QaHubPanel>(
                    hubScene,
                    nameof(QaHubPanel));

            GameObject triggerObject =
                FindByName(hubScene, HubTriggerName);
            if (triggerObject == null)
            {
                throw new InvalidOperationException(
                    "Saved QA Hub does not contain the C9C trigger object.");
            }

            RouteRequestTrigger trigger =
                triggerObject.GetComponent<RouteRequestTrigger>();
            if (trigger == null)
            {
                throw new InvalidOperationException(
                    "Saved C9C Hub object has no RouteRequestTrigger.");
            }

            if (trigger.TargetRoute == null)
            {
                throw new InvalidOperationException(
                    "Saved C9C Hub trigger has a null TargetRoute.");
            }

            string expectedPath =
                AssetDatabase.GetAssetPath(expectedRoute);
            string actualPath =
                AssetDatabase.GetAssetPath(trigger.TargetRoute);

            if (!string.Equals(
                actualPath,
                expectedPath,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Saved C9C Hub trigger Route path is '{actualPath}', expected '{expectedPath}'.");
            }

            var serialized = new SerializedObject(panel);
            serialized.Update();
            SerializedProperty entries =
                FindProperty(serialized, "entries");

            int matchingEntries = 0;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry =
                    entries.GetArrayElementAtIndex(i);
                SerializedProperty label =
                    entry.FindPropertyRelative("label");
                SerializedProperty entryTrigger =
                    entry.FindPropertyRelative("routeRequestTrigger");

                if (label != null &&
                    entryTrigger != null &&
                    string.Equals(
                        label.stringValue,
                        HubLabel,
                        StringComparison.Ordinal) &&
                    entryTrigger.objectReferenceValue == trigger)
                {
                    matchingEntries++;
                }
            }

            if (matchingEntries != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one valid C9C Hub entry, found '{matchingEntries}'.");
            }
        }

        private static void EnsureSceneInBuildSettings()
        {
            EditorBuildSettingsScene[] current =
                EditorBuildSettings.scenes;

            for (int i = 0; i < current.Length; i++)
            {
                if (!string.Equals(
                    current[i].path,
                    ScenePath,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (!current[i].enabled)
                {
                    current[i] =
                        new EditorBuildSettingsScene(
                            ScenePath,
                            true);
                    EditorBuildSettings.scenes = current;
                }

                return;
            }

            var next =
                new EditorBuildSettingsScene[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] =
                new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = next;
        }

        private static T FindSingleInScene<T>(
            Scene scene,
            string diagnosticName)
            where T : Component
        {
            T found = null;
            int count = 0;

            T[] candidates =
                Resources.FindObjectsOfTypeAll<T>();

            for (int i = 0; i < candidates.Length; i++)
            {
                T candidate = candidates[i];
                if (candidate == null ||
                    candidate.gameObject.scene != scene)
                {
                    continue;
                }

                found = candidate;
                count++;
            }

            if (count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one {diagnosticName} in '{scene.name}', found '{count}'.");
            }

            return found;
        }

        private static GameObject FindByName(
            Scene scene,
            string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms =
                    roots[i].GetComponentsInChildren<Transform>(true);

                for (int j = 0; j < transforms.Length; j++)
                {
                    if (string.Equals(
                        transforms[j].name,
                        objectName,
                        StringComparison.Ordinal))
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }

        private static GameObject CreateChild(
            Transform parent,
            string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static T LoadOrCreate<T>(string assetPath)
            where T : ScriptableObject
        {
            T existing =
                AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (existing != null)
            {
                return existing;
            }

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, assetPath);
            return created;
        }

        private static T RequireAsset<T>(string assetPath)
            where T : UnityEngine.Object
        {
            T loaded =
                AssetDatabase.LoadAssetAtPath<T>(assetPath);

            if (loaded == null)
            {
                throw new InvalidOperationException(
                    $"Required asset is missing at '{assetPath}'.");
            }

            return loaded;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder(Root, "Camera");
            EnsureFolder(CameraRoot, "Scenes");
            EnsureFolder(CameraRoot, "Routes");
            EnsureFolder(CameraRoot, "Activities");
            EnsureFolder(CameraRoot, "Scripts");
            EnsureFolder(CameraRoot + "/Scripts", "Runtime");
            EnsureFolder(CameraRoot + "/Scripts", "Editor");
            EnsureFolder(CameraRoot, "Documentation");
        }

        private static void EnsureFolder(
            string parent,
            string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void AssertReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object expected)
        {
            SerializedProperty property =
                FindProperty(serialized, propertyName);

            if (property.propertyType !=
                    SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue != expected)
            {
                throw new InvalidOperationException(
                    $"Serialized reference '{propertyName}' is missing or incorrect on '{serialized.targetObject.name}'.");
            }
        }

        private static void AssertString(
            SerializedObject serialized,
            string propertyName,
            string expected)
        {
            SerializedProperty property =
                FindProperty(serialized, propertyName);

            if (property.propertyType !=
                    SerializedPropertyType.String ||
                !string.Equals(
                    property.stringValue,
                    expected,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Serialized string '{propertyName}' expected '{expected}' but found '{property.stringValue}'.");
            }
        }

        private static void AssertEnum<TEnum>(
            SerializedObject serialized,
            string propertyName,
            TEnum expected)
            where TEnum : struct, Enum
        {
            SerializedProperty property =
                FindProperty(serialized, propertyName);

            if (property.propertyType !=
                SerializedPropertyType.Enum)
            {
                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' is not an enum.");
            }

            string actualName =
                property.enumValueIndex >= 0 &&
                property.enumValueIndex < property.enumNames.Length
                    ? property.enumNames[property.enumValueIndex]
                    : string.Empty;

            if (!string.Equals(
                actualName,
                expected.ToString(),
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Serialized enum '{propertyName}' expected '{expected}' but found '{actualName}'.");
            }
        }

        private static void SetObject(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                FindProperty(serialized, propertyName);

            if (property.propertyType !=
                SerializedPropertyType.ObjectReference)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' is not an object reference.");
            }

            property.objectReferenceValue = value;
        }

        private static void SetString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                FindProperty(serialized, propertyName);

            if (property.propertyType !=
                SerializedPropertyType.String)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' is not a string.");
            }

            property.stringValue = value ?? string.Empty;
        }

        private static void SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            SerializedProperty property =
                FindProperty(serialized, propertyName);

            if (property.propertyType !=
                SerializedPropertyType.Boolean)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' is not a bool.");
            }

            property.boolValue = value;
        }

        private static void SetEnum<TEnum>(
            SerializedObject serialized,
            string propertyName,
            TEnum value)
            where TEnum : struct, Enum
        {
            SerializedProperty property =
                FindProperty(serialized, propertyName);

            if (property.propertyType !=
                SerializedPropertyType.Enum)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' is not an enum.");
            }

            int index = Array.IndexOf(
                property.enumNames,
                value.ToString());

            if (index < 0)
            {
                throw new InvalidOperationException(
                    $"Enum value '{value}' is unavailable for '{propertyName}'. " +
                    $"Available: '{string.Join(",", property.enumNames)}'.");
            }

            property.enumValueIndex = index;
        }

        private static void SetRect(
            SerializedObject serialized,
            string propertyName,
            Rect value)
        {
            SerializedProperty property =
                FindProperty(serialized, propertyName);

            if (property.propertyType !=
                SerializedPropertyType.Rect)
            {
                throw new InvalidOperationException(
                    $"Property '{propertyName}' is not a Rect.");
            }

            property.rectValue = value;
        }

        private static SerializedProperty FindProperty(
            SerializedObject serialized,
            string propertyName)
        {
            if (serialized == null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            SerializedProperty property =
                serialized.FindProperty(propertyName);

            if (property == null)
            {
                string targetName =
                    serialized.targetObject != null
                        ? serialized.targetObject.GetType().Name
                        : "<null>";

                throw new InvalidOperationException(
                    $"Serialized property '{propertyName}' was not found on {targetName}.");
            }

            return property;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
        }
    }
}

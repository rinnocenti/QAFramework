using System;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerBinding;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.PlayerViews;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public static class QaPlayerViewCameraTargetBindingSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Player";
        private const string ScenePath = Root + "/Scenes/QA_PlayerViewCameraTargetBinding.unity";
        private const string RoutePath = Root + "/Routes/QA_PlayerViewCameraTargetBindingRoute.asset";
        private const string ActivityPath = Root + "/Activities/QA_PlayerViewCameraTargetBindingActivity.asset";

        [MenuItem("Immersive Framework QA/Player/Create or Refresh F51B PlayerView Camera Target Binding QA Scene")]
        public static void CreateOrRefreshPlayerViewCameraTargetBindingScene()
        {
            EnsureFolders();

            ActivityAsset activity = CreateActivityAsset(
                ActivityPath,
                "QA PlayerView Camera Target Binding Activity",
                "F51B PlayerView camera target binding QA activity.");

            CreateRouteAsset(
                RoutePath,
                "QA PlayerView Camera Target Binding Route",
                ScenePath,
                "F51B PlayerView camera target binding QA route.",
                activity);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_PlayerViewCameraTargetBinding";

            UnityEngine.Camera mainCamera = CreateCamera("QA_PlayerViewCameraTargetBindingCamera", new Color(0.026f, 0.038f, 0.052f, 1f));

            GameObject root = new GameObject("QA_PlayerViewCameraTargetBinding_Root");
            PlayerSlotDeclaration slotDeclaration = root.AddComponent<PlayerSlotDeclaration>();
            ActorDeclaration actorDeclaration = root.AddComponent<ActorDeclaration>();
            PlayerSlotOccupancy occupancy = root.AddComponent<PlayerSlotOccupancy>();
            ActorReadinessBehaviour readinessBehaviour = root.AddComponent<ActorReadinessBehaviour>();
            PlayerEntryBehaviour entryBehaviour = root.AddComponent<PlayerEntryBehaviour>();
            PlayerViewBehaviour viewBehaviour = root.AddComponent<PlayerViewBehaviour>();
            PlayerControlBehaviour controlBehaviour = root.AddComponent<PlayerControlBehaviour>();
            PlayerViewBindingTargetBehaviour viewBindingTarget = root.AddComponent<PlayerViewBindingTargetBehaviour>();
            PlayerViewCameraTargetBindingTargetBehaviour cameraTargetBindingTarget = root.AddComponent<PlayerViewCameraTargetBindingTargetBehaviour>();

            GameObject viewTarget = new GameObject("QA_PlayerViewCameraTargetBinding_ViewTarget");
            viewTarget.transform.SetParent(root.transform, false);
            viewTarget.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            GameObject controlTarget = new GameObject("QA_PlayerViewCameraTargetBinding_ControlTarget");
            controlTarget.transform.SetParent(root.transform, false);
            controlTarget.transform.localPosition = new Vector3(0f, 1f, 0f);

            GameObject noTargetViewObject = new GameObject("QA_PlayerViewCameraTargetBinding_NoTargetView");
            PlayerViewBehaviour viewWithoutTargetBehaviour = noTargetViewObject.AddComponent<PlayerViewBehaviour>();

            ConfigurePlayerSlotDeclaration(slotDeclaration);
            ConfigureActorDeclaration(actorDeclaration);
            ConfigurePlayerSlotOccupancy(occupancy, slotDeclaration, actorDeclaration);
            ConfigureActorReadinessBehaviour(readinessBehaviour);
            ConfigurePlayerEntryBehaviour(entryBehaviour, slotDeclaration, actorDeclaration, readinessBehaviour);
            ConfigurePlayerViewBehaviour(viewBehaviour, slotDeclaration, entryBehaviour, mainCamera, viewTarget.transform, "qa.playerview-camera-target-binding.view.initial");
            ConfigurePlayerViewBehaviour(viewWithoutTargetBehaviour, slotDeclaration, entryBehaviour, mainCamera, null, "qa.playerview-camera-target-binding.no-target-view.initial");
            ConfigurePlayerControlBehaviour(controlBehaviour, slotDeclaration, entryBehaviour, controlTarget.transform);
            ConfigureViewBindingTarget(viewBindingTarget);
            ConfigureCameraTargetBindingTarget(cameraTargetBindingTarget);

            GameObject fixtureObject = new GameObject("QA_F51B_PlayerViewCameraTargetBinding");
            QaPlayerViewCameraTargetBindingFixture fixture = fixtureObject.AddComponent<QaPlayerViewCameraTargetBindingFixture>();
            ConfigureFixture(
                fixture,
                root,
                readinessBehaviour,
                entryBehaviour,
                viewBehaviour,
                viewWithoutTargetBehaviour,
                controlBehaviour,
                viewBindingTarget,
                cameraTargetBindingTarget);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[F51B_PLAYER_VIEW_CAMERA_TARGET_BINDING_QA] Fixture scene ready: " + ScenePath);
        }

        [MenuItem("Immersive Framework QA/Player/Open F51B PlayerView Camera Target Binding QA Scene")]
        public static void OpenPlayerViewCameraTargetBindingScene()
        {
            if (!File.Exists(ToFullPath(ScenePath)))
            {
                CreateOrRefreshPlayerViewCameraTargetBindingScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void ConfigurePlayerSlotDeclaration(PlayerSlotDeclaration declaration)
        {
            SerializedObject serialized = new SerializedObject(declaration);
            SetString(serialized, "slotId", "player.1");
            SetString(serialized, "displayName", "QA PlayerView Camera Target Binding Slot");
            SetString(serialized, "reason", "qa.playerview-camera-target-binding.slot-declaration");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(declaration);
        }

        private static void ConfigureActorDeclaration(ActorDeclaration declaration)
        {
            SerializedObject serialized = new SerializedObject(declaration);
            SetString(serialized, "actorId", "qa.playerview-camera-target-binding.actor");
            SetString(serialized, "displayName", "QA PlayerView Camera Target Binding Actor");
            SetString(serialized, "reason", "qa.playerview-camera-target-binding.actor-declaration");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(declaration);
        }

        private static void ConfigurePlayerSlotOccupancy(
            PlayerSlotOccupancy occupancy,
            PlayerSlotDeclaration slotDeclaration,
            ActorDeclaration actorDeclaration)
        {
            SerializedObject serialized = new SerializedObject(occupancy);
            SetObject(serialized, "slotDeclaration", slotDeclaration);
            SetString(serialized, "slotId", "player.1");
            SetObject(serialized, "actorDeclaration", actorDeclaration);
            SetObject(serialized, "playerActorDeclaration", null);
            SetString(serialized, "occupiedActorId", "qa.playerview-camera-target-binding.actor");
            SetString(serialized, "displayName", "QA PlayerView Camera Target Binding Occupancy");
            SetString(serialized, "reason", "qa.playerview-camera-target-binding.occupancy");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(occupancy);
        }

        private static void ConfigureActorReadinessBehaviour(ActorReadinessBehaviour readiness)
        {
            SerializedObject serialized = new SerializedObject(readiness);
            SetEnum(serialized, "initialState", ActorReadinessState.NotReady);
            SetString(serialized, "initialReason", string.Empty);
            SetBool(serialized, "applyInitialStateOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(readiness);
        }

        private static void ConfigurePlayerEntryBehaviour(
            PlayerEntryBehaviour entry,
            PlayerSlotDeclaration slotDeclaration,
            ActorDeclaration actorDeclaration,
            ActorReadinessBehaviour readinessBehaviour)
        {
            SerializedObject serialized = new SerializedObject(entry);
            SetObject(serialized, "playerSlotDeclaration", slotDeclaration);
            SetString(serialized, "playerSlotId", "player.1");
            SetObject(serialized, "actorDeclaration", actorDeclaration);
            SetObject(serialized, "playerActorDeclaration", null);
            SetString(serialized, "actorId", "qa.playerview-camera-target-binding.actor");
            SetObject(serialized, "actorReadinessBehaviour", readinessBehaviour);
            SetEnum(serialized, "initialActorReadinessState", ActorReadinessState.NotReady);
            SetString(serialized, "initialActorReadinessReason", string.Empty);
            SetEnum(serialized, "initialState", PlayerEntryState.Configured);
            SetString(serialized, "initialSuspensionReason", string.Empty);
            SetString(serialized, "initialReason", "qa.playerview-camera-target-binding.entry.initial");
            SetBool(serialized, "rebuildOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static void ConfigurePlayerViewBehaviour(
            PlayerViewBehaviour view,
            PlayerSlotDeclaration slotDeclaration,
            PlayerEntryBehaviour entryBehaviour,
            UnityEngine.Camera viewCamera,
            Transform viewTarget,
            string initialReason)
        {
            SerializedObject serialized = new SerializedObject(view);
            SetObject(serialized, "playerSlotDeclaration", slotDeclaration);
            SetString(serialized, "playerSlotId", "player.1");
            SetObject(serialized, "viewCamera", viewCamera);
            SetObject(serialized, "viewTarget", viewTarget);
            SetObject(serialized, "playerEntryBehaviour", entryBehaviour);
            SetEnum(serialized, "initialState", PlayerViewState.Declared);
            SetString(serialized, "initialReason", initialReason);
            SetBool(serialized, "rebuildOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);
        }

        private static void ConfigurePlayerControlBehaviour(
            PlayerControlBehaviour control,
            PlayerSlotDeclaration slotDeclaration,
            PlayerEntryBehaviour entryBehaviour,
            Transform controlTarget)
        {
            SerializedObject serialized = new SerializedObject(control);
            SetObject(serialized, "playerSlotDeclaration", slotDeclaration);
            SetString(serialized, "playerSlotId", "player.1");
            SetObject(serialized, "playerEntryBehaviour", entryBehaviour);
            SetObject(serialized, "controlTarget", controlTarget);
            SetString(serialized, "inputSourceId", "qa.input.playerview-camera-target-binding.intent");
            SetEnum(serialized, "initialState", PlayerControlState.Declared);
            SetString(serialized, "initialSuspensionReason", string.Empty);
            SetString(serialized, "initialReason", "qa.playerview-camera-target-binding.control.initial");
            SetBool(serialized, "rebuildOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(control);
        }

        private static void ConfigureViewBindingTarget(PlayerViewBindingTargetBehaviour target)
        {
            SerializedObject serialized = new SerializedObject(target);
            SetString(serialized, "bindingTargetName", "QA PlayerView Binding Target");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureCameraTargetBindingTarget(PlayerViewCameraTargetBindingTargetBehaviour target)
        {
            SerializedObject serialized = new SerializedObject(target);
            SetString(serialized, "cameraTargetBindingTargetName", "QA PlayerView Camera Target Binding Target");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void ConfigureFixture(
            QaPlayerViewCameraTargetBindingFixture fixture,
            GameObject validationRoot,
            ActorReadinessBehaviour readinessBehaviour,
            PlayerEntryBehaviour entryBehaviour,
            PlayerViewBehaviour viewBehaviour,
            PlayerViewBehaviour viewWithoutTargetBehaviour,
            PlayerControlBehaviour controlBehaviour,
            PlayerViewBindingTargetBehaviour viewBindingTarget,
            PlayerViewCameraTargetBindingTargetBehaviour cameraTargetBindingTarget)
        {
            SerializedObject serialized = new SerializedObject(fixture);
            SetObject(serialized, "validationRoot", validationRoot);
            SetObject(serialized, "actorReadinessBehaviour", readinessBehaviour);
            SetObject(serialized, "entryBehaviour", entryBehaviour);
            SetObject(serialized, "viewBehaviour", viewBehaviour);
            SetObject(serialized, "viewWithoutTargetBehaviour", viewWithoutTargetBehaviour);
            SetObject(serialized, "controlBehaviour", controlBehaviour);
            SetObject(serialized, "viewBindingTarget", viewBindingTarget);
            SetObject(serialized, "cameraTargetBindingTarget", cameraTargetBindingTarget);
            SetBool(serialized, "runOnStart", true);
            SetBool(serialized, "throwOnFailure", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fixture);
        }

        private static UnityEngine.Camera CreateCamera(string name, Color backgroundColor)
        {
            GameObject cameraObject = new GameObject(name);
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "Player");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Routes");
            EnsureFolder(Root, "Activities");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root + "/Scripts", "Runtime");
            EnsureFolder(Root + "/Scripts", "Editor");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static RouteAsset CreateRouteAsset(string assetPath, string routeName, string scenePath, string description, ActivityAsset startupActivity)
        {
            RouteAsset asset = LoadOrCreate<RouteAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "routeName", routeName);
            SetString(serialized, "primaryScenePath", scenePath);
            SetString(serialized, "primarySceneName", Path.GetFileNameWithoutExtension(scenePath));
            SetObject(serialized, "startupActivity", startupActivity);
            SetString(serialized, "description", description);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ActivityAsset CreateActivityAsset(string assetPath, string activityName, string description)
        {
            ActivityAsset asset = LoadOrCreate<ActivityAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "activityName", activityName);
            SetString(serialized, "description", description);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = FindProperty(serialized, propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = FindProperty(serialized, propertyName);
            property.stringValue = value ?? string.Empty;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = FindProperty(serialized, propertyName);
            property.boolValue = value;
        }

        private static void SetEnum<TEnum>(SerializedObject serialized, string propertyName, TEnum value)
            where TEnum : Enum
        {
            SerializedProperty property = FindProperty(serialized, propertyName);
            string targetName = value.ToString();
            for (int i = 0; i < property.enumNames.Length; i++)
            {
                if (property.enumNames[i] == targetName)
                {
                    property.enumValueIndex = i;
                    return;
                }
            }

            throw new InvalidOperationException($"Enum value '{targetName}' was not found on serialized property '{propertyName}'.");
        }

        private static SerializedProperty FindProperty(SerializedObject serialized, string propertyName)
        {
            if (serialized == null)
            {
                throw new ArgumentNullException(nameof(serialized));
            }

            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                string targetName = serialized.targetObject != null ? serialized.targetObject.GetType().Name : "<null>";
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {targetName}.");
            }

            return property;
        }

        private static string ToFullPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Unable to resolve Unity project root.");
            }

            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}

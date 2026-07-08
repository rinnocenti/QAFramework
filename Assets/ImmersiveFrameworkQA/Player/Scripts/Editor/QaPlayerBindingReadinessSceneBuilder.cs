using System;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
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
    public static class QaPlayerBindingReadinessSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Player";
        private const string ScenePath = Root + "/Scenes/QA_PlayerBindingReadiness.unity";
        private const string RoutePath = Root + "/Routes/QA_PlayerBindingReadinessRoute.asset";
        private const string ActivityPath = Root + "/Activities/QA_PlayerBindingReadinessActivity.asset";

        [MenuItem("Immersive Framework QA/Player/Create or Refresh F49K Player Binding Readiness QA Scene")]
        public static void CreateOrRefreshPlayerBindingReadinessScene()
        {
            EnsureFolders();

            ActivityAsset activity = CreateActivityAsset(
                ActivityPath,
                "QA Player Binding Readiness Activity",
                "F49K Player binding readiness passive summary QA activity.");

            CreateRouteAsset(
                RoutePath,
                "QA Player Binding Readiness Route",
                ScenePath,
                "F49K Player binding readiness passive summary QA route.",
                activity);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_PlayerBindingReadiness";

            UnityEngine.Camera mainCamera = CreateCamera("QA_PlayerBindingReadinessCamera", new Color(0.032f, 0.04f, 0.06f, 1f));

            GameObject target = new GameObject("QA_PlayerBindingReadiness_Target");
            PlayerSlotDeclaration slotDeclaration = target.AddComponent<PlayerSlotDeclaration>();
            ActorDeclaration actorDeclaration = target.AddComponent<ActorDeclaration>();
            PlayerSlotOccupancy occupancy = target.AddComponent<PlayerSlotOccupancy>();
            ActorReadinessBehaviour readinessBehaviour = target.AddComponent<ActorReadinessBehaviour>();
            PlayerEntryBehaviour entryBehaviour = target.AddComponent<PlayerEntryBehaviour>();
            PlayerViewBehaviour viewBehaviour = target.AddComponent<PlayerViewBehaviour>();
            PlayerControlBehaviour controlBehaviour = target.AddComponent<PlayerControlBehaviour>();

            GameObject viewTarget = new GameObject("QA_PlayerBindingReadiness_ViewTarget");
            viewTarget.transform.SetParent(target.transform, false);
            viewTarget.transform.localPosition = new Vector3(0f, 1.5f, 0f);

            GameObject controlTarget = new GameObject("QA_PlayerBindingReadiness_ControlTarget");
            controlTarget.transform.SetParent(target.transform, false);
            controlTarget.transform.localPosition = new Vector3(0f, 1f, 0f);

            ConfigurePlayerSlotDeclaration(slotDeclaration);
            ConfigureActorDeclaration(actorDeclaration);
            ConfigurePlayerSlotOccupancy(occupancy, slotDeclaration, actorDeclaration);
            ConfigureActorReadinessBehaviour(readinessBehaviour);
            ConfigurePlayerEntryBehaviour(entryBehaviour, slotDeclaration, actorDeclaration, readinessBehaviour);
            ConfigurePlayerViewBehaviour(viewBehaviour, slotDeclaration, entryBehaviour, mainCamera, viewTarget.transform);
            ConfigurePlayerControlBehaviour(controlBehaviour, slotDeclaration, entryBehaviour, controlTarget.transform);

            GameObject fixtureObject = new GameObject("QA_F49K_PlayerBindingReadiness");
            QaPlayerBindingReadinessFixture fixture = fixtureObject.AddComponent<QaPlayerBindingReadinessFixture>();
            ConfigureFixture(fixture, slotDeclaration, occupancy, readinessBehaviour, entryBehaviour, viewBehaviour, controlBehaviour);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[F49K_PLAYER_BINDING_READINESS_QA] Fixture scene ready: " + ScenePath);
        }

        [MenuItem("Immersive Framework QA/Player/Open F49K Player Binding Readiness QA Scene")]
        public static void OpenPlayerBindingReadinessScene()
        {
            if (!File.Exists(ToFullPath(ScenePath)))
            {
                CreateOrRefreshPlayerBindingReadinessScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void ConfigurePlayerSlotDeclaration(PlayerSlotDeclaration declaration)
        {
            SerializedObject serialized = new SerializedObject(declaration);
            SetString(serialized, "slotId", "player.1");
            SetString(serialized, "displayName", "QA Player Binding Readiness Slot");
            SetString(serialized, "reason", "qa.player-binding-readiness.slot-declaration");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(declaration);
        }

        private static void ConfigureActorDeclaration(ActorDeclaration declaration)
        {
            SerializedObject serialized = new SerializedObject(declaration);
            SetString(serialized, "actorId", "qa.player-binding-readiness.actor");
            SetString(serialized, "displayName", "QA Player Binding Readiness Actor");
            SetString(serialized, "reason", "qa.player-binding-readiness.actor-declaration");
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
            SetString(serialized, "occupiedActorId", "qa.player-binding-readiness.actor");
            SetString(serialized, "displayName", "QA Player Binding Readiness Occupancy");
            SetString(serialized, "reason", "qa.player-binding-readiness.occupancy");
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
            SetString(serialized, "actorId", "qa.player-binding-readiness.actor");
            SetObject(serialized, "actorReadinessBehaviour", readinessBehaviour);
            SetEnum(serialized, "initialActorReadinessState", ActorReadinessState.NotReady);
            SetString(serialized, "initialActorReadinessReason", string.Empty);
            SetEnum(serialized, "initialState", PlayerEntryState.Configured);
            SetString(serialized, "initialSuspensionReason", string.Empty);
            SetString(serialized, "initialReason", "qa.player-binding-readiness.entry.initial");
            SetBool(serialized, "rebuildOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static void ConfigurePlayerViewBehaviour(
            PlayerViewBehaviour view,
            PlayerSlotDeclaration slotDeclaration,
            PlayerEntryBehaviour entryBehaviour,
            UnityEngine.Camera viewCamera,
            Transform viewTarget)
        {
            SerializedObject serialized = new SerializedObject(view);
            SetObject(serialized, "playerSlotDeclaration", slotDeclaration);
            SetString(serialized, "playerSlotId", "player.1");
            SetObject(serialized, "viewCamera", viewCamera);
            SetObject(serialized, "viewTarget", viewTarget);
            SetObject(serialized, "playerEntryBehaviour", entryBehaviour);
            SetEnum(serialized, "initialState", PlayerViewState.Declared);
            SetString(serialized, "initialReason", "qa.player-binding-readiness.view.initial");
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
            SetString(serialized, "inputSourceId", "qa.input.binding-readiness.intent");
            SetEnum(serialized, "initialState", PlayerControlState.Declared);
            SetString(serialized, "initialSuspensionReason", string.Empty);
            SetString(serialized, "initialReason", "qa.player-binding-readiness.control.initial");
            SetBool(serialized, "rebuildOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(control);
        }

        private static void ConfigureFixture(
            QaPlayerBindingReadinessFixture fixture,
            PlayerSlotDeclaration slotDeclaration,
            PlayerSlotOccupancy occupancy,
            ActorReadinessBehaviour readinessBehaviour,
            PlayerEntryBehaviour entryBehaviour,
            PlayerViewBehaviour viewBehaviour,
            PlayerControlBehaviour controlBehaviour)
        {
            SerializedObject serialized = new SerializedObject(fixture);
            SetObject(serialized, "slotDeclaration", slotDeclaration);
            SetObject(serialized, "slotOccupancy", occupancy);
            SetObject(serialized, "actorReadinessBehaviour", readinessBehaviour);
            SetObject(serialized, "entryBehaviour", entryBehaviour);
            SetObject(serialized, "viewBehaviour", viewBehaviour);
            SetObject(serialized, "controlBehaviour", controlBehaviour);
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

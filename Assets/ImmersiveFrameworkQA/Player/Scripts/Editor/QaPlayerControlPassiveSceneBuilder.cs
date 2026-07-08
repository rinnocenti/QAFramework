using System;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerControls;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public static class QaPlayerControlPassiveSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Player";
        private const string ScenePath = Root + "/Scenes/QA_PlayerControlPassive.unity";
        private const string RoutePath = Root + "/Routes/QA_PlayerControlPassiveRoute.asset";
        private const string ActivityPath = Root + "/Activities/QA_PlayerControlPassiveActivity.asset";

        [MenuItem("Immersive Framework QA/Player/Create or Refresh F49I PlayerControl Passive QA Scene")]
        public static void CreateOrRefreshPlayerControlPassiveScene()
        {
            EnsureFolders();

            ActivityAsset activity = CreateActivityAsset(
                ActivityPath,
                "QA PlayerControl Passive Activity",
                "F49I PlayerControl passive contract QA activity.");

            CreateRouteAsset(
                RoutePath,
                "QA PlayerControl Passive Route",
                ScenePath,
                "F49I PlayerControl passive contract QA route.",
                activity);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_PlayerControlPassive";

            CreateCamera("QA_PlayerControlPassiveCamera", new Color(0.035f, 0.045f, 0.058f, 1f));

            GameObject target = new GameObject("QA_PlayerControl_Target");
            PlayerSlotDeclaration slotDeclaration = target.AddComponent<PlayerSlotDeclaration>();
            ActorDeclaration actorDeclaration = target.AddComponent<ActorDeclaration>();
            ActorReadinessBehaviour readinessBehaviour = target.AddComponent<ActorReadinessBehaviour>();
            PlayerEntryBehaviour entryBehaviour = target.AddComponent<PlayerEntryBehaviour>();
            PlayerControlBehaviour controlBehaviour = target.AddComponent<PlayerControlBehaviour>();

            GameObject controlAnchor = new GameObject("QA_PlayerControl_TargetAnchor");
            controlAnchor.transform.SetParent(target.transform, false);
            controlAnchor.transform.localPosition = new Vector3(0f, 1f, 0f);

            ConfigurePlayerSlotDeclaration(slotDeclaration);
            ConfigureActorDeclaration(actorDeclaration);
            ConfigureActorReadinessBehaviour(readinessBehaviour);
            ConfigurePlayerEntryBehaviour(entryBehaviour, slotDeclaration, actorDeclaration, readinessBehaviour);
            ConfigurePlayerControlBehaviour(controlBehaviour, slotDeclaration, entryBehaviour, controlAnchor.transform);

            GameObject fixtureObject = new GameObject("QA_F49I_PlayerControlPassive");
            QaPlayerControlPassiveFixture fixture = fixtureObject.AddComponent<QaPlayerControlPassiveFixture>();
            ConfigureFixture(fixture, controlBehaviour, entryBehaviour, readinessBehaviour, controlAnchor.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[F49I_PLAYER_CONTROL_QA] Fixture scene ready: " + ScenePath);
        }

        [MenuItem("Immersive Framework QA/Player/Open F49I PlayerControl Passive QA Scene")]
        public static void OpenPlayerControlPassiveScene()
        {
            if (!File.Exists(ToFullPath(ScenePath)))
            {
                CreateOrRefreshPlayerControlPassiveScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void ConfigurePlayerSlotDeclaration(PlayerSlotDeclaration declaration)
        {
            SerializedObject serialized = new SerializedObject(declaration);
            SetSerialized(serialized, "slotId", "player.1");
            SetSerialized(serialized, "displayName", "QA PlayerControl Slot");
            SetSerialized(serialized, "reason", "qa.player-control.slot-declaration");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(declaration);
        }

        private static void ConfigureActorDeclaration(ActorDeclaration declaration)
        {
            SerializedObject serialized = new SerializedObject(declaration);
            SetSerialized(serialized, "actorId", "qa.player-control.actor");
            SetSerialized(serialized, "displayName", "QA PlayerControl Actor");
            SetSerialized(serialized, "reason", "qa.player-control.actor-declaration");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(declaration);
        }

        private static void ConfigureActorReadinessBehaviour(ActorReadinessBehaviour readiness)
        {
            SerializedObject serialized = new SerializedObject(readiness);
            SetEnum(serialized, "initialState", ActorReadinessState.NotReady);
            SetSerialized(serialized, "initialReason", string.Empty);
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
            SetSerialized(serialized, "playerSlotDeclaration", slotDeclaration);
            SetSerialized(serialized, "playerSlotId", "player.1");
            SetSerialized(serialized, "actorDeclaration", actorDeclaration);
            SetSerialized(serialized, "playerActorDeclaration", (UnityEngine.Object)null);
            SetSerialized(serialized, "actorId", "qa.player-control.actor");
            SetSerialized(serialized, "actorReadinessBehaviour", readinessBehaviour);
            SetEnum(serialized, "initialActorReadinessState", ActorReadinessState.NotReady);
            SetSerialized(serialized, "initialActorReadinessReason", string.Empty);
            SetEnum(serialized, "initialState", PlayerEntryState.Configured);
            SetSerialized(serialized, "initialSuspensionReason", string.Empty);
            SetSerialized(serialized, "initialReason", "qa.player-control.entry.initial");
            SetBool(serialized, "rebuildOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static void ConfigurePlayerControlBehaviour(
            PlayerControlBehaviour control,
            PlayerSlotDeclaration slotDeclaration,
            PlayerEntryBehaviour entryBehaviour,
            UnityEngine.Transform controlTarget)
        {
            SerializedObject serialized = new SerializedObject(control);
            SetSerialized(serialized, "playerSlotDeclaration", slotDeclaration);
            SetSerialized(serialized, "playerSlotId", "player.1");
            SetSerialized(serialized, "playerEntryBehaviour", entryBehaviour);
            SetSerialized(serialized, "controlTarget", controlTarget);
            SetSerialized(serialized, "inputSourceId", "qa.input.local.intent");
            SetEnum(serialized, "initialState", PlayerControlState.Declared);
            SetSerialized(serialized, "initialSuspensionReason", string.Empty);
            SetSerialized(serialized, "initialReason", "qa.player-control.initial");
            SetBool(serialized, "rebuildOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(control);
        }

        private static void ConfigureFixture(
            QaPlayerControlPassiveFixture fixture,
            PlayerControlBehaviour controlBehaviour,
            PlayerEntryBehaviour entryBehaviour,
            ActorReadinessBehaviour readinessBehaviour,
            UnityEngine.Transform controlTarget)
        {
            SerializedObject serialized = new SerializedObject(fixture);
            SetSerialized(serialized, "controlBehaviour", controlBehaviour);
            SetSerialized(serialized, "entryBehaviour", entryBehaviour);
            SetSerialized(serialized, "actorReadinessBehaviour", readinessBehaviour);
            SetSerialized(serialized, "controlTarget", controlTarget);
            SetBool(serialized, "runOnStart", true);
            SetBool(serialized, "throwOnFailure", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fixture);
        }

        private static void CreateCamera(string name, Color backgroundColor)
        {
            GameObject cameraObject = new GameObject(name);
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
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
            SetSerialized(serialized, "routeName", routeName);
            SetSerialized(serialized, "primaryScenePath", scenePath);
            SetSerialized(serialized, "primarySceneName", Path.GetFileNameWithoutExtension(scenePath));
            SetSerialized(serialized, "startupActivity", startupActivity);
            SetSerialized(serialized, "description", description);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ActivityAsset CreateActivityAsset(string assetPath, string activityName, string description)
        {
            ActivityAsset asset = LoadOrCreate<ActivityAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetSerialized(serialized, "activityName", activityName);
            SetSerialized(serialized, "description", description);
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

        private static void SetSerialized(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = FindProperty(serialized, propertyName);
            property.objectReferenceValue = value;
        }

        private static void SetSerialized(SerializedObject serialized, string propertyName, string value)
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

using System;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerEntry;
using Immersive.Framework.PlayerSlots;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public static class QaPlayerTopologyPassiveSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Player";
        private const string ScenePath = Root + "/Scenes/QA_PlayerTopologyPassive.unity";
        private const string RoutePath = Root + "/Routes/QA_PlayerTopologyPassiveRoute.asset";
        private const string ActivityPath = Root + "/Activities/QA_PlayerTopologyPassiveActivity.asset";

        [MenuItem("Immersive Framework QA/Player/Create or Refresh F49F PlayerTopology Passive QA Scene")]
        public static void CreateOrRefreshPlayerTopologyPassiveScene()
        {
            EnsureFolders();

            ActivityAsset activity = CreateActivityAsset(
                ActivityPath,
                "QA PlayerTopology Passive Activity",
                "F49F PlayerTopology passive validation QA activity.");

            CreateRouteAsset(
                RoutePath,
                "QA PlayerTopology Passive Route",
                ScenePath,
                "F49F PlayerTopology passive validation QA route.",
                activity);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_PlayerTopologyPassive";

            CreateCamera("QA_PlayerTopologyPassiveCamera", new Color(0.032f, 0.044f, 0.052f, 1f));

            GameObject target = new GameObject("QA_PlayerTopologyPassive_Target");
            PlayerSlotDeclaration slotDeclaration = target.AddComponent<PlayerSlotDeclaration>();
            ActorDeclaration actorDeclaration = target.AddComponent<ActorDeclaration>();
            ActorReadinessBehaviour readinessBehaviour = target.AddComponent<ActorReadinessBehaviour>();
            PlayerEntryBehaviour entryBehaviour = target.AddComponent<PlayerEntryBehaviour>();
            PlayerSlotOccupancy occupancy = target.AddComponent<PlayerSlotOccupancy>();

            ConfigurePlayerSlotDeclaration(slotDeclaration);
            ConfigureActorDeclaration(actorDeclaration);
            ConfigureActorReadinessBehaviour(readinessBehaviour);
            ConfigurePlayerEntryBehaviour(entryBehaviour, slotDeclaration, actorDeclaration, readinessBehaviour);
            ConfigurePlayerSlotOccupancy(occupancy, slotDeclaration, actorDeclaration);

            GameObject fixtureObject = new GameObject("QA_F49F_PlayerTopologyPassive");
            QaPlayerTopologyPassiveFixture fixture = fixtureObject.AddComponent<QaPlayerTopologyPassiveFixture>();
            ConfigureFixture(fixture, slotDeclaration, actorDeclaration, occupancy, readinessBehaviour, entryBehaviour);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[F49F_PLAYER_TOPOLOGY_QA] Fixture scene ready: " + ScenePath);
        }

        [MenuItem("Immersive Framework QA/Player/Open F49F PlayerTopology Passive QA Scene")]
        public static void OpenPlayerTopologyPassiveScene()
        {
            if (!File.Exists(ToFullPath(ScenePath)))
            {
                CreateOrRefreshPlayerTopologyPassiveScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void ConfigurePlayerSlotDeclaration(PlayerSlotDeclaration declaration)
        {
            SerializedObject serialized = new SerializedObject(declaration);
            SetSerialized(serialized, "slotId", "player.1");
            SetSerialized(serialized, "displayName", "QA PlayerTopology Slot");
            SetSerialized(serialized, "reason", "qa.player-topology.slot-declaration");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(declaration);
        }

        private static void ConfigureActorDeclaration(ActorDeclaration declaration)
        {
            SerializedObject serialized = new SerializedObject(declaration);
            SetSerialized(serialized, "actorId", "qa.player-topology.actor");
            SetSerialized(serialized, "displayName", "QA PlayerTopology Actor");
            SetSerialized(serialized, "reason", "qa.player-topology.actor-declaration");
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
            SetSerialized(serialized, "actorId", "qa.player-topology.actor");
            SetSerialized(serialized, "actorReadinessBehaviour", readinessBehaviour);
            SetEnum(serialized, "initialActorReadinessState", ActorReadinessState.NotReady);
            SetSerialized(serialized, "initialActorReadinessReason", string.Empty);
            SetEnum(serialized, "initialState", PlayerEntryState.Configured);
            SetSerialized(serialized, "initialSuspensionReason", string.Empty);
            SetSerialized(serialized, "initialReason", "qa.player-topology.entry-initial");
            SetBool(serialized, "rebuildOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static void ConfigurePlayerSlotOccupancy(
            PlayerSlotOccupancy occupancy,
            PlayerSlotDeclaration slotDeclaration,
            ActorDeclaration actorDeclaration)
        {
            SerializedObject serialized = new SerializedObject(occupancy);
            SetSerialized(serialized, "slotDeclaration", slotDeclaration);
            SetSerialized(serialized, "slotId", "player.1");
            SetSerialized(serialized, "actorDeclaration", actorDeclaration);
            SetSerialized(serialized, "playerActorDeclaration", (UnityEngine.Object)null);
            SetSerialized(serialized, "occupiedActorId", "qa.player-topology.actor");
            SetSerialized(serialized, "displayName", "QA PlayerTopology Occupancy");
            SetSerialized(serialized, "reason", "qa.player-topology.occupancy");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(occupancy);
        }

        private static void ConfigureFixture(
            QaPlayerTopologyPassiveFixture fixture,
            PlayerSlotDeclaration slotDeclaration,
            ActorDeclaration actorDeclaration,
            PlayerSlotOccupancy occupancy,
            ActorReadinessBehaviour readinessBehaviour,
            PlayerEntryBehaviour entryBehaviour)
        {
            SerializedObject serialized = new SerializedObject(fixture);
            SetSerialized(serialized, "playerSlotDeclaration", slotDeclaration);
            SetSerialized(serialized, "actorDeclaration", actorDeclaration);
            SetSerialized(serialized, "playerSlotOccupancy", occupancy);
            SetSerialized(serialized, "actorReadinessBehaviour", readinessBehaviour);
            SetSerialized(serialized, "playerEntryBehaviour", entryBehaviour);
            SetBool(serialized, "runOnStart", true);
            SetBool(serialized, "throwOnFailure", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(fixture);
        }

        private static void CreateCamera(string name, Color backgroundColor)
        {
            GameObject cameraObject = new GameObject(name);
            Camera camera = cameraObject.AddComponent<Camera>();
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

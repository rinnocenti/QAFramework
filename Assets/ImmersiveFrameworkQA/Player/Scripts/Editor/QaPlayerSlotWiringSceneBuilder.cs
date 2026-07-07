using System;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.UnityInput;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public static class QaPlayerSlotWiringSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Player";
        private const string ScenePath = Root + "/Scenes/QA_PlayerSlotWiring.unity";
        private const string InputActionsPath = Root + "/Input/QA_PlayerSlotWiring_InputActions.asset";

        [MenuItem("Immersive Framework QA/Player/Create or Refresh F48C PlayerSlot Wiring QA Scene")]
        public static void CreateOrRefreshPlayerSlotWiringScene()
        {
            EnsureFolders();
            InputActionAsset inputActions = EnsureInputActions();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_PlayerSlotWiring";

            GameObject root = new GameObject("QA_F48C_PlayerSlotWiring");
            var fixture = root.AddComponent<QaPlayerSlotWiringFixture>();

            GameObject player = new GameObject("QA_PlayerSlotWiring_Player");
            player.transform.SetParent(root.transform, false);
            var playerInput = player.AddComponent<PlayerInput>();
            playerInput.actions = inputActions;
            playerInput.defaultActionMap = "Player";

            var slot = player.AddComponent<PlayerSlotDeclaration>();
            SetString(slot, "slotId", "player.1");
            SetString(slot, "displayName", "QA Player 1");
            SetObject(slot, "playerInput", playerInput);
            SetString(slot, "reason", "qa.playerslot-wiring.slot");

            var playerActor = player.AddComponent<PlayerActorDeclaration>();
            SetString(playerActor, "actorId", "qa.player.actor");
            SetString(playerActor, "displayName", "QA Player Actor");
            SetObject(playerActor, "playerInput", playerInput);
            SetString(playerActor, "reason", "qa.playerslot-wiring.player-actor");

            var occupancy = player.AddComponent<PlayerSlotOccupancy>();
            SetObject(occupancy, "slotDeclaration", slot);
            SetString(occupancy, "slotId", string.Empty);
            SetObject(occupancy, "actorDeclaration", null);
            SetObject(occupancy, "playerActorDeclaration", playerActor);
            SetString(occupancy, "occupiedActorId", string.Empty);
            SetString(occupancy, "displayName", "QA Player 1 occupies QA Player Actor");
            SetString(occupancy, "reason", "qa.playerslot-wiring.occupancy");

            var gateAdapter = player.AddComponent<UnityPlayerInputGateAdapter>();
            SetObject(gateAdapter, "playerInput", playerInput);
            SetString(gateAdapter, "gameplayActionMapName", "Player");
            SetObject(gateAdapter, "sourceSlot", slot);
            SetBool(gateAdapter, "blockOnInputAcceptance", true);
            SetBool(gateAdapter, "blockOnGameplayAction", true);
            SetEnumValue(gateAdapter, "blockMode", UnityPlayerInputGateBlockMode.DisableActionMap);
            SetBool(gateAdapter, "restorePreviousState", true);
            SetBool(gateAdapter, "applyOnEnable", true);
            SetBool(gateAdapter, "logStateChanges", true);
            SetBool(gateAdapter, "logMissingRuntimeOnce", true);
            SetBool(gateAdapter, "logMissingTargetOnce", true);

            var resetSubject = player.AddComponent<UnityResetSubjectAdapter>();
            SetBool(resetSubject, "registerOnEnable", true);
            SetBool(resetSubject, "unregisterOnDisable", true);
            SetBool(resetSubject, "retryUntilRuntimeAvailable", true);
            SetEnumValue(resetSubject, "idGeneration", UnityResetSubjectIdGenerationMode.AuthoredStableId);
            SetString(resetSubject, "subjectId", string.Empty);
            SetString(resetSubject, "runtimeSubjectIdPrefix", string.Empty);
            SetEnumValue(resetSubject, "scope", ResetSubjectScope.Route);
            SetString(resetSubject, "displayName", "QA Player Reset Subject");
            SetString(resetSubject, "diagnosticTag", "QA:F48C:PlayerSlotWiring");
            SetObject(resetSubject, "sourceActor", null);
            SetObject(resetSubject, "sourcePlayerActor", playerActor);
            SetEnumValue(resetSubject, "participantDiscovery", UnityResetParticipantDiscoveryMode.Children);
            SetBool(resetSubject, "includeInactiveParticipants", true);
            SetBool(resetSubject, "includeUnityResettableComponents", true);

            player.AddComponent<UnityTransformResetParticipant>();

            GameObject conflict = new GameObject("QA_PlayerSlotWiring_Conflict");
            conflict.transform.SetParent(root.transform, false);
            var conflictInput = conflict.AddComponent<PlayerInput>();
            conflictInput.actions = inputActions;
            conflictInput.defaultActionMap = "Player";

            var conflictPlayerActor = conflict.AddComponent<PlayerActorDeclaration>();
            SetString(conflictPlayerActor, "actorId", "qa.player.actor");
            SetString(conflictPlayerActor, "displayName", "QA Conflict PlayerActor");
            SetObject(conflictPlayerActor, "playerInput", conflictInput);
            SetString(conflictPlayerActor, "reason", "qa.playerslot-wiring.conflict-player-actor");

            var conflictActor = conflict.AddComponent<ActorDeclaration>();
            SetString(conflictActor, "actorId", "qa.conflicting.actor");
            SetEnumValue(conflictActor, "actorKind", ActorKind.NonPlayer);
            SetEnumValue(conflictActor, "actorRole", ActorRole.Neutral);
            SetString(conflictActor, "displayName", "QA Conflicting Actor");
            SetString(conflictActor, "reason", "qa.playerslot-wiring.conflict-actor");

            var conflictOccupancy = conflict.AddComponent<PlayerSlotOccupancy>();
            SetObject(conflictOccupancy, "slotDeclaration", slot);
            SetString(conflictOccupancy, "slotId", string.Empty);
            SetObject(conflictOccupancy, "actorDeclaration", conflictActor);
            SetObject(conflictOccupancy, "playerActorDeclaration", conflictPlayerActor);
            SetString(conflictOccupancy, "occupiedActorId", string.Empty);
            SetString(conflictOccupancy, "displayName", "QA Conflicting Occupancy");
            SetString(conflictOccupancy, "reason", "qa.playerslot-wiring.conflict");

            SetObject(fixture, "playerInput", playerInput);
            SetObject(fixture, "playerSlot", slot);
            SetObject(fixture, "playerActor", playerActor);
            SetObject(fixture, "occupancy", occupancy);
            SetObject(fixture, "inputGateAdapter", gateAdapter);
            SetObject(fixture, "resetSubjectAdapter", resetSubject);
            SetObject(fixture, "conflictingOccupancy", conflictOccupancy);
            SetBool(fixture, "runOnStart", true);
            SetBool(fixture, "applyGateAdapterOnStart", true);
            SetBool(fixture, "registerResetSubjectOnStart", true);
            SetBool(fixture, "retryUntilPassed", true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[F48C_PLAYER_SLOT_WIRING_QA] Fixture scene ready: " + ScenePath);
        }

        [MenuItem("Immersive Framework QA/Player/Open F48C PlayerSlot Wiring QA Scene")]
        public static void OpenPlayerSlotWiringScene()
        {
            if (!File.Exists(ToFullPath(ScenePath)))
            {
                CreateOrRefreshPlayerSlotWiringScene();
                return;
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static InputActionAsset EnsureInputActions()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (existing != null)
            {
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var playerMap = new InputActionMap("Player");
            playerMap.AddAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
            playerMap.AddAction("Confirm", InputActionType.Button, "<Keyboard>/space");
            asset.AddActionMap(playerMap);

            AssetDatabase.CreateAsset(asset, InputActionsPath);
            return asset;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "Player");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Input");
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

        private static void SetString(Component component, string propertyName, string value)
        {
            SerializedProperty property = FindProperty(component, propertyName);
            property.stringValue = value ?? string.Empty;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Component component, string propertyName, bool value)
        {
            SerializedProperty property = FindProperty(component, propertyName);
            property.boolValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnumValue<TEnum>(Component component, string propertyName, TEnum value)
            where TEnum : Enum
        {
            SerializedProperty property = FindProperty(component, propertyName);
            string enumName = Enum.GetName(typeof(TEnum), value);
            if (string.IsNullOrWhiteSpace(enumName))
            {
                throw new InvalidOperationException($"Enum value '{value}' is not valid for {typeof(TEnum).Name}.");
            }

            for (int i = 0; i < property.enumNames.Length; i++)
            {
                if (string.Equals(property.enumNames[i], enumName, StringComparison.Ordinal))
                {
                    property.enumValueIndex = i;
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    return;
                }
            }

            throw new InvalidOperationException($"Serialized enum property '{propertyName}' on {component.GetType().Name} does not expose value '{enumName}'.");
        }

        private static void SetObject(Component component, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = FindProperty(component, propertyName);
            property.objectReferenceValue = value;
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty FindProperty(Component component, string propertyName)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {component.GetType().Name}.");
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

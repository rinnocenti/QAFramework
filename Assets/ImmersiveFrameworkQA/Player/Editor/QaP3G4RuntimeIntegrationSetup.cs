using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent P3G.4 fixture installer. It creates the real local Player prefab and installs
    /// one explicit manual PlayerInputManager + LocalPlayerProvisioningAuthoring in QA_UIGlobal.
    /// </summary>
    public static class QaP3G4RuntimeIntegrationSetup
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3G.4 Apply Real Join Fixture";
        private const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/P3G4";
        private const string ActionsPath =
            RootFolder + "/P3G4_InputActions.asset";
        private const string PlayerPrefabPath =
            RootFolder + "/P3G4_LocalPlayerHost.prefab";
        private const string FixtureName =
            "P3G4 Local Player Provisioning";
        private const string TargetSceneName = "QA_UIGlobal";
        private const string JoinEvidenceBindingPath = "<Keyboard>/space";

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            try
            {
                EnsureFolder(RootFolder);
                InputActionAsset actions = CreateOrUpdateInputActions();
                GameObject playerPrefab = CreateOrUpdatePlayerPrefab(actions);
                string scenePath = FindUniqueScenePath(TargetSceneName);

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Debug.LogWarning(
                        "[P3G4_RUNTIME_JOIN_FIXTURE_SETUP] status='Cancelled' reason='UnsavedScenes'.");
                    return;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                PlayerInputManager manager = ResolveOrCreateManager(scene);
                ConfigureManager(manager, playerPrefab);

                LocalPlayerProvisioningAuthoring authoring =
                    manager.GetComponent<LocalPlayerProvisioningAuthoring>();
                if (authoring == null)
                {
                    authoring = manager.gameObject.AddComponent<LocalPlayerProvisioningAuthoring>();
                }

                var serializedAuthoring = new SerializedObject(authoring);
                SerializedProperty managerProperty =
                    serializedAuthoring.FindProperty("playerInputManager");
                if (managerProperty == null)
                {
                    throw new InvalidOperationException(
                        "LocalPlayerProvisioningAuthoring.playerInputManager was not found.");
                }

                managerProperty.objectReferenceValue = manager;
                serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(manager.gameObject);
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(authoring);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3G4_RUNTIME_JOIN_FIXTURE_SETUP] status='Applied' " +
                    $"scene='{scenePath}' fixture='{manager.gameObject.name}' " +
                    $"prefab='{PlayerPrefabPath}' joinBehavior='{manager.joinBehavior}' " +
                    $"notificationBehavior='{manager.notificationBehavior}' " +
                    $"maxPlayers='{manager.maxPlayerCount}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3G4_RUNTIME_JOIN_FIXTURE_SETUP] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static InputActionAsset CreateOrUpdateInputActions()
        {
            InputActionAsset actions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null)
            {
                actions = ScriptableObject.CreateInstance<InputActionAsset>();
                actions.name = "P3G4 Input Actions";
                AssetDatabase.CreateAsset(actions, ActionsPath);
            }

            InputActionMap gameplay = actions.FindActionMap("Gameplay", false);
            if (gameplay == null)
            {
                gameplay = actions.AddActionMap("Gameplay");
            }

            InputAction joinEvidence = gameplay.FindAction("JoinEvidence", false);
            if (joinEvidence == null)
            {
                joinEvidence = gameplay.AddAction("JoinEvidence", InputActionType.Button);
            }

            EnsureBinding(joinEvidence, JoinEvidenceBindingPath);

            EditorUtility.SetDirty(actions);
            AssetDatabase.SaveAssets();
            return actions;
        }

        private static void EnsureBinding(InputAction action, string path)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            for (int index = 0; index < action.bindings.Count; index++)
            {
                InputBinding binding = action.bindings[index];
                if (string.Equals(binding.path, path, StringComparison.Ordinal))
                {
                    return;
                }
            }

            action.AddBinding(path);
        }

        private static GameObject CreateOrUpdatePlayerPrefab(InputActionAsset actions)
        {
            GameObject temporary = new GameObject("P3G4 Local Player Host");
            try
            {
                PlayerInput playerInput = temporary.AddComponent<PlayerInput>();
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Gameplay";

                PlayerActorDeclaration declaration =
                    temporary.AddComponent<PlayerActorDeclaration>();
                var serializedDeclaration = new SerializedObject(declaration);
                serializedDeclaration.FindProperty("actorId").stringValue =
                    "qa.p3g4.actor.local-player";
                serializedDeclaration.FindProperty("displayName").stringValue =
                    "P3G4 Local Player";
                serializedDeclaration.FindProperty("playerInput").objectReferenceValue =
                    playerInput;
                serializedDeclaration.FindProperty("reason").stringValue =
                    "qa.p3g4.real-playerinputmanager-join";
                serializedDeclaration.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporary,
                    PlayerPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Could not create Player prefab at '{PlayerPrefabPath}'.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static PlayerInputManager ResolveOrCreateManager(Scene scene)
        {
            var managers = new List<PlayerInputManager>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                managers.AddRange(
                    roots[rootIndex].GetComponentsInChildren<PlayerInputManager>(true));
            }

            if (managers.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.name}' contains '{managers.Count}' PlayerInputManager components. P3G.4 requires one Session manager.");
            }

            if (managers.Count == 1)
            {
                return managers[0];
            }

            var fixture = new GameObject(FixtureName);
            SceneManager.MoveGameObjectToScene(fixture, scene);
            return fixture.AddComponent<PlayerInputManager>();
        }

        private static void ConfigureManager(
            PlayerInputManager manager,
            GameObject playerPrefab)
        {
            manager.gameObject.name = FixtureName;
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            manager.playerPrefab = playerPrefab;

            var serializedManager = new SerializedObject(manager);
            serializedManager.Update();
            SerializedProperty maxPlayerCountProperty =
                serializedManager.FindProperty("m_MaxPlayerCount") ??
                serializedManager.FindProperty("maxPlayerCount");
            if (maxPlayerCountProperty == null)
            {
                throw new InvalidOperationException(
                    "PlayerInputManager serialized max-player-count field was not found.");
            }

            maxPlayerCountProperty.intValue = 4;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string FindUniqueScenePath(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            var exactMatches = new List<string>();
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                if (string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        sceneName,
                        StringComparison.Ordinal))
                {
                    exactMatches.Add(path);
                }
            }

            if (exactMatches.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one Scene named '{sceneName}', but found '{exactMatches.Count}'.");
            }

            return exactMatches[0];
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }

                current = next;
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

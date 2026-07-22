using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent real-join fixture. Creates one reusable Local Player technical-host prefab and
    /// installs one explicit manual PlayerInputManager + provisioning authoring and host
    /// registration in QA_UIGlobal.
    /// </summary>
    internal static class QaLocalPlayerRuntimeIntegrationSetup
    {
        private const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/LocalPlayerRuntimeIntegration";
        private const string ActionsPath = RootFolder + "/LocalPlayerInputActions.asset";
        private const string PlayerPrefabPath = RootFolder + "/LocalPlayerHost.prefab";
        private const string FixtureName = "Local Player Provisioning";
        private const string TargetSceneName = "QA_UIGlobal";
        private const string JoinEvidenceBindingPath = "<Keyboard>/space";

        internal static void Apply()
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
                        "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='Cancelled' reason='UnsavedScenes'.");
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
                serializedAuthoring.FindProperty("playerInputManager").objectReferenceValue = manager;
                serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();

                LocalPlayerActorSelectionRequestAuthoring[] selectionEndpoints =
                    ResolveSceneActorSelectionEndpoints(scene);
                if (selectionEndpoints.Length > 1)
                {
                    throw new InvalidOperationException(
                        $"Scene '{scene.name}' contains '{selectionEndpoints.Length}' Local Player Actor Selection Request authorings. Exactly one is required.");
                }

                if (selectionEndpoints.Length == 1 &&
                    !ReferenceEquals(
                        selectionEndpoints[0].gameObject,
                        manager.gameObject))
                {
                    throw new InvalidOperationException(
                        $"Scene '{scene.name}' contains its Local Player Actor Selection Request authoring on '{selectionEndpoints[0].gameObject.name}', but the canonical provisioning object is '{manager.gameObject.name}'.");
                }

                LocalPlayerActorSelectionRequestAuthoring selectionEndpoint =
                    selectionEndpoints.Length == 1
                        ? selectionEndpoints[0]
                        : manager.gameObject.AddComponent<
                            LocalPlayerActorSelectionRequestAuthoring>();
                var serializedSelectionEndpoint =
                    new SerializedObject(selectionEndpoint);
                serializedSelectionEndpoint.FindProperty("provisioningAuthoring")
                    .objectReferenceValue = authoring;
                serializedSelectionEndpoint.ApplyModifiedPropertiesWithoutUndo();

                LocalPlayerProvisioningHostRegistration registration =
                    ResolveOrCreateRegistration(scene, manager, authoring);

                EditorUtility.SetDirty(manager.gameObject);
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(authoring);
                EditorUtility.SetDirty(selectionEndpoint);
                EditorUtility.SetDirty(registration);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                LocalPlayerHostAuthoring host =
                    playerPrefab.GetComponent<LocalPlayerHostAuthoring>();
                Debug.Log(
                    "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='Applied' " +
                    $"scene='{scenePath}' fixture='{manager.gameObject.name}' " +
                    $"registration='{registration.name}' " +
                    $"actorSelectionEndpoint='{selectionEndpoint.name}' " +
                    $"prefab='{PlayerPrefabPath}' host='{host.name}' actorMount='{host.ActorMount.name}' " +
                    $"joinBehavior='{manager.joinBehavior}' notificationBehavior='{manager.notificationBehavior}' " +
                    $"maxPlayers='{manager.maxPlayerCount}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static LocalPlayerActorSelectionRequestAuthoring[]
            ResolveSceneActorSelectionEndpoints(Scene scene)
        {
            var endpoints =
                new List<LocalPlayerActorSelectionRequestAuthoring>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                endpoints.AddRange(
                    roots[index].GetComponentsInChildren<
                        LocalPlayerActorSelectionRequestAuthoring>(true));
            }

            return endpoints.ToArray();
        }

        private static InputActionAsset CreateOrUpdateInputActions()
        {
            InputActionAsset actions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null)
            {
                actions = ScriptableObject.CreateInstance<InputActionAsset>();
                actions.name = "Local Player Input Actions";
                AssetDatabase.CreateAsset(actions, ActionsPath);
            }

            InputActionMap gameplay = actions.FindActionMap("Gameplay", false) ??
                actions.AddActionMap("Gameplay");
            InputAction joinEvidence = gameplay.FindAction("JoinEvidence", false) ??
                gameplay.AddAction("JoinEvidence", InputActionType.Button);
            EnsureBinding(joinEvidence, JoinEvidenceBindingPath);

            EditorUtility.SetDirty(actions);
            AssetDatabase.SaveAssets();
            return actions;
        }

        private static void EnsureBinding(InputAction action, string path)
        {
            for (int index = 0; index < action.bindings.Count; index++)
            {
                if (string.Equals(action.bindings[index].path, path, StringComparison.Ordinal))
                {
                    return;
                }
            }

            action.AddBinding(path);
        }

        private static GameObject CreateOrUpdatePlayerPrefab(InputActionAsset actions)
        {
            GameObject temporary = new GameObject("Local Player Host");
            try
            {
                PlayerInput playerInput = temporary.AddComponent<PlayerInput>();
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Gameplay";

                var actorMountObject = new GameObject("ActorMount");
                actorMountObject.transform.SetParent(temporary.transform, false);

                LocalPlayerHostAuthoring host =
                    temporary.AddComponent<LocalPlayerHostAuthoring>();
                var serializedHost = new SerializedObject(host);
                serializedHost.FindProperty("playerInput").objectReferenceValue = playerInput;
                serializedHost.FindProperty("actorMount").objectReferenceValue =
                    actorMountObject.transform;
                serializedHost.ApplyModifiedPropertiesWithoutUndo();

                UnityPlayerInputGateAdapter gate =
                    temporary.AddComponent<UnityPlayerInputGateAdapter>();
                var serializedGate = new SerializedObject(gate);
                serializedGate.FindProperty("playerInput").objectReferenceValue =
                    playerInput;
                serializedGate.FindProperty("gameplayActionMapName").stringValue =
                    "Gameplay";
                SetOptionalBoolean(serializedGate, "logStateChanges", false);
                SetOptionalBoolean(serializedGate, "logMissingRuntimeOnce", false);
                SetOptionalBoolean(serializedGate, "logMissingTargetOnce", false);
                serializedGate.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporary,
                    PlayerPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Could not create Local Player Host prefab at '{PlayerPrefabPath}'.");
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
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                managers.AddRange(root.GetComponentsInChildren<PlayerInputManager>(true));
            }

            if (managers.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.name}' contains '{managers.Count}' PlayerInputManager components. Local Player runtime integration requires one Session manager.");
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
                serializedManager.FindProperty("m_MaxPlayerCount");
            if (maxPlayerCountProperty == null)
            {
                throw new InvalidOperationException(
                    "PlayerInputManager serialized max-player-count field was not found.");
            }

            maxPlayerCountProperty.intValue = 4;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static LocalPlayerProvisioningHostRegistration ResolveOrCreateRegistration(
            Scene scene,
            PlayerInputManager manager,
            LocalPlayerProvisioningAuthoring authoring)
        {
            var registrations = new List<LocalPlayerProvisioningHostRegistration>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                registrations.AddRange(
                    root.GetComponentsInChildren<LocalPlayerProvisioningHostRegistration>(true));
            }

            if (registrations.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.name}' contains '{registrations.Count}' Local Player Provisioning Host Registrations. Exactly one is required.");
            }

            LocalPlayerProvisioningHostRegistration registration = registrations.Count == 1
                ? registrations[0]
                : manager.gameObject.AddComponent<LocalPlayerProvisioningHostRegistration>();
            var serializedRegistration = new SerializedObject(registration);
            serializedRegistration.FindProperty("provisioningAuthoring").objectReferenceValue = authoring;
            serializedRegistration.ApplyModifiedPropertiesWithoutUndo();
            return registration;
        }

        private static void SetOptionalBoolean(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static string FindUniqueScenePath(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            var exactMatches = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName,
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

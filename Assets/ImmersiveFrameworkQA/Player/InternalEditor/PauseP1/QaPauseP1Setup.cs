using System;
using System.Linq;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace ImmersiveFrameworkQA.InputMode.Internal.Editor.ImmersiveFrameworkQA.Player.InternalEditor.PauseP1
{
    internal static class QaPauseP1Paths
    {
        internal const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/PauseP1";
        internal const string SettingsFolder =
            RootFolder + "/Settings";
        internal const string ScenesFolder =
            RootFolder + "/Scenes";
        internal const string ScenePath =
            ScenesFolder + "/QA_PauseProductBinding.unity";
        internal const string InputActionsPath =
            "Assets/InputSystem_Actions.inputactions";
        internal const string PauseActionReferencePath =
            SettingsFolder + "/QA_PauseToggle.inputactionreference.asset";
        internal const string PauseActionReferenceAssetName =
            "QA_PauseToggle.inputactionreference";
        internal const string PauseActionPath =
            "Global/PauseToggle";
    }

    internal static class QaPauseP1Setup
    {
        private const string MenuRoot =
            "Immersive Framework/QA/Player/Pause P1/";
        private const string LogPrefix =
            "[QA][PAUSE-P1][SETUP]";

        internal static void SetupOrRebuild()
        {
            try
            {
                EnsureFolder(QaPauseP1Paths.RootFolder);
                EnsureFolder(QaPauseP1Paths.SettingsFolder);
                EnsureFolder(QaPauseP1Paths.ScenesFolder);

                InputActionAsset actionAsset =
                    AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                        QaPauseP1Paths.InputActionsPath);
                if (actionAsset == null)
                {
                    throw new InvalidOperationException(
                        $"QA Pause P1 requires '{QaPauseP1Paths.InputActionsPath}'.");
                }

                InputAction pauseAction =
                    actionAsset.FindAction(
                        QaPauseP1Paths.PauseActionPath,
                        false);
                if (pauseAction == null)
                {
                    throw new InvalidOperationException(
                        $"QA Pause P1 requires action '{QaPauseP1Paths.PauseActionPath}'.");
                }

                RequireMap(actionAsset, "Global");
                RequireMap(actionAsset, "Player");

                InputActionReference pauseReference =
                    CreatePersistAndReloadPauseReference(pauseAction);

                Scene scene = EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

                pauseReference =
                    LoadAndValidatePersistedPauseReference(
                        pauseAction);

                var sceneRoot =
                    new GameObject("QA Pause P1 Consumer");
                SceneManager.MoveGameObjectToScene(sceneRoot, scene);

                CreatePlayer(
                    sceneRoot.transform,
                    actionAsset,
                    pauseReference);
                CreateControls(sceneRoot.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(
                        scene,
                        QaPauseP1Paths.ScenePath))
                {
                    throw new InvalidOperationException(
                        $"Unable to save QA Pause P1 scene '{QaPauseP1Paths.ScenePath}'.");
                }

                AddSceneToBuildSettings(QaPauseP1Paths.ScenePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    QaPauseP1Paths.ScenePath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);

                Scene persistedScene =
                    EditorSceneManager.OpenScene(
                        QaPauseP1Paths.ScenePath,
                        OpenSceneMode.Single);

                ValidatePersistedScene(
                    persistedScene,
                    actionAsset,
                    pauseAction);

                Debug.Log(
                    $"{LogPrefix} PASS. " +
                    $"scene='{QaPauseP1Paths.ScenePath}' " +
                    $"action='{QaPauseP1Paths.PauseActionPath}' " +
                    "components='PlayerInput,PausePlayerInputBinding," +
                    "UnityPlayerInputGateAdapter,PauseRequestTrigger' " +
                    "serializedPauseAction='Valid'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. " +
                    $"type='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");

                throw;
            }
        }

        internal static void OpenScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    QaPauseP1Paths.ScenePath) == null)
            {
                throw new InvalidOperationException(
                    "QA Pause P1 scene does not exist. " +
                    "Run Setup or Rebuild first.");
            }

            EditorSceneManager.OpenScene(
                QaPauseP1Paths.ScenePath,
                OpenSceneMode.Single);
        }

        private static void CreatePlayer(
            Transform parent,
            InputActionAsset actionAsset,
            InputActionReference pauseReference)
        {
            var playerObject =
                new GameObject("QA Pause P1 PlayerInput");
            playerObject.transform.SetParent(parent, false);
            playerObject.SetActive(false);

            PlayerInput playerInput =
                playerObject.AddComponent<PlayerInput>();
            playerInput.actions = actionAsset;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior =
                PlayerNotifications.InvokeCSharpEvents;

            UnityPlayerInputGateAdapter adapter =
                playerObject.AddComponent<UnityPlayerInputGateAdapter>();
            ConfigureAdapter(adapter, playerInput);

            PausePlayerInputBinding binding =
                playerObject.AddComponent<PausePlayerInputBinding>();
            ConfigureBinding(
                binding,
                playerInput,
                pauseReference);

            playerObject.SetActive(true);
        }

        private static void CreateControls(Transform parent)
        {
            var controls =
                new GameObject("QA Pause P1 UIGlobal Controls");
            controls.transform.SetParent(parent, false);

            PauseRequestTrigger trigger =
                controls.AddComponent<PauseRequestTrigger>();

            var serialized = new SerializedObject(trigger);
            serialized.Update();

            SerializedProperty reason =
                serialized.FindProperty("reason");
            if (reason != null)
            {
                reason.stringValue = "qa.pause.p1.ui";
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);
        }

        private static void ConfigureAdapter(
            UnityPlayerInputGateAdapter adapter,
            PlayerInput playerInput)
        {
            var serialized = new SerializedObject(adapter);
            serialized.Update();

            SetObject(
                serialized,
                "playerInput",
                playerInput);
            SetString(
                serialized,
                "gameplayActionMapName",
                "Player");

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(adapter);
        }

        private static void ConfigureBinding(
            PausePlayerInputBinding binding,
            PlayerInput playerInput,
            InputActionReference pauseReference)
        {
            if (pauseReference == null ||
                pauseReference.action == null)
            {
                throw new InvalidOperationException(
                    "Pause binding configuration requires a persisted " +
                    "InputActionReference with a resolved action.");
            }

            var serialized = new SerializedObject(binding);
            serialized.Update();

            SetObject(
                serialized,
                "playerInput",
                playerInput);
            SetObject(
                serialized,
                "pauseAction",
                pauseReference);
            SetString(
                serialized,
                "globalActionMapName",
                "Global");
            SetString(
                serialized,
                "gameplayActionMapName",
                "Player");

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binding);

            if (!ReferenceEquals(binding.PlayerInput, playerInput) ||
                !ReferenceEquals(binding.PauseAction, pauseReference))
            {
                throw new InvalidOperationException(
                    "Pause binding serialized references were not applied " +
                    "before scene save.");
            }
        }

        private static InputActionReference
            CreatePersistAndReloadPauseReference(
                InputAction pauseAction)
        {
            if (pauseAction == null)
            {
                throw new ArgumentNullException(nameof(pauseAction));
            }

            InputActionReference reference =
                AssetDatabase.LoadAssetAtPath<InputActionReference>(
                    QaPauseP1Paths.PauseActionReferencePath);

            if (reference == null)
            {
                UnityEngine.Object existingMainObject =
                    AssetDatabase.LoadMainAssetAtPath(
                        QaPauseP1Paths.PauseActionReferencePath);
                if (existingMainObject != null)
                {
                    throw new InvalidOperationException(
                        "The generated Pause InputActionReference path is occupied " +
                        "by an incompatible asset. " +
                        $"path='{QaPauseP1Paths.PauseActionReferencePath}' " +
                        $"type='{existingMainObject.GetType().FullName}'.");
                }

                reference =
                    InputActionReference.Create(pauseAction);

                reference.name =
                    QaPauseP1Paths.PauseActionReferenceAssetName;

                AssetDatabase.CreateAsset(
                    reference,
                    QaPauseP1Paths.PauseActionReferencePath);
            }
            else
            {
                reference.Set(pauseAction);

                reference.name =
                    QaPauseP1Paths.PauseActionReferenceAssetName;
                EditorUtility.SetDirty(reference);
            }

            reference.name =
                QaPauseP1Paths.PauseActionReferenceAssetName;
            EditorUtility.SetDirty(reference);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                QaPauseP1Paths.PauseActionReferencePath,
                ImportAssetOptions.ForceSynchronousImport |
                ImportAssetOptions.ForceUpdate);

            return LoadAndValidatePersistedPauseReference(
                pauseAction);
        }

        private static InputActionReference
            LoadAndValidatePersistedPauseReference(
                InputAction expectedPauseAction)
        {
            InputActionReference reference =
                AssetDatabase.LoadAssetAtPath<InputActionReference>(
                    QaPauseP1Paths.PauseActionReferencePath);

            if (reference == null)
            {
                throw new InvalidOperationException(
                    "Pause InputActionReference was not persisted.");
            }

            if (!string.Equals(
                    reference.name,
                    QaPauseP1Paths.PauseActionReferenceAssetName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Persisted Pause InputActionReference main-object name is invalid. " +
                    $"expected='{QaPauseP1Paths.PauseActionReferenceAssetName}' " +
                    $"actual='{reference.name}'.");
            }

            string persistedPath =
                AssetDatabase.GetAssetPath(reference);
            if (!string.Equals(
                    persistedPath,
                    QaPauseP1Paths.PauseActionReferencePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Pause InputActionReference resolved from an unexpected asset path. " +
                    $"expected='{QaPauseP1Paths.PauseActionReferencePath}' " +
                    $"actual='{persistedPath}'.");
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                    reference,
                    out string assetGuid,
                    out long localFileId) ||
                string.IsNullOrWhiteSpace(assetGuid) ||
                localFileId == 0)
            {
                throw new InvalidOperationException(
                    "Pause InputActionReference is not a persistent Unity asset.");
            }

            InputAction resolvedAction = reference.action;
            if (resolvedAction == null)
            {
                throw new InvalidOperationException(
                    "Persisted Pause InputActionReference does not resolve an action. " +
                    $"assetGuid='{assetGuid}' localFileId='{localFileId}'.");
            }

            if (resolvedAction.id != expectedPauseAction.id)
            {
                throw new InvalidOperationException(
                    "Persisted Pause InputActionReference points to a different action. " +
                    $"expected='{expectedPauseAction.id}' " +
                    $"actual='{resolvedAction.id}'.");
            }

            return reference;
        }

        private static void ValidatePersistedScene(
            Scene scene,
            InputActionAsset expectedActionAsset,
            InputAction expectedPauseAction)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                throw new InvalidOperationException(
                    "Persisted QA Pause P1 scene did not reopen correctly.");
            }

            GameObject[] roots = scene.GetRootGameObjects();

            PlayerInput playerInput =
                FindExactlyOne<PlayerInput>(roots);
            PausePlayerInputBinding binding =
                FindExactlyOne<PausePlayerInputBinding>(roots);
            UnityPlayerInputGateAdapter adapter =
                FindExactlyOne<UnityPlayerInputGateAdapter>(roots);
            FindExactlyOne<PauseRequestTrigger>(roots);

            if (!ReferenceEquals(
                    binding.gameObject,
                    playerInput.gameObject) ||
                !ReferenceEquals(
                    adapter.gameObject,
                    playerInput.gameObject))
            {
                throw new InvalidOperationException(
                    "PlayerInput, PausePlayerInputBinding and " +
                    "UnityPlayerInputGateAdapter must be co-located.");
            }

            if (!ReferenceEquals(
                    binding.PlayerInput,
                    playerInput))
            {
                throw new InvalidOperationException(
                    "Persisted PausePlayerInputBinding lost its PlayerInput reference.");
            }

            if (!ReferenceEquals(
                    adapter.PlayerInput,
                    playerInput))
            {
                throw new InvalidOperationException(
                    "Persisted UnityPlayerInputGateAdapter lost its PlayerInput reference.");
            }

            if (playerInput.actions == null)
            {
                throw new InvalidOperationException(
                    "Persisted PlayerInput has no InputActionAsset.");
            }

            string actualActionsPath =
                AssetDatabase.GetAssetPath(playerInput.actions);
            string expectedActionsPath =
                AssetDatabase.GetAssetPath(expectedActionAsset);
            if (!string.Equals(
                    actualActionsPath,
                    expectedActionsPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Persisted PlayerInput references a different InputActionAsset. " +
                    $"expected='{expectedActionsPath}' " +
                    $"actual='{actualActionsPath}'.");
            }

            if (binding.PauseAction == null)
            {
                throw new InvalidOperationException(
                    "Persisted PausePlayerInputBinding has no Pause InputActionReference.");
            }

            string pauseReferencePath =
                AssetDatabase.GetAssetPath(binding.PauseAction);
            if (!string.Equals(
                    pauseReferencePath,
                    QaPauseP1Paths.PauseActionReferencePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Persisted PausePlayerInputBinding references an unexpected " +
                    "InputActionReference asset. " +
                    $"expected='{QaPauseP1Paths.PauseActionReferencePath}' " +
                    $"actual='{pauseReferencePath}'.");
            }

            if (binding.PauseAction.action == null)
            {
                throw new InvalidOperationException(
                    "Persisted Pause InputActionReference does not resolve an action.");
            }

            if (binding.PauseAction.action.id !=
                expectedPauseAction.id)
            {
                throw new InvalidOperationException(
                    "Persisted Pause InputActionReference resolves a different action. " +
                    $"expected='{expectedPauseAction.id}' " +
                    $"actual='{binding.PauseAction.action.id}'.");
            }

            if (!string.Equals(
                    binding.GlobalActionMapName,
                    "Global",
                    StringComparison.Ordinal) ||
                !string.Equals(
                    binding.GameplayActionMapName,
                    "Player",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Persisted PausePlayerInputBinding action-map names are invalid.");
            }

            if (playerInput.actions.FindActionMap(
                    binding.GlobalActionMapName,
                    false) == null ||
                playerInput.actions.FindActionMap(
                    binding.GameplayActionMapName,
                    false) == null)
            {
                throw new InvalidOperationException(
                    "Persisted PlayerInput does not contain all configured action maps.");
            }

            InputAction persistedPauseAction =
                playerInput.actions.FindAction(
                    expectedPauseAction.id.ToString(),
                    false);
            if (persistedPauseAction == null)
            {
                throw new InvalidOperationException(
                    "Persisted PlayerInput cannot resolve Global/PauseToggle by GUID.");
            }

            if (persistedPauseAction.actionMap == null ||
                !string.Equals(
                    persistedPauseAction.actionMap.name,
                    "Global",
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Persisted Pause action does not belong to the Global action map.");
            }
        }

        private static T FindExactlyOne<T>(
            GameObject[] roots)
            where T : Component
        {
            T[] matches = roots
                .Where(root => root != null)
                .SelectMany(
                    root =>
                        root.GetComponentsInChildren<T>(true))
                .ToArray();

            if (matches.Length != 1)
            {
                string rootNames =
                    string.Join(
                        ",",
                        roots
                            .Where(root => root != null)
                            .Select(root => root.name));

                throw new InvalidOperationException(
                    $"Expected exactly one {typeof(T).Name}; " +
                    $"found '{matches.Length}'. " +
                    $"scene='{QaPauseP1Paths.ScenePath}' " +
                    $"roots='{rootNames}'.");
            }

            return matches[0];
        }

        private static void RequireMap(
            InputActionAsset asset,
            string mapName)
        {
            if (asset.FindActionMap(mapName, false) == null)
            {
                throw new InvalidOperationException(
                    $"QA Pause P1 requires action map '{mapName}'.");
            }
        }

        private static void AddSceneToBuildSettings(
            string scenePath)
        {
            EditorBuildSettingsScene[] current =
                EditorBuildSettings.scenes ??
                Array.Empty<EditorBuildSettingsScene>();

            if (current.Any(
                    scene =>
                        string.Equals(
                            scene.path,
                            scenePath,
                            StringComparison.Ordinal)))
            {
                return;
            }

            EditorBuildSettings.scenes = current
                .Concat(
                    new[]
                    {
                        new EditorBuildSettingsScene(
                            scenePath,
                            true)
                    })
                .ToArray();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int separatorIndex =
                folder.LastIndexOf('/');
            if (separatorIndex <= 0)
            {
                throw new InvalidOperationException(
                    $"Invalid QA folder path '{folder}'.");
            }

            string parent =
                folder[..separatorIndex];
            string name =
                folder[(separatorIndex + 1)..];

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetObject(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(
                    serialized.targetObject
                        .GetType()
                        .FullName,
                    propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static void SetString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(
                    serialized.targetObject
                        .GetType()
                        .FullName,
                    propertyName);
            }

            property.stringValue = value;
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

using System;
using System.Linq;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.PauseP1.Editor
{
    internal static class QaPauseP1Paths
    {
        internal const string RootFolder = "Assets/ImmersiveFrameworkQA/Player/PauseP1";
        internal const string SettingsFolder = RootFolder + "/Settings";
        internal const string ScenesFolder = RootFolder + "/Scenes";
        internal const string ScenePath = ScenesFolder + "/QA_PauseProductBinding.unity";
        internal const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        internal const string PauseActionReferencePath = SettingsFolder + "/QA_PauseToggle.inputactionreference.asset";
        internal const string PauseActionPath = "Global/PauseToggle";
    }

    internal static class QaPauseP1Setup
    {
        private const string MenuRoot = "Immersive Framework/QA/Player/Pause P1/";

        [MenuItem(MenuRoot + "Setup or Rebuild Consumer Scene")]
        internal static void SetupOrRebuild()
        {
            EnsureFolder(QaPauseP1Paths.RootFolder);
            EnsureFolder(QaPauseP1Paths.SettingsFolder);
            EnsureFolder(QaPauseP1Paths.ScenesFolder);

            InputActionAsset actionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(QaPauseP1Paths.InputActionsPath);
            if (actionAsset == null)
            {
                throw new InvalidOperationException($"QA Pause P1 requires '{QaPauseP1Paths.InputActionsPath}'.");
            }

            InputAction pauseAction = actionAsset.FindAction(QaPauseP1Paths.PauseActionPath, false);
            if (pauseAction == null)
            {
                throw new InvalidOperationException($"QA Pause P1 requires action '{QaPauseP1Paths.PauseActionPath}'.");
            }

            RequireMap(actionAsset, "Global");
            RequireMap(actionAsset, "Player");
            RequireMap(actionAsset, "UI");

            InputActionReference pauseReference = CreateOrUpdatePauseReference(pauseAction);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sceneRoot = new GameObject("QA Pause P1 Consumer");
            SceneManager.MoveGameObjectToScene(sceneRoot, scene);

            CreatePlayer(sceneRoot.transform, actionAsset, pauseReference);
            CreateControls(sceneRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, QaPauseP1Paths.ScenePath))
            {
                throw new InvalidOperationException($"Unable to save QA Pause P1 scene '{QaPauseP1Paths.ScenePath}'.");
            }

            AddSceneToBuildSettings(QaPauseP1Paths.ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[QA][PAUSE-P1][SETUP] PASS. " +
                $"scene='{QaPauseP1Paths.ScenePath}' " +
                $"action='{QaPauseP1Paths.PauseActionPath}' " +
                "components='PlayerInput,PausePlayerInputBinding,UnityPlayerInputGateAdapter,PauseRequestTrigger'.");
        }

        [MenuItem(MenuRoot + "Open Consumer Scene")]
        internal static void OpenScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(QaPauseP1Paths.ScenePath) == null)
            {
                throw new InvalidOperationException("QA Pause P1 scene does not exist. Run Setup or Rebuild first.");
            }

            EditorSceneManager.OpenScene(QaPauseP1Paths.ScenePath, OpenSceneMode.Single);
        }

        private static void CreatePlayer(Transform parent, InputActionAsset actionAsset, InputActionReference pauseReference)
        {
            var playerObject = new GameObject("QA Pause P1 PlayerInput");
            playerObject.transform.SetParent(parent, false);
            playerObject.SetActive(false);

            PlayerInput playerInput = playerObject.AddComponent<PlayerInput>();
            playerInput.actions = actionAsset;
            playerInput.defaultActionMap = "Player";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            UnityPlayerInputGateAdapter adapter = playerObject.AddComponent<UnityPlayerInputGateAdapter>();
            ConfigureAdapter(adapter, playerInput);

            PausePlayerInputBinding binding = playerObject.AddComponent<PausePlayerInputBinding>();
            ConfigureBinding(binding, playerInput, pauseReference);

            playerObject.SetActive(true);
        }

        private static void CreateControls(Transform parent)
        {
            var controls = new GameObject("QA Pause P1 UIGlobal Controls");
            controls.transform.SetParent(parent, false);

            PauseRequestTrigger trigger = controls.AddComponent<PauseRequestTrigger>();
            var serialized = new SerializedObject(trigger);
            SerializedProperty reason = serialized.FindProperty("reason");
            if (reason != null)
            {
                reason.stringValue = "qa.pause.p1.ui";
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);
        }

        private static void ConfigureAdapter(UnityPlayerInputGateAdapter adapter, PlayerInput playerInput)
        {
            var serialized = new SerializedObject(adapter);
            SetObject(serialized, "playerInput", playerInput);
            SetString(serialized, "gameplayActionMapName", "Player");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(adapter);
        }

        private static void ConfigureBinding(PausePlayerInputBinding binding, PlayerInput playerInput, InputActionReference pauseReference)
        {
            var serialized = new SerializedObject(binding);
            SetObject(serialized, "playerInput", playerInput);
            SetObject(serialized, "pauseAction", pauseReference);
            SetString(serialized, "globalActionMapName", "Global");
            SetString(serialized, "gameplayActionMapName", "Player");
            SetString(serialized, "uiActionMapName", "UI");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binding);
        }

        private static InputActionReference CreateOrUpdatePauseReference(InputAction pauseAction)
        {
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(QaPauseP1Paths.PauseActionReferencePath);

            if (reference == null)
            {
                reference = InputActionReference.Create(pauseAction);
                reference.name = "QA Pause Toggle";
                AssetDatabase.CreateAsset(reference, QaPauseP1Paths.PauseActionReferencePath);
            }
            else
            {
                reference.Set(pauseAction);
                EditorUtility.SetDirty(reference);
            }

            return reference;
        }

        private static void RequireMap(InputActionAsset asset, string mapName)
        {
            if (asset.FindActionMap(mapName, false) == null)
            {
                throw new InvalidOperationException($"QA Pause P1 requires action map '{mapName}'.");
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            if (current.Any(scene => string.Equals(scene.path, scenePath, StringComparison.Ordinal)))
            {
                return;
            }

            EditorBuildSettings.scenes = current
                .Concat(new[] { new EditorBuildSettingsScene(scenePath, true) })
                .ToArray();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = folder[..folder.LastIndexOf('/')];
            string name = folder[(folder.LastIndexOf('/') + 1)..];
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(serialized.targetObject.GetType().FullName, propertyName);
            }

            property.objectReferenceValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new MissingFieldException(serialized.targetObject.GetType().FullName, propertyName);
            }

            property.stringValue = value;
        }
    }
}

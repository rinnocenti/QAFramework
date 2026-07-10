using System;
using System.Linq;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotently installs the P2E Gate runtime fixture into the P2D canonical QA scene.
    /// It does not create another Route, Activity, PlayerInput, Gate adapter or runtime authority.
    /// </summary>
    public static class QaP2EPlayerInputGateRuntimeInstaller
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P2E Install PlayerInput Gate Runtime Proof";

        private const string ScenePath =
            "Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerRuntimeBaseline.unity";

        private const string FixtureObjectName =
            "QA_P2E_PlayerInputGateRuntime";

        [MenuItem(MenuPath)]
        public static void Install()
        {
            try
            {
                SceneAsset sceneAsset =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        ScenePath);
                if (sceneAsset == null)
                {
                    throw new InvalidOperationException(
                        $"P2D scene is missing at '{ScenePath}'. Run the P2D installer first.");
                }

                Scene scene =
                    EditorSceneManager.OpenScene(
                        ScenePath,
                        OpenSceneMode.Single);

                PlayerInput playerInput =
                    FindSingleInScene<PlayerInput>(
                        scene,
                        nameof(PlayerInput));

                UnityPlayerInputGateAdapter gateAdapter =
                    FindSingleInScene<UnityPlayerInputGateAdapter>(
                        scene,
                        nameof(UnityPlayerInputGateAdapter));

                GameObject fixtureObject =
                    FindRoot(
                        scene,
                        FixtureObjectName);

                if (fixtureObject == null)
                {
                    fixtureObject =
                        new GameObject(
                            FixtureObjectName);
                    SceneManager.MoveGameObjectToScene(
                        fixtureObject,
                        scene);
                }

                PauseRequestTrigger pauseTrigger =
                    fixtureObject.GetComponent<PauseRequestTrigger>();
                if (pauseTrigger == null)
                {
                    pauseTrigger =
                        fixtureObject.AddComponent<PauseRequestTrigger>();
                }

                QaP2EPlayerInputGateRuntimeFixture fixture =
                    fixtureObject.GetComponent<QaP2EPlayerInputGateRuntimeFixture>();
                if (fixture == null)
                {
                    fixture =
                        fixtureObject.AddComponent<QaP2EPlayerInputGateRuntimeFixture>();
                }

                ConfigurePauseTrigger(
                    pauseTrigger);
                ConfigureFixture(
                    fixture,
                    playerInput,
                    gateAdapter,
                    pauseTrigger);

                EditorUtility.SetDirty(
                    fixtureObject);
                EditorUtility.SetDirty(
                    pauseTrigger);
                EditorUtility.SetDirty(
                    fixture);

                EditorSceneManager.MarkSceneDirty(
                    scene);

                if (!EditorSceneManager.SaveScene(
                    scene,
                    ScenePath))
                {
                    throw new InvalidOperationException(
                        $"Could not save P2E fixture into '{ScenePath}'.");
                }

                ValidateSavedScene();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P2E_PLAYER_INPUT_GATE_RUNTIME_SETUP] status='Succeeded' " +
                    $"scene='{ScenePath}' fixture='{FixtureObjectName}' " +
                    "message='Enter Play Mode through QA Hub and select Player Runtime Baseline.'");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P2E_PLAYER_INPUT_GATE_RUNTIME_SETUP] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static T FindSingleInScene<T>(
            Scene scene,
            string diagnosticName)
            where T : Component
        {
            T[] matches =
                UnityEngine.Object.FindObjectsByType<T>(
                    FindObjectsInactive.Include)
                .Where(component =>
                    component != null
                    && component.gameObject.scene == scene)
                .ToArray();

            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one {diagnosticName} in '{scene.name}', found '{matches.Length}'.");
            }

            return matches[0];
        }

        private static GameObject FindRoot(
            Scene scene,
            string objectName)
        {
            GameObject[] roots =
                scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root =
                    roots[i];
                if (root != null
                    && string.Equals(
                        root.name,
                        objectName,
                        StringComparison.Ordinal))
                {
                    return root;
                }
            }

            return null;
        }

        private static void ConfigurePauseTrigger(
            PauseRequestTrigger trigger)
        {
            var serialized =
                new SerializedObject(trigger);
            serialized.Update();

            SerializedProperty reason =
                serialized.FindProperty("reason");
            if (reason == null
                || reason.propertyType
                    != SerializedPropertyType.String)
            {
                throw new InvalidOperationException(
                    "PauseRequestTrigger reason field was not found.");
            }

            reason.stringValue =
                "qa.p2e.player-input-gate-runtime";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFixture(
            QaP2EPlayerInputGateRuntimeFixture fixture,
            PlayerInput playerInput,
            UnityPlayerInputGateAdapter gateAdapter,
            PauseRequestTrigger pauseTrigger)
        {
            var serialized =
                new SerializedObject(fixture);
            serialized.Update();

            SetObject(
                serialized,
                "playerInput",
                playerInput);
            SetObject(
                serialized,
                "gateAdapter",
                gateAdapter);
            SetObject(
                serialized,
                "pauseRequestTrigger",
                pauseTrigger);
            SetString(
                serialized,
                "gameplayActionMapName",
                "Player");
            SetBool(
                serialized,
                "runOnStart",
                true);
            SetInt(
                serialized,
                "timeoutFrames",
                120);
            SetBool(
                serialized,
                "throwOnFailure",
                false);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateSavedScene()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            PlayerInput playerInput =
                FindSingleInScene<PlayerInput>(
                    scene,
                    nameof(PlayerInput));

            UnityPlayerInputGateAdapter gateAdapter =
                FindSingleInScene<UnityPlayerInputGateAdapter>(
                    scene,
                    nameof(UnityPlayerInputGateAdapter));

            QaP2EPlayerInputGateRuntimeFixture fixture =
                FindSingleInScene<QaP2EPlayerInputGateRuntimeFixture>(
                    scene,
                    nameof(QaP2EPlayerInputGateRuntimeFixture));

            PauseRequestTrigger pauseTrigger =
                fixture.GetComponent<PauseRequestTrigger>();

            if (pauseTrigger == null)
            {
                throw new InvalidOperationException(
                    "Saved P2E fixture has no PauseRequestTrigger.");
            }

            var serialized =
                new SerializedObject(fixture);
            serialized.Update();

            AssertReference(
                serialized,
                "playerInput",
                playerInput);
            AssertReference(
                serialized,
                "gateAdapter",
                gateAdapter);
            AssertReference(
                serialized,
                "pauseRequestTrigger",
                pauseTrigger);
        }

        private static void AssertReference(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object expected)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (property == null
                || property.propertyType
                    != SerializedPropertyType.ObjectReference
                || property.objectReferenceValue != expected)
            {
                throw new InvalidOperationException(
                    $"Saved fixture reference '{propertyName}' is missing or incorrect.");
            }
        }

        private static void SetObject(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);
            if (property == null
                || property.propertyType
                    != SerializedPropertyType.ObjectReference)
            {
                throw new InvalidOperationException(
                    $"Fixture object reference '{propertyName}' was not found.");
            }

            property.objectReferenceValue =
                value;
        }

        private static void SetString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);
            if (property == null
                || property.propertyType
                    != SerializedPropertyType.String)
            {
                throw new InvalidOperationException(
                    $"Fixture string field '{propertyName}' was not found.");
            }

            property.stringValue =
                value ?? string.Empty;
        }

        private static void SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);
            if (property == null
                || property.propertyType
                    != SerializedPropertyType.Boolean)
            {
                throw new InvalidOperationException(
                    $"Fixture bool field '{propertyName}' was not found.");
            }

            property.boolValue =
                value;
        }

        private static void SetInt(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);
            if (property == null
                || property.propertyType
                    != SerializedPropertyType.Integer)
            {
                throw new InvalidOperationException(
                    $"Fixture int field '{propertyName}' was not found.");
            }

            property.intValue =
                value;
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("'", "\\'");
        }
    }
}

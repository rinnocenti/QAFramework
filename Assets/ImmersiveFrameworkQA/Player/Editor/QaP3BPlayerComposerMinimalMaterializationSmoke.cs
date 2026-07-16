using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.Editor.PlayerAuthoring;
using Immersive.Framework.PlayerAuthoring;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// P3B editor-only regression smoke for the simplified PlayerComposer product surface.
    /// Creates temporary in-memory objects only and destroys all created content after execution.
    /// </summary>
    internal static class QaP3BPlayerComposerMinimalMaterializationSmoke
    {
        private const string PlayerMapName = "Player";

        private static readonly string[] LegacyTypeNames =
        {
            "PlayerControlBindingTargetBehaviour",
            "UnityPlayerInputBridgeTargetBehaviour",
            "UnityPlayerInputActivationTargetBehaviour",
            "PlayerSlotOccupancy",
            "PlayerEntryBehaviour",
            "PlayerViewBehaviour",
            "PlayerControlBehaviour"
        };

        internal static void Run()
        {
            var completedCases = new List<string>();

            try
            {
                RunNominalCameraEnabled(completedCases);
                RunCameraDisabled(completedCases);
                RunInvalidAuthoredMap(completedCases);
                RunMissingRequiredCameraTargets(completedCases);
                RunLookAtFollowPolicy(completedCases);

                Debug.Log(
                    "[P3B_PLAYER_COMPOSER_MINIMAL_SMOKE] status='Passed' " +
                    $"cases='{completedCases.Count}' " +
                    $"completed='{string.Join(",", completedCases)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3B_PLAYER_COMPOSER_MINIMAL_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completedCases)}'.");
                throw;
            }
        }

        private static void RunNominalCameraEnabled(List<string> completed)
        {
            using var fixture = PlayerFixture.Create("QA_P3B_Nominal");

            ConfigureComposer(
                fixture.Composer,
                cameraBindingRequired: true,
                createAnchorsIfMissing: true,
                lookAtPolicy: PlayerComposerLookAtPolicy.ExplicitTarget,
                authoredMap: PlayerMapName);

            PreAuthoredPlayerComposerApplyRebuildResult first =
                PreAuthoredPlayerComposerApplyRebuildUtility.ApplyOrRebuild(
                    fixture.Composer,
                    logDiagnostics: false,
                    useUndo: false);

            AssertTrue(first.Succeeded, $"Nominal first Apply failed: {first.Issue}");
            AssertTrue(first.BlockedCount == 0, "Nominal first Apply reported blocking issues.");
            AssertNotNull(fixture.Composer.CameraTarget, "Nominal CameraTarget was not generated.");
            AssertNotNull(fixture.Composer.LookAtTarget, "Nominal LookAtTarget was not generated.");
            AssertPath(
                fixture.Composer.transform,
                fixture.Composer.CameraTarget,
                "Anchors/CameraTarget");
            AssertPath(
                fixture.Composer.transform,
                fixture.Composer.LookAtTarget,
                "Anchors/LookAtTarget");

            AssertCanonicalMaterialization(fixture);
            AssertNoLegacyMaterialization(fixture.Composer.gameObject);
            AssertGateWiring(fixture);

            PreAuthoredPlayerComposerApplyRebuildResult second =
                PreAuthoredPlayerComposerApplyRebuildUtility.ApplyOrRebuild(
                    fixture.Composer,
                    logDiagnostics: false,
                    useUndo: false);

            AssertTrue(second.Succeeded, $"Nominal second Apply failed: {second.Issue}");
            AssertTrue(second.CreatedCount == 0, $"Idempotency failed: created='{second.CreatedCount}'.");
            AssertTrue(second.RepairedCount == 0, $"Idempotency failed: repaired='{second.RepairedCount}'.");
            AssertTrue(second.BlockedCount == 0, $"Idempotency failed: blocked='{second.BlockedCount}'.");

            PreAuthoredPlayerComposerApplyRebuildResult validation =
                PreAuthoredPlayerComposerApplyRebuildUtility.Validate(
                    fixture.Composer,
                    logDiagnostics: false);

            AssertTrue(validation.Succeeded, $"Nominal validation failed: {validation.Issue}");
            completed.Add("nominal-camera-enabled-idempotent");
        }

        private static void RunCameraDisabled(List<string> completed)
        {
            using var fixture = PlayerFixture.Create("QA_P3B_CameraDisabled");

            ConfigureComposer(
                fixture.Composer,
                cameraBindingRequired: false,
                createAnchorsIfMissing: true,
                lookAtPolicy: PlayerComposerLookAtPolicy.ExplicitTarget,
                authoredMap: PlayerMapName);

            PreAuthoredPlayerComposerApplyRebuildResult result =
                PreAuthoredPlayerComposerApplyRebuildUtility.ApplyOrRebuild(
                    fixture.Composer,
                    logDiagnostics: false,
                    useUndo: false);

            AssertTrue(result.Succeeded, $"Camera-disabled Apply failed: {result.Issue}");
            AssertNull(
                FindDirectChild(fixture.Composer.transform, "Anchors"),
                "Camera-disabled Player unexpectedly created an Anchors container.");
            AssertNull(fixture.Composer.CameraTarget, "Camera-disabled Player unexpectedly assigned CameraTarget.");
            AssertNull(fixture.Composer.LookAtTarget, "Camera-disabled Player unexpectedly assigned LookAtTarget.");
            AssertCanonicalMaterialization(fixture);
            AssertNoLegacyMaterialization(fixture.Composer.gameObject);

            completed.Add("camera-disabled-no-anchors");
        }

        private static void RunInvalidAuthoredMap(List<string> completed)
        {
            using var fixture = PlayerFixture.Create("QA_P3B_InvalidMap");

            ConfigureComposer(
                fixture.Composer,
                cameraBindingRequired: false,
                createAnchorsIfMissing: true,
                lookAtPolicy: PlayerComposerLookAtPolicy.ExplicitTarget,
                authoredMap: "MissingMap");

            PreAuthoredPlayerComposerApplyRebuildResult validation =
                PreAuthoredPlayerComposerApplyRebuildUtility.Validate(
                    fixture.Composer,
                    logDiagnostics: false);

            AssertTrue(validation.Failed, "Invalid authored map unexpectedly passed validation.");
            AssertContains(
                validation.Issue,
                "MissingMap",
                "Invalid authored map failure did not identify the missing map.");

            AssertNull(
                fixture.Root.GetComponent<PlayerActorDeclaration>(),
                "Validation failure created PlayerActorDeclaration.");
            completed.Add("invalid-authored-map");
        }

        private static void RunMissingRequiredCameraTargets(List<string> completed)
        {
            using var fixture = PlayerFixture.Create("QA_P3B_MissingCameraTargets");

            ConfigureComposer(
                fixture.Composer,
                cameraBindingRequired: true,
                createAnchorsIfMissing: false,
                lookAtPolicy: PlayerComposerLookAtPolicy.ExplicitTarget,
                authoredMap: PlayerMapName);

            PreAuthoredPlayerComposerApplyRebuildResult result =
                PreAuthoredPlayerComposerApplyRebuildUtility.Validate(
                    fixture.Composer,
                    logDiagnostics: false);

            AssertTrue(result.Failed, "Missing required camera targets unexpectedly passed validation.");
            AssertContains(
                result.Issue,
                "Camera Target",
                "Missing camera-target failure was not explicit.");

            completed.Add("missing-camera-targets-explicit-failure");
        }

        private static void RunLookAtFollowPolicy(List<string> completed)
        {
            using var fixture = PlayerFixture.Create("QA_P3B_LookAtFollow");

            ConfigureComposer(
                fixture.Composer,
                cameraBindingRequired: true,
                createAnchorsIfMissing: true,
                lookAtPolicy: PlayerComposerLookAtPolicy.UseFollowTarget,
                authoredMap: PlayerMapName);

            PreAuthoredPlayerComposerApplyRebuildResult result =
                PreAuthoredPlayerComposerApplyRebuildUtility.ApplyOrRebuild(
                    fixture.Composer,
                    logDiagnostics: false,
                    useUndo: false);

            AssertTrue(result.Succeeded, $"UseFollowTarget Apply failed: {result.Issue}");
            AssertNotNull(fixture.Composer.CameraTarget, "UseFollowTarget did not create CameraTarget.");
            AssertSame(
                fixture.Composer.CameraTarget,
                fixture.Composer.LookAtTarget,
                "UseFollowTarget did not resolve LookAtTarget to CameraTarget.");
            AssertNull(
                FindDirectChild(
                    FindDirectChild(fixture.Composer.transform, "Anchors"),
                    "LookAtTarget"),
                "UseFollowTarget unexpectedly created a separate LookAtTarget object.");

            completed.Add("look-at-use-follow-target");
        }

        private static void AssertCanonicalMaterialization(PlayerFixture fixture)
        {
            PlayerActorDeclaration actor =
                fixture.Root.GetComponent<PlayerActorDeclaration>();
            AssertNotNull(actor, "PlayerActorDeclaration is missing from Player root.");
            AssertEqual(
                fixture.Composer.ActorId,
                ReadString(actor, "actorId"),
                "PlayerActorDeclaration actorId differs from Composer.");
            AssertSame(
                fixture.PlayerInput,
                ReadObject(actor, "playerInput"),
                "PlayerActorDeclaration PlayerInput differs from Composer.");
        }

        private static void AssertGateWiring(PlayerFixture fixture)
        {
            Type gateType = ResolveMonoBehaviourType("UnityPlayerInputGateAdapter");
            AssertNotNull(gateType, "UnityPlayerInputGateAdapter type is unavailable.");

            Component gate = fixture.Root.GetComponent(gateType);
            AssertNotNull(gate, "UnityPlayerInputGateAdapter was not materialized.");

            AssertSame(
                fixture.PlayerInput,
                ReadObject(gate, "playerInput"),
                "Gate PlayerInput is not the Composer PlayerInput.");
            string map =
                ReadFirstString(
                    gate,
                    "gameplayActionMapName",
                    "actionMapName",
                    "gameplayActionMap");

            AssertEqual(
                PlayerMapName,
                map,
                "Gate map was not derived from the authored default action map.");
            AssertEqual(
                PlayerMapName,
                fixture.PlayerInput.defaultActionMap,
                "Apply/Rebuild did not write the authored map into PlayerInput.defaultActionMap.");
        }

        private static void ConfigureComposer(
            PreAuthoredPlayerComposer composer,
            bool cameraBindingRequired,
            bool createAnchorsIfMissing,
            PlayerComposerLookAtPolicy lookAtPolicy,
            string authoredMap)
        {
            var serialized = new SerializedObject(composer);
            serialized.Update();

            SetString(serialized, "actorId", "qa.player.actor");
            SetBool(serialized, "controlEnabled", true);
            SetObject(serialized, "playerInput", composer.GetComponent<PlayerInput>());
            SetString(serialized, "gameplayActionMap", authoredMap);
            SetBool(serialized, "inputBindingRequired", true);
            SetBool(serialized, "gateParticipation", true);
            SetBool(serialized, "cameraBindingRequired", cameraBindingRequired);
            SetObject(serialized, "cameraTarget", null);
            SetObject(serialized, "lookAtTarget", null);
            SetEnum(serialized, "lookAtPolicy", (int)lookAtPolicy);
            SetBool(serialized, "resetEnabled", false);
            SetBool(serialized, "createBindingsRootIfMissing", true);
            SetBool(serialized, "createAnchorsIfMissing", createAnchorsIfMissing);
            SetBool(serialized, "logApplyRebuildDiagnostics", false);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertNoLegacyMaterialization(GameObject root)
        {
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null)
                {
                    continue;
                }

                string name = component.GetType().Name;
                if (LegacyTypeNames.Contains(name, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Legacy component '{name}' remains at '{GetPath(root.transform, component.transform)}'.");
                }
            }
        }

        private static Type ResolveMonoBehaviourType(string simpleName)
        {
            return TypeCache
                .GetTypesDerivedFrom<MonoBehaviour>()
                .FirstOrDefault(type =>
                    string.Equals(
                        type.Name,
                        simpleName,
                        StringComparison.Ordinal));
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, name, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static string ReadString(Component component, string propertyName)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.String)
            {
                throw new InvalidOperationException(
                    $"String property '{propertyName}' was not found on '{component.GetType().Name}'.");
            }

            return property.stringValue;
        }

        private static string ReadFirstString(Component component, params string[] propertyNames)
        {
            var serialized = new SerializedObject(component);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serialized.FindProperty(propertyName);
                if (property != null && property.propertyType == SerializedPropertyType.String)
                {
                    return property.stringValue;
                }
            }

            throw new InvalidOperationException(
                $"None of the expected string properties were found on '{component.GetType().Name}': " +
                string.Join(", ", propertyNames));
        }

        private static UnityEngine.Object ReadObject(Component component, string propertyName)
        {
            var serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                throw new InvalidOperationException(
                    $"Object property '{propertyName}' was not found on '{component.GetType().Name}'.");
            }

            return property.objectReferenceValue;
        }

        private static void SetString(SerializedObject serialized, string name, string value)
        {
            SerializedProperty property = RequireProperty(
                serialized,
                name,
                SerializedPropertyType.String);
            property.stringValue = value ?? string.Empty;
        }

        private static void SetBool(SerializedObject serialized, string name, bool value)
        {
            SerializedProperty property = RequireProperty(
                serialized,
                name,
                SerializedPropertyType.Boolean);
            property.boolValue = value;
        }

        private static void SetObject(
            SerializedObject serialized,
            string name,
            UnityEngine.Object value)
        {
            SerializedProperty property = RequireProperty(
                serialized,
                name,
                SerializedPropertyType.ObjectReference);
            property.objectReferenceValue = value;
        }

        private static void SetEnum(SerializedObject serialized, string name, int value)
        {
            SerializedProperty property = RequireProperty(
                serialized,
                name,
                SerializedPropertyType.Enum);
            property.enumValueIndex = value;
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name,
            SerializedPropertyType type)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || property.propertyType != type)
            {
                throw new InvalidOperationException(
                    $"Expected serialized property '{name}' of type '{type}' on '{serialized.targetObject.GetType().Name}'.");
            }

            return property;
        }

        private static void AssertPath(
            Transform root,
            Transform target,
            string expectedRelativePath)
        {
            string actual = GetPath(root, target);
            AssertEqual(
                root.name + "/" + expectedRelativePath,
                actual,
                $"Unexpected generated anchor path for '{target.name}'.");
        }

        private static string GetPath(Transform root, Transform target)
        {
            var segments = new List<string>();
            Transform current = target;
            while (current != null)
            {
                segments.Add(current.name);
                if (current == root)
                {
                    break;
                }

                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNull(object value, string message)
        {
            if (value != null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static void AssertContains(string value, string expected, string message)
        {
            if (string.IsNullOrEmpty(value)
                || value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException(
                    $"{message} expectedFragment='{expected}' actual='{value ?? string.Empty}'.");
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'");
        }

        private sealed class PlayerFixture : IDisposable
        {
            private PlayerFixture(
                GameObject root,
                PreAuthoredPlayerComposer composer,
                PlayerInput playerInput,
                InputActionAsset actionAsset)
            {
                Root = root;
                Composer = composer;
                PlayerInput = playerInput;
                ActionAsset = actionAsset;
            }

            public GameObject Root { get; }
            public PreAuthoredPlayerComposer Composer { get; }
            public PlayerInput PlayerInput { get; }
            public InputActionAsset ActionAsset { get; }

            public static PlayerFixture Create(string name)
            {
                var root = new GameObject(name);
                PreAuthoredPlayerComposer composer = root.AddComponent<PreAuthoredPlayerComposer>();
                PlayerInput playerInput = root.GetComponent<PlayerInput>();

                var actionAsset = ScriptableObject.CreateInstance<InputActionAsset>();
                InputActionMap playerMap = actionAsset.AddActionMap(PlayerMapName);
                InputAction moveAction =
                    playerMap.AddAction(
                        "Move",
                        InputActionType.Value);
                moveAction.expectedControlType =
                    "Vector2";

                playerInput.actions = actionAsset;
                playerInput.defaultActionMap = string.Empty;

                var serialized = new SerializedObject(composer);
                serialized.Update();
                SetObject(serialized, "playerInput", playerInput);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                return new PlayerFixture(root, composer, playerInput, actionAsset);
            }

            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                }

                if (ActionAsset != null)
                {
                    UnityEngine.Object.DestroyImmediate(ActionAsset);
                }
            }
        }
    }
}

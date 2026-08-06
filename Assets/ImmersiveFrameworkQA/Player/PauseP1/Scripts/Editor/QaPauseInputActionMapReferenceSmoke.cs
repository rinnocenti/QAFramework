using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.PauseP1.Editor
{
    internal static class QaPauseInputActionMapReferenceSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run Pause Input Authoring UX Smoke";

        private const string LogPrefix =
            "[QA][Pause Input Authoring UX]";

        [MenuItem(MenuPath)]
        private static void Run()
        {
            var completed =
                new List<string>();

            InputActionAsset sourceAsset = null;
            InputActionAsset renamedAsset = null;
            InputActionAsset sameNameForeignAsset = null;
            GameObject host = null;
            InputActionReference pauseReference = null;

            try
            {
                sourceAsset =
                    ScriptableObject.CreateInstance<
                        InputActionAsset>();

                InputActionMap global =
                    sourceAsset.AddActionMap(
                        "Global");

                InputAction pauseAction =
                    global.AddAction(
                        "PauseToggle",
                        InputActionType.Button);

                InputActionMap gameplay =
                    sourceAsset.AddActionMap(
                        "Gameplay");

                gameplay.AddAction(
                    "Move",
                    InputActionType.Value);

                var reference =
                    PlayerInputActionMapReference.From(
                        gameplay);

                Require(
                    reference.IsConfigured,
                    "Typed Gameplay Action Map reference was not configured.");

                completed.Add(
                    "typed-reference-created");

                Require(
                    reference.TryResolve(
                        sourceAsset,
                        out InputActionMap resolved,
                        out string resolveDiagnostic) &&
                    resolved.id == gameplay.id,
                    "Typed Gameplay Action Map did not resolve by GUID. " +
                    resolveDiagnostic);

                completed.Add(
                    "guid-resolution-valid");

                string sourceJson =
                    sourceAsset.ToJson();

                const string gameplayNamePattern =
                    "\"name\"\\s*:\\s*\"Gameplay\"";

                const string renamedGameplayName =
                    "\"name\":\"GameplayRenamed\"";

                string renamedJson =
                    Regex.Replace(
                        sourceJson,
                        gameplayNamePattern,
                        renamedGameplayName,
                        RegexOptions.CultureInvariant);

                Require(
                    !string.Equals(
                        sourceJson,
                        renamedJson,
                        StringComparison.Ordinal),
                    "QA could not produce a renamed Action Map JSON clone.");

                renamedAsset =
                    InputActionAsset.FromJson(
                        renamedJson);

                Require(
                    reference.TryResolve(
                        renamedAsset,
                        out InputActionMap renamed,
                        out string renameDiagnostic) &&
                    renamed.id == gameplay.id &&
                    string.Equals(
                        renamed.name,
                        "GameplayRenamed",
                        StringComparison.Ordinal),
                    "Gameplay Action Map reference was not stable across rename. " +
                    renameDiagnostic);

                completed.Add(
                    "map-rename-stable");

                sameNameForeignAsset =
                    ScriptableObject.CreateInstance<
                        InputActionAsset>();

                sameNameForeignAsset.AddActionMap(
                    "Gameplay");

                Require(
                    !reference.TryResolve(
                        sameNameForeignAsset,
                        out _,
                        out string foreignDiagnostic) &&
                    foreignDiagnostic.Contains(
                        "Name fallback is not used",
                        StringComparison.Ordinal),
                    "Foreign Action Asset with the same map name was not rejected explicitly.");

                completed.Add(
                    "foreign-same-name-rejected");

                Require(
                    !default(PlayerInputActionMapReference).TryResolve(
                        sourceAsset,
                        out _,
                        out _),
                    "Missing typed Action Map reference was not rejected.");

                completed.Add(
                    "missing-reference-rejected");

                host =
                    new GameObject(
                        "QA Pause Input Authoring UX Host");

                PlayerInput input =
                    host.AddComponent<
                        PlayerInput>();

                input.actions =
                    sourceAsset;

                UnityPlayerInputGateAdapter adapter =
                    host.AddComponent<
                        UnityPlayerInputGateAdapter>();

                PausePlayerInputBinding binding =
                    host.AddComponent<
                        PausePlayerInputBinding>();

                pauseReference =
                    InputActionReference.Create(
                        pauseAction);

                SetObject(
                    adapter,
                    "playerInput",
                    input);

                SetMapReference(
                    adapter,
                    "gameplayActionMap",
                    gameplay);

                SetObject(
                    binding,
                    "playerInput",
                    input);

                SetObject(
                    binding,
                    "pauseAction",
                    pauseReference);

                SetMapReference(
                    binding,
                    "gameplayActionMap",
                    gameplay);

                Require(
                    adapter.TryValidateAuthoring(
                        out string adapterDiagnostic),
                    "Gate Adapter typed authoring failed. " +
                    adapterDiagnostic);

                Require(
                    binding.TryValidateAuthoring(
                        out string bindingDiagnostic),
                    "Pause binding typed authoring failed. " +
                    bindingDiagnostic);

                completed.Add(
                    "binding-and-adapter-aligned");

                InputActionMap differentGameplay =
                    sourceAsset.AddActionMap(
                        "DifferentGameplay");

                SetMapReference(
                    adapter,
                    "gameplayActionMap",
                    differentGameplay);

                Require(
                    !binding.TryValidateAuthoring(
                        out string mismatchDiagnostic) &&
                    mismatchDiagnostic.Contains(
                        "same Gameplay Action Map GUID",
                        StringComparison.Ordinal),
                    "Pause binding did not reject a mismatched Gate Adapter map.");

                completed.Add(
                    "mismatched-adapter-map-rejected");

                SetMapReference(
                    adapter,
                    "gameplayActionMap",
                    gameplay);

                Require(
                    binding.TryValidateAuthoring(
                        out string restoredDiagnostic),
                    "Aligned authoring was not restored after the negative case. " +
                    restoredDiagnostic);

                completed.Add(
                    "aligned-authoring-restored");

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");

                throw;
            }
            finally
            {
                if (pauseReference != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        pauseReference);
                }

                if (host != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        host);
                }

                if (sameNameForeignAsset != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        sameNameForeignAsset);
                }

                if (renamedAsset != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        renamedAsset);
                }

                if (sourceAsset != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        sourceAsset);
                }
            }
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized =
                new SerializedObject(
                    target);

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing object property '{target.GetType().Name}.{propertyName}'.");

            property.objectReferenceValue =
                value;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetMapReference(
            UnityEngine.Object target,
            string propertyName,
            InputActionMap actionMap)
        {
            var serialized =
                new SerializedObject(
                    target);

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing map-reference property '{target.GetType().Name}.{propertyName}'.");

            SerializedProperty actionAsset =
                property.FindPropertyRelative(
                    "actionAsset");

            SerializedProperty actionMapId =
                property.FindPropertyRelative(
                    "actionMapId");

            SerializedProperty cachedActionMapName =
                property.FindPropertyRelative(
                    "cachedActionMapName");

            Require(
                actionAsset != null &&
                actionMapId != null &&
                cachedActionMapName != null,
                $"Typed map-reference fields are missing for '{target.GetType().Name}.{propertyName}'.");

            actionAsset.objectReferenceValue =
                actionMap.asset;

            actionMapId.stringValue =
                actionMap.id.ToString("D");

            cachedActionMapName.stringValue =
                actionMap.name;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}

using System;
using System.Collections.Generic;
using Immersive.Framework.GlobalUi;
using Immersive.Framework.InputMode;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    public static class QaH21PauseRuntimeCompositionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Input Mode/H2.1 Run Pause Runtime Composition Smoke";
        private const string LogPrefix =
            "[H21_PAUSE_RUNTIME_COMPOSITION_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();

            try
            {
                GameObject emptyRoot = CreateRoot("H21 Empty Root", objects);
                GlobalUiPauseRuntimeBindingResult absent =
                    GlobalUiSceneRuntime.TryBindPauseInputModeRuntime(
                        new[] { emptyRoot },
                        new QaFakePauseRuntimePort());
                Require(
                    absent.Succeeded &&
                    !absent.IntegrationConfigured &&
                    absent.Status == "OptionalIntegrationAbsent" &&
                    absent.Bridge == null,
                    "Zero registration composition did not remain explicitly optional.");
                completed.Add("zero-registrations-optional");

                GameObject validRoot = CreateRoot("H21 Valid Root", objects);
                PauseInputModeUnityPlayerInputRuntimeBridge validBridge =
                    CreateBridge(validRoot, objects);
                PauseInputModeRuntimeBridgeRegistration validRegistration =
                    validRoot.AddComponent<PauseInputModeRuntimeBridgeRegistration>();
                Require(validRegistration.TryConfigureBridge(validBridge, out string validIssue),
                    "Could not configure the valid explicit bridge registration. " + validIssue);
                var fake = new QaFakePauseRuntimePort();
                GlobalUiPauseRuntimeBindingResult valid =
                    GlobalUiSceneRuntime.TryBindPauseInputModeRuntime(
                        new[] { validRoot }, fake);
                Require(valid.Succeeded && valid.IntegrationConfigured &&
                    valid.Status == "Bound" && valid.Bridge == validBridge &&
                    validBridge.HasPauseRuntimeBinding,
                    "Valid Pause runtime composition was not bound explicitly. " + valid.Message);
                validBridge.SubmitForDiagnostics(
                    PauseRequestKind.Pause,
                    nameof(QaH21PauseRuntimeCompositionSmoke),
                    "valid-registration");
                Require(fake.TryGetPauseSnapshotCount == 1,
                    "Bound composition bridge did not consult the configured Pause runtime port.");
                completed.Add("one-valid-registration-bound");

                GameObject missingRoot = CreateRoot("H21 Missing Bridge Root", objects);
                missingRoot.AddComponent<PauseInputModeRuntimeBridgeRegistration>();
                GlobalUiPauseRuntimeBindingResult missing =
                    GlobalUiSceneRuntime.TryBindPauseInputModeRuntime(
                        new[] { missingRoot }, new QaFakePauseRuntimePort());
                Require(!missing.Succeeded && missing.Status == "RejectedMissingBridge" &&
                    missing.Message.Contains("has no bridge reference"),
                    "Null bridge registration did not fail explicitly. " + missing.Message);
                completed.Add("null-bridge-registration-rejected");

                GameObject foreignRegistrationRoot = CreateRoot("H21 Foreign Registration Root", objects);
                GameObject foreignBridgeRoot = CreateRoot("H21 Foreign Bridge Root", objects);
                PauseInputModeUnityPlayerInputRuntimeBridge foreignBridge =
                    CreateBridge(foreignBridgeRoot, objects);
                PauseInputModeRuntimeBridgeRegistration foreignRegistration =
                    foreignRegistrationRoot.AddComponent<PauseInputModeRuntimeBridgeRegistration>();
                Require(foreignRegistration.TryConfigureBridge(foreignBridge, out string foreignIssue),
                    "Could not configure foreign bridge registration. " + foreignIssue);
                var foreignFake = new QaFakePauseRuntimePort();
                GlobalUiPauseRuntimeBindingResult foreign =
                    GlobalUiSceneRuntime.TryBindPauseInputModeRuntime(
                        new[] { foreignRegistrationRoot }, foreignFake);
                Require(!foreign.Succeeded && foreign.Status == "RejectedForeignBridge" &&
                    !foreignBridge.HasPauseRuntimeBinding &&
                    foreignFake.TryGetPauseSnapshotCount == 0,
                    "Bridge outside the known persistent roots was not rejected. " + foreign.Message);
                completed.Add("foreign-bridge-rejected");

                GameObject duplicateRoot = CreateRoot("H21 Duplicate Root", objects);
                GameObject duplicateChild = new GameObject("H21 Duplicate Child");
                duplicateChild.transform.SetParent(duplicateRoot.transform, false);
                objects.Add(duplicateChild);
                duplicateRoot.AddComponent<PauseInputModeRuntimeBridgeRegistration>();
                duplicateChild.AddComponent<PauseInputModeRuntimeBridgeRegistration>();
                var duplicateFake = new QaFakePauseRuntimePort();
                GlobalUiPauseRuntimeBindingResult duplicate =
                    GlobalUiSceneRuntime.TryBindPauseInputModeRuntime(
                        new[] { duplicateRoot }, duplicateFake);
                Require(!duplicate.Succeeded &&
                    duplicate.Status == "RejectedDuplicateRegistration" &&
                    duplicate.RegistrationCount == 2 &&
                    duplicateFake.TryGetPauseSnapshotCount == 0,
                    "Duplicate registrations were not rejected before a bridge was selected. " + duplicate.Message);
                completed.Add("duplicate-registrations-rejected");

                Require(completed.Count == 5,
                    "H2.1 Pause runtime composition smoke case count changed unexpectedly.");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = objects.Count - 1; index >= 0; index--)
                {
                    if (objects[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(objects[index]);
                    }
                }
            }
        }

        private static GameObject CreateRoot(
            string name,
            ICollection<UnityEngine.Object> objects)
        {
            var root = new GameObject(name);
            objects.Add(root);
            return root;
        }

        private static PauseInputModeUnityPlayerInputRuntimeBridge CreateBridge(
            GameObject root,
            ICollection<UnityEngine.Object> objects)
        {
            var playerObject = new GameObject(root.name + " PlayerInput");
            playerObject.transform.SetParent(root.transform, false);
            objects.Add(playerObject);
            var actions = ScriptableObject.CreateInstance<InputActionAsset>();
            actions.AddActionMap(new InputActionMap("Global"));
            actions.AddActionMap(new InputActionMap("Player"));
            actions.AddActionMap(new InputActionMap("UI"));
            objects.Add(actions);
            PlayerInput playerInput = playerObject.AddComponent<PlayerInput>();
            playerInput.actions = actions;
            playerInput.defaultActionMap = "Global";
            PauseInputModeUnityPlayerInputRuntimeBridge bridge =
                root.AddComponent<PauseInputModeUnityPlayerInputRuntimeBridge>();
            bridge.ConfigureForDiagnostics(
                playerInput,
                Array.Empty<Immersive.Framework.UnityInput.UnityInputTargetDeclaration>(),
                Array.Empty<Immersive.Framework.Actors.PlayerActorDeclaration>(),
                null,
                "Player",
                "UI",
                false);
            return bridge;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

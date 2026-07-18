using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.UnityInput.Editor
{
    public static class QaIc1PlayerInputSingleWriterSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Unity Input/IC1 Run PlayerInput Single Writer Smoke";
        private const string LogPrefix =
            "[IC1_PLAYER_INPUT_SINGLE_WRITER_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            GameObject root = null;
            InputActionAsset actions = null;

            try
            {
                Require(EditorApplication.isPlaying,
                    "IC1 smoke requires Play Mode.");
                completed.Add("play-mode-required");

                string packageRoot = ResolvePackageRoot();
                ValidateSourceOwnership(packageRoot, completed);

                root = new GameObject("IC1 PlayerInput Single Writer Probe");
                root.SetActive(false);
                PlayerInput playerInput = root.AddComponent<PlayerInput>();
                UnityPlayerInputGateAdapter authority =
                    root.AddComponent<UnityPlayerInputGateAdapter>();

                actions = ScriptableObject.CreateInstance<InputActionAsset>();
                actions.AddActionMap("Global").AddAction("PauseToggle");
                actions.AddActionMap("UI").AddAction("Navigate");
                actions.AddActionMap("Player").AddAction("Move");
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Global";

                var serializedAuthority = new SerializedObject(authority);
                serializedAuthority.FindProperty("playerInput")
                    .objectReferenceValue = playerInput;
                serializedAuthority.FindProperty("gameplayActionMapName")
                    .stringValue = "Player";
                serializedAuthority.FindProperty("applyOnEnable")
                    .boolValue = false;
                serializedAuthority.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(true);

                Require(ReferenceEquals(authority.PlayerInput, playerInput),
                    "Write authority did not preserve the exact PlayerInput target.");
                completed.Add("explicit-authority-target");

                MethodInfo select = RequireMethod(
                    typeof(UnityPlayerInputGateAdapter),
                    "TrySelectActionMap",
                    parameterCount: 5);
                MethodInfo restore = RequireMethod(
                    typeof(UnityPlayerInputGateAdapter),
                    "TryRestoreActionMap",
                    parameterCount: 4);
                MethodInfo applySet = RequireMethod(
                    typeof(UnityPlayerInputGateAdapter),
                    "TryApplyActionMapSet",
                    parameterCount: 6);
                MethodInfo restoreSet = RequireMethod(
                    typeof(UnityPlayerInputGateAdapter),
                    "TryRestoreActionMapSet",
                    parameterCount: 4);

                object uiReceipt = Select(
                    select,
                    authority,
                    "UI",
                    "initial-ui");
                Require(CurrentMap(playerInput) == "UI",
                    "Authority did not select UI action map.");
                completed.Add("ui-map-selected-by-authority");

                object gameplayReceipt = Select(
                    select,
                    authority,
                    "Player",
                    "gameplay-bind");
                Require(CurrentMap(playerInput) == "Player",
                    "Authority did not select Player action map.");
                completed.Add("gameplay-map-selected-by-authority");

                Restore(
                    restore,
                    authority,
                    gameplayReceipt,
                    "gameplay-release");
                Require(CurrentMap(playerInput) == "UI",
                    "Authority did not restore the previous UI action map.");
                completed.Add("previous-map-restored");

                Restore(
                    restore,
                    authority,
                    uiReceipt,
                    "initial-ui-release");
                Require(string.IsNullOrEmpty(CurrentMap(playerInput)),
                    "Initial empty action-map state was not restored.");
                completed.Add("empty-map-state-restored");

                object secondUiReceipt = Select(
                    select,
                    authority,
                    "UI",
                    "idempotent-ui");
                object sameUiReceipt = Select(
                    select,
                    authority,
                    "UI",
                    "idempotent-ui-repeat");
                Require(CurrentMap(playerInput) == "UI",
                    "Repeated selection changed the requested action map.");
                completed.Add("repeated-selection-idempotent");

                Restore(
                    restore,
                    authority,
                    sameUiReceipt,
                    "idempotent-ui-repeat-release");
                Require(CurrentMap(playerInput) == "UI",
                    "No-change receipt incorrectly cleared the selected map.");
                completed.Add("no-change-receipt-safe");

                Restore(
                    restore,
                    authority,
                    secondUiReceipt,
                    "idempotent-ui-release");
                completed.Add("synthetic-state-clean");

                object gameplaySetReceipt = ApplySet(
                    applySet,
                    authority,
                    "Player",
                    new[] { "Global", "Player" },
                    "global-gameplay-baseline");
                Require(
                    CurrentMap(playerInput) == "Player" &&
                    HasExactEnabledMaps(playerInput, "Global", "Player"),
                    "Exact Global + Player posture was not applied.");
                completed.Add("global-player-set-applied");

                object pauseSetReceipt = ApplySet(
                    applySet,
                    authority,
                    "UI",
                    new[] { "Global", "UI" },
                    "global-ui-baseline");
                Require(
                    CurrentMap(playerInput) == "UI" &&
                    HasExactEnabledMaps(playerInput, "Global", "UI"),
                    "Exact Global + UI posture was not applied.");
                completed.Add("global-ui-set-applied");

                RestoreSet(
                    restoreSet,
                    authority,
                    pauseSetReceipt,
                    "global-ui-release");
                Require(
                    CurrentMap(playerInput) == "Player" &&
                    HasExactEnabledMaps(playerInput, "Global", "Player"),
                    "Exact Global + Player posture was not restored.");
                completed.Add("layered-set-rollback-exact");

                RestoreSet(
                    restoreSet,
                    authority,
                    gameplaySetReceipt,
                    "global-gameplay-release");
                completed.Add("layered-set-baseline-restored");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.Destroy(root);
                }

                if (actions != null)
                {
                    UnityEngine.Object.Destroy(actions);
                }
            }
        }

        private static void ValidateSourceOwnership(
            string packageRoot,
            ICollection<string> completed)
        {
            string writer = Read(packageRoot,
                "Runtime/UnityInput/UnityPlayerInputStateWriter.cs");
            string gate = Read(packageRoot,
                "Runtime/UnityInput/UnityPlayerInputGateAdapter.cs");
            string inputMode = Read(packageRoot,
                "Runtime/InputMode/InputModeUnityPlayerInputAdapter.cs");
            string gameplay = Read(packageRoot,
                "Runtime/PlayerParticipation/Runtime/PlayerGameplayInputBindingRuntimeContext.cs");
            string pause = Read(packageRoot,
                "Runtime/Pause/PauseInputActionTrigger.cs");
            string inputModeApplication = Read(packageRoot,
                "Runtime/InputMode/InputModeUnityPlayerInputApplication.cs");

            Require(writer.Contains("playerInput.currentActionMap = targetActionMap"),
                "Canonical writer has no action-map selection side effect.");
            Require(!writer.Contains("SwitchCurrentActionMap("),
                "Canonical writer still depends on PlayerInput's private lifecycle gate.");
            Require(writer.Contains("currentActionMap = null"),
                "Canonical writer has no empty-map restoration side effect.");
            Require(writer.Contains("TryApplyActionMapSet"),
                "Canonical writer has no exact action-map set application.");
            Require(!writer.Contains(".ActivateInput("),
                "Canonical map writer incorrectly owns PlayerInput activation.");
            Require(!writer.Contains(".DeactivateInput("),
                "Canonical map writer incorrectly owns PlayerInput deactivation.");
            completed.Add("physical-writer-present");

            Require(!writer.Contains("inputIsActive"),
                "Canonical map writer still depends on PlayerInput lifecycle activation state.");
            completed.Add("writer-lifecycle-independent");

            Require(gate.Contains("UnityPlayerInputStateWriter.Try"),
                "Gate authority does not delegate to the physical writer.");
            completed.Add("gate-is-write-port");

            AssertNoDirectPlayerInputWrite(inputMode, "InputMode adapter");
            AssertNoDirectPlayerInputWrite(gameplay, "Gameplay binding context");
            AssertNoDirectPlayerInputWrite(gate, "Gate authority");
            AssertNoDirectPlayerInputWrite(pause, "Pause trigger");
            AssertNoDirectPlayerInputWrite(
                inputModeApplication,
                "InputMode application");
            completed.Add("requesters-have-no-direct-write");

            Require(!inputMode.Contains("UnityPlayerInputStateWriter.Try"),
                "InputMode bypasses the explicit Gate write authority.");
            Require(!gameplay.Contains("UnityPlayerInputStateWriter.Try"),
                "Gameplay binding bypasses the explicit Gate write authority.");
            Require(!pause.Contains("UnityPlayerInputStateWriter.Try"),
                "Pause trigger bypasses the explicit Gate write authority.");
            completed.Add("single-request-port-enforced");
        }

        private static void AssertNoDirectPlayerInputWrite(
            string source,
            string label)
        {
            string[] forbidden =
            {
                "SwitchCurrentActionMap(",
                "currentActionMap =",
                ".ActivateInput(",
                ".DeactivateInput("
            };

            for (int index = 0; index < forbidden.Length; index++)
            {
                Require(!source.Contains(forbidden[index]),
                    $"{label} contains forbidden direct write '{forbidden[index]}'.");
            }
        }

        private static object Select(
            MethodInfo method,
            UnityPlayerInputGateAdapter authority,
            string actionMapName,
            string reason)
        {
            object[] arguments =
            {
                actionMapName,
                nameof(QaIc1PlayerInputSingleWriterSmoke),
                reason,
                null,
                null
            };
            bool succeeded = (bool)method.Invoke(authority, arguments);
            Require(succeeded,
                $"Action-map selection '{actionMapName}' failed. issue='{arguments[4]}'.");
            Require(arguments[3] != null,
                "Action-map selection returned no write receipt.");
            return arguments[3];
        }

        private static object ApplySet(
            MethodInfo method,
            UnityPlayerInputGateAdapter authority,
            string primaryActionMapName,
            string[] enabledActionMapNames,
            string reason)
        {
            object[] arguments =
            {
                primaryActionMapName,
                enabledActionMapNames,
                nameof(QaIc1PlayerInputSingleWriterSmoke),
                reason,
                null,
                null
            };
            bool succeeded = (bool)method.Invoke(authority, arguments);
            Require(succeeded,
                $"Action-map set application failed. issue='{arguments[5]}'.");
            Require(arguments[4] != null,
                "Action-map set application returned no write receipt.");
            return arguments[4];
        }

        private static void RestoreSet(
            MethodInfo method,
            UnityPlayerInputGateAdapter authority,
            object receipt,
            string reason)
        {
            object[] arguments =
            {
                receipt,
                nameof(QaIc1PlayerInputSingleWriterSmoke),
                reason,
                null
            };
            bool succeeded = (bool)method.Invoke(authority, arguments);
            Require(succeeded,
                $"Action-map set restore failed. issue='{arguments[3]}'.");
        }

        private static bool HasExactEnabledMaps(
            PlayerInput playerInput,
            params string[] expectedNames)
        {
            var expected = new HashSet<string>(
                expectedNames ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            foreach (InputActionMap map in playerInput.actions.actionMaps)
            {
                if (map.enabled != expected.Contains(map.name))
                {
                    return false;
                }
            }

            return true;
        }

        private static void Restore(
            MethodInfo method,
            UnityPlayerInputGateAdapter authority,
            object receipt,
            string reason)
        {
            object[] arguments =
            {
                receipt,
                nameof(QaIc1PlayerInputSingleWriterSmoke),
                reason,
                null
            };
            bool succeeded = (bool)method.Invoke(authority, arguments);
            Require(succeeded,
                $"Action-map restore failed. issue='{arguments[3]}'.");
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            int parameterCount)
        {
            MethodInfo[] methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public);
            for (int index = 0; index < methods.Length; index++)
            {
                MethodInfo method = methods[index];
                if (method.Name == name &&
                    method.GetParameters().Length == parameterCount)
                {
                    return method;
                }
            }

            throw new InvalidOperationException(
                $"Required method '{type.FullName}.{name}' is unavailable.");
        }

        private static string CurrentMap(PlayerInput playerInput) =>
            playerInput != null && playerInput.currentActionMap != null
                ? playerInput.currentActionMap.name
                : string.Empty;

        private static string ResolvePackageRoot()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(UnityPlayerInputGateAdapter).Assembly);
            Require(package != null && !string.IsNullOrWhiteSpace(package.resolvedPath),
                "Could not resolve com.immersive.framework package path.");
            return package.resolvedPath;
        }

        private static string Read(string root, string relativePath)
        {
            string path = Path.Combine(
                root,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            Require(File.Exists(path),
                $"Required package source is missing: '{path}'.");
            return File.ReadAllText(path);
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
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
    }
}

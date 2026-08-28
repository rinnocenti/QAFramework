using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Edit Mode structural/authoring contract for the canonical Player QA
    /// fixtures. It does not start Play Mode or materialize a live Session.
    /// </summary>
    public static class PlayerQaAuthoringRegression
    {
        private const string Prefix = "[QA_PLAYER_AUTHORING]";
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run Authoring Contract";
        private const int ExpectedCaseCount = 10;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            Validate(emitResult: true);
        }

        internal static void Validate(bool emitResult)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "Player QA authoring contract must run in Edit Mode.");
            }

            var completed = new List<string>(ExpectedCaseCount);
            try
            {
                InputActionAsset actions = Require<InputActionAsset>(PlayerQaPaths.InputActionsPath);
                PlayerSlotProfile playerOne = Require<PlayerSlotProfile>(PlayerQaPaths.PlayerOneSlotPath);
                PlayerSlotProfile playerTwo = Require<PlayerSlotProfile>(PlayerQaPaths.PlayerTwoSlotPath);
                ActorProfile defaultActor = Require<ActorProfile>(PlayerQaPaths.DefaultActorPath);
                ActorProfile alternateActor = Require<ActorProfile>(PlayerQaPaths.AlternateActorPath);
                GameObject defaultPresentation = Require<GameObject>(PlayerQaPaths.DefaultPresentationPath);
                GameObject alternatePresentation = Require<GameObject>(PlayerQaPaths.AlternatePresentationPath);
                GameObject runtimeHostPrefab = Require<GameObject>(PlayerQaPaths.RuntimeHostPath);
                GameObject managerHostPrefab = Require<GameObject>(PlayerQaPaths.ManagerHostPath);
                GameObject sceneHostPrefab = Require<GameObject>(PlayerQaPaths.SceneHostPath);
                PlayerSessionProfile managerSession = Require<PlayerSessionProfile>(PlayerQaPaths.ManagerSessionPath);
                PlayerSessionProfile sceneSession = Require<PlayerSessionProfile>(PlayerQaPaths.SceneSessionPath);
                PlayerSessionProfile closedSession =
                    Require<PlayerSessionProfile>(PlayerQaPaths.ClosedUnresolvedSessionPath);

                ValidateInput(actions);
                completed.Add("input");

                ValidateProfiles(playerOne, playerTwo, defaultActor, alternateActor);
                completed.Add("profiles");

                ValidatePresentations(defaultPresentation, alternatePresentation);
                completed.Add("presentations");

                PlayerActorRuntimeHost runtimeHost = ValidateRuntimeHost(runtimeHostPrefab);
                completed.Add("runtime-host");

                ValidateManagerHost(managerHostPrefab, actions, runtimeHost);
                completed.Add("manager-host");

                ValidateSceneHost(
                    sceneHostPrefab,
                    actions,
                    playerOne,
                    defaultActor,
                    defaultPresentation,
                    runtimeHostPrefab);
                completed.Add("scene-host");

                ValidateSession(
                    managerSession,
                    playerOne,
                    playerTwo,
                    true,
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault);
                ValidateSession(
                    sceneSession,
                    playerOne,
                    playerTwo,
                    false,
                    PlayerHostProvisioningMode.SceneProvided,
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault);
                completed.Add("session-profiles");

                ValidateSession(
                    closedSession,
                    playerOne,
                    playerTwo,
                    false,
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    PlayerActorResolutionPolicy.LeaveUnresolved);
                completed.Add("leave-unresolved-profile");

                ValidateExplicitCommandSurface();
                completed.Add("explicit-command-surface");

                ValidateGameplayInputReader();
                completed.Add("gameplay-input-reader");

                Require(completed.Count == ExpectedCaseCount,
                    "Player QA authoring case count changed unexpectedly.");

                if (emitResult)
                {
                    Debug.Log(
                        $"{Prefix} status='Passed' verdict='AuthoringComplete' " +
                        $"cases='{completed.Count}/{ExpectedCaseCount}' " +
                        $"completed='{string.Join(",", completed)}'.");
                }
            }
            catch (Exception exception)
            {
                if (emitResult)
                {
                    Debug.LogError(
                        $"{Prefix} status='Failed' verdict='AuthoringInvalid' " +
                        $"cases='{completed.Count}/{ExpectedCaseCount}' " +
                        $"completed='{string.Join(",", completed)}' " +
                        $"missing='{Escape(exception.Message)}'.");
                }

                throw;
            }
        }

        private static void ValidateInput(InputActionAsset actions)
        {
            Require(actions != null, "Player QA Input Actions asset is missing.");
            InputActionMap gameplay = actions.FindActionMap("Gameplay", false);
            Require(gameplay != null, "Player QA Input Actions asset has no Gameplay map.");
            InputAction activate = gameplay.FindAction("Activate", false);
            Require(activate != null, "Player QA Gameplay map has no Activate action.");
            Require(
                activate.bindings.Count >= 1 &&
                activate.bindings[0].effectivePath == "<Keyboard>/space",
                "Player QA Activate action must keep its explicit space binding.");
            InputActionMap global = actions.FindActionMap("Global", false);
            Require(global != null && global.FindAction("Pause", false) != null,
                "Player QA Input Actions asset must author a Global Pause action.");
        }

        private static void ValidateProfiles(
            PlayerSlotProfile playerOne,
            PlayerSlotProfile playerTwo,
            ActorProfile defaultActor,
            ActorProfile alternateActor)
        {
            Require(
                playerOne.PlayerSlotIdText == PlayerQaPaths.PlayerOneSlotId &&
                playerTwo.PlayerSlotIdText == PlayerQaPaths.PlayerTwoSlotId &&
                playerOne.PlayerSlotId != playerTwo.PlayerSlotId,
                "Player QA Slot identities must remain distinct and stable.");
            Require(
                defaultActor.ActorProfileIdText == PlayerQaPaths.DefaultActorId &&
                alternateActor.ActorProfileIdText == PlayerQaPaths.AlternateActorId,
                "Player QA Actor identities must remain stable.");
            Require(
                playerOne.DefaultActorProfile == defaultActor &&
                playerTwo.DefaultActorProfile == alternateActor,
                "Player QA Slot Profiles must retain their explicit default Actor Profiles.");
        }

        private static void ValidatePresentations(
            GameObject defaultPresentation,
            GameObject alternatePresentation)
        {
            Require(defaultPresentation != alternatePresentation,
                "Default and Alternate Presentation fixtures must be distinct prefabs.");
            ValidatePresentation(defaultPresentation, "Default");
            ValidatePresentation(alternatePresentation, "Alternate");
        }

        private static void ValidatePresentation(GameObject presentation, string label)
        {
            Require(
                PrefabUtility.IsPartOfPrefabAsset(presentation),
                $"{label} Presentation must be a prefab asset.");
            Require(
                presentation.GetComponentsInChildren<PlayerInput>(true).Length == 0 &&
                presentation.GetComponentsInChildren<PlayerActorRuntimeHost>(true).Length == 0,
                $"{label} Presentation must not contain PlayerInput or Player Actor runtime infrastructure.");
        }

        private static PlayerActorRuntimeHost ValidateRuntimeHost(GameObject runtimeHostPrefab)
        {
            PlayerActorRuntimeHost runtimeHost =
                runtimeHostPrefab.GetComponent<PlayerActorRuntimeHost>();
            string issue = string.Empty;
            Require(
                runtimeHost != null &&
                runtimeHost.TryValidateConfiguration(out issue),
                $"Player QA Runtime Host prefab is invalid. {issue}");
            Require(
                runtimeHostPrefab.GetComponentsInChildren<PlayerInput>(true).Length == 0 &&
                runtimeHost.PresentationMount != null &&
                runtimeHost.PresentationMount.childCount == 0,
                "Runtime Host prefab must have no PlayerInput and an empty Presentation Mount.");
            return runtimeHost;
        }

        private static void ValidateManagerHost(
            GameObject managerHostPrefab,
            InputActionAsset actions,
            PlayerActorRuntimeHost runtimeHost)
        {
            LocalPlayerHostAuthoring host = RequireSingle<LocalPlayerHostAuthoring>(managerHostPrefab);
            PlayerInput playerInput = RequireSingle<PlayerInput>(managerHostPrefab);
            Require(
                host.TryValidateConfiguration(out string issue),
                $"Manager Local Player Host is invalid. {issue}");
            Require(
                playerInput.actions == actions &&
                playerInput.defaultActionMap == "Gameplay" &&
                host.PlayerActorRuntimeHostPrefab == runtimeHost,
                "Manager Local Player Host lost its Input or Runtime Host prefab reference.");
            Require(
                host.ActorMount.childCount == 0 &&
                managerHostPrefab.GetComponent<SceneLocalPlayerAdmissionAuthoring>() == null,
                "Manager Local Player Host must keep an empty Actor Mount and no Scene admission.");
        }

        private static void ValidateSceneHost(
            GameObject sceneHostPrefab,
            InputActionAsset actions,
            PlayerSlotProfile playerOne,
            ActorProfile defaultActor,
            GameObject defaultPresentation,
            GameObject runtimeHostPrefab)
        {
            LocalPlayerHostAuthoring host = RequireSingle<LocalPlayerHostAuthoring>(sceneHostPrefab);
            PlayerInput playerInput = RequireSingle<PlayerInput>(sceneHostPrefab);
            SceneLocalPlayerAdmissionAuthoring admission =
                RequireSingle<SceneLocalPlayerAdmissionAuthoring>(sceneHostPrefab);
            PlayerActorRuntimeHost sceneRuntimeHost =
                RequireSingleInChildren<PlayerActorRuntimeHost>(host.ActorMount);
            Require(
                playerInput.actions == actions &&
                admission.PlayerSlotProfile == playerOne &&
                admission.ActorProfile == defaultActor &&
                admission.ScenePlayerActorRuntimeHost == sceneRuntimeHost,
                "Scene Local Player Host admission references are incomplete or foreign.");
            Require(
                host.TryValidateAdmissionConfiguration(sceneRuntimeHost, true, out string hostIssue),
                $"Scene Local Player Host is invalid. {hostIssue}");
            Require(
                admission.TryValidateRuntimeEvidence(out string admissionIssue),
                $"Scene Local Player Admission evidence is invalid. {admissionIssue}");
            Require(
                SourcePrefab(sceneRuntimeHost.gameObject) == runtimeHostPrefab &&
                SourcePrefab(admission.ScenePresentation) == defaultPresentation,
                "Scene Local Player Host must retain exact Runtime Host and Presentation prefab provenance.");
        }

        private static void ValidateSession(
            PlayerSessionProfile profile,
            PlayerSlotProfile playerOne,
            PlayerSlotProfile playerTwo,
            bool initialJoiningOpen,
            PlayerHostProvisioningMode hostProvisioning,
            PlayerActorResolutionPolicy actorResolution)
        {
            Require(profile.TryValidate(out string issue),
                $"Player Session Profile '{profile.name}' is invalid. {issue}");
            Require(
                profile.SupportedSlotCount == 2 &&
                profile.SupportedSlots[0] == playerOne &&
                profile.SupportedSlots[1] == playerTwo &&
                profile.InitialJoiningOpen == initialJoiningOpen &&
                profile.HostProvisioning == hostProvisioning &&
                profile.ActorResolutionPolicy == actorResolution,
                $"Player Session Profile '{profile.name}' lost its authored Slot universe or policies.");
        }

        private static void ValidateExplicitCommandSurface()
        {
            Type[] commands =
            {
                typeof(PlayerSessionOpenJoiningCommandTrigger),
                typeof(PlayerSessionCloseJoiningCommandTrigger),
                typeof(PlayerSessionJoinCommandTrigger),
                typeof(PlayerSessionSelectActorCommandTrigger),
                typeof(PlayerSessionDefaultActorSelectionCommandTrigger),
                typeof(PlayerSessionReplaceActorSelectionCommandTrigger),
                typeof(PlayerSessionClearActorSelectionCommandTrigger),
                typeof(PlayerSessionLeaveCommandTrigger)
            };

            Require(commands.Length == 8,
                "Player Session explicit command surface must remain eight distinct components.");
            var unique = new HashSet<Type>(commands);
            Require(unique.Count == commands.Length,
                "Player Session commands must not collapse into a shared operation enum type.");

            var host = new GameObject("QA Player Command Surface");
            try
            {
                host.SetActive(false);
                for (int index = 0; index < commands.Length; index++)
                {
                    var command = host.AddComponent(commands[index]) as PlayerSessionCommandTriggerBase;
                    Require(command != null, $"Command '{commands[index].Name}' did not materialize.");
                    var serialized = new SerializedObject(command);
                    Require(
                        serialized.FindProperty("operation") == null &&
                        serialized.FindProperty("command") == null,
                        $"Command '{commands[index].Name}' must not serialize an operation enum.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static void ValidateGameplayInputReader()
        {
            Require(
                typeof(PlayerGameplayInputReader).GetCustomAttributes(
                    typeof(DisallowMultipleComponent), true).Length > 0,
                "PlayerGameplayInputReader must prevent duplicate consumers on one Actor.");
            Require(
                typeof(PlayerGameplayInputReader).GetCustomAttributes(
                    typeof(RequireComponent), true).Length > 0,
                "PlayerGameplayInputReader must require PlayerActorDeclaration.");

            var host = new GameObject("QA Player Gameplay Input Reader");
            try
            {
                host.SetActive(false);
                host.AddComponent<PlayerActorDeclaration>();
                PlayerGameplayInputReader reader = host.AddComponent<PlayerGameplayInputReader>();
                var serialized = new SerializedObject(reader);
                Require(
                    serialized.FindProperty("playerInput") == null &&
                    serialized.FindProperty("actions") == null,
                    "PlayerGameplayInputReader must not serialize PlayerInput or a raw Input Action Asset.");
                Require(
                    !reader.HasCurrentGameplayBinding,
                    "Unbound PlayerGameplayInputReader must stay fail-closed.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static T Require<T>(string path)
            where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null,
                $"Required Player QA fixture is missing at '{path}'. Run Configure Player QA.");
            return asset;
        }

        private static T RequireSingle<T>(GameObject root)
            where T : Component
        {
            T[] components = root.GetComponents<T>();
            Require(
                components.Length == 1,
                $"Fixture '{root.name}' requires exactly one '{typeof(T).Name}', found '{components.Length}'.");
            return components[0];
        }

        private static T RequireSingleInChildren<T>(Transform root)
            where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            Require(
                components.Length == 1,
                $"Fixture subtree '{root.name}' requires exactly one '{typeof(T).Name}', found '{components.Length}'.");
            return components[0];
        }

        private static GameObject SourcePrefab(GameObject instance)
        {
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(instance);
            GameObject source = root != null
                ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(root)
                : PrefabUtility.GetCorrespondingObjectFromOriginalSource(instance);
            return source != null ? source.transform.root.gameObject : null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}

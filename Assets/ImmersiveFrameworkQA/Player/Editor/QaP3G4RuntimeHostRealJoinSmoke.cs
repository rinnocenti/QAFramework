using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Play Mode smoke using the real PlayerInputManager backend and stable Local Player Host.
    /// One-shot per Play Mode because local leave is outside this cut.
    /// </summary>
    public static class QaP3G4RuntimeHostRealJoinSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3G.4 Run Runtime Host Real Join Smoke";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "P3G.4 real join smoke must run in Play Mode.");
                LocalPlayerProvisioningAuthoring authoring = ResolveAuthoring();
                completed.Add("runtime-authoring-resolved");

                AssertTrue(authoring.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " +
                    authoring.RuntimeDiagnostic);
                completed.Add("runtime-bound-to-session");

                PlayerInputManager manager = authoring.PlayerInputManager;
                AssertNotNull(manager, "Authoring has no PlayerInputManager.");
                AssertEqual(PlayerJoinBehavior.JoinPlayersManually,
                    manager.joinBehavior,
                    "Manager is not configured for manual join.");
                completed.Add("manager-manual-join");

                AssertEqual(PlayerNotifications.InvokeCSharpEvents,
                    manager.notificationBehavior,
                    "Manager is not configured for C# callbacks.");
                completed.Add("manager-csharp-join-notifications");

                GameObject prefab = manager.playerPrefab;
                AssertNotNull(prefab, "Manager has no Player Prefab.");
                LocalPlayerHostAuthoring prefabHost =
                    prefab.GetComponent<LocalPlayerHostAuthoring>();
                AssertNotNull(prefabHost,
                    "Player Prefab has no LocalPlayerHostAuthoring.");
                AssertTrue(prefab.GetComponentInChildren<ActorDeclaration>(true) == null,
                    "Player Prefab contains a Logical Actor declaration.");
                completed.Add("player-prefab-is-technical-host");

                PlayerParticipationSnapshot initial = authoring.RuntimeSnapshot;
                AssertTrue(initial.IsInitialized,
                    "Session participation snapshot is not initialized.");
                AssertEqual(0, initial.JoinedCount,
                    "Smoke is one-shot. Re-enter Play Mode before running again.");
                AssertEqual(0, manager.playerCount,
                    "PlayerInputManager already contains Players.");
                string contextId = initial.ContextId;
                completed.Add("initial-session-clean");

                AssertTrue(!manager.joiningEnabled,
                    "Technical joining gate must start closed.");
                completed.Add("manager-technical-joining-starts-closed");

                PlayerParticipationOperationResult openResult = authoring.OpenJoining(
                    nameof(QaP3G4RuntimeHostRealJoinSmoke),
                    "real-playerinputmanager-join");
                AssertTrue(openResult.Completed && openResult.Snapshot.JoiningOpen,
                    "Opening joining failed. " + openResult.ToDiagnosticString());
                AssertTrue(manager.joiningEnabled,
                    "Technical joining gate did not open.");
                completed.Add("joining-opened-explicitly");

                LocalPlayerJoinResult joinResult = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaP3G4RuntimeHostRealJoinSmoke),
                        "real-playerinputmanager-join"));
                AssertNotNull(joinResult, "Real join returned no result.");
                AssertTrue(joinResult.Succeeded,
                    "Real PlayerInputManager join failed. " + joinResult.ToDiagnosticString());
                completed.Add("real-playerinputmanager-join-succeeded");

                AssertEqual(
                    LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput,
                    joinResult.CallbackConfirmation,
                    "Real joined callback did not confirm direct result.");
                completed.Add("real-callback-confirmed");

                AssertNotNull(joinResult.PlayerInput,
                    "Successful join has no PlayerInput evidence.");
                AssertTrue(joinResult.PlayerInput.gameObject.scene.IsValid(),
                    "Provisioned PlayerInput is not a real Scene instance.");
                completed.Add("real-playerinput-created");

                LocalPlayerHostAuthoring host = joinResult.LocalPlayerHost;
                AssertNotNull(host,
                    "Successful join has no Local Player Host evidence.");
                AssertSame(joinResult.PlayerInput.gameObject, host.gameObject,
                    "Local Player Host is not the provisioned PlayerInput root.");
                AssertSame(joinResult.PlayerInput, host.PlayerInput,
                    "Local Player Host does not resolve provisioned PlayerInput.");
                AssertNotNull(host.ActorMount,
                    "Local Player Host has no Actor Mount.");
                completed.Add("local-player-host-resolved");

                AssertTrue(host.HasJoinedSlot,
                    "Local Player Host did not commit Slot binding.");
                AssertEqual(joinResult.Slot.PlayerSlotId, host.JoinedPlayerSlotId,
                    "Host Slot differs from Session Slot.");
                AssertEqual(joinResult.Slot.ConfiguredIndex, host.JoinedConfiguredIndex,
                    "Host configured index differs from Session Slot.");
                AssertSame(joinResult.PlayerInput, host.PlayerInput,
                    "Joined host lost PlayerInput evidence.");
                completed.Add("joined-slot-bound-to-host");

                AssertTrue(!host.HasLogicalActor,
                    "Join prepared a Logical Actor implicitly.");
                AssertTrue(host.ActorMount.GetComponentInChildren<ActorDeclaration>(true) == null,
                    "Actor Mount is not empty after join.");
                completed.Add("logical-actor-remains-unprepared");

                AssertTrue(joinResult.UnityPlayerIndex >= 0,
                    "Unity playerIndex evidence is invalid.");
                AssertEqual(joinResult.PlayerInput.playerIndex,
                    joinResult.UnityPlayerIndex,
                    "Result playerIndex differs from PlayerInput.");
                completed.Add("player-index-diagnostic-only");

                AssertTrue(joinResult.Slot.IsValid && joinResult.Slot.IsJoined,
                    "Successful result has no Joined Slot snapshot.");
                AssertEqual(0, joinResult.Slot.ConfiguredIndex,
                    "First join did not use first configured Slot.");
                completed.Add("first-configured-slot-joined");

                AssertTrue(joinResult.HasReservationEvidence &&
                    joinResult.HasCommitEvidence &&
                    !joinResult.HasRollbackEvidence,
                    "Join evidence is incomplete.");
                completed.Add("join-evidence-preserved");

                PlayerParticipationSnapshot afterJoin = authoring.RuntimeSnapshot;
                AssertEqual(contextId, afterJoin.ContextId,
                    "Join replaced Session context.");
                AssertEqual(1, afterJoin.JoinedCount,
                    "Session did not record one Joined Slot.");
                AssertEqual(0, afterJoin.SelectedActorCount,
                    "Join selected an Actor implicitly.");
                completed.Add("host-context-records-joined-unselected-slot");

                AssertEqual(1, manager.playerCount,
                    "PlayerInputManager did not record one Player.");
                AssertTrue(ContainsPlayerInput(joinResult.PlayerInput),
                    "Provisioned PlayerInput is absent from PlayerInput.all.");
                completed.Add("unity-playerinput-global-evidence");

                AssertSame(joinResult, authoring.LastJoinResult,
                    "Authoring did not preserve last typed result.");
                completed.Add("authoring-last-result-preserved");

                PlayerParticipationOperationResult closeResult = authoring.CloseJoining(
                    nameof(QaP3G4RuntimeHostRealJoinSmoke),
                    "real-join-smoke-complete");
                AssertTrue(closeResult.Completed && !closeResult.Snapshot.JoiningOpen,
                    "Closing joining failed. " + closeResult.ToDiagnosticString());
                AssertEqual(1, closeResult.Snapshot.JoinedCount,
                    "Closing joining removed admitted host.");
                AssertTrue(!manager.joiningEnabled,
                    "Technical joining gate remained open.");
                completed.Add("joining-closed-non-destructively");

                Debug.Log(
                    "[P3G4_RUNTIME_HOST_REAL_JOIN_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' context='{contextId}' " +
                    $"slot='{joinResult.Slot.PlayerSlotId.StableText}' " +
                    $"host='{host.name}' actorMount='{host.ActorMount.name}' " +
                    $"playerIndex='{joinResult.UnityPlayerIndex}' " +
                    $"callback='{joinResult.CallbackConfirmation}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3G4_RUNTIME_HOST_REAL_JOIN_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return EditorApplication.isPlaying &&
                TryResolveAuthoring(out LocalPlayerProvisioningAuthoring authoring) &&
                authoring.RuntimeReady;
        }

        private static LocalPlayerProvisioningAuthoring ResolveAuthoring()
        {
            if (!TryResolveAuthoring(out LocalPlayerProvisioningAuthoring authoring))
            {
                throw new InvalidOperationException(
                    "Expected exactly one loaded LocalPlayerProvisioningAuthoring.");
            }
            return authoring;
        }

        private static bool TryResolveAuthoring(
            out LocalPlayerProvisioningAuthoring authoring)
        {
            authoring = null;
            LocalPlayerProvisioningAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<LocalPlayerProvisioningAuthoring>(
                    FindObjectsInactive.Include);
            int loadedCount = 0;
            foreach (LocalPlayerProvisioningAuthoring candidate in candidates)
            {
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded)
                {
                    continue;
                }

                loadedCount++;
                authoring = candidate;
            }

            if (loadedCount == 1)
            {
                return true;
            }

            authoring = null;
            return false;
        }

        private static bool ContainsPlayerInput(PlayerInput expected)
        {
            for (int index = 0; index < PlayerInput.all.Count; index++)
            {
                if (ReferenceEquals(PlayerInput.all[index], expected))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
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

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

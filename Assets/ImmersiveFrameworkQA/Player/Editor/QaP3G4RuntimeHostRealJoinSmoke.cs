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
    /// P3G.4 Play Mode smoke using the real PlayerInputManager backend and the public authoring
    /// request surface. This smoke is intentionally one-shot per Play Mode because local leave
    /// is scheduled for P3M.
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
                    "Local Player provisioning authoring is not bound to the Session runtime. " +
                    authoring.RuntimeDiagnostic);
                completed.Add("runtime-bound-to-session");

                PlayerInputManager manager = authoring.PlayerInputManager;
                AssertNotNull(manager, "Authoring has no PlayerInputManager.");
                AssertEqual(
                    PlayerJoinBehavior.JoinPlayersManually,
                    manager.joinBehavior,
                    "PlayerInputManager is not configured for manual join.");
                completed.Add("manager-manual-join");

                AssertEqual(
                    PlayerNotifications.InvokeCSharpEvents,
                    manager.notificationBehavior,
                    "PlayerInputManager is not configured for typed C# joined callbacks.");
                completed.Add("manager-csharp-join-notifications");

                PlayerParticipationSnapshot initial = authoring.RuntimeSnapshot;
                AssertTrue(initial.IsInitialized, "Session participation snapshot is not initialized.");
                AssertEqual(0, initial.JoinedCount,
                    "P3G.4 smoke is one-shot. Re-enter Play Mode before running it again.");
                AssertEqual(0, initial.ReservedCount,
                    "Session has a reservation before the real join smoke.");
                AssertEqual(0, manager.playerCount,
                    "PlayerInputManager already contains Players. Re-enter Play Mode.");
                string contextId = initial.ContextId;
                completed.Add("initial-session-clean");

                AssertTrue(!manager.joiningEnabled,
                    "PlayerInputManager technical joining gate must start closed with the Session logical window.");
                completed.Add("manager-technical-joining-starts-closed");

                PlayerParticipationOperationResult openResult = authoring.OpenJoining(
                    nameof(QaP3G4RuntimeHostRealJoinSmoke),
                    "real-playerinputmanager-join");
                AssertTrue(openResult.Completed,
                    "Opening joining failed. " + openResult.ToDiagnosticString());
                AssertTrue(openResult.Snapshot.JoiningOpen,
                    "Joining did not become open.");
                completed.Add("joining-opened-explicitly");

                AssertTrue(manager.joiningEnabled,
                    "PlayerInputManager technical joining gate did not open with the Session logical window.");
                completed.Add("manager-technical-joining-opened");

                var request = new LocalPlayerJoinRequest(
                    nameof(QaP3G4RuntimeHostRealJoinSmoke),
                    "real-playerinputmanager-join");
                LocalPlayerJoinResult joinResult = authoring.RequestJoin(request);
                AssertNotNull(joinResult, "Real join returned no result.");
                AssertTrue(joinResult.Succeeded,
                    "Real PlayerInputManager join failed. " + joinResult.ToDiagnosticString());
                completed.Add("real-playerinputmanager-join-succeeded");

                AssertEqual(
                    LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput,
                    joinResult.CallbackConfirmation,
                    "Real PlayerInputManager joined callback did not confirm the direct result.");
                completed.Add("real-callback-confirmed");

                AssertNotNull(joinResult.PlayerInput,
                    "Successful join has no PlayerInput evidence.");
                AssertTrue(joinResult.PlayerInput.gameObject.scene.IsValid(),
                    "Provisioned PlayerInput is not a real Scene instance.");
                completed.Add("real-playerinput-created");

                PlayerActorDeclaration declaration = joinResult.PlayerActorDeclaration;
                AssertNotNull(declaration,
                    "Successful join has no PlayerActorDeclaration evidence.");
                AssertSame(joinResult.PlayerInput.gameObject, declaration.gameObject,
                    "PlayerActorDeclaration is not on the provisioned PlayerInput host.");
                AssertSame(joinResult.PlayerInput, declaration.PlayerInput,
                    "PlayerActorDeclaration does not resolve the provisioned PlayerInput.");
                completed.Add("player-actor-declaration-resolved");

                AssertTrue(joinResult.UnityPlayerIndex >= 0,
                    "Unity playerIndex evidence is invalid.");
                AssertEqual(joinResult.PlayerInput.playerIndex, joinResult.UnityPlayerIndex,
                    "Result playerIndex differs from PlayerInput evidence.");
                completed.Add("player-index-diagnostic-only");

                AssertTrue(joinResult.Slot.IsValid,
                    "Successful join has no valid Slot snapshot.");
                AssertEqual(0, joinResult.Slot.ConfiguredIndex,
                    "First real join did not use the first Available configured Slot.");
                AssertTrue(joinResult.Slot.IsJoined,
                    "Committed Slot is not Joined.");
                completed.Add("first-configured-slot-joined");

                AssertTrue(joinResult.HasReservationEvidence,
                    "Successful join did not preserve reservation evidence.");
                AssertTrue(joinResult.HasCommitEvidence,
                    "Successful join did not preserve commit evidence.");
                AssertTrue(!joinResult.HasRollbackEvidence,
                    "Successful join unexpectedly contains rollback evidence.");
                completed.Add("join-evidence-preserved");

                PlayerParticipationSnapshot afterJoin = authoring.RuntimeSnapshot;
                AssertEqual(contextId, afterJoin.ContextId,
                    "Join replaced the Session participation context.");
                AssertEqual(1, afterJoin.JoinedCount,
                    "Session snapshot did not record one Joined Slot.");
                AssertEqual(0, afterJoin.ReservedCount,
                    "Reservation remained after Joined commit.");
                completed.Add("host-context-records-joined-slot");

                AssertEqual(1, manager.playerCount,
                    "PlayerInputManager did not record one real Player.");
                completed.Add("manager-player-count-updated");

                AssertTrue(ContainsPlayerInput(joinResult.PlayerInput),
                    "Provisioned PlayerInput is absent from PlayerInput.all.");
                completed.Add("unity-playerinput-global-evidence");

                AssertSame(joinResult, authoring.LastJoinResult,
                    "Authoring did not preserve the last typed join result.");
                completed.Add("authoring-last-result-preserved");

                PlayerParticipationOperationResult closeResult = authoring.CloseJoining(
                    nameof(QaP3G4RuntimeHostRealJoinSmoke),
                    "real-join-smoke-complete");
                AssertTrue(closeResult.Completed,
                    "Closing joining failed. " + closeResult.ToDiagnosticString());
                AssertTrue(!closeResult.Snapshot.JoiningOpen,
                    "Joining remained open after explicit close.");
                AssertEqual(1, closeResult.Snapshot.JoinedCount,
                    "Closing joining removed the admitted Player.");
                completed.Add("joining-closed-non-destructively");

                AssertTrue(!manager.joiningEnabled,
                    "PlayerInputManager technical joining gate remained open after Session joining closed.");
                completed.Add("manager-technical-joining-closed");

                Debug.Log(
                    "[P3G4_RUNTIME_HOST_REAL_JOIN_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' context='{contextId}' " +
                    $"slot='{joinResult.Slot.PlayerSlotId.StableText}' " +
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
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.InstanceID);
            int loadedCount = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                LocalPlayerProvisioningAuthoring candidate = candidates[index];
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

using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    public static class QaP3K7IPublicDefaultActorSelectionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.7I Run Public Default Actor Selection Smoke";

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();
            LocalPlayerActorSelectionRequestAuthoring endpoint = null;
            bool endpointCreated = false;

            try
            {
                AssertTrue(
                    EditorApplication.isPlaying,
                    "P3K.7I public selection smoke requires Play Mode.");
                completed.Add("play-mode-required");

                LocalPlayerProvisioningAuthoring provisioning =
                    await AwaitProvisioningAsync();
                AssertTrue(
                    provisioning.RuntimeReady,
                    "Provisioning runtime is not ready. " +
                    provisioning.RuntimeDiagnostic);
                completed.Add("provisioning-runtime-ready");

                PlayerInputManager manager =
                    provisioning.PlayerInputManager;
                AssertNotNull(
                    manager,
                    "Provisioning authoring has no PlayerInputManager.");
                AssertEqual(
                    0,
                    manager.playerCount,
                    "P3K.7I smoke is one-shot. Re-enter Play Mode.");
                completed.Add("fresh-player-manager");

                endpoint =
                    provisioning.GetComponent<
                        LocalPlayerActorSelectionRequestAuthoring>();
                if (endpoint == null)
                {
                    endpoint =
                        provisioning.gameObject.AddComponent<
                            LocalPlayerActorSelectionRequestAuthoring>();
                    endpointCreated = true;
                }
                endpoint.ProvisioningAuthoring = provisioning;
                AssertTrue(
                    endpoint.TryValidateConfiguration(out string endpointIssue),
                    endpointIssue);
                completed.Add("public-endpoint-configured");

                PlayerParticipationOperationResult opened =
                    provisioning.OpenJoining(
                        nameof(
                            QaP3K7IPublicDefaultActorSelectionSmoke),
                        "public-default-actor-selection");
                AssertTrue(
                    opened.Completed &&
                    opened.Snapshot.JoiningOpen,
                    "Opening joining failed. " +
                    opened.ToDiagnosticString());
                completed.Add("joining-opened");

                LocalPlayerJoinResult joined =
                    provisioning.RequestJoin(
                        nameof(
                            QaP3K7IPublicDefaultActorSelectionSmoke),
                        "public-default-actor-selection");
                AssertNotNull(
                    joined,
                    "Public join returned no result.");
                AssertTrue(
                    joined.Succeeded,
                    "Public join failed. " +
                    joined.ToDiagnosticString());
                completed.Add("public-join-succeeded");

                AssertNotNull(
                    joined.LocalPlayerHost,
                    "Joined Player has no stable host.");
                AssertNotNull(
                    joined.PlayerInput,
                    "Joined Player has no PlayerInput.");
                AssertSame(
                    joined.PlayerInput,
                    joined.LocalPlayerHost.PlayerInput,
                    "Stable host does not own the joined PlayerInput.");
                completed.Add("stable-host-evidence");

                AssertNotNull(
                    joined.Slot.Profile,
                    "Joined Slot has no PlayerSlotProfile.");
                AssertNotNull(
                    joined.Slot.Profile.DefaultActorProfile,
                    "Joined Slot Profile has no default ActorProfile.");
                completed.Add("default-actor-intent-present");

                PlayerActorSelectionResult selected =
                    endpoint.RequestDefaultActorSelection(
                        joined.Slot.PlayerSlotId,
                        joined.Slot.SelectionRevision,
                        nameof(
                            QaP3K7IPublicDefaultActorSelectionSmoke),
                        "select-default-after-public-join");
                AssertNotNull(
                    selected,
                    "Public default selection returned no result.");
                AssertTrue(
                    selected.Succeeded,
                    "Public default selection failed. " +
                    selected.ToDiagnosticString());
                completed.Add("public-default-selection-succeeded");

                AssertTrue(
                    selected.Slot.IsJoined &&
                    selected.Slot.HasSelectedActor,
                    "Selected Slot is not Joined and Selected.");
                AssertSame(
                    joined.Slot.Profile.DefaultActorProfile,
                    selected.SelectedActorProfile,
                    "Selected Actor differs from PlayerSlotProfile default.");
                completed.Add("selected-default-is-authoritative");

                AssertTrue(
                    selected.SelectionRevision >
                    joined.Slot.SelectionRevision,
                    "Selection revision did not advance.");
                completed.Add("selection-revision-advanced");

                PlayerParticipationSnapshot snapshot =
                    provisioning.RuntimeSnapshot;
                PlayerSlotRuntimeSnapshot current =
                    FindSlot(
                        snapshot,
                        joined.Slot.PlayerSlotId);
                AssertTrue(
                    current.IsJoined &&
                    current.HasSelectedActor,
                    "Provisioning RuntimeSnapshot lost selected Actor state.");
                AssertSame(
                    selected.SelectedActorProfile,
                    current.SelectedActorProfile,
                    "RuntimeSnapshot selected Actor differs from result.");
                completed.Add("runtime-snapshot-selected");

                PlayerActorSelectionResult repeated =
                    endpoint.RequestDefaultActorSelection(
                        current.PlayerSlotId,
                        current.SelectionRevision,
                        nameof(
                            QaP3K7IPublicDefaultActorSelectionSmoke),
                        "repeat-default-selection");
                AssertNotNull(
                    repeated,
                    "Repeated default selection returned no result.");
                AssertTrue(
                    repeated.Succeeded,
                    "Repeated default selection was not idempotent. " +
                    repeated.ToDiagnosticString());
                AssertEqual(
                    current.SelectionRevision,
                    repeated.SelectionRevision,
                    "Idempotent default selection changed revision.");
                completed.Add("default-selection-idempotent");

                AssertEqual(
                    2,
                    endpoint.RequestCount,
                    "Public endpoint request count is incorrect.");
                AssertSame(
                    repeated,
                    endpoint.LastResult,
                    "Public endpoint did not retain its last result.");
                completed.Add("endpoint-diagnostics-retained");

                PlayerParticipationOperationResult closed =
                    provisioning.CloseJoining(
                        nameof(
                            QaP3K7IPublicDefaultActorSelectionSmoke),
                        "public-default-actor-selection-complete");
                AssertTrue(
                    closed.Completed &&
                    !closed.Snapshot.JoiningOpen,
                    "Closing joining failed. " +
                    closed.ToDiagnosticString());
                completed.Add("joining-closed");

                Type endpointType =
                    typeof(
                        LocalPlayerActorSelectionRequestAuthoring);
                AssertTrue(
                    endpointType.IsPublic &&
                    endpointType.GetMethod(
                        "RequestDefaultActorSelection") != null,
                    "Default Actor selection endpoint is not public.");
                completed.Add("public-api-shape-valid");

                AssertEqual(
                    16,
                    completed.Count,
                    "Unexpected P3K.7I public selection case count.");

                Debug.Log(
                    "[P3K7I_PUBLIC_DEFAULT_ACTOR_SELECTION_SMOKE] " +
                    "status='Passed' cases='16' " +
                    $"slot='{joined.Slot.PlayerSlotId.StableText}' " +
                    $"actor='{selected.SelectedActorProfileId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K7I_PUBLIC_DEFAULT_ACTOR_SELECTION_SMOKE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{exception.Message}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (endpointCreated && endpoint != null)
                {
                    UnityEngine.Object.Destroy(endpoint);
                }
            }
        }

        private static async System.Threading.Tasks.Task<
            LocalPlayerProvisioningAuthoring>
            AwaitProvisioningAsync()
        {
            const int MaxFrames = 600;
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                LocalPlayerProvisioningAuthoring[] candidates =
                    Resources.FindObjectsOfTypeAll<
                        LocalPlayerProvisioningAuthoring>();
                for (int index = 0;
                    index < candidates.Length;
                    index++)
                {
                    LocalPlayerProvisioningAuthoring candidate =
                        candidates[index];
                    if (candidate != null &&
                        candidate.gameObject.scene.IsValid() &&
                        candidate.gameObject.scene.isLoaded &&
                        candidate.RuntimeReady)
                    {
                        return candidate;
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "Timed out waiting for LocalPlayerProvisioningAuthoring RuntimeReady.");
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            Immersive.Framework.PlayerSlots.PlayerSlotId playerSlotId)
        {
            AssertNotNull(
                snapshot,
                "Player participation snapshot is missing.");

            for (int index = 0;
                index < snapshot.Slots.Count;
                index++)
            {
                if (snapshot.Slots[index].PlayerSlotId ==
                    playerSlotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"Player Slot '{playerSlotId.StableText}' is missing from RuntimeSnapshot.");
        }

        private static void AssertTrue(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(
            object value,
            string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertSame(
            object expected,
            object actual,
            string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(
                    expected,
                    actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }
    }
}

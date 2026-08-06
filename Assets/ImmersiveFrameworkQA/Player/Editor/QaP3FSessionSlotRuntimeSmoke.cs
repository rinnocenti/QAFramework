using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Editor-only P3F.1 smoke for the isolated Session Slot runtime state machine.
    /// No scenes, persistent fixtures or PlayerInput instances are created.
    /// </summary>
    internal static class QaP3FSessionSlotRuntimeSmoke
    {
        private const string ContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeContext";

        private static readonly BindingFlags StaticInternal =
            BindingFlags.Static | BindingFlags.NonPublic;

        private static readonly BindingFlags InstanceInternal =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Immersive Framework/QA/Regressions/Player/Run Session Player Slots Regression")]
        internal static void Run()
        {
            var completed = new List<string>();
            var createdProfiles = new List<PlayerSlotProfile>();

            try
            {
                PlayerSlotProfile playerOne = CreateProfile(createdProfiles, "QA P3F Player 1", "qa.p3f.player.1");
                PlayerSlotProfile playerTwo = CreateProfile(createdProfiles, "QA P3F Player 2", "qa.p3f.player.2");
                PlayerSlotProfile playerThree = CreateProfile(createdProfiles, "QA P3F Player 3", "qa.p3f.player.3");

                string playerOneBefore = EditorJsonUtility.ToJson(playerOne);
                string playerTwoBefore = EditorJsonUtility.ToJson(playerTwo);
                string playerThreeBefore = EditorJsonUtility.ToJson(playerThree);

                object context = CreateContext(
                    new[] { playerOne, playerTwo, playerThree },
                    2,
                    false,
                    out PlayerParticipationOperationResult initialization);
                AssertStatus(initialization, PlayerParticipationOperationStatus.Succeeded, "Valid context initialization failed.");
                AssertNotNull(context, "Valid initialization did not return a context.");
                AssertEqual(3, initialization.Snapshot.ConfiguredSlotCount, "Configured Slot count changed.");
                AssertSame(playerOne, initialization.Snapshot.Slots[0].Profile, "Configured order index 0 changed.");
                AssertSame(playerTwo, initialization.Snapshot.Slots[1].Profile, "Configured order index 1 changed.");
                AssertSame(playerThree, initialization.Snapshot.Slots[2].Profile, "Configured order index 2 changed.");
                completed.Add("ordered-roster-initialized");

                PlayerParticipationOperationResult closedReservation = Reserve(context);
                AssertStatus(
                    closedReservation,
                    PlayerParticipationOperationStatus.RejectedJoiningClosed,
                    "Closed joining accepted a reservation.");
                completed.Add("joining-closed-rejected");

                AssertStatus(
                    InvokeResult(context, "TryOpenJoining", "QA.P3F", "open-joining"),
                    PlayerParticipationOperationStatus.Succeeded,
                    "Opening joining failed.");
                completed.Add("joining-opened");

                PlayerParticipationOperationResult firstReservation = Reserve(context);
                AssertStatus(firstReservation, PlayerParticipationOperationStatus.Succeeded, "First reservation failed.");
                AssertSame(playerOne, firstReservation.Slot.Profile, "First configured Available Slot was not reserved.");
                AssertEqual(PlayerSlotAllocationState.Reserved, firstReservation.Slot.AllocationState, "First Slot is not Reserved.");
                completed.Add("first-available-reserved");

                PlayerParticipationOperationResult secondReservation = Reserve(context);
                AssertStatus(secondReservation, PlayerParticipationOperationStatus.Succeeded, "Second reservation failed.");
                AssertSame(playerTwo, secondReservation.Slot.Profile, "Second configured Available Slot was not reserved.");
                AssertTrue(
                    firstReservation.ReservationToken != secondReservation.ReservationToken,
                    "Two reservations received the same token.");
                completed.Add("ordered-second-slot-reserved");

                PlayerParticipationOperationResult capacityReached = Reserve(context);
                AssertStatus(
                    capacityReached,
                    PlayerParticipationOperationStatus.RejectedCapacityReached,
                    "Reservation exceeded dynamic capacity.");
                completed.Add("reserved-count-consumes-capacity");

                PlayerParticipationOperationResult releaseFirst = InvokeResult(
                    context,
                    "TryReleaseReservation",
                    firstReservation.ReservationToken,
                    "QA.P3F",
                    "release-first");
                AssertStatus(releaseFirst, PlayerParticipationOperationStatus.Succeeded, "Reservation release failed.");
                AssertEqual(PlayerSlotAllocationState.Available, releaseFirst.Slot.AllocationState, "Released Slot did not return to Available.");
                completed.Add("reservation-release-restores-available");

                PlayerParticipationOperationResult staleRelease = InvokeResult(
                    context,
                    "TryReleaseReservation",
                    firstReservation.ReservationToken,
                    "QA.P3F",
                    "stale-release");
                AssertStatus(
                    staleRelease,
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation,
                    "Stale reservation token was accepted.");
                completed.Add("stale-reservation-rejected");

                PlayerParticipationOperationResult replacementReservation = Reserve(context);
                AssertStatus(replacementReservation, PlayerParticipationOperationStatus.Succeeded, "Replacement reservation failed.");
                AssertSame(playerOne, replacementReservation.Slot.Profile, "Released first Slot was not selected again by configured order.");

                PlayerParticipationOperationResult markJoined = InvokeResult(
                    context,
                    "TryMarkJoined",
                    replacementReservation.ReservationToken,
                    "QA.P3F",
                    "mark-joined");
                AssertStatus(markJoined, PlayerParticipationOperationStatus.Succeeded, "Mark Joined failed.");
                AssertEqual(PlayerSlotAllocationState.Joined, markJoined.Slot.AllocationState, "Slot did not become Joined.");
                completed.Add("reserved-slot-marked-joined");

                PlayerParticipationOperationResult reduceCapacity = InvokeResult(
                    context,
                    "TrySetDynamicCapacity",
                    1,
                    "QA.P3F",
                    "reduce-capacity");
                AssertStatus(reduceCapacity, PlayerParticipationOperationStatus.Succeeded, "Capacity reduction failed.");
                AssertTrue(reduceCapacity.Snapshot.IsOverCapacity, "Capacity reduction did not report over-capacity state.");
                AssertEqual(1, reduceCapacity.Snapshot.JoinedCount, "Capacity reduction evicted the Joined Slot.");
                AssertEqual(1, reduceCapacity.Snapshot.ReservedCount, "Capacity reduction released the existing reservation.");
                completed.Add("capacity-reduction-is-non-destructive");

                PlayerParticipationOperationResult releaseSecond = InvokeResult(
                    context,
                    "TryReleaseReservation",
                    secondReservation.ReservationToken,
                    "QA.P3F",
                    "release-second");
                AssertStatus(releaseSecond, PlayerParticipationOperationStatus.Succeeded, "Second reservation release failed.");
                AssertTrue(!releaseSecond.Snapshot.IsOverCapacity, "Context remained over capacity after release.");

                AssertStatus(
                    Reserve(context),
                    PlayerParticipationOperationStatus.RejectedCapacityReached,
                    "Joined Slot did not consume dynamic capacity.");
                completed.Add("joined-slot-consumes-capacity");

                AssertStatus(
                    InvokeResult(context, "TrySetDynamicCapacity", 2, "QA.P3F", "increase-capacity"),
                    PlayerParticipationOperationStatus.Succeeded,
                    "Capacity increase failed.");
                PlayerParticipationOperationResult afterIncrease = Reserve(context);
                AssertStatus(afterIncrease, PlayerParticipationOperationStatus.Succeeded, "Capacity increase did not permit a new reservation.");
                AssertSame(playerTwo, afterIncrease.Slot.Profile, "Capacity increase changed configured allocation order.");
                completed.Add("capacity-increase-allows-reservation");

                object foreignContext = CreateContext(
                    new[] { playerOne },
                    1,
                    true,
                    out PlayerParticipationOperationResult foreignInitialization);
                AssertStatus(foreignInitialization, PlayerParticipationOperationStatus.Succeeded, "Foreign context initialization failed.");
                PlayerParticipationOperationResult foreignReservation = Reserve(foreignContext);
                AssertStatus(foreignReservation, PlayerParticipationOperationStatus.Succeeded, "Foreign context reservation failed.");
                PlayerParticipationOperationResult foreignUse = InvokeResult(
                    context,
                    "TryReleaseReservation",
                    foreignReservation.ReservationToken,
                    "QA.P3F",
                    "foreign-token");
                AssertStatus(
                    foreignUse,
                    PlayerParticipationOperationStatus.RejectedForeignOrStaleReservation,
                    "Foreign Session reservation token was accepted.");
                completed.Add("foreign-reservation-rejected");

                object duplicateReferenceContext = CreateContext(
                    new[] { playerOne, playerOne },
                    2,
                    false,
                    out PlayerParticipationOperationResult duplicateReferenceResult);
                AssertNull(duplicateReferenceContext, "Duplicate Profile reference produced a context.");
                AssertStatus(
                    duplicateReferenceResult,
                    PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                    "Duplicate Profile reference was accepted.");
                completed.Add("duplicate-profile-reference-rejected");

                PlayerSlotProfile duplicateIdentity = CreateProfile(
                    createdProfiles,
                    "QA P3F Duplicate Identity",
                    "qa.p3f.player.1");
                object duplicateIdentityContext = CreateContext(
                    new[] { playerOne, duplicateIdentity },
                    2,
                    false,
                    out PlayerParticipationOperationResult duplicateIdentityResult);
                AssertNull(duplicateIdentityContext, "Duplicate PlayerSlotId produced a context.");
                AssertStatus(
                    duplicateIdentityResult,
                    PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                    "Duplicate PlayerSlotId was accepted.");
                completed.Add("duplicate-slot-identity-rejected");

                object invalidCapacityContext = CreateContext(
                    new[] { playerOne, playerTwo },
                    3,
                    false,
                    out PlayerParticipationOperationResult invalidCapacityResult);
                AssertNull(invalidCapacityContext, "Invalid initial capacity produced a context.");
                AssertStatus(
                    invalidCapacityResult,
                    PlayerParticipationOperationStatus.FailedInvalidConfiguration,
                    "Initial capacity above configured Slot count was accepted.");
                completed.Add("invalid-initial-capacity-rejected");

                AssertEqual(playerOneBefore, EditorJsonUtility.ToJson(playerOne), "Player 1 Profile was mutated by runtime operations.");
                AssertEqual(playerTwoBefore, EditorJsonUtility.ToJson(playerTwo), "Player 2 Profile was mutated by runtime operations.");
                AssertEqual(playerThreeBefore, EditorJsonUtility.ToJson(playerThree), "Player 3 Profile was mutated by runtime operations.");
                completed.Add("profiles-remain-immutable");

                Debug.Log(
                    "[P3F_SESSION_SLOT_RUNTIME_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3F_SESSION_SLOT_RUNTIME_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = 0; index < createdProfiles.Count; index++)
                {
                    if (createdProfiles[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(createdProfiles[index]);
                    }
                }
            }
        }

        private static PlayerSlotProfile CreateProfile(
            ICollection<PlayerSlotProfile> createdProfiles,
            string displayName,
            string slotId)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = slotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            createdProfiles.Add(profile);
            return profile;
        }

        private static object CreateContext(
            IReadOnlyList<PlayerSlotProfile> profiles,
            int capacity,
            bool joiningOpen,
            out PlayerParticipationOperationResult result)
        {
            Type contextType = ResolveContextType();
            MethodInfo method = contextType.GetMethod("TryCreate", StaticInternal);
            AssertNotNull(method, "PlayerParticipationRuntimeContext.TryCreate was not found.");

            object[] arguments =
            {
                profiles,
                capacity,
                joiningOpen,
                "QA.P3F",
                "create-context",
                null
            };

            object returned = method.Invoke(null, arguments);
            result = returned as PlayerParticipationOperationResult;
            AssertNotNull(result, "TryCreate did not return PlayerParticipationOperationResult.");
            return arguments[5];
        }

        private static PlayerParticipationOperationResult Reserve(object context)
        {
            return InvokeResult(context, "TryReserveNextAvailableSlot", "QA.P3F", "reserve-next");
        }

        private static PlayerParticipationOperationResult InvokeResult(
            object context,
            string methodName,
            params object[] arguments)
        {
            AssertNotNull(context, $"Cannot invoke '{methodName}' on a null context.");
            MethodInfo method = context.GetType().GetMethod(methodName, InstanceInternal);
            AssertNotNull(method, $"Runtime method '{methodName}' was not found.");
            object returned = method.Invoke(context, arguments);
            var result = returned as PlayerParticipationOperationResult;
            AssertNotNull(result, $"Runtime method '{methodName}' did not return PlayerParticipationOperationResult.");
            return result;
        }

        private static Type ResolveContextType()
        {
            Type type = typeof(PlayerSlotProfile).Assembly.GetType(ContextTypeName, false);
            AssertNotNull(type, $"Runtime type '{ContextTypeName}' was not found.");
            return type;
        }

        private static void AssertStatus(
            PlayerParticipationOperationResult result,
            PlayerParticipationOperationStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{result.Status}' diagnostics='{result.ToDiagnosticString()}'.");
            }
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
                throw new InvalidOperationException($"{message} expected='{expected}' actual='{actual}'.");
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

        private static void AssertNull(object value, string message)
        {
            if (value != null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

using System;
using System.Threading.Tasks;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// ADR 19.1A regression proving that Session Join truth is observed through
    /// the official Player participation snapshot, not through physical Player
    /// Host or Activity representation evidence.
    /// </summary>
    internal static class QaAdr19SessionParticipationAuthorityRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Session/ADR 19/Run 19.1A Session Participation Authority";
        private const string Source =
            nameof(QaAdr19SessionParticipationAuthorityRegression);

        [MenuItem(MenuPath)]
        private static async void RunFromMenu()
        {
            try
            {
                PlayerSlotId joinedSlotId = await RunAsync();
                Debug.Log(
                    "[ADR19_1A_SESSION_PARTICIPATION_AUTHORITY] status='Passed' " +
                    $"slot='{joinedSlotId.StableText}' authority='PlayerParticipationSnapshot' " +
                    "proof='official Join is reflected as Joined Session participation truth'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[ADR19_1A_SESSION_PARTICIPATION_AUTHORITY] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        internal static async Task<PlayerSlotId> RunAsync()
        {
            Require(
                EditorApplication.isPlaying,
                "ADR 19.1A Session Participation Authority regression must run in Play Mode.");

            QaPlayerGameplayAdmissionFixture fixture = null;
            LocalPlayerProvisioningAuthoring provisioning = null;
            bool joiningStateCaptured = false;
            bool joiningWasOpen = false;
            Exception failure = null;
            PlayerSlotId joinedSlotId = default;

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                provisioning = fixture.ProvisioningAuthoring;

                Require(
                    provisioning != null && provisioning.RuntimeReady,
                    "ADR 19.1A requires the official Local Player provisioning runtime to be ready.");

                PlayerParticipationSnapshot before = provisioning.RuntimeSnapshot;
                RequireParticipationSnapshot(before, "before join");
                Require(
                    before.AvailableCount > 0,
                    "ADR 19.1A requires at least one Available Session Player Slot before the proof join.");

                int joinedBefore = before.JoinedCount;
                int availableBefore = before.AvailableCount;
                int revisionBefore = before.Revision;
                joiningWasOpen = before.JoiningOpen;
                joiningStateCaptured = true;

                if (!joiningWasOpen)
                {
                    PlayerParticipationOperationResult opened = provisioning.OpenJoining(
                        Source,
                        "adr19-1a-session-participation-authority");
                    Require(
                        opened != null && opened.Completed,
                        "ADR 19.1A could not explicitly open Session joining. " +
                        opened?.ToDiagnosticString());
                    Require(
                        provisioning.RuntimeSnapshot.JoiningOpen,
                        "ADR 19.1A opened joining but the canonical participation snapshot did not report JoiningOpen.");
                }

                LocalPlayerJoinResult joined = fixture.JoinPlayer(
                    "adr19-1a-session-participation-authority");
                Require(
                    joined != null && joined.Succeeded,
                    "ADR 19.1A official Join failed. " + joined?.ToDiagnosticString());
                Require(
                    joined.Slot.IsJoined,
                    "ADR 19.1A Join result did not report its committed Slot as Joined.");
                Require(
                    joined.Slot.PlayerSlotId.IsValid,
                    "ADR 19.1A Join result returned an invalid PlayerSlotId.");

                joinedSlotId = joined.Slot.PlayerSlotId;

                PlayerParticipationSnapshot after = provisioning.RuntimeSnapshot;
                RequireParticipationSnapshot(after, "after join");
                Require(
                    after.JoinedCount == joinedBefore + 1,
                    $"Canonical Session participation did not gain exactly one Joined Slot. before='{joinedBefore}' after='{after.JoinedCount}'.");
                Require(
                    after.AvailableCount == availableBefore - 1,
                    $"Canonical Session participation did not consume exactly one Available Slot. before='{availableBefore}' after='{after.AvailableCount}'.");
                Require(
                    after.Revision > revisionBefore,
                    $"Canonical Session participation revision did not advance. before='{revisionBefore}' after='{after.Revision}'.");

                PlayerSlotRuntimeSnapshot authoritativeSlot =
                    FindExactlyOneSlot(after, joinedSlotId);
                Require(
                    authoritativeSlot.IsJoined,
                    $"Canonical Session participation Slot '{joinedSlotId.StableText}' is not Joined after the official Join.");
                Require(
                    authoritativeSlot.AllocationState == PlayerSlotAllocationState.Joined,
                    $"Canonical Session participation Slot '{joinedSlotId.StableText}' has allocation state '{authoritativeSlot.AllocationState}' instead of Joined.");
                Require(
                    authoritativeSlot.Revision >= joined.Slot.Revision,
                    $"Canonical Session participation Slot '{joinedSlotId.StableText}' is older than the committed Join result. snapshotRevision='{authoritativeSlot.Revision}' joinRevision='{joined.Slot.Revision}'.");

            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (fixture != null)
                {
                    await fixture.CleanupAsync();
                    if (fixture.CleanupFailure != null)
                    {
                        failure = CombineFailures(
                            failure,
                            new InvalidOperationException(
                                "ADR 19.1A fixture cleanup failed.",
                                fixture.CleanupFailure));
                    }
                }

                if (joiningStateCaptured &&
                    provisioning != null &&
                    provisioning.RuntimeReady &&
                    provisioning.RuntimeSnapshot.JoiningOpen != joiningWasOpen)
                {
                    PlayerParticipationOperationResult restored = joiningWasOpen
                        ? provisioning.OpenJoining(Source, "adr19-1a-restore-joining-state")
                        : provisioning.CloseJoining(Source, "adr19-1a-restore-joining-state");

                    if (restored == null ||
                        !restored.Completed ||
                        provisioning.RuntimeSnapshot.JoiningOpen != joiningWasOpen)
                    {
                        failure = CombineFailures(
                            failure,
                            new InvalidOperationException(
                                "ADR 19.1A could not restore the original Session joining state. " +
                                restored?.ToDiagnosticString()));
                    }
                }
            }

            if (failure != null)
            {
                throw failure;
            }

            Require(
                joinedSlotId.IsValid,
                "ADR 19.1A ended without producing a valid Joined PlayerSlotId.");
            return joinedSlotId;
        }

        private static PlayerSlotRuntimeSnapshot FindExactlyOneSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId slotId)
        {
            int matches = 0;
            PlayerSlotRuntimeSnapshot result = default;

            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot candidate = snapshot.Slots[index];
                if (candidate.PlayerSlotId == slotId)
                {
                    matches++;
                    result = candidate;
                }
            }

            Require(
                matches == 1,
                $"Canonical Session participation must contain exactly one Slot '{slotId.StableText}', actual='{matches}'.");
            return result;
        }

        private static void RequireParticipationSnapshot(
            PlayerParticipationSnapshot snapshot,
            string phase)
        {
            Require(
                snapshot != null && snapshot.IsInitialized,
                $"ADR 19.1A requires an initialized canonical Player participation snapshot {phase}.");
            Require(
                snapshot.ConfiguredSlotCount > 0,
                $"ADR 19.1A requires configured Session Player Slots {phase}.");
        }

        private static Exception CombineFailures(
            Exception primary,
            Exception secondary)
        {
            if (primary == null)
            {
                return secondary;
            }

            if (secondary == null)
            {
                return primary;
            }

            return new AggregateException(primary, secondary);
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
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "''");
        }
    }
}

using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEngine.InputSystem;

namespace Immersive.QaFramework.PlayerSession
{
    internal readonly struct QaPlayerSessionJoinEvidence
    {
        internal QaPlayerSessionJoinEvidence(
            PlayerSlotRuntimeSnapshot slot,
            PlayerInput playerInput,
            LocalPlayerHostAuthoring host)
        {
            Slot = slot;
            PlayerInput = playerInput;
            Host = host;
        }

        internal PlayerSlotRuntimeSnapshot Slot { get; }
        internal PlayerInput PlayerInput { get; }
        internal LocalPlayerHostAuthoring Host { get; }
    }

    internal static class QaPlayerSessionJoinEvidenceReader
    {
        internal static bool TryCapture(
            PlayerSessionObserver observer,
            PlayerSlotId expectedSlotId,
            InputDevice requestedDevice,
            out QaPlayerSessionJoinEvidence evidence,
            out string issue)
        {
            evidence = default;
            issue = string.Empty;
            if (observer == null || requestedDevice == null || !requestedDevice.added ||
                !expectedSlotId.IsValid)
            {
                issue = "Observer, expected Slot identity or requested InputDevice is unavailable.";
                return false;
            }

            if (!observer.TryGetObservation(
                    out PlayerSessionScopedObservationSnapshot observation) ||
                observation == null || !observation.IsAvailable)
            {
                issue = observer.Diagnostic;
                return false;
            }

            PlayerSessionScopedSlotObservation matchedSlot = default;
            int slotMatches = 0;
            int deviceOwnershipMatches = 0;
            for (int slotIndex = 0; slotIndex < observation.Slots.Count; slotIndex++)
            {
                PlayerSessionScopedSlotObservation candidate = observation.Slots[slotIndex];
                bool ownsRequestedDevice = candidate.HasInputOwnershipEvidence &&
                    OwnsDevice(candidate.InputOwnership, requestedDevice);
                if (ownsRequestedDevice)
                {
                    if (candidate.InputOwnership.Devices.Count != 1)
                    {
                        issue = "The joined Slot owns devices beyond the exact requested InputDevice.";
                        return false;
                    }

                    deviceOwnershipMatches++;
                    if (candidate.Slot.PlayerSlotId != expectedSlotId || !candidate.IsJoined)
                    {
                        issue = "The requested InputDevice is observed on a different or non-joined Slot.";
                        return false;
                    }

                    matchedSlot = candidate;
                    slotMatches++;
                }
            }

            if (deviceOwnershipMatches != 1 || slotMatches != 1 ||
                matchedSlot.InputOwnership.UnityPlayerIndex < 0)
            {
                issue = "Public Session evidence did not identify exactly one joined expected Slot owning the requested device.";
                return false;
            }

            PlayerInput matchedInput = null;
            int inputMatches = 0;
            for (int inputIndex = 0; inputIndex < PlayerInput.all.Count; inputIndex++)
            {
                PlayerInput candidate = PlayerInput.all[inputIndex];
                if (candidate == null ||
                    candidate.playerIndex != matchedSlot.InputOwnership.UnityPlayerIndex ||
                    candidate.devices.Count != 1 ||
                    !OwnsDevice(candidate, requestedDevice))
                {
                    continue;
                }

                matchedInput = candidate;
                inputMatches++;
            }

            if (inputMatches != 1)
            {
                issue = "Unity PlayerInput registry did not resolve exactly one instance from the observed playerIndex and exact paired device.";
                return false;
            }

            LocalPlayerHostAuthoring host = matchedInput.GetComponent<LocalPlayerHostAuthoring>();
            if (host == null || !object.ReferenceEquals(host.PlayerInput, matchedInput) || !host.IsJoined)
            {
                issue = "The correlated PlayerInput did not expose its joined LocalPlayerHostAuthoring on the same GameObject.";
                return false;
            }

            evidence = new QaPlayerSessionJoinEvidence(matchedSlot.Slot, matchedInput, host);
            return true;
        }

        private static bool OwnsDevice(
            LocalPlayerInputOwnershipSummary ownership,
            InputDevice requestedDevice)
        {
            for (int index = 0; index < ownership.Devices.Count; index++)
            {
                if (ownership.Devices[index].DeviceId == requestedDevice.deviceId)
                    return true;
            }

            return false;
        }

        private static bool OwnsDevice(PlayerInput playerInput, InputDevice requestedDevice)
        {
            for (int index = 0; index < playerInput.devices.Count; index++)
            {
                if (ReferenceEquals(playerInput.devices[index], requestedDevice))
                    return true;
            }

            return false;
        }
    }
}

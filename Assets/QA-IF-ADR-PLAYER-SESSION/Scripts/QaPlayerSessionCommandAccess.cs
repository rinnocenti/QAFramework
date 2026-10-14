using System.Linq;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;

namespace Immersive.QaFramework.PlayerSession
{
    internal static class QaPlayerSessionCommandAccess
    {
        internal static PlayerParticipationOperationResult OpenJoining(
            PlayerSessionObserver observer)
        {
            PlayerSessionOpenJoiningCommandTrigger trigger = FindOpenJoining(observer);
            if (trigger == null)
                return null;

            trigger.Invoke();
            return trigger.LastOpenJoiningResult;
        }

        internal static PlayerParticipationOperationResult CloseJoining(
            PlayerSessionObserver observer)
        {
            PlayerSessionCloseJoiningCommandTrigger trigger = FindCloseJoining(observer);
            if (trigger == null)
                return null;

            trigger.Invoke();
            return trigger.LastCloseJoiningResult;
        }

        internal static SessionPlayerLeaveResult Leave(
            PlayerSessionObserver observer,
            PlayerSlotProfile slot,
            int expectedOccurrenceRevision = -1)
        {
            if (slot == null || !slot.TryGetPlayerSlotId(out PlayerSlotId slotId, out _))
                return null;

            return Leave(observer, slotId, expectedOccurrenceRevision);
        }

        internal static SessionPlayerLeaveResult Leave(
            PlayerSessionObserver observer,
            PlayerSlotId slotId,
            int expectedOccurrenceRevision = -1)
        {
            PlayerSessionLeaveCommandTrigger trigger = FindLeave(
                observer,
                slotId,
                expectedOccurrenceRevision);
            if (trigger == null)
                return null;

            trigger.Invoke();
            return trigger.LastLeaveResult;
        }

        internal static PlayerGameplayAvailabilityBlockTrigger[] AvailabilityBlocks(
            PlayerSessionObserver observer,
            PlayerSlotProfile slot)
        {
            if (slot == null)
                return System.Array.Empty<PlayerGameplayAvailabilityBlockTrigger>();

            return Find<PlayerGameplayAvailabilityBlockTrigger>(observer)
                .Where(candidate => candidate.PlayerSlot == slot)
                .ToArray();
        }

        internal static PlayerSessionOpenJoiningCommandTrigger FindOpenJoining(
            PlayerSessionObserver observer) =>
            Find<PlayerSessionOpenJoiningCommandTrigger>(observer).FirstOrDefault();

        internal static PlayerSessionCloseJoiningCommandTrigger FindCloseJoining(
            PlayerSessionObserver observer) =>
            Find<PlayerSessionCloseJoiningCommandTrigger>(observer).FirstOrDefault();

        internal static PlayerSessionLeaveCommandTrigger FindLeave(
            PlayerSessionObserver observer,
            PlayerSlotProfile slot,
            int expectedOccurrenceRevision = -1) =>
            slot != null && slot.TryGetPlayerSlotId(out PlayerSlotId slotId, out _)
                ? FindLeave(observer, slotId, expectedOccurrenceRevision)
                : null;

        internal static PlayerSessionLeaveCommandTrigger FindLeave(
            PlayerSessionObserver observer,
            PlayerSlotId slotId,
            int expectedOccurrenceRevision = -1) =>
            Find<PlayerSessionLeaveCommandTrigger>(observer)
                .FirstOrDefault(candidate => candidate.PlayerSlot != null &&
                    candidate.PlayerSlot.TryGetPlayerSlotId(out PlayerSlotId candidateSlotId, out _) &&
                    candidateSlotId.Equals(slotId) &&
                    candidate.ExpectedLeaveOccurrenceRevision == expectedOccurrenceRevision);

        private static T[] Find<T>(PlayerSessionObserver observer)
            where T : UnityEngine.Component
        {
            return observer != null
                ? observer.GetComponentsInChildren<T>(true)
                : System.Array.Empty<T>();
        }
    }
}

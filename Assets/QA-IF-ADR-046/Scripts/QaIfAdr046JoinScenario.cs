using System.Collections;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.QaFramework.Certification;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.QaFramework.IfAdr046
{
    /// <summary>First integrated I4 vertical slice: public Activity-scoped Join and cleanup.</summary>
    [DisallowMultipleComponent]
    public sealed class QaIfAdr046JoinScenario : MonoBehaviour
    {
        private const string ScenarioId = "IF-ADR-046-I4-MINIMAL";

        [SerializeField] private PlayerSessionObserver observer;
        [SerializeField] private PlayerSessionJoinTrigger joinTrigger;
        [SerializeField, Min(1)] private int frameBudget = 600;

        private Keyboard _keyboard;
        private IPlayerSessionScopedAccess _access;
        private int _initialPlayerInputCount;
        private string _firstCause = string.Empty;
        private bool _started;
        private bool _terminal;
        private bool _caseAttempted;
        private string _actorPrepared = "not-observed";
        private string _gameplayAdmitted = "not-observed";

        private void Start()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            if (observer == null || joinTrigger == null ||
                joinTrigger.Scope != LocalPlayerProvisioningConsumerScope.Activity)
            {
                _firstCause = "BLOCKED: fixture requires one Activity-scoped observer and join trigger.";
                yield return Finish();
                yield break;
            }

            for (int i = 0; i < Mathf.Max(1, frameBudget); i++)
            {
                if (observer.TryGetSnapshot(out PlayerParticipationSnapshot ready) &&
                    ready.IsInitialized && ready.JoinedCount == 0 && ready.ReservedCount == 0 &&
                    !ready.JoiningOpen && observer.TryGetAccess(out _access, out _))
                    break;
                yield return null;
            }

            if (_access == null || !TryGetEmptyClosedSession(out PlayerParticipationSnapshot baseline))
            {
                _firstCause = "BLOCKED: known initialized, empty, Joining-closed Session baseline was unavailable.";
                yield return Finish();
                yield break;
            }

            _initialPlayerInputCount = PlayerInput.all.Count;
            PlayerParticipationOperationResult opened = _access.OpenJoining(
                ScenarioId, "Open Joining for the I4 public boundary proof.");
            if (opened == null || !opened.Succeeded)
            {
                _firstCause = "Public OpenJoining failed: " + (opened != null ? opened.ToDiagnosticString() : "null result");
                yield return Finish();
                yield break;
            }

            string deviceIssue = string.Empty;
            try
            {
                _keyboard = InputSystem.AddDevice<Keyboard>("QA IF-ADR-046 I4");
            }
            catch (System.Exception exception)
            {
                deviceIssue = exception.Message;
            }
            if (!string.IsNullOrEmpty(deviceIssue))
            {
                _firstCause = "BLOCKED: could not create the owned synthetic Keyboard: " + deviceIssue;
                yield return Finish();
                yield break;
            }

            if (_keyboard == null || !_keyboard.added)
            {
                _firstCause = "BLOCKED: Input System did not add the synthetic Keyboard.";
                yield return Finish();
                yield break;
            }

            _caseAttempted = true;
            LocalPlayerJoinOutcome outcome = joinTrigger.RequestJoin(
                new LocalPlayerJoinRequest(ScenarioId, "I4 Activity-scoped Join", _keyboard, "QA.Keyboard"));
            if (outcome == null || outcome.Code != LocalPlayerJoinOutcomeCode.Joined ||
                outcome.AdmissionState != LocalPlayerJoinAdmissionState.Admitted)
            {
                _firstCause = "Public Activity Join returned " +
                    (outcome != null ? outcome.Code + "/" + outcome.AdmissionState : "null outcome") + ".";
                yield return Finish();
                yield break;
            }

            if (!observer.TryGetSnapshot(out PlayerParticipationSnapshot joined) ||
                joined.Revision <= baseline.Revision || joined.JoinedCount != 1 ||
                joined.ReservedCount != 0 || joined.Slots.Count != 1 || !joined.Slots[0].IsJoined ||
                PlayerInput.all.Count != _initialPlayerInputCount + 1 ||
                !observer.TryGetObservation(out PlayerSessionScopedObservationSnapshot joinedObservation) ||
                joinedObservation.Slots.Count != 1 || !joinedObservation.Slots[0].HasHostEvidence ||
                !joinedObservation.Slots[0].HostEvidence.IsConfirmed ||
                joinedObservation.Slots[0].HostEvidence.PhysicalProvisioningMode != PlayerHostProvisioningMode.ManagerProvisioned)
            {
                _firstCause = "Join outcome did not correlate with exactly one Session Slot and one materialized PlayerInput.";
                yield return Finish();
                yield break;
            }
            _actorPrepared = joinedObservation.Slots[0].IsLogicalActorPrepared.ToString();
            _gameplayAdmitted = joinedObservation.Slots[0].IsGameplayAdmitted.ToString();

            // A typed Join proves Session admission only. Actor/gameplay readiness is reported separately.
            var slotId = joined.Slots[0].PlayerSlotId;
            int slotRevision = joined.Slots[0].Revision;
            SessionPlayerLeaveResult leave = _access.RequestLeave(new SessionPlayerLeaveRequest(
                slotId, slotRevision, ScenarioId, "Restore the I4 empty Session baseline."));
            if (!IsLeaveConfirmed(leave))
                AppendCleanupIssue("Public Leave did not confirm Slot/Host release: " + (leave != null ? leave.ToDiagnosticString() : "null result"));

            for (int i = 0; i < Mathf.Max(1, frameBudget); i++)
            {
                if (observer.TryGetSnapshot(out PlayerParticipationSnapshot afterLeave) &&
                    afterLeave.JoinedCount == 0 && afterLeave.ReservedCount == 0 && afterLeave.LeavingCount == 0)
                    break;
                yield return null;
            }

            if (_access != null && observer.TryGetSnapshot(out PlayerParticipationSnapshot beforeClose) &&
                beforeClose.JoinedCount == 0 && beforeClose.ReservedCount == 0 && beforeClose.LeavingCount == 0 &&
                beforeClose.JoiningOpen)
            {
                PlayerParticipationOperationResult closed = _access.CloseJoining(
                    ScenarioId, "Restore Joining-closed baseline after Leave.");
                if (closed == null || !closed.Succeeded)
                    AppendCleanupIssue("CloseJoining failed: " + (closed != null ? closed.ToDiagnosticString() : "null result"));
            }

            yield return Finish();
        }

        private IEnumerator Finish()
        {
            if (_access != null && TryGetEmptyClosedSession(out _) == false &&
                observer != null && observer.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) &&
                snapshot.Slots.Count == 1 && snapshot.Slots[0].IsJoined)
            {
                SessionPlayerLeaveResult leave = _access.RequestLeave(new SessionPlayerLeaveRequest(
                    snapshot.Slots[0].PlayerSlotId, snapshot.Slots[0].Revision,
                    ScenarioId, "Best-effort owned Player cleanup."));
                if (!IsLeaveConfirmed(leave))
                    AppendCleanupIssue("Leave request did not confirm Slot/Host release: " + (leave != null ? leave.ToDiagnosticString() : "null"));
            }

            if (_access != null && observer != null && observer.TryGetSnapshot(out PlayerParticipationSnapshot beforeClose) &&
                beforeClose.JoinedCount == 0 && beforeClose.ReservedCount == 0 && beforeClose.LeavingCount == 0 &&
                beforeClose.JoiningOpen)
            {
                PlayerParticipationOperationResult closed = _access.CloseJoining(
                    ScenarioId, "Restore Joining-closed baseline during cleanup.");
                if (closed == null || !closed.Succeeded)
                    AppendCleanupIssue("CloseJoining failed: " + (closed != null ? closed.ToDiagnosticString() : "null result"));
            }

            if (_keyboard != null && _keyboard.added)
            {
                try { InputSystem.RemoveDevice(_keyboard); }
                catch (System.Exception exception) { AppendCleanupIssue("Synthetic device removal failed: " + exception.Message); }
            }

            bool restored = false;
            for (int i = 0; i < Mathf.Max(1, frameBudget); i++)
            {
                if (observer != null && observer.TryGetSnapshot(out PlayerParticipationSnapshot final) &&
                    final.IsInitialized && final.JoinedCount == 0 && final.ReservedCount == 0 &&
                    final.LeavingCount == 0 && final.AvailableCount == 1 && !final.JoiningOpen &&
                    PlayerInput.all.Count == _initialPlayerInputCount &&
                    (_keyboard == null || !_keyboard.added) &&
                    observer.TryGetObservation(out PlayerSessionScopedObservationSnapshot finalObservation) &&
                    finalObservation.Slots.Count == 1 && !finalObservation.Slots[0].HasHostEvidence)
                {
                    restored = true;
                    break;
                }
                yield return null;
            }

            if (!restored && string.IsNullOrEmpty(_cleanupIssue))
                _cleanupIssue = "Session, PlayerInput, synthetic device, or public Host evidence did not return to the captured baseline.";

            var recorder = new QaCertificationRecorder();
            if (_firstCause.StartsWith("BLOCKED:"))
                recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, _firstCause);
            else if (!string.IsNullOrEmpty(_firstCause))
                recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, _firstCause);
            else if (!restored || !string.IsNullOrEmpty(_cleanupIssue))
                recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Fail,
                    "Cleanup failed to restore the declared baseline.");
            else
                recorder.RecordPass();

            QaCertificationResult terminal = recorder.CreateResult(
                ScenarioId,
                restored && string.IsNullOrEmpty(_cleanupIssue)
                    ? QaCleanupDisposition.BaselineRestored
                    : QaCleanupDisposition.FreshBootRequired,
                _cleanupIssue);
            string terminalVerdict = terminal.Verdict switch
            {
                QaCertificationVerdict.Pass => "PASS",
                QaCertificationVerdict.Fail => "FAIL",
                QaCertificationVerdict.Blocked => "BLOCKED",
                _ => "FAIL"
            };
            string finalSession = observer != null && observer.TryGetSnapshot(out PlayerParticipationSnapshot finalState)
                ? $"revision:{finalState.Revision},available:{finalState.AvailableCount},reserved:{finalState.ReservedCount},joined:{finalState.JoinedCount},leaving:{finalState.LeavingCount},joiningOpen:{finalState.JoiningOpen}"
                : "unobserved";
            Debug.Log($"[QA-IF-ADR-046-I4] scenario='{terminal.ScenarioId}' scope='Activity' owner='QA IF-ADR-046 Activity A' cases='{(_caseAttempted ? "1/1" : "0/1")}' verdict='{terminalVerdict}' firstCause='{terminal.FirstCausalDivergence}' outcome='{(joinTrigger != null && joinTrigger.LastOutcome != null ? joinTrigger.LastOutcome.Code.ToString() : "not-observed")}' actorPrepared='{_actorPrepared}' gameplayAdmitted='{_gameplayAdmitted}' sessionFinal='{finalSession}' sessionRestored='{restored}' playerInputBaseline='{_initialPlayerInputCount}' playerInputFinal='{PlayerInput.all.Count}' keyboardRemoved='{(_keyboard == null || !_keyboard.added)}' cleanup='{terminal.CleanupDisposition}' cleanupIssue='{terminal.CleanupIssue}'");
            _terminal = true;
        }

        private bool TryGetEmptyClosedSession(out PlayerParticipationSnapshot snapshot)
        {
            snapshot = null;
            return observer != null && observer.TryGetSnapshot(out snapshot) &&
                snapshot.IsInitialized && snapshot.Slots.Count == 1 && snapshot.AvailableCount == 1 &&
                snapshot.JoinedCount == 0 && snapshot.ReservedCount == 0 && snapshot.LeavingCount == 0 &&
                !snapshot.JoiningOpen;
        }

        private static bool IsLeaveConfirmed(SessionPlayerLeaveResult result) =>
            result != null && result.Succeeded && result.ProvisioningReleased && result.TerminalCommitted;

        private string _cleanupIssue = string.Empty;

        private void AppendCleanupIssue(string issue)
        {
            _cleanupIssue = string.IsNullOrEmpty(_cleanupIssue) ? issue : _cleanupIssue + " | " + issue;
        }

        private void OnDisable()
        {
            if (_started && !_terminal)
                Debug.LogError($"[QA-IF-ADR-046-I4] scenario='{ScenarioId}' verdict='BLOCKED' firstCause='Scenario component disabled before terminal cleanup.' cleanup='FreshBootRequired'");
        }
    }
}

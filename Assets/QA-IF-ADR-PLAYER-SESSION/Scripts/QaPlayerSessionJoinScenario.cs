using System;
using System.Collections;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.QaFramework.Certification;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.QaFramework.PlayerSession
{
    [DisallowMultipleComponent]
    public sealed class QaPlayerSessionJoinScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-PLAYER-SESSION-JOIN";
        private const int ExpectedCases = 8;
        [SerializeField] private PlayerSessionObserver sessionObserver;
        [SerializeField] private PlayerSessionJoinTrigger joinTrigger;
        [SerializeField] private PlayerSlotProfile expectedSlot;
        [SerializeField] private ActorProfile expectedDefaultActor;
        [SerializeField, Min(1)] private int frameBudget = 600;

        private readonly QaCertificationRecorder _recorder = new QaCertificationRecorder();
        private IPlayerSessionScopedAccess _access;
        private Keyboard _keyboard;
        private PlayerInput _joinedInput;
        private LocalPlayerHostAuthoring _joinedHost;
        private int _completed;
        private string _firstIssue = string.Empty;
        private string _cleanupIssue = string.Empty;
        private string _stage = "baseline";
        private QaCleanupDisposition _cleanup = QaCleanupDisposition.FreshBootRequired;
        private bool _joiningOpened;
        private bool _terminal;
        private bool _started;
        private bool _actorSelected;
        private bool _actorPrepared;
        private bool _actorMaterialized;
        private bool _gameplayAdmitted;
        private string _invalidJoinStatus = "not-run";
        private bool _invalidRevisionPreserved;
        private string _joinStatus = "not-run";
        private bool _joinSlotMatched;
        private string _leaveStatus = "not-run";
        private bool _leaveProvisioningReleased;
        private bool _leaveTerminalCommitted;
        private string _finalSessionState = "not-observed";

        private void Start()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (!_started || _terminal) return;
            RecordBlocked("Scenario interrupted before its owned lifecycle completed; use a fresh Play Mode boot.");
            TrySynchronousLeave();
            RemoveKeyboard();
            _cleanup = QaCleanupDisposition.FreshBootRequired;
            SetCleanupIssue("Interrupted coroutine cannot confirm asynchronous Activity/Actor restoration.");
            PublishTerminal();
        }

        private IEnumerator Run()
        {
            yield return WaitForBaseline();
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }

            _stage = "invalid-device-rejection";
            int revision = sessionObserver.CurrentObservation.SessionRevision;
            joinTrigger.InvokeFromDevice(null);
            LocalPlayerJoinOutcome invalid = joinTrigger.LastOutcome;
            _invalidJoinStatus = invalid != null ? invalid.Code.ToString() : "null";
            _invalidRevisionPreserved = sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot afterInvalid) &&
                afterInvalid.Revision == revision && afterInvalid.JoinedCount == 0 && !afterInvalid.JoiningOpen;
            if (invalid == null || invalid.Code != LocalPlayerJoinOutcomeCode.RejectedInvalidRequest ||
                !_invalidRevisionPreserved)
            {
                RecordFail("Null-device Join did not return the exact public RejectedInvalidRequest while preserving the empty closed Session.");
                yield return CleanupAndPublish(); yield break;
            }
            _completed++;

            _stage = "open-joining";
            PlayerParticipationOperationResult opened = _access.OpenJoining(ScenarioId, "Open Joining for explicit P1 Join.");
            if (opened == null || !opened.Succeeded)
            {
                RecordFail("Public OpenJoining failed from a valid baseline. " + (opened != null ? opened.ToDiagnosticString() : "null result"));
                yield return CleanupAndPublish(); yield break;
            }
            _joiningOpened = true;
            _completed++;

            _stage = "join-p1";
            string deviceIssue = string.Empty;
            try { _keyboard = InputSystem.AddDevice<Keyboard>("QA Player Session P1"); }
            catch (Exception exception)
            {
                deviceIssue = exception.Message;
            }
            if (!string.IsNullOrEmpty(deviceIssue))
            {
                RecordBlocked("Could not add the QA-owned synthetic Keyboard: " + deviceIssue);
                yield return CleanupAndPublish(); yield break;
            }
            if (_keyboard == null || !_keyboard.added)
            {
                RecordBlocked("Input System did not add the QA-owned synthetic Keyboard.");
                yield return CleanupAndPublish(); yield break;
            }
            LocalPlayerJoinOutcome join = joinTrigger.RequestJoin(
                new LocalPlayerJoinRequest(ScenarioId, "join-p1", _keyboard));
            _joinStatus = join != null ? join.Code.ToString() : "null";
            if (join == null || join.Code != LocalPlayerJoinOutcomeCode.Joined ||
                join.AdmissionState != LocalPlayerJoinAdmissionState.Admitted)
            {
                string diagnostic = join != null
                    ? $"code='{join.Code}' admission='{join.AdmissionState}'"
                    : "null result";
                if (join == null || join.Code is LocalPlayerJoinOutcomeCode.RejectedUnavailable or
                    LocalPlayerJoinOutcomeCode.RejectedConfiguration)
                    RecordBlocked("Valid Join could not be exercised because runtime/manager preconditions were unavailable. " + diagnostic);
                else RecordFail("Valid P1 Join returned an unsuccessful typed outcome. " + diagnostic);
                yield return CleanupAndPublish(); yield break;
            }

            bool physicalJoinCorrelated = false;
            string physicalJoinIssue = string.Empty;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (QaPlayerSessionJoinEvidenceReader.TryCapture(
                        sessionObserver,
                        expectedSlot.PlayerSlotId,
                        _keyboard,
                        out QaPlayerSessionJoinEvidence joinEvidence,
                        out physicalJoinIssue))
                {
                    _joinedInput = joinEvidence.PlayerInput;
                    _joinedHost = joinEvidence.Host;
                    _joinSlotMatched = joinEvidence.Slot.IsJoined &&
                        joinEvidence.Slot.PlayerSlotId == expectedSlot.PlayerSlotId;
                    physicalJoinCorrelated = _joinSlotMatched;
                    break;
                }
                yield return null;
            }
            if (!physicalJoinCorrelated)
            {
                RecordFail("Joined outcome did not correlate the expected public Slot, PlayerInput, exact device and technical Host. " + physicalJoinIssue);
                yield return CleanupAndPublish(); yield break;
            }
            _completed++;

            _stage = "actor-default-and-readiness";
            string observationIssue = string.Empty;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (TryGetP1(out PlayerSessionScopedSlotObservation slot, out observationIssue))
                {
                    _actorSelected = slot.HasSelectedActor && slot.Slot.SelectedActorProfile == expectedDefaultActor;
                    _actorPrepared = slot.IsLogicalActorPrepared;
                    _actorMaterialized = slot.IsPhysicallyMaterialized;
                    _gameplayAdmitted = slot.IsGameplayAdmitted;
                    if (_actorSelected && _actorPrepared) break;
                }
                yield return null;
            }
            if (!_actorSelected)
            {
                RecordFail("ResolveConfiguredDefault did not select P1's authored DefaultActorProfile. " + observationIssue);
                yield return CleanupAndPublish(); yield break;
            }
            _completed++;
            if (!_actorPrepared)
            {
                RecordFail("Default Actor selection was observed, but Activity did not reach LogicalActorsPrepared. " + observationIssue);
                yield return CleanupAndPublish(); yield break;
            }
            if (!_actorMaterialized)
            {
                RecordFail("Logical Actor preparation completed without the physical Actor materialization expected by the authored Host pipeline. " + observationIssue);
                yield return CleanupAndPublish(); yield break;
            }
            if (_gameplayAdmitted)
            {
                RecordFail("Activity requires LogicalActorsPrepared; unexpected Gameplay Admission was observed.");
                yield return CleanupAndPublish(); yield break;
            }
            _completed++;

            _stage = "cleanup";
            yield return CleanupAndPublish();
        }

        private IEnumerator WaitForBaseline()
        {
            _stage = "baseline";
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (sessionObserver != null && joinTrigger != null && expectedSlot != null && expectedDefaultActor != null &&
                    sessionObserver.IsAvailable && sessionObserver.TryGetAccess(out _access, out _) && _access != null &&
                    sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) && snapshot.IsInitialized &&
                    snapshot.ConfiguredSlotCount == 1 && snapshot.Slots.Count == 1 && snapshot.Slots[0].Profile == expectedSlot &&
                    snapshot.Slots[0].AllocationState == PlayerSlotAllocationState.Available && !snapshot.JoiningOpen && snapshot.JoinedCount == 0 &&
                    expectedSlot.DefaultActorProfile == expectedDefaultActor &&
                    snapshot.HasActorSelectionPolicy &&
                    sessionObserver.CurrentObservation.InitializationConfiguration != null &&
                    sessionObserver.CurrentObservation.InitializationConfiguration.ActorResolutionPolicy == PlayerActorResolutionPolicy.ResolveConfiguredDefault &&
                    sessionObserver.Scope == LocalPlayerProvisioningConsumerScope.Activity &&
                    joinTrigger.Scope == LocalPlayerProvisioningConsumerScope.Activity)
                {
                    _completed++;
                    yield break;
                }
                yield return null;
            }
            RecordBlocked("Valid Activity-scoped Session baseline or public Join trigger was unavailable. Observer='" +
                (sessionObserver != null ? sessionObserver.ScopedAccessDiagnostic : "missing observer") + "'.");
        }

        private bool TryGetP1(out PlayerSessionScopedSlotObservation slot, out string issue)
        {
            slot = default;
            issue = string.Empty;
            if (sessionObserver == null || !sessionObserver.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) ||
                observation == null || !observation.IsAvailable)
            {
                issue = sessionObserver != null ? sessionObserver.Diagnostic : "Session Observer is missing.";
                return false;
            }
            PlayerSessionScopedSlotObservation[] matches = observation.Slots
                .Where(candidate => candidate.Slot.PlayerSlotId == expectedSlot.PlayerSlotId).ToArray();
            if (matches.Length != 1)
            {
                issue = "Expected exactly one P1 observation; found '" + matches.Length + "'.";
                return false;
            }
            slot = matches[0];
            if (!slot.IsJoined)
            {
                issue = "P1 observation has allocation='" + slot.Slot.AllocationState + "' selectedActor='" +
                    slot.HasSelectedActor + "' logicalPrepared='" + slot.IsLogicalActorPrepared +
                    "' physicallyMaterialized='" + slot.IsPhysicallyMaterialized + "'.";
                return false;
            }
            return true;
        }

        private IEnumerator CleanupAndPublish()
        {
            bool leaveConfirmed = false;
            if (_access == null && sessionObserver != null) sessionObserver.TryGetAccess(out _access, out _);
            if (_access != null && sessionObserver != null && sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot))
            {
                PlayerSlotRuntimeSnapshot joined = snapshot.Slots.FirstOrDefault(slot => slot.IsJoined);
                if (joined.IsJoined)
                {
                    SessionPlayerLeaveResult leave = _access.RequestLeave(new SessionPlayerLeaveRequest(
                        joined.PlayerSlotId, joined.Revision, ScenarioId, "Restore the empty Session baseline after Slice 1."));
                    _leaveStatus = leave != null ? leave.Status.ToString() : "null";
                    _leaveProvisioningReleased = leave != null && leave.ProvisioningReleased;
                    _leaveTerminalCommitted = leave != null && leave.TerminalCommitted;
                    leaveConfirmed = leave != null && leave.Succeeded && leave.ProvisioningReleased && leave.TerminalCommitted;
                    if (!leaveConfirmed) SetCleanupIssue("Public Leave did not fully release the joined Slot/Host. " + (leave != null ? leave.ToDiagnosticString() : "null result"));
                }
                else leaveConfirmed = true;

                if (_joiningOpened || snapshot.JoiningOpen)
                {
                    PlayerParticipationOperationResult closed = _access.CloseJoining(ScenarioId, "Restore initially closed Joining state.");
                    if (closed == null || !closed.Succeeded) SetCleanupIssue("Public CloseJoining failed. " + (closed != null ? closed.ToDiagnosticString() : "null result"));
                    else _joiningOpened = false;
                }
            }
            else if (_joiningOpened) SetCleanupIssue("Scoped Session access ended before public cleanup.");

            RemoveKeyboard();
            yield return null;
            if (leaveConfirmed && string.IsNullOrEmpty(_cleanupIssue)) _completed++;

            bool restored = string.IsNullOrEmpty(_cleanupIssue) && sessionObserver != null &&
                sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot finalSnapshot) && finalSnapshot.IsInitialized &&
                finalSnapshot.JoinedCount == 0 && finalSnapshot.AvailableCount == 1 && finalSnapshot.ReservedCount == 0 && finalSnapshot.LeavingCount == 0 && !finalSnapshot.JoiningOpen && (_keyboard == null || !_keyboard.added) &&
                _joinedInput == null && _joinedHost == null;
            if (sessionObserver != null && sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot observedFinal))
                _finalSessionState = "initialized:" + observedFinal.IsInitialized + ",joined:" + observedFinal.JoinedCount +
                    ",available:" + observedFinal.AvailableCount + ",reserved:" + observedFinal.ReservedCount +
                    ",leaving:" + observedFinal.LeavingCount + ",joiningOpen:" + observedFinal.JoiningOpen;
            if (restored) _completed++;
            else if (string.IsNullOrEmpty(_cleanupIssue)) SetCleanupIssue("Final public snapshot, Host release, PlayerInput destruction or synthetic device baseline could not be confirmed.");
            _cleanup = restored ? QaCleanupDisposition.BaselineRestored : QaCleanupDisposition.FreshBootRequired;

            if (_completed == ExpectedCases && !HasIssue && restored) _recorder.RecordPass();
            else if (!HasIssue) RecordBlocked("Scenario did not reach all checkpoints and restore its baseline.");
            PublishTerminal();
        }

        private void TrySynchronousLeave()
        {
            if (_access == null && sessionObserver != null) sessionObserver.TryGetAccess(out _access, out _);
            if (_access == null || sessionObserver == null || !sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot)) return;
            PlayerSlotRuntimeSnapshot joined = snapshot.Slots.FirstOrDefault(slot => slot.IsJoined);
            if (joined.IsJoined)
            {
                SessionPlayerLeaveResult result = _access.RequestLeave(new SessionPlayerLeaveRequest(
                    joined.PlayerSlotId, joined.Revision, ScenarioId, "Best-effort public Leave on interruption."));
                if (result == null || !result.Succeeded) SetCleanupIssue("Interrupted scenario public Leave failed.");
            }
            PlayerParticipationOperationResult close = _access.CloseJoining(ScenarioId, "Best-effort close on interruption.");
            if (close == null || !close.Succeeded) SetCleanupIssue("Interrupted scenario public CloseJoining failed.");
        }

        private void RemoveKeyboard()
        {
            if (_keyboard == null || !_keyboard.added) return;
            try { InputSystem.RemoveDevice(_keyboard); }
            catch (Exception exception) { SetCleanupIssue("QA-owned Keyboard removal failed: " + exception.Message); }
        }

        private bool HasIssue => !string.IsNullOrEmpty(_firstIssue);
        private void RecordFail(string issue)
        {
            if (HasIssue) return;
            _firstIssue = string.IsNullOrWhiteSpace(issue) ? "Contract divergence without diagnostic." : issue;
            _recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, _firstIssue);
        }
        private void RecordBlocked(string issue)
        {
            if (HasIssue) return;
            _firstIssue = string.IsNullOrWhiteSpace(issue) ? "Valid exercise could not start." : issue;
            _recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, _firstIssue);
        }
        private void SetCleanupIssue(string issue)
        {
            if (string.IsNullOrEmpty(_cleanupIssue)) _cleanupIssue = issue ?? string.Empty;
        }
        private void PublishTerminal()
        {
            if (_terminal) return;
            _terminal = true;
            QaCertificationResult result = _recorder.CreateResult(ScenarioId, _cleanup, _cleanupIssue);
            string status = result.Verdict == QaCertificationVerdict.Pass ? "Passed" : result.Verdict == QaCertificationVerdict.Blocked ? "Blocked" : "Failed";
            string missing = _completed < ExpectedCases ? _stage : string.Empty;
            string line = $"[{ScenarioId}] status='{status}' verdict='{result.Verdict.ToString().ToUpperInvariant()}' cases='{_completed}/{ExpectedCases}' next='{missing}' completed='{_completed}' missing='{missing}' invalidJoinStatus='{_invalidJoinStatus}' invalidRevisionPreserved='{_invalidRevisionPreserved}' joinStatus='{_joinStatus}' joinSlotMatched='{_joinSlotMatched}' actorSelected='{_actorSelected}' actorPrepared='{_actorPrepared}' actorMaterialized='{_actorMaterialized}' gameplayAdmitted='{_gameplayAdmitted}' leaveStatus='{_leaveStatus}' leaveProvisioningReleased='{_leaveProvisioningReleased}' leaveTerminalCommitted='{_leaveTerminalCommitted}' finalSession='{Sanitize(_finalSessionState)}' cleanup='{result.CleanupDisposition}' execution='{Sanitize(result.FirstCausalDivergence)}' cleanupIssue='{Sanitize(result.CleanupIssue)}'.";
            if (result.Verdict == QaCertificationVerdict.Pass) Debug.Log(line, this); else Debug.LogError(line, this);
        }
        private static string Sanitize(string value) => string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\r", " ").Replace("\n", " ").Replace("'", "’");
    }
}




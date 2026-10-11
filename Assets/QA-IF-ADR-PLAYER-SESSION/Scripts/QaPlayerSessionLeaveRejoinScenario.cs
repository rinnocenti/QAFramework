using System;
using System.Collections;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.QaFramework.Certification;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.QaFramework.PlayerSession
{
    [DisallowMultipleComponent]
    public sealed class QaPlayerSessionLeaveRejoinScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-PLAYER-LEAVE-REJOIN-OCCURRENCE";

        [SerializeField] private PlayerSessionObserver sessionObserver;
        [SerializeField] private PlayerSessionJoinCommandTrigger joinCommand;
        [SerializeField] private PlayerSlotProfile expectedSlot;
        [SerializeField] private ActorProfile expectedDefaultActor;
        [SerializeField, Min(1)] private int frameBudget = 600;

        private readonly QaCertificationRecorder _recorder = new QaCertificationRecorder();
        private IPlayerSessionScopedAccess _access;
        private Keyboard _keyboard;
        private LocalPlayerJoinResult _joinA;
        private LocalPlayerJoinResult _joinB;
        private OccurrenceEvidence _occurrenceA;
        private OccurrenceEvidence _occurrenceB;
        private string _firstIssue = string.Empty;
        private string _cleanupIssue = string.Empty;
        private string _stage = "baseline";
        private string _joinAStatus = "not-run";
        private string _joinBStatus = "not-run";
        private string _leaveAStatus = "not-run";
        private string _staleLeaveStatus = "not-run";
        private string _leaveBStatus = "not-run";
        private QaCleanupDisposition _cleanup = QaCleanupDisposition.FreshBootRequired;
        private bool _started;
        private bool _terminal;

        private void Start()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (!_started || _terminal) return;
            RecordBlocked("Scenario interrupted before lifecycle and cleanup completed; use a fresh Play Mode boot.");
            TrySynchronousCleanup();
            RemoveKeyboard();
            _cleanup = QaCleanupDisposition.FreshBootRequired;
            SetCleanupIssue("Interrupted execution cannot prove the asynchronous Actor occurrence release and baseline restoration.");
            PublishTerminal();
        }

        private IEnumerator Run()
        {
            yield return WaitForBaseline();
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }

            _stage = "open-joining";
            PlayerParticipationOperationResult opened = _access.OpenJoining(ScenarioId, "Open Joining for P1 Leave/Rejoin lifecycle.");
            if (opened == null || !opened.Succeeded)
            {
                RecordFail("Public OpenJoining failed from the valid empty baseline. " + (opened != null ? opened.ToDiagnosticString() : "null result"));
                yield return CleanupAndPublish(); yield break;
            }
            _stage = "create-synthetic-device";
            string deviceIssue = string.Empty;
            try { _keyboard = InputSystem.AddDevice<Keyboard>("QA Player Leave-Rejoin P1"); }
            catch (Exception exception) { deviceIssue = exception.Message; }
            if (!string.IsNullOrEmpty(deviceIssue) || _keyboard == null || !_keyboard.added)
            {
                RecordBlocked("Could not create the QA-owned synthetic Keyboard. " + deviceIssue);
                yield return CleanupAndPublish(); yield break;
            }

            _stage = "join-a";
            joinCommand.InvokeFromDevice(_keyboard);
            _joinA = joinCommand.LastJoinResult;
            _joinAStatus = _joinA != null ? _joinA.Status.ToString() : "null";
            if (!ValidateJoin(_joinA, "Actor A"))
            {
                yield return CleanupAndPublish(); yield break;
            }
            yield return WaitForOccurrence(_joinA, true);
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }
            LogStage("join-a-ready", Describe(_occurrenceA));

            _stage = "leave-a";
            SessionPlayerLeaveResult leaveA = RequestLeave(_occurrenceA, "Leave Actor occurrence A before rejoining the same Slot.");
            _leaveAStatus = leaveA != null ? leaveA.Status.ToString() : "null";
            if (!ValidateSuccessfulLeave(leaveA, "Actor A"))
            {
                yield return CleanupAndPublish(); yield break;
            }
            yield return WaitForAInvalidation();
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }
            LogStage("leave-a-released", "oldActorRuntimeDestroyed='true' sessionSlotAvailable='true' currentActor='none'");

            _stage = "join-b";
            joinCommand.InvokeFromDevice(_keyboard);
            _joinB = joinCommand.LastJoinResult;
            _joinBStatus = _joinB != null ? _joinB.Status.ToString() : "null";
            if (!ValidateJoin(_joinB, "Actor B"))
            {
                yield return CleanupAndPublish(); yield break;
            }
            yield return WaitForOccurrence(_joinB, false);
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }
            if (!ValidateRejoinIdentity())
            {
                yield return CleanupAndPublish(); yield break;
            }
            LogStage("join-b-ready", Describe(_occurrenceB));

            _stage = "reject-stale-a-evidence";
            SessionPlayerLeaveResult stale = _access.RequestLeave(new SessionPlayerLeaveRequest(
                _occurrenceA.SlotId,
                _occurrenceA.SlotRevision,
                ScenarioId,
                "Reject stale Leave evidence captured for Actor occurrence A while B is current."));
            _staleLeaveStatus = stale != null ? stale.Status.ToString() : "null";
            if (stale == null || stale.Status != SessionPlayerLeaveStatus.RejectedForeignOrStaleOccurrence || !BRemainsCurrent())
            {
                RecordFail("Stale A Leave evidence was not rejected as RejectedForeignOrStaleOccurrence while preserving B. " +
                    (stale != null ? stale.ToDiagnosticString() : "null result"));
                yield return CleanupAndPublish(); yield break;
            }
            LogStage("stale-a-rejected", stale.ToDiagnosticString());

            _stage = "leave-b";
            SessionPlayerLeaveResult leaveB = RequestLeave(_occurrenceB, "Release Actor occurrence B and restore the empty Session baseline.");
            _leaveBStatus = leaveB != null ? leaveB.Status.ToString() : "null";
            if (!ValidateSuccessfulLeave(leaveB, "Actor B"))
            {
                yield return CleanupAndPublish(); yield break;
            }
            yield return WaitForBInvalidation();
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }
            yield return CleanupAndPublish();
        }

        private IEnumerator WaitForBaseline()
        {
            _stage = "baseline";
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (sessionObserver != null && joinCommand != null && expectedSlot != null && expectedDefaultActor != null &&
                    sessionObserver.IsAvailable && sessionObserver.TryGetAccess(out _access, out _) && _access != null &&
                    sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) && snapshot.IsInitialized &&
                    snapshot.ConfiguredSlotCount == 1 && snapshot.Slots.Count == 1 && snapshot.Slots[0].Profile == expectedSlot &&
                    snapshot.Slots[0].AllocationState == PlayerSlotAllocationState.Available && !snapshot.JoiningOpen && snapshot.JoinedCount == 0 &&
                    expectedSlot.DefaultActorProfile == expectedDefaultActor &&
                    sessionObserver.CurrentObservation.InitializationConfiguration != null &&
                    sessionObserver.CurrentObservation.InitializationConfiguration.ActorResolutionPolicy == PlayerActorResolutionPolicy.ResolveConfiguredDefault &&
                    sessionObserver.Scope == LocalPlayerProvisioningConsumerScope.Activity &&
                    joinCommand.Scope == LocalPlayerProvisioningConsumerScope.Activity &&
                    joinCommand.TryGetJoinAccess(out ILocalPlayerJoinAccess joinAccess, out _) && joinAccess != null)
                {
                    LogStage("baseline", $"sessionRevision='{snapshot.Revision}' slots='{snapshot.ConfiguredSlotCount}' joiningOpen='false' joined='0' provisioning='ManagerProvisioned'");
                    yield break;
                }
                yield return null;
            }
            RecordBlocked("Valid empty/closed Activity-scoped Session baseline or Manager-Provisioned Join authority was unavailable.");
        }

        private bool ValidateJoin(LocalPlayerJoinResult result, string occurrenceName)
        {
            if (result == null || !result.Completed || !result.Succeeded)
            {
                string diagnostic = result != null ? result.ToDiagnosticString() : "null result";
                if (result == null || result.Status is LocalPlayerJoinStatus.RejectedRuntimeUnavailable or
                    LocalPlayerJoinStatus.RejectedManagerUnavailable or LocalPlayerJoinStatus.RejectedManagerConfiguration)
                    RecordBlocked(occurrenceName + " Join could not be exercised because runtime/manager preconditions were unavailable. " + diagnostic);
                else RecordFail(occurrenceName + " Join returned an unsuccessful typed result. " + diagnostic);
                return false;
            }

            if (!result.Slot.IsJoined || result.Slot.PlayerSlotId != expectedSlot.PlayerSlotId ||
                result.LocalPlayerHost == null || result.PlayerInput == null ||
                !ReferenceEquals(result.LocalPlayerHost.PlayerInput, result.PlayerInput))
            {
                RecordFail(occurrenceName + " Join did not correlate the configured P1 Slot, PlayerInput and Local Player Host.");
                return false;
            }
            return true;
        }

        private IEnumerator WaitForOccurrence(LocalPlayerJoinResult join, bool captureA)
        {
            _stage = captureA ? "observe-actor-a" : "observe-actor-b";
            PlayerSessionScopedSlotObservation lastObservation = default;
            bool hasObservation = false;
            string issue = string.Empty;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (TryGetP1(out PlayerSessionScopedSlotObservation current, out issue))
                {
                    lastObservation = current;
                    hasObservation = true;
                    if (current.IsJoined && current.HasCurrentActorEvidence && current.CurrentActor.HasCurrentActor &&
                        current.IsLogicalActorPrepared && current.IsPhysicallyMaterialized)
                    {
                        if (!TryCapture(join, current, out OccurrenceEvidence evidence, out bool observabilityUnavailable, out issue))
                        {
                            if (observabilityUnavailable)
                                RecordBlocked("Required public Actor/occurrence identity evidence is unavailable. " + issue);
                            else
                                RecordFail("Current prepared Actor occurrence diverged from the joined Host/physical composition. " + issue);
                            yield break;
                        }
                        if (current.Slot.SelectedActorProfile != expectedDefaultActor)
                        {
                            RecordFail("ResolveConfiguredDefault did not select the authored default Actor for " + (captureA ? "A" : "B") + ".");
                            yield break;
                        }
                        if (current.IsGameplayAdmitted)
                        {
                            RecordFail("Actor " + (captureA ? "A" : "B") + " unexpectedly reached Gameplay Admission; this scenario only requires preparation/materialization.");
                            yield break;
                        }
                        if (captureA) _occurrenceA = evidence;
                        else _occurrenceB = evidence;
                        yield break;
                    }
                }
                yield return null;
            }

            if (hasObservation && lastObservation.IsPhysicallyMaterialized && !lastObservation.HasCurrentActorEvidence)
                RecordBlocked("Actor is physically materialized but the public current-Actor correlation evidence is unavailable. " + issue);
            else
                RecordFail("Joined P1 did not reach the configured LogicalActorsPrepared/materialized Actor state. " + issue);
        }

        private bool TryCapture(
            LocalPlayerJoinResult join,
            PlayerSessionScopedSlotObservation observation,
            out OccurrenceEvidence evidence,
            out bool observabilityUnavailable,
            out string issue)
        {
            evidence = default;
            observabilityUnavailable = false;
            issue = string.Empty;
            if (!observation.CurrentActor.ActorEvidence.IsValid ||
                !observation.CurrentActor.ActorEvidence.RuntimeContentIdentity.IsValid ||
                !observation.CurrentActor.Assignment.HostBindingIdentity.IsValid)
            {
                observabilityUnavailable = true;
                issue = "Actor correlation, RuntimeContentIdentity or HostBindingIdentity is not available from the public observation.";
                return false;
            }
            if (join == null || join.LocalPlayerHost == null || join.PlayerInput == null || join.LocalPlayerHost.ActorMount == null)
            {
                issue = "A successful Join did not retain the exact Local Player Host, PlayerInput or Actor Mount.";
                return false;
            }

            PlayerActorRuntimeHost[] actorHosts = join.LocalPlayerHost.ActorMount.GetComponentsInChildren<PlayerActorRuntimeHost>(true);
            if (actorHosts.Length != 1 || actorHosts[0] == null)
            {
                issue = "The explicitly owned Host Actor Mount must contain exactly one materialized PlayerActorRuntimeHost; found '" + actorHosts.Length + "'.";
                return false;
            }

            evidence = new OccurrenceEvidence(
                observation.Slot.PlayerSlotId,
                observation.Slot.Revision,
                observation.Slot.PlayerOccurrenceId,
                observation.CurrentActor.Assignment.HostBindingIdentity,
                observation.CurrentActor.ActorEvidence.ActorId,
                observation.CurrentActor.ActorEvidence.RuntimeContentIdentity,
                observation.CurrentActor.ActorEvidence.MaterializationRevision,
                observation.CurrentActor.ActorEvidence.CorrelationRevision,
                join.LocalPlayerHost,
                join.PlayerInput,
                actorHosts[0]);
            if (!evidence.SlotId.IsValid || !evidence.PlayerOccurrenceId.IsValid)
            {
                observabilityUnavailable = true;
                issue = "Public P1 Slot or Player occurrence identity is invalid.";
                evidence = default;
                return false;
            }
            return true;
        }

        private bool ValidateSuccessfulLeave(SessionPlayerLeaveResult result, string occurrenceName)
        {
            if (result == null || !result.Succeeded || !result.LeaveStarted ||
                !result.ActivityRepresentationReleased || !result.ProvisioningReleased || !result.TerminalCommitted)
            {
                RecordFail("Public Leave did not release " + occurrenceName + " Activity representation, provisioning Host and terminal Session ownership. " +
                    (result != null ? result.ToDiagnosticString() : "null result"));
                return false;
            }
            return true;
        }

        private IEnumerator WaitForAInvalidation()
        {
            _stage = "verify-a-invalidated";
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (TryGetP1(out PlayerSessionScopedSlotObservation slot, out string issue) &&
                    !slot.IsJoined && slot.Slot.AllocationState == PlayerSlotAllocationState.Available &&
                    !slot.CurrentActor.HasCurrentActor && !slot.HostEvidence.IsConfirmed && _occurrenceA.ActorRuntimeHost == null)
                    yield break;
                yield return null;
            }
            RecordFail("Leave A did not invalidate the old Actor runtime Host and clear current Host/Actor evidence. " +
                (_occurrenceA.ActorRuntimeHost != null ? "Actor A GameObject is still alive." : "P1 observation did not return to Available without current Actor evidence."));
        }

        private bool ValidateRejoinIdentity()
        {
            if (_occurrenceA.SlotId != _occurrenceB.SlotId ||
                _occurrenceA.PlayerOccurrenceId == _occurrenceB.PlayerOccurrenceId ||
                _occurrenceA.HostBindingIdentity.Equals(_occurrenceB.HostBindingIdentity) ||
                _occurrenceA.RuntimeContentIdentity.Equals(_occurrenceB.RuntimeContentIdentity) ||
                _occurrenceA.MaterializationRevision == _occurrenceB.MaterializationRevision ||
                ReferenceEquals(_occurrenceA.Host, _occurrenceB.Host) ||
                ReferenceEquals(_occurrenceA.PlayerInput, _occurrenceB.PlayerInput) ||
                ReferenceEquals(_occurrenceA.ActorRuntimeHost, _occurrenceB.ActorRuntimeHost))
            {
                RecordFail("Rejoin did not create a distinct Player/Host/Actor physical occurrence in the same Slot. A=" +
                    Describe(_occurrenceA) + " B=" + Describe(_occurrenceB));
                return false;
            }
            if (!sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) || snapshot.JoinedCount != 1 || snapshot.Slots.Count != 1)
            {
                RecordFail("Rejoin produced an unexpected Session Slot/Player cardinality; exactly one P1 must be joined.");
                return false;
            }
            return true;
        }

        private bool BRemainsCurrent()
        {
            if (!sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) || snapshot.JoinedCount != 1 ||
                !TryGetP1(out PlayerSessionScopedSlotObservation current, out _) || !current.IsJoined)
                return false;
            return current.Slot.Revision == _occurrenceB.SlotRevision &&
                current.Slot.PlayerOccurrenceId == _occurrenceB.PlayerOccurrenceId &&
                current.CurrentActor.HasCurrentActor &&
                current.CurrentActor.ActorEvidence.RuntimeContentIdentity.Equals(_occurrenceB.RuntimeContentIdentity) &&
                current.CurrentActor.Assignment.HostBindingIdentity.Equals(_occurrenceB.HostBindingIdentity) &&
                _occurrenceB.ActorRuntimeHost != null && _occurrenceB.Host != null && _occurrenceB.PlayerInput != null;
        }

        private IEnumerator WaitForBInvalidation()
        {
            _stage = "verify-b-invalidated";
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (TryGetP1(out PlayerSessionScopedSlotObservation slot, out _) &&
                    !slot.IsJoined && slot.Slot.AllocationState == PlayerSlotAllocationState.Available &&
                    !slot.CurrentActor.HasCurrentActor && !slot.HostEvidence.IsConfirmed && _occurrenceB.ActorRuntimeHost == null)
                    yield break;
                yield return null;
            }
            RecordFail("Final Leave did not invalidate Actor B and release its current Host/Actor evidence.");
        }

        private SessionPlayerLeaveResult RequestLeave(OccurrenceEvidence occurrence, string reason)
        {
            if (_access == null && sessionObserver != null) sessionObserver.TryGetAccess(out _access, out _);
            return _access != null
                ? _access.RequestLeave(new SessionPlayerLeaveRequest(occurrence.SlotId, occurrence.SlotRevision, ScenarioId, reason))
                : null;
        }

        private bool TryGetP1(out PlayerSessionScopedSlotObservation slot, out string issue)
        {
            slot = default;
            issue = string.Empty;
            if (expectedSlot == null || sessionObserver == null || !sessionObserver.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) ||
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
            return true;
        }

        private IEnumerator CleanupAndPublish()
        {
            _stage = "cleanup";
            if (_access == null && sessionObserver != null) sessionObserver.TryGetAccess(out _access, out _);
            if (_access != null && sessionObserver != null && sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot))
            {
                PlayerSlotRuntimeSnapshot joined = snapshot.Slots.FirstOrDefault(candidate => candidate.IsJoined);
                if (joined.IsJoined)
                {
                    SessionPlayerLeaveResult leave = _access.RequestLeave(new SessionPlayerLeaveRequest(
                        joined.PlayerSlotId, joined.Revision, ScenarioId, "Best-effort cleanup after scenario exit/failure."));
                    if (leave == null || !leave.Succeeded || !leave.ProvisioningReleased || !leave.TerminalCommitted)
                        SetCleanupIssue("Cleanup Leave did not confirm Host provisioning release and terminal commit. " + (leave != null ? leave.ToDiagnosticString() : "null result"));
                }

                for (int frame = 0; frame < Mathf.Min(30, Mathf.Max(1, frameBudget)); frame++)
                {
                    if (sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot current) && current.JoinedCount == 0) break;
                    yield return null;
                }

                if (sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot beforeClose) && beforeClose.JoiningOpen)
                {
                    PlayerParticipationOperationResult closed = _access.CloseJoining(ScenarioId, "Restore initially closed Joining state after Slice 2.");
                    if (closed == null || !closed.Succeeded)
                        SetCleanupIssue("Public CloseJoining failed. " + (closed != null ? closed.ToDiagnosticString() : "null result"));
                }
            }
            else SetCleanupIssue("Could not reacquire public Session access/snapshot for cleanup.");

            RemoveKeyboard();
            yield return null;

            bool restored = sessionObserver != null && sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot finalSnapshot) &&
                finalSnapshot.IsInitialized && finalSnapshot.ConfiguredSlotCount == 1 && finalSnapshot.Slots.Count == 1 &&
                finalSnapshot.Slots[0].Profile == expectedSlot && finalSnapshot.Slots[0].AllocationState == PlayerSlotAllocationState.Available &&
                finalSnapshot.JoinedCount == 0 && !finalSnapshot.JoiningOpen;
            if (!restored) SetCleanupIssue("Final public Session snapshot did not return to one Available P1 Slot, zero joined Players and closed Joining.");
            bool physicalResourcesReleased = _keyboard == null && _occurrenceA.Host == null && _occurrenceA.PlayerInput == null &&
                _occurrenceA.ActorRuntimeHost == null && _occurrenceB.Host == null && _occurrenceB.PlayerInput == null &&
                _occurrenceB.ActorRuntimeHost == null;
            if (!physicalResourcesReleased)
                SetCleanupIssue("Final cleanup did not prove release of the QA Keyboard and captured Host/PlayerInput/Actor Runtime occurrences.");
            if (restored && physicalResourcesReleased && string.IsNullOrEmpty(_cleanupIssue)) _cleanup = QaCleanupDisposition.BaselineRestored;
            else _cleanup = QaCleanupDisposition.FreshBootRequired;

            if (!HasIssue && _cleanup == QaCleanupDisposition.BaselineRestored) _recorder.RecordPass();
            else if (!HasIssue) RecordBlocked("Scenario could not prove baseline restoration after cleanup.");
            LogStage("cleanup", $"baselineRestored='{_cleanup == QaCleanupDisposition.BaselineRestored}' cleanupIssue='{_cleanupIssue}'");
            PublishTerminal();
        }

        private void TrySynchronousCleanup()
        {
            try
            {
                if (_access == null && sessionObserver != null) sessionObserver.TryGetAccess(out _access, out _);
                if (_access != null && sessionObserver != null && sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot))
                {
                    PlayerSlotRuntimeSnapshot joined = snapshot.Slots.FirstOrDefault(candidate => candidate.IsJoined);
                    if (joined.IsJoined)
                    {
                        SessionPlayerLeaveResult leave = _access.RequestLeave(new SessionPlayerLeaveRequest(
                            joined.PlayerSlotId, joined.Revision, ScenarioId, "Synchronous interruption cleanup."));
                        if (leave == null || !leave.Succeeded || !leave.ProvisioningReleased || !leave.TerminalCommitted)
                            SetCleanupIssue("Synchronous interruption cleanup Leave was not confirmed.");
                    }
                    if (snapshot.JoiningOpen)
                    {
                        PlayerParticipationOperationResult close = _access.CloseJoining(ScenarioId, "Interruption cleanup.");
                        if (close == null || !close.Succeeded) SetCleanupIssue("Synchronous interruption cleanup could not close Joining.");
                    }
                }
            }
            catch (Exception exception) { SetCleanupIssue("Synchronous interruption cleanup threw: " + exception.Message); }
        }

        private void RemoveKeyboard()
        {
            if (_keyboard == null) return;
            try
            {
                if (_keyboard.added) InputSystem.RemoveDevice(_keyboard);
                _keyboard = null;
            }
            catch (Exception exception) { SetCleanupIssue("Could not remove the QA-owned synthetic Keyboard: " + exception.Message); }
        }

        private void RecordFail(string issue)
        {
            if (HasIssue) return;
            _firstIssue = issue ?? "Scenario failed without a diagnostic.";
            _recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, _firstIssue);
            LogStage("first-causal-failure", _firstIssue);
        }

        private void RecordBlocked(string issue)
        {
            if (HasIssue) return;
            _firstIssue = issue ?? "Scenario blocked without a diagnostic.";
            _recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, _firstIssue);
            LogStage("blocked", _firstIssue);
        }

        private void SetCleanupIssue(string issue)
        {
            if (string.IsNullOrWhiteSpace(_cleanupIssue)) _cleanupIssue = issue ?? string.Empty;
        }

        private bool HasIssue => !string.IsNullOrEmpty(_firstIssue);

        private void LogStage(string stage, string evidence) =>
            Debug.Log("[" + ScenarioId + "][" + stage + "] " + evidence, this);

        private void PublishTerminal()
        {
            if (_terminal) return;
            _terminal = true;
            QaCertificationResult result = _recorder.CreateResult(ScenarioId, _cleanup, _cleanupIssue);
            string terminal = result.Verdict.ToString().ToUpperInvariant();
            string message = "[" + ScenarioId + "][terminal] verdict='" + terminal + "' " +
                "slot='" + (_occurrenceB.SlotId.IsValid ? _occurrenceB.SlotId.StableText : _occurrenceA.SlotId.StableText) + "' " +
                "actorA='" + (_occurrenceA.RuntimeContentIdentity.IsValid ? _occurrenceA.RuntimeContentIdentity.StableText : "not-captured") + "' " +
                "actorB='" + (_occurrenceB.RuntimeContentIdentity.IsValid ? _occurrenceB.RuntimeContentIdentity.StableText : "not-captured") + "' " +
                "joinA='" + _joinAStatus + "' leaveA='" + _leaveAStatus + "' joinB='" + _joinBStatus + "' " +
                "staleLeave='" + _staleLeaveStatus + "' leaveB='" + _leaveBStatus + "' " +
                "cleanup='" + result.CleanupDisposition + "' firstCause='" + result.FirstCausalDivergence + "' cleanupIssue='" + result.CleanupIssue + "'";
            if (result.Verdict == QaCertificationVerdict.Pass) Debug.Log(message, this);
            else Debug.LogError(message, this);
        }

        private static string Describe(OccurrenceEvidence evidence) =>
            "slot='" + evidence.SlotId.StableText + "' slotRevision='" + evidence.SlotRevision + "' playerOccurrence='" +
            evidence.PlayerOccurrenceId + "' hostBinding='" + evidence.HostBindingIdentity + "' actorId='" + evidence.ActorId +
            "' runtimeContent='" + evidence.RuntimeContentIdentity.StableText + "' materializationRevision='" +
            evidence.MaterializationRevision + "' correlationRevision='" + evidence.CorrelationRevision + "'";

        private readonly struct OccurrenceEvidence
        {
            internal OccurrenceEvidence(
                PlayerSlotId slotId,
                int slotRevision,
                PlayerOccurrenceId playerOccurrenceId,
                PlayerHostBindingIdentity hostBindingIdentity,
                ActorId actorId,
                RuntimeContentIdentity runtimeContentIdentity,
                int materializationRevision,
                int correlationRevision,
                LocalPlayerHostAuthoring host,
                PlayerInput playerInput,
                PlayerActorRuntimeHost actorRuntimeHost)
            {
                SlotId = slotId;
                SlotRevision = slotRevision;
                PlayerOccurrenceId = playerOccurrenceId;
                HostBindingIdentity = hostBindingIdentity;
                ActorId = actorId;
                RuntimeContentIdentity = runtimeContentIdentity;
                MaterializationRevision = materializationRevision;
                CorrelationRevision = correlationRevision;
                Host = host;
                PlayerInput = playerInput;
                ActorRuntimeHost = actorRuntimeHost;
            }

            internal PlayerSlotId SlotId { get; }
            internal int SlotRevision { get; }
            internal PlayerOccurrenceId PlayerOccurrenceId { get; }
            internal PlayerHostBindingIdentity HostBindingIdentity { get; }
            internal ActorId ActorId { get; }
            internal RuntimeContentIdentity RuntimeContentIdentity { get; }
            internal int MaterializationRevision { get; }
            internal int CorrelationRevision { get; }
            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerInput PlayerInput { get; }
            internal PlayerActorRuntimeHost ActorRuntimeHost { get; }
        }
    }
}

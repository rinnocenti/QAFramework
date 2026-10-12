using System;
using System.Collections;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.QaFramework.Certification;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Immersive.QaFramework.PlayerSession
{
    [DisallowMultipleComponent]
    public sealed class QaPlayerSessionMultiplayerOwnershipScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-PLAYER-MULTIPLAYER-OWNERSHIP";

        [SerializeField] private PlayerSessionObserver sessionObserver;
        [SerializeField] private PlayerSessionJoinTrigger joinTrigger;
        [SerializeField] private PlayerSlotProfile expectedP1;
        [SerializeField] private PlayerSlotProfile expectedP2;
        [SerializeField] private ActorProfile expectedDefaultActor;
        [SerializeField, Min(1)] private int frameBudget = 600;

        private readonly QaCertificationRecorder _recorder = new QaCertificationRecorder();
        private IPlayerSessionScopedAccess _access;
        private Keyboard _deviceA;
        private Keyboard _deviceB;
        private QaPlayerSessionJoinEvidence _joinA;
        private QaPlayerSessionJoinEvidence _joinB;
        private QaPlayerSessionJoinEvidence _rejoinA;
        private QaPlayerSessionJoinEvidence _waitJoin;
        private PlayerOccurrenceId _occurrenceA;
        private PlayerOccurrenceId _occurrenceB;
        private PlayerInput _inputA;
        private PlayerInput _inputB;
        private LocalPlayerHostAuthoring _hostA;
        private LocalPlayerHostAuthoring _hostB;
        private PlayerActorDeclaration _actorA;
        private PlayerActorDeclaration _actorB;
        private PlayerOccurrenceId _waitOccurrence;
        private PlayerActorDeclaration _waitActor;
        private string _firstIssue = string.Empty;
        private string _cleanupIssue = string.Empty;
        private string _stage = "baseline";
        private QaCleanupDisposition _cleanupDisposition = QaCleanupDisposition.FreshBootRequired;
        private bool _terminal;
        private bool _started;
        private bool _joiningOpened;
        private string _joinAStatus = "not-run";
        private string _joinBStatus = "not-run";
        private string _leaveAStatus = "not-run";
        private string _rejoinAStatus = "not-run";
        private string _leaveFinalStatus = "not-run";
        private int _p1RevisionAfterJoin;
        private int _p2RevisionAfterJoin;
        private bool _simultaneousIsolation;
        private bool _p2PreservedAfterLeave;
        private bool _p2PreservedAfterRejoin;
        private bool _baselineRestored;

        private void Start()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (!_started || _terminal) return;
            RecordBlocked("Scenario interrupted before owned Session and synthetic devices were restored; use a fresh Play Mode boot.");
            TrySynchronousLeave();
            RemoveDevices();
            PublishTerminal();
        }

        private void TrySynchronousLeave()
        {
            if (_access == null || sessionObserver == null || !sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot)) return;
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (!slot.IsJoined) continue;
                SessionPlayerLeaveResult result = RequestLeave(slot.PlayerSlotId, slot.Revision, "Synchronous interruption cleanup.");
                if (result == null || !result.Succeeded)
                    SetCleanupIssue("Interrupted-run Leave failed: " + (result != null ? result.ToDiagnosticString() : "null result"));
            }
            if (_joiningOpened)
            {
                PlayerParticipationOperationResult close = _access.CloseJoining(ScenarioId, "Close Joining after interrupted execution.");
                if (close == null || !close.Succeeded) SetCleanupIssue("Interrupted-run CloseJoining failed.");
            }
        }

        private IEnumerator Run()
        {
            yield return WaitForBaseline();
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }

            _stage = "open-joining";
            PlayerParticipationOperationResult opened = _access.OpenJoining(ScenarioId, "Open admission for two independent synthetic devices.");
            if (opened == null || !opened.Succeeded)
            {
                RecordBlocked("Public OpenJoining was unavailable from the valid baseline. " + (opened != null ? opened.ToDiagnosticString() : "null result"));
                yield return CleanupAndPublish(); yield break;
            }
            _joiningOpened = true;

            _stage = "create-synthetic-devices";
            string issue = string.Empty;
            try { _deviceA = InputSystem.AddDevice<Keyboard>(); }
            catch (Exception exception) { issue = exception.Message; }
            if (!string.IsNullOrEmpty(issue) || _deviceA == null || !_deviceA.added)
            {
                RecordBlocked("Could not create synthetic InputDevice A. " + issue);
                yield return CleanupAndPublish(); yield break;
            }
            try { _deviceB = InputSystem.AddDevice<Keyboard>(); }
            catch (Exception exception) { issue = exception.Message; }
            if (!string.IsNullOrEmpty(issue) || _deviceB == null || !_deviceB.added || ReferenceEquals(_deviceA, _deviceB))
            {
                RecordBlocked("Could not create a second independent synthetic InputDevice B. " + issue);
                yield return CleanupAndPublish(); yield break;
            }
            LogStage("devices", $"deviceA='{_deviceA.deviceId}' deviceB='{_deviceB.deviceId}' distinct='{!ReferenceEquals(_deviceA, _deviceB)}'");

            _stage = "join-p1-with-device-a";
            LocalPlayerJoinOutcome joinA = RequestJoin(_deviceA, "join-p1-device-a");
            _joinAStatus = joinA != null ? joinA.Code.ToString() : "null";
            if (!ValidateJoin(joinA, "P1/Device A")) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForActor(expectedP1, _deviceA);
            _joinA = _waitJoin;
            _inputA = _joinA.PlayerInput;
            _hostA = _joinA.Host;
            _occurrenceA = _waitOccurrence;
            _actorA = _waitActor;
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }
            _p1RevisionAfterJoin = CurrentSlotRevision(_joinA.Slot.PlayerSlotId);
            LogStage("join-p1-ready", DescribeCurrent(_joinA.Slot.PlayerSlotId));

            _stage = "join-p2-with-device-b";
            LocalPlayerJoinOutcome joinB = RequestJoin(_deviceB, "join-p2-device-b");
            _joinBStatus = joinB != null ? joinB.Code.ToString() : "null";
            if (!ValidateJoin(joinB, "P2/Device B")) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForActor(expectedP2, _deviceB);
            _joinB = _waitJoin;
            _inputB = _joinB.PlayerInput;
            _hostB = _joinB.Host;
            _occurrenceB = _waitOccurrence;
            _actorB = _waitActor;
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }
            _p2RevisionAfterJoin = CurrentSlotRevision(_joinB.Slot.PlayerSlotId);
            _simultaneousIsolation = ValidateIsolation();
            if (!_simultaneousIsolation)
            {
                RecordFail("Simultaneous P1/P2 state did not preserve distinct Slot, occurrence, Host, Actor, PlayerInput and device ownership.");
                yield return CleanupAndPublish(); yield break;
            }
            LogStage("simultaneous-isolation", DescribeCurrent(_joinB.Slot.PlayerSlotId));

            _stage = "leave-p1";
            SessionPlayerLeaveResult leaveA = RequestLeave(_joinA.Slot.PlayerSlotId, CurrentSlotRevision(_joinA.Slot.PlayerSlotId), "Leave P1 while P2 remains active.");
            _leaveAStatus = leaveA != null ? leaveA.Status.ToString() : "null";
            if (leaveA == null || !leaveA.Succeeded)
            {
                RecordFail("P1 Leave did not return Succeeded while P2 was concurrently Joined. " + (leaveA != null ? leaveA.ToDiagnosticString() : "null result"));
                yield return CleanupAndPublish(); yield break;
            }
            yield return WaitForState(expectedP1, PlayerSlotAllocationState.Available, expectedP2, PlayerSlotAllocationState.Joined);
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }
            _p2PreservedAfterLeave = ValidatePlayer(expectedP2, _inputB, _hostB, _actorB, _occurrenceB, _deviceB);
            if (!_p2PreservedAfterLeave)
            {
                RecordFail("P2 changed or lost ownership while P1 left.");
                yield return CleanupAndPublish(); yield break;
            }
            LogStage("leave-p1-p2-retained", DescribeCurrent(_joinB.Slot.PlayerSlotId));

            bool p1Released = false;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (_hostA == null && _inputA == null && _actorA == null &&
                    TryGetSlot(_joinA.Slot.PlayerSlotId, out PlayerSessionScopedSlotObservation p1AfterLeave) &&
                    p1AfterLeave.Slot.AllocationState == PlayerSlotAllocationState.Available &&
                    !p1AfterLeave.IsJoined && !p1AfterLeave.HasSelectedActor && !p1AfterLeave.IsPhysicallyMaterialized)
                {
                    p1Released = true;
                    break;
                }
                yield return null;
            }
            if (!p1Released)
            {
                RecordFail("P1 Host/Input/Actor resources or current Slot state remained after Leave.");
                yield return CleanupAndPublish(); yield break;
            }
            _stage = "rejoin-p1-with-device-a";
            LocalPlayerJoinOutcome rejoinA = RequestJoin(_deviceA, "rejoin-p1-device-a");
            _rejoinAStatus = rejoinA != null ? rejoinA.Code.ToString() : "null";
            if (!ValidateJoin(rejoinA, "P1 rejoin/Device A")) { yield return CleanupAndPublish(); yield break; }
            PlayerOccurrenceId rejoinedOccurrence = default;
            PlayerActorDeclaration rejoinedActor = null;
            yield return WaitForActor(expectedP1, _deviceA);
            _rejoinA = _waitJoin;
            rejoinedOccurrence = _waitOccurrence;
            rejoinedActor = _waitActor;
            if (HasIssue) { yield return CleanupAndPublish(); yield break; }
            _p2PreservedAfterRejoin = ValidatePlayer(expectedP2, _inputB, _hostB, _actorB, _occurrenceB, _deviceB);
            if (rejoinedOccurrence == _occurrenceA || ReferenceEquals(rejoinedActor, _actorA) ||
                ReferenceEquals(_rejoinA.Host, _hostA) || ReferenceEquals(_rejoinA.PlayerInput, _inputA) || !_p2PreservedAfterRejoin)
            {
                RecordFail("P1 rejoin reused its old occurrence/Actor/Host/PlayerInput reference or interfered with P2.");
                yield return CleanupAndPublish(); yield break;
            }
            _inputA = _rejoinA.PlayerInput;
            _hostA = _rejoinA.Host;
            _actorA = rejoinedActor;
            LogStage("rejoin-p1-isolated", $"oldOccurrence='{_occurrenceA}' newOccurrence='{rejoinedOccurrence}' p2Preserved='{_p2PreservedAfterRejoin}'");
            _stage = "cleanup";
            yield return CleanupAndPublish();
        }

        private IEnumerator WaitForBaseline()
        {
            _stage = "baseline";
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (sessionObserver != null && joinTrigger != null && expectedP1 != null && expectedP2 != null && expectedDefaultActor != null &&
                    expectedP1 != expectedP2 && sessionObserver.IsAvailable && sessionObserver.TryGetAccess(out _access, out _) && _access != null &&
                    sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) && snapshot.IsInitialized && snapshot.ConfiguredSlotCount == 2 &&
                    snapshot.Slots.Count == 2 && snapshot.Slots[0].Profile == expectedP1 && snapshot.Slots[1].Profile == expectedP2 &&
                    snapshot.AvailableCount == 2 && snapshot.JoinedCount == 0 && snapshot.ReservedCount == 0 && !snapshot.JoiningOpen &&
                    snapshot.ActorSelectionDuplicatePolicy == PlayerActorSelectionDuplicatePolicy.AllowDuplicates &&
                    expectedP1.DefaultActorProfile == expectedDefaultActor && expectedP2.DefaultActorProfile == expectedDefaultActor &&
                    joinTrigger.Scope == LocalPlayerProvisioningConsumerScope.Activity)
                {
                    LogStage("baseline", $"slots='{snapshot.ConfiguredSlotCount}' available='{snapshot.AvailableCount}' joined='{snapshot.JoinedCount}' reserved='{snapshot.ReservedCount}' joiningOpen='{snapshot.JoiningOpen}' actorDuplicatePolicy='{snapshot.ActorSelectionDuplicatePolicy}'");
                    yield break;
                }
                yield return null;
            }
            RecordBlocked("Two-Slot empty Session baseline, ordered allocation, or Activity-scoped public Join trigger was unavailable.");
        }

        private LocalPlayerJoinOutcome RequestJoin(InputDevice device, string reason)
        {
            return joinTrigger != null
                ? joinTrigger.RequestJoin(new LocalPlayerJoinRequest(ScenarioId, reason, device))
                : null;
        }

        private bool ValidateJoin(LocalPlayerJoinOutcome outcome, string label)
        {
            if (outcome == null || outcome.Code != LocalPlayerJoinOutcomeCode.Joined ||
                outcome.AdmissionState != LocalPlayerJoinAdmissionState.Admitted)
            {
                string diagnostic = outcome != null
                    ? $"code='{outcome.Code}' admission='{outcome.AdmissionState}'"
                    : "null outcome";
                if (outcome == null || outcome.Code is LocalPlayerJoinOutcomeCode.RejectedUnavailable or LocalPlayerJoinOutcomeCode.RejectedConfiguration)
                    RecordBlocked(label + " could not be exercised because Join authority or provisioning preconditions were unavailable. " + diagnostic);
                else RecordFail(label + " Join returned an unsuccessful typed outcome. " + diagnostic);
                return false;
            }
            return true;
        }

        private IEnumerator WaitForActor(PlayerSlotProfile expectedSlot, InputDevice requestedDevice)
        {
            _waitOccurrence = default;
            _waitActor = null;
            _waitJoin = default;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (expectedSlot != null && TryGetSlot(expectedSlot.PlayerSlotId, out PlayerSessionScopedSlotObservation observed) &&
                    observed.IsJoined && observed.HasSelectedActor && observed.Slot.SelectedActorProfile == expectedDefaultActor &&
                    observed.IsLogicalActorPrepared && observed.IsPhysicallyMaterialized &&
                    observed.HasCurrentActorEvidence && observed.CurrentActor.HasCurrentActor &&
                    observed.HasInputOwnershipEvidence &&
                    QaPlayerSessionJoinEvidenceReader.TryCapture(
                        sessionObserver,
                        expectedSlot.PlayerSlotId,
                        requestedDevice,
                        out _waitJoin,
                        out _))
                {
                    _waitOccurrence = observed.Slot.PlayerOccurrenceId;
                    LocalPlayerHostAuthoring host = _waitJoin.Host;
                    PlayerActorRuntimeHost runtimeHost = host != null && host.ActorMount != null ? host.ActorMount.GetComponentInChildren<PlayerActorRuntimeHost>(true) : null;
                    _waitActor = runtimeHost != null ? runtimeHost.PlayerActorDeclaration : null;
                    if (!_waitOccurrence.IsValid || _waitActor == null || _waitActor.PlayerInput != _waitJoin.PlayerInput ||
                        !OwnsOnly(_waitJoin.PlayerInput, requestedDevice) || !DeviceEvidence(observed, _waitJoin.PlayerInput, requestedDevice))
                    {
                        RecordFail("Prepared/materialized Actor or public device ownership evidence did not match its exact Join result.");
                        yield break;
                    }
                    yield break;
                }
                yield return null;
            }
            RecordFail("Joined Player did not reach selected, logically prepared and physically materialized Actor with public input ownership evidence.");
        }

        private bool ValidateIsolation()
        {
            return CurrentSlotRevision(_joinA.Slot.PlayerSlotId) == _p1RevisionAfterJoin &&
                   CurrentSlotRevision(_joinB.Slot.PlayerSlotId) == _p2RevisionAfterJoin &&
                   _joinA.Slot.PlayerSlotId != _joinB.Slot.PlayerSlotId && _occurrenceA.IsValid && _occurrenceB.IsValid && _occurrenceA != _occurrenceB &&
                   !ReferenceEquals(_inputA, _inputB) && !ReferenceEquals(_hostA, _hostB) && !ReferenceEquals(_actorA, _actorB) &&
                   OwnsOnly(_inputA, _deviceA) && OwnsOnly(_inputB, _deviceB) &&
                   ValidatePlayer(expectedP1, _inputA, _hostA, _actorA, _occurrenceA, _deviceA) &&
                   ValidatePlayer(expectedP2, _inputB, _hostB, _actorB, _occurrenceB, _deviceB);
        }

        private bool ValidatePlayer(PlayerSlotProfile profile, PlayerInput input, LocalPlayerHostAuthoring host, PlayerActorDeclaration actor, PlayerOccurrenceId occurrence, InputDevice device)
        {
            if (!sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) || !TryFindSlot(snapshot, profile, out PlayerSlotRuntimeSnapshot slot)) return false;
            return slot.IsJoined && slot.PlayerOccurrenceId == occurrence && occurrence.IsValid && input != null && host != null && actor != null &&
                   ReferenceEquals(host.PlayerInput, input) && actor.PlayerInput == input && OwnsOnly(input, device) &&
                   TryGetSlot(slot.PlayerSlotId, out PlayerSessionScopedSlotObservation observed) && observed.IsPhysicallyMaterialized &&
                   observed.HasCurrentActorEvidence && observed.CurrentActor.HasCurrentActor &&
                   observed.HasInputOwnershipEvidence && observed.InputOwnership.UnityPlayerIndex == input.playerIndex && DeviceEvidence(observed, input, device);
        }

        private bool DeviceEvidence(PlayerSessionScopedSlotObservation observed, PlayerInput input, InputDevice device)
        {
            if (input == null || device == null || !observed.HasInputOwnershipEvidence || !OwnsOnly(input, device)) return false;
            return observed.InputOwnership.Devices.Count == 1 && observed.InputOwnership.Devices[0].DeviceId == device.deviceId;
        }

        private static bool OwnsOnly(PlayerInput input, InputDevice device)
        {
            return input != null && device != null && input.devices.Count == 1 && ReferenceEquals(input.devices[0], device);
        }

        private IEnumerator WaitForState(PlayerSlotProfile firstProfile, PlayerSlotAllocationState firstState, PlayerSlotProfile secondProfile, PlayerSlotAllocationState secondState)
        {
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) &&
                    TryFindSlot(snapshot, firstProfile, out PlayerSlotRuntimeSnapshot first) && first.AllocationState == firstState &&
                    TryFindSlot(snapshot, secondProfile, out PlayerSlotRuntimeSnapshot second) && second.AllocationState == secondState)
                    yield break;
                yield return null;
            }
            RecordFail("Session did not reach the expected two-Slot state before the frame budget expired.");
        }

        private IEnumerator CleanupAndPublish()
        {
            _stage = "cleanup-leave";
            for (int index = 0; index < 2; index++)
            {
                if (!sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot))
                {
                    SetCleanupIssue("Could not read Session snapshot during cleanup.");
                    break;
                }

                PlayerSlotRuntimeSnapshot target = snapshot.Slots.FirstOrDefault(slot => slot.IsJoined || slot.IsReserved);
                if (!target.PlayerSlotId.IsValid) break;
                if (!target.IsJoined)
                {
                    SetCleanupIssue("Reserved Slot remained during cleanup; no public release request is available in this Scenario.");
                    break;
                }

                SessionPlayerLeaveResult leave = RequestLeave(target.PlayerSlotId, target.Revision, "Scenario cleanup.");
                _leaveFinalStatus += (string.IsNullOrEmpty(_leaveFinalStatus) ? string.Empty : ",") + (leave != null ? leave.Status.ToString() : "null");
                if (leave == null || !leave.Succeeded)
                {
                    SetCleanupIssue("Cleanup Leave failed: " + (leave != null ? leave.ToDiagnosticString() : "null result"));
                    break;
                }

                bool released = false;
                for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
                {
                    if (sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot after) &&
                        TryFindSlot(after, target.Profile, out PlayerSlotRuntimeSnapshot releasedSlot) &&
                        releasedSlot.AllocationState == PlayerSlotAllocationState.Available)
                    {
                        released = true;
                        break;
                    }
                    yield return null;
                }
                if (!released)
                {
                    SetCleanupIssue("Cleanup Leave did not restore its Slot within the frame budget.");
                    break;
                }
            }

            if (_joiningOpened && _access != null)
            {
                PlayerParticipationOperationResult closed = _access.CloseJoining(ScenarioId, "Close Joining after cleanup.");
                if (closed == null || !closed.Succeeded)
                    SetCleanupIssue("CloseJoining cleanup failed: " + (closed != null ? closed.ToDiagnosticString() : "null result"));
            }

            RemoveDevices();
            // Session Leave commits synchronously, but Unity destroys the admitted Host at frame end.
            // Match the one-frame release observation used by Slices 1 and 2 before inspecting Unity objects.
            yield return null;

            PlayerParticipationSnapshot final = null;
            bool hasFinalSnapshot = sessionObserver != null && sessionObserver.TryGetSnapshot(out final) && final != null;
            bool sessionInitialized = hasFinalSnapshot && final.IsInitialized;
            bool sessionHasExpectedSlots = sessionInitialized && final.ConfiguredSlotCount == 2 && final.Slots.Count == 2 &&
                expectedP1 != null && expectedP2 != null && final.Slots[0].Profile == expectedP1 && final.Slots[1].Profile == expectedP2;
            bool sessionJoinedZero = sessionInitialized && final.JoinedCount == 0;
            bool sessionAvailableTwo = sessionInitialized && final.AvailableCount == 2;
            bool sessionReservedZero = sessionInitialized && final.ReservedCount == 0;
            bool sessionLeavingZero = sessionInitialized && final.LeavingCount == 0;
            bool sessionSelectedActorsZero = sessionInitialized && final.SelectedActorCount == 0;
            bool joiningClosed = sessionInitialized && !final.JoiningOpen;

            PlayerSessionScopedSlotObservation p1Observation = default;
            PlayerSessionScopedSlotObservation p2Observation = default;
            bool hasP1Observation = expectedP1 != null && TryGetSlot(expectedP1.PlayerSlotId, out p1Observation);
            bool hasP2Observation = expectedP2 != null && TryGetSlot(expectedP2.PlayerSlotId, out p2Observation);
            bool p1SlotAvailable = hasP1Observation && p1Observation.Slot.AllocationState == PlayerSlotAllocationState.Available &&
                !p1Observation.IsJoined && !p1Observation.HasSelectedActor;
            bool p2SlotAvailable = hasP2Observation && p2Observation.Slot.AllocationState == PlayerSlotAllocationState.Available &&
                !p2Observation.IsJoined && !p2Observation.HasSelectedActor;
            bool p1ActorReleased = hasP1Observation && p1Observation.HasPreparationEvidence && p1Observation.Preparation.IsUnprepared &&
                !p1Observation.Preparation.HasMaterialization && !p1Observation.Preparation.HasActorEvidence &&
                !p1Observation.IsPhysicallyMaterialized && (!p1Observation.HasCurrentActorEvidence || !p1Observation.CurrentActor.HasCurrentActor);
            bool p2ActorReleased = hasP2Observation && p2Observation.HasPreparationEvidence && p2Observation.Preparation.IsUnprepared &&
                !p2Observation.Preparation.HasMaterialization && !p2Observation.Preparation.HasActorEvidence &&
                !p2Observation.IsPhysicallyMaterialized && (!p2Observation.HasCurrentActorEvidence || !p2Observation.CurrentActor.HasCurrentActor);
            bool p1HostEvidenceReleased = hasP1Observation && (!p1Observation.HasHostEvidence || !p1Observation.HostEvidence.IsConfirmed);
            bool p2HostEvidenceReleased = hasP2Observation && (!p2Observation.HasHostEvidence || !p2Observation.HostEvidence.IsConfirmed);
            bool hostAndInputObjectsDestroyed = _hostA == null && _hostB == null && _inputA == null && _inputB == null;
            bool actorObjectsDestroyed = _actorA == null && _actorB == null;
            bool deviceAUnpaired = _deviceA == null || !InputUser.FindUserPairedToDevice(_deviceA).HasValue;
            bool deviceBUnpaired = _deviceB == null || !InputUser.FindUserPairedToDevice(_deviceB).HasValue;
            bool deviceAAbsent = _deviceA == null || !_deviceA.added;
            bool deviceBAbsent = _deviceB == null || !_deviceB.added;
            bool cleanupOperationsSucceeded = string.IsNullOrEmpty(_cleanupIssue);

            string sessionState = hasFinalSnapshot
                ? $"initialized={final.IsInitialized},slots={final.ConfiguredSlotCount},available={final.AvailableCount},joined={final.JoinedCount},reserved={final.ReservedCount},leaving={final.LeavingCount},selectedActors={final.SelectedActorCount},joiningOpen={final.JoiningOpen}"
                : "unavailable";
            string[] failedChecks = new[]
            {
                sessionInitialized ? null : "sessionInitialized",
                sessionHasExpectedSlots ? null : "sessionSlotsP1P2",
                sessionJoinedZero ? null : "sessionJoinedZero",
                sessionAvailableTwo ? null : "sessionAvailableTwo",
                sessionReservedZero ? null : "sessionReservedZero",
                sessionLeavingZero ? null : "sessionLeavingZero",
                sessionSelectedActorsZero ? null : "sessionSelectedActorsZero",
                joiningClosed ? null : "joiningClosed",
                hasP1Observation ? null : "p1ObservationAvailable",
                hasP2Observation ? null : "p2ObservationAvailable",
                p1SlotAvailable ? null : "p1SlotAvailable",
                p2SlotAvailable ? null : "p2SlotAvailable",
                p1ActorReleased ? null : "p1ActorPreparationMaterializationReleased",
                p2ActorReleased ? null : "p2ActorPreparationMaterializationReleased",
                p1HostEvidenceReleased ? null : "p1HostEvidenceReleased",
                p2HostEvidenceReleased ? null : "p2HostEvidenceReleased",
                hostAndInputObjectsDestroyed ? null : "hostPlayerInputObjectsDestroyed",
                actorObjectsDestroyed ? null : "actorObjectsDestroyed",
                deviceAUnpaired ? null : "syntheticDeviceAUnpaired",
                deviceBUnpaired ? null : "syntheticDeviceBUnpaired",
                deviceAAbsent ? null : "syntheticDeviceARemoved",
                deviceBAbsent ? null : "syntheticDeviceBRemoved",
                cleanupOperationsSucceeded ? null : "cleanupOperationsSucceeded"
            }.Where(check => check != null).ToArray();
            _baselineRestored = failedChecks.Length == 0;
            LogStage("cleanup-evidence", $"session='{sessionState}' sessionHasExpectedSlots='{sessionHasExpectedSlots}' sessionJoinedZero='{sessionJoinedZero}' sessionAvailableTwo='{sessionAvailableTwo}' sessionReservedZero='{sessionReservedZero}' sessionLeavingZero='{sessionLeavingZero}' sessionSelectedActorsZero='{sessionSelectedActorsZero}' joiningClosed='{joiningClosed}' p1Observation='{hasP1Observation}' p2Observation='{hasP2Observation}' p1SlotAvailable='{p1SlotAvailable}' p2SlotAvailable='{p2SlotAvailable}' p1ActorReleased='{p1ActorReleased}' p2ActorReleased='{p2ActorReleased}' p1HostEvidenceReleased='{p1HostEvidenceReleased}' p2HostEvidenceReleased='{p2HostEvidenceReleased}' hostPlayerInputObjectsDestroyed='{hostAndInputObjectsDestroyed}' actorObjectsDestroyed='{actorObjectsDestroyed}' deviceAUnpaired='{deviceAUnpaired}' deviceBUnpaired='{deviceBUnpaired}' deviceAAbsent='{deviceAAbsent}' deviceBAbsent='{deviceBAbsent}' cleanupOperationsSucceeded='{cleanupOperationsSucceeded}' failed='{string.Join(",", failedChecks)}'");
            if (!_baselineRestored)
            {
                string finalCheckIssue = "Final cleanup checks failed: " + string.Join(",", failedChecks) + ".";
                _cleanupIssue = string.IsNullOrEmpty(_cleanupIssue) ? finalCheckIssue : _cleanupIssue + " | " + finalCheckIssue;
            }
            _cleanupDisposition = _baselineRestored && string.IsNullOrEmpty(_cleanupIssue)
                ? QaCleanupDisposition.BaselineRestored
                : QaCleanupDisposition.FreshBootRequired;
            if (string.IsNullOrEmpty(_firstIssue))
            {
                if (_baselineRestored) _recorder.RecordPass();
                else RecordFail("Final cleanup checks failed: " + string.Join(",", failedChecks) + ".");
            }
            PublishTerminal();
            yield break;
        }
        private SessionPlayerLeaveResult RequestLeave(PlayerSlotId slotId, int revision, string reason)
        {
            return _access.RequestLeave(new SessionPlayerLeaveRequest(slotId, revision, ScenarioId, reason));
        }

        private int CurrentSlotRevision(PlayerSlotId id)
        {
            return sessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) &&
                   snapshot.Slots.FirstOrDefault(slot => slot.PlayerSlotId == id).IsValid
                ? snapshot.Slots.First(slot => slot.PlayerSlotId == id).Revision
                : 0;
        }

        private bool TryGetSlot(PlayerSlotId id, out PlayerSessionScopedSlotObservation slot)
        {
            slot = default;
            if (sessionObserver == null || !sessionObserver.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) ||
                observation == null || !observation.IsAvailable) return false;
            for (int index = 0; index < observation.Slots.Count; index++)
                if (observation.Slots[index].Slot.PlayerSlotId == id) { slot = observation.Slots[index]; return true; }
            return false;
        }

        private static bool TryFindSlot(PlayerParticipationSnapshot snapshot, PlayerSlotProfile profile, out PlayerSlotRuntimeSnapshot slot)
        {
            for (int i = 0; i < snapshot.Slots.Count; i++)
                if (snapshot.Slots[i].Profile == profile) { slot = snapshot.Slots[i]; return true; }
            slot = default;
            return false;
        }

        private string DescribeCurrent(PlayerSlotId id)
        {
            if (!TryGetSlot(id, out PlayerSessionScopedSlotObservation slot)) return "publicObservation='unavailable'";
            return $"slot='{slot.Slot.PlayerSlotId}' state='{slot.Slot.AllocationState}' revision='{slot.Slot.Revision}' occurrence='{slot.Slot.PlayerOccurrenceId}' host='{slot.HostEvidence.HostBindingIdentity.StableText}' devices='{slot.InputOwnership.Devices.Count}' materialized='{slot.IsPhysicallyMaterialized}'";
        }

        private void RemoveDevices()
        {
            if (_deviceB != null && _deviceB.added) { try { InputSystem.RemoveDevice(_deviceB); } catch (Exception exception) { SetCleanupIssue("Could not remove synthetic device B: " + exception.Message); } }
            if (_deviceA != null && _deviceA.added) { try { InputSystem.RemoveDevice(_deviceA); } catch (Exception exception) { SetCleanupIssue("Could not remove synthetic device A: " + exception.Message); } }
        }

        private bool HasIssue => !string.IsNullOrEmpty(_firstIssue);
        private void RecordFail(string message) { if (HasIssue) return; _firstIssue = message; _recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, message); LogStage("first-cause", message); }
        private void RecordBlocked(string message) { if (HasIssue) return; _firstIssue = message; _recorder.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, message); LogStage("blocked", message); }
        private void SetCleanupIssue(string message) { if (string.IsNullOrEmpty(_cleanupIssue)) _cleanupIssue = message; }
        private void LogStage(string stage, string evidence) => Debug.Log($"[{ScenarioId}][{stage}] {evidence}");
        private void PublishTerminal()
        {
            if (_terminal) return;
            _terminal = true;
            QaCertificationResult result = _recorder.CreateResult(ScenarioId, _cleanupDisposition, _cleanupIssue);
            string verdict = result.Verdict.ToString().ToUpperInvariant();
            Debug.Log($"[{ScenarioId}][terminal] verdict='{verdict}' joinA='{_joinAStatus}' joinB='{_joinBStatus}' leaveA='{_leaveAStatus}' rejoinA='{_rejoinAStatus}' leaveFinal='{_leaveFinalStatus}' simultaneousIsolation='{_simultaneousIsolation}' p2AfterLeave='{_p2PreservedAfterLeave}' p2AfterRejoin='{_p2PreservedAfterRejoin}' cleanup='{result.CleanupDisposition}' firstCause='{result.FirstCausalDivergence}' cleanupIssue='{result.CleanupIssue}'");
        }
    }
}













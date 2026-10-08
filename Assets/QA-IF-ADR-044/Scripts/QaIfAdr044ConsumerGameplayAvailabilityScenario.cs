using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.QaFramework.Certification;
using Immersive.QaFramework.IfAdr042;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.QaFramework.IfAdr044
{
    [DisallowMultipleComponent]
    public sealed class QaIfAdr044ConsumerGameplayAvailabilityScenario : MonoBehaviour
    {
        private const string ScenarioId = "IF-ADR-044";
        private const string ControlScheme = "QA.Keyboard";
        private static readonly string[] ExpectedCases =
        {
            "zero-Player baseline",
            "0 -> P1",
            "P1 -> P1+P2",
            "P1 consumer block preserves Player state and suppresses gameplay input",
            "independent P1 blocks compose",
            "releasing one P1 block leaves the other active",
            "releasing the last P1 block restores gameplay input",
            "Leave invalidates held block and Rejoin starts available"
        };

        [SerializeField] private GameApplicationAsset gameApplication;
        [SerializeField] private LocalPlayerProvisioningAuthoring provisioning;
        [SerializeField] private LocalPlayerProvisioningEndpointRegistration provisioningRegistration;
        [SerializeField] private QaIfAdr042SharedGroupEvidence outputEvidence;
        [SerializeField] private PlayerSlotProfile player1;
        [SerializeField] private PlayerSlotProfile player2;
        [SerializeField, Min(1)] private int frameBudget = 180;

        private readonly QaCertificationRecorder _certification = new QaCertificationRecorder();
        private readonly Dictionary<PlayerSlotId, PlayerInput> _inputs = new Dictionary<PlayerSlotId, PlayerInput>();
        private readonly Dictionary<PlayerSlotId, PlayerGameplayInputReader> _readers = new Dictionary<PlayerSlotId, PlayerGameplayInputReader>();
        private Keyboard _keyboard1;
        private Keyboard _keyboard2;
        private PlayerSessionObserver _observer;
        private PlayerGameplayAvailabilityBlockToken _firstBlock;
        private PlayerGameplayAvailabilityBlockToken _secondBlock;
        private bool _started;
        private bool _terminal;
        private int _completed;
        private bool _waitSucceeded;
        private string _waitIssue = string.Empty;
        private string _firstIssue = string.Empty;
        private string _cleanupIssue = string.Empty;
        private QaCleanupDisposition _cleanup = QaCleanupDisposition.FreshBootRequired;

        private void Start()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (!_started || _terminal) return;
            StopAllCoroutines();
            RecordBlocked("Scenario was interrupted before certification completed.");
            StartCoroutine(CleanupAndPublish());
        }

        private IEnumerator Run()
        {
            if (!TryValidateAuthoring(out string issue))
            {
                RecordBlocked("Invalid authored baseline: " + issue);
                PublishTerminal();
                yield break;
            }

            yield return WaitFor(() => provisioning.RuntimeReady && TryResolveObserver(out _) &&
                _observer.IsAvailable && provisioning.RuntimeSnapshot != null &&
                outputEvidence != null && outputEvidence.Outputs.Count == 1 &&
                outputEvidence.Outputs[0] != null && outputEvidence.Outputs[0].IsReady &&
                outputEvidence.Outputs[0].Output != null && outputEvidence.Outputs[0].Output.Session != null &&
                outputEvidence.Outputs[0].Output.Session.OutputState != null &&
                outputEvidence.Occurrences.Count == 1 && outputEvidence.Occurrences[0] != null &&
                outputEvidence.Occurrences[0].Composer != null &&
                outputEvidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup != null,
                "Player Session provisioning, one Activity-scoped observer and one QA Output did not become available.");
            if (!_waitSucceeded)
            {
                RecordBlocked(_waitIssue);
                PublishTerminal();
                yield break;
            }

            if (provisioning.RuntimeSnapshot.Slots.Count != 2 ||
                provisioning.RuntimeSnapshot.Slots.Any(slot => slot.IsJoined))
            {
                RecordBlocked("Certification requires the configured two-Slot Session to begin empty.");
                PublishTerminal();
                yield break;
            }
            _completed++;

            string keyboardCreationIssue = string.Empty;
            try
            {
                _keyboard1 = InputSystem.AddDevice<Keyboard>("QA IF-ADR-044 P1");
                _keyboard2 = InputSystem.AddDevice<Keyboard>("QA IF-ADR-044 P2");
            }
            catch (Exception exception)
            {
                keyboardCreationIssue = exception.Message;
            }

            if (!string.IsNullOrEmpty(keyboardCreationIssue))
            {
                RecordBlocked("QA Keyboard creation failed: " + keyboardCreationIssue);
                yield return CleanupAndPublish();
                yield break;
            }

            if (_keyboard1 == null || !_keyboard1.added || _keyboard2 == null || !_keyboard2.added ||
                _keyboard1 == _keyboard2)
            {
                RecordBlocked("Input System did not provide two distinct QA-owned Keyboards.");
                yield return CleanupAndPublish();
                yield break;
            }

            PlayerParticipationOperationResult opened = provisioning.OpenJoining(
                ScenarioId, "Open deterministic QA joining.");
            if (opened == null || !opened.Succeeded)
            {
                RecordFail("Public OpenJoining failed. " + opened?.ToDiagnosticString());
                yield return CleanupAndPublish();
                yield break;
            }

            yield return Join(player1, _keyboard1, "0 -> P1");
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }
            yield return Join(player2, _keyboard2, "P1 -> P1+P2");
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out issue) ||
                !access.TryGetObservation(out PlayerSessionScopedObservationSnapshot before))
            {
                RecordBlocked("Activity-scoped public Player Session access is unavailable after Join. " + issue);
                yield return CleanupAndPublish();
                yield break;
            }

            PlayerSlotId p1 = player1.PlayerSlotId;
            PlayerSlotId p2 = player2.PlayerSlotId;
            if (!TryCapturePlayerState(before, p1, p2, out PlayerStateBaseline baseline, out issue))
            {
                RecordFail("Could not capture the public Player baseline. " + issue);
                yield return CleanupAndPublish();
                yield break;
            }

            PlayerGameplayAvailabilityBlockResult first = access.RequestBlockRuntimeGameplay(
                p1, ScenarioId, "Verify one consumer-owned gameplay block.");
            if (first == null || !first.Succeeded || !first.BlockToken.IsValid)
            {
                RecordFail("First public consumer block was not acquired. " + first?.Message);
                yield return CleanupAndPublish();
                yield break;
            }
            _firstBlock = first.BlockToken;
            yield return WaitFor(() => IsBlocked(p1) && IsAvailable(p2),
                "P1 did not become consumer-blocked while P2 remained available.");
            if (!_waitSucceeded || !VerifyPlayerState(baseline, p1, p2, out issue))
            {
                RecordFail(_waitSucceeded ? "Consumer block changed unrelated Player state. " + issue : _waitIssue);
                yield return CleanupAndPublish();
                yield break;
            }
            _completed++;

            PlayerGameplayAvailabilityBlockResult second = access.RequestBlockRuntimeGameplay(
                p1, ScenarioId, "Verify independent consumer blocks compose.");
            if (second == null || !second.Succeeded || !second.BlockToken.IsValid ||
                second.BlockToken == _firstBlock)
            {
                RecordFail("Second independent consumer block was not acquired with a distinct token. " + second?.Message);
                yield return CleanupAndPublish();
                yield break;
            }
            _secondBlock = second.BlockToken;
            yield return WaitFor(() => IsBlocked(p1) && IsAvailable(p2),
                "P1 did not remain blocked after a second block was acquired.");
            if (!_waitSucceeded || !VerifyPlayerState(baseline, p1, p2, out issue))
            {
                RecordFail(_waitSucceeded ? "Composed blocks changed unrelated Player state. " + issue : _waitIssue);
                yield return CleanupAndPublish();
                yield break;
            }
            _completed++;

            PlayerGameplayAvailabilityBlockResult releaseFirst = access.RequestReleaseRuntimeGameplay(
                _firstBlock, ScenarioId, "Release only the first independent block.");
            if (releaseFirst == null || releaseFirst.Status != PlayerGameplayAvailabilityBlockStatus.SucceededReleased)
            {
                RecordFail("Releasing the first consumer block failed. " + releaseFirst?.Message);
                yield return CleanupAndPublish();
                yield break;
            }
            _firstBlock = default;
            yield return WaitFor(() => IsBlocked(p1) && IsAvailable(p2),
                "Releasing one block bypassed the other active consumer block.");
            if (!_waitSucceeded) { RecordFail(_waitIssue); yield return CleanupAndPublish(); yield break; }
            _completed++;

            PlayerGameplayAvailabilityBlockResult releaseSecond = access.RequestReleaseRuntimeGameplay(
                _secondBlock, ScenarioId, "Release the remaining independent block.");
            if (releaseSecond == null || releaseSecond.Status != PlayerGameplayAvailabilityBlockStatus.SucceededReleased)
            {
                RecordFail("Releasing the final consumer block failed. " + releaseSecond?.Message);
                yield return CleanupAndPublish();
                yield break;
            }
            _secondBlock = default;
            yield return WaitFor(() => IsAvailable(p1) && IsAvailable(p2),
                "Releasing the final block did not restore gameplay availability.");
            if (!_waitSucceeded) { RecordFail(_waitIssue); yield return CleanupAndPublish(); yield break; }
            _completed++;

            PlayerGameplayAvailabilityBlockResult leaveBlock = access.RequestBlockRuntimeGameplay(
                p1, ScenarioId, "Hold a block through public Leave.");
            if (leaveBlock == null || !leaveBlock.Succeeded || !leaveBlock.BlockToken.IsValid)
            {
                RecordFail("Could not acquire the Leave invalidation block. " + leaveBlock?.Message);
                yield return CleanupAndPublish();
                yield break;
            }
            _firstBlock = leaveBlock.BlockToken;
            PlayerSlotRuntimeSnapshot joinedSlot = provisioning.RuntimeSnapshot.Slots.First(slot => slot.PlayerSlotId == p1);
            SessionPlayerLeaveResult leave = provisioning.RequestLeave(new SessionPlayerLeaveRequest(
                p1, joinedSlot.Revision, ScenarioId, "Leave while a consumer block is held."));
            if (leave == null || !leave.Succeeded)
            {
                RecordFail("Public Leave failed while a consumer block was held. " + leave?.ToDiagnosticString());
                yield return CleanupAndPublish();
                yield break;
            }
            _inputs.Remove(p1);
            _readers.Remove(p1);
            _firstBlock = default;
            yield return WaitFor(() => !IsJoined(p1) && IsAvailable(p2),
                "Leave did not clear P1 membership and its consumer block.");
            if (!_waitSucceeded) { RecordFail(_waitIssue); yield return CleanupAndPublish(); yield break; }

            PlayerGameplayAvailabilityBlockResult staleRelease = access.RequestReleaseRuntimeGameplay(
                leaveBlock.BlockToken, ScenarioId, "Confirm the old occurrence token is stale after Leave.");
            if (staleRelease == null || staleRelease.Status != PlayerGameplayAvailabilityBlockStatus.RejectedForeignOrStaleToken)
            {
                RecordFail("A block token from the departed Player occurrence was not rejected as stale. " + staleRelease?.Message);
                yield return CleanupAndPublish();
                yield break;
            }

            yield return Join(player1, _keyboard1, "P1 rejoin after blocked Leave");
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return CleanupAndPublish();
        }

        private bool TryValidateAuthoring(out string issue)
        {
            issue = string.Empty;
            if (gameApplication == null || provisioning == null || provisioningRegistration == null ||
                outputEvidence == null || player1 == null || player2 == null || player1 == player2)
            {
                issue = "Game Application, registered Manager-Provisioned endpoint, and two distinct Slot Profiles are required.";
                return false;
            }
            if (!provisioningRegistration.TryResolveAuthoring(out LocalPlayerProvisioningAuthoring registered, out issue) ||
                registered != provisioning)
            {
                issue = "The registration does not resolve this exact provisioning endpoint. " + issue;
                return false;
            }
            if (!gameApplication.PlayerSessionEnabled || gameApplication.DefaultPlayerSessionProfile == null ||
                gameApplication.DefaultPlayerSessionProfile.SupportedSlotCount != 2 ||
                gameApplication.StartupRoute == null || gameApplication.StartupRoute.StartupActivity == null ||
                !player1.TryGetPlayerSlotId(out PlayerSlotId first, out issue) ||
                !player2.TryGetPlayerSlotId(out PlayerSlotId second, out issue) || first == second)
            {
                if (string.IsNullOrEmpty(issue)) issue = "A valid two-Slot Player Session is required.";
                return false;
            }
            PlayerSessionProfile session = gameApplication.DefaultPlayerSessionProfile;
            if (session.SupportedSlots.Count != 2 || session.SupportedSlots[0] != player1 || session.SupportedSlots[1] != player2)
            {
                issue = "Game Application Player Session must preserve the exact authored P1/P2 Slot order.";
                return false;
            }
            if (!provisioning.HasPlayerInputManager || !provisioning.UsesManualJoin ||
                provisioning.PlayerInputManager.maxPlayerCount != 2)
            {
                issue = "Provisioning must use the authored manual two-Player Manager.";
                return false;
            }
            return true;
        }

        private IEnumerator Join(PlayerSlotProfile profile, Keyboard keyboard, string phase)
        {
            if (!profile.TryGetPlayerSlotId(out PlayerSlotId slotId, out string issue))
            {
                RecordBlocked(phase + ": Slot identity unavailable. " + issue);
                yield break;
            }
            LocalPlayerJoinResult joined = provisioning.RequestJoin(
                new LocalPlayerJoinRequest(ScenarioId, phase, keyboard, ControlScheme));
            if (joined == null || !joined.Succeeded || joined.PlayerInput == null || joined.LocalPlayerHost == null)
            {
                RecordFail(phase + ": public Join failed. " + joined?.ToDiagnosticString());
                yield break;
            }
            if (joined.Slot.PlayerSlotId != slotId || !joined.Slot.IsJoined ||
                joined.LocalPlayerHost.JoinedPlayerSlotId != slotId)
            {
                RecordFail(phase + ": public Join returned a different Slot occurrence.");
                yield break;
            }
            _inputs[slotId] = joined.PlayerInput;
            int readerCount = 0;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                PlayerGameplayInputReader[] readers = joined.LocalPlayerHost.GetComponentsInChildren<PlayerGameplayInputReader>(true);
                readerCount = readers.Length;
                if (readerCount > 1)
                {
                    RecordFail(phase + ": the prepared Actor contains duplicate PlayerGameplayInputReaders; found=" + readerCount + ".");
                    yield break;
                }
                if (readerCount == 1)
                {
                    _readers[slotId] = readers[0];
                    if (IsAvailable(slotId))
                    {
                        _completed++;
                        yield break;
                    }
                }
                yield return null;
            }
            string finalReaderState = "<missing>";
            if (readerCount == 1 && _readers.TryGetValue(slotId, out PlayerGameplayInputReader finalReader) && finalReader != null)
            {
                PlayerInput finalInput = _inputs.TryGetValue(slotId, out PlayerInput currentInput) ? currentInput : null;
                InputActionMap finalMap = finalInput != null ? finalInput.currentActionMap : null;
                finalReaderState = "binding=" + finalReader.HasCurrentGameplayBinding +
                    ", gameplayReady=" + finalReader.GameplayReady +
                    ", availability=" + finalReader.RuntimeGameplayAvailability +
                    ", available=" + finalReader.RuntimeGameplayAvailable +
                    ", playerInputEnabled=" + (finalInput != null && finalInput.enabled) +
                    ", actionMap='" + (finalMap != null ? finalMap.name : "<none>") +
                    "', actionMapEnabled=" + (finalMap != null && finalMap.enabled);
            }
            _readers.Remove(slotId);
            RecordFail(phase + ": Actor preparation did not expose one current PlayerGameplayInputReader with available gameplay input within " +
                frameBudget + " frames; finalReaderCount=" + readerCount + "; finalReaderState=" + finalReaderState + ".");
        }

        private bool TryCapturePlayerState(PlayerSessionScopedObservationSnapshot observation,
            PlayerSlotId p1, PlayerSlotId p2, out PlayerStateBaseline baseline, out string issue)
        {
            baseline = default;
            issue = string.Empty;
            if (!TryGetSlot(observation, p1, out PlayerSessionScopedSlotObservation first) ||
                !TryGetSlot(observation, p2, out PlayerSessionScopedSlotObservation second) ||
                !first.IsJoined || !second.IsJoined || !first.IsPhysicallyMaterialized || !second.IsPhysicallyMaterialized ||
                !_inputs.TryGetValue(p1, out PlayerInput input1) || !_inputs.TryGetValue(p2, out PlayerInput input2) ||
                input1 == null || input2 == null || input1.currentActionMap == null || input2.currentActionMap == null ||
                !_readers.TryGetValue(p1, out PlayerGameplayInputReader reader1) ||
                !_readers.TryGetValue(p2, out PlayerGameplayInputReader reader2))
            {
                issue = "Both Joined Players require current public Actor preparation, input and gameplay-reader evidence.";
                return false;
            }
            CameraOutputAuthoring output = outputEvidence.Outputs[0].Output;
            CameraOutputState cameraState = output.Session.OutputState;
            Transform[] subjects = CaptureCameraSubjects();
            if (!cameraState.HasActiveAssignment || !cameraState.HasRetainedNormalOccurrence ||
                subjects == null || subjects.Length != 2)
            {
                issue = "The baseline requires the retained shared Assignment occurrence and exactly two current camera Subjects.";
                return false;
            }
            baseline = new PlayerStateBaseline(first.Slot.Revision, second.Slot.Revision,
                first.CurrentActor.ActorEvidence.ActorId, second.CurrentActor.ActorEvidence.ActorId,
                input1.devices.ToArray(), input2.devices.ToArray(), input1.currentActionMap,
                input2.currentActionMap, reader1, reader2, observation.Participation.Revision,
                output.OutputId, cameraState.ActiveAssignmentId,
                cameraState.RetainedNormalOccurrence, subjects);
            return IsAvailable(p1) && IsAvailable(p2);
        }

        private bool VerifyPlayerState(PlayerStateBaseline baseline, PlayerSlotId p1, PlayerSlotId p2, out string issue)
        {
            issue = string.Empty;
            if (!TryGetAccess(out IPlayerSessionScopedAccess access, out issue) ||
                !access.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) ||
                !TryGetSlot(observation, p1, out PlayerSessionScopedSlotObservation first) ||
                !TryGetSlot(observation, p2, out PlayerSessionScopedSlotObservation second))
            {
                if (string.IsNullOrEmpty(issue)) issue = "Current public Player observation is unavailable.";
                return false;
            }
            if (!first.IsJoined || !second.IsJoined || !first.IsPhysicallyMaterialized || !second.IsPhysicallyMaterialized ||
                first.CurrentActor.ActorEvidence.ActorId != baseline.Actor1 ||
                second.CurrentActor.ActorEvidence.ActorId != baseline.Actor2 ||
                first.Slot.Revision != baseline.SlotRevision1 || second.Slot.Revision != baseline.SlotRevision2 ||
                observation.Participation.Revision != baseline.SessionRevision ||
                !SameDevices(_inputs[p1], baseline.Devices1) || !SameDevices(_inputs[p2], baseline.Devices2) ||
                !baseline.Reader1.GameplayReady || !baseline.Reader2.GameplayReady ||
                baseline.Map1.enabled || !baseline.Map2.enabled ||
                outputEvidence == null || outputEvidence.Outputs.Count != 1 ||
                outputEvidence.Outputs[0] == null || outputEvidence.Outputs[0].Output == null ||
                outputEvidence.Outputs[0].Output.Session == null || outputEvidence.Outputs[0].Output.Session.OutputState == null ||
                outputEvidence.Outputs[0].Output.OutputId != baseline.OutputId ||
                outputEvidence.Outputs[0].Output.Session.OutputState.ActiveAssignmentId != baseline.AssignmentId ||
                outputEvidence.Outputs[0].Output.Session.OutputState.RetainedNormalOccurrence != baseline.CameraOccurrence ||
                !SameCameraSubjects(baseline.CameraSubjects))
            {
                issue = "Consumer block changed membership, Actor, Slot revision, paired devices, or the unblocked Player posture.";
                return false;
            }
            return true;
        }

        private bool TryGetAccess(out IPlayerSessionScopedAccess access, out string issue)
        {
            access = null;
            if (!TryResolveObserver(out issue)) return false;
            return _observer.TryGetAccess(out access, out issue);
        }

        private bool TryResolveObserver(out string issue)
        {
            issue = string.Empty;
            ActivityAsset activity = gameApplication != null && gameApplication.StartupRoute != null
                ? gameApplication.StartupRoute.StartupActivity
                : null;
            if (activity == null || activity.ActivityContentProfile == null || !activity.ActivityContentProfile.HasScenes)
            {
                issue = "The configured startup Activity has no declared scene for its scoped Player Session observer.";
                _observer = null;
                return false;
            }
            string[] activityScenePaths = activity.ActivityContentProfile.Scenes
                .Select(entry => entry.ScenePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            PlayerSessionObserver[] candidates = FindObjectsByType<PlayerSessionObserver>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(observer => observer != null && observer.isActiveAndEnabled &&
                    observer.Scope == LocalPlayerProvisioningConsumerScope.Activity &&
                    activityScenePaths.Contains(observer.gameObject.scene.path) &&
                    observer.gameObject.scene.IsValid() && observer.gameObject.scene.isLoaded)
                .ToArray();
            if (candidates.Length != 1)
            {
                issue = "Expected exactly one enabled Activity-scoped PlayerSessionObserver; found=" + candidates.Length + ".";
                _observer = null;
                return false;
            }
            _observer = candidates[0];
            return true;
        }

        private bool IsJoined(PlayerSlotId slot) => provisioning.RuntimeSnapshot != null &&
            provisioning.RuntimeSnapshot.Slots.Any(value => value.PlayerSlotId == slot && value.IsJoined);

        private bool IsBlocked(PlayerSlotId slot) => _readers.TryGetValue(slot, out PlayerGameplayInputReader reader) &&
            reader != null && reader.HasCurrentGameplayBinding && !reader.RuntimeGameplayAvailable &&
            reader.RuntimeGameplayAvailability == PlayerGameplayInputAvailability.BlockedByConsumer &&
            _inputs.TryGetValue(slot, out PlayerInput input) && input != null && input.currentActionMap != null &&
            !input.currentActionMap.enabled;

        private bool IsAvailable(PlayerSlotId slot) => _readers.TryGetValue(slot, out PlayerGameplayInputReader reader) &&
            reader != null && reader.HasCurrentGameplayBinding && reader.RuntimeGameplayAvailable &&
            reader.RuntimeGameplayAvailability == PlayerGameplayInputAvailability.Allowed &&
            _inputs.TryGetValue(slot, out PlayerInput input) && input != null && input.currentActionMap != null &&
            input.currentActionMap.enabled;

        private bool TryGetSlot(PlayerSessionScopedObservationSnapshot observation, PlayerSlotId id,
            out PlayerSessionScopedSlotObservation result)
        {
            foreach (PlayerSessionScopedSlotObservation slot in observation.Slots)
                if (slot.Slot.PlayerSlotId == id) { result = slot; return true; }
            result = default;
            return false;
        }

        private Transform[] CaptureCameraSubjects()
        {
            CinemachineTargetGroup group = outputEvidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup;
            return group.Targets.Select(target => target.Object).ToArray();
        }

        private bool SameCameraSubjects(Transform[] expected)
        {
            if (outputEvidence.Occurrences.Count != 1 || outputEvidence.Occurrences[0] == null ||
                outputEvidence.Occurrences[0].Composer == null ||
                outputEvidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup == null)
                return false;
            var actual = outputEvidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup.Targets;
            if (actual == null || actual.Count != expected.Length) return false;
            for (int index = 0; index < expected.Length; index++)
                if (!ReferenceEquals(actual[index].Object, expected[index])) return false;
            return true;
        }

        private static bool SameDevices(PlayerInput input, InputDevice[] baseline) => input != null &&
            input.devices.Count == baseline.Length && baseline.All(device => input.devices.Contains(device));

        private IEnumerator WaitFor(Func<bool> predicate, string failure)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (predicate()) { _waitSucceeded = true; _waitIssue = string.Empty; yield break; }
                yield return null;
            }
            _waitSucceeded = false;
            _waitIssue = failure;
        }

        private bool HasDivergence => _certificationHasDiverged;
        private bool _certificationHasDiverged;

        private void RecordBlocked(string issue)
        {
            _certificationHasDiverged = true;
            if (string.IsNullOrEmpty(_firstIssue)) _firstIssue = issue;
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, issue);
        }

        private void RecordFail(string issue)
        {
            _certificationHasDiverged = true;
            if (string.IsNullOrEmpty(_firstIssue)) _firstIssue = issue;
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, issue);
        }

        private IEnumerator CleanupAndPublish()
        {
            if (_firstBlock.IsValid || _secondBlock.IsValid)
            {
                if (TryGetAccess(out IPlayerSessionScopedAccess access, out string issue))
                {
                    if (_firstBlock.IsValid)
                    {
                        PlayerGameplayAvailabilityBlockResult release = access.RequestReleaseRuntimeGameplay(_firstBlock, ScenarioId, "QA cleanup.");
                        if (release == null || release.Status != PlayerGameplayAvailabilityBlockStatus.SucceededReleased)
                            _cleanupIssue = Append(_cleanupIssue, "First owned block could not be released during cleanup.");
                    }
                    if (_secondBlock.IsValid)
                    {
                        PlayerGameplayAvailabilityBlockResult release = access.RequestReleaseRuntimeGameplay(_secondBlock, ScenarioId, "QA cleanup.");
                        if (release == null || release.Status != PlayerGameplayAvailabilityBlockStatus.SucceededReleased)
                            _cleanupIssue = Append(_cleanupIssue, "Second owned block could not be released during cleanup.");
                    }
                }
                else _cleanupIssue = "Could not release consumer blocks during cleanup: " + issue;
                _firstBlock = default;
                _secondBlock = default;
            }

            if (provisioning != null && provisioning.RuntimeSnapshot != null)
            {
                foreach (PlayerSlotRuntimeSnapshot slot in provisioning.RuntimeSnapshot.Slots.Where(item => item.IsJoined).ToArray())
                {
                    SessionPlayerLeaveResult leave = provisioning.RequestLeave(new SessionPlayerLeaveRequest(
                        slot.PlayerSlotId, slot.Revision, ScenarioId, "Terminal QA cleanup public Leave."));
                    if (leave == null || !leave.Succeeded)
                        _cleanupIssue = Append(_cleanupIssue, "Public Leave cleanup failed for " + slot.PlayerSlotId.StableText + ".");
                }
            }
            for (int frame = 0; frame < frameBudget && provisioning != null &&
                provisioning.RuntimeSnapshot != null && provisioning.RuntimeSnapshot.Slots.Any(slot => slot.IsJoined); frame++)
                yield return null;
            if (provisioning != null && provisioning.RuntimeSnapshot != null &&
                provisioning.RuntimeSnapshot.Slots.Any(slot => slot.IsJoined))
                _cleanupIssue = Append(_cleanupIssue, "Joined Players remained after public Leave cleanup.");
            if (outputEvidence == null || outputEvidence.Outputs.Count != 1 || outputEvidence.Outputs[0] == null ||
                outputEvidence.Outputs[0].Output == null || outputEvidence.Outputs[0].Output.Session == null ||
                outputEvidence.Outputs[0].Output.Session.OutputState == null ||
                !outputEvidence.Outputs[0].Output.Session.OutputState.IsFallbackAvailable ||
                !outputEvidence.Outputs[0].Output.Session.OutputState.IsFallbackCovering ||
                outputEvidence.Occurrences.Count != 1 || outputEvidence.Occurrences[0] == null ||
                outputEvidence.Occurrences[0].Composer == null ||
                outputEvidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup == null ||
                outputEvidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup.Targets == null ||
                outputEvidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup.Targets.Count != 0)
                _cleanupIssue = Append(_cleanupIssue, "Shared camera fallback/zero-Subject cleanup baseline was not restored.");
            yield return null;
            if (_keyboard1 != null && _keyboard1.added) InputSystem.RemoveDevice(_keyboard1);
            if (_keyboard2 != null && _keyboard2.added) InputSystem.RemoveDevice(_keyboard2);
            if ((_keyboard1 != null && _keyboard1.added) || (_keyboard2 != null && _keyboard2.added))
                _cleanupIssue = Append(_cleanupIssue, "QA-owned virtual Keyboard cleanup did not complete.");
            _cleanup = string.IsNullOrEmpty(_cleanupIssue)
                ? QaCleanupDisposition.BaselineRestored
                : QaCleanupDisposition.FreshBootRequired;
            PublishTerminal();
        }

        private void PublishTerminal()
        {
            if (_terminal) return;
            _terminal = true;
            if (!_certificationHasDiverged) _certification.RecordPass();
            QaCertificationResult result = _certification.CreateResult(ScenarioId, _cleanup, _cleanupIssue);
            string status = result.Verdict switch
            {
                QaCertificationVerdict.Pass => "Passed",
                QaCertificationVerdict.Fail => "Failed",
                _ => "Blocked"
            };
            string next = _completed < ExpectedCases.Length ? ExpectedCases[_completed] : "none";
            Debug.Log($"[IF-ADR-044] status='{status}' verdict='{result.Verdict.ToString().ToUpperInvariant()}' cases='{_completed}/{ExpectedCases.Length}' next='{Sanitize(next)}' completed='{_completed}' missing='{Sanitize(next)}' execution='{Sanitize(result.FirstCausalDivergence)}' unwind='public-leave-requests' cleanup='{result.CleanupDisposition}' cleanupIssue='{Sanitize(result.CleanupIssue)}'.");
        }

        private static string Append(string first, string second) =>
            string.IsNullOrEmpty(first) ? second : first + " | " + second;

        private static string Sanitize(string value) => (value ?? string.Empty)
            .Replace("\\", "\\\\").Replace("'", "\\'")
            .Replace("\r", "\\r").Replace("\n", "\\n");

        private readonly struct PlayerStateBaseline
        {
            public PlayerStateBaseline(int slotRevision1, int slotRevision2, ActorId actor1, ActorId actor2,
                InputDevice[] devices1, InputDevice[] devices2, InputActionMap map1, InputActionMap map2,
                PlayerGameplayInputReader reader1, PlayerGameplayInputReader reader2, int sessionRevision,
                CameraOutputId outputId, SessionCameraAssignmentId assignmentId, CameraOccurrenceIdentity cameraOccurrence,
                Transform[] cameraSubjects)
            {
                SlotRevision1 = slotRevision1; SlotRevision2 = slotRevision2; Actor1 = actor1; Actor2 = actor2;
                Devices1 = devices1; Devices2 = devices2; Map1 = map1; Map2 = map2;
                Reader1 = reader1; Reader2 = reader2; SessionRevision = sessionRevision;
                OutputId = outputId; AssignmentId = assignmentId; CameraOccurrence = cameraOccurrence;
                CameraSubjects = cameraSubjects;
            }
            public int SlotRevision1 { get; }
            public int SlotRevision2 { get; }
            public ActorId Actor1 { get; }
            public ActorId Actor2 { get; }
            public InputDevice[] Devices1 { get; }
            public InputDevice[] Devices2 { get; }
            public InputActionMap Map1 { get; }
            public InputActionMap Map2 { get; }
            public PlayerGameplayInputReader Reader1 { get; }
            public PlayerGameplayInputReader Reader2 { get; }
            public int SessionRevision { get; }
            public CameraOutputId OutputId { get; }
            public SessionCameraAssignmentId AssignmentId { get; }
            public CameraOccurrenceIdentity CameraOccurrence { get; }
            public Transform[] CameraSubjects { get; }
        }
    }
}

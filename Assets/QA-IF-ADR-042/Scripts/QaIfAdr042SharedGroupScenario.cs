using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.QaFramework.Certification;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr042
{
    [DisallowMultipleComponent]
    public sealed class QaIfAdr042SharedGroupScenario : MonoBehaviour
    {
        private const string ScenarioId = "IF-ADR-042-SHARED-GROUP";
        private const string QaControlScheme = "QA.Keyboard";
        private static readonly string[] ExpectedCases =
        {
            "0 baseline",
            "0 -> P1",
            "P1 -> P1+P2",
            "P1 Actor/Subject replacement",
            "P1+P2 -> P2",
            "P2 -> 0",
            "0 -> P1"
        };

        [SerializeField] private GameApplicationAsset gameApplication;
        [SerializeField] private LocalPlayerProvisioningAuthoring provisioning;
        [SerializeField] private LocalPlayerProvisioningEndpointRegistration provisioningRegistration;
        private PlayerSessionObserver playerSessionObserver;
        [SerializeField] private QaIfAdr042SharedGroupEvidence evidence;
        [SerializeField] private PlayerSlotProfile player1;
        [SerializeField] private PlayerSlotProfile player2;
        [SerializeField] private ActorProfile player1Replacement;
        [SerializeField] private SessionCameraAssignmentAsset assignment;
        [SerializeField, Min(1)] private int frameBudget = 180;

        private readonly QaCertificationRecorder _certification = new QaCertificationRecorder();
        private readonly Dictionary<PlayerSlotId, PlayerInput> _inputs = new Dictionary<PlayerSlotId, PlayerInput>();
        private readonly Dictionary<PlayerSlotId, LocalPlayerHostAuthoring> _hosts = new Dictionary<PlayerSlotId, LocalPlayerHostAuthoring>();
        private Keyboard _player1Device;
        private Keyboard _player2Device;
        private CameraOutputAuthoring _output;
        private CameraRigComposer _occurrenceComposer;
        private CinemachineTargetGroup _targetGroup;
        private CameraOutputId _outputId;
        private SessionCameraAssignmentId _assignmentId;
        private CameraOccurrenceIdentity _occurrenceId;
        private ActorProfile _player1DefaultProfile;
        private ActorId _player1ReplacementActorId;
        private Transform _player1ReplacementSubject;
        private bool _joiningWasOpened;
        private bool _replacementApplied;
        private bool _selectionRestored;
        private bool _started;
        private bool _terminal;
        private bool _waitSucceeded;
        private string _waitIssue = string.Empty;
        private string _firstIssue = string.Empty;
        private string _cleanupIssue = string.Empty;
        private string _restoreIssue = string.Empty;
        private int _completedCases;
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
            if (!TryValidateAuthoredComposition(out string issue))
            {
                RecordBlocked("Invalid authored precondition: " + issue);
                PublishTerminal(false);
                yield break;
            }

            yield return WaitUntil(() => provisioning.RuntimeReady &&
                TryResolveActivityObserver(out _) && playerSessionObserver.IsAvailable && evidence.Outputs.Count == 1 &&
                evidence.Occurrences.Count == 1 && evidence.Outputs[0] != null &&
                evidence.Outputs[0].IsReady && evidence.Occurrences[0] != null &&
                evidence.Occurrences[0].Composer != null &&
                evidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup != null,
                "Framework did not expose the Activity Player Session access, one initialized Output, and one materialized SharedGroup occurrence.");
            if (!_waitSucceeded)
            {
                RecordBlocked(_waitIssue);
                PublishTerminal(false);
                yield break;
            }

            if (!playerSessionObserver.TryValidateConfiguration(out issue) ||
                playerSessionObserver.Scope != LocalPlayerProvisioningConsumerScope.Activity)
            {
                RecordBlocked("Activity-scoped Player Session Observer is invalid. " + issue);
                PublishTerminal(false);
                yield break;
            }

            if (!CaptureBaseline(out issue))
            {
                RecordBlocked("Invalid runtime baseline: " + issue);
                PublishTerminal(false);
                yield break;
            }

            if (!TryCreateQaDevices(out issue))
            {
                RecordBlocked("QA input infrastructure is unavailable: " + issue);
                yield return CleanupAndPublish();
                yield break;
            }

            if (!VerifyStage("0 baseline", Array.Empty<PlayerSlotId>(), out issue))
            {
                RecordBlocked("Invalid zero-Player baseline: " + issue);
                yield return CleanupAndPublish();
                yield break;
            }
            _completedCases++;

            PlayerParticipationOperationResult opening = provisioning.OpenJoining(
                ScenarioId, "Open deterministic QA joining before public ManagerProvisioned joins.");
            if (opening == null || !opening.Succeeded)
            {
                RecordFail("Public OpenJoining failed before the first Join. result='" + opening?.ToDiagnosticString() + "'.");
                yield return CleanupAndPublish();
                yield break;
            }
            _joiningWasOpened = true;

            yield return JoinAndVerify(player1, _player1Device, "0 -> P1", new[] { player1.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return JoinAndVerify(player2, _player2Device, "P1 -> P1+P2",
                new[] { player1.PlayerSlotId, player2.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return ReplacePlayer1ActorAndVerify();
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return RestorePlayer1ActorAndVerify(
                "P1 original Actor/Subject restoration");
            if (!_selectionRestored)
            {
                RecordFail("P1 original Actor/Subject could not be restored while P1 was Joined. " + _restoreIssue);
                yield return CleanupAndPublish();
                yield break;
            }

            yield return LeaveAndVerify(player1, "P1+P2 -> P2", new[] { player2.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return LeaveAndVerify(player2, "P2 -> 0", Array.Empty<PlayerSlotId>());
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return JoinAndVerify(player1, _player1Device, "0 -> P1", new[] { player1.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return CleanupAndPublish();
        }

        private bool TryValidateAuthoredComposition(out string issue)
        {
            issue = string.Empty;
            if (gameApplication == null || provisioning == null ||
                provisioningRegistration == null ||
                evidence == null || player1 == null || player2 == null ||
                player1Replacement == null || assignment == null)
            {
                issue = "Game Application, explicit provisioning registration, evidence, two Slots, replacement Actor Profile, and Assignment are required.";
                return false;
            }

            if (!provisioningRegistration.TryResolveAuthoring(
                    out LocalPlayerProvisioningAuthoring registered, out issue) ||
                registered != provisioning)
            {
                issue = "Provisioning registration does not resolve the scenario's exact ManagerProvisioned endpoint. " + issue;
                return false;
            }

            if (!provisioning.HasPlayerInputManager || !provisioning.UsesManualJoin ||
                !provisioning.UsesCSharpJoinNotifications || !provisioning.IsManagerPrefabMaterialized ||
                provisioning.PlayerInputManager.maxPlayerCount != 2 ||
                provisioning.PlayerInputManager.splitScreen ||
                provisioning.PlayerInputManager.gameObject != provisioning.gameObject)
            {
                issue = "The explicit provisioning endpoint must own one manual, non-split-screen two-Player Manager using its authored Host prefab.";
                return false;
            }

            PlayerInput hostInput = provisioning.LocalPlayerHostPrefab != null
                ? provisioning.LocalPlayerHostPrefab.GetComponent<PlayerInput>()
                : null;
            LocalPlayerHostAuthoring host = provisioning.LocalPlayerHostPrefab != null
                ? provisioning.LocalPlayerHostPrefab.GetComponent<LocalPlayerHostAuthoring>()
                : null;
            if (hostInput == null || host == null || host.PlayerInput != hostInput ||
                host.ActorMount == null || host.PlayerActorRuntimeHostPrefab == null ||
                !host.PlayerActorRuntimeHostPrefab.TryValidateConfiguration(out issue))
            {
                issue = "Manager Host must expose its root PlayerInput, Actor Mount and valid PlayerActorRuntimeHost prefab. " + issue;
                return false;
            }

            if (hostInput.actions == null || hostInput.defaultActionMap != "Player" ||
                hostInput.defaultControlScheme != QaControlScheme ||
                hostInput.actions.FindActionMap("Player") == null ||
                hostInput.actions.FindAction("Move") == null)
            {
                issue = "Host PlayerInput must expose the Player map and QA.Keyboard action configuration.";
                return false;
            }
            InputControlScheme? scheme = hostInput.actions.FindControlScheme(QaControlScheme);
            if (!scheme.HasValue || !scheme.Value.deviceRequirements.Any(
                    requirement => requirement.controlPath == "<Keyboard>" && !requirement.isOptional))
            {
                issue = "PlayerInput Actions must define QA.Keyboard with a required Keyboard device.";
                return false;
            }

            if (!gameApplication.PlayerSessionEnabled ||
                gameApplication.DefaultPlayerSessionProfile == null ||
                gameApplication.DefaultPlayerSessionProfile.SupportedSlotCount != 2 ||
                !gameApplication.DefaultPlayerSessionProfile.TryValidate(out issue) ||
                gameApplication.DefaultPlayerSessionProfile.SupportedSlots.Count != 2 ||
                gameApplication.DefaultPlayerSessionProfile.SupportedSlots[0] != player1 ||
                gameApplication.DefaultPlayerSessionProfile.SupportedSlots[1] != player2)
            {
                if (string.IsNullOrEmpty(issue)) issue = "Player Session must declare exactly P1 then P2.";
                return false;
            }

            if (!gameApplication.StartupRoute || !gameApplication.StartupRoute.HasPrimaryScene ||
                gameApplication.StartupRoute.StartupActivity == null ||
                gameApplication.CameraSession == null || gameApplication.CameraSession.OutputPrefabs.Count != 1 ||
                gameApplication.StartupCameraAssignments.Count != 1 ||
                gameApplication.StartupCameraAssignments[0] != assignment)
            {
                issue = "Game Application requires a valid startup Route/Activity, one physical Output and this single startup Assignment.";
                return false;
            }

            if (!player1.TryGetPlayerSlotId(out PlayerSlotId firstSlot, out issue) ||
                !player2.TryGetPlayerSlotId(out PlayerSlotId secondSlot, out issue) ||
                firstSlot == secondSlot || firstSlot != PlayerSlotId.Player1 || secondSlot != PlayerSlotId.Player2)
            {
                issue = "QA Slot profiles must expose the distinct ordered Player1 and Player2 identities. " + issue;
                return false;
            }

            if (!assignment.TryBuild(out SessionCameraAssignment resolved, out issue) ||
                resolved.OccurrenceMode != CameraOccurrenceMode.SharedGroup ||
                resolved.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots ||
                resolved.TargetPolicy != CameraTargetPolicy.MemberActorTargets ||
                resolved.MemberSlots.Count != 2 || resolved.Outputs.Count != 1 ||
                resolved.MemberSlots[0] != firstSlot || resolved.MemberSlots[1] != secondSlot)
            {
                issue = "Assignment must be SharedGroup / ExplicitPlayerSlots / MemberActorTargets for exactly P1, P2 and one Output. " + issue;
                return false;
            }

            ActorCameraSubjectAuthoring subject = host.PlayerActorRuntimeHostPrefab
                .GetComponent<ActorCameraSubjectAuthoring>();
            if (subject == null || !subject.TryValidateConfiguration(out issue))
            {
                issue = "Player Actor Runtime Host must carry an explicit valid Actor Camera Subject on its Actor root. " + issue;
                return false;
            }

            if (!player1.DefaultActorProfile || !player2.DefaultActorProfile ||
                !player1Replacement.TryGetActorProfileId(out _, out issue) ||
                player1Replacement.ActorKind != ActorKind.Player ||
                player1Replacement.ActorRole != ActorRole.Protagonist ||
                player1Replacement == player1.DefaultActorProfile)
            {
                issue = "P1/P2 default profiles and a distinct valid replacement Player Actor Profile are required. " + issue;
                return false;
            }

            return true;
        }

        private bool TryResolveActivityObserver(out string issue)
        {
            issue = string.Empty;
            ActivityAsset activity = gameApplication != null && gameApplication.StartupRoute != null
                ? gameApplication.StartupRoute.StartupActivity
                : null;
            if (activity == null || activity.ActivityContentProfile == null ||
                !activity.ActivityContentProfile.HasScenes)
            {
                issue = "The active startup Activity has no declared content scene for its scoped observer.";
                return false;
            }

            string[] activityScenePaths = activity.ActivityContentProfile.Scenes
                .Select(entry => entry.ScenePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
            PlayerSessionObserver[] candidates = UnityEngine.Object.FindObjectsByType<PlayerSessionObserver>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(observer => observer != null && observer.isActiveAndEnabled &&
                    observer.Scope == LocalPlayerProvisioningConsumerScope.Activity &&
                    activityScenePaths.Contains(observer.gameObject.scene.path) &&
                    observer.gameObject.scene.IsValid() && observer.gameObject.scene.isLoaded)
                .ToArray();
            if (candidates.Length != 1)
            {
                issue = "Expected one enabled Activity-scoped PlayerSessionObserver in the current Activity content scenes; found '" +
                    candidates.Length + "'.";
                playerSessionObserver = null;
                return false;
            }

            playerSessionObserver = candidates[0];
            return true;
        }

        private bool CaptureBaseline(out string issue)
        {
            issue = string.Empty;
            if (provisioning.RuntimeSnapshot == null ||
                provisioning.RuntimeSnapshot.Slots.Count != 2 ||
                provisioning.RuntimeSnapshot.Slots.Any(slot => slot.IsJoined))
            {
                issue = "Player Session is not at the expected zero-Player baseline.";
                return false;
            }

            if (!provisioning.RuntimeSnapshot.JoiningOpen)
            {
                _joiningWasOpened = false;
            }
            else
            {
                issue = "The QA baseline requires Joining closed so the Scenario can restore that exact state.";
                return false;
            }

            if (evidence.Outputs.Count != 1 || evidence.Occurrences.Count != 1)
            {
                issue = $"Expected exactly one runtime Output and one occurrence. outputs='{evidence.Outputs.Count}' occurrences='{evidence.Occurrences.Count}' evidence='{evidence.Diagnostic}'.";
                return false;
            }

            _output = evidence.Outputs[0].Output;
            _occurrenceComposer = evidence.Occurrences[0].Composer;
            _targetGroup = _occurrenceComposer != null
                ? _occurrenceComposer.FrameworkOwnedGroupTargetGroup
                : null;
            if (_output == null || !_output.IsInitialized || _targetGroup == null ||
                _output.Session == null || _output.Session.OutputState == null)
            {
                issue = "Public Output Session or occurrence TargetGroup evidence is unavailable.";
                return false;
            }

            CameraOutputState state = _output.Session.OutputState;
            if (!state.HasActiveAssignment || !state.HasRetainedNormalOccurrence ||
                !state.IsFallbackAvailable || !state.IsFallbackCovering)
            {
                issue = "Startup Assignment, retained occurrence, and zero-Subject Fallback must all be present before the first action.";
                return false;
            }

            _outputId = _output.OutputId;
            _assignmentId = state.ActiveAssignmentId;
            _occurrenceId = state.RetainedNormalOccurrence;
            _player1DefaultProfile = player1.DefaultActorProfile;
            return true;
        }

        private bool TryCreateQaDevices(out string issue)
        {
            issue = string.Empty;
            try
            {
                _player1Device = InputSystem.AddDevice<Keyboard>("QA IF-ADR-042 P1");
                _player2Device = InputSystem.AddDevice<Keyboard>("QA IF-ADR-042 P2");
            }
            catch (Exception exception)
            {
                issue = exception.Message;
                return false;
            }

            if (_player1Device == null || !_player1Device.added ||
                _player2Device == null || !_player2Device.added ||
                _player1Device == _player2Device)
            {
                issue = "Input System did not provide two distinct added QA-owned Keyboards.";
                return false;
            }
            return true;
        }

        private IEnumerator JoinAndVerify(PlayerSlotProfile profile, Keyboard device,
            string stage, IReadOnlyList<PlayerSlotId> expectedJoined)
        {
            if (device == null || !device.added)
            {
                RecordBlocked($"{stage}: required QA-owned Keyboard is unavailable; the Player contract was not exercised.");
                yield break;
            }

            var request = new LocalPlayerJoinRequest(ScenarioId, stage, device, QaControlScheme);
            LocalPlayerJoinResult result = provisioning.RequestJoin(request);
            if (result == null || !result.Succeeded || result.PlayerInput == null ||
                result.LocalPlayerHost == null)
            {
                RecordFail($"{stage}: public Join did not return a current PlayerInput and Local Player Host. result='{result?.ToDiagnosticString()}' provisioning='{provisioning.RuntimeDiagnostic}'.");
                yield break;
            }

            if (!profile.TryGetPlayerSlotId(out PlayerSlotId slotId, out string slotIssue) ||
                !result.Slot.IsJoined || result.Slot.PlayerSlotId != slotId ||
                !result.LocalPlayerHost.IsJoined || !result.LocalPlayerHost.HasJoinedSlot ||
                result.LocalPlayerHost.JoinedPlayerSlotId != slotId ||
                !ReferenceEquals(result.LocalPlayerHost.PlayerInput, result.PlayerInput))
            {
                RecordFail($"{stage}: public Join did not return coherent physical Host evidence for expected Slot='{(profile != null ? profile.PlayerSlotIdText : "<invalid>")}'. returnedSlot='{result.Slot.PlayerSlotId.StableText}' hostJoined='{result.LocalPlayerHost.IsJoined}' hostSlot='{result.LocalPlayerHost.JoinedPlayerSlotId.StableText}' playerInputMatchesHost='{ReferenceEquals(result.LocalPlayerHost.PlayerInput, result.PlayerInput)}'. {slotIssue}");
                yield break;
            }

            _inputs[slotId] = result.PlayerInput;
            _hosts[slotId] = result.LocalPlayerHost;
            yield return WaitForStage(stage, expectedJoined);
            if (!_waitSucceeded) RecordFail(_waitIssue);
            else _completedCases++;
        }

        private IEnumerator LeaveAndVerify(PlayerSlotProfile profile, string stage,
            IReadOnlyList<PlayerSlotId> expectedJoined)
        {
            if (!profile.TryGetPlayerSlotId(out PlayerSlotId slotId, out string issue))
            {
                RecordBlocked($"{stage}: authored Slot identity is unavailable before Leave. {issue}");
                yield break;
            }

            if (!TryGetCurrentSlot(slotId, out PlayerSlotRuntimeSnapshot slot) || !slot.IsJoined)
            {
                RecordFail($"{stage}: public Player Session snapshot has no Joined Slot '{slotId.StableText}'.");
                yield break;
            }

            SessionPlayerLeaveResult result = provisioning.RequestLeave(
                new SessionPlayerLeaveRequest(slotId, slot.Revision, ScenarioId, stage));
            if (result == null || !result.Succeeded)
            {
                RecordFail($"{stage}: public Leave failed. result='{result?.ToDiagnosticString()}'.");
                yield break;
            }

            _inputs.Remove(slotId);
            _hosts.Remove(slotId);
            yield return null;
            yield return WaitForStage(stage, expectedJoined);
            if (!_waitSucceeded) RecordFail(_waitIssue);
            else _completedCases++;
        }

        private IEnumerator ReplacePlayer1ActorAndVerify()
        {
            PlayerSlotId slotId = player1.PlayerSlotId;
            if (!playerSessionObserver.TryGetAccess(
                    out IPlayerSessionScopedAccess access, out string accessIssue) ||
                !access.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation))
            {
                RecordBlocked("Activity scoped Player Session access could not be acquired for public prepared Actor replacement. " + accessIssue);
                yield break;
            }

            string subjectIssue = string.Empty;
            if (!TryGetObservedSlot(observation, slotId,
                    out PlayerSessionScopedSlotObservation slotObservation) ||
                !slotObservation.IsPhysicallyMaterialized ||
                !slotObservation.CurrentActor.HasCurrentActor ||
                slotObservation.CurrentActor.ActorEvidence.ActorProfileId != _player1DefaultProfile.ActorProfileId ||
                !_hosts.TryGetValue(slotId, out LocalPlayerHostAuthoring host) || host == null ||
                !TryResolveSubject(host, out Transform previousObservation, out _, out subjectIssue))
            {
                RecordFail("P1 has no current public Actor/Subject evidence before replacement. " + subjectIssue);
                yield break;
            }

            ActorId previousActorId = slotObservation.CurrentActor.ActorEvidence.ActorId;
            var request = new PlayerPreparedActorReplacementRequest(
                slotId,
                player1Replacement,
                ScenarioId,
                "Replace P1's prepared Actor while preserving the SharedGroup occurrence.",
                slotObservation.Slot.SelectionRevision,
                observation.Participation.Revision);
            PlayerPreparedActorReplacementResult result = access.RequestReplacePreparedActor(request);
            _replacementApplied = result != null && result.ReplacementCommitted;
            if (result == null || !result.ReplacementCommitted || result.CleanupPending ||
                !result.CurrentActor.HasActorEvidence || result.CurrentActor.ActorEvidence.ActorId == previousActorId ||
                result.CurrentActor.ActorEvidence.ActorProfileId != player1Replacement.ActorProfileId)
            {
                RecordFail($"Public prepared Actor replacement did not commit a distinct current Actor. status='{result?.Status}' cleanupPending='{result?.CleanupPending}' message='{result?.Message}'.");
                yield break;
            }

            _player1ReplacementActorId = result.CurrentActor.ActorEvidence.ActorId;
            yield return WaitForStage("P1 Actor/Subject replacement", new[]
                { player1.PlayerSlotId, player2.PlayerSlotId }, previousObservation);
            if (!_waitSucceeded)
            {
                RecordFail(_waitIssue);
                yield break;
            }

            if (!_hosts.TryGetValue(player1.PlayerSlotId,
                    out LocalPlayerHostAuthoring currentHost) || currentHost == null ||
                !TryResolveSubject(currentHost, out _player1ReplacementSubject,
                    out _, out subjectIssue))
            {
                RecordFail("P1 replacement Subject could not be resolved after SharedGroup reconciliation. " + subjectIssue);
                yield break;
            }

            _completedCases++;
        }

        private IEnumerator RestorePlayer1ActorAndVerify(string stage)
        {
            _restoreIssue = string.Empty;
            if (!_replacementApplied)
            {
                _selectionRestored = true;
                yield break;
            }

            if (!playerSessionObserver.TryGetAccess(
                    out IPlayerSessionScopedAccess access,
                    out _restoreIssue) ||
                !access.TryGetObservation(
                    out PlayerSessionScopedObservationSnapshot observation))
            {
                if (string.IsNullOrEmpty(_restoreIssue))
                    _restoreIssue = "Activity-scoped Player Session observation is unavailable.";
                yield break;
            }

            if (!TryGetObservedSlot(observation, player1.PlayerSlotId,
                    out PlayerSessionScopedSlotObservation slot) || !slot.IsJoined)
            {
                _restoreIssue = "P1 is not Joined; the public Actor replacement contract cannot restore its selection.";
                yield break;
            }

            string subjectIssue = string.Empty;
            if (!_hosts.TryGetValue(player1.PlayerSlotId,
                    out LocalPlayerHostAuthoring host) || host == null ||
                !TryResolveSubject(host, out Transform currentSubject, out _, out subjectIssue))
            {
                _restoreIssue = "P1's current Actor Subject is unavailable before restoring its original Actor. " + subjectIssue;
                yield break;
            }

            bool alreadySelectedOriginal =
                slot.Slot.SelectedActorProfile == _player1DefaultProfile;
            Transform subjectToRemove = alreadySelectedOriginal
                ? _player1ReplacementSubject
                : currentSubject;
            if (!alreadySelectedOriginal)
            {
                if (slot.Slot.SelectedActorProfile != player1Replacement ||
                    slot.CurrentActor.ActorEvidence.ActorProfileId !=
                        player1Replacement.ActorProfileId)
                {
                    _restoreIssue = "P1's current prepared Actor is not the certified replacement or the original Actor profile.";
                    yield break;
                }

                var request = new PlayerPreparedActorReplacementRequest(
                    player1.PlayerSlotId,
                    _player1DefaultProfile,
                    ScenarioId,
                    "Restore P1's original prepared Actor while P1 remains Joined.",
                    slot.Slot.SelectionRevision,
                    observation.Participation.Revision);
                PlayerPreparedActorReplacementResult result =
                    access.RequestReplacePreparedActor(request);
                if (result == null || !result.ReplacementCommitted ||
                    result.CleanupPending || !result.CurrentActor.HasActorEvidence ||
                    result.CurrentActor.ActorEvidence.ActorProfileId !=
                        _player1DefaultProfile.ActorProfileId ||
                    result.CurrentActor.ActorEvidence.ActorId == _player1ReplacementActorId)
                {
                    _restoreIssue = "Public prepared Actor replacement did not restore P1's original Actor profile. " +
                        (result != null
                            ? $"status='{result.Status}' committed='{result.ReplacementCommitted}' cleanupPending='{result.CleanupPending}' message='{result.Message}'."
                            : "No replacement result was returned.");
                    yield break;
                }
            }
            else if (!slot.CurrentActor.HasCurrentActor ||
                slot.CurrentActor.ActorEvidence.ActorProfileId !=
                    _player1DefaultProfile.ActorProfileId)
            {
                _restoreIssue = "P1's selected original Actor profile does not match its current prepared Actor evidence.";
                yield break;
            }

            PlayerSlotId[] expectedJoined = observation.Participation.Slots
                .Where(candidate => candidate.IsJoined)
                .Select(candidate => candidate.PlayerSlotId)
                .ToArray();
            yield return WaitForStage(stage, expectedJoined, subjectToRemove);
            if (!_waitSucceeded)
            {
                _restoreIssue = _waitIssue;
                yield break;
            }

            if (!playerSessionObserver.TryGetAccess(
                    out access,
                    out _restoreIssue) ||
                !access.TryGetObservation(
                    out PlayerSessionScopedObservationSnapshot restoredObservation) ||
                !TryGetObservedSlot(restoredObservation, player1.PlayerSlotId,
                    out PlayerSessionScopedSlotObservation restoredSlot) ||
                !restoredSlot.IsJoined || !restoredSlot.IsLogicalActorPrepared ||
                restoredSlot.Slot.SelectedActorProfile != _player1DefaultProfile ||
                !restoredSlot.CurrentActor.HasCurrentActor ||
                restoredSlot.CurrentActor.ActorEvidence.ActorProfileId !=
                    _player1DefaultProfile.ActorProfileId ||
                restoredSlot.CurrentActor.ActorEvidence.ActorId == _player1ReplacementActorId)
            {
                if (string.IsNullOrEmpty(_restoreIssue))
                    _restoreIssue = "P1's original prepared Actor was not observable after SharedGroup reconciliation.";
                yield break;
            }

            _selectionRestored = true;
        }

        private IEnumerator WaitForStage(string stage, IReadOnlyList<PlayerSlotId> expectedJoined,
            Transform mustBeAbsent = null)
        {
            _waitSucceeded = false;
            _waitIssue = string.Empty;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (VerifyStage(stage, expectedJoined, out _waitIssue, mustBeAbsent))
                {
                    _waitSucceeded = true;
                    yield break;
                }
                yield return null;
            }
            _waitIssue = $"{stage}: SharedGroup did not converge within {frameBudget} frames. last='{_waitIssue}' evidence='{evidence.Diagnostic}'.";
        }

        private bool VerifyStage(string stage, IReadOnlyList<PlayerSlotId> expectedJoined,
            out string issue, Transform mustBeAbsent = null)
        {
            issue = string.Empty;
            if (evidence == null || evidence.Outputs.Count != 1 ||
                evidence.Occurrences.Count != 1 || evidence.Outputs[0] == null ||
                !evidence.Outputs[0].IsReady || evidence.Occurrences[0] == null)
            {
                issue = $"{stage}: expected exactly one ready Output and one occurrence probe. {evidence?.Diagnostic}";
                return false;
            }

            CameraOutputAuthoring output = evidence.Outputs[0].Output;
            CameraRigComposer occurrence = evidence.Occurrences[0].Composer;
            if (output == null || occurrence == null ||
                !ReferenceEquals(output, _output) || !ReferenceEquals(occurrence, _occurrenceComposer) ||
                !ReferenceEquals(occurrence.FrameworkOwnedGroupTargetGroup, _targetGroup))
            {
                issue = $"{stage}: registered Output or SharedGroup occurrence reference changed.";
                return false;
            }

            CameraOutputState state = output.Session != null ? output.Session.OutputState : null;
            if (state == null || !state.HasActiveAssignment || state.ActiveAssignmentId != _assignmentId ||
                output.OutputId != _outputId || !state.HasRetainedNormalOccurrence ||
                state.RetainedNormalOccurrence != _occurrenceId ||
                state.RetainedNormalOccurrence.AssignmentId != _assignmentId ||
                state.RetainedNormalOccurrence.OutputId != _outputId)
            {
                issue = $"{stage}: Assignment, Output, or occurrence identity changed. activeAssignment='{state?.ActiveAssignmentId}' occurrence='{state?.RetainedNormalOccurrence}'.";
                return false;
            }

            if (output.UnityCamera == null || !output.UnityCamera.enabled ||
                !output.UnityCamera.gameObject.activeInHierarchy)
            {
                issue = $"{stage}: physical Output Camera is not enabled and active in the hierarchy.";
                return false;
            }

            PlayerParticipationSnapshot participation = provisioning.RuntimeSnapshot;
            PlayerSlotId[] observed = participation.Slots.Where(slot => slot.IsJoined)
                .Select(slot => slot.PlayerSlotId).ToArray();
            if (participation.Slots.Count != 2 || observed.Length != expectedJoined.Count ||
                expectedJoined.Except(observed).Any() || observed.Except(expectedJoined).Any())
            {
                issue = $"{stage}: public Session membership='{DescribeSlots(observed)}', expected='{DescribeSlots(expectedJoined)}'.";
                return false;
            }

            if (expectedJoined.Count > 0)
            {
                string accessIssue = string.Empty;
                if (playerSessionObserver == null ||
                    !playerSessionObserver.TryGetAccess(
                        out IPlayerSessionScopedAccess access,
                        out accessIssue))
                {
                    issue = $"{stage}: Activity-scoped Player Session observation is unavailable while waiting for contextual Actor preparation. {accessIssue}";
                    return false;
                }

                if (!access.TryGetObservation(
                        out PlayerSessionScopedObservationSnapshot observation))
                {
                    issue = $"{stage}: Activity-scoped Player Session observation could not be read while waiting for contextual Actor preparation.";
                    return false;
                }

                if (!observation.HasCurrentActivityOccurrence)
                {
                    issue = $"{stage}: Activity-scoped Player Session observation has no current Activity occurrence.";
                    return false;
                }

                foreach (PlayerSlotId slotId in expectedJoined)
                {
                    if (!TryGetObservedSlot(
                            observation,
                            slotId,
                            out PlayerSessionScopedSlotObservation slotObservation) ||
                        !slotObservation.IsJoined ||
                        !slotObservation.HasHostEvidence ||
                        !slotObservation.HostEvidence.IsConfirmed ||
                        !slotObservation.HostEvidence.HasContextualProjection ||
                        !slotObservation.IsLogicalActorPrepared ||
                        !slotObservation.IsPhysicallyMaterialized ||
                        !slotObservation.HasCurrentActorEvidence ||
                        !slotObservation.CurrentActor.HasCurrentActor)
                    {
                        issue = $"{stage}: Slot '{slotId.StableText}' has not reached confirmed contextual Host assignment and prepared current Actor evidence. {observation.Diagnostic}";
                        return false;
                    }
                }
            }

            CinemachineTargetGroup group = occurrence.FrameworkOwnedGroupTargetGroup;
            if (group == null)
            {
                issue = $"{stage}: registered Assignment occurrence has no public FrameworkOwnedGroupTargetGroup.";
                return false;
            }

            if (expectedJoined.Count == 0)
            {
                if (group.Targets == null || group.Targets.Count != 0)
                {
                    issue = $"{stage}: zero Subjects requires an empty TargetGroup; actual='{(group.Targets != null ? group.Targets.Count : -1)}'.";
                    return false;
                }
                if (!state.IsFallbackAvailable || !state.IsFallbackCovering ||
                    output.FallbackCameraRig == null || output.FallbackCameraRig.CinemachineCamera == null ||
                    !output.FallbackCameraRig.CinemachineCamera.enabled ||
                    occurrence.CinemachineCamera == null || occurrence.CinemachineCamera.enabled ||
                    state.HasPresentedNormalOccurrence)
                {
                    issue = $"{stage}: zero Subjects requires exactly the Output Fallback rig covering the Output; normal occurrence must not be presented.";
                    return false;
                }
                return true;
            }

            if (state.IsFallbackCovering || !state.HasPresentedNormalOccurrence ||
                output.FallbackCameraRig == null || output.FallbackCameraRig.CinemachineCamera == null ||
                output.FallbackCameraRig.CinemachineCamera.enabled ||
                occurrence.CinemachineCamera == null || !occurrence.CinemachineCamera.enabled)
            {
                issue = $"{stage}: current Player Subjects require the normal SharedGroup occurrence to be presented and the Output Fallback released.";
                return false;
            }

            var subjects = new List<SubjectExpectation>(expectedJoined.Count);
            foreach (PlayerSlotId slotId in expectedJoined)
            {
                string subjectIssue = string.Empty;
                if (!_hosts.TryGetValue(slotId, out LocalPlayerHostAuthoring host) || host == null ||
                    !TryResolveSubject(host, out Transform observation, out float radius, out subjectIssue))
                {
                    issue = $"{stage}: current Actor Subject for Slot '{slotId.StableText}' is not observable through its joined Host. {subjectIssue}";
                    return false;
                }
                subjects.Add(new SubjectExpectation(observation, radius));
            }

            if (group.Targets == null || group.Targets.Count != subjects.Count)
            {
                issue = $"{stage}: TargetGroup member count='{(group.Targets != null ? group.Targets.Count : -1)}', expected current Observation count='{subjects.Count}'.";
                return false;
            }

            for (int subjectIndex = 0; subjectIndex < subjects.Count; subjectIndex++)
            {
                SubjectExpectation expected = subjects[subjectIndex];
                int occurrences = 0;
                for (int targetIndex = 0; targetIndex < group.Targets.Count; targetIndex++)
                {
                    CinemachineTargetGroup.Target target = group.Targets[targetIndex];
                    if (target.Object == expected.Observation) occurrences++;
                }
                if (occurrences != 1)
                {
                    issue = $"{stage}: current Observation '{expected.Observation.name}' appears '{occurrences}' times; exactly one membership is required.";
                    return false;
                }
            }

            for (int targetIndex = 0; targetIndex < group.Targets.Count; targetIndex++)
            {
                CinemachineTargetGroup.Target target = group.Targets[targetIndex];
                int expectedIndex = subjects.FindIndex(subject => subject.Observation == target.Object);
                if (expectedIndex < 0)
                {
                    issue = $"{stage}: TargetGroup contains stale or non-current member '{target.Object}'.";
                    return false;
                }

                float expectedRadius = subjects[expectedIndex].Radius > 0f
                    ? subjects[expectedIndex].Radius
                    : occurrence.GroupMemberRadius;
                if (!Approximately(target.Radius, expectedRadius) ||
                    !Approximately(target.Weight, occurrence.GroupMemberWeight))
                {
                    issue = $"{stage}: projected radius/weight for '{subjects[expectedIndex].Observation.name}' is radius='{target.Radius}' weight='{target.Weight}', expected radius='{expectedRadius}' weight='{occurrence.GroupMemberWeight}'.";
                    return false;
                }
            }

            if (mustBeAbsent != null && group.Targets.Any(target => target.Object == mustBeAbsent))
            {
                issue = $"{stage}: replaced Actor Observation '{mustBeAbsent.name}' remains in TargetGroup.";
                return false;
            }
            return true;
        }

        private bool TryResolveSubject(LocalPlayerHostAuthoring host, out Transform observation,
            out float radius, out string issue)
        {
            observation = null;
            radius = 0f;
            issue = string.Empty;
            if (host == null || host.ActorMount == null)
            {
                issue = "Joined Local Player Host or Actor Mount is unavailable.";
                return false;
            }

            ActorDeclaration[] actors = host.ActorMount.GetComponentsInChildren<ActorDeclaration>(true);
            if (actors.Length != 1)
            {
                issue = $"Joined Host exposes '{actors.Length}' current Actor declarations under its public Actor Mount; exactly one is required.";
                return false;
            }

            ActorCameraSubjectAuthoring[] subjects = actors[0].GetComponents<ActorCameraSubjectAuthoring>();
            if (subjects.Length != 1 || !subjects[0].TryResolveSubject(
                    actors[0], out observation, out radius, out issue))
            {
                if (string.IsNullOrEmpty(issue)) issue = "Current Actor must expose exactly one valid Actor Camera Subject.";
                return false;
            }
            return true;
        }

        private bool TryGetCurrentSlot(PlayerSlotId slotId, out PlayerSlotRuntimeSnapshot snapshot)
        {
            PlayerParticipationSnapshot session = provisioning.RuntimeSnapshot;
            foreach (PlayerSlotRuntimeSnapshot slot in session.Slots)
            {
                if (slot.PlayerSlotId != slotId) continue;
                snapshot = slot;
                return true;
            }
            snapshot = default;
            return false;
        }

        private static bool TryGetObservedSlot(PlayerSessionScopedObservationSnapshot observation,
            PlayerSlotId slotId, out PlayerSessionScopedSlotObservation slot)
        {
            foreach (PlayerSessionScopedSlotObservation candidate in observation.Slots)
            {
                if (candidate.Slot.PlayerSlotId != slotId) continue;
                slot = candidate;
                return true;
            }
            slot = default;
            return false;
        }

        private IEnumerator CleanupAndPublish()
        {
            if (provisioning == null || !provisioning.RuntimeReady)
            {
                SetCleanupIssue("Provisioning endpoint is unavailable; public Leaves cannot be confirmed.");
                PublishTerminal(false);
                yield break;
            }

            if (_replacementApplied && !_selectionRestored)
            {
                if (TryGetCurrentSlot(player1.PlayerSlotId, out PlayerSlotRuntimeSnapshot p1) &&
                    p1.IsJoined)
                {
                    yield return RestorePlayer1ActorAndVerify(
                        "cleanup P1 original Actor/Subject restoration");
                    if (!_selectionRestored)
                        SetCleanupIssue("Public P1 Actor restoration failed during cleanup: " + _restoreIssue);
                }
                else
                {
                    SetCleanupIssue("P1 is no longer Joined; the public contract does not allow restoring Actor selection and cleanup will not issue a temporary Join.");
                }
            }

            int leaveBudget = Mathf.Max(2, provisioning.RuntimeSnapshot.Slots.Count);
            for (int pass = 0; pass < leaveBudget; pass++)
            {
                PlayerSlotRuntimeSnapshot joined = provisioning.RuntimeSnapshot.Slots
                    .FirstOrDefault(slot => slot.IsJoined);
                if (!joined.IsJoined) break;

                SessionPlayerLeaveResult leave = provisioning.RequestLeave(
                    new SessionPlayerLeaveRequest(joined.PlayerSlotId, joined.Revision,
                        ScenarioId, "Unwind public Player Leave during QA cleanup."));
                if (leave == null || !leave.Succeeded)
                {
                    SetCleanupIssue("Public Leave cleanup failed: " + leave?.ToDiagnosticString());
                    break;
                }
                _inputs.Remove(joined.PlayerSlotId);
                _hosts.Remove(joined.PlayerSlotId);
                yield return null;
            }

            if (_joiningWasOpened)
            {
                PlayerParticipationOperationResult close = provisioning.CloseJoining(
                    ScenarioId, "Restore the initially closed QA Player joining state.");
                if (close == null || !close.Succeeded)
                    SetCleanupIssue("Public CloseJoining cleanup failed: " + close?.ToDiagnosticString());
                _joiningWasOpened = false;
            }

            yield return null;
            RemoveQaDevice(_player1Device, "P1");
            RemoveQaDevice(_player2Device, "P2");

            string baselineIssue = string.Empty;
            bool baselineRestored = string.IsNullOrEmpty(_cleanupIssue) &&
                VerifyStage("cleanup baseline", Array.Empty<PlayerSlotId>(), out baselineIssue) &&
                !provisioning.RuntimeSnapshot.JoiningOpen &&
                (_player1Device == null || !_player1Device.added) &&
                (_player2Device == null || !_player2Device.added);
            if (!baselineRestored && string.IsNullOrEmpty(_cleanupIssue))
                SetCleanupIssue("Baseline verification failed after public unwind: " + baselineIssue);
            _cleanup = baselineRestored
                ? QaCleanupDisposition.BaselineRestored
                : QaCleanupDisposition.FreshBootRequired;

            if (_completedCases == ExpectedCases.Length && string.IsNullOrEmpty(_firstIssue) && baselineRestored)
                _certification.RecordPass();
            else if (string.IsNullOrEmpty(_firstIssue))
                RecordBlocked("Scenario did not complete all cases and restore its baseline.");
            PublishTerminal(baselineRestored);
        }

        private void RemoveQaDevice(Keyboard device, string player)
        {
            if (device == null || !device.added) return;
            try
            {
                InputSystem.RemoveDevice(device);
                if (device.added) SetCleanupIssue($"QA-owned Keyboard for {player} remained added after removal.");
            }
            catch (Exception exception)
            {
                SetCleanupIssue($"QA-owned Keyboard for {player} could not be removed after public Leave: {exception.Message}");
            }
        }

        private IEnumerator WaitUntil(Func<bool> condition, string timeoutIssue)
        {
            _waitSucceeded = false;
            _waitIssue = string.Empty;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (condition())
                {
                    _waitSucceeded = true;
                    yield break;
                }
                yield return null;
            }
            _waitIssue = timeoutIssue + " " +
                (provisioning != null ? provisioning.RuntimeDiagnostic : string.Empty) + " " +
                (evidence != null
                    ? $"evidenceOutputs='{evidence.Outputs.Count}' evidenceOccurrences='{evidence.Occurrences.Count}' evidenceDiagnostic='{evidence.Diagnostic}'"
                    : "evidence='<missing>'") + " " +
                (playerSessionObserver != null
                    ? $"observerAvailable='{playerSessionObserver.IsAvailable}' observerScope='{playerSessionObserver.Scope}' observerDiagnostic='{playerSessionObserver.Diagnostic}'"
                    : "observer='<not-found-in-Activity-content>'");
        }

        private static bool Approximately(float actual, float expected) =>
            Mathf.Abs(actual - expected) <= 0.0001f;

        private static string DescribeSlots(IEnumerable<PlayerSlotId> slots) =>
            string.Join(",", slots.Select(slot => slot.StableText));

        private static string Sanitize(string value) => (value ?? string.Empty)
            .Replace("\\", "\\\\").Replace("'", "\\'")
            .Replace("\r", "\\r").Replace("\n", "\\n");

        private bool HasDivergence => !string.IsNullOrEmpty(_firstIssue);

        private void RecordFail(string issue)
        {
            if (HasDivergence) return;
            _firstIssue = string.IsNullOrWhiteSpace(issue) ? "Functional divergence without diagnostic." : issue;
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, _firstIssue);
        }

        private void RecordBlocked(string issue)
        {
            if (HasDivergence) return;
            _firstIssue = string.IsNullOrWhiteSpace(issue) ? "A required precondition was not satisfied." : issue;
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, _firstIssue);
        }

        private void SetCleanupIssue(string issue)
        {
            if (string.IsNullOrEmpty(_cleanupIssue)) _cleanupIssue = issue;
        }

        private void PublishTerminal(bool baselineRestored)
        {
            if (_terminal) return;
            _terminal = true;
            if (_completedCases == ExpectedCases.Length && string.IsNullOrEmpty(_firstIssue) && baselineRestored)
                _certification.RecordPass();
            else if (string.IsNullOrEmpty(_firstIssue))
                RecordBlocked("Certification did not reach its expected terminal baseline.");

            QaCertificationResult result = _certification.CreateResult(
                ScenarioId, _cleanup, _cleanupIssue);
            string next = _completedCases < ExpectedCases.Length
                ? ExpectedCases[_completedCases]
                : string.Empty;
            string status = result.Verdict == QaCertificationVerdict.Pass
                ? "Passed"
                : result.Verdict == QaCertificationVerdict.Blocked ? "Blocked" : "Failed";
            string message = $"[{ScenarioId}] status='{status}' verdict='{result.Verdict.ToString().ToUpperInvariant()}' cases='{_completedCases}/{ExpectedCases.Length}' next='{Sanitize(next)}' completed='{_completedCases}' missing='{Sanitize(next)}' assignment='{_assignmentId}' output='{_outputId}' occurrence='{_occurrenceId}' targetMembers='{(_targetGroup != null ? _targetGroup.Targets.Count : -1)}' cleanup='{result.CleanupDisposition}' execution='{Sanitize(result.FirstCausalDivergence)}' cleanupIssue='{Sanitize(result.CleanupIssue)}'.";
            if (result.Verdict == QaCertificationVerdict.Pass) Debug.Log(message, this);
            else Debug.LogError(message, this);
        }

        private readonly struct SubjectExpectation
        {
            internal SubjectExpectation(Transform observation, float radius)
            {
                Observation = observation;
                Radius = radius;
            }

            internal Transform Observation { get; }
            internal float Radius { get; }
        }
    }
}

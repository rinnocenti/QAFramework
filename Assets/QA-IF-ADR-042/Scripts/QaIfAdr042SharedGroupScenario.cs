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
using Immersive.QaFramework.PlayerSession;
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
            "P1+P2 -> P2",
            "P2 -> 0",
            "0 -> P1"
        };

        [SerializeField] private GameApplicationAsset gameApplication;
        [SerializeField] private PlayerSessionObserver routeSessionObserver;
        [SerializeField] private PlayerSessionJoinTrigger joinTrigger;
        private PlayerSessionObserver playerSessionObserver;
        [SerializeField] private QaIfAdr042SharedGroupEvidence evidence;
        [SerializeField] private PlayerSlotProfile player1;
        [SerializeField] private PlayerSlotProfile player2;
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
        private bool _joiningWasOpened;
        private bool _started;
        private bool _terminal;
        private bool _waitSucceeded;
        private string _waitIssue = string.Empty;
        private string _firstIssue = string.Empty;
        private string _cleanupIssue = string.Empty;
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

            string readinessIssue = string.Empty;
            yield return WaitUntil(
                () => TryResolveStartupReadiness(out readinessIssue),
                "Framework did not expose the Route Player Session access, one initialized Output, and one materialized SharedGroup occurrence.",
                () => readinessIssue);
            if (!_waitSucceeded)
            {
                RecordBlocked(_waitIssue);
                PublishTerminal(false);
                yield break;
            }

            if (!routeSessionObserver.TryValidateConfiguration(out issue) ||
                routeSessionObserver.Scope != LocalPlayerProvisioningConsumerScope.Route ||
                !playerSessionObserver.TryValidateConfiguration(out issue) ||
                playerSessionObserver.Scope != LocalPlayerProvisioningConsumerScope.Activity ||
                joinTrigger == null || joinTrigger.Scope != LocalPlayerProvisioningConsumerScope.Route)
            {
                RecordBlocked("Route-scoped Player Session Observer or Join trigger is invalid. " + issue);
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

            PlayerParticipationOperationResult opening =
                QaPlayerSessionCommandAccess.OpenJoining(routeSessionObserver);
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
            if (gameApplication == null || routeSessionObserver == null || joinTrigger == null ||
                evidence == null || player1 == null || player2 == null || assignment == null)
            {
                issue = "Game Application, Route-scoped Observer and Join trigger, evidence, two Slots, and Assignment are required.";
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

            if (!player1.DefaultActorProfile || !player2.DefaultActorProfile)
            {
                issue = "P1/P2 default Actor Profiles are required.";
                return false;
            }

            return true;
        }

        private bool TryResolveRouteCommands(out string issue)
        {
            issue = string.Empty;
            if (routeSessionObserver == null || joinTrigger == null ||
                routeSessionObserver.Scope != LocalPlayerProvisioningConsumerScope.Route ||
                joinTrigger.Scope != LocalPlayerProvisioningConsumerScope.Route)
            {
                issue = "The Route scene must own one explicit Route-scoped Observer and Join trigger.";
                return false;
            }
            PlayerSessionOpenJoiningCommandTrigger[] opens =
                routeSessionObserver.GetComponentsInChildren<PlayerSessionOpenJoiningCommandTrigger>(true);
            PlayerSessionCloseJoiningCommandTrigger[] closes =
                routeSessionObserver.GetComponentsInChildren<PlayerSessionCloseJoiningCommandTrigger>(true);
            PlayerSessionLeaveCommandTrigger[] leaves =
                routeSessionObserver.GetComponentsInChildren<PlayerSessionLeaveCommandTrigger>(true);
            if (!routeSessionObserver.IsAvailable)
            {
                issue = "Route-scoped Player Session Observer is unavailable. " + routeSessionObserver.Diagnostic;
                return false;
            }
            if (opens.Length != 1 || opens[0] == null)
            {
                issue = "Route requires exactly one Open Joining command; found " + opens.Length + ".";
                return false;
            }
            if (!opens[0].isActiveAndEnabled || !opens[0].IsScopedAccessAvailable)
            {
                issue = "Route Open Joining command is disabled or unbound. " + opens[0].ScopeBindingDiagnostic;
                return false;
            }
            if (!opens[0].TryValidateConfiguration(out issue))
            {
                issue = "Route Open Joining command configuration is invalid. " + issue;
                return false;
            }
            if (closes.Length != 1 || closes[0] == null)
            {
                issue = "Route requires exactly one Close Joining command; found " + closes.Length + ".";
                return false;
            }
            if (!closes[0].isActiveAndEnabled || !closes[0].IsScopedAccessAvailable)
            {
                issue = "Route Close Joining command is disabled or unbound. " + closes[0].ScopeBindingDiagnostic;
                return false;
            }
            if (!closes[0].TryValidateConfiguration(out issue))
            {
                issue = "Route Close Joining command configuration is invalid. " + issue;
                return false;
            }
            if (leaves.Length != 2)
            {
                issue = "Route requires exactly two Leave commands, one per Player Slot; found " + leaves.Length + ".";
                return false;
            }
            foreach (PlayerSessionLeaveCommandTrigger leave in leaves)
            {
                if (leave == null)
                {
                    issue = "Route contains a missing Leave command component.";
                    return false;
                }
                if (!leave.isActiveAndEnabled || !leave.IsScopedAccessAvailable)
                {
                    issue = "Route Leave command is disabled or unbound. " + leave.ScopeBindingDiagnostic;
                    return false;
                }
                if (!leave.TryValidateConfiguration(out issue))
                {
                    issue = "Route Leave command configuration is invalid. " + issue;
                    return false;
                }
            }
            if (leaves.Count(candidate => candidate.PlayerSlot == player1) != 1)
            {
                issue = "Route requires exactly one Leave targeting Player Slot P1; " +
                    "check the serialized playerSlot reference on 'Leave Player 1'.";
                return false;
            }
            if (leaves.Count(candidate => candidate.PlayerSlot == player2) != 1)
            {
                issue = "Route requires exactly one Leave targeting Player Slot P2; " +
                    "check the serialized playerSlot reference on 'Leave Player 2'.";
                return false;
            }
            if (joinTrigger == null || !joinTrigger.isActiveAndEnabled ||
                joinTrigger.Scope != LocalPlayerProvisioningConsumerScope.Route)
            {
                issue = "Route Player Session Join trigger is missing, disabled, or has an invalid scope.";
                return false;
            }
            return true;
        }

        private bool TryResolveStartupReadiness(out string issue)
        {
            if (!TryResolveActivityObserver(out issue)) return false;
            if (playerSessionObserver == null || !playerSessionObserver.IsAvailable)
            {
                issue = "Activity-scoped Player Session observation is unavailable. " +
                    (playerSessionObserver != null ? playerSessionObserver.Diagnostic : "Observer not found.");
                return false;
            }
            if (!TryResolveRouteCommands(out issue)) return false;
            if (evidence == null)
            {
                issue = "SharedGroup evidence asset is missing.";
                return false;
            }
            if (evidence.Outputs.Count != 1)
            {
                issue = "Expected exactly one initialized Camera Output; evidence contains " + evidence.Outputs.Count +
                    " Output reference(s). " + evidence.Diagnostic;
                return false;
            }
            if (evidence.Outputs[0] == null || !evidence.Outputs[0].IsReady)
            {
                issue = "The authored Camera Output has not initialized. " + evidence.Diagnostic;
                return false;
            }
            if (evidence.Occurrences.Count != 1)
            {
                issue = "Expected exactly one materialized SharedGroup occurrence; evidence contains " +
                    evidence.Occurrences.Count + " occurrence reference(s). " + evidence.Diagnostic;
                return false;
            }
            if (evidence.Occurrences[0] == null || evidence.Occurrences[0].Composer == null ||
                evidence.Occurrences[0].Composer.FrameworkOwnedGroupTargetGroup == null)
            {
                issue = "The SharedGroup occurrence has no materialized Composer/TargetGroup. " + evidence.Diagnostic;
                return false;
            }
            issue = string.Empty;
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
                issue = "The startup Activity has no declared content scene for its Actor observer.";
                return false;
            }

            string[] paths = activity.ActivityContentProfile.Scenes
                .Select(entry => entry.ScenePath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (paths.Length == 0)
            {
                issue = "The startup Activity has no saved content-scene paths for its Actor observer.";
                playerSessionObserver = null;
                return false;
            }

            var candidates = new List<PlayerSessionObserver>();
            var sceneStates = new List<string>(paths.Length);
            foreach (string path in paths)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    sceneStates.Add(path + "=not-loaded");
                    continue;
                }

                sceneStates.Add(path + "=loaded");
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    candidates.AddRange(root.GetComponentsInChildren<PlayerSessionObserver>(true));
                }
            }

            if (candidates.Count != 1)
            {
                string observed = string.Join(", ", candidates
                    .Where(observer => observer != null)
                    .Select(observer => observer.gameObject.name +
                        "(scope=" + observer.Scope +
                        ", enabled=" + observer.isActiveAndEnabled +
                        ", scene=" + observer.gameObject.scene.path + ")"));
                issue = "Expected exactly one PlayerSessionObserver in the startup Activity's declared loaded content scenes; found '" +
                    candidates.Count + "'. scenes='" + string.Join(",", sceneStates) +
                    "' observers='" + (string.IsNullOrWhiteSpace(observed) ? "<none>" : observed) + "'.";
                playerSessionObserver = null;
                return false;
            }

            PlayerSessionObserver candidate = candidates[0];
            if (candidate == null || !candidate.isActiveAndEnabled ||
                candidate.Scope != LocalPlayerProvisioningConsumerScope.Activity)
            {
                issue = "The sole PlayerSessionObserver in the startup Activity's declared content scenes must be enabled and Activity-scoped; " +
                    (candidate != null
                        ? "observed scope='" + candidate.Scope + "' enabled='" + candidate.isActiveAndEnabled +
                            "' scene='" + candidate.gameObject.scene.path + "'."
                        : "the scene returned a missing component.");
                playerSessionObserver = null;
                return false;
            }

            playerSessionObserver = candidate;
            return true;
        }

        private bool CaptureBaseline(out string issue)
        {
            issue = string.Empty;
            if (!routeSessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) || snapshot == null ||
                snapshot.Slots.Count != 2 || snapshot.Slots.Any(slot => slot.IsJoined))
            {
                issue = "Player Session is not at the expected zero-Player baseline.";
                return false;
            }

            if (!snapshot.JoiningOpen)
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

            if (!profile.TryGetPlayerSlotId(out PlayerSlotId slotId, out string slotIssue))
            {
                RecordFail($"{stage}: configured Slot identity is unavailable. {slotIssue}");
                yield break;
            }
            LocalPlayerJoinOutcome outcome = joinTrigger.RequestJoin(
                new LocalPlayerJoinRequest(ScenarioId, stage, device, QaControlScheme));
            if (outcome == null || outcome.Code != LocalPlayerJoinOutcomeCode.Joined ||
                outcome.AdmissionState != LocalPlayerJoinAdmissionState.Admitted)
            {
                RecordFail($"{stage}: scoped public Join outcome was not Joined/Admitted. code='{outcome?.Code}' admission='{outcome?.AdmissionState}'.");
                yield break;
            }

            if (!QaPlayerSessionJoinEvidenceReader.TryCapture(routeSessionObserver, slotId, device,
                    out QaPlayerSessionJoinEvidence joined, out string evidenceIssue) ||
                !joined.Slot.IsJoined || joined.Slot.PlayerSlotId != slotId ||
                !joined.Host.IsJoined || !joined.Host.HasJoinedSlot ||
                joined.Host.JoinedPlayerSlotId != slotId ||
                !ReferenceEquals(joined.Host.PlayerInput, joined.PlayerInput))
            {
                RecordFail($"{stage}: public Session/Host evidence did not correlate the expected Slot and exact requested device. {evidenceIssue}");
                yield break;
            }

            _inputs[slotId] = joined.PlayerInput;
            _hosts[slotId] = joined.Host;
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

            SessionPlayerLeaveResult result = QaPlayerSessionCommandAccess.Leave(
                routeSessionObserver,
                profile);
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

            if (!routeSessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot participation) || participation == null)
            {
                issue = $"{stage}: Route-scoped Session snapshot is unavailable.";
                return false;
            }
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
                if (playerSessionObserver == null ||
                    !playerSessionObserver.TryGetObservation(
                        out PlayerSessionScopedObservationSnapshot observation))
                {
                    issue = $"{stage}: Activity-scoped Player Session observation is unavailable while waiting for contextual Actor preparation. {playerSessionObserver?.Diagnostic}";
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
            if (!routeSessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot session) || session == null)
            {
                snapshot = default;
                return false;
            }
            foreach (PlayerSlotRuntimeSnapshot slot in session.Slots)
            {
                if (slot.PlayerSlotId != slotId) continue;
                snapshot = slot;
                return true;
            }
            snapshot = default;
            return false;
        }

        private PlayerParticipationSnapshot CurrentSnapshot =>
            routeSessionObserver != null &&
            routeSessionObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot)
                ? snapshot
                : null;

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
            string accessIssue = string.Empty;
            if (routeSessionObserver == null || !routeSessionObserver.IsAvailable ||
                !TryResolveRouteCommands(out accessIssue))
            {
                SetCleanupIssue("Route-scoped Player Session commands are unavailable; public Leaves cannot be confirmed. " + accessIssue);
                PublishTerminal(false);
                yield break;
            }

            PlayerParticipationSnapshot beforeCleanup = CurrentSnapshot;
            int leaveBudget = Mathf.Max(2, beforeCleanup != null ? beforeCleanup.Slots.Count : 2);
            for (int pass = 0; pass < leaveBudget; pass++)
            {
                PlayerParticipationSnapshot current = CurrentSnapshot;
                if (current == null) break;
                PlayerSlotRuntimeSnapshot joined = current.Slots
                    .FirstOrDefault(slot => slot.IsJoined);
                if (!joined.IsJoined) break;

                PlayerSlotProfile joinedProfile = joined.PlayerSlotId == player1.PlayerSlotId
                    ? player1
                    : joined.PlayerSlotId == player2.PlayerSlotId ? player2 : null;
                SessionPlayerLeaveResult leave = QaPlayerSessionCommandAccess.Leave(
                    routeSessionObserver,
                    joinedProfile);
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
                PlayerParticipationOperationResult close =
                    QaPlayerSessionCommandAccess.CloseJoining(routeSessionObserver);
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
                CurrentSnapshot != null && !CurrentSnapshot.JoiningOpen &&
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

        private IEnumerator WaitUntil(Func<bool> condition, string timeoutIssue,
            Func<string> readinessDiagnostic = null)
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
            string readiness = readinessDiagnostic != null ? readinessDiagnostic() : string.Empty;
            _waitIssue = timeoutIssue + " " +
                (string.IsNullOrWhiteSpace(readiness) ? string.Empty : "readinessIssue='" + Sanitize(readiness) + "' ") +
                (routeSessionObserver != null ? routeSessionObserver.Diagnostic : string.Empty) + " " +
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

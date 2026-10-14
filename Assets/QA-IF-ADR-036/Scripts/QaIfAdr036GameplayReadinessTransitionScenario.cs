using System;
using System.Collections;
using System.Linq;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Identity;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Transition;
using Immersive.QaFramework.Certification;
using Immersive.QaFramework.PlayerSession;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr036
{
    [DisallowMultipleComponent]
    public sealed class QaIfAdr036GameplayReadinessTransitionScenario : MonoBehaviour
    {
        private const string ScenarioId = "IF-ADR-036";
        private const string ParticipantId = "qa-if-adr-036.activity-b.required";
        private const string ControlScheme = "QA.Keyboard";
        private static readonly string[] Cases =
        {
            "A available", "B structurally ready behind Transition Gate", "Pause rejected during Transition Gate",
            "B ready and available after Transition Gate", "Pause blocks available B", "Resume restores B availability",
            "second Pause blocks B", "final Resume restores availability"
        };

        [SerializeField] private GameApplicationAsset gameApplication;
        [SerializeField] private ActivityAsset activityA;
        [SerializeField] private ActivityAsset activityB;
        [SerializeField] private PlayerSlotProfile playerSlot;
        [SerializeField] private PlayerSessionObserver routeObserver;
        [SerializeField] private PlayerSessionJoinTrigger joinTrigger;
        [SerializeField] private ActivityRequestTrigger requestA;
        [SerializeField] private ActivityRequestTrigger requestB;
        [SerializeField] private PauseRequestTrigger pauseTrigger;
        [SerializeField, Min(1)] private int frameBudget = 240;

        private readonly QaCertificationRecorder _result = new QaCertificationRecorder();
        private Keyboard _keyboard;
        private PlayerInput _input;
        private LocalPlayerHostAuthoring _host;
        private PlayerGameplayInputReader _reader;
        private PlayerSlotId _slotId;
        private PlayerSessionScopedSlotObservation _bSlotBaseline;
        private RuntimeContentOwner _bOwnerBaseline;
        private int _bOccurrence;
        private int _bSessionRevision;
        private int _bParticipantOccurrence;
        private int _cases;
        private bool _started, _terminal, _bAccepted, _bCompleted, _hasBBaseline, _hasDivergence;
        private string _cleanupIssue = string.Empty;

        private void Start()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (!_started || _terminal) return;
            RecordBlocked("Scenario interrupted before terminal evidence.");
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            Publish(QaCleanupDisposition.FreshBootRequired, "Interruption prevented runtime restoration evidence.");
        }

        private IEnumerator Run()
        {
            if (!ValidateAuthoring(out string issue))
            {
                RecordBlocked(issue);
                yield return CleanupAndPublish();
                yield break;
            }
            if (!playerSlot.TryGetPlayerSlotId(out _slotId, out issue))
            {
                RecordBlocked("P1 Slot identity unavailable: " + issue);
                yield return CleanupAndPublish();
                yield break;
            }

            yield return WaitUntil(() => routeObserver.IsAvailable &&
                QaPlayerSessionCommandAccess.FindOpenJoining(routeObserver)?.IsScopedAccessAvailable == true &&
                QaPlayerSessionCommandAccess.FindCloseJoining(routeObserver)?.IsScopedAccessAvailable == true,
                "Route-scoped Player Session Observer and Open/Close command triggers did not bind.");
            if (Diverged) { yield return CleanupAndPublish(); yield break; }
            if (!routeObserver.TryGetObservation(out PlayerSessionScopedObservationSnapshot initial) ||
                !TryGetSlot(initial, out PlayerSessionScopedSlotObservation initialSlot) || initialSlot.IsJoined ||
                !initial.HasCurrentActivityOccurrence || !IsOwner(initial.ActivityOwner, activityA))
            {
                RecordBlocked("Expected empty Session baseline in Activity A was not observed.");
                yield return CleanupAndPublish();
                yield break;
            }

            try { _keyboard = InputSystem.AddDevice<Keyboard>("QA IF-ADR-036 P1"); }
            catch (Exception exception) { RecordBlocked("QA Keyboard creation failed: " + exception.Message); }
            if (Diverged || _keyboard == null || !_keyboard.added)
            {
                if (!Diverged) RecordBlocked("Input System did not provide a QA-owned Keyboard.");
                yield return CleanupAndPublish();
                yield break;
            }

            PlayerParticipationOperationResult opened =
                QaPlayerSessionCommandAccess.OpenJoining(routeObserver);
            if (opened == null || !opened.Succeeded)
            {
                RecordBlocked("OpenJoining did not succeed: " + opened?.ToDiagnosticString());
                yield return CleanupAndPublish();
                yield break;
            }
            LocalPlayerJoinOutcome join = joinTrigger.RequestJoin(
                new LocalPlayerJoinRequest(ScenarioId, "Join P1 for IF-ADR-036", _keyboard, ControlScheme));
            if (join == null || join.Code != LocalPlayerJoinOutcomeCode.Joined ||
                join.AdmissionState != LocalPlayerJoinAdmissionState.Admitted ||
                !QaPlayerSessionJoinEvidenceReader.TryCapture(routeObserver, _slotId, _keyboard,
                    out QaPlayerSessionJoinEvidence evidence, out issue))
            {
                RecordBlocked("Public Manager-Provisioned Join evidence is incomplete: " + issue);
                yield return CleanupAndPublish();
                yield break;
            }
            _input = evidence.PlayerInput;
            _host = evidence.Host;
            PlayerParticipationOperationResult closed =
                QaPlayerSessionCommandAccess.CloseJoining(routeObserver);
            if (closed == null || !closed.Succeeded)
            {
                RecordFail("CloseJoining failed after Join: " + closed?.ToDiagnosticString());
                yield return CleanupAndPublish();
                yield break;
            }

            yield return WaitForReadyAvailable(activityA);
            if (Diverged) { yield return CleanupAndPublish(); yield break; }
            if (!Checkpoint("A-available", true, true, PauseState.Running, true, false))
            { yield return CleanupAndPublish(); yield break; }

            requestB.RequestActivity();
            _bAccepted = requestB.IsRequestInFlight || requestB.LastOutcome != FlowRequestOutcome.None;
            if (!_bAccepted)
            {
                RecordBlocked("Public Activity B request was not observed in-flight.");
                yield return CleanupAndPublish();
                yield break;
            }
            yield return WaitForBReadyGated();
            if (Diverged) { yield return CleanupAndPublish(); yield break; }
            if (!Checkpoint("B-ready-transition-gated", true, false, PauseState.Running, false, true))
            { yield return CleanupAndPublish(); yield break; }

            pauseTrigger.RequestPause();
            if (!IsTransitionPolicyPauseRejection() || !requestB.IsRequestInFlight ||
                !Checkpoint("pause-rejected-transition-active", true, false, PauseState.Running, false, true))
            {
                if (!Diverged) RecordFail("Pause during the active Transition Gate was not rejected with unchanged Running state and B availability posture.");
                yield return CleanupAndPublish();
                yield break;
            }

            if (!TryResolveParticipant(out ActivityReadinessParticipant participant, out issue) ||
                participant.State != ActivityReadinessParticipantState.Preparing)
            {
                RecordFail("Activity B does not expose the controlled Required Preparing participant: " + issue);
                yield return CleanupAndPublish();
                yield break;
            }
            participant.CompletePreparation();
            _bCompleted = true;
            yield return WaitUntil(() => !requestB.IsRequestInFlight && IsBReady() &&
                _reader.RuntimeGameplayAvailable && _input.currentActionMap != null && _input.currentActionMap.enabled,
                "B did not complete after releasing its Required participant.");
            if (Diverged) { yield return CleanupAndPublish(); yield break; }
            if (!Checkpoint("B-ready-transition-released", true, true, PauseState.Running, true, true))
            { yield return CleanupAndPublish(); yield break; }

            pauseTrigger.RequestPause();
            yield return WaitPause(PauseState.Paused);
            if (Diverged || !IsAppliedPauseResult(PauseState.Running, PauseState.Paused) ||
                !Checkpoint("pause-after-transition", true, false, PauseState.Paused, false, true))
            {
                if (!Diverged) RecordFail("Pause after Transition completion did not apply while preserving B readiness.");
                yield return CleanupAndPublish();
                yield break;
            }

            pauseTrigger.RequestResume();
            yield return WaitPause(PauseState.Running);
            if (Diverged || !IsAppliedPauseResult(PauseState.Paused, PauseState.Running))
            { if (!Diverged) RecordFail("Resume after Transition did not apply."); yield return CleanupAndPublish(); yield break; }
            yield return WaitUntil(() => ResolveReader() && _reader.RuntimeGameplayAvailable &&
                _input != null && _input.currentActionMap != null && _input.currentActionMap.enabled,
                "Gameplay availability/action map did not return after first Resume.");
            if (Diverged || !Checkpoint("resume-after-transition", true, true, PauseState.Running, true, true))
            { yield return CleanupAndPublish(); yield break; }

            pauseTrigger.RequestPause();
            yield return WaitPause(PauseState.Paused);
            if (Diverged || !IsAppliedPauseResult(PauseState.Running, PauseState.Paused) ||
                !Checkpoint("second-pause", true, false, PauseState.Paused, false, true))
            {
                if (!Diverged) RecordFail("Second Pause did not block B gameplay while retaining structural readiness.");
                yield return CleanupAndPublish();
                yield break;
            }

            pauseTrigger.RequestResume();
            yield return WaitPause(PauseState.Running);
            if (Diverged || !IsAppliedPauseResult(PauseState.Paused, PauseState.Running))
            { if (!Diverged) RecordFail("Final Resume did not apply."); yield return CleanupAndPublish(); yield break; }
            yield return WaitUntil(() => ResolveReader() && _reader.RuntimeGameplayAvailable &&
                _input != null && _input.currentActionMap != null && _input.currentActionMap.enabled,
                "Gameplay availability/action map did not return after final Resume.");
            if (Diverged || !Checkpoint("final-resume-available", true, true, PauseState.Running, true, true))
            { yield return CleanupAndPublish(); yield break; }
            yield return CleanupAndPublish();
        }

        private IEnumerator WaitForReadyAvailable(ActivityAsset activity)
        {
            yield return WaitUntil(() => ResolveReader() && _reader.GameplayReady && _reader.RuntimeGameplayAvailable &&
                _input != null && _input.currentActionMap != null && _input.currentActionMap.enabled,
                "GameplayReader did not become structurally ready and available in " + activity.ActivityName + ".");
        }

        private IEnumerator WaitForBReadyGated()
        {
            string issue = string.Empty;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (TryReadActivity(activityB, out PlayerSessionObserver observer, out issue) &&
                    observer.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) &&
                    observation.HasCurrentActivityOccurrence && IsOwner(observation.ActivityOwner, activityB) &&
                    TryGetSlot(observation, out PlayerSessionScopedSlotObservation slot) && slot.IsJoined &&
                    slot.HasPreparationEvidence && slot.Preparation.IsPrepared &&
                    slot.HasCurrentActorEvidence && slot.CurrentActor.ActorEvidence.IsValid &&
                    slot.HasGameplayAdmissionEvidence && slot.GameplayAdmission.IsReady &&
                    ResolveReader() && _reader.GameplayReady && !_reader.RuntimeGameplayAvailable &&
                    _input.currentActionMap != null && !_input.currentActionMap.enabled && requestB.IsRequestInFlight &&
                    TryResolveParticipant(out ActivityReadinessParticipant participant, out _) &&
                    participant.State == ActivityReadinessParticipantState.Preparing)
                {
                    _bSlotBaseline = slot;
                    _bOwnerBaseline = observation.ActivityOwner;
                    _bOccurrence = observation.ActivityOccurrence;
                    _bSessionRevision = observation.SessionRevision;
                    _bParticipantOccurrence = participant.Occurrence;
                    _hasBBaseline = true;
                    yield break;
                }
                yield return null;
            }
            RecordFail("B pending/gated did not expose structural readiness, Required Preparing and public per-Slot admission evidence. " + issue);
        }

        private IEnumerator WaitPause(PauseState expected)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (pauseTrigger.TryGetPauseSnapshot(out PauseSnapshot snapshot) && snapshot.State == expected)
                    yield break;
                yield return null;
            }
            RecordFail("Pause did not reach " + expected + ".");
        }

        private bool IsTransitionPolicyPauseRejection() => pauseTrigger != null &&
            pauseTrigger.LastStatus == PauseRequestStatus.Rejected &&
            pauseTrigger.LastProductStatus == "Rejected" &&
            pauseTrigger.LastPreviousState == PauseState.Running &&
            pauseTrigger.LastCurrentState == PauseState.Running &&
            pauseTrigger.LastRequestFailed &&
            pauseTrigger.LastMessage.IndexOf("transition policy", StringComparison.OrdinalIgnoreCase) >= 0;

        private bool IsAppliedPauseResult(PauseState previous, PauseState current) => pauseTrigger != null &&
            pauseTrigger.LastStatus == PauseRequestStatus.Applied &&
            pauseTrigger.LastProductStatus == "Applied" &&
            pauseTrigger.LastPreviousState == previous &&
            pauseTrigger.LastCurrentState == current &&
            pauseTrigger.LastRequestSucceeded;

        private IEnumerator WaitUntil(Func<bool> condition, string failure)
        {
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (condition()) yield break;
                yield return null;
            }
            RecordFail(failure);
        }

        private bool Checkpoint(string name, bool ready, bool available, PauseState pause,
            bool mapEnabled, bool requireB)
        {
            bool hasPause = pauseTrigger.TryGetPauseSnapshot(out PauseSnapshot snapshot);
            InputActionMap playerMap = _input != null && _input.actions != null ? _input.actions.FindActionMap("Player") : null;
            PlayerGameplayInputAvailability expectedAvailability = available
                ? PlayerGameplayInputAvailability.Allowed
                : pause == PauseState.Paused
                    ? PlayerGameplayInputAvailability.ActionsUnavailable
                    : PlayerGameplayInputAvailability.BlockedByGate;
            bool stateMatches = ResolveReader() && _reader.GameplayReady == ready &&
                _reader.RuntimeGameplayAvailable == available && hasPause && snapshot.State == pause &&
                _reader.RuntimeGameplayAvailability == expectedAvailability &&
                _input != null && _input.currentActionMap != null && playerMap != null && playerMap.enabled == mapEnabled;
            PlayerSessionScopedObservationSnapshot observation = null;
            PlayerSessionScopedSlotObservation slot = default;
            bool bMatches = !requireB || TryReadB(out observation, out slot) && SameB(observation, slot);
            string b = requireB && bMatches ? DescribeB(observation, slot) : "B='<not-created>'";
            Debug.Log("[IF-ADR-036] checkpoint='" + name + "' GameplayReady='" + (_reader != null && _reader.GameplayReady) +
                "' RuntimeGameplayAvailable='" + (_reader != null && _reader.RuntimeGameplayAvailable) +
                "' availability='" + (_reader != null ? _reader.RuntimeGameplayAvailability.ToString() : "Unavailable") +
                "' pause='" + (hasPause ? snapshot.State.ToString() : "Unavailable") + "' actionMap='" +
                (_input != null && _input.currentActionMap != null ? _input.currentActionMap.name : "<none>") +
                "' actionMapEnabled='" + (_input != null && _input.currentActionMap != null && _input.currentActionMap.enabled) +
                "' gameplayActionMapEnabled='" + (playerMap != null && playerMap.enabled) +
                "' pauseRequestStatus='" + pauseTrigger.LastStatus + "' pauseProductStatus='" + pauseTrigger.LastProductStatus +
                "' requestBInFlight='" + requestB.IsRequestInFlight + "' " + b, this);
            if (!stateMatches || !bMatches)
            {
                RecordFail("Checkpoint '" + name + "' diverged in readiness, availability, Pause, physical Action Map or B identity/preparation/admission evidence.");
                return false;
            }
            _cases++;
            return true;
        }

        private bool TryReadB(out PlayerSessionScopedObservationSnapshot observation,
            out PlayerSessionScopedSlotObservation slot)
        {
            observation = null;
            slot = default;
            return TryReadActivity(activityB, out PlayerSessionObserver observer, out _) &&
                observer.TryGetObservation(out observation) && observation != null && TryGetSlot(observation, out slot);
        }

        private bool SameB(PlayerSessionScopedObservationSnapshot observation, PlayerSessionScopedSlotObservation slot)
        {
            bool participantValid = TryResolveParticipant(out ActivityReadinessParticipant participant, out _) &&
                participant.Occurrence == _bParticipantOccurrence && participant.State ==
                (_bCompleted ? ActivityReadinessParticipantState.Completed : ActivityReadinessParticipantState.Preparing);
            return _hasBBaseline && participantValid && observation.HasCurrentActivityOccurrence &&
                observation.ActivityOccurrence == _bOccurrence && observation.ActivityOwner.Equals(_bOwnerBaseline) &&
                observation.SessionRevision == _bSessionRevision && slot.IsJoined &&
                slot.Slot.Revision == _bSlotBaseline.Slot.Revision &&
                slot.CurrentActor.ActorEvidence.ActorId == _bSlotBaseline.CurrentActor.ActorEvidence.ActorId &&
                slot.Preparation.Token == _bSlotBaseline.Preparation.Token &&
                slot.GameplayAdmission.Token == _bSlotBaseline.GameplayAdmission.Token &&
                slot.GameplayAdmission.OccupancyToken == _bSlotBaseline.GameplayAdmission.OccupancyToken &&
                slot.GameplayAdmission.AdmissionRevision == _bSlotBaseline.GameplayAdmission.AdmissionRevision &&
                slot.GameplayAdmission.InputBindingToken == _bSlotBaseline.GameplayAdmission.InputBindingToken;
        }

        private string DescribeB(PlayerSessionScopedObservationSnapshot observation, PlayerSessionScopedSlotObservation slot)
        {
            bool hasParticipant = TryResolveParticipant(out ActivityReadinessParticipant participant, out _);
            return "B(owner='" + observation.ActivityOwner.StableText + "' occurrence='" + observation.ActivityOccurrence +
                "' sessionRevision='" + observation.SessionRevision + "' slotRevision='" + slot.Slot.Revision +
                "' actor='" + slot.CurrentActor.ActorEvidence.ActorId.StableText + "' preparation='" + slot.Preparation.Token.StableText +
                "' admission='" + slot.GameplayAdmission.Token.StableText + "' admissionRevision='" + slot.GameplayAdmission.AdmissionRevision +
                "' occupancy='" + slot.GameplayAdmission.OccupancyToken.StableText + "' inputBinding='" +
                slot.GameplayAdmission.InputBindingToken.StableText + "' readinessParticipantOccurrence='" +
                (hasParticipant ? participant.Occurrence.ToString() : "<missing>") + "' readinessParticipantState='" +
                (hasParticipant ? participant.State.ToString() : "<missing>") + "')";
        }

        private bool IsBReady() => TryReadB(out PlayerSessionScopedObservationSnapshot observation,
            out PlayerSessionScopedSlotObservation slot) && observation.ActivityOccurrence == _bOccurrence &&
            slot.GameplayAdmission.IsReady && ResolveReader() && _reader.GameplayReady;

        private bool ResolveReader()
        {
            if (_host == null || !_host.IsJoined) return false;
            PlayerGameplayInputReader[] readers = _host.GetComponentsInChildren<PlayerGameplayInputReader>(true);
            if (readers.Length != 1) return false;
            _reader = readers[0];
            return true;
        }

        private bool TryResolveParticipant(out ActivityReadinessParticipant participant, out string issue)
        {
            participant = null;
            if (!TryGetActivityComponents(activityB, out ActivityReadinessParticipant[] found, out issue) || found.Length != 1)
            {
                issue = "Activity B must contain exactly one ActivityReadinessParticipant.";
                return false;
            }
            participant = found[0];
            return participant.Requiredness == ActivityContentExecutionRequiredness.Required &&
                string.Equals(participant.ParticipantId, ParticipantId, StringComparison.Ordinal);
        }

        private bool TryReadActivity(ActivityAsset activity, out PlayerSessionObserver observer, out string issue)
        {
            observer = null;
            if (!TryGetActivityComponents(activity, out PlayerSessionObserver[] found, out issue)) return false;
            found = found.Where(value => value.Scope == LocalPlayerProvisioningConsumerScope.Activity).ToArray();
            if (found.Length != 1) { issue = "Expected exactly one Activity-scoped observer."; return false; }
            observer = found[0];
            return observer.IsAvailable;
        }

        private bool TryGetActivityComponents<T>(ActivityAsset activity, out T[] found, out string issue) where T : Component
        {
            found = Array.Empty<T>();
            issue = string.Empty;
            if (activity == null || activity.ActivityContentProfile == null || activity.ActivityContentProfile.SceneCount != 1)
            { issue = "Activity requires exactly one content scene."; return false; }
            Scene scene = SceneManager.GetSceneByPath(activity.ActivityContentProfile.Scenes[0].ScenePath);
            if (!scene.IsValid() || !scene.isLoaded) { issue = "Activity scene is not loaded."; return false; }
            found = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            return true;
        }

        private bool TryGetSlot(PlayerSessionScopedObservationSnapshot observation, out PlayerSessionScopedSlotObservation slot)
        {
            foreach (PlayerSessionScopedSlotObservation candidate in observation.Slots)
                if (candidate.Slot.PlayerSlotId == _slotId) { slot = candidate; return true; }
            slot = default;
            return false;
        }

        private static bool IsOwner(RuntimeContentOwner owner, ActivityAsset activity) => owner.IsValid &&
            owner.OwnerIdentity == FrameworkIdentityKey.From(activity.ActivityId);

        private bool ValidateAuthoring(out string issue)
        {
            issue = string.Empty;
            if (gameApplication == null || activityA == null || activityB == null || playerSlot == null || routeObserver == null ||
                joinTrigger == null || requestA == null || requestB == null || pauseTrigger == null)
            { issue = "Route-authored references are incomplete."; return false; }
            if (gameApplication.StartupRoute == null || gameApplication.StartupRoute.StartupActivity != activityA ||
                requestA.TargetActivity != activityA || requestB.TargetActivity != activityB ||
                routeObserver.Scope != LocalPlayerProvisioningConsumerScope.Route || joinTrigger.Scope != LocalPlayerProvisioningConsumerScope.Route ||
                activityB.EntryReadinessPolicy != ActivityEntryReadinessPolicy.WaitVisible ||
                activityB.TransitionGateMode != TransitionGateMode.InputInteractionAndGameplay ||
                activityB.PlayerParticipationRequirementLevel != PlayerParticipationRequirementLevel.GameplayReady ||
                activityB.PlayerParticipationProjectionMode != ActivityParticipationProjectionMode.ExplicitSlots ||
                activityB.PlayerParticipationExplicitSlotProfiles.Count != 1 ||
                activityB.PlayerParticipationExplicitSlotProfiles[0] != playerSlot)
            { issue = "Activity A/B request, scope, readiness or Transition Gate configuration is inconsistent."; return false; }
            return true;
        }

        private IEnumerator CleanupAndPublish()
        {
            if (TryResolveParticipant(out ActivityReadinessParticipant pending, out _) &&
                pending.State == ActivityReadinessParticipantState.Preparing) pending.CompletePreparation();
            for (int frame = 0; frame < frameBudget && requestB != null && requestB.IsRequestInFlight; frame++) yield return null;
            if (requestB != null && requestB.IsRequestInFlight) _cleanupIssue = Append(_cleanupIssue, "B request remained in-flight.");
            if (pauseTrigger != null && pauseTrigger.IsPaused)
            {
                pauseTrigger.RequestResume();
                if (!IsAppliedPauseResult(PauseState.Paused, PauseState.Running))
                    _cleanupIssue = Append(_cleanupIssue, "Pause could not be resumed after the Activity transition settled.");
            }

            PlayerSessionObserver active = null;
            for (int frame = 0; frame < frameBudget && active == null; frame++)
            {
                if (TryReadActivity(activityA, out PlayerSessionObserver observerA, out _)) active = observerA;
                else if (TryReadActivity(activityB, out PlayerSessionObserver observerB, out _)) active = observerB;
                if (active == null) yield return null;
            }
            if (active == null) _cleanupIssue = Append(_cleanupIssue, "No active Activity-scoped observer was available for Leave cleanup.");
            bool restoreAAfterLeave = active != null &&
                active.TryGetObservation(out PlayerSessionScopedObservationSnapshot current) &&
                IsOwner(current.ActivityOwner, activityB);
            if (active != null &&
                active.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) &&
                TryGetSlot(observation, out PlayerSessionScopedSlotObservation slot) && slot.IsJoined)
            {
                SessionPlayerLeaveResult leave = QaPlayerSessionCommandAccess.Leave(
                    active,
                    playerSlot);
                if (leave == null || !leave.Succeeded) _cleanupIssue = Append(_cleanupIssue, "Public Leave failed.");
                for (int frame = 0; frame < frameBudget; frame++)
                {
                    if (active.TryGetObservation(out PlayerSessionScopedObservationSnapshot after) &&
                        TryGetSlot(after, out PlayerSessionScopedSlotObservation afterSlot) && !afterSlot.IsJoined) break;
                    if (frame == frameBudget - 1) _cleanupIssue = Append(_cleanupIssue, "Leave not observed before deadline.");
                    yield return null;
                }
            }

            // Retire the exact Session Player while its current Activity still owns
            // the contextual Actor/admission evidence. Only then restore the empty
            // Activity A baseline; Leave must not depend on re-preparing an Actor
            // merely to clean up the QA environment.
            if (restoreAAfterLeave && requestA != null && !requestA.IsRequestInFlight)
            {
                requestA.RequestActivity();
                for (int frame = 0; frame < frameBudget && requestA.IsRequestInFlight; frame++) yield return null;
                if (requestA.IsRequestInFlight) _cleanupIssue = Append(_cleanupIssue, "A restoration request remained in-flight.");
            }

            if (restoreAAfterLeave)
            {
                bool aRestored = false;
                for (int frame = 0; frame < frameBudget; frame++)
                {
                    if (TryReadActivity(activityA, out _, out _)) { aRestored = true; break; }
                    yield return null;
                }
                if (!aRestored) _cleanupIssue = Append(_cleanupIssue, "Activity A baseline was not restored after Session Leave.");
            }
            if (routeObserver != null)
            {
                PlayerParticipationOperationResult close =
                    QaPlayerSessionCommandAccess.CloseJoining(routeObserver);
                if (close == null || !close.Completed) _cleanupIssue = Append(_cleanupIssue, "CloseJoining cleanup failed.");
            }
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);

            bool restored = string.IsNullOrEmpty(_cleanupIssue) && (_keyboard == null || !_keyboard.added) &&
                pauseTrigger != null && !pauseTrigger.IsPaused && requestA != null && !requestA.IsRequestInFlight &&
                requestB != null && !requestB.IsRequestInFlight && routeObserver != null &&
                routeObserver.TryGetObservation(out PlayerSessionScopedObservationSnapshot final) &&
                TryGetSlot(final, out PlayerSessionScopedSlotObservation finalSlot) && !finalSlot.IsJoined && IsOwner(final.ActivityOwner, activityA);
            if (!restored) _cleanupIssue = Append(_cleanupIssue, "Empty Session/Activity A/Running baseline restoration was not fully observed.");
            if (!_hasDivergence)
            {
                if (restored && _cases == Cases.Length) _result.RecordPass();
                else RecordFail("Expected checkpoints were incomplete or cleanup failed.");
            }
            Publish(restored ? QaCleanupDisposition.BaselineRestored : QaCleanupDisposition.FreshBootRequired, _cleanupIssue);
        }

        private void RecordBlocked(string issue)
        {
            if (_hasDivergence) return;
            _hasDivergence = true;
            _result.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, issue);
        }
        private void RecordFail(string issue)
        {
            if (_hasDivergence) return;
            _hasDivergence = true;
            _result.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, issue);
        }
        private bool Diverged => _hasDivergence;
        private static string Append(string current, string next) => string.IsNullOrEmpty(current) ? next : current + " | " + next;
        private static string Sanitize(string value) => (value ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n");

        private void Publish(QaCleanupDisposition cleanup, string cleanupIssue)
        {
            if (_terminal) return;
            _terminal = true;
            QaCertificationResult result = _result.CreateResult(ScenarioId, cleanup, cleanupIssue);
            string status = result.Verdict == QaCertificationVerdict.Pass ? "Passed" :
                result.Verdict == QaCertificationVerdict.Blocked ? "Blocked" : "Failed";
            string message = "[IF-ADR-036] status='" + status + "' verdict='" + result.Verdict.ToString().ToUpperInvariant() +
                "' cases='" + _cases + "/" + Cases.Length + "' requestBAccepted='" + _bAccepted + "' requestBCompleted='" + _bCompleted +
                "' bOccurrence='" + _bOccurrence + "' cleanup='" + result.CleanupDisposition + "' firstDivergence='" +
                Sanitize(result.FirstCausalDivergence) + "' cleanupIssue='" + Sanitize(result.CleanupIssue) + "'.";
            if (result.Verdict == QaCertificationVerdict.Pass) Debug.Log(message, this);
            else if (result.Verdict == QaCertificationVerdict.Blocked) Debug.LogWarning(message, this);
            else Debug.LogError(message, this);
        }
    }
}

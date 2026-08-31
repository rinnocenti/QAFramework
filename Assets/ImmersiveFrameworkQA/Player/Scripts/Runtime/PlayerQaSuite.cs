using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Actors;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player
{
    internal static class PlayerQaSuite
    {
        private const int FrameBudget = 360;
        private const string Source = nameof(PlayerQaSuite);

        internal sealed class Result
        {
            internal readonly List<string> Completed = new List<string>(16);
            internal string FailedCase;
            internal string FailureMessage;
            internal int Passed;
            internal int Failed;
            internal LocalPlayerHostAuthoring PlayerOneHost;
            internal PlayerGameplayInputReader CurrentGameplayReader;
            internal PlayerGameplayInputBindingToken LastReleasedGameplayBinding;
            internal bool PreviousReaderOccurrenceReleased;

            internal bool Ok => Failed == 0 && string.IsNullOrEmpty(FailedCase);

            internal void Pass(string caseId)
            {
                Completed.Add(caseId);
                Passed++;
            }

            internal void Fail(string caseId, string expected, string actual, string message)
            {
                FailedCase = caseId;
                Failed++;
                FailureMessage =
                    $"{caseId}: expected '{expected}', actual '{actual}'. {message}";
            }
        }

        internal static IEnumerator Run(PlayerQaPanel fixture, Action<Result> completed)
        {
            var result = new Result();
            if (fixture == null)
            {
                result.Fail("fixture", "configured panel", "null", "Player QA panel is missing.");
                completed?.Invoke(result);
                yield break;
            }

            yield return RunGroup(result, "access", () => ProveAccess(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "join", () => ProveJoin(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "observation", () => ProveObservation(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "actor-default", () => ProveDefaultActor(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "actor-lifecycle", () => ProveActorLifecycle(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "gameplay-ready-reader", () => ProveGameplayReadyReader(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "reader-cardinality", () => ProveReaderCardinality(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "actor-replace", () => ProveReplaceActor(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "joining-control", () => ProveJoiningControl(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "second-player", () => ProveSecondPlayer(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "commands", () => ProveCommands(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "leave", () => ProveLeave(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "rejoin", () => ProveRejoin(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return WaitThen(result, "negatives", () => ProveNegatives(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return RunGroup(result, "spatial", () => ProveSpatial(fixture, result));
            if (!result.Ok)
            {
                completed?.Invoke(result);
                yield break;
            }

            yield return RunGroup(result, "relocation", () => ProveRelocation(fixture, result));
            completed?.Invoke(result);
        }

        private static IEnumerator RunGroup(Result result, string caseId, Action body)
        {
            try
            {
                body();
                if (result.Ok)
                {
                    result.Pass(caseId);
                }
            }
            catch (Exception exception)
            {
                result.Fail(caseId, "no exception", exception.GetType().Name, exception.Message);
            }

            yield return null;
        }

        private static IEnumerator WaitThen(Result result, string caseId, Func<IEnumerator> body)
        {
            Exception caught = null;
            IEnumerator routine = null;
            try
            {
                routine = body();
            }
            catch (Exception exception)
            {
                caught = exception;
            }

            if (caught != null)
            {
                result.Fail(caseId, "no exception", caught.GetType().Name, caught.Message);
                yield break;
            }

            while (true)
            {
                bool moveNext;
                try
                {
                    moveNext = routine.MoveNext();
                }
                catch (Exception exception)
                {
                    result.Fail(caseId, "no exception", exception.GetType().Name, exception.Message);
                    yield break;
                }

                if (!moveNext)
                {
                    break;
                }

                if (!result.Ok)
                {
                    (routine as IDisposable)?.Dispose();
                    yield break;
                }

                yield return routine.Current;

                if (!result.Ok)
                {
                    (routine as IDisposable)?.Dispose();
                    yield break;
                }
            }

            if (result.Ok)
            {
                result.Pass(caseId);
            }
        }

        private static void ProveAccess(PlayerQaPanel fixture, Result result)
        {
            Require(result, "access",
                fixture.Probe != null &&
                fixture.Observer != null &&
                fixture.ActivityScopeProbe != null,
                "Route and Activity scoped probes assigned",
                "missing",
                "Player QA scene requires Route-scoped access/observation and an Activity-scoped access probe.");
            if (!result.Ok)
            {
                return;
            }

            Require(result, "access",
                fixture.Probe.Scope == LocalPlayerProvisioningConsumerScope.Route &&
                fixture.Observer.Scope == LocalPlayerProvisioningConsumerScope.Route,
                "Route-scoped access consumers",
                $"{fixture.Probe.Scope}/{fixture.Observer.Scope}",
                "The canonical Player QA access probe and observer must remain Route-scoped.");
            if (!result.Ok)
            {
                return;
            }

            bool activityScopeBound = fixture.ActivityScopeProbe.TryGetAccess(
                out IPlayerSessionScopedAccess activityScopeAccess,
                out string activityScopeIssue);
            Require(result, "access",
                fixture.ActivityScopeProbe.Scope == LocalPlayerProvisioningConsumerScope.Activity &&
                activityScopeBound &&
                activityScopeAccess != null &&
                activityScopeAccess.Snapshot.IsAvailable,
                "Activity-scoped access bound to the active Activity lifecycle scope",
                activityScopeBound
                    ? "available"
                    : activityScopeIssue,
                "Activity scope is a Framework lifecycle scope; it is not inferred from nominal scene ownership.");
        }

        private static IEnumerator ProveJoin(PlayerQaPanel fixture, Result result)
        {
            IPlayerSessionScopedAccess access = null;
            yield return WaitFor(
                result,
                "join",
                () => fixture.Probe.TryGetAccess(out access, out _) &&
                      access != null &&
                      access.Snapshot.IsAvailable,
                "Timed out waiting for Route-scoped IPlayerSessionScopedAccess.");

            Require(result, "join", access != null && access.Snapshot.IsAvailable,
                "scoped access available", access == null ? "null" : access.Snapshot.Diagnostic,
                "Timed out waiting for Route-scoped IPlayerSessionScopedAccess.");
            if (!result.Ok)
            {
                yield break;
            }

            Require(result, "join", access.Snapshot.HasJoinCapability,
                "HasJoinCapability", "false",
                "Manager-Provisioned Player QA requires ILocalPlayerJoinAccess.");
            if (!result.Ok)
            {
                yield break;
            }

            Require(result, "join",
                fixture.Probe.TryGetJoinAccess(out ILocalPlayerJoinAccess joinAccess, out string joinIssue) &&
                joinAccess != null,
                "join access", joinIssue,
                "ILocalPlayerJoinAccess is unavailable.");
            if (!result.Ok)
            {
                yield break;
            }

            if (TryFindSlot(fixture, ExpectedSlotId(fixture), out PlayerSessionScopedSlotObservation existing) &&
                existing.IsJoined)
            {
                Require(result, "join",
                    existing.HasHostEvidence,
                    "joined P1 with Host evidence",
                    existing.HasHostEvidence ? "present" : "missing",
                    "Existing joined P1 does not expose retained Session Host evidence.");
                yield break;
            }

            LocalPlayerJoinResult join = joinAccess.RequestJoin(
                new LocalPlayerJoinRequest(Source, "player-qa-join-p1"));
            Require(result, "join", join != null && join.Succeeded,
                "SucceededJoined", join == null ? "null" : $"{join.Status} {join.Message}",
                "RequestJoin failed.");
            if (!result.Ok)
            {
                yield break;
            }

            Require(result, "join",
                join.Slot.PlayerSlotId == ExpectedSlotId(fixture) &&
                join.HasLocalPlayerHostEvidence &&
                join.LocalPlayerHost != null &&
                join.PlayerInput != null,
                "P1 host evidence",
                join.Slot.PlayerSlotId.IsValid ? join.Slot.PlayerSlotId.StableText : "invalid",
                "Join did not expose complete public Host evidence.");
            if (!result.Ok)
            {
                yield break;
            }

            ValidateManagerHost(result, fixture, join.LocalPlayerHost);
            if (result.Ok)
            {
                result.PlayerOneHost = join.LocalPlayerHost;
            }
            yield return WaitForSlot(
                result,
                "join",
                fixture,
                slot => slot.IsJoined &&
                        slot.HasHostEvidence);
        }

        private static IEnumerator ProveObservation(PlayerQaPanel fixture, Result result)
        {
            yield return WaitForSlot(result, "observation", fixture, slot => slot.IsJoined);
            if (!TryGetAccess(fixture, out IPlayerSessionScopedAccess access, out string issue))
            {
                result.Fail("observation", "available access", issue, issue);
                yield break;
            }

            Require(result, "observation",
                access.TryGetObservation(out PlayerSessionScopedObservationSnapshot fromAccess) &&
                fixture.Observer.TryGetObservation(out PlayerSessionScopedObservationSnapshot fromObserver) &&
                fromAccess != null &&
                fromObserver != null &&
                fromAccess.IsAvailable &&
                fromObserver.IsAvailable &&
                fromAccess.SessionRevision == fromObserver.SessionRevision,
                "matching observer/access snapshots",
                "diverged",
                "PlayerSessionObserver and IPlayerSessionScopedAccess did not expose the same Session.");
            if (!result.Ok)
            {
                yield break;
            }

            PlayerSessionScopedSlotObservation slot = FindSlot(fromAccess, ExpectedSlotId(fixture));
            Require(result, "observation", slot.IsJoined,
                "P1 joined", slot.Slot.AllocationState.ToString(),
                "Observation does not show the joined P1 Slot.");
            if (!result.Ok)
            {
                yield break;
            }

            bool sawChange = false;
            bool sawDesignerJoining = false;
            void OnChanged(PlayerSessionChange change)
            {
                if (change != null &&
                    (change.Kind == PlayerSessionChangeKind.SlotAllocationChanged ||
                     change.Kind == PlayerSessionChangeKind.ActorSelectionChanged ||
                     change.Kind == PlayerSessionChangeKind.JoiningChanged))
                {
                    sawChange = true;
                }
            }

            void OnJoiningChanged()
            {
                sawDesignerJoining = true;
            }

            fixture.Observer.Changed += OnChanged;
            fixture.Observer.OnJoiningClosed.AddListener(OnJoiningChanged);
            fixture.Observer.OnJoiningOpened.AddListener(OnJoiningChanged);
            try
            {
                PlayerParticipationOperationResult closed = access.CloseJoining(
                    Source, "player-qa-observe-close");
                PlayerParticipationOperationResult opened = access.OpenJoining(
                    Source, "player-qa-observe-open");
                Require(result, "observation",
                    closed != null && closed.Completed &&
                    opened != null && opened.Completed,
                    "joining close/open mutation",
                    $"close='{(closed != null ? closed.Status.ToString() : "null")}' " +
                    $"open='{(opened != null ? opened.Status.ToString() : "null")}'",
                    "Could not mutate Joining state to prove Player Session observation.");
                if (!result.Ok)
                {
                    yield break;
                }

                yield return WaitFor(
                    result,
                    "observation",
                    () => sawChange && sawDesignerJoining,
                    "PlayerSessionObserver did not emit both Changed and a designer Joining event after Close/Open Joining.");
            }
            finally
            {
                fixture.Observer.Changed -= OnChanged;
                fixture.Observer.OnJoiningClosed.RemoveListener(OnJoiningChanged);
                fixture.Observer.OnJoiningOpened.RemoveListener(OnJoiningChanged);
            }

            Require(result, "observation", sawChange && sawDesignerJoining,
                "observer change and designer joining event",
                $"changed='{sawChange}' designerJoining='{sawDesignerJoining}'",
                "Player Session observation did not emit the expected committed change surfaces.");
        }

        private static IEnumerator ProveDefaultActor(PlayerQaPanel fixture, Result result)
        {
            if (!TryGetAccess(fixture, out IPlayerSessionScopedAccess access, out string issue))
            {
                result.Fail("actor-default", "available access", issue, issue);
                yield break;
            }

            if (!TryFindSlot(fixture, ExpectedSlotId(fixture), out PlayerSessionScopedSlotObservation current))
            {
                result.Fail("actor-default", "joined P1", "missing", "P1 Slot observation is missing.");
                yield break;
            }

            if (current.Slot.SelectedActorProfile != fixture.DefaultActor)
            {
                PlayerActorSelectionResult selection = access.RequestSelectDefaultActor(
                    ExpectedSlotId(fixture),
                    current.Slot.SelectionRevision,
                    Source,
                    "player-qa-default-actor");
                Require(result, "actor-default",
                    selection != null && selection.Succeeded,
                    "default actor selected",
                    selection == null ? "null" : $"{selection.Status} {selection.Message}",
                    "RequestSelectDefaultActor failed.");
                if (!result.Ok)
                {
                    yield break;
                }
            }

            yield return WaitForSlot(
                result,
                "actor-default",
                fixture,
                slot => slot.Slot.SelectedActorProfile == fixture.DefaultActor);

            if (!result.Ok)
            {
                yield break;
            }

            Require(result, "actor-default",
                fixture.DefaultActor != null &&
                fixture.DefaultActor.PresentationPrefab != null,
                "default ActorProfile with PresentationPrefab",
                fixture.DefaultActor != null && fixture.DefaultActor.PresentationPrefab != null
                    ? fixture.DefaultActor.PresentationPrefab.name
                    : "missing",
                "Default Actor selection must retain an explicit ActorProfile PresentationPrefab.");
            if (!result.Ok)
            {
                yield break;
            }

        }
        private static IEnumerator ProveReplaceActor(PlayerQaPanel fixture, Result result)
        {
            if (fixture.AlternateActor == null)
            {
                result.Fail("actor-replace", "alternate actor", "null",
                    "Player QA requires the Alternate Actor Profile.");
                yield break;
            }

            if (!TryGetAccess(fixture, out IPlayerSessionScopedAccess access, out string issue))
            {
                result.Fail("actor-replace", "available access", issue, issue);
                yield break;
            }

            if (!TryFindSlot(fixture, ExpectedSlotId(fixture), out PlayerSessionScopedSlotObservation current))
            {
                result.Fail("actor-replace", "joined P1", "missing", "P1 Slot observation is missing.");
                yield break;
            }

            PlayerGameplayInputReader previousReader = result.CurrentGameplayReader;
            Require(result, "actor-replace",
                previousReader != null && previousReader.HasCurrentGameplayBinding,
                "bound Default Presentation reader before replacement",
                previousReader == null ? "missing" : previousReader.Diagnostic,
                "Actor replacement must start from the GameplayReady Default Presentation reader.");
            if (!result.Ok)
            {
                yield break;
            }

            PlayerActorSelectionResult replace = access.RequestReplaceActorSelection(
                new PlayerActorSelectionRequest(
                    ExpectedSlotId(fixture),
                    fixture.AlternateActor,
                    Source,
                    "player-qa-replace-actor",
                    current.Slot.SelectionRevision));
            Require(result, "actor-replace", replace != null && replace.Succeeded,
                "replace succeeded",
                replace == null ? "null" : $"{replace.Status} {replace.Message}",
                "RequestReplaceActorSelection failed.");
            if (!result.Ok)
            {
                yield break;
            }

            yield return WaitFor(
                result,
                "actor-replace",
                () => TryResolveCurrentGameplayReader(
                          result.PlayerOneHost,
                          out PlayerGameplayInputReader alternateReader,
                          out _) &&
                      alternateReader.HasCurrentGameplayBinding &&
                      !ReferenceEquals(alternateReader, previousReader) &&
                      TryFindSlot(
                          fixture,
                          ExpectedSlotId(fixture),
                          out PlayerSessionScopedSlotObservation alternateSlot) &&
                          alternateReader.CurrentBindingToken ==
                          alternateSlot.GameplayAdmission.InputBindingToken,
                "FRAMEWORK CONTRACT GAP / REGRESSION: Actor replacement during an active completed GameplayReady Activity does not reconcile the current gameplay projection.");
            if (!result.Ok)
            {
                yield break;
            }

            string readerIssue = string.Empty;
            Require(result, "actor-replace",
                (previousReader == null || !previousReader.HasCurrentGameplayBinding) &&
                TryResolveCurrentGameplayReader(
                    result.PlayerOneHost,
                    out PlayerGameplayInputReader currentReader,
                    out readerIssue) &&
                currentReader.HasCurrentGameplayBinding,
                "released Default reader and bound Alternate reader",
                readerIssue,
                "Actor replacement left the retired Presentation reader bound or did not bind the Alternate Presentation reader.");
        }
        private static IEnumerator ProveActorLifecycle(PlayerQaPanel fixture, Result result)
        {
            var trigger = fixture.RelocateActivityTrigger;
            Require(result, "actor-lifecycle",
                trigger != null &&
                fixture.RelocateActivity != null &&
                ReferenceEquals(trigger.TargetActivity, fixture.RelocateActivity),
                "Relocate Activity trigger",
                trigger != null && trigger.TargetActivity != null
                    ? trigger.TargetActivity.ActivityName
                    : "missing",
                "Actor lifecycle proof requires the dedicated Relocate Activity trigger.");
            if (!result.Ok)
            {
                yield break;
            }

            trigger.RequestActivity();
            yield return WaitFor(
                result,
                "actor-lifecycle",
                () => !trigger.IsRequestInFlight &&
                      (trigger.LastRequestSucceeded ||
                       trigger.LastRequestFailed ||
                       trigger.LastRequestIgnored),
                "Timed out waiting for the dedicated Actor lifecycle Activity request.");
            if (!result.Ok)
            {
                yield break;
            }

            Require(result, "actor-lifecycle",
                trigger.LastRequestSucceeded,
                "Relocate Activity request succeeded",
                $"outcome={trigger.LastOutcome} message={trigger.LastMessage}",
                "Dedicated Actor lifecycle Activity request did not succeed.");
            if (!result.Ok)
            {
                yield break;
            }

            yield return WaitForSlot(
                result,
                "actor-lifecycle",
                fixture,
                slot => slot.IsJoined &&
                        slot.Slot.SelectedActorProfile == fixture.DefaultActor &&
                        slot.IsLogicalActorPrepared &&
                        slot.IsPhysicallyMaterialized &&
                        slot.CurrentActor.HasCurrentActor &&
                        slot.HasHostEvidence &&
                        slot.HostEvidence.AssignmentOrigin ==
                            PlayerSlotAssignmentOrigin.ManagerProvisioned);
        }

        private static IEnumerator ProveGameplayReadyReader(
            PlayerQaPanel fixture,
            Result result)
        {
            ActivityRequestTrigger trigger = fixture.GameplayReadyActivityTrigger;
            Require(result, "gameplay-ready-reader",
                trigger != null &&
                fixture.GameplayReadyActivity != null &&
                ReferenceEquals(trigger.TargetActivity, fixture.GameplayReadyActivity),
                "GameplayReady Activity trigger",
                trigger != null && trigger.TargetActivity != null
                    ? trigger.TargetActivity.ActivityName
                    : "missing",
                "Player QA requires its dedicated GameplayReady Activity trigger.");
            if (!result.Ok)
            {
                yield break;
            }

            yield return RequestActivity(
                result,
                "gameplay-ready-reader",
                trigger,
                expectSuccess: true,
                "GameplayReady Activity request did not complete successfully.");
            if (!result.Ok)
            {
                yield break;
            }

            EmitGameplayReadyReaderTopologyDiagnostic(result);

            yield return WaitFor(
                result,
                "gameplay-ready-reader",
                () => TryResolveCurrentGameplayReader(
                          result.PlayerOneHost,
                          out PlayerGameplayInputReader reader,
                          out _) &&
                      reader.HasCurrentGameplayBinding &&
                      TryFindSlot(
                          fixture,
                          ExpectedSlotId(fixture),
                          out PlayerSessionScopedSlotObservation slot) &&
                      slot.Slot.SelectedActorProfile == fixture.DefaultActor &&
                      slot.HasGameplayAdmissionEvidence &&
                      slot.GameplayAdmission.IsAdmitted &&
                      reader.CurrentBindingToken == slot.GameplayAdmission.InputBindingToken,
                DescribeGameplayReadyReaderFailure(fixture, result));
            if (!result.Ok)
            {
                yield break;
            }

            string readerIssue = string.Empty;
            Require(result, "gameplay-ready-reader",
                TryResolveCurrentGameplayReader(
                    result.PlayerOneHost,
                    out PlayerGameplayInputReader reader,
                    out readerIssue) &&
                reader.gameObject.GetComponent<PlayerActorDeclaration>() == null &&
                TryFindSlot(
                    fixture,
                    ExpectedSlotId(fixture),
                    out PlayerSessionScopedSlotObservation slot) &&
                reader.HasCurrentGameplayBinding &&
                reader.CurrentBindingToken.IsValid &&
                reader.CurrentBindingToken == slot.GameplayAdmission.InputBindingToken &&
                reader.GameplayReady == slot.GameplayAdmission.GameplayReady,
                "current Default Presentation gameplay reader",
                readerIssue,
                DescribeGameplayReadyReaderFailure(fixture, result));
            if (result.Ok)
            {
                result.CurrentGameplayReader = reader;
            }
        }

        private static IEnumerator ProveReaderCardinality(PlayerQaPanel fixture, Result result)
        {
            Require(result, "reader-cardinality",
                fixture.NoGameplayReaderActor != null &&
                fixture.DefaultActor != null &&
                fixture.AmbiguousGameplayReaderActor != null,
                "zero, one and ambiguous Actor Profiles",
                "missing",
                "Reader cardinality requires the three authored Actor fixtures.");
            if (!result.Ok)
            {
                yield break;
            }

            // A suíte chega com a ocorrência canônica em GameplayReady.
            // Ela termina antes da cardinalidade, pois downgrade de Activity é apenas contextual.
            yield return LeaveReaderCardinalityOccurrence(
                fixture, result, "initial-occurrence");
            if (!result.Ok)
            {
                yield break;
            }

            yield return RunFreshReaderCardinalityFixture(
                fixture, result, "zero", fixture.NoGameplayReaderActor, 0,
                expectGameplayReady: true, leaveAfterProof: true);
            if (!result.Ok)
            {
                yield break;
            }

            yield return RunFreshReaderCardinalityFixture(
                fixture, result, "one", fixture.DefaultActor, 1,
                expectGameplayReady: true, leaveAfterProof: true);
            if (!result.Ok)
            {
                yield break;
            }

            yield return RunFreshReaderCardinalityFixture(
                fixture, result, "ambiguous", fixture.AmbiguousGameplayReaderActor, 2,
                expectGameplayReady: false, leaveAfterProof: true);
            if (!result.Ok)
            {
                yield break;
            }

            // Preserva a pré-condição da prova separada de actor-replace sem alterá-la
            // nem tratar replacement como mecanismo de cardinalidade.
            yield return RunFreshReaderCardinalityFixture(
                fixture, result, "actor-replace-precondition", fixture.DefaultActor, 1,
                expectGameplayReady: true, leaveAfterProof: false);
        }

        private static IEnumerator RunFreshReaderCardinalityFixture(
            PlayerQaPanel fixture,
            Result result,
            string fixtureName,
            ActorProfile actor,
            int expectedReaderCount,
            bool expectGameplayReady,
            bool leaveAfterProof)
        {
            if (!TryGetAccess(fixture, out IPlayerSessionScopedAccess access, out string issue))
            {
                result.Fail("reader-cardinality", "available Route-scoped access", issue, issue);
                yield break;
            }

            PlayerParticipationOperationResult open = access.OpenJoining(
                Source, $"player-qa-reader-cardinality-{fixtureName}-open-joining");
            Require(result, "reader-cardinality",
                open != null && open.Completed && open.Snapshot != null && open.Snapshot.JoiningOpen,
                "joining open before fresh occurrence",
                open == null ? "null" : $"{open.Status} {open.Message}",
                "Reader cardinality could not establish the public joining policy for a fresh Player occurrence.");
            string joinIssue = string.Empty;
            if (!result.Ok || !fixture.Probe.TryGetJoinAccess(
                    out ILocalPlayerJoinAccess joinAccess, out joinIssue) ||
                joinAccess == null)
            {
                if (result.Ok)
                {
                    result.Fail("reader-cardinality", "ILocalPlayerJoinAccess", joinIssue,
                        "Fresh Player occurrence requires the authored Route-scoped join access.");
                }

                yield break;
            }

            LocalPlayerJoinResult join = joinAccess.RequestJoin(
                new LocalPlayerJoinRequest(
                    Source, $"player-qa-reader-cardinality-{fixtureName}-join"));
            Require(result, "reader-cardinality",
                join != null && join.Succeeded &&
                join.Slot.PlayerSlotId == ExpectedSlotId(fixture) &&
                join.LocalPlayerHost != null && join.HasLocalPlayerHostEvidence,
                "fresh joined P1 occurrence with Host evidence",
                join == null ? "null" : $"{join.Status} {join.Message}",
                "Fresh Player occurrence did not join the canonical P1 Slot.");
            if (!result.Ok)
            {
                yield break;
            }

            result.PlayerOneHost = join.LocalPlayerHost;
            result.CurrentGameplayReader = null;
            yield return WaitFor(result, "reader-cardinality", () =>
                TryFindSlot(fixture, ExpectedSlotId(fixture), out PlayerSessionScopedSlotObservation fresh) &&
                fresh.IsJoined &&
                fresh.Slot.Revision == join.Slot.Revision &&
                !fresh.Slot.HasSelectedActor &&
                !fresh.IsLogicalActorPrepared &&
                !fresh.IsPhysicallyMaterialized &&
                join.LocalPlayerHost != null && join.LocalPlayerHost.ActorMount != null &&
                join.LocalPlayerHost.ActorMount.GetComponentsInChildren<PlayerActorRuntimeHost>(true).Length == 0,
                "Fresh Player occurrence did not reach mutable selection with no prepared Actor or Runtime Host.");
            if (!result.Ok || !TryFindSlot(
                    fixture, ExpectedSlotId(fixture), out PlayerSessionScopedSlotObservation mutable))
            {
                yield break;
            }

            PlayerActorSelectionResult selection = access.RequestSelectActorProfile(
                new PlayerActorSelectionRequest(
                    ExpectedSlotId(fixture), actor, Source,
                    $"player-qa-reader-cardinality-{fixtureName}-select",
                    mutable.Slot.SelectionRevision));
            Require(result, "reader-cardinality",
                selection != null && selection.Succeeded && selection.StateChanged &&
                selection.Slot.SelectedActorProfile == actor,
                $"{fixtureName} initial Actor selection before preparation",
                selection == null ? "null" : $"{selection.Status} {selection.Message}",
                "A fresh unselected Player occurrence must use SelectActorProfile before preparation.");
            if (!result.Ok)
            {
                yield break;
            }

            yield return RequestActivity(result, "reader-cardinality", fixture.RelocateActivityTrigger,
                expectSuccess: true,
                $"Relocate Activity did not prepare the {fixtureName} Actor through the canonical lifecycle.");
            yield return WaitFor(result, "reader-cardinality", () =>
                HasPreparedReaderCardinality(
                    fixture, result.PlayerOneHost, actor, expectedReaderCount),
                $"Timed out preparing the {fixtureName} Presentation.");
            if (!result.Ok)
            {
                yield break;
            }

            yield return RequestActivity(result, "reader-cardinality", fixture.GameplayReadyActivityTrigger,
                expectSuccess: expectGameplayReady,
                expectGameplayReady
                    ? $"GameplayReady Activity rejected the valid {fixtureName} Presentation."
                    : "GameplayReady Activity did not reject the ambiguous Presentation.");
            if (!result.Ok)
            {
                yield break;
            }

            yield return WaitFor(result, "reader-cardinality", () =>
                HasGameplayReaderCardinality(
                    fixture, result.PlayerOneHost, actor, expectedReaderCount,
                    expectGameplayReady) &&
                (!expectGameplayReady || expectedReaderCount != 1 ||
                    HasBoundCurrentGameplayReader(fixture, result, actor)) &&
                (expectGameplayReady || HasNoBoundCurrentGameplayReader(result.PlayerOneHost)),
                expectGameplayReady
                    ? $"Timed out admitting the {fixtureName} Presentation under GameplayReady authority."
                    : "Timed out waiting for the normal Player gameplay chain to reject ambiguous reader cardinality.");
            if (!result.Ok)
            {
                yield break;
            }

            if (expectGameplayReady && expectedReaderCount == 1 &&
                TryResolveCurrentGameplayReader(
                    result.PlayerOneHost, out PlayerGameplayInputReader currentReader, out _))
            {
                result.CurrentGameplayReader = currentReader;
                Require(result, "reader-cardinality",
                    !result.LastReleasedGameplayBinding.IsValid ||
                    currentReader.CurrentBindingToken != result.LastReleasedGameplayBinding,
                    "fresh occurrence gameplay binding token",
                    currentReader.CurrentBindingToken.StableText,
                    "A fresh Player occurrence reused the gameplay binding token released by its predecessor.");
            }

            Require(result, "reader-cardinality",
                !expectGameplayReady || expectedReaderCount != 0 ||
                HasNoBoundCurrentGameplayReader(result.PlayerOneHost),
                "zero-reader Presentation admitted without a current reader binding",
                result.CurrentGameplayReader == null ? "no-current-reader" : result.CurrentGameplayReader.Diagnostic,
                "A zero-reader Presentation retained gameplay input authority.");
            if (!expectGameplayReady)
            {
                Require(result, "reader-cardinality",
                    fixture.GameplayReadyActivityTrigger.LastMessage != null &&
                    fixture.GameplayReadyActivityTrigger.LastMessage.Contains(
                        "requires at most one PlayerGameplayInputReader"),
                    "HostScoped Player gameplay endpoint cardinality rejection",
                    fixture.GameplayReadyActivityTrigger.LastMessage ?? "missing",
                    "Ambiguous cardinality was not rejected by the normal HostScoped Player gameplay endpoint resolution.");
            }
            if (!result.Ok)
            {
                yield break;
            }

            EmitReaderCardinalityDiagnostic(
                fixtureName, actor, join, selection, result,
                DescribeActivityRequestOutcome(fixture.GameplayReadyActivityTrigger),
                leaveOutcome: leaveAfterProof ? "pending" : "not-requested");
            if (leaveAfterProof)
            {
                yield return LeaveReaderCardinalityOccurrence(
                    fixture, result, fixtureName);
            }
        }

        private static IEnumerator LeaveReaderCardinalityOccurrence(
            PlayerQaPanel fixture,
            Result result,
            string phase)
        {
            if (!TryGetAccess(fixture, out IPlayerSessionScopedAccess access, out string issue) ||
                !TryFindSlot(fixture, ExpectedSlotId(fixture), out PlayerSessionScopedSlotObservation current) ||
                !current.IsJoined)
            {
                result.Fail("reader-cardinality", "current joined P1 occurrence",
                    string.IsNullOrEmpty(issue) ? "P1 unavailable" : issue,
                    "Reader cardinality can only reset through the public Leave operation for the current occurrence.");
                yield break;
            }

            LocalPlayerHostAuthoring previousHost = result.PlayerOneHost;
            PlayerActorRuntimeHost previousRuntimeHost = null;
            GameObject previousPresentation = null;
            if (previousHost != null && previousHost.ActorMount != null)
            {
                PlayerActorRuntimeHost[] runtimeHosts = previousHost.ActorMount
                    .GetComponentsInChildren<PlayerActorRuntimeHost>(true);
                if (runtimeHosts.Length == 1)
                {
                    previousRuntimeHost = runtimeHosts[0];
                    if (previousRuntimeHost != null &&
                        previousRuntimeHost.PresentationMount != null &&
                        previousRuntimeHost.PresentationMount.childCount == 1)
                    {
                        previousPresentation = previousRuntimeHost.PresentationMount
                            .GetChild(0).gameObject;
                    }
                }
            }

            PlayerGameplayInputReader previousReader = result.CurrentGameplayReader;
            PlayerGameplayInputBindingToken previousBinding = previousReader != null
                ? previousReader.CurrentBindingToken
                : default;
            SessionPlayerLeaveResult leave = access.RequestLeave(
                new SessionPlayerLeaveRequest(
                    ExpectedSlotId(fixture), current.Slot.Revision, Source,
                    $"player-qa-reader-cardinality-{phase}-leave"));
            Require(result, "reader-cardinality", leave != null && leave.Succeeded,
                $"{phase} Leave succeeded",
                leave == null ? "null" : $"{leave.Status} {leave.Message}",
                "Fresh Player occurrence teardown must use the public Session Player Leave authority.");
            if (!result.Ok)
            {
                yield break;
            }

            yield return WaitFor(result, "reader-cardinality", () =>
                TryFindSlot(fixture, ExpectedSlotId(fixture), out PlayerSessionScopedSlotObservation released) &&
                !released.IsJoined &&
                !released.IsLogicalActorPrepared &&
                !released.IsPhysicallyMaterialized &&
                (previousReader == null || !previousReader.HasCurrentGameplayBinding) &&
                previousRuntimeHost == null &&
                previousPresentation == null &&
                previousHost == null,
                $"Public Leave did not release the {phase} Actor, Runtime Host, Presentation and gameplay binding.");
            Require(result, "reader-cardinality",
                !previousBinding.IsValid || previousReader == null ||
                !previousReader.HasCurrentGameplayBinding,
                $"{phase} gameplay binding released",
                previousReader == null ? "destroyed" : previousReader.Diagnostic,
                "A previous Player occurrence binding remained authoritative after Leave.");
            if (result.Ok)
            {
                if (previousBinding.IsValid)
                {
                    result.LastReleasedGameplayBinding = previousBinding;
                }

                result.PreviousReaderOccurrenceReleased = true;
                result.PlayerOneHost = null;
                result.CurrentGameplayReader = null;
            }
        }

        private static bool HasPreparedReaderCardinality(
            PlayerQaPanel fixture,
            LocalPlayerHostAuthoring host,
            ActorProfile expectedActor,
            int expectedReaderCount)
        {
            return TryFindSlot(
                       fixture,
                       ExpectedSlotId(fixture),
                       out PlayerSessionScopedSlotObservation slot) &&
                   slot.Slot.SelectedActorProfile == expectedActor &&
                   slot.IsLogicalActorPrepared &&
                   slot.IsPhysicallyMaterialized &&
                   TryGetCurrentGameplayReaderCount(
                       host,
                       out int readerCount,
                       out _) &&
                   readerCount == expectedReaderCount;
        }

        private static bool HasGameplayReaderCardinality(
            PlayerQaPanel fixture,
            LocalPlayerHostAuthoring host,
            ActorProfile expectedActor,
            int expectedReaderCount,
            bool expectedAdmission)
        {
            return TryFindSlot(
                       fixture,
                       ExpectedSlotId(fixture),
                       out PlayerSessionScopedSlotObservation slot) &&
                   slot.Slot.SelectedActorProfile == expectedActor &&
                   slot.HasGameplayAdmissionEvidence &&
                   slot.GameplayAdmission.IsAdmitted == expectedAdmission &&
                   TryGetCurrentGameplayReaderCount(
                       host,
                       out int readerCount,
                       out _) &&
                   readerCount == expectedReaderCount;
        }

        private static bool HasBoundCurrentGameplayReader(
            PlayerQaPanel fixture,
            Result result,
            ActorProfile expectedActor)
        {
            return TryResolveCurrentGameplayReader(
                       result.PlayerOneHost,
                       out PlayerGameplayInputReader reader,
                       out _) &&
                   reader.HasCurrentGameplayBinding &&
                   TryFindSlot(
                       fixture,
                       ExpectedSlotId(fixture),
                       out PlayerSessionScopedSlotObservation slot) &&
                   slot.Slot.SelectedActorProfile == expectedActor &&
                   slot.HasGameplayAdmissionEvidence &&
                   slot.GameplayAdmission.IsAdmitted &&
                   reader.CurrentBindingToken == slot.GameplayAdmission.InputBindingToken;
        }

        private static bool HasNoBoundCurrentGameplayReader(LocalPlayerHostAuthoring host)
        {
            if (!TryGetCurrentGameplayReaders(
                    host,
                    out PlayerGameplayInputReader[] readers,
                    out _))
            {
                return false;
            }

            for (int index = 0; index < readers.Length; index++)
            {
                if (readers[index] != null && readers[index].HasCurrentGameplayBinding)
                {
                    return false;
                }
            }

            return true;
        }

        private static void EmitReaderCardinalityDiagnostic(
            string fixtureName,
            ActorProfile actor,
            LocalPlayerJoinResult join,
            PlayerActorSelectionResult selection,
            Result result,
            string gameplayProjectionOutcome,
            string leaveOutcome)
        {
            LocalPlayerHostAuthoring host = result != null ? result.PlayerOneHost : null;
            PlayerActorRuntimeHost runtimeHost = null;
            GameObject presentation = null;
            PlayerGameplayInputReader boundReader = null;
            int readerCount = -1;
            if (host != null && host.ActorMount != null)
            {
                PlayerActorRuntimeHost[] runtimeHosts = host.ActorMount
                    .GetComponentsInChildren<PlayerActorRuntimeHost>(true);
                if (runtimeHosts.Length == 1)
                {
                    runtimeHost = runtimeHosts[0];
                    if (runtimeHost != null && runtimeHost.PresentationMount != null &&
                        runtimeHost.PresentationMount.childCount == 1)
                    {
                        presentation = runtimeHost.PresentationMount.GetChild(0).gameObject;
                    }
                }

                if (TryGetCurrentGameplayReaders(host, out PlayerGameplayInputReader[] readers, out _))
                {
                    readerCount = readers.Length;
                    for (int index = 0; index < readers.Length; index++)
                    {
                        if (readers[index] != null && readers[index].HasCurrentGameplayBinding)
                        {
                            boundReader = readers[index];
                            break;
                        }
                    }
                }
            }

            Debug.Log(
                $"[QA_PLAYER_READER_CARDINALITY] fixture='{fixtureName}' " +
                $"actor='{DescribeObject(actor)}' " +
                $"playerOccurrence='{(join != null ? join.Slot.Revision.ToString() : "unavailable")}' " +
                $"joinOutcome='{(join != null ? join.Status.ToString() : "not-requested")}' " +
                "selectionOperation='SelectActorProfile' " +
                $"selectionOutcome='{(selection != null ? selection.Status.ToString() : "not-requested")}' " +
                $"preparedActor='{(runtimeHost != null)}' " +
                $"runtimeHost='{DescribeObject(runtimeHost)}' " +
                $"presentation='{DescribeObject(presentation)}' " +
                $"readerCount='{readerCount}' " +
                $"boundReader='{DescribeObject(boundReader)}' " +
                $"stalePreviousReaderBound='{(result != null && !result.PreviousReaderOccurrenceReleased)}' " +
                $"gameplayProjectionOutcome='{gameplayProjectionOutcome}' " +
                $"leaveOutcome='{leaveOutcome}'.");
        }


        private static string DescribeActivityRequestOutcome(ActivityRequestTrigger trigger)
        {
            return trigger == null
                ? "missing"
                : trigger.LastOutcome.ToString();
        }

        private static IEnumerator ProveSecondPlayer(PlayerQaPanel fixture, Result result)
        {
            if (fixture.PlayerTwoSlot == null)
            {
                result.Fail("second-player", "P2 slot", "null", "Player QA requires the P2 Slot Profile.");
                yield break;
            }

            if (!fixture.Probe.TryGetJoinAccess(out ILocalPlayerJoinAccess joinAccess, out string joinIssue))
            {
                result.Fail("second-player", "join access", joinIssue, joinIssue);
                yield break;
            }

            if (TryFindSlot(fixture, ExpectedSlotTwoId(fixture), out PlayerSessionScopedSlotObservation existing) &&
                existing.IsJoined)
            {
                result.Fail(
                    "second-player",
                    "P2 available before second Join",
                    existing.Slot.AllocationState.ToString(),
                    "Second-player proof cannot certify a Join that was already consumed by an earlier case.");
                yield break;
            }

            Keyboard sharedKeyboard = Keyboard.current;
            Require(result, "second-player",
                sharedKeyboard != null,
                "explicit QA Keyboard device",
                "missing",
                "Second-player provisioning proof requires the Editor Keyboard so the QA can explicitly share one deterministic device instead of depending on unpaired-device auto-selection.");
            if (!result.Ok)
            {
                yield break;
            }

            LocalPlayerJoinResult join = joinAccess.RequestJoin(
                new LocalPlayerJoinRequest(
                    Source,
                    "player-qa-join-p2",
                    sharedKeyboard));
            Require(result, "second-player", join != null && join.Succeeded,
                "P2 joined",
                join == null ? "null" : $"{join.Status} {join.Message}",
                "Second-player RequestJoin failed.");
            if (!result.Ok)
            {
                yield break;
            }

            Require(result, "second-player",
                join.Slot.PlayerSlotId == ExpectedSlotTwoId(fixture) &&
                join.Slot.PlayerSlotId != ExpectedSlotId(fixture),
                "distinct P2 slot",
                join.Slot.PlayerSlotId.IsValid ? join.Slot.PlayerSlotId.StableText : "invalid",
                "Second join did not allocate the distinct P2 Slot.");
            yield return WaitForSlot(
                result,
                "second-player",
                fixture,
                slot => slot.IsJoined,
                ExpectedSlotTwoId(fixture));
        }

        private static IEnumerator ProveJoiningControl(PlayerQaPanel fixture, Result result)
        {
            if (!TryGetAccess(fixture, out IPlayerSessionScopedAccess access, out string issue))
            {
                result.Fail("joining-control", "available access", issue, issue);
                yield break;
            }

            if (TryFindSlot(
                    fixture,
                    ExpectedSlotTwoId(fixture),
                    out PlayerSessionScopedSlotObservation occupiedP2) &&
                occupiedP2.IsJoined)
            {
                SessionPlayerLeaveResult prepareLeave = access.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        ExpectedSlotTwoId(fixture),
                        occupiedP2.Slot.Revision,
                        Source,
                        "player-qa-prepare-joining-control"));
                Require(result, "joining-control",
                    prepareLeave != null && prepareLeave.Succeeded,
                    "P2 available before JoiningClosed proof",
                    prepareLeave == null
                        ? "null"
                        : $"{prepareLeave.Status} {prepareLeave.Message}",
                    "Could not establish an available Slot for the JoiningClosed proof.");
                if (!result.Ok)
                {
                    yield break;
                }

                yield return WaitFor(
                    result,
                    "joining-control",
                    () => TryFindSlot(
                              fixture,
                              ExpectedSlotTwoId(fixture),
                              out PlayerSessionScopedSlotObservation availableP2) &&
                          !availableP2.IsJoined,
                    "Timed out preparing an available P2 Slot for JoiningClosed proof.");
            }

            PlayerParticipationOperationResult closed = access.CloseJoining(Source, "player-qa-close-joining");
            Require(result, "joining-control", closed != null && closed.Completed,
                "joining closed",
                closed == null ? "null" : $"{closed.Status} {closed.Message}",
                "CloseJoining failed.");
            if (!result.Ok)
            {
                yield break;
            }

            Require(result, "joining-control",
                fixture.Probe.TryGetJoinAccess(
                    out ILocalPlayerJoinAccess joinAccess,
                    out string joinIssue) &&
                joinAccess != null,
                "join access",
                joinIssue,
                "ILocalPlayerJoinAccess is unavailable during JoiningClosed proof.");
            if (!result.Ok)
            {
                yield break;
            }

            LocalPlayerJoinResult rejected = joinAccess.RequestJoin(
                new LocalPlayerJoinRequest(Source, "player-qa-join-while-closed"));
            Require(result, "joining-control",
                rejected != null &&
                rejected.Status == LocalPlayerJoinStatus.RejectedJoiningClosed,
                "RejectedJoiningClosed",
                rejected == null ? "null" : rejected.Status.ToString(),
                "Join while closed was rejected for a reason other than JoiningClosed.");
            if (!result.Ok)
            {
                yield break;
            }

            PlayerParticipationOperationResult opened = access.OpenJoining(Source, "player-qa-open-joining");
            Require(result, "joining-control", opened != null && opened.Completed,
                "joining opened",
                opened == null ? "null" : $"{opened.Status} {opened.Message}",
                "OpenJoining failed.");
            yield return null;
        }

        private static IEnumerator ProveCommands(PlayerQaPanel fixture, Result result)
        {
            Require(result, "commands",
                fixture.JoinCommand != null &&
                fixture.LeaveCommand != null &&
                fixture.SelectActorCommand != null &&
                fixture.DefaultActorCommand != null &&
                fixture.ReplaceActorCommand != null &&
                fixture.ClearActorCommand != null &&
                fixture.OpenJoiningCommand != null &&
                fixture.CloseJoiningCommand != null,
                "all command triggers", "missing",
                "Player QA scene is missing one or more official Player Session command triggers.");
            if (!result.Ok)
            {
                yield break;
            }

            Require(result, "commands",
                fixture.JoinCommand.TryValidateConfiguration(out string joinIssue),
                "join command valid", joinIssue, joinIssue);
            Require(result, "commands",
                fixture.LeaveCommand.TryValidateConfiguration(out string leaveIssue),
                "leave command valid", leaveIssue, leaveIssue);
            Require(result, "commands",
                fixture.SelectActorCommand.TryValidateConfiguration(out string selectIssue),
                "select-actor command valid", selectIssue, selectIssue);
            Require(result, "commands",
                fixture.DefaultActorCommand.TryValidateConfiguration(out string defaultIssue),
                "default-actor command valid", defaultIssue, defaultIssue);
            Require(result, "commands",
                fixture.ReplaceActorCommand.TryValidateConfiguration(out string replaceIssue),
                "replace-actor command valid", replaceIssue, replaceIssue);
            Require(result, "commands",
                fixture.ClearActorCommand.TryValidateConfiguration(out string clearIssue),
                "clear-actor command valid", clearIssue, clearIssue);
            Require(result, "commands",
                fixture.OpenJoiningCommand.TryValidateConfiguration(out string openJoiningIssue),
                "open-joining command valid", openJoiningIssue, openJoiningIssue);
            Require(result, "commands",
                fixture.CloseJoiningCommand.TryValidateConfiguration(out string closeJoiningIssue),
                "close-joining command valid", closeJoiningIssue, closeJoiningIssue);
            yield return null;
        }

        private static IEnumerator ProveLeave(PlayerQaPanel fixture, Result result)
        {
            if (!TryGetAccess(fixture, out IPlayerSessionScopedAccess access, out string issue))
            {
                result.Fail("leave", "available access", issue, issue);
                yield break;
            }

            if (!TryFindSlot(fixture, ExpectedSlotTwoId(fixture), out PlayerSessionScopedSlotObservation p2) ||
                !p2.IsJoined)
            {
                result.Fail(
                    "leave",
                    "joined P2 before Leave",
                    p2.Slot.PlayerSlotId.IsValid
                        ? p2.Slot.AllocationState.ToString()
                        : "missing",
                    "Leave proof requires the P2 produced by the second-player case.");
                yield break;
            }

            SessionPlayerLeaveResult leave = access.RequestLeave(
                new SessionPlayerLeaveRequest(
                    ExpectedSlotTwoId(fixture),
                    p2.Slot.Revision,
                    Source,
                    "player-qa-leave-p2"));
            Require(result, "leave", leave != null && leave.Succeeded,
                "P2 left",
                leave == null ? "null" : $"{leave.Status} {leave.Message}",
                "RequestLeave for P2 failed.");
            if (!result.Ok)
            {
                yield break;
            }

            yield return WaitFor(
                result,
                "leave",
                () => TryFindSlot(fixture, ExpectedSlotTwoId(fixture), out PlayerSessionScopedSlotObservation after) &&
                      !after.IsJoined,
                "Timed out waiting for P2 to leave.");
        }

        private static IEnumerator ProveRejoin(PlayerQaPanel fixture, Result result)
        {
            if (!fixture.Probe.TryGetJoinAccess(out ILocalPlayerJoinAccess joinAccess, out string joinIssue))
            {
                result.Fail("rejoin", "join access", joinIssue, joinIssue);
                yield break;
            }

            if (TryFindSlot(fixture, ExpectedSlotTwoId(fixture), out PlayerSessionScopedSlotObservation existing) &&
                existing.IsJoined)
            {
                result.Fail(
                    "rejoin",
                    "P2 available after Leave",
                    existing.Slot.AllocationState.ToString(),
                    "Rejoin proof requires Leave to have released P2 first.");
                yield break;
            }

            Keyboard sharedKeyboard = Keyboard.current;
            Require(result, "rejoin",
                sharedKeyboard != null,
                "explicit QA Keyboard device",
                "missing",
                "Rejoin provisioning proof requires the Editor Keyboard so the QA does not depend on an unpaired device becoming available after P2 leaves.");
            if (!result.Ok)
            {
                yield break;
            }

            LocalPlayerJoinResult join = joinAccess.RequestJoin(
                new LocalPlayerJoinRequest(
                    Source,
                    "player-qa-rejoin-p2",
                    sharedKeyboard));
            Require(result, "rejoin", join != null && join.Succeeded,
                "P2 rejoined",
                join == null ? "null" : $"{join.Status} {join.Message}",
                "Rejoin after Leave failed.");
            yield return WaitForSlot(
                result,
                "rejoin",
                fixture,
                slot => slot.IsJoined,
                ExpectedSlotTwoId(fixture));
        }

        private static IEnumerator ProveNegatives(PlayerQaPanel fixture, Result result)
        {
            if (!TryGetAccess(fixture, out IPlayerSessionScopedAccess access, out string issue))
            {
                result.Fail("negatives", "available access", issue, issue);
                yield break;
            }

            if (!TryFindSlot(fixture, ExpectedSlotId(fixture), out PlayerSessionScopedSlotObservation p1) ||
                !p1.IsJoined)
            {
                result.Fail(
                    "negatives",
                    "joined P1 for stale-revision proof",
                    p1.Slot.PlayerSlotId.IsValid
                        ? p1.Slot.AllocationState.ToString()
                        : "missing",
                    "Negative stale-revision proof requires the canonical joined P1.");
                yield break;
            }

            Require(result, "negatives",
                p1.Slot.Revision > 0,
                "positive joined P1 revision",
                p1.Slot.Revision.ToString(),
                "Cannot construct a prior stale revision from the current P1 Slot revision.");
            if (!result.Ok)
            {
                yield break;
            }

            SessionPlayerLeaveResult stale = access.RequestLeave(
                new SessionPlayerLeaveRequest(
                    ExpectedSlotId(fixture),
                    p1.Slot.Revision - 1,
                    Source,
                    "player-qa-stale-leave"));
            Require(result, "negatives", stale != null && !stale.Succeeded,
                "stale leave rejected",
                stale == null ? "null" : $"{stale.Status} {stale.Message}",
                "Stale Leave occurrence revision was not rejected.");
            if (!result.Ok)
            {
                yield break;
            }


            yield return null;
        }

        private static void ProveSpatial(PlayerQaPanel fixture, Result result)
        {
            Require(result, "spatial", fixture.SpatialEntry != null,
                "RoutePlayerSpatialEntryAuthoring", "null",
                "Player QA primary scene requires Route Player Spatial Entry authoring.");
            if (!result.Ok)
            {
                return;
            }

            bool bound = false;
            IReadOnlyList<RoutePlayerSpatialEntryAuthoring.Binding> bindings =
                fixture.SpatialEntry.Bindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                RoutePlayerSpatialEntryAuthoring.Binding binding = bindings[index];
                if (binding.PlayerSlotProfile == fixture.PlayerOneSlot &&
                    binding.PlacementAnchor != null)
                {
                    bound = true;
                    break;
                }
            }

            Require(result, "spatial", bound,
                "P1 spatial anchor", "missing",
                "Route spatial entry does not bind P1 to an explicit world anchor.");
        }

        private static void ProveRelocation(PlayerQaPanel fixture, Result result)
        {
            Require(result, "relocation", fixture.Relocation != null && fixture.RelocateActivity != null,
                "ActivityPlayerRelocationAuthoring", "null",
                "Player QA primary scene requires Activity Player Relocation authoring.");
            if (!result.Ok)
            {
                return;
            }

            bool bound = false;
            IReadOnlyList<ActivityPlayerRelocationAuthoring.Binding> bindings =
                fixture.Relocation.Bindings;
            for (int index = 0; index < bindings.Count; index++)
            {
                ActivityPlayerRelocationAuthoring.Binding binding = bindings[index];
                if (binding.Activity == fixture.RelocateActivity &&
                    binding.PlayerSlotProfile == fixture.PlayerOneSlot &&
                    binding.RelocationAnchor != null)
                {
                    bound = true;
                    break;
                }
            }

            Require(result, "relocation", bound,
                "Relocate Activity P1 relocation anchor", "missing",
                "Activity relocation does not bind the dedicated Relocate Activity and P1 to an explicit world anchor.");
        }
        private static void ValidateManagerHost(
            Result result,
            PlayerQaPanel fixture,
            LocalPlayerHostAuthoring actualHost)
        {
            Require(result, "join",
                fixture.ManagerHostTemplate != null &&
                actualHost != null &&
                actualHost != fixture.ManagerHostTemplate &&
                actualHost.IsJoined &&
                actualHost.PlayerInput != null &&
                actualHost.ActorMount != null &&
                actualHost.PlayerActorRuntimeHostPrefab ==
                    fixture.ManagerHostTemplate.PlayerActorRuntimeHostPrefab,
                "canonical manager host instance",
                actualHost == null ? "null" : actualHost.name,
                "Join did not materialize the canonical Manager Local Player Host composition.");
        }

        private static void EmitGameplayReadyReaderTopologyDiagnostic(Result result)
        {
            const string prefix = "[QA_PLAYER_PRESENTATION_DIAGNOSTIC]";
            LocalPlayerHostAuthoring host = result != null ? result.PlayerOneHost : null;
            if (host == null || host.ActorMount == null)
            {
                Debug.LogError(
                    $"{prefix} host='{DescribeObject(host)}' " +
                    $"actorMount='{DescribeObject(host != null ? host.ActorMount : null)}' " +
                    "runtimeHostCount='0' runtimeHost='<unavailable>' " +
                    "declarationCount='0' declaration='<unavailable>' " +
                    "declarationOnRuntimeHostRoot='false' presentationMount='<unavailable>' " +
                    "presentationChildCount='0' runtimeHostReaderCount='0' " +
                    "declarationSubtreeReaderCount='0' presentationMountReaderCount='0' " +
                    "reason='Canonical joined Local Player Host or Actor Mount is unavailable.'",
                    host);
                return;
            }

            PlayerActorRuntimeHost[] runtimeHosts = host.ActorMount
                .GetComponentsInChildren<PlayerActorRuntimeHost>(true);
            PlayerActorRuntimeHost runtimeHost = runtimeHosts.Length == 1
                ? runtimeHosts[0]
                : null;

            PlayerActorDeclaration[] declarations = runtimeHost != null
                ? runtimeHost.GetComponentsInChildren<PlayerActorDeclaration>(true)
                : System.Array.Empty<PlayerActorDeclaration>();
            PlayerActorDeclaration declaration = runtimeHost != null
                ? runtimeHost.PlayerActorDeclaration
                : null;
            Transform presentationMount = runtimeHost != null
                ? runtimeHost.PresentationMount
                : null;

            PlayerGameplayInputReader[] runtimeHostReaders = runtimeHost != null
                ? runtimeHost.GetComponentsInChildren<PlayerGameplayInputReader>(true)
                : System.Array.Empty<PlayerGameplayInputReader>();
            PlayerGameplayInputReader[] declarationReaders = declaration != null
                ? declaration.GetComponentsInChildren<PlayerGameplayInputReader>(true)
                : System.Array.Empty<PlayerGameplayInputReader>();
            PlayerGameplayInputReader[] presentationReaders = presentationMount != null
                ? presentationMount.GetComponentsInChildren<PlayerGameplayInputReader>(true)
                : System.Array.Empty<PlayerGameplayInputReader>();

            var readerEvidence = new List<PlayerGameplayInputReader>();
            AddDistinctReaders(readerEvidence, runtimeHostReaders);
            AddDistinctReaders(readerEvidence, declarationReaders);
            AddDistinctReaders(readerEvidence, presentationReaders);

            var diagnostic = new StringBuilder(prefix);
            diagnostic.Append(" host='").Append(DescribeObject(host)).Append("'")
                .Append(" actorMount='").Append(DescribeObject(host.ActorMount)).Append("'")
                .Append(" runtimeHostCount='").Append(runtimeHosts.Length).Append("'")
                .Append(" runtimeHost='").Append(DescribeObject(runtimeHost)).Append("'")
                .Append(" declarationCount='").Append(declarations.Length).Append("'")
                .Append(" declaration='").Append(DescribeObject(declaration)).Append("'")
                .Append(" declarationOnRuntimeHostRoot='")
                .Append(runtimeHost != null && declaration != null &&
                    ReferenceEquals(declaration.gameObject, runtimeHost.gameObject))
                .Append("'")
                .Append(" declarationTransformEqualsRuntimeHostTransform='")
                .Append(runtimeHost != null && declaration != null &&
                    declaration.transform == runtimeHost.transform)
                .Append("'")
                .Append(" presentationMount='").Append(DescribeObject(presentationMount)).Append("'")
                .Append(" presentationMountIsChildOfRuntimeHost='")
                .Append(runtimeHost != null && presentationMount != null &&
                    presentationMount.IsChildOf(runtimeHost.transform))
                .Append("'")
                .Append(" presentationChildCount='")
                .Append(presentationMount != null ? presentationMount.childCount : 0)
                .Append("'")
                .Append(" runtimeHostReaderCount='").Append(runtimeHostReaders.Length).Append("'")
                .Append(" declarationSubtreeReaderCount='").Append(declarationReaders.Length).Append("'")
                .Append(" presentationMountReaderCount='").Append(presentationReaders.Length).Append("'");

            if (presentationMount != null)
            {
                for (int index = 0; index < presentationMount.childCount; index++)
                {
                    Transform child = presentationMount.GetChild(index);
                    diagnostic.Append(" presentationChild[").Append(index).Append("]='")
                        .Append(DescribeObject(child))
                        .Append("' activeSelf='").Append(child.gameObject.activeSelf)
                        .Append("' activeInHierarchy='").Append(child.gameObject.activeInHierarchy)
                        .Append("'");
                }
            }

            for (int index = 0; index < readerEvidence.Count; index++)
            {
                PlayerGameplayInputReader reader = readerEvidence[index];
                diagnostic.Append(" readerIndex='").Append(index)
                    .Append("' readerGameObject='").Append(DescribeObject(reader != null ? reader.gameObject : null))
                    .Append("' readerInstanceId='");
                if (reader != null)
                {
                    diagnostic.Append(reader.GetEntityId().ToString());
                }
                else
                {
                    diagnostic.Append("<missing>");
                }

                diagnostic.Append("' hasCurrentGameplayBinding='")
                    .Append(reader != null && reader.HasCurrentGameplayBinding)
                    .Append("' gameplayReady='").Append(reader != null && reader.GameplayReady)
                    .Append("' bindingTokenValid='").Append(reader != null && reader.CurrentBindingToken.IsValid)
                    .Append("' readerIsChildOfRuntimeHost='")
                    .Append(reader != null && runtimeHost != null &&
                        reader.transform.IsChildOf(runtimeHost.transform))
                    .Append("' readerIsChildOfDeclaration='")
                    .Append(reader != null && declaration != null &&
                        reader.transform.IsChildOf(declaration.transform))
                    .Append("'");
            }

            Debug.Log(diagnostic.ToString(), host);
        }

        private static void AddDistinctReaders(
            List<PlayerGameplayInputReader> destination,
            PlayerGameplayInputReader[] candidates)
        {
            for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
            {
                PlayerGameplayInputReader candidate = candidates[candidateIndex];
                bool alreadyPresent = false;
                for (int existingIndex = 0; existingIndex < destination.Count; existingIndex++)
                {
                    if (ReferenceEquals(destination[existingIndex], candidate))
                    {
                        alreadyPresent = true;
                        break;
                    }
                }

                if (!alreadyPresent)
                {
                    destination.Add(candidate);
                }
            }
        }

        private static string DescribeObject(UnityEngine.Object value)
        {
            return value != null ? value.name : "<missing>";
        }

        private static string DescribeGameplayReadyReaderFailure(
            PlayerQaPanel fixture,
            Result result)
        {
            if (!TryGetCurrentGameplayReaders(
                    result != null ? result.PlayerOneHost : null,
                    out PlayerGameplayInputReader[] readers,
                    out string topologyIssue))
            {
                return $"GameplayReady reader not found because canonical topology is invalid: {topologyIssue}";
            }

            if (readers.Length == 0)
            {
                return "GameplayReady reader is absent from the Default Presentation hierarchy.";
            }

            if (readers.Length != 1 || readers[0] == null)
            {
                return $"GameplayReady reader topology is invalid: expected exactly one reader, found '{readers.Length}'.";
            }

            PlayerGameplayInputReader reader = readers[0];
            if (!reader.HasCurrentGameplayBinding)
            {
                return "GameplayReady reader was found but remains unbound.";
            }

            if (!reader.CurrentBindingToken.IsValid)
            {
                return "GameplayReady reader is bound but its binding token is invalid.";
            }

            if (!TryFindSlot(
                    fixture,
                    ExpectedSlotId(fixture),
                    out PlayerSessionScopedSlotObservation slot) ||
                !slot.HasGameplayAdmissionEvidence ||
                reader.CurrentBindingToken != slot.GameplayAdmission.InputBindingToken)
            {
                return "GameplayReady reader is bound but its binding token does not match current gameplay admission.";
            }

            if (!reader.GameplayReady)
            {
                return IsCurrentPresentationInactive(result != null ? result.PlayerOneHost : null)
                    ? "GameplayReady reader is bound with a valid token but its Presentation occurrence is inactive."
                    : "GameplayReady reader is bound with a valid token but is not GameplayReady.";
            }

            return "GameplayReady reader verification did not reach the expected terminal state.";
        }

        private static bool TryResolveCurrentGameplayReader(
            LocalPlayerHostAuthoring localPlayerHost,
            out PlayerGameplayInputReader reader,
            out string issue)
        {
            reader = null;
            if (!TryGetCurrentGameplayReaders(localPlayerHost, out PlayerGameplayInputReader[] readers, out issue))
            {
                return false;
            }

            if (readers.Length != 1)
            {
                issue =
                    $"Canonical Player Presentation requires exactly one PlayerGameplayInputReader. Found '{readers.Length}'.";
                return false;
            }

            reader = readers[0];
            return reader != null;
        }

        private static bool TryGetCurrentGameplayReaderCount(
            LocalPlayerHostAuthoring localPlayerHost,
            out int readerCount,
            out string issue)
        {
            readerCount = 0;
            if (!TryGetCurrentGameplayReaders(localPlayerHost, out PlayerGameplayInputReader[] readers, out issue))
            {
                return false;
            }

            readerCount = readers.Length;
            return true;
        }

        private static bool TryGetCurrentGameplayReaders(
            LocalPlayerHostAuthoring localPlayerHost,
            out PlayerGameplayInputReader[] readers,
            out string issue)
        {
            readers = System.Array.Empty<PlayerGameplayInputReader>();
            issue = string.Empty;
            if (localPlayerHost == null || localPlayerHost.ActorMount == null)
            {
                issue = "Joined Local Player Host or its Actor Mount is unavailable.";
                return false;
            }

            PlayerActorRuntimeHost[] runtimeHosts = localPlayerHost.ActorMount
                .GetComponentsInChildren<PlayerActorRuntimeHost>(true);
            if (runtimeHosts.Length != 1 || runtimeHosts[0] == null ||
                runtimeHosts[0].transform.parent != localPlayerHost.ActorMount)
            {
                issue =
                    $"Joined Local Player Host requires exactly one direct PlayerActorRuntimeHost. Found '{runtimeHosts.Length}'.";
                return false;
            }

            PlayerActorRuntimeHost runtimeHost = runtimeHosts[0];
            if (!runtimeHost.TryValidateConfiguration(out issue) ||
                runtimeHost.PresentationMount == null ||
                runtimeHost.PresentationMount.parent != runtimeHost.transform ||
                runtimeHost.PresentationMount.childCount != 1)
            {
                issue = string.IsNullOrEmpty(issue)
                    ? "Canonical Player Actor Runtime Host requires one direct Presentation instance."
                    : issue;
                return false;
            }

            readers = runtimeHost.PresentationMount
                .GetComponentsInChildren<PlayerGameplayInputReader>(true);
            return true;
        }

        private static bool IsCurrentPresentationInactive(
            LocalPlayerHostAuthoring localPlayerHost)
        {
            if (localPlayerHost == null || localPlayerHost.ActorMount == null)
            {
                return false;
            }

            PlayerActorRuntimeHost[] runtimeHosts = localPlayerHost.ActorMount
                .GetComponentsInChildren<PlayerActorRuntimeHost>(true);
            if (runtimeHosts.Length != 1 || runtimeHosts[0] == null ||
                runtimeHosts[0].PresentationMount == null ||
                runtimeHosts[0].PresentationMount.childCount != 1)
            {
                return false;
            }

            return !runtimeHosts[0].PresentationMount.GetChild(0).gameObject.activeSelf;
        }

        private static bool TryGetAccess(
            PlayerQaPanel fixture,
            out IPlayerSessionScopedAccess access,
            out string issue)
        {
            access = null;
            issue = "probe missing";
            return fixture.Probe != null &&
                fixture.Probe.TryGetAccess(out access, out issue) &&
                access != null &&
                access.Snapshot.IsAvailable;
        }

        private static bool TryFindSlot(
            PlayerQaPanel fixture,
            PlayerSlotId slotId,
            out PlayerSessionScopedSlotObservation slot)
        {
            slot = default;
            if (fixture.Observer == null ||
                !fixture.Observer.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) ||
                observation == null ||
                !observation.IsAvailable)
            {
                return false;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                PlayerSessionScopedSlotObservation candidate = observation.Slots[index];
                if (candidate.Slot.PlayerSlotId == slotId)
                {
                    slot = candidate;
                    return true;
                }
            }

            return false;
        }

        private static PlayerSessionScopedSlotObservation FindSlot(
            PlayerSessionScopedObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            for (int index = 0; index < observation.Slots.Count; index++)
            {
                PlayerSessionScopedSlotObservation candidate = observation.Slots[index];
                if (candidate.Slot.PlayerSlotId == slotId)
                {
                    return candidate;
                }
            }

            return default;
        }

        private static IEnumerator WaitForSlot(
            Result result,
            string caseId,
            PlayerQaPanel fixture,
            Func<PlayerSessionScopedSlotObservation, bool> predicate,
            PlayerSlotId? slotId = null)
        {
            PlayerSlotId expected = slotId ?? ExpectedSlotId(fixture);
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                if (TryFindSlot(
                        fixture,
                        expected,
                        out PlayerSessionScopedSlotObservation slot) &&
                    predicate(slot))
                {
                    yield break;
                }

                yield return null;
            }

            if (!result.Ok)
            {
                yield break;
            }

            string diagnostic = DescribeSlotObservation(fixture, expected);
            UnityEngine.Debug.LogError(
                $"[QA_PLAYER_SLOT_OBSERVATION] case='{caseId}' {diagnostic}",
                fixture);
            result.Fail(
                caseId,
                "condition met",
                diagnostic,
                "Timed out waiting for the expected Player Slot observation.");
        }

        private static string DescribeSlotObservation(
            PlayerQaPanel fixture,
            PlayerSlotId expected)
        {
            string expectedText = expected.IsValid ? expected.StableText : "<invalid>";
            if (fixture == null || fixture.Observer == null)
            {
                return $"status='timeout' expectedSlot='{expectedText}' observer='missing' " +
                       "observationAvailable='false' observedSlots='0' slotFound='false'";
            }

            if (!fixture.Observer.TryGetObservation(
                    out PlayerSessionScopedObservationSnapshot observation) ||
                observation == null)
            {
                return $"status='timeout' expectedSlot='{expectedText}' observer='available' " +
                       "observationAvailable='false' observedSlots='0' slotFound='false'";
            }

            int observedSlots = observation.Slots.Count;
            for (int index = 0; index < observedSlots; index++)
            {
                PlayerSessionScopedSlotObservation candidate = observation.Slots[index];
                if (candidate.Slot.PlayerSlotId != expected)
                {
                    continue;
                }

                string slotText = candidate.Slot.PlayerSlotId.IsValid
                    ? candidate.Slot.PlayerSlotId.StableText
                    : "<invalid>";
                string assignmentOrigin = candidate.HasHostEvidence
                    ? candidate.HostEvidence.AssignmentOrigin.ToString()
                    : "<none>";
                return $"status='timeout' expectedSlot='{expectedText}' " +
                       $"observationAvailable='{observation.IsAvailable}' " +
                       $"observedSlots='{observedSlots}' slotFound='true' " +
                       $"slot='{slotText}' isJoined='{candidate.IsJoined}' " +
                       $"allocationState='{candidate.Slot.AllocationState}' " +
                       $"hasHostEvidence='{candidate.HasHostEvidence}' " +
                       $"assignmentOrigin='{assignmentOrigin}'";
            }

            return $"status='timeout' expectedSlot='{expectedText}' " +
                   $"observationAvailable='{observation.IsAvailable}' " +
                   $"observedSlots='{observedSlots}' slotFound='false'";
        }

        private static IEnumerator RequestActivity(
            Result result,
            string caseId,
            ActivityRequestTrigger trigger,
            bool expectSuccess,
            string failure)
        {
            Require(result, caseId,
                trigger != null && trigger.TargetActivity != null,
                "configured Activity trigger",
                trigger == null ? "missing" : "target missing",
                failure);
            if (!result.Ok)
            {
                yield break;
            }

            trigger.RequestActivity();
            yield return WaitFor(
                result,
                caseId,
                () => !trigger.IsRequestInFlight &&
                      (trigger.LastRequestSucceeded ||
                       trigger.LastRequestFailed ||
                       trigger.LastRequestIgnored),
                "Timed out waiting for the Activity request outcome.");
            if (!result.Ok)
            {
                yield break;
            }

            bool expectedOutcome = expectSuccess
                ? trigger.LastRequestSucceeded
                : trigger.LastRequestFailed;
            Require(result, caseId,
                expectedOutcome,
                expectSuccess ? "successful Activity request" : "failed Activity request",
                $"outcome={trigger.LastOutcome} message={trigger.LastMessage}",
                failure);
        }

        private static IEnumerator WaitFor(
            Result result,
            string caseId,
            Func<bool> predicate,
            string timeoutMessage)
        {
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                if (predicate())
                {
                    yield break;
                }

                yield return null;
            }

            if (result.Ok)
            {
                result.Fail(caseId, "condition met", "timeout", timeoutMessage);
            }
        }

        private static PlayerSlotId ExpectedSlotId(PlayerQaPanel fixture)
        {
            if (fixture.PlayerOneSlot != null &&
                fixture.PlayerOneSlot.TryGetPlayerSlotId(out PlayerSlotId slotId, out _))
            {
                return slotId;
            }

            return default;
        }

        private static PlayerSlotId ExpectedSlotTwoId(PlayerQaPanel fixture)
        {
            if (fixture.PlayerTwoSlot != null &&
                fixture.PlayerTwoSlot.TryGetPlayerSlotId(out PlayerSlotId slotId, out _))
            {
                return slotId;
            }

            return default;
        }

        private static void Require(
            Result result,
            string caseId,
            bool condition,
            string expected,
            string actual,
            string message)
        {
            if (!condition && result.Ok)
            {
                result.Fail(caseId, expected, actual, message);
            }
        }
    }
}

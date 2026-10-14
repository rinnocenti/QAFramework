using System;
using System.Collections;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.QaFramework.Certification;
using Immersive.QaFramework.PlayerSession;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.IfAdr046
{
    /// <summary>Integrated public lifecycle checks for occurrence-scoped Join bindings.</summary>
    [DisallowMultipleComponent]
    public sealed class QaIfAdr046ScopedBindingScenario : MonoBehaviour
    {
        private const string ScenarioId = "IF-ADR-046-I4.2-SCOPED-BINDING";
        private const string ActivityAScenePath = "Assets/QA-IF-ADR-046/OccurrenceBinding/Scenes/I4_Binding_Activity_A.unity";
        private const string ActivityBScenePath = "Assets/QA-IF-ADR-046/OccurrenceBinding/Scenes/I4_Binding_Activity_B.unity";
        private const string RouteAScenePath = "Assets/QA-IF-ADR-046/OccurrenceBinding/Scenes/I4_Binding_Route_A.unity";
        private const string RouteBScenePath = "Assets/QA-IF-ADR-046/OccurrenceBinding/Scenes/I4_Binding_Route_B.unity";

        [SerializeField] private RouteAsset routeA;
        [SerializeField] private RouteAsset routeB;
        [SerializeField] private ActivityAsset activityA;
        [SerializeField] private ActivityAsset activityB;
        [SerializeField] private RouteRequestTrigger routeToB;
        [SerializeField] private RouteRequestTrigger routeToA;
        [SerializeField] private ActivityRequestTrigger activityToB;
        [SerializeField] private ActivityRequestTrigger activityToA;
        [SerializeField, Min(1)] private int frameBudget = 900;

        private bool _started;
        private bool _terminal;
        private bool _failed;
        private string _firstIssue = string.Empty;
        private string _cleanupIssue = string.Empty;
        private string _joinEvidence = string.Empty;
        private string _activityOccurrenceEvidence = string.Empty;
        private string _caseEvidence = string.Empty;
        private string _routeEntryEvidence = string.Empty;
        private int _completedCases;
        private int _initialPlayerInputCount;
        private int _initialDeviceCount;
        private Keyboard _ownedKeyboard;
        private PlayerSessionObserver _activeObserver;
        private bool _joiningOpened;
        private bool _observingRouteAReentry;
        private bool _routeAUnloadObserved;
        private bool _routeAReloadObserved;
        private ulong _routeAExpectedUnloadHandle;
        private ulong _routeAObservedUnloadHandle;
        private ulong _routeAObservedReloadHandle;
        private int _observedActivityOccurrence;

        private void Start()
        {
            if (_started) return;
            _started = true;
            _initialPlayerInputCount = PlayerInput.all.Count;
            _initialDeviceCount = InputSystem.devices.Count;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (!_started || _terminal) return;
            StopRouteAReentryObservation();
            SetFailure("Scenario was disabled before its terminal cleanup completed.");
            CleanupActiveJoinSynchronously();
            PublishTerminals(false);
        }

        private IEnumerator Run()
        {
            string issue = string.Empty;
            if (!ValidateAuthoring())
            {
                yield return CleanupAndPublish();
                yield break;
            }

            yield return WaitForCurrentPair(routeA, activityA, ActivityAScenePath, RouteAScenePath);
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }

            PlayerSessionObserver observerA1 = FindExactActivityObserver(ActivityAScenePath);
            PlayerSessionJoinTrigger activityJoinA1 = FindExactActivityJoin(ActivityAScenePath);
            PlayerSessionJoinTrigger routeJoinA1 = FindExactRouteJoin(RouteAScenePath);
            yield return WaitForPublicRouteEntry(routeA, RouteAScenePath, "initial Route A");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            RouteContentLifecycleEvents routeLifecycleA1 = FindExactRouteLifecycleEvents(RouteAScenePath);
            RouteContentContribution routeContributionA1 = FindExactRouteContribution(RouteAScenePath);
            bool routeEntryA1Observed = HasPublicRouteEntered(routeLifecycleA1, routeA, routeContributionA1);
            yield return WaitForActivityOccurrence(observerA1, activityA, ActivityAScenePath, "initial Activity A");
            if (HasFailure)
            {
                yield return CleanupAndPublish(); yield break;
            }
            int activityOccurrenceA1 = _observedActivityOccurrence;
            ulong activitySceneHandleA1 = GetSceneHandle(ActivityAScenePath);
            ulong routeSceneHandleA1 = GetSceneHandle(RouteAScenePath);

            yield return JoinAndLeave(activityJoinA1, observerA1, "activity-a1-current");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            yield return JoinAndLeave(routeJoinA1, observerA1, "route-a1-current");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }

            RequestActivity(activityToB, activityB, "Activity A to B");
            yield return WaitForActivityRequest(activityToB, "Activity A to B");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForCurrentPair(routeA, activityB, ActivityBScenePath, RouteAScenePath);
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            RequestActivity(activityToA, activityA, "Activity B to A");
            yield return WaitForActivityRequest(activityToA, "Activity B to A");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForCurrentPair(routeA, activityA, ActivityAScenePath, RouteAScenePath);
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }

            PlayerSessionObserver observerA2 = FindExactActivityObserver(ActivityAScenePath);
            PlayerSessionJoinTrigger activityJoinA2 = FindExactActivityJoin(ActivityAScenePath);
            ActivityContentContribution activityContributionA2 = FindExactActivityContribution(ActivityAScenePath);
            yield return WaitForActivityOccurrence(observerA2, activityA, ActivityAScenePath, "Activity A re-entry");
            if (HasFailure)
            {
                yield return CleanupAndPublish(); yield break;
            }
            int activityOccurrenceA2 = _observedActivityOccurrence;
            if (
                activityContributionA2 == null || !ReferenceEquals(activityContributionA2.Activity, activityA) ||
                ReferenceEquals(activityJoinA1, activityJoinA2) ||
                ReferenceEquals(observerA1, observerA2) ||
                GetSceneHandle(ActivityAScenePath) == activitySceneHandleA1 || activityOccurrenceA2 <= activityOccurrenceA1)
            {
                SetFailure("Activity A re-entry did not prove same definition with a distinct occurrence. " + issue);
                yield return CleanupAndPublish(); yield break;
            }
            _activityOccurrenceEvidence = Append(_activityOccurrenceEvidence,
                "activity-a1-to-a2=" + activityOccurrenceA1 + "->" + activityOccurrenceA2 + " sceneHandle=" + activitySceneHandleA1 + "->" + GetSceneHandle(ActivityAScenePath));
            _completedCases++;
            _caseEvidence = Append(_caseEvidence, "activity-a-reentry-new-occurrence=PASS");
            yield return JoinAndLeave(activityJoinA2, observerA2, "activity-a2-current");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }

            BeginRouteAReentryObservation(routeSceneHandleA1);
            RequestRoute(routeToB, routeB, "Route A to B");
            yield return WaitForRouteRequest(routeToB, "Route A to B");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForCurrentPair(routeB, activityB, ActivityBScenePath, RouteBScenePath);
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            RequestRoute(routeToA, routeA, "Route B to A");
            yield return WaitForRouteRequest(routeToA, "Route B to A");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForCurrentPair(routeA, activityA, ActivityAScenePath, RouteAScenePath);
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForRouteAReentryObservation();
            StopRouteAReentryObservation();
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }

            PlayerSessionObserver observerA3 = FindExactActivityObserver(ActivityAScenePath);
            PlayerSessionJoinTrigger activityJoinA3 = FindExactActivityJoin(ActivityAScenePath);
            PlayerSessionJoinTrigger routeJoinA2 = FindExactRouteJoin(RouteAScenePath);
            ulong routeSceneHandleA2 = GetSceneHandle(RouteAScenePath);
            RouteContentContribution routeContributionA2 = FindExactRouteContribution(RouteAScenePath);
            yield return WaitForPublicRouteEntry(routeA, RouteAScenePath, "Route A re-entry");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            RouteContentLifecycleEvents routeLifecycleA2 = FindExactRouteLifecycleEvents(RouteAScenePath);
            yield return WaitForActivityOccurrence(observerA3, activityA, ActivityAScenePath, "Route A re-entry Activity");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            int activityOccurrenceA3 = _observedActivityOccurrence;
            bool routeAssetSame = routeContributionA2 != null && ReferenceEquals(routeContributionA2.Route, routeA);
            bool routeSceneReloaded = _routeAUnloadObserved && _routeAReloadObserved;
            RouteContentLifecycleContext routeContextA2 = routeLifecycleA2 != null ? routeLifecycleA2.LastRouteContentContext : default;
            bool routeLifecycleActive = routeLifecycleA2 != null && routeLifecycleA2.IsRouteContentActive;
            bool routeLifecycleContextAvailable = routeLifecycleA2 != null && routeLifecycleA2.HasRouteContentContext;
            bool routeLifecycleEntered = routeLifecycleContextAvailable && routeContextA2.IsEntered;
            bool routeLifecycleAssetSame = routeLifecycleContextAvailable && ReferenceEquals(routeContextA2.Route, routeA);
            bool routeLifecycleContributionSame = routeLifecycleContextAvailable && ReferenceEquals(routeContextA2.Binding, routeContributionA2);
            bool routeLifecycleInstanceChanged = HasManagedReference(routeLifecycleA1) && HasManagedReference(routeLifecycleA2) && !ReferenceEquals(routeLifecycleA1, routeLifecycleA2);
            bool routeJoinInstanceChanged = HasManagedReference(routeJoinA1) && HasManagedReference(routeJoinA2) && !ReferenceEquals(routeJoinA1, routeJoinA2);
            bool activityObserverChanged = HasManagedReference(observerA2) && HasManagedReference(observerA3) && !ReferenceEquals(observerA2, observerA3);
            bool activityOccurrenceAdvanced = activityOccurrenceA3 > activityOccurrenceA2;
            _routeEntryEvidence = $"routeAssetSame='{routeAssetSame}' sceneHandle='{routeSceneHandleA1}->{routeSceneHandleA2}' unloadObserved='{_routeAUnloadObserved}' unloadedHandle='{_routeAObservedUnloadHandle}' reloadObserved='{_routeAReloadObserved}' reloadedHandle='{_routeAObservedReloadHandle}' routeSceneReloaded='{routeSceneReloaded}' initialLifecycleEntered='{routeEntryA1Observed}' currentLifecycleActive='{routeLifecycleActive}' currentLifecycleContextAvailable='{routeLifecycleContextAvailable}' currentLifecycleEntered='{routeLifecycleEntered}' currentLifecycleAssetSame='{routeLifecycleAssetSame}' currentLifecycleContributionSame='{routeLifecycleContributionSame}' lifecycleInstanceChanged='{routeLifecycleInstanceChanged}' routeJoinInstanceChanged='{routeJoinInstanceChanged}' activityObserverChanged='{activityObserverChanged}' activityOccurrence='{activityOccurrenceA2}->{activityOccurrenceA3}' activityOccurrenceAdvanced='{activityOccurrenceAdvanced}' routeOccurrenceOrdinal='not-exposed-by-public-contract'";

            if (!routeAssetSame)
            {
                SetFailure("Route A re-entry asset identity failed: the current RouteContentContribution does not reference the exact Route A asset. " + _routeEntryEvidence);
                yield return CleanupAndPublish(); yield break;
            }
            if (!routeSceneReloaded)
            {
                SetFailure("Route A re-entry scene identity failed: its loaded Unity scene handle did not change. " + _routeEntryEvidence);
                yield return CleanupAndPublish(); yield break;
            }
            if (!routeLifecycleContextAvailable)
            {
                SetBlocked("Route A re-entry has no public RouteContentLifecycleContext; Route lifecycle cannot be observed in this fixture. " + _routeEntryEvidence);
                yield return CleanupAndPublish(); yield break;
            }
            if (!routeLifecycleActive || !routeLifecycleEntered || !routeLifecycleAssetSame || !routeLifecycleContributionSame)
            {
                SetFailure("Route A re-entry lifecycle context does not show an active Entered phase for the exact Route A asset and current contribution. " + _routeEntryEvidence);
                yield return CleanupAndPublish(); yield break;
            }
            if (!routeLifecycleInstanceChanged || !routeJoinInstanceChanged)
            {
                SetFailure("Route A re-entry component identity failed: the reloaded scene did not provide new Route lifecycle and Join component instances. " + _routeEntryEvidence);
                yield return CleanupAndPublish(); yield break;
            }
            if (!activityObserverChanged || !activityOccurrenceAdvanced)
            {
                SetFailure("Route A re-entry Activity evidence failed independently of Route entry: the Activity observer was reused or its own published occurrence did not advance. " + _routeEntryEvidence);
                yield return CleanupAndPublish(); yield break;
            }
            _completedCases++;
            _caseEvidence = Append(_caseEvidence, "route-a-reentry-new-occurrence=PASS");
            yield return JoinAndLeave(routeJoinA2, observerA3, "route-a2-current");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }

            PlayerSessionJoinTrigger activityJoinBeforeOwnerChange = activityJoinA3;
            int activityOccurrenceBeforeOwnerChange = activityOccurrenceA3;
            RequestRoute(routeToB, routeB, "Owning Route A to B");
            yield return WaitForRouteRequest(routeToB, "Owning Route A to B");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForCurrentPair(routeB, activityB, ActivityBScenePath, RouteBScenePath);
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            RequestRoute(routeToA, routeA, "Owning Route B to A");
            yield return WaitForRouteRequest(routeToA, "Owning Route B to A");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            yield return WaitForCurrentPair(routeA, activityA, ActivityAScenePath, RouteAScenePath);
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }

            PlayerSessionObserver observerA4 = FindExactActivityObserver(ActivityAScenePath);
            PlayerSessionJoinTrigger activityJoinA4 = FindExactActivityJoin(ActivityAScenePath);
            ActivityContentContribution activityContributionA4 = FindExactActivityContribution(ActivityAScenePath);
            yield return WaitForActivityOccurrence(observerA4, activityA, ActivityAScenePath, "Activity A after owning Route change");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }
            int activityOccurrenceA4 = _observedActivityOccurrence;
            if (activityContributionA4 == null || !ReferenceEquals(activityContributionA4.Activity, activityA) ||
                ReferenceEquals(activityJoinBeforeOwnerChange, activityJoinA4) ||
                ReferenceEquals(observerA3, observerA4) || activityOccurrenceA4 <= activityOccurrenceBeforeOwnerChange)
            {
                SetFailure("Changing the owning Route did not recreate Activity A as a distinct observed occurrence. " + issue);
                yield return CleanupAndPublish(); yield break;
            }
            _activityOccurrenceEvidence = Append(_activityOccurrenceEvidence,
                "owner-route-activity-a3-to-a4=" + activityOccurrenceBeforeOwnerChange + "->" + activityOccurrenceA4);
            _completedCases++;
            _caseEvidence = Append(_caseEvidence, "owner-route-change-recreates-activity-occurrence=PASS");
            yield return JoinAndLeave(activityJoinA4, observerA4, "activity-a-after-owner-route-change-current");
            if (HasFailure) { yield return CleanupAndPublish(); yield break; }

            yield return CleanupAndPublish();
        }

        private IEnumerator JoinAndLeave(PlayerSessionJoinTrigger trigger, PlayerSessionObserver observer, string caseId)
        {
            if (trigger == null || observer == null || !observer.IsAvailable)
            {
                SetFailure($"{caseId}: current public Join trigger or Observer unavailable.");
                yield break;
            }
            if (!observer.TryGetSnapshot(out PlayerParticipationSnapshot baseline) || !IsEmptyClosed(baseline))
            {
                SetFailure(caseId + ": Session was not empty, available and Joining-closed before Join.");
                yield break;
            }

            _activeObserver = observer;
            PlayerParticipationOperationResult opened =
                QaPlayerSessionCommandAccess.OpenJoining(observer);
            if (opened == null || !opened.Succeeded)
            {
                SetFailure(caseId + ": public OpenJoining failed. " + (opened != null ? opened.ToDiagnosticString() : "null result"));
                yield break;
            }
            _joiningOpened = true;

            try { _ownedKeyboard = InputSystem.AddDevice<Keyboard>("QA IF-ADR-046 I4.2 " + caseId); }
            catch (Exception exception)
            {
                SetBlocked(caseId + ": could not add owned synthetic Keyboard: " + exception.Message);
                yield break;
            }
            if (_ownedKeyboard == null || !_ownedKeyboard.added)
            {
                SetBlocked(caseId + ": Input System did not add the owned synthetic Keyboard.");
                yield break;
            }

            LocalPlayerJoinOutcome outcome = trigger.RequestJoin(
                new LocalPlayerJoinRequest(ScenarioId, caseId, _ownedKeyboard, "QA.Keyboard"));
            if (outcome == null || outcome.Code != LocalPlayerJoinOutcomeCode.Joined ||
                outcome.AdmissionState != LocalPlayerJoinAdmissionState.Admitted)
            {
                SetFailure(caseId + ": current occurrence Join did not return Joined/Admitted. " + (outcome != null ? outcome.Code + "/" + outcome.AdmissionState : "null outcome"));
                yield break;
            }
            _joinEvidence = Append(_joinEvidence, caseId + ":" + outcome.Code + "/" + outcome.AdmissionState);

            if (!observer.TryGetSnapshot(out PlayerParticipationSnapshot joined) || joined.JoinedCount != 1 ||
                joined.ReservedCount != 0 || joined.Slots.Count != 1 || !joined.Slots[0].IsJoined ||
                PlayerInput.all.Count != _initialPlayerInputCount + 1 ||
                !observer.TryGetObservation(out PlayerSessionScopedObservationSnapshot joinedObservation) ||
                joinedObservation.Slots.Count != 1 || !joinedObservation.Slots[0].HasHostEvidence ||
                !joinedObservation.Slots[0].HostEvidence.IsConfirmed ||
                joinedObservation.Slots[0].HostEvidence.PhysicalProvisioningMode != PlayerHostProvisioningMode.ManagerProvisioned)
            {
                SetFailure(caseId + ": Joined outcome did not correlate with one current Slot and confirmed Manager-Provisioned Host/PlayerInput.");
                yield break;
            }

            PlayerSlotRuntimeSnapshot slot = joined.Slots[0];
            SessionPlayerLeaveResult leave = QaPlayerSessionCommandAccess.Leave(
                observer,
                slot.Profile);
            if (leave == null || !leave.Succeeded || !leave.ProvisioningReleased || !leave.TerminalCommitted)
            {
                SetFailure(caseId + ": Leave did not confirm ProvisioningReleased and TerminalCommitted. " + (leave != null ? leave.ToDiagnosticString() : "null result"));
                yield break;
            }

            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (observer.TryGetSnapshot(out PlayerParticipationSnapshot released) && released.JoinedCount == 0 &&
                    released.ReservedCount == 0 && released.LeavingCount == 0) break;
                yield return null;
            }
            if (!observer.TryGetSnapshot(out PlayerParticipationSnapshot beforeClose) || !IsEmptyOpen(beforeClose))
            {
                SetFailure(caseId + ": Session did not return to empty state before CloseJoining.");
                yield break;
            }

            PlayerParticipationOperationResult closed =
                QaPlayerSessionCommandAccess.CloseJoining(observer);
            if (closed == null || !closed.Succeeded)
            {
                SetFailure(caseId + ": public CloseJoining failed. " + (closed != null ? closed.ToDiagnosticString() : "null result"));
                yield break;
            }
            _joiningOpened = false;
            RemoveOwnedKeyboard();

            bool clean = false;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (IsSessionRestored(observer)) { clean = true; break; }
                yield return null;
            }
            if (!clean)
            {
                SetFailure(caseId + ": Session, PlayerInput, Host evidence, or synthetic Keyboard did not return to baseline.");
                yield break;
            }
            _activeObserver = null;
            _completedCases++;
            _caseEvidence = Append(_caseEvidence, caseId + "=PASS");
        }

        private IEnumerator WaitForCurrentPair(RouteAsset expectedRoute, ActivityAsset expectedActivity, string activityScenePath, string routeScenePath)
        {
            ActivityReadinessEvents readiness = null;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                bool routeCurrent = IsExactRouteCurrent(expectedRoute, routeScenePath);
                bool activityCurrent = IsExactActivityCurrent(expectedActivity, activityScenePath);
                if (routeCurrent && activityCurrent)
                {
                    ActivityReadinessEvents[] observers = FindComponents<ActivityReadinessEvents>(SceneManager.GetSceneByPath(activityScenePath));
                    if (observers.Length != 1)
                    {
                        SetFailure($"Activity publication observer cardinality is invalid in '{activityScenePath}'; expected=1 actual={observers.Length}.");
                        yield break;
                    }
                    readiness = observers[0];
                    if (IsReadinessPublished(readiness, expectedActivity)) yield break;
                }
                yield return null;
            }
            ActivityReadinessSnapshot last = readiness != null ? readiness.LastSnapshot : default;
            SetFailure($"Timed out waiting for Route/Activity composition and public Activity occurrence publication. expectedRoute='{expectedRoute?.RouteId.StableText}' routeScene='{routeScenePath}' expectedActivity='{expectedActivity?.ActivityId.StableText}' activityScene='{activityScenePath}' activeScene='{SceneManager.GetActiveScene().path}' readinessObserverFound='{readiness != null}' readinessActivityMatches='{ReferenceEquals(last.Activity, expectedActivity)}' readinessHasOccurrence='{last.HasOccurrence}' readinessOccurrence='{last.Occurrence}' readinessRevision='{(readiness != null ? readiness.LastRevision : 0)}'.");
        }

        private IEnumerator WaitForActivityOccurrence(PlayerSessionObserver observer, ActivityAsset expectedActivity, string scenePath, string label)
        {
            _observedActivityOccurrence = 0;
            ActivityReadinessEvents readiness = FindExactActivityReadinessEvents(scenePath);
            if (readiness == null)
            {
                SetFailure(label + ": exact public ActivityReadinessEvents surface is missing or duplicated.");
                yield break;
            }
            if (observer == null)
            {
                SetFailure(label + ": exact Activity PlayerSessionObserver is missing or duplicated; this is fixture/surface acquisition, not an occurrence identity result.");
                yield break;
            }

            string lastAccessState = observer.ScopedAccessState.ToString();
            string lastAccessDiagnostic = observer.ScopedAccessDiagnostic;
            string lastObservationDiagnostic = string.Empty;
            string lastObservationState = "not-read";
            int publishedOccurrence = 0;
            bool readinessPublished = false;
            bool sawObservationWithoutOccurrence = false;
            bool sawOwnerMismatch = false;
            bool sawOccurrenceMismatch = false;

            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                ActivityReadinessSnapshot published = readiness.LastSnapshot;
                readinessPublished = IsReadinessPublished(readiness, expectedActivity);
                if (readinessPublished)
                {
                    publishedOccurrence = published.Occurrence;
                    if (observer.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) &&
                        observation != null && observation.IsAvailable)
                    {
                        lastAccessState = observer.ScopedAccessState.ToString();
                        lastAccessDiagnostic = string.Empty;
                        lastObservationState = "available";
                        lastObservationDiagnostic = observation.Diagnostic;
                        if (!observation.HasCurrentActivityOccurrence)
                        {
                            sawObservationWithoutOccurrence = true;
                        }
                        else
                        {
                            RuntimeContentOwner expectedOwner = RuntimeContentOwner.Activity(
                                expectedActivity.ActivityId.StableText,
                                expectedActivity.ActivityName,
                                RuntimeDefinitionToken.FromUnityObject(expectedActivity));
                            if (!observation.ActivityOwner.Equals(expectedOwner)) sawOwnerMismatch = true;
                            else if (observation.ActivityOccurrence != published.Occurrence) sawOccurrenceMismatch = true;
                            else
                            {
                                _observedActivityOccurrence = observation.ActivityOccurrence;
                                yield break;
                            }
                        }
                    }
                    else
                    {
                        lastAccessState = observer.ScopedAccessState.ToString();
                        lastAccessDiagnostic = observer.ScopedAccessDiagnostic;
                    }
                }
                else
                {
                    lastAccessState = observer.ScopedAccessState.ToString();
                    lastAccessDiagnostic = observer.ScopedAccessDiagnostic;
                }
                yield return null;
            }

            string evidence = $"readinessPublished='{readinessPublished}' readinessOccurrence='{publishedOccurrence}' readinessRevision='{readiness.LastRevision}' observerState='{lastAccessState}' observerDiagnostic='{lastAccessDiagnostic}' observationState='{lastObservationState}' observationDiagnostic='{lastObservationDiagnostic}' missingOccurrenceObserved='{sawObservationWithoutOccurrence}' ownerMismatchObserved='{sawOwnerMismatch}' occurrenceMismatchObserved='{sawOccurrenceMismatch}'";
            if (!readinessPublished)
                SetFailure(label + ": Activity publication was not observed on the public ActivityReadinessEvents surface before the frame budget. " + evidence);
            else if (sawOwnerMismatch)
                SetFailure(label + ": public Player Session observation reported an Activity owner different from the exact expected Activity asset. " + evidence);
            else if (sawObservationWithoutOccurrence)
                SetBlocked(label + ": readiness publicly proves an Activity occurrence, but the public Player Session observation omits it; occurrence observability is unavailable. " + evidence);
            else if (sawOccurrenceMismatch)
                SetFailure(label + ": public Player Session occurrence did not converge to the published Activity occurrence. " + evidence);
            else if (lastAccessState != PlayerSessionScopedAccessState.Bound.ToString())
                SetFailure(label + ": public scoped-access acquisition did not complete after Activity publication. " + evidence);
            else
                SetFailure(label + ": public scoped observation remained unavailable after Activity publication. " + evidence);
        }

        private bool IsReadinessPublished(ActivityReadinessEvents readiness, ActivityAsset expectedActivity)
        {
            if (readiness == null || expectedActivity == null) return false;
            ActivityReadinessSnapshot snapshot = readiness.LastSnapshot;
            return ReferenceEquals(snapshot.Activity, expectedActivity) && snapshot.HasOccurrence;
        }

        private void RequestRoute(RouteRequestTrigger trigger, RouteAsset target, string label)
        {
            if (trigger == null || !trigger.HasRouteRuntimeBinding || trigger.IsRequestInFlight || !ReferenceEquals(trigger.TargetRoute, target))
            {
                SetFailure(label + ": public RouteRequestTrigger is missing, unbound, in flight, or targets the wrong Route.");
                return;
            }
            trigger.RequestRoute();
            if (!trigger.IsRequestInFlight && !trigger.LastRequestSucceeded)
                SetFailure(label + ": public Route request was not accepted. " + trigger.LastMessage);
        }

        private void RequestActivity(ActivityRequestTrigger trigger, ActivityAsset target, string label)
        {
            if (trigger == null || !trigger.HasActivityRuntimeBinding || trigger.IsRequestInFlight || !ReferenceEquals(trigger.TargetActivity, target))
            {
                SetFailure(label + ": public ActivityRequestTrigger is missing, unbound, in flight, or targets the wrong Activity.");
                return;
            }
            trigger.RequestActivity();
            if (!trigger.IsRequestInFlight && !trigger.LastRequestSucceeded)
                SetFailure(label + ": public Activity request was not accepted. " + trigger.LastMessage);
        }

        private IEnumerator WaitForRouteRequest(RouteRequestTrigger trigger, string label)
        {
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (trigger != null && !trigger.IsRequestInFlight)
                {
                    if (!trigger.LastRequestSucceeded) SetFailure(label + ": public Route request failed. " + trigger.LastMessage);
                    yield break;
                }
                yield return null;
            }
            SetFailure(label + ": public Route request did not reach its terminal state before the frame budget.");
        }

        private IEnumerator WaitForActivityRequest(ActivityRequestTrigger trigger, string label)
        {
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (trigger != null && !trigger.IsRequestInFlight)
                {
                    if (!trigger.LastRequestSucceeded) SetFailure(label + ": public Activity request failed. " + trigger.LastMessage);
                    yield break;
                }
                yield return null;
            }
            SetFailure(label + ": public Activity request did not reach its terminal state before the frame budget.");
        }

        private bool ValidateAuthoring()
        {
            if (routeA == null || routeB == null || activityA == null || activityB == null ||
                ReferenceEquals(routeA, routeB) || ReferenceEquals(activityA, activityB) ||
                routeToA == null || routeToB == null || activityToA == null || activityToB == null ||
                !ReferenceEquals(routeToA.TargetRoute, routeA) || !ReferenceEquals(routeToB.TargetRoute, routeB) ||
                !ReferenceEquals(activityToA.TargetActivity, activityA) || !ReferenceEquals(activityToB.TargetActivity, activityB))
            {
                SetBlocked("I4.2 Persistent fixture does not have exact, distinct Route/Activity assets and public request trigger targets.");
                return false;
            }
            if (routeA.StartupActivity != activityA || routeB.StartupActivity != activityB ||
                !string.Equals(routeA.PrimaryScenePath, RouteAScenePath, StringComparison.Ordinal) ||
                !string.Equals(routeB.PrimaryScenePath, RouteBScenePath, StringComparison.Ordinal) ||
                !HasActivityScene(activityA, ActivityAScenePath) || !HasActivityScene(activityB, ActivityBScenePath))
            {
                SetBlocked("I4.2 Route/Activity graph does not match its authored occurrence scenes.");
                return false;
            }
            return true;
        }

        private bool IsExactRouteCurrent(RouteAsset expected, string scenePath)
        {
            if (expected == null || !string.Equals(SceneManager.GetActiveScene().path, scenePath, StringComparison.Ordinal)) return false;
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return false;
            RouteContentContribution[] components = FindComponents<RouteContentContribution>(scene);
            return components.Length == 1 && ReferenceEquals(components[0].Route, expected);
        }

        private bool IsExactActivityCurrent(ActivityAsset expected, string scenePath)
        {
            if (expected == null) return false;
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return false;
            ActivityContentContribution[] components = FindComponents<ActivityContentContribution>(scene);
            return components.Length == 1 && ReferenceEquals(components[0].Activity, expected);
        }

        private PlayerSessionObserver FindExactActivityObserver(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return null;
            PlayerSessionObserver[] values = FindComponents<PlayerSessionObserver>(scene);
            return values.Length == 1 ? values[0] : null;
        }

        private ActivityReadinessEvents FindExactActivityReadinessEvents(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return null;
            ActivityReadinessEvents[] values = FindComponents<ActivityReadinessEvents>(scene);
            return values.Length == 1 ? values[0] : null;
        }

        private PlayerSessionJoinTrigger FindExactActivityJoin(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return null;
            PlayerSessionJoinTrigger[] values = FindComponents<PlayerSessionJoinTrigger>(scene);
            return values.Length == 1 && values[0].Scope == LocalPlayerProvisioningConsumerScope.Activity ? values[0] : null;
        }

        private PlayerSessionJoinTrigger FindExactRouteJoin(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return null;
            PlayerSessionJoinTrigger[] values = FindComponents<PlayerSessionJoinTrigger>(scene);
            return values.Length == 1 && values[0].Scope == LocalPlayerProvisioningConsumerScope.Route ? values[0] : null;
        }

        private RouteContentLifecycleEvents FindExactRouteLifecycleEvents(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return null;
            RouteContentLifecycleEvents[] values = FindComponents<RouteContentLifecycleEvents>(scene);
            return values.Length == 1 ? values[0] : null;
        }

        private static bool HasPublicRouteEntered(RouteContentLifecycleEvents observer, RouteAsset expectedRoute, RouteContentContribution expectedContribution)
        {
            if (observer == null || expectedRoute == null || expectedContribution == null ||
                !observer.IsRouteContentActive || !observer.HasRouteContentContext)
                return false;

            RouteContentLifecycleContext context = observer.LastRouteContentContext;
            return context.IsEntered && ReferenceEquals(context.Route, expectedRoute) &&
                   ReferenceEquals(context.Binding, expectedContribution);
        }

        private static bool HasManagedReference(object value) => !ReferenceEquals(value, null);

        private void BeginRouteAReentryObservation(ulong expectedUnloadHandle)
        {
            StopRouteAReentryObservation();
            _routeAExpectedUnloadHandle = expectedUnloadHandle;
            _routeAObservedUnloadHandle = 0UL;
            _routeAObservedReloadHandle = 0UL;
            _routeAUnloadObserved = false;
            _routeAReloadObserved = false;
            _observingRouteAReentry = true;
            SceneManager.sceneUnloaded += OnRouteSceneUnloaded;
            SceneManager.sceneLoaded += OnRouteSceneLoaded;
        }

        private void OnRouteSceneUnloaded(Scene scene)
        {
            if (!_observingRouteAReentry || scene.handle.GetRawData() != _routeAExpectedUnloadHandle) return;
            _routeAUnloadObserved = true;
            _routeAObservedUnloadHandle = scene.handle.GetRawData();
        }

        private void OnRouteSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_observingRouteAReentry || !_routeAUnloadObserved ||
                !scene.IsValid() || !scene.isLoaded || !string.Equals(scene.path, RouteAScenePath, StringComparison.Ordinal)) return;
            _routeAReloadObserved = true;
            _routeAObservedReloadHandle = scene.handle.GetRawData();
        }

        private void StopRouteAReentryObservation()
        {
            if (!_observingRouteAReentry) return;
            SceneManager.sceneUnloaded -= OnRouteSceneUnloaded;
            SceneManager.sceneLoaded -= OnRouteSceneLoaded;
            _observingRouteAReentry = false;
        }

        private IEnumerator WaitForRouteAReentryObservation()
        {
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (_routeAUnloadObserved && _routeAReloadObserved) yield break;
                yield return null;
            }
            SetBlocked($"Route A re-entry lifecycle events were not both observed through Unity's public SceneManager events; expectedUnloadHandle='{_routeAExpectedUnloadHandle}' unloadObserved='{_routeAUnloadObserved}' unloadedHandle='{_routeAObservedUnloadHandle}' reloadObserved='{_routeAReloadObserved}' reloadedHandle='{_routeAObservedReloadHandle}'.");
        }

        private IEnumerator WaitForPublicRouteEntry(RouteAsset expectedRoute, string scenePath, string label)
        {
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                Scene scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid() && scene.isLoaded)
                {
                    RouteContentLifecycleEvents[] observers = FindComponents<RouteContentLifecycleEvents>(scene);
                    RouteContentContribution[] contributions = FindComponents<RouteContentContribution>(scene);
                    if (observers.Length != 1 || contributions.Length != 1)
                    {
                        SetBlocked($"{label}: public Route lifecycle fixture cardinality is invalid; observers='{observers.Length}' contributions='{contributions.Length}'.");
                        yield break;
                    }
                    if (HasPublicRouteEntered(observers[0], expectedRoute, contributions[0])) yield break;
                }
                yield return null;
            }

            RouteContentLifecycleEvents observer = FindExactRouteLifecycleEvents(scenePath);
            RouteContentContribution contribution = FindExactRouteContribution(scenePath);
            RouteContentLifecycleContext context = observer != null ? observer.LastRouteContentContext : default;
            SetBlocked($"{label}: public Route entry was not observed before the frame budget; observerFound='{observer != null}' contextAvailable='{observer != null && observer.HasRouteContentContext}' active='{observer != null && observer.IsRouteContentActive}' phase='{context.Phase}' exactRoute='{ReferenceEquals(context.Route, expectedRoute)}' exactContribution='{ReferenceEquals(context.Binding, contribution)}'.");
        }

        private ActivityContentContribution FindExactActivityContribution(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return null;
            ActivityContentContribution[] values = FindComponents<ActivityContentContribution>(scene);
            return values.Length == 1 ? values[0] : null;
        }

        private RouteContentContribution FindExactRouteContribution(string scenePath)
        {
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded) return null;
            RouteContentContribution[] values = FindComponents<RouteContentContribution>(scene);
            return values.Length == 1 ? values[0] : null;
        }

        private ulong GetSceneHandle(string path)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            return scene.IsValid() && scene.isLoaded ? scene.handle.GetRawData() : 0UL;
        }

        private static bool HasActivityScene(ActivityAsset activity, string path)
        {
            return activity != null && activity.ActivityContentProfile != null && activity.ActivityContentProfile.SceneCount == 1 &&
                   string.Equals(activity.ActivityContentProfile.Scenes[0].ScenePath, path, StringComparison.Ordinal);
        }

        private static bool IsEmptyClosed(PlayerParticipationSnapshot snapshot) => snapshot != null && snapshot.IsInitialized &&
            snapshot.Slots.Count == 1 && snapshot.AvailableCount == 1 && snapshot.JoinedCount == 0 && snapshot.ReservedCount == 0 &&
            snapshot.LeavingCount == 0 && !snapshot.JoiningOpen;

        private static bool IsEmptyOpen(PlayerParticipationSnapshot snapshot) => snapshot != null && snapshot.IsInitialized &&
            snapshot.Slots.Count == 1 && snapshot.AvailableCount == 1 && snapshot.JoinedCount == 0 && snapshot.ReservedCount == 0 &&
            snapshot.LeavingCount == 0 && snapshot.JoiningOpen;

        private bool IsSessionRestored(PlayerSessionObserver observer)
        {
            return observer != null && observer.TryGetSnapshot(out PlayerParticipationSnapshot snapshot) && IsEmptyClosed(snapshot) &&
                   PlayerInput.all.Count == _initialPlayerInputCount &&
                   (_ownedKeyboard == null || !_ownedKeyboard.added) &&
                   observer.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) &&
                   observation.Slots.Count == 1 && !observation.Slots[0].HasHostEvidence;
        }

        private IEnumerator CleanupAndPublish()
        {
            StopRouteAReentryObservation();
            yield return CleanupActiveJoin();

            if (!IsExactRouteCurrent(routeA, RouteAScenePath) && routeToA != null && !routeToA.IsRequestInFlight && routeToA.HasRouteRuntimeBinding)
            {
                routeToA.RequestRoute();
                for (int frame = 0; frame < Mathf.Max(1, frameBudget) &&
                     (routeToA.IsRequestInFlight || !IsExactRouteCurrent(routeA, RouteAScenePath)); frame++) yield return null;
            }
            if (IsExactRouteCurrent(routeA, RouteAScenePath) && !IsExactActivityCurrent(activityA, ActivityAScenePath) &&
                activityToA != null && !activityToA.IsRequestInFlight && activityToA.HasActivityRuntimeBinding)
            {
                activityToA.RequestActivity();
                for (int frame = 0; frame < Mathf.Max(1, frameBudget) &&
                     (activityToA.IsRequestInFlight || !IsExactActivityCurrent(activityA, ActivityAScenePath)); frame++) yield return null;
            }

            PlayerSessionObserver finalObserver = FindExactActivityObserver(ActivityAScenePath);
            bool restored = false;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (IsExactRouteCurrent(routeA, RouteAScenePath) && IsExactActivityCurrent(activityA, ActivityAScenePath) &&
                    IsSessionRestored(finalObserver) && InputSystem.devices.Count == _initialDeviceCount)
                {
                    restored = true;
                    break;
                }
                yield return null;
            }
            if (!restored && string.IsNullOrEmpty(_cleanupIssue))
                _cleanupIssue = "Final Route/Activity, Session, Host, PlayerInput, or InputDevice state did not match the declared baseline.";

            PublishTerminals(restored);
        }

        private IEnumerator CleanupActiveJoin()
        {
            if (_activeObserver != null && _activeObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot))
            {
                for (int index = 0; index < snapshot.Slots.Count; index++)
                {
                    PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                    if (!slot.IsJoined) continue;
                    SessionPlayerLeaveResult leave = QaPlayerSessionCommandAccess.Leave(
                        _activeObserver,
                        slot.Profile);
                    if (leave == null || !leave.Succeeded || !leave.ProvisioningReleased || !leave.TerminalCommitted)
                        _cleanupIssue = Append(_cleanupIssue, "Terminal Leave did not confirm Host release.");
                }

                bool empty = false;
                for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
                {
                    if (_activeObserver.TryGetSnapshot(out PlayerParticipationSnapshot current) &&
                        current.JoinedCount == 0 && current.ReservedCount == 0 && current.LeavingCount == 0)
                    {
                        empty = true;
                        break;
                    }
                    yield return null;
                }
                if (!empty) _cleanupIssue = Append(_cleanupIssue, "Terminal cleanup did not observe an empty Session after Leave.");
                else if (_joiningOpened)
                {
                    PlayerParticipationOperationResult close =
                        QaPlayerSessionCommandAccess.CloseJoining(_activeObserver);
                    if (close == null || !close.Succeeded) _cleanupIssue = Append(_cleanupIssue, "Terminal CloseJoining failed.");
                    else _joiningOpened = false;
                }
            }
            RemoveOwnedKeyboard();
        }

        private void CleanupActiveJoinSynchronously()
        {
            if (_activeObserver != null && _activeObserver.TryGetSnapshot(out PlayerParticipationSnapshot snapshot))
            {
                for (int index = 0; index < snapshot.Slots.Count; index++)
                {
                    PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                    if (!slot.IsJoined) continue;
                    SessionPlayerLeaveResult leave = QaPlayerSessionCommandAccess.Leave(
                        _activeObserver,
                        slot.Profile);
                    if (leave == null || !leave.Succeeded || !leave.ProvisioningReleased || !leave.TerminalCommitted)
                        _cleanupIssue = Append(_cleanupIssue, "Interruption Leave did not confirm Host release.");
                }
                if (snapshot.JoiningOpen)
                {
                    PlayerParticipationOperationResult close =
                        QaPlayerSessionCommandAccess.CloseJoining(_activeObserver);
                    if (close == null || !close.Succeeded) _cleanupIssue = Append(_cleanupIssue, "Interruption CloseJoining failed.");
                }
            }
            RemoveOwnedKeyboard();
        }

        private void RemoveOwnedKeyboard()
        {
            if (_ownedKeyboard == null || !_ownedKeyboard.added) return;
            try { InputSystem.RemoveDevice(_ownedKeyboard); }
            catch (Exception exception) { _cleanupIssue = Append(_cleanupIssue, "Keyboard removal failed: " + exception.Message); }
        }

        private void PublishTerminals(bool restored)
        {
            bool observablePass = !_failed && restored && string.IsNullOrEmpty(_cleanupIssue) && _completedCases == 8;
            var observableRecorder = new QaCertificationRecorder();
            if (observablePass) observableRecorder.RecordPass();
            else observableRecorder.RecordFirstCausalDivergence(
                _firstIssue.StartsWith("BLOCKED:", StringComparison.Ordinal) ? QaCertificationVerdict.Blocked : QaCertificationVerdict.Fail,
                string.IsNullOrEmpty(_firstIssue) ? "Observable lifecycle/current-binding cases or cleanup were incomplete." : _firstIssue);
            QaCertificationResult observable = observableRecorder.CreateResult(
                ScenarioId + "-OBSERVABLE", restored ? QaCleanupDisposition.BaselineRestored : QaCleanupDisposition.FreshBootRequired, _cleanupIssue);

            Debug.Log($"[QA-IF-ADR-046-I4.2-OBSERVABLE] scenario='{observable.ScenarioId}' status='{VerdictText(observable.Verdict)}' verdict='{VerdictCode(observable.Verdict)}' cases='{_completedCases}/8' caseResults='{_caseEvidence}' activityOccurrences='{_activityOccurrenceEvidence}' routeEntry='{_routeEntryEvidence}' currentJoin='{_joinEvidence}' cleanup='{observable.CleanupDisposition}' cleanupIssue='{observable.CleanupIssue}' firstCause='{observable.FirstCausalDivergence}'");
            _terminal = true;
        }

        private bool HasFailure => _failed || !string.IsNullOrEmpty(_firstIssue);

        private static string VerdictText(QaCertificationVerdict verdict)
        {
            switch (verdict)
            {
                case QaCertificationVerdict.Pass: return "Passed";
                case QaCertificationVerdict.Fail: return "Failed";
                default: return "Blocked";
            }
        }

        private static string VerdictCode(QaCertificationVerdict verdict) => verdict.ToString().ToUpperInvariant();

        private void SetFailure(string issue)
        {
            _failed = true;
            if (string.IsNullOrEmpty(_firstIssue)) _firstIssue = issue;
        }

        private void SetBlocked(string issue)
        {
            _failed = true;
            if (string.IsNullOrEmpty(_firstIssue)) _firstIssue = "BLOCKED: " + issue;
        }

        private static string Append(string existing, string issue) =>
            string.IsNullOrEmpty(existing) ? issue : existing + " | " + issue;

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            var values = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects()) values.AddRange(root.GetComponentsInChildren<T>(true));
            return values.ToArray();
        }
    }
}

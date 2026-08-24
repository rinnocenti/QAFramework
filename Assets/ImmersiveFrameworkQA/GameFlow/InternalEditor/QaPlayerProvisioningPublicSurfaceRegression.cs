using System;
using ImmersiveFrameworkQA.Player;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// QA-PLAYER-SURFACE-01 — Public-only positive Manager-Provisioned Player
    /// lifecycle contract proof.
    ///
    /// Requires the authored Hub fixture
    /// <c>QA_PlayerSurface_PublicNavigation</c> (composition-bound
    /// ActivityRequestTrigger + Route consumer binding). Player commands use P1/P2
    /// public surfaces; prepare/materialize/admit remain runtime-owned.
    /// </summary>
    public static class QaPlayerProvisioningPublicSurfaceRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Public Surface/" +
            "Run Positive Contract";
        private const string Prefix = "[QA_PLAYER_SURFACE_01]";
        private const string Source = nameof(QaPlayerProvisioningPublicSurfaceRegression);
        private const int FrameBudget = 360;
        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "runtime-started",
            "public-navigation-fixture-resolved",
            "public-activity-trigger-composition-bound",
            "consumer-binding-authored",
            "scoped-access-available",
            "fresh-session-confirmed",
            "waitcovered-activity-configured",
            "activity-entry-started",
            "waiting-for-join-observed",
            "waitcovered-loading-pending",
            "joining-opened",
            "public-join-succeeded",
            "joined-slot-host-observed",
            "default-actor-selection-requested",
            "selected-actor-observed",
            "normal-lifecycle-ready",
            "prepared-materialized-admitted",
            "physical-identity-captured",
            "waitcovered-loading-terminal",
            "activity-entry-completed",
            "manager-a1-contextual-evidence-captured",
            "manager-a-to-b-fresh-context",
            "manager-b-preserves-physical-identity",
            "manager-b-to-a2-fresh-context",
            "manager-a2-preserves-physical-identity",
            "activity-exit-released",
            "session-host-persists",
            "activity-exit-preserves-physical-actor",
            "reentry-newer-occurrence",
            "reentry-no-duplicate-slot-actor",
            "reentry-preserves-physical-identity",
            "player-excluded-context-absent",
            "player-excluded-physical-preserved",
            "player-reentry-after-exclusion-fresh",
            "joining-closed",
            "fixture-cleaned",
            "public-scan-clean"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            await RunAsync();
        }

        /// <summary>
        /// Entry point for the joint certification orchestrator.
        /// </summary>
        public static Task RunCertificationAsync() => RunAsync();

        /// <summary>
        /// Reuses the canonical public Manager lifecycle through contextual
        /// release, then proves real Session termination. This is distinct
        /// from Player Leave: the Framework Session host is the owner of the
        /// final physical teardown.
        /// </summary>
        public static Task RunManagerSessionTerminationAsync() =>
            RunAsync(terminateSession: true);

        [MenuItem("Immersive Framework/QA/Player/Manager Provisioned/Run Join Without Activity", true)]
        private static bool ValidateRunNoActivityJoin() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Player/Manager Provisioned/Run Join Without Activity")]
        private static async void RunNoActivityJoinFromMenu()
        {
            await RunNoActivityJoinAsync();
        }

        /// <summary>
        /// Public Route-scoped proof that a Manager Join creates only Session
        /// membership and the technical control plane. Activity contextual
        /// assignment is absent until an Activity occurrence is entered.
        /// </summary>
        public static async Task RunNoActivityJoinAsync()
        {
            const int noActivityFrameBudget = 240;
            const string noActivitySource = "QaManagerProvidedNoActivityJoin";
            string[] expectedCases =
            {
                "play-mode-required",
                "route-access-ready",
                "fresh-session-without-activity",
                "joining-opened",
                "joined-session-physical-control-plane",
                "contextual-assignment-absent",
                "physical-actor-not-materialized",
                "cleanup-terminal-leave"
            };
            var completed = new List<string>();
            ILocalPlayerProvisioningConsumerAccess access = null;
            bool joiningOpened = false;

            try
            {
                Require(EditorApplication.isPlaying,
                    "Manager Join Without Activity requires Play Mode.");
                completed.Add(expectedCases[0]);

                Require(
                    QaPlayerSurfacePublicNavigationSupport.TryResolveAuthoredFixture(
                        out QaPlayerSurfacePublicNavigationFixture fixture,
                        out string fixtureIssue),
                    fixtureIssue);
                Require(fixture.RouteConsumerBinding != null &&
                        fixture.RouteConsumerBinding.Scope ==
                        LocalPlayerProvisioningConsumerScope.Route,
                    "Manager Join Without Activity requires the authored Route consumer binding.");
                access = await AwaitScopedAccessAsync(
                    fixture.RouteConsumerBinding,
                    noActivityFrameBudget);
                completed.Add(expectedCases[1]);

                LocalPlayerProvisioningConsumerObservationSnapshot initial =
                    RequireObservation(access, "join-without-activity-initial");
                if (initial.HasCurrentActivityOccurrence)
                {
                    ActivityRequestTrigger clearTrigger =
                        fixture.ClearActivityTrigger;
                    Require(clearTrigger != null,
                        "Manager Join Without Activity requires the authored Clear Activity trigger.");
                    await QaPlayerSurfacePublicNavigationSupport
                        .RequireCompositionBoundAsync(
                            clearTrigger,
                            noActivityFrameBudget);
                    QaPlayerSurfacePublicNavigationSupport.ClearActivityPublic(
                        clearTrigger);
                    await QaPlayerSurfacePublicNavigationSupport
                        .AwaitTriggerTerminalSuccessAsync(
                            clearTrigger,
                            noActivityFrameBudget,
                            "Manager Join Without Activity could not clear the startup Activity.");
                    initial = await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.Participation != null &&
                            observation.Participation.IsInitialized &&
                            observation.Participation.JoinedCount == 0 &&
                            !observation.HasCurrentActivityOccurrence,
                        "Manager Join Without Activity did not reach a fresh Session with no current Activity",
                        noActivityFrameBudget);
                }

                Require(initial.Participation != null &&
                        initial.Participation.IsInitialized &&
                        initial.Participation.JoinedCount == 0 &&
                        !initial.HasCurrentActivityOccurrence,
                    "The regression requires a fresh Session with no current Activity. " +
                    DescribeObservation(initial));
                completed.Add(expectedCases[2]);

                PlayerParticipationOperationResult open = access.OpenJoining(
                    noActivitySource,
                    "manager-join-without-activity-open");
                Require(open != null && open.Succeeded &&
                        open.Snapshot != null && open.Snapshot.JoiningOpen,
                    open != null ? open.ToDiagnosticString() :
                        "OpenJoining returned no result.");
                joiningOpened = true;
                completed.Add(expectedCases[3]);

                LocalPlayerJoinResult join = access.RequestJoin(
                    new LocalPlayerJoinRequest(
                        noActivitySource,
                        "manager-join-without-activity-join"));
                Require(join != null && join.Succeeded && join.Slot.IsJoined &&
                        join.HasLocalPlayerHostEvidence && join.PlayerInput != null &&
                        join.LocalPlayerHost != null &&
                        join.LocalPlayerHost.ActorMount != null,
                    join != null ? join.ToDiagnosticString() :
                        "RequestJoin returned no result.");
                completed.Add(expectedCases[4]);

                LocalPlayerProvisioningConsumerObservationSnapshot joined =
                    await AwaitObservationAsync(
                        access,
                        observation => observation.Participation != null &&
                            observation.Participation.JoinedCount == 1 &&
                            !observation.HasCurrentActivityOccurrence,
                        "Joined Session evidence was not available without an Activity",
                        noActivityFrameBudget);
                PlayerSlotRuntimeSnapshot joinedSlot = FindSlot(
                    joined.Participation,
                    join.Slot.PlayerSlotId);
                Require(joinedSlot.IsJoined && !join.HasAssignmentEvidence &&
                        !HasContextualAssignment(joined, joinedSlot.PlayerSlotId),
                    "Manager Join fabricated a current contextual assignment before Activity entry. " +
                    DescribeObservation(joined));
                completed.Add(expectedCases[5]);

                Require(join.LocalPlayerHost.ActorMount.childCount == 0 &&
                        !join.LocalPlayerHost.HasLogicalActor,
                    "Manager Join materialized a physical Actor before an Activity required it.");
                completed.Add(expectedCases[6]);

                SessionPlayerLeaveResult leave = access.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        joinedSlot.PlayerSlotId,
                        joinedSlot.Revision,
                        noActivitySource,
                        "manager-join-without-activity-cleanup"));
                Require(leave != null && leave.Succeeded,
                    leave != null ? leave.ToDiagnosticString() :
                        "Cleanup Leave returned no result.");
                completed.Add(expectedCases[7]);

                Debug.Log(
                    "[QA_MANAGER_JOIN_WITHOUT_ACTIVITY] status='Passed' " +
                    "verdict='SessionJoinWithoutContextualAssignment' " +
                    $"cases='{completed.Count}/{expectedCases.Length}' " +
                    $"slot='{joinedSlot.PlayerSlotId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                string next = completed.Count < expectedCases.Length
                    ? expectedCases[completed.Count]
                    : string.Empty;
                Debug.LogError(
                    "[QA_MANAGER_JOIN_WITHOUT_ACTIVITY] status='Failed' " +
                    $"cases='{completed.Count}/{expectedCases.Length}' next='{next}' " +
                    $"completed='{string.Join(",", completed)}' " +
                    $"missing='{Escape(exception.Message)}'.");
                throw;
            }
            finally
            {
                if (joiningOpened && access != null)
                {
                    access.CloseJoining(
                        noActivitySource,
                        "manager-join-without-activity-finally");
                }
            }
        }

        private static async Task RunAsync(bool terminateSession = false)
        {
            var expectedCases = new List<string>(ExpectedCases);
            if (terminateSession)
            {
                int scanIndex = expectedCases.IndexOf("public-scan-clean");
                string[] terminationCases =
                {
                    "session-termination-preconditions",
                    "session-termination-physical-resources-released",
                    "session-termination-public-evidence-cleared"
                };
                if (scanIndex >= 0)
                {
                    expectedCases.InsertRange(scanIndex, terminationCases);
                }
                else
                {
                    expectedCases.AddRange(terminationCases);
                }
            }

            var cases = new QaCaseRegistry(
                expectedCases.ToArray(),
                expectedCases.Count);
            var failures = new QaFailureCollector();
            FrameworkRuntimeHost host = null;
            QaPlayerSurfacePublicNavigationFixture publicNav = null;
            QaPlayerSurfaceGlobalUiFixture globalUiFixture = null;
            LocalPlayerProvisioningConsumerAccessBinding consumerBinding = null;
            ILocalPlayerProvisioningConsumerAccess access = null;
            LocalPlayerActorSelectionRequestAuthoring actorSelection = null;
            LocalPlayerJoinResult joinResult = null;
            LocalPlayerHostAuthoring joinedHost = null;
            QaLoadingSurfaceVisibilityHoldAdapter loading = null;
            ActivityRequestTrigger enterTrigger = null;
            ActivityRequestTrigger enterSecondaryTrigger = null;
            ActivityRequestTrigger enterPlayerExcludedTrigger = null;
            ActivityRequestTrigger clearTrigger = null;
            ActivityAsset activity = null;
            ActivityAsset secondaryActivity = null;
            ActivityAsset playerExcludedActivity = null;
            bool joiningOpen = false;
            int firstOccurrence = 0;
            int sessionRevisionAfterJoin = 0;
            PlayerSlotId joinedSlotId = default;
            Transform physicalActor = null;
            string physicalActorEntityId = string.Empty;
            Vector3 physicalActorPosition = default;
            Quaternion physicalActorRotation = default;
            LocalPlayerProvisioningConsumerSlotObservation activityA1Slot = default;
            LocalPlayerProvisioningConsumerSlotObservation activityBSlot = default;

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "QA-PLAYER-SURFACE-01 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaPlayerSurfacePublicNavigationSetup.RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                await QaH2FrameworkReadiness.RequireStartedRouteAsync(
                    host,
                    FrameBudget);
                cases.Complete("runtime-started");

                Require(
                    QaPlayerSurfacePublicNavigationSupport.TryResolveAuthoredFixture(
                        out publicNav,
                        out string publicNavDiagnostic),
                    publicNavDiagnostic);
                cases.Complete("public-navigation-fixture-resolved");

                Require(
                    QaPlayerSurfacePublicNavigationSupport
                        .TryResolveGlobalUiFixture(
                            out globalUiFixture,
                            out string globalUiFixtureDiagnostic),
                    globalUiFixtureDiagnostic);
                loading = globalUiFixture.LoadingSurface;
                Require(
                    loading != null,
                    "Player Surface UIGlobal fixture has no authored Loading Surface adapter.");
                enterTrigger = publicNav.EnterActivityTrigger;
                enterSecondaryTrigger = publicNav.EnterSecondaryActivityTrigger;
                enterPlayerExcludedTrigger = publicNav.EnterPlayerExcludedActivityTrigger;
                clearTrigger = publicNav.ClearActivityTrigger;
                activity = publicNav.TargetActivity;
                secondaryActivity = publicNav.SecondaryPlayerActivity;
                playerExcludedActivity = publicNav.PlayerExcludedActivity;
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireCompositionBoundAsync(enterTrigger, FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireCompositionBoundAsync(clearTrigger, FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireCompositionBoundAsync(enterSecondaryTrigger, FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .RequireCompositionBoundAsync(enterPlayerExcludedTrigger, FrameBudget);
                cases.Complete("public-activity-trigger-composition-bound");

                await QaPlayerSurfacePublicNavigationSupport
                    .RequireProvisioningRuntimeReadyAsync(
                        globalUiFixture,
                        FrameBudget);

                PlayerSlotProfile slotProfile =
                    publicNav.PrimaryPlayerSlot ?? ResolveFirstLocalPlayerSlot();
                Require(
                    slotProfile != null &&
                    slotProfile.PlayerSlotId.IsValid &&
                    slotProfile.DefaultActorProfile != null &&
                    slotProfile.DefaultActorProfile.LogicalActorHostPrefab != null,
                    "QA-PLAYER-SURFACE-01 requires a configured first Local Player Slot with default Actor.");

                consumerBinding = publicNav.RouteConsumerBinding;
                Require(
                    consumerBinding != null &&
                    consumerBinding.Scope ==
                        LocalPlayerProvisioningConsumerScope.Route,
                    "Prepared public navigation fixture has no authored Route consumer binding.");
                cases.Complete("consumer-binding-authored");

                access = await AwaitScopedAccessAsync(
                    consumerBinding,
                    FrameBudget);
                Require(
                    access.Snapshot.IsAvailable && !access.Snapshot.IsDisposed,
                    "Scoped consumer access is not available. " + access.Snapshot.Diagnostic);
                cases.Complete("scoped-access-available");

                LocalPlayerProvisioningConsumerObservationSnapshot initialObservation =
                    RequireObservation(access, "initial");
                Require(
                    initialObservation.IsAvailable &&
                    initialObservation.Participation != null &&
                    initialObservation.Participation.IsInitialized &&
                    initialObservation.Participation.JoinedCount == 0,
                    "QA-PLAYER-SURFACE-01 is one-shot. Enter a fresh Play Mode with no joined Players. " +
                    DescribeObservation(initialObservation));
                cases.Complete("fresh-session-confirmed");

                Require(
                    activity != null &&
                    secondaryActivity != null &&
                    playerExcludedActivity != null &&
                    !ReferenceEquals(activity, secondaryActivity) &&
                    !ReferenceEquals(activity, playerExcludedActivity) &&
                    activity.EntryReadinessPolicy ==
                        ActivityEntryReadinessPolicy.WaitCovered &&
                    activity.VisualTransitionMode ==
                        ActivityVisualTransitionMode.FadeWithLoading &&
                    activity.TransitionGateMode ==
                        TransitionGateMode.InputInteractionAndGameplay &&
                    activity.PlayerParticipationRequirementLevel ==
                        PlayerParticipationRequirementLevel.GameplayReady &&
                    activity.HasActivityContentProfile,
                    "Authored public WaitCovered Activity does not preserve the canonical " +
                    "FadeWithLoading presentation and gate configuration.");
                cases.Complete("waitcovered-activity-configured");

                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(
                    enterTrigger);
                await QaPlayerSurfacePublicNavigationSupport.AwaitTriggerInFlightAsync(
                    enterTrigger,
                    FrameBudget,
                    "Public WaitCovered entry did not stay in-flight.");
                cases.Complete("activity-entry-started");

                LocalPlayerProvisioningConsumerObservationSnapshot waiting =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.IsAvailable &&
                            observation.HasCurrentActivityOccurrence &&
                            string.Equals(
                                observation.Lifecycle.ActivityName,
                                activity.ActivityName,
                                StringComparison.Ordinal) &&
                            observation.Lifecycle.Status ==
                                ManagerProvisionedPlayerLifecycleStatus
                                    .WaitingForJoin &&
                            observation.Lifecycle.GateHeld &&
                            observation.Lifecycle.HasGateEvidence &&
                            observation.Lifecycle.GateEvidenceScope ==
                                ManagerProvisionedPlayerGateEvidenceScope
                                    .ActivityPlayerReadinessContribution &&
                            observation.Participation.JoinedCount == 0,
                        "WaitingForJoin was not exposed through public consumer observation",
                        FrameBudget);
                firstOccurrence = waiting.ActivityOccurrence;
                Require(
                    enterTrigger.IsRequestInFlight &&
                    !enterTrigger.LastRequestSucceeded,
                    "WaitCovered public Activity request completed before any public Join. " +
                    DescribeObservation(waiting));
                cases.Complete("waiting-for-join-observed");

                LocalPlayerProvisioningConsumerObservationSnapshot pendingHold =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.IsAvailable &&
                            observation.ActivityOccurrence == firstOccurrence &&
                            observation.Lifecycle.Status ==
                                ManagerProvisionedPlayerLifecycleStatus
                                    .WaitingForJoin &&
                            observation.Lifecycle.GateHeld &&
                            observation.Participation.JoinedCount == 0,
                        "Player WaitCovered hold was not retained while no Player had joined",
                        FrameBudget);
                Require(
                    enterTrigger.IsRequestInFlight &&
                    pendingHold.Lifecycle.GateHeld &&
                    !pendingHold.Lifecycle.IsReady &&
                    loading.IsVisible &&
                    loading.CurrentAlpha >= 0.999f &&
                    !loading.HideHoldActive,
                    "WaitCovered did not keep loading/gate pending while no Player had joined. " +
                    DescribeObservation(pendingHold) +
                    DescribeLoading(loading));
                cases.Complete("waitcovered-loading-pending");

                PlayerParticipationOperationResult openResult =
                    access.OpenJoining(Source, "qa-player-surface-01-open-joining");
                Require(
                    openResult != null &&
                    openResult.Completed &&
                    openResult.Snapshot != null &&
                    openResult.Snapshot.JoiningOpen,
                    openResult != null
                        ? openResult.ToDiagnosticString()
                        : "OpenJoining returned no public result.");
                joiningOpen = true;
                cases.Complete("joining-opened");

                joinResult = access.RequestJoin(
                    new LocalPlayerJoinRequest(
                        Source,
                        "qa-player-surface-01-public-join"));
                Require(
                    joinResult != null &&
                    joinResult.Succeeded &&
                    joinResult.HasLocalPlayerHostEvidence &&
                    joinResult.HasCommitEvidence &&
                    joinResult.Slot.IsJoined &&
                    joinResult.Slot.PlayerSlotId == slotProfile.PlayerSlotId,
                    joinResult != null
                        ? joinResult.ToDiagnosticString()
                        : "Public RequestJoin returned no result.");
                joinedHost = joinResult.LocalPlayerHost;
                joinedSlotId = joinResult.Slot.PlayerSlotId;
                sessionRevisionAfterJoin =
                    joinResult.CommitResult != null
                        ? joinResult.CommitResult.CurrentRevision
                        : joinResult.Slot.IsJoined
                            ? RequireObservation(access, "post-join").SessionRevision
                            : 0;
                cases.Complete("public-join-succeeded");

                LocalPlayerProvisioningConsumerObservationSnapshot joinedObservation =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.IsAvailable &&
                            observation.Participation.JoinedCount == 1 &&
                            observation.Lifecycle.HostCount >= 1 &&
                            HasJoinedSlot(observation, joinedSlotId) &&
                            HasHostEvidence(observation, joinedSlotId),
                        "Joined Slot/Host evidence was not publicly observable after Join",
                        FrameBudget);
                cases.Complete("joined-slot-host-observed");

                actorSelection = await QaPlayerSurfacePublicNavigationSupport
                    .RequireActorSelectionRuntimeReadyAsync(
                        globalUiFixture,
                        FrameBudget);

                // Re-read immediately before the public selection request so a
                // concurrent normal-runtime default selection does not produce a
                // false stale-revision rejection in this positive path.
                LocalPlayerProvisioningConsumerObservationSnapshot preSelection =
                    RequireObservation(access, "pre-selection");
                int selectionRevisionBefore =
                    FindSlot(preSelection.Participation, joinedSlotId)
                        .SelectionRevision;
                PlayerActorSelectionResult selection =
                    actorSelection.RequestDefaultActorSelection(
                        joinedSlotId,
                        selectionRevisionBefore,
                        Source,
                        "qa-player-surface-01-default-actor-selection");
                Require(
                    selection != null &&
                    selection.Succeeded &&
                    selection.Slot.IsJoined &&
                    selection.Slot.HasSelectedActor &&
                    ReferenceEquals(
                        selection.SelectedActorProfile,
                        slotProfile.DefaultActorProfile),
                    selection != null
                        ? selection.ToDiagnosticString()
                        : "Public RequestDefaultActorSelection returned no result.");
                cases.Complete("default-actor-selection-requested");

                LocalPlayerProvisioningConsumerObservationSnapshot selectedObservation =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.IsAvailable &&
                            HasSelectedActor(
                                observation,
                                joinedSlotId,
                                slotProfile.DefaultActorProfile),
                        "Selected Actor was not publicly observable after default selection",
                        FrameBudget);
                cases.Complete("selected-actor-observed");

                // After public Join + default Actor selection, normal runtime must
                // prepare/materialize/admit without QA calling privileged lifecycle APIs.
                LocalPlayerProvisioningConsumerObservationSnapshot readyObservation =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.IsAvailable &&
                            observation.Lifecycle.IsReady &&
                            observation.Lifecycle.ActivityOccurrence ==
                                firstOccurrence &&
                            observation.Lifecycle.HasGateEvidence &&
                            !observation.Lifecycle.GateHeld &&
                            observation.AppliedSessionRevision ==
                                observation.SessionRevision &&
                            SlotIsFullyReady(observation, joinedSlotId),
                        "Normal runtime did not reach Player Ready after public Join + Actor selection. " +
                        "Package gap: public intent did not advance preparation/materialization/admission.",
                        FrameBudget);
                cases.Complete("normal-lifecycle-ready");

                Require(
                    SlotIsFullyReady(readyObservation, joinedSlotId) &&
                    CountActors(joinedHost) == 1,
                    "Public observation did not show prepared/materialized/admitted Slot with exactly one Actor. " +
                    DescribeObservation(readyObservation));
                cases.Complete("prepared-materialized-admitted");

                Require(joinedHost != null && joinedHost.ActorMount != null &&
                        joinedHost.ActorMount.childCount == 1,
                    "Prepared Manager-Provisioned Player has no unique physical Actor under its explicit Actor Mount.");
                physicalActor = joinedHost.ActorMount.GetChild(0);
                physicalActorEntityId = physicalActor.gameObject.GetEntityId().ToString();
                physicalActorPosition = physicalActor.position;
                physicalActorRotation = physicalActor.rotation;
                cases.Complete("physical-identity-captured");

                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        enterTrigger,
                        FrameBudget,
                        "WaitCovered public Activity entry did not succeed after Player Ready.");
                Require(
                    readyObservation.Lifecycle.IsReady &&
                    !readyObservation.Lifecycle.GateHeld &&
                    !loading.IsVisible &&
                    loading.CurrentAlpha <= 0.001f &&
                    !loading.HideHoldActive,
                    "Loading/gate did not reach a terminal released state after Player Ready. " +
                    DescribeObservation(readyObservation) +
                    DescribeLoading(loading));
                cases.Complete("waitcovered-loading-terminal");
                cases.Complete("activity-entry-completed");

                activityA1Slot = FindObservedSlot(readyObservation, joinedSlotId);
                Require(
                    activityA1Slot.HasHostEvidence &&
                    activityA1Slot.HostEvidence.IsRecorded &&
                    activityA1Slot.HasGameplayAdmissionEvidence &&
                    activityA1Slot.GameplayAdmission.IsAdmitted &&
                    activityA1Slot.GameplayAdmission.InputBindingToken.IsValid,
                    "Manager Activity A1 has no public contextual Host/gameplay evidence.");
                cases.Complete("manager-a1-contextual-evidence-captured");

                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(
                    enterSecondaryTrigger);
                LocalPlayerProvisioningConsumerObservationSnapshot activityB =
                    await AwaitObservationAsync(
                        access,
                        observation => observation.IsAvailable &&
                            observation.Lifecycle.IsReady &&
                            observation.ActivityOccurrence > firstOccurrence &&
                            string.Equals(observation.Lifecycle.ActivityName,
                                secondaryActivity.ActivityName,
                                StringComparison.Ordinal) &&
                            observation.Participation.JoinedCount == 1 &&
                            SlotIsFullyReady(observation, joinedSlotId),
                        "Manager Activity B did not acquire a fresh ready contextual occurrence.",
                        FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        enterSecondaryTrigger,
                        FrameBudget,
                        "Manager Activity B request did not succeed.");
                activityBSlot = FindObservedSlot(activityB, joinedSlotId);
                Require(
                    activityBSlot.HostEvidence.AssignmentToken !=
                        activityA1Slot.HostEvidence.AssignmentToken &&
                    activityBSlot.GameplayAdmission.InputBindingToken !=
                        activityA1Slot.GameplayAdmission.InputBindingToken &&
                    FindSlot(activityB.Participation, joinedSlotId).SelectionRevision ==
                        selectionRevisionBefore,
                    "Manager A1 -> B reused contextual assignment/input or reselected the Actor.");
                cases.Complete("manager-a-to-b-fresh-context");
                AssertPhysicalRepresentation(
                    joinedHost, physicalActor, physicalActorEntityId,
                    physicalActorPosition, physicalActorRotation, "Manager A1 -> B");
                cases.Complete("manager-b-preserves-physical-identity");

                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(enterTrigger);
                LocalPlayerProvisioningConsumerObservationSnapshot activityA2 =
                    await AwaitObservationAsync(
                        access,
                        observation => observation.IsAvailable &&
                            observation.Lifecycle.IsReady &&
                            observation.ActivityOccurrence > activityB.ActivityOccurrence &&
                            string.Equals(observation.Lifecycle.ActivityName,
                                activity.ActivityName,
                                StringComparison.Ordinal) &&
                            observation.Participation.JoinedCount == 1 &&
                            SlotIsFullyReady(observation, joinedSlotId),
                        "Manager Activity A2 did not reacquire a fresh ready contextual occurrence.",
                        FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        enterTrigger,
                        FrameBudget,
                        "Manager Activity A2 request did not succeed.");
                LocalPlayerProvisioningConsumerSlotObservation activityA2Slot =
                    FindObservedSlot(activityA2, joinedSlotId);
                Require(
                    activityA2Slot.HostEvidence.AssignmentToken !=
                        activityBSlot.HostEvidence.AssignmentToken &&
                    activityA2Slot.HostEvidence.AssignmentToken !=
                        activityA1Slot.HostEvidence.AssignmentToken &&
                    activityA2Slot.GameplayAdmission.InputBindingToken !=
                        activityBSlot.GameplayAdmission.InputBindingToken &&
                    FindSlot(activityA2.Participation, joinedSlotId).SelectionRevision ==
                        selectionRevisionBefore,
                    "Manager B -> A2 reused contextual assignment/input or reselected the Actor.");
                cases.Complete("manager-b-to-a2-fresh-context");
                AssertPhysicalRepresentation(
                    joinedHost, physicalActor, physicalActorEntityId,
                    physicalActorPosition, physicalActorRotation, "Manager B -> A2");
                cases.Complete("manager-a2-preserves-physical-identity");

                QaPlayerSurfacePublicNavigationSupport.ClearActivityPublic(
                    clearTrigger);
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        clearTrigger,
                        FrameBudget,
                        "Public Activity exit/clear did not succeed.");

                LocalPlayerProvisioningConsumerObservationSnapshot released =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.IsAvailable &&
                            observation.Lifecycle.IsReleased &&
                            observation.Lifecycle.SlotCount == 0 &&
                            observation.Participation.JoinedCount == 1 &&
                            observation.Lifecycle.HostCount >= 1,
                        "Activity exit did not release Activity-owned projection while preserving Session join/Host",
                        FrameBudget);
                Require(
                    joinedHost != null &&
                    joinedHost.IsJoined &&
                    CountActors(joinedHost) == 1,
                    "Activity exit destroyed the Session-owned physical Actor while releasing only the contextual projection.");
                cases.Complete("activity-exit-released");
                cases.Complete("session-host-persists");
                Require(
                    ReferenceEquals(physicalActor, joinedHost.ActorMount.GetChild(0)) &&
                    physicalActor.gameObject.GetEntityId().ToString() == physicalActorEntityId &&
                    physicalActor.position == physicalActorPosition &&
                    physicalActor.rotation == physicalActorRotation,
                    "Activity exit replaced, destroyed or implicitly repositioned the Session-owned physical Actor.");
                cases.Complete("activity-exit-preserves-physical-actor");

                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(
                    enterTrigger);
                LocalPlayerProvisioningConsumerObservationSnapshot reentered =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.IsAvailable &&
                            observation.HasCurrentActivityOccurrence &&
                            observation.ActivityOccurrence > firstOccurrence &&
                            string.Equals(
                                observation.Lifecycle.ActivityName,
                                activity.ActivityName,
                                StringComparison.Ordinal) &&
                            observation.Participation.JoinedCount == 1 &&
                            HasJoinedSlot(observation, joinedSlotId),
                        "Reentry did not expose a newer Activity occurrence with the same Session Slot",
                        FrameBudget);
                Require(
                    reentered.ActivityOccurrence > firstOccurrence &&
                    reentered.SessionRevision >= sessionRevisionAfterJoin,
                    "Reentry did not advance occurrence or preserve Session revision correlation. " +
                    DescribeObservation(reentered));
                cases.Complete("reentry-newer-occurrence");

                LocalPlayerProvisioningConsumerObservationSnapshot reentryReady =
                    await AwaitObservationAsync(
                        access,
                        observation =>
                            observation.IsAvailable &&
                            observation.Lifecycle.IsReady &&
                            observation.ActivityOccurrence ==
                                reentered.ActivityOccurrence &&
                            SlotIsFullyReady(observation, joinedSlotId),
                        "Reentry did not reach Ready without requiring a second Join",
                        FrameBudget);
                Require(
                    reentryReady.Participation.JoinedCount == 1 &&
                    CountActors(joinedHost) == 1 &&
                    joinedHost.IsJoined,
                    "Reentry duplicated Slot/Host or Actor evidence. " +
                    DescribeObservation(reentryReady));
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        enterTrigger,
                        FrameBudget,
                        "Public reentry Activity request did not succeed.");
                cases.Complete("reentry-no-duplicate-slot-actor");
                Require(
                    ReferenceEquals(physicalActor, joinedHost.ActorMount.GetChild(0)) &&
                    physicalActor.gameObject.GetEntityId().ToString() == physicalActorEntityId &&
                    physicalActor.position == physicalActorPosition &&
                    physicalActor.rotation == physicalActorRotation,
                    "Activity reentry replaced or implicitly repositioned the Session-owned physical Actor.");
                cases.Complete("reentry-preserves-physical-identity");

                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(
                    enterPlayerExcludedTrigger);
                LocalPlayerProvisioningConsumerObservationSnapshot playerExcluded =
                    await AwaitObservationAsync(
                        access,
                        observation => observation.IsAvailable &&
                            observation.HasCurrentActivityOccurrence &&
                            string.Equals(observation.Lifecycle.ActivityName,
                                playerExcludedActivity.ActivityName,
                                StringComparison.Ordinal) &&
                            observation.Participation.JoinedCount == 1 &&
                            observation.Lifecycle.SlotCount == 0 &&
                            !HasContextualAssignment(observation, joinedSlotId),
                        "Player-excluded Activity retained a current contextual Player assignment.",
                        FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        enterPlayerExcludedTrigger,
                        FrameBudget,
                        "Player-excluded Activity request did not succeed.");
                Require(
                    FindSlot(playerExcluded.Participation, joinedSlotId).SelectionRevision ==
                        selectionRevisionBefore,
                    "Player-excluded Activity changed the persistent selected Actor.");
                cases.Complete("player-excluded-context-absent");
                AssertPhysicalRepresentation(
                    joinedHost, physicalActor, physicalActorEntityId,
                    physicalActorPosition, physicalActorRotation,
                    "Manager Player-excluded Activity");
                cases.Complete("player-excluded-physical-preserved");

                QaPlayerSurfacePublicNavigationSupport.RequestActivityPublic(enterTrigger);
                LocalPlayerProvisioningConsumerObservationSnapshot afterExcluded =
                    await AwaitObservationAsync(
                        access,
                        observation => observation.IsAvailable && observation.Lifecycle.IsReady &&
                            observation.ActivityOccurrence > playerExcluded.ActivityOccurrence &&
                            string.Equals(observation.Lifecycle.ActivityName, activity.ActivityName,
                                StringComparison.Ordinal) &&
                            SlotIsFullyReady(observation, joinedSlotId),
                        "Player reentry after an excluded Activity did not produce a fresh ready context.",
                        FrameBudget);
                await QaPlayerSurfacePublicNavigationSupport
                    .AwaitTriggerTerminalSuccessAsync(
                        enterTrigger,
                        FrameBudget,
                        "Player reentry after an excluded Activity did not succeed.");
                LocalPlayerProvisioningConsumerSlotObservation afterExcludedSlot =
                    FindObservedSlot(afterExcluded, joinedSlotId);
                LocalPlayerProvisioningConsumerSlotObservation beforeExcludedSlot =
                    FindObservedSlot(reentryReady, joinedSlotId);
                Require(
                    afterExcludedSlot.HostEvidence.AssignmentToken !=
                        beforeExcludedSlot.HostEvidence.AssignmentToken &&
                    afterExcludedSlot.GameplayAdmission.InputBindingToken !=
                        beforeExcludedSlot.GameplayAdmission.InputBindingToken,
                    "Player reentry after the excluded Activity reused contextual Host/input evidence.");
                AssertPhysicalRepresentation(
                    joinedHost, physicalActor, physicalActorEntityId,
                    physicalActorPosition, physicalActorRotation,
                    "Manager Player reentry after excluded Activity");
                cases.Complete("player-reentry-after-exclusion-fresh");

                PlayerParticipationOperationResult closeResult =
                    access.CloseJoining(Source, "qa-player-surface-01-close-joining");
                Require(
                    closeResult != null &&
                    closeResult.Completed &&
                    closeResult.Snapshot != null &&
                    !closeResult.Snapshot.JoiningOpen,
                    closeResult != null
                        ? closeResult.ToDiagnosticString()
                        : "CloseJoining returned no public result.");
                joiningOpen = false;
                cases.Complete("joining-closed");

                if (clearTrigger != null &&
                    clearTrigger.HasActivityRuntimeBinding)
                {
                    try
                    {
                        QaPlayerSurfacePublicNavigationSupport.ClearActivityPublic(
                            clearTrigger);
                        await QaPlayerSurfacePublicNavigationSupport
                            .AwaitTriggerTerminalSuccessAsync(
                                clearTrigger,
                                FrameBudget,
                                "Final public Activity clear failed.");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("final-clear", exception);
                    }
                }

                cases.Complete("fixture-cleaned");

                if (terminateSession)
                {
                    Require(
                        joinedHost != null && joinedHost.IsJoined &&
                        physicalActor != null && joinResult.PlayerInput != null,
                        "Manager Session termination requires retained Session-owned Host, PlayerInput and physical Actor after contextual release.");
                    cases.Complete("session-termination-preconditions");

                    UnityEngine.Object.Destroy(host.gameObject);
                    await Awaitable.NextFrameAsync();
                    await Awaitable.NextFrameAsync();
                    Require(
                        host == null && joinedHost == null &&
                        joinResult.PlayerInput == null && physicalActor == null,
                        "Manager Session termination retained Framework, Host, PlayerInput, physical Actor, RuntimeContent or preparation/materialization evidence.");
                    cases.Complete("session-termination-physical-resources-released");

                    Require(
                        !access.Snapshot.IsAvailable ||
                        !access.TryGetObservation(
                            out LocalPlayerProvisioningConsumerObservationSnapshot terminated) ||
                        terminated == null || terminated.Participation == null ||
                        !terminated.Participation.IsInitialized ||
                        terminated.Participation.JoinedCount == 0,
                        "Manager Session termination retained public Session or contextual Player evidence.");
                    cases.Complete("session-termination-public-evidence-cleared");
                }

                RequirePublicSurfaceScanClean();
                cases.Complete("public-scan-clean");
                cases.RequireComplete();

                Debug.Log(
                    $"{Prefix} status='Passed' verdict='Q1_PASS' " +
                    $"cases='{cases.Count}' " +
                    $"occurrence1='{firstOccurrence}' " +
                    $"occurrence2='{reentered.ActivityOccurrence}' " +
                    $"sessionRevision='{reentryReady.SessionRevision}' " +
                    $"appliedRevision='{reentryReady.AppliedSessionRevision}' " +
                    $"availableSlots='{reentryReady.Participation.AvailableCount}' " +
                    $"sessionTermination='{terminateSession}' " +
                    $"slot='{joinedSlotId.StableText}' " +
                    "navigation='authored-ActivityRequestTrigger-composition-bound' " +
                    "proof='PublicNavigation,ScopedAccess,Joining,SupportedSlots,Join,Host,ActorSelection,NormalLifecycleReady,WaitCoveredPendingThenTerminal,ExitPreservesSession,ReentryNoDuplicate' " +
                    $"completed='{cases.DescribeCompleted()}'.");
            }
            catch (Exception exception)
            {
                failures.Add("execution", exception);
            }
            finally
            {
                if (clearTrigger != null &&
                    clearTrigger.HasActivityRuntimeBinding &&
                    (enterTrigger == null || enterTrigger.IsRequestInFlight ||
                     clearTrigger.IsRequestInFlight))
                {
                    try
                    {
                        if (!clearTrigger.IsRequestInFlight)
                        {
                            QaPlayerSurfacePublicNavigationSupport
                                .ClearActivityPublic(clearTrigger);
                        }

                        await QaPlayerSurfacePublicNavigationSupport
                            .AwaitTriggerTerminalSuccessAsync(
                                clearTrigger,
                                FrameBudget,
                                "Player Surface entry unwind did not settle before Play Mode teardown.");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("entry-unwind", exception);
                    }
                }

                if (joiningOpen && access != null && access.Snapshot.IsAvailable)
                {
                    try
                    {
                        access.CloseJoining(Source, "qa-player-surface-01-finally-close");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("joining-cleanup", exception);
                    }
                }

            }

            if (failures.HasFailures)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' verdict='Q1_FAIL' " +
                    $"cases='{cases.Count}/{cases.ExpectedCount}' " +
                    $"next='{cases.NextExpectedOrNone()}' " +
                    $"completed='{cases.DescribeCompleted()}' " +
                    $"missing='{cases.DescribeMissing()}' " +
                    $"execution='{Escape(failures.Describe("execution"))}' " +
                    $"entryUnwind='{Escape(failures.Describe("entry-unwind"))}' " +
                    $"reentryUnwind='{Escape(failures.Describe("reentry-unwind"))}' " +
                    $"joiningCleanup='{Escape(failures.Describe("joining-cleanup"))}' " +
                    $"fixtureCleanup='{Escape(failures.Describe("fixture-cleanup"))}'.");
                throw failures.ToAggregate(
                    "QA-PLAYER-SURFACE-01 public Player provisioning surface regression failed.");
            }
        }

        private static async Task<ILocalPlayerProvisioningConsumerAccess>
            AwaitScopedAccessAsync(
                LocalPlayerProvisioningConsumerAccessBinding binding,
                int frameBudget)
        {
            Require(binding != null, "Scoped access wait requires a consumer binding.");
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (binding.TryGetAccess(
                        out ILocalPlayerProvisioningConsumerAccess access,
                        out _) &&
                    access != null &&
                    access.Snapshot.IsAvailable)
                {
                    return access;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "LocalPlayerProvisioningConsumerAccessBinding did not become bound to a live public scope. " +
                $"state='{binding.BindingState}' diagnostic='{binding.Diagnostic}'.");
        }

        private static LocalPlayerProvisioningConsumerObservationSnapshot
            RequireObservation(
                ILocalPlayerProvisioningConsumerAccess access,
                string phase)
        {
            Require(access != null,
                $"Public consumer observation unavailable at phase '{phase}'. access=null");
            LocalPlayerProvisioningConsumerObservationSnapshot observation;
            bool available = access.TryGetObservation(out observation);
            Require(
                available &&
                observation != null &&
                observation.IsAvailable,
                $"Public consumer observation unavailable at phase '{phase}'. " +
                access.Snapshot.Diagnostic);
            return observation;
        }

        private static async Task<LocalPlayerProvisioningConsumerObservationSnapshot>
            AwaitObservationAsync(
                ILocalPlayerProvisioningConsumerAccess access,
                Func<LocalPlayerProvisioningConsumerObservationSnapshot, bool> predicate,
                string failure,
                int frameBudget)
        {
            Require(access != null && predicate != null,
                "Observation wait requires access and predicate.");
            LocalPlayerProvisioningConsumerObservationSnapshot latest = null;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (access.TryGetObservation(out latest) &&
                    latest != null &&
                    latest.IsAvailable &&
                    predicate(latest))
                {
                    return latest;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"{failure}. latest='{DescribeObservation(latest)}'.");
        }

        private static async Task<FrameworkActivityRequestResult>
            AwaitOwnedTerminalAsync(
                QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
                int frameBudget)
        {
            Require(owned != null && owned.HasOperation,
                "Owned Activity request wait requires an attached operation.");
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (owned.IsCompleted)
                {
                    return await owned.AwaitTerminalAsync();
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"Activity request did not terminate within '{frameBudget}' frames.");
        }

        private static async Task AwaitParticipantCycleAsync(
            QaActivityEntryReadinessFixture fixture,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
            int expectedPreparationCount,
            int frameBudget)
        {
            Require(fixture != null && owned != null,
                "Participant cycle wait requires fixture and owned operation.");
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (fixture.PreparationStartedCount >= expectedPreparationCount)
                {
                    return;
                }

                if (owned.IsCompleted)
                {
                    FrameworkActivityRequestResult early =
                        await owned.AwaitTerminalAsync();
                    throw new InvalidOperationException(
                        "Activity request terminated before the expected readiness preparation cycle. " +
                        $"expectedCycle='{expectedPreparationCount}' " +
                        $"started='{fixture.PreparationStartedCount}' " +
                        $"message='{early.Message}'.");
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Activity readiness participant did not start the expected preparation cycle. " +
                $"expectedCycle='{expectedPreparationCount}' " +
                $"started='{fixture.PreparationStartedCount}'.");
        }

        private static void ConfigurePlayerParticipation(
            ActivityAsset activity,
            PlayerParticipationRequirementLevel requirement,
            PlayerSlotProfile slotProfile)
        {
            var serialized = new SerializedObject(activity);
            SetEnumName(
                RequireProperty(serialized, "playerParticipationProjectionMode"),
                ActivityParticipationProjectionMode.ExplicitSlots.ToString());
            SetEnumName(
                RequireProperty(serialized, "playerParticipationZeroParticipantPolicy"),
                ActivityParticipationZeroParticipantPolicy.Rejected.ToString());
            SetEnumName(
                RequireProperty(serialized, "playerParticipationRequirementLevel"),
                requirement.ToString());
            SerializedProperty explicitSlots = RequireProperty(
                serialized,
                "playerParticipationExplicitSlotProfiles");
            explicitSlots.arraySize = 1;
            explicitSlots.GetArrayElementAtIndex(0).objectReferenceValue = slotProfile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerSlotProfile ResolveFirstLocalPlayerSlot()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            GameApplicationAsset application =
                settings != null ? settings.ActiveGameApplication : null;
            PlayerSlotProfile slot = null;
            Require(
                application != null &&
                QaPlayerSessionQaSupport.TryGetSupportedSlot(application, 0, out slot) &&
                slot != null,
                "Could not resolve first Local Player Slot from active GameApplication.");
            return slot;
        }

        private static bool HasJoinedSlot(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            if (observation?.Participation == null || !slotId.IsValid)
            {
                return false;
            }

            PlayerSlotRuntimeSnapshot slot =
                FindSlot(observation.Participation, slotId);
            return slot.IsJoined && slot.PlayerSlotId == slotId;
        }

        private static bool HasHostEvidence(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            if (observation?.Slots == null)
            {
                return false;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                if (slot.Slot.PlayerSlotId == slotId &&
                    slot.IsJoined &&
                    slot.HasHostEvidence &&
                    slot.HostEvidence.IsRecorded)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasContextualAssignment(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            if (observation?.Slots == null)
            {
                return false;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                if (slot.Slot.PlayerSlotId == slotId &&
                    slot.HasCurrentActorEvidence && slot.CurrentActor.IsAssigned)
                {
                    return true;
                }
            }

            return false;
        }

        private static LocalPlayerProvisioningConsumerSlotObservation FindObservedSlot(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            Require(observation?.Slots != null && slotId.IsValid,
                "Contextual Slot evidence lookup requires an available observation and Slot id.");
            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                if (slot.Slot.PlayerSlotId == slotId)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException(
                $"Public observation has no contextual evidence for Slot '{slotId.StableText}'.");
        }

        private static void AssertPhysicalRepresentation(
            LocalPlayerHostAuthoring host,
            Transform actor,
            string expectedEntityId,
            Vector3 expectedPosition,
            Quaternion expectedRotation,
            string transition)
        {
            Require(
                host != null && host.IsJoined && host.ActorMount != null &&
                host.ActorMount.childCount == 1 &&
                ReferenceEquals(actor, host.ActorMount.GetChild(0)) &&
                actor != null &&
                actor.gameObject.GetEntityId().ToString() == expectedEntityId &&
                actor.position == expectedPosition && actor.rotation == expectedRotation,
                transition + " replaced, destroyed or implicitly repositioned the Session-owned physical Actor.");
        }

        private static bool HasSelectedActor(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId,
            ActorProfile expected)
        {
            if (observation?.Participation == null || expected == null)
            {
                return false;
            }

            PlayerSlotRuntimeSnapshot slot =
                FindSlot(observation.Participation, slotId);
            return slot.IsJoined &&
                slot.HasSelectedActor &&
                ReferenceEquals(slot.SelectedActorProfile, expected);
        }

        private static bool SlotIsFullyReady(
            LocalPlayerProvisioningConsumerObservationSnapshot observation,
            PlayerSlotId slotId)
        {
            if (observation?.Slots == null)
            {
                return false;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                LocalPlayerProvisioningConsumerSlotObservation slot =
                    observation.Slots[index];
                if (slot.Slot.PlayerSlotId != slotId)
                {
                    continue;
                }

                return slot.IsJoined &&
                    slot.HasSelectedActor &&
                    slot.IsLogicalActorPrepared &&
                    slot.IsPhysicallyMaterialized &&
                    slot.IsGameplayAdmitted;
            }

            return false;
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot participation,
            PlayerSlotId slotId)
        {
            Require(participation != null && slotId.IsValid,
                "Slot lookup requires participation and a valid Slot id.");
            for (int index = 0; index < participation.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = participation.Slots[index];
                if (slot.PlayerSlotId == slotId)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException(
                $"Slot '{slotId.StableText}' is not present in the public participation snapshot.");
        }

        private static int CountActors(LocalPlayerHostAuthoring host)
        {
            Require(
                host != null && host.ActorMount != null,
                "Actor count requires a Local Player Host with ActorMount.");
            return host.ActorMount
                .GetComponentsInChildren<PlayerActorDeclaration>(true)
                .Length;
        }

        private static void RequirePublicSurfaceScanClean()
        {
            string path =
                "Assets/ImmersiveFrameworkQA/GameFlow/InternalEditor/" +
                "QaPlayerProvisioningPublicSurfaceRegression.cs";
            string source = System.IO.File.ReadAllText(path);
            // Tokens are assembled so the scan method itself does not contain
            // contiguous forbidden operational call-sites.
            string[] forbidden =
            {
                "System." + "Reflection",
                "FindObject" + "OfType<",
                "FindObjects" + "ByType<",
                "FindObjectsOfType" + "All<",
                "GetComponentsInChildren<LocalPlayerActor" +
                    "SelectionRequestAuthoring",
                "PrepareSelected" + "Actor(",
                "EnsureGameplay" + "Ready(",
                "TryReconcile" + "(",
                "GetComponent<PlayerActor" + "Preparation",
                "GetComponent<Player" + "Gameplay",
                "GetComponent<LocalPlayerProvisioning" + "RuntimeHostModule",
                "RuntimeScope" + "Context"
            };

            for (int index = 0; index < forbidden.Length; index++)
            {
                Require(
                    source.IndexOf(forbidden[index], StringComparison.Ordinal) < 0,
                    $"QA-PLAYER-SURFACE-01 source scan found forbidden token '{forbidden[index]}'.");
            }
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string propertyName)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Serialized property '{propertyName}' was not found.");
            return property;
        }

        private static void SetEnumName(
            SerializedProperty property,
            string enumName)
        {
            int index = Array.IndexOf(property.enumNames, enumName);
            Require(index >= 0,
                $"Enum value '{enumName}' was not found for '{property.propertyPath}'.");
            property.enumValueIndex = index;
        }

        private static string DescribeObservation(
            LocalPlayerProvisioningConsumerObservationSnapshot observation)
        {
            if (observation == null)
            {
                return "observation='null'";
            }

            return
                $"available='{observation.IsAvailable}' " +
                $"lifecycle='{observation.Lifecycle?.Status}' " +
                $"activity='{observation.Lifecycle?.ActivityName}' " +
                $"occurrence='{observation.ActivityOccurrence}' " +
                $"sessionRevision='{observation.SessionRevision}' " +
                $"appliedRevision='{observation.AppliedSessionRevision}' " +
                $"joined='{observation.Participation?.JoinedCount}' " +
                $"hostCount='{observation.Lifecycle?.HostCount}' " +
                $"gateHeld='{observation.Lifecycle?.GateHeld}' " +
                $"ready='{observation.Lifecycle?.IsReady}' " +
                $"diagnostic='{observation.Diagnostic}'";
        }

        private static string DescribeLoading(
            QaLoadingSurfaceVisibilityHoldAdapter loading)
        {
            if (loading == null)
            {
                return " loading='unavailable'";
            }

            return
                $" loadingVisible='{loading.IsVisible}' " +
                $"loadingAlpha='{loading.CurrentAlpha}'";
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

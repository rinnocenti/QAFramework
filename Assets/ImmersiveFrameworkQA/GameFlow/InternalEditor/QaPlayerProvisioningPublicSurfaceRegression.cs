using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Loading;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// QA-PLAYER-SURFACE-01 — Public-only positive Manager-Provisioned Player
    /// lifecycle contract proof.
    ///
    /// Player commands and observation use only the consumer surfaces available
    /// to a real game (P1 scoped access, P2 observation, public Actor selection,
    /// public readiness/loading evidence). Activity entry/exit arrangement reuses
    /// the existing readiness fixture environment; it does not replace the public
    /// Player path under test and does not call prepare/materialize/admit/reconcile.
    /// </summary>
    public static class QaPlayerProvisioningPublicSurfaceRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/" +
            "Run QA-PLAYER-SURFACE-01 Public Provisioning Surface Regression";
        private const string Prefix = "[QA_PLAYER_SURFACE_01]";
        private const string Source = nameof(QaPlayerProvisioningPublicSurfaceRegression);
        private const string ConsumerRootName = "QA_PLAYER_SURFACE_01_Consumer";
        private const int FrameBudget = 360;
        private const int ExpectedCaseCount = 28;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "runtime-started",
            "consumer-binding-created",
            "scoped-access-available",
            "fresh-session-confirmed",
            "fixture-created",
            "waitcovered-activity-configured",
            "activity-entry-started",
            "waiting-for-join-observed",
            "waitcovered-loading-pending",
            "joining-opened",
            "dynamic-capacity-set",
            "public-join-succeeded",
            "joined-slot-host-observed",
            "default-actor-selection-requested",
            "selected-actor-observed",
            "normal-lifecycle-ready",
            "prepared-materialized-admitted",
            "waitcovered-loading-terminal",
            "activity-entry-completed",
            "activity-exit-released",
            "session-host-persists",
            "reentry-newer-occurrence",
            "reentry-no-duplicate-slot-actor",
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

        private static async Task RunAsync()
        {
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            var failures = new QaFailureCollector();
            FrameworkRuntimeHost host = null;
            QaActivityEntryReadinessFixture fixture = null;
            GameObject consumerRoot = null;
            LocalPlayerProvisioningConsumerAccessBinding consumerBinding = null;
            ILocalPlayerProvisioningConsumerAccess access = null;
            LocalPlayerActorSelectionRequestAuthoring actorSelection = null;
            LocalPlayerJoinResult joinResult = null;
            LocalPlayerHostAuthoring joinedHost = null;
            QaLoadingSurfaceVisibilityHoldAdapter loading = null;
            var ownedEntry =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-player-surface-01-entry");
            var ownedReentry =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-player-surface-01-reentry");
            bool joiningOpen = false;
            int firstOccurrence = 0;
            int sessionRevisionAfterJoin = 0;
            PlayerSlotId joinedSlotId = default;

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "QA-PLAYER-SURFACE-01 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaM07InternalReconcileSetup.RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(
                    host != null && host.State.GameFlowStarted &&
                    host.State.CurrentRoute != null,
                    "QA-PLAYER-SURFACE-01 requires a started Game Flow runtime with a current Route.");
                cases.Complete("runtime-started");

                loading = ResolveLoadingAdapter();
                actorSelection = ResolveUniqueActorSelectionAuthoring();
                Require(
                    actorSelection != null,
                    "Public LocalPlayerActorSelectionRequestAuthoring was not available.");
                string selectionConfigIssue;
                Require(
                    actorSelection.TryValidateConfiguration(out selectionConfigIssue),
                    selectionConfigIssue);
                Require(
                    actorSelection.HasPlayerActorSelectionRuntimeBinding &&
                    actorSelection.RuntimeReady,
                    "Public Actor selection authoring is not runtime-ready. " +
                    actorSelection.PlayerActorSelectionRuntimeBindingDiagnostic);

                PlayerSlotProfile slotProfile = ResolveFirstLocalPlayerSlot();
                Require(
                    slotProfile != null &&
                    slotProfile.PlayerSlotId.IsValid &&
                    slotProfile.DefaultActorProfile != null &&
                    slotProfile.DefaultActorProfile.LogicalActorHostPrefab != null,
                    "QA-PLAYER-SURFACE-01 requires a configured first Local Player Slot with default Actor.");

                consumerRoot = CreateRouteConsumerRoot(
                    host.State.CurrentRoute,
                    out consumerBinding);
                cases.Complete("consumer-binding-created");

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

                fixture = await QaActivityEntryReadinessFixture.CreateAsync();
                fixture.ExpectParticipantPreparationCycles(2);
                cases.Complete("fixture-created");

                ActivityAsset activity = fixture.CreateActivity(
                    "qa.player.surface.01.waitcovered",
                    "QA Player Surface 01 WaitCovered",
                    ActivityEntryReadinessPolicy.WaitCovered,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup.ContentScenePath);
                ConfigurePlayerParticipation(
                    activity,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    slotProfile);
                cases.Complete("waitcovered-activity-configured");

                ownedEntry.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        Source,
                        "qa-player-surface-01-waitcovered-entry"));
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
                    !ownedEntry.IsCompleted,
                    "WaitCovered Activity request completed before any public Join. " +
                    DescribeObservation(waiting));
                cases.Complete("waiting-for-join-observed");

                await AwaitParticipantCycleAsync(fixture, ownedEntry, 1, FrameBudget);

                // Complete the temporary non-Player readiness participant so the
                // remaining WaitCovered hold is attributable to required Player
                // participation, which is the public contract under test.
                if (fixture.Participant.State ==
                    ActivityReadinessParticipantState.Preparing)
                {
                    fixture.Participant.CompletePreparation();
                }

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
                        "Player WaitCovered hold was not retained after non-Player readiness completed",
                        FrameBudget);
                Require(
                    !ownedEntry.IsCompleted &&
                    pendingHold.Lifecycle.GateHeld &&
                    !pendingHold.Lifecycle.IsReady &&
                    (loading == null ||
                        loading.IsVisible ||
                        loading.CurrentAlpha > 0.001f ||
                        loading.HideHoldActive),
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

                int configuredSlots = openResult.Snapshot.ConfiguredSlotCount;
                int requestedCapacity = configuredSlots > 0
                    ? Math.Max(1, Math.Min(configuredSlots, openResult.Snapshot.DynamicCapacity > 0
                        ? openResult.Snapshot.DynamicCapacity
                        : 1))
                    : 1;
                // Prefer an explicit public capacity write when structural room exists.
                if (configuredSlots >= 2)
                {
                    requestedCapacity = 2;
                }

                PlayerParticipationOperationResult capacityResult =
                    access.SetDynamicCapacity(
                        requestedCapacity,
                        Source,
                        "qa-player-surface-01-set-dynamic-capacity");
                Require(
                    capacityResult != null &&
                    capacityResult.Completed &&
                    capacityResult.Snapshot != null &&
                    capacityResult.Snapshot.DynamicCapacity == requestedCapacity,
                    capacityResult != null
                        ? capacityResult.ToDiagnosticString()
                        : "SetDynamicCapacity returned no public result.");
                cases.Complete("dynamic-capacity-set");

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

                FrameworkActivityRequestResult entryTerminal =
                    await AwaitOwnedTerminalAsync(ownedEntry, FrameBudget);
                Require(
                    entryTerminal.Succeeded,
                    string.IsNullOrWhiteSpace(entryTerminal.Message)
                        ? "WaitCovered Activity entry did not succeed after Player Ready."
                        : entryTerminal.Message);
                Require(
                    readyObservation.Lifecycle.IsReady &&
                    !readyObservation.Lifecycle.GateHeld &&
                    (loading == null ||
                        (!loading.IsVisible && loading.CurrentAlpha <= 0.001f)),
                    "Loading/gate did not reach a terminal released state after Player Ready. " +
                    DescribeObservation(readyObservation) +
                    DescribeLoading(loading));
                cases.Complete("waitcovered-loading-terminal");
                cases.Complete("activity-entry-completed");

                FrameworkActivityRequestResult clearResult =
                    await fixture.Activities.ClearActivityAsync(
                        Source,
                        "qa-player-surface-01-exit");
                Require(
                    clearResult.Succeeded,
                    string.IsNullOrWhiteSpace(clearResult.Message)
                        ? "Activity exit did not succeed."
                        : clearResult.Message);

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
                    CountActors(joinedHost) == 0,
                    "Activity exit did not release Activity-owned Actor while Session Host/join persisted.");
                cases.Complete("activity-exit-released");
                cases.Complete("session-host-persists");

                ownedReentry.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        Source,
                        "qa-player-surface-01-reentry"));
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

                await AwaitParticipantCycleAsync(fixture, ownedReentry, 2, FrameBudget);
                if (fixture.Participant.State ==
                    ActivityReadinessParticipantState.Preparing)
                {
                    fixture.Participant.CompletePreparation();
                }

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
                FrameworkActivityRequestResult reentryTerminal =
                    await AwaitOwnedTerminalAsync(ownedReentry, FrameBudget);
                Require(
                    reentryTerminal.Succeeded,
                    string.IsNullOrWhiteSpace(reentryTerminal.Message)
                        ? "Reentry Activity request did not succeed."
                        : reentryTerminal.Message);
                cases.Complete("reentry-no-duplicate-slot-actor");

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

                await fixture.DisposeAsync();
                fixture = null;
                cases.Complete("fixture-cleaned");

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
                    $"capacity='{capacityResult.Snapshot.DynamicCapacity}' " +
                    $"slot='{joinedSlotId.StableText}' " +
                    "proof='ScopedAccess,Joining,Capacity,Join,Host,ActorSelection,NormalLifecycleReady,WaitCoveredPendingThenTerminal,ExitPreservesSession,ReentryNoDuplicate' " +
                    $"completed='{cases.DescribeCompleted()}'.");
            }
            catch (Exception exception)
            {
                failures.Add("execution", exception);
            }
            finally
            {
                if (ownedReentry.HasOperation && !ownedReentry.ReachedTerminal)
                {
                    try
                    {
                        await ownedReentry.UnwindAsync(
                            async () =>
                            {
                                if (fixture != null)
                                {
                                    await fixture.Activities.ClearActivityAsync(
                                        Source,
                                        "qa-player-surface-01-reentry-unwind");
                                }
                            });
                    }
                    catch (Exception exception)
                    {
                        failures.Add("reentry-unwind", exception);
                    }
                }

                if (ownedEntry.HasOperation && !ownedEntry.ReachedTerminal)
                {
                    try
                    {
                        await ownedEntry.UnwindAsync(
                            async () =>
                            {
                                if (fixture != null)
                                {
                                    await fixture.Activities.ClearActivityAsync(
                                        Source,
                                        "qa-player-surface-01-entry-unwind");
                                }
                            });
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

                if (fixture != null)
                {
                    try
                    {
                        await fixture.DisposeAsync();
                    }
                    catch (Exception exception)
                    {
                        failures.Add("fixture-cleanup", exception);
                    }
                }

                if (consumerRoot != null)
                {
                    UnityEngine.Object.Destroy(consumerRoot);
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

        private static GameObject CreateRouteConsumerRoot(
            RouteAsset route,
            out LocalPlayerProvisioningConsumerAccessBinding binding)
        {
            Require(route != null, "Route consumer root requires the current Route.");
            Scene primary = default;
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene candidate = SceneManager.GetSceneAt(index);
                if (candidate.IsValid() &&
                    candidate.isLoaded &&
                    string.Equals(
                        candidate.name,
                        route.PrimarySceneName,
                        StringComparison.Ordinal))
                {
                    primary = candidate;
                    break;
                }
            }

            Require(
                primary.IsValid() && primary.isLoaded,
                $"Current Route primary scene '{route.PrimarySceneName}' is not loaded for consumer binding.");

            var root = new GameObject(ConsumerRootName);
            SceneManager.MoveGameObjectToScene(root, primary);
            binding = root.AddComponent<LocalPlayerProvisioningConsumerAccessBinding>();
            var serialized = new SerializedObject(binding);
            SerializedProperty scope = serialized.FindProperty("scope");
            Require(scope != null, "Consumer binding is missing serialized scope field.");
            int routeIndex = Array.IndexOf(
                scope.enumNames,
                nameof(LocalPlayerProvisioningConsumerScope.Route));
            Require(routeIndex >= 0, "Consumer binding scope enum lacks Route.");
            scope.enumValueIndex = routeIndex;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(
                binding.Scope == LocalPlayerProvisioningConsumerScope.Route,
                "Route-scoped consumer binding was not applied.");
            return root;
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
                application.TryGetLocalPlayerSlot(0, out slot) &&
                slot != null,
                "Could not resolve first Local Player Slot from active GameApplication.");
            return slot;
        }

        private static LocalPlayerActorSelectionRequestAuthoring
            ResolveUniqueActorSelectionAuthoring()
        {
            LocalPlayerActorSelectionRequestAuthoring found = null;
            for (int sceneIndex = 0;
                 sceneIndex < SceneManager.sceneCount;
                 sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    LocalPlayerActorSelectionRequestAuthoring[] candidates =
                        roots[rootIndex].GetComponentsInChildren<
                            LocalPlayerActorSelectionRequestAuthoring>(true);
                    for (int index = 0; index < candidates.Length; index++)
                    {
                        LocalPlayerActorSelectionRequestAuthoring candidate =
                            candidates[index];
                        if (candidate == null)
                        {
                            continue;
                        }

                        Require(
                            found == null || ReferenceEquals(found, candidate),
                            "Multiple LocalPlayerActorSelectionRequestAuthoring surfaces are loaded; public QA expects the canonical product authoring.");
                        found = candidate;
                    }
                }
            }

            return found;
        }

        private static QaLoadingSurfaceVisibilityHoldAdapter ResolveLoadingAdapter()
        {
            for (int sceneIndex = 0;
                 sceneIndex < SceneManager.sceneCount;
                 sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                GameObject[] roots = scene.GetRootGameObjects();
                for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                {
                    QaLoadingSurfaceVisibilityHoldAdapter adapter =
                        roots[rootIndex].GetComponentInChildren<
                            QaLoadingSurfaceVisibilityHoldAdapter>(true);
                    if (adapter != null)
                    {
                        return adapter;
                    }
                }
            }

            return null;
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

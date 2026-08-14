using System;
using System.Reflection;
using ImmersiveFrameworkQA.Player;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-M07-12B-5 Play Mode regression.
    ///
    /// Proves that LocalPlayerProvisioningAuthoring exposes the real read-only
    /// Activity projection over the Session Player state:
    /// - Session retains both joined technical Hosts;
    /// - an explicit Activity projection exposes only its selected Slot;
    /// - a Session revision outside that projection does not expand it;
    /// - Activity exit empties contextual Slot projection without releasing
    ///   Session-owned joins or Hosts;
    /// - a later Activity occurrence projects another Slot;
    /// - Player readiness contribution is not reported as the aggregate
    ///   Activity readiness gate;
    /// - Ready is evaluated against the Activity entry policy, so a
    ///   LogicalActorsPrepared projection does not require GameplayReady;
    /// - repeated public reads do not mutate runtime state.
    /// </summary>
    public static class QaM07ActivitySessionLifecycleProjectionRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Participation/Run Activity Session Lifecycle Projection";
        private const string Prefix =
            "[QA_IF_M07_12B_5_ACTIVITY_SESSION_PROJECTION]";
        private const string PlayerReadinessObjectName =
            "Player Activity Readiness";
        private const int FrameBudget = 240;
        private const int StartupFrameBudget = 300;
        private const int ExpectedCaseCount = 30;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "official-host-resolved",
            "provisioning-authoring-resolved",
            "two-slot-fixture-confirmed",
            "fresh-session-confirmed",
            "fixture-created",
            "first-activity-configured",
            "first-request-started",
            "first-projection-pending",
            "joining-opened",
            "first-public-join-succeeded",
            "first-projection-ready",
            "first-public-read-idempotent",
            "second-public-join-succeeded",
            "session-expanded-projection-stable",
            "excluded-slot-not-materialized",
            "first-activity-cleared",
            "released-projection-empty-session-retained",
            "second-activity-reconfigured",
            "second-request-started",
            "second-projection-switched",
            "player-gate-not-aggregate",
            "second-request-completed",
            "second-projection-ready",
            "second-public-read-idempotent",
            "second-activity-cleared",
            "final-release-preserved-session",
            "joining-closed",
            "fixture-cleaned"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            await RunAsync();
        }

        /// <summary>
        /// Typed Play Mode entry point for the canonical Player QA orchestrator.
        /// </summary>
        public static Task RunForFullPlayerQaAsync() => RunAsync();

        private static async Task RunAsync()
        {
            var cases = new QaCaseRegistry(
                ExpectedCases,
                ExpectedCaseCount);
            var failures = new QaFailureCollector();

            FrameworkRuntimeHost host = null;
            LocalPlayerProvisioningAuthoring authoring = null;
            QaActivityEntryReadinessFixture fixture = null;
            var firstRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-5-first-projection");
            var secondRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-5-second-projection");
            bool joiningOpen = false;

            LocalPlayerJoinResult firstJoin = null;
            LocalPlayerJoinResult secondJoin = null;
            int firstOccurrence = 0;
            int secondOccurrence = 0;

            try
            {
                Require(EditorApplication.isPlaying,
                    "IF-M07-12B-5 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaM07InternalReconcileSetup
                    .RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                string hostDiagnostic =
                    "FrameworkRuntimeHost startup has not been evaluated.";
                for (int frame = 0; frame < StartupFrameBudget; frame++)
                {
                    if (QaH2FrameworkReadiness.TryResolveUniqueHost(
                            out host,
                            out hostDiagnostic) &&
                        host != null &&
                        host.State.GameFlowStarted)
                    {
                        break;
                    }

                    await Awaitable.NextFrameAsync();
                }

                Require(host != null && host.State.GameFlowStarted,
                    "IF-M07-12B-5 requires the official started FrameworkRuntimeHost " +
                    $"within '{StartupFrameBudget}' frames. {hostDiagnostic}");
                cases.Complete("official-host-resolved");

                authoring = ResolveProvisioningAuthoring(host);
                Require(authoring != null && authoring.RuntimeReady,
                    "IF-M07-12B-5 could not resolve ready Local Player provisioning authoring.");
                cases.Complete("provisioning-authoring-resolved");

                ImmersiveFrameworkSettingsAsset settings =
                    Resources.Load<ImmersiveFrameworkSettingsAsset>(
                        ImmersiveFrameworkSettingsAsset.ResourcesPath);
                GameApplicationAsset application =
                    settings != null
                        ? settings.ActiveGameApplication
                        : null;
                PlayerSlotProfile firstSlotProfile = null;
                PlayerSlotProfile secondSlotProfile = null;
                Require(application != null &&
                    QaPlayerSessionQaSupport.TryGetSupportedSlot(
                        application,
                        0,
                        out firstSlotProfile) &&
                    firstSlotProfile != null &&
                    QaPlayerSessionQaSupport.TryGetSupportedSlot(
                        application,
                        1,
                        out secondSlotProfile) &&
                    secondSlotProfile != null &&
                    firstSlotProfile.PlayerSlotId.IsValid &&
                    secondSlotProfile.PlayerSlotId.IsValid &&
                    firstSlotProfile.PlayerSlotId !=
                        secondSlotProfile.PlayerSlotId &&
                    firstSlotProfile.DefaultActorProfile != null &&
                    firstSlotProfile.DefaultActorProfile
                        .LogicalActorHostPrefab != null &&
                    secondSlotProfile.DefaultActorProfile != null &&
                    secondSlotProfile.DefaultActorProfile
                        .LogicalActorHostPrefab != null,
                    "IF-M07-12B-5 requires two identity-distinct configured Slots with explicit default Actors.");
                cases.Complete("two-slot-fixture-confirmed");

                PlayerParticipationSnapshot initialSession =
                    authoring.RuntimeSnapshot;
                Require(CountJoined(initialSession) == 0 &&
                    authoring.PlayerInputManager != null &&
                    authoring.PlayerInputManager.playerCount == 0,
                    "IF-M07-12B-5 is one-shot. Enter a fresh Play Mode with no joined Players.");
                cases.Complete("fresh-session-confirmed");

                fixture =
                    await QaActivityEntryReadinessFixture.CreateAsync();
                fixture.ExpectParticipantPreparationCycles(2);
                cases.Complete("fixture-created");

                ActivityAsset activity = fixture.CreateActivity(
                    "qa.m07.12b5.activity-session-projection",
                    "Q3 M07 Activity Session Projection",
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup.ContentScenePath);
                ConfigureExplicitPlayerProjection(
                    activity,
                    PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared,
                    firstSlotProfile);
                cases.Complete("first-activity-configured");

                firstRequest.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07ActivitySessionLifecycleProjectionRegression),
                        "qa-if-m07-12b-5-first-projection"));
                cases.Complete("first-request-started");

                await AwaitParticipantCycleOrTerminalAsync(
                    fixture,
                    firstRequest,
                    1,
                    FrameBudget);

                ManagerProvisionedPlayerLifecycleSnapshot firstPending =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot.IsAvailable &&
                            snapshot.ActivityOccurrence > 0 &&
                            string.Equals(
                                snapshot.ActivityName,
                                activity.ActivityName,
                                StringComparison.Ordinal) &&
                            snapshot.Status ==
                                ManagerProvisionedPlayerLifecycleStatus
                                    .WaitingForJoin &&
                            snapshot.HasGateEvidence &&
                            snapshot.GateEvidenceScope ==
                                ManagerProvisionedPlayerGateEvidenceScope
                                    .ActivityPlayerReadinessContribution &&
                            snapshot.GateHeld &&
                            ProjectsOnly(
                                snapshot,
                                firstSlotProfile),
                        "first Activity occurrence did not expose its exact pending Slot projection",
                        FrameBudget);
                firstOccurrence =
                    firstPending.ActivityOccurrence;
                Require(!firstRequest.IsCompleted &&
                    fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing &&
                    ResolvePlayerReadinessParticipant(host).State ==
                        ActivityReadinessParticipantState.Preparing,
                    "First Activity request did not retain both readiness participants while Slot 1 was absent.");
                fixture.Participant.CompletePreparation();
                cases.Complete("first-projection-pending");

                PlayerParticipationOperationResult open =
                    authoring.OpenJoining(
                        nameof(
                            QaM07ActivitySessionLifecycleProjectionRegression),
                        "qa-if-m07-12b-5-open-joining");
                Require(open != null &&
                    open.Completed &&
                    open.Snapshot.JoiningOpen &&
                    authoring.PlayerInputManager.joiningEnabled,
                    open != null
                        ? open.ToDiagnosticString()
                        : "Opening joining returned no result.");
                joiningOpen = true;
                cases.Complete("joining-opened");

                firstJoin = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(
                            QaM07ActivitySessionLifecycleProjectionRegression),
                        "qa-if-m07-12b-5-first-public-join"));
                Require(firstJoin != null &&
                    firstJoin.Succeeded &&
                    firstJoin.HasCommitEvidence &&
                    firstJoin.HasAssignmentEvidence &&
                    firstJoin.Slot.PlayerSlotId ==
                        firstSlotProfile.PlayerSlotId &&
                    firstJoin.LocalPlayerHost != null &&
                    firstJoin.PlayerInput != null,
                    firstJoin != null
                        ? firstJoin.ToDiagnosticString()
                        : "First public Join returned no result.");
                cases.Complete("first-public-join-succeeded");

                FrameworkActivityRequestResult firstTerminal =
                    await AwaitOwnedTerminalAsync(
                        firstRequest,
                        FrameBudget);
                Require(
                    firstTerminal.Succeeded,
                    string.IsNullOrWhiteSpace(firstTerminal.Message)
                        ? "First Activity request did not succeed."
                        : firstTerminal.Message);

                ManagerProvisionedPlayerLifecycleSnapshot firstReady =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot.IsReady &&
                            snapshot.ActivityOccurrence ==
                                firstOccurrence &&
                            snapshot.SessionRevision ==
                                authoring.RuntimeSnapshot.Revision &&
                            snapshot.AppliedSessionRevision ==
                                snapshot.SessionRevision &&
                            snapshot.HasGateEvidence &&
                            snapshot.GateEvidenceScope ==
                                ManagerProvisionedPlayerGateEvidenceScope
                                    .ActivityPlayerReadinessContribution &&
                            !snapshot.GateHeld &&
                            snapshot.HostCount == 1 &&
                            ProjectsOnlyReadySlot(
                                snapshot,
                                firstSlotProfile),
                        "first Activity projection did not become ready from real Session and Activity evidence",
                        FrameBudget);
                Require(CountActors(firstJoin.LocalPlayerHost) == 1,
                    "Projected Slot 1 did not own exactly one Activity Actor after readiness.");
                cases.Complete("first-projection-ready");

                await RequirePublicReadIdempotentAsync(
                    authoring,
                    firstJoin.LocalPlayerHost,
                    null,
                    firstReady);
                cases.Complete("first-public-read-idempotent");

                InputDevice sharedDevice =
                    firstJoin.PlayerInput.devices.Count > 0
                        ? firstJoin.PlayerInput.devices[0]
                        : null;
                Require(sharedDevice != null && sharedDevice.added,
                    "Second Join requires one explicit active InputDevice from the first PlayerInput.");

                secondJoin = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(
                            QaM07ActivitySessionLifecycleProjectionRegression),
                        "qa-if-m07-12b-5-second-public-join",
                        sharedDevice));
                Require(secondJoin != null &&
                    secondJoin.Succeeded &&
                    secondJoin.HasCommitEvidence &&
                    secondJoin.HasAssignmentEvidence &&
                    secondJoin.Slot.PlayerSlotId ==
                        secondSlotProfile.PlayerSlotId &&
                    secondJoin.LocalPlayerHost != null &&
                    secondJoin.PlayerInput != null,
                    secondJoin != null
                        ? secondJoin.ToDiagnosticString()
                        : "Second public Join returned no result.");
                cases.Complete("second-public-join-succeeded");

                PlayerParticipationSnapshot expandedSession =
                    authoring.RuntimeSnapshot;
                Require(CountJoined(expandedSession) == 2,
                    "Second Join did not expand Session participation to two joined Slots.");

                ManagerProvisionedPlayerLifecycleSnapshot firstAfterSecond =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot.IsReady &&
                            snapshot.ActivityOccurrence ==
                                firstOccurrence &&
                            snapshot.SessionRevision ==
                                authoring.RuntimeSnapshot.Revision &&
                            snapshot.AppliedSessionRevision ==
                                snapshot.SessionRevision &&
                            snapshot.HostCount == 2 &&
                            ProjectsOnlyReadySlot(
                                snapshot,
                                firstSlotProfile),
                        "Session revision for excluded Slot 2 expanded or destabilized the first Activity projection",
                        FrameBudget);
                Require(firstAfterSecond.SlotCount == 1 &&
                    CountJoined(authoring.RuntimeSnapshot) == 2,
                    "Activity projection and Session participation were collapsed into the same Slot list.");
                cases.Complete("session-expanded-projection-stable");

                Require(CountActors(firstJoin.LocalPlayerHost) == 1 &&
                    CountActors(secondJoin.LocalPlayerHost) == 0,
                    "Excluded Slot 2 was physically materialized by an Activity that projects only Slot 1.");
                cases.Complete("excluded-slot-not-materialized");

                FrameworkActivityRequestResult firstClear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07ActivitySessionLifecycleProjectionRegression),
                        "qa-if-m07-12b-5-clear-first-projection");
                Require(
                    firstClear.Succeeded,
                    string.IsNullOrWhiteSpace(firstClear.Message)
                        ? "Clearing first Activity occurrence did not succeed."
                        : firstClear.Message);
                cases.Complete("first-activity-cleared");

                ManagerProvisionedPlayerLifecycleSnapshot firstReleased =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot.IsAvailable &&
                            snapshot.IsReleased &&
                            snapshot.SlotCount == 0 &&
                            snapshot.HostCount == 2 &&
                            snapshot.SessionRevision ==
                                authoring.RuntimeSnapshot.Revision,
                        "Activity exit did not empty contextual projection while preserving Session evidence",
                        FrameBudget);
                Require(CountJoined(authoring.RuntimeSnapshot) == 2 &&
                    firstJoin.LocalPlayerHost.IsJoined &&
                    secondJoin.LocalPlayerHost.IsJoined &&
                    CountActors(firstJoin.LocalPlayerHost) == 0 &&
                    CountActors(secondJoin.LocalPlayerHost) == 0,
                    "Activity exit released or rematerialized Session-owned joins/Hosts incorrectly.");
                cases.Complete(
                    "released-projection-empty-session-retained");

                ConfigureExplicitPlayerProjection(
                    activity,
                    PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared,
                    secondSlotProfile);
                cases.Complete("second-activity-reconfigured");

                secondRequest.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07ActivitySessionLifecycleProjectionRegression),
                        "qa-if-m07-12b-5-second-projection"));
                cases.Complete("second-request-started");

                await AwaitParticipantCycleOrTerminalAsync(
                    fixture,
                    secondRequest,
                    2,
                    FrameBudget);

                ManagerProvisionedPlayerLifecycleSnapshot secondProjected =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot.IsAvailable &&
                            snapshot.ActivityOccurrence >
                                firstOccurrence &&
                            string.Equals(
                                snapshot.ActivityName,
                                activity.ActivityName,
                                StringComparison.Ordinal) &&
                            snapshot.HasGateEvidence &&
                            snapshot.GateEvidenceScope ==
                                ManagerProvisionedPlayerGateEvidenceScope
                                    .ActivityPlayerReadinessContribution &&
                            ProjectsOnly(
                                snapshot,
                                secondSlotProfile),
                        "second Activity occurrence did not replace Slot 1 projection with Slot 2 projection",
                        FrameBudget);
                secondOccurrence =
                    secondProjected.ActivityOccurrence;
                Require(secondOccurrence > firstOccurrence,
                    "Activity re-entry did not create a newer occurrence.");
                cases.Complete("second-projection-switched");

                ManagerProvisionedPlayerLifecycleSnapshot secondPlayerReady =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot.IsReady &&
                            snapshot.ActivityOccurrence ==
                                secondOccurrence &&
                            snapshot.HasGateEvidence &&
                            snapshot.GateEvidenceScope ==
                                ManagerProvisionedPlayerGateEvidenceScope
                                    .ActivityPlayerReadinessContribution &&
                            !snapshot.GateHeld &&
                            snapshot.HostCount == 2 &&
                            ProjectsOnlyReadySlot(
                                snapshot,
                                secondSlotProfile),
                        "Player readiness contribution did not become ready for the second projected Slot",
                        FrameBudget);
                Require(!secondRequest.IsCompleted &&
                    fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing,
                    "Public Player lifecycle projection was incorrectly coupled to the aggregate Activity readiness gate.");
                cases.Complete("player-gate-not-aggregate");

                fixture.Participant.CompletePreparation();
                FrameworkActivityRequestResult secondTerminal =
                    await AwaitOwnedTerminalAsync(
                        secondRequest,
                        FrameBudget);
                Require(
                    secondTerminal.Succeeded,
                    string.IsNullOrWhiteSpace(secondTerminal.Message)
                        ? "Second Activity request did not succeed."
                        : secondTerminal.Message);
                cases.Complete("second-request-completed");

                ManagerProvisionedPlayerLifecycleSnapshot secondReady =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot.IsReady &&
                            snapshot.ActivityOccurrence ==
                                secondOccurrence &&
                            snapshot.SessionRevision ==
                                authoring.RuntimeSnapshot.Revision &&
                            snapshot.AppliedSessionRevision ==
                                snapshot.SessionRevision &&
                            snapshot.HostCount == 2 &&
                            ProjectsOnlyReadySlot(
                                snapshot,
                                secondSlotProfile),
                        "second Activity projection did not remain ready after aggregate Activity readiness completed",
                        FrameBudget);
                Require(CountActors(firstJoin.LocalPlayerHost) == 0 &&
                    CountActors(secondJoin.LocalPlayerHost) == 1,
                    "Second Activity occurrence did not materialize only the newly projected Slot 2 Actor.");
                cases.Complete("second-projection-ready");

                await RequirePublicReadIdempotentAsync(
                    authoring,
                    firstJoin.LocalPlayerHost,
                    secondJoin.LocalPlayerHost,
                    secondReady);
                cases.Complete("second-public-read-idempotent");

                FrameworkActivityRequestResult secondClear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07ActivitySessionLifecycleProjectionRegression),
                        "qa-if-m07-12b-5-clear-second-projection");
                Require(
                    secondClear.Succeeded,
                    string.IsNullOrWhiteSpace(secondClear.Message)
                        ? "Clearing second Activity occurrence did not succeed."
                        : secondClear.Message);
                cases.Complete("second-activity-cleared");

                ManagerProvisionedPlayerLifecycleSnapshot finalReleased =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot.IsAvailable &&
                            snapshot.IsReleased &&
                            snapshot.SlotCount == 0 &&
                            snapshot.HostCount == 2 &&
                            snapshot.SessionRevision ==
                                authoring.RuntimeSnapshot.Revision,
                        "final Activity exit did not preserve the Session while emptying Activity projection",
                        FrameBudget);
                Require(CountJoined(authoring.RuntimeSnapshot) == 2 &&
                    firstJoin.LocalPlayerHost.IsJoined &&
                    secondJoin.LocalPlayerHost.IsJoined &&
                    CountActors(firstJoin.LocalPlayerHost) == 0 &&
                    CountActors(secondJoin.LocalPlayerHost) == 0 &&
                    finalReleased.SessionRevision ==
                        authoring.RuntimeSnapshot.Revision,
                    "Final release changed Session-owned participation, Hosts or revision.");
                cases.Complete("final-release-preserved-session");

                PlayerParticipationOperationResult close =
                    authoring.CloseJoining(
                        nameof(
                            QaM07ActivitySessionLifecycleProjectionRegression),
                        "qa-if-m07-12b-5-close-joining");
                Require(close != null &&
                    close.Completed &&
                    !close.Snapshot.JoiningOpen &&
                    !authoring.PlayerInputManager.joiningEnabled,
                    close != null
                        ? close.ToDiagnosticString()
                        : "Closing joining returned no result.");
                joiningOpen = false;
                cases.Complete("joining-closed");

                await fixture.DisposeAsync();
                fixture = null;
                cases.Complete("fixture-cleaned");
                cases.RequireComplete();
            }
            catch (TargetInvocationException exception)
            {
                failures.Add(
                    "execution",
                    exception.InnerException ?? exception);
            }
            catch (Exception exception)
            {
                failures.Add("execution", exception);
            }
            finally
            {
                if (secondRequest.HasOperation &&
                    !secondRequest.ReachedTerminal)
                {
                    try
                    {
                        await secondRequest.UnwindAsync(
                            () => FailPendingReadinessAsync(
                                host,
                                fixture,
                                "qa-if-m07-12b-5-second-unwind"));
                    }
                    catch (Exception exception)
                    {
                        failures.Add(
                            "second-operation-unwind",
                            exception);
                    }
                }

                if (firstRequest.HasOperation &&
                    !firstRequest.ReachedTerminal)
                {
                    try
                    {
                        await firstRequest.UnwindAsync(
                            () => FailPendingReadinessAsync(
                                host,
                                fixture,
                                "qa-if-m07-12b-5-first-unwind"));
                    }
                    catch (Exception exception)
                    {
                        failures.Add(
                            "first-operation-unwind",
                            exception);
                    }
                }

                if (joiningOpen && authoring != null)
                {
                    try
                    {
                        PlayerParticipationOperationResult close =
                            authoring.CloseJoining(
                                nameof(
                                    QaM07ActivitySessionLifecycleProjectionRegression),
                                "qa-if-m07-12b-5-finally-close-joining");
                        if (close == null || !close.Completed)
                        {
                            throw new InvalidOperationException(
                                close != null
                                    ? close.ToDiagnosticString()
                                    : "Joining close returned no result.");
                        }
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
            }

            if (failures.HasFailures)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' " +
                    $"cases='{cases.Count}/{cases.ExpectedCount}' " +
                    $"next='{cases.NextExpectedOrNone()}' " +
                    $"completed='{cases.DescribeCompleted()}' " +
                    $"missing='{cases.DescribeMissing()}' " +
                    $"execution='{Escape(failures.Describe("execution"))}' " +
                    $"firstUnwind='{Escape(failures.Describe("first-operation-unwind"))}' " +
                    $"secondUnwind='{Escape(failures.Describe("second-operation-unwind"))}' " +
                    $"joiningCleanup='{Escape(failures.Describe("joining-cleanup"))}' " +
                    $"fixtureCleanup='{Escape(failures.Describe("fixture-cleanup"))}'.");
                throw failures.ToAggregate(
                    "IF-M07-12B-5 Activity/Session lifecycle projection regression failed.");
            }

            ManagerProvisionedPlayerLifecycleSnapshot finalSnapshot =
                authoring.ManagerProvisionedLifecycleSnapshot;
            Debug.Log(
                $"{Prefix} status='Passed' " +
                $"cases='{cases.Count}' " +
                $"firstOccurrence='{firstOccurrence}' " +
                $"secondOccurrence='{secondOccurrence}' " +
                $"sessionRevision='{finalSnapshot.SessionRevision}' " +
                $"hostCount='{finalSnapshot.HostCount}' " +
                $"projectedSlots='{finalSnapshot.SlotCount}' " +
                "proof='ExplicitActivitySubset,SessionExpansionWithoutProjectionExpansion,ActivityExitPreservesSession,OccurrenceProjectionReplacement,PlayerContributionNotAggregateGate,ReadOnlyIdempotency' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static async Task AwaitParticipantCycleOrTerminalAsync(
            QaActivityEntryReadinessFixture fixture,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
            int expectedPreparationCount,
            int frameBudget)
        {
            Require(fixture != null && owned != null,
                "Participant cycle wait requires fixture and owned operation.");
            Require(expectedPreparationCount > 0 && frameBudget > 0,
                "Participant cycle wait arguments are invalid.");

            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (fixture.PreparationStartedCount >=
                    expectedPreparationCount)
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

        private static async Task<FrameworkActivityRequestResult>
            AwaitOwnedTerminalAsync(
                QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
                int frameBudget)
        {
            Require(owned != null && owned.HasOperation,
                "Owned Activity request wait requires an attached operation.");
            Require(frameBudget > 0,
                "Owned Activity request frame budget must be positive.");

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

        private static async Task<ManagerProvisionedPlayerLifecycleSnapshot>
            AwaitSnapshotAsync(
                LocalPlayerProvisioningAuthoring authoring,
                Func<ManagerProvisionedPlayerLifecycleSnapshot, bool>
                    predicate,
                string failure,
                int frameBudget)
        {
            Require(authoring != null && predicate != null,
                "Lifecycle snapshot wait requires authoring and predicate.");
            Require(frameBudget > 0,
                "Lifecycle snapshot frame budget must be positive.");

            ManagerProvisionedPlayerLifecycleSnapshot latest = null;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                latest =
                    authoring.ManagerProvisionedLifecycleSnapshot;
                if (latest != null && predicate(latest))
                {
                    return latest;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"{failure}. latest='{latest?.ToDiagnosticString()}'.");
        }

        private static async Task RequirePublicReadIdempotentAsync(
            LocalPlayerProvisioningAuthoring authoring,
            LocalPlayerHostAuthoring firstHost,
            LocalPlayerHostAuthoring secondHost,
            ManagerProvisionedPlayerLifecycleSnapshot baseline)
        {
            Require(authoring != null && baseline != null,
                "Idempotency proof requires authoring and baseline snapshot.");

            int sessionRevision =
                authoring.RuntimeSnapshot.Revision;
            int firstActorCount =
                firstHost != null
                    ? CountActors(firstHost)
                    : 0;
            int secondActorCount =
                secondHost != null
                    ? CountActors(secondHost)
                    : 0;
            string expected = Fingerprint(baseline);

            ManagerProvisionedPlayerLifecycleSnapshot sameFrame =
                authoring.ManagerProvisionedLifecycleSnapshot;
            ManagerProvisionedPlayerLifecycleSnapshot sameFrameAgain =
                authoring.ManagerProvisionedLifecycleSnapshot;
            await Awaitable.NextFrameAsync();
            ManagerProvisionedPlayerLifecycleSnapshot nextFrame =
                authoring.ManagerProvisionedLifecycleSnapshot;

            Require(string.Equals(
                    expected,
                    Fingerprint(sameFrame),
                    StringComparison.Ordinal) &&
                string.Equals(
                    expected,
                    Fingerprint(sameFrameAgain),
                    StringComparison.Ordinal) &&
                string.Equals(
                    expected,
                    Fingerprint(nextFrame),
                    StringComparison.Ordinal) &&
                authoring.RuntimeSnapshot.Revision == sessionRevision &&
                (firstHost == null ||
                 CountActors(firstHost) == firstActorCount) &&
                (secondHost == null ||
                 CountActors(secondHost) == secondActorCount),
                "Repeated public lifecycle reads changed projection, Session revision or Actor materialization.");
        }

        private static string Fingerprint(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "<null>";
            }

            var builder = new StringBuilder();
            builder.Append(snapshot.IsAvailable).Append('|')
                .Append(snapshot.Status).Append('|')
                .Append(snapshot.ActivityName).Append('|')
                .Append(snapshot.ActivityOccurrence).Append('|')
                .Append(snapshot.SessionRevision).Append('|')
                .Append(snapshot.RequestedSessionRevision).Append('|')
                .Append(snapshot.AppliedSessionRevision).Append('|')
                .Append(snapshot.EntryPolicy).Append('|')
                .Append(snapshot.ReadinessStatus).Append('|')
                .Append(snapshot.ReadinessReason).Append('|')
                .Append(snapshot.GateEvidenceScope).Append('|')
                .Append(snapshot.HasGateEvidence).Append('|')
                .Append(snapshot.GateHeld).Append('|')
                .Append(snapshot.JoiningOpen).Append('|')
                .Append(snapshot.HostCount).Append('|')
                .Append(snapshot.SlotCount);

            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                ManagerProvisionedPlayerLifecycleSlotSnapshot slot =
                    snapshot.Slots[index];
                builder.Append("||")
                    .Append(slot.PlayerSlotId).Append('|')
                    .Append(slot.SlotState).Append('|')
                    .Append(slot.HasTechnicalHost).Append('|')
                    .Append(slot.SelectedActorProfile).Append('|')
                    .Append(slot.LogicalActorPrepared).Append('|')
                    .Append(slot.PhysicalActorMaterialized).Append('|')
                    .Append(slot.GameplayAdmitted);
            }

            return builder.ToString();
        }

        private static bool ProjectsOnly(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            PlayerSlotProfile expectedSlot)
        {
            return snapshot != null &&
                expectedSlot != null &&
                expectedSlot.PlayerSlotId.IsValid &&
                snapshot.SlotCount == 1 &&
                snapshot.Slots.Count == 1 &&
                string.Equals(
                    snapshot.Slots[0].PlayerSlotId,
                    expectedSlot.PlayerSlotId.StableText,
                    StringComparison.Ordinal);
        }

        private static bool ProjectsOnlyReadySlot(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            PlayerSlotProfile expectedSlot)
        {
            if (!ProjectsOnly(snapshot, expectedSlot))
            {
                return false;
            }

            ManagerProvisionedPlayerLifecycleSlotSnapshot slot =
                snapshot.Slots[0];
            return string.Equals(
                    snapshot.EntryPolicy,
                    PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared.ToString(),
                    StringComparison.Ordinal) &&
                slot.HasTechnicalHost &&
                slot.HasSelectedActor &&
                slot.LogicalActorPrepared &&
                slot.PhysicalActorMaterialized;
        }

        private static void ConfigureExplicitPlayerProjection(
            ActivityAsset activity,
            PlayerParticipationRequirementLevel requirement,
            PlayerSlotProfile slot)
        {
            Require(activity != null,
                "Player projection configuration requires an Activity.");
            Require(slot != null && slot.PlayerSlotId.IsValid,
                "Player projection configuration requires one valid Slot Profile.");

            var serialized = new SerializedObject(activity);
            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationProjectionMode"),
                ActivityParticipationProjectionMode
                    .ExplicitSlots.ToString());
            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationZeroParticipantPolicy"),
                ActivityParticipationZeroParticipantPolicy
                    .Rejected.ToString());
            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationRequirementLevel"),
                requirement.ToString());

            SerializedProperty explicitSlots = RequireProperty(
                serialized,
                "playerParticipationExplicitSlotProfiles");
            explicitSlots.arraySize = 1;
            explicitSlots.GetArrayElementAtIndex(0)
                .objectReferenceValue = slot;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(activity.PlayerParticipationProjectionMode ==
                    ActivityParticipationProjectionMode.ExplicitSlots &&
                activity.PlayerParticipationRequirementLevel ==
                    requirement,
                "Runtime Activity explicit Player projection did not apply.");
        }

        private static LocalPlayerProvisioningAuthoring
            ResolveProvisioningAuthoring(FrameworkRuntimeHost host)
        {
            bool resolved = QaPlayerRuntimeObservationBridge
                .TryGetLocalPlayerProvisioningAuthoring(
                    host,
                    out LocalPlayerProvisioningAuthoring authoring,
                    out string diagnostic);
            Require(resolved && authoring != null,
                string.IsNullOrWhiteSpace(diagnostic)
                    ? "Framework runtime did not expose Local Player provisioning authoring."
                    : diagnostic);
            return authoring;
        }

        private static ActivityReadinessParticipant
            ResolvePlayerReadinessParticipant(FrameworkRuntimeHost host)
        {
            Transform child = host.transform.Find(
                PlayerReadinessObjectName);
            ActivityReadinessParticipant participant =
                child != null
                    ? child.GetComponent<ActivityReadinessParticipant>()
                    : null;
            Require(participant != null,
                "FrameworkRuntimeHost has no Player Activity Readiness participant.");
            return participant;
        }

        private static Task FailPendingReadinessAsync(
            FrameworkRuntimeHost host,
            QaActivityEntryReadinessFixture fixture,
            string reason)
        {
            if (fixture != null &&
                fixture.Participant != null &&
                fixture.Participant.State ==
                    ActivityReadinessParticipantState.Preparing)
            {
                fixture.Participant.FailPreparation(reason);
            }

            if (host != null)
            {
                Transform child = host.transform.Find(
                    PlayerReadinessObjectName);
                ActivityReadinessParticipant participant =
                    child != null
                        ? child.GetComponent<ActivityReadinessParticipant>()
                        : null;
                if (participant != null &&
                    participant.State ==
                        ActivityReadinessParticipantState.Preparing)
                {
                    participant.FailPreparation(reason);
                }
            }

            return Task.CompletedTask;
        }

        private static int CountJoined(
            PlayerParticipationSnapshot snapshot)
        {
            Require(snapshot != null,
                "Player participation snapshot is missing.");
            int count = 0;
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                if (snapshot.Slots[index].IsJoined)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActors(
            LocalPlayerHostAuthoring host)
        {
            Require(host != null && host.ActorMount != null,
                "Actor count requires a Local Player Host with ActorMount.");
            return host.ActorMount.GetComponentsInChildren<
                PlayerActorDeclaration>(true).Length;
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string propertyName)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            Require(property != null,
                $"Serialized property '{propertyName}' was not found on '{serialized.targetObject.name}'.");
            return property;
        }

        private static void SetEnumName(
            SerializedProperty property,
            string enumName)
        {
            int index = Array.IndexOf(
                property.enumNames,
                enumName);
            Require(index >= 0,
                $"Enum value '{enumName}' was not found for serialized property '{property.propertyPath}'.");
            property.enumValueIndex = index;
        }

        private static void Require(
            bool condition,
            string message)
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

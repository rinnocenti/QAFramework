using System;
using System.Reflection;
using ImmersiveFrameworkQA.Player;
using ImmersiveFrameworkQA.Player.Internal.Editor;
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

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-M07-12B-8 Play Mode regression.
    ///
    /// Proves occurrence-scoped Activity Player projection freeze for the
    /// dynamic AllJoinedSlots authoring mode:
    /// - the first occurrence captures an empty projection while the Session
    ///   has no joined Players and zero participants are allowed;
    /// - a Player joining after the Activity becomes authoritative changes the
    ///   Session but cannot expand the already active occurrence;
    /// - Activity release uses the frozen empty projection and preserves the
    ///   Session-owned Join and technical Host;
    /// - re-entry creates a newer occurrence and captures the now joined Slot;
    /// - repeated public reads are observational and do not mutate Session,
    ///   projection membership or Actor materialization.
    /// </summary>
    public static class QaM07ActiveProjectionFreezeRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Participation/Run Active Projection Freeze";
        private const string Prefix =
            "[QA_IF_M07_12B_8_ACTIVE_PROJECTION_FREEZE]";
        private const int FrameBudget = 300;
        private const int ExpectedCaseCount = 30;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "official-host-resolved",
            "provisioning-authoring-resolved",
            "slot-fixture-confirmed",
            "fresh-session-confirmed",
            "fixture-created",
            "activity-configured",
            "first-request-started",
            "first-occurrence-empty",
            "first-occurrence-public-idempotent-before-commit",
            "first-request-completed",
            "joining-opened",
            "late-join-succeeded",
            "session-revision-advanced",
            "first-occurrence-owner-stable",
            "first-occurrence-membership-frozen",
            "first-occurrence-public-idempotent-after-mutation",
            "late-join-not-materialized",
            "first-activity-cleared",
            "first-release-preserved-session",
            "second-request-started",
            "second-occurrence-recaptured",
            "second-occurrence-public-ready",
            "second-request-completed",
            "second-occurrence-public-idempotent",
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
                    "qa-if-m07-12b-8-first-occurrence");
            var secondRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-8-second-occurrence");

            bool joiningOpen = false;
            LocalPlayerJoinResult lateJoin = null;
            int initialSessionRevision = 0;
            int joinedSessionRevision = 0;
            int firstOccurrence = 0;
            int secondOccurrence = 0;

            try
            {
                Require(EditorApplication.isPlaying,
                    "IF-M07-12B-8 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaM07InternalReconcileSetup
                    .RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(host != null && host.State.GameFlowStarted,
                    "IF-M07-12B-8 requires the official started FrameworkRuntimeHost.");
                cases.Complete("official-host-resolved");

                authoring = ResolveProvisioningAuthoring(host);
                Require(authoring != null && authoring.RuntimeReady,
                    "IF-M07-12B-8 could not resolve ready Local Player provisioning authoring.");
                cases.Complete("provisioning-authoring-resolved");

                PlayerSlotProfile firstSlotProfile =
                    ResolveFirstConfiguredSlotProfile();
                Require(firstSlotProfile != null &&
                    firstSlotProfile.PlayerSlotId.IsValid,
                    "IF-M07-12B-8 requires one configured Player Slot Profile.");
                cases.Complete("slot-fixture-confirmed");

                PlayerParticipationSnapshot initialSession =
                    authoring.RuntimeSnapshot;
                Require(initialSession != null &&
                    initialSession.IsInitialized &&
                    initialSession.Revision > 0 &&
                    CountJoined(initialSession) == 0 &&
                    authoring.PlayerInputManager != null &&
                    authoring.PlayerInputManager.playerCount == 0,
                    "IF-M07-12B-8 is one-shot. Enter a fresh Play Mode with no joined Players.");
                initialSessionRevision = initialSession.Revision;
                cases.Complete("fresh-session-confirmed");

                fixture =
                    await QaActivityEntryReadinessFixture.CreateAsync();
                fixture.ExpectParticipantPreparationCycles(2);
                cases.Complete("fixture-created");

                ActivityAsset activity = fixture.CreateActivity(
                    "qa.m07.12b8.active-projection-freeze",
                    "Q3 M07 Active Projection Freeze",
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup.ContentScenePath);
                ConfigureAllJoinedProjection(activity);
                cases.Complete("activity-configured");

                firstRequest.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07ActiveProjectionFreezeRegression),
                        "qa-if-m07-12b-8-first-occurrence"));
                cases.Complete("first-request-started");

                await AwaitParticipantCycleOrTerminalAsync(
                    fixture,
                    firstRequest,
                    1,
                    FrameBudget);

                ManagerProvisionedPlayerLifecycleSnapshot firstEmpty =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesFrozenEmptyOccurrence(
                                snapshot,
                                activity,
                                initialSessionRevision,
                                expectedHostCount: 0,
                                expectedOccurrence: 0),
                        "First Activity occurrence did not capture the empty AllJoinedSlots projection",
                        FrameBudget);
                firstOccurrence = firstEmpty.ActivityOccurrence;
                Require(firstOccurrence > 0,
                    "First Activity occurrence sequence is invalid.");
                cases.Complete("first-occurrence-empty");

                await RequirePublicReadIdempotentAsync(
                    authoring,
                    null,
                    firstEmpty);
                cases.Complete(
                    "first-occurrence-public-idempotent-before-commit");

                Require(fixture.Participant != null &&
                    fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing,
                    "First aggregate Activity readiness participant is not Preparing.");
                fixture.Participant.CompletePreparation();

                FrameworkActivityRequestResult firstTerminal =
                    await AwaitOwnedTerminalAsync(
                        firstRequest,
                        FrameBudget);
                Require(firstTerminal.Succeeded,
                    !string.IsNullOrWhiteSpace(firstTerminal.Message)
                        ? firstTerminal.Message
                        : "First Activity request did not succeed.");
                Require(fixture.RuntimeHost.State.CurrentActivity != null &&
                    fixture.RuntimeHost.State.CurrentActivity
                        .HasSameIdentity(activity),
                    "First Activity occurrence did not become authoritative.");
                cases.Complete("first-request-completed");

                PlayerParticipationOperationResult open =
                    authoring.OpenJoining(
                        nameof(
                            QaM07ActiveProjectionFreezeRegression),
                        "qa-if-m07-12b-8-open-joining");
                Require(open != null &&
                    open.Completed &&
                    open.Snapshot.JoiningOpen &&
                    authoring.PlayerInputManager.joiningEnabled,
                    open != null
                        ? open.ToDiagnosticString()
                        : "Opening joining returned no result.");
                joiningOpen = true;
                cases.Complete("joining-opened");

                lateJoin = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(
                            QaM07ActiveProjectionFreezeRegression),
                        "qa-if-m07-12b-8-late-join"));
                Require(lateJoin != null &&
                    lateJoin.Succeeded &&
                    lateJoin.HasCommitEvidence &&
                    lateJoin.HasAssignmentEvidence &&
                    lateJoin.Slot.PlayerSlotId ==
                        firstSlotProfile.PlayerSlotId &&
                    lateJoin.LocalPlayerHost != null &&
                    lateJoin.PlayerInput != null,
                    lateJoin != null
                        ? lateJoin.ToDiagnosticString()
                        : "Late public Join returned no result.");
                cases.Complete("late-join-succeeded");

                PlayerParticipationSnapshot joinedSession =
                    await AwaitSessionAsync(
                        authoring,
                        snapshot =>
                            snapshot.Revision >
                                initialSessionRevision &&
                            CountJoined(snapshot) == 1 &&
                            authoring.PlayerInputManager.playerCount == 1,
                        "Late Join did not advance Session revision and occupancy",
                        FrameBudget);
                joinedSessionRevision = joinedSession.Revision;
                cases.Complete("session-revision-advanced");

                Require(fixture.RuntimeHost.State.CurrentActivity != null &&
                    fixture.RuntimeHost.State.CurrentActivity
                        .HasSameIdentity(activity),
                    "Late Join replaced the active Activity authority.");
                cases.Complete("first-occurrence-owner-stable");

                ManagerProvisionedPlayerLifecycleSnapshot frozenAfterJoin =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesFrozenEmptyOccurrence(
                                snapshot,
                                activity,
                                joinedSessionRevision,
                                expectedHostCount: 1,
                                expectedOccurrence: firstOccurrence),
                        "Late Join expanded or replaced the first Activity occurrence projection",
                        FrameBudget);
                cases.Complete("first-occurrence-membership-frozen");

                await RequirePublicReadIdempotentAsync(
                    authoring,
                    lateJoin.LocalPlayerHost,
                    frozenAfterJoin);
                cases.Complete(
                    "first-occurrence-public-idempotent-after-mutation");

                Require(CountActors(lateJoin.LocalPlayerHost) == 0,
                    "A Slot excluded from the first occurrence was physically materialized retroactively.");
                cases.Complete("late-join-not-materialized");

                FrameworkActivityRequestResult firstClear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07ActiveProjectionFreezeRegression),
                        "qa-if-m07-12b-8-clear-first-occurrence");
                Require(firstClear.Succeeded,
                    !string.IsNullOrWhiteSpace(firstClear.Message)
                        ? firstClear.Message
                        : "Clearing the first Activity occurrence did not succeed.");
                cases.Complete("first-activity-cleared");

                ManagerProvisionedPlayerLifecycleSnapshot firstReleased =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot != null &&
                            snapshot.IsAvailable &&
                            snapshot.IsReleased &&
                            snapshot.SlotCount == 0 &&
                            snapshot.Slots.Count == 0 &&
                            snapshot.HostCount == 1 &&
                            snapshot.SessionRevision ==
                                joinedSessionRevision,
                        "First Activity release did not preserve the Session while releasing the frozen projection",
                        FrameBudget);
                Require(CountJoined(authoring.RuntimeSnapshot) == 1 &&
                    authoring.RuntimeSnapshot.Revision ==
                        joinedSessionRevision &&
                    lateJoin.LocalPlayerHost.IsJoined &&
                    authoring.PlayerInputManager.playerCount == 1 &&
                    CountActors(lateJoin.LocalPlayerHost) == 0,
                    "First Activity release changed Session-owned Join, Host, revision or Actor state.");
                cases.Complete("first-release-preserved-session");

                secondRequest.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07ActiveProjectionFreezeRegression),
                        "qa-if-m07-12b-8-second-occurrence"));
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
                            snapshot != null &&
                            snapshot.IsAvailable &&
                            snapshot.ActivityOccurrence >
                                firstOccurrence &&
                            string.Equals(
                                snapshot.ActivityName,
                                activity.ActivityName,
                                StringComparison.Ordinal) &&
                            snapshot.SessionRevision ==
                                joinedSessionRevision &&
                            snapshot.HostCount == 1 &&
                            ProjectsOnly(
                                snapshot,
                                firstSlotProfile),
                        "Second Activity occurrence did not recapture the joined Slot",
                        FrameBudget);
                secondOccurrence =
                    secondProjected.ActivityOccurrence;
                Require(secondOccurrence > firstOccurrence,
                    "Activity re-entry did not create a newer occurrence.");
                cases.Complete("second-occurrence-recaptured");

                await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot != null &&
                            snapshot.IsReady &&
                            snapshot.ActivityOccurrence ==
                                secondOccurrence &&
                            snapshot.SessionRevision ==
                                joinedSessionRevision &&
                            snapshot.HostCount == 1 &&
                            ProjectsOnlyJoinedHost(
                                snapshot,
                                firstSlotProfile),
                        "Second Activity occurrence did not expose a Ready projection for the recaptured Slot",
                        FrameBudget);
                Require(CountActors(lateJoin.LocalPlayerHost) == 0,
                    "JoinedSlots requirement materialized a Logical Actor unexpectedly.");
                cases.Complete("second-occurrence-public-ready");

                Require(fixture.Participant != null &&
                    fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing,
                    "Second aggregate Activity readiness participant is not Preparing.");
                fixture.Participant.CompletePreparation();

                FrameworkActivityRequestResult secondTerminal =
                    await AwaitOwnedTerminalAsync(
                        secondRequest,
                        FrameBudget);
                Require(secondTerminal.Succeeded,
                    !string.IsNullOrWhiteSpace(secondTerminal.Message)
                        ? secondTerminal.Message
                        : "Second Activity request did not succeed.");
                Require(fixture.RuntimeHost.State.CurrentActivity != null &&
                    fixture.RuntimeHost.State.CurrentActivity
                        .HasSameIdentity(activity),
                    "Second Activity occurrence did not become authoritative.");
                cases.Complete("second-request-completed");

                // The pre-commit Ready snapshot above is valid evidence for the
                // captured membership, but aggregate readiness completion may
                // legitimately advance readiness/gate diagnostics. Establish the
                // idempotency baseline only after the Activity request is terminal.
                await Awaitable.NextFrameAsync();
                ManagerProvisionedPlayerLifecycleSnapshot secondCommitted =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot != null &&
                            snapshot.IsAvailable &&
                            snapshot.ActivityOccurrence ==
                                secondOccurrence &&
                            snapshot.SessionRevision ==
                                joinedSessionRevision &&
                            snapshot.HostCount == 1 &&
                            ProjectsOnlyJoinedHost(
                                snapshot,
                                firstSlotProfile),
                        "Second Activity occurrence did not retain the recaptured Slot after commit",
                        FrameBudget);

                await RequirePublicReadIdempotentAsync(
                    authoring,
                    lateJoin.LocalPlayerHost,
                    secondCommitted);
                cases.Complete("second-occurrence-public-idempotent");

                FrameworkActivityRequestResult secondClear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07ActiveProjectionFreezeRegression),
                        "qa-if-m07-12b-8-clear-second-occurrence");
                Require(secondClear.Succeeded,
                    !string.IsNullOrWhiteSpace(secondClear.Message)
                        ? secondClear.Message
                        : "Clearing the second Activity occurrence did not succeed.");
                cases.Complete("second-activity-cleared");

                ManagerProvisionedPlayerLifecycleSnapshot finalReleased =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot != null &&
                            snapshot.IsAvailable &&
                            snapshot.IsReleased &&
                            snapshot.SlotCount == 0 &&
                            snapshot.Slots.Count == 0 &&
                            snapshot.HostCount == 1 &&
                            snapshot.SessionRevision ==
                                joinedSessionRevision,
                        "Final Activity release did not preserve Session ownership",
                        FrameBudget);
                Require(CountJoined(authoring.RuntimeSnapshot) == 1 &&
                    authoring.RuntimeSnapshot.Revision ==
                        joinedSessionRevision &&
                    lateJoin.LocalPlayerHost.IsJoined &&
                    authoring.PlayerInputManager.playerCount == 1 &&
                    CountActors(lateJoin.LocalPlayerHost) == 0 &&
                    finalReleased.SessionRevision ==
                        joinedSessionRevision,
                    "Final release changed Session-owned participation, Host, revision or Actor state.");
                cases.Complete("final-release-preserved-session");

                PlayerParticipationOperationResult close =
                    authoring.CloseJoining(
                        nameof(
                            QaM07ActiveProjectionFreezeRegression),
                        "qa-if-m07-12b-8-close-joining");
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
                                "qa-if-m07-12b-8-second-unwind"));
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
                                "qa-if-m07-12b-8-first-unwind"));
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
                                    QaM07ActiveProjectionFreezeRegression),
                                "qa-if-m07-12b-8-finally-close-joining");
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
                    "IF-M07-12B-8 Active projection freeze regression failed.");
            }

            PlayerParticipationSnapshot finalSession =
                authoring.RuntimeSnapshot;
            ManagerProvisionedPlayerLifecycleSnapshot finalProjection =
                authoring.ManagerProvisionedLifecycleSnapshot;
            Debug.Log(
                $"{Prefix} status='Passed' " +
                $"cases='{cases.Count}' " +
                $"firstOccurrence='{firstOccurrence}' " +
                $"secondOccurrence='{secondOccurrence}' " +
                $"initialSessionRevision='{initialSessionRevision}' " +
                $"joinedSessionRevision='{joinedSessionRevision}' " +
                $"finalSessionRevision='{finalSession.Revision}' " +
                $"joined='{CountJoined(finalSession)}' " +
                $"playerInputs='{authoring.PlayerInputManager.playerCount}' " +
                $"hostCount='{finalProjection.HostCount}' " +
                $"projectedSlots='{finalProjection.SlotCount}' " +
                "proof='AllJoinedOccurrenceFreeze,LateJoinDoesNotExpandActiveOccurrence,ReleaseUsesFrozenProjection,ReentryRecapturesSession,ReadOnlyIdempotency' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static bool MatchesFrozenEmptyOccurrence(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            ActivityAsset activity,
            int expectedSessionRevision,
            int expectedHostCount,
            int expectedOccurrence)
        {
            return snapshot != null &&
                snapshot.IsAvailable &&
                snapshot.IsReady &&
                activity != null &&
                string.Equals(
                    snapshot.ActivityName,
                    activity.ActivityName,
                    StringComparison.Ordinal) &&
                snapshot.ActivityOccurrence > 0 &&
                (expectedOccurrence <= 0 ||
                 snapshot.ActivityOccurrence == expectedOccurrence) &&
                string.Equals(
                    snapshot.EntryPolicy,
                    PlayerParticipationRequirementLevel
                        .JoinedSlots.ToString(),
                    StringComparison.Ordinal) &&
                snapshot.SessionRevision ==
                    expectedSessionRevision &&
                snapshot.HostCount == expectedHostCount &&
                snapshot.SlotCount == 0 &&
                snapshot.Slots.Count == 0;
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

        private static bool ProjectsOnlyJoinedHost(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            PlayerSlotProfile expectedSlot)
        {
            return ProjectsOnly(snapshot, expectedSlot) &&
                snapshot.Slots[0].HasTechnicalHost;
        }

        private static async Task<PlayerParticipationSnapshot>
            AwaitSessionAsync(
                LocalPlayerProvisioningAuthoring authoring,
                Func<PlayerParticipationSnapshot, bool> predicate,
                string failure,
                int frameBudget)
        {
            Require(authoring != null && predicate != null,
                "Session wait requires authoring and predicate.");
            Require(frameBudget > 0,
                "Session wait frame budget must be positive.");

            PlayerParticipationSnapshot latest = null;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                latest = authoring.RuntimeSnapshot;
                if (latest != null && predicate(latest))
                {
                    return latest;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"{failure}. revision='{(latest != null ? latest.Revision : -1)}' " +
                $"joined='{(latest != null ? CountJoined(latest) : -1)}'.");
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
            LocalPlayerHostAuthoring host,
            ManagerProvisionedPlayerLifecycleSnapshot baseline)
        {
            Require(authoring != null && baseline != null,
                "Idempotency proof requires authoring and baseline snapshot.");

            int sessionRevision =
                authoring.RuntimeSnapshot.Revision;
            int actorCount = host != null
                ? CountActors(host)
                : 0;
            string expected = Fingerprint(baseline);

            ManagerProvisionedPlayerLifecycleSnapshot sameFrame =
                authoring.ManagerProvisionedLifecycleSnapshot;
            ManagerProvisionedPlayerLifecycleSnapshot sameFrameAgain =
                authoring.ManagerProvisionedLifecycleSnapshot;
            await Awaitable.NextFrameAsync();
            ManagerProvisionedPlayerLifecycleSnapshot nextFrame =
                authoring.ManagerProvisionedLifecycleSnapshot;

            string sameFrameFingerprint = Fingerprint(sameFrame);
            string sameFrameAgainFingerprint =
                Fingerprint(sameFrameAgain);
            string nextFrameFingerprint = Fingerprint(nextFrame);
            int currentSessionRevision =
                authoring.RuntimeSnapshot.Revision;
            int currentActorCount = host != null
                ? CountActors(host)
                : 0;

            Require(string.Equals(
                    expected,
                    sameFrameFingerprint,
                    StringComparison.Ordinal) &&
                string.Equals(
                    expected,
                    sameFrameAgainFingerprint,
                    StringComparison.Ordinal) &&
                string.Equals(
                    expected,
                    nextFrameFingerprint,
                    StringComparison.Ordinal) &&
                currentSessionRevision == sessionRevision &&
                (host == null ||
                 currentActorCount == actorCount),
                "Repeated public lifecycle reads changed projection, Session revision or Actor materialization. " +
                $"expected='{expected}' " +
                $"sameFrame='{sameFrameFingerprint}' " +
                $"sameFrameAgain='{sameFrameAgainFingerprint}' " +
                $"nextFrame='{nextFrameFingerprint}' " +
                $"sessionRevision='{sessionRevision}->{currentSessionRevision}' " +
                $"actorCount='{actorCount}->{currentActorCount}'.");
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

        private static void ConfigureAllJoinedProjection(
            ActivityAsset activity)
        {
            Require(activity != null,
                "Player projection configuration requires an Activity.");

            var serialized = new SerializedObject(activity);
            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationProjectionMode"),
                ActivityParticipationProjectionMode
                    .AllJoinedSlots.ToString());
            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationZeroParticipantPolicy"),
                ActivityParticipationZeroParticipantPolicy
                    .Allowed.ToString());
            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationRequirementLevel"),
                PlayerParticipationRequirementLevel
                    .JoinedSlots.ToString());

            SerializedProperty explicitSlots = RequireProperty(
                serialized,
                "playerParticipationExplicitSlotProfiles");
            explicitSlots.arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(activity.PlayerParticipationProjectionMode ==
                    ActivityParticipationProjectionMode
                        .AllJoinedSlots &&
                activity.PlayerParticipationRequirementLevel ==
                    PlayerParticipationRequirementLevel.JoinedSlots,
                "Runtime Activity AllJoinedSlots projection did not apply.");
        }

        private static PlayerSlotProfile
            ResolveFirstConfiguredSlotProfile()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            GameApplicationAsset application =
                settings != null
                    ? settings.ActiveGameApplication
                    : null;
            PlayerSlotProfile slotProfile = null;
            bool resolved = application != null &&
                QaPlayerSessionQaSupport.TryGetSupportedSlot(
                    application,
                    0,
                    out slotProfile);
            Require(resolved &&
                slotProfile != null &&
                slotProfile.PlayerSlotId.IsValid,
                "Could not resolve the first configured local Player Slot Profile.");
            return slotProfile;
        }

        private static LocalPlayerProvisioningAuthoring
            ResolveProvisioningAuthoring(FrameworkRuntimeHost host)
        {
            bool resolved =
                QaPlayerRuntimeObservationBridge
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
                    "Player Activity Readiness");
                ActivityReadinessParticipant participant =
                    child != null
                        ? child.GetComponent<
                            ActivityReadinessParticipant>()
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

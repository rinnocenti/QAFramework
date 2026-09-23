using System;
using System.Threading.Tasks;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-M07-12B-7 Play Mode regression.
    ///
    /// Proves the zero-participant contract across all Activity participation
    /// projection modes:
    /// - NoSlots + None is a valid zero-Player Activity;
    /// - NoSlots cannot satisfy JoinedSlots or a stronger requirement;
    /// - AllJoinedSlots honors Allowed/Rejected when the Session has no joined Slots;
    /// - ExplicitSlots requires at least one valid authored PlayerSlotProfile;
    /// - zero-participant policy does not legalize invalid ExplicitSlots authoring;
    /// - accepted zero-Player occurrences expose a stable public Ready projection;
    /// - Activity exit exposes Released without mutating Session Player state.
    /// </summary>
    public static class QaM07PlayerZeroParticipantPolicyMatrixRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Participation/Run Zero Participant Policy Matrix";
        private const string Prefix =
            "[QA_IF_M07_12B_7_PLAYER_ZERO_PARTICIPANT_POLICY_MATRIX]";
        private const int FrameBudget = 300;
        private const int ExpectedCaseCount = 36;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "official-host-resolved",
            "provisioning-authoring-resolved",
            "session-fixture-confirmed",
            "fresh-session-confirmed",

            "no-slots-allowed-configured",
            "no-slots-allowed-request-started",
            "no-slots-allowed-public-ready",
            "no-slots-allowed-public-idempotent",
            "no-slots-allowed-request-completed",
            "no-slots-allowed-cleared",
            "no-slots-allowed-fixture-cleaned",

            "all-joined-allowed-configured",
            "all-joined-allowed-request-started",
            "all-joined-allowed-public-ready",
            "all-joined-allowed-public-idempotent",
            "all-joined-allowed-request-completed",
            "all-joined-allowed-cleared",
            "all-joined-allowed-fixture-cleaned",

            "explicit-empty-allowed-configured",
            "explicit-empty-allowed-request-started",
            "explicit-empty-allowed-rejected",
            "explicit-empty-allowed-fixture-cleaned",

            "no-slots-required-configured",
            "no-slots-required-request-started",
            "no-slots-required-rejected",
            "no-slots-required-fixture-cleaned",

            "all-joined-zero-configured",
            "all-joined-zero-request-started",
            "all-joined-zero-rejected",
            "all-joined-zero-fixture-cleaned",

            "explicit-empty-configured",
            "explicit-empty-request-started",
            "explicit-empty-rejected",
            "explicit-empty-fixture-cleaned"
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
            int initialSessionRevision = 0;

            try
            {
                Require(EditorApplication.isPlaying,
                    "IF-M07-12B-7 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaM07InternalReconcileSetup
                    .RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(host != null && host.State.GameFlowStarted,
                    "IF-M07-12B-7 requires the official started FrameworkRuntimeHost.");
                cases.Complete("official-host-resolved");

                authoring = ResolveProvisioningAuthoring(host);
                Require(authoring != null && authoring.RuntimeReady,
                    "IF-M07-12B-7 could not resolve ready Local Player provisioning authoring.");
                cases.Complete("provisioning-authoring-resolved");

                PlayerParticipationSnapshot initialSession =
                    authoring.RuntimeSnapshot;
                Require(initialSession != null &&
                    initialSession.IsInitialized &&
                    initialSession.Slots.Count > 0 &&
                    authoring.PlayerInputManager != null,
                    "IF-M07-12B-7 requires one initialized Session Player fixture.");
                initialSessionRevision = initialSession.Revision;
                cases.Complete("session-fixture-confirmed");

                Require(CountJoined(initialSession) == 0 &&
                    authoring.PlayerInputManager.playerCount == 0,
                    "IF-M07-12B-7 is one-shot. Enter a fresh Play Mode with no joined Players.");
                cases.Complete("fresh-session-confirmed");

                await RunAcceptedScenarioAsync(
                    cases,
                    authoring,
                    "no-slots-allowed",
                    "qa.m07.12b7.zero.no-slots-allowed",
                    "Q3 M07 Zero No Slots Allowed",
                    ActivityParticipationProjectionMode.NoSlots,
                    ActivityParticipationZeroParticipantPolicy.Allowed,
                    PlayerParticipationRequirementLevel.None,
                    initialSessionRevision);

                await RunAcceptedScenarioAsync(
                    cases,
                    authoring,
                    "all-joined-allowed",
                    "qa.m07.12b7.zero.all-joined-allowed",
                    "Q3 M07 Zero All Joined Allowed",
                    ActivityParticipationProjectionMode.AllJoinedSlots,
                    ActivityParticipationZeroParticipantPolicy.Allowed,
                    PlayerParticipationRequirementLevel.JoinedSlots,
                    initialSessionRevision);

                await RunRejectedScenarioAsync(
                    cases,
                    authoring,
                    "explicit-empty-allowed",
                    "qa.m07.12b7.zero.explicit-empty-allowed",
                    "Q3 M07 Zero Explicit Empty Allowed",
                    ActivityParticipationProjectionMode.ExplicitSlots,
                    ActivityParticipationZeroParticipantPolicy.Allowed,
                    PlayerParticipationRequirementLevel.JoinedSlots,
                    "uses ExplicitSlots but has no PlayerSlotProfile references",
                    initialSessionRevision);

                await RunRejectedScenarioAsync(
                    cases,
                    authoring,
                    "no-slots-required",
                    "qa.m07.12b7.zero.no-slots-required",
                    "Q3 M07 Zero No Slots Required",
                    ActivityParticipationProjectionMode.NoSlots,
                    ActivityParticipationZeroParticipantPolicy.Allowed,
                    PlayerParticipationRequirementLevel.JoinedSlots,
                    "projects no Slots but requires 'JoinedSlots'",
                    initialSessionRevision);

                await RunRejectedScenarioAsync(
                    cases,
                    authoring,
                    "all-joined-zero",
                    "qa.m07.12b7.zero.all-joined-rejected",
                    "Q3 M07 Zero All Joined Rejected",
                    ActivityParticipationProjectionMode.AllJoinedSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected,
                    PlayerParticipationRequirementLevel.JoinedSlots,
                    "rejects zero projected participants",
                    initialSessionRevision);

                await RunRejectedScenarioAsync(
                    cases,
                    authoring,
                    "explicit-empty",
                    "qa.m07.12b7.zero.explicit-empty-rejected",
                    "Q3 M07 Zero Explicit Empty Rejected",
                    ActivityParticipationProjectionMode.ExplicitSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected,
                    PlayerParticipationRequirementLevel.JoinedSlots,
                    "uses ExplicitSlots but has no PlayerSlotProfile references",
                    initialSessionRevision);
            }
            catch (Exception exception)
            {
                failures.Add("execution", exception);
            }

            if (failures.HasFailures)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' " +
                    $"cases='{cases.Count}/{cases.ExpectedCount}' " +
                    $"next='{cases.NextExpectedOrNone()}' " +
                    $"completed='{cases.DescribeCompleted()}' " +
                    $"missing='{cases.DescribeMissing()}' " +
                    $"execution='{Escape(failures.Describe("execution"))}'.");
                throw failures.ToAggregate(
                    "IF-M07-12B-7 Player zero-participant policy matrix regression failed.");
            }

            Require(cases.Count == cases.ExpectedCount,
                $"Case registry is incomplete. completed='{cases.DescribeCompleted()}' missing='{cases.DescribeMissing()}'.");
            PlayerParticipationSnapshot finalSession =
                authoring.RuntimeSnapshot;
            ManagerProvisionedPlayerLifecycleSnapshot finalProjection =
                authoring.ManagerProvisionedLifecycleSnapshot;
            Debug.Log(
                $"{Prefix} status='Passed' " +
                $"cases='{cases.Count}' " +
                $"sessionRevision='{finalSession.Revision}' " +
                $"joined='{CountJoined(finalSession)}' " +
                $"playerInputs='{authoring.PlayerInputManager.playerCount}' " +
                $"publicStatus='{(finalProjection != null ? finalProjection.Status.ToString() : "<none>")}' " +
                "proof='NoSlotsAllowed,NoSlotsRequirementRejected,AllJoinedAllowed,AllJoinedRejected,ExplicitEmptyInvalidRegardlessOfZeroPolicy,PublicProjectionIdempotent,ActivityReleasePreservesSession' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static async Task RunAcceptedScenarioAsync(
            QaCaseRegistry cases,
            LocalPlayerProvisioningAuthoring authoring,
            string casePrefix,
            string activityId,
            string activityName,
            ActivityParticipationProjectionMode projectionMode,
            ActivityParticipationZeroParticipantPolicy zeroPolicy,
            PlayerParticipationRequirementLevel requirement,
            int expectedSessionRevision)
        {
            QaActivityEntryReadinessFixture fixture = null;
            var owned =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    $"qa-if-m07-12b-7-{casePrefix}");
            Exception failure = null;

            try
            {
                fixture =
                    await QaActivityEntryReadinessFixture.CreateAsync();
                fixture.ExpectParticipantPreparationCycles(1);
                ActivityAsset activity = fixture.CreateActivity(
                    activityId,
                    activityName,
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup.ContentScenePath);

                ConfigureProjection(
                    activity,
                    projectionMode,
                    zeroPolicy,
                    requirement);
                cases.Complete($"{casePrefix}-configured");

                RequireFreshSession(
                    authoring,
                    expectedSessionRevision,
                    $"{casePrefix}:before-request");

                owned.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07PlayerZeroParticipantPolicyMatrixRegression),
                        $"qa-if-m07-12b-7-{casePrefix}"));
                cases.Complete($"{casePrefix}-request-started");

                await AwaitParticipantCycleOrTerminalAsync(
                    fixture,
                    owned,
                    1,
                    FrameBudget);

                ManagerProvisionedPlayerLifecycleSnapshot ready =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesZeroParticipantReady(
                                snapshot,
                                activity,
                                requirement,
                                expectedSessionRevision),
                        $"{casePrefix} did not expose a zero-participant Ready projection",
                        FrameBudget);
                cases.Complete($"{casePrefix}-public-ready");

                ManagerProvisionedPlayerLifecycleSnapshot immediate =
                    authoring.ManagerProvisionedLifecycleSnapshot;
                await Awaitable.NextFrameAsync();
                ManagerProvisionedPlayerLifecycleSnapshot nextFrame =
                    authoring.ManagerProvisionedLifecycleSnapshot;
                RequireEquivalentPublicProjection(
                    ready,
                    immediate,
                    $"{casePrefix}:immediate-read");
                RequireEquivalentPublicProjection(
                    ready,
                    nextFrame,
                    $"{casePrefix}:next-frame-read");
                RequireFreshSession(
                    authoring,
                    expectedSessionRevision,
                    $"{casePrefix}:idempotent-read");
                cases.Complete($"{casePrefix}-public-idempotent");

                Require(fixture.Participant != null &&
                    fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing,
                    $"{casePrefix} aggregate readiness participant is not Preparing.");
                fixture.Participant.CompletePreparation();

                FrameworkActivityRequestResult terminal =
                    await AwaitOwnedTerminalAsync(
                        owned,
                        FrameBudget);
                Require(terminal.Succeeded,
                    !string.IsNullOrWhiteSpace(terminal.Message)
                        ? terminal.Message
                        : $"{casePrefix} Activity request did not succeed.");
                Require(fixture.RuntimeHost.State.CurrentActivity != null &&
                    fixture.RuntimeHost.State.CurrentActivity
                        .HasSameIdentity(activity),
                    $"{casePrefix} target Activity did not become authoritative.");
                RequireFreshSession(
                    authoring,
                    expectedSessionRevision,
                    $"{casePrefix}:request-completed");
                cases.Complete($"{casePrefix}-request-completed");

                FrameworkActivityRequestResult clear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07PlayerZeroParticipantPolicyMatrixRegression),
                        $"qa-if-m07-12b-7-clear-{casePrefix}");
                Require(clear.Succeeded,
                    !string.IsNullOrWhiteSpace(clear.Message)
                        ? clear.Message
                        : $"{casePrefix} Activity clear did not succeed.");

                await AwaitSnapshotAsync(
                    authoring,
                    snapshot =>
                        snapshot != null &&
                        snapshot.IsAvailable &&
                        snapshot.IsReleased &&
                        snapshot.SlotCount == 0 &&
                        snapshot.HostCount == 0 &&
                        snapshot.SessionRevision ==
                            expectedSessionRevision,
                    $"{casePrefix} clear did not expose Released with an empty contextual projection",
                    FrameBudget);
                RequireFreshSession(
                    authoring,
                    expectedSessionRevision,
                    $"{casePrefix}:cleared");
                cases.Complete($"{casePrefix}-cleared");
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                try
                {
                    await CleanupScenarioAsync(
                        fixture,
                        owned,
                        $"qa-if-m07-12b-7-cleanup-{casePrefix}");
                }
                catch (Exception cleanupException)
                {
                    failure = Combine(
                        failure,
                        cleanupException,
                        $"{casePrefix} cleanup failed.");
                }
            }

            if (failure != null)
            {
                throw failure;
            }

            cases.Complete($"{casePrefix}-fixture-cleaned");
        }

        private static async Task RunRejectedScenarioAsync(
            QaCaseRegistry cases,
            LocalPlayerProvisioningAuthoring authoring,
            string casePrefix,
            string activityId,
            string activityName,
            ActivityParticipationProjectionMode projectionMode,
            ActivityParticipationZeroParticipantPolicy zeroPolicy,
            PlayerParticipationRequirementLevel requirement,
            string expectedDiagnostic,
            int expectedSessionRevision)
        {
            QaActivityEntryReadinessFixture fixture = null;
            var owned =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    $"qa-if-m07-12b-7-{casePrefix}");
            Exception failure = null;

            try
            {
                fixture =
                    await QaActivityEntryReadinessFixture.CreateAsync();
                fixture.ExpectParticipantPreparationCycles(1);
                ActivityAsset activity = fixture.CreateActivity(
                    activityId,
                    activityName,
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup.ContentScenePath);

                ConfigureProjection(
                    activity,
                    projectionMode,
                    zeroPolicy,
                    requirement);
                cases.Complete($"{casePrefix}-configured");

                RequireFreshSession(
                    authoring,
                    expectedSessionRevision,
                    $"{casePrefix}:before-request");

                owned.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07PlayerZeroParticipantPolicyMatrixRegression),
                        $"qa-if-m07-12b-7-{casePrefix}"));
                cases.Complete($"{casePrefix}-request-started");

                FrameworkActivityRequestResult? terminal =
                    await AwaitTerminalOrUnexpectedPreparationAsync(
                        fixture,
                        owned,
                        FrameBudget);

                if (!terminal.HasValue)
                {
                    Require(fixture.Participant != null &&
                        fixture.Participant.State ==
                            ActivityReadinessParticipantState.Preparing,
                        $"{casePrefix} entered readiness without a Preparing aggregate participant.");
                    fixture.Participant.CompletePreparation();
                    terminal = await AwaitOwnedTerminalAsync(
                        owned,
                        FrameBudget);
                }

                FrameworkActivityRequestResult terminalResult =
                    terminal.Value;
                if (terminalResult.Succeeded)
                {
                    if (fixture.RuntimeHost.State.CurrentActivity != null &&
                        fixture.RuntimeHost.State.CurrentActivity
                            .HasSameIdentity(activity))
                    {
                        FrameworkActivityRequestResult clear =
                            await fixture.Activities.ClearActivityAsync(
                                nameof(
                                    QaM07PlayerZeroParticipantPolicyMatrixRegression),
                                $"qa-if-m07-12b-7-contract-failure-clear-{casePrefix}");
                        Require(clear.Succeeded,
                            !string.IsNullOrWhiteSpace(clear.Message)
                                ? clear.Message
                                : $"{casePrefix} contract-failure clear did not succeed.");
                    }

                    throw new InvalidOperationException(
                        $"{casePrefix} admitted an Activity whose Player projection must be rejected. " +
                        $"Expected diagnostic fragment '{expectedDiagnostic}'. " +
                        $"terminal='{terminalResult.Message}' " +
                        $"public='{authoring.ManagerProvisionedLifecycleSnapshot?.ToDiagnosticString()}'.");
                }

                Require(!terminalResult.Succeeded,
                    $"{casePrefix} expected a terminal rejected Activity request.");
                Require(fixture.PreparationStartedCount == 0 &&
                    fixture.PreparationReleasedCount == 0 &&
                    fixture.Participant.Occurrence == 0,
                    $"{casePrefix} projection rejection occurred after aggregate readiness started. " +
                    $"started='{fixture.PreparationStartedCount}' " +
                    $"released='{fixture.PreparationReleasedCount}' " +
                    $"occurrence='{fixture.Participant.Occurrence}'.");
                Require(MatchesInitialAuthority(fixture),
                    $"{casePrefix} rejection did not preserve the initial Activity authority.");
                string diagnostic =
                    (terminalResult.Message ?? string.Empty) + " " +
                    (authoring.ManagerProvisionedLifecycleSnapshot
                        ?.ToDiagnosticString() ?? string.Empty);
                Require(diagnostic.IndexOf(
                        expectedDiagnostic,
                        StringComparison.Ordinal) >= 0,
                    $"{casePrefix} rejection did not expose the expected diagnostic. " +
                    $"expected='{expectedDiagnostic}' actual='{diagnostic}'.");
                RequireFreshSession(
                    authoring,
                    expectedSessionRevision,
                    $"{casePrefix}:rejected");
                cases.Complete($"{casePrefix}-rejected");
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                try
                {
                    await CleanupScenarioAsync(
                        fixture,
                        owned,
                        $"qa-if-m07-12b-7-cleanup-{casePrefix}");
                }
                catch (Exception cleanupException)
                {
                    failure = Combine(
                        failure,
                        cleanupException,
                        $"{casePrefix} cleanup failed.");
                }
            }

            if (failure != null)
            {
                throw failure;
            }

            cases.Complete($"{casePrefix}-fixture-cleaned");
        }

        private static async Task<FrameworkActivityRequestResult?>
            AwaitTerminalOrUnexpectedPreparationAsync(
                QaActivityEntryReadinessFixture fixture,
                QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
                int frameBudget)
        {
            Require(fixture != null && owned != null &&
                owned.HasOperation,
                "Rejected request wait requires fixture and owned operation.");
            Require(frameBudget > 0,
                "Rejected request frame budget must be positive.");

            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (owned.IsCompleted)
                {
                    return await owned.AwaitTerminalAsync();
                }

                if (fixture.PreparationStartedCount > 0)
                {
                    return null;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "Rejected Activity request neither terminated nor entered aggregate readiness " +
                $"within '{frameBudget}' frames.");
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

        private static bool MatchesZeroParticipantReady(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            ActivityAsset activity,
            PlayerParticipationRequirementLevel requirement,
            int expectedSessionRevision)
        {
            return snapshot != null &&
                snapshot.IsAvailable &&
                snapshot.IsReady &&
                snapshot.Status ==
                    ManagerProvisionedPlayerLifecycleStatus.Ready &&
                activity != null &&
                string.Equals(
                    snapshot.ActivityName,
                    activity.ActivityName,
                    StringComparison.Ordinal) &&
                snapshot.ActivityOccurrence > 0 &&
                string.Equals(
                    snapshot.EntryPolicy,
                    requirement.ToString(),
                    StringComparison.Ordinal) &&
                snapshot.SlotCount == 0 &&
                snapshot.Slots.Count == 0 &&
                snapshot.HostCount == 0 &&
                snapshot.SessionRevision ==
                    expectedSessionRevision;
        }

        private static void RequireEquivalentPublicProjection(
            ManagerProvisionedPlayerLifecycleSnapshot expected,
            ManagerProvisionedPlayerLifecycleSnapshot actual,
            string context)
        {
            Require(expected != null && actual != null,
                $"{context} public projection is missing.");
            Require(expected.Status == actual.Status &&
                string.Equals(
                    expected.ActivityName,
                    actual.ActivityName,
                    StringComparison.Ordinal) &&
                expected.ActivityOccurrence ==
                    actual.ActivityOccurrence &&
                string.Equals(
                    expected.EntryPolicy,
                    actual.EntryPolicy,
                    StringComparison.Ordinal) &&
                expected.SlotCount == actual.SlotCount &&
                expected.Slots.Count == actual.Slots.Count &&
                expected.HostCount == actual.HostCount &&
                expected.SessionRevision ==
                    actual.SessionRevision,
                $"{context} public projection changed during a read-only observation. " +
                $"expected='{expected.ToDiagnosticString()}' " +
                $"actual='{actual.ToDiagnosticString()}'.");
        }

        private static void ConfigureProjection(
            ActivityAsset activity,
            ActivityParticipationProjectionMode projectionMode,
            ActivityParticipationZeroParticipantPolicy zeroPolicy,
            PlayerParticipationRequirementLevel requirement)
        {
            Require(activity != null,
                "Player projection configuration requires an Activity.");

            var serialized = new SerializedObject(activity);
            SerializedProperty modeProperty = RequireProperty(
                serialized,
                "playerParticipationProjectionMode");
            SerializedProperty zeroPolicyProperty = RequireProperty(
                serialized,
                "playerParticipationZeroParticipantPolicy");
            SerializedProperty requirementProperty = RequireProperty(
                serialized,
                "playerParticipationRequirementLevel");
            SerializedProperty explicitSlots = RequireProperty(
                serialized,
                "playerParticipationExplicitSlotProfiles");

            SetEnumName(
                modeProperty,
                projectionMode.ToString());
            SetEnumName(
                zeroPolicyProperty,
                zeroPolicy.ToString());
            SetEnumName(
                requirementProperty,
                requirement.ToString());
            explicitSlots.arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(activity.PlayerParticipationProjectionMode ==
                    projectionMode &&
                activity.PlayerParticipationRequirementLevel ==
                    requirement,
                "Runtime Activity Player projection mode or requirement did not apply.");
            Require(string.Equals(
                    zeroPolicyProperty.enumNames[
                        zeroPolicyProperty.enumValueIndex],
                    zeroPolicy.ToString(),
                    StringComparison.Ordinal),
                "Runtime Activity zero-participant policy did not apply.");
        }

        private static void RequireFreshSession(
            LocalPlayerProvisioningAuthoring authoring,
            int expectedSessionRevision,
            string context)
        {
            Require(authoring != null &&
                authoring.RuntimeReady &&
                authoring.PlayerInputManager != null,
                $"{context} requires ready Local Player provisioning.");
            PlayerParticipationSnapshot session =
                authoring.RuntimeSnapshot;
            Require(session != null &&
                session.IsInitialized &&
                session.Revision == expectedSessionRevision &&
                CountJoined(session) == 0 &&
                authoring.PlayerInputManager.playerCount == 0,
                $"{context} mutated Session Player state. " +
                $"expectedRevision='{expectedSessionRevision}' " +
                $"actualRevision='{session?.Revision}' " +
                $"joined='{(session != null ? CountJoined(session) : -1)}' " +
                $"playerInputs='{authoring.PlayerInputManager.playerCount}'.");
        }

        private static bool MatchesInitialAuthority(
            QaActivityEntryReadinessFixture fixture)
        {
            if (fixture == null)
            {
                return false;
            }

            ActivityAsset current =
                fixture.RuntimeHost.State.CurrentActivity;
            if (fixture.InitialActivity == null)
            {
                return current == null;
            }

            return current != null &&
                current.HasSameIdentity(fixture.InitialActivity);
        }

        private static async Task CleanupScenarioAsync(
            QaActivityEntryReadinessFixture fixture,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
            string reason)
        {
            if (owned != null &&
                owned.HasOperation &&
                !owned.ReachedTerminal)
            {
                await owned.UnwindAsync(
                    () => CompletePendingFixtureReadinessAsync(
                        fixture,
                        reason));
            }

            if (fixture != null)
            {
                await fixture.DisposeAsync();
            }
        }

        private static Task CompletePendingFixtureReadinessAsync(
            QaActivityEntryReadinessFixture fixture,
            string reason)
        {
            if (fixture != null &&
                fixture.Participant != null &&
                fixture.Participant.State ==
                    ActivityReadinessParticipantState.Preparing)
            {
                fixture.Participant.CompletePreparation();
            }

            return Task.CompletedTask;
        }

        private static Exception Combine(
            Exception primary,
            Exception secondary,
            string message)
        {
            if (primary == null)
            {
                return secondary;
            }

            return new AggregateException(
                message,
                primary,
                secondary);
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

using System;
using System.Reflection;
using ImmersiveFrameworkQA.Player;
using ImmersiveFrameworkQA.Player.Internal.Editor;
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
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-M07-12B-6 Play Mode regression.
    ///
    /// Proves the public Manager-Provisioned Player lifecycle boundary for every
    /// PlayerParticipationRequirementLevel using one cumulative Session:
    /// - None requires no Join, Host, selection, Actor or gameplay admission;
    /// - JoinedSlots requires the committed Join/Host boundary only;
    /// - SelectedActors adds default Actor selection without preparation;
    /// - LogicalActorsPrepared adds logical preparation and physical materialization
    ///   without requiring gameplay admission;
    /// - GameplayReady adds gameplay admission over the exact prepared Actor;
    /// - Activity exit releases contextual Actor/gameplay evidence while preserving
    ///   Session-owned Join, Host and selection state.
    /// </summary>
    public static class QaM07PlayerRequirementPolicyMatrixRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Participation/Run Player Requirement Policy Matrix";
        private const string Prefix =
            "[QA_IF_M07_12B_6_PLAYER_REQUIREMENT_POLICY_MATRIX]";
        private const string PlayerReadinessObjectName =
            "Player Activity Readiness";
        private const int FrameBudget = 300;
        private const int ExpectedCaseCount = 38;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "official-host-resolved",
            "provisioning-authoring-resolved",
            "slot-fixture-confirmed",
            "fresh-session-confirmed",
            "fixture-created",

            "none-configured",
            "none-request-started",
            "none-public-ready",
            "none-no-side-effects",
            "none-request-completed",
            "none-cleared",

            "joined-configured",
            "joined-request-started",
            "joined-waiting",
            "joining-opened",
            "public-join-succeeded",
            "joined-ready-no-selection",
            "joined-request-completed",
            "joined-cleared-session-retained",

            "selected-configured",
            "selected-request-started",
            "selected-ready-no-actor",
            "selected-request-completed",
            "selected-cleared-selection-retained",

            "logical-configured",
            "logical-request-started",
            "logical-ready-no-gameplay",
            "logical-request-completed",
            "logical-cleared-context-released-physical-retained",

            "gameplay-configured",
            "gameplay-request-started",
            "gameplay-ready",
            "gameplay-request-completed",
            "gameplay-cleared-session-retained",

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
            LocalPlayerJoinResult join = null;
            bool joiningOpen = false;

            var noneRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-6-none");
            var joinedRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-6-joined");
            var selectedRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-6-selected");
            var logicalRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-6-logical");
            var gameplayRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-6-gameplay");

            int noneOccurrence = 0;
            int joinedOccurrence = 0;
            int selectedOccurrence = 0;
            int logicalOccurrence = 0;
            int gameplayOccurrence = 0;

            try
            {
                Require(EditorApplication.isPlaying,
                    "IF-M07-12B-6 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaM07InternalReconcileSetup
                    .RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(host != null && host.State.GameFlowStarted,
                    "IF-M07-12B-6 requires the official started FrameworkRuntimeHost.");
                cases.Complete("official-host-resolved");

                authoring = ResolveProvisioningAuthoring(host);
                Require(authoring != null && authoring.RuntimeReady,
                    "IF-M07-12B-6 could not resolve ready Local Player provisioning authoring.");
                cases.Complete("provisioning-authoring-resolved");

                ImmersiveFrameworkSettingsAsset settings =
                    Resources.Load<ImmersiveFrameworkSettingsAsset>(
                        ImmersiveFrameworkSettingsAsset.ResourcesPath);
                GameApplicationAsset application =
                    settings != null
                        ? settings.ActiveGameApplication
                        : null;
                PlayerSlotProfile slotProfile = null;
                Require(application != null &&
                    QaPlayerSessionQaSupport.TryGetSupportedSlot(
                        application,
                        0,
                        out slotProfile) &&
                    slotProfile != null &&
                    slotProfile.PlayerSlotId.IsValid &&
                    slotProfile.DefaultActorProfile != null &&
                    slotProfile.DefaultActorProfile.ActorProfileId.IsValid &&
                    slotProfile.DefaultActorProfile.LogicalActorHostPrefab != null,
                    "IF-M07-12B-6 requires one valid Local Player Slot with an explicit default Actor.");
                cases.Complete("slot-fixture-confirmed");

                PlayerParticipationSnapshot initialSession =
                    authoring.RuntimeSnapshot;
                Require(CountJoined(initialSession) == 0 &&
                    authoring.PlayerInputManager != null &&
                    authoring.PlayerInputManager.playerCount == 0,
                    "IF-M07-12B-6 is one-shot. Enter a fresh Play Mode with no joined Players.");
                cases.Complete("fresh-session-confirmed");

                fixture =
                    await QaActivityEntryReadinessFixture.CreateAsync();
                fixture.ExpectParticipantPreparationCycles(5);
                cases.Complete("fixture-created");

                ActivityAsset activity = fixture.CreateActivity(
                    "qa.m07.12b6.player-requirement-policy-matrix",
                    "Q3 M07 Player Requirement Policy Matrix",
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup.ContentScenePath);

                // None -----------------------------------------------------
                await StartStageAsync(
                    cases,
                    fixture,
                    noneRequest,
                    activity,
                    slotProfile,
                    PlayerParticipationRequirementLevel.None,
                    1,
                    "none-configured",
                    "none-request-started",
                    "qa-if-m07-12b-6-none");

                ManagerProvisionedPlayerLifecycleSnapshot noneReady =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesPolicySlot(
                                snapshot,
                                slotProfile,
                                PlayerParticipationRequirementLevel.None,
                                ManagerProvisionedPlayerLifecycleStatus.Ready,
                                hasTechnicalHost: false,
                                hasSelectedActor: false,
                                logicalActorPrepared: false,
                                physicalActorMaterialized: false,
                                gameplayAdmitted: false) &&
                            snapshot.HostCount == 0,
                        "None did not expose Ready without Player-side requirements",
                        FrameBudget);
                noneOccurrence = noneReady.ActivityOccurrence;
                Require(noneOccurrence > 0 &&
                    !noneRequest.IsCompleted &&
                    fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing,
                    "None did not remain independently observable while aggregate readiness was pending.");
                cases.Complete("none-public-ready");

                Require(CountJoined(authoring.RuntimeSnapshot) == 0 &&
                    authoring.PlayerInputManager.playerCount == 0 &&
                    noneReady.SlotCount == 1 &&
                    noneReady.HostCount == 0,
                    "None created Join, Host, selection, Actor or gameplay evidence.");
                cases.Complete("none-no-side-effects");

                await CompleteStageRequestAsync(
                    cases,
                    fixture,
                    noneRequest,
                    "none-request-completed");
                await ClearStageAsync(
                    fixture,
                    authoring,
                    expectedHostCount: 0,
                    reason: "qa-if-m07-12b-6-clear-none");
                Require(CountJoined(authoring.RuntimeSnapshot) == 0,
                    "None clear changed the fresh Session.");
                cases.Complete("none-cleared");

                // JoinedSlots ----------------------------------------------
                await StartStageAsync(
                    cases,
                    fixture,
                    joinedRequest,
                    activity,
                    slotProfile,
                    PlayerParticipationRequirementLevel.JoinedSlots,
                    2,
                    "joined-configured",
                    "joined-request-started",
                    "qa-if-m07-12b-6-joined");

                ManagerProvisionedPlayerLifecycleSnapshot joinedWaiting =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesPolicySlot(
                                snapshot,
                                slotProfile,
                                PlayerParticipationRequirementLevel.JoinedSlots,
                                ManagerProvisionedPlayerLifecycleStatus.WaitingForJoin,
                                hasTechnicalHost: false,
                                hasSelectedActor: false,
                                logicalActorPrepared: false,
                                physicalActorMaterialized: false,
                                gameplayAdmitted: false) &&
                            snapshot.HasGateEvidence &&
                            snapshot.GateHeld &&
                            snapshot.HostCount == 0,
                        "JoinedSlots did not stop exactly at WaitingForJoin",
                        FrameBudget);
                joinedOccurrence = joinedWaiting.ActivityOccurrence;
                Require(joinedOccurrence > noneOccurrence &&
                    !joinedRequest.IsCompleted,
                    "JoinedSlots occurrence did not advance or terminated before Join.");
                cases.Complete("joined-waiting");

                PlayerParticipationOperationResult open =
                    authoring.OpenJoining(
                        nameof(
                            QaM07PlayerRequirementPolicyMatrixRegression),
                        "qa-if-m07-12b-6-open-joining");
                Require(open != null &&
                    open.Completed &&
                    open.Snapshot.JoiningOpen &&
                    authoring.PlayerInputManager.joiningEnabled,
                    open != null
                        ? open.ToDiagnosticString()
                        : "Opening joining returned no result.");
                joiningOpen = true;
                cases.Complete("joining-opened");

                join = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(
                            QaM07PlayerRequirementPolicyMatrixRegression),
                        "qa-if-m07-12b-6-public-join"));
                Require(join != null &&
                    join.Succeeded &&
                    join.HasCommitEvidence &&
                    join.HasAssignmentEvidence &&
                    join.Slot.PlayerSlotId ==
                        slotProfile.PlayerSlotId &&
                    join.LocalPlayerHost != null &&
                    join.PlayerInput != null,
                    join != null
                        ? join.ToDiagnosticString()
                        : "Public Join returned no result.");
                cases.Complete("public-join-succeeded");

                ManagerProvisionedPlayerLifecycleSnapshot joinedReady =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesPolicySlot(
                                snapshot,
                                slotProfile,
                                PlayerParticipationRequirementLevel.JoinedSlots,
                                ManagerProvisionedPlayerLifecycleStatus.Ready,
                                hasTechnicalHost: true,
                                hasSelectedActor: false,
                                logicalActorPrepared: false,
                                physicalActorMaterialized: false,
                                gameplayAdmitted: false) &&
                            !snapshot.GateHeld &&
                            snapshot.HostCount == 1,
                        "JoinedSlots did not become Ready at the committed Join/Host boundary",
                        FrameBudget);
                Require(joinedReady.ActivityOccurrence == joinedOccurrence &&
                    CountJoined(authoring.RuntimeSnapshot) == 1 &&
                    CountActors(join.LocalPlayerHost) == 0 &&
                    !FindSessionSlot(
                        authoring.RuntimeSnapshot,
                        slotProfile.PlayerSlotId)
                        .HasSelectedActor,
                    "JoinedSlots leaked selection or Actor preparation from a higher requirement.");
                cases.Complete("joined-ready-no-selection");

                await CompleteStageRequestAsync(
                    cases,
                    fixture,
                    joinedRequest,
                    "joined-request-completed");
                await ClearStageAsync(
                    fixture,
                    authoring,
                    expectedHostCount: 1,
                    reason: "qa-if-m07-12b-6-clear-joined");
                Require(CountJoined(authoring.RuntimeSnapshot) == 1 &&
                    join.LocalPlayerHost.IsJoined &&
                    CountActors(join.LocalPlayerHost) == 0 &&
                    !FindSessionSlot(
                        authoring.RuntimeSnapshot,
                        slotProfile.PlayerSlotId)
                        .HasSelectedActor,
                    "JoinedSlots clear did not preserve only Session Join/Host evidence.");
                cases.Complete("joined-cleared-session-retained");

                // SelectedActors -------------------------------------------
                await StartStageAsync(
                    cases,
                    fixture,
                    selectedRequest,
                    activity,
                    slotProfile,
                    PlayerParticipationRequirementLevel.SelectedActors,
                    3,
                    "selected-configured",
                    "selected-request-started",
                    "qa-if-m07-12b-6-selected");

                ManagerProvisionedPlayerLifecycleSnapshot selectedReady =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesPolicySlot(
                                snapshot,
                                slotProfile,
                                PlayerParticipationRequirementLevel.SelectedActors,
                                ManagerProvisionedPlayerLifecycleStatus.Ready,
                                hasTechnicalHost: true,
                                hasSelectedActor: true,
                                logicalActorPrepared: false,
                                physicalActorMaterialized: false,
                                gameplayAdmitted: false) &&
                            !snapshot.GateHeld &&
                            snapshot.HostCount == 1,
                        "SelectedActors did not stop exactly after Actor selection",
                        FrameBudget);
                selectedOccurrence = selectedReady.ActivityOccurrence;
                PlayerSlotRuntimeSnapshot selectedSessionSlot =
                    FindSessionSlot(
                        authoring.RuntimeSnapshot,
                        slotProfile.PlayerSlotId);
                Require(selectedOccurrence > joinedOccurrence &&
                    selectedSessionSlot.HasSelectedActor &&
                    selectedSessionSlot.SelectedActorProfileId ==
                        slotProfile.DefaultActorProfile.ActorProfileId &&
                    CountActors(join.LocalPlayerHost) == 0,
                    "SelectedActors failed default selection or leaked Actor materialization.");
                cases.Complete("selected-ready-no-actor");

                await CompleteStageRequestAsync(
                    cases,
                    fixture,
                    selectedRequest,
                    "selected-request-completed");
                await ClearStageAsync(
                    fixture,
                    authoring,
                    expectedHostCount: 1,
                    reason: "qa-if-m07-12b-6-clear-selected");
                Require(CountJoined(authoring.RuntimeSnapshot) == 1 &&
                    FindSessionSlot(
                        authoring.RuntimeSnapshot,
                        slotProfile.PlayerSlotId)
                        .HasSelectedActor &&
                    CountActors(join.LocalPlayerHost) == 0,
                    "SelectedActors clear did not retain Session selection or leaked an Actor.");
                cases.Complete("selected-cleared-selection-retained");

                // LogicalActorsPrepared ------------------------------------
                await StartStageAsync(
                    cases,
                    fixture,
                    logicalRequest,
                    activity,
                    slotProfile,
                    PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared,
                    4,
                    "logical-configured",
                    "logical-request-started",
                    "qa-if-m07-12b-6-logical");

                ManagerProvisionedPlayerLifecycleSnapshot logicalReady =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesPolicySlot(
                                snapshot,
                                slotProfile,
                                PlayerParticipationRequirementLevel
                                    .LogicalActorsPrepared,
                                ManagerProvisionedPlayerLifecycleStatus.Ready,
                                hasTechnicalHost: true,
                                hasSelectedActor: true,
                                logicalActorPrepared: true,
                                physicalActorMaterialized: true,
                                gameplayAdmitted: false) &&
                            !snapshot.GateHeld &&
                            snapshot.HostCount == 1,
                        "LogicalActorsPrepared did not become Ready without gameplay admission",
                        FrameBudget);
                logicalOccurrence = logicalReady.ActivityOccurrence;
                Require(logicalOccurrence > selectedOccurrence &&
                    CountActors(join.LocalPlayerHost) == 1,
                    "LogicalActorsPrepared did not materialize exactly one Actor.");
                cases.Complete("logical-ready-no-gameplay");

                await CompleteStageRequestAsync(
                    cases,
                    fixture,
                    logicalRequest,
                    "logical-request-completed");
                await ClearStageAsync(
                    fixture,
                    authoring,
                    expectedHostCount: 1,
                    reason: "qa-if-m07-12b-6-clear-logical");
                Require(CountJoined(authoring.RuntimeSnapshot) == 1 &&
                    FindSessionSlot(
                        authoring.RuntimeSnapshot,
                        slotProfile.PlayerSlotId)
                        .HasSelectedActor &&
                    CountActors(join.LocalPlayerHost) == 1,
                    "LogicalActorsPrepared clear did not release the Session-owned physical Actor while releasing Activity context.");
                cases.Complete("logical-cleared-context-released-physical-retained");

                // GameplayReady --------------------------------------------
                await StartStageAsync(
                    cases,
                    fixture,
                    gameplayRequest,
                    activity,
                    slotProfile,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    5,
                    "gameplay-configured",
                    "gameplay-request-started",
                    "qa-if-m07-12b-6-gameplay");

                ManagerProvisionedPlayerLifecycleSnapshot gameplayReady =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            MatchesPolicySlot(
                                snapshot,
                                slotProfile,
                                PlayerParticipationRequirementLevel.GameplayReady,
                                ManagerProvisionedPlayerLifecycleStatus.Ready,
                                hasTechnicalHost: true,
                                hasSelectedActor: true,
                                logicalActorPrepared: true,
                                physicalActorMaterialized: true,
                                gameplayAdmitted: true) &&
                            !snapshot.GateHeld &&
                            snapshot.HostCount == 1,
                        "GameplayReady did not add gameplay admission over the prepared Actor",
                        FrameBudget);
                gameplayOccurrence = gameplayReady.ActivityOccurrence;
                Require(gameplayOccurrence > logicalOccurrence &&
                    CountActors(join.LocalPlayerHost) == 1,
                    "GameplayReady did not retain exactly one admitted Actor.");
                cases.Complete("gameplay-ready");

                await CompleteStageRequestAsync(
                    cases,
                    fixture,
                    gameplayRequest,
                    "gameplay-request-completed");
                await ClearStageAsync(
                    fixture,
                    authoring,
                    expectedHostCount: 1,
                    reason: "qa-if-m07-12b-6-clear-gameplay");
                Require(CountJoined(authoring.RuntimeSnapshot) == 1 &&
                    join.LocalPlayerHost.IsJoined &&
                    FindSessionSlot(
                        authoring.RuntimeSnapshot,
                        slotProfile.PlayerSlotId)
                        .HasSelectedActor &&
                    CountActors(join.LocalPlayerHost) == 1,
                    "GameplayReady clear did not retain Activity gameplay context or release the Session-owned physical Actor.");
                cases.Complete("gameplay-cleared-session-retained");

                PlayerParticipationOperationResult close =
                    authoring.CloseJoining(
                        nameof(
                            QaM07PlayerRequirementPolicyMatrixRegression),
                        "qa-if-m07-12b-6-close-joining");
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
                await UnwindIfPendingAsync(
                    gameplayRequest,
                    host,
                    fixture,
                    failures,
                    "gameplay-unwind");
                await UnwindIfPendingAsync(
                    logicalRequest,
                    host,
                    fixture,
                    failures,
                    "logical-unwind");
                await UnwindIfPendingAsync(
                    selectedRequest,
                    host,
                    fixture,
                    failures,
                    "selected-unwind");
                await UnwindIfPendingAsync(
                    joinedRequest,
                    host,
                    fixture,
                    failures,
                    "joined-unwind");
                await UnwindIfPendingAsync(
                    noneRequest,
                    host,
                    fixture,
                    failures,
                    "none-unwind");

                if (joiningOpen && authoring != null)
                {
                    try
                    {
                        PlayerParticipationOperationResult close =
                            authoring.CloseJoining(
                                nameof(
                                    QaM07PlayerRequirementPolicyMatrixRegression),
                                "qa-if-m07-12b-6-finally-close-joining");
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
                    $"noneUnwind='{Escape(failures.Describe("none-unwind"))}' " +
                    $"joinedUnwind='{Escape(failures.Describe("joined-unwind"))}' " +
                    $"selectedUnwind='{Escape(failures.Describe("selected-unwind"))}' " +
                    $"logicalUnwind='{Escape(failures.Describe("logical-unwind"))}' " +
                    $"gameplayUnwind='{Escape(failures.Describe("gameplay-unwind"))}' " +
                    $"joiningCleanup='{Escape(failures.Describe("joining-cleanup"))}' " +
                    $"fixtureCleanup='{Escape(failures.Describe("fixture-cleanup"))}'.");
                throw failures.ToAggregate(
                    "IF-M07-12B-6 Player requirement policy matrix regression failed.");
            }

            ManagerProvisionedPlayerLifecycleSnapshot finalSnapshot =
                authoring.ManagerProvisionedLifecycleSnapshot;
            Debug.Log(
                $"{Prefix} status='Passed' " +
                $"cases='{cases.Count}' " +
                $"occurrences='None:{noneOccurrence},JoinedSlots:{joinedOccurrence},SelectedActors:{selectedOccurrence},LogicalActorsPrepared:{logicalOccurrence},GameplayReady:{gameplayOccurrence}' " +
                $"sessionRevision='{finalSnapshot.SessionRevision}' " +
                $"hostCount='{finalSnapshot.HostCount}' " +
                $"joined='{CountJoined(authoring.RuntimeSnapshot)}' " +
                "proof='NoneNoSideEffects,JoinedHostBoundary,SelectionBoundary,LogicalPhysicalBoundary,GameplayAdmissionBoundary,ActivityReleasePreservesSession' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static async Task StartStageAsync(
            QaCaseRegistry cases,
            QaActivityEntryReadinessFixture fixture,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
            ActivityAsset activity,
            PlayerSlotProfile slot,
            PlayerParticipationRequirementLevel requirement,
            int expectedPreparationCount,
            string configuredCase,
            string startedCase,
            string reason)
        {
            ConfigureExplicitPlayerProjection(
                activity,
                requirement,
                slot);
            cases.Complete(configuredCase);

            owned.Attach(
                fixture.Activities.RequestActivityAsync(
                    activity,
                    nameof(
                        QaM07PlayerRequirementPolicyMatrixRegression),
                    reason));
            cases.Complete(startedCase);

            await AwaitParticipantCycleOrTerminalAsync(
                fixture,
                owned,
                expectedPreparationCount,
                FrameBudget);
        }

        private static async Task CompleteStageRequestAsync(
            QaCaseRegistry cases,
            QaActivityEntryReadinessFixture fixture,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
            string completedCase)
        {
            Require(fixture != null &&
                fixture.Participant != null &&
                fixture.Participant.State ==
                    ActivityReadinessParticipantState.Preparing,
                "Stage aggregate readiness participant is not Preparing.");
            fixture.Participant.CompletePreparation();

            FrameworkActivityRequestResult terminal =
                await AwaitOwnedTerminalAsync(
                    owned,
                    FrameBudget);
            Require(terminal.Succeeded,
                string.IsNullOrWhiteSpace(terminal.Message)
                    ? "Activity request did not succeed."
                    : terminal.Message);
            cases.Complete(completedCase);
        }

        private static async Task<ManagerProvisionedPlayerLifecycleSnapshot>
            ClearStageAsync(
                QaActivityEntryReadinessFixture fixture,
                LocalPlayerProvisioningAuthoring authoring,
                int expectedHostCount,
                string reason)
        {
            FrameworkActivityRequestResult clear =
                await fixture.Activities.ClearActivityAsync(
                    nameof(
                        QaM07PlayerRequirementPolicyMatrixRegression),
                    reason);
            Require(clear.Succeeded,
                string.IsNullOrWhiteSpace(clear.Message)
                    ? "Clearing Activity did not succeed."
                    : clear.Message);

            return await AwaitSnapshotAsync(
                authoring,
                snapshot =>
                    snapshot.IsAvailable &&
                    snapshot.IsReleased &&
                    snapshot.SlotCount == 0 &&
                    snapshot.HostCount == expectedHostCount &&
                    snapshot.SessionRevision ==
                        authoring.RuntimeSnapshot.Revision,
                "Activity clear did not expose Released with an empty contextual projection",
                FrameBudget);
        }

        private static async Task UnwindIfPendingAsync(
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
            FrameworkRuntimeHost host,
            QaActivityEntryReadinessFixture fixture,
            QaFailureCollector failures,
            string failureKey)
        {
            if (owned == null ||
                !owned.HasOperation ||
                owned.ReachedTerminal)
            {
                return;
            }

            try
            {
                await owned.UnwindAsync(
                    () => FailPendingReadinessAsync(
                        host,
                        fixture,
                        failureKey));
            }
            catch (Exception exception)
            {
                failures.Add(failureKey, exception);
            }
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

        private static bool MatchesPolicySlot(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            PlayerSlotProfile expectedSlot,
            PlayerParticipationRequirementLevel requirement,
            ManagerProvisionedPlayerLifecycleStatus status,
            bool hasTechnicalHost,
            bool hasSelectedActor,
            bool logicalActorPrepared,
            bool physicalActorMaterialized,
            bool gameplayAdmitted)
        {
            if (snapshot == null ||
                !snapshot.IsAvailable ||
                expectedSlot == null ||
                !expectedSlot.PlayerSlotId.IsValid ||
                snapshot.Status != status ||
                !string.Equals(
                    snapshot.EntryPolicy,
                    requirement.ToString(),
                    StringComparison.Ordinal) ||
                snapshot.SlotCount != 1 ||
                snapshot.Slots.Count != 1)
            {
                return false;
            }

            ManagerProvisionedPlayerLifecycleSlotSnapshot slot =
                snapshot.Slots[0];
            return string.Equals(
                    slot.PlayerSlotId,
                    expectedSlot.PlayerSlotId.StableText,
                    StringComparison.Ordinal) &&
                slot.HasTechnicalHost == hasTechnicalHost &&
                slot.HasSelectedActor == hasSelectedActor &&
                slot.LogicalActorPrepared == logicalActorPrepared &&
                slot.PhysicalActorMaterialized ==
                    physicalActorMaterialized &&
                slot.GameplayAdmitted == gameplayAdmitted;
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

        private static PlayerSlotRuntimeSnapshot FindSessionSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            Require(snapshot != null && playerSlotId.IsValid,
                "Session Slot lookup requires a valid snapshot and Slot identity.");

            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                PlayerSlotRuntimeSnapshot slot =
                    snapshot.Slots[index];
                if (slot.PlayerSlotId == playerSlotId)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException(
                $"Session Slot '{playerSlotId.StableText}' was not found.");
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
            LocalPlayerHostAuthoring localPlayerHost)
        {
            Require(localPlayerHost != null &&
                localPlayerHost.ActorMount != null,
                "Actor count requires a Local Player Host with ActorMount.");
            return localPlayerHost.ActorMount.GetComponentsInChildren<
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

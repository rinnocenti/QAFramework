using System;
using ImmersiveFrameworkQA.Player;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Q3 — QA-M07-INTERNAL.
    /// Proves occurrence-scoped Player reconcile authority through the real host while
    /// retaining internal access only for operations that have no public consumer surface.
    /// </summary>
    public static class QaM07InternalReconcileRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Participation/Run Reconcile Authority";
        private const string Prefix = "[QA_M07_INTERNAL]";
        private const string PlayerReadinessObjectName =
            "Player Activity Readiness";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";
        private const string GameplayModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayRuntimeHostModule";
        private const int ExpectedCaseCount = 54;

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "q3-setup-confirmed",
            "official-host-resolved",
            "provisioning-authoring-resolved",
            "two-slot-fixture-confirmed",
            "preparation-module-resolved",
            "gameplay-module-resolved",
            "one-shot-player-state-confirmed",

            "waiting-fixture-created",
            "waiting-activity-configured",
            "waiting-entry-succeeded",
            "waiting-lifecycle-preparing",
            "waiting-player-contribution-preparing",
            "waiting-exit-succeeded",
            "waiting-exit-released-contribution",
            "waiting-exit-preserved-session",

            "rollback-fixture-created",
            "rollback-activity-configured",
            "rollback-request-started",
            "rollback-participants-preparing",
            "joining-opened",
            "first-player-joined",
            "invalid-default-installed",
            "rollback-reconcile-failed-preparation",
            "rollback-delta-reverted",
            "rollback-request-terminal-not-ready",
            "rollback-activity-cleared",
            "rollback-fixture-cleaned",

            "main-fixture-created",
            "main-activity-configured",
            "main-request-started",
            "main-participants-preparing",
            "exact-owner-rejections-proved",
            "pre-delta-no-change-proved",
            "first-slot-progressed",
            "revision-coalescing-proved",
            "replacement-selection-applied",
            "replacement-reconcile-proved",
            "second-player-joined",
            "main-reconcile-completed",
            "main-request-succeeded",
            "one-actor-per-slot-proved",
            "completed-no-change-proved",
            "ready-exit-succeeded",
            "ready-exit-released-context",
            "session-authority-preserved",

            "reentry-fixture-created",
            "reentry-first-request-succeeded",
            "reentry-first-exit-succeeded",
            "reentry-second-request-succeeded",
            "reentry-occurrence-advanced",
            "reentry-actors-renewed",
            "reentry-cleared",
            "joining-closed"
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
            var cases = new QaCaseRegistry(
                ExpectedCases,
                ExpectedCaseCount);
            var failures = new QaFailureCollector();

            FrameworkRuntimeHost host = null;
            LocalPlayerProvisioningAuthoring authoring = null;
            object playerInputManager = null;
            GameApplicationAsset application = null;
            PlayerSlotProfile firstSlotProfile = null;
            PlayerSlotProfile secondSlotProfile = null;
            ActorProfile firstDefaultActor = null;
            ActorProfile replacementActor = null;
            object preparationModule = null;
            object gameplayModule = null;
            bool joiningOpen = false;
            ActorProfile transientInvalidActor = null;
            GameObject transientInvalidLogicalHost = null;
            FieldInfo defaultActorField = null;
            bool firstDefaultOverridden = false;

            try
            {
                Require(EditorApplication.isPlaying,
                    "Q3 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaM07InternalReconcileSetup
                    .RequirePreparedForCurrentPlayMode();
                cases.Complete("q3-setup-confirmed");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(host != null && host.State.GameFlowStarted,
                    "Q3 requires the official started FrameworkRuntimeHost.");
                cases.Complete("official-host-resolved");

                authoring = ResolveProvisioningAuthoring(host);
                Require(authoring != null && authoring.RuntimeReady,
                    "Q3 could not resolve ready Local Player provisioning authoring.");
                playerInputManager =
                    ResolvePlayerInputManager(authoring);
                Require(playerInputManager is Component,
                    "Q3 provisioning authoring has no Component-backed PlayerInputManager.");
                cases.Complete("provisioning-authoring-resolved");

                ImmersiveFrameworkSettingsAsset settings =
                    Resources.Load<ImmersiveFrameworkSettingsAsset>(
                        ImmersiveFrameworkSettingsAsset.ResourcesPath);
                application = settings != null
                    ? settings.ActiveGameApplication
                    : null;
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
                    secondSlotProfile != null,
                    "Q3 requires two configured Local Player Slots.");
                firstDefaultActor = firstSlotProfile.DefaultActorProfile;
                replacementActor =
                    QaM07InternalReconcileSetup
                        .ResolveReplacementActor();
                Require(firstDefaultActor != null &&
                    firstDefaultActor.LogicalActorHostPrefab != null &&
                    secondSlotProfile.DefaultActorProfile != null &&
                    secondSlotProfile.DefaultActorProfile
                        .LogicalActorHostPrefab != null &&
                    replacementActor != null &&
                    replacementActor.LogicalActorHostPrefab != null,
                    "Q3 Actor fixtures are incomplete.");
                cases.Complete("two-slot-fixture-confirmed");

                preparationModule = ResolveHostComponent(
                    host,
                    PreparationModuleTypeName);
                Require(preparationModule != null,
                    "Q3 could not resolve Player Actor preparation module.");
                cases.Complete("preparation-module-resolved");

                gameplayModule = ResolveHostComponent(
                    host,
                    GameplayModuleTypeName);
                Require(gameplayModule != null,
                    "Q3 could not resolve Player gameplay module.");
                cases.Complete("gameplay-module-resolved");

                Require(ReadInt(playerInputManager, "playerCount") == 0 &&
                    CountJoined(authoring.RuntimeSnapshot) == 0,
                    "Q3 is one-shot. Enter a fresh Play Mode with no joined Players.");
                cases.Complete("one-shot-player-state-confirmed");

                await RunExitWhileWaitingAsync(
                    cases,
                    host,
                    authoring,
                    preparationModule);
                await RunRollbackAsync(
                    cases,
                    host,
                    authoring,
                    preparationModule,
                    firstSlotProfile,
                    firstDefaultActor,
                    () =>
                    {
                        transientInvalidActor =
                            CreateInvalidActorClone(
                                firstDefaultActor,
                                out transientInvalidLogicalHost);
                        defaultActorField =
                            ResolveDefaultActorField();
                        defaultActorField.SetValue(
                            firstSlotProfile,
                            transientInvalidActor);
                        firstDefaultOverridden = true;
                    },
                    () =>
                    {
                        if (firstDefaultOverridden &&
                            defaultActorField != null)
                        {
                            defaultActorField.SetValue(
                                firstSlotProfile,
                                firstDefaultActor);
                            firstDefaultOverridden = false;
                        }
                    },
                    () => transientInvalidLogicalHost,
                    value => joiningOpen = value);
                await RunMainReconcileAsync(
                    cases,
                    host,
                    authoring,
                    playerInputManager,
                    preparationModule,
                    gameplayModule,
                    firstSlotProfile,
                    secondSlotProfile,
                    replacementActor,
                    value => joiningOpen = value);
                await RunReentryAsync(
                    cases,
                    host,
                    authoring,
                    preparationModule,
                    firstSlotProfile,
                    secondSlotProfile);

                PlayerParticipationOperationResult close =
                    InvokeReference<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryCloseJoining",
                        nameof(QaM07InternalReconcileRegression),
                        "qa-m07-internal-complete");
                Require(close != null &&
                    close.Completed &&
                    !close.Snapshot.JoiningOpen &&
                    !ReadBool(playerInputManager, "joiningEnabled"),
                    "Q3 could not close joining after completion.");
                joiningOpen = false;
                cases.Complete("joining-closed");

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
                if (firstDefaultOverridden &&
                    defaultActorField != null &&
                    firstSlotProfile != null)
                {
                    try
                    {
                        defaultActorField.SetValue(
                            firstSlotProfile,
                            firstDefaultActor);
                        firstDefaultOverridden = false;
                    }
                    catch (Exception exception)
                    {
                        failures.Add(
                            "default-actor-restoration",
                            exception);
                    }
                }

                if (transientInvalidActor != null)
                {
                    UnityEngine.Object.Destroy(
                        transientInvalidActor);
                }

                if (transientInvalidLogicalHost != null)
                {
                    UnityEngine.Object.Destroy(
                        transientInvalidLogicalHost);
                }

                if (joiningOpen &&
                    preparationModule != null)
                {
                    try
                    {
                        PlayerParticipationOperationResult close =
                            InvokeReference<
                                PlayerParticipationOperationResult>(
                                preparationModule,
                                "TryCloseJoining",
                                nameof(
                                    QaM07InternalReconcileRegression),
                                "qa-m07-internal-finally-close-joining");
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
                        failures.Add(
                            "joining-cleanup",
                            exception);
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
                    $"cleanup='{Escape(failures.Describe("joining-cleanup"))}'.");
                throw failures.ToAggregate(
                    "Q3 — QA-M07-INTERNAL failed.");
            }

            Debug.Log(
                $"{Prefix} status='Passed' " +
                $"cases='{cases.Count}' " +
                $"players='{ReadInt(playerInputManager, "playerCount")}' " +
                $"joined='{CountJoined(authoring.RuntimeSnapshot)}' " +
                "proof='Owner,RevisionCoalescing,OneActorPerSlot,DeltaRollback,ExitWaiting,ExitReady,Replacement,Reentry' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static async Task RunExitWhileWaitingAsync(
            QaCaseRegistry cases,
            FrameworkRuntimeHost host,
            LocalPlayerProvisioningAuthoring authoring,
            object preparationModule)
        {
            QaActivityEntryReadinessFixture fixture = null;
            try
            {
                fixture =
                    await QaActivityEntryReadinessFixture
                        .CreateAsync();
                cases.Complete("waiting-fixture-created");

                ActivityAsset activity = fixture.CreateActivity(
                    "qa.m07.internal.waiting-exit",
                    "Q3 M07 Waiting Exit",
                    ActivityEntryReadinessPolicy.ObserveOnly,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode
                        .InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup
                        .ContentScenePath);
                ConfigurePlayerParticipation(
                    activity,
                    PlayerParticipationRequirementLevel
                        .JoinedSlots,
                    ResolveSlotProfiles(
                        authoring,
                        1));
                cases.Complete("waiting-activity-configured");

                FrameworkActivityRequestResult request =
                    await fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-exit-while-waiting");
                Require(
                    request.Succeeded,
                    request.Message);
                cases.Complete("waiting-entry-succeeded");

                ActivityPlayerActorLifecycleSnapshot lifecycle =
                    GetLifecycleSnapshot(preparationModule);
                Require(lifecycle.Status ==
                        ActivityPlayerActorLifecycleStatus
                            .SucceededEnteredPreparing &&
                    lifecycle.ProjectedSlotCount == 1 &&
                    lifecycle.PreparedCount == 0,
                    "Waiting Activity did not enter as Player Preparing.");
                cases.Complete("waiting-lifecycle-preparing");

                ActivityReadinessParticipant playerReadiness =
                    ResolvePlayerReadinessParticipant(host);
                Require(playerReadiness.State ==
                        ActivityReadinessParticipantState
                            .Preparing &&
                    playerReadiness.Occurrence > 0,
                    "Waiting Activity Player contribution is not Preparing.");
                cases.Complete(
                    "waiting-player-contribution-preparing");

                FrameworkActivityRequestResult clear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-exit-while-waiting-clear");
                Require(
                    clear.Succeeded,
                    clear.Message);
                cases.Complete("waiting-exit-succeeded");

                lifecycle = GetLifecycleSnapshot(
                    preparationModule);
                Require(lifecycle.Status ==
                        ActivityPlayerActorLifecycleStatus
                            .SucceededExitedNoActors &&
                    playerReadiness.State ==
                        ActivityReadinessParticipantState.Released,
                    "Exit while Waiting did not release Player readiness without Actors.");
                cases.Complete(
                    "waiting-exit-released-contribution");

                Require(CountJoined(authoring.RuntimeSnapshot) == 0 &&
                    GetPreparationSnapshot(preparationModule)
                        .Preparation.PreparedCount == 0,
                    "Exit while Waiting changed Session participation or leaked an Actor.");
                cases.Complete(
                    "waiting-exit-preserved-session");
            }
            finally
            {
                if (fixture != null)
                {
                    await fixture.DisposeAsync();
                }
            }
        }

        private static async Task RunRollbackAsync(
            QaCaseRegistry cases,
            FrameworkRuntimeHost host,
            LocalPlayerProvisioningAuthoring authoring,
            object preparationModule,
            PlayerSlotProfile firstSlotProfile,
            ActorProfile firstDefaultActor,
            Action installInvalidDefault,
            Action restoreDefault,
            Func<GameObject> resolveInvalidLogicalHost,
            Action<bool> setJoiningOpen)
        {
            QaActivityEntryReadinessFixture fixture = null;
            var owned =
                new QaOwnedAsyncOperation<
                    FrameworkActivityRequestResult>(
                    "qa-m07-delta-rollback");
            try
            {
                fixture =
                    await QaActivityEntryReadinessFixture
                        .CreateAsync();
                cases.Complete("rollback-fixture-created");

                ActivityAsset activity = fixture.CreateActivity(
                    "qa.m07.internal.rollback",
                    "Q3 M07 Delta Rollback",
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode
                        .InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup
                        .ContentScenePath);
                ConfigurePlayerParticipation(
                    activity,
                    PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared,
                    new[] { firstSlotProfile });
                cases.Complete("rollback-activity-configured");

                owned.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-delta-rollback"));
                cases.Complete("rollback-request-started");

                await AwaitPreparationOrTerminalAsync(
                    fixture,
                    owned);
                ActivityReadinessParticipant playerReadiness =
                    ResolvePlayerReadinessParticipant(host);
                Require(playerReadiness.State ==
                        ActivityReadinessParticipantState
                            .Preparing &&
                    !owned.IsCompleted,
                    "Rollback Activity did not remain pending in Player readiness.");
                fixture.Participant.CompletePreparation();
                cases.Complete(
                    "rollback-participants-preparing");

                PlayerParticipationOperationResult open =
                    InvokeReference<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryOpenJoining",
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-rollback-open-joining");
                Require(open != null &&
                    open.Completed &&
                    open.Snapshot.JoiningOpen,
                    open != null
                        ? open.ToDiagnosticString()
                        : "Opening joining returned no result.");
                setJoiningOpen(true);
                cases.Complete("joining-opened");

                LocalPlayerJoinResult join =
                    authoring.RequestJoin(
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-rollback-first-player-join");
                Require(join != null &&
                    join.Succeeded &&
                    join.Slot.PlayerSlotId ==
                        firstSlotProfile.PlayerSlotId &&
                    !join.Slot.HasSelectedActor,
                    join != null
                        ? join.ToDiagnosticString()
                        : "First Player Join returned no result.");
                cases.Complete("first-player-joined");

                installInvalidDefault();
                GameObject invalidLogicalHost =
                    resolveInvalidLogicalHost();
                Require(firstSlotProfile.DefaultActorProfile !=
                        firstDefaultActor &&
                    firstSlotProfile.DefaultActorProfile != null &&
                    invalidLogicalHost != null &&
                    ReferenceEquals(
                        firstSlotProfile.DefaultActorProfile
                            .LogicalActorHostPrefab,
                        invalidLogicalHost) &&
                    invalidLogicalHost.GetComponentInChildren<
                        PlayerActorDeclaration>(true) == null,
                    "Structurally invalid in-memory default Actor was not installed.");
                cases.Complete("invalid-default-installed");

                ActivityPlayerActorLifecycleSnapshot lifecycle =
                    GetLifecycleSnapshot(preparationModule);
                int occurrence =
                    ResolvePlayerReadinessParticipant(host)
                        .Occurrence;
                ActivityPlayerActorReconcileResult reconcile =
                    Reconcile(
                        preparationModule,
                        activity,
                        lifecycle.Owner,
                        occurrence,
                        "q3-delta-rollback");
                restoreDefault();

                Require(reconcile != null &&
                    reconcile.Status ==
                        ActivityPlayerActorReconcileStatus
                            .FailedPreparation &&
                    reconcile.RollbackAttempted &&
                    reconcile.RollbackSucceeded,
                    reconcile != null
                        ? reconcile.ToDiagnosticString()
                        : "Rollback reconcile returned no result.");
                cases.Complete(
                    "rollback-reconcile-failed-preparation");

                PlayerSlotRuntimeSnapshot slot =
                    FindSlot(
                        authoring.RuntimeSnapshot,
                        firstSlotProfile.PlayerSlotId);
                PlayerActorPreparationRuntimeHostSnapshot
                    preparation =
                        GetPreparationSnapshot(
                            preparationModule);
                Require(!slot.HasSelectedActor &&
                    preparation.Preparation.PreparedCount == 0 &&
                    join.LocalPlayerHost.ActorMount
                        .GetComponentsInChildren<
                            PlayerActorDeclaration>(true)
                        .Length == 0,
                    "Reconcile rollback did not revert only the newly applied selection/materialization delta.");
                cases.Complete("rollback-delta-reverted");

                FrameworkActivityRequestResult terminal =
                    await owned.AwaitTerminalAsync();
                Require(
                    !terminal.Succeeded &&
                    ResolvePlayerReadinessParticipant(host)
                        .State ==
                        ActivityReadinessParticipantState.Failed,
                    "Failed Required Player contribution did not terminate WaitVisible as NotReady. " +
                    terminal.Message);
                cases.Complete(
                    "rollback-request-terminal-not-ready");

                FrameworkActivityRequestResult clear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-rollback-clear");
                Require(
                    clear.Succeeded,
                    clear.Message);
                cases.Complete("rollback-activity-cleared");

                PlayerParticipationOperationResult close =
                    InvokeReference<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryCloseJoining",
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-rollback-close-joining");
                Require(close != null &&
                    close.Completed &&
                    !close.Snapshot.JoiningOpen,
                    close != null
                        ? close.ToDiagnosticString()
                        : "Rollback joining close returned no result.");
                setJoiningOpen(false);
            }
            finally
            {
                restoreDefault();
                if (owned.HasOperation && !owned.ReachedTerminal)
                {
                    await owned.UnwindAsync(
                        () => FailPendingReadinessAsync(
                            host,
                            fixture,
                            "q3-rollback-unwind"));
                }

                if (fixture != null)
                {
                    await fixture.DisposeAsync();
                    cases.TryCompleteIfNext(
                        "rollback-fixture-cleaned");
                }
            }
        }

        private static async Task RunMainReconcileAsync(
            QaCaseRegistry cases,
            FrameworkRuntimeHost host,
            LocalPlayerProvisioningAuthoring authoring,
            object playerInputManager,
            object preparationModule,
            object gameplayModule,
            PlayerSlotProfile firstSlotProfile,
            PlayerSlotProfile secondSlotProfile,
            ActorProfile replacementActor,
            Action<bool> setJoiningOpen)
        {
            QaActivityEntryReadinessFixture fixture = null;
            var owned =
                new QaOwnedAsyncOperation<
                    FrameworkActivityRequestResult>(
                    "qa-m07-main-reconcile");

            try
            {
                fixture =
                    await QaActivityEntryReadinessFixture
                        .CreateAsync();
                cases.Complete("main-fixture-created");

                ActivityAsset activity = fixture.CreateActivity(
                    "qa.m07.internal.main",
                    "Q3 M07 Player Reconcile",
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode
                        .InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup
                        .ContentScenePath);
                ConfigurePlayerParticipation(
                    activity,
                    PlayerParticipationRequirementLevel
                        .GameplayReady,
                    new[]
                    {
                        firstSlotProfile,
                        secondSlotProfile
                    });
                cases.Complete("main-activity-configured");

                owned.Attach(
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-main-reconcile"));
                cases.Complete("main-request-started");

                await AwaitPreparationOrTerminalAsync(
                    fixture,
                    owned);
                ActivityReadinessParticipant playerReadiness =
                    ResolvePlayerReadinessParticipant(host);
                Require(playerReadiness.State ==
                        ActivityReadinessParticipantState
                            .Preparing &&
                    !owned.IsCompleted,
                    "Main Activity did not remain pending before reconcile.");
                fixture.Participant.CompletePreparation();
                int occurrence = playerReadiness.Occurrence;
                ActivityPlayerActorLifecycleSnapshot lifecycle =
                    GetLifecycleSnapshot(preparationModule);
                RuntimeContentOwner exactOwner = lifecycle.Owner;
                Require(lifecycle.Status ==
                        ActivityPlayerActorLifecycleStatus
                            .SucceededEnteredPreparing &&
                    exactOwner.IsValid &&
                    lifecycle.ProjectedSlotCount == 2,
                    "Main Player lifecycle did not capture two projected Slots as Preparing.");
                cases.Complete(
                    "main-participants-preparing");

                ActivityAsset foreignActivity =
                    CreateForeignActivity();
                try
                {
                    ActivityPlayerActorReconcileResult foreignActivityResult =
                        Reconcile(
                            preparationModule,
                            foreignActivity,
                            exactOwner,
                            occurrence,
                            "q3-foreign-activity");
                    ActivityPlayerActorReconcileResult foreignOwnerResult =
                        Reconcile(
                            preparationModule,
                            activity,
                            RuntimeContentOwner.Activity(
                                "qa.m07.foreign-owner",
                                "Q3 Foreign Owner",
                                RuntimeDefinitionToken.MintAnonymous()),
                            occurrence,
                            "q3-foreign-owner");
                    ActivityPlayerActorReconcileResult staleOccurrenceResult =
                        Reconcile(
                            preparationModule,
                            activity,
                            exactOwner,
                            occurrence + 1,
                            "q3-stale-occurrence");
                    Require(foreignActivityResult.Status ==
                            ActivityPlayerActorReconcileStatus
                                .RejectedForeignOrStaleActivity &&
                        foreignOwnerResult.Status ==
                            ActivityPlayerActorReconcileStatus
                                .RejectedForeignOrStaleOwner &&
                        staleOccurrenceResult.Status ==
                            ActivityPlayerActorReconcileStatus
                                .RejectedForeignOrStaleOccurrence,
                        "Q3 exact Activity/owner/occurrence rejection diverged.");
                    cases.Complete(
                        "exact-owner-rejections-proved");
                }
                finally
                {
                    UnityEngine.Object.Destroy(foreignActivity);
                }

                ActivityPlayerActorReconcileResult preDelta =
                    Reconcile(
                        preparationModule,
                        activity,
                        exactOwner,
                        occurrence,
                        "q3-pre-delta-no-change");
                Require(preDelta.Status ==
                        ActivityPlayerActorReconcileStatus
                            .SucceededNoChange &&
                    preDelta.ReadinessReason ==
                        ActivityPlayerActorReadinessReason
                            .WaitingForJoin &&
                    !preDelta.StateChanged,
                    preDelta.ToDiagnosticString());
                cases.Complete(
                    "pre-delta-no-change-proved");

                PlayerParticipationOperationResult open =
                    InvokeReference<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryOpenJoining",
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-main-open-joining");
                Require(open != null &&
                    open.Completed &&
                    open.Snapshot.JoiningOpen &&
                    ReadBool(playerInputManager, "joiningEnabled"),
                    open != null
                        ? open.ToDiagnosticString()
                        : "Main opening joining returned no result.");
                setJoiningOpen(true);

                ActivityPlayerActorReconcileResult firstProgress =
                    Reconcile(
                        preparationModule,
                        activity,
                        exactOwner,
                        occurrence,
                        "q3-first-slot-progress");
                Require(firstProgress.Status ==
                        ActivityPlayerActorReconcileStatus
                            .SucceededProgressed &&
                    firstProgress.SatisfiedSlotCount == 0 &&
                    firstProgress.PendingSlotCount == 2 &&
                    firstProgress.FailedSlotCount == 0 &&
                    firstProgress.StateChanged &&
                    firstProgress.ReadinessReason ==
                        ActivityPlayerActorReadinessReason
                            .WaitingForJoin,
                    "First Slot should progress to an authoritative gameplay chain while " +
                    "remaining pending behind the current Activity entry gate; the second " +
                    "Slot remains WaitingForJoin. " +
                    firstProgress.ToDiagnosticString());

                LocalPlayerHostAuthoring firstHost =
                    ResolveRegisteredHost(
                        preparationModule,
                        firstSlotProfile.PlayerSlotId);
                Require(firstHost != null &&
                    CountActors(firstHost) == 1,
                    "First joined Host does not own exactly one Actor after reconcile progress.");
                PlayerActorPreparationRuntimeHostSnapshot firstPreparation =
                    GetPreparationSnapshot(preparationModule);
                PlayerActorPreparationSummary firstSummary =
                    FindPreparation(
                        firstPreparation.Preparation,
                        firstSlotProfile.PlayerSlotId);
                PlayerGameplayAdmissionSummary firstAdmission =
                    FindAdmission(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission,
                        firstSlotProfile.PlayerSlotId);
                Require(firstSummary.IsPrepared &&
                    firstAdmission.IsAdmitted,
                    "First Slot did not receive preparation and gameplay admission.");
                cases.Complete("first-slot-progressed");

                ActorId firstActorBeforeReplacement =
                    FindSingleActor(firstHost)
                        .ActorId;
                PlayerActorPreparationToken firstTokenBeforeReplacement =
                    firstSummary.Token;
                PlayerGameplayAdmissionToken firstAdmissionBeforeReplacement =
                    firstAdmission.Token;

                ActivityPlayerActorReconcileResult coalesced =
                    Reconcile(
                        preparationModule,
                        activity,
                        exactOwner,
                        occurrence,
                        "q3-revision-coalescing");
                PlayerActorPreparationSummary coalescedSummary =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule)
                            .Preparation,
                        firstSlotProfile.PlayerSlotId);
                PlayerGameplayAdmissionSummary coalescedAdmission =
                    FindAdmission(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission,
                        firstSlotProfile.PlayerSlotId);
                Require(coalesced.Status ==
                        ActivityPlayerActorReconcileStatus
                            .SucceededNoChange &&
                    coalesced.AppliedSessionRevision ==
                        firstProgress.AppliedSessionRevision &&
                    coalescedSummary.Token ==
                        firstTokenBeforeReplacement &&
                    coalescedAdmission.Token ==
                        firstAdmissionBeforeReplacement &&
                    FindSingleActor(firstHost)
                        .ActorId ==
                        firstActorBeforeReplacement,
                    "No-change reconcile duplicated or replaced Player evidence.");
                cases.Complete(
                    "revision-coalescing-proved");

                PlayerSlotRuntimeSnapshot firstSlot =
                    FindSlot(
                        authoring.RuntimeSnapshot,
                        firstSlotProfile.PlayerSlotId);
                RuntimeScopeContext activityScope =
                    ResolveActivityScopeContext(
                        host,
                        exactOwner,
                        "q3-replacement-authority");

                PlayerGameplayRuntimeOperationResult gameplayRelease =
                    InvokeReference<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryReleaseCurrentGameplay",
                        firstSlot.PlayerSlotId,
                        firstAdmissionBeforeReplacement,
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-release-gameplay-before-prepared-actor-replacement");
                Require(
                    gameplayRelease.Succeeded &&
                    gameplayRelease.Status ==
                        PlayerGameplayRuntimeOperationStatus
                            .SucceededReleased &&
                    !gameplayRelease.CurrentAdmission.IsAdmitted,
                    gameplayRelease.ToDiagnosticString());

                PlayerActorPreparationResult replacement =
                    InvokeReference<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryReplacePreparedActor",
                        activityScope,
                        new PlayerActorSelectionRequest(
                            firstSlot.PlayerSlotId,
                            replacementActor,
                            nameof(
                                QaM07InternalReconcileRegression),
                            "q3-replacement-before-reconcile",
                            firstSlot.SelectionRevision),
                        firstTokenBeforeReplacement,
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-replace-prepared-actor-before-reconcile");
                Require(
                    replacement.Succeeded &&
                    replacement.Status ==
                        PlayerActorPreparationStatus
                            .SucceededReplaced &&
                    replacement.PreviousSummary.Token ==
                        firstTokenBeforeReplacement &&
                    replacement.CurrentSummary.IsValid &&
                    replacement.CurrentSummary.IsPrepared &&
                    replacement.CurrentSummary
                        .SelectedActorProfileId ==
                        replacementActor.ActorProfileId &&
                    replacement.CurrentSummary
                        .PreparedActorProfileId ==
                        replacementActor.ActorProfileId &&
                    replacement.CurrentSummary.Token !=
                        firstTokenBeforeReplacement &&
                    replacement.HasMaterializationResult &&
                    replacement.MaterializationResult != null &&
                    replacement.MaterializationResult.Succeeded &&
                    replacement.HasSelectionResult &&
                    replacement.SelectionResult != null &&
                    replacement.SelectionResult.Succeeded &&
                    replacement.PreviousReleaseAttempted &&
                    replacement.PreviousReleaseSucceeded,
                    replacement.ToDiagnosticString());

                await Awaitable.NextFrameAsync();
                PlayerActorDeclaration settledReplacementActor =
                    FindSingleActor(firstHost);
                Require(
                    CountActors(firstHost) == 1 &&
                    settledReplacementActor.ActorId !=
                        firstActorBeforeReplacement &&
                    settledReplacementActor.ActorId ==
                        replacement.CurrentSummary
                            .Materialization.ActorId,
                    "Prepared Actor replacement succeeded transactionally, but the Unity destruction boundary did not settle to exactly one replacement Actor. " +
                    replacement.ToDiagnosticString());
                cases.Complete(
                    "replacement-selection-applied");

                ActivityPlayerActorReconcileResult replacementReconcile =
                    Reconcile(
                        preparationModule,
                        activity,
                        exactOwner,
                        occurrence,
                        "q3-replacement-reconcile");
                bool replacementAppliedByExplicitCall =
                    replacementReconcile.Status ==
                        ActivityPlayerActorReconcileStatus
                            .SucceededProgressed &&
                    replacementReconcile.StateChanged;
                bool replacementAlreadyAppliedByCoordinator =
                    replacementReconcile.Status ==
                        ActivityPlayerActorReconcileStatus
                            .SucceededNoChange &&
                    !replacementReconcile.StateChanged &&
                    replacementReconcile.RequestedSessionRevision ==
                        replacementReconcile.AppliedSessionRevision &&
                    replacementReconcile.AppliedSessionRevision ==
                        authoring.RuntimeSnapshot.Revision;
                Require(
                    (replacementAppliedByExplicitCall ||
                        replacementAlreadyAppliedByCoordinator) &&
                    replacementReconcile.SatisfiedSlotCount == 0 &&
                    replacementReconcile.PendingSlotCount == 2 &&
                    replacementReconcile.FailedSlotCount == 0 &&
                    replacementReconcile.ReadinessReason ==
                        ActivityPlayerActorReadinessReason
                            .WaitingForJoin &&
                    !replacementReconcile.RollbackAttempted &&
                    replacementReconcile.RollbackSucceeded,
                    "Replacement reconcile must either consume the first Slot delta explicitly " +
                    "or observe that the automatic LateUpdate coordinator already consumed the " +
                    "same stable Session revision. In both paths the first Slot remains pending " +
                    "only behind the current Activity entry gate and the second Slot remains " +
                    "WaitingForJoin. " +
                    replacementReconcile.ToDiagnosticString());

                Debug.Log(
                    $"{Prefix} phase='ReplacementReconcileObserved' " +
                    $"path='{(replacementAppliedByExplicitCall ? "ExplicitDelta" : "CoordinatorAlreadyApplied")}' " +
                    $"result='{Escape(replacementReconcile.ToDiagnosticString())}'.");

                await Awaitable.NextFrameAsync();
                PlayerActorDeclaration replacementDeclaration =
                    FindSingleActor(firstHost);
                PlayerActorPreparationSummary replacementSummary =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule)
                            .Preparation,
                        firstSlotProfile.PlayerSlotId);
                PlayerGameplayAdmissionSummary replacementAdmission =
                    FindAdmission(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission,
                        firstSlotProfile.PlayerSlotId);
                Require(
                    CountActors(firstHost) == 1 &&
                    replacementDeclaration.ActorId ==
                        settledReplacementActor.ActorId &&
                    replacementDeclaration.ActorId ==
                        replacement.CurrentSummary
                            .Materialization.ActorId &&
                    replacementSummary.Token ==
                        replacement.CurrentSummary.Token &&
                    replacementSummary.Token !=
                        firstTokenBeforeReplacement &&
                    replacementAdmission.IsAdmitted &&
                    replacementAdmission.Token !=
                        firstAdmissionBeforeReplacement &&
                    replacementSummary.IsPrepared &&
                    replacementSummary.SelectedActorProfileId ==
                        replacementActor.ActorProfileId &&
                    replacementSummary.PreparedActorProfileId ==
                        replacementActor.ActorProfileId,
                    "Replacement reconcile changed the settled replacement Actor or failed to rebuild the gameplay admission for its exact preparation evidence.");
                cases.Complete(
                    "replacement-reconcile-proved");

                LocalPlayerJoinResult secondJoin =
                    RequestJoinSharingPrimaryDevice(
                        authoring,
                        firstHost,
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-second-player-join",
                        out object sharedJoinDevice);

                object firstPlayerInput =
                    ReadProperty(
                        firstHost,
                        "PlayerInput");
                object secondPlayerInput =
                    ReadProperty(
                        secondJoin,
                        "PlayerInput");
                Require(
                    secondJoin.Succeeded &&
                    secondJoin.Slot.PlayerSlotId ==
                        secondSlotProfile.PlayerSlotId &&
                    firstPlayerInput != null &&
                    secondPlayerInput != null &&
                    ReadInt(firstPlayerInput, "playerIndex") !=
                        ReadInt(secondPlayerInput, "playerIndex") &&
                    PlayerInputContainsDevice(
                        firstPlayerInput,
                        sharedJoinDevice) &&
                    PlayerInputContainsDevice(
                        secondPlayerInput,
                        sharedJoinDevice) &&
                    ReadInt(playerInputManager, "playerCount") == 2 &&
                    CountJoined(authoring.RuntimeSnapshot) == 2,
                    secondJoin.ToDiagnosticString());

                LocalPlayerHostAuthoring secondHost =
                    ResolveRegisteredHost(
                        preparationModule,
                        secondSlotProfile.PlayerSlotId);
                Require(
                    ReferenceEquals(
                        secondHost,
                        secondJoin.LocalPlayerHost) &&
                    secondHost.IsJoined &&
                    secondHost.HasJoinedSlot &&
                    secondHost.JoinedPlayerSlotId ==
                        secondSlotProfile.PlayerSlotId,
                    "Second Player Join did not register the exact retained Host with the preparation authority.");
                cases.Complete("second-player-joined");

                ActivityPlayerActorReconcileResult completed =
                    Reconcile(
                        preparationModule,
                        activity,
                        exactOwner,
                        occurrence,
                        "q3-complete-second-slot");
                Require(completed.Status ==
                        ActivityPlayerActorReconcileStatus
                            .SucceededCompleted &&
                    completed.Completed &&
                    completed.SatisfiedSlotCount == 2 &&
                    completed.PendingSlotCount == 0 &&
                    completed.FailedSlotCount == 0,
                    completed.ToDiagnosticString());
                cases.Complete(
                    "main-reconcile-completed");

                FrameworkActivityRequestResult request =
                    await owned.AwaitTerminalAsync();
                Require(
                    request.Succeeded &&
                    request.ActivityFlowResult
                        .ActivityReadinessState.IsReady &&
                    ResolvePlayerReadinessParticipant(host)
                        .State ==
                        ActivityReadinessParticipantState
                            .Completed,
                    request.Message);
                cases.Complete("main-request-succeeded");

                await Awaitable.NextFrameAsync();
                Require(CountActors(firstHost) == 1 &&
                    CountActors(secondHost) == 1 &&
                    GetPreparationSnapshot(preparationModule)
                        .Preparation.PreparedCount == 2 &&
                    CountAdmitted(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission) == 2,
                    "Ready occurrence does not retain exactly one Actor and one admission per Slot.");
                cases.Complete(
                    "one-actor-per-slot-proved");

                PlayerActorPreparationSummary readyFirstPreparation =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule)
                            .Preparation,
                        firstSlotProfile.PlayerSlotId);
                PlayerActorPreparationSummary readySecondPreparation =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule)
                            .Preparation,
                        secondSlotProfile.PlayerSlotId);
                PlayerGameplayAdmissionSummary readyFirstAdmission =
                    FindAdmission(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission,
                        firstSlotProfile.PlayerSlotId);
                PlayerGameplayAdmissionSummary readySecondAdmission =
                    FindAdmission(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission,
                        secondSlotProfile.PlayerSlotId);
                ActorId readyFirstActorId =
                    FindSingleActor(firstHost).ActorId;
                ActorId readySecondActorId =
                    FindSingleActor(secondHost).ActorId;

                ActivityPlayerActorReconcileResult completedNoChange =
                    Reconcile(
                        preparationModule,
                        activity,
                        exactOwner,
                        occurrence,
                        "q3-completed-no-change");
                PlayerActorPreparationSummary noChangeFirstPreparation =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule)
                            .Preparation,
                        firstSlotProfile.PlayerSlotId);
                PlayerActorPreparationSummary noChangeSecondPreparation =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule)
                            .Preparation,
                        secondSlotProfile.PlayerSlotId);
                PlayerGameplayAdmissionSummary noChangeFirstAdmission =
                    FindAdmission(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission,
                        firstSlotProfile.PlayerSlotId);
                PlayerGameplayAdmissionSummary noChangeSecondAdmission =
                    FindAdmission(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission,
                        secondSlotProfile.PlayerSlotId);
                Require(
                    completedNoChange.Status ==
                        ActivityPlayerActorReconcileStatus
                            .SucceededNoChange &&
                    CountActors(firstHost) == 1 &&
                    CountActors(secondHost) == 1 &&
                    GetPreparationSnapshot(preparationModule)
                        .Preparation.PreparedCount == 2 &&
                    noChangeFirstPreparation.Token ==
                        readyFirstPreparation.Token &&
                    noChangeSecondPreparation.Token ==
                        readySecondPreparation.Token &&
                    noChangeFirstAdmission.Token ==
                        readyFirstAdmission.Token &&
                    noChangeSecondAdmission.Token ==
                        readySecondAdmission.Token &&
                    FindSingleActor(firstHost).ActorId ==
                        readyFirstActorId &&
                    FindSingleActor(secondHost).ActorId ==
                        readySecondActorId,
                    "Completed reconcile changed an Actor, preparation token or gameplay admission.");
                cases.Complete(
                    "completed-no-change-proved");

                FrameworkActivityRequestResult clear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-ready-exit");
                Require(
                    clear.Succeeded,
                    clear.Message);
                cases.Complete("ready-exit-succeeded");

                await Awaitable.NextFrameAsync();
                Require(CountActors(firstHost) == 0 &&
                    CountActors(secondHost) == 0 &&
                    GetPreparationSnapshot(preparationModule)
                        .Preparation.PreparedCount == 0 &&
                    CountAdmitted(
                        GetGameplaySnapshot(gameplayModule)
                            .Admission) == 0,
                    "Ready exit left contextual Player Actor/gameplay evidence.");
                cases.Complete(
                    "ready-exit-released-context");

                PlayerSlotRuntimeSnapshot firstFinal =
                    FindSlot(
                        authoring.RuntimeSnapshot,
                        firstSlotProfile.PlayerSlotId);
                PlayerSlotRuntimeSnapshot secondFinal =
                    FindSlot(
                        authoring.RuntimeSnapshot,
                        secondSlotProfile.PlayerSlotId);
                Require(
                    firstFinal.IsJoined &&
                    secondFinal.IsJoined &&
                    firstFinal.HasSelectedActor &&
                    secondFinal.HasSelectedActor &&
                    firstFinal.SelectedActorProfileId ==
                        replacementActor.ActorProfileId &&
                    secondSlotProfile.DefaultActorProfile != null &&
                    secondFinal.SelectedActorProfileId ==
                        secondSlotProfile.DefaultActorProfile
                            .ActorProfileId &&
                    firstHost.IsJoined &&
                    secondHost.IsJoined,
                    "Ready exit did not preserve the exact Session-owned Hosts, Joined Slots and Actor selections.");
                cases.Complete(
                    "session-authority-preserved");
            }
            finally
            {
                if (owned.HasOperation && !owned.ReachedTerminal)
                {
                    await owned.UnwindAsync(
                        () => FailPendingReadinessAsync(
                            host,
                            fixture,
                            "q3-main-unwind"));
                }

                if (fixture != null)
                {
                    await fixture.DisposeAsync();
                }
            }
        }

        private static async Task RunReentryAsync(
            QaCaseRegistry cases,
            FrameworkRuntimeHost host,
            LocalPlayerProvisioningAuthoring authoring,
            object preparationModule,
            PlayerSlotProfile firstSlotProfile,
            PlayerSlotProfile secondSlotProfile)
        {
            QaActivityEntryReadinessFixture fixture = null;
            try
            {
                fixture =
                    await QaActivityEntryReadinessFixture
                        .CreateAsync();
                fixture.ExpectParticipantPreparationCycles(2);

                cases.Complete("reentry-fixture-created");

                ActivityAsset activity = fixture.CreateActivity(
                    "qa.m07.internal.reentry",
                    "Q3 M07 Reentry",
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode
                        .InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup
                        .ContentScenePath);
                ConfigurePlayerParticipation(
                    activity,
                    PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared,
                    new[]
                    {
                        firstSlotProfile,
                        secondSlotProfile
                    });

                FrameworkActivityRequestResult first =
                    await RequestAndCompleteAuthorableParticipantAsync(
                        fixture,
                        activity,
                        "q3-reentry-first");
                Require(
                    first.Succeeded &&
                    first.ActivityFlowResult
                        .ActivityReadinessState.IsReady,
                    first.Message);

                ActivityReadinessParticipant playerReadiness =
                    ResolvePlayerReadinessParticipant(host);
                int firstOccurrence = playerReadiness.Occurrence;
                PlayerActorPreparationRuntimeHostSnapshot firstPreparation =
                    GetPreparationSnapshot(preparationModule);
                Dictionary<PlayerSlotId, ActorId> firstActors =
                    CaptureActorIds(
                        preparationModule,
                        firstPreparation.Preparation);
                Dictionary<PlayerSlotId, PlayerActorPreparationToken>
                    firstTokens =
                        CapturePreparationTokens(
                            firstPreparation.Preparation);
                Require(firstActors.Count == 2 &&
                    firstTokens.Count == 2,
                    "First reentry occurrence did not prepare two Players.");
                cases.Complete(
                    "reentry-first-request-succeeded");

                FrameworkActivityRequestResult firstClear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-reentry-first-clear");
                Require(
                    firstClear.Succeeded,
                    firstClear.Message);
                await Awaitable.NextFrameAsync();
                Require(
                    GetPreparationSnapshot(preparationModule)
                        .Preparation.PreparedCount == 0 &&
                    CountActors(
                        ResolveRegisteredHost(
                            preparationModule,
                            firstSlotProfile.PlayerSlotId)) == 0 &&
                    CountActors(
                        ResolveRegisteredHost(
                            preparationModule,
                            secondSlotProfile.PlayerSlotId)) == 0,
                    "First reentry exit left prepared or physically retained Actors.");
                cases.Complete(
                    "reentry-first-exit-succeeded");

                FrameworkActivityRequestResult second =
                    await RequestAndCompleteAuthorableParticipantAsync(
                        fixture,
                        activity,
                        "q3-reentry-second");
                Require(
                    second.Succeeded &&
                    second.ActivityFlowResult
                        .ActivityReadinessState.IsReady,
                    second.Message);
                cases.Complete(
                    "reentry-second-request-succeeded");

                playerReadiness =
                    ResolvePlayerReadinessParticipant(host);
                Require(playerReadiness.Occurrence >
                    firstOccurrence,
                    "Reentry did not create a new Activity readiness occurrence.");
                cases.Complete(
                    "reentry-occurrence-advanced");

                PlayerActorPreparationSnapshot secondPreparation =
                    GetPreparationSnapshot(
                        preparationModule)
                        .Preparation;
                Dictionary<PlayerSlotId, ActorId> secondActors =
                    CaptureActorIds(
                        preparationModule,
                        secondPreparation);
                Dictionary<PlayerSlotId, PlayerActorPreparationToken>
                    secondTokens =
                        CapturePreparationTokens(
                            GetPreparationSnapshot(
                                preparationModule)
                                .Preparation);
                Require(secondActors.Count == 2 &&
                    secondTokens.Count == 2 &&
                    AllActorsAndTokensRenewed(
                        firstActors,
                        firstTokens,
                        secondActors,
                        secondTokens),
                    "Reentry reused an Actor identity or preparation token from the previous occurrence.");
                cases.Complete(
                    "reentry-actors-renewed");

                FrameworkActivityRequestResult secondClear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07InternalReconcileRegression),
                        "q3-reentry-second-clear");
                Require(
                    secondClear.Succeeded,
                    secondClear.Message);
                await Awaitable.NextFrameAsync();
                Require(
                    GetPreparationSnapshot(preparationModule)
                        .Preparation.PreparedCount == 0 &&
                    CountActors(
                        ResolveRegisteredHost(
                            preparationModule,
                            firstSlotProfile.PlayerSlotId)) == 0 &&
                    CountActors(
                        ResolveRegisteredHost(
                            preparationModule,
                            secondSlotProfile.PlayerSlotId)) == 0,
                    "Second reentry exit left prepared or physically retained Actors.");
                cases.Complete("reentry-cleared");
            }
            finally
            {
                if (fixture != null)
                {
                    await fixture.DisposeAsync();
                }
            }
        }

        private static async Task<FrameworkActivityRequestResult>
            RequestAndCompleteAuthorableParticipantAsync(
                QaActivityEntryReadinessFixture fixture,
                ActivityAsset activity,
                string reason)
        {
            var started =
                new TaskCompletionSource<bool>(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);
            UnityAction handler = () =>
                started.TrySetResult(true);
            fixture.Participant.PreparationStarted
                .AddListener(handler);

            try
            {
                Task<FrameworkActivityRequestResult> request =
                    fixture.Activities.RequestActivityAsync(
                        activity,
                        nameof(
                            QaM07InternalReconcileRegression),
                        reason);
                Task first = await Task.WhenAny(
                    started.Task,
                    request);
                if (ReferenceEquals(first, request))
                {
                    FrameworkActivityRequestResult early =
                        await request;
                    throw new InvalidOperationException(
                        "Reentry request terminated before authorable readiness began. " +
                        early.Message);
                }

                await started.Task;
                Require(fixture.Participant.State ==
                    ActivityReadinessParticipantState
                        .Preparing,
                    "Reentry authorable participant is not Preparing.");
                fixture.Participant.CompletePreparation();
                return await request;
            }
            finally
            {
                fixture.Participant.PreparationStarted
                    .RemoveListener(handler);
            }
        }

        private static async Task AwaitPreparationOrTerminalAsync(
            QaActivityEntryReadinessFixture fixture,
            QaOwnedAsyncOperation<
                FrameworkActivityRequestResult> owned)
        {
            Task first = await Task.WhenAny(
                fixture.PreparationStarted.Task,
                owned.Task);
            if (ReferenceEquals(first, owned.Task))
            {
                FrameworkActivityRequestResult early =
                    await owned.AwaitTerminalAsync();
                throw new InvalidOperationException(
                    "Activity request terminated before readiness preparation. " +
                    early.Message);
            }

            await fixture.PreparationStarted.Task;
        }

        private static Task FailPendingReadinessAsync(
            FrameworkRuntimeHost host,
            QaActivityEntryReadinessFixture fixture,
            string reason)
        {
            if (fixture != null &&
                fixture.Participant != null &&
                fixture.Participant.State ==
                    ActivityReadinessParticipantState
                        .Preparing)
            {
                fixture.Participant.FailPreparation(
                    reason);
            }

            if (host != null)
            {
                Transform child =
                    host.transform.Find(
                        PlayerReadinessObjectName);
                ActivityReadinessParticipant participant =
                    child != null
                        ? child.GetComponent<
                            ActivityReadinessParticipant>()
                        : null;
                if (participant != null &&
                    participant.State ==
                    ActivityReadinessParticipantState
                        .Preparing)
                {
                    participant.FailPreparation(reason);
                }
            }

            return Task.CompletedTask;
        }

        private static void ConfigurePlayerParticipation(
            ActivityAsset activity,
            PlayerParticipationRequirementLevel requirement,
            IReadOnlyList<PlayerSlotProfile> slots)
        {
            Require(activity != null,
                "Player participation configuration requires an Activity.");
            Require(slots != null && slots.Count > 0,
                "Player participation configuration requires explicit Slots.");

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
            SerializedProperty explicitSlots =
                RequireProperty(
                    serialized,
                    "playerParticipationExplicitSlotProfiles");
            explicitSlots.arraySize = slots.Count;
            for (int index = 0;
                 index < slots.Count;
                 index++)
            {
                explicitSlots.GetArrayElementAtIndex(index)
                    .objectReferenceValue = slots[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(activity.PlayerParticipationProjectionMode ==
                    ActivityParticipationProjectionMode
                        .ExplicitSlots &&
                activity.PlayerParticipationRequirementLevel ==
                    requirement,
                "Runtime Activity Player participation configuration did not apply.");
        }

        private static IReadOnlyList<PlayerSlotProfile>
            ResolveSlotProfiles(
                LocalPlayerProvisioningAuthoring authoring,
                int count)
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset
                        .ResourcesPath);
            GameApplicationAsset application =
                settings != null
                    ? settings.ActiveGameApplication
                    : null;
            Require(application != null,
                "Q3 could not resolve active Game Application.");

            var slots = new List<PlayerSlotProfile>();
            for (int index = 0; index < count; index++)
            {
                Require(QaPlayerSessionQaSupport.TryGetSupportedSlot(
                        application,
                        index,
                        out PlayerSlotProfile slot) &&
                    slot != null,
                    $"Q3 could not resolve Local Player Slot index '{index}'.");
                slots.Add(slot);
            }

            return slots;
        }

        private static LocalPlayerJoinResult
            RequestJoinSharingPrimaryDevice(
                LocalPlayerProvisioningAuthoring authoring,
                LocalPlayerHostAuthoring primaryHost,
                string source,
                string reason,
                out object sharedDevice)
        {
            Require(authoring != null &&
                authoring.RuntimeReady,
                "Shared-device join requires ready Local Player provisioning.");
            Require(primaryHost != null &&
                primaryHost.IsJoined &&
                primaryHost.HasJoinedSlot,
                "Shared-device join requires the current registered primary Host.");

            object primaryPlayerInput =
                ReadProperty(
                    primaryHost,
                    "PlayerInput");
            Require(primaryPlayerInput != null,
                "Primary Local Player Host has no PlayerInput.");

            object devices =
                ReadProperty(
                    primaryPlayerInput,
                    "devices");
            int deviceCount =
                ReadInt(
                    devices,
                    "Count");
            Require(deviceCount > 0,
                "Secondary Q3 join requires one explicit device from the primary PlayerInput.");

            PropertyInfo item =
                devices.GetType().GetProperty(
                    "Item",
                    InstanceAny);
            Require(item != null &&
                item.GetIndexParameters().Length == 1 &&
                item.GetIndexParameters()[0].ParameterType ==
                    typeof(int),
                "PlayerInput devices collection has no Int32 indexer.");

            sharedDevice =
                item.GetValue(
                    devices,
                    new object[] { 0 });
            Require(sharedDevice != null &&
                ReadBool(sharedDevice, "added"),
                "Primary PlayerInput device is missing or no longer added.");

            Type requestType =
                ResolveType(
                    "Immersive.Framework.PlayerParticipation.LocalPlayerJoinRequest");
            Type inputDeviceType =
                ResolveType(
                    "UnityEngine.InputSystem.InputDevice");
            Require(inputDeviceType.IsInstanceOfType(sharedDevice),
                "Primary PlayerInput device is not an InputDevice.");

            ConstructorInfo constructor =
                requestType.GetConstructor(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(string),
                        inputDeviceType,
                        typeof(string)
                    },
                    null);
            Require(constructor != null,
                "LocalPlayerJoinRequest shared-device constructor was not found.");

            object request =
                constructor.Invoke(
                    new[]
                    {
                        (object)source,
                        reason,
                        sharedDevice,
                        null
                    });

            MethodInfo requestJoin =
                FindMethodBySignature(
                    authoring.GetType(),
                    "RequestJoin",
                    requestType);
            object rawResult =
                requestJoin.Invoke(
                    authoring,
                    new[] { request });
            LocalPlayerJoinResult result =
                rawResult as LocalPlayerJoinResult;
            Require(result != null,
                "Shared-device Local Player join returned no result.");

            Debug.Log(
                $"{Prefix} phase='SecondJoinRequested' " +
                $"device='{DescribeInputDevice(sharedDevice)}' " +
                $"managerPlayers='{ReadInt(ResolvePlayerInputManager(authoring), "playerCount")}' " +
                $"result='{Escape(result.ToDiagnosticString())}'.");

            return result;
        }

        private static bool PlayerInputContainsDevice(
            object playerInput,
            object expectedDevice)
        {
            if (playerInput == null ||
                expectedDevice == null)
            {
                return false;
            }

            object devices =
                ReadProperty(
                    playerInput,
                    "devices");
            int count =
                ReadInt(
                    devices,
                    "Count");
            PropertyInfo item =
                devices.GetType().GetProperty(
                    "Item",
                    InstanceAny);
            if (item == null ||
                item.GetIndexParameters().Length != 1)
            {
                return false;
            }

            for (int index = 0;
                 index < count;
                 index++)
            {
                object candidate =
                    item.GetValue(
                        devices,
                        new object[] { index });
                if (ReferenceEquals(
                        candidate,
                        expectedDevice))
                {
                    return true;
                }
            }

            return false;
        }

        private static string DescribeInputDevice(
            object device)
        {
            if (device == null)
            {
                return "<missing>";
            }

            object layout =
                ReadProperty(
                    device,
                    "layout");
            object name =
                ReadProperty(
                    device,
                    "name");
            object deviceId =
                ReadProperty(
                    device,
                    "deviceId");
            object added =
                ReadProperty(
                    device,
                    "added");
            return
                $"name='{name}' layout='{layout}' deviceId='{deviceId}' added='{added}'";
        }

        private static MethodInfo FindMethodBySignature(
            Type type,
            string methodName,
            params Type[] parameterTypes)
        {
            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                MethodInfo candidate =
                    current.GetMethod(
                        methodName,
                        InstanceAny,
                        null,
                        parameterTypes,
                        null);
                if (candidate != null)
                {
                    return candidate;
                }
            }

            throw new MissingMethodException(
                type.FullName,
                methodName);
        }

        private static object ResolvePlayerInputManager(
            LocalPlayerProvisioningAuthoring authoring)
        {
            Require(authoring != null,
                "Local Player provisioning authoring is missing.");
            object manager = ReadProperty(
                authoring,
                "PlayerInputManager");
            Require(manager != null,
                "Local Player provisioning authoring has no PlayerInputManager.");
            return manager;
        }

        private static object ReadProperty(
            object target,
            string propertyName)
        {
            Require(target != null,
                $"Cannot read property '{propertyName}' from a missing target.");
            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    InstanceAny);
            Require(property != null &&
                property.GetIndexParameters().Length == 0,
                $"Property '{propertyName}' was not found on '{target.GetType().FullName}'.");
            return property.GetValue(target);
        }

        private static int ReadInt(
            object target,
            string propertyName)
        {
            object value = ReadProperty(
                target,
                propertyName);
            Require(value is int,
                $"Property '{propertyName}' on '{target.GetType().FullName}' is not Int32.");
            return (int)value;
        }

        private static bool ReadBool(
            object target,
            string propertyName)
        {
            object value = ReadProperty(
                target,
                propertyName);
            Require(value is bool,
                $"Property '{propertyName}' on '{target.GetType().FullName}' is not Boolean.");
            return (bool)value;
        }

        private static LocalPlayerProvisioningAuthoring
            ResolveProvisioningAuthoring(
                FrameworkRuntimeHost host)
        {
            MethodInfo method = FindMethod(
                host.GetType(),
                "TryResolveLocalPlayerProvisioningAuthoring",
                3);
            object[] arguments =
            {
                null,
                false,
                string.Empty
            };
            object raw = method.Invoke(host, arguments);
            bool succeeded = raw is bool value && value;
            LocalPlayerProvisioningAuthoring authoring =
                arguments[0] as
                    LocalPlayerProvisioningAuthoring;
            bool configured =
                arguments[1] is bool configuredValue &&
                configuredValue;
            string diagnostic =
                arguments[2] as string ?? string.Empty;
            Require(succeeded && configured && authoring != null,
                string.IsNullOrWhiteSpace(diagnostic)
                    ? "FrameworkRuntimeHost did not resolve Local Player provisioning authoring."
                    : diagnostic);
            return authoring;
        }

        private static RuntimeScopeContext
            ResolveActivityScopeContext(
                FrameworkRuntimeHost host,
                RuntimeContentOwner owner,
                string reason)
        {
            Require(host != null,
                "Activity scope resolution requires the exact FrameworkRuntimeHost.");
            Require(owner.IsValid,
                "Activity scope resolution requires a valid RuntimeContentOwner.");

            object runtimeContent =
                ReadProperty(
                    host,
                    "RuntimeContentRuntime");
            Require(runtimeContent != null,
                "FrameworkRuntimeHost has no RuntimeContentRuntime.");

            MethodInfo method =
                FindTryCreateScopeContextMethod(
                    runtimeContent.GetType());
            object[] arguments =
            {
                owner,
                nameof(QaM07InternalReconcileRegression),
                reason,
                default(RuntimeScopeContext)
            };
            bool succeeded =
                method.Invoke(
                    runtimeContent,
                    arguments) is bool value &&
                value;
            RuntimeScopeContext context =
                arguments[3] is RuntimeScopeContext resolved
                    ? resolved
                    : default;

            Require(
                succeeded &&
                context.IsValid &&
                context.Owner == owner,
                $"RuntimeContentRuntime did not resolve the exact Activity scope for owner '{owner.StableText}'.");

            return context;
        }

        private static MethodInfo
            FindTryCreateScopeContextMethod(Type type)
        {
            Type contextOutType =
                typeof(RuntimeScopeContext)
                    .MakeByRefType();

            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                MethodInfo[] methods =
                    current.GetMethods(InstanceAny);
                for (int index = 0;
                     index < methods.Length;
                     index++)
                {
                    MethodInfo candidate =
                        methods[index];
                    if (!string.Equals(
                            candidate.Name,
                            "TryCreateScopeContext",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters =
                        candidate.GetParameters();
                    if (parameters.Length == 4 &&
                        parameters[0].ParameterType ==
                            typeof(RuntimeContentOwner) &&
                        parameters[1].ParameterType ==
                            typeof(string) &&
                        parameters[2].ParameterType ==
                            typeof(string) &&
                        parameters[3].ParameterType ==
                            contextOutType)
                    {
                        return candidate;
                    }
                }
            }

            throw new MissingMethodException(
                type.FullName,
                "TryCreateScopeContext(RuntimeContentOwner, string, string, out RuntimeScopeContext)");
        }

        private static object ResolveHostComponent(
            FrameworkRuntimeHost host,
            string typeName)
        {
            Type type = ResolveType(typeName);
            return host.GetComponent(type);
        }

        private static ActivityReadinessParticipant
            ResolvePlayerReadinessParticipant(
                FrameworkRuntimeHost host)
        {
            Transform child =
                host.transform.Find(
                    PlayerReadinessObjectName);
            ActivityReadinessParticipant participant =
                child != null
                    ? child.GetComponent<
                        ActivityReadinessParticipant>()
                    : null;
            Require(participant != null,
                "FrameworkRuntimeHost has no Player Activity Readiness participant.");
            return participant;
        }

        private static ActivityPlayerActorReconcileResult
            Reconcile(
                object preparationModule,
                ActivityAsset activity,
                RuntimeContentOwner owner,
                int occurrence,
                string reason)
        {
            ActivityPlayerActorReconcileResult result =
                InvokeReference<
                    ActivityPlayerActorReconcileResult>(
                    preparationModule,
                    "TryReconcileActiveActivityPlayerLifecycle",
                    activity,
                    owner,
                    occurrence,
                    nameof(
                        QaM07InternalReconcileRegression),
                    reason);
            Require(result != null,
                "Player reconcile returned no result.");
            return result;
        }

        private static ActivityPlayerActorLifecycleSnapshot
            GetLifecycleSnapshot(
                object preparationModule)
        {
            object[] arguments = { null };
            MethodInfo method = FindMethod(
                preparationModule.GetType(),
                "TryGetActivityPlayerActorLifecycleSnapshot",
                1);
            bool succeeded =
                method.Invoke(
                    preparationModule,
                    arguments) is bool value &&
                value;
            ActivityPlayerActorLifecycleSnapshot snapshot =
                arguments[0] as
                    ActivityPlayerActorLifecycleSnapshot;
            Require(succeeded && snapshot != null,
                "Player lifecycle snapshot is unavailable.");
            return snapshot;
        }

        private static PlayerActorPreparationRuntimeHostSnapshot
            GetPreparationSnapshot(
                object preparationModule)
        {
            object[] arguments = { null };
            MethodInfo method = FindMethod(
                preparationModule.GetType(),
                "TryGetSnapshot",
                1);
            bool succeeded =
                method.Invoke(
                    preparationModule,
                    arguments) is bool value &&
                value;
            PlayerActorPreparationRuntimeHostSnapshot snapshot =
                arguments[0] as
                    PlayerActorPreparationRuntimeHostSnapshot;
            Require(succeeded && snapshot != null,
                "Player Actor preparation snapshot is unavailable.");
            return snapshot;
        }

        private static PlayerGameplayRuntimeHostSnapshot
            GetGameplaySnapshot(
                object gameplayModule)
        {
            object[] arguments = { null };
            MethodInfo method = FindMethod(
                gameplayModule.GetType(),
                "TryGetSnapshot",
                1);
            bool succeeded =
                method.Invoke(
                    gameplayModule,
                    arguments) is bool value &&
                value;
            PlayerGameplayRuntimeHostSnapshot snapshot =
                arguments[0] as
                    PlayerGameplayRuntimeHostSnapshot;
            Require(succeeded && snapshot != null,
                "Player gameplay snapshot is unavailable.");
            return snapshot;
        }

        private static LocalPlayerHostAuthoring
            ResolveRegisteredHost(
                object preparationModule,
                PlayerSlotId playerSlotId)
        {
            Require(preparationModule != null,
                "Player Actor preparation module is missing.");
            Require(playerSlotId.IsValid,
                "Registered Host resolution requires a valid Player Slot identity.");

            MethodInfo method =
                FindTryGetRegisteredHostMethod(
                    preparationModule.GetType());
            object[] arguments =
            {
                playerSlotId,
                null,
                string.Empty
            };
            bool succeeded =
                method.Invoke(
                    preparationModule,
                    arguments) is bool value &&
                value;
            LocalPlayerHostAuthoring host =
                arguments[1] as LocalPlayerHostAuthoring;
            string issue =
                arguments[2] as string ?? string.Empty;

            Require(
                succeeded &&
                host != null &&
                host.HasJoinedSlot &&
                host.JoinedPlayerSlotId == playerSlotId,
                string.IsNullOrWhiteSpace(issue)
                    ? $"Registered Local Player Host for Slot '{playerSlotId.StableText}' is unavailable or inconsistent."
                    : issue);

            return host;
        }

        private static MethodInfo
            FindTryGetRegisteredHostMethod(Type type)
        {
            Type hostOutType =
                typeof(LocalPlayerHostAuthoring)
                    .MakeByRefType();
            Type stringOutType =
                typeof(string).MakeByRefType();

            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                MethodInfo[] methods =
                    current.GetMethods(InstanceAny);
                for (int index = 0;
                     index < methods.Length;
                     index++)
                {
                    MethodInfo candidate =
                        methods[index];
                    if (!string.Equals(
                            candidate.Name,
                            "TryGetRegisteredHost",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters =
                        candidate.GetParameters();
                    if (parameters.Length == 3 &&
                        parameters[0].ParameterType ==
                            typeof(PlayerSlotId) &&
                        parameters[1].ParameterType ==
                            hostOutType &&
                        parameters[2].ParameterType ==
                            stringOutType)
                    {
                        return candidate;
                    }
                }
            }

            throw new MissingMethodException(
                type.FullName,
                "TryGetRegisteredHost(PlayerSlotId, out LocalPlayerHostAuthoring, out string)");
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            Require(snapshot != null,
                "Player participation snapshot is missing.");
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                if (snapshot.Slots[index].PlayerSlotId ==
                    playerSlotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"Player Slot '{playerSlotId.StableText}' was not found.");
        }

        private static PlayerActorPreparationSummary
            FindPreparation(
                PlayerActorPreparationSnapshot snapshot,
                PlayerSlotId playerSlotId)
        {
            Require(snapshot != null,
                "Player preparation snapshot is missing.");
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                if (snapshot.Slots[index].PlayerSlotId ==
                    playerSlotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"Preparation for Slot '{playerSlotId.StableText}' was not found.");
        }

        private static PlayerGameplayAdmissionSummary
            FindAdmission(
                PlayerGameplayAdmissionSnapshot snapshot,
                PlayerSlotId playerSlotId)
        {
            Require(
                snapshot != null,
                "Player gameplay admission snapshot is missing.");

            if (!snapshot.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary summary))
            {
                throw new InvalidOperationException(
                    $"Gameplay admission for Slot '{playerSlotId.StableText}' was not found.");
            }

            return summary;
        }

        private static int CountAdmitted(
            PlayerGameplayAdmissionSnapshot snapshot)
        {
            return snapshot != null
                ? snapshot.ReadyCount +
                  snapshot.BlockedByInputGateCount +
                  snapshot.ReleaseFailedCount
                : 0;
        }

        private static int CountJoined(
            PlayerParticipationSnapshot snapshot)
        {
            return snapshot?.JoinedCount ?? 0;
        }

        private static int CountActors(
            LocalPlayerHostAuthoring host)
        {
            return host != null &&
                host.ActorMount != null
                ? host.ActorMount
                    .GetComponentsInChildren<
                        PlayerActorDeclaration>(true)
                    .Length
                : 0;
        }

        private static PlayerActorDeclaration FindSingleActor(
            LocalPlayerHostAuthoring host)
        {
            PlayerActorDeclaration[] actors =
                host != null &&
                host.ActorMount != null
                    ? host.ActorMount
                        .GetComponentsInChildren<
                            PlayerActorDeclaration>(true)
                    : Array.Empty<
                        PlayerActorDeclaration>();
            Require(actors.Length == 1,
                $"Expected exactly one Actor under Host '{(host != null ? host.name : "<missing>")}', actual='{actors.Length}'.");
            return actors[0];
        }

        private static ActorProfile CreateInvalidActorClone(
            ActorProfile template,
            out GameObject invalidLogicalHost)
        {
            Require(template != null,
                "Invalid Actor fixture requires a template ActorProfile.");

            invalidLogicalHost =
                new GameObject(
                    "Q3 Invalid Logical Actor Host");
            invalidLogicalHost.SetActive(false);

            ActorProfile clone =
                UnityEngine.Object.Instantiate(template);
            clone.name =
                "Q3 Invalid Materialization Actor";

            var serialized = new SerializedObject(clone);
            SerializedProperty prefab =
                RequireProperty(
                    serialized,
                    "logicalActorHostPrefab");
            prefab.objectReferenceValue =
                invalidLogicalHost;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(clone.ActorProfileId.IsValid &&
                clone.LogicalActorHostPrefab != null &&
                ReferenceEquals(
                    clone.LogicalActorHostPrefab,
                    invalidLogicalHost) &&
                invalidLogicalHost.GetComponentInChildren<
                    PlayerActorDeclaration>(true) == null,
                "Invalid Actor fixture did not retain a non-null host without PlayerActorDeclaration.");

            return clone;
        }

        private static FieldInfo ResolveDefaultActorField()
        {
            FieldInfo field =
                typeof(PlayerSlotProfile).GetField(
                    "defaultActorProfile",
                    InstanceAny);
            Require(field != null,
                "PlayerSlotProfile default Actor field was not found.");
            return field;
        }

        private static ActivityAsset CreateForeignActivity()
        {
            ActivityAsset activity =
                ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = "Q3 Foreign Activity";
            var serialized = new SerializedObject(activity);
            RequireProperty(serialized, "activityId")
                .stringValue = "qa.m07.foreign-activity";
            RequireProperty(serialized, "activityName")
                .stringValue = "Q3 Foreign Activity";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static Dictionary<PlayerSlotId, ActorId>
            CaptureActorIds(
                object preparationModule,
                PlayerActorPreparationSnapshot snapshot)
        {
            Require(snapshot != null,
                "Actor identity capture requires a Player preparation snapshot.");

            var result =
                new Dictionary<PlayerSlotId, ActorId>();
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                PlayerActorPreparationSummary summary =
                    snapshot.Slots[index];
                if (!summary.IsPrepared)
                {
                    continue;
                }

                LocalPlayerHostAuthoring host =
                    ResolveRegisteredHost(
                        preparationModule,
                        summary.PlayerSlotId);
                Require(
                    CountActors(host) == 1,
                    $"Prepared Slot '{summary.PlayerSlotId.StableText}' must resolve exactly one Actor under its registered Host.");

                result.Add(
                    summary.PlayerSlotId,
                    FindSingleActor(host).ActorId);
            }

            return result;
        }

        private static Dictionary<
            PlayerSlotId,
            PlayerActorPreparationToken>
            CapturePreparationTokens(
                PlayerActorPreparationSnapshot snapshot)
        {
            var result =
                new Dictionary<
                    PlayerSlotId,
                    PlayerActorPreparationToken>();
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                PlayerActorPreparationSummary summary =
                    snapshot.Slots[index];
                if (summary.IsPrepared)
                {
                    result.Add(
                        summary.PlayerSlotId,
                        summary.Token);
                }
            }

            return result;
        }

        private static bool AllActorsAndTokensRenewed(
            IReadOnlyDictionary<PlayerSlotId, ActorId>
                previousActors,
            IReadOnlyDictionary<
                PlayerSlotId,
                PlayerActorPreparationToken>
                previousTokens,
            IReadOnlyDictionary<PlayerSlotId, ActorId>
                currentActors,
            IReadOnlyDictionary<
                PlayerSlotId,
                PlayerActorPreparationToken>
                currentTokens)
        {
            if (previousActors.Count != currentActors.Count ||
                previousTokens.Count != currentTokens.Count)
            {
                return false;
            }

            foreach (KeyValuePair<PlayerSlotId, ActorId> pair
                     in previousActors)
            {
                if (!currentActors.TryGetValue(
                        pair.Key,
                        out ActorId currentActor) ||
                    currentActor == pair.Value ||
                    !previousTokens.TryGetValue(
                        pair.Key,
                        out PlayerActorPreparationToken
                            previousToken) ||
                    !currentTokens.TryGetValue(
                        pair.Key,
                        out PlayerActorPreparationToken
                            currentToken) ||
                    currentToken == previousToken)
                {
                    return false;
                }
            }

            return true;
        }

        private static T InvokeReference<T>(
            object target,
            string methodName,
            params object[] arguments)
            where T : class
        {
            Require(target != null,
                $"Cannot invoke '{methodName}' on a missing target.");
            MethodInfo method = FindMethod(
                target.GetType(),
                methodName,
                arguments.Length);
            object result = method.Invoke(
                target,
                arguments);
            T typed = result as T;
            Require(typed != null,
                $"Method '{methodName}' returned no '{typeof(T).Name}'.");
            return typed;
        }

        private static MethodInfo FindMethod(
            Type type,
            string methodName,
            int parameterCount)
        {
            for (Type current = type;
                 current != null;
                 current = current.BaseType)
            {
                MethodInfo[] methods =
                    current.GetMethods(InstanceAny);
                for (int index = 0;
                     index < methods.Length;
                     index++)
                {
                    MethodInfo candidate =
                        methods[index];
                    if (string.Equals(
                            candidate.Name,
                            methodName,
                            StringComparison.Ordinal) &&
                        candidate.GetParameters().Length ==
                            parameterCount)
                    {
                        return candidate;
                    }
                }
            }

            throw new MissingMethodException(
                type.FullName,
                methodName);
        }

        private static Type ResolveType(string fullName)
        {
            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0;
                 index < assemblies.Length;
                 index++)
            {
                Type type = assemblies[index].GetType(
                    fullName,
                    false);
                if (type != null)
                {
                    return type;
                }
            }

            throw new TypeLoadException(
                $"Runtime type '{fullName}' was not found.");
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property =
                serialized.FindProperty(name);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Required serialized field '{name}' was not found.");
            }

            return property;
        }

        private static void SetEnumName(
            SerializedProperty property,
            string enumName)
        {
            for (int index = 0;
                 index < property.enumNames.Length;
                 index++)
            {
                if (string.Equals(
                        property.enumNames[index],
                        enumName,
                        StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Enum value '{enumName}' was not found on '{property.propertyPath}'.");
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

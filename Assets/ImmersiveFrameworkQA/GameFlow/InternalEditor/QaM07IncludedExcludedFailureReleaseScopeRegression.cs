using System;
using System.Reflection;
using ImmersiveFrameworkQA.Player;
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
    /// IF-M07-12B-9 Play Mode regression.
    ///
    /// Proves that an Activity-scoped explicit Player projection owns failure
    /// and release only for the Slots captured by that projection:
    /// - an invalid included Slot blocks Activity entry;
    /// - an excluded Slot remains unchanged when an included Slot fails;
    /// - an invalid excluded Slot cannot block an Activity whose included Slot
    ///   is valid;
    /// - Actor preparation and release operate only on the included Slot;
    /// - Session-owned Join, PlayerInput and technical Host state survive both
    ///   the failed occurrence and the successful occurrence release.
    /// </summary>
    public static class
        QaM07IncludedExcludedFailureReleaseScopeRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Participation/Run Included Excluded Release Scope";
        private const string Prefix =
            "[QA_IF_M07_12B_9_INCLUDED_EXCLUDED_FAILURE_RELEASE_SCOPE]";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";
        private const int FrameBudget = 300;
        private const int ExpectedCaseCount = 42;

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "setup-confirmed",
            "official-host-resolved",
            "provisioning-authoring-resolved",
            "two-slot-fixture-confirmed",
            "preparation-module-resolved",
            "fresh-session-confirmed",
            "joining-opened",
            "first-player-joined",
            "second-player-joined",
            "two-player-session-confirmed",

            "included-failure-fixture-created",
            "included-failure-activity-configured",
            "included-invalid-default-installed",
            "excluded-baseline-captured-before-failure",
            "included-failure-request-started",
            "included-failure-terminal-rejected",
            "included-failure-diagnostic-scoped",
            "included-failure-authority-preserved",
            "included-failure-no-actor-leak",
            "excluded-slot-unchanged-after-included-failure",
            "included-default-restored",
            "included-failure-fixture-cleaned",
            "failure-session-preserved",

            "excluded-success-fixture-created",
            "excluded-success-activity-configured",
            "excluded-invalid-default-installed",
            "excluded-baseline-captured-before-success",
            "excluded-success-request-started",
            "included-projection-ready",
            "included-only-materialized",
            "active-lifecycle-included-only",
            "excluded-slot-unchanged-while-active",
            "excluded-success-request-completed",
            "second-activity-cleared",
            "release-lifecycle-included-only",
            "included-actor-released",
            "excluded-slot-unchanged-after-release",
            "final-session-authority-preserved",
            "excluded-default-restored",
            "joining-closed",
            "success-fixture-cleaned"
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
            object preparationModule = null;
            PlayerSlotProfile includedSlotProfile = null;
            PlayerSlotProfile excludedSlotProfile = null;
            ActorProfile includedDefaultActor = null;
            ActorProfile excludedDefaultActor = null;
            FieldInfo defaultActorField = null;
            ActorProfile includedInvalidActor = null;
            GameObject includedInvalidHost = null;
            ActorProfile excludedInvalidActor = null;
            GameObject excludedInvalidHost = null;
            bool includedDefaultOverridden = false;
            bool excludedDefaultOverridden = false;
            bool joiningOpen = false;
            LocalPlayerJoinResult includedJoin = null;
            LocalPlayerJoinResult excludedJoin = null;
            QaActivityEntryReadinessFixture fixture = null;
            var includedFailureRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-9-included-failure");
            var excludedSuccessRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "qa-if-m07-12b-9-excluded-success");

            int joinedSessionRevision = 0;
            int finalSessionRevision = 0;
            string excludedFailureBaseline = string.Empty;
            string excludedSuccessBaseline = string.Empty;
            int successfulOccurrence = 0;

            try
            {
                Require(EditorApplication.isPlaying,
                    "IF-M07-12B-9 requires Play Mode.");
                cases.Complete("play-mode-required");

                QaM07InternalReconcileSetup
                    .RequirePreparedForCurrentPlayMode();
                cases.Complete("setup-confirmed");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(host != null && host.State.GameFlowStarted,
                    "IF-M07-12B-9 requires the official started FrameworkRuntimeHost.");
                cases.Complete("official-host-resolved");

                authoring = ResolveProvisioningAuthoring(host);
                Require(authoring != null &&
                    authoring.RuntimeReady &&
                    authoring.PlayerInputManager != null,
                    "IF-M07-12B-9 could not resolve ready Local Player provisioning authoring.");
                cases.Complete("provisioning-authoring-resolved");

                ResolveTwoConfiguredSlotProfiles(
                    out includedSlotProfile,
                    out excludedSlotProfile);
                includedDefaultActor =
                    includedSlotProfile.DefaultActorProfile;
                excludedDefaultActor =
                    excludedSlotProfile.DefaultActorProfile;
                Require(includedDefaultActor != null &&
                    includedDefaultActor.LogicalActorHostPrefab != null &&
                    excludedDefaultActor != null &&
                    excludedDefaultActor.LogicalActorHostPrefab != null &&
                    includedSlotProfile.PlayerSlotId.IsValid &&
                    excludedSlotProfile.PlayerSlotId.IsValid &&
                    includedSlotProfile.PlayerSlotId !=
                        excludedSlotProfile.PlayerSlotId,
                    "IF-M07-12B-9 requires two distinct Slots with valid default Actor fixtures.");
                defaultActorField = ResolveDefaultActorField();
                cases.Complete("two-slot-fixture-confirmed");

                preparationModule = ResolveHostComponent(
                    host,
                    PreparationModuleTypeName);
                Require(preparationModule != null,
                    "IF-M07-12B-9 could not resolve Player Actor preparation module.");
                cases.Complete("preparation-module-resolved");

                PlayerParticipationSnapshot initialSession =
                    authoring.RuntimeSnapshot;
                Require(initialSession != null &&
                    initialSession.IsInitialized &&
                    initialSession.Revision > 0 &&
                    CountJoined(initialSession) == 0 &&
                    authoring.PlayerInputManager.playerCount == 0,
                    "IF-M07-12B-9 is one-shot. Enter a fresh Play Mode with no joined Players.");
                cases.Complete("fresh-session-confirmed");

                PlayerParticipationOperationResult open =
                    authoring.OpenJoining(
                        nameof(
                            QaM07IncludedExcludedFailureReleaseScopeRegression),
                        "qa-if-m07-12b-9-open-joining");
                Require(open != null &&
                    open.Completed &&
                    open.Snapshot.JoiningOpen &&
                    authoring.PlayerInputManager.joiningEnabled,
                    open != null
                        ? open.ToDiagnosticString()
                        : "Opening joining returned no result.");
                joiningOpen = true;
                cases.Complete("joining-opened");

                includedJoin = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(
                            QaM07IncludedExcludedFailureReleaseScopeRegression),
                        "qa-if-m07-12b-9-join-included"));
                Require(includedJoin != null &&
                    includedJoin.Succeeded &&
                    includedJoin.HasCommitEvidence &&
                    includedJoin.HasAssignmentEvidence &&
                    includedJoin.Slot.PlayerSlotId ==
                        includedSlotProfile.PlayerSlotId &&
                    includedJoin.LocalPlayerHost != null &&
                    includedJoin.PlayerInput != null,
                    includedJoin != null
                        ? includedJoin.ToDiagnosticString()
                        : "Included Player Join returned no result.");
                cases.Complete("first-player-joined");

                excludedJoin = RequestJoinSharingPrimaryDevice(
                    authoring,
                    includedJoin.LocalPlayerHost,
                    nameof(
                        QaM07IncludedExcludedFailureReleaseScopeRegression),
                    "qa-if-m07-12b-9-join-excluded",
                    out object sharedJoinDevice);
                Require(excludedJoin != null &&
                    excludedJoin.Succeeded &&
                    excludedJoin.HasCommitEvidence &&
                    excludedJoin.HasAssignmentEvidence &&
                    excludedJoin.Slot.PlayerSlotId ==
                        excludedSlotProfile.PlayerSlotId &&
                    excludedJoin.LocalPlayerHost != null &&
                    excludedJoin.PlayerInput != null &&
                    !ReferenceEquals(
                        includedJoin.LocalPlayerHost,
                        excludedJoin.LocalPlayerHost) &&
                    includedJoin.PlayerInput.playerIndex !=
                        excludedJoin.PlayerInput.playerIndex &&
                    PlayerInputContainsDevice(
                        includedJoin.PlayerInput,
                        sharedJoinDevice) &&
                    PlayerInputContainsDevice(
                        excludedJoin.PlayerInput,
                        sharedJoinDevice),
                    excludedJoin != null
                        ? excludedJoin.ToDiagnosticString()
                        : "Excluded Player shared-device Join returned no result.");
                cases.Complete("second-player-joined");

                PlayerParticipationSnapshot joinedSession =
                    await AwaitSessionAsync(
                        authoring,
                        snapshot =>
                            CountJoined(snapshot) == 2 &&
                            authoring.PlayerInputManager.playerCount == 2,
                        "Two joined Players did not become Session-authoritative",
                        FrameBudget);
                joinedSessionRevision = joinedSession.Revision;
                Require(includedJoin.LocalPlayerHost.IsJoined &&
                    excludedJoin.LocalPlayerHost.IsJoined &&
                    CountActors(includedJoin.LocalPlayerHost) == 0 &&
                    CountActors(excludedJoin.LocalPlayerHost) == 0,
                    "Initial two-Player Session has invalid Host or Actor state.");
                cases.Complete("two-player-session-confirmed");

                // Scenario A: the included Slot is invalid. Entry must fail,
                // and the excluded Slot must remain completely untouched.
                fixture =
                    await QaActivityEntryReadinessFixture.CreateAsync();
                cases.Complete("included-failure-fixture-created");

                ActivityAsset failureActivity = fixture.CreateActivity(
                    "qa.m07.12b9.included-failure",
                    "Q3 M07 Included Slot Failure Scope",
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup.ContentScenePath);
                ConfigureExplicitLogicalActorProjection(
                    failureActivity,
                    includedSlotProfile);
                cases.Complete("included-failure-activity-configured");

                includedInvalidActor = CreateInvalidActorClone(
                    includedDefaultActor,
                    "Q3 M07 Included Invalid Logical Actor Host",
                    "Q3 M07 Included Invalid Actor",
                    out includedInvalidHost);
                defaultActorField.SetValue(
                    includedSlotProfile,
                    includedInvalidActor);
                includedDefaultOverridden = true;
                Require(ReferenceEquals(
                        includedSlotProfile.DefaultActorProfile,
                        includedInvalidActor) &&
                    includedInvalidHost != null &&
                    includedInvalidHost.GetComponentInChildren<
                        PlayerActorDeclaration>(true) == null,
                    "The structurally invalid included default Actor was not installed.");
                cases.Complete("included-invalid-default-installed");

                excludedFailureBaseline = SlotFingerprint(
                    authoring.RuntimeSnapshot,
                    excludedSlotProfile.PlayerSlotId);
                cases.Complete(
                    "excluded-baseline-captured-before-failure");

                includedFailureRequest.Attach(
                    fixture.Activities.RequestActivityAsync(
                        failureActivity,
                        nameof(
                            QaM07IncludedExcludedFailureReleaseScopeRegression),
                        "qa-if-m07-12b-9-included-failure"));
                cases.Complete("included-failure-request-started");

                FrameworkActivityRequestResult failureTerminal =
                    await AwaitTerminalAllowingFixtureReadinessAsync(
                        fixture,
                        includedFailureRequest,
                        FrameBudget);
                Require(!failureTerminal.Succeeded,
                    "An Activity with an invalid included Slot was admitted. " +
                    failureTerminal.Message);
                cases.Complete("included-failure-terminal-rejected");

                ActivityPlayerActorLifecycleSnapshot failedLifecycle =
                    GetLifecycleSnapshot(preparationModule);
                string failureDiagnostic =
                    (failureTerminal.Message ?? string.Empty) + " " +
                    (failedLifecycle?.ToDiagnosticString() ?? string.Empty) +
                    " " +
                    (authoring.ManagerProvisionedLifecycleSnapshot
                        ?.ToDiagnosticString() ?? string.Empty);
                Require(failedLifecycle != null &&
                    failedLifecycle.Failed &&
                    failedLifecycle.ProjectedSlotCount == 1 &&
                    failureDiagnostic.IndexOf(
                        includedSlotProfile.PlayerSlotId.StableText,
                        StringComparison.Ordinal) >= 0 &&
                    !LifecycleContainsSlot(
                        failedLifecycle,
                        excludedSlotProfile.PlayerSlotId),
                    "Included failure was not scoped to the explicit included Slot. " +
                    failureDiagnostic);
                cases.Complete("included-failure-diagnostic-scoped");

                Require(fixture.RuntimeHost.State.CurrentActivity != null &&
                    fixture.RuntimeHost.State.CurrentActivity
                        .HasSameIdentity(failureActivity),
                    "Included Slot failure did not preserve the committed target Activity authority.");
                cases.Complete("included-failure-authority-preserved");

                Require(CountActors(includedJoin.LocalPlayerHost) == 0 &&
                    CountActors(excludedJoin.LocalPlayerHost) == 0,
                    "Included failure leaked an Actor under an included or excluded Host.");
                cases.Complete("included-failure-no-actor-leak");

                string excludedAfterFailure = SlotFingerprint(
                    authoring.RuntimeSnapshot,
                    excludedSlotProfile.PlayerSlotId);
                Require(string.Equals(
                        excludedFailureBaseline,
                        excludedAfterFailure,
                        StringComparison.Ordinal),
                    "The excluded Slot changed while the included Slot failed. " +
                    $"before='{excludedFailureBaseline}' " +
                    $"after='{excludedAfterFailure}'.");
                cases.Complete(
                    "excluded-slot-unchanged-after-included-failure");

                defaultActorField.SetValue(
                    includedSlotProfile,
                    includedDefaultActor);
                includedDefaultOverridden = false;
                Require(ReferenceEquals(
                        includedSlotProfile.DefaultActorProfile,
                        includedDefaultActor),
                    "Included Slot default Actor was not restored.");
                cases.Complete("included-default-restored");

                await fixture.DisposeAsync();
                Require(MatchesInitialAuthority(fixture),
                    "Included failure fixture cleanup did not restore the initial Activity authority.");
                fixture = null;
                cases.Complete("included-failure-fixture-cleaned");

                Require(TryFindSlot(
                        authoring.RuntimeSnapshot,
                        includedSlotProfile.PlayerSlotId,
                        out PlayerSlotRuntimeSnapshot includedAfterFailure) &&
                    !includedAfterFailure.HasSelectedActor &&
                    CountJoined(authoring.RuntimeSnapshot) == 2 &&
                    authoring.PlayerInputManager.playerCount == 2 &&
                    includedJoin.LocalPlayerHost.IsJoined &&
                    excludedJoin.LocalPlayerHost.IsJoined &&
                    CountActors(includedJoin.LocalPlayerHost) == 0 &&
                    CountActors(excludedJoin.LocalPlayerHost) == 0,
                    "The failed occurrence changed Session-owned participation, retained its failed selection or changed Host state.");
                cases.Complete("failure-session-preserved");

                // Scenario B: only the excluded Slot is invalid. The Activity
                // must prepare and release the valid included Slot exclusively.
                fixture =
                    await QaActivityEntryReadinessFixture.CreateAsync();
                fixture.ExpectParticipantPreparationCycles(1);
                cases.Complete("excluded-success-fixture-created");

                ActivityAsset successActivity = fixture.CreateActivity(
                    "qa.m07.12b9.excluded-invalid-success",
                    "Q3 M07 Excluded Slot Isolation",
                    ActivityEntryReadinessPolicy.WaitVisible,
                    ActivityVisualTransitionMode.Fade,
                    TransitionGateMode.InputInteractionAndGameplay,
                    QaM07InternalReconcileSetup.ContentScenePath);
                ConfigureExplicitLogicalActorProjection(
                    successActivity,
                    includedSlotProfile);
                cases.Complete("excluded-success-activity-configured");

                excludedInvalidActor = CreateInvalidActorClone(
                    excludedDefaultActor,
                    "Q3 M07 Excluded Invalid Logical Actor Host",
                    "Q3 M07 Excluded Invalid Actor",
                    out excludedInvalidHost);
                defaultActorField.SetValue(
                    excludedSlotProfile,
                    excludedInvalidActor);
                excludedDefaultOverridden = true;
                Require(ReferenceEquals(
                        excludedSlotProfile.DefaultActorProfile,
                        excludedInvalidActor) &&
                    excludedInvalidHost != null &&
                    excludedInvalidHost.GetComponentInChildren<
                        PlayerActorDeclaration>(true) == null,
                    "The structurally invalid excluded default Actor was not installed.");
                cases.Complete("excluded-invalid-default-installed");

                excludedSuccessBaseline = SlotFingerprint(
                    authoring.RuntimeSnapshot,
                    excludedSlotProfile.PlayerSlotId);
                cases.Complete(
                    "excluded-baseline-captured-before-success");

                excludedSuccessRequest.Attach(
                    fixture.Activities.RequestActivityAsync(
                        successActivity,
                        nameof(
                            QaM07IncludedExcludedFailureReleaseScopeRegression),
                        "qa-if-m07-12b-9-excluded-success"));
                cases.Complete("excluded-success-request-started");

                await AwaitParticipantCycleOrTerminalAsync(
                    fixture,
                    excludedSuccessRequest,
                    1,
                    FrameBudget);

                ManagerProvisionedPlayerLifecycleSnapshot projected =
                    await AwaitSnapshotAsync(
                        authoring,
                        snapshot =>
                            snapshot != null &&
                            snapshot.IsAvailable &&
                            string.Equals(
                                snapshot.ActivityName,
                                successActivity.ActivityName,
                                StringComparison.Ordinal) &&
                            snapshot.ActivityOccurrence > 0 &&
                            snapshot.HostCount == 2 &&
                            ProjectsOnlyMaterializedSlot(
                                snapshot,
                                includedSlotProfile),
                        "The explicit included Slot did not become the sole materialized public projection",
                        FrameBudget);
                successfulOccurrence = projected.ActivityOccurrence;
                cases.Complete("included-projection-ready");

                Require(CountActors(includedJoin.LocalPlayerHost) == 1 &&
                    CountActors(excludedJoin.LocalPlayerHost) == 0,
                    "Actor materialization escaped the explicit included Slot.");
                cases.Complete("included-only-materialized");

                ActivityPlayerActorLifecycleSnapshot activeLifecycle =
                    GetLifecycleSnapshot(preparationModule);
                Require(activeLifecycle != null &&
                    activeLifecycle.Succeeded &&
                    activeLifecycle.ProjectedSlotCount == 1 &&
                    activeLifecycle.PreparedCount == 1 &&
                    activeLifecycle.Slots.Count == 1 &&
                    activeLifecycle.Slots[0].PlayerSlotId ==
                        includedSlotProfile.PlayerSlotId &&
                    activeLifecycle.Slots[0].PreparationToken.IsValid &&
                    !LifecycleContainsSlot(
                        activeLifecycle,
                        excludedSlotProfile.PlayerSlotId),
                    "Active lifecycle evidence is not restricted to the included Slot. " +
                    activeLifecycle?.ToDiagnosticString());
                var includedPreparationToken =
                    activeLifecycle.Slots[0].PreparationToken;
                cases.Complete("active-lifecycle-included-only");

                string excludedWhileActive = SlotFingerprint(
                    authoring.RuntimeSnapshot,
                    excludedSlotProfile.PlayerSlotId);
                Require(string.Equals(
                        excludedSuccessBaseline,
                        excludedWhileActive,
                        StringComparison.Ordinal),
                    "The excluded Slot changed while the included Slot was active. " +
                    $"before='{excludedSuccessBaseline}' " +
                    $"after='{excludedWhileActive}'.");
                cases.Complete("excluded-slot-unchanged-while-active");

                Require(fixture.Participant != null &&
                    fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing,
                    "Success fixture aggregate readiness participant is not Preparing.");
                fixture.Participant.CompletePreparation();

                FrameworkActivityRequestResult successTerminal =
                    await AwaitOwnedTerminalAsync(
                        excludedSuccessRequest,
                        FrameBudget);
                Require(successTerminal.Succeeded &&
                    fixture.RuntimeHost.State.CurrentActivity != null &&
                    fixture.RuntimeHost.State.CurrentActivity
                        .HasSameIdentity(successActivity),
                    !string.IsNullOrWhiteSpace(successTerminal.Message)
                        ? successTerminal.Message
                        : "The Activity with an invalid excluded Slot did not succeed.");
                cases.Complete("excluded-success-request-completed");

                FrameworkActivityRequestResult clear =
                    await fixture.Activities.ClearActivityAsync(
                        nameof(
                            QaM07IncludedExcludedFailureReleaseScopeRegression),
                        "qa-if-m07-12b-9-clear-success");
                Require(clear.Succeeded,
                    !string.IsNullOrWhiteSpace(clear.Message)
                        ? clear.Message
                        : "Clearing the successful scoped Activity did not succeed.");
                cases.Complete("second-activity-cleared");

                ActivityPlayerActorLifecycleSnapshot releasedLifecycle =
                    GetLifecycleSnapshot(preparationModule);
                Require(releasedLifecycle != null &&
                    releasedLifecycle.Status ==
                        ActivityPlayerActorLifecycleStatus.SucceededExited &&
                    releasedLifecycle.ProjectedSlotCount == 1 &&
                    releasedLifecycle.PreparedCount == 1 &&
                    releasedLifecycle.ReleasedCount == 1 &&
                    releasedLifecycle.Slots.Count == 1 &&
                    releasedLifecycle.Slots[0].PlayerSlotId ==
                        includedSlotProfile.PlayerSlotId &&
                    releasedLifecycle.Slots[0].Released &&
                    releasedLifecycle.Slots[0].PreparationToken.Equals(
                        includedPreparationToken) &&
                    !LifecycleContainsSlot(
                        releasedLifecycle,
                        excludedSlotProfile.PlayerSlotId),
                    "Release evidence escaped the explicit included Slot. " +
                    releasedLifecycle?.ToDiagnosticString());
                cases.Complete("release-lifecycle-included-only");

                Require(CountActors(includedJoin.LocalPlayerHost) == 0 &&
                    CountActors(excludedJoin.LocalPlayerHost) == 0,
                    "Scoped release did not release the included Actor or touched the excluded Host.");
                cases.Complete("included-actor-released");

                string excludedAfterRelease = SlotFingerprint(
                    authoring.RuntimeSnapshot,
                    excludedSlotProfile.PlayerSlotId);
                Require(string.Equals(
                        excludedSuccessBaseline,
                        excludedAfterRelease,
                        StringComparison.Ordinal),
                    "The excluded Slot changed during included Slot release. " +
                    $"before='{excludedSuccessBaseline}' " +
                    $"after='{excludedAfterRelease}'.");
                cases.Complete("excluded-slot-unchanged-after-release");

                PlayerParticipationSnapshot finalSession =
                    authoring.RuntimeSnapshot;
                ManagerProvisionedPlayerLifecycleSnapshot
                    finalAuthorityProjection =
                        authoring.ManagerProvisionedLifecycleSnapshot;
                Require(finalSession != null &&
                    finalSession.IsInitialized &&
                    finalAuthorityProjection != null &&
                    finalAuthorityProjection.IsAvailable,
                    "Final Session or public lifecycle authority is unavailable after scoped release.");
                finalSessionRevision = finalSession.Revision;
                Require(CountJoined(finalSession) == 2 &&
                    authoring.PlayerInputManager.playerCount == 2 &&
                    includedJoin.LocalPlayerHost.IsJoined &&
                    excludedJoin.LocalPlayerHost.IsJoined &&
                    finalAuthorityProjection.HostCount == 2,
                    "Scoped release changed Session-owned Join, PlayerInput or technical Host authority. " +
                    $"sessionRevision='{finalSession.Revision}' " +
                    $"joined='{CountJoined(finalSession)}' " +
                    $"playerInputs='{authoring.PlayerInputManager.playerCount}' " +
                    $"public='{finalAuthorityProjection.ToDiagnosticString()}'.");
                cases.Complete("final-session-authority-preserved");

                defaultActorField.SetValue(
                    excludedSlotProfile,
                    excludedDefaultActor);
                excludedDefaultOverridden = false;
                Require(ReferenceEquals(
                        excludedSlotProfile.DefaultActorProfile,
                        excludedDefaultActor),
                    "Excluded Slot default Actor was not restored.");
                cases.Complete("excluded-default-restored");

                PlayerParticipationOperationResult close =
                    authoring.CloseJoining(
                        nameof(
                            QaM07IncludedExcludedFailureReleaseScopeRegression),
                        "qa-if-m07-12b-9-close-joining");
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
                cases.Complete("success-fixture-cleaned");
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
                if (excludedSuccessRequest.HasOperation &&
                    !excludedSuccessRequest.ReachedTerminal)
                {
                    try
                    {
                        await excludedSuccessRequest.UnwindAsync(
                            () => FailPendingReadinessAsync(
                                host,
                                fixture,
                                "qa-if-m07-12b-9-success-unwind"));
                    }
                    catch (Exception exception)
                    {
                        failures.Add("success-operation-unwind", exception);
                    }
                }

                if (includedFailureRequest.HasOperation &&
                    !includedFailureRequest.ReachedTerminal)
                {
                    try
                    {
                        await includedFailureRequest.UnwindAsync(
                            () => FailPendingReadinessAsync(
                                host,
                                fixture,
                                "qa-if-m07-12b-9-failure-unwind"));
                    }
                    catch (Exception exception)
                    {
                        failures.Add("failure-operation-unwind", exception);
                    }
                }

                if (includedDefaultOverridden &&
                    defaultActorField != null &&
                    includedSlotProfile != null)
                {
                    try
                    {
                        defaultActorField.SetValue(
                            includedSlotProfile,
                            includedDefaultActor);
                    }
                    catch (Exception exception)
                    {
                        failures.Add("included-default-cleanup", exception);
                    }
                }

                if (excludedDefaultOverridden &&
                    defaultActorField != null &&
                    excludedSlotProfile != null)
                {
                    try
                    {
                        defaultActorField.SetValue(
                            excludedSlotProfile,
                            excludedDefaultActor);
                    }
                    catch (Exception exception)
                    {
                        failures.Add("excluded-default-cleanup", exception);
                    }
                }

                if (joiningOpen && authoring != null)
                {
                    try
                    {
                        PlayerParticipationOperationResult close =
                            authoring.CloseJoining(
                                nameof(
                                    QaM07IncludedExcludedFailureReleaseScopeRegression),
                                "qa-if-m07-12b-9-finally-close-joining");
                        if (close == null || !close.Completed)
                        {
                            throw new InvalidOperationException(
                                close != null
                                    ? close.ToDiagnosticString()
                                    : "Joining cleanup returned no result.");
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

                if (includedInvalidActor != null)
                {
                    UnityEngine.Object.Destroy(includedInvalidActor);
                }

                if (includedInvalidHost != null)
                {
                    UnityEngine.Object.Destroy(includedInvalidHost);
                }

                if (excludedInvalidActor != null)
                {
                    UnityEngine.Object.Destroy(excludedInvalidActor);
                }

                if (excludedInvalidHost != null)
                {
                    UnityEngine.Object.Destroy(excludedInvalidHost);
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
                    $"failureUnwind='{Escape(failures.Describe("failure-operation-unwind"))}' " +
                    $"successUnwind='{Escape(failures.Describe("success-operation-unwind"))}' " +
                    $"joiningCleanup='{Escape(failures.Describe("joining-cleanup"))}' " +
                    $"fixtureCleanup='{Escape(failures.Describe("fixture-cleanup"))}'.");
                throw failures.ToAggregate(
                    "IF-M07-12B-9 Included/Excluded failure and release scope regression failed.");
            }

            PlayerParticipationSnapshot completedSession =
                authoring.RuntimeSnapshot;
            ManagerProvisionedPlayerLifecycleSnapshot completedProjection =
                authoring.ManagerProvisionedLifecycleSnapshot;
            Require(completedSession != null &&
                completedSession.IsInitialized &&
                completedProjection != null &&
                completedProjection.IsAvailable,
                "Completed IF-M07-12B-9 authority snapshots are unavailable.");
            Debug.Log(
                $"{Prefix} status='Passed' " +
                $"cases='{cases.Count}' " +
                $"successfulOccurrence='{successfulOccurrence}' " +
                $"joinedSessionRevision='{joinedSessionRevision}' " +
                $"finalSessionRevision='{finalSessionRevision}' " +
                $"joined='{CountJoined(completedSession)}' " +
                $"playerInputs='{authoring.PlayerInputManager.playerCount}' " +
                $"hostCount='{completedProjection.HostCount}' " +
                $"projectedSlots='{completedProjection.SlotCount}' " +
                "proof='IncludedFailureBlocks,ExcludedFailureIgnored,ExcludedSlotImmutable,IncludedOnlyMaterialized,IncludedOnlyReleased,SessionAuthorityPreserved' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static async Task<FrameworkActivityRequestResult>
            AwaitTerminalAllowingFixtureReadinessAsync(
                QaActivityEntryReadinessFixture fixture,
                QaOwnedAsyncOperation<FrameworkActivityRequestResult> owned,
                int frameBudget)
        {
            Require(fixture != null &&
                owned != null &&
                owned.HasOperation &&
                frameBudget > 0,
                "Failure wait requires a fixture, owned operation and positive frame budget.");

            bool fixtureCompleted = false;
            for (int frame = 0; frame < frameBudget; frame++)
            {
                if (owned.IsCompleted)
                {
                    return await owned.AwaitTerminalAsync();
                }

                if (!fixtureCompleted &&
                    fixture.Participant != null &&
                    fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing)
                {
                    fixture.Participant.CompletePreparation();
                    fixtureCompleted = true;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                $"Included failure request did not terminate within '{frameBudget}' frames.");
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

        private static bool ProjectsOnlyMaterializedSlot(
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
                    StringComparison.Ordinal) &&
                snapshot.Slots[0].HasTechnicalHost &&
                snapshot.Slots[0].LogicalActorPrepared &&
                snapshot.Slots[0].PhysicalActorMaterialized;
        }

        private static void ConfigureExplicitLogicalActorProjection(
            ActivityAsset activity,
            PlayerSlotProfile includedSlot)
        {
            Require(activity != null &&
                includedSlot != null &&
                includedSlot.PlayerSlotId.IsValid,
                "Explicit projection configuration requires an Activity and valid included Slot.");

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
                PlayerParticipationRequirementLevel
                    .LogicalActorsPrepared.ToString());

            SerializedProperty explicitSlots = RequireProperty(
                serialized,
                "playerParticipationExplicitSlotProfiles");
            explicitSlots.arraySize = 1;
            explicitSlots.GetArrayElementAtIndex(0)
                .objectReferenceValue = includedSlot;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(activity.PlayerParticipationProjectionMode ==
                    ActivityParticipationProjectionMode.ExplicitSlots &&
                activity.PlayerParticipationRequirementLevel ==
                    PlayerParticipationRequirementLevel
                        .LogicalActorsPrepared,
                "Runtime explicit LogicalActorsPrepared projection did not apply.");
        }

        private static ActorProfile CreateInvalidActorClone(
            ActorProfile template,
            string logicalHostName,
            string actorName,
            out GameObject invalidLogicalHost)
        {
            Require(template != null,
                "Invalid Actor fixture requires a template ActorProfile.");

            invalidLogicalHost =
                new GameObject(logicalHostName);
            invalidLogicalHost.SetActive(false);

            ActorProfile clone =
                UnityEngine.Object.Instantiate(template);
            clone.name = actorName;

            var serialized = new SerializedObject(clone);
            SerializedProperty prefab = RequireProperty(
                serialized,
                "logicalActorHostPrefab");
            prefab.objectReferenceValue = invalidLogicalHost;
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
                "PlayerSlotProfile defaultActorProfile field was not found.");
            return field;
        }

        private static void ResolveTwoConfiguredSlotProfiles(
            out PlayerSlotProfile first,
            out PlayerSlotProfile second)
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            GameApplicationAsset application =
                settings != null
                    ? settings.ActiveGameApplication
                    : null;

            first = null;
            second = null;
            bool firstResolved = application != null &&
                QaPlayerSessionQaSupport.TryGetSupportedSlot(application, 0, out first);
            bool secondResolved = application != null &&
                QaPlayerSessionQaSupport.TryGetSupportedSlot(application, 1, out second);
            Require(firstResolved &&
                secondResolved &&
                first != null &&
                second != null &&
                first.PlayerSlotId.IsValid &&
                second.PlayerSlotId.IsValid,
                "Could not resolve two configured local Player Slot Profiles.");
        }

        private static LocalPlayerProvisioningAuthoring
            ResolveProvisioningAuthoring(FrameworkRuntimeHost host)
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
            bool succeeded = method.Invoke(
                    host,
                    arguments) is bool value &&
                value;
            LocalPlayerProvisioningAuthoring authoring =
                arguments[0] as LocalPlayerProvisioningAuthoring;
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

        private static object ResolveHostComponent(
            FrameworkRuntimeHost host,
            string typeName)
        {
            Require(host != null &&
                !string.IsNullOrWhiteSpace(typeName),
                "Host component resolution requires a host and type name.");

            Component[] components = host.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component != null &&
                    string.Equals(
                        component.GetType().FullName,
                        typeName,
                        StringComparison.Ordinal))
                {
                    return component;
                }
            }

            return null;
        }

        private static ActivityPlayerActorLifecycleSnapshot
            GetLifecycleSnapshot(object preparationModule)
        {
            Require(preparationModule != null,
                "Lifecycle snapshot requires the preparation module.");
            object[] arguments = { null };
            MethodInfo method = FindMethod(
                preparationModule.GetType(),
                "TryGetActivityPlayerActorLifecycleSnapshot",
                1);
            bool succeeded = method.Invoke(
                    preparationModule,
                    arguments) is bool value &&
                value;
            ActivityPlayerActorLifecycleSnapshot snapshot =
                arguments[0] as ActivityPlayerActorLifecycleSnapshot;
            Require(succeeded && snapshot != null,
                "Activity Player Actor lifecycle snapshot is unavailable.");
            return snapshot;
        }

        private static bool LifecycleContainsSlot(
            ActivityPlayerActorLifecycleSnapshot lifecycle,
            PlayerSlotId playerSlotId)
        {
            if (lifecycle == null || !playerSlotId.IsValid)
            {
                return false;
            }

            for (int index = 0;
                 index < lifecycle.Slots.Count;
                 index++)
            {
                if (lifecycle.Slots[index].PlayerSlotId == playerSlotId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesInitialAuthority(
            QaActivityEntryReadinessFixture fixture)
        {
            if (fixture == null)
            {
                return false;
            }

            ActivityAsset current = fixture.RuntimeHost.State.CurrentActivity;
            if (fixture.InitialActivity == null)
            {
                return current == null;
            }

            return current != null &&
                current.HasSameIdentity(fixture.InitialActivity);
        }

        private static string SlotFingerprint(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            Require(snapshot != null &&
                snapshot.IsInitialized &&
                playerSlotId.IsValid,
                "Slot fingerprint requires an initialized Session and valid Slot ID.");
            Require(TryFindSlot(
                    snapshot,
                    playerSlotId,
                    out PlayerSlotRuntimeSnapshot slot),
                $"Session has no Slot '{playerSlotId.StableText}'.");

            return
                $"slot='{slot.PlayerSlotId.StableText}'|" +
                $"allocation='{slot.AllocationState}'|" +
                $"joined='{slot.IsJoined}'|" +
                $"revision='{slot.Revision}'|" +
                $"selectionRevision='{slot.SelectionRevision}'|" +
                $"selected='{slot.HasSelectedActor}'|" +
                $"actor='{(slot.SelectedActorProfileId.IsValid ? slot.SelectedActorProfileId.StableText : string.Empty)}'";
        }

        private static bool TryFindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot slot)
        {
            if (snapshot != null && playerSlotId.IsValid)
            {
                for (int index = 0;
                     index < snapshot.Slots.Count;
                     index++)
                {
                    PlayerSlotRuntimeSnapshot candidate =
                        snapshot.Slots[index];
                    if (candidate.PlayerSlotId == playerSlotId)
                    {
                        slot = candidate;
                        return true;
                    }
                }
            }

            slot = default;
            return false;
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
                "Shared-device Join requires ready Local Player provisioning.");
            Require(primaryHost != null &&
                primaryHost.IsJoined &&
                primaryHost.HasJoinedSlot,
                "Shared-device Join requires the current registered primary Host.");

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
                "Secondary IF-M07-12B-9 Join requires one explicit device from the primary PlayerInput.");

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
            LocalPlayerJoinResult result =
                requestJoin.Invoke(
                    authoring,
                    new[] { request }) as
                        LocalPlayerJoinResult;
            Require(result != null,
                "Shared-device Local Player Join returned no result.");

            Debug.Log(
                $"{Prefix} phase='SecondJoinRequested' " +
                $"device='{DescribeInputDevice(sharedDevice)}' " +
                $"managerPlayers='{authoring.PlayerInputManager.playerCount}' " +
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

            return
                $"name='{ReadProperty(device, "name")}' " +
                $"layout='{ReadProperty(device, "layout")}' " +
                $"deviceId='{ReadProperty(device, "deviceId")}' " +
                $"added='{ReadProperty(device, "added")}'";
        }

        private static object ReadProperty(
            object target,
            string propertyName)
        {
            Require(target != null,
                $"Cannot read property '{propertyName}' from a null target.");
            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    InstanceAny);
            Require(property != null,
                $"Property '{propertyName}' was not found on '{target.GetType().FullName}'.");
            return property.GetValue(target);
        }

        private static int ReadInt(
            object target,
            string propertyName)
        {
            object value =
                ReadProperty(
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
            object value =
                ReadProperty(
                    target,
                    propertyName);
            Require(value is bool,
                $"Property '{propertyName}' on '{target.GetType().FullName}' is not Boolean.");
            return (bool)value;
        }

        private static Type ResolveType(string fullName)
        {
            Type type = Type.GetType(fullName, false);
            if (type != null)
            {
                return type;
            }

            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0;
                 index < assemblies.Length;
                 index++)
            {
                type = assemblies[index].GetType(
                    fullName,
                    false);
                if (type != null)
                {
                    return type;
                }
            }

            throw new TypeLoadException(
                $"Type '{fullName}' was not found in loaded assemblies.");
        }

        private static MethodInfo FindMethodBySignature(
            Type type,
            string methodName,
            Type parameterType)
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
                    MethodInfo candidate = methods[index];
                    ParameterInfo[] parameters =
                        candidate.GetParameters();
                    if (string.Equals(
                            candidate.Name,
                            methodName,
                            StringComparison.Ordinal) &&
                        parameters.Length == 1 &&
                        parameters[0].ParameterType ==
                            parameterType)
                    {
                        return candidate;
                    }
                }
            }

            throw new MissingMethodException(
                type.FullName,
                $"{methodName}({parameterType.FullName})");
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
                    MethodInfo candidate = methods[index];
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

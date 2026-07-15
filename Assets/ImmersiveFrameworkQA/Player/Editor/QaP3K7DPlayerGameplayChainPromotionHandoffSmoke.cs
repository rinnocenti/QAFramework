using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// One-shot Play Mode proof for the synchronous reversible cutover from the current
    /// P3J/P3K Player chain to one P3K.7C target Activity candidate.
    /// </summary>
    public static class QaP3K7DPlayerGameplayChainPromotionHandoffSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.7D Run Player Gameplay Chain Promotion Handoff Smoke";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";
        private const string CandidateModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorCandidateRuntimeHostModule";
        private const string OccupancyContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayOccupancyRuntimeContext";
        private const string InputContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayInputBindingRuntimeContext";
        private const string CameraContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayCameraEligibilityRuntimeContext";
        private const string AdmissionContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayAdmissionRuntimeContext";
        private const string HandoffContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayChainHandoffRuntimeContext";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "P3K.7D handoff smoke must run in Play Mode.");
                completed.Add("play-mode-required");

                LocalPlayerProvisioningAuthoring authoring = ResolveAuthoring();
                AssertTrue(authoring.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " +
                    authoring.RuntimeDiagnostic);
                completed.Add("real-provisioning-runtime-ready");

                object runtimeHost = ResolveCurrentRuntimeHost();
                object preparationModule = ResolvePreparationModule(runtimeHost);
                PlayerActorPreparationRuntimeHostSnapshot initialPreparation =
                    GetPreparationSnapshot(preparationModule);
                AssertTrue(initialPreparation.IsInitialized,
                    "P3J preparation module is not initialized. " +
                    initialPreparation.Diagnostic);
                AssertEqual(authoring.RuntimeSnapshot.ContextId,
                    initialPreparation.SessionContextId,
                    "Participation and preparation Session identities differ.");
                completed.Add("p3j-preparation-module-ready");

                PlayerInputManager manager = authoring.PlayerInputManager;
                AssertNotNull(manager, "Provisioning authoring has no PlayerInputManager.");
                AssertEqual(0, manager.playerCount,
                    "P3K.7D smoke is one-shot. Re-enter Play Mode before running again.");
                AssertEqual(0, authoring.RuntimeSnapshot.JoinedCount,
                    "Session already contains a Joined Player.");
                AssertEqual(0, initialPreparation.PreparedCount,
                    "P3J preparation already contains an active Actor.");
                completed.Add("initial-runtime-state-clean");

                PlayerParticipationOperationResult opened =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryOpenJoining",
                        nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                        "gameplay-chain-handoff");
                AssertTrue(opened.Completed && opened.Snapshot.JoiningOpen,
                    "Opening joining failed. " + opened.ToDiagnosticString());
                completed.Add("joining-opened");

                LocalPlayerJoinResult joined = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                        "gameplay-chain-handoff"));
                AssertNotNull(joined, "Real local Player join returned no result.");
                AssertTrue(joined.Succeeded,
                    "Real local Player join failed. " + joined.ToDiagnosticString());
                completed.Add("real-local-player-joined");

                LocalPlayerHostAuthoring stableHost = joined.LocalPlayerHost;
                PlayerInput stablePlayerInput = joined.PlayerInput;
                PlayerSlotId slotId = joined.Slot.PlayerSlotId;
                AssertNotNull(stableHost, "Join result has no stable Local Player Host.");
                AssertNotNull(stablePlayerInput, "Join result has no stable PlayerInput.");
                AssertTrue(stableHost.IsJoined && stableHost.JoinedPlayerSlotId == slotId,
                    "Stable host does not retain matching Joined Slot evidence.");
                AssertSame(stablePlayerInput, stableHost.PlayerInput,
                    "Stable host does not own the joined PlayerInput.");
                completed.Add("stable-host-and-playerinput-confirmed");

                PlayerActorSelectionResult selected = Invoke<PlayerActorSelectionResult>(
                    preparationModule,
                    "TrySelectDefaultActor",
                    slotId,
                    joined.Slot.SelectionRevision,
                    nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                    "select-default-current-actor");
                AssertNotNull(selected, "Default Actor selection returned no result.");
                AssertTrue(selected.Succeeded && selected.SelectedActorProfile != null,
                    "Default Actor selection failed. " + selected.ToDiagnosticString());
                completed.Add("default-actor-selected");

                RuntimeScopeContext currentContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7d.current." + Guid.NewGuid().ToString("N"),
                    "P3K.7D Current Activity",
                    out object runtimeContent,
                    out Type runtimeContentType);
                AssertTrue(currentContext.IsValid,
                    "Current Activity scope context is invalid.");
                completed.Add("current-activity-scope-created");

                PlayerActorPreparationResult prepared =
                    Invoke<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryPrepareSelectedActor",
                        currentContext,
                        slotId,
                        nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                        "prepare-current-activity-actor");
                AssertNotNull(prepared, "Current Actor preparation returned no result.");
                AssertTrue(prepared.Succeeded && prepared.CurrentSummary.IsPrepared,
                    "Current Actor preparation failed. " + prepared.ToDiagnosticString());
                PlayerActorPreparationSummary currentPreparation = prepared.CurrentSummary;
                completed.Add("current-activity-actor-prepared");

                PlayerActorDeclaration currentDeclaration = ResolveDeclaration(
                    stableHost,
                    currentPreparation.Materialization.ActorId);
                AssertNotNull(currentDeclaration,
                    "Current prepared Actor declaration was not found.");
                AssertTrue(currentDeclaration.gameObject.activeInHierarchy,
                    "Current prepared Actor is not active.");
                AssertSame(stablePlayerInput, currentDeclaration.PlayerInput,
                    "Current Actor lost stable PlayerInput evidence.");
                completed.Add("current-actor-active-and-bound");

                UnityPlayerInputGateAdapter gateAdapter =
                    ConfigureGateAdapter(stableHost, stablePlayerInput);
                AssertNotNull(gateAdapter,
                    "Stable host Gate adapter could not be configured.");
                AssertSame(stablePlayerInput, gateAdapter.PlayerInput,
                    "Gate adapter does not target the stable-host PlayerInput.");
                AssertTrue(!string.IsNullOrEmpty(gateAdapter.GameplayActionMapName),
                    "Gate adapter has no gameplay action map.");
                completed.Add("stable-host-gate-adapter-configured");

                Type occupancyType = ResolveRuntimeType(OccupancyContextTypeName);
                Type inputType = ResolveRuntimeType(InputContextTypeName);
                Type cameraType = ResolveRuntimeType(CameraContextTypeName);
                Type admissionType = ResolveRuntimeType(AdmissionContextTypeName);
                object occupancyContext = CreateOccupancyContext(
                    occupancyType,
                    prepared.Snapshot);
                object inputContext = CreateInputContext(inputType, occupancyContext);
                object cameraContext = CreateCameraContext(
                    cameraType,
                    occupancyContext,
                    inputContext);
                object admissionContext = CreateAdmissionContext(
                    admissionType,
                    occupancyContext,
                    inputContext,
                    cameraContext);
                completed.Add("p3k-authorities-created");

                GameplayChainEvidence currentChain = BuildOptionalGameplayChain(
                    occupancyType,
                    occupancyContext,
                    inputType,
                    inputContext,
                    cameraType,
                    cameraContext,
                    admissionType,
                    admissionContext,
                    currentPreparation,
                    stableHost,
                    currentDeclaration,
                    gateAdapter,
                    "build-current-gameplay-chain");
                AssertTrue(currentChain.Occupancy.IsOccupied,
                    "Current P3K.2 occupancy was not created.");
                completed.Add("current-occupancy-created");
                AssertTrue(currentChain.Input.IsBound,
                    "Current P3K.3 input binding was not created.");
                completed.Add("current-input-binding-created");
                AssertTrue(currentChain.Camera.IsSkippedOptional,
                    "Current P3K.4 camera decision was not optional skip.");
                completed.Add("current-camera-optional-skip-created");
                AssertTrue(currentChain.Admission.IsAdmitted &&
                    currentChain.Admission.Token.IsValid,
                    "Current P3K.5 admission was not created.");
                completed.Add("current-gameplay-admission-created");

                object candidateModule = AttachCandidateModule(runtimeHost);
                PlayerActorCandidateRuntimeHostSnapshot initialCandidate =
                    GetCandidateSnapshot(candidateModule);
                AssertTrue(initialCandidate.IsInitialized &&
                    initialCandidate.CandidateCount == 0,
                    "Candidate module is not initialized and clean.");
                completed.Add("candidate-module-attached");

                RuntimeScopeContext rollbackTargetContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7d.rollback-target." + Guid.NewGuid().ToString("N"),
                    "P3K.7D Rollback Target Activity",
                    out _,
                    out _);
                completed.Add("rollback-target-scope-created");

                PlayerActorCandidateStageResult rollbackCandidate =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryStageCandidate",
                        rollbackTargetContext,
                        slotId,
                        nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                        "stage-rollback-candidate");
                AssertNotNull(rollbackCandidate,
                    "Rollback candidate staging returned no result.");
                AssertTrue(rollbackCandidate.Succeeded &&
                    rollbackCandidate.CurrentSnapshot.IsStagedInactive,
                    "Rollback candidate staging failed. " +
                    rollbackCandidate.ToDiagnosticString());
                PlayerActorCandidateStageToken rollbackCandidateToken =
                    rollbackCandidate.CurrentSnapshot.Token;
                completed.Add("rollback-candidate-staged");

                PlayerActorDeclaration rollbackCandidateDeclaration = ResolveDeclaration(
                    stableHost,
                    rollbackCandidateToken.ActorId);
                AssertNotNull(rollbackCandidateDeclaration,
                    "Rollback candidate declaration was not found.");
                AssertTrue(!rollbackCandidateDeclaration.gameObject.activeInHierarchy,
                    "Rollback candidate became active before handoff.");
                completed.Add("rollback-candidate-inactive");

                var rollbackEndpointSource = new QaHandoffEndpointSource(
                    stableHost,
                    gateAdapter,
                    rollbackCandidateToken.ActorId);
                object rollbackHandoffContext = CreateHandoffContext(
                    preparationModule,
                    candidateModule,
                    rollbackEndpointSource,
                    occupancyContext,
                    inputContext,
                    cameraContext,
                    admissionContext);
                Type handoffType = rollbackHandoffContext.GetType();
                AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(handoffType),
                    "Gameplay chain handoff authority must remain plain C#.");
                AssertNotNull(handoffType.GetMethod("TryPromote", InstanceAny),
                    "Gameplay chain handoff authority has no TryPromote operation.");
                AssertNotNull(handoffType.GetMethod("TryRetryRollback", InstanceAny),
                    "Gameplay chain handoff authority has no rollback retry operation.");
                completed.Add("handoff-authority-surface-valid");
                AssertNotNull(handoffType.GetMethod("TryRetryCommitCleanup", InstanceAny),
                    "Gameplay chain handoff authority has no commit-cleanup retry operation.");
                completed.Add("handoff-retry-surface-valid");

                PlayerGameplayChainHandoffResult rolledBackHandoff =
                    Promote(
                        handoffType,
                        rollbackHandoffContext,
                        rollbackCandidateToken,
                        currentChain.Admission.Token,
                        "force-candidate-endpoint-failure");
                AssertNotNull(rolledBackHandoff,
                    "Rollback-path handoff returned no result.");
                AssertEqual(PlayerGameplayChainHandoffStatus.FailedCandidateChain,
                    rolledBackHandoff.Status,
                    "Candidate endpoint failure did not return FailedCandidateChain. " +
                    rolledBackHandoff.ToDiagnosticString());
                completed.Add("candidate-chain-failure-observed");

                AssertNotNull(rolledBackHandoff.CurrentSnapshot,
                    "Rollback-path handoff has no progress snapshot.");
                AssertEqual(PlayerGameplayChainHandoffState.RolledBack,
                    rolledBackHandoff.CurrentSnapshot.State,
                    "Candidate failure did not finish with explicit rollback evidence.");
                AssertTrue(rolledBackHandoff.CurrentSnapshot.RollbackAttempted &&
                    rolledBackHandoff.CurrentSnapshot.RollbackSucceeded,
                    "Candidate failure did not report successful rollback.");
                completed.Add("rollback-evidence-explicit");

                PlayerActorPreparationRuntimeHostSnapshot afterRollbackPreparation =
                    GetPreparationSnapshot(preparationModule);
                PlayerActorPreparationSummary restoredPreparation = FindPreparation(
                    afterRollbackPreparation.Preparation,
                    slotId);
                AssertEqual(currentPreparation.Token,
                    restoredPreparation.Token,
                    "Rollback did not restore the exact previous P3J preparation token.");
                completed.Add("previous-preparation-token-restored");

                AssertTrue(currentDeclaration != null &&
                    currentDeclaration.gameObject.activeInHierarchy,
                    "Previous Actor is not active after rollback.");
                AssertSame(stablePlayerInput, currentDeclaration.PlayerInput,
                    "Previous Actor lost stable PlayerInput after rollback.");
                completed.Add("previous-actor-reactivated");

                AssertTrue(rollbackCandidateDeclaration != null &&
                    !rollbackCandidateDeclaration.gameObject.activeInHierarchy,
                    "Candidate was not returned to staged inactive state.");
                PlayerActorCandidateRuntimeHostSnapshot afterRollbackCandidate =
                    GetCandidateSnapshot(candidateModule);
                AssertEqual(1, afterRollbackCandidate.StagedInactiveCount,
                    "Candidate runtime did not return to one staged inactive candidate.");
                AssertEqual(0, afterRollbackCandidate.PromotingCount,
                    "Candidate runtime retained promotion ownership after rollback.");
                completed.Add("candidate-returned-to-staging");

                PlayerGameplayAdmissionSummary restoredAdmission = FindAdmission(
                    AdmissionSnapshot(admissionType, admissionContext),
                    slotId);
                AssertTrue(restoredAdmission.IsAdmitted &&
                    restoredAdmission.Token.IsValid &&
                    restoredAdmission.Token != currentChain.Admission.Token,
                    "Previous gameplay chain was not rebuilt with a new current admission token.");
                completed.Add("previous-gameplay-chain-restored");

                AssertEqual(0, GetIntProperty(
                        rollbackHandoffContext,
                        "ActiveHandoffCount"),
                    "Rollback-path handoff remained active after successful restoration.");
                completed.Add("rollback-handoff-released");

                PlayerActorCandidateStageResult rollbackCandidateCleanup =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryRollbackCandidate",
                        rollbackCandidateToken,
                        nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                        "cleanup-rollback-candidate");
                AssertTrue(rollbackCandidateCleanup.Succeeded,
                    "Rollback candidate cleanup failed. " +
                    rollbackCandidateCleanup.ToDiagnosticString());
                await Awaitable.NextFrameAsync();
                AssertTrue(rollbackCandidateDeclaration == null,
                    "Rollback candidate physical instance survived cleanup.");
                completed.Add("rollback-candidate-cleaned");

                AssertEqual(0,
                    SnapshotHandles(runtimeContent, runtimeContentType, rollbackTargetContext).Length,
                    "Rollback target scope retained RuntimeContent handles.");
                RemoveScopeRoot(
                    runtimeContent,
                    runtimeContentType,
                    rollbackTargetContext.Owner);
                completed.Add("rollback-target-scope-clean");

                RuntimeScopeContext commitTargetContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7d.commit-target." + Guid.NewGuid().ToString("N"),
                    "P3K.7D Commit Target Activity",
                    out _,
                    out _);
                completed.Add("commit-target-scope-created");

                PlayerActorCandidateStageResult commitCandidate =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryStageCandidate",
                        commitTargetContext,
                        slotId,
                        nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                        "stage-commit-candidate");
                AssertTrue(commitCandidate.Succeeded &&
                    commitCandidate.CurrentSnapshot.IsStagedInactive,
                    "Commit candidate staging failed. " +
                    commitCandidate.ToDiagnosticString());
                PlayerActorCandidateStageToken commitCandidateToken =
                    commitCandidate.CurrentSnapshot.Token;
                completed.Add("commit-candidate-staged");

                PlayerActorDeclaration commitCandidateDeclaration = ResolveDeclaration(
                    stableHost,
                    commitCandidateToken.ActorId);
                AssertNotNull(commitCandidateDeclaration,
                    "Commit candidate declaration was not found.");
                AssertTrue(!commitCandidateDeclaration.gameObject.activeInHierarchy,
                    "Commit candidate became active before handoff.");
                completed.Add("commit-candidate-inactive");

                var commitEndpointSource = new QaHandoffEndpointSource(
                    stableHost,
                    gateAdapter,
                    default);
                object commitHandoffContext = CreateHandoffContext(
                    preparationModule,
                    candidateModule,
                    commitEndpointSource,
                    occupancyContext,
                    inputContext,
                    cameraContext,
                    admissionContext);
                Type commitHandoffType = commitHandoffContext.GetType();
                completed.Add("commit-handoff-authority-created");

                PlayerGameplayChainHandoffResult committed = Promote(
                    commitHandoffType,
                    commitHandoffContext,
                    commitCandidateToken,
                    restoredAdmission.Token,
                    "commit-candidate-gameplay-handoff");
                AssertNotNull(committed, "Commit handoff returned no result.");
                AssertEqual(PlayerGameplayChainHandoffStatus.SucceededCommitted,
                    committed.Status,
                    "Candidate handoff did not commit. " + committed.ToDiagnosticString());
                completed.Add("candidate-handoff-committed");

                AssertNotNull(committed.CurrentSnapshot,
                    "Committed handoff has no snapshot.");
                AssertTrue(committed.CurrentSnapshot.IsCommitted &&
                    committed.CurrentSnapshot.CandidateOwnershipCompleted &&
                    committed.CurrentSnapshot.PreviousActorReleased &&
                    committed.CurrentSnapshot.CandidateChainReady,
                    "Committed handoff snapshot is incomplete. " +
                    committed.CurrentSnapshot.ToDiagnosticString());
                completed.Add("commit-evidence-complete");

                PlayerActorPreparationRuntimeHostSnapshot committedPreparationHost =
                    GetPreparationSnapshot(preparationModule);
                PlayerActorPreparationSummary committedPreparation = FindPreparation(
                    committedPreparationHost.Preparation,
                    slotId);
                AssertEqual(commitCandidateToken.ActorId,
                    committedPreparation.Materialization.ActorId,
                    "P3J current preparation did not become the committed candidate Actor.");
                AssertEqual(committed.CurrentSnapshot.CurrentPreparationToken,
                    committedPreparation.Token,
                    "Committed handoff and P3J current preparation tokens differ.");
                completed.Add("candidate-became-current-p3j-preparation");

                AssertTrue(commitCandidateDeclaration != null &&
                    commitCandidateDeclaration.gameObject.activeInHierarchy,
                    "Committed candidate is not active.");
                AssertSame(stablePlayerInput, commitCandidateDeclaration.PlayerInput,
                    "Committed candidate lost stable PlayerInput evidence.");
                AssertTrue(currentDeclaration == null ||
                    !currentDeclaration.gameObject.activeInHierarchy,
                    "Previous Actor remained active after candidate commit.");
                completed.Add("candidate-active-previous-not-active");

                PlayerActorCandidateRuntimeHostSnapshot committedCandidateHost =
                    GetCandidateSnapshot(candidateModule);
                AssertEqual(0, committedCandidateHost.CandidateCount,
                    "Candidate module retained the promoted candidate record.");
                AssertTrue(InvokeBool(
                        candidateModule,
                        "WasCandidatePromoted",
                        commitCandidateToken),
                    "Candidate module did not retain exact promoted-token diagnostics.");
                completed.Add("candidate-ownership-completed");

                PlayerGameplayAdmissionSummary committedAdmission = FindAdmission(
                    AdmissionSnapshot(admissionType, admissionContext),
                    slotId);
                AssertTrue(committedAdmission.IsAdmitted &&
                    committedAdmission.Token ==
                        committed.CurrentSnapshot.CurrentAdmissionToken &&
                    committedAdmission.ActorId == commitCandidateToken.ActorId,
                    "P3K.5 current admission is not the committed candidate chain.");
                completed.Add("candidate-gameplay-chain-authoritative");

                await Awaitable.NextFrameAsync();
                AssertTrue(currentDeclaration == null,
                    "Previous Actor physical instance survived the commit frame boundary.");
                completed.Add("previous-actor-physically-released");

                AssertEqual(0,
                    SnapshotHandles(runtimeContent, runtimeContentType, currentContext).Length,
                    "Previous Activity scope retained RuntimeContent handles after commit.");
                AssertEqual(1,
                    SnapshotHandles(runtimeContent, runtimeContentType, commitTargetContext).Length,
                    "Committed target scope does not own exactly one current Actor handle.");
                completed.Add("runtime-content-ownership-cutover-complete");

                PlayerGameplayChainHandoffResult repeated = Promote(
                    commitHandoffType,
                    commitHandoffContext,
                    commitCandidateToken,
                    restoredAdmission.Token,
                    "repeat-committed-candidate-handoff");
                AssertEqual(PlayerGameplayChainHandoffStatus.SucceededAlreadyCommitted,
                    repeated.Status,
                    "Repeated exact candidate handoff was not idempotent.");
                AssertEqual(committed.CurrentSnapshot.Token,
                    repeated.CurrentSnapshot.Token,
                    "Repeated handoff replaced committed identity evidence.");
                completed.Add("committed-handoff-idempotent");

                PlayerGameplayChainHandoffResult committedRollback =
                    RetryRollback(
                        commitHandoffType,
                        commitHandoffContext,
                        committed.CurrentSnapshot.Token,
                        "reject-rollback-after-commit");
                AssertEqual(PlayerGameplayChainHandoffStatus.RejectedRollbackNotAvailable,
                    committedRollback.Status,
                    "Committed handoff allowed rollback.");
                completed.Add("committed-handoff-rollback-rejected");

                AssertPublicContractsContainNoUnityReferences(
                    typeof(PlayerGameplayChainHandoffToken),
                    typeof(PlayerGameplayChainHandoffSnapshot),
                    typeof(PlayerGameplayChainHandoffResult));
                completed.Add("public-handoff-contracts-no-unity-references");

                PlayerGameplayAdmissionResult finalAdmissionRelease =
                    ReleaseAdmission(
                        admissionType,
                        admissionContext,
                        slotId,
                        committedAdmission.Token,
                        "release-committed-gameplay-chain");
                AssertTrue(finalAdmissionRelease.Succeeded,
                    "Committed gameplay chain release failed. " +
                    finalAdmissionRelease.ToDiagnosticString());
                completed.Add("committed-gameplay-chain-released");

                PlayerActorPreparationResult finalActorRelease =
                    Invoke<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryReleasePreparedActor",
                        slotId,
                        committedPreparation.Token,
                        nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                        "release-committed-player-actor");
                AssertTrue(finalActorRelease.Succeeded,
                    "Committed Player Actor release failed. " +
                    finalActorRelease.ToDiagnosticString());
                await Awaitable.NextFrameAsync();
                AssertTrue(commitCandidateDeclaration == null,
                    "Committed Player Actor survived final release.");
                completed.Add("committed-player-actor-released");

                AssertEqual(0,
                    SnapshotHandles(runtimeContent, runtimeContentType, commitTargetContext).Length,
                    "Committed target scope retained RuntimeContent handles after final release.");
                RemoveScopeRoot(runtimeContent, runtimeContentType, currentContext.Owner);
                RemoveScopeRoot(runtimeContent, runtimeContentType, commitTargetContext.Owner);
                completed.Add("activity-runtime-scopes-cleaned");

                AssertSame(stablePlayerInput, stableHost.PlayerInput,
                    "Handoff replaced the stable PlayerInput.");
                AssertTrue(stableHost.IsJoined &&
                    stableHost.JoinedPlayerSlotId == slotId,
                    "Handoff removed Joined Slot evidence from the stable host.");
                completed.Add("stable-session-host-survives-handoff");

                PlayerParticipationOperationResult closed =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryCloseJoining",
                        nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                        "gameplay-chain-handoff-complete");
                AssertTrue(closed.Completed && !closed.Snapshot.JoiningOpen,
                    "Closing joining failed. " + closed.ToDiagnosticString());
                completed.Add("joining-closed");

                AssertEqual(52, completed.Count,
                    "P3K.7D smoke case count changed unexpectedly.");
                Debug.Log(
                    "[P3K7D_PLAYER_GAMEPLAY_CHAIN_PROMOTION_HANDOFF_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"session='{authoring.RuntimeSnapshot.ContextId}' " +
                    $"slot='{slotId.StableText}' " +
                    $"previousActor='{currentPreparation.Materialization.ActorId.StableText}' " +
                    $"committedActor='{commitCandidateToken.ActorId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                Debug.LogError(
                    "[P3K7D_PLAYER_GAMEPLAY_CHAIN_PROMOTION_HANDOFF_SMOKE] " +
                    $"status='Failed' exception='{inner.GetType().Name}' " +
                    $"message='{Escape(inner.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw inner;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K7D_PLAYER_GAMEPLAY_CHAIN_PROMOTION_HANDOFF_SMOKE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        private sealed class GameplayChainEvidence
        {
            internal PlayerGameplayOccupancySummary Occupancy;
            internal PlayerGameplayInputBindingSummary Input;
            internal PlayerGameplayCameraEligibilitySummary Camera;
            internal PlayerGameplayAdmissionSummary Admission;
        }

        private sealed class QaHandoffEndpointSource :
            IPlayerGameplayChainHandoffEndpointSource
        {
            private readonly LocalPlayerHostAuthoring host;
            private readonly UnityPlayerInputGateAdapter gateAdapter;
            private readonly ActorId rejectedActorId;

            internal QaHandoffEndpointSource(
                LocalPlayerHostAuthoring host,
                UnityPlayerInputGateAdapter gateAdapter,
                ActorId rejectedActorId)
            {
                this.host = host;
                this.gateAdapter = gateAdapter;
                this.rejectedActorId = rejectedActorId;
            }

            public bool TryResolveGameplayEndpoints(
                PlayerActorPreparationSummary preparation,
                out LocalPlayerHostAuthoring resolvedHost,
                out PlayerActorDeclaration actorDeclaration,
                out UnityPlayerInputGateAdapter resolvedGateAdapter,
                out PlayerGameplayCameraAuthoring cameraAuthoring,
                out PlayerGameplayCameraRequiredness cameraRequiredness,
                out CameraOutputSessionBinding outputSession,
                out string issue)
            {
                resolvedHost = host;
                actorDeclaration = null;
                resolvedGateAdapter = gateAdapter;
                cameraAuthoring = null;
                cameraRequiredness = PlayerGameplayCameraRequiredness.Optional;
                outputSession = null;
                issue = string.Empty;

                if (!preparation.IsPrepared || !preparation.Token.IsValid)
                {
                    issue = "QA endpoint source requires exact prepared evidence.";
                    return false;
                }

                if (rejectedActorId.IsValid &&
                    preparation.Materialization.ActorId == rejectedActorId)
                {
                    issue =
                        "QA forced candidate endpoint failure after P3J cutover to prove rollback.";
                    return false;
                }

                actorDeclaration = ResolveDeclaration(
                    host,
                    preparation.Materialization.ActorId);
                if (host == null || actorDeclaration == null || gateAdapter == null ||
                    !ReferenceEquals(gateAdapter.PlayerInput, host.PlayerInput))
                {
                    issue = "QA endpoint source could not resolve exact host/Actor/Gate evidence.";
                    return false;
                }

                // This smoke deliberately proves the optional-camera branch so camera
                // publication does not depend on the unrelated QA Hub output session.
                cameraAuthoring = null;
                cameraRequiredness = PlayerGameplayCameraRequiredness.Optional;
                return true;
            }
        }

        private static object ResolveCurrentRuntimeHost()
        {
            Type runtimeHostType = ResolveRuntimeType(RuntimeHostTypeName);
            MethodInfo tryGetCurrent = runtimeHostType.GetMethod("TryGetCurrent", StaticAny);
            AssertNotNull(tryGetCurrent,
                "FrameworkRuntimeHost.TryGetCurrent was not found.");
            object[] arguments = { null };
            bool resolved = (bool)tryGetCurrent.Invoke(null, arguments);
            AssertTrue(resolved && arguments[0] != null,
                "Current FrameworkRuntimeHost was not resolved.");
            return arguments[0];
        }

        private static object ResolvePreparationModule(object runtimeHost)
        {
            Type moduleType = ResolveRuntimeType(PreparationModuleTypeName);
            Component hostComponent = runtimeHost as Component;
            AssertNotNull(hostComponent,
                "FrameworkRuntimeHost is not a Unity Component.");
            Component module = hostComponent.GetComponent(moduleType);
            AssertNotNull(module,
                "FrameworkRuntimeHost has no PlayerActorPreparationRuntimeHostModule.");
            return module;
        }

        private static object AttachCandidateModule(object runtimeHost)
        {
            Type moduleType = ResolveRuntimeType(CandidateModuleTypeName);
            MethodInfo tryAttach = moduleType.GetMethod("TryAttach", StaticAny);
            AssertNotNull(tryAttach,
                "PlayerActorCandidateRuntimeHostModule.TryAttach was not found.");
            object[] arguments = { runtimeHost, null, null };
            bool attached = (bool)tryAttach.Invoke(null, arguments);
            AssertTrue(attached && arguments[1] != null,
                "Candidate module attachment failed. " + (arguments[2] as string));
            return arguments[1];
        }

        private static PlayerActorPreparationRuntimeHostSnapshot GetPreparationSnapshot(
            object module)
        {
            object[] arguments = { null };
            bool available = (bool)GetMethod(module.GetType(), "TryGetSnapshot").Invoke(
                module,
                arguments);
            var snapshot = arguments[0] as PlayerActorPreparationRuntimeHostSnapshot;
            AssertNotNull(snapshot,
                "P3J preparation runtime-host snapshot is missing.");
            AssertTrue(available || !snapshot.IsInitialized,
                "P3J snapshot availability and initialization disagree.");
            return snapshot;
        }

        private static PlayerActorCandidateRuntimeHostSnapshot GetCandidateSnapshot(
            object module)
        {
            object[] arguments = { null };
            bool available = (bool)GetMethod(module.GetType(), "TryGetSnapshot").Invoke(
                module,
                arguments);
            var snapshot = arguments[0] as PlayerActorCandidateRuntimeHostSnapshot;
            AssertNotNull(snapshot,
                "Candidate runtime-host snapshot is missing.");
            AssertTrue(available || !snapshot.IsInitialized,
                "Candidate snapshot availability and initialization disagree.");
            return snapshot;
        }

        private static PlayerActorPreparationSummary FindPreparation(
            PlayerActorPreparationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            AssertNotNull(snapshot, "P3J preparation snapshot is missing.");
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId == playerSlotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"P3J preparation snapshot has no Slot '{playerSlotId.StableText}'.");
        }

        private static PlayerGameplayAdmissionSummary FindAdmission(
            PlayerGameplayAdmissionSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            AssertNotNull(snapshot, "P3K.5 admission snapshot is missing.");
            AssertTrue(snapshot.TryGetSummary(playerSlotId, out var summary),
                $"P3K.5 admission snapshot has no Slot '{playerSlotId.StableText}'.");
            return summary;
        }

        private static PlayerActorDeclaration ResolveDeclaration(
            LocalPlayerHostAuthoring host,
            ActorId actorId)
        {
            if (host == null || host.ActorMount == null || !actorId.IsValid)
            {
                return null;
            }

            PlayerActorDeclaration[] declarations =
                host.ActorMount.GetComponentsInChildren<PlayerActorDeclaration>(true);
            for (int index = 0; index < declarations.Length; index++)
            {
                if (declarations[index] != null && declarations[index].ActorId == actorId)
                {
                    return declarations[index];
                }
            }

            return null;
        }

        private static UnityPlayerInputGateAdapter ConfigureGateAdapter(
            LocalPlayerHostAuthoring host,
            PlayerInput playerInput)
        {
            AssertNotNull(host, "Stable Local Player Host is missing.");
            AssertNotNull(playerInput, "Stable PlayerInput is missing.");
            AssertNotNull(playerInput.actions,
                "Stable PlayerInput has no InputActionAsset.");

            string actionMapName = ResolveGameplayActionMapName(playerInput);
            AssertTrue(!string.IsNullOrEmpty(actionMapName),
                "Stable PlayerInput has no usable action map.");

            UnityPlayerInputGateAdapter adapter =
                host.GetComponent<UnityPlayerInputGateAdapter>();
            if (adapter == null)
            {
                adapter = host.gameObject.AddComponent<UnityPlayerInputGateAdapter>();
            }

            SerializedObject serialized = new SerializedObject(adapter);
            SerializedProperty playerInputProperty = serialized.FindProperty("playerInput");
            SerializedProperty actionMapProperty = serialized.FindProperty("gameplayActionMapName");
            AssertNotNull(playerInputProperty,
                "Gate adapter playerInput property was not found.");
            AssertNotNull(actionMapProperty,
                "Gate adapter gameplayActionMapName property was not found.");
            playerInputProperty.objectReferenceValue = playerInput;
            actionMapProperty.stringValue = actionMapName;
            SerializedProperty logState = serialized.FindProperty("logStateChanges");
            SerializedProperty logRuntime = serialized.FindProperty("logMissingRuntimeOnce");
            SerializedProperty logTarget = serialized.FindProperty("logMissingTargetOnce");
            if (logState != null) logState.boolValue = false;
            if (logRuntime != null) logRuntime.boolValue = false;
            if (logTarget != null) logTarget.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return adapter;
        }

        private static string ResolveGameplayActionMapName(PlayerInput playerInput)
        {
            if (playerInput.currentActionMap != null)
            {
                return playerInput.currentActionMap.name;
            }

            if (!string.IsNullOrWhiteSpace(playerInput.defaultActionMap) &&
                playerInput.actions.FindActionMap(
                    playerInput.defaultActionMap,
                    false) != null)
            {
                return playerInput.defaultActionMap;
            }

            return playerInput.actions.actionMaps.Count > 0
                ? playerInput.actions.actionMaps[0].name
                : string.Empty;
        }

        private static GameplayChainEvidence BuildOptionalGameplayChain(
            Type occupancyType,
            object occupancyContext,
            Type inputType,
            object inputContext,
            Type cameraType,
            object cameraContext,
            Type admissionType,
            object admissionContext,
            PlayerActorPreparationSummary preparation,
            LocalPlayerHostAuthoring host,
            PlayerActorDeclaration declaration,
            UnityPlayerInputGateAdapter gateAdapter,
            string reason)
        {
            PlayerGameplayOccupancyResult occupancy =
                (PlayerGameplayOccupancyResult)GetMethod(
                    occupancyType,
                    "TryConfirmOccupancy").Invoke(
                        occupancyContext,
                        new object[]
                        {
                            preparation,
                            nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                            reason
                        });
            AssertTrue(occupancy.Succeeded,
                "P3K.2 occupancy failed. " + occupancy.ToDiagnosticString());

            PlayerGameplayInputBindingResult input =
                (PlayerGameplayInputBindingResult)GetMethod(
                    inputType,
                    "TryBind").Invoke(
                        inputContext,
                        new object[]
                        {
                            preparation,
                            occupancy.CurrentSummary,
                            host,
                            declaration,
                            gateAdapter,
                            nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                            reason
                        });
            AssertTrue(input.Succeeded,
                "P3K.3 input binding failed. " + input.ToDiagnosticString());

            PlayerGameplayCameraEligibilityResult camera =
                (PlayerGameplayCameraEligibilityResult)GetMethod(
                    cameraType,
                    "TrySkipOptional").Invoke(
                        cameraContext,
                        new object[]
                        {
                            preparation,
                            occupancy.CurrentSummary,
                            input.CurrentSummary,
                            PlayerGameplayCameraRequiredness.Optional,
                            nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                            reason
                        });
            AssertTrue(camera.Succeeded,
                "P3K.4 optional camera skip failed. " + camera.ToDiagnosticString());

            PlayerGameplayAdmissionResult admission =
                (PlayerGameplayAdmissionResult)GetMethod(
                    admissionType,
                    "TryAdmit").Invoke(
                        admissionContext,
                        new object[]
                        {
                            occupancy.CurrentSummary,
                            input.CurrentSummary,
                            camera.CurrentSummary,
                            null,
                            nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                            reason
                        });
            AssertTrue(admission.Succeeded,
                "P3K.5 admission failed. " + admission.ToDiagnosticString());

            return new GameplayChainEvidence
            {
                Occupancy = occupancy.CurrentSummary,
                Input = input.CurrentSummary,
                Camera = camera.CurrentSummary,
                Admission = admission.CurrentSummary
            };
        }

        private static object CreateOccupancyContext(
            Type contextType,
            PlayerActorPreparationSnapshot snapshot)
        {
            object[] arguments = { snapshot, null, null };
            bool succeeded = (bool)GetMethod(contextType, "TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.2 occupancy context creation failed. {arguments[2]}");
            return arguments[1];
        }

        private static object CreateInputContext(Type contextType, object occupancyContext)
        {
            object[] arguments = { occupancyContext, null, null };
            bool succeeded = (bool)GetMethod(contextType, "TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.3 input context creation failed. {arguments[2]}");
            return arguments[1];
        }

        private static object CreateCameraContext(
            Type contextType,
            object occupancyContext,
            object inputContext)
        {
            object[] arguments = { occupancyContext, inputContext, null, null };
            bool succeeded = (bool)GetMethod(contextType, "TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.4 camera context creation failed. {arguments[3]}");
            return arguments[2];
        }

        private static object CreateAdmissionContext(
            Type contextType,
            object occupancyContext,
            object inputContext,
            object cameraContext)
        {
            object[] arguments =
            {
                occupancyContext,
                inputContext,
                cameraContext,
                null,
                null
            };
            bool succeeded = (bool)GetMethod(contextType, "TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.5 admission context creation failed. {arguments[4]}");
            return arguments[3];
        }

        private static object CreateHandoffContext(
            object preparationModule,
            object candidateModule,
            IPlayerGameplayChainHandoffEndpointSource endpointSource,
            object occupancyContext,
            object inputContext,
            object cameraContext,
            object admissionContext)
        {
            Type contextType = ResolveRuntimeType(HandoffContextTypeName);
            object[] arguments =
            {
                preparationModule,
                candidateModule,
                endpointSource,
                occupancyContext,
                inputContext,
                cameraContext,
                admissionContext,
                null,
                null
            };
            bool succeeded = (bool)GetMethod(contextType, "TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.7D handoff context creation failed. {arguments[8]}");
            AssertNotNull(arguments[7],
                "P3K.7D handoff context creation returned no context.");
            return arguments[7];
        }

        private static PlayerGameplayChainHandoffResult Promote(
            Type contextType,
            object context,
            PlayerActorCandidateStageToken candidate,
            PlayerGameplayAdmissionToken currentAdmission,
            string reason)
        {
            return (PlayerGameplayChainHandoffResult)GetMethod(
                    contextType,
                    "TryPromote")
                .Invoke(context, new object[]
                {
                    candidate,
                    currentAdmission,
                    nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                    reason
                });
        }

        private static PlayerGameplayChainHandoffResult RetryRollback(
            Type contextType,
            object context,
            PlayerGameplayChainHandoffToken handoff,
            string reason)
        {
            return (PlayerGameplayChainHandoffResult)GetMethod(
                    contextType,
                    "TryRetryRollback")
                .Invoke(context, new object[]
                {
                    handoff,
                    nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                    reason
                });
        }

        private static PlayerGameplayAdmissionSnapshot AdmissionSnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayAdmissionSnapshot)GetMethod(
                    contextType,
                    "CreateSnapshot")
                .Invoke(context, Array.Empty<object>());
        }

        private static PlayerGameplayAdmissionResult ReleaseAdmission(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken token,
            string reason)
        {
            return (PlayerGameplayAdmissionResult)GetMethod(
                    contextType,
                    "TryRelease")
                .Invoke(context, new object[]
                {
                    playerSlotId,
                    token,
                    nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                    reason
                });
        }

        private static RuntimeScopeContext CreateActivityScopeContext(
            object runtimeHost,
            string ownerId,
            string displayName,
            out object runtimeContent,
            out Type runtimeContentType)
        {
            PropertyInfo runtimeContentProperty = runtimeHost.GetType().GetProperty(
                "RuntimeContentRuntime",
                InstanceAny);
            AssertNotNull(runtimeContentProperty,
                "FrameworkRuntimeHost.RuntimeContentRuntime was not found.");
            runtimeContent = runtimeContentProperty.GetValue(runtimeHost);
            AssertNotNull(runtimeContent,
                "FrameworkRuntimeHost has no RuntimeContentRuntime.");
            runtimeContentType = runtimeContent.GetType();

            RuntimeContentOwner owner = RuntimeContentOwner.Activity(ownerId, displayName);
            GetMethod(runtimeContentType, "CreateScopeRoot").Invoke(
                runtimeContent,
                new object[]
                {
                    owner,
                    nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                    "create-gameplay-handoff-scope"
                });

            object[] contextArguments =
            {
                owner,
                nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                "gameplay-chain-handoff",
                null
            };
            bool created = (bool)GetMethod(
                runtimeContentType,
                "TryCreateScopeContext").Invoke(
                    runtimeContent,
                    contextArguments);
            AssertTrue(created,
                $"RuntimeContentRuntime could not create Activity scope '{owner.StableText}'.");
            return (RuntimeScopeContext)contextArguments[3];
        }

        private static RuntimeContentHandle[] SnapshotHandles(
            object runtimeContent,
            Type runtimeContentType,
            RuntimeScopeContext context)
        {
            return GetMethod(runtimeContentType, "SnapshotHandles").Invoke(
                    runtimeContent,
                    new object[] { context }) as RuntimeContentHandle[] ??
                Array.Empty<RuntimeContentHandle>();
        }

        private static void RemoveScopeRoot(
            object runtimeContent,
            Type runtimeContentType,
            RuntimeContentOwner owner)
        {
            object result = GetMethod(runtimeContentType, "RemoveScopeRoot").Invoke(
                runtimeContent,
                new object[]
                {
                    owner,
                    nameof(QaP3K7DPlayerGameplayChainPromotionHandoffSmoke),
                    "gameplay-chain-handoff-cleanup"
                });
            AssertNotNull(result,
                $"RuntimeContent scope removal returned no result for '{owner.StableText}'.");
        }

        private static LocalPlayerProvisioningAuthoring ResolveAuthoring()
        {
            LocalPlayerProvisioningAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<LocalPlayerProvisioningAuthoring>(
                    FindObjectsInactive.Include);
            LocalPlayerProvisioningAuthoring resolved = null;
            int loadedCount = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                LocalPlayerProvisioningAuthoring candidate = candidates[index];
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded)
                {
                    continue;
                }

                loadedCount++;
                resolved = candidate;
            }

            AssertEqual(1, loadedCount,
                "Expected exactly one loaded LocalPlayerProvisioningAuthoring.");
            return resolved;
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = typeof(PlayerGameplayChainHandoffResult).Assembly.GetType(
                fullName,
                false);
            AssertNotNull(type,
                $"Runtime type '{fullName}' was not found.");
            return type;
        }

        private static MethodInfo GetMethod(
            Type type,
            string methodName,
            BindingFlags flags = default)
        {
            BindingFlags resolvedFlags = flags == default ? InstanceAny : flags;
            MethodInfo method = type.GetMethod(methodName, resolvedFlags);
            AssertNotNull(method,
                $"Method '{type.FullName}.{methodName}' was not found.");
            return method;
        }

        private static T Invoke<T>(object target, string methodName, params object[] arguments)
            where T : class
        {
            return GetMethod(target.GetType(), methodName).Invoke(target, arguments) as T;
        }

        private static bool InvokeBool(object target, string methodName, params object[] arguments)
        {
            return (bool)GetMethod(target.GetType(), methodName).Invoke(target, arguments);
        }

        private static int GetIntProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName, InstanceAny);
            AssertNotNull(property,
                $"Property '{target.GetType().FullName}.{propertyName}' was not found.");
            return (int)property.GetValue(target);
        }

        private static void AssertPublicContractsContainNoUnityReferences(
            params Type[] contractTypes)
        {
            for (int typeIndex = 0; typeIndex < contractTypes.Length; typeIndex++)
            {
                Type type = contractTypes[typeIndex];
                PropertyInfo[] properties = type.GetProperties(
                    BindingFlags.Instance | BindingFlags.Public);
                for (int index = 0; index < properties.Length; index++)
                {
                    AssertTrue(!typeof(UnityEngine.Object).IsAssignableFrom(
                            properties[index].PropertyType),
                        $"Public contract '{type.FullName}' property " +
                        $"'{properties[index].Name}' retains a Unity object reference.");
                }

                FieldInfo[] fields = type.GetFields(
                    BindingFlags.Instance | BindingFlags.Public);
                for (int index = 0; index < fields.Length; index++)
                {
                    AssertTrue(!typeof(UnityEngine.Object).IsAssignableFrom(
                            fields[index].FieldType),
                        $"Public contract '{type.FullName}' field " +
                        $"'{fields[index].Name}' retains a Unity object reference.");
                }
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("'", "\\'")
                    .Replace("\r", " ").Replace("\n", " ");
        }
    }
}

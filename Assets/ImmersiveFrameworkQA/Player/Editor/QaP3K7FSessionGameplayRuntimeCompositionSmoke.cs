using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
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
    /// One-shot Play Mode proof for the official FrameworkRuntimeHost-scoped
    /// P3K.2-P3K.7E composition.
    /// </summary>
    public static class QaP3K7FSessionGameplayRuntimeCompositionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.7F Run Session Gameplay Runtime Composition Smoke";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";
        private const string GameplayModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayRuntimeHostModule";
        private const string EndpointSourceTypeName =
            "Immersive.Framework.PlayerParticipation.HostScopedPlayerGameplayChainEndpointSource";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            object runtimeContent = null;
            Type runtimeContentType = null;
            RuntimeScopeContext currentContext = default;
            RuntimeScopeContext rollbackTargetContext = default;
            RuntimeScopeContext commitTargetContext = default;

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "P3K.7F composition smoke must run in Play Mode.");
                completed.Add("play-mode-required");

                LocalPlayerProvisioningAuthoring authoring = ResolveAuthoring();
                AssertTrue(authoring.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " +
                    authoring.RuntimeDiagnostic);
                completed.Add("provisioning-runtime-ready");

                object runtimeHost = ResolveCurrentRuntimeHost();
                completed.Add("runtime-host-resolved");

                object preparationModule = ResolveHostComponent(
                    runtimeHost,
                    PreparationModuleTypeName,
                    "PlayerActorPreparationRuntimeHostModule");
                PlayerActorPreparationRuntimeHostSnapshot initialPreparation =
                    GetPreparationSnapshot(preparationModule);
                AssertTrue(initialPreparation.IsInitialized,
                    "P3J preparation module is not initialized. " +
                    initialPreparation.Diagnostic);
                completed.Add("p3j-module-ready");

                object gameplayModule = ResolveHostComponent(
                    runtimeHost,
                    GameplayModuleTypeName,
                    "PlayerGameplayRuntimeHostModule");
                completed.Add("gameplay-module-auto-attached");

                PlayerGameplayRuntimeHostSnapshot initialGameplay =
                    GetGameplaySnapshot(gameplayModule);
                AssertTrue(initialGameplay.IsInitialized,
                    "Official Player gameplay runtime is not initialized. " +
                    initialGameplay.Diagnostic);
                completed.Add("gameplay-host-snapshot-initialized");

                AssertEqual(authoring.RuntimeSnapshot.ContextId,
                    initialGameplay.SessionContextId,
                    "Participation and gameplay Session identities differ.");
                AssertEqual(initialPreparation.SessionContextId,
                    initialGameplay.SessionContextId,
                    "P3J and gameplay Session identities differ.");
                completed.Add("session-identities-match");

                AssertEqual(authoring.RuntimeSnapshot.ConfiguredSlotCount,
                    initialGameplay.ConfiguredSlotCount,
                    "Gameplay composition Slot roster differs from participation.");
                AssertTrue(initialGameplay.ConfiguredSlotCount > 0,
                    "Gameplay composition has no configured Slots.");
                completed.Add("configured-slot-roster-composed");

                AssertEqual(0, initialGameplay.OccupiedCount,
                    "Gameplay composition already contains occupancy.");
                AssertEqual(0, initialGameplay.BoundInputCount,
                    "Gameplay composition already contains input binding.");
                AssertEqual(0, initialGameplay.CameraDecisionCount,
                    "Gameplay composition already contains camera decisions.");
                AssertEqual(0, initialGameplay.GameplayReadyCount,
                    "Gameplay composition already contains admissions.");
                AssertEqual(0, initialGameplay.CandidateCount,
                    "Gameplay composition already contains candidates.");
                completed.Add("initial-gameplay-authorities-clean");

                ValidateEndpointSourceShape(runtimeHost);
                completed.Add("multi-slot-endpoint-source-shape-valid");

                PlayerInputManager manager = authoring.PlayerInputManager;
                AssertNotNull(manager,
                    "Provisioning authoring has no PlayerInputManager.");
                AssertEqual(0, manager.playerCount,
                    "P3K.7F smoke is one-shot. Re-enter Play Mode before running again.");
                AssertEqual(0, authoring.RuntimeSnapshot.JoinedCount,
                    "Session already contains a Joined Player.");

                PlayerParticipationOperationResult opened =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryOpenJoining",
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "session-gameplay-runtime-composition");
                AssertTrue(opened.Completed && opened.Snapshot.JoiningOpen,
                    "Opening joining failed. " + opened.ToDiagnosticString());
                completed.Add("joining-opened");

                LocalPlayerJoinResult joined = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "session-gameplay-runtime-composition"));
                AssertNotNull(joined,
                    "Real local Player join returned no result.");
                AssertTrue(joined.Succeeded,
                    "Real local Player join failed. " +
                    joined.ToDiagnosticString());
                completed.Add("real-local-player-joined");

                LocalPlayerHostAuthoring stableHost = joined.LocalPlayerHost;
                PlayerInput stablePlayerInput = joined.PlayerInput;
                PlayerSlotId slotId = joined.Slot.PlayerSlotId;
                AssertNotNull(stableHost,
                    "Join result has no stable Local Player Host.");
                AssertNotNull(stablePlayerInput,
                    "Join result has no stable PlayerInput.");
                AssertSame(stablePlayerInput, stableHost.PlayerInput,
                    "Stable host does not own the joined PlayerInput.");
                AssertTrue(stableHost.IsJoined &&
                    stableHost.JoinedPlayerSlotId == slotId,
                    "Stable host lost Joined Slot evidence.");
                completed.Add("stable-host-playerinput-confirmed");

                PlayerActorSelectionResult selected =
                    Invoke<PlayerActorSelectionResult>(
                        preparationModule,
                        "TrySelectDefaultActor",
                        slotId,
                        joined.Slot.SelectionRevision,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "select-default-current-actor");
                AssertNotNull(selected,
                    "Default Actor selection returned no result.");
                AssertTrue(selected.Succeeded &&
                    selected.SelectedActorProfile != null,
                    "Default Actor selection failed. " +
                    selected.ToDiagnosticString());
                completed.Add("default-actor-selected");

                currentContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7f.current." + Guid.NewGuid().ToString("N"),
                    "P3K.7F Current Activity",
                    out runtimeContent,
                    out runtimeContentType);
                AssertTrue(currentContext.IsValid,
                    "Current Activity scope context is invalid.");
                completed.Add("current-activity-scope-created");

                PlayerActorPreparationResult prepared =
                    Invoke<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryPrepareSelectedActor",
                        currentContext,
                        slotId,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "prepare-current-activity-actor");
                AssertNotNull(prepared,
                    "Current Actor preparation returned no result.");
                AssertTrue(prepared.Succeeded &&
                    prepared.CurrentSummary.IsPrepared,
                    "Current Actor preparation failed. " +
                    prepared.ToDiagnosticString());
                PlayerActorPreparationSummary currentPreparation =
                    prepared.CurrentSummary;
                completed.Add("current-actor-prepared");

                PlayerActorDeclaration currentDeclaration =
                    ResolveDeclaration(
                        stableHost,
                        currentPreparation.Materialization.ActorId);
                AssertNotNull(currentDeclaration,
                    "Current prepared Actor declaration was not found.");
                AssertTrue(currentDeclaration.gameObject.activeInHierarchy,
                    "Current prepared Actor is not active.");
                completed.Add("current-actor-active");

                UnityPlayerInputGateAdapter gateAdapter =
                    ConfigureGateAdapter(stableHost, stablePlayerInput);
                AssertNotNull(gateAdapter,
                    "Stable host Gate adapter could not be configured.");
                AssertSame(stablePlayerInput, gateAdapter.PlayerInput,
                    "Gate adapter does not target the stable-host PlayerInput.");
                completed.Add("stable-host-gate-adapter-configured");

                PlayerGameplayRuntimeOperationResult ensured =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "ensure-current-gameplay");
                AssertNotNull(ensured,
                    "Current gameplay ensure returned no result.");
                AssertEqual(
                    PlayerGameplayRuntimeOperationStatus.SucceededReady,
                    ensured.Status,
                    "Current gameplay chain was not created. " +
                    ensured.ToDiagnosticString());
                completed.Add("current-gameplay-chain-created");

                PlayerGameplayRuntimeHostSnapshot readySnapshot =
                    ensured.Snapshot;
                AssertEqual(1, readySnapshot.OccupiedCount,
                    "P3K.2 occupancy is not authoritative.");
                completed.Add("occupancy-authoritative");

                AssertEqual(1, readySnapshot.BoundInputCount,
                    "P3K.3 input binding is not authoritative.");
                completed.Add("input-binding-authoritative");

                AssertEqual(1, readySnapshot.CameraDecisionCount,
                    "P3K.4 camera decision is not authoritative.");
                AssertTrue(
                    readySnapshot.CameraEligibility.SkippedOptionalCount == 1,
                    "Expected optional camera skip for the QA Actor.");
                completed.Add("camera-optional-decision-authoritative");

                AssertEqual(1, readySnapshot.GameplayReadyCount,
                    "P3K.5 gameplay admission is not authoritative.");
                AssertTrue(ensured.CurrentAdmission.GameplayReady &&
                    ensured.CurrentAdmission.Token.IsValid,
                    "Current gameplay admission token is invalid.");
                completed.Add("admission-authoritative");

                PlayerGameplayRuntimeOperationResult ensuredAgain =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "ensure-current-gameplay-again");
                AssertEqual(
                    PlayerGameplayRuntimeOperationStatus
                        .SucceededAlreadyReady,
                    ensuredAgain.Status,
                    "Current gameplay ensure is not idempotent. " +
                    ensuredAgain.ToDiagnosticString());
                AssertEqual(ensured.CurrentAdmission.Token,
                    ensuredAgain.CurrentAdmission.Token,
                    "Idempotent ensure changed the admission token.");
                completed.Add("ensure-idempotent");

                AssertEqual(
                    PlayerGameplayRuntimeOperationStatus
                        .SucceededAlreadyReady,
                    ensuredAgain.Snapshot.LastOperationStatus,
                    "Host diagnostics did not retain the last operation.");
                completed.Add("gameplay-host-diagnostics-updated");

                ActivityAsset targetActivity = CreateGameplayReadyActivity(
                    joined.Slot.Profile,
                    created);
                completed.Add("target-activity-authoring-created");

                rollbackTargetContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7f.rollback-target." +
                        Guid.NewGuid().ToString("N"),
                    "P3K.7F Rollback Target",
                    out _,
                    out _);
                completed.Add("target-activity-scope-created");

                PlayerActorCandidateStageResult rollbackCandidate =
                    Invoke<PlayerActorCandidateStageResult>(
                        gameplayModule,
                        "TryStageCandidate",
                        rollbackTargetContext,
                        slotId,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "stage-rollback-candidate");
                AssertNotNull(rollbackCandidate,
                    "Candidate staging returned no result.");
                AssertTrue(rollbackCandidate.Succeeded &&
                    rollbackCandidate.CurrentSnapshot.IsStagedInactive,
                    "Candidate staging failed. " +
                    rollbackCandidate.ToDiagnosticString());
                PlayerActorCandidateStageToken rollbackCandidateToken =
                    rollbackCandidate.CurrentSnapshot.Token;
                completed.Add("candidate-staged-through-host-module");

                PlayerActorDeclaration rollbackCandidateDeclaration =
                    ResolveDeclaration(
                        stableHost,
                        rollbackCandidateToken.ActorId);
                AssertNotNull(rollbackCandidateDeclaration,
                    "Rollback candidate declaration was not found.");
                AssertTrue(
                    !rollbackCandidateDeclaration.gameObject.activeInHierarchy,
                    "Candidate became active before group Begin.");
                completed.Add("candidate-inactive");

                var rollbackRequests =
                    new[]
                    {
                        new ActivityPlayerHandoffSlotRequest(
                            rollbackCandidateToken,
                            ensuredAgain.CurrentAdmission.Token)
                    };
                ActivityPlayerHandoffGroupResult rollbackGroup =
                    Invoke<ActivityPlayerHandoffGroupResult>(
                        gameplayModule,
                        "TryBeginActivityHandoffGroup",
                        targetActivity,
                        rollbackTargetContext.Owner,
                        rollbackRequests,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "begin-rollback-group");
                AssertNotNull(rollbackGroup,
                    "Handoff group Begin returned no result.");
                AssertTrue(rollbackGroup.ReadyToCommit &&
                    rollbackGroup.CurrentSnapshot.IsReadyToCommit,
                    "Official handoff group did not reach ReadyToCommit. " +
                    rollbackGroup.ToDiagnosticString());
                completed.Add("handoff-group-begin-ready");

                AssertNotNull(
                    rollbackGroup.CurrentSnapshot.AdmissionDecision,
                    "Ready group has no Activity admission decision.");
                AssertTrue(
                    rollbackGroup.CurrentSnapshot.AdmissionDecision.CanProceed,
                    "Ready group admission did not retain Proceed.");
                completed.Add("group-admission-proceed");

                PlayerGameplayRuntimeHostSnapshot groupSnapshot =
                    GetGameplaySnapshot(gameplayModule);
                AssertTrue(groupSnapshot.HasActiveHandoffGroup &&
                    groupSnapshot.HandoffGroup.Token ==
                        rollbackGroup.CurrentSnapshot.Token,
                    "Host snapshot does not expose the active group.");
                completed.Add("group-host-snapshot-active");

                AssertEqual(1, groupSnapshot.ActivePerSlotHandoffCount,
                    "Host snapshot does not expose the active per-Slot handoff.");
                completed.Add("per-slot-handoff-active");

                ActivityPlayerHandoffGroupResult rolledBack =
                    Invoke<ActivityPlayerHandoffGroupResult>(
                        gameplayModule,
                        "TryRollbackActivityHandoffGroup",
                        rollbackGroup.CurrentSnapshot.Token,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "rollback-official-group");
                AssertNotNull(rolledBack,
                    "Group rollback returned no result.");
                AssertEqual(
                    ActivityPlayerHandoffGroupStatus.SucceededRolledBack,
                    rolledBack.Status,
                    "Official group rollback failed. " +
                    rolledBack.ToDiagnosticString());
                completed.Add("group-rollback-succeeds");

                PlayerActorPreparationSummary restoredPreparation =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule).Preparation,
                        slotId);
                PlayerActorDeclaration restoredDeclaration =
                    ResolveDeclaration(
                        stableHost,
                        restoredPreparation.Materialization.ActorId);
                AssertNotNull(restoredDeclaration,
                    "Previous Actor declaration was not restored.");
                AssertTrue(restoredDeclaration.gameObject.activeInHierarchy,
                    "Previous Actor is not active after group rollback.");
                completed.Add("previous-actor-reactivated");

                PlayerGameplayRuntimeHostSnapshot restoredSnapshot =
                    GetGameplaySnapshot(gameplayModule);
                AssertEqual(1, restoredSnapshot.GameplayReadyCount,
                    "Previous gameplay chain was not rebuilt.");
                PlayerGameplayAdmissionSummary restoredAdmission =
                    FindAdmission(restoredSnapshot.Admission, slotId);
                AssertTrue(restoredAdmission.GameplayReady &&
                    restoredAdmission.ActorId ==
                        restoredPreparation.Materialization.ActorId,
                    "Restored admission does not target the previous Actor.");
                completed.Add("previous-gameplay-chain-restored");

                PlayerActorCandidateRuntimeHostSnapshot candidateAfterRollback =
                    restoredSnapshot.Candidates;
                AssertEqual(1, candidateAfterRollback.StagedInactiveCount,
                    "Rolled-back candidate did not return to staging.");
                completed.Add("rolled-back-candidate-returned-to-staging");

                PlayerActorCandidateStageResult rollbackCleanup =
                    Invoke<PlayerActorCandidateStageResult>(
                        gameplayModule,
                        "TryRollbackCandidate",
                        rollbackCandidateToken,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "cleanup-rollback-candidate");
                AssertTrue(rollbackCleanup.Succeeded,
                    "Rolled-back candidate cleanup failed. " +
                    rollbackCleanup.ToDiagnosticString());
                await Awaitable.NextFrameAsync();
                AssertTrue(rollbackCandidateDeclaration == null,
                    "Rolled-back candidate survived physical cleanup.");
                completed.Add("rolled-back-candidate-cleaned");

                commitTargetContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7f.commit-target." +
                        Guid.NewGuid().ToString("N"),
                    "P3K.7F Commit Target",
                    out _,
                    out _);
                completed.Add("second-target-scope-created");

                PlayerActorCandidateStageResult commitCandidate =
                    Invoke<PlayerActorCandidateStageResult>(
                        gameplayModule,
                        "TryStageCandidate",
                        commitTargetContext,
                        slotId,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "stage-commit-candidate");
                AssertTrue(commitCandidate.Succeeded &&
                    commitCandidate.CurrentSnapshot.IsStagedInactive,
                    "Commit candidate staging failed. " +
                    commitCandidate.ToDiagnosticString());
                PlayerActorCandidateStageToken commitCandidateToken =
                    commitCandidate.CurrentSnapshot.Token;
                PlayerActorDeclaration commitDeclaration =
                    ResolveDeclaration(
                        stableHost,
                        commitCandidateToken.ActorId);
                completed.Add("second-candidate-staged");

                var commitRequests =
                    new[]
                    {
                        new ActivityPlayerHandoffSlotRequest(
                            commitCandidateToken,
                            restoredAdmission.Token)
                    };
                ActivityPlayerHandoffGroupResult commitReady =
                    Invoke<ActivityPlayerHandoffGroupResult>(
                        gameplayModule,
                        "TryBeginActivityHandoffGroup",
                        targetActivity,
                        commitTargetContext.Owner,
                        commitRequests,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "begin-commit-group");
                AssertTrue(commitReady.ReadyToCommit,
                    "Second group did not reach ReadyToCommit. " +
                    commitReady.ToDiagnosticString());
                completed.Add("second-group-ready");

                ActivityPlayerHandoffGroupResult committed =
                    Invoke<ActivityPlayerHandoffGroupResult>(
                        gameplayModule,
                        "TryCommitActivityHandoffGroup",
                        commitReady.CurrentSnapshot.Token,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "commit-official-group");
                AssertNotNull(committed,
                    "Official group commit returned no result.");
                AssertEqual(
                    ActivityPlayerHandoffGroupStatus.SucceededCommitted,
                    committed.Status,
                    "Official group commit failed. " +
                    committed.ToDiagnosticString());
                completed.Add("group-commit-succeeds");

                PlayerActorPreparationSummary committedPreparation =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule).Preparation,
                        slotId);
                AssertEqual(commitCandidateToken.ActorId,
                    committedPreparation.Materialization.ActorId,
                    "Candidate did not become the current P3J Actor.");
                AssertNotNull(commitDeclaration,
                    "Committed candidate declaration is missing.");
                AssertTrue(commitDeclaration.gameObject.activeInHierarchy,
                    "Committed candidate Actor is not active.");
                completed.Add("candidate-became-current");

                PlayerGameplayRuntimeHostSnapshot committedSnapshot =
                    GetGameplaySnapshot(gameplayModule);
                PlayerGameplayAdmissionSummary committedAdmission =
                    FindAdmission(committedSnapshot.Admission, slotId);
                AssertTrue(committedAdmission.GameplayReady &&
                    committedAdmission.ActorId ==
                        commitCandidateToken.ActorId,
                    "Committed gameplay chain is not authoritative.");
                completed.Add("committed-gameplay-chain-authoritative");

                await Awaitable.NextFrameAsync();
                AssertTrue(currentDeclaration == null,
                    "Previous Actor physical instance survived group commit.");
                completed.Add("previous-actor-released");

                ActivityPlayerHandoffGroupResult committedAgain =
                    Invoke<ActivityPlayerHandoffGroupResult>(
                        gameplayModule,
                        "TryCommitActivityHandoffGroup",
                        commitReady.CurrentSnapshot.Token,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "commit-official-group-again");
                AssertEqual(
                    ActivityPlayerHandoffGroupStatus
                        .SucceededAlreadyCommitted,
                    committedAgain.Status,
                    "Committed group is not idempotent.");
                completed.Add("committed-group-idempotent");

                PlayerGameplayRuntimeOperationResult gameplayRelease =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryReleaseCurrentGameplay",
                        slotId,
                        committedAdmission.Token,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "release-committed-gameplay");
                AssertTrue(gameplayRelease.Succeeded &&
                    gameplayRelease.Snapshot.GameplayReadyCount == 0,
                    "Committed gameplay release failed. " +
                    gameplayRelease.ToDiagnosticString());

                PlayerActorPreparationResult actorRelease =
                    Invoke<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryReleasePreparedActor",
                        slotId,
                        committedPreparation.Token,
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "release-committed-actor");
                AssertTrue(actorRelease.Succeeded,
                    "Committed Actor release failed. " +
                    actorRelease.ToDiagnosticString());
                await Awaitable.NextFrameAsync();

                AssertEqual(0,
                    SnapshotHandles(
                        runtimeContent,
                        runtimeContentType,
                        currentContext).Length,
                    "Current Activity scope retained handles.");
                AssertEqual(0,
                    SnapshotHandles(
                        runtimeContent,
                        runtimeContentType,
                        rollbackTargetContext).Length,
                    "Rollback target scope retained handles.");
                AssertEqual(0,
                    SnapshotHandles(
                        runtimeContent,
                        runtimeContentType,
                        commitTargetContext).Length,
                    "Commit target scope retained handles.");

                RemoveScopeRoot(
                    runtimeContent,
                    runtimeContentType,
                    currentContext.Owner);
                RemoveScopeRoot(
                    runtimeContent,
                    runtimeContentType,
                    rollbackTargetContext.Owner);
                RemoveScopeRoot(
                    runtimeContent,
                    runtimeContentType,
                    commitTargetContext.Owner);

                PlayerParticipationOperationResult closed =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryCloseJoining",
                        nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                        "close-joining");
                AssertTrue(closed.Completed && !closed.Snapshot.JoiningOpen,
                    "Joining did not close.");

                PlayerGameplayRuntimeHostSnapshot finalSnapshot =
                    GetGameplaySnapshot(gameplayModule);
                AssertEqual(0, finalSnapshot.OccupiedCount,
                    "Final occupancy is not clean.");
                AssertEqual(0, finalSnapshot.BoundInputCount,
                    "Final input binding is not clean.");
                AssertEqual(0, finalSnapshot.GameplayReadyCount,
                    "Final admission is not clean.");
                AssertEqual(0, finalSnapshot.CandidateCount,
                    "Final candidate runtime is not clean.");
                completed.Add("final-release-and-scopes-clean");

                AssertPublicContractsContainNoUnityReferences(
                    typeof(PlayerGameplayRuntimeHostSnapshot),
                    typeof(PlayerGameplayRuntimeOperationResult),
                    typeof(ActivityPlayerHandoffGroupSnapshot),
                    typeof(PlayerGameplayChainHandoffSnapshot));
                completed.Add("public-contracts-no-unity-references");

                AssertEqual(48, completed.Count,
                    "P3K.7F smoke case count changed.");
                Debug.Log(
                    "[P3K7F_SESSION_GAMEPLAY_RUNTIME_COMPOSITION_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"session='{finalSnapshot.SessionContextId}' " +
                    $"slot='{slotId.StableText}' " +
                    $"committedActor='{commitCandidateToken.ActorId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                Debug.LogError(
                    "[P3K7F_SESSION_GAMEPLAY_RUNTIME_COMPOSITION_SMOKE] " +
                    $"status='Failed' exception='{inner.GetType().Name}' " +
                    $"message='{Escape(inner.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw inner;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K7F_SESSION_GAMEPLAY_RUNTIME_COMPOSITION_SMOKE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.Destroy(created[index]);
                    }
                }
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        private static LocalPlayerProvisioningAuthoring ResolveAuthoring()
        {
            LocalPlayerProvisioningAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<
                    LocalPlayerProvisioningAuthoring>(
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

        private static object ResolveCurrentRuntimeHost()
        {
            Type runtimeHostType = ResolveRuntimeType(RuntimeHostTypeName);
            MethodInfo tryGetCurrent =
                runtimeHostType.GetMethod("TryGetCurrent", StaticAny);
            AssertNotNull(tryGetCurrent,
                "FrameworkRuntimeHost.TryGetCurrent was not found.");
            object[] arguments = { null };
            bool resolved = (bool)tryGetCurrent.Invoke(null, arguments);
            AssertTrue(resolved && arguments[0] != null,
                "Current FrameworkRuntimeHost was not resolved.");
            return arguments[0];
        }

        private static object ResolveHostComponent(
            object runtimeHost,
            string typeName,
            string label)
        {
            Type type = ResolveRuntimeType(typeName);
            Component host = runtimeHost as Component;
            AssertNotNull(host,
                "FrameworkRuntimeHost is not a Unity Component.");
            Component component = host.GetComponent(type);
            AssertNotNull(component,
                $"FrameworkRuntimeHost has no {label}.");
            return component;
        }

        private static PlayerActorPreparationRuntimeHostSnapshot
            GetPreparationSnapshot(object module)
        {
            object[] arguments = { null };
            bool available = (bool)GetMethod(
                module.GetType(),
                "TryGetSnapshot").Invoke(module, arguments);
            var snapshot =
                arguments[0] as PlayerActorPreparationRuntimeHostSnapshot;
            AssertNotNull(snapshot,
                "P3J preparation runtime-host snapshot is missing.");
            AssertTrue(available || !snapshot.IsInitialized,
                "P3J snapshot availability and initialization disagree.");
            return snapshot;
        }

        private static PlayerGameplayRuntimeHostSnapshot
            GetGameplaySnapshot(object module)
        {
            object[] arguments = { null };
            bool available = (bool)GetMethod(
                module.GetType(),
                "TryGetSnapshot").Invoke(module, arguments);
            var snapshot =
                arguments[0] as PlayerGameplayRuntimeHostSnapshot;
            AssertNotNull(snapshot,
                "Player gameplay runtime-host snapshot is missing.");
            AssertTrue(available || !snapshot.IsInitialized,
                "Gameplay snapshot availability and initialization disagree.");
            return snapshot;
        }

        private static void ValidateEndpointSourceShape(
            object runtimeHost)
        {
            Type sourceType = ResolveRuntimeType(EndpointSourceTypeName);
            AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(sourceType),
                "Endpoint source must remain a plain runtime adapter.");

            FieldInfo[] fields = sourceType.GetFields(InstanceAny);
            for (int index = 0; index < fields.Length; index++)
            {
                AssertTrue(
                    fields[index].FieldType !=
                        typeof(UnityPlayerInputGateAdapter),
                    "Multi-Slot endpoint source retained one fixed Gate adapter.");
            }

            MethodInfo outputMethod =
                runtimeHost.GetType().GetMethod(
                    "TryGetPlayerGameplayCameraOutputSession",
                    InstanceAny);
            AssertNotNull(outputMethod,
                "FrameworkRuntimeHost Player gameplay camera output surface is missing.");
            object[] outputArguments = { null, null };
            bool outputAvailable =
                (bool)outputMethod.Invoke(runtimeHost, outputArguments);
            AssertTrue(outputAvailable &&
                outputArguments[0] is CameraOutputSessionBinding,
                "FrameworkRuntimeHost did not retain the Session camera output. " +
                (outputArguments[1] as string));
        }

        private static PlayerActorPreparationSummary FindPreparation(
            PlayerActorPreparationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            AssertNotNull(snapshot,
                "P3J preparation snapshot is missing.");
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
            AssertNotNull(snapshot,
                "P3K.5 admission snapshot is missing.");
            AssertTrue(snapshot.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary summary),
                $"P3K.5 admission snapshot has no Slot '{playerSlotId.StableText}'.");
            return summary;
        }

        private static PlayerActorDeclaration ResolveDeclaration(
            LocalPlayerHostAuthoring host,
            ActorId actorId)
        {
            if (host == null ||
                host.ActorMount == null ||
                !actorId.IsValid)
            {
                return null;
            }

            PlayerActorDeclaration[] declarations =
                host.ActorMount.GetComponentsInChildren<
                    PlayerActorDeclaration>(true);
            for (int index = 0; index < declarations.Length; index++)
            {
                if (declarations[index] != null &&
                    declarations[index].ActorId == actorId)
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
            AssertNotNull(host,
                "Stable Local Player Host is missing.");
            AssertNotNull(playerInput,
                "Stable PlayerInput is missing.");
            AssertNotNull(playerInput.actions,
                "Stable PlayerInput has no InputActionAsset.");

            string actionMapName =
                ResolveGameplayActionMapName(playerInput);
            AssertTrue(!string.IsNullOrEmpty(actionMapName),
                "Stable PlayerInput has no usable action map.");

            UnityPlayerInputGateAdapter adapter =
                host.GetComponent<UnityPlayerInputGateAdapter>();
            if (adapter == null)
            {
                adapter =
                    host.gameObject.AddComponent<
                        UnityPlayerInputGateAdapter>();
            }

            SerializedObject serialized = new SerializedObject(adapter);
            SerializedProperty playerInputProperty =
                serialized.FindProperty("playerInput");
            SerializedProperty sourceSlotProperty =
                serialized.FindProperty("sourceSlot");
            SerializedProperty actionMapProperty =
                serialized.FindProperty("gameplayActionMapName");
            AssertNotNull(playerInputProperty,
                "Gate adapter playerInput property was not found.");
            AssertNotNull(sourceSlotProperty,
                "Gate adapter sourceSlot property was not found.");
            AssertNotNull(actionMapProperty,
                "Gate adapter gameplayActionMapName property was not found.");
            playerInputProperty.objectReferenceValue = playerInput;
            sourceSlotProperty.objectReferenceValue =
                host.PlayerSlotDeclaration;
            actionMapProperty.stringValue = actionMapName;

            SerializedProperty logState =
                serialized.FindProperty("logStateChanges");
            SerializedProperty logRuntime =
                serialized.FindProperty("logMissingRuntimeOnce");
            SerializedProperty logTarget =
                serialized.FindProperty("logMissingTargetOnce");
            if (logState != null) logState.boolValue = false;
            if (logRuntime != null) logRuntime.boolValue = false;
            if (logTarget != null) logTarget.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return adapter;
        }

        private static string ResolveGameplayActionMapName(
            PlayerInput playerInput)
        {
            if (playerInput.currentActionMap != null)
            {
                return playerInput.currentActionMap.name;
            }

            if (!string.IsNullOrWhiteSpace(
                    playerInput.defaultActionMap) &&
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

        private static ActivityAsset CreateGameplayReadyActivity(
            PlayerSlotProfile slotProfile,
            List<UnityEngine.Object> created)
        {
            ActivityParticipationProjectionProfile projection =
                ScriptableObject.CreateInstance<
                    ActivityParticipationProjectionProfile>();
            projection.name = "P3K.7F Explicit Player";
            created.Add(projection);
            SerializedObject projectionSerialized =
                new SerializedObject(projection);
            projectionSerialized.FindProperty("displayName").stringValue =
                "P3K.7F Explicit Player";
            projectionSerialized.FindProperty("projectionMode").intValue =
                (int)ActivityParticipationProjectionMode.ExplicitSlots;
            projectionSerialized.FindProperty(
                    "zeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Rejected;
            SerializedProperty slots =
                projectionSerialized.FindProperty(
                    "explicitSlotProfiles");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue =
                slotProfile;
            projectionSerialized.ApplyModifiedPropertiesWithoutUndo();

            PlayerParticipationRequirementsProfile requirements =
                ScriptableObject.CreateInstance<
                    PlayerParticipationRequirementsProfile>();
            requirements.name = "P3K.7F Gameplay Ready";
            created.Add(requirements);
            SerializedObject requirementsSerialized =
                new SerializedObject(requirements);
            requirementsSerialized.FindProperty("displayName").stringValue =
                "P3K.7F Gameplay Ready";
            requirementsSerialized.FindProperty(
                    "requirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.GameplayReady;
            requirementsSerialized.ApplyModifiedPropertiesWithoutUndo();

            ActivityAsset activity =
                ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = "P3K.7F Target Activity";
            created.Add(activity);
            SerializedObject activitySerialized =
                new SerializedObject(activity);
            activitySerialized.FindProperty("activityName").stringValue =
                "P3K.7F Target Activity";
            activitySerialized.FindProperty(
                    "playerParticipationProjectionProfile")
                .objectReferenceValue = projection;
            activitySerialized.FindProperty(
                    "playerParticipationRequirementsProfile")
                .objectReferenceValue = requirements;
            activitySerialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static RuntimeScopeContext CreateActivityScopeContext(
            object runtimeHost,
            string ownerId,
            string displayName,
            out object runtimeContent,
            out Type runtimeContentType)
        {
            PropertyInfo runtimeContentProperty =
                runtimeHost.GetType().GetProperty(
                    "RuntimeContentRuntime",
                    InstanceAny);
            AssertNotNull(runtimeContentProperty,
                "FrameworkRuntimeHost.RuntimeContentRuntime was not found.");
            runtimeContent =
                runtimeContentProperty.GetValue(runtimeHost);
            AssertNotNull(runtimeContent,
                "FrameworkRuntimeHost has no RuntimeContentRuntime.");
            runtimeContentType = runtimeContent.GetType();

            RuntimeContentOwner owner =
                RuntimeContentOwner.Activity(ownerId, displayName);
            GetMethod(runtimeContentType, "CreateScopeRoot").Invoke(
                runtimeContent,
                new object[]
                {
                    owner,
                    nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                    "create-session-gameplay-scope"
                });

            object[] contextArguments =
            {
                owner,
                nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                "session-gameplay-runtime-composition",
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
            return GetMethod(
                    runtimeContentType,
                    "SnapshotHandles").Invoke(
                        runtimeContent,
                        new object[] { context })
                    as RuntimeContentHandle[] ??
                Array.Empty<RuntimeContentHandle>();
        }

        private static void RemoveScopeRoot(
            object runtimeContent,
            Type runtimeContentType,
            RuntimeContentOwner owner)
        {
            object result = GetMethod(
                    runtimeContentType,
                    "RemoveScopeRoot").Invoke(
                        runtimeContent,
                        new object[]
                        {
                            owner,
                            nameof(QaP3K7FSessionGameplayRuntimeCompositionSmoke),
                            "session-gameplay-runtime-cleanup"
                        });
            AssertNotNull(result,
                $"RuntimeContent scope removal returned no result for '{owner.StableText}'.");
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type =
                typeof(PlayerGameplayRuntimeHostSnapshot)
                    .Assembly.GetType(fullName, false);
            AssertNotNull(type,
                $"Runtime type '{fullName}' was not found.");
            return type;
        }

        private static MethodInfo GetMethod(
            Type type,
            string methodName,
            BindingFlags flags = default)
        {
            BindingFlags resolvedFlags =
                flags == default ? InstanceAny : flags;
            MethodInfo method =
                type.GetMethod(methodName, resolvedFlags);
            AssertNotNull(method,
                $"Method '{type.FullName}.{methodName}' was not found.");
            return method;
        }

        private static T Invoke<T>(
            object target,
            string methodName,
            params object[] arguments)
            where T : class
        {
            return GetMethod(
                    target.GetType(),
                    methodName).Invoke(
                        target,
                        arguments) as T;
        }

        private static void AssertPublicContractsContainNoUnityReferences(
            params Type[] contractTypes)
        {
            for (int typeIndex = 0;
                 typeIndex < contractTypes.Length;
                 typeIndex++)
            {
                Type type = contractTypes[typeIndex];
                PropertyInfo[] properties =
                    type.GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public);
                for (int index = 0;
                     index < properties.Length;
                     index++)
                {
                    AssertTrue(
                        !typeof(UnityEngine.Object).IsAssignableFrom(
                            properties[index].PropertyType),
                        $"Public contract '{type.FullName}' property " +
                        $"'{properties[index].Name}' retains a Unity object reference.");
                }

                FieldInfo[] fields =
                    type.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public);
                for (int index = 0;
                     index < fields.Length;
                     index++)
                {
                    AssertTrue(
                        !typeof(UnityEngine.Object).IsAssignableFrom(
                            fields[index].FieldType),
                        $"Public contract '{type.FullName}' field " +
                        $"'{fields[index].Name}' retains a Unity object reference.");
                }
            }
        }

        private static void AssertTrue(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(
            object value,
            string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertSame(
            object expected,
            object actual,
            string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(
                    expected,
                    actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

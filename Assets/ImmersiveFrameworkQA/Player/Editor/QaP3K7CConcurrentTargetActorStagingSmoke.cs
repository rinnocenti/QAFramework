using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// One-shot Play Mode proof that a target Activity Actor candidate may coexist inactive
    /// with the current active P3J Actor without mutating current preparation authority.
    /// </summary>
    public static class QaP3K7CConcurrentTargetActorStagingSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.7C Run Concurrent Target Actor Staging Smoke";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";
        private const string CandidateModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorCandidateRuntimeHostModule";

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
                    "P3K.7C concurrent candidate smoke must run in Play Mode.");
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
                    "P3K.7C smoke is one-shot. Re-enter Play Mode before running again.");
                AssertEqual(0, authoring.RuntimeSnapshot.JoinedCount,
                    "Session already contains a Joined Player.");
                AssertEqual(0, initialPreparation.PreparedCount,
                    "P3J preparation already contains an active Actor.");
                completed.Add("initial-runtime-state-clean");

                PlayerParticipationOperationResult opened =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryOpenJoining",
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "concurrent-target-actor-staging");
                AssertTrue(opened.Completed && opened.Snapshot.JoiningOpen,
                    "Opening joining failed. " + opened.ToDiagnosticString());
                completed.Add("joining-opened");

                LocalPlayerJoinResult joined = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "concurrent-target-actor-staging"));
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
                    nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                    "select-default-current-actor");
                AssertNotNull(selected, "Default Actor selection returned no result.");
                AssertTrue(selected.Succeeded && selected.SelectedActorProfile != null,
                    "Default Actor selection failed. " + selected.ToDiagnosticString());
                completed.Add("default-actor-selected");

                RuntimeScopeContext currentContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7c.current." + Guid.NewGuid().ToString("N"),
                    "P3K.7C Current Activity",
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
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "prepare-current-activity-actor");
                AssertNotNull(prepared, "Current Actor preparation returned no result.");
                AssertTrue(prepared.Succeeded && prepared.CurrentSummary.IsPrepared,
                    "Current Actor preparation failed. " + prepared.ToDiagnosticString());
                completed.Add("current-activity-actor-prepared");

                PlayerActorDeclaration currentDeclaration = ResolveDeclaration(
                    stableHost,
                    prepared.CurrentSummary.Materialization.ActorId);
                AssertNotNull(currentDeclaration,
                    "Current prepared Actor declaration was not found.");
                AssertTrue(currentDeclaration.gameObject.activeInHierarchy,
                    "Current prepared Actor is not active.");
                AssertSame(stablePlayerInput, currentDeclaration.PlayerInput,
                    "Current Actor lost stable PlayerInput evidence.");
                completed.Add("current-actor-active-and-bound");

                object candidateModule = AttachCandidateModule(runtimeHost);
                Type candidateContextType = ResolveRuntimeType(
                    "Immersive.Framework.PlayerParticipation.PlayerActorCandidateStageRuntimeContext");
                AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(candidateContextType),
                    "Candidate staging domain context must remain plain C#.");
                AssertTrue(candidateContextType.GetMethod("PromoteCandidate", InstanceAny) == null,
                    "P3K.7C candidate context must not expose promotion behavior.");
                completed.Add("candidate-context-plain-and-no-promotion");

                Component runtimeHostComponent = runtimeHost as Component;
                Component candidateModuleComponent = candidateModule as Component;
                AssertNotNull(runtimeHostComponent,
                    "FrameworkRuntimeHost is not a Unity Component.");
                AssertNotNull(candidateModuleComponent,
                    "Candidate runtime module is not a Unity Component.");
                AssertSame(runtimeHostComponent.gameObject,
                    candidateModuleComponent.gameObject,
                    "Candidate module is not composed on the explicit FrameworkRuntimeHost.");
                completed.Add("candidate-module-host-scoped");

                PlayerActorCandidateRuntimeHostSnapshot initialCandidate =
                    GetCandidateSnapshot(candidateModule);
                AssertTrue(initialCandidate.IsInitialized &&
                    initialCandidate.SessionContextId == authoring.RuntimeSnapshot.ContextId,
                    "Candidate module is not initialized against the current Session.");
                AssertEqual(0, initialCandidate.CandidateCount,
                    "Candidate module is not initially clean.");
                completed.Add("candidate-module-attached");

                PlayerActorCandidateStageResult sameOwner =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryStageCandidate",
                        currentContext,
                        slotId,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "reject-current-owner-as-target");
                AssertEqual(
                    PlayerActorCandidateStageStatus.RejectedTargetOwnerMatchesCurrent,
                    sameOwner.Status,
                    "Candidate staging accepted the current Actor owner.");
                completed.Add("current-owner-rejected-as-target");

                RuntimeScopeContext targetContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7c.target." + Guid.NewGuid().ToString("N"),
                    "P3K.7C Target Activity",
                    out _,
                    out _);
                AssertTrue(targetContext.IsValid,
                    "Target Activity scope context is invalid.");
                completed.Add("target-activity-scope-created");

                PlayerActorCandidateStageResult staged =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryStageCandidate",
                        targetContext,
                        slotId,
                        " qa.p3k7c ",
                        " stage-target-candidate ");
                AssertEqual(PlayerActorCandidateStageStatus.SucceededStaged,
                    staged.Status,
                    "Target Actor candidate staging failed. " + staged.ToDiagnosticString());
                AssertTrue(staged.CurrentSnapshot != null &&
                    staged.CurrentSnapshot.IsStagedInactive,
                    "Candidate result has no staged-inactive snapshot.");
                completed.Add("target-actor-candidate-staged");

                PlayerActorCandidateStageToken candidateToken =
                    staged.CurrentSnapshot.Token;
                AssertTrue(candidateToken.IsValid &&
                    candidateToken.Owner == targetContext.Owner,
                    "Candidate token does not retain exact target Activity ownership.");
                AssertEqual("qa.p3k7c", staged.CurrentSnapshot.Source,
                    "Candidate source normalization changed unexpectedly.");
                AssertEqual("stage-target-candidate", staged.CurrentSnapshot.Reason,
                    "Candidate reason normalization changed unexpectedly.");
                completed.Add("candidate-token-and-diagnostics-valid");

                AssertTrue(staged.CurrentSnapshot.HasCurrentPreparation,
                    "Candidate snapshot did not retain current preparation evidence.");
                AssertEqual(prepared.CurrentSummary.Token,
                    staged.CurrentSnapshot.CurrentPreparationToken,
                    "Candidate snapshot retained another current preparation token.");
                AssertEqual(prepared.CurrentSummary.Materialization.ActorId,
                    staged.CurrentSnapshot.CurrentActorId,
                    "Candidate snapshot retained another current Actor identity.");
                AssertEqual(currentContext.Owner,
                    staged.CurrentSnapshot.CurrentOwner,
                    "Candidate snapshot retained another current owner.");
                completed.Add("current-preparation-evidence-retained");

                object[] physicalArguments =
                {
                    candidateToken,
                    null,
                    null,
                    null,
                    null,
                    null
                };
                bool physicalAvailable = (bool)GetMethod(
                    candidateModule.GetType(),
                    "TryGetCandidatePhysicalEvidence").Invoke(
                        candidateModule,
                        physicalArguments);
                AssertTrue(physicalAvailable,
                    "Candidate physical evidence was not resolved. " +
                    (physicalArguments[5] as string));
                var candidateHost = physicalArguments[1] as LocalPlayerHostAuthoring;
                var candidatePlayerInput = physicalArguments[2] as PlayerInput;
                var candidateDeclaration = physicalArguments[3] as PlayerActorDeclaration;
                var candidateObject = physicalArguments[4] as GameObject;
                AssertNotNull(candidateDeclaration,
                    "Candidate physical evidence has no PlayerActorDeclaration.");
                AssertNotNull(candidateObject,
                    "Candidate physical evidence has no Logical Actor object.");
                completed.Add("candidate-physical-evidence-resolved");

                AssertSame(stableHost, candidateHost,
                    "Candidate does not use the stable Local Player Host.");
                AssertSame(stablePlayerInput, candidatePlayerInput,
                    "Candidate does not retain the stable PlayerInput.");
                AssertSame(stablePlayerInput, candidateDeclaration.PlayerInput,
                    "Candidate declaration does not retain stable PlayerInput evidence.");
                completed.Add("candidate-reuses-stable-host-and-input");

                AssertTrue(!ReferenceEquals(currentDeclaration, candidateDeclaration),
                    "Candidate reused the current PlayerActorDeclaration.");
                AssertTrue(candidateToken.ActorId != prepared.CurrentSummary.Materialization.ActorId,
                    "Candidate reused the current runtime ActorId.");
                AssertEqual(candidateToken.ActorId, candidateDeclaration.ActorId,
                    "Candidate declaration ActorId differs from the exact candidate token.");
                AssertTrue(candidateToken.RuntimeContentIdentity !=
                    prepared.CurrentSummary.Materialization.RuntimeContentIdentity,
                    "Candidate reused current RuntimeContent identity.");
                completed.Add("candidate-has-distinct-runtime-identity");

                AssertTrue(!candidateObject.activeSelf &&
                    !candidateObject.activeInHierarchy,
                    "Candidate Actor must remain inactive before promotion.");
                AssertTrue(currentDeclaration.gameObject.activeInHierarchy,
                    "Current Actor was deactivated by candidate staging.");
                completed.Add("candidate-inactive-current-remains-active");

                PlayerActorDeclaration[] declarations =
                    stableHost.ActorMount.GetComponentsInChildren<PlayerActorDeclaration>(true);
                AssertEqual(2, declarations.Length,
                    "Actor Mount does not contain exactly current + candidate declarations.");
                completed.Add("current-and-candidate-coexist");

                PlayerActorPreparationRuntimeHostSnapshot unchangedPreparation =
                    GetPreparationSnapshot(preparationModule);
                PlayerActorPreparationSummary unchangedCurrent = FindPreparation(
                    unchangedPreparation.Preparation,
                    slotId);
                AssertEqual(1, unchangedPreparation.PreparedCount,
                    "Candidate staging changed P3J prepared Actor count.");
                AssertEqual(prepared.CurrentSummary.Token,
                    unchangedCurrent.Token,
                    "Candidate staging replaced the current P3J preparation token.");
                completed.Add("p3j-authority-remains-unchanged");

                RuntimeContentHandle[] currentHandles = SnapshotHandles(
                    runtimeContent,
                    runtimeContentType,
                    currentContext);
                RuntimeContentHandle[] targetHandles = SnapshotHandles(
                    runtimeContent,
                    runtimeContentType,
                    targetContext);
                AssertEqual(1, currentHandles.Length,
                    "Current Activity scope lost its prepared Actor handle.");
                AssertEqual(1, targetHandles.Length,
                    "Target Activity scope did not receive one candidate handle.");
                completed.Add("runtime-content-owners-coexist");

                PlayerActorCandidateStageResult idempotent =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryStageCandidate",
                        targetContext,
                        slotId,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "stage-target-candidate-again");
                AssertEqual(PlayerActorCandidateStageStatus.SucceededAlreadyStaged,
                    idempotent.Status,
                    "Repeated candidate staging was not idempotent.");
                AssertEqual(candidateToken, idempotent.CurrentSnapshot.Token,
                    "Idempotent staging replaced candidate identity.");
                completed.Add("candidate-staging-idempotent");

                RuntimeScopeContext anotherTargetContext = CreateActivityScopeContext(
                    runtimeHost,
                    "qa.p3k7c.another." + Guid.NewGuid().ToString("N"),
                    "P3K.7C Another Target Activity",
                    out _,
                    out _);
                PlayerActorCandidateStageResult conflicting =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryStageCandidate",
                        anotherTargetContext,
                        slotId,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "reject-second-candidate");
                AssertEqual(PlayerActorCandidateStageStatus.RejectedAnotherCandidateActive,
                    conflicting.Status,
                    "A second candidate was accepted for the same Slot.");
                completed.Add("second-candidate-rejected");

                PlayerActorCandidateStageResult invalidRollback =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryRollbackCandidate",
                        default(PlayerActorCandidateStageToken),
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "reject-invalid-candidate-token");
                AssertEqual(
                    PlayerActorCandidateStageStatus.RejectedForeignOrStaleCandidate,
                    invalidRollback.Status,
                    "Invalid candidate token was accepted for rollback.");
                completed.Add("rollback-token-guarded");

                PlayerActorCandidateStageResult rolledBack =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryRollbackCandidate",
                        candidateToken,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "rollback-target-candidate");
                AssertEqual(PlayerActorCandidateStageStatus.SucceededRolledBack,
                    rolledBack.Status,
                    "Exact candidate rollback failed. " + rolledBack.ToDiagnosticString());
                completed.Add("candidate-rollback-succeeds");

                await Awaitable.NextFrameAsync();
                AssertTrue(candidateDeclaration == null && candidateObject == null,
                    "Rolled-back candidate was not destroyed after the frame boundary.");
                completed.Add("candidate-physical-instance-destroyed");

                AssertTrue(currentDeclaration != null &&
                    currentDeclaration.gameObject.activeInHierarchy,
                    "Candidate rollback disturbed the current active Actor.");
                PlayerActorPreparationSummary currentAfterRollback = FindPreparation(
                    GetPreparationSnapshot(preparationModule).Preparation,
                    slotId);
                AssertEqual(prepared.CurrentSummary.Token,
                    currentAfterRollback.Token,
                    "Candidate rollback changed current preparation identity.");
                completed.Add("rollback-preserves-current-actor");

                AssertEqual(0, SnapshotHandles(
                        runtimeContent,
                        runtimeContentType,
                        targetContext).Length,
                    "Candidate rollback left a target RuntimeContent handle.");
                AssertEqual(1, SnapshotHandles(
                        runtimeContent,
                        runtimeContentType,
                        currentContext).Length,
                    "Candidate rollback removed the current RuntimeContent handle.");
                completed.Add("rollback-cleans-only-target-runtime-content");

                PlayerActorCandidateStageResult repeatedRollback =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryRollbackCandidate",
                        candidateToken,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "rollback-target-candidate-again");
                AssertEqual(
                    PlayerActorCandidateStageStatus.SucceededAlreadyRolledBack,
                    repeatedRollback.Status,
                    "Repeated exact candidate rollback was not idempotent.");
                completed.Add("candidate-rollback-idempotent");

                PlayerActorCandidateStageResult restaged =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryStageCandidate",
                        targetContext,
                        slotId,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "restage-target-candidate");
                AssertEqual(PlayerActorCandidateStageStatus.SucceededStaged,
                    restaged.Status,
                    "Candidate could not be staged again after rollback.");
                AssertTrue(restaged.CurrentSnapshot.Token.IsValid &&
                    restaged.CurrentSnapshot.Token != candidateToken,
                    "Restaging did not generate a new exact candidate token.");
                completed.Add("restaging-generates-new-token");

                PlayerActorCandidateStageResult staleAfterRestage =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryRollbackCandidate",
                        candidateToken,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "reject-old-token-after-restage");
                AssertEqual(
                    PlayerActorCandidateStageStatus.RejectedForeignOrStaleCandidate,
                    staleAfterRestage.Status,
                    "Old candidate token remained functional after restaging.");
                completed.Add("old-token-stale-after-restage");

                PlayerActorCandidateStageResult restageRollback =
                    Invoke<PlayerActorCandidateStageResult>(
                        candidateModule,
                        "TryRollbackCandidate",
                        restaged.CurrentSnapshot.Token,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "rollback-restaged-candidate");
                AssertTrue(restageRollback.Succeeded,
                    "Restaged candidate rollback failed. " +
                    restageRollback.ToDiagnosticString());
                await Awaitable.NextFrameAsync();
                completed.Add("restaged-candidate-rolled-back");

                PlayerActorCandidateRuntimeHostSnapshot finalCandidates =
                    GetCandidateSnapshot(candidateModule);
                AssertEqual(0, finalCandidates.CandidateCount,
                    "Candidate runtime retained a candidate after rollback.");
                AssertEqual(0, finalCandidates.RollbackFailedCount,
                    "Candidate runtime retained rollback failure evidence.");
                completed.Add("candidate-runtime-final-snapshot-clean");

                AssertPublicContractsContainNoUnityReferences(
                    typeof(PlayerActorCandidateStageToken),
                    typeof(PlayerActorCandidateStageSnapshot),
                    typeof(PlayerActorCandidateStageResult),
                    typeof(PlayerActorCandidateRuntimeHostSnapshot));
                completed.Add("public-candidate-contracts-no-unity-references");

                PlayerActorPreparationResult releasedCurrent =
                    Invoke<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryReleasePreparedActor",
                        slotId,
                        prepared.CurrentSummary.Token,
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "release-current-actor-after-candidate-proof");
                AssertTrue(releasedCurrent.Succeeded,
                    "Current Actor cleanup failed. " +
                    releasedCurrent.ToDiagnosticString());
                await Awaitable.NextFrameAsync();
                AssertTrue(currentDeclaration == null,
                    "Current Actor was not destroyed during final cleanup.");
                completed.Add("current-actor-released-after-proof");

                AssertEqual(0, SnapshotHandles(
                        runtimeContent,
                        runtimeContentType,
                        currentContext).Length,
                    "Current Activity scope retained a RuntimeContent handle after cleanup.");
                RemoveScopeRoot(runtimeContent, runtimeContentType, anotherTargetContext.Owner);
                RemoveScopeRoot(runtimeContent, runtimeContentType, targetContext.Owner);
                RemoveScopeRoot(runtimeContent, runtimeContentType, currentContext.Owner);
                completed.Add("runtime-content-scopes-cleaned");

                AssertSame(stablePlayerInput, stableHost.PlayerInput,
                    "Candidate/current cleanup replaced the stable PlayerInput.");
                AssertTrue(stableHost.IsJoined && stableHost.JoinedPlayerSlotId == slotId,
                    "Candidate/current cleanup removed Joined Slot evidence.");
                completed.Add("stable-session-host-survives");

                PlayerParticipationOperationResult closed =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryCloseJoining",
                        nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                        "concurrent-target-actor-staging-complete");
                AssertTrue(closed.Completed && !closed.Snapshot.JoiningOpen,
                    "Closing joining failed. " + closed.ToDiagnosticString());
                completed.Add("joining-closed");

                AssertEqual(43, completed.Count,
                    "P3K.7C smoke case count changed unexpectedly.");
                Debug.Log(
                    "[P3K7C_CONCURRENT_TARGET_ACTOR_STAGING_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' session='{finalCandidates.SessionContextId}' " +
                    $"slot='{slotId.StableText}' currentActor='{prepared.CurrentSummary.Materialization.ActorId.StableText}' " +
                    $"candidateActor='{candidateToken.ActorId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                Debug.LogError(
                    "[P3K7C_CONCURRENT_TARGET_ACTOR_STAGING_SMOKE] status='Failed' " +
                    $"exception='{inner.GetType().Name}' message='{Escape(inner.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw inner;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K7C_CONCURRENT_TARGET_ACTOR_STAGING_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        private static object ResolveCurrentRuntimeHost()
        {
            Type runtimeHostType = ResolveRuntimeType(RuntimeHostTypeName);
            MethodInfo tryGetCurrent = runtimeHostType.GetMethod(
                "TryGetCurrent",
                StaticAny);
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

        private static PlayerActorDeclaration ResolveDeclaration(
            LocalPlayerHostAuthoring host,
            ActorId actorId)
        {
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
                    nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                    "create-concurrent-candidate-scope"
                });

            object[] contextArguments =
            {
                owner,
                nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                "concurrent-target-actor-staging",
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
                    nameof(QaP3K7CConcurrentTargetActorStagingSmoke),
                    "concurrent-target-actor-staging-cleanup"
                });
            AssertNotNull(result,
                $"RuntimeContent scope removal returned no result for '{owner.StableText}'.");
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

        private static T Invoke<T>(object target, string methodName, params object[] arguments)
            where T : class
        {
            return GetMethod(target.GetType(), methodName).Invoke(target, arguments) as T;
        }

        private static MethodInfo GetMethod(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName, InstanceAny);
            AssertNotNull(method,
                $"Method '{type.FullName}.{methodName}' was not found.");
            return method;
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = typeof(PlayerActorCandidateStageResult).Assembly.GetType(
                fullName,
                false);
            AssertNotNull(type,
                $"Runtime type '{fullName}' was not found.");
            return type;
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

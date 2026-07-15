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

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3K7BStagedActivityAdmissionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.7B Run Staged Activity Admission Transaction Smoke";
        private const string ContextTypeName =
            "Immersive.Framework.PlayerParticipation.ActivityPlayerAdmissionStageRuntimeContext";
        private const string GateTypeName =
            "Immersive.Framework.PlayerParticipation.ActivityPlayerAdmissionFlowGate";
        private const string CommitTypeName =
            "Immersive.Framework.PlayerParticipation.ActivityPlayerAdmissionStageCommit";
        private const string ChainResolverTypeName =
            "Immersive.Framework.PlayerParticipation.ActivityPlayerGameplayChainStageResolver";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                AssertTrue(Application.isPlaying,
                    "P3K.7B smoke must run in Play Mode.");

                Assembly assembly = typeof(ActivityPlayerAdmissionStageResult).Assembly;
                Type contextType = assembly.GetType(ContextTypeName, false);
                Type gateType = assembly.GetType(GateTypeName, false);
                Type commitType = assembly.GetType(CommitTypeName, false);
                Type chainResolverType = assembly.GetType(ChainResolverTypeName, false);
                AssertNotNull(contextType, "P3K.7B stage runtime context is missing.");
                AssertNotNull(gateType, "P3K.7A flow gate is missing.");
                AssertNotNull(commitType, "P3K.7B commit handoff is missing.");
                AssertNotNull(chainResolverType, "P3K.7B real chain resolver is missing.");
                ValidateContractSurface(contextType, commitType, chainResolverType);
                completed.Add("contract-surface-valid");

                AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(contextType),
                    "P3K.7B transaction must remain a plain scoped runtime object.");
                completed.Add("transaction-not-monobehaviour");

                PlayerParticipationRequirementsProfile none = CreateRequirements(
                    PlayerParticipationRequirementLevel.None,
                    "None",
                    created);
                PlayerParticipationRequirementsProfile gameplay = CreateRequirements(
                    PlayerParticipationRequirementLevel.GameplayReady,
                    "Gameplay Ready",
                    created);
                ActivityParticipationProjectionProfile noSlots =
                    CreateNoSlotsProjection(created);
                ActivityAsset noPlayers = CreateActivity(
                    noSlots,
                    none,
                    "P3K.7B No Players",
                    created);
                ActivityAsset contradictory = CreateActivity(
                    noSlots,
                    gameplay,
                    "P3K.7B Contradictory",
                    created);

                PlayerParticipationRequirementsProfile selectedActors =
                    CreateRequirements(
                        PlayerParticipationRequirementLevel.SelectedActors,
                        "Selected Actors",
                        created);
                ActivityParticipationProjectionProfile allJoined =
                    CreateAllJoinedProjection(created);
                PlayerSlotProfile selectionSlot = CreateSlotProfile(
                    "player.p3k7b.selection",
                    "P3K.7B Selection Slot",
                    created);
                ActorProfile selectionActor = CreateActorProfile(
                    "actor-profile.p3k7b.selection",
                    "P3K.7B Selection Actor",
                    created);
                ActivityAsset selectionActivity = CreateActivity(
                    allJoined,
                    selectedActors,
                    "P3K.7B Selected Actors",
                    created);

                var baseScope = new FakeScopeRuntime();
                var baseResolver = new FakeResolver();
                object context = CreateContext(contextType, gateType, baseScope, baseResolver);

                ActivityPlayerAdmissionStageResult invalid = Stage(
                    contextType, context, null, "qa", "missing-activity");
                AssertStatus(invalid,
                    ActivityPlayerAdmissionStageStatus.RejectedInvalidRequest,
                    "Missing Activity was not rejected.");
                completed.Add("missing-activity-rejected");

                var failedScope = new FakeScopeRuntime { FailCreate = true };
                context = CreateContext(contextType, gateType, failedScope, new FakeResolver());
                ActivityPlayerAdmissionStageResult scopeFailure = Stage(
                    contextType, context, noPlayers, "qa", "scope-failure");
                AssertStatus(scopeFailure,
                    ActivityPlayerAdmissionStageStatus.FailedScopeCreation,
                    "Scope creation failure was not explicit.");
                completed.Add("scope-creation-failure-explicit");

                var resolutionScope = new FakeScopeRuntime();
                var resolutionResolver = new FakeResolver { FailResolve = true };
                context = CreateContext(contextType, gateType, resolutionScope, resolutionResolver);
                ActivityPlayerAdmissionStageResult resolutionFailure = Stage(
                    contextType, context, noPlayers, "qa", "resolver-failure");
                AssertStatus(resolutionFailure,
                    ActivityPlayerAdmissionStageStatus.FailedResolution,
                    "Resolver failure did not fail the transaction.");
                completed.Add("resolver-failure-explicit");

                AssertEqual(1, resolutionResolver.RollbackCount,
                    "Resolver failure did not rollback resolver state.");
                completed.Add("resolver-failure-rolls-back-resolver");

                AssertEqual(1, resolutionScope.ReleaseCount,
                    "Resolver failure did not release the staged scope.");
                completed.Add("resolver-failure-releases-scope");

                var readyScope = new FakeScopeRuntime();
                var readyResolver = new FakeResolver();
                context = CreateContext(contextType, gateType, readyScope, readyResolver);
                ActivityPlayerAdmissionStageResult ready = Stage(
                    contextType, context, noPlayers, " qa ", " ready ");
                AssertStatus(ready,
                    ActivityPlayerAdmissionStageStatus.SucceededReadyToCommit,
                    "NoSlots/None stage did not become ReadyToCommit.");
                AssertTrue(ready.CurrentSnapshot.IsReadyToCommit,
                    "Ready stage snapshot does not expose ReadyToCommit.");
                completed.Add("no-slots-none-ready-to-commit");

                AssertTrue(
                    ready.CurrentSnapshot.Source == "qa" &&
                    ready.CurrentSnapshot.Reason == "ready",
                    "Stage source/reason normalization changed unexpectedly.");
                completed.Add("stage-diagnostics-normalized");

                ActivityPlayerAdmissionStageResult duplicate = Stage(
                    contextType, context, noPlayers, "qa", "duplicate-stage");
                AssertStatus(duplicate,
                    ActivityPlayerAdmissionStageStatus.RejectedAnotherStageActive,
                    "Second active stage was not rejected.");
                completed.Add("one-active-stage-enforced");

                ActivityPlayerAdmissionStageToken token = ready.CurrentSnapshot.Token;
                AssertTrue(token.IsValid,
                    "Ready stage did not create a functional token.");
                completed.Add("stage-token-valid-with-no-player-session");

                ActivityPlayerAdmissionStageToken stale = CreateStaleToken(token);
                ActivityPlayerAdmissionStageResult staleCommit = Commit(
                    contextType, context, stale, out _);
                AssertStatus(staleCommit,
                    ActivityPlayerAdmissionStageStatus.RejectedForeignOrStaleStage,
                    "Foreign commit token was accepted.");
                completed.Add("commit-token-guarded");

                ActivityPlayerAdmissionStageResult committed = Commit(
                    contextType, context, token, out object commit);
                AssertStatus(committed,
                    ActivityPlayerAdmissionStageStatus.SucceededCommitted,
                    "Ready stage did not commit.");
                AssertNotNull(commit, "Commit handoff was not returned.");
                completed.Add("commit-handoff-succeeds");

                AssertTrue(!HasActiveStage(contextType, context),
                    "Committed stage remained owned by the staging context.");
                completed.Add("commit-transfers-stage-ownership");

                bool commitRollback = RollbackCommit(
                    commitType, commit, "qa", "handoff-rollback", out string commitIssue);
                AssertTrue(commitRollback && string.IsNullOrEmpty(commitIssue),
                    "Commit handoff rollback failed.");
                completed.Add("commit-handoff-can-rollback");

                AssertEqual(1, readyResolver.RollbackCount,
                    "Commit rollback did not release resolver state.");
                AssertEqual(1, readyScope.ReleaseCount,
                    "Commit rollback did not release scope state.");
                completed.Add("commit-rollback-reverses-resolver-and-scope");

                AssertTrue(RollbackCommit(
                        commitType, commit, "qa", "handoff-rollback-again", out _),
                    "Commit rollback was not idempotent.");
                completed.Add("commit-rollback-idempotent");

                var completedScope = new FakeScopeRuntime();
                var completedResolver = new FakeResolver();
                context = CreateContext(
                    contextType,
                    gateType,
                    completedScope,
                    completedResolver);
                ready = Stage(
                    contextType,
                    context,
                    noPlayers,
                    "qa",
                    "commit-completion");
                committed = Commit(
                    contextType,
                    context,
                    ready.CurrentSnapshot.Token,
                    out object completedCommit);
                AssertStatus(
                    committed,
                    ActivityPlayerAdmissionStageStatus.SucceededCommitted,
                    "Commit completion fixture did not create a handoff.");
                AssertTrue(
                    CompleteCommit(commitType, completedCommit, out string completeIssue) &&
                    string.IsNullOrEmpty(completeIssue),
                    "Committed stage handoff did not complete.");
                completed.Add("commit-completion-succeeds");

                AssertTrue(
                    !RollbackCommit(
                        commitType,
                        completedCommit,
                        "qa",
                        "rollback-after-complete",
                        out string completedRollbackIssue) &&
                    !string.IsNullOrEmpty(completedRollbackIssue),
                    "Completed commit accepted rollback.");
                completed.Add("completed-commit-cannot-rollback");

                AssertTrue(
                    ReleaseCommit(
                        commitType,
                        completedCommit,
                        "qa",
                        "activity-exit-release",
                        out string releaseCommitIssue) &&
                    string.IsNullOrEmpty(releaseCommitIssue),
                    "Completed commit did not release on Activity exit.");
                completed.Add("completed-commit-release-succeeds");

                AssertTrue(
                    completedResolver.RollbackCount == 1 &&
                    completedScope.ReleaseCount == 1,
                    "Completed commit release did not reverse resolver and scope ownership.");
                completed.Add("completed-commit-release-reverses-resolver-and-scope");

                AssertTrue(
                    ReleaseCommit(
                        commitType,
                        completedCommit,
                        "qa",
                        "activity-exit-release-again",
                        out string releaseCommitAgainIssue) &&
                    string.IsNullOrEmpty(releaseCommitAgainIssue) &&
                    completedResolver.RollbackCount == 1 &&
                    completedScope.ReleaseCount == 1,
                    "Completed commit release was not idempotent.");
                completed.Add("completed-commit-release-idempotent");

                var explicitScope = new FakeScopeRuntime();
                var explicitResolver = new FakeResolver();
                context = CreateContext(contextType, gateType, explicitScope, explicitResolver);
                ready = Stage(contextType, context, noPlayers, "qa", "explicit-rollback");
                ActivityPlayerAdmissionStageResult rolledBack = Rollback(
                    contextType,
                    context,
                    ready.CurrentSnapshot.Token,
                    "qa",
                    "explicit-rollback");
                AssertStatus(rolledBack,
                    ActivityPlayerAdmissionStageStatus.SucceededRolledBack,
                    "Explicit rollback did not succeed.");
                completed.Add("explicit-rollback-succeeds");

                AssertTrue(!HasActiveStage(contextType, context),
                    "Successful explicit rollback retained an active stage.");
                completed.Add("successful-rollback-clears-stage");

                ActivityPlayerAdmissionStageResult staleRollback = Rollback(
                    contextType,
                    context,
                    ready.CurrentSnapshot.Token,
                    "qa",
                    "stale-rollback");
                AssertStatus(staleRollback,
                    ActivityPlayerAdmissionStageStatus.RejectedForeignOrStaleStage,
                    "Stale rollback token was not rejected.");
                completed.Add("rollback-token-guarded");

                var rejectScope = new FakeScopeRuntime();
                var rejectResolver = new FakeResolver();
                context = CreateContext(contextType, gateType, rejectScope, rejectResolver);
                ActivityPlayerAdmissionStageResult rejected = Stage(
                    contextType, context, contradictory, "qa", "contradictory");
                AssertStatus(rejected,
                    ActivityPlayerAdmissionStageStatus.SucceededRolledBack,
                    "Rejected P3K.7A decision did not rollback cleanly.");
                AssertTrue(rejected.CurrentSnapshot.Decision.IsRejected,
                    "Rejected stage did not preserve the P3K.7A decision.");
                completed.Add("rejected-decision-auto-rolls-back");

                AssertEqual(1, rejectResolver.RollbackCount,
                    "Rejected decision did not rollback resolver state.");
                AssertEqual(1, rejectScope.ReleaseCount,
                    "Rejected decision did not release staged scope.");
                completed.Add("rejected-decision-releases-all-stage-parts");

                var retryScope = new FakeScopeRuntime();
                var retryResolver = new FakeResolver { FailRollbackCount = 1 };
                context = CreateContext(contextType, gateType, retryScope, retryResolver);
                ready = Stage(contextType, context, noPlayers, "qa", "rollback-retry");
                token = ready.CurrentSnapshot.Token;
                ActivityPlayerAdmissionStageResult rollbackFailed = Rollback(
                    contextType, context, token, "qa", "rollback-fails-once");
                AssertStatus(rollbackFailed,
                    ActivityPlayerAdmissionStageStatus.FailedRollback,
                    "Rollback failure was not retained.");
                AssertTrue(HasActiveStage(contextType, context) &&
                    rollbackFailed.CurrentSnapshot.IsRollbackFailed,
                    "RollbackFailed did not retain retryable stage evidence.");
                completed.Add("rollback-failure-retains-stage");

                ActivityPlayerAdmissionStageResult rollbackRetried = Rollback(
                    contextType, context, token, "qa", "rollback-retry");
                AssertStatus(rollbackRetried,
                    ActivityPlayerAdmissionStageStatus.SucceededRolledBack,
                    "Rollback retry did not succeed.");
                AssertTrue(!HasActiveStage(contextType, context),
                    "Rollback retry did not clear the stage.");
                completed.Add("rollback-retry-succeeds");

                var scopeRetry = new FakeScopeRuntime { FailReleaseCount = 1 };
                var scopeRetryResolver = new FakeResolver();
                context = CreateContext(
                    contextType,
                    gateType,
                    scopeRetry,
                    scopeRetryResolver);
                ready = Stage(
                    contextType,
                    context,
                    noPlayers,
                    "qa",
                    "scope-release-retry");
                token = ready.CurrentSnapshot.Token;
                ActivityPlayerAdmissionStageResult scopeRollbackFailed = Rollback(
                    contextType,
                    context,
                    token,
                    "qa",
                    "scope-release-fails-once");
                AssertStatus(
                    scopeRollbackFailed,
                    ActivityPlayerAdmissionStageStatus.FailedRollback,
                    "Scope release failure was not retained.");
                AssertTrue(
                    HasActiveStage(contextType, context) &&
                    scopeRollbackFailed.CurrentSnapshot.ResolverRolledBack &&
                    !scopeRollbackFailed.CurrentSnapshot.ScopeReleased,
                    "Scope release failure did not preserve completed resolver rollback evidence.");
                completed.Add("scope-release-failure-retains-stage");

                ActivityPlayerAdmissionStageResult scopeRollbackRetried = Rollback(
                    contextType,
                    context,
                    token,
                    "qa",
                    "scope-release-retry");
                AssertStatus(
                    scopeRollbackRetried,
                    ActivityPlayerAdmissionStageStatus.SucceededRolledBack,
                    "Scope release retry did not succeed.");
                AssertTrue(
                    !HasActiveStage(contextType, context) &&
                    scopeRetry.ReleaseCount == 2,
                    "Scope release retry did not clear the staged scope.");
                completed.Add("scope-release-retry-succeeds");

                var sequenceScope = new FakeScopeRuntime();
                var sequenceResolver = new FakeResolver();
                context = CreateContext(contextType, gateType, sequenceScope, sequenceResolver);
                ActivityPlayerAdmissionStageResult first = Stage(
                    contextType, context, noPlayers, "qa", "sequence-one");
                Rollback(contextType, context, first.CurrentSnapshot.Token, "qa", "sequence-one-release");
                ActivityPlayerAdmissionStageResult second = Stage(
                    contextType, context, noPlayers, "qa", "sequence-two");
                AssertTrue(second.CurrentSnapshot.Token.StageSequence >
                    first.CurrentSnapshot.Token.StageSequence,
                    "Stage sequence is not monotonic.");
                Rollback(contextType, context, second.CurrentSnapshot.Token, "qa", "sequence-two-release");
                completed.Add("stage-sequence-monotonic");

                const string SelectionSession = "qa.p3k7b.selection.session";
                var realEndpoint = new FakeGameplayEndpointSource(
                    SelectionSession,
                    selectionSlot,
                    selectionActor);
                IActivityPlayerAdmissionStageResolver realResolver =
                    CreateRealChainResolver(
                        assembly,
                        chainResolverType,
                        realEndpoint);
                var realScope = new FakeScopeRuntime();
                context = CreateContext(
                    contextType,
                    gateType,
                    realScope,
                    realResolver);
                ActivityPlayerAdmissionStageResult realSelectionStage = Stage(
                    contextType,
                    context,
                    selectionActivity,
                    "qa",
                    "real-selection-stage");
                AssertStatus(
                    realSelectionStage,
                    ActivityPlayerAdmissionStageStatus.SucceededReadyToCommit,
                    "Real staged resolver did not resolve SelectedActors.");
                completed.Add("real-resolver-selection-stage-ready");

                AssertEqual(
                    1,
                    realEndpoint.SelectionCreateCount,
                    "Real staged resolver did not execute explicit Actor selection.");
                completed.Add("real-resolver-selects-actor");

                AssertTrue(
                    realSelectionStage.CurrentSnapshot.Decision != null &&
                    realSelectionStage.CurrentSnapshot.Decision.CanProceed &&
                    realSelectionStage.CurrentSnapshot.Decision.Evaluation
                        .Slots[0].SelectedActor,
                    "Real staged resolver did not re-evaluate the refreshed selection snapshot.");
                completed.Add("real-resolver-reevaluates-selection");

                ActivityPlayerAdmissionStageResult realSelectionRollback = Rollback(
                    contextType,
                    context,
                    realSelectionStage.CurrentSnapshot.Token,
                    "qa",
                    "real-selection-rollback");
                AssertStatus(
                    realSelectionRollback,
                    ActivityPlayerAdmissionStageStatus.SucceededRolledBack,
                    "Real staged selection rollback failed.");
                AssertTrue(
                    realEndpoint.SelectionReleaseCount == 1 &&
                    !realEndpoint.HasSelectedActor,
                    "Real staged resolver did not release stage-created Actor selection.");
                completed.Add("real-resolver-rolls-back-selection");

                ValidateNoUnityReferences(typeof(ActivityPlayerAdmissionStageToken));
                ValidateNoUnityReferences(typeof(ActivityPlayerAdmissionStageSnapshot));
                ValidateNoUnityReferences(typeof(ActivityPlayerAdmissionStageResult));
                completed.Add("public-stage-results-no-unity-references");

                AssertNotNull(chainResolverType.GetMethod("Resolve", InstanceAny),
                    "Real P3J/P3K chain resolver has no Resolve operation.");
                AssertNotNull(chainResolverType.GetMethod("TryRollback", InstanceAny),
                    "Real P3J/P3K chain resolver has no TryRollback operation.");
                completed.Add("real-p3j-p3k-resolver-surface-valid");

                AssertEqual(38, completed.Count,
                    "P3K.7B smoke case count changed.");

                Debug.Log(
                    $"[P3K7B_STAGED_ACTIVITY_ADMISSION_TRANSACTION_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception root = exception is TargetInvocationException invocation &&
                    invocation.InnerException != null
                        ? invocation.InnerException
                        : exception;
                Debug.LogError(
                    $"[P3K7B_STAGED_ACTIVITY_ADMISSION_TRANSACTION_SMOKE] " +
                    $"status='Failed' exception='{root.GetType().Name}' " +
                    $"message='{Escape(root.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                Debug.LogException(root);
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private sealed class FakeScopeRuntime : IActivityPlayerAdmissionStageScopeRuntime
        {
            public bool FailCreate;
            public int FailReleaseCount;
            public int CreateCount;
            public int ReleaseCount;

            public bool TryCreate(
                ActivityAsset activity,
                int stageSequence,
                string source,
                string reason,
                out ActivityPlayerAdmissionStageScope scope,
                out string issue)
            {
                CreateCount++;
                if (FailCreate)
                {
                    scope = null;
                    issue = "Synthetic staged scope creation failure.";
                    return false;
                }

                RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                    $"qa.p3k7b.stage.{stageSequence}",
                    $"P3K.7B Stage {stageSequence}");
                scope = new ActivityPlayerAdmissionStageScope(
                    activity,
                    owner,
                    $"qa.p3k7b.stage.{stageSequence}");
                issue = string.Empty;
                return true;
            }

            public bool TryRelease(
                ActivityPlayerAdmissionStageScope scope,
                string source,
                string reason,
                out string issue)
            {
                ReleaseCount++;
                if (FailReleaseCount > 0)
                {
                    FailReleaseCount--;
                    issue = "Synthetic staged scope release failure.";
                    return false;
                }

                issue = string.Empty;
                return true;
            }
        }

        private sealed class FakeResolver : IActivityPlayerAdmissionStageResolver
        {
            public bool FailResolve;
            public int FailRollbackCount;
            public int ResolveCount;
            public int RollbackCount;

            public ActivityPlayerAdmissionStageResolution Resolve(
                ActivityAsset activity,
                ActivityPlayerAdmissionStageScope stagedScope,
                string source,
                string reason)
            {
                ResolveCount++;
                var state = new object();
                return FailResolve
                    ? ActivityPlayerAdmissionStageResolution.Failed(
                        null,
                        null,
                        null,
                        state,
                        "Synthetic stage resolution failure.")
                    : new ActivityPlayerAdmissionStageResolution(
                        true,
                        null,
                        null,
                        null,
                        state,
                        "Synthetic stage resolution succeeded.");
            }

            public bool TryRollback(
                ActivityPlayerAdmissionStageResolution resolution,
                string source,
                string reason,
                out string issue)
            {
                RollbackCount++;
                if (FailRollbackCount > 0)
                {
                    FailRollbackCount--;
                    issue = "Synthetic resolver rollback failure.";
                    return false;
                }

                issue = string.Empty;
                return true;
            }
        }

        private sealed class FakeGameplayEndpointSource :
            IActivityPlayerGameplayStageEndpointSource
        {
            private readonly string sessionId;
            private readonly PlayerSlotProfile slotProfile;
            private readonly ActorProfile actorProfile;
            private readonly PlayerParticipationSnapshot withoutSelection;
            private readonly PlayerParticipationSnapshot withSelection;
            private readonly PlayerActorPreparationSnapshot preparation;
            private PlayerParticipationSnapshot current;

            internal FakeGameplayEndpointSource(
                string sessionId,
                PlayerSlotProfile slotProfile,
                ActorProfile actorProfile)
            {
                this.sessionId = sessionId;
                this.slotProfile = slotProfile;
                this.actorProfile = actorProfile;
                withoutSelection = QaP3K7BStagedActivityAdmissionSmoke.CreateParticipationSnapshot(
                    sessionId,
                    slotProfile,
                    null,
                    revision: 1,
                    selectionRevision: 0);
                withSelection = QaP3K7BStagedActivityAdmissionSmoke.CreateParticipationSnapshot(
                    sessionId,
                    slotProfile,
                    actorProfile,
                    revision: 2,
                    selectionRevision: 1);
                preparation = CreateUnpreparedPreparationSnapshot(
                    sessionId,
                    slotProfile.PlayerSlotId);
                current = withoutSelection;
            }

            internal int SelectionCreateCount { get; private set; }
            internal int SelectionReleaseCount { get; private set; }
            internal bool HasSelectedActor => current.SelectedActorCount == 1;

            public PlayerParticipationSnapshot CreateParticipationSnapshot() =>
                current;

            public PlayerActorPreparationSnapshot CreatePreparationSnapshot() =>
                preparation;

            public bool TryEnsureSelectedActor(
                ActivityAsset activity,
                PlayerSlotId playerSlotId,
                string source,
                string reason,
                out PlayerSlotRuntimeSnapshot selection,
                out bool createdByStage,
                out string issue)
            {
                if (activity == null || playerSlotId != slotProfile.PlayerSlotId)
                {
                    selection = default;
                    createdByStage = false;
                    issue = "Synthetic selection endpoint received invalid Activity or Slot evidence.";
                    return false;
                }

                createdByStage = !HasSelectedActor;
                if (createdByStage)
                {
                    SelectionCreateCount++;
                    current = withSelection;
                }

                selection = current.Slots[0];
                issue = string.Empty;
                return true;
            }

            public bool TryReleaseSelectedActor(
                PlayerSlotRuntimeSnapshot selection,
                string source,
                string reason,
                out string issue)
            {
                if (!selection.IsValid ||
                    selection.PlayerSlotId != slotProfile.PlayerSlotId)
                {
                    issue = "Synthetic selection rollback received invalid Slot evidence.";
                    return false;
                }

                if (HasSelectedActor)
                {
                    SelectionReleaseCount++;
                    current = withoutSelection;
                }

                issue = string.Empty;
                return true;
            }

            public bool TryEnsurePrepared(
                ActivityAsset activity,
                ActivityPlayerAdmissionStageScope stagedScope,
                PlayerSlotId playerSlotId,
                string source,
                string reason,
                out PlayerActorPreparationSummary currentPreparation,
                out bool createdByStage,
                out string issue)
            {
                currentPreparation = default;
                createdByStage = false;
                issue = "Synthetic SelectedActors fixture must not request preparation.";
                return false;
            }

            public bool TryResolveGameplayEndpoints(
                PlayerActorPreparationSummary currentPreparation,
                out LocalPlayerHostAuthoring host,
                out PlayerActorDeclaration actorDeclaration,
                out UnityPlayerInputGateAdapter gateAdapter,
                out PlayerGameplayCameraAuthoring cameraAuthoring,
                out PlayerGameplayCameraRequiredness cameraRequiredness,
                out CameraOutputSessionBinding outputSession,
                out string issue)
            {
                host = null;
                actorDeclaration = null;
                gateAdapter = null;
                cameraAuthoring = null;
                cameraRequiredness = PlayerGameplayCameraRequiredness.Optional;
                outputSession = null;
                issue = "Synthetic SelectedActors fixture must not request gameplay endpoints.";
                return false;
            }

            public bool TryReleasePreparation(
                PlayerActorPreparationSummary currentPreparation,
                string source,
                string reason,
                out string issue)
            {
                issue = string.Empty;
                return true;
            }
        }

        private static IActivityPlayerAdmissionStageResolver CreateRealChainResolver(
            Assembly assembly,
            Type chainResolverType,
            IActivityPlayerGameplayStageEndpointSource endpointSource)
        {
            PlayerActorPreparationSnapshot preparation =
                endpointSource.CreatePreparationSnapshot();

            Type occupancyType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerGameplayOccupancyRuntimeContext",
                true);
            Type inputType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerGameplayInputBindingRuntimeContext",
                true);
            Type cameraType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerGameplayCameraEligibilityRuntimeContext",
                true);
            Type admissionType = assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerGameplayAdmissionRuntimeContext",
                true);

            object occupancy = CreateAuthority(
                occupancyType,
                preparation);
            object input = CreateAuthority(
                inputType,
                occupancy);
            object camera = CreateAuthority(
                cameraType,
                occupancy,
                input);
            object admission = CreateAuthority(
                admissionType,
                occupancy,
                input,
                camera);

            ConstructorInfo constructor = chainResolverType.GetConstructor(
                InstanceAny,
                null,
                new[]
                {
                    typeof(IActivityPlayerGameplayStageEndpointSource),
                    occupancyType,
                    inputType,
                    cameraType,
                    admissionType
                },
                null);
            AssertNotNull(
                constructor,
                "P3K.7B real chain resolver constructor changed.");

            object resolver = constructor.Invoke(
                new[]
                {
                    endpointSource,
                    occupancy,
                    input,
                    camera,
                    admission
                });
            AssertTrue(
                resolver is IActivityPlayerAdmissionStageResolver,
                "P3K.7B real chain resolver does not implement the stage resolver contract.");
            return (IActivityPlayerAdmissionStageResolver)resolver;
        }

        private static object CreateAuthority(
            Type authorityType,
            params object[] inputs)
        {
            MethodInfo factory = null;
            MethodInfo[] methods = authorityType.GetMethods(StaticAny);
            for (int index = 0; index < methods.Length; index++)
            {
                if (methods[index].Name == "TryCreate" &&
                    methods[index].GetParameters().Length == inputs.Length + 2)
                {
                    factory = methods[index];
                    break;
                }
            }

            AssertNotNull(
                factory,
                $"Authority '{authorityType.Name}' has no matching TryCreate factory.");

            var args = new object[inputs.Length + 2];
            for (int index = 0; index < inputs.Length; index++)
            {
                args[index] = inputs[index];
            }

            args[inputs.Length] = null;
            args[inputs.Length + 1] = null;
            bool succeeded = (bool)factory.Invoke(null, args);
            string issue = args[inputs.Length + 1] as string ?? string.Empty;
            AssertTrue(
                succeeded && args[inputs.Length] != null,
                $"Authority '{authorityType.Name}' creation failed. {issue}");
            return args[inputs.Length];
        }

        private static object CreateContext(
            Type contextType,
            Type gateType,
            IActivityPlayerAdmissionStageScopeRuntime scopeRuntime,
            IActivityPlayerAdmissionStageResolver resolver)
        {
            ConstructorInfo constructor = contextType.GetConstructor(
                InstanceAny,
                null,
                new[]
                {
                    typeof(IActivityPlayerAdmissionStageScopeRuntime),
                    typeof(IActivityPlayerAdmissionStageResolver),
                    gateType
                },
                null);
            AssertNotNull(constructor,
                "P3K.7B context constructor changed.");
            return constructor.Invoke(new object[] { scopeRuntime, resolver, null });
        }

        private static ActivityPlayerAdmissionStageResult Stage(
            Type contextType,
            object context,
            ActivityAsset activity,
            string source,
            string reason)
        {
            MethodInfo method = contextType.GetMethod("TryStage", InstanceAny);
            AssertNotNull(method, "P3K.7B TryStage is missing.");
            return (ActivityPlayerAdmissionStageResult)method.Invoke(
                context,
                new object[] { activity, source, reason });
        }

        private static ActivityPlayerAdmissionStageResult Commit(
            Type contextType,
            object context,
            ActivityPlayerAdmissionStageToken token,
            out object commit)
        {
            MethodInfo method = contextType.GetMethod("TryCommit", InstanceAny);
            AssertNotNull(method, "P3K.7B TryCommit is missing.");
            object[] args = { token, null };
            var result = (ActivityPlayerAdmissionStageResult)method.Invoke(context, args);
            commit = args[1];
            return result;
        }

        private static ActivityPlayerAdmissionStageResult Rollback(
            Type contextType,
            object context,
            ActivityPlayerAdmissionStageToken token,
            string source,
            string reason)
        {
            MethodInfo method = contextType.GetMethod("TryRollback", InstanceAny);
            AssertNotNull(method, "P3K.7B TryRollback is missing.");
            return (ActivityPlayerAdmissionStageResult)method.Invoke(
                context,
                new object[] { token, source, reason });
        }

        private static bool CompleteCommit(
            Type commitType,
            object commit,
            out string issue)
        {
            MethodInfo method = commitType.GetMethod("TryComplete", InstanceAny);
            AssertNotNull(method, "P3K.7B commit TryComplete is missing.");
            object[] args = { null };
            bool result = (bool)method.Invoke(commit, args);
            issue = args[0] as string ?? string.Empty;
            return result;
        }

        private static bool ReleaseCommit(
            Type commitType,
            object commit,
            string source,
            string reason,
            out string issue)
        {
            MethodInfo method = commitType.GetMethod("TryRelease", InstanceAny);
            AssertNotNull(method, "P3K.7B commit TryRelease is missing.");
            object[] args = { source, reason, null };
            bool result = (bool)method.Invoke(commit, args);
            issue = args[2] as string ?? string.Empty;
            return result;
        }

        private static bool RollbackCommit(
            Type commitType,
            object commit,
            string source,
            string reason,
            out string issue)
        {
            MethodInfo method = commitType.GetMethod("TryRollback", InstanceAny);
            AssertNotNull(method, "P3K.7B commit TryRollback is missing.");
            object[] args = { source, reason, null };
            bool result = (bool)method.Invoke(commit, args);
            issue = args[2] as string ?? string.Empty;
            return result;
        }

        private static bool HasActiveStage(Type contextType, object context)
        {
            PropertyInfo property = contextType.GetProperty("HasActiveStage", InstanceAny);
            AssertNotNull(property, "P3K.7B HasActiveStage is missing.");
            return (bool)property.GetValue(context);
        }

        private static ActivityPlayerAdmissionStageToken CreateStaleToken(
            ActivityPlayerAdmissionStageToken current)
        {
            ConstructorInfo constructor = typeof(ActivityPlayerAdmissionStageToken)
                .GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(RuntimeContentOwner),
                        typeof(int)
                    },
                    null);
            AssertNotNull(constructor, "P3K.7B stage token constructor changed.");
            return (ActivityPlayerAdmissionStageToken)constructor.Invoke(
                new object[]
                {
                    current.SessionContextId,
                    current.Owner,
                    current.StageSequence + 100
                });
        }

        private static void ValidateContractSurface(
            Type contextType,
            Type commitType,
            Type chainResolverType)
        {
            AssertTrue(typeof(IActivityPlayerAdmissionStageScopeRuntime).IsInterface,
                "P3K.7B scope runtime contract is not an interface.");
            AssertTrue(typeof(IActivityPlayerAdmissionStageResolver).IsInterface,
                "P3K.7B resolver contract is not an interface.");
            AssertNotNull(contextType.GetMethod("TryStage", InstanceAny),
                "P3K.7B TryStage is missing.");
            AssertNotNull(contextType.GetMethod("TryCommit", InstanceAny),
                "P3K.7B TryCommit is missing.");
            AssertNotNull(contextType.GetMethod("TryRollback", InstanceAny),
                "P3K.7B TryRollback is missing.");
            AssertNotNull(contextType.GetMethod("CreateSnapshot", InstanceAny),
                "P3K.7B CreateSnapshot is missing.");
            AssertNotNull(commitType.GetMethod("TryComplete", InstanceAny),
                "P3K.7B commit TryComplete is missing.");
            AssertNotNull(commitType.GetMethod("TryRelease", InstanceAny),
                "P3K.7B commit TryRelease is missing.");
            AssertNotNull(chainResolverType,
                "P3K.7B chain resolver is missing.");

            Type endpointSourceType = contextType.Assembly.GetType(
                "Immersive.Framework.PlayerParticipation.IActivityPlayerGameplayStageEndpointSource",
                false);
            AssertNotNull(endpointSourceType,
                "P3K.7B real gameplay endpoint source contract is missing.");
            AssertNotNull(endpointSourceType.GetMethod("TryEnsureSelectedActor", InstanceAny),
                "P3K.7B endpoint source has no staged Actor selection operation.");
            AssertNotNull(endpointSourceType.GetMethod("TryReleaseSelectedActor", InstanceAny),
                "P3K.7B endpoint source has no staged Actor selection rollback operation.");
        }

        private static PlayerParticipationRequirementsProfile CreateRequirements(
            PlayerParticipationRequirementLevel level,
            string suffix,
            List<UnityEngine.Object> created)
        {
            var profile = ScriptableObject.CreateInstance<PlayerParticipationRequirementsProfile>();
            profile.name = $"QA P3K.7B Requirements {suffix}";
            SetSerialized(profile, "displayName", profile.name);
            SetSerialized(profile, "requirementLevel", (int)level);
            created.Add(profile);
            return profile;
        }

        private static ActivityParticipationProjectionProfile CreateNoSlotsProjection(
            List<UnityEngine.Object> created)
        {
            var profile = ScriptableObject.CreateInstance<ActivityParticipationProjectionProfile>();
            profile.name = "QA P3K.7B No Slots";
            SetSerialized(profile, "displayName", profile.name);
            SetSerialized(profile, "projectionMode", (int)ActivityParticipationProjectionMode.NoSlots);
            SetSerialized(profile, "zeroParticipantPolicy", (int)ActivityParticipationZeroParticipantPolicy.Allowed);
            SetSerializedArraySize(profile, "explicitSlotProfiles", 0);
            created.Add(profile);
            return profile;
        }

        private static ActivityParticipationProjectionProfile CreateAllJoinedProjection(
            List<UnityEngine.Object> created)
        {
            var profile = ScriptableObject.CreateInstance<ActivityParticipationProjectionProfile>();
            profile.name = "QA P3K.7B All Joined";
            SetSerialized(profile, "displayName", profile.name);
            SetSerialized(
                profile,
                "projectionMode",
                (int)ActivityParticipationProjectionMode.AllJoinedSlots);
            SetSerialized(
                profile,
                "zeroParticipantPolicy",
                (int)ActivityParticipationZeroParticipantPolicy.Rejected);
            SetSerializedArraySize(profile, "explicitSlotProfiles", 0);
            created.Add(profile);
            return profile;
        }

        private static PlayerSlotProfile CreateSlotProfile(
            string id,
            string displayName,
            List<UnityEngine.Object> created)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            SetSerialized(profile, "playerSlotId", id);
            SetSerialized(profile, "displayName", displayName);
            created.Add(profile);
            return profile;
        }

        private static ActorProfile CreateActorProfile(
            string id,
            string displayName,
            List<UnityEngine.Object> created)
        {
            var profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = displayName;
            SetSerialized(profile, "actorProfileId", id);
            SetSerialized(profile, "displayName", displayName);
            created.Add(profile);
            return profile;
        }

        private static ActivityAsset CreateActivity(
            ActivityParticipationProjectionProfile projection,
            PlayerParticipationRequirementsProfile requirements,
            string suffix,
            List<UnityEngine.Object> created)
        {
            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = $"QA P3K.7B Activity {suffix}";
            SetSerialized(activity, "activityName", activity.name);
            SetSerialized(activity, "playerParticipationProjectionProfile", projection);
            SetSerialized(activity, "playerParticipationRequirementsProfile", requirements);
            created.Add(activity);
            return activity;
        }

        private static PlayerParticipationSnapshot CreateParticipationSnapshot(
            string sessionId,
            PlayerSlotProfile profile,
            ActorProfile selectedActor,
            int revision,
            int selectionRevision)
        {
            ConstructorInfo slotConstructor = typeof(PlayerSlotRuntimeSnapshot)
                .GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(int),
                        typeof(PlayerSlotProfile),
                        typeof(PlayerSlotId),
                        typeof(PlayerSlotAllocationState),
                        typeof(PlayerSlotReservationToken),
                        typeof(int),
                        typeof(string),
                        typeof(string),
                        typeof(ActorProfile),
                        typeof(int),
                        typeof(string),
                        typeof(string)
                    },
                    null);
            AssertNotNull(
                slotConstructor,
                "PlayerSlotRuntimeSnapshot constructor changed.");

            var slot = (PlayerSlotRuntimeSnapshot)slotConstructor.Invoke(
                new object[]
                {
                    0,
                    profile,
                    profile.PlayerSlotId,
                    PlayerSlotAllocationState.Joined,
                    default(PlayerSlotReservationToken),
                    revision,
                    nameof(QaP3K7BStagedActivityAdmissionSmoke),
                    "real-resolver-selection-fixture",
                    selectedActor,
                    selectionRevision,
                    nameof(QaP3K7BStagedActivityAdmissionSmoke),
                    "real-resolver-selection-fixture"
                });

            ConstructorInfo snapshotConstructor = typeof(PlayerParticipationSnapshot)
                .GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(int),
                        typeof(bool),
                        typeof(int),
                        typeof(bool),
                        typeof(PlayerActorSelectionPolicyProfile),
                        typeof(PlayerSlotRuntimeSnapshot[]),
                        typeof(PlayerParticipationOperationStatus),
                        typeof(string)
                    },
                    null);
            AssertNotNull(
                snapshotConstructor,
                "PlayerParticipationSnapshot constructor changed.");
            return (PlayerParticipationSnapshot)snapshotConstructor.Invoke(
                new object[]
                {
                    sessionId,
                    revision,
                    true,
                    1,
                    false,
                    null,
                    new[] { slot },
                    PlayerParticipationOperationStatus.Succeeded,
                    "P3K.7B real resolver participation fixture."
                });
        }

        private static PlayerActorPreparationSnapshot
            CreateUnpreparedPreparationSnapshot(
                string sessionId,
                PlayerSlotId playerSlotId)
        {
            ConstructorInfo summaryConstructor =
                typeof(PlayerActorPreparationSummary).GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(PlayerSlotId),
                        typeof(PlayerActorPreparationState),
                        typeof(ActorProfileId),
                        typeof(int),
                        typeof(PlayerActorMaterializationSnapshot),
                        typeof(string),
                        typeof(string),
                        typeof(string)
                    },
                    null);
            AssertNotNull(
                summaryConstructor,
                "PlayerActorPreparationSummary constructor changed.");

            var summary = (PlayerActorPreparationSummary)summaryConstructor.Invoke(
                new object[]
                {
                    sessionId,
                    playerSlotId,
                    PlayerActorPreparationState.Unprepared,
                    default(ActorProfileId),
                    0,
                    default(PlayerActorMaterializationSnapshot),
                    nameof(QaP3K7BStagedActivityAdmissionSmoke),
                    "real-resolver-selection-fixture",
                    "No Logical Actor is prepared."
                });

            ConstructorInfo snapshotConstructor =
                typeof(PlayerActorPreparationSnapshot).GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(int),
                        typeof(PlayerActorPreparationSummary[]),
                        typeof(PlayerActorMaterializationSnapshot[]),
                        typeof(PlayerActorPreparationStatus),
                        typeof(string)
                    },
                    null);
            AssertNotNull(
                snapshotConstructor,
                "PlayerActorPreparationSnapshot constructor changed.");

            return (PlayerActorPreparationSnapshot)snapshotConstructor.Invoke(
                new object[]
                {
                    sessionId,
                    1,
                    new[] { summary },
                    Array.Empty<PlayerActorMaterializationSnapshot>(),
                    default(PlayerActorPreparationStatus),
                    "P3K.7B real resolver preparation fixture."
                });
        }

        private static void SetSerialized(UnityEngine.Object target, string name, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            AssertNotNull(property, $"Serialized property '{name}' is missing on '{target.GetType().Name}'.");
            switch (value)
            {
                case int integer:
                    property.intValue = integer;
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                case UnityEngine.Object unityObject:
                    property.objectReferenceValue = unityObject;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"Unsupported serialized value type '{value?.GetType().Name}'.");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedArraySize(
            UnityEngine.Object target,
            string name,
            int size)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            AssertNotNull(property, $"Serialized array '{name}' is missing.");
            property.arraySize = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ValidateNoUnityReferences(Type type)
        {
            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public))
            {
                AssertTrue(!typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType),
                    $"Public contract '{type.Name}.{property.Name}' retains Unity object type '{property.PropertyType.Name}'.");
            }
        }

        private static void AssertStatus(
            ActivityPlayerAdmissionStageResult result,
            ActivityPlayerAdmissionStageStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            AssertEqual(expected, result.Status,
                message + " " + result.ToDiagnosticString());
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
            AssertTrue(value != null, message);
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
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}

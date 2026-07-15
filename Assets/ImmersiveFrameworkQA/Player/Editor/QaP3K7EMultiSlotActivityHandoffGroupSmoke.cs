using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3K7EMultiSlotActivityHandoffGroupSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.7E Run Multi-Slot Activity Handoff Group Smoke";
        private const string ContextTypeName =
            "Immersive.Framework.PlayerParticipation.ActivityPlayerHandoffGroupRuntimeContext";
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
                AssertTrue(Application.isPlaying, "P3K.7E smoke must run in Play Mode.");
                completed.Add("play-mode-required");

                Type contextType = typeof(ActivityPlayerHandoffGroupResult).Assembly
                    .GetType(ContextTypeName, false);
                AssertNotNull(contextType, "P3K.7E group runtime context is missing.");
                AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(contextType),
                    "P3K.7E group authority must remain plain C#.");
                completed.Add("group-context-plain-csharp");

                ValidateSurface(contextType);
                completed.Add("two-phase-group-surface-valid");
                AssertTrue(typeof(IPlayerGameplayChainPromotionRuntime).IsInterface,
                    "P3K.7E per-Slot promotion contract is not an interface.");
                completed.Add("per-slot-promotion-contract-public");

                Fixture fixture = Fixture.Create(created);
                completed.Add("two-slot-gameplayready-fixture-created");

                var invalidCreateArgs = new object[] { null, fixture.Evidence, null, null };
                bool invalidCreate = (bool)GetMethod(contextType, "TryCreate", StaticAny)
                    .Invoke(null, invalidCreateArgs);
                AssertTrue(!invalidCreate && invalidCreateArgs[2] == null,
                    "P3K.7E context accepted a missing promotion authority.");
                completed.Add("missing-authority-rejected");

                var promotion = new FakePromotionRuntime(fixture.PreparationTokens);
                object context = CreateContext(contextType, promotion, fixture.Evidence);
                completed.Add("group-context-created");

                ActivityPlayerHandoffGroupResult invalidActivity = Begin(
                    contextType, context, null, fixture.TargetOwner, fixture.Requests,
                    "qa", "invalid-activity");
                AssertStatus(invalidActivity,
                    ActivityPlayerHandoffGroupStatus.RejectedInvalidRequest,
                    "Missing Activity was not rejected.");
                completed.Add("missing-activity-rejected");

                ActivityPlayerHandoffGroupResult invalidOwner = Begin(
                    contextType, context, fixture.Activity, fixture.CurrentOwner,
                    fixture.Requests, "qa", "invalid-owner");
                AssertStatus(invalidOwner,
                    ActivityPlayerHandoffGroupStatus.RejectedInvalidRequest,
                    "Non-target owner request was not rejected.");
                completed.Add("foreign-target-owner-rejected");

                var duplicate = new[] { fixture.Requests[0], fixture.Requests[0] };
                ActivityPlayerHandoffGroupResult duplicateResult = Begin(
                    contextType, context, fixture.Activity, fixture.TargetOwner,
                    duplicate, "qa", "duplicate-slot");
                AssertStatus(duplicateResult,
                    ActivityPlayerHandoffGroupStatus.RejectedInvalidRequest,
                    "Duplicate Slot group was not rejected.");
                completed.Add("duplicate-slot-rejected");

                ActivityPlayerHandoffGroupResult ready = Begin(
                    contextType, context, fixture.Activity, fixture.TargetOwner,
                    fixture.Requests, " qa ", " begin-two-slot ");
                AssertStatus(ready,
                    ActivityPlayerHandoffGroupStatus.SucceededReadyToCommit,
                    "Two-Slot group did not become ReadyToCommit.");
                completed.Add("two-slot-group-ready-to-commit");
                AssertTrue(ready.CurrentSnapshot.AdmissionDecision?.CanProceed == true,
                    "Ready group does not retain P3K.6/P3K.7A Proceed evidence.");
                completed.Add("group-admission-proceed-retained");
                AssertEqual(2, ready.CurrentSnapshot.SlotCount,
                    "Ready group lost ordered Slot evidence.");
                completed.Add("ordered-slot-evidence-retained");
                AssertSequence(promotion.Calls, "begin:player.p3k7e.1", "begin:player.p3k7e.2");
                completed.Add("slot-begin-order-deterministic");
                AssertTrue(ready.CurrentSnapshot.Source == "qa" &&
                    ready.CurrentSnapshot.Reason == "begin-two-slot",
                    "Group source/reason normalization changed.");
                completed.Add("group-diagnostics-normalized");
                AssertTrue(ready.CurrentSnapshot.Token.IsValid,
                    "Ready group has no exact token.");
                completed.Add("group-token-valid");

                ActivityPlayerHandoffGroupResult alreadyReady = Begin(
                    contextType, context, fixture.Activity, fixture.TargetOwner,
                    fixture.Requests, "qa", "repeat-ready");
                AssertStatus(alreadyReady,
                    ActivityPlayerHandoffGroupStatus.SucceededAlreadyReadyToCommit,
                    "Repeated same group was not idempotent.");
                completed.Add("ready-group-idempotent");

                ActivityPlayerHandoffGroupToken stale = CreateStaleGroupToken(
                    ready.CurrentSnapshot.Token);
                ActivityPlayerHandoffGroupResult staleCommit = Commit(
                    contextType, context, stale, "qa", "stale-commit");
                AssertStatus(staleCommit,
                    ActivityPlayerHandoffGroupStatus.RejectedForeignOrStaleGroup,
                    "Foreign group commit token was accepted.");
                completed.Add("group-commit-token-guarded");

                ActivityPlayerHandoffGroupResult rollback = Rollback(
                    contextType, context, ready.CurrentSnapshot.Token,
                    "qa", "explicit-rollback");
                AssertStatus(rollback,
                    ActivityPlayerHandoffGroupStatus.SucceededRolledBack,
                    "Explicit group rollback failed.");
                completed.Add("explicit-group-rollback-succeeds");
                AssertTailSequence(promotion.Calls,
                    "rollback:player.p3k7e.2", "rollback:player.p3k7e.1");
                completed.Add("group-rollback-reverse-order");
                AssertTrue(!HasActive(contextType, context),
                    "Successful rollback retained an active group.");
                completed.Add("rollback-clears-active-group");

                promotion = new FakePromotionRuntime(fixture.PreparationTokens)
                {
                    FailBeginSlot = fixture.SlotTwo
                };
                context = CreateContext(contextType, promotion, fixture.Evidence);
                ActivityPlayerHandoffGroupResult beginFailure = Begin(
                    contextType, context, fixture.Activity, fixture.TargetOwner,
                    fixture.Requests, "qa", "slot-two-begin-failure");
                AssertStatus(beginFailure,
                    ActivityPlayerHandoffGroupStatus.FailedSlotBegin,
                    "Second Slot begin failure did not fail the group.");
                completed.Add("second-slot-begin-failure-explicit");
                AssertSequence(promotion.Calls,
                    "begin:player.p3k7e.1", "begin:player.p3k7e.2",
                    "rollback:player.p3k7e.1");
                completed.Add("partial-begin-rolls-back-first-slot");
                AssertTrue(!HasActive(contextType, context),
                    "Clean begin failure retained an active group.");
                completed.Add("begin-failure-clears-group");

                promotion = new FakePromotionRuntime(fixture.PreparationTokens);
                fixture.Evidence.UseBlockedAdmission = true;
                context = CreateContext(contextType, promotion, fixture.Evidence);
                ActivityPlayerHandoffGroupResult rejectedAdmission = Begin(
                    contextType, context, fixture.Activity, fixture.TargetOwner,
                    fixture.Requests, "qa", "admission-rejected");
                AssertStatus(rejectedAdmission,
                    ActivityPlayerHandoffGroupStatus.RejectedAdmission,
                    "Non-Proceed P3K.6 decision did not reject the group.");
                completed.Add("non-proceed-admission-rejects-group");
                AssertTailSequence(promotion.Calls,
                    "rollback:player.p3k7e.2", "rollback:player.p3k7e.1");
                completed.Add("admission-rejection-rolls-back-all");
                fixture.Evidence.UseBlockedAdmission = false;

                promotion = new FakePromotionRuntime(fixture.PreparationTokens);
                fixture.Evidence.FailCapture = true;
                context = CreateContext(contextType, promotion, fixture.Evidence);
                ActivityPlayerHandoffGroupResult evidenceFailure = Begin(
                    contextType, context, fixture.Activity, fixture.TargetOwner,
                    fixture.Requests, "qa", "evidence-failure");
                AssertStatus(evidenceFailure,
                    ActivityPlayerHandoffGroupStatus.FailedEvidence,
                    "Evidence failure was not explicit.");
                completed.Add("evidence-failure-explicit");
                AssertTailSequence(promotion.Calls,
                    "rollback:player.p3k7e.2", "rollback:player.p3k7e.1");
                completed.Add("evidence-failure-rolls-back-all");
                fixture.Evidence.FailCapture = false;

                promotion = new FakePromotionRuntime(fixture.PreparationTokens);
                context = CreateContext(contextType, promotion, fixture.Evidence);
                ready = Begin(contextType, context, fixture.Activity,
                    fixture.TargetOwner, fixture.Requests, "qa", "commit-reevaluation");
                fixture.Evidence.UseBlockedAdmission = true;
                ActivityPlayerHandoffGroupResult commitRejected = Commit(
                    contextType, context, ready.CurrentSnapshot.Token,
                    "qa", "commit-reevaluation-rejected");
                AssertStatus(commitRejected,
                    ActivityPlayerHandoffGroupStatus.RejectedAdmission,
                    "Commit did not re-evaluate P3K.6 before irreversibility.");
                completed.Add("commit-reevaluates-admission");
                AssertTailSequence(promotion.Calls,
                    "rollback:player.p3k7e.2", "rollback:player.p3k7e.1");
                completed.Add("commit-admission-failure-rolls-back-all");
                fixture.Evidence.UseBlockedAdmission = false;

                promotion = new FakePromotionRuntime(fixture.PreparationTokens)
                {
                    FailValidationSlot = fixture.SlotTwo
                };
                context = CreateContext(contextType, promotion, fixture.Evidence);
                ready = Begin(contextType, context, fixture.Activity,
                    fixture.TargetOwner, fixture.Requests, "qa", "validation-failure");
                ActivityPlayerHandoffGroupResult validationFailure = Commit(
                    contextType, context, ready.CurrentSnapshot.Token,
                    "qa", "validation-failure");
                AssertStatus(validationFailure,
                    ActivityPlayerHandoffGroupStatus.FailedCommitValidation,
                    "Per-Slot commit validation failure was not explicit.");
                completed.Add("all-slot-commit-prevalidation-required");
                AssertTrue(!ContainsPrefix(promotion.Calls, "commit:"),
                    "Group committed a Slot before all commit validations passed.");
                completed.Add("no-commit-before-global-prevalidation");
                AssertTailSequence(promotion.Calls,
                    "rollback:player.p3k7e.2", "rollback:player.p3k7e.1");
                completed.Add("commit-validation-failure-rolls-back-all");

                promotion = new FakePromotionRuntime(fixture.PreparationTokens);
                context = CreateContext(contextType, promotion, fixture.Evidence);
                ready = Begin(contextType, context, fixture.Activity,
                    fixture.TargetOwner, fixture.Requests, "qa", "successful-commit");
                ActivityPlayerHandoffGroupResult committed = Commit(
                    contextType, context, ready.CurrentSnapshot.Token,
                    "qa", "successful-commit");
                AssertStatus(committed,
                    ActivityPlayerHandoffGroupStatus.SucceededCommitted,
                    "Two-Slot group commit failed.");
                completed.Add("two-slot-group-commit-succeeds");
                AssertTailSequence(promotion.Calls,
                    "commit:player.p3k7e.1", "commit:player.p3k7e.2");
                completed.Add("slot-commit-order-deterministic");
                AssertTrue(committed.CurrentSnapshot.IsCommitted &&
                    committed.CurrentSnapshot.Slots[0].Committed &&
                    committed.CurrentSnapshot.Slots[1].Committed,
                    "Committed group lost per-Slot ownership evidence.");
                completed.Add("all-slot-ownership-committed");
                AssertTrue(!HasActive(contextType, context),
                    "Committed group remained active.");
                completed.Add("commit-clears-active-group");
                ActivityPlayerHandoffGroupResult repeatedCommit = Commit(
                    contextType, context, committed.CurrentSnapshot.Token,
                    "qa", "commit-again");
                AssertStatus(repeatedCommit,
                    ActivityPlayerHandoffGroupStatus.SucceededAlreadyCommitted,
                    "Committed group request was not idempotent.");
                completed.Add("committed-group-idempotent");
                ActivityPlayerHandoffGroupResult rollbackAfterCommit = Rollback(
                    contextType, context, committed.CurrentSnapshot.Token,
                    "qa", "rollback-after-commit");
                AssertStatus(rollbackAfterCommit,
                    ActivityPlayerHandoffGroupStatus.RejectedRollbackNotAvailable,
                    "Committed group accepted rollback.");
                completed.Add("rollback-rejected-after-group-commit");

                promotion = new FakePromotionRuntime(fixture.PreparationTokens)
                {
                    CleanupFailureSlot = fixture.SlotOne
                };
                context = CreateContext(contextType, promotion, fixture.Evidence);
                ready = Begin(contextType, context, fixture.Activity,
                    fixture.TargetOwner, fixture.Requests, "qa", "cleanup-retry");
                ActivityPlayerHandoffGroupResult cleanupFailure = Commit(
                    contextType, context, ready.CurrentSnapshot.Token,
                    "qa", "cleanup-fails-once");
                AssertStatus(cleanupFailure,
                    ActivityPlayerHandoffGroupStatus.FailedCommitCleanup,
                    "Previous Actor cleanup failure was not retained.");
                completed.Add("commit-cleanup-failure-retained");
                AssertTrue(cleanupFailure.CurrentSnapshot.IsCommitCleanupFailed &&
                    cleanupFailure.CurrentSnapshot.Slots[0].CleanupPending &&
                    cleanupFailure.CurrentSnapshot.Slots[1].Committed,
                    "Cleanup failure lost target-authoritative evidence.");
                completed.Add("all-target-ownership-authoritative-before-cleanup-retry");
                ActivityPlayerHandoffGroupResult cleanupRollback = Rollback(
                    contextType, context, cleanupFailure.CurrentSnapshot.Token,
                    "qa", "rollback-during-cleanup");
                AssertStatus(cleanupRollback,
                    ActivityPlayerHandoffGroupStatus.RejectedRollbackNotAvailable,
                    "Cleanup-pending committed group accepted rollback.");
                completed.Add("rollback-rejected-during-commit-cleanup");
                ActivityPlayerHandoffGroupResult cleanupRetry = RetryCleanup(
                    contextType, context, cleanupFailure.CurrentSnapshot.Token,
                    "qa", "retry-cleanup");
                AssertStatus(cleanupRetry,
                    ActivityPlayerHandoffGroupStatus.SucceededCommitted,
                    "Commit cleanup retry did not complete the group.");
                completed.Add("group-commit-cleanup-retry-succeeds");
                AssertTrue(!HasActive(contextType, context),
                    "Successful cleanup retry retained active group state.");
                completed.Add("cleanup-retry-clears-active-group");

                ValidateNoUnityReferences(typeof(ActivityPlayerHandoffGroupToken));
                ValidateNoUnityReferences(typeof(ActivityPlayerHandoffGroupSlotSnapshot));
                ValidateNoUnityReferences(typeof(ActivityPlayerHandoffGroupSnapshot));
                ValidateNoUnityReferences(typeof(ActivityPlayerHandoffGroupResult));
                completed.Add("public-group-contracts-no-unity-references");

                AssertEqual(45, completed.Count,
                    "P3K.7E smoke case count changed.");
                Debug.Log(
                    "[P3K7E_MULTI_SLOT_ACTIVITY_HANDOFF_GROUP_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                Debug.LogError(
                    "[P3K7E_MULTI_SLOT_ACTIVITY_HANDOFF_GROUP_SMOKE] status='Failed' " +
                    $"exception='{inner.GetType().Name}' message='{Escape(inner.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw inner;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K7E_MULTI_SLOT_ACTIVITY_HANDOFF_GROUP_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
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

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        private sealed class FakePromotionRuntime : IPlayerGameplayChainPromotionRuntime
        {
            private readonly Dictionary<PlayerSlotId, PlayerActorPreparationToken> preparations;
            private readonly Dictionary<PlayerSlotId, PlayerGameplayChainHandoffSnapshot> active =
                new Dictionary<PlayerSlotId, PlayerGameplayChainHandoffSnapshot>();
            private int sequence;
            internal FakePromotionRuntime(
                Dictionary<PlayerSlotId, PlayerActorPreparationToken> preparations)
            {
                this.preparations = preparations;
            }
            internal readonly List<string> Calls = new List<string>();
            internal PlayerSlotId FailBeginSlot;
            internal PlayerSlotId FailValidationSlot;
            internal PlayerSlotId CleanupFailureSlot;

            public PlayerGameplayChainHandoffResult TryBeginPromotion(
                PlayerActorCandidateStageToken candidate,
                PlayerGameplayAdmissionToken currentAdmission,
                string source,
                string reason)
            {
                Calls.Add("begin:" + candidate.PlayerSlotId.Value.Value);
                if (candidate.PlayerSlotId == FailBeginSlot)
                {
                    return CreateHandoffResult(
                        PlayerGameplayChainHandoffStatus.FailedCandidateChain,
                        default, PlayerGameplayChainHandoffState.RolledBack,
                        "Synthetic begin failure.");
                }
                sequence++;
                PlayerGameplayChainHandoffToken token = Construct<PlayerGameplayChainHandoffToken>(
                    new[] { typeof(string), typeof(PlayerSlotId),
                        typeof(PlayerActorCandidateStageToken),
                        typeof(PlayerActorPreparationToken),
                        typeof(PlayerGameplayAdmissionToken), typeof(int) },
                    candidate.SessionContextId, candidate.PlayerSlotId, candidate,
                    preparations[candidate.PlayerSlotId], currentAdmission, sequence);
                PlayerGameplayChainHandoffSnapshot snapshot = CreateHandoffSnapshot(
                    token, PlayerGameplayChainHandoffState.CandidateChainReady,
                    preparations[candidate.PlayerSlotId], currentAdmission,
                    true, true, true, false, false, false, false,
                    "Synthetic handoff ready to commit.");
                active[candidate.PlayerSlotId] = snapshot;
                return CreateHandoffResult(
                    PlayerGameplayChainHandoffStatus.SucceededReadyToCommit,
                    snapshot, snapshot.State, snapshot.Message);
            }

            public bool TryValidateCommitPromotion(
                PlayerGameplayChainHandoffToken handoff, out string issue)
            {
                Calls.Add("validate:" + handoff.PlayerSlotId.Value.Value);
                if (handoff.PlayerSlotId == FailValidationSlot)
                {
                    issue = "Synthetic commit validation failure.";
                    return false;
                }
                issue = string.Empty;
                return active.ContainsKey(handoff.PlayerSlotId);
            }

            public PlayerGameplayChainHandoffResult TryCommitPromotion(
                PlayerGameplayChainHandoffToken handoff, string source, string reason)
            {
                Calls.Add("commit:" + handoff.PlayerSlotId.Value.Value);
                bool cleanupFailure = handoff.PlayerSlotId == CleanupFailureSlot;
                PlayerGameplayChainHandoffSnapshot snapshot = CreateHandoffSnapshot(
                    handoff,
                    cleanupFailure
                        ? PlayerGameplayChainHandoffState.CommitCleanupFailed
                        : PlayerGameplayChainHandoffState.Committed,
                    preparations[handoff.PlayerSlotId], handoff.PreviousAdmissionToken,
                    true, true, true, true, !cleanupFailure, false, false,
                    cleanupFailure
                        ? "Synthetic previous Actor cleanup failure."
                        : "Synthetic handoff committed.");
                active[handoff.PlayerSlotId] = snapshot;
                return CreateHandoffResult(
                    cleanupFailure
                        ? PlayerGameplayChainHandoffStatus.FailedPreviousActorRelease
                        : PlayerGameplayChainHandoffStatus.SucceededCommitted,
                    snapshot, snapshot.State, snapshot.Message);
            }

            public PlayerGameplayChainHandoffResult TryRollbackPromotion(
                PlayerGameplayChainHandoffToken handoff, string source, string reason)
            {
                Calls.Add("rollback:" + handoff.PlayerSlotId.Value.Value);
                active.Remove(handoff.PlayerSlotId);
                PlayerGameplayChainHandoffSnapshot snapshot = CreateHandoffSnapshot(
                    handoff, PlayerGameplayChainHandoffState.RolledBack,
                    handoff.PreviousPreparationToken, handoff.PreviousAdmissionToken,
                    false, false, false, false, false, true, true,
                    "Synthetic handoff rolled back.");
                return CreateHandoffResult(
                    PlayerGameplayChainHandoffStatus.SucceededRolledBack,
                    snapshot, snapshot.State, snapshot.Message);
            }

            public PlayerGameplayChainHandoffResult TryRetryCommitCleanup(
                PlayerGameplayChainHandoffToken handoff, string source, string reason)
            {
                Calls.Add("cleanup:" + handoff.PlayerSlotId.Value.Value);
                CleanupFailureSlot = default;
                PlayerGameplayChainHandoffSnapshot snapshot = CreateHandoffSnapshot(
                    handoff, PlayerGameplayChainHandoffState.Committed,
                    preparations[handoff.PlayerSlotId], handoff.PreviousAdmissionToken,
                    true, true, true, true, true, false, false,
                    "Synthetic cleanup completed.");
                active[handoff.PlayerSlotId] = snapshot;
                return CreateHandoffResult(
                    PlayerGameplayChainHandoffStatus.SucceededCommitted,
                    snapshot, snapshot.State, snapshot.Message);
            }
        }

        private sealed class FakeEvidenceSource : IActivityPlayerHandoffEvidenceSource
        {
            internal PlayerParticipationSnapshot Participation;
            internal PlayerActorPreparationSnapshot Preparation;
            internal PlayerGameplayAdmissionSnapshot ReadyAdmission;
            internal PlayerGameplayAdmissionSnapshot BlockedAdmission;
            internal bool UseBlockedAdmission;
            internal bool FailCapture;
            public bool TryCapture(
                out PlayerParticipationSnapshot participation,
                out PlayerActorPreparationSnapshot preparation,
                out PlayerGameplayAdmissionSnapshot admission,
                out string issue)
            {
                participation = Participation;
                preparation = Preparation;
                admission = UseBlockedAdmission ? BlockedAdmission : ReadyAdmission;
                issue = FailCapture ? "Synthetic evidence capture failure." : string.Empty;
                return !FailCapture;
            }
        }

        private sealed class Fixture
        {
            internal ActivityAsset Activity;
            internal RuntimeContentOwner CurrentOwner;
            internal RuntimeContentOwner TargetOwner;
            internal PlayerSlotId SlotOne;
            internal PlayerSlotId SlotTwo;
            internal ActivityPlayerHandoffSlotRequest[] Requests;
            internal Dictionary<PlayerSlotId, PlayerActorPreparationToken> PreparationTokens;
            internal FakeEvidenceSource Evidence;

            internal static Fixture Create(List<UnityEngine.Object> created)
            {
                const string session = "qa.p3k7e.session";
                PlayerSlotProfile slotOneProfile = CreateSlotProfile(
                    "player.p3k7e.1", "P3K.7E Player 1", created);
                PlayerSlotProfile slotTwoProfile = CreateSlotProfile(
                    "player.p3k7e.2", "P3K.7E Player 2", created);
                ActorProfile actorOne = CreateActorProfile(
                    "actor.p3k7e.1", "P3K.7E Actor 1", created);
                ActorProfile actorTwo = CreateActorProfile(
                    "actor.p3k7e.2", "P3K.7E Actor 2", created);
                ActivityParticipationProjectionProfile projection = CreateProjection(
                    new[] { slotOneProfile, slotTwoProfile }, created);
                PlayerParticipationRequirementsProfile requirements = CreateRequirements(created);
                ActivityAsset activity = CreateActivity(projection, requirements, created);
                RuntimeContentOwner currentOwner = RuntimeContentOwner.Activity(
                    "qa.p3k7e.current", "P3K.7E Current");
                RuntimeContentOwner targetOwner = RuntimeContentOwner.Activity(
                    "qa.p3k7e.target", "P3K.7E Target");

                PlayerActorPreparationSummary currentOne = CreatePreparation(
                    session, slotOneProfile.PlayerSlotId, actorOne.ActorProfileId,
                    ActorId.From("qa.p3k7e.current.actor.1"), currentOwner, 1, 1);
                PlayerActorPreparationSummary currentTwo = CreatePreparation(
                    session, slotTwoProfile.PlayerSlotId, actorTwo.ActorProfileId,
                    ActorId.From("qa.p3k7e.current.actor.2"), currentOwner, 2, 1);
                PlayerGameplayAdmissionSummary currentAdmissionOne = CreateAdmission(
                    currentOne, PlayerGameplayAdmissionState.Ready, 1);
                PlayerGameplayAdmissionSummary currentAdmissionTwo = CreateAdmission(
                    currentTwo, PlayerGameplayAdmissionState.Ready, 2);

                PlayerActorPreparationSummary targetOne = CreatePreparation(
                    session, slotOneProfile.PlayerSlotId, actorOne.ActorProfileId,
                    ActorId.From("qa.p3k7e.target.actor.1"), targetOwner, 3, 1);
                PlayerActorPreparationSummary targetTwo = CreatePreparation(
                    session, slotTwoProfile.PlayerSlotId, actorTwo.ActorProfileId,
                    ActorId.From("qa.p3k7e.target.actor.2"), targetOwner, 4, 1);
                PlayerGameplayAdmissionSummary targetAdmissionOne = CreateAdmission(
                    targetOne, PlayerGameplayAdmissionState.Ready, 3);
                PlayerGameplayAdmissionSummary targetAdmissionTwo = CreateAdmission(
                    targetTwo, PlayerGameplayAdmissionState.Ready, 4);
                PlayerGameplayAdmissionSummary blockedTwo = CreateAdmission(
                    targetTwo, PlayerGameplayAdmissionState.BlockedByInputGate, 4);

                PlayerActorCandidateStageToken candidateOne = CreateCandidate(
                    session, targetOwner, targetOne, 1);
                PlayerActorCandidateStageToken candidateTwo = CreateCandidate(
                    session, targetOwner, targetTwo, 2);

                return new Fixture
                {
                    Activity = activity,
                    CurrentOwner = currentOwner,
                    TargetOwner = targetOwner,
                    SlotOne = slotOneProfile.PlayerSlotId,
                    SlotTwo = slotTwoProfile.PlayerSlotId,
                    Requests = new[]
                    {
                        new ActivityPlayerHandoffSlotRequest(
                            candidateOne, currentAdmissionOne.Token),
                        new ActivityPlayerHandoffSlotRequest(
                            candidateTwo, currentAdmissionTwo.Token)
                    },
                    PreparationTokens = new Dictionary<PlayerSlotId, PlayerActorPreparationToken>
                    {
                        { slotOneProfile.PlayerSlotId, currentOne.Token },
                        { slotTwoProfile.PlayerSlotId, currentTwo.Token }
                    },
                    Evidence = new FakeEvidenceSource
                    {
                        Participation = CreateParticipationSnapshot(
                            session,
                            new SlotSpec(slotOneProfile, actorOne),
                            new SlotSpec(slotTwoProfile, actorTwo)),
                        Preparation = CreatePreparationSnapshot(
                            session, targetOne, targetTwo),
                        ReadyAdmission = CreateAdmissionSnapshot(
                            session, targetAdmissionOne, targetAdmissionTwo),
                        BlockedAdmission = CreateAdmissionSnapshot(
                            session, targetAdmissionOne, blockedTwo)
                    }
                };
            }
        }

        private readonly struct SlotSpec
        {
            internal SlotSpec(PlayerSlotProfile profile, ActorProfile actor)
            {
                Profile = profile;
                Actor = actor;
            }
            internal PlayerSlotProfile Profile { get; }
            internal ActorProfile Actor { get; }
        }

        private static object CreateContext(
            Type contextType,
            IPlayerGameplayChainPromotionRuntime promotion,
            IActivityPlayerHandoffEvidenceSource evidence)
        {
            object[] arguments = { promotion, evidence, null, null };
            bool created = (bool)GetMethod(contextType, "TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(created && arguments[2] != null,
                "P3K.7E group context creation failed. " + arguments[3]);
            return arguments[2];
        }

        private static ActivityPlayerHandoffGroupResult Begin(
            Type type, object context, ActivityAsset activity,
            RuntimeContentOwner owner,
            IReadOnlyList<ActivityPlayerHandoffSlotRequest> requests,
            string source, string reason) =>
            (ActivityPlayerHandoffGroupResult)GetMethod(type, "TryBegin", InstanceAny)
                .Invoke(context, new object[] { activity, owner, requests, source, reason });

        private static ActivityPlayerHandoffGroupResult Commit(
            Type type, object context, ActivityPlayerHandoffGroupToken token,
            string source, string reason) =>
            (ActivityPlayerHandoffGroupResult)GetMethod(type, "TryCommit", InstanceAny)
                .Invoke(context, new object[] { token, source, reason });

        private static ActivityPlayerHandoffGroupResult Rollback(
            Type type, object context, ActivityPlayerHandoffGroupToken token,
            string source, string reason) =>
            (ActivityPlayerHandoffGroupResult)GetMethod(type, "TryRollback", InstanceAny)
                .Invoke(context, new object[] { token, source, reason });

        private static ActivityPlayerHandoffGroupResult RetryCleanup(
            Type type, object context, ActivityPlayerHandoffGroupToken token,
            string source, string reason) =>
            (ActivityPlayerHandoffGroupResult)GetMethod(
                type, "TryRetryCommitCleanup", InstanceAny)
                .Invoke(context, new object[] { token, source, reason });

        private static bool HasActive(Type type, object context) =>
            (bool)type.GetProperty("HasActiveGroup", InstanceAny).GetValue(context);

        private static void ValidateSurface(Type contextType)
        {
            AssertNotNull(GetMethod(contextType, "TryBegin", InstanceAny),
                "P3K.7E TryBegin is missing.");
            AssertNotNull(GetMethod(contextType, "TryCommit", InstanceAny),
                "P3K.7E TryCommit is missing.");
            AssertNotNull(GetMethod(contextType, "TryRollback", InstanceAny),
                "P3K.7E TryRollback is missing.");
            Type handoffType = typeof(PlayerGameplayChainHandoffResult).Assembly.GetType(
                "Immersive.Framework.PlayerParticipation.PlayerGameplayChainHandoffRuntimeContext",
                false);
            AssertNotNull(handoffType, "P3K.7D handoff runtime is missing.");
            AssertNotNull(GetMethod(handoffType, "TryBeginPromotion", InstanceAny),
                "P3K.7D two-phase Begin is missing.");
            AssertNotNull(GetMethod(handoffType, "TryCommitPromotion", InstanceAny),
                "P3K.7D two-phase Commit is missing.");
            AssertNotNull(GetMethod(handoffType, "TryPromote", InstanceAny),
                "P3K.7D compatibility TryPromote wrapper is missing.");
        }

        private static PlayerSlotProfile CreateSlotProfile(
            string id, string name, List<UnityEngine.Object> created)
        {
            PlayerSlotProfile profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = name;
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static ActorProfile CreateActorProfile(
            string id, string name, List<UnityEngine.Object> created)
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = name;
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static ActivityParticipationProjectionProfile CreateProjection(
            PlayerSlotProfile[] slots, List<UnityEngine.Object> created)
        {
            ActivityParticipationProjectionProfile profile =
                ScriptableObject.CreateInstance<ActivityParticipationProjectionProfile>();
            profile.name = "P3K.7E Explicit Two Slots";
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = profile.name;
            serialized.FindProperty("projectionMode").intValue =
                (int)ActivityParticipationProjectionMode.ExplicitSlots;
            serialized.FindProperty("zeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Rejected;
            SerializedProperty array = serialized.FindProperty("explicitSlotProfiles");
            array.arraySize = slots.Length;
            for (int index = 0; index < slots.Length; index++)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = slots[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static PlayerParticipationRequirementsProfile CreateRequirements(
            List<UnityEngine.Object> created)
        {
            PlayerParticipationRequirementsProfile profile =
                ScriptableObject.CreateInstance<PlayerParticipationRequirementsProfile>();
            profile.name = "P3K.7E GameplayReady";
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = profile.name;
            serialized.FindProperty("requirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.GameplayReady;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static ActivityAsset CreateActivity(
            ActivityParticipationProjectionProfile projection,
            PlayerParticipationRequirementsProfile requirements,
            List<UnityEngine.Object> created)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = "P3K.7E Multi-Slot Target";
            created.Add(activity);
            SerializedObject serialized = new SerializedObject(activity);
            serialized.FindProperty("activityName").stringValue = activity.name;
            serialized.FindProperty("playerParticipationProjectionProfile")
                .objectReferenceValue = projection;
            serialized.FindProperty("playerParticipationRequirementsProfile")
                .objectReferenceValue = requirements;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static PlayerParticipationSnapshot CreateParticipationSnapshot(
            string session, params SlotSpec[] specs)
        {
            var slots = new PlayerSlotRuntimeSnapshot[specs.Length];
            ConstructorInfo slotConstructor = typeof(PlayerSlotRuntimeSnapshot).GetConstructor(
                InstanceAny, null, new[]
                {
                    typeof(int), typeof(PlayerSlotProfile), typeof(PlayerSlotId),
                    typeof(PlayerSlotAllocationState), typeof(PlayerSlotReservationToken),
                    typeof(int), typeof(string), typeof(string), typeof(ActorProfile),
                    typeof(int), typeof(string), typeof(string)
                }, null);
            for (int index = 0; index < specs.Length; index++)
            {
                slots[index] = (PlayerSlotRuntimeSnapshot)slotConstructor.Invoke(new object[]
                {
                    index, specs[index].Profile, specs[index].Profile.PlayerSlotId,
                    PlayerSlotAllocationState.Joined, default(PlayerSlotReservationToken),
                    2, "qa", "joined", specs[index].Actor, 1, "qa", "selected"
                });
            }
            return Construct<PlayerParticipationSnapshot>(new[]
            {
                typeof(string), typeof(int), typeof(bool), typeof(int), typeof(bool),
                typeof(PlayerActorSelectionPolicyProfile),
                typeof(PlayerSlotRuntimeSnapshot[]),
                typeof(PlayerParticipationOperationStatus), typeof(string)
            }, session, 1, true, specs.Length, false, null, slots,
                PlayerParticipationOperationStatus.Succeeded, "Synthetic participation.");
        }

        private static PlayerActorPreparationSummary CreatePreparation(
            string session, PlayerSlotId slot, ActorProfileId profile,
            ActorId actor, RuntimeContentOwner owner, int revision,
            int selectionRevision)
        {
            PlayerActorMaterializationOperationId operation = CreateOperationId(
                session, owner, slot, revision);
            RuntimeContentIdentity identity = RuntimeContentIdentity.From(
                owner, $"qa.p3k7e.content.{slot.Value.Value}.{revision}");
            PlayerActorMaterializationSnapshot materialization =
                Construct<PlayerActorMaterializationSnapshot>(new[]
                {
                    typeof(PlayerActorMaterializationOperationId),
                    typeof(RuntimeContentIdentity), typeof(PlayerSlotId),
                    typeof(ActorProfileId), typeof(ActorId), typeof(int),
                    typeof(PlayerActorMaterializationState), typeof(string), typeof(string)
                }, operation, identity, slot, profile, actor, revision,
                    PlayerActorMaterializationState.Active, "qa", "synthetic");
            return Construct<PlayerActorPreparationSummary>(new[]
            {
                typeof(string), typeof(PlayerSlotId),
                typeof(PlayerActorPreparationState), typeof(ActorProfileId), typeof(int),
                typeof(PlayerActorMaterializationSnapshot), typeof(string),
                typeof(string), typeof(string)
            }, session, slot, PlayerActorPreparationState.Prepared, profile,
                selectionRevision, materialization, "qa", "prepared", "Synthetic preparation.");
        }

        private static PlayerActorPreparationSnapshot CreatePreparationSnapshot(
            string session, params PlayerActorPreparationSummary[] preparations) =>
            Construct<PlayerActorPreparationSnapshot>(new[]
            {
                typeof(string), typeof(int), typeof(PlayerActorPreparationSummary[]),
                typeof(PlayerActorMaterializationSnapshot[]),
                typeof(PlayerActorPreparationStatus), typeof(string)
            }, session, 1, preparations, Array.Empty<PlayerActorMaterializationSnapshot>(),
                PlayerActorPreparationStatus.SucceededPrepared, "Synthetic preparation snapshot.");

        private static PlayerGameplayAdmissionSummary CreateAdmission(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayAdmissionState state, int revision)
        {
            string session = preparation.SessionContextId;
            PlayerSlotId slot = preparation.PlayerSlotId;
            ActorProfileId profile = preparation.PreparedActorProfileId;
            ActorId actor = preparation.Materialization.ActorId;
            RuntimeContentOwner owner = preparation.Materialization.Owner;
            RuntimeContentIdentity identity = preparation.Materialization.RuntimeContentIdentity;
            int materializationRevision = preparation.Materialization.MaterializationRevision;
            PlayerGameplayOccupancyToken occupancy = Construct<PlayerGameplayOccupancyToken>(
                new[] { typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                    typeof(ActorProfileId), typeof(ActorId), typeof(PlayerActorPreparationToken),
                    typeof(RuntimeContentIdentity), typeof(int), typeof(int) },
                session, owner, slot, profile, actor, preparation.Token, identity,
                materializationRevision, revision);
            PlayerGameplayInputBindingToken input = Construct<PlayerGameplayInputBindingToken>(
                new[] { typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                    typeof(ActorProfileId), typeof(ActorId), typeof(PlayerActorPreparationToken),
                    typeof(PlayerGameplayOccupancyToken), typeof(RuntimeContentIdentity),
                    typeof(int), typeof(int), typeof(int) },
                session, owner, slot, profile, actor, preparation.Token, occupancy,
                identity, materializationRevision, revision, revision);
            PlayerGameplayCameraEligibilityToken camera =
                Construct<PlayerGameplayCameraEligibilityToken>(new[]
                {
                    typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                    typeof(ActorProfileId), typeof(ActorId), typeof(PlayerActorPreparationToken),
                    typeof(PlayerGameplayOccupancyToken),
                    typeof(PlayerGameplayInputBindingToken), typeof(RuntimeContentIdentity),
                    typeof(int), typeof(int), typeof(int), typeof(int)
                }, session, owner, slot, profile, actor, preparation.Token, occupancy,
                    input, identity, materializationRevision, revision, revision, revision);
            PlayerGameplayAdmissionToken token = Construct<PlayerGameplayAdmissionToken>(new[]
            {
                typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                typeof(ActorProfileId), typeof(ActorId), typeof(RuntimeContentIdentity),
                typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)
            }, session, owner, slot, profile, actor, identity, materializationRevision,
                revision, revision, revision, revision);
            return Construct<PlayerGameplayAdmissionSummary>(new[]
            {
                typeof(string), typeof(PlayerSlotId), typeof(PlayerGameplayAdmissionState),
                typeof(ActorProfileId), typeof(ActorId), typeof(RuntimeContentOwner),
                typeof(RuntimeContentIdentity), typeof(PlayerActorPreparationToken),
                typeof(PlayerGameplayOccupancyToken), typeof(PlayerGameplayInputBindingToken),
                typeof(PlayerGameplayCameraEligibilityToken), typeof(PlayerGameplayAdmissionToken),
                typeof(PlayerGameplayCameraEligibilityState),
                typeof(PlayerGameplayCameraRequiredness), typeof(bool), typeof(string),
                typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                typeof(int), typeof(string), typeof(string), typeof(string)
            }, session, slot, state, profile, actor, owner, identity, preparation.Token,
                occupancy, input, camera, token,
                PlayerGameplayCameraEligibilityState.SkippedOptional,
                PlayerGameplayCameraRequiredness.Optional, false, string.Empty,
                string.Empty, true, false, false, false, revision,
                "qa", "admitted", "Synthetic admission.");
        }

        private static PlayerGameplayAdmissionSnapshot CreateAdmissionSnapshot(
            string session, params PlayerGameplayAdmissionSummary[] admissions) =>
            Construct<PlayerGameplayAdmissionSnapshot>(new[]
            {
                typeof(string), typeof(int), typeof(PlayerGameplayAdmissionSummary[]),
                typeof(PlayerGameplayAdmissionStatus), typeof(string)
            }, session, 1, admissions, PlayerGameplayAdmissionStatus.SucceededReady,
                "Synthetic admission snapshot.");

        private static PlayerActorCandidateStageToken CreateCandidate(
            string session, RuntimeContentOwner owner,
            PlayerActorPreparationSummary target, int revision) =>
            Construct<PlayerActorCandidateStageToken>(new[]
            {
                typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                typeof(ActorProfileId), typeof(ActorId),
                typeof(RuntimeContentIdentity), typeof(int)
            }, session, owner, target.PlayerSlotId, target.PreparedActorProfileId,
                target.Materialization.ActorId, target.Materialization.RuntimeContentIdentity,
                revision);

        private static PlayerActorMaterializationOperationId CreateOperationId(
            string session, RuntimeContentOwner owner, PlayerSlotId slot, int revision)
        {
            MethodInfo method = typeof(PlayerActorMaterializationOperationId)
                .GetMethod("TryCreate", StaticAny);
            object[] args = { session, owner, slot, revision,
                default(PlayerActorMaterializationOperationId), null };
            AssertTrue((bool)method.Invoke(null, args),
                "Synthetic operation identity failed. " + args[5]);
            return (PlayerActorMaterializationOperationId)args[4];
        }

        private static PlayerGameplayChainHandoffSnapshot CreateHandoffSnapshot(
            PlayerGameplayChainHandoffToken token,
            PlayerGameplayChainHandoffState state,
            PlayerActorPreparationToken preparation,
            PlayerGameplayAdmissionToken admission,
            bool currentReleased, bool swapped, bool chainReady,
            bool ownershipCompleted, bool previousReleased,
            bool rollbackAttempted, bool rollbackSucceeded, string message) =>
            Construct<PlayerGameplayChainHandoffSnapshot>(new[]
            {
                typeof(PlayerGameplayChainHandoffToken),
                typeof(PlayerGameplayChainHandoffState),
                typeof(PlayerActorPreparationToken), typeof(PlayerGameplayAdmissionToken),
                typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                typeof(bool), typeof(bool), typeof(string), typeof(string), typeof(string)
            }, token, state, preparation, admission, currentReleased, swapped,
                chainReady, ownershipCompleted, previousReleased, rollbackAttempted,
                rollbackSucceeded, "qa", "synthetic-handoff", message);

        private static PlayerGameplayChainHandoffResult CreateHandoffResult(
            PlayerGameplayChainHandoffStatus status,
            PlayerGameplayChainHandoffSnapshot snapshot,
            PlayerGameplayChainHandoffState state,
            string message)
        {
            PlayerGameplayChainHandoffSnapshot effective = snapshot ??
                CreateHandoffSnapshot(default, state, default, default,
                    false, false, false, false, false, false, false, message);
            return Construct<PlayerGameplayChainHandoffResult>(new[]
            {
                typeof(PlayerGameplayChainHandoffStatus), typeof(string),
                typeof(PlayerGameplayChainHandoffSnapshot),
                typeof(PlayerGameplayChainHandoffSnapshot), typeof(string)
            }, status, "Synthetic", effective, effective, message);
        }

        private static ActivityPlayerHandoffGroupToken CreateStaleGroupToken(
            ActivityPlayerHandoffGroupToken source) =>
            Construct<ActivityPlayerHandoffGroupToken>(new[]
            {
                typeof(string), typeof(RuntimeContentOwner), typeof(int), typeof(int)
            }, source.SessionContextId, source.TargetOwner, source.SlotCount,
                source.GroupRevision + 100);

        private static T Construct<T>(Type[] signature, params object[] args)
        {
            ConstructorInfo constructor = typeof(T).GetConstructor(
                InstanceAny, null, signature, null);
            AssertNotNull(constructor, typeof(T).Name + " constructor changed.");
            return (T)constructor.Invoke(args);
        }

        private static MethodInfo GetMethod(Type type, string name, BindingFlags flags)
        {
            MethodInfo method = type.GetMethod(name, flags);
            AssertNotNull(method, $"Method '{type.FullName}.{name}' is missing.");
            return method;
        }

        private static void ValidateNoUnityReferences(Type type)
        {
            foreach (PropertyInfo property in type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public))
            {
                AssertTrue(!typeof(UnityEngine.Object).IsAssignableFrom(property.PropertyType),
                    $"Public contract retains Unity reference: {type.Name}.{property.Name}.");
            }
        }

        private static bool ContainsPrefix(List<string> values, string prefix)
        {
            for (int index = 0; index < values.Count; index++)
            {
                if (values[index].StartsWith(prefix, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void AssertSequence(List<string> actual, params string[] expected)
        {
            AssertEqual(expected.Length, actual.Count, "Call sequence length changed.");
            for (int index = 0; index < expected.Length; index++)
            {
                AssertEqual(expected[index], actual[index], $"Call sequence mismatch at '{index}'.");
            }
        }

        private static void AssertTailSequence(List<string> actual, params string[] expected)
        {
            AssertTrue(actual.Count >= expected.Length, "Call sequence is shorter than expected tail.");
            int offset = actual.Count - expected.Length;
            for (int index = 0; index < expected.Length; index++)
            {
                AssertEqual(expected[index], actual[offset + index],
                    $"Call tail mismatch at '{index}'.");
            }
        }

        private static void AssertStatus(ActivityPlayerHandoffGroupResult result,
            ActivityPlayerHandoffGroupStatus expected, string message)
        {
            AssertNotNull(result, "P3K.7E group returned null.");
            AssertEqual(expected, result.Status, message + " " + result.ToDiagnosticString());
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private static void AssertNotNull(object value, string message)
        {
            if (value == null) throw new InvalidOperationException(message);
        }
        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }
        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

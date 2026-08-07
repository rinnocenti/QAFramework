using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.PlayerAssignment.Internal.Editor
{
    /// <summary>
    /// Synthetic regression for reservation, Local Player Host provisioning, correlation,
    /// staged Slot admission and rollback. No real PlayerInputManager timing is required.
    /// </summary>
    internal static class QaP3G3ProvisioningBridgeSyntheticSmoke
    {
        [MenuItem("Immersive Framework/QA/Regressions/Player/Run Local Player Provisioning Regression")]
        internal static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();
            var disposables = new List<IDisposable>();

            try
            {
                RunSuccessfulOrderedJoinCases(created, disposables, completed);
                RunHostEvidenceContractCases(created, disposables, completed);
                RunActorCorrelationCases(created, disposables, completed);
                RunLateCallbackCases(created, disposables, completed);
                RunRollbackCases(created, disposables, completed);
                RunPolicyRejectionCases(created, disposables, completed);
                RunReentrantCase(created, disposables, completed);

                Debug.Log(
                    "[P3G3_PROVISIONING_BRIDGE_SYNTHETIC_SMOKE] status='PASS' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3G3_PROVISIONING_BRIDGE_SYNTHETIC_SMOKE] status='FAIL' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = disposables.Count - 1; index >= 0; index--)
                {
                    disposables[index]?.Dispose();
                }

                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private static void RunSuccessfulOrderedJoinCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 2, true, 2);
            PlayerInput firstPlayer = CreatePlayerHost(created, "QA P3G3 Player 1", true);
            fixture.Backend.NextPlayerInput = firstPlayer;
            fixture.Backend.CallbackPlayerInput = firstPlayer;
            fixture.Backend.EmitCallbackBeforeReturn = true;

            LocalPlayerJoinResult first = fixture.Join("first-join");
            AssertStatus(first, LocalPlayerJoinStatus.SucceededJoined,
                "First synthetic join failed.");
            AssertEqual(
                LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput,
                first.CallbackConfirmation,
                "Callback-first join was not correlated.");
            AssertEqual(0, first.Slot.ConfiguredIndex,
                "First join did not receive configured Slot index 0.");
            AssertTrue(
                fixture.TryGetCurrentAssignment(
                    first.Slot.PlayerSlotId,
                    out PlayerSlotAssignmentSnapshot firstAssignment),
                "Successful Manager join created no canonical current assignment.");
            AssertEqual(
                PlayerSlotAssignmentOrigin.ManagerProvisioned,
                firstAssignment.AssignmentOrigin,
                "Successful Manager join created the wrong assignment origin.");
            AssertEqual(
                RuntimeContentScope.Session,
                firstAssignment.AssignmentOwner.Scope,
                "Manager-Provisioned assignment does not use a Session owner.");
            AssertEqual(
                fixture.Snapshot.ContextId,
                firstAssignment.AssignmentOwner.OwnerId,
                "Manager-Provisioned assignment owner belongs to another Session context.");
            AssertEqual(
                first.AssignmentToken,
                firstAssignment.AssignmentToken,
                "Join result and current assignment tokens differ.");
            AssertEqual(
                first.HostBindingIdentity,
                firstAssignment.HostBindingIdentity,
                "Join result and current assignment Host bindings differ.");
            AssertTrue(
                fixture.IsHostRegistered(first.PlayerInput),
                "Physical Host registry does not contain the PlayerInput correlated to the assignment.");
            PlayerHostEvidenceResult hostEvidence =
                fixture.RegisterHostEvidence(first);
            AssertHostEvidenceStatus(
                hostEvidence,
                PlayerHostEvidenceStatus.SucceededRegistered,
                "Manager join did not register correlated Host evidence.");
            completed.Add("manager-session-owner");
            completed.Add("manager-real-integration");
            completed.Add("manager-registers-correlated-host-evidence");

            AssertNotNull(first.LocalPlayerHost,
                "Successful result has no Local Player Host evidence.");
            AssertSame(firstPlayer, first.LocalPlayerHost.PlayerInput,
                "Local Player Host does not resolve direct PlayerInput.");
            AssertTrue(first.LocalPlayerHost.HasJoinedSlot,
                "Local Player Host did not commit Slot binding.");
            AssertEqual(first.Slot.PlayerSlotId, first.LocalPlayerHost.JoinedPlayerSlotId,
                "Host Slot identity differs from Session commit.");
            completed.Add("technical-host-slot-binding-committed");

            AssertTrue(first.LocalPlayerHost.transform.IsChildOf(fixture.TechnicalHostParent),
                "Admitted technical host is not parented below the explicit Session lifetime parent.");
            AssertEqual(
                fixture.TechnicalHostParent.gameObject.scene,
                first.LocalPlayerHost.gameObject.scene,
                "Admitted technical host and Session lifetime parent belong to different Scenes.");
            AssertTrue(
                fixture.TryAttachHost(first.LocalPlayerHost, out string attachIssue),
                "Idempotent technical-host admission failed. " + attachIssue);
            AssertEqual(string.Empty, attachIssue,
                "Successful technical-host admission retained an issue.");
            completed.Add("technical-host-parented-with-empty-issue");

            AssertTrue(!first.LocalPlayerHost.HasLogicalActor,
                "Join materialized a Logical Actor unexpectedly.");
            AssertTrue(first.LocalPlayerHost.ActorMount != null &&
                first.LocalPlayerHost.ActorMount.GetComponentInChildren<ActorDeclaration>(true) == null,
                "Actor Mount is not empty after join.");
            completed.Add("join-leaves-logical-actor-unprepared");

            PlayerInput secondPlayer = CreatePlayerHost(created, "QA P3G3 Player 2", true);
            fixture.Backend.NextPlayerInput = secondPlayer;
            fixture.Backend.CallbackPlayerInput = secondPlayer;
            LocalPlayerJoinResult second = fixture.Join("second-join");
            AssertStatus(second, LocalPlayerJoinStatus.SucceededJoined,
                "Second synthetic join failed.");
            AssertEqual(1, second.Slot.ConfiguredIndex,
                "Second join did not receive configured Slot index 1.");
            AssertTrue(first.Slot.PlayerSlotId != second.Slot.PlayerSlotId,
                "Two Players received one Slot identity.");
            completed.Add("second-join-next-slot");

            AssertEqual(secondPlayer.playerIndex, second.UnityPlayerIndex,
                "Unity playerIndex evidence was not copied.");
            AssertEqual("PlayerSlot:qa.p3g3.player.2",
                second.Slot.PlayerSlotId.StableText,
                "Slot identity was inferred from playerIndex.");
            completed.Add("player-index-is-diagnostic-only");

            AssertTrue(fixture.Backend.ReservationObservedBeforeProvisioning,
                "Slot was not Reserved before backend provisioning.");
            completed.Add("reservation-exists-before-provisioning");

            AssertTrue(first.HasReservationEvidence && first.HasCommitEvidence &&
                !first.HasRollbackEvidence,
                "Successful result did not preserve reservation/commit evidence.");
            completed.Add("result-preserves-reservation-and-commit-evidence");
        }

        private static void RunHostEvidenceContractCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 2, true, 2);
            PlayerInput firstPlayer = CreatePlayerHost(
                created,
                "QA CPSA2 Contract Player 1",
                true);
            fixture.Backend.NextPlayerInput = firstPlayer;
            LocalPlayerJoinResult first = fixture.Join("host-evidence-contract-first");
            AssertStatus(
                first,
                LocalPlayerJoinStatus.SucceededJoined,
                "Host evidence contract first join failed.");
            AssertHostEvidenceStatus(
                fixture.RegisterHostEvidence(first),
                PlayerHostEvidenceStatus.SucceededRegistered,
                "Manager Host evidence registration failed.");
            AssertHostEvidenceStatus(
                fixture.RegisterHostEvidence(first),
                PlayerHostEvidenceStatus.SucceededAlreadyRegistered,
                "Exact Host evidence registration was not idempotent.");
            completed.Add("exact-registration-idempotent");

            AssertTrue(
                fixture.TryGetProjectedHost(
                    first.Slot.PlayerSlotId,
                    out LocalPlayerHostAuthoring confirmedHost,
                    out PlayerHostEvidenceResult confirmation) &&
                ReferenceEquals(confirmedHost, first.LocalPlayerHost),
                "Current Host evidence lookup did not return the registered Host. " +
                confirmation.ToDiagnosticString());
            completed.Add("current-evidence-confirmed");
            completed.Add("manager-preparation-uses-confirmed-evidence");

            PlayerInput conflictingPlayer = CreatePlayerHost(
                created,
                "QA CPSA2 Conflicting Host",
                true);
            LocalPlayerHostAuthoring conflictingHost =
                CommitSyntheticHostToJoin(conflictingPlayer, first);
            AssertHostEvidenceStatus(
                fixture.RegisterHostEvidence(first, conflictingHost),
                PlayerHostEvidenceStatus.RejectedHostConflict,
                "Conflicting physical Host was accepted.");
            completed.Add("conflicting-host-rejected");

            AssertHostEvidenceStatus(
                fixture.HostEvidence.RegisterHostEvidence(
                    first.Slot.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    first.AssignmentToken,
                    fixture.Context.CreateHostBindingIdentity(),
                    first.LocalPlayerHost,
                    "QA.CPSA2",
                    "conflicting-binding"),
                PlayerHostEvidenceStatus.RejectedBindingConflict,
                "Conflicting Host binding was accepted.");
            completed.Add("conflicting-binding-rejected");

            PlayerInput secondPlayer = CreatePlayerHost(
                created,
                "QA CPSA2 Contract Player 2",
                true);
            fixture.Backend.NextPlayerInput = secondPlayer;
            LocalPlayerJoinResult second = fixture.Join("host-evidence-contract-second");
            AssertStatus(
                second,
                LocalPlayerJoinStatus.SucceededJoined,
                "Host evidence contract second join failed.");
            AssertHostEvidenceStatus(
                fixture.HostEvidence.RegisterHostEvidence(
                    first.Slot.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    second.AssignmentToken,
                    second.HostBindingIdentity,
                    second.LocalPlayerHost,
                    "QA.CPSA2",
                    "other-slot-token"),
                PlayerHostEvidenceStatus.RejectedTokenSlotMismatch,
                "Assignment token from another Slot was accepted.");
            completed.Add("other-slot-token-rejected");

            using Fixture foreignFixture =
                CreateFixture(created, disposables, 1, true, 1);
            PlayerInput foreignPlayer = CreatePlayerHost(
                created,
                "QA CPSA2 Foreign Player",
                true);
            foreignFixture.Backend.NextPlayerInput = foreignPlayer;
            LocalPlayerJoinResult foreign = foreignFixture.Join(
                "host-evidence-foreign");
            AssertStatus(
                foreign,
                LocalPlayerJoinStatus.SucceededJoined,
                "Foreign Host evidence setup failed.");
            AssertHostEvidenceStatus(
                fixture.HostEvidence.RegisterHostEvidence(
                    first.Slot.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    foreign.AssignmentToken,
                    foreign.HostBindingIdentity,
                    foreign.LocalPlayerHost,
                    "QA.CPSA2",
                    "foreign-token"),
                PlayerHostEvidenceStatus.RejectedForeignAssignmentToken,
                "Foreign Session assignment token was accepted.");
            completed.Add("foreign-assignment-token-rejected");

            PlayerSlotAssignmentResult assignmentRelease =
                fixture.Context.ReleaseAssignment(
                    first.Slot.PlayerSlotId,
                    first.AssignmentToken,
                    "QA.CPSA2",
                    "make-host-evidence-stale");
            AssertTrue(
                assignmentRelease.Succeeded,
                "Host evidence stale-token setup failed.");
            AssertFalse(
                fixture.TryGetProjectedHost(
                    first.Slot.PlayerSlotId,
                    out _,
                    out PlayerHostEvidenceResult staleLookup),
                "Stale Host evidence remained usable.");
            AssertHostEvidenceStatus(
                staleLookup,
                PlayerHostEvidenceStatus.RejectedStaleAssignmentToken,
                "Stale assignment token was not diagnosed.");
            AssertTrue(
                fixture.HostEvidence.TryGetRetainedEvidence(
                    first.Slot.PlayerSlotId,
                    out PlayerHostEvidenceSnapshot retainedStale) &&
                retainedStale.AssignmentToken == first.AssignmentToken,
                "Stale lookup deleted retained Host evidence.");
            AssertFalse(
                fixture.TryGetProjectedHost(
                    first.Slot.PlayerSlotId,
                    out _,
                    out _),
                "Repeated stale lookup returned a runtime Host.");
            AssertTrue(
                fixture.HostEvidence.TryGetRetainedEvidence(
                    first.Slot.PlayerSlotId,
                    out _),
                "Repeated stale lookup deleted retained evidence.");
            completed.Add("stale-assignment-token-rejected");
            completed.Add("lookup-does-not-delete-stale-evidence");
            completed.Add("divergent-evidence-blocks-runtime-use");

            AssertHostEvidenceStatus(
                fixture.HostEvidence.ClearDivergentHostEvidence(
                    first.Slot.PlayerSlotId,
                    first.AssignmentToken,
                    first.HostBindingIdentity,
                    first.LocalPlayerHost,
                    "QA.CPSA2",
                    "explicit-divergent-release"),
                PlayerHostEvidenceStatus.SucceededClearedDivergent,
                "Explicit divergent Host evidence cleanup failed.");
            AssertFalse(
                fixture.HostEvidence.TryGetRetainedEvidence(
                    first.Slot.PlayerSlotId,
                    out _),
                "Explicit cleanup retained divergent Host evidence.");
            completed.Add("explicit-release-clears-divergent-evidence");

            RunHostMismatchRetentionCase(created, disposables, completed);
            RunDestroyedHostRetentionCase(created, disposables, completed);
        }

        private static void RunActorCorrelationCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 2, true, 2);
            PlayerInput player = CreatePlayerHost(
                created,
                "QA CPSA3 Manager Actor Player",
                true);
            fixture.Backend.NextPlayerInput = player;
            LocalPlayerJoinResult joined = fixture.Join("cpsa3-manager-join");
            AssertStatus(
                joined,
                LocalPlayerJoinStatus.SucceededJoined,
                "CPSA-3 Manager join failed.");
            AssertHostEvidenceStatus(
                fixture.RegisterHostEvidence(joined),
                PlayerHostEvidenceStatus.SucceededRegistered,
                "CPSA-3 Manager Host evidence registration failed.");

            ActorProfile actorA = CreateActorProfile(
                created,
                "QA CPSA3 Actor A",
                "qa.cpsa3.actor.a");
            ActorProfile actorB = CreateActorProfile(
                created,
                "QA CPSA3 Actor B",
                "qa.cpsa3.actor.b");
            PlayerActorSelectionResult selectionA =
                fixture.Context.TrySelectActorProfile(
                    new PlayerActorSelectionRequest(
                        joined.Slot.PlayerSlotId,
                        actorA,
                        "QA.CPSA3",
                        "select-actor-a"));
            AssertTrue(
                selectionA != null && selectionA.Succeeded,
                "CPSA-3 Actor A selection failed. " +
                selectionA?.ToDiagnosticString());

            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                "qa.cpsa3.manager.activity",
                "QA CPSA3 Manager Activity",
                RuntimeDefinitionToken.MintAnonymous());
            RuntimeScopeContext scope = fixture.CreateScope(owner);

            AssertTrue(
                fixture.Preparation.TryGetCurrentSlotActorSnapshot(
                    joined.Slot.PlayerSlotId,
                    out CurrentPlayerSlotActorSnapshot beforePreparation),
                "Aggregate read model was unavailable before Actor preparation.");
            AssertTrue(
                beforePreparation.IsAssigned &&
                beforePreparation.HasConfirmedHost &&
                !beforePreparation.HasPreparedActor &&
                beforePreparation.ActorStatus ==
                    PlayerCurrentActorEvidenceStatus.NoPreparedActor,
                "Assigned + Host confirmed + no Actor state was not represented.");
            completed.Add("assigned-host-without-prepared-actor");

            PlayerActorPreparationResult preparedA =
                fixture.Preparation.TryPrepareSelectedActor(
                    scope,
                    joined.Slot.PlayerSlotId,
                    "QA.CPSA3",
                    "prepare-actor-a");
            AssertPreparationSucceeded(
                preparedA,
                "Manager Actor A preparation failed.");
            PlayerActorPreparationSummary summaryA = preparedA.CurrentSummary;
            PlayerActorPreparationToken tokenA = summaryA.Token;
            AssertEqual(
                joined.AssignmentToken,
                summaryA.ActorEvidence.AssignmentToken,
                "Manager preparation was not correlated to the canonical assignment.");
            AssertEqual(
                joined.HostBindingIdentity,
                summaryA.ActorEvidence.HostBindingIdentity,
                "Manager preparation was not correlated to the canonical Host binding.");
            AssertEqual(
                owner,
                summaryA.ActorEvidence.Owner,
                "Manager Actor preparation owner differs from the Activity scope.");
            AssertEqual(
                PlayerActorPhysicalOwnership.FrameworkOwned,
                summaryA.ActorEvidence.PhysicalOwnership,
                "Manager Actor physical ownership is not FrameworkOwned.");
            completed.Add("manager-preparation-correlated-to-assignment");

            PlayerCurrentActorEvidenceResult exact =
                fixture.Preparation.ConfirmCurrentActorEvidence(
                    joined.Slot.PlayerSlotId,
                    tokenA,
                    "QA.CPSA3",
                    "confirm-actor-a");
            AssertActorEvidenceStatus(
                exact,
                PlayerCurrentActorEvidenceStatus.SucceededCurrent,
                "Exact Actor confirmation failed.");
            completed.Add("exact-actor-confirmation");

            AssertTrue(
                fixture.Preparation.TryGetCurrentSlotActorSnapshot(
                    joined.Slot.PlayerSlotId,
                    out CurrentPlayerSlotActorSnapshot aggregateA) &&
                aggregateA.HasCurrentActor &&
                aggregateA.ActorEvidence.PreparationToken == tokenA,
                "Aggregate Slot + Host + Actor read model did not return Actor A.");
            completed.Add("aggregate-current-slot-host-actor");

            PlayerHostBindingIdentity foreignBinding =
                new PlayerHostBindingIdentity("qa.cpsa3.foreign", 1);
            var foreignAssignment = new PlayerSlotAssignmentToken(
                "qa.cpsa3.foreign",
                tokenA.PlayerSlotId,
                1,
                1,
                foreignBinding);
            var foreignToken = new PlayerActorPreparationToken(
                "qa.cpsa3.foreign",
                tokenA.PlayerSlotId,
                foreignAssignment,
                foreignBinding,
                tokenA.ActorProfileId,
                tokenA.SelectionRevision,
                tokenA.ActorId,
                tokenA.RuntimeContentIdentity,
                tokenA.MaterializationRevision,
                tokenA.CorrelationRevision);
            AssertActorEvidenceStatus(
                fixture.Preparation.ConfirmCurrentActorEvidence(
                    joined.Slot.PlayerSlotId,
                    foreignToken,
                    "QA.CPSA3",
                    "foreign-actor-correlation"),
                PlayerCurrentActorEvidenceStatus.RejectedForeignPreparation,
                "Foreign Actor correlation token was accepted.");
            completed.Add("foreign-actor-correlation-rejected");

            PlayerSlotId otherSlot =
                fixture.Snapshot.Slots[1].PlayerSlotId;
            AssertActorEvidenceStatus(
                fixture.Preparation.ConfirmCurrentActorEvidence(
                    otherSlot,
                    tokenA,
                    "QA.CPSA3",
                    "other-slot-preparation"),
                PlayerCurrentActorEvidenceStatus.RejectedOtherSlotPreparation,
                "Actor preparation token was accepted for another Slot.");
            completed.Add("other-slot-preparation-rejected");

            PlayerSlotAssignmentToken stableAssignment = joined.AssignmentToken;
            PlayerHostBindingIdentity stableBinding = joined.HostBindingIdentity;
            PlayerActorPreparationResult releasedA =
                fixture.Preparation.TryReleasePreparedActor(
                    joined.Slot.PlayerSlotId,
                    tokenA,
                    "QA.CPSA3",
                    "release-actor-a");
            AssertPreparationSucceeded(
                releasedA,
                "Actor A release failed.");
            AssertTrue(
                fixture.TryGetCurrentAssignment(
                    joined.Slot.PlayerSlotId,
                    out PlayerSlotAssignmentSnapshot afterReleaseAssignment) &&
                afterReleaseAssignment.AssignmentToken == stableAssignment,
                "Actor release changed the canonical assignment token.");
            AssertTrue(
                fixture.TryGetProjectedHost(
                    joined.Slot.PlayerSlotId,
                    out LocalPlayerHostAuthoring afterReleaseHost,
                    out _) &&
                ReferenceEquals(afterReleaseHost, joined.LocalPlayerHost),
                "Actor release removed or replaced Host evidence.");
            AssertTrue(
                fixture.Context.TryGetActorSelection(
                    joined.Slot.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot afterReleaseSelection) &&
                afterReleaseSelection.SelectedActorProfileId ==
                    actorA.ActorProfileId &&
                afterReleaseSelection.SelectionRevision ==
                    selectionA.SelectionRevision,
                "Actor release changed the explicit Actor selection.");
            completed.Add("release-clears-actor-only");
            completed.Add("release-preserves-assignment-token");
            completed.Add("release-preserves-host-evidence");

            PlayerActorSelectionResult selectionB =
                fixture.Context.TryReplaceActorSelection(
                    new PlayerActorSelectionRequest(
                        joined.Slot.PlayerSlotId,
                        actorB,
                        "QA.CPSA3",
                        "replace-selection-with-actor-b",
                        selectionA.SelectionRevision));
            AssertTrue(
                selectionB != null && selectionB.Succeeded,
                "Actor B selection replacement failed. " +
                selectionB?.ToDiagnosticString());
            AssertTrue(
                selectionB.SelectionRevision > selectionA.SelectionRevision,
                "Actor replacement did not advance selection revision.");
            PlayerActorPreparationResult preparedB =
                fixture.Preparation.TryPrepareSelectedActor(
                    scope,
                    joined.Slot.PlayerSlotId,
                    "QA.CPSA3",
                    "prepare-actor-b");
            AssertPreparationSucceeded(
                preparedB,
                "Manager Actor B preparation failed.");
            PlayerActorPreparationToken tokenB = preparedB.CurrentSummary.Token;
            AssertEqual(
                stableAssignment,
                tokenB.AssignmentToken,
                "Actor replacement changed assignment.");
            AssertEqual(
                stableBinding,
                tokenB.HostBindingIdentity,
                "Actor replacement changed Host binding.");
            AssertTrue(
                tokenB != tokenA &&
                tokenB.SelectionRevision > tokenA.SelectionRevision &&
                tokenB.CorrelationRevision > tokenA.CorrelationRevision,
                "Actor replacement reused the preparation correlation token.");
            AssertTrue(
                tokenB.ActorId != tokenA.ActorId,
                "Actor replacement reused ActorId.");
            completed.Add("actor-replacement-preserves-assignment");
            completed.Add("actor-replacement-preserves-host-binding");
            completed.Add("actor-replacement-renews-preparation-token");
            completed.Add("actor-replacement-renews-actor-id");

            AssertActorEvidenceStatus(
                fixture.Preparation.ConfirmCurrentActorEvidence(
                    joined.Slot.PlayerSlotId,
                    tokenA,
                    "QA.CPSA3",
                    "confirm-stale-actor-a"),
                PlayerCurrentActorEvidenceStatus.RejectedPreparationStale,
                "Released Actor A token remained current after Actor B preparation.");
            PlayerActorPreparationResult staleRelease =
                fixture.Preparation.TryReleasePreparedActor(
                    joined.Slot.PlayerSlotId,
                    tokenA,
                    "QA.CPSA3",
                    "reject-stale-actor-a-release");
            AssertEqual(
                PlayerActorPreparationStatus.RejectedForeignOrStalePreparation,
                staleRelease.Status,
                "Released Actor A token was accepted for Actor B release.");
            completed.Add("stale-preparation-rejected");

            PlayerActorPreparationResult releasedB =
                fixture.Preparation.TryReleasePreparedActor(
                    joined.Slot.PlayerSlotId,
                    tokenB,
                    "QA.CPSA3",
                    "release-actor-b");
            AssertPreparationSucceeded(
                releasedB,
                "Actor B cleanup failed.");

            RunActorDivergenceCases(created, disposables, completed);
        }

        private static void RunActorDivergenceCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using (Fixture fixture =
                   CreateFixture(created, disposables, 1, true, 1))
            {
                LocalPlayerJoinResult joined = PrepareManagerActor(
                    fixture,
                    created,
                    "QA CPSA3 Assignment Divergence",
                    "qa.cpsa3.assignment.divergence",
                    out PlayerActorPreparationResult prepared);
                PlayerActorPreparationToken token = prepared.CurrentSummary.Token;
                PlayerSlotAssignmentResult assignmentRelease =
                    fixture.Context.ReleaseAssignment(
                        joined.Slot.PlayerSlotId,
                        joined.AssignmentToken,
                        "QA.CPSA3",
                        "create-assignment-divergence");
                AssertTrue(
                    assignmentRelease != null && assignmentRelease.Succeeded,
                    "Could not create assignment divergence.");
                AssertActorEvidenceStatus(
                    fixture.Preparation.ConfirmCurrentActorEvidence(
                        joined.Slot.PlayerSlotId,
                        token,
                        "QA.CPSA3",
                        "confirm-assignment-divergence"),
                    PlayerCurrentActorEvidenceStatus.RejectedAssignmentDivergence,
                    "Assignment divergence did not block Actor use.");
                AssertTrue(
                    fixture.Preparation.TryGetRetainedActorEvidence(
                        joined.Slot.PlayerSlotId,
                        out PlayerActorCorrelationEvidence retained) &&
                    retained.PreparationToken == token,
                    "Assignment divergence lookup deleted Actor evidence.");
                completed.Add("assignment-divergence-blocks-actor-use");
                completed.Add("lookup-retains-divergent-actor-evidence");

                PlayerActorPreparationResult staleRelease =
                    fixture.Preparation.TryReleasePreparedActor(
                        joined.Slot.PlayerSlotId,
                        token,
                        "QA.CPSA3",
                        "release-assignment-divergent-actor");
                AssertPreparationSucceeded(
                    staleRelease,
                    "Exact release was blocked by assignment divergence.");
            }

            using (Fixture fixture =
                   CreateFixture(created, disposables, 1, true, 1))
            {
                LocalPlayerJoinResult joined = PrepareManagerActor(
                    fixture,
                    created,
                    "QA CPSA3 Host Divergence",
                    "qa.cpsa3.host.divergence",
                    out PlayerActorPreparationResult prepared);
                PlayerActorPreparationToken token = prepared.CurrentSummary.Token;
                AssertTrue(
                    joined.LocalPlayerHost.TryReleaseCommittedAdmission(
                        joined.Slot.PlayerSlotId,
                        "QA.CPSA3",
                        "create-host-divergence",
                        out string hostIssue),
                    "Could not create Host divergence. " + hostIssue);
                AssertActorEvidenceStatus(
                    fixture.Preparation.ConfirmCurrentActorEvidence(
                        joined.Slot.PlayerSlotId,
                        token,
                        "QA.CPSA3",
                        "confirm-host-divergence"),
                    PlayerCurrentActorEvidenceStatus.RejectedHostDivergence,
                    "Host divergence did not block Actor use.");
                AssertTrue(
                    fixture.Preparation.TryGetRetainedActorEvidence(
                        joined.Slot.PlayerSlotId,
                        out _),
                    "Host divergence lookup deleted Actor evidence.");
                completed.Add("host-divergence-blocks-actor-use");

                PlayerActorPreparationResult staleRelease =
                    fixture.Preparation.TryReleasePreparedActor(
                        joined.Slot.PlayerSlotId,
                        token,
                        "QA.CPSA3",
                        "release-host-divergent-actor");
                AssertPreparationSucceeded(
                    staleRelease,
                    "Exact release was blocked by Host divergence.");
            }

            using (Fixture fixture =
                   CreateFixture(created, disposables, 1, true, 1))
            {
                LocalPlayerJoinResult joined = PrepareManagerActor(
                    fixture,
                    created,
                    "QA CPSA3 Failed Release",
                    "qa.cpsa3.failed.release",
                    out PlayerActorPreparationResult prepared);
                PlayerActorPreparationToken token = prepared.CurrentSummary.Token;
                AssertTrue(
                    fixture.RuntimeContent.TryCreateScopeContext(
                        token.RuntimeContentIdentity.Owner,
                        "QA.CPSA3",
                        "create-failed-release-scope",
                        out RuntimeScopeContext failedReleaseScope),
                    "Could not resolve failed-release Runtime Content scope.");
                RuntimeRootRegistryOperationResult unregister =
                    fixture.RuntimeContent.UnregisterHandle(
                        failedReleaseScope,
                        token.RuntimeContentIdentity,
                        "QA.CPSA3",
                        "force-release-failure");
                AssertTrue(
                    unregister != null && unregister.Applied,
                    "Could not remove Runtime Content evidence before forced release failure.");
                PlayerActorPreparationResult failedRelease =
                    fixture.Preparation.TryReleasePreparedActor(
                        joined.Slot.PlayerSlotId,
                        token,
                        "QA.CPSA3",
                        "force-release-failure");
                AssertEqual(
                    PlayerActorPreparationStatus.FailedRelease,
                    failedRelease.Status,
                    "Destroyed Actor did not retain a failed-release record.");
                AssertActorEvidenceStatus(
                    fixture.Preparation.ConfirmCurrentActorEvidence(
                        joined.Slot.PlayerSlotId,
                        token,
                        "QA.CPSA3",
                        "confirm-failed-release"),
                    PlayerCurrentActorEvidenceStatus.RejectedReleaseFailed,
                    "Failed release evidence remained usable as current Actor.");
                AssertTrue(
                    fixture.Preparation.TryGetRetainedActorEvidence(
                        joined.Slot.PlayerSlotId,
                        out PlayerActorCorrelationEvidence retained) &&
                    retained.PreparationToken == token,
                    "Failed release discarded diagnostic Actor evidence.");
                completed.Add("failed-release-retains-diagnostic-evidence");
            }
        }

        private static LocalPlayerJoinResult PrepareManagerActor(
            Fixture fixture,
            ICollection<UnityEngine.Object> created,
            string name,
            string actorProfileId,
            out PlayerActorPreparationResult prepared)
        {
            PlayerInput player = CreatePlayerHost(created, name, true);
            fixture.Backend.NextPlayerInput = player;
            LocalPlayerJoinResult joined = fixture.Join("prepare-manager-actor");
            AssertStatus(
                joined,
                LocalPlayerJoinStatus.SucceededJoined,
                "Manager Actor fixture join failed.");
            AssertHostEvidenceStatus(
                fixture.RegisterHostEvidence(joined),
                PlayerHostEvidenceStatus.SucceededRegistered,
                "Manager Actor fixture Host registration failed.");
            ActorProfile profile = CreateActorProfile(
                created,
                name + " Profile",
                actorProfileId);
            PlayerActorSelectionResult selection =
                fixture.Context.TrySelectActorProfile(
                    new PlayerActorSelectionRequest(
                        joined.Slot.PlayerSlotId,
                        profile,
                        "QA.CPSA3",
                        "select-manager-actor"));
            AssertTrue(
                selection != null && selection.Succeeded,
                "Manager Actor fixture selection failed.");
            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                actorProfileId + ".activity",
                name + " Activity",
                RuntimeDefinitionToken.MintAnonymous());
            RuntimeScopeContext scope = fixture.CreateScope(owner);
            prepared = fixture.Preparation.TryPrepareSelectedActor(
                scope,
                joined.Slot.PlayerSlotId,
                "QA.CPSA3",
                "prepare-manager-actor");
            AssertPreparationSucceeded(
                prepared,
                "Manager Actor fixture preparation failed.");
            return joined;
        }

        private static void RunLateCallbackCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 1, true, 1);
            PlayerInput player = CreatePlayerHost(created, "QA P3G3 Late Callback", true);
            fixture.Backend.NextPlayerInput = player;
            fixture.Backend.EmitCallbackBeforeReturn = false;

            LocalPlayerJoinResult result = fixture.Join("late-callback");
            AssertStatus(result, LocalPlayerJoinStatus.SucceededJoined,
                "Direct result without callback was rejected.");
            AssertEqual(LocalPlayerJoinCallbackConfirmation.Pending,
                result.CallbackConfirmation,
                "Missing callback did not remain Pending.");
            completed.Add("no-callback-admits-pending-confirmation");

            fixture.Backend.EmitJoined(player);
            AssertTrue(fixture.TryGetConfirmation(result.OperationId,
                    out LocalPlayerJoinCallbackConfirmation confirmation),
                "Late callback confirmation was not stored.");
            AssertEqual(LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput,
                confirmation,
                "Late callback did not confirm direct PlayerInput.");
            completed.Add("late-callback-confirms");

            using Fixture unexpectedFixture = CreateFixture(created, disposables, 1, true, 1);
            PlayerInput unexpectedPlayer = CreatePlayerHost(created, "QA P3G3 Unexpected", true);
            unexpectedFixture.Backend.EmitJoined(unexpectedPlayer);
            LocalPlayerJoinResult unexpected = unexpectedFixture.LastUnexpectedResult;
            AssertNotNull(unexpected, "Unexpected joined callback produced no result.");
            AssertStatus(unexpected, LocalPlayerJoinStatus.RejectedUnexpectedJoin,
                "Unexpected joined callback was accepted.");
            AssertEqual(1, unexpectedFixture.Backend.RejectCallCount,
                "Unexpected host was not rejected.");
            completed.Add("unexpected-callback-rejected");
        }

        private static void RunRollbackCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                PlayerInput player = CreatePlayerHost(
                    created,
                    "QA P3G3 Physical Registry Failure",
                    true);
                fixture.Backend.NextPlayerInput = player;
                LocalPlayerJoinResult joined =
                    fixture.Join("physical-registry-failure-setup");
                AssertStatus(
                    joined,
                    LocalPlayerJoinStatus.SucceededJoined,
                    "Manager rollback setup join failed.");
                PlayerInput conflictingPlayer = CreatePlayerHost(
                    created,
                    "QA P3G3 Conflicting Registry Host",
                    true);
                LocalPlayerHostAuthoring conflictingHost =
                    CommitSyntheticHostToJoin(conflictingPlayer, joined);
                AssertHostEvidenceStatus(
                    fixture.RegisterHostEvidence(joined, conflictingHost),
                    PlayerHostEvidenceStatus.SucceededRegistered,
                    "Manager registration-failure setup could not retain conflicting evidence.");
                AssertHostEvidenceStatus(
                    fixture.RegisterHostEvidence(joined),
                    PlayerHostEvidenceStatus.RejectedHostConflict,
                    "Manager registration unexpectedly replaced conflicting Host evidence.");
                LocalPlayerJoinResult rollback = fixture.RollbackCommittedJoin(
                    joined,
                    "synthetic-physical-registry-failure");
                AssertStatus(
                    rollback,
                    LocalPlayerJoinStatus.FailedAdmission,
                    "Physical registry failure did not execute a complete rollback.");
                AssertTrue(
                    rollback.AssignmentRollbackResult != null &&
                    rollback.AssignmentRollbackResult.Succeeded,
                    "Physical registry failure did not release the assignment.");
                AssertFalse(
                    fixture.TryGetCurrentAssignment(joined.Slot.PlayerSlotId, out _),
                    "Physical registry rollback retained a current assignment.");
                AssertFalse(
                    joined.LocalPlayerHost.HasJoinedSlot,
                    "Physical registry rollback retained Host Slot evidence.");
                AssertEqual(
                    0,
                    fixture.Snapshot.JoinedCount,
                    "Physical registry rollback retained a Joined Slot.");
                AssertEqual(
                    1,
                    fixture.Snapshot.AvailableCount,
                    "Physical registry rollback did not restore Slot availability.");
                AssertEqual(
                    0,
                    fixture.AdmittedPlayerCount,
                    "Physical registry rollback retained admitted Host evidence.");
                AssertFalse(
                    fixture.IsHostRegistered(joined.PlayerInput),
                    "Physical registry rollback retained the joined PlayerInput.");
                AssertFalse(
                    fixture.TryGetProjectedHost(
                        joined.Slot.PlayerSlotId,
                        out _,
                        out _),
                    "Manager rollback left divergent evidence active for runtime use.");
                AssertHostEvidenceStatus(
                    fixture.HostEvidence.ClearDivergentHostEvidence(
                        joined.Slot.PlayerSlotId,
                        joined.AssignmentToken,
                        joined.HostBindingIdentity,
                        conflictingHost,
                        "QA.CPSA2",
                        "clear-manager-registration-failure"),
                    PlayerHostEvidenceStatus.SucceededClearedDivergent,
                    "Manager rollback divergent evidence cleanup failed.");
                AssertEqual(
                    0,
                    fixture.HostEvidence.RetainedEvidenceCount,
                    "Manager rollback retained physical Host evidence.");
                completed.Add("manager-registration-failure-rolls-back");
                completed.Add("manager-real-rollback");
                completed.Add("manager-rollback-leaves-no-active-evidence");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                fixture.Backend.ReturnNull = true;
                LocalPlayerJoinResult result = fixture.Join("null-result");
                AssertStatus(result,
                    LocalPlayerJoinStatus.RejectedProvisioningReturnedNull,
                    "Null provisioning result was accepted.");
                AssertRollbackRestoredAvailable(fixture, result, "Null result");
                completed.Add("join-null-rolls-back");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                PlayerInput destroyed = CreatePlayerHost(created, "QA P3G3 Destroyed", true);
                fixture.Backend.NextPlayerInput = destroyed;
                fixture.Backend.DestroyBeforeReturn = true;
                LocalPlayerJoinResult result = fixture.Join("destroyed-player-input");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedMissingPlayerInput,
                    "Destroyed PlayerInput was admitted.");
                AssertRollbackRestoredAvailable(fixture, result, "Destroyed PlayerInput");
                completed.Add("missing-player-input-rolls-back");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                PlayerInput missingHost = CreatePlayerHost(created, "QA P3G3 Missing Host", false);
                fixture.Backend.NextPlayerInput = missingHost;
                LocalPlayerJoinResult result = fixture.Join("missing-host");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost,
                    "PlayerInput without LocalPlayerHostAuthoring was admitted.");
                AssertRollbackRestoredAvailable(fixture, result, "Missing Local Player Host");
                completed.Add("missing-local-player-host-rolls-back");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                PlayerInput direct = CreatePlayerHost(created, "QA P3G3 Direct", true);
                PlayerInput callback = CreatePlayerHost(created, "QA P3G3 Divergent", true);
                fixture.Backend.NextPlayerInput = direct;
                fixture.Backend.CallbackPlayerInput = callback;
                fixture.Backend.EmitCallbackBeforeReturn = true;
                LocalPlayerJoinResult result = fixture.Join("callback-mismatch");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedCorrelationMismatch,
                    "Divergent callback was accepted.");
                AssertRollbackRestoredAvailable(fixture, result, "Callback mismatch");
                AssertTrue(fixture.Backend.RejectCallCount >= 2,
                    "Divergent hosts were not rejected.");
                completed.Add("callback-mismatch-rolls-back");
            }
        }

        private static void RunPolicyRejectionCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using (Fixture fixture = CreateFixture(created, disposables, 1, false, 1))
            {
                fixture.Backend.NextPlayerInput = CreatePlayerHost(created, "QA Closed", true);
                LocalPlayerJoinResult result = fixture.Join("joining-closed");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedJoiningClosed,
                    "Closed joining reached provisioning.");
                AssertEqual(0, fixture.Backend.JoinCallCount,
                    "Backend was called while joining was closed.");
                completed.Add("joining-closed-blocks-provisioning");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 0))
            {
                fixture.Backend.NextPlayerInput = CreatePlayerHost(created, "QA Capacity", true);
                LocalPlayerJoinResult result = fixture.Join("capacity-reached");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedCapacityReached,
                    "Zero Session capacity reached provisioning.");
                completed.Add("capacity-blocks-provisioning");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                fixture.Backend.UsesManualJoin = false;
                fixture.Backend.NextPlayerInput = CreatePlayerHost(created, "QA Automatic", true);
                LocalPlayerJoinResult result = fixture.Join("manual-manager-required");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedManagerConfiguration,
                    "Non-manual backend was accepted.");
                completed.Add("manual-manager-required");
            }
        }

        private static void RunReentrantCase(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 2, true, 2);
            PlayerInput player = CreatePlayerHost(created, "QA Reentrant", true);
            fixture.Backend.NextPlayerInput = player;
            fixture.Backend.CallbackPlayerInput = player;
            fixture.Backend.EmitCallbackBeforeReturn = true;

            LocalPlayerJoinResult nested = null;
            fixture.Backend.BeforeReturn = () => nested = fixture.Join("nested-join");
            LocalPlayerJoinResult outer = fixture.Join("outer-join");

            AssertStatus(nested, LocalPlayerJoinStatus.RejectedOperationInFlight,
                "Reentrant operation was accepted.");
            AssertStatus(outer, LocalPlayerJoinStatus.SucceededJoined,
                "Outer join failed after reentrant rejection.");
            completed.Add("reentrant-operation-rejected");
        }

        private static Fixture CreateFixture(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            int slotCount,
            bool joiningOpen,
            int capacity)
        {
            var profiles = new PlayerSlotProfile[slotCount];
            for (int index = 0; index < slotCount; index++)
            {
                profiles[index] = CreateProfile(
                    created,
                    $"QA P3G3 Slot {index + 1}",
                    $"qa.p3g3.player.{index + 1}");
            }

            PlayerParticipationRuntimeContext context =
                CreateContext(profiles, capacity, joiningOpen);
            GameObject validPrefab = CreateHostObject(created,
                "QA P3G3 Backend Host Prefab", true);
            var backend = new SyntheticProvisioningBackend
            {
                IsAvailable = true,
                UsesManualJoin = true,
                PlayerPrefab = validPrefab,
                TechnicalMaxPlayerCount = Math.Max(1, slotCount)
            };

            var parentObject = new GameObject("QA P3G3 Technical Host Parent");
            created.Add(parentObject);
            LocalPlayerProvisioningBridge bridge =
                CreateBridge(context, backend, parentObject.transform);
            var fixture = new Fixture(
                context,
                bridge,
                backend,
                parentObject.transform);
            disposables.Add(fixture);
            return fixture;
        }

        private static PlayerParticipationRuntimeContext CreateContext(
            IReadOnlyList<PlayerSlotProfile> profiles,
            int capacity,
            bool joiningOpen)
        {
            PlayerParticipationOperationResult result =
                PlayerParticipationRuntimeContext.TryCreateWithActorSelectionPolicy(
                profiles,
                capacity,
                joiningOpen,
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                "QA.P3G3",
                "synthetic-context",
                out PlayerParticipationRuntimeContext context);
            AssertNotNull(result, "Context creation returned no result.");
            AssertTrue(result.Succeeded, "Context creation failed. " + result.ToDiagnosticString());
            AssertNotNull(context, "Context creation returned no context.");
            return context;
        }

        private static void RunHostMismatchRetentionCase(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 1, true, 1);
            PlayerInput player = CreatePlayerHost(
                created,
                "QA CPSA2 Host Mismatch",
                true);
            fixture.Backend.NextPlayerInput = player;
            LocalPlayerJoinResult joined = fixture.Join("host-mismatch-retention");
            AssertStatus(
                joined,
                LocalPlayerJoinStatus.SucceededJoined,
                "Host mismatch retention setup failed.");
            AssertHostEvidenceStatus(
                fixture.RegisterHostEvidence(joined),
                PlayerHostEvidenceStatus.SucceededRegistered,
                "Host mismatch retention registration failed.");
            AssertTrue(
                joined.LocalPlayerHost.TryReleaseCommittedAdmission(
                    joined.Slot.PlayerSlotId,
                    "QA.CPSA2",
                    "create-host-mismatch",
                    out string hostReleaseIssue),
                "Host mismatch setup could not release Host evidence. " +
                hostReleaseIssue);
            AssertFalse(
                fixture.TryGetProjectedHost(
                    joined.Slot.PlayerSlotId,
                    out _,
                    out PlayerHostEvidenceResult mismatch),
                "Host mismatch remained usable.");
            AssertHostEvidenceStatus(
                mismatch,
                PlayerHostEvidenceStatus.RejectedHostMismatch,
                "Host mismatch was not diagnosed.");
            AssertTrue(
                fixture.HostEvidence.TryGetRetainedEvidence(
                    joined.Slot.PlayerSlotId,
                    out _),
                "Host mismatch lookup deleted retained evidence.");
            completed.Add("lookup-does-not-delete-host-mismatch");
        }

        private static void RunDestroyedHostRetentionCase(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 1, true, 1);
            PlayerInput player = CreatePlayerHost(
                created,
                "QA CPSA2 Destroyed Host",
                true);
            fixture.Backend.NextPlayerInput = player;
            LocalPlayerJoinResult joined = fixture.Join("destroyed-host-retention");
            AssertStatus(
                joined,
                LocalPlayerJoinStatus.SucceededJoined,
                "Destroyed Host retention setup failed.");
            AssertHostEvidenceStatus(
                fixture.RegisterHostEvidence(joined),
                PlayerHostEvidenceStatus.SucceededRegistered,
                "Destroyed Host retention registration failed.");
            LocalPlayerHostAuthoring destroyedHost = joined.LocalPlayerHost;
            UnityEngine.Object.DestroyImmediate(destroyedHost.gameObject);
            AssertFalse(
                fixture.TryGetProjectedHost(
                    joined.Slot.PlayerSlotId,
                    out _,
                    out PlayerHostEvidenceResult destroyed),
                "Destroyed Host remained usable.");
            AssertHostEvidenceStatus(
                destroyed,
                PlayerHostEvidenceStatus.RejectedDestroyedHost,
                "Destroyed Unity Host was not diagnosed.");
            AssertTrue(
                fixture.HostEvidence.TryGetRetainedEvidence(
                    joined.Slot.PlayerSlotId,
                    out PlayerHostEvidenceSnapshot retained) &&
                retained.HasRetainedHostReference &&
                !retained.HostIsAvailable,
                "Destroyed Host did not retain diagnostic evidence.");
            AssertHostEvidenceStatus(
                fixture.HostEvidence.ClearDivergentHostEvidence(
                    joined.Slot.PlayerSlotId,
                    joined.AssignmentToken,
                    joined.HostBindingIdentity,
                    destroyedHost,
                    "QA.CPSA2",
                    "clear-destroyed-host"),
                PlayerHostEvidenceStatus.SucceededClearedDivergent,
                "Destroyed Host evidence could not be cleared explicitly.");
            completed.Add("destroyed-host-retains-diagnostic-evidence");
        }

        private static LocalPlayerProvisioningBridge CreateBridge(
            PlayerParticipationRuntimeContext context,
            ILocalPlayerProvisioningBackend backend,
            Transform technicalHostParent)
        {
            return new LocalPlayerProvisioningBridge(
                context,
                backend,
                technicalHostParent);
        }

        private static PlayerSlotProfile CreateProfile(
            ICollection<UnityEngine.Object> created,
            string displayName,
            string slotId)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = slotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static ActorProfile CreateActorProfile(
            ICollection<UnityEngine.Object> created,
            string displayName,
            string actorProfileId)
        {
            var actorRoot = new GameObject(displayName + " Logical Actor");
            actorRoot.SetActive(false);
            actorRoot.AddComponent<PlayerActorDeclaration>();
            created.Add(actorRoot);

            var profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue =
                actorProfileId;
            serialized.FindProperty("displayName").stringValue =
                displayName;
            serialized.FindProperty("logicalActorHostPrefab")
                .objectReferenceValue = actorRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static GameObject CreateHostObject(
            ICollection<UnityEngine.Object> created,
            string name,
            bool includeHost)
        {
            var gameObject = new GameObject(name);
            gameObject.SetActive(false);
            PlayerInput playerInput = gameObject.AddComponent<PlayerInput>();
            if (includeHost)
            {
                var mount = new GameObject("ActorMount");
                mount.transform.SetParent(gameObject.transform, false);
                LocalPlayerHostAuthoring host =
                    gameObject.AddComponent<LocalPlayerHostAuthoring>();
                var serialized = new SerializedObject(host);
                serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
                serialized.FindProperty("actorMount").objectReferenceValue = mount.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            created.Add(gameObject);
            return gameObject;
        }

        private static PlayerInput CreatePlayerHost(
            ICollection<UnityEngine.Object> created,
            string name,
            bool includeHost)
        {
            return CreateHostObject(created, name, includeHost)
                .GetComponent<PlayerInput>();
        }

        private static LocalPlayerHostAuthoring CommitSyntheticHostToJoin(
            PlayerInput playerInput,
            LocalPlayerJoinResult join)
        {
            LocalPlayerHostAuthoring host =
                playerInput.GetComponent<LocalPlayerHostAuthoring>();
            AssertNotNull(host, "Synthetic conflicting Host is missing.");
            AssertTrue(
                host.TryStageAdmission(
                    join.ReservationResult.Slot,
                    "QA.CPSA2",
                    "stage-conflicting-host",
                    out string issue),
                "Synthetic conflicting Host could not stage admission. " +
                issue);
            host.CommitStagedAdmission(
                join.Slot,
                "QA.CPSA2",
                "commit-conflicting-host");
            return host;
        }

        private static PlayerParticipationSnapshot Snapshot(
            PlayerParticipationRuntimeContext context)
        {
            return context.CreateSnapshot();
        }

        private static void AssertRollbackRestoredAvailable(
            Fixture fixture,
            LocalPlayerJoinResult result,
            string label)
        {
            AssertTrue(result.HasReservationEvidence, label + " has no reservation evidence.");
            AssertTrue(result.HasRollbackEvidence, label + " has no rollback evidence.");
            AssertTrue(result.RollbackResult.Succeeded, label + " rollback failed.");
            PlayerParticipationSnapshot snapshot = fixture.Snapshot;
            AssertEqual(0, snapshot.ReservedCount, label + " stranded a Reserved Slot.");
            AssertEqual(0, snapshot.JoinedCount, label + " admitted a Player unexpectedly.");
            AssertEqual(1, snapshot.AvailableCount, label + " did not restore Available Slot.");
            AssertEqual(0, fixture.AdmittedPlayerCount,
                label + " left a host admitted in the provisioning bridge.");
            if (result.LocalPlayerHost != null)
            {
                AssertTrue(!result.LocalPlayerHost.HasJoinedSlot,
                    label + " left public joined-host evidence.");
            }
        }

        private static void AssertStatus(
            LocalPlayerJoinResult result,
            LocalPlayerJoinStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{result.Status}' diagnostics='{result.ToDiagnosticString()}'.");
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
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

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
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

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("'", "\\'")
                    .Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class Fixture : IDisposable
        {
            private readonly PlayerParticipationRuntimeContext context;
            private readonly LocalPlayerProvisioningBridge bridge;
            private bool disposed;

            internal Fixture(
                PlayerParticipationRuntimeContext context,
                LocalPlayerProvisioningBridge bridge,
                SyntheticProvisioningBackend backend,
                Transform technicalHostParent)
            {
                this.context = context;
                this.bridge = bridge;
                HostEvidence = new PlayerHostEvidenceProjection(context);
                RuntimeContent = new RuntimeContentRuntime();
                var adapter = new AttachedPlayerActorMaterializationAdapter(
                    RuntimeContent,
                    context.CreateSnapshot().ContextId);
                AssertTrue(
                    PlayerActorPreparationRuntimeContext.TryCreate(
                        context,
                        HostEvidence,
                        adapter,
                        out PlayerActorPreparationRuntimeContext preparation,
                        out string preparationIssue),
                    "Player Actor preparation context creation failed. " +
                    preparationIssue);
                Preparation = preparation;
                Backend = backend;
                TechnicalHostParent = technicalHostParent;
                Backend.SnapshotProvider = () => Snapshot(context);
            }

            internal SyntheticProvisioningBackend Backend { get; }
            internal PlayerParticipationRuntimeContext Context => context;
            internal PlayerHostEvidenceProjection HostEvidence { get; }
            internal RuntimeContentRuntime RuntimeContent { get; }
            internal PlayerActorPreparationRuntimeContext Preparation { get; }
            internal Transform TechnicalHostParent { get; }
            internal int AdmittedPlayerCount => bridge.AdmittedPlayerCount;
            internal PlayerParticipationSnapshot Snapshot =>
                QaP3G3ProvisioningBridgeSyntheticSmoke.Snapshot(context);
            internal LocalPlayerJoinResult LastUnexpectedResult =>
                bridge.LastUnexpectedJoinResult;

            internal LocalPlayerJoinResult Join(string reason)
            {
                return bridge.TryJoin(
                    new LocalPlayerJoinRequest("QA.P3G3", reason));
            }

            internal bool TryAttachHost(
                LocalPlayerHostAuthoring host,
                out string issue)
            {
                return bridge.TryAttachHostToSessionLifetime(host, out issue);
            }

            internal bool TryGetConfirmation(
                LocalPlayerJoinOperationId operationId,
                out LocalPlayerJoinCallbackConfirmation confirmation)
            {
                return bridge.TryGetCallbackConfirmation(
                    operationId,
                    out confirmation);
            }

            internal bool TryGetCurrentAssignment(
                PlayerSlotId playerSlotId,
                out PlayerSlotAssignmentSnapshot assignment)
            {
                return context.TryGetCurrentAssignment(
                    playerSlotId,
                    out assignment);
            }

            internal LocalPlayerJoinResult RollbackCommittedJoin(
                LocalPlayerJoinResult result,
                string reason)
            {
                return bridge.RollbackCommittedJoin(result, reason);
            }

            internal bool IsHostRegistered(PlayerInput playerInput)
            {
                return bridge.IsAdmittedPlayer(playerInput);
            }

            internal PlayerHostEvidenceResult RegisterHostEvidence(
                LocalPlayerJoinResult join,
                LocalPlayerHostAuthoring host = null)
            {
                return HostEvidence.RegisterHostEvidence(
                    join.Slot.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    join.AssignmentToken,
                    join.HostBindingIdentity,
                    host ?? join.LocalPlayerHost,
                    "QA.CPSA2",
                    "register-manager-host-evidence");
            }

            internal bool TryGetProjectedHost(
                PlayerSlotId playerSlotId,
                out LocalPlayerHostAuthoring host,
                out PlayerHostEvidenceResult result)
            {
                return HostEvidence.TryGetHostEvidence(
                    playerSlotId,
                    out host,
                    out result);
            }

            internal RuntimeScopeContext CreateScope(
                RuntimeContentOwner owner)
            {
                RuntimeRootRegistryOperationResult root =
                    RuntimeContent.CreateScopeRoot(
                        owner,
                        "QA.CPSA3",
                        "create-actor-scope");
                AssertTrue(
                    root != null && !root.Rejected,
                    "Runtime Content scope root creation failed. " +
                    root?.ToDiagnosticString());
                AssertTrue(
                    RuntimeContent.TryCreateScopeContext(
                        owner,
                        "QA.CPSA3",
                        "create-actor-scope-context",
                        out RuntimeScopeContext scope),
                    "Runtime Content scope context creation failed.");
                return scope;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                bridge.Dispose();
            }
        }

        private static void AssertPreparationSucceeded(
            PlayerActorPreparationResult result,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{message} diagnostics='{result.ToDiagnosticString()}'.");
            }
        }

        private static void AssertActorEvidenceStatus(
            PlayerCurrentActorEvidenceResult result,
            PlayerCurrentActorEvidenceStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{result.Status}' " +
                    $"diagnostics='{result.ToDiagnosticString()}'.");
            }
        }

        private static void AssertHostEvidenceStatus(
            PlayerHostEvidenceResult result,
            PlayerHostEvidenceStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{result.Status}' " +
                    $"diagnostics='{result.ToDiagnosticString()}'.");
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            AssertTrue(!condition, message);
        }

        private sealed class SyntheticProvisioningBackend : ILocalPlayerProvisioningBackend
        {
            internal bool IsAvailable { get; set; }
            bool ILocalPlayerProvisioningBackend.IsAvailable => IsAvailable;
            internal bool UsesManualJoin { get; set; }
            bool ILocalPlayerProvisioningBackend.UsesManualJoin => UsesManualJoin;
            internal GameObject PlayerPrefab { get; set; }
            GameObject ILocalPlayerProvisioningBackend.PlayerPrefab => PlayerPrefab;
            internal int CurrentPlayerCount { get; set; }
            int ILocalPlayerProvisioningBackend.CurrentPlayerCount => CurrentPlayerCount;
            internal int TechnicalMaxPlayerCount { get; set; }
            int ILocalPlayerProvisioningBackend.TechnicalMaxPlayerCount => TechnicalMaxPlayerCount;
            internal PlayerInput NextPlayerInput { get; set; }
            internal PlayerInput CallbackPlayerInput { get; set; }
            internal bool EmitCallbackBeforeReturn { get; set; }
            internal bool ReturnNull { get; set; }
            internal bool DestroyBeforeReturn { get; set; }
            internal Action BeforeReturn { get; set; }
            internal Func<PlayerParticipationSnapshot> SnapshotProvider { get; set; }
            internal int JoinCallCount { get; private set; }
            internal int RejectCallCount { get; private set; }
            internal bool ReservationObservedBeforeProvisioning { get; private set; }

            public event Action<PlayerInput> PlayerJoined;

            public PlayerInput JoinPlayer(LocalPlayerJoinRequest request)
            {
                JoinCallCount++;
                PlayerParticipationSnapshot snapshot = SnapshotProvider?.Invoke();
                ReservationObservedBeforeProvisioning |= snapshot != null &&
                    snapshot.ReservedCount == 1;
                BeforeReturn?.Invoke();

                if (ReturnNull)
                {
                    return null;
                }

                PlayerInput result = NextPlayerInput;
                if (EmitCallbackBeforeReturn)
                {
                    PlayerJoined?.Invoke(CallbackPlayerInput ?? result);
                }

                if (DestroyBeforeReturn && !ReferenceEquals(result, null))
                {
                    UnityEngine.Object.DestroyImmediate(result.gameObject);
                }

                return result;
            }

            public void RejectPlayer(PlayerInput playerInput, string source, string reason)
            {
                RejectCallCount++;
            }

            internal void EmitJoined(PlayerInput playerInput)
            {
                PlayerJoined?.Invoke(playerInput);
            }
        }
    }
}

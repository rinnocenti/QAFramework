using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Runtime-only regression contract for player-independent Game Flow navigation.
    /// A case is reported only after its real runtime operation and assertions complete.
    /// </summary>
    public static class QaGameFlowPlayerIndependentNavigationRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run Player-independent Navigation Regression";
        private const string LogPrefix =
            "[QA_GAME_FLOW_PLAYER_INDEPENDENT_NAVIGATION]";
        private const int ExpectedCompletedCaseCount = 16;

        private static readonly string[] CaseNames =
        {
            "no-player-route-startup-not-required",
            "no-player-activity-start-not-required",
            "zero-slots-not-required",
            "mixed-slots-no-partial-handoff",
            "all-slots-unavailable-not-required",
            "all-slots-transferable-handoff",
            "invalid-token-fails-before-commit",
            "preparation-token-mismatch-fails-before-commit",
            "owner-mismatch-fails-before-commit",
            "committed-not-ready-keeps-destination",
            "committed-finalization-failed-keeps-destination",
            "failed-before-commit-keeps-origin",
            "invalid-activity-id-typed-failure",
            "runtime-unavailable-typed-failure",
            "loading-rejected-before-presentation",
            "host-lifecycle-authority-coherent"
        };

        private static readonly IReadOnlyDictionary<string, Func<Task>> RuntimeCases =
            new Dictionary<string, Func<Task>>
            {
                { "no-player-route-startup-not-required", RunNoPlayerRouteStartupNotRequiredAsync },
                { "no-player-activity-start-not-required", RunNoPlayerActivityStartNotRequiredAsync },
                { "zero-slots-not-required", RunZeroSlotsNotRequiredAsync },
                { "mixed-slots-no-partial-handoff", RunMixedSlotsNoPartialHandoffAsync },
                { "all-slots-unavailable-not-required", RunAllSlotsUnavailableNotRequiredAsync },
                { "all-slots-transferable-handoff", RunAllSlotsTransferableHandoffAsync },
                {
                    "invalid-token-fails-before-commit",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunInvalidTokenFailsBeforeCommitAsync
                },
                {
                    "preparation-token-mismatch-fails-before-commit",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunPreparationTokenMismatchFailsBeforeCommitAsync
                },
                {
                    "owner-mismatch-fails-before-commit",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunOwnerMismatchFailsBeforeCommitAsync
                },
                {
                    "committed-not-ready-keeps-destination",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunCommittedNotReadyKeepsDestinationAsync
                },
                {
                    "committed-finalization-failed-keeps-destination",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunCommittedFinalizationFailedKeepsDestinationAsync
                },
                {
                    "failed-before-commit-keeps-origin",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunFailedBeforeCommitKeepsOriginAsync
                },
                {
                    "invalid-activity-id-typed-failure",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunInvalidActivityIdTypedFailureAsync
                },
                {
                    "runtime-unavailable-typed-failure",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunRuntimeUnavailableTypedFailureAsync
                },
                {
                    "loading-rejected-before-presentation",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunLoadingRejectedBeforePresentationAsync
                },
                {
                    "host-lifecycle-authority-coherent",
                    QaGameFlowPlayerIndependentNavigationSupplementalCases
                        .RunHostLifecycleAuthorityCoherentAsync
                }
            };
        private static IReadOnlyList<string> lastCompleted = Array.Empty<string>();

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            try
            {
                IReadOnlyList<string> completed = await RunRegressionAsync();
                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' " +
                    $"evidence='runtime-operations-and-assertions' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' completed='{string.Join(",", lastCompleted)}'.");
                throw;
            }
        }

        internal static async Task<IReadOnlyList<string>> RunRegressionAsync()
        {
            Require(EditorApplication.isPlaying,
                "Player-independent Navigation Regression requires Play Mode.");

            var completed = new List<string>();
            lastCompleted = completed;
            ValidateRuntimeCaseRegistration();
            for (int index = 0; index < CaseNames.Length; index++)
            {
                string caseName = CaseNames[index];
                if (!RuntimeCases.TryGetValue(caseName, out Func<Task> body))
                {
                    throw CreateIncompleteCasesException(index);
                }

                await RunCaseAsync(caseName, body, completed);
            }

            Require(completed.Count == ExpectedCompletedCaseCount,
                "Player-independent Navigation Regression case count changed unexpectedly.");
            return completed;
        }

        private static async Task RunCaseAsync(
            string caseName,
            Func<Task> body,
            ICollection<string> completed)
        {
            if (string.IsNullOrWhiteSpace(caseName))
                throw new InvalidOperationException("Game Flow regression case name is required.");
            if (body == null)
                throw new InvalidOperationException(
                    $"Game Flow regression case '{caseName}' has no runtime implementation.");

            await body();
            completed.Add(caseName);
        }

        private static async Task RunNoPlayerRouteStartupNotRequiredAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            RouteAsset entryRoute = null;
            ActivityAsset entryActivity = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            const string source = nameof(QaGameFlowPlayerIndependentNavigationRegression);
            const string reason = "no-player-route-startup-not-required";
            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                entryRoute = fixture.CurrentRoute;
                entryActivity = fixture.CurrentActivity;
                Require(entryRoute != null && entryActivity != null,
                    "No-player Route Startup requires an entry Route and Activity.");
                AssertNoPlayer(fixture, "before Route Startup request");

                ActivityAsset targetActivity = fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                    "qa.player-independent.no-player.route-startup.activity",
                    "QA No Player Route Startup Activity");
                RouteAsset targetRoute = fixture.CreateRouteStartupTarget(
                    entryRoute,
                    targetActivity,
                    "qa.player-independent.no-player.route-startup.route",
                    "QA No Player Route Startup Route");
                object result = await fixture.RequestRouteAsync(
                    targetRoute,
                    source,
                    reason);
                Require(GetBoolean(result, "Succeeded"),
                    "No-player Route Startup request failed. " + GetString(result, "Message"));
                Require(ReferenceEquals(fixture.CurrentRoute, targetRoute),
                    "No-player Route Startup did not publish the target Route.");
                Require(ReferenceEquals(fixture.CurrentActivity, targetActivity),
                    "No-player Route Startup did not publish the Startup Activity.");
                ActivityPlayerLifecycleAdmissionSnapshot lifecycle = fixture.GameplaySnapshot.LifecycleAdmission;
                AssertRouteStartupNotRequired(lifecycle, entryRoute, targetRoute, source, reason);
                RouteStartupReadinessEvidence readiness = CaptureRouteStartupReadinessEvidence(
                    fixture,
                    result,
                    lifecycle);
                Debug.Log(
                    $"{LogPrefix}[CASE_EVIDENCE] case='no-player-route-startup-not-required' " +
                    $"request='{Escape(readiness.Request)}' lifecycle='{Escape(readiness.Lifecycle)}' " +
                    $"activityReadiness='{Escape(readiness.ActivityReadiness)}' " +
                    $"blockingIssues='{Escape(readiness.BlockingIssues)}' players='{fixture.PlayerCount}'.");
                Require(readiness.IsReady,
                    "No-player Route Startup Activity is not Ready. " +
                    $"request='{Escape(readiness.Request)}' lifecycle='{Escape(readiness.Lifecycle)}' " +
                    $"readinessState='{Escape(readiness.ReadinessState)}' " +
                    $"readinessReason='{Escape(readiness.ReadinessReason)}' " +
                    $"blockingIssues='{Escape(readiness.BlockingIssues)}'.");
                AssertNoPlayer(fixture, "after Route Startup request");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                cleanupFailure = await CleanupRouteCaseAsync(fixture, entryRoute, entryActivity);
            }

            ThrowCombinedFailures(executionFailure, cleanupFailure);
        }

        private static async Task RunNoPlayerActivityStartNotRequiredAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            RouteAsset entryRoute = null;
            ActivityAsset entryActivity = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            const string source = nameof(QaGameFlowPlayerIndependentNavigationRegression);
            const string reason = "no-player-activity-start-not-required";
            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                entryRoute = fixture.CurrentRoute;
                entryActivity = fixture.CurrentActivity;
                Require(entryRoute != null && entryActivity != null,
                    "No-player Activity request requires an entry Route and Activity.");
                AssertNoPlayer(fixture, "before Activity request");

                ActivityAsset targetActivity = fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                    "qa.player-independent.no-player.activity-start.activity",
                    "QA No Player Activity Start Activity");
                object result = await fixture.RequestActivityAsync(targetActivity, source, reason);
                Require(GetBoolean(result, "Succeeded"),
                    "No-player Activity request failed. " + GetString(result, "Message"));
                Require(ReferenceEquals(fixture.CurrentRoute, entryRoute),
                    "No-player Activity request changed the current Route.");
                Require(ReferenceEquals(fixture.CurrentActivity, targetActivity),
                    "No-player Activity request did not publish the target Activity.");
                AssertCurrentActivityReady(fixture);
                AssertActivityNotRequired(fixture, source, reason);
                AssertNoPlayer(fixture, "after Activity request");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                cleanupFailure = await CleanupActivityCaseAsync(fixture, entryRoute, entryActivity);
            }

            ThrowCombinedFailures(executionFailure, cleanupFailure);
        }

        private static async Task RunZeroSlotsNotRequiredAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            const string source = nameof(QaGameFlowPlayerIndependentNavigationRegression);
            const string reason = "zero-slots-not-required";
            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                RouteAsset entryRoute = fixture.CurrentRoute;
                ActivityAsset entryActivity = fixture.CurrentActivity;
                PlayerActorPreparationRuntimeHostSnapshot preparationBefore = fixture.PreparationSnapshot;
                PlayerGameplayRuntimeHostSnapshot gameplayBefore = fixture.GameplaySnapshot;
                int rootCountBefore = fixture.RuntimeScopeRootCount;
                Require(entryRoute != null && entryActivity != null && entryActivity.HasValidActivityId,
                    "Zero-Slots lifecycle classification requires a valid current Route and Activity.");
                Require(fixture.PlayerCount == 0 && fixture.JoinedSlotCount == 0,
                    "Zero-Slots lifecycle classification requires no joined Players.");
                AssertNoActiveGameplayOrPreparedActor(preparationBefore, gameplayBefore, "before zero-Slots classification");

                ActivityAsset targetActivity = fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                    "qa.player-independent.zero-slots.activity",
                    "QA Zero Slots Activity");
                ActivityPlayerLifecycleAdmissionResult result = fixture.PrepareSameRouteLifecycle(
                    entryActivity,
                    targetActivity,
                    source,
                    reason);
                Require(result != null && result.NotRequired &&
                        result.Status == ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                    "Zero-Slots lifecycle classification did not return the official NotRequired result. " +
                    result?.ToDiagnosticString());
                AssertSameRouteNotRequired(
                    fixture.LifecycleSnapshot,
                    entryActivity,
                    targetActivity,
                    source,
                    reason);
                AssertNoClassificationSideEffects(
                    fixture,
                    entryRoute,
                    entryActivity,
                    rootCountBefore,
                    preparationBefore,
                    gameplayBefore,
                    "zero-Slots classification");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                cleanupFailure = await CleanupClassificationCaseAsync(fixture, "zero-Slots");
            }

            ThrowCombinedFailures(executionFailure, cleanupFailure);
        }

        private static async Task RunMixedSlotsNoPartialHandoffAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            const string source = nameof(QaGameFlowPlayerIndependentNavigationRegression);
            const string reason = "mixed-slots-no-partial-handoff";
            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                RouteAsset entryRoute = fixture.CurrentRoute;
                ActivityAsset entryActivity = fixture.CurrentActivity;
                Require(entryRoute != null && entryActivity != null && entryActivity.HasValidActivityId,
                    "Mixed-Slots classification requires a valid current Route and Activity.");
                fixture.AssertCleanBaseline(reason);
                Require(fixture.OpenJoining(reason)?.Completed == true,
                    "Mixed-Slots classification could not open joining.");
                LocalPlayerJoinResult transferable = fixture.JoinPlayer(reason);
                QaPlayerJoinEvidence transferableEvidence = fixture.CaptureJoinEvidence(transferable);
                LocalPlayerJoinResult unavailable = fixture.JoinAdditionalPlayerSharingPrimaryDevice(reason);
                QaPlayerJoinEvidence unavailableEvidence = fixture.CaptureJoinEvidence(unavailable);
                AssertDistinctSecondaryJoin(
                    fixture,
                    transferableEvidence,
                    unavailableEvidence,
                    reason);
                Require(fixture.PlayerCount == 2 && fixture.JoinedSlotCount == 2 &&
                        transferableEvidence.SlotId != unavailableEvidence.SlotId,
                    "Mixed-Slots classification requires two distinct joined Players.");
                fixture.CreateCurrentActivityScope(source, reason);
                GameplayReadySlotEvidence transferableBefore = PrepareGameplayReadySlot(
                    fixture, transferable.Slot.PlayerSlotId, entryActivity, source, reason);
                PlayerSlotRuntimeSnapshot transferableSlotBefore =
                    fixture.GetParticipationSlot(transferable.Slot.PlayerSlotId);
                Require(transferableSlotBefore.IsJoined && transferableSlotBefore.HasSelectedActor &&
                        IsSlotPrepared(fixture, transferable.Slot.PlayerSlotId) &&
                        fixture.PreparationSnapshot.PreparedCount == 1 &&
                        fixture.GameplaySnapshot.GameplayReadyCount == 1 &&
                        fixture.GameplaySnapshot.OccupiedCount == 1 &&
                        fixture.GameplaySnapshot.CandidateCount == 0 &&
                        fixture.GameplaySnapshot.ActivePerSlotHandoffCount == 0,
                    "Mixed-Slots classification requires the transferable Slot to remain joined with its selected Actor.");
                AssertActiveGameplayChain(
                    fixture,
                    transferable.Slot.PlayerSlotId,
                    "Mixed-Slots transferable Slot before classification");
                PlayerSlotRuntimeSnapshot unavailableBefore =
                    fixture.GetParticipationSlot(unavailable.Slot.PlayerSlotId);
                Require(transferable.HasAssignmentEvidence && transferable.AssignmentToken.IsValid &&
                        transferable.HostBindingIdentity.IsValid && unavailable.HasAssignmentEvidence &&
                        unavailable.AssignmentToken.IsValid && unavailable.HostBindingIdentity.IsValid,
                    "Mixed-Slots classification requires canonical Session Player assignment and Host binding evidence for both joined Players.");
                AssertJoinedWithoutTransferableGameplayReady(
                    fixture,
                    unavailable,
                    unavailableEvidence,
                    unavailableBefore,
                    "before mixed-Slots classification");
                LogMixedSlotState(fixture, transferable.Slot.PlayerSlotId, "transferable");
                LogMixedSlotState(fixture, unavailable.Slot.PlayerSlotId, "non-transferable");

                int rootCountBefore = fixture.RuntimeScopeRootCount;
                PlayerGameplayRuntimeHostSnapshot gameplayBefore = fixture.GameplaySnapshot;
                ActivityAsset targetActivity = fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                    "qa.player-independent.mixed-slots.activity",
                    "QA Mixed Slots Activity");
                ActivityPlayerLifecycleAdmissionResult result = fixture.PrepareSameRouteLifecycle(
                    entryActivity, targetActivity, source, reason);
                Require(result != null && result.NotRequired &&
                        result.Status == ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                    "Mixed-Slots classification did not return NotRequired. " + result?.ToDiagnosticString());
                AssertSameRouteNotRequired(
                    fixture.LifecycleSnapshot, entryActivity, targetActivity, source, reason);
                Require(fixture.RuntimeScopeRootCount == rootCountBefore &&
                        fixture.GameplaySnapshot.CandidateCount == gameplayBefore.CandidateCount &&
                        fixture.GameplaySnapshot.ActivePerSlotHandoffCount == gameplayBefore.ActivePerSlotHandoffCount &&
                        !fixture.GameplaySnapshot.HasActiveHandoffGroup &&
                        ReferenceEquals(fixture.CurrentRoute, entryRoute) &&
                        ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    "Mixed-Slots classification initiated partial handoff or changed lifecycle authority.");
                AssertSlotOwnershipUnchanged(fixture, transferableBefore, "mixed transferable Slot");
                PlayerSlotRuntimeSnapshot transferableSlotAfter =
                    fixture.GetParticipationSlot(transferable.Slot.PlayerSlotId);
                Require(transferableSlotAfter.IsJoined && transferableSlotAfter.HasSelectedActor &&
                        IsSlotPrepared(fixture, transferable.Slot.PlayerSlotId),
                    "Mixed-Slots classification changed Session ownership or Actor selection for the transferable Slot.");
                AssertActiveGameplayChain(
                    fixture,
                    transferable.Slot.PlayerSlotId,
                    "Mixed-Slots transferable Slot after classification");
                PlayerSlotRuntimeSnapshot unavailableAfter =
                    fixture.GetParticipationSlot(unavailable.Slot.PlayerSlotId);
                AssertJoinedWithoutTransferableGameplayReady(
                    fixture,
                    unavailable,
                    unavailableEvidence,
                    unavailableAfter,
                    "after mixed-Slots classification");
                LogMixedSlotState(fixture, transferable.Slot.PlayerSlotId, "transferable");
                LogMixedSlotState(fixture, unavailable.Slot.PlayerSlotId, "non-transferable");
                Debug.Log(
                    $"{LogPrefix}[CASE_EVIDENCE] case='mixed-slots-no-partial-handoff' " +
                    "players='2' joined='2' sessionOwned='2' transferable='1' nonTransferable='1' " +
                    "nonTransferableJoined='True' nonTransferableSelectedActor='False' " +
                    "nonTransferablePreparationRegistered='True' nonTransferablePrepared='False' " +
                    "nonTransferableGameplayReady='False' lifecycle='NotRequired' " +
                    $"candidates='{fixture.GameplaySnapshot.CandidateCount}' " +
                    $"handoffs='{fixture.GameplaySnapshot.ActivePerSlotHandoffCount}' " +
                    $"rootsDelta='{fixture.RuntimeScopeRootCount - rootCountBefore}'.");
            }
            catch (Exception exception) { executionFailure = exception; }
            finally { cleanupFailure = await CleanupClassificationCaseAsync(fixture, "mixed-Slots"); }

            ThrowCombinedFailures(executionFailure, cleanupFailure);
        }

        private static async Task RunAllSlotsUnavailableNotRequiredAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            const string source = nameof(QaGameFlowPlayerIndependentNavigationRegression);
            const string reason = "all-slots-unavailable-not-required";
            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                RouteAsset entryRoute = fixture.CurrentRoute;
                ActivityAsset entryActivity = fixture.CurrentActivity;
                Require(entryRoute != null && entryActivity != null && entryActivity.HasValidActivityId,
                    "Unavailable-Slots lifecycle classification requires a valid current Route and Activity.");
                fixture.AssertCleanBaseline(reason);
                Debug.Log(
                    LogPrefix + "[CASE_EVIDENCE] case='all-slots-unavailable-not-required' " +
                    $"previousCaseCleanup='clean' requestedSlot='player.1' registeredHostBaseline='{fixture.BaselineRegisteredHostCount}' " +
                    $"physicalHostBaseline='{fixture.BaselinePlayerCount}'.");

                PlayerParticipationOperationResult joining = fixture.OpenJoining(reason);
                Require(joining != null && joining.Completed && joining.Snapshot.JoiningOpen,
                    "Unavailable-Slots lifecycle classification could not open joining.");
                LocalPlayerJoinResult join = fixture.JoinPlayer(reason);
                Require(join != null && join.Succeeded && fixture.PlayerCount == 1 && fixture.JoinedSlotCount == 1,
                    "Unavailable-Slots lifecycle classification did not create exactly one joined Player.");
                QaPlayerJoinEvidence joinEvidence = fixture.CaptureJoinEvidence(join);
                PlayerSlotRuntimeSnapshot slot = FindSlot(fixture.ParticipationSnapshot, fixture.JoinedSlotId);
                Require(slot.IsJoined && !slot.HasSelectedActor,
                    "Unavailable-Slots lifecycle classification requires a joined Slot without Actor selection.");
                Require(joinEvidence.PlayerInput != null && joinEvidence.Host != null &&
                        join.HasAssignmentEvidence && join.AssignmentToken.IsValid &&
                        join.HostBindingIdentity.IsValid,
                    "Unavailable-Slots lifecycle classification must preserve official Session Player ownership evidence.");

                PlayerActorPreparationRuntimeHostSnapshot preparationBefore = fixture.PreparationSnapshot;
                PlayerGameplayRuntimeHostSnapshot gameplayBefore = fixture.GameplaySnapshot;
                int rootCountBefore = fixture.RuntimeScopeRootCount;
                AssertNoActiveGameplayOrPreparedActor(preparationBefore, gameplayBefore, "before unavailable-Slots classification");
                Require(!IsGameplayReadyAdmitted(fixture, fixture.JoinedSlotId, out _),
                    "Unavailable-Slots lifecycle classification found an active GameplayReady admission before the operation.");
                AssertJoinedWithoutTransferableGameplayReady(
                    fixture, join, joinEvidence, slot, "before unavailable-Slots classification");

                ActivityAsset targetActivity = fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                    "qa.player-independent.all-unavailable.activity",
                    "QA All Slots Unavailable Activity");
                ActivityPlayerLifecycleAdmissionResult result = fixture.PrepareSameRouteLifecycle(
                    entryActivity,
                    targetActivity,
                    source,
                    reason);
                Require(result != null && result.NotRequired &&
                        result.Status == ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired,
                    "Unavailable-Slots lifecycle classification did not return the official NotRequired result. " +
                    result?.ToDiagnosticString());
                AssertSameRouteNotRequired(
                    fixture.LifecycleSnapshot,
                    entryActivity,
                    targetActivity,
                    source,
                    reason);
                Require(fixture.PlayerCount == 1 && fixture.JoinedSlotCount == 1,
                    "Unavailable-Slots lifecycle classification changed the joined Player set.");
                PlayerSlotRuntimeSnapshot afterSlot = FindSlot(fixture.ParticipationSnapshot, fixture.JoinedSlotId);
                Require(afterSlot.IsJoined && !afterSlot.HasSelectedActor,
                    "Unavailable-Slots lifecycle classification selected an Actor automatically.");
                Require(!IsGameplayReadyAdmitted(fixture, fixture.JoinedSlotId, out _),
                    "Unavailable-Slots lifecycle classification unexpectedly has an active GameplayReady admission.");
                AssertJoinedWithoutTransferableGameplayReady(
                    fixture, join, joinEvidence, afterSlot, "after unavailable-Slots classification");
                AssertNoClassificationSideEffects(
                    fixture,
                    entryRoute,
                    entryActivity,
                    rootCountBefore,
                    preparationBefore,
                    gameplayBefore,
                    "unavailable-Slots classification");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                cleanupFailure = await CleanupClassificationCaseAsync(fixture, "unavailable-Slots");
            }

            ThrowCombinedFailures(executionFailure, cleanupFailure);
        }

        private static async Task RunAllSlotsTransferableHandoffAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            const string source = nameof(QaGameFlowPlayerIndependentNavigationRegression);
            const string reason = "all-slots-transferable-handoff";
            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                RouteAsset entryRoute = fixture.CurrentRoute;
                ActivityAsset entryActivity = fixture.CurrentActivity;
                Require(entryRoute != null && entryActivity != null && entryActivity.HasValidActivityId,
                    "All-Transferable handoff requires a valid current Route and Activity.");
                fixture.AssertCleanBaseline(reason);
                TwoPlayerActorAuthoringEvidence authoring =
                    fixture.AssertTwoPlayerActorAuthoringReady(reason);
                Require(fixture.OpenJoining(reason)?.Completed == true,
                    "All-Transferable handoff could not open joining.");
                LocalPlayerJoinResult first = fixture.JoinPlayer(reason);
                QaPlayerJoinEvidence firstEvidence = fixture.CaptureJoinEvidence(first);
                LocalPlayerJoinResult second = fixture.JoinAdditionalPlayerSharingPrimaryDevice(reason);
                QaPlayerJoinEvidence secondEvidence = fixture.CaptureJoinEvidence(second);
                AssertDistinctSecondaryJoin(
                    fixture,
                    firstEvidence,
                    secondEvidence,
                    reason);
                Require(fixture.PlayerCount == 2 && fixture.JoinedSlotCount == 2,
                    "All-Transferable handoff requires two joined Players.");
                fixture.CreateCurrentActivityScope(source, reason);
                PlayerSlotRuntimeSnapshot firstBeforeSelection =
                    fixture.GetParticipationSlot(first.Slot.PlayerSlotId);
                PlayerSlotRuntimeSnapshot secondBeforeSelection =
                    fixture.GetParticipationSlot(second.Slot.PlayerSlotId);
                PlayerActorSelectionResult firstSelection = fixture.SelectDefaultActor(
                    first.Slot.PlayerSlotId, source, reason);
                PlayerActorSelectionResult secondSelection = fixture.SelectDefaultActor(
                    second.Slot.PlayerSlotId, source, reason);
                PlayerSlotRuntimeSnapshot firstAfterSelection =
                    fixture.GetParticipationSlot(first.Slot.PlayerSlotId);
                PlayerSlotRuntimeSnapshot secondAfterSelection =
                    fixture.GetParticipationSlot(second.Slot.PlayerSlotId);
                Require(firstSelection != null && firstSelection.Succeeded && firstSelection.StateChanged &&
                        secondSelection != null && secondSelection.Succeeded && secondSelection.StateChanged &&
                        firstSelection.ConflictingPlayerSlotId.IsValid == false &&
                        secondSelection.ConflictingPlayerSlotId.IsValid == false &&
                        firstAfterSelection.HasSelectedActor && secondAfterSelection.HasSelectedActor &&
                        firstAfterSelection.SelectionRevision > firstBeforeSelection.SelectionRevision &&
                        secondAfterSelection.SelectionRevision > secondBeforeSelection.SelectionRevision &&
                        fixture.ParticipationSnapshot.SelectedActorCount == 2 &&
                        firstAfterSelection.SelectedActorProfileId == authoring.FirstActorProfileId &&
                        secondAfterSelection.SelectedActorProfileId == authoring.SecondActorProfileId &&
                        firstAfterSelection.SelectedActorProfileId != secondAfterSelection.SelectedActorProfileId &&
                        fixture.ParticipationSnapshot.ActorSelectionDuplicatePolicy ==
                        PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots,
                    "All-Transferable handoff did not select two distinct Actor Profiles without a conflict.");
                Debug.Log(
                    $"{LogPrefix}[ACTOR_SELECTION] case='all-slots-transferable-handoff' selected='2' " +
                    $"firstSlot='{firstAfterSelection.PlayerSlotId.StableText}' " +
                    $"firstProfile='{firstAfterSelection.SelectedActorProfileId.StableText}' " +
                    $"secondSlot='{secondAfterSelection.PlayerSlotId.StableText}' " +
                    $"secondProfile='{secondAfterSelection.SelectedActorProfileId.StableText}' " +
                    $"profilesDistinct='True' policy='{fixture.ParticipationSnapshot.ActorSelectionDuplicatePolicy}'.");
                GameplayReadySlotEvidence firstBefore = PrepareGameplayReadySelectedSlot(
                    fixture, first.Slot.PlayerSlotId, entryActivity, source, reason);
                GameplayReadySlotEvidence secondBefore = PrepareGameplayReadySelectedSlot(
                    fixture, second.Slot.PlayerSlotId, entryActivity, source, reason);
                Require(firstBefore.Stable.PreparationToken != secondBefore.Stable.PreparationToken &&
                        firstBefore.Gameplay.AdmissionToken != secondBefore.Gameplay.AdmissionToken &&
                        firstBefore.Stable.MaterializationIdentity != secondBefore.Stable.MaterializationIdentity &&
                        fixture.ParticipationSnapshot.SelectedActorCount == 2 &&
                        fixture.PreparationSnapshot.PreparedCount == 2 &&
                        fixture.GameplaySnapshot.GameplayReadyCount == 2 &&
                        fixture.GameplaySnapshot.OccupiedCount == 2,
                    "All-Transferable handoff did not establish two distinct GameplayReady ownership chains.");

                int rootCountBefore = fixture.RuntimeScopeRootCount;
                PlayerGameplayRuntimeHostSnapshot gameplayBefore = fixture.GameplaySnapshot;
                LogRollbackBaseline(firstBefore);
                LogRollbackBaseline(secondBefore);
                Require(gameplayBefore.CandidateCount == 0 &&
                        gameplayBefore.ActivePerSlotHandoffCount == 0 &&
                        !gameplayBefore.HasActiveHandoffGroup,
                    "All-Transferable handoff requires a clean candidate/group baseline.");
                ActivityAsset targetActivity = fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                    "qa.player-independent.all-transferable.activity",
                    "QA All Slots Transferable Activity");
                ActivityPlayerLifecycleAdmissionResult preparation = fixture.PrepareSameRouteLifecycle(
                    entryActivity, targetActivity, source, reason);
                ActivityPlayerLifecycleAdmissionSnapshot lifecycle = fixture.LifecycleSnapshot;
                RuntimeContentOwner previousOwner = RuntimeContentOwner.Activity(
                    entryActivity.ActivityId.StableText, entryActivity.ActivityName);
                RuntimeContentOwner targetOwner = RuntimeContentOwner.Activity(
                    targetActivity.ActivityId.StableText, targetActivity.ActivityName);
                Require(preparation != null && !preparation.NotRequired &&
                        preparation.Status == ActivityPlayerLifecycleAdmissionStatus.SucceededReadyToCommit &&
                        preparation.ReadyForTransition && lifecycle != null &&
                        lifecycle.State == ActivityPlayerLifecycleAdmissionState.ReadyToCommit &&
                        lifecycle.LastStatus == ActivityPlayerLifecycleAdmissionStatus.SucceededReadyToCommit &&
                        lifecycle.FlowKind == ActivityPlayerLifecycleAdmissionFlowKind.SameRouteActivitySwitch &&
                        lifecycle.Token.IsValid && lifecycle.PreviousOwner == previousOwner &&
                        lifecycle.TargetOwner == targetOwner &&
                        lifecycle.RequirementLevel == PlayerParticipationRequirementLevel.GameplayReady &&
                        lifecycle.SlotCount == 2 && lifecycle.IsReadyToCommit &&
                        lifecycle.IsRollbackAvailable && !lifecycle.TransitionAuthorized &&
                        !lifecycle.TargetEnterAdopted && !lifecycle.CommitCleanupPending &&
                        lifecycle.Source == source && lifecycle.Reason == reason,
                    "All-Transferable handoff did not reach a coherent multi-Slot ReadyToCommit state. " +
                    lifecycle?.ToDiagnosticString());
                Require(fixture.GameplaySnapshot.HandoffGroup != null &&
                        fixture.GameplaySnapshot.HandoffGroup.Token.IsValid &&
                        fixture.GameplaySnapshot.HandoffGroup.IsReadyToCommit &&
                        fixture.GameplaySnapshot.HasActiveHandoffGroup &&
                        fixture.GameplaySnapshot.CandidateCount == 2 &&
                        fixture.GameplaySnapshot.ActivePerSlotHandoffCount == 2 &&
                        fixture.RuntimeScopeRootCount == rootCountBefore + 1,
                    "All-Transferable handoff did not materialize the target multi-Slot handoff evidence.");
                AssertStagedLifecycleSlot(lifecycle, firstBefore.Gameplay);
                AssertStagedLifecycleSlot(lifecycle, secondBefore.Gameplay);
                Require(ReferenceEquals(fixture.CurrentRoute, entryRoute) &&
                        ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    "All-Transferable prepare changed published Route or Activity authority.");
                Debug.Log(
                    $"{LogPrefix}[CASE_EVIDENCE] case='all-slots-transferable-handoff' " +
                    "phase='ready-to-commit' players='2' slots='2' candidates='2' handoffs='2' " +
                    $"group='ReadyToCommit' rootsDelta='{fixture.RuntimeScopeRootCount - rootCountBefore}'.");

                ActivityPlayerLifecycleAdmissionResult rollback = fixture.RollbackSameRouteLifecycle(
                    lifecycle.Token, source, "all-slots-transferable-handoff-rollback");
                Require(rollback != null &&
                        rollback.Status == ActivityPlayerLifecycleAdmissionStatus.SucceededRolledBack &&
                        rollback.CurrentSnapshot != null &&
                        rollback.CurrentSnapshot.State == ActivityPlayerLifecycleAdmissionState.RolledBack &&
                        rollback.CurrentSnapshot.LastStatus == ActivityPlayerLifecycleAdmissionStatus.SucceededRolledBack &&
                        rollback.CurrentSnapshot.Token == lifecycle.Token &&
                        !rollback.CurrentSnapshot.IsRollbackAvailable,
                    "All-Transferable handoff exact rollback failed. " + rollback?.ToDiagnosticString());
                Require(fixture.GameplaySnapshot.CandidateCount == gameplayBefore.CandidateCount &&
                        fixture.GameplaySnapshot.ActivePerSlotHandoffCount == gameplayBefore.ActivePerSlotHandoffCount &&
                        !fixture.GameplaySnapshot.HasActiveHandoffGroup &&
                        fixture.RuntimeScopeRootCount == rootCountBefore &&
                        fixture.PreparationSnapshot.PreparedCount == 2 &&
                        fixture.GameplaySnapshot.GameplayReadyCount == 2 &&
                        fixture.GameplaySnapshot.OccupiedCount == 2 &&
                        fixture.GameplaySnapshot.BoundInputCount == 2 &&
                        fixture.GameplaySnapshot.CameraEligibility?.EligibleCount == 2,
                    "All-Transferable rollback retained target handoff materialization.");
                AssertSlotOwnershipRestoredAfterRollback(
                    fixture, firstBefore.Stable, firstBefore.Gameplay,
                    "first transferable Slot after rollback");
                AssertSlotOwnershipRestoredAfterRollback(
                    fixture, secondBefore.Stable, secondBefore.Gameplay,
                    "second transferable Slot after rollback");
                Debug.Log(
                    $"{LogPrefix}[CASE_EVIDENCE] case='all-slots-transferable-handoff' " +
                    "phase='rolled-back' players='2' previousAdmissions='2' restoredAdmissions='2' " +
                    "regeneratedGameplayChains='2' candidates='0' " +
                    $"handoffs='0' group='inactive' rootsDelta='{fixture.RuntimeScopeRootCount - rootCountBefore}'.");
            }
            catch (Exception exception) { executionFailure = exception; }
            finally { cleanupFailure = await CleanupClassificationCaseAsync(fixture, "all-Transferable"); }

            ThrowCombinedFailures(executionFailure, cleanupFailure);
        }

        private static GameplayReadySlotEvidence PrepareGameplayReadySlot(
            QaPlayerGameplayAdmissionFixture fixture,
            Immersive.Framework.PlayerSlots.PlayerSlotId slotId,
            ActivityAsset entryActivity,
            string source,
            string reason)
        {
            PlayerActorSelectionResult selection = fixture.SelectDefaultActor(slotId, source, reason);
            Require(selection != null && selection.Succeeded,
                $"Slot '{slotId.StableText}' did not select its Default Actor Profile.");
            return PrepareGameplayReadySelectedSlot(fixture, slotId, entryActivity, source, reason);
        }

        private static GameplayReadySlotEvidence PrepareGameplayReadySelectedSlot(
            QaPlayerGameplayAdmissionFixture fixture,
            Immersive.Framework.PlayerSlots.PlayerSlotId slotId,
            ActivityAsset entryActivity,
            string source,
            string reason)
        {
            PlayerActorPreparationResult preparationResult = fixture.PrepareSelectedActor(slotId, source, reason);
            PlayerGameplayRuntimeOperationResult gameplayResult = fixture.EnsureGameplayReady(slotId, source, reason);
            bool hasPreparation = fixture.TryGetPreparationSummary(
                slotId,
                out PlayerActorPreparationSummary preparation);
            bool hasAdmission = fixture.TryGetGameplayAdmissionSummary(
                slotId,
                out PlayerGameplayAdmissionSummary admission);
            Require(preparationResult != null && preparationResult.Succeeded &&
                    gameplayResult != null && gameplayResult.Succeeded &&
                    hasPreparation && preparation.IsPrepared &&
                    hasAdmission && admission.GameplayReady &&
                    admission.PreparationToken == preparation.Token,
                $"Slot '{slotId.StableText}' did not reach coherent GameplayReady ownership.");
            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                entryActivity.ActivityId.StableText,
                entryActivity.ActivityName);
            Require(preparation.Materialization.Owner == owner && admission.Owner == owner,
                $"Slot '{slotId.StableText}' GameplayReady ownership does not belong to the entry Activity.");
            AssertActiveGameplayChain(fixture, slotId,
                $"Slot '{slotId.StableText}' GameplayReady ownership");
            return CaptureGameplayReadySlotEvidence(fixture, slotId, preparation, admission);
        }

        private static void AssertSlotOwnershipUnchanged(
            QaPlayerGameplayAdmissionFixture fixture,
            GameplayReadySlotEvidence expected,
            string phase)
        {
            Require(fixture.TryGetPreparationSummary(expected.Stable.SlotId, out PlayerActorPreparationSummary preparation) &&
                    preparation.IsPrepared && preparation.Token == expected.Stable.PreparationToken &&
                    preparation.Materialization.Owner == expected.Stable.Owner &&
                    preparation.Materialization.RuntimeContentIdentity.StableText == expected.Stable.MaterializationIdentity &&
                    preparation.Materialization.ActorId.StableText == expected.Stable.ActorId &&
                    preparation.SelectedActorProfileId.StableText == expected.Stable.ActorProfileId &&
                    fixture.TryGetGameplayAdmissionSummary(expected.Stable.SlotId, out PlayerGameplayAdmissionSummary admission) &&
                    admission.GameplayReady && admission.Token == expected.Gameplay.AdmissionToken &&
                    admission.PreparationToken == expected.Stable.PreparationToken && admission.Owner == expected.Stable.Owner,
                $"{phase} did not preserve exact previous GameplayReady ownership.");
            AssertActiveGameplayChain(fixture, expected.Stable.SlotId, phase);
        }

        private static void AssertJoinedWithoutTransferableGameplayReady(
            QaPlayerGameplayAdmissionFixture fixture,
            LocalPlayerJoinResult join,
            QaPlayerJoinEvidence joinEvidence,
            PlayerSlotRuntimeSnapshot slot,
            string phase)
        {
            Require(join != null && join.Succeeded && joinEvidence != null &&
                    slot.IsJoined && slot.PlayerSlotId == join.Slot.PlayerSlotId &&
                    !slot.HasSelectedActor,
                phase + " requires a Session-owned joined non-transferable Slot without Actor selection.");
            Require(joinEvidence.PlayerInput != null && joinEvidence.Host != null &&
                    join.HasAssignmentEvidence && join.AssignmentToken.IsValid &&
                    join.HostBindingIdentity.IsValid,
                phase + " lost official Session Player ownership for non-transferable Slot '" +
                join.Slot.PlayerSlotId.StableText + "'.");
            bool hasPreparationRegistration = TryGetPreparationSummary(
                fixture,
                join.Slot.PlayerSlotId,
                out PlayerActorPreparationSummary preparation);
            string preparationTokenText = preparation.Token.IsValid
                ? preparation.Token.StableText
                : "<none>";
            string materializationOwnerText = preparation.Materialization.Owner.IsValid
                ? preparation.Materialization.Owner.StableText
                : "<none>";
            string actorIdText = preparation.Materialization.ActorId.IsValid
                ? preparation.Materialization.ActorId.StableText
                : "<none>";
            string actorProfileIdText = preparation.Materialization.ActorProfileId.IsValid
                ? preparation.Materialization.ActorProfileId.StableText
                : "<none>";
            Require(hasPreparationRegistration && !preparation.IsPrepared &&
                    !preparation.Token.IsValid && !preparation.HasMaterialization &&
                    !preparation.HasActorEvidence && !preparation.Materialization.Owner.IsValid,
                "Non-transferable Slot unexpectedly has an active prepared Actor. " +
                "slot='" + join.Slot.PlayerSlotId.StableText + "' " +
                "summaryFound='" + hasPreparationRegistration + "' " +
                "isPrepared='" + preparation.IsPrepared + "' " +
                "preparationToken='" + preparationTokenText + "' " +
                "materializationOwner='" + materializationOwnerText + "' " +
                "actorId='" + actorIdText + "' " +
                "actorProfile='" + actorProfileIdText + "' " +
                "preparedCount='" + fixture.PreparationSnapshot.PreparedCount + "'.");
            Require(!IsGameplayReadyAdmitted(fixture, join.Slot.PlayerSlotId, out _),
                phase + " unexpectedly has an active GameplayReady admission for non-transferable Slot '" +
                join.Slot.PlayerSlotId.StableText + "'.");

            PlayerGameplayRuntimeHostSnapshot gameplay = fixture.GameplaySnapshot;
            Require(!IsSlotOccupied(gameplay, join.Slot.PlayerSlotId, out _),
                phase + " unexpectedly became occupied for non-transferable Slot '" +
                join.Slot.PlayerSlotId.StableText + "'.");
            Require(!IsSlotInputBound(gameplay, join.Slot.PlayerSlotId, out _),
                phase + " unexpectedly acquired an active input binding for non-transferable Slot '" +
                join.Slot.PlayerSlotId.StableText + "'.");
            Require(!IsSlotCameraEligible(gameplay, join.Slot.PlayerSlotId, out _),
                phase + " unexpectedly became camera-eligible for non-transferable Slot '" +
                join.Slot.PlayerSlotId.StableText + "'.");
            Require(gameplay == null || gameplay.Candidates == null || gameplay.Candidates.CandidateCount == 0,
                phase + " created a Player Actor candidate for non-transferable Slot '" +
                join.Slot.PlayerSlotId.StableText + "'.");
            Require(gameplay == null || gameplay.ActivePerSlotHandoffCount == 0 &&
                    !gameplay.HasActiveHandoffGroup,
                phase + " created a per-Slot handoff for non-transferable Slot '" +
                join.Slot.PlayerSlotId.StableText + "'.");
        }

        private static bool TryGetPreparationSummary(
            QaPlayerGameplayAdmissionFixture fixture,
            PlayerSlotId slotId,
            out PlayerActorPreparationSummary summary)
        {
            return fixture.TryGetPreparationSummary(slotId, out summary);
        }

        private static bool IsSlotPrepared(
            QaPlayerGameplayAdmissionFixture fixture,
            PlayerSlotId slotId)
        {
            return TryGetPreparationSummary(fixture, slotId, out PlayerActorPreparationSummary summary) &&
                   summary.IsPrepared;
        }

        private static bool TryGetGameplayAdmissionSummary(
            QaPlayerGameplayAdmissionFixture fixture,
            PlayerSlotId slotId,
            out PlayerGameplayAdmissionSummary summary)
        {
            return fixture.TryGetGameplayAdmissionSummary(slotId, out summary);
        }

        private static bool IsGameplayReadyAdmitted(
            QaPlayerGameplayAdmissionFixture fixture,
            PlayerSlotId slotId,
            out PlayerGameplayAdmissionSummary summary)
        {
            return TryGetGameplayAdmissionSummary(fixture, slotId, out summary) &&
                   summary.GameplayReady;
        }

        private static bool TryGetOccupancySummary(
            PlayerGameplayRuntimeHostSnapshot gameplay,
            PlayerSlotId slotId,
            out PlayerGameplayOccupancySummary summary)
        {
            if (gameplay?.Occupancy != null &&
                gameplay.Occupancy.TryGetSummary(slotId, out summary)) return true;
            summary = default;
            return false;
        }

        private static bool IsSlotOccupied(
            PlayerGameplayRuntimeHostSnapshot gameplay,
            PlayerSlotId slotId,
            out PlayerGameplayOccupancySummary summary)
        {
            return TryGetOccupancySummary(gameplay, slotId, out summary) &&
                   summary.IsOccupied;
        }

        private static bool TryGetInputBindingSummary(
            PlayerGameplayRuntimeHostSnapshot gameplay,
            PlayerSlotId slotId,
            out PlayerGameplayInputBindingSummary summary)
        {
            if (gameplay?.InputBinding != null &&
                gameplay.InputBinding.TryGetSummary(slotId, out summary)) return true;
            summary = default;
            return false;
        }

        private static bool IsSlotInputBound(
            PlayerGameplayRuntimeHostSnapshot gameplay,
            PlayerSlotId slotId,
            out PlayerGameplayInputBindingSummary summary)
        {
            return TryGetInputBindingSummary(gameplay, slotId, out summary) &&
                   summary.IsBound;
        }

        private static bool TryGetCameraEligibilitySummary(
            PlayerGameplayRuntimeHostSnapshot gameplay,
            PlayerSlotId slotId,
            out PlayerGameplayCameraEligibilitySummary summary)
        {
            if (gameplay?.CameraEligibility != null &&
                gameplay.CameraEligibility.TryGetSummary(slotId, out summary)) return true;
            summary = default;
            return false;
        }

        private static bool IsSlotCameraEligible(
            PlayerGameplayRuntimeHostSnapshot gameplay,
            PlayerSlotId slotId,
            out PlayerGameplayCameraEligibilitySummary summary)
        {
            return TryGetCameraEligibilitySummary(gameplay, slotId, out summary) &&
                   summary.IsEligible;
        }

        private static void AssertActiveGameplayChain(
            QaPlayerGameplayAdmissionFixture fixture,
            PlayerSlotId slotId,
            string phase)
        {
            PlayerGameplayRuntimeHostSnapshot gameplay = fixture.GameplaySnapshot;
            Require(IsSlotOccupied(gameplay, slotId, out _),
                phase + " is missing active Gameplay occupancy.");
            Require(IsSlotInputBound(gameplay, slotId, out _),
                phase + " is missing an active Gameplay input binding.");
            Require(TryGetCameraEligibilitySummary(gameplay, slotId,
                        out PlayerGameplayCameraEligibilitySummary camera) &&
                    (camera.IsEligible || camera.IsSkippedOptional),
                phase + " has no camera state allowed by the official Gameplay policy.");
        }

        private static void LogMixedSlotState(
            QaPlayerGameplayAdmissionFixture fixture,
            PlayerSlotId slotId,
            string role)
        {
            PlayerSlotRuntimeSnapshot participation = fixture.GetParticipationSlot(slotId);
            bool preparationRegistered = TryGetPreparationSummary(
                fixture, slotId, out PlayerActorPreparationSummary preparation);
            bool admissionRegistered = TryGetGameplayAdmissionSummary(
                fixture, slotId, out PlayerGameplayAdmissionSummary admission);
            PlayerGameplayRuntimeHostSnapshot gameplay = fixture.GameplaySnapshot;
            bool occupancyRegistered = TryGetOccupancySummary(
                gameplay, slotId, out PlayerGameplayOccupancySummary occupancy);
            bool inputRegistered = TryGetInputBindingSummary(
                gameplay, slotId, out PlayerGameplayInputBindingSummary input);
            bool cameraRegistered = TryGetCameraEligibilitySummary(
                gameplay, slotId, out PlayerGameplayCameraEligibilitySummary camera);
            Debug.Log(
                LogPrefix + "[SLOT_STATE] " +
                "case='mixed-slots-no-partial-handoff' " +
                "slot='" + slotId.StableText + "' " +
                "role='" + role + "' " +
                "joined='" + participation.IsJoined + "' " +
                "selectedActor='" + participation.HasSelectedActor + "' " +
                "preparationRegistered='" + preparationRegistered + "' " +
                "prepared='" + (preparationRegistered && preparation.IsPrepared) + "' " +
                "admissionRegistered='" + admissionRegistered + "' " +
                "gameplayReady='" + (admissionRegistered && admission.GameplayReady) + "' " +
                "occupancyRegistered='" + occupancyRegistered + "' " +
                "occupied='" + (occupancyRegistered && occupancy.IsOccupied) + "' " +
                "inputRegistered='" + inputRegistered + "' " +
                "inputBound='" + (inputRegistered && input.IsBound) + "' " +
                "cameraRegistered='" + cameraRegistered + "' " +
                "cameraEligible='" + (cameraRegistered && camera.IsEligible) + "' " +
                "candidateActive='" + (gameplay?.CandidateCount > 0) + "' " +
                "handoffActive='" + (gameplay?.ActivePerSlotHandoffCount > 0) + "'.");
        }

        private static void AssertStagedLifecycleSlot(
            ActivityPlayerLifecycleAdmissionSnapshot lifecycle,
            GameplayChainTokenEvidence expected)
        {
            int matching = 0;
            for (int index = 0; index < lifecycle.Slots.Count; index++)
            {
                ActivityPlayerLifecycleAdmissionSlotSnapshot slot = lifecycle.Slots[index];
                if (slot.PlayerSlotId != expected.SlotId) continue;
                matching++;
                Require(slot.PreviousAdmissionToken == expected.AdmissionToken &&
                        slot.CandidateToken.IsValid && slot.TargetPreparationToken.IsValid &&
                        slot.TargetAdmissionToken.IsValid && slot.Staged && slot.GroupBegan &&
                        !slot.Committed && !slot.Adopted && !slot.Released,
                    $"ReadyToCommit evidence for Slot '{expected.SlotId.StableText}' is incomplete.");
            }

            Require(matching == 1,
                $"ReadyToCommit lifecycle evidence must contain Slot '{expected.SlotId.StableText}' exactly once.");
        }

        private static GameplayReadySlotEvidence CaptureGameplayReadySlotEvidence(
            QaPlayerGameplayAdmissionFixture fixture,
            PlayerSlotId slotId,
            PlayerActorPreparationSummary preparation,
            PlayerGameplayAdmissionSummary admission)
        {
            PlayerGameplayRuntimeHostSnapshot gameplay = fixture.GameplaySnapshot;
            bool hasOccupancy = TryGetOccupancySummary(
                gameplay, slotId, out PlayerGameplayOccupancySummary occupancy);
            bool hasInput = TryGetInputBindingSummary(
                gameplay, slotId, out PlayerGameplayInputBindingSummary input);
            bool hasCamera = TryGetCameraEligibilitySummary(
                gameplay, slotId, out PlayerGameplayCameraEligibilitySummary camera);
            Require(hasOccupancy && hasInput && hasCamera,
                $"Slot '{slotId.StableText}' is missing one or more Gameplay chain summaries.");
            return new GameplayReadySlotEvidence(
                new StableActorOwnershipEvidence(
                    slotId,
                    preparation.Token,
                    preparation.Materialization.Owner,
                    preparation.Materialization.RuntimeContentIdentity.StableText,
                    preparation.Materialization.ActorId.StableText,
                    preparation.SelectedActorProfileId.StableText,
                    input.AssignmentToken,
                    input.HostBindingIdentity),
                new GameplayChainTokenEvidence(
                    slotId,
                    admission.Token,
                    occupancy.Token,
                    input.Token,
                    camera.Token,
                    admission.PreparationToken,
                    admission.Owner));
        }

        private static void LogRollbackBaseline(GameplayReadySlotEvidence evidence)
        {
            Debug.Log(
                $"{LogPrefix}[ROLLBACK_BASELINE] case='all-slots-transferable-handoff' " +
                $"slot='{evidence.Stable.SlotId.StableText}' preparation='{evidence.Stable.PreparationToken.StableText}' " +
                $"admission='{evidence.Gameplay.AdmissionToken.StableText}' occupancy='{evidence.Gameplay.OccupancyToken.StableText}' " +
                $"input='{evidence.Gameplay.InputBindingToken.StableText}' camera='{evidence.Gameplay.CameraEligibilityToken.StableText}' " +
                $"owner='{evidence.Stable.Owner.StableText}' materialization='{evidence.Stable.MaterializationIdentity}'.");
        }

        private static void AssertSlotOwnershipRestoredAfterRollback(
            QaPlayerGameplayAdmissionFixture fixture,
            StableActorOwnershipEvidence stableExpected,
            GameplayChainTokenEvidence previousGameplay,
            string phase)
        {
            PlayerSlotRuntimeSnapshot slot = fixture.GetParticipationSlot(stableExpected.SlotId);
            bool hasPreparation = fixture.TryGetPreparationSummary(
                stableExpected.SlotId, out PlayerActorPreparationSummary preparation);
            bool hasAdmission = fixture.TryGetGameplayAdmissionSummary(
                stableExpected.SlotId, out PlayerGameplayAdmissionSummary admission);
            PlayerGameplayRuntimeHostSnapshot gameplay = fixture.GameplaySnapshot;
            bool hasOccupancy = TryGetOccupancySummary(gameplay, stableExpected.SlotId,
                out PlayerGameplayOccupancySummary occupancy);
            bool hasInput = TryGetInputBindingSummary(gameplay, stableExpected.SlotId,
                out PlayerGameplayInputBindingSummary input);
            bool hasCamera = TryGetCameraEligibilitySummary(gameplay, stableExpected.SlotId,
                out PlayerGameplayCameraEligibilitySummary camera);

            bool stablePreparationPreserved = hasPreparation && preparation.IsPrepared &&
                preparation.Token == stableExpected.PreparationToken;
            bool stableOwnerPreserved = hasPreparation &&
                preparation.Materialization.Owner == stableExpected.Owner;
            bool stableMaterializationPreserved = hasPreparation &&
                preparation.Materialization.RuntimeContentIdentity.StableText == stableExpected.MaterializationIdentity;
            bool stableActorPreserved = hasPreparation &&
                preparation.Materialization.ActorId.StableText == stableExpected.ActorId;
            bool stableProfilePreserved = slot.IsJoined && slot.HasSelectedActor && hasPreparation &&
                preparation.SelectedActorProfileId.StableText == stableExpected.ActorProfileId &&
                slot.SelectedActorProfileId.StableText == stableExpected.ActorProfileId;
            bool admissionRegenerated = hasAdmission && admission.Token.IsValid &&
                admission.Token != previousGameplay.AdmissionToken;
            bool occupancyRegenerated = hasOccupancy && occupancy.Token.IsValid &&
                occupancy.Token != previousGameplay.OccupancyToken;
            bool inputRegenerated = hasInput && input.Token.IsValid &&
                input.Token != previousGameplay.InputBindingToken;
            bool cameraRegenerated = hasCamera && camera.Token.IsValid &&
                camera.Token != previousGameplay.CameraEligibilityToken;
            bool gameplayReady = hasAdmission && admission.GameplayReady;
            bool occupied = hasOccupancy && occupancy.IsOccupied;
            bool inputBound = hasInput && input.IsBound;
            bool cameraEligible = hasCamera && camera.IsEligible;
            bool correlationValid = gameplayReady && occupied && inputBound && cameraEligible &&
                admission.PreparationToken == stableExpected.PreparationToken &&
                admission.Owner == stableExpected.Owner &&
                occupancy.PreparationToken == stableExpected.PreparationToken &&
                input.PreparationToken == stableExpected.PreparationToken &&
                camera.PreparationToken == stableExpected.PreparationToken &&
                admission.OccupancyToken == occupancy.Token &&
                admission.InputBindingToken == input.Token &&
                admission.CameraEligibilityToken == camera.Token &&
                occupancy.Owner == stableExpected.Owner && input.Owner == stableExpected.Owner &&
                camera.Owner == stableExpected.Owner &&
                input.AssignmentToken == stableExpected.AssignmentToken &&
                input.HostBindingIdentity == stableExpected.HostBindingIdentity;
            Debug.Log(
                $"{LogPrefix}[ROLLBACK_RESTORATION] case='all-slots-transferable-handoff' " +
                $"slot='{stableExpected.SlotId.StableText}' stablePreparationPreserved='{stablePreparationPreserved}' " +
                $"stableOwnerPreserved='{stableOwnerPreserved}' stableMaterializationPreserved='{stableMaterializationPreserved}' " +
                $"stableActorPreserved='{stableActorPreserved}' stableProfilePreserved='{stableProfilePreserved}' " +
                $"admissionRegenerated='{admissionRegenerated}' occupancyRegenerated='{occupancyRegenerated}' " +
                $"inputRegenerated='{inputRegenerated}' cameraRegenerated='{cameraRegenerated}' " +
                $"gameplayReady='{gameplayReady}' occupied='{occupied}' inputBound='{inputBound}' cameraEligible='{cameraEligible}' " +
                $"previousAdmission='{previousGameplay.AdmissionToken.StableText}' currentAdmission='{admission.Token.StableText}' " +
                $"previousOccupancy='{previousGameplay.OccupancyToken.StableText}' currentOccupancy='{occupancy.Token.StableText}' " +
                $"previousInput='{previousGameplay.InputBindingToken.StableText}' currentInput='{input.Token.StableText}' " +
                $"previousCamera='{previousGameplay.CameraEligibilityToken.StableText}' currentCamera='{camera.Token.StableText}'.");
            Require(stablePreparationPreserved && stableOwnerPreserved && stableMaterializationPreserved &&
                    stableActorPreserved && stableProfilePreserved && admissionRegenerated &&
                    occupancyRegenerated && inputRegenerated && cameraRegenerated && gameplayReady &&
                    occupied && inputBound && cameraEligible && correlationValid,
                $"{phase} failed semantic Gameplay chain restoration. " +
                $"stablePreparation='{stablePreparationPreserved}' stableOwner='{stableOwnerPreserved}' " +
                $"stableMaterialization='{stableMaterializationPreserved}' stableActor='{stableActorPreserved}' " +
                $"stableProfile='{stableProfilePreserved}' admissionRegenerated='{admissionRegenerated}' " +
                $"occupancyRegenerated='{occupancyRegenerated}' inputRegenerated='{inputRegenerated}' " +
                $"cameraRegenerated='{cameraRegenerated}' gameplayReady='{gameplayReady}' occupied='{occupied}' " +
                $"inputBound='{inputBound}' cameraEligible='{cameraEligible}' correlation='{correlationValid}'.");
        }

        private readonly struct GameplayReadySlotEvidence
        {
            public GameplayReadySlotEvidence(
                StableActorOwnershipEvidence stable,
                GameplayChainTokenEvidence gameplay)
            {
                Stable = stable;
                Gameplay = gameplay;
            }

            public StableActorOwnershipEvidence Stable { get; }
            public GameplayChainTokenEvidence Gameplay { get; }
        }

        private readonly struct StableActorOwnershipEvidence
        {
            public StableActorOwnershipEvidence(
                Immersive.Framework.PlayerSlots.PlayerSlotId slotId,
                PlayerActorPreparationToken preparationToken,
                RuntimeContentOwner owner,
                string materializationIdentity,
                string actorId,
                string actorProfileId,
                PlayerSlotAssignmentToken assignmentToken,
                PlayerHostBindingIdentity hostBindingIdentity)
            {
                SlotId = slotId;
                PreparationToken = preparationToken;
                Owner = owner;
                MaterializationIdentity = materializationIdentity;
                ActorId = actorId;
                ActorProfileId = actorProfileId;
                AssignmentToken = assignmentToken;
                HostBindingIdentity = hostBindingIdentity;
            }

            public Immersive.Framework.PlayerSlots.PlayerSlotId SlotId { get; }
            public PlayerActorPreparationToken PreparationToken { get; }
            public RuntimeContentOwner Owner { get; }
            public string MaterializationIdentity { get; }
            public string ActorId { get; }
            public string ActorProfileId { get; }
            public PlayerSlotAssignmentToken AssignmentToken { get; }
            public PlayerHostBindingIdentity HostBindingIdentity { get; }
        }

        private readonly struct GameplayChainTokenEvidence
        {
            public GameplayChainTokenEvidence(
                PlayerSlotId slotId,
                PlayerGameplayAdmissionToken admissionToken,
                PlayerGameplayOccupancyToken occupancyToken,
                PlayerGameplayInputBindingToken inputBindingToken,
                PlayerGameplayCameraEligibilityToken cameraEligibilityToken,
                PlayerActorPreparationToken preparationToken,
                RuntimeContentOwner owner)
            {
                SlotId = slotId;
                AdmissionToken = admissionToken;
                OccupancyToken = occupancyToken;
                InputBindingToken = inputBindingToken;
                CameraEligibilityToken = cameraEligibilityToken;
                PreparationToken = preparationToken;
                Owner = owner;
            }

            public PlayerSlotId SlotId { get; }
            public PlayerGameplayAdmissionToken AdmissionToken { get; }
            public PlayerGameplayOccupancyToken OccupancyToken { get; }
            public PlayerGameplayInputBindingToken InputBindingToken { get; }
            public PlayerGameplayCameraEligibilityToken CameraEligibilityToken { get; }
            public PlayerActorPreparationToken PreparationToken { get; }
            public RuntimeContentOwner Owner { get; }
        }

        private static async Task<Exception> CleanupClassificationCaseAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            string caseLabel)
        {
            if (fixture == null) return null;
            try
            {
                await fixture.CleanupAsync();
                Require(fixture.CleanupFailure == null,
                    $"{caseLabel} fixture cleanup failed. {fixture.CleanupFailure?.Message}");
                Require(fixture.PlayerCount == 0 && fixture.JoinedSlotCount == 0 &&
                        fixture.JoinedPlayers.Count == 0,
                    $"{caseLabel} fixture cleanup retained joined Player state.");
                AssertNoActiveGameplayOrPreparedActor(
                    fixture.PreparationSnapshot,
                    fixture.GameplaySnapshot,
                    $"after {caseLabel} cleanup");
                return null;
            }
            catch (Exception exception) { return exception; }
        }

        private static void AssertSameRouteNotRequired(
            ActivityPlayerLifecycleAdmissionSnapshot snapshot,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            RuntimeContentOwner previousOwner = RuntimeContentOwner.Activity(
                previousActivity.ActivityId.StableText,
                previousActivity.ActivityName);
            RuntimeContentOwner targetOwner = RuntimeContentOwner.Activity(
                targetActivity.ActivityId.StableText,
                targetActivity.ActivityName);
            Require(snapshot != null &&
                    snapshot.State == ActivityPlayerLifecycleAdmissionState.NotRequired &&
                    snapshot.LastStatus == ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired &&
                    snapshot.FlowKind == ActivityPlayerLifecycleAdmissionFlowKind.SameRouteActivitySwitch &&
                    snapshot.Token.IsValid &&
                    snapshot.PreviousActivityName == previousActivity.ActivityName &&
                    snapshot.TargetActivityName == targetActivity.ActivityName &&
                    snapshot.PreviousOwner == previousOwner && snapshot.TargetOwner == targetOwner &&
                    snapshot.RequirementLevel == PlayerParticipationRequirementLevel.GameplayReady &&
                    snapshot.SlotCount == 0 && !snapshot.IsReadyToCommit &&
                    !snapshot.TransitionAuthorized && !snapshot.TargetEnterAdopted &&
                    snapshot.Source == source && snapshot.Reason == reason,
                "Same-Route lifecycle snapshot is not the authoritative NotRequired decision. " +
                snapshot?.ToDiagnosticString());
        }

        private static void AssertNoClassificationSideEffects(
            QaPlayerGameplayAdmissionFixture fixture,
            RouteAsset entryRoute,
            ActivityAsset entryActivity,
            int rootCountBefore,
            PlayerActorPreparationRuntimeHostSnapshot preparationBefore,
            PlayerGameplayRuntimeHostSnapshot gameplayBefore,
            string phase)
        {
            Require(ReferenceEquals(fixture.CurrentRoute, entryRoute) &&
                    ReferenceEquals(fixture.CurrentActivity, entryActivity),
                $"{phase} changed Route or Activity authority.");
            Require(fixture.RuntimeScopeRootCount == rootCountBefore,
                $"{phase} created a target RuntimeContent scope root.");
            AssertNoActiveGameplayOrPreparedActor(fixture.PreparationSnapshot, fixture.GameplaySnapshot, phase);
            Require(preparationBefore.PreparedCount == fixture.PreparationSnapshot.PreparedCount &&
                    gameplayBefore.GameplayReadyCount == fixture.GameplaySnapshot.GameplayReadyCount &&
                    gameplayBefore.OccupiedCount == fixture.GameplaySnapshot.OccupiedCount &&
                    gameplayBefore.BoundInputCount == fixture.GameplaySnapshot.BoundInputCount &&
                    gameplayBefore.CameraDecisionCount == fixture.GameplaySnapshot.CameraDecisionCount &&
                    gameplayBefore.CandidateCount == fixture.GameplaySnapshot.CandidateCount &&
                    gameplayBefore.ActivePerSlotHandoffCount == fixture.GameplaySnapshot.ActivePerSlotHandoffCount,
                $"{phase} changed Player preparation, Gameplay, candidate or handoff evidence.");
        }

        private static void AssertNoActiveGameplayOrPreparedActor(
            PlayerActorPreparationRuntimeHostSnapshot preparation,
            PlayerGameplayRuntimeHostSnapshot gameplay,
            string phase)
        {
            Require(preparation != null && gameplay != null &&
                    preparation.PreparedCount == 0 && gameplay.GameplayReadyCount == 0 &&
                    gameplay.OccupiedCount == 0 && gameplay.BoundInputCount == 0 &&
                    (gameplay.CameraEligibility?.EligibleCount ?? 0) == 0 &&
                    gameplay.CandidateCount == 0 &&
                    gameplay.ActivePerSlotHandoffCount == 0 && !gameplay.HasActiveHandoffGroup,
                $"{phase} retained preparation, Gameplay, candidate or handoff state. " +
                $"preparation='{preparation?.Diagnostic}' gameplay='{gameplay?.ToDiagnosticString()}'.");
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot participation,
            Immersive.Framework.PlayerSlots.PlayerSlotId slotId)
        {
            if (participation != null)
            {
                for (int index = 0; index < participation.Slots.Count; index++)
                {
                    PlayerSlotRuntimeSnapshot slot = participation.Slots[index];
                    if (slot.PlayerSlotId == slotId) return slot;
                }
            }

            throw new InvalidOperationException(
                $"Joined Player Slot '{slotId.StableText}' was not found in the official participation snapshot.");
        }

        private static async Task<Exception> CleanupRouteCaseAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            RouteAsset entryRoute,
            ActivityAsset entryActivity)
        {
            if (fixture == null) return null;
            try
            {
                if (fixture.CurrentActivity != null)
                    await fixture.ClearActivityAsync(
                        nameof(QaGameFlowPlayerIndependentNavigationRegression),
                        "cleanup-no-player-route-startup-activity");
                if (entryRoute != null && !ReferenceEquals(fixture.CurrentRoute, entryRoute))
                    await fixture.RequestRouteAsync(
                        entryRoute,
                        nameof(QaGameFlowPlayerIndependentNavigationRegression),
                        "cleanup-no-player-route-startup-route");
                Require(ReferenceEquals(fixture.CurrentRoute, entryRoute),
                    "No-player Route Startup cleanup did not restore the entry Route.");
                Require(ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    "No-player Route Startup cleanup did not restore the entry Activity.");
                await fixture.CleanupAsync();
                Require(fixture.CleanupFailure == null,
                    "No-player Route Startup fixture cleanup failed. " + fixture.CleanupFailure?.Message);
                AssertNoPlayer(fixture, "after Route Startup cleanup");
                return null;
            }
            catch (Exception exception) { return exception; }
        }

        private static async Task<Exception> CleanupActivityCaseAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            RouteAsset entryRoute,
            ActivityAsset entryActivity)
        {
            if (fixture == null) return null;
            try
            {
                if (fixture.CurrentActivity != null && !ReferenceEquals(fixture.CurrentActivity, entryActivity))
                    await fixture.ClearActivityAsync(
                        nameof(QaGameFlowPlayerIndependentNavigationRegression),
                        "cleanup-no-player-activity-start-clear");
                if (entryActivity != null && !ReferenceEquals(fixture.CurrentActivity, entryActivity))
                    await fixture.RequestActivityAsync(
                        entryActivity,
                        nameof(QaGameFlowPlayerIndependentNavigationRegression),
                        "cleanup-no-player-activity-start-restore");
                Require(ReferenceEquals(fixture.CurrentRoute, entryRoute),
                    "No-player Activity cleanup changed the entry Route.");
                Require(ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    "No-player Activity cleanup did not restore the entry Activity.");
                await fixture.CleanupAsync();
                Require(fixture.CleanupFailure == null,
                    "No-player Activity fixture cleanup failed. " + fixture.CleanupFailure?.Message);
                AssertNoPlayer(fixture, "after Activity cleanup");
                return null;
            }
            catch (Exception exception) { return exception; }
        }

        private static void AssertRouteStartupNotRequired(
            ActivityPlayerLifecycleAdmissionSnapshot snapshot,
            RouteAsset previousRoute,
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            Require(snapshot != null && snapshot.State == ActivityPlayerLifecycleAdmissionState.NotRequired &&
                    snapshot.LastStatus == ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired &&
                    snapshot.FlowKind == ActivityPlayerLifecycleAdmissionFlowKind.RouteStartupActivitySwitch &&
                    snapshot.Token.IsValid && snapshot.Token.PreviousRouteId == previousRoute.RouteId &&
                    snapshot.Token.TargetRouteId == targetRoute.RouteId && snapshot.SlotCount == 0 &&
                    !snapshot.TransitionAuthorized && !snapshot.TargetEnterAdopted &&
                    snapshot.Source == source && snapshot.Reason == reason,
                "No-player Route Startup lifecycle snapshot is not terminal NotRequired. " + snapshot?.ToDiagnosticString());
        }

        private static void AssertActivityNotRequired(
            QaPlayerGameplayAdmissionFixture fixture,
            string source,
            string reason)
        {
            ActivityPlayerLifecycleAdmissionSnapshot snapshot = fixture.GameplaySnapshot.LifecycleAdmission;
            Require(snapshot != null && snapshot.State == ActivityPlayerLifecycleAdmissionState.NotRequired &&
                    snapshot.LastStatus == ActivityPlayerLifecycleAdmissionStatus.SucceededNotRequired &&
                    snapshot.SlotCount == 0 && snapshot.Source == source && snapshot.Reason == reason,
                "No-player Activity lifecycle snapshot is not terminal NotRequired. " + snapshot?.ToDiagnosticString());
        }

        private static void AssertCurrentActivityReady(QaPlayerGameplayAdmissionFixture fixture)
        {
            object state = Get(fixture.RuntimeHost, "State");
            Require(GetBoolean(state, "IsActivityReady"),
                "Current Activity is not Ready. " + Get(state, "ActivityReadinessState"));
        }

        private static RouteStartupReadinessEvidence CaptureRouteStartupReadinessEvidence(
            QaPlayerGameplayAdmissionFixture fixture,
            object routeRequestResult,
            ActivityPlayerLifecycleAdmissionSnapshot lifecycle)
        {
            object runtimeState = Get(fixture.RuntimeHost, "State");
            object readiness = Get(runtimeState, "ActivityReadinessState");
            object activityState = Get(runtimeState, "ActivityState");
            object activityFlow = Get(runtimeState, "ActivityFlowResult");
            object contentExecution = Get(activityFlow, "ActivityContentExecutionResult");
            object contentLifecycle = Get(readiness, "ActivityContentLifecycleResult");

            string readinessState = GetString(readiness, "DiagnosticStatus");
            string readinessReason = GetString(readiness, "DiagnosticReason");
            string blockingIssues =
                $"count='{Get(readiness, "BlockingIssueCount")}' " +
                $"reason='{readinessReason}' " +
                $"activityContentLifecycle='{DescribeDiagnostic(contentLifecycle)}' " +
                $"activityContentExecution='{DescribeDiagnostic(contentExecution)}' " +
                $"participantSource='{DescribeDiagnostic(Get(contentExecution, "ParticipantSourceResult"))}' " +
                $"participants='{DescribeDiagnostic(Get(contentExecution, "Participants"))}' " +
                $"enterResults='{DescribeDiagnostic(Get(contentExecution, "EnterResult"))}' " +
                $"exitResults='{DescribeDiagnostic(Get(contentExecution, "ExitResult"))}'";
            string activityReadiness =
                $"status='{readinessState}' reason='{readinessReason}' " +
                $"activityState='{GetString(activityState, "DiagnosticStatus")}' " +
                $"readiness='{DescribeDiagnostic(readiness)}'";
            string request =
                $"kind='{Get(routeRequestResult, "Kind")}' " +
                $"message='{GetString(routeRequestResult, "Message")}' " +
                $"source='{GetString(routeRequestResult, "Source")}' " +
                $"reason='{GetString(routeRequestResult, "Reason")}' " +
                $"routeLifecycle='{DescribeDiagnostic(Get(routeRequestResult, "RouteLifecycleResult"))}' " +
                $"transition='{DescribeDiagnostic(Get(routeRequestResult, "TransitionDiagnostics"))}' " +
                $"transitionGate='{DescribeDiagnostic(Get(routeRequestResult, "TransitionGateDiagnostics"))}'";

            return new RouteStartupReadinessEvidence(
                GetBoolean(runtimeState, "IsActivityReady"),
                request,
                lifecycle?.ToDiagnosticString() ?? "<missing>",
                activityReadiness,
                blockingIssues,
                readinessState,
                readinessReason);
        }

        private static void AssertDistinctSecondaryJoin(
            QaPlayerGameplayAdmissionFixture fixture,
            QaPlayerJoinEvidence primary,
            QaPlayerJoinEvidence secondary,
            string reason)
        {
            Require(primary != null,
                $"Multi-Player join did not freeze primary evidence. reason='{reason}'.");
            Require(secondary != null,
                $"Multi-Player join returned null secondary evidence. reason='{reason}'.");
            Require(secondary.JoinResult != null && secondary.JoinResult.Succeeded,
                $"Multi-Player secondary join was not successful. result='{secondary.JoinResult?.ToDiagnosticString()}'.");
            Require(secondary.SlotId.IsValid,
                $"Multi-Player secondary join returned an invalid Slot. result='{secondary.JoinResult?.ToDiagnosticString()}'.");
            Require(secondary.PlayerInput != null,
                $"Multi-Player secondary join returned no PlayerInput. result='{secondary.JoinResult?.ToDiagnosticString()}'.");
            Require(secondary.Host != null,
                $"Multi-Player secondary join returned no LocalPlayerHost. result='{secondary.JoinResult?.ToDiagnosticString()}'.");
            Require(secondary.SlotId != primary.SlotId,
                $"Multi-Player join reused Slot. primarySlot='{primary.SlotId.StableText}' secondarySlot='{secondary.SlotId.StableText}'.");
            Require(!ReferenceEquals(secondary.PlayerInput, primary.PlayerInput),
                "Multi-Player join reused PlayerInput. " +
                $"primary='{primary.PlayerInputDiagnostic}' secondary='{secondary.PlayerInputDiagnostic}'.");
            Require(!ReferenceEquals(secondary.Host, primary.Host),
                "Multi-Player join reused LocalPlayerHost. " +
                $"primary='{primary.HostDiagnostic}' secondary='{secondary.HostDiagnostic}'.");
            Require(fixture.PlayerCount == 2,
                $"Multi-Player join expected PlayerCount='2', actual='{fixture.PlayerCount}'.");
            Require(fixture.JoinedSlotCount == 2,
                $"Multi-Player join expected JoinedSlotCount='2', actual='{fixture.JoinedSlotCount}'.");
            Require(fixture.JoinedPlayers.Count == 2,
                $"Multi-Player join expected owned JoinedPlayers='2', actual='{fixture.JoinedPlayers.Count}'.");
            Require(fixture.IsPrimaryJoinEvidenceCurrent(primary),
                "Multi-Player join overwrote or mutated primary join evidence. " +
                $"primarySlot='{primary.SlotId.StableText}' playerInput='{primary.PlayerInputDiagnostic}' host='{primary.HostDiagnostic}'.");
        }

        private static void AssertNoPlayer(QaPlayerGameplayAdmissionFixture fixture, string phase)
        {
            Require(fixture.PlayerCount == 0 && fixture.JoinResult == null,
                $"A Player was created during '{phase}'. playerCount='{fixture.PlayerCount}'.");
        }

        private static InvalidOperationException CreateIncompleteCasesException(int firstPendingIndex)
        {
            var pending = new List<string>();
            for (int index = firstPendingIndex; index < CaseNames.Length; index++)
                if (!RuntimeCases.TryGetValue(CaseNames[index], out Func<Task> body) || body == null)
                    pending.Add(CaseNames[index]);
            return new InvalidOperationException(
                "Player-independent Navigation Regression cannot report Passed until all " +
                $"'{ExpectedCompletedCaseCount}' cases execute runtime operations and assertions. " +
                $"Pending='{string.Join(",", pending)}'.");
        }

        private static void ValidateRuntimeCaseRegistration()
        {
            foreach (KeyValuePair<string, Func<Task>> registration in RuntimeCases)
            {
                if (!IsKnownCase(registration.Key))
                    throw new InvalidOperationException(
                        $"Game Flow regression registered unknown case '{registration.Key}'.");
                if (registration.Value == null)
                    throw new InvalidOperationException(
                        $"Game Flow regression case '{registration.Key}' has a null runtime delegate.");
            }
        }

        private static bool IsKnownCase(string caseName)
        {
            for (int index = 0; index < CaseNames.Length; index++)
                if (CaseNames[index] == caseName) return true;
            return false;
        }

        private static void ThrowCombinedFailures(Exception executionFailure, Exception cleanupFailure)
        {
            if (executionFailure != null && cleanupFailure != null)
                throw new AggregateException("Game Flow case execution and cleanup both failed.", executionFailure, cleanupFailure);
            if (executionFailure != null) throw executionFailure;
            if (cleanupFailure != null) throw cleanupFailure;
        }

        private static object Get(object target, string propertyName)
        {
            PropertyInfo property = target?.GetType().GetProperty(
                propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null) throw new InvalidOperationException(
                $"Runtime evidence property '{propertyName}' is unavailable on '{target?.GetType().FullName}'.");
            return property.GetValue(target);
        }

        private static bool GetBoolean(object target, string propertyName) =>
            Get(target, propertyName) is bool value && value;

        private static string GetString(object target, string propertyName) =>
            Get(target, propertyName) as string ?? string.Empty;

        private static string DescribeDiagnostic(object value)
        {
            if (value == null) return "<missing>";
            MethodInfo diagnostic = value.GetType().GetMethod(
                "ToDiagnosticString",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);
            if (diagnostic != null)
                return diagnostic.Invoke(value, null) as string ?? value.ToString();
            return value.ToString();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");

        private sealed class RouteStartupReadinessEvidence
        {
            public RouteStartupReadinessEvidence(
                bool isReady,
                string request,
                string lifecycle,
                string activityReadiness,
                string blockingIssues,
                string readinessState,
                string readinessReason)
            {
                IsReady = isReady;
                Request = request;
                Lifecycle = lifecycle;
                ActivityReadiness = activityReadiness;
                BlockingIssues = blockingIssues;
                ReadinessState = readinessState;
                ReadinessReason = readinessReason;
            }

            public bool IsReady { get; }
            public string Request { get; }
            public string Lifecycle { get; }
            public string ActivityReadiness { get; }
            public string BlockingIssues { get; }
            public string ReadinessState { get; }
            public string ReadinessReason { get; }
        }
    }
}

using System;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaActivityEntryReadinessFoundationRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run Activity Entry Readiness Foundation Regression";
        private const string Prefix = "[IF_READY_04_QA_FOUNDATION]";
        private const string TargetActivityId = "qa.if-ready-04.foundation.observe-only";
        private const string TargetActivityName = "QA IF READY 04 Observe Only";
        private const int ExpectedCaseCount = 18;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "official-host-and-ports-resolved",
            "initial-authority-captured",
            "route-primary-scene-resolved",
            "temporary-readiness-surface-created",
            "runtime-observe-only-activity-created",
            "participant-preparation-started",
            "observe-only-request-completed-before-ready",
            "observe-only-destination-authoritative",
            "readiness-completed-through-public-api",
            "readiness-ready-event-observed",
            "activity-authority-preserved-after-ready",
            "temporary-target-cleared",
            "participant-released-before-surface-destruction",
            "listeners-removed-before-surface-destruction",
            "temporary-readiness-surface-destroyed-before-restore",
            "initial-authority-restored-without-participant-reentry",
            "target-activity-destruction-confirmed"
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
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            var failures = new QaFailureCollector();
            var ownedRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "activity-entry-readiness-foundation");
            QaOperationUnwindResult<FrameworkActivityRequestResult> unwind = default;
            QaActivityEntryReadinessFixture fixture = null;
            int activityRequestCount = 0;

            try
            {
                Require(EditorApplication.isPlaying, "Regression requires Play Mode.");
                cases.Complete("play-mode-required");

                fixture = await QaActivityEntryReadinessFixture.CreateAsync();
                cases.Complete("official-host-and-ports-resolved");
                cases.Complete("initial-authority-captured");
                cases.Complete("route-primary-scene-resolved");
                cases.Complete("temporary-readiness-surface-created");

                ActivityAsset target = fixture.CreateActivity(
                    TargetActivityId,
                    TargetActivityName,
                    ActivityEntryReadinessPolicy.ObserveOnly);
                cases.Complete("runtime-observe-only-activity-created");

                ownedRequest.Attach(fixture.Activities.RequestActivityAsync(
                    target,
                    nameof(QaActivityEntryReadinessFoundationRegression),
                    "direct-observe-only"));
                activityRequestCount++;

                Task preparationStartedTask = fixture.PreparationStarted.Task;
                Task firstCompletion = await Task.WhenAny(
                    preparationStartedTask,
                    ownedRequest.Task);
                if (ReferenceEquals(firstCompletion, ownedRequest.Task))
                {
                    FrameworkActivityRequestResult earlyResult =
                        await ownedRequest.AwaitTerminalAsync();
                    throw new InvalidOperationException(
                        $"Activity request completed before participant preparation started. {DescribeRequest(fixture, earlyResult)}");
                }

                Require(preparationStartedTask.IsCompleted,
                    "Participant preparation signal was not completed after winning request coordination.");
                Require(fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing &&
                    fixture.Participant.Occurrence > 0 &&
                    fixture.PreparationStartedCount == 1,
                    "Readiness participant did not enter a valid Preparing occurrence.");
                ownedRequest.SetPhase(QaOwnedAsyncOperationPhase.Preparing);
                cases.Complete("participant-preparation-started");

                FrameworkActivityRequestResult requestResult =
                    await ownedRequest.AwaitTerminalAsync();
                Require(requestResult.Succeeded,
                    $"ObserveOnly request did not succeed. message='{requestResult.Message}'.");
                Require(fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing &&
                    !fixture.RuntimeHost.State.IsActivityReady &&
                    fixture.ReadinessPreparingCount == 1,
                    "ObserveOnly request completed only after readiness was already Ready.");
                cases.Complete("observe-only-request-completed-before-ready");

                Require(fixture.RuntimeHost.State.CurrentActivity != null &&
                    fixture.RuntimeHost.State.CurrentActivity.HasSameIdentity(target) &&
                    requestResult.DestinationAuthoritative,
                    "ObserveOnly target Activity is not authoritative after its completed request.");
                cases.Complete("observe-only-destination-authoritative");

                int preparingRevision = fixture.Events.LastRevision;
                Task readinessReadyTask = fixture.ReadinessReady.Task;
                fixture.Participant.CompletePreparation();
                cases.Complete("readiness-completed-through-public-api");

                await readinessReadyTask;
                Require(readinessReadyTask.IsCompleted,
                    $"Readiness Ready event was not observed after public completion. {DescribeReadiness(fixture)}");
                Require(fixture.Participant.State ==
                        ActivityReadinessParticipantState.Completed &&
                    fixture.Events.LastSnapshot.IsReady &&
                    fixture.Events.LastRevision > preparingRevision &&
                    fixture.Participant.Occurrence > 0,
                    "Readiness observer did not publish an advanced Ready snapshot.");
                cases.Complete("readiness-ready-event-observed");

                Require(fixture.RuntimeHost.State.CurrentActivity != null &&
                    fixture.RuntimeHost.State.CurrentActivity.HasSameIdentity(target),
                    "Activity authority changed after readiness completion.");
                Require(fixture.ReadinessReadyCount == 1,
                    "ObserveOnly foundation published more than one Ready event.");
                Require(activityRequestCount == 1,
                    "ObserveOnly foundation submitted more than one Activity request.");
                cases.Complete("activity-authority-preserved-after-ready");
            }
            catch (Exception exception)
            {
                failures.Add("Execution", exception);
            }
            finally
            {
                if (ownedRequest.HasOperation)
                {
                    try
                    {
                        unwind = await ownedRequest.UnwindAsync(
                            ownedRequest.IsCompleted || ownedRequest.ReachedTerminal
                                ? null
                                : CreateReadinessCompletionCallback(fixture));
                        Require(unwind.ReachedTerminal,
                            "Owned ObserveOnly request did not reach terminal before fixture cleanup.");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("Unwind", exception);
                    }
                }

                bool cleanupAllowed = fixture != null &&
                    (!ownedRequest.HasOperation || ownedRequest.ReachedTerminal);
                if (fixture != null && !cleanupAllowed)
                {
                    failures.Add("CleanupTerminalPrecondition", new InvalidOperationException(
                        "Readiness fixture cleanup was blocked because its owned Activity request was not terminal."));
                }

                if (cleanupAllowed)
                {
                    try
                    {
                        await fixture.PrepareForReadinessSurfaceDestructionAsync();
                        Require(fixture.CleanupKind ==
                                QaTemporaryActivityCleanupKind.TargetCleared &&
                            fixture.PreparationStartedCount == 1 &&
                            fixture.PreparationReleasedCount == 1,
                            $"Happy-path cleanup did not clear the temporary target. kind='{fixture.CleanupKind}' started='{fixture.PreparationStartedCount}' released='{fixture.PreparationReleasedCount}'.");
                        cases.TryCompleteIfNext("temporary-target-cleared");
                        cases.TryCompleteIfNext("participant-released-before-surface-destruction");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("CleanupAuthorityPreparation", exception);
                    }

                    try
                    {
                        fixture.RemoveEventListeners();
                        cases.TryCompleteIfNext("listeners-removed-before-surface-destruction");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("ListenerCleanup", exception);
                    }

                    try
                    {
                        await fixture.DestroyReadinessSurfaceAsync();
                        Require(fixture.ReadinessSurfaceDestroyed &&
                            fixture.FinalPreparationStartedCount == 1 &&
                            fixture.FinalPreparationReleasedCount == 1 &&
                            fixture.FinalOccurrence > 0,
                            "Temporary readiness surface destruction evidence diverged.");
                        fixture.RequireValidFinalParticipantEvidence();
                        cases.TryCompleteIfNext("temporary-readiness-surface-destroyed-before-restore");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("ReadinessSurfaceDestruction", exception);
                    }

                    try
                    {
                        await fixture.RestoreInitialAuthorityAsync();
                        Require(fixture.PreparationStartedCount ==
                                fixture.FinalPreparationStartedCount &&
                            fixture.PreparationReleasedCount ==
                                fixture.FinalPreparationReleasedCount,
                            "Temporary participant re-entered while restoring initial authority.");
                        cases.TryCompleteIfNext("initial-authority-restored-without-participant-reentry");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("InitialAuthorityRestoration", exception);
                    }

                    try
                    {
                        await fixture.DestroyTargetActivityAsync();
                        Require(fixture.TargetActivityWasCreated &&
                            fixture.TargetActivityDestructionConfirmed,
                            "Target Activity destruction was not confirmed.");
                        cases.TryCompleteIfNext("target-activity-destruction-confirmed");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("TargetActivityDestruction", exception);
                    }
                }
            }

            if (failures.HasFailures)
            {
                Debug.LogError($"{Prefix} status='Failed' " +
                    $"execution='{Escape(failures.Describe("Execution"))}' " +
                    $"unwind='{Escape(failures.Describe("Unwind"))}' " +
                    $"cleanupTerminalPrecondition='{Escape(failures.Describe("CleanupTerminalPrecondition"))}' " +
                    $"cleanupAuthorityPreparation='{Escape(failures.Describe("CleanupAuthorityPreparation"))}' " +
                    $"listenerCleanup='{Escape(failures.Describe("ListenerCleanup"))}' " +
                    $"readinessSurfaceDestruction='{Escape(failures.Describe("ReadinessSurfaceDestruction"))}' " +
                    $"initialAuthorityRestoration='{Escape(failures.Describe("InitialAuthorityRestoration"))}' " +
                    $"targetActivityDestruction='{Escape(failures.Describe("TargetActivityDestruction"))}' " +
                    $"operationPhase='{ownedRequest.Phase}' " +
                    $"operationTerminal='{ownedRequest.ReachedTerminal}' " +
                    $"unwindCompletionIssued='{unwind.CompletionIssued}' " +
                    $"nextExpectedCase='{cases.NextExpectedOrNone()}' " +
                    $"missingCases='{cases.DescribeMissing()}' " +
                    $"completed='{cases.DescribeCompleted()}'.");
                throw failures.ToAggregate(
                    "Activity entry readiness foundation regression failed.");
            }

            cases.RequireComplete();
            Debug.Log($"{Prefix} status='Passed' cases='{cases.Count}' " +
                $"operationTerminal='{ownedRequest.ReachedTerminal}' " +
                $"unwindCompletionIssued='{unwind.CompletionIssued}' " +
                $"completed='{cases.DescribeCompleted()}' " +
                $"events='{string.Join(",", fixture.EventOrder)}'.");
        }

        private static Func<Task> CreateReadinessCompletionCallback(
            QaActivityEntryReadinessFixture fixture)
        {
            return () =>
            {
                Require(fixture != null,
                    "Cannot unwind readiness without an active fixture.");
                Require(fixture.Participant.State ==
                        ActivityReadinessParticipantState.Preparing,
                    $"Cannot unwind ObserveOnly request from participant state '{fixture.Participant.State}'.");
                fixture.Participant.CompletePreparation();
                return Task.CompletedTask;
            };
        }

        private static string DescribeRequest(
            QaActivityEntryReadinessFixture fixture,
            FrameworkActivityRequestResult result)
        {
            string currentActivity = fixture.RuntimeHost.State.CurrentActivity == null
                ? "<none>"
                : fixture.RuntimeHost.State.CurrentActivity.ActivityName;
            return $"kind='{result.Kind}' message='{Escape(result.Message)}' " +
                $"destinationAuthoritative='{result.DestinationAuthoritative}' " +
                $"currentActivity='{currentActivity}' participantState='{fixture.Participant.State}' " +
                $"participantStarts='{fixture.PreparationStartedCount}' " +
                $"participantOccurrence='{fixture.Participant.Occurrence}'.";
        }

        private static string DescribeReadiness(
            QaActivityEntryReadinessFixture fixture)
        {
            return $"participantState='{fixture.Participant.State}' " +
                $"participantOccurrence='{fixture.Participant.Occurrence}' " +
                $"readyCount='{fixture.ReadinessReadyCount}' " +
                $"preparingCount='{fixture.ReadinessPreparingCount}' " +
                $"notReadyCount='{fixture.ReadinessNotReadyCount}' " +
                $"lastRevision='{fixture.Events.LastRevision}' " +
                $"lastReason='{Escape(fixture.Events.LastReason)}' " +
                $"snapshotReady='{fixture.Events.LastSnapshot.IsReady}' " +
                $"snapshotPreparing='{fixture.Events.LastSnapshot.IsPreparing}'.";
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Gate;
using Immersive.Framework.Loading;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Q2A negative regression for participant-aware WaitCovered Loading progress.
    /// It proves the real direct Activity failure path and isolates the shared
    /// operation-scoped envelope for released, stale and duplicate terminal evidence.
    /// Route and Game Application end-to-end startup execution remain a separate Q2B
    /// because the current QA baseline has no isolated canonical startup-host fixture.
    /// </summary>
    public static class QaParticipantAwareReadinessLoadingTerminalRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run Participant-Aware Readiness Loading Terminal Regression";
        private const string Prefix = "[QA_READY_PROGRESS_02A]";
        private const string DirectReason = "participant-aware-terminal-required-failure";
        private const string ContentScenePath =
            "Assets/ImmersiveFrameworkQA/GameFlow/Scenes/QA_IF_READY_04_DirectPoliciesContent.unity";
        private const float ProgressTolerance = 0.0005f;
        private const int ExpectedCaseCount = 34;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "direct-policies-prepared",
            "official-host-resolved",
            "wrapper-signature-parity-confirmed",
            "direct-envelope-required-failure",
            "route-envelope-required-failure",
            "game-application-envelope-required-failure",
            "required-release-terminal-confirmed",
            "replacement-occurrence-rejected",
            "late-old-occurrence-rejected",
            "duplicate-terminal-idempotent",
            "owned-cancellation-started",
            "owned-cancellation-terminal",
            "duplicate-terminal-observation-idempotent",
            "direct-host-surfaces-resolved",
            "direct-fixture-created",
            "direct-participant-set-created",
            "direct-activity-created",
            "direct-request-started",
            "direct-participants-preparing",
            "direct-required-failed",
            "direct-terminal-result-typed",
            "direct-destination-authoritative",
            "direct-terminal-snapshot-confirmed",
            "direct-last-progress-below-one",
            "direct-no-terminal-progress-update",
            "direct-loading-retained",
            "direct-transition-retained",
            "direct-recovery-gate-retained",
            "direct-participants-released",
            "direct-fixture-cleaned",
            "direct-presentation-restored",
            "direct-gate-released",
            "direct-initial-authority-restored"
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
            FrameworkRuntimeHost host = null;

            try
            {
                Require(EditorApplication.isPlaying,
                    "Participant-aware terminal regression requires Play Mode.");
                cases.Complete("play-mode-required");

                QaActivityEntryPresentationEvidenceSetup
                    .RequireDirectPoliciesPreparedForCurrentPlayMode();
                cases.Complete("direct-policies-prepared");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(host.State.GameFlowStarted &&
                    host.State.CurrentRoute != null,
                    "Participant-aware terminal regression requires a started official host.");
                cases.Complete("official-host-resolved");

                RequireWrapperSignatureParity(host);
                cases.Complete("wrapper-signature-parity-confirmed");

                await RunSharedEnvelopeMatrixAsync(cases);
                await RunOwnedCancellationMatrixAsync(cases);
                await RunDirectRequiredFailureAsync(host, cases);
            }
            catch (Exception exception)
            {
                failures.Add("Execution", exception);
            }

            if (failures.HasFailures)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' " +
                    $"execution='{failures.Describe("Execution")}' " +
                    $"nextExpectedCase='{cases.NextExpectedOrNone()}' " +
                    $"missingCases='{cases.DescribeMissing()}' " +
                    $"completed='{cases.DescribeCompleted()}'.");
                throw failures.ToAggregate(
                    "Participant-aware readiness Loading terminal regression failed.");
            }

            cases.RequireComplete();
            Debug.Log(
                $"{Prefix} status='Passed' cases='{ExpectedCaseCount}' " +
                "runtimePath='DirectActivityRequiredFailure' " +
                "contractPaths='DirectActivity,RouteStartupActivity,GameApplicationStartupActivity' " +
                "terminals='RequiredFailed,RequiredReleased,ReplacementRejected," +
                "LateOldOccurrenceRejected,DuplicateTerminal,OwnedCancellation' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static void RequireWrapperSignatureParity(
            FrameworkRuntimeHost host)
        {
            Require(host != null && host.CurrentGameFlowRuntime != null,
                "Current Game Flow runtime is unavailable for wrapper parity.");

            Func<
                GameApplicationAsset,
                Func<Awaitable>,
                Func<Awaitable>,
                IFrameworkLoadingProgressReporter,
                Action<ActivityEntryLoadingProgressDiagnostics>,
                Task<FrameworkGameFlowStartResult>> gameApplicationStartup =
                    host.CurrentGameFlowRuntime
                        .StartWithActivityEntryLoadingProgressAsync;

            Func<
                RouteAsset,
                string,
                string,
                Func<Awaitable>,
                Func<Awaitable>,
                IFrameworkLoadingProgressReporter,
                Action<ActivityEntryLoadingProgressDiagnostics>,
                Task<FrameworkRouteRequestResult>> routeStartup =
                    host.CurrentGameFlowRuntime
                        .RequestRouteWithActivityEntryLoadingProgressAsync;

            Func<
                ActivityAsset,
                string,
                string,
                Func<Awaitable>,
                Func<Awaitable>,
                IFrameworkLoadingProgressReporter,
                Action<ActivityEntryLoadingProgressDiagnostics>,
                Task<FrameworkActivityRequestResult>> directActivity =
                    host.CurrentGameFlowRuntime
                        .RequestActivityWithActivityEntryLoadingProgressAsync;

            Require(gameApplicationStartup != null &&
                routeStartup != null &&
                directActivity != null,
                "The three participant-aware Loading wrappers do not expose " +
                "the same typed reporter and diagnostics contract.");
        }

        private static async Task RunSharedEnvelopeMatrixAsync(
            QaCaseRegistry cases)
        {
            ActivityAsset activity = CreateEnvelopeActivity();
            try
            {
                await RequireRequiredFailureEnvelopeAsync(
                    activity,
                    "DirectActivity");
                cases.Complete("direct-envelope-required-failure");

                await RequireRequiredFailureEnvelopeAsync(
                    activity,
                    "RouteStartupActivity");
                cases.Complete("route-envelope-required-failure");

                await RequireRequiredFailureEnvelopeAsync(
                    activity,
                    "GameApplicationStartupActivity");
                cases.Complete("game-application-envelope-required-failure");

                await RequireRequiredReleaseEnvelopeAsync(activity);
                cases.Complete("required-release-terminal-confirmed");

                await RequireReplacementOccurrenceRejectedAsync(activity);
                cases.Complete("replacement-occurrence-rejected");

                await RequireLateOldOccurrenceRejectedAsync(activity);
                cases.Complete("late-old-occurrence-rejected");

                await RequireDuplicateTerminalIdempotentAsync(activity);
                cases.Complete("duplicate-terminal-idempotent");
            }
            finally
            {
                UnityEngine.Object.Destroy(activity);
            }
        }

        private static async Task RequireRequiredFailureEnvelopeAsync(
            ActivityAsset activity,
            string operationLabel)
        {
            QaRecordingLoadingProgressReporter reporter =
                new QaRecordingLoadingProgressReporter(operationLabel);
            ActivityEntryLoadingProgressEnvelope envelope =
                CreateEnvelope(reporter, operationLabel);
            ActivityReadinessOccurrence occurrence =
                new ActivityReadinessOccurrence(activity, 1);

            await CompleteTechnicalRangeAsync(envelope, operationLabel);
            await envelope.ReportReadinessAsync(CreateSnapshot(
                occurrence,
                requiredPending: 4,
                requiredCompleted: 0,
                requiredFailed: 0,
                requiredReleased: 0,
                optionalPending: 1,
                optionalCompleted: 0,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: false,
                diagnostic: "Preparing"));

            float beforeFailure = envelope.LastAcceptedProgress01;
            int reportsBeforeFailure = reporter.Count;
            await envelope.ReportReadinessAsync(CreateSnapshot(
                occurrence,
                requiredPending: 3,
                requiredCompleted: 0,
                requiredFailed: 1,
                requiredReleased: 0,
                optionalPending: 1,
                optionalCompleted: 0,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: false,
                diagnostic: "RequiredFailed"));

            Require(envelope.TerminalFailureObserved &&
                !envelope.TerminalCompletionIssued &&
                envelope.LastAcceptedProgress01 < 1f &&
                Approximately(envelope.LastAcceptedProgress01, beforeFailure) &&
                reporter.Count == reportsBeforeFailure &&
                !reporter.ContainsTerminalProgress,
                $"{operationLabel} fabricated completion after Required failure.");
        }

        private static async Task RequireRequiredReleaseEnvelopeAsync(
            ActivityAsset activity)
        {
            QaRecordingLoadingProgressReporter reporter =
                new QaRecordingLoadingProgressReporter("RequiredReleased");
            ActivityEntryLoadingProgressEnvelope envelope =
                CreateEnvelope(reporter, "RequiredReleased");
            ActivityReadinessOccurrence occurrence =
                new ActivityReadinessOccurrence(activity, 2);

            await CompleteTechnicalRangeAsync(envelope, "RequiredReleased");
            await envelope.ReportReadinessAsync(CreateSnapshot(
                occurrence,
                requiredPending: 4,
                requiredCompleted: 0,
                requiredFailed: 0,
                requiredReleased: 0,
                optionalPending: 0,
                optionalCompleted: 1,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: false,
                diagnostic: "Preparing"));
            int reportsBeforeRelease = reporter.Count;

            await envelope.ReportReadinessAsync(CreateSnapshot(
                occurrence,
                requiredPending: 3,
                requiredCompleted: 0,
                requiredFailed: 0,
                requiredReleased: 1,
                optionalPending: 0,
                optionalCompleted: 1,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: false,
                diagnostic: "RequiredReleased"));

            Require(envelope.TerminalFailureObserved &&
                !envelope.TerminalCompletionIssued &&
                envelope.LastAcceptedProgress01 < 1f &&
                reporter.Count == reportsBeforeRelease &&
                !reporter.ContainsTerminalProgress,
                "Required release fabricated terminal Loading completion.");
        }

        private static async Task RequireReplacementOccurrenceRejectedAsync(
            ActivityAsset activity)
        {
            QaRecordingLoadingProgressReporter reporter =
                new QaRecordingLoadingProgressReporter("ReplacementOccurrence");
            ActivityEntryLoadingProgressEnvelope envelope =
                CreateEnvelope(reporter, "ReplacementOccurrence");
            ActivityReadinessOccurrence original =
                new ActivityReadinessOccurrence(activity, 10);
            ActivityReadinessOccurrence replacement =
                new ActivityReadinessOccurrence(activity, 11);

            await CompleteTechnicalRangeAsync(envelope, "ReplacementOccurrence");
            await envelope.ReportReadinessAsync(CreateSnapshot(
                original,
                requiredPending: 4,
                requiredCompleted: 0,
                requiredFailed: 0,
                requiredReleased: 0,
                optionalPending: 0,
                optionalCompleted: 0,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: false,
                diagnostic: "OriginalPreparing"));
            int reportsBeforeReplacement = reporter.Count;

            await envelope.ReportReadinessAsync(CreateSnapshot(
                replacement,
                requiredPending: 0,
                requiredCompleted: 4,
                requiredFailed: 0,
                requiredReleased: 0,
                optionalPending: 0,
                optionalCompleted: 0,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: true,
                diagnostic: "ReplacementReady"));

            Require(envelope.RejectedReadinessSnapshotCount == 1 &&
                reporter.Count == reportsBeforeReplacement &&
                !envelope.TerminalCompletionIssued &&
                !reporter.ContainsTerminalProgress,
                "Replacement occurrence advanced the original Loading envelope.");
        }

        private static async Task RequireLateOldOccurrenceRejectedAsync(
            ActivityAsset activity)
        {
            QaRecordingLoadingProgressReporter reporter =
                new QaRecordingLoadingProgressReporter("LateOldOccurrence");
            ActivityEntryLoadingProgressEnvelope envelope =
                CreateEnvelope(reporter, "LateOldOccurrence");
            ActivityReadinessOccurrence oldOccurrence =
                new ActivityReadinessOccurrence(activity, 20);
            ActivityReadinessOccurrence currentOccurrence =
                new ActivityReadinessOccurrence(activity, 21);

            await CompleteTechnicalRangeAsync(envelope, "LateOldOccurrence");
            await envelope.ReportReadinessAsync(CreateSnapshot(
                currentOccurrence,
                requiredPending: 2,
                requiredCompleted: 2,
                requiredFailed: 0,
                requiredReleased: 0,
                optionalPending: 0,
                optionalCompleted: 0,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: false,
                diagnostic: "CurrentPreparing"));
            float currentProgress = envelope.LastAcceptedProgress01;
            int reportsBeforeLateOld = reporter.Count;

            await envelope.ReportReadinessAsync(CreateSnapshot(
                oldOccurrence,
                requiredPending: 0,
                requiredCompleted: 4,
                requiredFailed: 0,
                requiredReleased: 0,
                optionalPending: 0,
                optionalCompleted: 0,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: true,
                diagnostic: "LateOldReady"));

            Require(envelope.RejectedReadinessSnapshotCount == 1 &&
                reporter.Count == reportsBeforeLateOld &&
                Approximately(envelope.LastAcceptedProgress01, currentProgress) &&
                !envelope.TerminalCompletionIssued &&
                !reporter.ContainsTerminalProgress,
                "Late completion from an old occurrence advanced the current Loading envelope.");
        }

        private static async Task RequireDuplicateTerminalIdempotentAsync(
            ActivityAsset activity)
        {
            QaRecordingLoadingProgressReporter reporter =
                new QaRecordingLoadingProgressReporter("DuplicateTerminal");
            ActivityEntryLoadingProgressEnvelope envelope =
                CreateEnvelope(reporter, "DuplicateTerminal");
            ActivityReadinessOccurrence occurrence =
                new ActivityReadinessOccurrence(activity, 30);

            await CompleteTechnicalRangeAsync(envelope, "DuplicateTerminal");
            ActivityReadinessProgressSnapshot failed = CreateSnapshot(
                occurrence,
                requiredPending: 3,
                requiredCompleted: 0,
                requiredFailed: 1,
                requiredReleased: 0,
                optionalPending: 0,
                optionalCompleted: 0,
                optionalFailed: 0,
                optionalReleased: 0,
                ready: false,
                diagnostic: "Failed");

            await envelope.ReportReadinessAsync(failed);
            int reportCount = reporter.Count;
            float lastProgress = envelope.LastAcceptedProgress01;
            envelope.MarkTerminalFailure();
            envelope.MarkTerminalFailure();
            await envelope.ReportReadinessAsync(failed);
            await envelope.FlushQueuedReportsAsync();

            Require(envelope.TerminalFailureObserved &&
                !envelope.TerminalCompletionIssued &&
                reporter.Count == reportCount &&
                Approximately(envelope.LastAcceptedProgress01, lastProgress) &&
                !reporter.ContainsTerminalProgress,
                "Duplicate terminal evidence was not idempotent.");
        }

        private static async Task RunOwnedCancellationMatrixAsync(
            QaCaseRegistry cases)
        {
            var source = new TaskCompletionSource<int>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            var owned = new QaOwnedAsyncOperation<int>(
                "participant-aware-terminal-owned-cancellation");
            owned.Attach(source.Task);
            cases.Complete("owned-cancellation-started");

            QaOperationUnwindResult<int> unwind = await owned.UnwindAsync(
                () =>
                {
                    source.TrySetCanceled();
                    return Task.CompletedTask;
                });

            Require(unwind.OperationExisted &&
                unwind.CompletionIssued &&
                unwind.ReachedTerminal &&
                unwind.WasCancelled &&
                !unwind.SucceededToAwait &&
                unwind.Failure is OperationCanceledException &&
                owned.ReachedTerminal &&
                owned.WasCancelled,
                "Owned cancellation did not terminate with typed cancellation evidence.");
            cases.Complete("owned-cancellation-terminal");

            Task<int> firstObservation = owned.AwaitTerminalAsync();
            Task<int> secondObservation = owned.AwaitTerminalAsync();
            Require(ReferenceEquals(firstObservation, secondObservation),
                "Duplicate terminal observation did not reuse the owned terminal task.");
            await RequireCancelledAsync(firstObservation);
            await RequireCancelledAsync(secondObservation);
            cases.Complete("duplicate-terminal-observation-idempotent");
        }

        private static async Task RunDirectRequiredFailureAsync(
            FrameworkRuntimeHost host,
            QaCaseRegistry cases)
        {
            RouteAsset initialRoute = host.State.CurrentRoute;
            ActivityAsset initialActivity = host.State.CurrentActivity;
            QaActivityEntryReadinessFixture fixture = null;
            QaParticipantAwareReadinessParticipants participants = null;
            QaLoadingSurfaceVisibilityHoldAdapter loading = null;
            UnityFadeCurtainEffectAdapter transition = null;
            QaTerminalDeterminateProgressProbe progressProbe = null;
            var ownedRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "participant-aware-direct-required-failure");
            var failures = new QaFailureCollector();
            bool participantSurfaceCleaned = false;
            bool fixtureCleaned = false;
            bool presentationRestored = false;
            bool gateReleased = false;
            bool authorityRestored = false;

            string source = nameof(
                QaParticipantAwareReadinessLoadingTerminalRegression);
            LoadingSurfaceRequest loadingCleanupRequest =
                LoadingSurfaceRequest.Hide(
                    "QA READY PROGRESS 02A",
                    "terminal cleanup",
                    source,
                    "terminal-cleanup-hide");
            TransitionOperationId cleanupOperationId =
                TransitionOperationId.From(
                    "qa.ready-progress-02a.cleanup");
            TransitionEffectRequest transitionCleanupRequest =
                TransitionEffectRequest.Required(
                    "qa.ready-progress-02a.cleanup.fade",
                    TransitionEffectKind.Fade,
                    cleanupOperationId,
                    TransitionKind.ActivitySwitch,
                    TransitionPhase.OperationClosed,
                    source,
                    "terminal-cleanup-hide");

            try
            {
                ValidateContentScene();
                transition = ResolveSinglePersistentRuntimeComponent<
                    UnityFadeCurtainEffectAdapter>(host, "Transition");
                loading = ResolveSinglePersistentRuntimeComponent<
                    QaLoadingSurfaceVisibilityHoldAdapter>(host, "Loading");
                Require(transition.ConfiguredEffectKind ==
                        TransitionEffectKind.Fade &&
                    transition.HasCanvasGroup &&
                    !transition.IsVisible &&
                    loading.HasCanvasGroup &&
                    loading.HasSurfaceImage &&
                    loading.HasProgressPresentation &&
                    !loading.IsVisible,
                    "Official host presentation surfaces are not ready for the terminal regression.");
                cases.Complete("direct-host-surfaces-resolved");

                fixture = await QaActivityEntryReadinessFixture.CreateAsync();
                cases.Complete("direct-fixture-created");

                participants =
                    QaParticipantAwareReadinessParticipants.Create(fixture);
                Require(participants.Required.Count == 4 &&
                    participants.All.Count == 5,
                    "Direct terminal fixture requires 4 Required and 1 Optional participant.");
                cases.Complete("direct-participant-set-created");

                ActivityAsset target = fixture.CreateActivity(
                    "qa.ready-progress-02a.required-failure",
                    "QA Required Failure Activity",
                    ActivityEntryReadinessPolicy.WaitCovered,
                    ActivityVisualTransitionMode.FadeWithLoading,
                    TransitionGateMode.InputInteractionAndGameplay,
                    ContentScenePath);
                ActivityOperationKind operationKind = initialActivity == null
                    ? ActivityOperationKind.Start
                    : ActivityOperationKind.Switch;
                ActivityOperationResult operationPreview =
                    host.CurrentGameFlowRuntime.PreviewActivityOperation(
                        operationKind,
                        initialActivity,
                        target,
                        target.VisualTransitionMode,
                        source,
                        DirectReason);
                Require(operationPreview.IsValid &&
                    !operationPreview.IsBlocked &&
                    operationPreview.VisualMode ==
                        ActivityVisualTransitionMode.FadeWithLoading &&
                    operationPreview.HasSceneSideEffects &&
                    operationPreview.SceneSideEffectCount > 0 &&
                    operationPreview.RequiresLoadingSurface,
                    "Direct terminal target did not produce a valid " +
                    "FadeWithLoading operation with real scene side-effects. " +
                    operationPreview.ToDiagnosticString());
                cases.Complete("direct-activity-created");

                loading.ResetPresentationEvidence();
                progressProbe = new QaTerminalDeterminateProgressProbe(
                    loading);
                progressProbe.Attach();
                ownedRequest.Attach(fixture.Activities.RequestActivityAsync(
                    target,
                    source,
                    DirectReason));
                cases.Complete("direct-request-started");

                await RequireSignalBeforeRequestAsync(
                    participants.AllPreparing,
                    ownedRequest,
                    "Direct request completed before all participants entered Preparing.");
                participants.RequireAllPreparing();
                Task<QaLoadingPresentationEvidenceEntry>
                    initialDeterminateProgressTask =
                        progressProbe.WaitForSubTerminalProgressAsync();
                await RequireSignalBeforeRequestAsync(
                    initialDeterminateProgressTask,
                    ownedRequest,
                    "Direct request completed before a determinate Loading " +
                    "update below 100% was materialized.");
                QaLoadingPresentationEvidenceEntry initialProgress =
                    await initialDeterminateProgressTask;
                Require(initialProgress.ProgressSupported &&
                    initialProgress.ProgressValue01 >= 0f &&
                    initialProgress.ProgressValue01 < 1f - ProgressTolerance,
                    "Direct terminal pre-failure Loading evidence was not " +
                    "determinate and below 100%.");
                cases.Complete("direct-participants-preparing");

                ActivityReadinessParticipant failedRequired =
                    participants.Required[0];
                failedRequired.FailPreparation(
                    "Q2ARequiredFailure");
                Require(failedRequired.State ==
                    ActivityReadinessParticipantState.Failed,
                    "Required participant did not enter Failed.");
                cases.Complete("direct-required-failed");

                FrameworkActivityRequestResult result =
                    await ownedRequest.AwaitTerminalAsync();
                Require(result.Kind ==
                        FrameworkActivityRequestKind
                            .FailedCommittedTargetNotReady &&
                    !result.Succeeded &&
                    result.CommitBoundaryReached,
                    "Direct Required failure did not produce the typed committed-not-ready result. " +
                    $"kind='{result.Kind}' message='{result.Message}'.");
                cases.Complete("direct-terminal-result-typed");

                Require(result.DestinationAuthoritative &&
                    result.TargetActivity != null &&
                    result.TargetActivity.HasSameStableId(target) &&
                    host.State.CurrentActivity != null &&
                    host.State.CurrentActivity.HasSameStableId(target),
                    "Direct failure did not preserve committed destination authority.");
                cases.Complete("direct-destination-authoritative");

                ActivityReadinessProgressSnapshot terminalSnapshot =
                    RequireCurrentTerminalSnapshot(host, target);
                Require(terminalSnapshot.RequiredCount == 4 &&
                    terminalSnapshot.RequiredFailedCount == 1 &&
                    terminalSnapshot.RequiredPendingCount == 3 &&
                    terminalSnapshot.RequiredCompletedCount == 0 &&
                    terminalSnapshot.RequiredReleasedCount == 0 &&
                    terminalSnapshot.HasTerminalFailure &&
                    !terminalSnapshot.IsReady,
                    "Direct terminal readiness snapshot diverged.");
                cases.Complete("direct-terminal-snapshot-confirmed");

                DirectProgressEvidence progressEvidence =
                    progressProbe.CaptureEvidence();
                Require(progressEvidence.UpdateCount > 0 &&
                    progressEvidence.LastProgress01 < 1f,
                    "Direct terminal path did not retain a determinate value below 100%.");
                cases.Complete("direct-last-progress-below-one");

                Require(!progressEvidence.HasTerminalProgress,
                    "Direct terminal path published a 100% Loading update.");
                cases.Complete("direct-no-terminal-progress-update");

                Require(loading.IsVisible &&
                    loading.CurrentAlpha > 0.001f &&
                    !progressEvidence.HideObserved,
                    "Direct terminal path did not retain Loading for explicit recovery.");
                cases.Complete("direct-loading-retained");

                Require(transition.IsVisible &&
                    transition.CurrentAlpha > 0.001f,
                    "Direct terminal path did not retain Transition cover.");
                cases.Complete("direct-transition-retained");

                RequireRecoveryGate(host);
                cases.Complete("direct-recovery-gate-retained");
            }
            catch (Exception exception)
            {
                failures.Add("Execution", exception);
            }
            finally
            {
                progressProbe?.Dispose();

                if (ownedRequest.HasOperation && !ownedRequest.ReachedTerminal)
                {
                    try
                    {
                        QaOperationUnwindResult<FrameworkActivityRequestResult>
                            unwind = await ownedRequest.UnwindAsync(
                                participants != null
                                    ? participants
                                        .CompleteAllPendingForUnwindAsync
                                    : () => Task.CompletedTask);
                        if (!unwind.SucceededToAwait)
                        {
                            throw unwind.Failure ??
                                new InvalidOperationException(
                                    "Direct terminal request unwind did not reach a successful await boundary.");
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add("Unwind", exception);
                    }
                }

                if (fixture != null)
                {
                    try
                    {
                        await fixture
                            .PrepareForReadinessSurfaceDestructionAsync();
                        if (participants != null)
                        {
                            Require(participants.ReleasedCount ==
                                    participants.All.Count,
                                "Direct terminal cleanup did not release every participant.");
                            await participants.DisposeAsync();
                            participantSurfaceCleaned = true;
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add("ParticipantCleanup", exception);
                    }

                    try
                    {
                        await fixture.DisposeAsync(ownedRequest);
                        fixtureCleaned = true;
                    }
                    catch (Exception exception)
                    {
                        failures.Add("FixtureCleanup", exception);
                    }
                }

                try
                {
                    if (transition != null && transition.IsVisible)
                    {
                        TransitionEffectResult transitionResult =
                            await transition.ExecuteAsync(
                                transitionCleanupRequest);
                        Require(transitionResult.Succeeded,
                            transitionResult.Message);
                    }

                    if (loading != null && loading.IsVisible)
                    {
                        LoadingSurfaceResult loadingResult =
                            loading.Hide(loadingCleanupRequest);
                        Require(loadingResult.Succeeded,
                            loadingResult.Message);
                    }

                    Require(transition == null ||
                        (!transition.IsVisible &&
                         transition.CurrentAlpha <= 0.001f),
                        "Transition cleanup did not finish hidden.");
                    Require(loading == null ||
                        (!loading.IsVisible &&
                         loading.CurrentAlpha <= 0.001f &&
                         !loading.HideHoldActive),
                        "Loading cleanup did not finish hidden.");
                    loading?.ResetPresentationEvidence();
                    presentationRestored = true;
                }
                catch (Exception exception)
                {
                    failures.Add("PresentationCleanup", exception);
                }

                try
                {
                    RequireRecoveryStateCleared(
                        host,
                        "after direct terminal cleanup");
                    gateReleased = true;
                }
                catch (Exception exception)
                {
                    failures.Add("GateCleanup", exception);
                }

                try
                {
                    RequireAuthority(
                        host,
                        initialRoute,
                        initialActivity);
                    authorityRestored = true;
                }
                catch (Exception exception)
                {
                    failures.Add("AuthorityCleanup", exception);
                }
            }

            if (participantSurfaceCleaned)
            {
                cases.TryCompleteIfNext("direct-participants-released");
            }
            if (fixtureCleaned)
            {
                cases.TryCompleteIfNext("direct-fixture-cleaned");
            }
            if (presentationRestored)
            {
                cases.TryCompleteIfNext("direct-presentation-restored");
            }
            if (gateReleased)
            {
                cases.TryCompleteIfNext("direct-gate-released");
            }
            if (authorityRestored)
            {
                cases.TryCompleteIfNext("direct-initial-authority-restored");
            }

            if (failures.HasFailures)
            {
                Debug.LogError(
                    $"{Prefix} stage='DirectRequiredFailure' " +
                    $"execution='{failures.Describe("Execution")}' " +
                    $"unwind='{failures.Describe("Unwind")}' " +
                    $"participantCleanup='{failures.Describe("ParticipantCleanup")}' " +
                    $"fixtureCleanup='{failures.Describe("FixtureCleanup")}' " +
                    $"presentationCleanup='{failures.Describe("PresentationCleanup")}' " +
                    $"gateCleanup='{failures.Describe("GateCleanup")}' " +
                    $"authorityCleanup='{failures.Describe("AuthorityCleanup")}' " +
                    $"nextExpectedCase='{cases.NextExpectedOrNone()}'.");
                throw failures.ToAggregate(
                    "Direct Required failure terminal proof failed.");
            }
        }

        private static ActivityEntryLoadingProgressEnvelope CreateEnvelope(
            IFrameworkLoadingProgressReporter reporter,
            string label)
        {
            ActivityEntryLoadingProgressPlan plan =
                ActivityEntryLoadingProgressPlan.Create(
                    technicalStepCount: 1,
                    reserveReadinessPhase: true);
            Require(plan.HasTechnicalRange &&
                plan.HasReadinessRange &&
                plan.TechnicalRange.End01 < 1f,
                $"{label} did not create the reserved readiness range.");
            return new ActivityEntryLoadingProgressEnvelope(
                reporter,
                plan,
                label,
                $"{label} technical Loading progress.");
        }

        private static async Task CompleteTechnicalRangeAsync(
            ActivityEntryLoadingProgressEnvelope envelope,
            string label)
        {
            await envelope.TechnicalReporter.ReportAsync(
                FrameworkLoadingProgress.Determinate(
                    1f,
                    label,
                    $"{label} technical complete."));
            Require(envelope.HasDeterminateProgress &&
                envelope.LastAcceptedProgress01 < 1f &&
                Approximately(
                    envelope.LastAcceptedProgress01,
                    envelope.Plan.TechnicalRange.End01),
                $"{label} technical range did not stop below terminal completion.");
        }

        private static ActivityReadinessProgressSnapshot CreateSnapshot(
            ActivityReadinessOccurrence occurrence,
            int requiredPending,
            int requiredCompleted,
            int requiredFailed,
            int requiredReleased,
            int optionalPending,
            int optionalCompleted,
            int optionalFailed,
            int optionalReleased,
            bool ready,
            string diagnostic)
        {
            int requiredCount = requiredPending +
                requiredCompleted +
                requiredFailed +
                requiredReleased;
            int optionalCount = optionalPending +
                optionalCompleted +
                optionalFailed +
                optionalReleased;
            int blockers = requiredFailed + requiredReleased;
            ActivityReadinessState state = new ActivityReadinessState(
                status: ready
                    ? ActivityReadinessStatus.Ready
                    : ActivityReadinessStatus.NotReady,
                activity: occurrence.Activity,
                activityContentSet: default,
                activityContentLifecycleResult: default,
                activityContentExecutionExecuted: true,
                activityContentExecutionBlocksReadiness: false,
                activityContentExecutionBlockingIssueCount: 0,
                requiredCount: requiredCount,
                optionalCount: optionalCount,
                requiredPendingCount: requiredPending,
                requiredCompletedCount: requiredCompleted,
                requiredFailedCount: requiredFailed,
                requiredReleasedCount: requiredReleased,
                optionalPendingCount: optionalPending,
                optionalCompletedCount: optionalCompleted,
                optionalFailedCount: optionalFailed,
                optionalReleasedCount: optionalReleased,
                blockingIssueCount: blockers,
                source: nameof(
                    QaParticipantAwareReadinessLoadingTerminalRegression),
                reason: "shared-envelope-terminal-matrix",
                diagnosticReason: diagnostic);
            return ActivityReadinessProgressSnapshot.Create(
                occurrence,
                state);
        }

        private static ActivityAsset CreateEnvelopeActivity()
        {
            ActivityAsset activity =
                ScriptableObject.CreateInstance<ActivityAsset>();
            var serialized = new SerializedObject(activity);
            RequireProperty(serialized, "activityId").stringValue =
                "qa.ready-progress-02a.envelope";
            RequireProperty(serialized, "activityName").stringValue =
                "QA Readiness Terminal Envelope";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(activity.HasValidActivityId,
                "Synthetic envelope Activity has an invalid identity.");
            return activity;
        }

        private static ActivityReadinessProgressSnapshot
            RequireCurrentTerminalSnapshot(
                FrameworkRuntimeHost host,
                ActivityAsset target)
        {
            Require(host != null &&
                host.CurrentGameFlowRuntime != null &&
                host.CurrentGameFlowRuntime
                    .CurrentRouteLifecycleRuntime != null,
                "Current Route Lifecycle runtime is unavailable.");
            var activityFlow = host.CurrentGameFlowRuntime
                .CurrentRouteLifecycleRuntime
                .CurrentActivityFlowRuntime;
            ActivityReadinessOccurrenceState state = null;
            bool found = activityFlow != null &&
                activityFlow.TryGetCurrentAuthorableReadinessState(
                    out state);
            Require(found &&
                state != null &&
                state.IsCurrent &&
                ReferenceEquals(state.Activity, target),
                "Current terminal readiness occurrence is unavailable.");
            return state.ProgressSnapshot;
        }

        private static async Task RequireCancelledAsync(Task<int> task)
        {
            try
            {
                await task;
                throw new InvalidOperationException(
                    "Owned cancellation task unexpectedly completed successfully.");
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static async Task RequireSignalBeforeRequestAsync(
            Task signal,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> request,
            string failureMessage)
        {
            await Task.WhenAny(signal, request.Task);
            Require(signal.IsCompleted, failureMessage);
            await signal;
        }

        private static T ResolveSinglePersistentRuntimeComponent<T>(
            FrameworkRuntimeHost host,
            string label)
            where T : Component
        {
            Require(host != null,
                "Persistent runtime component resolution requires the official host.");
            Scene runtimeScene = host.gameObject.scene;
            Require(runtimeScene.IsValid() && runtimeScene.isLoaded,
                "Official host persistent runtime scene is unavailable.");
            GameObject[] roots = runtimeScene.GetRootGameObjects();
            var matches = new List<T>();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                T[] components = roots[rootIndex] == null
                    ? Array.Empty<T>()
                    : roots[rootIndex].GetComponentsInChildren<T>(true);
                for (int componentIndex = 0;
                     componentIndex < components.Length;
                     componentIndex++)
                {
                    if (components[componentIndex] != null)
                    {
                        matches.Add(components[componentIndex]);
                    }
                }
            }

            Require(matches.Count == 1,
                $"Host runtime scene requires exactly one {label} component. actual='{matches.Count}'.");
            return matches[0];
        }

        private static void ValidateContentScene()
        {
            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                ContentScenePath);
            Require(asset != null,
                $"Q2A Activity content scene is missing. path='{ContentScenePath}'.");
            int enabledCount = 0;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int index = 0; index < scenes.Length; index++)
            {
                if (scenes[index].enabled &&
                    string.Equals(
                        scenes[index].path,
                        ContentScenePath,
                        StringComparison.Ordinal))
                {
                    enabledCount++;
                }
            }

            Require(enabledCount == 1,
                "Q2A Activity content scene must be enabled exactly once in Build Settings.");
            Scene loaded = SceneManager.GetSceneByPath(ContentScenePath);
            Require(!loaded.IsValid() || !loaded.isLoaded,
                "Q2A Activity content scene must be unloaded before execution.");
        }

        private static void RequireRecoveryGate(
            FrameworkRuntimeHost host)
        {
            // IF-TXN-03A: Transition Gate is pure residual; recovery lives on the
            // readiness composite. Terminal failure releases Transition Gate and
            // retains Activity Entry Readiness Recovery blockers only.
            Require(
                host != null &&
                !host.TransitionGateSnapshot.HasBlockers &&
                host.CurrentTransitionGateMode == TransitionGateMode.None,
                "Direct terminal failure must release the pure Transition Gate " +
                $"(mode='{host?.CurrentTransitionGateMode}' " +
                $"blockers='{host?.TransitionGateSnapshot.BlockerCount}').");

            GateSnapshot readiness = host.ActivityEntryReadinessGateSnapshot;
            Require(
                readiness.HasBlockers &&
                HasBlocker(
                    readiness,
                    GateScope.Input,
                    GateDomain.InputAcceptance) &&
                HasBlocker(
                    readiness,
                    GateScope.Interaction,
                    GateDomain.InteractionAcceptance) &&
                HasBlocker(
                    readiness,
                    GateScope.Gameplay,
                    GateDomain.GameplayAction),
                "Direct terminal failure did not retain the recovery gate.");
        }

        private static void RequireRecoveryStateCleared(
            FrameworkRuntimeHost host,
            string context)
        {
            Require(
                host != null &&
                !host.TransitionGateSnapshot.HasBlockers &&
                host.CurrentTransitionGateMode == TransitionGateMode.None &&
                !host.ActivityEntryReadinessGateSnapshot.HasBlockers,
                $"Recovery gate was not released {context}. " +
                $"transitionMode='{host?.CurrentTransitionGateMode}' " +
                $"transitionBlockers='{host?.TransitionGateSnapshot.BlockerCount}' " +
                $"readinessBlockers='{host?.ActivityEntryReadinessGateSnapshot.BlockerCount}'.");
        }

        private static bool HasBlocker(
            GateSnapshot snapshot,
            GateScope scope,
            GateDomain domain)
        {
            IReadOnlyList<GateBlocker> blockers = snapshot.Blockers;
            for (int index = 0; index < blockers.Count; index++)
            {
                if (blockers[index].Scope == scope &&
                    blockers[index].Domain == domain)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RequireAuthority(
            FrameworkRuntimeHost host,
            RouteAsset route,
            ActivityAsset activity)
        {
            Require(host != null &&
                host.State.GameFlowStarted &&
                host.State.CurrentRoute != null &&
                route != null &&
                host.State.CurrentRoute.HasSameStableId(route),
                "Initial Route authority was not restored.");
            Require((activity == null &&
                     host.State.CurrentActivity == null) ||
                (activity != null &&
                 host.State.CurrentActivity != null &&
                 host.State.CurrentActivity.HasSameStableId(activity)),
                "Initial Activity authority was not restored.");
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            Require(property != null,
                $"Required serialized property '{name}' was not found.");
            return property;
        }

        private static bool Approximately(float left, float right)
        {
            return Mathf.Abs(left - right) <= ProgressTolerance;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }


        private sealed class QaTerminalDeterminateProgressProbe :
            IDisposable
        {
            private readonly QaLoadingSurfaceVisibilityHoldAdapter _loading;
            private readonly TaskCompletionSource<
                QaLoadingPresentationEvidenceEntry> _completion =
                    new TaskCompletionSource<
                        QaLoadingPresentationEvidenceEntry>(
                            TaskCreationOptions.RunContinuationsAsynchronously);
            private int _updateCount;
            private float _lastProgress01;
            private bool _hasTerminalProgress;
            private bool _hideObserved;
            private bool _attached;

            internal QaTerminalDeterminateProgressProbe(
                QaLoadingSurfaceVisibilityHoldAdapter loading)
            {
                this._loading = loading ??
                    throw new ArgumentNullException(nameof(loading));
            }

            internal void Attach()
            {
                if (_attached)
                {
                    return;
                }

                _loading.PresentationEvidenceRecorded += HandleEvidence;
                _attached = true;
            }

            internal Task<QaLoadingPresentationEvidenceEntry>
                WaitForSubTerminalProgressAsync()
            {
                return _completion.Task;
            }

            internal DirectProgressEvidence CaptureEvidence()
            {
                return new DirectProgressEvidence(
                    _updateCount,
                    _lastProgress01,
                    _hasTerminalProgress,
                    _hideObserved);
            }

            public void Dispose()
            {
                if (!_attached)
                {
                    return;
                }

                _loading.PresentationEvidenceRecorded -= HandleEvidence;
                _attached = false;
            }

            private void HandleEvidence(
                QaLoadingPresentationEvidenceEntry entry)
            {
                if (entry.Kind ==
                        QaLoadingPresentationEvidenceKind.RequestReceived &&
                    entry.Action == LoadingSurfaceAction.Update &&
                    entry.ProgressSupported)
                {
                    _updateCount++;
                    _lastProgress01 = entry.ProgressValue01;
                    _hasTerminalProgress |= entry.ProgressValue01 >=
                        1f - ProgressTolerance;

                    if (entry.ProgressValue01 >= 0f &&
                        entry.ProgressValue01 <
                            1f - ProgressTolerance)
                    {
                        _completion.TrySetResult(entry);
                    }
                }

                _hideObserved |= entry.Kind ==
                        QaLoadingPresentationEvidenceKind.HiddenApplied &&
                    entry.Action == LoadingSurfaceAction.Hide;
            }
        }

        private readonly struct DirectProgressEvidence
        {
            internal DirectProgressEvidence(
                int updateCount,
                float lastProgress01,
                bool hasTerminalProgress,
                bool hideObserved)
            {
                UpdateCount = updateCount;
                LastProgress01 = lastProgress01;
                HasTerminalProgress = hasTerminalProgress;
                HideObserved = hideObserved;
            }

            internal int UpdateCount { get; }
            internal float LastProgress01 { get; }
            internal bool HasTerminalProgress { get; }
            internal bool HideObserved { get; }
        }

        private sealed class QaRecordingLoadingProgressReporter :
            IFrameworkLoadingProgressReporter
        {
            private readonly List<FrameworkLoadingProgress> _entries =
                new List<FrameworkLoadingProgress>();
            private readonly string _label;

            internal QaRecordingLoadingProgressReporter(string label)
            {
                this._label = string.IsNullOrWhiteSpace(label)
                    ? "Unknown"
                    : label.Trim();
            }

            public bool IsEnabled => true;
            public bool HasReportedProgress => _entries.Count > 0;
            public FrameworkLoadingProgress LastProgress =>
                _entries.Count > 0
                    ? _entries[_entries.Count - 1]
                    : FrameworkLoadingProgress.Unsupported(
                        _label,
                        "No progress recorded.");
            internal int Count => _entries.Count;
            internal bool ContainsTerminalProgress
            {
                get
                {
                    for (int index = 0; index < _entries.Count; index++)
                    {
                        if (_entries[index].Supported &&
                            _entries[index].IsDeterminate &&
                            _entries[index].Value01 >=
                            1f - ProgressTolerance)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public async Awaitable ReportAsync(
                FrameworkLoadingProgress progress)
            {
                _entries.Add(progress);
                await Task.CompletedTask;
            }
        }
    }
}

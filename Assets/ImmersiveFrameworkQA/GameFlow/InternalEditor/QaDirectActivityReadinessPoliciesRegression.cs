using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Gate;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaDirectActivityReadinessPoliciesRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run Direct Activity Readiness Policies Regression";
        private const string Prefix = "[IF_READY_04_QA_DIRECT_POLICIES]";
        private const string ContentScenePath =
            "Assets/ImmersiveFrameworkQA/GameFlow/Scenes/QA_IF_READY_04_DirectPoliciesContent.unity";
        private const string ContentSceneName = "QA_IF_READY_04_DirectPoliciesContent";
        private const string PersistentContentScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";
        private const string PersistentContentSceneName = "QA_UIGlobal";
        private const string ObserverRootName =
            "QA_IF_READY_04_DirectPoliciesPresentationObserver";
        private const int ExpectedCaseCount = 42;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required", "direct-policies-prepared", "official-host-resolved",
            "canonical-qa-hub-authority-confirmed", "activity-content-scene-validated",
            "host-transition-adapter-resolved", "host-loading-adapter-resolved",
            "temporary-transition-observer-created",
            "wait-visible-fixture-created", "wait-visible-activity-created",
            "wait-visible-evidence-reset", "wait-visible-request-started",
            "wait-visible-participant-preparing", "wait-visible-covered-boundary-observed",
            "wait-visible-release-observed-before-ready", "wait-visible-request-pending-after-release",
            "wait-visible-gate-retained-after-reveal", "wait-visible-readiness-completed-through-public-api",
            "wait-visible-request-succeeded", "wait-visible-ready-authority-confirmed",
            "wait-visible-presentation-order-confirmed", "wait-visible-gate-released",
            "wait-visible-fixture-cleaned", "wait-covered-fixture-created",
            "wait-covered-activity-created", "wait-covered-evidence-reset",
            "wait-covered-request-started", "wait-covered-participant-preparing",
            "wait-covered-presentation-retained-before-ready", "wait-covered-request-pending-before-ready",
            "wait-covered-gate-retained-while-covered",
            "wait-covered-readiness-completed-through-public-api", "wait-covered-request-succeeded",
            "wait-covered-ready-authority-confirmed", "wait-covered-presentation-released-after-ready",
            "wait-covered-presentation-order-confirmed", "wait-covered-gate-released",
            "wait-covered-fixture-cleaned", "initial-authority-confirmed",
            "temporary-transition-observer-destroyed", "host-presentation-surfaces-left-hidden",
            "presentation-evidence-cleaned"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            FrameworkRuntimeHost host = null;
            RouteAsset initialRoute = null;
            ActivityAsset initialActivity = null;
            UnityFadeCurtainEffectAdapter transition = null;
            QaLoadingSurfaceVisibilityHoldAdapter loading = null;
            QaTransitionPresentationEvidenceObserver observer = null;
            GameObject observerRoot = null;
            Exception execution = null;
            Exception waitVisibleUnwind = null;
            Exception waitCoveredUnwind = null;
            Exception waitVisibleCleanup = null;
            Exception waitCoveredCleanup = null;
            Exception observerCleanup = null;
            Exception presentationCleanup = null;
            Exception authorityVerification = null;
            PolicyRunResult waitVisible = default;
            PolicyRunResult waitCovered = default;
            PersistentRuntimeResolutionDiagnostic transitionResolution = default;
            PersistentRuntimeResolutionDiagnostic loadingResolution = default;
            ContentSceneDiagnostic contentSceneDiagnostic = default;

            try
            {
                Require(EditorApplication.isPlaying, "Direct readiness policies regression requires Play Mode.");
                cases.Complete("play-mode-required");
                QaActivityEntryPresentationEvidenceSetup.RequireDirectPoliciesPreparedForCurrentPlayMode();
                cases.Complete("direct-policies-prepared");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(out host, out string diagnostic), diagnostic);
                Require(host.State.GameFlowStarted && host.State.CurrentRoute != null,
                    "Direct readiness policies requires a started host with a current Route.");
                initialRoute = host.State.CurrentRoute;
                initialActivity = host.State.CurrentActivity;
                cases.Complete("official-host-resolved");

                GameApplicationAsset application = QaActivityEntryPresentationEvidenceSetup.ResolveCanonicalQaHubApplication();
                Require(initialRoute.HasSameIdentity(application.StartupRoute),
                    "Current Route is not the canonical QA Hub authority.");
                RequirePersistentContentAuthoring(application);
                cases.Complete("canonical-qa-hub-authority-confirmed");

                contentSceneDiagnostic = ValidateContentSceneBaseline();
                cases.Complete("activity-content-scene-validated");

                transition = ResolveSinglePersistentRuntimeComponent<UnityFadeCurtainEffectAdapter>(
                    host, application, "Transition", out transitionResolution);
                Require(transition.ConfiguredEffectKind == TransitionEffectKind.Fade && transition.HasCanvasGroup &&
                    !transition.IsVisible && transition.CurrentAlpha <= 0.001f,
                    "Host-owned Transition adapter must be configured as hidden Fade.");
                cases.Complete("host-transition-adapter-resolved");
                loading = ResolveSinglePersistentRuntimeComponent<QaLoadingSurfaceVisibilityHoldAdapter>(
                    host, application, "Loading", out loadingResolution);
                Require(loading.HasCanvasGroup && loading.HasSurfaceImage && !loading.IsVisible &&
                    loading.CurrentAlpha <= 0.001f && !loading.HideHoldActive,
                    "Host-owned Loading adapter must be hidden and have no active hold.");
                cases.Complete("host-loading-adapter-resolved");
                await Awaitable.NextFrameAsync();
                Require(!host.TransitionGateSnapshot.HasBlockers && !transition.IsVisible &&
                    !loading.IsVisible && !loading.HideHoldActive,
                    "Direct readiness policies requires a clean Transition gate and hidden host presentation surfaces before policy execution.");

                Scene observerScene = SceneManager.GetSceneByPath(initialRoute.PrimaryScenePath);
                Require(observerScene.IsValid() && observerScene.isLoaded,
                    "Canonical QA Hub primary scene is not loaded for the passive observer.");
                RequireNoObserverRoot(observerScene);
                observerRoot = new GameObject(ObserverRootName);
                SceneManager.MoveGameObjectToScene(observerRoot, observerScene);
                observer = observerRoot.AddComponent<QaTransitionPresentationEvidenceObserver>();
                observer.Bind(transition);
                cases.Complete("temporary-transition-observer-created");

                waitVisible = await RunPolicyAsync("wait-visible",
                    ActivityEntryReadinessPolicy.WaitVisible, cases, host, initialRoute,
                    initialActivity, transition, loading, observer);
                execution = waitVisible.ExecutionFailure;
                waitVisibleUnwind = waitVisible.UnwindFailure;
                waitVisibleCleanup = waitVisible.CleanupFailure;
                if (execution != null || waitVisibleUnwind != null || waitVisibleCleanup != null)
                {
                    throw new InvalidOperationException(
                        "WaitVisible policy did not complete cleanly; WaitCovered was not started.");
                }

                waitCovered = await RunPolicyAsync("wait-covered",
                    ActivityEntryReadinessPolicy.WaitCovered, cases, host, initialRoute,
                    initialActivity, transition, loading, observer);
                execution = waitCovered.ExecutionFailure;
                waitCoveredUnwind = waitCovered.UnwindFailure;
                waitCoveredCleanup = waitCovered.CleanupFailure;
                if (execution != null || waitCoveredUnwind != null || waitCoveredCleanup != null)
                {
                    throw new InvalidOperationException("WaitCovered policy did not complete cleanly.");
                }
                RequireAuthority(host, initialRoute, initialActivity);
                cases.Complete("initial-authority-confirmed");
            }
            catch (Exception exception)
            {
                if (execution == null && waitVisibleUnwind == null && waitVisibleCleanup == null &&
                    waitCoveredUnwind == null && waitCoveredCleanup == null)
                {
                    execution = exception;
                }
            }
            finally
            {
                if (observerRoot != null)
                {
                    try
                    {
                        Scene scene = observerRoot.scene;
                        UnityEngine.Object.Destroy(observerRoot);
                        await Awaitable.NextFrameAsync();
                        RequireNoObserverRoot(scene);
                        cases.TryCompleteIfNext("temporary-transition-observer-destroyed");
                    }
                    catch (Exception exception) { observerCleanup = exception; }
                }
                try
                {
                    Require(transition == null || (!transition.IsVisible && transition.CurrentAlpha <= 0.001f),
                        "Host-owned Transition adapter was not left hidden.");
                    Require(loading == null || (!loading.IsVisible && loading.CurrentAlpha <= 0.001f && !loading.HideHoldActive),
                        "Host-owned Loading adapter was not left hidden.");
                    bool anyPolicyRequestStarted = waitVisible.RequestStarted ||
                        waitCovered.RequestStarted;
                    if (host != null && anyPolicyRequestStarted)
                    {
                        RequireGateReleased(host);
                    }
                    if (anyPolicyRequestStarted)
                    {
                        RequireDedicatedContentSceneUnloaded();
                    }
                    cases.TryCompleteIfNext("host-presentation-surfaces-left-hidden");
                    if (loading != null) loading.ResetPresentationEvidence();
                    cases.TryCompleteIfNext("presentation-evidence-cleaned");
                }
                catch (Exception exception) { presentationCleanup = exception; }
                try
                {
                    if (host != null && initialRoute != null) RequireAuthority(host, initialRoute, initialActivity);
                }
                catch (Exception exception) { authorityVerification = exception; }
            }

            if (execution != null || waitVisibleUnwind != null || waitVisibleCleanup != null ||
                waitCoveredUnwind != null || waitCoveredCleanup != null ||
                observerCleanup != null || presentationCleanup != null || authorityVerification != null)
            {
                var failures = new QaFailureCollector();
                failures.Add("Execution", execution);
                failures.Add("WaitVisibleUnwind", waitVisibleUnwind);
                failures.Add("WaitVisibleCleanup", waitVisibleCleanup);
                failures.Add("WaitCoveredUnwind", waitCoveredUnwind);
                failures.Add("WaitCoveredCleanup", waitCoveredCleanup);
                failures.Add("ObserverCleanup", observerCleanup);
                failures.Add("PresentationCleanup", presentationCleanup);
                failures.Add("AuthorityVerification", authorityVerification);
                int transitionGateBlockerCount = host == null
                    ? 0
                    : host.TransitionGateSnapshot.BlockerCount;
                Debug.LogError($"{Prefix} status='Failed' execution='{Describe(execution)}' " +
                    $"waitVisibleUnwind='{Describe(waitVisibleUnwind)}' waitVisibleCleanup='{Describe(waitVisibleCleanup)}' " +
                    $"waitCoveredUnwind='{Describe(waitCoveredUnwind)}' waitCoveredCleanup='{Describe(waitCoveredCleanup)}' " +
                    $"observerCleanup='{Describe(observerCleanup)}' presentationCleanup='{Describe(presentationCleanup)}' " +
                    $"authorityVerification='{Describe(authorityVerification)}' " +
                    $"persistentContentScene='{transitionResolution.PersistentContentScenePath}' " +
                    $"hostRuntimeScene='{transitionResolution.HostRuntimeSceneName}' " +
                    $"hostRuntimeSceneHandle='{transitionResolution.HostRuntimeSceneHandle}' " +
                    $"hostRuntimeRootCount='{transitionResolution.RuntimeRootCount}' " +
                    $"transitionMatchCount='{transitionResolution.MatchCount}' " +
                    $"loadingMatchCount='{loadingResolution.MatchCount}' " +
                    $"contentScenePath='{contentSceneDiagnostic.Path}' " +
                    $"contentSceneName='{contentSceneDiagnostic.Name}' " +
                    $"contentSceneHandle='{contentSceneDiagnostic.Handle}' " +
                    $"contentSceneLoaded='{contentSceneDiagnostic.IsLoaded}' " +
                    $"contentSceneRootCount='{contentSceneDiagnostic.RootCount}' " +
                    $"contentSceneRoots='{contentSceneDiagnostic.RootNames}' " +
                    $"waitVisibleRequestStarted='{waitVisible.RequestStarted}' " +
                    $"waitCoveredRequestStarted='{waitCovered.RequestStarted}' " +
                    $"waitVisibleCleanupReadinessCompletionIssued='{waitVisible.RequestUnwind.CleanupReadinessCompletionIssued}' " +
                    $"waitVisibleRequestUnwindCompleted='{waitVisible.RequestUnwind.RequestCompleted}' " +
                    $"waitVisibleRequestUnwindKind='{waitVisible.RequestUnwind.Kind}' " +
                    $"waitVisibleRequestUnwindSucceeded='{waitVisible.RequestUnwind.Succeeded}' " +
                    $"waitVisibleRequestUnwindDestinationAuthoritative='{waitVisible.RequestUnwind.DestinationAuthoritative}' " +
                    $"waitCoveredCleanupReadinessCompletionIssued='{waitCovered.RequestUnwind.CleanupReadinessCompletionIssued}' " +
                    $"waitCoveredRequestUnwindCompleted='{waitCovered.RequestUnwind.RequestCompleted}' " +
                    $"waitCoveredRequestUnwindKind='{waitCovered.RequestUnwind.Kind}' " +
                    $"waitCoveredRequestUnwindSucceeded='{waitCovered.RequestUnwind.Succeeded}' " +
                    $"waitCoveredRequestUnwindDestinationAuthoritative='{waitCovered.RequestUnwind.DestinationAuthoritative}' " +
                    $"transitionGateBlockerCount='{transitionGateBlockerCount}' " +
                    $"transitionGateBlockers='{DescribeGateBlockers(host)}' " +
                    $"nextExpectedCase='{cases.NextExpectedOrNone()}' " +
                    $"missingCases='{cases.DescribeMissing()}' completed='{cases.DescribeCompleted()}'.");
                throw failures.ToAggregate("Direct activity readiness policies regression failed.");
            }

            cases.RequireComplete();
            Debug.Log($"{Prefix} status='Passed' cases='{ExpectedCaseCount}' waitVisible='Passed' " +
                "waitCovered='Passed' presentationSource='HostOwned' " +
                "presentationResolution='HostRuntimeScene' " +
                $"activityContentScene='{ContentSceneName}' " +
                $"waitVisibleLoadingEvidenceTotal='{waitVisible.Evidence.LoadingTotalEvidenceCount}' " +
                $"waitVisibleLoadingLifecycleEvidence='{waitVisible.Evidence.LoadingLifecycleEvidenceCount}' " +
                $"waitVisibleLoadingUpdateRequests='{waitVisible.Evidence.LoadingUpdateRequestCount}' " +
                $"waitVisibleLoadingUpdateEvidence='{waitVisible.Evidence.LoadingUpdateEvidenceCount}' " +
                $"waitVisibleTransitionEvidence='{waitVisible.Evidence.TransitionEvidenceCount}' " +
                $"waitVisibleStateChanges='{waitVisible.Evidence.TransitionStateChangedCount}' " +
                $"waitVisibleTransitioningSamples='{waitVisible.Evidence.TransitioningSampleCount}' " +
                $"waitVisibleGateBlockers='{waitVisible.Evidence.MaximumWaitingGateBlockerCount}' " +
                $"waitVisibleCleanupReadinessCompletionIssued='{waitVisible.RequestUnwind.CleanupReadinessCompletionIssued}' " +
                $"waitVisibleRequestUnwindCompleted='{waitVisible.RequestUnwind.RequestCompleted}' " +
                $"waitVisibleRequestUnwindKind='{waitVisible.RequestUnwind.Kind}' " +
                $"waitVisibleRequestUnwindSucceeded='{waitVisible.RequestUnwind.Succeeded}' " +
                $"waitVisibleRequestUnwindDestinationAuthoritative='{waitVisible.RequestUnwind.DestinationAuthoritative}' " +
                $"waitCoveredLoadingEvidenceTotal='{waitCovered.Evidence.LoadingTotalEvidenceCount}' " +
                $"waitCoveredLoadingLifecycleEvidence='{waitCovered.Evidence.LoadingLifecycleEvidenceCount}' " +
                $"waitCoveredLoadingUpdateRequests='{waitCovered.Evidence.LoadingUpdateRequestCount}' " +
                $"waitCoveredLoadingUpdateEvidence='{waitCovered.Evidence.LoadingUpdateEvidenceCount}' " +
                $"waitCoveredTransitionEvidence='{waitCovered.Evidence.TransitionEvidenceCount}' " +
                $"waitCoveredStateChanges='{waitCovered.Evidence.TransitionStateChangedCount}' " +
                $"waitCoveredTransitioningSamples='{waitCovered.Evidence.TransitioningSampleCount}' " +
                $"waitCoveredGateBlockers='{waitCovered.Evidence.MaximumWaitingGateBlockerCount}' " +
                $"waitCoveredCleanupReadinessCompletionIssued='{waitCovered.RequestUnwind.CleanupReadinessCompletionIssued}' " +
                $"waitCoveredRequestUnwindCompleted='{waitCovered.RequestUnwind.RequestCompleted}' " +
                $"waitCoveredRequestUnwindKind='{waitCovered.RequestUnwind.Kind}' " +
                $"waitCoveredRequestUnwindSucceeded='{waitCovered.RequestUnwind.Succeeded}' " +
                $"waitCoveredRequestUnwindDestinationAuthoritative='{waitCovered.RequestUnwind.DestinationAuthoritative}' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static async Task<PolicyRunResult> RunPolicyAsync(
            string prefix,
            ActivityEntryReadinessPolicy policy,
            QaCaseRegistry cases,
            FrameworkRuntimeHost host,
            RouteAsset initialRoute,
            ActivityAsset initialActivity,
            UnityFadeCurtainEffectAdapter transition,
            QaLoadingSurfaceVisibilityHoldAdapter loading,
            QaTransitionPresentationEvidenceObserver observer)
        {
            QaActivityEntryReadinessFixture fixture = null;
            Exception executionFailure = null;
            Exception unwindFailure = null;
            Exception cleanupFailure = null;
            PolicyEvidence evidence = default;
            bool requestStarted = false;
            var ownedRequest = new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                "direct-readiness-" + prefix);
            PolicyRequestUnwindResult requestUnwind = default;
            try
            {
                fixture = await QaActivityEntryReadinessFixture.CreateAsync();
                cases.Complete(prefix + "-fixture-created");
                ActivityAsset target = fixture.CreateActivity(
                    "qa.if-ready-04.direct-policies." + prefix,
                    "QA IF READY 04 " + prefix,
                    policy,
                    ActivityVisualTransitionMode.FadeWithLoading,
                    TransitionGateMode.InputInteractionAndGameplay,
                    ContentScenePath);
                cases.Complete(prefix + "-activity-created");
                loading.ResetPresentationEvidence();
                observer.ResetEvidence();
                observer.CaptureCheckpoint(prefix + "-policy-start");
                Require(observer.PresentationEvidence.Count == 2 &&
                    observer.PresentationEvidence[0].Kind == QaTransitionPresentationEvidenceKind.Baseline &&
                    observer.PresentationEvidence[0].VisualState == QaTransitionVisualState.Hidden &&
                    observer.PresentationEvidence[1].Kind == QaTransitionPresentationEvidenceKind.Checkpoint,
                    $"{prefix} Transition evidence did not establish Hidden baseline and policy-start checkpoint.");
                cases.Complete(prefix + "-evidence-reset");
                QaEvidenceCheckpoint policyStart = RequireCheckpoint(observer, prefix + "-policy-start");
                int policyStartSequence = policyStart.Sequence;

                var waitVisibleRevealObserved =
                    new TaskCompletionSource<QaTransitionPresentationEvidenceEntry>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                var waitCoveredCausalJoin =
                    new QaCausalSignalJoin<WaitCoveredVisualSignal, int>();
                Action<QaTransitionPresentationEvidenceEntry> handler = entry =>
                {
                    if (policy == ActivityEntryReadinessPolicy.WaitVisible &&
                        IsWaitVisibleRevealEntry(observer, entry, policyStartSequence,
                            fixture.Participant))
                    {
                        waitVisibleRevealObserved.TrySetResult(entry);
                    }
                    else if (policy == ActivityEntryReadinessPolicy.WaitCovered &&
                        IsWaitCoveredVisibleEntry(entry, policyStartSequence))
                    {
                        waitCoveredCausalJoin.TrySetFirst(new WaitCoveredVisualSignal(entry));
                    }
                };
                observer.PresentationEvidenceRecorded += handler;
                try
                {
                    ownedRequest.Attach(fixture.Activities.RequestActivityAsync(
                        target, nameof(QaDirectActivityReadinessPoliciesRegression) + "." + prefix,
                        prefix));
                    requestStarted = true;
                    ownedRequest.SetPhase(QaOwnedAsyncOperationPhase.Preparing);
                    cases.Complete(prefix + "-request-started");
                    Task first = await Task.WhenAny(fixture.PreparationStarted.Task, ownedRequest.Task);
                    Require(!ReferenceEquals(first, ownedRequest.Task),
                        $"{prefix} request completed before participant preparation started.");
                    Require(fixture.Participant.State == ActivityReadinessParticipantState.Preparing &&
                        fixture.PreparationStartedCount == 1 && fixture.Participant.Occurrence > 0,
                        $"{prefix} participant did not enter Preparing.");
                    if (policy == ActivityEntryReadinessPolicy.WaitCovered)
                    {
                        waitCoveredCausalJoin.TrySetSecond(fixture.Participant.Occurrence);
                    }
                    RequireDedicatedContentSceneLoaded();
                    cases.Complete(prefix + "-participant-preparing");
                    Require(transition.CurrentAlpha >= 0.999f && loading.CurrentAlpha >= 0.999f &&
                        loading.ShowRequestCount == 1,
                        $"{prefix} did not retain host-owned presentation surfaces during preparation.");
                    LogPolicyProgress(prefix, "Preparing", ownedRequest.Task, fixture, transition, loading,
                        observer, host);

                    if (policy == ActivityEntryReadinessPolicy.WaitVisible)
                    {
                        cases.Complete("wait-visible-covered-boundary-observed");
                        Task completed = await Task.WhenAny(waitVisibleRevealObserved.Task, ownedRequest.Task);
                        Require(!ReferenceEquals(completed, ownedRequest.Task),
                            $"WaitVisible request completed before reveal was observed. " +
                            DescribeWaitVisibleCoordination(ownedRequest.Task, fixture, transition, loading,
                                observer, host));
                        QaTransitionPresentationEvidenceEntry revealEntry =
                            await waitVisibleRevealObserved.Task;
                        Require(!ownedRequest.IsCompleted &&
                            fixture.Participant.State == ActivityReadinessParticipantState.Preparing &&
                            revealEntry.Kind == QaTransitionPresentationEvidenceKind.StateChanged &&
                            revealEntry.VisualState == QaTransitionVisualState.Hidden &&
                            revealEntry.Alpha <= 0.001f && transition.CurrentAlpha <= 0.001f,
                            "WaitVisible reveal signal diverged. " +
                            DescribeWaitVisibleCoordination(ownedRequest.Task, fixture, transition, loading,
                                observer, host));
                        QaEvidenceCheckpoint visible = new QaEvidenceCheckpoint("wait-visible-visible",
                            FindFirstStateChangedSequenceAfter(observer, QaTransitionVisualState.Visible, policyStartSequence));
                        QaEvidenceCheckpoint hidden = new QaEvidenceCheckpoint("wait-visible-hidden", revealEntry.Sequence);
                        visible.RequireAfter(policyStart);
                        hidden.RequireAfter(visible);
                        ownedRequest.SetPhase(QaOwnedAsyncOperationPhase.RevealObserved);
                        Require(!ownedRequest.IsCompleted &&
                            fixture.Participant.State == ActivityReadinessParticipantState.Preparing &&
                            revealEntry.Alpha <= 0.001f && transition.CurrentAlpha <= 0.001f,
                            "WaitVisible Transition passive evidence diverged. " +
                            DescribeWaitVisibleCoordination(ownedRequest.Task, fixture, transition, loading,
                                observer, host));
                        Require(loading.CurrentAlpha <= 0.001f && !loading.HideHoldActive,
                            "WaitVisible Loading release diverged after Transition reveal. " +
                            DescribeWaitVisibleCoordination(ownedRequest.Task, fixture, transition, loading,
                                observer, host));
                        LoadingEvidenceSummary loadingSummary = RequireLoadingEvidence(prefix, loading);
                        cases.Complete("wait-visible-release-observed-before-ready");
                        cases.Complete("wait-visible-request-pending-after-release");
                        int maximumWaitingGateBlockers = RequireGateStillActive(host);
                        cases.Complete("wait-visible-gate-retained-after-reveal");
                        LogPolicyProgress(prefix, "RevealObservedBeforeReady", ownedRequest.Task, fixture,
                            transition, loading, observer, host);
                        observer.CaptureCheckpoint("wait-visible-before-readiness-complete");
                        QaEvidenceCheckpoint readinessCompletion = RequireCheckpoint(observer,
                            "wait-visible-before-readiness-complete");
                        readinessCompletion.RequireAfter(hidden);
                        fixture.Participant.CompletePreparation();
                        ownedRequest.SetPhase(QaOwnedAsyncOperationPhase.ReadinessCompletionRequested);
                        cases.Complete(prefix + "-readiness-completed-through-public-api");
                        FrameworkActivityRequestResult result = await ownedRequest.AwaitTerminalAsync();
                        RequireSuccessfulRequest(prefix, result, target, host, fixture);
                        RequireDedicatedContentSceneLoaded();
                        cases.Complete(prefix + "-request-succeeded");
                        cases.Complete(prefix + "-ready-authority-confirmed");
                        Require(!transition.IsVisible && transition.CurrentAlpha <= 0.001f &&
                            transition.LastStatus == TransitionEffectStatus.Succeeded &&
                            !loading.IsVisible && loading.HideRequestCount == 1,
                            "WaitVisible did not finish host presentation hidden.");
                        loadingSummary = RequireLoadingEvidence(prefix, loading);
                        RequireWaitVisibleTransitionOrdering(observer, policyStartSequence,
                            readinessCompletion.Sequence);
                        cases.Complete("wait-visible-presentation-order-confirmed");
                        RequireGateReleased(host);
                        cases.Complete("wait-visible-gate-released");
                        evidence = CaptureEvidence(observer, loadingSummary,
                            maximumWaitingGateBlockers, policyStartSequence);
                    }
                    else
                    {
                        Task coveredCompleted = await Task.WhenAny(
                            waitCoveredCausalJoin.CompletionTask, ownedRequest.Task);
                        if (ReferenceEquals(coveredCompleted, ownedRequest.Task))
                        {
                            FrameworkActivityRequestResult earlyResult =
                                await ownedRequest.AwaitTerminalAsync();
                            if (earlyResult.Kind ==
                                FrameworkActivityRequestKind.FailedCommittedTargetReadinessCancelled)
                            {
                                throw new InvalidOperationException(
                                    "WaitCovered execution was interrupted because runtime readiness was cancelled " +
                                    "before the causal boundary completed. " +
                                    DescribeWaitCoveredEarlyCompletion(ownedRequest, waitCoveredCausalJoin,
                                        fixture, observer));
                            }

                            throw new InvalidOperationException(
                                DescribeWaitCoveredEarlyCompletion(ownedRequest, waitCoveredCausalJoin,
                                    fixture, observer));
                        }
                        QaCausalSignalJoinSnapshot<WaitCoveredVisualSignal, int> coveredSignals =
                            await waitCoveredCausalJoin.CompletionTask;
                        QaTransitionPresentationEvidenceEntry visibleEntry = coveredSignals.First.Entry;
                        QaEvidenceCheckpoint coveredVisible = coveredSignals.First.Checkpoint;
                        coveredVisible.RequireAfter(policyStart);
                        ownedRequest.SetPhase(QaOwnedAsyncOperationPhase.CoveredObserved);
                        Require(!ownedRequest.IsCompleted &&
                            fixture.Participant.State == ActivityReadinessParticipantState.Preparing &&
                            coveredSignals.Second == fixture.Participant.Occurrence &&
                            visibleEntry.Alpha >= 0.999f && transition.CurrentAlpha >= 0.999f &&
                            loading.CurrentAlpha >= 0.999f && loading.ShowRequestCount == 1,
                            "WaitCovered must retain causal presentation evidence until readiness completes.");
                        cases.Complete("wait-covered-presentation-retained-before-ready");
                        cases.Complete("wait-covered-request-pending-before-ready");
                        int maximumWaitingGateBlockers = RequireGateStillActive(host);
                        cases.Complete("wait-covered-gate-retained-while-covered");
                        LogWaitCoveredCoveredObserved(ownedRequest, fixture, transition, loading, host,
                            coveredVisible, waitCoveredCausalJoin);
                        observer.CaptureCheckpoint("wait-covered-before-readiness-complete");
                        QaEvidenceCheckpoint readinessCompletion = RequireCheckpoint(observer,
                            "wait-covered-before-readiness-complete");
                        RequireNoHiddenStateChangedAfter(observer, policyStartSequence,
                            readinessCompletion.Sequence);
                        fixture.Participant.CompletePreparation();
                        ownedRequest.SetPhase(QaOwnedAsyncOperationPhase.ReadinessCompletionRequested);
                        cases.Complete(prefix + "-readiness-completed-through-public-api");
                        LogPolicyProgress(prefix, "ReadinessCompleted", ownedRequest.Task, fixture, transition,
                            loading, observer, host);
                        FrameworkActivityRequestResult result = await ownedRequest.AwaitTerminalAsync();
                        RequireSuccessfulRequest(prefix, result, target, host, fixture);
                        RequireDedicatedContentSceneLoaded();
                        cases.Complete(prefix + "-request-succeeded");
                        cases.Complete(prefix + "-ready-authority-confirmed");
                        Require(!transition.IsVisible && transition.CurrentAlpha <= 0.001f &&
                            transition.LastStatus == TransitionEffectStatus.Succeeded &&
                            !loading.IsVisible && loading.HideRequestCount == 1,
                            "WaitCovered did not finish host presentation hidden.");
                        LoadingEvidenceSummary loadingSummary = RequireLoadingEvidence(prefix, loading);
                        RequireWaitCoveredTransitionOrdering(observer, policyStartSequence,
                            readinessCompletion.Sequence);
                        cases.Complete("wait-covered-presentation-released-after-ready");
                        cases.Complete("wait-covered-presentation-order-confirmed");
                        RequireGateReleased(host);
                        cases.Complete(prefix + "-gate-released");
                        evidence = CaptureEvidence(observer, loadingSummary,
                            maximumWaitingGateBlockers, policyStartSequence);
                    }
                }
                finally
                {
                    observer.PresentationEvidenceRecorded -= handler;
                }
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                if (fixture != null)
                {
                    try
                    {
                        requestUnwind = await UnwindPendingPolicyRequestAsync(
                            prefix, ownedRequest, fixture, host, transition, loading);
                        Require(!requestStarted || requestUnwind.RequestCompleted,
                            $"{prefix} owned request unwind did not complete.");
                    }
                    catch (Exception exception) { unwindFailure = exception; }
                    try
                    {
                        await fixture.DisposeAsync(ownedRequest);
                        Require(fixture.TargetActivityDestructionConfirmed &&
                            fixture.TargetContentProfileDestructionConfirmed &&
                            fixture.TargetContentSceneReleaseConfirmed,
                            $"{prefix} fixture did not complete staged target cleanup.");
                        RequireDedicatedContentSceneUnloaded();
                        if (executionFailure == null)
                        {
                            cases.Complete(prefix + "-fixture-cleaned");
                        }
                    }
                    catch (Exception exception) { cleanupFailure = exception; }
                }
                try { RequireAuthority(host, initialRoute, initialActivity); }
                catch (Exception exception) { cleanupFailure ??= exception; }
            }

            return new PolicyRunResult(executionFailure, unwindFailure, cleanupFailure, evidence, requestStarted,
                requestUnwind);
        }

        private static void RequirePersistentContentAuthoring(GameApplicationAsset application)
        {
            Require(application != null && application.PersistentContent != null &&
                application.PersistentContent.HasContainerScene &&
                application.PersistentContent.ContainerScene != null,
                "Canonical Game Application must define Persistent Content Container Scene.");
            Require(string.Equals(application.PersistentContent.ContainerSceneName,
                    PersistentContentSceneName, StringComparison.Ordinal) &&
                string.Equals(AssetDatabase.GetAssetPath(
                    application.PersistentContent.ContainerScene), PersistentContentScenePath,
                    StringComparison.Ordinal),
                "Canonical Persistent Content Container Scene does not match QA_UIGlobal authoring.");
            Scene sourceScene = SceneManager.GetSceneByName(PersistentContentSceneName);
            Require(!sourceScene.IsValid() || !sourceScene.isLoaded,
                "Persistent Content source scene must be unloaded after its roots are retained.");
        }

        private static ContentSceneDiagnostic ValidateContentSceneBaseline()
        {
            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ContentScenePath);
            Require(asset != null,
                $"Direct readiness content scene asset is missing. path='{ContentScenePath}'.");
            Require(string.Equals(asset.name, ContentSceneName, StringComparison.Ordinal),
                $"Direct readiness content scene asset name must be '{ContentSceneName}'. actual='{asset.name}'.");
            Require(CountEnabledBuildSettingsScenes(ContentScenePath) == 1,
                $"Direct readiness content scene must be enabled exactly once in Build Settings. path='{ContentScenePath}'.");
            ContentSceneDiagnostic diagnostic = CaptureContentSceneDiagnostic();
            Require(!diagnostic.IsLoaded,
                $"Direct readiness content scene is unexpectedly loaded. {diagnostic.Describe()}");
            return diagnostic;
        }

        private static void RequireDedicatedContentSceneLoaded()
        {
            ContentSceneDiagnostic diagnostic = CaptureContentSceneDiagnostic();
            Require(diagnostic.IsLoaded && diagnostic.RootCount == 1 &&
                string.Equals(diagnostic.RootNames, ContentSceneName, StringComparison.Ordinal) &&
                !diagnostic.HasMonoBehaviour,
                $"Dedicated direct readiness content scene did not load as neutral composition. {diagnostic.Describe()}");
        }

        private static void RequireDedicatedContentSceneUnloaded()
        {
            ContentSceneDiagnostic diagnostic = CaptureContentSceneDiagnostic();
            Require(!diagnostic.IsLoaded,
                $"Dedicated direct readiness content scene remained loaded after fixture cleanup. {diagnostic.Describe()}");
        }

        private static ContentSceneDiagnostic CaptureContentSceneDiagnostic()
        {
            Scene scene = SceneManager.GetSceneByPath(ContentScenePath);
            GameObject[] roots = scene.IsValid() && scene.isLoaded
                ? scene.GetRootGameObjects()
                : Array.Empty<GameObject>();
            var names = new List<string>();
            bool hasMonoBehaviour = false;
            for (int index = 0; index < roots.Length; index++)
            {
                GameObject root = roots[index];
                if (root == null)
                {
                    continue;
                }

                names.Add(root.name);
                if (root.GetComponentsInChildren<MonoBehaviour>(true).Length > 0)
                {
                    hasMonoBehaviour = true;
                }
            }

            Scene activeScene = SceneManager.GetActiveScene();
            return new ContentSceneDiagnostic(ContentScenePath, ContentSceneName,
                scene.IsValid() ? scene.handle.GetRawData() : 0UL,
                scene.IsValid() && scene.isLoaded, roots.Length,
                names.Count == 0 ? "<none>" : string.Join(",", names),
                hasMonoBehaviour, activeScene.name, activeScene.path);
        }

        private static int CountEnabledBuildSettingsScenes(string scenePath)
        {
            int count = 0;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int index = 0; index < scenes.Length; index++)
            {
                if (scenes[index].enabled && string.Equals(scenes[index].path, scenePath,
                    StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static string DescribeGateBlockers(FrameworkRuntimeHost host)
        {
            if (host == null)
            {
                return "<none>";
            }

            IReadOnlyList<GateBlocker> blockers = host.TransitionGateSnapshot.Blockers;
            if (blockers.Count == 0)
            {
                return "<none>";
            }

            var values = new List<string>();
            for (int index = 0; index < blockers.Count; index++)
            {
                GateBlocker blocker = blockers[index];
                values.Add($"{blocker.Scope}/{blocker.Domain}/{blocker.OwnerStableText}/" +
                    $"{blocker.Source}/{blocker.Reason}");
            }

            return string.Join(";", values);
        }

        private static T ResolveSinglePersistentRuntimeComponent<T>(
            FrameworkRuntimeHost host,
            GameApplicationAsset application,
            string label,
            out PersistentRuntimeResolutionDiagnostic diagnostic)
            where T : Component
        {
            Require(host != null, "Persistent runtime adapter resolution requires the official host.");
            Scene runtimeScene = host.gameObject.scene;
            Require(runtimeScene.IsValid() && runtimeScene.isLoaded,
                "Official host persistent runtime scene is unavailable.");
            Require(host.State.CurrentRoute == null ||
                runtimeScene.handle.GetRawData() != SceneManager.GetSceneByPath(
                    host.State.CurrentRoute.PrimaryScenePath).handle.GetRawData(),
                "Official host runtime scene must not be the current Route primary scene.");

            GameObject[] roots = runtimeScene.GetRootGameObjects();
            var matches = new List<T>();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                GameObject root = roots[rootIndex];
                if (root == null)
                {
                    continue;
                }

                T[] components = root.GetComponentsInChildren<T>(true);
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    if (components[componentIndex] != null)
                    {
                        matches.Add(components[componentIndex]);
                    }
                }
            }

            diagnostic = new PersistentRuntimeResolutionDiagnostic(
                label,
                typeof(T).Name,
                application != null && application.PersistentContent != null
                    ? AssetDatabase.GetAssetPath(application.PersistentContent.ContainerScene)
                    : string.Empty,
                application != null && application.PersistentContent != null
                    ? application.PersistentContent.ContainerSceneName
                    : string.Empty,
                runtimeScene.name,
                runtimeScene.handle.GetRawData(),
                roots.Length,
                matches.Count,
                DescribeMatches(matches));
            Require(matches.Count == 1,
                diagnostic.DescribeFailure());
            T resolved = matches[0];
            Require(resolved.gameObject.scene.handle.GetRawData() ==
                runtimeScene.handle.GetRawData(),
                diagnostic.DescribeFailure());
            return resolved;
        }

        private static string DescribeMatches<T>(IReadOnlyList<T> matches)
            where T : Component
        {
            if (matches == null || matches.Count == 0)
            {
                return "<none>";
            }

            var values = new List<string>();
            for (int index = 0; index < matches.Count; index++)
            {
                T match = matches[index];
                if (match != null)
                {
                    values.Add($"{match.gameObject.name}@{DescribeHierarchyPath(match.transform)}");
                }
            }

            return values.Count == 0 ? "<none>" : string.Join(";", values);
        }

        private static string DescribeHierarchyPath(Transform transform)
        {
            var segments = new List<string>();
            for (Transform current = transform; current != null; current = current.parent)
            {
                segments.Add(current.name);
            }

            segments.Reverse();
            return string.Join("/", segments);
        }

        private static int RequireGateStillActive(FrameworkRuntimeHost host)
        {
            GateSnapshot gate = host.TransitionGateSnapshot;
            Require(gate.HasBlockers &&
                HasGateBlocker(gate, GateScope.GameFlow, GateDomain.LifecycleRequest) &&
                HasGateBlocker(gate, GateScope.Input, GateDomain.InputAcceptance) &&
                HasGateBlocker(gate, GateScope.Interaction, GateDomain.InteractionAcceptance) &&
                HasGateBlocker(gate, GateScope.Gameplay, GateDomain.GameplayAction),
                "Activity readiness policy must retain Game Flow, Input, Interaction and Gameplay gate blockers before Ready.");
            return gate.BlockerCount;
        }

        private static void RequireGateReleased(FrameworkRuntimeHost host)
        {
            GateSnapshot gate = host.TransitionGateSnapshot;
            Require(!gate.HasBlockers,
                "Activity readiness policy must release the capability gate after Ready.");
        }

        private static void RequireSuccessfulRequest(
            string prefix,
            FrameworkActivityRequestResult result,
            ActivityAsset target,
            FrameworkRuntimeHost host,
            QaActivityEntryReadinessFixture fixture)
        {
            Require(result.Succeeded && result.DestinationAuthoritative &&
                result.ActivityTransitionMode == ActivityVisualTransitionMode.FadeWithLoading &&
                host.State.CurrentActivity != null && host.State.CurrentActivity.HasSameIdentity(target) &&
                fixture.Events.LastSnapshot.IsReady && fixture.ReadinessReadyCount == 1,
                $"{prefix} request did not complete with ready target authority. message='{result.Message}'.");
            TransitionGateDiagnostics gate = result.TransitionGateDiagnostics;
            Require(gate.Mode == TransitionGateMode.InputInteractionAndGameplay && gate.Applied &&
                gate.Released && gate.BlocksInputAcceptance && gate.BlocksInteractionAcceptance &&
                gate.BlocksGameplayAction && gate.BlockingIssueCount == 0,
                $"{prefix} request TransitionGateDiagnostics diverged. gate='{gate.GateText}'.");
            FrameworkTransitionDiagnostics transition = result.TransitionDiagnostics;
            Require(transition.Scope == TransitionScope.Activity && transition.HasBefore &&
                transition.HasAfter && transition.BlockingIssueCount == 0 &&
                transition.EffectBlockingIssueCount == 0,
                $"{prefix} request FrameworkTransitionDiagnostics diverged. diagnostics='{transition.TransitionText}'.");
        }

        private static LoadingEvidenceSummary RequireLoadingEvidence(
            string prefix,
            QaLoadingSurfaceVisibilityHoldAdapter loading)
        {
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence = loading.PresentationEvidence;
            QaLoadingPresentationEvidenceSummary summary =
                QaLoadingPresentationEvidenceGrammar.RequireValid(evidence,
                    nameof(QaDirectActivityReadinessPoliciesRegression) + "." + prefix, prefix);
            Require(loading.ShowRequestCount == summary.ShowRequestCount &&
                loading.HideRequestCount == summary.HideRequestCount &&
                loading.VisibleApplyCount == summary.VisibleApplyCount &&
                loading.HiddenApplyCount == summary.HiddenApplyCount &&
                loading.ResultEvidenceCount == summary.ResultCount,
                $"{prefix} Loading counters diverged from grammar.");
            return new LoadingEvidenceSummary(summary.TotalEvidenceCount,
                summary.LifecycleEvidenceCount, summary.UpdateRequestCount,
                summary.UpdateEvidenceCount);
        }

        private static void RequireWaitVisibleTransitionOrdering(
            QaTransitionPresentationEvidenceObserver observer,
            int policyStartSequence,
            int readinessCompletionRequestedSequence)
        {
            int visible = FindFirstStateChangedSequenceAfter(observer,
                QaTransitionVisualState.Visible, policyStartSequence);
            int hidden = FindFirstStateChangedSequenceAfter(observer,
                QaTransitionVisualState.Hidden, visible);
            Require(visible > policyStartSequence && hidden > visible &&
                hidden < readinessCompletionRequestedSequence,
                "WaitVisible Transition evidence must reveal then hide before readiness completion is requested.");
        }

        private static void RequireWaitCoveredTransitionOrdering(
            QaTransitionPresentationEvidenceObserver observer,
            int policyStartSequence,
            int readinessCompletionRequestedSequence)
        {
            int visible = FindFirstStateChangedSequenceAfter(observer,
                QaTransitionVisualState.Visible, policyStartSequence);
            int hidden = FindFirstStateChangedSequenceAfter(observer,
                QaTransitionVisualState.Hidden, visible);
            Require(visible > policyStartSequence && hidden > readinessCompletionRequestedSequence,
                "WaitCovered hidden evidence must follow readiness completion request.");
        }

        private static void RequireNoHiddenStateChangedAfter(
            QaTransitionPresentationEvidenceObserver observer,
            int policyStartSequence,
            int checkpointSequence)
        {
            int visible = FindFirstStateChangedSequenceAfter(observer,
                QaTransitionVisualState.Visible, policyStartSequence);
            int hidden = FindFirstStateChangedSequenceAfter(observer,
                QaTransitionVisualState.Hidden, visible);
            Require(visible > policyStartSequence && (hidden < 0 || hidden > checkpointSequence),
                "WaitCovered must not hide the Transition surface before readiness completion is requested.");
        }

        private static bool IsWaitVisibleRevealEntry(
            QaTransitionPresentationEvidenceObserver observer,
            QaTransitionPresentationEvidenceEntry entry,
            int policyStartSequence,
            ActivityReadinessParticipant participant)
        {
            try
            {
                if (observer == null || participant == null ||
                    entry.Kind != QaTransitionPresentationEvidenceKind.StateChanged ||
                    entry.VisualState != QaTransitionVisualState.Hidden ||
                    entry.Sequence <= policyStartSequence ||
                    participant.State != ActivityReadinessParticipantState.Preparing)
                {
                    return false;
                }

                int visibleSequence = FindFirstStateChangedSequenceAfter(observer,
                    QaTransitionVisualState.Visible, policyStartSequence);
                return visibleSequence > policyStartSequence && visibleSequence < entry.Sequence;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsWaitCoveredVisibleEntry(
            QaTransitionPresentationEvidenceEntry entry,
            int policyStartSequence)
        {
            return entry.Kind == QaTransitionPresentationEvidenceKind.StateChanged &&
                entry.VisualState == QaTransitionVisualState.Visible &&
                entry.Sequence > policyStartSequence &&
                entry.Alpha >= 0.999f;
        }

        private static string DescribeWaitCoveredEarlyCompletion(
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> ownedRequest,
            QaCausalSignalJoin<WaitCoveredVisualSignal, int> join,
            QaActivityEntryReadinessFixture fixture,
            QaTransitionPresentationEvidenceObserver observer)
        {
            QaOperationSnapshot<FrameworkActivityRequestResult> snapshot = ownedRequest.Snapshot();
            return $"WaitCovered request completed before the covered causal boundary. " +
                $"resultAvailable='{snapshot.ResultAvailable}' failure='{Describe(snapshot.Failure)}' " +
                $"join='{join.Describe()}' participantState='{fixture?.Participant.State}' " +
                $"participantOccurrence='{fixture?.Participant.Occurrence ?? 0}' " +
                $"transitionEvidence='{SummarizeTransitionEvidence(observer)}'.";
        }

        private static void LogWaitCoveredCoveredObserved(
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> ownedRequest,
            QaActivityEntryReadinessFixture fixture,
            UnityFadeCurtainEffectAdapter transition,
            QaLoadingSurfaceVisibilityHoldAdapter loading,
            FrameworkRuntimeHost host,
            QaEvidenceCheckpoint visibleCheckpoint,
            QaCausalSignalJoin<WaitCoveredVisualSignal, int> join)
        {
            Debug.Log($"{Prefix} status='Running' policy='WaitCovered' phase='CoveredObservedBeforeReady' " +
                $"requestCompleted='{ownedRequest.IsCompleted}' participantState='{fixture.Participant.State}' " +
                $"transitionAlpha='{transition.CurrentAlpha:0.###}' loadingVisible='{loading.IsVisible}' " +
                $"loadingAlpha='{loading.CurrentAlpha:0.###}' loadingHideRequests='{loading.HideRequestCount}' " +
                $"gateBlockers='{GetTransitionGateBlockerCount(host)}' " +
                $"visibleCheckpointSequence='{visibleCheckpoint.Sequence}' preparingObserved='{join.HasSecond}'.");
        }

        private static void LogPolicyProgress(
            string prefix,
            string phase,
            Task request,
            QaActivityEntryReadinessFixture fixture,
            UnityFadeCurtainEffectAdapter transition,
            QaLoadingSurfaceVisibilityHoldAdapter loading,
            QaTransitionPresentationEvidenceObserver observer,
            FrameworkRuntimeHost host)
        {
            Debug.Log($"{Prefix} status='Running' policy='{ToPolicyName(prefix)}' phase='{phase}' " +
                $"requestCompleted='{request?.IsCompleted ?? false}' " +
                $"participantState='{fixture?.Participant.State}' " +
                $"transitionVisible='{transition?.IsVisible ?? false}' " +
                $"transitionAlpha='{transition?.CurrentAlpha ?? 0f:0.###}' " +
                $"loadingVisible='{loading?.IsVisible ?? false}' " +
                $"loadingAlpha='{loading?.CurrentAlpha ?? 0f:0.###}' " +
                $"loadingShowRequests='{loading?.ShowRequestCount ?? 0}' " +
                $"loadingHideRequests='{loading?.HideRequestCount ?? 0}' " +
                $"gateBlockers='{GetTransitionGateBlockerCount(host)}' " +
                $"transitionEvidence='{observer?.PresentationEvidence.Count ?? 0}' " +
                $"loadingEvidence='{loading?.PresentationEvidence.Count ?? 0}'.");
        }

        private static string ToPolicyName(string prefix) =>
            string.Equals(prefix, "wait-visible", StringComparison.Ordinal)
                ? "WaitVisible"
                : "WaitCovered";

        private static string DescribeWaitVisibleCoordination(
            Task request,
            QaActivityEntryReadinessFixture fixture,
            UnityFadeCurtainEffectAdapter transition,
            QaLoadingSurfaceVisibilityHoldAdapter loading,
            QaTransitionPresentationEvidenceObserver observer,
            FrameworkRuntimeHost host)
        {
            return $"requestCompleted='{request?.IsCompleted ?? false}' " +
                $"participantState='{fixture?.Participant.State}' " +
                $"participantOccurrence='{fixture?.Participant.Occurrence ?? 0}' " +
                $"transitionVisible='{transition?.IsVisible ?? false}' " +
                $"transitionAlpha='{transition?.CurrentAlpha ?? 0f:0.###}' " +
                $"transitionStatus='{transition?.LastStatus}' " +
                $"loadingVisible='{loading?.IsVisible ?? false}' " +
                $"loadingAlpha='{loading?.CurrentAlpha ?? 0f:0.###}' " +
                $"loadingStatus='{loading?.LastStatus}' " +
                $"loadingHideHoldActive='{loading?.HideHoldActive ?? false}' " +
                $"loadingShowRequests='{loading?.ShowRequestCount ?? 0}' " +
                $"loadingHideRequests='{loading?.HideRequestCount ?? 0}' " +
                $"loadingVisibleApplies='{loading?.VisibleApplyCount ?? 0}' " +
                $"loadingHiddenApplies='{loading?.HiddenApplyCount ?? 0}' " +
                $"loadingResultEvidence='{loading?.ResultEvidenceCount ?? 0}' " +
                $"transitionEvidence='{SummarizeTransitionEvidence(observer)}' " +
                $"loadingEvidence='{SummarizeLoadingEvidence(loading)}' " +
                $"gateBlockers='{GetTransitionGateBlockerCount(host)}'.";
        }

        private static int GetTransitionGateBlockerCount(FrameworkRuntimeHost host) =>
            host == null ? 0 : host.TransitionGateSnapshot.BlockerCount;

        private static string SummarizeTransitionEvidence(
            QaTransitionPresentationEvidenceObserver observer)
        {
            if (observer == null)
            {
                return "<none>";
            }

            var values = new List<string>();
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence = observer.PresentationEvidence;
            for (int index = 0; index < evidence.Count; index++)
            {
                QaTransitionPresentationEvidenceEntry entry = evidence[index];
                values.Add($"{entry.Sequence}:{entry.Kind}:{entry.VisualState}");
            }

            return values.Count == 0 ? "<none>" : string.Join("|", values);
        }

        private static string SummarizeLoadingEvidence(
            QaLoadingSurfaceVisibilityHoldAdapter loading)
        {
            if (loading == null)
            {
                return "<none>";
            }

            var values = new List<string>();
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence = loading.PresentationEvidence;
            for (int index = 0; index < evidence.Count; index++)
            {
                QaLoadingPresentationEvidenceEntry entry = evidence[index];
                values.Add($"{entry.Sequence}:{entry.Kind}:{entry.Action}:{entry.Status}");
            }

            return values.Count == 0 ? "<none>" : string.Join("|", values);
        }

        private static int FindCheckpointSequence(
            QaTransitionPresentationEvidenceObserver observer,
            string label)
        {
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence = observer.PresentationEvidence;
            for (int index = 0; index < evidence.Count; index++)
            {
                if (evidence[index].Kind == QaTransitionPresentationEvidenceKind.Checkpoint &&
                    string.Equals(evidence[index].Label, label, StringComparison.Ordinal))
                {
                    return evidence[index].Sequence;
                }
            }

            return -1;
        }

        private static QaEvidenceCheckpoint RequireCheckpoint(
            QaTransitionPresentationEvidenceObserver observer,
            string label)
        {
            int sequence = FindCheckpointSequence(observer, label);
            Require(sequence > 0, $"Transition checkpoint '{label}' was not recorded.");
            return new QaEvidenceCheckpoint(label, sequence);
        }

        private static int FindFirstStateChangedSequenceAfter(
            QaTransitionPresentationEvidenceObserver observer,
            QaTransitionVisualState state,
            int afterSequence)
        {
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence = observer.PresentationEvidence;
            for (int index = 0; index < evidence.Count; index++)
            {
                if (evidence[index].Sequence > afterSequence &&
                    evidence[index].Kind == QaTransitionPresentationEvidenceKind.StateChanged &&
                    evidence[index].VisualState == state)
                {
                    return evidence[index].Sequence;
                }
            }

            return -1;
        }

        private static PolicyEvidence CaptureEvidence(
            QaTransitionPresentationEvidenceObserver observer,
            LoadingEvidenceSummary loading,
            int maximumWaitingGateBlockerCount,
            int policyStartSequence)
        {
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> transition = observer.PresentationEvidence;
            int stateChanges = 0;
            int transitioning = 0;
            for (int index = 0; index < transition.Count; index++)
            {
                if (transition[index].Kind == QaTransitionPresentationEvidenceKind.StateChanged)
                {
                    stateChanges++;
                }

                if (transition[index].VisualState == QaTransitionVisualState.Transitioning)
                {
                    transitioning++;
                }
            }

            int visible = FindFirstStateChangedSequenceAfter(observer,
                QaTransitionVisualState.Visible, policyStartSequence);
            int hidden = FindFirstStateChangedSequenceAfter(observer,
                QaTransitionVisualState.Hidden, visible);
            return new PolicyEvidence(loading.TotalEvidenceCount,
                loading.LifecycleEvidenceCount, loading.UpdateRequestCount,
                loading.UpdateEvidenceCount, transition.Count,
                stateChanges, transitioning, maximumWaitingGateBlockerCount, visible, hidden);
        }

        private static async Task<PolicyRequestUnwindResult> UnwindPendingPolicyRequestAsync(
            string prefix,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> ownedRequest,
            QaActivityEntryReadinessFixture fixture,
            FrameworkRuntimeHost host,
            UnityFadeCurtainEffectAdapter transition,
            QaLoadingSurfaceVisibilityHoldAdapter loading)
        {
            if (ownedRequest == null || !ownedRequest.HasOperation)
            {
                return PolicyRequestUnwindResult.None;
            }

            QaOperationUnwindResult<FrameworkActivityRequestResult> unwind;
            if (ownedRequest.ReachedTerminal)
            {
                unwind = await ownedRequest.UnwindAsync(null);
            }
            else
            {
                unwind = await ownedRequest.UnwindAsync(() =>
                {
                    Require(fixture != null &&
                        fixture.Participant.State == ActivityReadinessParticipantState.Preparing,
                        $"{prefix} pending request cannot be unwound because participant is not Preparing.");
                    fixture.Participant.CompletePreparation();
                    return Task.CompletedTask;
                });
            }
            if (!unwind.SucceededToAwait)
            {
                throw unwind.Failure ?? new InvalidOperationException(
                    $"{prefix} request unwind did not reach a successful task terminal state.");
            }

            FrameworkActivityRequestResult result = unwind.Result;
            Require(result.Succeeded && result.DestinationAuthoritative &&
                host.State.CurrentActivity != null &&
                host.State.CurrentActivity.HasSameIdentity(result.TargetActivity) &&
                !transition.IsVisible && transition.CurrentAlpha <= 0.001f &&
                !loading.IsVisible && !loading.HideHoldActive,
                $"{prefix} request unwind did not settle host authority or presentation.");
            RequireGateReleased(host);
            return new PolicyRequestUnwindResult(true, unwind.CompletionIssued, unwind.ReachedTerminal,
                result.Kind, result.Succeeded, result.DestinationAuthoritative);
        }

        private static bool HasGateBlocker(GateSnapshot gate, GateScope scope, GateDomain domain)
        {
            IReadOnlyList<GateBlocker> blockers = gate.Blockers;
            for (int index = 0; index < blockers.Count; index++)
            {
                if (blockers[index].Scope == scope && blockers[index].Domain == domain)
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
            Require(host.State.GameFlowStarted && host.State.CurrentRoute != null &&
                host.State.CurrentRoute.HasSameIdentity(route),
                "Game Flow Route authority diverged during direct readiness policies regression.");
            Require((activity == null && host.State.CurrentActivity == null) ||
                (activity != null && host.State.CurrentActivity != null &&
                 host.State.CurrentActivity.HasSameIdentity(activity)),
                "Game Flow Activity authority diverged during direct readiness policies regression.");
        }

        private static void RequireNoObserverRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Require(root == null || !string.Equals(root.name, ObserverRootName,
                    StringComparison.Ordinal),
                    $"Temporary observer root '{ObserverRootName}' already exists.");
            }
        }

        private static IReadOnlyList<Exception> Collect(params Exception[] candidates)
        {
            var failures = new List<Exception>();
            foreach (Exception candidate in candidates)
            {
                if (candidate != null) failures.Add(candidate);
            }
            return failures;
        }

        private static string Describe(Exception exception) => exception == null
            ? "<none>"
            : exception.GetType().Name + ":" + exception.Message.Replace("'", "\\'");

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private readonly struct PolicyRunResult
        {
            public PolicyRunResult(
                Exception executionFailure,
                Exception unwindFailure,
                Exception cleanupFailure,
                PolicyEvidence evidence,
                bool requestStarted,
                PolicyRequestUnwindResult requestUnwind)
            {
                ExecutionFailure = executionFailure;
                UnwindFailure = unwindFailure;
                CleanupFailure = cleanupFailure;
                Evidence = evidence;
                RequestStarted = requestStarted;
                RequestUnwind = requestUnwind;
            }

            public Exception ExecutionFailure { get; }
            public Exception UnwindFailure { get; }
            public Exception CleanupFailure { get; }
            public PolicyEvidence Evidence { get; }
            public bool RequestStarted { get; }
            public PolicyRequestUnwindResult RequestUnwind { get; }
        }

        private readonly struct PolicyRequestUnwindResult
        {
            public static PolicyRequestUnwindResult None => default;

            public PolicyRequestUnwindResult(bool requestExisted,
                bool cleanupReadinessCompletionIssued, bool requestCompleted,
                FrameworkActivityRequestKind kind, bool succeeded,
                bool destinationAuthoritative)
            {
                RequestExisted = requestExisted;
                CleanupReadinessCompletionIssued = cleanupReadinessCompletionIssued;
                RequestCompleted = requestCompleted;
                Kind = kind;
                Succeeded = succeeded;
                DestinationAuthoritative = destinationAuthoritative;
            }

            public bool RequestExisted { get; }
            public bool CleanupReadinessCompletionIssued { get; }
            public bool RequestCompleted { get; }
            public FrameworkActivityRequestKind Kind { get; }
            public bool Succeeded { get; }
            public bool DestinationAuthoritative { get; }
        }

        private readonly struct LoadingEvidenceSummary
        {
            public LoadingEvidenceSummary(int totalEvidenceCount, int lifecycleEvidenceCount,
                int updateRequestCount, int updateEvidenceCount)
            {
                TotalEvidenceCount = totalEvidenceCount;
                LifecycleEvidenceCount = lifecycleEvidenceCount;
                UpdateRequestCount = updateRequestCount;
                UpdateEvidenceCount = updateEvidenceCount;
            }

            public int TotalEvidenceCount { get; }
            public int LifecycleEvidenceCount { get; }
            public int UpdateRequestCount { get; }
            public int UpdateEvidenceCount { get; }
        }

        private readonly struct ContentSceneDiagnostic
        {
            public ContentSceneDiagnostic(
                string path,
                string name,
                ulong handle,
                bool isLoaded,
                int rootCount,
                string rootNames,
                bool hasMonoBehaviour,
                string activeSceneName,
                string activeScenePath)
            {
                Path = path ?? string.Empty;
                Name = name ?? string.Empty;
                Handle = handle;
                IsLoaded = isLoaded;
                RootCount = rootCount;
                RootNames = rootNames ?? string.Empty;
                HasMonoBehaviour = hasMonoBehaviour;
                ActiveSceneName = activeSceneName ?? string.Empty;
                ActiveScenePath = activeScenePath ?? string.Empty;
            }

            public string Path { get; }
            public string Name { get; }
            public ulong Handle { get; }
            public bool IsLoaded { get; }
            public int RootCount { get; }
            public string RootNames { get; }
            public bool HasMonoBehaviour { get; }
            public string ActiveSceneName { get; }
            public string ActiveScenePath { get; }

            public string Describe() =>
                $"path='{Path}' sceneName='{Name}' handle='{Handle}' isLoaded='{IsLoaded}' " +
                $"rootCount='{RootCount}' roots='{RootNames}' activeSceneName='{ActiveSceneName}' " +
                $"activeScenePath='{ActiveScenePath}'.";
        }

        private readonly struct PersistentRuntimeResolutionDiagnostic
        {
            public PersistentRuntimeResolutionDiagnostic(
                string label,
                string componentType,
                string persistentContentScenePath,
                string persistentContentSceneName,
                string hostRuntimeSceneName,
                ulong hostRuntimeSceneHandle,
                int runtimeRootCount,
                int matchCount,
                string matchedObjects)
            {
                Label = label ?? string.Empty;
                ComponentType = componentType ?? string.Empty;
                PersistentContentScenePath = persistentContentScenePath ?? string.Empty;
                PersistentContentSceneName = persistentContentSceneName ?? string.Empty;
                HostRuntimeSceneName = hostRuntimeSceneName ?? string.Empty;
                HostRuntimeSceneHandle = hostRuntimeSceneHandle;
                RuntimeRootCount = runtimeRootCount;
                MatchCount = matchCount;
                MatchedObjects = matchedObjects ?? string.Empty;
            }

            public string Label { get; }
            public string ComponentType { get; }
            public string PersistentContentScenePath { get; }
            public string PersistentContentSceneName { get; }
            public string HostRuntimeSceneName { get; }
            public ulong HostRuntimeSceneHandle { get; }
            public int RuntimeRootCount { get; }
            public string MatchedObjects { get; }
            public int MatchCount { get; }

            public string DescribeFailure() =>
                $"Persistent runtime adapter resolution failed label='{Label}' " +
                $"componentType='{ComponentType}' persistentContainerScenePath='{PersistentContentScenePath}' " +
                $"persistentContainerSceneName='{PersistentContentSceneName}' " +
                $"hostRuntimeSceneName='{HostRuntimeSceneName}' " +
                $"hostRuntimeSceneHandle='{HostRuntimeSceneHandle}' " +
                $"runtimeRootCount='{RuntimeRootCount}' matchCount='{MatchCount}' " +
                $"matches='{MatchedObjects}'.";
        }

        private readonly struct PolicyEvidence
        {
            public PolicyEvidence(
                int loadingTotalEvidenceCount,
                int loadingLifecycleEvidenceCount,
                int loadingUpdateRequestCount,
                int loadingUpdateEvidenceCount,
                int transitionEvidenceCount,
                int transitionStateChangedCount,
                int transitioningSampleCount,
                int maximumWaitingGateBlockerCount,
                int visibleSequence,
                int hiddenSequence)
            {
                LoadingTotalEvidenceCount = loadingTotalEvidenceCount;
                LoadingLifecycleEvidenceCount = loadingLifecycleEvidenceCount;
                LoadingUpdateRequestCount = loadingUpdateRequestCount;
                LoadingUpdateEvidenceCount = loadingUpdateEvidenceCount;
                TransitionEvidenceCount = transitionEvidenceCount;
                TransitionStateChangedCount = transitionStateChangedCount;
                TransitioningSampleCount = transitioningSampleCount;
                MaximumWaitingGateBlockerCount = maximumWaitingGateBlockerCount;
                VisibleSequence = visibleSequence;
                HiddenSequence = hiddenSequence;
            }

            public int LoadingTotalEvidenceCount { get; }
            public int LoadingLifecycleEvidenceCount { get; }
            public int LoadingUpdateRequestCount { get; }
            public int LoadingUpdateEvidenceCount { get; }
            public int TransitionEvidenceCount { get; }
            public int TransitionStateChangedCount { get; }
            public int TransitioningSampleCount { get; }
            public int MaximumWaitingGateBlockerCount { get; }
            public int VisibleSequence { get; }
            public int HiddenSequence { get; }
        }

        private readonly struct WaitCoveredVisualSignal
        {
            public WaitCoveredVisualSignal(QaTransitionPresentationEvidenceEntry entry)
            {
                Entry = entry;
                Checkpoint = new QaEvidenceCheckpoint("wait-covered-visible", entry.Sequence);
            }

            public QaTransitionPresentationEvidenceEntry Entry { get; }
            public QaEvidenceCheckpoint Checkpoint { get; }
        }

    }
}

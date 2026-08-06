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
    public static class QaParticipantAwareReadinessLoadingProgressRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run Participant-Aware Readiness Loading Progress Regression";
        private const string Prefix = "[QA_READY_PROGRESS_01]";
        private const string RequestReason = "participant-aware-progress";
        private const string ContentScenePath =
            "Assets/ImmersiveFrameworkQA/GameFlow/Scenes/QA_IF_READY_04_DirectPoliciesContent.unity";
        private const string ObserverRootName =
            "QA_READY_PROGRESS_01_TransitionObserver";
        private const float ProgressTolerance = 0.0005f;
        private const int ExpectedCaseCount = 32;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "direct-policies-prepared",
            "official-host-resolved",
            "canonical-authority-confirmed",
            "content-scene-validated",
            "host-transition-resolved",
            "host-loading-resolved",
            "fixture-created",
            "participant-set-created",
            "activity-created",
            "progress-plan-created",
            "evidence-reset",
            "request-started",
            "participants-preparing",
            "technical-completion-below-terminal",
            "readiness-zero-of-four-observed",
            "optional-pending-excluded-from-denominator",
            "optional-failure-observed",
            "optional-failure-did-not-advance-progress",
            "required-one-of-four-observed",
            "required-two-of-four-observed",
            "required-three-of-four-observed",
            "required-four-of-four-ready",
            "terminal-progress-before-hide",
            "hide-before-reveal",
            "request-succeeded",
            "ready-authority-confirmed",
            "gate-released",
            "determinate-grammar-confirmed",
            "participant-surface-cleaned",
            "fixture-cleaned",
            "initial-authority-restored"
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
            QaActivityEntryReadinessFixture fixture = null;
            QaParticipantAwareReadinessParticipants participants = null;
            QaLoadingSurfaceVisibilityHoldAdapter loading = null;
            UnityFadeCurtainEffectAdapter transition = null;
            QaTransitionPresentationEvidenceObserver observer = null;
            GameObject observerRoot = null;
            QaParticipantAwareProgressProbe probe = null;
            var ownedRequest =
                new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(
                    "participant-aware-readiness-progress");
            Exception executionFailure = null;
            Exception unwindFailure = null;
            Exception participantCleanupFailure = null;
            Exception fixtureCleanupFailure = null;
            Exception observerCleanupFailure = null;
            Exception authorityFailure = null;
            Exception presentationFailure = null;

            try
            {
                Require(EditorApplication.isPlaying,
                    "Participant-aware progress regression requires Play Mode.");
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
                    "Participant-aware progress requires a started official host.");
                initialRoute = host.State.CurrentRoute;
                initialActivity = host.State.CurrentActivity;
                cases.Complete("official-host-resolved");

                GameApplicationAsset application =
                    QaActivityEntryPresentationEvidenceSetup
                        .ResolveCanonicalQaHubApplication();
                Require(application != null &&
                    initialRoute.HasSameIdentity(application.StartupRoute),
                    "Current Route is not the canonical QA Hub authority.");
                cases.Complete("canonical-authority-confirmed");

                ValidateContentScene();
                cases.Complete("content-scene-validated");

                transition = ResolveSinglePersistentRuntimeComponent<
                    UnityFadeCurtainEffectAdapter>(host, "Transition");
                Require(transition.ConfiguredEffectKind ==
                        TransitionEffectKind.Fade &&
                    transition.HasCanvasGroup &&
                    !transition.IsVisible &&
                    transition.CurrentAlpha <= 0.001f,
                    "Host-owned Transition adapter is not a hidden Fade.");
                cases.Complete("host-transition-resolved");

                loading = ResolveSinglePersistentRuntimeComponent<
                    QaLoadingSurfaceVisibilityHoldAdapter>(host, "Loading");
                Require(loading.HasCanvasGroup &&
                    loading.HasSurfaceImage &&
                    loading.HasProgressPresentation &&
                    !loading.IsVisible &&
                    loading.CurrentAlpha <= 0.001f &&
                    !loading.HideHoldActive,
                    "Host-owned Loading adapter is not a hidden progress-capable surface.");
                cases.Complete("host-loading-resolved");

                fixture = await QaActivityEntryReadinessFixture.CreateAsync();
                cases.Complete("fixture-created");
                participants =
                    QaParticipantAwareReadinessParticipants.Create(fixture);
                Require(participants.Required.Count == 4 &&
                    participants.All.Count == 5 &&
                    participants.Optional.Requiredness ==
                    ActivityContentExecutionRequiredness.Optional,
                    "Participant-aware fixture must contain 4 Required and 1 Optional participant.");
                cases.Complete("participant-set-created");

                ActivityAsset target = fixture.CreateActivity(
                    "qa.ready-progress-01.activity",
                    "QA participant-aware-progress Activity",
                    ActivityEntryReadinessPolicy.WaitCovered,
                    ActivityVisualTransitionMode.FadeWithLoading,
                    TransitionGateMode.InputInteractionAndGameplay,
                    ContentScenePath);
                cases.Complete("activity-created");

                string requestSource = nameof(
                    QaParticipantAwareReadinessLoadingProgressRegression);
                int technicalStepCount = host.CurrentGameFlowRuntime
                    .CurrentRouteLifecycleRuntime
                    .PreviewActivityLoadingProgressStepCount(
                        target,
                        requestSource,
                        RequestReason);
                Require(technicalStepCount > 0,
                    "Q1 requires at least one real technical Loading step.");
                ActivityEntryLoadingProgressPlan plan =
                    ActivityEntryLoadingProgressPlan.Create(
                        technicalStepCount,
                        reserveReadinessPhase: true);
                Require(plan.HasTechnicalRange &&
                    plan.HasReadinessRange &&
                    plan.TechnicalRange.End01 < 1f &&
                    Approximately(
                        plan.TechnicalRange.End01,
                        plan.ReadinessRange.Start01),
                    "Participant-aware Loading plan did not reserve its final readiness range.");
                cases.Complete("progress-plan-created");

                Scene observerScene = SceneManager.GetSceneByPath(
                    initialRoute.PrimaryScenePath);
                Require(observerScene.IsValid() && observerScene.isLoaded,
                    "Canonical Route primary scene is unavailable for the Transition observer.");
                RequireNoObserverRoot(observerScene);
                observerRoot = new GameObject(ObserverRootName);
                SceneManager.MoveGameObjectToScene(observerRoot, observerScene);
                observer = observerRoot.AddComponent<
                    QaTransitionPresentationEvidenceObserver>();
                observer.Bind(transition);

                loading.ResetPresentationEvidence();
                observer.ResetEvidence();
                probe = new QaParticipantAwareProgressProbe(
                    loading,
                    observer);
                probe.Attach();
                cases.Complete("evidence-reset");

                ownedRequest.Attach(fixture.Activities.RequestActivityAsync(
                    target,
                    requestSource,
                    RequestReason));
                int requestStartedSequence = probe.CaptureCheckpoint();
                cases.Complete("request-started");

                await RequireSignalBeforeRequestAsync(
                    participants.AllPreparing,
                    ownedRequest,
                    "Activity request completed before all readiness participants entered Preparing.");
                participants.RequireAllPreparing();
                RequireContentSceneLoaded();
                RequireGateStillActive(host);
                cases.Complete("participants-preparing");

                ActivityReadinessProgressSnapshot zeroSnapshot =
                    RequireCurrentSnapshot(
                        host,
                        target,
                        requiredCompleted: 0,
                        requiredPending: 4,
                        optionalPending: 1,
                        optionalFailed: 0,
                        expectedReady: false);

                QaProgressSignal technicalBoundary =
                    await RequireSignalBeforeRequestAsync(
                        probe.WaitForProgressAsync(
                            plan.TechnicalRange.End01,
                            requestStartedSequence,
                            "technical-completion"),
                        ownedRequest,
                        "Activity request completed before technical Loading completion evidence.");
                Require(technicalBoundary.Entry.ProgressValue01 < 1f,
                    "Technical Loading completion reached terminal 100%.");
                cases.Complete("technical-completion-below-terminal");

                QaProgressSignal zeroOfFour =
                    await RequireSignalBeforeRequestAsync(
                        probe.WaitForProgressAsync(
                            plan.ReadinessRange.Start01,
                            technicalBoundary.Sequence,
                            "readiness-zero-of-four"),
                        ownedRequest,
                        "Activity request completed before 0/4 readiness progress evidence.");
                Require(zeroSnapshot.ReadinessRatio <= ProgressTolerance,
                    "Initial Required readiness ratio was not zero.");
                cases.Complete("readiness-zero-of-four-observed");
                Require(zeroSnapshot.RequiredCount == 4 &&
                    zeroSnapshot.OptionalCount == 1 &&
                    Approximately(
                        zeroOfFour.Entry.ProgressValue01,
                        plan.ReadinessRange.Start01),
                    "Optional pending participant changed the Required denominator.");
                cases.Complete("optional-pending-excluded-from-denominator");

                participants.FailOptional("Q1OptionalFailure");
                ActivityReadinessProgressSnapshot optionalFailedSnapshot =
                    RequireCurrentSnapshot(
                        host,
                        target,
                        requiredCompleted: 0,
                        requiredPending: 4,
                        optionalPending: 0,
                        optionalFailed: 1,
                        expectedReady: false);
                cases.Complete("optional-failure-observed");
                QaProgressSignal optionalFailure =
                    await RequireSignalBeforeRequestAsync(
                        probe.WaitForProgressAsync(
                            plan.ReadinessRange.Start01,
                            zeroOfFour.Sequence,
                            "optional-failure-no-advance"),
                        ownedRequest,
                        "Activity request completed before Optional failure progress evidence.");
                Require(optionalFailedSnapshot.ReadinessRatio <=
                        ProgressTolerance &&
                    Approximately(
                        optionalFailure.Entry.ProgressValue01,
                        zeroOfFour.Entry.ProgressValue01),
                    "Optional failure advanced Required Loading progress.");
                cases.Complete("optional-failure-did-not-advance-progress");

                QaProgressSignal one = await CompleteRequiredAndAwaitAsync(
                    0,
                    1,
                    optionalFailure.Sequence,
                    participants,
                    probe,
                    ownedRequest,
                    host,
                    target,
                    plan);
                cases.Complete("required-one-of-four-observed");

                QaProgressSignal two = await CompleteRequiredAndAwaitAsync(
                    1,
                    2,
                    one.Sequence,
                    participants,
                    probe,
                    ownedRequest,
                    host,
                    target,
                    plan);
                cases.Complete("required-two-of-four-observed");

                QaProgressSignal three = await CompleteRequiredAndAwaitAsync(
                    2,
                    3,
                    two.Sequence,
                    participants,
                    probe,
                    ownedRequest,
                    host,
                    target,
                    plan);
                cases.Complete("required-three-of-four-observed");

                participants.CompleteRequired(3);
                ActivityReadinessProgressSnapshot readySnapshot =
                    RequireCurrentSnapshot(
                        host,
                        target,
                        requiredCompleted: 4,
                        requiredPending: 0,
                        optionalPending: 0,
                        optionalFailed: 1,
                        expectedReady: true);
                Require(Approximately(readySnapshot.ReadinessRatio, 1f) &&
                    readySnapshot.IsReady &&
                    !readySnapshot.HasTerminalFailure,
                    "4/4 Required did not produce aggregate Ready.");
                QaProgressSignal terminal =
                    await RequireSignalBeforeRequestAsync(
                        probe.WaitForProgressAsync(
                            1f,
                            three.Sequence,
                            "required-four-of-four-terminal"),
                        ownedRequest,
                        "Activity request completed before terminal 100% progress evidence.");
                cases.Complete("required-four-of-four-ready");

                QaPresentationSignal loadingHide =
                    await RequireSignalBeforeRequestAsync(
                        probe.WaitForLoadingHideAsync(terminal.Sequence),
                        ownedRequest,
                        "Activity request completed before Loading Hide evidence.");
                Require(terminal.Sequence < loadingHide.Sequence,
                    "Loading Hide preceded terminal progress.");
                cases.Complete("terminal-progress-before-hide");

                QaPresentationSignal reveal =
                    await RequireSignalBeforeRequestAsync(
                        probe.WaitForTransitionRevealAsync(
                            loadingHide.Sequence),
                        ownedRequest,
                        "Activity request completed before Transition reveal evidence.");
                Require(loadingHide.Sequence < reveal.Sequence,
                    "Transition reveal preceded Loading Hide.");
                cases.Complete("hide-before-reveal");

                FrameworkActivityRequestResult result =
                    await ownedRequest.AwaitTerminalAsync();
                Require(result.Succeeded &&
                    result.DestinationAuthoritative &&
                    result.TargetActivity != null &&
                    result.TargetActivity.HasSameIdentity(target),
                    $"Participant-aware request failed. message='{result.Message}'.");
                cases.Complete("request-succeeded");
                Require(host.State.CurrentActivity != null &&
                    host.State.CurrentActivity.HasSameIdentity(target) &&
                    fixture.Events.LastSnapshot.IsReady &&
                    fixture.Events.LastSnapshot.RequiredCount == 4 &&
                    fixture.Events.LastSnapshot.OptionalCount == 1,
                    "Ready target authority or public readiness presentation diverged.");
                cases.Complete("ready-authority-confirmed");
                RequireGateReleased(host);
                cases.Complete("gate-released");

                IReadOnlyList<QaLoadingPresentationEvidenceEntry>
                    determinateUpdates =
                        QaLoadingPresentationEvidenceGrammar
                            .RequireDeterminateUpdates(
                                loading.PresentationEvidence,
                                requestSource,
                                RequestReason);
                RequireDeterminateProgression(
                    determinateUpdates,
                    technicalBoundary,
                    zeroOfFour,
                    optionalFailure,
                    one,
                    two,
                    three,
                    terminal);
                cases.Complete("determinate-grammar-confirmed");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                if (ownedRequest.HasOperation && !ownedRequest.ReachedTerminal)
                {
                    try
                    {
                        QaOperationUnwindResult<FrameworkActivityRequestResult>
                            unwind = await ownedRequest.UnwindAsync(
                                participants != null
                                    ? participants.CompleteAllPendingForUnwindAsync
                                    : null);
                        if (!unwind.SucceededToAwait)
                        {
                            throw unwind.Failure ??
                                new InvalidOperationException(
                                    "Q1 request unwind did not reach terminal.");
                        }
                    }
                    catch (Exception exception)
                    {
                        unwindFailure = exception;
                    }
                }

                probe?.Dispose();

                if (fixture != null)
                {
                    try
                    {
                        await fixture
                            .PrepareForReadinessSurfaceDestructionAsync();
                        if (participants != null)
                        {
                            await participants.DisposeAsync();
                            cases.TryCompleteIfNext(
                                "participant-surface-cleaned");
                        }
                    }
                    catch (Exception exception)
                    {
                        participantCleanupFailure = exception;
                    }

                    try
                    {
                        await fixture.DisposeAsync(ownedRequest);
                        cases.TryCompleteIfNext("fixture-cleaned");
                    }
                    catch (Exception exception)
                    {
                        fixtureCleanupFailure = exception;
                    }
                }

                if (observerRoot != null)
                {
                    try
                    {
                        Scene scene = observerRoot.scene;
                        UnityEngine.Object.Destroy(observerRoot);
                        await Awaitable.NextFrameAsync();
                        RequireNoObserverRoot(scene);
                    }
                    catch (Exception exception)
                    {
                        observerCleanupFailure = exception;
                    }
                }

                try
                {
                    if (host != null && initialRoute != null)
                    {
                        RequireAuthority(host, initialRoute, initialActivity);
                        cases.TryCompleteIfNext(
                            "initial-authority-restored");
                    }
                }
                catch (Exception exception)
                {
                    authorityFailure = exception;
                }

                try
                {
                    Require(transition == null ||
                        (!transition.IsVisible &&
                         transition.CurrentAlpha <= 0.001f),
                        "Transition surface was not left hidden.");
                    Require(loading == null ||
                        (!loading.IsVisible &&
                         loading.CurrentAlpha <= 0.001f &&
                         !loading.HideHoldActive),
                        "Loading surface was not left hidden.");
                    loading?.ResetPresentationEvidence();
                }
                catch (Exception exception)
                {
                    presentationFailure = exception;
                }
            }

            var failures = new QaFailureCollector();
            failures.Add("Execution", executionFailure);
            failures.Add("Unwind", unwindFailure);
            failures.Add("ParticipantCleanup", participantCleanupFailure);
            failures.Add("FixtureCleanup", fixtureCleanupFailure);
            failures.Add("ObserverCleanup", observerCleanupFailure);
            failures.Add("Authority", authorityFailure);
            failures.Add("Presentation", presentationFailure);
            if (failures.HasFailures)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' " +
                    $"execution='{failures.Describe("Execution")}' " +
                    $"unwind='{failures.Describe("Unwind")}' " +
                    $"participantCleanup='{failures.Describe("ParticipantCleanup")}' " +
                    $"fixtureCleanup='{failures.Describe("FixtureCleanup")}' " +
                    $"observerCleanup='{failures.Describe("ObserverCleanup")}' " +
                    $"authority='{failures.Describe("Authority")}' " +
                    $"presentation='{failures.Describe("Presentation")}' " +
                    $"nextExpectedCase='{cases.NextExpectedOrNone()}' " +
                    $"missingCases='{cases.DescribeMissing()}' " +
                    $"completed='{cases.DescribeCompleted()}'.");
                throw failures.ToAggregate(
                    "Participant-aware readiness Loading progress regression failed.");
            }

            cases.RequireComplete();
            Debug.Log(
                $"{Prefix} status='Passed' cases='{ExpectedCaseCount}' " +
                "required='4' optional='1' optionalOutcome='FailedNonBlocking' " +
                "ordering='Technical<100,0/4,1/4,2/4,3/4,4/4=100,Hide,Reveal,GateRelease' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static async Task<QaProgressSignal>
            CompleteRequiredAndAwaitAsync(
                int requiredIndex,
                int expectedCompleted,
                int afterSequence,
                QaParticipantAwareReadinessParticipants participants,
                QaParticipantAwareProgressProbe probe,
                QaOwnedAsyncOperation<FrameworkActivityRequestResult>
                    ownedRequest,
                FrameworkRuntimeHost host,
                ActivityAsset target,
                ActivityEntryLoadingProgressPlan plan)
        {
            participants.CompleteRequired(requiredIndex);
            ActivityReadinessProgressSnapshot snapshot =
                RequireCurrentSnapshot(
                    host,
                    target,
                    expectedCompleted,
                    4 - expectedCompleted,
                    optionalPending: 0,
                    optionalFailed: 1,
                    expectedReady: false);
            float expectedRatio = expectedCompleted / 4f;
            Require(Approximately(
                    snapshot.ReadinessRatio,
                    expectedRatio),
                $"Required readiness ratio diverged at '{expectedCompleted}/4'.");
            float expectedProgress = plan.ReadinessRange.Map(expectedRatio);
            QaProgressSignal signal = await RequireSignalBeforeRequestAsync(
                probe.WaitForProgressAsync(
                    expectedProgress,
                    afterSequence,
                    $"required-{expectedCompleted}-of-four"),
                ownedRequest,
                $"Activity request completed before '{expectedCompleted}/4' progress evidence.");
            Require(Approximately(
                    signal.Entry.ProgressValue01,
                    expectedProgress) &&
                signal.Entry.ProgressValue01 < 1f,
                $"Required progress diverged at '{expectedCompleted}/4'.");
            return signal;
        }

        private static ActivityReadinessProgressSnapshot
            RequireCurrentSnapshot(
                FrameworkRuntimeHost host,
                ActivityAsset target,
                int requiredCompleted,
                int requiredPending,
                int optionalPending,
                int optionalFailed,
                bool expectedReady)
        {
            Require(host != null &&
                host.CurrentGameFlowRuntime != null &&
                host.CurrentGameFlowRuntime.CurrentRouteLifecycleRuntime != null,
                "Current Route Lifecycle runtime is unavailable.");
            var activityFlow = host.CurrentGameFlowRuntime
                .CurrentRouteLifecycleRuntime
                .CurrentActivityFlowRuntime;
            ActivityReadinessOccurrenceState state = default;
            Require(activityFlow != null &&
                activityFlow.TryGetCurrentAuthorableReadinessState(out state) &&
                state.IsCurrent &&
                ReferenceEquals(state.Activity, target),
                "Current participant-aware readiness occurrence is unavailable.");

            ActivityReadinessProgressSnapshot snapshot =
                state.ProgressSnapshot;
            Require(snapshot.IsValid &&
                snapshot.RequiredCount == 4 &&
                snapshot.RequiredCompletedCount == requiredCompleted &&
                snapshot.RequiredPendingCount == requiredPending &&
                snapshot.RequiredFailedCount == 0 &&
                snapshot.RequiredReleasedCount == 0 &&
                snapshot.OptionalCount == 1 &&
                snapshot.OptionalPendingCount == optionalPending &&
                snapshot.OptionalFailedCount == optionalFailed &&
                snapshot.OptionalCompletedCount == 0 &&
                snapshot.OptionalReleasedCount == 0 &&
                snapshot.IsReady == expectedReady,
                "Participant-aware readiness snapshot counts diverged. " +
                $"required='{snapshot.RequiredCompletedCount}/{snapshot.RequiredCount}' " +
                $"requiredPending='{snapshot.RequiredPendingCount}' " +
                $"requiredFailed='{snapshot.RequiredFailedCount}' " +
                $"optionalPending='{snapshot.OptionalPendingCount}' " +
                $"optionalFailed='{snapshot.OptionalFailedCount}' " +
                $"ready='{snapshot.IsReady}'.");
            return snapshot;
        }

        private static async Task<T> RequireSignalBeforeRequestAsync<T>(
            Task<T> signal,
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> request,
            string failureMessage)
        {
            await Task.WhenAny(signal, request.Task);
            Require(signal.IsCompleted, failureMessage);
            return await signal;
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

        private static void RequireDeterminateProgression(
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> updates,
            params QaProgressSignal[] requiredSignals)
        {
            Require(updates != null && updates.Count >= requiredSignals.Length,
                "Determinate Loading evidence is incomplete.");
            float previous = -1f;
            for (int index = 0; index < updates.Count; index++)
            {
                QaLoadingPresentationEvidenceEntry entry = updates[index];
                Require(entry.ProgressSupported &&
                    entry.ProgressValue01 + ProgressTolerance >= previous &&
                    entry.ProgressValue01 >= 0f &&
                    entry.ProgressValue01 <= 1f,
                    $"Determinate Loading progress is not finite and monotonic at update '{index}'.");
                previous = entry.ProgressValue01;
            }

            for (int index = 1; index < requiredSignals.Length; index++)
            {
                Require(requiredSignals[index - 1].Sequence <
                        requiredSignals[index].Sequence,
                    "Required progress signal ordering diverged.");
            }

            QaProgressSignal terminal =
                requiredSignals[requiredSignals.Length - 1];
            for (int index = 0; index < updates.Count; index++)
            {
                QaLoadingPresentationEvidenceEntry entry = updates[index];
                if (entry.Sequence < terminal.Entry.Sequence)
                {
                    Require(entry.ProgressValue01 < 1f,
                        "Loading reached 100% before 4/4 Required became Ready.");
                }
            }
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
                $"Q1 Activity content scene is missing. path='{ContentScenePath}'.");
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
                "Q1 Activity content scene must be enabled exactly once in Build Settings.");
            Scene loaded = SceneManager.GetSceneByPath(ContentScenePath);
            Require(!loaded.IsValid() || !loaded.isLoaded,
                "Q1 Activity content scene must be unloaded before execution.");
        }

        private static void RequireContentSceneLoaded()
        {
            Scene scene = SceneManager.GetSceneByPath(ContentScenePath);
            Require(scene.IsValid() && scene.isLoaded,
                "Q1 Activity content scene did not load.");
        }

        private static void RequireGateStillActive(FrameworkRuntimeHost host)
        {
            GateSnapshot gate = host.TransitionGateSnapshot;
            Require(gate.HasBlockers &&
                HasBlocker(
                    gate,
                    GateScope.GameFlow,
                    GateDomain.LifecycleRequest) &&
                HasBlocker(
                    gate,
                    GateScope.Input,
                    GateDomain.InputAcceptance) &&
                HasBlocker(
                    gate,
                    GateScope.Interaction,
                    GateDomain.InteractionAcceptance) &&
                HasBlocker(
                    gate,
                    GateScope.Gameplay,
                    GateDomain.GameplayAction),
                "WaitCovered did not retain its capability gate while Preparing.");
        }

        private static void RequireGateReleased(FrameworkRuntimeHost host)
        {
            Require(host != null &&
                !host.TransitionGateSnapshot.HasBlockers,
                "WaitCovered capability gate was not released after Ready.");
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
            Require(host.State.GameFlowStarted &&
                host.State.CurrentRoute != null &&
                host.State.CurrentRoute.HasSameIdentity(route),
                "Initial Route authority was not restored.");
            Require((activity == null &&
                     host.State.CurrentActivity == null) ||
                (activity != null &&
                 host.State.CurrentActivity != null &&
                 host.State.CurrentActivity.HasSameIdentity(activity)),
                "Initial Activity authority was not restored.");
        }

        private static void RequireNoObserverRoot(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded,
                "Transition observer owner scene is unavailable.");
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Require(roots[index] == null ||
                    !string.Equals(
                        roots[index].name,
                        ObserverRootName,
                        StringComparison.Ordinal),
                    $"Temporary observer root '{ObserverRootName}' already exists.");
            }
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

        private sealed class QaParticipantAwareProgressProbe : IDisposable
        {
            private readonly object sync = new object();
            private readonly QaLoadingSurfaceVisibilityHoldAdapter loading;
            private readonly QaTransitionPresentationEvidenceObserver transition;
            private readonly List<QaProgressSignal> progress =
                new List<QaProgressSignal>();
            private readonly List<QaPresentationSignal> hides =
                new List<QaPresentationSignal>();
            private readonly List<QaPresentationSignal> reveals =
                new List<QaPresentationSignal>();
            private readonly List<ProgressWaiter> progressWaiters =
                new List<ProgressWaiter>();
            private readonly List<PresentationWaiter> hideWaiters =
                new List<PresentationWaiter>();
            private readonly List<PresentationWaiter> revealWaiters =
                new List<PresentationWaiter>();
            private int sequence;
            private bool transitionVisibleObserved;
            private bool attached;

            internal QaParticipantAwareProgressProbe(
                QaLoadingSurfaceVisibilityHoldAdapter loading,
                QaTransitionPresentationEvidenceObserver transition)
            {
                this.loading = loading ??
                    throw new ArgumentNullException(nameof(loading));
                this.transition = transition ??
                    throw new ArgumentNullException(nameof(transition));
            }

            internal void Attach()
            {
                if (attached)
                {
                    return;
                }

                loading.PresentationEvidenceRecorded +=
                    HandleLoadingEvidence;
                transition.PresentationEvidenceRecorded +=
                    HandleTransitionEvidence;
                attached = true;
            }

            internal int CaptureCheckpoint()
            {
                lock (sync)
                {
                    return sequence;
                }
            }

            internal Task<QaProgressSignal> WaitForProgressAsync(
                float expectedValue,
                int afterSequence,
                string label)
            {
                lock (sync)
                {
                    for (int index = 0; index < progress.Count; index++)
                    {
                        QaProgressSignal signal = progress[index];
                        if (signal.Sequence > afterSequence &&
                            Approximately(
                                signal.Entry.ProgressValue01,
                                expectedValue))
                        {
                            return Task.FromResult(signal);
                        }
                    }

                    var waiter = new ProgressWaiter(
                        expectedValue,
                        afterSequence,
                        label);
                    progressWaiters.Add(waiter);
                    return waiter.Completion.Task;
                }
            }

            internal Task<QaPresentationSignal> WaitForLoadingHideAsync(
                int afterSequence)
            {
                return WaitForPresentationAsync(
                    hides,
                    hideWaiters,
                    afterSequence,
                    "loading-hide");
            }

            internal Task<QaPresentationSignal>
                WaitForTransitionRevealAsync(int afterSequence)
            {
                return WaitForPresentationAsync(
                    reveals,
                    revealWaiters,
                    afterSequence,
                    "transition-reveal");
            }

            public void Dispose()
            {
                if (!attached)
                {
                    return;
                }

                loading.PresentationEvidenceRecorded -=
                    HandleLoadingEvidence;
                transition.PresentationEvidenceRecorded -=
                    HandleTransitionEvidence;
                attached = false;
            }

            private Task<QaPresentationSignal> WaitForPresentationAsync(
                IReadOnlyList<QaPresentationSignal> observations,
                List<PresentationWaiter> waiters,
                int afterSequence,
                string label)
            {
                lock (sync)
                {
                    for (int index = 0;
                         index < observations.Count;
                         index++)
                    {
                        if (observations[index].Sequence > afterSequence)
                        {
                            return Task.FromResult(observations[index]);
                        }
                    }

                    var waiter = new PresentationWaiter(
                        afterSequence,
                        label);
                    waiters.Add(waiter);
                    return waiter.Completion.Task;
                }
            }

            private void HandleLoadingEvidence(
                QaLoadingPresentationEvidenceEntry entry)
            {
                lock (sync)
                {
                    int current = ++sequence;
                    if (entry.Kind ==
                            QaLoadingPresentationEvidenceKind.RequestReceived &&
                        entry.Action == LoadingSurfaceAction.Update &&
                        entry.ProgressSupported)
                    {
                        var signal = new QaProgressSignal(
                            current,
                            entry);
                        progress.Add(signal);
                        CompleteProgressWaiters(signal);
                    }

                    if (entry.Kind ==
                            QaLoadingPresentationEvidenceKind.HiddenApplied &&
                        entry.Action == LoadingSurfaceAction.Hide)
                    {
                        var signal = new QaPresentationSignal(
                            current,
                            "loading-hide");
                        hides.Add(signal);
                        CompletePresentationWaiters(
                            hideWaiters,
                            signal);
                    }
                }
            }

            private void HandleTransitionEvidence(
                QaTransitionPresentationEvidenceEntry entry)
            {
                lock (sync)
                {
                    int current = ++sequence;
                    if (entry.Kind !=
                        QaTransitionPresentationEvidenceKind.StateChanged)
                    {
                        return;
                    }

                    if (entry.VisualState ==
                        QaTransitionVisualState.Visible)
                    {
                        transitionVisibleObserved = true;
                        return;
                    }

                    if (transitionVisibleObserved &&
                        entry.VisualState ==
                        QaTransitionVisualState.Hidden)
                    {
                        var signal = new QaPresentationSignal(
                            current,
                            "transition-reveal");
                        reveals.Add(signal);
                        CompletePresentationWaiters(
                            revealWaiters,
                            signal);
                    }
                }
            }

            private void CompleteProgressWaiters(
                QaProgressSignal signal)
            {
                for (int index = progressWaiters.Count - 1;
                     index >= 0;
                     index--)
                {
                    ProgressWaiter waiter = progressWaiters[index];
                    if (signal.Sequence > waiter.AfterSequence &&
                        Approximately(
                            signal.Entry.ProgressValue01,
                            waiter.ExpectedValue))
                    {
                        progressWaiters.RemoveAt(index);
                        waiter.Completion.TrySetResult(signal);
                    }
                }
            }

            private static void CompletePresentationWaiters(
                List<PresentationWaiter> waiters,
                QaPresentationSignal signal)
            {
                for (int index = waiters.Count - 1;
                     index >= 0;
                     index--)
                {
                    PresentationWaiter waiter = waiters[index];
                    if (signal.Sequence > waiter.AfterSequence)
                    {
                        waiters.RemoveAt(index);
                        waiter.Completion.TrySetResult(signal);
                    }
                }
            }

            private sealed class ProgressWaiter
            {
                internal ProgressWaiter(
                    float expectedValue,
                    int afterSequence,
                    string label)
                {
                    ExpectedValue = expectedValue;
                    AfterSequence = afterSequence;
                    Label = label ?? string.Empty;
                    Completion = new TaskCompletionSource<QaProgressSignal>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                }

                internal float ExpectedValue { get; }
                internal int AfterSequence { get; }
                internal string Label { get; }
                internal TaskCompletionSource<QaProgressSignal>
                    Completion { get; }
            }

            private sealed class PresentationWaiter
            {
                internal PresentationWaiter(
                    int afterSequence,
                    string label)
                {
                    AfterSequence = afterSequence;
                    Label = label ?? string.Empty;
                    Completion =
                        new TaskCompletionSource<QaPresentationSignal>(
                            TaskCreationOptions.RunContinuationsAsynchronously);
                }

                internal int AfterSequence { get; }
                internal string Label { get; }
                internal TaskCompletionSource<QaPresentationSignal>
                    Completion { get; }
            }
        }

        private readonly struct QaProgressSignal
        {
            internal QaProgressSignal(
                int sequence,
                QaLoadingPresentationEvidenceEntry entry)
            {
                Sequence = sequence;
                Entry = entry;
            }

            internal int Sequence { get; }
            internal QaLoadingPresentationEvidenceEntry Entry { get; }
        }

        private readonly struct QaPresentationSignal
        {
            internal QaPresentationSignal(
                int sequence,
                string label)
            {
                Sequence = sequence;
                Label = label ?? string.Empty;
            }

            internal int Sequence { get; }
            internal string Label { get; }
        }
    }
}

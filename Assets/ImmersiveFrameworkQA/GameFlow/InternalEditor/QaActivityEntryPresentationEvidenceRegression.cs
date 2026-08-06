using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Loading;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaActivityEntryPresentationEvidenceRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Game Flow/Run Activity Entry Presentation Evidence Regression";
        private const string Prefix = "[IF_READY_04_QA_PRESENTATION_EVIDENCE]";
        private const string FixtureRootName = "QA_IF_READY_04_PresentationEvidence";
        private const int ExpectedCaseCount = 26;

        private static readonly string[] ExpectedCases =
        {
            "play-mode-required",
            "prepared-for-current-play-mode",
            "official-host-resolved",
            "initial-authority-captured",
            "canonical-qa-hub-route-confirmed",
            "runtime-fixture-scene-resolved",
            "runtime-fixture-root-created",
            "synthetic-transition-surface-created",
            "synthetic-loading-surface-created",
            "temporary-transition-observer-created",
            "presentation-evidence-reset",
            "transition-show-result-succeeded",
            "transition-visible-settle-recorded",
            "transition-hide-result-succeeded",
            "transition-hidden-settle-recorded",
            "transition-passive-state-change-recorded",
            "loading-show-result-succeeded",
            "loading-show-evidence-recorded",
            "loading-hide-result-succeeded",
            "loading-hide-evidence-recorded",
            "surface-final-state-hidden",
            "gameflow-authority-preserved",
            "runtime-presentation-fixture-destroyed",
            "runtime-presentation-components-destroyed",
            "evidence-cleaned",
            "gameflow-authority-preserved-after-cleanup"
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
            QaLoadingSurfaceVisibilityHoldAdapter loading = null;
            UnityFadeCurtainEffectAdapter transition = null;
            QaTransitionPresentationEvidenceObserver observer = null;
            GameObject fixtureRoot = null;
            GameObject transitionInstance = null;
            GameObject loadingInstance = null;
            RouteAsset initialRoute = null;
            ActivityAsset initialActivity = null;
            Scene fixtureScene = default;
            LoadingSurfaceRequest loadingHideRequest = default;
            TransitionEffectRequest transitionHideRequest = default;
            bool loadingHideRequestCreated = false;
            bool transitionHideRequestCreated = false;
            int transitionEvidenceCount = 0;
            int loadingEvidenceCount = 0;
            int loadingLifecycleEvidenceCount = 0;
            int loadingUpdateRequestCount = 0;
            int loadingUpdateEvidenceCount = 0;
            int transitionStateChanges = 0;
            int transitioningSamples = 0;
            int visibleSamples = 0;
            int hiddenSamples = 0;
            bool loadingEvidenceReset = false;

            try
            {
                Require(EditorApplication.isPlaying, "Presentation evidence regression requires Play Mode.");
                cases.Complete("play-mode-required");
                QaActivityEntryPresentationEvidenceSetup.RequirePreparedForCurrentPlayMode();
                cases.Complete("prepared-for-current-play-mode");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(out host, out string diagnostic), diagnostic);
                Require(host.State.GameFlowStarted, "Game Flow is not started.");
                cases.Complete("official-host-resolved");
                initialRoute = host.State.CurrentRoute;
                initialActivity = host.State.CurrentActivity;
                Require(initialRoute != null, "Presentation evidence regression requires a current Route.");
                cases.Complete("initial-authority-captured");

                GameApplicationAsset canonicalApplication = QaActivityEntryPresentationEvidenceSetup.ResolveCanonicalQaHubApplication();
                Require(initialRoute.HasSameIdentity(canonicalApplication.StartupRoute),
                    "Current Route is not the canonical QA Hub Route selected by the prepared Game Application.");
                cases.Complete("canonical-qa-hub-route-confirmed");
                fixtureScene = SceneManager.GetSceneByPath(initialRoute.PrimaryScenePath);
                Require(fixtureScene.IsValid() && fixtureScene.isLoaded,
                    $"Current Route Primary Scene is not loaded. path='{initialRoute.PrimaryScenePath}'.");
                cases.Complete("runtime-fixture-scene-resolved");

                RequireNoFixtureRoot(fixtureScene);
                fixtureRoot = new GameObject(FixtureRootName);
                SceneManager.MoveGameObjectToScene(fixtureRoot, fixtureScene);
                cases.Complete("runtime-fixture-root-created");

                transitionInstance = CreateSyntheticTransitionSurface(fixtureRoot.transform);
                transition = transitionInstance.GetComponent<UnityFadeCurtainEffectAdapter>();
                Require(transition != null && transition.ConfiguredEffectKind == TransitionEffectKind.Fade && transition.HasCanvasGroup,
                    "Runtime Transition adapter is not configured for Fade with CanvasGroup.");
                Require(!transition.IsVisible && transition.CurrentAlpha <= 0.001f,
                    "Synthetic Transition surface must start hidden.");
                cases.Complete("synthetic-transition-surface-created");

                loadingInstance = CreateSyntheticLoadingSurface(fixtureRoot.transform);
                loading = loadingInstance.GetComponent<QaLoadingSurfaceVisibilityHoldAdapter>();
                Require(loading != null && loading.HasCanvasGroup && loading.HasSurfaceImage && !loading.IsVisible &&
                    loading.CurrentAlpha <= 0.001f && !loading.HideHoldActive,
                    "Synthetic Loading surface must start hidden with a CanvasGroup and Image.");
                cases.Complete("synthetic-loading-surface-created");
                Require(!transition.IsVisible && transition.CurrentAlpha <= 0.001f &&
                    !loading.IsVisible && loading.CurrentAlpha <= 0.001f && !loading.HideHoldActive,
                    "Runtime presentation surfaces must start hidden with no active Loading hold.");

                observer = fixtureRoot.AddComponent<QaTransitionPresentationEvidenceObserver>();
                observer.Bind(transition);
                Require(observer.IsBound, "Transition presentation observer did not bind.");
                cases.Complete("temporary-transition-observer-created");
                loading.ResetPresentationEvidence();
                observer.ResetEvidence();
                observer.CaptureCheckpoint("pre-exercise");
                Require(loading.PresentationEvidence.Count == 0 && observer.PresentationEvidence.Count == 2 &&
                    observer.PresentationEvidence[0].Kind == QaTransitionPresentationEvidenceKind.Baseline &&
                    observer.PresentationEvidence[0].VisualState == QaTransitionVisualState.Hidden &&
                    observer.PresentationEvidence[1].Kind == QaTransitionPresentationEvidenceKind.Checkpoint,
                    "Presentation evidence reset did not establish the expected baseline.");
                cases.Complete("presentation-evidence-reset");

                TransitionOperationId operationId = TransitionOperationId.From("qa.if-ready-04.presentation-evidence");
                TransitionEffectRequest showRequest = TransitionEffectRequest.Required(
                    "qa.if-ready-04.presentation.fade", TransitionEffectKind.Fade, operationId,
                    TransitionKind.ActivitySwitch, TransitionPhase.OperationOpened,
                    nameof(QaActivityEntryPresentationEvidenceRegression), "presentation-evidence-show");
                transitionHideRequest = TransitionEffectRequest.Required(
                    "qa.if-ready-04.presentation.fade", TransitionEffectKind.Fade, operationId,
                    TransitionKind.ActivitySwitch, TransitionPhase.OperationClosed,
                    nameof(QaActivityEntryPresentationEvidenceRegression), "presentation-evidence-hide");
                transitionHideRequestCreated = true;

                TransitionEffectResult transitionShowResult =
                    await transition.ExecuteAsync(showRequest);
                RequireTransitionSettled(
                    "show",
                    transitionShowResult,
                    transition,
                    observer,
                    shouldBeVisible: true);
                cases.Complete("transition-show-result-succeeded");
                observer.CaptureCheckpoint("post-show-settled");
                RequireLatestCheckpoint(
                    observer,
                    QaTransitionVisualState.Visible,
                    adapterVisible: true,
                    "post-show-settled");
                cases.Complete("transition-visible-settle-recorded");

                TransitionEffectResult transitionHideResult =
                    await transition.ExecuteAsync(transitionHideRequest);
                RequireTransitionSettled(
                    "hide",
                    transitionHideResult,
                    transition,
                    observer,
                    shouldBeVisible: false);
                cases.Complete("transition-hide-result-succeeded");
                observer.CaptureCheckpoint("post-hide-settled");
                RequireLatestCheckpoint(
                    observer,
                    QaTransitionVisualState.Hidden,
                    adapterVisible: false,
                    "post-hide-settled");
                cases.Complete("transition-hidden-settle-recorded");
                RequireTransitionCheckpointOrdering(observer);
                Require(CountTransitionEvidence(observer, QaTransitionPresentationEvidenceKind.StateChanged) >= 1,
                    "Transition observer did not record a passive StateChanged entry during the owned operations.");
                cases.Complete("transition-passive-state-change-recorded");

                LoadingSurfaceRequest showLoadingRequest = LoadingSurfaceRequest.Show(
                    "QA Presentation Evidence", "show", nameof(QaActivityEntryPresentationEvidenceRegression),
                    "presentation-evidence-show");
                loadingHideRequest = LoadingSurfaceRequest.Hide(
                    "QA Presentation Evidence", "hide", nameof(QaActivityEntryPresentationEvidenceRegression),
                    "presentation-evidence-hide");
                loadingHideRequestCreated = true;

                LoadingSurfaceResult loadingShowResult = loading.Show(showLoadingRequest);
                Require(loadingShowResult.Succeeded, loadingShowResult.Message);
                cases.Complete("loading-show-result-succeeded");
                RequireLoadingShowPrefix(loading, showLoadingRequest);
                cases.Complete("loading-show-evidence-recorded");

                LoadingSurfaceResult loadingHideResult = loading.Hide(loadingHideRequest);
                Require(loadingHideResult.Succeeded, loadingHideResult.Message);
                cases.Complete("loading-hide-result-succeeded");
                LoadingProtocolSummary loadingSummary = RequireLoadingProtocol(
                    loading,
                    showLoadingRequest,
                    loadingHideRequest);
                cases.Complete("loading-hide-evidence-recorded");

                Require(!transition.IsVisible && transition.CurrentAlpha <= 0.001f &&
                    !loading.IsVisible && loading.CurrentAlpha <= 0.001f && !loading.HideHoldActive,
                    "Runtime presentation surfaces did not finish hidden.");
                cases.Complete("surface-final-state-hidden");
                RequireAuthority(host, initialRoute, initialActivity);
                cases.Complete("gameflow-authority-preserved");

                transitionEvidenceCount = observer.PresentationEvidence.Count;
                loadingEvidenceCount = loadingSummary.TotalEvidenceCount;
                loadingLifecycleEvidenceCount = loadingSummary.LifecycleEvidenceCount;
                loadingUpdateRequestCount = loadingSummary.UpdateRequestCount;
                loadingUpdateEvidenceCount = loadingSummary.UpdateEvidenceCount;
                transitionStateChanges = CountTransitionEvidence(
                    observer,
                    QaTransitionPresentationEvidenceKind.StateChanged);
                transitioningSamples = CountTransitionVisualState(
                    observer,
                    QaTransitionVisualState.Transitioning);
                visibleSamples = CountTransitionVisualState(observer, QaTransitionVisualState.Visible);
                hiddenSamples = CountTransitionVisualState(observer, QaTransitionVisualState.Hidden);
            }
            catch (Exception exception)
            {
                failures.Add("Execution", exception);
            }
            finally
            {
                if (transition != null && transitionHideRequestCreated && transition.IsVisible)
                {
                    try
                    {
                        Require((await transition.ExecuteAsync(transitionHideRequest)).Succeeded,
                            "Transition cleanup hide failed.");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("SurfaceRestoration", exception);
                    }
                }

                if (loading != null && loadingHideRequestCreated && loading.IsVisible)
                {
                    try
                    {
                        Require(loading.Hide(loadingHideRequest).Succeeded,
                            "Loading cleanup hide failed.");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("SurfaceRestoration", exception);
                    }
                }

                try
                {
                    if (transition != null)
                    {
                        Require(!transition.IsVisible && transition.CurrentAlpha <= 0.001f,
                            "Transition surface cleanup did not finish hidden.");
                    }

                    if (loading != null)
                    {
                        Require(!loading.IsVisible && loading.CurrentAlpha <= 0.001f && !loading.HideHoldActive,
                            "Loading surface cleanup did not finish hidden with no active hold.");
                    }
                }
                catch (Exception exception)
                {
                    failures.Add("SurfaceRestoration", exception);
                }

                if (loading != null)
                {
                    try
                    {
                        loading.ResetPresentationEvidence();
                    }
                    catch (Exception exception)
                    {
                        failures.Add("LoadingEvidenceCleanup", exception);
                    }

                    Require(loading.PresentationEvidence.Count == 0,
                        "Loading presentation evidence cleanup did not clear all entries.");
                    loadingEvidenceReset = true;
                }

                if (fixtureRoot != null)
                {
                    try
                    {
                        Scene rootScene = fixtureRoot.scene;
                        UnityEngine.Object.Destroy(fixtureRoot);
                        // Unity destruction is committed at the next player-loop boundary.
                        await Awaitable.NextFrameAsync();
                        RequireNoFixtureRoot(rootScene);
                        cases.TryCompleteIfNext("runtime-presentation-fixture-destroyed");
                        Require(fixtureRoot == null && transitionInstance == null && loadingInstance == null &&
                            transition == null && loading == null && observer == null,
                            "Runtime presentation adapters were not destroyed with their fixture root.");
                        cases.TryCompleteIfNext("runtime-presentation-components-destroyed");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("RuntimeFixtureDestruction", exception);
                    }
                }

                if (loadingEvidenceReset)
                {
                    cases.TryCompleteIfNext("evidence-cleaned");
                }

                if (host != null && initialRoute != null)
                {
                    try
                    {
                        RequireAuthority(host, initialRoute, initialActivity);
                    }
                    catch (Exception exception)
                    {
                        failures.Add("AuthorityVerification", exception);
                    }

                    cases.TryCompleteIfNext("gameflow-authority-preserved-after-cleanup");
                }
            }

            if (failures.HasFailures)
            {
                Debug.LogError($"{Prefix} status='Failed' " +
                    $"execution='{Escape(failures.Describe("Execution"))}' " +
                    $"surfaceRestoration='{Escape(failures.Describe("SurfaceRestoration"))}' " +
                    $"loadingEvidenceCleanup='{Escape(failures.Describe("LoadingEvidenceCleanup"))}' " +
                    $"runtimeFixtureDestruction='{Escape(failures.Describe("RuntimeFixtureDestruction"))}' " +
                    $"authorityVerification='{Escape(failures.Describe("AuthorityVerification"))}' " +
                    $"nextExpectedCase='{cases.NextExpectedOrNone()}' " +
                    $"missingCases='{cases.DescribeMissing()}' " +
                    $"completed='{cases.DescribeCompleted()}'.");
                throw failures.ToAggregate(
                    "Activity entry presentation evidence regression failed.");
            }

            cases.RequireComplete();
            Debug.Log($"{Prefix} status='Passed' cases='{cases.Count}' fixtureMode='RuntimeSynthetic' fixtureScene='{fixtureScene.path}' " +
                $"transitionEvidence='{transitionEvidenceCount}' loadingEvidence='{loadingEvidenceCount}' " +
                $"loadingLifecycleEvidence='{loadingLifecycleEvidenceCount}' " +
                $"loadingUpdateRequests='{loadingUpdateRequestCount}' " +
                $"loadingUpdateEvidence='{loadingUpdateEvidenceCount}' " +
                $"transitionStateChanges='{transitionStateChanges}' transitioningSamples='{transitioningSamples}' " +
                $"visibleSamples='{visibleSamples}' hiddenSamples='{hiddenSamples}' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static void RequireTransitionSettled(
            string phase,
            TransitionEffectResult result,
            UnityFadeCurtainEffectAdapter transition,
            QaTransitionPresentationEvidenceObserver observer,
            bool shouldBeVisible)
        {
            bool alphaSettled = shouldBeVisible
                ? transition.CurrentAlpha >= 0.999f
                : transition.CurrentAlpha <= 0.001f;
            Require(result.Succeeded && transition.IsVisible == shouldBeVisible && alphaSettled &&
                transition.LastStatus == TransitionEffectStatus.Succeeded,
                $"Transition {phase} settle failed: resultStatus='{result.Status}' " +
                $"adapterVisible='{transition.IsVisible}' currentAlpha='{transition.CurrentAlpha:0.###}' " +
                $"lastStatus='{transition.LastStatus}' settledVisible='{observer.SettledVisibleCount}' " +
                $"settledHidden='{observer.SettledHiddenCount}' transitioning='{observer.TransitioningCount}' " +
                $"evidenceCount='{observer.PresentationEvidence.Count}' " +
                $"evidence='{SummarizeTransitionEvidence(observer)}'.");
        }

        private static void RequireLatestCheckpoint(
            QaTransitionPresentationEvidenceObserver observer,
            QaTransitionVisualState expectedState,
            bool adapterVisible,
            string label)
        {
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence =
                observer.PresentationEvidence;
            Require(evidence.Count > 0, "Transition checkpoint evidence is empty.");
            QaTransitionPresentationEvidenceEntry entry = evidence[evidence.Count - 1];
            bool alphaSettled = expectedState == QaTransitionVisualState.Visible
                ? entry.Alpha >= 0.999f
                : entry.Alpha <= 0.001f;
            Require(entry.Kind == QaTransitionPresentationEvidenceKind.Checkpoint &&
                entry.VisualState == expectedState && entry.AdapterVisible == adapterVisible &&
                alphaSettled && string.Equals(entry.Label, label, StringComparison.Ordinal),
                $"Transition checkpoint '{label}' diverged: sequence='{entry.Sequence}' " +
                $"kind='{entry.Kind}' state='{entry.VisualState}' adapterVisible='{entry.AdapterVisible}' " +
                $"alpha='{entry.Alpha:0.###}' label='{entry.Label}'.");
        }

        private static void RequireTransitionCheckpointOrdering(
            QaTransitionPresentationEvidenceObserver observer)
        {
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence =
                observer.PresentationEvidence;
            QaEvidenceCheckpoint baseline = FindTransitionEvidenceCheckpoint(
                evidence, QaTransitionPresentationEvidenceKind.Baseline, "baseline");
            QaEvidenceCheckpoint preExercise = FindTransitionEvidenceCheckpoint(
                evidence, QaTransitionPresentationEvidenceKind.Checkpoint, "pre-exercise");
            QaEvidenceCheckpoint postShow = FindTransitionEvidenceCheckpoint(
                evidence, QaTransitionPresentationEvidenceKind.Checkpoint, "post-show-settled");
            QaEvidenceCheckpoint postHide = FindTransitionEvidenceCheckpoint(
                evidence, QaTransitionPresentationEvidenceKind.Checkpoint, "post-hide-settled");
            baseline.RequireBefore(preExercise);
            preExercise.RequireBefore(postShow);
            postShow.RequireBefore(postHide);
        }

        private static QaEvidenceCheckpoint FindTransitionEvidenceCheckpoint(
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence,
            QaTransitionPresentationEvidenceKind kind,
            string label)
        {
            for (int index = 0; index < evidence.Count; index++)
            {
                QaTransitionPresentationEvidenceEntry entry = evidence[index];
                if (entry.Kind == kind && string.Equals(entry.Label, label, StringComparison.Ordinal))
                {
                    return new QaEvidenceCheckpoint(label, entry.Sequence);
                }
            }

            throw new InvalidOperationException(
                $"Transition evidence checkpoint '{label}' kind='{kind}' was not found.");
        }

        private static int CountTransitionEvidence(
            QaTransitionPresentationEvidenceObserver observer,
            QaTransitionPresentationEvidenceKind kind)
        {
            int count = 0;
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence =
                observer.PresentationEvidence;
            for (int index = 0; index < evidence.Count; index++)
            {
                if (evidence[index].Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountTransitionVisualState(
            QaTransitionPresentationEvidenceObserver observer,
            QaTransitionVisualState state)
        {
            int count = 0;
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence =
                observer.PresentationEvidence;
            for (int index = 0; index < evidence.Count; index++)
            {
                if (evidence[index].VisualState == state)
                {
                    count++;
                }
            }

            return count;
        }

        private static string SummarizeTransitionEvidence(
            QaTransitionPresentationEvidenceObserver observer)
        {
            var entries = new List<string>();
            IReadOnlyList<QaTransitionPresentationEvidenceEntry> evidence =
                observer.PresentationEvidence;
            for (int index = 0; index < evidence.Count; index++)
            {
                QaTransitionPresentationEvidenceEntry entry = evidence[index];
                entries.Add($"{entry.Sequence}:{entry.Kind}:{entry.VisualState}:{entry.Alpha:0.###}");
            }

            return string.Join("|", entries);
        }

        private static void RequireLoadingShowPrefix(
            QaLoadingSurfaceVisibilityHoldAdapter loading,
            LoadingSurfaceRequest showRequest)
        {
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence =
                loading.PresentationEvidence;
            Require(evidence.Count == 3,
                $"Loading Show evidence expected one triplet. actual='{evidence.Count}'.");
            RequireLoadingEntry(
                evidence,
                0,
                QaLoadingPresentationEvidenceKind.RequestReceived,
                showRequest,
                actualVisible: false,
                LoadingSurfaceResultStatus.Unknown,
                "Show request");
            RequireLoadingEntry(
                evidence,
                1,
                QaLoadingPresentationEvidenceKind.VisibleApplied,
                showRequest,
                actualVisible: true,
                LoadingSurfaceResultStatus.Unknown,
                "Show visible apply");
            RequireLoadingEntry(
                evidence,
                2,
                QaLoadingPresentationEvidenceKind.ResultRecorded,
                showRequest,
                actualVisible: true,
                LoadingSurfaceResultStatus.Succeeded,
                "Show result");
            Require(loading.ShowRequestCount == 1 && loading.UpdateRequestCount == 0 &&
                loading.HideRequestCount == 0 && loading.VisibleApplyCount == 1 &&
                loading.HiddenApplyCount == 0 && loading.ResultEvidenceCount == 1,
                "Loading Show counters diverged from the direct synthetic protocol.");
        }

        private static LoadingProtocolSummary RequireLoadingProtocol(
            QaLoadingSurfaceVisibilityHoldAdapter loading,
            LoadingSurfaceRequest showRequest,
            LoadingSurfaceRequest hideRequest)
        {
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence =
                loading.PresentationEvidence;
            Require(evidence.Count == 6,
                $"Direct synthetic Loading protocol expected six entries. actual='{evidence.Count}'.");

            RequireLoadingEntry(
                evidence,
                0,
                QaLoadingPresentationEvidenceKind.RequestReceived,
                showRequest,
                actualVisible: false,
                LoadingSurfaceResultStatus.Unknown,
                "Show request");
            RequireLoadingEntry(
                evidence,
                1,
                QaLoadingPresentationEvidenceKind.VisibleApplied,
                showRequest,
                actualVisible: true,
                LoadingSurfaceResultStatus.Unknown,
                "Show visible apply");
            RequireLoadingEntry(
                evidence,
                2,
                QaLoadingPresentationEvidenceKind.ResultRecorded,
                showRequest,
                actualVisible: true,
                LoadingSurfaceResultStatus.Succeeded,
                "Show result");
            RequireLoadingEntry(
                evidence,
                3,
                QaLoadingPresentationEvidenceKind.RequestReceived,
                hideRequest,
                actualVisible: true,
                LoadingSurfaceResultStatus.Unknown,
                "Hide request");
            RequireLoadingEntry(
                evidence,
                4,
                QaLoadingPresentationEvidenceKind.HiddenApplied,
                hideRequest,
                actualVisible: false,
                LoadingSurfaceResultStatus.Unknown,
                "Hide hidden apply");
            RequireLoadingEntry(
                evidence,
                5,
                QaLoadingPresentationEvidenceKind.ResultRecorded,
                hideRequest,
                actualVisible: false,
                LoadingSurfaceResultStatus.Succeeded,
                "Hide result");

            Require(loading.ShowRequestCount == 1 && loading.UpdateRequestCount == 0 &&
                loading.HideRequestCount == 1 && loading.VisibleApplyCount == 1 &&
                loading.HiddenApplyCount == 1 && loading.ResultEvidenceCount == 2,
                "Loading counters diverged from the direct synthetic Show/Hide protocol.");

            return new LoadingProtocolSummary(
                totalEvidenceCount: evidence.Count,
                lifecycleEvidenceCount: 6,
                updateRequestCount: 0,
                updateEvidenceCount: 0);
        }

        private static void RequireLoadingEntry(
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence,
            int index,
            QaLoadingPresentationEvidenceKind kind,
            LoadingSurfaceRequest request,
            bool actualVisible,
            LoadingSurfaceResultStatus status,
            string phase)
        {
            QaLoadingPresentationEvidenceEntry entry = evidence[index];
            Require(entry.Kind == kind && entry.Action == request.Action &&
                entry.RequestedVisible == request.ShouldBeVisible &&
                entry.ActualVisible == actualVisible && entry.Status == status &&
                string.Equals(entry.Source, request.Source, StringComparison.Ordinal) &&
                string.Equals(entry.Detail, request.Detail, StringComparison.Ordinal) &&
                (index == 0 || evidence[index - 1].Sequence < entry.Sequence),
                $"Loading presentation evidence entry '{index}' diverged during '{phase}'. " +
                $"expectedKind='{kind}' actualKind='{entry.Kind}' " +
                $"expectedAction='{request.Action}' actualAction='{entry.Action}' " +
                $"expectedRequestedVisible='{request.ShouldBeVisible}' " +
                $"actualRequestedVisible='{entry.RequestedVisible}' " +
                $"expectedActualVisible='{actualVisible}' actualVisible='{entry.ActualVisible}' " +
                $"expectedStatus='{status}' actualStatus='{entry.Status}' " +
                $"expectedSource='{Escape(request.Source)}' actualSource='{Escape(entry.Source)}' " +
                $"expectedDetail='{Escape(request.Detail)}' actualDetail='{Escape(entry.Detail)}'.");
        }

        private readonly struct LoadingProtocolSummary
        {
            public LoadingProtocolSummary(
                int totalEvidenceCount,
                int lifecycleEvidenceCount,
                int updateRequestCount,
                int updateEvidenceCount)
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

        private static void RequireAuthority(
            FrameworkRuntimeHost host,
            RouteAsset route,
            ActivityAsset activity)
        {
            Require(host.State.GameFlowStarted && host.State.CurrentRoute != null &&
                host.State.CurrentRoute.HasSameIdentity(route),
                "Game Flow Route authority changed during presentation evidence regression.");
            Require((activity == null && host.State.CurrentActivity == null) ||
                (activity != null && host.State.CurrentActivity != null &&
                    host.State.CurrentActivity.HasSameIdentity(activity)),
                "Game Flow Activity authority changed during presentation evidence regression.");
        }

        private static GameObject CreateSyntheticTransitionSurface(Transform parent)
        {
            var surface = new GameObject("QA Transition Curtain Synthetic Surface", typeof(RectTransform));
            surface.SetActive(false);
            surface.transform.SetParent(parent, false);
            surface.AddComponent<CanvasRenderer>();
            CanvasGroup canvasGroup = surface.AddComponent<CanvasGroup>();
            surface.AddComponent<Image>();
            UnityFadeCurtainEffectAdapter adapter =
                surface.AddComponent<UnityFadeCurtainEffectAdapter>();
            var serialized = new SerializedObject(adapter);
            RequireProperty(serialized, "canvasGroup", property => property.objectReferenceValue = canvasGroup);
            RequireProperty(serialized, "surfaceRoot", property => property.objectReferenceValue = surface);
            RequireProperty(serialized, "effectKind", property =>
            {
                int fadeIndex = Array.IndexOf(property.enumNames, nameof(TransitionEffectKind.Fade));
                Require(fadeIndex >= 0,
                    "Runtime synthetic Transition fixture could not resolve the Fade enum value.");
                property.enumValueIndex = fadeIndex;
            });
            RequireProperty(serialized, "hiddenAlpha", property => property.floatValue = 0f);
            RequireProperty(serialized, "visibleAlpha", property => property.floatValue = 1f);
            RequireProperty(serialized, "setSurfaceRootActive", property => property.boolValue = false);
            RequireProperty(serialized, "blockRaycastsWhenVisible", property => property.boolValue = true);
            RequireProperty(serialized, "interactableWhenVisible", property => property.boolValue = false);
            RequireProperty(serialized, "applyHiddenStateOnAwake", property => property.boolValue = true);
            RequireProperty(serialized, "animateAsyncExecution", property => property.boolValue = true);
            RequireProperty(serialized, "fadeInSeconds", property => property.floatValue = 0.25f);
            RequireProperty(serialized, "fadeOutSeconds", property => property.floatValue = 0.25f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            surface.SetActive(true);
            return surface;
        }

        private static GameObject CreateSyntheticLoadingSurface(Transform parent)
        {
            var surface = new GameObject("QA Loading Synthetic Surface", typeof(RectTransform));
            surface.SetActive(false);
            surface.transform.SetParent(parent, false);
            surface.AddComponent<CanvasRenderer>();
            CanvasGroup canvasGroup = surface.AddComponent<CanvasGroup>();
            Image image = surface.AddComponent<Image>();
            QaLoadingSurfaceVisibilityHoldAdapter adapter =
                surface.AddComponent<QaLoadingSurfaceVisibilityHoldAdapter>();
            var serialized = new SerializedObject(adapter);
            RequireProperty(serialized, "canvasGroup", property => property.objectReferenceValue = canvasGroup);
            RequireProperty(serialized, "surfaceRoot", property => property.objectReferenceValue = surface);
            RequireProperty(serialized, "surfaceImage", property => property.objectReferenceValue = image);
            RequireProperty(serialized, "hiddenAlpha", property => property.floatValue = 0f);
            RequireProperty(serialized, "visibleAlpha", property => property.floatValue = 1f);
            RequireProperty(serialized, "setSurfaceRootActive", property => property.boolValue = false);
            RequireProperty(serialized, "blockRaycastsWhenVisible", property => property.boolValue = true);
            RequireProperty(serialized, "interactableWhenVisible", property => property.boolValue = false);
            RequireProperty(serialized, "applyHiddenStateOnAwake", property => property.boolValue = true);
            RequireProperty(serialized, "holdHideForManualQa", property => property.boolValue = false);
            RequireProperty(serialized, "hideHoldSeconds", property => property.floatValue = 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            surface.SetActive(true);
            return surface;
        }

        private static void RequireProperty(
            SerializedObject target,
            string propertyName,
            Action<SerializedProperty> apply)
        {
            SerializedProperty property = target.FindProperty(propertyName);
            Require(property != null,
                $"Runtime synthetic fixture requires serialized property '{propertyName}' on '{target.targetObject.GetType().Name}'.");
            apply(property);
        }

        private static void RequireNoFixtureRoot(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Require(root == null || root.name != FixtureRootName,
                    $"Runtime presentation fixture root '{FixtureRootName}' already exists in '{scene.path}'.");
            }
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

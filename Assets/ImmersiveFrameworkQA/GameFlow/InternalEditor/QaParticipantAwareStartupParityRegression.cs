using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
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

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Q2B positive end-to-end parity proof for Route Startup Activity and
    /// Game Application Startup Activity participant-aware Loading progress.
    /// </summary>
    public static class QaParticipantAwareStartupParityRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/" +
            "Run Participant-Aware Startup Loading Parity Regression";
        private const string RoutePrefix = "[QA_READY_PROGRESS_02B_ROUTE]";
        private const string GameApplicationPrefix =
            "[QA_READY_PROGRESS_02B_GAME_APPLICATION]";
        private const string RouteReason = "route-startup-progress-parity";
        private const string RestoreReason = "restore-canonical-after-q2b";
        private const string ObserverRootName =
            "QA_READY_PROGRESS_02B_TransitionObserver";
        private const float ProgressTolerance = 0.0005f;
        private const int RouteCaseCount = 25;
        private const int GameApplicationCaseCount = 20;

        private static readonly string[] RouteCases =
        {
            "play-mode-required",
            "route-mode-prepared",
            "official-host-resolved",
            "canonical-authority-confirmed",
            "fixture-assets-resolved",
            "host-loading-resolved",
            "host-transition-resolved",
            "evidence-reset",
            "route-request-started",
            "route-request-succeeded",
            "fixture-route-authoritative",
            "fixture-driver-resolved",
            "fixture-driver-completed",
            "readiness-four-of-four-ready",
            "optional-failure-nonblocking",
            "route-diagnostics-participant-aware",
            "route-diagnostics-terminal",
            "determinate-progress-monotonic",
            "terminal-progress-before-hide",
            "hide-before-reveal",
            "presentation-finished-hidden",
            "transition-gate-released",
            "canonical-route-restored",
            "fixture-scene-released",
            "observer-cleaned"
        };

        private static readonly string[] GameApplicationCases =
        {
            "play-mode-required",
            "game-application-mode-prepared",
            "official-host-resolved",
            "fixture-assets-resolved",
            "fixture-game-application-booted",
            "fixture-route-authoritative",
            "fixture-driver-resolved",
            "fixture-driver-completed",
            "readiness-four-of-four-ready",
            "optional-failure-nonblocking",
            "host-loading-resolved",
            "host-transition-resolved",
            "startup-diagnostics-participant-aware",
            "startup-diagnostics-terminal",
            "determinate-progress-monotonic",
            "terminal-progress-before-hide",
            "presentation-finished-hidden",
            "transition-gate-released",
            "canonical-route-restored",
            "fixture-scene-released"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            QaParticipantAwareStartupParityMode mode =
                QaParticipantAwareStartupParitySetup
                    .RequirePreparedForCurrentPlayMode();
            if (mode == QaParticipantAwareStartupParityMode.RouteStartup)
            {
                await RunRouteStartupAsync();
                return;
            }

            if (mode == QaParticipantAwareStartupParityMode
                .GameApplicationStartup)
            {
                await RunGameApplicationStartupAsync();
                return;
            }

            throw new InvalidOperationException(
                $"Unsupported Q2B startup parity mode '{mode}'.");
        }

        private static async Task RunRouteStartupAsync()
        {
            var cases = new QaCaseRegistry(RouteCases, RouteCaseCount);
            var failures = new QaFailureCollector();
            FrameworkRuntimeHost host = null;
            QaLoadingSurfaceVisibilityHoldAdapter loading = null;
            UnityFadeCurtainEffectAdapter transition = null;
            GameObject observerRoot = null;
            QaStartupParityProbe probe = null;
            RouteAsset canonicalRoute = null;

            try
            {
                Require(EditorApplication.isPlaying,
                    "Q2B Route startup parity requires Play Mode.");
                cases.Complete("play-mode-required");
                Require(QaParticipantAwareStartupParitySetup
                        .RequirePreparedForCurrentPlayMode() ==
                    QaParticipantAwareStartupParityMode.RouteStartup,
                    "Q2B Route startup parity is not the prepared mode.");
                cases.Complete("route-mode-prepared");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(host.State.GameFlowStarted &&
                    host.State.CurrentRoute != null,
                    "Q2B Route startup parity requires a started official host.");
                cases.Complete("official-host-resolved");

                GameApplicationAsset canonicalApplication =
                    QaActivityEntryPresentationEvidenceSetup
                        .ResolveCanonicalQaHubApplication();
                canonicalRoute = canonicalApplication.StartupRoute;
                Require(canonicalRoute != null &&
                    host.State.CurrentRoute.HasSameIdentity(canonicalRoute),
                    "Q2B Route mode must start from the canonical QA Hub Route.");
                cases.Complete("canonical-authority-confirmed");

                QaParticipantAwareStartupParityAssets assets =
                    QaParticipantAwareStartupParitySetup.LoadAssets(
                        QaParticipantAwareStartupParityMode.RouteStartup);
                cases.Complete("fixture-assets-resolved");

                loading = ResolveSinglePersistentRuntimeComponent<
                    QaLoadingSurfaceVisibilityHoldAdapter>(host, "Loading");
                RequireLoadingReady(loading);
                RequireLoadingHidden(loading);
                cases.Complete("host-loading-resolved");

                transition = ResolveSinglePersistentRuntimeComponent<
                    UnityFadeCurtainEffectAdapter>(host, "Transition");
                RequireTransitionHidden(transition);
                cases.Complete("host-transition-resolved");

                Scene persistentScene = host.gameObject.scene;
                RequireNoObserverRoot(persistentScene);
                observerRoot = new GameObject(ObserverRootName);
                SceneManager.MoveGameObjectToScene(observerRoot,
                    persistentScene);
                var observer = observerRoot.AddComponent<
                    QaTransitionPresentationEvidenceObserver>();
                observer.Bind(transition);
                loading.ResetPresentationEvidence();
                observer.ResetEvidence();
                probe = new QaStartupParityProbe(loading, observer);
                probe.Attach();
                cases.Complete("evidence-reset");

                Task<FrameworkRouteRequestResult> request =
                    host.RequestRouteAsync(
                        assets.Route,
                        nameof(QaParticipantAwareStartupParityRegression),
                        RouteReason);
                cases.Complete("route-request-started");
                FrameworkRouteRequestResult result = await request;
                Require(result.Succeeded &&
                    result.DestinationAuthoritative &&
                    result.TargetRoute != null &&
                    result.TargetRoute.HasSameIdentity(assets.Route),
                    $"Q2B Route request failed. message='{result.Message}'.");
                cases.Complete("route-request-succeeded");

                RequireFixtureAuthority(host, assets);
                cases.Complete("fixture-route-authoritative");
                QaParticipantAwareStartupParityDriver driver =
                    ResolveFixtureDriver();
                cases.Complete("fixture-driver-resolved");
                RequireDriverCompleted(driver);
                cases.Complete("fixture-driver-completed");

                ActivityReadinessProgressSnapshot snapshot =
                    RequireReadySnapshot(host, assets.Activity);
                cases.Complete("readiness-four-of-four-ready");
                Require(snapshot.OptionalFailedCount == 1 &&
                    driver.OptionalFailureIssued,
                    "Q2B Optional failure was not retained as nonblocking evidence.");
                cases.Complete("optional-failure-nonblocking");

                FrameworkLoadingDiagnostics routeDiagnostics =
                    host.LastRouteActivityEntryLoadingDiagnostics;
                RequireSuccessfulSurfaceDiagnostics(
                    routeDiagnostics,
                    "Route Startup");
                cases.Complete("route-diagnostics-participant-aware");
                RequireSuccessfulActivityEntryDiagnostics(
                    routeDiagnostics.ActivityEntryProgress,
                    assets.Activity,
                    "Route Startup");
                cases.Complete("route-diagnostics-terminal");

                IReadOnlyList<QaLoadingPresentationEvidenceEntry> updates =
                    RequireStartupOperationDeterminateUpdates(
                        loading.PresentationEvidence,
                        nameof(QaParticipantAwareStartupParityRegression),
                        RouteReason,
                        out _,
                        out int routeHideSequence);
                int routeTerminalSequence =
                    RequireMonotonicTerminal(
                        updates,
                        minimumUpdateCount: 5,
                        "Route Startup");
                Require(routeHideSequence > routeTerminalSequence,
                    "Q2B Route Loading Hide preceded terminal 100% evidence.");
                cases.Complete("determinate-progress-monotonic");
                Require(probe.TerminalProgressSequence > 0 &&
                    probe.LoadingHideSequence >
                    probe.TerminalProgressSequence,
                    "Q2B Route Loading reached Hide before terminal 100%.");
                cases.Complete("terminal-progress-before-hide");
                Require(probe.TransitionRevealSequence >
                    probe.LoadingHideSequence,
                    "Q2B Route Transition reveal preceded Loading Hide.");
                cases.Complete("hide-before-reveal");

                RequireLoadingHidden(loading);
                RequireTransitionHidden(transition);
                cases.Complete("presentation-finished-hidden");
                Require(
                    !host.TransitionGateSnapshot.HasBlockers &&
                    host.CurrentTransitionGateMode == TransitionGateMode.None &&
                    !host.ActivityEntryReadinessGateSnapshot.HasBlockers,
                    "Q2B Route startup transition/readiness gates remained active. " +
                    $"transitionMode='{host.CurrentTransitionGateMode}' " +
                    $"transitionBlockers='{host.TransitionGateSnapshot.BlockerCount}' " +
                    $"readinessBlockers='{host.ActivityEntryReadinessGateSnapshot.BlockerCount}'.");
                cases.Complete("transition-gate-released");

                probe.Dispose();
                probe = null;
                await RestoreCanonicalRouteAsync(host, canonicalRoute);
                cases.Complete("canonical-route-restored");
                RequireFixtureSceneReleased();
                cases.Complete("fixture-scene-released");

                UnityEngine.Object.Destroy(observerRoot);
                observerRoot = null;
                await Awaitable.NextFrameAsync();
                RequireNoObserverRoot(persistentScene);
                cases.Complete("observer-cleaned");
            }
            catch (Exception exception)
            {
                failures.Add("Execution", exception);
            }
            finally
            {
                probe?.Dispose();
                if (host != null && canonicalRoute != null)
                {
                    try
                    {
                        await RestoreCanonicalRouteAsync(host, canonicalRoute);
                    }
                    catch (Exception exception)
                    {
                        failures.Add("AuthorityRestore", exception);
                    }
                }

                if (observerRoot != null)
                {
                    UnityEngine.Object.Destroy(observerRoot);
                    await Awaitable.NextFrameAsync();
                }

                try
                {
                    loading?.ResetPresentationEvidence();
                    if (loading != null)
                    {
                        RequireLoadingHidden(loading);
                    }
                    if (transition != null)
                    {
                        RequireTransitionHidden(transition);
                    }
                }
                catch (Exception exception)
                {
                    failures.Add("PresentationCleanup", exception);
                }
            }

            Finish(cases, failures, RoutePrefix,
                "RouteStartupActivity");
        }

        private static async Task RunGameApplicationStartupAsync()
        {
            var cases = new QaCaseRegistry(
                GameApplicationCases,
                GameApplicationCaseCount);
            var failures = new QaFailureCollector();
            FrameworkRuntimeHost host = null;
            QaLoadingSurfaceVisibilityHoldAdapter loading = null;
            UnityFadeCurtainEffectAdapter transition = null;
            RouteAsset canonicalRoute = null;

            try
            {
                Require(EditorApplication.isPlaying,
                    "Q2B Game Application startup parity requires Play Mode.");
                cases.Complete("play-mode-required");
                Require(QaParticipantAwareStartupParitySetup
                        .RequirePreparedForCurrentPlayMode() ==
                    QaParticipantAwareStartupParityMode
                        .GameApplicationStartup,
                    "Q2B Game Application startup parity is not the prepared mode.");
                cases.Complete("game-application-mode-prepared");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(host.State.GameFlowStarted &&
                    host.State.CurrentRoute != null,
                    "Q2B Game Application startup did not produce a started host.");
                cases.Complete("official-host-resolved");

                QaParticipantAwareStartupParityAssets assets =
                    QaParticipantAwareStartupParitySetup.LoadAssets(
                        QaParticipantAwareStartupParityMode
                            .GameApplicationStartup);
                cases.Complete("fixture-assets-resolved");
                Require(assets.GameApplication != null &&
                    host.State.GameApplication != null &&
                    ReferenceEquals(host.State.GameApplication,
                        assets.GameApplication),
                    "Q2B fixture Game Application was not the boot authority.");
                cases.Complete("fixture-game-application-booted");
                RequireFixtureAuthority(host, assets);
                cases.Complete("fixture-route-authoritative");

                QaParticipantAwareStartupParityDriver driver =
                    ResolveFixtureDriver();
                cases.Complete("fixture-driver-resolved");
                RequireDriverCompleted(driver);
                cases.Complete("fixture-driver-completed");
                ActivityReadinessProgressSnapshot snapshot =
                    RequireReadySnapshot(host, assets.Activity);
                cases.Complete("readiness-four-of-four-ready");
                Require(snapshot.OptionalFailedCount == 1 &&
                    driver.OptionalFailureIssued,
                    "Q2B Game Application Optional failure was not nonblocking.");
                cases.Complete("optional-failure-nonblocking");

                loading = ResolveSinglePersistentRuntimeComponent<
                    QaLoadingSurfaceVisibilityHoldAdapter>(host, "Loading");
                RequireLoadingReady(loading);
                cases.Complete("host-loading-resolved");
                transition = ResolveSinglePersistentRuntimeComponent<
                    UnityFadeCurtainEffectAdapter>(host, "Transition");
                RequireTransitionHidden(transition);
                cases.Complete("host-transition-resolved");

                FrameworkLoadingDiagnostics diagnostics =
                    host.LastStartupActivityEntryLoadingDiagnostics;
                RequireSuccessfulSurfaceDiagnostics(
                    diagnostics,
                    "Game Application Startup");
                cases.Complete("startup-diagnostics-participant-aware");
                RequireSuccessfulActivityEntryDiagnostics(
                    diagnostics.ActivityEntryProgress,
                    assets.Activity,
                    "Game Application Startup");
                cases.Complete("startup-diagnostics-terminal");

                IReadOnlyList<QaLoadingPresentationEvidenceEntry> updates =
                    RequireStartupOperationDeterminateUpdates(
                        loading.PresentationEvidence,
                        "GameApplication",
                        "startup",
                        out _,
                        out int hideSequence);
                int terminalSequence = RequireMonotonicTerminal(
                    updates,
                    minimumUpdateCount: 2,
                    "Game Application Startup");
                cases.Complete("determinate-progress-monotonic");
                Require(hideSequence > terminalSequence,
                    "Q2B Game Application Loading Hide preceded terminal 100%.");
                cases.Complete("terminal-progress-before-hide");

                RequireLoadingHidden(loading);
                RequireTransitionHidden(transition);
                cases.Complete("presentation-finished-hidden");
                Require(
                    !host.TransitionGateSnapshot.HasBlockers &&
                    host.CurrentTransitionGateMode == TransitionGateMode.None &&
                    !host.ActivityEntryReadinessGateSnapshot.HasBlockers,
                    "Q2B Game Application startup gate remained active. " +
                    $"transitionMode='{host.CurrentTransitionGateMode}' " +
                    $"transitionBlockers='{host.TransitionGateSnapshot.BlockerCount}' " +
                    $"readinessBlockers='{host.ActivityEntryReadinessGateSnapshot.BlockerCount}'.");
                cases.Complete("transition-gate-released");

                canonicalRoute = QaActivityEntryPresentationEvidenceSetup
                    .ResolveCanonicalQaHubApplication().StartupRoute;
                await RestoreCanonicalRouteAsync(host, canonicalRoute);
                cases.Complete("canonical-route-restored");
                RequireFixtureSceneReleased();
                cases.Complete("fixture-scene-released");
            }
            catch (Exception exception)
            {
                failures.Add("Execution", exception);
            }
            finally
            {
                if (host != null)
                {
                    try
                    {
                        canonicalRoute ??=
                            QaActivityEntryPresentationEvidenceSetup
                                .ResolveCanonicalQaHubApplication()
                                .StartupRoute;
                        await RestoreCanonicalRouteAsync(host,
                            canonicalRoute);
                    }
                    catch (Exception exception)
                    {
                        failures.Add("AuthorityRestore", exception);
                    }
                }
            }

            Finish(cases, failures, GameApplicationPrefix,
                "GameApplicationStartupActivity");
        }

        private static void Finish(
            QaCaseRegistry cases,
            QaFailureCollector failures,
            string prefix,
            string path)
        {
            if (failures.HasFailures)
            {
                Debug.LogError(
                    $"{prefix} status='Failed' " +
                    $"execution='{failures.Describe("Execution")}' " +
                    $"authorityRestore='{failures.Describe("AuthorityRestore")}' " +
                    $"presentationCleanup='{failures.Describe("PresentationCleanup")}' " +
                    $"nextExpectedCase='{cases.NextExpectedOrNone()}' " +
                    $"missingCases='{cases.DescribeMissing()}' " +
                    $"completed='{cases.DescribeCompleted()}'.");
                throw failures.ToAggregate(
                    $"Q2B {path} startup parity regression failed.");
            }

            cases.RequireComplete();
            Debug.Log($"{prefix} status='Passed' cases='{cases.Count}' " +
                $"path='{path}' required='4' optional='1' " +
                "optionalOutcome='FailedNonBlocking' terminal='100BeforeHide' " +
                $"completed='{cases.DescribeCompleted()}'.");
        }

        private static void RequireFixtureAuthority(
            FrameworkRuntimeHost host,
            QaParticipantAwareStartupParityAssets assets)
        {
            Require(host.State.CurrentRoute != null &&
                host.State.CurrentRoute.HasSameIdentity(assets.Route) &&
                host.State.CurrentActivity != null &&
                host.State.CurrentActivity.HasSameIdentity(assets.Activity),
                "Q2B fixture Route or Startup Activity is not authoritative.");
        }

        private static QaParticipantAwareStartupParityDriver
            ResolveFixtureDriver()
        {
            Scene scene = SceneManager.GetSceneByPath(
                QaParticipantAwareStartupParitySetup.FixtureScenePath);
            Require(scene.IsValid() && scene.isLoaded,
                "Q2B fixture scene is not loaded.");
            GameObject[] roots = scene.GetRootGameObjects();
            var matches = new List<
                QaParticipantAwareStartupParityDriver>();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                QaParticipantAwareStartupParityDriver[] drivers =
                    roots[rootIndex] == null
                        ? Array.Empty<
                            QaParticipantAwareStartupParityDriver>()
                        : roots[rootIndex].GetComponentsInChildren<
                            QaParticipantAwareStartupParityDriver>(true);
                for (int index = 0; index < drivers.Length; index++)
                {
                    if (drivers[index] != null)
                    {
                        matches.Add(drivers[index]);
                    }
                }
            }

            Require(matches.Count == 1,
                $"Q2B fixture requires one driver. actual='{matches.Count}'.");
            return matches[0];
        }

        private static void RequireDriverCompleted(
            QaParticipantAwareStartupParityDriver driver)
        {
            Require(driver != null && !driver.HasFailure &&
                driver.ParticipantCount == 5 &&
                driver.RequiredCount == 4 &&
                driver.OptionalCount == 1 &&
                driver.PreparationStartedCount == 5 &&
                driver.RequiredCompletionCount == 4 &&
                driver.OptionalFailureIssued &&
                driver.CompletionSequenceFinished &&
                driver.CompletionFrame > 0,
                "Q2B fixture driver did not complete its deterministic occurrence. " +
                $"failure='{driver?.Failure}' starts='{driver?.PreparationStartedCount}' " +
                $"requiredCompleted='{driver?.RequiredCompletionCount}'.");
        }

        private static ActivityReadinessProgressSnapshot
            RequireReadySnapshot(
                FrameworkRuntimeHost host,
                ActivityAsset activity)
        {
            Require(host != null && host.CurrentGameFlowRuntime != null &&
                host.CurrentGameFlowRuntime.CurrentRouteLifecycleRuntime != null,
                "Q2B current Route Lifecycle runtime is unavailable.");
            ActivityFlowRuntime activityFlow = host.CurrentGameFlowRuntime
                .CurrentRouteLifecycleRuntime.CurrentActivityFlowRuntime;
            ActivityReadinessOccurrenceState state = default;
            Require(activityFlow != null &&
                activityFlow.TryGetCurrentAuthorableReadinessState(
                    out state) &&
                state.IsCurrent &&
                ReferenceEquals(state.Activity, activity),
                "Q2B current authorable readiness occurrence is unavailable.");

            ActivityReadinessProgressSnapshot snapshot =
                state.ProgressSnapshot;
            Require(snapshot.IsValid && snapshot.IsReady &&
                snapshot.RequiredCount == 4 &&
                snapshot.RequiredCompletedCount == 4 &&
                snapshot.RequiredPendingCount == 0 &&
                snapshot.RequiredFailedCount == 0 &&
                snapshot.RequiredReleasedCount == 0 &&
                snapshot.OptionalCount == 1 &&
                snapshot.OptionalFailedCount == 1 &&
                snapshot.OptionalPendingCount == 0 &&
                snapshot.OptionalCompletedCount == 0 &&
                snapshot.OptionalReleasedCount == 0 &&
                Approximately(snapshot.ReadinessRatio, 1f),
                "Q2B readiness snapshot diverged from 4/4 Ready with one " +
                "nonblocking Optional failure.");
            return snapshot;
        }

        private static void RequireSuccessfulSurfaceDiagnostics(
            FrameworkLoadingDiagnostics diagnostics,
            string path)
        {
            Require(diagnostics.HasDiagnostics &&
                string.Equals(
                    diagnostics.LoadingText,
                    "SucceededWithUnitySurface",
                    StringComparison.Ordinal) &&
                string.Equals(
                    diagnostics.BeforeText,
                    "Succeeded",
                    StringComparison.Ordinal) &&
                string.Equals(
                    diagnostics.AfterText,
                    "Succeeded",
                    StringComparison.Ordinal) &&
                diagnostics.ProgressSupported &&
                diagnostics.Progress.IsDeterminate &&
                Approximately(diagnostics.Progress.Value01, 1f) &&
                diagnostics.BlockingIssueCount == 0 &&
                diagnostics.AdapterCount > 0 &&
                diagnostics.HasAdapterEvidence &&
                diagnostics.AppliedAdapterEvidenceCount > 0 &&
                diagnostics.FailedAdapterEvidenceCount == 0 &&
                diagnostics.AdapterEvidenceBlockingIssueCount == 0 &&
                diagnostics.HasActivityEntryProgress,
                $"Q2B {path} host Loading diagnostics did not retain " +
                "successful participant-aware surface evidence.");
        }

        private static void RequireSuccessfulActivityEntryDiagnostics(
            ActivityEntryLoadingProgressDiagnostics diagnostics,
            ActivityAsset activity,
            string path)
        {
            Require(diagnostics.IsValid &&
                diagnostics.HasOccurrence &&
                ReferenceEquals(diagnostics.Occurrence.Activity, activity) &&
                diagnostics.Occurrence.TransitionSequence > 0,
                $"Q2B {path} did not retain the expected readiness occurrence.");
            Require(Approximately(diagnostics.TechnicalRangeStart01, 0f) &&
                diagnostics.TechnicalRangeEnd01 > 0f &&
                diagnostics.TechnicalRangeEnd01 < 1f &&
                Approximately(
                    diagnostics.TechnicalRangeEnd01,
                    diagnostics.ReadinessRangeStart01) &&
                Approximately(diagnostics.ReadinessRangeEnd01, 1f),
                $"Q2B {path} progress envelope ranges are invalid.");
            Require(diagnostics.RequiredCount == 4 &&
                diagnostics.RequiredCompletedCount == 4 &&
                diagnostics.RequiredPendingCount == 0 &&
                diagnostics.RequiredFailedCount == 0 &&
                diagnostics.RequiredReleasedCount == 0 &&
                diagnostics.OptionalCount == 1 &&
                diagnostics.OptionalCompletedCount == 0 &&
                diagnostics.OptionalPendingCount == 0 &&
                diagnostics.OptionalFailedCount == 1 &&
                diagnostics.OptionalReleasedCount == 0 &&
                Approximately(diagnostics.ReadinessRatio, 1f),
                $"Q2B {path} retained participant counts diverged.");
            Require(diagnostics.HasReportedProgress &&
                diagnostics.LastProgress.Supported &&
                diagnostics.LastProgress.IsDeterminate &&
                Approximately(diagnostics.LastProgress.Value01, 1f) &&
                diagnostics.TerminalCompletionIssued &&
                !diagnostics.TerminalFailureObserved &&
                diagnostics.LoadingHidden &&
                diagnostics.RevealCompleted &&
                diagnostics.RejectedReadinessSnapshotCount == 0,
                $"Q2B {path} terminal progress diagnostics diverged.");
        }

        private static IReadOnlyList<QaLoadingPresentationEvidenceEntry>
            RequireStartupOperationDeterminateUpdates(
                IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence,
                string source,
                string operationLabel,
                out int showSequence,
                out int hideSequence)
        {
            Require(evidence != null,
                $"Q2B {operationLabel} Loading evidence is unavailable.");
            Require(!string.IsNullOrWhiteSpace(source),
                $"Q2B {operationLabel} Loading source is required.");

            var scoped = new List<QaLoadingPresentationEvidenceEntry>();
            for (int index = 0; index < evidence.Count; index++)
            {
                QaLoadingPresentationEvidenceEntry entry = evidence[index];
                if (string.Equals(entry.Source, source, StringComparison.Ordinal))
                {
                    scoped.Add(entry);
                }
            }

            Require(scoped.Count >= 6 && (scoped.Count - 6) % 3 == 0,
                $"Q2B {operationLabel} Loading evidence has invalid scoped " +
                $"protocol length '{scoped.Count}'. total='{evidence.Count}'.");
            string detail = scoped[0].Detail;
            Require(!string.IsNullOrWhiteSpace(detail),
                $"Q2B {operationLabel} Loading presentation detail is missing.");

            RequireStartupPresentationTriplet(
                scoped,
                0,
                QaLoadingPresentationEvidenceKind.VisibleApplied,
                LoadingSurfaceAction.Show,
                true,
                true,
                null,
                source,
                detail,
                operationLabel);
            showSequence = scoped[0].Sequence;

            int updateCount = (scoped.Count - 6) / 3;
            var updates = new List<QaLoadingPresentationEvidenceEntry>(
                updateCount);
            float previousProgress = -1f;
            for (int updateIndex = 0;
                updateIndex < updateCount;
                updateIndex++)
            {
                int offset = 3 + (updateIndex * 3);
                RequireStartupPresentationTriplet(
                    scoped,
                    offset,
                    QaLoadingPresentationEvidenceKind.VisibleApplied,
                    LoadingSurfaceAction.Update,
                    true,
                    true,
                    true,
                    source,
                    detail,
                    operationLabel);
                QaLoadingPresentationEvidenceEntry request = scoped[offset];
                Require(request.ProgressSupported &&
                    !float.IsNaN(request.ProgressValue01) &&
                    !float.IsInfinity(request.ProgressValue01) &&
                    request.ProgressValue01 >= 0f &&
                    request.ProgressValue01 <= 1f &&
                    request.ProgressValue01 + ProgressTolerance >=
                    previousProgress,
                    $"Q2B {operationLabel} determinate Loading progress " +
                    $"diverged at update='{updateIndex}' " +
                    $"entry='{DescribeLoadingEvidence(request)}'.");
                previousProgress = request.ProgressValue01;
                updates.Add(request);
            }

            Require(updates.Count > 0,
                $"Q2B {operationLabel} emitted no determinate Update request.");
            int hideOffset = 3 + (updateCount * 3);
            RequireStartupPresentationTriplet(
                scoped,
                hideOffset,
                QaLoadingPresentationEvidenceKind.HiddenApplied,
                LoadingSurfaceAction.Hide,
                false,
                false,
                true,
                source,
                detail,
                operationLabel);
            hideSequence = scoped[hideOffset + 1].Sequence;
            return updates;
        }

        private static void RequireStartupPresentationTriplet(
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence,
            int offset,
            QaLoadingPresentationEvidenceKind applyKind,
            LoadingSurfaceAction action,
            bool requestedVisible,
            bool appliedVisible,
            bool? requestActualVisible,
            string source,
            string detail,
            string operationLabel)
        {
            RequireStartupPresentationEntry(
                evidence,
                offset,
                QaLoadingPresentationEvidenceKind.RequestReceived,
                action,
                requestedVisible,
                requestActualVisible,
                LoadingSurfaceResultStatus.Unknown,
                source,
                detail,
                operationLabel);
            RequireStartupPresentationEntry(
                evidence,
                offset + 1,
                applyKind,
                action,
                requestedVisible,
                appliedVisible,
                LoadingSurfaceResultStatus.Unknown,
                source,
                detail,
                operationLabel);
            RequireStartupPresentationEntry(
                evidence,
                offset + 2,
                QaLoadingPresentationEvidenceKind.ResultRecorded,
                action,
                requestedVisible,
                appliedVisible,
                LoadingSurfaceResultStatus.Succeeded,
                source,
                detail,
                operationLabel);
        }

        private static void RequireStartupPresentationEntry(
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence,
            int index,
            QaLoadingPresentationEvidenceKind kind,
            LoadingSurfaceAction action,
            bool requestedVisible,
            bool? actualVisible,
            LoadingSurfaceResultStatus status,
            string source,
            string detail,
            string operationLabel)
        {
            QaLoadingPresentationEvidenceEntry entry = evidence[index];
            bool sequenceValid = index == 0 ||
                evidence[index - 1].Sequence < entry.Sequence;
            bool actualVisibleValid = !actualVisible.HasValue ||
                entry.ActualVisible == actualVisible.Value;
            Require(entry.Kind == kind &&
                entry.Action == action &&
                entry.RequestedVisible == requestedVisible &&
                actualVisibleValid &&
                entry.Status == status &&
                string.Equals(entry.Source, source,
                    StringComparison.Ordinal) &&
                string.Equals(entry.Detail, detail,
                    StringComparison.Ordinal) &&
                sequenceValid,
                $"Q2B {operationLabel} Loading evidence entry '{index}' " +
                $"diverged. expectedKind='{kind}' expectedAction='{action}' " +
                $"entry='{DescribeLoadingEvidence(entry)}'.");
        }

        private static string DescribeLoadingEvidence(
            QaLoadingPresentationEvidenceEntry entry)
        {
            return $"sequence={entry.Sequence};kind={entry.Kind};" +
                $"action={entry.Action};requested={entry.RequestedVisible};" +
                $"actual={entry.ActualVisible};status={entry.Status};" +
                $"source={entry.Source};detail={entry.Detail};" +
                $"progressSupported={entry.ProgressSupported};" +
                $"progress={entry.ProgressValue01:0.###}";
        }

        private static int RequireMonotonicTerminal(
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> updates,
            int minimumUpdateCount,
            string operationLabel)
        {
            Require(minimumUpdateCount >= 2,
                "Q2B minimum determinate update count must include " +
                "one nonterminal update and one terminal update.");
            Require(updates != null &&
                updates.Count >= minimumUpdateCount,
                $"Q2B {operationLabel} determinate Loading evidence is " +
                $"incomplete. expectedAtLeast='{minimumUpdateCount}' " +
                $"actual='{(updates != null ? updates.Count : 0)}'.");
            float previous = -1f;
            int terminalSequence = 0;
            for (int index = 0; index < updates.Count; index++)
            {
                QaLoadingPresentationEvidenceEntry entry = updates[index];
                Require(entry.ProgressSupported &&
                    entry.ProgressValue01 >= 0f &&
                    entry.ProgressValue01 <= 1f &&
                    entry.ProgressValue01 + ProgressTolerance >= previous,
                    $"Q2B Loading progress is not finite and monotonic at '{index}'.");
                if (Approximately(entry.ProgressValue01, 1f))
                {
                    Require(terminalSequence == 0,
                        "Q2B Loading emitted terminal 100% more than once.");
                    terminalSequence = entry.Sequence;
                }
                else
                {
                    Require(terminalSequence == 0,
                        "Q2B Loading emitted progress after terminal 100%.");
                }

                previous = entry.ProgressValue01;
            }

            Require(terminalSequence > 0,
                "Q2B Loading never emitted terminal 100%.");
            return terminalSequence;
        }

        private static int FindLoadingHideSequence(
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence,
            string source,
            string reason)
        {
            int sequence = 0;
            for (int index = 0; index < evidence.Count; index++)
            {
                QaLoadingPresentationEvidenceEntry entry = evidence[index];
                if (entry.Kind ==
                        QaLoadingPresentationEvidenceKind.HiddenApplied &&
                    entry.Action == LoadingSurfaceAction.Hide &&
                    string.Equals(entry.Source, source,
                        StringComparison.Ordinal) &&
                    entry.Detail.IndexOf(reason,
                        StringComparison.Ordinal) >= 0)
                {
                    sequence = entry.Sequence;
                }
            }

            Require(sequence > 0,
                "Q2B Loading Hide evidence was not recorded.");
            return sequence;
        }

        private static async Task RestoreCanonicalRouteAsync(
            FrameworkRuntimeHost host,
            RouteAsset canonicalRoute)
        {
            if (host == null || canonicalRoute == null)
            {
                return;
            }

            if (host.State.CurrentRoute != null &&
                host.State.CurrentRoute.HasSameIdentity(canonicalRoute))
            {
                return;
            }

            FrameworkRouteRequestResult restore =
                await host.RequestRouteAsync(
                    canonicalRoute,
                    nameof(QaParticipantAwareStartupParityRegression),
                    RestoreReason);
            Require(restore.Succeeded ||
                restore.Kind == FrameworkRouteRequestKind
                    .IgnoredAlreadyActive,
                $"Q2B canonical Route restore failed. message='{restore.Message}'.");
            Require(host.State.CurrentRoute != null &&
                host.State.CurrentRoute.HasSameIdentity(canonicalRoute),
                "Q2B canonical Route was not restored.");
        }

        private static void RequireFixtureSceneReleased()
        {
            Scene scene = SceneManager.GetSceneByPath(
                QaParticipantAwareStartupParitySetup.FixtureScenePath);
            Require(!scene.IsValid() || !scene.isLoaded,
                "Q2B fixture scene remained loaded after canonical restore.");
        }

        private static T ResolveSinglePersistentRuntimeComponent<T>(
            FrameworkRuntimeHost host,
            string label)
            where T : Component
        {
            Require(host != null,
                "Q2B persistent component resolution requires the host.");
            Scene runtimeScene = host.gameObject.scene;
            Require(runtimeScene.IsValid() && runtimeScene.isLoaded,
                "Q2B official host persistent scene is unavailable.");
            GameObject[] roots = runtimeScene.GetRootGameObjects();
            var matches = new List<T>();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                T[] components = roots[rootIndex] == null
                    ? Array.Empty<T>()
                    : roots[rootIndex].GetComponentsInChildren<T>(true);
                for (int index = 0; index < components.Length; index++)
                {
                    if (components[index] != null)
                    {
                        matches.Add(components[index]);
                    }
                }
            }

            Require(matches.Count == 1,
                $"Q2B host scene requires one {label}. actual='{matches.Count}'.");
            return matches[0];
        }

        private static void RequireLoadingReady(
            QaLoadingSurfaceVisibilityHoldAdapter loading)
        {
            Require(loading != null && loading.HasCanvasGroup &&
                loading.HasSurfaceImage && loading.HasProgressPresentation,
                "Q2B host Loading surface is not progress-capable.");
        }

        private static void RequireLoadingHidden(
            QaLoadingSurfaceVisibilityHoldAdapter loading)
        {
            Require(loading != null && !loading.IsVisible &&
                loading.CurrentAlpha <= 0.001f &&
                !loading.HideHoldActive,
                "Q2B Loading surface did not finish hidden.");
        }

        private static void RequireTransitionHidden(
            UnityFadeCurtainEffectAdapter transition)
        {
            Require(transition != null &&
                transition.ConfiguredEffectKind == TransitionEffectKind.Fade &&
                transition.HasCanvasGroup &&
                !transition.IsVisible &&
                transition.CurrentAlpha <= 0.001f,
                "Q2B Transition surface did not finish as a hidden Fade.");
        }

        private static void RequireNoObserverRoot(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded,
                "Q2B observer scene is unavailable.");
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                Require(roots[index] == null ||
                    !string.Equals(roots[index].name,
                        ObserverRootName,
                        StringComparison.Ordinal),
                    $"Q2B observer root '{ObserverRootName}' already exists.");
            }
        }

        private static bool Approximately(float left, float right)
        {
            return Math.Abs(left - right) <= ProgressTolerance;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class QaStartupParityProbe : IDisposable
        {
            private readonly object _sync = new object();
            private readonly QaLoadingSurfaceVisibilityHoldAdapter _loading;
            private readonly QaTransitionPresentationEvidenceObserver _transition;
            private int _sequence;
            private bool _transitionVisibleObserved;
            private bool _attached;

            internal QaStartupParityProbe(
                QaLoadingSurfaceVisibilityHoldAdapter loading,
                QaTransitionPresentationEvidenceObserver transition)
            {
                this._loading = loading ??
                    throw new ArgumentNullException(nameof(loading));
                this._transition = transition ??
                    throw new ArgumentNullException(nameof(transition));
            }

            internal int TerminalProgressSequence { get; private set; }
            internal int LoadingHideSequence { get; private set; }
            internal int TransitionRevealSequence { get; private set; }

            internal void Attach()
            {
                if (_attached)
                {
                    return;
                }

                _loading.PresentationEvidenceRecorded += HandleLoading;
                _transition.PresentationEvidenceRecorded += HandleTransition;
                _attached = true;
            }

            public void Dispose()
            {
                if (!_attached)
                {
                    return;
                }

                _loading.PresentationEvidenceRecorded -= HandleLoading;
                _transition.PresentationEvidenceRecorded -= HandleTransition;
                _attached = false;
            }

            private void HandleLoading(
                QaLoadingPresentationEvidenceEntry entry)
            {
                lock (_sync)
                {
                    int current = ++_sequence;
                    if (entry.Kind ==
                            QaLoadingPresentationEvidenceKind.RequestReceived &&
                        entry.Action == LoadingSurfaceAction.Update &&
                        entry.ProgressSupported &&
                        Approximately(entry.ProgressValue01, 1f))
                    {
                        TerminalProgressSequence = current;
                    }

                    if (entry.Kind ==
                            QaLoadingPresentationEvidenceKind.HiddenApplied &&
                        entry.Action == LoadingSurfaceAction.Hide)
                    {
                        LoadingHideSequence = current;
                    }
                }
            }

            private void HandleTransition(
                QaTransitionPresentationEvidenceEntry entry)
            {
                lock (_sync)
                {
                    int current = ++_sequence;
                    if (entry.Kind !=
                        QaTransitionPresentationEvidenceKind.StateChanged)
                    {
                        return;
                    }

                    if (entry.VisualState ==
                        QaTransitionVisualState.Visible)
                    {
                        _transitionVisibleObserved = true;
                        return;
                    }

                    if (_transitionVisibleObserved &&
                        entry.VisualState ==
                        QaTransitionVisualState.Hidden)
                    {
                        TransitionRevealSequence = current;
                    }
                }
            }
        }
    }
}

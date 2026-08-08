using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Canonical public runner for Identity Authority (IF-ID) smokes.
    /// Sole public IF-ID surface after Corte 5 consolidation.
    /// Six cases: baseline, route/activity collision, ownership release, readiness isolation, supersession.
    /// </summary>
    public static class QaRouteActivityIdentityRegression
    {
        private const string MenuPath =
            "Immersive Framework QA/Game Flow/Run Identity Authority Regression";
        private const string LogPrefix = "[IF_ID_QA]";
        private const string Source = nameof(QaRouteActivityIdentityRegression);
        private const int ExpectedCaseCount = 6;

        private const string SharedRouteStableId = "qa.if-id.route.collision";
        private const string SharedActivityStableId = "qa.if-id.activity.collision";
        private const string SharedOwnershipStableId = "qa.if-id.ownership.release";
        private const string SharedReadinessStableId = "qa.if-id.readiness.collision";
        private const string SharedSupersessionStableId = "qa.if-id.supersession.activity";

        private static readonly string[] ExpectedCases =
        {
            "baseline-authority-snapshot",
            "route-collision-transition",
            "activity-collision-transition",
            "ownership-release-isolation",
            "readiness-collision-isolation",
            "legitimate-supersession-preservation"
        };

        private static bool _sRunning;
        private static readonly List<string> CaseDiagnostics = new List<string>();

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            EditorApplication.isPlaying && !_sRunning;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            if (_sRunning)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' reason='concurrent-execution-rejected' " +
                    "message='Identity Authority Regression is already running.'.");
                return;
            }

            _sRunning = true;
            CaseDiagnostics.Clear();
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            var failures = new QaFailureCollector();
            QaIdentityAuthorityFixture fixture = null;
            var stopwatch = Stopwatch.StartNew();
            QaIdentityAuthorityFixture.AuthoritySnapshot initial = null;
            QaIdentityAuthorityFixture.AuthoritySnapshot final = null;

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "Identity Authority Regression requires Play Mode.");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string hostDiagnostic),
                    $"Unique FrameworkRuntimeHost is required. {hostDiagnostic}");

                Require(
                    host.State.GameFlowStarted,
                    $"Game Flow is not started. {hostDiagnostic}");

                fixture = QaIdentityAuthorityFixture.Capture(host, Source + ".initial");
                initial = fixture.Initial;

                await RunBaselineAuthoritySnapshotAsync(fixture, cases);
                await RunRouteCollisionTransitionAsync(fixture, cases);
                await RunActivityCollisionTransitionAsync(fixture, cases);
                await RunOwnershipReleaseIsolationAsync(fixture, cases);
                await RunReadinessCollisionIsolationAsync(fixture, cases);
                await RunLegitimateSupersessionPreservationAsync(fixture, cases);

                cases.RequireComplete();
            }
            catch (Exception exception)
            {
                failures.Add("execution", exception);
            }
            finally
            {
                if (fixture != null)
                {
                    try
                    {
                        await fixture.TeardownAsync(Source + ".teardown");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("teardown", exception);
                    }

                    if (fixture.Failures.HasFailures)
                    {
                        failures.Add(
                            "cleanup",
                            fixture.Failures.ToAggregate(
                                "Identity Authority fixture cleanup failures."));
                    }

                    try
                    {
                        final = fixture.CaptureCurrent(Source + ".report");
                    }
                    catch (Exception exception)
                    {
                        failures.Add("final-snapshot", exception);
                        final = fixture.Initial;
                    }
                }

                stopwatch.Stop();
                _sRunning = false;
                EmitFinalReport(
                    failures,
                    cases,
                    initial,
                    final,
                    fixture,
                    stopwatch.Elapsed);
            }

            if (failures.HasFailures)
            {
                throw failures.ToAggregate(
                    "Identity Authority Regression failed.");
            }
        }

        private static Task RunBaselineAuthoritySnapshotAsync(
            QaIdentityAuthorityFixture fixture,
            QaCaseRegistry cases)
        {
            QaIdentityAuthorityFixture.AuthoritySnapshot before = fixture.Initial;
            RouteAsset route = before.Route;
            ActivityAsset activity = before.Activity;

            Require(route != null && route.HasValidRouteId, "Baseline requires a valid current Route.");
            Require(activity != null && activity.HasValidActivityId, "Baseline requires a valid current Activity.");

            Require(before.RouteOwner.IsValid, "Current Route owner is invalid.");
            Require(before.ActivityOwner.IsValid, "Current Activity owner is invalid.");
            Require(
                before.RouteOwner.HasDefinitionToken && before.RouteToken.IsValid,
                "Current Route owner is missing a definition token.");
            Require(
                before.ActivityOwner.HasDefinitionToken && before.ActivityToken.IsValid,
                "Current Activity owner is missing a definition token.");

            RuntimeDefinitionToken routeTokenAgain =
                RuntimeDefinitionToken.FromUnityObject(route);
            RuntimeDefinitionToken activityTokenAgain =
                RuntimeDefinitionToken.FromUnityObject(activity);
            Require(
                routeTokenAgain == before.RouteToken,
                "RuntimeDefinitionToken.FromUnityObject is not stable for the same Route reference.");
            Require(
                activityTokenAgain == before.ActivityToken,
                "RuntimeDefinitionToken.FromUnityObject is not stable for the same Activity reference.");

            RuntimeContentOwner derivedRouteOwner = fixture.DeriveRouteOwner(route);
            RuntimeContentOwner derivedActivityOwner = fixture.DeriveActivityOwner(activity);
            Require(
                derivedRouteOwner == before.RouteOwner,
                "Derived Route owner diverged from the captured snapshot owner.");
            Require(
                derivedActivityOwner == before.ActivityOwner,
                "Derived Activity owner diverged from the captured snapshot owner.");

            RuntimeContentOwner observedRouteOwner =
                fixture.RequireObservedRouteOwner(route);
            RuntimeContentOwner observedActivityOwner =
                fixture.RequireObservedActivityOwner(activity);
            Require(
                observedRouteOwner == derivedRouteOwner,
                "Runtime-observed Route owner does not match the owner derived from the exact Route reference.");
            Require(
                observedActivityOwner == derivedActivityOwner,
                "Runtime-observed Activity owner does not match the owner derived from the exact Activity reference.");

            Require(
                !string.IsNullOrWhiteSpace(route.RouteId.StableText),
                "Route stable ID is blank.");
            Require(
                !string.IsNullOrWhiteSpace(activity.ActivityId.StableText),
                "Activity stable ID is blank.");
            Require(
                derivedRouteOwner.HasSameStableDefinition(before.RouteOwner),
                "Route stable definition evidence was lost.");
            Require(
                derivedActivityOwner.HasSameStableDefinition(before.ActivityOwner),
                "Activity stable definition evidence was lost.");

            QaIdentityAuthorityFixture.AuthoritySnapshot after =
                fixture.CaptureCurrent(Source + ".baseline-after");
            Require(
                ReferenceEquals(after.Route, before.Route),
                "Baseline case altered the current Route reference.");
            Require(
                ReferenceEquals(after.Activity, before.Activity),
                "Baseline case altered the current Activity reference.");
            Require(
                after.RouteOwner == before.RouteOwner &&
                after.ActivityOwner == before.ActivityOwner,
                "Baseline case altered runtime content owners.");
            Require(
                after.TotalRootCount == before.TotalRootCount &&
                after.RouteRootCount == before.RouteRootCount &&
                after.ActivityRootCount == before.ActivityRootCount,
                "Baseline case altered runtime content roots. " +
                $"before=({fixture.DescribeRoots(before)}) after=({fixture.DescribeRoots(after)}).");
            Require(
                after.GameFlowStarted == before.GameFlowStarted &&
                after.IsActivityReady == before.IsActivityReady,
                "Baseline case altered Game Flow readiness state.");

            CaseDiagnostics.Add(
                "case='baseline-authority-snapshot' status='Passed' " +
                fixture.DescribeAuthority(after));
            cases.Complete("baseline-authority-snapshot");
            return Task.CompletedTask;
        }

        private static async Task RunRouteCollisionTransitionAsync(
            QaIdentityAuthorityFixture fixture,
            QaCaseRegistry cases)
        {
            QaIdentityAuthorityFixture.AuthoritySnapshot caseBefore =
                fixture.CaptureCurrent(Source + ".route-collision.before");
            QaIdentityAuthorityFixture.LifecycleListenerScope listeners = null;
            Exception executionFailure = null;

            RouteAsset routeA = null;
            RouteAsset routeB = null;
            ActivityAsset startupA = null;
            ActivityAsset startupB = null;
            RuntimeContentOwner ownerA = default;
            RuntimeContentOwner ownerB = default;
            FrameworkRouteRequestResult transitionResult = default;
            int rootsBefore = 0;
            int rootsAfter = 0;
            int enterCount = 0;
            int exitCount = 0;

            try
            {
                startupA = fixture.CreateTemporaryActivity(
                    "qa.if-id.route.collision.startup.a",
                    "IF-ID Route Collision Startup A");
                startupB = fixture.CreateTemporaryActivity(
                    "qa.if-id.route.collision.startup.b",
                    "IF-ID Route Collision Startup B");

                routeA = fixture.CreateTemporaryRoute(
                    SharedRouteStableId,
                    "IF-ID Route Collision A",
                    caseBefore.Route,
                    startupA);
                routeB = fixture.CreateTemporaryRoute(
                    SharedRouteStableId,
                    "IF-ID Route Collision B",
                    caseBefore.Route,
                    startupB);

                Require(!ReferenceEquals(routeA, routeB), "Route A and B must be distinct references.");
                Require(routeA.HasSameStableId(routeB), "Route A and B must share the same RouteId.");
                Require(
                    !string.Equals(routeA.RouteName, routeB.RouteName, StringComparison.Ordinal),
                    "Route A and B must have distinct diagnostic names.");

                RuntimeDefinitionToken tokenA = RuntimeDefinitionToken.FromUnityObject(routeA);
                RuntimeDefinitionToken tokenB = RuntimeDefinitionToken.FromUnityObject(routeB);
                Require(tokenA.IsValid && tokenB.IsValid, "Route collision tokens must be valid.");
                Require(tokenA != tokenB, "Route A and B must mint distinct definition tokens.");

                ownerA = fixture.DeriveRouteOwner(routeA);
                ownerB = fixture.DeriveRouteOwner(routeB);
                Require(ownerA != ownerB, "Route A and B owners must differ when tokens differ.");
                Require(
                    ownerA.HasSameStableDefinition(ownerB),
                    "Route owners must still share stable-definition diagnostic evidence.");

                FrameworkRouteRequestResult enterA = await fixture.RequestRouteAsync(
                    routeA,
                    Source,
                    "route-collision-enter-a");
                Require(
                    enterA.Succeeded,
                    "Failed to enter Route A before collision transition. " +
                    $"kind='{enterA.Kind}' message='{enterA.Message}'.");
                Require(
                    ReferenceEquals(fixture.Host.State.CurrentRoute, routeA),
                    "Current Route did not become exact Route A reference.");
                Require(
                    enterA.Kind != FrameworkRouteRequestKind.IgnoredAlreadyActive,
                    "Route A enter was incorrectly classified as AlreadyActive.");

                rootsBefore = fixture.RuntimeContent.RootCount;
                int rootsForABeforeTransition = fixture.CountRootsForOwner(ownerA);
                Require(
                    rootsForABeforeTransition >= 1,
                    "Route A root was not present before the collision transition.");

                listeners = fixture.BindLifecycleListeners();
                transitionResult = await fixture.RequestRouteAsync(
                    routeB,
                    Source,
                    "route-collision-transition-a-to-b");

                Require(
                    transitionResult.Kind != FrameworkRouteRequestKind.IgnoredAlreadyActive,
                    "Route B with the same stable ID was incorrectly classified as AlreadyActive.");
                Require(
                    transitionResult.Succeeded,
                    "Route A → B collision transition failed. " +
                    $"kind='{transitionResult.Kind}' message='{transitionResult.Message}'.");
                Require(
                    ReferenceEquals(transitionResult.TargetRoute, routeB),
                    "Route request result target is not the exact Route B reference.");
                Require(
                    ReferenceEquals(fixture.Host.State.CurrentRoute, routeB),
                    "Current Route did not become exact Route B reference after transition.");

                RouteLifecycleStartResult lifecycle = transitionResult.RouteLifecycleResult;
                Require(
                    ReferenceEquals(lifecycle.Route, routeB),
                    "Route lifecycle result did not enter exact Route B.");
                Require(
                    ReferenceEquals(lifecycle.PreviousRoute, routeA),
                    "Route lifecycle result did not exit exact Route A.");
                Require(
                    lifecycle.ReplacedPreviousRoute,
                    "Route lifecycle did not report replacement of the previous Route.");
                Require(
                    lifecycle.HasRouteExitResult,
                    "Route lifecycle did not report Route A exit evidence.");
                Require(
                    lifecycle.RuntimeRouteScopeResult.HasEnterRootResult,
                    "Route B runtime scope enter was not observed.");
                Require(
                    lifecycle.RuntimeRouteScopeResult.HasExitRootResult,
                    "Route A runtime scope exit was not observed.");
                Require(
                    lifecycle.RuntimeRouteScopeResult.Owner == ownerB,
                    "Runtime route scope owner after transition is not Route B owner.");

                enterCount = listeners.RouteEnterCount;
                exitCount = listeners.RouteExitCount;
                Require(exitCount >= 1, "Route exit listener did not observe Route A exit.");
                Require(enterCount >= 1, "Route enter listener did not observe Route B enter.");
                Require(
                    ReferenceEquals(listeners.LastExitedRoute, routeA),
                    "Route exit listener did not observe exact Route A.");
                Require(
                    ReferenceEquals(listeners.LastEnteredRoute, routeB),
                    "Route enter listener did not observe exact Route B.");

                int rootsForAAfter = fixture.CountRootsForOwner(ownerA);
                int rootsForBAfter = fixture.CountRootsForOwner(ownerB);
                rootsAfter = fixture.RuntimeContent.RootCount;
                Require(
                    rootsForBAfter >= 1,
                    "Route B root does not exist after the collision transition.");
                Require(
                    rootsForAAfter == 0,
                    "Route A root remained active after the collision transition. " +
                    $"routeARoots='{rootsForAAfter}' routeBRoots='{rootsForBAfter}'.");
                Require(
                    !fixture.Host.CurrentGameFlowRuntime.CurrentRouteLifecycleRuntime.IsRouteActive(routeA),
                    "Route A is still considered active after transition to Route B.");
                Require(
                    fixture.Host.CurrentGameFlowRuntime.CurrentRouteLifecycleRuntime.IsRouteActive(routeB),
                    "Route B is not considered active after transition.");

                // Stable ID is diagnostic only: success depends on references/tokens/owners.
                Require(
                    routeA.RouteId.StableText == SharedRouteStableId &&
                    routeB.RouteId.StableText == SharedRouteStableId,
                    "Shared Route stable ID diagnostic diverged.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
                throw;
            }
            finally
            {
                fixture.ReleaseLifecycleListeners();
                try
                {
                    await fixture.RestoreToAsync(caseBefore, Source + ".route-collision.restore");
                }
                catch (Exception exception)
                {
                    fixture.Failures.Add("route-collision-restore", exception);
                    if (executionFailure == null)
                    {
                        throw;
                    }
                }

                CaseDiagnostics.Add(
                    "case='route-collision-transition' " +
                    $"status='{(executionFailure == null ? "Passed" : "Failed")}' " +
                    $"routeA='{Escape(routeA != null ? routeA.RouteName : "<null>")}' " +
                    $"routeB='{Escape(routeB != null ? routeB.RouteName : "<null>")}' " +
                    $"sharedStableId='{SharedRouteStableId}' " +
                    $"tokenA='{Escape(routeA != null ? RuntimeDefinitionToken.FromUnityObject(routeA).StableText : "<null>")}' " +
                    $"tokenB='{Escape(routeB != null ? RuntimeDefinitionToken.FromUnityObject(routeB).StableText : "<null>")}' " +
                    $"ownerA='{Escape(ownerA.IsValid ? ownerA.StableText : "<none>")}' " +
                    $"ownerB='{Escape(ownerB.IsValid ? ownerB.StableText : "<none>")}' " +
                    $"requestKind='{transitionResult.Kind}' " +
                    $"requestSucceeded='{transitionResult.Succeeded}' " +
                    $"enterCount='{enterCount}' exitCount='{exitCount}' " +
                    $"rootsBefore='{rootsBefore}' rootsAfter='{rootsAfter}' " +
                    $"executionFailure='{Escape(QaFailureCollector.Describe(executionFailure))}'.");
            }

            cases.Complete("route-collision-transition");
        }

        private static async Task RunActivityCollisionTransitionAsync(
            QaIdentityAuthorityFixture fixture,
            QaCaseRegistry cases)
        {
            QaIdentityAuthorityFixture.AuthoritySnapshot caseBefore =
                fixture.CaptureCurrent(Source + ".activity-collision.before");
            QaIdentityAuthorityFixture.LifecycleListenerScope listeners = null;
            Exception executionFailure = null;

            ActivityAsset activityA = null;
            ActivityAsset activityB = null;
            RuntimeContentOwner ownerA = default;
            RuntimeContentOwner ownerB = default;
            FrameworkActivityRequestResult transitionResult = default;
            int rootsBefore = 0;
            int rootsAfter = 0;
            int enterCount = 0;
            int exitCount = 0;
            int contentEnterBindings = 0;
            int contentExitBindings = 0;
            bool contentLifecycleExecuted = false;

            try
            {
                Require(
                    ReferenceEquals(fixture.Host.State.CurrentRoute, caseBefore.Route),
                    "Activity collision requires the case baseline Route reference.");

                activityA = fixture.CreateTemporaryActivity(
                    SharedActivityStableId,
                    "IF-ID Activity Collision A");
                activityB = fixture.CreateTemporaryActivity(
                    SharedActivityStableId,
                    "IF-ID Activity Collision B");

                Require(!ReferenceEquals(activityA, activityB), "Activity A and B must be distinct references.");
                Require(activityA.HasSameStableId(activityB), "Activity A and B must share the same ActivityId.");
                Require(
                    !string.Equals(activityA.ActivityName, activityB.ActivityName, StringComparison.Ordinal),
                    "Activity A and B must have distinct diagnostic names.");

                RuntimeDefinitionToken tokenA = RuntimeDefinitionToken.FromUnityObject(activityA);
                RuntimeDefinitionToken tokenB = RuntimeDefinitionToken.FromUnityObject(activityB);
                Require(tokenA.IsValid && tokenB.IsValid, "Activity collision tokens must be valid.");
                Require(tokenA != tokenB, "Activity A and B must mint distinct definition tokens.");

                ownerA = fixture.DeriveActivityOwner(activityA);
                ownerB = fixture.DeriveActivityOwner(activityB);
                Require(ownerA != ownerB, "Activity A and B owners must differ when tokens differ.");
                Require(
                    ownerA.HasSameStableDefinition(ownerB),
                    "Activity owners must still share stable-definition diagnostic evidence.");

                FrameworkActivityRequestResult enterA = await fixture.RequestActivityAsync(
                    activityA,
                    Source,
                    "activity-collision-enter-a");
                Require(
                    enterA.Succeeded,
                    "Failed to enter Activity A before collision transition. " +
                    $"kind='{enterA.Kind}' message='{enterA.Message}'.");
                Require(
                    ReferenceEquals(fixture.Host.State.CurrentActivity, activityA),
                    "Current Activity did not become exact Activity A reference.");
                Require(
                    enterA.Kind != FrameworkActivityRequestKind.IgnoredAlreadyActive,
                    "Activity A enter was incorrectly classified as AlreadyActive.");

                rootsBefore = fixture.RuntimeContent.RootCount;
                int rootsForABeforeTransition = fixture.CountRootsForOwner(ownerA);
                Require(
                    rootsForABeforeTransition >= 1,
                    "Activity A root was not present before the collision transition.");

                listeners = fixture.BindLifecycleListeners();
                transitionResult = await fixture.RequestActivityAsync(
                    activityB,
                    Source,
                    "activity-collision-transition-a-to-b");

                Require(
                    transitionResult.Kind != FrameworkActivityRequestKind.IgnoredAlreadyActive,
                    "Activity B with the same stable ID was incorrectly classified as already active.");
                Require(
                    transitionResult.Succeeded,
                    "Activity A → B collision transition failed. " +
                    $"kind='{transitionResult.Kind}' message='{transitionResult.Message}'.");
                Require(
                    ReferenceEquals(transitionResult.TargetActivity, activityB),
                    "Activity request result target is not the exact Activity B reference.");
                Require(
                    ReferenceEquals(fixture.Host.State.CurrentActivity, activityB),
                    "Current Activity did not become exact Activity B reference after transition.");

                ActivityFlowStartResult lifecycle = transitionResult.ActivityFlowResult;
                Require(lifecycle.Started, "Activity lifecycle did not report Activity B start.");
                Require(
                    ReferenceEquals(lifecycle.Activity, activityB),
                    "Activity lifecycle result did not activate exact Activity B.");
                Require(
                    ReferenceEquals(lifecycle.PreviousActivity, activityA),
                    "Activity lifecycle result did not finalize exact Activity A.");
                Require(
                    lifecycle.RuntimeActivityScopeResult.HasEnterRootResult,
                    "Activity B runtime scope enter was not observed.");
                Require(
                    lifecycle.RuntimeActivityScopeResult.HasExitRootResult,
                    "Activity A runtime scope exit was not observed.");
                Require(
                    lifecycle.RuntimeActivityScopeResult.Owner == ownerB,
                    "Runtime activity scope owner after transition is not Activity B owner.");

                // Content profiles are optional: binding counts may be zero, but apply result must exist
                // and must not validate success by stable ID alone.
                contentLifecycleExecuted = lifecycle.ActivityContentLifecycleResult.Executed;
                contentEnterBindings = lifecycle.ActivityContentLifecycleResult.EnterBindingCount;
                contentExitBindings = lifecycle.ActivityContentLifecycleResult.ExitBindingCount;
                Require(
                    !lifecycle.ActivityContentLifecycleResult.HasFailures,
                    "Activity content lifecycle reported failures during the collision transition.");
                Require(
                    ReferenceEquals(lifecycle.Activity, activityB),
                    "Activity flow result did not retain the exact Activity B reference.");
                if (lifecycle.ActivityContentLifecycleResult.ActiveActivity != null)
                {
                    Require(
                        ReferenceEquals(
                            lifecycle.ActivityContentLifecycleResult.ActiveActivity,
                            activityB),
                        "Activity content lifecycle ActiveActivity is not the exact Activity B reference.");
                }

                if (lifecycle.ActivityContentLifecycleResult.PreviousActivity != null)
                {
                    Require(
                        ReferenceEquals(
                            lifecycle.ActivityContentLifecycleResult.PreviousActivity,
                            activityA),
                        "Activity content lifecycle PreviousActivity is not the exact Activity A reference.");
                }

                enterCount = listeners.ActivityEnterCount;
                exitCount = listeners.ActivityExitCount;
                Require(exitCount >= 1, "Activity exit listener did not observe Activity A exit.");
                Require(enterCount >= 1, "Activity enter listener did not observe Activity B enter.");
                Require(
                    ReferenceEquals(listeners.LastExitedActivity, activityA),
                    "Activity exit listener did not observe exact Activity A.");
                Require(
                    ReferenceEquals(listeners.LastEnteredActivity, activityB),
                    "Activity enter listener did not observe exact Activity B.");

                int rootsForAAfter = fixture.CountRootsForOwner(ownerA);
                int rootsForBAfter = fixture.CountRootsForOwner(ownerB);
                rootsAfter = fixture.RuntimeContent.RootCount;
                Require(
                    rootsForBAfter >= 1,
                    "Activity B root does not exist after the collision transition.");
                Require(
                    rootsForAAfter == 0,
                    "Activity A root remained active after the collision transition. " +
                    $"activityARoots='{rootsForAAfter}' activityBRoots='{rootsForBAfter}'.");
                Require(
                    !fixture.Host.CurrentGameFlowRuntime.CurrentRouteLifecycleRuntime.IsActivityActive(activityA),
                    "Activity A is still considered active after transition to Activity B.");
                Require(
                    fixture.Host.CurrentGameFlowRuntime.CurrentRouteLifecycleRuntime.IsActivityActive(activityB),
                    "Activity B is not considered active after transition.");

                Require(
                    activityA.ActivityId.StableText == SharedActivityStableId &&
                    activityB.ActivityId.StableText == SharedActivityStableId,
                    "Shared Activity stable ID diagnostic diverged.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
                throw;
            }
            finally
            {
                fixture.ReleaseLifecycleListeners();
                try
                {
                    await fixture.RestoreToAsync(caseBefore, Source + ".activity-collision.restore");
                }
                catch (Exception exception)
                {
                    fixture.Failures.Add("activity-collision-restore", exception);
                    if (executionFailure == null)
                    {
                        throw;
                    }
                }

                CaseDiagnostics.Add(
                    "case='activity-collision-transition' " +
                    $"status='{(executionFailure == null ? "Passed" : "Failed")}' " +
                    $"activityA='{Escape(activityA != null ? activityA.ActivityName : "<null>")}' " +
                    $"activityB='{Escape(activityB != null ? activityB.ActivityName : "<null>")}' " +
                    $"sharedStableId='{SharedActivityStableId}' " +
                    $"tokenA='{Escape(activityA != null ? RuntimeDefinitionToken.FromUnityObject(activityA).StableText : "<null>")}' " +
                    $"tokenB='{Escape(activityB != null ? RuntimeDefinitionToken.FromUnityObject(activityB).StableText : "<null>")}' " +
                    $"ownerA='{Escape(ownerA.IsValid ? ownerA.StableText : "<none>")}' " +
                    $"ownerB='{Escape(ownerB.IsValid ? ownerB.StableText : "<none>")}' " +
                    $"requestKind='{transitionResult.Kind}' " +
                    $"requestSucceeded='{transitionResult.Succeeded}' " +
                    $"enterCount='{enterCount}' exitCount='{exitCount}' " +
                    $"contentLifecycleExecuted='{contentLifecycleExecuted}' " +
                    $"contentEnterBindings='{contentEnterBindings}' " +
                    $"contentExitBindings='{contentExitBindings}' " +
                    $"rootsBefore='{rootsBefore}' rootsAfter='{rootsAfter}' " +
                    $"executionFailure='{Escape(QaFailureCollector.Describe(executionFailure))}'.");
            }

            cases.Complete("activity-collision-transition");
        }

        private static async Task RunOwnershipReleaseIsolationAsync(
            QaIdentityAuthorityFixture fixture,
            QaCaseRegistry cases)
        {
            const string caseName = "ownership-release-isolation";
            QaIdentityAuthorityFixture.AuthoritySnapshot caseBefore =
                fixture.CaptureCurrent(Source + ".ownership-release.before");
            Exception executionFailure = null;

            ActivityAsset definitionA = null;
            ActivityAsset definitionB = null;
            RuntimeContentOwner ownerA = default;
            RuntimeContentOwner ownerB = default;
            int rootsBeforeCreate = 0;
            int rootsAfterCreate = 0;
            int rootsAfterReleaseA = 0;
            int rootsForAAfter = -1;
            int rootsForBAfter = -1;
            RuntimeRootRegistryOperationStatus releaseStatus =
                RuntimeRootRegistryOperationStatus.Unknown;

            try
            {
                definitionA = fixture.CreateTemporaryActivity(
                    SharedOwnershipStableId,
                    "IF-ID Ownership Release A");
                definitionB = fixture.CreateTemporaryActivity(
                    SharedOwnershipStableId,
                    "IF-ID Ownership Release B");

                Require(!ReferenceEquals(definitionA, definitionB),
                    "Ownership definitions A and B must be distinct references.");
                Require(definitionA.HasSameStableId(definitionB),
                    "Ownership definitions A and B must share the same stable ID.");

                ownerA = fixture.DeriveActivityOwner(definitionA);
                ownerB = fixture.DeriveActivityOwner(definitionB);
                Require(ownerA != ownerB, "Ownership owners A and B must differ.");
                Require(
                    ownerA.DefinitionToken != ownerB.DefinitionToken,
                    "Ownership tokens A and B must differ.");
                Require(
                    ownerA.HasSameStableDefinition(ownerB),
                    "Ownership owners must still share stable-definition diagnostic evidence.");

                rootsBeforeCreate = fixture.RuntimeContent.RootCount;

                RuntimeRootRegistryOperationResult createA = fixture.CreateScopeRoot(
                    ownerA,
                    Source,
                    "ownership-release-create-a");
                RuntimeRootRegistryOperationResult createB = fixture.CreateScopeRoot(
                    ownerB,
                    Source,
                    "ownership-release-create-b");
                Require(
                    createA.Applied && createA.Status == RuntimeRootRegistryOperationStatus.RootCreated,
                    "Failed to create Root A in the real RuntimeContent registry. " +
                    $"status='{createA.Status}' message='{createA.Message}'.");
                Require(
                    createB.Applied && createB.Status == RuntimeRootRegistryOperationStatus.RootCreated,
                    "Failed to create Root B in the real RuntimeContent registry. " +
                    $"status='{createB.Status}' message='{createB.Message}'.");
                Require(
                    createA.Owner == ownerA && createB.Owner == ownerB,
                    "Created roots did not retain the exact owners used at creation.");

                rootsAfterCreate = fixture.RuntimeContent.RootCount;
                Require(
                    rootsAfterCreate == rootsBeforeCreate + 2,
                    "Root count after creating A and B is incoherent. " +
                    $"before='{rootsBeforeCreate}' after='{rootsAfterCreate}'.");
                Require(
                    fixture.CountRootsForOwner(ownerA) == 1 &&
                    fixture.CountRootsForOwner(ownerB) == 1,
                    "Roots A and B were not both present after creation.");

                // Release only A by exact owner/token — never by stable ID alone.
                RuntimeRootRegistryOperationResult releaseA = fixture.RemoveScopeRoot(
                    ownerA,
                    Source,
                    "ownership-release-a");
                releaseStatus = releaseA.Status;
                Require(
                    releaseA.Applied &&
                    releaseA.Status == RuntimeRootRegistryOperationStatus.RootRemoved,
                    "Release of Root A did not apply on the real registry. " +
                    $"status='{releaseA.Status}' message='{releaseA.Message}'.");
                Require(
                    releaseA.Owner == ownerA,
                    "Release result owner is not the exact Owner A.");

                rootsAfterReleaseA = fixture.RuntimeContent.RootCount;
                rootsForAAfter = fixture.CountRootsForOwner(ownerA);
                rootsForBAfter = fixture.CountRootsForOwner(ownerB);

                Require(rootsForAAfter == 0, "Root A remained after release of Owner A.");
                Require(rootsForBAfter == 1, "Root B was affected by release of Owner A.");
                Require(
                    ownerB.IsValid && ownerB.HasDefinitionToken,
                    "Owner B is no longer valid after release of A.");
                Require(
                    rootsAfterReleaseA == rootsBeforeCreate + 1,
                    "Root count after releasing A is incoherent. " +
                    $"beforeCreate='{rootsBeforeCreate}' afterCreate='{rootsAfterCreate}' " +
                    $"afterReleaseA='{rootsAfterReleaseA}'.");

                // Stable-ID-only cleanup would have removed both; B must remain addressable by token.
                Require(
                    fixture.CountRootsForOwner(
                        RuntimeContentOwner.Activity(
                            definitionB.ActivityId.StableText,
                            definitionB.ActivityName,
                            ownerB.DefinitionToken)) == 1,
                    "Owner B root is not addressable by exact token after release of A.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                // Case still owns Root B (and A if create failed mid-way); release by exact owner only.
                try
                {
                    if (ownerB.IsValid && fixture.CountRootsForOwner(ownerB) > 0)
                    {
                        fixture.RemoveScopeRoot(ownerB, Source, "ownership-release-cleanup-b");
                    }

                    if (ownerA.IsValid && fixture.CountRootsForOwner(ownerA) > 0)
                    {
                        fixture.RemoveScopeRoot(ownerA, Source, "ownership-release-cleanup-a");
                    }
                }
                catch (Exception exception)
                {
                    fixture.Failures.Add(caseName + ".root-cleanup", exception);
                }

                Exception cleanupFailure = null;
                try
                {
                    await fixture.FinalizeCaseAsync(
                        caseBefore,
                        caseName,
                        definitionA,
                        definitionB);
                }
                catch (Exception exception)
                {
                    cleanupFailure = exception;
                    if (executionFailure == null)
                    {
                        executionFailure = exception;
                    }
                }

                CaseDiagnostics.Add(
                    $"case='{caseName}' " +
                    $"status='{(executionFailure == null ? "Passed" : "Failed")}' " +
                    $"refA='{Escape(definitionA != null ? definitionA.name : "<null>")}' " +
                    $"refB='{Escape(definitionB != null ? definitionB.name : "<null>")}' " +
                    $"sharedStableId='{SharedOwnershipStableId}' " +
                    $"ownerA='{Escape(ownerA.IsValid ? ownerA.StableText : "<none>")}' " +
                    $"ownerB='{Escape(ownerB.IsValid ? ownerB.StableText : "<none>")}' " +
                    $"tokenA='{Escape(ownerA.IsValid ? ownerA.DefinitionToken.StableText : "<none>")}' " +
                    $"tokenB='{Escape(ownerB.IsValid ? ownerB.DefinitionToken.StableText : "<none>")}' " +
                    $"rootsBefore='{rootsBeforeCreate}' rootsAfterCreate='{rootsAfterCreate}' " +
                    $"rootsAfterReleaseA='{rootsAfterReleaseA}' " +
                    $"rootsForAAfter='{rootsForAAfter}' rootsForBAfter='{rootsForBAfter}' " +
                    $"releaseStatus='{releaseStatus}' " +
                    $"waitOccurrenceSequence='n/a' supersession='n/a' " +
                    $"executionFailure='{Escape(QaFailureCollector.Describe(executionFailure))}' " +
                    $"cleanupFailure='{Escape(QaFailureCollector.Describe(cleanupFailure))}'.");
            }

            if (executionFailure != null)
            {
                throw executionFailure;
            }

            cases.Complete(caseName);
        }

        private static async Task RunReadinessCollisionIsolationAsync(
            QaIdentityAuthorityFixture fixture,
            QaCaseRegistry cases)
        {
            const string caseName = "readiness-collision-isolation";
            QaIdentityAuthorityFixture.AuthoritySnapshot caseBefore =
                fixture.CaptureCurrent(Source + ".readiness-collision.before");
            Exception executionFailure = null;

            ActivityAsset activityA = null;
            ActivityAsset activityB = null;
            RuntimeContentOwner ownerA = default;
            RuntimeContentOwner ownerB = default;
            int occurrenceSequenceA = 1;
            int occurrenceSequenceB = 1;
            int rootsBefore = caseBefore.TotalRootCount;
            int rootsAfter = rootsBefore;
            string waitAState = "<none>";
            string waitBState = "<none>";
            RouteAsset authorityRoute = caseBefore.Route;

            try
            {
                activityA = fixture.CreateTemporaryActivity(
                    SharedReadinessStableId,
                    "IF-ID Readiness Collision A");
                activityB = fixture.CreateTemporaryActivity(
                    SharedReadinessStableId,
                    "IF-ID Readiness Collision B");

                Require(!ReferenceEquals(activityA, activityB),
                    "Readiness activities A and B must be distinct references.");
                Require(activityA.HasSameStableId(activityB),
                    "Readiness activities A and B must share the same ActivityId.");

                ownerA = fixture.DeriveActivityOwner(activityA);
                ownerB = fixture.DeriveActivityOwner(activityB);
                Require(ownerA != ownerB, "Readiness owners A and B must differ.");
                Require(
                    ownerA.DefinitionToken != ownerB.DefinitionToken,
                    "Readiness tokens A and B must differ.");

                var occurrenceA = new ActivityReadinessOccurrence(activityA, occurrenceSequenceA);
                var occurrenceB = new ActivityReadinessOccurrence(activityB, occurrenceSequenceB);
                Require(occurrenceA.IsValid && occurrenceB.IsValid,
                    "Readiness occurrences A and B must be valid.");
                Require(occurrenceA.Matches(activityA, occurrenceSequenceA),
                    "Occurrence A must match Activity A by reference and sequence.");
                Require(!occurrenceA.Matches(activityB, occurrenceSequenceB),
                    "Occurrence A must not match colliding Activity B by stable ID alone.");
                Require(!occurrenceB.Matches(activityA, occurrenceSequenceA),
                    "Occurrence B must not match Activity A by stable ID alone.");

                TransitionOperationId operationIdA =
                    TransitionOperationId.From("qa.if-id.readiness.wait.a");
                TransitionOperationId operationIdB =
                    TransitionOperationId.From("qa.if-id.readiness.wait.b");

                using (var operationA = new ActivityEntryReadinessActiveOperation(
                           operationIdA,
                           occurrenceA,
                           authorityRoute))
                using (var operationB = new ActivityEntryReadinessActiveOperation(
                           operationIdB,
                           occurrenceB,
                           authorityRoute))
                {
                    Require(operationA.OwnsActivity(activityA),
                        "OwnsActivity(activityA) must be true for the wait directed at A.");
                    Require(!operationA.OwnsActivity(activityB),
                        "OwnsActivity(activityB) must be false for the wait directed at A.");
                    Require(operationB.OwnsActivity(activityB),
                        "OwnsActivity(activityB) must be true for the wait directed at B.");
                    Require(!operationB.OwnsActivity(activityA),
                        "OwnsActivity(activityA) must be false for the wait directed at B.");

                    Require(
                        operationA.OwnsRoute(authorityRoute),
                        "Wait A must own the exact authority Route reference.");
                    Require(
                        !operationA.WaitScope.CancellationRequested &&
                        !operationB.WaitScope.CancellationRequested,
                        "Readiness waits must start open.");

                    // An operation directed at B must not complete/cancel/replace A's wait by ID alone.
                    operationB.RequestCancellation(
                        ActivityEntryReadinessInterruptionReason.ActivityAuthorityReplaced);
                    Require(
                        operationB.WaitScope.CancellationRequested,
                        "Wait B did not cancel when directed at B.");
                    Require(
                        !operationA.WaitScope.CancellationRequested,
                        "Wait A was cancelled by an operation directed at colliding Activity B.");
                    Require(
                        operationA.WaitScope.InterruptionReason ==
                            ActivityEntryReadinessInterruptionReason.None,
                        "Wait A acquired an interruption reason from colliding Activity B.");
                    Require(
                        operationB.WaitScope.InterruptionReason ==
                            ActivityEntryReadinessInterruptionReason.ActivityAuthorityReplaced,
                        "Wait B interruption reason diverged.");

                    waitAState =
                        $"open cancelled='{operationA.WaitScope.CancellationRequested}' " +
                        $"reason='{operationA.WaitScope.InterruptionReason}' " +
                        $"sequence='{occurrenceSequenceA}'";
                    waitBState =
                        $"cancelled='{operationB.WaitScope.CancellationRequested}' " +
                        $"reason='{operationB.WaitScope.InterruptionReason}' " +
                        $"sequence='{occurrenceSequenceB}'";

                    // Sequence remains authority: same Activity A, different sequence is not the same wait.
                    var occurrenceA2 = new ActivityReadinessOccurrence(activityA, 2);
                    Require(!occurrenceA.Matches(activityA, 2),
                        "Occurrence sequence must participate in readiness authority.");
                    Require(occurrenceA2.Matches(activityA, 2),
                        "New occurrence for Activity A sequence 2 must match by reference and sequence.");
                }

                // Waits disposed by using-scope; no case roots created.
                rootsAfter = fixture.RuntimeContent.RootCount;
                Require(
                    rootsAfter == rootsBefore,
                    "Readiness collision case leaked runtime content roots.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                Exception cleanupFailure = null;
                try
                {
                    await fixture.FinalizeCaseAsync(
                        caseBefore,
                        caseName,
                        activityA,
                        activityB);
                }
                catch (Exception exception)
                {
                    cleanupFailure = exception;
                    if (executionFailure == null)
                    {
                        executionFailure = exception;
                    }
                }

                CaseDiagnostics.Add(
                    $"case='{caseName}' " +
                    $"status='{(executionFailure == null ? "Passed" : "Failed")}' " +
                    $"refA='{Escape(activityA != null ? activityA.name : "<null>")}' " +
                    $"refB='{Escape(activityB != null ? activityB.name : "<null>")}' " +
                    $"sharedStableId='{SharedReadinessStableId}' " +
                    $"ownerA='{Escape(ownerA.IsValid ? ownerA.StableText : "<none>")}' " +
                    $"ownerB='{Escape(ownerB.IsValid ? ownerB.StableText : "<none>")}' " +
                    $"tokenA='{Escape(ownerA.IsValid ? ownerA.DefinitionToken.StableText : "<none>")}' " +
                    $"tokenB='{Escape(ownerB.IsValid ? ownerB.DefinitionToken.StableText : "<none>")}' " +
                    $"rootsBefore='{rootsBefore}' rootsAfter='{rootsAfter}' " +
                    $"waitA=({Escape(waitAState)}) waitB=({Escape(waitBState)}) " +
                    $"occurrenceASequence='{occurrenceSequenceA}' occurrenceBSequence='{occurrenceSequenceB}' " +
                    $"supersession='n/a' " +
                    $"executionFailure='{Escape(QaFailureCollector.Describe(executionFailure))}' " +
                    $"cleanupFailure='{Escape(QaFailureCollector.Describe(cleanupFailure))}'.");
            }

            if (executionFailure != null)
            {
                throw executionFailure;
            }

            cases.Complete(caseName);
        }

        private static async Task RunLegitimateSupersessionPreservationAsync(
            QaIdentityAuthorityFixture fixture,
            QaCaseRegistry cases)
        {
            const string caseName = "legitimate-supersession-preservation";
            QaIdentityAuthorityFixture.AuthoritySnapshot caseBefore =
                fixture.CaptureCurrent(Source + ".supersession.before");
            Exception executionFailure = null;

            ActivityAsset activityA = null;
            ActivityAsset collidingB = null;
            RouteAsset authorityRoute = caseBefore.Route;
            int rootsBefore = caseBefore.TotalRootCount;
            int rootsAfter = rootsBefore;
            string supersessionResult = "<none>";
            string interruptionReason = "<none>";
            int occurrence1Sequence = 1;
            int occurrence2Sequence = 2;

            try
            {
                activityA = fixture.CreateTemporaryActivity(
                    SharedSupersessionStableId,
                    "IF-ID Supersession Activity A");
                collidingB = fixture.CreateTemporaryActivity(
                    SharedSupersessionStableId,
                    "IF-ID Supersession Colliding B");

                Require(!ReferenceEquals(activityA, collidingB),
                    "Supersession activities must be distinct references.");
                Require(activityA.HasSameStableId(collidingB),
                    "Supersession activities must share the same ActivityId for the collision probe.");

                var occurrence1 = new ActivityReadinessOccurrence(activityA, occurrence1Sequence);
                var occurrence2 = new ActivityReadinessOccurrence(activityA, occurrence2Sequence);
                var occurrenceColliding = new ActivityReadinessOccurrence(collidingB, 1);

                TransitionOperationId operationId1 =
                    TransitionOperationId.From("qa.if-id.supersession.wait.1");
                TransitionOperationId operationId2 =
                    TransitionOperationId.From("qa.if-id.supersession.wait.2");
                TransitionOperationId operationIdColliding =
                    TransitionOperationId.From("qa.if-id.supersession.wait.colliding");

                using (var operation1 = new ActivityEntryReadinessActiveOperation(
                           operationId1,
                           occurrence1,
                           authorityRoute))
                using (var operation2 = new ActivityEntryReadinessActiveOperation(
                           operationId2,
                           occurrence2,
                           authorityRoute))
                using (var operationColliding = new ActivityEntryReadinessActiveOperation(
                           operationIdColliding,
                           occurrenceColliding,
                           authorityRoute))
                {
                    Require(operation1.OwnsActivity(activityA),
                        "First occurrence wait must own Activity A.");
                    Require(operation2.OwnsActivity(activityA),
                        "Second occurrence wait must own Activity A.");
                    Require(!operationColliding.OwnsActivity(activityA),
                        "Colliding definition wait must not own Activity A.");
                    Require(!operation1.OwnsActivity(collidingB),
                        "First occurrence wait must not own colliding Activity B.");

                    // Legitimate supersession: Route authority replaced on the correct wait.
                    operation1.RequestCancellation(
                        ActivityEntryReadinessInterruptionReason.RouteAuthorityReplaced,
                        "IF-ID Replacement Route");
                    Require(
                        operation1.WaitScope.CancellationRequested,
                        "Legitimate RouteAuthorityReplaced did not interrupt occurrence 1.");
                    Require(
                        operation1.WaitScope.InterruptionReason ==
                            ActivityEntryReadinessInterruptionReason.RouteAuthorityReplaced,
                        "Occurrence 1 interruption is not RouteAuthorityReplaced.");
                    interruptionReason = operation1.WaitScope.InterruptionReason.ToString();
                    Require(
                        operation1.WaitScope.CancellationDiagnostic.IndexOf(
                            "RouteAuthorityReplaced",
                            StringComparison.Ordinal) >= 0,
                        "RouteAuthorityReplaced diagnostic was not preserved.");

                    // Map to typed Superseded — not generic Cancelled.
                    var supersededWait = ActivityEntryReadinessWaitResult.Supersession(
                        occurrence1,
                        default,
                        operation1.WaitScope.CancellationDiagnostic,
                        revision: 1);
                    Require(supersededWait.Superseded, "Supersession wait result is not Superseded.");
                    Require(!supersededWait.Cancelled, "Supersession wait result was classified as Cancelled.");

                    ActivityEntryReadinessExecutionStatus supersededExecution =
                        GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Superseded);
                    ActivityEntryReadinessExecutionStatus cancelledExecution =
                        GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Cancelled);
                    Require(
                        supersededExecution == ActivityEntryReadinessExecutionStatus.Superseded,
                        "MapWaitStatus lost Superseded classification.");
                    Require(
                        cancelledExecution == ActivityEntryReadinessExecutionStatus.Cancelled,
                        "MapWaitStatus lost Cancelled classification.");
                    Require(
                        supersededExecution != cancelledExecution,
                        "Superseded was collapsed into generic Cancelled.");

                    // Framework route result remains typed as superseded, not success.
                    var supersededRouteResult = new FrameworkRouteRequestResult(
                        FrameworkRouteRequestKind.SupersededCommittedTargetByRouteReplacement,
                        "superseded-by-route-authority",
                        null,
                        Source,
                        "RouteAuthorityReplaced",
                        default);
                    Require(supersededRouteResult.Superseded,
                        "FrameworkRouteRequestResult.Superseded is false for RouteAuthorityReplaced kind.");
                    Require(!supersededRouteResult.Succeeded,
                        "Superseded route result was treated as Succeeded.");
                    Require(!supersededRouteResult.DestinationAuthoritative,
                        "Superseded route result was treated as destination-authoritative.");

                    supersessionResult =
                        $"waitStatus='{supersededWait.Status}' " +
                        $"executionStatus='{supersededExecution}' " +
                        $"routeKind='{supersededRouteResult.Kind}' " +
                        $"interruption='{interruptionReason}'";

                    // New occurrence of the correct definition becomes the current open wait.
                    Require(
                        !operation2.WaitScope.CancellationRequested,
                        "New occurrence of Activity A was incorrectly interrupted by occurrence 1 supersession.");
                    Require(
                        occurrence2.Matches(activityA, occurrence2Sequence) &&
                        !occurrence1.Matches(activityA, occurrence2Sequence),
                        "New occurrence did not become a distinct authority for Activity A.");

                    // Colliding definition cannot produce that supersession against A's wait by ID alone.
                    operationColliding.RequestCancellation(
                        ActivityEntryReadinessInterruptionReason.RouteAuthorityReplaced,
                        "Colliding Replacement");
                    Require(
                        operationColliding.WaitScope.CancellationRequested,
                        "Colliding wait was not cancelled when directed at colliding B.");
                    Require(
                        !operation2.WaitScope.CancellationRequested,
                        "Activity A occurrence 2 was superseded by a colliding definition using only stable ID.");
                    Require(
                        operation2.WaitScope.InterruptionReason ==
                            ActivityEntryReadinessInterruptionReason.None,
                        "Activity A occurrence 2 acquired RouteAuthorityReplaced from colliding B.");

                    // ActivityAuthorityReplaced remains typed cancellation, not RouteAuthorityReplaced supersession.
                    operation2.RequestCancellation(
                        ActivityEntryReadinessInterruptionReason.ActivityAuthorityReplaced);
                    Require(
                        operation2.WaitScope.InterruptionReason ==
                            ActivityEntryReadinessInterruptionReason.ActivityAuthorityReplaced,
                        "ActivityAuthorityReplaced was not preserved on occurrence 2.");
                    var cancelledWait = ActivityEntryReadinessWaitResult.Cancellation(
                        occurrence2,
                        default,
                        operation2.WaitScope.CancellationDiagnostic,
                        revision: 1);
                    Require(cancelledWait.Cancelled && !cancelledWait.Superseded,
                        "ActivityAuthorityReplaced wait was converted into Superseded.");
                    Require(
                        GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Cancelled) ==
                            ActivityEntryReadinessExecutionStatus.Cancelled,
                        "ActivityAuthorityReplaced mapping lost Cancelled typing.");
                }

                rootsAfter = fixture.RuntimeContent.RootCount;
                Require(
                    rootsAfter == rootsBefore,
                    "Supersession case leaked runtime content roots.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                Exception cleanupFailure = null;
                try
                {
                    await fixture.FinalizeCaseAsync(
                        caseBefore,
                        caseName,
                        activityA,
                        collidingB);
                }
                catch (Exception exception)
                {
                    cleanupFailure = exception;
                    if (executionFailure == null)
                    {
                        executionFailure = exception;
                    }
                }

                CaseDiagnostics.Add(
                    $"case='{caseName}' " +
                    $"status='{(executionFailure == null ? "Passed" : "Failed")}' " +
                    $"refA='{Escape(activityA != null ? activityA.name : "<null>")}' " +
                    $"refB='{Escape(collidingB != null ? collidingB.name : "<null>")}' " +
                    $"sharedStableId='{SharedSupersessionStableId}' " +
                    $"rootsBefore='{rootsBefore}' rootsAfter='{rootsAfter}' " +
                    $"occurrence1Sequence='{occurrence1Sequence}' occurrence2Sequence='{occurrence2Sequence}' " +
                    $"interruptionReason='{Escape(interruptionReason)}' " +
                    $"supersession=({Escape(supersessionResult)}) " +
                    $"executionFailure='{Escape(QaFailureCollector.Describe(executionFailure))}' " +
                    $"cleanupFailure='{Escape(QaFailureCollector.Describe(cleanupFailure))}'.");
            }

            if (executionFailure != null)
            {
                throw executionFailure;
            }

            cases.Complete(caseName);
        }

        private static void EmitFinalReport(
            QaFailureCollector failures,
            QaCaseRegistry cases,
            QaIdentityAuthorityFixture.AuthoritySnapshot initial,
            QaIdentityAuthorityFixture.AuthoritySnapshot final,
            QaIdentityAuthorityFixture fixture,
            TimeSpan duration)
        {
            bool passed = !failures.HasFailures && cases.Count == cases.ExpectedCount;
            string status = passed ? "Passed" : "Failed";
            int failedCases = cases.ExpectedCount - cases.Count;
            if (!passed && failedCases == 0)
            {
                failedCases = 1;
            }

            string authority = fixture != null
                ? fixture.DescribeAuthority(final ?? initial)
                : "authority='unavailable'";
            string initialRoots = fixture != null && initial != null
                ? fixture.DescribeRoots(initial)
                : "roots='unavailable'";
            string finalRoots = fixture != null && final != null
                ? fixture.DescribeRoots(final)
                : "roots='unavailable'";
            string caseDetails = CaseDiagnostics.Count == 0
                ? "casesDetail='<none>'"
                : "casesDetail=[" + string.Join(" | ", CaseDiagnostics) + "]";

            string message =
                $"{LogPrefix} status='{status}' " +
                $"executed='{cases.ExpectedCount}' completed='{cases.Count}' " +
                $"completedNames='{cases.DescribeCompleted()}' " +
                $"failed='{failedCases}' " +
                $"missing='{cases.DescribeMissing()}' " +
                $"expected='{string.Join(",", ExpectedCases)}' " +
                $"durationMs='{(long)duration.TotalMilliseconds}' " +
                $"{authority} " +
                $"rootsBefore=({initialRoots}) rootsAfter=({finalRoots}) " +
                $"{caseDetails} " +
                $"executionFailure='{Escape(failures.Describe("execution"))}' " +
                $"cleanupFailure='{Escape(failures.Describe("cleanup"))}' " +
                $"teardownFailure='{Escape(failures.Describe("teardown"))}'.";

            if (passed)
            {
                Debug.Log(message);
            }
            else
            {
                Debug.LogError(message);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

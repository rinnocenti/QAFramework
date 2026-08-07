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
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Canonical public runner for Identity Authority (IF-ID) smokes.
    /// Corte 3 adds Route and Activity collision transitions on the same runner.
    /// </summary>
    public static class QaRouteActivityIdentityRegression
    {
        private const string MenuPath =
            "Immersive Framework QA/Game Flow/Run Identity Authority Regression";
        private const string LogPrefix = "[IF_ID_QA]";
        private const string Source = nameof(QaRouteActivityIdentityRegression);
        private const int ExpectedCaseCount = 3;

        private const string SharedRouteStableId = "qa.if-id.route.collision";
        private const string SharedActivityStableId = "qa.if-id.activity.collision";

        private static readonly string[] ExpectedCases =
        {
            "baseline-authority-snapshot",
            "route-collision-transition",
            "activity-collision-transition"
        };

        private static bool s_running;
        private static readonly List<string> CaseDiagnostics = new List<string>();

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            EditorApplication.isPlaying && !s_running;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            if (s_running)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' reason='concurrent-execution-rejected' " +
                    "message='Identity Authority Regression is already running.'.");
                return;
            }

            s_running = true;
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
                s_running = false;
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

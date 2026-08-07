using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Canonical public runner for Identity Authority (IF-ID) smokes.
    /// Corte 2 ships the runner, fixture and baseline snapshot case only.
    /// </summary>
    public static class QaRouteActivityIdentityRegression
    {
        private const string MenuPath =
            "Immersive Framework QA/Game Flow/Run Identity Authority Regression";
        private const string LogPrefix = "[IF_ID_QA]";
        private const string Source = nameof(QaRouteActivityIdentityRegression);
        private const int ExpectedCaseCount = 1;

        private static readonly string[] ExpectedCases =
        {
            "baseline-authority-snapshot"
        };

        private static bool s_running;

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

            // Stable IDs remain diagnostic evidence only; operational equality includes the token.
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

            cases.Complete("baseline-authority-snapshot");
            return Task.CompletedTask;
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

            string message =
                $"{LogPrefix} status='{status}' " +
                $"executed='{cases.ExpectedCount}' completed='{cases.Count}' " +
                $"completedNames='{cases.DescribeCompleted()}' " +
                $"failed='{failedCases}' " +
                $"missing='{cases.DescribeMissing()}' " +
                $"durationMs='{(long)duration.TotalMilliseconds}' " +
                $"{authority} " +
                $"rootsBefore=({initialRoots}) rootsAfter=({finalRoots}) " +
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

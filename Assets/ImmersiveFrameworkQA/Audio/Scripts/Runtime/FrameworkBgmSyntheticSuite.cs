using System;
using System.Collections;
using Immersive.Audio.Authoring;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.Audio;
using UnityEngine;

namespace ImmersiveFrameworkQA.Audio
{
    internal static class FrameworkBgmSyntheticSuite
    {
        private const float ProviderStopTimeoutSeconds = 5f;

        internal static IEnumerator Run(FrameworkBgmQaPanel fixture, Action<SyntheticSuiteResult> completed)
        {
            var result = new SyntheticSuiteResult();
            if (fixture == null || fixture.Director == null || fixture.RuntimeHost == null)
            {
                result.Fail("framework-bgm", "fixture", "configured director and host", "fixture missing", "Synthetic fixture is not configured.");
                completed?.Invoke(result);
                yield break;
            }

            FrameworkBgmDirector director = fixture.Director;
            AudioRuntimeHost host = fixture.RuntimeHost;
            AudioBgmCueAsset routeCue = fixture.ExpectedRouteBgm;
            AudioBgmCueAsset activityCue = fixture.ExpectedOwnActivityBgm;
            AudioBgmCueAsset startupCue = fixture.ExpectedStartupActivityBgm;
            AudioSource releaseSource = null;
            bool abort = false;

            try
            {
                director.ClearRouteBgm(null);
                Assert(result, "framework-bgm", "route-apply", director.SetRouteBgm(routeCue), FrameworkBgmOperationOutcome.Applied, routeCue);
                Assert(result, "framework-bgm", "startup-activity-precedence", director.SetActivityBgm(startupCue, FrameworkBgmActivityPolicy.UseOwnOrRoute), FrameworkBgmOperationOutcome.Applied, startupCue);
                Assert(result, "framework-bgm", "activity-own", director.SetActivityBgm(activityCue, FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit), FrameworkBgmOperationOutcome.Applied, activityCue);

                FrameworkBgmOperationResult retain = director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit);
                Check(result, "framework-bgm", "retain-confirmed-activity", retain.Outcome == FrameworkBgmOperationOutcome.NoChange && director.RetainedActivityBgmForCurrentRoute == activityCue && director.ConfirmedBgm == activityCue, "NoChange with retained confirmed activity cue", Describe(retain));
                Assert(result, "framework-bgm", "use-route", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseRoute), FrameworkBgmOperationOutcome.Applied, routeCue);
                Assert(result, "framework-bgm", "silence-release", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.Silence), FrameworkBgmOperationOutcome.Released, null);

                Component service = host.BgmService as Component;
                releaseSource = service != null ? service.GetComponent<AudioSource>() : null;
            }
            catch (Exception exception)
            {
                result.Fail("framework-bgm", "unexpected-exception", "no exception", exception.GetType().Name, exception.Message);
                abort = true;
            }

            if (abort)
            {
                completed?.Invoke(result);
                yield break;
            }

            if (releaseSource != null)
            {
                float stopDeadline = Time.realtimeSinceStartup + ProviderStopTimeoutSeconds;
                while (releaseSource != null && releaseSource.isPlaying && Time.realtimeSinceStartup < stopDeadline)
                {
                    yield return null;
                }
            }

            AudioSource applySource = null;
            try
            {
                Assert(result, "framework-bgm", "clear-activity-route", director.ClearActivityBgm(null), FrameworkBgmOperationOutcome.Applied, routeCue);
                director.SetActivityBgm(activityCue, FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit);
                FrameworkBgmOperationResult routeExit = director.ClearRouteBgm(null);
                Check(result, "framework-bgm", "route-exit-clears-retention", director.RetainedActivityBgmForCurrentRoute == null, "retained=<null>", Describe(routeExit));

                Assert(result, "adr013a", "apply-success", director.SetRouteBgm(routeCue), FrameworkBgmOperationOutcome.Applied, routeCue);
                applySource = RequireProviderSource(result, host, "apply-rejection");
            }
            catch (Exception exception)
            {
                result.Fail("framework-bgm", "unexpected-exception", "no exception", exception.GetType().Name, exception.Message);
                abort = true;
            }

            if (abort || applySource == null)
            {
                completed?.Invoke(result);
                yield break;
            }

            try
            {
                UnityEngine.Object.DestroyImmediate(applySource);
                FrameworkBgmOperationResult rejected = director.SetActivityBgm(activityCue, FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit);
                Check(result, "adr013a", "apply-rejection", rejected.Outcome == FrameworkBgmOperationOutcome.Rejected && rejected.PreviousConfirmedCue == routeCue && director.ConfirmedBgm == routeCue, "Rejected; previous/confirmed=RouteCue", Describe(rejected));
                Check(result, "adr013a", "rejected-not-retained", director.RetainedActivityBgmForCurrentRoute != activityCue && director.ConfirmedBgm == routeCue, "rejected cue not retained; confirmed=RouteCue", Describe(rejected));
                host.Compose();
                Assert(result, "adr013a", "apply-retry", director.Refresh(), FrameworkBgmOperationOutcome.Applied, activityCue);

                Assert(result, "adr013a", "apply-no-change", director.Refresh(), FrameworkBgmOperationOutcome.NoChange, activityCue);
                Assert(result, "adr013a", "release-success", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.Silence), FrameworkBgmOperationOutcome.Released, null);
                Assert(result, "adr013a", "release-no-change", director.Refresh(), FrameworkBgmOperationOutcome.NoChange, null);

                var unavailableRoot = new GameObject("QA_Synthetic_Bgm_NoAuthority");
                var unavailableDirector = unavailableRoot.AddComponent<FrameworkBgmDirector>();
                FrameworkBgmOperationResult unavailable = unavailableDirector.SetRouteBgm(routeCue);
                Check(result, "adr013a", "optional-authority-unavailable", unavailable.Outcome == FrameworkBgmOperationOutcome.OptionalAuthorityUnavailable && unavailableDirector.ConfirmedBgm == null, "OptionalAuthorityUnavailable; confirmed=<null>", Describe(unavailable));
                UnityEngine.Object.DestroyImmediate(unavailableRoot);

                RunReleaseRejection(result, director, host, routeCue);
            }
            catch (Exception exception)
            {
                result.Fail("framework-bgm", "unexpected-exception", "no exception", exception.GetType().Name, exception.Message);
            }

            completed?.Invoke(result);
        }

        private static void RunReleaseRejection(SyntheticSuiteResult result, FrameworkBgmDirector director, AudioRuntimeHost host, AudioBgmCueAsset routeCue)
        {
            Assert(result, "adr013a", "release-rejection-baseline", director.SetRouteBgm(routeCue), FrameworkBgmOperationOutcome.Applied, routeCue);
            AudioSource source = RequireProviderSource(result, host, "release-rejection");
            if (source == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(source);
            FrameworkBgmOperationResult rejected = director.SetActivityBgm(null, FrameworkBgmActivityPolicy.Silence);
            Check(result, "adr013a", "release-rejection", rejected.Outcome == FrameworkBgmOperationOutcome.Rejected && director.ConfirmedBgm == routeCue, "Rejected; confirmed=RouteCue", Describe(rejected));
            host.Compose();
            Assert(result, "adr013a", "release-retry", director.Refresh(), FrameworkBgmOperationOutcome.Released, null);
        }

        private static AudioSource RequireProviderSource(SyntheticSuiteResult result, AudioRuntimeHost host, string caseName)
        {
            Component service = host.BgmService as Component;
            AudioSource source = service != null ? service.GetComponent<AudioSource>() : null;
            if (source == null)
            {
                result.Fail("adr013a", caseName, "provider source available", "source missing", "AudioRuntimeHost did not expose the real BGM service source.");
            }

            return source;
        }

        private static void Assert(SyntheticSuiteResult result, string group, string name, FrameworkBgmOperationResult operation, FrameworkBgmOperationOutcome outcome, AudioBgmCueAsset confirmed)
        {
            Check(result, group, name, operation.Outcome == outcome && operation.ConfirmedCue == confirmed, $"outcome={outcome}; confirmed={NameOf(confirmed)}", Describe(operation));
        }

        private static void Check(SyntheticSuiteResult result, string group, string name, bool passed, string expected, string actual)
        {
            if (passed) result.Pass(group, name, expected, actual); else result.Fail(group, name, expected, actual, "Synthetic assertion failed.");
        }

        private static string Describe(FrameworkBgmOperationResult result) => $"outcome={result.Outcome}; requested={NameOf(result.RequestedCue)}; confirmed={NameOf(result.ConfirmedCue)}; reason={result.Reason}";
        private static string NameOf(AudioBgmCueAsset cue) => cue != null ? cue.name : "<null>";

        internal sealed class SyntheticSuiteResult
        {
            internal int Passed { get; private set; }
            internal int Failed { get; private set; }
            internal int FrameworkBgmPassed { get; private set; }
            internal int FrameworkBgmFailed { get; private set; }
            internal int Adr013aPassed { get; private set; }
            internal int Adr013aFailed { get; private set; }

            internal int FrameworkBgmTotal => FrameworkBgmPassed + FrameworkBgmFailed;
            internal int Adr013aTotal => Adr013aPassed + Adr013aFailed;

            internal void Pass(string group, string name, string expected, string actual)
            {
                Passed++;
                CountGroup(group, true);
                Debug.Log($"[AUDIO_QA] group='{group}' case='{name}' status='Passed' expected='{expected}' actual='{actual}'");
            }

            internal void Fail(string group, string name, string expected, string actual, string reason)
            {
                Failed++;
                CountGroup(group, false);
                Debug.LogError($"[AUDIO_QA] group='{group}' case='{name}' status='Failed' expected='{expected}' actual='{actual}' reason='{reason}'");
            }

            private void CountGroup(string group, bool passed)
            {
                if (group == "framework-bgm")
                {
                    if (passed) FrameworkBgmPassed++; else FrameworkBgmFailed++;
                    return;
                }

                if (group == "adr013a")
                {
                    if (passed) Adr013aPassed++; else Adr013aFailed++;
                }
            }
        }
    }
}

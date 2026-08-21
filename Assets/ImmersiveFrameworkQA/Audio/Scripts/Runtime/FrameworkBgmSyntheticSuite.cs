using System;
using System.Collections;
using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using Immersive.Audio.Unity.Services;
using Immersive.Framework.Audio;
using UnityEngine;

namespace ImmersiveFrameworkQA.Audio
{
    internal static class FrameworkBgmSyntheticSuite
    {
        private const float ProviderStopTimeoutSeconds = 5f;
        private const float TransitionSettleSeconds = 0.2f;

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

            if (routeCue == null || activityCue == null || startupCue == null)
            {
                result.Fail("framework-bgm", "fixture-cues", "route/startup/activity cues assigned", "one or more cues missing", "Run the Audio QA configurator.");
                completed?.Invoke(result);
                yield break;
            }

            // Establish a deterministic explicit-silence baseline. This is setup, not one of the
            // contract assertions below.
            director.SetActivityBgm(null, FrameworkBgmActivityPolicy.Silence);
            yield return WaitForProviderStop(host);

            try
            {
                Assert(result, "framework-bgm", "route-play-own", director.SetRouteBgm(routeCue, FrameworkBgmRoutePolicy.PlayOwn), FrameworkBgmOperationOutcome.Applied, routeCue, false);
                Assert(result, "framework-bgm", "same-confirmed-route", director.SetRouteBgm(routeCue, FrameworkBgmRoutePolicy.PlayOwn), FrameworkBgmOperationOutcome.NoChange, routeCue, false);

                Assert(result, "framework-bgm", "activity-play-before-route-replacement", director.SetActivityBgm(activityCue, FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent), FrameworkBgmOperationOutcome.Applied, activityCue, false);
                Assert(result, "framework-bgm", "route-play-own-replaces-activity", director.SetRouteBgm(routeCue, FrameworkBgmRoutePolicy.PlayOwn), FrameworkBgmOperationOutcome.Applied, routeCue, false);

                Assert(result, "framework-bgm", "activity-play-before-route-preserve", director.SetActivityBgm(activityCue, FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent), FrameworkBgmOperationOutcome.Applied, activityCue, false);
                Assert(result, "framework-bgm", "route-preserve-current-keeps-activity", director.SetRouteBgm(null, FrameworkBgmRoutePolicy.PreserveCurrent), FrameworkBgmOperationOutcome.NoChange, activityCue, false);
                Assert(result, "framework-bgm", "use-route-inherits-preserve", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseRoute), FrameworkBgmOperationOutcome.NoChange, activityCue, false);
                Assert(result, "framework-bgm", "use-own-or-route-inherits-preserve", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseOwnOrRoute), FrameworkBgmOperationOutcome.NoChange, activityCue, false);
                Assert(result, "framework-bgm", "use-own-or-preserve-ignores-route-preserve", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent), FrameworkBgmOperationOutcome.NoChange, activityCue, false);

                Assert(result, "framework-bgm", "activity-play-before-route-silence", director.SetActivityBgm(activityCue, FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent), FrameworkBgmOperationOutcome.NoChange, activityCue, false);
                Assert(result, "framework-bgm", "route-silence", director.SetRouteBgm(null, FrameworkBgmRoutePolicy.Silence), FrameworkBgmOperationOutcome.Released, null, true);
                Assert(result, "framework-bgm", "use-route-inherits-silence", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseRoute), FrameworkBgmOperationOutcome.NoChange, null, true);
                Assert(result, "framework-bgm", "use-own-or-route-inherits-silence", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseOwnOrRoute), FrameworkBgmOperationOutcome.NoChange, null, true);
                Assert(result, "framework-bgm", "use-own-or-preserve-ignores-route-silence", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent), FrameworkBgmOperationOutcome.NoChange, null, true);

                Assert(result, "framework-bgm", "route-play-own-before-use-route", director.SetRouteBgm(routeCue, FrameworkBgmRoutePolicy.PlayOwn), FrameworkBgmOperationOutcome.Applied, routeCue, false);
                Assert(result, "framework-bgm", "use-route-inherits-play", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseRoute), FrameworkBgmOperationOutcome.NoChange, routeCue, false);
                Assert(result, "framework-bgm", "use-own-or-preserve-ignores-route-play", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent), FrameworkBgmOperationOutcome.NoChange, routeCue, false);
                Assert(result, "framework-bgm", "use-own-or-route-own-cue-wins", director.SetActivityBgm(activityCue, FrameworkBgmActivityPolicy.UseOwnOrRoute), FrameworkBgmOperationOutcome.Applied, activityCue, false);

                FrameworkBgmOperationResult clearActivity = director.ClearActivityBgm(activityCue);
                Check(
                    result,
                    "framework-bgm",
                    "activity-exit-does-not-restore-route",
                    clearActivity.Operation == FrameworkBgmOperation.Preserve
                        && clearActivity.Outcome == FrameworkBgmOperationOutcome.NoChange
                        && clearActivity.RequestedCue == null
                        && !clearActivity.RequestedExplicitSilence
                        && director.ConfirmedBgm == activityCue,
                    "NoChange; requested=<null>; confirmed=ActivityCue",
                    Describe(clearActivity));

                FrameworkBgmOperationResult clearRoute = director.ClearRouteBgm(routeCue, FrameworkBgmRoutePolicy.PlayOwn);
                Check(
                    result,
                    "framework-bgm",
                    "route-exit-does-not-stop-or-restore",
                    clearRoute.Operation == FrameworkBgmOperation.Preserve
                        && clearRoute.Outcome == FrameworkBgmOperationOutcome.NoChange
                        && clearRoute.RequestedCue == null
                        && director.ConfirmedBgm == activityCue,
                    "NoChange; requested=<null>; confirmed=ActivityCue",
                    Describe(clearRoute));

                Assert(result, "framework-bgm", "startup-route-is-deferred", director.SetRouteBgm(routeCue, FrameworkBgmRoutePolicy.PlayOwn, true), FrameworkBgmOperationOutcome.NoChange, activityCue, false);
                Assert(result, "framework-bgm", "startup-activity-prevents-route-transient-play", director.SetActivityBgm(startupCue, FrameworkBgmActivityPolicy.UseOwnOrRoute), FrameworkBgmOperationOutcome.Applied, startupCue, false);

                Assert(result, "framework-bgm", "explicit-silence", director.SetActivityBgm(null, FrameworkBgmActivityPolicy.Silence), FrameworkBgmOperationOutcome.Released, null, true);
                FrameworkBgmOperationResult clearAfterSilence = director.ClearActivityBgm(null);
                Check(
                    result,
                    "framework-bgm",
                    "owner-exit-preserves-silence",
                    clearAfterSilence.Outcome == FrameworkBgmOperationOutcome.NoChange
                        && clearAfterSilence.RequestedCue == null
                        && !clearAfterSilence.RequestedExplicitSilence
                        && director.ConfirmedBgm == null
                        && director.ConfirmedExplicitSilence,
                    "NoChange; no new request; confirmed explicit silence",
                    Describe(clearAfterSilence));

                FrameworkBgmOperationResult noRouteAfterSilence = director.SetRouteBgm(null, FrameworkBgmRoutePolicy.PreserveCurrent);
                Check(
                    result,
                    "framework-bgm",
                    "preserve-after-silence",
                    noRouteAfterSilence.Operation == FrameworkBgmOperation.Preserve
                        && noRouteAfterSilence.Outcome == FrameworkBgmOperationOutcome.NoChange
                        && director.ConfirmedBgm == null
                        && director.ConfirmedExplicitSilence,
                    "NoChange; confirmed explicit silence",
                    Describe(noRouteAfterSilence));

                Assert(result, "framework-bgm", "play-after-silence", director.SetRouteBgm(routeCue, FrameworkBgmRoutePolicy.PlayOwn), FrameworkBgmOperationOutcome.Applied, routeCue, false);
                Assert(result, "framework-bgm", "route-exit-sticky-play", director.ClearRouteBgm(routeCue, FrameworkBgmRoutePolicy.PlayOwn), FrameworkBgmOperationOutcome.NoChange, routeCue, false);

                RunProviderRejectionCases(result, director, host, routeCue, activityCue);
            }
            catch (Exception exception)
            {
                result.Fail("framework-bgm", "unexpected-exception", "no exception", exception.GetType().Name, exception.Message);
                completed?.Invoke(result);
                yield break;
            }

            // The rejection helper can leave a pending intent that needs one frame after Compose.
            yield return null;

            yield return RunPhysicalProviderContinuity(result, host, routeCue, activityCue);
            completed?.Invoke(result);
        }

        private static void RunProviderRejectionCases(
            SyntheticSuiteResult result,
            FrameworkBgmDirector director,
            AudioRuntimeHost host,
            AudioBgmCueAsset routeCue,
            AudioBgmCueAsset activityCue)
        {
            AudioSource source = RequireProviderSource(result, host, "apply-rejection");
            if (source == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(source);
            FrameworkBgmOperationResult rejectedApply = director.SetActivityBgm(activityCue, FrameworkBgmActivityPolicy.UseOwnOrPreserveCurrent);
            Check(
                result,
                "adr013a",
                "apply-rejection",
                rejectedApply.Outcome == FrameworkBgmOperationOutcome.Rejected
                    && rejectedApply.PreviousConfirmedCue == routeCue
                    && director.ConfirmedBgm == routeCue,
                "Rejected; confirmed remains RouteCue",
                Describe(rejectedApply));

            host.Compose();
            Assert(result, "adr013a", "apply-retry", director.Refresh(), FrameworkBgmOperationOutcome.Applied, activityCue, false);

            source = RequireProviderSource(result, host, "release-rejection");
            if (source == null)
            {
                return;
            }

            UnityEngine.Object.DestroyImmediate(source);
            FrameworkBgmOperationResult rejectedRelease = director.SetActivityBgm(null, FrameworkBgmActivityPolicy.Silence);
            Check(
                result,
                "adr013a",
                "release-rejection",
                rejectedRelease.Outcome == FrameworkBgmOperationOutcome.Rejected
                    && director.ConfirmedBgm == activityCue
                    && !director.ConfirmedExplicitSilence,
                "Rejected; confirmed remains ActivityCue and not silence",
                Describe(rejectedRelease));

            host.Compose();
            Assert(result, "adr013a", "release-retry", director.Refresh(), FrameworkBgmOperationOutcome.Released, null, true);

            var unavailableRoot = new GameObject("QA_Synthetic_Bgm_NoAuthority");
            unavailableRoot.SetActive(false);
            var unavailableDirector = unavailableRoot.AddComponent<FrameworkBgmDirector>();
            FrameworkBgmOperationResult unavailable = unavailableDirector.SetRouteBgm(routeCue);
            Check(
                result,
                "adr013a",
                "optional-authority-unavailable",
                unavailable.Outcome == FrameworkBgmOperationOutcome.OptionalAuthorityUnavailable
                    && unavailableDirector.ConfirmedBgm == null
                    && !unavailableDirector.ConfirmedExplicitSilence,
                "OptionalAuthorityUnavailable; no confirmed presentation",
                Describe(unavailable));
            UnityEngine.Object.DestroyImmediate(unavailableRoot);
        }

        private static IEnumerator RunPhysicalProviderContinuity(
            SyntheticSuiteResult result,
            AudioRuntimeHost host,
            AudioBgmCueAsset firstCue,
            AudioBgmCueAsset secondCue)
        {
            AudioPlaybackResult firstPlay = host.PlayBgm(firstCue);
            if (!firstPlay.Succeeded)
            {
                result.Fail("audio-continuity", "provider-baseline", "Succeeded", firstPlay.Status.ToString(), "Could not establish provider baseline.");
                yield break;
            }

            yield return new WaitForSecondsRealtime(TransitionSettleSeconds);

            AudioSource source = RequireProviderSource(result, host, "provider-source");
            if (source == null)
            {
                yield break;
            }

            int beforeSameCueSamples = source.timeSamples;
            float beforeSameCueVolume = source.volume;
            AudioPlaybackResult sameCue = host.PlayBgm(firstCue);
            int afterSameCueSamples = source.timeSamples;

            Check(
                result,
                "audio-continuity",
                "same-cue-no-restart",
                sameCue.Succeeded
                    && source.isPlaying
                    && beforeSameCueSamples == afterSameCueSamples
                    && Mathf.Approximately(source.volume, beforeSameCueVolume),
                "Succeeded; source keeps position and volume synchronously",
                $"status={sameCue.Status}; beforeSamples={beforeSameCueSamples}; afterSamples={afterSameCueSamples}; beforeVolume={beforeSameCueVolume:F4}; afterVolume={source.volume:F4}");

            float beforeTransitionVolume = source.volume;
            int beforeTransitionSamples = source.timeSamples;
            AudioPlaybackResult transition = host.PlayBgm(secondCue);
            int immediatelyAfterTransitionSamples = source.timeSamples;

            Check(
                result,
                "audio-continuity",
                "different-cue-no-abrupt-cut",
                transition.Succeeded
                    && source.isPlaying
                    && source.volume > 0f
                    && beforeTransitionVolume > 0f
                    && beforeTransitionSamples == immediatelyAfterTransitionSamples,
                "Succeeded; old source remains playing immediately while fade-out starts",
                $"status={transition.Status}; playing={source.isPlaying}; beforeVolume={beforeTransitionVolume:F4}; immediateVolume={source.volume:F4}; beforeSamples={beforeTransitionSamples}; immediateSamples={immediatelyAfterTransitionSamples}");

            yield return new WaitForSecondsRealtime(TransitionSettleSeconds);

            AudioBgmService concreteService = host.BgmService as AudioBgmService;
            Check(
                result,
                "audio-continuity",
                "different-cue-transition-completes",
                concreteService != null
                    && ReferenceEquals(concreteService.ActiveCue, secondCue)
                    && source.isPlaying
                    && source.volume > 0f,
                "ActiveCue=second; source playing above zero volume",
                $"active={NameOf(concreteService != null ? concreteService.ActiveCue : null)}; playing={source.isPlaying}; volume={source.volume:F4}");

            float beforeStopVolume = source.volume;
            AudioPlaybackResult stop = host.StopBgm();
            bool continuedDuringFade = stop.Status == AudioPlaybackStatus.Stopped
                && source.isPlaying
                && beforeStopVolume > 0f
                && source.volume > 0f;

            float stopDeadline = Time.realtimeSinceStartup + ProviderStopTimeoutSeconds;
            while (source != null && source.isPlaying && Time.realtimeSinceStartup < stopDeadline)
            {
                yield return null;
            }

            concreteService = host.BgmService as AudioBgmService;
            Check(
                result,
                "audio-continuity",
                "explicit-stop-fades-to-silence",
                continuedDuringFade && source != null && !source.isPlaying && concreteService != null && concreteService.ActiveCue == null,
                "Stop accepted; source keeps playing during fade then reaches silence",
                $"status={stop.Status}; continuedDuringFade={continuedDuringFade}; finalPlaying={(source != null && source.isPlaying)}; active={NameOf(concreteService != null ? concreteService.ActiveCue : null)}");
        }

        private static IEnumerator WaitForProviderStop(AudioRuntimeHost host)
        {
            Component service = host != null ? host.BgmService as Component : null;
            AudioSource source = service != null ? service.GetComponent<AudioSource>() : null;
            if (source == null)
            {
                yield break;
            }

            float deadline = Time.realtimeSinceStartup + ProviderStopTimeoutSeconds;
            while (source.isPlaying && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static AudioSource RequireProviderSource(SyntheticSuiteResult result, AudioRuntimeHost host, string caseName)
        {
            Component service = host != null ? host.BgmService as Component : null;
            AudioSource source = service != null ? service.GetComponent<AudioSource>() : null;
            if (source == null)
            {
                result.Fail("audio-continuity", caseName, "provider source available", "source missing", "AudioRuntimeHost did not expose the real BGM service source.");
            }

            return source;
        }

        private static void Assert(
            SyntheticSuiteResult result,
            string group,
            string name,
            FrameworkBgmOperationResult operation,
            FrameworkBgmOperationOutcome outcome,
            AudioBgmCueAsset confirmed,
            bool confirmedSilence)
        {
            Check(
                result,
                group,
                name,
                operation.Outcome == outcome
                    && operation.ConfirmedCue == confirmed
                    && operation.ConfirmedExplicitSilence == confirmedSilence,
                $"outcome={outcome}; confirmed={NameOf(confirmed)}; confirmedSilence={confirmedSilence}",
                Describe(operation));
        }

        private static void Check(SyntheticSuiteResult result, string group, string name, bool passed, string expected, string actual)
        {
            if (passed)
            {
                result.Pass(group, name, expected, actual);
            }
            else
            {
                result.Fail(group, name, expected, actual, "Synthetic assertion failed.");
            }
        }

        private static string Describe(FrameworkBgmOperationResult result)
        {
            return $"outcome={result.Outcome}; requested={NameOf(result.RequestedCue)}; requestedSilence={result.RequestedExplicitSilence}; confirmed={NameOf(result.ConfirmedCue)}; confirmedSilence={result.ConfirmedExplicitSilence}; reason={result.Reason}";
        }

        private static string NameOf(AudioBgmCueAsset cue)
        {
            return cue != null ? cue.name : "<null>";
        }

        internal sealed class SyntheticSuiteResult
        {
            internal int Passed { get; private set; }
            internal int Failed { get; private set; }
            internal int FrameworkBgmPassed { get; private set; }
            internal int FrameworkBgmFailed { get; private set; }
            internal int Adr013aPassed { get; private set; }
            internal int Adr013aFailed { get; private set; }
            internal int AudioContinuityPassed { get; private set; }
            internal int AudioContinuityFailed { get; private set; }

            internal int FrameworkBgmTotal => FrameworkBgmPassed + FrameworkBgmFailed;
            internal int Adr013aTotal => Adr013aPassed + Adr013aFailed;
            internal int AudioContinuityTotal => AudioContinuityPassed + AudioContinuityFailed;

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
                    return;
                }

                if (group == "audio-continuity")
                {
                    if (passed) AudioContinuityPassed++; else AudioContinuityFailed++;
                }
            }
        }
    }
}

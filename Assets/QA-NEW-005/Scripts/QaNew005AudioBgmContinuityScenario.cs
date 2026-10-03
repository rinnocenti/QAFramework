using System.Collections;
using System.Collections.Generic;
using Immersive.Audio.Authoring;
using Immersive.Framework.Audio;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.New005
{
    [DisallowMultipleComponent]
    public sealed class QaNew005AudioBgmContinuityScenario : MonoBehaviour
    {
        private const string ScenarioId = "QA-NEW-005";

        [Header("Session authority and evidence")]
        [SerializeField] private GameApplicationAsset gameApplication;
        [SerializeField] private FrameworkBgmDirector director;
        [SerializeField] private QaNew005AudioEvidence evidence;
        [SerializeField] private AudioBgmCueAsset routeCue;
        [SerializeField] private AudioBgmCueAsset activityCue;
        [SerializeField] private AudioBgmCueAsset startupActivityCue;
        [SerializeField] private AudioBgmCueAsset contentlessStartupRouteCue;

        [Header("Route requests")]
        [SerializeField] private RouteAsset neutralRoute;
        [SerializeField] private RouteAsset routePlay;
        [SerializeField] private RouteAsset routePreserve;
        [SerializeField] private RouteAsset routeSilence;
        [SerializeField] private RouteAsset routeAfterSilence;
        [SerializeField] private RouteAsset routeStartupActivityCue;
        [SerializeField] private RouteAsset routeStartupActivityEmpty;
        [SerializeField] private RouteRequestTrigger requestNeutralRoute;
        [SerializeField] private RouteRequestTrigger requestRoutePlay;
        [SerializeField] private RouteRequestTrigger requestRoutePreserve;
        [SerializeField] private RouteRequestTrigger requestRouteSilence;
        [SerializeField] private RouteRequestTrigger requestRouteAfterSilence;
        [SerializeField] private RouteRequestTrigger requestRouteStartupCue;
        [SerializeField] private RouteRequestTrigger requestRouteStartupEmpty;

        [Header("Activity requests")]
        [SerializeField] private ActivityAsset activityOwn;
        [SerializeField] private ActivityAsset activityNeutral;
        [SerializeField] private ActivityAsset activityUseRoute;
        [SerializeField] private ActivityRequestTrigger requestActivityOwn;
        [SerializeField] private ActivityRequestTrigger requestActivityNeutral;
        [SerializeField] private ActivityRequestTrigger requestActivityUseRoute;

        [Header("Deadlines")]
        [SerializeField, Min(0.1f)] private float transitionTimeoutSeconds = 20f;

        private int _completed;
        private bool _started;
        private bool _terminal;
        private bool _waitSucceeded;
        private string _firstDivergence = string.Empty;
        private string _cleanupIssue = string.Empty;

        private void Start()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (!_started || _terminal) return;
            _cleanupIssue = "Scenario was disabled before cleanup could be confirmed; enter a fresh Play Mode session.";
            Fail(_cleanupIssue);
            PublishTerminal(false);
        }

        private IEnumerator Run()
        {
            if (evidence != null) evidence.ResetEvidence();
            if (!ValidateAuthoring(out string issue))
            {
                Fail(issue);
                yield return Cleanup();
                yield break;
            }

            yield return WaitFor(() => IsLoaded(neutralRoute), transitionTimeoutSeconds);
            if (!_waitSucceeded || director.ConfirmedBgm != null || director.ConfirmedExplicitSilence)
            {
                Fail("Neutral startup baseline was not loaded with a provider-confirmed neutral presentation.");
                yield return Cleanup();
                yield break;
            }
            yield return RequestRoute(requestRoutePlay, routePlay, "Route PlayOwn", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!evidence.RouteContentEntered)
            { Fail("Route content lifecycle was not dispatched; RouteBgmAuthoring did not enter its contribution."); yield return Cleanup(); yield break; }
            if (!evidence.RouteDirectorAttached)
            { Fail("Route consumer was not bound to the Session director before content entry."); yield return Cleanup(); yield break; }
            if (!evidence.RouteLastOperationObserved || evidence.RouteLastOperationOutcome != FrameworkBgmOperationOutcome.Applied)
            { Fail("Route content entered, but its BGM operation was not observed as Applied. " + evidence.LastRouteOperationResult); yield return Cleanup(); yield break; }
            if (!evidence.RouteProviderConfirmed || !IsConfirmed(routeCue, false))
            { Fail("Route BGM operation was Applied, but the provider did not confirm the expected cue."); yield return Cleanup(); yield break; }
            PassCase();

            int routeDetachedBefore = evidence.RouteContentDetachedCount;
            yield return RequestRoute(requestRoutePreserve, routePreserve, "Route A to no-request Route", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            yield return WaitFor(() => evidence.RouteContentDetachedCount > routeDetachedBefore, transitionTimeoutSeconds);
            if (!IsConfirmed(routeCue, false) || evidence.RouteContentDetachedCount <= routeDetachedBefore)
            { Fail("Route release did not detach its authoring or no-request Route changed the confirmed presentation."); yield return Cleanup(); yield break; }

            int routeEnteredBefore = evidence.RouteContentEnteredCount;
            yield return RequestRoute(requestRoutePlay, routePlay, "Route reentry", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!IsConfirmed(routeCue, false) || !evidence.RouteDirectorAttached || evidence.RouteContentEnteredCount <= routeEnteredBefore)
            { Fail("Route A reentry did not produce a newly bound authoring with no stale consumer state."); yield return Cleanup(); yield break; }
            PassCase();

            yield return RequestActivity(requestActivityOwn, activityOwn, "Activity own cue", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!evidence.ActivityContentEntered)
            { Fail("Activity content lifecycle was not dispatched; ActivityBgmAuthoring did not enter its contribution."); yield return Cleanup(); yield break; }
            if (!evidence.ActivityDirectorAttached)
            { Fail("Activity consumer was not bound to the Session director before content entry."); yield return Cleanup(); yield break; }
            if (!evidence.ActivityLastOperationObserved || evidence.ActivityLastOperationOutcome != FrameworkBgmOperationOutcome.Applied)
            { Fail("Activity content entered, but its BGM operation was not observed as Applied. " + evidence.LastActivityOperationResult); yield return Cleanup(); yield break; }
            if (!evidence.ActivityProviderConfirmed || !IsConfirmed(activityCue, false))
            { Fail("Activity BGM operation was Applied, but the provider did not confirm the expected cue."); yield return Cleanup(); yield break; }
            PassCase();

            int activityDetachedBefore = evidence.ActivityContentDetachedCount;
            yield return RequestActivity(requestActivityNeutral, activityNeutral, "Activity release to neutral Activity", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            yield return WaitFor(() => evidence.ActivityContentDetachedCount > activityDetachedBefore, transitionTimeoutSeconds);
            if (!IsConfirmed(activityCue, false) || evidence.ActivityContentDetachedCount <= activityDetachedBefore)
            { Fail("Activity release did not detach the old authoring or implicitly stopped its confirmed cue."); yield return Cleanup(); yield break; }
            PassCase();
            PassCase(); // Neutral Activity entry independently proves its No Request policy preserves the cue.

            yield return RequestActivity(requestActivityUseRoute, activityUseRoute, "Activity UseRoute", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!IsConfirmed(routeCue, false) || !evidence.ActivityDirectorAttached)
            { Fail("UseRoute did not resolve the current Route PlayOwn intent."); yield return Cleanup(); yield break; }
            PassCase();

            yield return RequestRoute(requestRouteSilence, routeSilence, "Explicit Route silence", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!IsConfirmed(null, true))
            { Fail("Explicit Silence was not provider-confirmed."); yield return Cleanup(); yield break; }
            PassCase();

            yield return RequestRoute(requestRoutePreserve, routePreserve, "No request after silence", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!IsConfirmed(null, true))
            { Fail("Explicit silence did not remain sticky after owner exit and a no-request Route."); yield return Cleanup(); yield break; }
            PassCase();

            yield return RequestRoute(requestRouteAfterSilence, routeAfterSilence, "Play after silence", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!IsConfirmed(routeCue, false))
            { Fail("A later Play did not replace confirmed explicit silence."); yield return Cleanup(); yield break; }
            PassCase();

            yield return RequestRoute(requestRouteStartupCue, routeStartupActivityCue, "Startup Activity with own cue", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!IsConfirmed(startupActivityCue, false) ||
                !ReferenceEquals(director.CurrentRouteBgm, routeCue) ||
                !ReferenceEquals(director.CurrentEffectiveBgm, startupActivityCue) ||
                !evidence.ActivityDirectorAttached ||
                director.LastOperationResult.Outcome != FrameworkBgmOperationOutcome.Applied)
            { Fail("Startup Activity intent/final state was inconsistent with the pending Route intent and confirmed Activity cue."); yield return Cleanup(); yield break; }
            PassCase();

            yield return RequestRoute(requestRouteStartupEmpty, routeStartupActivityEmpty, "Contentless Startup Activity", transitionTimeoutSeconds);
            if (!ContinueIfSuccessful()) { yield return Cleanup(); yield break; }
            if (!IsConfirmed(contentlessStartupRouteCue, false) ||
                !ReferenceEquals(director.CurrentRouteBgm, contentlessStartupRouteCue))
            { Fail("Contentless Startup Activity completion did not resolve and confirm the pending Route cue."); yield return Cleanup(); yield break; }
            PassCase();

            yield return Cleanup();
        }

        private IEnumerator Cleanup()
        {
            if (HasRequestInFlight())
            {
                yield return WaitFor(() => !HasRequestInFlight(), transitionTimeoutSeconds);
                if (!_waitSucceeded)
                {
                    SetCleanupIssue("An operation remained in flight at cleanup deadline.");
                    Fail("Cleanup could not complete because an operation remained in flight.");
                    PublishTerminal(false);
                    yield break;
                }
            }

            if (requestRouteSilence != null && requestRouteSilence.HasRouteRuntimeBinding)
            {
                requestRouteSilence.RequestRoute();
                yield return WaitFor(() => !requestRouteSilence.IsRequestInFlight &&
                    requestRouteSilence.LastEventPhase == FlowRequestEventPhase.Completed, transitionTimeoutSeconds);
                if (!_waitSucceeded || !requestRouteSilence.LastRequestSucceeded)
                    SetCleanupIssue("Could not apply explicit Silence during cleanup.");
            }

            if (requestNeutralRoute != null && requestNeutralRoute.HasRouteRuntimeBinding)
            {
                requestNeutralRoute.RequestRoute();
                yield return WaitFor(() => !requestNeutralRoute.IsRequestInFlight &&
                    requestNeutralRoute.LastEventPhase == FlowRequestEventPhase.Completed, transitionTimeoutSeconds);
                if (!_waitSucceeded || !requestNeutralRoute.LastRequestSucceeded)
                    SetCleanupIssue("Could not restore the neutral startup Route.");
            }

            yield return WaitFor(() => IsNeutralRouteCurrent() && IsLoaded(neutralRoute) &&
                GetLoadedTransientScenes().Count == 0, transitionTimeoutSeconds);
            if (!_waitSucceeded)
            {
                string unexpectedScenes = FormatSceneList(GetLoadedTransientScenes());
                string currentRouteIssue = IsNeutralRouteCurrent()
                    ? string.Empty
                    : "Route Neutral was not confirmed as the current Route after cleanup. ";
                SetCleanupIssue(currentRouteIssue + "Unexpected Route/Activity scenes remain loaded: " + unexpectedScenes + ".");
            }
            if (evidence != null && (evidence.RouteConsumersBound != 0 || evidence.ActivityConsumersBound != 0 ||
                                     evidence.RouteConsumersDestroyedWhileBound != 0 || evidence.ActivityConsumersDestroyedWhileBound != 0))
                SetCleanupIssue($"Temporary BGM consumers were not fully detached. routeBound='{evidence.RouteConsumersBound}' activityBound='{evidence.ActivityConsumersBound}' routeDestroyedBound='{evidence.RouteConsumersDestroyedWhileBound}' activityDestroyedBound='{evidence.ActivityConsumersDestroyedWhileBound}'.");
            if (!IsConfirmed(null, true))
                SetCleanupIssue("Cleanup did not leave provider-confirmed explicit silence as the neutral presentation.");

            if (string.IsNullOrEmpty(_cleanupIssue))
            {
                PassCase();
                PublishTerminal(true);
            }
            else
            {
                Fail("Cleanup failed: " + _cleanupIssue);
                PublishTerminal(false);
            }
        }

        private IEnumerator RequestRoute(RouteRequestTrigger trigger, RouteAsset route, string phase, float timeout)
        {
            if (trigger == null || route == null || trigger.TargetRoute != route || !trigger.HasRouteRuntimeBinding)
            { Fail($"{phase}: Route trigger missing, mismatched, or unbound."); yield break; }
            trigger.RequestRoute();
            yield return WaitFor(() => !trigger.IsRequestInFlight && trigger.LastEventPhase == FlowRequestEventPhase.Completed, timeout);
            if (!_waitSucceeded || !trigger.LastRequestSucceeded)
                Fail($"{phase}: request failed; outcome='{trigger.LastOutcome}' message='{trigger.LastMessage}'.");
        }

        private IEnumerator RequestActivity(ActivityRequestTrigger trigger, ActivityAsset activity, string phase, float timeout)
        {
            if (trigger == null || activity == null || trigger.TargetActivity != activity || !trigger.HasActivityRuntimeBinding)
            { Fail($"{phase}: Activity trigger missing, mismatched, or unbound."); yield break; }
            trigger.RequestActivity();
            yield return WaitFor(() => !trigger.IsRequestInFlight && trigger.LastEventPhase == FlowRequestEventPhase.Completed, timeout);
            if (!_waitSucceeded || !trigger.LastRequestSucceeded)
                Fail($"{phase}: request failed; outcome='{trigger.LastOutcome}' message='{trigger.LastMessage}'.");
        }

        private IEnumerator WaitFor(System.Func<bool> predicate, float timeout)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeout);
            while (Time.realtimeSinceStartup < deadline)
            {
                if (predicate()) { _waitSucceeded = true; yield break; }
                yield return null;
            }
            _waitSucceeded = predicate();
        }

        private bool ValidateAuthoring(out string issue)
        {
            issue = string.Empty;
            if (routeCue == null)
            { issue = "fixtureInvalid='Route PlayOwn BGM cue is missing'"; return false; }
            if (activityCue == null)
            { issue = "fixtureInvalid='Activity Own BGM cue is missing'"; return false; }
            if (startupActivityCue == null)
            { issue = "fixtureInvalid='Startup Activity BGM cue is missing'"; return false; }
            if (contentlessStartupRouteCue == null)
            { issue = "fixtureInvalid='Contentless Startup Route BGM cue is missing'"; return false; }
            if (gameApplication == null || director == null || evidence == null)
            { issue = "Game Application, Session Director, and evidence are required."; return false; }
            if (neutralRoute == null || routePlay == null || routePreserve == null || routeSilence == null ||
                routeAfterSilence == null || routeStartupActivityCue == null || routeStartupActivityEmpty == null ||
                activityOwn == null || activityNeutral == null || activityUseRoute == null)
            { issue = "fixtureInvalid='A required Route or Activity reference is missing'"; return false; }
            if (gameApplication.StartupRoute != neutralRoute)
            { issue = "GameApplication startup Route must be the neutral QA-NEW-005 Route."; return false; }
            if (director.ConfirmedBgm != null || director.ConfirmedExplicitSilence)
            { issue = "Session BGM baseline is not neutral before scenario execution."; return false; }
            if (requestNeutralRoute == null || requestRoutePlay == null || requestRoutePreserve == null ||
                requestRouteSilence == null || requestRouteAfterSilence == null ||
                requestRouteStartupCue == null || requestRouteStartupEmpty == null ||
                requestActivityOwn == null || requestActivityNeutral == null || requestActivityUseRoute == null)
            { issue = "All explicit Route and Activity request triggers are required."; return false; }
            issue = string.Empty;
            return true;
        }

        private bool IsConfirmed(AudioBgmCueAsset cue, bool silence) =>
            director != null && ReferenceEquals(director.ConfirmedBgm, cue) && director.ConfirmedExplicitSilence == silence;

        private bool HasRequestInFlight() =>
            (requestNeutralRoute != null && requestNeutralRoute.IsRequestInFlight) ||
            (requestRoutePlay != null && requestRoutePlay.IsRequestInFlight) ||
            (requestRoutePreserve != null && requestRoutePreserve.IsRequestInFlight) ||
            (requestRouteSilence != null && requestRouteSilence.IsRequestInFlight) ||
            (requestRouteAfterSilence != null && requestRouteAfterSilence.IsRequestInFlight) ||
            (requestRouteStartupCue != null && requestRouteStartupCue.IsRequestInFlight) ||
            (requestRouteStartupEmpty != null && requestRouteStartupEmpty.IsRequestInFlight) ||
            (requestActivityOwn != null && requestActivityOwn.IsRequestInFlight) ||
            (requestActivityNeutral != null && requestActivityNeutral.IsRequestInFlight) ||
            (requestActivityUseRoute != null && requestActivityUseRoute.IsRequestInFlight);

        private List<string> GetLoadedTransientScenes()
        {
            var loaded = new List<string>();
            AddLoadedRoute(loaded, neutralRoute, IsNeutralRouteCurrent());
            AddLoadedRoute(loaded, routePlay, false);
            AddLoadedRoute(loaded, routePreserve, false);
            AddLoadedRoute(loaded, routeSilence, false);
            AddLoadedRoute(loaded, routeAfterSilence, false);
            AddLoadedRoute(loaded, routeStartupActivityCue, false);
            AddLoadedRoute(loaded, routeStartupActivityEmpty, false);
            AddLoadedActivityScenes(loaded, activityOwn);
            AddLoadedActivityScenes(loaded, activityNeutral);
            AddLoadedActivityScenes(loaded, activityUseRoute);
            AddLoadedActivityScenes(loaded, routeStartupActivityCue != null ? routeStartupActivityCue.StartupActivity : null);
            return loaded;
        }

        private static void AddLoadedRoute(List<string> loaded, RouteAsset route, bool permittedCurrentBaseline)
        {
            if (route != null && !permittedCurrentBaseline && IsLoaded(route) && !loaded.Contains(route.PrimaryScenePath))
                loaded.Add(route.PrimaryScenePath);
        }

        private bool IsNeutralRouteCurrent() => requestNeutralRoute != null && neutralRoute != null &&
            ReferenceEquals(requestNeutralRoute.TargetRoute, neutralRoute) &&
            !requestNeutralRoute.IsRequestInFlight &&
            requestNeutralRoute.LastEventPhase == FlowRequestEventPhase.Completed &&
            requestNeutralRoute.LastRequestSucceeded;

        private static void AddLoadedActivityScenes(List<string> loaded, ActivityAsset activity)
        {
            if (activity == null || !activity.HasActivityContentProfile || !activity.ActivityContentProfile.HasScenes)
                return;
            for (int i = 0; i < activity.ActivityContentProfile.Scenes.Count; i++)
            {
                string path = activity.ActivityContentProfile.Scenes[i].ScenePath;
                Scene scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid() && scene.isLoaded && !loaded.Contains(path)) loaded.Add(path);
            }
        }

        private static string FormatSceneList(IReadOnlyList<string> scenes) =>
            scenes == null || scenes.Count == 0 ? "<none>" : string.Join(",", scenes);

        private static bool IsActivityLoaded(ActivityAsset activity)
        {
            if (activity == null || !activity.HasActivityContentProfile || !activity.ActivityContentProfile.HasScenes)
                return false;
            for (int i = 0; i < activity.ActivityContentProfile.Scenes.Count; i++)
            {
                string path = activity.ActivityContentProfile.Scenes[i].ScenePath;
                Scene scene = SceneManager.GetSceneByPath(path);
                if (scene.IsValid() && scene.isLoaded) return true;
            }
            return false;
        }

        private static bool IsLoaded(RouteAsset route)
        {
            if (route == null || string.IsNullOrWhiteSpace(route.PrimaryScenePath)) return false;
            Scene scene = SceneManager.GetSceneByPath(route.PrimaryScenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        private bool ContinueIfSuccessful() => string.IsNullOrEmpty(_firstDivergence);
        private void PassCase() => _completed++;
        private void Fail(string issue) { if (string.IsNullOrEmpty(_firstDivergence)) _firstDivergence = issue; }
        private void SetCleanupIssue(string issue) { if (string.IsNullOrEmpty(_cleanupIssue)) _cleanupIssue = issue; }

        private void PublishTerminal(bool baselineRestored)
        {
            if (_terminal) return;
            _terminal = true;
            bool passed = baselineRestored && string.IsNullOrEmpty(_firstDivergence);
            string verdict = passed ? "PASS" : "FAIL";
            string status = verdict == "PASS" ? "Passed" : "Failed";
            string message = $"[{ScenarioId}] status='{status}' verdict='{verdict}' cases='{_completed}/12' " +
                $"confirmedBgm='{(director != null && director.ConfirmedBgm != null ? director.ConfirmedBgm.CueIdValue : "<null>")}' " +
                $"explicitSilence='{(director != null && director.ConfirmedExplicitSilence).ToString().ToLowerInvariant()}' " +
                $"neutralRouteCurrent='{IsNeutralRouteCurrent().ToString().ToLowerInvariant()}' " +
                $"neutralRouteBaselineLoaded='{(neutralRoute != null && IsLoaded(neutralRoute)).ToString().ToLowerInvariant()}' " +
                $"routeDirectorAttached='{(evidence != null && evidence.RouteDirectorAttached).ToString().ToLowerInvariant()}' " +
                $"routeContentEntered='{(evidence != null && evidence.RouteContentEntered).ToString().ToLowerInvariant()}' " +
                $"routeContentExited='{(evidence != null && evidence.RouteContentExited).ToString().ToLowerInvariant()}' " +
                $"activityDirectorAttached='{(evidence != null && evidence.ActivityDirectorAttached).ToString().ToLowerInvariant()}' " +
                $"activityContentEntered='{(evidence != null && evidence.ActivityContentEntered).ToString().ToLowerInvariant()}' " +
                $"activityContentExited='{(evidence != null && evidence.ActivityContentExited).ToString().ToLowerInvariant()}' " +
                $"routeLastOperationResult='{Sanitize(evidence != null ? evidence.LastRouteOperationResult : "<missing>")}' " +
                $"activityLastOperationResult='{Sanitize(evidence != null ? evidence.LastActivityOperationResult : "<missing>")}' " +
                $"directorLastOperationResult='{Sanitize(FormatDirectorOperation())}' " +
                $"routeProviderConfirmed='{(evidence != null && evidence.RouteProviderConfirmed).ToString().ToLowerInvariant()}' " +
                $"activityProviderConfirmed='{(evidence != null && evidence.ActivityProviderConfirmed).ToString().ToLowerInvariant()}' " +
                $"loadedTemporaryScenes='{Sanitize(FormatSceneList(GetLoadedTransientScenes()))}' " +
                $"routeConsumersBound='{(evidence != null ? evidence.RouteConsumersBound : -1)}' " +
                $"activityConsumersBound='{(evidence != null ? evidence.ActivityConsumersBound : -1)}' " +
                $"consumersDestroyedWhileBound='{(evidence != null ? evidence.RouteConsumersDestroyedWhileBound + evidence.ActivityConsumersDestroyedWhileBound : -1)}' " +
                $"cleanup='{(baselineRestored ? "BaselineRestored" : "Failed")}' " +
                $"firstDivergence='{Sanitize(_firstDivergence)}' cleanupIssue='{Sanitize(_cleanupIssue)}'";
            if (passed) Debug.Log(message, this); else Debug.LogError(message, this);
        }

        private string FormatDirectorOperation()
        {
            if (director == null) return "<missing>";
            FrameworkBgmOperationResult result = director.LastOperationResult;
            string requestedCue = result.RequestedCue != null ? result.RequestedCue.CueIdValue : "<null>";
            string confirmedCue = result.ConfirmedCue != null ? result.ConfirmedCue.CueIdValue : "<null>";
            return $"operation='{result.Operation}' outcome='{result.Outcome}' requestedCue='{requestedCue}' confirmedCue='{confirmedCue}' explicitSilence='{result.ConfirmedExplicitSilence.ToString().ToLowerInvariant()}' reason='{result.Reason}'";
        }

        private static string Sanitize(string value) => (value ?? string.Empty)
            .Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n");
    }
}

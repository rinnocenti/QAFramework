using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.SceneLifecycle;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// ADR-006 focal behavioral closure for transaction boundary ownership.
    ///
    /// Directly covers:
    /// - QA-01: a failing Transition Before is pre-commit and preserves previous authority.
    /// - QA-02: a failing Transition After is post-commit and preserves destination authority.
    /// - QA-08: the focal transaction cases repeat twice in one Play Mode session and
    ///   terminate without pure Transition Gate, readiness composite, in-flight, runtime
    ///   content or temporary scene residue.
    ///
    /// QA-03 remains owned by QaRouteActivityIdentityRegression's canonical
    /// legitimate-supersession-preservation case and is intentionally not duplicated here.
    /// </summary>
    public static class QaAdr006TransactionBehavioralClosureRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/" +
            "Run ADR-006 Transaction Behavioral Closure Regression";
        private const string Prefix = "[ADR006_TRANSACTION_BEHAVIORAL_CLOSURE]";
        private const string Source = nameof(QaAdr006TransactionBehavioralClosureRegression);
        private const int RequiredPassCount = 2;
        private const string IsolationScenePath =
            "Assets/ImmersiveFrameworkQA/GameFlow/Scenes/QA_IF_READY_04_DirectPoliciesContent.unity";

        private static bool _running;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying && !_running;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            if (_running)
            {
                Debug.LogError($"{Prefix} status='Rejected' reason='already-running'.");
                return;
            }

            _running = true;
            var completed = new List<string>();
            Exception failure = null;
            FrameworkRuntimeHost officialHost = null;
            RouteAsset officialRoute = null;
            ActivityAsset officialActivity = null;
            QaAdr006TemporarySceneScope sceneScope = null;

            try
            {
                Require(EditorApplication.isPlaying,
                    "ADR-006 Transaction Behavioral Closure requires Play Mode.");
                completed.Add("play-mode-required");

                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out officialHost,
                        out string hostDiagnostic),
                    hostDiagnostic);
                Require(officialHost.State.GameFlowStarted &&
                    officialHost.State.CurrentRoute != null,
                    "ADR-006 Transaction Behavioral Closure requires a started official host with Route authority.");
                RequireOfficialHostIdle(officialHost, "before-isolation");
                officialRoute = officialHost.State.CurrentRoute;
                officialActivity = officialHost.State.CurrentActivity;
                completed.Add("official-authority-captured");

                sceneScope = await QaAdr006TemporarySceneScope.CreateAsync(
                    IsolationScenePath);
                completed.Add("isolation-scene-loaded");

                for (int pass = 1; pass <= RequiredPassCount; pass++)
                {
                    await RunPassAsync(
                        pass,
                        sceneScope.ScenePath,
                        sceneScope.SceneName,
                        completed);
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (sceneScope != null)
                {
                    try
                    {
                        await sceneScope.DisposeAsync();
                        completed.Add("isolation-scene-cleaned");
                    }
                    catch (Exception exception)
                    {
                        failure = CombineFailures(failure, exception);
                    }
                }

                if (officialHost != null)
                {
                    try
                    {
                        RequireOfficialAuthorityPreserved(
                            officialHost,
                            officialRoute,
                            officialActivity);
                        RequireOfficialHostIdle(officialHost, "after-isolation");
                        completed.Add("official-authority-preserved");
                    }
                    catch (Exception exception)
                    {
                        failure = CombineFailures(failure, exception);
                    }
                }

                _running = false;
            }

            if (failure == null)
            {
                try
                {
                    Require(completed.Contains("pass-1-before-precommit-preserved") &&
                            completed.Contains("pass-1-after-committed-reveal-preserved") &&
                            completed.Contains("pass-1-terminal-clean") &&
                            completed.Contains("pass-2-before-precommit-preserved") &&
                            completed.Contains("pass-2-after-committed-reveal-preserved") &&
                            completed.Contains("pass-2-terminal-clean") &&
                            completed.Contains("isolation-scene-cleaned") &&
                            completed.Contains("official-authority-preserved"),
                        "ADR-006 closure did not complete every required pass and cleanup case.");
                }
                catch (Exception exception)
                {
                    failure = exception;
                }
            }

            if (failure != null)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' passes='incomplete' " +
                    $"completed='{string.Join(",", completed)}' " +
                    $"exception='{failure.GetType().Name}' " +
                    $"message='{Escape(failure.Message)}'.");
                throw failure;
            }

            Debug.Log(
                $"{Prefix} status='Passed' passes='{RequiredPassCount}/{RequiredPassCount}' " +
                $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
        }

        private static async Task RunPassAsync(
            int pass,
            string scenePath,
            string sceneName,
            List<string> completed)
        {
            QaAdr006Harness harness = null;
            try
            {
                harness = QaAdr006Harness.Create(
                    pass,
                    scenePath,
                    sceneName);
                completed.Add($"pass-{pass}-harness-created");

                FrameworkRouteRequestResult establish =
                    await harness.EstablishInitialAuthorityAsync();
                Require(establish.Succeeded && establish.DestinationAuthoritative,
                    $"Pass {pass}: initial Route authority was not established. " +
                    $"kind='{establish.Kind}' message='{establish.Message}'.");
                Require(ReferenceEquals(
                        harness.Runtime.CurrentRoute,
                        harness.InitialRoute),
                    $"Pass {pass}: initial Route is not authoritative after establishment.");
                Require(harness.SceneLifecycle.AvailableCount == 1,
                    $"Pass {pass}: initial Route establishment did not cross exactly one real Scene Lifecycle boundary. " +
                    $"available='{harness.SceneLifecycle.AvailableCount}'.");
                harness.RequireAuthorityAndLifecycleClean(
                    harness.InitialRoute,
                    "initial-establishment");
                completed.Add($"pass-{pass}-initial-authority-established");

                await ProveBeforeFailureAsync(harness, pass);
                completed.Add($"pass-{pass}-before-precommit-preserved");

                await ProveAfterFailureAsync(harness, pass);
                completed.Add($"pass-{pass}-after-committed-reveal-preserved");

                harness.RequireAuthorityAndLifecycleClean(
                    harness.AfterFailureTarget,
                    "pass-terminal");
                completed.Add($"pass-{pass}-terminal-clean");
            }
            finally
            {
                harness?.Dispose();
            }
        }

        private static async Task ProveBeforeFailureAsync(
            QaAdr006Harness harness,
            int pass)
        {
            int availableBefore = harness.SceneLifecycle.AvailableCount;
            int releasingBefore = harness.SceneLifecycle.ReleasingCount;
            int transitionCallsBefore = harness.Transition.RequestCount;

            harness.Transition.FailPhase = TransitionPhase.OperationOpened;
            FrameworkRouteRequestResult result = await harness.Runtime.RequestRouteAsync(
                harness.BeforeFailureTarget,
                Source,
                $"pass-{pass}-before-failure");

            Require(result.Kind == FrameworkRouteRequestKind.FailedPreCommitTransition,
                $"Pass {pass}: Before failure returned unexpected kind '{result.Kind}'.");
            Require(!result.Succeeded,
                $"Pass {pass}: Before failure was reported as Succeeded.");
            Require(!result.DestinationAuthoritative,
                $"Pass {pass}: Before failure incorrectly marked destination authoritative.");
            Require(!result.Superseded,
                $"Pass {pass}: Before failure was incorrectly classified as Superseded.");
            Require(ReferenceEquals(
                    harness.Runtime.CurrentRoute,
                    harness.InitialRoute),
                $"Pass {pass}: Before failure advanced Route authority.");

            Require(harness.SceneLifecycle.AvailableCount == availableBefore &&
                    harness.SceneLifecycle.ReleasingCount == releasingBefore,
                $"Pass {pass}: Before failure reached Scene Lifecycle before commit. " +
                $"availableBefore='{availableBefore}' availableAfter='{harness.SceneLifecycle.AvailableCount}' " +
                $"releasingBefore='{releasingBefore}' releasingAfter='{harness.SceneLifecycle.ReleasingCount}'.");
            Require(harness.Transition.RequestCount == transitionCallsBefore + 1,
                $"Pass {pass}: Before failure must stop after one injected Before phase. " +
                $"before='{transitionCallsBefore}' after='{harness.Transition.RequestCount}'.");
            Require(harness.Transition.LastRequest.Phase ==
                    TransitionPhase.OperationOpened,
                $"Pass {pass}: injected Before failure did not occur on OperationOpened.");
            Require(harness.Transition.LastResult.Failed &&
                    !GameFlowRuntime.IsAcceptedTransitionPhase(
                        harness.Transition.LastResult),
                $"Pass {pass}: failed Before phase was accepted by GameFlow.");

            Require(result.TransitionDiagnostics.HasBefore &&
                    !result.TransitionDiagnostics.HasAfter &&
                    result.TransitionDiagnostics.BeforeResult.Failed &&
                    result.TransitionDiagnostics.BeforeResult.ObservedStepCount == 1 &&
                    result.TransitionDiagnostics.BeforeResult.ObservedSteps[0].Phase ==
                        TransitionPhase.OperationOpened,
                $"Pass {pass}: Before terminal diagnostics did not retain the injected pre-commit failure. " +
                $"transition='{result.TransitionDiagnostics.TransitionText}'.");
            Require(ContainsOrdinalIgnoreCase(
                        result.Message,
                        "before destination commit") &&
                    ContainsOrdinalIgnoreCase(
                        result.Message,
                        "Previous Route/Activity authority is preserved"),
                $"Pass {pass}: Before terminal diagnostic does not explain the pre-commit authority boundary. " +
                $"message='{result.Message}'.");

            harness.RequireAuthorityAndLifecycleClean(
                harness.InitialRoute,
                "before-failure");
        }

        private static async Task ProveAfterFailureAsync(
            QaAdr006Harness harness,
            int pass)
        {
            int availableBefore = harness.SceneLifecycle.AvailableCount;
            int transitionCallsBefore = harness.Transition.RequestCount;

            harness.Transition.FailPhase = TransitionPhase.OperationClosed;
            FrameworkRouteRequestResult result = await harness.Runtime.RequestRouteAsync(
                harness.AfterFailureTarget,
                Source,
                $"pass-{pass}-after-failure");

            Require(result.Kind == FrameworkRouteRequestKind.FailedCommittedTargetReveal,
                $"Pass {pass}: After failure returned unexpected kind '{result.Kind}'.");
            Require(!result.Succeeded,
                $"Pass {pass}: committed reveal failure was reported as Succeeded.");
            Require(result.DestinationAuthoritative,
                $"Pass {pass}: committed reveal failure lost destination authority.");
            Require(!result.Superseded,
                $"Pass {pass}: committed reveal failure was incorrectly classified as Superseded.");
            Require(ReferenceEquals(
                    harness.Runtime.CurrentRoute,
                    harness.AfterFailureTarget),
                $"Pass {pass}: After failure did not preserve committed destination authority.");

            Require(harness.SceneLifecycle.AvailableCount == availableBefore + 1,
                $"Pass {pass}: After failure did not cross the real Route/Scene commit boundary exactly once. " +
                $"before='{availableBefore}' after='{harness.SceneLifecycle.AvailableCount}'.");
            Require(harness.Transition.RequestCount == transitionCallsBefore + 2,
                $"Pass {pass}: After failure must execute accepted Before then failing After. " +
                $"before='{transitionCallsBefore}' after='{harness.Transition.RequestCount}'.");
            Require(harness.Transition.LastRequest.Phase ==
                    TransitionPhase.OperationClosed,
                $"Pass {pass}: injected After failure did not occur on OperationClosed.");
            Require(harness.Transition.LastResult.Failed &&
                    !GameFlowRuntime.IsAcceptedTransitionPhase(
                        harness.Transition.LastResult),
                $"Pass {pass}: failed After phase was accepted by GameFlow.");

            Require(result.TransitionDiagnostics.HasBefore &&
                    result.TransitionDiagnostics.HasAfter &&
                    GameFlowRuntime.IsAcceptedTransitionPhase(
                        result.TransitionDiagnostics.BeforeResult) &&
                    result.TransitionDiagnostics.AfterResult.Failed &&
                    result.TransitionDiagnostics.AfterResult.ObservedStepCount == 1 &&
                    result.TransitionDiagnostics.AfterResult.ObservedSteps[0].Phase ==
                        TransitionPhase.OperationClosed,
                $"Pass {pass}: committed reveal diagnostics did not retain accepted Before + failing After. " +
                $"transition='{result.TransitionDiagnostics.TransitionText}'.");
            Require(ContainsOrdinalIgnoreCase(
                        result.Message,
                        "committed the destination") &&
                    ContainsOrdinalIgnoreCase(
                        result.Message,
                        "Committed destination remains authoritative") &&
                    ContainsOrdinalIgnoreCase(
                        result.Message,
                        "no blind rollback"),
                $"Pass {pass}: After terminal diagnostic does not explain the committed authority boundary. " +
                $"message='{result.Message}'.");

            harness.RequireAuthorityAndLifecycleClean(
                harness.AfterFailureTarget,
                "after-failure");
        }

        private static void RequireOfficialHostIdle(
            FrameworkRuntimeHost host,
            string stage)
        {
            Require(host != null,
                $"Official host is missing at '{stage}'.");
            Require(!host.TransitionGateSnapshot.HasBlockers &&
                    host.CurrentTransitionGateMode == TransitionGateMode.None &&
                    !host.ActivityEntryReadinessGateSnapshot.HasBlockers,
                $"Official host must be idle at '{stage}'. " +
                $"transitionMode='{host.CurrentTransitionGateMode}' " +
                $"transitionBlockers='{host.TransitionGateSnapshot.BlockerCount}' " +
                $"readinessBlockers='{host.ActivityEntryReadinessGateSnapshot.BlockerCount}'.");
        }

        private static void RequireOfficialAuthorityPreserved(
            FrameworkRuntimeHost host,
            RouteAsset expectedRoute,
            ActivityAsset expectedActivity)
        {
            Require(host.State.GameFlowStarted,
                "Official host stopped Game Flow during ADR-006 isolation.");
            Require(ReferenceEquals(host.State.CurrentRoute, expectedRoute),
                "ADR-006 isolated regression changed official host Route authority.");
            Require(ReferenceEquals(host.State.CurrentActivity, expectedActivity),
                "ADR-006 isolated regression changed official host Activity authority.");
        }

        private static bool ContainsOrdinalIgnoreCase(
            string value,
            string fragment)
        {
            return !string.IsNullOrEmpty(value) &&
                !string.IsNullOrEmpty(fragment) &&
                value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Exception CombineFailures(
            Exception current,
            Exception next)
        {
            if (current == null)
            {
                return next;
            }

            if (next == null)
            {
                return current;
            }

            return new AggregateException(current, next);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private sealed class QaAdr006Harness : IDisposable
        {
            private readonly List<UnityEngine.Object> _temporaryAssets =
                new List<UnityEngine.Object>();

            private QaAdr006Harness(
                GameFlowRuntime runtime,
                RuntimeContentRuntime runtimeContent,
                QaAdr006ControlledTransitionOrchestrator transition,
                QaAdr006SceneLifecycleProbe sceneLifecycle,
                RouteAsset initialRoute,
                RouteAsset beforeFailureTarget,
                RouteAsset afterFailureTarget)
            {
                Runtime = runtime;
                RuntimeContent = runtimeContent;
                Transition = transition;
                SceneLifecycle = sceneLifecycle;
                InitialRoute = initialRoute;
                BeforeFailureTarget = beforeFailureTarget;
                AfterFailureTarget = afterFailureTarget;

                _temporaryAssets.Add(initialRoute);
                _temporaryAssets.Add(beforeFailureTarget);
                _temporaryAssets.Add(afterFailureTarget);
            }

            internal GameFlowRuntime Runtime { get; }
            internal RuntimeContentRuntime RuntimeContent { get; }
            internal QaAdr006ControlledTransitionOrchestrator Transition { get; }
            internal QaAdr006SceneLifecycleProbe SceneLifecycle { get; }
            internal RouteAsset InitialRoute { get; }
            internal RouteAsset BeforeFailureTarget { get; }
            internal RouteAsset AfterFailureTarget { get; }

            internal static QaAdr006Harness Create(
                int pass,
                string scenePath,
                string sceneName)
            {
                var transition = new QaAdr006ControlledTransitionOrchestrator();
                var runtimeContent = new RuntimeContentRuntime();
                var sceneProbe = new QaAdr006SceneLifecycleProbe();
                var sceneLifecycle = new SceneLifecycleRuntime(sceneProbe);
                var runtime = new GameFlowRuntime(
                    runtimeContent,
                    transition,
                    new QaFakeRouteRuntimePort(),
                    new QaFakeActivityRuntimePort(),
                    new QaFakeRouteCycleResetRuntimePort(),
                    new QaFakeActivityCycleResetRuntimePort(),
                    new QaFakeActivityRestartRuntimePort(),
                    sceneLifecycle);

                RouteAsset initial = CreateRoute(
                    $"qa.adr006.txn.pass{pass}.initial",
                    $"ADR006 Pass {pass} Initial",
                    scenePath,
                    sceneName);
                RouteAsset before = CreateRoute(
                    $"qa.adr006.txn.pass{pass}.before",
                    $"ADR006 Pass {pass} Before Target",
                    scenePath,
                    sceneName);
                RouteAsset after = CreateRoute(
                    $"qa.adr006.txn.pass{pass}.after",
                    $"ADR006 Pass {pass} After Target",
                    scenePath,
                    sceneName);

                return new QaAdr006Harness(
                    runtime,
                    runtimeContent,
                    transition,
                    sceneProbe,
                    initial,
                    before,
                    after);
            }

            internal async Task<FrameworkRouteRequestResult>
                EstablishInitialAuthorityAsync()
            {
                Transition.FailPhase = TransitionPhase.Unknown;
                return await Runtime.RequestRouteAsync(
                    InitialRoute,
                    Source,
                    "establish-initial-authority");
            }

            internal void RequireAuthorityAndLifecycleClean(
                RouteAsset expectedRoute,
                string stage)
            {
                Require(expectedRoute != null && expectedRoute.HasValidRouteId,
                    $"ADR-006 terminal cleanup '{stage}' requires valid expected Route authority.");
                Require(ReferenceEquals(Runtime.CurrentRoute, expectedRoute),
                    $"ADR-006 terminal cleanup '{stage}' retained stale Route authority.");
                Require(Runtime.CurrentActivity == null,
                    $"ADR-006 terminal cleanup '{stage}' retained unexpected Activity authority.");
                Require(!Runtime.HasLifecycleRequestInFlight,
                    $"ADR-006 terminal cleanup '{stage}' retained an in-flight lifecycle request.");
                Require(!Runtime.CurrentTransitionGateSnapshot.HasBlockers,
                    $"ADR-006 terminal cleanup '{stage}' retained pure Transition Gate blockers. " +
                    $"count='{Runtime.CurrentTransitionGateSnapshot.BlockerCount}'.");
                Require(Runtime.CurrentTransitionGateMode == TransitionGateMode.None,
                    $"ADR-006 terminal cleanup '{stage}' retained Transition Gate mode " +
                    $"'{Runtime.CurrentTransitionGateMode}'.");
                Require(!Runtime.CurrentActivityEntryReadinessGateSnapshot.HasBlockers,
                    $"ADR-006 terminal cleanup '{stage}' retained readiness/recovery composite blockers. " +
                    $"count='{Runtime.CurrentActivityEntryReadinessGateSnapshot.BlockerCount}'.");
                Require(!Transition.HasPendingExecution,
                    $"ADR-006 terminal cleanup '{stage}' retained a pending QA Transition execution.");

                RuntimeScopeRoot[] roots = RuntimeContent.SnapshotRoots();
                RuntimeContentOwner expectedOwner = RuntimeContentOwner.Route(
                    expectedRoute.RouteId.StableText,
                    expectedRoute.RouteName,
                    RuntimeDefinitionToken.FromUnityObject(expectedRoute));
                Require(roots.Length == 1,
                    $"ADR-006 terminal cleanup '{stage}' expected exactly one authoritative Route root. " +
                    $"actual='{roots.Length}' runtimeContent='{RuntimeContent.ToDiagnosticString()}'.");
                Require(roots[0] != null && roots[0].Owner.Equals(expectedOwner),
                    $"ADR-006 terminal cleanup '{stage}' retained stale Runtime Content owner. " +
                    $"expected='{expectedOwner.StableText}' " +
                    $"actual='{(roots[0] != null ? roots[0].Owner.StableText : "<null>")}'.");
                Require(roots[0].HandleCount == 0,
                    $"ADR-006 terminal cleanup '{stage}' retained unexpected Runtime Content handles. " +
                    $"handleCount='{roots[0].HandleCount}'.");
            }

            public void Dispose()
            {
                for (int index = _temporaryAssets.Count - 1; index >= 0; index--)
                {
                    if (_temporaryAssets[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_temporaryAssets[index]);
                    }
                }

                _temporaryAssets.Clear();
            }

            private static RouteAsset CreateRoute(
                string routeId,
                string routeName,
                string scenePath,
                string sceneName)
            {
                RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
                route.name = routeName;
                try
                {
                    var serialized = new SerializedObject(route);
                    RequireProperty(serialized, "routeId").stringValue = routeId;
                    RequireProperty(serialized, "routeName").stringValue = routeName;
                    RequireProperty(serialized, "primaryScenePath").stringValue = scenePath;
                    RequireProperty(serialized, "primarySceneName").stringValue = sceneName;
                    RequireProperty(serialized, "routeContentProfile").objectReferenceValue = null;
                    RequireProperty(serialized, "startupActivity").objectReferenceValue = null;
                    SetEnumName(
                        RequireProperty(serialized, "transitionGateMode"),
                        TransitionGateMode.LifecycleRequestsOnly.ToString());
                    serialized.ApplyModifiedPropertiesWithoutUndo();

                    Require(route.HasValidRouteId,
                        $"Temporary ADR-006 Route '{routeName}' has invalid RouteId.");
                    Require(route.HasPrimaryScene,
                        $"Temporary ADR-006 Route '{routeName}' has no primary scene.");
                    Require(string.Equals(
                            route.PrimaryScenePath,
                            scenePath,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            route.PrimarySceneName,
                            sceneName,
                            StringComparison.Ordinal),
                        $"Temporary ADR-006 Route '{routeName}' did not retain the isolation scene identity.");
                    Require(!route.HasStartupActivity && !route.HasRouteContentProfile,
                        $"Temporary ADR-006 Route '{routeName}' must not own startup Activity or Route content.");
                    Require(route.TransitionGateMode ==
                            TransitionGateMode.LifecycleRequestsOnly,
                        $"Temporary ADR-006 Route '{routeName}' did not retain Transition Gate policy.");
                    return route;
                }
                catch
                {
                    UnityEngine.Object.DestroyImmediate(route);
                    throw;
                }
            }

            private static SerializedProperty RequireProperty(
                SerializedObject serialized,
                string name)
            {
                SerializedProperty property = serialized.FindProperty(name);
                Require(property != null,
                    $"Required serialized property '{name}' was not found.");
                return property;
            }

            private static void SetEnumName(
                SerializedProperty property,
                string value)
            {
                string[] names = property.enumNames;
                for (int index = 0; index < names.Length; index++)
                {
                    if (string.Equals(names[index], value, StringComparison.Ordinal))
                    {
                        property.enumValueIndex = index;
                        return;
                    }
                }

                throw new InvalidOperationException(
                    $"Serialized enum value '{value}' is not available.");
            }
        }

        private sealed class QaAdr006SceneLifecycleProbe : ISceneLifecycleParticipant
        {
            internal int AvailableCount { get; private set; }
            internal int ReleasingCount { get; private set; }
            internal Scene LastAvailableScene { get; private set; }
            internal Scene LastReleasingScene { get; private set; }

            public bool OnSceneAvailable(
                Scene scene,
                IReadOnlyList<GameObject> roots,
                out string diagnostic)
            {
                AvailableCount++;
                LastAvailableScene = scene;
                diagnostic =
                    $"ADR-006 QA observed Scene available. scene='{scene.name}' roots='{(roots != null ? roots.Count : 0)}'.";
                return true;
            }

            public bool OnSceneReleasing(
                Scene scene,
                IReadOnlyList<GameObject> roots,
                string reason,
                out string diagnostic)
            {
                ReleasingCount++;
                LastReleasingScene = scene;
                diagnostic =
                    $"ADR-006 QA observed Scene releasing. scene='{scene.name}' reason='{reason}'.";
                return true;
            }
        }

        private sealed class QaAdr006ControlledTransitionOrchestrator :
            ITransitionOrchestrator
        {
            private readonly List<TransitionRequest> _requests =
                new List<TransitionRequest>();

            internal TransitionPhase FailPhase { get; set; }
            internal int RequestCount => _requests.Count;
            internal TransitionRequest LastRequest { get; private set; }
            internal TransitionResult LastResult { get; private set; }
            internal bool HasPendingExecution { get; private set; }

            public TransitionResult Execute(TransitionRequest request)
            {
                Require(request.IsValid,
                    "ADR-006 controlled Transition requires a valid request.");
                Require(request.Scope == TransitionScope.Route,
                    $"ADR-006 controlled Transition expected Route scope, actual='{request.Scope}'.");
                Require(!HasPendingExecution,
                    "ADR-006 controlled Transition does not permit overlapping executions.");

                HasPendingExecution = true;
                try
                {
                    _requests.Add(request);
                    LastRequest = request;
                    LastResult = request.Phase == FailPhase
                        ? CreateFailure(request)
                        : CreateSuccess(request);
                    return LastResult;
                }
                finally
                {
                    HasPendingExecution = false;
                }
            }

#pragma warning disable CS1998 // QA orchestrator intentionally settles synchronously.
            public async Awaitable<TransitionResult> ExecuteAsync(
                TransitionRequest request)
            {
                return Execute(request);
            }
#pragma warning restore CS1998

            private static TransitionResult CreateSuccess(
                TransitionRequest request)
            {
                return TransitionResult.SucceededResult(
                    request.OperationId,
                    request.Kind,
                    request.Source,
                    request.Reason,
                    "ADR006InjectedSuccess",
                    new[]
                    {
                        TransitionStep.Succeeded(
                            0,
                            request.Phase,
                            BuildLabel(request, "success"),
                            "ADR-006 QA controlled Transition phase succeeded.")
                    });
            }

            private static TransitionResult CreateFailure(
                TransitionRequest request)
            {
                string boundary = request.Phase ==
                    TransitionPhase.OperationOpened
                        ? "Before"
                        : "After";
                string issue = $"ADR006Injected{boundary}Failure";
                return TransitionResult.FailedResult(
                    request.OperationId,
                    request.Kind,
                    request.Source,
                    request.Reason,
                    issue,
                    new[]
                    {
                        TransitionStep.Failed(
                            0,
                            request.Phase,
                            BuildLabel(request, "failure"),
                            $"ADR-006 QA injected {boundary} failure.")
                    },
                    new[] { issue });
            }

            private static string BuildLabel(
                TransitionRequest request,
                string terminal)
            {
                string boundary = request.Phase ==
                    TransitionPhase.OperationOpened
                        ? "before"
                        : "after";
                return
                    $"adr006-{request.Scope.ToString().ToLowerInvariant()}-{boundary}-{terminal}";
            }
        }

        private sealed class QaAdr006TemporarySceneScope
        {
            private readonly Scene _originalActiveScene;
            private bool _disposed;

            private QaAdr006TemporarySceneScope(
                Scene originalActiveScene,
                Scene isolationScene)
            {
                _originalActiveScene = originalActiveScene;
                IsolationScene = isolationScene;
            }

            internal Scene IsolationScene { get; }
            internal string ScenePath => IsolationScene.path;
            internal string SceneName => IsolationScene.name;

            internal static async Task<QaAdr006TemporarySceneScope> CreateAsync(
                string scenePath)
            {
                Require(EditorApplication.isPlaying,
                    "ADR-006 isolation scene requires Play Mode.");
                Require(!string.IsNullOrWhiteSpace(scenePath),
                    "ADR-006 isolation scene path is missing.");

                Scene originalActive = SceneManager.GetActiveScene();
                Require(originalActive.IsValid() && originalActive.isLoaded,
                    "ADR-006 isolation requires a valid loaded original active Scene.");

                Scene existing = SceneManager.GetSceneByPath(scenePath);
                Require(!existing.IsValid() || !existing.isLoaded,
                    $"ADR-006 isolation Scene '{scenePath}' is already loaded. " +
                    "Clean prior QA state before running the closure regression.");
                Require(Application.CanStreamedLevelBeLoaded(scenePath),
                    $"ADR-006 isolation Scene '{scenePath}' is unavailable to runtime loading. " +
                    "Add it to the active Build Profile or Shared Scene List / prepare the direct-policies QA fixture.");

                Scene loadedScene = default;
                try
                {
                    AsyncOperation load = SceneManager.LoadSceneAsync(
                        scenePath,
                        LoadSceneMode.Additive);
                    Require(load != null,
                        $"ADR-006 failed to start loading isolation Scene '{scenePath}'.");
                    while (!load.isDone)
                    {
                        await Awaitable.NextFrameAsync();
                    }

                    loadedScene = SceneManager.GetSceneByPath(scenePath);
                    Require(loadedScene.IsValid() && loadedScene.isLoaded,
                        $"ADR-006 could not resolve loaded isolation Scene '{scenePath}'.");
                    RequireNoFrameworkHost(loadedScene);
                    Require(SceneManager.SetActiveScene(loadedScene),
                        $"ADR-006 could not make isolation Scene '{scenePath}' active.");

                    return new QaAdr006TemporarySceneScope(
                        originalActive,
                        loadedScene);
                }
                catch
                {
                    await CleanupPartialAsync(
                        originalActive,
                        loadedScene,
                        scenePath);
                    throw;
                }
            }

            internal async Task DisposeAsync()
            {
                if (_disposed)
                {
                    return;
                }

                Require(_originalActiveScene.IsValid() &&
                        _originalActiveScene.isLoaded,
                    "ADR-006 original active Scene was lost before isolation cleanup.");
                if (SceneManager.GetActiveScene() != _originalActiveScene)
                {
                    Require(SceneManager.SetActiveScene(_originalActiveScene),
                        "ADR-006 could not restore the original active Scene.");
                }

                string scenePath = ScenePath;
                if (IsolationScene.IsValid() && IsolationScene.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(IsolationScene);
                    Require(unload != null,
                        $"ADR-006 failed to start unloading isolation Scene '{scenePath}'.");
                    while (!unload.isDone)
                    {
                        await Awaitable.NextFrameAsync();
                    }
                }

                await Awaitable.NextFrameAsync();
                Scene residual = SceneManager.GetSceneByPath(scenePath);
                Require(!residual.IsValid() || !residual.isLoaded,
                    $"ADR-006 isolation Scene '{scenePath}' remained loaded after cleanup.");
                Require(SceneManager.GetActiveScene() == _originalActiveScene,
                    "ADR-006 cleanup did not restore the original active Scene.");
                _disposed = true;
            }

            private static void RequireNoFrameworkHost(Scene scene)
            {
                GameObject[] roots = scene.GetRootGameObjects();
                for (int index = 0; index < roots.Length; index++)
                {
                    GameObject root = roots[index];
                    if (root == null)
                    {
                        continue;
                    }

                    FrameworkRuntimeHost[] hosts =
                        root.GetComponentsInChildren<FrameworkRuntimeHost>(true);
                    Require(hosts == null || hosts.Length == 0,
                        $"ADR-006 isolation Scene '{scene.path}' contains a FrameworkRuntimeHost. " +
                        "The closure regression requires a non-authoritative content Scene.");
                }
            }

            private static async Task CleanupPartialAsync(
                Scene originalActive,
                Scene loadedScene,
                string scenePath)
            {
                if (originalActive.IsValid() && originalActive.isLoaded &&
                    SceneManager.GetActiveScene() != originalActive)
                {
                    SceneManager.SetActiveScene(originalActive);
                }

                Scene candidate = loadedScene;
                if ((!candidate.IsValid() || !candidate.isLoaded) &&
                    !string.IsNullOrWhiteSpace(scenePath))
                {
                    candidate = SceneManager.GetSceneByPath(scenePath);
                }

                if (candidate.IsValid() && candidate.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(candidate);
                    if (unload != null)
                    {
                        while (!unload.isDone)
                        {
                            await Awaitable.NextFrameAsync();
                        }
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Foundation.Events;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Narrow Play Mode fixture for Identity Authority smokes.
    /// Captures authority by exact asset reference and definition token; never by stable ID alone.
    /// </summary>
    internal sealed class QaIdentityAuthorityFixture
    {
        private readonly List<UnityEngine.Object> temporaryObjects = new List<UnityEngine.Object>();
        private readonly List<RuntimeContentOwner> caseCreatedRootOwners = new List<RuntimeContentOwner>();
        private readonly List<QaOwnedAsyncOperation<FrameworkRouteRequestResult>> ownedRouteOperations =
            new List<QaOwnedAsyncOperation<FrameworkRouteRequestResult>>();
        private readonly List<QaOwnedAsyncOperation<FrameworkActivityRequestResult>> ownedActivityOperations =
            new List<QaOwnedAsyncOperation<FrameworkActivityRequestResult>>();
        private readonly List<IEventBinding> eventBindings = new List<IEventBinding>();

        private QaIdentityAuthorityFixture(FrameworkRuntimeHost host, AuthoritySnapshot initial)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Initial = initial;
            Failures = new QaFailureCollector();
        }

        public FrameworkRuntimeHost Host { get; }

        public AuthoritySnapshot Initial { get; }

        public QaFailureCollector Failures { get; }

        public RuntimeContentRuntime RuntimeContent => Host.RuntimeContentRuntime;

        public static QaIdentityAuthorityFixture Capture(
            FrameworkRuntimeHost host,
            string source)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (host.RuntimeContentRuntime == null)
            {
                throw new InvalidOperationException(
                    "FrameworkRuntimeHost has no RuntimeContentRuntime.");
            }

            AuthoritySnapshot initial = CaptureSnapshot(host, source);
            return new QaIdentityAuthorityFixture(host, initial);
        }

        public static AuthoritySnapshot CaptureSnapshot(
            FrameworkRuntimeHost host,
            string source)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            FrameworkRuntimeState state = host.State;
            RouteAsset route = state.CurrentRoute;
            ActivityAsset activity = state.CurrentActivity;

            if (route == null)
            {
                throw new InvalidOperationException(
                    "Identity Authority snapshot requires a current Route.");
            }

            if (activity == null)
            {
                throw new InvalidOperationException(
                    "Identity Authority snapshot requires a current Activity.");
            }

            if (!route.HasValidRouteId)
            {
                throw new InvalidOperationException(
                    "Current Route has no valid RouteId.");
            }

            if (!activity.HasValidActivityId)
            {
                throw new InvalidOperationException(
                    "Current Activity has no valid ActivityId.");
            }

            RuntimeDefinitionToken routeToken =
                RuntimeDefinitionToken.FromUnityObject(route);
            RuntimeDefinitionToken activityToken =
                RuntimeDefinitionToken.FromUnityObject(activity);

            RuntimeContentOwner routeOwner = RuntimeContentOwner.Route(
                route.RouteId.StableText,
                route.RouteName,
                routeToken);
            RuntimeContentOwner activityOwner = RuntimeContentOwner.Activity(
                activity.ActivityId.StableText,
                activity.ActivityName,
                activityToken);

            RuntimeContentRuntime runtimeContent = host.RuntimeContentRuntime;
            if (runtimeContent == null)
            {
                throw new InvalidOperationException(
                    "FrameworkRuntimeHost has no RuntimeContentRuntime.");
            }

            RuntimeScopeRoot[] roots = runtimeContent.SnapshotRoots() ?? Array.Empty<RuntimeScopeRoot>();
            int routeRootCount = CountRootsForOwner(roots, routeOwner);
            int activityRootCount = CountRootsForOwner(roots, activityOwner);

            return new AuthoritySnapshot(
                route,
                activity,
                routeOwner,
                activityOwner,
                routeToken,
                activityToken,
                roots.Length,
                routeRootCount,
                activityRootCount,
                state.GameFlowStarted,
                state.IsActivityReady,
                source);
        }

        public AuthoritySnapshot CaptureCurrent(string source) =>
            CaptureSnapshot(Host, source);

        public RuntimeContentOwner DeriveRouteOwner(RouteAsset route)
        {
            RequireAsset(route, "Route");
            if (!route.HasValidRouteId)
            {
                throw new InvalidOperationException("Route has no valid RouteId.");
            }

            return RuntimeContentOwner.Route(
                route.RouteId.StableText,
                route.RouteName,
                RuntimeDefinitionToken.FromUnityObject(route));
        }

        public RuntimeContentOwner DeriveActivityOwner(ActivityAsset activity)
        {
            RequireAsset(activity, "Activity");
            if (!activity.HasValidActivityId)
            {
                throw new InvalidOperationException("Activity has no valid ActivityId.");
            }

            return RuntimeContentOwner.Activity(
                activity.ActivityId.StableText,
                activity.ActivityName,
                RuntimeDefinitionToken.FromUnityObject(activity));
        }

        public RuntimeContentOwner RequireObservedRouteOwner(RouteAsset route)
        {
            RuntimeContentOwner derived = DeriveRouteOwner(route);
            RuntimeContentOwner observed = RequireObservedOwner(
                derived,
                RuntimeContentScope.Route,
                "Route");
            if (observed != derived)
            {
                throw new InvalidOperationException(
                    "Observed Route runtime owner does not match the owner derived from the exact Route reference. " +
                    $"derived='{derived}' observed='{observed}'.");
            }

            return observed;
        }

        public RuntimeContentOwner RequireObservedActivityOwner(ActivityAsset activity)
        {
            RuntimeContentOwner derived = DeriveActivityOwner(activity);
            RuntimeContentOwner observed = RequireObservedOwner(
                derived,
                RuntimeContentScope.Activity,
                "Activity");
            if (observed != derived)
            {
                throw new InvalidOperationException(
                    "Observed Activity runtime owner does not match the owner derived from the exact Activity reference. " +
                    $"derived='{derived}' observed='{observed}'.");
            }

            return observed;
        }

        public int CountRootsForOwner(RuntimeContentOwner owner)
        {
            RuntimeScopeRoot[] roots =
                RuntimeContent.SnapshotRoots() ?? Array.Empty<RuntimeScopeRoot>();
            return CountRootsForOwner(roots, owner);
        }

        public ActivityAsset CreateTemporaryActivity(
            string activityId,
            string activityName)
        {
            if (string.IsNullOrWhiteSpace(activityId))
            {
                throw new ArgumentException("Activity ID is required.", nameof(activityId));
            }

            if (string.IsNullOrWhiteSpace(activityName))
            {
                throw new ArgumentException("Activity name is required.", nameof(activityName));
            }

            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = activityName;
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = activityId;
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("playerParticipationProjectionMode").intValue =
                (int)ActivityParticipationProjectionMode.NoSlots;
            serialized.FindProperty("playerParticipationZeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Allowed;
            serialized.FindProperty("playerParticipationExplicitSlotProfiles").arraySize = 0;
            serialized.FindProperty("playerParticipationRequirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.None;
            serialized.FindProperty("activityEntryReadinessPolicy").intValue =
                (int)ActivityEntryReadinessPolicy.ObserveOnly;
            serialized.FindProperty("visualTransitionMode").intValue =
                (int)ActivityVisualTransitionMode.Seamless;
            serialized.FindProperty("transitionGateMode").intValue =
                (int)TransitionGateMode.LifecycleRequestsOnly;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TrackTemporary(activity);
            return activity;
        }

        public RouteAsset CreateTemporaryRoute(
            string routeId,
            string routeName,
            RouteAsset sceneTemplate,
            ActivityAsset startupActivity)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                throw new ArgumentException("Route ID is required.", nameof(routeId));
            }

            if (string.IsNullOrWhiteSpace(routeName))
            {
                throw new ArgumentException("Route name is required.", nameof(routeName));
            }

            if (sceneTemplate == null)
            {
                throw new ArgumentNullException(nameof(sceneTemplate));
            }

            if (string.IsNullOrWhiteSpace(sceneTemplate.PrimaryScenePath))
            {
                throw new InvalidOperationException(
                    "Scene template Route has no Primary Scene path.");
            }

            if (startupActivity == null)
            {
                throw new ArgumentNullException(nameof(startupActivity));
            }

            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = routeName;
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = routeId;
            serialized.FindProperty("routeName").stringValue = routeName;
            serialized.FindProperty("primaryScenePath").stringValue = sceneTemplate.PrimaryScenePath;
            serialized.FindProperty("primarySceneName").stringValue = sceneTemplate.PrimarySceneName;
            serialized.FindProperty("startupActivity").objectReferenceValue = startupActivity;
            serialized.FindProperty("transitionGateMode").intValue =
                (int)sceneTemplate.TransitionGateMode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            TrackTemporary(route);
            return route;
        }

        public void TrackTemporary(UnityEngine.Object value)
        {
            if (value != null)
            {
                temporaryObjects.Add(value);
            }
        }

        public void TrackCaseCreatedRoot(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot track an invalid runtime content owner root.");
            }

            caseCreatedRootOwners.Add(owner);
        }

        public void UntrackCaseCreatedRoot(RuntimeContentOwner owner)
        {
            for (int index = caseCreatedRootOwners.Count - 1; index >= 0; index--)
            {
                if (caseCreatedRootOwners[index] == owner)
                {
                    caseCreatedRootOwners.RemoveAt(index);
                }
            }
        }

        public RuntimeRootRegistryOperationResult CreateScopeRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            if (!owner.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot create a scope root for an invalid owner.");
            }

            RuntimeRootRegistryOperationResult result = RuntimeContent.CreateScopeRoot(
                owner,
                source,
                reason);
            if (result == null)
            {
                throw new InvalidOperationException(
                    $"CreateScopeRoot returned null for owner '{owner}'.");
            }

            if (result.Applied ||
                result.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists)
            {
                TrackCaseCreatedRoot(owner);
            }

            return result;
        }

        public RuntimeRootRegistryOperationResult RemoveScopeRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            if (!owner.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot remove a scope root for an invalid owner.");
            }

            RuntimeRootRegistryOperationResult result = RuntimeContent.RemoveScopeRoot(
                owner,
                source,
                reason);
            if (result == null)
            {
                throw new InvalidOperationException(
                    $"RemoveScopeRoot returned null for owner '{owner}'.");
            }

            if (result.Applied ||
                result.Status == RuntimeRootRegistryOperationStatus.RootMissing)
            {
                UntrackCaseCreatedRoot(owner);
            }

            return result;
        }

        public LifecycleListenerScope BindLifecycleListeners()
        {
            RouteLifecycleRuntime routeLifecycle = RequireRouteLifecycleRuntime();
            ActivityFlowRuntime activityFlow = routeLifecycle.CurrentActivityFlowRuntime;
            if (activityFlow == null)
            {
                throw new InvalidOperationException(
                    "Route Lifecycle has no Activity Flow runtime for lifecycle listeners.");
            }

            var scope = new LifecycleListenerScope();
            TrackBinding(routeLifecycle.SubscribeRouteEntered(scope.OnRouteEntered));
            TrackBinding(routeLifecycle.SubscribeRouteExited(scope.OnRouteExited));
            TrackBinding(activityFlow.SubscribeActivityEntered(scope.OnActivityEntered));
            TrackBinding(activityFlow.SubscribeActivityExited(scope.OnActivityExited));
            return scope;
        }

        public void ReleaseLifecycleListeners()
        {
            for (int index = eventBindings.Count - 1; index >= 0; index--)
            {
                IEventBinding binding = eventBindings[index];
                try
                {
                    binding?.Dispose();
                }
                catch (Exception exception)
                {
                    Failures.Add("dispose-lifecycle-listener", exception);
                }
            }

            eventBindings.Clear();
        }

        public QaOwnedAsyncOperation<FrameworkRouteRequestResult> AttachRouteRequest(
            string operationName,
            Task<FrameworkRouteRequestResult> request)
        {
            var operation = new QaOwnedAsyncOperation<FrameworkRouteRequestResult>(operationName);
            operation.Attach(request);
            ownedRouteOperations.Add(operation);
            return operation;
        }

        public QaOwnedAsyncOperation<FrameworkActivityRequestResult> AttachActivityRequest(
            string operationName,
            Task<FrameworkActivityRequestResult> request)
        {
            var operation = new QaOwnedAsyncOperation<FrameworkActivityRequestResult>(operationName);
            operation.Attach(request);
            ownedActivityOperations.Add(operation);
            return operation;
        }

        public async Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            QaOwnedAsyncOperation<FrameworkRouteRequestResult> operation =
                AttachRouteRequest(
                    reason,
                    Host.RequestRouteAsync(targetRoute, source, reason));
            return await operation.AwaitTerminalAsync();
        }

        public async Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            QaOwnedAsyncOperation<FrameworkActivityRequestResult> operation =
                AttachActivityRequest(
                    reason,
                    Host.RequestActivityAsync(targetActivity, source, reason));
            return await operation.AwaitTerminalAsync();
        }

        public async Task RestoreToAsync(AuthoritySnapshot target, string source)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            RouteAsset currentRoute = Host.State.CurrentRoute;
            ActivityAsset currentActivity = Host.State.CurrentActivity;

            bool routeChanged = !ReferenceEquals(currentRoute, target.Route);
            bool activityChanged = !ReferenceEquals(currentActivity, target.Activity);
            if (!routeChanged && !activityChanged)
            {
                return;
            }

            if (routeChanged)
            {
                FrameworkRouteRequestResult routeResult = await RequestRouteAsync(
                    target.Route,
                    source,
                    "restore-target-route");
                bool routeAccepted =
                    routeResult.Succeeded ||
                    routeResult.DestinationAuthoritative ||
                    routeResult.Kind == FrameworkRouteRequestKind.IgnoredAlreadyActive;
                if (!routeAccepted)
                {
                    throw new InvalidOperationException(
                        "Failed to restore target Route by reference. " +
                        $"kind='{routeResult.Kind}' message='{routeResult.Message}'.");
                }
            }
            else if (activityChanged)
            {
                FrameworkActivityRequestResult activityResult = await RequestActivityAsync(
                    target.Activity,
                    source,
                    "restore-target-activity");
                bool activityAccepted =
                    activityResult.Succeeded ||
                    activityResult.DestinationAuthoritative ||
                    activityResult.Kind == FrameworkActivityRequestKind.IgnoredAlreadyActive;
                if (!activityAccepted)
                {
                    throw new InvalidOperationException(
                        "Failed to restore target Activity by reference. " +
                        $"kind='{activityResult.Kind}' message='{activityResult.Message}'.");
                }
            }

            if (!ReferenceEquals(Host.State.CurrentRoute, target.Route))
            {
                throw new InvalidOperationException(
                    "Restore completed without re-establishing the target Route reference.");
            }

            if (!ReferenceEquals(Host.State.CurrentActivity, target.Activity))
            {
                throw new InvalidOperationException(
                    "Restore completed without re-establishing the target Activity reference.");
            }
        }

        public async Task TeardownAsync(string source)
        {
            await AwaitOwnedOperationsAsync();
            ReleaseLifecycleListeners();
            try
            {
                await RestoreToAsync(Initial, source + ".restore-initial");
            }
            catch (Exception exception)
            {
                Failures.Add("restore-initial-authority", exception);
            }

            RemoveCaseCreatedRoots(source);
            DestroyTemporaryObjects();
            AssertAuthorityPreserved(source);
        }

        /// <summary>
        /// Verifies current Route/Activity authority and root counts match a prior snapshot by
        /// exact reference, token and owner equality — never by stable ID alone.
        /// </summary>
        public void AssertSnapshotPreserved(
            AuthoritySnapshot expected,
            string source)
        {
            if (expected == null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            AuthoritySnapshot current = CaptureSnapshot(Host, source + ".verify");
            if (!ReferenceEquals(current.Route, expected.Route))
            {
                throw new InvalidOperationException(
                    $"Route reference diverged from case snapshot. source='{source}'.");
            }

            if (!ReferenceEquals(current.Activity, expected.Activity))
            {
                throw new InvalidOperationException(
                    $"Activity reference diverged from case snapshot. source='{source}'.");
            }

            if (current.RouteOwner != expected.RouteOwner ||
                current.ActivityOwner != expected.ActivityOwner)
            {
                throw new InvalidOperationException(
                    $"Owners diverged from case snapshot. source='{source}' " +
                    $"expectedRouteOwner='{expected.RouteOwner}' actualRouteOwner='{current.RouteOwner}' " +
                    $"expectedActivityOwner='{expected.ActivityOwner}' actualActivityOwner='{current.ActivityOwner}'.");
            }

            if (current.RouteToken != expected.RouteToken ||
                current.ActivityToken != expected.ActivityToken)
            {
                throw new InvalidOperationException(
                    $"Definition tokens diverged from case snapshot. source='{source}'.");
            }

            if (current.TotalRootCount != expected.TotalRootCount ||
                current.RouteRootCount != expected.RouteRootCount ||
                current.ActivityRootCount != expected.ActivityRootCount)
            {
                throw new InvalidOperationException(
                    $"Root counts diverged from case snapshot. source='{source}' " +
                    $"expected=({DescribeRoots(expected)}) actual=({DescribeRoots(current)}).");
            }
        }

        public void DestroyTrackedTemporary(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            temporaryObjects.Remove(value);
            try
            {
                UnityEngine.Object.DestroyImmediate(value);
            }
            catch (Exception exception)
            {
                Failures.Add($"destroy-temporary:{value.name}", exception);
            }
        }

        public void DestroyTrackedTemporaries(params UnityEngine.Object[] values)
        {
            if (values == null)
            {
                return;
            }

            for (int index = values.Length - 1; index >= 0; index--)
            {
                DestroyTrackedTemporary(values[index]);
            }
        }

        /// <summary>
        /// Case-scoped teardown: await ops, drop listeners, remove remaining case roots by exact owner,
        /// restore Route/Activity by reference, destroy case temporaries, assert snapshot preservation.
        /// Cleanup failures are aggregated on <see cref="Failures"/> and also thrown so the case fails.
        /// </summary>
        public async Task FinalizeCaseAsync(
            AuthoritySnapshot caseBefore,
            string source,
            params UnityEngine.Object[] caseTemporaries)
        {
            var caseCleanup = new QaFailureCollector();

            try
            {
                await AwaitOwnedOperationsAsync();
            }
            catch (Exception exception)
            {
                caseCleanup.Add(source + ".await-ops", exception);
            }

            ReleaseLifecycleListeners();

            try
            {
                // Remove only case-created roots that remain (exact owner/token).
                RemoveCaseCreatedRoots(source + ".case-roots");
            }
            catch (Exception exception)
            {
                caseCleanup.Add(source + ".case-roots", exception);
            }

            try
            {
                await RestoreToAsync(caseBefore, source + ".restore");
            }
            catch (Exception exception)
            {
                caseCleanup.Add(source + ".restore", exception);
            }

            DestroyTrackedTemporaries(caseTemporaries);

            try
            {
                AssertSnapshotPreserved(caseBefore, source + ".preserved");
            }
            catch (Exception exception)
            {
                caseCleanup.Add(source + ".snapshot-preserved", exception);
            }

            if (!caseCleanup.HasFailures)
            {
                return;
            }

            // Promote into the long-lived collector for the final [IF_ID_QA] report.
            Failures.Add(source + ".cleanup", caseCleanup.ToAggregate(
                $"Case cleanup failed for '{source}'."));
            throw caseCleanup.ToAggregate($"Case cleanup failed for '{source}'.");
        }

        public string DescribeRoots(AuthoritySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "roots='unavailable'";
            }

            return
                $"totalRoots='{snapshot.TotalRootCount}' " +
                $"routeRoots='{snapshot.RouteRootCount}' " +
                $"activityRoots='{snapshot.ActivityRootCount}'";
        }

        public string DescribeAuthority(AuthoritySnapshot snapshot)
        {
            if (snapshot == null)
            {
                return "authority='unavailable'";
            }

            return
                $"route='{Escape(snapshot.Route != null ? snapshot.Route.RouteName : "<null>")}' " +
                $"routeStableId='{Escape(snapshot.Route != null ? snapshot.Route.RouteId.StableText : "<null>")}' " +
                $"routeToken='{Escape(snapshot.RouteToken.StableText)}' " +
                $"routeOwner='{Escape(snapshot.RouteOwner.StableText)}' " +
                $"activity='{Escape(snapshot.Activity != null ? snapshot.Activity.ActivityName : "<null>")}' " +
                $"activityStableId='{Escape(snapshot.Activity != null ? snapshot.Activity.ActivityId.StableText : "<null>")}' " +
                $"activityToken='{Escape(snapshot.ActivityToken.StableText)}' " +
                $"activityOwner='{Escape(snapshot.ActivityOwner.StableText)}' " +
                DescribeRoots(snapshot);
        }

        private RouteLifecycleRuntime RequireRouteLifecycleRuntime()
        {
            GameFlowRuntime gameFlow = Host.CurrentGameFlowRuntime;
            if (gameFlow == null)
            {
                throw new InvalidOperationException(
                    "FrameworkRuntimeHost has no GameFlowRuntime.");
            }

            RouteLifecycleRuntime routeLifecycle = gameFlow.CurrentRouteLifecycleRuntime;
            if (routeLifecycle == null)
            {
                throw new InvalidOperationException(
                    "GameFlowRuntime has no active Route Lifecycle runtime.");
            }

            return routeLifecycle;
        }

        private void TrackBinding(IEventBinding binding)
        {
            if (binding != null)
            {
                eventBindings.Add(binding);
            }
        }

        private RuntimeContentOwner RequireObservedOwner(
            RuntimeContentOwner derived,
            RuntimeContentScope scope,
            string label)
        {
            RuntimeScopeRoot[] roots =
                RuntimeContent.SnapshotRoots() ?? Array.Empty<RuntimeScopeRoot>();

            RuntimeContentOwner? exactMatch = null;
            RuntimeContentOwner? sameStableDifferentToken = null;
            int scopedCount = 0;

            for (int index = 0; index < roots.Length; index++)
            {
                RuntimeScopeRoot root = roots[index];
                if (root == null || root.Scope != scope)
                {
                    continue;
                }

                scopedCount++;
                RuntimeContentOwner owner = root.Owner;
                if (!owner.IsValid || !owner.HasDefinitionToken)
                {
                    throw new InvalidOperationException(
                        $"Runtime {label} content root is missing a valid definition token. owner='{owner}'.");
                }

                if (owner == derived)
                {
                    exactMatch = owner;
                }
                else if (owner.HasSameStableDefinition(derived))
                {
                    sameStableDifferentToken = owner;
                }
            }

            if (exactMatch.HasValue)
            {
                return exactMatch.Value;
            }

            if (sameStableDifferentToken.HasValue)
            {
                throw new InvalidOperationException(
                    $"Runtime {label} owner shares stable identity but not the definition token. " +
                    $"derived='{derived}' observed='{sameStableDifferentToken.Value}' scopedRoots='{scopedCount}'.");
            }

            throw new InvalidOperationException(
                $"No runtime content root was observed for the current {label} owner. " +
                $"derived='{derived}' scopedRoots='{scopedCount}' totalRoots='{roots.Length}'.");
        }

        private async Task AwaitOwnedOperationsAsync()
        {
            for (int index = 0; index < ownedRouteOperations.Count; index++)
            {
                QaOwnedAsyncOperation<FrameworkRouteRequestResult> operation =
                    ownedRouteOperations[index];
                if (operation == null || !operation.HasOperation || operation.ReachedTerminal)
                {
                    continue;
                }

                try
                {
                    await operation.AwaitTerminalAsync();
                }
                catch (Exception exception)
                {
                    Failures.Add($"await-route-operation:{operation.Name}", exception);
                }
            }

            for (int index = 0; index < ownedActivityOperations.Count; index++)
            {
                QaOwnedAsyncOperation<FrameworkActivityRequestResult> operation =
                    ownedActivityOperations[index];
                if (operation == null || !operation.HasOperation || operation.ReachedTerminal)
                {
                    continue;
                }

                try
                {
                    await operation.AwaitTerminalAsync();
                }
                catch (Exception exception)
                {
                    Failures.Add($"await-activity-operation:{operation.Name}", exception);
                }
            }
        }

        private void RemoveCaseCreatedRoots(string source)
        {
            for (int index = caseCreatedRootOwners.Count - 1; index >= 0; index--)
            {
                RuntimeContentOwner owner = caseCreatedRootOwners[index];
                try
                {
                    RuntimeRootRegistryOperationResult result = RuntimeContent.RemoveScopeRoot(
                        owner,
                        source,
                        "teardown-case-created-root");
                    if (result == null)
                    {
                        throw new InvalidOperationException(
                            $"RemoveScopeRoot returned null for owner '{owner}'.");
                    }
                }
                catch (Exception exception)
                {
                    Failures.Add($"remove-case-root:{owner.StableText}", exception);
                }
            }

            caseCreatedRootOwners.Clear();
        }

        private void DestroyTemporaryObjects()
        {
            for (int index = temporaryObjects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object value = temporaryObjects[index];
                if (value == null)
                {
                    continue;
                }

                try
                {
                    UnityEngine.Object.DestroyImmediate(value);
                }
                catch (Exception exception)
                {
                    Failures.Add($"destroy-temporary:{value.name}", exception);
                }
            }

            temporaryObjects.Clear();
        }

        private void AssertAuthorityPreserved(string source)
        {
            try
            {
                AuthoritySnapshot finalSnapshot = CaptureSnapshot(Host, source + ".final");
                if (!ReferenceEquals(finalSnapshot.Route, Initial.Route))
                {
                    throw new InvalidOperationException(
                        "Final Route reference diverged from the initial snapshot.");
                }

                if (!ReferenceEquals(finalSnapshot.Activity, Initial.Activity))
                {
                    throw new InvalidOperationException(
                        "Final Activity reference diverged from the initial snapshot.");
                }

                if (finalSnapshot.RouteOwner != Initial.RouteOwner)
                {
                    throw new InvalidOperationException(
                        "Final Route owner diverged from the initial snapshot. " +
                        $"initial='{Initial.RouteOwner}' final='{finalSnapshot.RouteOwner}'.");
                }

                if (finalSnapshot.ActivityOwner != Initial.ActivityOwner)
                {
                    throw new InvalidOperationException(
                        "Final Activity owner diverged from the initial snapshot. " +
                        $"initial='{Initial.ActivityOwner}' final='{finalSnapshot.ActivityOwner}'.");
                }

                if (finalSnapshot.RouteToken != Initial.RouteToken ||
                    finalSnapshot.ActivityToken != Initial.ActivityToken)
                {
                    throw new InvalidOperationException(
                        "Final definition tokens diverged from the initial snapshot.");
                }

                if (finalSnapshot.TotalRootCount != Initial.TotalRootCount ||
                    finalSnapshot.RouteRootCount != Initial.RouteRootCount ||
                    finalSnapshot.ActivityRootCount != Initial.ActivityRootCount)
                {
                    throw new InvalidOperationException(
                        "Final runtime content roots diverged from the initial snapshot. " +
                        $"initial=({DescribeRoots(Initial)}) final=({DescribeRoots(finalSnapshot)}).");
                }
            }
            catch (Exception exception)
            {
                Failures.Add("assert-authority-preserved", exception);
            }
        }

        private static int CountRootsForOwner(
            RuntimeScopeRoot[] roots,
            RuntimeContentOwner owner)
        {
            if (roots == null || !owner.IsValid)
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < roots.Length; index++)
            {
                RuntimeScopeRoot root = roots[index];
                if (root != null && root.Owner == owner)
                {
                    count++;
                }
            }

            return count;
        }

        private static void RequireAsset(UnityEngine.Object asset, string label)
        {
            if (asset == null)
            {
                throw new InvalidOperationException($"{label} asset reference is null.");
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");

        internal sealed class AuthoritySnapshot
        {
            public AuthoritySnapshot(
                RouteAsset route,
                ActivityAsset activity,
                RuntimeContentOwner routeOwner,
                RuntimeContentOwner activityOwner,
                RuntimeDefinitionToken routeToken,
                RuntimeDefinitionToken activityToken,
                int totalRootCount,
                int routeRootCount,
                int activityRootCount,
                bool gameFlowStarted,
                bool isActivityReady,
                string source)
            {
                Route = route;
                Activity = activity;
                RouteOwner = routeOwner;
                ActivityOwner = activityOwner;
                RouteToken = routeToken;
                ActivityToken = activityToken;
                TotalRootCount = totalRootCount;
                RouteRootCount = routeRootCount;
                ActivityRootCount = activityRootCount;
                GameFlowStarted = gameFlowStarted;
                IsActivityReady = isActivityReady;
                Source = source ?? string.Empty;
            }

            public RouteAsset Route { get; }
            public ActivityAsset Activity { get; }
            public RuntimeContentOwner RouteOwner { get; }
            public RuntimeContentOwner ActivityOwner { get; }
            public RuntimeDefinitionToken RouteToken { get; }
            public RuntimeDefinitionToken ActivityToken { get; }
            public int TotalRootCount { get; }
            public int RouteRootCount { get; }
            public int ActivityRootCount { get; }
            public bool GameFlowStarted { get; }
            public bool IsActivityReady { get; }
            public string Source { get; }
        }

        internal sealed class LifecycleListenerScope
        {
            public int RouteEnterCount { get; private set; }
            public int RouteExitCount { get; private set; }
            public int ActivityEnterCount { get; private set; }
            public int ActivityExitCount { get; private set; }
            public RouteAsset LastEnteredRoute { get; private set; }
            public RouteAsset LastExitedRoute { get; private set; }
            public ActivityAsset LastEnteredActivity { get; private set; }
            public ActivityAsset LastExitedActivity { get; private set; }

            public void OnRouteEntered(RouteEnteredEvent value)
            {
                if (value == null)
                {
                    return;
                }

                RouteEnterCount++;
                LastEnteredRoute = value.Route;
            }

            public void OnRouteExited(RouteExitedEvent value)
            {
                if (value == null)
                {
                    return;
                }

                RouteExitCount++;
                LastExitedRoute = value.Route;
            }

            public void OnActivityEntered(ActivityEnteredEvent value)
            {
                if (value == null)
                {
                    return;
                }

                ActivityEnterCount++;
                LastEnteredActivity = value.Activity;
            }

            public void OnActivityExited(ActivityExitedEvent value)
            {
                if (value == null)
                {
                    return;
                }

                ActivityExitCount++;
                LastExitedActivity = value.Activity;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RuntimeContent;
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

        public QaOwnedAsyncOperation<FrameworkRouteRequestResult> AttachRouteRequest(
            string operationName,
            Task<FrameworkRouteRequestResult> request)
        {
            var operation = new QaOwnedAsyncOperation<FrameworkRouteRequestResult>(operationName);
            operation.Attach(request);
            ownedRouteOperations.Add(operation);
            return operation;
        }

        public async Task TeardownAsync(string source)
        {
            await AwaitOwnedOperationsAsync();
            await RestoreInitialAuthorityAsync(source);
            RemoveCaseCreatedRoots(source);
            DestroyTemporaryObjects();
            AssertAuthorityPreserved(source);
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
                    Failures.Add($"await-operation:{operation.Name}", exception);
                }
            }
        }

        private async Task RestoreInitialAuthorityAsync(string source)
        {
            RouteAsset currentRoute = Host.State.CurrentRoute;
            ActivityAsset currentActivity = Host.State.CurrentActivity;

            bool routeChanged = !ReferenceEquals(currentRoute, Initial.Route);
            bool activityChanged = !ReferenceEquals(currentActivity, Initial.Activity);
            if (!routeChanged && !activityChanged)
            {
                return;
            }

            try
            {
                QaOwnedAsyncOperation<FrameworkRouteRequestResult> restore =
                    AttachRouteRequest(
                        "restore-initial-route",
                        Host.RequestRouteAsync(
                            Initial.Route,
                            source,
                            "restore-initial-authority"));
                FrameworkRouteRequestResult result = await restore.AwaitTerminalAsync();
                if (!result.Succeeded && !result.DestinationAuthoritative)
                {
                    throw new InvalidOperationException(
                        "Failed to restore the initial Route/Activity authority. " +
                        $"kind='{result.Kind}' message='{result.Message}'.");
                }

                if (!ReferenceEquals(Host.State.CurrentRoute, Initial.Route))
                {
                    throw new InvalidOperationException(
                        "Restore completed without re-establishing the initial Route reference.");
                }

                if (!ReferenceEquals(Host.State.CurrentActivity, Initial.Activity))
                {
                    throw new InvalidOperationException(
                        "Restore completed without re-establishing the initial Activity reference.");
                }
            }
            catch (Exception exception)
            {
                Failures.Add("restore-initial-authority", exception);
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
                    UnityEngine.Object.Destroy(value);
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
    }
}

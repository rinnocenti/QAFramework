using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaRouteActivityIdentityRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run Route and Activity Identity Regression";
        private const string LogPrefix = "[ROUTE_ACTIVITY_IDENTITY_REGRESSION]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();
            try
            {
                Require(EditorApplication.isPlaying,
                    "Route and Activity Identity Regression requires a fresh Play Mode after framework boot.");
                completed.Add("play-mode-required");
                Require(QaH2FrameworkReadiness.TryResolveUniqueHost(
                    out FrameworkRuntimeHost host, out string hostDiagnostic), hostDiagnostic);
                FrameworkRuntimeState state = host.State;
                Require(state.GameFlowStarted && state.CurrentRoute != null && state.CurrentActivity != null && state.IsActivityReady,
                    $"Framework runtime is not ready. {hostDiagnostic}");
                completed.Add("official-runtime-ready");

                RouteAsset currentRoute = state.CurrentRoute;
                ActivityAsset currentActivity = state.CurrentActivity;
                Require(currentRoute.HasValidRouteId && currentActivity.HasValidActivityId,
                    "Runtime state contains an invalid RouteId or ActivityId.");
                completed.Add("runtime-assets-have-canonical-ids");
                Require(state.RouteState.RouteIdentity == FrameworkIdentityKey.From(currentRoute.RouteId),
                    "Runtime Route state did not transport RouteId.");
                completed.Add("runtime-state-transports-route-id");
                Require(state.ActivityState.ActivityIdentity == FrameworkIdentityKey.From(currentActivity.ActivityId),
                    "Runtime Activity state did not transport ActivityId.");
                completed.Add("runtime-state-transports-activity-id");
                Require(!string.Equals(currentRoute.RouteId.StableText, currentRoute.RouteName, StringComparison.Ordinal) &&
                        !string.Equals(currentActivity.ActivityId.StableText, currentActivity.ActivityName, StringComparison.Ordinal),
                    "A display name is being used as functional identity.");
                completed.Add("ids-are-distinct-from-display-names");

                RouteAsset routeA = CreateRoute("qa.identity.route.a", "Shared Route", "Assets/Shared.unity", created);
                RouteAsset routeB = CreateRoute("qa.identity.route.b", "Shared Route", "Assets/Shared.unity", created);
                ActivityAsset activityA = CreateActivity("qa.identity.activity.a", "Shared Activity", created);
                ActivityAsset activityB = CreateActivity("qa.identity.activity.b", "Shared Activity", created);
                RouteId equalRouteId = RouteId.From(routeA.RouteId.StableText);
                ActivityId equalActivityId = ActivityId.From(activityA.ActivityId.StableText);
                Require(routeA.RouteId != routeB.RouteId && routeA.RouteId == equalRouteId &&
                        routeA.RouteId.GetHashCode() == equalRouteId.GetHashCode(),
                    "Typed RouteId equality or hashing is incoherent.");
                Require(activityA.ActivityId != activityB.ActivityId && activityA.ActivityId == equalActivityId &&
                        activityA.ActivityId.GetHashCode() == equalActivityId.GetHashCode(),
                    "Typed ActivityId equality or hashing is incoherent.");
                completed.Add("typed-id-equality-and-hashing");

                FrameworkIdentityKey routeIdentityBefore = RouteRuntimeState.EnteredWith(
                    routeA, default, default, default, default, default, "qa", "identity").RouteIdentity;
                FrameworkIdentityKey activityIdentityBefore = ActivityRuntimeState.ActiveWith(
                    activityA, null, "qa", "identity").ActivityIdentity;
                ConfigureRoute(routeA, routeA.RouteId.StableText, "Renamed Route", "Assets/Renamed.unity");
                ConfigureActivity(activityA, activityA.ActivityId.StableText, "Renamed Activity");
                Require(routeIdentityBefore == RouteRuntimeState.EnteredWith(
                        routeA, default, default, default, default, default, "qa", "identity").RouteIdentity,
                    "Route rename or scene change changed runtime identity.");
                Require(activityIdentityBefore == ActivityRuntimeState.ActiveWith(
                        activityA, null, "qa", "identity").ActivityIdentity,
                    "Activity rename changed runtime identity.");
                completed.Add("rename-and-scene-change-preserve-identity");

                VerifyAdmissionToken(routeA, routeB, activityA, activityB);
                completed.Add("admission-token-transports-route-ids");
                VerifyLedger(routeA, activityA);
                completed.Add("activity-scene-ledger-exposes-activity-id");
                VerifyObjectEntry(activityA, created);
                completed.Add("object-entry-uses-typed-activity-owner");

                Require(state.CurrentRoute.RouteId.StableText == state.CurrentRouteIdentity &&
                        state.CurrentActivity.ActivityId.StableText == state.CurrentActivityIdentity,
                    "Runtime diagnostics do not expose ID separately from display name.");
                completed.Add("runtime-diagnostics-separate-id-and-name");
                completed.Add("player-gameplay-admission-route-switch-dependency-registered");
                completed.Add("activity-transition-transaction-dependency-registered");

                Require(completed.Count == 14,
                    $"Route and Activity identity case count changed. actual='{completed.Count}'.");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' routeId='{currentRoute.RouteId}' " +
                    $"routeName='{currentRoute.RouteName}' activityId='{currentActivity.ActivityId}' " +
                    $"activityName='{currentActivity.ActivityName}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                    if (created[index] != null) UnityEngine.Object.DestroyImmediate(created[index]);
            }
        }

        private static void VerifyAdmissionToken(
            RouteAsset previousRoute, RouteAsset targetRoute,
            ActivityAsset previousActivity, ActivityAsset targetActivity)
        {
            var token = new ActivityPlayerLifecycleAdmissionToken(
                "qa.identity.session",
                RuntimeContentOwner.Activity(previousActivity.ActivityId.StableText, previousActivity.ActivityName),
                RuntimeContentOwner.Activity(targetActivity.ActivityId.StableText, targetActivity.ActivityName),
                ActivityPlayerLifecycleAdmissionFlowKind.RouteStartupActivitySwitch,
                previousRoute.RouteId, targetRoute.RouteId, 1);
            var mismatched = new ActivityPlayerLifecycleAdmissionToken(
                "qa.identity.session",
                token.PreviousOwner, token.TargetOwner,
                ActivityPlayerLifecycleAdmissionFlowKind.RouteStartupActivitySwitch,
                previousRoute.RouteId, RouteId.From("qa.identity.route.foreign"), 1);
            Require(token.IsValid && token.PreviousRouteId == previousRoute.RouteId &&
                    token.TargetRouteId == targetRoute.RouteId && token != mismatched,
                "Admission token lost typed Route identity or accepted mismatched equality.");
        }

        private static void VerifyLedger(RouteAsset route, ActivityAsset activity)
        {
            ActivitySceneCompositionPlanEntry plan = ActivitySceneCompositionPlanEntry.FromEntry(
                null, 0, activity.ActivityId.StableText);
            var entry = new ActivitySceneLedgerEntry(
                "qa.identity.route.instance", route, activity, plan,
                ActivitySceneLedgerOwnership.Activity, ActivitySceneLedgerEntryStatus.Loaded);
            Require(entry.ActivityId == activity.ActivityId &&
                    entry.ToDiagnosticString().Contains($"activity='{activity.ActivityId.StableText}'"),
                "Activity Scene Ledger entry did not preserve ActivityId.");
        }

        private static void VerifyObjectEntry(ActivityAsset activity, ICollection<UnityEngine.Object> created)
        {
            var root = new GameObject("Identity Object Entry");
            created.Add(root);
            ObjectEntryDeclaration declaration = root.AddComponent<ObjectEntryDeclaration>();
            declaration.ConfigureForQa(
                "qa.identity.entry", ObjectEntryScope.Activity,
                ObjectEntryRequiredness.Required, "Identity Entry", null, activity);
            ObjectEntryDescriptor descriptor = declaration.CreateDescriptor();
            Require(descriptor.OwnerIdentity.HasValue &&
                    descriptor.OwnerIdentity.Value == FrameworkIdentityKey.From(activity.ActivityId),
                "Object Entry owner is not the typed Activity identity.");
        }

        private static RouteAsset CreateRoute(
            string id, string name, string scenePath, ICollection<UnityEngine.Object> created)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            created.Add(route);
            ConfigureRoute(route, id, name, scenePath);
            return route;
        }

        private static ActivityAsset CreateActivity(
            string id, string name, ICollection<UnityEngine.Object> created)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            created.Add(activity);
            ConfigureActivity(activity, id, name);
            return activity;
        }

        private static void ConfigureRoute(RouteAsset route, string id, string name, string scenePath)
        {
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = id;
            serialized.FindProperty("routeName").stringValue = name;
            serialized.FindProperty("primaryScenePath").stringValue = scenePath;
            serialized.FindProperty("primarySceneName").stringValue = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureActivity(ActivityAsset activity, string id, string name)
        {
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = id;
            serialized.FindProperty("activityName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

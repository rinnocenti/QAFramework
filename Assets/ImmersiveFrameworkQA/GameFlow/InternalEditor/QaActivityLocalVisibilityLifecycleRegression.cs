using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaActivityLocalVisibilityLifecycleRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Activity Flow/Run Activity Local Visibility Lifecycle Regression";
        private const string RouteAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteA.asset";
        private const string RouteBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteB.asset";
        private const string ActivityAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityA.asset";
        private const string ActivityBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityB.asset";
        private const string ActivityCPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleNoContentActivity.asset";
        private const string AdditionalScenePath = "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleAdditional.unity";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            var roots = new List<GameObject>();
            FrameworkRuntimeHost host = null;
            RouteAsset initialRoute = null;
            ActivityAsset initialActivity = null;
            Exception failure = null;
            Exception restoreFailure = null;

            try
            {
                Require(EditorApplication.isPlaying, "Activity local visibility lifecycle regression requires Play Mode.");
                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out host,
                        out string diagnostic),
                    diagnostic);
                Require(
                    host.State.GameFlowStarted &&
                    host.State.CurrentRoute != null,
                    "Game Flow is not ready.");

                IRouteRuntimePort routes = (IRouteRuntimePort)host;
                IActivityRuntimePort activities = (IActivityRuntimePort)host;
                RouteAsset routeA = Load<RouteAsset>(RouteAPath);
                RouteAsset routeB = Load<RouteAsset>(RouteBPath);
                ActivityAsset activityA = Load<ActivityAsset>(ActivityAPath);
                ActivityAsset activityB = Load<ActivityAsset>(ActivityBPath);
                ActivityAsset activityC = Load<ActivityAsset>(ActivityCPath);
                initialRoute = host.State.CurrentRoute;
                initialActivity = host.State.CurrentActivity;

                if (host.State.CurrentRoute.HasSameIdentity(routeA))
                {
                    await RequireRouteAsync(routes, routeB, "route-a-entry-precondition");
                }

                await RequireRouteAsync(routes, routeA, "load-qa-lifecycle-additional");
                Scene additionalScene = SceneManager.GetSceneByPath(AdditionalScenePath);
                Require(additionalScene.IsValid() && additionalScene.isLoaded,
                    "QA_LifecycleAdditional must be loaded by Route A.");

                Fixture fixture = Fixture.Create(additionalScene, activityA, activityB, roots);
                await RunPositiveSingleCases(activities, fixture, activityA, activityB);
                await RunPositiveMultipleCases(activities, fixture, activityA, activityB, activityC);
                await RunNegativeSingleCases(activities, fixture, activityA, activityB);
                await RunNegativeMultipleCases(activities, fixture, activityA, activityB, activityC);
                await RunNoActiveVisibleCases(activities, fixture, activityA, activityB);
                await RunInvalidRuleCase(activities, additionalScene, activityC);
                await RunClearAndIdempotenceCases(host, activities, fixture, activityA);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                DestroyRoots(roots);

                if (host != null && initialRoute != null)
                {
                    try
                    {
                        IRouteRuntimePort routes = (IRouteRuntimePort)host;
                        IActivityRuntimePort activities = (IActivityRuntimePort)host;
                        if (host.State.CurrentRoute == null ||
                            !host.State.CurrentRoute.HasSameIdentity(initialRoute))
                        {
                            await RequireRouteAsync(routes, initialRoute, "restore-route");
                        }

                        if (initialActivity == null && host.State.CurrentActivity != null)
                        {
                            await RequireClearAsync(activities, "restore-no-active");
                        }
                        else if (initialActivity != null &&
                                 (host.State.CurrentActivity == null ||
                                  !host.State.CurrentActivity.HasSameIdentity(initialActivity)))
                        {
                            await RequireActivityAsync(activities, initialActivity, "restore-activity");
                        }
                    }
                    catch (Exception exception)
                    {
                        restoreFailure = exception;
                    }
                }
            }

            if (failure != null || restoreFailure != null)
            {
                Exception reported = failure != null && restoreFailure != null
                    ? new AggregateException(failure, restoreFailure)
                    : failure ?? restoreFailure;
                Debug.LogError($"[QA_ACTIVITY_LOCAL_VISIBILITY_LIFECYCLE] status='Failed' message='{Escape(reported.Message)}'.");
                throw reported;
            }

            Debug.Log(
                "[QA_ACTIVITY_LOCAL_VISIBILITY_LIFECYCLE] " +
                "status='Passed' cases='17' " +
                "completed='positive-single,positive-multiple,negative-single,negative-multiple,no-active-visible,invalid-no-mutation,clear,idempotence'");
        }

        private static async Task RunPositiveSingleCases(
            IActivityRuntimePort activities,
            Fixture fixture,
            ActivityAsset activityA,
            ActivityAsset activityB)
        {
            await RequireClearAsync(activities, "positive-single-reset");
            fixture.Reset();

            await RequireActivityAsync(activities, activityA, "positive-single-no-active-to-a");
            AssertProbe(fixture.PositiveSingleProbe, 1, 0, activityA, "Enter", 0, "Case 1");
            Require(fixture.PositiveSingle.gameObject.activeSelf, "Case 1 must leave positive single active.");

            await RequireActivityAsync(activities, activityB, "positive-single-a-to-b");
            AssertProbe(fixture.PositiveSingleProbe, 1, 1, activityA, "Exit", 1, "Case 2");
            Require(!fixture.PositiveSingle.gameObject.activeSelf, "Case 2 must leave positive single inactive.");

            await RequireActivityAsync(activities, activityA, "positive-single-b-to-a");
            AssertProbe(fixture.PositiveSingleProbe, 2, 1, activityA, "Enter", 2, "Case 3");
            Require(fixture.PositiveSingle.gameObject.activeSelf, "Case 3 must reenter positive single.");
        }

        private static async Task RunPositiveMultipleCases(
            IActivityRuntimePort activities,
            Fixture fixture,
            ActivityAsset activityA,
            ActivityAsset activityB,
            ActivityAsset activityC)
        {
            await RequireClearAsync(activities, "positive-multiple-reset");
            fixture.Reset();

            await RequireActivityAsync(activities, activityA, "positive-multiple-no-active-to-a");
            AssertProbe(fixture.PositiveMultipleProbe, 1, 0, activityA, "Enter", 0, "Case 4");
            Require(fixture.PositiveMultiple.gameObject.activeSelf, "Case 4 must leave positive multiple active.");

            await RequireActivityAsync(activities, activityB, "positive-multiple-a-to-b");
            AssertProbe(fixture.PositiveMultipleProbe, 2, 1, activityB, "Enter", 2, "Case 5");
            AssertCallback(fixture.PositiveMultipleProbe, 1, "Exit", activityA, "Case 5 exit");
            Require(fixture.PositiveMultiple.gameObject.activeSelf, "Case 5 must preserve activeSelf.");

            await RequireActivityAsync(activities, activityC, "positive-multiple-b-to-c");
            AssertProbe(fixture.PositiveMultipleProbe, 2, 2, activityB, "Exit", 3, "Case 6");
            Require(!fixture.PositiveMultiple.gameObject.activeSelf, "Case 6 must leave positive multiple inactive.");

            await RequireActivityAsync(activities, activityA, "positive-multiple-c-to-a");
            AssertProbe(fixture.PositiveMultipleProbe, 3, 2, activityA, "Enter", 4, "Case 7");
            Require(fixture.PositiveMultiple.gameObject.activeSelf, "Case 7 must reenter positive multiple.");
        }

        private static async Task RunNegativeSingleCases(
            IActivityRuntimePort activities,
            Fixture fixture,
            ActivityAsset activityA,
            ActivityAsset activityB)
        {
            await RequireClearAsync(activities, "negative-single-reset");
            fixture.Reset();

            await RequireActivityAsync(activities, activityB, "negative-single-no-active-to-b");
            AssertProbe(fixture.NegativeSingleProbe, 1, 0, activityB, "Enter", 0, "Case 8");
            Require(fixture.NegativeSingle.gameObject.activeSelf, "Case 8 must activate negative single for an unlisted Activity.");

            await RequireActivityAsync(activities, activityA, "negative-single-b-to-a");
            AssertProbe(fixture.NegativeSingleProbe, 1, 1, activityB, "Exit", 1, "Case 9");
            Require(!fixture.NegativeSingle.gameObject.activeSelf, "Case 9 must hide negative single for its listed Activity.");
        }

        private static async Task RunNegativeMultipleCases(
            IActivityRuntimePort activities,
            Fixture fixture,
            ActivityAsset activityA,
            ActivityAsset activityB,
            ActivityAsset activityC)
        {
            await RequireClearAsync(activities, "negative-multiple-reset");
            fixture.Reset();

            await RequireActivityAsync(activities, activityC, "negative-multiple-no-active-to-c");
            AssertProbe(fixture.NegativeMultipleProbe, 1, 0, activityC, "Enter", 0, "Case 10");
            Require(fixture.NegativeMultiple.gameObject.activeSelf, "Case 10 must activate negative multiple for C.");

            await RequireActivityAsync(activities, activityA, "negative-multiple-c-to-a");
            AssertProbe(fixture.NegativeMultipleProbe, 1, 1, activityC, "Exit", 1, "Case 11");
            Require(!fixture.NegativeMultiple.gameObject.activeSelf, "Case 11 must hide negative multiple for A.");

            await RequireActivityAsync(activities, activityB, "negative-multiple-a-to-b");
            AssertProbe(fixture.NegativeMultipleProbe, 1, 1, activityC, "Exit", 1, "Case 12");
            Require(!fixture.NegativeMultiple.gameObject.activeSelf, "Case 12 must preserve negative multiple hidden without callbacks.");
        }

        private static async Task RunNoActiveVisibleCases(
            IActivityRuntimePort activities,
            Fixture fixture,
            ActivityAsset activityA,
            ActivityAsset activityB)
        {
            await RequireClearAsync(activities, "no-active-visible-reset");
            fixture.Reset();

            await RequireActivityAsync(activities, activityB, "no-active-visible-to-b");
            Require(!fixture.NoActiveVisible.gameObject.activeSelf, "Case 13 precondition must hide No Active Visible for B.");
            Require(fixture.NoActiveVisibleProbe.Callbacks.Count == 0, "Case 13 precondition must not dispatch callbacks.");

            await RequireClearAsync(activities, "no-active-visible-clear");
            Require(fixture.NoActiveVisible.gameObject.activeSelf, "Case 13 must show No Active Visible after Clear.");
            Require(fixture.NoActiveVisibleProbe.Callbacks.Count == 0, "Case 13 must not dispatch lifecycle without an active Activity.");

            await RequireActivityAsync(activities, activityA, "no-active-visible-clear-to-a");
            Require(fixture.NoActiveVisible.gameObject.activeSelf, "Case 14 must preserve No Active Visible active for A.");
            Require(fixture.NoActiveVisibleProbe.Callbacks.Count == 1, "Case 14 must dispatch exactly one Enter for A.");
            AssertCallback(fixture.NoActiveVisibleProbe, 0, "Enter", activityA, "Case 14");
        }

        private static async Task RunInvalidRuleCase(
            IActivityRuntimePort activities,
            Scene scene,
            ActivityAsset activityC)
        {
            (ActivityLocalVisibilityAdapter invalid, QaActivityLocalVisibilityLifecycleProbe probe) =
                Fixture.CreateInvalidBinding(scene);
            try
            {
                bool activeBefore = invalid.gameObject.activeSelf;
                int enterBefore = probe.EnterCount;
                int exitBefore = probe.ExitCount;

                FrameworkActivityRequestResult request = await RequestActivityAsync(
                    activities,
                    activityC,
                    "invalid-rule-no-mutation");
                ActivityContentApplyResult content = request.ActivityFlowResult.ActivityContentResult;
                Require(content.InvalidBindingCount == 1,
                    "Case 15 must report exactly one invalid Activity Local Visibility Adapter binding.");
                Require(content.HasWarningMessage,
                    "Case 15 must report the invalid binding through the official warning result.");
                Require(content.WarningMessage.Contains(invalid.gameObject.name) &&
                        content.WarningMessage.Contains("CurrentActivitiesEmpty"),
                    "Case 15 warning must identify the temporary invalid root and CurrentActivitiesEmpty.");
                Require(!content.LifecycleResult.HasFailures,
                    "Case 15 invalid binding must not cause lifecycle failures.");
                Require(invalid.gameObject.activeSelf == activeBefore,
                    "Case 15 invalid rule must not mutate activeSelf.");
                Require(probe.EnterCount == enterBefore && probe.ExitCount == exitBefore && probe.Callbacks.Count == 0,
                    "Case 15 invalid rule must not dispatch lifecycle callbacks.");
            }
            finally
            {
                if (invalid != null)
                {
                    UnityEngine.Object.DestroyImmediate(invalid.gameObject);
                }
            }
        }

        private static async Task RunClearAndIdempotenceCases(
            FrameworkRuntimeHost host,
            IActivityRuntimePort activities,
            Fixture fixture,
            ActivityAsset activityA)
        {
            await RequireClearAsync(activities, "clear-idempotence-reset");
            fixture.Reset();

            await RequireActivityAsync(activities, activityA, "clear-from-a-setup");
            AssertProbe(fixture.PositiveSingleProbe, 1, 0, activityA, "Enter", 0, "Case 16 setup");
            await RequireClearAsync(activities, "clear-from-a");
            AssertProbe(fixture.PositiveSingleProbe, 1, 1, activityA, "Exit", 1, "Case 16");
            Require(!fixture.PositiveSingle.gameObject.activeSelf, "Case 16 must hide positive single after Clear.");
            Require(host.State.CurrentActivity == null, "Case 16 first Clear must leave no active Activity.");

            bool clearActiveSelf = fixture.PositiveSingle.gameObject.activeSelf;
            int clearEnterCount = fixture.PositiveSingleProbe.EnterCount;
            int clearExitCount = fixture.PositiveSingleProbe.ExitCount;

            FrameworkActivityRequestResult repeatedClear = await activities.ClearActivityAsync(
                nameof(QaActivityLocalVisibilityLifecycleRegression),
                "clear-idempotence");
            Require(repeatedClear.Kind == FrameworkActivityRequestKind.IgnoredNoActiveActivity,
                "Case 16 repeated Clear must be ignored because no Activity is active.");
            Require(!repeatedClear.Succeeded, "Case 16 repeated Clear must not succeed.");
            Require(repeatedClear.TargetActivity == null, "Case 16 repeated Clear target must be null.");
            Require(repeatedClear.Reason == "clear-idempotence", "Case 16 repeated Clear reason diverged.");
            Require(host.State.CurrentActivity == null, "Case 16 repeated Clear must preserve no active Activity.");
            Require(fixture.PositiveSingle.gameObject.activeSelf == clearActiveSelf,
                "Case 16 repeated Clear must preserve activeSelf.");
            Require(fixture.PositiveSingleProbe.EnterCount == clearEnterCount &&
                    fixture.PositiveSingleProbe.ExitCount == clearExitCount,
                "Case 16 repeated Clear must not dispatch lifecycle callbacks.");

            await RequireActivityAsync(activities, activityA, "activity-idempotence-first");
            AssertProbe(fixture.PositiveSingleProbe, 2, 1, activityA, "Enter", 2, "Case 17 setup");
            Require(host.State.CurrentActivity != null && host.State.CurrentActivity.HasSameIdentity(activityA),
                "Case 17 setup must leave Activity A active.");

            bool activityActiveSelf = fixture.PositiveSingle.gameObject.activeSelf;
            int activityEnterCount = fixture.PositiveSingleProbe.EnterCount;
            int activityExitCount = fixture.PositiveSingleProbe.ExitCount;
            const string repeatedActivityReason = "activity-idempotence-repeat";
            FrameworkActivityRequestResult repeatedActivity = await activities.RequestActivityAsync(
                activityA,
                nameof(QaActivityLocalVisibilityLifecycleRegression),
                repeatedActivityReason);
            Require(repeatedActivity.Kind == FrameworkActivityRequestKind.IgnoredAlreadyActive,
                "Case 17 repeated Activity request must be ignored because A is already active.");
            Require(!repeatedActivity.Succeeded, "Case 17 repeated Activity request must not succeed.");
            Require(repeatedActivity.TargetActivity != null && repeatedActivity.TargetActivity.HasSameIdentity(activityA),
                "Case 17 repeated Activity target must be A.");
            Require(repeatedActivity.Reason == repeatedActivityReason,
                "Case 17 repeated Activity reason diverged.");
            Require(host.State.CurrentActivity != null && host.State.CurrentActivity.HasSameIdentity(activityA),
                "Case 17 repeated Activity request must preserve Activity A.");
            Require(fixture.PositiveSingle.gameObject.activeSelf == activityActiveSelf,
                "Case 17 repeated Activity request must preserve activeSelf.");
            Require(fixture.PositiveSingleProbe.EnterCount == activityEnterCount &&
                    fixture.PositiveSingleProbe.ExitCount == activityExitCount,
                "Case 17 repeated Activity request must not dispatch lifecycle callbacks.");
        }

        private static async Task RequireRouteAsync(IRouteRuntimePort routes, RouteAsset route, string reason)
        {
            FrameworkRouteRequestResult result = await routes.RequestRouteAsync(
                route,
                nameof(QaActivityLocalVisibilityLifecycleRegression),
                reason);
            Require(result.Succeeded, result.Message);
        }

        private static async Task RequireActivityAsync(IActivityRuntimePort activities, ActivityAsset activity, string reason)
        {
            await RequestActivityAsync(activities, activity, reason);
        }

        private static async Task<FrameworkActivityRequestResult> RequestActivityAsync(
            IActivityRuntimePort activities,
            ActivityAsset activity,
            string reason)
        {
            FrameworkActivityRequestResult result = await activities.RequestActivityAsync(
                activity,
                nameof(QaActivityLocalVisibilityLifecycleRegression),
                reason);
            Require(result.Succeeded, result.Message);
            return result;
        }

        private static async Task RequireClearAsync(IActivityRuntimePort activities, string reason)
        {
            FrameworkActivityRequestResult result = await activities.ClearActivityAsync(
                nameof(QaActivityLocalVisibilityLifecycleRegression),
                reason);
            Require(result.Succeeded, result.Message);
        }

        private static void AssertProbe(QaActivityLocalVisibilityLifecycleProbe probe, int enters, int exits, ActivityAsset activity, string callback, int callbackIndex, string label)
        {
            Require(probe.EnterCount == enters && probe.ExitCount == exits && probe.LastActivity == activity, label + " counts or Activity diverged.");
            AssertCallback(probe, callbackIndex, callback, activity, label);
        }

        private static void AssertCallback(QaActivityLocalVisibilityLifecycleProbe probe, int index, string callback, ActivityAsset activity, string label)
        {
            Require(probe.Callbacks.Count > index, label + " callback is missing.");
            CallbackRecord record = probe.Callbacks[index];
            Require(record.Callback == callback && record.Activity == activity && record.ActiveSelfBefore && record.ActiveSelfAfter, label + " callback evidence diverged.");
        }

        private static T Load<T>(string path) where T : UnityEngine.Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing QA asset: " + path);
        private static void DestroyRoots(IReadOnlyList<GameObject> roots) { for (int index = roots.Count - 1; index >= 0; index--) if (roots[index] != null) UnityEngine.Object.DestroyImmediate(roots[index]); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static string Escape(string value) => string.IsNullOrEmpty(value) ? string.Empty : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");

        private sealed class Fixture
        {
            private readonly IReadOnlyList<QaActivityLocalVisibilityLifecycleProbe> _probes;
            private Fixture(
                ActivityLocalVisibilityAdapter positiveSingle,
                ActivityLocalVisibilityAdapter positiveMultiple,
                ActivityLocalVisibilityAdapter negativeSingle,
                ActivityLocalVisibilityAdapter negativeMultiple,
                ActivityLocalVisibilityAdapter noActiveVisible,
                QaActivityLocalVisibilityLifecycleProbe positiveSingleProbe,
                QaActivityLocalVisibilityLifecycleProbe positiveMultipleProbe,
                QaActivityLocalVisibilityLifecycleProbe negativeSingleProbe,
                QaActivityLocalVisibilityLifecycleProbe negativeMultipleProbe,
                QaActivityLocalVisibilityLifecycleProbe noActiveVisibleProbe)
            {
                PositiveSingle = positiveSingle;
                PositiveMultiple = positiveMultiple;
                NegativeSingle = negativeSingle;
                NegativeMultiple = negativeMultiple;
                NoActiveVisible = noActiveVisible;
                PositiveSingleProbe = positiveSingleProbe;
                PositiveMultipleProbe = positiveMultipleProbe;
                NegativeSingleProbe = negativeSingleProbe;
                NegativeMultipleProbe = negativeMultipleProbe;
                NoActiveVisibleProbe = noActiveVisibleProbe;
                _probes = new[] { positiveSingleProbe, positiveMultipleProbe, negativeSingleProbe, negativeMultipleProbe, noActiveVisibleProbe };
            }
            public ActivityLocalVisibilityAdapter PositiveSingle { get; }
            public ActivityLocalVisibilityAdapter PositiveMultiple { get; }
            public ActivityLocalVisibilityAdapter NegativeSingle { get; }
            public ActivityLocalVisibilityAdapter NegativeMultiple { get; }
            public ActivityLocalVisibilityAdapter NoActiveVisible { get; }
            public QaActivityLocalVisibilityLifecycleProbe PositiveSingleProbe { get; }
            public QaActivityLocalVisibilityLifecycleProbe PositiveMultipleProbe { get; }
            public QaActivityLocalVisibilityLifecycleProbe NegativeSingleProbe { get; }
            public QaActivityLocalVisibilityLifecycleProbe NegativeMultipleProbe { get; }
            public QaActivityLocalVisibilityLifecycleProbe NoActiveVisibleProbe { get; }
            public static Fixture Create(Scene scene, ActivityAsset activityA, ActivityAsset activityB, ICollection<GameObject> roots)
            {
                (ActivityLocalVisibilityAdapter single, QaActivityLocalVisibilityLifecycleProbe singleProbe) = CreateBinding(scene, roots, new[] { activityA }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, "qa.lifecycle.lifecycle-single");
                (ActivityLocalVisibilityAdapter multiple, QaActivityLocalVisibilityLifecycleProbe multipleProbe) = CreateBinding(scene, roots, new[] { activityA, activityB }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, "qa.lifecycle.lifecycle-multiple");
                (ActivityLocalVisibilityAdapter negativeSingle, QaActivityLocalVisibilityLifecycleProbe negativeSingleProbe) = CreateBinding(scene, roots, new[] { activityA }, ActivityVisibilityMatchMode.HiddenWhenAnyListedActivityIsActive, "qa.lifecycle.lifecycle-negative-single");
                (ActivityLocalVisibilityAdapter negativeMultiple, QaActivityLocalVisibilityLifecycleProbe negativeMultipleProbe) = CreateBinding(scene, roots, new[] { activityA, activityB }, ActivityVisibilityMatchMode.HiddenWhenAnyListedActivityIsActive, "qa.lifecycle.lifecycle-negative-multiple");
                (ActivityLocalVisibilityAdapter noActiveVisible, QaActivityLocalVisibilityLifecycleProbe noActiveVisibleProbe) = CreateBinding(scene, roots, new[] { activityA }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, "qa.lifecycle.lifecycle-no-active-visible", ActivityVisibilityNoActivePolicy.Visible);
                return new Fixture(single, multiple, negativeSingle, negativeMultiple, noActiveVisible, singleProbe, multipleProbe, negativeSingleProbe, negativeMultipleProbe, noActiveVisibleProbe);
            }

            public static (ActivityLocalVisibilityAdapter, QaActivityLocalVisibilityLifecycleProbe) CreateInvalidBinding(Scene scene)
            {
                return CreateBinding(
                    scene,
                    null,
                    Array.Empty<ActivityAsset>(),
                    ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive,
                    "qa.lifecycle.lifecycle-invalid",
                    ActivityVisibilityNoActivePolicy.Hidden,
                    allowInvalid: true,
                    rootName: "QA Activity Local Visibility Lifecycle Invalid Root");
            }

            public void Reset() { for (int index = 0; index < _probes.Count; index++) _probes[index].ResetCounters(); }
            private static (ActivityLocalVisibilityAdapter, QaActivityLocalVisibilityLifecycleProbe) CreateBinding(Scene scene, ICollection<GameObject> roots, ActivityAsset[] activities, ActivityVisibilityMatchMode mode, string id, ActivityVisibilityNoActivePolicy policy = ActivityVisibilityNoActivePolicy.Hidden, bool allowInvalid = false, string rootName = "QA Activity Local Visibility Lifecycle Temporary Root")
            {
                GameObject root = new GameObject(rootName); SceneManager.MoveGameObjectToScene(root, scene); roots?.Add(root);
                ActivityLocalVisibilityAdapter adapter = root.AddComponent<ActivityLocalVisibilityAdapter>(); QaActivityLocalVisibilityLifecycleProbe probe = root.AddComponent<QaActivityLocalVisibilityLifecycleProbe>();
                Require(activities != null && (allowInvalid || activities.Length > 0), "Lifecycle fixture requires one or more Activities.");
                SerializedObject serialized = new SerializedObject(adapter);
                SerializedProperty list = serialized.FindProperty("activities");
                Require(list != null && list.isArray, "ActivityLocalVisibilityAdapter activities property is unavailable.");
                list.arraySize = activities.Length;
                for (int index = 0; index < activities.Length; index++)
                {
                    Require(activities[index] != null, $"Lifecycle fixture Activity at index {index} is null.");
                    list.GetArrayElementAtIndex(index).objectReferenceValue = activities[index];
                }

                serialized.FindProperty("matchMode").intValue = (int)mode;
                serialized.FindProperty("noActiveActivityPolicy").intValue = (int)policy;
                serialized.FindProperty("localContentId").stringValue = id;
                serialized.ApplyModifiedProperties();

                SerializedObject applied = new SerializedObject(adapter);
                SerializedProperty appliedList = applied.FindProperty("activities");
                Require(appliedList != null && appliedList.arraySize == activities.Length,
                    "Lifecycle fixture activities array size was not applied.");
                for (int index = 0; index < appliedList.arraySize; index++)
                {
                    Require(appliedList.GetArrayElementAtIndex(index).objectReferenceValue != null,
                        $"Lifecycle fixture serialized Activity at index {index} is null after apply.");
                }

                Require(allowInvalid == !adapter.EvaluateVisibility(null).IsValid,
                    "Lifecycle fixture rule validity diverged from its requested contract.");

                return (adapter, probe);
            }
        }
    }

    public sealed class QaActivityLocalVisibilityLifecycleProbe : ActivityContentBehaviour
    {
        private readonly List<CallbackRecord> _callbacks = new List<CallbackRecord>();
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }
        public ActivityAsset LastActivity { get; private set; }
        public IReadOnlyList<CallbackRecord> Callbacks => _callbacks;
        public void ResetCounters() { EnterCount = 0; ExitCount = 0; LastActivity = null; _callbacks.Clear(); }
        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context) { Record(context, "Enter"); EnterCount++; }
        protected override void OnActivityContentExited(ActivityContentLifecycleContext context) { Record(context, "Exit"); ExitCount++; }
        private void Record(ActivityContentLifecycleContext context, string callback) { bool before = gameObject.activeSelf; LastActivity = context.Activity; _callbacks.Add(new CallbackRecord(callback, context.Activity, before, gameObject.activeSelf)); }
    }

    public readonly struct CallbackRecord
    {
        public CallbackRecord(string callback, ActivityAsset activity, bool activeSelfBefore, bool activeSelfAfter) { Callback = callback; Activity = activity; ActiveSelfBefore = activeSelfBefore; ActiveSelfAfter = activeSelfAfter; }
        public string Callback { get; }
        public ActivityAsset Activity { get; }
        public bool ActiveSelfBefore { get; }
        public bool ActiveSelfAfter { get; }
    }
}

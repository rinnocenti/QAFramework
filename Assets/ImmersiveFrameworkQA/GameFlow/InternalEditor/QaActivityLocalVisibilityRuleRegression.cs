using System;
using System.Reflection;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.ActivityFlow.Editor
{
    public static class QaActivityLocalVisibilityRuleRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Activity Flow/Run Activity Local Visibility Rule Regression";
        private const string ActivityAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityA.asset";
        private const string ActivityBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityB.asset";
        private const string ActivityCPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleNoContentActivity.asset";
        private static readonly FieldInfo ActivitiesField = typeof(ActivityLocalVisibilityAdapter)
            .GetField("activities", BindingFlags.Instance | BindingFlags.NonPublic);

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            ActivityAsset a = Load(ActivityAPath);
            ActivityAsset b = Load(ActivityBPath);
            ActivityAsset c = Load(ActivityCPath);
            ActivityAsset sameIdentityAsA = CreateSameIdentityActivity(a);
            GameObject root = new GameObject("QA Activity Local Visibility Rule");
            try
            {
                var adapter = root.AddComponent<ActivityLocalVisibilityAdapter>();
                int cases = 0;
                Configure(adapter, new[] { a }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, ActivityVisibilityNoActivePolicy.Hidden);
                Check(adapter, a, true, true, "positive-single-a", ref cases); Check(adapter, b, true, false, "positive-single-b", ref cases); Check(adapter, c, true, false, "positive-single-c", ref cases);
                CheckSingleOwner(adapter, true, a, "single-owner-positive-single", ref cases);
                Configure(adapter, new[] { a, b },
                    ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive,
                    ActivityVisibilityNoActivePolicy.Visible);
                Check(adapter, a, true, true, "positive-any-a", ref cases); Check(adapter, b, true, true, "positive-any-b", ref cases); Check(adapter, c, true, false, "positive-any-c", ref cases); Check(adapter, null, true, true, "positive-no-active-visible", ref cases);
                CheckSingleOwner(adapter, false, null, "single-owner-positive-multiple", ref cases);
                Configure(adapter, new[] { a, b },
                    ActivityVisibilityMatchMode.HiddenWhenAnyListedActivityIsActive,
                    ActivityVisibilityNoActivePolicy.Hidden);
                Check(adapter, a, true, false, "negative-any-a", ref cases); Check(adapter, b, true, false, "negative-any-b", ref cases); Check(adapter, c, true, true, "negative-any-c", ref cases); Check(adapter, null, true, false, "negative-no-active-hidden", ref cases);
                Configure(adapter, new[] { a }, ActivityVisibilityMatchMode.HiddenWhenAnyListedActivityIsActive, ActivityVisibilityNoActivePolicy.Visible);
                Check(adapter, a, true, false, "negative-single-a", ref cases); Check(adapter, b, true, true, "negative-single-b", ref cases); Check(adapter, null, true, true, "negative-no-active-visible", ref cases);
                CheckSingleOwner(adapter, false, null, "single-owner-negative", ref cases);
                Configure(adapter, new[] { a }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, ActivityVisibilityNoActivePolicy.Visible);
                CheckSingleOwner(adapter, false, null, "single-owner-no-active-visible", ref cases);
                Configure(adapter, Array.Empty<ActivityAsset>(),
                    ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive,
                    ActivityVisibilityNoActivePolicy.Hidden);
                CheckInvalid(adapter, a, "empty-invalid", ref cases);
                Configure(adapter, new ActivityAsset[] { null }, (ActivityVisibilityMatchMode)99, (ActivityVisibilityNoActivePolicy)99);
                CheckInvalid(adapter, a, "null-invalid", ref cases);
                Configure(adapter, new[] { a }, (ActivityVisibilityMatchMode)99, ActivityVisibilityNoActivePolicy.Hidden);
                CheckInvalid(adapter, a, "match-mode-invalid", ref cases);
                Configure(adapter, new[] { a }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, (ActivityVisibilityNoActivePolicy)99);
                CheckInvalid(adapter, a, "no-active-policy-invalid", ref cases);
                Configure(adapter, null, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, ActivityVisibilityNoActivePolicy.Hidden);
                CheckInvalid(adapter, a, "activities-null-invalid", ref cases);
                Configure(adapter, new[] { a, a }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, ActivityVisibilityNoActivePolicy.Hidden);
                CheckInvalid(adapter, a, "duplicate-invalid", ref cases);
                CheckSingleOwner(adapter, false, null, "single-owner-invalid", ref cases);
                Configure(adapter, new[] { a, sameIdentityAsA }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, ActivityVisibilityNoActivePolicy.Hidden);
                CheckInvalid(adapter, a, "duplicate-canonical-identity-invalid", ref cases);
                Configure(adapter, new[] { a }, ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive, ActivityVisibilityNoActivePolicy.Hidden, string.Empty);
                CheckInvalid(adapter, a, "local-content-id-empty-invalid", ref cases);
                var first=adapter.EvaluateVisibility(a); var second=adapter.EvaluateVisibility(a); Assert(first.IsValid==second.IsValid && first.DesiredVisibility==second.DesiredVisibility, "repeated-evaluation"); cases++;
                Debug.Log($"[QA_ACTIVITY_LOCAL_VISIBILITY_RULE] status='Passed' cases='{cases}' completed='positive,negative,no-active,invalid,idempotent,single-owner'.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(sameIdentityAsA); }
        }

        private static ActivityAsset Load(string path) => AssetDatabase.LoadAssetAtPath<ActivityAsset>(path) ?? throw new InvalidOperationException("Missing canonical Activity: " + path);
        private static ActivityAsset CreateSameIdentityActivity(ActivityAsset source) { var copy=ScriptableObject.CreateInstance<ActivityAsset>(); var from=new SerializedObject(source); var to=new SerializedObject(copy); to.FindProperty("activityId").stringValue=from.FindProperty("activityId").stringValue; to.FindProperty("activityName").stringValue="QA Same Identity Activity"; to.ApplyModifiedPropertiesWithoutUndo(); return copy; }
        private static void Configure(ActivityLocalVisibilityAdapter adapter, ActivityAsset[] activities, ActivityVisibilityMatchMode mode, ActivityVisibilityNoActivePolicy policy, string localContentId = "qa.activity.visibility")
        {
            var serialized = new SerializedObject(adapter);
            SerializedProperty list = serialized.FindProperty("activities");
            if (activities == null)
            {
                list.arraySize = 0;
            }
            else
            {
                list.arraySize = activities.Length;
                for (int i = 0; i < activities.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = activities[i];
            }
            serialized.FindProperty("matchMode").intValue = (int)mode;
            serialized.FindProperty("noActiveActivityPolicy").intValue = (int)policy;
            serialized.FindProperty("localContentId").stringValue = localContentId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (activities == null)
            {
                ActivitiesField.SetValue(adapter, null);
            }
        }
        private static void Assert(bool condition, string name) { if (!condition) throw new InvalidOperationException("Rule regression failed: " + name); }
        private static void Check(ActivityLocalVisibilityAdapter adapter, ActivityAsset activity, bool valid, bool visible, string name, ref int cases) { var result=adapter.EvaluateVisibility(activity); Assert(result.IsValid==valid && result.DesiredVisibility==visible,name); cases++; }
        private static void CheckInvalid(ActivityLocalVisibilityAdapter adapter, ActivityAsset activity, string name, ref int cases) { bool state=adapter.gameObject.activeSelf; string before=EditorJsonUtility.ToJson(adapter); var result=adapter.EvaluateVisibility(activity); Assert(!result.IsValid && !string.IsNullOrWhiteSpace(result.DiagnosticReason) && state==adapter.gameObject.activeSelf && before==EditorJsonUtility.ToJson(adapter),name); cases++; }
        private static void CheckSingleOwner(ActivityLocalVisibilityAdapter adapter, bool expected, ActivityAsset expectedActivity, string name, ref int cases) { bool actual=adapter.TryGetSingleActivityOwner(out ActivityAsset owner); Assert(actual==expected && owner==expectedActivity,name); cases++; }
    }
}

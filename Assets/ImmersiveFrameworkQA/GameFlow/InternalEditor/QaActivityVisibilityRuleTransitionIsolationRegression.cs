using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaActivityVisibilityRuleTransitionIsolationRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Activity Flow/Run Activity Visibility Rule Transition Isolation Regression";
        private const string LogPrefix = "[QA_ACTIVITY_VISIBILITY_RULE_ISOLATION]";
        private static readonly string[] ExpectedCases =
        {
            "invalid-rule-diagnostic-nonmutating-nonblocking",
            "valid-rule-presentation-without-activity-content"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            var completed = new List<string>();
            QaAdr009ActivityFlowScope scope = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;

            try
            {
                scope = new QaAdr009ActivityFlowScope(nameof(QaActivityVisibilityRuleTransitionIsolationRegression));
                await scope.InitializeAsync();
                await RunInvalidRuleCaseAsync(scope);
                completed.Add(ExpectedCases[0]);
                await RunValidRuleCaseAsync(scope);
                completed.Add(ExpectedCases[1]);
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                if (scope != null)
                {
                    try
                    {
                        await scope.RestoreAsync();
                    }
                    catch (Exception exception)
                    {
                        cleanupFailure = exception;
                    }
                }
            }

            Exception failure = Combine(executionFailure, cleanupFailure);
            EmitFinalReport(completed, executionFailure, cleanupFailure);
            if (failure != null)
            {
                throw failure;
            }
        }

        private static async Task RunInvalidRuleCaseAsync(QaAdr009ActivityFlowScope scope)
        {
            GameObject root = scope.CreateTemporaryRoot("QA ADR009 Invalid Visibility Rule");
            try
            {
                ActivityVisibilityRule rule = root.AddComponent<ActivityVisibilityRule>();
                ConfigureRule(rule, Array.Empty<ActivityAsset>(),
                    ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive,
                    ActivityVisibilityNoActivePolicy.Hidden);
                QaAdr009ActivityFlowScope.Require(root.GetComponent<ActivityContentContribution>() == null,
                    "Invalid Rule case must not add an ActivityContentContribution.");
                bool activeBefore = root.activeSelf;

                FrameworkActivityRequestResult result = await scope.Activities.RequestActivityAsync(
                    scope.ActivityC,
                    nameof(QaActivityVisibilityRuleTransitionIsolationRegression),
                    "invalid-rule-diagnostic-nonmutating-nonblocking");
                ActivityContentApplyResult content = result.ActivityFlowResult.ActivityContentResult;

                QaAdr009ActivityFlowScope.Require(result.Succeeded && result.DestinationAuthoritative,
                    "Invalid ActivityVisibilityRule must not block the Activity transition.");
                QaAdr009ActivityFlowScope.Require(ReferenceEquals(scope.Host.State.CurrentActivity, scope.ActivityC),
                    "Invalid ActivityVisibilityRule must allow the target Activity to become canonical.");
                QaAdr009ActivityFlowScope.Require(
                    content.InvalidBindingCount == 1 &&
                    content.RequiredInvalidBindingCount == 0 &&
                    content.OptionalInvalidBindingCount == 0 &&
                    content.HasWarningMessage &&
                    content.WarningMessage.Contains(root.name) &&
                    content.WarningMessage.Contains("CurrentActivitiesEmpty"),
                    "Invalid ActivityVisibilityRule must produce visibility-only diagnostic evidence.");
                QaAdr009ActivityFlowScope.Require(root.activeSelf == activeBefore,
                    "Invalid ActivityVisibilityRule must not mutate its GameObject activeSelf.");
                QaAdr009ActivityFlowScope.Require(content.ActivityContentCount == 0,
                    "Invalid ActivityVisibilityRule must not register Activity-owned content.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static async Task RunValidRuleCaseAsync(QaAdr009ActivityFlowScope scope)
        {
            await scope.EnsureActivityAsync(scope.ActivityA, "valid-rule-baseline");
            GameObject root = scope.CreateTemporaryRoot("QA ADR009 Valid Visibility Rule");
            try
            {
                ActivityVisibilityRule rule = root.AddComponent<ActivityVisibilityRule>();
                ConfigureRule(rule, new[] { scope.ActivityC },
                    ActivityVisibilityMatchMode.VisibleWhenAnyListedActivityIsActive,
                    ActivityVisibilityNoActivePolicy.Hidden);
                QaAdr009ActivityFlowScope.Require(root.GetComponent<ActivityContentContribution>() == null,
                    "Valid Rule case must not add an ActivityContentContribution.");
                QaAdr009ActivityFlowScope.Require(root.activeSelf,
                    "Valid Rule case requires an initially visible GameObject.");

                FrameworkActivityRequestResult enterTarget = await scope.Activities.RequestActivityAsync(
                    scope.ActivityC,
                    nameof(QaActivityVisibilityRuleTransitionIsolationRegression),
                    "valid-rule-enter-target");
                ActivityContentApplyResult targetContent = enterTarget.ActivityFlowResult.ActivityContentResult;
                QaAdr009ActivityFlowScope.Require(enterTarget.Succeeded && enterTarget.DestinationAuthoritative && root.activeSelf,
                    "Valid ActivityVisibilityRule must preserve visibility for its listed Activity.");
                QaAdr009ActivityFlowScope.Require(targetContent.InvalidBindingCount == 0 &&
                    targetContent.ActivityContentCount == 0,
                    "Valid ActivityVisibilityRule must not create invalid or Activity-owned content evidence.");

                await scope.EnsureActivityAsync(scope.ActivityA, "valid-rule-switch-away");
                QaAdr009ActivityFlowScope.Require(!root.activeSelf,
                    "Valid ActivityVisibilityRule must hide its GameObject for an unlisted Activity.");

                FrameworkActivityRequestResult reenterTarget = await scope.Activities.RequestActivityAsync(
                    scope.ActivityC,
                    nameof(QaActivityVisibilityRuleTransitionIsolationRegression),
                    "valid-rule-reenter-target");
                ActivityContentApplyResult reentryContent = reenterTarget.ActivityFlowResult.ActivityContentResult;
                QaAdr009ActivityFlowScope.Require(reenterTarget.Succeeded && reenterTarget.DestinationAuthoritative && root.activeSelf,
                    "Valid ActivityVisibilityRule must restore visibility when its listed Activity becomes canonical again.");
                QaAdr009ActivityFlowScope.Require(reentryContent.InvalidBindingCount == 0 &&
                    reentryContent.ActivityContentCount == 0,
                    "Visibility-only reentry must not register Activity-owned content.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureRule(
            ActivityVisibilityRule rule,
            ActivityAsset[] activities,
            ActivityVisibilityMatchMode matchMode,
            ActivityVisibilityNoActivePolicy noActivePolicy)
        {
            var serialized = new SerializedObject(rule);
            SerializedProperty list = serialized.FindProperty("activities");
            QaAdr009ActivityFlowScope.Require(list != null && list.isArray,
                "ActivityVisibilityRule activities property is unavailable.");
            list.arraySize = activities?.Length ?? 0;
            for (int index = 0; index < list.arraySize; index++)
            {
                list.GetArrayElementAtIndex(index).objectReferenceValue = activities[index];
            }

            serialized.FindProperty("matchMode").intValue = (int)matchMode;
            serialized.FindProperty("noActiveActivityPolicy").intValue = (int)noActivePolicy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Exception Combine(Exception executionFailure, Exception cleanupFailure)
        {
            if (executionFailure == null) return cleanupFailure;
            return cleanupFailure == null ? executionFailure : new AggregateException(executionFailure, cleanupFailure);
        }

        private static void EmitFinalReport(
            IReadOnlyList<string> completed,
            Exception executionFailure,
            Exception cleanupFailure)
        {
            string missing = completed.Count < ExpectedCases.Length
                ? ExpectedCases[completed.Count]
                : "<none>";
            string status = executionFailure == null && cleanupFailure == null && completed.Count == ExpectedCases.Length
                ? "Passed"
                : "Failed";
            string message =
                $"{LogPrefix} status='{status}' verdict='ADR009-VisibilityIsolation' " +
                $"cases='{completed.Count}/{ExpectedCases.Length}' next='{missing}' " +
                $"completed='{string.Join(",", completed)}' missing='{missing}' " +
                $"execution='{QaAdr009ActivityFlowScope.Escape(executionFailure?.Message)}' " +
                $"unwind='not-applicable' cleanup='{QaAdr009ActivityFlowScope.Escape(cleanupFailure?.Message)}'.";
            if (status == "Passed") Debug.Log(message); else Debug.LogError(message);
        }
    }
}

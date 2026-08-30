using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.GameFlow;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaActivityContentContributionTransitionAuthorityRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Activity Flow/Run Activity Content Contribution Transition Authority Regression";
        private const string LogPrefix = "[QA_ACTIVITY_CONTENT_CONTRIBUTION_AUTHORITY]";
        private static readonly string[] ExpectedCases =
        {
            "required-invalid-blocks-before-commit",
            "optional-invalid-diagnostic-nonblocking",
            "valid-contribution-registers-and-enters"
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
                scope = new QaAdr009ActivityFlowScope(nameof(QaActivityContentContributionTransitionAuthorityRegression));
                await scope.InitializeAsync();
                await RunRequiredInvalidCaseAsync(scope);
                completed.Add(ExpectedCases[0]);
                await RunOptionalInvalidCaseAsync(scope);
                completed.Add(ExpectedCases[1]);
                await RunValidContributionCaseAsync(scope);
                completed.Add(ExpectedCases[2]);
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

        private static async Task RunRequiredInvalidCaseAsync(QaAdr009ActivityFlowScope scope)
        {
            GameObject root = scope.CreateTemporaryRoot("QA ADR009 Required Invalid Contribution");
            try
            {
                ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
                ConfigureContribution(contribution, null, "qa.adr009.required.invalid", FrameworkContentRequiredness.Required);
                QaAdr009ActivityFlowScope.Require(root.GetComponent<ActivityVisibilityRule>() == null,
                    "Required Contribution case must not include an ActivityVisibilityRule.");
                ActivityAsset previous = scope.Host.State.CurrentActivity;
                QaAdr009ActivityFlowScope.Require(ReferenceEquals(previous, scope.ActivityA),
                    "Required Contribution case requires Activity A as the canonical previous Activity.");

                FrameworkActivityRequestResult result = await scope.Activities.RequestActivityAsync(
                    scope.ActivityC,
                    nameof(QaActivityContentContributionTransitionAuthorityRegression),
                    "required-invalid-contribution-blocks");
                ActivityContentApplyResult content = result.ActivityFlowResult.ActivityContentResult;

                QaAdr009ActivityFlowScope.Require(
                    result.Kind == FrameworkActivityRequestKind.FailedInvalidConfig,
                    "Required invalid Contribution must be classified as invalid configuration.");
                QaAdr009ActivityFlowScope.Require(
                    result.ActivityFlowResult.ActivityTransitionFailedBeforeCommit &&
                    !result.ActivityFlowResult.ActivityAuthorityCommitReached &&
                    !result.CommitBoundaryReached &&
                    !result.Succeeded &&
                    !result.DestinationAuthoritative,
                    "Required invalid Contribution must reject before the Activity commit boundary.");
                QaAdr009ActivityFlowScope.Require(
                    content.RequiredInvalidBindingCount == 1 &&
                    content.OptionalInvalidBindingCount == 0,
                    "Required invalid Contribution must be classified as Required only.");
                QaAdr009ActivityFlowScope.Require(ReferenceEquals(scope.Host.State.CurrentActivity, previous),
                    "Required invalid Contribution must preserve the previous canonical Activity.");
                QaAdr009ActivityFlowScope.Require(
                    result.Message.Contains("Required ActivityContentContribution configuration is invalid") &&
                    result.Message.Contains(root.name) &&
                    !result.Message.Contains("ActivityVisibilityRule"),
                    "Required invalid Contribution diagnostic must identify Contribution authority only.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static async Task RunOptionalInvalidCaseAsync(QaAdr009ActivityFlowScope scope)
        {
            await scope.EnsureActivityAsync(scope.ActivityA, "optional-invalid-contribution-baseline");
            GameObject root = scope.CreateTemporaryRoot("QA ADR009 Optional Invalid Contribution");
            try
            {
                ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
                ConfigureContribution(contribution, null, "qa.adr009.optional.invalid", FrameworkContentRequiredness.Optional);
                QaAdr009ActivityFlowScope.Require(root.GetComponent<ActivityVisibilityRule>() == null,
                    "Optional Contribution case must not include an ActivityVisibilityRule.");

                FrameworkActivityRequestResult result = await scope.Activities.RequestActivityAsync(
                    scope.ActivityC,
                    nameof(QaActivityContentContributionTransitionAuthorityRegression),
                    "optional-invalid-contribution-nonblocking");
                ActivityContentApplyResult content = result.ActivityFlowResult.ActivityContentResult;

                QaAdr009ActivityFlowScope.Require(result.Succeeded && result.DestinationAuthoritative,
                    "Optional invalid Contribution must not block the Activity transition.");
                QaAdr009ActivityFlowScope.Require(ReferenceEquals(scope.Host.State.CurrentActivity, scope.ActivityC),
                    "Optional invalid Contribution must commit the target Activity.");
                QaAdr009ActivityFlowScope.Require(
                    content.InvalidBindingCount == 1 &&
                    content.RequiredInvalidBindingCount == 0 &&
                    content.OptionalInvalidBindingCount == 1 &&
                    !content.HasRequiredInvalidBindings,
                    "Optional invalid Contribution must be classified only as Optional.");
                QaAdr009ActivityFlowScope.Require(
                    content.HasWarningMessage &&
                    content.WarningMessage.Contains(root.name) &&
                    content.WarningMessage.Contains("MissingActivity"),
                    "Optional invalid Contribution must retain its diagnostic evidence.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static async Task RunValidContributionCaseAsync(QaAdr009ActivityFlowScope scope)
        {
            await scope.EnsureActivityAsync(scope.ActivityA, "valid-contribution-baseline");
            GameObject root = scope.CreateTemporaryRoot("QA ADR009 Valid Contribution");
            try
            {
                ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
                ConfigureContribution(contribution, scope.ActivityC, "qa.adr009.valid.contribution", FrameworkContentRequiredness.Required);
                var probe = root.AddComponent<QaActivityLocalVisibilityLifecycleProbe>();
                QaAdr009ActivityFlowScope.Require(root.GetComponent<ActivityVisibilityRule>() == null,
                    "Valid Contribution case must not include an ActivityVisibilityRule.");

                FrameworkActivityRequestResult result = await scope.Activities.RequestActivityAsync(
                    scope.ActivityC,
                    nameof(QaActivityContentContributionTransitionAuthorityRegression),
                    "valid-contribution-registers-and-enters");
                ActivityContentApplyResult content = result.ActivityFlowResult.ActivityContentResult;

                QaAdr009ActivityFlowScope.Require(result.Succeeded && result.DestinationAuthoritative,
                    "Valid Contribution must allow the Activity transition.");
                QaAdr009ActivityFlowScope.Require(content.ActivityContentCount >= 1 &&
                    content.LifecycleResult.EnterBindingCount >= 1 &&
                    content.LifecycleResult.EnterReceiverCount >= 1,
                    "Valid Contribution must register Activity content and enter its lifecycle.");
                QaAdr009ActivityFlowScope.Require(probe.EnterCount == 1 &&
                    ReferenceEquals(probe.LastActivity, scope.ActivityC),
                    "Valid Contribution lifecycle probe did not receive the target Activity entry.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void ConfigureContribution(
            ActivityContentContribution contribution,
            ActivityAsset activity,
            string localContentId,
            FrameworkContentRequiredness requiredness)
        {
            var serialized = new SerializedObject(contribution);
            serialized.FindProperty("activity").objectReferenceValue = activity;
            serialized.FindProperty("localContentId").stringValue = localContentId;
            serialized.FindProperty("requiredness").intValue = (int)requiredness;
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
                $"{LogPrefix} status='{status}' verdict='ADR009-ContributionAuthority' " +
                $"cases='{completed.Count}/{ExpectedCases.Length}' next='{missing}' " +
                $"completed='{string.Join(",", completed)}' missing='{missing}' " +
                $"execution='{QaAdr009ActivityFlowScope.Escape(executionFailure?.Message)}' " +
                $"unwind='not-applicable' cleanup='{QaAdr009ActivityFlowScope.Escape(cleanupFailure?.Message)}'.";
            if (status == "Passed") Debug.Log(message); else Debug.LogError(message);
        }
    }
}

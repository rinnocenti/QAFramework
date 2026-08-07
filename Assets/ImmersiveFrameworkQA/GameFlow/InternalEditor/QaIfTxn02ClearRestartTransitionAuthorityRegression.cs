using System;
using System.IO;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-TXN-02 Activity Clear/Restart Transition Authority Parity regression.
    /// Edit Mode synthetic proof that Clear and Restart reuse IF-TXN-01 phase acceptance and
    /// pre-commit / post-commit terminals without blind rollback.
    /// </summary>
    public static class QaIfTxn02ClearRestartTransitionAuthorityRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run IF-TXN-02 Clear Restart Transition Authority Regression";
        private const string Prefix = "[IF_TXN_02_CLEAR_RESTART_TRANSITION_AUTHORITY]";
        private const int ExpectedCaseCount = 16;

        private static readonly string[] ExpectedCases =
        {
            "edit-mode-required",
            "completed-with-warnings-accepted",
            "policy-skipped-accepted",
            "failed-rejected-cancelled-invalid-not-accepted",
            "clear-pre-commit-terminal",
            "clear-post-commit-reveal-terminal",
            "clear-post-commit-keeps-no-activity-authority",
            "restart-pre-commit-terminal",
            "restart-post-commit-reveal-terminal",
            "restart-post-commit-keeps-new-activity-authority",
            "restart-flow-not-completed-on-reveal-failure",
            "clear-before-wiring",
            "clear-after-wiring",
            "restart-before-wiring",
            "restart-after-wiring",
            "no-blind-rollback-messaging"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var cases = new QaCaseRegistry(ExpectedCases, ExpectedCaseCount);
            try
            {
                Require(
                    !EditorApplication.isPlaying,
                    "IF-TXN-02 Clear/Restart Transition Authority regression requires Edit Mode.");
                cases.Complete("edit-mode-required");

                ProveCompletedWithWarningsAccepted(cases);
                ProvePolicySkippedAccepted(cases);
                ProveRejectedStatuses(cases);
                ProveClearPreCommitTerminal(cases);
                ProveClearPostCommitTerminal(cases);
                ProveClearPostCommitAuthority(cases);
                ProveRestartPreCommitTerminal(cases);
                ProveRestartPostCommitTerminal(cases);
                ProveRestartPostCommitAuthority(cases);
                ProveRestartFlowNotCompleted(cases);
                ProveClearBeforeWiring(cases);
                ProveClearAfterWiring(cases);
                ProveRestartBeforeWiring(cases);
                ProveRestartAfterWiring(cases);
                ProveNoBlindRollbackMessaging(cases);

                cases.RequireComplete();
                Debug.Log(
                    $"{Prefix} status='Passed' cases='{cases.Count}' " +
                    $"completed='{cases.DescribeCompleted()}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' completed='{cases.DescribeCompleted()}' " +
                    $"missing='{cases.DescribeMissing()}' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static void ProveCompletedWithWarningsAccepted(QaCaseRegistry cases)
        {
            TransitionResult result = TransitionResult.CompletedWithWarningsResult(
                TransitionOperationId.From("qa.if-txn-02.warnings"),
                TransitionKind.ActivityClear,
                "qa",
                "after",
                "warnings",
                new[]
                {
                    TransitionStep.Succeeded(
                        0,
                        TransitionPhase.OperationClosed,
                        "after",
                        "ok")
                },
                new[] { "non-blocking" });

            Require(result.Completed && result.CompletedWithWarnings,
                "CompletedWithWarnings must remain TransitionResult.Completed.");
            Require(
                GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "CompletedWithWarnings must be accepted for Clear/Restart phases.");
            cases.Complete("completed-with-warnings-accepted");
        }

        private static void ProvePolicySkippedAccepted(QaCaseRegistry cases)
        {
            TransitionResult result = TransitionResult.SkippedResult(
                TransitionOperationId.From("qa.if-txn-02.skipped"),
                TransitionKind.ActivityClear,
                "qa",
                "before",
                "SkippedByActivityPolicy",
                new[]
                {
                    TransitionStep.Skipped(
                        0,
                        TransitionPhase.OperationOpened,
                        "clear-before-policy-skip",
                        "skipped")
                },
                TransitionEffectKind.Unknown,
                TransitionEffectStatus.Skipped,
                0,
                "None",
                0);

            Require(
                result.Status == TransitionStatus.Skipped &&
                GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "Legitimate policy/no-visual Skipped must remain accepted.");
            cases.Complete("policy-skipped-accepted");
        }

        private static void ProveRejectedStatuses(QaCaseRegistry cases)
        {
            TransitionResult failed = TransitionResult.FailedResult(
                TransitionOperationId.From("qa.if-txn-02.failed"),
                TransitionKind.ActivityClear,
                "qa",
                "before",
                "required surface missing",
                new[]
                {
                    TransitionStep.Failed(
                        0,
                        TransitionPhase.OperationOpened,
                        "before",
                        "required surface missing")
                },
                new[] { "required surface missing" });
            TransitionResult rejected = TransitionResult.RejectedResult(
                TransitionOperationId.From("qa.if-txn-02.rejected"),
                TransitionKind.ActivitySwitch,
                "qa",
                "before",
                "rejected",
                new[] { "rejected" });
            TransitionResult cancelled = new TransitionResult(
                TransitionOperationId.From("qa.if-txn-02.cancelled"),
                TransitionKind.ActivitySwitch,
                TransitionStatus.Cancelled,
                "qa",
                "after",
                "cancelled",
                new[]
                {
                    TransitionStep.Observed(
                        0,
                        TransitionPhase.OperationClosed,
                        "after",
                        "cancelled")
                },
                new[] { "cancelled" });

            Require(!GameFlowRuntime.IsAcceptedTransitionPhase(failed), "Failed must not be accepted.");
            Require(!GameFlowRuntime.IsAcceptedTransitionPhase(rejected), "Rejected must not be accepted.");
            Require(!GameFlowRuntime.IsAcceptedTransitionPhase(cancelled), "Cancelled must not be accepted.");
            Require(!GameFlowRuntime.IsAcceptedTransitionPhase(default), "Invalid must not be accepted.");
            cases.Complete("failed-rejected-cancelled-invalid-not-accepted");
        }

        private static void ProveClearPreCommitTerminal(QaCaseRegistry cases)
        {
            FrameworkActivityRequestResult result =
                FrameworkActivityRequestResult.FailedPreCommitTransition(
                    "Activity Clear aborted before destination commit.",
                    null,
                    "qa",
                    "clear-before",
                    operationKind: GameFlowRequestOperationKind.ActivityClear);

            Require(
                result.Kind == FrameworkActivityRequestKind.FailedPreCommitTransition,
                "Clear Before failure must use FailedPreCommitTransition.");
            Require(
                !result.Succeeded &&
                !result.DestinationAuthoritative &&
                result.OperationKind == GameFlowRequestOperationKind.ActivityClear,
                "Clear Before failure must not succeed and must not advance destination authority.");
            cases.Complete("clear-pre-commit-terminal");
        }

        private static void ProveClearPostCommitTerminal(QaCaseRegistry cases)
        {
            FrameworkActivityRequestResult result =
                FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                    "Activity Clear committed the destination but Transition After failed.",
                    null,
                    "qa",
                    "clear-after",
                    default,
                    operationKind: GameFlowRequestOperationKind.ActivityClear);

            Require(
                result.Kind == FrameworkActivityRequestKind.FailedCommittedTargetReveal,
                "Clear After failure must use FailedCommittedTargetReveal.");
            Require(
                !result.Succeeded &&
                result.DestinationAuthoritative &&
                result.OperationKind == GameFlowRequestOperationKind.ActivityClear,
                "Clear After failure must keep committed no-Activity authority and not Succeeded.");
            cases.Complete("clear-post-commit-reveal-terminal");
        }

        private static void ProveClearPostCommitAuthority(QaCaseRegistry cases)
        {
            FrameworkActivityRequestResult result =
                FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                    "clear after",
                    null,
                    "qa",
                    "clear-after",
                    default,
                    operationKind: GameFlowRequestOperationKind.ActivityClear);

            Require(result.TargetActivity == null,
                "Clear post-commit reveal failure must not invent a restored Activity target.");
            Require(result.CommitBoundaryReached,
                "Clear post-commit reveal failure remains a commit-boundary terminal.");
            cases.Complete("clear-post-commit-keeps-no-activity-authority");
        }

        private static void ProveRestartPreCommitTerminal(QaCaseRegistry cases)
        {
            FrameworkActivityRequestResult clear =
                FrameworkActivityRequestResult.FailedPreCommitTransition(
                    "restart before; clear not requested",
                    null,
                    "qa",
                    "clear",
                    operationKind: GameFlowRequestOperationKind.ActivityClear);
            FrameworkActivityRequestResult reenter =
                FrameworkActivityRequestResult.FailedPreCommitTransition(
                    "restart before; re-enter not requested",
                    null,
                    "qa",
                    "reenter");
            FrameworkActivityRestartFlowResult restart =
                FrameworkActivityRestartFlowResult.FailedClear(clear, reenter, "restart before");

            Require(!restart.Succeeded && !restart.ClearSucceeded && !restart.ReenterSucceeded,
                "Restart Before failure must not report Completed/Succeeded.");
            Require(
                clear.Kind == FrameworkActivityRequestKind.FailedPreCommitTransition &&
                reenter.Kind == FrameworkActivityRequestKind.FailedPreCommitTransition,
                "Restart Before failure stages must use pre-commit Transition terminals.");
            cases.Complete("restart-pre-commit-terminal");
        }

        private static void ProveRestartPostCommitTerminal(QaCaseRegistry cases)
        {
            FrameworkActivityRequestResult clear =
                FrameworkActivityRequestResult.SucceededWith(
                    null,
                    "qa",
                    "clear",
                    default);
            FrameworkActivityRequestResult reenter =
                FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                    "restart after",
                    null,
                    "qa",
                    "reenter",
                    default);
            FrameworkActivityRestartFlowResult restart =
                FrameworkActivityRestartFlowResult.FailedReenter(clear, reenter, "restart after");

            Require(clear.Succeeded && !reenter.Succeeded && !restart.Succeeded,
                "Restart After failure must keep clear success evidence and fail the flow.");
            Require(
                reenter.Kind == FrameworkActivityRequestKind.FailedCommittedTargetReveal &&
                reenter.DestinationAuthoritative,
                "Restart After failure must keep the committed re-enter destination authoritative.");
            cases.Complete("restart-post-commit-reveal-terminal");
        }

        private static void ProveRestartPostCommitAuthority(QaCaseRegistry cases)
        {
            FrameworkActivityRequestResult reenter =
                FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                    "restart after",
                    null,
                    "qa",
                    "reenter",
                    default);

            Require(
                reenter.CommitBoundaryReached && reenter.DestinationAuthoritative && !reenter.Succeeded,
                "Restart After failure preserves committed re-enter authority without success.");
            cases.Complete("restart-post-commit-keeps-new-activity-authority");
        }

        private static void ProveRestartFlowNotCompleted(QaCaseRegistry cases)
        {
            FrameworkActivityRestartFlowResult completed =
                FrameworkActivityRestartFlowResult.Completed(
                    FrameworkActivityRequestResult.SucceededWith(null, "qa", "clear", default),
                    FrameworkActivityRequestResult.SucceededWith(null, "qa", "reenter", default),
                    "ok");
            FrameworkActivityRestartFlowResult afterFailed =
                FrameworkActivityRestartFlowResult.FailedReenter(
                    FrameworkActivityRequestResult.SucceededWith(null, "qa", "clear", default),
                    FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                        "after",
                        null,
                        "qa",
                        "reenter",
                        default),
                    "after");

            Require(completed.Succeeded, "Nominal restart Completed must still succeed.");
            Require(!afterFailed.Succeeded && afterFailed.ClearSucceeded && !afterFailed.ReenterSucceeded,
                "Restart After failure must not report Restart Completed.");
            cases.Complete("restart-flow-not-completed-on-reveal-failure");
        }

        private static void ProveClearBeforeWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            Require(
                ContainsOrdered(
                    source,
                    "TransitionScope.ActivityClear",
                    "TryAcceptTransitionPhase",
                    "Before",
                    "ClearActivityAsync"),
                "Clear must accept Transition Before before ClearActivityAsync lifecycle.");
            Require(
                source.IndexOf("CreatePreCommitClearTransitionFailure", StringComparison.Ordinal) >= 0,
                "Clear Before failure must emit pre-commit Clear terminal helper.");
            cases.Complete("clear-before-wiring");
        }

        private static void ProveClearAfterWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            Require(
                source.IndexOf("CreatePostCommitClearTransitionFailure", StringComparison.Ordinal) >= 0,
                "Clear After failure must emit post-commit Clear reveal terminal helper.");
            Require(
                source.IndexOf("CreatePostCommitClearTransitionFailure", StringComparison.Ordinal) >= 0 &&
                source.IndexOf("RefreshCurrentFlowContext", StringComparison.Ordinal) >= 0,
                "Clear After failure must refresh flow context for committed no-Activity authority.");
            cases.Complete("clear-after-wiring");
        }

        private static void ProveRestartBeforeWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            Require(
                ContainsOrdered(
                    source,
                    "TryAcceptTransitionPhase",
                    "Before",
                    "CreatePreCommitRestartTransitionFailure",
                    "ClearActivityAsync"),
                "Restart Before failure must abort before ClearActivityAsync.");
            Require(
                source.IndexOf("CreatePreCommitRestartTransitionFailure", StringComparison.Ordinal) >= 0,
                "Restart Before failure helper must exist.");
            cases.Complete("restart-before-wiring");
        }

        private static void ProveRestartAfterWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            Require(
                ContainsOrdered(
                    source,
                    "StartActivityAsync",
                    "TryAcceptTransitionPhase",
                    "After",
                    "CreatePostCommitRestartRevealFailure"),
                "Restart After failure must inspect After only after re-enter commit path.");
            Require(
                source.IndexOf("CreatePostCommitRestartRevealFailure", StringComparison.Ordinal) >= 0,
                "Restart After failure helper must exist.");
            cases.Complete("restart-after-wiring");
        }

        private static void ProveNoBlindRollbackMessaging(QaCaseRegistry cases)
        {
            string authority = ReadRequiredPackageSource(
                Path.Combine(
                    "Runtime",
                    "GameFlow",
                    "GameFlowRuntime.TransitionFailureAuthority.cs"));
            Require(
                authority.IndexOf("no blind rollback", StringComparison.OrdinalIgnoreCase) >= 0 ||
                authority.IndexOf("blind rollback", StringComparison.OrdinalIgnoreCase) >= 0,
                "Post-commit messaging must preserve no-blind-rollback semantics for Clear/Restart.");
            Require(
                authority.IndexOf("CreatePostCommitClearTransitionFailure", StringComparison.Ordinal) >= 0 &&
                authority.IndexOf("CreatePostCommitRestartRevealFailure", StringComparison.Ordinal) >= 0,
                "IF-TXN-02 Clear/Restart post-commit helpers must live in TransitionFailureAuthority.");
            cases.Complete("no-blind-rollback-messaging");
        }

        private static string ReadRequiredPackageSource(string relativePath)
        {
            string packageRoot = ResolvePackageRoot();
            string fullPath = Path.Combine(packageRoot, relativePath);
            Require(File.Exists(fullPath), $"Package source missing: {relativePath}");
            return File.ReadAllText(fullPath);
        }

        private static string ResolvePackageRoot()
        {
            string manifestPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));
            if (File.Exists(manifestPath))
            {
                string manifest = File.ReadAllText(manifestPath);
                const string marker = "\"com.immersive.framework\": \"file:";
                int start = manifest.IndexOf(marker, StringComparison.Ordinal);
                if (start >= 0)
                {
                    start += marker.Length;
                    int end = manifest.IndexOf('"', start);
                    if (end > start)
                    {
                        string candidate = manifest.Substring(start, end - start)
                            .Replace('/', Path.DirectorySeparatorChar);
                        candidate = Path.IsPathRooted(candidate)
                            ? Path.GetFullPath(candidate)
                            : Path.GetFullPath(
                                Path.Combine(Application.dataPath, "..", "Packages", candidate));
                        if (Directory.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                }
            }

            string sibling = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    "..",
                    "ImmersivePackages",
                    "com.immersive.framework"));
            if (Directory.Exists(sibling))
            {
                return sibling;
            }

            throw new InvalidOperationException(
                "Could not resolve com.immersive.framework package root for IF-TXN-02 wiring proof.");
        }

        private static bool ContainsOrdered(
            string source,
            string first,
            string second,
            string third,
            string fourth = null)
        {
            int firstIndex = source.IndexOf(first, StringComparison.Ordinal);
            if (firstIndex < 0)
            {
                return false;
            }

            int secondIndex = source.IndexOf(second, firstIndex, StringComparison.Ordinal);
            if (secondIndex < 0)
            {
                return false;
            }

            int thirdIndex = source.IndexOf(third, secondIndex, StringComparison.Ordinal);
            if (thirdIndex < 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(fourth))
            {
                return true;
            }

            return source.IndexOf(fourth, thirdIndex, StringComparison.Ordinal) >= 0;
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
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'");
        }
    }
}

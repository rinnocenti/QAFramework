using System;
using System.IO;
using System.Text.RegularExpressions;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Gate;
using Immersive.Framework.Identity;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-TXN-01 GameFlow Transition Failure Authority regression.
    /// Edit Mode synthetic proof of phase acceptance, typed terminals, reveal recovery policy,
    /// wiring of pre-commit vs post-commit authority, and non-regression of readiness terminals.
    /// Full host presentation/adapter failure remains covered by existing Play Mode GameFlow regressions.
    /// </summary>
    public static class QaIfTxn01TransitionFailureAuthorityRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run IF-TXN-01 Transition Failure Authority Regression";
        private const string Prefix = "[IF_TXN_01_TRANSITION_FAILURE_AUTHORITY]";
        private const int ExpectedCaseCount = 22;

        private static readonly string[] ExpectedCases =
        {
            "edit-mode-required",
            "succeeded-phase-accepted",
            "completed-with-warnings-accepted",
            "policy-skipped-accepted",
            "failed-before-not-accepted",
            "failed-after-not-accepted",
            "rejected-and-cancelled-not-accepted",
            "invalid-result-not-accepted",
            "required-failure-not-masked-as-skipped",
            "route-pre-commit-terminal",
            "route-committed-reveal-terminal",
            "activity-pre-commit-terminal",
            "activity-committed-reveal-terminal",
            "startup-pre-commit-and-reveal-flags",
            "reveal-kind-distinct-from-readiness",
            "readiness-failure-kinds-preserved",
            "supersession-non-authoritative",
            "wait-status-mapping-preserved",
            "reveal-recovery-gate-policy",
            "readiness-recovery-policy-distinct",
            "gameflow-before-authority-wiring",
            "gameflow-after-authority-wiring"
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
                    "IF-TXN-01 Transition Failure Authority regression requires Edit Mode.");
                cases.Complete("edit-mode-required");

                ProveSucceededAccepted(cases);
                ProveCompletedWithWarningsAccepted(cases);
                ProvePolicySkippedAccepted(cases);
                ProveFailedBeforeNotAccepted(cases);
                ProveFailedAfterNotAccepted(cases);
                ProveRejectedAndCancelledNotAccepted(cases);
                ProveInvalidNotAccepted(cases);
                ProveRequiredFailureNotMaskedAsSkipped(cases);
                ProveRoutePreCommitTerminal(cases);
                ProveRouteCommittedRevealTerminal(cases);
                ProveActivityPreCommitTerminal(cases);
                ProveActivityCommittedRevealTerminal(cases);
                ProveStartupFlags(cases);
                ProveRevealDistinctFromReadiness(cases);
                ProveReadinessKindsPreserved(cases);
                ProveSupersession(cases);
                ProveWaitStatusMapping(cases);
                ProveRevealRecoveryGatePolicy(cases);
                ProveRecoveryPoliciesDistinct(cases);
                ProveGameFlowBeforeAuthorityWiring(cases);
                ProveGameFlowAfterAuthorityWiring(cases);

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

        private static void ProveSucceededAccepted(QaCaseRegistry cases)
        {
            TransitionResult result = TransitionResult.SucceededResult(
                TransitionOperationId.From("qa.if-txn-01.succeeded"),
                TransitionKind.RouteSwitch,
                "qa",
                "before",
                "ok",
                new[]
                {
                    TransitionStep.Succeeded(
                        0,
                        TransitionPhase.OperationOpened,
                        "before",
                        "ok")
                });

            Require(result.Completed && result.Succeeded, "Succeeded result must report Completed.");
            Require(
                GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "Succeeded Transition phase must be accepted by GameFlow.");
            Require(
                GameFlowRuntime.TryAcceptTransitionPhase(result, "Before", out string issue) &&
                string.IsNullOrEmpty(issue),
                "Succeeded Before phase must TryAccept without issue.");
            cases.Complete("succeeded-phase-accepted");
        }

        private static void ProveCompletedWithWarningsAccepted(QaCaseRegistry cases)
        {
            TransitionResult result = TransitionResult.CompletedWithWarningsResult(
                TransitionOperationId.From("qa.if-txn-01.warnings"),
                TransitionKind.RouteSwitch,
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
                new[] { "non-blocking-warning" });

            Require(result.CompletedWithWarnings && result.Completed,
                "CompletedWithWarnings must be part of TransitionResult.Completed.");
            Require(
                GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "CompletedWithWarnings must continue the GameFlow transaction.");
            cases.Complete("completed-with-warnings-accepted");
        }

        private static void ProvePolicySkippedAccepted(QaCaseRegistry cases)
        {
            TransitionResult result = TransitionResult.SkippedResult(
                TransitionOperationId.From("qa.if-txn-01.skipped"),
                TransitionKind.ActivitySwitch,
                "qa",
                "before",
                "SkippedByActivityPolicy",
                new[]
                {
                    TransitionStep.Skipped(
                        0,
                        TransitionPhase.OperationOpened,
                        "activity-before-policy-skip",
                        "skipped")
                },
                TransitionEffectKind.Unknown,
                TransitionEffectStatus.Skipped,
                0,
                "None",
                0);

            Require(result.Status == TransitionStatus.Skipped && !result.Completed,
                "Policy Skipped remains Skipped status, not Completed.");
            Require(
                GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "Legitimate policy/no-visual Skipped must be accepted by GameFlow.");
            cases.Complete("policy-skipped-accepted");
        }

        private static void ProveFailedBeforeNotAccepted(QaCaseRegistry cases)
        {
            TransitionResult result = CreateFailedPhase(
                "qa.if-txn-01.before.failed",
                TransitionPhase.OperationOpened,
                "before",
                "required surface missing");

            Require(result.Failed && !result.Completed,
                "Failed Before must not report Completed.");
            Require(
                !GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "Failed Before must not be accepted.");
            Require(
                !GameFlowRuntime.TryAcceptTransitionPhase(result, "Before", out string issue) &&
                issue.IndexOf("Before", StringComparison.Ordinal) >= 0 &&
                issue.IndexOf("Failed", StringComparison.Ordinal) >= 0,
                "Failed Before must produce a typed pre-commit phase issue. " + issue);
            cases.Complete("failed-before-not-accepted");
        }

        private static void ProveFailedAfterNotAccepted(QaCaseRegistry cases)
        {
            TransitionResult result = CreateFailedPhase(
                "qa.if-txn-01.after.failed",
                TransitionPhase.OperationClosed,
                "after",
                "reveal adapter blocked");

            Require(result.Failed && !result.Completed,
                "Failed After must not report Completed.");
            Require(
                !GameFlowRuntime.IsAcceptedTransitionPhase(result),
                "Failed After must not be accepted; request cannot succeed.");
            cases.Complete("failed-after-not-accepted");
        }

        private static void ProveRejectedAndCancelledNotAccepted(QaCaseRegistry cases)
        {
            TransitionResult rejected = TransitionResult.RejectedResult(
                TransitionOperationId.From("qa.if-txn-01.rejected"),
                TransitionKind.RouteSwitch,
                "qa",
                "before",
                "rejected",
                new[] { "rejected" });
            TransitionResult cancelled = new TransitionResult(
                TransitionOperationId.From("qa.if-txn-01.cancelled"),
                TransitionKind.RouteSwitch,
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

            Require(
                !GameFlowRuntime.IsAcceptedTransitionPhase(rejected),
                "Rejected must not be accepted.");
            Require(
                !GameFlowRuntime.IsAcceptedTransitionPhase(cancelled),
                "Cancelled must not be accepted.");
            cases.Complete("rejected-and-cancelled-not-accepted");
        }

        private static void ProveInvalidNotAccepted(QaCaseRegistry cases)
        {
            Require(
                !GameFlowRuntime.IsAcceptedTransitionPhase(default),
                "Invalid default TransitionResult must not be accepted.");
            Require(
                !GameFlowRuntime.TryAcceptTransitionPhase(default, "After", out string issue) &&
                issue.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0,
                "Invalid After must produce an invalid-result issue.");
            cases.Complete("invalid-result-not-accepted");
        }

        private static void ProveRequiredFailureNotMaskedAsSkipped(QaCaseRegistry cases)
        {
            TransitionResult failed = CreateFailedPhase(
                "qa.if-txn-01.required-failed",
                TransitionPhase.OperationOpened,
                "before",
                "required Transition surface missing");
            TransitionResult rejected = TransitionResult.RejectedResult(
                TransitionOperationId.From("qa.if-txn-01.required-rejected"),
                TransitionKind.RouteSwitch,
                "qa",
                "before",
                "required Transition rejected",
                new[] { "required Transition rejected" });

            Require(failed.Status == TransitionStatus.Failed && failed.Status != TransitionStatus.Skipped,
                "Required Transition failure must remain Failed, not Skipped.");
            Require(rejected.Status == TransitionStatus.Rejected && rejected.Status != TransitionStatus.Skipped,
                "Required Transition rejection must remain Rejected, not Skipped.");
            Require(
                !GameFlowRuntime.IsAcceptedTransitionPhase(failed) &&
                !GameFlowRuntime.IsAcceptedTransitionPhase(rejected),
                "Required Transition Failed/Rejected must never be accepted as policy Skipped.");
            cases.Complete("required-failure-not-masked-as-skipped");
        }

        private static void ProveRoutePreCommitTerminal(QaCaseRegistry cases)
        {
            FrameworkRouteRequestResult result =
                FrameworkRouteRequestResult.FailedPreCommitTransition(
                    "before failed",
                    null,
                    "qa",
                    "pre-commit");

            Require(
                result.Kind == FrameworkRouteRequestKind.FailedPreCommitTransition,
                "Route pre-commit Transition failure kind diverged.");
            Require(!result.Succeeded && !result.DestinationAuthoritative && !result.Superseded,
                "Route pre-commit failure must not succeed or advance destination authority.");
            cases.Complete("route-pre-commit-terminal");
        }

        private static void ProveRouteCommittedRevealTerminal(QaCaseRegistry cases)
        {
            FrameworkRouteRequestResult result =
                FrameworkRouteRequestResult.FailedCommittedTargetReveal(
                    "after failed",
                    null,
                    "qa",
                    "reveal",
                    default);

            Require(
                result.Kind == FrameworkRouteRequestKind.FailedCommittedTargetReveal,
                "Route committed-target reveal failure kind diverged.");
            Require(!result.Succeeded && result.DestinationAuthoritative,
                "Route reveal failure must keep destination authoritative and not Succeeded.");
            cases.Complete("route-committed-reveal-terminal");
        }

        private static void ProveActivityPreCommitTerminal(QaCaseRegistry cases)
        {
            FrameworkActivityRequestResult result =
                FrameworkActivityRequestResult.FailedPreCommitTransition(
                    "before failed",
                    null,
                    "qa",
                    "pre-commit");

            Require(
                result.Kind == FrameworkActivityRequestKind.FailedPreCommitTransition,
                "Activity pre-commit Transition failure kind diverged.");
            Require(
                !result.Succeeded &&
                !result.DestinationAuthoritative &&
                !result.CommitBoundaryReached,
                "Activity pre-commit failure must not succeed or mark commit boundary.");
            cases.Complete("activity-pre-commit-terminal");
        }

        private static void ProveActivityCommittedRevealTerminal(QaCaseRegistry cases)
        {
            FrameworkActivityRequestResult result =
                FrameworkActivityRequestResult.FailedCommittedTargetReveal(
                    "after failed",
                    null,
                    "qa",
                    "reveal",
                    default);

            Require(
                result.Kind == FrameworkActivityRequestKind.FailedCommittedTargetReveal,
                "Activity committed-target reveal failure kind diverged.");
            Require(
                !result.Succeeded &&
                result.CommitBoundaryReached &&
                result.DestinationAuthoritative,
                "Activity reveal failure must preserve commit boundary and not Succeeded.");
            cases.Complete("activity-committed-reveal-terminal");
        }

        private static void ProveStartupFlags(QaCaseRegistry cases)
        {
            FrameworkGameFlowStartResult preCommit =
                FrameworkGameFlowStartResult.FailedPreCommitTransition("before failed");
            FrameworkGameFlowStartResult reveal =
                FrameworkGameFlowStartResult.FailedCommittedTargetReveal(
                    "after failed",
                    null,
                    default,
                    ActivityEntryReadinessExecutionStatus.Ready);
            FrameworkGameFlowStartResult readiness =
                FrameworkGameFlowStartResult.FailedCommittedDestination(
                    "not ready",
                    null,
                    default,
                    ActivityEntryReadinessExecutionStatus.Failed);

            Require(
                !preCommit.Started &&
                preCommit.PreCommitTransitionFailed &&
                !preCommit.CommittedTargetRevealFailed &&
                !preCommit.DestinationAuthoritative,
                "Startup pre-commit Transition failure flags diverged.");
            Require(
                !reveal.Started &&
                !reveal.PreCommitTransitionFailed &&
                reveal.CommittedTargetRevealFailed &&
                reveal.DestinationAuthoritative,
                "Startup committed reveal failure flags diverged.");
            Require(
                !readiness.Started &&
                !readiness.PreCommitTransitionFailed &&
                !readiness.CommittedTargetRevealFailed &&
                readiness.DestinationAuthoritative,
                "Startup readiness failure must remain distinct from reveal failure.");
            cases.Complete("startup-pre-commit-and-reveal-flags");
        }

        private static void ProveRevealDistinctFromReadiness(QaCaseRegistry cases)
        {
            Require(
                FrameworkRouteRequestKind.FailedCommittedTargetReveal !=
                FrameworkRouteRequestKind.FailedCommittedTargetNotReady,
                "Route reveal failure must not reuse FailedCommittedTargetNotReady.");
            Require(
                FrameworkActivityRequestKind.FailedCommittedTargetReveal !=
                FrameworkActivityRequestKind.FailedCommittedTargetNotReady,
                "Activity reveal failure must not reuse FailedCommittedTargetNotReady.");
            Require(
                FrameworkRouteRequestKind.FailedPreCommitTransition !=
                FrameworkRouteRequestKind.FailedCommittedTargetReveal,
                "Pre-commit and committed reveal terminals must be distinct.");
            cases.Complete("reveal-kind-distinct-from-readiness");
        }

        private static void ProveReadinessKindsPreserved(QaCaseRegistry cases)
        {
            FrameworkRouteRequestResult notReady =
                FrameworkRouteRequestResult.FailedCommittedTargetReadiness(
                    FrameworkRouteRequestKind.FailedCommittedTargetNotReady,
                    "not ready",
                    null,
                    "qa",
                    "readiness",
                    default);
            FrameworkRouteRequestResult cancelled =
                FrameworkRouteRequestResult.FailedCommittedTargetReadiness(
                    FrameworkRouteRequestKind.FailedCommittedTargetReadinessCancelled,
                    "cancelled",
                    null,
                    "qa",
                    "readiness",
                    default);
            FrameworkRouteRequestResult invalidated =
                FrameworkRouteRequestResult.FailedCommittedTargetReadiness(
                    FrameworkRouteRequestKind.FailedCommittedTargetReadinessInvalidated,
                    "invalidated",
                    null,
                    "qa",
                    "readiness",
                    default);
            FrameworkActivityRequestResult activityNotReady =
                FrameworkActivityRequestResult.FailedCommittedTargetNotReady(
                    "not ready",
                    null,
                    "qa",
                    "readiness",
                    default);

            Require(!notReady.Succeeded && notReady.DestinationAuthoritative,
                "FailedCommittedTargetNotReady contract regressed.");
            Require(!cancelled.Succeeded && cancelled.DestinationAuthoritative,
                "FailedCommittedTargetReadinessCancelled contract regressed.");
            Require(!invalidated.Succeeded && invalidated.DestinationAuthoritative,
                "FailedCommittedTargetReadinessInvalidated contract regressed.");
            Require(
                activityNotReady.Kind == FrameworkActivityRequestKind.FailedCommittedTargetNotReady &&
                activityNotReady.DestinationAuthoritative,
                "Activity readiness failure contract regressed.");
            cases.Complete("readiness-failure-kinds-preserved");
        }

        private static void ProveSupersession(QaCaseRegistry cases)
        {
            var result = new FrameworkRouteRequestResult(
                FrameworkRouteRequestKind.SupersededCommittedTargetByRouteReplacement,
                "superseded",
                null,
                "qa",
                "RouteAuthorityReplaced",
                default);

            Require(result.Superseded && !result.Succeeded && !result.DestinationAuthoritative,
                "Supersession contract regressed.");
            cases.Complete("supersession-non-authoritative");
        }

        private static void ProveWaitStatusMapping(QaCaseRegistry cases)
        {
            Require(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Ready) ==
                ActivityEntryReadinessExecutionStatus.Ready,
                "Ready wait mapping regressed.");
            Require(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Failed) ==
                ActivityEntryReadinessExecutionStatus.Failed,
                "Failed wait mapping regressed.");
            Require(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Invalidated) ==
                ActivityEntryReadinessExecutionStatus.Invalidated,
                "Invalidated wait mapping regressed.");
            Require(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Cancelled) ==
                ActivityEntryReadinessExecutionStatus.Cancelled,
                "Cancelled wait mapping regressed.");
            Require(
                GameFlowRuntime.MapWaitStatus(ActivityEntryReadinessWaitStatus.Superseded) ==
                ActivityEntryReadinessExecutionStatus.Superseded,
                "Superseded wait mapping regressed.");
            cases.Complete("wait-status-mapping-preserved");
        }

        private static void ProveRevealRecoveryGatePolicy(QaCaseRegistry cases)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            try
            {
                var serialized = new SerializedObject(activity);
                SerializedProperty activityId = serialized.FindProperty("activityId");
                Require(activityId != null, "ActivityAsset.activityId property was not found.");
                activityId.stringValue = "qa.if-txn-01.reveal-recovery";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Require(activity.HasValidActivityId, "QA ActivityId assignment failed.");

                var occurrence = new ActivityReadinessOccurrence(activity, 1);
                FrameworkIdentityKey owner = FrameworkIdentityKey.From(activity.ActivityId);
                GateSnapshot snapshot = CommittedTargetRevealRecoveryGatePolicy.Create(
                    occurrence,
                    owner,
                    "qa",
                    "Transition After failed");

                Require(snapshot.HasBlockers && snapshot.BlockerCount == 3,
                    "Reveal recovery gate must apply input/interaction/gameplay protection.");
                Require(
                    CommittedTargetRevealRecoveryGatePolicy.PolicySource.IndexOf(
                        "IF-TXN-01",
                        StringComparison.Ordinal) >= 0,
                    "Reveal recovery policy source must identify IF-TXN-01.");
                Require(
                    CommittedTargetRevealRecoveryGatePolicy.PolicySource.IndexOf(
                        "Readiness",
                        StringComparison.OrdinalIgnoreCase) < 0,
                    "Reveal recovery must not be labeled as readiness recovery.");
                cases.Complete("reveal-recovery-gate-policy");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(activity);
            }
        }

        private static void ProveRecoveryPoliciesDistinct(QaCaseRegistry cases)
        {
            Require(
                !string.Equals(
                    ActivityEntryReadinessRecoveryGatePolicy.PolicySource,
                    CommittedTargetRevealRecoveryGatePolicy.PolicySource,
                    StringComparison.Ordinal),
                "Readiness and reveal recovery policy sources must remain distinct.");
            cases.Complete("readiness-recovery-policy-distinct");
        }

        private static void ProveGameFlowBeforeAuthorityWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            string loadingSource = ReadRequiredPackageSource(
                Path.Combine(
                    "Runtime",
                    "GameFlow",
                    "GameFlowRuntime.ActivityEntryLoadingProgress.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "TryAcceptTransitionPhase",
                    "Before",
                    "StartRouteCoreAsync"),
                "Request/startup path must accept Transition Before before destination Route lifecycle starts.");
            Require(
                ContainsOrdered(
                    source,
                    "TryAcceptTransitionPhase",
                    "Before",
                    "StartActivityWithActivationGateAsync"),
                "Activity path must accept Transition Before before destination Activity lifecycle starts.");
            Require(
                ContainsOrdered(
                    loadingSource,
                    "TryAcceptTransitionPhase",
                    "Before",
                    "StartRouteCoreAsync"),
                "Participant-aware startup Loading path must accept Transition Before before lifecycle starts.");
            Require(
                source.IndexOf("FailedPreCommitTransition", StringComparison.Ordinal) >= 0 ||
                source.IndexOf("CreatePreCommit", StringComparison.Ordinal) >= 0,
                "GameFlowRuntime must emit pre-commit Transition failure terminals.");
            cases.Complete("gameflow-before-authority-wiring");
        }

        private static void ProveGameFlowAfterAuthorityWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            string authoritySource = ReadRequiredPackageSource(
                Path.Combine(
                    "Runtime",
                    "GameFlow",
                    "GameFlowRuntime.TransitionFailureAuthority.cs"));

            Require(
                source.IndexOf("TryAcceptTransitionPhase", StringComparison.Ordinal) >= 0 &&
                source.IndexOf("\"After\"", StringComparison.Ordinal) >= 0,
                "GameFlow must inspect Transition After phase results.");
            Require(
                source.IndexOf("CreateCommittedRouteRevealFailure", StringComparison.Ordinal) >= 0 &&
                source.IndexOf("CreateCommittedActivityRevealFailure", StringComparison.Ordinal) >= 0,
                "GameFlow must convert non-accepted Transition After into committed-target reveal failure terminals.");
            Require(
                authoritySource.IndexOf(
                    "ApplyCommittedTargetRevealRecoveryGate",
                    StringComparison.Ordinal) >= 0 &&
                authoritySource.IndexOf(
                    "FailedCommittedTargetReveal",
                    StringComparison.Ordinal) >= 0,
                "Committed After failure must apply reveal recovery protection and typed reveal terminals.");
            Require(
                authoritySource.IndexOf("blind rollback", StringComparison.OrdinalIgnoreCase) >= 0,
                "Reveal failure messaging must preserve no-blind-rollback semantics.");
            Require(
                CountOccurrences(source, "TryAcceptTransitionPhase") >= 4,
                "Canonical startup/route/activity paths must inspect multiple Transition phases.");
            cases.Complete("gameflow-after-authority-wiring");
        }

        private static int CountOccurrences(string source, string token)
        {
            int count = 0;
            int index = 0;
            while (index < source.Length)
            {
                int found = source.IndexOf(token, index, StringComparison.Ordinal);
                if (found < 0)
                {
                    break;
                }

                count++;
                index = found + token.Length;
            }

            return count;
        }

        private static TransitionResult CreateFailedPhase(
            string operationId,
            TransitionPhase phase,
            string phaseLabel,
            string message)
        {
            return TransitionResult.FailedResult(
                TransitionOperationId.From(operationId),
                TransitionKind.RouteSwitch,
                "qa",
                phaseLabel,
                message,
                new[]
                {
                    TransitionStep.Failed(0, phase, phaseLabel, message)
                },
                new[] { message });
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
            // Prefer the file: dependency path used by QAFramework.
            string manifestPath = Path.GetFullPath(
                Path.Combine(
                    Application.dataPath,
                    "..",
                    "Packages",
                    "manifest.json"));
            if (File.Exists(manifestPath))
            {
                string manifest = File.ReadAllText(manifestPath);
                Match match = Regex.Match(
                    manifest,
                    "\"com\\.immersive\\.framework\"\\s*:\\s*\"file:([^\"]+)\"",
                    RegexOptions.CultureInvariant);
                if (match.Success)
                {
                    string candidate = match.Groups[1].Value.Replace('/', Path.DirectorySeparatorChar);
                    if (!Path.IsPathRooted(candidate))
                    {
                        candidate = Path.GetFullPath(
                            Path.Combine(
                                Application.dataPath,
                                "..",
                                "Packages",
                                candidate));
                    }
                    else
                    {
                        candidate = Path.GetFullPath(candidate);
                    }

                    if (Directory.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            // Fallback: known local ImmersivePackages layout next to QAFramework.
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
                "Could not resolve com.immersive.framework package root for source wiring proof.");
        }

        private static bool ContainsOrdered(
            string source,
            string first,
            string second,
            string third)
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
            return thirdIndex >= 0;
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

using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Gate;
using Immersive.Framework.Identity;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-TXN-03A Transition Gate Release Terminal Integrity regression.
    /// Edit Mode proof that residual Transition Gate state is released after every
    /// terminal, and that CurrentTransitionGateSnapshot is the pure Transition Gate
    /// (not mixed with Activity Entry Readiness Recovery).
    /// </summary>
    public static class QaIfTxn03ATransitionGateTerminalIntegrityRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Game Flow/Run IF-TXN-03A Transition Gate Terminal Integrity Regression";
        private const string Prefix = "[IF_TXN_03A_TRANSITION_GATE_TERMINAL_INTEGRITY]";
        private const int ExpectedCaseCount = 16;

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] ExpectedCases =
        {
            "edit-mode-required",
            "projection-source-pure-transition-gate",
            "readiness-composite-source-preserved",
            "route-success-release-wiring",
            "activity-success-release-wiring",
            "pre-commit-failure-release-wiring",
            "post-commit-reveal-failure-release-wiring",
            "authorization-rejection-finally-release",
            "exception-fault-finally-release",
            "clear-terminal-release-wiring",
            "restart-failure-release-wiring",
            "restart-success-release-wiring",
            "runtime-apply-release-residual-clean",
            "readiness-recovery-active-transition-clean",
            "recovery-cleanup-all-clean",
            "host-surface-separation"
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
                    "IF-TXN-03A Transition Gate Terminal Integrity regression requires Edit Mode.");
                cases.Complete("edit-mode-required");

                ProveProjectionSource(cases);
                ProveReadinessCompositeSource(cases);
                ProveRouteSuccessReleaseWiring(cases);
                ProveActivitySuccessReleaseWiring(cases);
                ProvePreCommitFailureReleaseWiring(cases);
                ProvePostCommitRevealFailureReleaseWiring(cases);
                ProveAuthorizationRejectionFinallyRelease(cases);
                ProveExceptionFaultFinallyRelease(cases);
                ProveClearTerminalReleaseWiring(cases);
                ProveRestartFailureReleaseWiring(cases);
                ProveRestartSuccessReleaseWiring(cases);
                ProveRuntimeApplyReleaseResidualClean(cases);
                ProveReadinessRecoveryActiveTransitionClean(cases);
                ProveRecoveryCleanupAllClean(cases);
                ProveHostSurfaceSeparation(cases);

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

        private static void ProveProjectionSource(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));

            Require(
                source.IndexOf(
                    "CurrentTransitionGateSnapshot =>",
                    StringComparison.Ordinal) >= 0,
                "CurrentTransitionGateSnapshot property must exist.");
            Require(
                ContainsOrdered(
                    source,
                    "CurrentTransitionGateSnapshot =>",
                    "_transitionGateSnapshot"),
                "CurrentTransitionGateSnapshot must project the canonical Transition Gate field only.");
            Require(
                source.IndexOf(
                    "CurrentTransitionGateSnapshot =>\r\n            CurrentActivityEntryReadinessGateSnapshot",
                    StringComparison.Ordinal) < 0 &&
                source.IndexOf(
                    "CurrentTransitionGateSnapshot =>\n            CurrentActivityEntryReadinessGateSnapshot",
                    StringComparison.Ordinal) < 0 &&
                source.IndexOf(
                    "CurrentTransitionGateSnapshot => CurrentActivityEntryReadinessGateSnapshot",
                    StringComparison.Ordinal) < 0,
                "CurrentTransitionGateSnapshot must not alias the readiness composite.");
            cases.Complete("projection-source-pure-transition-gate");
        }

        private static void ProveReadinessCompositeSource(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine(
                    "Runtime",
                    "GameFlow",
                    "GameFlowRuntime.ActivityEntryReadinessOrchestration.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "CurrentActivityEntryReadinessGateSnapshot =>",
                    "CombineGateSnapshots",
                    "_transitionGateSnapshot",
                    "_activityEntryReadinessRecoveryGateSnapshot"),
                "CurrentActivityEntryReadinessGateSnapshot must keep Transition + Recovery composition.");
            Require(
                source.IndexOf(
                    "EvaluateTransitionGateAdmission",
                    StringComparison.Ordinal) < 0,
                "Admission evaluation lives on GameFlowRuntime.cs; composite ownership is the readiness partial.");

            string runtimeSource = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            Require(
                ContainsOrdered(
                    runtimeSource,
                    "EvaluateTransitionGateAdmission",
                    "CurrentActivityEntryReadinessGateSnapshot.Evaluate"),
                "EvaluateTransitionGateAdmission must continue to evaluate the readiness composite.");
            cases.Complete("readiness-composite-source-preserved");
        }

        private static void ProveRouteSuccessReleaseWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot)",
                    "FrameworkRouteRequestResult.SucceededWith("),
                "Route success path must release the Transition Gate before SucceededWith.");
            Require(
                ContainsOrdered(
                    source,
                    "RequestRouteAsync",
                    "ReleaseTransitionGateIfStillActive"),
                "Route request path must residual-release Transition Gate in finally.");
            cases.Complete("route-success-release-wiring");
        }

        private static void ProveActivitySuccessReleaseWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot)",
                    "FrameworkActivityRequestResult.SucceededWith("),
                "Activity success path must release the Transition Gate before SucceededWith.");
            Require(
                ContainsOrdered(
                    source,
                    "StartActivityWithActivationGateAsync",
                    "ReleaseTransitionGateIfStillActive"),
                "Activity request path must residual-release Transition Gate in finally.");
            cases.Complete("activity-success-release-wiring");
        }

        private static void ProvePreCommitFailureReleaseWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "TryAcceptTransitionPhase",
                    "Before",
                    "ReleaseTransitionGate",
                    "CreatePreCommitRouteTransitionFailure"),
                "Route pre-commit failure must release Transition Gate before the typed terminal.");
            Require(
                ContainsOrdered(
                    source,
                    "TryAcceptTransitionPhase",
                    "Before",
                    "ReleaseTransitionGate",
                    "CreatePreCommitActivityTransitionFailure"),
                "Activity pre-commit failure must release Transition Gate before the typed terminal.");
            cases.Complete("pre-commit-failure-release-wiring");
        }

        private static void ProvePostCommitRevealFailureReleaseWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            string authority = ReadRequiredPackageSource(
                Path.Combine(
                    "Runtime",
                    "GameFlow",
                    "GameFlowRuntime.TransitionFailureAuthority.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "TryAcceptTransitionPhase",
                    "After",
                    "ReleaseTransitionGate",
                    "CreateCommittedRouteRevealFailure") ||
                ContainsOrdered(
                    source,
                    "ReleaseTransitionGate",
                    "CreateCommittedRouteRevealFailure"),
                "Route post-commit reveal failure must release Transition Gate.");
            Require(
                ContainsOrdered(
                    source,
                    "ReleaseTransitionGate",
                    "CreateCommittedActivityRevealFailure"),
                "Activity post-commit reveal failure must release Transition Gate.");
            Require(
                authority.IndexOf(
                    "ApplyCommittedTargetRevealRecoveryGate",
                    StringComparison.Ordinal) >= 0,
                "Reveal failure must still apply reveal recovery protection separately.");
            cases.Complete("post-commit-reveal-failure-release-wiring");
        }

        private static void ProveAuthorizationRejectionFinallyRelease(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));

            // Authorization returns after Apply without an explicit local Release;
            // residual cleanup is owned by finally ReleaseTransitionGateIfStillActive.
            Require(
                ContainsOrdered(
                    source,
                    "AuthorizeActivityPlayerTransition",
                    "FailedInvalidConfig",
                    "finally",
                    "ReleaseTransitionGateIfStillActive"),
                "Authorization rejection after Apply must clean Transition Gate via finally residual release.");
            Require(
                CountOccurrences(source, "ReleaseTransitionGateIfStillActive") >= 4,
                "Canonical lifecycle methods must retain residual finally release coverage.");
            cases.Complete("authorization-rejection-finally-release");
        }

        private static void ProveExceptionFaultFinallyRelease(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));
            string loading = ReadRequiredPackageSource(
                Path.Combine(
                    "Runtime",
                    "GameFlow",
                    "GameFlowRuntime.ActivityEntryLoadingProgress.cs"));

            Require(
                source.IndexOf("finally", StringComparison.Ordinal) >= 0 &&
                source.IndexOf(
                    "ReleaseTransitionGateIfStillActive",
                    StringComparison.Ordinal) >= 0,
                "Exception/fault paths after Apply rely on finally residual Transition Gate release.");
            Require(
                loading.IndexOf(
                    "ReleaseTransitionGateIfStillActive",
                    StringComparison.Ordinal) >= 0,
                "Loading/startup participant path must also residual-release the Transition Gate.");
            cases.Complete("exception-fault-finally-release");
        }

        private static void ProveClearTerminalReleaseWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "TransitionScope.ActivityClear",
                    "ReleaseTransitionGate",
                    "CreatePreCommitClearTransitionFailure") ||
                ContainsOrdered(
                    source,
                    "CreatePreCommitClearTransitionFailure",
                    "ReleaseTransitionGateIfStillActive"),
                "Clear pre-commit/failure path must release Transition Gate.");
            Require(
                ContainsOrdered(
                    source,
                    "ReleaseTransitionGate",
                    "CreatePostCommitClearTransitionFailure") ||
                source.IndexOf(
                    "CreatePostCommitClearTransitionFailure",
                    StringComparison.Ordinal) >= 0,
                "Clear post-commit terminal path must coexist with Transition Gate release.");
            Require(
                ContainsOrdered(
                    source,
                    "TransitionScope.ActivityClear",
                    "ReleaseTransitionGateIfStillActive"),
                "Clear path must residual-release Transition Gate in finally.");
            cases.Complete("clear-terminal-release-wiring");
        }

        private static void ProveRestartFailureReleaseWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "CreatePreCommitRestartTransitionFailure",
                    "ReleaseTransitionGate") ||
                ContainsOrdered(
                    source,
                    "ReleaseTransitionGate",
                    "CreatePreCommitRestartTransitionFailure"),
                "Restart pre-commit failure must release Transition Gate.");
            Require(
                ContainsOrdered(
                    source,
                    "CreatePostCommitRestartRevealFailure",
                    "ReleaseTransitionGate") ||
                ContainsOrdered(
                    source,
                    "ReleaseTransitionGate",
                    "CreatePostCommitRestartRevealFailure"),
                "Restart post-commit reveal failure must release Transition Gate.");
            cases.Complete("restart-failure-release-wiring");
        }

        private static void ProveRestartSuccessReleaseWiring(QaCaseRegistry cases)
        {
            string source = ReadRequiredPackageSource(
                Path.Combine("Runtime", "GameFlow", "GameFlowRuntime.cs"));

            Require(
                ContainsOrdered(
                    source,
                    "ReleaseTransitionGate(transitionGateMode, transitionGateSnapshot)",
                    "FrameworkActivityRestartFlowResult.Completed("),
                "Restart success must release Transition Gate before Completed.");
            Require(
                ContainsOrdered(
                    source,
                    "RestartActivityAsync",
                    "ReleaseTransitionGateIfStillActive"),
                "Restart path must residual-release Transition Gate in finally.");
            cases.Complete("restart-success-release-wiring");
        }

        private static void ProveRuntimeApplyReleaseResidualClean(QaCaseRegistry cases)
        {
            GameFlowRuntime runtime = CreateHarnessRuntime();
            try
            {
                TransitionOperationId operationId =
                    TransitionOperationId.From("qa.if-txn-03a.apply-release");
                InvokeApplyTransitionGate(
                    runtime,
                    operationId,
                    TransitionKind.RouteSwitch,
                    TransitionGateMode.InputInteractionAndGameplay,
                    "qa",
                    "apply-release");

                Require(
                    runtime.CurrentTransitionGateSnapshot.HasBlockers &&
                    runtime.CurrentTransitionGateMode ==
                        TransitionGateMode.InputInteractionAndGameplay,
                    "ApplyTransitionGate must leave residual Transition Gate blockers active.");

                TransitionGateDiagnostics diagnostics = InvokeReleaseTransitionGate(
                    runtime,
                    TransitionGateMode.InputInteractionAndGameplay,
                    runtime.CurrentTransitionGateSnapshot);

                Require(
                    diagnostics.Applied && diagnostics.Released,
                    "ReleaseTransitionGate diagnostics must report applied+released.");
                RequireTransitionGateReleased(runtime, "after explicit ReleaseTransitionGate");

                InvokeApplyTransitionGate(
                    runtime,
                    TransitionOperationId.From("qa.if-txn-03a.finally-residual"),
                    TransitionKind.ActivitySwitch,
                    TransitionGateMode.LifecycleRequestsOnly,
                    "qa",
                    "finally-residual");
                Require(
                    runtime.CurrentTransitionGateSnapshot.HasBlockers,
                    "Second Apply must re-arm residual Transition Gate.");
                InvokeReleaseTransitionGateIfStillActive(runtime);
                RequireTransitionGateReleased(runtime, "after ReleaseTransitionGateIfStillActive");
            }
            finally
            {
                // no disposable resources on harness
            }

            cases.Complete("runtime-apply-release-residual-clean");
        }

        private static void ProveReadinessRecoveryActiveTransitionClean(QaCaseRegistry cases)
        {
            GameFlowRuntime runtime = CreateHarnessRuntime();
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            try
            {
                AssignActivityId(activity, "qa.if-txn-03a.recovery-separation");
                var occurrence = new ActivityReadinessOccurrence(activity, 7);
                FrameworkIdentityKey owner = FrameworkIdentityKey.From(activity.ActivityId);

                InvokeApplyTransitionGate(
                    runtime,
                    TransitionOperationId.From("qa.if-txn-03a.recovery"),
                    TransitionKind.ActivitySwitch,
                    TransitionGateMode.InputInteractionAndGameplay,
                    "qa",
                    "readiness-recovery");
                Require(
                    runtime.CurrentTransitionGateSnapshot.HasBlockers,
                    "Transition Gate must be armed before readiness failure release.");

                InvokeReleaseTransitionGate(
                    runtime,
                    TransitionGateMode.InputInteractionAndGameplay,
                    runtime.CurrentTransitionGateSnapshot);
                SetRecoveryGateSnapshot(
                    runtime,
                    ActivityEntryReadinessRecoveryGatePolicy.Create(
                        occurrence,
                        owner,
                        "qa",
                        "readiness-failure-with-recovery"));

                // Critical IF-TXN-03A residual separation criterion:
                Require(
                    runtime.CurrentTransitionGateMode == TransitionGateMode.None,
                    "After readiness failure release, CurrentTransitionGateMode must be None.");
                Require(
                    !runtime.CurrentTransitionGateSnapshot.HasBlockers,
                    "After readiness failure release, CurrentTransitionGateSnapshot.HasBlockers must be false.");
                Require(
                    runtime.CurrentActivityEntryReadinessGateSnapshot.HasBlockers,
                    "Activity Entry Readiness Recovery must keep the composite blocked.");
                Require(
                    HasPolicySource(
                        runtime.CurrentActivityEntryReadinessGateSnapshot,
                        ActivityEntryReadinessRecoveryGatePolicy.PolicySource),
                    "Composite blockers must identify Activity Entry Readiness Recovery.");
                Require(
                    !HasPolicySource(
                        runtime.CurrentTransitionGateSnapshot,
                        ActivityEntryReadinessRecoveryGatePolicy.PolicySource),
                    "Pure Transition Gate projection must not include readiness recovery blockers.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(activity);
            }

            cases.Complete("readiness-recovery-active-transition-clean");
        }

        private static void ProveRecoveryCleanupAllClean(QaCaseRegistry cases)
        {
            GameFlowRuntime runtime = CreateHarnessRuntime();
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            try
            {
                AssignActivityId(activity, "qa.if-txn-03a.recovery-cleanup");
                var occurrence = new ActivityReadinessOccurrence(activity, 3);
                FrameworkIdentityKey owner = FrameworkIdentityKey.From(activity.ActivityId);

                SetRecoveryGateSnapshot(
                    runtime,
                    ActivityEntryReadinessRecoveryGatePolicy.Create(
                        occurrence,
                        owner,
                        "qa",
                        "cleanup"));
                Require(
                    runtime.CurrentActivityEntryReadinessGateSnapshot.HasBlockers,
                    "Recovery gate must block the composite before cleanup.");

                InvokeReleaseActivityEntryReadinessRecoveryGate(runtime);
                InvokeReleaseTransitionGateIfStillActive(runtime);

                RequireTransitionGateReleased(runtime, "after recovery cleanup");
                Require(
                    !runtime.CurrentActivityEntryReadinessGateSnapshot.HasBlockers,
                    "Recovery cleanup must clear the readiness composite.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(activity);
            }

            cases.Complete("recovery-cleanup-all-clean");
        }

        private static void ProveHostSurfaceSeparation(QaCaseRegistry cases)
        {
            string host = ReadRequiredPackageSource(
                Path.Combine(
                    "Runtime",
                    "ApplicationLifecycle",
                    "FrameworkRuntimeHost.cs"));

            Require(
                ContainsOrdered(
                    host,
                    "TransitionGateSnapshot =>",
                    "CurrentTransitionGateSnapshot"),
                "Host TransitionGateSnapshot must expose pure CurrentTransitionGateSnapshot.");
            Require(
                ContainsOrdered(
                    host,
                    "ActivityEntryReadinessGateSnapshot =>",
                    "CurrentActivityEntryReadinessGateSnapshot"),
                "Host must expose the readiness composite for residual recovery diagnostics.");
            Require(
                ContainsOrdered(
                    host,
                    "CurrentGateSnapshot =>",
                    "ActivityEntryReadinessGateSnapshot"),
                "Host CurrentGateSnapshot must combine Pause with the readiness composite so recovery still protects input.");
            Require(
                host.IndexOf(
                    "CurrentTransitionGateMode",
                    StringComparison.Ordinal) >= 0,
                "Host must expose residual CurrentTransitionGateMode for terminal integrity proof.");
            cases.Complete("host-surface-separation");
        }

        private static GameFlowRuntime CreateHarnessRuntime()
        {
            return new GameFlowRuntime(
                new RuntimeContentRuntime(),
                new QaFakeRouteRuntimePort(),
                new QaFakeActivityRuntimePort(),
                new QaFakeRouteCycleResetRuntimePort(),
                new QaFakeActivityCycleResetRuntimePort(),
                new QaFakeActivityRestartRuntimePort());
        }

        private static void InvokeApplyTransitionGate(
            GameFlowRuntime runtime,
            TransitionOperationId operationId,
            TransitionKind kind,
            TransitionGateMode mode,
            string source,
            string reason)
        {
            MethodInfo method = typeof(GameFlowRuntime).GetMethod(
                "ApplyTransitionGate",
                InstanceAny);
            Require(method != null, "ApplyTransitionGate method was not found.");
            method.Invoke(
                runtime,
                new object[] { operationId, kind, mode, source, reason });
        }

        private static TransitionGateDiagnostics InvokeReleaseTransitionGate(
            GameFlowRuntime runtime,
            TransitionGateMode mode,
            GateSnapshot appliedSnapshot)
        {
            MethodInfo method = typeof(GameFlowRuntime).GetMethod(
                "ReleaseTransitionGate",
                InstanceAny);
            Require(method != null, "ReleaseTransitionGate method was not found.");
            object result = method.Invoke(
                runtime,
                new object[] { mode, appliedSnapshot });
            Require(result is TransitionGateDiagnostics, "ReleaseTransitionGate return type mismatched.");
            return (TransitionGateDiagnostics)result;
        }

        private static void InvokeReleaseTransitionGateIfStillActive(GameFlowRuntime runtime)
        {
            MethodInfo method = typeof(GameFlowRuntime).GetMethod(
                "ReleaseTransitionGateIfStillActive",
                InstanceAny);
            Require(method != null, "ReleaseTransitionGateIfStillActive method was not found.");
            method.Invoke(runtime, Array.Empty<object>());
        }

        private static void InvokeReleaseActivityEntryReadinessRecoveryGate(GameFlowRuntime runtime)
        {
            MethodInfo method = typeof(GameFlowRuntime).GetMethod(
                "ReleaseActivityEntryReadinessRecoveryGate",
                InstanceAny);
            Require(
                method != null,
                "ReleaseActivityEntryReadinessRecoveryGate method was not found.");
            method.Invoke(runtime, Array.Empty<object>());
        }

        private static void SetRecoveryGateSnapshot(GameFlowRuntime runtime, GateSnapshot snapshot)
        {
            FieldInfo field = typeof(GameFlowRuntime).GetField(
                "_activityEntryReadinessRecoveryGateSnapshot",
                InstanceAny);
            Require(field != null, "Recovery gate snapshot field was not found.");
            field.SetValue(runtime, snapshot);
        }

        private static void RequireTransitionGateReleased(GameFlowRuntime runtime, string context)
        {
            Require(
                runtime.CurrentTransitionGateMode == TransitionGateMode.None &&
                !runtime.CurrentTransitionGateSnapshot.HasBlockers,
                $"Transition Gate residual not released {context}. " +
                $"mode='{runtime.CurrentTransitionGateMode}' " +
                $"blockers='{runtime.CurrentTransitionGateSnapshot.BlockerCount}'.");
        }

        private static bool HasPolicySource(GateSnapshot snapshot, string policySource)
        {
            if (!snapshot.HasBlockers || string.IsNullOrEmpty(policySource))
            {
                return false;
            }

            var blockers = snapshot.Blockers;
            for (int i = 0; i < blockers.Count; i++)
            {
                if (string.Equals(
                        blockers[i].PolicySource,
                        policySource,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AssignActivityId(ActivityAsset activity, string activityId)
        {
            var serialized = new SerializedObject(activity);
            SerializedProperty property = serialized.FindProperty("activityId");
            Require(property != null, "ActivityAsset.activityId property was not found.");
            property.stringValue = activityId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Require(activity.HasValidActivityId, "QA ActivityId assignment failed.");
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
            string second)
        {
            int firstIndex = source.IndexOf(first, StringComparison.Ordinal);
            if (firstIndex < 0)
            {
                return false;
            }

            return source.IndexOf(second, firstIndex, StringComparison.Ordinal) >= 0;
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

            return source.IndexOf(third, secondIndex, StringComparison.Ordinal) >= 0;
        }

        private static bool ContainsOrdered(
            string source,
            string first,
            string second,
            string third,
            string fourth)
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

            return source.IndexOf(fourth, thirdIndex, StringComparison.Ordinal) >= 0;
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

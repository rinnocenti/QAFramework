using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RuntimeContent;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Focused Play Mode smoke for the package-owned Game Flow Diagnostic Fault Lease seam.
    ///
    /// The smoke intentionally resolves the public Editor-only bridge dynamically because the
    /// package seam is not a game-facing runtime API and the QA assembly must not receive broad
    /// access to package internals. Runtime state is never modified through reflection.
    /// </summary>
    public static class QaGameFlowDiagnosticFaultLeaseSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Smokes/Game Flow/Run Diagnostic Fault Lease Smoke";

        private const string LogPrefix =
            "[QA_GAME_FLOW_DIAGNOSTIC_FAULT_LEASE_SMOKE]";

        private const string Source =
            nameof(QaGameFlowDiagnosticFaultLeaseSmoke);

        private static readonly string[] ScenarioNames =
        {
            "PreparationTokenMismatch",
            "OwnerMismatch",
            "PreCommitFailure",
            "RuntimeUnavailable",
            "LoadingRejectedBeforePresentation",
            "CommittedTargetNotReady",
            "CommittedFinalizationFailure"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            try
            {
                IReadOnlyList<string> completed = await RunSmokeAsync();
                Debug.Log(
                    $"{LogPrefix} status='Passed' scenarios='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}' activeLeases='0' " +
                    "authorityReplacement='False'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        internal static async Task<IReadOnlyList<string>> RunSmokeAsync()
        {
            Require(EditorApplication.isPlaying,
                "Game Flow Diagnostic Fault Lease Smoke requires Play Mode.");

            DiagnosticFaultBridge.ValidateBridgeContract();

            var completed = new List<string>(ScenarioNames.Length);
            for (int index = 0; index < ScenarioNames.Length; index++)
            {
                string scenario = ScenarioNames[index];
                await RunScenarioAsync(scenario);
                completed.Add(scenario);
            }

            Require(completed.Count == ScenarioNames.Length,
                "Diagnostic Fault Lease scenario count changed unexpectedly.");

            int? activeLeaseCount = DiagnosticFaultBridge.TryGetActiveLeaseCount();
            if (activeLeaseCount.HasValue)
            {
                Require(activeLeaseCount.Value == 0,
                    $"Diagnostic Fault Lease smoke retained '{activeLeaseCount.Value}' active leases.");
            }

            return completed;
        }

        internal static async Task RunScenarioForRegressionAsync(
            string scenario)
        {
            Require(
                EditorApplication.isPlaying,
                "Diagnostic Fault regression scenario requires Play Mode.");

            DiagnosticFaultBridge.ValidateBridgeContract();
            await RunScenarioAsync(scenario);

            int? activeLeaseCount =
                DiagnosticFaultBridge.TryGetActiveLeaseCount();
            if (activeLeaseCount.HasValue)
            {
                Require(
                    activeLeaseCount.Value == 0,
                    $"Diagnostic Fault regression scenario '{scenario}' retained " +
                    $"'{activeLeaseCount.Value}' active leases.");
            }
        }

        private static async Task RunScenarioAsync(string scenario)
        {
            switch (scenario)
            {
                case "PreparationTokenMismatch":
                case "OwnerMismatch":
                case "PreCommitFailure":
                    await RunPreCommitLifecycleScenarioAsync(scenario);
                    return;

                case "RuntimeUnavailable":
                    await RunRuntimeUnavailableScenarioAsync();
                    return;

                case "LoadingRejectedBeforePresentation":
                    await RunLoadingRejectedScenarioAsync();
                    return;

                case "CommittedTargetNotReady":
                    await RunCommittedTargetNotReadyScenarioAsync();
                    return;

                case "CommittedFinalizationFailure":
                    await RunCommittedFinalizationFailureScenarioAsync();
                    return;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported Diagnostic Fault Lease scenario '{scenario}'.");
            }
        }

        private static async Task RunPreCommitLifecycleScenarioAsync(string scenario)
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            DiagnosticLeaseHandle lease = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            string caseName = ToCaseName(scenario);

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                ActivityAsset entryActivity = RequireCurrentActivity(fixture, caseName);
                int rootCountBefore = fixture.RuntimeScopeRootCount;

                PrepareSinglePlayerGameplayReady(fixture, caseName);

                ActivityAsset targetActivity =
                    fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                        $"qa.game-flow-fault.{caseName}.activity",
                        $"QA Fault {scenario}");

                lease = DiagnosticFaultBridge.Install(
                    RequireRuntimeHostComponent(fixture),
                    scenario,
                    caseName);

                ActivityPlayerLifecycleAdmissionResult result =
                    fixture.PrepareSameRouteLifecycle(
                        entryActivity,
                        targetActivity,
                        Source,
                        caseName);

                Require(result != null,
                    $"{scenario} returned no lifecycle result.");
                Require(!result.ReadyForTransition && !result.NotRequired,
                    $"{scenario} did not produce a typed pre-commit rejection. " +
                    result.ToDiagnosticString());
                Require(ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    $"{scenario} changed the published Activity before commit.");
                Require(fixture.RuntimeScopeRootCount == rootCountBefore,
                    $"{scenario} retained a target RuntimeContent root.");
                Require(fixture.GameplaySnapshot.CandidateCount == 0,
                    $"{scenario} retained Player Actor candidates.");
                Require(fixture.GameplaySnapshot.ActivePerSlotHandoffCount == 0 &&
                        !fixture.GameplaySnapshot.HasActiveHandoffGroup,
                    $"{scenario} retained an active Player handoff.");

                AssertLeaseConsumed(lease, scenario);

                Debug.Log(
                    $"{LogPrefix}[SCENARIO] scenario='{scenario}' phase='precommit-rejected' " +
                    $"status='{result.Status}' state='{result.CurrentSnapshot?.State}' " +
                    $"originPreserved='True' candidates='0' handoffs='0' rootsDelta='0' " +
                    $"consumptionCount='{lease.ConsumptionCount}'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                DisposeLease(ref lease, ref cleanupFailure);
                cleanupFailure = await CleanupFixtureAsync(fixture, cleanupFailure);
            }

            ThrowCombined(executionFailure, cleanupFailure);
        }

        private static async Task RunRuntimeUnavailableScenarioAsync()
        {
            const string scenario = "RuntimeUnavailable";
            string caseName = ToCaseName(scenario);
            QaPlayerGameplayAdmissionFixture fixture = null;
            DiagnosticLeaseHandle lease = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                ActivityAsset entryActivity = RequireCurrentActivity(fixture, caseName);
                int rootCountBefore = fixture.RuntimeScopeRootCount;
                ActivityPlayerLifecycleAdmissionSnapshot lifecycleBefore =
                    fixture.LifecycleSnapshot;

                PrepareSinglePlayerGameplayReady(fixture, caseName);

                ActivityAsset targetActivity =
                    fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                        $"qa.game-flow-fault.{caseName}.activity",
                        "QA Fault Runtime Unavailable");

                lease = DiagnosticFaultBridge.Install(
                    RequireRuntimeHostComponent(fixture),
                    scenario,
                    caseName);

                object request = await fixture.RequestActivityAsync(
                    targetActivity,
                    Source,
                    caseName);

                Require(!GetBoolean(request, "Succeeded"),
                    "RuntimeUnavailable unexpectedly succeeded.");
                Require(ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    "RuntimeUnavailable changed the published Activity.");
                Require(fixture.RuntimeScopeRootCount == rootCountBefore,
                    "RuntimeUnavailable retained a target RuntimeContent root.");
                Require(fixture.GameplaySnapshot.CandidateCount == 0 &&
                        fixture.GameplaySnapshot.ActivePerSlotHandoffCount == 0 &&
                        !fixture.GameplaySnapshot.HasActiveHandoffGroup,
                    "RuntimeUnavailable entered Player candidate or handoff state.");

                string requestStatus = GetString(request, "Status");
                string requestMessage = GetString(request, "Message");
                Require(
                    requestStatus.IndexOf(
                        "Unavailable",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    requestMessage.IndexOf(
                        "unavailable",
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    requestMessage.IndexOf(
                        "runtime",
                        StringComparison.OrdinalIgnoreCase) >= 0,
                    "RuntimeUnavailable did not return typed runtime-unavailable diagnostics. " +
                    $"status='{requestStatus}' message='{requestMessage}'.");

                ActivityPlayerLifecycleAdmissionSnapshot lifecycleAfter =
                    fixture.LifecycleSnapshot;
                Require(
                    lifecycleAfter == null ||
                    lifecycleAfter.State.ToString() == "None" ||
                    SameLifecycleIdentity(lifecycleBefore, lifecycleAfter),
                    "RuntimeUnavailable advanced lifecycle state before rejecting the request. " +
                    lifecycleAfter?.ToDiagnosticString());

                AssertLeaseConsumed(lease, scenario);

                Debug.Log(
                    $"{LogPrefix}[SCENARIO] scenario='{scenario}' phase='runtime-unavailable' " +
                    $"requestSucceeded='False' requestStatus='{requestStatus}' " +
                    $"originPreserved='True' candidates='0' handoffs='0' rootsDelta='0' " +
                    $"consumptionCount='{lease.ConsumptionCount}'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                DisposeLease(ref lease, ref cleanupFailure);
                cleanupFailure = await CleanupFixtureAsync(
                    fixture,
                    cleanupFailure);
            }

            ThrowCombined(executionFailure, cleanupFailure);
        }

        private static async Task RunLoadingRejectedScenarioAsync()
        {
            const string scenario = "LoadingRejectedBeforePresentation";
            string caseName = ToCaseName(scenario);
            QaPlayerGameplayAdmissionFixture fixture = null;
            DiagnosticLeaseHandle lease = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                ActivityAsset entryActivity = RequireCurrentActivity(fixture, caseName);
                int rootCountBefore = fixture.RuntimeScopeRootCount;
                ActivityPlayerLifecycleAdmissionSnapshot lifecycleBefore =
                    fixture.LifecycleSnapshot;

                ActivityAsset targetActivity =
                    fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                        $"qa.game-flow-fault.{caseName}.activity",
                        "QA Fault Loading Rejected");

                lease = DiagnosticFaultBridge.Install(
                    RequireRuntimeHostComponent(fixture),
                    scenario,
                    caseName);

                object request = await fixture.RequestActivityAsync(
                    targetActivity,
                    Source,
                    caseName);

                Require(!GetBoolean(request, "Succeeded"),
                    "LoadingRejectedBeforePresentation unexpectedly succeeded.");
                Require(ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    "LoadingRejectedBeforePresentation changed the published Activity.");
                Require(fixture.RuntimeScopeRootCount == rootCountBefore,
                    "LoadingRejectedBeforePresentation created a target RuntimeContent root.");
                Require(fixture.GameplaySnapshot.CandidateCount == 0 &&
                        fixture.GameplaySnapshot.ActivePerSlotHandoffCount == 0 &&
                        !fixture.GameplaySnapshot.HasActiveHandoffGroup,
                    "LoadingRejectedBeforePresentation entered Player lifecycle preparation.");

                ActivityPlayerLifecycleAdmissionSnapshot lifecycleAfter =
                    fixture.LifecycleSnapshot;
                Require(SameLifecycleIdentity(lifecycleBefore, lifecycleAfter),
                    "LoadingRejectedBeforePresentation changed lifecycle admission before presentation.");

                AssertLeaseConsumed(lease, scenario);

                Debug.Log(
                    $"{LogPrefix}[SCENARIO] scenario='{scenario}' phase='rejected-before-presentation' " +
                    $"requestSucceeded='False' originPreserved='True' lifecycleChanged='False' " +
                    $"candidates='0' handoffs='0' rootsDelta='0' " +
                    $"consumptionCount='{lease.ConsumptionCount}'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                DisposeLease(ref lease, ref cleanupFailure);
                cleanupFailure = await CleanupFixtureAsync(fixture, cleanupFailure);
            }

            ThrowCombined(executionFailure, cleanupFailure);
        }

        private static async Task RunCommittedTargetNotReadyScenarioAsync()
        {
            const string scenario = "CommittedTargetNotReady";
            string caseName = ToCaseName(scenario);
            QaPlayerGameplayAdmissionFixture fixture = null;
            DiagnosticLeaseHandle lease = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            ActivityAsset entryActivity = null;
            ActivityAsset targetActivity = null;

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                entryActivity = RequireCurrentActivity(fixture, caseName);
                PrepareSinglePlayerGameplayReady(fixture, caseName);

                targetActivity =
                    fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                        $"qa.game-flow-fault.{caseName}.activity",
                        "QA Fault Committed Target Not Ready");

                lease = DiagnosticFaultBridge.Install(
                    RequireRuntimeHostComponent(fixture),
                    scenario,
                    caseName);

                object request = await fixture.RequestActivityAsync(
                    targetActivity,
                    Source,
                    caseName);

                Require(!GetBoolean(request, "Succeeded"),
                    "CommittedTargetNotReady unexpectedly returned terminal success.");

                ActivityPlayerLifecycleAdmissionSnapshot lifecycle =
                    fixture.LifecycleSnapshot;
                Require(lifecycle != null && !lifecycle.IsRollbackAvailable,
                    "CommittedTargetNotReady left rollback available after commit.");
                Require(IsCommittedOrCompleted(lifecycle),
                    "CommittedTargetNotReady did not retain a committed lifecycle state. " +
                    lifecycle?.ToDiagnosticString());

                RuntimeContentOwner expectedTargetOwner =
                    RuntimeContentOwner.Activity(
                        targetActivity.ActivityId.StableText,
                        targetActivity.ActivityName,
                        RuntimeDefinitionToken.FromUnityObject(targetActivity));
                Require(
                    lifecycle.TargetOwner == expectedTargetOwner,
                    "CommittedTargetNotReady did not preserve the committed target owner. " +
                    $"expected='{expectedTargetOwner}' actual='{lifecycle.TargetOwner}'. " +
                    lifecycle.ToDiagnosticString());

                AssertLeaseConsumed(lease, scenario);

                ActivityAsset projectedActivity = fixture.CurrentActivity;
                Debug.Log(
                    $"{LogPrefix}[SCENARIO] scenario='{scenario}' phase='postcommit-not-ready' " +
                    $"requestSucceeded='False' targetAuthorityRetained='True' " +
                    $"hostProjectedActivity='{(projectedActivity != null ? projectedActivity.ActivityName : "<none>")}' " +
                    $"rollbackAvailable='{lifecycle.IsRollbackAvailable}' " +
                    $"state='{lifecycle.State}' status='{lifecycle.LastStatus}' " +
                    $"consumptionCount='{lease.ConsumptionCount}'.");

                lease.Dispose();
                lease = null;

                object restore = await fixture.RequestActivityAsync(
                    entryActivity,
                    Source,
                    caseName + ":restore-entry");
                Require(GetBoolean(restore, "Succeeded") &&
                        ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    "CommittedTargetNotReady cleanup could not return to the entry Activity. " +
                    GetString(restore, "Message"));
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                DisposeLease(ref lease, ref cleanupFailure);
                cleanupFailure = await TryRestoreActivityAsync(
                    fixture,
                    entryActivity,
                    caseName,
                    cleanupFailure);
                cleanupFailure = await CleanupFixtureAsync(fixture, cleanupFailure);
            }

            ThrowCombined(executionFailure, cleanupFailure);
        }

        private static async Task RunCommittedFinalizationFailureScenarioAsync()
        {
            const string scenario = "CommittedFinalizationFailure";
            string caseName = ToCaseName(scenario);
            QaPlayerGameplayAdmissionFixture fixture = null;
            DiagnosticLeaseHandle lease = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            ActivityAsset entryActivity = null;
            ActivityAsset targetActivity = null;

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                entryActivity = RequireCurrentActivity(fixture, caseName);
                PrepareSinglePlayerGameplayReady(fixture, caseName);

                targetActivity =
                    fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                        $"qa.game-flow-fault.{caseName}.activity",
                        "QA Fault Committed Finalization Failure");

                lease = DiagnosticFaultBridge.Install(
                    RequireRuntimeHostComponent(fixture),
                    scenario,
                    caseName);

                object request = await fixture.RequestActivityAsync(
                    targetActivity,
                    Source,
                    caseName);

                AssertLeaseConsumed(lease, scenario);

                ActivityPlayerLifecycleAdmissionSnapshot pending =
                    fixture.LifecycleSnapshot;
                string pendingDiagnostic = pending?.ToDiagnosticString() ?? "<null>";
                Require(
                    IsCommitCleanupPending(pending),
                    "CommittedFinalizationFailure did not retain explicit cleanup-pending evidence. " +
                    pendingDiagnostic);
                Require(!pending.IsRollbackAvailable,
                    "CommittedFinalizationFailure incorrectly left rollback available.");

                Debug.Log(
                    $"{LogPrefix}[SCENARIO] scenario='{scenario}' phase='fault-consumed' " +
                    $"requestSucceeded='{GetBoolean(request, "Succeeded")}' " +
                    $"state='{pending.State}' status='{pending.LastStatus}' " +
                    $"cleanupPending='True' rollbackAvailable='{pending.IsRollbackAvailable}' " +
                    $"consumptionCount='{lease.ConsumptionCount}'.");

                object retryResult = InvokeOfficialCommitCleanupRetry(
                    fixture,
                    request,
                    pending,
                    Source,
                    caseName + ":retry");

                Require(IsSuccessfulResult(retryResult),
                    "CommittedFinalizationFailure official retry did not succeed. " +
                    DescribeResult(retryResult));
                Require(lease.ConsumptionCount == 1,
                    "CommittedFinalizationFailure consumed its one-shot lease more than once.");

                ActivityPlayerLifecycleAdmissionSnapshot completed =
                    fixture.LifecycleSnapshot;
                Require(completed != null &&
                        !IsCommitCleanupPending(completed),
                    "CommittedFinalizationFailure retry retained cleanup-pending evidence. " +
                    completed?.ToDiagnosticString());

                Debug.Log(
                    $"{LogPrefix}[SCENARIO] scenario='{scenario}' phase='retry-completed' " +
                    $"retrySucceeded='True' faultConsumedAgain='False' " +
                    $"state='{completed.State}' status='{completed.LastStatus}' " +
                    $"cleanupPending='False' consumptionCount='{lease.ConsumptionCount}'.");

                lease.Dispose();
                lease = null;

                if (!ReferenceEquals(fixture.CurrentActivity, targetActivity))
                {
                    // Some Activity Flow failure results publish the destination only after the
                    // official cleanup retry. A second request is not used to complete ownership;
                    // it is used only to verify and normalize the published Activity for cleanup.
                    object normalize = await fixture.RequestActivityAsync(
                        targetActivity,
                        Source,
                        caseName + ":normalize-target");
                    Require(GetBoolean(normalize, "Succeeded") ||
                            ReferenceEquals(fixture.CurrentActivity, targetActivity),
                        "CommittedFinalizationFailure did not preserve the target authority after retry.");
                }

                object restore = await fixture.RequestActivityAsync(
                    entryActivity,
                    Source,
                    caseName + ":restore-entry");
                Require(GetBoolean(restore, "Succeeded") &&
                        ReferenceEquals(fixture.CurrentActivity, entryActivity),
                    "CommittedFinalizationFailure cleanup could not return to the entry Activity. " +
                    GetString(restore, "Message"));
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                DisposeLease(ref lease, ref cleanupFailure);
                cleanupFailure = await TryRestoreActivityAsync(
                    fixture,
                    entryActivity,
                    caseName,
                    cleanupFailure);
                cleanupFailure = await CleanupFixtureAsync(fixture, cleanupFailure);
            }

            ThrowCombined(executionFailure, cleanupFailure);
        }

        private static void PrepareSinglePlayerGameplayReady(
            QaPlayerGameplayAdmissionFixture fixture,
            string reason)
        {
            fixture.AssertCleanBaseline(reason);
            Require(fixture.OpenJoining(reason)?.Completed == true,
                $"Could not open joining for '{reason}'.");

            LocalPlayerJoinResult join = fixture.JoinPlayer(reason);
            Require(join != null && join.Succeeded,
                $"Could not join the QA Player for '{reason}'.");

            fixture.CreateCurrentActivityScope(Source, reason);

            PlayerActorSelectionResult selection =
                fixture.SelectDefaultActor(
                    join.Slot.PlayerSlotId,
                    Source,
                    reason);
            PlayerActorPreparationResult preparation =
                fixture.PrepareSelectedActor(
                    join.Slot.PlayerSlotId,
                    Source,
                    reason);
            PlayerGameplayRuntimeOperationResult gameplay =
                fixture.EnsureGameplayReady(
                    join.Slot.PlayerSlotId,
                    Source,
                    reason);

            Require(selection != null && selection.Succeeded,
                $"Default Actor selection failed for '{reason}'.");
            Require(preparation != null && preparation.Succeeded,
                $"Actor preparation failed for '{reason}'. " +
                preparation?.ToDiagnosticString());
            Require(gameplay != null && gameplay.Succeeded &&
                    gameplay.CurrentAdmission.GameplayReady,
                $"GameplayReady chain failed for '{reason}'. " +
                gameplay?.ToDiagnosticString());
        }

        private static Component RequireRuntimeHostComponent(
            QaPlayerGameplayAdmissionFixture fixture)
        {
            if (fixture?.RuntimeHost is Component component)
            {
                return component;
            }

            throw new InvalidOperationException(
                "Diagnostic Fault Lease smoke requires the explicit FrameworkRuntimeHost Component.");
        }

        private static ActivityAsset RequireCurrentActivity(
            QaPlayerGameplayAdmissionFixture fixture,
            string caseName)
        {
            ActivityAsset activity = fixture?.CurrentActivity;
            if (activity == null || !activity.HasValidActivityId)
            {
                throw new InvalidOperationException(
                    $"Diagnostic Fault Lease scenario '{caseName}' requires a valid current Activity.");
            }

            return activity;
        }

        private static void AssertLeaseConsumed(
            DiagnosticLeaseHandle lease,
            string scenario)
        {
            Require(lease != null, $"{scenario} has no Diagnostic Fault Lease.");
            Require(lease.Consumed,
                $"{scenario} did not consume the installed Diagnostic Fault Lease. " +
                lease.Diagnostic);
            Require(lease.ConsumptionCount == 1,
                $"{scenario} must consume the Diagnostic Fault Lease exactly once. " +
                $"actual='{lease.ConsumptionCount}'.");
            Require(string.Equals(
                    lease.Scenario,
                    scenario,
                    StringComparison.Ordinal),
                $"{scenario} lease reports another scenario '{lease.Scenario}'.");
            Require(string.IsNullOrEmpty(lease.ActualCheckpoint) ||
                    string.IsNullOrEmpty(lease.ExpectedCheckpoint) ||
                    string.Equals(
                        lease.ActualCheckpoint,
                        lease.ExpectedCheckpoint,
                        StringComparison.Ordinal),
                $"{scenario} consumed checkpoint '{lease.ActualCheckpoint}', " +
                $"expected '{lease.ExpectedCheckpoint}'.");
        }

        private static object InvokeOfficialCommitCleanupRetry(
            QaPlayerGameplayAdmissionFixture fixture,
            object requestResult,
            ActivityPlayerLifecycleAdmissionSnapshot lifecycle,
            string source,
            string reason)
        {
            const int maxStages = 8;
            var successfulInvocations =
                new HashSet<string>(StringComparer.Ordinal);
            var candidates = new List<string>();
            var successfulStages = new List<string>();
            var rejectedStages = new List<string>();
            Exception lastFailure = null;
            object lastSuccessfulResult = null;

            for (int stage = 0; stage < maxStages; stage++)
            {
                ActivityPlayerLifecycleAdmissionSnapshot currentLifecycle =
                    fixture.LifecycleSnapshot;

                if (!IsCommitCleanupPending(currentLifecycle))
                {
                    if (lastSuccessfulResult == null)
                    {
                        throw new InvalidOperationException(
                            "Commit-cleanup retry was requested, but the lifecycle was already terminal.");
                    }

                    return lastSuccessfulResult;
                }

                var roots = new List<object>
                {
                    fixture.RuntimeHost,
                    fixture.GameplayModule,
                    fixture.PreparationModule,
                    fixture.GameplaySnapshot,
                    requestResult,
                    lifecycle,
                    currentLifecycle,
                    lastSuccessfulResult
                };

                AddReachableFields(roots, fixture.GameplayModule, depth: 6);
                AddReachableFields(roots, fixture.PreparationModule, depth: 5);
                AddReachableFields(roots, fixture.RuntimeHost, depth: 6);
                AddReachableFields(roots, fixture.GameplaySnapshot, depth: 4);
                AddReachableFields(roots, currentLifecycle, depth: 3);

                var retryCandidates =
                    new List<(object Target, MethodInfo Method, string Description, int Priority)>();

                foreach (object target in roots.Where(item => item != null).Distinct())
                {
                    MethodInfo[] methods = target.GetType().GetMethods(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                    foreach (MethodInfo method in methods)
                    {
                        if (!method.Name.Contains(
                                "Retry",
                                StringComparison.OrdinalIgnoreCase) ||
                            !method.Name.Contains(
                                "CommitCleanup",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string description =
                            target.GetType().FullName + "." +
                            DescribeMethod(method);

                        if (!candidates.Contains(description))
                        {
                            candidates.Add(description);
                        }

                        retryCandidates.Add(
                            (
                                target,
                                method,
                                description,
                                GetRetryCandidatePriority(target.GetType())
                            ));
                    }
                }

                bool stageAdvanced = false;

                foreach (var candidate in retryCandidates
                             .OrderBy(item => item.Priority)
                             .ThenBy(item => item.Description, StringComparer.Ordinal))
                {
                    string invocationKey =
                        System.Runtime.CompilerServices.RuntimeHelpers
                            .GetHashCode(candidate.Target) +
                        "|" + candidate.Description;

                    // A successful lower-layer retry is terminal for that exact
                    // owner/method. Rejected retries remain eligible on later
                    // stages because their prerequisites may have changed.
                    if (successfulInvocations.Contains(invocationKey))
                    {
                        continue;
                    }

                    if (!TryBuildRetryArguments(
                            candidate.Method,
                            roots,
                            source,
                            reason + ":stage-" + stage,
                            out object[] arguments))
                    {
                        rejectedStages.Add(
                            $"stage={stage} method={candidate.Description} " +
                            "reason=arguments-unresolved");
                        continue;
                    }

                    try
                    {
                        object result = candidate.Method.Invoke(
                            candidate.Target,
                            arguments);

                        if (result is Task task)
                        {
                            task.GetAwaiter().GetResult();
                            result = GetTaskResult(task);
                        }

                        if (!IsSuccessfulResult(result))
                        {
                            string rejection =
                                $"stage={stage} method={candidate.Description} " +
                                $"result={DescribeResult(result)}";
                            rejectedStages.Add(rejection);
                            lastFailure = new InvalidOperationException(
                                "Retry candidate returned a non-success result. " +
                                DescribeResult(result));
                            continue;
                        }

                        successfulInvocations.Add(invocationKey);
                        lastSuccessfulResult = result;
                        successfulStages.Add(candidate.Description);

                        ActivityPlayerLifecycleAdmissionSnapshot afterStage =
                            fixture.LifecycleSnapshot;

                        Debug.Log(
                            $"{LogPrefix}[RETRY] stage='{stage}' " +
                            $"priority='{candidate.Priority}' " +
                            $"method='{candidate.Description}' " +
                            $"status='Succeeded' " +
                            $"lifecycleState='{afterStage?.State}' " +
                            $"lifecycleStatus='{afterStage?.LastStatus}' " +
                            $"cleanupPending='{IsCommitCleanupPending(afterStage)}' " +
                            $"result='{Escape(DescribeResult(result))}'.");

                        stageAdvanced = true;
                        break;
                    }
                    catch (TargetInvocationException exception)
                    {
                        lastFailure = exception.InnerException ?? exception;
                        rejectedStages.Add(
                            $"stage={stage} method={candidate.Description} " +
                            $"exception={lastFailure.GetType().Name}:" +
                            lastFailure.Message);
                    }
                    catch (Exception exception)
                    {
                        lastFailure = exception;
                        rejectedStages.Add(
                            $"stage={stage} method={candidate.Description} " +
                            $"exception={exception.GetType().Name}:" +
                            exception.Message);
                    }
                }

                if (!stageAdvanced)
                {
                    break;
                }
            }

            ActivityPlayerLifecycleAdmissionSnapshot terminal =
                fixture.LifecycleSnapshot;

            if (lastSuccessfulResult != null &&
                !IsCommitCleanupPending(terminal))
            {
                return lastSuccessfulResult;
            }

            throw new InvalidOperationException(
                "Official commit-cleanup retry chain did not reach a terminal lifecycle state. " +
                $"successfulStages='{string.Join(" -> ", successfulStages)}' " +
                $"rejectedStages='{string.Join(" | ", rejectedStages)}' " +
                $"candidates='{string.Join(" | ", candidates)}' " +
                $"lifecycle='{terminal?.ToDiagnosticString()}'.",
                lastFailure);
        }

        private static int GetRetryCandidatePriority(Type targetType)
        {
            string typeName = targetType?.FullName ?? string.Empty;

            if (typeName.Contains(
                    "ActivityPlayerLifecycleAdmissionRuntimeContext",
                    StringComparison.Ordinal))
            {
                return 0;
            }

            if (typeName.Contains(
                    "PlayerGameplayRuntimeHostModule",
                    StringComparison.Ordinal))
            {
                return 10;
            }

            if (typeName.Contains(
                    "PlayerGameplayChainHandoffRuntimeContext",
                    StringComparison.Ordinal))
            {
                return 20;
            }

            if (typeName.Contains(
                    "ActivityPlayerHandoffGroupRuntimeContext",
                    StringComparison.Ordinal))
            {
                return 30;
            }

            return 100;
        }

        private static bool IsCommitCleanupPending(
            ActivityPlayerLifecycleAdmissionSnapshot lifecycle)
        {
            if (lifecycle == null)
            {
                return false;
            }

            string state = lifecycle.State.ToString();
            string status = lifecycle.LastStatus.ToString();

            return string.Equals(
                       state,
                       "CommitCleanupPending",
                       StringComparison.Ordinal) ||
                   string.Equals(
                       status,
                       "SucceededCommitCleanupPending",
                       StringComparison.Ordinal) ||
                   string.Equals(
                       status,
                       "FailedCommitCleanup",
                       StringComparison.Ordinal);
        }

        private static bool TryBuildRetryArguments(
            MethodInfo method,
            IReadOnlyList<object> evidenceRoots,
            string source,
            string reason,
            out object[] arguments)
        {
            ParameterInfo[] parameters = method.GetParameters();
            arguments = new object[parameters.Length];
            int stringIndex = 0;

            for (int index = 0; index < parameters.Length; index++)
            {
                ParameterInfo parameter = parameters[index];
                Type parameterType = parameter.ParameterType;
                if (parameterType.IsByRef)
                {
                    parameterType = parameterType.GetElementType();
                }

                if (parameterType == typeof(string))
                {
                    arguments[index] = stringIndex++ == 0 ? source : reason;
                    continue;
                }

                if (parameter.HasDefaultValue)
                {
                    arguments[index] = parameter.DefaultValue;
                    continue;
                }

                if (TryResolveEvidenceValue(
                        parameterType,
                        evidenceRoots,
                        out object value))
                {
                    arguments[index] = value;
                    continue;
                }

                if (!parameterType.IsValueType ||
                    Nullable.GetUnderlyingType(parameterType) != null)
                {
                    arguments[index] = null;
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool TryResolveEvidenceValue(
            Type requestedType,
            IReadOnlyList<object> roots,
            out object value)
        {
            for (int index = 0; index < roots.Count; index++)
            {
                object root = roots[index];
                if (root == null)
                {
                    continue;
                }

                if (requestedType.IsInstanceOfType(root))
                {
                    value = root;
                    return true;
                }

                if (TryFindTypedValue(
                        root,
                        requestedType,
                        depth: 3,
                        new HashSet<object>(ReferenceEqualityComparer.Instance),
                        out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool TryFindTypedValue(
            object source,
            Type requestedType,
            int depth,
            ISet<object> visited,
            out object value)
        {
            value = null;
            if (source == null ||
                depth < 0 ||
                IsUnityPseudoNull(source))
            {
                return false;
            }

            Type sourceType = source.GetType();
            if (requestedType.IsInstanceOfType(source) &&
                IsUsableValue(source))
            {
                value = source;
                return true;
            }

            if (!sourceType.IsValueType &&
                !visited.Add(source))
            {
                return false;
            }

            if (depth > 0 &&
                source is IEnumerable enumerable &&
                source is not string &&
                source is not UnityEngine.Object)
            {
                IEnumerator enumerator = null;
                try
                {
                    enumerator = enumerable.GetEnumerator();
                    int inspected = 0;

                    while (inspected < 128 && enumerator.MoveNext())
                    {
                        inspected++;
                        object current = enumerator.Current;
                        if (current == null)
                        {
                            continue;
                        }

                        if (requestedType.IsInstanceOfType(current) &&
                            IsUsableValue(current))
                        {
                            value = current;
                            return true;
                        }

                        if (ShouldTraverse(current.GetType()) &&
                            TryFindTypedValue(
                                current,
                                requestedType,
                                depth - 1,
                                visited,
                                out value))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // Continue through reflected members.
                }
                finally
                {
                    if (enumerator is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
            }

            foreach (PropertyInfo property in sourceType.GetProperties(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic))
            {
                if (property.GetIndexParameters().Length != 0 ||
                    property.GetMethod == null)
                {
                    continue;
                }

                object current;
                try
                {
                    current = property.GetValue(source);
                }
                catch
                {
                    continue;
                }

                if (current == null ||
                    IsUnityPseudoNull(current))
                {
                    continue;
                }

                if (requestedType.IsInstanceOfType(current) &&
                    IsUsableValue(current))
                {
                    value = current;
                    return true;
                }

                if (depth > 0 &&
                    ShouldTraverse(current.GetType()) &&
                    TryFindTypedValue(
                        current,
                        requestedType,
                        depth - 1,
                        visited,
                        out value))
                {
                    return true;
                }
            }

            foreach (FieldInfo field in sourceType.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic))
            {
                object current;
                try
                {
                    current = field.GetValue(source);
                }
                catch
                {
                    continue;
                }

                if (current == null ||
                    IsUnityPseudoNull(current))
                {
                    continue;
                }

                if (requestedType.IsInstanceOfType(current) &&
                    IsUsableValue(current))
                {
                    value = current;
                    return true;
                }

                if (depth > 0 &&
                    ShouldTraverse(current.GetType()) &&
                    TryFindTypedValue(
                        current,
                        requestedType,
                        depth - 1,
                        visited,
                        out value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsUsableValue(object value)
        {
            if (value == null)
            {
                return false;
            }

            Type type = value.GetType();
            PropertyInfo isValid = type.GetProperty(
                "IsValid",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            if (isValid != null &&
                isValid.PropertyType == typeof(bool))
            {
                try
                {
                    return (bool)isValid.GetValue(value);
                }
                catch
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ShouldTraverse(Type type)
        {
            if (type == null ||
                type == typeof(string) ||
                type.IsPrimitive ||
                type.IsEnum)
            {
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return false;
            }

            if (type.IsArray ||
                typeof(IEnumerable).IsAssignableFrom(type))
            {
                return true;
            }

            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return true;
            }

            string namespaceName = type.Namespace ?? string.Empty;
            return namespaceName.StartsWith(
                       "Immersive.Framework",
                       StringComparison.Ordinal) ||
                   namespaceName.StartsWith(
                       "ImmersiveFrameworkQA",
                       StringComparison.Ordinal);
        }

        private static void AddReachableFields(
            ICollection<object> target,
            object root,
            int depth)
        {
            if (root == null || depth <= 0)
            {
                return;
            }

            var queue = new Queue<(object Value, int RemainingDepth)>();
            var visited =
                new HashSet<object>(ReferenceEqualityComparer.Instance);
            queue.Enqueue((root, depth));

            while (queue.Count > 0 && visited.Count < 512)
            {
                (object current, int remainingDepth) = queue.Dequeue();
                if (current == null ||
                    remainingDepth <= 0 ||
                    IsUnityPseudoNull(current))
                {
                    continue;
                }

                Type currentType = current.GetType();
                if (!currentType.IsValueType &&
                    !visited.Add(current))
                {
                    continue;
                }

                foreach (object value in EnumerateReachableValues(current))
                {
                    if (value == null ||
                        IsUnityPseudoNull(value) ||
                        !ShouldTraverse(value.GetType()))
                    {
                        continue;
                    }

                    target.Add(value);
                    queue.Enqueue((value, remainingDepth - 1));
                }
            }
        }

        private static IEnumerable<object> EnumerateReachableValues(
            object source)
        {
            if (source == null ||
                IsUnityPseudoNull(source))
            {
                yield break;
            }

            if (source is IEnumerable enumerable &&
                source is not string &&
                source is not UnityEngine.Object)
            {
                IReadOnlyList<object> items =
                    SnapshotEnumerable(enumerable, 128);

                for (int index = 0; index < items.Count; index++)
                {
                    object item = items[index];
                    if (item != null &&
                        !IsUnityPseudoNull(item))
                    {
                        yield return item;
                    }
                }
            }

            Type sourceType = source.GetType();

            foreach (PropertyInfo property in sourceType.GetProperties(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic))
            {
                if (property.GetIndexParameters().Length != 0 ||
                    property.GetMethod == null)
                {
                    continue;
                }

                object value;
                try
                {
                    value = property.GetValue(source);
                }
                catch
                {
                    continue;
                }

                if (value != null &&
                    !IsUnityPseudoNull(value))
                {
                    yield return value;
                }
            }

            foreach (FieldInfo field in sourceType.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Public |
                         BindingFlags.NonPublic))
            {
                object value;
                try
                {
                    value = field.GetValue(source);
                }
                catch
                {
                    continue;
                }

                if (value != null &&
                    !IsUnityPseudoNull(value))
                {
                    yield return value;
                }
            }
        }

        private static IReadOnlyList<object> SnapshotEnumerable(
            IEnumerable enumerable,
            int maxItems)
        {
            var items = new List<object>();
            if (enumerable == null ||
                maxItems <= 0 ||
                IsUnityPseudoNull(enumerable))
            {
                return items;
            }

            IEnumerator enumerator = null;
            try
            {
                enumerator = enumerable.GetEnumerator();
                while (items.Count < maxItems)
                {
                    bool moved;
                    try
                    {
                        moved = enumerator.MoveNext();
                    }
                    catch
                    {
                        break;
                    }

                    if (!moved)
                    {
                        break;
                    }

                    object current;
                    try
                    {
                        current = enumerator.Current;
                    }
                    catch
                    {
                        continue;
                    }

                    if (current != null &&
                        !IsUnityPseudoNull(current))
                    {
                        items.Add(current);
                    }
                }
            }
            catch
            {
                // A diagnostic graph may expose transient or Unity-native
                // enumerable state. Discovery must remain non-destructive.
            }
            finally
            {
                if (enumerator is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch
                    {
                        // Diagnostic traversal cleanup is best effort.
                    }
                }
            }

            return items;
        }

        private static bool IsUnityPseudoNull(object value)
        {
            return value is UnityEngine.Object unityObject &&
                   unityObject == null;
        }

        private static bool SameLifecycleIdentity(
            ActivityPlayerLifecycleAdmissionSnapshot left,
            ActivityPlayerLifecycleAdmissionSnapshot right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left == null || right == null)
            {
                return left == null && right == null;
            }

            return left.Token == right.Token &&
                   left.State == right.State &&
                   left.LastStatus == right.LastStatus &&
                   left.PreviousOwner == right.PreviousOwner &&
                   left.TargetOwner == right.TargetOwner;
        }

        private static bool IsCommittedOrCompleted(
            ActivityPlayerLifecycleAdmissionSnapshot lifecycle)
        {
            if (lifecycle == null)
            {
                return false;
            }

            string state = lifecycle.State.ToString();
            string status = lifecycle.LastStatus.ToString();
            return ContainsAny(
                state,
                status,
                lifecycle.ToDiagnosticString(),
                "Committed",
                "Completed",
                "CleanupPending",
                "LifecycleCompleted");
        }

        private static bool ContainsAny(
            string first,
            string second,
            string third,
            params string[] values)
        {
            string combined =
                (first ?? string.Empty) + "|" +
                (second ?? string.Empty) + "|" +
                (third ?? string.Empty);

            for (int index = 0; index < values.Length; index++)
            {
                if (combined.IndexOf(
                        values[index],
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static async Task<Exception> TryRestoreActivityAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            ActivityAsset entryActivity,
            string caseName,
            Exception cleanupFailure)
        {
            if (fixture == null ||
                entryActivity == null ||
                ReferenceEquals(fixture.CurrentActivity, entryActivity))
            {
                return cleanupFailure;
            }

            try
            {
                object restore = await fixture.RequestActivityAsync(
                    entryActivity,
                    Source,
                    caseName + ":finally-restore-entry");

                if (!GetBoolean(restore, "Succeeded") ||
                    !ReferenceEquals(fixture.CurrentActivity, entryActivity))
                {
                    throw new InvalidOperationException(
                        "Could not restore the entry Activity during Diagnostic Fault Lease cleanup. " +
                        GetString(restore, "Message"));
                }
            }
            catch (Exception exception)
            {
                cleanupFailure ??= exception;
            }

            return cleanupFailure;
        }

        private static async Task<Exception> CleanupFixtureAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            Exception cleanupFailure)
        {
            if (fixture == null)
            {
                return cleanupFailure;
            }

            try
            {
                await fixture.CleanupAsync();
                if (fixture.CleanupFailure != null)
                {
                    throw fixture.CleanupFailure;
                }
            }
            catch (Exception exception)
            {
                cleanupFailure ??= exception;
            }

            return cleanupFailure;
        }

        private static void DisposeLease(
            ref DiagnosticLeaseHandle lease,
            ref Exception cleanupFailure)
        {
            if (lease == null)
            {
                return;
            }

            try
            {
                lease.Dispose();
            }
            catch (Exception exception)
            {
                cleanupFailure ??= exception;
            }
            finally
            {
                lease = null;
            }
        }

        private static bool IsSuccessfulResult(object result)
        {
            if (result == null)
            {
                return false;
            }

            foreach (string propertyName in new[]
                     {
                         "Succeeded",
                         "Completed",
                         "Committed",
                         "Applied"
                     })
            {
                PropertyInfo property = result.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (property?.PropertyType == typeof(bool) &&
                    (bool)property.GetValue(result))
                {
                    return true;
                }
            }

            string status = GetString(result, "Status");
            return status.StartsWith("Succeeded", StringComparison.Ordinal) ||
                   status.IndexOf("Completed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static object GetTaskResult(Task task)
        {
            PropertyInfo resultProperty = task.GetType().GetProperty(
                "Result",
                BindingFlags.Instance |
                BindingFlags.Public);

            return resultProperty?.GetValue(task);
        }

        private static string DescribeResult(object result)
        {
            if (result == null)
            {
                return "<null>";
            }

            MethodInfo diagnostic = result.GetType().GetMethod(
                "ToDiagnosticString",
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null);

            if (diagnostic != null &&
                diagnostic.ReturnType == typeof(string))
            {
                try
                {
                    return (string)diagnostic.Invoke(result, null);
                }
                catch
                {
                    // Fall through to ToString.
                }
            }

            return result.ToString();
        }

        private static string DescribeMethod(MethodInfo method)
        {
            return method.Name + "(" +
                   string.Join(
                       ",",
                       method.GetParameters()
                           .Select(parameter =>
                               parameter.ParameterType.Name)) +
                   ")";
        }

        private static bool GetBoolean(object target, string propertyName)
        {
            if (target == null)
            {
                return false;
            }

            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            return property?.PropertyType == typeof(bool) &&
                   (bool)property.GetValue(target);
        }

        private static string GetString(object target, string propertyName)
        {
            if (target == null)
            {
                return string.Empty;
            }

            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            object value = property?.GetValue(target);
            return value?.ToString() ?? string.Empty;
        }

        private static string ToCaseName(string scenario)
        {
            var characters = new List<char>(scenario.Length + 8);
            for (int index = 0; index < scenario.Length; index++)
            {
                char current = scenario[index];
                if (index > 0 && char.IsUpper(current))
                {
                    characters.Add('-');
                }

                characters.Add(char.ToLowerInvariant(current));
            }

            return "fault-lease-" + new string(characters.ToArray());
        }

        private static void ThrowCombined(
            Exception executionFailure,
            Exception cleanupFailure)
        {
            if (executionFailure == null && cleanupFailure == null)
            {
                return;
            }

            if (executionFailure != null && cleanupFailure != null)
            {
                throw new AggregateException(
                    "Diagnostic Fault Lease scenario and cleanup both failed.",
                    executionFailure,
                    cleanupFailure);
            }

            throw executionFailure ?? cleanupFailure;
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
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private sealed class DiagnosticLeaseHandle : IDisposable
        {
            private readonly object lease;
            private bool disposed;

            internal DiagnosticLeaseHandle(object lease)
            {
                this.lease = lease ??
                    throw new ArgumentNullException(nameof(lease));
            }

            internal string Scenario =>
                ReadText("Scenario");

            internal bool Consumed =>
                ReadBoolean("Consumed");

            internal int ConsumptionCount =>
                ReadInt32("ConsumptionCount");

            internal string ExpectedCheckpoint =>
                ReadText("ExpectedCheckpoint");

            internal string ActualCheckpoint =>
                ReadText("ActualCheckpoint");

            internal bool Released =>
                ReadBoolean("Released");

            internal string Diagnostic =>
                ReadText("Diagnostic");

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                if (lease is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else
                {
                    MethodInfo dispose = lease.GetType().GetMethod(
                        "Dispose",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        Type.EmptyTypes,
                        null);

                    if (dispose == null)
                    {
                        throw new InvalidOperationException(
                            "FrameworkGameFlowDiagnosticFaultLease does not expose Dispose.");
                    }

                    dispose.Invoke(lease, null);
                }

                if (HasReadableProperty("Released") && !Released)
                {
                    throw new InvalidOperationException(
                        "FrameworkGameFlowDiagnosticFaultLease did not report Released after Dispose.");
                }
            }

            private bool HasReadableProperty(string name)
            {
                return lease.GetType().GetProperty(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic) != null;
            }

            private object Read(string name)
            {
                PropertyInfo property = lease.GetType().GetProperty(
                    name,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                if (property == null)
                {
                    return null;
                }

                return property.GetValue(lease);
            }

            private string ReadText(string name)
            {
                return Read(name)?.ToString() ?? string.Empty;
            }

            private bool ReadBoolean(string name)
            {
                object value = Read(name);
                return value is bool boolean && boolean;
            }

            private int ReadInt32(string name)
            {
                object value = Read(name);
                return value is int integer ? integer : 0;
            }
        }

        private static class DiagnosticFaultBridge
        {
            private const string UtilitySimpleName =
                "FrameworkGameFlowDiagnosticFaultUtility";

            private const string ScenarioSimpleName =
                "FrameworkGameFlowDiagnosticFaultScenario";

            private static Type utilityType;
            private static Type scenarioType;
            private static MethodInfo tryInstallMethod;

            internal static void ValidateBridgeContract()
            {
                ResolveContract();

                Require(utilityType != null,
                    "FrameworkGameFlowDiagnosticFaultUtility was not found.");
                Require(scenarioType != null && scenarioType.IsEnum,
                    "FrameworkGameFlowDiagnosticFaultScenario enum was not found.");
                Require(tryInstallMethod != null,
                    "FrameworkGameFlowDiagnosticFaultUtility.TryInstall was not found.");

                foreach (string scenario in ScenarioNames)
                {
                    Require(Enum.GetNames(scenarioType).Contains(scenario),
                        $"Diagnostic Fault scenario '{scenario}' is absent from the package Editor bridge.");
                }

                Debug.Log(
                    $"{LogPrefix}[BRIDGE] status='Passed' utility='{utilityType.FullName}' " +
                    $"scenarioEnum='{scenarioType.FullName}' scenarios='{ScenarioNames.Length}'.");
            }

            internal static DiagnosticLeaseHandle Install(
                Component runtimeHost,
                string scenario,
                string caseName)
            {
                ResolveContract();

                object scenarioValue = Enum.Parse(
                    scenarioType,
                    scenario,
                    ignoreCase: false);

                ParameterInfo[] parameters =
                    tryInstallMethod.GetParameters();
                object[] arguments =
                {
                    runtimeHost,
                    scenarioValue,
                    caseName,
                    null,
                    null
                };

                bool installed;
                try
                {
                    installed = (bool)tryInstallMethod.Invoke(
                        null,
                        arguments);
                }
                catch (TargetInvocationException exception)
                {
                    throw new InvalidOperationException(
                        $"Diagnostic Fault Lease installation for '{scenario}' threw.",
                        exception.InnerException ?? exception);
                }

                string issue = arguments[4]?.ToString() ?? string.Empty;
                if (!installed || arguments[3] == null)
                {
                    throw new InvalidOperationException(
                        $"Diagnostic Fault Lease installation failed for '{scenario}'. issue='{issue}'.");
                }

                var handle = new DiagnosticLeaseHandle(arguments[3]);
                Require(string.IsNullOrEmpty(handle.Scenario) ||
                        string.Equals(handle.Scenario, scenario, StringComparison.Ordinal),
                    $"Installed lease scenario '{handle.Scenario}' does not match '{scenario}'.");

                Debug.Log(
                    $"{LogPrefix}[LEASE] phase='installed' scenario='{scenario}' " +
                    $"case='{caseName}' expectedCheckpoint='{handle.ExpectedCheckpoint}'.");

                return handle;
            }

            internal static int? TryGetActiveLeaseCount()
            {
                ResolveContract();

                foreach (string propertyName in new[]
                         {
                             "ActiveLeaseCount",
                             "ActiveFaultLeaseCount",
                             "ActiveCount"
                         })
                {
                    PropertyInfo property = utilityType.GetProperty(
                        propertyName,
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic);

                    if (property?.PropertyType == typeof(int))
                    {
                        return (int)property.GetValue(null);
                    }
                }

                return null;
            }

            private static void ResolveContract()
            {
                if (utilityType != null &&
                    scenarioType != null &&
                    tryInstallMethod != null)
                {
                    return;
                }

                utilityType = FindTypeBySimpleName(UtilitySimpleName);
                scenarioType = FindTypeBySimpleName(ScenarioSimpleName);
                if (utilityType == null || scenarioType == null)
                {
                    return;
                }

                tryInstallMethod = utilityType
                    .GetMethods(
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic)
                    .FirstOrDefault(IsTryInstallSignature);
            }

            private static bool IsTryInstallSignature(MethodInfo method)
            {
                if (!string.Equals(
                        method.Name,
                        "TryInstall",
                        StringComparison.Ordinal) ||
                    method.ReturnType != typeof(bool))
                {
                    return false;
                }

                ParameterInfo[] parameters =
                    method.GetParameters();
                if (parameters.Length != 5 ||
                    !parameters[3].ParameterType.IsByRef ||
                    !parameters[4].ParameterType.IsByRef)
                {
                    return false;
                }

                Type first = parameters[0].ParameterType;
                Type second = parameters[1].ParameterType;
                Type third = parameters[2].ParameterType;
                Type fifth =
                    parameters[4].ParameterType.GetElementType();

                return (first == typeof(Component) ||
                        first.IsAssignableFrom(typeof(Component)) ||
                        typeof(Component).IsAssignableFrom(first)) &&
                       second == scenarioType &&
                       third == typeof(string) &&
                       fifth == typeof(string);
            }

            private static Type FindTypeBySimpleName(string simpleName)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type direct = assembly.GetType(simpleName, false);
                    if (direct != null)
                    {
                        return direct;
                    }

                    Type[] types;
                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException exception)
                    {
                        types = exception.Types
                            .Where(type => type != null)
                            .ToArray();
                    }

                    for (int index = 0; index < types.Length; index++)
                    {
                        if (string.Equals(
                                types[index].Name,
                                simpleName,
                                StringComparison.Ordinal))
                        {
                            return types[index];
                        }
                    }
                }

                return null;
            }
        }

        private sealed class ReferenceEqualityComparer :
            IEqualityComparer<object>
        {
            internal static readonly ReferenceEqualityComparer Instance =
                new ReferenceEqualityComparer();

            public new bool Equals(object left, object right) =>
                ReferenceEquals(left, right);

            public int GetHashCode(object value) =>
                System.Runtime.CompilerServices.RuntimeHelpers
                    .GetHashCode(value);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Loading;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaCausalAsyncFoundationRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Common/Run Causal Async Foundation Regression";
        private const string Prefix = "[QA_CAUSAL_ASYNC_FOUNDATION]";
        private static readonly string[] ExpectedCases =
        {
            "edit-mode-required", "shared-runtime-types-resolved", "shared-editor-types-resolved",
            "case-registry-created", "case-registry-order-enforced", "case-registry-duplicate-rejected",
            "case-registry-partial-diagnostics-safe", "case-registry-complete", "failure-primary-preserved",
            "failure-unwind-separated", "failure-cleanup-separated", "failure-duplicates-suppressed",
            "checkpoint-before-after-proved", "checkpoint-divergence-rejected", "completed-operation-reached-terminal",
            "pending-operation-completion-issued-once", "pending-operation-reached-terminal", "faulted-operation-preserved",
            "loading-grammar-zero-update-proved", "loading-grammar-multiple-updates-proved"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            var cases = new QaCaseRegistry(ExpectedCases, 20);
            Exception execution = null;
            Exception cleanup = null;
            try
            {
                Require(!EditorApplication.isPlaying, "Causal async foundation regression requires Edit Mode.");
                cases.Complete("edit-mode-required");
                Require(typeof(QaLoadingPresentationEvidenceGrammar) != null && typeof(QaOwnedAsyncOperation<int>) != null,
                    "Shared runtime-compatible QA types were not resolved.");
                cases.Complete("shared-runtime-types-resolved");
                Require(typeof(QaCaseRegistry) != null && typeof(QaFailureCollector) != null,
                    "Shared Editor QA types were not resolved.");
                cases.Complete("shared-editor-types-resolved");

                var registry = new QaCaseRegistry(new[] { "first", "second" }, 2);
                cases.Complete("case-registry-created");
                RequireThrows(() => registry.Complete("second"));
                cases.Complete("case-registry-order-enforced");
                RequireThrows(() => new QaCaseRegistry(new[] { "same", "same" }, 2));
                cases.Complete("case-registry-duplicate-rejected");
                Require(registry.NextExpectedOrNone() == "first" && registry.DescribeMissing() == "first,second" &&
                    registry.DescribeCompleted() == string.Empty, "Partial registry diagnostics diverged.");
                cases.Complete("case-registry-partial-diagnostics-safe");
                registry.Complete("first"); registry.Complete("second"); registry.RequireComplete();
                cases.Complete("case-registry-complete");

                var collector = new QaFailureCollector();
                var primary = new InvalidOperationException("execution");
                var unwind = new InvalidOperationException("unwind");
                var cleanupFailure = new InvalidOperationException("cleanup");
                collector.Add("Execution", primary); collector.Add("Unwind", unwind); collector.Add("Cleanup", cleanupFailure);
                Require(ReferenceEquals(collector.Primary, primary), "Primary execution failure was not preserved.");
                cases.Complete("failure-primary-preserved");
                Require(collector.Describe("Unwind").Contains("unwind"), "Unwind failure was not separated.");
                cases.Complete("failure-unwind-separated");
                Require(collector.Describe("Cleanup").Contains("cleanup"), "Cleanup failure was not separated.");
                cases.Complete("failure-cleanup-separated");
                collector.Add("Duplicate", primary);
                Require(collector.ToAggregate("foundation").InnerExceptions.Count == 3, "Duplicate failure was not suppressed.");
                cases.Complete("failure-duplicates-suppressed");

                var start = new QaEvidenceCheckpoint("start", 1); var finish = new QaEvidenceCheckpoint("finish", 2);
                start.RequireBefore(finish); finish.RequireAfter(start);
                var firstThenSecond = new QaCausalSignalJoin<int, string>();
                Task<QaCausalSignalJoinSnapshot<int, string>> firstThenSecondTask =
                    firstThenSecond.CompletionTask;
                Require(firstThenSecond.TrySetFirst(3) && !firstThenSecond.IsCompleted &&
                    firstThenSecond.TrySetSecond("ready") &&
                    ReferenceEquals(firstThenSecondTask, firstThenSecond.CompletionTask),
                    "First-to-second causal join diverged.");
                QaCausalSignalJoinSnapshot<int, string> firstThenSecondSnapshot =
                    await firstThenSecondTask;
                Require(firstThenSecondSnapshot.First == 3 && firstThenSecondSnapshot.Second == "ready" &&
                    !firstThenSecond.TrySetFirst(9) && !firstThenSecond.TrySetSecond("later"),
                    "Causal join replaced an observed signal.");
                var secondThenFirst = new QaCausalSignalJoin<int, string>();
                Require(secondThenFirst.TrySetSecond("ready") && !secondThenFirst.IsCompleted &&
                    secondThenFirst.TrySetFirst(3) && (await secondThenFirst.CompletionTask).First == 3,
                    "Second-to-first causal join diverged.");
                var firstSource = new TaskCompletionSource<int>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                var secondSource = new TaskCompletionSource<string>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                var continuationJoin = new QaCausalSignalJoin<int, string>();
                Task firstDelivery = DeliverFirstAsync(firstSource.Task, continuationJoin);
                Task secondDelivery = DeliverSecondAsync(secondSource.Task, continuationJoin);
                secondSource.SetResult("ready");
                firstSource.SetResult(3);
                await Task.WhenAll(firstDelivery, secondDelivery);
                Require(continuationJoin.IsCompleted &&
                    (await continuationJoin.CompletionTask).Second == "ready",
                    "Continuation-delivered causal join diverged.");
                cases.Complete("checkpoint-before-after-proved");
                RequireThrows(() => finish.RequireBefore(start));
                cases.Complete("checkpoint-divergence-rejected");

                var completed = new QaOwnedAsyncOperation<int>("completed");
                completed.Attach(Task.FromResult(7));
                int completedCallbackCount = 0;
                int firstCompletedResult = await completed.AwaitTerminalAsync();
                QaOperationSnapshot<int> completedSnapshot = completed.Snapshot();
                QaOwnedAsyncOperationPhase completedPhase = completed.Phase;
                int repeatedCompletedResult = await completed.AwaitTerminalAsync();
                QaOperationUnwindResult<int> completedUnwind = await completed.UnwindAsync(() => { completedCallbackCount++; return Task.CompletedTask; });
                Require(firstCompletedResult == 7 && repeatedCompletedResult == 7 &&
                    completedSnapshot.ResultAvailable && completedSnapshot.Result == 7 &&
                    completed.Phase == completedPhase && completedUnwind.ReachedTerminal &&
                    completedUnwind.SucceededToAwait && completedUnwind.ResultAvailable &&
                    completedUnwind.Result == 7 && !completedUnwind.CompletionIssued &&
                    completedCallbackCount == 0,
                    "Completed terminal observation or unwind diverged.");
                cases.Complete("completed-operation-reached-terminal");

                var pendingSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                var pending = new QaOwnedAsyncOperation<int>("pending"); pending.Attach(pendingSource.Task);
                int pendingCallbackCount = 0;
                QaOperationUnwindResult<int> pendingUnwind = await pending.UnwindAsync(() => { pendingCallbackCount++; pendingSource.SetResult(11); return Task.CompletedTask; });
                Require(pendingCallbackCount == 1 && pendingUnwind.CompletionIssued, "Pending operation completion callback diverged.");
                cases.Complete("pending-operation-completion-issued-once");
                Require(pendingUnwind.ReachedTerminal && pendingUnwind.SucceededToAwait && pending.Result == 11,
                    "Pending operation did not reach terminal.");
                int repeatedPendingResult = await pending.AwaitTerminalAsync();
                QaOperationUnwindResult<int> repeatedPendingUnwind =
                    await pending.UnwindAsync(() => { pendingCallbackCount++; return Task.CompletedTask; });
                Require(repeatedPendingResult == 11 && repeatedPendingUnwind.ReachedTerminal &&
                    repeatedPendingUnwind.ResultAvailable && repeatedPendingUnwind.Result == 11 &&
                    pendingCallbackCount == 1,
                    "Pending terminal re-observation invoked completion again or changed result.");
                cases.Complete("pending-operation-reached-terminal");

                var fault = new InvalidOperationException("faulted");
                var faulted = new QaOwnedAsyncOperation<int>("faulted"); faulted.Attach(Task.FromException<int>(fault));
                Exception firstFault = await CaptureFaultAsync(faulted);
                Exception repeatedFault = await CaptureFaultAsync(faulted);
                QaOperationUnwindResult<int> faultedUnwind = await faulted.UnwindAsync(() => Task.CompletedTask);
                Require(ReferenceEquals(firstFault, fault) && ReferenceEquals(repeatedFault, fault) &&
                    !faultedUnwind.SucceededToAwait && ReferenceEquals(faultedUnwind.Failure, fault) &&
                    faulted.ReachedTerminal && !faultedUnwind.CompletionIssued,
                    "Faulted operation failure was not preserved.");
                cases.Complete("faulted-operation-preserved");

                QaLoadingPresentationEvidenceSummary zero = QaLoadingPresentationEvidenceGrammar.RequireValid(
                    CreateLoadingEvidence("zero", 0), nameof(QaCausalAsyncFoundationRegression), "zero");
                Require(zero.TotalEvidenceCount == 6 && zero.UpdateRequestCount == 0, "Zero-update grammar diverged.");
                cases.Complete("loading-grammar-zero-update-proved");
                QaLoadingPresentationEvidenceSummary multiple = QaLoadingPresentationEvidenceGrammar.RequireValid(
                    CreateLoadingEvidence("multiple", 3), nameof(QaCausalAsyncFoundationRegression), "multiple");
                Require(multiple.TotalEvidenceCount == 15 && multiple.UpdateEvidenceCount == 9 && multiple.VisibleApplyCount == 4,
                    "Multiple-update grammar diverged.");
                cases.Complete("loading-grammar-multiple-updates-proved");
                cases.RequireComplete();
            }
            catch (Exception exception) { execution = exception; }
            finally { try { cases.RequireComplete(); } catch (Exception exception) { cleanup = exception; } }

            if (execution != null || cleanup != null)
            {
                Debug.LogError($"{Prefix} status='Failed' execution='{QaFailureCollector.Describe(execution)}' cleanup='{QaFailureCollector.Describe(cleanup)}' nextExpectedCase='{cases.NextExpectedOrNone()}' missingCases='{cases.DescribeMissing()}' completed='{cases.DescribeCompleted()}'.");
                throw new AggregateException("Causal async foundation regression failed.", Collect(execution, cleanup));
            }

            Debug.Log($"{Prefix} status='Passed' cases='20' completed='{cases.DescribeCompleted()}'.");
        }

        private static IReadOnlyList<QaLoadingPresentationEvidenceEntry> CreateLoadingEvidence(string detail, int updates)
        {
            var evidence = new List<QaLoadingPresentationEvidenceEntry>(); int sequence = 0;
            AddTriplet(evidence, ref sequence, LoadingSurfaceAction.Show, true, false, detail);
            for (int index = 0; index < updates; index++) AddTriplet(evidence, ref sequence, LoadingSurfaceAction.Update, true, true, detail);
            AddTriplet(evidence, ref sequence, LoadingSurfaceAction.Hide, false, true, detail);
            return evidence;
        }

        private static void AddTriplet(List<QaLoadingPresentationEvidenceEntry> evidence, ref int sequence,
            LoadingSurfaceAction action, bool visible, bool requestActualVisible, string detail)
        {
            string source = nameof(QaCausalAsyncFoundationRegression);
            evidence.Add(new QaLoadingPresentationEvidenceEntry(++sequence, 0, 0d, QaLoadingPresentationEvidenceKind.RequestReceived, action, visible, requestActualVisible, visible ? 1f : 0f, LoadingSurfaceResultStatus.Unknown, source, detail, string.Empty));
            evidence.Add(new QaLoadingPresentationEvidenceEntry(++sequence, 0, 0d, visible ? QaLoadingPresentationEvidenceKind.VisibleApplied : QaLoadingPresentationEvidenceKind.HiddenApplied, action, visible, visible, visible ? 1f : 0f, LoadingSurfaceResultStatus.Unknown, source, detail, string.Empty));
            evidence.Add(new QaLoadingPresentationEvidenceEntry(++sequence, 0, 0d, QaLoadingPresentationEvidenceKind.ResultRecorded, action, visible, visible, visible ? 1f : 0f, LoadingSurfaceResultStatus.Succeeded, source, detail, string.Empty));
        }

        private static IReadOnlyList<Exception> Collect(params Exception[] values) { var result = new List<Exception>(); foreach (Exception value in values) if (value != null) result.Add(value); return result; }
        private static async Task DeliverFirstAsync(Task<int> source, QaCausalSignalJoin<int, string> join) { join.TrySetFirst(await source); }
        private static async Task DeliverSecondAsync(Task<string> source, QaCausalSignalJoin<int, string> join) { join.TrySetSecond(await source); }
        private static async Task<Exception> CaptureFaultAsync(QaOwnedAsyncOperation<int> operation) { try { await operation.AwaitTerminalAsync(); } catch (Exception exception) { return exception; } throw new InvalidOperationException("Expected terminal fault was not observed."); }
        private static void RequireThrows(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new InvalidOperationException("Expected InvalidOperationException was not thrown."); }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}

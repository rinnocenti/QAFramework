using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.Loading;
using ImmersiveFrameworkQA.UnityBuildSurface;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public enum QaOwnedAsyncOperationPhase
    {
        Created = 0,
        RequestStarted = 1,
        Preparing = 2,
        CoveredObserved = 3,
        RevealObserved = 4,
        ReadinessCompletionRequested = 5,
        AwaitingRequest = 6,
        RequestCompleted = 7,
        Unwinding = 8,
        Terminal = 9
    }

    public sealed class QaOwnedAsyncOperation<TResult>
    {
        private Task<TResult> operation;
        private Task<TResult> terminalObservation;
        private Task<QaOperationUnwindResult<TResult>> unwindObservation;
        private readonly object sync = new object();
        private TResult result;
        private Exception failure;
        private bool resultAvailable;
        private bool completionIssuedDuringUnwind;
        private bool unwindInvoked;

        public QaOwnedAsyncOperation(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Operation name is required.", nameof(name));
            }

            Name = name.Trim();
            Phase = QaOwnedAsyncOperationPhase.Created;
        }

        public string Name { get; }
        public QaOwnedAsyncOperationPhase Phase { get; private set; }
        public bool HasOperation => operation != null;
        public bool IsCompleted => operation != null && operation.IsCompleted;
        public bool ReachedTerminal => Phase == QaOwnedAsyncOperationPhase.Terminal;
        public bool CompletionIssuedDuringUnwind => completionIssuedDuringUnwind;
        public bool ResultAvailable => resultAvailable;
        public TResult Result => resultAvailable
            ? result
            : throw new InvalidOperationException($"Operation '{Name}' has no result.");
        public Exception Failure => failure;
        public bool WasCancelled => operation != null && operation.IsCanceled;
        public Task<TResult> Task => operation ?? throw new InvalidOperationException(
            $"Operation '{Name}' has not been attached.");

        public void Attach(Task<TResult> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            lock (sync)
            {
                if (operation != null)
                {
                    throw new InvalidOperationException($"Operation '{Name}' is already attached.");
                }

                operation = task;
                SetPhase(QaOwnedAsyncOperationPhase.RequestStarted);
            }
        }

        public void SetPhase(QaOwnedAsyncOperationPhase phase)
        {
            lock (sync)
            {
                if (ReachedTerminal && phase != QaOwnedAsyncOperationPhase.Terminal)
                {
                    throw new InvalidOperationException($"Operation '{Name}' is already terminal.");
                }

                Phase = phase;
            }
        }

        public Task<TResult> AwaitTerminalAsync()
        {
            lock (sync)
            {
                RequireAttachedOperation();
                if (terminalObservation != null)
                {
                    return terminalObservation;
                }

                if (ReachedTerminal)
                {
                    return ObserveExistingTerminalResult();
                }

                SetPhase(QaOwnedAsyncOperationPhase.AwaitingRequest);
                terminalObservation = ObserveTerminalAsync();
                return terminalObservation;
            }
        }

        private async Task<TResult> ObserveTerminalAsync()
        {
            try
            {
                result = await operation;
                resultAvailable = true;
                SetPhase(QaOwnedAsyncOperationPhase.RequestCompleted);
                return result;
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }
            finally
            {
                lock (sync)
                {
                    Phase = QaOwnedAsyncOperationPhase.Terminal;
                }
            }
        }

        public Task<QaOperationUnwindResult<TResult>> UnwindAsync(
            Func<Task> completionCallback)
        {
            lock (sync)
            {
                if (operation == null)
                {
                    return System.Threading.Tasks.Task.FromResult(
                        QaOperationUnwindResult<TResult>.None(Phase));
                }

                if (unwindObservation != null)
                {
                    return unwindObservation;
                }

                if (ReachedTerminal || operation.IsCompleted)
                {
                    unwindObservation = ObserveTerminalForUnwindAsync();
                    return unwindObservation;
                }

                if (completionCallback == null)
                {
                    throw new ArgumentNullException(nameof(completionCallback));
                }

                if (unwindInvoked)
                {
                    throw new InvalidOperationException($"Operation '{Name}' unwind was already invoked.");
                }

                unwindInvoked = true;
                SetPhase(QaOwnedAsyncOperationPhase.Unwinding);
                unwindObservation = ExecuteUnwindAsync(completionCallback);
                return unwindObservation;
            }
        }

        private async Task<QaOperationUnwindResult<TResult>> ExecuteUnwindAsync(
            Func<Task> completionCallback)
        {
            await completionCallback();
            completionIssuedDuringUnwind = true;
            return await ObserveTerminalForUnwindAsync();
        }

        private async Task<QaOperationUnwindResult<TResult>> ObserveTerminalForUnwindAsync()
        {
            try
            {
                await AwaitTerminalAsync();
                return new QaOperationUnwindResult<TResult>(true, completionIssuedDuringUnwind,
                    ReachedTerminal, true, false, resultAvailable, result, null, Phase);
            }
            catch (OperationCanceledException exception)
            {
                return new QaOperationUnwindResult<TResult>(true, completionIssuedDuringUnwind,
                    ReachedTerminal, false, true, false, default, exception, Phase);
            }
            catch (Exception exception)
            {
                return new QaOperationUnwindResult<TResult>(true, completionIssuedDuringUnwind,
                    ReachedTerminal, false, false, false, default, exception, Phase);
            }
        }

        private Task<TResult> ObserveExistingTerminalResult()
        {
            if (resultAvailable)
            {
                terminalObservation = System.Threading.Tasks.Task.FromResult(result);
                return terminalObservation;
            }

            if (failure != null)
            {
                terminalObservation = System.Threading.Tasks.Task.FromException<TResult>(failure);
                return terminalObservation;
            }

            if (WasCancelled)
            {
                terminalObservation = operation;
                return terminalObservation;
            }

            throw new InvalidOperationException(
                $"Operation '{Name}' reached terminal without an observable result.");
        }

        private void RequireAttachedOperation()
        {
            if (operation == null)
            {
                throw new InvalidOperationException($"Operation '{Name}' has not been attached.");
            }
        }

        public QaOperationSnapshot<TResult> Snapshot() => new QaOperationSnapshot<TResult>(Name,
            Phase, HasOperation, IsCompleted, ReachedTerminal, CompletionIssuedDuringUnwind,
            ResultAvailable, result, failure, WasCancelled);
    }

    public readonly struct QaOperationUnwindResult<TResult>
    {
        public QaOperationUnwindResult(bool operationExisted, bool completionIssued,
            bool reachedTerminal, bool succeededToAwait, bool wasCancelled, bool resultAvailable,
            TResult result, Exception failure, QaOwnedAsyncOperationPhase finalPhase)
        {
            OperationExisted = operationExisted;
            CompletionIssued = completionIssued;
            ReachedTerminal = reachedTerminal;
            SucceededToAwait = succeededToAwait;
            WasCancelled = wasCancelled;
            ResultAvailable = resultAvailable;
            Result = result;
            Failure = failure;
            FinalPhase = finalPhase;
        }

        public bool OperationExisted { get; }
        public bool CompletionIssued { get; }
        public bool ReachedTerminal { get; }
        public bool SucceededToAwait { get; }
        public bool WasCancelled { get; }
        public bool ResultAvailable { get; }
        public TResult Result { get; }
        public Exception Failure { get; }
        public QaOwnedAsyncOperationPhase FinalPhase { get; }

        public static QaOperationUnwindResult<TResult> None(QaOwnedAsyncOperationPhase phase) =>
            new QaOperationUnwindResult<TResult>(false, false, false, false, false, false,
                default, null, phase);
    }

    public readonly struct QaOperationSnapshot<TResult>
    {
        public QaOperationSnapshot(string name, QaOwnedAsyncOperationPhase phase, bool hasOperation,
            bool isCompleted, bool reachedTerminal, bool completionIssuedDuringUnwind,
            bool resultAvailable, TResult result, Exception failure, bool wasCancelled)
        {
            Name = name;
            Phase = phase;
            HasOperation = hasOperation;
            IsCompleted = isCompleted;
            ReachedTerminal = reachedTerminal;
            CompletionIssuedDuringUnwind = completionIssuedDuringUnwind;
            ResultAvailable = resultAvailable;
            Result = result;
            Failure = failure;
            WasCancelled = wasCancelled;
        }

        public string Name { get; }
        public QaOwnedAsyncOperationPhase Phase { get; }
        public bool HasOperation { get; }
        public bool IsCompleted { get; }
        public bool ReachedTerminal { get; }
        public bool CompletionIssuedDuringUnwind { get; }
        public bool ResultAvailable { get; }
        public TResult Result { get; }
        public Exception Failure { get; }
        public bool WasCancelled { get; }
    }

    public readonly struct QaEvidenceCheckpoint
    {
        public QaEvidenceCheckpoint(string name, int sequence)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Checkpoint name is required.", nameof(name));
            if (sequence <= 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            Name = name.Trim();
            Sequence = sequence;
        }

        public string Name { get; }
        public int Sequence { get; }
        public void RequireBefore(QaEvidenceCheckpoint other)
        {
            if (Sequence >= other.Sequence) throw new InvalidOperationException(
                $"Checkpoint '{Name}' sequence='{Sequence}' must precede '{other.Name}' sequence='{other.Sequence}'.");
        }

        public void RequireAfter(QaEvidenceCheckpoint other) => other.RequireBefore(this);
        public string Describe() => $"name='{Name}' sequence='{Sequence}'";
    }

    public sealed class QaCausalSignalJoin<TFirst, TSecond>
    {
        private readonly object sync = new object();
        private readonly TaskCompletionSource<QaCausalSignalJoinSnapshot<TFirst, TSecond>> completion =
            new TaskCompletionSource<QaCausalSignalJoinSnapshot<TFirst, TSecond>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        private TFirst first;
        private TSecond second;
        private bool hasFirst;
        private bool hasSecond;

        public bool HasFirst { get { lock (sync) return hasFirst; } }
        public bool HasSecond { get { lock (sync) return hasSecond; } }
        public bool IsCompleted => completion.Task.IsCompleted;
        public TFirst First { get { lock (sync) { if (!hasFirst) throw new InvalidOperationException("First signal was not observed."); return first; } } }
        public TSecond Second { get { lock (sync) { if (!hasSecond) throw new InvalidOperationException("Second signal was not observed."); return second; } } }
        public Task<QaCausalSignalJoinSnapshot<TFirst, TSecond>> CompletionTask => completion.Task;

        public bool TrySetFirst(TFirst value)
        {
            lock (sync)
            {
                if (hasFirst) return false;
                first = value;
                hasFirst = true;
                TryComplete();
                return true;
            }
        }

        public bool TrySetSecond(TSecond value)
        {
            lock (sync)
            {
                if (hasSecond) return false;
                second = value;
                hasSecond = true;
                TryComplete();
                return true;
            }
        }

        public string Describe() => $"hasFirst='{HasFirst}' hasSecond='{HasSecond}' completed='{IsCompleted}'";

        private void TryComplete()
        {
            if (hasFirst && hasSecond)
            {
                completion.TrySetResult(new QaCausalSignalJoinSnapshot<TFirst, TSecond>(first, second));
            }
        }
    }

    public readonly struct QaCausalSignalJoinSnapshot<TFirst, TSecond>
    {
        public QaCausalSignalJoinSnapshot(TFirst first, TSecond second)
        {
            First = first;
            Second = second;
        }

        public TFirst First { get; }
        public TSecond Second { get; }
    }

    public readonly struct QaLoadingPresentationEvidenceSummary
    {
        public QaLoadingPresentationEvidenceSummary(int totalEvidenceCount, int updateRequestCount)
        {
            TotalEvidenceCount = totalEvidenceCount;
            LifecycleEvidenceCount = 6;
            UpdateRequestCount = updateRequestCount;
            UpdateEvidenceCount = 3 * updateRequestCount;
            ShowRequestCount = 1;
            HideRequestCount = 1;
            VisibleApplyCount = 1 + updateRequestCount;
            HiddenApplyCount = 1;
            ResultCount = 2 + updateRequestCount;
        }

        public int TotalEvidenceCount { get; }
        public int LifecycleEvidenceCount { get; }
        public int UpdateRequestCount { get; }
        public int UpdateEvidenceCount { get; }
        public int ShowRequestCount { get; }
        public int HideRequestCount { get; }
        public int VisibleApplyCount { get; }
        public int HiddenApplyCount { get; }
        public int ResultCount { get; }
    }

    public static class QaLoadingPresentationEvidenceGrammar
    {
        public static QaLoadingPresentationEvidenceSummary RequireValid(
            IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence, string source, string policyPrefix)
        {
            if (evidence == null) throw new ArgumentNullException(nameof(evidence));
            if (string.IsNullOrWhiteSpace(source)) throw new ArgumentException("Source is required.", nameof(source));
            if (string.IsNullOrWhiteSpace(policyPrefix)) throw new ArgumentException("Policy prefix is required.", nameof(policyPrefix));
            if (evidence.Count < 6 || (evidence.Count - 6) % 3 != 0) throw new InvalidOperationException(
                $"Loading evidence has invalid protocol length '{evidence.Count}'.");

            RequireTriplet(evidence, 0, QaLoadingPresentationEvidenceKind.VisibleApplied,
                LoadingSurfaceAction.Show, true, source, policyPrefix);
            int updateCount = (evidence.Count - 6) / 3;
            for (int update = 0; update < updateCount; update++)
            {
                RequireTriplet(evidence, 3 + (update * 3), QaLoadingPresentationEvidenceKind.VisibleApplied,
                    LoadingSurfaceAction.Update, true, source, policyPrefix);
            }

            RequireTriplet(evidence, 3 + (updateCount * 3), QaLoadingPresentationEvidenceKind.HiddenApplied,
                LoadingSurfaceAction.Hide, false, source, policyPrefix);
            return new QaLoadingPresentationEvidenceSummary(evidence.Count, updateCount);
        }


        public static IReadOnlyList<QaLoadingPresentationEvidenceEntry>
            RequireDeterminateUpdates(
                IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence,
                string source,
                string policyPrefix)
        {
            RequireValid(evidence, source, policyPrefix);
            var updates =
                new List<QaLoadingPresentationEvidenceEntry>();
            float previous = -1f;
            for (int index = 0; index < evidence.Count; index++)
            {
                QaLoadingPresentationEvidenceEntry entry = evidence[index];
                if (entry.Kind !=
                        QaLoadingPresentationEvidenceKind.RequestReceived ||
                    entry.Action != LoadingSurfaceAction.Update)
                {
                    continue;
                }

                if (!entry.ProgressSupported ||
                    float.IsNaN(entry.ProgressValue01) ||
                    float.IsInfinity(entry.ProgressValue01) ||
                    entry.ProgressValue01 < 0f ||
                    entry.ProgressValue01 > 1f ||
                    entry.ProgressValue01 < previous)
                {
                    throw new InvalidOperationException(
                        $"Determinate Loading progress diverged at evidence " +
                        $"index='{index}' value='{entry.ProgressValue01}'.");
                }

                previous = entry.ProgressValue01;
                updates.Add(entry);
            }

            if (updates.Count == 0)
            {
                throw new InvalidOperationException(
                    "Loading evidence contains no determinate Update request.");
            }

            return updates;
        }

        private static void RequireTriplet(IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence,
            int offset, QaLoadingPresentationEvidenceKind applyKind, LoadingSurfaceAction action,
            bool visible, string source, string policyPrefix)
        {
            bool requestActualVisible = action != LoadingSurfaceAction.Show;
            RequireEntry(evidence, offset, QaLoadingPresentationEvidenceKind.RequestReceived, action,
                visible, requestActualVisible, LoadingSurfaceResultStatus.Unknown, source, policyPrefix);
            RequireEntry(evidence, offset + 1, applyKind, action, visible, visible,
                LoadingSurfaceResultStatus.Unknown, source, policyPrefix);
            RequireEntry(evidence, offset + 2, QaLoadingPresentationEvidenceKind.ResultRecorded, action,
                visible, visible, LoadingSurfaceResultStatus.Succeeded, source, policyPrefix);
        }

        private static void RequireEntry(IReadOnlyList<QaLoadingPresentationEvidenceEntry> evidence,
            int index, QaLoadingPresentationEvidenceKind kind, LoadingSurfaceAction action,
            bool requestedVisible, bool actualVisible, LoadingSurfaceResultStatus status,
            string source, string policyPrefix)
        {
            QaLoadingPresentationEvidenceEntry entry = evidence[index];
            if (entry.Kind != kind || entry.Action != action || entry.RequestedVisible != requestedVisible ||
                entry.ActualVisible != actualVisible || entry.Status != status ||
                !string.Equals(entry.Source, source, StringComparison.Ordinal) ||
                entry.Detail.IndexOf(policyPrefix, StringComparison.Ordinal) < 0 ||
                (index > 0 && evidence[index - 1].Sequence >= entry.Sequence))
            {
                throw new InvalidOperationException($"Loading evidence entry '{index}' diverged from grammar.");
            }
        }
    }

    public sealed class QaFailureCollector
    {
        private readonly List<Entry> entries = new List<Entry>();
        public void Add(string name, Exception exception)
        {
            if (exception == null) return;
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Failure name is required.", nameof(name));
            for (int index = 0; index < entries.Count; index++) if (ReferenceEquals(entries[index].Exception, exception)) return;
            entries.Add(new Entry(name.Trim(), exception));
        }

        public Exception Primary => entries.Count == 0 ? null : entries[0].Exception;
        public bool HasFailures => entries.Count > 0;
        public string Describe(string name)
        {
            for (int index = 0; index < entries.Count; index++) if (string.Equals(entries[index].Name, name, StringComparison.Ordinal)) return Describe(entries[index].Exception);
            return "<none>";
        }

        public AggregateException ToAggregate(string message)
        {
            var failures = new List<Exception>();
            for (int index = 0; index < entries.Count; index++) failures.Add(entries[index].Exception);
            return new AggregateException(message, failures);
        }

        public static string Describe(Exception exception) => exception == null ? "<none>" : exception.GetType().Name + ": " + exception.Message;
        private readonly struct Entry { public Entry(string name, Exception exception) { Name = name; Exception = exception; } public string Name { get; } public Exception Exception { get; } }
    }

    public sealed class QaCaseRegistry
    {
        private readonly IReadOnlyList<string> expected;
        private readonly List<string> completed = new List<string>();
        public QaCaseRegistry(IReadOnlyList<string> expectedCases, int declaredCount)
        {
            expected = expectedCases ?? throw new ArgumentNullException(nameof(expectedCases));
            if (expected.Count != declaredCount) throw new InvalidOperationException($"Expected case count diverged. declared='{declaredCount}' actual='{expected.Count}'.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < expected.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(expected[index])) throw new InvalidOperationException($"Expected case '{index}' is blank.");
                if (!names.Add(expected[index])) throw new InvalidOperationException($"Expected case '{expected[index]}' is duplicated.");
            }
        }

        public int Count => completed.Count;
        public int ExpectedCount => expected.Count;
        public void Complete(string name)
        {
            string next = NextExpectedOrNone();
            if (!string.Equals(next, name, StringComparison.Ordinal)) throw new InvalidOperationException($"Case order diverged. expected='{next}' actual='{name}'.");
            completed.Add(name);
        }
        public bool TryCompleteIfNext(string name) { if (!string.Equals(NextExpectedOrNone(), name, StringComparison.Ordinal)) return false; completed.Add(name); return true; }
        public void RequireComplete() { if (Count != ExpectedCount) throw new InvalidOperationException($"Missing cases. expected='{ExpectedCount}' actual='{Count}' next='{NextExpectedOrNone()}'."); }
        public string NextExpectedOrNone() => Count < ExpectedCount ? expected[Count] : "<none>";
        public string DescribeCompleted() => string.Join(",", completed);
        public string DescribeMissing() { if (Count >= ExpectedCount) return "<none>"; var missing = new List<string>(); for (int i = Count; i < ExpectedCount; i++) missing.Add(expected[i]); return string.Join(",", missing); }
    }
}

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
        private Task<TResult> _operation;
        private Task<TResult> _terminalObservation;
        private Task<QaOperationUnwindResult<TResult>> _unwindObservation;
        private readonly object _sync = new object();
        private TResult _result;
        private Exception _failure;
        private bool _resultAvailable;
        private bool _completionIssuedDuringUnwind;
        private bool _unwindInvoked;

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
        public bool HasOperation => _operation != null;
        public bool IsCompleted => _operation != null && _operation.IsCompleted;
        public bool ReachedTerminal => Phase == QaOwnedAsyncOperationPhase.Terminal;
        public bool CompletionIssuedDuringUnwind => _completionIssuedDuringUnwind;
        public bool ResultAvailable => _resultAvailable;
        public TResult Result => _resultAvailable
            ? _result
            : throw new InvalidOperationException($"Operation '{Name}' has no result.");
        public Exception Failure => _failure;
        public bool WasCancelled => _operation != null && _operation.IsCanceled;
        public Task<TResult> Task => _operation ?? throw new InvalidOperationException(
            $"Operation '{Name}' has not been attached.");

        public void Attach(Task<TResult> task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            lock (_sync)
            {
                if (_operation != null)
                {
                    throw new InvalidOperationException($"Operation '{Name}' is already attached.");
                }

                _operation = task;
                SetPhase(QaOwnedAsyncOperationPhase.RequestStarted);
            }
        }

        public void SetPhase(QaOwnedAsyncOperationPhase phase)
        {
            lock (_sync)
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
            lock (_sync)
            {
                RequireAttachedOperation();
                if (_terminalObservation != null)
                {
                    return _terminalObservation;
                }

                if (ReachedTerminal)
                {
                    return ObserveExistingTerminalResult();
                }

                SetPhase(QaOwnedAsyncOperationPhase.AwaitingRequest);
                _terminalObservation = ObserveTerminalAsync();
                return _terminalObservation;
            }
        }

        private async Task<TResult> ObserveTerminalAsync()
        {
            try
            {
                _result = await _operation;
                _resultAvailable = true;
                SetPhase(QaOwnedAsyncOperationPhase.RequestCompleted);
                return _result;
            }
            catch (Exception exception)
            {
                _failure = exception;
                throw;
            }
            finally
            {
                lock (_sync)
                {
                    Phase = QaOwnedAsyncOperationPhase.Terminal;
                }
            }
        }

        public Task<QaOperationUnwindResult<TResult>> UnwindAsync(
            Func<Task> completionCallback)
        {
            lock (_sync)
            {
                if (_operation == null)
                {
                    return System.Threading.Tasks.Task.FromResult(
                        QaOperationUnwindResult<TResult>.None(Phase));
                }

                if (_unwindObservation != null)
                {
                    return _unwindObservation;
                }

                if (ReachedTerminal || _operation.IsCompleted)
                {
                    _unwindObservation = ObserveTerminalForUnwindAsync();
                    return _unwindObservation;
                }

                if (completionCallback == null)
                {
                    throw new ArgumentNullException(nameof(completionCallback));
                }

                if (_unwindInvoked)
                {
                    throw new InvalidOperationException($"Operation '{Name}' unwind was already invoked.");
                }

                _unwindInvoked = true;
                SetPhase(QaOwnedAsyncOperationPhase.Unwinding);
                _unwindObservation = ExecuteUnwindAsync(completionCallback);
                return _unwindObservation;
            }
        }

        private async Task<QaOperationUnwindResult<TResult>> ExecuteUnwindAsync(
            Func<Task> completionCallback)
        {
            await completionCallback();
            _completionIssuedDuringUnwind = true;
            return await ObserveTerminalForUnwindAsync();
        }

        private async Task<QaOperationUnwindResult<TResult>> ObserveTerminalForUnwindAsync()
        {
            try
            {
                await AwaitTerminalAsync();
                return new QaOperationUnwindResult<TResult>(true, _completionIssuedDuringUnwind,
                    ReachedTerminal, true, false, _resultAvailable, _result, null, Phase);
            }
            catch (OperationCanceledException exception)
            {
                return new QaOperationUnwindResult<TResult>(true, _completionIssuedDuringUnwind,
                    ReachedTerminal, false, true, false, default, exception, Phase);
            }
            catch (Exception exception)
            {
                return new QaOperationUnwindResult<TResult>(true, _completionIssuedDuringUnwind,
                    ReachedTerminal, false, false, false, default, exception, Phase);
            }
        }

        private Task<TResult> ObserveExistingTerminalResult()
        {
            if (_resultAvailable)
            {
                _terminalObservation = System.Threading.Tasks.Task.FromResult(_result);
                return _terminalObservation;
            }

            if (_failure != null)
            {
                _terminalObservation = System.Threading.Tasks.Task.FromException<TResult>(_failure);
                return _terminalObservation;
            }

            if (WasCancelled)
            {
                _terminalObservation = _operation;
                return _terminalObservation;
            }

            throw new InvalidOperationException(
                $"Operation '{Name}' reached terminal without an observable result.");
        }

        private void RequireAttachedOperation()
        {
            if (_operation == null)
            {
                throw new InvalidOperationException($"Operation '{Name}' has not been attached.");
            }
        }

        public QaOperationSnapshot<TResult> Snapshot() => new QaOperationSnapshot<TResult>(Name,
            Phase, HasOperation, IsCompleted, ReachedTerminal, CompletionIssuedDuringUnwind,
            ResultAvailable, _result, _failure, WasCancelled);
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
        private readonly object _sync = new object();
        private readonly TaskCompletionSource<QaCausalSignalJoinSnapshot<TFirst, TSecond>> _completion =
            new TaskCompletionSource<QaCausalSignalJoinSnapshot<TFirst, TSecond>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        private TFirst _first;
        private TSecond _second;
        private bool _hasFirst;
        private bool _hasSecond;

        public bool HasFirst { get { lock (_sync) return _hasFirst; } }
        public bool HasSecond { get { lock (_sync) return _hasSecond; } }
        public bool IsCompleted => _completion.Task.IsCompleted;
        public TFirst First { get { lock (_sync) { if (!_hasFirst) throw new InvalidOperationException("First signal was not observed."); return _first; } } }
        public TSecond Second { get { lock (_sync) { if (!_hasSecond) throw new InvalidOperationException("Second signal was not observed."); return _second; } } }
        public Task<QaCausalSignalJoinSnapshot<TFirst, TSecond>> CompletionTask => _completion.Task;

        public bool TrySetFirst(TFirst value)
        {
            lock (_sync)
            {
                if (_hasFirst) return false;
                _first = value;
                _hasFirst = true;
                TryComplete();
                return true;
            }
        }

        public bool TrySetSecond(TSecond value)
        {
            lock (_sync)
            {
                if (_hasSecond) return false;
                _second = value;
                _hasSecond = true;
                TryComplete();
                return true;
            }
        }

        public string Describe() => $"hasFirst='{HasFirst}' hasSecond='{HasSecond}' completed='{IsCompleted}'";

        private void TryComplete()
        {
            if (_hasFirst && _hasSecond)
            {
                _completion.TrySetResult(new QaCausalSignalJoinSnapshot<TFirst, TSecond>(_first, _second));
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
        private readonly List<Entry> _entries = new List<Entry>();
        public void Add(string name, Exception exception)
        {
            if (exception == null) return;
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Failure name is required.", nameof(name));
            for (int index = 0; index < _entries.Count; index++) if (ReferenceEquals(_entries[index].Exception, exception)) return;
            _entries.Add(new Entry(name.Trim(), exception));
        }

        public Exception Primary => _entries.Count == 0 ? null : _entries[0].Exception;
        public bool HasFailures => _entries.Count > 0;
        public string Describe(string name)
        {
            for (int index = 0; index < _entries.Count; index++) if (string.Equals(_entries[index].Name, name, StringComparison.Ordinal)) return Describe(_entries[index].Exception);
            return "<none>";
        }

        public AggregateException ToAggregate(string message)
        {
            var failures = new List<Exception>();
            for (int index = 0; index < _entries.Count; index++) failures.Add(_entries[index].Exception);
            return new AggregateException(message, failures);
        }

        public static string Describe(Exception exception) => exception == null ? "<none>" : exception.GetType().Name + ": " + exception.Message;
        private readonly struct Entry { public Entry(string name, Exception exception) { Name = name; Exception = exception; } public string Name { get; } public Exception Exception { get; } }
    }

    public sealed class QaCaseRegistry
    {
        private readonly IReadOnlyList<string> _expected;
        private readonly List<string> _completed = new List<string>();
        public QaCaseRegistry(IReadOnlyList<string> expectedCases, int declaredCount)
        {
            _expected = expectedCases ?? throw new ArgumentNullException(nameof(expectedCases));
            if (_expected.Count != declaredCount) throw new InvalidOperationException($"Expected case count diverged. declared='{declaredCount}' actual='{_expected.Count}'.");
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < _expected.Count; index++)
            {
                if (string.IsNullOrWhiteSpace(_expected[index])) throw new InvalidOperationException($"Expected case '{index}' is blank.");
                if (!names.Add(_expected[index])) throw new InvalidOperationException($"Expected case '{_expected[index]}' is duplicated.");
            }
        }

        public int Count => _completed.Count;
        public int ExpectedCount => _expected.Count;
        public void Complete(string name)
        {
            string next = NextExpectedOrNone();
            if (!string.Equals(next, name, StringComparison.Ordinal)) throw new InvalidOperationException($"Case order diverged. expected='{next}' actual='{name}'.");
            _completed.Add(name);
        }
        public bool TryCompleteIfNext(string name) { if (!string.Equals(NextExpectedOrNone(), name, StringComparison.Ordinal)) return false; _completed.Add(name); return true; }
        public void RequireComplete() { if (Count != ExpectedCount) throw new InvalidOperationException($"Missing cases. expected='{ExpectedCount}' actual='{Count}' next='{NextExpectedOrNone()}'."); }
        public string NextExpectedOrNone() => Count < ExpectedCount ? _expected[Count] : "<none>";
        public string DescribeCompleted() => string.Join(",", _completed);
        public string DescribeMissing() { if (Count >= ExpectedCount) return "<none>"; var missing = new List<string>(); for (int i = Count; i < ExpectedCount; i++) missing.Add(_expected[i]); return string.Join(",", missing); }
    }
}

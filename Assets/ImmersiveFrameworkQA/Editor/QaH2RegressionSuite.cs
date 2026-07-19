using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImmersiveFrameworkQA.GameFlow.Internal.Editor;
using ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor;
using ImmersiveFrameworkQA.InputMode.Editor;
using ImmersiveFrameworkQA.Player.Editor;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Editor
{
    [InitializeOnLoad]
    public static class QaH2RegressionSuite
    {
        private const string MenuPath = "Immersive Framework/QA/H2 Run Full Regression Suite";
        private const string H226MenuPath = "Immersive Framework/QA/H2 Run H2.2.6 Suite";
        private const string Prefix = "ImmersiveFrameworkQA.H2Regression.";
        private const string PhaseKey = Prefix + "Phase";
        private const string CompletedKey = Prefix + "Completed";
        private const string FailedKey = Prefix + "Failed";
        private const string ResultCountKey = Prefix + "ResultCount";
        private const string ResultKeyPrefix = Prefix + "Result.";
        private const string H2SnapshotCountKey = Prefix + "H2SnapshotCount";
        private const string H2SnapshotKeyPrefix = Prefix + "H2Snapshot.";
        private const string CurrentCutKey = Prefix + "CurrentCut";
        private const string CurrentSmokeKey = Prefix + "CurrentSmoke";
        private const string P3StartedTicksKey = Prefix + "P3StartedTicks";
        private const string P3RegressionStatusKey = "ImmersiveFrameworkQA.P3Canonical.RegressionStatus";
        private const string P3PhaseKey = "ImmersiveFrameworkQA.P3Canonical.Phase";
        private const string P3CaseKey = "ImmersiveFrameworkQA.P3Canonical.CurrentCase";
        private const string P3ExceptionKey = "ImmersiveFrameworkQA.P3Canonical.Exception";
        private const string P3MessageKey = "ImmersiveFrameworkQA.P3Canonical.Message";
        private const string P3LastPhaseKey = Prefix + "P3LastPhase";
        private const string P3LastCaseKey = Prefix + "P3LastCase";
        private const string P3LastExceptionKey = Prefix + "P3LastException";
        private const string P3LastMessageKey = Prefix + "P3LastMessage";
        private const string P3PendingPhase = "P3Pending";
        private const string P3ScheduledPhase = "P3Scheduled";
        private const string P3RunningPhase = "P3Running";
        private const double FrameworkReadinessTimeoutSeconds = 30d;
        private const double P3TimeoutSeconds = 120d;
        private static bool _playRunnerStarted;
        private static bool _p3ContinuationInProgress;
        private static bool _p3ContinuationRegistered;
        private static bool _p3StartScheduled;

        private sealed class SmokeResult
        {
            public SmokeResult(string status, string cut, string smoke, string phase, string message)
            {
                Status = status;
                Cut = cut;
                Smoke = smoke;
                Phase = phase;
                Message = message;
            }

            public string Status { get; }
            public string Cut { get; }
            public string Smoke { get; }
            public string Phase { get; }
            public string Message { get; }
        }

        static QaH2RegressionSuite()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            string phase = SessionState.GetString(PhaseKey, string.Empty);
            if (phase == P3ScheduledPhase)
            {
                ScheduleP3Start();
            }
            else if (phase == P3RunningPhase)
            {
                RegisterP3Continuation();
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !IsRunning && !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        public static void RunFull()
        {
            Start(includeP3: true, includeAllH2: true);
        }

        [MenuItem(H226MenuPath, true)]
        private static bool ValidateH226Run() => !IsRunning && !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(H226MenuPath)]
        public static void RunH226()
        {
            Start(includeP3: false, includeAllH2: false);
        }

        private static bool IsRunning => !string.IsNullOrEmpty(SessionState.GetString(PhaseKey, string.Empty));

        private static void Start(bool includeP3, bool includeAllH2)
        {
            ClearState();
            SessionState.SetBool(Prefix + "IncludeP3", includeP3);
            SessionState.SetBool(Prefix + "IncludeAllH2", includeAllH2);
            try
            {
                RunEditMode(includeAllH2);
                SessionState.SetString(PhaseKey, "PlayMode");
                EditorApplication.EnterPlaymode();
            }
            catch (Exception exception)
            {
                Fail("EditMode", "dispatch", exception);
            }
        }

        private static void RunEditMode(bool includeAllH2)
        {
            if (includeAllH2)
            {
                Execute("H2.2.1", "Composition", QaH221PauseRequestTriggerCompositionSmoke.Run);
                Execute("H2.2.2", "Composition", QaH222RouteRequestTriggerCompositionSmoke.Run);
                Execute("H2.2.3", "Composition", QaH223ActivityRequestTriggerCompositionSmoke.Run);
                Execute("H2.2.4", "Composition", QaH224RouteCycleResetTriggerCompositionSmoke.Run);
                Execute("H2.2.5", "Composition", QaH225ActivityCycleResetTriggerCompositionSmoke.Run);
            }
            Execute("H2.2.6", "Composition", QaH226ActivityRestartTriggerCompositionSmoke.Run);
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsRunning)
            {
                return;
            }
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetString(PhaseKey, string.Empty) == "PlayMode")
            {
                RunPlayModeAsync();
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetString(PhaseKey, string.Empty) == P3PendingPhase)
            {
                SessionState.SetString(PhaseKey, P3ScheduledPhase);
                ScheduleP3Start();
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetString(PhaseKey, string.Empty) == "Complete")
            {
                CompleteSuite();
            }
        }

        private static async void RunPlayModeAsync()
        {
            if (_playRunnerStarted) return;
            _playRunnerStarted = true;
            try
            {
                SetCurrent("Framework", "Readiness");
                await WaitForFrameworkReadinessAsync();
                bool includeAllH2 = SessionState.GetBool(Prefix + "IncludeAllH2", true);
                if (includeAllH2)
                {
                    Execute("H2.2.1", "Binding", QaH221PauseRequestTriggerBindingSmoke.Run);
                    Execute("H2.2.2", "Binding", QaH222RouteRequestTriggerBindingSmoke.Run);
                    Execute("H2.2.3", "Binding", QaH223ActivityRequestTriggerBindingSmoke.Run);
                    Execute("H2.2.4", "Binding", QaH224RouteCycleResetTriggerBindingSmoke.Run);
                    await ExecuteAsync("H2.2.4", "Vertical", QaH224RouteCycleResetVerticalSmoke.RunInternalAsync);
                    Execute("H2.2.5", "Binding", QaH225ActivityCycleResetTriggerBindingSmoke.Run);
                    await ExecuteAsync("H2.2.5", "Vertical", QaH225ActivityCycleResetVerticalSmoke.RunInternalAsync);
                }
                await ExecuteAsync("H2.2.6", "Binding", QaH226ActivityRestartTriggerBindingSmoke.RunInternalAsync);
                await ExecuteAsync("H2.2.6", "Vertical", QaH226ActivityRestartVerticalSmoke.RunInternalAsync);
                if (SessionState.GetBool(Prefix + "IncludeP3", false))
                {
                    SaveH2Snapshot();
                    SessionState.SetString(PhaseKey, P3PendingPhase);
                }
                else
                {
                    SessionState.SetString(PhaseKey, "Complete");
                }
            }
            catch (Exception exception)
            {
                Fail("PlayMode", CurrentSmokeLabel(), exception);
            }
            finally
            {
                _playRunnerStarted = false;
                EditorApplication.ExitPlaymode();
            }
        }

        private static void ContinueAfterP3()
        {
            if (!IsRunning || SessionState.GetString(PhaseKey, string.Empty) != P3RunningPhase || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
            if (_p3ContinuationInProgress)
            {
                return;
            }

            _p3ContinuationInProgress = true;
            try
            {
                CaptureP3Diagnostics();
                if (QaP3CanonicalPreFirstGameSmoke.TryGetRegressionSuiteStatus(out string status))
                {
                    if (status == "Running")
                    {
                        return;
                    }

                    if (status == "Passed")
                    {
                        RestoreH2Snapshot();
                        AddCompleted("P3", "Canonical Pre-FIRSTGAME", "EditMode", "P3 reported Passed.");
                        CompleteSuite();
                        return;
                    }

                    if (status == "Failed")
                    {
                        RestoreH2Snapshot();
                        string phase = ReadLastP3Diagnostic(P3LastPhaseKey, "unknown");
                        string currentCase = ReadLastP3Diagnostic(P3LastCaseKey, "unknown");
                        string exception = ReadLastP3Diagnostic(P3LastExceptionKey, "unknown");
                        string message = ReadLastP3Diagnostic(P3LastMessageKey, "No diagnostic remained available.");
                        Fail(
                            "P3",
                            "P3",
                            "Canonical Pre-FIRSTGAME/" + currentCase,
                            new InvalidOperationException(
                                $"P3 reported Failed. phase='{phase}' case='{currentCase}' exception='{exception}' message='{message}'."));
                        return;
                    }
                }

                if (TryGetP3ElapsedSeconds(out double elapsed) && elapsed >= P3TimeoutSeconds)
                {
                    RestoreH2Snapshot();
                    Fail(
                        "P3",
                        "P3",
                        "Canonical Pre-FIRSTGAME",
                        new TimeoutException($"P3 timed out after '{P3TimeoutSeconds:0}' seconds while awaiting a terminal status."));
                }
            }
            finally
            {
                _p3ContinuationInProgress = false;
            }
        }

        private static void ScheduleP3Start()
        {
            if (_p3StartScheduled)
            {
                return;
            }

            EditorApplication.delayCall -= StartScheduledP3;
            EditorApplication.delayCall += StartScheduledP3;
            _p3StartScheduled = true;
        }

        private static void StartScheduledP3()
        {
            EditorApplication.delayCall -= StartScheduledP3;
            _p3StartScheduled = false;

            if (!IsRunning || SessionState.GetString(PhaseKey, string.Empty) != P3ScheduledPhase)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RestoreH2Snapshot();
                Fail(
                    "P3",
                    "P3",
                    "Canonical Pre-FIRSTGAME",
                    new InvalidOperationException("P3 scheduling was cancelled because Unity is entering or already in Play Mode."));
                return;
            }

            try
            {
                SessionState.SetString(P3StartedTicksKey, DateTime.UtcNow.Ticks.ToString());
                SessionState.SetString(PhaseKey, P3RunningPhase);
                RegisterP3Continuation();
                QaP3CanonicalPreFirstGameSmoke.RunForRegressionSuite();
            }
            catch (Exception exception)
            {
                RestoreH2Snapshot();
                Fail("P3", "P3", "Canonical Pre-FIRSTGAME", exception);
            }
        }

        private static void RegisterP3Continuation()
        {
            if (_p3ContinuationRegistered)
            {
                return;
            }

            EditorApplication.update -= ContinueAfterP3;
            EditorApplication.update += ContinueAfterP3;
            _p3ContinuationRegistered = true;
        }

        private static void UnregisterP3Continuation()
        {
            if (!_p3ContinuationRegistered)
            {
                return;
            }

            EditorApplication.update -= ContinueAfterP3;
            _p3ContinuationRegistered = false;
        }

        private static void CaptureP3Diagnostics()
        {
            CaptureP3Diagnostic(P3PhaseKey, P3LastPhaseKey);
            CaptureP3Diagnostic(P3CaseKey, P3LastCaseKey);
            CaptureP3Diagnostic(P3ExceptionKey, P3LastExceptionKey);
            CaptureP3Diagnostic(P3MessageKey, P3LastMessageKey);
        }

        private static void CaptureP3Diagnostic(string sourceKey, string destinationKey)
        {
            string value = SessionState.GetString(sourceKey, string.Empty);
            if (!string.IsNullOrEmpty(value))
            {
                SessionState.SetString(destinationKey, value);
            }
        }

        private static string ReadLastP3Diagnostic(string key, string fallback)
        {
            return SessionState.GetString(key, fallback);
        }

        private static bool TryGetP3ElapsedSeconds(out double elapsed)
        {
            elapsed = 0d;
            string value = SessionState.GetString(P3StartedTicksKey, string.Empty);
            if (!long.TryParse(value, out long startedTicks))
            {
                return false;
            }

            elapsed = (DateTime.UtcNow - new DateTime(startedTicks, DateTimeKind.Utc)).TotalSeconds;
            return true;
        }

        private static void Execute(string cut, string smoke, Action action)
        {
            SetCurrent(cut, smoke);
            action();
            AddCompleted(cut, smoke);
            ClearCurrent();
        }

        private static async Task ExecuteAsync(string cut, string smoke, Func<Task> action)
        {
            SetCurrent(cut, smoke);
            await action();
            AddCompleted(cut, smoke);
            ClearCurrent();
        }

        private static async Task WaitForFrameworkReadinessAsync()
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(FrameworkReadinessTimeoutSeconds);
            string lastState = "FrameworkRuntimeHost unavailable.";
            while (DateTime.UtcNow < deadline)
            {
                if (QaH2FrameworkReadiness.TryGetReady(out string diagnostic))
                {
                    return;
                }
                else lastState = diagnostic;

                await Task.Yield();
            }

            throw new TimeoutException(
                $"Framework readiness timed out after '{FrameworkReadinessTimeoutSeconds:0}' seconds. lastState={lastState}");
        }

        private static void SetCurrent(string cut, string smoke)
        {
            SessionState.SetString(CurrentCutKey, cut ?? string.Empty);
            SessionState.SetString(CurrentSmokeKey, smoke ?? string.Empty);
        }

        private static void ClearCurrent()
        {
            SessionState.EraseString(CurrentCutKey);
            SessionState.EraseString(CurrentSmokeKey);
        }

        private static string CurrentSmokeLabel()
        {
            string cut = SessionState.GetString(CurrentCutKey, "Unknown");
            string smoke = SessionState.GetString(CurrentSmokeKey, "Unknown");
            return cut + "/" + smoke;
        }

        private static void AddCompleted(string cut, string smoke)
        {
            AddCompleted(cut, smoke, GetExecutionPhase(), "Completed.");
        }

        private static void AddCompleted(string cut, string smoke, string phase, string message)
        {
            RecordResult(new SmokeResult("Passed", cut, smoke, phase, message));
            RebuildCompletedSummary();
        }

        private static void CompleteSuite()
        {
            EmitFinalReport("Passed");
            ClearState();
        }

        private static void Fail(string phase, string smoke, Exception exception)
        {
            string cut = SessionState.GetString(CurrentCutKey, phase);
            string currentSmoke = SessionState.GetString(CurrentSmokeKey, string.Empty);
            Fail(phase, cut, string.IsNullOrEmpty(currentSmoke) ? smoke : currentSmoke, exception);
        }

        private static void Fail(string phase, string cut, string smoke, Exception exception)
        {
            SessionState.SetString(FailedKey, smoke);
            RecordResult(new SmokeResult("Failed", cut, smoke, phase, exception.Message));
            RebuildCompletedSummary();
            EmitFinalReport("Failed");
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            ClearState();
        }

        private static string GetExecutionPhase()
        {
            return EditorApplication.isPlaying ? "PlayMode" : "EditMode";
        }

        private static void RecordResult(SmokeResult result)
        {
            int index = SessionState.GetInt(ResultCountKey, 0);
            WriteResult(ResultKeyPrefix, index, result);
            SessionState.SetInt(ResultCountKey, index + 1);
        }

        private static List<SmokeResult> ReadResults()
        {
            return ReadResults(ResultKeyPrefix, SessionState.GetInt(ResultCountKey, 0));
        }

        private static List<SmokeResult> ReadResults(string keyPrefix, int count)
        {
            var results = new List<SmokeResult>(count);
            for (int index = 0; index < count; index++)
            {
                results.Add(new SmokeResult(
                    SessionState.GetString(ResultKey(keyPrefix, index, "Status"), "Unknown"),
                    SessionState.GetString(ResultKey(keyPrefix, index, "Cut"), "Unknown"),
                    SessionState.GetString(ResultKey(keyPrefix, index, "Smoke"), "Unknown"),
                    SessionState.GetString(ResultKey(keyPrefix, index, "Phase"), "Unknown"),
                    SessionState.GetString(ResultKey(keyPrefix, index, "Message"), string.Empty)));
            }

            return results;
        }

        private static void WriteResult(string keyPrefix, int index, SmokeResult result)
        {
            SessionState.SetString(ResultKey(keyPrefix, index, "Status"), result.Status);
            SessionState.SetString(ResultKey(keyPrefix, index, "Cut"), result.Cut);
            SessionState.SetString(ResultKey(keyPrefix, index, "Smoke"), result.Smoke);
            SessionState.SetString(ResultKey(keyPrefix, index, "Phase"), result.Phase);
            SessionState.SetString(ResultKey(keyPrefix, index, "Message"), result.Message);
        }

        private static string ResultKey(string keyPrefix, int index, string field)
        {
            return keyPrefix + index + "." + field;
        }

        private static void SaveH2Snapshot()
        {
            ClearResultStore(H2SnapshotKeyPrefix, H2SnapshotCountKey);
            List<SmokeResult> results = ReadResults();
            for (int index = 0; index < results.Count; index++)
            {
                WriteResult(H2SnapshotKeyPrefix, index, results[index]);
            }

            SessionState.SetInt(H2SnapshotCountKey, results.Count);
        }

        private static void RestoreH2Snapshot()
        {
            int count = SessionState.GetInt(H2SnapshotCountKey, 0);
            List<SmokeResult> snapshot = ReadResults(H2SnapshotKeyPrefix, count);
            ClearResultStore(ResultKeyPrefix, ResultCountKey);
            for (int index = 0; index < snapshot.Count; index++)
            {
                WriteResult(ResultKeyPrefix, index, snapshot[index]);
            }

            SessionState.SetInt(ResultCountKey, snapshot.Count);
            RebuildCompletedSummary();
        }

        private static void RebuildCompletedSummary()
        {
            List<SmokeResult> results = ReadResults();
            var completed = new List<string>();
            for (int index = 0; index < results.Count; index++)
            {
                SmokeResult result = results[index];
                if (result.Status == "Passed")
                {
                    completed.Add(result.Cut + "/" + result.Smoke);
                }
            }

            SessionState.SetString(CompletedKey, string.Join("|", completed));
        }

        private static void EmitFinalReport(string status)
        {
            List<SmokeResult> results = ReadResults();
            var entries = new List<string>(results.Count);
            int failed = 0;
            for (int index = 0; index < results.Count; index++)
            {
                SmokeResult result = results[index];
                if (result.Status == "Failed")
                {
                    failed++;
                }

                entries.Add(
                    $"status='{EscapeForReport(result.Status)}' cut='{EscapeForReport(result.Cut)}' smoke='{EscapeForReport(result.Smoke)}' phase='{EscapeForReport(result.Phase)}' message='{EscapeForReport(result.Message)}'");
            }

            string report =
                $"[H2_REGRESSION_SUITE] status='{status}' smokes='{results.Count}' cases='{CountCases(results)}' failed='{failed}' results='{string.Join(" | ", entries)}'.";
            if (status == "Passed")
            {
                Debug.Log(report);
            }
            else
            {
                Debug.LogError(report);
            }
        }

        private static string EscapeForReport(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }

        private static void ClearResultStore(string keyPrefix, string countKey)
        {
            int count = SessionState.GetInt(countKey, 0);
            for (int index = 0; index < count; index++)
            {
                SessionState.EraseString(ResultKey(keyPrefix, index, "Status"));
                SessionState.EraseString(ResultKey(keyPrefix, index, "Cut"));
                SessionState.EraseString(ResultKey(keyPrefix, index, "Smoke"));
                SessionState.EraseString(ResultKey(keyPrefix, index, "Phase"));
                SessionState.EraseString(ResultKey(keyPrefix, index, "Message"));
            }

            SessionState.EraseInt(countKey);
        }

        private static void ClearState()
        {
            SessionState.EraseString(PhaseKey);
            SessionState.EraseString(CompletedKey);
            SessionState.EraseString(FailedKey);
            ClearCurrent();
            SessionState.EraseBool(Prefix + "IncludeP3");
            SessionState.EraseBool(Prefix + "IncludeAllH2");
            SessionState.EraseString(P3StartedTicksKey);
            SessionState.EraseString(P3RegressionStatusKey);
            SessionState.EraseString(P3LastPhaseKey);
            SessionState.EraseString(P3LastCaseKey);
            SessionState.EraseString(P3LastExceptionKey);
            SessionState.EraseString(P3LastMessageKey);
            ClearResultStore(ResultKeyPrefix, ResultCountKey);
            ClearResultStore(H2SnapshotKeyPrefix, H2SnapshotCountKey);
            UnregisterP3Continuation();
            _p3ContinuationInProgress = false;
        }

        private static int CountCases(IReadOnlyList<SmokeResult> results)
        {
            int total = 0;
            for (int index = 0; index < results.Count; index++)
            {
                total += (results[index].Cut + "/" + results[index].Smoke) switch
                {
                    "H2.2.1/Composition" => 5, "H2.2.1/Binding" => 8,
                    "H2.2.2/Composition" => 5, "H2.2.2/Binding" => 8,
                    "H2.2.3/Composition" => 5, "H2.2.3/Binding" => 9,
                    "H2.2.4/Composition" => 5, "H2.2.4/Binding" => 10, "H2.2.4/Vertical" => 6,
                    "H2.2.5/Composition" => 5, "H2.2.5/Binding" => 9, "H2.2.5/Vertical" => 8,
                    "H2.2.6/Composition" => 5, "H2.2.6/Binding" => 10, "H2.2.6/Vertical" => 8,
                    _ => 0
                };
            }
            return total;
        }
    }
}

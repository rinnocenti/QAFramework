using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// IF-M07 Close Gate.
    ///
    /// Runs the six canonical M07 Play Mode regressions in isolated, fresh
    /// Play Mode sessions. The gate persists only orchestration state through
    /// Unity SessionState; every product/runtime assertion remains owned by
    /// the original regression that introduced it.
    /// </summary>
    [InitializeOnLoad]
    public static class QaM07CloseGateRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/M07 Run Close Gate";
        private const string AbortMenuPath =
            "Immersive Framework/QA/Regressions/Player/M07 Abort Close Gate";
        private const string Prefix = "[QA_IF_M07_CLOSE_GATE]";
        private const string SetupMenuPath =
            "Immersive Framework/QA/Setup/Player/M07 Prepare Internal Reconcile Regression";
        private const string SetupMethodName = "Prepare";

        private const string StatePrefix =
            "ImmersiveFrameworkQA.M07CloseGate.";
        private const string RunningKey = StatePrefix + "Running";
        private const string StepIndexKey = StatePrefix + "StepIndex";
        private const string PassedCasesKey = StatePrefix + "PassedCases";
        private const string CompletedStepsKey =
            StatePrefix + "CompletedSteps";
        private const string FailureKey = StatePrefix + "Failure";
        private const string ExecutingKey = StatePrefix + "Executing";
        private const string TransitionScheduledKey =
            StatePrefix + "TransitionScheduled";
        private const int FrameworkReadyFrameBudget = 900;

        private static readonly BindingFlags StaticAny =
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private static readonly GateStep[] Steps =
        {
            new GateStep(
                "M07-10",
                "ImmersiveFrameworkQA.GameFlow.Internal.Editor.QaM07InternalReconcileRegression",
                "Immersive Framework/QA/Regressions/Player/M07 Run Internal Reconcile Authority Regression",
                "[QA_M07_INTERNAL]",
                54,
                "InternalReconcile"),
            new GateStep(
                "IF-M07-12B-5",
                "ImmersiveFrameworkQA.GameFlow.Internal.Editor.QaM07ActivitySessionLifecycleProjectionRegression",
                "Immersive Framework/QA/Regressions/Player/M07 Run Activity Session Lifecycle Projection Regression",
                "[QA_IF_M07_12B_5_ACTIVITY_SESSION_PROJECTION]",
                30,
                "ActivitySessionProjection"),
            new GateStep(
                "IF-M07-12B-6",
                "ImmersiveFrameworkQA.GameFlow.Internal.Editor.QaM07PlayerRequirementPolicyMatrixRegression",
                "Immersive Framework/QA/Regressions/Player/M07 Run Player Requirement Policy Matrix Regression",
                "[QA_IF_M07_12B_6_PLAYER_REQUIREMENT_POLICY_MATRIX]",
                38,
                "RequirementPolicyMatrix"),
            new GateStep(
                "IF-M07-12B-7",
                "ImmersiveFrameworkQA.GameFlow.Internal.Editor.QaM07PlayerZeroParticipantPolicyMatrixRegression",
                "Immersive Framework/QA/Regressions/Player/M07 Run Player Zero-Participant Policy Matrix Regression",
                "[QA_IF_M07_12B_7_PLAYER_ZERO_PARTICIPANT_POLICY_MATRIX]",
                36,
                "ZeroParticipantPolicyMatrix"),
            new GateStep(
                "IF-M07-12B-8",
                "ImmersiveFrameworkQA.GameFlow.Internal.Editor.QaM07ActiveProjectionFreezeRegression",
                "Immersive Framework/QA/Regressions/Player/M07 Run Active Projection Freeze Regression",
                "[QA_IF_M07_12B_8_ACTIVE_PROJECTION_FREEZE]",
                30,
                "ActiveProjectionFreeze"),
            new GateStep(
                "IF-M07-12B-9",
                "ImmersiveFrameworkQA.GameFlow.Internal.Editor.QaM07IncludedExcludedFailureReleaseScopeRegression",
                "Immersive Framework/QA/Regressions/Player/M07 Run Included Excluded Failure and Release Scope Regression",
                "[QA_IF_M07_12B_9_INCLUDED_EXCLUDED_FAILURE_RELEASE_SCOPE]",
                42,
                "IncludedExcludedFailureReleaseScope")
        };

        private static readonly int ExpectedTotalCases =
            CalculateExpectedTotalCases();

        static QaM07CloseGateRegression()
        {
            // A pending delayCall is not preserved by an Editor domain reload.
            // Clear only the transient scheduling latch so ResumeIfRequired can
            // rebuild the transition from the persisted gate state.
            if (IsRunning)
            {
                SessionState.SetBool(
                    TransitionScheduledKey,
                    false);
            }

            EditorApplication.playModeStateChanged -=
                OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                OnPlayModeStateChanged;
            EditorApplication.delayCall += ResumeIfRequired;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode &&
                !IsRunning;
        }

        [MenuItem(MenuPath)]
        private static void Run()
        {
            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,
                    "IF-M07 Close Gate must start from Edit Mode.");
                Require(!IsRunning,
                    "IF-M07 Close Gate is already running.");

                ValidateGateDefinitions();
                ResetState();
                SessionState.SetBool(RunningKey, true);

                Debug.Log(
                    $"{Prefix} status='Started' " +
                    $"steps='{Steps.Length}' " +
                    $"cases='{ExpectedTotalCases}' " +
                    "isolation='FreshPlayModePerRegression' " +
                    $"plan='{DescribePlan()}'.");

                ScheduleNextPlayMode();
            }
            catch (Exception exception)
            {
                ResetState();
                Debug.LogError(
                    $"{Prefix} status='FailedPreflight' " +
                    $"execution='{Escape(exception)}'.");
                throw;
            }
        }

        [MenuItem(AbortMenuPath, true)]
        private static bool ValidateAbort()
        {
            return IsRunning;
        }

        [MenuItem(AbortMenuPath)]
        private static void Abort()
        {
            if (!IsRunning)
            {
                return;
            }

            SessionState.SetString(
                FailureKey,
                "Close Gate aborted explicitly by the user.");

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            CompleteFailed();
        }

        private static bool IsRunning =>
            SessionState.GetBool(RunningKey, false);

        private static void OnPlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (!IsRunning)
            {
                return;
            }

            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    SessionState.SetBool(
                        TransitionScheduledKey,
                        false);
                    StartCurrentStepIfRequired();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    SessionState.SetBool(ExecutingKey, false);
                    ResumeFromEditMode();
                    break;
            }
        }

        private static void ResumeIfRequired()
        {
            if (!IsRunning)
            {
                return;
            }

            if (EditorApplication.isPlaying)
            {
                StartCurrentStepIfRequired();
                return;
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ResumeFromEditMode();
            }
        }

        private static void ResumeFromEditMode()
        {
            if (!IsRunning)
            {
                return;
            }

            string failure =
                SessionState.GetString(FailureKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(failure))
            {
                CompleteFailed();
                return;
            }

            int index = SessionState.GetInt(StepIndexKey, 0);
            if (index >= Steps.Length)
            {
                CompletePassed();
                return;
            }

            ScheduleNextPlayMode();
        }

        private static void ScheduleNextPlayMode()
        {
            if (!IsRunning ||
                SessionState.GetBool(
                    TransitionScheduledKey,
                    false))
            {
                return;
            }

            SessionState.SetBool(
                TransitionScheduledKey,
                true);
            EditorApplication.delayCall += PrepareAndEnterNextPlayMode;
        }

        private static void PrepareAndEnterNextPlayMode()
        {
            if (!IsRunning)
            {
                SessionState.SetBool(
                    TransitionScheduledKey,
                    false);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(
                    TransitionScheduledKey,
                    false);
                return;
            }

            int index = SessionState.GetInt(StepIndexKey, 0);
            GateStep step = null;

            try
            {
                Require(index >= 0 && index < Steps.Length,
                    $"Invalid Close Gate step index '{index}'.");
                step = Steps[index];

                Debug.Log(
                    $"{Prefix} phase='PreparingFreshPlayMode' " +
                    $"step='{index + 1}/{Steps.Length}' " +
                    $"cut='{step.Cut}' " +
                    $"setup='{SetupMenuPath}'.");

                InvokeM07SetupPreparation();

                Require(IsRunning,
                    "Close Gate stopped while preparing the next Play Mode.");
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,
                    "M07 setup unexpectedly entered Play Mode.");

                Debug.Log(
                    $"{Prefix} phase='FreshPlayModePrepared' " +
                    $"step='{index + 1}/{Steps.Length}' " +
                    $"cut='{step.Cut}' " +
                    $"setup='{SetupMenuPath}'.");

                // Let AssetDatabase.SaveAssets/Refresh and all setup-side Editor
                // callbacks settle before requesting the Play Mode transition.
                EditorApplication.delayCall += () =>
                    EnterPreparedPlayMode(index, step);
            }
            catch (Exception exception)
            {
                SessionState.SetBool(
                    TransitionScheduledKey,
                    false);
                SessionState.SetString(
                    FailureKey,
                    DescribePreparationFailure(
                        index,
                        step,
                        exception));
                CompleteFailed();
            }
        }

        private static void EnterPreparedPlayMode(
            int preparedIndex,
            GateStep preparedStep)
        {
            SessionState.SetBool(
                TransitionScheduledKey,
                false);

            if (!IsRunning)
            {
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            int currentIndex =
                SessionState.GetInt(StepIndexKey, 0);
            if (currentIndex != preparedIndex)
            {
                SessionState.SetString(
                    FailureKey,
                    $"Prepared Close Gate step index changed before Play Mode. " +
                    $"prepared='{preparedIndex}' current='{currentIndex}'.");
                CompleteFailed();
                return;
            }

            Debug.Log(
                $"{Prefix} phase='EnteringFreshPlayMode' " +
                $"step='{preparedIndex + 1}/{Steps.Length}' " +
                $"cut='{preparedStep.Cut}' " +
                $"regression='{preparedStep.ProofName}'.");
            EditorApplication.isPlaying = true;
        }

        private static void InvokeM07SetupPreparation()
        {
            Type setupType = typeof(QaM07InternalReconcileSetup);
            MethodInfo prepare = setupType.GetMethod(
                SetupMethodName,
                StaticAny,
                null,
                Type.EmptyTypes,
                null);
            Require(prepare != null && prepare.ReturnType == typeof(void),
                $"M07 setup '{setupType.FullName}.{SetupMethodName}' is missing or has an unexpected signature.");

            try
            {
                prepare.Invoke(null, null);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static void StartCurrentStepIfRequired()
        {
            if (!IsRunning ||
                !EditorApplication.isPlaying ||
                SessionState.GetBool(ExecutingKey, false))
            {
                return;
            }

            SessionState.SetBool(ExecutingKey, true);
            RunCurrentStepAsync();
        }

        private static async void RunCurrentStepAsync()
        {
            int index = SessionState.GetInt(StepIndexKey, 0);
            GateStep step = null;

            try
            {
                Require(index >= 0 && index < Steps.Length,
                    $"Invalid Close Gate step index '{index}'.");
                step = Steps[index];

                await AwaitFrameworkReadyAsync();
                Require(IsRunning,
                    "Close Gate stopped before the current regression started.");
                Require(EditorApplication.isPlaying,
                    "Close Gate left Play Mode before the current regression started.");

                Debug.Log(
                    $"{Prefix} phase='StepStarted' " +
                    $"step='{index + 1}/{Steps.Length}' " +
                    $"cut='{step.Cut}' " +
                    $"cases='{step.ExpectedCases}' " +
                    $"expectedPrefix='{step.ExpectedPrefix}'.");

                await InvokeRegressionAsync(step);

                Require(EditorApplication.isPlaying,
                    $"Regression '{step.Cut}' left Play Mode before returning success.");

                int passedCases =
                    SessionState.GetInt(PassedCasesKey, 0) +
                    step.ExpectedCases;
                SessionState.SetInt(PassedCasesKey, passedCases);
                AppendCompletedStep(step.Cut);
                SessionState.SetInt(StepIndexKey, index + 1);
                SessionState.SetBool(ExecutingKey, false);

                Debug.Log(
                    $"{Prefix} phase='StepPassed' " +
                    $"step='{index + 1}/{Steps.Length}' " +
                    $"cut='{step.Cut}' " +
                    $"cases='{step.ExpectedCases}' " +
                    $"accumulatedCases='{passedCases}/{ExpectedTotalCases}'.");

                EditorApplication.isPlaying = false;
            }
            catch (Exception exception)
            {
                SessionState.SetBool(ExecutingKey, false);
                SessionState.SetString(
                    FailureKey,
                    DescribeFailure(index, step, exception));

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                }
                else
                {
                    CompleteFailed();
                }
            }
        }

        private static async Task AwaitFrameworkReadyAsync()
        {
            string lastDiagnostic = "<none>";

            for (int frame = 0;
                 frame < FrameworkReadyFrameBudget;
                 frame++)
            {
                if (QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string diagnostic) &&
                    host != null &&
                    host.State.GameFlowStarted)
                {
                    await Awaitable.NextFrameAsync();
                    return;
                }

                lastDiagnostic = diagnostic;
                await Awaitable.NextFrameAsync();
            }

            throw new TimeoutException(
                "IF-M07 Close Gate could not resolve a started official " +
                $"FrameworkRuntimeHost within '{FrameworkReadyFrameBudget}' frames. " +
                $"diagnostic='{lastDiagnostic}'.");
        }

        private static async Task InvokeRegressionAsync(
            GateStep step)
        {
            Type type = ResolveType(step.TypeName);
            MethodInfo runAsync = type.GetMethod(
                "RunAsync",
                StaticAny,
                null,
                Type.EmptyTypes,
                null);
            Require(runAsync != null,
                $"Regression '{step.TypeName}' has no parameterless RunAsync method.");

            object rawResult;
            try
            {
                rawResult = runAsync.Invoke(null, null);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }

            Require(rawResult is Task,
                $"Regression '{step.TypeName}.RunAsync' did not return Task.");
            await (Task)rawResult;
        }

        private static void ValidateGateDefinitions()
        {
            Type setupType = typeof(QaM07InternalReconcileSetup);
            Require(ReadConstant<string>(setupType, "MenuPath") ==
                    SetupMenuPath,
                "M07 setup MenuPath diverged from the Close Gate contract.");
            MethodInfo setupPrepare = setupType.GetMethod(
                SetupMethodName,
                StaticAny,
                null,
                Type.EmptyTypes,
                null);
            Require(setupPrepare != null &&
                setupPrepare.ReturnType == typeof(void),
                $"M07 setup '{setupType.FullName}.{SetupMethodName}' is missing or has an unexpected signature.");

            Require(Steps.Length == 6,
                $"IF-M07 Close Gate requires exactly six regressions, found '{Steps.Length}'.");
            Require(ExpectedTotalCases == 230,
                $"IF-M07 Close Gate expected case total diverged. expected='230' actual='{ExpectedTotalCases}'.");

            var types = new HashSet<string>(StringComparer.Ordinal);
            var cuts = new HashSet<string>(StringComparer.Ordinal);
            var prefixes = new HashSet<string>(StringComparer.Ordinal);

            foreach (GateStep step in Steps)
            {
                Require(types.Add(step.TypeName),
                    $"Duplicate Close Gate regression type '{step.TypeName}'.");
                Require(cuts.Add(step.Cut),
                    $"Duplicate Close Gate cut '{step.Cut}'.");
                Require(prefixes.Add(step.ExpectedPrefix),
                    $"Duplicate Close Gate result prefix '{step.ExpectedPrefix}'.");

                Type type = ResolveType(step.TypeName);
                Require(type != typeof(QaM07CloseGateRegression),
                    "Close Gate cannot include itself as a regression step.");

                MethodInfo runAsync = type.GetMethod(
                    "RunAsync",
                    StaticAny,
                    null,
                    Type.EmptyTypes,
                    null);
                Require(runAsync != null &&
                    typeof(Task).IsAssignableFrom(runAsync.ReturnType),
                    $"Regression '{step.TypeName}' has no canonical Task-returning RunAsync method.");

                Require(ReadConstant<string>(type, "MenuPath") ==
                        step.ExpectedMenuPath,
                    $"Regression '{step.TypeName}' MenuPath diverged from the Close Gate contract.");
                Require(ReadConstant<string>(type, "Prefix") ==
                        step.ExpectedPrefix,
                    $"Regression '{step.TypeName}' Prefix diverged from the Close Gate contract.");
                Require(ReadConstant<int>(type, "ExpectedCaseCount") ==
                        step.ExpectedCases,
                    $"Regression '{step.TypeName}' ExpectedCaseCount diverged from the Close Gate contract.");
            }
        }

        private static T ReadConstant<T>(
            Type type,
            string fieldName)
        {
            FieldInfo field = type.GetField(
                fieldName,
                StaticAny);
            Require(field != null && field.IsLiteral,
                $"Regression '{type.FullName}' has no constant '{fieldName}'.");

            object value = field.GetRawConstantValue();
            Require(value is T,
                $"Regression '{type.FullName}.{fieldName}' has unexpected type '{value?.GetType().FullName ?? "<null>"}'.");
            return (T)value;
        }

        private static Type ResolveType(string fullName)
        {
            foreach (Assembly assembly in
                     AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(
                    fullName,
                    false);
                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException(
                $"Required regression type '{fullName}' was not found in loaded assemblies.");
        }

        private static void AppendCompletedStep(string cut)
        {
            string completed = SessionState.GetString(
                CompletedStepsKey,
                string.Empty);
            SessionState.SetString(
                CompletedStepsKey,
                string.IsNullOrWhiteSpace(completed)
                    ? cut
                    : $"{completed},{cut}");
        }

        private static void CompletePassed()
        {
            int passedCases =
                SessionState.GetInt(PassedCasesKey, 0);
            string completed =
                SessionState.GetString(
                    CompletedStepsKey,
                    string.Empty);

            Require(passedCases == ExpectedTotalCases,
                $"Close Gate reached the end with '{passedCases}/{ExpectedTotalCases}' cases.");

            ResetState();
            Debug.Log(
                $"{Prefix} status='Passed' " +
                $"steps='{Steps.Length}/{Steps.Length}' " +
                $"cases='{passedCases}' " +
                $"freshPlayModes='{Steps.Length}' " +
                "proof='InternalReconcile,ActivitySessionProjection,RequirementPolicyMatrix,ZeroParticipantPolicyMatrix,ActiveProjectionFreeze,IncludedExcludedFailureReleaseScope' " +
                $"completed='{completed}'.");
        }

        private static void CompleteFailed()
        {
            int index = SessionState.GetInt(StepIndexKey, 0);
            int passedCases =
                SessionState.GetInt(PassedCasesKey, 0);
            string completed =
                SessionState.GetString(
                    CompletedStepsKey,
                    string.Empty);
            string failure =
                SessionState.GetString(
                    FailureKey,
                    "Close Gate failed without a diagnostic.");

            string step = index >= 0 && index < Steps.Length
                ? Steps[index].Cut
                : "<none>";

            ResetState();
            Debug.LogError(
                $"{Prefix} status='Failed' " +
                $"steps='{index}/{Steps.Length}' " +
                $"cases='{passedCases}/{ExpectedTotalCases}' " +
                $"failedStep='{step}' " +
                $"completed='{completed}' " +
                $"execution='{Escape(failure)}'.");
        }

        private static string DescribePreparationFailure(
            int index,
            GateStep step,
            Exception exception)
        {
            string cut = step != null
                ? step.Cut
                : "<unresolved>";
            return
                $"phase='EditModePreparation' " +
                $"step='{index + 1}/{Steps.Length}' cut='{cut}' " +
                $"setup='{SetupMenuPath}' exception='{exception}'.";
        }

        private static string DescribeFailure(
            int index,
            GateStep step,
            Exception exception)
        {
            string cut = step != null
                ? step.Cut
                : "<unresolved>";
            return
                $"step='{index + 1}/{Steps.Length}' cut='{cut}' " +
                $"exception='{exception}'.";
        }

        private static string DescribePlan()
        {
            var parts = new string[Steps.Length];
            for (int index = 0;
                 index < Steps.Length;
                 index++)
            {
                GateStep step = Steps[index];
                parts[index] =
                    $"{step.Cut}:{step.ExpectedCases}";
            }

            return string.Join(",", parts);
        }

        private static int CalculateExpectedTotalCases()
        {
            int total = 0;
            foreach (GateStep step in Steps)
            {
                total += step.ExpectedCases;
            }

            return total;
        }

        private static void ResetState()
        {
            SessionState.SetBool(RunningKey, false);
            SessionState.SetInt(StepIndexKey, 0);
            SessionState.SetInt(PassedCasesKey, 0);
            SessionState.SetString(
                CompletedStepsKey,
                string.Empty);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetBool(ExecutingKey, false);
            SessionState.SetBool(
                TransitionScheduledKey,
                false);
        }

        private static string Escape(Exception exception)
        {
            return Escape(exception?.ToString());
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? "<none>"
                : value
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class GateStep
        {
            internal GateStep(
                string cut,
                string typeName,
                string expectedMenuPath,
                string expectedPrefix,
                int expectedCases,
                string proofName)
            {
                Cut = cut;
                TypeName = typeName;
                ExpectedMenuPath = expectedMenuPath;
                ExpectedPrefix = expectedPrefix;
                ExpectedCases = expectedCases;
                ProofName = proofName;
            }

            internal string Cut { get; }
            internal string TypeName { get; }
            internal string ExpectedMenuPath { get; }
            internal string ExpectedPrefix { get; }
            internal int ExpectedCases { get; }
            internal string ProofName { get; }
        }
    }
}

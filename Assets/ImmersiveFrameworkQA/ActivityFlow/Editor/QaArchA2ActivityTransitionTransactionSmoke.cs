using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.ActivityFlow.Editor
{
    /// <summary>
    /// Focused QA proof for ARCH-A2 Activity transition transaction semantics.
    /// It does not depend on Player, camera, provisioning, or P3M5B authoring.
    /// </summary>
    public static class QaArchA2ActivityTransitionTransactionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Activity Flow/Run Activity Transition Transaction Regression";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string ActivityFlowRuntimeTypeName =
            "Immersive.Framework.ActivityFlow.ActivityFlowRuntime";
        private const string TransactionTypeName =
            "Immersive.Framework.ActivityFlow.ActivityTransitionRuntimeTransaction";
        private const string ReadinessTypeName =
            "Immersive.Framework.ActivityFlow.ActivityReadinessState";
        private const string ReadinessStatusTypeName =
            "Immersive.Framework.ActivityFlow.ActivityReadinessStatus";
        private const string ContentSetTypeName =
            "Immersive.Framework.ActivityFlow.ActivityContentSet";
        private const string ContentLifecycleTypeName =
            "Immersive.Framework.ActivityFlow.ActivityContentLifecycleResult";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();
            Exception failure = null;
            object runtimeHost = null;
            ActivityAsset originalActivity = null;
            ActivityAsset syntheticPrevious = null;
            ActivityAsset syntheticTarget = null;

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "ARCH-A2 smoke must run in Play Mode.");
                completed.Add("play-mode-required");

                Assembly frameworkAssembly = typeof(ActivityAsset).Assembly;
                Type transactionType = frameworkAssembly.GetType(
                    TransactionTypeName,
                    true);
                Type flowRuntimeType = frameworkAssembly.GetType(
                    ActivityFlowRuntimeTypeName,
                    true);
                AssertNotNull(transactionType,
                    "ARCH-A2 transaction type is unavailable.");
                AssertNotNull(flowRuntimeType,
                    "ActivityFlowRuntime type is unavailable.");
                completed.Add("transaction-types-resolved");

                syntheticPrevious = ScriptableObject.CreateInstance<ActivityAsset>();
                syntheticPrevious.name = "ARCH-A2 Synthetic Previous Activity";
                syntheticTarget = ScriptableObject.CreateInstance<ActivityAsset>();
                syntheticTarget.name = "ARCH-A2 Synthetic Target Activity";

                RunDirectTransactionMatrix(
                    frameworkAssembly,
                    flowRuntimeType,
                    transactionType,
                    syntheticPrevious,
                    syntheticTarget,
                    completed);

                runtimeHost = await AwaitRuntimeHostAsync();
                originalActivity = ResolveCurrentActivity(runtimeHost);
                AssertNotNull(originalActivity,
                    "ARCH-A2 integrated proof requires one active Activity.");
                completed.Add("runtime-host-ready");

                object clearRequest = await InvokeTaskResultAsync(
                    runtimeHost,
                    "ClearActivityAsync",
                    nameof(QaArchA2ActivityTransitionTransactionSmoke),
                    "arch-a2-integrated-clear");
                AssertRequestSucceeded(
                    clearRequest,
                    "ARCH-A2 integrated clear failed.");
                object clearFlow = GetPropertyValue(
                    clearRequest,
                    "ActivityFlowResult");
                object clearSnapshot = GetPropertyValue(
                    clearFlow,
                    "ActivityTransitionSnapshot");
                int clearSequence = AssertIntegratedSnapshot(
                    clearSnapshot,
                    "CommittedReady",
                    originalActivity,
                    null,
                    "Succeeded",
                    "None",
                    "ARCH-A2 integrated clear");
                AssertTrue(ResolveCurrentActivity(runtimeHost) == null,
                    "ARCH-A2 clear did not commit the no-active-Activity authority state.");
                completed.Add("integrated-clear-committed-ready");

                object restartRequest = await InvokeTaskResultAsync(
                    runtimeHost,
                    "RequestActivityAsync",
                    originalActivity,
                    nameof(QaArchA2ActivityTransitionTransactionSmoke),
                    "arch-a2-integrated-restart");
                AssertRequestSucceeded(
                    restartRequest,
                    "ARCH-A2 integrated restart failed.");
                object restartFlow = GetPropertyValue(
                    restartRequest,
                    "ActivityFlowResult");
                object restartSnapshot = GetPropertyValue(
                    restartFlow,
                    "ActivityTransitionSnapshot");
                int restartSequence = AssertIntegratedSnapshot(
                    restartSnapshot,
                    "CommittedReady",
                    null,
                    originalActivity,
                    "NotRequired",
                    "Ready",
                    "ARCH-A2 integrated restart");
                AssertTrue(restartSequence > clearSequence,
                    "ARCH-A2 transition sequence did not advance between clear and restart.");
                AssertSame(originalActivity, ResolveCurrentActivity(runtimeHost),
                    "ARCH-A2 restart did not restore the original Activity authority.");
                completed.Add("integrated-restart-committed-ready");
                completed.Add("integrated-sequence-monotonic");
            }
            catch (Exception exception)
            {
                failure = Unwrap(exception);
            }
            finally
            {
                if (runtimeHost != null && originalActivity != null)
                {
                    try
                    {
                        ActivityAsset currentActivity = ResolveCurrentActivity(runtimeHost);
                        if (!ReferenceEquals(currentActivity, originalActivity))
                        {
                            object restoreRequest = await InvokeTaskResultAsync(
                                runtimeHost,
                                "RequestActivityAsync",
                                originalActivity,
                                nameof(QaArchA2ActivityTransitionTransactionSmoke),
                                "arch-a2-finally-restore-original-activity");
                            AssertRequestSucceeded(
                                restoreRequest,
                                "ARCH-A2 finally restore failed.");
                        }
                    }
                    catch (Exception restoreException)
                    {
                        Exception resolvedRestore = Unwrap(restoreException);
                        failure = failure == null
                            ? resolvedRestore
                            : new AggregateException(failure, resolvedRestore);
                    }
                }

                if (syntheticPrevious != null)
                {
                    UnityEngine.Object.DestroyImmediate(syntheticPrevious);
                }

                if (syntheticTarget != null)
                {
                    UnityEngine.Object.DestroyImmediate(syntheticTarget);
                }
            }

            if (failure != null)
            {
                Debug.LogError(
                    "[ARCH_A2_ACTIVITY_TRANSITION_TRANSACTION_SMOKE] " +
                    $"status='Failed' exception='{failure.GetType().Name}' " +
                    $"message='{Escape(failure.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw failure;
            }

            Debug.Log(
                "[ARCH_A2_ACTIVITY_TRANSITION_TRANSACTION_SMOKE] " +
                $"status='Passed' cases='{completed.Count}' " +
                $"completed='{string.Join(",", completed)}'.");
        }

        private static void RunDirectTransactionMatrix(
            Assembly frameworkAssembly,
            Type flowRuntimeType,
            Type transactionType,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            List<string> completed)
        {
            object failedBeforeCommit = CreateTransaction(
                transactionType,
                1,
                previousActivity,
                targetActivity,
                "arch-a2-direct",
                "failed-before-commit");
            object failedSnapshot = InvokeMember(
                failedBeforeCommit,
                "FailBeforeCommit",
                "Synthetic pre-commit failure.");
            AssertSnapshotStatus(
                failedSnapshot,
                "FailedBeforeCommit",
                commitReached: false,
                "FailedBeforeCommit direct case");
            completed.Add("failed-before-commit-terminal");

            object previousOrder = CreateTransaction(
                transactionType,
                2,
                previousActivity,
                targetActivity,
                "arch-a2-direct",
                "previous-exit-order");
            InvokeMember(previousOrder, "MarkReadyToCommit", "ready");
            InvokeMember(previousOrder, "Commit", "committed");
            InvokeMember(previousOrder, "BeginPreviousExit", "previous exit");
            InvokeMember(previousOrder, "MarkPreviousContentExited", "content exited");
            AssertThrowsInvalidOperation(
                () => InvokeMember(previousOrder, "BeginTargetEnter", "target enter"),
                "Target Enter was allowed before participant Exit.");
            completed.Add("target-enter-before-previous-exit-rejected");

            object targetOrder = CreateTransaction(
                transactionType,
                3,
                previousActivity,
                targetActivity,
                "arch-a2-direct",
                "target-enter-order");
            InvokeMember(targetOrder, "MarkReadyToCommit", "ready");
            InvokeMember(targetOrder, "Commit", "committed");
            InvokeMember(targetOrder, "BeginPreviousExit", "previous exit");
            InvokeMember(targetOrder, "MarkPreviousContentExited", "content exited");
            InvokeMember(targetOrder, "MarkPreviousParticipantsExited", "participants exited");
            InvokeMember(targetOrder, "BeginTargetEnter", "target enter");
            AssertThrowsInvalidOperation(
                () => InvokeMember(targetOrder, "MarkTargetContentEntered", "content entered"),
                "Target content Enter was allowed before target participant Enter.");
            completed.Add("target-content-before-participant-enter-rejected");

            object readyState = CreateReadinessState(
                frameworkAssembly,
                "Ready",
                targetActivity,
                0,
                "SyntheticReady");
            object readySnapshot = CompleteTransaction(
                transactionType,
                4,
                null,
                targetActivity,
                readyState,
                previousFinalizationSucceeded: true,
                previousSceneReleaseSucceeded: true,
                "committed-ready");
            AssertSnapshotStatus(
                readySnapshot,
                "CommittedReady",
                commitReached: true,
                "CommittedReady direct case");
            AssertEqual("NotRequired",
                GetPropertyValue(readySnapshot, "PreviousFinalizationStatus").ToString(),
                "CommittedReady direct case returned an unexpected previous finalization status.");
            completed.Add("committed-ready-terminal");

            object notReadyState = CreateReadinessState(
                frameworkAssembly,
                "NotReady",
                targetActivity,
                1,
                "SyntheticBlockingIssue");
            object notReadySnapshot = CompleteTransaction(
                transactionType,
                5,
                null,
                targetActivity,
                notReadyState,
                previousFinalizationSucceeded: true,
                previousSceneReleaseSucceeded: true,
                "committed-not-ready");
            AssertSnapshotStatus(
                notReadySnapshot,
                "CommittedNotReady",
                commitReached: true,
                "CommittedNotReady direct case");
            completed.Add("committed-not-ready-terminal");

            object finalizationFailedSnapshot = CompleteTransaction(
                transactionType,
                6,
                previousActivity,
                targetActivity,
                readyState,
                previousFinalizationSucceeded: false,
                previousSceneReleaseSucceeded: false,
                "committed-finalization-failed");
            AssertSnapshotStatus(
                finalizationFailedSnapshot,
                "CommittedFinalizationFailed",
                commitReached: true,
                "CommittedFinalizationFailed direct case");
            AssertEqual("Failed",
                GetPropertyValue(finalizationFailedSnapshot, "PreviousFinalizationStatus").ToString(),
                "CommittedFinalizationFailed did not retain failed previous finalization evidence.");
            completed.Add("committed-finalization-failed-terminal");

            object completedTransaction = CreateCompletedTransaction(
                transactionType,
                7,
                targetActivity,
                readyState);
            AssertThrowsInvalidOperation(
                () => InvokeMember(completedTransaction, "BeginPreviousExit", "illegal terminal mutation"),
                "Completed transaction accepted a post-terminal mutation.");
            completed.Add("post-terminal-mutation-rejected");

            AssertConcurrentTransactionRejected(
                flowRuntimeType,
                transactionType,
                previousActivity,
                targetActivity);
            completed.Add("concurrent-transaction-rejected");
        }

        private static object CreateCompletedTransaction(
            Type transactionType,
            int sequence,
            ActivityAsset targetActivity,
            object readinessState)
        {
            object transaction = CreateTransaction(
                transactionType,
                sequence,
                null,
                targetActivity,
                "arch-a2-direct",
                "terminal-mutation");
            CompleteExistingTransaction(
                transaction,
                readinessState,
                previousFinalizationSucceeded: true,
                previousSceneReleaseSucceeded: true,
                "terminal-mutation");
            return transaction;
        }

        private static object CompleteTransaction(
            Type transactionType,
            int sequence,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            object readinessState,
            bool previousFinalizationSucceeded,
            bool previousSceneReleaseSucceeded,
            string reason)
        {
            object transaction = CreateTransaction(
                transactionType,
                sequence,
                previousActivity,
                targetActivity,
                "arch-a2-direct",
                reason);
            return CompleteExistingTransaction(
                transaction,
                readinessState,
                previousFinalizationSucceeded,
                previousSceneReleaseSucceeded,
                reason);
        }

        private static object CompleteExistingTransaction(
            object transaction,
            object readinessState,
            bool previousFinalizationSucceeded,
            bool previousSceneReleaseSucceeded,
            string reason)
        {
            InvokeMember(transaction, "MarkReadyToCommit", "ready");
            InvokeMember(transaction, "Commit", "committed");
            InvokeMember(transaction, "BeginPreviousExit", "previous exit");
            InvokeMember(transaction, "MarkPreviousContentExited", "content exited");
            InvokeMember(transaction, "MarkPreviousParticipantsExited", "participants exited");
            InvokeMember(transaction, "BeginTargetEnter", "target enter");
            InvokeMember(transaction, "MarkTargetParticipantsEntered", "participants entered");
            InvokeMember(transaction, "MarkTargetContentEntered", "content entered");
            InvokeMember(transaction, "BeginPreviousFinalization", "previous finalization");
            InvokeMember(
                transaction,
                "MarkPreviousFinalized",
                previousFinalizationSucceeded,
                "previous finalized");
            InvokeMember(
                transaction,
                "MarkPreviousScenesReleased",
                previousSceneReleaseSucceeded,
                "previous scenes released");
            return InvokeMember(
                transaction,
                "Complete",
                readinessState,
                previousFinalizationSucceeded,
                previousSceneReleaseSucceeded,
                reason);
        }

        private static void AssertConcurrentTransactionRejected(
            Type flowRuntimeType,
            Type transactionType,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity)
        {
#pragma warning disable SYSLIB0050
            object uninitializedFlow = FormatterServices.GetUninitializedObject(
                flowRuntimeType);
#pragma warning restore SYSLIB0050
            object activeTransaction = CreateTransaction(
                transactionType,
                10,
                previousActivity,
                targetActivity,
                "arch-a2-direct",
                "active-concurrent-transaction");
            FieldInfo activeField = flowRuntimeType.GetField(
                "_activeActivityTransition",
                InstanceAny);
            AssertNotNull(activeField,
                "ActivityFlowRuntime active transaction field is unavailable.");
            activeField.SetValue(uninitializedFlow, activeTransaction);

            object[] arguments =
            {
                previousActivity,
                targetActivity,
                "arch-a2-direct",
                "rejected-concurrent-transaction",
                null,
                null
            };
            bool accepted = Convert.ToBoolean(InvokeMember(
                uninitializedFlow,
                "TryBeginActivityTransition",
                arguments));
            AssertFalse(accepted,
                "ActivityFlowRuntime accepted a second non-terminal transaction.");
            AssertTrue(arguments[4] == null,
                "Rejected concurrent transaction unexpectedly returned a transaction.");
            string issue = Convert.ToString(arguments[5]);
            AssertTrue(!string.IsNullOrWhiteSpace(issue) &&
                issue.IndexOf("non-terminal", StringComparison.OrdinalIgnoreCase) >= 0,
                "Concurrent transaction rejection did not return explicit diagnostics.");
        }

        private static object CreateTransaction(
            Type transactionType,
            int sequence,
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            ConstructorInfo constructor = FindConstructor(
                transactionType,
                parameterCount: 5);
            return constructor.Invoke(new object[]
            {
                sequence,
                previousActivity,
                targetActivity,
                source,
                reason
            });
        }

        private static object CreateReadinessState(
            Assembly frameworkAssembly,
            string statusName,
            ActivityAsset activity,
            int blockingIssueCount,
            string diagnosticReason)
        {
            Type readinessType = frameworkAssembly.GetType(
                ReadinessTypeName,
                true);
            Type readinessStatusType = frameworkAssembly.GetType(
                ReadinessStatusTypeName,
                true);
            Type contentSetType = frameworkAssembly.GetType(
                ContentSetTypeName,
                true);
            Type contentLifecycleType = frameworkAssembly.GetType(
                ContentLifecycleTypeName,
                true);
            ConstructorInfo constructor = FindConstructor(
                readinessType,
                parameterCount: 8);
            object status = Enum.Parse(
                readinessStatusType,
                statusName,
                ignoreCase: false);
            object contentSet = Activator.CreateInstance(contentSetType);
            object lifecycle = Activator.CreateInstance(contentLifecycleType);
            return constructor.Invoke(new[]
            {
                status,
                activity,
                contentSet,
                lifecycle,
                (object)blockingIssueCount,
                "QaArchA2ActivityTransitionTransactionSmoke",
                "direct-transaction-matrix",
                diagnosticReason
            });
        }

        private static void AssertSnapshotStatus(
            object snapshot,
            string expectedTerminal,
            bool commitReached,
            string label)
        {
            AssertNotNull(snapshot,
                label + " returned no snapshot.");
            AssertTrue(GetBooleanProperty(snapshot, "IsTerminal"),
                label + " did not become terminal.");
            AssertEqual(expectedTerminal,
                GetPropertyValue(snapshot, "TerminalStatus").ToString(),
                label + " returned an unexpected terminal status.");
            AssertEqual(commitReached,
                GetBooleanProperty(snapshot, "CommitReached"),
                label + " returned unexpected commit evidence.");
        }

        private static int AssertIntegratedSnapshot(
            object snapshot,
            string expectedTerminal,
            ActivityAsset expectedPrevious,
            ActivityAsset expectedTarget,
            string expectedPreviousFinalization,
            string expectedReadiness,
            string label)
        {
            AssertSnapshotStatus(
                snapshot,
                expectedTerminal,
                commitReached: true,
                label);
            AssertSame(expectedPrevious,
                GetPropertyValue(snapshot, "PreviousActivity") as ActivityAsset,
                label + " returned an unexpected previous Activity.");
            AssertSame(expectedTarget,
                GetPropertyValue(snapshot, "TargetActivity") as ActivityAsset,
                label + " returned an unexpected target Activity.");
            AssertTrue(GetBooleanProperty(snapshot, "PreviousContentExited"),
                label + " did not record previous content Exit completion.");
            AssertTrue(GetBooleanProperty(snapshot, "PreviousParticipantsExited"),
                label + " did not record previous participant Exit completion.");
            AssertTrue(GetBooleanProperty(snapshot, "TargetParticipantsEntered"),
                label + " did not record target participant Enter completion.");
            AssertTrue(GetBooleanProperty(snapshot, "TargetContentEntered"),
                label + " did not record target content Enter completion.");
            AssertTrue(GetBooleanProperty(snapshot, "PreviousScenesReleased"),
                label + " did not record previous scene release completion.");
            AssertEqual(expectedPreviousFinalization,
                GetPropertyValue(snapshot, "PreviousFinalizationStatus").ToString(),
                label + " returned unexpected previous finalization evidence.");
            object readiness = GetPropertyValue(snapshot, "ReadinessState");
            AssertEqual(expectedReadiness,
                GetStringProperty(readiness, "DiagnosticStatus"),
                label + " returned unexpected readiness evidence.");
            int sequence = GetIntProperty(snapshot, "Sequence");
            AssertTrue(sequence > 0,
                label + " returned a non-positive transaction sequence.");
            return sequence;
        }

        private static async Task<object> AwaitRuntimeHostAsync()
        {
            Type runtimeHostType = typeof(ImmersiveFrameworkSettingsAsset)
                .Assembly
                .GetType(RuntimeHostTypeName, true);
            FieldInfo currentField = runtimeHostType.GetField(
                "_current",
                StaticAny);
            AssertNotNull(currentField,
                "FrameworkRuntimeHost current field is unavailable.");

            for (int frame = 0; frame < 300; frame++)
            {
                object current = currentField.GetValue(null);
                if (current != null)
                {
                    object state = GetPropertyValue(current, "State");
                    if (state != null &&
                        GetBooleanProperty(state, "GameFlowStarted") &&
                        GetPropertyValue(state, "CurrentActivity") is ActivityAsset)
                    {
                        return current;
                    }
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "FrameworkRuntimeHost did not become ready with an active Activity within 300 frames.");
        }

        private static ActivityAsset ResolveCurrentActivity(object runtimeHost)
        {
            object state = GetPropertyValue(runtimeHost, "State");
            return state == null
                ? null
                : GetPropertyValue(state, "CurrentActivity") as ActivityAsset;
        }

        private static async Task<object> InvokeTaskResultAsync(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = FindMethod(
                target.GetType(),
                methodName,
                arguments.Length);
            object invocation;
            try
            {
                invocation = method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw Unwrap(exception);
            }

            Task task = invocation as Task;
            AssertNotNull(task,
                $"Method '{methodName}' did not return a Task.");
            await task;
            PropertyInfo resultProperty = invocation.GetType().GetProperty(
                "Result",
                InstanceAny);
            AssertNotNull(resultProperty,
                $"Task returned by '{methodName}' has no Result property.");
            return resultProperty.GetValue(invocation);
        }

        private static void AssertRequestSucceeded(
            object result,
            string message)
        {
            AssertNotNull(result,
                message + " No request result was returned.");
            AssertTrue(GetBooleanProperty(result, "Succeeded"),
                message + " " + GetStringProperty(result, "Message"));
        }

        private static ConstructorInfo FindConstructor(
            Type type,
            int parameterCount)
        {
            ConstructorInfo[] constructors = type.GetConstructors(InstanceAny);
            for (int index = 0; index < constructors.Length; index++)
            {
                if (constructors[index].GetParameters().Length == parameterCount)
                {
                    return constructors[index];
                }
            }

            throw new MissingMethodException(
                type.FullName,
                $".ctor({parameterCount} parameters)");
        }

        private static MethodInfo FindMethod(
            Type type,
            string methodName,
            int argumentCount)
        {
            MethodInfo[] methods = type.GetMethods(InstanceAny);
            for (int index = 0; index < methods.Length; index++)
            {
                if (string.Equals(
                        methods[index].Name,
                        methodName,
                        StringComparison.Ordinal) &&
                    methods[index].GetParameters().Length == argumentCount)
                {
                    return methods[index];
                }
            }

            throw new MissingMethodException(
                type.FullName,
                $"{methodName}({argumentCount} arguments)");
        }

        private static object InvokeMember(
            object target,
            string methodName,
            params object[] arguments)
        {
            AssertNotNull(target,
                $"Cannot invoke '{methodName}' on a null target.");
            MethodInfo method = FindMethod(
                target.GetType(),
                methodName,
                arguments.Length);
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw Unwrap(exception);
            }
        }

        private static object GetPropertyValue(
            object target,
            string propertyName)
        {
            AssertNotNull(target,
                $"Cannot read property '{propertyName}' from null target.");
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                InstanceAny);
            AssertNotNull(property,
                $"Missing property '{propertyName}' on '{target.GetType().Name}'.");
            return property.GetValue(target);
        }

        private static int GetIntProperty(
            object target,
            string propertyName)
        {
            return Convert.ToInt32(GetPropertyValue(target, propertyName));
        }

        private static bool GetBooleanProperty(
            object target,
            string propertyName)
        {
            return Convert.ToBoolean(GetPropertyValue(target, propertyName));
        }

        private static string GetStringProperty(
            object target,
            string propertyName)
        {
            return Convert.ToString(GetPropertyValue(target, propertyName));
        }

        private static void AssertThrowsInvalidOperation(
            Action action,
            string message)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Exception resolved = Unwrap(exception);
                if (resolved is InvalidOperationException)
                {
                    return;
                }


                throw new InvalidOperationException(
                    message +
                    $" Unexpected exception='{resolved.GetType().Name}' message='{resolved.Message}'.",
                    resolved);
            }

            throw new InvalidOperationException(message);
        }

        private static Exception Unwrap(Exception exception)
        {
            if (exception is TargetInvocationException invocation &&
                invocation.InnerException != null)
            {
                return Unwrap(invocation.InnerException);
            }

            return exception;
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            AssertTrue(!condition, message);
        }

        private static void AssertNotNull(object value, string message)
        {
            AssertTrue(value != null, message);
        }

        private static void AssertSame(
            object expected,
            object actual,
            string message)
        {
            AssertTrue(ReferenceEquals(expected, actual), message);
        }

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

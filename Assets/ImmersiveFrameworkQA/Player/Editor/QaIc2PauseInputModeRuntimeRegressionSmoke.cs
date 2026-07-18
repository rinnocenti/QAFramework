using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    public static class QaIc2PauseInputModeRuntimeRegressionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Input Mode/IC2 Run Pause Runtime Regression Smoke";
        private const string LogPrefix =
            "[IC2_PAUSE_INPUT_MODE_RUNTIME_REGRESSION_SMOKE]";
        private const string RunnerTypeName =
            "Immersive.Framework.Diagnostics." +
            "PauseInputModeUnityPlayerInputRuntimeBridgeQaSmokeRunner";
        private const string LoggerTypeName =
            "Immersive.Framework.Diagnostics.FrameworkLogger";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();

            try
            {
                Require(EditorApplication.isPlaying,
                    "IC2 runtime regression requires Play Mode.");
                completed.Add("play-mode-required");

                Assembly frameworkAssembly =
                    typeof(Immersive.Framework.InputMode.InputModeRuntimeContext)
                        .Assembly;
                Type runtimeHostType = frameworkAssembly.GetType(
                    "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost",
                    throwOnError: false);
                Require(runtimeHostType != null,
                    "Package FrameworkRuntimeHost type is unavailable.");
                MethodInfo tryGetCurrent = RequireMethod(
                    runtimeHostType,
                    "TryGetCurrent",
                    BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic,
                    parameterCount: 1);
                var hostArguments = new object[] { null };
                object resolvedCurrent = tryGetCurrent.Invoke(null, hostArguments);
                Require(resolvedCurrent is bool && (bool)resolvedCurrent &&
                        hostArguments[0] != null,
                    "FrameworkRuntimeHost is unavailable.");
                object runtimeHost = hostArguments[0];
                completed.Add("runtime-host-available");

                Type runnerType = frameworkAssembly.GetType(
                    RunnerTypeName,
                    throwOnError: false);
                Require(runnerType != null,
                    $"Package runtime smoke runner '{RunnerTypeName}' is unavailable.");

                Type loggerType = frameworkAssembly.GetType(
                    LoggerTypeName,
                    throwOnError: false);
                Require(loggerType != null,
                    $"Package logger '{LoggerTypeName}' is unavailable.");
                MethodInfo createLogger = RequireMethod(
                    loggerType,
                    "Create",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    parameterCount: 1);
                object logger = createLogger.Invoke(
                    null,
                    new object[]
                    {
                        typeof(QaIc2PauseInputModeRuntimeRegressionSmoke)
                    });
                Require(logger != null,
                    "Could not create package diagnostics logger.");

                MethodInfo run = RequireMethod(
                    runnerType,
                    "RunRuntimeBridgeSmokeAsync",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    parameterCount: 3);
                object taskObject = run.Invoke(
                    null,
                    new[]
                    {
                        runtimeHost,
                        logger,
                        nameof(QaIc2PauseInputModeRuntimeRegressionSmoke)
                    });
                bool passed = await AwaitBooleanTask(taskObject);
                Require(passed,
                    "Existing Pause Runtime PlayerInput Bridge regression failed.");
                completed.Add("f33a-runtime-bridge-regression");

                Require(completed.Count == 3,
                    "IC2 runtime regression case count changed unexpectedly.");
                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception resolved = exception is TargetInvocationException
                    { InnerException: not null }
                    ? exception.InnerException
                    : exception;
                Debug.LogError(
                    $"{LogPrefix} status='Failed' " +
                    $"exception='{resolved.GetType().Name}' " +
                    $"message='{Escape(resolved.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw resolved;
            }
        }

        private static MethodInfo RequireMethod(
            Type type,
            string name,
            BindingFlags flags,
            int parameterCount)
        {
            MethodInfo[] methods = type.GetMethods(flags);
            for (int index = 0; index < methods.Length; index++)
            {
                MethodInfo method = methods[index];
                if (method.Name == name &&
                    method.GetParameters().Length == parameterCount)
                {
                    return method;
                }
            }

            throw new InvalidOperationException(
                $"Required method '{type.FullName}.{name}' is unavailable.");
        }

        private static async Task<bool> AwaitBooleanTask(object taskObject)
        {
            Require(taskObject is Task,
                "Package runtime smoke did not return a Task.");
            var task = (Task)taskObject;
            await task;

            PropertyInfo resultProperty = task.GetType().GetProperty(
                "Result",
                BindingFlags.Instance | BindingFlags.Public);
            Require(resultProperty != null,
                "Package runtime smoke task has no Boolean Result.");
            object result = resultProperty.GetValue(task);
            Require(result is bool,
                "Package runtime smoke task Result is not Boolean.");
            return (bool)result;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
    }
}

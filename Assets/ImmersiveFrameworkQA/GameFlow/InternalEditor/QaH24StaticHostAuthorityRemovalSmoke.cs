using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Immersive.Framework.ApplicationLifecycle;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH24StaticHostAuthorityRemovalSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Bootstrap/Run Runtime Host Authority Regression";

        private const string LogPrefix =
            "[H24_STATIC_HOST_AUTHORITY_REMOVAL_SMOKE]";

        private static readonly Regex StaticLookupInvocation =
            new Regex(
                "FrameworkRuntimeHost\\s*\\.\\s*" +
                "TryGet" +
                "Current\\s*\\(",
                RegexOptions.CultureInvariant);

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed =
                new List<string>();

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "H2.4 closure smoke requires Play Mode.");
                completed.Add("play-mode-required");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string hostDiagnostic),
                    "H2.4 QA resolver did not resolve exactly one loaded host. " +
                    hostDiagnostic);
                completed.Add("qa-harness-resolves-one-loaded-host");

                FrameworkRuntimeState state =
                    host.State;
                Require(
                    state.GameFlowStarted &&
                    state.CurrentRoute != null &&
                    state.CurrentActivity != null &&
                    state.IsActivityReady,
                    $"Framework runtime is not ready. {hostDiagnostic} " +
                    $"gameFlowStarted='{state.GameFlowStarted}' " +
                    $"route='{state.CurrentRouteName}' " +
                    $"activity='{state.CurrentActivityName}' " +
                    $"activityReady='{state.IsActivityReady}'.");
                completed.Add("resolved-host-is-canonical-and-ready");

                Type hostType =
                    typeof(FrameworkRuntimeHost);
                string legacyFieldName =
                    "_" + "current";
                Require(
                    hostType.GetField(
                        legacyFieldName,
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic) == null,
                    "FrameworkRuntimeHost still declares the legacy static host field.");
                completed.Add("static-host-field-absent");

                string legacyMethodName =
                    "TryGet" + "Current";
                Require(
                    hostType.GetMethod(
                        legacyMethodName,
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic) == null,
                    "FrameworkRuntimeHost still declares the legacy static lookup method.");
                completed.Add("static-host-lookup-method-absent");

                string packageRoot =
                    ResolvePackageRoot();
                string hostSourcePath =
                    Path.Combine(
                        packageRoot,
                        "Runtime",
                        "ApplicationLifecycle",
                        "FrameworkRuntimeHost.cs");
                string hostSource =
                    ReadRequired(hostSourcePath);

                Require(
                    !Regex.IsMatch(
                        hostSource,
                        "private\\s+static\\s+FrameworkRuntimeHost\\s+_" +
                        "current\\s*;") &&
                    !Regex.IsMatch(
                        hostSource,
                        "internal\\s+static\\s+bool\\s+TryGet" +
                        "Current\\s*\\(") &&
                    !Regex.IsMatch(
                        hostSource,
                        "if\\s*\\(\\s*_" +
                        "current\\s*==\\s*this\\s*\\)"),
                    "FrameworkRuntimeHost source still retains the legacy static field, lookup declaration or OnDestroy cleanup.");
                completed.Add("host-source-retains-no-static-authority");

                Require(
                    hostSource.Contains(
                        "internal static FrameworkRuntimeHost Create") &&
                    hostSource.Contains(
                        "DontDestroyOnLoad(runtimeObject);") &&
                    hostSource.Contains(
                        "host.Initialize(gameApplication);") &&
                    hostSource.Contains(
                        "return host;") &&
                    !hostSource.Contains(
                        "Destroy(" + "_" + "current.gameObject)") &&
                    !hostSource.Contains(
                        "_" + "current = host"),
                    "FrameworkRuntimeHost factory no longer has the expected stateless creation shape.");
                completed.Add("static-factory-retains-no-host-reference");

                int packageLookupCount =
                    CountLookupInvocations(
                        packageRoot);
                Require(
                    packageLookupCount == 0,
                    $"Package source retains '{packageLookupCount}' static host lookup invocation(s).");
                completed.Add("package-has-zero-static-host-lookups");

                string qaRoot =
                    Path.Combine(
                        Application.dataPath,
                        "ImmersiveFrameworkQA");
                int qaLookupCount =
                    CountLookupInvocations(
                        qaRoot);
                Require(
                    qaLookupCount == 0,
                    $"QAFramework source retains '{qaLookupCount}' static host lookup invocation(s).");
                completed.Add("qa-harness-has-zero-static-host-lookups");

                string resolverPath =
                    Path.Combine(
                        qaRoot,
                        "GameFlow",
                        "InternalEditor",
                        "QaH2FrameworkReadiness.cs");
                string resolverSource =
                    ReadRequired(resolverPath);
                Require(
                    resolverSource.Contains(
                        "Resources.FindObjectsOfTypeAll<") &&
                    resolverSource.Contains(
                        "FrameworkRuntimeHost>()") &&
                    resolverSource.Contains(
                        "scene.IsValid()") &&
                    resolverSource.Contains(
                        "scene.isLoaded") &&
                    resolverSource.Contains(
                        "host='ambiguous'") &&
                    resolverSource.Contains(
                        "loaded.Count != 1"),
                    "QA runtime host resolver is not explicit, loaded-scene scoped and ambiguity rejecting.");
                completed.Add("qa-resolver-is-explicit-and-ambiguity-rejecting");

                Require(
                    completed.Count == 10,
                    $"Unexpected H2.4 case count. actual='{completed.Count}'.");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='10' " +
                    $"route='{state.CurrentRouteName}' " +
                    $"activity='{state.CurrentActivityName}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static int CountLookupInvocations(
            string root)
        {
            Require(
                Directory.Exists(root),
                $"Source root is missing: '{root}'.");

            int count = 0;
            string[] sources =
                Directory.GetFiles(
                    root,
                    "*.cs",
                    SearchOption.AllDirectories);

            for (int index = 0;
                 index < sources.Length;
                 index++)
            {
                string source =
                    File.ReadAllText(
                        sources[index]);
                count +=
                    StaticLookupInvocation
                        .Matches(source)
                        .Count;
            }

            return count;
        }

        private static string ResolvePackageRoot()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(FrameworkRuntimeHost).Assembly);
            Require(
                package != null &&
                !string.IsNullOrWhiteSpace(
                    package.resolvedPath),
                "Could not resolve com.immersive.framework package path.");
            return package.resolvedPath;
        }

        private static string ReadRequired(
            string path)
        {
            Require(
                File.Exists(path),
                $"Required source file is missing: '{path}'.");
            return File.ReadAllText(path);
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static string Escape(
            string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

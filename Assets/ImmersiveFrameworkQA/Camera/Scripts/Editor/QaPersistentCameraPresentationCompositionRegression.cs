using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.Loading;
using Immersive.Framework.Pause;
using Immersive.Framework.TransitionEffects;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Contract regression for Persistent Content camera composition.
    /// It exercises the internal resolver with isolated authored roots.
    /// </summary>
    internal static class QaPersistentCameraPresentationCompositionRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run Persistent Camera Presentation Composition Regression";

        private const string RuntimeTypeName =
            "Immersive.Framework.GlobalUi.GlobalUiSceneRuntime";

        private const BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath, priority = 235)]
        private static void Run()
        {
            IReadOnlyList<string> completed = RunForCertification();

            Debug.Log(
                "[QA_PERSISTENT_CAMERA_PRESENTATION_COMPOSITION] " +
                "status='Passed' " +
                $"cases='{completed.Count}' " +
                $"evidence='{string.Join(",", completed)}'.");
        }

        internal static IReadOnlyList<string> RunForCertification()
        {
            var completed = new List<string>();

            VerifyCase(completed, "one-output-zero-session-override", 1, 0, true);
            VerifyCase(completed, "one-output-one-session-override", 1, 1, true);
            VerifyCase(completed, "one-output-two-session-overrides", 1, 2, false);
            VerifyCase(completed, "zero-outputs", 0, 0, false);
            VerifyCase(completed, "two-outputs", 2, 0, false);
            VerifyPresentationCase(completed, "zero-transition-zero-loading", 0, 0);
            VerifyPresentationCase(completed, "one-transition-zero-loading", 1, 0);
            VerifyPresentationCase(completed, "zero-transition-one-loading", 0, 1);
            VerifyPresentationCase(completed, "one-transition-one-loading", 1, 1);

            return completed;
        }

        internal static IReadOnlyList<string> RunAdr004BDuplicateOutputCertification()
        {
            var completed = new List<string>();
            VerifyCase(completed, "two-outputs", 2, 0, false);
            return completed;
        }

        private static void VerifyCase(
            ICollection<string> completed,
            string caseName,
            int outputCount,
            int sessionOverrideCount,
            bool expectedSuccess)
        {
            var roots = new List<GameObject>();

            try
            {
                for (int index = 0; index < outputCount; index++)
                {
                    var root = new GameObject($"QA_Output_{index}");
                    root.SetActive(false);
                    root.AddComponent<CameraOutputSessionBinding>();
                    roots.Add(root);
                }

                for (int index = 0; index < sessionOverrideCount; index++)
                {
                    var root = new GameObject($"QA_SessionOverride_{index}");
                    root.SetActive(false);
                    root.AddComponent<SessionCameraOverrideBinding>();
                    roots.Add(root);
                }

                object runtime = CreateRuntime(roots);
                MethodInfo resolver = runtime.GetType().GetMethod(
                    "TryResolveCameraPresentation",
                    InstanceAny);
                Require(resolver != null,
                    "Persistent camera presentation resolver is unavailable.");

                object[] arguments = { null, null, null };
                bool succeeded = (bool)resolver.Invoke(runtime, arguments);
                string diagnostic = arguments[2] as string ?? string.Empty;
                Require(succeeded == expectedSuccess,
                    $"Case '{caseName}' returned unexpected success='{succeeded}'. " +
                    $"diagnostic='{diagnostic}'.");

                if (succeeded)
                {
                    Require(arguments[0] is CameraOutputSessionBinding,
                        $"Case '{caseName}' did not resolve the mandatory output.");
                    Require(
                        sessionOverrideCount == 0
                            ? arguments[1] == null
                            : arguments[1] is SessionCameraOverrideBinding,
                        $"Case '{caseName}' resolved an unexpected Session override.");
                }
                else
                {
                    Require(
                        !string.IsNullOrWhiteSpace(diagnostic),
                        $"Case '{caseName}' blocked without an actionable composition diagnostic.");
                }

                completed.Add(caseName);
            }
            finally
            {
                for (int index = roots.Count - 1; index >= 0; index--)
                {
                    UnityEngine.Object.DestroyImmediate(roots[index]);
                }
            }
        }

        private static object CreateRuntime(
            IReadOnlyList<GameObject> roots)
        {
            return CreateRuntime(
                roots,
                Array.Empty<ITransitionEffectAdapter>(),
                Array.Empty<ILoadingSurfaceAdapter>());
        }

        private static void VerifyPresentationCase(
            ICollection<string> completed,
            string caseName,
            int transitionAdapterCount,
            int loadingAdapterCount)
        {
            object runtime = CreateRuntime(
                Array.Empty<GameObject>(),
                CreateAdapterPlaceholders<ITransitionEffectAdapter>(
                    transitionAdapterCount),
                CreateAdapterPlaceholders<ILoadingSurfaceAdapter>(
                    loadingAdapterCount));

            PropertyInfo transitionCount = runtime.GetType().GetProperty(
                "TransitionAdapterCount",
                InstanceAny);
            PropertyInfo loadingCount = runtime.GetType().GetProperty(
                "LoadingAdapterCount",
                InstanceAny);
            PropertyInfo blocking = runtime.GetType().GetProperty(
                "HasBlockingConfigurationIssue",
                InstanceAny);

            Require(
                transitionCount != null &&
                loadingCount != null &&
                blocking != null,
                "Persistent presentation composition evidence is unavailable.");
            Require(
                (int)transitionCount.GetValue(runtime) ==
                    transitionAdapterCount &&
                (int)loadingCount.GetValue(runtime) ==
                    loadingAdapterCount &&
                !(bool)blocking.GetValue(runtime),
                $"Case '{caseName}' did not preserve optional presentation composition.");

            completed.Add(caseName);
        }

        private static object CreateRuntime(
            IReadOnlyList<GameObject> roots,
            IReadOnlyList<ITransitionEffectAdapter> transitionAdapters,
            IReadOnlyList<ILoadingSurfaceAdapter> loadingAdapters)
        {
            Type runtimeType = ResolveRuntimeType();

            ConstructorInfo constructor = Array.Find(
                runtimeType.GetConstructors(InstanceAny),
                item => item.GetParameters().Length == 9);
            Require(constructor != null,
                "GlobalUiSceneRuntime composition constructor is unavailable.");

            return constructor.Invoke(new object[]
            {
                null,
                "QA Persistent Camera Presentation",
                roots,
                transitionAdapters,
                loadingAdapters,
                Array.Empty<IPauseSurfaceAdapter>(),
                false,
                string.Empty,
                string.Empty
            });
        }

        private static T[] CreateAdapterPlaceholders<T>(int count)
            where T : class
        {
            return new T[Math.Max(0, count)];
        }

        private static Type ResolveRuntimeType()
        {
            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (int index = 0; index < assemblies.Length; index++)
            {
                Type type = assemblies[index].GetType(
                    RuntimeTypeName,
                    false);

                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException(
                $"Type '{RuntimeTypeName}' is unavailable.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

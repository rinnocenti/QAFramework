using System;
using System.IO;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.InputMode.Internal.Editor.ImmersiveFrameworkQA.Player.InternalEditor.PauseP1
{
    public static class QaPauseP1LegacyBoundaryStaticSmoke
    {
        private const string MenuPath = "Immersive Framework/QA/Player/Pause P1/Run Legacy Boundary Static Smoke";

        private static bool ValidateRun() => !EditorApplication.isPlaying;

        public static void Run()
        {
            string packageRoot = ResolvePackageRoot();
            ValidateDoesNotContain(
                FindQaSource(nameof(QaPauseRequestTriggerBindingSmoke)),
                "TryBindPauseRuntime",
                "IPauseRuntimePort",
                "QaFakePauseRuntimePort",
                "PauseInputModeUnityPlayerInputRuntimeBridge");
            ValidateDoesNotContain(
                FindQaSource(nameof(QaH221PauseRequestTriggerCompositionSmoke)),
                "TryBindPauseRuntime",
                "IPauseRuntimePort",
                "QaFakePauseRuntimePort",
                "PauseInputModeUnityPlayerInputRuntimeBridge");

            ValidateDoesNotContain(
                Path.Combine(packageRoot, "Runtime", "Pause", "PauseRequestTrigger.cs"),
                "IPauseRuntimePort",
                "TryBindPauseRuntime");
            ValidateDoesNotContain(
                Path.Combine(packageRoot, "Runtime", "GlobalUi", "GlobalUiSceneRuntime.cs"),
                "TryBindPauseInputModeRuntime",
                "PauseInputModeRuntimeBridgeRegistration",
                "GlobalUiPauseRuntimeBindingResult");
            Require(!File.Exists(Path.Combine(packageRoot, "Runtime", "GlobalUi", "PauseInputModeRuntimeBridgeRegistration.cs")),
                "Superseded PauseInputModeRuntimeBridgeRegistration source remains in the package.");
            Require(!File.Exists(Path.Combine(packageRoot, "Runtime", "GlobalUi", "GlobalUiPauseRuntimeBindingResult.cs")),
                "Superseded GlobalUiPauseRuntimeBindingResult source remains in the package.");
            Require(!File.Exists(Path.Combine(packageRoot, "Runtime", "InputMode", "PauseInputModeUnityPlayerInputRuntimeBridge.cs")),
                "Retired Pause InputMode runtime bridge remains in the package.");
            Require(!File.Exists(Path.Combine(packageRoot, "Runtime", "InputMode", "PauseInputActionRuntimeBridgeTrigger.cs")),
                "Retired Pause InputAction bridge trigger remains in the package.");
            Require(File.Exists(Path.Combine(packageRoot, "Runtime", "Pause", "PausePlayerInputBinding.cs")),
                "Canonical Pause PlayerInput product binding is missing.");
            Require(File.Exists(Path.Combine(packageRoot, "Runtime", "Pause", "PauseProductBindingRuntimeContext.cs")),
                "Canonical Pause product runtime context is missing.");

            string runtimeRoot = Path.Combine(packageRoot, "Runtime");
            foreach (string path in Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                ValidateDoesNotContain(path, "TryBindPauseInputModeRuntime", "PauseInputModeRuntimeBridgeRegistration", "GlobalUiPauseRuntimeBindingResult");
            }

            Debug.Log("[QA][PAUSE-P1][LEGACY-BOUNDARY] status='Passed' checks='h221-product-only,trigger-product-only,legacy-uiglobal-removed,technical-bridge-removed,canonical-product-binding-present'.");
        }

        private static string ResolvePackageRoot()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(PauseRequestTrigger).Assembly);
            Require(package != null && !string.IsNullOrWhiteSpace(package.resolvedPath),
                "Could not resolve com.immersive.framework package path.");
            return package.resolvedPath;
        }

        private static string FindQaSource(string className)
        {
            string[] guids = AssetDatabase.FindAssets(className + " t:MonoScript");
            Require(guids.Length == 1, $"Expected exactly one QA source for '{className}', found '{guids.Length}'.");
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            Require(File.Exists(path), $"QA source '{path}' is missing.");
            return path;
        }

        private static void ValidateDoesNotContain(string path, params string[] prohibited)
        {
            Require(File.Exists(path), $"Required source is missing: '{path}'.");
            string source = File.ReadAllText(path);
            for (int index = 0; index < prohibited.Length; index++)
            {
                Require(!source.Contains(prohibited[index], StringComparison.Ordinal),
                    $"Source '{path}' retains prohibited legacy reference '{prohibited[index]}'.");
            }
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

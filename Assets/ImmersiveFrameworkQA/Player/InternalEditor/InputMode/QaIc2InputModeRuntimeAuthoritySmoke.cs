using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.InputMode;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.InputMode.Internal.Editor
{
    public static class QaIc2InputModeRuntimeAuthoritySmoke
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Input/Run Player Input Mode Authority Regression";
        private const string LogPrefix = "[IC2_INPUT_MODE_RUNTIME_AUTHORITY_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            try
            {
                Require(EditorApplication.isPlaying, "IC2 smoke requires Play Mode.");
                completed.Add("play-mode-required");

                var initial = InputModeState.InitialGameplay(nameof(QaIc2InputModeRuntimeAuthoritySmoke), "initial-gameplay");
                var context = new InputModeRuntimeContext("qa.inputmode.player.1", initial);
                InputModeRuntimeSnapshot initialSnapshot = context.CreateSnapshot();
                Require(initialSnapshot.IsInitialized && initialSnapshot.CurrentMode == InputModeKind.Gameplay && initialSnapshot.Revision == 0 && !initialSnapshot.OperationInFlight,
                    "Resident InputMode context did not preserve initial Gameplay state.");
                completed.Add("initial-state-owned");

                var pauseRequest = InputModeRequest.To(InputModeKind.PauseOverlay, nameof(QaIc2InputModeRuntimeAuthoritySmoke), "pause");
                InputModeRuntimeOperationResult pauseBegin = context.TryBegin(pauseRequest, nameof(QaIc2InputModeRuntimeAuthoritySmoke), out InputModeRuntimeTransaction pauseTransaction);
                Require(pauseBegin.Prepared && pauseTransaction.IsValid && pauseTransaction.PreviousState.CurrentKind == InputModeKind.Gameplay && pauseTransaction.NextState.CurrentKind == InputModeKind.PauseOverlay && context.OperationInFlight,
                    "Pause transaction was not prepared with exact state evidence.");
                completed.Add("pause-transaction-prepared");

                InputModeRuntimeOperationResult concurrent = context.TryBegin(
                    InputModeRequest.To(InputModeKind.FrontendMenu, "qa.concurrent", "concurrent"),
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke), out _);
                Require(concurrent.Status == InputModeRuntimeOperationStatus.RejectedOperationInFlight && context.CurrentState.CurrentKind == InputModeKind.Gameplay,
                    "Concurrent InputMode request was not rejected without state mutation.");
                completed.Add("concurrent-request-rejected");

                InputModeRuntimeOperationResult pauseCommit = context.Commit(pauseTransaction, nameof(QaIc2InputModeRuntimeAuthoritySmoke), "pause-applied");
                Require(pauseCommit.Committed && context.CurrentState.CurrentKind == InputModeKind.PauseOverlay && context.CurrentState.Revision == 1 && !context.OperationInFlight,
                    "Pause transaction did not commit the resident state.");
                completed.Add("pause-committed");

                InputModeRuntimeOperationResult duplicatePause = context.TryBegin(pauseRequest, nameof(QaIc2InputModeRuntimeAuthoritySmoke), out InputModeRuntimeTransaction duplicateTransaction);
                Require(duplicatePause.Ignored && !duplicateTransaction.IsValid && context.CurrentState.Revision == 1 && !context.OperationInFlight,
                    "Already-current Pause request was not idempotent.");
                completed.Add("duplicate-pause-idempotent");

                var gameplayRequest = InputModeRequest.To(InputModeKind.Gameplay, nameof(QaIc2InputModeRuntimeAuthoritySmoke), "resume");
                InputModeRuntimeOperationResult gameplayBegin = context.TryBegin(gameplayRequest, nameof(QaIc2InputModeRuntimeAuthoritySmoke), out InputModeRuntimeTransaction gameplayTransaction);
                Require(gameplayBegin.Prepared && gameplayTransaction.IsValid && gameplayTransaction.PreviousState.CurrentKind == InputModeKind.PauseOverlay && gameplayTransaction.NextState.CurrentKind == InputModeKind.Gameplay,
                    "Gameplay transaction was not prepared from resident Pause state.");
                completed.Add("gameplay-transaction-prepared");

                InputModeRuntimeOperationResult gameplayRollback = context.Rollback(gameplayTransaction, nameof(QaIc2InputModeRuntimeAuthoritySmoke), "physical-apply-failed");
                Require(gameplayRollback.RolledBack && context.CurrentState.CurrentKind == InputModeKind.PauseOverlay && context.CurrentState.Revision == 1 && !context.OperationInFlight,
                    "Rollback did not preserve the previous resident state.");
                completed.Add("rollback-preserves-pause");

                InputModeRuntimeOperationResult staleCommit = context.Commit(gameplayTransaction, nameof(QaIc2InputModeRuntimeAuthoritySmoke), "stale-commit");
                Require(staleCommit.Status == InputModeRuntimeOperationStatus.RejectedForeignOrStaleTransaction && context.CurrentState.CurrentKind == InputModeKind.PauseOverlay,
                    "Stale InputMode transaction was not rejected.");
                completed.Add("stale-transaction-rejected");

                InputModeRuntimeOperationResult freshGameplayBegin = context.TryBegin(gameplayRequest, nameof(QaIc2InputModeRuntimeAuthoritySmoke), out InputModeRuntimeTransaction freshGameplayTransaction);
                Require(freshGameplayBegin.Prepared && freshGameplayTransaction.Sequence > gameplayTransaction.Sequence,
                    "Fresh Gameplay transaction did not receive a monotonic sequence.");
                InputModeRuntimeOperationResult freshGameplayCommit = context.Commit(freshGameplayTransaction, nameof(QaIc2InputModeRuntimeAuthoritySmoke), "resume-applied");
                Require(freshGameplayCommit.Committed && context.CurrentState.CurrentKind == InputModeKind.Gameplay && context.CurrentState.Revision == 2,
                    "Fresh Gameplay transaction did not commit.");
                completed.Add("fresh-gameplay-committed");

                InputModeRuntimeSnapshot finalSnapshot = context.CreateSnapshot();
                Require(finalSnapshot.IsInitialized && finalSnapshot.CurrentMode == InputModeKind.Gameplay && finalSnapshot.Revision == 2 && finalSnapshot.OperationSequence == 3 && !finalSnapshot.OperationInFlight && !finalSnapshot.ActiveTransaction.IsValid,
                    "Final InputMode runtime snapshot is incoherent.");
                completed.Add("final-snapshot-clean");

                ValidateSourceOwnership(ResolvePackageRoot(), completed);
                Require(completed.Count == 13, "IC2 InputMode authority smoke case count changed unexpectedly.");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static void ValidateSourceOwnership(string packageRoot, ICollection<string> completed)
        {
            string contextPath = Path.Combine(packageRoot, "Runtime", "InputMode", "InputModeRuntimeContext.cs");
            Require(File.Exists(contextPath), $"Canonical InputMode runtime context is missing: '{contextPath}'.");
            string contextSource = File.ReadAllText(contextPath);
            Require(contextSource.Contains("sealed class InputModeRuntimeContext") && contextSource.Contains("TryBegin(") && contextSource.Contains("Commit(") && contextSource.Contains("Rollback("),
                "Canonical InputMode runtime context no longer owns its transaction protocol.");
            completed.Add("pure-runtime-owner-present");

            string[] retiredSources =
            {
                "PauseInputModeUnityPlayerInputRuntimeBridge.cs",
                "PauseInputActionRuntimeBridgeTrigger.cs",
                "InputModeUnityPlayerInputAdapter.cs",
                "InputModeUnityPlayerInputApplication.cs",
                "PauseInputModeApplyRequest.cs",
                "PauseInputModeApplyService.cs"
            };
            for (int index = 0; index < retiredSources.Length; index++)
            {
                Require(!File.Exists(Path.Combine(packageRoot, "Runtime", "InputMode", retiredSources[index])),
                    $"Retired Unity-facing InputMode source remains: '{retiredSources[index]}'.");
            }
            completed.Add("retired-unity-inputmode-layer-absent");
        }

        private static string ResolvePackageRoot()
        {
            UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(InputModeRuntimeContext).Assembly);
            Require(package != null && !string.IsNullOrWhiteSpace(package.resolvedPath), "Could not resolve com.immersive.framework package path.");
            return package.resolvedPath;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.InputMode;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    public static class QaIc2InputModeRuntimeAuthoritySmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Input Mode/IC2 Run Runtime Authority Smoke";
        private const string LogPrefix =
            "[IC2_INPUT_MODE_RUNTIME_AUTHORITY_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();

            try
            {
                Require(!EditorApplication.isPlaying,
                    "IC2 smoke requires Edit Mode.");
                completed.Add("edit-mode-required");

                var initial = InputModeState.InitialGameplay(
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    "initial-gameplay");
                var context = new InputModeRuntimeContext(
                    "qa.inputmode.player.1",
                    initial);
                InputModeRuntimeSnapshot initialSnapshot =
                    context.CreateSnapshot();
                Require(initialSnapshot.IsInitialized &&
                        initialSnapshot.CurrentMode == InputModeKind.Gameplay &&
                        initialSnapshot.Revision == 0 &&
                        !initialSnapshot.OperationInFlight,
                    "Resident InputMode context did not preserve initial Gameplay state.");
                completed.Add("initial-state-owned");

                var pauseRequest = InputModeRequest.To(
                    InputModeKind.PauseOverlay,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    "pause");
                InputModeRuntimeOperationResult pauseBegin = context.TryBegin(
                    pauseRequest,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    out InputModeRuntimeTransaction pauseTransaction);
                Require(pauseBegin.Prepared && pauseTransaction.IsValid &&
                        pauseTransaction.PreviousState.CurrentKind ==
                        InputModeKind.Gameplay &&
                        pauseTransaction.NextState.CurrentKind ==
                        InputModeKind.PauseOverlay &&
                        context.OperationInFlight,
                    "Pause transaction was not prepared with exact state evidence.");
                completed.Add("pause-transaction-prepared");

                InputModeRuntimeOperationResult concurrent = context.TryBegin(
                    InputModeRequest.To(
                        InputModeKind.FrontendMenu,
                        "qa.concurrent",
                        "concurrent"),
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    out _);
                Require(
                    concurrent.Status ==
                    InputModeRuntimeOperationStatus.RejectedOperationInFlight &&
                    context.CurrentState.CurrentKind == InputModeKind.Gameplay,
                    "Concurrent InputMode request was not rejected without state mutation.");
                completed.Add("concurrent-request-rejected");

                InputModeRuntimeOperationResult pauseCommit = context.Commit(
                    pauseTransaction,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    "pause-applied");
                Require(pauseCommit.Committed &&
                        context.CurrentState.CurrentKind ==
                        InputModeKind.PauseOverlay &&
                        context.CurrentState.Revision == 1 &&
                        !context.OperationInFlight,
                    "Pause transaction did not commit the resident state.");
                completed.Add("pause-committed");

                InputModeRuntimeOperationResult duplicatePause = context.TryBegin(
                    pauseRequest,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    out InputModeRuntimeTransaction duplicateTransaction);
                Require(duplicatePause.Ignored &&
                        !duplicateTransaction.IsValid &&
                        context.CurrentState.Revision == 1 &&
                        !context.OperationInFlight,
                    "Already-current Pause request was not idempotent.");
                completed.Add("duplicate-pause-idempotent");

                var gameplayRequest = InputModeRequest.To(
                    InputModeKind.Gameplay,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    "resume");
                InputModeRuntimeOperationResult gameplayBegin = context.TryBegin(
                    gameplayRequest,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    out InputModeRuntimeTransaction gameplayTransaction);
                Require(gameplayBegin.Prepared && gameplayTransaction.IsValid &&
                        gameplayTransaction.PreviousState.CurrentKind ==
                        InputModeKind.PauseOverlay &&
                        gameplayTransaction.NextState.CurrentKind ==
                        InputModeKind.Gameplay,
                    "Gameplay transaction was not prepared from resident Pause state.");
                completed.Add("gameplay-transaction-prepared");

                InputModeRuntimeOperationResult gameplayRollback =
                    context.Rollback(
                        gameplayTransaction,
                        nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                        "physical-apply-failed");
                Require(gameplayRollback.RolledBack &&
                        context.CurrentState.CurrentKind ==
                        InputModeKind.PauseOverlay &&
                        context.CurrentState.Revision == 1 &&
                        !context.OperationInFlight,
                    "Rollback did not preserve the previous resident state.");
                completed.Add("rollback-preserves-pause");

                InputModeRuntimeOperationResult staleCommit = context.Commit(
                    gameplayTransaction,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    "stale-commit");
                Require(
                    staleCommit.Status ==
                    InputModeRuntimeOperationStatus
                        .RejectedForeignOrStaleTransaction &&
                    context.CurrentState.CurrentKind ==
                    InputModeKind.PauseOverlay,
                    "Stale InputMode transaction was not rejected.");
                completed.Add("stale-transaction-rejected");

                InputModeRuntimeOperationResult freshGameplayBegin =
                    context.TryBegin(
                        gameplayRequest,
                        nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                        out InputModeRuntimeTransaction freshGameplayTransaction);
                Require(freshGameplayBegin.Prepared &&
                        freshGameplayTransaction.Sequence >
                        gameplayTransaction.Sequence,
                    "Fresh Gameplay transaction did not receive a monotonic sequence.");
                InputModeRuntimeOperationResult freshGameplayCommit =
                    context.Commit(
                        freshGameplayTransaction,
                        nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                        "resume-applied");
                Require(freshGameplayCommit.Committed &&
                        context.CurrentState.CurrentKind ==
                        InputModeKind.Gameplay &&
                        context.CurrentState.Revision == 2,
                    "Fresh Gameplay transaction did not commit.");
                completed.Add("fresh-gameplay-committed");

                InputModeRuntimeSnapshot finalSnapshot =
                    context.CreateSnapshot();
                Require(finalSnapshot.IsInitialized &&
                        finalSnapshot.CurrentMode == InputModeKind.Gameplay &&
                        finalSnapshot.Revision == 2 &&
                        finalSnapshot.OperationSequence == 3 &&
                        !finalSnapshot.OperationInFlight &&
                        !finalSnapshot.ActiveTransaction.IsValid,
                    "Final InputMode runtime snapshot is incoherent.");
                completed.Add("final-snapshot-clean");

                ValidateSourceOwnership(ResolvePackageRoot(), completed);

                Require(completed.Count == 18,
                    "IC2 InputMode authority smoke case count changed unexpectedly.");
                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' " +
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

        private static void ValidateSourceOwnership(
            string packageRoot,
            ICollection<string> completed)
        {
            string bridge = Read(
                packageRoot,
                "Runtime/InputMode/PauseInputModeUnityPlayerInputRuntimeBridge.cs");
            string canonicalTrigger = Read(
                packageRoot,
                "Runtime/InputMode/PauseInputActionRuntimeBridgeTrigger.cs");
            string legacyTrigger = Read(
                packageRoot,
                "Runtime/Pause/PauseInputActionTrigger.cs");
            string inputModeAdapter = Read(
                packageRoot,
                "Runtime/InputMode/InputModeUnityPlayerInputAdapter.cs");
            string writer = Read(
                packageRoot,
                "Runtime/UnityInput/UnityPlayerInputStateWriter.cs");
            string applyRequest = Read(
                packageRoot,
                "Runtime/InputMode/PauseInputModeApplyRequest.cs");
            string applyService = Read(
                packageRoot,
                "Runtime/InputMode/PauseInputModeApplyService.cs");

            Require(bridge.Contains("InputModeRuntimeContext") &&
                    bridge.Contains("InputModeRuntimeSnapshot") &&
                    bridge.Contains("HasInputModeRuntime"),
                "Canonical Pause bridge does not own resident InputMode state.");
            completed.Add("bridge-resident-owner");

            Require(bridge.Contains(".TryBegin(") &&
                    bridge.Contains(".Commit(") &&
                    bridge.Contains(".Rollback(") &&
                    bridge.Contains("No implicit reconciliation was applied"),
                "Canonical Pause bridge does not use explicit transactional arbitration.");
            completed.Add("bridge-transactional-apply");

            Require(applyRequest.Contains("CurrentInputModeState") &&
                    applyService.Contains(
                        "request.CurrentInputModeState") &&
                    !applyService.Contains(
                        "CreateInputModeStateForPauseState"),
                "Pause/InputMode apply still reconstructs a temporary logical state.");
            completed.Add("apply-consumes-resident-state");

            Require(canonicalTrigger.Contains("SubmitForDiagnostics(") &&
                    canonicalTrigger.Contains(
                        "PauseInputModeUnityPlayerInputRuntimeBridge") &&
                    !canonicalTrigger.Contains("FrameworkRuntimeHost.TryGetCurrent") &&
                    !canonicalTrigger.Contains("RequestPause("),
                "Canonical InputAction trigger bypasses the Pause/InputMode authority.");
            completed.Add("canonical-trigger-delegates");

            Require(legacyTrigger.Contains("FrameworkApiStatus.Removed") &&
                    legacyTrigger.Contains("[AddComponentMenu(\"\")]") &&
                    !legacyTrigger.Contains(".performed +=") &&
                    !legacyTrigger.Contains("FrameworkRuntimeHost.TryGetCurrent") &&
                    !legacyTrigger.Contains("RequestPause(") &&
                    !legacyTrigger.Contains("TrySelectActionMap("),
                "Legacy Pause trigger remains an active parallel submitter.");
            completed.Add("legacy-trigger-inert");

            Require(inputModeAdapter.Contains(
                        "UnityPlayerInputGateAdapter writeAuthority") &&
                    !inputModeAdapter.Contains(
                        "UnityPlayerInputStateWriter.Try") &&
                    writer.Contains(
                        "playerInput.currentActionMap = targetActionMap"),
                "IC1 physical writer boundary was not preserved by IC2/IC3.");
            completed.Add("physical-writer-boundary-preserved");

            string context = Read(
                packageRoot,
                "Runtime/InputMode/InputModeRuntimeContext.cs");
            Require(!context.Contains("UnityEngine") &&
                    !context.Contains("UnityEngine.InputSystem") &&
                    !context.Contains("FindObjectsByType") &&
                    !context.Contains("static InputModeRuntimeContext"),
                "Resident InputMode context is not a pure scoped runtime authority.");
            completed.Add("runtime-context-is-scoped");
        }

        private static string ResolvePackageRoot()
        {
            UnityEditor.PackageManager.PackageInfo package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(InputModeRuntimeContext).Assembly);
            Require(package != null &&
                    !string.IsNullOrWhiteSpace(package.resolvedPath),
                "Could not resolve com.immersive.framework package path.");
            return package.resolvedPath;
        }

        private static string Read(string root, string relativePath)
        {
            string path = Path.Combine(
                root,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            Require(File.Exists(path),
                $"Required package source is missing: '{path}'.");
            return File.ReadAllText(path);
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

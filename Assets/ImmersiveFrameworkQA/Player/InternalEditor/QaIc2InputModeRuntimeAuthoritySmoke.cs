using System;
using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.InputMode;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;
using PauseState = Immersive.Framework.Pause.PauseState;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    public static class QaIc2InputModeRuntimeAuthoritySmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Input Mode/IC2 Run Runtime Authority Smoke";
        private const string LogPrefix =
            "[IC2_INPUT_MODE_RUNTIME_AUTHORITY_SMOKE]";

        private static readonly string[] RemovedPauseScriptGuids =
        {
            "7f3e8a5c2d5c46f8a8f64d3a8f2f6d21",
            "d596d6704c2843d9b0769c1bb7e543d2"
        };

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();

            try
            {
                Require(EditorApplication.isPlaying,
                    "IC2 smoke requires Play Mode.");
                completed.Add("play-mode-required");

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

                RunPauseRuntimeBindingProofs(completed);

                ValidateSourceOwnership(ResolvePackageRoot(), completed);

                Require(completed.Count == 24,
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

        private static void RunPauseRuntimeBindingProofs(
            ICollection<string> completed)
        {
            QaIc2PauseInputModeBridgeFixture boundFixture = null;
            QaIc2PauseInputModeBridgeFixture unboundFixture = null;

            try
            {
                Require(FrameworkRuntimeHost.TryGetCurrent(
                            out FrameworkRuntimeHost runtimeHost) &&
                        runtimeHost != null,
                    "H2.1 Pause runtime binding proofs require an active global FrameworkRuntimeHost in Play Mode.");
                LocalPlayerProvisioningAuthoring provisioningAuthoring =
                    ResolveProvisioningAuthoring(runtimeHost);
                boundFixture = QaIc2PauseInputModeBridgeFixture.Create(
                    "IC2 H2.1 Bound Bridge",
                    provisioningAuthoring);
                PauseInputModeUnityPlayerInputRuntimeBridge bridge =
                    boundFixture.Bridge;
                var fakeA = new QaFakePauseRuntimePort();
                fakeA.Snapshot = PauseSnapshot.FromState(
                    PauseState.Running,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    "initial-binding",
                    Array.Empty<string>());
                fakeA.ConfiguredRequestResultFactory = request =>
                    PauseResult.AppliedResult(
                        request,
                        PauseState.Running,
                        PauseState.Paused,
                        "QA fake A applied Pause.");
                bool initialBindingSucceeded =
                    bridge.TryBindPauseRuntime(fakeA, out string initialIssue);
                PauseInputModeUnityPlayerInputRuntimeBridgeResult initialResult =
                    boundFixture.TrySubmit(
                        PauseRequestKind.Pause,
                        nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                        "initial-binding");
                Require(initialBindingSucceeded &&
                    bridge.HasPauseRuntimeBinding &&
                    bridge.PauseRuntimeBindingStatus == "Bound" &&
                    !initialResult.Failed &&
                    fakeA.SnapshotCallCount > 0 &&
                    fakeA.RequestCallCount == 1 &&
                    fakeA.LastPauseRequest.IsValid &&
                    fakeA.LastPauseRequest.Kind == PauseRequestKind.Pause,
                    DescribeBindingSubmission(
                        "initial-binding",
                        boundFixture,
                        fakeA,
                        initialBindingSucceeded,
                        initialIssue,
                        initialResult));
                completed.Add("initial-pause-runtime-binding");

                Require(bridge.TryBindPauseRuntime(fakeA, out string idempotentIssue) &&
                    bridge.HasPauseRuntimeBinding &&
                    bridge.PauseRuntimeBindingStatus == "Bound" &&
                    !bridge.PauseRuntimeBindingDiagnostic.Contains("rejected"),
                    "Idempotent Pause runtime rebinding was not accepted. " + idempotentIssue);
                completed.Add("idempotent-pause-runtime-rebinding");

                var fakeB = new QaFakePauseRuntimePort();
                bool incompatibleBindingAccepted =
                    bridge.TryBindPauseRuntime(fakeB, out string incompatibleIssue);
                Require(
                    !incompatibleBindingAccepted,
                    DescribeIncompatibleRebindingSubmission(
                        "incompatible-binding-was-accepted",
                        boundFixture,
                        fakeA,
                        fakeB,
                        default,
                        default,
                        default,
                        default,
                        null,
                        null,
                        incompatibleIssue,
                        null));
                Require(
                    incompatibleIssue.Contains("different port") &&
                    incompatibleIssue.Contains("current lifetime"),
                    DescribeIncompatibleRebindingSubmission(
                        "incompatible-binding-diagnostic",
                        boundFixture,
                        fakeA,
                        fakeB,
                        default,
                        default,
                        default,
                        default,
                        null,
                        null,
                        incompatibleIssue,
                        null));
                Require(
                    bridge.HasPauseRuntimeBinding &&
                    bridge.PauseRuntimeBindingStatus == "Bound",
                    DescribeIncompatibleRebindingSubmission(
                        "incompatible-binding-preserves-bound-status",
                        boundFixture,
                        fakeA,
                        fakeB,
                        default,
                        default,
                        default,
                        default,
                        null,
                        null,
                        incompatibleIssue,
                        null));

                PauseSnapshot fakeASnapshotBeforeResume = fakeA.Snapshot;
                InputModeRuntimeSnapshot inputModeBeforeResume =
                    bridge.InputModeRuntimeSnapshot;
                int fakeASnapshotCallsBeforeResume = fakeA.SnapshotCallCount;
                int fakeARequestCallsBeforeResume = fakeA.RequestCallCount;
                int fakeBSnapshotCallsBeforeResume = fakeB.SnapshotCallCount;
                int fakeBRequestCallsBeforeResume = fakeB.RequestCallCount;
                Require(
                    fakeBSnapshotCallsBeforeResume == 0 &&
                    fakeBRequestCallsBeforeResume == 0,
                    DescribeIncompatibleRebindingSubmission(
                        "fake-b-no-calls-before-resume",
                        boundFixture,
                        fakeA,
                        fakeB,
                        fakeASnapshotCallsBeforeResume,
                        fakeARequestCallsBeforeResume,
                        fakeBSnapshotCallsBeforeResume,
                        fakeBRequestCallsBeforeResume,
                        inputModeBeforeResume,
                        fakeASnapshotBeforeResume,
                        incompatibleIssue,
                        null));
                Require(
                    fakeASnapshotBeforeResume.State == PauseState.Paused &&
                    inputModeBeforeResume is
                    { CurrentMode: InputModeKind.PauseOverlay, OperationInFlight: false },
                    DescribeIncompatibleRebindingSubmission(
                        "resume-baseline",
                        boundFixture,
                        fakeA,
                        fakeB,
                        fakeASnapshotCallsBeforeResume,
                        fakeARequestCallsBeforeResume,
                        fakeBSnapshotCallsBeforeResume,
                        fakeBRequestCallsBeforeResume,
                        inputModeBeforeResume,
                        fakeASnapshotBeforeResume,
                        incompatibleIssue,
                        null));

                PauseSnapshot configuredResumeSnapshot = PauseSnapshot.FromState(
                    PauseState.Paused,
                    nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                    "incompatible-rebinding",
                    Array.Empty<string>());
                PauseResult configuredResumeResult = PauseResult.AppliedResult(
                    PauseRequest.Resume(
                        "qa.ic2.incompatible-rebinding.resume",
                        nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                        "configured-resume"),
                    PauseState.Paused,
                    PauseState.Running,
                    "QA fake A configured Resume.");
                fakeA.Snapshot = configuredResumeSnapshot;
                fakeA.ConfiguredRequestResultFactory = request =>
                    PauseResult.AppliedResult(
                        request,
                        PauseState.Paused,
                        PauseState.Running,
                        "QA fake A applied Resume.");
                PauseInputModeUnityPlayerInputRuntimeBridgeResult resumedResult =
                    boundFixture.TrySubmit(
                        PauseRequestKind.Resume,
                        nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                        "incompatible-rebinding");
                InputModeRuntimeSnapshot inputModeAfterResume =
                    bridge.InputModeRuntimeSnapshot;
                Require(
                    !resumedResult.Failed && resumedResult.PauseRequestSubmitted,
                    DescribeIncompatibleRebindingSubmission(
                        "resume-submit-result",
                        boundFixture,
                        fakeA,
                        fakeB,
                        fakeASnapshotCallsBeforeResume,
                        fakeARequestCallsBeforeResume,
                        fakeBSnapshotCallsBeforeResume,
                        fakeBRequestCallsBeforeResume,
                        inputModeBeforeResume,
                        configuredResumeSnapshot,
                        incompatibleIssue,
                        configuredResumeResult,
                        resumedResult));
                Require(
                    fakeA.SnapshotCallCount > fakeASnapshotCallsBeforeResume,
                    DescribeIncompatibleRebindingSubmission(
                        "fake-a-snapshot-count",
                        boundFixture,
                        fakeA,
                        fakeB,
                        fakeASnapshotCallsBeforeResume,
                        fakeARequestCallsBeforeResume,
                        fakeBSnapshotCallsBeforeResume,
                        fakeBRequestCallsBeforeResume,
                        inputModeBeforeResume,
                        configuredResumeSnapshot,
                        incompatibleIssue,
                        configuredResumeResult,
                        resumedResult));
                Require(
                    fakeA.RequestCallCount == fakeARequestCallsBeforeResume + 1,
                    DescribeIncompatibleRebindingSubmission(
                        "fake-a-request-count",
                        boundFixture,
                        fakeA,
                        fakeB,
                        fakeASnapshotCallsBeforeResume,
                        fakeARequestCallsBeforeResume,
                        fakeBSnapshotCallsBeforeResume,
                        fakeBRequestCallsBeforeResume,
                        inputModeBeforeResume,
                        configuredResumeSnapshot,
                        incompatibleIssue,
                        configuredResumeResult,
                        resumedResult));
                Require(
                    fakeA.LastPauseRequest.IsValid &&
                    fakeA.LastPauseRequest.Kind == PauseRequestKind.Resume,
                    DescribeIncompatibleRebindingSubmission(
                        "fake-a-resume-request",
                        boundFixture,
                        fakeA,
                        fakeB,
                        fakeASnapshotCallsBeforeResume,
                        fakeARequestCallsBeforeResume,
                        fakeBSnapshotCallsBeforeResume,
                        fakeBRequestCallsBeforeResume,
                        inputModeBeforeResume,
                        configuredResumeSnapshot,
                        incompatibleIssue,
                        configuredResumeResult,
                        resumedResult));
                Require(
                    fakeB.SnapshotCallCount == fakeBSnapshotCallsBeforeResume &&
                    fakeB.RequestCallCount == fakeBRequestCallsBeforeResume,
                    DescribeIncompatibleRebindingSubmission(
                        "fake-b-counts-preserved",
                        boundFixture,
                        fakeA,
                        fakeB,
                        fakeASnapshotCallsBeforeResume,
                        fakeARequestCallsBeforeResume,
                        fakeBSnapshotCallsBeforeResume,
                        fakeBRequestCallsBeforeResume,
                        inputModeBeforeResume,
                        configuredResumeSnapshot,
                        incompatibleIssue,
                        configuredResumeResult,
                        resumedResult));
                Require(
                    inputModeAfterResume is
                    { CurrentMode: InputModeKind.Gameplay, OperationInFlight: false },
                    DescribeIncompatibleRebindingSubmission(
                        "inputmode-restored-gameplay",
                        boundFixture,
                        fakeA,
                        fakeB,
                        fakeASnapshotCallsBeforeResume,
                        fakeARequestCallsBeforeResume,
                        fakeBSnapshotCallsBeforeResume,
                        fakeBRequestCallsBeforeResume,
                        inputModeBeforeResume,
                        configuredResumeSnapshot,
                        incompatibleIssue,
                        configuredResumeResult,
                        resumedResult));
                completed.Add("incompatible-pause-runtime-rebinding-rejected");

                unboundFixture = QaIc2PauseInputModeBridgeFixture.Create(
                    "IC2 H2.1 Unbound Bridge",
                    provisioningAuthoring);
                PauseInputModeUnityPlayerInputRuntimeBridge unboundBridge =
                    unboundFixture.Bridge;
                InputModeRuntimeSnapshot beforeUnboundSnapshot =
                    unboundBridge.InputModeRuntimeSnapshot;
                InputModeRuntimeOperationResult beforeUnboundOperation =
                    unboundBridge.LastInputModeRuntimeOperation;
                PauseInputModeUnityPlayerInputRuntimeBridgeResult unboundResult =
                    unboundFixture.TrySubmit(
                        PauseRequestKind.Pause,
                        nameof(QaIc2InputModeRuntimeAuthoritySmoke),
                        "unbound-does-not-fallback");
                InputModeRuntimeSnapshot afterUnboundSnapshot =
                    unboundBridge.InputModeRuntimeSnapshot;
                Require(unboundResult.Failed &&
                    unboundResult.Status ==
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus.FailedPreflight &&
                    !unboundResult.PauseRequestSubmitted &&
                    unboundResult.Message.Contains("Pause runtime port is not bound") &&
                    !unboundBridge.HasPauseRuntimeBinding &&
                    !unboundBridge.HasInputModeRuntime &&
                    ReferenceEquals(beforeUnboundSnapshot, afterUnboundSnapshot) &&
                    ReferenceEquals(
                        beforeUnboundOperation,
                        unboundBridge.LastInputModeRuntimeOperation),
                    DescribeUnboundSubmission(
                        unboundFixture,
                        beforeUnboundSnapshot,
                        beforeUnboundOperation,
                        unboundResult));
                completed.Add("unbound-bridge-does-not-fallback-to-current-host");
            }
            finally
            {
                unboundFixture?.Dispose();
                boundFixture?.Dispose();
            }
        }

        private static string DescribeIncompatibleRebindingSubmission(
            string step,
            QaIc2PauseInputModeBridgeFixture fixture,
            QaFakePauseRuntimePort fakeA,
            QaFakePauseRuntimePort fakeB,
            int fakeASnapshotCallsBefore,
            int fakeARequestCallsBefore,
            int fakeBSnapshotCallsBefore,
            int fakeBRequestCallsBefore,
            InputModeRuntimeSnapshot inputModeBefore,
            PauseSnapshot? configuredPauseSnapshot,
            string incompatibleIssue,
            PauseResult? configuredPauseResult,
            PauseInputModeUnityPlayerInputRuntimeBridgeResult submitResult = null)
        {
            InputModeRuntimeSnapshot inputModeAfter =
                fixture.Bridge.InputModeRuntimeSnapshot;
            return
                $"Incompatible Pause runtime rebinding proof failed at '{step}'. " +
                $"submitStatus='{submitResult?.Status}' " +
                $"submitMessage='{Escape(submitResult?.Message)}' " +
                $"pauseRequestSubmitted='{submitResult?.PauseRequestSubmitted}' " +
                $"fakeASnapshotBefore='{fakeASnapshotCallsBefore}' " +
                $"fakeASnapshotAfter='{fakeA.SnapshotCallCount}' " +
                $"fakeARequestBefore='{fakeARequestCallsBefore}' " +
                $"fakeARequestAfter='{fakeA.RequestCallCount}' " +
                $"fakeBSnapshotBefore='{fakeBSnapshotCallsBefore}' " +
                $"fakeBSnapshotAfter='{fakeB.SnapshotCallCount}' " +
                $"fakeBRequestBefore='{fakeBRequestCallsBefore}' " +
                $"fakeBRequestAfter='{fakeB.RequestCallCount}' " +
                $"fakeALastRequest='{DescribePauseRequest(fakeA.LastPauseRequest)}' " +
                $"inputModeBefore='{inputModeBefore?.CurrentMode}' " +
                $"inputModeAfter='{inputModeAfter?.CurrentMode}' " +
                $"revisionBefore='{inputModeBefore?.Revision}' " +
                $"revisionAfter='{inputModeAfter?.Revision}' " +
                $"fakeAConfiguredSnapshot='{configuredPauseSnapshot?.ToDiagnosticString()}' " +
                $"fakeAConfiguredResult='{configuredPauseResult?.ToDiagnosticString()}' " +
                $"incompatibleIssue='{Escape(incompatibleIssue)}'.";
        }

        private static LocalPlayerProvisioningAuthoring
            ResolveProvisioningAuthoring(FrameworkRuntimeHost runtimeHost)
        {
            LocalPlayerProvisioningAuthoring authoring = null;
            bool configured = false;
            string diagnostic = string.Empty;
            bool resolved = runtimeHost != null &&
                runtimeHost.TryResolveLocalPlayerProvisioningAuthoring(
                    out authoring,
                    out configured,
                    out diagnostic);
            Require(
                resolved && configured && authoring != null,
                "H2.1 binding fixture requires explicit Local Player provisioning " +
                "from the active UIGlobal composition. " +
                $"resolved='{resolved}' configured='{configured}' " +
                $"diagnostic='{Escape(diagnostic)}'.");
            return authoring;
        }

        private static string DescribeBindingSubmission(
            string step,
            QaIc2PauseInputModeBridgeFixture fixture,
            QaFakePauseRuntimePort fake,
            bool bindingSucceeded,
            string bindingIssue,
            PauseInputModeUnityPlayerInputRuntimeBridgeResult result)
        {
            PauseInputModeUnityPlayerInputRuntimeBridge bridge = fixture.Bridge;
            InputModeRuntimeSnapshot inputMode = bridge.InputModeRuntimeSnapshot;
            InputModeRuntimeOperationResult operation =
                bridge.LastInputModeRuntimeOperation;
            return
                $"H2.1 binding proof failed at '{step}'. " +
                $"bindingSucceeded='{bindingSucceeded}' bindingIssue='{Escape(bindingIssue)}' " +
                $"hasBinding='{bridge.HasPauseRuntimeBinding}' " +
                $"bindingStatus='{bridge.PauseRuntimeBindingStatus}' " +
                $"bindingDiagnostic='{Escape(bridge.PauseRuntimeBindingDiagnostic)}' " +
                $"submitStatus='{result?.Status}' submitMessage='{Escape(result?.Message)}' " +
                $"pauseRequestSubmitted='{result?.PauseRequestSubmitted}' " +
                $"fakeSnapshotCalls='{fake.SnapshotCallCount}' " +
                $"fakeRequestCalls='{fake.RequestCallCount}' " +
                $"fakeLastRequest='{DescribePauseRequest(fake.LastPauseRequest)}' " +
                $"inputMode='{inputMode?.CurrentMode}' revision='{inputMode?.Revision}' " +
                $"operationInFlight='{inputMode?.OperationInFlight}' " +
                $"lastOperation='{operation?.Status}' " +
                $"lastOperationMessage='{Escape(operation?.Message)}' " +
                $"playerInput='{EntityId(fixture.PlayerInput)}' " +
                $"actionAsset='{EntityId(fixture.ActionAsset)}' " +
                $"hasGlobal='{HasActionMap(fixture.ActionAsset, "Global")}' " +
                $"hasPlayer='{HasActionMap(fixture.ActionAsset, "Player")}' " +
                $"hasUi='{HasActionMap(fixture.ActionAsset, "UI")}' " +
                $"playerActorEvidence='{fixture.PlayerActor.HasPlayerInputEvidence}'.";
        }

        private static string DescribeUnboundSubmission(
            QaIc2PauseInputModeBridgeFixture fixture,
            InputModeRuntimeSnapshot beforeSnapshot,
            InputModeRuntimeOperationResult beforeOperation,
            PauseInputModeUnityPlayerInputRuntimeBridgeResult result)
        {
            PauseInputModeUnityPlayerInputRuntimeBridge bridge = fixture.Bridge;
            InputModeRuntimeSnapshot afterSnapshot = bridge.InputModeRuntimeSnapshot;
            InputModeRuntimeOperationResult afterOperation =
                bridge.LastInputModeRuntimeOperation;
            return
                "Unbound bridge did not fail specifically at Pause runtime binding. " +
                $"status='{result.Status}' message='{Escape(result.Message)}' " +
                $"pauseRequestSubmitted='{result.PauseRequestSubmitted}' " +
                $"bindingStatus='{bridge.PauseRuntimeBindingStatus}' " +
                $"bindingDiagnostic='{Escape(bridge.PauseRuntimeBindingDiagnostic)}' " +
                $"revisionBefore='{beforeSnapshot?.Revision}' " +
                $"revisionAfter='{afterSnapshot?.Revision}' " +
                $"operationInFlightAfter='{afterSnapshot?.OperationInFlight}' " +
                $"operationPreserved='{ReferenceEquals(beforeOperation, afterOperation)}' " +
                $"lastOperationBefore='{beforeOperation?.Status}' " +
                $"lastOperationAfter='{afterOperation?.Status}' " +
                $"hasPlayerInput='{fixture.PlayerInput != null}' " +
                $"hasGlobal='{HasActionMap(fixture.ActionAsset, "Global")}' " +
                $"hasPlayer='{HasActionMap(fixture.ActionAsset, "Player")}' " +
                $"hasUi='{HasActionMap(fixture.ActionAsset, "UI")}' " +
                $"playerActorEvidence='{fixture.PlayerActor.HasPlayerInputEvidence}'.";
        }

        private static string DescribePauseRequest(PauseRequest request) =>
            request.IsValid ? request.ToDiagnosticString() : "<none>";

        private static string EntityId(UnityEngine.Object value) =>
            value == null ? string.Empty : value.GetEntityId().ToString();

        private static bool HasActionMap(
            InputActionAsset actionAsset,
            string name) => actionAsset?.FindActionMap(name, false) != null;

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
            string inputModeApplication = Read(
                packageRoot,
                "Runtime/InputMode/InputModeUnityPlayerInputApplication.cs");

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

            Require(!ContainsRemovedPauseScriptGuidInSerializedAssets() &&
                    !ContainsRemovedPauseSurface(packageRoot),
                "Removed Pause scripts or their menu surface remain available.");
            completed.Add("legacy-pause-surface-absent");

            Require(inputModeAdapter.Contains(
                        "UnityPlayerInputGateAdapter writeAuthority") &&
                    !inputModeAdapter.Contains(
                        "UnityPlayerInputStateWriter.Try") &&
                    writer.Contains(
                        "TryApplyActionMapSet") &&
                    writer.Contains(
                        "playerInput.currentActionMap = primaryMap"),
                "IC1 physical writer boundary was not preserved by IC2/IC3.");
            completed.Add("physical-writer-boundary-preserved");

            Require(bridge.Contains("globalActionMapName = \"Global\"") &&
                    bridge.Contains("CreatePersistentActionMapNames") &&
                    applyRequest.Contains("PersistentActionMapNames") &&
                    applyService.Contains("TryValidatePersistentActionMaps"),
                "Canonical Pause/InputMode path does not require the persistent Global map.");
            completed.Add("global-map-is-persistent-policy");

            Require(!inputModeApplication.Contains(".ActivateInput(") &&
                    inputModeAdapter.Contains("TryApplyActionMapSet") &&
                    writer.Contains("TryRestoreActionMapSet"),
                "Layered InputMode application still performs implicit activation or lacks exact rollback.");
            completed.Add("layered-map-set-application");

            string context = Read(
                packageRoot,
                "Runtime/InputMode/InputModeRuntimeContext.cs");
            Require(!context.Contains("UnityEngine") &&
                    !context.Contains("UnityEngine.InputSystem") &&
                    !context.Contains("FindObjects" + "ByType") &&
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

        private static bool ContainsRemovedPauseScriptGuidInSerializedAssets()
        {
            foreach (string assetPath in AssetDatabase.GetAllAssetPaths())
            {
                if (!IsSerializedAsset(assetPath) || !File.Exists(assetPath))
                {
                    continue;
                }

                string serializedText = File.ReadAllText(assetPath);
                for (int index = 0; index < RemovedPauseScriptGuids.Length; index++)
                {
                    if (serializedText.Contains(
                            "guid: " + RemovedPauseScriptGuids[index]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsRemovedPauseSurface(string packageRoot)
        {
            string runtimeRoot = Path.Combine(packageRoot, "Runtime");
            foreach (string sourcePath in Directory.GetFiles(
                         runtimeRoot,
                         "*.cs",
                         SearchOption.AllDirectories))
            {
                string source = File.ReadAllText(sourcePath);
                if (source.Contains("FrameworkApiStatus.Removed") ||
                    source.Contains("Immersive Framework/Removed/"))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSerializedAsset(string path)
        {
            return path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                   path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase);
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

using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.InputMode;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using PauseState = Immersive.Framework.Pause.PauseState;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    public static class QaIc2PauseInputModeRuntimeRegressionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Input Mode/IC2 Run Pause Runtime Regression Smoke";
        private const string LogPrefix =
            "[IC2_PAUSE_INPUT_MODE_RUNTIME_REGRESSION_SMOKE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            RuntimeFixture fixture = null;

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "IC2 runtime regression requires Play Mode.");
                completed.Add("play-mode-required");

                fixture = RuntimeFixture.Create(
                    "QA IC2 Pause InputMode Runtime Regression");
                Require(
                    fixture.TryGetPauseState(out _),
                    "Framework Pause runtime is unavailable.");
                fixture.EnsurePauseState(PauseState.Running);
                Require(
                    fixture.TryGetPauseState(out PauseState runningState) &&
                    runningState == PauseState.Running,
                    "Could not establish the Running Pause baseline.");
                completed.Add("runtime-host-available");

                Require(
                    fixture.IsValid,
                    "Synthetic PlayerInput-scoped authority fixture is invalid.");
                completed.Add("scoped-authority-fixture-created");

                PauseInputModeUnityPlayerInputRuntimeBridgeResult pauseResult =
                    fixture.SubmitToggle();
                InputModeRuntimeSnapshot pauseSnapshot =
                    fixture.RequireRuntimeSnapshot();
                Require(
                    pauseResult.Succeeded &&
                    pauseResult.PauseRequestSubmitted &&
                    pauseResult.PauseStatus == PauseRequestStatus.Applied &&
                    pauseResult.CurrentPauseState == PauseState.Paused &&
                    pauseResult.RequestedMode == InputModeKind.PauseOverlay &&
                    pauseResult.AppliedActionMapName.ToString() == "UI" &&
                    pauseResult.Applied &&
                    pauseResult.SelectedActionMap &&
                    fixture.CurrentActionMapName == "UI" &&
                    fixture.HasExactEnabledMaps("Global", "UI") &&
                    fixture.HasAction("Global", "PauseToggle") &&
                    pauseSnapshot.CurrentMode == InputModeKind.PauseOverlay &&
                    pauseSnapshot.Revision == 1 &&
                    !pauseSnapshot.OperationInFlight &&
                    fixture.Bridge.LastInputModeRuntimeOperation is
                    { Committed: true },
                    "Canonical Pause did not commit PauseOverlay/UI through the resident authority.");
                completed.Add("toggle-pause-committed-global-ui");

                int idempotentRevision = pauseSnapshot.Revision;
                PauseInputModeUnityPlayerInputRuntimeBridgeResult duplicatePause =
                    fixture.SubmitPause();
                InputModeRuntimeSnapshot duplicateSnapshot =
                    fixture.RequireRuntimeSnapshot();
                Require(
                    duplicatePause.Ignored &&
                    duplicatePause.PauseRequestSubmitted &&
                    duplicatePause.PauseStatus ==
                    PauseRequestStatus.IgnoredNoChange &&
                    duplicatePause.CurrentPauseState == PauseState.Paused &&
                    !duplicatePause.Applied &&
                    !duplicatePause.SelectedActionMap &&
                    fixture.HasExactEnabledMaps("Global", "UI") &&
                    duplicateSnapshot.CurrentMode ==
                    InputModeKind.PauseOverlay &&
                    duplicateSnapshot.Revision == idempotentRevision &&
                    !duplicateSnapshot.OperationInFlight &&
                    fixture.Bridge.LastInputModeRuntimeOperation is
                    { Ignored: true },
                    "Repeated Pause was not idempotent in the resident authority.");
                completed.Add("duplicate-pause-idempotent");

                PauseInputModeUnityPlayerInputRuntimeBridgeResult resumeResult =
                    fixture.SubmitToggle();
                InputModeRuntimeSnapshot resumeSnapshot =
                    fixture.RequireRuntimeSnapshot();
                Require(
                    resumeResult.Succeeded &&
                    resumeResult.PauseRequestSubmitted &&
                    resumeResult.PauseStatus == PauseRequestStatus.Applied &&
                    resumeResult.CurrentPauseState == PauseState.Running &&
                    resumeResult.RequestedMode == InputModeKind.Gameplay &&
                    resumeResult.AppliedActionMapName.ToString() == "Player" &&
                    resumeResult.Applied &&
                    resumeResult.SelectedActionMap &&
                    fixture.CurrentActionMapName == "Player" &&
                    fixture.HasExactEnabledMaps("Global", "Player") &&
                    fixture.HasAction("Global", "PauseToggle") &&
                    resumeSnapshot.CurrentMode == InputModeKind.Gameplay &&
                    resumeSnapshot.Revision == idempotentRevision + 1 &&
                    !resumeSnapshot.OperationInFlight &&
                    fixture.Bridge.LastInputModeRuntimeOperation is
                    { Committed: true },
                    "Canonical Resume did not commit Gameplay/Player through the resident authority.");
                completed.Add("toggle-resume-committed-global-player");
                completed.Add("global-pause-toggle-remains-enabled");

                int beforeMissingGlobalRevision = resumeSnapshot.Revision;
                Require(
                    fixture.TryGetPauseState(
                        out PauseState beforeMissingGlobalPause) &&
                    beforeMissingGlobalPause == PauseState.Running,
                    "Missing Global map preflight requires a Running Pause baseline.");
                InputModeRuntimeSnapshot beforeMissingGlobalSnapshot =
                    fixture.RequireRuntimeSnapshot();
                InputModeRuntimeOperationResult beforeMissingGlobalOperation =
                    fixture.Bridge.LastInputModeRuntimeOperation;
                fixture.ReplaceActionAsset(
                    includeGlobalMap: false,
                    includePlayerMap: true,
                    includeUiMap: true);
                PauseInputModeUnityPlayerInputRuntimeBridgeResult missingGlobal =
                    fixture.SubmitPause();
                InputModeRuntimeSnapshot missingGlobalSnapshot =
                    fixture.RequireRuntimeSnapshot();
                PauseState afterMissingGlobal = PauseState.Unknown;
                bool missingGlobalPausePreserved =
                    fixture.TryGetPauseState(out afterMissingGlobal) &&
                    afterMissingGlobal == PauseState.Running;
                Require(
                    missingGlobal.Failed &&
                    missingGlobal.Status ==
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedPreflight &&
                    !missingGlobal.PauseRequestSubmitted &&
                    missingGlobalPausePreserved &&
                    missingGlobalSnapshot.CurrentMode == InputModeKind.Gameplay &&
                    missingGlobalSnapshot.Revision == beforeMissingGlobalRevision &&
                    !missingGlobalSnapshot.OperationInFlight &&
                    fixture.Bridge.LastInputModeRuntimeOperation is
                    not null &&
                    ReferenceEquals(
                        fixture.Bridge.LastInputModeRuntimeOperation,
                        beforeMissingGlobalOperation) &&
                    fixture.HasBridgeFixtureInputAndActionIdentity,
                    "Missing Global map did not fail before Pause mutation and preserve resident Gameplay state. " +
                    fixture.DescribeMissingGlobalPreflight(
                        beforeMissingGlobalPause,
                        beforeMissingGlobalSnapshot,
                        beforeMissingGlobalOperation,
                        missingGlobal,
                        afterMissingGlobal,
                        missingGlobalSnapshot));
                completed.Add("missing-global-map-preflight-preserves-state");

                fixture.ReplaceActionAsset(
                    includeGlobalMap: true,
                    includePlayerMap: true,
                    includeUiMap: true);

                int beforeMissingUiRevision = resumeSnapshot.Revision;
                Require(
                    fixture.TryGetPauseState(out PauseState beforeMissingUiPause) &&
                    beforeMissingUiPause == PauseState.Running,
                    "Missing UI map preflight requires a Running Pause baseline.");
                InputModeRuntimeSnapshot beforeMissingUiSnapshot =
                    fixture.RequireRuntimeSnapshot();
                InputModeRuntimeOperationResult beforeMissingUiOperation =
                    fixture.Bridge.LastInputModeRuntimeOperation;
                fixture.ReplaceActionAsset(
                    includeGlobalMap: true,
                    includePlayerMap: true,
                    includeUiMap: false);
                PauseInputModeUnityPlayerInputRuntimeBridgeResult missingUi =
                    fixture.SubmitPause();
                InputModeRuntimeSnapshot missingUiSnapshot =
                    fixture.RequireRuntimeSnapshot();
                PauseState afterMissingUi = PauseState.Unknown;
                bool missingUiPausePreserved =
                    fixture.TryGetPauseState(out afterMissingUi) &&
                    afterMissingUi == PauseState.Running;
                Require(
                    missingUi.Failed &&
                    missingUi.Status ==
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedPreflight &&
                    !missingUi.PauseRequestSubmitted &&
                    missingUi.RequestedMode == InputModeKind.PauseOverlay &&
                    missingUiPausePreserved &&
                    missingUiSnapshot.CurrentMode == InputModeKind.Gameplay &&
                    missingUiSnapshot.Revision == beforeMissingUiRevision &&
                    !missingUiSnapshot.OperationInFlight &&
                    beforeMissingUiOperation is { Committed: true } &&
                    ReferenceEquals(
                        fixture.Bridge.LastInputModeRuntimeOperation,
                        beforeMissingUiOperation),
                    "Missing UI map did not fail before Pause mutation and preserve resident Gameplay state. " +
                    "No InputMode transaction can begin when preflight rejects the required UI map. " +
                    fixture.DescribePreflightFailure(
                        beforeMissingUiPause,
                        beforeMissingUiSnapshot,
                        beforeMissingUiOperation,
                        missingUi,
                        afterMissingUi,
                        missingUiSnapshot));
                completed.Add("missing-ui-map-preflight-preserves-state");

                fixture.ReplaceActionAsset(
                    includeGlobalMap: true,
                    includePlayerMap: true,
                    includeUiMap: true);
                PauseInputModeUnityPlayerInputRuntimeBridgeResult recoveredPause =
                    fixture.SubmitPause();
                InputModeRuntimeSnapshot recoveredPauseSnapshot =
                    fixture.RequireRuntimeSnapshot();
                Require(
                    recoveredPause.Succeeded &&
                    recoveredPause.CurrentPauseState == PauseState.Paused &&
                    recoveredPause.RequestedMode == InputModeKind.PauseOverlay &&
                    fixture.CurrentActionMapName == "UI" &&
                    fixture.HasExactEnabledMaps("Global", "UI") &&
                    recoveredPauseSnapshot.CurrentMode ==
                    InputModeKind.PauseOverlay &&
                    recoveredPauseSnapshot.Revision ==
                    beforeMissingUiRevision + 1,
                    "The authority did not recover after restoring the UI action map.");
                completed.Add("restored-ui-map-pause-committed");

                int beforeMissingActorRevision =
                    recoveredPauseSnapshot.Revision;
                Require(
                    fixture.TryGetPauseState(
                        out PauseState beforeMissingActorPause) &&
                    beforeMissingActorPause == PauseState.Paused,
                    "Missing PlayerActor preflight requires a Paused Pause baseline.");
                InputModeRuntimeSnapshot beforeMissingActorSnapshot =
                    fixture.RequireRuntimeSnapshot();
                InputModeRuntimeOperationResult beforeMissingActorOperation =
                    fixture.Bridge.LastInputModeRuntimeOperation;
                fixture.SetPlayerActorEvidence(enabled: false);
                PauseInputModeUnityPlayerInputRuntimeBridgeResult missingActor =
                    fixture.SubmitResume();
                InputModeRuntimeSnapshot missingActorSnapshot =
                    fixture.RequireRuntimeSnapshot();
                PauseState afterMissingActor = PauseState.Unknown;
                bool missingActorPausePreserved =
                    fixture.TryGetPauseState(out afterMissingActor) &&
                    afterMissingActor == PauseState.Paused;
                Require(
                    missingActor.Failed &&
                    missingActor.Status ==
                    PauseInputModeUnityPlayerInputRuntimeBridgeStatus
                        .FailedPreflight &&
                    !missingActor.PauseRequestSubmitted &&
                    missingActor.RequestedMode == InputModeKind.Gameplay &&
                    missingActorPausePreserved &&
                    missingActorSnapshot.CurrentMode ==
                    InputModeKind.PauseOverlay &&
                    missingActorSnapshot.Revision ==
                    beforeMissingActorRevision &&
                    !missingActorSnapshot.OperationInFlight &&
                    beforeMissingActorOperation is { Committed: true } &&
                    ReferenceEquals(
                        fixture.Bridge.LastInputModeRuntimeOperation,
                        beforeMissingActorOperation),
                    "Missing PlayerActor evidence did not fail before Resume mutation and preserve resident PauseOverlay state. " +
                    "No InputMode transaction can begin when preflight rejects PlayerActor evidence. " +
                    fixture.DescribePreflightFailure(
                        beforeMissingActorPause,
                        beforeMissingActorSnapshot,
                        beforeMissingActorOperation,
                        missingActor,
                        afterMissingActor,
                        missingActorSnapshot));
                completed.Add("missing-playeractor-preflight-preserves-state");

                fixture.SetPlayerActorEvidence(enabled: true);
                PauseInputModeUnityPlayerInputRuntimeBridgeResult finalResume =
                    fixture.SubmitResume();
                InputModeRuntimeSnapshot finalSnapshot =
                    fixture.RequireRuntimeSnapshot();
                Require(
                    finalResume.Succeeded &&
                    finalResume.CurrentPauseState == PauseState.Running &&
                    finalResume.RequestedMode == InputModeKind.Gameplay &&
                    fixture.CurrentActionMapName == "Player" &&
                    fixture.HasExactEnabledMaps("Global", "Player") &&
                    finalSnapshot.CurrentMode == InputModeKind.Gameplay &&
                    finalSnapshot.Revision == beforeMissingActorRevision + 1 &&
                    !finalSnapshot.OperationInFlight &&
                    !finalResume.CallsPlayerJoin &&
                    !finalResume.SpawnsActor &&
                    !finalResume.UsesCustomInputManager,
                    "Final Resume did not restore a clean Gameplay authority without join/spawn ownership.");
                completed.Add("final-resume-clean-no-join-spawn");

                fixture.EnsurePauseState(PauseState.Running);
                Require(
                    fixture.TryGetPauseState(out PauseState finalPauseState) &&
                    finalPauseState == PauseState.Running &&
                    fixture.RequireRuntimeSnapshot().CurrentMode ==
                    InputModeKind.Gameplay,
                    "Runtime regression did not leave Pause and InputMode in a clean Running/Gameplay state.");
                completed.Add("cleanup-restores-running-gameplay");

                Require(
                    completed.Count == 13,
                    "IC2 runtime regression case count changed unexpectedly.");
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
            finally
            {
                if (fixture != null)
                {
                    try
                    {
                        fixture.EnsurePauseState(PauseState.Running);
                    }
                    catch (Exception cleanupException)
                    {
                        Debug.LogWarning(
                            $"{LogPrefix} cleanupWarning='" +
                            $"{Escape(cleanupException.Message)}'.");
                    }

                    fixture.Dispose();
                }
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serializedObject,
            string propertyName)
        {
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);
            Require(
                property != null,
                $"Serialized property '{propertyName}' is unavailable on " +
                $"'{serializedObject.targetObject.GetType().FullName}'.");
            return property;
        }

        private static void SetObjectArray(
            SerializedObject serializedObject,
            string propertyName,
            UnityEngine.Object[] values)
        {
            SerializedProperty property =
                RequireProperty(serializedObject, propertyName);
            property.arraySize = values?.Length ?? 0;
            for (int index = 0; index < property.arraySize; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue =
                    values[index];
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");

        private sealed class RuntimeFixture : IDisposable
        {
            private readonly GameObject root;
            private readonly PauseRequestTrigger pauseControl;
            private InputActionAsset actionAsset;

            private RuntimeFixture(
                GameObject root,
                PauseRequestTrigger pauseControl,
                PlayerInput playerInput,
                PlayerActorDeclaration playerActor,
                UnityInputTargetDeclaration gameplayTarget,
                UnityInputTargetDeclaration globalTarget,
                PauseInputModeUnityPlayerInputRuntimeBridge bridge,
                LocalPlayerProvisioningAuthoring provisioningAuthoring,
                InputActionAsset actionAsset)
            {
                this.root = root;
                this.pauseControl = pauseControl;
                PlayerInput = playerInput;
                PlayerActor = playerActor;
                GameplayTarget = gameplayTarget;
                GlobalTarget = globalTarget;
                Bridge = bridge;
                ProvisioningAuthoring = provisioningAuthoring;
                this.actionAsset = actionAsset;
            }

            internal PlayerInput PlayerInput { get; }
            internal PlayerActorDeclaration PlayerActor { get; }
            internal UnityInputTargetDeclaration GameplayTarget { get; }
            internal UnityInputTargetDeclaration GlobalTarget { get; }
            internal PauseInputModeUnityPlayerInputRuntimeBridge Bridge { get; }
            internal LocalPlayerProvisioningAuthoring ProvisioningAuthoring { get; }

            internal bool IsValid =>
                root != null &&
                PlayerInput != null &&
                PlayerActor != null &&
                GameplayTarget != null &&
                GlobalTarget != null &&
                Bridge != null &&
                ProvisioningAuthoring != null &&
                PlayerInput.GetComponent<UnityPlayerInputGateAdapter>() != null;

            internal string CurrentActionMapName =>
                PlayerInput.currentActionMap == null
                    ? string.Empty
                    : PlayerInput.currentActionMap.name;

            internal static RuntimeFixture Create(string name)
            {
                LocalPlayerProvisioningAuthoring provisioningAuthoring =
                    ResolveProvisioningAuthoring();
                GameObject root = null;
                InputActionAsset actionAsset = null;

                try
                {
                    root = new GameObject(name);
                    root.SetActive(false);

                    PauseRequestTrigger pauseControl =
                        root.AddComponent<PauseRequestTrigger>();

                    var playerObject = new GameObject(name + " Player");
                    playerObject.transform.SetParent(root.transform, false);
                    actionAsset = CreateActionAsset(
                        includeGlobalMap: true,
                        includePlayerMap: true,
                        includeUiMap: true);
                    PlayerInput playerInput =
                        playerObject.AddComponent<PlayerInput>();
                    playerInput.actions = actionAsset;
                    playerInput.defaultActionMap = "Global";
                    playerObject.AddComponent<UnityPlayerInputGateAdapter>();

                    PlayerActorDeclaration playerActor =
                        playerObject.AddComponent<PlayerActorDeclaration>();
                    ConfigurePlayerActor(playerActor, playerInput);

                    UnityInputTargetDeclaration gameplayTarget =
                        playerObject.AddComponent<
                            UnityInputTargetDeclaration>();
                    ConfigureInputTarget(
                        gameplayTarget,
                        UnityInputTargetRole.GameplayCommands,
                        "qa.ic2.input.target.gameplay",
                        "QA IC2 Gameplay Target",
                        playerInput,
                        requirePlayerInput: true);

                    var globalTargetObject =
                        new GameObject(name + " Global Pause Target");
                    globalTargetObject.transform.SetParent(
                        root.transform,
                        false);
                    UnityInputTargetDeclaration globalTarget =
                        globalTargetObject.AddComponent<
                            UnityInputTargetDeclaration>();
                    ConfigureInputTarget(
                        globalTarget,
                        UnityInputTargetRole.GlobalUiPause,
                        "qa.ic2.input.target.global-pause",
                        "QA IC2 Global Pause Target",
                        null,
                        requirePlayerInput: false);

                    var bridgeObject = new GameObject(name + " Bridge");
                    bridgeObject.transform.SetParent(root.transform, false);
                    PauseInputModeUnityPlayerInputRuntimeBridge bridge =
                        bridgeObject.AddComponent<
                            PauseInputModeUnityPlayerInputRuntimeBridge>();
                    ConfigureBridge(
                        bridge,
                        playerInput,
                        new[] { globalTarget, gameplayTarget },
                        new[] { playerActor },
                        provisioningAuthoring);

                    root.SetActive(true);
                    return new RuntimeFixture(
                        root,
                        pauseControl,
                        playerInput,
                        playerActor,
                        gameplayTarget,
                        globalTarget,
                        bridge,
                        provisioningAuthoring,
                        actionAsset);
                }
                catch
                {
                    DestroyImmediateSafe(root);
                    DestroyImmediateSafe(actionAsset);
                    throw;
                }
            }

            internal PauseInputModeUnityPlayerInputRuntimeBridgeResult
                SubmitPause()
            {
                Bridge.RequestPause();
                Require(
                    Bridge.LastResult != null,
                    "Canonical bridge did not produce a Pause result.");
                return Bridge.LastResult;
            }

            internal PauseInputModeUnityPlayerInputRuntimeBridgeResult
                SubmitResume()
            {
                Bridge.RequestResume();
                Require(
                    Bridge.LastResult != null,
                    "Canonical bridge did not produce a Resume result.");
                return Bridge.LastResult;
            }

            internal PauseInputModeUnityPlayerInputRuntimeBridgeResult
                SubmitToggle()
            {
                Bridge.TogglePause();
                Require(
                    Bridge.LastResult != null,
                    "Canonical bridge did not produce a Toggle result.");
                return Bridge.LastResult;
            }

            internal InputModeRuntimeSnapshot RequireRuntimeSnapshot()
            {
                InputModeRuntimeSnapshot snapshot =
                    Bridge.InputModeRuntimeSnapshot;
                Require(
                    snapshot != null && snapshot.IsInitialized,
                    "Canonical bridge has no initialized resident InputMode snapshot.");
                return snapshot;
            }

            internal bool TryGetPauseState(out PauseState state)
            {
                if (!pauseControl.TryGetPauseSnapshot(
                        out PauseSnapshot snapshot))
                {
                    state = PauseState.Unknown;
                    return false;
                }

                state = snapshot.State;
                return true;
            }

            internal void EnsurePauseState(PauseState targetState)
            {
                if (!TryGetPauseState(out PauseState currentState))
                {
                    throw new InvalidOperationException(
                        "Framework Pause runtime is unavailable.");
                }

                if (currentState == targetState)
                {
                    return;
                }

                if (targetState == PauseState.Paused)
                {
                    pauseControl.RequestPause();
                }
                else if (targetState == PauseState.Running)
                {
                    pauseControl.RequestResume();
                }
                else
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(targetState),
                        targetState,
                        "Pause target state must be Running or Paused.");
                }

                Require(
                    TryGetPauseState(out PauseState resolvedState) &&
                    resolvedState == targetState,
                    $"Could not establish Pause state '{targetState}'.");
            }

            internal void ReplaceActionAsset(
                bool includeGlobalMap,
                bool includePlayerMap,
                bool includeUiMap)
            {
                InputActionAsset previous = actionAsset;
                actionAsset = CreateActionAsset(
                    includeGlobalMap,
                    includePlayerMap,
                    includeUiMap);
                PlayerInput.actions = actionAsset;
                PlayerInput.defaultActionMap = includeGlobalMap
                    ? "Global"
                    : includePlayerMap
                        ? "Player"
                        : includeUiMap
                            ? "UI"
                            : string.Empty;
                DestroyImmediateSafe(previous);
            }

            internal bool HasExactEnabledMaps(params string[] expectedNames)
            {
                if (PlayerInput.actions == null)
                {
                    return false;
                }

                var expected = new HashSet<string>(
                    expectedNames ?? Array.Empty<string>(),
                    StringComparer.Ordinal);
                foreach (InputActionMap map in PlayerInput.actions.actionMaps)
                {
                    if (map.enabled != expected.Contains(map.name))
                    {
                        return false;
                    }
                }

                return true;
            }

            internal bool HasAction(string mapName, string actionName)
            {
                InputActionMap map = PlayerInput.actions?.FindActionMap(
                    mapName,
                    false);
                return map?.FindAction(actionName, false) != null && map.enabled;
            }

            internal bool HasBridgeFixtureInputAndActionIdentity =>
                ReferenceEquals(Bridge.PlayerInput, PlayerInput) &&
                ReferenceEquals(
                    Bridge.PlayerInput == null
                        ? null
                        : Bridge.PlayerInput.actions,
                    PlayerInput.actions);

            internal string DescribeMissingGlobalPreflight(
                PauseState pauseBefore,
                InputModeRuntimeSnapshot inputModeBefore,
                InputModeRuntimeOperationResult operationBefore,
                PauseInputModeUnityPlayerInputRuntimeBridgeResult result,
                PauseState pauseAfter,
                InputModeRuntimeSnapshot inputModeAfter)
            {
                PlayerInput bridgePlayerInput = Bridge.PlayerInput;
                InputActionAsset bridgeActions = bridgePlayerInput == null
                    ? null
                    : bridgePlayerInput.actions;
                InputActionAsset fixtureActions = PlayerInput.actions;
                InputModeRuntimeOperationResult operationAfter =
                    Bridge.LastInputModeRuntimeOperation;

                return
                    $"bridgePlayerInputEntityId='{EntityId(bridgePlayerInput)}' " +
                    $"fixturePlayerInputEntityId='{EntityId(PlayerInput)}' " +
                    $"bridgeActionsEntityId='{EntityId(bridgeActions)}' " +
                    $"fixtureActionsEntityId='{EntityId(fixtureActions)}' " +
                    $"bridgePlayerInputMatchesFixture='{ReferenceEquals(bridgePlayerInput, PlayerInput)}' " +
                    $"bridgeActionsMatchFixture='{ReferenceEquals(bridgeActions, fixtureActions)}' " +
                    $"globalPresentOnBridgeAsset='{HasActionMap(bridgeActions, "Global")}' " +
                    $"globalPresentOnFixtureAsset='{HasActionMap(fixtureActions, "Global")}' " +
                    $"pauseBefore='{pauseBefore}' pauseAfter='{pauseAfter}' " +
                    $"inputModeBefore='{inputModeBefore.CurrentMode}' " +
                    $"inputModeAfter='{inputModeAfter.CurrentMode}' " +
                    $"operationBeforeStatus='{OperationStatus(operationBefore)}' " +
                    $"operationBeforeMessage='{OperationMessage(operationBefore)}' " +
                    $"operationAfterStatus='{OperationStatus(operationAfter)}' " +
                    $"operationAfterMessage='{OperationMessage(operationAfter)}' " +
                    $"bridgeResultStatus='{result.Status}' " +
                    $"bridgeResultMessage='{Escape(result.Message)}'.";
            }

            internal string DescribePreflightFailure(
                PauseState pauseBefore,
                InputModeRuntimeSnapshot inputModeBefore,
                InputModeRuntimeOperationResult operationBefore,
                PauseInputModeUnityPlayerInputRuntimeBridgeResult result,
                PauseState pauseAfter,
                InputModeRuntimeSnapshot inputModeAfter)
            {
                InputModeRuntimeOperationResult operationAfter =
                    Bridge.LastInputModeRuntimeOperation;
                return
                    $"resultStatus='{result.Status}' " +
                    $"pauseRequestSubmitted='{result.PauseRequestSubmitted}' " +
                    $"pauseBefore='{pauseBefore}' pauseAfter='{pauseAfter}' " +
                    $"inputModeBefore='{inputModeBefore.CurrentMode}' " +
                    $"inputModeAfter='{inputModeAfter.CurrentMode}' " +
                    $"revisionBefore='{inputModeBefore.Revision}' " +
                    $"revisionAfter='{inputModeAfter.Revision}' " +
                    $"operationInFlight='{inputModeAfter.OperationInFlight}' " +
                    $"operationBeforeStatus='{OperationStatus(operationBefore)}' " +
                    $"operationAfterStatus='{OperationStatus(operationAfter)}' " +
                    $"operationReferencePreserved='{ReferenceEquals(operationBefore, operationAfter)}' " +
                    $"bridgeResultMessage='{Escape(result.Message)}'.";
            }

            internal void SetPlayerActorEvidence(bool enabled)
            {
                var serializedBridge = new SerializedObject(Bridge);
                SetObjectArray(
                    serializedBridge,
                    "playerActors",
                    enabled
                        ? new UnityEngine.Object[] { PlayerActor }
                        : Array.Empty<UnityEngine.Object>());
                serializedBridge.ApplyModifiedPropertiesWithoutUndo();
            }

            public void Dispose()
            {
                DestroyImmediateSafe(root);
                DestroyImmediateSafe(actionAsset);
            }

            private static string EntityId(UnityEngine.Object value) =>
                value == null ? string.Empty : value.GetEntityId().ToString();

            private static bool HasActionMap(
                InputActionAsset actionAsset,
                string actionMapName) =>
                actionAsset?.FindActionMap(actionMapName, false) != null;

            private static string OperationStatus(
                InputModeRuntimeOperationResult operation) =>
                operation == null ? "null" : operation.Status.ToString();

            private static string OperationMessage(
                InputModeRuntimeOperationResult operation) =>
                operation == null ? string.Empty : Escape(operation.Message);


            private static LocalPlayerProvisioningAuthoring
                ResolveProvisioningAuthoring()
            {
                LocalPlayerProvisioningAuthoring[] candidates =
                    UnityEngine.Object.FindObjectsByType<
                        LocalPlayerProvisioningAuthoring>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                LocalPlayerProvisioningAuthoring resolved = null;
                int loadedCount = 0;
                for (int index = 0; index < candidates.Length; index++)
                {
                    LocalPlayerProvisioningAuthoring candidate =
                        candidates[index];
                    if (candidate == null ||
                        !candidate.gameObject.scene.IsValid() ||
                        !candidate.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    loadedCount++;
                    resolved = candidate;
                }

                Require(
                    loadedCount == 1 && resolved != null,
                    "IC2 runtime regression requires exactly one loaded " +
                    "LocalPlayerProvisioningAuthoring from the canonical " +
                    $"UIGlobal composition. found='{loadedCount}'.");
                return resolved;
            }

            private static void ConfigurePlayerActor(
                PlayerActorDeclaration playerActor,
                PlayerInput playerInput)
            {
                var serializedActor = new SerializedObject(playerActor);
                RequireProperty(serializedActor, "actorId").stringValue =
                    "qa.ic2.actor.player.primary";
                RequireProperty(serializedActor, "displayName").stringValue =
                    "QA IC2 Player Actor";
                RequireProperty(serializedActor, "reason").stringValue =
                    "qa.ic2.runtime-authority";
                RequireProperty(serializedActor, "playerInput")
                    .objectReferenceValue = playerInput;
                serializedActor.ApplyModifiedPropertiesWithoutUndo();
            }

            private static void ConfigureInputTarget(
                UnityInputTargetDeclaration target,
                UnityInputTargetRole role,
                string targetId,
                string displayName,
                PlayerInput playerInput,
                bool requirePlayerInput)
            {
                var serializedTarget = new SerializedObject(target);
                RequireProperty(serializedTarget, "targetRole").intValue =
                    (int)role;
                RequireProperty(serializedTarget, "targetId").stringValue =
                    targetId;
                RequireProperty(serializedTarget, "displayName").stringValue =
                    displayName;
                RequireProperty(serializedTarget, "playerInput")
                    .objectReferenceValue = playerInput;
                RequireProperty(
                    serializedTarget,
                    "requirePlayerInputEvidence").boolValue =
                    requirePlayerInput;
                RequireProperty(serializedTarget, "reason").stringValue =
                    "qa.ic2.runtime-authority";
                serializedTarget.ApplyModifiedPropertiesWithoutUndo();
            }

            private static void ConfigureBridge(
                PauseInputModeUnityPlayerInputRuntimeBridge bridge,
                PlayerInput playerInput,
                UnityInputTargetDeclaration[] targets,
                PlayerActorDeclaration[] actors,
                LocalPlayerProvisioningAuthoring provisioningAuthoring)
            {
                var serializedBridge = new SerializedObject(bridge);
                RequireProperty(serializedBridge, "playerInput")
                    .objectReferenceValue = playerInput;
                SetObjectArray(
                    serializedBridge,
                    "unityInputTargets",
                    Array.ConvertAll<
                        UnityInputTargetDeclaration,
                        UnityEngine.Object>(
                        targets,
                        value => value));
                SetObjectArray(
                    serializedBridge,
                    "playerActors",
                    Array.ConvertAll<
                        PlayerActorDeclaration,
                        UnityEngine.Object>(
                        actors,
                        value => value));
                RequireProperty(
                    serializedBridge,
                    "localPlayerProvisioningAuthoring")
                    .objectReferenceValue = provisioningAuthoring;
                RequireProperty(
                    serializedBridge,
                    "requireLocalPlayerProvisioning").boolValue = true;
                RequireProperty(
                    serializedBridge,
                    "globalActionMapName").stringValue = "Global";
                RequireProperty(
                    serializedBridge,
                    "gameplayActionMapName").stringValue = "Player";
                RequireProperty(serializedBridge, "uiActionMapName")
                    .stringValue = "UI";
                RequireProperty(serializedBridge, "reason").stringValue =
                    "qa.ic2.runtime-authority";
                RequireProperty(serializedBridge, "logResults").boolValue =
                    false;
                serializedBridge.ApplyModifiedPropertiesWithoutUndo();
            }

            private static InputActionAsset CreateActionAsset(
                bool includeGlobalMap,
                bool includePlayerMap,
                bool includeUiMap)
            {
                var asset = ScriptableObject.CreateInstance<InputActionAsset>();
                asset.name = "QA IC2 Runtime Authority Actions";

                if (includeGlobalMap)
                {
                    var globalMap = new InputActionMap("Global");
                    globalMap.AddAction(
                        "PauseToggle",
                        InputActionType.Button);
                    asset.AddActionMap(globalMap);
                }

                if (includePlayerMap)
                {
                    var playerMap = new InputActionMap("Player");
                    playerMap.AddAction("Move", InputActionType.Value);
                    asset.AddActionMap(playerMap);
                }

                if (includeUiMap)
                {
                    var uiMap = new InputActionMap("UI");
                    uiMap.AddAction("Submit", InputActionType.Button);
                    asset.AddActionMap(uiMap);
                }

                return asset;
            }

            private static void DestroyImmediateSafe(UnityEngine.Object target)
            {
                if (target != null)
                {
                    UnityEngine.Object.DestroyImmediate(target);
                }
            }
        }
    }
}

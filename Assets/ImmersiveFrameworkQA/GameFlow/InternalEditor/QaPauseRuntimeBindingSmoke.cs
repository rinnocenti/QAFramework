using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Gate;
using Immersive.Framework.Pause;
using FrameworkPauseState = Immersive.Framework.Pause.PauseState;
using Immersive.Framework.Reset;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaPauseRuntimeBindingSmoke
    {
        private const string LogPrefix = "[QA_PAUSE_CONTRACT]";

        [MenuItem("Immersive Framework/QA/Regressions/Pause/Run Pause Contract Regression", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Regressions/Pause/Run Pause Contract Regression")]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        public static async Task RunInternalAsync()
        {
            var completed = new List<string>();

            try
            {
                Require(EditorApplication.isPlaying, "Pause contract regression requires Play Mode.");
                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(out FrameworkRuntimeHost host) && host != null,
                    "Pause contract regression requires the canonical FrameworkRuntimeHost.");
                Require(
                    host.State.CurrentRoute != null && host.State.CurrentActivity != null,
                    "Pause contract regression requires an active Route and Activity.");
                Require(
                    host.TryGetPauseSnapshot(out PauseSnapshot entryPause) && entryPause.State == FrameworkPauseState.Running,
                    $"Pause contract regression requires a Running baseline and does not assume ownership of a pre-existing Pause. baseline='{entryPause.ToDiagnosticString()}'.");

                await RunPassAsync(host, "run-1", completed);
                await RunPassAsync(host, "run-2", completed);

                Require(
                    host.TryGetPauseSnapshot(out PauseSnapshot terminalPause) && terminalPause.State == FrameworkPauseState.Running,
                    $"Pause contract regression left residual Pause state. terminal='{terminalPause.ToDiagnosticString()}'.");
                Require(
                    !host.PauseGateSnapshot.HasBlockers,
                    $"Pause contract regression left residual Pause Gate blockers. blockers='{host.PauseGateSnapshot.BlockerCount}'.");
                completed.Add("terminal-no-residual-pause-or-gate");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}/{completed.Count}' failed='0' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' cases='{completed.Count}/pending' failed='1' firstFailure='{exception.Message}' completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static async Task RunPassAsync(
            FrameworkRuntimeHost host,
            string pass,
            ICollection<string> completed)
        {
            VerifyUnboundSurfacesDoNotFallback(host, pass, completed);
            VerifyMandatoryAuthoringFailure(pass, completed);

            PauseFixture fixture = null;
            try
            {
                fixture = PauseFixture.Create(host, $"QA Pause Contract {pass}");

                Require(
                    fixture.Trigger.HasPauseProductRequestBinding &&
                    fixture.Binding.HasActiveBinding &&
                    fixture.Adapter.HasInputGateRuntimeBinding,
                    fixture.Diagnostic("Canonical Pause composition did not bind all required endpoints."));
                completed.Add($"{pass}:runtime-and-bindings-available");

                Require(
                    fixture.Trigger.TryGetPauseSnapshot(out PauseSnapshot baseline) && baseline.State == FrameworkPauseState.Running,
                    fixture.Diagnostic("Pause baseline was not Running before mutation."));
                Require(
                    fixture.GameplayMap.enabled && fixture.GlobalMap.enabled && fixture.PauseAction.enabled,
                    fixture.Diagnostic("Canonical binding did not establish the expected Gameplay + Global baseline."));
                completed.Add($"{pass}:baseline-captured-before-pause");

                var routeBeforePause = host.State.CurrentRoute;
                var activityBeforePause = host.State.CurrentActivity;
                fixture.Trigger.RequestPause();
                Require(
                    fixture.Trigger.LastRequestSucceeded &&
                    string.Equals(fixture.Trigger.LastProductStatus, "Applied", StringComparison.Ordinal) &&
                    fixture.Trigger.TryGetPauseSnapshot(out PauseSnapshot paused) &&
                    paused.State == FrameworkPauseState.Paused &&
                    !fixture.GameplayMap.enabled &&
                    fixture.GlobalMap.enabled &&
                    fixture.PauseAction.enabled,
                    fixture.Diagnostic("Pause did not apply the canonical logical/input posture."));
                Require(
                    host.PauseGateSnapshot.IsBlockedForAnyOwner(GateScope.Input, GateDomain.InputAcceptance) &&
                    host.PauseGateSnapshot.IsBlockedForAnyOwner(GateScope.Interaction, GateDomain.InteractionAcceptance),
                    fixture.Diagnostic("Pause did not expose the expected Input/Interaction Gate blockers."));
                Require(
                    ReferenceEquals(host.State.CurrentRoute, routeBeforePause) &&
                    ReferenceEquals(host.State.CurrentActivity, activityBeforePause),
                    fixture.Diagnostic("Pause incorrectly assumed Route or Activity authority."));
                completed.Add($"{pass}:pause-applies-state-input-and-gate-without-flow-authority");

                fixture.Trigger.RequestPause();
                Require(
                    fixture.Trigger.LastRequestIgnored &&
                    string.Equals(fixture.Trigger.LastProductStatus, "Ignored", StringComparison.Ordinal) &&
                    fixture.Trigger.TryGetPauseSnapshot(out PauseSnapshot repeatedPause) &&
                    repeatedPause.State == FrameworkPauseState.Paused &&
                    !string.IsNullOrWhiteSpace(fixture.Trigger.LastMessage),
                    fixture.Diagnostic("Repeated Pause did not remain an explicit diagnostic no-change operation."));
                completed.Add($"{pass}:repeated-pause-is-explicit-no-change");

                fixture.Trigger.RequestResume();
                Require(
                    fixture.Trigger.LastRequestSucceeded &&
                    fixture.Trigger.TryGetPauseSnapshot(out PauseSnapshot resumed) &&
                    resumed.State == FrameworkPauseState.Running &&
                    fixture.GameplayMap.enabled &&
                    fixture.GlobalMap.enabled &&
                    fixture.PauseAction.enabled &&
                    !host.PauseGateSnapshot.HasBlockers,
                    fixture.Diagnostic("Resume did not restore the canonical enabled Gameplay baseline."));
                completed.Add($"{pass}:resume-restores-enabled-gameplay-baseline");

                await VerifyPauseActivityRestartAsync(host, fixture, pass, completed);

                fixture.ReleaseAndVerify();
                completed.Add($"{pass}:scene-release-cleans-binding-pause-and-input-posture");
            }
            finally
            {
                fixture?.Dispose();
            }

            PauseFixture destroyFixture = null;
            try
            {
                destroyFixture = PauseFixture.Create(host, $"QA Pause Destroy Teardown {pass}");
                destroyFixture.Trigger.RequestPause();
                Require(
                    destroyFixture.Trigger.LastRequestSucceeded && destroyFixture.Trigger.IsPaused,
                    destroyFixture.Diagnostic("Failed to establish Pause before destroy teardown."));
                destroyFixture.DestroyBoundRoot();
                await Task.Yield();
                destroyFixture.VerifyDestroyedRootCleanup();
                completed.Add($"{pass}:destroy-teardown-releases-pause-binding-and-gate");
            }
            finally
            {
                destroyFixture?.Dispose();
            }

            PauseFixture disabledBaseline = null;
            try
            {
                disabledBaseline = PauseFixture.Create(host, $"QA Pause Disabled Baseline {pass}");
                disabledBaseline.GameplayMap.Disable();
                Require(
                    !disabledBaseline.GameplayMap.enabled,
                    disabledBaseline.Diagnostic("Failed to establish disabled Gameplay baseline before Pause."));

                disabledBaseline.Trigger.RequestPause();
                Require(
                    disabledBaseline.Trigger.LastRequestSucceeded &&
                    disabledBaseline.Trigger.IsPaused &&
                    !disabledBaseline.GameplayMap.enabled,
                    disabledBaseline.Diagnostic("Pause changed a Gameplay map that was already disabled."));

                disabledBaseline.Trigger.RequestResume();
                Require(
                    disabledBaseline.Trigger.LastRequestSucceeded &&
                    !disabledBaseline.Trigger.IsPaused &&
                    !disabledBaseline.GameplayMap.enabled,
                    disabledBaseline.Diagnostic(
                        "Resume enabled Gameplay even though Gameplay was disabled immediately before Pause. " +
                        "The Pause boundary must restore the pre-mutation input baseline instead of assuming Gameplay enabled."));
                completed.Add($"{pass}:resume-preserves-disabled-gameplay-baseline");

                disabledBaseline.ReleaseAndVerify();
                completed.Add($"{pass}:disabled-baseline-release-is-clean");
            }
            finally
            {
                disabledBaseline?.Dispose();
            }
        }

        private static void VerifyUnboundSurfacesDoNotFallback(
            FrameworkRuntimeHost host,
            string pass,
            ICollection<string> completed)
        {
            Require(
                host.TryGetPauseSnapshot(out PauseSnapshot baseline),
                "Pause baseline was unavailable before the unbound trigger negative case.");

            var root = new GameObject($"QA Unbound Pause Trigger {pass}");
            try
            {
                var trigger = root.AddComponent<PauseRequestTrigger>();
                trigger.RequestPause();

                PauseSnapshot after = default;
                Require(
                    !trigger.HasPauseProductRequestBinding &&
                    trigger.LastRequestFailed &&
                    string.Equals(trigger.LastProductStatus, "BindingUnavailable", StringComparison.Ordinal) &&
                    trigger.ProductRequestBindingDiagnostic.Contains("not bound") &&
                    host.TryGetPauseSnapshot(out after) &&
                    after.Equals(baseline),
                    $"Unbound PauseRequestTrigger unexpectedly discovered/mutated runtime. " +
                    $"bound='{trigger.HasPauseProductRequestBinding}' status='{trigger.LastProductStatus}' " +
                    $"binding='{trigger.ProductRequestBindingDiagnostic}' before='{baseline.ToDiagnosticString()}' after='{after.ToDiagnosticString()}'.");
                completed.Add($"{pass}:unbound-trigger-does-not-fallback-to-host");
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static void VerifyMandatoryAuthoringFailure(
            string pass,
            ICollection<string> completed)
        {
            var root = new GameObject($"QA Invalid Pause Binding {pass}");
            try
            {
                var binding = root.AddComponent<PlayerPauseInput>();
                Require(
                    !binding.TryValidateAuthoring(out string diagnostic) &&
                    !string.IsNullOrWhiteSpace(diagnostic),
                    "PlayerPauseInput accepted missing mandatory authoring without a diagnostic.");
                completed.Add($"{pass}:missing-binding-authoring-fails-explicitly");
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
            }
        }

        private static async Task VerifyPauseActivityRestartAsync(
            FrameworkRuntimeHost host,
            PauseFixture fixture,
            string pass,
            ICollection<string> completed)
        {
            var activityBeforeRestart = host.State.CurrentActivity;
            var routeBeforeRestart = host.State.CurrentRoute;

            fixture.Trigger.RequestPause();
            Require(
                fixture.Trigger.LastRequestSucceeded && fixture.Trigger.IsPaused && !fixture.GameplayMap.enabled,
                fixture.Diagnostic("Failed to establish Pause before Activity Restart."));

            ActivityRestartTrigger restart = fixture.RestartTrigger;
            IActivityRestartRuntimePort restartRuntime = host;
            Require(
                restart.TryBindActivityRestartRuntime(restartRuntime, out string bindingIssue),
                bindingIssue);
            restart.ConfigureForQa(
                null,
                true,
                true,
                "pause-active-restart",
                ResetSelectionMode.ExplicitSubjects,
                Array.Empty<ResetSubjectReference>(),
                true,
                true,
                true,
                false);

            ActivityRestartResult result = await restart.RequestActivityRestartAsync();
            Require(
                result != null &&
                (result.Succeeded || result.CompletedWithWarnings) &&
                result.ClearStatus == "Succeeded" &&
                result.ReenterStatus == "Succeeded" &&
                !restart.IsRequestInFlight &&
                ReferenceEquals(host.State.CurrentActivity, activityBeforeRestart) &&
                ReferenceEquals(host.State.CurrentRoute, routeBeforeRestart),
                result != null ? result.ToDiagnosticString() : "Activity Restart returned no result.");
            Require(
                fixture.Trigger.TryGetPauseSnapshot(out PauseSnapshot afterRestart) &&
                afterRestart.State == FrameworkPauseState.Paused &&
                !fixture.GameplayMap.enabled &&
                fixture.GlobalMap.enabled &&
                host.PauseGateSnapshot.IsBlockedForAnyOwner(GateScope.Input, GateDomain.InputAcceptance) &&
                host.PauseGateSnapshot.IsBlockedForAnyOwner(GateScope.Interaction, GateDomain.InteractionAcceptance),
                fixture.Diagnostic(
                    "Activity Restart did not preserve the existing Pause authority/input posture. " +
                    $"restart='{result.ToDiagnosticString()}'."));
            completed.Add($"{pass}:paused-activity-restart-completes-and-preserves-pause");

            fixture.Trigger.RequestResume();
            Require(
                fixture.Trigger.LastRequestSucceeded &&
                fixture.Trigger.TryGetPauseSnapshot(out PauseSnapshot resumed) &&
                resumed.State == FrameworkPauseState.Running &&
                fixture.GameplayMap.enabled &&
                !host.PauseGateSnapshot.HasBlockers,
                fixture.Diagnostic("Resume after Activity Restart did not restore Running/input state."));
            completed.Add($"{pass}:resume-after-restart-restores-running-input");
        }

        private static void Require(bool value, string message)
        {
            if (!value)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class PauseFixture : IDisposable
        {
            private readonly FrameworkRuntimeHost _host;
            private readonly GameObject _root;
            private readonly InputActionAsset _actions;
            private readonly InputActionReference _pauseActionReference;
            private readonly PauseProductBindingRuntimeContext _productRuntime;
            private readonly PauseProductBindingSceneLifecycleParticipant _sceneParticipant;
            private readonly IReadOnlyList<GameObject> _roots;
            private bool _released;

            private PauseFixture(
                FrameworkRuntimeHost host,
                GameObject root,
                InputActionAsset actions,
                InputActionReference pauseActionReference,
                PlayerInput playerInput,
                UnityPlayerInputGateAdapter adapter,
                PlayerPauseInput binding,
                PauseRequestTrigger trigger,
                ActivityRestartTrigger restartTrigger,
                InputActionMap globalMap,
                InputActionMap gameplayMap,
                InputAction pauseAction,
                PauseProductBindingRuntimeContext productRuntime,
                PauseProductBindingSceneLifecycleParticipant sceneParticipant)
            {
                _host = host;
                _root = root;
                _actions = actions;
                _pauseActionReference = pauseActionReference;
                PlayerInput = playerInput;
                Adapter = adapter;
                Binding = binding;
                Trigger = trigger;
                RestartTrigger = restartTrigger;
                GlobalMap = globalMap;
                GameplayMap = gameplayMap;
                PauseAction = pauseAction;
                _productRuntime = productRuntime;
                _sceneParticipant = sceneParticipant;
                _roots = new[] { root };
            }

            internal PlayerInput PlayerInput { get; }
            internal UnityPlayerInputGateAdapter Adapter { get; }
            internal PlayerPauseInput Binding { get; }
            internal PauseRequestTrigger Trigger { get; }
            internal ActivityRestartTrigger RestartTrigger { get; }
            internal InputActionMap GlobalMap { get; }
            internal InputActionMap GameplayMap { get; }
            internal InputAction PauseAction { get; }

            internal static PauseFixture Create(FrameworkRuntimeHost host, string name)
            {
                var root = new GameObject(name);
                root.SetActive(false);

                var actions = ScriptableObject.CreateInstance<InputActionAsset>();
                actions.name = $"{name} Actions";
                InputActionMap globalMap = actions.AddActionMap("Global");
                InputAction pauseAction = globalMap.AddAction("Pause", InputActionType.Button);
                InputActionMap gameplayMap = actions.AddActionMap("Gameplay");
                gameplayMap.AddAction("Move", InputActionType.Value);
                InputActionReference pauseActionReference = InputActionReference.Create(pauseAction);

                var playerInput = root.AddComponent<PlayerInput>();
                playerInput.actions = actions;
                playerInput.defaultActionMap = gameplayMap.name;

                var adapter = root.AddComponent<UnityPlayerInputGateAdapter>();
                var binding = root.AddComponent<PlayerPauseInput>();
                var trigger = root.AddComponent<PauseRequestTrigger>();
                var restartTrigger = root.AddComponent<ActivityRestartTrigger>();

                ConfigureAdapter(adapter, playerInput, actions, gameplayMap);
                ConfigureBinding(binding, playerInput, pauseActionReference, actions, gameplayMap);

                root.SetActive(true);
                InputActionMap runtimeGlobalMap = playerInput.actions.FindActionMap(globalMap.id);
                InputActionMap runtimeGameplayMap = playerInput.actions.FindActionMap(gameplayMap.id);
                InputAction runtimePauseAction = playerInput.actions.FindAction(pauseAction.id.ToString(), false);
                if (runtimeGlobalMap == null || runtimeGameplayMap == null || runtimePauseAction == null)
                {
                    UnityEngine.Object.Destroy(root);
                    UnityEngine.Object.Destroy(pauseActionReference);
                    UnityEngine.Object.Destroy(actions);
                    throw new InvalidOperationException(
                        "PlayerInput runtime action copy did not preserve the authored Global/Gameplay/Pause GUIDs.");
                }

                runtimeGlobalMap.Enable();
                runtimeGameplayMap.Enable();

                IInputGateRuntimePort gateRuntime = host;
                if (!adapter.TryBindInputGateRuntime(gateRuntime, out string gateIssue))
                {
                    UnityEngine.Object.Destroy(root);
                    UnityEngine.Object.Destroy(pauseActionReference);
                    UnityEngine.Object.Destroy(actions);
                    throw new InvalidOperationException(gateIssue);
                }

                if (!binding.TryValidateAuthoring(out string authoringIssue))
                {
                    UnityEngine.Object.Destroy(root);
                    UnityEngine.Object.Destroy(pauseActionReference);
                    UnityEngine.Object.Destroy(actions);
                    throw new InvalidOperationException(authoringIssue);
                }

                var productRuntime = new PauseProductBindingRuntimeContext((IPauseProductApplicationPort)host);
                var sceneParticipant = new PauseProductBindingSceneLifecycleParticipant(productRuntime);
                var fixture = new PauseFixture(
                    host,
                    root,
                    actions,
                    pauseActionReference,
                    playerInput,
                    adapter,
                    binding,
                    trigger,
                    restartTrigger,
                    runtimeGlobalMap,
                    runtimeGameplayMap,
                    runtimePauseAction,
                    productRuntime,
                    sceneParticipant);

                if (!sceneParticipant.OnSceneAvailable(root.scene, fixture._roots, out string compositionIssue))
                {
                    fixture.Dispose();
                    throw new InvalidOperationException(compositionIssue);
                }

                return fixture;
            }

            internal void ReleaseAndVerify()
            {
                if (_released)
                {
                    return;
                }

                Require(
                    _sceneParticipant.OnSceneReleasing(
                        _root.scene,
                        _roots,
                        "qa-pause-contract-cleanup",
                        out string releaseIssue),
                    releaseIssue);
                _released = true;

                Require(
                    !Trigger.HasPauseProductRequestBinding &&
                    !Binding.HasActiveBinding &&
                    !_productRuntime.HasActivePlayerInputBinding &&
                    _host.TryGetPauseSnapshot(out PauseSnapshot pause) &&
                    pause.State == FrameworkPauseState.Running &&
                    !_host.PauseGateSnapshot.HasBlockers,
                    Diagnostic("Scene Lifecycle release left residual Pause/binding evidence."));
            }

            internal void DestroyBoundRoot()
            {
                Require(!_released, "Destroy teardown requires an active composed fixture.");
                _released = true;
                UnityEngine.Object.Destroy(_root);
            }

            internal void VerifyDestroyedRootCleanup()
            {
                PauseSnapshot pause = default;
                Require(
                    !_productRuntime.HasActivePlayerInputBinding &&
                    _host.TryGetPauseSnapshot(out pause) &&
                    pause.State == FrameworkPauseState.Running &&
                    !_host.PauseGateSnapshot.HasBlockers,
                    $"Destroy teardown left residual Pause/product binding evidence. " +
                    $"pause='{pause.ToDiagnosticString()}' activeProductBinding='{_productRuntime.HasActivePlayerInputBinding}' " +
                    $"pauseGateBlockers='{_host.PauseGateSnapshot.BlockerCount}'.");
            }

            internal string Diagnostic(string message)
            {
                _host.TryGetPauseSnapshot(out PauseSnapshot pause);
                return
                    $"{message} fixture='{_root.name}' pause='{pause.ToDiagnosticString()}' " +
                    $"triggerBound='{Trigger.HasPauseProductRequestBinding}' triggerStatus='{Trigger.LastProductStatus}' " +
                    $"bindingStatus='{Binding.BindingStatus}' bindingDiagnostic='{Binding.BindingDiagnostic}' " +
                    $"adapterBound='{Adapter.HasInputGateRuntimeBinding}' adapterStatus='{Adapter.LastStatus}' " +
                    $"globalEnabled='{GlobalMap.enabled}' gameplayEnabled='{GameplayMap.enabled}' pauseActionEnabled='{PauseAction.enabled}'.";
            }

            public void Dispose()
            {
                if (!_released && _root != null)
                {
                    try
                    {
                        _sceneParticipant.OnSceneReleasing(
                            _root.scene,
                            _roots,
                            "qa-pause-contract-finally",
                            out _);
                    }
                    catch
                    {
                        // Preserve the first regression failure; terminal residual checks remain diagnostic.
                    }

                    _released = true;
                }

                if (_root != null)
                {
                    UnityEngine.Object.Destroy(_root);
                }

                if (_pauseActionReference != null)
                {
                    UnityEngine.Object.Destroy(_pauseActionReference);
                }

                if (_actions != null)
                {
                    UnityEngine.Object.Destroy(_actions);
                }
            }

            private static void ConfigureAdapter(
                UnityPlayerInputGateAdapter adapter,
                PlayerInput playerInput,
                InputActionAsset actions,
                InputActionMap gameplayMap)
            {
                var serialized = new SerializedObject(adapter);
                serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
                ConfigureMapReference(serialized.FindProperty("gameplayActionMap"), actions, gameplayMap);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            private static void ConfigureBinding(
                PlayerPauseInput binding,
                PlayerInput playerInput,
                InputActionReference pauseAction,
                InputActionAsset actions,
                InputActionMap gameplayMap)
            {
                var serialized = new SerializedObject(binding);
                serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
                serialized.FindProperty("pauseAction").objectReferenceValue = pauseAction;
                ConfigureMapReference(serialized.FindProperty("gameplayActionMap"), actions, gameplayMap);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            private static void ConfigureMapReference(
                SerializedProperty property,
                InputActionAsset actions,
                InputActionMap gameplayMap)
            {
                Require(property != null, "Serialized Gameplay Action Map reference was not found.");
                property.FindPropertyRelative("actionAsset").objectReferenceValue = actions;
                property.FindPropertyRelative("actionMapId").stringValue = gameplayMap.id.ToString("D");
                property.FindPropertyRelative("cachedActionMapName").stringValue = gameplayMap.name;
            }
        }
    }
}

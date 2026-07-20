using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using PauseState = Immersive.Framework.Pause.PauseState;

namespace ImmersiveFrameworkQA.PauseP1.Editor
{
    internal static class QaPauseP1ConsumerSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Pause P1/Run Consumer Smoke";
        private const string LogPrefix =
            "[QA][PAUSE-P1]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            EditorApplication.isPlaying &&
            !EditorApplication.isCompiling;

        [MenuItem(MenuPath)]
        internal static async void Run()
        {
            var completed = new List<string>();
            Scene loadedScene = default;
            Keyboard addedKeyboard = null;
            float entryTimeScale = Time.timeScale;

            try
            {
                Require(
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        QaPauseP1Paths.ScenePath) != null,
                    "QA Pause P1 scene is missing. Run Setup or Rebuild.");

                loadedScene =
                    await LoadExactSceneInstanceAsync(
                        QaPauseP1Paths.ScenePath);

                GameObject[] roots =
                    loadedScene.GetRootGameObjects();

                PausePlayerInputBinding binding =
                    FindExactlyOne<PausePlayerInputBinding>(
                        loadedScene,
                        roots);
                PauseRequestTrigger trigger =
                    FindExactlyOne<PauseRequestTrigger>(
                        loadedScene,
                        roots);
                PlayerInput playerInput =
                    FindExactlyOne<PlayerInput>(
                        loadedScene,
                        roots);

                var application =
                    new QaPauseP1ApplicationPort(
                        entryTimeScale);
                var runtime =
                    new PauseProductBindingRuntimeContext(
                        application);
                var lifecycle =
                    new PauseProductBindingSceneLifecycleParticipant(
                        runtime);

                Require(
                    lifecycle.OnSceneAvailable(
                        loadedScene,
                        roots,
                        out string bindDiagnostic),
                    bindDiagnostic);

                Require(
                    binding.HasActiveBinding,
                    "Binding token is not active.");

                Require(
                    string.Equals(
                        binding.BindingStatus,
                        "Bound",
                        StringComparison.Ordinal),
                    $"Unexpected binding status '{binding.BindingStatus}'.");

                completed.Add("binding-admitted");

                RequireMaps(
                    playerInput.actions,
                    global: true,
                    player: true,
                    ui: false);
                completed.Add("initial-global-player");

                Require(
                    binding.TryGetRuntimeConfiguration(
                        out PlayerInput configuredInput,
                        out InputAction runtimeAction,
                        out _,
                        out string configurationDiagnostic),
                    configurationDiagnostic);

                Require(
                    ReferenceEquals(
                        configuredInput,
                        playerInput),
                    "Binding resolved a different PlayerInput.");

                Require(
                    !ReferenceEquals(
                        runtimeAction,
                        binding.PauseAction.action),
                    "Pause action was not resolved from PlayerInput.actions clone.");

                Require(
                    runtimeAction.id ==
                        binding.PauseAction.action.id,
                    "Runtime Pause action GUID differs from authoring reference.");

                Require(
                    ReferenceEquals(
                        runtimeAction.actionMap?.asset,
                        playerInput.actions),
                    "Runtime Pause action does not belong to PlayerInput.actions.");

                completed.Add(
                    "pause-action-runtime-clone");

                Require(
                    trigger.TryBindPauseProductRequest(
                        runtime,
                        out string triggerDiagnostic),
                    triggerDiagnostic);

                completed.Add("ui-request-bound");

                trigger.RequestPause();

                Require(
                    trigger.LastRequestSucceeded,
                    trigger.LastMessage);

                Require(
                    application.Snapshot.State ==
                        PauseState.Paused,
                    "UI Pause did not reach Paused.");

                RequireMaps(
                    playerInput.actions,
                    global: true,
                    player: false,
                    ui: true);

                Require(
                    Mathf.Approximately(
                        Time.timeScale,
                        0f),
                    "UI Pause did not apply Time.timeScale 0.");

                completed.Add("ui-pause-global-ui");

                trigger.RequestResume();

                Require(
                    trigger.LastRequestSucceeded,
                    trigger.LastMessage);

                Require(
                    application.Snapshot.State ==
                        PauseState.Running,
                    "UI Resume did not reach Running.");

                RequireMaps(
                    playerInput.actions,
                    global: true,
                    player: true,
                    ui: false);

                Require(
                    Mathf.Approximately(
                        Time.timeScale,
                        entryTimeScale),
                    "UI Resume did not restore Time.timeScale.");

                completed.Add("ui-resume-global-player");

                Keyboard keyboard =
                    Keyboard.current;

                if (keyboard == null)
                {
                    addedKeyboard =
                        InputSystem.AddDevice<Keyboard>();
                    keyboard = addedKeyboard;
                }

                int beforePhysical =
                    application.ApplyCount;

                PressAndReleaseEscape(keyboard);

                Require(
                    application.ApplyCount ==
                        beforePhysical + 1,
                    "One Escape press did not produce exactly one request.");

                Require(
                    application.Snapshot.State ==
                        PauseState.Paused,
                    "Escape did not pause.");

                RequireMaps(
                    playerInput.actions,
                    global: true,
                    player: false,
                    ui: true);

                completed.Add("input-pause-global-ui");

                PressAndReleaseEscape(keyboard);

                Require(
                    application.Snapshot.State ==
                        PauseState.Running,
                    "Second Escape did not resume.");

                RequireMaps(
                    playerInput.actions,
                    global: true,
                    player: true,
                    ui: false);

                completed.Add(
                    "input-resume-global-player");

                trigger.RequestPause();

                Require(
                    application.Snapshot.State ==
                        PauseState.Paused,
                    "Pre-release Pause setup failed.");

                Require(
                    lifecycle.OnSceneReleasing(
                        loadedScene,
                        roots,
                        "qa-pause-p1-unload",
                        out string releaseDiagnostic),
                    releaseDiagnostic);

                Require(
                    application.Snapshot.State ==
                        PauseState.Running,
                    "Release while paused did not restore Running.");

                Require(
                    !binding.HasActiveBinding,
                    "Binding token remained active after release.");

                Require(
                    Mathf.Approximately(
                        Time.timeScale,
                        entryTimeScale),
                    "Release while paused did not restore Time.timeScale.");

                RequireMaps(
                    playerInput.actions,
                    global: false,
                    player: true,
                    ui: false);

                completed.Add(
                    "release-paused-restores-running");
                completed.Add(
                    "prebind-posture-restored");

                var emptyApplication =
                    new QaPauseP1ApplicationPort(
                        entryTimeScale);
                var emptyRuntime =
                    new PauseProductBindingRuntimeContext(
                        emptyApplication);

                PauseProductRequestResult missingResult =
                    emptyRuntime.RequestPause(
                        PauseRequest.Toggle(
                            "qa.pause.p1.missing-binding",
                            nameof(
                                QaPauseP1ConsumerSmoke),
                            "missing-binding"));

                Require(
                    missingResult.Status ==
                        PauseProductRequestStatus
                            .BindingUnavailable,
                    $"Unexpected missing-binding status '{missingResult.Status}'.");

                Require(
                    emptyApplication.ApplyCount == 0,
                    "Missing-binding request mutated application Pause.");

                completed.Add(
                    "missing-binding-rejected");

                Require(
                    lifecycle.OnSceneAvailable(
                        loadedScene,
                        roots,
                        out string rebindDiagnostic),
                    rebindDiagnostic);

                Require(
                    binding.HasActiveBinding,
                    "Rebind did not issue a token.");

                GameObject secondPlayer =
                    UnityEngine.Object.Instantiate(
                        playerInput.gameObject);
                secondPlayer.name =
                    "QA Pause P1 Rejected Second Binding";

                SceneManager.MoveGameObjectToScene(
                    secondPlayer,
                    loadedScene);

                PausePlayerInputBinding secondBinding =
                    secondPlayer.GetComponent<
                        PausePlayerInputBinding>();

                Require(
                    secondBinding != null,
                    "Second binding clone is missing.");

                Require(
                    !secondBinding.TryInjectBindingPort(
                        runtime,
                        out string secondDiagnostic),
                    "Second concurrent binding was unexpectedly accepted.");

                Require(
                    secondDiagnostic.Contains(
                        "another PlayerInput binding",
                        StringComparison.OrdinalIgnoreCase),
                    $"Unexpected second-binding diagnostic '{secondDiagnostic}'.");

                Require(
                    binding.HasActiveBinding,
                    "First binding was displaced by rejected second binding.");

                UnityEngine.Object.Destroy(secondPlayer);
                completed.Add("second-binding-rejected");

                int beforeDuplicateCheck =
                    application.ApplyCount;

                PressAndReleaseEscape(keyboard);

                Require(
                    application.ApplyCount ==
                        beforeDuplicateCheck + 1,
                    "Rebind produced duplicate Pause callbacks.");

                completed.Add(
                    "rebind-no-duplicate-callback");

                Require(
                    lifecycle.OnSceneReleasing(
                        loadedScene,
                        roots,
                        "qa-pause-p1-final-release",
                        out string finalReleaseDiagnostic),
                    finalReleaseDiagnostic);

                completed.Add("final-release");

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"completed='{string.Join(",", completed)}' " +
                    $"message='{Escape(exception.Message)}'.");

                throw;
            }
            finally
            {
                Time.timeScale = entryTimeScale;

                if (addedKeyboard != null &&
                    addedKeyboard.added)
                {
                    InputSystem.RemoveDevice(
                        addedKeyboard);
                }

                if (loadedScene.IsValid() &&
                    loadedScene.isLoaded)
                {
                    await UnloadSceneAsync(
                        loadedScene);
                }
            }
        }

        private static async Task<Scene>
            LoadExactSceneInstanceAsync(string path)
        {
            var preexistingScenes =
                new HashSet<Scene>();

            for (int index = 0;
                 index < SceneManager.sceneCount;
                 index++)
            {
                preexistingScenes.Add(
                    SceneManager.GetSceneAt(index));
            }

            Scene loadedByEvent = default;
            int matchingLoadEventCount = 0;

            void OnSceneLoaded(
                Scene scene,
                LoadSceneMode mode)
            {
                if (mode != LoadSceneMode.Additive ||
                    !string.Equals(
                        scene.path,
                        path,
                        StringComparison.Ordinal) ||
                    preexistingScenes.Contains(scene))
                {
                    return;
                }

                loadedByEvent = scene;
                matchingLoadEventCount++;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

            try
            {
                AsyncOperation operation =
                    SceneManager.LoadSceneAsync(
                        path,
                        LoadSceneMode.Additive);

                Require(
                    operation != null,
                    $"Unable to start loading scene '{path}'.");

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                // Allow sceneLoaded callbacks and root activation to
                // complete before inspecting the loaded instance.
                await Task.Yield();

                Require(
                    matchingLoadEventCount == 1 &&
                    loadedByEvent.IsValid() &&
                    loadedByEvent.isLoaded &&
                    !preexistingScenes.Contains(loadedByEvent),
                    BuildLoadDiagnostic(
                        path,
                        matchingLoadEventCount,
                        loadedByEvent,
                        preexistingScenes));

                return loadedByEvent;
            }
            finally
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        }

        private static string BuildLoadDiagnostic(
            string expectedPath,
            int matchingLoadEventCount,
            Scene loadedByEvent,
            IReadOnlyCollection<Scene> preexistingScenes)
        {
            string loadedScenes =
                string.Join(
                    " | ",
                    Enumerable.Range(
                            0,
                            SceneManager.sceneCount)
                        .Select(
                            SceneManager.GetSceneAt)
                        .Select(
                            scene =>
                                $"name='{scene.name}' " +
                                $"path='{scene.path}' " +
                                $"loaded='{scene.isLoaded}' " +
                                $"preexisting='" +
                                $"{preexistingScenes.Contains(scene)}'"));

            return
                "Could not resolve exactly one newly loaded QA scene instance. " +
                $"expectedPath='{expectedPath}' " +
                $"matchingLoadEvents='{matchingLoadEventCount}' " +
                $"eventSceneName='{loadedByEvent.name}' " +
                $"eventScenePath='{loadedByEvent.path}' " +
                $"loadedScenes='[{loadedScenes}]'.";
        }

        private static async Task UnloadSceneAsync(
            Scene scene)
        {
            AsyncOperation operation =
                SceneManager.UnloadSceneAsync(scene);

            if (operation == null)
            {
                return;
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        private static T FindExactlyOne<T>(
            Scene scene,
            IReadOnlyList<GameObject> roots)
            where T : Component
        {
            T[] matches =
                roots
                    .Where(root => root != null)
                    .SelectMany(
                        root =>
                            root.GetComponentsInChildren<T>(
                                true))
                    .ToArray();

            Require(
                matches.Length == 1,
                BuildComponentDiagnostic<T>(
                    scene,
                    roots,
                    matches.Length));

            return matches[0];
        }

        private static string BuildComponentDiagnostic<T>(
            Scene scene,
            IReadOnlyList<GameObject> roots,
            int matchCount)
            where T : Component
        {
            string rootEvidence =
                string.Join(
                    " | ",
                    roots
                        .Where(root => root != null)
                        .Select(
                            root =>
                                $"root='{root.name}' " +
                                $"active='{root.activeSelf}' " +
                                $"components='[" +
                                $"{string.Join(",", root.GetComponentsInChildren<Component>(true).Where(component => component != null).Select(component => component.GetType().FullName))}]'"));

            return
                $"Expected exactly one {typeof(T).Name}; " +
                $"found '{matchCount}'. " +
                $"sceneName='{scene.name}' " +
                $"scenePath='{scene.path}' " +
                $"rootCount='{roots.Count}' " +
                $"roots='[{rootEvidence}]'.";
        }

        private static void PressAndReleaseEscape(
            Keyboard keyboard)
        {
            InputSystem.QueueStateEvent(
                keyboard,
                new KeyboardState(Key.Escape));
            InputSystem.Update();

            InputSystem.QueueStateEvent(
                keyboard,
                new KeyboardState());
            InputSystem.Update();
        }

        private static void RequireMaps(
            InputActionAsset asset,
            bool global,
            bool player,
            bool ui)
        {
            Require(
                asset.FindActionMap(
                    "Global",
                    true).enabled == global,
                $"Global enabled mismatch. expected='{global}'.");

            Require(
                asset.FindActionMap(
                    "Player",
                    true).enabled == player,
                $"Player enabled mismatch. expected='{player}'.");

            Require(
                asset.FindActionMap(
                    "UI",
                    true).enabled == ui,
                $"UI enabled mismatch. expected='{ui}'.");
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");

        private sealed class QaPauseP1ApplicationPort :
            IPauseProductApplicationPort
        {
            private readonly PauseRuntime _runtime =
                new PauseRuntime();
            private readonly float _runningTimeScale;

            internal QaPauseP1ApplicationPort(
                float runningTimeScale)
            {
                _runningTimeScale =
                    runningTimeScale;
            }

            internal int ApplyCount
            {
                get;
                private set;
            }

            internal PauseSnapshot Snapshot =>
                _runtime.Snapshot;

            public bool TryApplyProductPause(
                PauseRequest request,
                out PauseResult result,
                out string diagnostic)
            {
                result =
                    _runtime.Request(request);
                ApplyCount++;

                ApplyApplicationState();

                diagnostic =
                    result.Message;

                return result.Completed;
            }

            public bool TryRestorePauseSnapshot(
                PauseSnapshot snapshot,
                string reason,
                out string diagnostic)
            {
                if (!snapshot.IsValid)
                {
                    diagnostic =
                        "Restore requires a valid Pause snapshot.";
                    return false;
                }

                if (_runtime.State != snapshot.State)
                {
                    PauseRequest request =
                        snapshot.State ==
                            PauseState.Paused
                            ? PauseRequest.Pause(
                                "qa.pause.p1.restore.paused",
                                nameof(
                                    QaPauseP1ApplicationPort),
                                reason)
                            : PauseRequest.Resume(
                                "qa.pause.p1.restore.running",
                                nameof(
                                    QaPauseP1ApplicationPort),
                                reason);

                    _runtime.Request(request);
                }

                ApplyApplicationState();

                diagnostic =
                    $"QA application Pause restored '{_runtime.State}'.";

                return
                    _runtime.State == snapshot.State;
            }

            public bool TryGetApplicationPauseSnapshot(
                out PauseSnapshot snapshot)
            {
                snapshot =
                    _runtime.Snapshot;

                return snapshot.IsValid;
            }

            private void ApplyApplicationState()
            {
                Time.timeScale =
                    _runtime.State ==
                        PauseState.Paused
                        ? 0f
                        : _runningTimeScale;
            }
        }
    }
}

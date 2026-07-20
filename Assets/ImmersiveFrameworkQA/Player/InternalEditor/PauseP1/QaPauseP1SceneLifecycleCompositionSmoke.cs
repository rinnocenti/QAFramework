using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using PauseState = Immersive.Framework.Pause.PauseState;

namespace ImmersiveFrameworkQA.PauseP1.Editor
{
    public static class QaPauseP1SceneLifecycleCompositionSmoke
    {
        private const string MenuPath = "Immersive Framework/QA/Player/Pause P1/Run Scene Lifecycle Composition Smoke";
        private const string LogPrefix = "[QA][PAUSE-P1][SCENE-LIFECYCLE]";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying && !EditorApplication.isCompiling;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();
            Scene loadedScene = default;
            Scene foreignScene = default;

            try
            {
                Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(QaPauseP1Paths.ScenePath) != null,
                    "QA Pause P1 scene is missing. Run Setup or Rebuild.");

                loadedScene = await LoadSceneAsync(QaPauseP1Paths.ScenePath);
                GameObject[] exactRoots = loadedScene.GetRootGameObjects();
                PausePlayerInputBinding binding = FindExactlyOne<PausePlayerInputBinding>(exactRoots);
                PlayerInput playerInput = FindExactlyOne<PlayerInput>(exactRoots);
                var runtime = new PauseProductBindingRuntimeContext(new QaLifecycleApplicationPort());
                var lifecycle = new PauseProductBindingSceneLifecycleParticipant(runtime);

                foreignScene = SceneManager.CreateScene("QA Pause P1 Foreign Lifecycle Scope");
                GameObject foreignPlayer = UnityEngine.Object.Instantiate(playerInput.gameObject);
                foreignPlayer.name = "QA Pause P1 Foreign PlayerInput";
                SceneManager.MoveGameObjectToScene(foreignPlayer, foreignScene);
                PausePlayerInputBinding foreignBinding = foreignPlayer.GetComponent<PausePlayerInputBinding>();
                Require(foreignBinding != null, "Foreign scope clone has no PausePlayerInputBinding.");

                Require(lifecycle.OnSceneAvailable(loadedScene, exactRoots, out string bindDiagnostic), bindDiagnostic);
                Require(binding.HasActiveBinding && runtime.State == PauseProductBindingState.Bound &&
                    runtime.ActiveToken.IsValid && !foreignBinding.HasActiveBinding,
                    "Scene lifecycle did not compose only the supplied scene roots.");
                completed.Add("exact-scene-roots-only");
                completed.Add("binding-registered-token-valid");

                PauseProductBindingToken activeToken = runtime.ActiveToken;
                PauseProductBindingToken foreignToken = new PauseProductBindingToken(
                    activeToken.Generation + 1,
                    activeToken.PlayerInstanceId);
                Require(!runtime.ReleaseBinding(foreignToken, "qa-pause-p1-foreign-token", out string foreignDiagnostic) &&
                    foreignDiagnostic.Contains("foreign or stale token") && binding.HasActiveBinding,
                    "Foreign token release changed the active binding.");
                completed.Add("foreign-token-rejected");

                GameObject secondPlayer = UnityEngine.Object.Instantiate(playerInput.gameObject);
                secondPlayer.name = "QA Pause P1 Second Binding";
                SceneManager.MoveGameObjectToScene(secondPlayer, loadedScene);
                PausePlayerInputBinding secondBinding = secondPlayer.GetComponent<PausePlayerInputBinding>();
                Require(secondBinding != null, "Second binding clone is missing.");
                Require(!secondBinding.TryInjectBindingPort(runtime, out string secondDiagnostic) &&
                    secondDiagnostic.Contains("another PlayerInput binding", StringComparison.OrdinalIgnoreCase) &&
                    binding.HasActiveBinding,
                    "Second scene-local binding was not rejected without preserving the first.");
                completed.Add("second-binding-rejected");

                Require(lifecycle.OnSceneReleasing(loadedScene, exactRoots, "qa-pause-p1-scene-release", out string releaseDiagnostic),
                    releaseDiagnostic);
                Require(runtime.State == PauseProductBindingState.Unbound && !runtime.ActiveToken.IsValid &&
                    !binding.HasActiveBinding,
                    "Scene lifecycle release did not clear the binding before unload.");
                completed.Add("release-before-scene-exit");

                Require(!runtime.ReleaseBinding(activeToken, "qa-pause-p1-stale-token", out string staleDiagnostic) &&
                    staleDiagnostic.Contains("foreign or stale token") && !binding.HasActiveBinding,
                    "Stale token release retained a reference after lifecycle release.");
                completed.Add("stale-token-and-references-cleared");

                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' completed='{string.Join(",", completed)}' message='{Escape(exception.Message)}'.");
                throw;
            }
            finally
            {
                if (foreignScene.IsValid() && foreignScene.isLoaded)
                {
                    await UnloadSceneAsync(foreignScene);
                }

                if (loadedScene.IsValid() && loadedScene.isLoaded)
                {
                    await UnloadSceneAsync(loadedScene);
                }
            }
        }

        private static async Task<Scene> LoadSceneAsync(string path)
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
            Require(operation != null, $"Unable to start loading scene '{path}'.");
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            Scene scene = SceneManager.GetSceneByPath(path);
            Require(scene.IsValid() && scene.isLoaded, $"Scene '{path}' did not load.");
            return scene;
        }

        private static async Task UnloadSceneAsync(Scene scene)
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            if (operation == null)
            {
                return;
            }

            while (!operation.isDone)
            {
                await Task.Yield();
            }
        }

        private static T FindExactlyOne<T>(IReadOnlyList<GameObject> roots) where T : Component
        {
            T[] matches = roots.Where(root => root != null)
                .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
            Require(matches.Length == 1, $"Expected exactly one {typeof(T).Name}; found {matches.Length}.");
            return matches[0];
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value)
            ? string.Empty : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");

        private sealed class QaLifecycleApplicationPort : IPauseProductApplicationPort
        {
            private readonly PauseRuntime runtime = new PauseRuntime();

            public bool TryApplyProductPause(PauseRequest request, out PauseResult result, out string diagnostic)
            {
                result = runtime.Request(request);
                diagnostic = result.Message;
                return result.Completed;
            }

            public bool TryRestorePauseSnapshot(PauseSnapshot snapshot, string reason, out string diagnostic)
            {
                if (!snapshot.IsValid)
                {
                    diagnostic = "Restore requires a valid Pause snapshot.";
                    return false;
                }

                if (runtime.State != snapshot.State)
                {
                    PauseRequest request = snapshot.State == PauseState.Paused
                        ? PauseRequest.Pause("qa.pause.p1.lifecycle.restore.paused", nameof(QaLifecycleApplicationPort), reason)
                        : PauseRequest.Resume("qa.pause.p1.lifecycle.restore.running", nameof(QaLifecycleApplicationPort), reason);
                    runtime.Request(request);
                }

                diagnostic = $"QA lifecycle Pause restored '{runtime.State}'.";
                return runtime.State == snapshot.State;
            }

            public bool TryGetApplicationPauseSnapshot(out PauseSnapshot snapshot)
            {
                snapshot = runtime.Snapshot;
                return snapshot.IsValid;
            }
        }
    }
}

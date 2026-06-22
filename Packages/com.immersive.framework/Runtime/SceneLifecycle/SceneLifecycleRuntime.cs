using System;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using UnityEngine;
using UnityEngine.SceneManagement;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Minimal owner for scene lifecycle operations.
    /// It resolves, loads and activates the Startup Route primary scene when requested by Game Flow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Runtime implementation detail; not game-facing API.")]
    internal sealed class SceneLifecycleRuntime
    {
        private const string AlreadyLoadedMode = "AlreadyLoaded";
        private const string SingleLoadMode = "Single";
        private const string AdditiveLoadMode = "Additive";

        internal async Task<SceneLifecycleLoadResult> LoadPrimarySceneAsync(RouteAsset route)
        {
            if (route == null)
            {
                return SceneLifecycleLoadResult.Failed("Route is missing.");
            }

            if (!route.HasPrimaryScene)
            {
                return SceneLifecycleLoadResult.Failed("Route Primary Scene is missing.");
            }

            string sceneName = route.PrimarySceneName;
            string scenePath = route.PrimaryScenePath;
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return SceneLifecycleLoadResult.Failed("Route Primary Scene name is empty.");
            }

            var activeScene = SceneManager.GetActiveScene();
            if (IsSceneMatch(activeScene, scenePath, sceneName) && activeScene.isLoaded)
            {
                return SceneLifecycleLoadResult.LoadedPrimaryScene(sceneName, scenePath, true, AlreadyLoadedMode);
            }

            var loadedScene = FindLoadedScene(scenePath, sceneName);
            bool alreadyLoaded = loadedScene.IsValid() && loadedScene.isLoaded;
            string loadMode = AlreadyLoadedMode;

            if (!alreadyLoaded)
            {
                var loadResult = await TryLoadSceneSingleAsync(scenePath, sceneName);
                if (!loadResult.Loaded)
                {
                    return loadResult;
                }

                loadMode = SingleLoadMode;
                loadedScene = FindLoadedScene(scenePath, sceneName);
            }

            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle could not resolve loaded Primary Scene '{sceneName}' after load.");
            }

            if (!IsSceneMatch(SceneManager.GetActiveScene(), scenePath, sceneName))
            {
                if (!SceneManager.SetActiveScene(loadedScene))
                {
                    var currentActiveScene = SceneManager.GetActiveScene();
                    if (!IsSceneMatch(currentActiveScene, scenePath, sceneName) || !currentActiveScene.isLoaded)
                    {
                        return SceneLifecycleLoadResult.Failed(
                            $"Scene Lifecycle failed to set Primary Scene '{sceneName}' as active.");
                    }
                }
            }

            return SceneLifecycleLoadResult.LoadedPrimaryScene(sceneName, scenePath, alreadyLoaded, loadMode);
        }


        internal async Task<SceneLifecycleLoadResult> LoadAdditiveSceneAsync(string sceneName, string scenePath)
        {
            sceneName = Normalize(sceneName);
            scenePath = Normalize(scenePath);
            if (string.IsNullOrWhiteSpace(sceneName) && string.IsNullOrWhiteSpace(scenePath))
            {
                return SceneLifecycleLoadResult.Failed("Scene Lifecycle cannot load Additive Scene because scene name and path are empty.");
            }

            var loadedScene = FindLoadedScene(scenePath, sceneName);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                return SceneLifecycleLoadResult.LoadedAdditiveScene(
                    GetSceneNameForDiagnostics(loadedScene, sceneName),
                    GetScenePathForDiagnostics(loadedScene, scenePath),
                    true,
                    AlreadyLoadedMode);
            }

            var loadResult = await TryLoadSceneAdditiveAsync(scenePath, sceneName);
            if (!loadResult.Loaded)
            {
                return loadResult;
            }

            loadedScene = FindLoadedScene(scenePath, sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle could not resolve loaded Additive Scene '{ResolveSceneLabel(scenePath, sceneName)}' after load.");
            }

            return SceneLifecycleLoadResult.LoadedAdditiveScene(
                GetSceneNameForDiagnostics(loadedScene, sceneName),
                GetScenePathForDiagnostics(loadedScene, scenePath),
                false,
                AdditiveLoadMode);
        }


        internal async Task<SceneLifecycleUnloadResult> UnloadSceneAsync(string sceneName, string scenePath)
        {
            sceneName = Normalize(sceneName);
            scenePath = Normalize(scenePath);
            if (string.IsNullOrWhiteSpace(sceneName) && string.IsNullOrWhiteSpace(scenePath))
            {
                return SceneLifecycleUnloadResult.Failed("Scene Lifecycle cannot unload Scene because scene name and path are empty.");
            }

            var loadedScene = FindLoadedScene(scenePath, sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return SceneLifecycleUnloadResult.SkippedScene(
                    sceneName,
                    scenePath,
                    $"Scene Lifecycle skipped unload for Scene '{ResolveSceneLabel(scenePath, sceneName)}' because it is not loaded.");
            }

            if (IsSceneMatch(SceneManager.GetActiveScene(), scenePath, sceneName))
            {
                return SceneLifecycleUnloadResult.Failed(
                    $"Scene Lifecycle cannot unload active Scene '{ResolveSceneLabel(scenePath, sceneName)}'. Active Primary Scene is controlled by Single load.");
            }

            try
            {
                var operation = SceneManager.UnloadSceneAsync(loadedScene);
                if (operation == null)
                {
                    return SceneLifecycleUnloadResult.Failed(
                        $"Scene Lifecycle failed to start unloading Scene '{ResolveSceneLabel(scenePath, sceneName)}'.");
                }

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                var remainingScene = FindLoadedScene(scenePath, sceneName);
                if (remainingScene.IsValid() && remainingScene.isLoaded)
                {
                    return SceneLifecycleUnloadResult.Failed(
                        $"Scene Lifecycle could not confirm Scene '{ResolveSceneLabel(scenePath, sceneName)}' was unloaded.");
                }

                return SceneLifecycleUnloadResult.UnloadedScene(
                    GetSceneNameForDiagnostics(loadedScene, sceneName),
                    GetScenePathForDiagnostics(loadedScene, scenePath));
            }
            catch (Exception exception)
            {
                return SceneLifecycleUnloadResult.Failed(
                    $"Scene Lifecycle failed to unload Scene '{ResolveSceneLabel(scenePath, sceneName)}'. {exception.GetType().Name}: {exception.Message}");
            }
        }

        private static Task<SceneLifecycleLoadResult> TryLoadSceneSingleAsync(string scenePath, string sceneName)
        {
            return TryLoadSceneAsync(
                scenePath,
                sceneName,
                LoadSceneMode.Single,
                "Primary Scene",
                SingleLoadMode,
                SceneLifecycleLoadResult.LoadedPrimaryScene);
        }

        private static Task<SceneLifecycleLoadResult> TryLoadSceneAdditiveAsync(string scenePath, string sceneName)
        {
            return TryLoadSceneAsync(
                scenePath,
                sceneName,
                LoadSceneMode.Additive,
                "Additive Scene",
                AdditiveLoadMode,
                SceneLifecycleLoadResult.LoadedAdditiveScene);
        }

        private static async Task<SceneLifecycleLoadResult> TryLoadSceneAsync(
            string scenePath,
            string sceneName,
            LoadSceneMode sceneLoadMode,
            string sceneRoleLabel,
            string resultLoadMode,
            Func<string, string, bool, string, SceneLifecycleLoadResult> createLoadedResult)
        {
            var sceneLabel = ResolveSceneLabel(scenePath, sceneName);
            if (!TryGetLoadSceneIdentifier(scenePath, sceneName, out string sceneIdentifier))
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle cannot load {sceneRoleLabel} '{sceneLabel}'. Add it to the active Build Profile or Shared Scene List.");
            }

            try
            {
                var operation = SceneManager.LoadSceneAsync(sceneIdentifier, sceneLoadMode);
                if (operation == null)
                {
                    return SceneLifecycleLoadResult.Failed(
                        $"Scene Lifecycle failed to start loading {sceneRoleLabel} '{sceneLabel}'. Make sure the scene is included in Build Settings.");
                }

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                return createLoadedResult(sceneName, scenePath, false, resultLoadMode);
            }
            catch (Exception exception)
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle failed to load {sceneRoleLabel} '{sceneLabel}'. {exception.GetType().Name}: {exception.Message}");
            }
        }

        private static bool TryGetLoadSceneIdentifier(string scenePath, string sceneName, out string sceneIdentifier)
        {
            if (!string.IsNullOrWhiteSpace(scenePath) && Application.CanStreamedLevelBeLoaded(scenePath))
            {
                sceneIdentifier = scenePath;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(sceneName) && Application.CanStreamedLevelBeLoaded(sceneName))
            {
                sceneIdentifier = sceneName;
                return true;
            }

            sceneIdentifier = null;
            return false;
        }

        private static Scene FindLoadedScene(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                var sceneByPath = SceneManager.GetSceneByPath(scenePath);
                if (sceneByPath.IsValid() && sceneByPath.isLoaded)
                {
                    return sceneByPath;
                }
            }

            return SceneManager.GetSceneByName(sceneName);
        }

        private static string GetSceneNameForDiagnostics(Scene scene, string fallbackSceneName)
        {
            if (scene.IsValid() && !string.IsNullOrWhiteSpace(scene.name))
            {
                return scene.name;
            }

            return Normalize(fallbackSceneName);
        }

        private static string GetScenePathForDiagnostics(Scene scene, string fallbackScenePath)
        {
            if (scene.IsValid() && !string.IsNullOrWhiteSpace(scene.path))
            {
                return scene.path;
            }

            return Normalize(fallbackScenePath);
        }

        private static string ResolveSceneLabel(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                return sceneName;
            }

            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return scenePath;
            }

            return "<missing>";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool IsSceneMatch(Scene scene, string scenePath, string sceneName)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(scenePath) && string.Equals(scene.path, scenePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(sceneName) && string.Equals(scene.name, sceneName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

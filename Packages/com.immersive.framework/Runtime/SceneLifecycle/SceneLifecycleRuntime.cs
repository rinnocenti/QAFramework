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

        private static async Task<SceneLifecycleLoadResult> TryLoadSceneSingleAsync(string scenePath, string sceneName)
        {
            if (!TryGetLoadSceneIdentifier(scenePath, sceneName, out string sceneIdentifier))
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle cannot load Primary Scene '{sceneName}'. Add it to the active Build Profile or Shared Scene List.");
            }

            try
            {
                var operation = SceneManager.LoadSceneAsync(sceneIdentifier, LoadSceneMode.Single);
                if (operation == null)
                {
                    return SceneLifecycleLoadResult.Failed(
                        $"Scene Lifecycle failed to start loading Primary Scene '{sceneName}'. Make sure the scene is included in Build Settings.");
                }

                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                return SceneLifecycleLoadResult.LoadedPrimaryScene(sceneName, scenePath, false, SingleLoadMode);
            }
            catch (Exception exception)
            {
                return SceneLifecycleLoadResult.Failed(
                    $"Scene Lifecycle failed to load Primary Scene '{sceneName}'. {exception.GetType().Name}: {exception.Message}");
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

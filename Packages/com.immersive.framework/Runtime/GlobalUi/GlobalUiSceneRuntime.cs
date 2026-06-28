using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Loading;
using Immersive.Framework.Pause;
using Immersive.Framework.TransitionEffects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.GlobalUi
{
    /// <summary>
    /// API status: Internal. Loads the canonical app/session-scoped UIGlobal scene before route startup,
    /// moves its authored UI roots under the persistent FrameworkRuntimeHost, and exposes visual adapters.
    /// It does not own RouteLifecycle, ActivityFlow, SceneLifecycle, Loading lifecycle or Transition lifecycle.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24E canonical UIGlobal scene loader; scene-authored visual surfaces only.")]
    internal sealed class GlobalUiSceneRuntime
    {
        private readonly List<ITransitionEffectAdapter> _transitionAdapters;
        private readonly List<ILoadingSurfaceAdapter> _loadingAdapters;
        private readonly List<IPauseSurfaceAdapter> _pauseAdapters;

        private GlobalUiSceneRuntime(
            GlobalUiScenePolicy policy,
            string scenePath,
            string sceneName,
            string label,
            IReadOnlyList<GameObject> persistedRoots,
            IReadOnlyList<ITransitionEffectAdapter> transitionAdapters,
            IReadOnlyList<ILoadingSurfaceAdapter> loadingAdapters,
            IReadOnlyList<IPauseSurfaceAdapter> pauseAdapters,
            bool hasBlockingConfigurationIssue,
            string blockingConfigurationMessage,
            string message)
        {
            Policy = policy;
            ScenePath = Normalize(scenePath);
            SceneName = Normalize(sceneName);
            Label = string.IsNullOrWhiteSpace(label) ? "UIGlobal" : label.Trim();
            PersistedRootCount = persistedRoots != null ? persistedRoots.Count : 0;
            _transitionAdapters = Copy(transitionAdapters);
            _loadingAdapters = Copy(loadingAdapters);
            _pauseAdapters = Copy(pauseAdapters);
            HasBlockingConfigurationIssue = hasBlockingConfigurationIssue;
            BlockingConfigurationMessage = Normalize(blockingConfigurationMessage);
            Message = Normalize(message);
        }

        public GlobalUiScenePolicy Policy { get; }
        public string ScenePath { get; }
        public string SceneName { get; }
        public string Label { get; }
        public int PersistedRootCount { get; }
        public int TransitionAdapterCount => _transitionAdapters.Count;
        public int LoadingAdapterCount => _loadingAdapters.Count;
        public int PauseAdapterCount => _pauseAdapters.Count;
        public bool HasBlockingConfigurationIssue { get; }
        public string BlockingConfigurationMessage { get; }
        public string Message { get; }
        public IReadOnlyList<ITransitionEffectAdapter> TransitionAdapters => _transitionAdapters;
        public IReadOnlyList<ILoadingSurfaceAdapter> LoadingAdapters => _loadingAdapters;
        public IReadOnlyList<IPauseSurfaceAdapter> PauseAdapters => _pauseAdapters;

        internal static async Awaitable<GlobalUiSceneRuntime> LoadAndPersistAsync(
            GameApplicationAsset application,
            Transform persistentParent,
            FrameworkLogger logger)
        {
            logger ??= FrameworkLogger.Create<GlobalUiSceneRuntime>();

            if (application == null)
            {
                const string message = "UIGlobal scene is not configured because the Game Application is missing.";
                logger.Warning(message);
                return new GlobalUiSceneRuntime(
                    GlobalUiScenePolicy.NoneConfigured,
                    string.Empty,
                    string.Empty,
                    "UIGlobal",
                    Array.Empty<GameObject>(),
                    Array.Empty<ITransitionEffectAdapter>(),
                    Array.Empty<ILoadingSurfaceAdapter>(),
                    Array.Empty<IPauseSurfaceAdapter>(),
                    false,
                    string.Empty,
                    message);
            }

            if (application.GlobalUiScenePolicyValue == GlobalUiScenePolicy.NoneConfigured)
            {
                if (application.HasGlobalUiScene)
                {
                    logger.Warning($"UIGlobal scene '{application.GlobalUiScenePath}' is assigned but Global UI Scene Policy is NoneConfigured. The scene will not be loaded and the runtime will keep explicit NoOp behavior.");
                }
                else
                {
                    logger.Info("UIGlobal scene is not configured. The runtime will keep explicit NoOp behavior.");
                }

                return new GlobalUiSceneRuntime(
                    application.GlobalUiScenePolicyValue,
                    application.GlobalUiScenePath,
                    application.GlobalUiSceneName,
                    "UIGlobal",
                    Array.Empty<GameObject>(),
                    Array.Empty<ITransitionEffectAdapter>(),
                    Array.Empty<ILoadingSurfaceAdapter>(),
                    Array.Empty<IPauseSurfaceAdapter>(),
                    false,
                    string.Empty,
                    "UIGlobal scene is explicit NoOp.");
            }

            if (!application.HasGlobalUiScene)
            {
                const string message = "Global UI Scene Policy is Required, but UIGlobal Scene is missing.";
                logger.Error(message);
                return Failed(application, message);
            }

            var scenePath = application.GlobalUiScenePath;
            var asyncOperation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            if (asyncOperation == null)
            {
                var message = $"UIGlobal scene '{scenePath}' could not be loaded. Ensure it is in Build Settings.";
                logger.Error(message);
                return Failed(application, message);
            }

            while (!asyncOperation.isDone)
            {
                await Awaitable.NextFrameAsync();
            }

            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid())
            {
                scene = SceneManager.GetSceneByName(application.GlobalUiSceneName);
            }

            if (!scene.IsValid() || !scene.isLoaded)
            {
                var message = $"UIGlobal scene '{scenePath}' finished loading but could not be resolved as a loaded scene.";
                logger.Error(message);
                return Failed(application, message);
            }

            var roots = scene.GetRootGameObjects();
            var persistedRoots = new List<GameObject>();
            if (roots != null)
            {
                for (var i = 0; i < roots.Length; i++)
                {
                    var root = roots[i];
                    if (root == null)
                    {
                        continue;
                    }

                    root.name = root.name.EndsWith(" (UIGlobal)", StringComparison.Ordinal)
                        ? root.name
                        : root.name + " (UIGlobal)";
                    root.transform.SetParent(persistentParent, false);
                    persistedRoots.Add(root);
                }
            }

            var transitionAdapters = CollectAdapters<ITransitionEffectAdapter>(persistedRoots);
            var loadingAdapters = CollectAdapters<ILoadingSurfaceAdapter>(persistedRoots);
            var pauseAdapters = CollectAdapters<IPauseSurfaceAdapter>(persistedRoots);
            var blockingMessage = BuildBlockingMessageIfRequired(
                application.GlobalUiScenePolicyValue,
                application.GlobalUiSceneName,
                transitionAdapters.Count,
                loadingAdapters.Count);

            var unloadOperation = SceneManager.UnloadSceneAsync(scene);
            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                {
                    await Awaitable.NextFrameAsync();
                }
            }

            var label = string.IsNullOrWhiteSpace(application.GlobalUiSceneName)
                ? "UIGlobal"
                : application.GlobalUiSceneName;
            if (!string.IsNullOrWhiteSpace(blockingMessage))
            {
                logger.Error($"UIGlobal scene '{label}' loaded and persisted, but required adapters are missing. {blockingMessage}");
                return new GlobalUiSceneRuntime(
                    application.GlobalUiScenePolicyValue,
                    application.GlobalUiScenePath,
                    application.GlobalUiSceneName,
                    label,
                    persistedRoots,
                    transitionAdapters,
                    loadingAdapters,
                    pauseAdapters,
                    true,
                    blockingMessage,
                    $"UIGlobal scene '{label}' loaded and persisted with rootCount='{persistedRoots.Count}' transitionAdapterCount='{transitionAdapters.Count}' loadingAdapterCount='{loadingAdapters.Count}' pauseAdapterCount='{pauseAdapters.Count}'.");
            }

            logger.Info($"UIGlobal scene '{label}' loaded and persisted with rootCount='{persistedRoots.Count}' transitionAdapterCount='{transitionAdapters.Count}' loadingAdapterCount='{loadingAdapters.Count}' pauseAdapterCount='{pauseAdapters.Count}'.");

            return new GlobalUiSceneRuntime(
                application.GlobalUiScenePolicyValue,
                application.GlobalUiScenePath,
                application.GlobalUiSceneName,
                label,
                persistedRoots,
                transitionAdapters,
                loadingAdapters,
                pauseAdapters,
                false,
                string.Empty,
                "UIGlobal scene loaded and persisted.");
        }

        private static GlobalUiSceneRuntime Failed(GameApplicationAsset application, string message)
        {
            return new GlobalUiSceneRuntime(
                application != null ? application.GlobalUiScenePolicyValue : GlobalUiScenePolicy.NoneConfigured,
                application != null ? application.GlobalUiScenePath : string.Empty,
                application != null ? application.GlobalUiSceneName : string.Empty,
                application != null ? application.GlobalUiSceneName : "UIGlobal",
                Array.Empty<GameObject>(),
                Array.Empty<ITransitionEffectAdapter>(),
                Array.Empty<ILoadingSurfaceAdapter>(),
                Array.Empty<IPauseSurfaceAdapter>(),
                true,
                message,
                message);
        }

        private static List<TAdapter> CollectAdapters<TAdapter>(IReadOnlyList<GameObject> roots)
        {
            var adapters = new List<TAdapter>();
            if (roots == null || roots.Count == 0)
            {
                return adapters;
            }

            for (var i = 0; i < roots.Count; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                if (behaviours == null)
                {
                    continue;
                }

                for (var j = 0; j < behaviours.Length; j++)
                {
                    if (behaviours[j] is TAdapter adapter)
                    {
                        adapters.Add(adapter);
                    }
                }
            }

            return adapters;
        }

        private static List<T> Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null || source.Count == 0)
            {
                return new List<T>();
            }

            return new List<T>(source);
        }

        private static string BuildBlockingMessageIfRequired(
            GlobalUiScenePolicy policy,
            string label,
            int transitionAdapterCount,
            int loadingAdapterCount)
        {
            if (policy != GlobalUiScenePolicy.Required)
            {
                return string.Empty;
            }

            var missing = new List<string>(2);
            if (transitionAdapterCount == 0)
            {
                missing.Add("Transition adapter");
            }

            if (loadingAdapterCount == 0)
            {
                missing.Add("Loading adapter");
            }

            if (missing.Count == 0)
            {
                return string.Empty;
            }

            return $"UIGlobal scene '{label}' is required, but missing {string.Join(" and ", missing)}.";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Framework.SceneLifecycle
{
    /// <summary>
    /// Scene-scoped component lookup for loaded authored content.
    /// This is not a registry, service locator, materializer, loader, or lifecycle owner.
    /// It only enumerates roots from already-loaded Unity scenes selected by an explicit Route scene scope.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Loaded scene component query used to keep runtime content discovery scoped before F6 scene composition.")]
    internal static class SceneScopedComponentQuery
    {
        public static IReadOnlyList<T> GetComponentsInRoutePrimaryScene<T>(RouteAsset route)
            where T : Component
        {
            if (!TryGetLoadedPrimaryScene(route, out var scene))
            {
                return Array.Empty<T>();
            }

            return GetComponentsInScene<T>(scene);
        }

        public static IReadOnlyList<T> GetComponentsInLoadedScenes<T>()
            where T : Component
        {
            var components = new List<T>();
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                AddComponentsInScene(scene, components);
            }

            return components;
        }

        public static bool TryGetLoadedPrimaryScene(RouteAsset route, out Scene scene)
        {
            scene = default;

            if (route == null || !route.HasPrimaryScene)
            {
                return false;
            }

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var candidate = SceneManager.GetSceneAt(i);
                if (!candidate.IsValid() || !candidate.isLoaded)
                {
                    continue;
                }

                if (MatchesRoutePrimaryScene(candidate, route))
                {
                    scene = candidate;
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<T> GetComponentsInScene<T>(Scene scene)
            where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Array.Empty<T>();
            }

            var components = new List<T>();
            AddComponentsInScene(scene, components);
            return components;
        }

        private static void AddComponentsInScene<T>(Scene scene, List<T> components)
            where T : Component
        {
            if (components == null || !scene.IsValid() || !scene.isLoaded)
            {
                return;
            }

            var roots = scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                return;
            }

            for (int i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root == null)
                {
                    continue;
                }

                var found = root.GetComponentsInChildren<T>(true);
                if (found == null || found.Length == 0)
                {
                    continue;
                }

                for (int j = 0; j < found.Length; j++)
                {
                    if (found[j] != null)
                    {
                        components.Add(found[j]);
                    }
                }
            }
        }

        private static bool MatchesRoutePrimaryScene(Scene scene, RouteAsset route)
        {
            if (route == null || !scene.IsValid())
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(route.PrimaryScenePath)
                && string.Equals(scene.path, route.PrimaryScenePath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(route.PrimarySceneName)
                && string.Equals(scene.name, route.PrimarySceneName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

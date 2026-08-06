using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.SceneLifecycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaRouteOwnedSceneDiscoveryRegression
    {
        private const string MenuPath = "Immersive Framework/QA/Regressions/Lifecycle/Run Route-Owned Scene Discovery Regression";
        private const string LogPrefix = "[QA_ROUTE_OWNED_SCENE_DISCOVERY]";
        private const string TemporaryRoot = "Assets/ImmersiveFrameworkQA/__RouteOwnedSceneDiscoveryTemp";
        private const string RouteAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteA.asset";
        private const string RouteBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Routes/QA_LifecycleRouteB.asset";
        private const string ActivityAPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityA.asset";
        private const string ActivityBPath = "Assets/ImmersiveFrameworkQA/Lifecycle/Activities/QA_LifecycleActivityB.asset";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"{LogPrefix} status='QA Not Started / Play Mode'.");
                return;
            }

            if (!TryValidateEditorWorkspace(out string workspaceIssue))
            {
                Debug.LogWarning($"{LogPrefix} status='QA Not Started / Unsaved Editor Workspace' message='{Escape(workspaceIssue)}'.");
                return;
            }

            SceneSetup[] initialSetup = EditorSceneManager.GetSceneManagerSetup();
            var completed = new List<string>();
            Exception primaryFailure = null;
            Exception cleanupFailure = null;
            try
            {
                RouteAsset routeA = LoadRequiredAsset<RouteAsset>(RouteAPath);
                RouteAsset routeB = LoadRequiredAsset<RouteAsset>(RouteBPath);
                LoadRequiredAsset<ActivityAsset>(ActivityAPath);
                LoadRequiredAsset<ActivityAsset>(ActivityBPath);

                CreateTemporaryRoot();
                string templatePath = CreateTemplateScene();
                string primaryPath = CopyTemporaryScene(templatePath, "Primary/Primary.unity");
                string additionalPath = CopyTemporaryScene(templatePath, "Additional/Additional.unity");
                string differentRoutePath = CopyTemporaryScene(templatePath, "DifferentRoute/DifferentRoute.unity");
                string persistentPath = CopyTemporaryScene(templatePath, "Persistent/Persistent.unity");
                string externalPath = CopyTemporaryScene(templatePath, "External/External.unity");
                string sameNameAPath = CopyTemporaryScene(templatePath, "PathA/Shared.unity");
                string sameNameBPath = CopyTemporaryScene(templatePath, "PathB/Shared.unity");
                string divergentPath = CopyTemporaryScene(templatePath, "Divergent/Primary.unity");
                AssetDatabase.Refresh();

                Scene primary = EditorSceneManager.OpenScene(primaryPath, OpenSceneMode.Single);
                Scene additional = EditorSceneManager.OpenScene(additionalPath, OpenSceneMode.Additive);
                Scene differentRoute = EditorSceneManager.OpenScene(differentRoutePath, OpenSceneMode.Additive);
                Scene persistent = EditorSceneManager.OpenScene(persistentPath, OpenSceneMode.Additive);
                Scene external = EditorSceneManager.OpenScene(externalPath, OpenSceneMode.Additive);
                Scene sameNameA = EditorSceneManager.OpenScene(sameNameAPath, OpenSceneMode.Additive);
                Scene sameNameB = EditorSceneManager.OpenScene(sameNameBPath, OpenSceneMode.Additive);
                EditorSceneManager.OpenScene(divergentPath, OpenSceneMode.Additive);

                RouteContentBinding primaryBinding = CreateBinding(primary, "Primary Binding", routeA);
                RouteContentBinding additionalFirstBinding = CreateBinding(additional, "Additional Binding A", routeA);
                RouteContentBinding additionalSecondBinding = CreateBinding(additional, "Additional Binding B", routeA);
                RouteContentBinding differentRouteBinding = CreateBinding(differentRoute, "Different Route Binding", routeB);
                RouteContentBinding persistentBinding = CreateBinding(persistent, "Persistent Binding", routeA);
                RouteContentBinding externalBinding = CreateBinding(external, "External Binding", routeA);
                RouteContentBinding sameNameABinding = CreateBinding(sameNameA, "Path A Binding", routeA);
                RouteContentBinding sameNameBBinding = CreateBinding(sameNameB, "Path B Binding", routeA);
                SaveTemporaryScenes();

                RouteContentDiscoveryScope scope = RouteContentDiscoveryScope.FromCompositionResult(
                    CreateCompositionResult(routeA, primary, additional, differentRoute, sameNameA, sameNameB));
                Require(scope.RouteOwnedScenes.Count == 5, "Composition-result scope did not remove the repeated Route-owned scene.");
                Require(scope.RouteOwnedScenes[0].Source == RouteContentDiscoverySceneSource.Primary &&
                        scope.RouteOwnedScenes[1].Source == RouteContentDiscoverySceneSource.Additional,
                    "Primary Route scene was not kept before additional Route scenes.");
                completed.Add("from-composition-result-primary-first-and-duplicate-removed");

                IReadOnlyList<RouteContentBinding> bindings = SceneScopedComponentQuery.GetComponentsInRouteContentScope<RouteContentBinding>(scope);
                Require(bindings.Count == 6 && Contains(bindings, primaryBinding) &&
                        Contains(bindings, additionalFirstBinding) && Contains(bindings, additionalSecondBinding) &&
                        Contains(bindings, differentRouteBinding) && Contains(bindings, sameNameABinding) && Contains(bindings, sameNameBBinding) &&
                        !Contains(bindings, persistentBinding) && !Contains(bindings, externalBinding),
                    "Route-owned scene scope did not preserve owned components or excluded persistent/external scenes.");
                completed.Add("persistent-external-excluded-and-distinct-components-preserved");

                Require(SceneScopedComponentQuery.GetComponentsInRouteContentScope<RouteContentBinding>(
                            RouteContentDiscoveryScope.FromCompositionResult(CreateDivergentPathResult(routeA, primary, divergentPath))).Count == 0,
                    "A present divergent scene path must not fall back to a matching scene name.");
                completed.Add("divergent-path-has-no-name-fallback");
                Debug.Log($"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                primaryFailure = exception;
                Debug.LogError($"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.");
            }
            finally
            {
                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(initialSetup);
                    CloseRemainingTemporaryScenes();
                    if (AssetDatabase.IsValidFolder(TemporaryRoot)) AssetDatabase.DeleteAsset(TemporaryRoot);
                    AssetDatabase.Refresh();
                }
                catch (Exception exception)
                {
                    cleanupFailure = exception;
                    Debug.LogError($"{LogPrefix} status='Cleanup Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                }
            }

            if (primaryFailure != null) throw primaryFailure;
            if (cleanupFailure != null) throw cleanupFailure;
        }

        private static RouteSceneCompositionResult CreateCompositionResult(RouteAsset route, params Scene[] scenes)
        {
            RouteSceneCompositionPlanEntry primary = CreatePlanEntry(scenes[0], RouteSceneRole.Primary, 0, "primary");
            RouteSceneCompositionPlanEntry additional = CreatePlanEntry(scenes[1], RouteSceneRole.Additive, 1, "additional");
            RouteSceneCompositionPlanEntry differentRoute = CreatePlanEntry(scenes[2], RouteSceneRole.Additive, 2, "different-route");
            RouteSceneCompositionPlanEntry sameNameA = CreatePlanEntry(scenes[3], RouteSceneRole.Additive, 3, "same-name-a");
            RouteSceneCompositionPlanEntry sameNameB = CreatePlanEntry(scenes[4], RouteSceneRole.Additive, 4, "same-name-b");
            var plan = new RouteSceneCompositionPlan(route, null, "qa.route.owned", route.RouteName, primary,
                new[] { additional, differentRoute, sameNameA, sameNameB }, RouteSceneActiveScenePolicy.PrimarySceneActive, "QA", "synthetic-scope");
            return RouteSceneCompositionResult.ExecutedResult(plan, new[]
            {
                RouteSceneCompositionResultEntry.LoadedEntry(primary, false, true, string.Empty),
                RouteSceneCompositionResultEntry.LoadedEntry(additional, false, false, string.Empty),
                RouteSceneCompositionResultEntry.LoadedEntry(primary, true, true, "duplicate evidence"),
                RouteSceneCompositionResultEntry.LoadedEntry(differentRoute, false, false, string.Empty),
                RouteSceneCompositionResultEntry.LoadedEntry(sameNameA, false, false, string.Empty),
                RouteSceneCompositionResultEntry.LoadedEntry(sameNameB, false, false, string.Empty)
            }, SceneLifecycleLoadResult.LoadedPrimaryScene(primary.SceneName, primary.ScenePath, false, "Single"), "QA", "synthetic-scope");
        }

        private static RouteSceneCompositionResult CreateDivergentPathResult(RouteAsset route, Scene primary, string divergentPath)
        {
            var divergent = new RouteSceneCompositionPlanEntry(
                FrameworkContentIdentity.FromOwnerValue(FrameworkContentScope.Route, FrameworkContentKind.Scene, "qa.route.owned", "divergent"), "divergent",
                primary.name, divergentPath, RouteSceneRole.Primary, FrameworkContentRequiredness.Required, RouteContentOwnership.Owned, RouteSceneLoadMode.Single, 0, true);
            var plan = new RouteSceneCompositionPlan(route, null, "qa.route.owned", route.RouteName, divergent,
                Array.Empty<RouteSceneCompositionPlanEntry>(), RouteSceneActiveScenePolicy.PrimarySceneActive, "QA", "divergent-path");
            return RouteSceneCompositionResult.ExecutedResult(plan,
                new[] { RouteSceneCompositionResultEntry.LoadedEntry(divergent, false, true, string.Empty) },
                SceneLifecycleLoadResult.LoadedPrimaryScene(divergent.SceneName, divergent.ScenePath, false, "Single"), "QA", "divergent-path");
        }

        private static RouteSceneCompositionPlanEntry CreatePlanEntry(Scene scene, RouteSceneRole role, int order, string contentId)
        {
            return new RouteSceneCompositionPlanEntry(
                FrameworkContentIdentity.FromOwnerValue(FrameworkContentScope.Route, FrameworkContentKind.Scene, "qa.route.owned", contentId), contentId,
                scene.name, scene.path, role, FrameworkContentRequiredness.Required, RouteContentOwnership.Owned,
                role == RouteSceneRole.Primary ? RouteSceneLoadMode.Single : RouteSceneLoadMode.Additive, order, true);
        }

        private static RouteContentBinding CreateBinding(Scene scene, string name, RouteAsset route)
        {
            var gameObject = new GameObject(name);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            RouteContentBinding binding = gameObject.AddComponent<RouteContentBinding>();
            var serialized = new SerializedObject(binding);
            SerializedProperty property = serialized.FindProperty("route");
            Require(property != null, "RouteContentBinding route property was not found.");
            property.objectReferenceValue = route;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(binding);
            return binding;
        }

        private static void SaveTemporaryScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && scene.path.StartsWith(TemporaryRoot, StringComparison.Ordinal)) EditorSceneManager.SaveScene(scene);
            }
        }

        private static string CreateTemplateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            string path = TemporaryRoot + "/Template.unity";
            Require(EditorSceneManager.SaveScene(scene, path), "Could not save the temporary scene template.");
            return path;
        }

        private static void CreateTemporaryRoot()
        {
            if (AssetDatabase.IsValidFolder(TemporaryRoot)) AssetDatabase.DeleteAsset(TemporaryRoot);
            Require(AssetDatabase.CreateFolder("Assets/ImmersiveFrameworkQA", "__RouteOwnedSceneDiscoveryTemp").Length > 0, "Could not create the temporary QA folder.");
        }

        private static string CopyTemporaryScene(string templatePath, string relativePath)
        {
            string path = TemporaryRoot + "/" + relativePath;
            EnsureAssetFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            Require(AssetDatabase.CopyAsset(templatePath, path), $"Could not create temporary scene '{path}'.");
            return path;
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        private static void CloseRemainingTemporaryScenes()
        {
            for (int index = SceneManager.sceneCount - 1; index >= 0 && SceneManager.sceneCount > 1; index--)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && scene.path.StartsWith(TemporaryRoot, StringComparison.Ordinal)) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static bool TryValidateEditorWorkspace(out string issue)
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.isLoaded && (string.IsNullOrWhiteSpace(scene.path) || scene.isDirty))
                {
                    issue = $"Open scene '{scene.name}' must be saved and not dirty.";
                    return false;
                }
            }
            issue = string.Empty;
            return true;
        }

        private static T LoadRequiredAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(asset != null, $"Required canonical QA asset is missing: '{path}'.");
            return asset;
        }

        private static bool Contains<T>(IReadOnlyList<T> items, T expected) where T : class
        {
            for (int index = 0; index < items.Count; index++) if (ReferenceEquals(items[index], expected)) return true;
            return false;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) => string.IsNullOrEmpty(value) ? string.Empty : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
    }
}

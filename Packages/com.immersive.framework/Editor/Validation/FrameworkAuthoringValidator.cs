using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Validation
{
    internal static class FrameworkAuthoringValidator
    {
        internal static FrameworkAuthoringValidationReport ValidateProjectSettings(
            ImmersiveFrameworkSettingsAsset settings,
            bool includeOpenSceneBindings)
        {
            var validationMode = ResolveValidationMode(settings);
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (settings == null)
            {
                report.AddError(
                    "Framework Settings asset is missing. Open Project Settings > Immersive Framework to create it.",
                    null);
                return report;
            }

            report.AddInfo($"Validation Mode policy: {FrameworkValidationModePolicy.GetSummary(validationMode)}", settings);

            if (settings.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.CurrentSceneOnly)
            {
                report.AddInfo(
                    "Editor Play Mode Startup is Current Scene Only. Framework boot validation is skipped in Play Mode, but authoring assets can still be checked.",
                    settings);
            }

            if (settings.ActiveGameApplication == null)
            {
                report.AddError(
                    "Active Game Application is missing in Project Settings > Immersive Framework.",
                    settings);
            }
            else
            {
                report.AddRange(ValidateGameApplication(settings.ActiveGameApplication, true, validationMode));
            }

            if (includeOpenSceneBindings)
            {
                ValidateOpenSceneActivityLocalVisibilityAdapters(report, validationMode);
                ValidateOpenSceneRouteContentBindings(report, validationMode);
            }

            if (!report.HasIssues)
            {
                report.AddInfo("Authoring validation passed with no findings.", settings);
            }

            return report;
        }

        internal static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication,
            bool validateDependencies)
        {
            return ValidateGameApplication(gameApplication, validateDependencies, ResolveValidationMode(gameApplication));
        }

        internal static FrameworkAuthoringValidationReport ValidateRoute(RouteAsset route, bool validateDependencies)
        {
            return ValidateRoute(route, validateDependencies, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivity(ActivityAsset activity)
        {
            return ValidateActivity(activity, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivityLocalVisibilityAdapter(ActivityLocalVisibilityAdapter binding)
        {
            return ValidateActivityLocalVisibilityAdapter(binding, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateRouteContentBinding(RouteContentBinding binding)
        {
            return ValidateRouteContentBinding(binding, FrameworkValidationMode.Standard);
        }

        private static FrameworkAuthoringValidationReport ValidateGameApplication(
            GameApplicationAsset gameApplication,
            bool validateDependencies,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (gameApplication == null)
            {
                report.AddError("Game Application is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(gameApplication.ApplicationName))
            {
                report.AddWarning(
                    "Game Application has no display name. The asset name will be used in diagnostics.",
                    gameApplication);
            }

            if (gameApplication.StartupRoute == null)
            {
                report.AddError(
                    "Startup Route is missing. Assign the first Route in this Game Application.",
                    gameApplication);
            }
            else if (validateDependencies)
            {
                report.AddRange(ValidateRoute(gameApplication.StartupRoute, true, validationMode));
            }

            if (!report.HasIssues)
            {
                report.AddInfo("Game Application authoring is valid for the current framework scope.", gameApplication);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateRoute(
            RouteAsset route,
            bool validateDependencies,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (route == null)
            {
                report.AddError("Route is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(route.RouteName))
            {
                report.AddWarning(
                    "Route has no display name. The asset name will be used in diagnostics.",
                    route);
            }

            if (string.IsNullOrWhiteSpace(route.PrimaryScenePath))
            {
                report.AddError(
                    "Primary Scene is missing. A Route must declare one Primary Scene for Scene Lifecycle.",
                    route);
            }
            else
            {
                ValidatePrimarySceneReference(report, route);
            }

            if (route.StartupActivity == null)
            {
                report.AddInfo(
                    "Route has no Startup Activity. This is valid for menu/no-activity routes.",
                    route);
            }
            else if (validateDependencies)
            {
                report.AddRange(ValidateActivity(route.StartupActivity, validationMode));
            }

            if (!report.HasIssues)
            {
                report.AddInfo("Route authoring is valid for the current framework scope.", route);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateActivity(
            ActivityAsset activity,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (activity == null)
            {
                report.AddError("Activity is missing.", null);
                return report;
            }

            if (string.IsNullOrWhiteSpace(activity.ActivityName))
            {
                report.AddWarning(
                    "Activity has no display name. The asset name will be used in diagnostics.",
                    activity);
            }

            if (!report.HasIssues)
            {
                report.AddInfo("Activity authoring is valid for the current framework scope.", activity);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateActivityLocalVisibilityAdapter(
            ActivityLocalVisibilityAdapter binding,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (binding == null)
            {
                report.AddError("Activity Local Visibility Adapter is missing.", null);
                return report;
            }

            if (binding.Activity == null)
            {
                report.AddError(
                    $"Activity Local Visibility Adapter on GameObject '{binding.gameObject.name}' has no Activity assigned.",
                    binding);
            }

            var parentBinding = FindParentActivityLocalVisibilityAdapter(binding);
            if (parentBinding != null)
            {
                report.AddWarning(
                    $"Activity Local Visibility Adapter on GameObject '{binding.gameObject.name}' is nested under '{parentBinding.gameObject.name}'. Nested Activity local visibility policy is not defined yet.",
                    binding);
            }

            int childBindingCount = CountChildActivityLocalVisibilityAdapters(binding);
            if (childBindingCount > 0)
            {
                report.AddWarning(
                    $"Activity Local Visibility Adapter on GameObject '{binding.gameObject.name}' has {childBindingCount} child Activity Local Visibility Adapter component(s). Keep Activity local visibility adapter roots flat for now.",
                    binding);
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Local Visibility Adapter on GameObject '{binding.gameObject.name}' is valid for the current framework scope.",
                    binding);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateRouteContentBinding(
            RouteContentBinding binding,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (binding == null)
            {
                report.AddError("Route Content Binding is missing.", null);
                return report;
            }

            string objectName = binding.gameObject != null ? binding.gameObject.name : "<missing>";

            if (binding.Route == null)
            {
                report.AddError(
                    $"Route Content Binding on GameObject '{objectName}' has no Route assigned.",
                    binding);
            }
            else
            {
                ValidateRouteContentBindingSceneRoute(report, binding, objectName);
            }

            var parentBinding = FindParentRouteContentBinding(binding);
            if (parentBinding != null)
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' is nested under '{parentBinding.gameObject.name}'. Nested Route content policy is not defined in F3; keep Route content roots flat.",
                    binding);
            }

            int childBindingCount = CountChildRouteContentBindings(binding);
            if (childBindingCount > 0)
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' has {childBindingCount} child Route Content Binding component(s). Keep Route content roots flat for the F3 callback baseline.",
                    binding);
            }

            int receiverCount = CountRouteContentLifecycleReceivers(binding);
            if (receiverCount == 0)
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' has no IRouteContentLifecycleReceiver in itself or its children. Route Content Runtime will dispatch with zero receivers, and Route Callback Smoke cannot use this binding as callback proof.",
                    binding);
            }
            else
            {
                report.AddInfo(
                    $"Route Content Binding on GameObject '{objectName}' has {receiverCount} Route content lifecycle receiver(s).",
                    binding);
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Route Content Binding on GameObject '{objectName}' is valid for the F3 Route callback baseline.",
                    binding);
            }

            return report;
        }

        private static void ValidateRouteContentBindingSceneRoute(
            FrameworkAuthoringValidationReport report,
            RouteContentBinding binding,
            string objectName)
        {
            var route = binding.Route;
            var scene = binding.gameObject != null ? binding.gameObject.scene : default;

            if (!scene.IsValid())
            {
                report.AddInfo(
                    $"Route Content Binding on GameObject '{objectName}' is not in a valid scene. Scene-route validation is skipped for prefabs or disconnected objects.",
                    binding);
                return;
            }

            if (!scene.isLoaded)
            {
                report.AddInfo(
                    $"Route Content Binding on GameObject '{objectName}' is in scene '{scene.name}', but the scene is not loaded. Scene-route validation only checks loaded scenes.",
                    binding);
                return;
            }

            string scenePath = scene.path;
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' is in an unsaved scene. Save the scene so it can be compared against Route.PrimaryScenePath.",
                    binding);
                return;
            }

            if (string.IsNullOrWhiteSpace(route.PrimaryScenePath))
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' points to Route '{GetRouteLabel(route)}', but that Route has no Primary Scene path.",
                    binding);
                return;
            }

            if (!string.Equals(scenePath, route.PrimaryScenePath, System.StringComparison.OrdinalIgnoreCase))
            {
                report.AddWarning(
                    $"Route Content Binding on GameObject '{objectName}' points to Route '{GetRouteLabel(route)}', but it is authored in scene '{scenePath}'. The Route primary scene is '{route.PrimaryScenePath}'. This will cause Route callbacks and Route Callback Smoke to resolve the binding for the wrong Route.",
                    binding);
            }
        }

        private static void ValidatePrimarySceneReference(FrameworkAuthoringValidationReport report, RouteAsset route)
        {
            string scenePath = route.PrimaryScenePath;

            if (!scenePath.StartsWith("Assets/"))
            {
                report.AddError(
                    $"Primary Scene path must be project-relative under Assets. Current path: '{scenePath}'.",
                    route);
                return;
            }

            if (!scenePath.EndsWith(".unity"))
            {
                report.AddError(
                    $"Primary Scene path must reference a Unity scene asset. Current path: '{scenePath}'.",
                    route);
                return;
            }

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
            if (sceneAsset == null)
            {
                report.AddError(
                    $"Primary Scene asset could not be found at '{scenePath}'. Reassign the scene in the Route Inspector.",
                    route);
                return;
            }

            if (!string.Equals(sceneAsset.name, route.PrimarySceneName, System.StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(route.PrimarySceneName))
            {
                report.AddWarning(
                    $"Primary Scene cached name '{route.PrimarySceneName}' does not match scene asset name '{sceneAsset.name}'. Reassign the scene to refresh diagnostics.",
                    route);
            }
        }

        private static void ValidateOpenSceneActivityLocalVisibilityAdapters(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            ActivityLocalVisibilityAdapter[] bindings = Object.FindObjectsByType<ActivityLocalVisibilityAdapter>(FindObjectsInactive.Include);
            if (bindings == null || bindings.Length == 0)
            {
                report.AddInfo("No Activity Local Visibility Adapter components were found in open scenes.", null);
                return;
            }

            int sceneBindingCount = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || !binding.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!binding.gameObject.scene.isLoaded)
                {
                    continue;
                }

                sceneBindingCount++;
                report.AddRange(ValidateActivityLocalVisibilityAdapter(binding, validationMode));
            }

            if (sceneBindingCount == 0)
            {
                report.AddInfo("No scene-authored Activity Local Visibility Adapter components were found in loaded scenes.", null);
            }
        }

        private static void ValidateOpenSceneRouteContentBindings(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            RouteContentBinding[] bindings = Object.FindObjectsByType<RouteContentBinding>(FindObjectsInactive.Include);
            if (bindings == null || bindings.Length == 0)
            {
                report.AddInfo("No Route Content Binding components were found in open scenes.", null);
                return;
            }

            int sceneBindingCount = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || !binding.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!binding.gameObject.scene.isLoaded)
                {
                    continue;
                }

                sceneBindingCount++;
                report.AddRange(ValidateRouteContentBinding(binding, validationMode));
            }

            if (sceneBindingCount == 0)
            {
                report.AddInfo("No scene-authored Route Content Binding components were found in loaded scenes.", null);
            }
        }

        private static FrameworkValidationMode ResolveValidationMode(ImmersiveFrameworkSettingsAsset settings)
        {
            return settings != null && settings.ActiveGameApplication != null
                ? settings.ActiveGameApplication.ValidationMode
                : FrameworkValidationMode.Strict;
        }

        private static FrameworkValidationMode ResolveValidationMode(GameApplicationAsset gameApplication)
        {
            return gameApplication != null
                ? gameApplication.ValidationMode
                : FrameworkValidationMode.Strict;
        }

        private static ActivityLocalVisibilityAdapter FindParentActivityLocalVisibilityAdapter(ActivityLocalVisibilityAdapter binding)
        {
            var parent = binding.transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<ActivityLocalVisibilityAdapter>(out var parentBinding))
                {
                    return parentBinding;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static int CountChildActivityLocalVisibilityAdapters(ActivityLocalVisibilityAdapter binding)
        {
            ActivityLocalVisibilityAdapter[] all = binding.GetComponentsInChildren<ActivityLocalVisibilityAdapter>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != binding)
                {
                    count++;
                }
            }

            return count;
        }

        private static RouteContentBinding FindParentRouteContentBinding(RouteContentBinding binding)
        {
            var parent = binding.transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<RouteContentBinding>(out var parentBinding))
                {
                    return parentBinding;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static int CountChildRouteContentBindings(RouteContentBinding binding)
        {
            RouteContentBinding[] all = binding.GetComponentsInChildren<RouteContentBinding>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != binding)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountRouteContentLifecycleReceivers(RouteContentBinding binding)
        {
            MonoBehaviour[] behaviours = binding.GetComponentsInChildren<MonoBehaviour>(true);
            if (behaviours == null || behaviours.Length == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IRouteContentLifecycleReceiver)
                {
                    count++;
                }
            }

            return count;
        }

        private static string GetRouteLabel(RouteAsset route)
        {
            if (route == null)
            {
                return "<none>";
            }

            return string.IsNullOrWhiteSpace(route.RouteName)
                ? route.name
                : route.RouteName;
        }
    }
}

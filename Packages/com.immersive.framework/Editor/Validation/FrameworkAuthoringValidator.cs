using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
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
                ValidateOpenSceneActivityContentBindings(report, validationMode);
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

        internal static FrameworkAuthoringValidationReport ValidateActivityContentBinding(ActivityContentBinding binding)
        {
            return ValidateActivityContentBinding(binding, FrameworkValidationMode.Standard);
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

        private static FrameworkAuthoringValidationReport ValidateActivityContentBinding(
            ActivityContentBinding binding,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (binding == null)
            {
                report.AddError("Activity Content Binding is missing.", null);
                return report;
            }

            if (binding.Activity == null)
            {
                report.AddError(
                    $"Activity Content Binding on GameObject '{binding.gameObject.name}' has no Activity assigned.",
                    binding);
            }

            var parentBinding = FindParentBinding(binding);
            if (parentBinding != null)
            {
                report.AddWarning(
                    $"Activity Content Binding on GameObject '{binding.gameObject.name}' is nested under '{parentBinding.gameObject.name}'. Nested Activity content policy is not defined yet.",
                    binding);
            }

            int childBindingCount = CountChildBindings(binding);
            if (childBindingCount > 0)
            {
                report.AddWarning(
                    $"Activity Content Binding on GameObject '{binding.gameObject.name}' has {childBindingCount} child Activity Content Binding component(s). Keep Activity content roots flat for now.",
                    binding);
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Content Binding on GameObject '{binding.gameObject.name}' is valid for the current framework scope.",
                    binding);
            }

            return report;
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

        private static void ValidateOpenSceneActivityContentBindings(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            ActivityContentBinding[] bindings = Object.FindObjectsByType<ActivityContentBinding>(FindObjectsInactive.Include);
            if (bindings == null || bindings.Length == 0)
            {
                report.AddInfo("No Activity Content Binding components were found in open scenes.", null);
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
                report.AddRange(ValidateActivityContentBinding(binding, validationMode));
            }

            if (sceneBindingCount == 0)
            {
                report.AddInfo("No scene-authored Activity Content Binding components were found in loaded scenes.", null);
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

        private static ActivityContentBinding FindParentBinding(ActivityContentBinding binding)
        {
            var parent = binding.transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<ActivityContentBinding>(out var parentBinding))
                {
                    return parentBinding;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static int CountChildBindings(ActivityContentBinding binding)
        {
            ActivityContentBinding[] all = binding.GetComponentsInChildren<ActivityContentBinding>(true);
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
    }
}

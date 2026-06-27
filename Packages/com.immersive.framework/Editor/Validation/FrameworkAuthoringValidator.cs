using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Editor.Editor.Authoring;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.TransitionEffects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
namespace Immersive.Framework.Editor.Editor.Validation
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
                ValidateOpenSceneRouteContentAnchors(report, validationMode);
                ValidateOpenSceneActivityContentAnchors(report, validationMode);
                ValidateOpenSceneCycleResetTriggers(report, validationMode);
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

        internal static FrameworkAuthoringValidationReport ValidateRouteContentAnchor(RouteContentAnchor anchor)
        {
            return ValidateRouteContentAnchor(anchor, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivityContentAnchor(ActivityContentAnchor anchor)
        {
            return ValidateActivityContentAnchor(anchor, FrameworkValidationMode.Standard);
        }
        internal static FrameworkAuthoringValidationReport ValidateRouteCycleResetTrigger(RouteCycleResetTrigger trigger)
        {
            return ValidateRouteCycleResetTrigger(trigger, FrameworkValidationMode.Standard);
        }

        internal static FrameworkAuthoringValidationReport ValidateActivityCycleResetTrigger(ActivityCycleResetTrigger trigger)
        {
            return ValidateActivityCycleResetTrigger(trigger, FrameworkValidationMode.Standard);
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

            if (gameApplication.TransitionSurfacePolicyValue == TransitionSurfacePolicy.NoneConfigured)
            {
                if (gameApplication.TransitionSurfacePrefab != null)
                {
                    report.AddWarning(
                        "Transition Surface Prefab is assigned, but Transition Surface Policy is NoneConfigured. The prefab will not be instantiated and transition will remain explicit NoOp.",
                        gameApplication);
                }
                else
                {
                    report.AddInfo(
                        "Transition surface is configured as explicit NoOp.",
                        gameApplication);
                }
            }
            else if (gameApplication.TransitionSurfacePrefab == null)
            {
                report.AddError(
                    "Transition Surface Policy is Required, but Transition Surface Prefab is missing.",
                    gameApplication);
            }
            else
            {
                report.AddRange(ValidateTransitionSurfacePrefab(gameApplication.TransitionSurfacePrefab, validationMode));
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

        private static FrameworkAuthoringValidationReport ValidateTransitionSurfacePrefab(
            GameObject transitionSurfacePrefab,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (transitionSurfacePrefab == null)
            {
                report.AddError("Transition Surface Prefab is missing.", null);
                return report;
            }

            var prefabPath = AssetDatabase.GetAssetPath(transitionSurfacePrefab);
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                report.AddError(
                    $"Transition Surface Prefab '{transitionSurfacePrefab.name}' is not a prefab asset.",
                    transitionSurfacePrefab);
                return report;
            }

            if (!PrefabUtility.IsPartOfPrefabAsset(transitionSurfacePrefab))
            {
                report.AddError(
                    $"Transition Surface Prefab '{transitionSurfacePrefab.name}' must be a prefab asset.",
                    transitionSurfacePrefab);
                return report;
            }

            GameObject prefabRoot = null;
            try
            {
                prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                if (prefabRoot == null)
                {
                    report.AddError(
                        $"Transition Surface Prefab '{transitionSurfacePrefab.name}' could not be loaded for validation.",
                        transitionSurfacePrefab);
                    return report;
                }

                ValidateTransitionSurfacePrefabStructure(report, prefabRoot, transitionSurfacePrefab);
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Transition Surface Prefab '{transitionSurfacePrefab.name}' could not be validated. {exception.Message}",
                    transitionSurfacePrefab);
            }
            finally
            {
                if (prefabRoot != null)
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Transition Surface Prefab '{transitionSurfacePrefab.name}' is valid for the current framework scope.",
                    transitionSurfacePrefab);
            }

            return report;
        }

        private static void ValidateTransitionSurfacePrefabStructure(
            FrameworkAuthoringValidationReport report,
            GameObject prefabRoot,
            GameObject source)
        {
            var canvas = prefabRoot.GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                report.AddError(
                    $"Transition Surface Prefab '{source.name}' must contain a Canvas.",
                    source);
            }

            var adapter = prefabRoot.GetComponentInChildren<UnityFadeCurtainEffectAdapter>(true);
            if (adapter == null)
            {
                report.AddError(
                    $"Transition Surface Prefab '{source.name}' must contain a UnityFadeCurtainEffectAdapter.",
                    source);
                return;
            }

            var adapterGameObject = adapter.gameObject;
            var canvasGroup = adapterGameObject != null ? adapterGameObject.GetComponent<CanvasGroup>() : null;
            if (canvasGroup == null)
            {
                report.AddError(
                    $"Transition Surface Prefab '{source.name}' must place a CanvasGroup on the same GameObject as UnityFadeCurtainEffectAdapter.",
                    source);
            }

            var image = adapterGameObject != null ? adapterGameObject.GetComponent<Image>() : null;
            if (image == null)
            {
                report.AddError(
                    $"Transition Surface Prefab '{source.name}' must include an Image on the same GameObject as UnityFadeCurtainEffectAdapter.",
                    source);
            }
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

            string objectName = binding.gameObject != null ? binding.gameObject.name : "<missing>";

            if (binding.Activity == null)
            {
                report.AddError(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' has no Activity assigned.",
                    binding);
            }

            if (!binding.HasExplicitLocalContentId)
            {
                report.AddError(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' has no Local Content Id. F5 local identity requires an explicit id; GameObject names and hierarchy paths are diagnostics only.",
                    binding);
            }

            var parentBinding = FindParentActivityLocalVisibilityAdapter(binding);
            if (parentBinding != null)
            {
                report.AddWarning(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' is nested under '{parentBinding.gameObject.name}'. Nested Activity local visibility policy is not defined yet.",
                    binding);
            }

            int childBindingCount = CountChildActivityLocalVisibilityAdapters(binding);
            if (childBindingCount > 0)
            {
                report.AddWarning(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' has {childBindingCount} child Activity Local Visibility Adapter component(s). Keep Activity local visibility adapter roots flat for now.",
                    binding);
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Local Visibility Adapter on GameObject '{objectName}' is valid for the current framework scope.",
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

            if (!binding.HasExplicitLocalContentId)
            {
                report.AddError(
                    $"Route Content Binding on GameObject '{objectName}' has no Local Content Id. F5 local identity requires an explicit id; GameObject names and hierarchy paths are diagnostics only.",
                    binding);
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

        private static FrameworkAuthoringValidationReport ValidateRouteContentAnchor(
            RouteContentAnchor anchor,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (anchor == null)
            {
                report.AddError("Route Content Anchor is missing.", null);
                return report;
            }

            string objectName = anchor.gameObject != null ? anchor.gameObject.name : "<missing>";

            if (anchor.Route == null)
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{objectName}' has no Route assigned.",
                    anchor);
            }
            else
            {
                ValidateRouteContentAnchorSceneRoute(report, anchor, objectName);
            }

            if (!anchor.HasExplicitAnchorId)
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{objectName}' has no Anchor Id. Content Anchor identity must be explicit; GameObject names and hierarchy paths are diagnostics only.",
                    anchor);
            }

            if (!anchor.HasExplicitKind)
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{objectName}' has Kind set to Unknown. Choose Root, Slot or Point.",
                    anchor);
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), anchor.Requiredness))
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{objectName}' has an invalid Requiredness value.",
                    anchor);
            }

            if (anchor.HasValidAuthoring)
            {
                try
                {
                    if (anchor.TryCreateDeclaration(out var declaration))
                    {
                        report.AddInfo(
                            $"Route Content Anchor on GameObject '{objectName}' declares '{declaration.AnchorId.StableText}' as {declaration.Kind}/{declaration.Requiredness}.",
                            anchor);
                    }
                }
                catch (Exception exception)
                {
                    report.AddError(
                        $"Route Content Anchor on GameObject '{objectName}' could not create a ContentAnchorDeclaration. {exception.Message}",
                        anchor);
                }
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Route Content Anchor on GameObject '{objectName}' is valid for the current F7 authoring-validation scope.",
                    anchor);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateActivityContentAnchor(
            ActivityContentAnchor anchor,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (anchor == null)
            {
                report.AddError("Activity Content Anchor is missing.", null);
                return report;
            }

            string objectName = anchor.gameObject != null ? anchor.gameObject.name : "<missing>";

            if (anchor.Activity == null)
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{objectName}' has no Activity assigned.",
                    anchor);
            }

            if (!anchor.HasExplicitAnchorId)
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{objectName}' has no Anchor Id. Content Anchor identity must be explicit; GameObject names and hierarchy paths are diagnostics only.",
                    anchor);
            }

            if (!anchor.HasExplicitKind)
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{objectName}' has Kind set to Unknown. Choose Root, Slot or Point.",
                    anchor);
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), anchor.Requiredness))
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{objectName}' has an invalid Requiredness value.",
                    anchor);
            }

            if (anchor.HasValidAuthoring)
            {
                try
                {
                    if (anchor.TryCreateDeclaration(out var declaration))
                    {
                        report.AddInfo(
                            $"Activity Content Anchor on GameObject '{objectName}' declares '{declaration.AnchorId.StableText}' as {declaration.Kind}/{declaration.Requiredness}.",
                            anchor);
                    }
                }
                catch (Exception exception)
                {
                    report.AddError(
                        $"Activity Content Anchor on GameObject '{objectName}' could not create a ContentAnchorDeclaration. {exception.Message}",
                        anchor);
                }
            }

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Content Anchor on GameObject '{objectName}' is valid for the current F9G authoring-validation scope.",
                    anchor);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateRouteCycleResetTrigger(
            RouteCycleResetTrigger trigger,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (trigger == null)
            {
                report.AddError("Route Cycle Reset Trigger is missing.", null);
                return report;
            }

            string objectName = trigger.gameObject != null ? trigger.gameObject.name : "<missing>";
            ValidateCycleResetTriggerCommon(report, trigger, objectName, "Route Cycle Reset Trigger", trigger.AuthoringReason);

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Route Cycle Reset Trigger on GameObject '{objectName}' is valid for the F12 Cycle Reset authoring UX scope.",
                    trigger);
            }

            return report;
        }

        private static FrameworkAuthoringValidationReport ValidateActivityCycleResetTrigger(
            ActivityCycleResetTrigger trigger,
            FrameworkValidationMode validationMode)
        {
            var report = new FrameworkAuthoringValidationReport(validationMode);

            if (trigger == null)
            {
                report.AddError("Activity Cycle Reset Trigger is missing.", null);
                return report;
            }

            string objectName = trigger.gameObject != null ? trigger.gameObject.name : "<missing>";
            ValidateCycleResetTriggerCommon(report, trigger, objectName, "Activity Cycle Reset Trigger", trigger.AuthoringReason);

            if (!report.HasIssues)
            {
                report.AddInfo(
                    $"Activity Cycle Reset Trigger on GameObject '{objectName}' is valid for the F12 Cycle Reset authoring UX scope.",
                    trigger);
            }

            return report;
        }

        private static void ValidateCycleResetTriggerCommon(
            FrameworkAuthoringValidationReport report,
            MonoBehaviour trigger,
            string objectName,
            string triggerLabel,
            string reason)
        {
            if (trigger == null)
            {
                return;
            }

            if (!trigger.gameObject.scene.IsValid())
            {
                report.AddInfo(
                    $"{triggerLabel} on GameObject '{objectName}' is not in a valid scene. Scene authoring validation is skipped for prefabs or disconnected objects.",
                    trigger);
            }

            if (!trigger.gameObject.activeInHierarchy)
            {
                report.AddInfo(
                    $"{triggerLabel} on GameObject '{objectName}' is inactive in hierarchy. It will not submit requests until active.",
                    trigger);
            }

            if (CycleResetTriggerAuthoringText.ContainsFutureResetVocabulary(reason))
            {
                report.AddWarning(
                    $"{triggerLabel} on GameObject '{objectName}' uses reason '{reason}'. Cycle Reset is Route/Activity-level only; object/component/player/actor/pool/save/reload wording belongs to later reset phases.",
                    trigger);
            }

            bool hasRouteTrigger = trigger.GetComponent<RouteCycleResetTrigger>() != null;
            bool hasActivityTrigger = trigger.GetComponent<ActivityCycleResetTrigger>() != null;
            if (hasRouteTrigger && hasActivityTrigger)
            {
                report.AddWarning(
                    $"GameObject '{objectName}' has both Route and Activity Cycle Reset Triggers. This is allowed for tooling, but separate buttons/objects are clearer for authoring.",
                    trigger);
            }
        }

        private static void ValidateRouteContentAnchorSceneRoute(
            FrameworkAuthoringValidationReport report,
            RouteContentAnchor anchor,
            string objectName)
        {
            var route = anchor.Route;
            var scene = anchor.gameObject != null ? anchor.gameObject.scene : default;

            if (!scene.IsValid())
            {
                report.AddInfo(
                    $"Route Content Anchor on GameObject '{objectName}' is not in a valid scene. Scene-route validation is skipped for prefabs or disconnected objects.",
                    anchor);
                return;
            }

            if (!scene.isLoaded)
            {
                report.AddInfo(
                    $"Route Content Anchor on GameObject '{objectName}' is in scene '{scene.name}', but the scene is not loaded. Scene-route validation only checks loaded scenes.",
                    anchor);
                return;
            }

            string scenePath = scene.path;
            string sceneName = scene.name;
            if (string.IsNullOrWhiteSpace(scenePath) && string.IsNullOrWhiteSpace(sceneName))
            {
                report.AddWarning(
                    $"Route Content Anchor on GameObject '{objectName}' is in an unidentified scene. Save the scene so it can be compared against Route scene composition.",
                    anchor);
                return;
            }

            if (!DoesRouteDeclareScene(route, scenePath, sceneName))
            {
                report.AddWarning(
                    $"Route Content Anchor on GameObject '{objectName}' points to Route '{GetRouteLabel(route)}', but it is authored in scene '{GetSceneLabel(scenePath, sceneName)}'. That scene is not declared as the Route primary scene or an additional Route Content Profile scene.",
                    anchor);
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

        private static void ValidateOpenSceneRouteContentAnchors(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            RouteContentAnchor[] anchors = Object.FindObjectsByType<RouteContentAnchor>(FindObjectsInactive.Include);
            if (anchors == null || anchors.Length == 0)
            {
                report.AddInfo("No Route Content Anchor components were found in open scenes.", null);
                return;
            }

            int sceneAnchorCount = 0;
            var declarations = new List<ContentAnchorDeclaration>(anchors.Length);

            for (int i = 0; i < anchors.Length; i++)
            {
                var anchor = anchors[i];
                if (anchor == null || anchor.gameObject == null || !anchor.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!anchor.gameObject.scene.isLoaded)
                {
                    continue;
                }

                sceneAnchorCount++;
                report.AddRange(ValidateRouteContentAnchor(anchor, validationMode));
                TryCollectContentAnchorDeclaration(anchor, declarations, report);
            }

            if (sceneAnchorCount == 0)
            {
                report.AddInfo("No scene-authored Route Content Anchor components were found in loaded scenes.", null);
                return;
            }

            ValidateContentAnchorSetIssues(report, declarations);
        }

        private static void ValidateOpenSceneActivityContentAnchors(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            ActivityContentAnchor[] anchors = Object.FindObjectsByType<ActivityContentAnchor>(FindObjectsInactive.Include);
            if (anchors == null || anchors.Length == 0)
            {
                report.AddInfo("No Activity Content Anchor components were found in open scenes.", null);
                return;
            }

            int sceneAnchorCount = 0;
            var declarations = new List<ContentAnchorDeclaration>(anchors.Length);

            for (int i = 0; i < anchors.Length; i++)
            {
                var anchor = anchors[i];
                if (anchor == null || anchor.gameObject == null || !anchor.gameObject.scene.IsValid())
                {
                    continue;
                }

                if (!anchor.gameObject.scene.isLoaded)
                {
                    continue;
                }

                sceneAnchorCount++;
                report.AddRange(ValidateActivityContentAnchor(anchor, validationMode));
                TryCollectContentAnchorDeclaration(anchor, declarations, report);
            }

            if (sceneAnchorCount == 0)
            {
                report.AddInfo("No scene-authored Activity Content Anchor components were found in loaded scenes.", null);
                return;
            }

            ValidateContentAnchorSetIssues(report, declarations, "Activity Content Anchor");
        }

        private static void ValidateOpenSceneCycleResetTriggers(
            FrameworkAuthoringValidationReport report,
            FrameworkValidationMode validationMode)
        {
            RouteCycleResetTrigger[] routeTriggers = Object.FindObjectsByType<RouteCycleResetTrigger>(FindObjectsInactive.Include);
            ActivityCycleResetTrigger[] activityTriggers = Object.FindObjectsByType<ActivityCycleResetTrigger>(FindObjectsInactive.Include);

            int routeTriggerCount = 0;
            if (routeTriggers != null)
            {
                for (int i = 0; i < routeTriggers.Length; i++)
                {
                    var trigger = routeTriggers[i];
                    if (trigger == null || trigger.gameObject == null || !trigger.gameObject.scene.IsValid() || !trigger.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    routeTriggerCount++;
                    report.AddRange(ValidateRouteCycleResetTrigger(trigger, validationMode));
                }
            }

            int activityTriggerCount = 0;
            if (activityTriggers != null)
            {
                for (int i = 0; i < activityTriggers.Length; i++)
                {
                    var trigger = activityTriggers[i];
                    if (trigger == null || trigger.gameObject == null || !trigger.gameObject.scene.IsValid() || !trigger.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    activityTriggerCount++;
                    report.AddRange(ValidateActivityCycleResetTrigger(trigger, validationMode));
                }
            }

            if (routeTriggerCount == 0 && activityTriggerCount == 0)
            {
                report.AddInfo("No scene-authored Cycle Reset Trigger components were found in loaded scenes.", null);
                return;
            }

            report.AddInfo(
                $"Cycle Reset Trigger validation scanned routeTriggers='{routeTriggerCount}' activityTriggers='{activityTriggerCount}'.",
                null);
        }

        private static void TryCollectContentAnchorDeclaration(
            RouteContentAnchor anchor,
            List<ContentAnchorDeclaration> declarations,
            FrameworkAuthoringValidationReport report)
        {
            if (anchor == null || !anchor.HasValidAuthoring)
            {
                return;
            }

            try
            {
                if (anchor.TryCreateDeclaration(out var declaration))
                {
                    declarations.Add(declaration);
                }
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Route Content Anchor on GameObject '{anchor.ObjectName}' could not be collected for duplicate validation. {exception.Message}",
                    anchor);
            }
        }

        private static void TryCollectContentAnchorDeclaration(
            ActivityContentAnchor anchor,
            List<ContentAnchorDeclaration> declarations,
            FrameworkAuthoringValidationReport report)
        {
            if (anchor == null || !anchor.HasValidAuthoring)
            {
                return;
            }

            try
            {
                if (anchor.TryCreateDeclaration(out var declaration))
                {
                    declarations.Add(declaration);
                }
            }
            catch (Exception exception)
            {
                report.AddError(
                    $"Activity Content Anchor on GameObject '{anchor.ObjectName}' could not be collected for duplicate validation. {exception.Message}",
                    anchor);
            }
        }

        private static void ValidateContentAnchorSetIssues(
            FrameworkAuthoringValidationReport report,
            IReadOnlyList<ContentAnchorDeclaration> declarations,
            string label = "Route Content Anchor")
        {
            if (declarations == null || declarations.Count == 0)
            {
                return;
            }

            var set = ContentAnchorSet.FromDeclarations(declarations);
            if (!set.HasIssues)
            {
                report.AddInfo(
                    $"{label} duplicate validation passed. anchors='{set.Count}' required='{set.RequiredCount}' optional='{set.OptionalCount}'.",
                    null);
                return;
            }

            var issues = set.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                switch (issue.Kind)
                {
                    case ContentAnchorSetIssueKind.DuplicateIdentity:
                    case ContentAnchorSetIssueKind.DuplicateAnchorId:
                    case ContentAnchorSetIssueKind.InvalidDeclaration:
                        report.AddError($"{label} duplicate validation issue: {issue.ToDiagnosticString()}.", null);
                        break;
                    default:
                        report.AddWarning($"{label} validation issue: {issue.ToDiagnosticString()}.", null);
                        break;
                }
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

        private static bool DoesRouteDeclareScene(RouteAsset route, string scenePath, string sceneName)
        {
            if (route == null)
            {
                return false;
            }

            if (MatchesScene(route.PrimaryScenePath, route.PrimarySceneName, scenePath, sceneName))
            {
                return true;
            }

            var profile = route.RouteContentProfile;
            if (profile == null || profile.AdditionalScenes == null)
            {
                return false;
            }

            for (int i = 0; i < profile.AdditionalScenes.Count; i++)
            {
                var entry = profile.AdditionalScenes[i];
                if (entry != null && MatchesScene(entry.ScenePath, entry.SceneName, scenePath, sceneName))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesScene(string declaredPath, string declaredName, string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(declaredPath)
                && !string.IsNullOrWhiteSpace(scenePath)
                && string.Equals(declaredPath.Trim(), scenePath.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return !string.IsNullOrWhiteSpace(declaredName)
                && !string.IsNullOrWhiteSpace(sceneName)
                && string.Equals(declaredName.Trim(), sceneName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSceneLabel(string scenePath, string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                return scenePath.Trim();
            }

            return string.IsNullOrWhiteSpace(sceneName) ? "<none>" : sceneName.Trim();
        }
    }
}

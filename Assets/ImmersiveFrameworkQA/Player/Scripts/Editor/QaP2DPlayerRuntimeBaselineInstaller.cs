using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerBinding;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.UnityInput;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent installer for the P2D QA Route/Activity module.
    /// It clones an existing canonical QA Route/Activity pair, creates the runtime scene,
    /// adds Back-to-Hub navigation, appends one Hub entry and registers the scene for builds.
    /// Existing Hub entries and unrelated assets are preserved.
    /// </summary>
    public static class QaP2DPlayerRuntimeBaselineInstaller
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P2D Install Player Runtime Baseline";

        private const string PlayerRoot =
            "Assets/ImmersiveFrameworkQA/Player";
        private const string AssetsFolder =
            PlayerRoot + "/Assets";
        private const string ScenesFolder =
            PlayerRoot + "/Scenes";

        private const string ActionsPath =
            AssetsFolder + "/QA_PlayerRuntimeBaseline.inputactions";
        private const string ActivityPath =
            AssetsFolder + "/QA_PlayerRuntimeBaselineActivity.asset";
        private const string RoutePath =
            AssetsFolder + "/QA_PlayerRuntimeBaselineRoute.asset";
        private const string ScenePath =
            ScenesFolder + "/QA_PlayerRuntimeBaseline.unity";

        private const string RouteName =
            "QA Player Runtime Baseline Route";
        private const string ActivityName =
            "QA Player Runtime Baseline Activity";
        private const string SceneName =
            "QA_PlayerRuntimeBaseline";
        private const string HubEntryLabel =
            "Player Runtime Baseline";
        private const string SlotId =
            "qa.player.1";
        private const string ActionMapName =
            "Player";
        private const string MoveActionName =
            "Move";

        private const string QaHubSceneName = "QA_Hub";
        private const string QaHubRouteGuid =
            "1addd390562c4f15a738f4125bb94be8";

        [MenuItem(MenuPath)]
        public static void Install()
        {
            try
            {
                EnsureFolder(PlayerRoot, "Assets");
                EnsureFolder(PlayerRoot, "Scenes");

                InputActionAsset actions = CreateOrReplaceInputActions();
                SceneAsset sceneAsset = CreateOrReplaceRuntimeScene(actions);

                string hubScenePath = FindUniqueAssetPath(
                    $"t:Scene {QaHubSceneName}",
                    QaHubSceneName + ".unity");

                RouteRequestTrigger templateTrigger =
                    FindTemplateRouteTrigger(hubScenePath);
                ScriptableObject templateRoute =
                    templateTrigger != null
                        ? templateTrigger.TargetRoute
                        : null;

                if (templateRoute == null)
                {
                    throw new InvalidOperationException(
                        "Could not resolve a canonical non-Hub QA Route template from QA_Hub.");
                }

                ScriptableObject templateActivity =
                    ResolveActivityReference(templateRoute);
                if (templateActivity == null)
                {
                    throw new InvalidOperationException(
                        $"Template Route '{templateRoute.name}' does not expose a startup Activity reference.");
                }

                ScriptableObject activity =
                    CopyAndConfigureActivity(templateActivity);
                ScriptableObject route =
                    CopyAndConfigureRoute(
                        templateRoute,
                        activity,
                        sceneAsset);

                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(
                    RoutePath,
                    ImportAssetOptions.ForceSynchronousImport
                    | ImportAssetOptions.ForceUpdate);

                route = LoadTypedRouteAsset(RoutePath);
                if (route == null)
                {
                    throw new InvalidOperationException(
                        $"Generated Route asset '{RoutePath}' could not be reloaded as the concrete RouteRequestTrigger target type.");
                }

                ConfigureBackToHubNavigation();

                ScriptableObject hubFeatureRoute =
                    LoadTypedRouteAsset(RoutePath);
                if (hubFeatureRoute == null)
                {
                    throw new InvalidOperationException(
                        $"Generated Route asset '{RoutePath}' could not be reloaded before Hub integration.");
                }

                AppendHubEntry(
                    hubScenePath,
                    hubFeatureRoute);
                EnsureSceneInEditorBuildSettings(ScenePath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P2D_PLAYER_RUNTIME_BASELINE_SETUP] status='Succeeded' " +
                    $"route='{RoutePath}' activity='{ActivityPath}' scene='{ScenePath}' " +
                    $"inputActions='{ActionsPath}' hub='{hubScenePath}' " +
                    "message='Enter Play Mode through the normal QA bootstrap and select Player Runtime Baseline in QA Hub.'");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P2D_PLAYER_RUNTIME_BASELINE_SETUP] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static InputActionAsset CreateOrReplaceInputActions()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ActionsPath) != null)
            {
                AssetDatabase.DeleteAsset(ActionsPath);
            }

            InputActionAsset temporary =
                ScriptableObject.CreateInstance<InputActionAsset>();
            temporary.name = "QA_PlayerRuntimeBaseline";

            InputActionMap playerMap =
                temporary.AddActionMap(ActionMapName);
            InputAction move =
                playerMap.AddAction(
                    MoveActionName,
                    InputActionType.Value,
                    expectedControlLayout: "Vector2");

            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick");

            File.WriteAllText(ActionsPath, temporary.ToJson());
            UnityEngine.Object.DestroyImmediate(temporary);

            AssetDatabase.ImportAsset(
                ActionsPath,
                ImportAssetOptions.ForceSynchronousImport
                | ImportAssetOptions.ForceUpdate);

            InputActionAsset imported =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (imported == null)
            {
                throw new InvalidOperationException(
                    $"Could not import InputActionAsset '{ActionsPath}'.");
            }

            return imported;
        }

        private static SceneAsset CreateOrReplaceRuntimeScene(
            InputActionAsset actions)
        {
            Scene scene =
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

            var playerRoot = new GameObject("QA_P2D_Player");

            PlayerInput playerInput =
                playerRoot.AddComponent<PlayerInput>();
            playerInput.actions = actions;
            playerInput.defaultActionMap = ActionMapName;
            playerInput.notificationBehavior =
                PlayerNotifications.InvokeCSharpEvents;

            PlayerSlotDeclaration slot =
                playerRoot.AddComponent<PlayerSlotDeclaration>();
            ConfigureSlot(slot, playerInput);

            UnityPlayerInputGateAdapter gate =
                playerRoot.AddComponent<UnityPlayerInputGateAdapter>();
            ConfigureGate(gate, playerInput, slot);

            var framework = new GameObject("_Framework");
            framework.transform.SetParent(playerRoot.transform, false);
            var bindings = new GameObject("_Bindings");
            bindings.transform.SetParent(framework.transform, false);

            PlayerControlBindingTargetBehaviour controlTarget =
                bindings.AddComponent<PlayerControlBindingTargetBehaviour>();
            UnityPlayerInputBridgeTargetBehaviour bridgeTarget =
                bindings.AddComponent<UnityPlayerInputBridgeTargetBehaviour>();
            UnityPlayerInputActivationTargetBehaviour activationTarget =
                bindings.AddComponent<UnityPlayerInputActivationTargetBehaviour>();

            ConfigureControlTarget(controlTarget);
            ConfigureBridgeTarget(bridgeTarget, playerInput);
            ConfigureActivationTarget(activationTarget, playerInput);

            QaP2DPlayerRuntimeBaselineFixture fixture =
                playerRoot.AddComponent<QaP2DPlayerRuntimeBaselineFixture>();
            ConfigureFixture(
                fixture,
                playerInput,
                slot,
                gate,
                controlTarget,
                bridgeTarget,
                activationTarget);

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                throw new InvalidOperationException(
                    $"Unity could not save QA scene '{ScenePath}'.");
            }

            AssetDatabase.ImportAsset(
                ScenePath,
                ImportAssetOptions.ForceSynchronousImport
                | ImportAssetOptions.ForceUpdate);

            SceneAsset sceneAsset =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            if (sceneAsset == null)
            {
                throw new InvalidOperationException(
                    $"Could not load generated QA scene '{ScenePath}'.");
            }

            return sceneAsset;
        }

        private static RouteRequestTrigger FindTemplateRouteTrigger(
            string hubScenePath)
        {
            Scene hubScene =
                EditorSceneManager.OpenScene(
                    hubScenePath,
                    OpenSceneMode.Single);

            RouteRequestTrigger[] triggers =
                UnityEngine.Object.FindObjectsByType<RouteRequestTrigger>(FindObjectsInactive.Include);

            RouteRequestTrigger preferred = null;
            RouteRequestTrigger fallback = null;

            for (int i = 0; i < triggers.Length; i++)
            {
                RouteRequestTrigger trigger = triggers[i];
                if (trigger == null || trigger.TargetRoute == null)
                {
                    continue;
                }

                string routePath =
                    AssetDatabase.GetAssetPath(trigger.TargetRoute);
                string routeGuid =
                    AssetDatabase.AssetPathToGUID(routePath);

                if (string.Equals(
                    routeGuid,
                    QaHubRouteGuid,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                fallback ??= trigger;

                string routeName =
                    trigger.TargetRoute.RouteName ?? string.Empty;
                if (routeName.IndexOf(
                    "Player",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    preferred = trigger;
                    break;
                }
            }

            return preferred != null ? preferred : fallback;
        }

        private static ScriptableObject ResolveActivityReference(
            ScriptableObject route)
        {
            var serialized = new SerializedObject(route);
            serialized.Update();

            SerializedProperty iterator =
                serialized.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType
                    != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                UnityEngine.Object reference =
                    iterator.objectReferenceValue;
                if (reference == null)
                {
                    continue;
                }

                Type type = reference.GetType();
                if (type.Name.IndexOf(
                    "Activity",
                    StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                return reference as ScriptableObject;
            }

            return null;
        }

        private static ScriptableObject CopyAndConfigureActivity(
            ScriptableObject templateActivity)
        {
            CopyAssetReplacing(
                AssetDatabase.GetAssetPath(templateActivity),
                ActivityPath);

            ScriptableObject activity =
                AssetDatabase.LoadAssetAtPath(
                    ActivityPath,
                    templateActivity.GetType())
                as ScriptableObject;
            if (activity == null)
            {
                throw new InvalidOperationException(
                    $"Could not load copied Activity asset '{ActivityPath}'.");
            }

            activity.name =
                Path.GetFileNameWithoutExtension(ActivityPath);
            var serialized = new SerializedObject(activity);
            serialized.Update();
            SetFirstString(
                serialized,
                ActivityName,
                "activityName",
                "displayName",
                "name");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);
            return activity;
        }

        private static ScriptableObject CopyAndConfigureRoute(
            ScriptableObject templateRoute,
            ScriptableObject activity,
            SceneAsset sceneAsset)
        {
            CopyAssetReplacing(
                AssetDatabase.GetAssetPath(templateRoute),
                RoutePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                RoutePath,
                ImportAssetOptions.ForceSynchronousImport
                | ImportAssetOptions.ForceUpdate);

            ScriptableObject route =
                AssetDatabase.LoadAssetAtPath(
                    RoutePath,
                    templateRoute.GetType())
                as ScriptableObject;

            if (route == null)
            {
                route = LoadTypedRouteAsset(RoutePath);
            }

            if (route == null)
            {
                throw new InvalidOperationException(
                    $"Could not load copied Route asset '{RoutePath}' as template type '{templateRoute.GetType().FullName}' or RouteRequestTrigger target type.");
            }

            route.name =
                Path.GetFileNameWithoutExtension(RoutePath);
            var serialized = new SerializedObject(route);
            serialized.Update();

            bool routeNameSet =
                SetFirstString(
                    serialized,
                    RouteName,
                    "routeName",
                    "displayName",
                    "name");

            bool activitySet =
                SetFirstObjectReferenceByCandidates(
                    serialized,
                    activity,
                    "startupActivity",
                    "initialActivity",
                    "defaultActivity",
                    "activity");

            bool sceneSet =
                SetSceneReference(
                    serialized,
                    sceneAsset,
                    ScenePath,
                    SceneName);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);

            if (!routeNameSet || !activitySet || !sceneSet)
            {
                throw new InvalidOperationException(
                    "Copied Route shape could not be configured completely. " +
                    $"routeNameSet='{routeNameSet}' activitySet='{activitySet}' sceneSet='{sceneSet}'.");
            }

            return route;
        }

        private static void ConfigureBackToHubNavigation()
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);

            FieldInfo routeField =
                typeof(RouteRequestTrigger).GetField(
                    "targetRoute",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);
            if (routeField == null)
            {
                throw new InvalidOperationException(
                    "RouteRequestTrigger targetRoute field was not found.");
            }

            string hubRoutePath =
                AssetDatabase.GUIDToAssetPath(QaHubRouteGuid);
            ScriptableObject hubRoute =
                string.IsNullOrEmpty(hubRoutePath)
                    ? null
                    : AssetDatabase.LoadAssetAtPath(
                        hubRoutePath,
                        routeField.FieldType)
                        as ScriptableObject;

            if (hubRoute == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve typed QA Hub Route GUID '{QaHubRouteGuid}'.");
            }

            GameObject navigation =
                FindRoot(scene, "QA_BackToHubNavigation");
            if (navigation == null)
            {
                navigation = new GameObject(
                    "QA_BackToHubNavigation");
                SceneManager.MoveGameObjectToScene(
                    navigation,
                    scene);
            }

            RouteRequestTrigger trigger =
                navigation.GetComponent<RouteRequestTrigger>();
            if (trigger == null)
            {
                trigger =
                    navigation.AddComponent<RouteRequestTrigger>();
            }

            ConfigureRouteRequestTrigger(
                trigger,
                hubRoute,
                "qa.route.back-to-hub");

            Type panelType =
                FindType("ImmersiveFrameworkQA.Hub.QaHubPanel");
            if (panelType == null
                || !typeof(Component).IsAssignableFrom(panelType))
            {
                throw new InvalidOperationException(
                    "Could not resolve canonical QaHubPanel runtime type.");
            }

            Component panel =
                navigation.GetComponent(panelType)
                ?? navigation.AddComponent(panelType);

            ConfigureHubPanel(
                panel,
                "QA Navigation",
                "Back to QA Hub",
                trigger,
                new Rect(16f, 16f, 360f, 92f));

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save Back-to-Hub navigation into '{ScenePath}'.");
            }
        }

        private static void AppendHubEntry(
            string hubScenePath,
            ScriptableObject featureRoute)
        {
            if (featureRoute == null)
            {
                featureRoute =
                    LoadTypedRouteAsset(RoutePath);
            }

            Scene hubScene =
                EditorSceneManager.OpenScene(
                    hubScenePath,
                    OpenSceneMode.Single);

            // Opening a scene can invalidate a managed Unity asset wrapper.
            // Reload the Route after the scene operation and use only this fresh reference.
            featureRoute =
                LoadTypedRouteAsset(RoutePath);

            if (featureRoute == null)
            {
                throw new InvalidOperationException(
                    $"Generated Route asset '{RoutePath}' is unavailable after opening QA Hub.");
            }

            Type panelType =
                FindType("ImmersiveFrameworkQA.Hub.QaHubPanel");
            if (panelType == null)
            {
                throw new InvalidOperationException(
                    "Could not resolve canonical QaHubPanel runtime type.");
            }

            Component panel =
                UnityEngine.Object.FindObjectsByType(panelType, FindObjectsInactive.Include)
                .OfType<Component>()
                .FirstOrDefault(component =>
                    component != null
                    && component.gameObject.scene == hubScene);

            if (panel == null)
            {
                throw new InvalidOperationException(
                    $"QA Hub scene '{hubScenePath}' does not contain QaHubPanel.");
            }

            RouteRequestTrigger trigger =
                UnityEngine.Object.FindObjectsByType<RouteRequestTrigger>(FindObjectsInactive.Include)
                .FirstOrDefault(candidate =>
                    candidate != null
                    && candidate.gameObject.scene == hubScene
                    && string.Equals(
                        candidate.gameObject.name,
                        "RouteTrigger_Player_Runtime_Baseline_QA",
                        StringComparison.Ordinal));

            if (trigger == null)
            {
                GameObject triggerRoot =
                    new GameObject(
                        "RouteTrigger_Player_Runtime_Baseline_QA");
                SceneManager.MoveGameObjectToScene(
                    triggerRoot,
                    hubScene);
                triggerRoot.transform.SetParent(
                    panel.transform,
                    false);
                trigger =
                    triggerRoot.AddComponent<RouteRequestTrigger>();
            }

            ConfigureRouteRequestTrigger(
                trigger,
                featureRoute,
                "qa.hub.route.player_runtime_baseline");

            if (trigger.TargetRoute == null)
            {
                throw new InvalidOperationException(
                    "Player Runtime Baseline RouteRequestTrigger did not retain targetRoute after configuration.");
            }

            var serialized = new SerializedObject(panel);
            serialized.Update();

            SerializedProperty entries =
                serialized.FindProperty("entries");
            if (entries == null || !entries.isArray)
            {
                throw new InvalidOperationException(
                    "QaHubPanel does not expose the expected serialized entries array.");
            }

            int existingIndex = -1;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty element =
                    entries.GetArrayElementAtIndex(i);
                SerializedProperty labelProperty =
                    element.FindPropertyRelative("label");

                if (labelProperty != null
                    && string.Equals(
                        labelProperty.stringValue,
                        HubEntryLabel,
                        StringComparison.Ordinal))
                {
                    existingIndex = i;
                    break;
                }
            }

            if (existingIndex < 0)
            {
                existingIndex = entries.arraySize;
                entries.arraySize++;
            }

            SerializedProperty entry =
                entries.GetArrayElementAtIndex(existingIndex);
            SerializedProperty entryLabel =
                entry.FindPropertyRelative("label");
            SerializedProperty entryTrigger =
                entry.FindPropertyRelative(
                    "routeRequestTrigger");

            if (entryLabel == null || entryTrigger == null)
            {
                throw new InvalidOperationException(
                    "QaHubPanel entry shape does not match label/routeRequestTrigger.");
            }

            entryLabel.stringValue = HubEntryLabel;
            entryTrigger.objectReferenceValue = trigger;

            if (!serialized.ApplyModifiedPropertiesWithoutUndo())
            {
                serialized.Update();
            }

            EditorUtility.SetDirty(panel);
            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(trigger.gameObject);
            EditorSceneManager.MarkSceneDirty(hubScene);

            if (!EditorSceneManager.SaveScene(
                hubScene,
                hubScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save QA Hub scene '{hubScenePath}'.");
            }

            ValidateSavedHubEntry(
                hubScenePath);
        }

        private static void ValidateSavedHubEntry(
            string hubScenePath)
        {
            Scene hubScene =
                EditorSceneManager.OpenScene(
                    hubScenePath,
                    OpenSceneMode.Single);

            ScriptableObject expectedRoute =
                LoadTypedRouteAsset(RoutePath);
            if (expectedRoute == null)
            {
                throw new InvalidOperationException(
                    $"Generated Route asset '{RoutePath}' is unavailable while validating saved QA Hub entry.");
            }

            Type panelType =
                FindType("ImmersiveFrameworkQA.Hub.QaHubPanel");
            Component panel =
                panelType == null
                    ? null
                    : UnityEngine.Object.FindObjectsByType(panelType, FindObjectsInactive.Include)
                        .OfType<Component>()
                        .FirstOrDefault(component =>
                            component != null
                            && component.gameObject.scene == hubScene);

            if (panel == null)
            {
                throw new InvalidOperationException(
                    "Saved QA Hub scene no longer contains QaHubPanel.");
            }

            var serialized = new SerializedObject(panel);
            serialized.Update();
            SerializedProperty entries =
                serialized.FindProperty("entries");

            if (entries == null || !entries.isArray)
            {
                throw new InvalidOperationException(
                    "Saved QaHubPanel entries array is missing.");
            }

            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry =
                    entries.GetArrayElementAtIndex(i);
                SerializedProperty label =
                    entry.FindPropertyRelative("label");
                SerializedProperty triggerProperty =
                    entry.FindPropertyRelative("routeRequestTrigger");

                if (label == null
                    || !string.Equals(
                        label.stringValue,
                        HubEntryLabel,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                RouteRequestTrigger trigger =
                    triggerProperty != null
                        ? triggerProperty.objectReferenceValue
                            as RouteRequestTrigger
                        : null;

                if (trigger == null)
                {
                    throw new InvalidOperationException(
                        "Saved Player Runtime Baseline Hub entry has no RouteRequestTrigger reference.");
                }

                if (trigger.TargetRoute == null)
                {
                    throw new InvalidOperationException(
                        "Saved Player Runtime Baseline RouteRequestTrigger has no targetRoute.");
                }

                if (trigger.TargetRoute != expectedRoute)
                {
                    throw new InvalidOperationException(
                        $"Saved Player Runtime Baseline trigger points to unexpected route '{trigger.TargetRoute.name}'.");
                }

                return;
            }

            throw new InvalidOperationException(
                "Saved QA Hub scene does not contain the Player Runtime Baseline entry.");
        }

        private static void ConfigureRouteRequestTrigger(
            RouteRequestTrigger trigger,
            ScriptableObject route,
            string reason)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }

            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            FieldInfo targetField =
                typeof(RouteRequestTrigger).GetField(
                    "targetRoute",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            if (targetField == null)
            {
                throw new InvalidOperationException(
                    "RouteRequestTrigger targetRoute field was not found.");
            }

            if (!targetField.FieldType.IsInstanceOfType(route))
            {
                throw new InvalidOperationException(
                    $"Route asset type '{route.GetType().FullName}' is not assignable to " +
                    $"RouteRequestTrigger target type '{targetField.FieldType.FullName}'.");
            }

            targetField.SetValue(trigger, route);

            var serialized =
                new SerializedObject(trigger);
            serialized.Update();

            SerializedProperty reasonProperty =
                serialized.FindProperty("reason");
            if (reasonProperty != null)
            {
                reasonProperty.stringValue =
                    reason ?? string.Empty;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(trigger.gameObject);

            object assigned =
                targetField.GetValue(trigger);

            if (!ReferenceEquals(assigned, route))
            {
                throw new InvalidOperationException(
                    "RouteRequestTrigger did not retain the assigned typed Route asset.");
            }
        }

        private static void ConfigureHubPanel(
            Component panel,
            string title,
            string label,
            RouteRequestTrigger trigger,
            Rect panelRect)
        {
            var serialized = new SerializedObject(panel);
            serialized.Update();

            SerializedProperty entries =
                serialized.FindProperty("entries");
            if (entries == null || !entries.isArray)
            {
                throw new InvalidOperationException(
                    "QaHubPanel entries field was not found.");
            }

            entries.arraySize = 1;
            SerializedProperty entry =
                entries.GetArrayElementAtIndex(0);

            SerializedProperty labelProperty =
                entry.FindPropertyRelative("label");
            SerializedProperty triggerProperty =
                entry.FindPropertyRelative(
                    "routeRequestTrigger");

            if (labelProperty == null || triggerProperty == null)
            {
                throw new InvalidOperationException(
                    "QaHubPanel entry fields were not found.");
            }

            labelProperty.stringValue = label;
            triggerProperty.objectReferenceValue = trigger;

            SetString(
                serialized,
                "title",
                title);
            SetBool(
                serialized,
                "showPanel",
                true);
            SetBool(
                serialized,
                "restrictToActiveScene",
                true);

            SerializedProperty rect =
                serialized.FindProperty("panelRect");
            if (rect != null
                && rect.propertyType
                    == SerializedPropertyType.Rect)
            {
                rect.rectValue = panelRect;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        private static void ConfigureSlot(
            PlayerSlotDeclaration slot,
            PlayerInput playerInput)
        {
            var serialized = new SerializedObject(slot);
            serialized.Update();
            SetString(serialized, "slotId", SlotId);
            SetString(serialized, "displayName", "QA Player");
            SetObject(serialized, "playerInput", playerInput);
            SetString(
                serialized,
                "reason",
                "qa.p2d.runtime-baseline");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureGate(
            UnityPlayerInputGateAdapter gate,
            PlayerInput playerInput,
            PlayerSlotDeclaration slot)
        {
            var serialized = new SerializedObject(gate);
            serialized.Update();
            SetObject(serialized, "playerInput", playerInput);
            SetString(
                serialized,
                "gameplayActionMapName",
                ActionMapName);
            SetObject(serialized, "sourceSlot", slot);
            SetBool(
                serialized,
                "blockOnInputAcceptance",
                true);
            SetBool(
                serialized,
                "blockOnGameplayAction",
                true);
            SetBool(
                serialized,
                "restorePreviousState",
                true);
            SetBool(
                serialized,
                "applyOnEnable",
                true);
            SetBool(
                serialized,
                "logStateChanges",
                true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureControlTarget(
            PlayerControlBindingTargetBehaviour target)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SetString(
                serialized,
                "bindingTargetName",
                "QA Player Control Binding Target");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBridgeTarget(
            UnityPlayerInputBridgeTargetBehaviour target,
            PlayerInput playerInput)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SetString(
                serialized,
                "bridgeTargetName",
                "QA Unity PlayerInput Bridge Target");
            SetString(
                serialized,
                "expectedPlayerSlotId",
                SlotId);
            SetObject(
                serialized,
                "playerInput",
                playerInput);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureActivationTarget(
            UnityPlayerInputActivationTargetBehaviour target,
            PlayerInput playerInput)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SetString(
                serialized,
                "activationTargetName",
                "QA Unity PlayerInput Activation Target");
            SetString(
                serialized,
                "expectedPlayerSlotId",
                SlotId);
            SetObject(
                serialized,
                "playerInput",
                playerInput);
            SetString(
                serialized,
                "actionMapName",
                ActionMapName);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFixture(
            QaP2DPlayerRuntimeBaselineFixture fixture,
            PlayerInput playerInput,
            PlayerSlotDeclaration slot,
            UnityPlayerInputGateAdapter gate,
            PlayerControlBindingTargetBehaviour controlTarget,
            UnityPlayerInputBridgeTargetBehaviour bridgeTarget,
            UnityPlayerInputActivationTargetBehaviour activationTarget)
        {
            var serialized = new SerializedObject(fixture);
            serialized.Update();
            SetObject(
                serialized,
                "playerInput",
                playerInput);
            SetObject(
                serialized,
                "playerSlot",
                slot);
            SetObject(
                serialized,
                "gateAdapter",
                gate);
            SetObject(
                serialized,
                "controlTarget",
                controlTarget);
            SetObject(
                serialized,
                "bridgeTarget",
                bridgeTarget);
            SetObject(
                serialized,
                "activationTarget",
                activationTarget);
            SetString(
                serialized,
                "expectedSlotId",
                SlotId);
            SetString(
                serialized,
                "expectedActionMap",
                ActionMapName);
            SetString(
                serialized,
                "expectedMoveAction",
                MoveActionName);
            SetBool(
                serialized,
                "runOnStart",
                true);
            SetBool(
                serialized,
                "throwOnFailure",
                false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool SetSceneReference(
            SerializedObject serialized,
            SceneAsset sceneAsset,
            string scenePath,
            string sceneName)
        {
            bool changed = false;
            SerializedProperty iterator =
                serialized.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = true;
                string normalized =
                    iterator.name.ToLowerInvariant();
                string display =
                    iterator.displayName
                        .Replace(" ", string.Empty)
                        .ToLowerInvariant();

                bool looksLikePrimaryScene =
                    normalized.Contains("primaryscene")
                    || display.Contains("primaryscene")
                    || normalized == "scene"
                    || normalized == "scenereference";

                if (!looksLikePrimaryScene
                    && !PropertyPathContains(
                        iterator.propertyPath,
                        "primaryScene"))
                {
                    continue;
                }

                if (iterator.propertyType
                    == SerializedPropertyType.ObjectReference)
                {
                    iterator.objectReferenceValue = sceneAsset;
                    changed = true;
                }
                else if (iterator.propertyType
                    == SerializedPropertyType.String)
                {
                    iterator.stringValue =
                        normalized.Contains("path")
                            ? scenePath
                            : sceneName;
                    changed = true;
                }
                else if (iterator.propertyType
                    == SerializedPropertyType.Generic)
                {
                    changed |= SetSceneReferenceChildren(
                        iterator.Copy(),
                        sceneAsset,
                        scenePath,
                        sceneName);
                }
            }

            return changed;
        }

        private static bool SetSceneReferenceChildren(
            SerializedProperty parent,
            SceneAsset sceneAsset,
            string scenePath,
            string sceneName)
        {
            SerializedProperty child = parent.Copy();
            SerializedProperty end = parent.GetEndProperty();
            bool enterChildren = true;
            bool changed = false;

            while (child.NextVisible(enterChildren)
                && !SerializedProperty.EqualContents(
                    child,
                    end))
            {
                enterChildren = true;

                if (child.propertyType
                    == SerializedPropertyType.ObjectReference
                    && (child.name.IndexOf(
                            "scene",
                            StringComparison.OrdinalIgnoreCase) >= 0
                        || child.objectReferenceValue is SceneAsset))
                {
                    child.objectReferenceValue = sceneAsset;
                    changed = true;
                }
                else if (child.propertyType
                    == SerializedPropertyType.String)
                {
                    if (child.name.IndexOf(
                        "path",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        child.stringValue = scenePath;
                        changed = true;
                    }
                    else if (child.name.IndexOf(
                        "scene",
                        StringComparison.OrdinalIgnoreCase) >= 0
                        || child.name.IndexOf(
                            "name",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        child.stringValue = sceneName;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool SetFirstString(
            SerializedObject serialized,
            string value,
            params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                SerializedProperty property =
                    serialized.FindProperty(candidates[i]);
                if (property != null
                    && property.propertyType
                        == SerializedPropertyType.String)
                {
                    property.stringValue = value;
                    return true;
                }
            }

            return false;
        }

        private static bool SetFirstObjectReferenceByCandidates(
            SerializedObject serialized,
            UnityEngine.Object value,
            params string[] candidates)
        {
            for (int i = 0; i < candidates.Length; i++)
            {
                SerializedProperty property =
                    serialized.FindProperty(candidates[i]);
                if (property != null
                    && property.propertyType
                        == SerializedPropertyType.ObjectReference)
                {
                    property.objectReferenceValue = value;
                    return true;
                }
            }

            SerializedProperty iterator =
                serialized.GetIterator();
            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType
                    != SerializedPropertyType.ObjectReference)
                {
                    continue;
                }

                if (iterator.name.IndexOf(
                    "activity",
                    StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                iterator.objectReferenceValue = value;
                return true;
            }

            return false;
        }

        private static void EnsureSceneInEditorBuildSettings(
            string scenePath)
        {
            List<EditorBuildSettingsScene> scenes =
                EditorBuildSettings.scenes.ToList();

            int index = scenes.FindIndex(scene =>
                string.Equals(
                    scene.path,
                    scenePath,
                    StringComparison.Ordinal));

            if (index >= 0)
            {
                if (!scenes[index].enabled)
                {
                    scenes[index] =
                        new EditorBuildSettingsScene(
                            scenePath,
                            true);
                    EditorBuildSettings.scenes =
                        scenes.ToArray();
                }

                return;
            }

            scenes.Add(
                new EditorBuildSettingsScene(
                    scenePath,
                    true));
            EditorBuildSettings.scenes =
                scenes.ToArray();
        }

        private static string FindUniqueAssetPath(
            string filter,
            string expectedFileName)
        {
            string[] guids =
                AssetDatabase.FindAssets(filter);

            string exact =
                guids.Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(path =>
                        string.Equals(
                            Path.GetFileName(path),
                            expectedFileName,
                            StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(exact))
            {
                return exact;
            }

            if (guids.Length == 1)
            {
                return AssetDatabase.GUIDToAssetPath(
                    guids[0]);
            }

            throw new InvalidOperationException(
                $"Could not uniquely resolve asset '{expectedFileName}' with filter '{filter}'.");
        }

        private static ScriptableObject LoadTypedRouteAsset(
            string assetPath)
        {
            FieldInfo routeField =
                typeof(RouteRequestTrigger).GetField(
                    "targetRoute",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic);

            if (routeField == null)
            {
                throw new InvalidOperationException(
                    "RouteRequestTrigger targetRoute field was not found.");
            }

            UnityEngine.Object loaded =
                AssetDatabase.LoadAssetAtPath(
                    assetPath,
                    routeField.FieldType);

            return loaded as ScriptableObject;
        }

        private static T LoadAssetByGuid<T>(
            string guid)
            where T : UnityEngine.Object
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static GameObject FindRoot(
            Scene scene,
            string name)
        {
            GameObject[] roots =
                scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null
                    && string.Equals(
                        roots[i].name,
                        name,
                        StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static Type FindType(
            string fullName)
        {
            Assembly[] assemblies =
                AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                Type type =
                    assemblies[i].GetType(
                        fullName,
                        false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static bool PropertyPathContains(
            string propertyPath,
            string token)
        {
            return !string.IsNullOrEmpty(propertyPath)
                && propertyPath.IndexOf(
                    token,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void CopyAssetReplacing(
            string sourcePath,
            string destinationPath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new InvalidOperationException(
                    "Source asset path is empty.");
            }

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                destinationPath) != null)
            {
                AssetDatabase.DeleteAsset(destinationPath);
            }

            if (!AssetDatabase.CopyAsset(
                sourcePath,
                destinationPath))
            {
                throw new InvalidOperationException(
                    $"Could not copy '{sourcePath}' to '{destinationPath}'.");
            }

            AssetDatabase.ImportAsset(
                destinationPath,
                ImportAssetOptions.ForceSynchronousImport
                | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(
                ImportAssetOptions.ForceSynchronousImport);
        }

        private static void EnsureFolder(
            string parent,
            string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(
                    parent,
                    child);
            }
        }

        private static void SetString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null
                && property.propertyType
                    == SerializedPropertyType.String)
            {
                property.stringValue =
                    value ?? string.Empty;
            }
        }

        private static void SetBool(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null
                && property.propertyType
                    == SerializedPropertyType.Boolean)
            {
                property.boolValue = value;
            }
        }

        private static void SetObject(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            if (property != null
                && property.propertyType
                    == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = value;
            }
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("'", "\\'");
        }
    }
}

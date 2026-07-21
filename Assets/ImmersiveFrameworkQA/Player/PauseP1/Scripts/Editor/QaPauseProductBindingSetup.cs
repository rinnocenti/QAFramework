using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.Hub.Editor;
using ImmersiveFrameworkQA.PauseP1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.PauseP1.Editor
{
    internal static class QaPauseProductBindingPaths
    {
        internal const string Root = "Assets/ImmersiveFrameworkQA/Player/PauseP1";
        internal const string InputActions =
            "Assets/ImmersiveFrameworkQA/Player/P3G4/P3G4_InputActions.asset";
        internal const string PlayerHost =
            "Assets/ImmersiveFrameworkQA/Player/P3G4/P3G4_LocalPlayerHost.prefab";
        internal const string ActionReference =
            Root + "/P3G4_PauseToggle.inputactionreference.asset";
        internal const string Route = Root + "/QA_PauseP1Route.asset";
        internal const string Activity = Root + "/QA_PauseP1Activity.asset";
        internal const string Content = Root + "/QA_PauseP1ActivityContent.asset";
        internal const string RouteScene = Root + "/QA_PauseP1Route.unity";
        internal const string ActivityScene = Root + "/QA_PauseP1ActivityContent.unity";
        internal const string HubRoute =
            "Assets/ImmersiveFrameworkQA/Hub/Routes/QA_HubRoute.asset";
        internal const string HubScene =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        internal const string UiGlobalScene =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";
        internal const string PreflightSource =
            Root + "/Scripts/Runtime/PauseOfficialPlayerPreflightPanel.cs";
        internal const string Projection =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/PlayerParticipation/ActivityParticipation_AllJoined_AtLeastOne.asset";
        internal const string Requirements =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/PlayerParticipation/Player Participation — Joined Slots.asset";
        internal const string PauseAction = "Global/PauseToggle";
    }

    public static class QaPauseProductBindingSetup
    {
        private const string LogPrefix = "[PAUSE_PRODUCT_BINDING_SETUP]";

        public static void CreateOrRefresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError($"{LogPrefix} status='RejectedPlayMode'");
                return;
            }

            try
            {
                InputActionAsset actions = ConfigureInputActions();
                InputAction pauseAction = actions.FindAction(
                    QaPauseProductBindingPaths.PauseAction,
                    true);
                InputActionReference pauseReference =
                    CreateOrUpdateActionReference(pauseAction);
                ConfigureOfficialPlayerHost(actions, pauseReference);
                ConfigureOfficialPlayerPreflight();

                ActivityContentProfileAsset content = CreateOrUpdateContent();
                ActivityAsset activity = CreateOrUpdateActivity(content);
                RouteAsset route = CreateOrUpdateRoute(activity);
                CreateOrUpdateRouteScene();
                CreateOrUpdateActivityScene();
                EnsureSceneInBuildSettings(QaPauseProductBindingPaths.RouteScene);
                EnsureSceneInBuildSettings(QaPauseProductBindingPaths.ActivityScene);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                QaHubSceneBuilder.CreateOrRefreshHubForSetup();
                AssetDatabase.SaveAssets();

                QaPauseProductBindingValidation result =
                    QaPauseProductBindingStaticValidator.Validate(
                        actions,
                        pauseAction,
                        pauseReference,
                        route,
                        activity,
                        content);

                Debug.Log(
                    $"{LogPrefix} status='Applied' " +
                    $"route='{route.RouteName}' " +
                    $"activity='{activity.ActivityName}' " +
                    "playerHost='P3G4_LocalPlayerHost' " +
                    $"pauseBinding='{result.PauseBindingCount}' " +
                    $"playerInputsInPauseScenes='{result.PlayerInputsInPauseScenes}' " +
                    $"hubEntries='{result.HubEntryCount}' " +
                    $"duplicates='{result.DuplicateCount}'");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' " +
                    $"reason='{Escape(exception.GetBaseException().Message)}'");
                throw;
            }
        }

        private static InputActionAsset ConfigureInputActions()
        {
            InputActionAsset asset =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    QaPauseProductBindingPaths.InputActions);
            Require(asset != null,
                $"Official action asset is missing: '{QaPauseProductBindingPaths.InputActions}'.");

            InputActionMap gameplay = GetExactlyOneMap(
                asset,
                "Gameplay",
                createIfMissing: false);
            Require(gameplay.FindAction("JoinEvidence", false) != null,
                "Gameplay/JoinEvidence must be preserved.");
            InputActionMap global = GetExactlyOneMap(
                asset,
                "Global",
                createIfMissing: true);
            GetExactlyOneMap(asset, "UI", createIfMissing: true);

            InputAction[] pauseActions = global.actions
                .Where(action => string.Equals(
                    action.name,
                    "PauseToggle",
                    StringComparison.Ordinal))
                .ToArray();
            Require(pauseActions.Length <= 1,
                "Duplicate Global/PauseToggle actions are not accepted.");
            InputAction pauseAction = pauseActions.Length == 1
                ? pauseActions[0]
                : global.AddAction("PauseToggle", InputActionType.Button);

            EnsureSingleBinding(pauseAction, "<Keyboard>/escape");
            EnsureSingleBinding(pauseAction, "<Gamepad>/start");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            return asset;
        }

        private static InputActionMap GetExactlyOneMap(
            InputActionAsset asset,
            string name,
            bool createIfMissing)
        {
            InputActionMap[] maps = asset.actionMaps
                .Where(map => string.Equals(
                    map.name,
                    name,
                    StringComparison.Ordinal))
                .ToArray();
            Require(maps.Length <= 1,
                $"Duplicate '{name}' action maps are not accepted.");
            if (maps.Length == 1)
            {
                return maps[0];
            }

            Require(createIfMissing,
                $"Official action map '{name}' is missing.");
            return asset.AddActionMap(name);
        }

        private static void EnsureSingleBinding(InputAction action, string path)
        {
            int count = action.bindings.Count(binding =>
                string.Equals(
                    binding.path,
                    path,
                    StringComparison.OrdinalIgnoreCase));
            if (count == 0)
            {
                action.AddBinding(path);
                count = 1;
            }

            Require(count == 1,
                $"Action '{action.actionMap?.name}/{action.name}' requires exactly one '{path}' binding; found '{count}'.");
        }

        private static InputActionReference CreateOrUpdateActionReference(
            InputAction pauseAction)
        {
            InputActionReference reference =
                AssetDatabase.LoadAssetAtPath<InputActionReference>(
                    QaPauseProductBindingPaths.ActionReference);
            if (reference == null)
            {
                Require(
                    AssetDatabase.LoadMainAssetAtPath(
                        QaPauseProductBindingPaths.ActionReference) == null,
                    "Pause action reference path is occupied by an incompatible asset.");
                reference = InputActionReference.Create(pauseAction);
                AssetDatabase.CreateAsset(
                    reference,
                    QaPauseProductBindingPaths.ActionReference);
            }
            else
            {
                reference.Set(pauseAction);
            }

            reference.name = "P3G4_PauseToggle";
            EditorUtility.SetDirty(reference);
            AssetDatabase.SaveAssetIfDirty(reference);
            return reference;
        }

        private static void ConfigureOfficialPlayerHost(
            InputActionAsset actionAsset,
            InputActionReference pauseReference)
        {
            Require(
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    QaPauseProductBindingPaths.PlayerHost) != null,
                "Official P3G4 Local Player host prefab is missing.");

            GameObject root = PrefabUtility.LoadPrefabContents(
                QaPauseProductBindingPaths.PlayerHost);
            try
            {
                PlayerInput[] inputs =
                    root.GetComponentsInChildren<PlayerInput>(true);
                Require(inputs.Length == 1,
                    $"Official host requires exactly one PlayerInput; found '{inputs.Length}'.");
                PlayerInput input = inputs[0];
                input.actions = actionAsset;
                input.defaultActionMap = "Gameplay";
                EditorUtility.SetDirty(input);

                UnityPlayerInputGateAdapter[] gates =
                    root.GetComponentsInChildren<UnityPlayerInputGateAdapter>(true);
                Require(gates.Length == 1,
                    $"Official host requires exactly one UnityPlayerInputGateAdapter; found '{gates.Length}'.");
                Require(ReferenceEquals(gates[0].gameObject, input.gameObject),
                    "Input Gate adapter must share the PlayerInput GameObject.");
                SetObject(gates[0], "playerInput", input);
                SetString(gates[0], "gameplayActionMapName", "Gameplay");

                PausePlayerInputBinding[] bindings =
                    root.GetComponentsInChildren<PausePlayerInputBinding>(true);
                Require(bindings.Length <= 1,
                    $"Duplicate PausePlayerInputBinding components are rejected; found '{bindings.Length}'.");
                PausePlayerInputBinding binding = bindings.Length == 1
                    ? bindings[0]
                    : input.gameObject.AddComponent<PausePlayerInputBinding>();
                Require(ReferenceEquals(binding.gameObject, input.gameObject),
                    "Pause binding must share the official PlayerInput GameObject.");
                SetObject(binding, "playerInput", input);
                SetObject(binding, "pauseAction", pauseReference);
                SetString(binding, "globalActionMapName", "Global");
                SetString(binding, "gameplayActionMapName", "Gameplay");
                SetString(binding, "uiActionMapName", "UI");

                Require(
                    PrefabUtility.SaveAsPrefabAsset(
                        root,
                        QaPauseProductBindingPaths.PlayerHost) != null,
                    "Could not save the official P3G4 Local Player host.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureOfficialPlayerPreflight()
        {
            Scene existing = SceneManager.GetSceneByPath(
                QaPauseProductBindingPaths.UiGlobalScene);
            bool opened = !existing.IsValid() || !existing.isLoaded;
            Scene scene = opened
                ? EditorSceneManager.OpenScene(
                    QaPauseProductBindingPaths.UiGlobalScene,
                    OpenSceneMode.Additive)
                : existing;

            try
            {
                LocalPlayerProvisioningAuthoring[] authorings =
                    scene.GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<
                                LocalPlayerProvisioningAuthoring>(true))
                        .ToArray();
                PlayerInputManager[] managers =
                    scene.GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<PlayerInputManager>(true))
                        .ToArray();
                PauseOfficialPlayerPreflightPanel[] preflights =
                    scene.GetRootGameObjects()
                        .SelectMany(root =>
                            root.GetComponentsInChildren<
                                PauseOfficialPlayerPreflightPanel>(true))
                        .ToArray();

                Require(authorings.Length == 1,
                    $"QA_UIGlobal requires exactly one LocalPlayerProvisioningAuthoring; found '{authorings.Length}'.");
                Require(managers.Length == 1,
                    $"QA_UIGlobal requires exactly one PlayerInputManager; found '{managers.Length}'.");
                Require(preflights.Length <= 1,
                    $"Duplicate Pause official Player preflight components are rejected; found '{preflights.Length}'.");
                Require(ReferenceEquals(
                        authorings[0].PlayerInputManager,
                        managers[0]),
                    "QA_UIGlobal provisioning must own the existing PlayerInputManager.");

                PauseOfficialPlayerPreflightPanel preflight =
                    preflights.Length == 1
                        ? preflights[0]
                        : authorings[0].gameObject.AddComponent<
                            PauseOfficialPlayerPreflightPanel>();
                SetObject(
                    preflight,
                    "provisioningAuthoring",
                    authorings[0]);

                EditorSceneManager.MarkSceneDirty(scene);
                Require(
                    EditorSceneManager.SaveScene(
                        scene,
                        QaPauseProductBindingPaths.UiGlobalScene,
                        false),
                    "Could not save QA_UIGlobal with the Pause official Player preflight.");
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static ActivityContentProfileAsset CreateOrUpdateContent()
        {
            ActivityContentProfileAsset content =
                LoadOrCreate<ActivityContentProfileAsset>(
                    QaPauseProductBindingPaths.Content);
            content.name = "QA_PauseP1ActivityContent";
            var serialized = new SerializedObject(content);
            SetString(serialized, "profileId", "qa.pause.p1.activity-content");
            SetString(serialized, "description",
                "Official Pause Product Binding QA Activity content.");
            SerializedProperty scenes = Property(serialized, "scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            SetString(entry, "contentId", "qa.pause.p1.activity-content.scene");
            SetString(entry, "scenePath", QaPauseProductBindingPaths.ActivityScene);
            SetString(entry, "sceneName", "QA_PauseP1ActivityContent");
            SetEnum(entry.FindPropertyRelative("requiredness"), "Required");
            SetEnum(entry.FindPropertyRelative("loadMode"), "Additive");
            SetEnum(entry.FindPropertyRelative("releasePolicy"),
                "ReleaseOnActivityChange");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(content);
            return content;
        }

        private static ActivityAsset CreateOrUpdateActivity(
            ActivityContentProfileAsset content)
        {
            ActivityParticipationProjectionProfile projection =
                AssetDatabase.LoadAssetAtPath<
                    ActivityParticipationProjectionProfile>(
                    QaPauseProductBindingPaths.Projection);
            PlayerParticipationRequirementsProfile requirements =
                AssetDatabase.LoadAssetAtPath<
                    PlayerParticipationRequirementsProfile>(
                    QaPauseProductBindingPaths.Requirements);
            Require(projection != null,
                "Pause QA All Joined projection profile is missing.");
            Require(requirements != null,
                "Pause QA Joined Slots requirements profile is missing.");

            ActivityAsset activity = LoadOrCreate<ActivityAsset>(
                QaPauseProductBindingPaths.Activity);
            activity.name = "QA_PauseP1Activity";
            var serialized = new SerializedObject(activity);
            SetString(serialized, "activityId", "qa.pause.p1.product-binding");
            SetString(serialized, "activityName", "QA Pause P1 Activity");
            SetString(serialized, "description",
                "Declares required Pause product binding for the officially admitted Local Player.");
            SetObject(serialized, "playerParticipationProjectionProfile", projection);
            SetObject(serialized, "playerParticipationRequirementsProfile", requirements);
            SetObject(serialized, "activityContentProfile", content);
            SetEnum(Property(serialized, "visualTransitionMode"), "Seamless");
            SetEnum(Property(serialized, "transitionGateMode"),
                "LifecycleRequestsOnly");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);
            return activity;
        }

        private static RouteAsset CreateOrUpdateRoute(ActivityAsset activity)
        {
            RouteAsset route = LoadOrCreate<RouteAsset>(
                QaPauseProductBindingPaths.Route);
            route.name = "QA_PauseP1Route";
            var serialized = new SerializedObject(route);
            SetString(serialized, "routeName", "QA Pause P1 Route");
            SetString(serialized, "primaryScenePath",
                QaPauseProductBindingPaths.RouteScene);
            SetString(serialized, "primarySceneName", "QA_PauseP1Route");
            SetObject(serialized, "routeContentProfile", null);
            SetObject(serialized, "startupActivity", activity);
            SetEnum(Property(serialized, "transitionGateMode"),
                "InputInteractionAndGameplay");
            SetString(serialized, "description",
                "Official Pause Product Binding QA Route with explicit return to QA Hub.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);
            return route;
        }

        private static void CreateOrUpdateRouteScene()
        {
            RouteAsset hubRoute = AssetDatabase.LoadAssetAtPath<RouteAsset>(
                QaPauseProductBindingPaths.HubRoute);
            Require(hubRoute != null, "QA Hub Route is missing.");

            ReplaceScene(
                QaPauseProductBindingPaths.RouteScene,
                scene =>
                {
                    GameObject root = NewSceneObject(
                        "Pause Product Binding QA",
                        scene);
                    RouteRequestTrigger trigger =
                        root.AddComponent<RouteRequestTrigger>();
                    SetObject(trigger, "targetRoute", hubRoute);
                    SetString(trigger, "reason",
                        "qa.pause.product-binding.back-to-hub");
                    QaHubReturnPanel panel =
                        root.AddComponent<QaHubReturnPanel>();
                    panel.Configure(
                        trigger,
                        "Pause Product Binding QA",
                        "Official vertical: Escape or Gamepad Start toggles Pause through the framework-provisioned Local Player. The Activity declares Pause intent; it does not own PlayerInput.");
                    EditorUtility.SetDirty(panel);
                });
        }

        private static void CreateOrUpdateActivityScene()
        {
            ReplaceScene(
                QaPauseProductBindingPaths.ActivityScene,
                scene =>
                {
                    GameObject root = NewSceneObject(
                        "Pause Activity Intent - Official Local Player Binding Required",
                        scene);
                    root.AddComponent<PauseActivityBindingAuthoring>();
                    PauseQaIntentPanel panel =
                        root.AddComponent<PauseQaIntentPanel>();
                    panel.Configure(
                        "Pause Activity Intent",
                        "Required",
                        "This Activity declares Pause binding intent. Input remains owned by the framework-provisioned Local Player host.");
                    EditorUtility.SetDirty(panel);
                });
        }

        private static void ReplaceScene(string path, Action<Scene> populate)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Additive);
            try
            {
                scene.name = Path.GetFileNameWithoutExtension(path);
                populate(scene);
                Require(EditorSceneManager.SaveScene(scene, path, false),
                    $"Could not save scene '{path}'.");
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }

                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }
            }
        }

        private static void EnsureSceneInBuildSettings(string path)
        {
            string normalized = path.Replace(Path.DirectorySeparatorChar, '/');
            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            int found = -1;
            for (int index = scenes.Count - 1; index >= 0; index--)
            {
                if (!string.Equals(
                        scenes[index].path.Replace(Path.DirectorySeparatorChar, '/'),
                        normalized,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (found < 0)
                {
                    found = index;
                    scenes[index] =
                        new EditorBuildSettingsScene(normalized, true);
                }
                else
                {
                    scenes.RemoveAt(index);
                    found--;
                }
            }

            if (found < 0)
            {
                scenes.Add(new EditorBuildSettingsScene(normalized, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            if (value != null)
            {
                return value;
            }

            Require(AssetDatabase.LoadMainAssetAtPath(path) == null,
                $"Asset path is occupied by an incompatible asset: '{path}'.");
            value = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        private static GameObject NewSceneObject(string name, Scene scene)
        {
            var value = new GameObject(name);
            SceneManager.MoveGameObjectToScene(value, scene);
            return value;
        }

        private static void SetObject(
            UnityEngine.Object target,
            string property,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SetObject(serialized, property, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(
            UnityEngine.Object target,
            string property,
            string value)
        {
            var serialized = new SerializedObject(target);
            SetString(serialized, property, value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObject(
            SerializedObject serialized,
            string property,
            UnityEngine.Object value) =>
            Property(serialized, property).objectReferenceValue = value;

        private static void SetString(
            SerializedObject serialized,
            string property,
            string value) =>
            Property(serialized, property).stringValue =
                value ?? string.Empty;

        private static void SetString(
            SerializedProperty parent,
            string property,
            string value)
        {
            SerializedProperty child =
                parent.FindPropertyRelative(property);
            Require(child != null,
                $"Missing serialized property '{parent.propertyPath}.{property}'.");
            child.stringValue = value ?? string.Empty;
        }

        private static SerializedProperty Property(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            Require(property != null,
                $"Missing serialized property '{serialized.targetObject.GetType().Name}.{name}'.");
            return property;
        }

        private static void SetEnum(
            SerializedProperty property,
            string value)
        {
            Require(property != null,
                $"Missing enum property for '{value}'.");
            for (int index = 0;
                 index < property.enumNames.Length;
                 index++)
            {
                if (string.Equals(
                        property.enumNames[index],
                        value,
                        StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Enum value '{value}' is unavailable for '{property.propertyPath}'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "’")
                    .Replace(Environment.NewLine, " ");
    }
}

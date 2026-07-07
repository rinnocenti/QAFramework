using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Audio;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Audio.Editor
{
    /// <summary>
    /// Editor-only, idempotent setup for the QA Route/Activity BGM adapter fixture.
    /// </summary>
    public static class FrameworkBgmQaSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Audio";
        private const string Scenes = Root + "/Scenes";
        private const string ScriptableObjects = Root + "/ScriptableObjects";
        private const string Routes = Root + "/Routes";
        private const string Activities = Root + "/Activities";

        private const string CanonicalScenePath = Scenes + "/QA_FrameworkBgm.unity";
        private const string AlternateScenePath = Scenes + "/QA_FrameworkBgmRouteB.unity";

        private const string CanonicalRoutePath = Routes + "/QA_FrameworkBgmRoute.asset";
        private const string AlternateRoutePath = Routes + "/QA_FrameworkBgmRouteB.asset";

        private const string StartupActivityPath = Activities + "/QA_FrameworkBgmStartupActivity.asset";
        private const string OwnActivityPath = Activities + "/QA_FrameworkBgmOwnActivity.asset";
        private const string RetainActivityPath = Activities + "/QA_FrameworkBgmRetainPreviousActivity.asset";
        private const string RouteFallbackActivityPath = Activities + "/QA_FrameworkBgmRouteFallbackActivity.asset";
        private const string SilenceActivityPath = Activities + "/QA_FrameworkBgmSilenceActivity.asset";

        private const string DefaultsPath = ScriptableObjects + "/QA_AudioDefaults.asset";
        private const string RouteCuePath = ScriptableObjects + "/QA_FrameworkBgm_RouteCue.asset";
        private const string AlternateRouteCuePath = ScriptableObjects + "/QA_FrameworkBgm_RouteBCue.asset";
        private const string StartupActivityCuePath = ScriptableObjects + "/QA_FrameworkBgm_StartupActivityCue.asset";
        private const string OwnActivityCuePath = ScriptableObjects + "/QA_FrameworkBgm_ActivityCue.asset";

        [MenuItem("Immersive Framework QA/Audio/Configure Framework BGM Route-Activity QA")]
        public static void ConfigureFrameworkBgmQa()
        {
            EnsureFolders();

            AudioQaGeneratedClips generatedClips = AudioQaGeneratedClipRepair.EnsureGeneratedClipsAndAssignments();
            if (!generatedClips.IsValid)
            {
                Debug.LogError("[FRAMEWORK_BGM_QA_SETUP] Framework BGM QA setup aborted because generated clips are not valid.");
                return;
            }

            AudioDefaultsAsset defaults = CreateDefaultsAsset();
            AudioBgmCueAsset routeCue = CreateBgmCue(RouteCuePath, "qa.framework.bgm.route", generatedClips.BgmClip, 0.45f);
            AudioBgmCueAsset alternateRouteCue = CreateBgmCue(AlternateRouteCuePath, "qa.framework.bgm.route-b", generatedClips.BgmClip, 0.5f);
            AudioBgmCueAsset startupCue = CreateBgmCue(StartupActivityCuePath, "qa.framework.bgm.startup-activity", generatedClips.BgmClip, 0.55f);
            AudioBgmCueAsset ownActivityCue = CreateBgmCue(OwnActivityCuePath, "qa.framework.bgm.activity-own", generatedClips.BgmClip, 0.6f);

            ActivityAsset startupActivity = CreateActivityAsset(StartupActivityPath, "QA Framework BGM Startup Activity", "Startup Activity BGM pre-apply.");
            ActivityAsset ownActivity = CreateActivityAsset(OwnActivityPath, "QA Framework BGM Own Activity", "Activity with its own retained BGM.");
            ActivityAsset retainActivity = CreateActivityAsset(RetainActivityPath, "QA Framework BGM Retain Previous Activity", "Activity with no own BGM that retains previous Activity BGM.");
            ActivityAsset routeFallbackActivity = CreateActivityAsset(RouteFallbackActivityPath, "QA Framework BGM Route Fallback Activity", "Activity that explicitly uses Route BGM.");
            ActivityAsset silenceActivity = CreateActivityAsset(SilenceActivityPath, "QA Framework BGM Silence Activity", "Activity that explicitly stops BGM.");

            RouteAsset canonicalRoute = CreateRouteAsset(
                CanonicalRoutePath,
                "QA Framework BGM Route",
                CanonicalScenePath,
                "Canonical Framework BGM QA route.",
                startupActivity);

            RouteAsset alternateRoute = CreateRouteAsset(
                AlternateRoutePath,
                "QA Framework BGM Route B",
                AlternateScenePath,
                "Alternate Framework BGM QA route used to prove retained Activity cleanup.",
                null);

            bool canonicalConfigured = ConfigureScene(
                CanonicalScenePath,
                "Canonical",
                canonicalRoute,
                alternateRoute,
                startupActivity,
                ownActivity,
                retainActivity,
                routeFallbackActivity,
                silenceActivity,
                defaults,
                routeCue,
                startupCue,
                ownActivityCue,
                new Vector3(0f, 2f, -10f),
                Color.black);

            bool alternateConfigured = ConfigureScene(
                AlternateScenePath,
                "Alternate",
                alternateRoute,
                canonicalRoute,
                startupActivity,
                ownActivity,
                retainActivity,
                routeFallbackActivity,
                silenceActivity,
                defaults,
                alternateRouteCue,
                null,
                ownActivityCue,
                new Vector3(4f, 2f, -10f),
                new Color(0.02f, 0.04f, 0.07f, 1f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (canonicalConfigured && alternateConfigured)
            {
                Debug.Log("[FRAMEWORK_BGM_QA_SETUP] Framework BGM Route/Activity QA configured under Assets/ImmersiveFrameworkQA/Audio.");
                return;
            }

            Debug.LogError("[FRAMEWORK_BGM_QA_SETUP] Framework BGM QA configuration completed with blocking setup errors. Fix the errors above before running the smoke.");
        }

        private static bool ConfigureScene(
            string scenePath,
            string label,
            RouteAsset activeRoute,
            RouteAsset otherRoute,
            ActivityAsset startupActivity,
            ActivityAsset ownActivity,
            ActivityAsset retainActivity,
            ActivityAsset routeFallbackActivity,
            ActivityAsset silenceActivity,
            AudioDefaultsAsset defaults,
            AudioBgmCueAsset routeCue,
            AudioBgmCueAsset startupCue,
            AudioBgmCueAsset ownActivityCue,
            Vector3 cameraPosition,
            Color backgroundColor)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            CreateCamera(cameraPosition, backgroundColor);
            CreateLight(label);

            GameObject root = EnsureRoot(scene, $"QA_FrameworkBgmRoot_{label}");
            RouteContentBinding routeContentBinding = EnsureComponent<RouteContentBinding>(root);
            AudioRuntimeHost runtimeHost = EnsureAudioRuntimeHost(root.transform, defaults);
            FrameworkBgmDirector director = EnsureComponent<FrameworkBgmDirector>(root);
            FrameworkRouteBgmBinding routeBgmBinding = EnsureComponent<FrameworkRouteBgmBinding>(root);

            SetSerialized(routeContentBinding, "route", activeRoute);
            SetSerialized(routeContentBinding, "localContentId", $"qa-framework-bgm-route-{label.ToLowerInvariant()}");
            SetSerialized(routeContentBinding, "requiredness", 10);

            SetSerialized(director, "audioRuntimeHost", runtimeHost);
            SetSerialized(director, "logTransitions", true);

            FrameworkActivityBgmBinding startupBinding = null;
            if (startupCue != null)
            {
                startupBinding = EnsureActivityBinding(
                    scene,
                    $"QA_FrameworkBgm_Activity_Startup_{label}",
                    startupActivity,
                    startupCue,
                    FrameworkBgmActivityPolicy.UseOwnOrRoute,
                    director,
                    "startup");
            }

            EnsureActivityBinding(
                scene,
                $"QA_FrameworkBgm_Activity_Own_{label}",
                ownActivity,
                ownActivityCue,
                FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit,
                director,
                "own");

            EnsureActivityBinding(
                scene,
                $"QA_FrameworkBgm_Activity_RetainPrevious_{label}",
                retainActivity,
                null,
                FrameworkBgmActivityPolicy.UseOwnOrRetainActivityUntilRouteExit,
                director,
                "retain-previous");

            EnsureActivityBinding(
                scene,
                $"QA_FrameworkBgm_Activity_RouteFallback_{label}",
                routeFallbackActivity,
                null,
                FrameworkBgmActivityPolicy.UseRoute,
                director,
                "route-fallback");

            EnsureActivityBinding(
                scene,
                $"QA_FrameworkBgm_Activity_Silence_{label}",
                silenceActivity,
                null,
                FrameworkBgmActivityPolicy.Silence,
                director,
                "silence");

            SetSerialized(routeBgmBinding, "routeBgm", routeCue);
            SetSerialized(routeBgmBinding, "director", director);
            SetSerialized(routeBgmBinding, "startupActivityBgmBinding", startupBinding);

            ConfigurePanel(
                scene,
                label,
                otherRoute,
                startupActivity,
                ownActivity,
                retainActivity,
                routeFallbackActivity,
                silenceActivity,
                director,
                routeCue,
                startupCue,
                ownActivityCue);

            bool valid = ValidateObjectReference(routeContentBinding, "route", activeRoute, $"RouteContentBinding route on '{root.name}'")
                & ValidateObjectReference(routeBgmBinding, "routeBgm", routeCue, $"FrameworkRouteBgmBinding routeBgm on '{root.name}'")
                & ValidateObjectReference(routeBgmBinding, "director", director, $"FrameworkRouteBgmBinding director on '{root.name}'");

            if (startupCue != null)
            {
                valid &= ValidateObjectReference(routeBgmBinding, "startupActivityBgmBinding", startupBinding, $"FrameworkRouteBgmBinding startupActivityBgmBinding on '{root.name}'");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, scenePath);
            return valid;
        }

        private static FrameworkActivityBgmBinding EnsureActivityBinding(
            Scene scene,
            string rootName,
            ActivityAsset activity,
            AudioBgmCueAsset cue,
            FrameworkBgmActivityPolicy policy,
            FrameworkBgmDirector director,
            string contentIdSuffix)
        {
            GameObject root = EnsureRoot(scene, rootName);
            ActivityLocalVisibilityAdapter contentBinding = EnsureComponent<ActivityLocalVisibilityAdapter>(root);
            FrameworkActivityBgmBinding bgmBinding = EnsureComponent<FrameworkActivityBgmBinding>(root);

            SetSerialized(contentBinding, "activity", activity);
            SetSerialized(contentBinding, "localContentId", $"qa-framework-bgm-activity-{contentIdSuffix}-{scene.name.ToLowerInvariant()}");
            SetSerialized(contentBinding, "requiredness", 10);

            SetSerialized(bgmBinding, "assignedActivity", activity);
            SetSerialized(bgmBinding, "activityBgm", cue);
            SetSerialized(bgmBinding, "policy", (int)policy);
            SetSerialized(bgmBinding, "director", director);

            ValidateObjectReference(contentBinding, "activity", activity, $"ActivityLocalVisibilityAdapter activity on '{rootName}'");
            ValidateObjectReference(bgmBinding, "assignedActivity", activity, $"FrameworkActivityBgmBinding assignedActivity on '{rootName}'");
            ValidateObjectReference(bgmBinding, "director", director, $"FrameworkActivityBgmBinding director on '{rootName}'");
            return bgmBinding;
        }

        private static void ConfigurePanel(
            Scene scene,
            string label,
            RouteAsset otherRoute,
            ActivityAsset startupActivity,
            ActivityAsset ownActivity,
            ActivityAsset retainActivity,
            ActivityAsset routeFallbackActivity,
            ActivityAsset silenceActivity,
            FrameworkBgmDirector director,
            AudioBgmCueAsset routeCue,
            AudioBgmCueAsset startupCue,
            AudioBgmCueAsset ownActivityCue)
        {
            GameObject panelObject = EnsureRoot(scene, $"QA_FrameworkBgmPanel_{label}");
            FrameworkBgmQaPanel panel = EnsureComponent<FrameworkBgmQaPanel>(panelObject);

            RouteRequestTrigger routeTrigger = EnsureRouteTrigger(
                scene,
                panelObject.transform,
                $"QA_FrameworkBgm_RouteRequest_{label}",
                otherRoute,
                $"qa.framework-bgm.route.{label.ToLowerInvariant()}.other");

            ActivityRequestTrigger startupTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_FrameworkBgm_ActivityRequest_Startup_{label}",
                startupActivity,
                $"qa.framework-bgm.activity.{label.ToLowerInvariant()}.startup");

            ActivityRequestTrigger ownTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_FrameworkBgm_ActivityRequest_Own_{label}",
                ownActivity,
                $"qa.framework-bgm.activity.{label.ToLowerInvariant()}.own");

            ActivityRequestTrigger retainTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_FrameworkBgm_ActivityRequest_RetainPrevious_{label}",
                retainActivity,
                $"qa.framework-bgm.activity.{label.ToLowerInvariant()}.retain-previous");

            ActivityRequestTrigger routeFallbackTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_FrameworkBgm_ActivityRequest_RouteFallback_{label}",
                routeFallbackActivity,
                $"qa.framework-bgm.activity.{label.ToLowerInvariant()}.route-fallback");

            ActivityRequestTrigger silenceTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_FrameworkBgm_ActivityRequest_Silence_{label}",
                silenceActivity,
                $"qa.framework-bgm.activity.{label.ToLowerInvariant()}.silence");

            ActivityRequestTrigger clearTrigger = EnsureActivityTrigger(
                scene,
                panelObject.transform,
                $"QA_FrameworkBgm_ActivityRequest_Clear_{label}",
                null,
                $"qa.framework-bgm.activity.{label.ToLowerInvariant()}.clear");

            panel.Configure(
                routeTrigger,
                startupTrigger,
                ownTrigger,
                retainTrigger,
                routeFallbackTrigger,
                silenceTrigger,
                clearTrigger,
                $"QA Framework BGM {label}",
                director,
                routeCue,
                startupCue,
                ownActivityCue);
            panel.ConfigureLayout(new Rect(16f, 16f, 700f, 740f), new Vector2(480f, 440f), 640f);
            EditorUtility.SetDirty(panel);
        }

        private static RouteRequestTrigger EnsureRouteTrigger(Scene scene, Transform parent, string name, RouteAsset targetRoute, string reason)
        {
            GameObject go = EnsureChild(scene, parent, name);
            RouteRequestTrigger trigger = EnsureComponent<RouteRequestTrigger>(go);
            trigger.TargetRoute = targetRoute;
            SetSerialized(trigger, "targetRoute", targetRoute);
            SetSerialized(trigger, "reason", reason);
            EditorUtility.SetDirty(trigger);
            return trigger;
        }

        private static ActivityRequestTrigger EnsureActivityTrigger(Scene scene, Transform parent, string name, ActivityAsset targetActivity, string reason)
        {
            GameObject go = EnsureChild(scene, parent, name);
            ActivityRequestTrigger trigger = EnsureComponent<ActivityRequestTrigger>(go);
            trigger.TargetActivity = targetActivity;
            SetSerialized(trigger, "targetActivity", targetActivity);
            SetSerialized(trigger, "reason", reason);
            EditorUtility.SetDirty(trigger);
            return trigger;
        }

        private static AudioRuntimeHost EnsureAudioRuntimeHost(Transform parent, AudioDefaultsAsset defaults)
        {
            GameObject hostObject = EnsureChild(parent.gameObject.scene, parent, "QA_FrameworkBgm_AudioRuntimeHost");
            GameObject playbackRoot = EnsureChild(parent.gameObject.scene, hostObject.transform, "AudioPlayback");

            AudioRuntimeHost host = EnsureComponent<AudioRuntimeHost>(hostObject);
            SetSerialized(host, "defaults", defaults);
            SetSerialized(host, "playbackRoot", playbackRoot.transform);
            SetSerialized(host, "composeOnAwake", true);
            return host;
        }

        private static AudioDefaultsAsset CreateDefaultsAsset()
        {
            AudioDefaultsAsset asset = LoadOrCreate<AudioDefaultsAsset>(DefaultsPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetSerialized(serialized, "masterVolume", 1f);
            SetSerialized(serialized, "sfxVolume", 1f);
            SetSerialized(serialized, "bgmVolume", 0.35f);
            SetSerialized(serialized, "masterBus", AudioBusKeys.Master);
            SetSerialized(serialized, "sfxBus", AudioBusKeys.Sfx);
            SetSerialized(serialized, "bgmBus", AudioBusKeys.Bgm);
            SetSerialized(serialized, "defaultFadeInSeconds", 0.05f);
            SetSerialized(serialized, "defaultFadeOutSeconds", 0.05f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static AudioBgmCueAsset CreateBgmCue(string assetPath, string cueId, AudioClip clip, float volume)
        {
            AudioBgmCueAsset asset = LoadOrCreate<AudioBgmCueAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetSerialized(serialized, "cueId", cueId);
            SetSerialized(serialized, "clip", clip);
            SetSerialized(serialized, "volume", volume);
            SetSerialized(serialized, "pitch", 1f);
            SetSerialized(serialized, "loopMode", (int)AudioLoopMode.On);
            SetSerialized(serialized, "routingBus", AudioBusKeys.Bgm);
            SetSerialized(serialized, "fadeInSeconds", 0.05f);
            SetSerialized(serialized, "fadeOutSeconds", 0.05f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static RouteAsset CreateRouteAsset(string assetPath, string routeName, string scenePath, string description, ActivityAsset startupActivity)
        {
            RouteAsset asset = LoadOrCreate<RouteAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetSerialized(serialized, "routeName", routeName);
            SetSerialized(serialized, "primaryScenePath", scenePath);
            SetSerialized(serialized, "primarySceneName", System.IO.Path.GetFileNameWithoutExtension(scenePath));
            SetSerialized(serialized, "startupActivity", startupActivity);
            SetSerialized(serialized, "description", description);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ActivityAsset CreateActivityAsset(string assetPath, string activityName, string description)
        {
            ActivityAsset asset = LoadOrCreate<ActivityAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetSerialized(serialized, "activityName", activityName);
            SetSerialized(serialized, "description", description);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null)
            {
                return existing;
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void CreateCamera(Vector3 position, Color backgroundColor)
        {
            GameObject cameraObject = new GameObject("QA_FrameworkBgmCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = position;
            cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }

        private static void CreateLight(string label)
        {
            GameObject lightObject = new GameObject($"QA_FrameworkBgmLight_{label}");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "Audio");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "ScriptableObjects");
            EnsureFolder(Root, "Routes");
            EnsureFolder(Root, "Activities");
            EnsureFolder(Root, "AudioClips");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static GameObject EnsureRoot(Scene scene, string name)
        {
            GameObject root = FindInScene(scene, name);
            if (root != null)
            {
                return root;
            }

            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            return root;
        }

        private static GameObject EnsureChild(Scene scene, Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform match = FindInChildren(root.transform, name);
                if (match != null)
                {
                    return match.gameObject;
                }
            }

            return null;
        }

        private static Transform FindInChildren(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform match = FindInChildren(root.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool ValidateObjectReference(Object target, string propertyName, Object expectedValue, string context)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FRAMEWORK_BGM_QA_SETUP] Serialized validation property not found. context='{context}' property='{propertyName}'.", target);
                return false;
            }

            if (!ReferencesMatch(property.objectReferenceValue, expectedValue))
            {
                Debug.LogError(
                    $"[FRAMEWORK_BGM_QA_SETUP] Object reference was not assigned. context='{context}' expected='{FormatObject(expectedValue)}' actual='{FormatObject(property.objectReferenceValue)}'.",
                    target);
                return false;
            }

            return true;
        }

        private static bool ReferencesMatch(Object actual, Object expected)
        {
            if (actual == null || expected == null)
            {
                return actual == expected;
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(actual, out string actualGuid, out long actualLocalId)
                && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(expected, out string expectedGuid, out long expectedLocalId))
            {
                return actualGuid == expectedGuid && actualLocalId == expectedLocalId;
            }

            return actual == expected;
        }

        private static string FormatObject(Object value)
        {
            if (value == null)
            {
                return "<missing>";
            }

            string path = AssetDatabase.GetAssetPath(value);
            return string.IsNullOrWhiteSpace(path) ? value.name : $"{value.name} ({path})";
        }

        private static void SetSerialized(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FRAMEWORK_BGM_QA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FRAMEWORK_BGM_QA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FRAMEWORK_BGM_QA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"[FRAMEWORK_BGM_QA_SETUP] Serialized property not found. target='{target.GetType().Name}' property='{propertyName}'.", target);
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetSerialized(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetSerialized(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetSerialized(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetSerialized(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                if (property.propertyType == SerializedPropertyType.Enum)
                {
                    property.enumValueIndex = value;
                    return;
                }

                property.intValue = value;
            }
        }
    }
}

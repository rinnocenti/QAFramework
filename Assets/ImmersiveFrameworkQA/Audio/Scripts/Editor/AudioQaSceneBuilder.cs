using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using Immersive.Pooling.Policies;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Hosts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Audio.Editor
{
    public static class AudioQaSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Audio";
        private const string Scenes = Root + "/Scenes";
        private const string ScriptableObjects = Root + "/ScriptableObjects";
        private const string Clips = Root + "/AudioClips";
        private const string Prefabs = Root + "/Prefabs";
        private const string ScenePath = Scenes + "/QA_Audio.unity";

        private const string DefaultsPath = ScriptableObjects + "/QA_AudioDefaults.asset";
        private const string SfxCuePath = ScriptableObjects + "/QA_SfxCue.asset";
        private const string PooledSfxCuePath = ScriptableObjects + "/QA_SfxCue_Pooled.asset";
        private const string MissingClipSfxCuePath = ScriptableObjects + "/QA_SfxCue_MissingClip.asset";
        private const string PoolDefinitionPath = ScriptableObjects + "/QA_AudioSfxPool.asset";
        private const string BgmCuePath = ScriptableObjects + "/QA_BgmCue.asset";
        private const string PooledAudioSourcePrefabPath = Prefabs + "/QA_PooledAudioSource.prefab";

        [MenuItem("Immersive Framework/QA/Setup/Audio/Create or Refresh Audio QA Scene")]
        public static void CreateOrRefreshAudioQaScene()
        {
            EnsureFolders();

            AudioQaGeneratedClips generatedClips = AudioQaGeneratedClipRepair.EnsureGeneratedClipsAndAssignments();
            if (!generatedClips.IsValid)
            {
                Debug.LogError("[AUDIO_QA] QA Audio scene build aborted because generated clips are not valid.");
                return;
            }

            AudioDefaultsAsset defaults = CreateDefaultsAsset();
            AudioSfxCueAsset sfxCue = CreateSfxCue(SfxCuePath, "qa.audio.sfx.beep", generatedClips.SfxClip, AudioPlaybackMode.Global);
            GameObject pooledAudioSourcePrefab = CreatePooledAudioSourcePrefab();
            PoolDefinitionAsset poolDefinition = CreatePoolDefinitionAsset(pooledAudioSourcePrefab);
            AudioSfxCueAsset pooledSfxCue = CreateSfxCue(
                PooledSfxCuePath,
                "qa.audio.sfx.pooled",
                generatedClips.SfxClip,
                AudioPlaybackMode.Global,
                AudioSfxExecutionMode.Pooled,
                poolDefinition);
            AudioSfxCueAsset missingClipSfxCue = CreateSfxCue(MissingClipSfxCuePath, "qa.audio.sfx.missing-clip", null, AudioPlaybackMode.Global);
            AudioBgmCueAsset bgmCue = CreateBgmCue(BgmCuePath, "qa.audio.bgm.tone", generatedClips.BgmClip);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_Audio";

            CreateCamera();
            CreateLight();

            GameObject root = new GameObject("Audio QA Root");
            PoolRuntimeHost poolHost = CreatePoolRuntimeHost(root.transform, poolDefinition);

            AudioRuntimeHost runtimeHost = CreateRuntimeHost(
                "QA_AudioRuntimeHost",
                root.transform,
                defaults,
                new Vector3(-2f, 0f, 0f),
                poolHost);

            AudioRuntimeHost missingDefaultsHost = CreateRuntimeHost(
                "QA_AudioRuntimeHost_MissingDefaults",
                root.transform,
                null,
                new Vector3(2f, 0f, 0f));

            AudioRuntimeHost missingPoolHost = CreateRuntimeHost(
                "QA_AudioRuntimeHost_MissingPool",
                root.transform,
                defaults,
                new Vector3(0f, 0f, 2f));

            AudioListenerRuntimeHost listenerHost = CreateListenerHost(root.transform);

            GameObject panelObject = new GameObject("QA_AudioPanel");
            panelObject.transform.SetParent(root.transform, false);
            AudioQaPanel panel = panelObject.AddComponent<AudioQaPanel>();
            ConfigurePanel(panel, runtimeHost, missingDefaultsHost, missingPoolHost, listenerHost, sfxCue, pooledSfxCue, missingClipSfxCue, bgmCue);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AudioQaGeneratedClipRepair.EnsureGeneratedClipsAndAssignments();

            Debug.Log($"[AUDIO_QA] QA Audio scene created or refreshed at '{ScenePath}'.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "Audio");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "ScriptableObjects");
            EnsureFolder(Root, "AudioClips");
            EnsureFolder(Root, "Prefabs");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static AudioDefaultsAsset CreateDefaultsAsset()
        {
            AudioDefaultsAsset asset = LoadOrCreate<AudioDefaultsAsset>(DefaultsPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetFloat(serialized, "masterVolume", 1f);
            SetFloat(serialized, "sfxVolume", 1f);
            SetFloat(serialized, "bgmVolume", 0.35f);
            SetString(serialized, "masterBus", AudioBusKeys.Master);
            SetString(serialized, "sfxBus", AudioBusKeys.Sfx);
            SetString(serialized, "bgmBus", AudioBusKeys.Bgm);
            SetFloat(serialized, "defaultFadeInSeconds", 0.05f);
            SetFloat(serialized, "defaultFadeOutSeconds", 0.05f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static AudioSfxCueAsset CreateSfxCue(
            string assetPath,
            string cueId,
            AudioClip clip,
            AudioPlaybackMode playbackMode,
            AudioSfxExecutionMode executionMode = AudioSfxExecutionMode.Direct,
            PoolDefinitionAsset poolDefinition = null)
        {
            AudioSfxCueAsset asset = LoadOrCreate<AudioSfxCueAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "cueId", cueId);
            SetObject(serialized, "clip", clip);
            SetFloat(serialized, "volume", 1f);
            SetFloat(serialized, "pitch", 1f);
            SetEnum(serialized, "loopMode", (int)AudioLoopMode.Off);
            SetString(serialized, "routingBus", AudioBusKeys.Sfx);
            SetEnum(serialized, "playbackMode", (int)playbackMode);
            SetEnum(serialized, "executionMode", (int)executionMode);
            SetObject(serialized, "pooledAudioSourcePool", poolDefinition);
            SetFloat(serialized, "spatialBlend", playbackMode == AudioPlaybackMode.Spatial ? 1f : 0f);
            SetFloat(serialized, "minDistance", 1f);
            SetFloat(serialized, "maxDistance", 30f);
            SetInt(serialized, "maxSimultaneousInstances", 4);
            SetFloat(serialized, "retriggerCooldownSeconds", 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static GameObject CreatePooledAudioSourcePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PooledAudioSourcePrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(PooledAudioSourcePrefabPath);
            }

            GameObject prefabRoot = new GameObject("QA_PooledAudioSource");
            AudioSource source = prefabRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PooledAudioSourcePrefabPath);
            UnityEngine.Object.DestroyImmediate(prefabRoot);
            return prefab;
        }

        private static PoolDefinitionAsset CreatePoolDefinitionAsset(GameObject prefab)
        {
            PoolDefinitionAsset asset = LoadOrCreate<PoolDefinitionAsset>(PoolDefinitionPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetObject(serialized, "prefab", prefab);
            SetString(serialized, "poolLabel", "QA Audio SFX");
            SetInt(serialized, "initialCapacity", 2);
            SetInt(serialized, "maxSize", 8);
            SetBool(serialized, "canExpand", true);
            SetBool(serialized, "prewarmOnRegister", true);
            SetEnum(serialized, "lifetimeScope", (int)PoolLifetimeScope.Temporary);
            SetEnum(serialized, "registrationMode", (int)PoolRegistrationMode.LazyOnFirstRent);
            SetFloat(serialized, "autoReturnSeconds", 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static AudioBgmCueAsset CreateBgmCue(string assetPath, string cueId, AudioClip clip)
        {
            AudioBgmCueAsset asset = LoadOrCreate<AudioBgmCueAsset>(assetPath);
            SerializedObject serialized = new SerializedObject(asset);
            SetString(serialized, "cueId", cueId);
            SetObject(serialized, "clip", clip);
            SetFloat(serialized, "volume", 0.6f);
            SetFloat(serialized, "pitch", 1f);
            SetEnum(serialized, "loopMode", (int)AudioLoopMode.On);
            SetString(serialized, "routingBus", AudioBusKeys.Bgm);
            SetFloat(serialized, "fadeInSeconds", 0.05f);
            SetFloat(serialized, "fadeOutSeconds", 0.05f);
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

        private static AudioRuntimeHost CreateRuntimeHost(
            string name,
            Transform parent,
            AudioDefaultsAsset defaults,
            Vector3 localPosition,
            PoolRuntimeHost poolRuntimeHost = null)
        {
            GameObject hostObject = new GameObject(name);
            hostObject.transform.SetParent(parent, false);
            hostObject.transform.localPosition = localPosition;

            GameObject playbackRoot = new GameObject("AudioPlayback");
            playbackRoot.transform.SetParent(hostObject.transform, false);

            AudioRuntimeHost host = hostObject.AddComponent<AudioRuntimeHost>();
            SerializedObject serialized = new SerializedObject(host);
            SetObject(serialized, "defaults", defaults);
            SetObject(serialized, "playbackRoot", playbackRoot.transform);
            SetObject(serialized, "poolRuntimeHost", poolRuntimeHost);
            SetBool(serialized, "composeOnAwake", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(host);
            return host;
        }

        private static PoolRuntimeHost CreatePoolRuntimeHost(Transform parent, PoolDefinitionAsset poolDefinition)
        {
            GameObject hostObject = new GameObject("QA_PoolRuntimeHost");
            hostObject.transform.SetParent(parent, false);
            hostObject.transform.localPosition = new Vector3(-4f, 0f, 0f);

            PoolRuntimeHost host = hostObject.AddComponent<PoolRuntimeHost>();
            SerializedObject serialized = new SerializedObject(host);
            SetBool(serialized, "initializeOnAwake", true);
            SetBool(serialized, "persistAcrossScenes", false);

            SerializedProperty definitions = serialized.FindProperty("poolDefinitions");
            if (definitions != null)
            {
                definitions.arraySize = 1;
                definitions.GetArrayElementAtIndex(0).objectReferenceValue = poolDefinition;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(host);
            return host;
        }

        private static AudioListenerRuntimeHost CreateListenerHost(Transform parent)
        {
            GameObject listenerObject = new GameObject("QA_AudioListenerRuntimeHost");
            listenerObject.transform.SetParent(parent, false);
            listenerObject.transform.localPosition = new Vector3(0f, 0f, -5f);
            AudioListenerRuntimeHost listenerHost = listenerObject.AddComponent<AudioListenerRuntimeHost>();

            SerializedObject serialized = new SerializedObject(listenerHost);
            SetEnum(serialized, "duplicatePolicy", (int)AudioListenerDuplicatePolicy.ReportOnly);
            SetBool(serialized, "includeInactiveListeners", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(listenerHost);
            return listenerHost;
        }

        private static void ConfigurePanel(
            AudioQaPanel panel,
            AudioRuntimeHost runtimeHost,
            AudioRuntimeHost missingDefaultsHost,
            AudioRuntimeHost missingPoolHost,
            AudioListenerRuntimeHost listenerHost,
            AudioSfxCueAsset sfxCue,
            AudioSfxCueAsset pooledSfxCue,
            AudioSfxCueAsset missingClipSfxCue,
            AudioBgmCueAsset bgmCue)
        {
            SerializedObject serialized = new SerializedObject(panel);
            SetObject(serialized, "audioRuntimeHost", runtimeHost);
            SetObject(serialized, "missingDefaultsHost", missingDefaultsHost);
            SetObject(serialized, "missingPoolHost", missingPoolHost);
            SetObject(serialized, "listenerRuntimeHost", listenerHost);
            SetObject(serialized, "sfxCue", sfxCue);
            SetObject(serialized, "pooledSfxCue", pooledSfxCue);
            SetObject(serialized, "missingClipSfxCue", missingClipSfxCue);
            SetObject(serialized, "bgmCue", bgmCue);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("QA_AudioCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);
            cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }

        private static void CreateLight()
        {
            GameObject lightObject = new GameObject("QA_AudioLight");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetEnum(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static void SetObject(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }
    }
}

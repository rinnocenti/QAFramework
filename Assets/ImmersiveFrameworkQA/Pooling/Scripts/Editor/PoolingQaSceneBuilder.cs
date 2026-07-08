using Immersive.Pooling.Policies;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Hosts;
using ImmersiveFrameworkQA.Pooling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Pooling.Editor
{
    /// <summary>
    /// Editor-only deterministic creator for the Pooling QA assets and scene.
    /// This avoids hand-authored YAML and lets Unity generate stable serialized assets.
    /// </summary>
    public static class PoolingQaSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA/Pooling";
        private const string Scenes = Root + "/Scenes";
        private const string Prefabs = Root + "/Prefabs";
        private const string ScriptableObjects = Root + "/ScriptableObjects";

        private const string CubePrefabPath = Prefabs + "/QA_PooledCube.prefab";
        private const string SpherePrefabPath = Prefabs + "/QA_PooledSphere.prefab";
        private const string CubeDefinitionPath = ScriptableObjects + "/QA_CubePool.asset";
        private const string LimitedDefinitionPath = ScriptableObjects + "/QA_LimitedCubePool.asset";
        private const string AutoReturnDefinitionPath = ScriptableObjects + "/QA_AutoReturnPool.asset";
        private const string ScenePath = Scenes + "/QA_Pooling.unity";

        [MenuItem("Immersive Framework QA/Pooling/Create or Refresh Pooling QA Scene")]
        public static void CreateOrRefreshPoolingQaScene()
        {
            EnsureFolders();

            var cubePrefab = CreatePrimitivePrefab(CubePrefabPath, PrimitiveType.Cube, "pooling.qa.cube");
            var spherePrefab = CreatePrimitivePrefab(SpherePrefabPath, PrimitiveType.Sphere, "pooling.qa.sphere");

            var cubeDefinition = CreateOrUpdateDefinition(
                CubeDefinitionPath,
                cubePrefab,
                "qa.cube.pool",
                initialCapacity: 2,
                maxSize: 5,
                canExpand: true,
                prewarmOnRegister: false,
                autoReturnSeconds: 0f,
                registrationMode: PoolRegistrationMode.LazyOnFirstRent);

            var limitedDefinition = CreateOrUpdateDefinition(
                LimitedDefinitionPath,
                cubePrefab,
                "qa.limited-cube.pool",
                initialCapacity: 2,
                maxSize: 2,
                canExpand: false,
                prewarmOnRegister: false,
                autoReturnSeconds: 0f,
                registrationMode: PoolRegistrationMode.LazyOnFirstRent);

            var autoReturnDefinition = CreateOrUpdateDefinition(
                AutoReturnDefinitionPath,
                spherePrefab,
                "qa.auto-return.pool",
                initialCapacity: 1,
                maxSize: 3,
                canExpand: true,
                prewarmOnRegister: false,
                autoReturnSeconds: 1f,
                registrationMode: PoolRegistrationMode.LazyOnFirstRent);

            CreateScene(cubeDefinition, limitedDefinition, autoReturnDefinition);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[POOLING_QA] Pooling QA scene created or refreshed at '{ScenePath}'.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "ImmersiveFrameworkQA");
            EnsureFolder("Assets/ImmersiveFrameworkQA", "Pooling");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "ScriptableObjects");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static GameObject CreatePrimitivePrefab(string path, PrimitiveType primitiveType, string probeId)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                return existing;
            }

            var instance = GameObject.CreatePrimitive(primitiveType);
            instance.name = System.IO.Path.GetFileNameWithoutExtension(path);

            var probe = instance.GetComponent<PoolingQaCallbackProbe>();
            if (probe == null)
            {
                probe = instance.AddComponent<PoolingQaCallbackProbe>();
            }

            probe.Configure(probeId);

            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static PoolDefinitionAsset CreateOrUpdateDefinition(
            string path,
            GameObject prefab,
            string label,
            int initialCapacity,
            int maxSize,
            bool canExpand,
            bool prewarmOnRegister,
            float autoReturnSeconds,
            PoolRegistrationMode registrationMode)
        {
            var definition = AssetDatabase.LoadAssetAtPath<PoolDefinitionAsset>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<PoolDefinitionAsset>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("poolLabel").stringValue = label;
            serialized.FindProperty("initialCapacity").intValue = initialCapacity;
            serialized.FindProperty("maxSize").intValue = maxSize;
            serialized.FindProperty("canExpand").boolValue = canExpand;
            serialized.FindProperty("prewarmOnRegister").boolValue = prewarmOnRegister;
            serialized.FindProperty("autoReturnSeconds").floatValue = autoReturnSeconds;
            serialized.FindProperty("registrationMode").enumValueIndex = (int)registrationMode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void CreateScene(
            PoolDefinitionAsset cubeDefinition,
            PoolDefinitionAsset limitedDefinition,
            PoolDefinitionAsset autoReturnDefinition)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_Pooling";

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 5f, -9f);
            camera.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);

            var lightObject = new GameObject("Directional Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var root = new GameObject("Pooling QA Root");
            var hostObject = new GameObject("PoolRuntimeHost");
            hostObject.transform.SetParent(root.transform, false);
            var host = hostObject.AddComponent<PoolRuntimeHost>();

            var spawnRoot = new GameObject("Spawn Root");
            spawnRoot.transform.SetParent(root.transform, false);
            spawnRoot.transform.position = Vector3.zero;

            var panelObject = new GameObject("Pooling QA Panel");
            panelObject.transform.SetParent(root.transform, false);
            var panel = panelObject.AddComponent<PoolingQaPanel>();
            ConfigurePanel(panel, host, cubeDefinition, limitedDefinition, autoReturnDefinition, spawnRoot.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void ConfigurePanel(
            PoolingQaPanel panel,
            PoolRuntimeHost host,
            PoolDefinitionAsset cubeDefinition,
            PoolDefinitionAsset limitedDefinition,
            PoolDefinitionAsset autoReturnDefinition,
            Transform spawnParent)
        {
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("poolRuntimeHost").objectReferenceValue = host;
            serialized.FindProperty("cubeDefinition").objectReferenceValue = cubeDefinition;
            serialized.FindProperty("limitedDefinition").objectReferenceValue = limitedDefinition;
            serialized.FindProperty("autoReturnDefinition").objectReferenceValue = autoReturnDefinition;
            serialized.FindProperty("spawnParent").objectReferenceValue = spawnParent;
            serialized.FindProperty("title").stringValue = "Pooling QA";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
        }
    }
}

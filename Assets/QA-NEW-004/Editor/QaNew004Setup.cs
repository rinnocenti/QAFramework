using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.RouteLifecycle;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.QaFramework.New004.Editor
{
    internal static class QaNew004Setup
    {
        private const string Root = "Assets/QA-NEW-004";
        private const string PersistentScenePath =
            Root + "/Scenes/QA_NEW_004_Persistent.unity";
        private const string RouteAScenePath =
            Root + "/Scenes/QA_NEW_004_RouteA.unity";
        private const string RouteBScenePath =
            Root + "/Scenes/QA_NEW_004_RouteB.unity";
        private const string OutputDefinitionPath =
            Root + "/Camera/CameraOutput_QaNew004.asset";
        private const string BehaviorPath =
            Root + "/Camera/CameraBehavior_QaNew004Fixed.asset";
        private const string OutputPrefabPath =
            Root + "/Camera/PF_CameraOutput_QaNew004.prefab";
        private const string RigPrefabPath =
            Root + "/Camera/PF_CameraPresentation_QaNew004_A.prefab";
        private const string PresentationPath =
            Root + "/Camera/CameraPresentation_QaNew004_A.asset";
        private const string RouteAPath =
            Root + "/Settings/Route_QaNew004_A.asset";
        private const string RouteBPath =
            Root + "/Settings/Route_QaNew004_B.asset";
        private const string GameApplicationPath =
            Root + "/Settings/GameApplication_QaNew004.asset";
        private const string FrameworkSettingsPath =
            "Assets/_Project/Settings/ImmersiveFramework/Resources/ImmersiveFrameworkSettings.asset";

        private static readonly string[] GeneratedPaths =
        {
            PersistentScenePath,
            RouteAScenePath,
            RouteBScenePath,
            OutputDefinitionPath,
            BehaviorPath,
            OutputPrefabPath,
            RigPrefabPath,
            PresentationPath,
            RouteAPath,
            RouteBPath,
            GameApplicationPath
        };

        [MenuItem("QA/QA-NEW-004/Create Isolated Composition")]
        private static void CreateComposition()
        {
            for (int index = 0; index < GeneratedPaths.Length; index++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(GeneratedPaths[index]) != null)
                {
                    Debug.LogError(
                        $"QA-NEW-004 setup preserved existing asset '{GeneratedPaths[index]}'. Remove the isolated generated composition explicitly before recreating it.");
                    return;
                }
            }

            CameraOutputDefinition outputDefinition =
                ScriptableObject.CreateInstance<CameraOutputDefinition>();
            outputDefinition.name = "CameraOutput_QaNew004";
            SetString(
                outputDefinition,
                "stableId",
                "94040000000000000000000000000001");
            SetString(
                outputDefinition,
                "description",
                "QA-NEW-004 isolated physical Camera Output.");
            AssetDatabase.CreateAsset(outputDefinition, OutputDefinitionPath);

            FixedCameraRigBehaviorDefinition behavior =
                ScriptableObject.CreateInstance<
                    FixedCameraRigBehaviorDefinition>();
            behavior.name = "CameraBehavior_QaNew004Fixed";
            AssetDatabase.CreateAsset(behavior, BehaviorPath);

            GameObject outputPrefab =
                CreateOutputPrefab(outputDefinition, behavior);
            GameObject rigPrefab = CreatePresentationRigPrefab(behavior);

            CameraPresentationDefinition presentation =
                ScriptableObject.CreateInstance<
                    CameraPresentationDefinition>();
            presentation.name = "CameraPresentation_QaNew004_A";
            SerializedObject presentationSerialized =
                new SerializedObject(presentation);
            presentationSerialized.FindProperty("stableId").stringValue =
                "94040000000000000000000000000002";
            presentationSerialized.FindProperty("description").stringValue =
                "Session-owned selection occurrence declared by Route A.";
            presentationSerialized.FindProperty("outputDefinition")
                .objectReferenceValue = outputDefinition;
            presentationSerialized.FindProperty("rigPrefab")
                .objectReferenceValue = rigPrefab;
            presentationSerialized.FindProperty("transitionMode").intValue =
                (int)CameraPresentationTransitionMode.Cut;
            presentationSerialized.FindProperty("subjectPolicy").intValue =
                (int)CameraSharedCompositionSubjectPolicyKind
                    .AllAvailableSubjects;
            presentationSerialized.FindProperty("requestPrecedence").intValue =
                100;
            presentationSerialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(presentation, PresentationPath);

            CreateRoute(
                RouteAPath,
                "qa-new-004.route-a",
                "QA-NEW-004 Route A",
                RouteAScenePath,
                presentation);
            CreateRoute(
                RouteBPath,
                "qa-new-004.route-b",
                "QA-NEW-004 Route B",
                RouteBScenePath,
                null);
            AssetDatabase.SaveAssets();

            CreateRouteScene(RouteAPath, RouteAScenePath, "route-a");
            CreateRouteScene(RouteBPath, RouteBScenePath, "route-b");
            CreatePersistentScene();

            SceneAsset persistentScene =
                AssetDatabase.LoadAssetAtPath<SceneAsset>(
                    PersistentScenePath);
            RouteAsset startupRoute = LoadPersistedRoute(RouteAPath);
            if (persistentScene == null || startupRoute == null)
            {
                Debug.LogError(
                    "QA-NEW-004 could not reload the persistent scene or Route A after scene creation.");
                return;
            }

            GameApplicationAsset application =
                ScriptableObject.CreateInstance<GameApplicationAsset>();
            application.name = "GameApplication_QaNew004";
            SerializedObject applicationSerialized =
                new SerializedObject(application);
            applicationSerialized.FindProperty("applicationName").stringValue =
                "QA-NEW-004 Route Camera Selection Continuity";
            applicationSerialized.FindProperty("startupRoute")
                .objectReferenceValue = startupRoute;
            applicationSerialized.FindProperty("playerSessionEnabled")
                .boolValue = false;
            SerializedProperty outputPrefabs = applicationSerialized
                .FindProperty("cameraSession")
                .FindPropertyRelative("outputPrefabs");
            outputPrefabs.arraySize = 1;
            outputPrefabs.GetArrayElementAtIndex(0).objectReferenceValue =
                outputPrefab;
            applicationSerialized.FindProperty("persistentContent")
                .FindPropertyRelative("containerScene")
                .objectReferenceValue = persistentScene;
            applicationSerialized.FindProperty("validationMode").intValue = 1;
            applicationSerialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(application, GameApplicationPath);

            EnsureBuildScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!VerifySavedComposition())
            {
                return;
            }

            Selection.activeObject = application;
            Debug.Log(
                "[QA-NEW-004] Isolated composition created. Activate it through the adjacent QA menu command, then open QA_NEW_004_Persistent and enter Play Mode.");
        }

        [MenuItem("QA/QA-NEW-004/Activate Game Application")]
        private static void ActivateGameApplication()
        {
            GameApplicationAsset application =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    GameApplicationPath);
            ImmersiveFrameworkSettingsAsset settings =
                AssetDatabase.LoadAssetAtPath<
                    ImmersiveFrameworkSettingsAsset>(FrameworkSettingsPath);
            if (application == null || settings == null)
            {
                Debug.LogError(
                    "QA-NEW-004 activation requires its generated Game Application and the project Immersive Framework settings asset.");
                return;
            }

            SerializedObject serializedSettings =
                new SerializedObject(settings);
            serializedSettings.FindProperty("activeGameApplication")
                .objectReferenceValue = application;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[QA-NEW-004] Game Application activated.");
        }

        private static GameObject CreateOutputPrefab(
            CameraOutputDefinition outputDefinition,
            FixedCameraRigBehaviorDefinition behavior)
        {
            GameObject root = new GameObject("PF_CameraOutput_QaNew004");
            try
            {
                GameObject defaultRigRoot = new GameObject("DefaultRig");
                defaultRigRoot.transform.SetParent(root.transform, false);
                CameraRigComposer defaultComposer =
                    ConfigureFixedComposer(defaultRigRoot, behavior);

                GameObject outputRoot = new GameObject("Output");
                outputRoot.tag = "MainCamera";
                outputRoot.transform.SetParent(root.transform, false);
                UnityEngine.Camera unityCamera =
                    outputRoot.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain =
                    outputRoot.AddComponent<CinemachineBrain>();
                CameraOutputAuthoring output =
                    outputRoot.AddComponent<CameraOutputAuthoring>();
                SerializedObject serializedOutput =
                    new SerializedObject(output);
                serializedOutput.FindProperty("outputDefinition")
                    .objectReferenceValue = outputDefinition;
                serializedOutput.FindProperty("unityCamera")
                    .objectReferenceValue = unityCamera;
                serializedOutput.FindProperty("cinemachineBrain")
                    .objectReferenceValue = brain;
                serializedOutput.FindProperty("defaultCameraRig")
                    .objectReferenceValue = defaultComposer;
                serializedOutput.FindProperty("initializeOnAwake").boolValue =
                    true;
                serializedOutput.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(
                    root,
                    OutputPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreatePresentationRigPrefab(
            FixedCameraRigBehaviorDefinition behavior)
        {
            GameObject root =
                new GameObject("PF_CameraPresentation_QaNew004_A");
            try
            {
                root.AddComponent<QaNew004CameraOccurrenceProbe>();
                ConfigureFixedComposer(root, behavior);
                return PrefabUtility.SaveAsPrefabAsset(
                    root,
                    RigPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static CameraRigComposer ConfigureFixedComposer(
            GameObject root,
            FixedCameraRigBehaviorDefinition behavior)
        {
            CameraRigComposer composer =
                root.AddComponent<CameraRigComposer>();
            GameObject cameraRoot = new GameObject("Cinemachine Camera");
            cameraRoot.transform.SetParent(root.transform, false);
            CinemachineCamera camera =
                cameraRoot.AddComponent<CinemachineCamera>();
            SerializedObject serializedComposer =
                new SerializedObject(composer);
            serializedComposer.FindProperty("behaviorDefinition")
                .objectReferenceValue = behavior;
            serializedComposer.FindProperty("cinemachineCamera")
                .objectReferenceValue = camera;
            serializedComposer.FindProperty("materializedPresentationIntent")
                .intValue = (int)CameraRigPresentationIntent.Fixed;
            serializedComposer.FindProperty("frameworkOwnedCinemachineCamera")
                .objectReferenceValue = camera;
            serializedComposer.FindProperty("materializationRevision")
                .intValue = 1;
            serializedComposer.FindProperty("lastApplyRebuildStatus")
                .stringValue = "ValidationSucceeded";
            serializedComposer.ApplyModifiedPropertiesWithoutUndo();
            return composer;
        }

        private static void CreateRoute(
            string assetPath,
            string routeId,
            string routeName,
            string scenePath,
            CameraPresentationDefinition selection)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = routeName.Replace(" ", string.Empty);
            SerializedObject serializedRoute = new SerializedObject(route);
            serializedRoute.FindProperty("routeId").stringValue = routeId;
            serializedRoute.FindProperty("routeName").stringValue = routeName;
            serializedRoute.FindProperty("primaryScenePath").stringValue =
                scenePath;
            serializedRoute.FindProperty("primarySceneName").stringValue =
                System.IO.Path.GetFileNameWithoutExtension(scenePath);
            serializedRoute.FindProperty("cameraPresentations").arraySize = 0;
            SerializedProperty selections =
                serializedRoute.FindProperty(
                    "cameraPresentationSelections");
            selections.arraySize = selection != null ? 1 : 0;
            if (selection != null)
            {
                selections.GetArrayElementAtIndex(0).objectReferenceValue =
                    selection;
            }
            serializedRoute.FindProperty("transitionGateMode").intValue = 30;
            serializedRoute.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(route, assetPath);
        }

        private static void CreateRouteScene(
            string routeAssetPath,
            string scenePath,
            string localIdSuffix)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            RouteAsset route = LoadPersistedRoute(routeAssetPath);
            if (route == null)
            {
                Debug.LogError(
                    $"QA-NEW-004 could not reload Route '{routeAssetPath}' after creating its scene.");
                return;
            }

            GameObject root = new GameObject(route.RouteName + " Content");
            RouteContentContribution contribution =
                root.AddComponent<RouteContentContribution>();
            root.AddComponent<QaNew004RouteLifecycleProbe>();
            SerializedObject serializedContribution =
                new SerializedObject(contribution);
            serializedContribution.FindProperty("route")
                .objectReferenceValue = route;
            serializedContribution.FindProperty("localContentId")
                .stringValue = "qa-new-004." + localIdSuffix + ".content";
            serializedContribution.FindProperty("requiredness").intValue = 10;
            serializedContribution.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreatePersistentScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            RouteAsset routeA = LoadPersistedRoute(RouteAPath);
            RouteAsset routeB = LoadPersistedRoute(RouteBPath);
            if (routeA == null || routeB == null)
            {
                Debug.LogError(
                    "QA-NEW-004 could not reload Route A and Route B after creating the persistent scene.");
                return;
            }

            GameObject root = new GameObject("QA-NEW-004 Persistent Scenario");
            QaNew004RouteCameraSelectionScenario scenario =
                root.AddComponent<QaNew004RouteCameraSelectionScenario>();

            GameObject requestBRoot = new GameObject("Request Route B");
            requestBRoot.transform.SetParent(root.transform, false);
            RouteRequestTrigger requestB =
                requestBRoot.AddComponent<RouteRequestTrigger>();
            ConfigureTrigger(
                requestB,
                routeB,
                "QA-NEW-004 preserve selection while entering empty Route B");

            GameObject requestARoot = new GameObject("Request Route A");
            requestARoot.transform.SetParent(root.transform, false);
            RouteRequestTrigger requestA =
                requestARoot.AddComponent<RouteRequestTrigger>();
            ConfigureTrigger(
                requestA,
                routeA,
                "QA-NEW-004 restore Route A baseline");

            SerializedObject serializedScenario =
                new SerializedObject(scenario);
            serializedScenario.FindProperty("routeA").objectReferenceValue =
                routeA;
            serializedScenario.FindProperty("routeB").objectReferenceValue =
                routeB;
            serializedScenario.FindProperty("requestRouteB")
                .objectReferenceValue = requestB;
            serializedScenario.FindProperty("requestRouteA")
                .objectReferenceValue = requestA;
            serializedScenario.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, PersistentScenePath);
        }

        private static void ConfigureTrigger(
            RouteRequestTrigger trigger,
            RouteAsset route,
            string reason)
        {
            SerializedObject serializedTrigger =
                new SerializedObject(trigger);
            serializedTrigger.FindProperty("targetRoute")
                .objectReferenceValue = route;
            serializedTrigger.FindProperty("reason").stringValue = reason;
            serializedTrigger.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RouteAsset LoadPersistedRoute(string assetPath)
        {
            // NewScene unloads the in-memory Route instances created earlier
            // in this command. References must be read back from the imported
            // asset or they serialize as fileID 0.
            return AssetDatabase.LoadAssetAtPath<RouteAsset>(assetPath);
        }

        private static bool VerifySavedComposition()
        {
            bool routeScenes =
                VerifyRouteScene(RouteAScenePath, RouteAPath) &&
                VerifyRouteScene(RouteBScenePath, RouteBPath);
            bool persistentScene = VerifyPersistentScene();
            bool application = VerifyGameApplication();
            if (routeScenes && persistentScene && application)
            {
                return true;
            }

            Debug.LogError(
                "QA-NEW-004 saved a composition whose Route references do not satisfy preflight. Remove the generated QA-NEW-004 assets before running Create Isolated Composition again.");
            return false;
        }

        private static bool VerifyRouteScene(
            string scenePath,
            string routeAssetPath)
        {
            EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);
            RouteAsset route = LoadPersistedRoute(routeAssetPath);
            RouteContentContribution contribution =
                UnityEngine.Object.FindAnyObjectByType<
                    RouteContentContribution>();
            if (route != null &&
                contribution != null &&
                contribution.Route == route)
            {
                return true;
            }

            Debug.LogError(
                $"QA-NEW-004 scene '{scenePath}' does not reference Route '{routeAssetPath}'.");
            return false;
        }

        private static bool VerifyPersistentScene()
        {
            EditorSceneManager.OpenScene(
                PersistentScenePath,
                OpenSceneMode.Single);
            RouteAsset routeA = LoadPersistedRoute(RouteAPath);
            RouteAsset routeB = LoadPersistedRoute(RouteBPath);
            QaNew004RouteCameraSelectionScenario scenario =
                UnityEngine.Object.FindAnyObjectByType<
                    QaNew004RouteCameraSelectionScenario>();
            if (scenario == null || routeA == null || routeB == null)
            {
                Debug.LogError(
                    "QA-NEW-004 persistent scene is missing its scenario or Routes.");
                return false;
            }

            SerializedObject serializedScenario =
                new SerializedObject(scenario);
            UnityEngine.Object savedRouteA = serializedScenario
                .FindProperty("routeA").objectReferenceValue;
            UnityEngine.Object savedRouteB = serializedScenario
                .FindProperty("routeB").objectReferenceValue;
            RouteRequestTrigger requestRouteB = serializedScenario
                .FindProperty("requestRouteB").objectReferenceValue
                as RouteRequestTrigger;
            RouteRequestTrigger requestRouteA = serializedScenario
                .FindProperty("requestRouteA").objectReferenceValue
                as RouteRequestTrigger;
            if (savedRouteA == routeA &&
                savedRouteB == routeB &&
                requestRouteB != null &&
                requestRouteA != null &&
                requestRouteB.TargetRoute == routeB &&
                requestRouteA.TargetRoute == routeA)
            {
                return true;
            }

            Debug.LogError(
                "QA-NEW-004 persistent scene is missing Route A, Route B, or the two persistent Route Request Trigger targets.");
            return false;
        }

        private static bool VerifyGameApplication()
        {
            GameApplicationAsset application =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    GameApplicationPath);
            RouteAsset routeA = LoadPersistedRoute(RouteAPath);
            if (application != null &&
                routeA != null &&
                application.StartupRoute == routeA)
            {
                return true;
            }

            Debug.LogError(
                "QA-NEW-004 Game Application did not persist Route A as the startup Route.");
            return false;
        }

        private static void EnsureBuildScenes()
        {
            var scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);
            AddBuildScene(scenes, PersistentScenePath);
            AddBuildScene(scenes, RouteAScenePath);
            AddBuildScene(scenes, RouteBScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddBuildScene(
            List<EditorBuildSettingsScene> scenes,
            string path)
        {
            for (int index = 0; index < scenes.Count; index++)
            {
                if (string.Equals(
                        scenes[index].path,
                        path,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

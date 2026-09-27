using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityFlow;
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
        private const string RouteCScenePath =
            Root + "/Scenes/QA_NEW_004_RouteC.unity";
        private const string RigCPrefabPath =
            Root + "/Camera/PF_CameraPresentation_QaNew004_C.prefab";
        private const string PresentationCPath =
            Root + "/Camera/CameraPresentation_QaNew004_C.asset";
        private const string RouteCPath =
            Root + "/Settings/Route_QaNew004_C.asset";

        private const string RouteDScenePath =
            Root + "/Scenes/QA_NEW_004_RouteD.unity";
        private const string RouteDPath =
            Root + "/Settings/Route_QaNew004_D.asset";
        private const string ActivityDAScenePath =
            Root + "/Scenes/QA_NEW_004_ActivityDA.unity";
        private const string ActivityDAContentProfilePath =
            Root + "/Settings/ActivityContentProfile_QaNew004_D_A.asset";
        private const string ActivityDAPath =
            Root + "/Settings/Activity_QaNew004_D_A.asset";
        private const string ActivityDBPath =
            Root + "/Settings/Activity_QaNew004_D_B.asset";
        private const string ActivityDAContentId =
            "qa-new-004.activity-d-a.content";

        private const string RouteEScenePath =
            Root + "/Scenes/QA_NEW_004_RouteE.unity";
        private const string RouteEPath =
            Root + "/Settings/Route_QaNew004_E.asset";
        private const string ActivityEAScenePath =
            Root + "/Scenes/QA_NEW_004_ActivityEA.unity";
        private const string ActivityEAContentProfilePath =
            Root + "/Settings/ActivityContentProfile_QaNew004_E_A.asset";
        private const string ActivityEAPath =
            Root + "/Settings/Activity_QaNew004_E_A.asset";
        private const string ActivityECPath =
            Root + "/Settings/Activity_QaNew004_E_C.asset";
        private const string ActivityEAContentId =
            "qa-new-004.activity-e-a.content";

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

        [MenuItem("QA/QA-NEW-004/Add Route C Replacement Composition")]
        private static void AddRouteCReplacementComposition()
        {
            for (int index = 0; index < GeneratedPaths.Length; index++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(GeneratedPaths[index]) == null)
                {
                    Debug.LogError(
                        "QA-NEW-004 Route C composition requires the base composition (Route A/B, Camera Output, Game Application) to already exist. Run 'Create Isolated Composition' first.");
                    return;
                }
            }

            CameraOutputDefinition outputDefinition =
                AssetDatabase.LoadAssetAtPath<CameraOutputDefinition>(
                    OutputDefinitionPath);
            FixedCameraRigBehaviorDefinition behavior =
                AssetDatabase.LoadAssetAtPath<FixedCameraRigBehaviorDefinition>(
                    BehaviorPath);
            if (outputDefinition == null || behavior == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition could not load the existing Camera Output or Behavior assets.");
                return;
            }

            // Idempotent/resumable: reuse any Route C asset that already
            // exists on disk (e.g. from a prior partial run) instead of
            // aborting, so this command can heal a broken composition
            // without requiring the generated assets to be deleted first.
            GameObject rigPrefabC =
                AssetDatabase.LoadAssetAtPath<GameObject>(RigCPrefabPath);
            if (rigPrefabC == null)
            {
                rigPrefabC = CreatePresentationRigPrefabAt(
                    RigCPrefabPath,
                    "PF_CameraPresentation_QaNew004_C",
                    behavior);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                rigPrefabC =
                    AssetDatabase.LoadAssetAtPath<GameObject>(RigCPrefabPath);
            }

            if (rigPrefabC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition could not create or reload the Route C Rig prefab.");
                return;
            }

            CameraPresentationDefinition presentationC =
                AssetDatabase.LoadAssetAtPath<CameraPresentationDefinition>(
                    PresentationCPath);
            if (presentationC == null)
            {
                presentationC =
                    ScriptableObject.CreateInstance<
                        CameraPresentationDefinition>();
                presentationC.name = "CameraPresentation_QaNew004_C";
                SerializedObject presentationCSerialized =
                    new SerializedObject(presentationC);
                presentationCSerialized.FindProperty("stableId").stringValue =
                    "94040000000000000000000000000003";
                presentationCSerialized.FindProperty("description")
                    .stringValue =
                    "Session-owned replacement occurrence declared by Route C for CAMERA-037-C.";
                presentationCSerialized.FindProperty("outputDefinition")
                    .objectReferenceValue = outputDefinition;
                presentationCSerialized.FindProperty("rigPrefab")
                    .objectReferenceValue = rigPrefabC;
                presentationCSerialized.FindProperty("transitionMode")
                    .intValue = (int)CameraPresentationTransitionMode.Cut;
                presentationCSerialized.FindProperty("subjectPolicy")
                    .intValue = (int)CameraSharedCompositionSubjectPolicyKind
                        .AllAvailableSubjects;
                presentationCSerialized.FindProperty("requestPrecedence")
                    .intValue = 100;
                presentationCSerialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(presentationC, PresentationCPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                presentationC =
                    AssetDatabase.LoadAssetAtPath<
                        CameraPresentationDefinition>(PresentationCPath);
            }

            if (presentationC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition could not create or reload Presentation C.");
                return;
            }

            RouteAsset routeC =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteCPath);
            if (routeC == null)
            {
                CreateRoute(
                    RouteCPath,
                    "qa-new-004.route-c",
                    "QA-NEW-004 Route C",
                    RouteCScenePath,
                    presentationC);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                routeC = LoadPersistedRoute(RouteCPath);
            }

            if (routeC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition could not create or reload the Route C asset.");
                return;
            }

            if (AssetDatabase.LoadMainAssetAtPath(RouteCScenePath) == null)
            {
                CreateRouteScene(RouteCPath, RouteCScenePath, "route-c");
                AssetDatabase.Refresh();
            }

            if (AssetDatabase.LoadMainAssetAtPath(RouteCScenePath) == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition could not create or find the Route C scene.");
                return;
            }

            if (!VerifyRouteScene(RouteCScenePath, RouteCPath))
            {
                return;
            }

            // --- Wire the persistent scene ---
            EditorSceneManager.OpenScene(
                PersistentScenePath,
                OpenSceneMode.Single);
            QaNew004RouteCameraSelectionScenario scenario =
                UnityEngine.Object.FindAnyObjectByType<
                    QaNew004RouteCameraSelectionScenario>();
            if (scenario == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition could not find the existing Scenario in the persistent scene.");
                return;
            }

            // CRITICAL: reload Route C fresh from disk AFTER opening the
            // persistent scene. A RouteAsset reference captured before this
            // scene switch can go stale (the same class of bug previously
            // seen with EditorSceneManager.NewScene/OpenScene) and must
            // never be serialized without reloading it post-switch.
            routeC = LoadPersistedRoute(RouteCPath);
            if (routeC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition could not reload Route C after opening the persistent scene.");
                return;
            }

            RouteRequestTrigger requestC = FindOrCreateRouteCTrigger(scenario);
            ConfigureTrigger(
                requestC,
                routeC,
                "QA-NEW-004 replace the persistent selection through Route C (CAMERA-037-C)");

            SerializedObject serializedScenario =
                new SerializedObject(scenario);
            serializedScenario.FindProperty("routeC").objectReferenceValue =
                routeC;
            serializedScenario.FindProperty("requestRouteC")
                .objectReferenceValue = requestC;
            serializedScenario.ApplyModifiedPropertiesWithoutUndo();

            // Round-trip verify the IN-MEMORY wiring before saving anything
            // to disk, so a stale/broken reference aborts loudly instead of
            // silently persisting a partially-wired scene.
            serializedScenario.Update();
            UnityEngine.Object wiredRouteC = serializedScenario
                .FindProperty("routeC").objectReferenceValue;
            RouteRequestTrigger wiredTrigger = serializedScenario
                .FindProperty("requestRouteC").objectReferenceValue
                as RouteRequestTrigger;
            if (wiredRouteC == null ||
                wiredTrigger == null ||
                wiredTrigger.TargetRoute != routeC)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition failed to wire routeC/requestRouteC onto the Scenario in memory. Aborting without saving a broken persistent scene.");
                return;
            }

            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                PersistentScenePath);

            var scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);
            AddBuildScene(scenes, RouteCScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Definitive verification: reopen the persistent scene FROM
            // DISK and read back what was actually serialized, not the
            // in-memory state that was just saved.
            EditorSceneManager.OpenScene(
                PersistentScenePath,
                OpenSceneMode.Single);
            QaNew004RouteCameraSelectionScenario verifyScenario =
                UnityEngine.Object.FindAnyObjectByType<
                    QaNew004RouteCameraSelectionScenario>();
            RouteAsset verifyRouteC = LoadPersistedRoute(RouteCPath);
            if (verifyScenario == null || verifyRouteC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Route C composition verification could not reload the Scenario or Route C.");
                return;
            }

            SerializedObject verifySerializedScenario =
                new SerializedObject(verifyScenario);
            UnityEngine.Object savedRouteC = verifySerializedScenario
                .FindProperty("routeC").objectReferenceValue;
            RouteRequestTrigger savedRequestC = verifySerializedScenario
                .FindProperty("requestRouteC").objectReferenceValue
                as RouteRequestTrigger;
            if (savedRouteC != verifyRouteC ||
                savedRequestC == null ||
                savedRequestC.TargetRoute != verifyRouteC)
            {
                Debug.LogError(
                    $"QA-NEW-004 Route C composition did not persist the Scenario's Route C or Route C Request Trigger to disk. routeC='{(savedRouteC != null ? "set" : "NULL")}' requestRouteC='{(savedRequestC != null ? "set" : "NULL")}' targetRouteMatches='{(savedRequestC != null && savedRequestC.TargetRoute == verifyRouteC)}'.");
                return;
            }

            Selection.activeObject = verifyScenario;
            Debug.Log(
                "[QA-NEW-004] Route C replacement composition ready and verified on disk. The Scenario now proves CAMERA-037-C (A -> C same-Output replacement) after the existing A -> B leg.");
        }

        private static RouteRequestTrigger FindOrCreateRouteCTrigger(
            QaNew004RouteCameraSelectionScenario scenario)
        {
            // Reuse a previously created "Request Route C" child (e.g. from
            // an earlier partial run) instead of creating a duplicate; it
            // will be re-wired unconditionally by the caller regardless of
            // whether its current targetRoute is valid.
            Transform existing = scenario.transform.Find("Request Route C");
            if (existing != null)
            {
                RouteRequestTrigger existingTrigger =
                    existing.GetComponent<RouteRequestTrigger>();
                if (existingTrigger != null)
                {
                    return existingTrigger;
                }
            }

            GameObject requestCRoot = new GameObject("Request Route C");
            requestCRoot.transform.SetParent(scenario.transform, false);
            return requestCRoot.AddComponent<RouteRequestTrigger>();
        }

        [MenuItem("QA/QA-NEW-004/Add Activity D Continuity Composition")]
        private static void AddActivityContinuityComposition()
        {
            for (int index = 0; index < GeneratedPaths.Length; index++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(GeneratedPaths[index]) == null)
                {
                    Debug.LogError(
                        "QA-NEW-004 Activity D composition requires the base composition (Route A/B, Camera Output, Game Application) to already exist. Run 'Create Isolated Composition' first.");
                    return;
                }
            }

            CameraPresentationDefinition presentationA =
                AssetDatabase.LoadAssetAtPath<CameraPresentationDefinition>(
                    PresentationPath);
            if (presentationA == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not load Presentation A to reuse for Activity D-A's persistent selection.");
                return;
            }

            // --- Activity D-A: persistent selection (reuses Presentation A) ---
            ActivityAsset activityDA =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDAPath);
            if (activityDA == null)
            {
                CreateActivity(
                    ActivityDAPath,
                    "qa-new-004.activity-d-a",
                    "QA-NEW-004 Activity D-A",
                    null,
                    presentationA);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                activityDA =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDAPath);
            }

            if (activityDA == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not create or reload Activity D-A.");
                return;
            }

            ActivityContentProfileAsset activityDAProfile =
                AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(
                    ActivityDAContentProfilePath);
            if (activityDAProfile == null)
            {
                activityDAProfile =
                    ScriptableObject.CreateInstance<
                        ActivityContentProfileAsset>();
                activityDAProfile.name =
                    "ActivityContentProfile_QaNew004_D_A";
                SerializedObject profileSerialized =
                    new SerializedObject(activityDAProfile);
                profileSerialized.FindProperty("profileId").stringValue =
                    "qa-new-004.activity-d-a.profile";
                SerializedProperty scenesProperty =
                    profileSerialized.FindProperty("scenes");
                scenesProperty.arraySize = 1;
                SerializedProperty entry =
                    scenesProperty.GetArrayElementAtIndex(0);
                entry.FindPropertyRelative("contentId").stringValue =
                    ActivityDAContentId;
                entry.FindPropertyRelative("scenePath").stringValue =
                    ActivityDAScenePath;
                entry.FindPropertyRelative("sceneName").stringValue =
                    "QA_NEW_004_ActivityDA";
                entry.FindPropertyRelative("requiredness").intValue = 10;
                profileSerialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(
                    activityDAProfile,
                    ActivityDAContentProfilePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                activityDAProfile =
                    AssetDatabase.LoadAssetAtPath<
                        ActivityContentProfileAsset>(
                        ActivityDAContentProfilePath);
            }

            if (activityDAProfile == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not create or reload the Activity D-A content profile.");
                return;
            }

            // Assign the profile onto Activity D-A if it is not wired yet
            // (idempotent: re-check via a fresh SerializedObject read).
            SerializedObject activityDASerialized =
                new SerializedObject(activityDA);
            if (activityDASerialized
                    .FindProperty("activityContentProfile")
                    .objectReferenceValue != activityDAProfile)
            {
                activityDASerialized
                        .FindProperty("activityContentProfile")
                        .objectReferenceValue =
                    activityDAProfile;
                activityDASerialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }

            if (AssetDatabase.LoadMainAssetAtPath(ActivityDAScenePath) == null)
            {
                CreateActivityDAScene();
                AssetDatabase.Refresh();
            }

            if (AssetDatabase.LoadMainAssetAtPath(ActivityDAScenePath) == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not create or find the Activity D-A content scene.");
                return;
            }

            // --- Activity D-B: zero selections, no content ---
            ActivityAsset activityDB =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDBPath);
            if (activityDB == null)
            {
                CreateActivity(
                    ActivityDBPath,
                    "qa-new-004.activity-d-b",
                    "QA-NEW-004 Activity D-B",
                    null,
                    null);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                activityDB =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDBPath);
            }

            if (activityDB == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not create or reload Activity D-B.");
                return;
            }

            // --- Route D: no persistent selection of its own; StartupActivity = Activity D-A ---
            // CRITICAL: reload Activity D-A fresh from disk here. It was
            // last loaded before CreateActivityDAScene() above, which
            // performed a scene switch (EditorSceneManager.NewScene) that
            // can invalidate a previously-held asset reference — the same
            // class of bug already fixed for Route C's persistent-scene
            // wiring. Never reuse an asset reference captured before a
            // scene switch without reloading it first.
            activityDA =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDAPath);
            if (activityDA == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not reload Activity D-A before wiring Route D's Startup Activity.");
                return;
            }

            RouteAsset routeD =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteDPath);
            if (routeD == null)
            {
                CreateRoute(
                    RouteDPath,
                    "qa-new-004.route-d",
                    "QA-NEW-004 Route D",
                    RouteDScenePath,
                    null);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                routeD = AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteDPath);
            }

            if (routeD == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not create or reload Route D.");
                return;
            }

            if (routeD.StartupActivity != activityDA)
            {
                SerializedObject routeDSerialized =
                    new SerializedObject(routeD);
                routeDSerialized.FindProperty("startupActivity")
                        .objectReferenceValue =
                    activityDA;
                routeDSerialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Verify the write actually landed by rereading both
                // assets fresh from disk, not from the in-memory
                // SerializedObject/local variable we just wrote with.
                RouteAsset verifyRouteDStartup =
                    AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteDPath);
                ActivityAsset verifyActivityDAForRouteD =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                        ActivityDAPath);
                if (verifyRouteDStartup == null ||
                    verifyActivityDAForRouteD == null ||
                    verifyRouteDStartup.StartupActivity !=
                        verifyActivityDAForRouteD)
                {
                    Debug.LogError(
                        "QA-NEW-004 Activity D composition failed to persist Route D's Startup Activity = Activity D-A. Aborting without proceeding.");
                    return;
                }

                routeD = verifyRouteDStartup;
                activityDA = verifyActivityDAForRouteD;
            }

            if (AssetDatabase.LoadMainAssetAtPath(RouteDScenePath) == null)
            {
                CreateRouteScene(RouteDPath, RouteDScenePath, "route-d");
                AssetDatabase.Refresh();
            }

            if (AssetDatabase.LoadMainAssetAtPath(RouteDScenePath) == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not create or find the Route D scene.");
                return;
            }

            if (!VerifyRouteScene(RouteDScenePath, RouteDPath))
            {
                return;
            }

            // --- Wire the persistent scene ---
            EditorSceneManager.OpenScene(
                PersistentScenePath,
                OpenSceneMode.Single);
            QaNew004RouteCameraSelectionScenario scenario =
                UnityEngine.Object.FindAnyObjectByType<
                    QaNew004RouteCameraSelectionScenario>();
            if (scenario == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not find the existing Scenario in the persistent scene.");
                return;
            }

            // Reload fresh from disk AFTER opening the persistent scene (see
            // the Route C composition fix: a reference captured before this
            // scene switch can go stale).
            routeD = AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteDPath);
            activityDA =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDAPath);
            activityDB =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDBPath);
            if (routeD == null || activityDA == null || activityDB == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition could not reload Route D, Activity D-A or Activity D-B after opening the persistent scene.");
                return;
            }

            RouteRequestTrigger requestRouteD =
                FindOrCreateChildTrigger<RouteRequestTrigger>(
                    scenario,
                    "Request Route D");
            ConfigureTrigger(
                requestRouteD,
                routeD,
                "QA-NEW-004 enter Route D (Startup Activity D-A selects Camera A)");

            ActivityRequestTrigger requestActivityDB =
                FindOrCreateChildTrigger<ActivityRequestTrigger>(
                    scenario,
                    "Request Activity D-B");
            SerializedObject requestActivityDBSerialized =
                new SerializedObject(requestActivityDB);
            requestActivityDBSerialized
                .FindProperty("targetActivity").objectReferenceValue =
                activityDB;
            requestActivityDBSerialized.FindProperty("reason").stringValue =
                "QA-NEW-004 preserve selection while entering empty Activity D-B";
            requestActivityDBSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedScenario =
                new SerializedObject(scenario);
            serializedScenario.FindProperty("routeD").objectReferenceValue =
                routeD;
            serializedScenario.FindProperty("activityDA").objectReferenceValue =
                activityDA;
            serializedScenario.FindProperty("activityDB").objectReferenceValue =
                activityDB;
            serializedScenario.FindProperty("requestRouteD")
                .objectReferenceValue = requestRouteD;
            serializedScenario.FindProperty("requestActivityDB")
                .objectReferenceValue = requestActivityDB;
            serializedScenario.ApplyModifiedPropertiesWithoutUndo();

            // Round-trip verify the IN-MEMORY wiring before saving.
            serializedScenario.Update();
            bool wiredOk =
                serializedScenario.FindProperty("routeD")
                    .objectReferenceValue == routeD &&
                serializedScenario.FindProperty("activityDA")
                    .objectReferenceValue == activityDA &&
                serializedScenario.FindProperty("activityDB")
                    .objectReferenceValue == activityDB &&
                serializedScenario.FindProperty("requestRouteD")
                    .objectReferenceValue == requestRouteD &&
                serializedScenario.FindProperty("requestActivityDB")
                    .objectReferenceValue == requestActivityDB &&
                requestRouteD.TargetRoute == routeD &&
                requestActivityDB.TargetActivity == activityDB;
            if (!wiredOk)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition failed to wire Route D/Activity D-A/D-B onto the Scenario in memory. Aborting without saving.");
                return;
            }

            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                PersistentScenePath);

            var scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);
            AddBuildScene(scenes, RouteDScenePath);
            AddBuildScene(scenes, ActivityDAScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Definitive verification: reopen the persistent scene FROM DISK.
            EditorSceneManager.OpenScene(
                PersistentScenePath,
                OpenSceneMode.Single);
            QaNew004RouteCameraSelectionScenario verifyScenario =
                UnityEngine.Object.FindAnyObjectByType<
                    QaNew004RouteCameraSelectionScenario>();
            RouteAsset verifyRouteD =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteDPath);
            ActivityAsset verifyActivityDB =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDBPath);
            if (verifyScenario == null ||
                verifyRouteD == null ||
                verifyActivityDB == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition verification could not reload the Scenario, Route D or Activity D-B.");
                return;
            }

            SerializedObject verifySerializedScenario =
                new SerializedObject(verifyScenario);
            UnityEngine.Object savedRouteD = verifySerializedScenario
                .FindProperty("routeD").objectReferenceValue;
            UnityEngine.Object savedActivityDB = verifySerializedScenario
                .FindProperty("activityDB").objectReferenceValue;
            RouteRequestTrigger savedRequestRouteD = verifySerializedScenario
                .FindProperty("requestRouteD").objectReferenceValue
                as RouteRequestTrigger;
            ActivityRequestTrigger savedRequestActivityDB =
                verifySerializedScenario
                    .FindProperty("requestActivityDB").objectReferenceValue
                    as ActivityRequestTrigger;
            if (savedRouteD != verifyRouteD ||
                savedActivityDB != verifyActivityDB ||
                savedRequestRouteD == null ||
                savedRequestRouteD.TargetRoute != verifyRouteD ||
                savedRequestActivityDB == null ||
                savedRequestActivityDB.TargetActivity != verifyActivityDB)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity D composition did not persist Route D / Activity D-B wiring to disk.");
                return;
            }

            Selection.activeObject = verifyScenario;
            Debug.Log(
                "[QA-NEW-004] Activity D continuity composition ready and verified on disk. The Scenario can now prove CAMERA-037-D (Activity A -> B empty preserves selection) through Route D's real ActivityFlowRuntime.");
        }

        [MenuItem("QA/QA-NEW-004/Add Activity E Replacement Composition")]
        private static void AddActivityReplacementComposition()
        {
            for (int index = 0; index < GeneratedPaths.Length; index++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(GeneratedPaths[index]) == null)
                {
                    Debug.LogError(
                        "QA-NEW-004 Activity E composition requires the base composition. Run 'Create Isolated Composition' first.");
                    return;
                }
            }

            CameraPresentationDefinition presentationA =
                AssetDatabase.LoadAssetAtPath<CameraPresentationDefinition>(
                    PresentationPath);
            CameraPresentationDefinition presentationC =
                AssetDatabase.LoadAssetAtPath<CameraPresentationDefinition>(
                    PresentationCPath);
            if (presentationA == null || presentationC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity E composition requires Presentations A and C. Run 'Add Route C Replacement Composition' first.");
                return;
            }

            ActivityContentProfileAsset activityEAProfile =
                AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(
                    ActivityEAContentProfilePath);
            if (activityEAProfile == null)
            {
                activityEAProfile =
                    ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
                activityEAProfile.name =
                    "ActivityContentProfile_QaNew004_E_A";
                AssetDatabase.CreateAsset(
                    activityEAProfile,
                    ActivityEAContentProfilePath);
            }

            SerializedObject profileSerialized =
                new SerializedObject(activityEAProfile);
            profileSerialized.FindProperty("profileId").stringValue =
                "qa-new-004.activity-e-a.profile";
            SerializedProperty profileScenes =
                profileSerialized.FindProperty("scenes");
            profileScenes.arraySize = 1;
            SerializedProperty profileEntry =
                profileScenes.GetArrayElementAtIndex(0);
            profileEntry.FindPropertyRelative("contentId").stringValue =
                ActivityEAContentId;
            profileEntry.FindPropertyRelative("scenePath").stringValue =
                ActivityEAScenePath;
            profileEntry.FindPropertyRelative("sceneName").stringValue =
                "QA_NEW_004_ActivityEA";
            profileEntry.FindPropertyRelative("requiredness").intValue = 10;
            profileSerialized.ApplyModifiedPropertiesWithoutUndo();

            ActivityAsset activityEA =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityEAPath);
            if (activityEA == null)
            {
                CreateActivity(
                    ActivityEAPath,
                    "qa-new-004.activity-e-a",
                    "QA-NEW-004 Activity E-A",
                    activityEAProfile,
                    presentationA);
                activityEA =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityEAPath);
            }

            ActivityAsset activityEC =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityECPath);
            if (activityEC == null)
            {
                CreateActivity(
                    ActivityECPath,
                    "qa-new-004.activity-e-c",
                    "QA-NEW-004 Activity E-C",
                    null,
                    presentationC);
                activityEC =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityECPath);
            }

            if (activityEA == null || activityEC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity E composition could not create or reload Activities E-A/E-C.");
                return;
            }

            ConfigureActivity(
                activityEA,
                "qa-new-004.activity-e-a",
                "QA-NEW-004 Activity E-A",
                activityEAProfile,
                presentationA);
            ConfigureActivity(
                activityEC,
                "qa-new-004.activity-e-c",
                "QA-NEW-004 Activity E-C",
                null,
                presentationC);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Deterministically rebuild the generated Activity scene so a
            // partial setup run is repaired instead of retaining stale
            // contribution/probe references.
            CreateActivityEAScene();
            AssetDatabase.Refresh();

            // NewScene invalidates previously-held UnityEngine.Object
            // references in this workflow. Reload every asset used below.
            activityEA =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityEAPath);
            activityEC =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityECPath);
            presentationA =
                AssetDatabase.LoadAssetAtPath<CameraPresentationDefinition>(
                    PresentationPath);
            presentationC =
                AssetDatabase.LoadAssetAtPath<CameraPresentationDefinition>(
                    PresentationCPath);
            if (activityEA == null || activityEC == null ||
                presentationA == null || presentationC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity E composition could not reload its assets after rebuilding the Activity E-A scene.");
                return;
            }

            RouteAsset routeE =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteEPath);
            if (routeE == null)
            {
                CreateRoute(
                    RouteEPath,
                    "qa-new-004.route-e",
                    "QA-NEW-004 Route E",
                    RouteEScenePath,
                    null);
                routeE = AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteEPath);
            }

            if (routeE == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity E composition could not create or reload Route E.");
                return;
            }

            SerializedObject routeESerialized = new SerializedObject(routeE);
            routeESerialized.FindProperty("routeId").stringValue =
                "qa-new-004.route-e";
            routeESerialized.FindProperty("routeName").stringValue =
                "QA-NEW-004 Route E";
            routeESerialized.FindProperty("primaryScenePath").stringValue =
                RouteEScenePath;
            routeESerialized.FindProperty("primarySceneName").stringValue =
                "QA_NEW_004_RouteE";
            routeESerialized.FindProperty("cameraPresentations").arraySize = 0;
            routeESerialized.FindProperty("cameraPresentationSelections")
                .arraySize = 0;
            routeESerialized.FindProperty("startupActivity")
                .objectReferenceValue = activityEA;
            routeESerialized.FindProperty("transitionGateMode").intValue = 30;
            routeESerialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Rebuild Route E's generated scene, then reload again before
            // crossing into the persistent scene.
            CreateRouteScene(RouteEPath, RouteEScenePath, "route-e");
            AssetDatabase.Refresh();
            if (!VerifyRouteScene(RouteEScenePath, RouteEPath))
            {
                return;
            }

            EditorSceneManager.OpenScene(
                PersistentScenePath,
                OpenSceneMode.Single);
            QaNew004RouteCameraSelectionScenario scenario =
                UnityEngine.Object.FindAnyObjectByType<
                    QaNew004RouteCameraSelectionScenario>();
            routeE = AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteEPath);
            activityEA =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityEAPath);
            activityEC =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityECPath);
            if (scenario == null || routeE == null || activityEA == null ||
                activityEC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity E composition could not reload the Scenario, Route E or Activities E-A/E-C after opening the persistent scene.");
                return;
            }

            RouteRequestTrigger requestRouteE =
                FindOrCreateChildTrigger<RouteRequestTrigger>(
                    scenario,
                    "Request Route E");
            ConfigureTrigger(
                requestRouteE,
                routeE,
                "QA-NEW-004 enter Route E (Startup Activity E-A selects Camera A)");
            ActivityRequestTrigger requestActivityEC =
                FindOrCreateChildTrigger<ActivityRequestTrigger>(
                    scenario,
                    "Request Activity E-C");
            SerializedObject requestActivityECSerialized =
                new SerializedObject(requestActivityEC);
            requestActivityECSerialized.FindProperty("targetActivity")
                .objectReferenceValue = activityEC;
            requestActivityECSerialized.FindProperty("reason").stringValue =
                "QA-NEW-004 replace Activity E-A Camera A with Activity E-C Camera C";
            requestActivityECSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serializedScenario =
                new SerializedObject(scenario);
            serializedScenario.FindProperty("routeE").objectReferenceValue =
                routeE;
            serializedScenario.FindProperty("activityEA")
                .objectReferenceValue = activityEA;
            serializedScenario.FindProperty("activityEC")
                .objectReferenceValue = activityEC;
            serializedScenario.FindProperty("requestRouteE")
                .objectReferenceValue = requestRouteE;
            serializedScenario.FindProperty("requestActivityEC")
                .objectReferenceValue = requestActivityEC;
            serializedScenario.ApplyModifiedPropertiesWithoutUndo();

            serializedScenario.Update();
            bool wiredOk =
                serializedScenario.FindProperty("routeE")
                    .objectReferenceValue == routeE &&
                serializedScenario.FindProperty("activityEA")
                    .objectReferenceValue == activityEA &&
                serializedScenario.FindProperty("activityEC")
                    .objectReferenceValue == activityEC &&
                serializedScenario.FindProperty("requestRouteE")
                    .objectReferenceValue == requestRouteE &&
                serializedScenario.FindProperty("requestActivityEC")
                    .objectReferenceValue == requestActivityEC &&
                requestRouteE.TargetRoute == routeE &&
                requestActivityEC.TargetActivity == activityEC;
            if (!wiredOk)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity E composition failed its in-memory wiring round trip. Aborting without saving.");
                return;
            }

            EditorSceneManager.SaveScene(
                EditorSceneManager.GetActiveScene(),
                PersistentScenePath);
            var scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes);
            AddBuildScene(scenes, RouteEScenePath);
            AddBuildScene(scenes, ActivityEAScenePath);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Final disk round trip: reopen and compare only freshly loaded
            // objects, never the references held across scene switches.
            EditorSceneManager.OpenScene(
                PersistentScenePath,
                OpenSceneMode.Single);
            QaNew004RouteCameraSelectionScenario verifyScenario =
                UnityEngine.Object.FindAnyObjectByType<
                    QaNew004RouteCameraSelectionScenario>();
            RouteAsset verifyRouteE =
                AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteEPath);
            ActivityAsset verifyActivityEA =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityEAPath);
            ActivityAsset verifyActivityEC =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityECPath);
            CameraPresentationDefinition verifyPresentationA =
                AssetDatabase.LoadAssetAtPath<CameraPresentationDefinition>(
                    PresentationPath);
            CameraPresentationDefinition verifyPresentationC =
                AssetDatabase.LoadAssetAtPath<CameraPresentationDefinition>(
                    PresentationCPath);
            if (verifyScenario == null || verifyRouteE == null ||
                verifyActivityEA == null || verifyActivityEC == null ||
                verifyPresentationA == null || verifyPresentationC == null)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity E disk verification could not reload its authored surface.");
                return;
            }

            SerializedObject verifySerializedScenario =
                new SerializedObject(verifyScenario);
            RouteRequestTrigger savedRequestRouteE =
                verifySerializedScenario.FindProperty("requestRouteE")
                    .objectReferenceValue as RouteRequestTrigger;
            ActivityRequestTrigger savedRequestActivityEC =
                verifySerializedScenario.FindProperty("requestActivityEC")
                    .objectReferenceValue as ActivityRequestTrigger;
            bool persisted =
                verifySerializedScenario.FindProperty("routeE")
                    .objectReferenceValue == verifyRouteE &&
                verifySerializedScenario.FindProperty("activityEA")
                    .objectReferenceValue == verifyActivityEA &&
                verifySerializedScenario.FindProperty("activityEC")
                    .objectReferenceValue == verifyActivityEC &&
                savedRequestRouteE != null &&
                savedRequestRouteE.TargetRoute == verifyRouteE &&
                savedRequestActivityEC != null &&
                savedRequestActivityEC.TargetActivity == verifyActivityEC &&
                verifyRouteE.StartupActivity == verifyActivityEA &&
                verifyActivityEA.CameraPresentationSelections.Count == 1 &&
                verifyActivityEA.CameraPresentationSelections[0] ==
                    verifyPresentationA &&
                verifyActivityEC.CameraPresentationSelections.Count == 1 &&
                verifyActivityEC.CameraPresentationSelections[0] ==
                    verifyPresentationC &&
                verifyPresentationA.OutputDefinition ==
                    verifyPresentationC.OutputDefinition;
            if (!persisted)
            {
                Debug.LogError(
                    "QA-NEW-004 Activity E composition did not survive the disk round trip.");
                return;
            }

            Selection.activeObject = verifyScenario;
            Debug.Log(
                "[QA-NEW-004] Activity E replacement composition ready and verified on disk. The Scenario can now prove CAMERA-037-E through the public Activity lifecycle.");
        }

        private static T FindOrCreateChildTrigger<T>(
            Component parent,
            string childName)
            where T : Component
        {
            Transform existing = parent.transform.Find(childName);
            if (existing != null)
            {
                T existingTrigger = existing.GetComponent<T>();
                if (existingTrigger != null)
                {
                    return existingTrigger;
                }
            }

            GameObject root = new GameObject(childName);
            root.transform.SetParent(parent.transform, false);
            return root.AddComponent<T>();
        }

        private static void CreateActivityDAScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityDAPath);
            if (activity == null)
            {
                Debug.LogError(
                    "QA-NEW-004 could not reload Activity D-A after creating its content scene.");
                return;
            }

            GameObject root =
                new GameObject(activity.ActivityName + " Content");
            ActivityContentContribution contribution =
                root.AddComponent<ActivityContentContribution>();
            root.AddComponent<QaNew004ActivityLifecycleProbe>();
            SerializedObject serializedContribution =
                new SerializedObject(contribution);
            serializedContribution.FindProperty("activity")
                .objectReferenceValue = activity;
            serializedContribution.FindProperty("localContentId")
                .stringValue = ActivityDAContentId;
            serializedContribution.FindProperty("requiredness").intValue =
                10;
            serializedContribution.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ActivityDAScenePath);
        }

        private static void CreateActivityEAScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityEAPath);
            if (activity == null)
            {
                throw new InvalidOperationException(
                    "QA-NEW-004 could not reload Activity E-A after creating its content scene.");
            }

            GameObject root =
                new GameObject(activity.ActivityName + " Content");
            ActivityContentContribution contribution =
                root.AddComponent<ActivityContentContribution>();
            root.AddComponent<QaNew004ActivityLifecycleProbe>();
            SerializedObject serializedContribution =
                new SerializedObject(contribution);
            serializedContribution.FindProperty("activity")
                .objectReferenceValue = activity;
            serializedContribution.FindProperty("localContentId")
                .stringValue = ActivityEAContentId;
            serializedContribution.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ActivityEAScenePath);
        }

        private static void CreateActivity(
            string assetPath,
            string activityId,
            string activityName,
            ActivityContentProfileAsset contentProfile,
            CameraPresentationDefinition selection)
        {
            ActivityAsset activity =
                ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = activityName.Replace(" ", string.Empty);
            SerializedObject serializedActivity =
                new SerializedObject(activity);
            serializedActivity.FindProperty("activityId").stringValue =
                activityId;
            serializedActivity.FindProperty("activityName").stringValue =
                activityName;
            if (contentProfile != null)
            {
                serializedActivity.FindProperty("activityContentProfile")
                    .objectReferenceValue = contentProfile;
            }

            serializedActivity.FindProperty("cameraPresentations").arraySize =
                0;
            SerializedProperty selections =
                serializedActivity.FindProperty(
                    "cameraPresentationSelections");
            selections.arraySize = selection != null ? 1 : 0;
            if (selection != null)
            {
                selections.GetArrayElementAtIndex(0).objectReferenceValue =
                    selection;
            }

            serializedActivity.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(activity, assetPath);
        }

        private static void ConfigureActivity(
            ActivityAsset activity,
            string activityId,
            string activityName,
            ActivityContentProfileAsset contentProfile,
            CameraPresentationDefinition selection)
        {
            SerializedObject serializedActivity =
                new SerializedObject(activity);
            serializedActivity.FindProperty("activityId").stringValue =
                activityId;
            serializedActivity.FindProperty("activityName").stringValue =
                activityName;
            serializedActivity.FindProperty("activityContentProfile")
                .objectReferenceValue = contentProfile;
            serializedActivity.FindProperty("cameraPresentations").arraySize = 0;
            SerializedProperty selections =
                serializedActivity.FindProperty(
                    "cameraPresentationSelections");
            selections.arraySize = selection != null ? 1 : 0;
            if (selection != null)
            {
                selections.GetArrayElementAtIndex(0).objectReferenceValue =
                    selection;
            }

            serializedActivity.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);
        }

        private static GameObject CreatePresentationRigPrefabAt(
            string prefabPath,
            string rootName,
            FixedCameraRigBehaviorDefinition behavior)
        {
            GameObject root = new GameObject(rootName);
            try
            {
                root.AddComponent<QaNew004CameraOccurrenceProbe>();
                ConfigureFixedComposer(root, behavior);
                return PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
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

using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Focused IF-ADR-032 certification for transition force-default continuity
    /// and Route/Activity Camera Presentation restoration.
    /// </summary>
    internal static class QaCamera032GameFlowLifecycleCertification
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run CAMERA-032 Game Flow Lifecycle Certification";
        private const string Prefix =
            "[CAMERA-032-GAMEFLOW-CERTIFICATION]";
        private const string PhaseKey =
            "ImmersiveFrameworkQA.CAMERA_032_GameFlow.Phase";
        private const string FailureKey =
            "ImmersiveFrameworkQA.CAMERA_032_GameFlow.Failure";
        private const string SessionCaseCountKey =
            "ImmersiveFrameworkQA.CAMERA_032_GameFlow.SessionCases";
        private const string DefaultCaseCountKey =
            "ImmersiveFrameworkQA.CAMERA_032_GameFlow.DefaultCases";
        private const string AggregateResultKey =
            "ImmersiveFrameworkQA.CAMERA_032_GameFlow.AggregatePassed";
        private const double TimeoutSeconds = 360d;

        private const string GeneratedRoot =
            "Assets/ImmersiveFrameworkQA/Camera/Generated032GameFlow";
        private const string CanonicalGameApplicationPath =
            "Assets/ImmersiveFrameworkQA/GameApplications/GameApplication.asset";
        private const string BackupRoot =
            "Assets/ImmersiveFrameworkQA/Camera/Generated032GameFlowBackup";
        private const string BackupGameApplicationPath =
            BackupRoot + "/GameApplication.asset";
        private const string RouteAPath =
            GeneratedRoot + "/QA_Camera032_GameFlow_RouteA.asset";
        private const string RouteBPath =
            GeneratedRoot + "/QA_Camera032_GameFlow_RouteB.asset";
        private const string ActivityAPath =
            GeneratedRoot + "/QA_Camera032_GameFlow_ActivityA.asset";
        private const string PresentationRigPath =
            GeneratedRoot + "/PF_QA_Camera032_GameFlow_Presentation.prefab";
        private const string OutputPrefabPath =
            GeneratedRoot + "/PF_QA_Camera032_GameFlow_Output.prefab";
        private const string SessionPresentationPath =
            GeneratedRoot + "/CameraPresentation_QA_Camera032_Session.asset";
        private const string RoutePresentationPath =
            GeneratedRoot + "/CameraPresentation_QA_Camera032_Route.asset";
        private const string ActivityPresentationPath =
            GeneratedRoot + "/CameraPresentation_QA_Camera032_Activity.asset";

        private const string PersistentScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";
        private const string StartupScenePath =
            GeneratedRoot + "/QA_Camera032_GameFlow_RouteA.unity";
        private const string ReplacementScenePath =
            GeneratedRoot + "/QA_Camera032_GameFlow_RouteB.unity";
        private const string StartupSceneName =
            "QA_Camera032_GameFlow_RouteA";
        private const string ReplacementSceneName =
            "QA_Camera032_GameFlow_RouteB";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string OutputDefinitionPath =
            "Assets/ImmersiveFrameworkQA/Camera/Definitions/MainOutput.asset";
        private const string FixedBehaviorPath =
            "Assets/ImmersiveFrameworkQA/Camera/Definitions/FixedBehavior.asset";
        private const string RegressionRootName =
            "[QA CAMERA-032] Game Flow Lifecycle";

        private static double startedAt;
        private static bool watching;

        private enum Phase
        {
            Idle = 0,
            RunningSessionFallback = 10,
            SessionFallbackPassed = 20,
            RunningDefaultFallback = 30,
            DefaultFallbackPassed = 40,
            Failed = 50,
            Completed = 60
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (CurrentPhase == Phase.SessionFallbackPassed)
            {
                EditorApplication.delayCall -= BeginDefaultPhase;
                EditorApplication.delayCall += BeginDefaultPhase;
            }
            else if (CurrentPhase == Phase.DefaultFallbackPassed)
            {
                EditorApplication.delayCall -= FinishSuccess;
                EditorApplication.delayCall += FinishSuccess;
            }
            else if (CurrentPhase is
                Phase.RunningSessionFallback or
                Phase.RunningDefaultFallback)
            {
                EditorApplication.delayCall -= RecoverInterruptedRun;
                EditorApplication.delayCall += RecoverInterruptedRun;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            CurrentPhase is Phase.Idle or Phase.Failed or Phase.Completed;

        [MenuItem(MenuPath, priority = 236)]
        private static void Run()
        {
            SessionState.SetBool(
                AggregateResultKey,
                false);
            StopWatching();
            SessionState.EraseString(FailureKey);
            SessionState.EraseInt(SessionCaseCountKey);
            SessionState.EraseInt(DefaultCaseCountKey);

            try
            {
                RecoverBackupBeforeNewRun();

                // Match the established Full Camera / CAMERA-028-D rail:
                // repair and verify the canonical persisted baseline first,
                // then mutate only the already-active canonical GameApplication.
                QaCameraPersistentBaselineGuard.PrepareAndVerify();
                PrepareGeneratedAssets();
                BackupCanonicalGameApplication();
                PreparePhase(true);
                SetPhase(Phase.RunningSessionFallback);
                Debug.Log(
                    $"{Prefix} status='Running' phase='SessionFallback' " +
                    "proof='Activity->Route; Route->Session; force-default continuity; Route replacement'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "prepare-session",
                    exception.GetBaseException().Message);
                FinishFailure();
            }
        }

        internal static void RunForAggregate()
        {
            Run();
        }

        internal static bool AggregateCompleted =>
            CurrentPhase == Phase.Completed &&
            SessionState.GetBool(
                AggregateResultKey,
                false);

        internal static bool AggregateFailed =>
            CurrentPhase == Phase.Failed;

        private static void PrepareGeneratedAssets()
        {
            RequireEnabledBuildScene(PersistentScenePath);
            RequireEnabledBuildScene(HubScenePath);

            RemoveGeneratedBuildScenes();
            AssetDatabase.DeleteAsset(GeneratedRoot);
            EnsureFolder(GeneratedRoot);

            CreateCameraFreeRouteScene(
                StartupScenePath);
            CreateCameraFreeRouteScene(
                ReplacementScenePath);
            EnsureGeneratedBuildScenes();

            RequireEnabledBuildScene(StartupScenePath);
            RequireEnabledBuildScene(ReplacementScenePath);

            CameraOutputDefinition outputDefinition =
                QaCameraDefinitionAssetGuard
                    .RequireDefinition<CameraOutputDefinition>(
                        OutputDefinitionPath);
            FixedCameraRigBehaviorDefinition fixedBehavior =
                QaCameraDefinitionAssetGuard
                    .RequireBehavior<FixedCameraRigBehaviorDefinition>(
                        FixedBehaviorPath);

            GameObject outputPrefab =
                CreateOutputPrefab(
                    OutputPrefabPath,
                    outputDefinition,
                    fixedBehavior);
            GameObject presentationRig =
                CreatePresentationRigPrefab(
                    PresentationRigPath,
                    fixedBehavior);

            CameraPresentationDefinition sessionPresentation =
                CreatePresentation(
                    SessionPresentationPath,
                    "QA CAMERA-032 Session Presentation",
                    outputDefinition,
                    presentationRig,
                    100);
            CameraPresentationDefinition routePresentation =
                CreatePresentation(
                    RoutePresentationPath,
                    "QA CAMERA-032 Route Presentation",
                    outputDefinition,
                    presentationRig,
                    200);
            CameraPresentationDefinition activityPresentation =
                CreatePresentation(
                    ActivityPresentationPath,
                    "QA CAMERA-032 Activity Presentation",
                    outputDefinition,
                    presentationRig,
                    300);

            ActivityAsset activityA =
                CreateActivity(
                    activityPresentation);
            RouteAsset routeA =
                CreateRoute(
                    RouteAPath,
                    "QA CAMERA-032 Game Flow Route A",
                    StartupScenePath,
                    StartupSceneName,
                    activityA,
                    routePresentation);
            RouteAsset routeB =
                CreateRoute(
                    RouteBPath,
                    "QA CAMERA-032 Game Flow Route B",
                    ReplacementScenePath,
                    ReplacementSceneName,
                    null);

            Require(routeA != null, "Generated Route A is missing.");
            Require(routeB != null, "Generated Route B is missing.");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void PreparePhase(
            bool expectSessionFallback)
        {
            ConfigureCanonicalGameApplication(
                expectSessionFallback);
            ConfigurePersistentScene(
                expectSessionFallback);

            AssetDatabase.SaveAssets();

            // Prove the authored graph and QA_UIGlobal fixture from fresh
            // AssetDatabase/scene loads before entering Play Mode. No generated
            // UnityEngine.Object wrapper is carried across OpenScene boundaries.
            VerifyGeneratedAssetGraph(
                expectSessionFallback);
            VerifyGeneratedRouteScenes();
            VerifyPersistedFixture(
                expectSessionFallback);

            // Match Full Camera / CAMERA-028-D: leave the Hub open for the fresh
            // Framework bootstrap.
            EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);

            VerifyCanonicalGameApplicationConfiguration(
                expectSessionFallback);
        }

        private static GameObject CreateOutputPrefab(
            string path,
            CameraOutputDefinition definition,
            CameraRigBehaviorDefinition fixedBehavior)
        {
            var root =
                new GameObject(
                    "QA CAMERA-032 Game Flow Output");
            try
            {
                UnityEngine.Camera unityCamera =
                    root.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain =
                    root.AddComponent<CinemachineBrain>();
                CameraOutputAuthoring output =
                    root.AddComponent<CameraOutputAuthoring>();

                var rigRoot =
                    new GameObject("DefaultRig");
                rigRoot.transform.SetParent(
                    root.transform,
                    false);
                CameraRigComposer composer =
                    rigRoot.AddComponent<CameraRigComposer>();
                var cameraRoot =
                    new GameObject(
                        "Default Cinemachine Camera");
                cameraRoot.transform.SetParent(
                    rigRoot.transform,
                    false);
                CinemachineCamera cinemachine =
                    cameraRoot.AddComponent<CinemachineCamera>();

                ConfigureComposer(
                    composer,
                    fixedBehavior,
                    cinemachine,
                    "Output Default");

                CameraRigComposerApplyRebuildResult materialized =
                    CameraRigComposerApplyRebuildUtility
                        .ApplyOrRebuild(
                            composer,
                            false,
                            false);
                Require(
                    materialized.Succeeded,
                    "CAMERA-032 Game Flow Output Default Rig materialization failed. " +
                    materialized.BlockingIssue);

                composer.CinemachineCamera.enabled = false;

                SetReference(
                    output,
                    "outputDefinition",
                    definition);
                SetReference(
                    output,
                    "unityCamera",
                    unityCamera);
                SetReference(
                    output,
                    "cinemachineBrain",
                    brain);
                SetReference(
                    output,
                    "defaultCameraRig",
                    composer);
                SetBool(
                    output,
                    "initializeOnAwake",
                    true);
                SetBool(
                    output,
                    "logDiagnostics",
                    true);

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(
                        root,
                        path);
                Require(
                    prefab != null,
                    $"Could not save CAMERA-032 Game Flow Output prefab '{path}'.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreatePresentationRigPrefab(
            string path,
            CameraRigBehaviorDefinition fixedBehavior)
        {
            var root =
                new GameObject(
                    "PF_QA_Camera032_GameFlow_Presentation");
            try
            {
                CameraRigComposer composer =
                    root.AddComponent<CameraRigComposer>();
                var cameraRoot =
                    new GameObject("Cinemachine Camera");
                cameraRoot.transform.SetParent(
                    root.transform,
                    false);
                CinemachineCamera cinemachine =
                    cameraRoot.AddComponent<CinemachineCamera>();

                ConfigureComposer(
                    composer,
                    fixedBehavior,
                    cinemachine,
                    "Game Flow Presentation");

                CameraRigComposerApplyRebuildResult materialized =
                    CameraRigComposerApplyRebuildUtility
                        .ApplyOrRebuild(
                            composer,
                            false,
                            false);
                Require(
                    materialized.Succeeded,
                    "CAMERA-032 Game Flow Presentation Rig materialization failed. " +
                    materialized.BlockingIssue);

                composer.CinemachineCamera.enabled = false;

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(
                        root,
                        path);
                Require(
                    prefab != null,
                    $"Could not save CAMERA-032 Game Flow Presentation prefab '{path}'.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static CameraPresentationDefinition
            CreatePresentation(
                string path,
                string name,
                CameraOutputDefinition outputDefinition,
                GameObject rigPrefab,
                int precedence)
        {
            CameraPresentationDefinition definition =
                ScriptableObject.CreateInstance<
                    CameraPresentationDefinition>();
            definition.name = name;
            CameraDefinitionIdentityEditorUtility
                .GenerateMissingId(definition);

            var serialized =
                new SerializedObject(definition);
            serialized.FindProperty(
                    "outputDefinition")
                .objectReferenceValue =
                outputDefinition;
            serialized.FindProperty(
                    "rigPrefab")
                .objectReferenceValue =
                rigPrefab;
            serialized.FindProperty(
                    "transitionMode")
                .intValue =
                (int)CameraPresentationTransitionMode.Cut;
            serialized.FindProperty(
                    "subjectPolicy")
                .intValue =
                (int)CameraSharedCompositionSubjectPolicyKind
                    .AllAvailableSubjects;
            serialized.FindProperty(
                    "requestPrecedence")
                .intValue =
                precedence;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(
                definition,
                path);
            Require(
                definition.TryValidate(
                    out string issue),
                $"Generated CAMERA-032 Game Flow Presentation '{name}' is invalid. {issue}");
            return definition;
        }

        private static ActivityAsset CreateActivity(
            CameraPresentationDefinition presentation)
        {
            ActivityAsset activity =
                ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name =
                "QA_Camera032_GameFlow_ActivityA";

            var serialized =
                new SerializedObject(activity);
            serialized.FindProperty(
                    "activityId")
                .stringValue =
                Guid.NewGuid().ToString("N");
            serialized.FindProperty(
                    "activityName")
                .stringValue =
                "QA CAMERA-032 Game Flow Activity A";
            serialized.FindProperty(
                    "playerParticipationProjectionMode")
                .intValue =
                (int)ActivityParticipationProjectionMode.NoSlots;
            serialized.FindProperty(
                    "playerParticipationZeroParticipantPolicy")
                .intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Allowed;
            serialized.FindProperty(
                    "playerParticipationRequirementLevel")
                .intValue =
                (int)PlayerParticipationRequirementLevel.None;
            serialized.FindProperty(
                    "visualTransitionMode")
                .intValue =
                (int)ActivityVisualTransitionMode.Fade;
            serialized.FindProperty(
                    "transitionGateMode")
                .intValue =
                (int)Immersive.Framework.Transition.TransitionGateMode
                    .InputInteractionAndGameplay;
            SetObjectArray(
                serialized.FindProperty(
                    "cameraPresentations"),
                presentation);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(
                activity,
                ActivityAPath);
            return activity;
        }

        private static RouteAsset CreateRoute(
            string path,
            string routeName,
            string scenePath,
            string sceneName,
            ActivityAsset startupActivity,
            params CameraPresentationDefinition[] presentations)
        {
            RouteAsset route =
                ScriptableObject.CreateInstance<RouteAsset>();
            route.name =
                routeName.Replace(" ", string.Empty);

            var serialized =
                new SerializedObject(route);
            serialized.FindProperty(
                    "routeId")
                .stringValue =
                Guid.NewGuid().ToString("N");
            serialized.FindProperty(
                    "routeName")
                .stringValue =
                routeName;
            serialized.FindProperty(
                    "primaryScenePath")
                .stringValue =
                scenePath;
            serialized.FindProperty(
                    "primarySceneName")
                .stringValue =
                sceneName;
            serialized.FindProperty(
                    "startupActivity")
                .objectReferenceValue =
                startupActivity;
            serialized.FindProperty(
                    "playerSpatialEntryPolicy")
                .intValue =
                (int)RoutePlayerSpatialEntryPolicy
                    .PreserveCurrentPose;
            SetObjectArray(
                serialized.FindProperty(
                    "cameraPresentations"),
                presentations);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(
                route,
                path);
            return route;
        }

        private static void ConfigureCanonicalGameApplication(
            bool expectSessionFallback)
        {
            GameApplicationAsset application =
                RequireActiveCanonicalGameApplication();
            RouteAsset startupRoute =
                RequireAsset<RouteAsset>(
                    RouteAPath);
            GameObject outputPrefab =
                RequireAsset<GameObject>(
                    OutputPrefabPath);
            CameraPresentationDefinition sessionPresentation =
                expectSessionFallback
                    ? RequireAsset<CameraPresentationDefinition>(
                        SessionPresentationPath)
                    : null;

            var serialized =
                new SerializedObject(application);

            SerializedProperty startupRouteProperty =
                serialized.FindProperty("startupRoute");
            SerializedProperty playerSessionEnabled =
                serialized.FindProperty("playerSessionEnabled");
            SerializedProperty cameraSession =
                serialized.FindProperty("cameraSession");
            SerializedProperty sessionPresentations =
                serialized.FindProperty("sessionCameraPresentations");

            Require(
                startupRouteProperty != null &&
                playerSessionEnabled != null &&
                cameraSession != null &&
                sessionPresentations != null,
                "Canonical GameApplication does not expose the required CAMERA-032 authoring fields.");

            startupRouteProperty.objectReferenceValue =
                startupRoute;
            playerSessionEnabled.boolValue =
                false;

            SetObjectArray(
                cameraSession.FindPropertyRelative(
                    "outputPrefabs"),
                outputPrefab);

            SerializedProperty playerOutputBindings =
                cameraSession.FindPropertyRelative(
                    "playerOutputBindings");
            SerializedProperty playerPresentationBindings =
                cameraSession.FindPropertyRelative(
                    "playerPresentationBindings");

            Require(
                playerOutputBindings != null &&
                playerPresentationBindings != null,
                "Canonical Camera Session binding arrays are missing.");

            playerOutputBindings.arraySize = 0;
            playerPresentationBindings.arraySize = 0;

            SetObjectArray(
                sessionPresentations,
                sessionPresentation != null
                    ? new Object[] { sessionPresentation }
                    : Array.Empty<Object>());

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            AssetDatabase.SaveAssetIfDirty(application);

            Require(
                application.CameraSession.TryValidate(
                    out string issue),
                "Configured canonical CAMERA-032 Game Flow Camera Session is invalid. " +
                issue);
        }

        private static void ConfigurePersistentScene(
            bool expectSessionFallback)
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    PersistentScenePath,
                    OpenSceneMode.Single);

            // Load authored assets only after the scene boundary above. This
            // avoids retaining UnityEngine.Object wrappers across OpenScene.
            CameraOutputDefinition outputDefinition =
                RequireAsset<CameraOutputDefinition>(
                    OutputDefinitionPath);
            ActivityAsset activityA =
                RequireAsset<ActivityAsset>(
                    ActivityAPath);
            RouteAsset routeB =
                RequireAsset<RouteAsset>(
                    RouteBPath);

            RemoveLegacyCameraAuthority(scene);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null &&
                    root.name == RegressionRootName)
                {
                    Object.DestroyImmediate(root);
                }
            }

            var regressionRoot =
                new GameObject(RegressionRootName);
            SceneManager.MoveGameObjectToScene(
                regressionRoot,
                scene);

            RouteRequestTrigger routeTrigger =
                regressionRoot.AddComponent<
                    RouteRequestTrigger>();
            routeTrigger.TargetRoute =
                routeB;

            ActivityRequestTrigger activityTrigger =
                regressionRoot.AddComponent<
                    ActivityRequestTrigger>();
            activityTrigger.TargetActivity =
                activityA;

            QaCamera032GameFlowLifecycleRegression regression =
                regressionRoot.AddComponent<
                    QaCamera032GameFlowLifecycleRegression>();
            regression.Configure(
                outputDefinition,
                routeTrigger,
                activityTrigger,
                expectSessionFallback,
                StartupSceneName,
                ReplacementSceneName);

            EditorUtility.SetDirty(routeTrigger);
            EditorUtility.SetDirty(activityTrigger);
            EditorUtility.SetDirty(regression);
            EditorUtility.SetDirty(regressionRoot);
            EditorSceneManager.MarkSceneDirty(scene);

            Require(
                EditorSceneManager.SaveScene(
                    scene,
                    PersistentScenePath),
                "CAMERA-032 Game Flow QA persistent scene could not be saved.");
        }

        private static void RemoveLegacyCameraAuthority(
            Scene scene)
        {
            var destroy =
                new HashSet<GameObject>();
            CollectRoots<CameraOutputAuthoring>(
                scene,
                destroy);
            CollectRoots<QaCamera032PlayerOutputLifecycleRegression>(
                scene,
                destroy);

            foreach (GameObject value in destroy)
            {
                if (value != null)
                {
                    Object.DestroyImmediate(value);
                }
            }
        }

        private static void RecoverBackupBeforeNewRun()
        {
            EnsureGeneratedSceneIsNotOpen();
            RemoveGeneratedBuildScenes();

            GameApplicationAsset backup =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    BackupGameApplicationPath);

            bool restoredBackup = backup != null;
            if (restoredBackup)
            {
                RestoreCanonicalGameApplicationFromBackup();
            }

            AssetDatabase.DeleteAsset(
                BackupRoot);
            AssetDatabase.DeleteAsset(
                GeneratedRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (restoredBackup)
            {
                Debug.Log(
                    $"{Prefix} setup='RecoveredInterruptedBackup' " +
                    $"application='{CanonicalGameApplicationPath}'.");
            }
        }

        private static void RecoverInterruptedRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                (CurrentPhase != Phase.RunningSessionFallback &&
                 CurrentPhase != Phase.RunningDefaultFallback))
            {
                return;
            }

            StopWatching();

            try
            {
                RestoreCanonicalState();
                RecordFailure(
                    "interrupted-run-recovered",
                    "Editor/domain reload found an unfinished CAMERA-032 Game Flow certification and restored the canonical QA baseline.");
                Debug.LogWarning(
                    $"{Prefix} status='RecoveredInterruptedRun' cleanup='CanonicalSharedRestored'.");
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "interrupted-run-recovery-failed",
                    exception.GetBaseException().Message);
                Debug.LogError(
                    $"{Prefix} status='RecoveryFailed' diagnostic='{Escape(exception.GetBaseException().Message)}'.");
            }
        }

        private static void RestoreCanonicalGameApplicationFromBackup()
        {
            GameApplicationAsset canonical =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    CanonicalGameApplicationPath);
            GameApplicationAsset backup =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    BackupGameApplicationPath);

            Require(
                canonical != null &&
                backup != null,
                "CAMERA-032 Game Flow canonical GameApplication backup is unavailable.");

            EditorUtility.CopySerialized(
                backup,
                canonical);
            EditorUtility.SetDirty(canonical);
            AssetDatabase.SaveAssetIfDirty(canonical);
        }

        private static void BackupCanonicalGameApplication()
        {
            EnsureFolder(BackupRoot);

            Require(
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    BackupGameApplicationPath) == null,
                $"CAMERA-032 Game Flow backup already exists at '{BackupGameApplicationPath}'.");

            Require(
                AssetDatabase.CopyAsset(
                    CanonicalGameApplicationPath,
                    BackupGameApplicationPath),
                "Could not snapshot the canonical GameApplication before CAMERA-032 Game Flow mutation.");

            AssetDatabase.SaveAssets();

            GameApplicationAsset backup =
                RequireAsset<GameApplicationAsset>(
                    BackupGameApplicationPath);
            Require(
                backup != null,
                "Canonical GameApplication backup could not be reloaded from disk.");
        }

        private static GameApplicationAsset
            RequireActiveCanonicalGameApplication()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);

            Require(
                settings != null,
                "CAMERA-032 Game Flow requires the canonical Immersive Framework settings Resources asset.");

            GameApplicationAsset application =
                settings.ActiveGameApplication;

            Require(
                application != null,
                "CAMERA-032 Game Flow requires the active canonical GameApplication.");

            Require(
                string.Equals(
                    AssetDatabase.GetAssetPath(application),
                    CanonicalGameApplicationPath,
                    StringComparison.Ordinal),
                $"CAMERA-032 Game Flow must mutate the canonical active GameApplication in place. actual='{AssetDatabase.GetAssetPath(application)}' expected='{CanonicalGameApplicationPath}'.");

            return application;
        }

        private static void VerifyGeneratedAssetGraph(
            bool expectSessionFallback)
        {
            CameraOutputDefinition outputDefinition =
                RequireAsset<CameraOutputDefinition>(
                    OutputDefinitionPath);
            GameObject outputPrefab =
                RequireAsset<GameObject>(
                    OutputPrefabPath);
            GameObject presentationRig =
                RequireAsset<GameObject>(
                    PresentationRigPath);
            CameraPresentationDefinition sessionPresentation =
                RequireAsset<CameraPresentationDefinition>(
                    SessionPresentationPath);
            CameraPresentationDefinition routePresentation =
                RequireAsset<CameraPresentationDefinition>(
                    RoutePresentationPath);
            CameraPresentationDefinition activityPresentation =
                RequireAsset<CameraPresentationDefinition>(
                    ActivityPresentationPath);
            ActivityAsset activityA =
                RequireAsset<ActivityAsset>(
                    ActivityAPath);
            RouteAsset routeA =
                RequireAsset<RouteAsset>(
                    RouteAPath);
            RouteAsset routeB =
                RequireAsset<RouteAsset>(
                    RouteBPath);

            VerifyPresentation(
                sessionPresentation,
                outputDefinition,
                presentationRig,
                100,
                "Session");
            VerifyPresentation(
                routePresentation,
                outputDefinition,
                presentationRig,
                200,
                "Route");
            VerifyPresentation(
                activityPresentation,
                outputDefinition,
                presentationRig,
                300,
                "Activity");

            Require(
                routeA.HasPrimaryScene &&
                string.Equals(
                    routeA.PrimaryScenePath,
                    StartupScenePath,
                    StringComparison.Ordinal) &&
                string.Equals(
                    routeA.PrimarySceneName,
                    StartupSceneName,
                    StringComparison.Ordinal),
                "Generated Route A primary scene authoring diverged.");

            RequireSameAsset(
                routeA.StartupActivity,
                activityA,
                "Generated Route A startup Activity");

            Require(
                routeA.CameraPresentations.Count == 1,
                $"Generated Route A Camera Presentation count diverged. actual='{routeA.CameraPresentations.Count}' expected='1'.");

            RequireSameAsset(
                routeA.CameraPresentations[0],
                routePresentation,
                "Generated Route A Camera Presentation");

            Require(
                routeB.HasPrimaryScene &&
                string.Equals(
                    routeB.PrimaryScenePath,
                    ReplacementScenePath,
                    StringComparison.Ordinal) &&
                string.Equals(
                    routeB.PrimarySceneName,
                    ReplacementSceneName,
                    StringComparison.Ordinal),
                "Generated Route B primary scene authoring diverged.");

            Require(
                !routeB.HasStartupActivity &&
                routeB.CameraPresentations.Count == 0,
                "Generated Route B must have no startup Activity or Route Camera Presentation.");

            Require(
                activityA.PlayerParticipationProjectionMode ==
                    ActivityParticipationProjectionMode.NoSlots &&
                activityA.PlayerParticipationZeroParticipantPolicy ==
                    ActivityParticipationZeroParticipantPolicy.Allowed &&
                activityA.PlayerParticipationRequirementLevel ==
                    PlayerParticipationRequirementLevel.None &&
                activityA.VisualTransitionMode ==
                    ActivityVisualTransitionMode.Fade,
                "Generated Activity A participation/transition authoring diverged.");

            Require(
                activityA.CameraPresentations.Count == 1,
                $"Generated Activity A Camera Presentation count diverged. actual='{activityA.CameraPresentations.Count}' expected='1'.");

            RequireSameAsset(
                activityA.CameraPresentations[0],
                activityPresentation,
                "Generated Activity A Camera Presentation");

            CameraOutputAuthoring[] outputComponents =
                outputPrefab.GetComponentsInChildren<CameraOutputAuthoring>(
                    true);
            Require(
                outputComponents.Length == 1 &&
                outputComponents[0] != null &&
                outputComponents[0].OutputDefinition != null &&
                outputComponents[0].OutputDefinition.OutputId ==
                    outputDefinition.OutputId &&
                outputComponents[0].UnityCamera != null &&
                outputComponents[0].CinemachineBrain != null &&
                outputComponents[0].DefaultCameraRig != null,
                "Generated Session Output prefab is incomplete or targets the wrong Output definition.");

            VerifyCanonicalGameApplicationConfiguration(
                expectSessionFallback);

            Debug.Log(
                $"{Prefix} setup='GeneratedAssetGraphVerified' " +
                $"sessionFallback='{expectSessionFallback}' " +
                $"routeA='{RouteAPath}' routeB='{RouteBPath}'.");
        }

        private static void VerifyPresentation(
            CameraPresentationDefinition presentation,
            CameraOutputDefinition expectedOutput,
            GameObject expectedRigPrefab,
            int expectedPrecedence,
            string label)
        {
            string issue = string.Empty;
            bool valid =
                presentation != null &&
                presentation.TryValidate(
                    out issue);

            Require(
                valid,
                $"Generated {label} Camera Presentation is invalid. {issue}");

            RequireSameAsset(
                presentation.OutputDefinition,
                expectedOutput,
                $"Generated {label} Camera Presentation Output");

            RequireSameAsset(
                presentation.RigPrefab,
                expectedRigPrefab,
                $"Generated {label} Camera Presentation Rig");

            Require(
                presentation.RequestPrecedence ==
                    expectedPrecedence &&
                presentation.SubjectPolicy ==
                    CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects &&
                presentation.TransitionMode ==
                    CameraPresentationTransitionMode.Cut,
                $"Generated {label} Camera Presentation policy diverged.");
        }

        private static void VerifyPersistedFixture(
            bool expectSessionFallback)
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    PersistentScenePath,
                    OpenSceneMode.Single);

            Require(
                FindInScene<CameraOutputAuthoring>(scene).Count == 0,
                "CAMERA-032 Game Flow fixture retained historical scene-owned Camera Output authority.");

            GameObject regressionRoot =
                RequireRegressionRoot(scene);

            QaCamera032GameFlowLifecycleRegression regression =
                RequireSingleOnRoot<
                    QaCamera032GameFlowLifecycleRegression>(
                    regressionRoot,
                    "CAMERA-032 Game Flow regression");
            RouteRequestTrigger routeTrigger =
                RequireSingleOnRoot<RouteRequestTrigger>(
                    regressionRoot,
                    "CAMERA-032 Route Request Trigger");
            ActivityRequestTrigger activityTrigger =
                RequireSingleOnRoot<ActivityRequestTrigger>(
                    regressionRoot,
                    "CAMERA-032 Activity Request Trigger");

            RequireSameAsset(
                routeTrigger.TargetRoute,
                RequireAsset<RouteAsset>(
                    RouteBPath),
                "Persisted Route Request target");

            RequireSameAsset(
                activityTrigger.TargetActivity,
                RequireAsset<ActivityAsset>(
                    ActivityAPath),
                "Persisted Activity Request target");

            var serialized =
                new SerializedObject(regression);
            SerializedProperty outputDefinition =
                serialized.FindProperty(
                    "outputDefinition");
            SerializedProperty sessionFallback =
                serialized.FindProperty(
                    "expectSessionFallback");
            SerializedProperty outgoingScene =
                serialized.FindProperty(
                    "outgoingSceneName");
            SerializedProperty replacementScene =
                serialized.FindProperty(
                    "replacementSceneName");

            Require(
                outputDefinition != null &&
                sessionFallback != null &&
                outgoingScene != null &&
                replacementScene != null,
                "Persisted CAMERA-032 regression serialized fixture is incomplete.");

            RequireSameAsset(
                outputDefinition.objectReferenceValue,
                RequireAsset<CameraOutputDefinition>(
                    OutputDefinitionPath),
                "Persisted regression Output definition");

            Require(
                sessionFallback.boolValue ==
                    expectSessionFallback &&
                string.Equals(
                    outgoingScene.stringValue,
                    StartupSceneName,
                    StringComparison.Ordinal) &&
                string.Equals(
                    replacementScene.stringValue,
                    ReplacementSceneName,
                    StringComparison.Ordinal),
                "Persisted CAMERA-032 regression phase/scene configuration diverged.");

            Debug.Log(
                $"{Prefix} setup='PersistentFixtureVerified' " +
                $"sessionFallback='{expectSessionFallback}' " +
                $"scene='{PersistentScenePath}'.");
        }

        private static void VerifyCanonicalGameApplicationConfiguration(
            bool expectSessionFallback)
        {
            AssetDatabase.SaveAssets();

            GameApplicationAsset application =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    CanonicalGameApplicationPath);
            RouteAsset expectedRoute =
                RequireAsset<RouteAsset>(
                    RouteAPath);
            GameObject expectedOutputPrefab =
                RequireAsset<GameObject>(
                    OutputPrefabPath);
            CameraPresentationDefinition expectedSessionPresentation =
                expectSessionFallback
                    ? RequireAsset<CameraPresentationDefinition>(
                        SessionPresentationPath)
                    : null;

            Require(
                application != null,
                $"Canonical GameApplication could not be reloaded from '{CanonicalGameApplicationPath}'.");

            GameApplicationAsset active =
                RequireActiveCanonicalGameApplication();
            RequireSameAsset(
                active,
                application,
                "Active GameApplication");

            var serialized =
                new SerializedObject(application);

            SerializedProperty startupRoute =
                serialized.FindProperty("startupRoute");
            SerializedProperty playerSessionEnabled =
                serialized.FindProperty("playerSessionEnabled");
            SerializedProperty cameraSession =
                serialized.FindProperty("cameraSession");
            SerializedProperty sessionPresentations =
                serialized.FindProperty("sessionCameraPresentations");

            Require(
                startupRoute != null &&
                playerSessionEnabled != null &&
                cameraSession != null &&
                sessionPresentations != null,
                "Persisted canonical GameApplication is missing CAMERA-032 authoring fields.");

            RequireSameAsset(
                startupRoute.objectReferenceValue,
                expectedRoute,
                "Persisted canonical GameApplication startup Route");

            Require(
                !playerSessionEnabled.boolValue,
                "CAMERA-032 Game Flow certification requires Player Session disabled.");

            SerializedProperty outputs =
                cameraSession.FindPropertyRelative(
                    "outputPrefabs");
            SerializedProperty playerOutputBindings =
                cameraSession.FindPropertyRelative(
                    "playerOutputBindings");
            SerializedProperty playerPresentationBindings =
                cameraSession.FindPropertyRelative(
                    "playerPresentationBindings");

            Require(
                outputs != null &&
                playerOutputBindings != null &&
                playerPresentationBindings != null,
                "Persisted canonical Camera Session arrays are missing.");

            Require(
                outputs.arraySize == 1,
                $"Persisted canonical Camera Session Output count diverged. actual='{outputs.arraySize}' expected='1'.");

            RequireSameAsset(
                outputs.GetArrayElementAtIndex(0)
                    .objectReferenceValue,
                expectedOutputPrefab,
                "Persisted canonical Camera Session Output prefab");

            Require(
                playerOutputBindings.arraySize == 0 &&
                playerPresentationBindings.arraySize == 0,
                "CAMERA-032 Game Flow certification requires zero Player Camera bindings.");

            int expectedSessionPresentationCount =
                expectedSessionPresentation != null ? 1 : 0;
            Require(
                sessionPresentations.arraySize ==
                    expectedSessionPresentationCount,
                $"Persisted Session Presentation count diverged. actual='{sessionPresentations.arraySize}' expected='{expectedSessionPresentationCount}'.");

            if (expectedSessionPresentation != null)
            {
                RequireSameAsset(
                    sessionPresentations
                        .GetArrayElementAtIndex(0)
                        .objectReferenceValue,
                    expectedSessionPresentation,
                    "Persisted Session Presentation");
            }

            Require(
                application.CameraSession.TryValidate(
                    out string cameraSessionIssue),
                "Persisted canonical CAMERA-032 Camera Session is invalid. " +
                cameraSessionIssue);

            Debug.Log(
                $"{Prefix} setup='CanonicalGameApplicationVerified' " +
                $"application='{CanonicalGameApplicationPath}' " +
                $"startupRoute='{RouteAPath}' " +
                $"output='{OutputPrefabPath}' " +
                $"sessionFallback='{expectSessionFallback}'.");
        }

        private static void RequireSameAsset(
            Object actual,
            Object expected,
            string label)
        {
            string actualPath =
                actual != null
                    ? AssetDatabase.GetAssetPath(actual)
                    : string.Empty;
            string expectedPath =
                expected != null
                    ? AssetDatabase.GetAssetPath(expected)
                    : string.Empty;

            Require(
                !string.IsNullOrWhiteSpace(actualPath) &&
                !string.IsNullOrWhiteSpace(expectedPath) &&
                string.Equals(
                    actualPath,
                    expectedPath,
                    StringComparison.Ordinal),
                $"{label} diverged. actual='{(string.IsNullOrWhiteSpace(actualPath) ? "<null>" : actualPath)}' expected='{(string.IsNullOrWhiteSpace(expectedPath) ? "<null>" : expectedPath)}'.");
        }

        private static List<T> FindInScene<T>(
            Scene scene)
            where T : Component
        {
            var results =
                new List<T>();

            foreach (GameObject root in
                scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                results.AddRange(
                    root.GetComponentsInChildren<T>(
                        true));
            }

            return results;
        }

        private static GameObject RequireRegressionRoot(
            Scene scene)
        {
            GameObject resolved = null;
            int matches = 0;

            foreach (GameObject root in
                scene.GetRootGameObjects())
            {
                if (root == null ||
                    !string.Equals(
                        root.name,
                        RegressionRootName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                resolved = root;
                matches++;
            }

            Require(
                matches == 1 &&
                resolved != null,
                $"CAMERA-032 Game Flow requires exactly one QA-owned regression root; found='{matches}'.");

            return resolved;
        }

        private static T RequireSingleOnRoot<T>(
            GameObject root,
            string label)
            where T : Component
        {
            T[] candidates =
                root != null
                    ? root.GetComponentsInChildren<T>(true)
                    : Array.Empty<T>();

            Require(
                candidates.Length == 1 &&
                candidates[0] != null,
                $"{label} requires exactly one QA-owned component; found='{candidates.Length}'.");

            return candidates[0];
        }

        private static void VerifyGeneratedRouteScenes()
        {
            RequireEnabledBuildScene(
                StartupScenePath);
            RequireEnabledBuildScene(
                ReplacementScenePath);

            VerifyGeneratedRouteScene(
                StartupScenePath,
                StartupSceneName);
            VerifyGeneratedRouteScene(
                ReplacementScenePath,
                ReplacementSceneName);
        }

        private static void VerifyGeneratedRouteScene(
            string path,
            string expectedName)
        {
            Scene scene =
                EditorSceneManager.OpenScene(
                    path,
                    OpenSceneMode.Single);

            Require(
                scene.IsValid() &&
                scene.isLoaded &&
                string.Equals(
                    scene.name,
                    expectedName,
                    StringComparison.Ordinal),
                $"Generated CAMERA-032 Route scene identity diverged. path='{path}' actual='{scene.name}' expected='{expectedName}'.");

            List<UnityEngine.Camera> cameras =
                FindInScene<UnityEngine.Camera>(
                    scene);
            List<AudioListener> listeners =
                FindInScene<AudioListener>(
                    scene);
            List<CameraOutputAuthoring> outputs =
                FindInScene<CameraOutputAuthoring>(
                    scene);
            Require(
                cameras.Count == 0 &&
                listeners.Count == 0 &&
                outputs.Count == 0,
                $"Generated CAMERA-032 Route scene must contain no Camera authority. path='{path}' cameras='{cameras.Count}' listeners='{listeners.Count}' outputs='{outputs.Count}'.");
        }

        private static void EnsureGeneratedSceneIsNotOpen()
        {
            Scene active =
                SceneManager.GetActiveScene();
            string activePath =
                active.IsValid()
                    ? active.path
                    : string.Empty;

            if (string.IsNullOrWhiteSpace(activePath) ||
                !activePath.StartsWith(
                    GeneratedRoot + "/",
                    StringComparison.Ordinal))
            {
                return;
            }

            EditorSceneManager.OpenScene(
                HubScenePath,
                OpenSceneMode.Single);
        }

        private static void CreateCameraFreeRouteScene(
            string path)
        {
            Scene scene =
                EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Single);

            Require(
                scene.IsValid(),
                $"Could not create generated CAMERA-032 Route scene '{path}'.");

            Require(
                EditorSceneManager.SaveScene(
                    scene,
                    path),
                $"Could not save generated CAMERA-032 Route scene '{path}'.");

            AssetDatabase.ImportAsset(
                path,
                ImportAssetOptions.ForceSynchronousImport);

            Require(
                !string.IsNullOrWhiteSpace(
                    AssetDatabase.AssetPathToGUID(path)),
                $"Generated CAMERA-032 Route scene was not imported as an AssetDatabase scene. path='{path}'.");

            List<UnityEngine.Camera> cameras =
                FindInScene<UnityEngine.Camera>(
                    scene);
            List<AudioListener> listeners =
                FindInScene<AudioListener>(
                    scene);

            Require(
                cameras.Count == 0 &&
                listeners.Count == 0,
                $"Generated CAMERA-032 Route scene must be camera-free. path='{path}' cameras='{cameras.Count}' listeners='{listeners.Count}'.");
        }

        private static void EnsureGeneratedBuildScenes()
        {
            var scenes =
                new List<EditorBuildSettingsScene>(
                    EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>());

            AddBuildSceneIfMissing(
                scenes,
                StartupScenePath);
            AddBuildSceneIfMissing(
                scenes,
                ReplacementScenePath);

            EditorBuildSettings.scenes =
                scenes.ToArray();
        }

        private static void AddBuildSceneIfMissing(
            IList<EditorBuildSettingsScene> scenes,
            string path)
        {
            for (int index = 0;
                 index < scenes.Count;
                 index++)
            {
                EditorBuildSettingsScene candidate =
                    scenes[index];
                if (candidate != null &&
                    string.Equals(
                        candidate.path,
                        path,
                        StringComparison.Ordinal))
                {
                    if (!candidate.enabled)
                    {
                        scenes[index] =
                            new EditorBuildSettingsScene(
                                path,
                                true);
                    }

                    return;
                }
            }

            scenes.Add(
                new EditorBuildSettingsScene(
                    path,
                    true));
        }

        private static void RemoveGeneratedBuildScenes()
        {
            EditorBuildSettingsScene[] current =
                EditorBuildSettings.scenes ??
                Array.Empty<EditorBuildSettingsScene>();
            var retained =
                new List<EditorBuildSettingsScene>(
                    current.Length);

            for (int index = 0;
                 index < current.Length;
                 index++)
            {
                EditorBuildSettingsScene candidate =
                    current[index];
                if (candidate == null)
                {
                    retained.Add(candidate);
                    continue;
                }

                if (string.Equals(
                        candidate.path,
                        StartupScenePath,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        candidate.path,
                        ReplacementScenePath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                retained.Add(candidate);
            }

            if (retained.Count != current.Length)
            {
                EditorBuildSettings.scenes =
                    retained.ToArray();
            }
        }

        private static void RequireEnabledBuildScene(
            string scenePath)
        {
            bool enabled = false;
            EditorBuildSettingsScene[] scenes =
                EditorBuildSettings.scenes;

            for (int index = 0;
                 index < scenes.Length;
                 index++)
            {
                EditorBuildSettingsScene candidate =
                    scenes[index];
                if (candidate != null &&
                    candidate.enabled &&
                    string.Equals(
                        candidate.path,
                        scenePath,
                        StringComparison.Ordinal))
                {
                    enabled = true;
                    break;
                }
            }

            Require(
                enabled,
                $"CAMERA-032 Game Flow requires enabled Build Settings scene '{scenePath}'.");
        }

        private static void RestoreCanonicalState()
        {
            GameApplicationAsset canonical =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    CanonicalGameApplicationPath);
            GameApplicationAsset backup =
                AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(
                    BackupGameApplicationPath);

            if (canonical != null &&
                backup != null)
            {
                RestoreCanonicalGameApplicationFromBackup();
            }

            // Reuse the same canonical restore rail as Full Camera and
            // CAMERA-028-D. It repairs the persistent Shared topology,
            // canonical Player boot profile, verifies the disk state and
            // leaves QA_Hub open.
            QaCameraPersistentBaselineGuard
                .RestoreCanonicalBaseline();

            RemoveGeneratedBuildScenes();

            AssetDatabase.DeleteAsset(
                GeneratedRoot);
            AssetDatabase.DeleteAsset(
                BackupRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state ==
                    PlayModeStateChange.EnteredPlayMode &&
                CurrentPhase is
                    Phase.RunningSessionFallback or
                    Phase.RunningDefaultFallback)
            {
                BeginWatching();
                return;
            }

            if (state !=
                PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            StopWatching();

            if (CurrentPhase ==
                Phase.SessionFallbackPassed)
            {
                EditorApplication.delayCall -=
                    BeginDefaultPhase;
                EditorApplication.delayCall +=
                    BeginDefaultPhase;
                return;
            }

            if (CurrentPhase ==
                Phase.DefaultFallbackPassed)
            {
                EditorApplication.delayCall -=
                    FinishSuccess;
                EditorApplication.delayCall +=
                    FinishSuccess;
                return;
            }

            if (CurrentPhase == Phase.Failed)
            {
                EditorApplication.delayCall -=
                    FinishFailure;
                EditorApplication.delayCall +=
                    FinishFailure;
                return;
            }

            if (CurrentPhase is
                Phase.RunningSessionFallback or
                Phase.RunningDefaultFallback)
            {
                RecordFailure(
                    "play-mode-interrupted",
                    "Play Mode exited before CAMERA-032 Game Flow lifecycle evidence completed.");
                EditorApplication.delayCall -=
                    FinishFailure;
                EditorApplication.delayCall +=
                    FinishFailure;
            }
        }

        private static void BeginDefaultPhase()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                CurrentPhase !=
                    Phase.SessionFallbackPassed)
            {
                return;
            }

            try
            {
                PreparePhase(false);
                SetPhase(
                    Phase.RunningDefaultFallback);
                Debug.Log(
                    $"{Prefix} status='Running' phase='DefaultFallback' " +
                    "proof='Activity->Route; Route->Default; force-default continuity; Route replacement'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "prepare-default",
                    exception.GetBaseException().Message);
                FinishFailure();
            }
        }

        private static void BeginWatching()
        {
            StopWatching();
            watching = true;
            startedAt =
                EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!watching ||
                !EditorApplication.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup -
                    startedAt >
                TimeoutSeconds)
            {
                RecordFailure(
                    "runtime-timeout",
                    $"Runtime regression did not reach terminal evidence within '{TimeoutSeconds}' seconds.");
                StopWatching();
                EditorApplication.isPlaying = false;
                return;
            }

            if (!QaCamera032GameFlowLifecycleRegression
                    .Executed)
            {
                return;
            }

            if (!QaCamera032GameFlowLifecycleRegression
                    .Passed)
            {
                RecordFailure(
                    "runtime",
                    QaCamera032GameFlowLifecycleRegression
                        .Diagnostic);
                StopWatching();
                EditorApplication.isPlaying = false;
                return;
            }

            int cases =
                QaCamera032GameFlowLifecycleRegression
                    .CompletedCaseCount;
            if (cases !=
                QaCamera032GameFlowLifecycleRegression
                    .ExpectedCaseCount)
            {
                RecordFailure(
                    "case-count",
                    $"CAMERA-032 Game Flow case count diverged. actual='{cases}' expected='{QaCamera032GameFlowLifecycleRegression.ExpectedCaseCount}'.");
                StopWatching();
                EditorApplication.isPlaying = false;
                return;
            }

            if (CurrentPhase ==
                Phase.RunningSessionFallback)
            {
                SessionState.SetInt(
                    SessionCaseCountKey,
                    cases);
                SetPhase(
                    Phase.SessionFallbackPassed);
            }
            else if (CurrentPhase ==
                Phase.RunningDefaultFallback)
            {
                SessionState.SetInt(
                    DefaultCaseCountKey,
                    cases);
                SetPhase(
                    Phase.DefaultFallbackPassed);
            }
            else
            {
                RecordFailure(
                    "phase",
                    $"Unexpected certification phase '{CurrentPhase}' after runtime PASS.");
            }

            StopWatching();
            EditorApplication.isPlaying = false;
        }

        private static void FinishSuccess()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                CurrentPhase !=
                    Phase.DefaultFallbackPassed)
            {
                return;
            }

            try
            {
                RestoreCanonicalState();

                int sessionCases =
                    SessionState.GetInt(
                        SessionCaseCountKey,
                        0);
                int defaultCases =
                    SessionState.GetInt(
                        DefaultCaseCountKey,
                        0);
                int expected =
                    QaCamera032GameFlowLifecycleRegression
                        .ExpectedCaseCount;

                Require(
                    sessionCases == expected,
                    $"Session fallback case count diverged. actual='{sessionCases}' expected='{expected}'.");
                Require(
                    defaultCases == expected,
                    $"Default fallback case count diverged. actual='{defaultCases}' expected='{expected}'.");

                SetPhase(Phase.Completed);
                SessionState.SetBool(
                    AggregateResultKey,
                    true);
                Debug.Log(
                    $"{Prefix} status='Passed' " +
                    "verdict='CAMERA_032_GAMEFLOW_LIFECYCLE_CERTIFIED' " +
                    $"sessionCases='{sessionCases}/{expected}' " +
                    $"defaultCases='{defaultCases}/{expected}' " +
                    $"totalCases='{sessionCases + defaultCases}/{expected * 2}' " +
                    "canonicalRestore='PASS'.");
            }
            catch (Exception exception)
            {
                RecordFailure(
                    "finish-success",
                    exception.GetBaseException().Message);
                FinishFailure();
            }
        }

        private static void FinishFailure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            string failure =
                SessionState.GetString(
                    FailureKey,
                    "unknown");
            string cleanup =
                "NotAttempted";

            try
            {
                RestoreCanonicalState();
                cleanup =
                    "CanonicalSharedRestored";
            }
            catch (Exception exception)
            {
                cleanup =
                    "RestoreFailed:" +
                    exception.GetBaseException()
                        .Message;
            }

            SetPhase(Phase.Failed);
            SessionState.SetBool(
                AggregateResultKey,
                false);
            Debug.LogError(
                $"{Prefix} status='Failed' " +
                "verdict='CAMERA_032_GAMEFLOW_LIFECYCLE_FAIL' " +
                $"diagnostic='{Escape(failure)}' " +
                $"cleanup='{Escape(cleanup)}'.");
        }

        private static void RecordFailure(
            string stage,
            string reason)
        {
            SessionState.SetString(
                FailureKey,
                $"{stage}: {reason}");
            SetPhase(Phase.Failed);
        }

        private static void ConfigureComposer(
            CameraRigComposer composer,
            CameraRigBehaviorDefinition behavior,
            CinemachineCamera cinemachine,
            string label)
        {
            var serialized =
                new SerializedObject(composer);
            serialized.FindProperty(
                    "behaviorDefinition")
                .objectReferenceValue =
                behavior;
            serialized.FindProperty(
                    "cinemachineCamera")
                .objectReferenceValue =
                cinemachine;
            serialized.FindProperty(
                    "logApplyRebuildDiagnostics")
                .boolValue =
                false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(
                ReferenceEquals(
                    composer.BehaviorDefinition,
                    behavior),
                $"{label} did not receive its exact Camera Rig Behavior definition.");
        }

        private static void SetObjectArray(
            SerializedProperty array,
            params Object[] values)
        {
            Require(
                array != null,
                "Expected serialized array was not found.");

            array.arraySize =
                values?.Length ?? 0;

            for (int index = 0;
                 values != null &&
                 index < values.Length;
                 index++)
            {
                array.GetArrayElementAtIndex(index)
                    .objectReferenceValue =
                    values[index];
            }
        }

        private static void SetReference(
            Object target,
            string property,
            Object value)
        {
            var serialized =
                new SerializedObject(target);
            SerializedProperty field =
                serialized.FindProperty(property);

            Require(
                field != null,
                $"Serialized property '{property}' is missing on '{target.name}'.");

            field.objectReferenceValue =
                value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(
            Object target,
            string property,
            bool value)
        {
            var serialized =
                new SerializedObject(target);
            SerializedProperty field =
                serialized.FindProperty(property);

            Require(
                field != null,
                $"Serialized property '{property}' is missing on '{target.name}'.");

            field.boolValue =
                value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CollectRoots<T>(
            Scene scene,
            ISet<GameObject> roots)
            where T : Component
        {
            foreach (GameObject root in
                scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                T[] components =
                    root.GetComponentsInChildren<T>(true);
                if (components.Length > 0)
                {
                    roots.Add(root);
                }
            }
        }

        private static T RequireAsset<T>(
            string path)
            where T : Object
        {
            T value =
                AssetDatabase.LoadAssetAtPath<T>(
                    path);
            Require(
                value != null,
                $"Required QA asset is missing at '{path}'.");
            return value;
        }

        private static void EnsureFolder(
            string folderPath)
        {
            string[] segments =
                folderPath.Split('/');
            string current =
                segments[0];

            for (int index = 1;
                 index < segments.Length;
                 index++)
            {
                string next =
                    $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }

                current = next;
            }
        }

        private static void StopWatching()
        {
            EditorApplication.update -= Tick;
            watching = false;
            startedAt = 0d;
        }

        private static Phase CurrentPhase =>
            (Phase)SessionState.GetInt(
                PhaseKey,
                (int)Phase.Idle);

        private static void SetPhase(
            Phase phase) =>
            SessionState.SetInt(
                PhaseKey,
                (int)phase);

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static string Escape(
            string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
    }
}

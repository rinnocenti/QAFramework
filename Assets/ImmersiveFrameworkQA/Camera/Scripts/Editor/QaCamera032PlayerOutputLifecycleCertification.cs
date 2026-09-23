using System;
using System.Collections.Generic;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Focused IF-ADR-032 certification for two Player-bound Session Outputs.
    /// It authors a temporary current GameApplication Camera Session and restores
    /// the historical QA Camera baseline after the run.
    /// </summary>
    internal static class QaCamera032PlayerOutputLifecycleCertification
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run CAMERA-032 Player Output Lifecycle Certification";
        private const string Prefix =
            "[CAMERA-032-PLAYER-OUTPUT-CERTIFICATION]";
        private const string PhaseKey =
            "ImmersiveFrameworkQA.CAMERA_032_PlayerOutput.Phase";
        private const string FailureKey =
            "ImmersiveFrameworkQA.CAMERA_032_PlayerOutput.Failure";
        private const string CaseCountKey =
            "ImmersiveFrameworkQA.CAMERA_032_PlayerOutput.Cases";
        private const string AggregateResultKey =
            "ImmersiveFrameworkQA.CAMERA_032_PlayerOutput.AggregatePassed";
        private const double TimeoutSeconds = 360d;

        private const string GeneratedRoot =
            "Assets/ImmersiveFrameworkQA/Camera/Generated032PlayerOutput";
        private const string CanonicalGameApplicationPath =
            "Assets/ImmersiveFrameworkQA/GameApplications/GameApplication.asset";
        private const string GeneratedGameApplicationPath =
            GeneratedRoot + "/QA_Camera032_GameApplication.asset";
        private const string GeneratedRoutePath =
            GeneratedRoot + "/QA_Camera032_Route.asset";
        private const string GeneratedActivityPath =
            GeneratedRoot + "/QA_Camera032_Activity.asset";
        private const string ThirdPersonBehaviorPath =
            GeneratedRoot + "/QA_Camera032_ThirdPersonBehavior.asset";
        private const string ThirdPersonRigPath =
            GeneratedRoot + "/PF_QA_Camera032_ThirdPerson.prefab";
        private const string OutputP1PrefabPath =
            GeneratedRoot + "/PF_QA_Camera032_Output_P1.prefab";
        private const string OutputP2PrefabPath =
            GeneratedRoot + "/PF_QA_Camera032_Output_P2.prefab";
        private const string PresentationP1Path =
            GeneratedRoot + "/CameraPresentation_QA_Camera032_P1.asset";
        private const string PresentationP2Path =
            GeneratedRoot + "/CameraPresentation_QA_Camera032_P2.asset";

        private const string SettingsPath =
            "Assets/ImmersiveFrameworkQA/SmokeData/Resources/ImmersiveFrameworkSettings.asset";
        private const string PersistentScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";
        private const string PlayerScenePath =
            "Assets/ImmersiveFrameworkQA/Player/Scenes/QA_Player.unity";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string SlotP1Path =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/QA_PlayerSlot_P1.asset";
        private const string SlotP2Path =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/QA_PlayerSlot_P2.asset";
        private const string OutputP1DefinitionPath =
            "Assets/ImmersiveFrameworkQA/Camera/Definitions/MainOutput.asset";
        private const string OutputP2DefinitionPath =
            "Assets/ImmersiveFrameworkQA/Camera/Definitions/SecondaryOutput.asset";
        private const string FixedBehaviorPath =
            "Assets/ImmersiveFrameworkQA/Camera/Definitions/FixedBehavior.asset";
        private const string RegressionRootName =
            "[QA CAMERA-032] Player Output Lifecycle";

        private static double startedAt;
        private static bool watching;

        private enum Phase
        {
            Idle = 0,
            Running = 10,
            RuntimePassed = 20,
            Failed = 30,
            Completed = 40
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode &&
            CurrentPhase is Phase.Idle or Phase.Failed or Phase.Completed;

        [MenuItem(MenuPath, priority = 235)]
        private static void Run()
        {
            SessionState.SetBool(
                AggregateResultKey,
                false);
            StopWatching();
            SessionState.EraseString(FailureKey);
            SessionState.EraseInt(CaseCountKey);

            try
            {
                Prepare();
                SetPhase(Phase.Running);
                Debug.Log(
                    $"{Prefix} status='Running' " +
                    "topology='GameApplicationSessionOutputs' " +
                    "cycle='0->1->2->1->2->0' " +
                    "presentations='ExplicitSelectionThirdPerson'.");
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception)
            {
                RecordFailure("prepare", exception.GetBaseException().Message);
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

        private static void Prepare()
        {
            // Reusable Camera definitions are shared QA assets. Physical Output
            // and Presentation topology is authored only by this 032 fixture.
            QaCameraDefinitionAssetGuard.Ensure();

            AssetDatabase.DeleteAsset(GeneratedRoot);
            EnsureFolder(GeneratedRoot);

            CameraOutputDefinition outputP1 =
                QaCameraDefinitionAssetGuard
                    .RequireDefinition<CameraOutputDefinition>(OutputP1DefinitionPath);
            CameraOutputDefinition outputP2 =
                QaCameraDefinitionAssetGuard
                    .RequireDefinition<CameraOutputDefinition>(OutputP2DefinitionPath);
            FixedCameraRigBehaviorDefinition fixedBehavior =
                QaCameraDefinitionAssetGuard
                    .RequireBehavior<FixedCameraRigBehaviorDefinition>(FixedBehaviorPath);
            PlayerSlotProfile slotP1 = RequireAsset<PlayerSlotProfile>(SlotP1Path);
            PlayerSlotProfile slotP2 = RequireAsset<PlayerSlotProfile>(SlotP2Path);

            ThirdPersonCameraRigBehaviorDefinition thirdPersonBehavior =
                ScriptableObject.CreateInstance<ThirdPersonCameraRigBehaviorDefinition>();
            thirdPersonBehavior.name = "QA CAMERA-032 ThirdPerson";
            AssetDatabase.CreateAsset(thirdPersonBehavior, ThirdPersonBehaviorPath);

            GameObject outputPrefabP1 =
                CreateOutputPrefab(
                    OutputP1PrefabPath,
                    "QA CAMERA-032 Output P1",
                    outputP1,
                    fixedBehavior);
            GameObject outputPrefabP2 =
                CreateOutputPrefab(
                    OutputP2PrefabPath,
                    "QA CAMERA-032 Output P2",
                    outputP2,
                    fixedBehavior);
            GameObject thirdPersonRig =
                CreatePresentationRigPrefab(ThirdPersonRigPath, thirdPersonBehavior);

            CameraPresentationDefinition presentationP1 =
                CreatePresentation(
                    PresentationP1Path,
                    "QA CAMERA-032 P1 ThirdPerson",
                    outputP1,
                    thirdPersonRig);
            CameraPresentationDefinition presentationP2 =
                CreatePresentation(
                    PresentationP2Path,
                    "QA CAMERA-032 P2 ThirdPerson",
                    outputP2,
                    thirdPersonRig);

            ActivityAsset activity = CreateActivity(presentationP1, presentationP2);
            RouteAsset route = CreateRoute(activity);
            GameApplicationAsset application =
                CreateGameApplication(
                    route,
                    outputPrefabP1,
                    outputPrefabP2,
                    slotP1,
                    slotP2,
                    outputP1,
                    outputP2,
                    presentationP1,
                    presentationP2);

            ConfigurePersistentScene(slotP1, slotP2, outputP1, outputP2);
            SetActiveGameApplication(application);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
        }

        private static GameObject CreateOutputPrefab(
            string path,
            string name,
            CameraOutputDefinition definition,
            CameraRigBehaviorDefinition fixedBehavior)
        {
            var root = new GameObject(name);
            try
            {
                UnityEngine.Camera unityCamera = root.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
                CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();

                var rigRoot = new GameObject("DefaultRig");
                rigRoot.transform.SetParent(root.transform, false);
                CameraRigComposer composer = rigRoot.AddComponent<CameraRigComposer>();
                var cameraRoot = new GameObject("Default Cinemachine Camera");
                cameraRoot.transform.SetParent(rigRoot.transform, false);
                CinemachineCamera cinemachine = cameraRoot.AddComponent<CinemachineCamera>();

                ConfigureComposer(composer, fixedBehavior, cinemachine, "Output Default");
                CameraRigComposerApplyRebuildResult materialized =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        composer, false, false);
                Require(
                    materialized.Succeeded,
                    "CAMERA-032 Output Default Rig materialization failed. " +
                    materialized.BlockingIssue);
                composer.CinemachineCamera.enabled = false;

                SetReference(output, "outputDefinition", definition);
                SetReference(output, "unityCamera", unityCamera);
                SetReference(output, "cinemachineBrain", brain);
                SetReference(output, "defaultCameraRig", composer);
                SetBool(output, "initializeOnAwake", true);
                SetBool(output, "logDiagnostics", true);

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Require(prefab != null, $"Could not save CAMERA-032 Output prefab '{path}'.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreatePresentationRigPrefab(
            string path,
            CameraRigBehaviorDefinition behavior)
        {
            var root = new GameObject("PF_QA_Camera032_ThirdPerson");
            try
            {
                CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
                var cameraRoot = new GameObject("Cinemachine Camera");
                cameraRoot.transform.SetParent(root.transform, false);
                CinemachineCamera cinemachine = cameraRoot.AddComponent<CinemachineCamera>();

                ConfigureComposer(composer, behavior, cinemachine, "ThirdPerson Presentation");
                CameraRigComposerApplyRebuildResult materialized =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        composer, false, false);
                Require(
                    materialized.Succeeded,
                    "CAMERA-032 ThirdPerson Rig materialization failed. " +
                    materialized.BlockingIssue);
                composer.CinemachineCamera.enabled = false;

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Require(prefab != null, $"Could not save CAMERA-032 Presentation prefab '{path}'.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static CameraPresentationDefinition CreatePresentation(
            string path,
            string name,
            CameraOutputDefinition output,
            GameObject rigPrefab)
        {
            CameraPresentationDefinition definition =
                ScriptableObject.CreateInstance<CameraPresentationDefinition>();
            definition.name = name;
            CameraDefinitionIdentityEditorUtility.GenerateMissingId(definition);

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("outputDefinition").objectReferenceValue = output;
            serialized.FindProperty("rigPrefab").objectReferenceValue = rigPrefab;
            serialized.FindProperty("transitionMode").intValue =
                (int)CameraPresentationTransitionMode.Blend;
            serialized.FindProperty("subjectPolicy").intValue =
                (int)CameraSharedCompositionSubjectPolicyKind.ExplicitSelection;
            serialized.FindProperty("requestPrecedence").intValue = 300;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(definition, path);
            Require(
                definition.TryValidate(out string issue),
                $"Generated CAMERA-032 Presentation '{name}' is invalid. {issue}");
            return definition;
        }

        private static ActivityAsset CreateActivity(
            CameraPresentationDefinition p1,
            CameraPresentationDefinition p2)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = "QA_Camera032_PlayerOutputLifecycleActivity";
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = Guid.NewGuid().ToString("N");
            serialized.FindProperty("activityName").stringValue =
                "QA CAMERA-032 Player Output Lifecycle";
            serialized.FindProperty("playerParticipationProjectionMode").intValue =
                (int)ActivityParticipationProjectionMode.AllJoinedSlots;
            serialized.FindProperty("playerParticipationZeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Allowed;
            serialized.FindProperty("playerParticipationRequirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.GameplayReady;
            SetObjectArray(serialized.FindProperty("cameraPresentations"), p1, p2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(activity, GeneratedActivityPath);
            return activity;
        }

        private static RouteAsset CreateRoute(ActivityAsset activity)
        {
            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = "QA_Camera032_PlayerOutputLifecycleRoute";
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = Guid.NewGuid().ToString("N");
            serialized.FindProperty("routeName").stringValue =
                "QA CAMERA-032 Player Output Lifecycle";
            serialized.FindProperty("primaryScenePath").stringValue = PlayerScenePath;
            serialized.FindProperty("primarySceneName").stringValue = "QA_Player";
            serialized.FindProperty("startupActivity").objectReferenceValue = activity;
            serialized.FindProperty("playerSpatialEntryPolicy").intValue =
                (int)RoutePlayerSpatialEntryPolicy.PreserveCurrentPose;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(route, GeneratedRoutePath);
            return route;
        }

        private static GameApplicationAsset CreateGameApplication(
            RouteAsset route,
            GameObject outputPrefabP1,
            GameObject outputPrefabP2,
            PlayerSlotProfile slotP1,
            PlayerSlotProfile slotP2,
            CameraOutputDefinition outputP1,
            CameraOutputDefinition outputP2,
            CameraPresentationDefinition presentationP1,
            CameraPresentationDefinition presentationP2)
        {
            Require(
                AssetDatabase.CopyAsset(
                    CanonicalGameApplicationPath,
                    GeneratedGameApplicationPath),
                "Could not copy canonical GameApplication for CAMERA-032 certification.");
            GameApplicationAsset application =
                RequireAsset<GameApplicationAsset>(GeneratedGameApplicationPath);
            application.name = "QA_Camera032_GameApplication";

            var serialized = new SerializedObject(application);
            serialized.FindProperty("applicationName").stringValue =
                "QA CAMERA-032 Player Output Lifecycle";
            serialized.FindProperty("startupRoute").objectReferenceValue = route;

            SerializedProperty cameraSession = serialized.FindProperty("cameraSession");
            Require(cameraSession != null, "GameApplication cameraSession property is missing.");

            SetObjectArray(
                cameraSession.FindPropertyRelative("outputPrefabs"),
                outputPrefabP1,
                outputPrefabP2);

            SetBindingArray(
                cameraSession.FindPropertyRelative("playerOutputBindings"),
                new[]
                {
                    new BindingData(slotP1, outputP1),
                    new BindingData(slotP2, outputP2)
                });

            SetPresentationBindingArray(
                cameraSession.FindPropertyRelative("playerPresentationBindings"),
                new[]
                {
                    new PresentationBindingData(slotP1, presentationP1),
                    new PresentationBindingData(slotP2, presentationP2)
                });

            serialized.FindProperty("sessionCameraPresentations").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);

            Require(
                application.CameraSession.TryValidate(out string issue),
                "Generated CAMERA-032 Camera Session is invalid. " + issue);
            return application;
        }

        private static void ConfigurePersistentScene(
            PlayerSlotProfile slotP1,
            PlayerSlotProfile slotP2,
            CameraOutputDefinition outputP1,
            CameraOutputDefinition outputP2)
        {
            Scene scene = EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
            RemoveLegacyCameraAuthority(scene);

            PlayerInputManager manager = RequireSingle<PlayerInputManager>(scene, "PlayerInputManager");
            LocalPlayerProvisioningAuthoring provisioning =
                RequireSingle<LocalPlayerProvisioningAuthoring>(
                    scene, "LocalPlayerProvisioningAuthoring");
            Require(
                ReferenceEquals(provisioning.PlayerInputManager, manager),
                "CAMERA-032 QA provisioning does not reference the exact PlayerInputManager.");

            var managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("m_SplitScreen").boolValue = true;
            managerSerialized.FindProperty("m_MaintainAspectRatioInSplitScreen").boolValue = false;
            managerSerialized.FindProperty("m_FixedNumberOfSplitScreens").intValue = -1;
            managerSerialized.FindProperty("m_MaxPlayerCount").intValue = 2;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == RegressionRootName)
                    Object.DestroyImmediate(root);
            }

            var regressionRoot = new GameObject(RegressionRootName);
            SceneManager.MoveGameObjectToScene(regressionRoot, scene);
            QaCamera032PlayerOutputLifecycleRegression regression =
                regressionRoot.AddComponent<QaCamera032PlayerOutputLifecycleRegression>();
            regression.Configure(
                provisioning,
                manager,
                slotP1,
                slotP2,
                outputP1,
                outputP2);

            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(regression);
            EditorUtility.SetDirty(regressionRoot);
            EditorSceneManager.MarkSceneDirty(scene);
            Require(
                EditorSceneManager.SaveScene(scene, PersistentScenePath),
                "CAMERA-032 QA persistent scene could not be saved.");
        }

        private static void RemoveLegacyCameraAuthority(Scene scene)
        {
            var destroy = new HashSet<GameObject>();
            CollectRoots<CameraOutputAuthoring>(scene, destroy);

            foreach (GameObject value in destroy)
            {
                if (value != null) Object.DestroyImmediate(value);
            }
        }

        private static void SetActiveGameApplication(GameApplicationAsset application)
        {
            ImmersiveFrameworkSettingsAsset settings =
                RequireAsset<ImmersiveFrameworkSettingsAsset>(SettingsPath);
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("activeGameApplication").objectReferenceValue = application;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void RestoreCanonicalState()
        {
            ImmersiveFrameworkSettingsAsset settings =
                RequireAsset<ImmersiveFrameworkSettingsAsset>(SettingsPath);
            GameApplicationAsset canonical =
                RequireAsset<GameApplicationAsset>(CanonicalGameApplicationPath);
            var serialized = new SerializedObject(settings);
            serialized.FindProperty("activeGameApplication").objectReferenceValue = canonical;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(PersistentScenePath, OpenSceneMode.Single);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == RegressionRootName)
                    Object.DestroyImmediate(root);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PersistentScenePath);

            QaCameraDefinitionAssetGuard.Ensure();

            AssetDatabase.DeleteAsset(GeneratedRoot);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Single);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode &&
                CurrentPhase == Phase.Running)
            {
                BeginWatching();
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode) return;

            StopWatching();
            if (CurrentPhase == Phase.RuntimePassed)
            {
                EditorApplication.delayCall -= FinishSuccess;
                EditorApplication.delayCall += FinishSuccess;
            }
            else if (CurrentPhase == Phase.Failed)
            {
                EditorApplication.delayCall -= FinishFailure;
                EditorApplication.delayCall += FinishFailure;
            }
            else if (CurrentPhase == Phase.Running)
            {
                RecordFailure(
                    "play-mode-interrupted",
                    "Play Mode exited before CAMERA-032 lifecycle evidence completed.");
                EditorApplication.delayCall -= FinishFailure;
                EditorApplication.delayCall += FinishFailure;
            }
        }

        private static void BeginWatching()
        {
            StopWatching();
            watching = true;
            startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (!watching || !EditorApplication.isPlaying) return;

            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                RecordFailure(
                    "runtime-timeout",
                    $"Runtime regression did not reach terminal evidence within '{TimeoutSeconds}' seconds.");
                StopWatching();
                EditorApplication.isPlaying = false;
                return;
            }

            if (!QaCamera032PlayerOutputLifecycleRegression.Executed) return;

            if (!QaCamera032PlayerOutputLifecycleRegression.Passed)
            {
                RecordFailure(
                    "runtime",
                    QaCamera032PlayerOutputLifecycleRegression.Diagnostic);
                StopWatching();
                EditorApplication.isPlaying = false;
                return;
            }

            int cases = QaCamera032PlayerOutputLifecycleRegression.CompletedCaseCount;
            if (cases != QaCamera032PlayerOutputLifecycleRegression.ExpectedCaseCount)
            {
                RecordFailure(
                    "case-count",
                    $"CAMERA-032 case count diverged. actual='{cases}' expected='{QaCamera032PlayerOutputLifecycleRegression.ExpectedCaseCount}'.");
                StopWatching();
                EditorApplication.isPlaying = false;
                return;
            }

            SessionState.SetInt(CaseCountKey, cases);
            SetPhase(Phase.RuntimePassed);
            StopWatching();
            EditorApplication.isPlaying = false;
        }

        private static void FinishSuccess()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                CurrentPhase != Phase.RuntimePassed)
                return;

            try
            {
                RestoreCanonicalState();
                int cases = SessionState.GetInt(CaseCountKey, 0);
                SetPhase(Phase.Completed);
                SessionState.SetBool(
                    AggregateResultKey,
                    true);
                Debug.Log(
                    $"{Prefix} status='Passed' verdict='CAMERA_032_PLAYER_OUTPUT_CERTIFIED' " +
                    $"cases='{cases}/{QaCamera032PlayerOutputLifecycleRegression.ExpectedCaseCount}' " +
                    "cycle='0->1->2->1->2->0' " +
                    "thirdPerson='PASS' channels='PASS' leaveRecompose='PASS' " +
                    "canonicalRestore='PASS'.");
            }
            catch (Exception exception)
            {
                RecordFailure("canonical-restore", exception.GetBaseException().Message);
                FinishFailure();
            }
        }

        private static void FinishFailure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            string failure = SessionState.GetString(FailureKey, "unknown");
            string cleanup = "NotAttempted";
            try
            {
                RestoreCanonicalState();
                cleanup = "CanonicalSharedRestored";
            }
            catch (Exception exception)
            {
                cleanup = "RestoreFailed:" + exception.GetBaseException().Message;
            }

            SetPhase(Phase.Failed);
            SessionState.SetBool(
                AggregateResultKey,
                false);
            Debug.LogError(
                $"{Prefix} status='Failed' verdict='CAMERA_032_PLAYER_OUTPUT_FAIL' " +
                $"diagnostic='{Escape(failure)}' cleanup='{Escape(cleanup)}'.");
        }

        private static void RecordFailure(string stage, string reason)
        {
            SessionState.SetString(FailureKey, $"{stage}: {reason}");
            SetPhase(Phase.Failed);
        }

        private static void ConfigureComposer(
            CameraRigComposer composer,
            CameraRigBehaviorDefinition behavior,
            CinemachineCamera cinemachine,
            string label)
        {
            var serialized = new SerializedObject(composer);
            serialized.FindProperty("behaviorDefinition").objectReferenceValue = behavior;
            serialized.FindProperty("cinemachineCamera").objectReferenceValue = cinemachine;
            serialized.FindProperty("logApplyRebuildDiagnostics").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(
                ReferenceEquals(composer.BehaviorDefinition, behavior),
                $"{label} did not receive its exact Camera Rig Behavior definition.");
        }

        private static void SetBindingArray(
            SerializedProperty array,
            IReadOnlyList<BindingData> bindings)
        {
            Require(array != null, "Camera Session playerOutputBindings property is missing.");
            array.arraySize = bindings.Count;
            for (int index = 0; index < bindings.Count; index++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("playerSlotProfile").objectReferenceValue =
                    bindings[index].Slot;
                element.FindPropertyRelative("outputDefinition").objectReferenceValue =
                    bindings[index].Output;
            }
        }

        private static void SetPresentationBindingArray(
            SerializedProperty array,
            IReadOnlyList<PresentationBindingData> bindings)
        {
            Require(
                array != null,
                "Camera Session playerPresentationBindings property is missing.");
            array.arraySize = bindings.Count;
            for (int index = 0; index < bindings.Count; index++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("playerSlotProfile").objectReferenceValue =
                    bindings[index].Slot;
                element.FindPropertyRelative("presentationDefinition").objectReferenceValue =
                    bindings[index].Presentation;
            }
        }

        private static void SetObjectArray(
            SerializedProperty array,
            params Object[] values)
        {
            Require(array != null, "Expected serialized array was not found.");
            array.arraySize = values?.Length ?? 0;
            for (int index = 0; values != null && index < values.Length; index++)
                array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static void SetReference(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            Require(field != null, $"Serialized property '{property}' is missing on '{target.name}'.");
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string property, bool value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty field = serialized.FindProperty(property);
            Require(field != null, $"Serialized property '{property}' is missing on '{target.name}'.");
            field.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CollectRoots<T>(Scene scene, ISet<GameObject> roots)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null) continue;
                T[] components = root.GetComponentsInChildren<T>(true);
                if (components.Length > 0) roots.Add(root);
            }
        }

        private static T RequireSingle<T>(Scene scene, string label)
            where T : Component
        {
            T resolved = null;
            int matches = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (T candidate in root.GetComponentsInChildren<T>(true))
                {
                    if (candidate == null) continue;
                    resolved = candidate;
                    matches++;
                }
            }

            Require(
                matches == 1 && resolved != null,
                $"CAMERA-032 requires exactly one {label}; found '{matches}'.");
            return resolved;
        }

        private static T RequireAsset<T>(string path) where T : Object
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            Require(value != null, $"Required QA asset is missing at '{path}'.");
            return value;
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
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
            (Phase)SessionState.GetInt(PhaseKey, (int)Phase.Idle);

        private static void SetPhase(Phase phase) =>
            SessionState.SetInt(PhaseKey, (int)phase);

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");

        private readonly struct BindingData
        {
            internal BindingData(PlayerSlotProfile slot, CameraOutputDefinition output)
            {
                Slot = slot;
                Output = output;
            }

            internal PlayerSlotProfile Slot { get; }
            internal CameraOutputDefinition Output { get; }
        }

        private readonly struct PresentationBindingData
        {
            internal PresentationBindingData(
                PlayerSlotProfile slot,
                CameraPresentationDefinition presentation)
            {
                Slot = slot;
                Presentation = presentation;
            }

            internal PlayerSlotProfile Slot { get; }
            internal CameraPresentationDefinition Presentation { get; }
        }
    }
}

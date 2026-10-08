using System;
using System.Linq;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.UnityInput;
using Immersive.QaFramework.IfAdr042;
using Immersive.QaFramework.IfAdr044;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Immersive.QaFramework.IfAdr044.Editor
{
    public static class QaIfAdr044Setup
    {
        private const string Root = "Assets/QA-IF-ADR-044";
        private const string Players = Root + "/Players";
        private const string Settings = Root + "/Settings";
        private const string PersistentScene = Root + "/Scenes/QA_IF_ADR_044_Persistent.unity";
        private const string ApplicationPath = Settings + "/GameApplication_QaIfAdr044.asset";
        private const string HostPrefabPath = Players + "/PF_LocalPlayerHost_QaIfAdr044.prefab";
        private const string ActorPrefabPath = Players + "/PF_PlayerActorRuntime_QaIfAdr044.prefab";
        private const string InputActionsPath = Players + "/InputActions_QaIfAdr044.asset";
        private const string PauseActionReferencePath = Players + "/PauseAction_QaIfAdr044.asset";
        private const string ActivityContentProfilePath = Settings + "/ActivityContentProfile_QaIfAdr044.asset";
        private const string ActivityAssetPath = Settings + "/Activity_QaIfAdr044.asset";
        private const string RouteAssetPath = Settings + "/Route_QaIfAdr044.asset";
        private const string RouteScene = Root + "/Scenes/QA_IF_ADR_044_Route.unity";
        private const string ActivityScene = Root + "/Scenes/QA_IF_ADR_044_ActivityContent.unity";
        private const string Base = "Assets/QA-IF-ADR-042";
        private const string BaseRouteScene = Base + "/Scenes/QA_IF_ADR_042_Route.unity";
        private const string BaseActivityScene = Base + "/Scenes/QA_IF_ADR_042_ActivityContent.unity";

        [MenuItem("Immersive Framework/QA/IF-ADR-044/Configure Gameplay Availability Certification")]
        public static void Configure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureBaseFixture();
            ValidateCanonicalRouteScene();
            ValidateCanonicalActivityScene();
            EnsureFolder("Assets", "QA-IF-ADR-044");
            EnsureFolder(Root, "Players");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Scenes");

            PlayerSessionProfile session = Load<PlayerSessionProfile>(Base + "/Settings/PlayerSession_QaIfAdr042.asset");
            PlayerSlotProfile p1 = Load<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P1.asset");
            PlayerSlotProfile p2 = Load<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P2.asset");
            QaIfAdr042SharedGroupEvidence outputEvidence = Load<QaIfAdr042SharedGroupEvidence>(Base + "/Settings/QaIfAdr042SharedGroupEvidence.asset");
            InputActionAsset sourceActions = Load<InputActionAsset>(Base + "/Players/InputActions_QaIfAdr042.asset");
            GameObject hostSource = Load<GameObject>(Base + "/Players/PF_LocalPlayerHost_QaIfAdr042.prefab");
            GameApplicationAsset applicationSource = Load<GameApplicationAsset>(Base + "/Settings/GameApplication_QaIfAdr042.asset");
            if (session == null || p1 == null || p2 == null || sourceActions == null || hostSource == null || applicationSource == null)
                throw new InvalidOperationException("IF-ADR-042 canonical Player fixture is incomplete; configure and validate it before IF-ADR-044.");
            ValidateBaseAssets(session, p1, p2, outputEvidence, sourceActions, hostSource, applicationSource);

            InputActionAsset actions = CreateInputActions(sourceActions);
            InputActionReference pauseAction = CreatePauseActionReference(actions);
            PlayerActorRuntimeHost actorHost = CreateActorPrefab();
            GameObject hostPrefab = CreateHostPrefab(actorHost, actions, pauseAction);

            ActivityContentProfileAsset activityContent = CreateActivityContentProfile();
            ActivityAsset activity = CreateActivity(activityContent, p1, p2);
            RouteAsset route = CreateRoute(activity);
            AssetDatabase.SaveAssets();
            CreateRouteScene();
            CreateActivityScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // Scene generation/restoration can invalidate wrappers loaded before NewScene(Single).
            // Reload every shared authoring reference before persisting Application -> Session.
            session = Load<PlayerSessionProfile>(Base + "/Settings/PlayerSession_QaIfAdr042.asset");
            p1 = Load<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P1.asset");
            p2 = Load<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P2.asset");
            outputEvidence = Load<QaIfAdr042SharedGroupEvidence>(Base + "/Settings/QaIfAdr042SharedGroupEvidence.asset");
            SessionCameraAssignmentAsset assignment = Load<SessionCameraAssignmentAsset>(Base + "/Settings/CameraAssignment_QaIfAdr042_SharedGroup.asset");
            GameObject outputPrefab = Load<GameObject>(Base + "/Camera/PF_CameraOutput_QaIfAdr042_Shared.prefab");
            route = Load<RouteAsset>(RouteAssetPath);
            GameApplicationAsset application = CreateApplication(session, route, assignment, outputPrefab);
            AssetDatabase.SaveAssets();
            CreatePersistentScene();
            // Scene restoration can invalidate the Application wrapper before binding.
            application = Load<GameApplicationAsset>(ApplicationPath);
            BindPersistentScene(application);
            AddToBuildSettings(RouteScene);
            AddToBuildSettings(ActivityScene);
            AddToBuildSettings(PersistentScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateGeneratedBaseline();
            Debug.Log("[IF-ADR-044] setup='Configured' baseline='Valid' source='QA-IF-ADR-042 Player Session and SharedGroup Camera' slots='2' host='ManagerProvisioned/Manual' activity='QA-owned/GameplayReady' route='QA_IF_ADR_044_Route' availability='canonical-input-adapter' actions='Player+Global/Pause' pause='PlayerPauseInput' scene='QA_IF_ADR_044_Persistent' application='GameApplication_QaIfAdr044'. Select the generated Game Application in ImmersiveFrameworkSettings and enter a fresh Play Mode session.");
        }

        private static void EnsureBaseFixture()
        {
            if (!HasBaseFixture())
                throw new InvalidOperationException("IF-ADR-044 requires the complete canonical IF-ADR-042 Player Session fixture. Configure IF-ADR-042 and resolve any setup diagnostics first.");
        }

        private static bool HasBaseFixture() =>
            AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(Base + "/Settings/GameApplication_QaIfAdr042.asset") != null &&
            AssetDatabase.LoadAssetAtPath<PlayerSessionProfile>(Base + "/Settings/PlayerSession_QaIfAdr042.asset") != null &&
            AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P1.asset") != null &&
            AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P2.asset") != null &&
            AssetDatabase.LoadAssetAtPath<QaIfAdr042SharedGroupEvidence>(Base + "/Settings/QaIfAdr042SharedGroupEvidence.asset") != null &&
            AssetDatabase.LoadAssetAtPath<RouteAsset>(Base + "/Settings/Route_QaIfAdr042.asset") != null &&
            AssetDatabase.LoadAssetAtPath<ActivityAsset>(Base + "/Settings/Activity_QaIfAdr042_SharedGroup.asset") != null &&
            AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(Base + "/Settings/ActivityContentProfile_QaIfAdr042.asset") != null &&
            AssetDatabase.LoadAssetAtPath<ActorProfile>(Base + "/Players/Actor_QaIfAdr042_P1.asset") != null &&
            AssetDatabase.LoadAssetAtPath<ActorProfile>(Base + "/Players/Actor_QaIfAdr042_P2.asset") != null &&
            AssetDatabase.LoadAssetAtPath<SessionCameraAssignmentAsset>(Base + "/Settings/CameraAssignment_QaIfAdr042_SharedGroup.asset") != null &&
            AssetDatabase.LoadAssetAtPath<CameraOutputDefinition>(Base + "/Camera/CameraOutput_QaIfAdr042_Shared.asset") != null &&
            AssetDatabase.LoadAssetAtPath<InputActionAsset>(Base + "/Players/InputActions_QaIfAdr042.asset") != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Players/PF_LocalPlayerHost_QaIfAdr042.prefab") != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Players/PF_PlayerActorRuntime_QaIfAdr042.prefab") != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Camera/PF_CameraOutput_QaIfAdr042_Shared.prefab") != null &&
            AssetDatabase.LoadAssetAtPath<GameObject>(Base + "/Camera/PF_CameraRig_QaIfAdr042_SharedGroup.prefab") != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(BaseRouteScene) != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(BaseActivityScene) != null &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(Base + "/Scenes/QA_IF_ADR_042_Persistent.unity") != null;

        private static PlayerActorRuntimeHost CreateActorPrefab()
        {
            string sourcePath = Base + "/Players/PF_PlayerActorRuntime_QaIfAdr042.prefab";
            string targetPath = ActorPrefabPath;
            EnsureParentFolder(targetPath);
            GameObject source = Load<GameObject>(sourcePath);
            GameObject contents = PrefabUtility.LoadPrefabContents(sourcePath);
            if (contents == null) throw new InvalidOperationException("Canonical IF-ADR-042 Player Actor prefab could not be opened for IF-ADR-044 authoring.");
            try
            {
                PlayerGameplayInputReader[] existing = contents.GetComponentsInChildren<PlayerGameplayInputReader>(true);
                if (existing.Length == 0) contents.AddComponent<PlayerGameplayInputReader>();
                else if (existing.Length != 1) throw new InvalidOperationException("Canonical Player Actor prefab must contain at most one gameplay reader before IF-ADR-044 authoring.");
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, targetPath);
                if (saved == null) throw new InvalidOperationException("IF-ADR-044 Player Actor Runtime prefab could not be saved.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            PlayerActorRuntimeHost actor = Load<GameObject>(targetPath).GetComponent<PlayerActorRuntimeHost>();
            if (source == null || actor == null || actor.GetComponent<PlayerActorDeclaration>() == null)
                throw new InvalidOperationException("IF-ADR-044 Actor prefab lost its canonical Player Actor declaration/runtime host.");
            return actor;
        }

        private static InputActionAsset CreateInputActions(InputActionAsset source)
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (actions == null)
            {
                actions = Object.Instantiate(source);
                if (actions == null) throw new InvalidOperationException("IF-ADR-044 could not clone the canonical IF-ADR-042 Input Actions asset.");
                actions.name = "InputActions_QaIfAdr044";
                AssetDatabase.CreateAsset(actions, InputActionsPath);
            }

            if (actions.FindActionMap("Player") == null || !actions.FindControlScheme("QA.Keyboard").HasValue)
                throw new InvalidOperationException("IF-ADR-044 Input Actions must preserve the canonical Player map and QA.Keyboard control scheme.");

            InputActionMap globalMap = actions.FindActionMap("Global");
            if (globalMap == null) globalMap = actions.AddActionMap("Global");
            InputAction pauseAction = globalMap.FindAction("Pause");
            if (pauseAction == null) pauseAction = globalMap.AddAction("Pause", InputActionType.Button);
            if (pauseAction.type != InputActionType.Button || globalMap.actions.Count != 1)
                throw new InvalidOperationException("IF-ADR-044 Global map must contain exactly one Button action named Pause.");

            InputBinding[] escapeBindings = pauseAction.bindings
                .Where(binding => binding.path == "<Keyboard>/escape" &&
                    (binding.groups ?? string.Empty).Split(';').Contains("QA.Keyboard"))
                .ToArray();
            if (escapeBindings.Length == 0)
                pauseAction.AddBinding("<Keyboard>/escape", groups: "QA.Keyboard");
            else if (escapeBindings.Length != 1)
                throw new InvalidOperationException("IF-ADR-044 Global/Pause must contain exactly one QA.Keyboard Escape binding.");
            if (pauseAction.bindings.Count != 1)
                throw new InvalidOperationException("IF-ADR-044 Global/Pause must contain only its authored Escape binding.");

            EditorUtility.SetDirty(actions);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return Load<InputActionAsset>(InputActionsPath);
        }

        private static InputActionReference CreatePauseActionReference(InputActionAsset actions)
        {
            InputActionMap globalMap = actions.FindActionMap("Global");
            InputAction pauseAction = globalMap != null ? globalMap.FindAction("Pause") : null;
            if (pauseAction == null) throw new InvalidOperationException("IF-ADR-044 cannot author a Pause reference without Global/Pause.");

            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(PauseActionReferencePath);
            if (reference == null)
            {
                reference = InputActionReference.Create(pauseAction);
                if (reference == null) throw new InvalidOperationException("IF-ADR-044 Pause InputActionReference could not be created.");
                AssetDatabase.CreateAsset(reference, PauseActionReferencePath);
                AssetDatabase.SaveAssets();
                reference = Load<InputActionReference>(PauseActionReferencePath);
            }

            if (reference.action != pauseAction)
            {
                reference.Set(pauseAction);
                EditorUtility.SetDirty(reference);
                AssetDatabase.SaveAssets();
            }
            if (reference.action == null || reference.action.id != pauseAction.id ||
                reference.action.actionMap == null || reference.action.actionMap.id != globalMap.id ||
                reference.action.actionMap.asset != actions)
                throw new InvalidOperationException("IF-ADR-044 Pause InputActionReference must resolve to the exact generated Global/Pause action.");
            return reference;
        }

        private static GameObject CreateHostPrefab(PlayerActorRuntimeHost actorHost, InputActionAsset actions,
            InputActionReference pauseAction)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(Base + "/Players/PF_LocalPlayerHost_QaIfAdr042.prefab");
            if (contents == null) throw new InvalidOperationException("Canonical IF-ADR-042 Player Host prefab could not be opened for IF-ADR-044 authoring.");
            try
            {
                PlayerInput[] inputs = contents.GetComponents<PlayerInput>();
                LocalPlayerHostAuthoring[] hosts = contents.GetComponents<LocalPlayerHostAuthoring>();
                if (inputs.Length != 1 || hosts.Length != 1)
                    throw new InvalidOperationException("Canonical Host prefab must contain exactly one root PlayerInput and LocalPlayerHostAuthoring.");
                PlayerInput input = inputs[0];
                LocalPlayerHostAuthoring host = hosts[0];
                input.actions = actions;
                Set(host, "playerActorRuntimeHostPrefab", actorHost);
                UnityPlayerInputGateAdapter[] adapters = contents.GetComponentsInChildren<UnityPlayerInputGateAdapter>(true);
                if (adapters.Length > 1 || (adapters.Length == 1 && adapters[0].gameObject != contents))
                    throw new InvalidOperationException("Canonical Host must have at most one root Unity Player Input Gate Adapter.");
                UnityPlayerInputGateAdapter adapter = adapters.Length == 1 ? adapters[0] : contents.AddComponent<UnityPlayerInputGateAdapter>();
                PlayerPauseInput[] pauseBindings = contents.GetComponents<PlayerPauseInput>();
                if (pauseBindings.Length > 1)
                    throw new InvalidOperationException("Canonical Host must have at most one co-located PlayerPauseInput.");
                PlayerPauseInput pauseBinding = pauseBindings.Length == 1
                    ? pauseBindings[0]
                    : contents.AddComponent<PlayerPauseInput>();
                Set(pauseBinding, "pauseAction", pauseAction);
                SerializedObject serialized = new SerializedObject(adapter);
                serialized.Update();
                SerializedProperty adapterInput = serialized.FindProperty("playerInput");
                SerializedProperty blockOnInputAcceptance = serialized.FindProperty("blockOnInputAcceptance");
                SerializedProperty blockOnGameplayAction = serialized.FindProperty("blockOnGameplayAction");
                SerializedProperty map = serialized.FindProperty("gameplayActionMap");
                SerializedProperty actionMapAsset = map != null ? map.FindPropertyRelative("actionAsset") : null;
                SerializedProperty actionMapId = map != null ? map.FindPropertyRelative("actionMapId") : null;
                SerializedProperty cachedActionMapName = map != null ? map.FindPropertyRelative("cachedActionMapName") : null;
                if (adapterInput == null || blockOnInputAcceptance == null || blockOnGameplayAction == null ||
                    actionMapAsset == null || actionMapId == null || cachedActionMapName == null)
                    throw new InvalidOperationException("IF-ADR-044 Unity Player Input Gate Adapter serialized fields do not match the canonical authoring contract.");
                adapterInput.objectReferenceValue = input;
                InputActionMap actionMap = actions.FindActionMap("Player");
                if (actionMap == null) throw new InvalidOperationException("IF-ADR-044 requires the canonical Player Action Map.");
                actionMapAsset.objectReferenceValue = actions;
                actionMapId.stringValue = actionMap.id.ToString("D");
                cachedActionMapName.stringValue = actionMap.name;
                blockOnInputAcceptance.boolValue = false;
                blockOnGameplayAction.boolValue = true;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                GameObject saved = PrefabUtility.SaveAsPrefabAsset(contents, HostPrefabPath);
                if (saved == null) throw new InvalidOperationException("IF-ADR-044 Local Player Host prefab could not be saved.");
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
            GameObject result = Load<GameObject>(HostPrefabPath);
            PlayerInput savedInput = result != null ? result.GetComponent<PlayerInput>() : null;
            UnityPlayerInputGateAdapter savedAdapter = result != null ? result.GetComponent<UnityPlayerInputGateAdapter>() : null;
            if (savedInput == null || savedAdapter == null || savedAdapter.PlayerInput != savedInput)
                throw new InvalidOperationException("IF-ADR-044 Host Gate Adapter authoring is missing its exact PlayerInput.");
            if (!savedAdapter.TryValidateAuthoring(out string issue))
                throw new InvalidOperationException("IF-ADR-044 Host Gate Adapter authoring is invalid. " + issue);
            PlayerPauseInput savedPauseBinding = result.GetComponent<PlayerPauseInput>();
            if (savedPauseBinding == null || savedPauseBinding.PauseAction != pauseAction ||
                !savedPauseBinding.TryValidateAuthoring(out issue))
                throw new InvalidOperationException("IF-ADR-044 Host Pause PlayerInput Binding authoring is invalid. " + issue);
            return result;
        }

        private static ActivityContentProfileAsset CreateActivityContentProfile()
        {
            EnsureParentFolder(ActivityContentProfilePath);
            if (AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(ActivityContentProfilePath) == null &&
                !AssetDatabase.CopyAsset(Base + "/Settings/ActivityContentProfile_QaIfAdr042.asset", ActivityContentProfilePath))
                throw new InvalidOperationException("Could not create the isolated IF-ADR-044 Activity Content Profile from the validated IF-ADR-042 baseline.");

            ActivityContentProfileAsset profile = Load<ActivityContentProfileAsset>(ActivityContentProfilePath);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.Update();
            RequireProperty(serialized, "profileId").stringValue = "qa-if-adr-044.activity-content";
            SerializedProperty scenes = RequireProperty(serialized, "scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            RequireProperty(entry, "contentId").stringValue = "qa-if-adr-044.activity-content.main";
            RequireProperty(entry, "scenePath").stringValue = ActivityScene;
            RequireProperty(entry, "sceneName").stringValue = System.IO.Path.GetFileNameWithoutExtension(ActivityScene);
            SetEnum(RequireProperty(entry, "requiredness"), "Required");
            SetEnum(RequireProperty(entry, "loadMode"), "Additive");
            SetEnum(RequireProperty(entry, "releasePolicy"), "ReleaseOnActivityChange");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateActivity(ActivityContentProfileAsset content,
            PlayerSlotProfile p1, PlayerSlotProfile p2)
        {
            EnsureParentFolder(ActivityAssetPath);
            if (AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityAssetPath) == null &&
                !AssetDatabase.CopyAsset(Base + "/Settings/Activity_QaIfAdr042_SharedGroup.asset", ActivityAssetPath))
                throw new InvalidOperationException("Could not create the isolated IF-ADR-044 Activity from the validated IF-ADR-042 baseline.");

            ActivityAsset activity = Load<ActivityAsset>(ActivityAssetPath);
            Set(activity, "activityId", "qa-if-adr-044.consumer-gameplay-availability");
            Set(activity, "activityName", "IF-ADR-044 Consumer Gameplay Availability");
            Set(activity, "activityContentProfile", content);
            SerializedObject serialized = new SerializedObject(activity);
            serialized.Update();
            SetEnum(RequireProperty(serialized, "playerParticipationRequirementLevel"), "GameplayReady");
            SerializedProperty slots = RequireProperty(serialized, "playerParticipationExplicitSlotProfiles");
            slots.arraySize = 2;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = p1;
            slots.GetArrayElementAtIndex(1).objectReferenceValue = p2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);
            return activity;
        }

        private static RouteAsset CreateRoute(ActivityAsset activity)
        {
            EnsureParentFolder(RouteAssetPath);
            if (AssetDatabase.LoadAssetAtPath<RouteAsset>(RouteAssetPath) == null &&
                !AssetDatabase.CopyAsset(Base + "/Settings/Route_QaIfAdr042.asset", RouteAssetPath))
                throw new InvalidOperationException("Could not create the isolated IF-ADR-044 Route from the validated IF-ADR-042 baseline.");

            RouteAsset route = Load<RouteAsset>(RouteAssetPath);
            Set(route, "routeId", "qa-if-adr-044.start");
            Set(route, "routeName", "IF-ADR-044 Certification");
            Set(route, "primaryScenePath", RouteScene);
            Set(route, "primarySceneName", System.IO.Path.GetFileNameWithoutExtension(RouteScene));
            Set(route, "startupActivity", activity);
            return route;
        }

        private static void CreateRouteScene()
        {
            SceneSetup[] previous;
            Scene scene = BeginGeneratedScene(out previous);
            try
            {
                RouteAsset route = Load<RouteAsset>(RouteAssetPath);
                GameObject root = new GameObject("IF-ADR-044 Startup Route Content");
                RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
                Set(contribution, "route", route);
                Set(contribution, "localContentId", "qa-if-adr-044.route-content");
                if (contribution.Route != route || !contribution.HasExplicitLocalContentId)
                    throw new InvalidOperationException("IF-ADR-044 Route contribution did not retain its generated Route before scene save.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, RouteScene))
                    throw new InvalidOperationException("Could not save IF-ADR-044 Route scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void CreateActivityScene()
        {
            SceneSetup[] previous;
            Scene scene = BeginGeneratedScene(out previous);
            try
            {
                ActivityAsset activity = Load<ActivityAsset>(ActivityAssetPath);
                GameObject root = new GameObject("IF-ADR-044 Activity Content");
                ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
                Set(contribution, "activity", activity);
                Set(contribution, "localContentId", "qa-if-adr-044.activity-content.main");
                if (contribution.Activity != activity || !contribution.HasExplicitLocalContentId)
                    throw new InvalidOperationException("IF-ADR-044 Activity contribution did not retain its generated Activity before scene save.");
                PlayerSessionObserver observer = root.AddComponent<PlayerSessionObserver>();
                Set(observer, "scope", (int)LocalPlayerProvisioningConsumerScope.Activity);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ActivityScene))
                    throw new InvalidOperationException("Could not save IF-ADR-044 Activity Content scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static Scene BeginGeneratedScene(out SceneSetup[] previousSetup)
        {
            previousSetup = EditorSceneManager.GetSceneManagerSetup();
            bool hasLoadedScene = previousSetup != null && previousSetup.Any(setup => setup.isLoaded);
            bool hasActiveScene = previousSetup != null && previousSetup.Any(setup => setup.isActive && setup.isLoaded);
            if (!hasLoadedScene || !hasActiveScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                previousSetup = EditorSceneManager.GetSceneManagerSetup();
            }
            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static GameApplicationAsset CreateApplication(PlayerSessionProfile session,
            RouteAsset startupRoute, SessionCameraAssignmentAsset assignment, GameObject outputPrefab)
        {
            GameApplicationAsset app = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationPath);
            if (app == null)
            {
                if (!AssetDatabase.CopyAsset(Base + "/Settings/GameApplication_QaIfAdr042.asset", ApplicationPath))
                    throw new InvalidOperationException("Could not clone the canonical IF-ADR-042 Game Application for the isolated IF-ADR-044 scene.");
                app = Load<GameApplicationAsset>(ApplicationPath);
            }
            if (app == null) throw new InvalidOperationException("IF-ADR-044 Game Application could not be loaded after copy.");
            SerializedObject serialized = new SerializedObject(app);
            serialized.Update();
            RequireProperty(serialized, "applicationName").stringValue = "IF-ADR-044 Consumer Gameplay Availability Certification";
            RequireProperty(serialized, "playerSessionEnabled").boolValue = true;
            RequireProperty(serialized, "startupRoute").objectReferenceValue = startupRoute;
            RequireProperty(serialized, "defaultPlayerSessionProfile").objectReferenceValue = session;
            SerializedProperty assignments = RequireProperty(serialized, "startupCameraAssignments");
            assignments.arraySize = 1;
            assignments.GetArrayElementAtIndex(0).objectReferenceValue = assignment;
            SerializedProperty outputs = RequireProperty(serialized, "cameraSession.outputPrefabs");
            outputs.arraySize = 1;
            outputs.GetArrayElementAtIndex(0).objectReferenceValue = outputPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(app);
            return app;
        }

        private static void CreatePersistentScene()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                // NewScene(Single) can invalidate Unity wrappers loaded before the scene switch.
                // Reload every asset reference after the switch and before serializing the scene.
                GameApplicationAsset app = Load<GameApplicationAsset>(ApplicationPath);
                GameObject hostPrefab = Load<GameObject>(HostPrefabPath);
                PlayerSlotProfile p1 = Load<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P1.asset");
                PlayerSlotProfile p2 = Load<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P2.asset");
                QaIfAdr042SharedGroupEvidence outputEvidence = Load<QaIfAdr042SharedGroupEvidence>(Base + "/Settings/QaIfAdr042SharedGroupEvidence.asset");
                GameObject root = new GameObject("IF-ADR-044 Player Provisioning");
                PlayerInputManager manager = root.AddComponent<PlayerInputManager>();
                manager.playerPrefab = hostPrefab;
                manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
                manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                manager.splitScreen = false;
                Set(manager, "m_MaxPlayerCount", 2);
                LocalPlayerProvisioningAuthoring provisioning = root.AddComponent<LocalPlayerProvisioningAuthoring>();
                Set(provisioning, "playerInputManager", manager);
                Set(provisioning, "localPlayerHostPrefab", hostPrefab);
                LocalPlayerProvisioningEndpointRegistration registration = root.AddComponent<LocalPlayerProvisioningEndpointRegistration>();
                Set(registration, "provisioningAuthoring", provisioning);
                GameObject scenarioObject = new GameObject("IF-ADR-044 Consumer Gameplay Availability Certification");
                QaIfAdr044ConsumerGameplayAvailabilityScenario scenario = scenarioObject.AddComponent<QaIfAdr044ConsumerGameplayAvailabilityScenario>();
                Set(scenario, "gameApplication", app);
                Set(scenario, "provisioning", provisioning);
                Set(scenario, "provisioningRegistration", registration);
                Set(scenario, "outputEvidence", outputEvidence);
                Set(scenario, "player1", p1);
                Set(scenario, "player2", p2);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, PersistentScene))
                    throw new InvalidOperationException("Could not save IF-ADR-044 Persistent Content scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void BindPersistentScene(GameApplicationAsset app)
        {
            if (app == null) throw new InvalidOperationException("IF-ADR-044 Game Application could not be reloaded before persistent-scene binding.");
            SerializedObject serialized = new SerializedObject(app);
            serialized.Update();
            SerializedProperty persistent = serialized.FindProperty("persistentContent.containerScene");
            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScene);
            if (persistent == null || scene == null) throw new InvalidOperationException("IF-ADR-044 persistent scene reference could not be authored.");
            persistent.objectReferenceValue = scene;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(app);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int retainedIndex = -1;
            for (int index = scenes.Count - 1; index >= 0; index--)
            {
                if (scenes[index].path != scenePath) continue;
                if (retainedIndex < 0)
                {
                    retainedIndex = index;
                    scenes[index] = new EditorBuildSettingsScene(scenePath, true);
                }
                else
                {
                    scenes.RemoveAt(index);
                    retainedIndex--;
                }
            }
            if (retainedIndex < 0) scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ValidateGeneratedBaseline()
        {
            ValidateGeneratedRouteScene();
            ValidateGeneratedActivityScene();
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = default;
            try
            {
                Scene alreadyLoaded = SceneManager.GetSceneByPath(PersistentScene);
                scene = alreadyLoaded.IsValid() && alreadyLoaded.isLoaded
                    ? alreadyLoaded
                    : EditorSceneManager.OpenScene(PersistentScene, OpenSceneMode.Additive);

                // Scene authoring/restoration can invalidate wrappers. Resolve every asset
                // used by the assertions after opening the generated scene.
                GameApplicationAsset app = Load<GameApplicationAsset>(ApplicationPath);
                PlayerSessionProfile session = Load<PlayerSessionProfile>(Base + "/Settings/PlayerSession_QaIfAdr042.asset");
                PlayerSlotProfile p1 = Load<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P1.asset");
                PlayerSlotProfile p2 = Load<PlayerSlotProfile>(Base + "/Settings/PlayerSlot_QaIfAdr042_P2.asset");
                QaIfAdr042SharedGroupEvidence outputEvidence = Load<QaIfAdr042SharedGroupEvidence>(Base + "/Settings/QaIfAdr042SharedGroupEvidence.asset");
                RouteAsset route = Load<RouteAsset>(RouteAssetPath);
                ActivityAsset activity = Load<ActivityAsset>(ActivityAssetPath);
                ActivityContentProfileAsset activityContent = Load<ActivityContentProfileAsset>(ActivityContentProfilePath);
                SessionCameraAssignmentAsset assignment = Load<SessionCameraAssignmentAsset>(Base + "/Settings/CameraAssignment_QaIfAdr042_SharedGroup.asset");
                CameraOutputDefinition outputDefinition = Load<CameraOutputDefinition>(Base + "/Camera/CameraOutput_QaIfAdr042_Shared.asset");
                GameObject outputPrefab = Load<GameObject>(Base + "/Camera/PF_CameraOutput_QaIfAdr042_Shared.prefab");
                InputActionAsset actions = Load<InputActionAsset>(InputActionsPath);
                InputActionReference pauseAction = Load<InputActionReference>(PauseActionReferencePath);
                GameObject actorPrefab = Load<GameObject>(ActorPrefabPath);
                GameObject hostPrefab = Load<GameObject>(HostPrefabPath);
                SceneAsset persistentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScene);

                bool sessionValid = session.TryValidate(out string sessionIssue);
                if (!sessionValid)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: canonical IF-ADR-042 Player Session is invalid. " + sessionIssue);
                bool exactSessionSlots = session.SupportedSlotCount == 2 &&
                    session.SupportedSlots.Count == 2 && session.SupportedSlots[0] == p1 &&
                    session.SupportedSlots[1] == p2;
                bool sessionPolicyValid = !session.InitialJoiningOpen &&
                    session.HostProvisioning == PlayerHostProvisioningMode.ManagerProvisioned &&
                    session.ActorResolutionPolicy == PlayerActorResolutionPolicy.ResolveConfiguredDefault &&
                    p1.DefaultActorProfile != null && p2.DefaultActorProfile != null;
                bool applicationSessionBound = app.PlayerSessionEnabled &&
                    app.DefaultPlayerSessionProfile == session;
                if (!exactSessionSlots || !sessionPolicyValid || !applicationSessionBound)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: generated Application/Session baseline mismatch (sessionBound=" +
                        applicationSessionBound + ", exactOrderedSlots=" + exactSessionSlots + ", sessionPolicy=" + sessionPolicyValid +
                        ", slotCount=" + session.SupportedSlots.Count + ", appSession='" +
                        (app.DefaultPlayerSessionProfile != null ? app.DefaultPlayerSessionProfile.name : "<null>") + "').");

                if (app.StartupRoute != route || route.StartupActivity != activity || !route.HasPrimaryScene ||
                    route.PrimaryScenePath != RouteScene ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(route.PrimaryScenePath) == null ||
                    activity.ActivityContentProfile != activityContent ||
                    activity.PlayerParticipationRequirementLevel != PlayerParticipationRequirementLevel.GameplayReady ||
                    activity.PlayerParticipationProjectionMode != ActivityParticipationProjectionMode.ExplicitSlots ||
                    activity.PlayerParticipationZeroParticipantPolicy != ActivityParticipationZeroParticipantPolicy.Allowed ||
                    activity.PlayerParticipationExplicitSlotProfiles.Count != 2 ||
                    activity.PlayerParticipationExplicitSlotProfiles[0] != p1 ||
                    activity.PlayerParticipationExplicitSlotProfiles[1] != p2 ||
                    !activityContent.HasScenes || activityContent.SceneCount != 1 ||
                    activityContent.ProfileId != "qa-if-adr-044.activity-content" ||
                    !activityContent.Scenes[0].HasExplicitContentId ||
                    activityContent.Scenes[0].ContentId != "qa-if-adr-044.activity-content.main" ||
                    activityContent.Scenes[0].ScenePath != ActivityScene ||
                    activityContent.Scenes[0].Requiredness != FrameworkContentRequiredness.Required ||
                    activityContent.Scenes[0].LoadMode != ActivityContentSceneLoadMode.Additive ||
                    activityContent.Scenes[0].ReleasePolicy != ActivityContentReleasePolicy.ReleaseOnActivityChange ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(activityContent.Scenes[0].ScenePath) == null)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: generated Application → Route → GameplayReady Activity scene chain is invalid.");

                if (app.CameraSession == null)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: canonical Game Application Camera Session is missing.");
                if (!app.CameraSession.TryValidate(out string cameraIssue))
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: canonical Game Application Camera Session is invalid. " + cameraIssue);
                if (app.CameraSession.OutputPrefabs.Count != 1 || app.CameraSession.OutputPrefabs[0] != outputPrefab ||
                    app.StartupCameraAssignments.Count != 1 || app.StartupCameraAssignments[0] != assignment ||
                    outputEvidence == null || persistentScene == null || app.PersistentContent == null ||
                    app.PersistentContent.ContainerScene != persistentScene)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Game Application must retain the canonical IF-ADR-042 camera composition and generated Persistent scene.");
                if (!assignment.TryBuild(out SessionCameraAssignment builtAssignment, out string assignmentIssue) ||
                    builtAssignment.OccurrenceMode != CameraOccurrenceMode.SharedGroup ||
                    builtAssignment.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots ||
                    builtAssignment.TargetPolicy != CameraTargetPolicy.MemberActorTargets ||
                    builtAssignment.MemberSlots.Count != 2 || builtAssignment.Outputs.Count != 1 ||
                    builtAssignment.MemberSlots[0] != p1.PlayerSlotId || builtAssignment.MemberSlots[1] != p2.PlayerSlotId ||
                    builtAssignment.Outputs[0].OutputId != outputDefinition.OutputId || builtAssignment.MemberOutputs.Count != 0)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: canonical IF-ADR-042 SharedGroup Camera Assignment is invalid. " + assignmentIssue);
                ValidateSharedOutputPrefab(outputPrefab, outputEvidence, outputDefinition);
                ValidateSharedAssignmentRig(assignment.RigPrefab, outputEvidence);

                PlayerInput[] hostInputs = hostPrefab.GetComponents<PlayerInput>();
                LocalPlayerHostAuthoring[] hostAuthorings = hostPrefab.GetComponents<LocalPlayerHostAuthoring>();
                UnityPlayerInputGateAdapter[] adapters = hostPrefab.GetComponentsInChildren<UnityPlayerInputGateAdapter>(true);
                PlayerPauseInput[] pauseBindings = hostPrefab.GetComponents<PlayerPauseInput>();
                LocalPlayerHostAuthoring hostAuthoring = hostAuthorings.Length == 1 ? hostAuthorings[0] : null;
                PlayerInput hostInput = hostInputs.Length == 1 ? hostInputs[0] : null;
                UnityPlayerInputGateAdapter adapter = adapters.Length == 1 ? adapters[0] : null;
                PlayerActorRuntimeHost actorRuntime = actorPrefab.GetComponent<PlayerActorRuntimeHost>();
                PlayerActorDeclaration actorDeclaration = actorPrefab.GetComponent<PlayerActorDeclaration>();
                PlayerGameplayInputReader[] readers = actorPrefab.GetComponentsInChildren<PlayerGameplayInputReader>(true);
                if (hostInputs.Length != 1 || hostAuthorings.Length != 1 || adapters.Length != 1 || pauseBindings.Length != 1 ||
                    hostInput == null || hostAuthoring == null || adapter == null || actorRuntime == null ||
                    actorDeclaration == null || readers.Length != 1 || pauseBindings[0] == null)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Host and Actor prefabs must have one exact PlayerInput/Gate Adapter/Pause binding and one declared gameplay reader.");
                if (adapter.gameObject != hostPrefab || hostAuthoring.PlayerInput != hostInput ||
                    hostAuthoring.PlayerActorRuntimeHostPrefab != actorRuntime ||
                    actorRuntime.PlayerActorDeclaration != actorDeclaration)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Host and Actor references do not resolve to the canonical generated components.");
                if (!actorRuntime.TryValidateConfiguration(out string actorIssue))
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: generated Player Actor Runtime prefab is invalid. " + actorIssue);

                if (hostInput.actions != actions || hostInput.defaultActionMap != "Player" ||
                    hostInput.defaultControlScheme != "QA.Keyboard" || actions.FindActionMap("Player") == null ||
                    !actions.FindControlScheme("QA.Keyboard").HasValue)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Host PlayerInput must use the generated Player map and QA.Keyboard scheme.");
                if (!adapter.TryValidateAuthoring(out string adapterIssue))
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Host Gate Adapter authoring is invalid. " + adapterIssue);
                if (adapter.PlayerInput != hostInput || adapter.BlockOnInputAcceptance || !adapter.BlockOnGameplayAction)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Host Gate Adapter must target the exact PlayerInput and gate gameplay action without blocking Input Acceptance.");
                InputActionMap globalMap = actions.FindActionMap("Global");
                InputAction pauseRuntimeAction = globalMap != null ? globalMap.FindAction("Pause") : null;
                if (pauseRuntimeAction == null || pauseRuntimeAction.type != InputActionType.Button ||
                    pauseRuntimeAction.bindings.Count != 1 ||
                    pauseRuntimeAction.bindings[0].path != "<Keyboard>/escape" ||
                    !(pauseRuntimeAction.bindings[0].groups ?? string.Empty).Split(';').Contains("QA.Keyboard"))
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: generated Global/Pause action must have exactly one QA.Keyboard Escape binding.");
                if (pauseBindings[0].PauseAction != pauseAction)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Host Pause binding does not reference the generated Global/Pause action.");
                if (!pauseBindings[0].TryValidateAuthoring(out string pauseIssue))
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Host Pause PlayerInput Binding authoring is invalid. " + pauseIssue);

                PlayerInputManager[] managers = FindComponents<PlayerInputManager>(scene);
                LocalPlayerProvisioningAuthoring[] endpoints = FindComponents<LocalPlayerProvisioningAuthoring>(scene);
                LocalPlayerProvisioningEndpointRegistration[] registrations = FindComponents<LocalPlayerProvisioningEndpointRegistration>(scene);
                QaIfAdr044ConsumerGameplayAvailabilityScenario[] scenarios = FindComponents<QaIfAdr044ConsumerGameplayAvailabilityScenario>(scene);
                if (managers.Length != 1 || endpoints.Length != 1 || registrations.Length != 1 || scenarios.Length != 1 ||
                    managers[0] == null || endpoints[0] == null || registrations[0] == null || scenarios[0] == null)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Persistent scene must contain exactly one live Manager, Provisioning endpoint, registration and Scenario.");
                if (
                    !managers[0].isActiveAndEnabled || managers[0].gameObject != endpoints[0].gameObject ||
                    managers[0].playerPrefab != hostPrefab || managers[0].maxPlayerCount != 2 ||
                    managers[0].joinBehavior != PlayerJoinBehavior.JoinPlayersManually ||
                    managers[0].notificationBehavior != PlayerNotifications.InvokeCSharpEvents || managers[0].splitScreen ||
                    !endpoints[0].isActiveAndEnabled || !registrations[0].isActiveAndEnabled || !scenarios[0].isActiveAndEnabled ||
                    endpoints[0].PlayerInputManager != managers[0] || endpoints[0].LocalPlayerHostPrefab != hostPrefab ||
                    !endpoints[0].UsesManualJoin || !endpoints[0].UsesCSharpJoinNotifications ||
                    registrations[0].gameObject != endpoints[0].gameObject ||
                    registrations[0].ProvisioningAuthoring != endpoints[0])
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Persistent Manager-Provisioned composition does not match the generated Host.");

                SerializedObject serialized = new SerializedObject(scenarios[0]);
                serialized.Update();
                RequireReference(serialized, "gameApplication", app);
                RequireReference(serialized, "provisioning", endpoints[0]);
                RequireReference(serialized, "provisioningRegistration", registrations[0]);
                RequireReference(serialized, "outputEvidence", outputEvidence);
                RequireReference(serialized, "player1", p1);
                RequireReference(serialized, "player2", p2);
                SerializedObject appSerialized = new SerializedObject(app);
                appSerialized.Update();
                SerializedProperty persistent = appSerialized.FindProperty("persistentContent.containerScene");
                if (persistent == null || persistent.objectReferenceValue != persistentScene)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: Game Application is not bound to its generated Persistent scene.");
                ValidateBuildSettingsScene(RouteScene);
                ValidateBuildSettingsScene(ActivityScene);
                ValidateBuildSettingsScene(PersistentScene);
            }
            finally
            {
                // Restore the previous scene set first. The generated scene may be
                // the only loaded scene and Unity refuses to close the last one.
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void ValidateBaseAssets(PlayerSessionProfile session, PlayerSlotProfile p1,
            PlayerSlotProfile p2, QaIfAdr042SharedGroupEvidence outputEvidence, InputActionAsset actions,
            GameObject hostPrefab, GameApplicationAsset app)
        {
            if (!session.TryValidate(out string sessionIssue) || session.SupportedSlotCount != 2 ||
                session.SupportedSlots.Count != 2 || session.SupportedSlots[0] != p1 ||
                session.SupportedSlots[1] != p2 || session.InitialJoiningOpen ||
                session.HostProvisioning != PlayerHostProvisioningMode.ManagerProvisioned ||
                session.ActorResolutionPolicy != PlayerActorResolutionPolicy.ResolveConfiguredDefault ||
                p1.DefaultActorProfile == null || p2.DefaultActorProfile == null ||
                p1.DefaultActorProfile.ActorKind != ActorKind.Player || p2.DefaultActorProfile.ActorKind != ActorKind.Player ||
                p1.DefaultActorProfile.ActorRole != ActorRole.Protagonist || p2.DefaultActorProfile.ActorRole != ActorRole.Protagonist)
                throw new InvalidOperationException("IF-ADR-044 requires the valid canonical IF-ADR-042 two-Slot Manager-Provisioned Session. " + sessionIssue);

            RouteAsset route = Load<RouteAsset>(Base + "/Settings/Route_QaIfAdr042.asset");
            ActivityAsset activity = Load<ActivityAsset>(Base + "/Settings/Activity_QaIfAdr042_SharedGroup.asset");
            SessionCameraAssignmentAsset assignment = Load<SessionCameraAssignmentAsset>(Base + "/Settings/CameraAssignment_QaIfAdr042_SharedGroup.asset");
            CameraOutputDefinition outputDefinition = Load<CameraOutputDefinition>(Base + "/Camera/CameraOutput_QaIfAdr042_Shared.asset");
            GameObject outputPrefab = Load<GameObject>(Base + "/Camera/PF_CameraOutput_QaIfAdr042_Shared.prefab");
            GameObject actorPrefab = Load<GameObject>(Base + "/Players/PF_PlayerActorRuntime_QaIfAdr042.prefab");

            if (!app.PlayerSessionEnabled || app.DefaultPlayerSessionProfile != session || app.StartupRoute != route ||
                route.StartupActivity != activity || !route.HasPrimaryScene || route.PrimaryScenePath != BaseRouteScene ||
                app.PersistentContent == null || app.PersistentContent.ContainerScene !=
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(Base + "/Scenes/QA_IF_ADR_042_Persistent.unity"))
                throw new InvalidOperationException("IF-ADR-044 requires the canonical IF-ADR-042 Game Application → Session/Route/Activity composition.");
            if (activity.PlayerParticipationProjectionMode != ActivityParticipationProjectionMode.ExplicitSlots ||
                activity.PlayerParticipationZeroParticipantPolicy != ActivityParticipationZeroParticipantPolicy.Allowed ||
                activity.PlayerParticipationRequirementLevel != PlayerParticipationRequirementLevel.LogicalActorsPrepared ||
                activity.PlayerParticipationExplicitSlotProfiles.Count != 2 ||
                activity.PlayerParticipationExplicitSlotProfiles[0] != p1 ||
                activity.PlayerParticipationExplicitSlotProfiles[1] != p2)
                throw new InvalidOperationException("IF-ADR-044 startup Activity must preserve the canonical two-Slot Player participation projection.");

            if (app.CameraSession == null)
                throw new InvalidOperationException("IF-ADR-044 canonical IF-ADR-042 Game Application has no Camera Session.");
            if (!app.CameraSession.TryValidate(out string cameraIssue))
                throw new InvalidOperationException("IF-ADR-044 canonical Camera Session is invalid. " + cameraIssue);
            if (app.CameraSession.OutputPrefabs.Count != 1 || app.CameraSession.OutputPrefabs[0] != outputPrefab ||
                app.StartupCameraAssignments.Count != 1 || app.StartupCameraAssignments[0] != assignment)
                throw new InvalidOperationException("IF-ADR-044 requires the canonical IF-ADR-042 Camera Session and SharedGroup Assignment.");

            if (!assignment.TryBuild(out SessionCameraAssignment built, out string assignmentIssue) ||
                built.OccurrenceMode != CameraOccurrenceMode.SharedGroup ||
                built.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots ||
                built.TargetPolicy != CameraTargetPolicy.MemberActorTargets || built.MemberSlots.Count != 2 ||
                built.MemberSlots[0] != p1.PlayerSlotId || built.MemberSlots[1] != p2.PlayerSlotId ||
                built.Outputs.Count != 1 || built.Outputs[0].OutputId != outputDefinition.OutputId || built.MemberOutputs.Count != 0)
                throw new InvalidOperationException("IF-ADR-044 requires the canonical valid SharedGroup Assignment. " + assignmentIssue);

            ValidateSharedOutputPrefab(outputPrefab, outputEvidence, outputDefinition);
            ValidateSharedAssignmentRig(assignment.RigPrefab, outputEvidence);

            InputControlScheme? keyboardScheme = actions.FindControlScheme("QA.Keyboard");
            if (outputEvidence == null || actions.FindActionMap("Player") == null ||
                !keyboardScheme.HasValue || !keyboardScheme.Value.deviceRequirements.Any(requirement =>
                    requirement.controlPath == "<Keyboard>" && !requirement.isOptional))
                throw new InvalidOperationException("IF-ADR-044 requires the canonical output evidence and Player/QA.Keyboard Input Actions.");

            PlayerInput[] inputs = hostPrefab.GetComponents<PlayerInput>();
            LocalPlayerHostAuthoring[] hosts = hostPrefab.GetComponents<LocalPlayerHostAuthoring>();
            PlayerActorRuntimeHost sourceActor = actorPrefab.GetComponent<PlayerActorRuntimeHost>();
            if (inputs.Length != 1 || hosts.Length != 1 || inputs[0] == null || hosts[0] == null || sourceActor == null ||
                hosts[0].PlayerInput != inputs[0] ||
                hosts[0].PlayerActorRuntimeHostPrefab != sourceActor || inputs[0].actions != actions ||
                inputs[0].defaultActionMap != "Player" || inputs[0].defaultControlScheme != "QA.Keyboard")
                throw new InvalidOperationException("IF-ADR-044 requires the canonical IF-ADR-042 Host, Actor Runtime and PlayerInput composition.");
            if (!sourceActor.TryValidateConfiguration(out string actorIssue))
                throw new InvalidOperationException("IF-ADR-044 canonical Player Actor Runtime is invalid. " + actorIssue);
        }

        private static void ValidateGeneratedRouteScene()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = SceneManager.GetSceneByPath(RouteScene);
                if (!scene.IsValid() || !scene.isLoaded)
                    scene = EditorSceneManager.OpenScene(RouteScene, OpenSceneMode.Additive);
                RouteAsset route = Load<RouteAsset>(RouteAssetPath);
                RouteContentContribution[] contributions = FindComponents<RouteContentContribution>(scene);
                if (contributions.Length != 1 || contributions[0] == null ||
                    contributions[0].Route != route || !contributions[0].HasExplicitLocalContentId ||
                    contributions[0].LocalContentIdText != "qa-if-adr-044.route-content")
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: generated Route scene must contain exactly one explicit contribution bound to its own Route.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void ValidateGeneratedActivityScene()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = SceneManager.GetSceneByPath(ActivityScene);
                if (!scene.IsValid() || !scene.isLoaded)
                    scene = EditorSceneManager.OpenScene(ActivityScene, OpenSceneMode.Additive);
                ActivityAsset activity = Load<ActivityAsset>(ActivityAssetPath);
                ActivityContentContribution[] contributions = FindComponents<ActivityContentContribution>(scene);
                PlayerSessionObserver[] observers = FindComponents<PlayerSessionObserver>(scene);
                if (contributions.Length != 1 || contributions[0] == null ||
                    contributions[0].Activity != activity || !contributions[0].HasExplicitLocalContentId ||
                    contributions[0].LocalContentIdText != "qa-if-adr-044.activity-content.main" ||
                    observers.Length != 1 || observers[0] == null || !observers[0].isActiveAndEnabled ||
                    observers[0].Scope != LocalPlayerProvisioningConsumerScope.Activity)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: generated Activity scene must contain its exact contribution and one enabled Activity-scoped Player Session Observer.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void ValidateBuildSettingsScene(string path)
        {
            EditorBuildSettingsScene[] matches = EditorBuildSettings.scenes
                .Where(entry => entry.path == path).ToArray();
            if (matches.Length != 1 || !matches[0].enabled ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                throw new InvalidOperationException("IF-ADR-044 setup validation failed: required scene '" + path + "' must exist and appear exactly once enabled in Build Settings.");
        }

        private static void ValidateSharedOutputPrefab(GameObject prefab,
            QaIfAdr042SharedGroupEvidence evidence, CameraOutputDefinition expectedDefinition)
        {
            CameraOutputAuthoring[] outputs = prefab.GetComponents<CameraOutputAuthoring>();
            QaIfAdr042OutputProbe[] probes = prefab.GetComponents<QaIfAdr042OutputProbe>();
            if (outputs.Length != 1 || probes.Length != 1 || outputs[0] == null || probes[0] == null)
                throw new InvalidOperationException("IF-ADR-044 canonical SharedGroup Output prefab must contain exactly one Output authoring and one QA evidence probe.");
            if (outputs[0].OutputDefinition != expectedDefinition || outputs[0].UnityCamera == null ||
                outputs[0].CinemachineBrain == null || outputs[0].FallbackCameraRig == null)
                throw new InvalidOperationException("IF-ADR-044 canonical SharedGroup Output prefab does not reference its exact definition, Camera, Brain and fallback rig.");
            if (!outputs[0].UnityCamera.enabled || !outputs[0].UnityCamera.gameObject.activeSelf)
                throw new InvalidOperationException("IF-ADR-044 canonical SharedGroup Output Camera must be enabled on an active GameObject.");
            if (!outputs[0].FallbackCameraRig.TryValidateForApply(out string fallbackIssue))
                throw new InvalidOperationException("IF-ADR-044 canonical SharedGroup Output fallback is not ready. " + fallbackIssue);
            if (!outputs[0].TryValidateDefinition(out string issue))
                throw new InvalidOperationException("IF-ADR-044 canonical SharedGroup Output definition is invalid. " + issue);

            SerializedObject probe = new SerializedObject(probes[0]);
            probe.Update();
            RequireReference(probe, "evidence", evidence);
        }

        private static void ValidateSharedAssignmentRig(GameObject prefab,
            QaIfAdr042SharedGroupEvidence evidence)
        {
            if (prefab == null)
                throw new InvalidOperationException("IF-ADR-044 canonical SharedGroup Assignment has no Rig prefab.");
            CameraRigComposer[] composers = prefab.GetComponents<CameraRigComposer>();
            QaIfAdr042OccurrenceProbe[] probes = prefab.GetComponents<QaIfAdr042OccurrenceProbe>();
            if (composers.Length != 1 || probes.Length != 1 || composers[0] == null || probes[0] == null ||
                composers[0].BehaviorDefinition == null ||
                composers[0].FrameworkOwnedGroupTargetGroup == null || composers[0].FrameworkOwnedGroupFraming == null)
                throw new InvalidOperationException("IF-ADR-044 canonical SharedGroup Rig must contain one valid Composer, Group TargetGroup and Group Framing surface, plus one QA occurrence probe.");
            if (!composers[0].TryValidateForApply(out string issue))
                throw new InvalidOperationException("IF-ADR-044 canonical SharedGroup Rig is invalid. " + issue);

            SerializedObject probe = new SerializedObject(probes[0]);
            probe.Update();
            RequireReference(probe, "evidence", evidence);
        }

        private static void ValidateCanonicalActivityScene()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = default;
            try
            {
                Scene alreadyLoaded = SceneManager.GetSceneByPath(BaseActivityScene);
                scene = alreadyLoaded.IsValid() && alreadyLoaded.isLoaded
                    ? alreadyLoaded
                    : EditorSceneManager.OpenScene(BaseActivityScene, OpenSceneMode.Additive);

                ActivityAsset activity = Load<ActivityAsset>(Base + "/Settings/Activity_QaIfAdr042_SharedGroup.asset");
                ActivityContentProfileAsset profile = Load<ActivityContentProfileAsset>(Base + "/Settings/ActivityContentProfile_QaIfAdr042.asset");
                ActivityContentContribution[] contributions = FindComponents<ActivityContentContribution>(scene);
                PlayerSessionObserver[] observers = FindComponents<PlayerSessionObserver>(scene);
                if (activity.ActivityContentProfile != profile || profile.SceneCount != 1 ||
                    profile.Scenes[0].ScenePath != BaseActivityScene ||
                    profile.Scenes[0].Requiredness != FrameworkContentRequiredness.Required ||
                    profile.Scenes[0].LoadMode != ActivityContentSceneLoadMode.Additive ||
                    profile.Scenes[0].ReleasePolicy != ActivityContentReleasePolicy.ReleaseOnActivityChange ||
                    contributions.Length != 1 || contributions[0] == null || contributions[0].Activity != activity ||
                    !contributions[0].HasExplicitLocalContentId ||
                    observers.Length != 1 || observers[0] == null || !observers[0].isActiveAndEnabled ||
                    observers[0].Scope != LocalPlayerProvisioningConsumerScope.Activity)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: canonical Activity scene must contain its exact Activity contribution and one enabled Activity-scoped Player Session Observer.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void ValidateCanonicalRouteScene()
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = default;
            try
            {
                Scene alreadyLoaded = SceneManager.GetSceneByPath(BaseRouteScene);
                scene = alreadyLoaded.IsValid() && alreadyLoaded.isLoaded
                    ? alreadyLoaded
                    : EditorSceneManager.OpenScene(BaseRouteScene, OpenSceneMode.Additive);

                RouteAsset route = Load<RouteAsset>(Base + "/Settings/Route_QaIfAdr042.asset");
                RouteContentContribution[] contributions = FindComponents<RouteContentContribution>(scene);
                if (contributions.Length != 1 || contributions[0] == null || contributions[0].Route != route ||
                    !contributions[0].HasExplicitLocalContentId)
                    throw new InvalidOperationException("IF-ADR-044 setup validation failed: canonical Route scene must contain exactly one explicit contribution bound to the startup Route.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            var results = new System.Collections.Generic.List<T>();
            foreach (GameObject root in scene.GetRootGameObjects())
                results.AddRange(root.GetComponentsInChildren<T>(true));
            return results.ToArray();
        }

        private static void RequireReference(SerializedObject serialized, string path, Object expected)
        {
            SerializedProperty property = RequireProperty(serialized, path);
            if (property.objectReferenceValue != expected)
                throw new InvalidOperationException("IF-ADR-044 setup validation failed: Serialized authoring reference differs at '" + path + "'.");
        }

        private static SerializedProperty RequireProperty(SerializedObject serialized, string path)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property == null)
                throw new InvalidOperationException("IF-ADR-044 setup could not find the serialized authoring field '" + path + "'.");
            return property;
        }

        private static SerializedProperty RequireProperty(SerializedProperty parent, string path)
        {
            SerializedProperty property = parent.FindPropertyRelative(path);
            if (property == null)
                throw new InvalidOperationException("IF-ADR-044 setup could not find the serialized authoring field '" + parent.propertyPath + "." + path + "'.");
            return property;
        }

        private static void SetEnum(SerializedProperty property, string enumName)
        {
            for (int index = 0; index < property.enumNames.Length; index++)
            {
                if (property.enumNames[index] != enumName) continue;
                property.enumValueIndex = index;
                return;
            }
            throw new InvalidOperationException("IF-ADR-044 setup enum value '" + enumName + "' is unavailable for '" + property.propertyPath + "'.");
        }

        private static void EnsureParentFolder(string assetPath)
        {
            string parent = System.IO.Path.GetDirectoryName(assetPath).Replace('\\', '/');
            string[] parts = parent.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static T Load<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required QA authoring asset is missing or invalid: " + path);
            return asset;
        }

        private static void Set(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = CreateSerializedObject(target, propertyName);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException("Serialized authoring field was not found: " + propertyName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Set(Object target, string propertyName, int value)
        {
            SerializedObject serialized = CreateSerializedObject(target, propertyName);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException("Serialized authoring field was not found: " + propertyName);
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Set(Object target, string propertyName, string value)
        {
            SerializedObject serialized = CreateSerializedObject(target, propertyName);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException("Serialized authoring field was not found: " + propertyName);
            property.stringValue = value ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static SerializedObject CreateSerializedObject(Object target, string propertyName)
        {
            if (target == null)
                throw new InvalidOperationException("Cannot author serialized field '" + propertyName + "' because its target is null.");
            return new SerializedObject(target);
        }
    }
}

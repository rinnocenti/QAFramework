using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.Editor.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using Immersive.Framework.RouteLifecycle;
using Immersive.QaFramework.PlayerSession;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Immersive.QaFramework.PlayerSession.Editor
{
    public static class QaPlayerSessionMultiplayerOwnershipSetup
    {
        private const string Root = "Assets/QA-IF-ADR-PLAYER-SESSION";
        private const string Settings = Root + "/Settings";
        private const string Players = Root + "/Players";
        private const string Camera = Root + "/Camera";
        private const string Scenes = Root + "/Scenes";
        private const string SessionPath = Settings + "/PlayerSession_QaPlayerMultiplayerOwnership.asset";
        private const string SlotP1Path = Settings + "/PlayerSlot_QaPlayerMultiplayerOwnership_P1.asset";
        private const string SlotP2Path = Settings + "/PlayerSlot_QaPlayerMultiplayerOwnership_P2.asset";
        private const string ActorPath = Players + "/Actor_QaPlayerMultiplayerOwnership_Default.asset";
        private const string ApplicationPath = Settings + "/GameApplication_QaPlayerMultiplayerOwnership.asset";
        private const string RoutePath = Settings + "/Route_QaPlayerMultiplayerOwnership.asset";
        private const string ActivityPath = Settings + "/Activity_QaPlayerMultiplayerOwnership.asset";
        private const string ActivityProfilePath = Settings + "/ActivityContentProfile_QaPlayerMultiplayerOwnership.asset";
        private const string ActionsPath = Players + "/InputActions_QaPlayerMultiplayerOwnership.asset";
        private const string PauseActionReferencePath = Players + "/PauseAction_QaPlayerMultiplayerOwnership.asset";
        private const string ActorRuntimePath = Players + "/PF_PlayerActorRuntime_QaPlayerMultiplayerOwnership.prefab";
        private const string HostPath = Players + "/PF_LocalPlayerHost_QaPlayerMultiplayerOwnership.prefab";
        private const string CameraBehaviorPath = Camera + "/CameraBehavior_QaPlayerMultiplayerOwnership_Fixed.asset";
        private const string CameraDefinitionPath = Camera + "/CameraOutput_QaPlayerMultiplayerOwnership.asset";
        private const string CameraPrefabPath = Camera + "/PF_CameraOutput_QaPlayerMultiplayerOwnership_Technical.prefab";
        private const string RouteScenePath = Scenes + "/QA_PlayerMultiplayerOwnership_Route.unity";
        private const string ActivityScenePath = Scenes + "/QA_PlayerMultiplayerOwnership_Activity.unity";
        private const string PersistentScenePath = Scenes + "/QA_PlayerMultiplayerOwnership_Persistent.unity";
        private const string ControlScheme = "QA.Keyboard";

        [MenuItem("Immersive Framework/QA/Player/Multiplayer/Configure Local Multiplayer Ownership Slice")]
        public static void Configure()
        {
            EnsureNoModifiedOpenScenes();
            EnsureFolders();
            PreflightGeneratedPaths();
            ActorProfile actor = CreateActorProfile();
            PlayerSlotProfile slotP1 = CreateSlot(actor, 1);
            PlayerSlotProfile slotP2 = CreateSlot(actor, 2);
            PlayerSessionProfile session = CreateSession(slotP1, slotP2);
            InputActionAsset actions = CreateInputActions();
            InputActionReference pauseAction = CreatePauseActionReference(actions);
            GameObject actorRuntime = CreateActorRuntime();
            GameObject host = CreateHost(actorRuntime, actions, pauseAction);
            FixedCameraRigBehaviorDefinition behavior = GetOrCreate<FixedCameraRigBehaviorDefinition>(CameraBehaviorPath);
            if (!behavior.TryValidate(out string issue)) throw new InvalidOperationException("Fixed technical behavior invalid. " + issue);
            CameraOutputDefinition outputDefinition = CreateCameraDefinition();
            GameObject output = CreateCameraOutput(outputDefinition, behavior);
            ActivityContentProfileAsset activityProfile = CreateActivityProfile();
            ActivityAsset activity = CreateActivity(activityProfile, slotP1, slotP2);
            RouteAsset route = CreateRoute(activity);

            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                CreateRouteScene(route);
                CreateActivityScene(activity, slotP1, slotP2, actor);
                CreatePersistentScene(host);
                GameApplicationAsset application = CreateApplication(route, session, output);
                BindPersistentScene(application);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AddScenesToBuildSettings();
                ValidateGeneratedBaseline();
                Debug.Log("[QA-PLAYER-MULTIPLAYER-OWNERSHIP] setup='Configured' baseline='SerializedPostflight' assets='updated-in-place' slots='P1,P2' provisioning='ManagerProvisioned' actorResolution='ResolveConfiguredDefault' activityRequirement='LogicalActorsPrepared' cameraOutput='technical-bootstrap-only' sessionPause='valid-unbound-technical-binding' scenes='route,activity,persistent'. Run Configure a second time to confirm idempotency, select GameApplication_QaPlayerMultiplayerOwnership in ImmersiveFrameworkSettings, open the Persistent scene, and enter a fresh Play Mode session.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void EnsureNoModifiedOpenScenes()
        {
            for (int index = 0; index < SceneManager.sceneCount; index++)
            {
                Scene scene = SceneManager.GetSceneAt(index);
                if (scene.IsValid() && scene.isLoaded && (scene.isDirty || string.IsNullOrEmpty(scene.path)))
                    throw new InvalidOperationException("Save and close modified or untitled scenes before configuring the Slice 3 fixture. No scene changes were made.");
            }
        }
        private static void PreflightGeneratedPaths()
        {
            RequireExpectedAssetType<ActorProfile>(ActorPath);
            RequireExpectedAssetType<PlayerSlotProfile>(SlotP1Path);
            RequireExpectedAssetType<PlayerSlotProfile>(SlotP2Path);
            RequireExpectedAssetType<PlayerSessionProfile>(SessionPath);
            RequireExpectedAssetType<InputActionAsset>(ActionsPath);
            RequireExpectedAssetType<GameObject>(ActorRuntimePath);
            RequireExpectedAssetType<GameObject>(HostPath);
            RequireExpectedAssetType<FixedCameraRigBehaviorDefinition>(CameraBehaviorPath);
            RequireExpectedAssetType<CameraOutputDefinition>(CameraDefinitionPath);
            RequireExpectedAssetType<GameObject>(CameraPrefabPath);
            RequireExpectedAssetType<ActivityContentProfileAsset>(ActivityProfilePath);
            RequireExpectedAssetType<ActivityAsset>(ActivityPath);
            RequireExpectedAssetType<RouteAsset>(RoutePath);
            RequireExpectedAssetType<GameApplicationAsset>(ApplicationPath);
            RequireExpectedAssetType<SceneAsset>(RouteScenePath);
            RequireExpectedAssetType<SceneAsset>(ActivityScenePath);
            RequireExpectedAssetType<SceneAsset>(PersistentScenePath);
        }

        private static void RequireExpectedAssetType<T>(string path) where T : Object
        {
            if (!File.Exists(path)) return;
            if (AssetDatabase.LoadAssetAtPath<T>(path) == null)
                throw new InvalidOperationException("Refusing to overwrite a QA path with unexpected content/type: " + path);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "QA-IF-ADR-PLAYER-SESSION");
            foreach (string child in new[] { "Settings", "Players", "Camera", "Scenes", "Scripts", "Editor" }) EnsureFolder(Root, child);
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (File.Exists(path)) throw new InvalidOperationException("QA output path has unexpected asset type: " + path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static ActorProfile CreateActorProfile()
        {
            ActorProfile profile = GetOrCreate<ActorProfile>(ActorPath);
            Set(profile, "actorProfileId", "qa.player.multiplayer.ownership.default");
            Set(profile, "displayName", "QA Default Player Actor");
            SetEnum(profile, "actorKind", "Player");
            SetEnum(profile, "actorRole", "Protagonist");
            return profile;
        }

        private static PlayerSlotProfile CreateSlot(ActorProfile actor, int slotNumber)
        {
            string slotPath = slotNumber == 1 ? SlotP1Path : SlotP2Path;
            PlayerSlotProfile profile = GetOrCreate<PlayerSlotProfile>(slotPath);
            Set(profile, "playerSlotId", "player." + slotNumber);
            Set(profile, "displayName", "Player " + slotNumber);
            Set(profile, "defaultActorProfile", actor);
            return profile;
        }

        private static PlayerSessionProfile CreateSession(PlayerSlotProfile slotP1, PlayerSlotProfile slotP2)
        {
            PlayerSessionProfile profile = GetOrCreate<PlayerSessionProfile>(SessionPath);
            SetObjectArray(profile, "supportedSlots", slotP1, slotP2);
            Set(profile, "initialJoiningOpen", false);
            SetEnum(profile, "hostProvisioning", "ManagerProvisioned");
            SetEnum(profile, "actorResolutionPolicy", "ResolveConfiguredDefault");
            if (!profile.TryValidate(out string issue)) throw new InvalidOperationException("Player Session Profile invalid. " + issue);
            return profile;
        }

        private static InputActionAsset CreateInputActions()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (asset == null)
            {
                if (File.Exists(ActionsPath)) throw new InvalidOperationException("Input Actions path has unexpected type: " + ActionsPath);
                asset = ScriptableObject.CreateInstance<InputActionAsset>();
                AssetDatabase.CreateAsset(asset, ActionsPath);
            }
            while (asset.actionMaps.Count > 0) asset.RemoveActionMap(asset.actionMaps[0]);
            while (asset.controlSchemes.Count > 0) asset.RemoveControlScheme(asset.controlSchemes[0].name);
            InputActionMap map = asset.AddActionMap("Player");
            InputAction move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w", ControlScheme)
                .With("Down", "<Keyboard>/s", ControlScheme).With("Left", "<Keyboard>/a", ControlScheme)
                .With("Right", "<Keyboard>/d", ControlScheme);
            InputActionMap globalMap = asset.AddActionMap("Global");
            globalMap.AddAction("Pause", InputActionType.Button);
            asset.AddControlScheme(ControlScheme).WithBindingGroup(ControlScheme).WithRequiredDevice("<Keyboard>");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        }

        private static InputActionReference CreatePauseActionReference(InputActionAsset actions)
        {
            InputActionMap globalMap = actions.FindActionMap("Global");
            InputAction pauseAction = globalMap != null ? globalMap.FindAction("Pause") : null;
            if (pauseAction == null || pauseAction.type != InputActionType.Button || pauseAction.bindings.Count != 0)
                throw new InvalidOperationException("QA technical Global/Pause must exist as an unbound Button action; this slice does not author functional Pause input.");

            string assetName = Path.GetFileNameWithoutExtension(PauseActionReferencePath);
            InputActionReference reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(PauseActionReferencePath);
            if (reference == null)
            {
                if (File.Exists(PauseActionReferencePath))
                    throw new InvalidOperationException("Pause InputActionReference path has unexpected type: " + PauseActionReferencePath);
                reference = InputActionReference.Create(pauseAction);
                if (reference == null) throw new InvalidOperationException("QA technical Pause InputActionReference could not be created.");
                reference.name = assetName;
                AssetDatabase.CreateAsset(reference, PauseActionReferencePath);
            }
            else
            {
                bool changed = false;
                if (reference.action != pauseAction)
                {
                    reference.Set(pauseAction);
                    changed = true;
                }
                if (reference.name != assetName)
                {
                    reference.name = assetName;
                    changed = true;
                }
                if (changed) EditorUtility.SetDirty(reference);
            }

            AssetDatabase.SaveAssets();
            reference = AssetDatabase.LoadAssetAtPath<InputActionReference>(PauseActionReferencePath);
            if (reference == null || reference.name != assetName || reference.action == null || reference.action.id != pauseAction.id ||
                reference.action.actionMap == null || reference.action.actionMap.id != globalMap.id ||
                reference.action.actionMap.asset != actions)
                throw new InvalidOperationException("QA Pause InputActionReference does not resolve to the exact generated Global/Pause action.");
            return reference;
        }

        private static GameObject CreateActorRuntime()
        {
            GameObject root = new GameObject("QA Player Actor Runtime");
            try
            {
                PlayerActorDeclaration declaration = root.AddComponent<PlayerActorDeclaration>();
                Set(declaration, "actorId", "qa.player.multiplayer.ownership.actor.runtime");
                Set(declaration, "displayName", "QA Player Actor Runtime");
                Set(declaration, "reason", "qa.player.multiplayer.ownership.slice1");
                PlayerActorRuntimeHost host = root.AddComponent<PlayerActorRuntimeHost>();
                Set(host, "playerActorDeclaration", declaration);
                if (!host.TryValidateConfiguration(out string issue)) throw new InvalidOperationException("Actor Runtime Host invalid. " + issue);
                return SavePrefab(root, ActorRuntimePath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject CreateHost(GameObject actorRuntime, InputActionAsset actions, InputActionReference pauseAction)
        {
            GameObject root = new GameObject("QA Local Player Host");
            try
            {
                PlayerInput input = root.AddComponent<PlayerInput>();
                Transform mount = new GameObject("Actor Mount").transform;
                mount.SetParent(root.transform, false);
                LocalPlayerHostAuthoring host = root.AddComponent<LocalPlayerHostAuthoring>();
                Set(host, "playerInput", input);
                Set(host, "actorMount", mount);
                Set(host, "playerActorRuntimeHostPrefab", actorRuntime.GetComponent<PlayerActorRuntimeHost>());
                UnityPlayerInputGateAdapter gate = root.AddComponent<UnityPlayerInputGateAdapter>();
                PlayerPauseInput pauseBinding = root.AddComponent<PlayerPauseInput>();
                Set(pauseBinding, "pauseAction", pauseAction);
                ConfigureGateAdapter(gate, input, actions);
                GameObject saved = SavePrefab(root, HostPath);
                ConfigurePlayerInput(saved, actions);
                return AssetDatabase.LoadAssetAtPath<GameObject>(HostPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void ConfigureGateAdapter(UnityPlayerInputGateAdapter adapter, PlayerInput input, InputActionAsset actions)
        {
            InputActionMap gameplayMap = actions.FindActionMap("Player");
            if (gameplayMap == null) throw new InvalidOperationException("QA Local Player Host requires the explicit Player Action Map.");
            SerializedObject so = new SerializedObject(adapter);
            so.Update();
            SerializedProperty adapterInput = Require(so, "playerInput");
            SerializedProperty blockOnInputAcceptance = Require(so, "blockOnInputAcceptance");
            SerializedProperty blockOnGameplayAction = Require(so, "blockOnGameplayAction");
            SerializedProperty map = Require(so, "gameplayActionMap");
            SerializedProperty actionMapAsset = Require(map, "actionAsset");
            SerializedProperty actionMapId = Require(map, "actionMapId");
            SerializedProperty cachedActionMapName = Require(map, "cachedActionMapName");
            adapterInput.objectReferenceValue = input;
            blockOnInputAcceptance.boolValue = false;
            blockOnGameplayAction.boolValue = true;
            actionMapAsset.objectReferenceValue = actions;
            actionMapId.stringValue = gameplayMap.id.ToString("D");
            cachedActionMapName.stringValue = gameplayMap.name;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePlayerInput(GameObject prefab, InputActionAsset actions)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(HostPath);
            try
            {
                PlayerInput input = contents.GetComponent<PlayerInput>();
                Set(input, "m_Actions", actions);
                Set(input, "m_DefaultControlScheme", ControlScheme);
                Set(input, "m_DefaultActionMap", "Player");
                PrefabUtility.SaveAsPrefabAsset(contents, HostPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static CameraOutputDefinition CreateCameraDefinition()
        {
            CameraOutputDefinition definition = GetOrCreate<CameraOutputDefinition>(CameraDefinitionPath);
            Set(definition, "stableId", "f1f0e0a3d6234db6b711a1fa9290a101");
            Set(definition, "description", "Technical bootstrap output for Player Session QA; no gameplay camera claim.");
            if (!definition.HasValidId) throw new InvalidOperationException("Technical Camera Output definition has no valid stable identity.");
            return definition;
        }

        private static GameObject CreateCameraOutput(CameraOutputDefinition definition, FixedCameraRigBehaviorDefinition behavior)
        {
            GameObject root = new GameObject("QA Technical Camera Output");
            try
            {
                UnityEngine.Camera camera = root.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
                brain.ChannelMask = (OutputChannels)1;
                GameObject rigObject = new GameObject("Persistent Fallback Rig");
                rigObject.transform.SetParent(root.transform, false);
                CinemachineCamera virtualCamera = rigObject.AddComponent<CinemachineCamera>();
                virtualCamera.OutputChannel = (OutputChannels)1;
                CameraRigComposer composer = rigObject.AddComponent<CameraRigComposer>();
                Set(composer, "behaviorDefinition", behavior);
                Set(composer, "cinemachineCamera", virtualCamera);
                CameraRigComposerApplyRebuildResult materialized = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
                if (!materialized.Succeeded) throw new InvalidOperationException("Technical fallback rig failed materialization: " + materialized.BlockingIssue);
                CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
                Set(output, "outputDefinition", definition);
                Set(output, "unityCamera", camera);
                Set(output, "cinemachineBrain", brain);
                Set(output, "fallbackCameraRig", composer);
                Set(output, "initializeOnAwake", true);
                if (!output.TryValidateDefinition(out string issue) || !composer.TryValidateForApply(out issue))
                    throw new InvalidOperationException("Technical Camera Output invalid. " + issue);
                GameObject prefab = SavePrefab(root, CameraPrefabPath);
                GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
                if (saved == null || !saved.GetComponent<CameraOutputAuthoring>().TryValidateDefinition(out issue))
                    throw new InvalidOperationException("Persisted technical Camera Output failed validation. " + issue);
                return prefab;
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static ActivityContentProfileAsset CreateActivityProfile()
        {
            ActivityContentProfileAsset profile = GetOrCreate<ActivityContentProfileAsset>(ActivityProfilePath);
            SerializedObject so = new SerializedObject(profile); so.Update();
            Require(so, "profileId").stringValue = "qa.player.multiplayer.ownership.activity-content";
            SerializedProperty scenes = Require(so, "scenes"); scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            Require(entry, "contentId").stringValue = "qa.player.multiplayer.ownership.activity.main";
            Require(entry, "scenePath").stringValue = ActivityScenePath;
            Require(entry, "sceneName").stringValue = Path.GetFileNameWithoutExtension(ActivityScenePath);
            SetEnum(Require(entry, "requiredness"), "Required");
            SetEnum(Require(entry, "loadMode"), "Additive");
            SetEnum(Require(entry, "releasePolicy"), "ReleaseOnActivityChange");
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateActivity(ActivityContentProfileAsset profile, PlayerSlotProfile slotP1, PlayerSlotProfile slotP2)
        {
            ActivityAsset activity = GetOrCreate<ActivityAsset>(ActivityPath);
            Set(activity, "activityId", "qa.player.multiplayer.ownership.activity");
            Set(activity, "activityName", "QA Player Multiplayer Ownership");
            Set(activity, "activityContentProfile", profile);
            SetEnum(activity, "playerParticipationProjectionMode", "ExplicitSlots");
            SetEnum(activity, "playerParticipationZeroParticipantPolicy", "Allowed");
            SetEnum(activity, "playerParticipationRequirementLevel", "LogicalActorsPrepared");
            SetObjectArray(activity, "playerParticipationExplicitSlotProfiles", slotP1, slotP2);
            return activity;
        }

        private static RouteAsset CreateRoute(ActivityAsset activity)
        {
            RouteAsset route = GetOrCreate<RouteAsset>(RoutePath);
            Set(route, "routeId", "qa.player.multiplayer.ownership.start");
            Set(route, "routeName", "QA Player Multiplayer Ownership");
            Set(route, "primaryScenePath", RouteScenePath);
            Set(route, "primarySceneName", Path.GetFileNameWithoutExtension(RouteScenePath));
            Set(route, "startupActivity", activity);
            return route;
        }

        private static GameApplicationAsset CreateApplication(RouteAsset route, PlayerSessionProfile session, GameObject output)
        {
            route = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
            session = AssetDatabase.LoadAssetAtPath<PlayerSessionProfile>(SessionPath);
            output = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            GameApplicationAsset app = GetOrCreate<GameApplicationAsset>(ApplicationPath);
            Set(app, "applicationName", "QA Player Multiplayer Ownership");
            Set(app, "playerSessionEnabled", true);
            Set(app, "defaultPlayerSessionProfile", session);
            SetEnum(app, "playerActorSelectionDuplicatePolicy", "AllowDuplicates");
            SerializedObject so = new SerializedObject(app); so.Update();
            Require(so, "startupRoute").objectReferenceValue = route;
            SerializedProperty outputs = Require(so, "cameraSession.outputPrefabs"); outputs.arraySize = 1;
            outputs.GetArrayElementAtIndex(0).objectReferenceValue = output;
            Require(so, "startupCameraAssignments").arraySize = 0;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(app);
            return app;
        }

        private static void CreateRouteScene(RouteAsset route)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            route = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
            GameObject root = new GameObject("QA Player Session Route Content");
            RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
            Set(contribution, "route", route); Set(contribution, "localContentId", "qa.player.multiplayer.ownership.route-content");
            SaveScene(scene, RouteScenePath);
        }

        private static void CreateActivityScene(ActivityAsset activity, PlayerSlotProfile slotP1, PlayerSlotProfile slotP2, ActorProfile actor)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);
            slotP1 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP1Path);
            slotP2 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP2Path);
            actor = AssetDatabase.LoadAssetAtPath<ActorProfile>(ActorPath);
            GameObject root = new GameObject("QA Player Session Activity Content");
            ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
            Set(contribution, "activity", activity); Set(contribution, "localContentId", "qa.player.multiplayer.ownership.activity.main");
            PlayerSessionObserver observer = root.AddComponent<PlayerSessionObserver>();
            SetEnum(observer, "scope", "Activity");
            PlayerSessionJoinTrigger join = root.AddComponent<PlayerSessionJoinTrigger>();
            SetEnum(join, "scope", "Activity");
            Set(join, "controlScheme", ControlScheme);
            QaPlayerSessionMultiplayerOwnershipScenario scenario = root.AddComponent<QaPlayerSessionMultiplayerOwnershipScenario>();
            Set(scenario, "sessionObserver", observer); Set(scenario, "joinTrigger", join);
            Set(scenario, "expectedP1", slotP1); Set(scenario, "expectedP2", slotP2); Set(scenario, "expectedDefaultActor", actor);
            SaveScene(scene, ActivityScenePath);
        }

        private static void CreatePersistentScene(GameObject hostPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            hostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HostPath);
            GameObject root = new GameObject("QA Player Session Manager Provisioning");
            PlayerInputManager manager = root.AddComponent<PlayerInputManager>();
            manager.playerPrefab = hostPrefab;
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            manager.splitScreen = false;
            Set(manager, "m_MaxPlayerCount", 2);
            LocalPlayerProvisioningAuthoring provisioning = root.AddComponent<LocalPlayerProvisioningAuthoring>();
            Set(provisioning, "playerInputManager", manager); Set(provisioning, "localPlayerHostPrefab", hostPrefab);
            LocalPlayerProvisioningEndpointRegistration registration = root.AddComponent<LocalPlayerProvisioningEndpointRegistration>();
            Set(registration, "provisioningAuthoring", provisioning);
            SaveScene(scene, PersistentScenePath);
        }

        private static void BindPersistentScene(GameApplicationAsset app)
        {
            SceneAsset persistent = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScenePath);
            if (persistent == null)
                throw new InvalidOperationException("Persistent Content scene asset could not be loaded: " + PersistentScenePath);
            SerializedObject so = new SerializedObject(app); so.Update();
            SerializedProperty composition = Require(so, "persistentContent");
            Require(composition, "scenePath").stringValue = AssetDatabase.GetAssetPath(persistent);
            Require(composition, "sceneName").stringValue = persistent.name;
            Require(composition, "containerScene").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(app);
        }

        private static bool HasCanonicalPersistentContent(GameApplicationAsset app)
        {
            if (app == null || app.PersistentContent == null || app.PersistentContent.ContainerScene != null ||
                !app.PersistentContent.IsComplete || app.PersistentContent.ContainerScenePath != PersistentScenePath)
                return false;

            SceneAsset expectedScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScenePath);
            if (expectedScene == null || app.PersistentContent.ContainerSceneName != expectedScene.name)
                return false;

            SerializedObject serialized = new SerializedObject(app);
            serialized.Update();
            SerializedProperty composition = Require(serialized, "persistentContent");
            return Require(composition, "scenePath").stringValue == PersistentScenePath &&
                Require(composition, "sceneName").stringValue == expectedScene.name &&
                Require(composition, "containerScene").objectReferenceValue == null;
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Could not persist QA scene: " + path);
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (saved == null) throw new InvalidOperationException("Could not persist QA prefab: " + path);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (string path in new[] { RouteScenePath, ActivityScenePath, PersistentScenePath })
            {
                scenes.RemoveAll(scene => scene.path == path);
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ValidateGeneratedBaseline()
        {
            AssetDatabase.SaveAssets();
            GameApplicationAsset app = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(ApplicationPath);
            PlayerSessionProfile session = AssetDatabase.LoadAssetAtPath<PlayerSessionProfile>(SessionPath);
            PlayerSlotProfile slotP1 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP1Path);
            PlayerSlotProfile slotP2 = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP2Path);
            ActorProfile actor = AssetDatabase.LoadAssetAtPath<ActorProfile>(ActorPath);
            RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
            ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);
            ActivityContentProfileAsset profile = AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(ActivityProfilePath);
            GameObject output = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath);
            GameObject hostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HostPath);
            PlayerActorRuntimeHost actorRuntime = AssetDatabase.LoadAssetAtPath<GameObject>(ActorRuntimePath)?.GetComponent<PlayerActorRuntimeHost>();
            if (app == null || session == null || slotP1 == null || slotP2 == null || actor == null || route == null || activity == null || profile == null || output == null || hostPrefab == null || actorRuntime == null)
                throw new InvalidOperationException("Generated QA baseline is missing a typed asset.");
            if (!session.TryValidate(out string issue) || session.SupportedSlotCount != 2 || session.SupportedSlots[0] != slotP1 || session.SupportedSlots[1] != slotP2 ||
                session.HostProvisioning != PlayerHostProvisioningMode.ManagerProvisioned || session.ActorResolutionPolicy != PlayerActorResolutionPolicy.ResolveConfiguredDefault ||
                slotP1.DefaultActorProfile != actor || slotP2.DefaultActorProfile != actor || !actor.TryGetActorProfileId(out _, out issue) ||
                app.StartupRoute != route || !app.PlayerSessionEnabled || app.DefaultPlayerSessionProfile != session ||
                !app.HasDefinedPlayerActorSelectionDuplicatePolicy || app.PlayerActorSelectionDuplicatePolicy != PlayerActorSelectionDuplicatePolicy.AllowDuplicates || !HasCanonicalPersistentContent(app) ||
                app.CameraSession == null || !app.CameraSession.HasOutputs || app.CameraSession.OutputPrefabs.Count != 1 || app.CameraSession.OutputPrefabs[0] != output ||
                !app.CameraSession.TryValidate(out issue) || route.StartupActivity != activity || !route.HasPrimaryScene || route.PrimaryScenePath != RouteScenePath ||
                activity.ActivityContentProfile != profile || activity.PlayerParticipationProjectionMode != ActivityParticipationProjectionMode.ExplicitSlots ||
                activity.PlayerParticipationZeroParticipantPolicy != ActivityParticipationZeroParticipantPolicy.Allowed ||
                activity.PlayerParticipationRequirementLevel != PlayerParticipationRequirementLevel.LogicalActorsPrepared ||
                activity.PlayerParticipationExplicitSlotProfiles.Count != 2 || activity.PlayerParticipationExplicitSlotProfiles[0] != slotP1 || activity.PlayerParticipationExplicitSlotProfiles[1] != slotP2 ||
                !actorRuntime.TryValidateConfiguration(out issue))
                throw new InvalidOperationException("Generated QA application/session/flow baseline is invalid. " + issue);
            PlayerInput input = hostPrefab.GetComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = hostPrefab.GetComponent<LocalPlayerHostAuthoring>();
            UnityPlayerInputGateAdapter[] gates = hostPrefab.GetComponents<UnityPlayerInputGateAdapter>();
            PlayerPauseInput[] pauseBindings = hostPrefab.GetComponents<PlayerPauseInput>();
            InputActionReference pauseAction = AssetDatabase.LoadAssetAtPath<InputActionReference>(PauseActionReferencePath);
            if (input == null || host == null || host.PlayerInput != input || host.PlayerActorRuntimeHostPrefab != actorRuntime ||
                host.ActorMount == null || !host.TryValidateConfiguration(out issue) || input.actions == null ||
                input.actions.FindAction("Player/Move") == null || input.defaultControlScheme != ControlScheme ||
                gates.Length != 1 || gates[0].PlayerInput != input || !gates[0].TryValidateAuthoring(out issue) ||
                pauseBindings.Length != 1 || pauseAction == null || pauseBindings[0].PauseAction != pauseAction ||
                !pauseBindings[0].TryValidateAuthoring(out issue) || input.actions.FindAction("Global/Pause") == null ||
                input.actions.FindAction("Global/Pause").bindings.Count != 0)
                throw new InvalidOperationException("Generated Manager-Provisioned Host baseline or required Session Pause binding is invalid. " + issue);
            foreach (string path in new[] { RouteScenePath, ActivityScenePath, PersistentScenePath })
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null || !IsSceneInBuildSettings(path))
                    throw new InvalidOperationException("Generated scene missing or not enabled in Editor Build Settings: " + path);
            }

            ValidatePersistedSceneBindings();
        }

        private static void ValidatePersistedSceneBindings()
        {
            string issue;
            Scene routeScene = OpenForValidation(RouteScenePath);
            try
            {
                RouteContentContribution[] contributions = Roots(routeScene).SelectMany(root => root.GetComponentsInChildren<RouteContentContribution>(true)).ToArray();
                RouteAsset persistedRoute = AssetDatabase.LoadAssetAtPath<RouteAsset>(RoutePath);
                if (contributions.Length != 1 || contributions[0].Route != persistedRoute || !contributions[0].HasExplicitLocalContentId)
                    throw new InvalidOperationException("Persisted Route scene contribution does not bind exactly to the QA Route.");
            }
            finally { CloseAfterValidation(routeScene, RouteScenePath); }

            Scene activityScene = OpenForValidation(ActivityScenePath);
            try
            {
                ActivityContentContribution[] contributions = Roots(activityScene).SelectMany(root => root.GetComponentsInChildren<ActivityContentContribution>(true)).ToArray();
                PlayerSessionObserver[] observers = Roots(activityScene).SelectMany(root => root.GetComponentsInChildren<PlayerSessionObserver>(true)).ToArray();
                PlayerSessionJoinTrigger[] joins = Roots(activityScene).SelectMany(root => root.GetComponentsInChildren<PlayerSessionJoinTrigger>(true)).ToArray();
                QaPlayerSessionMultiplayerOwnershipScenario[] scenarios = Roots(activityScene).SelectMany(root => root.GetComponentsInChildren<QaPlayerSessionMultiplayerOwnershipScenario>(true)).ToArray();
                ActivityAsset persistedActivity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);
                ActivityContentProfileAsset persistedProfile = AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(ActivityProfilePath);
                if (persistedProfile == null)
                    throw new InvalidOperationException("Persisted Activity Content Profile asset is missing or has the wrong type.");
                if (contributions.Length != 1 || contributions[0].Activity != persistedActivity || !contributions[0].HasExplicitLocalContentId ||
                    observers.Length != 1 || observers[0].Scope != LocalPlayerProvisioningConsumerScope.Activity ||
                    joins.Length != 1 || joins[0].Scope != LocalPlayerProvisioningConsumerScope.Activity ||
                    scenarios.Length != 1 || !scenarios[0].enabled || !scenarios[0].gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Persisted Activity scene has an invalid contribution/Observer/Join/scenario cardinality or binding.");
                SerializedObject profile = new SerializedObject(persistedProfile); profile.Update();
                SerializedProperty profileScenes = Require(profile, "scenes");
                if (profileScenes.arraySize != 1)
                    throw new InvalidOperationException("Persisted Activity Content Profile must contain exactly one scene entry.");
                SerializedProperty profileScene = profileScenes.GetArrayElementAtIndex(0);
                if (Require(profileScene, "contentId").stringValue != "qa.player.multiplayer.ownership.activity.main" ||
                    Require(profileScene, "scenePath").stringValue != ActivityScenePath ||
                    Require(profileScene, "sceneName").stringValue != Path.GetFileNameWithoutExtension(ActivityScenePath) ||
                    !HasEnumValue(Require(profileScene, "requiredness"), "Required") ||
                    !HasEnumValue(Require(profileScene, "loadMode"), "Additive") ||
                    !HasEnumValue(Require(profileScene, "releasePolicy"), "ReleaseOnActivityChange"))
                    throw new InvalidOperationException("Persisted Activity Content Profile scene entry is incomplete or points at the wrong scene.");
                if (!observers[0].TryValidateConfiguration(out issue)) throw new InvalidOperationException("Activity Observer validation failed: " + issue);
                SerializedObject so = new SerializedObject(scenarios[0]); so.Update();
                if (Require(so, "sessionObserver").objectReferenceValue != observers[0] ||
                    Require(so, "joinTrigger").objectReferenceValue != joins[0] ||
                    Require(so, "expectedP1").objectReferenceValue != AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP1Path) ||
                    Require(so, "expectedP2").objectReferenceValue != AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(SlotP2Path) ||
                    Require(so, "expectedDefaultActor").objectReferenceValue != AssetDatabase.LoadAssetAtPath<ActorProfile>(ActorPath))
                    throw new InvalidOperationException("Persisted scenario references do not resolve to the canonical P1 Slot, default Actor and Activity consumers.");
            }
            finally { CloseAfterValidation(activityScene, ActivityScenePath); }

            Scene persistentScene = OpenForValidation(PersistentScenePath);
            try
            {
                LocalPlayerProvisioningEndpointRegistration[] registrations = Roots(persistentScene).SelectMany(root => root.GetComponentsInChildren<LocalPlayerProvisioningEndpointRegistration>(true)).ToArray();
                LocalPlayerProvisioningAuthoring[] endpoints = Roots(persistentScene).SelectMany(root => root.GetComponentsInChildren<LocalPlayerProvisioningAuthoring>(true)).ToArray();
                PlayerInputManager[] managers = Roots(persistentScene).SelectMany(root => root.GetComponentsInChildren<PlayerInputManager>(true)).ToArray();
                GameObject expectedHost = AssetDatabase.LoadAssetAtPath<GameObject>(HostPath);
                if (registrations.Length != 1 || endpoints.Length != 1 || managers.Length != 1)
                    throw new InvalidOperationException("Persistent Content must contain exactly one registration, provisioning endpoint and PlayerInputManager.");
                if (!registrations[0].TryResolveAuthoring(out LocalPlayerProvisioningAuthoring registered, out issue) || registered != endpoints[0])
                    throw new InvalidOperationException("Persistent Content provisioning registration is invalid: " + issue);
                if (endpoints[0].PlayerInputManager != managers[0] || endpoints[0].LocalPlayerHostPrefab != expectedHost ||
                    managers[0].playerPrefab != expectedHost || managers[0].joinBehavior != PlayerJoinBehavior.JoinPlayersManually ||
                    managers[0].notificationBehavior != PlayerNotifications.InvokeCSharpEvents || managers[0].splitScreen || managers[0].maxPlayerCount != 2)
                    throw new InvalidOperationException("Persistent Content Manager-Provisioned references or manual-Join settings are invalid.");
            }
            finally { CloseAfterValidation(persistentScene, PersistentScenePath); }
        }

        private static Scene OpenForValidation(string path)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            return loaded.IsValid() && loaded.isLoaded ? loaded : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static void CloseAfterValidation(Scene scene, string path)
        {
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.IsValid() && loaded.isLoaded && loaded != SceneManager.GetActiveScene() &&
                !EditorSceneManager.CloseScene(loaded, true))
                throw new InvalidOperationException("Could not close postflight scene: " + path);
        }

        private static GameObject[] Roots(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Postflight scene is not loaded.");
            return scene.GetRootGameObjects();
        }

        private static bool IsSceneInBuildSettings(string path)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                if (scene.path == path && scene.enabled) return true;
            return false;
        }

        private static SerializedProperty Require(SerializedObject so, string name)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property == null) throw new InvalidOperationException("Required serialized authoring field not found: " + name);
            return property;
        }
        private static SerializedProperty Require(SerializedProperty parent, string name)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            if (property == null) throw new InvalidOperationException("Required serialized authoring field not found: " + parent.propertyPath + "." + name);
            return property;
        }
        private static void Set(Object target, string name, Object value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); Require(so, name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void Set(Object target, string name, string value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); Require(so, name).stringValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void Set(Object target, string name, int value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); Require(so, name).intValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void Set(Object target, string name, bool value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); Require(so, name).boolValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void SetEnum(Object target, string name, string value)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); SetEnum(Require(so, name), value); so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
        private static void SetEnum(SerializedProperty property, string value)
        {
            int index = Array.IndexOf(property.enumNames, value);
            if (index < 0) throw new InvalidOperationException("Enum value not found for " + property.propertyPath + ": " + value);
            property.enumValueIndex = index;
        }
        private static bool HasEnumValue(SerializedProperty property, string value)
        {
            return property.enumValueIndex >= 0 && property.enumValueIndex < property.enumNames.Length &&
                property.enumNames[property.enumValueIndex] == value;
        }
        private static void SetObjectArray(Object target, string name, params Object[] values)
        {
            SerializedObject so = new SerializedObject(target); so.Update(); SerializedProperty list = Require(so, name); list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
    }
}














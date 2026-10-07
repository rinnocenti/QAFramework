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
using Immersive.Framework.RouteLifecycle;
using Immersive.QaFramework.IfAdr042;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Immersive.QaFramework.IfAdr042.Editor
{
    public static class QaIfAdr042Setup
    {
        private const string Root = "Assets/QA-IF-ADR-042";
        private const string Settings = Root + "/Settings";
        private const string Players = Root + "/Players";
        private const string Camera = Root + "/Camera";
        private const string Scenes = Root + "/Scenes";
        private const string PersistentScene = Scenes + "/QA_IF_ADR_042_Persistent.unity";
        private const string RouteScene = Scenes + "/QA_IF_ADR_042_Route.unity";
        private const string ActivityScene = Scenes + "/QA_IF_ADR_042_ActivityContent.unity";
        private const string ApplicationAssetPath = Settings + "/GameApplication_QaIfAdr042.asset";
        private const string RouteAssetPath = Settings + "/Route_QaIfAdr042.asset";
        private const string ActivityAssetPath = Settings + "/Activity_QaIfAdr042_SharedGroup.asset";
        private const string ControlScheme = "QA.Keyboard";
        private const string ScenarioPrefabPath = Root + "/PF_QaIfAdr042_SharedGroupScenario.prefab";

        [MenuItem("Immersive Framework/QA/IF-ADR-042/Configure SharedGroup Certification")]
        public static void Configure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets", "QA-IF-ADR-042");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Players");
            EnsureFolder(Root, "Camera");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root, "Editor");

            QaIfAdr042SharedGroupEvidence evidence = CreateAsset<QaIfAdr042SharedGroupEvidence>(Settings + "/QaIfAdr042SharedGroupEvidence.asset");
            ActorProfile p1Actor = CreateActorProfile("Actor_QaIfAdr042_P1", "qa-if-adr-042.p1.default", "QA Player 1");
            ActorProfile p2Actor = CreateActorProfile("Actor_QaIfAdr042_P2", "qa-if-adr-042.p2.default", "QA Player 2");
            ActorProfile p1Replacement = CreateActorProfile("Actor_QaIfAdr042_P1_Replacement", "qa-if-adr-042.p1.replacement", "QA Player 1 Replacement");
            PlayerSlotProfile p1 = CreateSlot("PlayerSlot_QaIfAdr042_P1", "player.1", "Player 1", p1Actor);
            PlayerSlotProfile p2 = CreateSlot("PlayerSlot_QaIfAdr042_P2", "player.2", "Player 2", p2Actor);

            InputActionAsset actions = CreateInputActions();
            PlayerActorRuntimeHost actorRuntimePrefab = CreateActorRuntimePrefab();
            GameObject hostPrefab = CreatePlayerHostPrefab(actorRuntimePrefab, actions);
            PlayerSessionProfile playerSession = CreatePlayerSession(p1, p2);

            GroupCameraRigBehaviorDefinition groupBehavior = CreateGroupBehavior();
            FixedCameraRigBehaviorDefinition fallbackBehavior = CreateAsset<FixedCameraRigBehaviorDefinition>(Camera + "/CameraBehavior_QaIfAdr042_Fallback.asset");
            CameraOutputDefinition outputDefinition = CreateOutputDefinition();
            GameObject assignmentRig = CreateGroupRigPrefab(groupBehavior, evidence);
            GameObject outputPrefab = CreateOutputPrefab(outputDefinition, fallbackBehavior, evidence);
            SessionCameraAssignmentAsset assignment = CreateAssignment(p1, p2, outputDefinition, assignmentRig);

            ActivityContentProfileAsset activityContent = CreateActivityContentProfile();
            ActivityAsset activity = CreateActivity(activityContent);
            RouteAsset route = CreateRoute(activity);
            // Match the working IF-ADR-043 setup: compose the Route scene immediately
            // from the Route asset just created, then reload and validate its saved scene.
            CreateRouteScene();
            route = LoadAsset<RouteAsset>(RouteAssetPath);
            if (route == null || !route.HasPrimaryScene ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(route.PrimaryScenePath) == null)
                throw new InvalidOperationException("IF-ADR-042 requires a Route asset with a saved Primary Scene.");

            activity = LoadAsset<ActivityAsset>(ActivityAssetPath);
            CreateActivityScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            route = LoadAsset<RouteAsset>(RouteAssetPath);
            activity = LoadAsset<ActivityAsset>(ActivityAssetPath);
            playerSession = LoadAsset<PlayerSessionProfile>(Settings + "/PlayerSession_QaIfAdr042.asset");
            evidence = LoadAsset<QaIfAdr042SharedGroupEvidence>(Settings + "/QaIfAdr042SharedGroupEvidence.asset");
            p1 = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P1.asset");
            p2 = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P2.asset");
            p1Replacement = LoadAsset<ActorProfile>(Players + "/Actor_QaIfAdr042_P1_Replacement.asset");
            assignment = LoadAsset<SessionCameraAssignmentAsset>(Settings + "/CameraAssignment_QaIfAdr042_SharedGroup.asset");
            hostPrefab = LoadAsset<GameObject>(Players + "/PF_LocalPlayerHost_QaIfAdr042.prefab");
            outputPrefab = LoadAsset<GameObject>(Camera + "/PF_CameraOutput_QaIfAdr042_Shared.prefab");
            GameApplicationAsset application = CreateApplication(route, playerSession, outputPrefab, assignment);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Scene/prefab serialization always uses freshly loaded canonical assets.
            application = LoadAsset<GameApplicationAsset>(ApplicationAssetPath);
            evidence = LoadAsset<QaIfAdr042SharedGroupEvidence>(Settings + "/QaIfAdr042SharedGroupEvidence.asset");
            p1 = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P1.asset");
            p2 = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P2.asset");
            p1Replacement = LoadAsset<ActorProfile>(Players + "/Actor_QaIfAdr042_P1_Replacement.asset");
            assignment = LoadAsset<SessionCameraAssignmentAsset>(Settings + "/CameraAssignment_QaIfAdr042_SharedGroup.asset");
            hostPrefab = LoadAsset<GameObject>(Players + "/PF_LocalPlayerHost_QaIfAdr042.prefab");
            GameObject scenarioPrefab = CreateScenarioPrefab(
                application, evidence, p1, p2, p1Replacement, assignment);
            AssetDatabase.SaveAssets();
            scenarioPrefab = LoadAsset<GameObject>(ScenarioPrefabPath);
            hostPrefab = LoadAsset<GameObject>(Players + "/PF_LocalPlayerHost_QaIfAdr042.prefab");
            application = LoadAsset<GameApplicationAsset>(ApplicationAssetPath);
            evidence = LoadAsset<QaIfAdr042SharedGroupEvidence>(Settings + "/QaIfAdr042SharedGroupEvidence.asset");
            p1 = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P1.asset");
            p2 = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P2.asset");
            p1Replacement = LoadAsset<ActorProfile>(Players + "/Actor_QaIfAdr042_P1_Replacement.asset");
            assignment = LoadAsset<SessionCameraAssignmentAsset>(Settings + "/CameraAssignment_QaIfAdr042_SharedGroup.asset");
            CreatePersistentScene(hostPrefab, scenarioPrefab);
            AssetDatabase.SaveAssets();
            SetPersistentScene();
            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateGeneratedBaseline();

            Debug.Log("[IF-ADR-042-SHARED-GROUP] setup='Configured' baseline='Valid' assets='updated-in-place' assignment='SharedGroup/ExplicitPlayerSlots/MemberActorTargets' slots='P1,P2' output='1' route='QA_IF_ADR_042_Route' activity='QA_IF_ADR_042_ActivityContent' persistent='QA_IF_ADR_042_Persistent' application='GameApplication_QaIfAdr042'. Run Configure a second time to confirm idempotency, select GameApplication_QaIfAdr042 in ImmersiveFrameworkSettings, open the Persistent scene and enter a fresh Play Mode session.");
        }

        private static PlayerSlotProfile CreateSlot(string file, string slotId, string label, ActorProfile actor)
        {
            PlayerSlotProfile profile = CreateAsset<PlayerSlotProfile>(Settings + "/" + file + ".asset");
            Set(profile, "playerSlotId", slotId);
            Set(profile, "displayName", label);
            Set(profile, "defaultActorProfile", actor);
            return profile;
        }

        private static ActorProfile CreateActorProfile(string file, string id, string displayName)
        {
            ActorProfile profile = CreateAsset<ActorProfile>(Players + "/" + file + ".asset");
            Set(profile, "actorProfileId", id);
            Set(profile, "displayName", displayName);
            Set(profile, "actorKind", ActorKind.Player);
            Set(profile, "actorRole", ActorRole.Protagonist);
            return profile;
        }

        private static PlayerSessionProfile CreatePlayerSession(PlayerSlotProfile p1, PlayerSlotProfile p2)
        {
            PlayerSessionProfile profile = CreateAsset<PlayerSessionProfile>(Settings + "/PlayerSession_QaIfAdr042.asset");
            Set(profile, "supportedSlots", new[] { p1, p2 });
            Set(profile, "initialJoiningOpen", false);
            Set(profile, "hostProvisioning", PlayerHostProvisioningMode.ManagerProvisioned);
            Set(profile, "actorResolutionPolicy", PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            return profile;
        }

        private static InputActionAsset CreateInputActions()
        {
            const string path = Players + "/InputActions_QaIfAdr042.asset";
            InputActionAsset existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            InputActionAsset actions = existing != null ? existing : ScriptableObject.CreateInstance<InputActionAsset>();
            while (actions.actionMaps.Count > 0) actions.RemoveActionMap(actions.actionMaps[0]);
            while (actions.controlSchemes.Count > 0) actions.RemoveControlScheme(actions.controlSchemes[0].name);

            InputActionMap player = actions.AddActionMap("Player");
            InputAction move = player.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", ControlScheme)
                .With("Down", "<Keyboard>/s", ControlScheme)
                .With("Left", "<Keyboard>/a", ControlScheme)
                .With("Right", "<Keyboard>/d", ControlScheme);
            actions.AddControlScheme(ControlScheme)
                .WithBindingGroup(ControlScheme)
                .WithRequiredDevice("<Keyboard>");
            if (actions.FindActionMap("Player") == null || actions.FindAction("Move") == null ||
                !actions.FindControlScheme(ControlScheme).HasValue || move.bindings.Count == 0)
                throw new InvalidOperationException("IF-ADR-042 Input Actions must define Player/Move and a QA.Keyboard binding.");

            if (existing == null) AssetDatabase.CreateAsset(actions, path);
            else EditorUtility.SetDirty(actions);
            AssetDatabase.SaveAssets();
            return LoadAsset<InputActionAsset>(path);
        }

        private static PlayerActorRuntimeHost CreateActorRuntimePrefab()
        {
            const string path = Players + "/PF_PlayerActorRuntime_QaIfAdr042.prefab";
            var root = new GameObject("QA Player Actor Runtime");
            try
            {
                PlayerActorDeclaration declaration = root.AddComponent<PlayerActorDeclaration>();
                PlayerActorRuntimeHost runtime = root.AddComponent<PlayerActorRuntimeHost>();
                Set(runtime, "playerActorDeclaration", declaration);
                ActorCameraSubjectAuthoring subject = root.AddComponent<ActorCameraSubjectAuthoring>();
                Transform observation = new GameObject("GroupObservation").transform;
                observation.SetParent(root.transform, false);
                observation.localPosition = new Vector3(0f, 1f, 0f);
                Set(subject, "observationTransform", observation);
                Set(subject, "framingRadius", 1.35f);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("Player Actor Runtime prefab could not be saved.");
                return LoadAsset<GameObject>(path).GetComponent<PlayerActorRuntimeHost>();
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject CreatePlayerHostPrefab(PlayerActorRuntimeHost actorRuntime, InputActionAsset actions)
        {
            const string path = Players + "/PF_LocalPlayerHost_QaIfAdr042.prefab";
            var root = new GameObject("QA Local Player Host");
            try
            {
                PlayerInput input = root.AddComponent<PlayerInput>();
                Transform mount = new GameObject("Actor Mount").transform;
                mount.SetParent(root.transform, false);
                LocalPlayerHostAuthoring host = root.AddComponent<LocalPlayerHostAuthoring>();
                Set(host, "playerInput", input);
                Set(host, "actorMount", mount);
                Set(host, "playerActorRuntimeHostPrefab", actorRuntime);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("Local Player Host prefab could not be saved.");
            }
            finally { Object.DestroyImmediate(root); }

            ConfigurePlayerInput(LoadAsset<GameObject>(path), actions);
            return LoadAsset<GameObject>(path);
        }

        private static void ConfigurePlayerInput(GameObject prefab, InputActionAsset actions)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                PlayerInput input = contents.GetComponent<PlayerInput>();
                if (input == null) throw new InvalidOperationException("QA Local Player Host has no PlayerInput.");
                var serialized = new SerializedObject(input);
                serialized.Update();
                Require(serialized, "m_Actions").objectReferenceValue = actions;
                Require(serialized, "m_DefaultControlScheme").stringValue = ControlScheme;
                Require(serialized, "m_DefaultActionMap").stringValue = "Player";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(input);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }

        private static GroupCameraRigBehaviorDefinition CreateGroupBehavior()
        {
            GroupCameraRigBehaviorDefinition behavior = CreateAsset<GroupCameraRigBehaviorDefinition>(Camera + "/CameraBehavior_QaIfAdr042_Group.asset");
            Set(behavior, "lookAtRequirement", CameraTargetRequirement.Optional);
            Set(behavior, "followOffset", new Vector3(0f, 5f, -8f));
            Set(behavior, "memberWeight", 1.25f);
            Set(behavior, "memberRadius", 0.7f);
            Set(behavior, "framingSize", 0.8f);
            Set(behavior, "damping", 2f);
            Set(behavior, "fovRange", new Vector2(40f, 70f));
            Set(behavior, "dollyRange", new Vector2(-10f, 10f));
            Set(behavior, "orthoSizeRange", new Vector2(1f, 40f));
            if (!behavior.TryValidate(out string issue)) throw new InvalidOperationException("IF-ADR-042 Group Behavior is invalid: " + issue);
            return behavior;
        }

        private static CameraOutputDefinition CreateOutputDefinition()
        {
            CameraOutputDefinition definition = CreateAsset<CameraOutputDefinition>(Camera + "/CameraOutput_QaIfAdr042_Shared.asset");
            Set(definition, "stableId", "04200000000000000000000000000001");
            return definition;
        }

        private static GameObject CreateGroupRigPrefab(GroupCameraRigBehaviorDefinition behavior,
            QaIfAdr042SharedGroupEvidence evidence)
        {
            const string path = Camera + "/PF_CameraRig_QaIfAdr042_SharedGroup.prefab";
            var root = new GameObject("QA IF-ADR-042 SharedGroup Occurrence");
            try
            {
                CinemachineCamera camera = root.AddComponent<CinemachineCamera>();
                camera.OutputChannel = (OutputChannels)1;
                CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
                Set(composer, "behaviorDefinition", behavior);
                Set(composer, "cinemachineCamera", camera);
                QaIfAdr042OccurrenceProbe probe = root.AddComponent<QaIfAdr042OccurrenceProbe>();
                Set(probe, "evidence", evidence);
                CameraRigComposerApplyRebuildResult materialized = CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(composer, false, false);
                if (!materialized.Succeeded || composer.FrameworkOwnedGroupTargetGroup == null ||
                    composer.FrameworkOwnedGroupFraming == null)
                    throw new InvalidOperationException("IF-ADR-042 Group rig materialization failed: " + materialized.BlockingIssue);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("SharedGroup occurrence prefab could not be saved.");
                return LoadAsset<GameObject>(path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static GameObject CreateOutputPrefab(CameraOutputDefinition definition,
            FixedCameraRigBehaviorDefinition fallbackBehavior, QaIfAdr042SharedGroupEvidence evidence)
        {
            const string path = Camera + "/PF_CameraOutput_QaIfAdr042_Shared.prefab";
            var root = new GameObject("QA IF-ADR-042 Camera Output");
            try
            {
                Camera camera = root.AddComponent<Camera>();
                CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
                brain.ChannelMask = (OutputChannels)1;
                CameraRigComposer fallback = CreateFixedRig(root.transform, "Fallback Rig", fallbackBehavior);
                CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
                Set(output, "outputDefinition", definition);
                Set(output, "unityCamera", camera);
                Set(output, "cinemachineBrain", brain);
                Set(output, "fallbackCameraRig", fallback);
                Set(output, "initializeOnAwake", true);
                QaIfAdr042OutputProbe probe = root.AddComponent<QaIfAdr042OutputProbe>();
                Set(probe, "evidence", evidence);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("SharedGroup Output prefab could not be saved.");
                return LoadAsset<GameObject>(path);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static CameraRigComposer CreateFixedRig(Transform parent, string name,
            FixedCameraRigBehaviorDefinition behavior)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            CinemachineCamera camera = root.AddComponent<CinemachineCamera>();
            camera.OutputChannel = (OutputChannels)1;
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            Set(composer, "behaviorDefinition", behavior);
            Set(composer, "cinemachineCamera", camera);
            return composer;
        }

        private static SessionCameraAssignmentAsset CreateAssignment(PlayerSlotProfile p1,
            PlayerSlotProfile p2, CameraOutputDefinition output, GameObject rigPrefab)
        {
            SessionCameraAssignmentAsset asset = CreateAsset<SessionCameraAssignmentAsset>(Settings + "/CameraAssignment_QaIfAdr042_SharedGroup.asset");
            var serialized = new SerializedObject(asset);
            serialized.Update();
            Require(serialized, "rigPrefab").objectReferenceValue = rigPrefab;
            SetEnum(Require(serialized, "occurrenceMode"), "SharedGroup");
            SetEnum(Require(serialized, "membershipPolicy"), "ExplicitPlayerSlots");
            SetEnum(Require(serialized, "targetPolicy"), "MemberActorTargets");
            SetObjectList(serialized, "memberSlots", p1, p2);
            SetObjectList(serialized, "outputDefinitions", output);
            SerializedProperty mappings = Require(serialized, "individualMemberOutputMappings");
            mappings.arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            if (!asset.TryBuild(out _, out string issue)) throw new InvalidOperationException("IF-ADR-042 Assignment is invalid: " + issue);
            return asset;
        }

        private static ActivityContentProfileAsset CreateActivityContentProfile()
        {
            ActivityContentProfileAsset profile = CreateAsset<ActivityContentProfileAsset>(Settings + "/ActivityContentProfile_QaIfAdr042.asset");
            var serialized = new SerializedObject(profile);
            serialized.Update();
            Require(serialized, "profileId").stringValue = "qa-if-adr-042.activity-content";
            SerializedProperty scenes = Require(serialized, "scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            Require(entry, "contentId").stringValue = "qa-if-adr-042.activity-content.main";
            Require(entry, "scenePath").stringValue = ActivityScene;
            Require(entry, "sceneName").stringValue = Path.GetFileNameWithoutExtension(ActivityScene);
            SetEnum(Require(entry, "requiredness"), "Required");
            SetEnum(Require(entry, "loadMode"), "Additive");
            SetEnum(Require(entry, "releasePolicy"), "ReleaseOnActivityChange");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateActivity(ActivityContentProfileAsset content)
        {
            ActivityAsset activity = CreateAsset<ActivityAsset>(Settings + "/Activity_QaIfAdr042_SharedGroup.asset");
            Set(activity, "activityId", "qa-if-adr-042.shared-group");
            Set(activity, "activityName", "IF-ADR-042 SharedGroup Certification");
            Set(activity, "activityContentProfile", content);
            Set(activity, "playerParticipationProjectionMode", ActivityParticipationProjectionMode.ExplicitSlots);
            Set(activity, "playerParticipationZeroParticipantPolicy", ActivityParticipationZeroParticipantPolicy.Allowed);
            Set(activity, "playerParticipationRequirementLevel", PlayerParticipationRequirementLevel.LogicalActorsPrepared);
            Set(activity, "playerParticipationExplicitSlotProfiles", new[]
            {
                LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P1.asset"),
                LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P2.asset")
            });
            return activity;
        }

        private static RouteAsset CreateRoute(ActivityAsset activity)
        {
            RouteAsset route = CreateAsset<RouteAsset>(Settings + "/Route_QaIfAdr042.asset");
            Set(route, "routeId", "qa-if-adr-042.start");
            Set(route, "routeName", "IF-ADR-042 Certification");
            Set(route, "primaryScenePath", RouteScene);
            Set(route, "primarySceneName", Path.GetFileNameWithoutExtension(RouteScene));
            Set(route, "startupActivity", activity);
            return route;
        }

        private static GameApplicationAsset CreateApplication(RouteAsset route,
            PlayerSessionProfile session, GameObject outputPrefab, SessionCameraAssignmentAsset assignment)
        {
            GameApplicationAsset application = CreateAsset<GameApplicationAsset>(ApplicationAssetPath);
            Set(application, "applicationName", "IF-ADR-042 SharedGroup Certification");
            Set(application, "playerSessionEnabled", true);
            var serialized = new SerializedObject(application);
            serialized.Update();
            Require(serialized, "startupRoute").objectReferenceValue = route;
            Require(serialized, "defaultPlayerSessionProfile").objectReferenceValue = session;
            SerializedProperty assignments = Require(serialized, "startupCameraAssignments");
            assignments.arraySize = 1;
            assignments.GetArrayElementAtIndex(0).objectReferenceValue = assignment;
            SerializedProperty outputs = Require(serialized, "cameraSession.outputPrefabs");
            outputs.arraySize = 1;
            outputs.GetArrayElementAtIndex(0).objectReferenceValue = outputPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            return application;
        }

        private static void CreateRouteScene()
        {
            SceneSetup[] previous;
            Scene scene = BeginGeneratedScene(out previous);
            try
            {
                RouteAsset route = LoadAsset<RouteAsset>(Settings + "/Route_QaIfAdr042.asset");
                var root = new GameObject("IF-ADR-042 Startup Route Content");
                RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
                Set(contribution, "route", route);
                Set(contribution, "localContentId", "qa-if-adr-042.route-content");
                if (contribution.Route != route || !contribution.HasExplicitLocalContentId)
                    throw new InvalidOperationException("IF-ADR-042 Route contribution did not retain its freshly loaded Route before scene save.");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, route.PrimaryScenePath))
                    throw new InvalidOperationException("Could not save IF-ADR-042 Route scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }

            ValidateRouteScene();
        }

        private static void CreateActivityScene()
        {
            SceneSetup[] previous;
            Scene scene = BeginGeneratedScene(out previous);
            try
            {
                ActivityAsset activity = LoadAsset<ActivityAsset>(Settings + "/Activity_QaIfAdr042_SharedGroup.asset");
                GameObject root = new GameObject("IF-ADR-042 Activity Content");
                ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
                Set(contribution, "activity", activity);
                Set(contribution, "localContentId", "qa-if-adr-042.activity-content.main");
                if (contribution.Activity != activity || !contribution.HasExplicitLocalContentId)
                    throw new InvalidOperationException("IF-ADR-042 Activity contribution did not retain its freshly loaded Activity before scene save.");
                PlayerSessionObserver observer = root.AddComponent<PlayerSessionObserver>();
                Set(observer, "scope", LocalPlayerProvisioningConsumerScope.Activity);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, ActivityScene))
                    throw new InvalidOperationException("Could not save IF-ADR-042 Activity Content scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }

            ValidateActivityScene();
        }

        // Keep scene generation safe when Configure is invoked with no active Editor scene,
        // following the scene lifecycle used by QA-NEW-005.
        private static Scene BeginGeneratedScene(out SceneSetup[] previousSetup)
        {
            previousSetup = EditorSceneManager.GetSceneManagerSetup();
            bool hasLoadedScene = false;
            bool hasActiveScene = false;
            if (previousSetup != null)
            {
                foreach (SceneSetup setup in previousSetup)
                {
                    hasLoadedScene |= setup.isLoaded;
                    hasActiveScene |= setup.isActive && setup.isLoaded;
                }
            }

            if (!hasLoadedScene || !hasActiveScene)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                previousSetup = EditorSceneManager.GetSceneManagerSetup();
            }

            return EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static GameObject CreateScenarioPrefab(GameApplicationAsset application,
            QaIfAdr042SharedGroupEvidence evidence, PlayerSlotProfile p1,
            PlayerSlotProfile p2, ActorProfile replacement, SessionCameraAssignmentAsset assignment)
        {
            var root = new GameObject("IF-ADR-042 SharedGroup Certification Scenario");
            try
            {
                QaIfAdr042SharedGroupScenario scenario = root.AddComponent<QaIfAdr042SharedGroupScenario>();
                Set(scenario, "gameApplication", application);
                Set(scenario, "evidence", evidence);
                Set(scenario, "player1", p1);
                Set(scenario, "player2", p2);
                Set(scenario, "player1Replacement", replacement);
                Set(scenario, "assignment", assignment);
                var serialized = new SerializedObject(scenario);
                serialized.Update();
                Require(serialized, "provisioning").objectReferenceValue = null;
                Require(serialized, "provisioningRegistration").objectReferenceValue = null;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(scenario);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ScenarioPrefabPath);
                if (prefab == null) throw new InvalidOperationException("IF-ADR-042 Scenario prefab could not be saved.");
                return LoadAsset<GameObject>(ScenarioPrefabPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void CreatePersistentScene(GameObject hostPrefab, GameObject scenarioPrefab)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("IF-ADR-042 Player Provisioning and Scenario");
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
                GameObject scenarioObject = PrefabUtility.InstantiatePrefab(scenarioPrefab, scene) as GameObject;
                if (scenarioObject == null) throw new InvalidOperationException("IF-ADR-042 Scenario prefab could not be placed in Persistent Content.");
                QaIfAdr042SharedGroupScenario scenario = scenarioObject.GetComponent<QaIfAdr042SharedGroupScenario>();
                if (scenario == null) throw new InvalidOperationException("Persistent Scenario prefab has no certification component.");
                Set(scenario, "provisioning", provisioning);
                Set(scenario, "provisioningRegistration", registration);
                PrefabUtility.RecordPrefabInstancePropertyModifications(scenario);

                SaveScene(scene, PersistentScene);
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void SetPersistentScene()
        {
            SceneAsset persistentScene = LoadAsset<SceneAsset>(PersistentScene);
            GameApplicationAsset application = LoadAsset<GameApplicationAsset>(ApplicationAssetPath);
            SerializedObject serialized = new SerializedObject(application);
            serialized.Update();
            Require(serialized, "persistentContent.containerScene").objectReferenceValue = persistentScene;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            AssetDatabase.SaveAssets();

            application = LoadAsset<GameApplicationAsset>(ApplicationAssetPath);
            if (application.PersistentContent == null || application.PersistentContent.ContainerScene != persistentScene)
                throw new InvalidOperationException("IF-ADR-042 setup failed: Game Application did not retain the generated Persistent Content scene reference.");
        }

        private static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AddScene(scenes, RouteScene);
            AddScene(scenes, ActivityScene);
            AddScene(scenes, PersistentScene);
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void AddScene(List<EditorBuildSettingsScene> scenes, string path)
        {
            int retainedIndex = -1;
            for (int index = scenes.Count - 1; index >= 0; index--)
            {
                if (scenes[index].path != path) continue;
                if (retainedIndex < 0)
                {
                    retainedIndex = index;
                    scenes[index] = new EditorBuildSettingsScene(path, true);
                }
                else
                {
                    scenes.RemoveAt(index);
                    retainedIndex--;
                }
            }
            if (retainedIndex >= 0) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        private static void ValidateGeneratedBaseline()
        {
            GameApplicationAsset application = LoadAsset<GameApplicationAsset>(Settings + "/GameApplication_QaIfAdr042.asset");
            RouteAsset startupRoute = LoadAsset<RouteAsset>(RouteAssetPath);
            ActivityAsset startupActivity = LoadAsset<ActivityAsset>(ActivityAssetPath);
            PlayerSessionProfile session = LoadAsset<PlayerSessionProfile>(Settings + "/PlayerSession_QaIfAdr042.asset");
            PlayerSlotProfile p1 = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P1.asset");
            PlayerSlotProfile p2 = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr042_P2.asset");
            ActorProfile replacement = LoadAsset<ActorProfile>(Players + "/Actor_QaIfAdr042_P1_Replacement.asset");
            SessionCameraAssignmentAsset assignment = LoadAsset<SessionCameraAssignmentAsset>(Settings + "/CameraAssignment_QaIfAdr042_SharedGroup.asset");
            CameraOutputDefinition outputDefinition = LoadAsset<CameraOutputDefinition>(Camera + "/CameraOutput_QaIfAdr042_Shared.asset");
            GameObject outputPrefab = LoadAsset<GameObject>(Camera + "/PF_CameraOutput_QaIfAdr042_Shared.prefab");
            GameObject hostPrefab = LoadAsset<GameObject>(Players + "/PF_LocalPlayerHost_QaIfAdr042.prefab");
            GameObject scenarioPrefab = LoadAsset<GameObject>(ScenarioPrefabPath);
            PlayerActorRuntimeHost actorRuntime = LoadAsset<GameObject>(Players + "/PF_PlayerActorRuntime_QaIfAdr042.prefab").GetComponent<PlayerActorRuntimeHost>();
            QaIfAdr042SharedGroupEvidence evidence = LoadAsset<QaIfAdr042SharedGroupEvidence>(Settings + "/QaIfAdr042SharedGroupEvidence.asset");

            if (!application.PlayerSessionEnabled || application.DefaultPlayerSessionProfile != session ||
                session.SupportedSlotCount != 2 || session.SupportedSlots.Count != 2 ||
                session.SupportedSlots[0] != p1 || session.SupportedSlots[1] != p2 ||
                session.HostProvisioning != PlayerHostProvisioningMode.ManagerProvisioned ||
                session.ActorResolutionPolicy != PlayerActorResolutionPolicy.ResolveConfiguredDefault ||
                p1.DefaultActorProfile == null || p2.DefaultActorProfile == null ||
                replacement.ActorKind != ActorKind.Player || replacement.ActorRole != ActorRole.Protagonist)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: Player Session, ordered Slots or Actor Profiles are incomplete.");

            if (application.StartupRoute != startupRoute || startupRoute.StartupActivity != startupActivity ||
                !startupRoute.HasPrimaryScene || startupRoute.PrimaryScenePath != RouteScene ||
                application.PersistentContent == null ||
                application.PersistentContent.ContainerScene != AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScene) ||
                application.CameraSession == null || application.CameraSession.OutputPrefabs.Count != 1 ||
                application.CameraSession.OutputPrefabs[0] != outputPrefab ||
                application.StartupCameraAssignments.Count != 1 || application.StartupCameraAssignments[0] != assignment ||
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(RouteScene) ||
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(ActivityScene) ||
                !AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScene))
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: Application Route/Activity, Output or scene references are incomplete.");

            if (!session.TryValidate(out string issue) || !assignment.TryBuild(out SessionCameraAssignment built, out issue) ||
                built.OccurrenceMode != CameraOccurrenceMode.SharedGroup ||
                built.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots ||
                built.TargetPolicy != CameraTargetPolicy.MemberActorTargets ||
                built.MemberSlots.Count != 2 || built.Outputs.Count != 1 ||
                built.MemberSlots[0] != p1.PlayerSlotId || built.MemberSlots[1] != p2.PlayerSlotId ||
                built.Outputs[0].OutputId != outputDefinition.OutputId || built.MemberOutputs.Count != 0)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: SharedGroup Assignment contract is invalid. " + issue);

            PlayerInput[] authoredInputs = hostPrefab.GetComponents<PlayerInput>();
            LocalPlayerHostAuthoring[] authoredHosts = hostPrefab.GetComponents<LocalPlayerHostAuthoring>();
            if (authoredInputs.Length != 1 || authoredHosts.Length != 1)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: Host root must have exactly one PlayerInput and one LocalPlayerHostAuthoring.");
            PlayerInput input = authoredInputs[0];
            LocalPlayerHostAuthoring host = authoredHosts[0];
            ActorCameraSubjectAuthoring actorSubject = actorRuntime != null
                ? actorRuntime.GetComponent<ActorCameraSubjectAuthoring>()
                : null;
            InputAction move = input != null && input.actions != null
                ? input.actions.FindAction("Player/Move")
                : null;
            InputControlScheme? qaScheme = input != null && input.actions != null
                ? input.actions.FindControlScheme(ControlScheme)
                : null;
            if (actorRuntime == null || actorSubject == null || input == null || host == null || host.PlayerInput != input ||
                host.PlayerActorRuntimeHostPrefab != actorRuntime || host.ActorMount == null ||
                input.actions == null || input.actions.FindActionMap("Player") == null ||
                input.defaultActionMap != "Player" || input.defaultControlScheme != ControlScheme ||
                move == null || move.bindings.Count == 0 || !qaScheme.HasValue ||
                !qaScheme.Value.deviceRequirements.Any(requirement =>
                    requirement.controlPath == "<Keyboard>" && !requirement.isOptional) ||
                !move.bindings.Any(binding => binding.path.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    binding.groups.Split(';').Contains(ControlScheme)) ||
                !actorRuntime.TryValidateConfiguration(out issue) ||
                !actorSubject.TryValidateConfiguration(out issue))
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: PlayerInput/Host/Actor Subject prefab composition is invalid. " + issue);

            if (startupActivity.PlayerParticipationProjectionMode != ActivityParticipationProjectionMode.ExplicitSlots ||
                startupActivity.PlayerParticipationZeroParticipantPolicy != ActivityParticipationZeroParticipantPolicy.Allowed ||
                startupActivity.PlayerParticipationRequirementLevel != PlayerParticipationRequirementLevel.LogicalActorsPrepared ||
                startupActivity.PlayerParticipationExplicitSlotProfiles.Count != 2 ||
                startupActivity.PlayerParticipationExplicitSlotProfiles[0] != p1 ||
                startupActivity.PlayerParticipationExplicitSlotProfiles[1] != p2)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: startup Activity must project exactly the two configured Slots while allowing zero participants.");

            CameraOutputAuthoring output = outputPrefab.GetComponent<CameraOutputAuthoring>();
            Camera outputCamera = outputPrefab.GetComponent<Camera>();
            CinemachineBrain brain = outputPrefab.GetComponent<CinemachineBrain>();
            if (output == null || outputCamera == null || !outputCamera.enabled || !outputCamera.gameObject.activeSelf ||
                brain == null || output.OutputDefinition != outputDefinition ||
                output.UnityCamera != outputCamera || output.CinemachineBrain != brain ||
                output.FallbackCameraRig == null || output.FallbackCameraRig.CinemachineCamera == null ||
                brain.ChannelMask != output.FallbackCameraRig.CinemachineCamera.OutputChannel ||
                !output.FallbackCameraRig.TryValidateForApply(out issue) ||
                !CameraRigComposerApplyRebuildUtility.Validate(output.FallbackCameraRig).Succeeded ||
                !output.TryValidateDefinition(out issue))
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: Output or Fallback composition is invalid. " + issue);

            GameObject assignmentRig = assignment.RigPrefab;
            CameraRigComposer composer = assignmentRig != null ? assignmentRig.GetComponent<CameraRigComposer>() : null;
            QaIfAdr042OccurrenceProbe occurrenceProbe = assignmentRig != null ? assignmentRig.GetComponent<QaIfAdr042OccurrenceProbe>() : null;
            if (composer == null || composer.BehaviorDefinition == null ||
                composer.FrameworkOwnedGroupTargetGroup == null || composer.FrameworkOwnedGroupFraming == null ||
                occurrenceProbe == null || !composer.TryValidateForApply(out issue) ||
                !CameraRigComposerApplyRebuildUtility.Validate(composer).Succeeded)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: Group rig materialization/probe is invalid. " + issue);

            ValidatePersistentScene(application, session, hostPrefab, p1, p2, replacement, assignment, evidence);
            ValidateScenarioPrefab(scenarioPrefab, application, p1, p2, replacement, assignment, evidence);
            // Scene restoration can invalidate Unity wrappers returned through another asset.
            // Validate against the freshly loaded canonical assets and separately assert that
            // the Application -> Route -> Activity chain still points at those assets.
            startupRoute = LoadAsset<RouteAsset>(RouteAssetPath);
            startupActivity = LoadAsset<ActivityAsset>(ActivityAssetPath);
            if (application.StartupRoute != startupRoute || startupRoute.StartupActivity != startupActivity)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: canonical Application -> Route -> Activity references changed before persisted-scene validation.");
            ValidateRouteScene();
            ValidateActivityScene();
            ValidateBuildSettingsScene(RouteScene);
            ValidateBuildSettingsScene(ActivityScene);
            ValidateBuildSettingsScene(PersistentScene);
        }

        private static void ValidateScenarioPrefab(GameObject prefab, GameApplicationAsset application,
            PlayerSlotProfile p1, PlayerSlotProfile p2, ActorProfile replacement,
            SessionCameraAssignmentAsset assignment, QaIfAdr042SharedGroupEvidence evidence)
        {
            QaIfAdr042SharedGroupScenario[] scenarios = prefab.GetComponentsInChildren<QaIfAdr042SharedGroupScenario>(true);
            if (scenarios.Length != 1 || scenarios[0] == null || !scenarios[0].enabled || !scenarios[0].gameObject.activeSelf)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: Scenario prefab must contain exactly one enabled certification component.");

            SerializedObject serialized = new SerializedObject(scenarios[0]);
            serialized.Update();
            RequireReference(serialized, "gameApplication", application);
            RequireReference(serialized, "evidence", evidence);
            RequireReference(serialized, "player1", p1);
            RequireReference(serialized, "player2", p2);
            RequireReference(serialized, "player1Replacement", replacement);
            RequireReference(serialized, "assignment", assignment);
            if (Require(serialized, "provisioning").objectReferenceValue != null ||
                Require(serialized, "provisioningRegistration").objectReferenceValue != null)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: scene-scoped provisioning references must remain unassigned on the Scenario prefab.");
        }

        private static void ValidateRouteScene()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = OpenFreshSceneForValidation(RouteScene);
            try
            {
                RouteAsset route = LoadAsset<RouteAsset>(RouteAssetPath);
                RouteContentContribution[] contributions = FindComponents<RouteContentContribution>(scene);
                if (contributions.Length != 1 || contributions[0].Route != route ||
                    !contributions[0].HasExplicitLocalContentId)
                {
                    throw new InvalidOperationException("IF-ADR-042 setup validation failed: neutral startup Route scene contribution is missing or bound to a different Route.");
                }
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static void ValidatePersistentScene(GameApplicationAsset application,
            PlayerSessionProfile session, GameObject hostPrefab, PlayerSlotProfile p1,
            PlayerSlotProfile p2, ActorProfile replacement, SessionCameraAssignmentAsset assignment,
            QaIfAdr042SharedGroupEvidence evidence)
        {
            Scene scene = OpenSceneForValidation(PersistentScene, out bool openedForValidation);
            try
            {
                LocalPlayerProvisioningAuthoring[] provisioning = FindComponents<LocalPlayerProvisioningAuthoring>(scene);
                LocalPlayerProvisioningEndpointRegistration[] registrations = FindComponents<LocalPlayerProvisioningEndpointRegistration>(scene);
                QaIfAdr042SharedGroupScenario[] scenarios = FindComponents<QaIfAdr042SharedGroupScenario>(scene);
                if (provisioning.Length != 1 || registrations.Length != 1 || scenarios.Length != 1)
                    throw new InvalidOperationException("IF-ADR-042 setup validation failed: Persistent scene requires exactly one Provisioning endpoint, registration and Scenario.");

                LocalPlayerProvisioningAuthoring endpoint = provisioning[0];
                PlayerInputManager manager = endpoint.PlayerInputManager;
                if (manager == null || !manager.isActiveAndEnabled || !endpoint.isActiveAndEnabled ||
                    !registrations[0].isActiveAndEnabled || !scenarios[0].isActiveAndEnabled ||
                    manager.gameObject != endpoint.gameObject || manager.maxPlayerCount != 2 ||
                    manager.joinBehavior != PlayerJoinBehavior.JoinPlayersManually ||
                    manager.notificationBehavior != PlayerNotifications.InvokeCSharpEvents || manager.playerPrefab != hostPrefab ||
                    endpoint.LocalPlayerHostPrefab != hostPrefab || !endpoint.UsesManualJoin ||
                    !endpoint.UsesCSharpJoinNotifications || manager.splitScreen ||
                    registrations[0].ProvisioningAuthoring != endpoint ||
                    !endpoint.gameObject.scene.IsValid())
                    throw new InvalidOperationException("IF-ADR-042 setup validation failed: persistent ManagerProvisioned endpoint/observer binding differs from generated baseline.");

                SerializedObject serialized = new SerializedObject(scenarios[0]);
                serialized.Update();
                RequireReference(serialized, "gameApplication", application);
                RequireReference(serialized, "provisioning", endpoint);
                RequireReference(serialized, "provisioningRegistration", registrations[0]);
                RequireReference(serialized, "evidence", evidence);
                RequireReference(serialized, "player1", p1);
                RequireReference(serialized, "player2", p2);
                RequireReference(serialized, "player1Replacement", replacement);
                RequireReference(serialized, "assignment", assignment);
                if (application.DefaultPlayerSessionProfile != session)
                    throw new InvalidOperationException("IF-ADR-042 setup validation failed: persistent Scenario and Game Application do not share the generated Player Session baseline.");
            }
            finally { if (openedForValidation) EditorSceneManager.CloseScene(scene, false); }
        }

        private static void ValidateActivityScene()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene scene = OpenFreshSceneForValidation(ActivityScene);
            try
            {
                ActivityAsset activity = LoadAsset<ActivityAsset>(ActivityAssetPath);
                ActivityContentContribution[] contributions = FindComponents<ActivityContentContribution>(scene);
                PlayerSessionObserver[] observers = FindComponents<PlayerSessionObserver>(scene);
                bool contributionValid = contributions.Length == 1 && contributions[0].Activity == activity &&
                    contributions[0].HasExplicitLocalContentId;
                bool observerValid = observers.Length == 1 && observers[0].isActiveAndEnabled &&
                    observers[0].Scope == LocalPlayerProvisioningConsumerScope.Activity;
                ActivityContentProfileAsset profile = activity != null ? activity.ActivityContentProfile : null;
                bool profileValid = profile != null && profile.SceneCount == 1 &&
                    profile.Scenes[0].ScenePath == ActivityScene &&
                    profile.Scenes[0].Requiredness == FrameworkContentRequiredness.Required &&
                    profile.Scenes[0].LoadMode == ActivityContentSceneLoadMode.Additive &&
                    profile.Scenes[0].ReleasePolicy == ActivityContentReleasePolicy.ReleaseOnActivityChange;
                if (!contributionValid || !observerValid || !profileValid)
                {
                    string observedActivity = contributions.Length == 1 && contributions[0].Activity != null
                        ? AssetDatabase.GetAssetPath(contributions[0].Activity)
                        : "<null-or-missing>";
                    string expectedActivity = activity != null ? AssetDatabase.GetAssetPath(activity) : "<null>";
                    string profileState = profile == null
                        ? "<missing>"
                        : "count=" + profile.SceneCount + (profile.SceneCount == 1
                            ? ", path='" + profile.Scenes[0].ScenePath + "', requiredness='" + profile.Scenes[0].Requiredness +
                                "', loadMode='" + profile.Scenes[0].LoadMode + "', releasePolicy='" + profile.Scenes[0].ReleasePolicy + "'"
                            : string.Empty);
                    throw new InvalidOperationException("IF-ADR-042 setup validation failed: Activity scene/Profile binding is invalid (contribution=" +
                        contributionValid + ", expectedActivity='" + expectedActivity + "', observedActivity='" + observedActivity +
                        "', explicitContentId=" + (contributions.Length == 1 && contributions[0].HasExplicitLocalContentId) +
                        ", activityObserverValid=" + observerValid + ", activityObservers=" + observers.Length +
                        ", profileValid=" + profileValid + ", profile=" + profileState + ").");
                }
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static Scene OpenFreshSceneForValidation(string path)
        {
            Scene existing = SceneManager.GetSceneByPath(path);
            if (existing.IsValid() && existing.isLoaded)
                EditorSceneManager.CloseScene(existing, true);
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static Scene OpenSceneForValidation(string path, out bool openedForValidation)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            if (scene.IsValid() && scene.isLoaded)
            {
                openedForValidation = false;
                return scene;
            }
            openedForValidation = true;
            return EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        private static void ValidateBuildSettingsScene(string path)
        {
            if (EditorBuildSettings.scenes.Count(scene => scene.path == path) != 1 ||
                !EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == path) ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: Build Settings must include scene '" + path + "'.");
        }

        private static T[] FindComponents<T>(Scene scene) where T : Component
        {
            var result = new List<T>();
            foreach (GameObject root in scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
            return result.ToArray();
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException("IF-ADR-042 scene could not be saved to '" + path + "'.");
        }

        private static void RequireReference(SerializedObject serialized, string path, Object expected)
        {
            if (Require(serialized, path).objectReferenceValue != expected)
                throw new InvalidOperationException("IF-ADR-042 setup validation failed: persistent Scenario binding '" + path + "' does not reference the generated baseline.");
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("IF-ADR-042 setup validation failed: required asset '" + path + "' is missing or has the wrong type.");
            return asset;
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Object occupied = AssetDatabase.LoadMainAssetAtPath(path);
            if (occupied != null) throw new InvalidOperationException("Cannot create '" + typeof(T).Name + "'; path is occupied by '" + occupied.GetType().Name + "'.");
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return LoadAsset<T>(path);
        }

        private static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty field = Require(serialized, property);
            if (field.propertyType != SerializedPropertyType.ObjectReference)
                throw new InvalidOperationException("Serialized field '" + property + "' is not an object reference.");
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Set(Object target, string property, object value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty field = Require(serialized, property);
            if (value == null && field.propertyType == SerializedPropertyType.ObjectReference)
                field.objectReferenceValue = null;
            else if (value is Object reference) field.objectReferenceValue = reference;
            else if (value is string text) field.stringValue = text;
            else if (value is bool boolean) field.boolValue = boolean;
            else if (value is int integer) field.intValue = integer;
            else if (value is float number) field.floatValue = number;
            else if (value is Vector2 vector2) field.vector2Value = vector2;
            else if (value is Vector3 vector3) field.vector3Value = vector3;
            else if (value is PlayerSlotProfile[] slots)
            {
                field.arraySize = slots.Length;
                for (int index = 0; index < slots.Length; index++) field.GetArrayElementAtIndex(index).objectReferenceValue = slots[index];
            }
            else if (value is Enum enumValue) SetEnum(field, enumValue.ToString());
            else throw new InvalidOperationException("Unsupported generated QA value type for '" + property + "'.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectList(SerializedObject serialized, string path, params Object[] values)
        {
            SerializedProperty list = Require(serialized, path);
            list.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) list.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static SerializedProperty Require(SerializedObject serialized, string path) =>
            serialized.FindProperty(path) ?? throw new InvalidOperationException("Serialized field '" + path + "' was not found.");

        private static SerializedProperty Require(SerializedProperty parent, string path) =>
            parent.FindPropertyRelative(path) ?? throw new InvalidOperationException("Serialized field '" + parent.propertyPath + "." + path + "' was not found.");

        private static void SetEnum(SerializedProperty property, string value)
        {
            for (int index = 0; index < property.enumNames.Length; index++)
            {
                if (property.enumNames[index] != value) continue;
                property.enumValueIndex = index;
                return;
            }
            throw new InvalidOperationException("Enum value '" + value + "' is unavailable for '" + property.propertyPath + "'.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}

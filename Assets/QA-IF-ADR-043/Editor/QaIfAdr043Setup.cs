using System;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RouteLifecycle;
using Immersive.QaFramework.IfAdr043;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Immersive.QaFramework.IfAdr043.Editor
{
    public static class QaIfAdr043Setup
    {
        private const string Root = "Assets/QA-IF-ADR-043";
        private const string Settings = Root + "/Settings";
        private const string Camera = Root + "/Camera";
        private const string Players = Root + "/Players";
        private const string ScenarioPrefabPath = Root + "/PF_QaIfAdr043_PhysicalParticipationScenario.prefab";
        private const string Scenes = Root + "/Scenes";
        private const string RouteScene = Scenes + "/QA_IF_ADR_043_Route.unity";
        private const string PersistentScene = Scenes + "/QA_IF_ADR_043_Persistent.unity";
        private const string PlayerControlScheme = "QA.Keyboard";

        [MenuItem("Immersive Framework/QA/IF-ADR-043/Configure Physical Participation Certification")]
        public static void Configure()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets", "QA-IF-ADR-043");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Camera");
            EnsureFolder(Root, "Players");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Scripts");
            EnsureFolder(Root, "Editor");

            var evidence = CreateAsset<QaIfAdr043CameraOutputEvidence>(Settings + "/QaIfAdr043CameraOutputEvidence.asset");
            PlayerSlotProfile first = CreateSlot("PlayerSlot_QaIfAdr043_P1", "player.1", "Player 1");
            PlayerSlotProfile second = CreateSlot("PlayerSlot_QaIfAdr043_P2", "player.2", "Player 2");
            PlayerSessionProfile session = CreateAsset<PlayerSessionProfile>(Settings + "/PlayerSession_QaIfAdr043.asset");
            Set(session, "supportedSlots", new[] { first, second });
            Set(session, "initialJoiningOpen", true);
            Set(session, "hostProvisioning", PlayerHostProvisioningMode.ManagerProvisioned);
            Set(session, "actorResolutionPolicy", PlayerActorResolutionPolicy.LeaveUnresolved);

            CameraOutputDefinition firstDefinition = CreateOutputDefinition("CameraOutput_QaIfAdr043_P1", "04300000000000000000000000000001");
            CameraOutputDefinition secondDefinition = CreateOutputDefinition("CameraOutput_QaIfAdr043_P2", "04300000000000000000000000000002");
            GameObject firstOutput = CreateOutputPrefab("PF_CameraOutput_QaIfAdr043_P1", firstDefinition, first, evidence, "04300000000000000000000000000011");
            GameObject secondOutput = CreateOutputPrefab("PF_CameraOutput_QaIfAdr043_P2", secondDefinition, second, evidence, "04300000000000000000000000000012");
            GameObject assignmentRig = CreateAssignmentRigPrefab();
            SessionCameraAssignmentAsset assignment = CreateAssignment(first, second, firstDefinition, secondDefinition, assignmentRig);

            GameObject playerPrefab = CreateLocalPlayerHostPrefab();
            InputActionAsset actions = CreateInputActions();
            ConfigurePlayerInput(playerPrefab, actions);

            RouteAsset route = CreateRoute();
            CreateRouteScene(route);
            route = AssetDatabase.LoadAssetAtPath<RouteAsset>(Settings + "/Route_QaIfAdr043.asset");
            if (route == null || !route.HasPrimaryScene ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(route.PrimaryScenePath) == null)
                throw new InvalidOperationException("IF-ADR-043 requires a QA Route asset with a saved Primary Scene.");
            AssetDatabase.SaveAssets();
            evidence = AssetDatabase.LoadAssetAtPath<QaIfAdr043CameraOutputEvidence>(Settings + "/QaIfAdr043CameraOutputEvidence.asset");
            first = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr043_P1.asset");
            second = AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr043_P2.asset");
            session = AssetDatabase.LoadAssetAtPath<PlayerSessionProfile>(Settings + "/PlayerSession_QaIfAdr043.asset");
            assignment = AssetDatabase.LoadAssetAtPath<SessionCameraAssignmentAsset>(Settings + "/CameraAssignment_QaIfAdr043_Individual.asset");
            firstOutput = AssetDatabase.LoadAssetAtPath<GameObject>(Camera + "/PF_CameraOutput_QaIfAdr043_P1.prefab");
            secondOutput = AssetDatabase.LoadAssetAtPath<GameObject>(Camera + "/PF_CameraOutput_QaIfAdr043_P2.prefab");
            playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Players + "/PF_LocalPlayerHost_QaIfAdr043.prefab");
            if (evidence == null || first == null || second == null || session == null || assignment == null ||
                firstOutput == null || secondOutput == null || playerPrefab == null)
                throw new InvalidOperationException("IF-ADR-043 authoring assets could not be reloaded before persistent-scene composition.");
            GameApplicationAsset application = CreateAsset<GameApplicationAsset>(Settings + "/GameApplication_QaIfAdr043.asset");
            Set(application, "applicationName", "IF-ADR-043 Physical Participation Certification");
            Set(application, "playerSessionEnabled", true);
            SerializedObject serialized = new SerializedObject(application);
            serialized.Update();
            Require(serialized, "startupRoute").objectReferenceValue = route;
            Require(serialized, "defaultPlayerSessionProfile").objectReferenceValue = session;
            SerializedProperty assignments = Require(serialized, "startupCameraAssignments");
            assignments.arraySize = 1;
            assignments.GetArrayElementAtIndex(0).objectReferenceValue = assignment;
            SerializedProperty outputs = Require(serialized, "cameraSession.outputPrefabs");
            outputs.arraySize = 2;
            outputs.GetArrayElementAtIndex(0).objectReferenceValue = firstOutput;
            outputs.GetArrayElementAtIndex(1).objectReferenceValue = secondOutput;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            AssetDatabase.SaveAssets();

            // Reacquire external references immediately before serializing the scene.
            evidence = LoadAsset<QaIfAdr043CameraOutputEvidence>(Settings + "/QaIfAdr043CameraOutputEvidence.asset");
            first = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr043_P1.asset");
            second = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr043_P2.asset");
            playerPrefab = LoadAsset<GameObject>(Players + "/PF_LocalPlayerHost_QaIfAdr043.prefab");
            application = LoadAsset<GameApplicationAsset>(Settings + "/GameApplication_QaIfAdr043.asset");
            GameObject scenarioPrefab = CreateScenarioPrefab(application, evidence, first, second);

            CreatePersistentScene(evidence, playerPrefab, first, second, scenarioPrefab);
            application = AssetDatabase.LoadAssetAtPath<GameApplicationAsset>(Settings + "/GameApplication_QaIfAdr043.asset");
            if (application == null)
                throw new InvalidOperationException("The IF-ADR-043 Game Application asset could not be reloaded after scene authoring.");
            serialized = new SerializedObject(application);
            serialized.Update();
            Require(serialized, "persistentContent.containerScene").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScene);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);

            AddScenesToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateGeneratedBaseline();
            Debug.Log("[IF-ADR-043] setup='Configured' baseline='Valid' assets='updated-in-place' outputs='2' slots='2' assignment='IndividualPerPlayer' manager='ManagerProvisioned/Manual/2' route='QA_IF_ADR_043_Route' persistent='QA_IF_ADR_043_Persistent' application='GameApplication_QaIfAdr043'. Select this Game Application in ImmersiveFrameworkSettings and enter a fresh Play Mode session.");
        }

        private static PlayerSlotProfile CreateSlot(string file, string id, string label)
        {
            PlayerSlotProfile profile = CreateAsset<PlayerSlotProfile>(Settings + "/" + file + ".asset");
            Set(profile, "playerSlotId", id);
            Set(profile, "displayName", label);
            return profile;
        }

        private static CameraOutputDefinition CreateOutputDefinition(string file, string stableId)
        {
            CameraOutputDefinition definition = CreateAsset<CameraOutputDefinition>(Camera + "/" + file + ".asset");
            Set(definition, "stableId", stableId);
            return definition;
        }

        private static GameObject CreateOutputPrefab(string file, CameraOutputDefinition definition,
            PlayerSlotProfile slot, QaIfAdr043CameraOutputEvidence evidence, string channel)
        {
            string path = Camera + "/" + file + ".prefab";
            var root = new GameObject(file);
            try
            {
                UnityEngine.Camera unityCamera = root.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
                brain.ChannelMask = ParseChannel(channel);
                CameraRigComposer fallback = CreateRigChild(root.transform, "Fallback Camera", CreateBehavior(file + "_FallbackBehavior"), channel);
                CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
                Set(output, "outputDefinition", definition);
                Set(output, "unityCamera", unityCamera);
                Set(output, "cinemachineBrain", brain);
                Set(output, "fallbackCameraRig", fallback);
                Set(output, "initializeOnAwake", true);
                QaIfAdr043CameraOutputProbe probe = root.AddComponent<QaIfAdr043CameraOutputProbe>();
                Set(probe, "evidence", evidence);
                Set(probe, "mappedSlot", slot);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateAssignmentRigPrefab()
        {
            const string path = Camera + "/PF_CameraRig_QaIfAdr043_Assignment.prefab";
            var root = new GameObject("Camera Rig IF-ADR-043 Assignment");
            try
            {
                AddRig(root, CreateBehavior("CameraBehavior_QaIfAdr043_Assignment"), "04300000000000000000000000000021");
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static CameraRigComposer CreateRigChild(Transform parent, string name, CameraRigBehaviorDefinition behavior, string channel)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return AddRig(child, behavior, channel);
        }

        private static CameraRigComposer AddRig(GameObject root, CameraRigBehaviorDefinition behavior, string channel)
        {
            CinemachineCamera camera = root.AddComponent<CinemachineCamera>();
            camera.OutputChannel = ParseChannel(channel);
            CameraRigComposer composer = root.AddComponent<CameraRigComposer>();
            Set(composer, "behaviorDefinition", behavior);
            Set(composer, "cinemachineCamera", camera);
            return composer;
        }

        private static FixedCameraRigBehaviorDefinition CreateBehavior(string file)
        {
            FixedCameraRigBehaviorDefinition behavior = CreateAsset<FixedCameraRigBehaviorDefinition>(Camera + "/" + file + ".asset");
            return behavior;
        }

        private static SessionCameraAssignmentAsset CreateAssignment(PlayerSlotProfile first, PlayerSlotProfile second,
            CameraOutputDefinition firstOutput, CameraOutputDefinition secondOutput, GameObject rigPrefab)
        {
            SessionCameraAssignmentAsset assignment = CreateAsset<SessionCameraAssignmentAsset>(Settings + "/CameraAssignment_QaIfAdr043_Individual.asset");
            SerializedObject serialized = new SerializedObject(assignment);
            serialized.Update();
            Require(serialized, "rigPrefab").objectReferenceValue = rigPrefab;
            SetEnum(Require(serialized, "occurrenceMode"), "IndividualPerPlayer");
            SetEnum(Require(serialized, "membershipPolicy"), "ExplicitPlayerSlots");
            SetEnum(Require(serialized, "targetPolicy"), "NoSubject");
            SetObjectList(serialized, "memberSlots", first, second);
            SetObjectList(serialized, "outputDefinitions", firstOutput, secondOutput);
            SerializedProperty mappings = Require(serialized, "individualMemberOutputMappings");
            mappings.arraySize = 2;
            ConfigureMapping(mappings.GetArrayElementAtIndex(0), first, firstOutput);
            ConfigureMapping(mappings.GetArrayElementAtIndex(1), second, secondOutput);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(assignment);
            if (!assignment.TryBuild(out _, out string issue)) throw new InvalidOperationException("IF-ADR-043 Assignment setup failed: " + issue);
            return assignment;
        }

        private static void ConfigureMapping(SerializedProperty element, PlayerSlotProfile slot, CameraOutputDefinition output)
        {
            Require(element, "playerSlotProfile").objectReferenceValue = slot;
            Require(element, "outputDefinition").objectReferenceValue = output;
        }

        private static GameObject CreateLocalPlayerHostPrefab()
        {
            const string actorPath = Players + "/PF_PlayerActorRuntime_QaIfAdr043.prefab";
            const string hostPath = Players + "/PF_LocalPlayerHost_QaIfAdr043.prefab";
            var actorRoot = new GameObject("Player Actor Runtime");
            GameObject actorPrefab;
            try
            {
                PlayerActorDeclaration declaration = actorRoot.AddComponent<PlayerActorDeclaration>();
                PlayerActorRuntimeHost actorHost = actorRoot.AddComponent<PlayerActorRuntimeHost>();
                Set(actorHost, "playerActorDeclaration", declaration);
                actorPrefab = PrefabUtility.SaveAsPrefabAsset(actorRoot, actorPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(actorRoot);
            }

            var hostRoot = new GameObject("Local Player Host");
            try
            {
                PlayerInput input = hostRoot.AddComponent<PlayerInput>();
                Transform mount = new GameObject("Actor Mount").transform;
                mount.SetParent(hostRoot.transform, false);
                LocalPlayerHostAuthoring host = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
                Set(host, "playerInput", input);
                Set(host, "actorMount", mount);
                Set(host, "playerActorRuntimeHostPrefab", actorPrefab.GetComponent<PlayerActorRuntimeHost>());
                return PrefabUtility.SaveAsPrefabAsset(hostRoot, hostPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hostRoot);
            }
        }

        private static InputActionAsset CreateInputActions()
        {
            const string path = Players + "/InputActions_QaIfAdr043.asset";
            InputActionAsset existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            InputActionAsset actions = existing != null ? existing : ScriptableObject.CreateInstance<InputActionAsset>();
            while (actions.actionMaps.Count > 0)
                actions.RemoveActionMap(actions.actionMaps[0]);
            while (actions.controlSchemes.Count > 0)
                actions.RemoveControlScheme(actions.controlSchemes[0].name);

            InputActionMap map = actions.AddActionMap("Player");
            InputAction move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", PlayerControlScheme)
                .With("Down", "<Keyboard>/s", PlayerControlScheme)
                .With("Left", "<Keyboard>/a", PlayerControlScheme)
                .With("Right", "<Keyboard>/d", PlayerControlScheme);
            actions.AddControlScheme(PlayerControlScheme)
                .WithBindingGroup(PlayerControlScheme)
                .WithRequiredDevice("<Keyboard>");
            if (actions.FindActionMap("Player") == null || actions.FindAction("Move") == null ||
                actions.FindControlScheme(PlayerControlScheme) == null || actions.FindAction("Move").bindings.Count == 0)
                throw new InvalidOperationException("IF-ADR-043 PlayerInput Actions must include a bound Player map and its QA keyboard control scheme.");

            if (existing == null)
                AssetDatabase.CreateAsset(actions, path);
            else
                EditorUtility.SetDirty(actions);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        private static void ConfigurePlayerInput(GameObject prefab, InputActionAsset actions)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                PlayerInput input = contents.GetComponent<PlayerInput>();
                if (input == null) throw new InvalidOperationException("Local Player Host Prefab must have a root PlayerInput component.");
                var serialized = new SerializedObject(input);
                serialized.Update();
                SerializedProperty actionAsset = Require(serialized, "m_Actions");
                actionAsset.objectReferenceValue = actions;
                Require(serialized, "m_DefaultControlScheme").stringValue = PlayerControlScheme;
                Require(serialized, "m_DefaultActionMap").stringValue = "Player";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(input);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static RouteAsset CreateRoute()
        {
            RouteAsset route = CreateAsset<RouteAsset>(Settings + "/Route_QaIfAdr043.asset");
            Set(route, "routeId", "qa-if-adr-043.start");
            Set(route, "routeName", "IF-ADR-043 Certification");
            Set(route, "primaryScenePath", RouteScene);
            Set(route, "primarySceneName", "QA_IF_ADR_043_Route");
            return route;
        }

        private static void CreateRouteScene(RouteAsset route)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("IF-ADR-043 Route Content");
                RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
                Set(contribution, "route", route);
                Set(contribution, "localContentId", "qa-if-adr-043.route-content");
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, RouteScene)) throw new InvalidOperationException("Could not save IF-ADR-043 Route scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static GameObject CreateScenarioPrefab(GameApplicationAsset application,
            QaIfAdr043CameraOutputEvidence evidence, PlayerSlotProfile first, PlayerSlotProfile second)
        {
            var root = new GameObject("IF-ADR-043 Physical Participation Certification");
            try
            {
                QaIfAdr043PhysicalParticipationScenario scenario = root.AddComponent<QaIfAdr043PhysicalParticipationScenario>();
                var serialized = new SerializedObject(scenario);
                serialized.Update();
                Require(serialized, "gameApplication").objectReferenceValue = application;
                Require(serialized, "outputEvidence").objectReferenceValue = evidence;
                Require(serialized, "player1").objectReferenceValue = first;
                Require(serialized, "player2").objectReferenceValue = second;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(scenario);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, ScenarioPrefabPath);
                if (saved == null)
                    throw new InvalidOperationException("IF-ADR-043 scenario prefab could not be saved.");
                return LoadAsset<GameObject>(ScenarioPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreatePersistentScene(QaIfAdr043CameraOutputEvidence evidence,
            GameObject playerPrefab, PlayerSlotProfile first, PlayerSlotProfile second, GameObject scenarioPrefab)
        {
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var root = new GameObject("IF-ADR-043 Provisioning");
                PlayerInputManager manager = root.AddComponent<PlayerInputManager>();
                manager.playerPrefab = playerPrefab;
                Set(manager, "m_MaxPlayerCount", 2);
                manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
                manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                manager.splitScreen = false;
                LocalPlayerProvisioningAuthoring provisioning = root.AddComponent<LocalPlayerProvisioningAuthoring>();
                Set(provisioning, "playerInputManager", manager);
                Set(provisioning, "localPlayerHostPrefab", playerPrefab);
                LocalPlayerProvisioningEndpointRegistration registration = root.AddComponent<LocalPlayerProvisioningEndpointRegistration>();
                Set(registration, "provisioningAuthoring", provisioning);
                if (!registration.enabled || registration.ProvisioningAuthoring != provisioning ||
                    manager.maxPlayerCount != 2 || manager.playerPrefab != playerPrefab)
                    throw new InvalidOperationException("IF-ADR-043 provisioning registration must point to the authored endpoint and a two-player manager before the persistent scene is saved.");

                GameObject scenarioObject = PrefabUtility.InstantiatePrefab(scenarioPrefab, scene) as GameObject;
                if (scenarioObject == null)
                    throw new InvalidOperationException("IF-ADR-043 scenario prefab could not be instantiated into Persistent Content.");
                QaIfAdr043PhysicalParticipationScenario scenario = scenarioObject.GetComponent<QaIfAdr043PhysicalParticipationScenario>();
                if (scenario == null)
                    throw new InvalidOperationException("IF-ADR-043 scenario prefab is missing its certification component.");
                ConfigureScenarioProvisioningReferences(scenario, provisioning, registration);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, PersistentScene)) throw new InvalidOperationException("Could not save IF-ADR-043 Persistent Content scene.");
            }
            finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        }

        private static void ConfigureScenarioProvisioningReferences(QaIfAdr043PhysicalParticipationScenario scenario,
            LocalPlayerProvisioningAuthoring provisioning, LocalPlayerProvisioningEndpointRegistration registration)
        {
            var serialized = new SerializedObject(scenario);
            serialized.Update();
            Require(serialized, "provisioning").objectReferenceValue = provisioning;
            Require(serialized, "provisioningRegistration").objectReferenceValue = registration;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(scenario);
            PrefabUtility.RecordPrefabInstancePropertyModifications(scenario);

            serialized.Update();
            var missing = new List<string>();
            ValidateScenarioBinding(serialized, "provisioning", provisioning, missing);
            ValidateScenarioBinding(serialized, "provisioningRegistration", registration, missing);
            if (missing.Count > 0)
                throw new InvalidOperationException("IF-ADR-043 scenario provisioning references could not be applied before scene save: " + string.Join(", ", missing) + ".");
        }

        private static OutputChannels ParseChannel(string id)
        {
            int value = id.EndsWith("1") ? 0 : 1;
            return (OutputChannels)(1 << value);
        }

        private static void AddScenesToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            AddScene(scenes, RouteScene);
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
            QaIfAdr043CameraOutputEvidence evidence = LoadAsset<QaIfAdr043CameraOutputEvidence>(Settings + "/QaIfAdr043CameraOutputEvidence.asset");
            PlayerSlotProfile first = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr043_P1.asset");
            PlayerSlotProfile second = LoadAsset<PlayerSlotProfile>(Settings + "/PlayerSlot_QaIfAdr043_P2.asset");
            PlayerSessionProfile session = LoadAsset<PlayerSessionProfile>(Settings + "/PlayerSession_QaIfAdr043.asset");
            SessionCameraAssignmentAsset assignmentAsset = LoadAsset<SessionCameraAssignmentAsset>(Settings + "/CameraAssignment_QaIfAdr043_Individual.asset");
            GameApplicationAsset application = LoadAsset<GameApplicationAsset>(Settings + "/GameApplication_QaIfAdr043.asset");
            GameObject playerPrefab = LoadAsset<GameObject>(Players + "/PF_LocalPlayerHost_QaIfAdr043.prefab");
            RouteAsset route = LoadAsset<RouteAsset>(Settings + "/Route_QaIfAdr043.asset");
            InputActionAsset expectedActions = LoadAsset<InputActionAsset>(Players + "/InputActions_QaIfAdr043.asset");

            if (!session.TryValidate(out string issue) || session.SupportedSlotCount != 2 ||
                session.SupportedSlots[0] != first || session.SupportedSlots[1] != second ||
                !session.InitialJoiningOpen || session.HostProvisioning != PlayerHostProvisioningMode.ManagerProvisioned)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Player Session is not a valid two-Slot ManagerProvisioned baseline. " + issue);

            if (!route.HasPrimaryScene || route.PrimaryScenePath != RouteScene ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(route.PrimaryScenePath) == null)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: startup Route has no saved valid Primary Scene.");

            if (!assignmentAsset.TryBuild(out SessionCameraAssignment assignment, out issue) ||
                assignment.OccurrenceMode != CameraOccurrenceMode.IndividualPerPlayer ||
                assignment.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots ||
                assignment.MemberSlots.Count != 2 || assignment.Outputs.Count != 2 || assignment.MemberOutputs.Count != 2)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Assignment is not the required two-Slot IndividualPerPlayer mapping. " + issue);

            CameraOutputDefinition firstDefinition = LoadAsset<CameraOutputDefinition>(Camera + "/CameraOutput_QaIfAdr043_P1.asset");
            CameraOutputDefinition secondDefinition = LoadAsset<CameraOutputDefinition>(Camera + "/CameraOutput_QaIfAdr043_P2.asset");
            if (!assignment.MemberOutputs.Any(mapping => mapping.PlayerSlotId == first.PlayerSlotId && mapping.OutputId == firstDefinition.OutputId) ||
                !assignment.MemberOutputs.Any(mapping => mapping.PlayerSlotId == second.PlayerSlotId && mapping.OutputId == secondDefinition.OutputId))
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Assignment does not preserve the exact P1/P2 Output identities.");
            if (!assignment.MemberSlots.Contains(first.PlayerSlotId) || !assignment.MemberSlots.Contains(second.PlayerSlotId) ||
                assignment.Outputs.Count(output => output.OutputId == firstDefinition.OutputId || output.OutputId == secondDefinition.OutputId) != 2)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Assignment members and Output definitions must preserve the exact P1/P2 identities.");

            if (!application.PlayerSessionEnabled || application.DefaultPlayerSessionProfile != session ||
                application.StartupRoute != route || application.CameraSession == null ||
                application.CameraSession.OutputPrefabs.Count != 2 || application.StartupCameraAssignments.Count != 1 ||
                application.StartupCameraAssignments[0] != assignmentAsset)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Game Application does not reference the complete startup composition.");
            if (!application.CameraSession.TryValidate(out issue))
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Camera Session Output configuration is invalid. " + issue);

            SerializedObject applicationObject = new SerializedObject(application);
            applicationObject.Update();
            if (Require(applicationObject, "persistentContent.containerScene").objectReferenceValue !=
                AssetDatabase.LoadAssetAtPath<SceneAsset>(PersistentScene))
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Game Application is not bound to the generated Persistent Content scene.");

            ValidatePlayerHostPrefab(playerPrefab, expectedActions);

            GameObject firstOutputPrefab = application.CameraSession.OutputPrefabs[0];
            GameObject secondOutputPrefab = application.CameraSession.OutputPrefabs[1];
            if (firstOutputPrefab != LoadAsset<GameObject>(Camera + "/PF_CameraOutput_QaIfAdr043_P1.prefab") ||
                secondOutputPrefab != LoadAsset<GameObject>(Camera + "/PF_CameraOutput_QaIfAdr043_P2.prefab"))
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Camera Session Output order must be the generated P1 then P2 prefabs.");
            ValidateOutputPrefab(firstOutputPrefab, evidence, first, firstDefinition);
            ValidateOutputPrefab(secondOutputPrefab, evidence, second, secondDefinition);
            GameObject scenarioPrefab = LoadAsset<GameObject>(ScenarioPrefabPath);
            ValidateScenarioPrefab(scenarioPrefab, application, evidence, first, second);

            ValidateBuildSettingsScene(RouteScene);
            ValidateBuildSettingsScene(PersistentScene);

            ValidatePersistentScene(evidence, application, playerPrefab, first, second);
            ValidateRouteScene(route);
        }

        private static void ValidatePlayerHostPrefab(GameObject playerPrefab, InputActionAsset expectedActions)
        {
            PlayerInput[] inputs = playerPrefab.GetComponents<PlayerInput>();
            LocalPlayerHostAuthoring[] hosts = playerPrefab.GetComponents<LocalPlayerHostAuthoring>();
            if (inputs.Length != 1 || hosts.Length != 1)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: generated Host root must have exactly one PlayerInput and one LocalPlayerHostAuthoring.");

            PlayerInput playerInput = inputs[0];
            LocalPlayerHostAuthoring localHost = hosts[0];
            if (localHost.PlayerInput != playerInput || localHost.ActorMount == null ||
                localHost.ActorMount.root != playerPrefab.transform || localHost.PlayerActorRuntimeHostPrefab == null ||
                localHost.PlayerActorRuntimeHostPrefab.GetComponent<PlayerActorDeclaration>() == null)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: LocalPlayerHostAuthoring must reference the same root PlayerInput and a valid authored actor mount/runtime prefab.");

            SerializedObject playerInputObject = new SerializedObject(playerInput);
            playerInputObject.Update();
            if (Require(playerInputObject, "m_Actions").objectReferenceValue != expectedActions ||
                Require(playerInputObject, "m_DefaultActionMap").stringValue != "Player" ||
                Require(playerInputObject, "m_DefaultControlScheme").stringValue != PlayerControlScheme ||
                playerInput.actions != expectedActions || playerInput.defaultActionMap != "Player" ||
                playerInput.defaultControlScheme != PlayerControlScheme)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: serialized and loaded PlayerInput must reference the generated Actions asset, Player map and QA.Keyboard scheme.");

            if (expectedActions.actionMaps.Count != 1 || expectedActions.controlSchemes.Count != 1)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: generated Input Actions asset must contain exactly the authored Player map and QA.Keyboard scheme.");

            InputActionMap playerMap = expectedActions.FindActionMap("Player");
            InputAction move = playerMap != null ? playerMap.FindAction("Move") : null;
            if (playerMap == null || move == null || move.expectedControlType != "Vector2")
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: generated Player map must contain Move with Vector2 control type.");

            InputControlScheme? scheme = expectedActions.FindControlScheme(PlayerControlScheme);
            if (!scheme.HasValue || scheme.Value.name != PlayerControlScheme ||
                scheme.Value.deviceRequirements.Count != 1 ||
                scheme.Value.deviceRequirements[0].controlPath != "<Keyboard>" ||
                scheme.Value.deviceRequirements[0].isOptional)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: QA.Keyboard must require exactly one Keyboard device.");

            InputBinding[] bindings = move.bindings.ToArray();
            if (bindings.Length < 5 || !bindings.Any(binding => binding.isComposite) ||
                !bindings.Any(binding => binding.isPartOfComposite && binding.path == "<Keyboard>/w" && HasBindingGroup(binding, PlayerControlScheme)) ||
                !bindings.Any(binding => binding.isPartOfComposite && binding.path == "<Keyboard>/s" && HasBindingGroup(binding, PlayerControlScheme)) ||
                !bindings.Any(binding => binding.isPartOfComposite && binding.path == "<Keyboard>/a" && HasBindingGroup(binding, PlayerControlScheme)) ||
                !bindings.Any(binding => binding.isPartOfComposite && binding.path == "<Keyboard>/d" && HasBindingGroup(binding, PlayerControlScheme)))
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: persisted Player/Move bindings must contain the QA.Keyboard 2D keyboard directions.");
        }

        private static bool HasBindingGroup(InputBinding binding, string group) =>
            (binding.groups ?? string.Empty).Split(';').Contains(group);

        private static void ValidateBuildSettingsScene(string path)
        {
            EditorBuildSettingsScene[] matches = EditorBuildSettings.scenes.Where(scene => scene.path == path).ToArray();
            if (matches.Length != 1 || !matches[0].enabled || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                throw new InvalidOperationException($"IF-ADR-043 setup validation failed: scene '{path}' must exist and appear exactly once enabled in Build Settings.");
        }

        private static void ValidateOutputPrefab(GameObject prefab, QaIfAdr043CameraOutputEvidence evidence,
            PlayerSlotProfile mappedSlot, CameraOutputDefinition expectedDefinition)
        {
            CameraOutputAuthoring[] outputs = prefab != null
                ? prefab.GetComponentsInChildren<CameraOutputAuthoring>(true)
                : Array.Empty<CameraOutputAuthoring>();
            QaIfAdr043CameraOutputProbe[] probes = prefab != null
                ? prefab.GetComponentsInChildren<QaIfAdr043CameraOutputProbe>(true)
                : Array.Empty<QaIfAdr043CameraOutputProbe>();
            string issue = string.Empty;
            if (outputs.Length != 1 || outputs[0] == null || probes.Length != 1 || probes[0] == null ||
                probes[0].gameObject != outputs[0].gameObject || probes[0].MappedSlot != mappedSlot ||
                outputs[0].OutputDefinition != expectedDefinition ||
                !outputs[0].TryValidateDefinition(out issue) ||
                outputs[0].UnityCamera == null || outputs[0].UnityCamera.gameObject != outputs[0].gameObject ||
                !outputs[0].UnityCamera.enabled || outputs[0].CinemachineBrain == null ||
                outputs[0].CinemachineBrain.gameObject != outputs[0].UnityCamera.gameObject ||
                outputs[0].FallbackCameraRig == null ||
                !outputs[0].FallbackCameraRig.TryValidateForApply(out issue))
                throw new InvalidOperationException($"IF-ADR-043 setup validation failed: Output prefab '{prefab?.name}' is incomplete. {issue}");

            SerializedObject probeObject = new SerializedObject(probes[0]);
            probeObject.Update();
            if (Require(probeObject, "evidence").objectReferenceValue != evidence)
                throw new InvalidOperationException($"IF-ADR-043 setup validation failed: Output prefab '{prefab.name}' is not bound to the generated evidence asset.");
        }

        private static void ValidateScenarioPrefab(GameObject prefab, GameApplicationAsset application,
            QaIfAdr043CameraOutputEvidence evidence, PlayerSlotProfile first, PlayerSlotProfile second)
        {
            QaIfAdr043PhysicalParticipationScenario[] scenarios = prefab != null
                ? prefab.GetComponentsInChildren<QaIfAdr043PhysicalParticipationScenario>(true)
                : Array.Empty<QaIfAdr043PhysicalParticipationScenario>();
            if (scenarios.Length != 1 || scenarios[0] == null || !scenarios[0].enabled || !scenarios[0].gameObject.activeSelf)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: Scenario prefab must contain exactly one enabled certification component.");

            SerializedObject serialized = new SerializedObject(scenarios[0]);
            serialized.Update();
            var mismatchedBindings = new List<string>();
            ValidateScenarioBinding(serialized, "gameApplication", application, mismatchedBindings);
            ValidateScenarioBinding(serialized, "outputEvidence", evidence, mismatchedBindings);
            ValidateScenarioBinding(serialized, "player1", first, mismatchedBindings);
            ValidateScenarioBinding(serialized, "player2", second, mismatchedBindings);
            if (Require(serialized, "provisioning").objectReferenceValue != null ||
                Require(serialized, "provisioningRegistration").objectReferenceValue != null)
                mismatchedBindings.Add("scene-scoped provisioning references must remain unassigned on the prefab");
            if (mismatchedBindings.Count > 0)
                throw new InvalidOperationException("IF-ADR-043 setup validation failed: scenario prefab serialized bindings differ from the generated baseline: " + string.Join(", ", mismatchedBindings) + ".");
        }

        private static void ValidatePersistentScene(QaIfAdr043CameraOutputEvidence evidence,
            GameApplicationAsset application, GameObject playerPrefab, PlayerSlotProfile first, PlayerSlotProfile second)
        {
            Scene scene = OpenSceneForValidation(PersistentScene, out bool openedForValidation);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                PlayerInputManager[] managers = roots.SelectMany(root => root.GetComponentsInChildren<PlayerInputManager>(true)).ToArray();
                LocalPlayerProvisioningAuthoring[] endpoints = roots.SelectMany(root => root.GetComponentsInChildren<LocalPlayerProvisioningAuthoring>(true)).ToArray();
                LocalPlayerProvisioningEndpointRegistration[] registrations = roots.SelectMany(root => root.GetComponentsInChildren<LocalPlayerProvisioningEndpointRegistration>(true)).ToArray();
                QaIfAdr043PhysicalParticipationScenario[] scenarios = roots.SelectMany(root => root.GetComponentsInChildren<QaIfAdr043PhysicalParticipationScenario>(true)).ToArray();
                if (managers.Length != 1 || endpoints.Length != 1 || registrations.Length != 1 || scenarios.Length != 1)
                    throw new InvalidOperationException("IF-ADR-043 setup validation failed: Persistent scene must contain exactly one Manager, Provisioning endpoint, registration and scenario.");

                PlayerInputManager manager = managers[0];
                LocalPlayerProvisioningAuthoring endpoint = endpoints[0];
                if (!manager.isActiveAndEnabled || !endpoint.isActiveAndEnabled || !registrations[0].isActiveAndEnabled ||
                    !scenarios[0].isActiveAndEnabled || manager.gameObject != endpoint.gameObject || manager.maxPlayerCount != 2 ||
                    manager.joinBehavior != PlayerJoinBehavior.JoinPlayersManually ||
                    manager.notificationBehavior != PlayerNotifications.InvokeCSharpEvents || manager.splitScreen ||
                    manager.playerPrefab != playerPrefab ||
                    endpoint.PlayerInputManager != manager || endpoint.LocalPlayerHostPrefab != playerPrefab ||
                    registrations[0].ProvisioningAuthoring != endpoint)
                    throw new InvalidOperationException("IF-ADR-043 setup validation failed: ManagerProvisioned composition must be co-located, manual, two-player and reference the same Local Player Host prefab.");

                SerializedObject scenario = new SerializedObject(scenarios[0]);
                scenario.Update();
                var mismatchedBindings = new List<string>();
                ValidateScenarioBinding(scenario, "gameApplication", application, mismatchedBindings);
                ValidateScenarioBinding(scenario, "provisioning", endpoint, mismatchedBindings);
                ValidateScenarioBinding(scenario, "provisioningRegistration", registrations[0], mismatchedBindings);
                ValidateScenarioBinding(scenario, "outputEvidence", evidence, mismatchedBindings);
                ValidateScenarioBinding(scenario, "player1", first, mismatchedBindings);
                ValidateScenarioBinding(scenario, "player2", second, mismatchedBindings);
                if (mismatchedBindings.Count > 0)
                    throw new InvalidOperationException("IF-ADR-043 setup validation failed: persistent scenario serialized bindings differ from the generated baseline: " + string.Join(", ", mismatchedBindings) + ".");
            }
            finally
            {
                if (openedForValidation) EditorSceneManager.CloseScene(scene, false);
            }
        }

        private static void ValidateScenarioBinding(SerializedObject scenario, string propertyPath,
            Object expected, ICollection<string> mismatchedBindings)
        {
            if (Require(scenario, propertyPath).objectReferenceValue != expected)
                mismatchedBindings.Add(propertyPath);
        }

        private static void ValidateRouteScene(RouteAsset route)
        {
            Scene scene = OpenSceneForValidation(RouteScene, out bool openedForValidation);
            try
            {
                RouteContentContribution[] contributions = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<RouteContentContribution>(true)).ToArray();
                if (contributions.Length != 1)
                    throw new InvalidOperationException("IF-ADR-043 setup validation failed: Route Primary Scene must contain exactly one QA Route Content contribution.");
                SerializedObject contribution = new SerializedObject(contributions[0]);
                contribution.Update();
                if (Require(contribution, "route").objectReferenceValue != route)
                    throw new InvalidOperationException("IF-ADR-043 setup validation failed: Route content contribution does not reference the owning baseline Route.");
                if (!contributions[0].HasExplicitLocalContentId)
                    throw new InvalidOperationException("IF-ADR-043 setup validation failed: Route content contribution requires an explicit local content ID.");
            }
            finally
            {
                if (openedForValidation) EditorSceneManager.CloseScene(scene, false);
            }
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

        private static T LoadAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"IF-ADR-043 setup validation failed: required asset '{path}' is missing or has the wrong type.");
            return asset;
        }

        private static T CreateAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            Object existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null)
                throw new InvalidOperationException($"Cannot create {typeof(T).Name} at '{path}': the path is occupied by {existing.GetType().Name}.");
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            T created = AssetDatabase.LoadAssetAtPath<T>(path);
            if (created == null)
                throw new InvalidOperationException($"Created {typeof(T).Name} could not be loaded from '{path}'.");
            return created;
        }

        private static void Set(Object target, string property, object value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty field = serialized.FindProperty(property);
            if (field == null) throw new InvalidOperationException($"Serialized authoring field '{property}' was not found.");
            if (value is Object objectValue) field.objectReferenceValue = objectValue;
            else if (value is PlayerSlotProfile[] slots)
            {
                field.arraySize = slots.Length;
                for (int index = 0; index < slots.Length; index++) field.GetArrayElementAtIndex(index).objectReferenceValue = slots[index];
            }
            else if (value is List<SessionCameraAssignmentAsset> assignments)
            {
                field.arraySize = assignments.Count;
                for (int index = 0; index < assignments.Count; index++) field.GetArrayElementAtIndex(index).objectReferenceValue = assignments[index];
            }
            else if (value is string text) field.stringValue = text;
            else if (value is bool boolean) field.boolValue = boolean;
            else if (value is int integer) field.intValue = integer;
            else if (value is Enum enumValue) SetEnum(field, enumValue.ToString());
            else throw new InvalidOperationException("Unsupported authored value type.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectList(SerializedObject serialized, string path, params Object[] values)
        {
            SerializedProperty list = Require(serialized, path);
            list.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) list.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static SerializedProperty Require(SerializedObject serialized, string path)
        {
            SerializedProperty property = serialized.FindProperty(path);
            return property ?? throw new InvalidOperationException($"Serialized authoring property '{path}' was not found.");
        }

        private static SerializedProperty Require(SerializedProperty parent, string path)
        {
            SerializedProperty property = parent.FindPropertyRelative(path);
            return property ?? throw new InvalidOperationException($"Serialized authoring property '{parent.propertyPath}.{path}' was not found.");
        }

        private static void SetEnum(SerializedProperty property, string value)
        {
            for (int index = 0; index < property.enumNames.Length; index++)
            {
                if (property.enumNames[index] == value) { property.enumValueIndex = index; return; }
            }
            throw new InvalidOperationException($"Enum value '{value}' is unavailable on '{property.propertyPath}'.");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}

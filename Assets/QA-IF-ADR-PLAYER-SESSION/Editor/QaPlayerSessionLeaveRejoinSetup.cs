using System;
using System.IO;
using System.Linq;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RouteLifecycle;
using Immersive.Framework.UnityInput;
using Immersive.QaFramework.PlayerSession;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Immersive.QaFramework.PlayerSession.Editor
{
    public static class QaPlayerSessionLeaveRejoinSetup
    {
        private const string Root = "Assets/QA-IF-ADR-PLAYER-SESSION";
        private const string Settings = Root + "/Settings";
        private const string Scenes = Root + "/Scenes";
        private const string Players = Root + "/Players";
        private const string BaseApplicationPath = Settings + "/GameApplication_QaPlayerSessionJoin.asset";
        private const string ApplicationPath = Settings + "/GameApplication_QaPlayerLeaveRejoin.asset";
        private const string SessionPath = Settings + "/PlayerSession_QaPlayerSessionJoin.asset";
        private const string SlotPath = Settings + "/PlayerSlot_QaPlayerSessionJoin_P1.asset";
        private const string ActorPath = Players + "/Actor_QaPlayerSessionJoin_Default.asset";
        private const string HostPath = Players + "/PF_LocalPlayerHost_QaPlayerSessionJoin.prefab";
        private const string ActorRuntimePath = Players + "/PF_PlayerActorRuntime_QaPlayerSessionJoin.prefab";
        private const string InputActionsPath = Players + "/InputActions_QaPlayerSessionJoin.asset";
        private const string PauseActionReferencePath = Players + "/PauseAction_QaPlayerSessionJoin.asset";
        private const string PersistentScenePath = Scenes + "/QA_PlayerSessionJoin_Persistent.unity";
        private const string RoutePath = Settings + "/Route_QaPlayerLeaveRejoin.asset";
        private const string ActivityPath = Settings + "/Activity_QaPlayerLeaveRejoin.asset";
        private const string ActivityProfilePath = Settings + "/ActivityContentProfile_QaPlayerLeaveRejoin.asset";
        private const string RouteScenePath = Scenes + "/QA_PlayerLeaveRejoin_Route.unity";
        private const string ActivityScenePath = Scenes + "/QA_PlayerLeaveRejoin_Activity.unity";
        private const string ControlScheme = "QA.Keyboard";
        private const string ScenarioId = "QA-PLAYER-LEAVE-REJOIN-OCCURRENCE";

        [MenuItem("Immersive Framework/QA/Player/Lifecycle/Configure Leave-Rejoin Slice")]
        public static void Configure()
        {
            EnsureNoModifiedOpenScenes();
            PreflightGeneratedPaths();
            ValidateSharedSliceOneComposition();
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Scenes");

            PlayerSessionProfile session = Load<PlayerSessionProfile>(SessionPath);
            PlayerSlotProfile slot = Load<PlayerSlotProfile>(SlotPath);
            ActorProfile actor = Load<ActorProfile>(ActorPath);
            ActivityContentProfileAsset profile = CreateActivityProfile();
            ActivityAsset activity = CreateActivity(profile, slot);
            RouteAsset route = CreateRoute(activity);
            GameApplicationAsset application = CreateApplication(route);
            AssetDatabase.SaveAssets();

            ValidateExistingSceneOwnership();
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                CreateRouteScene();
                CreateActivityScene();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                AddGeneratedScenesToBuildList();
                ValidateGeneratedBaseline(application, session, slot, actor, route, activity, profile);
                Debug.Log("[" + ScenarioId + "] setup='Configured' baseline='SerializedValid' application='GameApplication_QaPlayerLeaveRejoin' " +
                    "route='qa.player.session.leave-rejoin.start' activity='qa.player.session.leave-rejoin' " +
                    "provisioning='ManagerProvisioned' actorResolution='ResolveConfiguredDefault' persistent='shared-slice1' " +
                    "hostPause='contract-valid-unbound-technical-binding' gameplayAdmission='not-required' " +
                    "scenes='new-route,new-activity' sourceFramework='1.1.0-preview.7'. " +
                    "Run Configure again to check idempotence; then inspect the generated Inspectors before Play Mode.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void EnsureNoModifiedOpenScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                if (scene.isDirty || string.IsNullOrEmpty(scene.path))
                    throw new InvalidOperationException("Save and close modified or untitled scenes before configuring the QA Leave/Rejoin slice. No scene changes were made.");
            }
        }

        private static void PreflightGeneratedPaths()
        {
            RequireExpectedAssetType<ActivityContentProfileAsset>(ActivityProfilePath);
            RequireExpectedAssetType<ActivityAsset>(ActivityPath);
            RequireExpectedAssetType<RouteAsset>(RoutePath);
            RequireExpectedAssetType<GameApplicationAsset>(ApplicationPath);
            RequireExpectedAssetType<SceneAsset>(RouteScenePath);
            RequireExpectedAssetType<SceneAsset>(ActivityScenePath);
        }

        private static void ValidateSharedSliceOneComposition()
        {
            GameApplicationAsset sourceApp = Load<GameApplicationAsset>(BaseApplicationPath);
            PlayerSessionProfile session = Load<PlayerSessionProfile>(SessionPath);
            PlayerSlotProfile slot = Load<PlayerSlotProfile>(SlotPath);
            ActorProfile actor = Load<ActorProfile>(ActorPath);
            GameObject hostPrefab = Load<GameObject>(HostPath);
            GameObject actorPrefab = Load<GameObject>(ActorRuntimePath);
            InputActionAsset actions = Load<InputActionAsset>(InputActionsPath);
            InputActionReference pauseAction = Load<InputActionReference>(PauseActionReferencePath);
            SceneAsset persistentScene = Load<SceneAsset>(PersistentScenePath);
            if (sourceApp == null || session == null || slot == null || actor == null || hostPrefab == null ||
                actorPrefab == null || actions == null || pauseAction == null || persistentScene == null)
                throw new InvalidOperationException("Run Configure Session Join Slice first; the canonical shared Slice 1 assets are incomplete.");

            if (!session.TryValidate(out string issue) || session.HostProvisioning != PlayerHostProvisioningMode.ManagerProvisioned ||
                session.ActorResolutionPolicy != PlayerActorResolutionPolicy.ResolveConfiguredDefault ||
                session.SupportedSlotCount != 1 || session.SupportedSlots[0] != slot || slot.DefaultActorProfile != actor ||
                !actor.TryGetActorProfileId(out _, out issue) || sourceApp.DefaultPlayerSessionProfile != session ||
                sourceApp.CameraSession == null || !sourceApp.CameraSession.TryValidate(out issue) ||
                sourceApp.PersistentContent == null || !sourceApp.PersistentContent.IsComplete ||
                sourceApp.PersistentContent.ContainerScenePath != PersistentScenePath)
                throw new InvalidOperationException("Shared Slice 1 application, Session, Slot, Actor, Camera or Persistent Content is invalid. " + issue);

            PlayerInput[] inputs = hostPrefab.GetComponents<PlayerInput>();
            LocalPlayerHostAuthoring[] hosts = hostPrefab.GetComponents<LocalPlayerHostAuthoring>();
            UnityPlayerInputGateAdapter[] gates = hostPrefab.GetComponents<UnityPlayerInputGateAdapter>();
            PlayerPauseInput[] pauseBindings = hostPrefab.GetComponents<PlayerPauseInput>();
            PlayerActorRuntimeHost actorRuntime = actorPrefab.GetComponent<PlayerActorRuntimeHost>();
            InputAction globalPause = actions.FindAction("Global/Pause");
            if (inputs.Length != 1 || hosts.Length != 1 || gates.Length != 1 || pauseBindings.Length != 1 || actorRuntime == null ||
                hosts[0].PlayerInput != inputs[0] || hosts[0].PlayerActorRuntimeHostPrefab != actorRuntime ||
                !hosts[0].TryValidateConfiguration(out issue) || gates[0].PlayerInput != inputs[0] ||
                !gates[0].TryValidateAuthoring(out issue) || pauseAction.action == null ||
                pauseAction.action != globalPause || globalPause == null || globalPause.bindings.Count != 0 ||
                pauseBindings[0].PauseAction != pauseAction || !pauseBindings[0].TryValidateAuthoring(out issue))
                throw new InvalidOperationException("Shared Slice 1 Host does not satisfy the current Session Pause binding contract. Run Configure Session Join Slice after applying its setup update. " + issue);
        }

        private static ActivityContentProfileAsset CreateActivityProfile()
        {
            ActivityContentProfileAsset profile = GetOrCreate<ActivityContentProfileAsset>(ActivityProfilePath);
            SerializedObject so = new SerializedObject(profile);
            so.Update();
            Require(so, "profileId").stringValue = "qa.player.session.leave-rejoin.activity-content";
            SerializedProperty scenes = Require(so, "scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            Require(entry, "contentId").stringValue = "qa.player.session.leave-rejoin.activity.main";
            Require(entry, "scenePath").stringValue = ActivityScenePath;
            Require(entry, "sceneName").stringValue = Path.GetFileNameWithoutExtension(ActivityScenePath);
            SetEnum(Require(entry, "requiredness"), "Required");
            SetEnum(Require(entry, "loadMode"), "Additive");
            SetEnum(Require(entry, "releasePolicy"), "ReleaseOnActivityChange");
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateActivity(ActivityContentProfileAsset profile, PlayerSlotProfile slot)
        {
            ActivityAsset activity = GetOrCreate<ActivityAsset>(ActivityPath);
            Set(activity, "activityId", "qa.player.session.leave-rejoin.activity");
            Set(activity, "activityName", "QA Player Leave and Rejoin");
            Set(activity, "activityContentProfile", profile);
            SetEnum(activity, "playerParticipationProjectionMode", "ExplicitSlots");
            SetEnum(activity, "playerParticipationZeroParticipantPolicy", "Allowed");
            SetEnum(activity, "playerParticipationRequirementLevel", "LogicalActorsPrepared");
            SetObjectArray(activity, "playerParticipationExplicitSlotProfiles", slot);
            return activity;
        }

        private static RouteAsset CreateRoute(ActivityAsset activity)
        {
            RouteAsset route = GetOrCreate<RouteAsset>(RoutePath);
            Set(route, "routeId", "qa.player.session.leave-rejoin.start");
            Set(route, "routeName", "QA Player Leave and Rejoin");
            Set(route, "primaryScenePath", RouteScenePath);
            Set(route, "primarySceneName", Path.GetFileNameWithoutExtension(RouteScenePath));
            Set(route, "startupActivity", activity);
            return route;
        }

        private static GameApplicationAsset CreateApplication(RouteAsset route)
        {
            if (!File.Exists(ApplicationPath) && !AssetDatabase.CopyAsset(BaseApplicationPath, ApplicationPath))
                throw new InvalidOperationException("Could not copy the validated Slice 1 GameApplication template to the Slice 2 output path.");
            GameApplicationAsset application = Load<GameApplicationAsset>(ApplicationPath);
            Set(application, "applicationName", "QA Player Leave and Rejoin");
            SerializedObject so = new SerializedObject(application);
            so.Update();
            Require(so, "startupRoute").objectReferenceValue = route;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            return Load<GameApplicationAsset>(ApplicationPath);
        }

        private static void CreateRouteScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RouteAsset route = Load<RouteAsset>(RoutePath);
            GameObject root = new GameObject("QA Player Leave-Rejoin Route Content");
            RouteContentContribution contribution = root.AddComponent<RouteContentContribution>();
            Set(contribution, "route", route);
            Set(contribution, "localContentId", "qa.player.session.leave-rejoin.route-content");
            SaveScene(scene, RouteScenePath);
        }

        private static void CreateActivityScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ActivityAsset activity = Load<ActivityAsset>(ActivityPath);
            PlayerSlotProfile slot = Load<PlayerSlotProfile>(SlotPath);
            ActorProfile actor = Load<ActorProfile>(ActorPath);
            GameObject root = new GameObject("QA Player Leave-Rejoin Activity Content");
            ActivityContentContribution contribution = root.AddComponent<ActivityContentContribution>();
            Set(contribution, "activity", activity);
            Set(contribution, "localContentId", "qa.player.session.leave-rejoin.activity.main");
            PlayerSessionObserver observer = root.AddComponent<PlayerSessionObserver>();
            SetEnum(observer, "scope", "Activity");
            QaPlayerSessionCommandTriggerSetup.EnsureOpenJoining(
                root,
                LocalPlayerProvisioningConsumerScope.Activity);
            QaPlayerSessionCommandTriggerSetup.EnsureCloseJoining(
                root,
                LocalPlayerProvisioningConsumerScope.Activity);
            QaPlayerSessionCommandTriggerSetup.EnsureLeave(
                root,
                "Leave P1",
                LocalPlayerProvisioningConsumerScope.Activity,
                slot);
            PlayerSessionJoinTrigger join = root.AddComponent<PlayerSessionJoinTrigger>();
            SetEnum(join, "scope", "Activity");
            Set(join, "controlScheme", ControlScheme);
            QaPlayerSessionLeaveRejoinScenario scenario = root.AddComponent<QaPlayerSessionLeaveRejoinScenario>();
            Set(scenario, "sessionObserver", observer);
            Set(scenario, "joinTrigger", join);
            Set(scenario, "expectedSlot", slot);
            Set(scenario, "expectedDefaultActor", actor);
            SaveScene(scene, ActivityScenePath);
        }

        private static void SaveScene(Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path))
                throw new InvalidOperationException("QA-generated scene could not be saved: " + path);
        }

        private static void ValidateExistingSceneOwnership()
        {
            ValidateOwnedSceneIfPresent(RouteScenePath, routeScene =>
            {
                RouteContentContribution[] contributions = Components<RouteContentContribution>(routeScene);
                if (contributions.Length != 1 || contributions[0].Route != Load<RouteAsset>(RoutePath))
                    throw new InvalidOperationException("Existing Leave/Rejoin Route scene is not the expected QA-owned composition. Inspect it before recovery: " + RouteScenePath);
            });
            ValidateOwnedSceneIfPresent(ActivityScenePath, activityScene =>
            {
                if (Components<ActivityContentContribution>(activityScene).Length != 1 ||
                    Components<QaPlayerSessionLeaveRejoinScenario>(activityScene).Length != 1)
                    throw new InvalidOperationException("Existing Leave/Rejoin Activity scene is not the expected QA-owned composition. Inspect it before recovery: " + ActivityScenePath);
            });
        }

        private static void ValidateOwnedSceneIfPresent(string path, Action<Scene> validate)
        {
            if (Load<SceneAsset>(path) == null) return;
            Scene scene = OpenForValidation(path);
            try { validate(scene); }
            finally { CloseAfterValidation(scene, path); }
        }

        private static void AddGeneratedScenesToBuildList()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (string path in new[] { RouteScenePath, ActivityScenePath })
            {
                scenes.RemoveAll(scene => scene.path == path);
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ValidateGeneratedBaseline(
            GameApplicationAsset application,
            PlayerSessionProfile session,
            PlayerSlotProfile slot,
            ActorProfile actor,
            RouteAsset route,
            ActivityAsset activity,
            ActivityContentProfileAsset profile)
        {
            AssetDatabase.SaveAssets();
            application = Load<GameApplicationAsset>(ApplicationPath);
            session = Load<PlayerSessionProfile>(SessionPath);
            slot = Load<PlayerSlotProfile>(SlotPath);
            actor = Load<ActorProfile>(ActorPath);
            route = Load<RouteAsset>(RoutePath);
            activity = Load<ActivityAsset>(ActivityPath);
            profile = Load<ActivityContentProfileAsset>(ActivityProfilePath);
            GameApplicationAsset sourceApp = Load<GameApplicationAsset>(BaseApplicationPath);
            if (application == null || sourceApp == null || session == null || slot == null || actor == null || route == null || activity == null || profile == null)
                throw new InvalidOperationException("Slice 2 generated baseline is missing a typed persisted asset.");
            string issue = string.Empty;
            if (application.StartupRoute != route || application.DefaultPlayerSessionProfile != session ||
                application.CameraSession == null || !application.CameraSession.TryValidate(out issue) ||
                application.PersistentContent == null || !application.PersistentContent.IsComplete ||
                application.PersistentContent.ContainerScenePath != PersistentScenePath ||
                application.PersistentContent.ContainerScene != sourceApp.PersistentContent.ContainerScene ||
                !route.HasValidRouteId || route.StartupActivity != activity || route.PrimaryScenePath != RouteScenePath ||
                !activity.HasValidActivityId || activity.ActivityContentProfile != profile ||
                activity.PlayerParticipationProjectionMode != ActivityParticipationProjectionMode.ExplicitSlots ||
                activity.PlayerParticipationRequirementLevel != PlayerParticipationRequirementLevel.LogicalActorsPrepared ||
                activity.PlayerParticipationExplicitSlotProfiles.Count != 1 || activity.PlayerParticipationExplicitSlotProfiles[0] != slot ||
                slot.DefaultActorProfile != actor || profile.ProfileId != "qa.player.session.leave-rejoin.activity-content" ||
                profile.SceneCount != 1 || profile.Scenes[0].ScenePath != ActivityScenePath)
                throw new InvalidOperationException("Slice 2 application/Route/Activity/Profile references or authored semantics are invalid. " + issue);

            foreach (string scenePath in new[] { RouteScenePath, ActivityScenePath, PersistentScenePath })
            {
                if (Load<SceneAsset>(scenePath) == null || !IsSceneEnabled(scenePath))
                    throw new InvalidOperationException("Generated Scene is missing or disabled in Editor Build Settings: " + scenePath);
            }

            ValidatePersistedRouteScene(route);
            ValidatePersistedActivityScene(activity, profile, slot, actor);
            ValidatePersistentScene();
        }

        private static void ValidatePersistedRouteScene(RouteAsset route)
        {
            Scene scene = OpenForValidation(RouteScenePath);
            try
            {
                RouteContentContribution[] contributions = Components<RouteContentContribution>(scene);
                if (contributions.Length != 1 || contributions[0].Route != route || !contributions[0].HasExplicitLocalContentId)
                    throw new InvalidOperationException("Persisted Slice 2 Route scene does not contain exactly one contribution bound to its Route.");
            }
            finally { CloseAfterValidation(scene, RouteScenePath); }
        }

        private static void ValidatePersistedActivityScene(ActivityAsset activity, ActivityContentProfileAsset profile, PlayerSlotProfile slot, ActorProfile actor)
        {
            Scene scene = OpenForValidation(ActivityScenePath);
            try
            {
                ActivityContentContribution[] contributions = Components<ActivityContentContribution>(scene);
                PlayerSessionObserver[] observers = Components<PlayerSessionObserver>(scene);
                PlayerSessionJoinTrigger[] joins = Components<PlayerSessionJoinTrigger>(scene);
                QaPlayerSessionLeaveRejoinScenario[] scenarios = Components<QaPlayerSessionLeaveRejoinScenario>(scene);
                string issue = string.Empty;
                if (contributions.Length != 1 || contributions[0].Activity != activity || !contributions[0].HasExplicitLocalContentId ||
                    observers.Length != 1 || joins.Length != 1 || scenarios.Length != 1 ||
                    observers[0].Scope != LocalPlayerProvisioningConsumerScope.Activity ||
                    joins[0].Scope != LocalPlayerProvisioningConsumerScope.Activity ||
                    !observers[0].TryValidateConfiguration(out issue))
                    throw new InvalidOperationException("Persisted Slice 2 Activity contribution, scoped Observer/Join or scenario cardinality is invalid. " + issue);

                SerializedObject scenario = new SerializedObject(scenarios[0]);
                scenario.Update();
                if (Require(scenario, "sessionObserver").objectReferenceValue != observers[0] ||
                    Require(scenario, "joinTrigger").objectReferenceValue != joins[0] ||
                    Require(scenario, "expectedSlot").objectReferenceValue != slot ||
                    Require(scenario, "expectedDefaultActor").objectReferenceValue != actor ||
                    !scenarios[0].enabled || !scenarios[0].gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Persisted Slice 2 Scenario references are incomplete or do not point to the canonical shared Slot/Actor.");

                SerializedObject serializedProfile = new SerializedObject(profile);
                serializedProfile.Update();
                SerializedProperty entries = Require(serializedProfile, "scenes");
                if (entries.arraySize != 1)
                    throw new InvalidOperationException("Slice 2 Activity Content Profile must have one Required scene entry.");
                SerializedProperty entry = entries.GetArrayElementAtIndex(0);
                if (Require(entry, "contentId").stringValue != "qa.player.session.leave-rejoin.activity.main" ||
                    Require(entry, "scenePath").stringValue != ActivityScenePath ||
                    Require(entry, "sceneName").stringValue != Path.GetFileNameWithoutExtension(ActivityScenePath) ||
                    !HasEnumValue(Require(entry, "requiredness"), "Required") ||
                    !HasEnumValue(Require(entry, "loadMode"), "Additive") ||
                    !HasEnumValue(Require(entry, "releasePolicy"), "ReleaseOnActivityChange"))
                    throw new InvalidOperationException("Persisted Slice 2 Activity Content Profile scene entry is invalid.");
            }
            finally { CloseAfterValidation(scene, ActivityScenePath); }
        }

        private static void ValidatePersistentScene()
        {
            Scene scene = OpenForValidation(PersistentScenePath);
            try
            {
                LocalPlayerProvisioningEndpointRegistration[] registrations = Components<LocalPlayerProvisioningEndpointRegistration>(scene);
                LocalPlayerProvisioningAuthoring[] endpoints = Components<LocalPlayerProvisioningAuthoring>(scene);
                PlayerInputManager[] managers = Components<PlayerInputManager>(scene);
                GameObject expectedHost = Load<GameObject>(HostPath);
                string issue = string.Empty;
                if (registrations.Length != 1 || endpoints.Length != 1 || managers.Length != 1 ||
                    !registrations[0].TryResolveAuthoring(out LocalPlayerProvisioningAuthoring registered, out issue) || registered != endpoints[0] ||
                    endpoints[0].PlayerInputManager != managers[0] || endpoints[0].LocalPlayerHostPrefab != expectedHost ||
                    managers[0].playerPrefab != expectedHost || managers[0].joinBehavior != PlayerJoinBehavior.JoinPlayersManually)
                    throw new InvalidOperationException("Shared Persistent Content does not register exactly one valid Manager-Provisioned endpoint/PlayerInputManager. " + issue);
            }
            finally { CloseAfterValidation(scene, PersistentScenePath); }
        }

        private static T[] Components<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Postflight Scene is not loaded.");
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
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

        private static bool IsSceneEnabled(string path) =>
            EditorBuildSettings.scenes.Any(scene => scene.path == path && scene.enabled);

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (File.Exists(path)) throw new InvalidOperationException("QA output path has an unexpected asset type: " + path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        private static void RequireExpectedAssetType<T>(string path) where T : Object
        {
            if (File.Exists(path) && AssetDatabase.LoadAssetAtPath<T>(path) == null)
                throw new InvalidOperationException("Refusing to overwrite a QA output path with unexpected content/type: " + path);
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
            SerializedObject so = new SerializedObject(target);
            so.Update();
            Require(so, name).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void Set(Object target, string name, string value)
        {
            SerializedObject so = new SerializedObject(target);
            so.Update();
            Require(so, name).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(Object target, string name, string value)
        {
            SerializedObject so = new SerializedObject(target);
            so.Update();
            SetEnum(Require(so, name), value);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(SerializedProperty property, string value)
        {
            int index = Array.IndexOf(property.enumNames, value);
            if (index < 0) throw new InvalidOperationException("Enum value not found for " + property.propertyPath + ": " + value);
            property.enumValueIndex = index;
        }

        private static bool HasEnumValue(SerializedProperty property, string value) =>
            property.enumValueIndex >= 0 && property.enumValueIndex < property.enumNames.Length && property.enumNames[property.enumValueIndex] == value;

        private static void SetObjectArray(Object target, string name, params Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            so.Update();
            SerializedProperty list = Require(so, name);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}

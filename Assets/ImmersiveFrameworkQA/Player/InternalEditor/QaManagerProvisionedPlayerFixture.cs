using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Internal.Editor
{
    public sealed class QaManagerProvisionedPlayerFixtureContext
    {
        internal QaManagerProvisionedPlayerFixtureContext(
            ImmersiveFrameworkSettingsAsset settings,
            GameApplicationAsset application,
            PlayerSessionProfile sessionProfile,
            LocalPlayerProvisioningAuthoring provisioning,
            PlayerInputManager playerInputManager,
            GameObject playerHostPrefab,
            LocalPlayerProvisioningHostRegistration hostRegistration)
        {
            Settings = settings;
            Application = application;
            SessionProfile = sessionProfile;
            Provisioning = provisioning;
            PlayerInputManager = playerInputManager;
            PlayerHostPrefab = playerHostPrefab;
            HostRegistration = hostRegistration;
        }

        public ImmersiveFrameworkSettingsAsset Settings { get; }
        public GameApplicationAsset Application { get; }
        public PlayerSessionProfile SessionProfile { get; }
        public LocalPlayerProvisioningAuthoring Provisioning { get; }
        public PlayerInputManager PlayerInputManager { get; }
        public GameObject PlayerHostPrefab { get; }
        public LocalPlayerProvisioningHostRegistration HostRegistration { get; }
    }

    /// <summary>
    /// Idempotent real-join fixture. Creates one reusable Local Player technical-host prefab and
    /// installs one explicit manual PlayerInputManager + provisioning authoring and host
    /// registration in QA_UIGlobal.
    /// </summary>
    public static class QaManagerProvisionedPlayerFixture
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Manager Provisioned/Prepare Fixture";
        private const string GameplayAdmissionSetupMenuPath =
            "Immersive Framework/QA/Player/Manager Provisioned/Prepare Gameplay Admission Fixture";
        private const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/LocalPlayerRuntimeIntegration";
        private const string ActionsPath = RootFolder + "/LocalPlayerInputActions.asset";
        private const string PlayerPrefabPath = RootFolder + "/LocalPlayerHost.prefab";
        private const string SessionProfilePath =
            RootFolder + "/CanonicalPlayerSessionProfile.asset";
        private const string DivergentPlayerPrefabPath =
            RootFolder + "/LocalPlayerHost.Divergent.prefab";
        private const string RestoreCanonicalAfterPlayKey =
            "ImmersiveFrameworkQA.LocalPlayerProvisioning.RestoreCanonicalAfterPlay";
        private const string FixtureName = "Local Player Provisioning";
        private const string TargetSceneName = "QA_UIGlobal";
        private const string JoinEvidenceBindingPath = "<Keyboard>/space";

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeRestoration()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem("Immersive Framework/QA/Player/Manager Provisioned/Prepare Prefab Divergence Fixture")]
        private static void PrepareDivergenceFromMenu()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Local Player divergence fixture setup must run outside Play Mode.");
            }

            PrepareAndValidate();
            InputActionAsset actions = CreateOrUpdateInputActions();
            GameObject divergentPrefab = CreateOrUpdatePlayerPrefab(
                actions,
                DivergentPlayerPrefabPath,
                "Divergent Local Player Host");
            string scenePath = FindUniqueScenePath(TargetSceneName);
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            PlayerInputManager manager = ResolveOrCreateManager(scene);
            LocalPlayerProvisioningAuthoring authoring =
                manager.GetComponent<LocalPlayerProvisioningAuthoring>();
            if (authoring == null || authoring.LocalPlayerHostPrefab == null)
            {
                throw new InvalidOperationException(
                    "Canonical Local Player authoring was not created before divergence setup.");
            }

            manager.playerPrefab = divergentPrefab;
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SessionState.SetBool(RestoreCanonicalAfterPlayKey, true);

            Debug.Log(
                "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='Applied' " +
                $"scenario='DivergentManagerPrefab' scene='{scenePath}' " +
                $"authoredPrefab='{authoring.LocalPlayerHostPrefab.name}' " +
                $"managerPrefab='{divergentPrefab.name}' restoreAfterPlay='True'.");
        }

        [MenuItem(GameplayAdmissionSetupMenuPath)]
        private static void PrepareGameplayAdmissionRegressionFromMenu()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Player Gameplay Admission setup must run outside Play Mode.");
            }

            PrepareAndValidate();
            Debug.Log(
                "[PLAYER_GAMEPLAY_ADMISSION_SETUP] status='Prepared' " +
                $"scene='{TargetSceneName}' expectedInitialPlayers='0' " +
                "next='Enter fresh Play Mode and run Player Gameplay Admission Regression before any Pause or Player preflight.'.");
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode ||
                !SessionState.GetBool(RestoreCanonicalAfterPlayKey, false))
            {
                return;
            }

            try
            {
                PrepareAndValidate();
                SessionState.EraseBool(RestoreCanonicalAfterPlayKey);
                Debug.Log(
                    "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='Restored' scenario='DivergentManagerPrefab'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='RestoreFailed' " +
                    $"scenario='DivergentManagerPrefab' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
            }
        }

        public static void ValidatePreparedComposition(
            QaManagerProvisionedPlayerFixtureContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            RequireCanonicalPlayerSessionConfiguration(
                context.Settings,
                context.Application,
                context.SessionProfile);

            Scene scene = context.PlayerInputManager != null
                ? context.PlayerInputManager.gameObject.scene
                : default;
            if (!scene.isLoaded ||
                !string.Equals(scene.name, TargetSceneName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Manager-Provisioned context requires loaded scene '{TargetSceneName}', but scene is '{scene.name}'.");
            }

            PlayerInputManager manager = context.PlayerInputManager;
            LocalPlayerProvisioningAuthoring authoring = context.Provisioning;
            if (manager == null || authoring == null ||
                !ReferenceEquals(authoring.PlayerInputManager, manager))
            {
                throw new InvalidOperationException(
                    "Manager-Provisioned context does not retain the canonical provisioning and PlayerInputManager references.");
            }

            if (context.PlayerHostPrefab == null ||
                !ReferenceEquals(authoring.LocalPlayerHostPrefab, context.PlayerHostPrefab) ||
                manager.playerPrefab != null)
            {
                throw new InvalidOperationException(
                    "Manager-Provisioned context requires its authored Local Player Host Prefab and an empty manager playerPrefab in Edit Mode.");
            }

            LocalPlayerHostAuthoring host =
                context.PlayerHostPrefab.GetComponent<LocalPlayerHostAuthoring>();
            string hostIssue = string.Empty;
            if (host == null || !host.TryValidateConfiguration(out hostIssue))
            {
                throw new InvalidOperationException(
                    "Manager-Provisioned context requires a valid Local Player Host Prefab. " +
                    hostIssue);
            }

            bool bridgeIsValid = QaPlayerSessionQaSupport.TryValidateManagerBridge(
                context.SessionProfile,
                manager,
                out string bridgeIssue);
            if (manager.joinBehavior != PlayerJoinBehavior.JoinPlayersManually ||
                manager.notificationBehavior != PlayerNotifications.InvokeCSharpEvents ||
                !bridgeIsValid)
            {
                throw new InvalidOperationException(
                    "Manager-Provisioned context requires manual C# event joining " +
                    "and a PlayerInputManager bridge derived from PlayerSessionProfile. " +
                    bridgeIssue);
            }

            if (context.HostRegistration == null ||
                !ReferenceEquals(context.HostRegistration.ProvisioningAuthoring, authoring))
            {
                throw new InvalidOperationException(
                    "Manager-Provisioned context does not retain the canonical provisioning registration.");
            }
        }

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            if (EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Canonical Local Player runtime fixture setup must run outside Play Mode.");
            }

            PrepareAndValidate();
        }

        public static QaManagerProvisionedPlayerFixtureContext Prepare()
        {
            try
            {
                EnsureFolder(RootFolder);
                InputActionAsset actions = CreateOrUpdateInputActions();
                GameObject playerPrefab = CreateOrUpdatePlayerPrefab(
                    actions,
                    PlayerPrefabPath,
                    "Local Player Host");
                PlayerSessionProfile session = ConfigureCanonicalPlayerSession(
                    out ImmersiveFrameworkSettingsAsset settings,
                    out GameApplicationAsset application);
                string scenePath = FindUniqueScenePath(TargetSceneName);

                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    throw new OperationCanceledException(
                        "Manager-Provisioned fixture preparation was cancelled because modified scenes were not saved.");
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                PlayerInputManager manager = ResolveOrCreateManager(scene);
                ConfigureManager(manager, session);

                LocalPlayerProvisioningAuthoring authoring =
                    manager.GetComponent<LocalPlayerProvisioningAuthoring>();
                if (authoring == null)
                {
                    authoring = manager.gameObject.AddComponent<LocalPlayerProvisioningAuthoring>();
                }

                var serializedAuthoring = new SerializedObject(authoring);
                serializedAuthoring.FindProperty("playerInputManager").objectReferenceValue = manager;
                serializedAuthoring.FindProperty("localPlayerHostPrefab").objectReferenceValue = playerPrefab;
                serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();

                LocalPlayerProvisioningHostRegistration registration =
                    ResolveOrCreateRegistration(scene, manager, authoring);

                EditorUtility.SetDirty(manager.gameObject);
                EditorUtility.SetDirty(manager);
                EditorUtility.SetDirty(authoring);
                EditorUtility.SetDirty(registration);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                settings = Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
                application = settings != null
                    ? settings.ActiveGameApplication
                    : null;
                session = application != null
                    ? application.DefaultPlayerSessionProfile
                    : null;
                RequireCanonicalPlayerSessionConfiguration(
                    settings,
                    application,
                    session);

                LocalPlayerHostAuthoring host =
                    playerPrefab.GetComponent<LocalPlayerHostAuthoring>();
                var context = new QaManagerProvisionedPlayerFixtureContext(
                    settings,
                    application,
                    session,
                    authoring,
                    manager,
                    playerPrefab,
                    registration);
                Debug.Log(
                    "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='Applied' " +
                    $"scene='{scenePath}' fixture='{manager.gameObject.name}' " +
                    $"registration='{registration.name}' " +
                    $"prefab='{PlayerPrefabPath}' host='{host.name}' actorMount='{host.ActorMount.name}' " +
                    $"application='{application.name}' session='{session.name}' supportedSlots='{session.SupportedSlotCount}' " +
                    $"joinBehavior='{manager.joinBehavior}' notificationBehavior='{manager.notificationBehavior}' " +
                    $"maxPlayers='{manager.maxPlayerCount}'.");
                return context;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        public static QaManagerProvisionedPlayerFixtureContext PrepareAndValidate()
        {
            QaManagerProvisionedPlayerFixtureContext context = Prepare();
            ValidatePreparedComposition(context);
            Debug.Log(
                "[LOCAL_PLAYER_RUNTIME_INTEGRATION_SETUP] status='Prepared' " +
                $"application='{context.Application.name}' session='{context.SessionProfile.name}' " +
                $"supportedSlots='{context.SessionProfile.SupportedSlotCount}' " +
                $"maxPlayers='{context.PlayerInputManager.maxPlayerCount}'.");
            return context;
        }

        private static InputActionAsset CreateOrUpdateInputActions()
        {
            InputActionAsset actions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            bool changed = false;
            if (actions == null)
            {
                actions = ScriptableObject.CreateInstance<InputActionAsset>();
                AssetDatabase.CreateAsset(actions, ActionsPath);
                changed = true;
            }

            string expectedName = Path.GetFileNameWithoutExtension(ActionsPath);
            if (!string.Equals(actions.name, expectedName, StringComparison.Ordinal))
            {
                actions.name = expectedName;
                changed = true;
            }

            InputActionMap gameplay = actions.FindActionMap("Gameplay", false);
            if (gameplay == null)
            {
                gameplay = actions.AddActionMap("Gameplay");
                changed = true;
            }

            InputAction joinEvidence = gameplay.FindAction("JoinEvidence", false);
            if (joinEvidence == null)
            {
                joinEvidence = gameplay.AddAction("JoinEvidence", InputActionType.Button);
                changed = true;
            }

            changed |= EnsureBinding(joinEvidence, JoinEvidenceBindingPath);

            if (changed)
            {
                EditorUtility.SetDirty(actions);
            }

            AssetDatabase.SaveAssets();
            return actions;
        }

        private static PlayerSessionProfile ConfigureCanonicalPlayerSession(
            out ImmersiveFrameworkSettingsAsset settings,
            out GameApplicationAsset application)
        {
            settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null || settings.ActiveGameApplication == null)
            {
                throw new InvalidOperationException(
                    "Canonical Local Player setup requires an active Game Application.");
            }

            application = settings.ActiveGameApplication;
            PlayerSessionProfile session =
                AssetDatabase.LoadAssetAtPath<PlayerSessionProfile>(
                    SessionProfilePath);
            if (session == null)
            {
                throw new InvalidOperationException(
                    "Canonical Local Player setup requires the persisted Player Session Profile asset with its Supported Slots.");
            }

            var slots = new List<PlayerSlotProfile>(session.SupportedSlots);
            if (slots.Count == 0)
            {
                throw new InvalidOperationException(
                    "Canonical Local Player setup requires at least one Supported Slot on PlayerSessionProfile.");
            }

            var serializedSession = new SerializedObject(session);
            SerializedProperty supportedSlots =
                serializedSession.FindProperty("supportedSlots");
            supportedSlots.arraySize = slots.Count;
            for (int index = 0; index < slots.Count; index++)
            {
                supportedSlots.GetArrayElementAtIndex(index)
                    .objectReferenceValue = slots[index];
            }

            serializedSession.FindProperty("initialJoiningOpen").boolValue = false;
            serializedSession.FindProperty("hostProvisioning").intValue =
                (int)PlayerHostProvisioningMode.ManagerProvisioned;
            serializedSession.FindProperty("actorResolutionPolicy").intValue =
                (int)PlayerActorResolutionPolicy.ResolveConfiguredDefault;
            serializedSession.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(session);

            var serializedApplication = new SerializedObject(application);
            serializedApplication.FindProperty("playerSessionEnabled").boolValue = true;
            serializedApplication.FindProperty("defaultPlayerSessionProfile")
                .objectReferenceValue = session;
            serializedApplication.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);

            RequireCanonicalPlayerSessionConfiguration(
                settings,
                application,
                session);
            return session;
        }

        private static void RequireCanonicalPlayerSessionConfiguration(
            ImmersiveFrameworkSettingsAsset settings,
            GameApplicationAsset application,
            PlayerSessionProfile session)
        {
            string sessionIssue = string.Empty;
            if (settings == null ||
                application == null ||
                !ReferenceEquals(settings.ActiveGameApplication, application) ||
                !application.PlayerSessionEnabled ||
                session == null ||
                !ReferenceEquals(application.DefaultPlayerSessionProfile, session) ||
                !session.TryValidate(
                    out sessionIssue))
            {
                throw new InvalidOperationException(
                    "Canonical Local Player setup requires an enabled, valid " +
                    "Player Session Profile. " +
                    (settings == null || application == null
                        ? "Active Game Application is missing."
                        : !ReferenceEquals(settings.ActiveGameApplication, application)
                            ? "Settings no longer points to the prepared Game Application."
                        : !application.PlayerSessionEnabled
                            ? "Player Session is disabled."
                            : session == null ||
                              !ReferenceEquals(
                                  application.DefaultPlayerSessionProfile,
                                  session)
                                ? "Game Application no longer points to the prepared Player Session Profile."
                                : sessionIssue));
            }

            if (session.HostProvisioning !=
                    PlayerHostProvisioningMode.ManagerProvisioned ||
                session.ActorResolutionPolicy !=
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault)
            {
                throw new InvalidOperationException(
                    "Canonical Local Player setup requires Manager-Provisioned Hosts " +
                    "and configured default Actor resolution.");
            }

            if (session.SupportedSlotCount <= 0)
            {
                throw new InvalidOperationException(
                    "Canonical Local Player setup requires PlayerSessionProfile.SupportedSlotCount greater than zero.");
            }
        }

        private static bool EnsureBinding(InputAction action, string path)
        {
            for (int index = 0; index < action.bindings.Count; index++)
            {
                if (string.Equals(action.bindings[index].path, path, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            action.AddBinding(path);
            return true;
        }

        private static GameObject CreateOrUpdatePlayerPrefab(
            InputActionAsset actions,
            string prefabPath,
            string displayName)
        {
            GameObject temporary = new GameObject(displayName);
            try
            {
                PlayerInput playerInput = temporary.AddComponent<PlayerInput>();
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Gameplay";

                var actorMountObject = new GameObject("ActorMount");
                actorMountObject.transform.SetParent(temporary.transform, false);

                LocalPlayerHostAuthoring host =
                    temporary.AddComponent<LocalPlayerHostAuthoring>();
                var serializedHost = new SerializedObject(host);
                serializedHost.FindProperty("playerInput").objectReferenceValue = playerInput;
                serializedHost.FindProperty("actorMount").objectReferenceValue =
                    actorMountObject.transform;
                serializedHost.ApplyModifiedPropertiesWithoutUndo();

                UnityPlayerInputGateAdapter gate =
                    temporary.AddComponent<UnityPlayerInputGateAdapter>();
                var serializedGate = new SerializedObject(gate);
                serializedGate.FindProperty("playerInput").objectReferenceValue =
                    playerInput;
                serializedGate.FindProperty("gameplayActionMapName").stringValue =
                    "Gameplay";
                SetOptionalBoolean(serializedGate, "logStateChanges", false);
                SetOptionalBoolean(serializedGate, "logMissingRuntimeOnce", false);
                SetOptionalBoolean(serializedGate, "logMissingTargetOnce", false);
                serializedGate.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
                    temporary,
                    prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Could not create Local Player Host prefab at '{prefabPath}'.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static PlayerInputManager ResolveOrCreateManager(Scene scene)
        {
            var managers = new List<PlayerInputManager>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                managers.AddRange(root.GetComponentsInChildren<PlayerInputManager>(true));
            }

            if (managers.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.name}' contains '{managers.Count}' PlayerInputManager components. Local Player runtime integration requires one Session manager.");
            }

            if (managers.Count == 1)
            {
                return managers[0];
            }

            var fixture = new GameObject(FixtureName);
            SceneManager.MoveGameObjectToScene(fixture, scene);
            return fixture.AddComponent<PlayerInputManager>();
        }

        private static void ConfigureManager(
            PlayerInputManager manager,
            PlayerSessionProfile session)
        {
            manager.gameObject.name = FixtureName;
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            manager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            // The authored Local Player Host Prefab is the product authority. Leave this
            // technical manager field empty so the official runtime bootstrap materializes it.
            manager.playerPrefab = null;

            QaPlayerSessionQaSupport.ConfigureManagerBridge(session, manager);
        }

        private static LocalPlayerProvisioningHostRegistration ResolveOrCreateRegistration(
            Scene scene,
            PlayerInputManager manager,
            LocalPlayerProvisioningAuthoring authoring)
        {
            var registrations = new List<LocalPlayerProvisioningHostRegistration>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                registrations.AddRange(
                    root.GetComponentsInChildren<LocalPlayerProvisioningHostRegistration>(true));
            }

            if (registrations.Count > 1)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.name}' contains '{registrations.Count}' Local Player Provisioning Host Registrations. Exactly one is required.");
            }

            LocalPlayerProvisioningHostRegistration registration = registrations.Count == 1
                ? registrations[0]
                : manager.gameObject.AddComponent<LocalPlayerProvisioningHostRegistration>();
            var serializedRegistration = new SerializedObject(registration);
            serializedRegistration.FindProperty("provisioningAuthoring").objectReferenceValue = authoring;
            serializedRegistration.ApplyModifiedPropertiesWithoutUndo();
            return registration;
        }

        private static void SetOptionalBoolean(
            SerializedObject serialized,
            string propertyName,
            bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static string FindUniqueScenePath(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            var exactMatches = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName,
                        StringComparison.Ordinal))
                {
                    exactMatches.Add(path);
                }
            }

            if (exactMatches.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one Scene named '{sceneName}', but found '{exactMatches.Count}'.");
            }

            return exactMatches[0];
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }
                current = next;
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

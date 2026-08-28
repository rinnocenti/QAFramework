using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using ImmersiveFrameworkQA.Player.Editor;
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
    /// Wires the canonical QA persistent content with the Player functional's
    /// Manager-Provisioned host, Input Manager bridge and Session profile.
    /// </summary>
    public static class QaManagerProvisionedPlayerFixture
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Manager Provisioned/Prepare Fixture";
        private const string FixtureName = "Local Player Provisioning";
        private const string TargetSceneName = "QA_UIGlobal";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            PrepareAndValidate();
        }

        public static QaManagerProvisionedPlayerFixtureContext Prepare()
        {
            try
            {
                PlayerQaSceneBuilder.EnsureSharedFixtures();
                GameObject playerPrefab = RequirePrefab(PlayerQaPaths.ManagerHostPath);
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
                application = settings != null ? settings.ActiveGameApplication : null;
                session = application != null ? application.DefaultPlayerSessionProfile : null;
                RequireCanonicalPlayerSessionConfiguration(settings, application, session);

                LocalPlayerHostAuthoring host = playerPrefab.GetComponent<LocalPlayerHostAuthoring>();
                var context = new QaManagerProvisionedPlayerFixtureContext(
                    settings,
                    application,
                    session,
                    authoring,
                    manager,
                    playerPrefab,
                    registration);
                Debug.Log(
                    "[QA_PLAYER_SETUP] status='Applied' fixture='ManagerProvisioned' " +
                    $"scene='{scenePath}' prefab='{PlayerQaPaths.ManagerHostPath}' " +
                    $"host='{host.name}' application='{application.name}' " +
                    $"session='{session.name}' supportedSlots='{session.SupportedSlotCount}' " +
                    $"maxPlayers='{manager.maxPlayerCount}'.");
                return context;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[QA_PLAYER_SETUP] status='Failed' fixture='ManagerProvisioned' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        public static QaManagerProvisionedPlayerFixtureContext PrepareAndValidate()
        {
            QaManagerProvisionedPlayerFixtureContext context = Prepare();
            ValidatePreparedComposition(context);
            Debug.Log(
                "[QA_PLAYER_SETUP] status='Prepared' fixture='ManagerProvisioned' " +
                $"application='{context.Application.name}' session='{context.SessionProfile.name}' " +
                $"supportedSlots='{context.SessionProfile.SupportedSlotCount}' " +
                $"maxPlayers='{context.PlayerInputManager.maxPlayerCount}'.");
            return context;
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
            if (manager == null ||
                authoring == null ||
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
                    "Manager-Provisioned context requires its authored Local Player Host Prefab and an empty manager playerPrefab.");
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

        private static PlayerSessionProfile ConfigureCanonicalPlayerSession(
            out ImmersiveFrameworkSettingsAsset settings,
            out GameApplicationAsset application)
        {
            settings = Resources.Load<ImmersiveFrameworkSettingsAsset>(
                ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null || settings.ActiveGameApplication == null)
            {
                throw new InvalidOperationException(
                    "Player QA setup requires an active Game Application.");
            }

            application = settings.ActiveGameApplication;
            PlayerSessionProfile session = AssetDatabase.LoadAssetAtPath<PlayerSessionProfile>(
                PlayerQaPaths.ManagerSessionPath);
            if (session == null)
            {
                throw new InvalidOperationException(
                    "Player QA setup requires the Manager Player Session Profile. Run Configure Player QA.");
            }

            var serializedApplication = new SerializedObject(application);
            serializedApplication.FindProperty("playerSessionEnabled").boolValue = true;
            serializedApplication.FindProperty("defaultPlayerSessionProfile")
                .objectReferenceValue = session;
            serializedApplication.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(application);
            RequireCanonicalPlayerSessionConfiguration(settings, application, session);
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
                !session.TryValidate(out sessionIssue))
            {
                throw new InvalidOperationException(
                    "Player QA requires an enabled, valid Player Session Profile on the active Game Application. " +
                    sessionIssue);
            }

            if (session.HostProvisioning != PlayerHostProvisioningMode.ManagerProvisioned ||
                session.ActorResolutionPolicy != PlayerActorResolutionPolicy.ResolveConfiguredDefault)
            {
                throw new InvalidOperationException(
                    "Canonical Player QA requires Manager-Provisioned Hosts and configured default Actor resolution.");
            }
        }

        private static GameObject RequirePrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Required Player QA prefab is missing at '{path}'. Run Configure Player QA.");
            }

            return prefab;
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
                    $"Scene '{scene.name}' contains '{managers.Count}' PlayerInputManager components. Player QA requires one Session manager.");
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

        private static string FindUniqueScenePath(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            var exactMatches = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), sceneName, StringComparison.Ordinal))
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

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

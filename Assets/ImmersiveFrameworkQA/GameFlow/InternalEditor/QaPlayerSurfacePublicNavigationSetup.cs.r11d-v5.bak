using System;
using System.IO;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.Lifecycle;
using ImmersiveFrameworkQA.Player;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using ImmersiveFrameworkQA.UnityBuildSurface;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Edit Mode authoring for the public Player Surface navigation fixture.
    /// Places ActivityRequestTrigger and Route consumer binding in QA Hub so
    /// Framework composition can bind them at boot/route start without QA
    /// calling privileged bind APIs.
    /// </summary>
    internal static class QaPlayerSurfacePublicNavigationSetup
    {
        private const string Prefix = "[QA_PLAYER_SURFACE_PUBLIC_NAV]";
        private const string MenuPath =
            "Immersive Framework/QA/Player/Public Surface/Prepare Fixture";
        private const string Root =
            "Assets/ImmersiveFrameworkQA/Player/Surface";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string GlobalUiScenePath =
            "Assets/ImmersiveFrameworkQA/UnityBuildSurface/Scenes/QA_UIGlobal.unity";
        internal const string ContentScenePath =
            "Assets/ImmersiveFrameworkQA/Player/Scenes/QA_PlayerSurfacePublicActivityContent.unity";
        private const string ContentProfilePath =
            Root + "/QA_PlayerSurfacePublic_ContentProfile.asset";
        private const string ActivityPath =
            Root + "/QA_PlayerSurfacePublic_WaitCoveredActivity.asset";
        private const string SecondaryActivityPath =
            Root + "/QA_PlayerSurfacePublic_SecondaryWaitCoveredActivity.asset";
        private const string PlayerExcludedActivityPath =
            Root + "/QA_PlayerSurfacePublic_PlayerExcludedActivity.asset";
        private const string PlayerSlotPath =
            "Assets/ImmersiveFrameworkQA/Player/Profiles/SlotsProfiles/PlayerSlotProfileP1.asset";
        private const string PreparedKey =
            "ImmersiveFrameworkQA.QA_PLAYER_SURFACE.PublicNavPrepared";

        [MenuItem(MenuPath, true)]
        private static bool ValidatePrepare() => !EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        private static void PrepareFromMenu()
        {
            PrepareForCertification();
        }

        /// <summary>
        /// Prepares authored navigation assets and Hub scene fixture.
        /// Throws on any failure and never leaves a false Prepared marker.
        /// </summary>
        internal static void PrepareForCertification()
        {
            Require(!EditorApplication.isPlaying,
                "Public navigation fixture preparation must run in Edit Mode.");

            SessionState.EraseBool(PreparedKey);

            try
            {
                QaManagerProvisionedPlayerFixtureContext managerContext =
                    QaManagerProvisionedPlayerFixture.PrepareAndValidate();
                Require(
                    managerContext != null &&
                    managerContext.Application != null &&
                    managerContext.SessionProfile != null &&
                    managerContext.PlayerInputManager != null &&
                    managerContext.Provisioning != null &&
                    ReferenceEquals(
                        managerContext.Application.DefaultPlayerSessionProfile,
                        managerContext.SessionProfile),
                    "Manager-Provisioned fixture returned an incomplete or mismatched explicit Player Session context.");
                string applicationName = managerContext.Application.name;
                string sessionName = managerContext.SessionProfile.name;
                int supportedSlotCount = managerContext.SessionProfile.SupportedSlotCount;
                int serializedPlayerLimit = managerContext.PlayerInputManager.maxPlayerCount;
                ConfigureGlobalUiFixture(managerContext);
                EnsureFolder(Root);
                PlayerSlotProfile slot =
                    AssetDatabase.LoadAssetAtPath<PlayerSlotProfile>(PlayerSlotPath);
                Require(slot != null && slot.PlayerSlotId.IsValid,
                    $"Primary Player Slot missing at '{PlayerSlotPath}'.");

                SceneAsset contentScene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(ContentScenePath);
                Require(contentScene != null,
                    $"Activity content scene missing at '{ContentScenePath}'.");
                EnsureContentSceneEnabledInBuildSettings();

                ActivityContentProfileAsset contentProfile =
                    CreateOrUpdateContentProfile(contentScene);
                ActivityAsset activity = CreateOrUpdateActivity(
                    ActivityPath,
                    "qa.player.surface.public.waitcovered.a",
                    "QA Player Surface Public WaitCovered A",
                    "Authored Player-representing Activity A for public Player Surface certification.",
                    contentProfile,
                    slot,
                    true);
                ActivityAsset secondaryActivity = CreateOrUpdateActivity(
                    SecondaryActivityPath,
                    "qa.player.surface.public.waitcovered.b",
                    "QA Player Surface Public WaitCovered B",
                    "Authored distinct Player-representing Activity B for contextual reprojection certification.",
                    contentProfile,
                    slot,
                    true);
                ActivityAsset excludedActivity = CreateOrUpdateActivity(
                    PlayerExcludedActivityPath,
                    "qa.player.surface.public.player-excluded",
                    "QA Player Surface Public Player Excluded",
                    "Authored Activity that deliberately excludes Player contextual participation.",
                    contentProfile,
                    slot,
                    false);

                Scene hub = EditorSceneManager.OpenScene(
                    HubScenePath,
                    OpenSceneMode.Single);
                Require(hub.IsValid(),
                    $"Could not open Hub scene '{HubScenePath}'.");

                GameObject root = FindOrCreateRoot(hub);
                QaPlayerSurfacePublicNavigationFixture fixture =
                    ConfigureFixtureRoot(
                        root,
                        activity,
                        secondaryActivity,
                        excludedActivity,
                        slot);
                string surfaceIssue = string.Empty;
                bool authoredSurfaceIsValid = fixture != null &&
                    fixture.TryValidateAuthoredSurface(out surfaceIssue);
                Require(
                    authoredSurfaceIsValid,
                    string.IsNullOrWhiteSpace(surfaceIssue)
                        ? "Public navigation fixture failed validation after authoring."
                        : surfaceIssue);
                Require(
                    fixture.EnterActivityTrigger != null &&
                    fixture.ClearActivityTrigger != null &&
                    fixture.RouteConsumerBinding != null,
                    "Public navigation fixture is incomplete after ConfigureFixtureRoot.");

                EditorSceneManager.MarkSceneDirty(hub);
                Require(
                    EditorSceneManager.SaveScene(hub),
                    $"Could not save Hub scene '{HubScenePath}'.");

                ConfigureActivityContentFixture();
                Require(
                    !IsGlobalUiSourceSceneLoaded(),
                    "Player Surface Prepare must leave the UIGlobal source scene closed so Framework bootstrap owns its single runtime load.");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                SessionState.SetBool(PreparedKey, true);

                Debug.Log(
                    $"{Prefix} status='Prepared' " +
                    $"activity='{activity.ActivityName}' " +
                    $"content='{ContentScenePath}' " +
                    $"hub='{HubScenePath}' " +
                    $"root='{QaPlayerSurfacePublicNavigationFixture.RootObjectName}' " +
                    $"globalUi='{GlobalUiScenePath}' " +
                    $"application='{applicationName}' session='{sessionName}' " +
                    $"supportedSlots='{supportedSlotCount}' maxPlayers='{serializedPlayerLimit}' " +
                    "binding='composition-time Route primary + Framework ActivityRequestTrigger bind' " +
                    "next='Enter fresh Play Mode; composition binds the authored trigger before public RequestActivity'.");
            }
            catch (Exception exception)
            {
                CloseGlobalUiSourceSceneIfLoaded();
                SessionState.EraseBool(PreparedKey);
                Debug.LogError(
                    $"{Prefix} status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        internal static bool IsPrepared =>
            SessionState.GetBool(PreparedKey, false);

        internal static void RequirePrepared()
        {
            Require(
                IsPrepared,
                $"Player Surface public navigation fixture is not prepared. Run '{MenuPath}'.");
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(ActivityPath);
            Require(
                activity != null,
                $"Authored public Activity missing at '{ActivityPath}'.");
        }

        private static ActivityContentProfileAsset CreateOrUpdateContentProfile(
            SceneAsset contentScene)
        {
            ActivityContentProfileAsset profile =
                AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(
                    ContentProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ActivityContentProfileAsset>();
                AssetDatabase.CreateAsset(profile, ContentProfilePath);
            }

            var serialized = new SerializedObject(profile);
            RequireProperty(serialized, "profileId").stringValue =
                "qa.player.surface.public.content";
            SerializedProperty scenes = RequireProperty(serialized, "scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            RequireProperty(entry, "contentId").stringValue =
                "qa.player.surface.public.activity-content";
            RequireProperty(entry, "scenePath").stringValue = ContentScenePath;
            RequireProperty(entry, "sceneName").stringValue =
                Path.GetFileNameWithoutExtension(ContentScenePath);
            SetEnumName(RequireProperty(entry, "requiredness"), "Required");
            SetEnumName(RequireProperty(entry, "loadMode"), "Additive");
            SetEnumName(
                RequireProperty(entry, "releasePolicy"),
                "ReleaseOnActivityChange");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateOrUpdateActivity(
            string assetPath,
            string activityId,
            string activityName,
            string description,
            ActivityContentProfileAsset contentProfile,
            PlayerSlotProfile slot,
            bool representsPlayer)
        {
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(assetPath);
            if (activity == null)
            {
                activity = ScriptableObject.CreateInstance<ActivityAsset>();
                AssetDatabase.CreateAsset(activity, assetPath);
            }

            var serialized = new SerializedObject(activity);
            RequireProperty(serialized, "activityId").stringValue =
                activityId;
            RequireProperty(serialized, "activityName").stringValue =
                activityName;
            RequireProperty(serialized, "description").stringValue =
                description;
            SetEnumName(
                RequireProperty(serialized, "playerParticipationProjectionMode"),
                (representsPlayer
                    ? ActivityParticipationProjectionMode.ExplicitSlots
                    : ActivityParticipationProjectionMode.NoSlots).ToString());
            SetEnumName(
                RequireProperty(serialized, "playerParticipationZeroParticipantPolicy"),
                (representsPlayer
                    ? ActivityParticipationZeroParticipantPolicy.Rejected
                    : ActivityParticipationZeroParticipantPolicy.Allowed).ToString());
            SetEnumName(
                RequireProperty(serialized, "playerParticipationRequirementLevel"),
                (representsPlayer
                    ? PlayerParticipationRequirementLevel.GameplayReady
                    : PlayerParticipationRequirementLevel.None).ToString());
            SerializedProperty slots = RequireProperty(
                serialized,
                "playerParticipationExplicitSlotProfiles");
            slots.arraySize = representsPlayer ? 1 : 0;
            if (representsPlayer)
            {
                slots.GetArrayElementAtIndex(0).objectReferenceValue = slot;
            }
            RequireProperty(serialized, "activityContentProfile")
                .objectReferenceValue = contentProfile;
            SetEnumName(
                RequireProperty(serialized, "activityEntryReadinessPolicy"),
                ActivityEntryReadinessPolicy.WaitCovered.ToString());
            SetEnumName(
                RequireProperty(serialized, "visualTransitionMode"),
                ActivityVisualTransitionMode.FadeWithLoading.ToString());
            SetEnumName(
                RequireProperty(serialized, "transitionGateMode"),
                TransitionGateMode.InputInteractionAndGameplay.ToString());
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);

            Require(
                activity.EntryReadinessPolicy ==
                    ActivityEntryReadinessPolicy.WaitCovered &&
                activity.VisualTransitionMode ==
                    ActivityVisualTransitionMode.FadeWithLoading &&
                activity.TransitionGateMode ==
                    TransitionGateMode.InputInteractionAndGameplay &&
                activity.PlayerParticipationRequirementLevel ==
                    (representsPlayer
                        ? PlayerParticipationRequirementLevel.GameplayReady
                        : PlayerParticipationRequirementLevel.None) &&
                activity.HasActivityContentProfile,
                "Authored public Activity did not retain the canonical " +
                "WaitCovered/FadeWithLoading/player configuration.");
            return activity;
        }

        private static GameObject FindOrCreateRoot(Scene hub)
        {
            GameObject[] roots = hub.GetRootGameObjects();
            for (int index = 0; index < roots.Length; index++)
            {
                if (string.Equals(
                        roots[index].name,
                        QaPlayerSurfacePublicNavigationFixture.RootObjectName,
                        StringComparison.Ordinal))
                {
                    return roots[index];
                }
            }

            var root = new GameObject(
                QaPlayerSurfacePublicNavigationFixture.RootObjectName);
            SceneManager.MoveGameObjectToScene(root, hub);
            return root;
        }

        private static QaPlayerSurfacePublicNavigationFixture ConfigureFixtureRoot(
            GameObject root,
            ActivityAsset activity,
            ActivityAsset secondaryActivity,
            ActivityAsset excludedActivity,
            PlayerSlotProfile slot)
        {
            QaPlayerSurfacePublicNavigationFixture fixture =
                root.GetComponent<QaPlayerSurfacePublicNavigationFixture>();
            if (fixture == null)
            {
                fixture = root.AddComponent<QaPlayerSurfacePublicNavigationFixture>();
            }

            Require(
                fixture != null,
                "Failed to add runtime QaPlayerSurfacePublicNavigationFixture. " +
                "Confirm the type lives in a non-Editor assembly.");

            ActivityRequestTrigger enter =
                FindOrCreateChildTrigger(root, "EnterActivityTrigger");
            ActivityRequestTrigger enterSecondary =
                FindOrCreateChildTrigger(root, "EnterSecondaryActivityTrigger");
            ActivityRequestTrigger enterExcluded =
                FindOrCreateChildTrigger(root, "EnterPlayerExcludedActivityTrigger");
            ActivityRequestTrigger clear =
                FindOrCreateChildTrigger(root, "ClearActivityTrigger");
            LocalPlayerProvisioningConsumerAccessBinding binding =
                root.GetComponent<LocalPlayerProvisioningConsumerAccessBinding>();
            if (binding == null)
            {
                binding = root.AddComponent<
                    LocalPlayerProvisioningConsumerAccessBinding>();
            }

            Require(enter != null, "Failed to create enter ActivityRequestTrigger.");
            Require(clear != null, "Failed to create clear ActivityRequestTrigger.");
            Require(
                binding != null,
                "Failed to create Route LocalPlayerProvisioningConsumerAccessBinding.");
            LocalPlayerProvisioningConsumerAccessBinding wrongScopeBinding =
                FindOrCreateChildBinding(
                    root,
                    "WrongScopeBinding",
                    LocalPlayerProvisioningConsumerScope.Activity);
            LocalPlayerProvisioningConsumerAccessBinding destroyProbeBinding =
                FindOrCreateChildBinding(
                    root,
                    "DestroyProbeBinding",
                    LocalPlayerProvisioningConsumerScope.Route);
            ConfigureTrigger(enter, activity, "qa.player.surface.public.enter");
            ConfigureTrigger(
                enterSecondary,
                secondaryActivity,
                "qa.player.surface.public.enter-secondary");
            ConfigureTrigger(
                enterExcluded,
                excludedActivity,
                "qa.player.surface.public.enter-player-excluded");
            ConfigureTrigger(clear, activity, "qa.player.surface.public.clear");
            ApplyScope(binding, LocalPlayerProvisioningConsumerScope.Route);

            fixture.Configure(
                activity,
                secondaryActivity,
                excludedActivity,
                enter,
                enterSecondary,
                enterExcluded,
                clear,
                binding,
                wrongScopeBinding,
                destroyProbeBinding,
                slot);
            Require(
                fixture.TryValidateAuthoredSurface(out string issue),
                issue);
            EditorUtility.SetDirty(fixture);
            EditorUtility.SetDirty(root);
            return fixture;
        }

        internal static void RequirePreparedForCurrentPlayMode()
        {
            Require(EditorApplication.isPlaying,
                "Public Player Surface fixture requires Play Mode.");
        }

        private static void ConfigureGlobalUiFixture(
            QaManagerProvisionedPlayerFixtureContext managerContext)
        {
            Require(managerContext != null,
                "Public Player Surface fixture requires a Manager-Provisioned context.");
            PlayerSessionProfile session = managerContext.SessionProfile;
            PlayerInputManager manager = managerContext.PlayerInputManager;
            LocalPlayerActorSelectionRequestAuthoring found =
                managerContext.ActorSelectionRequest;
            Require(session != null && manager != null && found != null,
                "Manager-Provisioned context is missing Session, PlayerInputManager or Actor Selection authoring.");

            Scene globalUi = manager.gameObject.scene;
            Require(
                globalUi.IsValid() && globalUi.isLoaded &&
                string.Equals(globalUi.path, GlobalUiScenePath, StringComparison.Ordinal),
                "Manager-Provisioned context PlayerInputManager is not in the expected UIGlobal scene.");
            Require(
                ReferenceEquals(found.gameObject, manager.gameObject) &&
                ReferenceEquals(found.ProvisioningAuthoring, managerContext.Provisioning),
                "Manager-Provisioned context Actor Selection authoring does not belong to its provisioning object.");
            Require(
                found.TryValidateConfiguration(out string actorSelectionIssue),
                actorSelectionIssue);
            QaPlayerSessionQaSupport.ConfigureManagerBridge(session, manager);
            Require(
                QaPlayerSessionQaSupport.TryValidateManagerBridge(
                    session,
                    manager,
                    out string bridgeIssue),
                bridgeIssue);

            QaPlayerSurfaceGlobalUiFixture fixture =
                found.gameObject.GetComponent<QaPlayerSurfaceGlobalUiFixture>();
            if (fixture == null)
            {
                fixture = found.gameObject.AddComponent<QaPlayerSurfaceGlobalUiFixture>();
            }

            Require(
                fixture != null,
                "Failed to add runtime QaPlayerSurfaceGlobalUiFixture to the UIGlobal Player composition.");
            QaLoadingSurfaceVisibilityHoldAdapter[] loadingSurfaces =
                Array.Empty<QaLoadingSurfaceVisibilityHoldAdapter>();
            var loadingCandidates = new System.Collections.Generic.List<
                QaLoadingSurfaceVisibilityHoldAdapter>();
            foreach (GameObject sceneRoot in globalUi.GetRootGameObjects())
            {
                loadingCandidates.AddRange(
                    sceneRoot.GetComponentsInChildren<
                        QaLoadingSurfaceVisibilityHoldAdapter>(true));
            }
            loadingSurfaces = loadingCandidates.ToArray();
            Require(
                loadingSurfaces.Length == 1,
                $"UIGlobal scene requires exactly one Loading Surface adapter; found '{loadingSurfaces.Length}'.");
            fixture.Configure(found, loadingSurfaces[0]);
            Require(
                fixture.TryValidateAuthoredSurface(out string fixtureIssue),
                fixtureIssue);

            int fixtureCount = 0;
            GameObject[] fixtureRoots = globalUi.GetRootGameObjects();
            for (int rootIndex = 0;
                 rootIndex < fixtureRoots.Length;
                 rootIndex++)
            {
                fixtureCount += fixtureRoots[rootIndex]
                    .GetComponentsInChildren<QaPlayerSurfaceGlobalUiFixture>(true)
                    .Length;
            }

            Require(
                fixtureCount == 1,
                $"UIGlobal scene '{GlobalUiScenePath}' requires exactly one " +
                $"QaPlayerSurfaceGlobalUiFixture; found '{fixtureCount}'.");

            EditorUtility.SetDirty(fixture);
            EditorUtility.SetDirty(found.gameObject);
            EditorSceneManager.MarkSceneDirty(globalUi);
            Require(
                EditorSceneManager.SaveScene(globalUi),
                $"Could not save UIGlobal scene '{GlobalUiScenePath}'.");
        }

        private static void ConfigureActivityContentFixture()
        {
            Scene content = SceneManager.GetSceneByPath(ContentScenePath);
            if (!content.IsValid() || !content.isLoaded)
            {
                content = EditorSceneManager.OpenScene(
                    ContentScenePath,
                    OpenSceneMode.Additive);
            }

            Require(
                content.IsValid() && content.isLoaded,
                $"Could not open Player Surface Activity content scene '{ContentScenePath}'.");

            GameObject root = null;
            foreach (GameObject candidate in content.GetRootGameObjects())
            {
                if (candidate != null && string.Equals(
                        candidate.name,
                        QaPlayerSurfaceActivityConsumerFixture.RootObjectName,
                        StringComparison.Ordinal))
                {
                    Require(root == null,
                        "Player Surface Activity content contains duplicate QA fixture roots.");
                    root = candidate;
                }
            }

            if (root == null)
            {
                root = new GameObject(
                    QaPlayerSurfaceActivityConsumerFixture.RootObjectName);
                SceneManager.MoveGameObjectToScene(root, content);
            }

            LocalPlayerProvisioningConsumerAccessBinding binding =
                root.GetComponent<LocalPlayerProvisioningConsumerAccessBinding>() ??
                root.AddComponent<LocalPlayerProvisioningConsumerAccessBinding>();
            ApplyScope(binding, LocalPlayerProvisioningConsumerScope.Activity);

            QaPlayerSurfaceActivityConsumerFixture fixture =
                root.GetComponent<QaPlayerSurfaceActivityConsumerFixture>() ??
                root.AddComponent<QaPlayerSurfaceActivityConsumerFixture>();
            fixture.Configure(binding);
            Require(fixture.TryValidateAuthoredSurface(out string issue), issue);

            EditorUtility.SetDirty(binding);
            EditorUtility.SetDirty(fixture);
            EditorUtility.SetDirty(root);
            EditorSceneManager.MarkSceneDirty(content);
            Require(
                EditorSceneManager.SaveScene(content),
                $"Could not save Player Surface Activity content scene '{ContentScenePath}'.");
            Require(
                EditorSceneManager.CloseScene(content, true),
                $"Could not close Player Surface Activity content scene '{ContentScenePath}'.");
        }

        private static void EnsureContentSceneEnabledInBuildSettings()
        {
            EditorBuildSettingsScene[] current =
                EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            int matchIndex = -1;
            int matchCount = 0;

            for (int index = 0; index < current.Length; index++)
            {
                EditorBuildSettingsScene scene = current[index];
                if (scene == null ||
                    !string.Equals(
                        scene.path,
                        ContentScenePath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                matchIndex = index;
                matchCount++;
            }

            Require(
                matchCount <= 1,
                $"Player Surface Activity content scene appears '{matchCount}' times in Build Settings. path='{ContentScenePath}'.");

            if (matchIndex >= 0 && current[matchIndex].enabled)
            {
                return;
            }

            var updated =
                new System.Collections.Generic.List<EditorBuildSettingsScene>(current);
            var enabledScene =
                new EditorBuildSettingsScene(ContentScenePath, true);

            if (matchIndex >= 0)
            {
                updated[matchIndex] = enabledScene;
            }
            else
            {
                updated.Add(enabledScene);
            }

            EditorBuildSettings.scenes = updated.ToArray();
        }

        private static bool IsGlobalUiSourceSceneLoaded()
        {
            Scene scene = SceneManager.GetSceneByPath(GlobalUiScenePath);
            return scene.IsValid() && scene.isLoaded;
        }

        private static void CloseGlobalUiSourceSceneIfLoaded()
        {
            Scene scene = SceneManager.GetSceneByPath(GlobalUiScenePath);
            if (scene.IsValid() && scene.isLoaded && SceneManager.sceneCount > 1)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static ActivityRequestTrigger FindOrCreateChildTrigger(
            GameObject root,
            string childName)
        {
            Transform child = root.transform.Find(childName);
            GameObject childObject = child != null
                ? child.gameObject
                : new GameObject(childName);
            if (child == null)
            {
                childObject.transform.SetParent(root.transform, false);
            }

            return childObject.GetComponent<ActivityRequestTrigger>() ??
                childObject.AddComponent<ActivityRequestTrigger>();
        }

        private static LocalPlayerProvisioningConsumerAccessBinding
            FindOrCreateChildBinding(
                GameObject root,
                string childName,
                LocalPlayerProvisioningConsumerScope scope)
        {
            Transform child = root.transform.Find(childName);
            GameObject childObject = child != null
                ? child.gameObject
                : new GameObject(childName);
            if (child == null)
            {
                childObject.transform.SetParent(root.transform, false);
            }

            LocalPlayerProvisioningConsumerAccessBinding binding =
                childObject.GetComponent<
                    LocalPlayerProvisioningConsumerAccessBinding>() ??
                childObject.AddComponent<
                    LocalPlayerProvisioningConsumerAccessBinding>();
            ApplyScope(binding, scope);
            return binding;
        }

        private static void ConfigureTrigger(
            ActivityRequestTrigger trigger,
            ActivityAsset activity,
            string reason)
        {
            var serialized = new SerializedObject(trigger);
            RequireProperty(serialized, "targetActivity").objectReferenceValue =
                activity;
            RequireProperty(serialized, "reason").stringValue = reason;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            trigger.TargetActivity = activity;
        }

        private static void ApplyScope(
            LocalPlayerProvisioningConsumerAccessBinding binding,
            LocalPlayerProvisioningConsumerScope scope)
        {
            var serialized = new SerializedObject(binding);
            SerializedProperty scopeProperty = RequireProperty(serialized, "scope");
            int index = Array.IndexOf(scopeProperty.enumNames, scope.ToString());
            Require(index >= 0, $"Scope enum lacks '{scope}'.");
            scopeProperty.enumValueIndex = index;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property = serialized.FindProperty(name);
            Require(property != null, $"Missing property '{name}'.");
            return property;
        }

        private static SerializedProperty RequireProperty(
            SerializedProperty parent,
            string name)
        {
            SerializedProperty property = parent.FindPropertyRelative(name);
            Require(property != null, $"Missing relative property '{name}'.");
            return property;
        }

        private static void SetEnumName(SerializedProperty property, string value)
        {
            int index = Array.IndexOf(property.enumNames, value);
            Require(index >= 0, $"Enum '{value}' missing on '{property.propertyPath}'.");
            property.enumValueIndex = index;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
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

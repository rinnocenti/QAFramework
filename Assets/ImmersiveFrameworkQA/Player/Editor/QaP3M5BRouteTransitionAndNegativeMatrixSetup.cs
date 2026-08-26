using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
using Immersive.Framework.UnityInput;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.Player.P3M5B;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent P3M5B fixture for real Route transitions and the Scene Local Player
    /// automatic-authoring negative matrix. All generated content is QA-only.
    /// </summary>
    public static class QaP3M5BRouteTransitionAndNegativeMatrixSetup
    {
        private enum PlayerSceneShape
        {
            ValidSingle = 0,
            DuplicateSlot = 10,
            MissingActor = 20,
            MismatchedProfile = 30
        }

        private const string MenuPath =
            "Immersive Framework/QA/Player/Scene Provided/Prepare Fixture";
        private const string RegressionMenuPath =
            "Immersive Framework/QA/Player/Scene Provided/Run Integration";
        private const string HubRoutePath =
            "Assets/ImmersiveFrameworkQA/Hub/Routes/QA_HubRoute.asset";
        private const string HubScenePath =
            "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const string LocalPlayerInputActionsPath =
            "Assets/ImmersiveFrameworkQA/Player/LocalPlayerRuntimeIntegration/LocalPlayerInputActions.asset";
        private const string GameplayActionMapName = "Gameplay";

        internal const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/P3M5B";

        internal const string RouteAPrimaryScenePath =
            RootFolder + "/P3M5B_RouteA.unity";
        internal const string RouteBPrimaryScenePath =
            RootFolder + "/P3M5B_RouteB.unity";
        internal const string RouteAActivityScenePath =
            RootFolder + "/P3M5B_RouteA_PlayerActivity.unity";
        internal const string RouteBActivityScenePath =
            RootFolder + "/P3M5B_RouteB_PlayerActivity.unity";
        internal const string FailedFirstAdoptionPrimaryScenePath =
            RootFolder + "/P3M5B_FailedFirstAdoption_Route.unity";
        internal const string FailedFirstAdoptionActivityScenePath =
            RootFolder + "/P3M5B_FailedFirstAdoption_PlayerActivity.unity";
        internal const string FailedReprojectionPrimaryScenePath =
            RootFolder + "/P3M5B_FailedReprojection_Route.unity";
        internal const string FailedReprojectionActivityScenePath =
            RootFolder + "/P3M5B_FailedReprojection_PlayerActivity.unity";
        internal const string DuplicateSlotScenePath =
            RootFolder + "/P3M5B_Negative_DuplicateSlot.unity";
        internal const string MissingActorScenePath =
            RootFolder + "/P3M5B_Negative_MissingActor.unity";
        internal const string MismatchedProfileScenePath =
            RootFolder + "/P3M5B_Negative_MismatchedProfile.unity";
        internal const string UndeclaredSurfaceScenePath =
            RootFolder + "/P3M5B_Negative_UndeclaredSurface.unity";
        internal const string ActorPrefabPath =
            RootFolder + "/P3M5B_SceneLogicalPlayerActor.prefab";
        internal const string ActorProfilePath =
            RootFolder + "/P3M5B_SceneActorProfile.asset";
        internal const string AlternateActorProfilePath =
            RootFolder + "/P3M5B_AlternateActorProfile.asset";

        internal const string RouteAContentPath =
            RootFolder + "/P3M5B_RouteA_ActivityContent.asset";
        internal const string RouteBContentPath =
            RootFolder + "/P3M5B_RouteB_ActivityContent.asset";
        internal const string FailedFirstAdoptionContentPath =
            RootFolder + "/P3M5B_FailedFirstAdoption_ActivityContent.asset";
        internal const string FailedReprojectionContentPath =
            RootFolder + "/P3M5B_FailedReprojection_ActivityContent.asset";
        internal const string DuplicateSlotContentPath =
            RootFolder + "/P3M5B_Negative_DuplicateSlot_Content.asset";
        internal const string MissingActorContentPath =
            RootFolder + "/P3M5B_Negative_MissingActor_Content.asset";
        internal const string MismatchedProfileContentPath =
            RootFolder + "/P3M5B_Negative_MismatchedProfile_Content.asset";
        internal const string RouteAActivityPath =
            RootFolder + "/P3M5B_RouteA_StartupActivity.asset";
        internal const string RouteBActivityPath =
            RootFolder + "/P3M5B_RouteB_StartupActivity.asset";
        internal const string FailedFirstAdoptionActivityPath =
            RootFolder + "/P3M5B_FailedFirstAdoption_Activity.asset";
        internal const string FailedReprojectionActivityPath =
            RootFolder + "/P3M5B_FailedReprojection_Activity.asset";
        internal const string DuplicateSlotActivityPath =
            RootFolder + "/P3M5B_Negative_DuplicateSlot_Activity.asset";
        internal const string MissingActorActivityPath =
            RootFolder + "/P3M5B_Negative_MissingActor_Activity.asset";
        internal const string MismatchedProfileActivityPath =
            RootFolder + "/P3M5B_Negative_MismatchedProfile_Activity.asset";
        internal const string UndeclaredSurfaceActivityPath =
            RootFolder + "/P3M5B_Negative_UndeclaredSurface_Activity.asset";

        internal const string RouteAPath =
            RootFolder + "/P3M5B_RouteA.asset";
        internal const string RouteBPath =
            RootFolder + "/P3M5B_RouteB.asset";
        internal const string FailedFirstAdoptionRoutePath =
            RootFolder + "/P3M5B_FailedFirstAdoption_Route.asset";
        internal const string FailedReprojectionRoutePath =
            RootFolder + "/P3M5B_FailedReprojection_Route.asset";

        internal const string AuthoredActorId =
            "qa.p3m5b.scene-player.authored";
        internal const string ActorProfileId =
            "qa.p3m5b.scene-player.profile";
        internal const string AlternateActorProfileId =
            "qa.p3m5b.scene-player.profile.alternate";

        internal const string RouteAActivityId =
            "qa.p3m5b.activity.route-a.startup";
        internal const string RouteBActivityId =
            "qa.p3m5b.activity.route-b.startup";
        internal const string FailedFirstAdoptionActivityId =
            "qa.p3m5b.activity.failed-first-adoption";
        internal const string FailedReprojectionActivityId =
            "qa.p3m5b.activity.failed-reprojection";
        internal const string DuplicateSlotActivityId =
            "qa.p3m5b.activity.negative.duplicate-slot";
        internal const string MissingActorActivityId =
            "qa.p3m5b.activity.negative.missing-actor";
        internal const string MismatchedProfileActivityId =
            "qa.p3m5b.activity.negative.mismatched-profile";
        internal const string UndeclaredSurfaceActivityId =
            "qa.p3m5b.activity.negative.undeclared-surface";

        [MenuItem(MenuPath, true)]
        private static bool ValidateApply()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(MenuPath)]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[P3M5B_ROUTE_TRANSITION_NEGATIVE_MATRIX_FIXTURE] " +
                    "status='RejectedPlayMode' message='Exit Play Mode before applying the fixture.'.");
                return;
            }

            try
            {
                EnsureFolder(RootFolder);
                PlayerSlotProfile[] slots = PrepareSceneProvidedSession();
                GameObject actorPrefab = CreateOrUpdateActorPrefab();
                ActorProfile actorProfile = CreateOrUpdateActorProfile(
                    ActorProfilePath,
                    ActorProfileId,
                    "P3M5B Scene Player",
                    actorPrefab);
                ActorProfile alternateProfile = CreateOrUpdateActorProfile(
                    AlternateActorProfilePath,
                    AlternateActorProfileId,
                    "P3M5B Alternate Scene Player",
                    actorPrefab);

                CreateOrUpdateEmptyScene(
                    RouteAPrimaryScenePath,
                    "P3M5B Route A Primary");
                CreateOrUpdateEmptyScene(
                    RouteBPrimaryScenePath,
                    "P3M5B Route B Primary");
                CreateOrUpdateEmptyScene(
                    FailedFirstAdoptionPrimaryScenePath,
                    "P3M5B Failed First Adoption Primary");
                CreateOrUpdateEmptyScene(
                    FailedReprojectionPrimaryScenePath,
                    "P3M5B Failed Reprojection Primary");
                CreateOrUpdatePlayerScene(
                    RouteAActivityScenePath,
                    PlayerSceneShape.ValidSingle,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Route A Activity");
                CreateOrUpdatePlayerScene(
                    RouteBActivityScenePath,
                    PlayerSceneShape.ValidSingle,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Route B Activity");
                CreateOrUpdatePlayerScene(
                    FailedFirstAdoptionActivityScenePath,
                    PlayerSceneShape.MismatchedProfile,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Failed First Adoption Activity");
                CreateOrUpdatePlayerScene(
                    FailedReprojectionActivityScenePath,
                    PlayerSceneShape.MismatchedProfile,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Failed Reprojection Activity");
                CreateOrUpdatePlayerScene(
                    DuplicateSlotScenePath,
                    PlayerSceneShape.DuplicateSlot,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Duplicate Slot");
                CreateOrUpdatePlayerScene(
                    MissingActorScenePath,
                    PlayerSceneShape.MissingActor,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Missing Actor");
                CreateOrUpdatePlayerScene(
                    MismatchedProfileScenePath,
                    PlayerSceneShape.MismatchedProfile,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Mismatched Profile");
                CreateOrUpdatePlayerScene(
                    UndeclaredSurfaceScenePath,
                    PlayerSceneShape.ValidSingle,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Undeclared Surface");
                string[] buildScenes =
                {
                    RouteAPrimaryScenePath,
                    RouteBPrimaryScenePath,
                    RouteAActivityScenePath,
                    RouteBActivityScenePath,
                    FailedFirstAdoptionPrimaryScenePath,
                    FailedFirstAdoptionActivityScenePath,
                    FailedReprojectionPrimaryScenePath,
                    FailedReprojectionActivityScenePath,
                    DuplicateSlotScenePath,
                    MissingActorScenePath,
                    MismatchedProfileScenePath,
                    UndeclaredSurfaceScenePath
                };
                for (int index = 0; index < buildScenes.Length; index++)
                {
                    EnsureSceneInBuildSettings(buildScenes[index]);
                }

                PlayerSlotProfile[] firstSlotProjection = { slots[0] };
                PlayerParticipationRequirementLevel positiveRequirementLevel =
                    PlayerParticipationRequirementLevel.GameplayReady;
                PlayerParticipationRequirementLevel negativeRequirementLevel =
                    PlayerParticipationRequirementLevel.LogicalActorsPrepared;

                ActivityAsset routeAActivity = CreateOrUpdateActivity(
                    RouteAActivityPath,
                    RouteAActivityId,
                    "Scene Player Route Lifecycle A Activity",
                    firstSlotProjection,
                    positiveRequirementLevel,
                    CreateOrUpdateContentProfile(
                        RouteAContentPath,
                        "qa.p3m5b.route-a.activity-content",
                        RouteAActivityScenePath));
                ActivityAsset routeBActivity = CreateOrUpdateActivity(
                    RouteBActivityPath,
                    RouteBActivityId,
                    "Scene Player Route Lifecycle B Activity",
                    firstSlotProjection,
                    positiveRequirementLevel,
                    CreateOrUpdateContentProfile(
                        RouteBContentPath,
                        "qa.p3m5b.route-b.activity-content",
                        RouteBActivityScenePath));
                ActivityAsset failedFirstAdoptionActivity = CreateOrUpdateActivity(
                    FailedFirstAdoptionActivityPath,
                    FailedFirstAdoptionActivityId,
                    "P3M5B Failed First Scene Adoption Activity",
                    firstSlotProjection,
                    PlayerParticipationRequirementLevel.LogicalActorsPrepared,
                    CreateOrUpdateContentProfile(
                        FailedFirstAdoptionContentPath,
                        "qa.p3m5b.failed-first-adoption.activity-content",
                        FailedFirstAdoptionActivityScenePath));
                ActivityAsset failedReprojectionActivity = CreateOrUpdateActivity(
                    FailedReprojectionActivityPath,
                    FailedReprojectionActivityId,
                    "P3M5B Failed Contextual Reprojection Activity",
                    firstSlotProjection,
                    PlayerParticipationRequirementLevel.LogicalActorsPrepared,
                    CreateOrUpdateContentProfile(
                        FailedReprojectionContentPath,
                        "qa.p3m5b.failed-reprojection.activity-content",
                        FailedReprojectionActivityScenePath));
                CreateOrUpdateActivity(
                    DuplicateSlotActivityPath,
                    DuplicateSlotActivityId,
                    "P3M5B Negative Duplicate Slot Activity",
                    firstSlotProjection,
                    negativeRequirementLevel,
                    CreateOrUpdateContentProfile(
                        DuplicateSlotContentPath,
                        "qa.p3m5b.negative.duplicate-slot",
                        DuplicateSlotScenePath));
                CreateOrUpdateActivity(
                    MissingActorActivityPath,
                    MissingActorActivityId,
                    "P3M5B Negative Missing Actor Activity",
                    firstSlotProjection,
                    negativeRequirementLevel,
                    CreateOrUpdateContentProfile(
                        MissingActorContentPath,
                        "qa.p3m5b.negative.missing-actor",
                        MissingActorScenePath));
                CreateOrUpdateActivity(
                    MismatchedProfileActivityPath,
                    MismatchedProfileActivityId,
                    "P3M5B Negative Mismatched Profile Activity",
                    firstSlotProjection,
                    negativeRequirementLevel,
                    CreateOrUpdateContentProfile(
                        MismatchedProfileContentPath,
                        "qa.p3m5b.negative.mismatched-profile",
                        MismatchedProfileScenePath));
                CreateOrUpdateActivity(
                    UndeclaredSurfaceActivityPath,
                    UndeclaredSurfaceActivityId,
                    "P3M5B Negative Undeclared Surface Activity",
                    firstSlotProjection,
                    negativeRequirementLevel,
                    null);

                RouteAsset routeA = CreateOrUpdateRoute(
                    RouteAPath,
                    "qa.p3m5b.route.a",
                    "Scene Player Route Lifecycle A",
                    RouteAPrimaryScenePath,
                    routeAActivity);
                RouteAsset routeB = CreateOrUpdateRoute(
                    RouteBPath,
                    "qa.p3m5b.route.b",
                    "Scene Player Route Lifecycle B",
                    RouteBPrimaryScenePath,
                    routeBActivity);
                CreateOrUpdateRoute(
                    FailedFirstAdoptionRoutePath,
                    "qa.p3m5b.route.failed-first-adoption",
                    "P3M5B Failed First Scene Adoption",
                    FailedFirstAdoptionPrimaryScenePath,
                    failedFirstAdoptionActivity);
                CreateOrUpdateRoute(
                    FailedReprojectionRoutePath,
                    "qa.p3m5b.route.failed-reprojection",
                    "P3M5B Failed Contextual Reprojection",
                    FailedReprojectionPrimaryScenePath,
                    failedReprojectionActivity);

                ConfigureHubSessionWitness(routeA);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3M5B_ROUTE_TRANSITION_NEGATIVE_MATRIX_FIXTURE] " +
                    "status='Applied' " +
                    $"routeA='{routeA.RouteName}' routeB='{routeB.RouteName}' " +
                    $"routeAActivityId='{routeAActivity.ActivityId.StableText}' " +
                    $"routeBActivityId='{routeBActivity.ActivityId.StableText}' " +
                    $"slot1='{slots[0].PlayerSlotId.StableText}' " +
                    $"slot2='{slots[1].PlayerSlotId.StableText}' " +
                    "hostProvisioning='SceneProvided' " +
                    $"supportedSlots='{slots.Length}' " +
                    "negativeCases='duplicate-slot,missing-actor,mismatched-profile,undeclared-surface'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3M5B_ROUTE_TRANSITION_NEGATIVE_MATRIX_FIXTURE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static PlayerSlotProfile[] PrepareSceneProvidedSession()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null || settings.ActiveGameApplication == null)
            {
                throw new InvalidOperationException(
                    "P3M5B requires the QA Active Game Application in Immersive Framework settings.");
            }

            GameApplicationAsset application = settings.ActiveGameApplication;
            PlayerSessionProfile profile = application.DefaultPlayerSessionProfile;
            if (!application.PlayerSessionEnabled || profile == null)
            {
                throw new InvalidOperationException(
                    "P3M5B requires an enabled active Player Session Profile.");
            }

            var serialized = new SerializedObject(profile);
            serialized.FindProperty("initialJoiningOpen").boolValue = false;
            serialized.FindProperty("hostProvisioning").intValue =
                (int)PlayerHostProvisioningMode.SceneProvided;
            serialized.FindProperty("actorResolutionPolicy").intValue =
                (int)PlayerActorResolutionPolicy.LeaveUnresolved;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);

            PlayerSlotProfile[] slots = ResolveConfiguredSlots();
            string profileIssue = string.Empty;
            if (profile.HostProvisioning != PlayerHostProvisioningMode.SceneProvided ||
                profile.ActorResolutionPolicy !=
                    PlayerActorResolutionPolicy.LeaveUnresolved ||
                profile.InitialJoiningOpen ||
                !profile.TryValidate(out profileIssue))
            {
                throw new InvalidOperationException(
                    "P3M5B Scene-Provided preparation requires a valid active " +
                    "PlayerSessionProfile with SceneProvided provisioning. " +
                    profileIssue);
            }

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(profile);
            if (!resolution.Succeeded || resolution.Configuration == null ||
                resolution.Configuration.HostProvisioning !=
                    PlayerHostProvisioningMode.SceneProvided ||
                resolution.Configuration.SupportedSlotCount != slots.Length)
            {
                throw new InvalidOperationException(
                    "P3M5B Scene-Provided preparation did not resolve the expected " +
                    "effective Player Session configuration. " +
                    (resolution != null ? resolution.Message : string.Empty));
            }

            return slots;
        }

        private static PlayerSlotProfile[] ResolveConfiguredSlots()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null || settings.ActiveGameApplication == null)
            {
                throw new InvalidOperationException(
                    "P3M5B requires the QA Active Game Application in Immersive Framework settings.");
            }

            var slots = new PlayerSlotProfile[2];
            for (int index = 0; index < slots.Length; index++)
            {
                if (!ImmersiveFrameworkQA.Player.QaPlayerSessionQaSupport.TryGetSupportedSlot(
                        settings.ActiveGameApplication,
                        index,
                        out slots[index]) ||
                    slots[index] == null)
                {
                    throw new InvalidOperationException(
                        "P3M5B requires two configured Local Player Slots.");
                }
            }

            return slots;
        }

        private static GameObject CreateOrUpdateActorPrefab()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(
                ActorPrefabPath);
            bool loadedContents = asset != null;
            GameObject root = loadedContents
                ? PrefabUtility.LoadPrefabContents(ActorPrefabPath)
                : new GameObject(Path.GetFileNameWithoutExtension(ActorPrefabPath));

            try
            {
                root.name = Path.GetFileNameWithoutExtension(ActorPrefabPath);
                PlayerInput[] inputs = root.GetComponentsInChildren<PlayerInput>(true);
                for (int index = inputs.Length - 1; index >= 0; index--)
                {
                    UnityEngine.Object.DestroyImmediate(inputs[index]);
                }

                PlayerActorDeclaration declaration =
                    root.GetComponent<PlayerActorDeclaration>();
                if (declaration == null)
                {
                    declaration = root.AddComponent<PlayerActorDeclaration>();
                }

                ActorDeclaration[] declarations =
                    root.GetComponentsInChildren<ActorDeclaration>(true);
                for (int index = declarations.Length - 1; index >= 0; index--)
                {
                    if (!ReferenceEquals(declarations[index], declaration))
                    {
                        UnityEngine.Object.DestroyImmediate(declarations[index]);
                    }
                }

                SetString(declaration, "actorId", AuthoredActorId);
                SetString(declaration, "displayName", "P3M5B Scene Player");
                SetString(declaration, "reason", "p3m5b.scene-player.authored");

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    ActorPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        "P3M5B could not save the Scene Logical Player Actor prefab.");
                }

                return saved;
            }
            finally
            {
                if (loadedContents)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static ActorProfile CreateOrUpdateActorProfile(
            string path,
            string profileId,
            string displayName,
            GameObject actorPrefab)
        {
            ActorProfile profile = AssetDatabase.LoadAssetAtPath<ActorProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ActorProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.name = Path.GetFileNameWithoutExtension(path);
            SetString(profile, "actorProfileId", profileId);
            SetString(profile, "displayName", displayName);
            SetString(
                profile,
                "description",
                "QA-only P3M5B Scene Local Player Actor Profile.");
            SetEnum(profile, "actorKind", "Player");
            SetEnum(profile, "actorRole", "Protagonist");
            SetObject(profile, "logicalActorHostPrefab", actorPrefab);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void CreateOrUpdateEmptyScene(
            string path,
            string rootName)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = default;
            bool opened = false;
            try
            {
                scene = OpenOrCreateScene(path);
                opened = true;
                ClearScene(scene);
                NewSceneObject(rootName, scene);
                CreateHubReturnSurface(scene);
                SaveScene(scene, path);
            }
            finally
            {
                CloseAndRestoreScene(scene, opened, previousActiveScene);
            }
        }

        private static void CreateOrUpdatePlayerScene(
            string path,
            PlayerSceneShape shape,
            PlayerSlotProfile[] slots,
            GameObject actorPrefab,
            ActorProfile actorProfile,
            ActorProfile alternateProfile,
            string label)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = default;
            bool opened = false;
            try
            {
                scene = OpenOrCreateScene(path);
                opened = true;
                ClearScene(scene);

                switch (shape)
                {
                    case PlayerSceneShape.ValidSingle:
                    {
                        CreateValidSurface(
                            scene,
                            label,
                            slots[0],
                            actorPrefab,
                            actorProfile,
                            actorProfile);
                        break;
                    }
                    case PlayerSceneShape.DuplicateSlot:
                    {
                        CreateValidSurface(
                            scene,
                            label + " A",
                            slots[0],
                            actorPrefab,
                            actorProfile,
                            actorProfile);
                        CreateValidSurface(
                            scene,
                            label + " B",
                            slots[0],
                            actorPrefab,
                            actorProfile,
                            actorProfile);
                        break;
                    }
                    case PlayerSceneShape.MissingActor:
                    {
                        LocalPlayerHostAuthoring host = CreateHost(
                            scene,
                            label + " Host");
                        CreateAdmission(
                            scene,
                            label + " Admission",
                            slots[0],
                            host,
                            actorProfile,
                            null,
                            actorProfile,
                            actorPrefab);
                        break;
                    }
                    case PlayerSceneShape.MismatchedProfile:
                    {
                        CreateValidSurface(
                            scene,
                            label,
                            slots[0],
                            actorPrefab,
                            actorProfile,
                            alternateProfile,
                            validate: false);
                        break;
                    }
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported P3M5B Player scene shape '{shape}'.");
                }

                if (string.Equals(
                        path,
                        RouteAPrimaryScenePath,
                        StringComparison.Ordinal))
                {
                    CreateHubReturnSurface(scene);
                }

                SaveScene(scene, path);
            }
            finally
            {
                CloseAndRestoreScene(scene, opened, previousActiveScene);
            }
        }

        private static Scene OpenOrCreateScene(string path)
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)
                : EditorSceneManager.NewScene(
                    NewSceneSetup.EmptyScene,
                    NewSceneMode.Additive);
        }

        private static void CreateHubReturnSurface(Scene scene)
        {
            RouteAsset hubRoute = AssetDatabase.LoadAssetAtPath<RouteAsset>(
                HubRoutePath);
            Require(hubRoute != null,
                $"Required QA Hub route is missing: '{HubRoutePath}'.");

            GameObject navigation = NewSceneObject(
                "Scene Player Route Lifecycle Regression Navigation",
                scene);
            RouteRequestTrigger trigger =
                navigation.AddComponent<RouteRequestTrigger>();
            SetObject(trigger, "targetRoute", hubRoute);
            SetString(
                trigger,
                "reason",
                "qa.scene-player-route-lifecycle.back-to-hub");

            QaHubReturnPanel panel = navigation.AddComponent<QaHubReturnPanel>();
            panel.Configure(
                trigger,
                "Scene Player Route Lifecycle Regression",
                $"Run from Unity Editor: {RegressionMenuPath}");
            EditorUtility.SetDirty(panel);
        }

        private static void ConfigureHubSessionWitness(RouteAsset routeA)
        {
            Require(routeA != null,
                "P3M5B Hub Session witness requires Route A.");
            Scene existing = SceneManager.GetSceneByPath(HubScenePath);
            bool opened = !existing.IsValid() || !existing.isLoaded;
            Scene hub = opened
                ? EditorSceneManager.OpenScene(HubScenePath, OpenSceneMode.Additive)
                : existing;
            Scene previousActive = SceneManager.GetActiveScene();

            try
            {
                GameObject root = null;
                foreach (GameObject candidate in hub.GetRootGameObjects())
                {
                    if (candidate != null && string.Equals(
                            candidate.name,
                            P3M5BSessionProvisioningWitness.RootObjectName,
                            StringComparison.Ordinal))
                    {
                        Require(root == null,
                            "P3M5B Hub contains duplicate Session provisioning witnesses.");
                        root = candidate;
                    }
                }

                root ??= NewSceneObject(
                    P3M5BSessionProvisioningWitness.RootObjectName,
                    hub);
                PlayerSessionScopedAccessConsumer binding =
                    root.GetComponent<PlayerSessionScopedAccessConsumer>() ??
                    root.AddComponent<PlayerSessionObserver>();
                var serializedBinding = new SerializedObject(binding);
                SerializedProperty scope = serializedBinding.FindProperty("scope");
                Require(scope != null,
                    "P3M5B Hub witness binding has no serialized scope.");
                int routeScopeIndex = Array.IndexOf(
                    scope.enumNames,
                    LocalPlayerProvisioningConsumerScope.Route.ToString());
                Require(routeScopeIndex >= 0,
                    "P3M5B Hub witness binding cannot resolve Route scope.");
                scope.enumValueIndex = routeScopeIndex;
                serializedBinding.ApplyModifiedPropertiesWithoutUndo();

                RouteRequestTrigger enterRouteA =
                    root.GetComponent<RouteRequestTrigger>() ??
                    root.AddComponent<RouteRequestTrigger>();
                enterRouteA.TargetRoute = routeA;
                SetString(enterRouteA, "reason", "qa.p3m5b.hub-enter-route-a");

                P3M5BSessionProvisioningWitness witness =
                    root.GetComponent<P3M5BSessionProvisioningWitness>() ??
                    root.AddComponent<P3M5BSessionProvisioningWitness>();
                witness.Configure(binding, enterRouteA);
                Require(witness.TryValidate(out string issue), issue);
                EditorUtility.SetDirty(binding);
                EditorUtility.SetDirty(enterRouteA);
                EditorUtility.SetDirty(witness);
                EditorSceneManager.MarkSceneDirty(hub);
                Require(EditorSceneManager.SaveScene(hub),
                    $"Could not save P3M5B Hub witness in '{HubScenePath}'.");
            }
            finally
            {
                if (opened && hub.IsValid() && hub.isLoaded)
                {
                    EditorSceneManager.CloseScene(hub, true);
                }

                if (previousActive.IsValid() && previousActive.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActive);
                }
            }
        }

        private static void ClearScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int index = roots.Length - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(roots[index]);
            }
        }

        private static void SaveScene(Scene scene, string path)
        {
            if (!EditorSceneManager.SaveScene(scene, path, false))
            {
                throw new InvalidOperationException(
                    $"Could not save P3M5B scene '{path}'.");
            }
        }

        private static void CloseAndRestoreScene(
            Scene scene,
            bool opened,
            Scene previousActiveScene)
        {
            if (opened && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousActiveScene);
            }
        }

        private static SceneLocalPlayerAdmissionAuthoring CreateValidSurface(
            Scene scene,
            string label,
            PlayerSlotProfile slot,
            GameObject actorPrefab,
            ActorProfile selectedProfile,
            ActorProfile evidenceProfile,
            bool validate = true)
        {
            LocalPlayerHostAuthoring host = CreateHost(scene, label + " Host");
            PlayerActorDeclaration actor = CreateActor(
                scene,
                label + " Actor",
                actorPrefab,
                host.ActorMount,
                selectedProfile,
                evidenceProfile);
            SceneLocalPlayerAdmissionAuthoring admission = CreateAdmission(
                scene,
                label + " Admission",
                slot,
                host,
                selectedProfile,
                actor,
                evidenceProfile,
                actorPrefab);
            CreateContextualAdmissionWitness(
                scene,
                label + " Contextual Admission Witness",
                admission);
            if (validate && !admission.TryValidateRuntimeEvidence(out string issue))
            {
                throw new InvalidOperationException(
                    $"P3M5B valid Scene Local Player surface '{label}' is invalid. {issue}");
            }

            return admission;
        }

        private static P3M5BContextualAdmissionWitness
            CreateContextualAdmissionWitness(
                Scene scene,
                string name,
                SceneLocalPlayerAdmissionAuthoring admission)
        {
            Require(admission != null,
                "P3M5B contextual witness requires an admission authoring surface.");
            GameObject root = NewSceneObject(name, scene);
            PlayerSessionScopedAccessConsumer binding =
                root.AddComponent<PlayerSessionObserver>();
            var serializedBinding = new SerializedObject(binding);
            SerializedProperty scope = serializedBinding.FindProperty("scope");
            Require(scope != null,
                "P3M5B contextual witness binding has no serialized scope.");
            int activityScopeIndex = Array.IndexOf(
                scope.enumNames,
                LocalPlayerProvisioningConsumerScope.Activity.ToString());
            Require(activityScopeIndex >= 0,
                "P3M5B contextual witness binding cannot resolve Activity scope.");
            scope.enumValueIndex = activityScopeIndex;
            serializedBinding.ApplyModifiedPropertiesWithoutUndo();
            P3M5BContextualAdmissionWitness witness =
                root.AddComponent<P3M5BContextualAdmissionWitness>();
            witness.EditorConfigure(admission, binding);
            EditorUtility.SetDirty(witness);
            return witness;
        }

        private static LocalPlayerHostAuthoring CreateHost(
            Scene scene,
            string name)
        {
            GameObject root = NewSceneObject(name, scene);
            PlayerInput playerInput = root.AddComponent<PlayerInput>();
            playerInput.enabled = false;
            InputActionAsset inputActions = RequireCanonicalGameplayInputActions();
            InputActionMap gameplayActionMap = inputActions.FindActionMap(
                GameplayActionMapName,
                false);
            Require(gameplayActionMap != null,
                $"P3M5B canonical input asset has no '{GameplayActionMapName}' action map.");
            SetObject(playerInput, "m_Actions", inputActions);
            SetString(playerInput, "m_DefaultActionMap", GameplayActionMapName);

            UnityPlayerInputGateAdapter inputGate =
                root.AddComponent<UnityPlayerInputGateAdapter>();
            ConfigureGameplayInputGate(inputGate, playerInput, inputActions, gameplayActionMap);

            LocalPlayerHostAuthoring host =
                root.AddComponent<LocalPlayerHostAuthoring>();
            GameObject mount = NewSceneObject(name + " Actor Mount", scene);
            mount.transform.SetParent(root.transform, false);
            SetObject(host, "playerInput", playerInput);
            SetObject(host, "actorMount", mount.transform);
            return host;
        }

        private static InputActionAsset RequireCanonicalGameplayInputActions()
        {
            InputActionAsset inputActions =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                    LocalPlayerInputActionsPath);
            Require(inputActions != null,
                $"P3M5B requires canonical input actions at '{LocalPlayerInputActionsPath}'.");
            Require(inputActions.FindActionMap(GameplayActionMapName, false) != null,
                $"P3M5B canonical input actions at '{LocalPlayerInputActionsPath}' " +
                $"have no '{GameplayActionMapName}' action map.");
            return inputActions;
        }

        private static void ConfigureGameplayInputGate(
            UnityPlayerInputGateAdapter inputGate,
            PlayerInput playerInput,
            InputActionAsset inputActions,
            InputActionMap gameplayActionMap)
        {
            var serialized = new SerializedObject(inputGate);
            SerializedProperty playerInputProperty = serialized.FindProperty("playerInput");
            SerializedProperty actionMapProperty = serialized.FindProperty("gameplayActionMap");
            SerializedProperty actionMapAssetProperty =
                actionMapProperty?.FindPropertyRelative("actionAsset");
            SerializedProperty actionMapIdProperty =
                actionMapProperty?.FindPropertyRelative("actionMapId");
            SerializedProperty cachedActionMapNameProperty =
                actionMapProperty?.FindPropertyRelative("cachedActionMapName");
            SerializedProperty actionMapNameProperty =
                serialized.FindProperty("gameplayActionMapName");

            Require(playerInputProperty != null,
                "P3M5B UnityPlayerInputGateAdapter has no playerInput property.");
            Require(actionMapAssetProperty != null && actionMapIdProperty != null &&
                    cachedActionMapNameProperty != null && actionMapNameProperty != null,
                "P3M5B UnityPlayerInputGateAdapter has no Gameplay action-map contract.");

            playerInputProperty.objectReferenceValue = playerInput;
            actionMapAssetProperty.objectReferenceValue = inputActions;
            actionMapIdProperty.stringValue = gameplayActionMap.id.ToString("D");
            cachedActionMapNameProperty.stringValue = GameplayActionMapName;
            actionMapNameProperty.stringValue = GameplayActionMapName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(inputGate);
        }

        private static PlayerActorDeclaration CreateActor(
            Scene scene,
            string name,
            GameObject actorPrefab,
            Transform parent,
            ActorProfile selectedProfile,
            ActorProfile evidenceProfile)
        {
            GameObject root = PrefabUtility.InstantiatePrefab(
                actorPrefab,
                scene) as GameObject;
            if (root == null)
            {
                throw new InvalidOperationException(
                    $"P3M5B could not instantiate Actor prefab for '{name}'.");
            }

            root.name = name;
            root.transform.SetParent(parent, false);
            PlayerActorDeclaration actor =
                root.GetComponent<PlayerActorDeclaration>();
            if (actor == null)
            {
                throw new InvalidOperationException(
                    $"P3M5B Actor '{name}' has no PlayerActorDeclaration.");
            }

            SceneLogicalPlayerActorEvidence evidence =
                root.GetComponent<SceneLogicalPlayerActorEvidence>();
            if (evidence == null)
            {
                evidence = root.AddComponent<SceneLogicalPlayerActorEvidence>();
            }

            evidence.EditorSetEvidence(
                evidenceProfile,
                actorPrefab,
                $"P3M5B evidence for '{name}' selected='{selectedProfile?.name}'.");
            return actor;
        }

        private static SceneLocalPlayerAdmissionAuthoring CreateAdmission(
            Scene scene,
            string name,
            PlayerSlotProfile slot,
            LocalPlayerHostAuthoring host,
            ActorProfile actorProfile,
            PlayerActorDeclaration actor,
            ActorProfile evidenceProfile,
            GameObject evidenceLogicalActorHostPrefab)
        {
            SceneLocalPlayerAdmissionAuthoring admission =
                host.gameObject.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
            host.gameObject.name = name;
            SetObject(admission, "playerSlotProfile", slot);
            SetObject(admission, "actorProfile", actorProfile);
            SetObject(admission, "sceneLogicalPlayerActor", actor);
            SetEnum(admission, "admissionTiming", "OnActivityEnter");
            admission.EditorSetProfileEvidence(
                evidenceProfile,
                evidenceLogicalActorHostPrefab,
                $"P3M5B fixture evidence for '{name}'.");
            return admission;
        }

        private static ActivityContentProfileAsset CreateOrUpdateContentProfile(
            string path,
            string profileId,
            string scenePath)
        {
            ActivityContentProfileAsset profile =
                AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    ActivityContentProfileAsset>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.name = Path.GetFileNameWithoutExtension(path);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue = profileId;
            serialized.FindProperty("description").stringValue =
                "P3M5B QA-only Activity scene content.";
            SerializedProperty scenes = serialized.FindProperty("scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("contentId").stringValue = profileId + ".scene";
            entry.FindPropertyRelative("scenePath").stringValue = scenePath;
            entry.FindPropertyRelative("sceneName").stringValue =
                Path.GetFileNameWithoutExtension(scenePath);
            SetEnum(entry.FindPropertyRelative("requiredness"), "Required");
            SetEnum(entry.FindPropertyRelative("loadMode"), "Additive");
            SetEnum(
                entry.FindPropertyRelative("releasePolicy"),
                "ReleaseOnActivityChange");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateOrUpdateActivity(
            string path,
            string activityId,
            string activityName,
            PlayerSlotProfile[] projection,
            PlayerParticipationRequirementLevel requirementLevel,
            ActivityContentProfileAsset content)
        {
            ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(path);
            if (activity == null)
            {
                activity = ScriptableObject.CreateInstance<ActivityAsset>();
                AssetDatabase.CreateAsset(activity, path);
            }

            activity.name = Path.GetFileNameWithoutExtension(path);
            var serialized = new SerializedObject(activity);
            SerializedProperty activityIdProperty =
                serialized.FindProperty("activityId");
            if (activityIdProperty == null)
            {
                throw new InvalidOperationException(
                    $"ActivityAsset '{activity.name}' does not expose the required serialized activityId field.");
            }

            activityIdProperty.stringValue = activityId;
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("description").stringValue =
                "P3M5B QA-only Route transition and negative-matrix Activity.";
            serialized.FindProperty("playerParticipationProjectionMode").intValue =
                (int)ActivityParticipationProjectionMode.ExplicitSlots;
            serialized.FindProperty("playerParticipationZeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Rejected;
            SerializedProperty explicitSlots =
                serialized.FindProperty("playerParticipationExplicitSlotProfiles");
            explicitSlots.arraySize = projection.Length;
            for (int index = 0; index < projection.Length; index++)
            {
                explicitSlots.GetArrayElementAtIndex(index).objectReferenceValue =
                    projection[index];
            }
            serialized.FindProperty("playerParticipationRequirementLevel")
                .intValue = (int)requirementLevel;
            serialized.FindProperty("activityContentProfile")
                .objectReferenceValue = content;
            serialized.FindProperty("visualTransitionMode").intValue =
                (int)ActivityVisualTransitionMode.Seamless;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);
            return activity;
        }

        private static RouteAsset CreateOrUpdateRoute(
            string path,
            string routeId,
            string routeName,
            string primaryScenePath,
            ActivityAsset startupActivity)
        {
            RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(path);
            if (route == null)
            {
                route = ScriptableObject.CreateInstance<RouteAsset>();
                AssetDatabase.CreateAsset(route, path);
            }

            route.name = Path.GetFileNameWithoutExtension(path);
            var serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = routeId;
            serialized.FindProperty("routeName").stringValue = routeName;
            serialized.FindProperty("primaryScenePath").stringValue =
                primaryScenePath;
            serialized.FindProperty("primarySceneName").stringValue =
                Path.GetFileNameWithoutExtension(primaryScenePath);
            serialized.FindProperty("routeContentProfile")
                .objectReferenceValue = null;
            serialized.FindProperty("startupActivity")
                .objectReferenceValue = startupActivity;
            serialized.FindProperty("description").stringValue =
                "P3M5B QA-only Route with a real Scene Local Player Startup Activity.";
            SetEnum(
                serialized.FindProperty("transitionGateMode"),
                "InputInteractionAndGameplay");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(route);
            return route;
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            string normalized = scenePath.Replace('\\', '/');
            var scenes = new List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);
            bool found = false;
            for (int index = 0; index < scenes.Count; index++)
            {
                if (!string.Equals(
                        scenes[index].path.Replace('\\', '/'),
                        normalized,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                scenes[index] = new EditorBuildSettingsScene(normalized, true);
                found = true;
                break;
            }

            if (!found)
            {
                scenes.Add(new EditorBuildSettingsScene(normalized, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static GameObject NewSceneObject(string name, Scene scene)
        {
            var value = new GameObject(name);
            SceneManager.MoveGameObjectToScene(value, scene);
            return value;
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

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Missing object property '{propertyName}' on '{target.GetType().Name}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Missing string property '{propertyName}' on '{target.GetType().Name}'.");
            property.stringValue = value ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(
            UnityEngine.Object target,
            string propertyName,
            string enumName)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Missing enum property '{propertyName}' on '{target.GetType().Name}'.");
            SetEnum(property, enumName);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(
            SerializedProperty property,
            string enumName)
        {
            Require(property != null,
                $"Missing serialized enum property for '{enumName}'.");
            string[] names = property.enumNames;
            for (int index = 0; index < names.Length; index++)
            {
                if (string.Equals(names[index], enumName, StringComparison.Ordinal))
                {
                    property.enumValueIndex = index;
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Enum value '{enumName}' is unavailable for '{property.propertyPath}'.");
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
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

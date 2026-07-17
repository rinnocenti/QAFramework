using System;
using System.Collections.Generic;
using System.IO;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
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
            MismatchedProfile = 30,
            ReusedHost = 40
        }

        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M5B Apply Route Transition and Negative Matrix Fixture";

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
        internal const string DuplicateSlotScenePath =
            RootFolder + "/P3M5B_Negative_DuplicateSlot.unity";
        internal const string MissingActorScenePath =
            RootFolder + "/P3M5B_Negative_MissingActor.unity";
        internal const string MismatchedProfileScenePath =
            RootFolder + "/P3M5B_Negative_MismatchedProfile.unity";
        internal const string ReusedHostScenePath =
            RootFolder + "/P3M5B_Negative_ReusedHost.unity";

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
        internal const string DuplicateSlotContentPath =
            RootFolder + "/P3M5B_Negative_DuplicateSlot_Content.asset";
        internal const string MissingActorContentPath =
            RootFolder + "/P3M5B_Negative_MissingActor_Content.asset";
        internal const string MismatchedProfileContentPath =
            RootFolder + "/P3M5B_Negative_MismatchedProfile_Content.asset";
        internal const string ReusedHostContentPath =
            RootFolder + "/P3M5B_Negative_ReusedHost_Content.asset";

        internal const string FirstSlotProjectionPath =
            RootFolder + "/P3M5B_FirstSlotProjection.asset";
        internal const string TwoSlotProjectionPath =
            RootFolder + "/P3M5B_TwoSlotProjection.asset";
        internal const string LogicalActorsPreparedRequirementsPath =
            RootFolder + "/P3M5B_LogicalActorsPrepared.asset";

        internal const string RouteAActivityPath =
            RootFolder + "/P3M5B_RouteA_StartupActivity.asset";
        internal const string RouteBActivityPath =
            RootFolder + "/P3M5B_RouteB_StartupActivity.asset";
        internal const string DuplicateSlotActivityPath =
            RootFolder + "/P3M5B_Negative_DuplicateSlot_Activity.asset";
        internal const string MissingActorActivityPath =
            RootFolder + "/P3M5B_Negative_MissingActor_Activity.asset";
        internal const string MismatchedProfileActivityPath =
            RootFolder + "/P3M5B_Negative_MismatchedProfile_Activity.asset";
        internal const string ReusedHostActivityPath =
            RootFolder + "/P3M5B_Negative_ReusedHost_Activity.asset";
        internal const string UndeclaredSurfaceActivityPath =
            RootFolder + "/P3M5B_Negative_UndeclaredSurface_Activity.asset";

        internal const string RouteAPath =
            RootFolder + "/P3M5B_RouteA.asset";
        internal const string RouteBPath =
            RootFolder + "/P3M5B_RouteB.asset";

        internal const string AuthoredActorId =
            "qa.p3m5b.scene-player.authored";
        internal const string ActorProfileId =
            "qa.p3m5b.scene-player.profile";
        internal const string AlternateActorProfileId =
            "qa.p3m5b.scene-player.profile.alternate";

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
                PlayerSlotProfile[] slots = ResolveConfiguredSlots();
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

                CreateOrUpdatePlayerScene(
                    RouteAPrimaryScenePath,
                    PlayerSceneShape.ValidSingle,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Route A Undeclared");
                CreateOrUpdateEmptyScene(
                    RouteBPrimaryScenePath,
                    "P3M5B Route B Primary");
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
                    ReusedHostScenePath,
                    PlayerSceneShape.ReusedHost,
                    slots,
                    actorPrefab,
                    actorProfile,
                    alternateProfile,
                    "P3M5B Reused Host");

                string[] buildScenes =
                {
                    RouteAPrimaryScenePath,
                    RouteBPrimaryScenePath,
                    RouteAActivityScenePath,
                    RouteBActivityScenePath,
                    DuplicateSlotScenePath,
                    MissingActorScenePath,
                    MismatchedProfileScenePath,
                    ReusedHostScenePath
                };
                for (int index = 0; index < buildScenes.Length; index++)
                {
                    EnsureSceneInBuildSettings(buildScenes[index]);
                }

                ActivityParticipationProjectionProfile firstSlotProjection =
                    CreateOrUpdateProjection(
                        FirstSlotProjectionPath,
                        "P3M5B — First Slot",
                        new[] { slots[0] });
                ActivityParticipationProjectionProfile twoSlotProjection =
                    CreateOrUpdateProjection(
                        TwoSlotProjectionPath,
                        "P3M5B — Two Slots",
                        slots);
                PlayerParticipationRequirementsProfile requirements =
                    CreateOrUpdateRequirements();

                ActivityAsset routeAActivity = CreateOrUpdateActivity(
                    RouteAActivityPath,
                    "P3M5B Route A Startup Activity",
                    firstSlotProjection,
                    requirements,
                    CreateOrUpdateContentProfile(
                        RouteAContentPath,
                        "qa.p3m5b.route-a.activity-content",
                        RouteAActivityScenePath));
                ActivityAsset routeBActivity = CreateOrUpdateActivity(
                    RouteBActivityPath,
                    "P3M5B Route B Startup Activity",
                    firstSlotProjection,
                    requirements,
                    CreateOrUpdateContentProfile(
                        RouteBContentPath,
                        "qa.p3m5b.route-b.activity-content",
                        RouteBActivityScenePath));
                CreateOrUpdateActivity(
                    DuplicateSlotActivityPath,
                    "P3M5B Negative Duplicate Slot Activity",
                    firstSlotProjection,
                    requirements,
                    CreateOrUpdateContentProfile(
                        DuplicateSlotContentPath,
                        "qa.p3m5b.negative.duplicate-slot",
                        DuplicateSlotScenePath));
                CreateOrUpdateActivity(
                    MissingActorActivityPath,
                    "P3M5B Negative Missing Actor Activity",
                    firstSlotProjection,
                    requirements,
                    CreateOrUpdateContentProfile(
                        MissingActorContentPath,
                        "qa.p3m5b.negative.missing-actor",
                        MissingActorScenePath));
                CreateOrUpdateActivity(
                    MismatchedProfileActivityPath,
                    "P3M5B Negative Mismatched Profile Activity",
                    firstSlotProjection,
                    requirements,
                    CreateOrUpdateContentProfile(
                        MismatchedProfileContentPath,
                        "qa.p3m5b.negative.mismatched-profile",
                        MismatchedProfileScenePath));
                CreateOrUpdateActivity(
                    ReusedHostActivityPath,
                    "P3M5B Negative Reused Host Activity",
                    twoSlotProjection,
                    requirements,
                    CreateOrUpdateContentProfile(
                        ReusedHostContentPath,
                        "qa.p3m5b.negative.reused-host",
                        ReusedHostScenePath));
                CreateOrUpdateActivity(
                    UndeclaredSurfaceActivityPath,
                    "P3M5B Negative Undeclared Surface Activity",
                    firstSlotProjection,
                    requirements,
                    null);

                RouteAsset routeA = CreateOrUpdateRoute(
                    RouteAPath,
                    "P3M5B Route A",
                    RouteAPrimaryScenePath,
                    routeAActivity);
                RouteAsset routeB = CreateOrUpdateRoute(
                    RouteBPath,
                    "P3M5B Route B",
                    RouteBPrimaryScenePath,
                    routeBActivity);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3M5B_ROUTE_TRANSITION_NEGATIVE_MATRIX_FIXTURE] " +
                    "status='Applied' " +
                    $"routeA='{routeA.RouteName}' routeB='{routeB.RouteName}' " +
                    $"slot1='{slots[0].PlayerSlotId.StableText}' " +
                    $"slot2='{slots[1].PlayerSlotId.StableText}' " +
                    "negativeCases='duplicate-slot,missing-actor,mismatched-profile,reused-host,undeclared-surface'.");
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
                if (!settings.ActiveGameApplication.TryGetLocalPlayerSlot(
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
                            null);
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
                    case PlayerSceneShape.ReusedHost:
                    {
                        LocalPlayerHostAuthoring host = CreateHost(
                            scene,
                            label + " Shared Host");
                        PlayerActorDeclaration actor = CreateActor(
                            scene,
                            label + " Shared Actor",
                            actorPrefab,
                            host.ActorMount,
                            actorProfile,
                            actorProfile);
                        CreateAdmission(
                            scene,
                            label + " Slot 1 Admission",
                            slots[0],
                            host,
                            actorProfile,
                            actor);
                        CreateAdmission(
                            scene,
                            label + " Slot 2 Admission",
                            slots[1],
                            host,
                            actorProfile,
                            actor);
                        break;
                    }
                    default:
                        throw new InvalidOperationException(
                            $"Unsupported P3M5B Player scene shape '{shape}'.");
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
                actor);
            if (validate && !admission.TryValidateRuntimeEvidence(out string issue))
            {
                throw new InvalidOperationException(
                    $"P3M5B valid Scene Local Player surface '{label}' is invalid. {issue}");
            }

            return admission;
        }

        private static LocalPlayerHostAuthoring CreateHost(
            Scene scene,
            string name)
        {
            GameObject root = NewSceneObject(name, scene);
            PlayerInput playerInput = root.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host =
                root.AddComponent<LocalPlayerHostAuthoring>();
            GameObject mount = NewSceneObject(name + " Actor Mount", scene);
            mount.transform.SetParent(root.transform, false);
            SetObject(host, "playerInput", playerInput);
            SetObject(host, "actorMount", mount.transform);
            return host;
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
            PlayerActorDeclaration actor)
        {
            GameObject root = NewSceneObject(name, scene);
            SceneLocalPlayerAdmissionAuthoring admission =
                root.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
            SetObject(admission, "playerSlotProfile", slot);
            SetObject(admission, "localPlayerHost", host);
            SetObject(admission, "actorProfile", actorProfile);
            SetObject(admission, "sceneLogicalPlayerActor", actor);
            SetEnum(admission, "admissionTiming", "OnActivityEnter");
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

        private static ActivityParticipationProjectionProfile
            CreateOrUpdateProjection(
                string path,
                string displayName,
                PlayerSlotProfile[] slots)
        {
            ActivityParticipationProjectionProfile profile =
                AssetDatabase.LoadAssetAtPath<
                    ActivityParticipationProjectionProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    ActivityParticipationProjectionProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.name = Path.GetFileNameWithoutExtension(path);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue =
                "P3M5B explicit configured Slot projection.";
            SetEnum(serialized.FindProperty("projectionMode"), "ExplicitSlots");
            SetEnum(
                serialized.FindProperty("zeroParticipantPolicy"),
                "Rejected");
            SerializedProperty explicitSlots =
                serialized.FindProperty("explicitSlotProfiles");
            explicitSlots.arraySize = slots.Length;
            for (int index = 0; index < slots.Length; index++)
            {
                explicitSlots.GetArrayElementAtIndex(index)
                    .objectReferenceValue = slots[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static PlayerParticipationRequirementsProfile
            CreateOrUpdateRequirements()
        {
            PlayerParticipationRequirementsProfile profile =
                AssetDatabase.LoadAssetAtPath<
                    PlayerParticipationRequirementsProfile>(
                    LogicalActorsPreparedRequirementsPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    PlayerParticipationRequirementsProfile>();
                AssetDatabase.CreateAsset(
                    profile,
                    LogicalActorsPreparedRequirementsPath);
            }

            profile.name = Path.GetFileNameWithoutExtension(
                LogicalActorsPreparedRequirementsPath);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue =
                "P3M5B — Logical Actors Prepared";
            serialized.FindProperty("description").stringValue =
                "Scene Local Player admission, selection and adoption are required.";
            serialized.FindProperty("requirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.LogicalActorsPrepared;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateOrUpdateActivity(
            string path,
            string activityName,
            ActivityParticipationProjectionProfile projection,
            PlayerParticipationRequirementsProfile requirements,
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
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("description").stringValue =
                "P3M5B QA-only Route transition and negative-matrix Activity.";
            serialized.FindProperty("playerParticipationProjectionProfile")
                .objectReferenceValue = projection;
            serialized.FindProperty("playerParticipationRequirementsProfile")
                .objectReferenceValue = requirements;
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

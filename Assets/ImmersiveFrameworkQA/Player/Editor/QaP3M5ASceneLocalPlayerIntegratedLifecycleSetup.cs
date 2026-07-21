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
    /// Idempotent P3M5A fixture for a real Activity-owned Scene Local Player.
    /// It creates one Build Profile scene, a Scene Local Player Admission surface,
    /// and three Activities used by the Play Mode lifecycle smoke.
    /// </summary>
    public static class QaP3M5ASceneLocalPlayerIntegratedLifecycleSetup
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M5A Apply Scene Local Player Integrated Lifecycle Fixture";

        internal const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/P3M5A";
        internal const string ScenePath =
            RootFolder + "/P3M5A_SceneLocalPlayerActivity.unity";
        internal const string ActorPrefabPath =
            RootFolder + "/P3M5A_SceneLogicalPlayerActor.prefab";
        internal const string ActorProfilePath =
            RootFolder + "/P3M5A_SceneActorProfile.asset";
        internal const string ContentProfilePath =
            RootFolder + "/P3M5A_ActivityContent.asset";
        internal const string ExplicitProjectionPath =
            RootFolder + "/P3M5A_ExplicitSlotProjection.asset";
        internal const string PreparedRequirementsPath =
            RootFolder + "/P3M5A_LogicalActorsPrepared.asset";
        internal const string GameplayRequirementsPath =
            RootFolder + "/P3M5A_GameplayReady.asset";
        internal const string NoPlayersProjectionPath =
            RootFolder + "/P3M5A_NoPlayersProjection.asset";
        internal const string NoPlayersRequirementsPath =
            RootFolder + "/P3M5A_NoPlayersRequirements.asset";
        internal const string PreparedActivityPath =
            RootFolder + "/P3M5A_ScenePlayerPreparedActivity.asset";
        internal const string GameplayActivityPath =
            RootFolder + "/P3M5A_ScenePlayerGameplayReadyActivity.asset";
        internal const string NoPlayersActivityPath =
            RootFolder + "/P3M5A_NoPlayersActivity.asset";

        internal const string AuthoredActorId = "qa.p3m5a.scene-player.authored";
        internal const string ActorProfileId = "qa.p3m5a.scene-player.profile";
        internal const string SceneContentId = "qa.p3m5a.scene-player.activity-content";

        private static bool ValidateApply()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_FIXTURE] " +
                    "status='RejectedPlayMode' message='Exit Play Mode before applying the fixture.'.");
                return;
            }

            try
            {
                EnsureFolder(RootFolder);
                PlayerSlotProfile firstSlot = ResolveFirstConfiguredSlot();
                GameObject actorPrefab = CreateOrUpdateActorPrefab();
                ActorProfile actorProfile = CreateOrUpdateActorProfile(actorPrefab);
                CreateOrUpdateScene(firstSlot, actorProfile, actorPrefab);
                EnsureSceneInBuildSettings(ScenePath);

                ActivityContentProfileAsset content =
                    CreateOrUpdateContentProfile();
                ActivityParticipationProjectionProfile explicitProjection =
                    CreateOrUpdateExplicitProjection(firstSlot);
                PlayerParticipationRequirementsProfile preparedRequirements =
                    CreateOrUpdateRequirements(
                        PreparedRequirementsPath,
                        "Logical Actors Prepared",
                        PlayerParticipationRequirementLevel.LogicalActorsPrepared,
                        "The Scene Local Player must be admitted, selected and adopted before Activity readiness.");
                PlayerParticipationRequirementsProfile gameplayRequirements =
                    CreateOrUpdateRequirements(
                        GameplayRequirementsPath,
                        "Gameplay Ready",
                        PlayerParticipationRequirementLevel.GameplayReady,
                        "Negative integration target: adoption must succeed before the canonical gameplay chain reports missing gameplay authoring.");
                ActivityParticipationProjectionProfile noPlayersProjection =
                    CreateOrUpdateNoPlayersProjection();
                PlayerParticipationRequirementsProfile noPlayersRequirements =
                    CreateOrUpdateRequirements(
                        NoPlayersRequirementsPath,
                        "No Players",
                        PlayerParticipationRequirementLevel.None,
                        "Transition target with no projected Players and no Activity content.");

                ActivityAsset preparedActivity = CreateOrUpdateActivity(
                    PreparedActivityPath,
                    "P3M5A Scene Player Prepared Activity",
                    explicitProjection,
                    preparedRequirements,
                    content,
                    ActivityVisualTransitionMode.Seamless);
                ActivityAsset gameplayActivity = CreateOrUpdateActivity(
                    GameplayActivityPath,
                    "P3M5A Scene Player Gameplay Ready Activity",
                    explicitProjection,
                    gameplayRequirements,
                    content,
                    ActivityVisualTransitionMode.FadeWithLoading);
                ActivityAsset noPlayersActivity = CreateOrUpdateActivity(
                    NoPlayersActivityPath,
                    "P3M5A No Players Activity",
                    noPlayersProjection,
                    noPlayersRequirements,
                    null,
                    ActivityVisualTransitionMode.Seamless);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_FIXTURE] " +
                    "status='Applied' " +
                    $"scene='{ScenePath}' slot='{firstSlot.PlayerSlotId.StableText}' " +
                    $"preparedActivity='{preparedActivity.ActivityName}' " +
                    $"gameplayActivity='{gameplayActivity.ActivityName}' " +
                    "gameplayTransition='FadeWithLoading' " +
                    $"noPlayersActivity='{noPlayersActivity.ActivityName}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3M5A_SCENE_LOCAL_PLAYER_INTEGRATED_FIXTURE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static PlayerSlotProfile ResolveFirstConfiguredSlot()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset.ResourcesPath);
            if (settings == null || settings.ActiveGameApplication == null)
            {
                throw new InvalidOperationException(
                    "P3M5A requires the QA Active Game Application in Immersive Framework settings.");
            }

            if (!settings.ActiveGameApplication.TryGetLocalPlayerSlot(
                    0,
                    out PlayerSlotProfile firstSlot) ||
                firstSlot == null)
            {
                throw new InvalidOperationException(
                    "P3M5A requires a configured first Local Player Slot.");
            }

            return firstSlot;
        }

        private static GameObject CreateOrUpdateActorPrefab()
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(ActorPrefabPath);
            bool loadedContents = asset != null;
            GameObject root = loadedContents
                ? PrefabUtility.LoadPrefabContents(ActorPrefabPath)
                : new GameObject(Path.GetFileNameWithoutExtension(ActorPrefabPath));

            try
            {
                root.name = Path.GetFileNameWithoutExtension(ActorPrefabPath);

                PlayerInput[] unexpectedInputs =
                    root.GetComponentsInChildren<PlayerInput>(true);
                for (int index = unexpectedInputs.Length - 1; index >= 0; index--)
                {
                    UnityEngine.Object.DestroyImmediate(unexpectedInputs[index]);
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
                SetString(declaration, "displayName", "P3M5A Scene Player");
                SetString(declaration, "reason", "p3m5a.scene-player.authored");

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    ActorPrefabPath);
                if (saved == null)
                {
                    throw new InvalidOperationException(
                        $"Could not save P3M5A Actor prefab '{ActorPrefabPath}'.");
                }
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(ActorPrefabPath);
        }

        private static ActorProfile CreateOrUpdateActorProfile(
            GameObject actorPrefab)
        {
            ActorProfile profile =
                AssetDatabase.LoadAssetAtPath<ActorProfile>(ActorProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ActorProfile>();
                AssetDatabase.CreateAsset(profile, ActorProfilePath);
            }

            profile.name = Path.GetFileNameWithoutExtension(ActorProfilePath);
            SetString(profile, "actorProfileId", ActorProfileId);
            SetString(profile, "displayName", "P3M5A Scene Player");
            SetString(
                profile,
                "description",
                "QA-only Actor Profile for real Scene Local Player Activity integration.");
            SetEnum(profile, "actorKind", "Player");
            SetEnum(profile, "actorRole", "Protagonist");
            SetObject(profile, "logicalActorHostPrefab", actorPrefab);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void CreateOrUpdateScene(
            PlayerSlotProfile firstSlot,
            ActorProfile actorProfile,
            GameObject actorPrefab)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene scene = default;
            bool opened = false;

            try
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                {
                    scene = EditorSceneManager.OpenScene(
                        ScenePath,
                        OpenSceneMode.Additive);
                }
                else
                {
                    scene = EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Additive);
                }

                opened = true;
                GameObject[] previousRoots = scene.GetRootGameObjects();
                for (int index = previousRoots.Length - 1; index >= 0; index--)
                {
                    UnityEngine.Object.DestroyImmediate(previousRoots[index]);
                }

                GameObject hostRoot = NewSceneObject(
                    "P3M5A Local Player Host",
                    scene);
                PlayerInput playerInput = hostRoot.AddComponent<PlayerInput>();
                LocalPlayerHostAuthoring host =
                    hostRoot.AddComponent<LocalPlayerHostAuthoring>();
                GameObject actorMountObject = NewSceneObject(
                    "Actor Mount",
                    scene);
                actorMountObject.transform.SetParent(hostRoot.transform, false);
                SetObject(host, "playerInput", playerInput);
                SetObject(host, "actorMount", actorMountObject.transform);

                GameObject actorRoot = PrefabUtility.InstantiatePrefab(
                    actorPrefab,
                    scene) as GameObject;
                if (actorRoot == null)
                {
                    throw new InvalidOperationException(
                        "P3M5A could not instantiate the Scene Logical Player Actor prefab.");
                }

                actorRoot.name = "P3M5A Scene Logical Player Actor";
                actorRoot.transform.SetParent(actorMountObject.transform, false);
                PlayerActorDeclaration actor =
                    actorRoot.GetComponent<PlayerActorDeclaration>();
                if (actor == null)
                {
                    throw new InvalidOperationException(
                        "P3M5A Actor prefab has no PlayerActorDeclaration.");
                }

                SceneLogicalPlayerActorEvidence evidence =
                    actorRoot.GetComponent<SceneLogicalPlayerActorEvidence>();
                if (evidence == null)
                {
                    evidence = actorRoot.AddComponent<
                        SceneLogicalPlayerActorEvidence>();
                }

                evidence.EditorSetEvidence(
                    actorProfile,
                    actorPrefab,
                    "P3M5A real Activity scene Actor evidence.");

                GameObject admissionRoot = NewSceneObject(
                    "P3M5A Scene Local Player Admission",
                    scene);
                SceneLocalPlayerAdmissionAuthoring admission =
                    admissionRoot.AddComponent<
                        SceneLocalPlayerAdmissionAuthoring>();
                SetObject(admission, "playerSlotProfile", firstSlot);
                SetObject(admission, "localPlayerHost", host);
                SetObject(admission, "actorProfile", actorProfile);
                SetObject(admission, "sceneLogicalPlayerActor", actor);
                SetEnum(admission, "admissionTiming", "OnActivityEnter");

                if (!admission.TryValidateRuntimeEvidence(out string issue))
                {
                    throw new InvalidOperationException(
                        "P3M5A Scene Local Player authoring is invalid. " + issue);
                }

                if (!EditorSceneManager.SaveScene(scene, ScenePath, false))
                {
                    throw new InvalidOperationException(
                        $"Could not save P3M5A Activity scene '{ScenePath}'.");
                }
            }
            finally
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
        }

        private static ActivityContentProfileAsset CreateOrUpdateContentProfile()
        {
            ActivityContentProfileAsset profile =
                AssetDatabase.LoadAssetAtPath<ActivityContentProfileAsset>(
                    ContentProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    ActivityContentProfileAsset>();
                AssetDatabase.CreateAsset(profile, ContentProfilePath);
            }

            profile.name = Path.GetFileNameWithoutExtension(ContentProfilePath);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("profileId").stringValue =
                "qa.p3m5a.scene-player.content-profile";
            serialized.FindProperty("description").stringValue =
                "Loads the real P3M5A Scene Local Player fixture additively.";
            SerializedProperty scenes = serialized.FindProperty("scenes");
            scenes.arraySize = 1;
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("contentId").stringValue = SceneContentId;
            entry.FindPropertyRelative("scenePath").stringValue = ScenePath;
            entry.FindPropertyRelative("sceneName").stringValue =
                Path.GetFileNameWithoutExtension(ScenePath);
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
            CreateOrUpdateExplicitProjection(PlayerSlotProfile slot)
        {
            ActivityParticipationProjectionProfile profile =
                AssetDatabase.LoadAssetAtPath<
                    ActivityParticipationProjectionProfile>(
                    ExplicitProjectionPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    ActivityParticipationProjectionProfile>();
                AssetDatabase.CreateAsset(profile, ExplicitProjectionPath);
            }

            profile.name = Path.GetFileNameWithoutExtension(
                ExplicitProjectionPath);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue =
                "P3M5A — Explicit Scene Player Slot";
            serialized.FindProperty("description").stringValue =
                "Projects the exact configured first Slot admitted by the Activity scene.";
            SetEnum(serialized.FindProperty("projectionMode"), "ExplicitSlots");
            SetEnum(
                serialized.FindProperty("zeroParticipantPolicy"),
                "Rejected");
            SerializedProperty slots =
                serialized.FindProperty("explicitSlotProfiles");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = slot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityParticipationProjectionProfile
            CreateOrUpdateNoPlayersProjection()
        {
            ActivityParticipationProjectionProfile profile =
                AssetDatabase.LoadAssetAtPath<
                    ActivityParticipationProjectionProfile>(
                    NoPlayersProjectionPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    ActivityParticipationProjectionProfile>();
                AssetDatabase.CreateAsset(profile, NoPlayersProjectionPath);
            }

            profile.name = Path.GetFileNameWithoutExtension(
                NoPlayersProjectionPath);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue =
                "P3M5A — No Players";
            serialized.FindProperty("description").stringValue =
                "Projects no Player Slots and allows zero participants.";
            SetEnum(serialized.FindProperty("projectionMode"), "NoSlots");
            SetEnum(
                serialized.FindProperty("zeroParticipantPolicy"),
                "Allowed");
            serialized.FindProperty("explicitSlotProfiles").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static PlayerParticipationRequirementsProfile
            CreateOrUpdateRequirements(
                string path,
                string displayName,
                PlayerParticipationRequirementLevel level,
                string description)
        {
            PlayerParticipationRequirementsProfile profile =
                AssetDatabase.LoadAssetAtPath<
                    PlayerParticipationRequirementsProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    PlayerParticipationRequirementsProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.name = Path.GetFileNameWithoutExtension(path);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue =
                "P3M5A — " + displayName;
            serialized.FindProperty("description").stringValue = description;
            serialized.FindProperty("requirementLevel").intValue = (int)level;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateOrUpdateActivity(
            string path,
            string activityName,
            ActivityParticipationProjectionProfile projection,
            PlayerParticipationRequirementsProfile requirements,
            ActivityContentProfileAsset content,
            ActivityVisualTransitionMode visualTransitionMode)
        {
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<ActivityAsset>(path);
            if (activity == null)
            {
                activity = ScriptableObject.CreateInstance<ActivityAsset>();
                AssetDatabase.CreateAsset(activity, path);
            }

            activity.name = Path.GetFileNameWithoutExtension(path);
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = CreateActivityId(path);
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("description").stringValue =
                "P3M5A QA-only real Activity lifecycle fixture.";
            serialized.FindProperty("playerParticipationProjectionProfile")
                .objectReferenceValue = projection;
            serialized.FindProperty("playerParticipationRequirementsProfile")
                .objectReferenceValue = requirements;
            serialized.FindProperty("activityContentProfile")
                .objectReferenceValue = content;
            serialized.FindProperty("visualTransitionMode").intValue =
                (int)visualTransitionMode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);
            return activity;
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

        private static string CreateActivityId(string assetPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(assetPath) ?? string.Empty;
            return "qa." + fileName.Replace("_", ".").ToLowerInvariant();
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

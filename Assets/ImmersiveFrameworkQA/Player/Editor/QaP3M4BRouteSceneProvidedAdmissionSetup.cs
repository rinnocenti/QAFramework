using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.PlayerParticipation;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent Edit Mode setup for the P3M4B Route Primary Scene
    /// Scene-Provided Player runtime regression.
    /// </summary>
    internal static class QaP3M4BRouteSceneProvidedAdmissionSetup
    {
        internal const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/SceneProvidedRouteRuntime";

        internal const string ActorPrefabPath =
            RootFolder + "/QA_P3M4B_Actor.prefab";

        internal const string PlayerPrefabPath =
            RootFolder + "/QA_P3M4B_Player.prefab";

        internal const string ActorProfilePath =
            RootFolder + "/QA_P3M4B_ActorProfile.asset";

        internal const string ActivityPath =
            RootFolder + "/QA_P3M4B_Activity.asset";

        internal const string RoutePath =
            RootFolder + "/QA_P3M4B_Route.asset";

        internal const string ScenePath =
            RootFolder + "/QA_P3M4B_RoutePrimary.unity";

        internal const string SceneName =
            "QA_P3M4B_RoutePrimary";

        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4B Setup Route Scene-Provided Admission Fixture";

        private const string LogPrefix =
            "[QA][P3M4B Route Scene-Provided Setup]";

        [MenuItem(MenuPath)]
        internal static void Apply()
        {
            try
            {
                Require(
                    !EditorApplication.isPlaying,
                    "P3M4B setup must run in Edit Mode.");

                Require(
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(),
                    "P3M4B setup was cancelled because modified scenes were not saved.");

                EnsureFolder(RootFolder);

                GameApplicationAsset application =
                    ResolveCanonicalApplication();
                Require(
                    application.TryGetLocalPlayerSlot(
                        0,
                        out PlayerSlotProfile slotProfile),
                    $"Game Application '{application.name}' has no configured local Player Slot at index 0.");

                GameObject actorPrefab =
                    CreateOrUpdateActorPrefab();

                ActorProfile actorProfile =
                    CreateOrUpdateActorProfile(
                        actorPrefab);

                GameObject playerPrefab =
                    CreateOrUpdatePlayerPrefab(
                        slotProfile,
                        actorProfile,
                        actorPrefab);

                ActivityAsset activity =
                    CreateOrUpdateActivity(
                        slotProfile);

                RouteAsset route =
                    CreateOrUpdateRoute(
                        activity);

                CreateOrUpdateScene(
                    playerPrefab);

                EnsureSceneInBuildSettings(
                    ScenePath);

                EditorUtility.SetDirty(actorProfile);
                EditorUtility.SetDirty(activity);
                EditorUtility.SetDirty(route);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"{LogPrefix} PASS. status='Applied' " +
                    $"application='{application.name}' " +
                    $"slot='{slotProfile.name}' " +
                    $"actorPrefab='{ActorPrefabPath}' " +
                    $"playerPrefab='{PlayerPrefabPath}' " +
                    $"scene='{ScenePath}' " +
                    $"activity='{ActivityPath}' " +
                    $"route='{RoutePath}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static GameApplicationAsset ResolveCanonicalApplication()
        {
            string[] guids =
                AssetDatabase.FindAssets(
                    "t:GameApplicationAsset");

            var preferred =
                new List<GameApplicationAsset>();
            var compatible =
                new List<GameApplicationAsset>();

            for (int index = 0; index < guids.Length; index++)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guids[index]);

                GameApplicationAsset application =
                    AssetDatabase.LoadAssetAtPath<
                        GameApplicationAsset>(path);

                if (application == null ||
                    application.LocalPlayerSlotCount <= 0 ||
                    application.StartupRoute == null)
                {
                    continue;
                }

                compatible.Add(application);

                if (string.Equals(
                        application.StartupRoute.PrimarySceneName,
                        "QA_LifecycleRouteA",
                        StringComparison.Ordinal))
                {
                    preferred.Add(application);
                }
            }

            if (preferred.Count == 1)
            {
                return preferred[0];
            }

            if (preferred.Count > 1)
            {
                throw new InvalidOperationException(
                    "More than one GameApplicationAsset with local Player Slots starts in QA_LifecycleRouteA. " +
                    BuildApplicationDiagnostics(preferred));
            }

            if (compatible.Count == 1)
            {
                return compatible[0];
            }

            throw new InvalidOperationException(
                "P3M4B setup requires exactly one compatible GameApplicationAsset, or exactly one whose Startup Route uses QA_LifecycleRouteA. " +
                BuildApplicationDiagnostics(compatible));
        }

        private static string BuildApplicationDiagnostics(
            IReadOnlyList<GameApplicationAsset> applications)
        {
            if (applications == null ||
                applications.Count == 0)
            {
                return "No compatible applications were found.";
            }

            var values =
                new List<string>(
                    applications.Count);

            for (int index = 0;
                 index < applications.Count;
                 index++)
            {
                GameApplicationAsset application =
                    applications[index];

                values.Add(
                    $"asset='{AssetDatabase.GetAssetPath(application)}' " +
                    $"startupRoute='{application.StartupRoute?.name}' " +
                    $"primaryScene='{application.StartupRoute?.PrimarySceneName}' " +
                    $"slots='{application.LocalPlayerSlotCount}'");
            }

            return string.Join(
                "; ",
                values);
        }

        private static GameObject CreateOrUpdateActorPrefab()
        {
            GameObject temporary =
                new GameObject(
                    "QA_P3M4B_Actor");

            try
            {
                PlayerActorDeclaration declaration =
                    temporary.AddComponent<
                        PlayerActorDeclaration>();

                SetString(
                    declaration,
                    "actorId",
                    "qa.p3m4b.actor.1");

                SetString(
                    declaration,
                    "displayName",
                    "QA P3M4B Route Scene-Provided Actor");

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(
                        temporary,
                        ActorPrefabPath);

                Require(
                    prefab != null,
                    $"Could not create Actor prefab at '{ActorPrefabPath}'.");

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    temporary);
            }
        }

        private static ActorProfile CreateOrUpdateActorProfile(
            GameObject actorPrefab)
        {
            ActorProfile profile =
                AssetDatabase.LoadAssetAtPath<
                    ActorProfile>(ActorProfilePath);

            if (profile == null)
            {
                profile =
                    ScriptableObject.CreateInstance<
                        ActorProfile>();
                profile.name =
                    "QA P3M4B Actor Profile";

                AssetDatabase.CreateAsset(
                    profile,
                    ActorProfilePath);
            }

            var serialized =
                new SerializedObject(profile);

            serialized.Update();

            SetRequiredString(
                serialized,
                "actorProfileId",
                "qa.p3m4b.actor.profile");

            SetOptionalString(
                serialized,
                "displayName",
                "QA P3M4B Route Scene-Provided Actor");

            SetRequiredInteger(
                serialized,
                "actorKind",
                (int)ActorKind.Player);

            SetRequiredInteger(
                serialized,
                "actorRole",
                (int)ActorRole.Protagonist);

            SetRequiredObject(
                serialized,
                "logicalActorHostPrefab",
                actorPrefab);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static GameObject CreateOrUpdatePlayerPrefab(
            PlayerSlotProfile slotProfile,
            ActorProfile actorProfile,
            GameObject actorPrefab)
        {
            GameObject temporary =
                new GameObject(
                    "QA_P3M4B_Player");

            try
            {
                PlayerInput playerInput =
                    temporary.AddComponent<
                        PlayerInput>();

                LocalPlayerHostAuthoring host =
                    temporary.AddComponent<
                        LocalPlayerHostAuthoring>();

                GameObject actorMount =
                    new GameObject(
                        "Actor Mount");

                actorMount.transform.SetParent(
                    temporary.transform,
                    false);

                SetObject(
                    host,
                    "playerInput",
                    playerInput);

                SetObject(
                    host,
                    "actorMount",
                    actorMount.transform);

                GameObject actorInstance =
                    PrefabUtility.InstantiatePrefab(
                        actorPrefab) as GameObject;

                Require(
                    actorInstance != null,
                    "Could not instantiate the P3M4B Actor prefab.");

                actorInstance.transform.SetParent(
                    actorMount.transform,
                    false);

                PlayerActorDeclaration actor =
                    actorInstance.GetComponent<
                        PlayerActorDeclaration>();

                Require(
                    actor != null,
                    "P3M4B Actor prefab has no PlayerActorDeclaration.");

                SceneLocalPlayerAdmissionAuthoring authoring =
                    temporary.AddComponent<
                        SceneLocalPlayerAdmissionAuthoring>();

                SetObject(
                    authoring,
                    "playerSlotProfile",
                    slotProfile);

                SetObject(
                    authoring,
                    "actorProfile",
                    actorProfile);

                SetObject(
                    authoring,
                    "sceneLogicalPlayerActor",
                    actor);

                SetInteger(
                    authoring,
                    "admissionTiming",
                    (int)SceneLocalPlayerAdmissionTiming
                        .OnActivityEnter);

                SceneLocalPlayerAdmissionAuthoringResult applied =
                    SceneLocalPlayerAdmissionAuthoringUtility
                        .ApplyOrRebuild(
                            authoring,
                            logDiagnostics: false,
                            useUndo: false);

                Require(
                    applied.Succeeded,
                    "P3M4B Player prefab Apply / Rebuild failed. " +
                    applied.Message);

                GameObject prefab =
                    PrefabUtility.SaveAsPrefabAsset(
                        temporary,
                        PlayerPrefabPath);

                Require(
                    prefab != null,
                    $"Could not create Player prefab at '{PlayerPrefabPath}'.");

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    temporary);
            }
        }

        private static ActivityAsset CreateOrUpdateActivity(
            PlayerSlotProfile slotProfile)
        {
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<
                    ActivityAsset>(ActivityPath);

            if (activity == null)
            {
                activity =
                    ScriptableObject.CreateInstance<
                        ActivityAsset>();
                activity.name =
                    "QA P3M4B Activity";

                AssetDatabase.CreateAsset(
                    activity,
                    ActivityPath);
            }

            var serialized =
                new SerializedObject(activity);

            serialized.Update();

            SetRequiredString(
                serialized,
                "activityId",
                "qa.p3m4b.activity");

            SetRequiredString(
                serialized,
                "activityName",
                "QA P3M4B Route Scene-Provided Activity");

            SetRequiredInteger(
                serialized,
                "playerParticipationProjectionMode",
                (int)ActivityParticipationProjectionMode
                    .ExplicitSlots);

            SetRequiredInteger(
                serialized,
                "playerParticipationZeroParticipantPolicy",
                (int)ActivityParticipationZeroParticipantPolicy
                    .Rejected);

            SerializedProperty slots =
                serialized.FindProperty(
                    "playerParticipationExplicitSlotProfiles");

            Require(
                slots != null,
                "Activity explicit Slot Profile array was not found.");

            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0)
                .objectReferenceValue =
                slotProfile;

            SetRequiredInteger(
                serialized,
                "playerParticipationRequirementLevel",
                (int)PlayerParticipationRequirementLevel
                    .LogicalActorsPrepared);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static RouteAsset CreateOrUpdateRoute(
            ActivityAsset activity)
        {
            RouteAsset route =
                AssetDatabase.LoadAssetAtPath<
                    RouteAsset>(RoutePath);

            if (route == null)
            {
                route =
                    ScriptableObject.CreateInstance<
                        RouteAsset>();
                route.name =
                    "QA P3M4B Route";

                AssetDatabase.CreateAsset(
                    route,
                    RoutePath);
            }

            var serialized =
                new SerializedObject(route);

            serialized.Update();

            SetRequiredString(
                serialized,
                "routeId",
                "qa.p3m4b.route");

            SetRequiredString(
                serialized,
                "routeName",
                "QA P3M4B Route Scene-Provided Admission");

            SetRequiredString(
                serialized,
                "primaryScenePath",
                ScenePath);

            SetRequiredString(
                serialized,
                "primarySceneName",
                SceneName);

            SetRequiredObject(
                serialized,
                "startupActivity",
                activity);

            SetRequiredInteger(
                serialized,
                "transitionGateMode",
                (int)TransitionGateMode
                    .InputInteractionAndGameplay);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return route;
        }

        private static void CreateOrUpdateScene(
            GameObject playerPrefab)
        {
            Scene scene;

            if (AssetDatabase.LoadAssetAtPath<
                    SceneAsset>(ScenePath) != null)
            {
                scene =
                    EditorSceneManager.OpenScene(
                        ScenePath,
                        OpenSceneMode.Additive);

                GameObject[] roots =
                    scene.GetRootGameObjects();

                for (int index = 0;
                     index < roots.Length;
                     index++)
                {
                    UnityEngine.Object.DestroyImmediate(
                        roots[index]);
                }
            }
            else
            {
                scene =
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Additive);
            }

            try
            {
                GameObject instance =
                    PrefabUtility.InstantiatePrefab(
                        playerPrefab,
                        scene) as GameObject;

                Require(
                    instance != null,
                    "Could not instantiate P3M4B Player prefab in the target scene.");

                instance.name =
                    "QA_P3M4B_Player";

                Require(
                    EditorSceneManager.SaveScene(
                        scene,
                        ScenePath),
                    $"Could not save P3M4B scene at '{ScenePath}'.");
            }
            finally
            {
                if (scene.IsValid() &&
                    scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(
                        scene,
                        true);
                }
            }
        }

        private static void EnsureSceneInBuildSettings(
            string scenePath)
        {
            EditorBuildSettingsScene[] current =
                EditorBuildSettings.scenes;

            for (int index = 0;
                 index < current.Length;
                 index++)
            {
                if (string.Equals(
                        current[index].path,
                        scenePath,
                        StringComparison.Ordinal))
                {
                    if (!current[index].enabled)
                    {
                        current[index] =
                            new EditorBuildSettingsScene(
                                scenePath,
                                true);

                        EditorBuildSettings.scenes =
                            current;
                    }

                    return;
                }
            }

            var updated =
                new EditorBuildSettingsScene[
                    current.Length + 1];

            Array.Copy(
                current,
                updated,
                current.Length);

            updated[updated.Length - 1] =
                new EditorBuildSettingsScene(
                    scenePath,
                    true);

            EditorBuildSettings.scenes =
                updated;
        }

        private static void EnsureFolder(
            string folderPath)
        {
            string[] segments =
                folderPath.Split('/');

            string current =
                segments[0];

            for (int index = 1;
                 index < segments.Length;
                 index++)
            {
                string next =
                    current + "/" + segments[index];

                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }

                current = next;
            }
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized =
                new SerializedObject(target);

            serialized.Update();

            SetRequiredObject(
                serialized,
                propertyName,
                value);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serialized =
                new SerializedObject(target);

            serialized.Update();

            SetRequiredInteger(
                serialized,
                propertyName,
                value);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            var serialized =
                new SerializedObject(target);

            serialized.Update();

            SetRequiredString(
                serialized,
                propertyName,
                value);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRequiredObject(
            SerializedObject serialized,
            string propertyName,
            UnityEngine.Object value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing object property '{propertyName}' on '{serialized.targetObject.GetType().Name}'.");

            property.objectReferenceValue =
                value;
        }

        private static void SetRequiredString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing string property '{propertyName}' on '{serialized.targetObject.GetType().Name}'.");

            property.stringValue =
                value ?? string.Empty;
        }

        private static void SetOptionalString(
            SerializedObject serialized,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            if (property != null)
            {
                property.stringValue =
                    value ?? string.Empty;
            }
        }

        private static void SetRequiredInteger(
            SerializedObject serialized,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing integer property '{propertyName}' on '{serialized.targetObject.GetType().Name}'.");

            property.intValue =
                value;
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}

using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent Edit Mode fixture for Scene-Provided Player scope resolution.
    /// Reuses the validated P3M4B Player prefab and creates competing loaded-scene
    /// origins without changing the package or FIRSTGAME.
    /// </summary>
    internal static class QaP3M4CSceneProvidedAdmissionScopeSetup
    {
        internal const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/SceneProvidedScopeRuntime";

        internal const string ActivityContentProfilePath =
            RootFolder + "/QA_P3M4C_ActivityContentProfile.asset";

        internal const string ActivityContentActivityPath =
            RootFolder + "/QA_P3M4C_ActivityContentActivity.asset";

        internal const string MismatchActivityPath =
            RootFolder + "/QA_P3M4C_MismatchActivity.asset";

        internal const string NeutralRoutePath =
            RootFolder + "/QA_P3M4C_NeutralRoute.asset";

        internal const string ForeignRoutePath =
            RootFolder + "/QA_P3M4C_ForeignRoute.asset";

        internal const string ActivityContentScenePath =
            RootFolder + "/QA_P3M4C_ActivityContent.unity";

        internal const string ActivityContentSceneName =
            "QA_P3M4C_ActivityContent";

        internal const string UnrelatedScenePath =
            RootFolder + "/QA_P3M4C_Unrelated.unity";

        internal const string UnrelatedSceneName =
            "QA_P3M4C_Unrelated";

        internal const string ForeignRouteScenePath =
            RootFolder + "/QA_P3M4C_ForeignRoutePrimary.unity";

        internal const string ForeignRouteSceneName =
            "QA_P3M4C_ForeignRoutePrimary";

        internal const string NeutralRouteScenePath =
            RootFolder + "/QA_P3M4C_NeutralRoutePrimary.unity";

        internal const string NeutralRouteSceneName =
            "QA_P3M4C_NeutralRoutePrimary";

        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4C Setup Scene-Provided Scope Fixture";

        private const string LogPrefix =
            "[QA][P3M4C Scene-Provided Scope Setup]";

        [MenuItem(MenuPath)]
        internal static void Apply()
        {
            try
            {
                Require(
                    !EditorApplication.isPlaying,
                    "P3M4C setup must run in Edit Mode.");

                Require(
                    EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo(),
                    "P3M4C setup was cancelled because modified scenes were not saved.");

                QaP3M4BRouteSceneProvidedAdmissionSetup.Apply();

                EnsureFolder(
                    RootFolder);

                GameObject playerPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.PlayerPrefabPath);

                ActivityAsset routeActivity =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.ActivityPath);

                RouteAsset route =
                    AssetDatabase.LoadAssetAtPath<RouteAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath);

                Require(
                    playerPrefab != null &&
                    routeActivity != null &&
                    route != null,
                    "P3M4C requires the P3M4B Player prefab, Activity and Route fixtures.");

                Require(
                    routeActivity.PlayerParticipationExplicitSlotProfiles.Count == 1,
                    "P3M4B Activity must expose exactly one Explicit Slot for P3M4C.");

                PlayerSlotProfile slotProfile =
                    routeActivity.PlayerParticipationExplicitSlotProfiles[0];

                Require(
                    slotProfile != null,
                    "P3M4B Activity Explicit Slot is missing.");

                CreateOrUpdateScene(
                    ActivityContentScenePath,
                    playerPrefab,
                    "QA_P3M4C_ActivityContent_Player");

                CreateOrUpdateScene(
                    UnrelatedScenePath,
                    playerPrefab,
                    "QA_P3M4C_Unrelated_Player");

                CreateOrUpdateScene(
                    ForeignRouteScenePath,
                    playerPrefab,
                    "QA_P3M4C_ForeignRoute_Player");

                CreateOrUpdateScene(
                    NeutralRouteScenePath,
                    null,
                    string.Empty);

                ActivityContentProfileAsset contentProfile =
                    CreateOrUpdateActivityContentProfile();

                ActivityAsset contentActivity =
                    CreateOrUpdateActivity(
                        ActivityContentActivityPath,
                        "qa.p3m4c.activity-content",
                        "QA P3M4C Activity Content Scope",
                        slotProfile,
                        contentProfile);

                ActivityAsset mismatchActivity =
                    CreateOrUpdateActivity(
                        MismatchActivityPath,
                        "qa.p3m4c.mismatch",
                        "QA P3M4C Mismatch Activity",
                        slotProfile,
                        null);

                RouteAsset neutralRoute =
                    CreateOrUpdateRoute(
                        NeutralRoutePath,
                        "qa.p3m4c.neutral-route",
                        "QA P3M4C Neutral Route",
                        NeutralRouteScenePath,
                        NeutralRouteSceneName,
                        contentActivity);

                RouteAsset foreignRoute =
                    CreateOrUpdateRoute(
                        ForeignRoutePath,
                        "qa.p3m4c.foreign-route",
                        "QA P3M4C Foreign Route",
                        ForeignRouteScenePath,
                        ForeignRouteSceneName,
                        mismatchActivity);

                EnsureSceneInBuildSettings(
                    ActivityContentScenePath);

                EnsureSceneInBuildSettings(
                    UnrelatedScenePath);

                EnsureSceneInBuildSettings(
                    ForeignRouteScenePath);

                EnsureSceneInBuildSettings(
                    NeutralRouteScenePath);

                EditorUtility.SetDirty(
                    contentProfile);
                EditorUtility.SetDirty(
                    contentActivity);
                EditorUtility.SetDirty(
                    mismatchActivity);
                EditorUtility.SetDirty(
                    neutralRoute);
                EditorUtility.SetDirty(
                    foreignRoute);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"{LogPrefix} PASS. status='Applied' " +
                    $"playerPrefab='{QaP3M4BRouteSceneProvidedAdmissionSetup.PlayerPrefabPath}' " +
                    $"targetRoute='{QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath}' " +
                    $"activityContentScene='{ActivityContentScenePath}' " +
                    $"unrelatedScene='{UnrelatedScenePath}' " +
                    $"foreignRouteScene='{ForeignRouteScenePath}' " +
                    $"neutralRouteScene='{NeutralRouteScenePath}'.");
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

        private static ActivityContentProfileAsset
            CreateOrUpdateActivityContentProfile()
        {
            ActivityContentProfileAsset profile =
                AssetDatabase.LoadAssetAtPath<
                    ActivityContentProfileAsset>(
                        ActivityContentProfilePath);

            if (profile == null)
            {
                profile =
                    ScriptableObject.CreateInstance<
                        ActivityContentProfileAsset>();

                profile.name =
                    "QA P3M4C Activity Content Profile";

                AssetDatabase.CreateAsset(
                    profile,
                    ActivityContentProfilePath);
            }

            var serialized =
                new SerializedObject(profile);

            serialized.Update();

            SetRequiredString(
                serialized,
                "profileId",
                "qa.p3m4c.activity-content-profile");

            SerializedProperty scenes =
                serialized.FindProperty(
                    "scenes");

            Require(
                scenes != null,
                "Activity Content Profile scenes array was not found.");

            scenes.arraySize = 1;

            SerializedProperty entry =
                scenes.GetArrayElementAtIndex(0);

            SetRequiredRelativeString(
                entry,
                "contentId",
                "qa.p3m4c.activity-content-scene");

            SetRequiredRelativeString(
                entry,
                "scenePath",
                ActivityContentScenePath);

            SetRequiredRelativeString(
                entry,
                "sceneName",
                ActivityContentSceneName);

            SetRequiredRelativeInteger(
                entry,
                "requiredness",
                (int)FrameworkContentRequiredness.Required);

            SetRequiredRelativeInteger(
                entry,
                "loadMode",
                (int)ActivityContentSceneLoadMode.Additive);

            SetRequiredRelativeInteger(
                entry,
                "releasePolicy",
                (int)ActivityContentReleasePolicy.ReleaseOnActivityChange);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static ActivityAsset CreateOrUpdateActivity(
            string path,
            string activityId,
            string activityName,
            PlayerSlotProfile slotProfile,
            ActivityContentProfileAsset contentProfile)
        {
            ActivityAsset activity =
                AssetDatabase.LoadAssetAtPath<
                    ActivityAsset>(path);

            if (activity == null)
            {
                activity =
                    ScriptableObject.CreateInstance<
                        ActivityAsset>();

                activity.name =
                    activityName;

                AssetDatabase.CreateAsset(
                    activity,
                    path);
            }

            var serialized =
                new SerializedObject(activity);

            serialized.Update();

            SetRequiredString(
                serialized,
                "activityId",
                activityId);

            SetRequiredString(
                serialized,
                "activityName",
                activityName);

            SetRequiredInteger(
                serialized,
                "playerParticipationProjectionMode",
                (int)ActivityParticipationProjectionMode.ExplicitSlots);

            SetRequiredInteger(
                serialized,
                "playerParticipationZeroParticipantPolicy",
                (int)ActivityParticipationZeroParticipantPolicy.Rejected);

            SerializedProperty slots =
                serialized.FindProperty(
                    "playerParticipationExplicitSlotProfiles");

            Require(
                slots != null,
                "Activity Explicit Slot array was not found.");

            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0)
                .objectReferenceValue =
                slotProfile;

            SetRequiredInteger(
                serialized,
                "playerParticipationRequirementLevel",
                (int)PlayerParticipationRequirementLevel.LogicalActorsPrepared);

            SetRequiredObject(
                serialized,
                "activityContentProfile",
                contentProfile);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static RouteAsset CreateOrUpdateRoute(
            string path,
            string routeId,
            string routeName,
            string primaryScenePath,
            string primarySceneName,
            ActivityAsset startupActivity)
        {
            RouteAsset route =
                AssetDatabase.LoadAssetAtPath<
                    RouteAsset>(path);

            if (route == null)
            {
                route =
                    ScriptableObject.CreateInstance<
                        RouteAsset>();

                route.name =
                    routeName;

                AssetDatabase.CreateAsset(
                    route,
                    path);
            }

            var serialized =
                new SerializedObject(route);

            serialized.Update();

            SetRequiredString(
                serialized,
                "routeId",
                routeId);

            SetRequiredString(
                serialized,
                "routeName",
                routeName);

            SetRequiredString(
                serialized,
                "primaryScenePath",
                primaryScenePath);

            SetRequiredString(
                serialized,
                "primarySceneName",
                primarySceneName);

            SetRequiredObject(
                serialized,
                "startupActivity",
                startupActivity);

            SetRequiredInteger(
                serialized,
                "transitionGateMode",
                (int)TransitionGateMode.InputInteractionAndGameplay);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return route;
        }

        private static void CreateOrUpdateScene(
            string scenePath,
            GameObject playerPrefab,
            string instanceName)
        {
            Scene scene;

            if (AssetDatabase.LoadAssetAtPath<
                    SceneAsset>(scenePath) != null)
            {
                scene =
                    EditorSceneManager.OpenScene(
                        scenePath,
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
                if (playerPrefab != null)
                {
                    GameObject instance =
                        PrefabUtility.InstantiatePrefab(
                            playerPrefab,
                            scene) as GameObject;

                    Require(
                        instance != null,
                        $"Could not instantiate Player prefab in '{scenePath}'.");

                    instance.name =
                        instanceName;
                }

                Require(
                    EditorSceneManager.SaveScene(
                        scene,
                        scenePath),
                    $"Could not save P3M4C scene at '{scenePath}'.");
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
                if (!string.Equals(
                        current[index].path,
                        scenePath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

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

                current =
                    next;
            }
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

        private static void SetRequiredRelativeString(
            SerializedProperty parent,
            string propertyName,
            string value)
        {
            SerializedProperty property =
                parent.FindPropertyRelative(
                    propertyName);

            Require(
                property != null,
                $"Missing relative string property '{propertyName}'.");

            property.stringValue =
                value ?? string.Empty;
        }

        private static void SetRequiredRelativeInteger(
            SerializedProperty parent,
            string propertyName,
            int value)
        {
            SerializedProperty property =
                parent.FindPropertyRelative(
                    propertyName);

            Require(
                property != null,
                $"Missing relative integer property '{propertyName}'.");

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

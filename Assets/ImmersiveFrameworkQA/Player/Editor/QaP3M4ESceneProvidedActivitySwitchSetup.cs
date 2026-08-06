using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent Edit Mode setup for the public Activity switch regression
    /// using the P3M4B Route Primary Scene Scene-Provided Player.
    /// </summary>
    internal static class QaP3M4ESceneProvidedActivitySwitchSetup
    {
        internal const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/SceneProvidedActivitySwitchRuntime";

        internal const string ActivityBPath =
            RootFolder + "/QA_P3M4E_ActivityB.asset";

        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4E Setup Scene-Provided Activity Switch Fixture";

        private const string LogPrefix =
            "[QA][P3M4E Scene-Provided Activity Switch Setup]";

        [MenuItem(MenuPath)]
        internal static void Apply()
        {
            try
            {
                Require(
                    !EditorApplication.isPlaying,
                    "P3M4E setup must run in Edit Mode.");

                QaP3M4BRouteSceneProvidedAdmissionSetup.Apply();

                EnsureFolder(
                    RootFolder);

                RouteAsset route =
                    AssetDatabase.LoadAssetAtPath<RouteAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath);

                ActivityAsset activityA =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.ActivityPath);

                GameObject playerPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.PlayerPrefabPath);

                SceneAsset routeScene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.ScenePath);

                Require(
                    route != null &&
                    activityA != null &&
                    playerPrefab != null &&
                    routeScene != null,
                    "P3M4E requires the complete P3M4B Route Scene-Provided fixture.");

                Require(
                    ReferenceEquals(
                        route.StartupActivity,
                        activityA),
                    "P3M4B Route does not reference the expected Activity A.");

                Require(
                    activityA.PlayerParticipationExplicitSlotProfiles.Count == 1,
                    "P3M4B Activity A must expose exactly one Explicit Slot.");

                var slotProfile =
                    activityA.PlayerParticipationExplicitSlotProfiles[0];

                Require(
                    slotProfile != null,
                    "P3M4B Activity A Explicit Slot is missing.");

                ActivityAsset activityB =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                        ActivityBPath);

                if (activityB == null)
                {
                    activityB =
                        ScriptableObject.CreateInstance<ActivityAsset>();

                    activityB.name =
                        "QA P3M4E Activity B";

                    AssetDatabase.CreateAsset(
                        activityB,
                        ActivityBPath);
                }

                var serialized =
                    new SerializedObject(
                        activityB);

                serialized.Update();

                SetRequiredString(
                    serialized,
                    "activityId",
                    "qa.p3m4e.activity-b");

                SetRequiredString(
                    serialized,
                    "activityName",
                    "QA P3M4E Scene-Provided Activity B");

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
                    "Activity B Explicit Slot array was not found.");

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
                    null);

                SetRequiredInteger(
                    serialized,
                    "visualTransitionMode",
                    (int)ActivityVisualTransitionMode.Seamless);

                SetRequiredInteger(
                    serialized,
                    "transitionGateMode",
                    (int)TransitionGateMode.InputInteractionAndGameplay);

                serialized.ApplyModifiedPropertiesWithoutUndo();

                EditorUtility.SetDirty(
                    activityB);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    $"{LogPrefix} PASS. status='Applied' " +
                    $"route='{QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath}' " +
                    $"activityA='{QaP3M4BRouteSceneProvidedAdmissionSetup.ActivityPath}' " +
                    $"activityB='{ActivityBPath}' " +
                    $"scene='{QaP3M4BRouteSceneProvidedAdmissionSetup.ScenePath}' " +
                    $"playerPrefab='{QaP3M4BRouteSceneProvidedAdmissionSetup.PlayerPrefabPath}'.");
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

                if (!AssetDatabase.IsValidFolder(
                        next))
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

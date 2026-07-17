using System;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent P3J.6 fixture. Extends P3J.5 with one Activity that projects all joined
    /// local Player Slots and requires Logical Actors Prepared.
    /// </summary>
    internal static class QaP3J6ActivityPlayerActorLifecycleSetup
    {
        private const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/P3J6";
        internal const string ProjectionPath =
            RootFolder + "/P3J6_AllJoinedProjection.asset";
        internal const string RequirementsPath =
            RootFolder + "/P3J6_LogicalActorsPrepared.asset";
        internal const string ActivityPath =
            RootFolder + "/P3J6_PlayerActorLifecycleActivity.asset";
        internal const string NegativeProjectionPath =
            RootFolder + "/P3J6_ExplicitUnjoinedProjection.asset";
        internal const string NegativeActivityPath =
            RootFolder + "/P3J6_UnjoinedSlotRequiredActivity.asset";

        internal static void Apply()
        {
            try
            {
                QaP3J5RuntimeHostPreparationSetup.Apply();
                EnsureFolder(RootFolder);

                ActivityParticipationProjectionProfile projection =
                    CreateOrUpdateProjection();
                PlayerParticipationRequirementsProfile requirements =
                    CreateOrUpdateRequirements();
                ActivityAsset activity = CreateOrUpdateActivity(
                    ActivityPath,
                    "P3J6 Player Actor Lifecycle Activity",
                    projection,
                    requirements);

                ImmersiveFrameworkSettingsAsset settings =
                    Resources.Load<ImmersiveFrameworkSettingsAsset>(
                        ImmersiveFrameworkSettingsAsset.ResourcesPath);
                if (settings == null || settings.ActiveGameApplication == null ||
                    !settings.ActiveGameApplication.TryGetLocalPlayerSlot(
                        1,
                        out PlayerSlotProfile secondSlot) ||
                    secondSlot == null)
                {
                    throw new InvalidOperationException(
                        "P3J.6 negative readiness fixture requires a configured second Local Player Slot.");
                }

                ActivityParticipationProjectionProfile negativeProjection =
                    CreateOrUpdateExplicitProjection(secondSlot);
                ActivityAsset negativeActivity = CreateOrUpdateActivity(
                    NegativeActivityPath,
                    "P3J6 Unjoined Slot Required Activity",
                    negativeProjection,
                    requirements);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3J6_ACTIVITY_PLAYER_ACTOR_LIFECYCLE_FIXTURE] status='Applied' " +
                    $"activity='{activity.ActivityName}' projection='{projection.ProjectionMode}' " +
                    $"zeroPolicy='{projection.ZeroParticipantPolicy}' " +
                    $"requirement='{requirements.RequirementLevel}' " +
                    $"negativeActivity='{negativeActivity.ActivityName}' " +
                    $"negativeSlot='{secondSlot.PlayerSlotId.StableText}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3J6_ACTIVITY_PLAYER_ACTOR_LIFECYCLE_FIXTURE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static ActivityParticipationProjectionProfile
            CreateOrUpdateProjection()
        {
            ActivityParticipationProjectionProfile profile =
                AssetDatabase.LoadAssetAtPath<ActivityParticipationProjectionProfile>(
                    ProjectionPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    ActivityParticipationProjectionProfile>();
                AssetDatabase.CreateAsset(profile, ProjectionPath);
            }

            profile.name = "P3J6 All Joined Players";
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue =
                "P3J.6 — All Joined Players";
            serialized.FindProperty("description").stringValue =
                "Projects every currently Joined Session Slot in configured order.";
            serialized.FindProperty("projectionMode").intValue =
                (int)ActivityParticipationProjectionMode.AllJoinedSlots;
            serialized.FindProperty("zeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Rejected;
            serialized.FindProperty("explicitSlotProfiles").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static PlayerParticipationRequirementsProfile
            CreateOrUpdateRequirements()
        {
            PlayerParticipationRequirementsProfile profile =
                AssetDatabase.LoadAssetAtPath<PlayerParticipationRequirementsProfile>(
                    RequirementsPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    PlayerParticipationRequirementsProfile>();
                AssetDatabase.CreateAsset(profile, RequirementsPath);
            }

            profile.name = "P3J6 Logical Actors Prepared";
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue =
                "P3J.6 — Logical Actors Prepared";
            serialized.FindProperty("description").stringValue =
                "Every projected joined Slot must have its selected/default Logical Actor prepared by the Activity owner.";
            serialized.FindProperty("requirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.LogicalActorsPrepared;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityParticipationProjectionProfile
            CreateOrUpdateExplicitProjection(PlayerSlotProfile slot)
        {
            ActivityParticipationProjectionProfile profile =
                AssetDatabase.LoadAssetAtPath<ActivityParticipationProjectionProfile>(
                    NegativeProjectionPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<
                    ActivityParticipationProjectionProfile>();
                AssetDatabase.CreateAsset(profile, NegativeProjectionPath);
            }

            profile.name = "P3J6 Explicit Unjoined Slot";
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue =
                "P3J.6 — Explicit Unjoined Slot";
            serialized.FindProperty("description").stringValue =
                "Negative readiness fixture requiring the configured second Slot before it joins.";
            serialized.FindProperty("projectionMode").intValue =
                (int)ActivityParticipationProjectionMode.ExplicitSlots;
            serialized.FindProperty("zeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Rejected;
            SerializedProperty slots =
                serialized.FindProperty("explicitSlotProfiles");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = slot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static ActivityAsset CreateOrUpdateActivity(
            string assetPath,
            string activityName,
            ActivityParticipationProjectionProfile projection,
            PlayerParticipationRequirementsProfile requirements)
        {
            ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                assetPath);
            if (activity == null)
            {
                activity = ScriptableObject.CreateInstance<ActivityAsset>();
                AssetDatabase.CreateAsset(activity, assetPath);
            }

            activity.name = activityName;
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityId").stringValue = CreateActivityId(assetPath);
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("description").stringValue =
                "QA Activity proving Activity-owned Logical Player Actor lifecycle and readiness.";
            serialized.FindProperty("playerParticipationProjectionProfile")
                .objectReferenceValue = projection;
            serialized.FindProperty("playerParticipationRequirementsProfile")
                .objectReferenceValue = requirements;
            serialized.FindProperty("activityContentProfile")
                .objectReferenceValue = null;
            serialized.FindProperty("visualTransitionMode").intValue =
                (int)ActivityVisualTransitionMode.Seamless;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(activity);
            return activity;
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

        private static string CreateActivityId(string assetPath)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath) ?? string.Empty;
            return "qa." + fileName.Replace("_", ".").ToLowerInvariant();
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

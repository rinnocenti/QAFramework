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
        internal const string RequirementsPath =
            RootFolder + "/P3J6_LogicalActorsPrepared.asset";
        internal const string ActivityPath =
            RootFolder + "/P3J6_PlayerActorLifecycleActivity.asset";
        internal const string NegativeActivityPath =
            RootFolder + "/P3J6_UnjoinedSlotRequiredActivity.asset";

        internal static void Apply()
        {
            try
            {
                QaP3J5RuntimeHostPreparationSetup.Apply();
                EnsureFolder(RootFolder);

                PlayerParticipationRequirementsProfile requirements =
                    CreateOrUpdateRequirements();
                ActivityAsset activity = CreateOrUpdateActivity(
                    ActivityPath,
                    "P3J6 Player Actor Lifecycle Activity",
                    requirements,
                    ActivityParticipationProjectionMode.AllJoinedSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected);

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

                ActivityAsset negativeActivity = CreateOrUpdateActivity(
                    NegativeActivityPath,
                    "P3J6 Unjoined Slot Required Activity",
                    requirements,
                    ActivityParticipationProjectionMode.ExplicitSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected,
                    secondSlot);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3J6_ACTIVITY_PLAYER_ACTOR_LIFECYCLE_FIXTURE] status='Applied' " +
                    $"activity='{activity.ActivityName}' projection='{activity.PlayerParticipationProjectionMode}' " +
                    $"zeroPolicy='{activity.PlayerParticipationZeroParticipantPolicy}' " +
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

        private static ActivityAsset CreateOrUpdateActivity(
            string assetPath,
            string activityName,
            PlayerParticipationRequirementsProfile requirements,
            ActivityParticipationProjectionMode projectionMode,
            ActivityParticipationZeroParticipantPolicy zeroPolicy,
            params PlayerSlotProfile[] explicitSlots)
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
            serialized.FindProperty("playerParticipationProjectionMode").intValue =
                (int)projectionMode;
            serialized.FindProperty("playerParticipationZeroParticipantPolicy").intValue =
                (int)zeroPolicy;
            SerializedProperty slots =
                serialized.FindProperty("playerParticipationExplicitSlotProfiles");
            int count = explicitSlots != null ? explicitSlots.Length : 0;
            slots.arraySize = count;
            for (int index = 0; index < count; index++)
            {
                slots.GetArrayElementAtIndex(index).objectReferenceValue = explicitSlots[index];
            }
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

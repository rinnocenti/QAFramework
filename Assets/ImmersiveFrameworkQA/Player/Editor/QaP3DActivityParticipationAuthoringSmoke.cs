using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// P3D editor-only smoke for Activity Projection and Requirements authoring.
    /// Uses temporary assets only and removes the complete fixture after execution.
    /// </summary>
    internal static class QaP3DActivityParticipationAuthoringSmoke
    {
        private const string TempFolder =
            "Assets/ImmersiveFrameworkQA/__P3D_ActivityParticipationAuthoring_Temp";

        private const string ValidatorTypeName =
            "Immersive.Framework.Editor.Editor.PlayerParticipation.ActivityParticipationProjectionAuthoringValidator";

        internal static void Run()
        {
            var completed = new List<string>();

            try
            {
                PrepareTempFolder();
                RunSmoke(completed);

                Debug.Log(
                    "[P3D_ACTIVITY_PARTICIPATION_AUTHORING_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3D_ACTIVITY_PARTICIPATION_AUTHORING_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                CleanupTempFolder();
            }
        }

        private static void RunSmoke(List<string> completed)
        {
            PlayerSlotProfile playerOne = CreateSlotProfile("P3D_Player_One", "qa.p3d.player.1");
            PlayerSlotProfile playerTwo = CreateSlotProfile("P3D_Player_Two", "qa.p3d.player.2");
            PlayerParticipationRequirementLevel none =
                PlayerParticipationRequirementLevel.None;
            PlayerParticipationRequirementLevel joined =
                PlayerParticipationRequirementLevel.JoinedSlots;

            ActivityAsset noPlayersActivity = CreateActivity(
                "P3D_Activity_NoPlayers",
                "QA P3D No Players",
                none,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            AssertNoErrors(ValidateActivity(noPlayersActivity), "NoSlots + None was rejected.");
            completed.Add("no-slots-none-valid");

            ActivityAsset allJoinedActivity = CreateActivity(
                "P3D_Activity_AllJoined",
                "QA P3D All Joined",
                joined,
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            AssertNoErrors(ValidateActivity(allJoinedActivity), "AllJoinedSlots + JoinedSlots was rejected.");
            completed.Add("all-joined-zero-allowed-valid");

            AssertTrue(
                CreateActivity(
                        "P3D_Activity_Explicit",
                        "QA P3D Explicit",
                        joined,
                        ActivityParticipationProjectionMode.ExplicitSlots,
                        ActivityParticipationZeroParticipantPolicy.Rejected,
                        playerOne,
                        playerTwo)
                    .TryGetPlayerParticipationProjectionDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out string descriptorIssue),
                $"Explicit descriptor failed. issue='{descriptorIssue}'.");
            AssertEqual(2, descriptor.ExplicitSlotProfiles.Count, "Explicit descriptor Slot count changed.");
            AssertSame(playerOne, descriptor.ExplicitSlotProfiles[0], "Explicit Slot order index 0 changed.");
            AssertSame(playerTwo, descriptor.ExplicitSlotProfiles[1], "Explicit Slot order index 1 changed.");
            completed.Add("activity-owned-explicit-slots-order-preserved");

            ActivityAsset defaultProjection = CreateActivity(
                "P3D_Activity_DefaultProjection",
                "QA P3D Default Projection",
                none,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            AssertNoErrors(
                ValidateActivity(defaultProjection),
                "Default Activity-owned NoSlots projection was rejected.");
            completed.Add("activity-owned-default-projection-valid");

            ActivityAsset invalidRequirements = CreateActivity(
                "P3D_Activity_InvalidRequirements",
                "QA P3D Invalid Requirements",
                (PlayerParticipationRequirementLevel)999,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            object invalidRequirementsReport = ValidateActivity(invalidRequirements);
            AssertHasErrors(invalidRequirementsReport, "Invalid Requirement Level was accepted.");
            AssertReportContains(invalidRequirementsReport, "invalid Player participation Requirement Level");
            completed.Add("invalid-requirement-level-rejected");

            ActivityAsset contradictory = CreateActivity(
                "P3D_Activity_Contradictory",
                "QA P3D Contradictory",
                joined,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            object contradictoryReport = ValidateActivity(contradictory);
            AssertHasErrors(contradictoryReport, "NoSlots + JoinedSlots contradiction was accepted.");
            AssertReportContains(contradictoryReport, "projects No Slots");
            completed.Add("no-slots-non-none-rejected");

            ActivityAsset allJoinedWithExplicit = CreateActivity(
                "P3D_Activity_AllJoined_WithExplicit",
                "QA P3D All Joined With Explicit",
                joined,
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                playerOne);
            object allJoinedExplicitReport = ValidateActivity(allJoinedWithExplicit);
            AssertHasErrors(allJoinedExplicitReport, "AllJoinedSlots with explicit references was accepted.");
            AssertReportContains(allJoinedExplicitReport, "contains 1 Explicit Slot reference");
            completed.Add("all-joined-explicit-list-rejected");

            ActivityAsset explicitEmpty = CreateActivity(
                "P3D_Activity_Explicit_Empty",
                "QA P3D Explicit Empty",
                joined,
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected);
            object explicitEmptyReport = ValidateActivity(explicitEmpty);
            AssertHasErrors(explicitEmptyReport, "Empty ExplicitSlots was accepted.");
            AssertReportContains(explicitEmptyReport, "has no PlayerSlotProfile references");
            completed.Add("explicit-empty-rejected");

            ActivityAsset explicitDuplicate = CreateActivity(
                "P3D_Activity_Explicit_Duplicate",
                "QA P3D Explicit Duplicate",
                joined,
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected,
                playerOne,
                playerOne);
            object explicitDuplicateReport = ValidateActivity(explicitDuplicate);
            AssertHasErrors(explicitDuplicateReport, "Duplicate Explicit Slot Profile was accepted.");
            AssertReportContains(explicitDuplicateReport, "repeats PlayerSlotProfile");
            completed.Add("explicit-duplicate-rejected");

            string activityBefore = EditorJsonUtility.ToJson(allJoinedActivity);
            ValidateActivity(allJoinedActivity);
            AssertEqual(
                activityBefore,
                EditorJsonUtility.ToJson(allJoinedActivity),
                "Activity validation mutated the Activity.");
            completed.Add("validation-is-non-mutating");

            ActivityAsset projectScanTarget = CreateActivity(
                "P3D_Activity_ProjectScanTarget",
                "QA P3D Project Scan Invalid Requirements",
                (PlayerParticipationRequirementLevel)999,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            object projectReport = ValidateProjectAssets(FrameworkValidationMode.Standard);
            AssertHasErrors(projectReport, "Project scan accepted an Activity with an invalid Requirement Level.");
            AssertReportContains(projectReport, projectScanTarget.ActivityName);
            completed.Add("project-scan-detects-invalid-activity");
        }

        private static PlayerSlotProfile CreateSlotProfile(string fileName, string playerSlotId)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = fileName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = playerSlotId;
            serialized.FindProperty("displayName").stringValue = fileName;
            serialized.FindProperty("description").stringValue = "P3D QA Player Slot fixture.";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            return profile;
        }

        private static ActivityAsset CreateActivity(
            string fileName,
            string activityName,
            PlayerParticipationRequirementLevel requirementLevel,
            ActivityParticipationProjectionMode projectionMode,
            ActivityParticipationZeroParticipantPolicy zeroPolicy,
            params PlayerSlotProfile[] explicitSlots)
        {
            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = fileName;
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("description").stringValue = "P3D QA Activity fixture.";
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
            serialized.FindProperty("playerParticipationRequirementLevel").intValue =
                (int)requirementLevel;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(activity, $"{TempFolder}/{fileName}.asset");
            AssetDatabase.SaveAssets();
            return activity;
        }

        private static object ValidateActivity(ActivityAsset activity)
        {
            return InvokeValidator(
                "ValidateActivity",
                new[] { typeof(ActivityAsset) },
                new object[] { activity });
        }

        private static object ValidateProjectAssets(FrameworkValidationMode validationMode)
        {
            return InvokeValidator(
                "ValidateProjectAssets",
                new[] { typeof(FrameworkValidationMode) },
                new object[] { validationMode });
        }

        private static object InvokeValidator(
            string methodName,
            Type[] parameterTypes,
            object[] arguments)
        {
            Type validatorType = ResolveType(ValidatorTypeName);
            AssertNotNull(validatorType, $"Package validator type was not found: '{ValidatorTypeName}'.");

            MethodInfo method = validatorType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            AssertNotNull(method, $"Package validator method was not found: '{methodName}'.");

            try
            {
                object report = method.Invoke(null, arguments);
                AssertNotNull(report, $"Package validator '{methodName}' returned null.");
                return report;
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw new InvalidOperationException(
                    $"Package validator '{methodName}' failed. {exception.InnerException.Message}",
                    exception.InnerException);
            }
        }

        private static Type ResolveType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Type type = assemblies[index].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private static void PrepareTempFolder()
        {
            CleanupTempFolder();
            string guid = AssetDatabase.CreateFolder("Assets/ImmersiveFrameworkQA", "__P3D_ActivityParticipationAuthoring_Temp");
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssertEqual(TempFolder, path, "P3D temporary folder path differs from the expected path.");
        }

        private static void CleanupTempFolder()
        {
            if (AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.DeleteAsset(TempFolder);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static int ReadInt(object target, string propertyName)
        {
            object value = ReadProperty(target, propertyName);
            return value is int intValue
                ? intValue
                : throw new InvalidOperationException(
                    $"Property '{propertyName}' is not an Int32 on '{target.GetType().FullName}'.");
        }

        private static object ReadProperty(object target, string propertyName)
        {
            AssertNotNull(target, $"Cannot read '{propertyName}' from a null target.");
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(property, $"Property '{propertyName}' was not found on '{target.GetType().FullName}'.");
            return property.GetValue(target);
        }

        private static void AssertReportContains(object report, string fragment)
        {
            object issuesValue = ReadProperty(report, "Issues");
            AssertTrue(issuesValue is IEnumerable, "Validation report Issues is not enumerable.");
            foreach (object issue in (IEnumerable)issuesValue)
            {
                object message = ReadProperty(issue, "Message");
                if (message is string text && text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }
            }
            throw new InvalidOperationException(
                $"Validation report did not contain expected text '{fragment}'.");
        }

        private static void AssertNoErrors(object report, string message)
        {
            if (ReadInt(report, "ErrorCount") != 0)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertHasErrors(object report, string message)
        {
            if (ReadInt(report, "ErrorCount") <= 0)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
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

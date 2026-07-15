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

        private const string ProjectionTemplateMenuPath =
            "Assets/Create/Immersive Framework/Player/Templates/Activity Projection Set";

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
            PlayerParticipationRequirementsProfile none = CreateRequirementsProfile(
                "P3D_Requirements_None",
                PlayerParticipationRequirementLevel.None);
            PlayerParticipationRequirementsProfile joined = CreateRequirementsProfile(
                "P3D_Requirements_Joined",
                PlayerParticipationRequirementLevel.JoinedSlots);

            ActivityParticipationProjectionProfile noSlots = CreateProjectionProfile(
                "P3D_Projection_NoSlots",
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            ActivityParticipationProjectionProfile allJoinedAllowed = CreateProjectionProfile(
                "P3D_Projection_AllJoined_Allowed",
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            ActivityParticipationProjectionProfile explicitSlots = CreateProjectionProfile(
                "P3D_Projection_Explicit",
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected,
                playerOne,
                playerTwo);

            ActivityAsset noPlayersActivity = CreateActivity(
                "P3D_Activity_NoPlayers",
                "QA P3D No Players",
                noSlots,
                none);
            AssertNoErrors(ValidateActivity(noPlayersActivity), "NoSlots + None was rejected.");
            completed.Add("no-slots-none-valid");

            ActivityAsset allJoinedActivity = CreateActivity(
                "P3D_Activity_AllJoined",
                "QA P3D All Joined",
                allJoinedAllowed,
                joined);
            AssertNoErrors(ValidateActivity(allJoinedActivity), "AllJoinedSlots + JoinedSlots was rejected.");
            completed.Add("all-joined-zero-allowed-valid");

            AssertTrue(
                explicitSlots.TryCreateDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out string descriptorIssue),
                $"Explicit descriptor failed. issue='{descriptorIssue}'.");
            AssertEqual(2, descriptor.ExplicitSlotProfiles.Count, "Explicit descriptor Slot count changed.");
            AssertSame(playerOne, descriptor.ExplicitSlotProfiles[0], "Explicit Slot order index 0 changed.");
            AssertSame(playerTwo, descriptor.ExplicitSlotProfiles[1], "Explicit Slot order index 1 changed.");
            completed.Add("explicit-slots-order-preserved");

            ActivityAsset missingProjection = CreateActivity(
                "P3D_Activity_MissingProjection",
                "QA P3D Missing Projection",
                null,
                none);
            object missingProjectionReport = ValidateActivity(missingProjection);
            AssertHasErrors(missingProjectionReport, "Missing Projection was accepted.");
            AssertReportContains(missingProjectionReport, "missing its mandatory Activity Participation Projection Profile");
            completed.Add("missing-projection-rejected");

            ActivityAsset missingRequirements = CreateActivity(
                "P3D_Activity_MissingRequirements",
                "QA P3D Missing Requirements",
                noSlots,
                null);
            object missingRequirementsReport = ValidateActivity(missingRequirements);
            AssertHasErrors(missingRequirementsReport, "Missing Requirements was accepted.");
            AssertReportContains(missingRequirementsReport, "missing its mandatory Player Participation Requirements Profile");
            completed.Add("missing-requirements-rejected");

            ActivityAsset contradictory = CreateActivity(
                "P3D_Activity_Contradictory",
                "QA P3D Contradictory",
                noSlots,
                joined);
            object contradictoryReport = ValidateActivity(contradictory);
            AssertHasErrors(contradictoryReport, "NoSlots + JoinedSlots contradiction was accepted.");
            AssertReportContains(contradictoryReport, "projects No Slots");
            completed.Add("no-slots-non-none-rejected");

            ActivityParticipationProjectionProfile allJoinedWithExplicit = CreateProjectionProfile(
                "P3D_Projection_AllJoined_WithExplicit",
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                playerOne);
            object allJoinedExplicitReport = ValidateProjectionProfile(allJoinedWithExplicit);
            AssertHasErrors(allJoinedExplicitReport, "AllJoinedSlots with explicit references was accepted.");
            AssertReportContains(allJoinedExplicitReport, "contains 1 Explicit Slot reference");
            completed.Add("all-joined-explicit-list-rejected");

            ActivityParticipationProjectionProfile explicitEmpty = CreateProjectionProfile(
                "P3D_Projection_Explicit_Empty",
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected);
            object explicitEmptyReport = ValidateProjectionProfile(explicitEmpty);
            AssertHasErrors(explicitEmptyReport, "Empty ExplicitSlots was accepted.");
            AssertReportContains(explicitEmptyReport, "has no PlayerSlotProfile references");
            completed.Add("explicit-empty-rejected");

            ActivityParticipationProjectionProfile explicitDuplicate = CreateProjectionProfile(
                "P3D_Projection_Explicit_Duplicate",
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected,
                playerOne,
                playerOne);
            object explicitDuplicateReport = ValidateProjectionProfile(explicitDuplicate);
            AssertHasErrors(explicitDuplicateReport, "Duplicate Explicit Slot Profile was accepted.");
            AssertReportContains(explicitDuplicateReport, "repeats PlayerSlotProfile");
            completed.Add("explicit-duplicate-rejected");

            string projectionBefore = EditorJsonUtility.ToJson(explicitSlots);
            string activityBefore = EditorJsonUtility.ToJson(allJoinedActivity);
            ValidateProjectionProfile(explicitSlots);
            ValidateActivity(allJoinedActivity);
            AssertEqual(
                projectionBefore,
                EditorJsonUtility.ToJson(explicitSlots),
                "Projection validation mutated the Profile.");
            AssertEqual(
                activityBefore,
                EditorJsonUtility.ToJson(allJoinedActivity),
                "Activity validation mutated the Activity.");
            completed.Add("validation-is-non-mutating");

            RunProjectionTemplateCase(completed);

            ActivityAsset projectScanTarget = CreateActivity(
                "P3D_Activity_ProjectScanTarget",
                "QA P3D Project Scan Missing Profiles",
                null,
                null);
            object projectReport = ValidateProjectAssets(FrameworkValidationMode.Standard);
            AssertHasErrors(projectReport, "Project scan accepted an Activity with missing Profiles.");
            AssertReportContains(projectReport, projectScanTarget.ActivityName);
            completed.Add("project-scan-detects-invalid-activity");
        }

        private static void RunProjectionTemplateCase(List<string> completed)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(TempFolder);
            bool executed = EditorApplication.ExecuteMenuItem(ProjectionTemplateMenuPath);
            AssertTrue(executed, $"Template menu command was not found: '{ProjectionTemplateMenuPath}'.");

            string templateFolder = $"{TempFolder}/PlayerParticipation";
            AssertTrue(
                AssetDatabase.IsValidFolder(templateFolder),
                "Activity Projection template command did not create its folder.");

            string[] guids = AssetDatabase.FindAssets(
                "t:ActivityParticipationProjectionProfile",
                new[] { templateFolder });
            AssertEqual(3, guids.Length, "Activity Projection template set did not create three Profiles.");

            int noSlots = 0;
            int allJoinedAllowed = 0;
            int allJoinedRejected = 0;
            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                ActivityParticipationProjectionProfile profile =
                    AssetDatabase.LoadAssetAtPath<ActivityParticipationProjectionProfile>(path);
                AssertNotNull(profile, $"Projection template at '{path}' could not be loaded.");
                AssertTrue(
                    profile.TryCreateDescriptor(
                        out ActivityParticipationProjectionDescriptor descriptor,
                        out string issue),
                    $"Projection template '{profile.name}' is invalid. issue='{issue}'.");

                if (descriptor.ProjectsNoSlots)
                {
                    noSlots++;
                }
                else if (descriptor.ProjectsAllJoinedSlots && descriptor.AllowsZeroParticipants)
                {
                    allJoinedAllowed++;
                }
                else if (descriptor.ProjectsAllJoinedSlots && !descriptor.AllowsZeroParticipants)
                {
                    allJoinedRejected++;
                }
            }

            AssertEqual(1, noSlots, "Projection template set requires one NoSlots Profile.");
            AssertEqual(1, allJoinedAllowed, "Projection template set requires one zero-allowed AllJoined Profile.");
            AssertEqual(1, allJoinedRejected, "Projection template set requires one zero-rejected AllJoined Profile.");
            completed.Add("projection-template-set-created");
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

        private static PlayerParticipationRequirementsProfile CreateRequirementsProfile(
            string fileName,
            PlayerParticipationRequirementLevel level)
        {
            var profile = ScriptableObject.CreateInstance<PlayerParticipationRequirementsProfile>();
            profile.name = fileName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = fileName;
            serialized.FindProperty("description").stringValue = "P3D QA Requirements fixture.";
            serialized.FindProperty("requirementLevel").intValue = (int)level;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            return profile;
        }

        private static ActivityParticipationProjectionProfile CreateProjectionProfile(
            string fileName,
            ActivityParticipationProjectionMode mode,
            ActivityParticipationZeroParticipantPolicy zeroPolicy,
            params PlayerSlotProfile[] explicitSlots)
        {
            var profile = ScriptableObject.CreateInstance<ActivityParticipationProjectionProfile>();
            profile.name = fileName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = fileName;
            serialized.FindProperty("description").stringValue = "P3D QA Projection fixture.";
            serialized.FindProperty("projectionMode").intValue = (int)mode;
            serialized.FindProperty("zeroParticipantPolicy").intValue = (int)zeroPolicy;
            SerializedProperty slots = serialized.FindProperty("explicitSlotProfiles");
            int count = explicitSlots != null ? explicitSlots.Length : 0;
            slots.arraySize = count;
            for (int index = 0; index < count; index++)
            {
                slots.GetArrayElementAtIndex(index).objectReferenceValue = explicitSlots[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            return profile;
        }

        private static ActivityAsset CreateActivity(
            string fileName,
            string activityName,
            ActivityParticipationProjectionProfile projection,
            PlayerParticipationRequirementsProfile requirements)
        {
            var activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = fileName;
            var serialized = new SerializedObject(activity);
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("description").stringValue = "P3D QA Activity fixture.";
            serialized.FindProperty("playerParticipationProjectionProfile").objectReferenceValue = projection;
            serialized.FindProperty("playerParticipationRequirementsProfile").objectReferenceValue = requirements;
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

        private static object ValidateProjectionProfile(
            ActivityParticipationProjectionProfile profile)
        {
            return InvokeValidator(
                "ValidateProjectionProfile",
                new[] { typeof(ActivityParticipationProjectionProfile) },
                new object[] { profile });
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

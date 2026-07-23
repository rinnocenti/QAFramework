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
    /// P3C editor-only smoke for Player Slot and participation requirements authoring.
    /// Uses temporary QA assets and removes the complete fixture after execution.
    /// </summary>
    internal static class QaP3CPlayerProfileAuthoringSmoke
    {
        private const string TempFolder =
            "Assets/ImmersiveFrameworkQA/__P3C_PlayerProfileAuthoring_Temp";

        private const string CompleteTemplateMenuPath =
            "Assets/Create/Immersive Framework/Player/Templates/Complete Local Player Profile Set";

        private const string ValidatorTypeName =
            "Immersive.Framework.Editor.Editor.PlayerParticipation.PlayerParticipationAuthoringValidator";

        [MenuItem("Immersive Framework/QA/Regressions/Player/Run Player Participation Authoring Regression")]
        internal static void Run()
        {
            var completed = new List<string>();

            try
            {
                PrepareTempFolder();
                RunSmoke(completed);

                Debug.Log(
                    "[P3C_PLAYER_PROFILE_AUTHORING_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3C_PLAYER_PROFILE_AUTHORING_SMOKE] status='Failed' " +
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
            PlayerSlotProfile playerOne = CreateSlotProfile(
                "PlayerSlot_QA_One",
                "  qa.p3c.player.1  ",
                "QA Player 1",
                0);
            PlayerSlotProfile playerTwo = CreateSlotProfile(
                "PlayerSlot_QA_Two",
                "qa.p3c.player.2",
                "QA Player 2",
                1);
            PlayerParticipationRequirementsProfile explicitNone = CreateRequirementsProfile(
                "PlayerParticipation_QA_None",
                "QA Participation — None",
                PlayerParticipationRequirementLevel.None);
            GameApplicationAsset gameApplication = CreateGameApplication(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                playerOne,
                playerTwo);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssertEqual(
                "qa.p3c.player.1",
                playerOne.PlayerSlotIdText,
                "PlayerSlotProfile did not normalize identity text.");
            AssertTrue(
                playerOne.TryGetPlayerSlotId(out _, out string playerOneIssue),
                $"Normalized PlayerSlotId did not resolve. issue='{playerOneIssue}'.");
            completed.Add("profile-identity-normalized");

            AssertEqual(2, gameApplication.LocalPlayerSlotCount, "Configured Slot count is incorrect.");
            AssertSame(playerOne, gameApplication.LocalPlayerSlots[0], "Configured Slot order index 0 changed.");
            AssertSame(playerTwo, gameApplication.LocalPlayerSlots[1], "Configured Slot order index 1 changed.");
            AssertTrue(
                gameApplication.TryGetLocalPlayerSlot(0, out PlayerSlotProfile first) && first == playerOne,
                "TryGetLocalPlayerSlot did not preserve configured order.");
            completed.Add("ordered-game-application-configuration");

            AssertEqual(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                gameApplication.PlayerActorSelectionDuplicatePolicy,
                "Player Actor Selection Policy duplicate rule changed.");
            completed.Add("actor-selection-policy-configured");

            object validReport = ValidateGameApplication(gameApplication);
            AssertReportHasNoErrors(
                validReport,
                "Valid Game Application configuration reported errors.");
            completed.Add("valid-configuration-accepted");

            ConfigureActorSelectionPolicy(
                gameApplication,
                PlayerActorSelectionDuplicatePolicy.Unspecified);
            object missingActorSelectionPolicyReport =
                ValidateGameApplication(gameApplication);
            AssertTrue(
                ReadInt(missingActorSelectionPolicyReport, "ErrorCount") > 0,
                "Unspecified Player Actor Selection Policy was accepted.");
            AssertReportContains(
                missingActorSelectionPolicyReport,
                "duplicate-selection policy");
            completed.Add("missing-actor-selection-policy-rejected");

            ConfigureActorSelectionPolicy(
                gameApplication,
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates);
            object restoredValidReport = ValidateGameApplication(gameApplication);
            AssertReportHasNoErrors(
                restoredValidReport,
                "Restored Game Application configuration reported errors.");

            AssertTrue(explicitNone.IsExplicitNone, "None requirements Profile is not explicit None.");
            AssertTrue(
                explicitNone.HasDefinedRequirementLevel,
                "None requirements Profile has an undefined Requirement Level.");
            object requirementsReport = ValidateRequirementsProfile(explicitNone);
            AssertReportHasNoErrors(
                requirementsReport,
                "Explicit None Requirements Profile reported errors.");
            completed.Add("explicit-none-requirements-profile");

            string playerOneBefore = EditorJsonUtility.ToJson(playerOne);
            string playerTwoBefore = EditorJsonUtility.ToJson(playerTwo);
            string requirementsBefore = EditorJsonUtility.ToJson(explicitNone);
            string applicationBefore = EditorJsonUtility.ToJson(gameApplication);

            ValidateGameApplication(gameApplication);
            ValidateRequirementsProfile(explicitNone);
            ValidateProjectProfiles(gameApplication.ValidationMode);

            AssertEqual(playerOneBefore, EditorJsonUtility.ToJson(playerOne), "Validation mutated Player 1 Profile.");
            AssertEqual(playerTwoBefore, EditorJsonUtility.ToJson(playerTwo), "Validation mutated Player 2 Profile.");
            AssertEqual(requirementsBefore, EditorJsonUtility.ToJson(explicitNone), "Validation mutated Requirements Profile.");
            AssertEqual(applicationBefore, EditorJsonUtility.ToJson(gameApplication), "Validation mutated Game Application.");
            completed.Add("validation-is-non-mutating");

            ConfigureLocalPlayerSlots(gameApplication, playerOne, null);
            object missingReferenceReport = ValidateGameApplication(gameApplication);
            AssertTrue(ReadInt(missingReferenceReport, "ErrorCount") > 0, "Missing Slot reference was accepted.");
            AssertReportContains(missingReferenceReport, "Local Player Slots[1] is missing");
            completed.Add("missing-profile-reference-rejected");

            ConfigureLocalPlayerSlots(gameApplication, playerOne, playerOne);
            object repeatedProfileReport = ValidateGameApplication(gameApplication);
            AssertTrue(ReadInt(repeatedProfileReport, "ErrorCount") > 0, "Repeated Profile reference was accepted.");
            AssertReportContains(repeatedProfileReport, "repeats PlayerSlotProfile");
            completed.Add("repeated-profile-reference-rejected");

            PlayerSlotProfile duplicateIdentity = CreateSlotProfile(
                "PlayerSlot_QA_Duplicate",
                "qa.p3c.player.1",
                "QA Duplicate Player",
                2);
            ConfigureLocalPlayerSlots(gameApplication, playerOne, duplicateIdentity);
            object duplicateIdentityReport = ValidateGameApplication(gameApplication);
            AssertTrue(ReadInt(duplicateIdentityReport, "ErrorCount") > 0, "Duplicate PlayerSlotId was accepted.");
            AssertReportContains(duplicateIdentityReport, "duplicates PlayerSlotId");

            object projectReport = ValidateProjectProfiles(gameApplication.ValidationMode);
            AssertTrue(ReadInt(projectReport, "ErrorCount") > 0, "Project duplicate scan accepted duplicate identity.");
            AssertReportContains(projectReport, "qa.p3c.player.1");
            completed.Add("duplicate-identity-rejected");

            ConfigureSlotProfile(duplicateIdentity, string.Empty, "QA Invalid Player", 2);
            object invalidIdentityReport = ValidateGameApplication(gameApplication);
            AssertTrue(ReadInt(invalidIdentityReport, "ErrorCount") > 0, "Empty PlayerSlotId was accepted.");
            AssertReportContains(invalidIdentityReport, "requires a non-empty PlayerSlotId");
            completed.Add("empty-identity-rejected");

            RunCompleteTemplateCase(completed);
        }

        private static void RunCompleteTemplateCase(List<string> completed)
        {
            ConfigureSelectionForTemplateCreation();
            bool executed = EditorApplication.ExecuteMenuItem(CompleteTemplateMenuPath);
            AssertTrue(executed, $"Template menu command was not found: '{CompleteTemplateMenuPath}'.");

            string templateFolder = $"{TempFolder}/PlayerParticipation";
            AssertTrue(AssetDatabase.IsValidFolder(templateFolder), "Complete template command did not create its folder.");

            string[] slotGuids = AssetDatabase.FindAssets("t:PlayerSlotProfile", new[] { templateFolder });
            string[] requirementsGuids = AssetDatabase.FindAssets(
                "t:PlayerParticipationRequirementsProfile",
                new[] { templateFolder });
            string[] projectionGuids = AssetDatabase.FindAssets(
                "t:ActivityParticipationProjectionProfile",
                new[] { templateFolder });

            AssertEqual(4, slotGuids.Length, "Complete template did not create four Player Slot Profiles.");
            AssertEqual(5, requirementsGuids.Length, "Complete template did not create five Requirements Profiles.");
            AssertEqual(
                3,
                projectionGuids.Length,
                "Complete template did not create three Activity Participation Projection Profiles.");
            completed.Add("complete-template-projections-created");

            string[] actorSelectionPolicyGuids = AssetDatabase.FindAssets(
                "ActorSelection",
                new[] { templateFolder });
            AssertEqual(
                0,
                actorSelectionPolicyGuids.Length,
                "Complete template created an obsolete Actor Selection Policy asset.");
            completed.Add("complete-template-uses-game-application-actor-policy");

            bool foundExplicitNone = false;
            for (int index = 0; index < requirementsGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(requirementsGuids[index]);
                PlayerParticipationRequirementsProfile profile =
                    AssetDatabase.LoadAssetAtPath<PlayerParticipationRequirementsProfile>(path);
                if (profile != null && profile.IsExplicitNone)
                {
                    foundExplicitNone = true;
                    break;
                }
            }

            AssertTrue(foundExplicitNone, "Complete template set did not create an explicit None Profile.");
            completed.Add("complete-template-set-created");
        }

        private static PlayerSlotProfile CreateSlotProfile(
            string fileName,
            string playerSlotId,
            string displayName,
            int displayOrder)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = fileName;
            ConfigureSlotProfile(profile, playerSlotId, displayName, displayOrder);
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            return profile;
        }

        private static void ConfigureSlotProfile(
            PlayerSlotProfile profile,
            string playerSlotId,
            string displayName,
            int displayOrder)
        {
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("playerSlotId").stringValue = playerSlotId;
            serializedProfile.FindProperty("displayName").stringValue = displayName;
            serializedProfile.FindProperty("description").stringValue = "P3C QA authoring fixture.";
            serializedProfile.FindProperty("displayOrder").intValue = displayOrder;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            if (EditorUtility.IsPersistent(profile))
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static PlayerParticipationRequirementsProfile CreateRequirementsProfile(
            string fileName,
            string displayName,
            PlayerParticipationRequirementLevel requirementLevel)
        {
            var profile = ScriptableObject.CreateInstance<PlayerParticipationRequirementsProfile>();
            profile.name = fileName;

            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("displayName").stringValue = displayName;
            serializedProfile.FindProperty("description").stringValue = "P3C QA explicit requirements fixture.";
            serializedProfile.FindProperty("requirementLevel").intValue = (int)requirementLevel;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            return profile;
        }

        private static GameApplicationAsset CreateGameApplication(
            PlayerActorSelectionDuplicatePolicy actorSelectionPolicy,
            params PlayerSlotProfile[] profiles)
        {
            var gameApplication = ScriptableObject.CreateInstance<GameApplicationAsset>();
            gameApplication.name = "QA P3C Game Application";
            AssetDatabase.CreateAsset(gameApplication, $"{TempFolder}/GameApplication_QA_P3C.asset");
            ConfigureActorSelectionPolicy(
                gameApplication,
                actorSelectionPolicy);
            ConfigureLocalPlayerSlots(gameApplication, profiles);
            return gameApplication;
        }

        private static void ConfigureActorSelectionPolicy(
            GameApplicationAsset gameApplication,
            PlayerActorSelectionDuplicatePolicy actorSelectionPolicy)
        {
            var serializedApplication = new SerializedObject(gameApplication);
            SerializedProperty property = serializedApplication.FindProperty(
                "playerActorSelectionDuplicatePolicy");
            AssertNotNull(
                property,
                "GameApplicationAsset.playerActorSelectionDuplicatePolicy serialized field was not found.");
            property.intValue = (int)actorSelectionPolicy;
            serializedApplication.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameApplication);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureLocalPlayerSlots(
            GameApplicationAsset gameApplication,
            params PlayerSlotProfile[] profiles)
        {
            var serializedApplication = new SerializedObject(gameApplication);
            SerializedProperty slots = serializedApplication.FindProperty("localPlayerSlots");
            AssertNotNull(slots, "GameApplicationAsset.localPlayerSlots serialized field was not found.");

            int count = profiles != null ? profiles.Length : 0;
            slots.arraySize = count;
            for (int index = 0; index < count; index++)
            {
                slots.GetArrayElementAtIndex(index).objectReferenceValue = profiles[index];
            }

            serializedApplication.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(gameApplication);
            AssetDatabase.SaveAssets();
        }

        private static object ValidateGameApplication(GameApplicationAsset gameApplication)
        {
            return InvokeValidator(
                "ValidateGameApplication",
                new[] { typeof(GameApplicationAsset), typeof(bool) },
                new object[] { gameApplication, true });
        }

        private static object ValidateRequirementsProfile(
            PlayerParticipationRequirementsProfile profile)
        {
            return InvokeValidator(
                "ValidateRequirementsProfile",
                new[] { typeof(PlayerParticipationRequirementsProfile) },
                new object[] { profile });
        }

        private static object ValidateProjectProfiles(FrameworkValidationMode validationMode)
        {
            return InvokeValidator(
                "ValidateProjectProfiles",
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
            AssertNotNull(
                property,
                $"Property '{propertyName}' was not found on '{target.GetType().FullName}'.");
            return property.GetValue(target);
        }

        private static void AssertReportContains(object report, string expected)
        {
            object issuesObject = ReadProperty(report, "Issues");
            AssertTrue(issuesObject is IEnumerable, "Validation report Issues is not enumerable.");

            foreach (object issue in (IEnumerable)issuesObject)
            {
                string message = ReadProperty(issue, "Message") as string;
                if (!string.IsNullOrEmpty(message) &&
                    message.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Validation report did not contain expected text '{expected}'. " +
                $"report='{DescribeReport(report)}'.");
        }

        private static void AssertReportHasNoErrors(
            object report,
            string message)
        {
            int errorCount = ReadInt(report, "ErrorCount");
            if (errorCount > 0)
            {
                throw new InvalidOperationException(
                    $"{message} errors='{errorCount}' " +
                    $"report='{DescribeReport(report)}'.");
            }
        }

        private static string DescribeReport(object report)
        {
            object issuesObject = ReadProperty(report, "Issues");
            AssertTrue(
                issuesObject is IEnumerable,
                "Validation report Issues is not enumerable.");

            var messages = new List<string>();
            foreach (object issue in (IEnumerable)issuesObject)
            {
                string message = ReadProperty(issue, "Message") as string;
                messages.Add(message ?? string.Empty);
            }

            return messages.Count == 0
                ? "<no issues>"
                : string.Join(" | ", messages);
        }

        private static void PrepareTempFolder()
        {
            CleanupTempFolder();
            string guid = AssetDatabase.CreateFolder(
                "Assets/ImmersiveFrameworkQA",
                "__P3C_PlayerProfileAuthoring_Temp");
            string createdPath = AssetDatabase.GUIDToAssetPath(guid);
            AssertEqual(TempFolder, createdPath, "Could not create deterministic P3C QA temp folder.");
            AssetDatabase.Refresh();
        }

        private static void ConfigureSelectionForTemplateCreation()
        {
            DefaultAsset folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(TempFolder);
            AssertNotNull(folder, "P3C QA temp folder could not be selected for template creation.");
            Selection.activeObject = folder;
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

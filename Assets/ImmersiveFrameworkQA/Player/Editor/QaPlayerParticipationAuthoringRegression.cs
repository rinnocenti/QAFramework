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
    /// Regressão unificada de autoria de participação de player.
    /// Valida perfis de slot, políticas de seleção de ator e projeções de participação de atividade.
    /// </summary>
    internal static class QaPlayerParticipationAuthoringRegression
    {
        private const string TempFolder = "Assets/ImmersiveFrameworkQA/__PlayerParticipationAuthoring_Temp";
        private const string SlotValidatorTypeName = "Immersive.Framework.Editor.Editor.PlayerParticipation.PlayerParticipationAuthoringValidator";
        private const string ProjectionValidatorTypeName = "Immersive.Framework.Editor.Editor.PlayerParticipation.ActivityParticipationProjectionAuthoringValidator";

        [MenuItem("Immersive Framework/QA/Regressions/Player/Run Player Participation Authoring Regression")]
        internal static void Run()
        {
            var completed = new List<string>();
            try
            {
                PrepareTempFolder();
                RunSlotProfileAuthoringCases(completed);
                RunActivityProjectionAuthoringCases(completed);

                Debug.Log(
                    "[PLAYER_PARTICIPATION_AUTHORING_REGRESSION] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[PLAYER_PARTICIPATION_AUTHORING_REGRESSION] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                CleanupTempFolder();
            }
        }

        private static void RunSlotProfileAuthoringCases(List<string> completed)
        {
            PlayerSlotProfile playerOne = CreateSlotProfile("PlayerSlot_QA_One", "  qa.p3c.player.1  ", "QA Player 1", 0);
            PlayerSlotProfile playerTwo = CreateSlotProfile("PlayerSlot_QA_Two", "qa.p3c.player.2", "QA Player 2", 1);
            GameApplicationAsset gameApplication = CreateGameApplication(
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                playerOne,
                playerTwo);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssertEqual("qa.p3c.player.1", playerOne.PlayerSlotIdText, "PlayerSlotProfile did not normalize identity text.");
            AssertTrue(playerOne.TryGetPlayerSlotId(out _, out string playerOneIssue), $"Normalized PlayerSlotId did not resolve: '{playerOneIssue}'.");
            completed.Add("profile-identity-normalized");

            AssertEqual(2, gameApplication.LocalPlayerSlotCount, "Configured Slot count is incorrect.");
            AssertSame(playerOne, gameApplication.LocalPlayerSlots[0], "Configured Slot order index 0 changed.");
            AssertSame(playerTwo, gameApplication.LocalPlayerSlots[1], "Configured Slot order index 1 changed.");
            completed.Add("ordered-game-application-configuration");

            object validReport = ValidateGameApplication(gameApplication);
            AssertReportHasNoErrors(validReport, "Valid Game Application configuration reported errors.");
            completed.Add("valid-configuration-accepted");

            string playerOneBefore = EditorJsonUtility.ToJson(playerOne);
            string applicationBefore = EditorJsonUtility.ToJson(gameApplication);
            ValidateGameApplication(gameApplication);
            AssertEqual(playerOneBefore, EditorJsonUtility.ToJson(playerOne), "Validation mutated Player 1 Profile.");
            AssertEqual(applicationBefore, EditorJsonUtility.ToJson(gameApplication), "Validation mutated Game Application.");
            completed.Add("slot-validation-is-non-mutating");
        }

        private static void RunActivityProjectionAuthoringCases(List<string> completed)
        {
            PlayerSlotProfile playerOne = CreateSlotProfile("P3D_Player_One", "qa.p3d.player.1", "P3D Player 1", 0);
            PlayerSlotProfile playerTwo = CreateSlotProfile("P3D_Player_Two", "qa.p3d.player.2", "P3D Player 2", 1);
            PlayerParticipationRequirementLevel none = PlayerParticipationRequirementLevel.None;
            PlayerParticipationRequirementLevel joined = PlayerParticipationRequirementLevel.JoinedSlots;

            ActivityAsset noPlayersActivity = CreateActivity(
                "P3D_Activity_NoPlayers",
                "QA P3D No Players",
                none,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            AssertReportHasNoErrors(ValidateActivity(noPlayersActivity), "NoSlots + None was rejected.");
            completed.Add("no-slots-none-valid");

            ActivityAsset allJoinedActivity = CreateActivity(
                "P3D_Activity_AllJoined",
                "QA P3D All Joined",
                joined,
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            AssertReportHasNoErrors(ValidateActivity(allJoinedActivity), "AllJoinedSlots + JoinedSlots was rejected.");
            completed.Add("all-joined-zero-allowed-valid");

            ActivityAsset explicitActivity = CreateActivity(
                "P3D_Activity_Explicit",
                "QA P3D Explicit",
                joined,
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected,
                playerOne,
                playerTwo);
            AssertTrue(
                explicitActivity.TryGetPlayerParticipationProjectionDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out string descriptorIssue),
                $"Explicit descriptor failed: '{descriptorIssue}'.");
            AssertEqual(2, descriptor.ExplicitSlotProfiles.Count, "Explicit descriptor Slot count changed.");
            completed.Add("activity-owned-explicit-slots-order-preserved");
        }

        private static PlayerSlotProfile CreateSlotProfile(string fileName, string playerSlotId, string displayName, int displayOrder)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = fileName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = playerSlotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue = "QA authoring fixture.";
            serialized.FindProperty("displayOrder").intValue = displayOrder;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            return profile;
        }

        private static GameApplicationAsset CreateGameApplication(
            PlayerActorSelectionDuplicatePolicy actorSelectionPolicy,
            params PlayerSlotProfile[] profiles)
        {
            var gameApplication = ScriptableObject.CreateInstance<GameApplicationAsset>();
            gameApplication.name = "QA Game Application";
            AssetDatabase.CreateAsset(gameApplication, $"{TempFolder}/GameApplication_QA.asset");
            var serialized = new SerializedObject(gameApplication);
            serialized.FindProperty("playerActorSelectionDuplicatePolicy").intValue = (int)actorSelectionPolicy;
            SerializedProperty slots = serialized.FindProperty("localPlayerSlots");
            slots.arraySize = profiles != null ? profiles.Length : 0;
            for (int i = 0; i < slots.arraySize; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gameApplication;
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
            serialized.FindProperty("playerParticipationProjectionMode").intValue = (int)projectionMode;
            serialized.FindProperty("playerParticipationZeroParticipantPolicy").intValue = (int)zeroPolicy;
            serialized.FindProperty("playerParticipationRequirementLevel").intValue = (int)requirementLevel;
            SerializedProperty slots = serialized.FindProperty("playerParticipationExplicitSlotProfiles");
            slots.arraySize = explicitSlots != null ? explicitSlots.Length : 0;
            for (int i = 0; i < slots.arraySize; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = explicitSlots[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(activity, $"{TempFolder}/{fileName}.asset");
            return activity;
        }

        private static object ValidateGameApplication(GameApplicationAsset gameApplication)
        {
            return InvokeValidator(
                SlotValidatorTypeName,
                "ValidateGameApplication",
                new[] { typeof(GameApplicationAsset), typeof(bool) },
                new object[] { gameApplication, true });
        }

        private static object ValidateActivity(ActivityAsset activity)
        {
            return InvokeValidator(
                ProjectionValidatorTypeName,
                "ValidateActivity",
                new[] { typeof(ActivityAsset) },
                new object[] { activity });
        }

        private static object InvokeValidator(string validatorTypeName, string methodName, Type[] parameterTypes, object[] arguments)
        {
            Type validatorType = ResolveType(validatorTypeName);
            AssertNotNull(validatorType, $"Validator type not found: '{validatorTypeName}'.");
            MethodInfo method = validatorType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            AssertNotNull(method, $"Validator method not found: '{methodName}'.");
            return method.Invoke(null, arguments);
        }

        private static Type ResolveType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName, false);
                if (type != null) return type;
            }
            return null;
        }

        private static void AssertReportHasNoErrors(object report, string message)
        {
            PropertyInfo property = report.GetType().GetProperty("ErrorCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(property, "Property 'ErrorCount' not found.");
            int errorCount = (int)property.GetValue(report);
            if (errorCount > 0) throw new InvalidOperationException($"{message} errorCount='{errorCount}'.");
        }

        private static void PrepareTempFolder()
        {
            CleanupTempFolder();
            AssetDatabase.CreateFolder("Assets/ImmersiveFrameworkQA", "__PlayerParticipationAuthoring_Temp");
            AssetDatabase.Refresh();
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
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null) throw new InvalidOperationException(message);
        }

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual)) throw new InvalidOperationException(message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

using System;
using System.Collections.Generic;
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

        [MenuItem("Immersive Framework/QA/Player/Session/Run Authoring Contract")]
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
            PlayerSessionProfile sessionProfile = CreatePlayerSessionProfile(
                playerOne,
                playerTwo);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AssertEqual("qa.p3c.player.1", playerOne.PlayerSlotIdText, "PlayerSlotProfile did not normalize identity text.");
            AssertTrue(playerOne.TryGetPlayerSlotId(out _, out string playerOneIssue), $"Normalized PlayerSlotId did not resolve: '{playerOneIssue}'.");
            completed.Add("profile-identity-normalized");

            AssertEqual(2, sessionProfile.SupportedSlotCount, "Supported Slot count is incorrect.");
            AssertSame(playerOne, sessionProfile.SupportedSlots[0], "Supported Slot order index 0 changed.");
            AssertSame(playerTwo, sessionProfile.SupportedSlots[1], "Supported Slot order index 1 changed.");
            completed.Add("ordered-player-session-configuration");

            AssertTrue(sessionProfile.TryValidate(out string profileIssue),
                $"Valid Player Session Profile reported an error: '{profileIssue}'.");
            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(sessionProfile);
            AssertTrue(resolution.Succeeded,
                $"Valid Player Session Profile did not resolve: '{resolution.Message}'.");
            completed.Add("valid-player-session-configuration-accepted");

            string playerOneBefore = EditorJsonUtility.ToJson(playerOne);
            string profileBefore = EditorJsonUtility.ToJson(sessionProfile);
            sessionProfile.TryValidate(out _);
            PlayerSessionConfigurationResolver.Resolve(sessionProfile);
            AssertEqual(playerOneBefore, EditorJsonUtility.ToJson(playerOne), "Validation mutated Player 1 Profile.");
            AssertEqual(profileBefore, EditorJsonUtility.ToJson(sessionProfile), "Validation mutated Player Session Profile.");
            completed.Add("player-session-validation-is-non-mutating");
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
            AssertProjectionDescriptor(
                noPlayersActivity,
                ActivityParticipationProjectionMode.NoSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                0,
                "NoSlots + None authoring descriptor is invalid.");
            completed.Add("no-slots-none-valid");

            ActivityAsset allJoinedActivity = CreateActivity(
                "P3D_Activity_AllJoined",
                "QA P3D All Joined",
                joined,
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed);
            AssertProjectionDescriptor(
                allJoinedActivity,
                ActivityParticipationProjectionMode.AllJoinedSlots,
                ActivityParticipationZeroParticipantPolicy.Allowed,
                0,
                "AllJoinedSlots + JoinedSlots authoring descriptor is invalid.");
            completed.Add("all-joined-zero-allowed-valid");

            ActivityAsset explicitActivity = CreateActivity(
                "P3D_Activity_Explicit",
                "QA P3D Explicit",
                joined,
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected,
                playerOne,
                playerTwo);
            AssertProjectionDescriptor(
                explicitActivity,
                ActivityParticipationProjectionMode.ExplicitSlots,
                ActivityParticipationZeroParticipantPolicy.Rejected,
                2,
                "Explicit Slot authoring descriptor is invalid.");
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

        private static PlayerSessionProfile CreatePlayerSessionProfile(
            params PlayerSlotProfile[] profiles)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSessionProfile>();
            profile.name = "QA Player Session Profile";
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/PlayerSessionProfile_QA.asset");
            var serialized = new SerializedObject(profile);
            SerializedProperty slots = serialized.FindProperty("supportedSlots");
            slots.arraySize = profiles != null ? profiles.Length : 0;
            for (int i = 0; i < slots.arraySize; i++)
            {
                slots.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
            }
            serialized.FindProperty("initialJoiningOpen").boolValue = true;
            serialized.FindProperty("hostProvisioning").intValue =
                (int)PlayerHostProvisioningMode.ManagerProvisioned;
            serialized.FindProperty("actorResolutionPolicy").intValue =
                (int)PlayerActorResolutionPolicy.ResolveConfiguredDefault;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static void AssertProjectionDescriptor(
            ActivityAsset activity,
            ActivityParticipationProjectionMode expectedMode,
            ActivityParticipationZeroParticipantPolicy expectedZeroPolicy,
            int expectedExplicitSlotCount,
            string message)
        {
            AssertTrue(
                activity.TryGetPlayerParticipationProjectionDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out string issue),
                message + " issue='" + issue + "'.");
            AssertEqual(expectedMode, descriptor.Mode,
                message + " Projection mode changed.");
            AssertEqual(expectedZeroPolicy, descriptor.ZeroParticipantPolicy,
                message + " Zero-participant policy changed.");
            AssertEqual(expectedExplicitSlotCount, descriptor.ExplicitSlotProfiles.Count,
                message + " Explicit Slot count changed.");
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Identity;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3H2ActorSelectionAuthoringSmoke
    {
        private const string TempFolder =
            "Assets/ImmersiveFrameworkQA/__P3H2_ActorSelectionAuthoring_Temp";
        private const string ActorValidatorTypeName =
            "Immersive.Framework.Editor.Editor.PlayerParticipation.PlayerActorSelectionAuthoringValidator";
        private const string ParticipationValidatorTypeName =
            "Immersive.Framework.Editor.Editor.PlayerParticipation.PlayerParticipationAuthoringValidator";
        private const string PolicyTemplateMenuPath =
            "Assets/Create/Immersive Framework/Player/Templates/Actor Selection Policy Set";

        [MenuItem("Immersive Framework/QA/Player/P3H.2 Run Actor Selection Authoring Smoke")]
        private static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[P3H2_ACTOR_SELECTION_AUTHORING_SMOKE] status='Failed' message='Run outside Play Mode.'.");
                return;
            }

            var completed = new List<string>();
            try
            {
                PrepareTempFolder();
                RunCases(completed);
                Debug.Log(
                    $"[P3H2_ACTOR_SELECTION_AUTHORING_SMOKE] status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[P3H2_ACTOR_SELECTION_AUTHORING_SMOKE] status='Failed' exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' completed='{string.Join(",", completed)}'.\n{exception}");
                throw;
            }
            finally
            {
                CleanupTempFolder();
            }
        }

        private static void RunCases(List<string> completed)
        {
            GameObject logicalPlayerPrefab = CreatePlayerLogicalHostPrefab(
                "P3H2_PlayerLogicalActor.prefab",
                includePlayerInput: false);
            ActorProfile validProfile = CreateActorProfile(
                "ActorProfile_QA_Hero",
                "  actor-profile.qa.hero  ",
                "QA Hero",
                ActorKind.Player,
                ActorRole.Protagonist,
                logicalPlayerPrefab);

            AssertEqual("actor-profile.qa.hero", validProfile.ActorProfileIdText,
                "ActorProfileId text was not normalized.");
            completed.Add("actor-profile-id-normalized");

            ActorProfileId typedId = validProfile.ActorProfileId;
            AssertTrue(typedId.IsValid && typedId.Domain == FrameworkIdentityDomain.ActorProfile,
                "Typed ActorProfileId is invalid.");
            completed.Add("actor-profile-typed-identity");

            object validReport = ValidateActorProfile(validProfile, true);
            AssertEqual(0, ReadInt(validReport, "ErrorCount"),
                "Valid contextual Player ActorProfile was rejected.");
            AssertTrue(logicalPlayerPrefab.GetComponentInChildren<PlayerInput>(true) == null,
                "Valid Logical Actor Host contains PlayerInput.");
            completed.Add("valid-contextual-player-actor-profile");

            ActorProfile emptyIdentityProfile = CreateActorProfile(
                "ActorProfile_QA_EmptyIdentity",
                string.Empty,
                "QA Empty Identity",
                ActorKind.Player,
                ActorRole.Protagonist,
                logicalPlayerPrefab);
            object emptyIdentityReport = ValidateActorProfile(emptyIdentityProfile, false);
            AssertTrue(ReadInt(emptyIdentityReport, "ErrorCount") > 0,
                "Empty ActorProfileId was accepted.");
            completed.Add("empty-actor-profile-id-rejected");

            ActorProfile duplicateProfile = CreateActorProfile(
                "ActorProfile_QA_Hero_Duplicate",
                "actor-profile.qa.hero",
                "QA Hero Duplicate",
                ActorKind.Player,
                ActorRole.Protagonist,
                logicalPlayerPrefab);
            object duplicateReport = ValidateActorProfile(duplicateProfile, true);
            AssertTrue(ReadInt(duplicateReport, "ErrorCount") > 0,
                "Duplicate ActorProfileId was accepted.");
            completed.Add("duplicate-actor-profile-id-rejected");

            ActorProfile missingHostProfile = CreateActorProfile(
                "ActorProfile_QA_MissingHost",
                "actor-profile.qa.missing-host",
                "QA Missing Host",
                ActorKind.Player,
                ActorRole.Protagonist,
                null);
            object missingHostReport = ValidateActorProfile(missingHostProfile, false);
            AssertTrue(ReadInt(missingHostReport, "ErrorCount") > 0,
                "Missing Logical Actor Host was accepted.");
            completed.Add("missing-logical-host-rejected");

            ActorProfile unknownKindProfile = CreateActorProfile(
                "ActorProfile_QA_UnknownKind",
                "actor-profile.qa.unknown-kind",
                "QA Unknown Kind",
                ActorKind.Unknown,
                ActorRole.Protagonist,
                logicalPlayerPrefab);
            object unknownKindReport = ValidateActorProfile(unknownKindProfile, false);
            AssertTrue(ReadInt(unknownKindReport, "ErrorCount") > 0,
                "Unknown Actor Kind was accepted.");
            completed.Add("unknown-actor-kind-rejected");

            ActorProfile mismatchedHostProfile = CreateActorProfile(
                "ActorProfile_QA_MismatchedHost",
                "actor-profile.qa.mismatched-host",
                "QA Mismatched Host",
                ActorKind.NonPlayer,
                ActorRole.Protagonist,
                logicalPlayerPrefab);
            object mismatchedHostReport = ValidateActorProfile(mismatchedHostProfile, false);
            AssertTrue(ReadInt(mismatchedHostReport, "ErrorCount") > 0,
                "Mismatched host declaration was accepted.");
            completed.Add("mismatched-host-kind-rejected");

            GameObject invalidPlayerInputHost = CreatePlayerLogicalHostPrefab(
                "P3H2_InvalidPlayerInputLogicalActor.prefab",
                includePlayerInput: true);
            ActorProfile invalidPlayerInputProfile = CreateActorProfile(
                "ActorProfile_QA_PlayerInputHost",
                "actor-profile.qa.player-input-host",
                "QA Invalid PlayerInput Host",
                ActorKind.Player,
                ActorRole.Protagonist,
                invalidPlayerInputHost);
            object invalidPlayerInputReport = ValidateActorProfile(
                invalidPlayerInputProfile,
                false);
            AssertTrue(ReadInt(invalidPlayerInputReport, "ErrorCount") > 0,
                "Logical Player Actor Host with PlayerInput was accepted.");
            AssertReportContains(invalidPlayerInputReport, "must not contain PlayerInput");
            completed.Add("logical-player-host-playerinput-rejected");

            PlayerActorSelectionPolicyProfile allowPolicy = CreatePolicyProfile(
                "ActorSelection_QA_AllowDuplicates",
                "QA Allow Duplicates",
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates);
            AssertEqual(0, ReadInt(ValidateSelectionPolicy(allowPolicy), "ErrorCount"),
                "AllowDuplicates policy was rejected.");
            completed.Add("allow-duplicates-policy-valid");

            PlayerActorSelectionPolicyProfile uniquePolicy = CreatePolicyProfile(
                "ActorSelection_QA_Unique",
                "QA Unique Across Joined Slots",
                PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots);
            AssertEqual(0, ReadInt(ValidateSelectionPolicy(uniquePolicy), "ErrorCount"),
                "Unique policy was rejected.");
            completed.Add("unique-policy-valid");

            PlayerActorSelectionPolicyProfile invalidPolicy = CreatePolicyProfile(
                "ActorSelection_QA_Unspecified",
                "QA Unspecified",
                PlayerActorSelectionDuplicatePolicy.Unspecified);
            AssertTrue(ReadInt(ValidateSelectionPolicy(invalidPolicy), "ErrorCount") > 0,
                "Unspecified policy was accepted.");
            completed.Add("unspecified-policy-rejected");

            PlayerSlotProfile optionalDefaultSlot = CreateSlotProfile(
                "PlayerSlot_QA_NoDefault",
                "qa.p3h2.slot.no-default",
                null);
            AssertEqual(0, ReadInt(ValidatePlayerSlotProfile(optionalDefaultSlot), "ErrorCount"),
                "Slot without default Actor was rejected.");
            completed.Add("slot-default-is-optional");

            PlayerSlotProfile validDefaultSlot = CreateSlotProfile(
                "PlayerSlot_QA_DefaultHero",
                "qa.p3h2.slot.default-hero",
                validProfile);
            AssertEqual(0, ReadInt(ValidatePlayerSlotProfile(validDefaultSlot), "ErrorCount"),
                "Valid Slot default Actor was rejected.");
            completed.Add("slot-default-reference-valid");

            PlayerSlotProfile invalidDefaultSlot = CreateSlotProfile(
                "PlayerSlot_QA_InvalidDefault",
                "qa.p3h2.slot.invalid-default",
                missingHostProfile);
            AssertTrue(ReadInt(ValidatePlayerSlotProfile(invalidDefaultSlot), "ErrorCount") > 0,
                "Invalid Slot default Actor was accepted.");
            completed.Add("invalid-slot-default-rejected");

            object projectReport = ValidateProjectActorSelectionProfiles();
            AssertTrue(ReadInt(projectReport, "ErrorCount") > 0,
                "Project duplicate scan accepted invalid Profiles.");
            completed.Add("project-duplicate-scan-rejected");

            string idBefore = validProfile.ActorProfileIdText;
            GameObject hostBefore = validProfile.LogicalActorHostPrefab;
            ValidateActorProfile(validProfile, true);
            AssertEqual(idBefore, validProfile.ActorProfileIdText,
                "Validation mutated ActorProfileId.");
            AssertSame(hostBefore, validProfile.LogicalActorHostPrefab,
                "Validation mutated Logical Actor Host.");
            AssertRuntimeImmutableProfile(typeof(ActorProfile));
            AssertRuntimeImmutableProfile(typeof(PlayerActorSelectionPolicyProfile));
            completed.Add("validation-and-profiles-are-immutable");

            RunPolicyTemplateCase();
            completed.Add("policy-template-set-created");
        }

        private static GameObject CreatePlayerLogicalHostPrefab(
            string fileName,
            bool includePlayerInput)
        {
            string prefabPath = TempFolder + "/" + fileName;
            var root = new GameObject("P3H2 Player Logical Actor Host");
            try
            {
                PlayerInput playerInput = includePlayerInput
                    ? root.AddComponent<PlayerInput>()
                    : null;
                PlayerActorDeclaration declaration =
                    root.AddComponent<PlayerActorDeclaration>();
                var serializedDeclaration = new SerializedObject(declaration);
                serializedDeclaration.FindProperty("actorId").stringValue =
                    "qa.p3h2.actor.host";
                serializedDeclaration.FindProperty("displayName").stringValue =
                    "QA P3H2 Actor Host";
                SerializedProperty playerInputProperty =
                    serializedDeclaration.FindProperty("playerInput");
                if (playerInputProperty != null)
                {
                    playerInputProperty.objectReferenceValue = playerInput;
                }
                serializedDeclaration.FindProperty("reason").stringValue =
                    "qa.p3h2.authoring";
                serializedDeclaration.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssertNotNull(prefab, "Could not create P3H.2 Logical Actor Host prefab.");
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static ActorProfile CreateActorProfile(
            string fileName,
            string actorProfileId,
            string displayName,
            ActorKind actorKind,
            ActorRole actorRole,
            GameObject logicalHost)
        {
            var profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = fileName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue = actorProfileId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue =
                "P3H.2 QA Actor Profile fixture.";
            serialized.FindProperty("actorKind").intValue = (int)actorKind;
            serialized.FindProperty("actorRole").intValue = (int)actorRole;
            serialized.FindProperty("logicalActorHostPrefab").objectReferenceValue = logicalHost;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static PlayerActorSelectionPolicyProfile CreatePolicyProfile(
            string fileName,
            string displayName,
            PlayerActorSelectionDuplicatePolicy duplicatePolicy)
        {
            var profile = ScriptableObject.CreateInstance<PlayerActorSelectionPolicyProfile>();
            profile.name = fileName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue =
                "P3H.2 QA selection policy fixture.";
            serialized.FindProperty("duplicatePolicy").intValue = (int)duplicatePolicy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static PlayerSlotProfile CreateSlotProfile(
            string fileName,
            string playerSlotId,
            ActorProfile defaultActorProfile)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = fileName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = playerSlotId;
            serialized.FindProperty("displayName").stringValue = fileName;
            serialized.FindProperty("description").stringValue =
                "P3H.2 QA Player Slot fixture.";
            serialized.FindProperty("defaultActorProfile").objectReferenceValue =
                defaultActorProfile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void RunPolicyTemplateCase()
        {
            DefaultAsset folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(TempFolder);
            AssertNotNull(folder, "P3H.2 temp folder could not be selected.");
            Selection.activeObject = folder;
            AssertTrue(EditorApplication.ExecuteMenuItem(PolicyTemplateMenuPath),
                $"Template menu command not found: '{PolicyTemplateMenuPath}'.");
            string templateFolder = $"{TempFolder}/PlayerParticipation";
            string[] policyGuids = AssetDatabase.FindAssets(
                "t:PlayerActorSelectionPolicyProfile",
                new[] { templateFolder });
            AssertEqual(2, policyGuids.Length,
                "Policy template set did not create two Profiles.");
        }

        private static void AssertRuntimeImmutableProfile(Type type)
        {
            foreach (PropertyInfo property in type.GetProperties(
                         BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                AssertTrue(property.SetMethod == null,
                    $"Profile '{type.FullName}' exposes writable property '{property.Name}'.");
            }
        }

        private static object ValidateActorProfile(ActorProfile profile, bool duplicateScan) =>
            InvokeValidator(
                ActorValidatorTypeName,
                "ValidateActorProfile",
                new[] { typeof(ActorProfile), typeof(bool) },
                new object[] { profile, duplicateScan });

        private static object ValidateSelectionPolicy(PlayerActorSelectionPolicyProfile profile) =>
            InvokeValidator(
                ActorValidatorTypeName,
                "ValidateSelectionPolicyProfile",
                new[] { typeof(PlayerActorSelectionPolicyProfile) },
                new object[] { profile });

        private static object ValidateProjectActorSelectionProfiles() =>
            InvokeValidator(
                ActorValidatorTypeName,
                "ValidateProjectActorSelectionProfiles",
                new[] { typeof(FrameworkValidationMode) },
                new object[] { FrameworkValidationMode.Standard });

        private static object ValidatePlayerSlotProfile(PlayerSlotProfile profile) =>
            InvokeValidator(
                ParticipationValidatorTypeName,
                "ValidatePlayerSlotProfile",
                new[] { typeof(PlayerSlotProfile), typeof(bool) },
                new object[] { profile, false });

        private static object InvokeValidator(
            string typeName,
            string methodName,
            Type[] parameterTypes,
            object[] arguments)
        {
            Type validatorType = ResolveType(typeName);
            AssertNotNull(validatorType, $"Validator type not found: '{typeName}'.");
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
                if (type != null)
                {
                    return type;
                }
            }
            return null;
        }

        private static int ReadInt(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(property, $"Property '{propertyName}' not found.");
            return (int)property.GetValue(target);
        }

        private static void AssertReportContains(object report, string expected)
        {
            PropertyInfo issuesProperty = report.GetType().GetProperty(
                "Issues",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            AssertTrue(issuesProperty?.GetValue(report) is IEnumerable,
                "Validation issues are unavailable.");
            foreach (object issue in (IEnumerable)issuesProperty.GetValue(report))
            {
                PropertyInfo messageProperty = issue.GetType().GetProperty(
                    "Message",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                string message = messageProperty?.GetValue(issue) as string;
                if (!string.IsNullOrEmpty(message) &&
                    message.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }
            }
            throw new InvalidOperationException(
                $"Validation report did not contain '{expected}'.");
        }

        private static void PrepareTempFolder()
        {
            CleanupTempFolder();
            string guid = AssetDatabase.CreateFolder(
                "Assets/ImmersiveFrameworkQA",
                "__P3H2_ActorSelectionAuthoring_Temp");
            AssertEqual(TempFolder, AssetDatabase.GUIDToAssetPath(guid),
                "Could not create deterministic P3H.2 temp folder.");
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

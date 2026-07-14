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
            }
            finally
            {
                CleanupTempFolder();
            }
        }

        private static void RunCases(List<string> completed)
        {
            GameObject playerHostPrefab = CreatePlayerHostPrefab();
            ActorProfile validProfile = CreateActorProfile(
                "ActorProfile_QA_Hero",
                "  actor-profile.qa.hero  ",
                "QA Hero",
                ActorKind.Player,
                ActorRole.Protagonist,
                playerHostPrefab);

            AssertEqual(
                "actor-profile.qa.hero",
                validProfile.ActorProfileIdText,
                "ActorProfileId text was not normalized.");
            completed.Add("actor-profile-id-normalized");

            ActorProfileId typedId = validProfile.ActorProfileId;
            AssertTrue(typedId.IsValid, "Typed ActorProfileId is invalid.");
            AssertEqual(
                FrameworkIdentityDomain.ActorProfile,
                typedId.Domain,
                "ActorProfileId uses the wrong identity domain.");
            completed.Add("actor-profile-typed-identity");

            object validReport = ValidateActorProfile(validProfile, true);
            AssertEqual(0, ReadInt(validReport, "ErrorCount"), "Valid ActorProfile was rejected.");
            completed.Add("valid-player-actor-profile");

            ActorProfile emptyIdentityProfile = CreateActorProfile(
                "ActorProfile_QA_EmptyIdentity",
                string.Empty,
                "QA Empty Identity",
                ActorKind.Player,
                ActorRole.Protagonist,
                playerHostPrefab);
            object emptyIdentityReport = ValidateActorProfile(emptyIdentityProfile, false);
            AssertTrue(ReadInt(emptyIdentityReport, "ErrorCount") > 0, "Empty ActorProfileId was accepted.");
            AssertReportContains(emptyIdentityReport, "requires a non-empty ActorProfileId");
            completed.Add("empty-actor-profile-id-rejected");

            ActorProfile duplicateProfile = CreateActorProfile(
                "ActorProfile_QA_Hero_Duplicate",
                "actor-profile.qa.hero",
                "QA Hero Duplicate",
                ActorKind.Player,
                ActorRole.Protagonist,
                playerHostPrefab);
            object duplicateReport = ValidateActorProfile(duplicateProfile, true);
            AssertTrue(ReadInt(duplicateReport, "ErrorCount") > 0, "Duplicate ActorProfileId was accepted.");
            AssertReportContains(duplicateReport, "also owned by ActorProfile");
            completed.Add("duplicate-actor-profile-id-rejected");

            ActorProfile missingHostProfile = CreateActorProfile(
                "ActorProfile_QA_MissingHost",
                "actor-profile.qa.missing-host",
                "QA Missing Host",
                ActorKind.Player,
                ActorRole.Protagonist,
                null);
            object missingHostReport = ValidateActorProfile(missingHostProfile, false);
            AssertTrue(ReadInt(missingHostReport, "ErrorCount") > 0, "Missing Logical Actor Host was accepted.");
            AssertReportContains(missingHostReport, "requires an explicit Logical Actor Host Prefab");
            completed.Add("missing-logical-host-rejected");

            ActorProfile unknownKindProfile = CreateActorProfile(
                "ActorProfile_QA_UnknownKind",
                "actor-profile.qa.unknown-kind",
                "QA Unknown Kind",
                ActorKind.Unknown,
                ActorRole.Protagonist,
                playerHostPrefab);
            object unknownKindReport = ValidateActorProfile(unknownKindProfile, false);
            AssertTrue(ReadInt(unknownKindReport, "ErrorCount") > 0, "Unknown Actor Kind was accepted.");
            AssertReportContains(unknownKindReport, "Actor Kind");
            completed.Add("unknown-actor-kind-rejected");

            ActorProfile mismatchedHostProfile = CreateActorProfile(
                "ActorProfile_QA_MismatchedHost",
                "actor-profile.qa.mismatched-host",
                "QA Mismatched Host",
                ActorKind.NonPlayer,
                ActorRole.Protagonist,
                playerHostPrefab);
            object mismatchedHostReport = ValidateActorProfile(mismatchedHostProfile, false);
            AssertTrue(ReadInt(mismatchedHostReport, "ErrorCount") > 0, "Mismatched host declaration was accepted.");
            AssertReportContains(mismatchedHostReport, "requires a Logical Actor Host with one ActorDeclaration");
            completed.Add("mismatched-host-kind-rejected");

            PlayerActorSelectionPolicyProfile allowPolicy = CreatePolicyProfile(
                "ActorSelection_QA_AllowDuplicates",
                "QA Allow Duplicates",
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates);
            object allowPolicyReport = ValidateSelectionPolicy(allowPolicy);
            AssertEqual(0, ReadInt(allowPolicyReport, "ErrorCount"), "AllowDuplicates policy was rejected.");
            AssertTrue(allowPolicy.AllowsDuplicates, "AllowDuplicates policy evidence is false.");
            completed.Add("allow-duplicates-policy-valid");

            PlayerActorSelectionPolicyProfile uniquePolicy = CreatePolicyProfile(
                "ActorSelection_QA_Unique",
                "QA Unique Across Joined Slots",
                PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots);
            object uniquePolicyReport = ValidateSelectionPolicy(uniquePolicy);
            AssertEqual(0, ReadInt(uniquePolicyReport, "ErrorCount"), "Unique policy was rejected.");
            AssertTrue(uniquePolicy.RequiresUniqueActors, "Unique policy evidence is false.");
            completed.Add("unique-policy-valid");

            PlayerActorSelectionPolicyProfile invalidPolicy = CreatePolicyProfile(
                "ActorSelection_QA_Unspecified",
                "QA Unspecified",
                PlayerActorSelectionDuplicatePolicy.Unspecified);
            object invalidPolicyReport = ValidateSelectionPolicy(invalidPolicy);
            AssertTrue(ReadInt(invalidPolicyReport, "ErrorCount") > 0, "Unspecified policy was accepted.");
            AssertReportContains(invalidPolicyReport, "requires an explicit Duplicate Policy");
            completed.Add("unspecified-policy-rejected");

            PlayerSlotProfile optionalDefaultSlot = CreateSlotProfile(
                "PlayerSlot_QA_NoDefault",
                "qa.p3h2.slot.no-default",
                null);
            object optionalDefaultReport = ValidatePlayerSlotProfile(optionalDefaultSlot);
            AssertEqual(0, ReadInt(optionalDefaultReport, "ErrorCount"), "Slot without default Actor was rejected.");
            AssertTrue(!optionalDefaultSlot.HasDefaultActorProfile, "Slot unexpectedly reports a default Actor.");
            completed.Add("slot-default-is-optional");

            PlayerSlotProfile validDefaultSlot = CreateSlotProfile(
                "PlayerSlot_QA_DefaultHero",
                "qa.p3h2.slot.default-hero",
                validProfile);
            object validDefaultReport = ValidatePlayerSlotProfile(validDefaultSlot);
            AssertEqual(0, ReadInt(validDefaultReport, "ErrorCount"), "Valid Slot default Actor was rejected.");
            AssertSame(validProfile, validDefaultSlot.DefaultActorProfile, "Slot default Actor reference changed.");
            completed.Add("slot-default-reference-valid");

            PlayerSlotProfile invalidDefaultSlot = CreateSlotProfile(
                "PlayerSlot_QA_InvalidDefault",
                "qa.p3h2.slot.invalid-default",
                missingHostProfile);
            object invalidDefaultReport = ValidatePlayerSlotProfile(invalidDefaultSlot);
            AssertTrue(ReadInt(invalidDefaultReport, "ErrorCount") > 0, "Invalid Slot default Actor was accepted.");
            AssertReportContains(invalidDefaultReport, "Logical Actor Host");
            completed.Add("invalid-slot-default-rejected");

            object projectReport = ValidateProjectActorSelectionProfiles();
            AssertTrue(ReadInt(projectReport, "ErrorCount") > 0, "Project duplicate scan accepted invalid Actor Profiles.");
            AssertReportContains(projectReport, "actor-profile.qa.hero");
            completed.Add("project-duplicate-scan-rejected");

            string idBeforeValidation = validProfile.ActorProfileIdText;
            GameObject hostBeforeValidation = validProfile.LogicalActorHostPrefab;
            ActorProfile defaultBeforeValidation = validDefaultSlot.DefaultActorProfile;
            ValidateActorProfile(validProfile, true);
            ValidatePlayerSlotProfile(validDefaultSlot);
            AssertEqual(idBeforeValidation, validProfile.ActorProfileIdText, "Validation mutated ActorProfileId.");
            AssertSame(hostBeforeValidation, validProfile.LogicalActorHostPrefab, "Validation mutated Logical Actor Host.");
            AssertSame(defaultBeforeValidation, validDefaultSlot.DefaultActorProfile, "Validation mutated Slot default Actor.");
            completed.Add("validation-is-non-mutating");

            AssertRuntimeImmutableProfile(typeof(ActorProfile));
            AssertRuntimeImmutableProfile(typeof(PlayerActorSelectionPolicyProfile));
            AssertRuntimeImmutableProfile(typeof(PlayerSlotProfile));
            completed.Add("profiles-are-runtime-immutable");

            RunPolicyTemplateCase();
            completed.Add("policy-template-set-created");
        }

        private static GameObject CreatePlayerHostPrefab()
        {
            const string prefabPath = TempFolder + "/P3H2_PlayerActorHost.prefab";
            var root = new GameObject("P3H2 Player Actor Host");
            try
            {
                PlayerInput playerInput = root.AddComponent<PlayerInput>();
                PlayerActorDeclaration declaration = root.AddComponent<PlayerActorDeclaration>();
                var serializedDeclaration = new SerializedObject(declaration);
                serializedDeclaration.FindProperty("actorId").stringValue = "qa.p3h2.actor.host";
                serializedDeclaration.FindProperty("displayName").stringValue = "QA P3H2 Actor Host";
                serializedDeclaration.FindProperty("playerInput").objectReferenceValue = playerInput;
                serializedDeclaration.FindProperty("reason").stringValue = "qa.p3h2.authoring";
                serializedDeclaration.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssertNotNull(prefab, "Could not create P3H.2 Player Actor Host prefab.");
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
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("actorProfileId").stringValue = actorProfileId;
            serializedProfile.FindProperty("displayName").stringValue = displayName;
            serializedProfile.FindProperty("description").stringValue = "P3H.2 QA Actor Profile fixture.";
            serializedProfile.FindProperty("actorKind").intValue = (int)actorKind;
            serializedProfile.FindProperty("actorRole").intValue = (int)actorRole;
            serializedProfile.FindProperty("logicalActorHostPrefab").objectReferenceValue = logicalHost;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
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
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("displayName").stringValue = displayName;
            serializedProfile.FindProperty("description").stringValue = "P3H.2 QA selection policy fixture.";
            serializedProfile.FindProperty("duplicatePolicy").intValue = (int)duplicatePolicy;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
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
            var serializedProfile = new SerializedObject(profile);
            serializedProfile.FindProperty("playerSlotId").stringValue = playerSlotId;
            serializedProfile.FindProperty("displayName").stringValue = fileName;
            serializedProfile.FindProperty("description").stringValue = "P3H.2 QA Player Slot fixture.";
            serializedProfile.FindProperty("defaultActorProfile").objectReferenceValue = defaultActorProfile;
            serializedProfile.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(profile, $"{TempFolder}/{fileName}.asset");
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void RunPolicyTemplateCase()
        {
            DefaultAsset folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(TempFolder);
            AssertNotNull(folder, "P3H.2 temp folder could not be selected for template creation.");
            Selection.activeObject = folder;

            bool executed = EditorApplication.ExecuteMenuItem(PolicyTemplateMenuPath);
            AssertTrue(executed, $"Template menu command was not found: '{PolicyTemplateMenuPath}'.");

            string templateFolder = $"{TempFolder}/PlayerParticipation";
            AssertTrue(AssetDatabase.IsValidFolder(templateFolder), "Policy template folder was not created.");

            string[] policyGuids = AssetDatabase.FindAssets(
                "t:PlayerActorSelectionPolicyProfile",
                new[] { templateFolder });
            AssertEqual(2, policyGuids.Length, "Policy template set did not create two Profiles.");

            bool foundAllow = false;
            bool foundUnique = false;
            for (int index = 0; index < policyGuids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(policyGuids[index]);
                PlayerActorSelectionPolicyProfile profile =
                    AssetDatabase.LoadAssetAtPath<PlayerActorSelectionPolicyProfile>(path);
                AssertNotNull(profile, $"Policy template could not be loaded at '{path}'.");
                object report = ValidateSelectionPolicy(profile);
                AssertEqual(0, ReadInt(report, "ErrorCount"), $"Policy template '{profile.name}' is invalid.");
                foundAllow |= profile.DuplicatePolicy == PlayerActorSelectionDuplicatePolicy.AllowDuplicates;
                foundUnique |= profile.DuplicatePolicy == PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots;
            }

            AssertTrue(foundAllow, "AllowDuplicates policy template was not created.");
            AssertTrue(foundUnique, "UniqueAcrossJoinedSlots policy template was not created.");
        }

        private static void AssertRuntimeImmutableProfile(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            for (int index = 0; index < properties.Length; index++)
            {
                AssertTrue(
                    properties[index].SetMethod == null,
                    $"Profile '{type.FullName}' exposes writable public property '{properties[index].Name}'.");
            }

            string[] lifecycleNames = { "Awake", "OnEnable", "Start", "Update" };
            for (int index = 0; index < lifecycleNames.Length; index++)
            {
                MethodInfo lifecycle = type.GetMethod(
                    lifecycleNames[index],
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                AssertTrue(lifecycle == null, $"Profile '{type.FullName}' declares gameplay lifecycle '{lifecycleNames[index]}'.");
            }
        }

        private static object ValidateActorProfile(ActorProfile profile, bool includeDuplicateScan)
        {
            return InvokeValidator(
                ActorValidatorTypeName,
                "ValidateActorProfile",
                new[] { typeof(ActorProfile), typeof(bool) },
                new object[] { profile, includeDuplicateScan });
        }

        private static object ValidateSelectionPolicy(PlayerActorSelectionPolicyProfile profile)
        {
            return InvokeValidator(
                ActorValidatorTypeName,
                "ValidateSelectionPolicyProfile",
                new[] { typeof(PlayerActorSelectionPolicyProfile) },
                new object[] { profile });
        }

        private static object ValidateProjectActorSelectionProfiles()
        {
            return InvokeValidator(
                ActorValidatorTypeName,
                "ValidateProjectActorSelectionProfiles",
                new[] { typeof(FrameworkValidationMode) },
                new object[] { FrameworkValidationMode.Standard });
        }

        private static object ValidatePlayerSlotProfile(PlayerSlotProfile profile)
        {
            return InvokeValidator(
                ParticipationValidatorTypeName,
                "ValidatePlayerSlotProfile",
                new[] { typeof(PlayerSlotProfile), typeof(bool) },
                new object[] { profile, false });
        }

        private static object InvokeValidator(
            string typeName,
            string methodName,
            Type[] parameterTypes,
            object[] arguments)
        {
            Type validatorType = ResolveType(typeName);
            AssertNotNull(validatorType, $"Package validator type was not found: '{typeName}'.");
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
            AssertNotNull(property, $"Property '{propertyName}' was not found on '{target.GetType().FullName}'.");
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
                $"Validation report did not contain expected text '{expected}'.");
        }

        private static void PrepareTempFolder()
        {
            CleanupTempFolder();
            string guid = AssetDatabase.CreateFolder(
                "Assets/ImmersiveFrameworkQA",
                "__P3H2_ActorSelectionAuthoring_Temp");
            string createdPath = AssetDatabase.GUIDToAssetPath(guid);
            AssertEqual(TempFolder, createdPath, "Could not create deterministic P3H.2 QA temp folder.");
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

using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Edit Mode smoke for the mandatory GameApplication Actor-selection policy contract.
    /// </summary>
    public static class QaP3H4GameApplicationPolicyAuthoringSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3H.4 Run Game Application Policy Authoring Smoke";
        private const string EditorAssemblyName = "Immersive.Framework.Editor";
        private const string ValidatorTypeName =
            "Immersive.Framework.Editor.Editor.PlayerParticipation.PlayerParticipationAuthoringValidator";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                GameApplicationAsset gameApplication =
                    ScriptableObject.CreateInstance<GameApplicationAsset>();
                gameApplication.name = "P3H4 Game Application";
                created.Add(gameApplication);

                PlayerSlotProfile slot = ScriptableObject.CreateInstance<PlayerSlotProfile>();
                slot.name = "P3H4 Slot";
                var serializedSlot = new SerializedObject(slot);
                serializedSlot.FindProperty("playerSlotId").stringValue = "qa.p3h4.slot.1";
                serializedSlot.FindProperty("displayName").stringValue = "P3H4 Player 1";
                serializedSlot.ApplyModifiedPropertiesWithoutUndo();
                created.Add(slot);

                ConfigureApplication(gameApplication, slot, null);
                AssertTrue(gameApplication.PlayerActorSelectionPolicyProfile == null,
                    "GameApplication unexpectedly inferred a selection policy.");
                AssertTrue(!gameApplication.HasPlayerActorSelectionPolicy,
                    "GameApplication reports a policy when none is configured.");
                completed.Add("policy-has-no-silent-fallback");

                object missingReport = Validate(gameApplication);
                AssertTrue(ReadInt(missingReport, "ErrorCount") > 0,
                    "Missing GameApplication Actor selection policy was not rejected.");
                completed.Add("missing-policy-rejected");

                PlayerActorSelectionPolicyProfile invalidPolicy = CreatePolicy(
                    created,
                    "Invalid Policy",
                    PlayerActorSelectionDuplicatePolicy.Unspecified);
                ConfigureApplication(gameApplication, slot, invalidPolicy);
                object invalidReport = Validate(gameApplication);
                AssertTrue(ReadInt(invalidReport, "ErrorCount") > 0,
                    "Unspecified GameApplication Actor selection policy was not rejected.");
                completed.Add("invalid-policy-rejected");

                PlayerActorSelectionPolicyProfile validPolicy = CreatePolicy(
                    created,
                    "Unique Policy",
                    PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots);
                ConfigureApplication(gameApplication, slot, validPolicy);
                AssertSame(validPolicy, gameApplication.PlayerActorSelectionPolicyProfile,
                    "GameApplication did not preserve the configured policy reference.");
                AssertTrue(gameApplication.HasPlayerActorSelectionPolicy,
                    "GameApplication did not recognize the valid policy.");
                completed.Add("valid-policy-exposed");

                string applicationBefore = EditorJsonUtility.ToJson(gameApplication);
                string slotBefore = EditorJsonUtility.ToJson(slot);
                string policyBefore = EditorJsonUtility.ToJson(validPolicy);
                object validReport = Validate(gameApplication);
                AssertEqual(0, ReadInt(validReport, "ErrorCount"),
                    "Valid GameApplication participation policy configuration was rejected.");
                completed.Add("valid-policy-accepted");

                AssertEqual(applicationBefore, EditorJsonUtility.ToJson(gameApplication),
                    "Validation mutated GameApplication.");
                AssertEqual(slotBefore, EditorJsonUtility.ToJson(slot),
                    "Validation mutated PlayerSlotProfile.");
                AssertEqual(policyBefore, EditorJsonUtility.ToJson(validPolicy),
                    "Validation mutated selection policy Profile.");
                completed.Add("validation-is-non-mutating");

                Debug.Log(
                    "[P3H4_GAME_APPLICATION_POLICY_AUTHORING_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3H4_GAME_APPLICATION_POLICY_AUTHORING_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private static void ConfigureApplication(
            GameApplicationAsset gameApplication,
            PlayerSlotProfile slot,
            PlayerActorSelectionPolicyProfile policy)
        {
            var serialized = new SerializedObject(gameApplication);
            SerializedProperty slots = serialized.FindProperty("localPlayerSlots");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = slot;
            serialized.FindProperty("playerActorSelectionPolicyProfile").objectReferenceValue = policy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerActorSelectionPolicyProfile CreatePolicy(
            ICollection<UnityEngine.Object> created,
            string name,
            PlayerActorSelectionDuplicatePolicy duplicatePolicy)
        {
            var policy = ScriptableObject.CreateInstance<PlayerActorSelectionPolicyProfile>();
            policy.name = name;
            var serialized = new SerializedObject(policy);
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("duplicatePolicy").intValue = (int)duplicatePolicy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(policy);
            return policy;
        }

        private static object Validate(GameApplicationAsset gameApplication)
        {
            Type validatorType = Type.GetType($"{ValidatorTypeName}, {EditorAssemblyName}");
            if (validatorType == null)
            {
                throw new InvalidOperationException(
                    $"Validator type '{ValidatorTypeName}' was not found.");
            }

            MethodInfo validate = validatorType.GetMethod(
                "ValidateGameApplication",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(GameApplicationAsset), typeof(bool) },
                null);
            if (validate == null)
            {
                throw new MissingMethodException(validatorType.FullName, "ValidateGameApplication");
            }

            return validate.Invoke(null, new object[] { gameApplication, true });
        }

        private static int ReadInt(object target, string propertyName)
        {
            if (target == null)
            {
                throw new InvalidOperationException("Validation report is missing.");
            }

            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (property == null)
            {
                throw new MissingMemberException(target.GetType().FullName, propertyName);
            }

            return (int)property.GetValue(target);
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
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

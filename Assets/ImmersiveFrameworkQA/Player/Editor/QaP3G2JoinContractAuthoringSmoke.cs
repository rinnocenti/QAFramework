
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Editor-only P3G.2 smoke for passive join contracts and provisioning authoring.
    /// It never calls PlayerInputManager.JoinPlayer.
    /// </summary>
    public static class QaP3G2JoinContractAuthoringSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3G.2 Run Join Contract Authoring Smoke";

        private const string ValidatorTypeName =
            "Immersive.Framework.Editor.Editor.PlayerParticipation.LocalPlayerProvisioningValidator";

        private static readonly BindingFlags StaticInternal =
            BindingFlags.Static | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                var request = new LocalPlayerJoinRequest(
                    "  QA.P3G2  ",
                    "  validate-contract  ",
                    null,
                    "  Keyboard&Mouse  ");
                AssertTrue(request.IsValid, "Nominal LocalPlayerJoinRequest is invalid.");
                AssertEqual("QA.P3G2", request.Source, "Request source was not normalized.");
                AssertEqual("validate-contract", request.Reason, "Request reason was not normalized.");
                AssertEqual("Keyboard&Mouse", request.ControlScheme, "Control scheme was not normalized.");
                AssertTrue(!request.HasDeviceHint, "Null device was reported as a device hint.");
                AssertTrue(request.HasControlSchemeHint, "Control scheme hint was not preserved.");
                completed.Add("request-normalized-and-valid");

                var invalidSource = new LocalPlayerJoinRequest(" ", "join");
                AssertTrue(!invalidSource.TryValidate(out string sourceIssue), "Empty request source was accepted.");
                AssertContains(sourceIssue, "source");
                completed.Add("empty-source-rejected");

                var invalidReason = new LocalPlayerJoinRequest("QA.P3G2", " ");
                AssertTrue(!invalidReason.TryValidate(out string reasonIssue), "Empty request reason was accepted.");
                AssertContains(reasonIssue, "reason");
                completed.Add("empty-reason-rejected");

                LocalPlayerJoinOperationId firstId = CreateOperationId("qa-session", 1);
                LocalPlayerJoinOperationId secondId = CreateOperationId("qa-session", 2);
                AssertTrue(firstId.IsValid, "Generated operation id is invalid.");
                AssertTrue(firstId != secondId, "Distinct sequences produced equal operation ids.");
                AssertEqual(
                    "local-player-join:qa-session:1",
                    firstId.StableText,
                    "Operation id diagnostic shape changed.");
                completed.Add("operation-id-session-scoped");

                AssertOperationIdRejected("", 1, "Empty Session context was accepted.");
                AssertOperationIdRejected("qa-session", 0, "Non-positive operation sequence was accepted.");
                completed.Add("invalid-operation-id-rejected");

                object missingAuthoringReport = Validate(null, null);
                AssertHasErrors(missingAuthoringReport, "Missing authoring was accepted.");
                AssertReportContains(missingAuthoringReport, "Authoring is missing");
                completed.Add("missing-authoring-rejected");

                GameObject authoringObject = CreateGameObject(created, "QA P3G2 Authoring");
                LocalPlayerProvisioningAuthoring authoring =
                    authoringObject.AddComponent<LocalPlayerProvisioningAuthoring>();
                object missingManagerReport = Validate(authoring, null);
                AssertHasErrors(missingManagerReport, "Missing PlayerInputManager was accepted.");
                AssertReportContains(missingManagerReport, "explicit PlayerInputManager reference");
                completed.Add("missing-manager-rejected");

                GameObject managerObject = CreateGameObject(created, "QA P3G2 Manager");
                PlayerInputManager manager = managerObject.AddComponent<PlayerInputManager>();
                AssignManager(authoring, manager);
                manager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
                object automaticJoinReport = Validate(authoring, null);
                AssertHasErrors(automaticJoinReport, "Automatic join behavior was accepted.");
                AssertReportContains(automaticJoinReport, "Join Players Manually");
                completed.Add("automatic-join-rejected");

                manager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
                object missingPrefabReport = Validate(authoring, null);
                AssertHasErrors(missingPrefabReport, "Missing Player Prefab was accepted.");
                AssertReportContains(missingPrefabReport, "has no Player Prefab");
                completed.Add("missing-player-prefab-rejected");

                GameObject invalidPrefab = CreateGameObject(created, "QA P3G2 Invalid Player Prefab");
                manager.playerPrefab = invalidPrefab;
                object missingPlayerInputReport = Validate(authoring, null);
                AssertHasErrors(missingPlayerInputReport, "Player Prefab without PlayerInput was accepted.");
                AssertReportContains(missingPlayerInputReport, "has no PlayerInput component");
                completed.Add("missing-player-input-rejected");

                GameObject validPrefab = CreateGameObject(created, "QA P3G2 Valid Player Prefab");
                validPrefab.AddComponent<PlayerInput>();
                manager.playerPrefab = validPrefab;

                PlayerSlotProfile slotOne = CreateSlot(created, "QA P3G2 Slot 1", "qa.p3g2.player.1");
                PlayerSlotProfile slotTwo = CreateSlot(created, "QA P3G2 Slot 2", "qa.p3g2.player.2");
                GameApplicationAsset application = CreateApplication(created, slotOne, slotTwo);

                object validReport = Validate(authoring, application);
                AssertNoErrors(validReport, "Valid manual provisioning authoring was rejected.");
                AssertReportContains(validReport, "authoring is valid");
                completed.Add("manual-provisioning-valid");

                AssertSame(manager, authoring.PlayerInputManager, "Authoring did not preserve explicit manager reference.");
                AssertTrue(authoring.UsesManualJoin, "Authoring does not report manual join.");
                AssertSame(validPrefab, authoring.PlayerPrefab, "Authoring Player Prefab evidence changed.");
                completed.Add("authoring-evidence-exposed");

                string authoringBefore = EditorJsonUtility.ToJson(authoring);
                string applicationBefore = EditorJsonUtility.ToJson(application);
                Validate(authoring, application);
                AssertEqual(authoringBefore, EditorJsonUtility.ToJson(authoring), "Validation mutated authoring.");
                AssertEqual(applicationBefore, EditorJsonUtility.ToJson(application), "Validation mutated Game Application.");
                completed.Add("validation-is-non-mutating");

                AssertTrue(
                    typeof(LocalPlayerProvisioningAuthoring).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null &&
                    typeof(LocalPlayerProvisioningAuthoring).GetMethod("Start", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) == null,
                    "Authoring component introduced lifecycle gameplay execution.");
                completed.Add("authoring-has-no-gameplay-lifecycle");

                Debug.Log(
                    "[P3G2_JOIN_CONTRACT_AUTHORING_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3G2_JOIN_CONTRACT_AUTHORING_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
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

        private static LocalPlayerJoinOperationId CreateOperationId(
            string context,
            int sequence)
        {
            MethodInfo method = typeof(LocalPlayerJoinOperationId).GetMethod(
                "TryCreate",
                StaticInternal);
            AssertNotNull(method, "LocalPlayerJoinOperationId.TryCreate was not found.");
            object[] arguments = { context, sequence, null, null };
            bool succeeded = (bool)method.Invoke(null, arguments);
            AssertTrue(succeeded, $"Operation id creation failed. issue='{arguments[3]}'.");
            return (LocalPlayerJoinOperationId)arguments[2];
        }

        private static void AssertOperationIdRejected(
            string context,
            int sequence,
            string message)
        {
            MethodInfo method = typeof(LocalPlayerJoinOperationId).GetMethod(
                "TryCreate",
                StaticInternal);
            AssertNotNull(method, "LocalPlayerJoinOperationId.TryCreate was not found.");
            object[] arguments = { context, sequence, null, null };
            bool succeeded = (bool)method.Invoke(null, arguments);
            AssertTrue(!succeeded, message);
            AssertTrue(!string.IsNullOrWhiteSpace(arguments[3] as string), message + " No issue was returned.");
        }

        private static object Validate(
            LocalPlayerProvisioningAuthoring authoring,
            GameApplicationAsset application)
        {
            Type validatorType = typeof(LocalPlayerProvisioningAuthoring).Assembly
                .GetType(ValidatorTypeName, false);
            if (validatorType == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    validatorType = assembly.GetType(ValidatorTypeName, false);
                    if (validatorType != null)
                    {
                        break;
                    }
                }
            }

            AssertNotNull(validatorType, $"Validator type '{ValidatorTypeName}' was not found.");
            MethodInfo method = validatorType.GetMethod("Validate", StaticInternal);
            AssertNotNull(method, "LocalPlayerProvisioningValidator.Validate was not found.");
            return method.Invoke(null, new object[] { authoring, application });
        }

        private static void AssignManager(
            LocalPlayerProvisioningAuthoring authoring,
            PlayerInputManager manager)
        {
            var serialized = new SerializedObject(authoring);
            SerializedProperty property = serialized.FindProperty("playerInputManager");
            AssertNotNull(property, "Authoring PlayerInputManager serialized property was not found.");
            property.objectReferenceValue = manager;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameApplicationAsset CreateApplication(
            ICollection<UnityEngine.Object> created,
            params PlayerSlotProfile[] slots)
        {
            var application = ScriptableObject.CreateInstance<GameApplicationAsset>();
            application.name = "QA P3G2 Game Application";
            var serialized = new SerializedObject(application);
            SerializedProperty localSlots = serialized.FindProperty("localPlayerSlots");
            localSlots.arraySize = slots.Length;
            for (int index = 0; index < slots.Length; index++)
            {
                localSlots.GetArrayElementAtIndex(index).objectReferenceValue = slots[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(application);
            return application;
        }

        private static PlayerSlotProfile CreateSlot(
            ICollection<UnityEngine.Object> created,
            string displayName,
            string slotId)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = slotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static GameObject CreateGameObject(
            ICollection<UnityEngine.Object> created,
            string name)
        {
            var gameObject = new GameObject(name);
            created.Add(gameObject);
            return gameObject;
        }

        private static int ErrorCount(object report)
        {
            AssertNotNull(report, "Validation report is null.");
            PropertyInfo property = report.GetType().GetProperty(
                "ErrorCount",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            AssertNotNull(property, "Validation report ErrorCount was not found.");
            return (int)property.GetValue(report);
        }

        private static void AssertNoErrors(object report, string message)
        {
            if (ErrorCount(report) != 0)
            {
                throw new InvalidOperationException(message + " " + DescribeReport(report));
            }
        }

        private static void AssertHasErrors(object report, string message)
        {
            if (ErrorCount(report) <= 0)
            {
                throw new InvalidOperationException(message + " " + DescribeReport(report));
            }
        }

        private static void AssertReportContains(object report, string expected)
        {
            string description = DescribeReport(report);
            AssertTrue(
                description.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0,
                $"Validation report does not contain '{expected}'. report='{description}'.");
        }

        private static string DescribeReport(object report)
        {
            PropertyInfo issuesProperty = report.GetType().GetProperty(
                "Issues",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (issuesProperty == null || issuesProperty.GetValue(report) is not IEnumerable issues)
            {
                return report.ToString();
            }

            var messages = new List<string>();
            foreach (object issue in issues)
            {
                if (issue == null)
                {
                    continue;
                }

                PropertyInfo messageProperty = issue.GetType().GetProperty(
                    "Message",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                messages.Add(messageProperty != null
                    ? messageProperty.GetValue(issue) as string ?? string.Empty
                    : issue.ToString());
            }

            return string.Join(" | ", messages);
        }

        private static void AssertContains(string actual, string expected)
        {
            AssertTrue(
                actual != null && actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0,
                $"Expected '{expected}' in '{actual}'.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
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

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
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

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

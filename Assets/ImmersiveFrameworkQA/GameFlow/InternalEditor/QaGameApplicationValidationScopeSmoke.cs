using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaGameApplicationValidationScopeSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Authoring/Run Game Application Validation Scope Smoke";
        private const string LogPrefix =
            "[GAME_APPLICATION_VALIDATION_SCOPE_SMOKE]";
        private const string TemporaryRoot =
            "Assets/ImmersiveFrameworkQA/Generated/GameApplicationValidationScope";
        private const string SentinelActorName =
            "QA_Unrelated_Invalid_Actor_Profile";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            global::UnityEditor.Editor customEditor = null;
            GameApplicationAsset gameApplication = null;

            try
            {
                EnsureFolder(TemporaryRoot);
                CreateSentinelActorProfile();

                gameApplication =
                    ScriptableObject.CreateInstance<GameApplicationAsset>();
                gameApplication.name =
                    "QA_GameApplication_NoPlayerSlots";

                object participationReport =
                    InvokePlayerParticipationValidation(
                        gameApplication);

                Require(
                    ReadIntProperty(
                        participationReport,
                        "ErrorCount") == 0,
                    "A Game Application with zero Local Player Slots must be valid when Player participation is not configured. " +
                    BuildReportDiagnostic(participationReport));
                Require(
                    ContainsMessage(
                        participationReport,
                        "No Local Player Slots are configured"),
                    "Zero-Slot validation did not preserve explicit optional-state diagnostics. " +
                    BuildReportDiagnostic(participationReport));

                customEditor =
                    global::UnityEditor.Editor.CreateEditor(gameApplication);
                Require(
                    customEditor != null,
                    "GameApplicationAsset custom Editor could not be created.");

                object localReport =
                    InvokeGameApplicationEditorValidation(
                        customEditor);

                Require(
                    !ContainsMessage(
                        localReport,
                        "Local Player Slots are missing"),
                    "Local Game Application validation still treats zero Slots as a blocking requirement. " +
                    BuildReportDiagnostic(localReport));
                Require(
                    !ContainsMessage(
                        localReport,
                        SentinelActorName),
                    "Local Game Application validation leaked an unrelated project ActorProfile finding. " +
                    BuildReportDiagnostic(localReport));

                object projectAuditReport =
                    InvokeProjectProfileAudit(
                        gameApplication.ValidationMode);
                Require(
                    ContainsMessage(
                        projectAuditReport,
                        SentinelActorName),
                    "Explicit project Profile audit did not report the unrelated invalid ActorProfile. " +
                    BuildReportDiagnostic(projectAuditReport));

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='3' " +
                    "zeroSlots='ValidOptional' localScope='Isolated' projectAudit='Explicit'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' exception='{exception.GetType().Name}' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                if (customEditor != null)
                {
                    UnityObject.DestroyImmediate(customEditor);
                }

                if (gameApplication != null)
                {
                    UnityObject.DestroyImmediate(gameApplication);
                }

                AssetDatabase.DeleteAsset(TemporaryRoot);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void CreateSentinelActorProfile()
        {
            string assetPath =
                $"{TemporaryRoot}/{SentinelActorName}.asset";
            AssetDatabase.DeleteAsset(assetPath);

            ActorProfile profile =
                ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = SentinelActorName;
            AssetDatabase.CreateAsset(
                profile,
                assetPath);
            AssetDatabase.SaveAssets();
        }

        private static object InvokePlayerParticipationValidation(
            GameApplicationAsset gameApplication)
        {
            Type validatorType =
                RequireType(
                    "Immersive.Framework.Editor.Editor.PlayerParticipation.PlayerParticipationAuthoringValidator, Immersive.Framework.Editor");
            MethodInfo method =
                validatorType.GetMethod(
                    "ValidateGameApplication",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new[]
                    {
                        typeof(GameApplicationAsset),
                        typeof(bool)
                    },
                    null);

            Require(
                method != null,
                "PlayerParticipationAuthoringValidator.ValidateGameApplication(GameApplicationAsset, bool) was not found.");

            return method.Invoke(
                null,
                new object[]
                {
                    gameApplication,
                    true
                });
        }

        private static object InvokeGameApplicationEditorValidation(
            global::UnityEditor.Editor customEditor)
        {
            MethodInfo method =
                customEditor.GetType().GetMethod(
                    "RunAuthoringValidation",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo reportField =
                customEditor.GetType().GetField(
                    "_lastValidationReport",
                    BindingFlags.Instance | BindingFlags.NonPublic);

            Require(
                method != null,
                "GameApplicationAssetEditor.RunAuthoringValidation was not found.");
            Require(
                reportField != null,
                "GameApplicationAssetEditor validation report field was not found.");

            method.Invoke(customEditor, null);
            object report =
                reportField.GetValue(customEditor);
            Require(
                report != null,
                "GameApplicationAssetEditor did not produce a validation report.");
            return report;
        }

        private static object InvokeProjectProfileAudit(
            FrameworkValidationMode validationMode)
        {
            Type validatorType =
                RequireType(
                    "Immersive.Framework.Editor.Editor.PlayerParticipation.PlayerParticipationAuthoringValidator, Immersive.Framework.Editor");
            MethodInfo method =
                validatorType.GetMethod(
                    "ValidateProjectProfiles",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(FrameworkValidationMode) },
                    null);

            Require(
                method != null,
                "PlayerParticipationAuthoringValidator.ValidateProjectProfiles was not found.");

            return method.Invoke(
                null,
                new object[] { validationMode });
        }

        private static Type RequireType(
            string assemblyQualifiedName)
        {
            Type type =
                Type.GetType(
                    assemblyQualifiedName,
                    false);
            Require(
                type != null,
                $"Required Editor type was not found: '{assemblyQualifiedName}'.");
            return type;
        }

        private static int ReadIntProperty(
            object report,
            string propertyName)
        {
            PropertyInfo property =
                report?.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.NonPublic);
            Require(
                property != null,
                $"Validation report property '{propertyName}' was not found.");
            return (int)property.GetValue(report);
        }

        private static bool ContainsMessage(
            object report,
            string expectedText)
        {
            IReadOnlyList<string> messages =
                ReadMessages(report);
            for (int index = 0;
                 index < messages.Count;
                 index++)
            {
                if (messages[index].IndexOf(
                        expectedText,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<string> ReadMessages(
            object report)
        {
            PropertyInfo issuesProperty =
                report?.GetType().GetProperty(
                    "Issues",
                    BindingFlags.Instance | BindingFlags.NonPublic);
            Require(
                issuesProperty != null,
                "Validation report Issues property was not found.");

            var messages = new List<string>();
            IEnumerable issues =
                issuesProperty.GetValue(report) as IEnumerable;
            if (issues == null)
            {
                return messages;
            }

            foreach (object issue in issues)
            {
                PropertyInfo messageProperty =
                    issue.GetType().GetProperty(
                        "Message",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                if (messageProperty == null)
                {
                    continue;
                }

                messages.Add(
                    messageProperty.GetValue(issue) as string ??
                    string.Empty);
            }

            return messages;
        }

        private static string BuildReportDiagnostic(
            object report)
        {
            IReadOnlyList<string> messages =
                ReadMessages(report);
            return
                $"errors='{ReadIntProperty(report, "ErrorCount")}' " +
                $"issues='{string.Join(" | ", messages)}'.";
        }

        private static void EnsureFolder(
            string folderPath)
        {
            string[] segments =
                folderPath.Split('/');
            string current = segments[0];

            for (int index = 1;
                 index < segments.Length;
                 index++)
            {
                string next =
                    $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        segments[index]);
                }

                current = next;
            }
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

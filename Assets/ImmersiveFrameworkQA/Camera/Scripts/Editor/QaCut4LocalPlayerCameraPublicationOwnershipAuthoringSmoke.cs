using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.Camera.Scripts.Editor
{
    /// <summary>
    /// Edit Mode proof for Cut 4 Local Player camera publication ownership.
    /// It validates the product default and the duplicate-publisher authoring guard.
    /// </summary>
    internal static class QaCut4LocalPlayerCameraPublicationOwnershipAuthoringSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Cut 4 Run Local Player Camera Publication Ownership Authoring Smoke";
        private const string ReportTypeName =
            "Immersive.Framework.Editor.Editor.Validation.FrameworkAuthoringValidationReport";
        private const string ValidatorTypeName =
            "Immersive.Framework.Editor.Editor.Validation.FrameworkLocalPlayerCameraPublicationValidator";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        internal static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,
                    "Cut 4 authoring smoke must run in Edit Mode.");
                completed.Add("edit-mode-required");

                int baselineErrors = ValidateAndReadErrorCount();

                GameObject hostObject = CreateObject("Cut4 Camera Host", created);
                LocalPlayerHostAuthoring host =
                    hostObject.AddComponent<LocalPlayerHostAuthoring>();

                GameObject actorObject = CreateObject("Cut4 Camera Actor", created);
                actorObject.transform.SetParent(hostObject.transform, false);
                PlayerActorDeclaration actor =
                    actorObject.AddComponent<PlayerActorDeclaration>();

                GameObject otherActorObject = CreateObject(
                    "Cut4 Camera Other Actor",
                    created);
                otherActorObject.transform.SetParent(hostObject.transform, false);
                PlayerActorDeclaration otherActor =
                    otherActorObject.AddComponent<PlayerActorDeclaration>();

                LocalPlayerCameraRequestBinding binding =
                    actorObject.AddComponent<LocalPlayerCameraRequestBinding>();
                SetObjectReference(binding, "localPlayerHost", host);
                SetObjectReference(binding, "playerActor", actor);
                SetBoolean(binding, "eligibleOnEnable", false);
                SetBoolean(binding, "logDiagnostics", false);

                SceneLocalPlayerAdmissionAuthoring admission =
                    hostObject.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
                SetObjectReference(admission, "localPlayerHost", host);
                SetObjectReference(admission, "sceneLogicalPlayerActor", actor);

                Require(!binding.IsSceneAutoPublisherOptIn,
                    "Local Player Camera Request Binding did not default to authoring evidence.");
                completed.Add("scene-auto-publisher-default-off");

                Require(string.Equals(
                        binding.PublisherSource,
                        "AuthoringEvidence",
                        StringComparison.Ordinal),
                    "Default Local Player camera binding source is not AuthoringEvidence.");
                completed.Add("default-source-is-authoring-evidence");

                Require(!binding.TryPublish() && !binding.IsPublished,
                    "Authoring-evidence binding published without explicit Scene Auto-Publisher opt-in.");
                Require(string.Equals(binding.LastStatus, "Blocked", StringComparison.Ordinal),
                    "Blocked default publication did not retain an explicit diagnostic state.");
                completed.Add("default-publication-blocked");

                SetBoolean(binding, "eligibleOnEnable", true);
                Require(binding.IsSceneAutoPublisherOptIn,
                    "Scene Auto-Publisher opt-in was not retained.");
                Require(string.Equals(
                        binding.PublisherSource,
                        "SceneAutoPublisherOptIn",
                        StringComparison.Ordinal),
                    "Opt-in binding source is not SceneAutoPublisherOptIn.");
                completed.Add("explicit-scene-publisher-opt-in-visible");

                int conflictingErrors = ValidateAndReadErrorCount();
                Require(conflictingErrors == baselineErrors + 1,
                    $"Matching Scene Admission plus Scene Auto-Publisher should add one blocking issue. baseline='{baselineErrors}' current='{conflictingErrors}'.");
                completed.Add("matching-admission-and-scene-publisher-rejected");

                SetBoolean(binding, "eligibleOnEnable", false);
                Require(ValidateAndReadErrorCount() == baselineErrors,
                    "Disabling Scene Auto-Publisher did not remove the duplicate-publication issue.");
                completed.Add("authoring-evidence-with-admission-accepted");

                SetBoolean(binding, "eligibleOnEnable", true);
                SetObjectReference(admission, "sceneLogicalPlayerActor", otherActor);
                Require(ValidateAndReadErrorCount() == baselineErrors,
                    "A Scene Auto-Publisher for a different Actor was incorrectly treated as the same admitted Player.");
                completed.Add("different-player-evidence-not-collapsed");

                Require(completed.Count == 8,
                    "Cut 4 authoring smoke case count changed unexpectedly.");
                Debug.Log(
                    "[CUT4_LOCAL_PLAYER_CAMERA_PUBLICATION_OWNERSHIP_AUTHORING_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception resolved = Unwrap(exception);
                Debug.LogError(
                    "[CUT4_LOCAL_PLAYER_CAMERA_PUBLICATION_OWNERSHIP_AUTHORING_SMOKE] " +
                    $"status='Failed' exception='{resolved.GetType().Name}' message='{Escape(resolved.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw resolved;
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

        private static int ValidateAndReadErrorCount()
        {
            Type reportType = ResolveType(ReportTypeName);
            Type validatorType = ResolveType(ValidatorTypeName);
            object report = Activator.CreateInstance(reportType, nonPublic: true);
            Require(report != null,
                "Framework authoring validation report could not be created.");

            MethodInfo validate = validatorType.GetMethod(
                "ValidateOpenScenes",
                StaticAny);
            Require(validate != null,
                "Framework Local Player camera publication validator was not found.");
            validate.Invoke(null, new[] { report });

            PropertyInfo errorCount = reportType.GetProperty("ErrorCount", InstanceAny);
            Require(errorCount != null,
                "Framework authoring validation report ErrorCount was not found.");
            return (int)errorCount.GetValue(report);
        }

        private static Type ResolveType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Type type = assemblies[index].GetType(fullName, throwOnError: false);
                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException($"Type '{fullName}' was not found.");
        }

        private static GameObject CreateObject(
            string name,
            ICollection<UnityEngine.Object> created)
        {
            var instance = new GameObject(name);
            created.Add(instance);
            return instance;
        }

        private static void SetObjectReference(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Serialized property '{target.GetType().Name}.{propertyName}' was not found.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBoolean(
            UnityEngine.Object target,
            string propertyName,
            bool value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Serialized property '{target.GetType().Name}.{propertyName}' was not found.");
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Exception Unwrap(Exception exception)
        {
            if (exception is TargetInvocationException invocation &&
                invocation.InnerException != null)
            {
                return Unwrap(invocation.InnerException);
            }

            if (exception is AggregateException aggregate &&
                aggregate.InnerExceptions.Count == 1)
            {
                return Unwrap(aggregate.InnerExceptions[0]);
            }

            return exception;
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

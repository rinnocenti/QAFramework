using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Edit Mode proof for Cut 5 explicit UIGlobal Local Player provisioning composition.
    /// It exercises the internal composition resolver without changing package visibility.
    /// </summary>
    internal static class QaCut5ProvisioningCompositionRootSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Cut 5 Run Provisioning Composition Root Smoke";
        private const string RuntimeTypeName =
            "Immersive.Framework.GlobalUi.GlobalUiSceneRuntime";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                Require(!EditorApplication.isPlayingOrWillChangePlaymode,
                    "Cut 5 provisioning composition smoke must run in Edit Mode.");
                completed.Add("edit-mode-required");

                GameObject strayRoot = CreateObject(
                    "Cut5 Stray Authoring Root",
                    created);
                LocalPlayerProvisioningAuthoring strayAuthoring =
                    strayRoot.AddComponent<LocalPlayerProvisioningAuthoring>();
                ResolveResult noRegistration = Resolve(strayRoot);
                Require(noRegistration.Succeeded &&
                    !noRegistration.IsConfigured &&
                    noRegistration.Authoring == null,
                    "A global LocalPlayerProvisioningAuthoring was consumed without Host Registration.");
                completed.Add("unregistered-global-authoring-ignored");

                GameObject invalidRoot = CreateObject(
                    "Cut5 Invalid Registration Root",
                    created);
                LocalPlayerProvisioningHostRegistration invalidRegistration =
                    invalidRoot.AddComponent<LocalPlayerProvisioningHostRegistration>();
                Require(!invalidRegistration.TryResolveAuthoring(out _, out string directIssue) &&
                    !string.IsNullOrWhiteSpace(directIssue),
                    "Host Registration without explicit authoring did not fail directly.");
                completed.Add("missing-registration-reference-rejected");

                ResolveResult invalid = Resolve(invalidRoot);
                Require(!invalid.Succeeded &&
                    invalid.IsConfigured &&
                    invalid.Authoring == null &&
                    !string.IsNullOrWhiteSpace(invalid.Diagnostic),
                    "UIGlobal composition accepted an invalid Host Registration.");
                completed.Add("invalid-composition-rejected");

                GameObject validRoot = CreateObject(
                    "Cut5 Valid Registration Root",
                    created);
                LocalPlayerProvisioningAuthoring validAuthoring =
                    validRoot.AddComponent<LocalPlayerProvisioningAuthoring>();
                LocalPlayerProvisioningHostRegistration validRegistration =
                    validRoot.AddComponent<LocalPlayerProvisioningHostRegistration>();
                SetObjectReference(
                    validRegistration,
                    "provisioningAuthoring",
                    validAuthoring);

                ResolveResult valid = Resolve(validRoot);
                Require(valid.Succeeded &&
                    valid.IsConfigured &&
                    ReferenceEquals(valid.Authoring, validAuthoring),
                    "One explicit Host Registration did not resolve its exact authoring.");
                completed.Add("single-registration-resolves-exact-authoring");

                GameObject duplicateRoot = CreateObject(
                    "Cut5 Duplicate Registration Root",
                    created);
                LocalPlayerProvisioningAuthoring duplicateAuthoring =
                    duplicateRoot.AddComponent<LocalPlayerProvisioningAuthoring>();
                LocalPlayerProvisioningHostRegistration duplicateRegistration =
                    duplicateRoot.AddComponent<LocalPlayerProvisioningHostRegistration>();
                SetObjectReference(
                    duplicateRegistration,
                    "provisioningAuthoring",
                    duplicateAuthoring);

                ResolveResult duplicates = Resolve(validRoot, duplicateRoot);
                Require(!duplicates.Succeeded &&
                    duplicates.IsConfigured &&
                    duplicates.Authoring == null &&
                    duplicates.Diagnostic.Contains("found '2'", StringComparison.Ordinal),
                    "Duplicate UIGlobal Host Registrations were not rejected explicitly.");
                completed.Add("duplicate-registrations-rejected");

                GameObject neutralRoot = CreateObject(
                    "Cut5 Neutral UIGlobal Root",
                    created);
                ResolveResult multipleRoots = Resolve(neutralRoot, validRoot);
                Require(multipleRoots.Succeeded &&
                    multipleRoots.IsConfigured &&
                    ReferenceEquals(multipleRoots.Authoring, validAuthoring),
                    "One registration across multiple persisted UIGlobal roots did not resolve.");
                completed.Add("multiple-roots-preserve-single-registration");

                ResolveResult registeredWins = Resolve(strayRoot, validRoot);
                Require(registeredWins.Succeeded &&
                    registeredWins.IsConfigured &&
                    ReferenceEquals(registeredWins.Authoring, validAuthoring) &&
                    !ReferenceEquals(registeredWins.Authoring, strayAuthoring),
                    "Unregistered authoring competed with the explicit composition root.");
                completed.Add("registered-authoring-is-sole-authority");

                ResolveResult unavailable = Resolve(neutralRoot);
                Require(unavailable.Succeeded &&
                    !unavailable.IsConfigured &&
                    unavailable.Authoring == null &&
                    unavailable.Diagnostic.Contains(
                        "explicitly unavailable",
                        StringComparison.OrdinalIgnoreCase),
                    "No-registration composition did not expose explicit unavailable behavior.");
                completed.Add("no-registration-is-explicitly-unavailable");

                Require(completed.Count == 9,
                    "Cut 5 provisioning composition smoke case count changed unexpectedly.");
                Debug.Log(
                    "[CUT5_PROVISIONING_COMPOSITION_ROOT_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception resolved = Unwrap(exception);
                Debug.LogError(
                    "[CUT5_PROVISIONING_COMPOSITION_ROOT_SMOKE] " +
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

        private static ResolveResult Resolve(params GameObject[] roots)
        {
            Type runtimeType = ResolveType(RuntimeTypeName);
            ConstructorInfo constructor = ResolveConstructor(runtimeType);
            ParameterInfo[] parameters = constructor.GetParameters();
            object[] arguments = new object[parameters.Length];

            arguments[0] = Enum.ToObject(parameters[0].ParameterType, 0);
            arguments[1] = "Assets/ImmersiveFrameworkQA/Synthetic/Cut5_UIGlobal.unity";
            arguments[2] = "Cut5_UIGlobal";
            arguments[3] = "Cut5 UIGlobal";
            arguments[4] = roots ?? Array.Empty<GameObject>();
            arguments[5] = CreateEmptyListArgument(parameters[5].ParameterType);
            arguments[6] = CreateEmptyListArgument(parameters[6].ParameterType);
            arguments[7] = CreateEmptyListArgument(parameters[7].ParameterType);
            arguments[8] = false;
            arguments[9] = string.Empty;
            arguments[10] = "Cut 5 QA synthetic UIGlobal runtime.";

            object runtime = constructor.Invoke(arguments);
            MethodInfo resolve = runtimeType.GetMethod(
                "TryResolveLocalPlayerProvisioning",
                InstanceAny);
            Require(resolve != null,
                "GlobalUiSceneRuntime provisioning resolver was not found.");

            object[] resolveArguments = { null, false, null };
            bool succeeded = (bool)resolve.Invoke(runtime, resolveArguments);
            return new ResolveResult(
                succeeded,
                resolveArguments[0] as LocalPlayerProvisioningAuthoring,
                (bool)resolveArguments[1],
                resolveArguments[2] as string ?? string.Empty);
        }

        private static ConstructorInfo ResolveConstructor(Type runtimeType)
        {
            ConstructorInfo[] constructors = runtimeType.GetConstructors(InstanceAny);
            for (int index = 0; index < constructors.Length; index++)
            {
                ParameterInfo[] parameters = constructors[index].GetParameters();
                if (parameters.Length == 11 &&
                    parameters[4].ParameterType.IsGenericType)
                {
                    return constructors[index];
                }
            }

            throw new InvalidOperationException(
                "GlobalUiSceneRuntime 11-parameter composition constructor was not found.");
        }

        private static object CreateEmptyListArgument(Type listType)
        {
            Require(listType.IsGenericType,
                $"Expected a generic read-only list type, found '{listType.FullName}'.");
            Type elementType = listType.GetGenericArguments()[0];
            return Array.CreateInstance(elementType, 0);
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

        private static Exception Unwrap(Exception exception)
        {
            if (exception is TargetInvocationException invocation &&
                invocation.InnerException != null)
            {
                return Unwrap(invocation.InnerException);
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

        private readonly struct ResolveResult
        {
            internal ResolveResult(
                bool succeeded,
                LocalPlayerProvisioningAuthoring authoring,
                bool isConfigured,
                string diagnostic)
            {
                Succeeded = succeeded;
                Authoring = authoring;
                IsConfigured = isConfigured;
                Diagnostic = diagnostic ?? string.Empty;
            }

            internal bool Succeeded { get; }
            internal LocalPlayerProvisioningAuthoring Authoring { get; }
            internal bool IsConfigured { get; }
            internal string Diagnostic { get; }
        }
    }
}

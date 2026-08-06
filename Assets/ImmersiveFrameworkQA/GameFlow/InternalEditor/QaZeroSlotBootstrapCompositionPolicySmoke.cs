using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Proves the bootstrap and host composition boundary for applications with no
    /// configured local Player Slots. Zero Slots disables Player runtime composition;
    /// it does not synthesize fallback Player state or weaken configured Player contracts.
    /// </summary>
    public static class QaZeroSlotBootstrapCompositionPolicySmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/Authoring/Run Zero Slot Bootstrap Composition Policy Smoke";
        private const string RuntimeAssemblyName = "Immersive.Framework.Runtime";
        private const string BootstrapTypeName =
            "Immersive.Framework.Bootstrap.ImmersiveFrameworkBootstrap";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string SceneAdmissionModuleTypeName =
            "Immersive.Framework.PlayerParticipation.SceneLocalPlayerAdmissionRuntimeHostModule";
        private const string PolicyMethodName =
            "ShouldComposePlayerParticipationRuntime";
        private const string SceneAdmissionMethodName =
            "ApplySceneLocalPlayerAdmissionRuntime";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            GameApplicationAsset application = null;
            PlayerSlotProfile slotProfile = null;
            GameObject runtimeHostObject = null;

            try
            {
                MethodInfo policy = ResolvePolicyMethod();

                AssertFalse(
                    InvokePolicy(policy, null),
                    "A missing Game Application must not compose Player participation runtime.");
                completed.Add("missing-application-disabled");

                application = ScriptableObject.CreateInstance<GameApplicationAsset>();
                application.name = "QA_ZeroSlot_GameApplication";
                SetLocalPlayerSlots(application, Array.Empty<PlayerSlotProfile>());

                AssertFalse(
                    InvokePolicy(policy, application),
                    "A Game Application with zero Local Player Slots must not compose Player participation runtime.");
                AssertEqual(
                    0,
                    application.LocalPlayerSlotCount,
                    "Synthetic zero-Slot application changed configured capacity.");
                completed.Add("zero-slots-disabled");

                slotProfile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
                slotProfile.name = "QA_Configured_PlayerSlot";
                SetLocalPlayerSlots(application, new[] { slotProfile });

                AssertTrue(
                    InvokePolicy(policy, application),
                    "A Game Application with a configured Local Player Slot must compose Player participation runtime.");
                AssertEqual(
                    1,
                    application.LocalPlayerSlotCount,
                    "Synthetic configured application did not expose one Slot.");
                completed.Add("configured-slots-enabled");

                Type runtimeHostType = ResolveRuntimeType(RuntimeHostTypeName);
                runtimeHostObject =
                    new GameObject("QA Zero Player Runtime Host");
                Component runtimeHost =
                    runtimeHostObject.AddComponent(runtimeHostType);
                MethodInfo sceneAdmissionMethod =
                    ResolveInstanceMethod(
                        runtimeHostType,
                        SceneAdmissionMethodName);

                sceneAdmissionMethod.Invoke(
                    runtimeHost,
                    Array.Empty<object>());
                completed.Add("scene-admission-disabled");

                Type sceneAdmissionModuleType =
                    ResolveRuntimeType(SceneAdmissionModuleTypeName);
                AssertEqual(
                    0,
                    runtimeHost.GetComponents(sceneAdmissionModuleType).Length,
                    "Zero-Player Host unexpectedly received a Scene Local Player admission module.");
                completed.Add("scene-admission-module-absent");

                Debug.Log(
                    "[ZERO_SLOT_BOOTSTRAP_COMPOSITION_POLICY_SMOKE] " +
                    "status='Passed' cases='5' " +
                    "zeroSlots='PlayerRuntimeDisabled' " +
                    "sceneAdmission='NotConfigured' " +
                    "configuredSlots='PlayerRuntimeEnabled' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (TargetInvocationException exception)
            {
                Exception cause = exception.InnerException ?? exception;
                Debug.LogError(
                    "[ZERO_SLOT_BOOTSTRAP_COMPOSITION_POLICY_SMOKE] " +
                    "status='Failed' " +
                    $"exception='{cause.GetType().Name}' " +
                    $"message='{Escape(cause.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[ZERO_SLOT_BOOTSTRAP_COMPOSITION_POLICY_SMOKE] " +
                    "status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (runtimeHostObject != null)
                {
                    Object.DestroyImmediate(runtimeHostObject);
                }

                if (slotProfile != null)
                {
                    Object.DestroyImmediate(slotProfile);
                }

                if (application != null)
                {
                    Object.DestroyImmediate(application);
                }
            }
        }

        private static MethodInfo ResolvePolicyMethod()
        {
            Type bootstrapType = ResolveRuntimeType(BootstrapTypeName);
            MethodInfo method = bootstrapType.GetMethod(
                PolicyMethodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(
                    bootstrapType.FullName,
                    PolicyMethodName);
            }

            return method;
        }

        private static MethodInfo ResolveInstanceMethod(
            Type ownerType,
            string methodName)
        {
            MethodInfo method = ownerType.GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(
                    ownerType.FullName,
                    methodName);
            }

            return method;
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type =
                Type.GetType($"{fullName}, {RuntimeAssemblyName}");
            if (type == null)
            {
                throw new InvalidOperationException(
                    $"Runtime type '{fullName}' was not found.");
            }

            return type;
        }

        private static bool InvokePolicy(
            MethodInfo policy,
            GameApplicationAsset application)
        {
            return (bool)policy.Invoke(
                null,
                new object[] { application });
        }

        private static void SetLocalPlayerSlots(
            GameApplicationAsset application,
            PlayerSlotProfile[] profiles)
        {
            var serialized = new SerializedObject(application);
            SerializedProperty slots =
                serialized.FindProperty("localPlayerSlots");
            if (slots == null)
            {
                throw new MissingFieldException(
                    typeof(GameApplicationAsset).FullName,
                    "localPlayerSlots");
            }

            profiles ??= Array.Empty<PlayerSlotProfile>();
            slots.arraySize = profiles.Length;
            for (int index = 0; index < profiles.Length; index++)
            {
                slots.GetArrayElementAtIndex(index).objectReferenceValue =
                    profiles[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertTrue(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertFalse(
            bool condition,
            string message)
        {
            AssertTrue(!condition, message);
        }

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(
                    expected,
                    actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}

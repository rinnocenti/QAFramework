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
    /// Play Mode integration smoke for the P3F.2 host-scoped Session participation runtime.
    /// It reads the boot-created runtime without mutating Slot state.
    /// </summary>
    public static class QaP3F2RuntimeHostIntegrationSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3F.2 Run Runtime Host Integration Smoke";

        private const string RuntimeAssemblyName = "Immersive.Framework.Runtime";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string RuntimeModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeHostModule";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();

            try
            {
                AssertTrue(EditorApplication.isPlaying, "P3F.2 integration smoke requires Play Mode.");

                Type hostType = ResolveRuntimeType(RuntimeHostTypeName);
                Type moduleType = ResolveRuntimeType(RuntimeModuleTypeName);
                Component runtimeHost = ResolveCurrentRuntimeHost(hostType);
                AssertNotNull(runtimeHost, "FrameworkRuntimeHost was not resolved after boot.");
                completed.Add("runtime-host-resolved");

                Component[] modules = runtimeHost.GetComponents(moduleType);
                AssertEqual(1, modules.Length, "Runtime host must contain exactly one Player participation module.");
                Component module = modules[0];
                completed.Add("single-host-module");

                AssertSame(runtimeHost.gameObject, module.gameObject, "Player participation module is not scoped to the runtime host GameObject.");
                completed.Add("module-shares-host-lifetime");

                GameApplicationAsset gameApplication = ResolveGameApplication(hostType, runtimeHost);
                AssertNotNull(gameApplication, "Runtime host GameApplication could not be resolved.");
                AssertTrue(gameApplication.LocalPlayerSlotCount > 0, "QA GameApplication requires configured Local Player Slots.");
                completed.Add("game-application-resolved");

                PlayerParticipationSnapshot firstSnapshot = ResolveSnapshot(moduleType, module);
                AssertNotNull(firstSnapshot, "Player participation snapshot was not returned.");
                AssertTrue(firstSnapshot.IsInitialized, "Player participation snapshot is not initialized.");
                completed.Add("session-context-initialized");

                AssertEqual(
                    gameApplication.LocalPlayerSlotCount,
                    firstSnapshot.ConfiguredSlotCount,
                    "Runtime roster count does not match GameApplication Local Player Slot count.");
                completed.Add("configured-roster-copied");

                AssertEqual(
                    firstSnapshot.ConfiguredSlotCount,
                    firstSnapshot.DynamicCapacity,
                    "Initial dynamic capacity must equal configured Slot count.");
                completed.Add("initial-capacity-matches-roster");

                AssertTrue(!firstSnapshot.JoiningOpen, "Joining must start explicitly closed.");
                completed.Add("joining-starts-closed");

                AssertEqual(
                    firstSnapshot.ConfiguredSlotCount,
                    firstSnapshot.AvailableCount,
                    "Every configured Slot must start Available.");
                AssertEqual(0, firstSnapshot.ConsumedCapacityCount, "Boot must not reserve or join Slots.");
                completed.Add("slots-start-available");

                for (int index = 0; index < gameApplication.LocalPlayerSlotCount; index++)
                {
                    AssertTrue(
                        gameApplication.TryGetLocalPlayerSlot(index, out PlayerSlotProfile configuredProfile),
                        $"Configured Slot at index '{index}' could not be read.");
                    AssertSame(
                        configuredProfile,
                        firstSnapshot.Slots[index].Profile,
                        $"Runtime Slot order changed at index '{index}'.");
                    AssertEqual(
                        index,
                        firstSnapshot.Slots[index].ConfiguredIndex,
                        $"Runtime configured index changed at index '{index}'.");
                }
                completed.Add("configured-order-preserved");

                PlayerParticipationSnapshot secondSnapshot = ResolveSnapshot(moduleType, module);
                AssertEqual(firstSnapshot.ContextId, secondSnapshot.ContextId, "Repeated access returned another Session context.");
                AssertEqual(firstSnapshot.Revision, secondSnapshot.Revision, "Read-only snapshot access changed runtime revision.");
                completed.Add("snapshot-access-is-stable");

                object runtimeContext = ResolveRuntimeContext(moduleType, module);
                AssertNotNull(runtimeContext, "Host module did not expose its typed runtime context.");
                AssertTrue(!(runtimeContext is UnityEngine.Object), "PlayerParticipationRuntimeContext must remain a plain C# object.");
                completed.Add("domain-context-is-plain-csharp");

                Debug.Log(
                    "[P3F2_RUNTIME_HOST_INTEGRATION_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"context='{firstSnapshot.ContextId}' " +
                    $"slots='{firstSnapshot.ConfiguredSlotCount}' " +
                    $"capacity='{firstSnapshot.DynamicCapacity}' " +
                    $"joiningOpen='{firstSnapshot.JoiningOpen}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3F2_RUNTIME_HOST_INTEGRATION_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return EditorApplication.isPlaying;
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = Type.GetType($"{fullName}, {RuntimeAssemblyName}");
            if (type == null)
            {
                throw new InvalidOperationException($"Runtime type '{fullName}' was not found.");
            }

            return type;
        }

        private static Component ResolveCurrentRuntimeHost(Type hostType)
        {
            MethodInfo tryGetCurrent = hostType.GetMethod(
                "TryGetCurrent",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (tryGetCurrent == null)
            {
                throw new MissingMethodException(hostType.FullName, "TryGetCurrent");
            }

            object[] arguments = { null };
            bool resolved = (bool)tryGetCurrent.Invoke(null, arguments);
            return resolved ? arguments[0] as Component : null;
        }

        private static GameApplicationAsset ResolveGameApplication(Type hostType, Component runtimeHost)
        {
            FieldInfo field = hostType.GetField(
                "_gameApplication",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(hostType.FullName, "_gameApplication");
            }

            return field.GetValue(runtimeHost) as GameApplicationAsset;
        }

        private static PlayerParticipationSnapshot ResolveSnapshot(Type moduleType, Component module)
        {
            MethodInfo method = moduleType.GetMethod(
                "TryGetSnapshot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(moduleType.FullName, "TryGetSnapshot");
            }

            object[] arguments = { null };
            bool resolved = (bool)method.Invoke(module, arguments);
            if (!resolved)
            {
                throw new InvalidOperationException("Player participation module rejected snapshot access.");
            }

            return arguments[0] as PlayerParticipationSnapshot;
        }

        private static object ResolveRuntimeContext(Type moduleType, Component module)
        {
            MethodInfo method = moduleType.GetMethod(
                "TryGetRuntimeContext",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(moduleType.FullName, "TryGetRuntimeContext");
            }

            object[] arguments = { null };
            bool resolved = (bool)method.Invoke(module, arguments);
            return resolved ? arguments[0] : null;
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
                throw new InvalidOperationException($"{message} expected='{expected}' actual='{actual}'.");
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

using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Gate;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    public static class QaH229InputGateRuntimeBindingSmoke
    {
        private const string LogPrefix = "[H229_INPUT_GATE_RUNTIME_BINDING_SMOKE]";
        private const string RuntimeSource = nameof(QaH229InputGateRuntimeBindingSmoke);

        [MenuItem("Immersive Framework/QA/Regressions/Input/Run Input Gate Regression", true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem("Immersive Framework/QA/Regressions/Input/Run Input Gate Regression")]
        public static void Run()
        {
            RunInternal();
        }

        public static void RunInternal()
        {
            var completed = new List<string>();
            var objects = new List<UnityEngine.Object>();
            var fixtures = new List<InputGateFixture>();

            try
            {
                Require(EditorApplication.isPlaying, "H2.2.9 vertical smoke requires Play Mode.");
                Require(
                    global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(out FrameworkRuntimeHost host) && host != null,
                    "H2.2.9 vertical smoke requires FrameworkRuntimeHost.");

                IInputGateRuntimePort hostRuntime = host;
                Require(hostRuntime != null, "FrameworkRuntimeHost did not expose Input Gate runtime port.");
                _ = hostRuntime.CurrentGateSnapshot;
                completed.Add("runtime-port-available");

                RunBindingCompositionCases(objects, fixtures, completed);

                InputGateFixture unbound = InputGateFixture.Create(
                    "H229 Unbound Adapter",
                    includeGameplayMap: true,
                    gameplayInitiallyEnabled: true);
                fixtures.Add(unbound);
                unbound.Adapter.ApplyCurrentGate();
                Require(
                    !unbound.Adapter.HasInputGateRuntimeBinding
                    && !unbound.Adapter.IsBlockedByAdapter
                    && unbound.GameplayMapEnabled
                    && unbound.Adapter.LastStatus == "SkippedMissingInputGateRuntime"
                    && unbound.Adapter.InputGateRuntimeBindingDiagnostic.Contains("not bound"),
                    BuildFixtureDiagnostic(unbound));
                completed.Add("unbound-adapter-does-not-fallback-to-current-host");

                var gameplayPort = new MutableInputGateRuntimePort();
                InputGateFixture gameplay = InputGateFixture.Create(
                    "H229 Gameplay Gate Adapter",
                    includeGameplayMap: true,
                    gameplayInitiallyEnabled: true);
                fixtures.Add(gameplay);
                Require(
                    gameplay.Adapter.TryBindInputGateRuntime(gameplayPort, out string gameplayBindingIssue),
                    gameplayBindingIssue);

                gameplayPort.CurrentGateSnapshot = Snapshot(
                    "qa.h229.gameplay",
                    GateScope.Gameplay,
                    GateDomain.GameplayAction);
                gameplay.Adapter.ApplyCurrentGate();
                Require(
                    gameplay.Adapter.IsBlockedByAdapter
                    && !gameplay.GameplayMapEnabled
                    && gameplay.Adapter.LastStatus == "BlockedActionMap",
                    BuildFixtureDiagnostic(gameplay));
                completed.Add("gameplay-gate-blocks-enabled-action-map");

                gameplayPort.CurrentGateSnapshot = GateSnapshot.Empty();
                gameplay.Adapter.ApplyCurrentGate();
                Require(
                    !gameplay.Adapter.IsBlockedByAdapter
                    && gameplay.GameplayMapEnabled
                    && gameplay.Adapter.LastStatus == "Released",
                    BuildFixtureDiagnostic(gameplay));
                completed.Add("gate-release-restores-previously-enabled-action-map");

                var inputAcceptancePort = new MutableInputGateRuntimePort
                {
                    CurrentGateSnapshot = Snapshot(
                        "qa.h229.input-acceptance",
                        GateScope.Input,
                        GateDomain.InputAcceptance)
                };
                InputGateFixture inputAcceptance = InputGateFixture.Create(
                    "H229 Input Acceptance Gate Adapter",
                    includeGameplayMap: true,
                    gameplayInitiallyEnabled: true);
                fixtures.Add(inputAcceptance);
                Require(
                    inputAcceptance.Adapter.TryBindInputGateRuntime(
                        inputAcceptancePort,
                        out string inputAcceptanceBindingIssue),
                    inputAcceptanceBindingIssue);
                inputAcceptance.Adapter.ApplyCurrentGate();
                Require(
                    inputAcceptance.Adapter.IsBlockedByAdapter
                    && !inputAcceptance.GameplayMapEnabled
                    && inputAcceptance.Adapter.LastStatus == "BlockedActionMap",
                    BuildFixtureDiagnostic(inputAcceptance));
                inputAcceptancePort.CurrentGateSnapshot = GateSnapshot.Empty();
                inputAcceptance.Adapter.ApplyCurrentGate();
                Require(
                    !inputAcceptance.Adapter.IsBlockedByAdapter
                    && inputAcceptance.GameplayMapEnabled,
                    BuildFixtureDiagnostic(inputAcceptance));
                completed.Add("input-acceptance-gate-blocks-and-releases-gameplay-map");

                var unrelatedPort = new MutableInputGateRuntimePort
                {
                    CurrentGateSnapshot = Snapshot(
                        "qa.h229.unrelated",
                        GateScope.Input,
                        GateDomain.UiNavigation)
                };
                InputGateFixture unrelated = InputGateFixture.Create(
                    "H229 Unrelated Gate Adapter",
                    includeGameplayMap: true,
                    gameplayInitiallyEnabled: true);
                fixtures.Add(unrelated);
                Require(
                    unrelated.Adapter.TryBindInputGateRuntime(
                        unrelatedPort,
                        out string unrelatedBindingIssue),
                    unrelatedBindingIssue);
                unrelated.Adapter.ApplyCurrentGate();
                Require(
                    !unrelated.Adapter.IsBlockedByAdapter
                    && unrelated.GameplayMapEnabled
                    && unrelated.Adapter.LastStatus == "Allowed",
                    BuildFixtureDiagnostic(unrelated));
                completed.Add("unrelated-gate-domain-does-not-block-gameplay-map");

                var disabledPort = new MutableInputGateRuntimePort
                {
                    CurrentGateSnapshot = Snapshot(
                        "qa.h229.disabled-baseline",
                        GateScope.Gameplay,
                        GateDomain.GameplayAction)
                };
                InputGateFixture disabledBaseline = InputGateFixture.Create(
                    "H229 Disabled Baseline Adapter",
                    includeGameplayMap: true,
                    gameplayInitiallyEnabled: false);
                fixtures.Add(disabledBaseline);
                Require(
                    disabledBaseline.Adapter.TryBindInputGateRuntime(
                        disabledPort,
                        out string disabledBindingIssue),
                    disabledBindingIssue);
                disabledBaseline.Adapter.ApplyCurrentGate();
                Require(
                    disabledBaseline.Adapter.IsBlockedByAdapter
                    && !disabledBaseline.GameplayMapEnabled,
                    BuildFixtureDiagnostic(disabledBaseline));
                disabledPort.CurrentGateSnapshot = GateSnapshot.Empty();
                disabledBaseline.Adapter.ApplyCurrentGate();
                Require(
                    !disabledBaseline.Adapter.IsBlockedByAdapter
                    && !disabledBaseline.GameplayMapEnabled
                    && disabledBaseline.Adapter.LastStatus == "Released",
                    BuildFixtureDiagnostic(disabledBaseline));
                completed.Add("gate-release-preserves-previously-disabled-action-map");

                var missingMapPort = new MutableInputGateRuntimePort
                {
                    CurrentGateSnapshot = Snapshot(
                        "qa.h229.missing-map",
                        GateScope.Gameplay,
                        GateDomain.GameplayAction)
                };
                InputGateFixture missingMap = InputGateFixture.Create(
                    "H229 Missing Gameplay Map Adapter",
                    includeGameplayMap: false,
                    gameplayInitiallyEnabled: false);
                fixtures.Add(missingMap);
                Require(
                    missingMap.Adapter.TryBindInputGateRuntime(
                        missingMapPort,
                        out string missingMapBindingIssue),
                    missingMapBindingIssue);
                missingMap.Adapter.ApplyCurrentGate();
                Require(
                    !missingMap.Adapter.IsBlockedByAdapter
                    && missingMap.Adapter.LastStatus == "FailedActionMapBlock",
                    BuildFixtureDiagnostic(missingMap));
                completed.Add("missing-gameplay-map-fails-explicitly-without-block-state");

                for (int index = 0; index < fixtures.Count; index++)
                {
                    fixtures[index].Adapter.Restore();
                    Require(
                        !fixtures[index].Adapter.IsBlockedByAdapter,
                        $"Adapter '{fixtures[index].Root.name}' retained blocked state after cleanup.");
                }
                completed.Add("no-adapter-remains-blocked");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{LogPrefix} status='Failed' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                for (int index = fixtures.Count - 1; index >= 0; index--)
                {
                    fixtures[index]?.Dispose();
                }

                for (int index = objects.Count - 1; index >= 0; index--)
                {
                    if (objects[index] != null)
                    {
                        UnityEngine.Object.Destroy(objects[index]);
                    }
                }
            }
        }

        private static void RunBindingCompositionCases(
            ICollection<UnityEngine.Object> objects,
            ICollection<InputGateFixture> fixtures,
            ICollection<string> completed)
        {
            var port = new MutableInputGateRuntimePort();
            GameObject empty = CreateInactiveRoot("H229 Binding Empty", objects);

            UnityPlayerInputGateAdapterBindingResult missingRuntime =
                UnityPlayerInputGateAdapterBinding.TryBind(
                    new[] { empty },
                    null);
            Require(
                !missingRuntime.Succeeded
                && missingRuntime.Status == "RejectedMissingInputGateRuntime"
                && missingRuntime.RootCount == 1
                && missingRuntime.AdapterCount == 0,
                missingRuntime.Message);

            UnityPlayerInputGateAdapterBindingResult absent =
                UnityPlayerInputGateAdapterBinding.TryBind(
                    new[] { empty, empty },
                    port);
            Require(
                absent.Succeeded
                && absent.Status == "OptionalAbsent"
                && absent.RootCount == 1
                && absent.AdapterCount == 0,
                absent.Message);

            InputGateFixture boundFixture = InputGateFixture.Create(
                "H229 Binding Root",
                includeGameplayMap: true,
                gameplayInitiallyEnabled: true);
            fixtures.Add(boundFixture);
            UnityPlayerInputGateAdapterBindingResult bound =
                UnityPlayerInputGateAdapterBinding.TryBind(
                    new[] { boundFixture.Root, boundFixture.Root },
                    port);
            Require(
                bound.Succeeded
                && bound.RootCount == 1
                && bound.AdapterCount == 1
                && bound.BoundCount == 1
                && bound.IdempotentCount == 0
                && bound.RejectedCount == 0
                && boundFixture.Adapter.HasInputGateRuntimeBinding,
                bound.Message);

            GameObject child = CreateInactiveRoot("H229 Binding Child", objects);
            child.transform.SetParent(boundFixture.Root.transform, false);
            UnityPlayerInputGateAdapterBindingResult idempotent =
                UnityPlayerInputGateAdapterBinding.TryBind(
                    new[] { boundFixture.Root, child, boundFixture.Root },
                    port);
            Require(
                idempotent.Succeeded
                && idempotent.RootCount == 2
                && idempotent.AdapterCount == 1
                && idempotent.BoundCount == 0
                && idempotent.IdempotentCount == 1
                && idempotent.RejectedCount == 0,
                idempotent.Message);

            InputGateFixture incompatible = InputGateFixture.Create(
                "H229 Binding Incompatible",
                includeGameplayMap: true,
                gameplayInitiallyEnabled: true);
            fixtures.Add(incompatible);
            Require(
                incompatible.Adapter.TryBindInputGateRuntime(
                    new MutableInputGateRuntimePort(),
                    out string initialIssue),
                initialIssue);
            UnityPlayerInputGateAdapterBindingResult rejected =
                UnityPlayerInputGateAdapterBinding.TryBind(
                    new[] { incompatible.Root },
                    new MutableInputGateRuntimePort());
            Require(
                !rejected.Succeeded
                && rejected.Status == "RejectedAdapterBinding"
                && rejected.AdapterCount == 1
                && rejected.RejectedCount == 1
                && rejected.Message.Contains("different port"),
                rejected.Message);

            completed.Add("explicit-root-binding-optional-idempotent-and-divergent-cases");
        }

        private static GateSnapshot Snapshot(
            string blockerId,
            GateScope scope,
            GateDomain domain)
        {
            return new GateSnapshot(
                new[]
                {
                    GateBlocker.ForAnyOwner(
                        blockerId,
                        scope,
                        domain,
                        RuntimeSource,
                        "qa-gate",
                        "H2.2.9")
                });
        }

        private static GameObject CreateInactiveRoot(
            string name,
            ICollection<UnityEngine.Object> objects)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            objects.Add(root);
            return root;
        }

        private static string BuildFixtureDiagnostic(InputGateFixture fixture)
        {
            return
                $"fixture='{fixture.Root.name}' bound='{fixture.Adapter.HasInputGateRuntimeBinding}' " +
                $"blocked='{fixture.Adapter.IsBlockedByAdapter}' status='{fixture.Adapter.LastStatus}' " +
                $"gameplayMap='{fixture.GameplayMapStatus}' " +
                $"binding='{fixture.Adapter.InputGateRuntimeBindingDiagnostic}'.";
        }

        private static void Require(bool value, string message)
        {
            if (!value)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class MutableInputGateRuntimePort : IInputGateRuntimePort
        {
            public GateSnapshot CurrentGateSnapshot { get; set; } =
                GateSnapshot.Empty();
        }

        private sealed class InputGateFixture : IDisposable
        {
            private const string InputSystemAssemblyName = "Unity.InputSystem";
            private const string PlayerInputTypeName =
                "UnityEngine.InputSystem.PlayerInput";
            private const string InputActionAssetTypeName =
                "UnityEngine.InputSystem.InputActionAsset";
            private const string InputActionSetupExtensionsTypeName =
                "UnityEngine.InputSystem.InputActionSetupExtensions";

            private InputGateFixture(
                GameObject root,
                Component playerInput,
                UnityPlayerInputGateAdapter adapter,
                ScriptableObject actions,
                object gameplayMap)
            {
                Root = root;
                PlayerInput = playerInput;
                Adapter = adapter;
                Actions = actions;
                GameplayMap = gameplayMap;
            }

            internal GameObject Root { get; }

            internal Component PlayerInput { get; }

            internal UnityPlayerInputGateAdapter Adapter { get; }

            internal ScriptableObject Actions { get; }

            internal object GameplayMap { get; }

            internal bool GameplayMapEnabled =>
                GameplayMap != null && ReadBooleanProperty(
                    GameplayMap,
                    "enabled");

            internal string GameplayMapStatus =>
                GameplayMap == null
                    ? "missing"
                    : GameplayMapEnabled.ToString();

            internal static InputGateFixture Create(
                string name,
                bool includeGameplayMap,
                bool gameplayInitiallyEnabled)
            {
                Type playerInputType = RequireInputSystemType(
                    PlayerInputTypeName);
                Type inputActionAssetType = RequireInputSystemType(
                    InputActionAssetTypeName);
                Type inputActionSetupExtensionsType = RequireInputSystemType(
                    InputActionSetupExtensionsTypeName);

                var root = new GameObject(name);
                root.SetActive(false);

                ScriptableObject actions = ScriptableObject.CreateInstance(
                    inputActionAssetType);
                Require(
                    actions != null,
                    "H2.2.9 fixture failed to create InputActionAsset.");
                actions.name = name + " Actions";

                object gameplayMap = null;
                if (includeGameplayMap)
                {
                    gameplayMap = InvokeExtensionWithOptionalArguments(
                        inputActionSetupExtensionsType,
                        actions,
                        "AddActionMap",
                        "Player");
                    Require(
                        gameplayMap != null,
                        "H2.2.9 fixture failed to create Player action map.");
                    InvokeExtensionWithOptionalArguments(
                        inputActionSetupExtensionsType,
                        gameplayMap,
                        "AddAction",
                        "Move");
                }

                object uiMap = InvokeExtensionWithOptionalArguments(
                    inputActionSetupExtensionsType,
                    actions,
                    "AddActionMap",
                    "UI");
                Require(
                    uiMap != null,
                    "H2.2.9 fixture failed to create UI action map.");
                InvokeExtensionWithOptionalArguments(
                    inputActionSetupExtensionsType,
                    uiMap,
                    "AddAction",
                    "Navigate");

                Component playerInput = root.AddComponent(playerInputType);
                Require(
                    playerInput != null,
                    "H2.2.9 fixture failed to create PlayerInput.");
                WriteProperty(
                    playerInput,
                    "actions",
                    actions);

                object resolvedGameplayMap = includeGameplayMap
                    ? FindActionMap(
                        ReadProperty(playerInput, "actions"),
                        "Player",
                        throwIfNotFound: true)
                    : null;
                if (resolvedGameplayMap != null)
                {
                    InvokeNoArguments(
                        resolvedGameplayMap,
                        gameplayInitiallyEnabled
                            ? "Enable"
                            : "Disable");
                }

                UnityPlayerInputGateAdapter adapter =
                    root.AddComponent<UnityPlayerInputGateAdapter>();
                return new InputGateFixture(
                    root,
                    playerInput,
                    adapter,
                    actions,
                    resolvedGameplayMap);
            }

            public void Dispose()
            {
                if (Adapter != null)
                {
                    Adapter.Restore();
                }

                if (Root != null)
                {
                    UnityEngine.Object.Destroy(Root);
                }

                if (Actions != null)
                {
                    UnityEngine.Object.Destroy(Actions);
                }
            }

            private static Type RequireInputSystemType(
                string fullName)
            {
                Type type = Type.GetType(
                    fullName + ", " + InputSystemAssemblyName,
                    throwOnError: false);
                if (type != null)
                {
                    return type;
                }

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int index = 0; index < assemblies.Length; index++)
                {
                    type = assemblies[index].GetType(
                        fullName,
                        throwOnError: false);
                    if (type != null)
                    {
                        return type;
                    }
                }

                throw new InvalidOperationException(
                    $"H2.2.9 fixture requires Input System type '{fullName}'.");
            }

            private static object FindActionMap(
                object actionAsset,
                string actionMapName,
                bool throwIfNotFound)
            {
                Require(
                    actionAsset != null,
                    "H2.2.9 fixture requires InputActionAsset evidence.");
                MethodInfo method = actionAsset.GetType().GetMethod(
                    "FindActionMap",
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: new[] { typeof(string), typeof(bool) },
                    modifiers: null);
                Require(
                    method != null,
                    "H2.2.9 fixture could not resolve InputActionAsset.FindActionMap(string, bool).");
                return method.Invoke(
                    actionAsset,
                    new object[] { actionMapName, throwIfNotFound });
            }

            private static object InvokeExtensionWithOptionalArguments(
                Type extensionType,
                object target,
                string methodName,
                string firstArgument)
            {
                Require(
                    extensionType != null,
                    $"H2.2.9 fixture requires an extension type for '{methodName}'.");
                Require(
                    target != null,
                    $"H2.2.9 fixture requires a target for '{methodName}'.");

                MethodInfo method = FindExtensionMethod(
                    extensionType,
                    target.GetType(),
                    methodName);
                ParameterInfo[] parameters = method.GetParameters();
                var arguments = new object[parameters.Length];
                arguments[0] = target;
                arguments[1] = firstArgument;
                for (int index = 2; index < arguments.Length; index++)
                {
                    arguments[index] = parameters[index].HasDefaultValue
                        ? parameters[index].DefaultValue
                        : Type.Missing;
                }

                return method.Invoke(null, arguments);
            }

            private static MethodInfo FindExtensionMethod(
                Type extensionType,
                Type targetType,
                string methodName)
            {
                MethodInfo[] methods = extensionType.GetMethods(
                    BindingFlags.Static | BindingFlags.Public);
                for (int index = 0; index < methods.Length; index++)
                {
                    MethodInfo candidate = methods[index];
                    if (candidate.Name != methodName)
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = candidate.GetParameters();
                    if (parameters.Length < 2 ||
                        !parameters[0].ParameterType.IsAssignableFrom(targetType) ||
                        parameters[1].ParameterType != typeof(string))
                    {
                        continue;
                    }

                    bool supported = true;
                    for (int parameterIndex = 2;
                         parameterIndex < parameters.Length;
                         parameterIndex++)
                    {
                        if (!parameters[parameterIndex].IsOptional)
                        {
                            supported = false;
                            break;
                        }
                    }

                    if (supported)
                    {
                        return candidate;
                    }
                }

                throw new InvalidOperationException(
                    $"H2.2.9 fixture could not resolve extension '{extensionType.FullName}.{methodName}' for target '{targetType.FullName}' and one required string argument.");
            }

            private static void InvokeNoArguments(
                object target,
                string methodName)
            {
                Require(
                    target != null,
                    $"H2.2.9 fixture requires a target for '{methodName}'.");
                MethodInfo method = target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                Require(
                    method != null,
                    $"H2.2.9 fixture could not resolve '{target.GetType().FullName}.{methodName}()'.");
                method.Invoke(target, Array.Empty<object>());
            }

            private static object ReadProperty(
                object target,
                string propertyName)
            {
                Require(
                    target != null,
                    $"H2.2.9 fixture requires a target property '{propertyName}'.");
                PropertyInfo property = target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public);
                Require(
                    property != null && property.CanRead,
                    $"H2.2.9 fixture could not read '{target.GetType().FullName}.{propertyName}'.");
                return property.GetValue(target);
            }

            private static bool ReadBooleanProperty(
                object target,
                string propertyName)
            {
                object value = ReadProperty(target, propertyName);
                return value is bool boolean && boolean;
            }

            private static void WriteProperty(
                object target,
                string propertyName,
                object value)
            {
                Require(
                    target != null,
                    $"H2.2.9 fixture requires a target property '{propertyName}'.");
                PropertyInfo property = target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance | BindingFlags.Public);
                Require(
                    property != null && property.CanWrite,
                    $"H2.2.9 fixture could not write '{target.GetType().FullName}.{propertyName}'.");
                property.SetValue(target, value);
            }
        }
    }
}

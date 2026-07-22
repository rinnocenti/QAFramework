using System;
using System.IO;
using System.Linq;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using ImmersiveFrameworkQA.Hub;
using ImmersiveFrameworkQA.PauseP1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.PauseP1.Editor
{
    internal readonly struct QaPauseProductBindingValidation
    {
        internal QaPauseProductBindingValidation(
            int pauseBindingCount,
            int runtimeEvidenceCount,
            int playerInputsInPauseScenes,
            int hubEntryCount,
            int pauseRequestTriggerCount,
            int preflightPauseReferenceCount,
            int duplicateCount)
        {
            PauseBindingCount = pauseBindingCount;
            RuntimeEvidenceCount = runtimeEvidenceCount;
            PlayerInputsInPauseScenes = playerInputsInPauseScenes;
            HubEntryCount = hubEntryCount;
            PauseRequestTriggerCount = pauseRequestTriggerCount;
            PreflightPauseReferenceCount = preflightPauseReferenceCount;
            DuplicateCount = duplicateCount;
        }

        internal int PauseBindingCount { get; }
        internal int RuntimeEvidenceCount { get; }
        internal int PlayerInputsInPauseScenes { get; }
        internal int HubEntryCount { get; }
        internal int PauseRequestTriggerCount { get; }
        internal int PreflightPauseReferenceCount { get; }
        internal int DuplicateCount { get; }
    }

    internal static class QaPauseProductBindingStaticValidator
    {
        internal static QaPauseProductBindingValidation Validate(
            InputActionAsset actionAsset,
            InputAction pauseAction,
            InputActionReference pauseReference,
            RouteAsset route,
            ActivityAsset activity,
            ActivityContentProfileAsset content)
        {
            ValidateActions(actionAsset, pauseAction, pauseReference);
            int duplicates = 0;
            int pauseBindings = ValidatePlayerHost(
                pauseAction,
                pauseReference,
                ref duplicates,
                out int runtimeEvidenceCount);
            ValidateOfficialPlayerPreflight(
                ref duplicates,
                out int pauseRequestTriggerCount,
                out int preflightPauseReferenceCount);
            ValidateAssets(route, activity, content);

            int scenePlayerInputs = 0;
            ValidateRouteScene(ref scenePlayerInputs);
            ValidateActivityScene(ref scenePlayerInputs);
            Require(scenePlayerInputs == 0,
                "Pause Route and Activity scenes must contain zero PlayerInput components.");

            ValidateBuildProfile();
            int hubEntries = ValidateHub(ref duplicates);
            ValidateNoPublicPauseRegressionMenu();
            Require(duplicates == 0,
                $"Pause Product Binding QA rejects duplicates; found '{duplicates}'.");

            return new QaPauseProductBindingValidation(
                pauseBindings,
                runtimeEvidenceCount,
                scenePlayerInputs,
                hubEntries,
                pauseRequestTriggerCount,
                preflightPauseReferenceCount,
                duplicates);
        }

        private static void ValidateActions(
            InputActionAsset asset,
            InputAction pauseAction,
            InputActionReference pauseReference)
        {
            Require(asset != null && pauseAction != null,
                "Official action asset and Pause action are required.");
            Require(asset.actionMaps.Count(map =>
                    string.Equals(map.name, "Global", StringComparison.Ordinal)) == 1,
                "Exactly one Global action map is required.");
            Require(asset.actionMaps.Count(map =>
                    string.Equals(map.name, "Gameplay", StringComparison.Ordinal)) == 1,
                "Exactly one Gameplay action map is required.");
            Require(asset.actionMaps.Count(map =>
                    string.Equals(map.name, "UI", StringComparison.Ordinal)) == 1,
                "Exactly one UI action map is required.");
            Require(asset.FindActionMap("Gameplay", true)
                    .FindAction("JoinEvidence", false) != null,
                "Gameplay/JoinEvidence was not preserved.");
            Require(string.Equals(
                    pauseAction.actionMap?.name,
                    "Global",
                    StringComparison.Ordinal) &&
                string.Equals(
                    pauseAction.name,
                    "PauseToggle",
                    StringComparison.Ordinal),
                "InputActionReference target must be Global/PauseToggle.");
            Require(CountBinding(pauseAction, "<Keyboard>/escape") == 1,
                "Global/PauseToggle requires exactly one Keyboard Escape binding.");
            Require(CountBinding(pauseAction, "<Gamepad>/start") == 1,
                "Global/PauseToggle requires exactly one Gamepad Start binding.");
            Require(pauseReference != null &&
                    pauseReference.action != null &&
                    pauseReference.action.id == pauseAction.id &&
                    string.Equals(
                        AssetDatabase.GetAssetPath(pauseReference),
                        QaPauseProductBindingPaths.ActionReference,
                        StringComparison.Ordinal),
                "The persisted InputActionReference must resolve Global/PauseToggle by GUID.");
        }

        private static int CountBinding(InputAction action, string path) =>
            action.bindings.Count(binding =>
                string.Equals(
                    binding.path,
                    path,
                    StringComparison.OrdinalIgnoreCase));

        private static int ValidatePlayerHost(
            InputAction expectedPauseAction,
            InputActionReference expectedReference,
            ref int duplicateCount,
            out int runtimeEvidenceCount)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                QaPauseProductBindingPaths.PlayerHost);
            try
            {
                PlayerInput[] inputs =
                    root.GetComponentsInChildren<PlayerInput>(true);
                UnityPlayerInputGateAdapter[] gates =
                    root.GetComponentsInChildren<UnityPlayerInputGateAdapter>(true);
                PausePlayerInputBinding[] bindings =
                    root.GetComponentsInChildren<PausePlayerInputBinding>(true);
                PauseRuntimeEvidencePanel[] evidencePanels =
                    root.GetComponentsInChildren<
                        PauseRuntimeEvidencePanel>(true);

                duplicateCount += Math.Max(0, inputs.Length - 1);
                duplicateCount += Math.Max(0, gates.Length - 1);
                duplicateCount += Math.Max(0, bindings.Length - 1);
                duplicateCount += Math.Max(
                    0,
                    evidencePanels.Length - 1);

                Require(inputs.Length == 1,
                    $"Official host requires exactly one PlayerInput; found '{inputs.Length}'.");
                Require(gates.Length == 1,
                    $"Official host requires exactly one UnityPlayerInputGateAdapter; found '{gates.Length}'.");
                Require(bindings.Length == 1,
                    $"Official host requires exactly one PausePlayerInputBinding; found '{bindings.Length}'.");
                Require(evidencePanels.Length == 1,
                    $"Official host requires exactly one PauseRuntimeEvidencePanel; found '{evidencePanels.Length}'.");

                PlayerInput input = inputs[0];
                UnityPlayerInputGateAdapter gate = gates[0];
                PausePlayerInputBinding binding = bindings[0];
                PauseRuntimeEvidencePanel evidencePanel = evidencePanels[0];
                Require(ReferenceEquals(gate.gameObject, input.gameObject) &&
                        ReferenceEquals(binding.gameObject, input.gameObject) &&
                        ReferenceEquals(
                            evidencePanel.gameObject,
                            input.gameObject),
                    "PlayerInput, Input Gate, Pause binding and runtime evidence must share the same host GameObject.");
                Require(ExplicitObject(gate, "playerInput") == input &&
                        ExplicitObject(binding, "playerInput") == input,
                    "Input Gate and Pause binding must point explicitly to the same PlayerInput; global auto-resolution is not accepted.");
                Require(
                    ExplicitObject(evidencePanel, "playerInput") == input &&
                    ReferenceEquals(evidencePanel.PlayerInput, input),
                    "Pause runtime evidence must point explicitly to the official PlayerInput.");
                Require(
                    ExplicitObject(evidencePanel, "pauseBinding") == binding &&
                    ReferenceEquals(evidencePanel.PauseBinding, binding),
                    "Pause runtime evidence must point explicitly to the official PausePlayerInputBinding.");
                Require(input.actions != null &&
                        string.Equals(
                            AssetDatabase.GetAssetPath(input.actions),
                            QaPauseProductBindingPaths.InputActions,
                            StringComparison.Ordinal),
                    "Official PlayerInput must use P3G4_InputActions.");
                Require(string.Equals(input.defaultActionMap, "Gameplay", StringComparison.Ordinal),
                    "Official PlayerInput default action map must be Gameplay.");
                Require(ExplicitObject(binding, "pauseAction") == expectedReference &&
                        binding.PauseAction != null &&
                        binding.PauseAction.action != null &&
                        binding.PauseAction.action.id == expectedPauseAction.id,
                    "Pause binding must point to the dedicated Global/PauseToggle reference.");
                Require(string.Equals(binding.GlobalActionMapName, "Global", StringComparison.Ordinal) &&
                        string.Equals(binding.GameplayActionMapName, "Gameplay", StringComparison.Ordinal) &&
                        string.Equals(binding.UiActionMapName, "UI", StringComparison.Ordinal),
                    "Pause binding action-map names are invalid.");
                Require(string.Equals(gate.GameplayActionMapName, "Gameplay", StringComparison.Ordinal),
                    "Input Gate must target the Gameplay action map.");
                runtimeEvidenceCount = evidencePanels.Length;
                return bindings.Length;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static UnityEngine.Object ExplicitObject(
            UnityEngine.Object target,
            string propertyName)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null,
                $"Missing serialized property '{target.GetType().Name}.{propertyName}'.");
            return property.objectReferenceValue;
        }

        private static void ValidateOfficialPlayerPreflight(
            ref int duplicateCount,
            out int pauseRequestTriggerCount,
            out int preflightPauseReferenceCount)
        {
            int localDuplicateCount = duplicateCount;
            int localPauseRequestTriggerCount = 0;
            int localPreflightPauseReferenceCount = 0;
            WithScene(
                QaPauseProductBindingPaths.UiGlobalScene,
                scene =>
                {
                    Component[] components = Components(scene);
                    LocalPlayerProvisioningAuthoring[] authorings =
                        components
                            .OfType<LocalPlayerProvisioningAuthoring>()
                            .ToArray();
                    PlayerInputManager[] managers =
                        components
                            .OfType<PlayerInputManager>()
                            .ToArray();
                    PauseOfficialPlayerPreflightPanel[] preflights =
                        components
                            .OfType<PauseOfficialPlayerPreflightPanel>()
                            .ToArray();
                    PauseRequestTrigger[] pauseRequestTriggers =
                        components
                            .OfType<PauseRequestTrigger>()
                            .ToArray();

                    localDuplicateCount += Math.Max(
                        0,
                        authorings.Length - 1);
                    localDuplicateCount += Math.Max(
                        0,
                        managers.Length - 1);
                    localDuplicateCount += Math.Max(
                        0,
                        preflights.Length - 1);
                    localDuplicateCount += Math.Max(
                        0,
                        pauseRequestTriggers.Length - 1);

                    Require(authorings.Length == 1,
                        $"QA_UIGlobal requires exactly one LocalPlayerProvisioningAuthoring; found '{authorings.Length}'.");
                    Require(managers.Length == 1,
                        $"QA_UIGlobal requires exactly one PlayerInputManager; found '{managers.Length}'.");
                    Require(preflights.Length == 1,
                        $"QA_UIGlobal requires exactly one Pause official Player preflight; found '{preflights.Length}'.");
                    Require(pauseRequestTriggers.Length == 1,
                        $"QA_UIGlobal requires exactly one official PauseRequestTrigger; found '{pauseRequestTriggers.Length}'.");
                    Require(ReferenceEquals(
                            authorings[0].PlayerInputManager,
                            managers[0]),
                        "QA_UIGlobal provisioning must own the existing PlayerInputManager.");
                    Require(ReferenceEquals(
                            ExplicitObject(
                                preflights[0],
                                "provisioningAuthoring"),
                            authorings[0]),
                        "Pause preflight requires an explicit serialized reference to the official LocalPlayerProvisioningAuthoring.");
                    Require(ReferenceEquals(
                            preflights[0].ProvisioningAuthoring,
                            authorings[0]),
                        "Pause preflight public topology does not expose the official provisioning reference.");
                    bool serializedPauseReference = ReferenceEquals(
                        ExplicitObject(
                            preflights[0],
                            "pauseRequestTrigger"),
                        pauseRequestTriggers[0]);
                    bool publicPauseReference = ReferenceEquals(
                        preflights[0].PauseRequestTrigger,
                        pauseRequestTriggers[0]);
                    localPauseRequestTriggerCount =
                        pauseRequestTriggers.Length;
                    localPreflightPauseReferenceCount =
                        serializedPauseReference && publicPauseReference
                            ? 1
                            : 0;
                    Require(serializedPauseReference,
                        "Pause preflight requires an explicit serialized reference to the official PauseRequestTrigger.");
                    Require(publicPauseReference,
                        "Pause preflight public topology does not expose the official PauseRequestTrigger reference.");
                });
            duplicateCount = localDuplicateCount;
            pauseRequestTriggerCount =
                localPauseRequestTriggerCount;
            preflightPauseReferenceCount =
                localPreflightPauseReferenceCount;
        }

        private static void ValidateAssets(
            RouteAsset route,
            ActivityAsset activity,
            ActivityContentProfileAsset content)
        {
            Require(route != null && activity != null && content != null,
                "Route, Activity and Activity Content assets are required.");
            Require(string.Equals(
                    route.PrimaryScenePath,
                    QaPauseProductBindingPaths.RouteScene,
                    StringComparison.Ordinal) &&
                ReferenceEquals(route.StartupActivity, activity),
                "Route primary scene or startup Activity reference is invalid.");
            Require(ReferenceEquals(activity.ActivityContentProfile, content) &&
                    activity.PlayerParticipationProjectionProfile != null &&
                    activity.PlayerParticipationRequirementsProfile != null,
                "Activity content or Local Player participation references are invalid.");

            var serialized = new SerializedObject(content);
            SerializedProperty scenes = serialized.FindProperty("scenes");
            Require(scenes != null && scenes.arraySize == 1,
                "Activity Content requires exactly one scene entry.");
            SerializedProperty entry = scenes.GetArrayElementAtIndex(0);
            Require(string.Equals(
                    entry.FindPropertyRelative("scenePath").stringValue,
                    QaPauseProductBindingPaths.ActivityScene,
                    StringComparison.Ordinal),
                "Activity Content scene reference is invalid.");
        }

        private static void ValidateRouteScene(ref int playerInputCount)
        {
            int localPlayerInputCount = playerInputCount;
            WithScene(
                QaPauseProductBindingPaths.RouteScene,
                scene =>
                {
                    Component[] components = Components(scene);
                    localPlayerInputCount += components.OfType<PlayerInput>().Count();
                    RouteRequestTrigger[] hubReturns = components
                        .OfType<RouteRequestTrigger>()
                        .Where(trigger =>
                            trigger.TargetRoute != null &&
                            string.Equals(
                                AssetDatabase.GetAssetPath(trigger.TargetRoute),
                                QaPauseProductBindingPaths.HubRoute,
                                StringComparison.Ordinal))
                        .ToArray();
                    Require(hubReturns.Length == 1,
                        "Pause Route scene requires exactly one explicit return to QA Hub Route.");
                    Require(components.OfType<QaHubReturnPanel>().Count() == 1,
                        "Pause Route scene requires exactly one visual return panel.");
                    RejectForbidden(components);
                });
            playerInputCount = localPlayerInputCount;
        }

        private static void ValidateActivityScene(ref int playerInputCount)
        {
            int localPlayerInputCount = playerInputCount;
            WithScene(
                QaPauseProductBindingPaths.ActivityScene,
                scene =>
                {
                    Component[] components = Components(scene);
                    localPlayerInputCount += components.OfType<PlayerInput>().Count();
                    PauseActivityBindingAuthoring[] authorings =
                        components
                            .OfType<PauseActivityBindingAuthoring>()
                            .ToArray();
                    Require(authorings.Length == 1,
                        "Pause Activity scene requires exactly one PauseActivityBindingAuthoring.");
                    Require(
                        authorings[0].Requiredness ==
                            PauseActivityBindingRequiredness.Required,
                        "Pause Activity authoring Requiredness must be Required.");
                    Require(
                        authorings[0].TryCreateIntent(
                            out PauseActivityBindingIntent intent,
                            out string intentDiagnostic),
                        $"Pause Activity authoring failed to create intent. {intentDiagnostic}");
                    Require(
                        intent.IsValid &&
                        intent.IsRequired &&
                        intent.Requiredness ==
                            PauseActivityBindingRequiredness.Required,
                        $"Pause Activity intent must be valid and Required. {intentDiagnostic}");
                    Require(components.OfType<PauseQaIntentPanel>().Count() == 1,
                        "Pause Activity scene requires exactly one designer-first Pause intent panel.");
                    RejectForbidden(components);
                });
            playerInputCount = localPlayerInputCount;
        }

        private static void RejectForbidden(Component[] components)
        {
            string[] forbidden =
            {
                nameof(PlayerInput),
                nameof(PlayerInputManager),
                nameof(LocalPlayerProvisioningAuthoring),
                "QaFakePauseRuntime",
                "QaFakePauseRuntimePort",
                "PauseProductBindingRuntimeContext",
                "QaFakePauseProductBindingRuntimeContext"
            };
            foreach (Component component in components.Where(value => value != null))
            {
                string typeName = component.GetType().Name;
                Require(!forbidden.Contains(typeName, StringComparer.Ordinal),
                    $"Pause scenes cannot contain '{typeName}'.");
            }
        }

        private static void ValidateBuildProfile()
        {
            string[] expected =
            {
                QaPauseProductBindingPaths.RouteScene,
                QaPauseProductBindingPaths.ActivityScene
            };
            foreach (string path in expected)
            {
                int count = EditorBuildSettings.scenes.Count(scene =>
                    scene.enabled &&
                    string.Equals(
                        scene.path.Replace(Path.DirectorySeparatorChar, '/'),
                        path,
                        StringComparison.OrdinalIgnoreCase));
                Require(count == 1,
                    $"Build Profile requires exactly one enabled '{path}' entry; found '{count}'.");
            }
        }

        private static int ValidateHub(ref int duplicateCount)
        {
            int matches = 0;
            WithScene(
                QaPauseProductBindingPaths.HubScene,
                scene =>
                {
                    QaHubPanel[] panels = Components(scene)
                        .OfType<QaHubPanel>()
                        .ToArray();
                    Require(panels.Length == 1,
                        "QA Hub requires exactly one QaHubPanel.");
                    var serialized = new SerializedObject(panels[0]);
                    SerializedProperty entries = serialized.FindProperty("entries");
                    Require(entries != null, "QA Hub entries are unavailable.");
                    for (int index = 0; index < entries.arraySize; index++)
                    {
                        SerializedProperty entry =
                            entries.GetArrayElementAtIndex(index);
                        string domain =
                            entry.FindPropertyRelative("domain").stringValue;
                        string label =
                            entry.FindPropertyRelative("label").stringValue;
                        if (!string.Equals(domain, "Pause", StringComparison.Ordinal) ||
                            !string.Equals(
                                label,
                                "Pause Product Binding QA",
                                StringComparison.Ordinal))
                        {
                            continue;
                        }

                        matches++;
                        RouteRequestTrigger trigger = entry
                            .FindPropertyRelative("routeRequestTrigger")
                            .objectReferenceValue as RouteRequestTrigger;
                        Require(trigger != null &&
                                trigger.TargetRoute != null &&
                                string.Equals(
                                    AssetDatabase.GetAssetPath(trigger.TargetRoute),
                                    QaPauseProductBindingPaths.Route,
                                    StringComparison.Ordinal),
                            "QA Hub Pause entry must navigate through the official Pause Route.");
                    }
                });

            duplicateCount += Math.Max(0, matches - 1);
            Require(matches == 1,
                $"QA Hub requires exactly one Pause Product Binding QA entry; found '{matches}'.");
            return matches;
        }

        private static void ValidateNoPublicPauseRegressionMenu()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:MonoScript",
                new[] { "Assets/ImmersiveFrameworkQA/Player" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (string line in File.ReadLines(path))
                {
                    if (line.Contains("MenuItem", StringComparison.Ordinal) &&
                        line.Contains("Regressions", StringComparison.Ordinal) &&
                        line.Contains("Pause", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            $"A public Pause regression menu is out of scope: '{path}'.");
                    }
                }
            }
        }

        private static void WithScene(string path, Action<Scene> action)
        {
            Scene existing = SceneManager.GetSceneByPath(path);
            bool opened = !existing.IsValid() || !existing.isLoaded;
            Scene scene = opened
                ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive)
                : existing;
            try
            {
                action(scene);
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static Component[] Components(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Component>(true))
                .Where(component => component != null)
                .ToArray();

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Edit Mode regression for the P0 Pause/Input/Gate authoring composition.
    /// Proves that UnityPlayerInputGateAdapter is the single PlayerInput/gameplay-map
    /// authority and PlayerPauseInput only authors the Pause action.
    /// </summary>
    internal static class QaPauseInputGateCompositionRegression
    {
        private const string Prefix = "[P0_PAUSE_INPUT_GATE_COMPOSITION]";
        private const int ExpectedCaseCount = 8;

        private static readonly string[] ExpectedCases =
        {
            "pause-authors-only-pause-action",
            "gate-owns-playerinput-and-gameplay-map",
            "gate-valid-without-pause",
            "pause-derives-gate-authority",
            "pause-rejects-missing-gate",
            "gate-authoring-prevents-duplicate-adapters",
            "gameplay-map-resolution-does-not-fallback-by-name",
            "gate-restore-remains-idempotent"
        };

        [MenuItem("Immersive Framework/QA/Player/Pause/Run Pause Input Gate Composition")]
        internal static void Run()
        {
            var created = new List<UnityEngine.Object>();
            var completed = new List<string>();

            try
            {
                Require(
                    !EditorApplication.isPlayingOrWillChangePlaymode,
                    "Pause/Input/Gate composition regression must run in Edit Mode.");

                RunPauseAuthoringSurfaceCase(created);
                completed.Add(ExpectedCases[0]);

                RunGateAuthoritySurfaceCase(created);
                completed.Add(ExpectedCases[1]);

                RunGateWithoutPauseCase(created);
                completed.Add(ExpectedCases[2]);

                RunPauseDerivesGateAuthorityCase(created);
                completed.Add(ExpectedCases[3]);

                RunMissingGateCase(created);
                completed.Add(ExpectedCases[4]);

                RunSingleGateAuthoringCase();
                completed.Add(ExpectedCases[5]);

                RunGuidIdentityCase(created);
                completed.Add(ExpectedCases[6]);

                RunRestoreIdempotenceCase(created);
                completed.Add(ExpectedCases[7]);

                Require(
                    completed.Count == ExpectedCaseCount,
                    "Pause/Input/Gate composition case count changed unexpectedly.");

                Debug.Log(
                    $"{Prefix} status='Passed' verdict='StaticContractComplete' " +
                    $"cases='{completed.Count}/{ExpectedCaseCount}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' verdict='StaticContractFailed' " +
                    $"cases='{completed.Count}/{ExpectedCaseCount}' " +
                    $"next='{NextCase(completed)}' " +
                    $"completed='{string.Join(",", completed)}' " +
                    $"missing='{Escape(exception.Message)}'.");
                throw;
            }
            finally
            {
                DestroyCreated(created);
            }
        }

        private static void RunPauseAuthoringSurfaceCase(
            ICollection<UnityEngine.Object> created)
        {
            GameObject host = CreateInactiveHost(created, "QA Pause Surface");
            PlayerPauseInput pauseInput = host.AddComponent<PlayerPauseInput>();
            var serialized = new SerializedObject(pauseInput);

            Require(
                serialized.FindProperty("pauseAction") != null,
                "PlayerPauseInput must author Pause Action.");
            Require(
                serialized.FindProperty("playerInput") == null,
                "PlayerPauseInput must not serialize a duplicate PlayerInput authority.");
            Require(
                serialized.FindProperty("gameplayActionMap") == null,
                "PlayerPauseInput must not serialize a duplicate Gameplay Action Map authority.");
            Require(
                serialized.FindProperty("gameplayActionMapName") == null,
                "PlayerPauseInput must not retain the legacy Gameplay Action Map name authority.");
        }

        private static void RunGateAuthoritySurfaceCase(
            ICollection<UnityEngine.Object> created)
        {
            GameObject host = CreateInactiveHost(created, "QA Gate Authority Surface");
            UnityPlayerInputGateAdapter gate = host.AddComponent<UnityPlayerInputGateAdapter>();
            var serialized = new SerializedObject(gate);

            Require(
                serialized.FindProperty("playerInput") != null,
                "UnityPlayerInputGateAdapter must serialize the PlayerInput target authority.");
            Require(
                serialized.FindProperty("gameplayActionMap") != null,
                "UnityPlayerInputGateAdapter must serialize the Gameplay Action Map authority.");
            Require(
                serialized.FindProperty("restorePreviousState") != null,
                "UnityPlayerInputGateAdapter must retain explicit restore-state authoring.");
        }

        private static void RunGateWithoutPauseCase(
            ICollection<UnityEngine.Object> created)
        {
            ActionFixture fixture = CreateActionFixture(created, "QA Gate Only Actions");
            GameObject host = CreateInactiveHost(created, "QA Gate Only Host");
            PlayerInput playerInput = ConfigurePlayerInput(host, fixture.Asset);
            UnityPlayerInputGateAdapter gate = ConfigureGate(host, playerInput, fixture.GameplayMap);

            Require(
                host.GetComponent<PlayerPauseInput>() == null,
                "Gate-only proof accidentally materialized PlayerPauseInput.");
            Require(
                gate.TryValidateAuthoring(out string diagnostic),
                "UnityPlayerInputGateAdapter must be valid without PlayerPauseInput. " + diagnostic);
            Require(
                ReferenceEquals(gate.PlayerInput, playerInput),
                "Gate Adapter did not retain its explicit PlayerInput authority.");
            Require(
                gate.GameplayActionMapReference.HasSameIdentity(fixture.GameplayMap),
                "Gate Adapter did not retain the authored Gameplay Action Map identity.");
        }

        private static void RunPauseDerivesGateAuthorityCase(
            ICollection<UnityEngine.Object> created)
        {
            ActionFixture fixture = CreateActionFixture(created, "QA Pause Composition Actions");
            GameObject host = CreateInactiveHost(created, "QA Pause Composition Host");
            PlayerInput playerInput = ConfigurePlayerInput(host, fixture.Asset);
            UnityPlayerInputGateAdapter gate = ConfigureGate(host, playerInput, fixture.GameplayMap);
            PlayerPauseInput pauseInput = ConfigurePause(host, fixture.PauseReference);

            Require(
                gate.TryValidateAuthoring(out string gateDiagnostic),
                "Gate Adapter authoring must be valid before Pause validation. " + gateDiagnostic);
            Require(
                pauseInput.TryValidateAuthoring(out string pauseDiagnostic),
                "PlayerPauseInput must validate from the co-located Gate authority. " + pauseDiagnostic);
            Require(
                ReferenceEquals(pauseInput.PlayerInput, playerInput),
                "PlayerPauseInput did not derive PlayerInput from the Gate Adapter.");
            Require(
                pauseInput.GameplayActionMapReference.HasSameIdentity(fixture.GameplayMap),
                "PlayerPauseInput did not derive Gameplay Action Map identity from the Gate Adapter.");
            Require(
                string.Equals(pauseInput.GameplayActionMapName, fixture.GameplayMap.name, StringComparison.Ordinal),
                "PlayerPauseInput did not expose the Gate-owned Gameplay Action Map name.");
            Require(
                string.Equals(pauseInput.GlobalActionMapName, fixture.GlobalMap.name, StringComparison.Ordinal),
                "PlayerPauseInput did not derive the Global Action Map from Pause Action identity.");
        }

        private static void RunMissingGateCase(
            ICollection<UnityEngine.Object> created)
        {
            ActionFixture fixture = CreateActionFixture(created, "QA Missing Gate Actions");
            GameObject host = CreateInactiveHost(created, "QA Missing Gate Host");
            ConfigurePlayerInput(host, fixture.Asset);
            PlayerPauseInput pauseInput = ConfigurePause(host, fixture.PauseReference);

            Require(
                !pauseInput.TryValidateAuthoring(out string diagnostic),
                "PlayerPauseInput must reject authoring without a co-located Gate Adapter.");
            Require(
                diagnostic.IndexOf("exactly one UnityPlayerInputGateAdapter", StringComparison.Ordinal) >= 0,
                "Missing-Gate rejection did not report the exact-one Gate composition contract. " + diagnostic);
            Require(
                pauseInput.PlayerInput == null,
                "PlayerPauseInput must not fall back to the co-located PlayerInput when Gate authority is absent.");
        }

        private static void RunSingleGateAuthoringCase()
        {
            Require(
                Attribute.IsDefined(
                    typeof(UnityPlayerInputGateAdapter),
                    typeof(DisallowMultipleComponent),
                    true),
                "UnityPlayerInputGateAdapter must prevent duplicate authored adapters on one GameObject.");
        }

        private static void RunGuidIdentityCase(
            ICollection<UnityEngine.Object> created)
        {
            ActionFixture authored = CreateActionFixture(created, "QA Authored Map Identity");
            ActionFixture target = CreateActionFixture(created, "QA Same Names Different Identity");
            GameObject host = CreateInactiveHost(created, "QA GUID Identity Host");
            PlayerInput playerInput = ConfigurePlayerInput(host, target.Asset);
            UnityPlayerInputGateAdapter gate = ConfigureGate(
                host,
                playerInput,
                authored.GameplayMap);

            Require(
                string.Equals(authored.GameplayMap.name, target.GameplayMap.name, StringComparison.Ordinal),
                "GUID identity proof requires equal map names.");
            Require(
                authored.GameplayMap.id != target.GameplayMap.id,
                "GUID identity proof requires different map GUIDs.");
            Require(
                !gate.TryValidateAuthoring(out string diagnostic),
                "Gate Adapter must reject a same-name Gameplay map whose GUID is absent from PlayerInput.actions.");
            Require(
                diagnostic.IndexOf("Name fallback is not used", StringComparison.Ordinal) >= 0,
                "Gameplay map rejection did not prove GUID-only resolution. " + diagnostic);
        }

        private static void RunRestoreIdempotenceCase(
            ICollection<UnityEngine.Object> created)
        {
            ActionFixture fixture = CreateActionFixture(created, "QA Gate Restore Actions");
            GameObject host = CreateInactiveHost(created, "QA Gate Restore Host");
            PlayerInput playerInput = ConfigurePlayerInput(host, fixture.Asset);
            UnityPlayerInputGateAdapter gate = ConfigureGate(host, playerInput, fixture.GameplayMap);

            Require(
                gate.TryValidateAuthoring(out string diagnostic),
                "Restore proof requires valid Gate authoring. " + diagnostic);

            gate.Restore();
            gate.Restore();

            Require(
                !gate.IsBlockedByAdapter,
                "Repeated Restore on an unblocked Gate Adapter must remain idempotently unblocked.");
            Require(
                ReferenceEquals(gate.PlayerInput, playerInput),
                "Restore must not rewrite Gate Adapter PlayerInput authority.");
            Require(
                gate.GameplayActionMapReference.HasSameIdentity(fixture.GameplayMap),
                "Restore must not rewrite Gate Adapter Gameplay Action Map authority.");
        }

        private static ActionFixture CreateActionFixture(
            ICollection<UnityEngine.Object> created,
            string name)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = name;
            created.Add(asset);

            InputActionMap globalMap = asset.AddActionMap("Global");
            InputAction pauseAction = globalMap.AddAction("Pause", InputActionType.Button);
            InputActionMap gameplayMap = asset.AddActionMap("Gameplay");
            gameplayMap.AddAction("Move", InputActionType.Value);

            InputActionReference pauseReference = InputActionReference.Create(pauseAction);
            pauseReference.name = name + " Pause";
            created.Add(pauseReference);

            return new ActionFixture(
                asset,
                globalMap,
                gameplayMap,
                pauseReference);
        }

        private static GameObject CreateInactiveHost(
            ICollection<UnityEngine.Object> created,
            string name)
        {
            var host = new GameObject(name);
            host.SetActive(false);
            created.Add(host);
            return host;
        }

        private static PlayerInput ConfigurePlayerInput(
            GameObject host,
            InputActionAsset actions)
        {
            PlayerInput playerInput = host.AddComponent<PlayerInput>();
            playerInput.actions = actions;
            return playerInput;
        }

        private static UnityPlayerInputGateAdapter ConfigureGate(
            GameObject host,
            PlayerInput playerInput,
            InputActionMap gameplayMap)
        {
            UnityPlayerInputGateAdapter gate = host.AddComponent<UnityPlayerInputGateAdapter>();
            var serialized = new SerializedObject(gate);
            serialized.FindProperty("playerInput").objectReferenceValue = playerInput;

            SerializedProperty mapReference = serialized.FindProperty("gameplayActionMap");
            SerializedProperty actionAsset = mapReference.FindPropertyRelative("actionAsset");
            SerializedProperty actionMapId = mapReference.FindPropertyRelative("actionMapId");
            SerializedProperty cachedActionMapName = mapReference.FindPropertyRelative("cachedActionMapName");

            actionAsset.objectReferenceValue = gameplayMap.asset;
            actionMapId.stringValue = gameplayMap.id.ToString("D");
            cachedActionMapName.stringValue = gameplayMap.name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gate;
        }

        private static PlayerPauseInput ConfigurePause(
            GameObject host,
            InputActionReference pauseReference)
        {
            PlayerPauseInput pauseInput = host.AddComponent<PlayerPauseInput>();
            var serialized = new SerializedObject(pauseInput);
            serialized.FindProperty("pauseAction").objectReferenceValue = pauseReference;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return pauseInput;
        }

        private static void DestroyCreated(
            IReadOnlyList<UnityEngine.Object> created)
        {
            for (int index = created.Count - 1; index >= 0; index--)
            {
                if (created[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(created[index]);
                }
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

        private static string NextCase(
            IReadOnlyList<string> completed)
        {
            return completed.Count < ExpectedCases.Length
                ? ExpectedCases[completed.Count]
                : string.Empty;
        }

        private static string Escape(
            string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class ActionFixture
        {
            public ActionFixture(
                InputActionAsset asset,
                InputActionMap globalMap,
                InputActionMap gameplayMap,
                InputActionReference pauseReference)
            {
                Asset = asset;
                GlobalMap = globalMap;
                GameplayMap = gameplayMap;
                PauseReference = pauseReference;
            }

            public InputActionAsset Asset { get; }

            public InputActionMap GlobalMap { get; }

            public InputActionMap GameplayMap { get; }

            public InputActionReference PauseReference { get; }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.PlayerBinding;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player
{
    /// <summary>
    /// P2D runtime baseline executed inside a QA Route/Activity scene loaded by the framework.
    /// It observes the canonical PlayerInput topology without changing Gate state,
    /// switching action maps, executing movement or creating a parallel runtime authority.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/P2D Player Runtime Baseline Fixture")]
    public sealed class QaP2DPlayerRuntimeBaselineFixture : MonoBehaviour
    {
        private const string Prefix = "[P2D_PLAYER_RUNTIME_BASELINE]";

        [Header("Runtime Topology")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerSlotDeclaration playerSlot;
        [SerializeField] private UnityPlayerInputGateAdapter gateAdapter;
        [SerializeField] private PlayerControlBindingTargetBehaviour controlTarget;
        [SerializeField] private UnityPlayerInputBridgeTargetBehaviour bridgeTarget;
        [SerializeField] private UnityPlayerInputActivationTargetBehaviour activationTarget;

        [Header("Expected Authoring")]
        [SerializeField] private string expectedSlotId = "qa.player.1";
        [SerializeField] private string expectedActionMap = "Player";
        [SerializeField] private string expectedMoveAction = "Move";

        [Header("Execution")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool throwOnFailure;

        private IEnumerator Start()
        {
            if (!runOnStart)
            {
                yield break;
            }

            // Let PlayerInput and the framework Route/Activity lifecycle finish their first ticks.
            yield return null;
            yield return null;

            bool succeeded = RunBaseline();
            if (!succeeded && throwOnFailure)
            {
                throw new System.InvalidOperationException(
                    "P2D Player runtime baseline failed. Inspect the preceding QA evidence.");
            }
        }

        [ContextMenu("Run P2D Player Runtime Baseline")]
        public void RunFromContextMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' case='RequiresPlayMode' " +
                    "message='Run this fixture through the QA Hub in Play Mode.'",
                    this);
                return;
            }

            RunBaseline();
        }

        public bool RunBaseline()
        {
            var cases = new List<CaseResult>();

            InputActionAsset actions = playerInput != null ? playerInput.actions : null;
            InputActionMap actionMap = actions != null
                ? actions.FindActionMap(expectedActionMap, false)
                : null;
            InputAction moveAction = actionMap != null
                ? actionMap.FindAction(expectedMoveAction, false)
                : null;

            cases.Add(Case(
                "RunsInFrameworkLoadedScene",
                Application.isPlaying
                && gameObject.scene.IsValid()
                && gameObject.scene.isLoaded,
                $"isPlaying='{Application.isPlaying}' scene='{gameObject.scene.name}' " +
                $"loaded='{gameObject.scene.isLoaded}' activeScene='{SceneManager.GetActiveScene().name}'"));

            cases.Add(Case(
                "PlayerInputPresentAndActive",
                playerInput != null
                && playerInput.enabled
                && playerInput.gameObject.activeInHierarchy,
                $"present='{playerInput != null}' enabled='{(playerInput != null && playerInput.enabled)}' " +
                $"activeInHierarchy='{(playerInput != null && playerInput.gameObject.activeInHierarchy)}'"));

            cases.Add(Case(
                "InputActionAssetAssigned",
                actions != null,
                $"assigned='{actions != null}' asset='{(actions != null ? actions.name : "<none>")}'"));

            cases.Add(Case(
                "ExpectedActionMapExists",
                actionMap != null,
                $"expected='{expectedActionMap}' found='{actionMap != null}'"));

            cases.Add(Case(
                "ExpectedActionMapIsCurrent",
                playerInput != null
                && playerInput.currentActionMap != null
                && playerInput.currentActionMap == actionMap,
                $"expected='{expectedActionMap}' current='" +
                $"{(playerInput != null && playerInput.currentActionMap != null ? playerInput.currentActionMap.name : "<none>")}'"));

            cases.Add(Case(
                "MoveActionExists",
                moveAction != null,
                $"map='{expectedActionMap}' action='{expectedMoveAction}' found='{moveAction != null}'"));

            bool slotValid = false;
            bool slotMatches = false;
            bool slotInputMatches = false;
            string actualSlot = "<invalid>";

            if (playerSlot != null)
            {
                try
                {
                    slotValid = playerSlot.PlayerSlotId.IsValid;
                    actualSlot = slotValid
                        ? playerSlot.PlayerSlotId.Value.Value
                        : "<invalid>";
                    slotMatches = slotValid
                        && string.Equals(
                            actualSlot,
                            expectedSlotId,
                            System.StringComparison.Ordinal);
                    slotInputMatches = playerSlot.PlayerInputEvidence == playerInput;
                }
                catch (System.Exception exception)
                {
                    actualSlot = $"<invalid:{exception.Message}>";
                }
            }

            cases.Add(Case(
                "PlayerSlotIdentityStable",
                playerSlot != null && slotValid && slotMatches,
                $"present='{playerSlot != null}' expected='{expectedSlotId}' actual='{actualSlot}'"));

            cases.Add(Case(
                "PlayerSlotReferencesSamePlayerInput",
                playerSlot != null && slotInputMatches,
                $"sameReference='{slotInputMatches}'"));

            cases.Add(Case(
                "GateAdapterTopologyMatches",
                gateAdapter != null
                && gateAdapter.PlayerInput == playerInput
                && gateAdapter.SourceSlot == playerSlot
                && string.Equals(
                    gateAdapter.GameplayActionMapName,
                    expectedActionMap,
                    System.StringComparison.Ordinal)
                && gateAdapter.BlockOnInputAcceptance
                && gateAdapter.BlockOnGameplayAction,
                $"present='{gateAdapter != null}' " +
                $"sameInput='{(gateAdapter != null && gateAdapter.PlayerInput == playerInput)}' " +
                $"sameSlot='{(gateAdapter != null && gateAdapter.SourceSlot == playerSlot)}' " +
                $"map='{(gateAdapter != null ? gateAdapter.GameplayActionMapName : "<none>")}' " +
                $"blockInput='{(gateAdapter != null && gateAdapter.BlockOnInputAcceptance)}' " +
                $"blockGameplay='{(gateAdapter != null && gateAdapter.BlockOnGameplayAction)}'"));

            bool bridgeSlotMatches = false;
            if (bridgeTarget != null
                && playerSlot != null
                && bridgeTarget.TryGetExpectedPlayerSlotId(out PlayerSlotId bridgeSlot))
            {
                bridgeSlotMatches = bridgeSlot == playerSlot.PlayerSlotId;
            }

            cases.Add(Case(
                "BridgeTargetTopologyMatches",
                bridgeTarget != null
                && bridgeTarget.HasUnityPlayerInput
                && bridgeSlotMatches,
                $"present='{bridgeTarget != null}' " +
                $"hasInput='{(bridgeTarget != null && bridgeTarget.HasUnityPlayerInput)}' " +
                $"sameSlot='{bridgeSlotMatches}'"));

            bool activationSlotMatches = false;
            if (activationTarget != null
                && playerSlot != null
                && activationTarget.TryGetExpectedPlayerSlotId(out PlayerSlotId activationSlot))
            {
                activationSlotMatches = activationSlot == playerSlot.PlayerSlotId;
            }

            cases.Add(Case(
                "ActivationTargetTopologyMatches",
                activationTarget != null
                && activationTarget.HasUnityPlayerInput
                && activationSlotMatches
                && string.Equals(
                    activationTarget.ConfiguredActionMapName,
                    expectedActionMap,
                    System.StringComparison.Ordinal)
                && activationTarget.HasConfiguredActionMap,
                $"present='{activationTarget != null}' " +
                $"hasInput='{(activationTarget != null && activationTarget.HasUnityPlayerInput)}' " +
                $"sameSlot='{activationSlotMatches}' " +
                $"map='{(activationTarget != null ? activationTarget.ConfiguredActionMapName : "<none>")}' " +
                $"mapExists='{(activationTarget != null && activationTarget.HasConfiguredActionMap)}'"));

            cases.Add(Case(
                "ControlTargetPresentWithoutRuntimeBinding",
                controlTarget != null && !controlTarget.HasPlayerControlBinding,
                $"present='{controlTarget != null}' " +
                $"hasRuntimeBinding='{(controlTarget != null && controlTarget.HasPlayerControlBinding)}'"));

            int playerInputCount = CountPlayerInputsInScene(gameObject.scene);
            cases.Add(Case(
                "ExactlyOnePlayerInputInQaScene",
                playerInputCount == 1,
                $"count='{playerInputCount}'"));

            return Report(cases);
        }

        private static int CountPlayerInputsInScene(Scene scene)
        {
            PlayerInput[] inputs = Object.FindObjectsByType<PlayerInput>(FindObjectsInactive.Include);

            int count = 0;
            for (int i = 0; i < inputs.Length; i++)
            {
                PlayerInput input = inputs[i];
                if (input != null && input.gameObject.scene == scene)
                {
                    count++;
                }
            }

            return count;
        }

        private static CaseResult Case(string name, bool passed, string detail)
        {
            return new CaseResult(name, passed, detail);
        }

        private bool Report(IReadOnlyList<CaseResult> cases)
        {
            int passed = 0;
            int failed = 0;

            for (int i = 0; i < cases.Count; i++)
            {
                CaseResult result = cases[i];
                if (result.Passed)
                {
                    passed++;
                    Debug.Log(
                        $"{Prefix} case='{result.Name}' status='Passed' detail='{result.Detail}'",
                        this);
                }
                else
                {
                    failed++;
                    Debug.LogError(
                        $"{Prefix} case='{result.Name}' status='Failed' detail='{result.Detail}'",
                        this);
                }
            }

            string status = failed == 0 ? "Succeeded" : "Failed";
            string summary =
                $"{Prefix} status='{status}' passed='{passed}' failed='{failed}' cases='{cases.Count}'.";

            if (failed == 0)
            {
                Debug.Log(summary, this);
                return true;
            }

            Debug.LogError(summary, this);
            return false;
        }

        private readonly struct CaseResult
        {
            public CaseResult(string name, bool passed, string detail)
            {
                Name = name ?? string.Empty;
                Passed = passed;
                Detail = detail ?? string.Empty;
            }

            public string Name { get; }
            public bool Passed { get; }
            public string Detail { get; }
        }
    }
}

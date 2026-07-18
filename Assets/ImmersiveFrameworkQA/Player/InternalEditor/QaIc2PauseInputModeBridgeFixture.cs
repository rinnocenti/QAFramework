using System;
using Immersive.Framework.Actors;
using Immersive.Framework.InputMode;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    internal sealed class QaIc2PauseInputModeBridgeFixture : IDisposable
    {
        private readonly GameObject root;

        private QaIc2PauseInputModeBridgeFixture(
            GameObject root,
            PauseRequestTrigger pauseControl,
            PlayerInput playerInput,
            PlayerActorDeclaration playerActor,
            UnityInputTargetDeclaration gameplayTarget,
            UnityInputTargetDeclaration globalTarget,
            PauseInputModeUnityPlayerInputRuntimeBridge bridge,
            LocalPlayerProvisioningAuthoring provisioningAuthoring,
            InputActionAsset actionAsset)
        {
            this.root = root;
            PauseControl = pauseControl;
            PlayerInput = playerInput;
            PlayerActor = playerActor;
            GameplayTarget = gameplayTarget;
            GlobalTarget = globalTarget;
            Bridge = bridge;
            ProvisioningAuthoring = provisioningAuthoring;
            ActionAsset = actionAsset;
        }

        internal PauseRequestTrigger PauseControl { get; }
        internal PlayerInput PlayerInput { get; }
        internal PlayerActorDeclaration PlayerActor { get; }
        internal UnityInputTargetDeclaration GameplayTarget { get; }
        internal UnityInputTargetDeclaration GlobalTarget { get; }
        internal PauseInputModeUnityPlayerInputRuntimeBridge Bridge { get; }
        internal LocalPlayerProvisioningAuthoring ProvisioningAuthoring { get; }
        internal InputActionAsset ActionAsset { get; private set; }
        internal InputModeRuntimeSnapshot InputModeRuntime =>
            Bridge.InputModeRuntimeSnapshot;

        internal static QaIc2PauseInputModeBridgeFixture Create(
            string name,
            LocalPlayerProvisioningAuthoring provisioningAuthoring)
        {
            if (provisioningAuthoring == null)
            {
                throw new ArgumentNullException(nameof(provisioningAuthoring));
            }

            GameObject root = null;
            InputActionAsset actionAsset = null;
            try
            {
                root = new GameObject(name);
                root.SetActive(false);

                PauseRequestTrigger pauseControl =
                    root.AddComponent<PauseRequestTrigger>();
                var playerObject = new GameObject(name + " Player");
                playerObject.transform.SetParent(root.transform, false);
                actionAsset = CreateActionAsset(true, true, true);
                PlayerInput playerInput = playerObject.AddComponent<PlayerInput>();
                playerInput.actions = actionAsset;
                playerInput.defaultActionMap = "Global";
                playerObject.AddComponent<UnityPlayerInputGateAdapter>();

                PlayerActorDeclaration playerActor =
                    playerObject.AddComponent<PlayerActorDeclaration>();
                playerActor.ConfigureForDiagnostics(
                    "qa.ic2.actor.player.primary",
                    "QA IC2 Player Actor",
                    playerInput,
                    "qa.ic2.runtime-authority");

                UnityInputTargetDeclaration gameplayTarget =
                    playerObject.AddComponent<UnityInputTargetDeclaration>();
                gameplayTarget.ConfigureForDiagnostics(
                    UnityInputTargetRole.GameplayCommands,
                    "qa.ic2.input.target.gameplay",
                    "QA IC2 Gameplay Target",
                    playerInput,
                    "qa.ic2.runtime-authority",
                    true);

                var globalTargetObject =
                    new GameObject(name + " Global Pause Target");
                globalTargetObject.transform.SetParent(root.transform, false);
                UnityInputTargetDeclaration globalTarget =
                    globalTargetObject.AddComponent<UnityInputTargetDeclaration>();
                globalTarget.ConfigureForDiagnostics(
                    UnityInputTargetRole.GlobalUiPause,
                    "qa.ic2.input.target.global-pause",
                    "QA IC2 Global Pause Target",
                    null,
                    "qa.ic2.runtime-authority");

                PauseInputModeUnityPlayerInputRuntimeBridge bridge =
                    CreateBridge(
                        root.transform,
                        name + " Bridge",
                        playerInput,
                        globalTarget,
                        gameplayTarget,
                        playerActor,
                        provisioningAuthoring);
                root.SetActive(true);
                return new QaIc2PauseInputModeBridgeFixture(
                    root,
                    pauseControl,
                    playerInput,
                    playerActor,
                    gameplayTarget,
                    globalTarget,
                    bridge,
                    provisioningAuthoring,
                    actionAsset);
            }
            catch
            {
                DestroyImmediateSafe(root);
                DestroyImmediateSafe(actionAsset);
                throw;
            }
        }

        internal PauseInputModeUnityPlayerInputRuntimeBridgeResult TrySubmit(
            PauseRequestKind kind,
            string source,
            string reason)
        {
            PauseInputModeUnityPlayerInputRuntimeBridgeResult result =
                Bridge.SubmitForDiagnostics(kind, source, reason);
            if (result == null)
            {
                throw new InvalidOperationException(
                    "Canonical bridge did not produce an InputMode result.");
            }

            return result;
        }

        internal PauseInputModeUnityPlayerInputRuntimeBridge
            CreateIsolatedUnboundBridge()
        {
            return CreateBridge(
                root.transform,
                root.name + " Missing Pause Runtime Port Bridge",
                PlayerInput,
                GlobalTarget,
                GameplayTarget,
                PlayerActor,
                ProvisioningAuthoring);
        }

        internal void ReplaceActionAsset(
            bool includeGlobalMap,
            bool includePlayerMap,
            bool includeUiMap)
        {
            InputActionAsset previous = ActionAsset;
            ActionAsset = CreateActionAsset(
                includeGlobalMap,
                includePlayerMap,
                includeUiMap);
            PlayerInput.actions = ActionAsset;
            PlayerInput.defaultActionMap = includeGlobalMap
                ? "Global"
                : includePlayerMap
                    ? "Player"
                    : includeUiMap
                        ? "UI"
                        : string.Empty;
            DestroyImmediateSafe(previous);
        }

        internal void SetPlayerActorEvidence(bool enabled)
        {
            if (enabled)
            {
                PlayerActor.BindPlayerInputEvidence(PlayerInput);
            }
            else
            {
                PlayerActor.ClearPlayerInputEvidence(PlayerInput);
            }
        }

        public void Dispose()
        {
            DestroyImmediateSafe(root);
            DestroyImmediateSafe(ActionAsset);
        }

        private static PauseInputModeUnityPlayerInputRuntimeBridge CreateBridge(
            Transform parent,
            string name,
            PlayerInput playerInput,
            UnityInputTargetDeclaration globalTarget,
            UnityInputTargetDeclaration gameplayTarget,
            PlayerActorDeclaration playerActor,
            LocalPlayerProvisioningAuthoring provisioningAuthoring)
        {
            var bridgeObject = new GameObject(name);
            bridgeObject.transform.SetParent(parent, false);
            PauseInputModeUnityPlayerInputRuntimeBridge bridge =
                bridgeObject.AddComponent<PauseInputModeUnityPlayerInputRuntimeBridge>();
            bridge.ConfigureForDiagnostics(
                playerInput,
                new[] { globalTarget, gameplayTarget },
                new[] { playerActor },
                provisioningAuthoring,
                "Player",
                "UI",
                true);
            return bridge;
        }

        private static InputActionAsset CreateActionAsset(
            bool includeGlobalMap,
            bool includePlayerMap,
            bool includeUiMap)
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "QA IC2 Runtime Authority Actions";

            if (includeGlobalMap)
            {
                var globalMap = new InputActionMap("Global");
                globalMap.AddAction("PauseToggle", InputActionType.Button);
                asset.AddActionMap(globalMap);
            }

            if (includePlayerMap)
            {
                var playerMap = new InputActionMap("Player");
                playerMap.AddAction("Move", InputActionType.Value);
                asset.AddActionMap(playerMap);
            }

            if (includeUiMap)
            {
                var uiMap = new InputActionMap("UI");
                uiMap.AddAction("Submit", InputActionType.Button);
                asset.AddActionMap(uiMap);
            }

            return asset;
        }

        private static void DestroyImmediateSafe(UnityEngine.Object target)
        {
            if (target != null)
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}

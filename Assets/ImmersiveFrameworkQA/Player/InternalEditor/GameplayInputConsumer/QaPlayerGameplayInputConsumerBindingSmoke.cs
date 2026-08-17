using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Internal.Editor
{
    internal static class QaPlayerGameplayInputConsumerBindingSmoke
    {
        private const string Prefix =
            "[QA][PLAYER-GAMEPLAY-INPUT-CONSUMER-01]";

        [MenuItem("Immersive Framework QA/Player/Gameplay Input Consumer Surface")]
        internal static void Run()
        {
            var completed = new List<string>();
            GameObject actorObject = null;
            InputActionAsset authoredAsset = null;
            InputActionAsset runtimeAsset = null;
            InputActionAsset foreignAsset = null;
            InputActionReference moveReference = null;
            InputActionReference foreignMoveReference = null;

            try
            {
                authoredAsset = ScriptableObject.CreateInstance<InputActionAsset>();
                InputActionMap authoredGameplay = new InputActionMap("Gameplay");
                InputAction authoredMove = authoredGameplay.AddAction(
                    "Move", InputActionType.Value, expectedControlLayout: "Vector2");
                authoredAsset.AddActionMap(authoredGameplay);
                moveReference = InputActionReference.Create(authoredMove);

                Guid authoredGameplayId = authoredGameplay.id;
                Guid authoredMoveId = authoredMove.id;

                // Materialize a distinct identity-preserving action-asset copy.
                // Do not use UnityEngine.Object.Instantiate here: that cloning path
                // is not the PlayerInput-owned runtime-copy contract this smoke is
                // trying to model and may regenerate Input System map/action IDs.
                runtimeAsset = InputActionAsset.FromJson(authoredAsset.ToJson());

                Require(
                    runtimeAsset.actionMaps.Count == 1,
                    "Fixture runtime copy did not preserve the authored Action Map count.");
                InputActionMap runtimeGameplay = runtimeAsset.actionMaps[0];
                Require(
                    runtimeGameplay.actions.Count == 1,
                    "Fixture runtime copy did not preserve the authored action count.");
                InputAction runtimeMove = runtimeGameplay.actions[0];

                Require(
                    !ReferenceEquals(authoredGameplay, runtimeGameplay) &&
                    !ReferenceEquals(authoredMove, runtimeMove),
                    "Fixture did not produce distinct runtime map/action instances.");
                Require(
                    runtimeGameplay.id == authoredGameplayId,
                    "Runtime Action Map copy did not preserve authored GUID identity.");
                Require(
                    runtimeMove.id == authoredMoveId,
                    "Runtime action copy did not preserve authored GUID identity.");
                completed.Add("runtime-copy-distinct");

                Require(
                    PlayerGameplayInputConsumerBinding.TryResolveRuntimeActionIdentity(
                        runtimeAsset,
                        runtimeGameplay,
                        moveReference,
                        out InputAction resolvedMove,
                        out string resolveIssue),
                    resolveIssue);
                Require(
                    ReferenceEquals(resolvedMove, runtimeMove),
                    "Consumer resolver did not return the exact runtime action instance.");
                Require(
                    !ReferenceEquals(resolvedMove, moveReference.action),
                    "Consumer resolver returned the authored action instance instead of the runtime clone.");
                completed.Add("guid-resolves-runtime-instance");

                InputActionMap runtimeUi = runtimeAsset.AddActionMap("UI");
                runtimeUi.AddAction("Submit", InputActionType.Button);
                Require(
                    !PlayerGameplayInputConsumerBinding.TryResolveRuntimeActionIdentity(
                        runtimeAsset,
                        runtimeUi,
                        moveReference,
                        out _,
                        out _),
                    "Resolver accepted an authored action outside the current gameplay Action Map.");
                completed.Add("current-map-required");

                foreignAsset = ScriptableObject.CreateInstance<InputActionAsset>();
                InputActionMap foreignGameplay = new InputActionMap("Gameplay");
                InputAction foreignMove = foreignGameplay.AddAction(
                    "Move", InputActionType.Value, expectedControlLayout: "Vector2");
                foreignAsset.AddActionMap(foreignGameplay);
                foreignMoveReference = InputActionReference.Create(foreignMove);

                Require(
                    foreignMove.id != authoredMove.id,
                    "Foreign action fixture unexpectedly reused authored GUID.");
                Require(
                    !PlayerGameplayInputConsumerBinding.TryResolveRuntimeActionIdentity(
                        runtimeAsset,
                        runtimeGameplay,
                        foreignMoveReference,
                        out _,
                        out string noFallbackIssue),
                    "Resolver silently fell back to action name instead of GUID identity.");
                Require(
                    noFallbackIssue.IndexOf("name fallback is not used",
                        StringComparison.OrdinalIgnoreCase) >= 0,
                    "GUID miss did not report the no-name-fallback contract.");
                completed.Add("no-name-fallback");

                actorObject = new GameObject("QA Gameplay Input Consumer Actor");
                PlayerGameplayInputConsumerBinding consumer =
                    actorObject.AddComponent<PlayerGameplayInputConsumerBinding>();
                Require(
                    !consumer.HasCurrentGameplayBinding && !consumer.GameplayReady,
                    "Fresh consumer unexpectedly reported current gameplay authority.");
                Require(
                    !consumer.TryReadValue<Vector2>(moveReference, out Vector2 unboundValue) &&
                    unboundValue == Vector2.zero,
                    "Unbound consumer did not fail closed with a default value.");
                completed.Add("unbound-fails-closed");

                Type surface = typeof(PlayerGameplayInputConsumerBinding);
                bool exposesRawInput = surface
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Any(p => IsRawInputType(p.PropertyType)) ||
                    surface
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName)
                    .Any(m => IsRawInputType(m.ReturnType));
                Require(
                    !exposesRawInput,
                    "Public gameplay input consumer surface exposes raw PlayerInput/InputAction/InputActionMap authority.");
                completed.Add("no-raw-input-authority");

                Debug.Log(
                    $"{Prefix} PASS. cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"{Prefix} FAIL. {exception}");
            }
            finally
            {
                if (actorObject != null) UnityEngine.Object.DestroyImmediate(actorObject);
                if (moveReference != null) UnityEngine.Object.DestroyImmediate(moveReference);
                if (foreignMoveReference != null) UnityEngine.Object.DestroyImmediate(foreignMoveReference);
                if (runtimeAsset != null) UnityEngine.Object.DestroyImmediate(runtimeAsset);
                if (authoredAsset != null) UnityEngine.Object.DestroyImmediate(authoredAsset);
                if (foreignAsset != null) UnityEngine.Object.DestroyImmediate(foreignAsset);
            }
        }

        private static bool IsRawInputType(Type type) =>
            type == typeof(PlayerInput) ||
            type == typeof(InputAction) ||
            type == typeof(InputActionMap) ||
            type == typeof(InputActionAsset);

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }
    }
}

using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// IF-PLAYER-SERIALIZATION-01 — proves that the serialized numeric identity
    /// of PlayerProvisioningCommandOperation remains stable and that retired
    /// values are explicitly rejected instead of being remapped.
    /// </summary>
    internal static class QaPlayerSerializationIdentityRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run Player Serialization Identity QA";
        private const string Prefix = "[QA_PLAYER_SERIALIZATION]";
        private const int ExpectedCaseCount = 5;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            !EditorApplication.isPlayingOrWillChangePlaymode;

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            if (!Execute(out string error))
            {
                throw new InvalidOperationException(error);
            }
        }

        /// <summary>
        /// Typed Edit Mode entry point. The Player Full QA orchestrator may call
        /// this directly without reproducing any serialization proof.
        /// </summary>
        internal static bool Execute(out string error)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                error = "Player Serialization Identity QA must run in Edit Mode.";
                Debug.LogError(
                    $"{Prefix} status='Failed' cases='0/{ExpectedCaseCount}' " +
                    $"error='{Escape(error)}'.");
                return false;
            }

            var failures = new List<string>();
            int passed = 0;
            GameObject owner = null;

            try
            {
                owner = new GameObject("QA_PlayerSerializationIdentity");
                PlayerSessionCommandTrigger trigger =
                    owner.AddComponent<PlayerSessionCommandTrigger>();

                RunCase(
                    "OpenJoiningIdentity",
                    10,
                    null,
                    () => ProveOpenJoiningIdentity(trigger),
                    failures,
                    ref passed);
                RunCase(
                    "CloseJoiningIdentity",
                    20,
                    null,
                    () => ProveCloseJoiningIdentity(trigger),
                    failures,
                    ref passed);
                RunCase(
                    "RetiredCapacityValue",
                    30,
                    "UNSUPPORTED AS EXPECTED",
                    () => ProveRetiredCapacityValue(trigger),
                    failures,
                    ref passed);
                RunCase(
                    "RequestJoinIdentity",
                    40,
                    null,
                    () => ProveRequestJoinIdentity(trigger),
                    failures,
                    ref passed);
                RunCase(
                    "RequestDefaultActorSelectionIdentity",
                    50,
                    null,
                    () => ProveRequestDefaultActorSelectionIdentity(trigger),
                    failures,
                    ref passed);
            }
            catch (Exception exception)
            {
                failures.Add(
                    $"harness: {exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                if (owner != null)
                {
                    UnityEngine.Object.DestroyImmediate(owner);
                }
            }

            if (passed != ExpectedCaseCount || failures.Count > 0)
            {
                error = failures.Count > 0
                    ? string.Join(" | ", failures)
                    : $"Expected {ExpectedCaseCount} passing cases but observed {passed}.";
                Debug.LogError(
                    $"{Prefix} status='Failed' cases='{passed}/{ExpectedCaseCount}' " +
                    $"error='{Escape(error)}'.");
                return false;
            }

            error = string.Empty;
            Debug.Log(
                $"{Prefix} status='Passed' " +
                "verdict='PLAYER SERIALIZED COMMAND IDENTITY CERTIFIED' " +
                $"cases='{passed}/{ExpectedCaseCount}'.");
            return true;
        }

        private static void ProveOpenJoiningIdentity(
            PlayerSessionCommandTrigger trigger)
        {
            InjectSerializedOperation(trigger, 10);
            Require(
                trigger.Operation == PlayerProvisioningCommandOperation.OpenJoining,
                $"Serialized value 10 resolved to '{trigger.Operation}' instead of OpenJoining.");
            RequireRecognizedOperation(trigger, 10);
        }

        private static void ProveCloseJoiningIdentity(
            PlayerSessionCommandTrigger trigger)
        {
            InjectSerializedOperation(trigger, 20);
            Require(
                trigger.Operation == PlayerProvisioningCommandOperation.CloseJoining,
                $"Serialized value 20 resolved to '{trigger.Operation}' instead of CloseJoining.");
            RequireRecognizedOperation(trigger, 20);
        }

        private static void ProveRetiredCapacityValue(
            PlayerSessionCommandTrigger trigger)
        {
            InjectSerializedOperation(trigger, 30);

            bool valid = trigger.TryValidateConfiguration(out string issue);
            Require(
                !valid,
                "Serialized value 30 unexpectedly passed TryValidateConfiguration().");
            Require(
                IsUnsupportedDiagnostic(issue),
                $"Serialized value 30 was rejected without an explicit unsupported-operation diagnostic: '{issue}'.");

            // Supplemental enum-shape evidence only. The authoring validation above
            // is the primary proof that the retired serialized value is rejected.
            Require(
                !Enum.IsDefined(
                    typeof(PlayerProvisioningCommandOperation),
                    trigger.Operation),
                "Serialized value 30 is still defined as a supported Player provisioning command.");

            int invocationCountBefore = trigger.InvocationCount;
            trigger.InvokeConfiguredOperation();

            Require(
                trigger.InvocationCount == invocationCountBefore + 1,
                "Retired value 30 invocation was not observed by the authoring component.");
            Require(
                trigger.LastResultKind == PlayerProvisioningCommandResultKind.None,
                $"Retired value 30 executed supported result kind '{trigger.LastResultKind}'.");
            Require(
                !trigger.HasLastTypedResult,
                "Retired value 30 produced a supported typed command result.");
            Require(
                trigger.LastParticipationResult == null &&
                trigger.LastJoinResult == null &&
                trigger.LastActorSelectionResult == null,
                "Retired value 30 populated a supported command result surface.");
            Require(
                IsUnsupportedDiagnostic(trigger.LastDiagnostic),
                $"Retired value 30 invocation did not retain an explicit unsupported diagnostic: '{trigger.LastDiagnostic}'.");
            Require(
                (int)trigger.Operation == 30,
                $"Retired value 30 was remapped during invocation to numeric value '{(int)trigger.Operation}'.");
        }

        private static void ProveRequestJoinIdentity(
            PlayerSessionCommandTrigger trigger)
        {
            InjectSerializedOperation(trigger, 40);
            Require(
                trigger.Operation == PlayerProvisioningCommandOperation.RequestJoin,
                $"Serialized value 40 resolved to '{trigger.Operation}' instead of RequestJoin.");
            Require(
                trigger.Operation !=
                    PlayerProvisioningCommandOperation.RequestDefaultActorSelection,
                "Serialized value 40 was reinterpreted as RequestDefaultActorSelection.");
            RequireRecognizedOperation(trigger, 40);

            int invocationCountBefore = trigger.InvocationCount;
            trigger.InvokeConfiguredOperation();

            Require(
                trigger.InvocationCount == invocationCountBefore + 1,
                "RequestJoin invocation was not observed.");
            Require(
                trigger.LastResultKind ==
                    PlayerProvisioningCommandResultKind.LocalPlayerJoin,
                $"Serialized value 40 dispatched result kind '{trigger.LastResultKind}' instead of LocalPlayerJoin.");
            Require(
                trigger.HasLastTypedResult && trigger.LastJoinResult != null,
                "Serialized value 40 did not execute the RequestJoin command path.");
            Require(
                trigger.LastParticipationResult == null &&
                trigger.LastActorSelectionResult == null,
                "Serialized value 40 populated a non-Join command result surface.");
            Require(
                !IsUnsupportedDiagnostic(trigger.LastDiagnostic),
                $"Serialized value 40 was treated as unsupported: '{trigger.LastDiagnostic}'.");
        }

        private static void ProveRequestDefaultActorSelectionIdentity(
            PlayerSessionCommandTrigger trigger)
        {
            InjectSerializedOperation(trigger, 50);
            Require(
                trigger.Operation ==
                    PlayerProvisioningCommandOperation.RequestDefaultActorSelection,
                $"Serialized value 50 resolved to '{trigger.Operation}' instead of RequestDefaultActorSelection.");
            Require(
                trigger.Operation != PlayerProvisioningCommandOperation.RequestJoin,
                "Serialized value 50 was reinterpreted as RequestJoin.");
            RequireRecognizedOperation(trigger, 50);

            int invocationCountBefore = trigger.InvocationCount;
            trigger.InvokeConfiguredOperation();

            Require(
                trigger.InvocationCount == invocationCountBefore + 1,
                "RequestDefaultActorSelection invocation was not observed.");
            Require(
                trigger.LastResultKind ==
                    PlayerProvisioningCommandResultKind.ActorSelection,
                $"Serialized value 50 dispatched result kind '{trigger.LastResultKind}' instead of ActorSelection.");
            Require(
                trigger.HasLastTypedResult &&
                trigger.LastActorSelectionResult != null,
                "Serialized value 50 did not execute the RequestDefaultActorSelection command path.");
            Require(
                trigger.LastParticipationResult == null &&
                trigger.LastJoinResult == null,
                "Serialized value 50 populated a non-ActorSelection command result surface.");
            Require(
                !IsUnsupportedDiagnostic(trigger.LastDiagnostic),
                $"Serialized value 50 was treated as unsupported: '{trigger.LastDiagnostic}'.");
        }

        private static void InjectSerializedOperation(
            PlayerSessionCommandTrigger trigger,
            int rawValue)
        {
            var serialized = new SerializedObject(trigger);
            SerializedProperty operation = serialized.FindProperty("operation");
            Require(
                operation != null,
                "Serialized PlayerSessionCommandTrigger field 'operation' was not found.");

            operation.intValue = rawValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            serialized.Update();

            Require(
                operation.intValue == rawValue,
                $"SerializedProperty did not preserve raw operation value '{rawValue}'; read back '{operation.intValue}'.");
            Require(
                (int)trigger.Operation == rawValue,
                $"Authoring component did not expose serialized operation value '{rawValue}'; exposed '{(int)trigger.Operation}'.");
        }

        private static void RequireRecognizedOperation(
            PlayerSessionCommandTrigger trigger,
            int rawValue)
        {
            bool valid = trigger.TryValidateConfiguration(out string issue);
            if (!valid)
            {
                Require(
                    !IsUnsupportedDiagnostic(issue),
                    $"Supported serialized value {rawValue} was rejected as unsupported: '{issue}'.");
            }
        }

        private static bool IsUnsupportedDiagnostic(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.IndexOf(
                       "unsupported operation",
                       StringComparison.OrdinalIgnoreCase) >= 0 ||
                   value.IndexOf(
                       "not supported",
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void RunCase(
            string caseName,
            int rawValue,
            string verdict,
            Action proof,
            List<string> failures,
            ref int passed)
        {
            try
            {
                proof();
                passed++;
                string verdictSuffix = string.IsNullOrEmpty(verdict)
                    ? string.Empty
                    : $" verdict='{verdict}'";
                Debug.Log(
                    $"{Prefix} case='{caseName}' value='{rawValue}' status='PASS'" +
                    $"{verdictSuffix}.");
            }
            catch (Exception exception)
            {
                string failure =
                    $"{caseName}: {exception.GetType().Name}: {exception.Message}";
                failures.Add(failure);
                Debug.LogError(
                    $"{Prefix} case='{caseName}' value='{rawValue}' status='FAIL' " +
                    $"error='{Escape(exception.Message)}'.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "''").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// IF-PLAYER-SERIALIZATION-01 — verifies that every supported authored
    /// command has its own concrete component and no serialized operation enum.
    /// </summary>
    internal static class QaPlayerExplicitCommandSurfaceRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/Run Explicit Command Surface QA";
        private const string Prefix = "[QA_PLAYER_EXPLICIT_COMMAND_SURFACE]";
        private const int ExpectedCaseCount = 8;

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

        internal static bool Execute(out string error)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                error = "Explicit Player command surface QA must run in Edit Mode.";
                return false;
            }

            var failures = new List<string>();
            GameObject owner = new GameObject("QA_PlayerExplicitCommandSurface");
            try
            {
                ProveExplicitSurface<PlayerSessionOpenJoiningCommandTrigger>(owner, failures);
                ProveExplicitSurface<PlayerSessionCloseJoiningCommandTrigger>(owner, failures);
                ProveExplicitSurface<PlayerSessionJoinCommandTrigger>(owner, failures);
                ProveExplicitSurface<PlayerSessionSelectActorCommandTrigger>(owner, failures);
                ProveExplicitSurface<PlayerSessionDefaultActorSelectionCommandTrigger>(owner, failures);
                ProveExplicitSurface<PlayerSessionReplaceActorSelectionCommandTrigger>(owner, failures);
                ProveExplicitSurface<PlayerSessionClearActorSelectionCommandTrigger>(owner, failures);
                ProveExplicitSurface<PlayerSessionLeaveCommandTrigger>(owner, failures);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }

            if (failures.Count > 0)
            {
                error = string.Join(" | ", failures);
                Debug.LogError($"{Prefix} status='Failed' error='{Escape(error)}'.");
                return false;
            }

            error = string.Empty;
            Debug.Log($"{Prefix} status='Passed' cases='{ExpectedCaseCount}/{ExpectedCaseCount}'.");
            return true;
        }

        private static void ProveExplicitSurface<T>(GameObject owner, List<string> failures)
            where T : PlayerSessionCommandTriggerBase
        {
            T component = null;
            try
            {
                component = owner.AddComponent<T>();
                var serialized = new SerializedObject(component);
                SerializedProperty scope = serialized.FindProperty("scope");
                Require(scope != null,
                    $"{typeof(T).Name} has no serialized scope.");
                Require(serialized.FindProperty("operation") == null,
                    $"{typeof(T).Name} still serializes a generic operation selector.");
                string defaultScope = scope.enumNames[scope.enumValueIndex];
                Require(
                    string.Equals(
                        defaultScope,
                        LocalPlayerProvisioningConsumerScope.Route.ToString(),
                        StringComparison.Ordinal) ||
                    string.Equals(
                        defaultScope,
                        LocalPlayerProvisioningConsumerScope.Activity.ToString(),
                        StringComparison.Ordinal),
                    $"{typeof(T).Name} default scope must be Route or Activity; actual='{defaultScope}'.");
                bool requiresActor = typeof(T) ==
                    typeof(PlayerSessionSelectActorCommandTrigger) ||
                    typeof(T) == typeof(PlayerSessionReplaceActorSelectionCommandTrigger);
                Require(
                    (serialized.FindProperty("actorProfile") != null) == requiresActor,
                    $"{typeof(T).Name} has an invalid Actor Profile field surface.");
                if (typeof(T) == typeof(PlayerSessionSelectActorCommandTrigger) ||
                    typeof(T) == typeof(PlayerSessionDefaultActorSelectionCommandTrigger) ||
                    typeof(T) == typeof(PlayerSessionReplaceActorSelectionCommandTrigger) ||
                    typeof(T) == typeof(PlayerSessionClearActorSelectionCommandTrigger))
                {
                    Require(serialized.FindProperty("playerSlot") != null &&
                        serialized.FindProperty("expectedSelectionRevision") != null,
                        $"{typeof(T).Name} has an incomplete Actor command field surface.");
                }
                int unspecifiedIndex = Array.IndexOf(
                    scope.enumNames,
                    LocalPlayerProvisioningConsumerScope.Unspecified.ToString());
                Require(unspecifiedIndex >= 0,
                    $"{typeof(T).Name} scope has no Unspecified value.");
                scope.enumValueIndex = unspecifiedIndex;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Require(!component.TryValidateConfiguration(out string issue) &&
                    !string.IsNullOrWhiteSpace(issue),
                    $"{typeof(T).Name} did not reject an explicitly Unspecified scope.");
            }
            catch (Exception exception)
            {
                failures.Add($"{typeof(T).Name}: {exception.Message}");
            }
            finally
            {
                if (component != null)
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
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

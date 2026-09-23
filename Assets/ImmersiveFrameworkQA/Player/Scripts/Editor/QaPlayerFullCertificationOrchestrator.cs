using System;
using Immersive.Framework.GameFlow;
using ImmersiveFrameworkQA.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Canonical one-button Player QA entry. In Edit Mode it validates
    /// authoring. In Play Mode it runs the scene panel suite, requesting the
    /// Player route from Hub when needed.
    /// </summary>
    public static class QaPlayerFullCertificationOrchestrator
    {
        private const string Prefix = "[QA_PLAYER_FULL]";
        private const string MenuPath = "Immersive Framework/QA/Player/Run Full Player QA";
        private const string CanonicalScenePath = PlayerQaPaths.PrimaryScenePath;
        private const string HubScenePath = "Assets/ImmersiveFrameworkQA/Hub/Scenes/QA_Hub.unity";
        private const double TimeoutSeconds = 90d;

        private static bool running;
        private static double startedAt;
        private static bool waitingForScene;

        [MenuItem(MenuPath)]
        private static void RunFromMenu()
        {
            if (!EditorApplication.isPlaying)
            {
                try
                {
                    PlayerQaAuthoringRegression.Validate(emitResult: true);
                    PlayerQaPauseCompositionRegression.Execute(out _);
                    Debug.Log(
                        $"{Prefix} status='AuthoringPassed' " +
                        "next='Enter Play Mode on QA_Hub and run Full Player QA again, or open Player QA and click Run All Player QA'.");
                }
                catch (Exception exception)
                {
                    Debug.LogError(
                        $"{Prefix} status='Failed' verdict='PLAYER QA NOT CERTIFIED' " +
                        $"phase='authoring' message='{Escape(exception.Message)}'.");
                }

                return;
            }

            RunPlayModeCertification();
        }

        private static void RunPlayModeCertification()
        {
            if (running)
            {
                Debug.LogWarning($"{Prefix} status='Ignored' reason='AlreadyRunning'.");
                return;
            }

            PlayerQaPanel panel = FindPanel();
            if (panel != null)
            {
                panel.RunAllPlayerQa();
                return;
            }

            RouteRequestTrigger trigger = FindHubPlayerTrigger();
            if (trigger == null)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' reason='PlayerRouteTriggerMissing' " +
                    "message='Open QA_Hub in Play Mode, or enter the Player QA scene first'.");
                return;
            }

            running = true;
            waitingForScene = true;
            startedAt = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            Debug.Log($"{Prefix} status='Running' phase='RequestingPlayerRoute'.");
            trigger.RequestRoute();
        }

        private static void Tick()
        {
            if (!running)
            {
                EditorApplication.update -= Tick;
                return;
            }

            if (!EditorApplication.isPlaying)
            {
                Fail("Play Mode ended before Full Player QA completed.");
                return;
            }

            if (EditorApplication.timeSinceStartup - startedAt > TimeoutSeconds)
            {
                Fail("Timed out waiting for the Player QA scene and panel.");
                return;
            }

            PlayerQaPanel panel = FindPanel();
            if (panel == null)
            {
                return;
            }

            if (waitingForScene)
            {
                waitingForScene = false;
                panel.RunAllPlayerQa();
            }

            if (!panel.IsRunning)
            {
                running = false;
                EditorApplication.update -= Tick;
            }
        }

        private static PlayerQaPanel FindPanel()
        {
            PlayerQaPanel[] panels = UnityEngine.Object.FindObjectsByType<PlayerQaPanel>(
                FindObjectsInactive.Exclude);
            return panels != null && panels.Length > 0 ? panels[0] : null;
        }

        private static RouteRequestTrigger FindHubPlayerTrigger()
        {
            Scene hub = SceneManager.GetSceneByPath(HubScenePath);
            if (!hub.IsValid() || !hub.isLoaded)
            {
                return null;
            }

            RouteRequestTrigger[] triggers = UnityEngine.Object.FindObjectsByType<RouteRequestTrigger>(
                FindObjectsInactive.Exclude);
            for (int index = 0; index < triggers.Length; index++)
            {
                RouteRequestTrigger trigger = triggers[index];
                if (trigger != null &&
                    trigger.TargetRoute != null &&
                    string.Equals(
                        trigger.TargetRoute.PrimaryScenePath,
                        CanonicalScenePath,
                        StringComparison.Ordinal))
                {
                    return trigger;
                }
            }

            return null;
        }

        private static void Fail(string message)
        {
            running = false;
            waitingForScene = false;
            EditorApplication.update -= Tick;
            Debug.LogError($"{Prefix} status='Failed' message='{Escape(message)}'.");
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

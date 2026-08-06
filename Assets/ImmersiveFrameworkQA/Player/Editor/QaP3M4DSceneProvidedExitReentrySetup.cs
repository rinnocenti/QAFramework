using System;
using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent dependency setup for the P3M4D Scene-Provided Player
    /// lifecycle regression. No additional runtime assets are required.
    /// </summary>
    internal static class QaP3M4DSceneProvidedExitReentrySetup
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4D Setup Scene-Provided Exit Reentry Fixture";

        private const string LogPrefix =
            "[QA][P3M4D Scene-Provided Exit Reentry Setup]";

        [MenuItem(MenuPath)]
        internal static void Apply()
        {
            try
            {
                Require(
                    !EditorApplication.isPlaying,
                    "P3M4D setup must run in Edit Mode.");

                QaP3M4BRouteSceneProvidedAdmissionSetup.Apply();

                RouteAsset route =
                    AssetDatabase.LoadAssetAtPath<RouteAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath);

                ActivityAsset activity =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.ActivityPath);

                GameObject playerPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.PlayerPrefabPath);

                SceneAsset scene =
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        QaP3M4BRouteSceneProvidedAdmissionSetup.ScenePath);

                Require(
                    route != null &&
                    activity != null &&
                    playerPrefab != null &&
                    scene != null,
                    "P3M4D requires the complete P3M4B Route Scene-Provided fixture.");

                Require(
                    ReferenceEquals(
                        route.StartupActivity,
                        activity),
                    "P3M4B Route does not reference the expected Startup Activity.");

                Debug.Log(
                    $"{LogPrefix} PASS. status='Applied' " +
                    $"route='{QaP3M4BRouteSceneProvidedAdmissionSetup.RoutePath}' " +
                    $"activity='{QaP3M4BRouteSceneProvidedAdmissionSetup.ActivityPath}' " +
                    $"scene='{QaP3M4BRouteSceneProvidedAdmissionSetup.ScenePath}' " +
                    $"playerPrefab='{QaP3M4BRouteSceneProvidedAdmissionSetup.PlayerPrefabPath}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}

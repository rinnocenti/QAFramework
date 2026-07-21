using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using System;
using System.Collections.Generic;
using ImmersiveFrameworkQA.Hub;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Hub.Editor
{
    public static class QaHubSceneBuilder
    {
        private const string Root = "Assets/ImmersiveFrameworkQA";
        private const string HubScenePath = Root + "/Hub/Scenes/QA_Hub.unity";

        private static readonly HubTarget[] Targets =
        {
            new(
                "Camera",
                "Local Player Camera Publication Regression",
                Root + "/Lifecycle/Routes/QA_LifecycleRouteA.asset"),
            new(
                "Player",
                "Player Gameplay Admission Regression",
                Root + "/Lifecycle/Routes/QA_LifecycleRouteA.asset"),
            new(
                "Player",
                "Scene Player Route Lifecycle Regression",
                Root + "/Player/P3M5B/P3M5B_RouteA.asset"),
            new(
                "Pause",
                "Pause Product Binding QA",
                Root + "/Player/PauseP1/QA_PauseP1Route.asset"),
            new(
                "Pooling",
                "Pooling Runtime Regression",
                Root + "/Pooling/Routes/QA_PoolingRoute.asset")
        };

        [MenuItem("Immersive Framework/QA/Setup/Hub/Create or Refresh QA Hub")]
        public static void CreateOrRefreshHub()
        {
            try
            {
                HubSetupSummary summary = ApplyHubConfiguration();
                Debug.Log(
                    $"[QA_HUB_SETUP] status='Applied' scene='{summary.SceneName}' " +
                    $"groups='{summary.GroupCount}' entries='{summary.EntryCount}' " +
                    $"duplicates='{summary.DuplicateCount}'");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[QA_HUB_SETUP] status='Failed' reason='{FormatDiagnostic(exception)}'");
                throw;
            }
        }

        public static void CreateOrRefreshHubForSetup()
        {
            HubSetupSummary summary = ApplyHubConfiguration();
            if (summary.DuplicateCount != 0)
            {
                throw new InvalidOperationException(
                    $"QA Hub setup produced '{summary.DuplicateCount}' duplicate entries.");
            }
        }

        private static HubSetupSummary ApplyHubConfiguration()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QA_Hub";
            var cameraRoot = new GameObject("QA_HubCamera");
            Camera camera = cameraRoot.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.06f, 1f);
            cameraRoot.tag = "MainCamera";
            cameraRoot.transform.position = new Vector3(0f, 0f, -10f);

            var canvasRoot = new GameObject("QA_HubCanvas");
            canvasRoot.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            QaHubPanel panel = canvasRoot.AddComponent<QaHubPanel>();
            var entries = new QaHubPanel.QaHubEntry[Targets.Length];
            for (int index = 0; index < Targets.Length; index++)
            {
                HubTarget target = Targets[index];
                var triggerRoot = new GameObject("RouteTrigger_" + SanitizeName(target.Label));
                triggerRoot.transform.SetParent(canvasRoot.transform, false);
                RouteRequestTrigger trigger = triggerRoot.AddComponent<RouteRequestTrigger>();
                RouteAsset route = AssetDatabase.LoadAssetAtPath<RouteAsset>(target.RoutePath)
                    ?? throw new System.InvalidOperationException($"Required QA route is missing: '{target.RoutePath}'.");
                var serialized = new SerializedObject(trigger);
                serialized.FindProperty("targetRoute").objectReferenceValue = route;
                serialized.FindProperty("reason").stringValue = "qa.hub.route." + SanitizeName(target.Label).ToLowerInvariant();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                entries[index] = new QaHubPanel.QaHubEntry(
                    target.Domain,
                    target.Label,
                    trigger);
            }

            panel.Configure(entries, "Immersive Framework QA");
            HubSetupSummary summary = Summarize(scene.name, entries);
            if (!EditorSceneManager.SaveScene(scene, HubScenePath))
            {
                throw new InvalidOperationException(
                    $"Could not save the QA Hub scene at '{HubScenePath}'.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return summary;
        }

        private static HubSetupSummary Summarize(
            string sceneName,
            QaHubPanel.QaHubEntry[] entries)
        {
            QaHubPanel.QaHubEntry[] materializedEntries =
                entries ?? Array.Empty<QaHubPanel.QaHubEntry>();
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uniqueEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int duplicateCount = 0;

            foreach (QaHubPanel.QaHubEntry entry in materializedEntries)
            {
                string domain = string.IsNullOrWhiteSpace(entry.Domain)
                    ? "Other"
                    : entry.Domain.Trim();
                string label = string.IsNullOrWhiteSpace(entry.Label)
                    ? entry.RouteRequestTrigger != null &&
                      entry.RouteRequestTrigger.TargetRoute != null
                        ? entry.RouteRequestTrigger.TargetRoute.RouteName
                        : "Missing Route"
                    : entry.Label.Trim();
                groups.Add(domain);
                if (!uniqueEntries.Add(domain + "\u001f" + label))
                {
                    duplicateCount++;
                }
            }

            return new HubSetupSummary(
                sceneName,
                groups.Count,
                materializedEntries.Length,
                duplicateCount);
        }

        private static string FormatDiagnostic(Exception exception)
        {
            Exception root = exception.GetBaseException();
            return $"{root.GetType().Name}: {root.Message}"
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string SanitizeName(string value) => value.Replace(" / ", "_").Replace(" ", "_");

        private readonly struct HubTarget
        {
            public HubTarget(string domain, string label, string routePath)
            {
                Domain = domain;
                Label = label;
                RoutePath = routePath;
            }

            public string Domain { get; }
            public string Label { get; }
            public string RoutePath { get; }
        }

        private readonly struct HubSetupSummary
        {
            public HubSetupSummary(
                string sceneName,
                int groupCount,
                int entryCount,
                int duplicateCount)
            {
                SceneName = sceneName;
                GroupCount = groupCount;
                EntryCount = entryCount;
                DuplicateCount = duplicateCount;
            }

            public string SceneName { get; }
            public int GroupCount { get; }
            public int EntryCount { get; }
            public int DuplicateCount { get; }
        }
    }
}

using System;
using System.Linq;
using Immersive.Framework.Pause;
using ImmersiveFrameworkQA.PauseP1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.PauseP1.Editor
{
    internal static class QaPauseSceneRequestBindingSetup
    {
        private const string RouteScene =
            "Assets/ImmersiveFrameworkQA/Player/PauseP1/QA_PauseP1Route.unity";
        private const string ActivityScene =
            "Assets/ImmersiveFrameworkQA/Player/PauseP1/QA_PauseP1ActivityContent.unity";
        private const string LogPrefix =
            "[PAUSE_SCENE_REQUEST_BINDING]";

        [MenuItem(
            "Tools/Immersive Framework QA/Pause/Apply Scene Request Binding QA")]
        private static void ApplyMenu()
        {
            try
            {
                Apply(
                    RouteScene,
                    "Route",
                    "qa.pause.route-scene",
                    new Rect(16f, 220f, 440f, 250f));
                Apply(
                    ActivityScene,
                    "Activity",
                    "qa.pause.activity-scene",
                    new Rect(500f, 220f, 440f, 250f));
                Validate();
                Debug.Log(
                    $"{LogPrefix} PASS. " +
                    "routeTrigger='1' activityTrigger='1' " +
                    "routeProbe='1' activityProbe='1'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. " +
                    $"reason='{Sanitize(exception.GetBaseException().Message)}'");
                throw;
            }
        }

        [MenuItem(
            "Tools/Immersive Framework QA/Pause/Validate Scene Request Binding QA")]
        private static void ValidateMenu()
        {
            try
            {
                Validate();
                Debug.Log(
                    $"{LogPrefix} PASS. " +
                    "Route and Activity scenes each contain one explicit " +
                    "PauseRequestTrigger and one matching probe.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. " +
                    $"reason='{Sanitize(exception.GetBaseException().Message)}'");
                throw;
            }
        }

        internal static void Validate()
        {
            ValidateScene(RouteScene, "Route");
            ValidateScene(ActivityScene, "Activity");
        }

        private static void Apply(
            string path,
            string scope,
            string reason,
            Rect panelRect)
        {
            WithScene(
                path,
                scene =>
                {
                    Component[] components = Components(scene);
                    PauseRequestTrigger[] triggers =
                        components
                            .OfType<PauseRequestTrigger>()
                            .ToArray();
                    PauseSceneRequestBindingProbe[] probes =
                        components
                            .OfType<
                                PauseSceneRequestBindingProbe>()
                            .ToArray();

                    Require(
                        triggers.Length <= 1,
                        $"{scope} scene contains duplicate PauseRequestTrigger components.");
                    Require(
                        probes.Length <= 1,
                        $"{scope} scene contains duplicate PauseSceneRequestBindingProbe components.");

                    GameObject target = probes.Length == 1
                        ? probes[0].gameObject
                        : triggers.Length == 1
                            ? triggers[0].gameObject
                            : scene.GetRootGameObjects()
                                .FirstOrDefault();

                    if (target == null)
                    {
                        target = new GameObject(
                            $"{scope} Pause Request Binding QA");
                        SceneManager.MoveGameObjectToScene(
                            target,
                            scene);
                    }

                    PauseRequestTrigger trigger =
                        triggers.Length == 1
                            ? triggers[0]
                            : target.AddComponent<
                                PauseRequestTrigger>();
                    SetString(
                        trigger,
                        "reason",
                        reason);

                    PauseSceneRequestBindingProbe probe =
                        probes.Length == 1
                            ? probes[0]
                            : target.AddComponent<
                                PauseSceneRequestBindingProbe>();
                    probe.Configure(
                        trigger,
                        scope,
                        panelRect);
                    EditorUtility.SetDirty(trigger);
                    EditorUtility.SetDirty(probe);
                    EditorSceneManager.MarkSceneDirty(scene);
                    Require(
                        EditorSceneManager.SaveScene(
                            scene,
                            path,
                            false),
                        $"Could not save {scope} scene.");
                });
        }

        private static void ValidateScene(
            string path,
            string scope)
        {
            WithScene(
                path,
                scene =>
                {
                    Component[] components = Components(scene);
                    PauseRequestTrigger[] triggers =
                        components
                            .OfType<PauseRequestTrigger>()
                            .ToArray();
                    PauseSceneRequestBindingProbe[] probes =
                        components
                            .OfType<
                                PauseSceneRequestBindingProbe>()
                            .ToArray();

                    Require(
                        triggers.Length == 1,
                        $"{scope} scene requires exactly one PauseRequestTrigger; found '{triggers.Length}'.");
                    Require(
                        probes.Length == 1,
                        $"{scope} scene requires exactly one PauseSceneRequestBindingProbe; found '{probes.Length}'.");
                    Require(
                        ReferenceEquals(
                            probes[0].PauseRequestTrigger,
                            triggers[0]),
                        $"{scope} probe must reference the exact scene PauseRequestTrigger.");
                    Require(
                        string.Equals(
                            probes[0].ScopeLabel,
                            scope,
                            StringComparison.Ordinal),
                        $"{scope} probe label is invalid.");
                });
        }

        private static void WithScene(
            string path,
            Action<Scene> action)
        {
            Scene existing =
                SceneManager.GetSceneByPath(path);
            bool opened =
                !existing.IsValid() ||
                !existing.isLoaded;
            Scene scene = opened
                ? EditorSceneManager.OpenScene(
                    path,
                    OpenSceneMode.Additive)
                : existing;

            try
            {
                action(scene);
            }
            finally
            {
                if (opened &&
                    scene.IsValid() &&
                    scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(
                        scene,
                        true);
                }
            }
        }

        private static Component[] Components(
            Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(
                    root =>
                        root.GetComponentsInChildren<
                            Component>(true))
                .Where(component => component != null)
                .ToArray();

        private static void SetString(
            UnityEngine.Object target,
            string property,
            string value)
        {
            var serialized =
                new SerializedObject(target);
            SerializedProperty field =
                serialized.FindProperty(property);
            Require(
                field != null,
                $"Missing serialized property '{target.GetType().Name}.{property}'.");
            field.stringValue = value ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static string Sanitize(
            string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "’")
                    .Replace(
                        Environment.NewLine,
                        " ");
    }
}

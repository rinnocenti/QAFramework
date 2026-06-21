using Immersive.Framework.Authoring;
using Immersive.Framework.Bootstrap;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Settings
{
    internal static class ImmersiveFrameworkSettingsProvider
    {
        private static FrameworkAuthoringValidationReport _lastAuthoringValidationReport;
        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/Immersive Framework", SettingsScope.Project)
            {
                label = "Immersive Framework",
                guiHandler = _ => DrawSettingsGui(),
                keywords = new[]
                {
                    "Immersive",
                    "Framework",
                    "Game Application",
                    "Validation Mode",
                    "Bootstrap",
                    "Usage Guide",
                    "Boot Status",
                    "Editor Play Mode",
                    "Current Scene Only",
                    "Authoring Validation",
                    "Validate"
                }
            };
        }

        private static void DrawSettingsGui()
        {
            var settings = ImmersiveFrameworkEditorSettingsUtility.LoadOrCreateSettingsAsset();
            if (settings == null)
            {
                EditorGUILayout.HelpBox(
                    "Unable to create Immersive Framework settings asset.",
                    MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Immersive Framework", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Project-level entry point for the framework. Assign one Game Application here; gameplay lifecycle concepts are added only when a real framework cut needs them.",
                MessageType.Info);

            var serializedSettings = new SerializedObject(settings);
            var activeGameApplication = serializedSettings.FindProperty("activeGameApplication");
            var editorPlayModeStartup = serializedSettings.FindProperty("editorPlayModeStartup");

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Editor Play Mode", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Controls how the framework behaves when entering Play Mode in the Unity Editor. Player builds always use Framework Startup.",
                MessageType.None);
            EditorGUILayout.PropertyField(editorPlayModeStartup, new GUIContent("Startup"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Application", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(activeGameApplication, new GUIContent("Active Game Application"));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create and Assign Game Application"))
                {
                    var created = ImmersiveFrameworkEditorSettingsUtility.CreateGameApplicationAsset();
                    if (created != null)
                    {
                        activeGameApplication.objectReferenceValue = created;
                        Selection.activeObject = created;
                    }
                }

                using (new EditorGUI.DisabledScope(activeGameApplication.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Game Application"))
                    {
                        Selection.activeObject = activeGameApplication.objectReferenceValue;
                    }
                }
            }

            serializedSettings.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            DrawBootStatus(settings);

            EditorGUILayout.Space(8);
            DrawAuthoringValidation(settings);

            EditorGUILayout.Space(8);
            DrawConfigurationFiles();

            EditorGUILayout.Space(8);
            DrawCurrentScope();
        }

        private static void DrawBootStatus(ImmersiveFrameworkSettingsAsset settings)
        {
            EditorGUILayout.LabelField("Boot Status", EditorStyles.boldLabel);

            if (settings.EditorPlayModeStartup == FrameworkEditorPlayModeStartup.CurrentSceneOnly)
            {
                EditorGUILayout.HelpBox(
                    "Skipped in Editor Play Mode: Startup is set to Current Scene Only. The open scene will run without Game Application, Game Flow, or Scene Lifecycle boot.",
                    MessageType.Info);
                return;
            }

            var bootStatus = FrameworkBootValidator.Validate(settings);
            if (bootStatus.Succeeded)
            {
                EditorGUILayout.HelpBox(
                    $"Ready: {bootStatus.Message} Validation Mode: {bootStatus.ValidationMode}.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Required: {bootStatus.Message}",
                    MessageType.Error);
            }
        }


        private static void DrawAuthoringValidation(ImmersiveFrameworkSettingsAsset settings)
        {
            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Checks the current framework authoring baseline: Active Game Application, Startup Route, Primary Scene, optional Startup Activity, and scene-authored Activity Content Bindings. This does not create runtime lifecycle or configure Actor, Input, Camera, Save, or Pooling.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Authoring"))
                {
                    _lastAuthoringValidationReport = FrameworkAuthoringValidator.ValidateProjectSettings(settings, true);
                    FrameworkAuthoringValidationGui.LogReport("Project Settings", _lastAuthoringValidationReport);
                }

                using (new EditorGUI.DisabledScope(_lastAuthoringValidationReport == null))
                {
                    if (GUILayout.Button("Clear Result"))
                    {
                        _lastAuthoringValidationReport = null;
                    }
                }
            }

            if (_lastAuthoringValidationReport == null)
            {
                EditorGUILayout.HelpBox(
                    "Run validation to produce an explicit authoring report for the current open scenes.",
                    MessageType.Info);
                return;
            }

            FrameworkAuthoringValidationGui.DrawSummary(_lastAuthoringValidationReport);
            FrameworkAuthoringValidationGui.DrawIssues(_lastAuthoringValidationReport, false);
        }

        private static void DrawConfigurationFiles()
        {
            EditorGUILayout.LabelField("Configuration Files", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Settings Asset", ImmersiveFrameworkEditorSettingsUtility.SettingsPath);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Settings Asset"))
                {
                    ImmersiveFrameworkEditorSettingsUtility.SelectSettingsAsset();
                }

                if (GUILayout.Button("Open Usage Guide"))
                {
                    ImmersiveFrameworkEditorSettingsUtility.OpenUsageGuide();
                }
            }
        }

        private static void DrawCurrentScope()
        {
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This settings page currently assigns the active Game Application, controls Editor Play Mode startup, and previews boot validation. Activity, Actor, Input, Camera, Save, and Pooling are intentionally not configured here yet.",
                MessageType.None);
        }
    }
}

using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Validation;
using UnityEditor;
using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityAsset))]
    internal sealed class ActivityAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _activityName;
        private SerializedProperty _description;

        private void OnEnable()
        {
            _activityName = serializedObject.FindProperty("activityName");
            _description = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Activity", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "An Activity is a gameplay step inside a Route. This cut only starts the Activity by identity; content and gameplay integrations are added later.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_activityName, new GUIContent("Activity Name"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Activity is an identity target for Activity Flow and scene-authored ActivityLocalVisibilityAdapter. It does not bind actors, control input, camera, save, pause or pooling yet.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }
        private void DrawAuthoringValidation()
        {
            var report = FrameworkAuthoringValidator.ValidateActivity((ActivityAsset)target);

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }
    }
}

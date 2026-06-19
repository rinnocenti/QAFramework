using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(ActivityAsset))]
    internal sealed class ActivityAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty activityName;
        private SerializedProperty description;

        private void OnEnable()
        {
            activityName = serializedObject.FindProperty("activityName");
            description = serializedObject.FindProperty("description");
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
            EditorGUILayout.PropertyField(activityName, new GUIContent("Activity Name"));
            EditorGUILayout.PropertyField(description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Activity currently has identity only. It does not load content, bind actors, control input, camera, save, pause or pooling yet.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }
    }
}

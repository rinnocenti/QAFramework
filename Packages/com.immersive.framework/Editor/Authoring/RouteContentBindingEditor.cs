using Immersive.Framework.RouteLifecycle;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(RouteContentBinding))]
    [CanEditMultipleObjects]
    internal sealed class RouteContentBindingEditor : UnityEditor.Editor
    {
        private SerializedProperty _route;
        private SerializedProperty _localContentId;

        private void OnEnable()
        {
            _route = serializedObject.FindProperty("route");
            _localContentId = serializedObject.FindProperty("localContentId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Route Content Binding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _route,
                new GUIContent(
                    "Route",
                    "Route asset that owns this scene. Use the Route whose Primary Scene is this scene."));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Local Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _localContentId,
                new GUIContent(
                    "Local Content Id",
                    "Explicit local id required by F5 local identity. GameObject name and hierarchy path are diagnostics only and are not used as fallback."));

            if (_localContentId != null && _localContentId.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(
                    "Multiple selected Route Content Bindings have different Local Content Id values.",
                    MessageType.Info);
            }
            else if (_localContentId == null || string.IsNullOrWhiteSpace(_localContentId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Local Content Id is required for F5 local identity. GameObject names and hierarchy paths are diagnostics only and are not fallback identities.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"This Route local contribution uses explicit local id '{_localContentId.stringValue.Trim()}'.",
                    MessageType.Info);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}

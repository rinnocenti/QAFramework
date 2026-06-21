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

        private void OnEnable()
        {
            _route = serializedObject.FindProperty("route");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(
                _route,
                new GUIContent(
                    "Route",
                    "Route asset that owns this scene. Use the Route whose Primary Scene is this scene."));
            serializedObject.ApplyModifiedProperties();
        }
    }
}

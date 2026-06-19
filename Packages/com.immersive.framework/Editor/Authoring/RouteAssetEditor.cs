using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(RouteAsset))]
    internal sealed class RouteAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty routeName;
        private SerializedProperty primaryScenePath;
        private SerializedProperty primarySceneName;
        private SerializedProperty description;

        private void OnEnable()
        {
            routeName = serializedObject.FindProperty("routeName");
            primaryScenePath = serializedObject.FindProperty("primaryScenePath");
            primarySceneName = serializedObject.FindProperty("primarySceneName");
            description = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Route", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A Route is an entry in the Game Flow. This cut declares the Primary Scene but does not load it yet.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(routeName, new GUIContent("Route Name"));
            EditorGUILayout.PropertyField(description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            DrawPrimaryScene();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Route currently declares identity and one Primary Scene. Scene loading, transitions, Activity, Input, Camera, Save, and Pooling are intentionally not part of this Route yet.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPrimaryScene()
        {
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);

            var currentScene = LoadCurrentSceneAsset();
            var selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent("Primary Scene"),
                currentScene,
                typeof(SceneAsset),
                false);

            if (selectedScene != currentScene)
            {
                SetPrimaryScene(selectedScene);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Scene Path", primaryScenePath.stringValue ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(primaryScenePath.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Primary Scene is required for the Startup Route to pass boot validation.",
                    MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Primary Scene is declared for validation and diagnostics. The framework still does not load scenes in this cut.",
                    MessageType.Info);
            }
        }

        private SceneAsset LoadCurrentSceneAsset()
        {
            if (string.IsNullOrWhiteSpace(primaryScenePath.stringValue))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(primaryScenePath.stringValue);
        }

        private void SetPrimaryScene(SceneAsset sceneAsset)
        {
            if (sceneAsset == null)
            {
                primaryScenePath.stringValue = string.Empty;
                primarySceneName.stringValue = string.Empty;
                return;
            }

            primaryScenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
            primarySceneName.stringValue = sceneAsset.name;
        }
    }
}

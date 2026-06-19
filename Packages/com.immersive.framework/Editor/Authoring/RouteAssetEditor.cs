using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Editor.Settings;
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
        private SerializedProperty startupActivity;
        private SerializedProperty description;

        private void OnEnable()
        {
            routeName = serializedObject.FindProperty("routeName");
            primaryScenePath = serializedObject.FindProperty("primaryScenePath");
            primarySceneName = serializedObject.FindProperty("primarySceneName");
            startupActivity = serializedObject.FindProperty("startupActivity");
            description = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Route", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "A Route is an entry in the Game Flow. It declares a Primary Scene and can optionally declare the first Activity to start after the scene is resolved.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(routeName, new GUIContent("Route Name"));
            EditorGUILayout.PropertyField(description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            DrawPrimaryScene();

            EditorGUILayout.Space(6);
            DrawStartupActivity();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Route currently declares identity, one Primary Scene, and an optional Startup Activity reference. Activity content, actors, input, camera, save, pause and pooling are intentionally not part of this Route yet.",
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
                    "Primary Scene is loaded by Scene Lifecycle when this Route starts.",
                    MessageType.Info);
            }
        }

        private void DrawStartupActivity()
        {
            EditorGUILayout.LabelField("Activity", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(startupActivity, new GUIContent("Startup Activity"));
            EditorGUILayout.HelpBox(
                "Optional. If assigned, Activity Flow starts this Activity after the Route Primary Scene is resolved. The Activity currently has identity only; it does not load content, actors, input, camera, save or pause.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create and Assign Startup Activity"))
                {
                    var activity = ImmersiveFrameworkEditorSettingsUtility.CreateStartupActivityAsset();
                    if (activity != null)
                    {
                        startupActivity.objectReferenceValue = activity;
                        Selection.activeObject = activity;
                    }
                }

                using (new EditorGUI.DisabledScope(startupActivity.objectReferenceValue == null))
                {
                    if (GUILayout.Button("Select Startup Activity"))
                    {
                        Selection.activeObject = startupActivity.objectReferenceValue;
                    }
                }
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

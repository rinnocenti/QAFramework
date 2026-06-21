using Immersive.Framework.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Editor.Authoring
{
    [CustomEditor(typeof(RouteContentProfileAsset))]
    internal sealed class RouteContentProfileAssetEditor : UnityEditor.Editor
    {
        private SerializedProperty _profileId;
        private SerializedProperty _additionalScenes;
        private SerializedProperty _description;

        private void OnEnable()
        {
            _profileId = serializedObject.FindProperty("profileId");
            _additionalScenes = serializedObject.FindProperty("additionalScenes");
            _description = serializedObject.FindProperty("description");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Route Content Profile", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Declares Route-owned content for planning. In this baseline, additional scenes are recorded in the Route Content Plan but are not loaded yet.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.PropertyField(_profileId, new GUIContent("Profile Id"));
            EditorGUILayout.PropertyField(_description, new GUIContent("Description"));

            EditorGUILayout.Space(6);
            DrawAdditionalScenes();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAdditionalScenes()
        {
            EditorGUILayout.LabelField("Additional Route Scenes", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These scenes are declarations only in this cut. They become additive Route scene materialization inputs in a later cut.",
                MessageType.None);

            for (var i = 0; i < _additionalScenes.arraySize; i++)
            {
                var entry = _additionalScenes.GetArrayElementAtIndex(i);
                DrawSceneEntry(i, entry);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Scene"))
                {
                    _additionalScenes.InsertArrayElementAtIndex(_additionalScenes.arraySize);
                    var entry = _additionalScenes.GetArrayElementAtIndex(_additionalScenes.arraySize - 1);
                    ResetSceneEntry(entry);
                }

                using (new EditorGUI.DisabledScope(_additionalScenes.arraySize == 0))
                {
                    if (GUILayout.Button("Remove Last"))
                    {
                        _additionalScenes.DeleteArrayElementAtIndex(_additionalScenes.arraySize - 1);
                    }
                }
            }
        }

        private void DrawSceneEntry(int index, SerializedProperty entry)
        {
            var contentId = entry.FindPropertyRelative("contentId");
            var scenePath = entry.FindPropertyRelative("scenePath");
            var sceneName = entry.FindPropertyRelative("sceneName");
            var requiredness = entry.FindPropertyRelative("requiredness");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Scene {index + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    _additionalScenes.DeleteArrayElementAtIndex(index);
                    EditorGUILayout.EndVertical();
                    return;
                }
            }

            EditorGUILayout.PropertyField(contentId, new GUIContent("Content Id"));

            var currentScene = LoadSceneAsset(scenePath.stringValue);
            var selectedScene = (SceneAsset)EditorGUILayout.ObjectField(
                new GUIContent("Scene"),
                currentScene,
                typeof(SceneAsset),
                false);

            if (selectedScene != currentScene)
            {
                SetScene(scenePath, sceneName, selectedScene);
            }

            EditorGUILayout.PropertyField(requiredness, new GUIContent("Requiredness"));

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Scene Path", scenePath.stringValue ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(scenePath.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Scene is missing. This declaration is ignored by the runtime plan until a scene is assigned.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private static SceneAsset LoadSceneAsset(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        private static void SetScene(
            SerializedProperty scenePath,
            SerializedProperty sceneName,
            SceneAsset sceneAsset)
        {
            if (sceneAsset == null)
            {
                scenePath.stringValue = string.Empty;
                sceneName.stringValue = string.Empty;
                return;
            }

            scenePath.stringValue = AssetDatabase.GetAssetPath(sceneAsset);
            sceneName.stringValue = sceneAsset.name;
        }

        private static void ResetSceneEntry(SerializedProperty entry)
        {
            entry.FindPropertyRelative("contentId").stringValue = string.Empty;
            entry.FindPropertyRelative("scenePath").stringValue = string.Empty;
            entry.FindPropertyRelative("sceneName").stringValue = string.Empty;
            entry.FindPropertyRelative("requiredness").enumValueIndex = 0;
        }
    }
}

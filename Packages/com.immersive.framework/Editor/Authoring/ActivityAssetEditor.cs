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
        private SerializedProperty _visualTransitionMode;

        private void OnEnable()
        {
            _activityName = serializedObject.FindProperty("activityName");
            _description = serializedObject.FindProperty("description");
            _visualTransitionMode = serializedObject.FindProperty("visualTransitionMode");
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
            DrawVisualOperationPolicy();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This Activity is an identity target for Activity Flow and scene-authored ActivityLocalVisibilityAdapter. It can request a Session UIGlobal transition by policy, but it does not own TransitionSurface, LoadingSurface, actors, input, camera, save, pause or pooling.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }

        private void DrawVisualOperationPolicy()
        {
            EditorGUILayout.LabelField("Visual Operation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(
                _visualTransitionMode,
                new GUIContent(
                    "Transition Mode",
                    "Controls whether Activity requests use the Session UIGlobal TransitionSurface. Route transitions remain mandatory and are not configured here."));

            var mode = _visualTransitionMode != null && !_visualTransitionMode.hasMultipleDifferentValues
                ? (ActivityVisualTransitionMode)_visualTransitionMode.intValue
                : ActivityVisualTransitionMode.Seamless;

            switch (mode)
            {
                case ActivityVisualTransitionMode.Seamless:
                    EditorGUILayout.HelpBox(
                        "Default. Activity switch/clear runs without fade. Use this for local or seamless Activity changes inside the same Route.",
                        MessageType.Info);
                    break;
                case ActivityVisualTransitionMode.Fade:
                    EditorGUILayout.HelpBox(
                        "Activity switch/clear uses the Session UIGlobal TransitionSurface fade. Loading remains skipped unless a future Activity content/scene loading source exists.",
                        MessageType.Info);
                    break;
                case ActivityVisualTransitionMode.FadeWithLoading:
                    EditorGUILayout.HelpBox(
                        "Reserved for future Activity scene/content loading. In the current runtime it behaves as Fade and keeps Loading skipped because Activity loading has no real progress/content source yet.",
                        MessageType.Warning);
                    break;
            }
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

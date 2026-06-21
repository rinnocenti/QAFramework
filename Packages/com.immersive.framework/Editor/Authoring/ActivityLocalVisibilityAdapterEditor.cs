using Immersive.Framework.ActivityFlow;
using Immersive.Framework.Authoring;
using Immersive.Framework.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace Immersive.Framework.Editor.Authoring
{
    [CustomEditor(typeof(ActivityLocalVisibilityAdapter))]
    [CanEditMultipleObjects]
    internal sealed class ActivityLocalVisibilityAdapterEditor : UnityEditor.Editor
    {
        private SerializedProperty _activity;

        private void OnEnable()
        {
            _activity = serializedObject.FindProperty("activity");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Activity Content Binding", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Marks this GameObject as scene-authored content for one Activity. Activity Flow activates it when the assigned Activity is active and deactivates it otherwise.",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Binding", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_activity, new GUIContent("Activity"));

            DrawActivityStatus();
            DrawHierarchyGuardrails();

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Current Scope", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This binding only controls the active state of this GameObject. It does not spawn, pool, reset, save, bind actors, bind input, bind camera or load Activity content.",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            DrawAuthoringValidation();
        }

        private void DrawAuthoringValidation()
        {
            var report = new FrameworkAuthoringValidationReport();
            for (int i = 0; i < targets.Length; i++)
            {
                report.AddRange(FrameworkAuthoringValidator.ValidateActivityContentBinding(targets[i] as ActivityLocalVisibilityAdapter));
            }

            EditorGUILayout.LabelField("Authoring Validation", EditorStyles.boldLabel);
            FrameworkAuthoringValidationGui.DrawSummary(report);
            FrameworkAuthoringValidationGui.DrawIssues(report, false);
        }

        private void DrawActivityStatus()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                if (_activity.hasMultipleDifferentValues)
                {
                    EditorGUILayout.HelpBox(
                        "Multiple selected bindings have different Activity references.",
                        MessageType.Info);
                    return;
                }
            }

            if (_activity.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Activity is missing. This binding will be skipped by Activity Content Runtime and will produce a runtime warning.",
                    MessageType.Error);
                return;
            }

            var activityAsset = _activity.objectReferenceValue as ActivityAsset;
            string activityName = activityAsset != null ? activityAsset.ActivityName : _activity.objectReferenceValue.name;

            EditorGUILayout.HelpBox(
                $"This GameObject is authored as content for Activity '{activityName}'. It will be active only while that Activity is active.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select Activity", GUILayout.Width(140)))
                {
                    Selection.activeObject = _activity.objectReferenceValue;
                }
            }
        }

        private void DrawHierarchyGuardrails()
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            var binding = target as ActivityLocalVisibilityAdapter;
            if (binding == null)
            {
                return;
            }

            var parentBinding = FindParentBinding(binding);
            if (parentBinding != null)
            {
                EditorGUILayout.HelpBox(
                    $"A parent GameObject also has Activity Content Binding: '{parentBinding.gameObject.name}'. Nested Activity content policy does not exist yet. Keep Activity content roots flat for now.",
                    MessageType.Warning);
            }

            int childBindingCount = CountChildBindings(binding);
            if (childBindingCount > 0)
            {
                EditorGUILayout.HelpBox(
                    $"This GameObject contains {childBindingCount} child Activity Content Binding component(s). Nested Activity content policy does not exist yet. Keep Activity content roots flat for now.",
                    MessageType.Warning);
            }
        }

        private static ActivityLocalVisibilityAdapter FindParentBinding(ActivityLocalVisibilityAdapter binding)
        {
            var parent = binding.transform.parent;
            while (parent != null)
            {
                if (parent.TryGetComponent<ActivityLocalVisibilityAdapter>(out var parentBinding))
                {
                    return parentBinding;
                }

                parent = parent.parent;
            }

            return null;
        }

        private static int CountChildBindings(ActivityLocalVisibilityAdapter binding)
        {
            ActivityLocalVisibilityAdapter[] all = binding.GetComponentsInChildren<ActivityLocalVisibilityAdapter>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i] != binding)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

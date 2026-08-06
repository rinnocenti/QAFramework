using System;
using System.Collections.Generic;
using Immersive.Framework.ActivityRestart;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectReset;
using Immersive.Framework.Pause;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.EditorUx.Internal.Editor
{
    internal static class QaCoreAuthoringEditorUxSmoke
    {
        [MenuItem("Immersive Framework/QA/Regressions/Editor UX/Run Core Authoring Editor UX Smoke", priority = 260)]
        private static void Run()
        {
            var evidence = new List<string>();
            Verify<PauseRequestTrigger>("Immersive.Framework.Editor.Pause.PauseRequestTriggerEditor", new[] { "reason" }, evidence);
            Verify<ObjectResetTrigger>("Immersive.Framework.Editor.Editor.Authoring.ObjectResetTriggerEditor", new[] { "targetSubject", "reason", "allowNoParticipants", "stopOnFailure" }, evidence);
            Verify<ObjectResetGroupTrigger>("Immersive.Framework.Reset.Editor.ObjectResetGroupTriggerEditor", new[] { "groupId", "reason", "selection" }, evidence);
            Verify<ActivityRestartTrigger>("Immersive.Framework.Editor.ActivityRestart.ActivityRestartTriggerEditor", new[] { "targetActivity", "reason", "resetSelection" }, evidence);
            Verify<RouteRequestTrigger>("Immersive.Framework.Editor.GameFlow.RouteRequestTriggerEditor", new[] { "targetRoute", "reason" }, evidence);
            Verify<ActivityRequestTrigger>("Immersive.Framework.Editor.GameFlow.ActivityRequestTriggerEditor", new[] { "targetActivity", "reason" }, evidence);
            Debug.Log("[QA_EDITOR_UX_CORE_AUTHORING] status='Passed' evidence='" + string.Join(",", evidence) + "'.");
        }

        private static void Verify<T>(string expectedEditor, IReadOnlyList<string> properties, ICollection<string> evidence) where T : Component
        {
            var root = new GameObject("QA_" + typeof(T).Name);
            UnityEditor.Editor editor = null;
            try
            {
                T component = root.AddComponent<T>();
                editor = UnityEditor.Editor.CreateEditor(component);
                Require(editor != null, typeof(T).Name + " did not resolve a custom Editor.");
                Require(editor.GetType().FullName == expectedEditor, typeof(T).Name + " resolved '" + editor.GetType().FullName + "' instead of '" + expectedEditor + "'.");
                foreach (string property in properties)
                {
                    Require(editor.serializedObject.FindProperty(property) != null, typeof(T).Name + " is missing serialized property '" + property + "'.");
                }

                string before = EditorJsonUtility.ToJson(component);
                editor.OnInspectorGUI();
                string after = EditorJsonUtility.ToJson(component);
                Require(before == after, typeof(T).Name + " Inspector repaint mutated serialized authoring data.");
                evidence.Add(typeof(T).Name + "-editor-and-repaint");
            }
            finally
            {
                if (editor != null) UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}

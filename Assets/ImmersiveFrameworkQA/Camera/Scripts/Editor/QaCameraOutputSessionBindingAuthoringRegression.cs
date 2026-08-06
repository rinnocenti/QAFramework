using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Regression for the persistent Camera Output authoring surface.
    /// The regression uses reflection only for package-internal Editor APIs.
    /// </summary>
    internal static class QaCameraOutputSessionBindingAuthoringRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run Camera Output Session Binding Authoring Regression";

        private const string ValidatorTypeName =
            "Immersive.Framework.Editor.CameraAuthoring.CameraOutputSessionBindingAuthoringValidator";
        private const string EditorTypeName =
            "Immersive.Framework.Editor.CameraAuthoring.CameraOutputSessionBindingEditor";
        private const string BrainTypeName =
            "Unity.Cinemachine.CinemachineBrain";

        private const BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath, priority = 236)]
        private static void Run()
        {
            var completed = new List<string>();

            VerifyAutomaticId(completed);
            VerifyResetPreservesExistingId(completed);
            VerifyGenerateOnlyWhenMissing(completed);
            VerifyValidationCase(completed, "valid-composition", true, true, true, true);
            VerifyValidationCase(completed, "missing-camera", false, true, true, false);
            VerifyValidationCase(completed, "missing-brain", true, false, true, false);
            VerifyValidationCase(completed, "split-camera-brain", true, true, false, false);

            Debug.Log(
                "[QA_CAMERA_OUTPUT_SESSION_BINDING_AUTHORING] " +
                "status='Passed' " +
                $"cases='{completed.Count}' " +
                $"evidence='{string.Join(",", completed)}'.");
        }

        private static void VerifyAutomaticId(
            ICollection<string> completed)
        {
            var root = new GameObject("QA_CameraOutput_AutomaticId");
            try
            {
                CameraOutputSessionBinding binding =
                    root.AddComponent<CameraOutputSessionBinding>();
                Require(
                    !string.IsNullOrWhiteSpace(binding.OutputIdText),
                    "A newly created Camera Output Session Binding did not receive an Output ID.");
                completed.Add("automatic-id");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void VerifyResetPreservesExistingId(
            ICollection<string> completed)
        {
            var root = new GameObject("QA_CameraOutput_PreserveId");
            try
            {
                CameraOutputSessionBinding binding =
                    root.AddComponent<CameraOutputSessionBinding>();
                SetOutputId(binding, "qa.camera.output.existing");

                MethodInfo reset = typeof(CameraOutputSessionBinding).GetMethod(
                    "Reset",
                    InstanceAny);
                Require(reset != null,
                    "Camera Output Reset hook is unavailable.");
                reset.Invoke(binding, null);

                Require(
                    binding.OutputIdText == "qa.camera.output.existing",
                    "Reset replaced an existing Camera Output ID.");
                completed.Add("existing-id-preserved");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void VerifyGenerateOnlyWhenMissing(
            ICollection<string> completed)
        {
            var root = new GameObject("QA_CameraOutput_GenerateId");
            UnityEditor.Editor editor = null;

            try
            {
                CameraOutputSessionBinding binding =
                    root.AddComponent<CameraOutputSessionBinding>();
                SetOutputId(binding, string.Empty);

                Type editorType = ResolveType(EditorTypeName);
                editor = UnityEditor.Editor.CreateEditor(binding, editorType);
                Require(editor != null,
                    "Camera Output custom Editor could not be created.");

                MethodInfo generate = editorType.GetMethod(
                    "GenerateOutputId",
                    InstanceAny);
                Require(generate != null,
                    "Camera Output Generate action is unavailable.");

                generate.Invoke(editor, null);
                string generated = binding.OutputIdText;
                Require(
                    !string.IsNullOrWhiteSpace(generated),
                    "Generate did not fill an empty Camera Output ID.");

                SetOutputId(binding, "qa.camera.output.preserved");
                generate.Invoke(editor, null);
                Require(
                    binding.OutputIdText == "qa.camera.output.preserved",
                    "Generate replaced a populated Camera Output ID.");

                completed.Add("generate-missing-only");
            }
            finally
            {
                if (editor != null)
                {
                    UnityEngine.Object.DestroyImmediate(editor);
                }

                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void VerifyValidationCase(
            ICollection<string> completed,
            string caseName,
            bool includeCamera,
            bool includeBrain,
            bool sameObject,
            bool expectedValid)
        {
            var roots = new List<GameObject>();

            try
            {
                var outputRoot = new GameObject($"QA_{caseName}_Output");
                roots.Add(outputRoot);

                CameraOutputSessionBinding binding =
                    outputRoot.AddComponent<CameraOutputSessionBinding>();

                UnityEngine.Camera camera = includeCamera
                    ? outputRoot.AddComponent<UnityEngine.Camera>()
                    : null;

                Component brain = null;
                if (includeBrain)
                {
                    GameObject brainRoot = outputRoot;
                    if (!sameObject)
                    {
                        brainRoot = new GameObject($"QA_{caseName}_Brain");
                        roots.Add(brainRoot);
                    }

                    brain = brainRoot.AddComponent(ResolveType(BrainTypeName));
                }

                AssignOutputReferences(binding, camera, brain);
                bool isValid = Validate(binding);

                Require(
                    isValid == expectedValid,
                    $"Case '{caseName}' returned unexpected validity='{isValid}'.");
                completed.Add(caseName);
            }
            finally
            {
                for (int index = roots.Count - 1; index >= 0; index--)
                {
                    UnityEngine.Object.DestroyImmediate(roots[index]);
                }
            }
        }

        private static bool Validate(
            CameraOutputSessionBinding binding)
        {
            Type validatorType = ResolveType(ValidatorTypeName);
            MethodInfo validate = validatorType.GetMethod(
                "Validate",
                StaticAny);
            Require(validate != null,
                "Camera Output authoring validator is unavailable.");

            object result = validate.Invoke(null, new object[] { binding });
            Require(result != null,
                "Camera Output authoring validator returned no result.");

            PropertyInfo isValid = result.GetType().GetProperty(
                "IsValid",
                InstanceAny);
            Require(isValid != null,
                "Camera Output validation result does not expose IsValid.");

            return (bool)isValid.GetValue(result);
        }

        private static void SetOutputId(
            CameraOutputSessionBinding binding,
            string value)
        {
            var serialized = new SerializedObject(binding);
            SerializedProperty property = serialized.FindProperty("outputId");
            Require(property != null,
                "Camera Output ID serialized field is unavailable.");
            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignOutputReferences(
            CameraOutputSessionBinding binding,
            UnityEngine.Camera camera,
            Component brain)
        {
            var serialized = new SerializedObject(binding);
            SerializedProperty cameraProperty =
                serialized.FindProperty("unityCamera");
            SerializedProperty brainProperty =
                serialized.FindProperty("cinemachineBrain");

            Require(cameraProperty != null && brainProperty != null,
                "Camera Output component references are unavailable.");

            cameraProperty.objectReferenceValue = camera;
            brainProperty.objectReferenceValue = brain;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Type ResolveType(string fullName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int index = 0; index < assemblies.Length; index++)
            {
                Type type = assemblies[index].GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            throw new InvalidOperationException(
                $"Type '{fullName}' is unavailable.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

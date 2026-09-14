using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// ADR-026 public composition negatives that complement, rather than duplicate,
    /// the real boot/injection proof in the canonical Camera fixture.
    /// </summary>
    internal static class QaPersistentCameraPresentationCompositionRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run Persistent Camera Presentation Composition Regression";
        private const string RetiredInvalidViewportCase =
            "invalid-viewport:SupersededByCAMERA028B";

        [MenuItem(MenuPath, priority = 235)]
        private static void Run()
        {
            IReadOnlyList<string> completed = RunForCertification();
            Debug.Log("[QA_PERSISTENT_CAMERA_PRESENTATION_COMPOSITION] " +
                $"status='Passed' cases='{completed.Count}/{completed.Count}' " +
                $"evidence='{string.Join(",", completed)}' " +
                $"retired='{RetiredInvalidViewportCase}'.");
        }

        internal static IReadOnlyList<string> RunForCertification()
        {
            var completed = new List<string>();
            VerifyOutputTopology(completed, "one-output", 1, false, true);
            VerifyOutputTopology(completed, "two-distinct-outputs", 2, false, true);
            VerifyOutputTopology(completed, "zero-outputs", 0, false, false);
            VerifyOutputTopology(completed, "duplicate-output-id", 2, true, false);
            VerifyEmptyViewOutputTopology(completed);
            VerifyFullDistinctViewOutputTopology(completed);
            VerifyPartialOutputParticipation(completed);
            VerifyOneViewMultipleOutputs(completed);
            VerifyConflictingOutputBinding(completed);
            VerifyUnavailableOutputBinding(completed);
            VerifySimpleAdvancedDistinctOutputs(completed);
            VerifySimpleAdvancedSameOutputConflict(completed);
            Require(completed.Count == 12,
                $"Logical Camera structural case count diverged. expected='12' actual='{completed.Count}'.");
            return completed;
        }

        internal static IReadOnlyList<string> RunAdr004BDuplicateOutputCertification()
        {
            var completed = new List<string>();
            VerifyOutputTopology(completed, "duplicate-output-id", 2, true, false);
            return completed;
        }

        private static void VerifyOutputTopology(
            ICollection<string> completed,
            string caseName,
            int outputCount,
            bool duplicateId,
            bool expectedSuccess)
        {
            var roots = new List<GameObject>();
            var definitions = new List<CameraOutputDefinition>();
            CameraOutputSessionTopology topology = null;
            try
            {
                var outputs = new List<CameraOutputAuthoring>();
                string duplicateStableId = duplicateId
                    ? "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
                    : null;
                for (int index = 0; index < outputCount; index++)
                {
                    CameraOutputDefinition definition =
                        QaCameraAuthoringFixtures.CreateOutputDefinition(
                            duplicateStableId ?? Guid.NewGuid().ToString("N"));
                    definitions.Add(definition);
                    outputs.Add(CreateOutput(roots, definition, index));
                }

                bool succeeded = CameraOutputSessionTopology.TryCreate(
                    outputs, out topology, out string diagnostic);
                Require(succeeded == expectedSuccess,
                    $"Case '{caseName}' returned unexpected success='{succeeded}' diagnostic='{diagnostic}'.");
                if (succeeded)
                {
                    Require(topology != null && topology.OutputCount == outputCount,
                        $"Case '{caseName}' did not retain exact output cardinality.");
                }
                else
                {
                    Require(!string.IsNullOrWhiteSpace(diagnostic),
                        $"Case '{caseName}' blocked without diagnostic.");
                }
                completed.Add(caseName);
            }
            finally
            {
                topology?.Dispose();
                for (int index = roots.Count - 1; index >= 0; index--)
                    UnityEngine.Object.DestroyImmediate(roots[index]);
                for (int index = definitions.Count - 1; index >= 0; index--)
                    QaCameraAuthoringFixtures.Destroy(definitions[index]);
            }
        }

        private static CameraOutputAuthoring CreateOutput(
            ICollection<GameObject> roots,
            CameraOutputDefinition outputDefinition,
            int index)
        {
            var root = new GameObject($"QA_ADR026_Output_{index}");
            root.SetActive(false);
            roots.Add(root);
            UnityEngine.Camera camera = root.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain = root.AddComponent<CinemachineBrain>();
            var rigRoot = new GameObject($"Rig_{index}");
            rigRoot.transform.SetParent(root.transform, false);
            CameraRigComposer composer = rigRoot.AddComponent<CameraRigComposer>();
            CinemachineCamera cinemachine = rigRoot.AddComponent<CinemachineCamera>();
            composer.EditorSetGeneratedReference(cinemachine);
            CameraOutputAuthoring output = root.AddComponent<CameraOutputAuthoring>();
            Set(output, "outputDefinition", outputDefinition);
            Set(output, "unityCamera", camera);
            Set(output, "cinemachineBrain", brain);
            Set(output, "defaultCameraRig", composer);
            Set(output, "initializeOnAwake", false);
            Set(output, "logDiagnostics", false);
            return output;
        }

        private static void VerifyEmptyViewOutputTopology(ICollection<string> completed)
        {
            bool succeeded = CameraViewOutputTopology.TryCreate(
                Array.Empty<CameraViewOutputBinding>(),
                out CameraViewOutputTopology topology,
                out string diagnostic);
            Require(succeeded && topology != null && topology.BindingCount == 0,
                $"Empty logical View-to-Output topology must be valid. diagnostic='{diagnostic}'.");
            completed.Add("empty-view-output-topology");
        }

        private static void VerifyFullDistinctViewOutputTopology(ICollection<string> completed)
        {
            var roots = new List<GameObject>();
            var definitions = new List<ScriptableObject>();
            try
            {
                CameraViewDefinition viewA = QaCameraAuthoringFixtures.CreateViewDefinition();
                CameraViewDefinition viewB = QaCameraAuthoringFixtures.CreateViewDefinition();
                CameraOutputDefinition outputA = QaCameraAuthoringFixtures.CreateOutputDefinition();
                CameraOutputDefinition outputB = QaCameraAuthoringFixtures.CreateOutputDefinition();
                definitions.Add(viewA);
                definitions.Add(viewB);
                definitions.Add(outputA);
                definitions.Add(outputB);
                CameraOutputAuthoring physicalA = CreateOutput(roots, outputA, 0);
                CameraOutputAuthoring physicalB = CreateOutput(roots, outputB, 1);

                var first = new CameraViewOutputBindingAuthoring();
                var second = new CameraViewOutputBindingAuthoring();
                first.Configure(viewA, outputA);
                second.Configure(viewB, outputB);
                var policyRoot = new GameObject("QA_ADR028B_FullLogicalTopology");
                policyRoot.SetActive(false);
                roots.Add(policyRoot);
                CameraViewOutputPolicyAuthoring policy =
                    policyRoot.AddComponent<CameraViewOutputPolicyAuthoring>();
                policy.Configure(new[] { first, second });

                Require(policy.TryValidateOutputs(new[] { physicalA, physicalB }, out string outputIssue),
                    $"Full logical policy did not resolve both available physical Outputs. {outputIssue}");
                Require(policy.TryBuildTopology(out CameraViewOutputTopology topology, out string topologyIssue) &&
                        topology != null && topology.BindingCount == 2 &&
                        topology.TryGetBinding(viewA.ViewId, outputA.OutputId, out _) &&
                        topology.TryGetBinding(viewB.ViewId, outputB.OutputId, out _),
                    $"Full distinct logical View-to-Output topology failed. {topologyIssue}");
                completed.Add("full-distinct-view-output-associations");
            }
            finally
            {
                Destroy(roots, definitions);
            }
        }

        private static void VerifyPartialOutputParticipation(ICollection<string> completed)
        {
            var roots = new List<GameObject>();
            var definitions = new List<ScriptableObject>();
            try
            {
                CameraViewDefinition view = QaCameraAuthoringFixtures.CreateViewDefinition();
                CameraOutputDefinition outputA = QaCameraAuthoringFixtures.CreateOutputDefinition();
                CameraOutputDefinition outputB = QaCameraAuthoringFixtures.CreateOutputDefinition();
                definitions.Add(view);
                definitions.Add(outputA);
                definitions.Add(outputB);
                CameraOutputAuthoring physicalA = CreateOutput(roots, outputA, 0);
                CameraOutputAuthoring physicalB = CreateOutput(roots, outputB, 1);
                CameraViewOutputPolicyAuthoring policy = CreatePolicy(roots, view, outputA);

                Require(policy.TryValidateOutputs(new[] { physicalA, physicalB }, out string outputIssue),
                    $"Partial policy did not accept its exact available Output subset. {outputIssue}");
                Require(policy.TryBuildTopology(out CameraViewOutputTopology topology, out string topologyIssue) &&
                        topology != null && topology.BindingCount == 1 &&
                        topology.TryGetBinding(view.ViewId, outputA.OutputId, out _) &&
                        !topology.TryGetBinding(outputB.OutputId, out _),
                    $"Partial logical participation did not retain only Output A. {topologyIssue}");
                completed.Add("partial-output-participation");
            }
            finally
            {
                Destroy(roots, definitions);
            }
        }

        private static void VerifyOneViewMultipleOutputs(ICollection<string> completed)
        {
            var view = new CameraViewId("qa.camera.view.shared");
            var outputA = new CameraOutputId("qa.camera.output.a");
            var outputB = new CameraOutputId("qa.camera.output.b");
            bool succeeded = CameraViewOutputTopology.TryCreate(
                new[]
                {
                    new CameraViewOutputBinding(view, outputA),
                    new CameraViewOutputBinding(view, outputB)
                },
                out CameraViewOutputTopology topology,
                out string diagnostic);
            Require(succeeded && topology != null && topology.BindingCount == 2 &&
                    topology.GetBindings(view).Count == 2,
                $"One View must retain two explicit logical Output associations. diagnostic='{diagnostic}'.");
            completed.Add("one-view-multiple-outputs");
        }

        private static void VerifyConflictingOutputBinding(ICollection<string> completed)
        {
            var output = new CameraOutputId("qa.camera.output.shared");
            bool succeeded = CameraViewOutputTopology.TryCreate(
                new[]
                {
                    new CameraViewOutputBinding(new CameraViewId("qa.camera.view.a"), output),
                    new CameraViewOutputBinding(new CameraViewId("qa.camera.view.b"), output)
                },
                out _,
                out string diagnostic);
            Require(!succeeded && !string.IsNullOrWhiteSpace(diagnostic) &&
                    diagnostic.IndexOf("conflicting bindings", StringComparison.Ordinal) >= 0,
                $"Two Views targeting one Output must be rejected explicitly. diagnostic='{diagnostic}'.");
            completed.Add("conflicting-output-binding");
        }

        private static void VerifyUnavailableOutputBinding(ICollection<string> completed)
        {
            var roots = new List<GameObject>();
            var definitions = new List<ScriptableObject>();
            try
            {
                CameraViewDefinition view = QaCameraAuthoringFixtures.CreateViewDefinition();
                CameraOutputDefinition available = QaCameraAuthoringFixtures.CreateOutputDefinition();
                CameraOutputDefinition unavailable = QaCameraAuthoringFixtures.CreateOutputDefinition();
                definitions.Add(view);
                definitions.Add(available);
                definitions.Add(unavailable);
                CameraOutputAuthoring physical = CreateOutput(roots, available, 0);
                CameraViewOutputPolicyAuthoring policy = CreatePolicy(roots, view, unavailable);

                bool accepted = policy.TryValidateOutputs(new[] { physical }, out string diagnostic);
                Require(!accepted && !string.IsNullOrWhiteSpace(diagnostic) &&
                        diagnostic.IndexOf("no exact physical Output", StringComparison.Ordinal) >= 0,
                    $"Binding to an unavailable physical Output must be rejected explicitly. diagnostic='{diagnostic}'.");
                completed.Add("unavailable-output-binding-rejected");
            }
            finally
            {
                Destroy(roots, definitions);
            }
        }

        private static void VerifySimpleAdvancedDistinctOutputs(ICollection<string> completed)
        {
            var roots = new List<GameObject>();
            var definitions = new List<ScriptableObject>();
            try
            {
                CameraViewDefinition viewA = QaCameraAuthoringFixtures.CreateViewDefinition();
                CameraViewDefinition viewB = QaCameraAuthoringFixtures.CreateViewDefinition();
                CameraOutputDefinition outputA = QaCameraAuthoringFixtures.CreateOutputDefinition();
                CameraOutputDefinition outputB = QaCameraAuthoringFixtures.CreateOutputDefinition();
                definitions.Add(viewA);
                definitions.Add(viewB);
                definitions.Add(outputA);
                definitions.Add(outputB);

                CameraSharedComposition composition = CreateComposition(
                    roots, viewA, outputA);
                CameraViewOutputPolicyAuthoring policy = CreatePolicy(
                    roots, viewB, outputB);

                Require(composition.TryCreateAssociationBinding(
                        out CameraViewOutputBinding simple, out string simpleIssue),
                    $"Simple association failed. {simpleIssue}");
                Require(policy.TryBuildTopology(
                        out CameraViewOutputTopology advanced, out string advancedIssue) &&
                    advanced != null && advanced.BindingCount == 1,
                    $"Advanced policy must author one distinct Output association. {advancedIssue}");
                Require(!advanced.TryGetBinding(simple.OutputId, out _),
                    "Advanced policy bound the simple Output.");
                Require(advanced.TryGetBinding(viewB.ViewId, outputB.OutputId, out CameraViewOutputBinding policyBinding),
                    "Advanced policy did not project the Secondary Output association.");

                bool succeeded = CameraViewOutputTopology.TryCreate(
                    new[] { simple, policyBinding },
                    out CameraViewOutputTopology aggregate,
                    out string diagnostic);
                Require(succeeded && aggregate != null && aggregate.BindingCount == 2,
                    $"Simple + advanced distinct Outputs must form a valid aggregate. diagnostic='{diagnostic}'.");
                Require(
                    aggregate.TryGetBinding(viewA.ViewId, outputA.OutputId, out _) &&
                    aggregate.TryGetBinding(viewB.ViewId, outputB.OutputId, out _),
                    "Aggregate topology did not retain exact simple and advanced associations.");
                completed.Add("simple-advanced-distinct-outputs");
            }
            finally
            {
                for (int index = roots.Count - 1; index >= 0; index--)
                    UnityEngine.Object.DestroyImmediate(roots[index]);
                for (int index = definitions.Count - 1; index >= 0; index--)
                    QaCameraAuthoringFixtures.Destroy(definitions[index]);
            }
        }

        private static void VerifySimpleAdvancedSameOutputConflict(ICollection<string> completed)
        {
            var roots = new List<GameObject>();
            var definitions = new List<ScriptableObject>();
            try
            {
                CameraViewDefinition viewA = QaCameraAuthoringFixtures.CreateViewDefinition();
                CameraViewDefinition viewB = QaCameraAuthoringFixtures.CreateViewDefinition();
                CameraOutputDefinition output = QaCameraAuthoringFixtures.CreateOutputDefinition();
                definitions.Add(viewA);
                definitions.Add(viewB);
                definitions.Add(output);

                CameraSharedComposition composition = CreateComposition(
                    roots, viewA, output);
                CameraViewOutputPolicyAuthoring policy = CreatePolicy(
                    roots, viewB, output);

                Require(composition.TryCreateAssociationBinding(
                        out CameraViewOutputBinding simple, out string simpleIssue),
                    $"Simple association failed. {simpleIssue}");
                Require(policy.TryBuildTopology(
                        out CameraViewOutputTopology advanced, out string advancedIssue) &&
                    advanced != null && advanced.BindingCount == 1,
                    $"Advanced policy failed. {advancedIssue}");
                Require(advanced.TryGetBinding(simple.OutputId, out CameraViewOutputBinding policyBinding),
                    "Conflict case requires the advanced policy to target the same Output.");

                bool succeeded = CameraViewOutputTopology.TryCreate(
                    new[] { simple, policyBinding },
                    out _,
                    out string diagnostic);
                Require(!succeeded,
                    "Simple + advanced associations for the same Output must block.");
                Require(!string.IsNullOrWhiteSpace(diagnostic) && diagnostic.IndexOf(
                        "conflicting bindings", StringComparison.Ordinal) >= 0,
                    $"Same-Output conflict did not report conflicting bindings. diagnostic='{diagnostic}'.");
                completed.Add("simple-advanced-same-output-conflict");
            }
            finally
            {
                for (int index = roots.Count - 1; index >= 0; index--)
                    UnityEngine.Object.DestroyImmediate(roots[index]);
                for (int index = definitions.Count - 1; index >= 0; index--)
                    QaCameraAuthoringFixtures.Destroy(definitions[index]);
            }
        }

        private static CameraSharedComposition CreateComposition(
            ICollection<GameObject> roots,
            CameraViewDefinition view,
            CameraOutputDefinition output)
        {
            var root = new GameObject("QA_ADR027D_SimpleAssociation");
            root.SetActive(false);
            roots.Add(root);
            CameraSharedComposition composition = root.AddComponent<CameraSharedComposition>();
            composition.Configure(
                view,
                output,
                CameraSharedCompositionSubjectPolicyKind.AllAvailableSubjects);
            return composition;
        }

        private static CameraViewOutputPolicyAuthoring CreatePolicy(
            ICollection<GameObject> roots,
            CameraViewDefinition view,
            CameraOutputDefinition output)
        {
            var root = new GameObject("QA_ADR027D_AdvancedPolicy");
            root.SetActive(false);
            roots.Add(root);
            var binding = new CameraViewOutputBindingAuthoring();
            binding.Configure(view, output);
            CameraViewOutputPolicyAuthoring policy = root.AddComponent<CameraViewOutputPolicyAuthoring>();
            policy.Configure(new[] { binding });
            return policy;
        }

        private static void Destroy(
            IReadOnlyList<GameObject> roots,
            IReadOnlyList<ScriptableObject> definitions)
        {
            for (int index = roots.Count - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(roots[index]);
            for (int index = definitions.Count - 1; index >= 0; index--)
                QaCameraAuthoringFixtures.Destroy(definitions[index]);
        }

        private static void Set(UnityEngine.Object target, string name, object value)
        {
            var serialized = new SerializedObject(target);
            serialized.Update();
            SerializedProperty property = serialized.FindProperty(name) ??
                throw new InvalidOperationException($"Missing serialized property '{name}'.");
            if (value is bool flag) property.boolValue = flag;
            else if (value is UnityEngine.Object reference) property.objectReferenceValue = reference;
            else throw new InvalidOperationException($"Unsupported value for '{name}'.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}

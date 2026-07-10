using Immersive.Framework.Camera.Cinemachine;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    public static class QaC8B2CinemachineOutputApplierSmoke
    {
        private const string LogPrefix = "[QA][C8B2 Cinemachine Output]";

        [MenuItem("Immersive Framework/QA/Camera/C8B2 Cinemachine Output Applier Smoke")]
        public static void Run()
        {
            bool succeeded = RunForRegression();
            if (succeeded)
            {
                Debug.Log($"{LogPrefix} PASS. Cinemachine output applier validates explicit outputs, applies priority/targets, and blocks/skips invalid output states.");
            }
        }

        public static bool RunForRegression()
        {
            Debug.Log($"{LogPrefix} Smoke started.");

            GameObject root = null;
            try
            {
                root = CreateHiddenRoot("QA_C8B2_CinemachineOutput_Root");
                SmokeFixture fixture = CreateFixture(root.transform, "Primary", 10);

                if (!RunValidApplyCase(fixture))
                {
                    return false;
                }

                if (!RunMissingRequiredOutputCase())
                {
                    return false;
                }

                if (!RunMissingOptionalOutputCase())
                {
                    return false;
                }

                if (!RunMissingBrainCase(fixture))
                {
                    return false;
                }

                if (!RunMultipleBrainsCase(root.transform, fixture))
                {
                    return false;
                }

                if (!RunBrainScopeMismatchCase(root.transform, fixture))
                {
                    return false;
                }

                if (!RunMissingRequiredFollowCase(fixture))
                {
                    return false;
                }

                return true;
            }
            catch (System.Exception exception)
            {
                return Fail("unexpected-exception", exception.ToString());
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static bool RunValidApplyCase(SmokeFixture fixture)
        {
            fixture.UnityCamera.enabled = false;
            bool rootActiveBefore = fixture.Root.activeSelf;
            bool rigActiveBefore = fixture.CinemachineCamera.gameObject.activeSelf;

            CinemachineCameraOutput output = new CinemachineCameraOutput(
                "qa.c8b2.primary",
                "QA C8B2 Primary",
                fixture.CinemachineCamera,
                fixture.Brain,
                fixture.FollowTarget,
                fixture.LookAtTarget,
                77,
                true);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Apply(
                output,
                requireFollowTarget: true,
                requireLookAtTarget: true,
                explicitBrainScope: new[] { fixture.Brain });

            if (!diagnostic.IsSucceeded)
            {
                return Fail("valid-output-not-applied", FormatDiagnostic(diagnostic));
            }

            if (fixture.CinemachineCamera.Priority != 77)
            {
                return Fail("priority-not-applied", $"expected='77' actual='{fixture.CinemachineCamera.Priority}'");
            }

            if (fixture.CinemachineCamera.Target.TrackingTarget != fixture.FollowTarget)
            {
                return Fail("follow-target-not-applied", "TrackingTarget did not match the explicit follow target.");
            }

            if (fixture.CinemachineCamera.Target.LookAtTarget != fixture.LookAtTarget)
            {
                return Fail("look-at-target-not-applied", "LookAtTarget did not match the explicit look-at target.");
            }

            if (fixture.UnityCamera.enabled)
            {
                return Fail("unity-camera-enabled-mutated", "The output applier changed Unity Camera.enabled.");
            }

            if (fixture.Root.activeSelf != rootActiveBefore || fixture.CinemachineCamera.gameObject.activeSelf != rigActiveBefore)
            {
                return Fail("gameobject-active-state-mutated", "The output applier changed GameObject active state.");
            }

            Debug.Log($"{LogPrefix} step='valid-output-applied' status='{diagnostic.Status}' code='{diagnostic.Code}' priority='{fixture.CinemachineCamera.Priority}' follow='{fixture.FollowTarget.name}' lookAt='{fixture.LookAtTarget.name}' cameraEnabled='{fixture.UnityCamera.enabled}'");
            return true;
        }

        private static bool RunMissingRequiredOutputCase()
        {
            CinemachineCameraOutput output = CinemachineCameraOutput.Missing(
                "qa.c8b2.required-missing",
                "QA C8B2 Required Missing",
                required: true);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Validate(output);
            if (!diagnostic.IsBlocked || diagnostic.Code != CinemachineCameraOutputDiagnostic.CameraOutputMissing)
            {
                return Fail("required-missing-output-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='required-output-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static bool RunMissingOptionalOutputCase()
        {
            CinemachineCameraOutput output = CinemachineCameraOutput.Missing(
                "qa.c8b2.optional-missing",
                "QA C8B2 Optional Missing",
                required: false);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Validate(output);
            if (!diagnostic.IsSkipped || diagnostic.Code != CinemachineCameraOutputDiagnostic.OptionalOutputSkipped)
            {
                return Fail("optional-missing-output-not-skipped", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='optional-output-skipped' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static bool RunMissingBrainCase(SmokeFixture fixture)
        {
            CinemachineCameraOutput output = new CinemachineCameraOutput(
                "qa.c8b2.missing-brain",
                "QA C8B2 Missing Brain",
                fixture.CinemachineCamera,
                null,
                fixture.FollowTarget,
                fixture.LookAtTarget,
                20,
                true);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Validate(output);
            if (!diagnostic.IsBlocked || diagnostic.Code != CinemachineCameraOutputDiagnostic.CinemachineBrainMissing)
            {
                return Fail("missing-brain-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='missing-brain-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static bool RunMultipleBrainsCase(Transform parent, SmokeFixture fixture)
        {
            CinemachineBrain secondBrain = CreateUnityCameraWithBrain(parent, "SecondaryBrain").Brain;

            CinemachineCameraOutput output = new CinemachineCameraOutput(
                "qa.c8b2.multiple-brains",
                "QA C8B2 Multiple Brains",
                fixture.CinemachineCamera,
                fixture.Brain,
                fixture.FollowTarget,
                fixture.LookAtTarget,
                30,
                true);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Validate(
                output,
                explicitBrainScope: new[] { fixture.Brain, secondBrain });

            if (!diagnostic.IsBlocked || diagnostic.Code != CinemachineCameraOutputDiagnostic.MultipleCinemachineBrains)
            {
                return Fail("multiple-brains-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='multiple-brains-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static bool RunBrainScopeMismatchCase(Transform parent, SmokeFixture fixture)
        {
            CinemachineBrain scopedBrain = CreateUnityCameraWithBrain(parent, "MismatchBrain").Brain;

            CinemachineCameraOutput output = new CinemachineCameraOutput(
                "qa.c8b2.brain-mismatch",
                "QA C8B2 Brain Mismatch",
                fixture.CinemachineCamera,
                fixture.Brain,
                fixture.FollowTarget,
                fixture.LookAtTarget,
                40,
                true);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Validate(
                output,
                explicitBrainScope: new[] { scopedBrain });

            if (!diagnostic.IsBlocked || diagnostic.Code != CinemachineCameraOutputDiagnostic.BrainScopeMismatch)
            {
                return Fail("brain-scope-mismatch-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='brain-scope-mismatch-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static bool RunMissingRequiredFollowCase(SmokeFixture fixture)
        {
            CinemachineCameraOutput output = new CinemachineCameraOutput(
                "qa.c8b2.missing-follow",
                "QA C8B2 Missing Follow",
                fixture.CinemachineCamera,
                fixture.Brain,
                null,
                fixture.LookAtTarget,
                50,
                true);

            CinemachineCameraOutputDiagnostic diagnostic = FrameworkCinemachineOutputApplier.Validate(
                output,
                requireFollowTarget: true,
                requireLookAtTarget: false,
                explicitBrainScope: new[] { fixture.Brain });

            if (!diagnostic.IsBlocked || diagnostic.Code != CinemachineCameraOutputDiagnostic.FollowTargetMissing)
            {
                return Fail("missing-required-follow-not-blocked", FormatDiagnostic(diagnostic));
            }

            Debug.Log($"{LogPrefix} step='missing-required-follow-blocked' status='{diagnostic.Status}' code='{diagnostic.Code}'");
            return true;
        }

        private static SmokeFixture CreateFixture(Transform parent, string label, int initialPriority)
        {
            GameObject root = CreateHiddenChild(parent, $"QA_C8B2_{label}_Rig");

            BrainFixture brainFixture = CreateUnityCameraWithBrain(root.transform, $"{label}_UnityCamera");
            GameObject cinemachineObject = CreateHiddenChild(root.transform, $"{label}_CinemachineCamera");
            CinemachineCamera cinemachineCamera = cinemachineObject.AddComponent<CinemachineCamera>();
            cinemachineCamera.Priority = initialPriority;

            GameObject follow = CreateHiddenChild(root.transform, $"{label}_FollowTarget");
            GameObject lookAt = CreateHiddenChild(root.transform, $"{label}_LookAtTarget");

            return new SmokeFixture(
                root,
                brainFixture.UnityCamera,
                brainFixture.Brain,
                cinemachineCamera,
                follow.transform,
                lookAt.transform);
        }

        private static BrainFixture CreateUnityCameraWithBrain(Transform parent, string label)
        {
            GameObject cameraObject = CreateHiddenChild(parent, label);
            UnityEngine.Camera unityCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            CinemachineBrain brain = cameraObject.AddComponent<CinemachineBrain>();
            return new BrainFixture(unityCamera, brain);
        }

        private static GameObject CreateHiddenRoot(string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            return gameObject;
        }

        private static GameObject CreateHiddenChild(Transform parent, string name)
        {
            GameObject gameObject = CreateHiddenRoot(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static bool Fail(string code, string detail)
        {
            Debug.LogError($"{LogPrefix} FAIL. code='{code}' detail='{detail}'");
            return false;
        }

        private static string FormatDiagnostic(CinemachineCameraOutputDiagnostic diagnostic)
        {
            return $"status='{diagnostic.Status}' code='{diagnostic.Code}' outputId='{diagnostic.OutputId}' message='{diagnostic.Message}'";
        }

        private readonly struct SmokeFixture
        {
            public SmokeFixture(
                GameObject root,
                UnityEngine.Camera unityCamera,
                CinemachineBrain brain,
                CinemachineCamera cinemachineCamera,
                Transform followTarget,
                Transform lookAtTarget)
            {
                Root = root;
                UnityCamera = unityCamera;
                Brain = brain;
                CinemachineCamera = cinemachineCamera;
                FollowTarget = followTarget;
                LookAtTarget = lookAtTarget;
            }

            public GameObject Root { get; }

            public UnityEngine.Camera UnityCamera { get; }

            public CinemachineBrain Brain { get; }

            public CinemachineCamera CinemachineCamera { get; }

            public Transform FollowTarget { get; }

            public Transform LookAtTarget { get; }
        }

        private readonly struct BrainFixture
        {
            public BrainFixture(UnityEngine.Camera unityCamera, CinemachineBrain brain)
            {
                UnityCamera = unityCamera;
                Brain = brain;
            }

            public UnityEngine.Camera UnityCamera { get; }

            public CinemachineBrain Brain { get; }
        }
    }
}

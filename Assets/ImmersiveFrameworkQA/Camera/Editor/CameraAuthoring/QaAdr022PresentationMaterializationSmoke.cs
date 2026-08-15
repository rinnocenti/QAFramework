using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Editor.CameraAuthoring
{
    /// <summary>
    /// IF-ADR-022 C5 — Editor-only presentation materialization regression.
    ///
    /// This deliberately follows the proven QaC9MFollowPipelineSmoke shape:
    /// transient GameObjects, SerializedObject authoring, direct Apply/Rebuild,
    /// strong assertions, one menu entry and unconditional cleanup.
    /// </summary>
    public static class QaAdr022PresentationMaterializationSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run ADR-022 Presentation Materialization Regression";

        private const string LogPrefix =
            "[QA][ADR022 Presentation Models]";

        private static readonly string[] ExpectedCases =
        {
            "follow-existing-compatibility",
            "follow-lookat-rotation-materialized",
            "fixed-authored-pose-preserved",
            "fixed-lookat-rotation-materialized",
            "mounted-pipeline-materialized",
            "third-person-pipeline-materialized",
            "switch-follow-thirdperson-follow",
            "idempotent-rebuild",
            "external-compatible-not-adopted",
            "unknown-conflict-blocks",
            "blocked-switch-no-partial-mutation",
            "external-component-not-deleted",
            "no-output-authority-mutation",
            "unsupported-model-no-fallback"
        };

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();

            try
            {
                Require(
                    (int)CameraRigPresentationIntent.Follow == 10,
                    "Serialized compatibility regression: CameraRigPresentationIntent.Follow must remain numeric value 10.");

                // Reuse the already-working legacy Follow QA rather than cloning it.
                QaC9MFollowPipelineSmoke.Run();
                completed.Add(ExpectedCases[0]);

                RunFollowLookAtCase();
                completed.Add(ExpectedCases[1]);

                RunFixedCase();
                completed.Add(ExpectedCases[2]);

                RunFixedLookAtCase();
                completed.Add(ExpectedCases[3]);

                RunMountedCase();
                completed.Add(ExpectedCases[4]);

                RunThirdPersonCase();
                completed.Add(ExpectedCases[5]);

                RunSwitchCase();
                completed.Add(ExpectedCases[6]);

                RunIdempotentCase();
                completed.Add(ExpectedCases[7]);

                RunExternalCompatibleNotAdoptedCase();
                completed.Add(ExpectedCases[8]);

                RunUnknownConflictCase();
                completed.Add(ExpectedCases[9]);

                RunBlockedSwitchNoPartialMutationCase();
                completed.Add(ExpectedCases[10]);

                RunExternalComponentNotDeletedCase();
                completed.Add(ExpectedCases[11]);

                RunNoOutputAuthorityMutationCase();
                completed.Add(ExpectedCases[12]);

                RunUnsupportedModelCase();
                completed.Add(ExpectedCases[13]);

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' " +
                    $"cases='{completed.Count}/{ExpectedCases.Length}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                string next =
                    completed.Count < ExpectedCases.Length
                        ? ExpectedCases[completed.Count]
                        : string.Empty;

                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"cases='{completed.Count}/{ExpectedCases.Length}' " +
                    $"next='{next}' completed='{string.Join(",", completed)}' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");

                throw;
            }
        }

        private static void RunFollowLookAtCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_FollowLookAt");

            try
            {
                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Follow,
                    CameraTargetRequirement.Required);

                SetVector3(
                    fixture.Composer,
                    "followOffset",
                    new Vector3(1.25f, 4.5f, -7.75f));

                CameraRigComposerApplyRebuildResult result =
                    Apply(fixture.Composer);

                Require(
                    fixture.Composer.EffectiveFollowRequirement ==
                    CameraTargetRequirement.Required,
                    "Follow must require a Tracking target.");

                Require(
                    fixture.Composer.EffectiveLookAtRequirement ==
                    CameraTargetRequirement.Required,
                    "Follow Required Look At authoring was not preserved.");

                CinemachineCamera camera =
                    RequireCamera(fixture.Composer);

                CinemachineComponentBase body =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body);

                CinemachineComponentBase aim =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Aim);

                Require(
                    body is CinemachineFollow,
                    $"Follow Body is invalid. actual='{TypeName(body)}'.");

                Require(
                    aim is CinemachineHardLookAt,
                    $"Follow Look At did not materialize CinemachineHardLookAt. actual='{TypeName(aim)}'.");

                Require(
                    camera.Follow == fixture.Tracking.transform,
                    "Follow Tracking target was not assigned.");

                Require(
                    camera.LookAt == fixture.LookAt.transform,
                    "Follow Look At target was not assigned.");

                Require(
                    fixture.Composer.FrameworkOwnedPositionControl ==
                    body,
                    "Follow Body provenance was not committed as Framework-owned.");

                Require(
                    fixture.Composer.FrameworkOwnedRotationControl ==
                    aim,
                    "Follow Aim provenance was not committed as Framework-owned.");

                Require(
                    fixture.Composer.MaterializedPresentationIntent ==
                    CameraRigPresentationIntent.Follow &&
                    fixture.Composer.MaterializationRevision > 0,
                    "Follow materialization evidence was not committed.");

                CinemachineFollow follow =
                    (CinemachineFollow)body;

                RequireVector3(
                    follow.FollowOffset,
                    new Vector3(1.25f, 4.5f, -7.75f),
                    "Follow Offset");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunFixedCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_Fixed");

            try
            {
                Vector3 authoredPosition =
                    new Vector3(13.5f, 4.25f, -9.75f);

                Quaternion authoredRotation =
                    Quaternion.Euler(17f, 132f, -3f);

                CinemachineCamera preexisting =
                    fixture.CreatePreexistingCamera(
                        authoredPosition,
                        authoredRotation);

                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Fixed,
                    CameraTargetRequirement.NotUsed);

                Apply(fixture.Composer);

                CinemachineCamera camera =
                    RequireCamera(fixture.Composer);

                Require(
                    camera == preexisting,
                    "Fixed replaced the explicitly authored CinemachineCamera.");

                RequireVector3(
                    camera.transform.position,
                    authoredPosition,
                    "Fixed authored world position");

                RequireQuaternion(
                    camera.transform.rotation,
                    authoredRotation,
                    "Fixed authored world rotation");

                Require(
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body) == null,
                    "Fixed must not materialize a Position Control.");

                Require(
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Aim) == null,
                    "Fixed without Look At must not materialize a Rotation Control.");

                Require(
                    camera.Follow == null &&
                    camera.LookAt == null,
                    "Fixed without Look At retained procedural targets.");

                Require(
                    fixture.Composer.EffectiveFollowRequirement ==
                    CameraTargetRequirement.NotUsed,
                    "Fixed must not require a Tracking target.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunFixedLookAtCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_FixedLookAt");

            try
            {
                CinemachineCamera preexisting =
                    fixture.CreatePreexistingCamera(
                        new Vector3(8f, 2f, -5f),
                        Quaternion.Euler(5f, 45f, 0f));

                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Fixed,
                    CameraTargetRequirement.Required);

                Apply(fixture.Composer);

                CinemachineCamera camera =
                    RequireCamera(fixture.Composer);

                CinemachineComponentBase body =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body);

                CinemachineComponentBase aim =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Aim);

                Require(
                    camera == preexisting,
                    "Fixed + Look At replaced the authored CinemachineCamera.");

                Require(
                    body == null,
                    $"Fixed + Look At must not materialize a Body control. actual='{TypeName(body)}'.");

                Require(
                    aim is CinemachineHardLookAt,
                    $"Fixed + Look At did not materialize CinemachineHardLookAt. actual='{TypeName(aim)}'.");

                Require(
                    camera.Follow == null &&
                    camera.LookAt == fixture.LookAt.transform,
                    "Fixed + Look At target assignment is invalid.");

                Require(
                    fixture.Composer.FrameworkOwnedPositionControl ==
                    null &&
                    fixture.Composer.FrameworkOwnedRotationControl ==
                    aim,
                    "Fixed + Look At provenance is invalid.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunMountedCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_Mounted");

            try
            {
                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Mounted,
                    CameraTargetRequirement.NotUsed);

                SetFloat(
                    fixture.Composer,
                    "mountedPositionDamping",
                    0.35f);

                SetFloat(
                    fixture.Composer,
                    "mountedRotationDamping",
                    0.6f);

                Apply(fixture.Composer);

                CinemachineCamera camera =
                    RequireCamera(fixture.Composer);

                CinemachineComponentBase body =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body);

                CinemachineComponentBase aim =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Aim);

                CinemachineHardLockToTarget hardLock =
                    body as CinemachineHardLockToTarget;

                CinemachineRotateWithFollowTarget rotate =
                    aim as CinemachineRotateWithFollowTarget;

                Require(
                    hardLock != null,
                    $"Mounted Body is invalid. actual='{TypeName(body)}'.");

                Require(
                    rotate != null,
                    $"Mounted Aim is invalid. actual='{TypeName(aim)}'.");

                Require(
                    Mathf.Approximately(
                        hardLock.Damping,
                        0.35f),
                    $"Mounted Position Damping was not applied. actual='{hardLock.Damping}'.");

                Require(
                    Mathf.Approximately(
                        rotate.Damping,
                        0.6f),
                    $"Mounted Rotation Damping was not applied. actual='{rotate.Damping}'.");

                Require(
                    camera.Follow == fixture.Tracking.transform,
                    "Mounted Tracking target was not assigned.");

                Require(
                    camera.LookAt == null,
                    "Mounted first contract must not assign a separate Look At target.");

                Require(
                    fixture.Composer.EffectiveFollowRequirement ==
                    CameraTargetRequirement.Required &&
                    fixture.Composer.EffectiveLookAtRequirement ==
                    CameraTargetRequirement.NotUsed,
                    "Mounted target requirements are not canonical.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunThirdPersonCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_ThirdPerson");

            try
            {
                ConfigureThirdPerson(
                    fixture.Composer,
                    fixture.Source);

                Apply(fixture.Composer);

                CinemachineCamera camera =
                    RequireCamera(fixture.Composer);

                CinemachineComponentBase body =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body);

                CinemachineComponentBase aim =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Aim);

                CinemachineThirdPersonFollow thirdPerson =
                    body as CinemachineThirdPersonFollow;

                Require(
                    thirdPerson != null,
                    $"Third Person Body is invalid. actual='{TypeName(body)}'.");

                Require(
                    aim == null,
                    $"Third Person must not add a separate Aim controller. actual='{TypeName(aim)}'.");

                RequireVector3(
                    thirdPerson.ShoulderOffset,
                    new Vector3(0.75f, -0.2f, 0.1f),
                    "Third Person Shoulder Offset");

                Require(
                    Mathf.Approximately(
                        thirdPerson.VerticalArmLength,
                        0.65f),
                    $"Third Person Vertical Arm Length mismatch. actual='{thirdPerson.VerticalArmLength}'.");

                Require(
                    Mathf.Approximately(
                        thirdPerson.CameraSide,
                        0.25f),
                    $"Third Person Camera Side mismatch. actual='{thirdPerson.CameraSide}'.");

                Require(
                    Mathf.Approximately(
                        thirdPerson.CameraDistance,
                        4.5f),
                    $"Third Person Camera Distance mismatch. actual='{thirdPerson.CameraDistance}'.");

                RequireVector3(
                    thirdPerson.Damping,
                    new Vector3(0.2f, 0.3f, 0.4f),
                    "Third Person Damping");

                Require(
                    camera.Follow == fixture.Tracking.transform &&
                    camera.LookAt == null,
                    "Third Person target assignment is invalid.");

                Require(
                    fixture.Composer.EffectiveFollowRequirement ==
                    CameraTargetRequirement.Required &&
                    fixture.Composer.EffectiveLookAtRequirement ==
                    CameraTargetRequirement.NotUsed,
                    "Third Person target requirements are not canonical.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunSwitchCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_Switch");

            try
            {
                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Follow,
                    CameraTargetRequirement.NotUsed);

                Apply(fixture.Composer);

                CinemachineCamera camera =
                    RequireCamera(fixture.Composer);

                CinemachineFollow firstFollow =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body)
                    as CinemachineFollow;

                Require(
                    firstFollow != null,
                    "Switch precondition did not materialize Follow.");

                ConfigureThirdPerson(
                    fixture.Composer,
                    fixture.Source);

                Apply(fixture.Composer);

                Require(
                    fixture.Composer.CinemachineCamera ==
                    camera,
                    "Follow -> Third Person replaced the local CinemachineCamera.");

                Require(
                    firstFollow == null,
                    "Follow -> Third Person did not release the Framework-owned Follow component.");

                CinemachineThirdPersonFollow thirdPerson =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body)
                    as CinemachineThirdPersonFollow;

                Require(
                    thirdPerson != null,
                    "Follow -> Third Person did not materialize Third Person Body.");

                Require(
                    camera.GetComponents<CinemachineFollow>().Length == 0 &&
                    camera.GetComponents<CinemachineThirdPersonFollow>().Length == 1,
                    "Follow -> Third Person left duplicate/incompatible Body controls.");

                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Follow,
                    CameraTargetRequirement.NotUsed);

                Apply(fixture.Composer);

                Require(
                    fixture.Composer.CinemachineCamera ==
                    camera,
                    "Third Person -> Follow replaced the local CinemachineCamera.");

                Require(
                    thirdPerson == null,
                    "Third Person -> Follow did not release the Framework-owned Third Person component.");

                Require(
                    camera.GetComponents<CinemachineFollow>().Length == 1 &&
                    camera.GetComponents<CinemachineThirdPersonFollow>().Length == 0,
                    "Third Person -> Follow did not converge on exactly one Follow Body.");

                Require(
                    fixture.Composer.MaterializedPresentationIntent ==
                    CameraRigPresentationIntent.Follow,
                    "Switch terminal materialization evidence is not Follow.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunIdempotentCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_Idempotent");

            try
            {
                ConfigureThirdPerson(
                    fixture.Composer,
                    fixture.Source);

                CameraRigComposerApplyRebuildResult first =
                    Apply(fixture.Composer);

                CinemachineCamera camera =
                    RequireCamera(fixture.Composer);

                CinemachineThirdPersonFollow body =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body)
                    as CinemachineThirdPersonFollow;

                Require(
                    body != null,
                    "Idempotency precondition did not materialize Third Person Body.");

                int firstRevision =
                    fixture.Composer.MaterializationRevision;

                CameraRigComposerApplyRebuildResult second =
                    Apply(fixture.Composer);

                Require(
                    first.Succeeded &&
                    second.Succeeded,
                    "Idempotency requires both Apply/Rebuild operations to succeed.");

                Require(
                    second.CreatedCount == 0,
                    $"Second Apply/Rebuild created new objects/components. created='{second.CreatedCount}'.");

                Require(
                    fixture.Composer.CinemachineCamera ==
                    camera,
                    "Second Apply/Rebuild replaced CinemachineCamera identity.");

                Require(
                    camera.GetComponents<CinemachineThirdPersonFollow>().Length == 1,
                    "Second Apply/Rebuild duplicated Third Person Body.");

                Require(
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body) ==
                    body,
                    "Second Apply/Rebuild replaced an already-valid Framework-owned Third Person Body.");

                Require(
                    fixture.Composer.MaterializationRevision >
                    firstRevision,
                    "Materialization revision did not advance after the second successful Apply/Rebuild.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunExternalCompatibleNotAdoptedCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_ExternalCompatible");

            try
            {
                CinemachineCamera camera =
                    fixture.CreatePreexistingCamera(
                        Vector3.zero,
                        Quaternion.identity);

                CinemachineFollow externalFollow =
                    camera.gameObject.AddComponent<CinemachineFollow>();

                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Follow,
                    CameraTargetRequirement.NotUsed);

                SetVector3(
                    fixture.Composer,
                    "followOffset",
                    new Vector3(0f, 3f, -6f));

                Apply(fixture.Composer);

                Require(
                    camera.GetComponents<CinemachineFollow>().Length == 1 &&
                    camera.GetComponent<CinemachineFollow>() ==
                    externalFollow,
                    "Compatible external Follow was duplicated or replaced.");

                Require(
                    fixture.Composer.FrameworkOwnedPositionControl ==
                    null,
                    "Compatible external Follow was silently adopted as Framework-owned.");

                Require(
                    fixture.Composer.LastMaterializationSummary.Contains(
                        "positionOwnership='ExternalOrUnknown'"),
                    "Compatible external Follow provenance was not reported as ExternalOrUnknown.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunUnknownConflictCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_UnknownConflict");

            try
            {
                CinemachineCamera camera =
                    fixture.CreatePreexistingCamera(
                        Vector3.zero,
                        Quaternion.identity);

                CinemachineHardLockToTarget external =
                    camera.gameObject.AddComponent<CinemachineHardLockToTarget>();

                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Follow,
                    CameraTargetRequirement.NotUsed);

                CameraRigComposerApplyRebuildResult result =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        fixture.Composer,
                        false,
                        false);

                Require(
                    !result.Succeeded &&
                    result.BlockedCount > 0,
                    "Unknown incompatible Body did not block Apply/Rebuild.");

                Require(
                    result.BlockingIssue.Contains(
                        "external-or-unknown-conflict"),
                    $"Unknown conflict did not expose explicit provenance. issue='{result.BlockingIssue}'.");

                Require(
                    external != null,
                    "Unknown conflicting Body was destroyed.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunBlockedSwitchNoPartialMutationCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_PreflightTransaction");

            try
            {
                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Follow,
                    CameraTargetRequirement.NotUsed);

                Apply(fixture.Composer);

                CinemachineCamera camera =
                    RequireCamera(fixture.Composer);

                CinemachineFollow frameworkFollow =
                    camera.GetCinemachineComponent(
                        CinemachineCore.Stage.Body)
                    as CinemachineFollow;

                Require(
                    frameworkFollow != null &&
                    fixture.Composer.FrameworkOwnedPositionControl ==
                    frameworkFollow,
                    "Transactional precondition lacks Framework-owned Follow evidence.");

                CinemachineHardLookAt externalAim =
                    camera.gameObject.AddComponent<CinemachineHardLookAt>();

                ConfigureThirdPerson(
                    fixture.Composer,
                    fixture.Source);

                CameraRigComposerApplyRebuildResult blocked =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        fixture.Composer,
                        false,
                        false);

                Require(
                    !blocked.Succeeded &&
                    blocked.BlockedCount > 0,
                    "External Aim conflict did not block model switching.");

                Require(
                    frameworkFollow != null &&
                    camera.GetComponents<CinemachineFollow>().Length == 1,
                    "Blocked switch partially removed the existing Framework-owned Body.");

                Require(
                    externalAim != null &&
                    camera.GetComponents<CinemachineHardLookAt>().Length == 1,
                    "Blocked switch removed the external Aim conflict.");

                Require(
                    camera.GetComponents<CinemachineThirdPersonFollow>().Length == 0,
                    "Blocked switch partially materialized Third Person Body.");

                Require(
                    fixture.Composer.MaterializedPresentationIntent ==
                    CameraRigPresentationIntent.Follow &&
                    fixture.Composer.FrameworkOwnedPositionControl ==
                    frameworkFollow,
                    "Blocked switch committed partial materialization provenance.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunExternalComponentNotDeletedCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_NoExternalDeletion");

            try
            {
                CinemachineCamera camera =
                    fixture.CreatePreexistingCamera(
                        Vector3.zero,
                        Quaternion.identity);

                CinemachineHardLockToTarget external =
                    camera.gameObject.AddComponent<CinemachineHardLockToTarget>();

                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Follow,
                    CameraTargetRequirement.NotUsed);

                CameraRigComposerApplyRebuildResult result =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        fixture.Composer,
                        false,
                        false);

                Require(
                    !result.Succeeded,
                    "External-deletion case requires an explicit blocked result.");

                Require(
                    external != null &&
                    camera.GetComponent<CinemachineHardLockToTarget>() ==
                    external,
                    "Apply/Rebuild destroyed or replaced an external incompatible Body.");

                Require(
                    camera.GetComponents<CinemachineHardLockToTarget>().Length == 1,
                    "External incompatible Body is no longer present after the blocked operation.");

                Require(
                    camera.GetComponents<CinemachineFollow>().Length == 0,
                    "Blocked operation added a competing Follow Body.");

                Require(
                    fixture.Composer.FrameworkOwnedPositionControl ==
                    null &&
                    fixture.Composer.MaterializedPresentationIntent ==
                    CameraRigPresentationIntent.Undefined,
                    "Blocked operation fabricated Framework ownership evidence.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunNoOutputAuthorityMutationCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_NoOutputAuthority");

            try
            {
                ConfigurePresentation(
                    fixture.Composer,
                    fixture.Source,
                    CameraRigPresentationIntent.Follow,
                    CameraTargetRequirement.NotUsed);

                Apply(fixture.Composer);

                Require(
                    fixture.Rig.GetComponentsInChildren<UnityEngine.Camera>(
                        true).Length == 0,
                    "CameraRigComposer materialization created a Unity Camera output.");

                Require(
                    fixture.Rig.GetComponentsInChildren<CinemachineBrain>(
                        true).Length == 0,
                    "CameraRigComposer materialization created a CinemachineBrain.");

                Require(
                    fixture.Rig.GetComponentsInChildren<CinemachineCamera>(
                        true).Length == 1,
                    "Local materialization must converge on exactly one CinemachineCamera.");

                Require(
                    fixture.Composer.CinemachineCamera != null,
                    "No-output case did not materialize the local CinemachineCamera.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static void RunUnsupportedModelCase()
        {
            RigFixture fixture =
                new RigFixture("QA_ADR022_Unsupported");

            try
            {
                SerializedObject serialized =
                    new SerializedObject(
                        fixture.Composer);

                serialized.Update();

                RequireProperty(
                    serialized,
                    "presentationIntent")
                    .intValue = 999;

                RequireProperty(
                    serialized,
                    "targetSource")
                    .objectReferenceValue =
                    fixture.Source;

                serialized.ApplyModifiedPropertiesWithoutUndo();

                CameraRigComposerApplyRebuildResult result =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        fixture.Composer,
                        false,
                        false);

                Require(
                    !result.Succeeded,
                    "Unsupported Presentation unexpectedly succeeded.");

                Require(
                    result.BlockingIssue.Contains(
                        "does not support Presentation intent"),
                    $"Unsupported Presentation did not fail explicitly. issue='{result.BlockingIssue}'.");

                Require(
                    fixture.Composer.CinemachineCamera == null &&
                    fixture.Rig.GetComponentsInChildren<CinemachineCamera>(
                        true).Length == 0,
                    "Unsupported Presentation silently fell back to a materialized camera model.");

                Require(
                    fixture.Composer.MaterializedPresentationIntent ==
                    CameraRigPresentationIntent.Undefined &&
                    fixture.Composer.FrameworkOwnedPositionControl ==
                    null &&
                    fixture.Composer.FrameworkOwnedRotationControl ==
                    null,
                    "Unsupported Presentation committed fabricated materialization evidence.");
            }
            finally
            {
                fixture.Dispose();
            }
        }

        private static CameraRigComposerApplyRebuildResult Apply(
            CameraRigComposer composer)
        {
            CameraRigComposerApplyRebuildResult result =
                CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                    composer,
                    false,
                    false);

            Require(
                result.Succeeded,
                $"Apply/Rebuild failed. status='{result.Status}' issue='{result.BlockingIssue}' summary='{result.MaterializationSummary}'.");

            return result;
        }

        private static CinemachineCamera RequireCamera(
            CameraRigComposer composer)
        {
            Require(
                composer != null &&
                composer.CinemachineCamera != null,
                "CinemachineCamera evidence is missing.");

            return composer.CinemachineCamera;
        }

        private static void ConfigurePresentation(
            CameraRigComposer composer,
            ExplicitCameraTargetSourceAuthoring source,
            CameraRigPresentationIntent presentation,
            CameraTargetRequirement lookAtRequirement)
        {
            SerializedObject serialized =
                new SerializedObject(composer);

            serialized.Update();

            RequireProperty(
                serialized,
                "presentationIntent")
                .intValue =
                (int)presentation;

            RequireProperty(
                serialized,
                "targetSource")
                .objectReferenceValue =
                source;

            RequireProperty(
                serialized,
                "followRequirement")
                .intValue =
                (int)CameraTargetRequirement.Required;

            RequireProperty(
                serialized,
                "lookAtRequirement")
                .intValue =
                (int)lookAtRequirement;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureThirdPerson(
            CameraRigComposer composer,
            ExplicitCameraTargetSourceAuthoring source)
        {
            ConfigurePresentation(
                composer,
                source,
                CameraRigPresentationIntent.ThirdPerson,
                CameraTargetRequirement.NotUsed);

            SerializedObject serialized =
                new SerializedObject(composer);

            serialized.Update();

            RequireProperty(
                serialized,
                "thirdPersonShoulderOffset")
                .vector3Value =
                new Vector3(
                    0.75f,
                    -0.2f,
                    0.1f);

            RequireProperty(
                serialized,
                "thirdPersonVerticalArmLength")
                .floatValue =
                0.65f;

            RequireProperty(
                serialized,
                "thirdPersonCameraSide")
                .floatValue =
                0.25f;

            RequireProperty(
                serialized,
                "thirdPersonCameraDistance")
                .floatValue =
                4.5f;

            RequireProperty(
                serialized,
                "thirdPersonDamping")
                .vector3Value =
                new Vector3(
                    0.2f,
                    0.3f,
                    0.4f);

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector3(
            CameraRigComposer composer,
            string propertyName,
            Vector3 value)
        {
            SerializedObject serialized =
                new SerializedObject(composer);

            serialized.Update();

            RequireProperty(
                serialized,
                propertyName)
                .vector3Value =
                value;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(
            CameraRigComposer composer,
            string propertyName,
            float value)
        {
            SerializedObject serialized =
                new SerializedObject(composer);

            serialized.Update();

            RequireProperty(
                serialized,
                propertyName)
                .floatValue =
                value;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string name)
        {
            SerializedProperty property =
                serialized.FindProperty(name);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Required serialized property '{name}' was not found on '{serialized.targetObject.GetType().FullName}'.");
            }

            return property;
        }

        private static void ConfigureSource(
            ExplicitCameraTargetSourceAuthoring source,
            string logicalSourceId,
            Transform tracking,
            Transform lookAt)
        {
            SerializedObject serialized =
                new SerializedObject(source);

            serialized.Update();

            RequireProperty(
                serialized,
                "logicalSourceId")
                .stringValue =
                logicalSourceId;

            RequireProperty(
                serialized,
                "followTarget")
                .objectReferenceValue =
                tracking;

            RequireProperty(
                serialized,
                "lookAtTarget")
                .objectReferenceValue =
                lookAt;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RequireVector3(
            Vector3 actual,
            Vector3 expected,
            string label)
        {
            Require(
                (actual - expected).sqrMagnitude <=
                0.000001f,
                $"{label} mismatch. expected='{expected}' actual='{actual}'.");
        }

        private static void RequireQuaternion(
            Quaternion actual,
            Quaternion expected,
            string label)
        {
            Require(
                Quaternion.Angle(
                    actual,
                    expected) <=
                0.001f,
                $"{label} mismatch. expected='{expected.eulerAngles}' actual='{actual.eulerAngles}'.");
        }

        private static string TypeName(
            UnityEngine.Object value)
        {
            return value != null
                ? value.GetType().FullName
                : "<none>";
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace(
                    "'",
                    "\\'");
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private sealed class RigFixture :
            IDisposable
        {
            public RigFixture(
                string name)
            {
                Root =
                    new GameObject(name);

                Tracking =
                    new GameObject(
                        "TrackingTarget");

                Tracking.transform.SetParent(
                    Root.transform,
                    false);

                LookAt =
                    new GameObject(
                        "LookAtTarget");

                LookAt.transform.SetParent(
                    Root.transform,
                    false);

                Rig =
                    new GameObject(
                        "CameraRig");

                Rig.transform.SetParent(
                    Root.transform,
                    false);

                Source =
                    Rig.AddComponent<
                        ExplicitCameraTargetSourceAuthoring>();

                ConfigureSource(
                    Source,
                    "qa.adr022." +
                    name.ToLowerInvariant(),
                    Tracking.transform,
                    LookAt.transform);

                Composer =
                    Rig.AddComponent<
                        CameraRigComposer>();
            }

            public GameObject Root { get; }

            public GameObject Rig { get; }

            public GameObject Tracking { get; }

            public GameObject LookAt { get; }

            public ExplicitCameraTargetSourceAuthoring Source { get; }

            public CameraRigComposer Composer { get; }

            public CinemachineCamera CreatePreexistingCamera(
                Vector3 worldPosition,
                Quaternion worldRotation)
            {
                GameObject cameraObject =
                    new GameObject(
                        "Authored Cinemachine Camera");

                cameraObject.transform.SetParent(
                    Rig.transform,
                    false);

                cameraObject.transform.SetPositionAndRotation(
                    worldPosition,
                    worldRotation);

                CinemachineCamera camera =
                    cameraObject.AddComponent<
                        CinemachineCamera>();

                SerializedObject serialized =
                    new SerializedObject(
                        Composer);

                serialized.Update();

                RequireProperty(
                    serialized,
                    "cinemachineCamera")
                    .objectReferenceValue =
                    camera;

                serialized.ApplyModifiedPropertiesWithoutUndo();

                return camera;
            }

            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(
                        Root);
                }
            }
        }
    }
}

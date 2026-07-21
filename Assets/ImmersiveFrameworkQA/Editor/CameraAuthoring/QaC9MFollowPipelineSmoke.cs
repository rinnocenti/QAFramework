using System;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Editor.CameraAuthoring
{
    public static class QaC9MFollowPipelineSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Camera/Run Camera Follow Authoring Regression";
        private const string LogPrefix = "[QA][C9M Follow Pipeline]";

        [MenuItem(MenuPath)]
        public static void Run()
        {
            GameObject root = null;

            try
            {
                root = new GameObject("QA_C9M_FollowPipeline");
                GameObject target = new GameObject("FollowTarget");
                target.transform.SetParent(root.transform, false);

                GameObject rig = new GameObject("FollowRig");
                rig.transform.SetParent(root.transform, false);

                ExplicitCameraTargetSourceAuthoring source =
                    rig.AddComponent<ExplicitCameraTargetSourceAuthoring>();
                Configure(source, target.transform);

                CameraRigComposer composer =
                    rig.AddComponent<CameraRigComposer>();
                Configure(composer, source);

                CameraRigComposerApplyRebuildResult first =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        composer,
                        false,
                        false);

                Require(first.Succeeded,
                    $"First Apply/Rebuild failed: {first.BlockingIssue}");
                Require(composer.TargetSourceBehaviour == source,
                    "Typed target source was not assigned.");
                Require(composer.CinemachineCamera != null,
                    "CinemachineCamera was not materialized.");
                Require(
                    composer.CinemachineCamera.TryGetComponent(
                        out CinemachineFollow follow),
                    "CinemachineFollow was not materialized for Follow intent.");
                Require(follow != null,
                    "CinemachineFollow evidence is missing.");
                Require(composer.CinemachineCamera.Follow == target.transform,
                    "Follow target was not assigned.");
                Require(
                    follow.FollowOffset == new Vector3(0f, 6f, -9f),
                    $"Follow offset was not applied. actual='{follow.FollowOffset}'.");

                CameraRigComposerApplyRebuildResult second =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        composer,
                        false,
                        false);

                Require(second.Succeeded,
                    $"Second Apply/Rebuild failed: {second.BlockingIssue}");
                Require(second.CreatedCount == 0,
                    $"Second Apply/Rebuild was not idempotent. created='{second.CreatedCount}'.");
                Require(
                    composer.CinemachineCamera
                        .GetComponents<CinemachineFollow>().Length == 1,
                    "Apply/Rebuild created duplicate CinemachineFollow components.");

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' cases='6' " +
                    "completed='target-source-assigned,camera-materialized,follow-pipeline-materialized,target-assigned,offset-applied,idempotent'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        private static void Configure(
            ExplicitCameraTargetSourceAuthoring source,
            Transform target)
        {
            var serialized = new SerializedObject(source);
            serialized.Update();
            serialized.FindProperty("logicalSourceId").stringValue =
                "qa.c9m.follow-target";
            serialized.FindProperty("followTarget").objectReferenceValue =
                target;
            serialized.FindProperty("lookAtTarget").objectReferenceValue =
                target;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Configure(
            CameraRigComposer composer,
            ExplicitCameraTargetSourceAuthoring source)
        {
            var serialized = new SerializedObject(composer);
            serialized.Update();
            serialized.FindProperty("presentationIntent").intValue =
                (int)CameraRigPresentationIntent.Follow;
            serialized.FindProperty("targetSource").objectReferenceValue =
                source;
            serialized.FindProperty("followRequirement").intValue =
                (int)CameraTargetRequirement.Required;
            serialized.FindProperty("lookAtRequirement").intValue =
                (int)CameraTargetRequirement.Optional;
            serialized.FindProperty("followOffset").vector3Value =
                new Vector3(0f, 6f, -9f);
            serialized.FindProperty("createCinemachineCameraIfMissing").boolValue =
                true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

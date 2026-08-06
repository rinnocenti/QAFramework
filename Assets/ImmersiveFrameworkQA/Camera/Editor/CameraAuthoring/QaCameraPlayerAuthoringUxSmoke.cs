using System;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Editor.CameraAuthoring
{
    internal static class QaCameraPlayerAuthoringUxSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run Player Camera Authoring UX Smoke";

        private const string LogPrefix =
            "[QA][Player Camera Authoring UX]";

        [MenuItem(MenuPath)]
        private static void Run()
        {
            var completed =
                new List<string>();

            GameObject actor = null;

            try
            {
                actor =
                    new GameObject(
                        "QA Player Camera Actor");

                Transform follow =
                    CreateChild(
                        actor.transform,
                        "CameraTarget");

                Transform lookAt =
                    CreateChild(
                        actor.transform,
                        "LookAtTarget");

                GameObject rigObject =
                    new GameObject(
                        "Player Camera Rig");

                rigObject.transform.SetParent(
                    actor.transform,
                    false);

                CameraRigComposer rig =
                    rigObject.AddComponent<
                        CameraRigComposer>();

                ConfigureRig(
                    rig,
                    follow,
                    lookAt);

                var rigSerialized =
                    new SerializedObject(
                        rig);

                Require(
                    rigSerialized.FindProperty(
                        "recipe") == null,
                    "CameraRigComposer still exposes a Recipe field.");

                Require(
                    rigSerialized.FindProperty(
                        "createCinemachineCameraIfMissing") ==
                    null,
                    "CameraRigComposer still exposes Camera creation as designer policy.");

                Require(
                    rigSerialized.FindProperty(
                        "cinemachineCameraObjectName") ==
                    null,
                    "CameraRigComposer still exposes a generated object-name policy.");

                completed.Add(
                    "recipe-and-creation-policy-removed");

                CameraRigComposerApplyRebuildResult validation =
                    CameraRigComposerApplyRebuildUtility
                        .Validate(
                            rig,
                            false);

                Require(
                    validation.Succeeded,
                    "Configured Camera Rig did not validate. " +
                    validation.BlockingIssue);

                completed.Add(
                    "composer-target-authority-valid");

                CameraRigReference rigReference =
                    CameraRigReference.FromComposer(
                        rig);

                Require(
                    rigReference.IsValid &&
                    rigReference.HasComposer &&
                    ReferenceEquals(
                        rigReference.Composer,
                        rig),
                    "CameraRigReference did not retain the concrete Composer-only rig evidence.");

                completed.Add(
                    "camera-rig-reference-composer-only");

                CameraRigComposerApplyRebuildResult first =
                    CameraRigComposerApplyRebuildUtility
                        .ApplyOrRebuild(
                            rig,
                            false,
                            false);

                Require(
                    first.Succeeded &&
                    first.BlockedCount == 0,
                    "First Camera Rig materialization failed. " +
                    first.BlockingIssue);

                CinemachineCamera[] cameras =
                    rigObject.GetComponentsInChildren<
                        CinemachineCamera>(
                        true);

                CinemachineFollow[] follows =
                    rigObject.GetComponentsInChildren<
                        CinemachineFollow>(
                        true);

                Require(
                    cameras.Length == 1,
                    $"Expected one materialized Cinemachine Camera, found '{cameras.Length}'.");

                Require(
                    follows.Length == 1,
                    $"Expected one materialized Cinemachine Follow, found '{follows.Length}'.");

                Require(
                    ReferenceEquals(
                        cameras[0].Follow,
                        follow) &&
                    ReferenceEquals(
                        cameras[0].LookAt,
                        lookAt),
                    "Materialized Cinemachine targets do not match Composer targets.");

                completed.Add(
                    "missing-camera-created");

                CameraRigComposerApplyRebuildResult second =
                    CameraRigComposerApplyRebuildUtility
                        .ApplyOrRebuild(
                            rig,
                            false,
                            false);

                Require(
                    second.Succeeded &&
                    second.BlockedCount == 0 &&
                    rigObject.GetComponentsInChildren<
                        CinemachineCamera>(
                        true).Length == 1 &&
                    rigObject.GetComponentsInChildren<
                        CinemachineFollow>(
                        true).Length == 1,
                    "Repeated Camera Rig materialization was not idempotent.");

                completed.Add(
                    "apply-rebuild-idempotent");

                PlayerGameplayCameraAuthoring authoring =
                    actor.AddComponent<
                        PlayerGameplayCameraAuthoring>();

                SetObject(
                    authoring,
                    "cameraRig",
                    rig);

                SetEnum(
                    authoring,
                    "requiredness",
                    (int)PlayerGameplayCameraRequiredness.Required);

                SetInt(
                    authoring,
                    "precedence",
                    50);

                var authoringSerialized =
                    new SerializedObject(
                        authoring);

                Require(
                    authoringSerialized.FindProperty(
                        "followTarget") == null &&
                    authoringSerialized.FindProperty(
                        "lookAtTarget") == null,
                    "Player Gameplay Camera still serializes duplicate target fields.");

                completed.Add(
                    "duplicate-player-targets-removed");

                Require(
                    authoring.TryResolveCameraTargets(
                        out CameraResolvedTargets resolved,
                        out string diagnostic) &&
                    ReferenceEquals(
                        resolved.FollowTarget,
                        follow) &&
                    ReferenceEquals(
                        resolved.LookAtTarget,
                        lookAt),
                    "Player Gameplay Camera did not resolve targets from the Composer. " +
                    diagnostic);

                Require(
                    ReferenceEquals(
                        authoring.FollowTarget,
                        follow) &&
                    ReferenceEquals(
                        authoring.LookAtTarget,
                        lookAt) &&
                    authoring.HasExplicitCameraReferences,
                    "Player Gameplay Camera public evidence is not Composer-derived.");

                completed.Add(
                    "player-targets-derived-from-composer");

                Transform foreign =
                    new GameObject(
                        "Foreign Camera Target")
                        .transform;

                try
                {
                    ConfigureRig(
                        rig,
                        foreign,
                        lookAt);

                    Require(
                        authoring.TryResolveCameraTargets(
                            out CameraResolvedTargets foreignResolved,
                            out _) &&
                        ReferenceEquals(
                            foreignResolved.FollowTarget,
                            foreign),
                        "Composer did not retain explicit foreign target evidence.");

                    completed.Add(
                        "foreign-target-remains-explicit-evidence");
                }
                finally
                {
                    UnityEngine.Object
                        .DestroyImmediate(
                            foreign.gameObject);
                }

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");

                throw;
            }
            finally
            {
                if (actor != null)
                {
                    UnityEngine.Object
                        .DestroyImmediate(
                            actor);
                }
            }
        }

        private static Transform CreateChild(
            Transform parent,
            string objectName)
        {
            var child =
                new GameObject(
                    objectName);

            child.transform.SetParent(
                parent,
                false);

            return child.transform;
        }

        private static void ConfigureRig(
            CameraRigComposer rig,
            Transform follow,
            Transform lookAt)
        {
            var serialized =
                new SerializedObject(
                    rig);

            serialized.Update();

            serialized.FindProperty(
                    "targetSourceKind")
                .intValue =
                (int)CameraTargetSourceKind
                    .ExplicitTransform;

            serialized.FindProperty(
                    "targetSource")
                .objectReferenceValue =
                null;

            serialized.FindProperty(
                    "explicitFollowTarget")
                .objectReferenceValue =
                follow;

            serialized.FindProperty(
                    "explicitLookAtTarget")
                .objectReferenceValue =
                lookAt;

            serialized.FindProperty(
                    "followRequirement")
                .intValue =
                (int)CameraTargetRequirement.Required;

            serialized.FindProperty(
                    "lookAtRequirement")
                .intValue =
                (int)CameraTargetRequirement.Optional;

            serialized.FindProperty(
                    "followOffset")
                .vector3Value =
                new Vector3(
                    0f,
                    1f,
                    -5f);

            serialized
                .ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized =
                new SerializedObject(
                    target);

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing property '{target.GetType().Name}.{propertyName}'.");

            property.objectReferenceValue =
                value;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serialized =
                new SerializedObject(
                    target);

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing enum property '{target.GetType().Name}.{propertyName}'.");

            property.intValue =
                value;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serialized =
                new SerializedObject(
                    target);

            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Missing int property '{target.GetType().Name}.{propertyName}'.");

            property.intValue =
                value;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();
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

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }
    }
}

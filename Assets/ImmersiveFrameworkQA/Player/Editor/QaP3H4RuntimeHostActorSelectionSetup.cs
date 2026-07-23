using System;
using System.Linq;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Idempotent P3H.4 fixture installer. Extends the real technical-host join fixture with
    /// explicit GameApplication Actor-selection policy and separate contextual Logical Actor Host prefabs.
    /// </summary>
    internal static class QaP3H4RuntimeHostActorSelectionSetup
    {
        private const string RootFolder =
            "Assets/ImmersiveFrameworkQA/Player/P3H4";
        private const string DefaultLogicalHostPath =
            RootFolder + "/P3H4_DefaultLogicalActor.prefab";
        private const string AlternateLogicalHostPath =
            RootFolder + "/P3H4_AlternateLogicalActor.prefab";
        private const string DefaultActorPath = RootFolder + "/P3H4_DefaultActor.asset";
        private const string AlternateActorPath = RootFolder + "/P3H4_AlternateActor.asset";
        private static readonly Vector3 LogicalActorCameraFollowOffset =
            new Vector3(0f, 6f, -9f);

        internal static void Apply()
        {
            try
            {
                QaLocalPlayerRuntimeIntegrationSetup.Apply();
                EnsureFolder(RootFolder);

                GameObject defaultLogicalHost = CreateOrUpdateLogicalActorHost(
                    DefaultLogicalHostPath,
                    "P3H4 Default Logical Player Actor",
                    "qa.p3h4.logical.default.template");
                GameObject alternateLogicalHost = CreateOrUpdateLogicalActorHost(
                    AlternateLogicalHostPath,
                    "P3H4 Alternate Logical Player Actor",
                    "qa.p3h4.logical.alternate.template");

                ActorProfile defaultActor = CreateOrUpdateActorProfile(
                    DefaultActorPath,
                    "P3H4 Default Player Actor",
                    "qa.p3h4.actor-profile.default",
                    defaultLogicalHost);
                ActorProfile alternateActor = CreateOrUpdateActorProfile(
                    AlternateActorPath,
                    "P3H4 Alternate Player Actor",
                    "qa.p3h4.actor-profile.alternate",
                    alternateLogicalHost);

                ValidateLogicalActorHost(defaultActor, "default");
                ValidateLogicalActorHost(alternateActor, "alternate");

                ImmersiveFrameworkSettingsAsset settings =
                    Resources.Load<ImmersiveFrameworkSettingsAsset>(
                        ImmersiveFrameworkSettingsAsset.ResourcesPath);
                if (settings == null || settings.ActiveGameApplication == null)
                {
                    throw new InvalidOperationException(
                        "Active Game Application was not resolved from ImmersiveFrameworkSettings.");
                }

                GameApplicationAsset gameApplication = settings.ActiveGameApplication;
                var serializedApplication = new SerializedObject(gameApplication);
                SerializedProperty policyProperty = serializedApplication.FindProperty(
                    "playerActorSelectionDuplicatePolicy");
                if (policyProperty == null)
                {
                    throw new InvalidOperationException(
                        "GameApplication Actor duplicate-selection field was not found.");
                }
                policyProperty.intValue =
                    (int)PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots;
                serializedApplication.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(gameApplication);

                if (!gameApplication.TryGetLocalPlayerSlot(0, out PlayerSlotProfile firstSlot) ||
                    firstSlot == null)
                {
                    throw new InvalidOperationException(
                        "Active Game Application requires a configured first Local Player Slot.");
                }

                var serializedSlot = new SerializedObject(firstSlot);
                serializedSlot.FindProperty("defaultActorProfile").objectReferenceValue =
                    defaultActor;
                serializedSlot.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(firstSlot);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "[P3H4_RUNTIME_HOST_ACTOR_SELECTION_FIXTURE] status='Applied' " +
                    $"gameApplication='{gameApplication.name}' " +
                    $"duplicatePolicy='{gameApplication.PlayerActorSelectionDuplicatePolicy}' slot='{firstSlot.PlayerSlotId.StableText}' " +
                    $"defaultActor='{defaultActor.ActorProfileId.StableText}' " +
                    $"alternateActor='{alternateActor.ActorProfileId.StableText}' " +
                    $"logicalHostsSeparated='True'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3H4_RUNTIME_HOST_ACTOR_SELECTION_FIXTURE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static GameObject CreateOrUpdateLogicalActorHost(
            string assetPath,
            string displayName,
            string actorId)
        {
            var temporary = new GameObject(displayName);
            try
            {
                PlayerActorDeclaration declaration =
                    temporary.AddComponent<PlayerActorDeclaration>();
                var serialized = new SerializedObject(declaration);
                serialized.FindProperty("actorId").stringValue = actorId;
                serialized.FindProperty("displayName").stringValue = displayName;
                SerializedProperty playerInput = serialized.FindProperty("playerInput");
                if (playerInput != null)
                {
                    playerInput.objectReferenceValue = null;
                }
                serialized.FindProperty("reason").stringValue =
                    "qa.p3h4.logical-actor-template";
                serialized.ApplyModifiedPropertiesWithoutUndo();

                var cameraTargets = new GameObject("Camera Targets");
                cameraTargets.transform.SetParent(temporary.transform, false);
                var followTarget = new GameObject("Follow Target");
                followTarget.transform.SetParent(cameraTargets.transform, false);
                var lookAtTarget = new GameObject("Look At Target");
                lookAtTarget.transform.SetParent(cameraTargets.transform, false);

                var cameraRigObject = new GameObject("Camera Rig");
                cameraRigObject.transform.SetParent(temporary.transform, false);
                CameraRigComposer composer =
                    cameraRigObject.AddComponent<CameraRigComposer>();
                var serializedComposer = new SerializedObject(composer);
                serializedComposer.FindProperty("presentationIntent").intValue =
                    (int)CameraRigPresentationIntent.Follow;
                serializedComposer.FindProperty("targetSourceKind").intValue =
                    (int)CameraTargetSourceKind.ExplicitTransform;
                serializedComposer.FindProperty("explicitFollowTarget").objectReferenceValue =
                    followTarget.transform;
                serializedComposer.FindProperty("explicitLookAtTarget").objectReferenceValue =
                    lookAtTarget.transform;
                serializedComposer.FindProperty("followRequirement").intValue =
                    (int)CameraTargetRequirement.Required;
                serializedComposer.FindProperty("lookAtRequirement").intValue =
                    (int)CameraTargetRequirement.Optional;
                serializedComposer.FindProperty("followOffset").vector3Value =
                    LogicalActorCameraFollowOffset;
                serializedComposer.FindProperty("createCinemachineCameraIfMissing").boolValue =
                    true;
                serializedComposer.FindProperty("logApplyRebuildDiagnostics").boolValue =
                    false;
                serializedComposer.ApplyModifiedPropertiesWithoutUndo();

                CameraRigComposerApplyRebuildResult rigResult =
                    CameraRigComposerApplyRebuildUtility.ApplyOrRebuild(
                        composer,
                        false,
                        false);
                if (!rigResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Could not materialize the Logical Actor Host camera rig. " +
                        $"status='{rigResult.Status}' issue='{rigResult.BlockingIssue}' " +
                        $"materialization='{rigResult.MaterializationSummary}'.");
                }

                if (composer.CinemachineCamera == null ||
                    composer.CinemachineCamera.Follow != followTarget.transform)
                {
                    throw new InvalidOperationException(
                        "Logical Actor Host camera rig did not materialize with the explicit Follow Target.");
                }

                PlayerGameplayCameraAuthoring cameraAuthoring =
                    temporary.AddComponent<PlayerGameplayCameraAuthoring>();
                var serializedCamera = new SerializedObject(cameraAuthoring);
                serializedCamera.FindProperty("cameraRig").objectReferenceValue =
                    composer;
                serializedCamera.FindProperty("followTarget").objectReferenceValue =
                    followTarget.transform;
                serializedCamera.FindProperty("lookAtTarget").objectReferenceValue =
                    lookAtTarget.transform;
                serializedCamera.ApplyModifiedPropertiesWithoutUndo();

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, assetPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Could not create Logical Actor Host prefab at '{assetPath}'.");
                }

                return prefab;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporary);
            }
        }

        private static ActorProfile CreateOrUpdateActorProfile(
            string assetPath,
            string displayName,
            string actorProfileId,
            GameObject logicalHostPrefab)
        {
            ActorProfile profile = AssetDatabase.LoadAssetAtPath<ActorProfile>(assetPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ActorProfile>();
                AssetDatabase.CreateAsset(profile, assetPath);
            }

            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue = actorProfileId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("description").stringValue =
                "P3H.4 runtime-host Actor selection QA Profile.";
            serialized.FindProperty("actorKind").intValue = (int)ActorKind.Player;
            serialized.FindProperty("actorRole").intValue = (int)ActorRole.Protagonist;
            serialized.FindProperty("logicalActorHostPrefab").objectReferenceValue =
                logicalHostPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void ValidateLogicalActorHost(
            ActorProfile actorProfile,
            string actorRole)
        {
            if (actorProfile == null)
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} ActorProfile is missing.");
            }

            GameObject prefab = actorProfile.LogicalActorHostPrefab;
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} ActorProfile has no Logical Actor Host prefab.");
            }

            PlayerGameplayCameraAuthoring[] cameraAuthorings =
                prefab.GetComponentsInChildren<PlayerGameplayCameraAuthoring>(true);
            CameraRigComposer[] composers =
                prefab.GetComponentsInChildren<CameraRigComposer>(true);
            CinemachineCamera[] cinemachineCameras =
                prefab.GetComponentsInChildren<CinemachineCamera>(true);
            CinemachineFollow[] cinemachineFollows =
                prefab.GetComponentsInChildren<CinemachineFollow>(true);

            RequireExactlyOne(
                cameraAuthorings.Length,
                actorRole,
                nameof(PlayerGameplayCameraAuthoring));
            RequireExactlyOne(
                composers.Length,
                actorRole,
                nameof(CameraRigComposer));
            RequireExactlyOne(
                cinemachineCameras.Length,
                actorRole,
                nameof(CinemachineCamera));
            RequireExactlyOne(
                cinemachineFollows.Length,
                actorRole,
                nameof(CinemachineFollow));

            PlayerGameplayCameraAuthoring cameraAuthoring = cameraAuthorings[0];
            CameraRigComposer composer = composers[0];
            if (cameraAuthoring.CameraRig != composer)
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} camera authoring does not reference its CameraRigComposer.");
            }

            if (!composer.TryValidateForApply(out string validationIssue))
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} CameraRigComposer is invalid: {validationIssue}");
            }

            if (composer.PresentationIntent != CameraRigPresentationIntent.Follow ||
                composer.TargetSourceKind != CameraTargetSourceKind.ExplicitTransform ||
                composer.FollowRequirement != CameraTargetRequirement.Required ||
                composer.LookAtRequirement != CameraTargetRequirement.Optional ||
                composer.FollowOffset != LogicalActorCameraFollowOffset ||
                !composer.CreateCinemachineCameraIfMissing ||
                composer.LogApplyRebuildDiagnostics)
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} CameraRigComposer configuration drifted from the explicit QA contract.");
            }

            if (composer.CinemachineCamera == null ||
                composer.CinemachineCamera != cinemachineCameras[0] ||
                !composer.CinemachineCamera.transform.IsChildOf(composer.transform))
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} CameraRigComposer has no materialized CinemachineCamera reference.");
            }

            Transform followTarget = cameraAuthoring.FollowTarget;
            Transform lookAtTarget = cameraAuthoring.LookAtTarget;
            if (followTarget == null || !followTarget.IsChildOf(prefab.transform))
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} Follow Target is missing or outside the Logical Actor Host prefab.");
            }

            if (lookAtTarget == null || !lookAtTarget.IsChildOf(prefab.transform))
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} Look At Target is missing or outside the Logical Actor Host prefab.");
            }

            if (composer.ExplicitFollowTarget != followTarget ||
                composer.ExplicitLookAtTarget != lookAtTarget ||
                composer.CinemachineCamera.Follow != followTarget ||
                composer.CinemachineCamera.LookAt != lookAtTarget ||
                cinemachineFollows[0].FollowOffset != LogicalActorCameraFollowOffset)
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} camera references do not preserve the explicit target composition.");
            }

            RequireExactlyOne(
                CountNamedTransforms(prefab, "Camera Targets"),
                actorRole,
                "Camera Targets object");
            RequireExactlyOne(
                CountNamedTransforms(prefab, "Follow Target"),
                actorRole,
                "Follow Target object");
            RequireExactlyOne(
                CountNamedTransforms(prefab, "Look At Target"),
                actorRole,
                "Look At Target object");
            RequireExactlyOne(
                CountNamedTransforms(prefab, "Camera Rig"),
                actorRole,
                "Camera Rig object");

            if (prefab.GetComponentsInChildren<UnityEngine.Camera>(true).Length != 0)
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} Logical Actor Host must not own a Unity Camera.");
            }

            if (prefab.GetComponentsInChildren<CinemachineBrain>(true).Length != 0)
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} Logical Actor Host must not own a CinemachineBrain.");
            }
        }

        private static int CountNamedTransforms(GameObject root, string objectName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Count(candidate => candidate.name == objectName);
        }

        private static void RequireExactlyOne(
            int count,
            string actorRole,
            string evidence)
        {
            if (count != 1)
            {
                throw new InvalidOperationException(
                    $"Canonical {actorRole} Logical Actor Host requires exactly one {evidence}; actual='{count}'.");
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[index]);
                }
                current = next;
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

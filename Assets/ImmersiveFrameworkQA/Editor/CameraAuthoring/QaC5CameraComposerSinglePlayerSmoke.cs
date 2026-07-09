using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using Immersive.Framework.PlayerAuthoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.CameraAuthoring.Editor
{
    /// <summary>
    /// QAFramework editor smoke for the C5 CameraComposer SinglePlayer MVP.
    /// It builds an isolated authoring setup in the open scene, runs Validate/Apply twice,
    /// verifies idempotency, and verifies a negative missing PlayerComposer case.
    /// </summary>
    public static class QaC5CameraComposerSinglePlayerSmoke
    {
        private const string RootName = "QA_C5_CameraComposer_SinglePlayer_Smoke";
        private const string CameraScenePath = "Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera.unity";

        [MenuItem("Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke")]
        private static void Run()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path != CameraScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    Fail("camera-scene-open-cancelled");
                    return;
                }

                if (EditorSceneManager.OpenScene(CameraScenePath, OpenSceneMode.Single).path != CameraScenePath)
                {
                    Fail("camera-scene-not-opened");
                    return;
                }
            }

            GameObject previous = GameObject.Find(RootName);
            if (previous != null)
            {
                Object.DestroyImmediate(previous);
            }

            GameObject root = new GameObject(RootName);
            GameObject playerRoot = CreateChild(root.transform, "QA_PlayerPrototype");
            GameObject cameraTarget = CreateChild(playerRoot.transform, "CameraTarget");
            GameObject lookAtTarget = CreateChild(playerRoot.transform, "LookAtTarget");
            cameraTarget.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            lookAtTarget.transform.localPosition = new Vector3(0f, 1.8f, 1f);

            PlayerComposer playerComposer = playerRoot.AddComponent<PlayerComposer>();
            ConfigurePlayerComposer(playerComposer, cameraTarget.transform, lookAtTarget.transform);

            GameObject cameraRig = CreateChild(root.transform, "QA_CameraRig");
            CameraComposer cameraComposer = cameraRig.AddComponent<CameraComposer>();
            ConfigureCameraComposer(cameraComposer, playerComposer);

            CameraComposerApplyRebuildResult validation = CameraComposerApplyRebuildUtility.Validate(cameraComposer, true);
            Debug.Log($"[QA][C5 CameraComposer] Validate: succeeded='{validation.Succeeded}' status='{validation.Status}' blocked='{validation.BlockedCount}' issue='{validation.BlockingIssue}'");

            CameraComposerApplyRebuildResult first = CameraComposerApplyRebuildUtility.ApplyOrRebuild(cameraComposer, true, false);
            Debug.Log($"[QA][C5 CameraComposer] First Apply/Rebuild: succeeded='{first.Succeeded}' created='{first.CreatedCount}' repaired='{first.RepairedCount}' alreadyValid='{first.AlreadyValidCount}' skipped='{first.SkippedCount}' blocked='{first.BlockedCount}' issue='{first.BlockingIssue}'");

            CameraComposerApplyRebuildResult second = CameraComposerApplyRebuildUtility.ApplyOrRebuild(cameraComposer, true, false);
            Debug.Log($"[QA][C5 CameraComposer] Second Apply/Rebuild: succeeded='{second.Succeeded}' created='{second.CreatedCount}' repaired='{second.RepairedCount}' alreadyValid='{second.AlreadyValidCount}' skipped='{second.SkippedCount}' blocked='{second.BlockedCount}' issue='{second.BlockingIssue}'");

            GameObject negativeRig = CreateChild(root.transform, "QA_Negative_MissingPlayerComposer");
            CameraComposer negativeComposer = negativeRig.AddComponent<CameraComposer>();
            ConfigureCameraComposer(negativeComposer, null);
            CameraComposerApplyRebuildResult negative = CameraComposerApplyRebuildUtility.Validate(negativeComposer, true);
            Debug.Log($"[QA][C5 CameraComposer] Negative Missing PlayerComposer: succeeded='{negative.Succeeded}' status='{negative.Status}' blocked='{negative.BlockedCount}' issue='{negative.BlockingIssue}'");

            if (!validation.Succeeded)
            {
                Fail($"Validation failed unexpectedly. issue='{validation.BlockingIssue}'");
                return;
            }

            if (!first.Succeeded || first.BlockedCount != 0)
            {
                Fail($"First Apply/Rebuild failed unexpectedly. issue='{first.BlockingIssue}'");
                return;
            }

            if (!second.Succeeded || second.BlockedCount != 0)
            {
                Fail($"Second Apply/Rebuild failed unexpectedly. issue='{second.BlockingIssue}'");
                return;
            }

            if (second.CreatedCount != 0)
            {
                Fail($"Second Apply/Rebuild is not idempotent. created='{second.CreatedCount}'");
                return;
            }

            if (second.AlreadyValidCount <= 0)
            {
                Fail("Second Apply/Rebuild did not report already-valid materialization.");
                return;
            }

            if (negative.Succeeded || negative.BlockedCount == 0)
            {
                Fail("Negative missing PlayerComposer did not block as expected.");
                return;
            }

            Selection.activeGameObject = root;
            Debug.Log("[QA][C5 CameraComposer] PASS. SinglePlayer CameraComposer resolves PlayerComposer anchors, materializes a Cinemachine rig, remains idempotent, and blocks missing PlayerComposer.", root);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void ConfigurePlayerComposer(PlayerComposer composer, Transform cameraTarget, Transform lookAtTarget)
        {
            var serialized = new SerializedObject(composer);
            serialized.Update();
            SetString(serialized, "actorId", "qa.player.actor");
            SetString(serialized, "playerSlotId", "qa.player.1");
            SetObject(serialized, "cameraTarget", cameraTarget);
            SetObject(serialized, "lookAtTarget", lookAtTarget);
            SetBool(serialized, "inputBindingRequired", false);
            SetBool(serialized, "cameraBindingRequired", true);
            SetBool(serialized, "createBindingsRootIfMissing", true);
            SetBool(serialized, "createAnchorsIfMissing", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(composer);
        }

        private static void ConfigureCameraComposer(CameraComposer composer, PlayerComposer playerComposer)
        {
            var serialized = new SerializedObject(composer);
            serialized.Update();
            SetEnum(serialized, "mode", CameraMode.SinglePlayerFollowCamera);
            SetEnum(serialized, "ownershipScope", CameraOwnershipScope.SinglePlayer);
            SetEnum(serialized, "targetSourceKind", CameraTargetSourceKind.PlayerComposer);
            SetObject(serialized, "playerComposer", playerComposer);
            SetEnum(serialized, "followRequirement", CameraTargetRequirement.Required);
            SetEnum(serialized, "lookAtRequirement", CameraTargetRequirement.Optional);
            SetInt(serialized, "priority", 10);
            SetBool(serialized, "createUnityCameraIfMissing", true);
            SetBool(serialized, "createCinemachineCameraIfMissing", true);
            SetString(serialized, "unityCameraObjectName", "Unity Camera");
            SetString(serialized, "cinemachineCameraObjectName", "Cinemachine Camera");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(composer);
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.intValue = value;
            }
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetEnum<TEnum>(SerializedObject serialized, string propertyName, TEnum value)
            where TEnum : System.Enum
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = System.Convert.ToInt32(value);
            }
        }

        private static void Fail(string issue)
        {
            Debug.LogError($"[QA][C5 CameraComposer] FAIL. {issue}");
        }
    }
}

using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.Editor.CameraAuthoring;
using ImmersiveFrameworkQA.Camera;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// C5 smoke over the scene-authored Camera Product Surface fixture.
    /// </summary>
    public static class QaC5CameraComposerSinglePlayerSmoke
    {
        private const string ScenePath = "Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera_ProductSurface.unity";

        [MenuItem("Immersive Framework/QA/Camera/C5 CameraComposer SinglePlayer Smoke")]
        private static void Run()
        {
            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                Fail("fixture-not-found", $"active-scene='{SceneManager.GetActiveScene().path}' expected='{ScenePath}'");
                return;
            }

            QaCameraProductSurfaceFixture fixture = Object.FindAnyObjectByType<QaCameraProductSurfaceFixture>();
            if (fixture == null)
            {
                Fail("fixture-not-found");
                return;
            }

            if (fixture.CameraComposer == null || fixture.PlayerComposer == null || fixture.CameraTarget == null ||
                fixture.LookAtTarget == null || fixture.CameraRig == null || fixture.NegativeMissingPlayerComposer == null)
            {
                Fail("fixture-not-found");
                return;
            }

            CameraComposer composer = fixture.CameraComposer;
            if (composer.PlayerComposer != fixture.PlayerComposer ||
                fixture.PlayerComposer.CameraTarget != fixture.CameraTarget ||
                fixture.PlayerComposer.LookAtTarget != fixture.LookAtTarget)
            {
                Fail("fixture-target-references-not-resolved");
                return;
            }

            CameraComposerApplyRebuildResult validation = CameraComposerApplyRebuildUtility.Validate(composer, true);
            Debug.Log($"[QA][C5 CameraComposer] Validate: succeeded='{validation.Succeeded}' blocked='{validation.BlockedCount}' issue='{validation.BlockingIssue}'");
            if (!validation.Succeeded)
            {
                Fail("happy-path-validation-failed", validation.BlockingIssue);
                return;
            }

            CameraComposerApplyRebuildResult first = CameraComposerApplyRebuildUtility.ApplyOrRebuild(composer, true, false);
            Debug.Log($"[QA][C5 CameraComposer] First Apply/Rebuild: succeeded='{first.Succeeded}' created='{first.CreatedCount}' blocked='{first.BlockedCount}' issue='{first.BlockingIssue}'");
            if (!first.Succeeded || first.BlockedCount != 0)
            {
                Fail("first-apply-failed", first.BlockingIssue);
                return;
            }

            if (composer.CinemachineCamera == null)
            {
                Fail("cinemachine-rig-not-materialized");
                return;
            }

            if (composer.LastResolvedFollowTarget != fixture.CameraTarget)
            {
                Fail("follow-target-not-resolved");
                return;
            }

            if (composer.LastResolvedLookAtTarget != fixture.LookAtTarget)
            {
                Fail("look-at-target-not-resolved");
                return;
            }

            CameraComposerApplyRebuildResult second = CameraComposerApplyRebuildUtility.ApplyOrRebuild(composer, true, false);
            Debug.Log($"[QA][C5 CameraComposer] Second Apply/Rebuild: succeeded='{second.Succeeded}' created='{second.CreatedCount}' blocked='{second.BlockedCount}' issue='{second.BlockingIssue}'");
            if (!second.Succeeded || second.BlockedCount != 0)
            {
                Fail("second-apply-failed", second.BlockingIssue);
                return;
            }

            if (second.CreatedCount != 0)
            {
                Fail("second-apply-created-new-objects", $"created='{second.CreatedCount}'");
                return;
            }

            CameraComposerApplyRebuildResult negative = CameraComposerApplyRebuildUtility.Validate(
                fixture.NegativeMissingPlayerComposer,
                true);
            Debug.Log($"[QA][C5 CameraComposer] Negative Missing PlayerComposer: succeeded='{negative.Succeeded}' blocked='{negative.BlockedCount}' issue='{negative.BlockingIssue}'");
            if (negative.Succeeded || negative.BlockedCount == 0)
            {
                Fail("missing-player-composer-not-blocked");
                return;
            }

            Selection.activeGameObject = fixture.gameObject;
            Debug.Log("[QA][C5 CameraComposer] PASS. SinglePlayer CameraComposer resolves PlayerComposer anchors, materializes a Cinemachine rig, remains idempotent, and blocks missing PlayerComposer.", fixture);
        }

        private static void Fail(string code, string detail = null)
        {
            Debug.LogError($"[QA][C5 CameraComposer] FAIL. code='{code}' detail='{detail ?? string.Empty}'");
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.CameraAuthoring.Editor
{
    /// <summary>
    /// C7 regression entry point for the current Camera Product Surface QA chain.
    /// This runner does not define a new product contract. It opens the clean Camera Product Surface
    /// QA scene and delegates to the canonical C5 CameraComposer SinglePlayer smoke.
    /// </summary>
    public static class QaC7CameraProductSurfaceRegressionSmoke
    {
        private const string TargetScenePath = "Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_Camera_ProductSurface.unity";

        [MenuItem("Immersive Framework/QA/Camera/C7 Camera Product Surface Regression Smoke")]
        public static void Run()
        {
            if (!EnsureTargetSceneOpen())
            {
                return;
            }

            Debug.Log("[QA][C7 Camera Product Surface] Regression started. Delegating to C5 CameraComposer SinglePlayer smoke.");

            bool succeeded = ImmersiveFrameworkQA.Camera.Editor.QaC5CameraComposerSinglePlayerSmoke.RunForRegression();
            if (!succeeded)
            {
                Debug.LogError("[QA][C7 Camera Product Surface] FAILED. reason='c5-regression-failed'");
                return;
            }

            Debug.Log("[QA][C7 Camera Product Surface] PASS. C5 CameraComposer SinglePlayer regression passed.");
        }

        private static bool EnsureTargetSceneOpen()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && activeScene.path == TargetScenePath)
            {
                return true;
            }

            if (activeScene.IsValid() && activeScene.isDirty)
            {
                Debug.LogError(
                    "[QA][C7 Camera Product Surface] FAILED. reason='active-scene-dirty' " +
                    $"activeScene='{activeScene.path}' targetScene='{TargetScenePath}'");
                return false;
            }

            if (!File.Exists(TargetScenePath))
            {
                Debug.LogError(
                    "[QA][C7 Camera Product Surface] FAILED. reason='target-scene-not-found' " +
                    $"targetScene='{TargetScenePath}'");
                return false;
            }

            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
            return true;
        }
    }
}

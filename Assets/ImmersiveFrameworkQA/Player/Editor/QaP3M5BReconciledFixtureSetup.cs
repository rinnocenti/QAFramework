using System;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Canonical P3M5B fixture entry point.
    ///
    /// The original generator creates the complete asset/scene matrix. The persisted-reference
    /// repair then converts scene Actor references into a stable serialized shape, and the final
    /// preflight reopens every generated scene before the fixture is declared ready for Play Mode.
    /// </summary>
    public static class QaP3M5BReconciledFixtureSetup
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M5B Apply Reconciled Route Transition Fixture";

        private static bool ValidateApply()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    "[P3M5B_RECONCILED_FIXTURE] " +
                    "status='RejectedPlayMode' message='Exit Play Mode before applying the fixture.'.");
                return;
            }

            try
            {
                QaP3M5BRouteTransitionAndNegativeMatrixSetup.Apply();
                QaP3M5BPersistedSceneReferenceRepair.Repair();
                QaP3M5BPersistedFixturePreflight.ValidateOrThrow();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                Debug.Log(
                    "[P3M5B_RECONCILED_FIXTURE] " +
                    "status='Applied' generation='Passed' persistedRepair='Passed' " +
                    "postSavePreflight='Passed' readyForPlayMode='True'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3M5B_RECONCILED_FIXTURE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}'.");
                throw;
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

using System;
using ImmersiveFrameworkQA.Pooling;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Pooling.Editor
{
    internal static class PoolingRuntimeRegressionMenu
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Pooling/Run Pooling Runtime Regression";

        [MenuItem(MenuPath)]
        private static void Run()
        {
            if (!EditorApplication.isPlaying)
            {
                throw new InvalidOperationException(
                    "Enter Play Mode before running the Pooling Runtime Regression.");
            }

            PoolingQaPanel[] panels = UnityEngine.Object.FindObjectsByType<PoolingQaPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            PoolingQaPanel selected = null;
            int loadedPanelCount = 0;
            foreach (PoolingQaPanel panel in panels)
            {
                if (panel == null || !panel.gameObject.scene.IsValid() || !panel.gameObject.scene.isLoaded)
                {
                    continue;
                }

                selected = panel;
                loadedPanelCount++;
            }

            if (loadedPanelCount != 1)
            {
                throw new InvalidOperationException(
                    "Pooling Runtime Regression requires exactly one loaded PoolingQaPanel, " +
                    $"but found {loadedPanelCount}.");
            }

            selected.RunPoolingRuntimeRegression();
        }
    }
}

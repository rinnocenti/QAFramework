using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaC9IPersistentCompletion : MonoBehaviour
    {
        private const string LogPrefix = "[QA][C9I Canonical Camera Bindings]";

        private string[] completedBeforeExit;
        private bool cameraEnabledBefore;
        private bool cameraActiveBefore;
        private Vector3 cameraPositionBefore;
        private bool throwOnFailure;

        public void Configure(
            string[] completed,
            bool enabledBefore,
            bool activeBefore,
            Vector3 positionBefore,
            bool shouldThrow)
        {
            completedBeforeExit = completed ?? System.Array.Empty<string>();
            cameraEnabledBefore = enabledBefore;
            cameraActiveBefore = activeBefore;
            cameraPositionBefore = positionBefore;
            throwOnFailure = shouldThrow;

            StartCoroutine(CompleteAfterRouteExit());
        }

        private IEnumerator CompleteAfterRouteExit()
        {
            int frames = 0;

            while (!QaC9IRouteExitEvidence.Recorded && frames < 600)
            {
                frames++;
                yield return null;
            }

            yield return null;
            yield return null;

            try
            {
                if (!QaC9IRouteExitEvidence.Recorded)
                {
                    throw new System.InvalidOperationException(
                        "Route exit evidence was not recorded.");
                }

                if (!QaC9IRouteExitEvidence.BindingReleased)
                {
                    throw new System.InvalidOperationException(
                        "Route binding was not released before scene exit. " +
                        QaC9IRouteExitEvidence.Diagnostic);
                }

                if (!QaC9IRouteExitEvidence.OutputCleared)
                {
                    throw new System.InvalidOperationException(
                        "Camera output was not cleared on Route exit. " +
                        QaC9IRouteExitEvidence.Diagnostic);
                }

                var completed = new List<string>(completedBeforeExit)
                {
                    "canonical-route-exit-clears-output"
                };

                Debug.Log(
                    $"{LogPrefix} PASS. status='Passed' cases='{completed.Count}' " +
                    $"cameraEnabledBefore='{cameraEnabledBefore}' cameraActiveBefore='{cameraActiveBefore}' " +
                    $"cameraPositionBefore='{cameraPositionBefore}' " +
                    $"completed='{string.Join(",", completed)}'.",
                    this);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} FAIL. status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{exception.Message}'.",
                    this);

                if (throwOnFailure)
                {
                    throw;
                }
            }
            finally
            {
                Destroy(gameObject);
            }
        }
    }
}

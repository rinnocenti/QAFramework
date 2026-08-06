using System.Collections;
using Immersive.Foundation.Events;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// Persistent Hub coordinator that starts the  authority fixture only
    /// after the RouteRequestTrigger reports Completed/Succeeded.
    ///
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QaCameraOverrideAuthorityCompletionCoordinator : MonoBehaviour
    {
        private const int MaxFixtureLookupFrames = 600;

        [SerializeField] private RouteRequestTrigger routeTrigger;

        private IEventBinding binding;
        private bool waitingForTargetScene;

        private void Awake()
        {
            if (transform.parent != null)
            {
                Debug.LogError(
                    "[QA][ Camera Override Authority] Route completion coordinator must be installed on a root GameObject.",
                    this);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            if (routeTrigger == null)
            {
                Debug.LogError(
                    "[QA][ Camera Override Authority] Route completion coordinator requires an explicit RouteRequestTrigger.",
                    this);
                return;
            }

            binding = routeTrigger.SubscribeRequestEvents(
                HandleRouteRequestEvent);
        }

        private void OnDisable()
        {
            binding?.Dispose();
            binding = null;
        }

        private void HandleRouteRequestEvent(
            RouteRequestTriggerEvent routeEvent)
        {
            if (routeEvent == null ||
                !ReferenceEquals(routeEvent.Trigger, routeTrigger) ||
                !routeEvent.IsCompleted ||
                waitingForTargetScene)
            {
                return;
            }

            if (!routeEvent.Succeeded)
            {
                Debug.LogError(
                    "[QA][ Camera Override Authority] Route request did not succeed. " +
                    $"outcome='{routeEvent.Outcome}' message='{routeEvent.Message}'.",
                    this);
                Destroy(gameObject);
                return;
            }

            waitingForTargetScene = true;
            StartCoroutine(StartFixtureWhenAvailable());
        }

        private IEnumerator StartFixtureWhenAvailable()
        {
            for (int frame = 0; frame < MaxFixtureLookupFrames; frame++)
            {
                QaCameraOverrideAuthorityFixture fixture =
                    FindAnyObjectByType<QaCameraOverrideAuthorityFixture>(
                        FindObjectsInactive.Include);

                if (fixture != null)
                {
                    Debug.Log(
                        "[QA][ Camera Override Authority] Route request completed; starting smoke after transition-gate release.",
                        fixture);
                    fixture.Begin();
                    Destroy(gameObject);
                    yield break;
                }

                yield return null;
            }

            Debug.LogError(
                "[QA][ Camera Override Authority] Route request completed but the target fixture was not found before timeout.",
                this);
            Destroy(gameObject);
        }
    }
}

using System.Collections;
using Immersive.Foundation.Events;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaC9MRouteCompletionCoordinator : MonoBehaviour
    {
        [SerializeField] private RouteRequestTrigger routeTrigger;

        private IEventBinding binding;
        private bool waitingForTargetScene;

        private void Awake()
        {
            if (transform.parent != null)
            {
                Debug.LogError(
                    "[QA][C9M Activity Route Teardown] Route completion coordinator must be installed on a root GameObject.",
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
                    "[QA][C9M Activity Route Teardown] Route completion coordinator requires an explicit RouteRequestTrigger.",
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
                    "[QA][C9M Activity Route Teardown] Route request did not succeed. " +
                    $"outcome='{routeEvent.Outcome}' message='{routeEvent.Message}'.",
                    this);

                Destroy(gameObject);
                return;
            }

            waitingForTargetScene = true;
            StartCoroutine(StartSmokeWhenAvailable());
        }

        private IEnumerator StartSmokeWhenAvailable()
        {
            int frames = 0;

            while (frames < 600)
            {
                QaC9MRouteChangeCoordinator coordinator =
                    FindAnyObjectByType<QaC9MRouteChangeCoordinator>(
                        FindObjectsInactive.Include);

                if (coordinator != null)
                {
                    // Let the request completion unwind and release its
                    // transition-gate blocker before issuing the return route.
                    yield return null;

                    Debug.Log(
                        "[QA][C9M Activity Route Teardown] Route request completed; starting smoke.",
                        coordinator);

                    coordinator.Begin();
                    Destroy(gameObject);
                    yield break;
                }

                frames++;
                yield return null;
            }

            Debug.LogError(
                "[QA][C9M Activity Route Teardown] Target coordinator was not found before timeout.",
                this);

            Destroy(gameObject);
        }
    }
}

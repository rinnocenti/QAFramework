using System.Collections;
using Immersive.Foundation.Events;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaC9RCameraOverrideAuthorityLegacyCoordinator : MonoBehaviour
    {
        [SerializeField] private RouteRequestTrigger routeTrigger;

        private IEventBinding binding;
        private bool waitingForTargetScene;

        private void Awake()
        {
            if (transform.parent != null)
            {
                Debug.LogError(
                    "[QA][C9R Camera Override Authority] Legacy coordinator must be installed on a root GameObject.",
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
                    "[QA][C9R Camera Override Authority] Legacy coordinator requires an explicit RouteRequestTrigger.",
                    this);
                return;
            }

            binding = routeTrigger.SubscribeRequestEvents(HandleRouteRequestEvent);
        }

        private void OnDisable()
        {
            binding?.Dispose();
            binding = null;
        }

        private void HandleRouteRequestEvent(RouteRequestTriggerEvent routeEvent)
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
                    "[QA][C9R Camera Override Authority] Route request did not succeed. " +
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
            int frames = 0;

            while (frames < 600)
            {
                QaC9RCameraOverrideAuthorityFixture fixture =
                    FindFirstObjectByType<QaC9RCameraOverrideAuthorityFixture>(
                        FindObjectsInactive.Include);

                if (fixture != null)
                {
                    Debug.Log(
                        "[QA][C9R Camera Override Authority] Route request completed; starting smoke.",
                        fixture);
                    fixture.Begin();
                    Destroy(gameObject);
                    yield break;
                }

                frames++;
                yield return null;
            }

            Debug.LogError(
                "[QA][C9R Camera Override Authority] Target fixture was not found before timeout.",
                this);
            Destroy(gameObject);
        }
    }
}

using System.Collections;
using Immersive.Foundation.Events;
using Immersive.Framework.GameFlow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// Persistent Hub coordinator that starts the canonical Camera authority
    /// fixture only after its explicit Route request completes successfully.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QaCameraOverrideAuthorityCompletionCoordinator : MonoBehaviour
    {
        private const int MaxFixtureLookupFrames = 600;
        private const string TargetScenePath =
            "Assets/ImmersiveFrameworkQA/Camera/Scenes/QA_PlayerCameraArbitration.unity";
        private const string FixtureRootName =
            "QA_C9R_Controls";

        [SerializeField] private RouteRequestTrigger routeTrigger;

        private IEventBinding binding;
        private bool waitingForTargetScene;

        public RouteRequestTrigger RouteTrigger => routeTrigger;

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
                if (TryResolveCanonicalFixture(
                        out QaCameraOverrideAuthorityFixture fixture,
                        out string diagnostic))
                {
                    Debug.Log(
                        "[QA][ Camera Override Authority] Route request completed; starting canonical smoke after transition-gate release.",
                        fixture);
                    fixture.Begin();
                    Destroy(gameObject);
                    yield break;
                }

                if (diagnostic.StartsWith("Invalid", System.StringComparison.Ordinal))
                {
                    Debug.LogError(
                        "[QA][ Camera Override Authority] " + diagnostic,
                        this);
                    Destroy(gameObject);
                    yield break;
                }

                yield return null;
            }

            Debug.LogError(
                "[QA][ Camera Override Authority] Route request completed but the exact authored target fixture was not available before timeout.",
                this);
            Destroy(gameObject);
        }

        private static bool TryResolveCanonicalFixture(
            out QaCameraOverrideAuthorityFixture fixture,
            out string diagnostic)
        {
            fixture = null;

            Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                diagnostic = "Waiting for canonical Camera QA scene.";
                return false;
            }

            int namedRoots = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null || root.name != FixtureRootName)
                {
                    continue;
                }

                namedRoots++;
                QaCameraOverrideAuthorityFixture candidate =
                    root.GetComponent<QaCameraOverrideAuthorityFixture>();
                if (candidate != null)
                {
                    if (fixture != null)
                    {
                        diagnostic =
                            "Invalid Camera QA composition: multiple canonical fixture roots expose QaCameraOverrideAuthorityFixture.";
                        fixture = null;
                        return false;
                    }

                    fixture = candidate;
                }
            }

            if (namedRoots > 1)
            {
                diagnostic =
                    $"Invalid Camera QA composition: expected one root named '{FixtureRootName}', found '{namedRoots}'.";
                fixture = null;
                return false;
            }

            if (fixture == null)
            {
                diagnostic =
                    $"Waiting for exact fixture '{FixtureRootName}' in '{TargetScenePath}'.";
                return false;
            }

            diagnostic = string.Empty;
            return true;
        }
    }
}

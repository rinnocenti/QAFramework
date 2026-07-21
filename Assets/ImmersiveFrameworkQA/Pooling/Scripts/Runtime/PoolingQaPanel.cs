using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Contracts;
using Immersive.Pooling.Unity.Hosts;
using Immersive.Pooling.Unity.Runtime;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace ImmersiveFrameworkQA.Pooling
{
    /// <summary>
    /// QA-only IMGUI panel for the public Pooling Runtime Regression.
    /// </summary>
    [MovedFrom(
        true,
        sourceNamespace: "ImmersiveFrameworkQA.Pooling.ImmersiveFrameworkQA.Pooling.Scripts.Runtime",
        sourceAssembly: "ImmersiveFrameworkQA.Pooling.Runtime",
        sourceClassName: nameof(PoolingQaPanel))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Pooling/Pooling QA Panel")]
    public sealed class PoolingQaPanel : MonoBehaviour
    {
        private const string LogPrefix = "[POOLING_RUNTIME_REGRESSION]";

        [Header("Pooling")]
        [SerializeField] private PoolRuntimeHost poolRuntimeHost;
        [SerializeField] private PoolDefinitionAsset cubeDefinition;
        [SerializeField] private PoolDefinitionAsset limitedDefinition;
        [SerializeField] private PoolDefinitionAsset autoReturnDefinition;
        [SerializeField] private Transform spawnParent;

        [Header("Panel")]
        [SerializeField] private string title = "Pooling Runtime Regression";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 460f, 360f);

        private readonly List<GameObject> _rented = new List<GameObject>();
        private string _lastResult = "Ready.";
        private bool _lastPass;
        private bool _regressionRunning;
        private int _rentRequests;
        private int _returnRequests;

        public void RunPoolingRuntimeRegression()
        {
            if (_regressionRunning)
            {
                const string message = "Pooling Runtime Regression is already running.";
                SetPanelResult(false, message);
                Debug.LogError(
                    $"{LogPrefix} status='Failed' case='precondition' " +
                    $"message='{Escape(message)}'",
                    this);
                return;
            }

            StartCoroutine(RunPoolingRuntimeRegressionRoutine());
        }

        private IEnumerator RunPoolingRuntimeRegressionRoutine()
        {
            _regressionRunning = true;
            _rentRequests = 0;
            _returnRequests = 0;
            _lastPass = false;
            _lastResult = "Running basic.";

            var completed = new List<string>(3);
            string failedCase = null;
            string failureMessage = null;

            if (TryRunSynchronousCase(RunBasicCase, out failureMessage))
            {
                completed.Add("basic");
                _lastResult = "Running max-limit.";
            }
            else
            {
                failedCase = "basic";
            }

            if (failedCase == null)
            {
                if (TryRunSynchronousCase(RunMaxLimitCase, out failureMessage))
                {
                    completed.Add("max-limit");
                    _lastResult = "Running auto-return.";
                }
                else
                {
                    failedCase = "max-limit";
                }
            }

            if (failedCase == null)
            {
                bool autoReturnCompleted = false;
                string autoReturnFailure = null;
                yield return RunAutoReturnCase(
                    failure =>
                    {
                        autoReturnCompleted = true;
                        autoReturnFailure = failure;
                    });

                if (!autoReturnCompleted || autoReturnFailure != null)
                {
                    failedCase = "auto-return";
                    failureMessage = autoReturnFailure ??
                        "Auto-return case ended without completion evidence.";
                }
                else
                {
                    completed.Add("auto-return");
                }
            }

            bool cleanupCompleted = TryCleanupAndConfirmZeroActive(
                out string cleanupFailure);
            if (!cleanupCompleted)
            {
                if (failedCase == null)
                {
                    failedCase = "cleanup";
                    failureMessage = cleanupFailure;
                }
                else
                {
                    failureMessage =
                        $"{failureMessage} Cleanup failed: {cleanupFailure}";
                }
            }

            if (failedCase == null && completed.Count == 3)
            {
                SetPanelResult(
                    true,
                    "Passed: basic, max-limit, auto-return; cleanup confirmed.");
                Debug.Log(
                    "[POOLING_RUNTIME_REGRESSION] status='Passed' cases='3' completed='basic,max-limit,auto-return'",
                    this);
            }
            else
            {
                failedCase ??= "regression";
                failureMessage ??= "Regression ended without all three cases completed.";
                SetPanelResult(false, $"Failed at {failedCase}: {failureMessage}");
                Debug.LogError(
                    $"{LogPrefix} status='Failed' case='{failedCase}' " +
                    $"message='{Escape(failureMessage)}' " +
                    $"completed='{string.Join(",", completed)}'",
                    this);
            }

            _regressionRunning = false;
        }

        private void RunBasicCase()
        {
            PoolDefinitionAsset definition = RequireDefinition(cubeDefinition);
            IPoolService service = EnsureService();

            ClearDefinition(service, definition);
            service.Prewarm(definition);

            GameObject first = RentOne(definition);
            GameObject second = RentOne(definition);
            GameObject third = RentOne(definition);
            Require(first != null && second != null && third != null,
                "Expected three rented objects.");
            RequireSnapshot(service, definition, 3, "after three rents");

            Require(ReturnLast(definition),
                "Could not return the last rented object.");
            GameObject reused = RentOne(definition);
            Require(reused != null,
                "Could not rent an object after returning one.");

            ReturnAll(definition);
            RequireSnapshot(service, definition, 0, "after ReturnAll");
        }

        private void RunMaxLimitCase()
        {
            PoolDefinitionAsset definition = RequireDefinition(limitedDefinition);
            IPoolService service = EnsureService();

            ClearDefinition(service, definition);
            service.Prewarm(definition);
            RentOne(definition);
            RentOne(definition);

            bool thirdRentRejected = false;
            try
            {
                RentOne(definition);
            }
            catch (InvalidOperationException)
            {
                thirdRentRejected = true;
            }

            Require(thirdRentRejected,
                "Third rent was not rejected at maxSize=2 with expansion disabled.");
            Require(
                service.TryGetSnapshot(definition, out PoolRuntimeSnapshot snapshot) &&
                snapshot.TotalCount == 2 &&
                snapshot.ActiveCount == 2,
                "Expected total=2 and active=2 after the rejected third rent.");

            ReturnAll(definition);
            RequireSnapshot(service, definition, 0, "after max-limit ReturnAll");
        }

        private IEnumerator RunAutoReturnCase(Action<string> complete)
        {
            PoolDefinitionAsset definition;
            IPoolService service;
            try
            {
                definition = RequireDefinition(autoReturnDefinition);
                Require(definition.AutoReturnSeconds > 0f,
                    "autoReturnSeconds must be greater than zero.");
                service = EnsureService();
                ClearDefinition(service, definition);
                service.Prewarm(definition);
                Require(RentOne(definition) != null,
                    "Could not rent the auto-return object.");
                RequireSnapshot(service, definition, 1, "before auto-return wait");
            }
            catch (Exception exception)
            {
                complete(EscapeException(exception));
                yield break;
            }

            yield return new WaitForSeconds(definition.AutoReturnSeconds + 0.35f);

            try
            {
                Require(
                    service.TryGetSnapshot(definition, out PoolRuntimeSnapshot snapshot) &&
                    snapshot.ActiveCount == 0 &&
                    snapshot.InactiveCount >= 1,
                    $"Expected active=0 and inactive>=1 after auto-return. " +
                    $"Snapshot: {FormatSnapshot(snapshot)}");
                RemoveInactiveTracked();
                complete(null);
            }
            catch (Exception exception)
            {
                complete(EscapeException(exception));
            }
        }

        private bool TryCleanupAndConfirmZeroActive(out string failure)
        {
            try
            {
                IPoolService service = EnsureService();
                CleanupDefinition(service, cubeDefinition);
                CleanupDefinition(service, limitedDefinition);
                CleanupDefinition(service, autoReturnDefinition);
                RemoveInactiveTracked();

                Require(_rented.Count == 0,
                    $"Tracked active objects remain after cleanup: {_rented.Count}.");

                PoolingQaCallbackProbe[] probes = FindObjectsByType<PoolingQaCallbackProbe>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                int activeProbeCount = 0;
                foreach (PoolingQaCallbackProbe probe in probes)
                {
                    if (probe != null && probe.gameObject.activeInHierarchy)
                    {
                        activeProbeCount++;
                    }
                }

                Require(activeProbeCount == 0,
                    $"Active pooled objects remain after cleanup: {activeProbeCount}.");
                failure = null;
                return true;
            }
            catch (Exception exception)
            {
                failure = EscapeException(exception);
                return false;
            }
        }

        private static bool TryRunSynchronousCase(
            Action caseBody,
            out string failure)
        {
            try
            {
                caseBody();
                failure = null;
                return true;
            }
            catch (Exception exception)
            {
                failure = EscapeException(exception);
                return false;
            }
        }

        private void ClearDefinition(
            IPoolService service,
            PoolDefinitionAsset definition)
        {
            service.ReturnAll(definition);
            service.Clear(definition);
            RemoveInactiveTracked();
        }

        private void CleanupDefinition(
            IPoolService service,
            PoolDefinitionAsset definition)
        {
            if (definition == null)
            {
                return;
            }

            service.ReturnAll(definition);
            if (service.TryGetSnapshot(definition, out PoolRuntimeSnapshot snapshot))
            {
                Require(snapshot.ActiveCount == 0,
                    $"Definition '{definition.name}' retained active={snapshot.ActiveCount}.");
            }

            service.Clear(definition);
            Require(
                !service.TryGetSnapshot(definition, out _),
                $"Definition '{definition.name}' remained registered after cleanup.");
        }

        private static void RequireSnapshot(
            IPoolService service,
            PoolDefinitionAsset definition,
            int expectedActive,
            string phase)
        {
            Require(
                service.TryGetSnapshot(definition, out PoolRuntimeSnapshot snapshot) &&
                snapshot.ActiveCount == expectedActive,
                $"Expected active={expectedActive} {phase}. " +
                $"Snapshot: {FormatSnapshot(snapshot)}");
        }

        private GameObject RentOne(PoolDefinitionAsset definition)
        {
            GameObject instance = EnsureService().Rent(definition, spawnParent);
            _rentRequests++;
            if (instance != null && !_rented.Contains(instance))
            {
                _rented.Add(instance);
            }

            return instance;
        }

        private bool ReturnLast(PoolDefinitionAsset definition)
        {
            RemoveDestroyedTracked();
            Require(_rented.Count > 0,
                "No tracked rented object is available to return.");

            int lastIndex = _rented.Count - 1;
            GameObject instance = _rented[lastIndex];
            _rented.RemoveAt(lastIndex);
            bool returned = EnsureService().Return(definition, instance);
            if (returned)
            {
                _returnRequests++;
            }

            return returned;
        }

        private int ReturnAll(PoolDefinitionAsset definition)
        {
            int returned = EnsureService().ReturnAll(definition);
            _returnRequests += returned;
            RemoveInactiveTracked();
            return returned;
        }

        private IPoolService EnsureService()
        {
            if (poolRuntimeHost == null)
            {
                throw new InvalidOperationException(
                    "PoolingQaPanel requires a PoolRuntimeHost.");
            }

            if (!poolRuntimeHost.IsInitialized)
            {
                poolRuntimeHost.Initialize();
            }

            return poolRuntimeHost.Service;
        }

        private static PoolDefinitionAsset RequireDefinition(
            PoolDefinitionAsset definition)
        {
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Pooling QA definition is missing.");
            }

            definition.ValidateOrThrow();
            return definition;
        }

        private void SetPanelResult(bool passed, string message)
        {
            _lastPass = passed;
            _lastResult = message;
        }

        private void RemoveDestroyedTracked()
        {
            _rented.RemoveAll(item => item == null);
        }

        private void RemoveInactiveTracked()
        {
            _rented.RemoveAll(item => item == null || !item.activeSelf);
        }

        private string GetCallbacksSummary()
        {
            PoolingQaCallbackProbe[] probes = FindObjectsByType<PoolingQaCallbackProbe>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            int created = 0;
            int taken = 0;
            int returned = 0;
            int destroyed = 0;

            foreach (PoolingQaCallbackProbe probe in probes)
            {
                created += probe.CreatedCallbacks;
                taken += probe.TakenCallbacks;
                returned += probe.ReturnedCallbacks;
                destroyed += probe.DestroyedCallbacks;
            }

            return $"created={created} taken={taken} returned={returned} destroyed={destroyed}";
        }

        private static string FormatSnapshot(PoolRuntimeSnapshot snapshot)
        {
            return $"active={snapshot.ActiveCount} inactive={snapshot.InactiveCount} " +
                $"total={snapshot.TotalCount}";
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            panelRect = ClampToScreen(
                GUI.Window(
                    System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this),
                    panelRect,
                    DrawWindow,
                    title));
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(8f);
            GUILayout.Label($"Status: {(_regressionRunning ? "RUNNING" : _lastPass ? "PASS" : "READY/FAIL")}");
            GUILayout.Label($"Last Result: {_lastResult}");
            GUILayout.Label($"Tracked Active: {_rented.Count}");
            GUILayout.Label($"Rent Requests: {_rentRequests} | Return Requests: {_returnRequests}");
            GUILayout.Label($"Callbacks: {GetCallbacksSummary()}");
            GUILayout.Space(12f);

            GUI.enabled = !_regressionRunning;
            if (GUILayout.Button(
                    _regressionRunning
                        ? "Pooling Runtime Regression Running..."
                        : "Run Pooling Runtime Regression",
                    GUILayout.Height(36f)))
            {
                RunPoolingRuntimeRegression();
            }

            GUI.enabled = true;
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(300f, rect.width);
            float height = Mathf.Max(240f, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(
                Mathf.Clamp(rect.x, 0f, maxX),
                Mathf.Clamp(rect.y, 0f, maxY),
                width,
                height);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string EscapeException(Exception exception)
        {
            return $"{exception.GetType().Name}: {exception.Message}";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("'", "\\'", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal);
        }
    }
}

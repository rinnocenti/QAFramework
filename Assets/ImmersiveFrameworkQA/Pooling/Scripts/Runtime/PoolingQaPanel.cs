using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Contracts;
using Immersive.Pooling.Unity.Hosts;
using Immersive.Pooling.Unity.Runtime;
using UnityEngine;
namespace ImmersiveFrameworkQA.Pooling.ImmersiveFrameworkQA.Pooling.Scripts.Runtime
{
    /// <summary>
    /// QA-only IMGUI panel for synthetic validation of com.immersive.pooling.
    /// This script is a consumer of the pooling package; it is not framework runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Pooling/Pooling QA Panel")]
    public sealed class PoolingQaPanel : MonoBehaviour
    {
        private enum DefinitionSlot
        {
            Cube,
            Limited,
            AutoReturn
        }

        [Header("Pooling")]
        [SerializeField] private PoolRuntimeHost poolRuntimeHost;
        [SerializeField] private PoolDefinitionAsset cubeDefinition;
        [SerializeField] private PoolDefinitionAsset limitedDefinition;
        [SerializeField] private PoolDefinitionAsset autoReturnDefinition;
        [SerializeField] private Transform spawnParent;

        [Header("Panel")]
        [SerializeField] private string title = "Pooling QA";
        [SerializeField] private bool showPanel = true;
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 460f, 680f);

        private readonly List<GameObject> _rented = new List<GameObject>();
        private DefinitionSlot _slot = DefinitionSlot.Cube;
        private string _lastResult = "Idle.";
        private bool _lastPass;
        private bool _autoReturnRunning;
        private int _rentRequests;
        private int _returnRequests;
        private int _smokePasses;
        private int _smokeFailures;

        private PoolDefinitionAsset ActiveDefinition => _slot switch
        {
            DefinitionSlot.Limited => limitedDefinition,
            DefinitionSlot.AutoReturn => autoReturnDefinition,
            _ => cubeDefinition
        };

        [ContextMenu("Pooling QA/Prewarm Active")]
        public void PrewarmActive()
        {
            RunOperation("Prewarm", () =>
            {
                var service = EnsureService();
                service.Prewarm(RequireDefinition(ActiveDefinition));
                return true;
            });
        }

        [ContextMenu("Pooling QA/Rent One")]
        public void RentOneActive()
        {
            RunOperation("Rent One", () => RentOne(RequireDefinition(ActiveDefinition)) != null);
        }

        [ContextMenu("Pooling QA/Rent Three")]
        public void RentThreeActive()
        {
            RunOperation("Rent Three", () =>
            {
                var definition = RequireDefinition(ActiveDefinition);
                for (var i = 0; i < 3; i++)
                {
                    RentOne(definition);
                }

                return true;
            });
        }

        [ContextMenu("Pooling QA/Return Last")]
        public void ReturnLastActive()
        {
            RunOperation("Return Last", () => ReturnLast(RequireDefinition(ActiveDefinition)));
        }

        [ContextMenu("Pooling QA/Return All")]
        public void ReturnAllActive()
        {
            RunOperation("Return All", () =>
            {
                var returned = ReturnAll(RequireDefinition(ActiveDefinition));
                return returned >= 0;
            });
        }

        [ContextMenu("Pooling QA/Clear Active")]
        public void ClearActive()
        {
            RunOperation("Clear", () =>
            {
                var definition = RequireDefinition(ActiveDefinition);
                EnsureService().Clear(definition);
                RemoveTrackedForDefinition(definition);
                return true;
            });
        }

        [ContextMenu("Pooling QA/Run Basic Smoke")]
        public void RunBasicSmoke()
        {
            RunOperation("Basic Smoke", () =>
            {
                var definition = RequireDefinition(cubeDefinition);
                var service = EnsureService();

                service.Clear(definition);
                RemoveTrackedForDefinition(definition);
                service.Prewarm(definition);

                var first = RentOne(definition);
                var second = RentOne(definition);
                var third = RentOne(definition);

                if (first == null || second == null || third == null)
                {
                    return false;
                }

                if (!service.TryGetSnapshot(definition, out var rentedSnapshot) || rentedSnapshot.ActiveCount != 3)
                {
                    SetResult(false, "Basic Smoke failed: expected active=3 after three rents.");
                    return false;
                }

                if (!ReturnLast(definition))
                {
                    return false;
                }

                var reused = RentOne(definition);
                if (reused == null)
                {
                    return false;
                }

                ReturnAll(definition);

                if (!service.TryGetSnapshot(definition, out var finalSnapshot) || finalSnapshot.ActiveCount != 0)
                {
                    SetResult(false, "Basic Smoke failed: expected active=0 after Return All.");
                    return false;
                }

                return true;
            });
        }

        [ContextMenu("Pooling QA/Run Max Limit Smoke")]
        public void RunMaxLimitSmoke()
        {
            RunOperation("Max Limit Smoke", () =>
            {
                var definition = RequireDefinition(limitedDefinition);
                var service = EnsureService();

                service.Clear(definition);
                RemoveTrackedForDefinition(definition);
                service.Prewarm(definition);

                RentOne(definition);
                RentOne(definition);

                var thirdRentFailed = false;
                try
                {
                    RentOne(definition);
                }
                catch (InvalidOperationException)
                {
                    thirdRentFailed = true;
                }

                if (!thirdRentFailed)
                {
                    SetResult(false, "Max Limit Smoke failed: third rent should fail when maxSize=2 and canExpand=false.");
                    return false;
                }

                if (!service.TryGetSnapshot(definition, out var snapshot) || snapshot.TotalCount != 2)
                {
                    SetResult(false, "Max Limit Smoke failed: expected total=2 after rejected third rent.");
                    return false;
                }

                ReturnAll(definition);
                return true;
            });
        }

        [ContextMenu("Pooling QA/Run Auto Return Smoke")]
        public void RunAutoReturnSmoke()
        {
            if (_autoReturnRunning)
            {
                SetResult(false, "Auto Return Smoke is already running.");
                return;
            }

            StartCoroutine(RunAutoReturnSmokeRoutine());
        }

        [ContextMenu("Pooling QA/Reset Panel")]
        public void ResetPanel()
        {
            _rented.Clear();
            _rentRequests = 0;
            _returnRequests = 0;
            _smokePasses = 0;
            _smokeFailures = 0;
            SetResult(true, "Panel counters reset.");
        }

        private IEnumerator RunAutoReturnSmokeRoutine()
        {
            _autoReturnRunning = true;

            var definition = autoReturnDefinition;
            if (definition == null)
            {
                _autoReturnRunning = false;
                SetResult(false, "Auto Return Smoke skipped: auto return definition is missing.");
                yield break;
            }

            if (definition.AutoReturnSeconds <= 0f)
            {
                _autoReturnRunning = false;
                SetResult(false, "Auto Return Smoke skipped: autoReturnSeconds must be greater than zero.");
                yield break;
            }

            var service = EnsureService();
            service.Clear(definition);
            RemoveTrackedForDefinition(definition);
            service.Prewarm(definition);
            RentOne(definition);

            yield return new WaitForSeconds(definition.AutoReturnSeconds + 0.35f);

            var passed = service.TryGetSnapshot(definition, out var snapshot) && snapshot.ActiveCount == 0 && snapshot.InactiveCount >= 1;
            if (passed)
            {
                _rented.RemoveAll(item => item == null || !item.activeSelf);
            }

            _autoReturnRunning = false;
            SetResult(passed, passed
                ? "Auto Return Smoke PASS."
                : $"Auto Return Smoke failed. Snapshot: {FormatSnapshot(snapshot)}");
        }

        private GameObject RentOne(PoolDefinitionAsset definition)
        {
            var instance = EnsureService().Rent(definition, spawnParent);
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

            if (_rented.Count == 0)
            {
                SetResult(false, "Return Last failed: no tracked rented objects.");
                return false;
            }

            var lastIndex = _rented.Count - 1;
            var instance = _rented[lastIndex];
            _rented.RemoveAt(lastIndex);

            var returned = EnsureService().Return(definition, instance);
            if (returned)
            {
                _returnRequests++;
            }

            return returned;
        }

        private int ReturnAll(PoolDefinitionAsset definition)
        {
            var returned = EnsureService().ReturnAll(definition);
            _returnRequests += returned;
            RemoveTrackedForDefinition(definition);
            return returned;
        }

        private IPoolService EnsureService()
        {
            if (poolRuntimeHost == null)
            {
                throw new InvalidOperationException("PoolingQaPanel requires a PoolRuntimeHost.");
            }

            if (!poolRuntimeHost.IsInitialized)
            {
                poolRuntimeHost.Initialize();
            }

            return poolRuntimeHost.Service;
        }

        private static PoolDefinitionAsset RequireDefinition(PoolDefinitionAsset definition)
        {
            if (definition == null)
            {
                throw new InvalidOperationException("Pooling QA definition is missing.");
            }

            definition.ValidateOrThrow();
            return definition;
        }

        private void RunOperation(string operation, Func<bool> body)
        {
            try
            {
                var passed = body();
                SetResult(passed, passed ? $"{operation} PASS." : $"{operation} FAIL.");
            }
            catch (Exception exception)
            {
                SetResult(false, $"{operation} exception: {exception.GetType().Name}: {exception.Message}");
                Debug.LogWarning($"[POOLING_QA] {operation} failed. {exception}", this);
            }
        }

        private void SetResult(bool passed, string message)
        {
            _lastPass = passed;
            _lastResult = message;

            if (message.Contains("Smoke", StringComparison.OrdinalIgnoreCase))
            {
                if (passed)
                {
                    _smokePasses++;
                }
                else
                {
                    _smokeFailures++;
                }
            }

            Debug.Log($"[POOLING_QA] {message}", this);
        }

        private void RemoveDestroyedTracked()
        {
            _rented.RemoveAll(item => item == null);
        }

        private void RemoveTrackedForDefinition(PoolDefinitionAsset definition)
        {
            _rented.RemoveAll(item => item == null || !item.activeSelf || definition == null);
        }

        private string GetCallbacksSummary()
        {
            var probes = FindObjectsByType<PoolingQaCallbackProbe>(FindObjectsInactive.Include);
            var created = 0;
            var taken = 0;
            var returned = 0;
            var destroyed = 0;

            for (var i = 0; i < probes.Length; i++)
            {
                created += probes[i].CreatedCallbacks;
                taken += probes[i].TakenCallbacks;
                returned += probes[i].ReturnedCallbacks;
                destroyed += probes[i].DestroyedCallbacks;
            }

            return $"created={created} taken={taken} returned={returned} destroyed={destroyed}";
        }

        private string GetSnapshotText(PoolDefinitionAsset definition)
        {
            if (definition == null || poolRuntimeHost == null || !poolRuntimeHost.IsInitialized)
            {
                return "Snapshot: <not initialized>";
            }

            if (!poolRuntimeHost.Service.TryGetSnapshot(definition, out var snapshot))
            {
                return "Snapshot: <not registered>";
            }

            return $"Snapshot: {FormatSnapshot(snapshot)}";
        }

        private static string FormatSnapshot(PoolRuntimeSnapshot snapshot)
        {
            return $"active={snapshot.ActiveCount} inactive={snapshot.InactiveCount} total={snapshot.TotalCount}";
        }

        private void OnGUI()
        {
            if (!showPanel)
            {
                return;
            }

            panelRect = ClampToScreen(GUI.Window(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), panelRect, DrawWindow, title));
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(4f);

            GUILayout.Label($"Active Definition: {_slot}");
            GUILayout.Label($"{GetSnapshotText(ActiveDefinition)}");
            GUILayout.Label($"Tracked Active: {_rented.Count}");
            GUILayout.Label($"Rent Requests: {_rentRequests} | Return Requests: {_returnRequests}");
            GUILayout.Label($"Callbacks: {GetCallbacksSummary()}");
            GUILayout.Label($"Smoke Passes: {_smokePasses} | Smoke Failures: {_smokeFailures}");
            GUILayout.Label($"Last Result: {(_lastPass ? "PASS" : "FAIL")} - {_lastResult}");
            GUILayout.Space(8f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_slot == DefinitionSlot.Cube, "Cube", GUI.skin.button))
            {
                _slot = DefinitionSlot.Cube;
            }

            if (GUILayout.Toggle(_slot == DefinitionSlot.Limited, "Limited", GUI.skin.button))
            {
                _slot = DefinitionSlot.Limited;
            }

            if (GUILayout.Toggle(_slot == DefinitionSlot.AutoReturn, "Auto Return", GUI.skin.button))
            {
                _slot = DefinitionSlot.AutoReturn;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(8f);

            if (GUILayout.Button("Prewarm", GUILayout.Height(28f)))
            {
                PrewarmActive();
            }

            if (GUILayout.Button("Rent One", GUILayout.Height(28f)))
            {
                RentOneActive();
            }

            if (GUILayout.Button("Rent Three", GUILayout.Height(28f)))
            {
                RentThreeActive();
            }

            if (GUILayout.Button("Return Last", GUILayout.Height(28f)))
            {
                ReturnLastActive();
            }

            if (GUILayout.Button("Return All", GUILayout.Height(28f)))
            {
                ReturnAllActive();
            }

            if (GUILayout.Button("Clear Active", GUILayout.Height(28f)))
            {
                ClearActive();
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("Run Basic Smoke", GUILayout.Height(32f)))
            {
                RunBasicSmoke();
            }

            if (GUILayout.Button("Run Max Limit Smoke", GUILayout.Height(32f)))
            {
                RunMaxLimitSmoke();
            }

            GUI.enabled = !_autoReturnRunning;
            if (GUILayout.Button(_autoReturnRunning ? "Auto Return Smoke Running..." : "Run Auto Return Smoke", GUILayout.Height(32f)))
            {
                RunAutoReturnSmoke();
            }

            GUI.enabled = true;

            if (GUILayout.Button("Reset Panel Counters", GUILayout.Height(28f)))
            {
                ResetPanel();
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(300f, rect.width);
            float height = Mathf.Max(320f, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(Mathf.Clamp(rect.x, 0f, maxX), Mathf.Clamp(rect.y, 0f, maxY), width, height);
        }
    }
}

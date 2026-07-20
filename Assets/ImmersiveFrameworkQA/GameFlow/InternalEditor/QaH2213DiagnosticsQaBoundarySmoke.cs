using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.CycleReset;
using Immersive.Framework.Diagnostics;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH2213DiagnosticsQaBoundarySmoke
    {
        private const string LogPrefix =
            "[H2213_DIAGNOSTICS_QA_BOUNDARY_SMOKE]";

        private const string MenuPath =
            "Immersive Framework/QA/Game Flow/H2.2.13 Run Diagnostics QA Boundary Smoke";

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() =>
            EditorApplication.isPlaying;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            await RunInternalAsync();
        }

        public static async Task RunInternalAsync()
        {
            var completed =
                new List<string>();
            var temporaryObjects =
                new List<UnityObject>();

            int baselineCanvasCount =
                CountLoadedObjects<
                    FrameworkQaCanvas>();
            int baselineProbeCount =
                CountLoadedObjects<
                    CycleResetSmokeProbe>();

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "H2.2.13 smoke requires Play Mode.");

                Require(
                    FrameworkRuntimeHost.TryGetCurrent(
                        out FrameworkRuntimeHost host) &&
                    host != null,
                    "H2.2.13 smoke requires FrameworkRuntimeHost.");

                IFrameworkRuntimeDiagnosticsPort diagnosticsRuntime =
                    host;
                Require(
                    diagnosticsRuntime != null,
                    "FrameworkRuntimeHost does not expose IFrameworkRuntimeDiagnosticsPort.");
                completed.Add(
                    "diagnostics-runtime-port-available");

                FrameworkRuntimeDiagnosticsSnapshot snapshot =
                    diagnosticsRuntime.CreateFrameworkRuntimeDiagnosticsSnapshot();
                var state =
                    host.State;
                bool hostHasPause =
                    host.TryGetPauseSnapshot(
                        out var pauseSnapshot);

                Require(
                    snapshot.ApplicationName ==
                        (state.GameApplication != null
                            ? state.GameApplication.ApplicationName
                            : string.Empty) &&
                    ReferenceEquals(
                        snapshot.StartupRoute,
                        state.GameApplication != null
                            ? state.GameApplication.StartupRoute
                            : null) &&
                    snapshot.CurrentRouteName ==
                        state.CurrentRouteName &&
                    snapshot.CurrentActivityName ==
                        state.CurrentActivityName &&
                    snapshot.ContentAnchorBindingCount ==
                        host.ContentAnchorBindingCount &&
                    snapshot.HasPauseSnapshot ==
                        hostHasPause &&
                    (!hostHasPause ||
                        snapshot.PauseState ==
                            pauseSnapshot.State) &&
                    snapshot.PauseGateBlockerCount ==
                        host.PauseGateSnapshot.BlockerCount,
                    BuildSnapshotDiagnostic(
                        snapshot,
                        state.CurrentRouteName,
                        state.CurrentActivityName,
                        host.ContentAnchorBindingCount,
                        host.PauseGateSnapshot.BlockerCount));
                completed.Add(
                    "diagnostics-snapshot-matches-canonical-authority");

                RunBindingCases(
                    diagnosticsRuntime,
                    temporaryObjects,
                    completed);

                Type canvasType =
                    typeof(FrameworkQaCanvas);
                Require(
                    canvasType.GetField(
                        "_current",
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.NonPublic) == null &&
                    canvasType.GetMethod(
                        "TryGetRuntimeHost",
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic) == null &&
                    !HasFieldAssignableTo(
                        canvasType,
                        typeof(FrameworkRuntimeHost)),
                    "FrameworkQaCanvas still exposes static Current or concrete host access.");
                completed.Add(
                    "qa-canvas-has-no-static-current-or-host-surface");

                GameObject unboundRoot =
                    CreateInactiveRoot(
                        "H2213 Unbound Diagnostics Canvas",
                        temporaryObjects);
                FrameworkQaCanvas unboundCanvas =
                    unboundRoot.AddComponent<
                        FrameworkQaCanvas>();

                Require(
                    !unboundCanvas.HasFrameworkRuntimeDiagnosticsBinding &&
                    unboundCanvas.FrameworkRuntimeDiagnosticsBindingStatus ==
                        "Missing",
                    unboundCanvas.FrameworkRuntimeDiagnosticsBindingDiagnostic);

                diagnosticsRuntime.CreateFrameworkRuntimeDiagnosticsSnapshot();

                Require(
                    !unboundCanvas.HasFrameworkRuntimeDiagnosticsBinding,
                    "Unbound FrameworkQaCanvas acquired runtime authority without explicit binding.");
                completed.Add(
                    "unbound-canvas-does-not-fallback-to-current-host");

                Type probeType =
                    typeof(CycleResetSmokeProbe);
                Require(
                    !HasFieldAssignableTo(
                        probeType,
                        typeof(FrameworkRuntimeHost)) &&
                    probeType.GetMethod(
                        "RunCycleResetSmoke",
                        BindingFlags.Instance |
                        BindingFlags.Public) != null,
                    "CycleResetSmokeProbe still exposes a concrete runtime host dependency.");
                completed.Add(
                    "cycle-reset-probe-is-retired-and-host-free");

                GameObject probeRoot =
                    CreateInactiveRoot(
                        "H2213 Retired Cycle Reset Probe",
                        temporaryObjects);
                CycleResetSmokeProbe probe =
                    probeRoot.AddComponent<
                        CycleResetSmokeProbe>();

                FrameworkRuntimeDiagnosticsSnapshot beforeProbe =
                    diagnosticsRuntime.CreateFrameworkRuntimeDiagnosticsSnapshot();
                probe.RunCycleResetSmoke();
                FrameworkRuntimeDiagnosticsSnapshot afterProbe =
                    diagnosticsRuntime.CreateFrameworkRuntimeDiagnosticsSnapshot();

                Require(
                    !string.IsNullOrWhiteSpace(
                        probe.Diagnostic) &&
                    probe.Diagnostic.Contains(
                        "QAFramework") &&
                    beforeProbe.CurrentRouteName ==
                        afterProbe.CurrentRouteName &&
                    beforeProbe.CurrentActivityName ==
                        afterProbe.CurrentActivityName &&
                    beforeProbe.ContentAnchorBindingCount ==
                        afterProbe.ContentAnchorBindingCount &&
                    beforeProbe.PauseState ==
                        afterProbe.PauseState &&
                    beforeProbe.PauseGateBlockerCount ==
                        afterProbe.PauseGateBlockerCount,
                    "Retired CycleResetSmokeProbe mutated framework runtime state.");
                completed.Add(
                    "retired-cycle-reset-probe-executes-no-runtime-request");

                await CleanupTemporaryObjectsAsync(
                    temporaryObjects);

                int finalCanvasCount =
                    CountLoadedObjects<
                        FrameworkQaCanvas>();
                int finalProbeCount =
                    CountLoadedObjects<
                        CycleResetSmokeProbe>();

                Require(
                    finalCanvasCount ==
                        baselineCanvasCount &&
                    finalProbeCount ==
                        baselineProbeCount,
                    $"Temporary diagnostics cleanup failed. canvasBaseline='{baselineCanvasCount}' canvasFinal='{finalCanvasCount}' probeBaseline='{baselineProbeCount}' probeFinal='{finalProbeCount}'.");

                completed.Add(
                    "no-temporary-diagnostics-state-remains");

                Require(
                    completed.Count == 8,
                    $"Unexpected H2.2.13 case count. actual='{completed.Count}'.");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='8' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{exception.Message}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (temporaryObjects.Count > 0)
                {
                    await CleanupTemporaryObjectsAsync(
                        temporaryObjects);
                }
            }
        }

        private static void RunBindingCases(
            IFrameworkRuntimeDiagnosticsPort diagnosticsRuntime,
            ICollection<UnityObject> temporaryObjects,
            ICollection<string> completed)
        {
            GameObject emptyRoot =
                CreateInactiveRoot(
                    "H2213 Empty Diagnostics Root",
                    temporaryObjects);

            FrameworkQaCanvasBindingResult optionalAbsent =
                FrameworkQaCanvasBinding.TryBind(
                    new[]
                    {
                        emptyRoot,
                        emptyRoot
                    },
                    diagnosticsRuntime);

            Require(
                optionalAbsent.Succeeded &&
                optionalAbsent.Status ==
                    "OptionalAbsent" &&
                optionalAbsent.RootCount == 1 &&
                optionalAbsent.CanvasCount == 0,
                optionalAbsent.Message);

            GameObject authoredRoot =
                CreateInactiveRoot(
                    "H2213 Authored Diagnostics Root",
                    temporaryObjects);
            var child =
                new GameObject(
                    "H2213 Diagnostics Canvas");
            child.transform.SetParent(
                authoredRoot.transform,
                false);
            FrameworkQaCanvas canvas =
                child.AddComponent<
                    FrameworkQaCanvas>();

            FrameworkQaCanvasBindingResult missingRuntime =
                FrameworkQaCanvasBinding.TryBind(
                    new[] { authoredRoot },
                    null);

            Require(
                !missingRuntime.Succeeded &&
                missingRuntime.Status ==
                    "RejectedMissingFrameworkRuntimeDiagnostics" &&
                missingRuntime.RootCount == 1 &&
                missingRuntime.CanvasCount == 0 &&
                !canvas.HasFrameworkRuntimeDiagnosticsBinding,
                missingRuntime.Message);

            FrameworkQaCanvasBindingResult bound =
                FrameworkQaCanvasBinding.TryBind(
                    new[]
                    {
                        authoredRoot,
                        child,
                        authoredRoot,
                        null
                    },
                    diagnosticsRuntime);

            Require(
                bound.Succeeded &&
                bound.Status == "Bound" &&
                bound.RootCount == 2 &&
                bound.CanvasCount == 1 &&
                bound.BoundCount == 1 &&
                bound.IdempotentCount == 0 &&
                bound.RejectedCount == 0 &&
                canvas.HasFrameworkRuntimeDiagnosticsBinding,
                bound.Message);

            FrameworkQaCanvasBindingResult idempotent =
                FrameworkQaCanvasBinding.TryBind(
                    new[]
                    {
                        authoredRoot,
                        child
                    },
                    diagnosticsRuntime);

            Require(
                idempotent.Succeeded &&
                idempotent.Status == "Bound" &&
                idempotent.RootCount == 2 &&
                idempotent.CanvasCount == 1 &&
                idempotent.BoundCount == 0 &&
                idempotent.IdempotentCount == 1 &&
                idempotent.RejectedCount == 0,
                idempotent.Message);

            var divergent =
                new DivergentDiagnosticsRuntime();

            FrameworkQaCanvasBindingResult rejected =
                FrameworkQaCanvasBinding.TryBind(
                    new[] { authoredRoot },
                    divergent);

            Require(
                !rejected.Succeeded &&
                rejected.Status ==
                    "RejectedCanvasBinding" &&
                rejected.RootCount == 1 &&
                rejected.CanvasCount == 1 &&
                rejected.BoundCount == 0 &&
                rejected.IdempotentCount == 0 &&
                rejected.RejectedCount == 1 &&
                divergent.SnapshotRequestCount == 0,
                rejected.Message);

            completed.Add(
                "explicit-root-binding-missing-optional-idempotent-and-divergent-cases");
        }

        private static GameObject CreateInactiveRoot(
            string name,
            ICollection<UnityObject> temporaryObjects)
        {
            var root =
                new GameObject(name);
            root.SetActive(false);
            temporaryObjects.Add(root);
            return root;
        }

        private static async Task CleanupTemporaryObjectsAsync(
            IList<UnityObject> temporaryObjects)
        {
            for (int index =
                     temporaryObjects.Count - 1;
                 index >= 0;
                 index--)
            {
                if (temporaryObjects[index] != null)
                {
                    UnityObject.Destroy(
                        temporaryObjects[index]);
                }
            }

            temporaryObjects.Clear();
            await Awaitable.NextFrameAsync();
        }

        private static int CountLoadedObjects<T>()
            where T : Component
        {
            T[] candidates =
                Resources.FindObjectsOfTypeAll<T>();
            int count = 0;

            for (int index = 0;
                 index < candidates.Length;
                 index++)
            {
                T candidate =
                    candidates[index];
                if (candidate != null &&
                    candidate.gameObject.scene.IsValid() &&
                    candidate.gameObject.scene.isLoaded)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasFieldAssignableTo(
            Type type,
            Type fieldType)
        {
            FieldInfo[] fields =
                type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            for (int index = 0;
                 index < fields.Length;
                 index++)
            {
                if (fieldType.IsAssignableFrom(
                        fields[index].FieldType))
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildSnapshotDiagnostic(
            FrameworkRuntimeDiagnosticsSnapshot snapshot,
            string expectedRoute,
            string expectedActivity,
            int expectedBindings,
            int expectedPauseBlockers)
        {
            return
                $"application='{snapshot.ApplicationName}' " +
                $"route='{snapshot.CurrentRouteName}' expectedRoute='{expectedRoute}' " +
                $"activity='{snapshot.CurrentActivityName}' expectedActivity='{expectedActivity}' " +
                $"bindings='{snapshot.ContentAnchorBindingCount}' expectedBindings='{expectedBindings}' " +
                $"pause='{snapshot.PauseState}' blockers='{snapshot.PauseGateBlockerCount}' expectedBlockers='{expectedPauseBlockers}'.";
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }

        private sealed class DivergentDiagnosticsRuntime :
            IFrameworkRuntimeDiagnosticsPort
        {
            internal int SnapshotRequestCount { get; private set; }

            public FrameworkRuntimeDiagnosticsSnapshot
                CreateFrameworkRuntimeDiagnosticsSnapshot()
            {
                SnapshotRequestCount++;
                return new FrameworkRuntimeDiagnosticsSnapshot(
                    "Divergent QA Runtime",
                    null,
                    "divergent.route",
                    "divergent.activity",
                    0,
                    false,
                    Immersive.Framework.Pause.PauseState.Unknown,
                    0);
            }
        }
    }
}

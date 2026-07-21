using System;
using System.Collections.Generic;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH2211ContentAnchorMaterializationRuntimeBindingSmoke
    {
        private const string LogPrefix =
            "[H2211_CONTENT_ANCHOR_MATERIALIZATION_RUNTIME_BINDING_SMOKE]";
        private const string Source =
            nameof(QaH2211ContentAnchorMaterializationRuntimeBindingSmoke);

        [MenuItem(
            "Immersive Framework/QA/Regressions/Scene Composition/Run Content Anchor Materialization Regression",
            true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        [MenuItem(
            "Immersive Framework/QA/Regressions/Scene Composition/Run Content Anchor Materialization Regression")]
        public static void Run()
        {
            RunInternal();
        }

        public static void RunInternal()
        {
            var completed = new List<string>();
            var objects = new List<UnityObject>();
            UnityContentAnchorMaterializationBridge materializationBridge = null;
            IContentAnchorMaterializationRuntimePort hostRuntime = null;
            RuntimeContentOwner materializationOwner = default;
            bool materializationOwnerCreated = false;
            int baselineRootCount = 0;
            int baselineBindingCount = 0;

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "H2.2.11 vertical smoke requires Play Mode.");
                Require(
                    global::ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor.QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host) &&
                    host != null,
                    "H2.2.11 vertical smoke requires FrameworkRuntimeHost.");

                hostRuntime = host;
                Require(
                    hostRuntime != null && hostRuntime.ContentRuntime != null,
                    "FrameworkRuntimeHost did not expose Content Anchor materialization runtime port.");
                completed.Add("runtime-port-available");

                RunBindingCompositionCases(
                    hostRuntime,
                    objects,
                    completed);

                baselineRootCount = hostRuntime.ContentRuntime.RootCount;
                baselineBindingCount = host.ContentAnchorBindingCount;

                string runId = Guid.NewGuid().ToString("N");
                UnityContentAnchorMaterializationBridge unbound = CreateConfiguredBridge(
                    "H2211 Unbound Bridge",
                    $"qa.h2211.unbound.owner.{runId}",
                    $"qa.h2211.unbound.anchor-owner.{runId}",
                    $"qa.h2211.unbound.anchor.{runId}",
                    $"qa.h2211.unbound.content.{runId}",
                    objects,
                    out _,
                    out _);

                bool unboundPreflight = unbound.TryPreflightMaterializationForBridgeSet(
                    Source,
                    "unbound-preflight",
                    out string unboundPreflightMessage);
                UnityContentAnchorMaterializationBridgeResult unboundResult =
                    unbound.SubmitMaterializationForDiagnostics(
                        Source,
                        "unbound-materialize");
                Require(
                    !unboundPreflight &&
                    unboundPreflightMessage.Contains("not bound") &&
                    !unboundResult.Succeeded &&
                    unboundResult.Status ==
                        UnityContentAnchorMaterializationBridgeStatus.FailedRuntimeUnavailable &&
                    unboundResult.Message.Contains("not bound") &&
                    !unbound.HasContentAnchorMaterializationRuntimeBinding &&
                    unbound.RegistryCount == 0 &&
                    hostRuntime.ContentRuntime.RootCount == baselineRootCount &&
                    host.ContentAnchorBindingCount == baselineBindingCount,
                    BuildBridgeDiagnostic(unbound, unboundResult));
                completed.Add("unbound-bridge-does-not-fallback-to-current-host");

                string ownerId = $"qa.h2211.owner.{runId}";
                string ownerName = "H2.2.11 QA Materialization Owner";
                materializationOwner = RuntimeContentOwner.Transient(ownerId, ownerName);
                materializationOwnerCreated = true;
                materializationBridge = CreateConfiguredBridge(
                    "H2211 Materialization Bridge",
                    ownerId,
                    $"qa.h2211.anchor-owner.{runId}",
                    $"qa.h2211.anchor.{runId}",
                    $"qa.h2211.content.{runId}",
                    objects,
                    out Transform anchor,
                    out GameObject template);

                UnityContentAnchorMaterializationBridgeBindingResult bridgeBinding =
                    UnityContentAnchorMaterializationBridgeBinding.TryBind(
                        new[] { materializationBridge.gameObject },
                        hostRuntime);
                Require(
                    bridgeBinding.Succeeded &&
                    bridgeBinding.Status == "Bound" &&
                    bridgeBinding.RootCount == 1 &&
                    bridgeBinding.BridgeCount == 1 &&
                    bridgeBinding.BoundCount == 1 &&
                    materializationBridge.HasContentAnchorMaterializationRuntimeBinding,
                    bridgeBinding.Message);

                int rootsBeforePreflight = hostRuntime.ContentRuntime.RootCount;
                int bindingsBeforePreflight = host.ContentAnchorBindingCount;
                bool preflightSucceeded =
                    materializationBridge.TryPreflightMaterializationForBridgeSet(
                        Source,
                        "bound-preflight",
                        out string preflightMessage);
                Require(
                    preflightSucceeded &&
                    preflightMessage.Contains("succeeded") &&
                    hostRuntime.ContentRuntime.RootCount == rootsBeforePreflight &&
                    host.ContentAnchorBindingCount == bindingsBeforePreflight &&
                    materializationBridge.RegistryCount == 0,
                    preflightMessage);
                completed.Add("bound-preflight-validates-without-side-effects");

                UnityContentAnchorMaterializationBridgeResult materializationResult =
                    materializationBridge.SubmitMaterializationForDiagnostics(
                        Source,
                        "materialize");
                Require(
                    materializationResult.Succeeded &&
                    materializationResult.Materialized &&
                    materializationResult.Status ==
                        UnityContentAnchorMaterializationBridgeStatus.SucceededMaterialized &&
                    materializationResult.MaterializationAttempted &&
                    materializationResult.PhysicalPlacementApplied &&
                    materializationResult.RegistryEntries == 1 &&
                    materializationResult.RegistryActive == 1 &&
                    materializationResult.ContentHandleCount == 1 &&
                    materializationBridge.RegistryCount == 1 &&
                    materializationBridge.RegistryActiveCount == 1 &&
                    hostRuntime.ContentRuntime.RootCount == baselineRootCount + 1,
                    BuildBridgeDiagnostic(
                        materializationBridge,
                        materializationResult));

                Require(
                    hostRuntime.ContentRuntime.TryCreateScopeContext(
                        materializationOwner,
                        Source,
                        "inspect-materialization",
                        out RuntimeScopeContext materializationContext) &&
                    hostRuntime.ContentRuntime.SnapshotHandles(materializationContext).Length == 1,
                    "Materialization did not leave exactly one runtime content handle.");
                completed.Add("materialization-creates-one-physical-and-logical-handle");

                UnityRuntimeMaterializedObjectEvidence[] evidenceSnapshot =
                    materializationBridge.Registry.Snapshot();
                Require(
                    evidenceSnapshot.Length == 1 &&
                    evidenceSnapshot[0] != null &&
                    evidenceSnapshot[0].HasLiveInstance &&
                    evidenceSnapshot[0].Prefab == template,
                    materializationBridge.Registry.ToDiagnosticString());
                GameObject instance = evidenceSnapshot[0].Instance;
                Require(
                    instance != null &&
                    instance.transform.parent == anchor &&
                    Vector3.Distance(instance.transform.localPosition, Vector3.zero) < 0.0001f &&
                    Quaternion.Angle(instance.transform.localRotation, Quaternion.identity) < 0.001f &&
                    Vector3.Distance(instance.transform.localScale, Vector3.one) < 0.0001f,
                    evidenceSnapshot[0].ToDiagnosticString());
                completed.Add("placement-parents-instance-and-resets-local-transform");

                Require(
                    host.ContentAnchorBindingCount == baselineBindingCount + 1,
                    $"Content Anchor binding count mismatch. baseline='{baselineBindingCount}' current='{host.ContentAnchorBindingCount}'.");
                completed.Add("content-anchor-binding-created");

                var divergentRuntime =
                    new DelegatingContentAnchorMaterializationRuntimePort(hostRuntime);
                Require(
                    !materializationBridge.TryBindContentAnchorMaterializationRuntime(
                        divergentRuntime,
                        out string divergentIssue) &&
                    divergentIssue.Contains("different runtime port") &&
                    materializationBridge.HasContentAnchorMaterializationRuntimeBinding,
                    divergentIssue);

                UnityContentAnchorMaterializationBridgeResult releaseResult =
                    materializationBridge.SubmitScopeReleaseForDiagnostics(
                        Source,
                        "release");
                Require(
                    releaseResult.Succeeded &&
                    releaseResult.Released &&
                    releaseResult.Status ==
                        UnityContentAnchorMaterializationBridgeStatus.SucceededReleased &&
                    releaseResult.PhysicalReleaseRequests == 1 &&
                    releaseResult.LogicalReleaseResults == 1 &&
                    releaseResult.BindingRemovedCount == 1 &&
                    divergentRuntime.BindCallCount == 0 &&
                    divergentRuntime.UnbindHandleCallCount == 0 &&
                    divergentRuntime.UnbindOwnerCallCount == 0,
                    BuildBridgeDiagnostic(
                        materializationBridge,
                        releaseResult));
                completed.Add("divergent-rebind-cannot-redirect-release-authority");

                Require(
                    materializationBridge.RegistryCount == 1 &&
                    materializationBridge.RegistryActiveCount == 0 &&
                    materializationBridge.PhysicalReleaseRequestedCount == 1 &&
                    hostRuntime.ContentRuntime.SnapshotHandles(materializationContext).Length == 0 &&
                    host.ContentAnchorBindingCount == baselineBindingCount &&
                    evidenceSnapshot[0].PhysicalReleaseRequested,
                    BuildBridgeDiagnostic(
                        materializationBridge,
                        releaseResult));
                completed.Add("scope-release-cleans-active-physical-logical-and-binding-state");

                UnityContentAnchorMaterializationBridgeResult secondRelease =
                    materializationBridge.SubmitScopeReleaseForDiagnostics(
                        Source,
                        "release-idempotent");
                Require(
                    secondRelease.Succeeded &&
                    secondRelease.Status ==
                        UnityContentAnchorMaterializationBridgeStatus.SucceededReleaseNoContent &&
                    secondRelease.PhysicalReleaseRequests == 0 &&
                    secondRelease.LogicalReleaseResults == 0 &&
                    secondRelease.BindingRemovedCount == 0 &&
                    hostRuntime.ContentRuntime.SnapshotHandles(materializationContext).Length == 0 &&
                    host.ContentAnchorBindingCount == baselineBindingCount,
                    BuildBridgeDiagnostic(
                        materializationBridge,
                        secondRelease));
                completed.Add("release-is-idempotent-when-no-active-content-remains");

                RuntimeRootRegistryOperationResult rootRemoval =
                    hostRuntime.ContentRuntime.RemoveScopeRoot(
                        materializationOwner,
                        Source,
                        "smoke-cleanup");
                Require(
                    rootRemoval.Applied &&
                    hostRuntime.ContentRuntime.RootCount == baselineRootCount &&
                    host.ContentAnchorBindingCount == baselineBindingCount &&
                    materializationBridge.RegistryActiveCount == 0,
                    rootRemoval.Message);
                materializationOwnerCreated = false;
                completed.Add("temporary-runtime-scope-root-removed");

                completed.Add("no-active-content-anchor-materialization-state-remains");

                Debug.Log(
                    $"{LogPrefix} status='Passed' cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='Failed' message='{exception.Message}'.");
                throw;
            }
            finally
            {
                if (materializationBridge != null &&
                    materializationOwnerCreated &&
                    materializationBridge.HasContentAnchorMaterializationRuntimeBinding)
                {
                    try
                    {
                        materializationBridge.SubmitScopeReleaseForDiagnostics(
                            Source,
                            "finally-release");
                    }
                    catch
                    {
                        // Preserve the primary smoke failure.
                    }
                }

                if (hostRuntime != null &&
                    materializationOwnerCreated &&
                    materializationOwner.IsValid)
                {
                    try
                    {
                        hostRuntime.ContentRuntime.RemoveScopeRoot(
                            materializationOwner,
                            Source,
                            "finally-remove-root");
                    }
                    catch
                    {
                        // Preserve the primary smoke failure.
                    }
                }

                for (int index = objects.Count - 1; index >= 0; index--)
                {
                    if (objects[index] != null)
                    {
                        UnityObject.Destroy(objects[index]);
                    }
                }
            }
        }

        private static void RunBindingCompositionCases(
            IContentAnchorMaterializationRuntimePort hostRuntime,
            ICollection<UnityObject> objects,
            ICollection<string> completed)
        {
            GameObject emptyRoot = CreateInactiveRoot(
                "H2211 Binding Empty",
                objects);

            UnityContentAnchorMaterializationBridgeBindingResult missingRuntime =
                UnityContentAnchorMaterializationBridgeBinding.TryBind(
                    new[] { emptyRoot },
                    null);
            Require(
                !missingRuntime.Succeeded &&
                missingRuntime.Status ==
                    "RejectedMissingContentAnchorMaterializationRuntime" &&
                missingRuntime.RootCount == 1 &&
                missingRuntime.BridgeCount == 0,
                missingRuntime.Message);

            UnityContentAnchorMaterializationBridgeBindingResult optionalAbsent =
                UnityContentAnchorMaterializationBridgeBinding.TryBind(
                    new[] { emptyRoot, emptyRoot },
                    hostRuntime);
            Require(
                optionalAbsent.Succeeded &&
                optionalAbsent.Status == "OptionalAbsent" &&
                optionalAbsent.RootCount == 1 &&
                optionalAbsent.BridgeCount == 0,
                optionalAbsent.Message);

            GameObject authoredRoot = CreateInactiveRoot(
                "H2211 Binding Authored Root",
                objects);
            var child = new GameObject("H2211 Binding Bridge Child");
            child.transform.SetParent(authoredRoot.transform, false);
            var bridge =
                child.AddComponent<UnityContentAnchorMaterializationBridge>();

            UnityContentAnchorMaterializationBridgeBindingResult bound =
                UnityContentAnchorMaterializationBridgeBinding.TryBind(
                    new[] { authoredRoot, child, authoredRoot },
                    hostRuntime);
            Require(
                bound.Succeeded &&
                bound.Status == "Bound" &&
                bound.RootCount == 2 &&
                bound.BridgeCount == 1 &&
                bound.BoundCount == 1 &&
                bound.IdempotentCount == 0 &&
                bound.RejectedCount == 0 &&
                bridge.HasContentAnchorMaterializationRuntimeBinding,
                bound.Message);

            UnityContentAnchorMaterializationBridgeBindingResult idempotent =
                UnityContentAnchorMaterializationBridgeBinding.TryBind(
                    new[] { authoredRoot, child },
                    hostRuntime);
            Require(
                idempotent.Succeeded &&
                idempotent.BridgeCount == 1 &&
                idempotent.BoundCount == 0 &&
                idempotent.IdempotentCount == 1 &&
                idempotent.RejectedCount == 0,
                idempotent.Message);

            var divergentRuntime =
                new DelegatingContentAnchorMaterializationRuntimePort(hostRuntime);
            UnityContentAnchorMaterializationBridgeBindingResult divergent =
                UnityContentAnchorMaterializationBridgeBinding.TryBind(
                    new[] { authoredRoot },
                    divergentRuntime);
            Require(
                !divergent.Succeeded &&
                divergent.Status == "RejectedBridgeBinding" &&
                divergent.BridgeCount == 1 &&
                divergent.RejectedCount == 1 &&
                divergent.Message.Contains("different runtime port"),
                divergent.Message);

            completed.Add(
                "explicit-root-binding-missing-optional-idempotent-and-divergent-cases");
        }

        private static UnityContentAnchorMaterializationBridge CreateConfiguredBridge(
            string name,
            string runtimeOwnerId,
            string anchorOwnerId,
            string anchorId,
            string runtimeContentId,
            ICollection<UnityObject> objects,
            out Transform anchor,
            out GameObject template)
        {
            GameObject root = CreateInactiveRoot(name, objects);

            var anchorObject = new GameObject($"{name} Anchor");
            anchorObject.transform.SetParent(root.transform, false);
            anchorObject.transform.localPosition = new Vector3(3f, 4f, 5f);
            anchor = anchorObject.transform;

            template = new GameObject($"{name} Template");
            template.transform.SetParent(root.transform, false);
            template.transform.localPosition = new Vector3(7f, 8f, 9f);
            template.transform.localRotation = Quaternion.Euler(10f, 20f, 30f);
            template.transform.localScale = new Vector3(2f, 3f, 4f);
            template.SetActive(false);

            UnityContentAnchorMaterializationBridge bridge =
                root.AddComponent<UnityContentAnchorMaterializationBridge>();
            bridge.ConfigureForDiagnostics(
                template,
                anchor,
                RuntimeContentScope.Transient,
                runtimeOwnerId,
                name,
                true,
                ContentAnchorScope.Route,
                ContentAnchorKind.Slot,
                ContentAnchorRequiredness.Required,
                anchorOwnerId,
                anchorId,
                runtimeContentId,
                $"{runtimeContentId}.prefab",
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                true,
                false);
            return bridge;
        }

        private static GameObject CreateInactiveRoot(
            string name,
            ICollection<UnityObject> objects)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            objects.Add(root);
            return root;
        }

        private static string BuildBridgeDiagnostic(
            UnityContentAnchorMaterializationBridge bridge,
            UnityContentAnchorMaterializationBridgeResult result)
        {
            string resultDiagnostic = result != null
                ? result.ToDiagnosticString()
                : "<none>";
            return
                $"bridge='{bridge.name}' binding='{bridge.ContentAnchorMaterializationRuntimeBindingStatus}' bindingDiagnostic='{bridge.ContentAnchorMaterializationRuntimeBindingDiagnostic}' registryEntries='{bridge.RegistryCount}' registryActive='{bridge.RegistryActiveCount}' releaseRequested='{bridge.PhysicalReleaseRequestedCount}' result='{resultDiagnostic}'.";
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class DelegatingContentAnchorMaterializationRuntimePort :
            IContentAnchorMaterializationRuntimePort
        {
            private readonly IContentAnchorMaterializationRuntimePort _inner;

            internal DelegatingContentAnchorMaterializationRuntimePort(
                IContentAnchorMaterializationRuntimePort inner)
            {
                _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            }

            internal int BindCallCount { get; private set; }

            internal int UnbindHandleCallCount { get; private set; }

            internal int UnbindOwnerCallCount { get; private set; }

            public RuntimeContentRuntime ContentRuntime => _inner.ContentRuntime;

            public ContentAnchorBindingResult BindContentAnchor(
                ContentAnchorSet anchorSet,
                ContentAnchorBindingRequest request,
                string source,
                string reason)
            {
                BindCallCount++;
                return _inner.BindContentAnchor(
                    anchorSet,
                    request,
                    source,
                    reason);
            }

            public bool UnbindContentAnchor(ContentAnchorContentHandle handle)
            {
                UnbindHandleCallCount++;
                return _inner.UnbindContentAnchor(handle);
            }

            public ContentAnchorBindingLifecycleResult
                UnbindContentAnchorRuntimeOwner(
                    RuntimeContentOwner owner,
                    string source,
                    string reason)
            {
                UnbindOwnerCallCount++;
                return _inner.UnbindContentAnchorRuntimeOwner(
                    owner,
                    source,
                    reason);
            }
        }
    }
}

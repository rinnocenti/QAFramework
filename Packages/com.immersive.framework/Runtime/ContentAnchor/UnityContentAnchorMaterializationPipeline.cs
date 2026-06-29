using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Common;
using Immersive.Framework.RuntimeContent;
using UnityEngine;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Explicit Unity composition proof for prefab materialization, logical ContentAnchor binding and physical placement.
    /// This is not ContentAnchor core behavior, not automatic lifecycle wiring and not a consumer adapter.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9R-C Unity RuntimeContent/ContentAnchor composition proof; no automatic consumer ownership.")]
    internal sealed class UnityContentAnchorMaterializationPipeline
    {
        private readonly UnityPrefabRuntimeMaterializationAdapter _materializationAdapter;
        private readonly UnityContentAnchorPlacementAdapter _placementAdapter;
        private readonly UnityObjectRuntimeReleaseAdapter _releaseAdapter;
        private readonly string _source;

        internal UnityContentAnchorMaterializationPipeline(
            UnityPrefabRuntimeMaterializationAdapter materializationAdapter,
            UnityContentAnchorPlacementAdapter placementAdapter,
            UnityObjectRuntimeReleaseAdapter releaseAdapter,
            string source)
        {
            _materializationAdapter = materializationAdapter ?? throw new ArgumentNullException(nameof(materializationAdapter));
            _placementAdapter = placementAdapter ?? throw new ArgumentNullException(nameof(placementAdapter));
            _releaseAdapter = releaseAdapter ?? throw new ArgumentNullException(nameof(releaseAdapter));
            if (!ReferenceEquals(_materializationAdapter.Registry, _releaseAdapter.Registry))
            {
                throw new ArgumentException("Content Anchor materialization pipeline requires materialization and release adapters to share the same explicit Unity materialized object registry.", nameof(releaseAdapter));
            }

            _source = source.NormalizeTextOrFallback(nameof(UnityContentAnchorMaterializationPipeline));
        }

        internal UnityContentAnchorMaterializationPipelineResult MaterializeBindPlace(
            FrameworkRuntimeHost runtimeHost,
            ContentAnchorSet anchorSet,
            ContentAnchorBindingRequest bindingRequest,
            Transform anchorTransform,
            bool resetLocalTransform,
            string reason)
        {
            if (!bindingRequest.IsValid)
            {
                throw new ArgumentException("Content Anchor materialization pipeline requires a valid binding request.", nameof(bindingRequest));
            }

            if (runtimeHost == null)
            {
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedMissingRuntimeHost,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    default(RuntimeReleaseResult),
                    default(RuntimeReleaseResult),
                    false,
                    false,
                    reason,
                    "Content Anchor materialization pipeline requires an explicit FrameworkRuntimeHost.");
            }

            var runtimeContentRuntime = runtimeHost.RuntimeContentRuntime;
            if (runtimeContentRuntime == null)
            {
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedMissingRuntimeContentRuntime,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    default(RuntimeReleaseResult),
                    default(RuntimeReleaseResult),
                    false,
                    false,
                    reason,
                    "Content Anchor materialization pipeline requires RuntimeContentRuntime.");
            }

            if (anchorTransform == null)
            {
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedMissingAnchorTransform,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    default(RuntimeReleaseResult),
                    default(RuntimeReleaseResult),
                    false,
                    false,
                    reason,
                    "Content Anchor materialization pipeline requires an explicit anchor Transform before materialization side effects.");
            }

            if (!runtimeContentRuntime.TryCreateMaterializationRequest(
                    bindingRequest.RuntimeContext,
                    bindingRequest.RuntimeContentId,
                    bindingRequest.Resource,
                    _source,
                    reason,
                    out var materializationRequest,
                    out var guardResult))
            {
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedMaterializationRequest,
                    default(RuntimeMaterializationResult),
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    default(RuntimeReleaseResult),
                    default(RuntimeReleaseResult),
                    false,
                    false,
                    reason,
                    guardResult.Message);
            }

            var materializationResult = _materializationAdapter.Materialize(materializationRequest);
            if (!materializationResult.Succeeded)
            {
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedMaterialization,
                    materializationResult,
                    default(RuntimeMaterializationResult),
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    default(RuntimeReleaseResult),
                    default(RuntimeReleaseResult),
                    false,
                    false,
                    reason,
                    materializationResult.Message);
            }

            var appliedMaterializationResult = runtimeContentRuntime.ApplyMaterializationResult(
                materializationResult,
                _source,
                reason);
            if (!appliedMaterializationResult.Succeeded)
            {
                var rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    false,
                    runtimeHost,
                    default(ContentAnchorBindingResult),
                    reason);
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedMaterializationApply,
                    materializationResult,
                    appliedMaterializationResult,
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    rollback.PhysicalReleaseResult,
                    rollback.LogicalReleaseResult,
                    rollback.Attempted,
                    rollback.Succeeded,
                    reason,
                    appliedMaterializationResult.Message);
            }

            if (!_materializationAdapter.Registry.TryGet(materializationRequest.Identity, out var evidence)
                || evidence == null
                || !evidence.HasLiveInstance)
            {
                var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                    materializationRequest.Context,
                    materializationRequest.Identity,
                    RuntimeReleasePolicy.MarkReleasedAndUnregister,
                    _source,
                    reason);
                var physicalReleaseResult = _releaseAdapter.Release(releaseRequest);
                var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(physicalReleaseResult, _source, reason);
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedMissingPhysicalEvidence,
                    materializationResult,
                    appliedMaterializationResult,
                    default(ContentAnchorBindingResult),
                    default(UnityContentAnchorPlacementResult),
                    physicalReleaseResult,
                    logicalReleaseResult,
                    true,
                    physicalReleaseResult.Succeeded && logicalReleaseResult.Succeeded,
                    reason,
                    "Content Anchor materialization pipeline failed because physical materialization evidence was not available after successful materialization.");
            }

            var bindingResult = runtimeHost.BindContentAnchor(
                anchorSet,
                bindingRequest,
                _source,
                reason);
            if (!bindingResult.Succeeded)
            {
                var rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    false,
                    runtimeHost,
                    bindingResult,
                    reason);
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedLogicalBinding,
                    materializationResult,
                    appliedMaterializationResult,
                    bindingResult,
                    default(UnityContentAnchorPlacementResult),
                    rollback.PhysicalReleaseResult,
                    rollback.LogicalReleaseResult,
                    rollback.Attempted,
                    rollback.Succeeded,
                    reason,
                    bindingResult.Message);
            }

            var placementResult = _placementAdapter.Place(
                bindingResult,
                evidence,
                anchorTransform,
                resetLocalTransform,
                reason);
            if (!placementResult.Succeeded)
            {
                var rollback = RollbackPhysicalAndLogical(
                    runtimeContentRuntime,
                    materializationRequest,
                    true,
                    runtimeHost,
                    bindingResult,
                    reason);
                return Failure(
                    bindingRequest,
                    UnityContentAnchorMaterializationPipelineStatus.FailedPhysicalPlacement,
                    materializationResult,
                    appliedMaterializationResult,
                    bindingResult,
                    placementResult,
                    rollback.PhysicalReleaseResult,
                    rollback.LogicalReleaseResult,
                    rollback.Attempted,
                    rollback.Succeeded,
                    reason,
                    placementResult.Message);
            }

            return UnityContentAnchorMaterializationPipelineResult.Success(
                bindingRequest,
                materializationResult,
                appliedMaterializationResult,
                bindingResult,
                placementResult,
                _source,
                reason,
                "Content Anchor materialization pipeline materialized, logically bound and physically placed runtime content.");
        }

        private RollbackResult RollbackPhysicalAndLogical(
            RuntimeContentRuntime runtimeContentRuntime,
            RuntimeMaterializationRequest materializationRequest,
            bool shouldUnbind,
            FrameworkRuntimeHost runtimeHost,
            ContentAnchorBindingResult bindingResult,
            string reason)
        {
            if (shouldUnbind && bindingResult.HasHandle)
            {
                runtimeHost.UnbindContentAnchor(bindingResult.Handle);
            }

            var releaseRequest = runtimeContentRuntime.CreateReleaseRequest(
                materializationRequest.Context,
                materializationRequest.Identity,
                RuntimeReleasePolicy.MarkReleasedAndUnregister,
                _source,
                reason);
            var physicalReleaseResult = _releaseAdapter.Release(releaseRequest);
            if (!physicalReleaseResult.Succeeded)
            {
                return new RollbackResult(
                    true,
                    false,
                    physicalReleaseResult,
                    default(RuntimeReleaseResult));
            }

            var logicalReleaseResult = runtimeContentRuntime.ApplyReleaseResult(
                physicalReleaseResult,
                _source,
                reason);
            return new RollbackResult(
                true,
                logicalReleaseResult.Succeeded,
                physicalReleaseResult,
                logicalReleaseResult);
        }

        private UnityContentAnchorMaterializationPipelineResult Failure(
            ContentAnchorBindingRequest request,
            UnityContentAnchorMaterializationPipelineStatus status,
            RuntimeMaterializationResult materializationResult,
            RuntimeMaterializationResult appliedMaterializationResult,
            ContentAnchorBindingResult bindingResult,
            UnityContentAnchorPlacementResult placementResult,
            RuntimeReleaseResult rollbackPhysicalReleaseResult,
            RuntimeReleaseResult rollbackLogicalReleaseResult,
            bool rollbackAttempted,
            bool rollbackSucceeded,
            string reason,
            string message)
        {
            return UnityContentAnchorMaterializationPipelineResult.Failure(
                request,
                status,
                materializationResult,
                appliedMaterializationResult,
                bindingResult,
                placementResult,
                rollbackPhysicalReleaseResult,
                rollbackLogicalReleaseResult,
                rollbackAttempted,
                rollbackSucceeded,
                _source,
                reason,
                message);
        }

        private readonly struct RollbackResult
        {
            public RollbackResult(
                bool attempted,
                bool succeeded,
                RuntimeReleaseResult physicalReleaseResult,
                RuntimeReleaseResult logicalReleaseResult)
            {
                Attempted = attempted;
                Succeeded = succeeded;
                PhysicalReleaseResult = physicalReleaseResult;
                LogicalReleaseResult = logicalReleaseResult;
            }

            public bool Attempted { get; }

            public bool Succeeded { get; }

            public RuntimeReleaseResult PhysicalReleaseResult { get; }

            public RuntimeReleaseResult LogicalReleaseResult { get; }
        }
    }
}

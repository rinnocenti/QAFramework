using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Internal owner for runtime-created content state in the current framework runtime.
    /// It coordinates explicit scope roots, scope contexts, transition guards, scoped cancellation, passive handles and materialization request creation without becoming a service locator or materializer.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8H internal RuntimeContentRuntime owner; adds transition guard/scoped cancellation before concrete materializer execution.")]
    internal sealed class RuntimeContentRuntime
    {
        private readonly RuntimeRootRegistry registry;
        private readonly RuntimeScopeTransitionGuard transitionGuard;

        public RuntimeContentRuntime()
        {
            registry = new RuntimeRootRegistry();
            transitionGuard = new RuntimeScopeTransitionGuard();
        }

        public int RootCount => registry.RootCount;

        public bool HasRoots => registry.HasRoots;

        public RuntimeRootRegistryOperationResult CreateScopeRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            var result = registry.CreateRoot(owner, source, reason);

            if (result.Applied || result.Status == RuntimeRootRegistryOperationStatus.RootAlreadyExists)
            {
                transitionGuard.OpenScope(owner, source, reason);
            }

            return result;
        }

        public RuntimeRootRegistryOperationResult RemoveScopeRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            ValidateOwner(owner);

            transitionGuard.RequestCancellation(owner, source, reason);
            var result = registry.RemoveRoot(owner, source, reason);

            if (result.Applied || result.Status == RuntimeRootRegistryOperationStatus.RootMissing)
            {
                transitionGuard.MarkRemoved(owner, source, reason);
            }

            return result;
        }

        public bool TryCreateScopeContext(
            RuntimeContentOwner owner,
            string source,
            string reason,
            out RuntimeScopeContext context)
        {
            ValidateOwner(owner);

            if (!registry.TryGetRoot(owner, out _))
            {
                context = default(RuntimeScopeContext);
                return false;
            }

            context = new RuntimeScopeContext(owner, source, reason);
            return true;
        }

        public RuntimeScopeRoot[] SnapshotRoots()
        {
            return registry.SnapshotRoots();
        }

        public RuntimeScopeRoot[] SnapshotRoots(RuntimeContentScope scope)
        {
            return registry.SnapshotRoots(scope);
        }

        public RuntimeContentHandle[] SnapshotHandles(RuntimeScopeContext context)
        {
            ValidateContext(context);
            return registry.SnapshotHandles(context.Owner);
        }

        public RuntimeRootRegistryOperationResult DeclareHandle(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            string source,
            string reason)
        {
            ValidateContext(context);

            var identity = context.CreateIdentity(contentId);
            var handle = new RuntimeContentHandle(
                identity,
                RuntimeContentState.Declared,
                source,
                reason,
                "Runtime content handle declared by RuntimeContentRuntime.");

            return RegisterHandle(context, handle, source, reason);
        }

        public RuntimeRootRegistryOperationResult DeclareHandle(
            RuntimeScopeContext context,
            string contentId,
            string source,
            string reason)
        {
            return DeclareHandle(context, RuntimeContentId.From(contentId), source, reason);
        }

        public RuntimeRootRegistryOperationResult RegisterHandle(
            RuntimeScopeContext context,
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            ValidateContext(context);

            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (handle.Owner != context.Owner)
            {
                if (registry.TryGetRoot(context.Owner, out var contextRoot))
                {
                    return RuntimeRootRegistryOperationResult.RejectedMismatchedOwner(
                        contextRoot,
                        handle.Identity,
                        source,
                        reason);
                }

                return RuntimeRootRegistryOperationResult.RejectedMissingRoot(
                    context.Owner,
                    handle.Identity,
                    true,
                    source,
                    reason);
            }

            return registry.RegisterHandle(handle, source, reason);
        }

        public RuntimeRootRegistryOperationResult UnregisterHandle(
            RuntimeScopeContext context,
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            ValidateContext(context);
            ValidateIdentity(identity);

            if (identity.Owner != context.Owner)
            {
                if (registry.TryGetRoot(context.Owner, out var contextRoot))
                {
                    return RuntimeRootRegistryOperationResult.RejectedMismatchedOwner(
                        contextRoot,
                        identity,
                        source,
                        reason);
                }

                return RuntimeRootRegistryOperationResult.RejectedMissingRoot(
                    context.Owner,
                    identity,
                    true,
                    source,
                    reason);
            }

            return registry.UnregisterHandle(identity, source, reason);
        }

        public bool TryGetHandle(
            RuntimeScopeContext context,
            RuntimeContentIdentity identity,
            out RuntimeContentHandle handle)
        {
            ValidateContext(context);
            ValidateIdentity(identity);

            if (identity.Owner != context.Owner)
            {
                handle = null;
                return false;
            }

            return registry.TryGetHandle(identity, out handle);
        }

        public RuntimeMaterializationRequest CreateMaterializationRequest(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            if (!TryCreateMaterializationRequest(context, contentId, resource, source, reason, out var request, out var guardResult))
            {
                throw new InvalidOperationException(guardResult.ToDiagnosticString());
            }

            return request;
        }

        public RuntimeMaterializationRequest CreateMaterializationRequest(
            RuntimeScopeContext context,
            string contentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            return CreateMaterializationRequest(
                context,
                RuntimeContentId.From(contentId),
                resource,
                source,
                reason);
        }

        public bool TryCreateMaterializationRequest(
            RuntimeScopeContext context,
            RuntimeContentId contentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason,
            out RuntimeMaterializationRequest request,
            out RuntimeScopeTransitionGuardResult guardResult)
        {
            ValidateContext(context);

            if (!contentId.IsValid)
            {
                throw new ArgumentException("Runtime content id must be valid.", nameof(contentId));
            }

            if (!resource.IsValid)
            {
                throw new ArgumentException("Runtime materialization resource must be valid.", nameof(resource));
            }

            guardResult = transitionGuard.AllowMaterialization(context, source, reason);
            if (!guardResult.Allowed)
            {
                request = default(RuntimeMaterializationRequest);
                return false;
            }

            request = new RuntimeMaterializationRequest(
                context,
                contentId,
                resource,
                guardResult.Token,
                source,
                reason);

            return true;
        }

        public bool TryCreateMaterializationRequest(
            RuntimeScopeContext context,
            string contentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason,
            out RuntimeMaterializationRequest request,
            out RuntimeScopeTransitionGuardResult guardResult)
        {
            return TryCreateMaterializationRequest(
                context,
                RuntimeContentId.From(contentId),
                resource,
                source,
                reason,
                out request,
                out guardResult);
        }

        public RuntimeMaterializationResult RejectMaterializationFromGuard(
            RuntimeMaterializationRequest request,
            string source,
            string reason)
        {
            ValidateMaterializationRequest(request);

            var guardResult = transitionGuard.ValidateMaterializationRequest(request, source, reason);
            if (guardResult.Allowed)
            {
                return RuntimeMaterializationResult.Failure(
                    request,
                    RuntimeMaterializationStatus.FailedInvalidRequest,
                    source,
                    reason,
                    "Runtime materialization request is still allowed; no guard rejection was produced.");
            }

            return RuntimeMaterializationResult.Failure(
                request,
                guardResult.ToMaterializationStatus(),
                source,
                reason,
                guardResult.Message);
        }

        public bool IsMaterializationRequestCurrent(
            RuntimeMaterializationRequest request,
            string source,
            string reason,
            out RuntimeScopeTransitionGuardResult guardResult)
        {
            ValidateMaterializationRequest(request);
            guardResult = transitionGuard.ValidateMaterializationRequest(request, source, reason);
            return guardResult.Allowed;
        }

        public string ToDiagnosticString()
        {
            return registry.ToDiagnosticString();
        }

        private static void ValidateOwner(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }
        }

        private static void ValidateContext(RuntimeScopeContext context)
        {
            if (!context.IsValid)
            {
                throw new ArgumentException("Runtime scope context must be valid.", nameof(context));
            }
        }

        private static void ValidateIdentity(RuntimeContentIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }
        }

        private static void ValidateMaterializationRequest(RuntimeMaterializationRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Runtime materialization request must be valid.", nameof(request));
            }
        }
    }
}

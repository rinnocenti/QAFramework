using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Internal owner for runtime-created content state in the current framework runtime.
    /// It coordinates explicit scope roots, scope contexts and passive handles without becoming a service locator or materializer.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8F internal RuntimeContentRuntime owner; coordinates lifecycle-created roots/context/handles, no materialization or release execution.")]
    internal sealed class RuntimeContentRuntime
    {
        private readonly RuntimeRootRegistry registry;

        public RuntimeContentRuntime()
        {
            registry = new RuntimeRootRegistry();
        }

        public int RootCount => registry.RootCount;

        public bool HasRoots => registry.HasRoots;

        public RuntimeRootRegistryOperationResult CreateScopeRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            return registry.CreateRoot(owner, source, reason);
        }

        public RuntimeRootRegistryOperationResult RemoveScopeRoot(
            RuntimeContentOwner owner,
            string source,
            string reason)
        {
            return registry.RemoveRoot(owner, source, reason);
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
    }
}

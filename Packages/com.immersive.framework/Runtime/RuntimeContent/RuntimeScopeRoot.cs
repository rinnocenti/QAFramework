using System;
using System.Collections.Generic;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Internal logical root for runtime-created content owned by one lifecycle scope owner.
    /// F8D keeps this as a registry boundary only: no GameObject, Transform, Instantiate, Destroy or Content Anchor binding.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8D internal logical runtime scope root; no hierarchy object or materialization behavior.")]
    internal sealed class RuntimeScopeRoot
    {
        private readonly Dictionary<RuntimeContentId, RuntimeContentHandle> handles = new Dictionary<RuntimeContentId, RuntimeContentHandle>();
        private string source;
        private string reason;

        public RuntimeScopeRoot(RuntimeContentOwner owner, string source, string reason)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime scope root owner must be valid.", nameof(owner));
            }

            Owner = owner;
            this.source = Normalize(source);
            this.reason = Normalize(reason);
        }

        public RuntimeContentOwner Owner { get; }

        public RuntimeContentScope Scope => Owner.Scope;

        public string Source => source ?? string.Empty;

        public string Reason => reason ?? string.Empty;

        public int HandleCount => handles.Count;

        public bool HasHandles => handles.Count > 0;

        public RuntimeContentHandle[] SnapshotHandles()
        {
            var snapshot = new RuntimeContentHandle[handles.Count];
            handles.Values.CopyTo(snapshot, 0);
            return snapshot;
        }

        public bool TryGetHandle(RuntimeContentIdentity identity, out RuntimeContentHandle handle)
        {
            ValidateIdentity(identity);

            if (identity.Owner != Owner)
            {
                handle = null;
                return false;
            }

            return handles.TryGetValue(identity.ContentId, out handle);
        }

        public bool TryGetHandle(RuntimeContentId contentId, out RuntimeContentHandle handle)
        {
            if (!contentId.IsValid)
            {
                throw new ArgumentException("Runtime content id must be valid.", nameof(contentId));
            }

            return handles.TryGetValue(contentId, out handle);
        }

        public RuntimeRootRegistryOperationResult RegisterHandle(
            RuntimeContentHandle handle,
            string source,
            string reason)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            if (handle.Owner != Owner)
            {
                return RuntimeRootRegistryOperationResult.RejectedMismatchedOwner(
                    this,
                    handle.Identity,
                    source,
                    reason);
            }

            if (handles.TryGetValue(handle.ContentId, out var existingHandle))
            {
                if (ReferenceEquals(existingHandle, handle))
                {
                    return RuntimeRootRegistryOperationResult.HandleAlreadyRegistered(
                        this,
                        handle,
                        source,
                        reason);
                }

                return RuntimeRootRegistryOperationResult.RejectedDuplicateHandle(
                    this,
                    existingHandle,
                    source,
                    reason);
            }

            handles.Add(handle.ContentId, handle);
            Touch(source, reason);

            return RuntimeRootRegistryOperationResult.HandleRegistered(
                this,
                handle,
                source,
                reason);
        }

        public RuntimeRootRegistryOperationResult UnregisterHandle(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            ValidateIdentity(identity);

            if (identity.Owner != Owner)
            {
                return RuntimeRootRegistryOperationResult.RejectedMismatchedOwner(
                    this,
                    identity,
                    source,
                    reason);
            }

            if (!handles.TryGetValue(identity.ContentId, out var handle))
            {
                return RuntimeRootRegistryOperationResult.HandleMissing(
                    this,
                    identity,
                    source,
                    reason);
            }

            handles.Remove(identity.ContentId);
            Touch(source, reason);

            return RuntimeRootRegistryOperationResult.HandleUnregistered(
                this,
                handle,
                source,
                reason);
        }

        public string ToDiagnosticString()
        {
            var sourceText = !string.IsNullOrWhiteSpace(Source) ? Source : "<none>";
            var reasonText = !string.IsNullOrWhiteSpace(Reason) ? Reason : "<none>";
            return $"owner='{Owner.StableText}' scope='{Scope}' handleCount='{HandleCount}' source='{sourceText}' reason='{reasonText}'";
        }

        private void Touch(string source, string reason)
        {
            this.source = Normalize(source);
            this.reason = Normalize(reason);
        }

        private static void ValidateIdentity(RuntimeContentIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

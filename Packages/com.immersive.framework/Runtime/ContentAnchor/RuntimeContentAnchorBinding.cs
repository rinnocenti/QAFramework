using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.ContentAnchor
{
    /// <summary>
    /// API status: Experimental. Internal logical runtime for Content Anchor binding correlation.
    /// It resolves explicit binding requests against a passive ContentAnchorSet and registered RuntimeContent handles;
    /// it does not move transforms, instantiate prefabs, unload scenes, perform physical release or create fallback anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F9B RuntimeContentAnchorBinding logical runtime; resolves anchor/runtime handle correlation without physical placement.")]
    internal sealed class RuntimeContentAnchorBinding
    {
        private readonly Dictionary<string, ContentAnchorContentHandle> bindings;

        public RuntimeContentAnchorBinding()
        {
            bindings = new Dictionary<string, ContentAnchorContentHandle>(StringComparer.Ordinal);
        }

        public int BindingCount => bindings.Count;

        public bool HasBindings => BindingCount > 0;

        public ContentAnchorContentHandle[] SnapshotBindings()
        {
            if (bindings.Count == 0)
            {
                return Array.Empty<ContentAnchorContentHandle>();
            }

            var results = new ContentAnchorContentHandle[bindings.Count];
            bindings.Values.CopyTo(results, 0);
            return results;
        }

        public bool TryGetBinding(
            ContentAnchorBindingRequest request,
            out ContentAnchorContentHandle handle)
        {
            ValidateRequest(request);

            if (bindings.TryGetValue(CreateBindingKey(request), out handle) && handle.IsValid)
            {
                return true;
            }

            handle = default(ContentAnchorContentHandle);
            return false;
        }

        public ContentAnchorBindingResult Bind(
            ContentAnchorSet anchorSet,
            RuntimeContentRuntime runtimeContentRuntime,
            ContentAnchorBindingRequest request,
            string source,
            string reason)
        {
            if (runtimeContentRuntime == null)
            {
                throw new ArgumentNullException(nameof(runtimeContentRuntime));
            }

            ValidateRequest(request);

            if (!anchorSet.TryGetByBindingRequest(request, out var anchor))
            {
                return ContentAnchorBindingResult.MissingAnchor(
                    request,
                    source,
                    reason,
                    "Content Anchor binding failed because the requested anchor was not present in the supplied ContentAnchorSet.");
            }

            if (!request.Matches(anchor))
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedMismatchedAnchor,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding failed because the resolved anchor did not match the request.");
            }

            if (!runtimeContentRuntime.TryGetHandle(request.RuntimeContext, request.RuntimeIdentity, out var runtimeHandle))
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.FailedRuntimeRegistration,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding failed because the runtime content handle is not registered in the request scope root.");
            }

            if (runtimeHandle == null || runtimeHandle.Identity != request.RuntimeIdentity)
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedMismatchedRuntimeContent,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding failed because the runtime content handle does not match the request identity.");
            }

            if (runtimeHandle.IsReleased)
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedReleasedRuntimeContent,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding failed because the runtime content handle is already released.");
            }

            var bindingKey = CreateBindingKey(request);
            if (bindings.TryGetValue(bindingKey, out var existingHandle))
            {
                return EvaluateExistingBinding(
                    request,
                    anchor,
                    runtimeHandle,
                    existingHandle,
                    source,
                    reason);
            }

            var result = ContentAnchorBindingResult.Success(
                request,
                anchor,
                runtimeHandle,
                source,
                reason,
                "Content Anchor binding succeeded logically.");

            bindings.Add(bindingKey, result.Handle);
            return result;
        }

        public bool Unbind(ContentAnchorBindingRequest request)
        {
            ValidateRequest(request);
            return bindings.Remove(CreateBindingKey(request));
        }

        public bool Unbind(ContentAnchorContentHandle handle)
        {
            if (!handle.IsValid)
            {
                throw new ArgumentException("Content Anchor binding handle must be valid.", nameof(handle));
            }

            return bindings.Remove(CreateBindingKey(handle.Request));
        }

        public int RemoveBindingsForRuntimeContent(RuntimeContentIdentity identity)
        {
            ValidateIdentity(identity);

            if (bindings.Count == 0)
            {
                return 0;
            }

            var keys = new List<string>();
            foreach (var pair in bindings)
            {
                if (pair.Value.RuntimeIdentity == identity)
                {
                    keys.Add(pair.Key);
                }
            }

            return RemoveKeys(keys);
        }

        public int RemoveBindingsForRuntimeOwner(RuntimeContentOwner owner)
        {
            ValidateOwner(owner);

            if (bindings.Count == 0)
            {
                return 0;
            }

            var keys = new List<string>();
            foreach (var pair in bindings)
            {
                if (pair.Value.RuntimeOwner == owner)
                {
                    keys.Add(pair.Key);
                }
            }

            return RemoveKeys(keys);
        }

        public string ToDiagnosticString()
        {
            if (bindings.Count == 0)
            {
                return "contentAnchorBindings='0'";
            }

            var builder = new StringBuilder();
            builder.Append("contentAnchorBindings='");
            builder.Append(bindings.Count);
            builder.Append("'");

            foreach (var binding in bindings.Values)
            {
                builder.Append(" [");
                builder.Append(binding.ToDiagnosticString());
                builder.Append(']');
            }

            return builder.ToString();
        }

        private static ContentAnchorBindingResult EvaluateExistingBinding(
            ContentAnchorBindingRequest request,
            ContentAnchorDeclaration anchor,
            RuntimeContentHandle runtimeHandle,
            ContentAnchorContentHandle existingHandle,
            string source,
            string reason)
        {
            if (!existingHandle.IsValid)
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.FailedRuntimeRegistration,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding already exists, but the stored binding handle is no longer valid.");
            }

            if (!request.Matches(existingHandle.Anchor))
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedMismatchedAnchor,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding already exists for this key, but its stored anchor does not match the request.");
            }

            if (existingHandle.RuntimeIdentity != request.RuntimeIdentity)
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.RejectedMismatchedRuntimeContent,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding already exists for this key, but its runtime identity does not match the request.");
            }

            if (!ReferenceEquals(existingHandle.RuntimeHandle, runtimeHandle))
            {
                return ContentAnchorBindingResult.Failure(
                    request,
                    ContentAnchorBindingStatus.FailedRuntimeRegistration,
                    anchor,
                    source,
                    reason,
                    "Content Anchor binding already exists, but its stored runtime handle is not the registered handle for the request identity.");
            }

            return ContentAnchorBindingResult.AlreadyBound(
                request,
                anchor,
                existingHandle,
                source,
                reason,
                "Content Anchor binding already exists and remains valid.");
        }

        private int RemoveKeys(List<string> keys)
        {
            var removed = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                if (bindings.Remove(keys[i]))
                {
                    removed++;
                }
            }

            return removed;
        }

        private static string CreateBindingKey(ContentAnchorBindingRequest request)
        {
            return $"{request.AnchorStableText}|{request.RuntimeIdentity.StableText}";
        }

        private static void ValidateRequest(ContentAnchorBindingRequest request)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Content Anchor binding request must be valid.", nameof(request));
            }
        }

        private static void ValidateIdentity(RuntimeContentIdentity identity)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }
        }

        private static void ValidateOwner(RuntimeContentOwner owner)
        {
            if (!owner.IsValid)
            {
                throw new ArgumentException("Runtime content owner must be valid.", nameof(owner));
            }
        }
    }
}

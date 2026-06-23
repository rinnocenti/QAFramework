using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.RuntimeContent
{
    /// <summary>
    /// API status: Experimental. Passive canonical handle for one runtime-created content identity.
    /// The handle tracks ownership and release state only; it does not instantiate, destroy, pool, register roots or bind anchors.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F8C passive RuntimeContentHandle with release state; no materialization or release execution.")]
    public sealed class RuntimeContentHandle
    {
        private RuntimeContentState state;
        private string source;
        private string reason;
        private string message;

        public RuntimeContentHandle(
            RuntimeContentIdentity identity,
            RuntimeContentState initialState,
            string source,
            string reason,
            string message)
        {
            if (!identity.IsValid)
            {
                throw new ArgumentException("Runtime content identity must be valid.", nameof(identity));
            }

            ValidateInitialState(initialState);

            Identity = identity;
            state = initialState;
            this.source = Normalize(source);
            this.reason = Normalize(reason);
            this.message = Normalize(message);
        }

        public RuntimeContentHandle(RuntimeContentIdentity identity)
            : this(
                identity,
                RuntimeContentState.Declared,
                string.Empty,
                string.Empty,
                "Runtime content handle declared.")
        {
        }

        public RuntimeContentIdentity Identity { get; }

        public RuntimeContentOwner Owner => Identity.Owner;

        public RuntimeContentScope Scope => Identity.Scope;

        public RuntimeContentId ContentId => Identity.ContentId;

        public RuntimeContentState State => state;

        public string Source => source ?? string.Empty;

        public string Reason => reason ?? string.Empty;

        public string Message => message ?? string.Empty;

        public bool IsDeclared => state == RuntimeContentState.Declared;

        public bool IsMaterialized => state == RuntimeContentState.Materialized;

        public bool IsReleaseRequested => state == RuntimeContentState.ReleaseRequested;

        public bool IsReleased => state == RuntimeContentState.Released;

        public bool IsReleaseFailed => state == RuntimeContentState.ReleaseFailed;

        public bool CanRequestRelease => state == RuntimeContentState.Declared
            || state == RuntimeContentState.Materialized
            || state == RuntimeContentState.ReleaseFailed;

        public bool CanMarkMaterialized => state == RuntimeContentState.Declared;

        public bool CanMarkReleased => state == RuntimeContentState.ReleaseRequested;

        public bool CanMarkReleaseFailed => state == RuntimeContentState.ReleaseRequested;

        public RuntimeContentHandleTransitionResult MarkMaterialized(string source, string reason)
        {
            if (state == RuntimeContentState.Materialized)
            {
                return RuntimeContentHandleTransitionResult.IgnoredAlreadyInState(
                    Identity,
                    state,
                    RuntimeContentState.Materialized,
                    source,
                    reason,
                    "Runtime content handle is already materialized.");
            }

            if (!CanMarkMaterialized)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    state,
                    RuntimeContentState.Materialized,
                    source,
                    reason,
                    $"Runtime content handle cannot transition from '{state}' to '{RuntimeContentState.Materialized}'.");
            }

            return ApplyState(
                RuntimeContentState.Materialized,
                source,
                reason,
                "Runtime content handle marked as materialized.");
        }

        public RuntimeContentHandleTransitionResult RequestRelease(string source, string reason)
        {
            if (state == RuntimeContentState.ReleaseRequested)
            {
                return RuntimeContentHandleTransitionResult.IgnoredAlreadyInState(
                    Identity,
                    state,
                    RuntimeContentState.ReleaseRequested,
                    source,
                    reason,
                    "Runtime content release was already requested.");
            }

            if (state == RuntimeContentState.Released)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    state,
                    RuntimeContentState.ReleaseRequested,
                    source,
                    reason,
                    "Runtime content release cannot be requested after the handle is already released.");
            }

            if (!CanRequestRelease)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    state,
                    RuntimeContentState.ReleaseRequested,
                    source,
                    reason,
                    $"Runtime content handle cannot transition from '{state}' to '{RuntimeContentState.ReleaseRequested}'.");
            }

            return ApplyState(
                RuntimeContentState.ReleaseRequested,
                source,
                reason,
                "Runtime content release requested.");
        }

        public RuntimeContentHandleTransitionResult MarkReleased(string source, string reason)
        {
            if (state == RuntimeContentState.Released)
            {
                return RuntimeContentHandleTransitionResult.IgnoredAlreadyInState(
                    Identity,
                    state,
                    RuntimeContentState.Released,
                    source,
                    reason,
                    "Runtime content is already released.");
            }

            if (!CanMarkReleased)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    state,
                    RuntimeContentState.Released,
                    source,
                    reason,
                    $"Runtime content handle cannot transition from '{state}' to '{RuntimeContentState.Released}' without a release request.");
            }

            return ApplyState(
                RuntimeContentState.Released,
                source,
                reason,
                "Runtime content marked as released.");
        }

        public RuntimeContentHandleTransitionResult MarkReleaseFailed(string source, string reason, string failureMessage)
        {
            var normalizedFailureMessage = Normalize(failureMessage);
            if (!CanMarkReleaseFailed)
            {
                return RuntimeContentHandleTransitionResult.RejectedInvalidTransition(
                    Identity,
                    state,
                    RuntimeContentState.ReleaseFailed,
                    source,
                    reason,
                    !string.IsNullOrWhiteSpace(normalizedFailureMessage)
                        ? normalizedFailureMessage
                        : $"Runtime content handle cannot transition from '{state}' to '{RuntimeContentState.ReleaseFailed}' without a release request.");
            }

            return ApplyState(
                RuntimeContentState.ReleaseFailed,
                source,
                reason,
                !string.IsNullOrWhiteSpace(normalizedFailureMessage)
                    ? normalizedFailureMessage
                    : "Runtime content release failed.");
        }

        public string ToDiagnosticString()
        {
            var sourceText = !string.IsNullOrWhiteSpace(Source) ? Source : "<none>";
            var reasonText = !string.IsNullOrWhiteSpace(Reason) ? Reason : "<none>";
            var messageText = !string.IsNullOrWhiteSpace(Message) ? Message : "<none>";
            return $"identity='{Identity.StableText}' scope='{Scope}' contentId='{ContentId.StableText}' owner='{Owner.OwnerName}' state='{state}' source='{sourceText}' reason='{reasonText}' message='{messageText}'";
        }

        public static RuntimeContentHandle Declared(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            return new RuntimeContentHandle(
                identity,
                RuntimeContentState.Declared,
                source,
                reason,
                "Runtime content handle declared.");
        }

        public static RuntimeContentHandle Materialized(
            RuntimeContentIdentity identity,
            string source,
            string reason)
        {
            return new RuntimeContentHandle(
                identity,
                RuntimeContentState.Materialized,
                source,
                reason,
                "Runtime content handle materialized.");
        }

        private RuntimeContentHandleTransitionResult ApplyState(
            RuntimeContentState newState,
            string source,
            string reason,
            string message)
        {
            var previousState = state;
            state = newState;
            this.source = Normalize(source);
            this.reason = Normalize(reason);
            this.message = Normalize(message);

            return RuntimeContentHandleTransitionResult.AppliedResult(
                Identity,
                previousState,
                state,
                this.source,
                this.reason,
                this.message);
        }

        private static void ValidateInitialState(RuntimeContentState initialState)
        {
            if (!Enum.IsDefined(typeof(RuntimeContentState), initialState) || initialState == RuntimeContentState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(initialState), initialState, "Runtime content handle initial state must be explicit.");
            }

            if (initialState == RuntimeContentState.ReleaseRequested
                || initialState == RuntimeContentState.Released
                || initialState == RuntimeContentState.ReleaseFailed)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialState),
                    initialState,
                    "Runtime content handle cannot start in a release terminal or release-requested state.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

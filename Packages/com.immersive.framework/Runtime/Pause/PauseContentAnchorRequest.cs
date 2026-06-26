using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ContentAnchor;
using Immersive.Framework.Identity;
using Immersive.Framework.RuntimeContent;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive Pause consumer request for an existing Content Anchor.
    /// The request bridges Pause intent to the canonical Content Anchor binding contract without finding objects,
    /// creating anchors, materializing UI, binding input, changing Time.timeScale or mutating Pause state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23B Pause Content Anchor consumer request; produces contract data only.")]
    public readonly struct PauseContentAnchorRequest : IEquatable<PauseContentAnchorRequest>
    {
        public PauseContentAnchorRequest(
            PauseContentAnchorRequestId requestId,
            PauseContentAnchorPurpose purpose,
            PauseState pauseState,
            RuntimeScopeContext runtimeContext,
            ContentAnchorScope anchorScope,
            FrameworkIdentityKey anchorOwner,
            ContentAnchorKind anchorKind,
            ContentAnchorId anchorId,
            ContentAnchorRequiredness requiredness,
            RuntimeContentId runtimeContentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            if (!requestId.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor request requires a valid request id.", nameof(requestId));
            }

            if (!Enum.IsDefined(typeof(PauseContentAnchorPurpose), purpose) || purpose == PauseContentAnchorPurpose.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Pause Content Anchor purpose must be explicit.");
            }

            if (!Enum.IsDefined(typeof(PauseState), pauseState) || pauseState == PauseState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(pauseState), pauseState, "Pause Content Anchor request must carry an explicit Pause state.");
            }

            if (!runtimeContext.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor request runtime context must be valid.", nameof(runtimeContext));
            }

            if (anchorScope == ContentAnchorScope.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(anchorScope), anchorScope, "Pause Content Anchor request scope must be explicit.");
            }

            if (!anchorOwner.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor request owner must be explicit and valid.", nameof(anchorOwner));
            }

            if (anchorKind == ContentAnchorKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(anchorKind), anchorKind, "Pause Content Anchor request kind must be explicit.");
            }

            if (!anchorId.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor request id must be explicit and valid.", nameof(anchorId));
            }

            if (!Enum.IsDefined(typeof(ContentAnchorRequiredness), requiredness))
            {
                throw new ArgumentOutOfRangeException(nameof(requiredness), requiredness, "Pause Content Anchor request requiredness must be defined.");
            }

            if (!runtimeContentId.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor request runtime content id must be valid.", nameof(runtimeContentId));
            }

            if (!resource.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor request resource must be valid.", nameof(resource));
            }

            RequestId = requestId;
            Purpose = purpose;
            PauseState = pauseState;
            RuntimeContext = runtimeContext;
            AnchorScope = anchorScope;
            AnchorOwner = anchorOwner;
            AnchorKind = anchorKind;
            AnchorId = anchorId;
            Requiredness = requiredness;
            RuntimeContentId = runtimeContentId;
            Resource = resource;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public PauseContentAnchorRequestId RequestId { get; }

        public PauseContentAnchorPurpose Purpose { get; }

        public PauseState PauseState { get; }

        public RuntimeScopeContext RuntimeContext { get; }

        public RuntimeContentOwner RuntimeOwner => RuntimeContext.Owner;

        public RuntimeContentScope RuntimeScope => RuntimeContext.Scope;

        public ContentAnchorScope AnchorScope { get; }

        public FrameworkIdentityKey AnchorOwner { get; }

        public ContentAnchorKind AnchorKind { get; }

        public ContentAnchorId AnchorId { get; }

        public ContentAnchorRequiredness Requiredness { get; }

        public RuntimeContentId RuntimeContentId { get; }

        public RuntimeMaterializationResource Resource { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsRequired => Requiredness == ContentAnchorRequiredness.Required;

        public bool IsOptional => Requiredness == ContentAnchorRequiredness.Optional;

        public bool IsPaused => PauseState == PauseState.Paused;

        public bool IsRunning => PauseState == PauseState.Running;

        public bool IsValid => RequestId.IsValid
            && Purpose != PauseContentAnchorPurpose.Unknown
            && PauseState != PauseState.Unknown
            && RuntimeContext.IsValid
            && AnchorScope != ContentAnchorScope.Unknown
            && AnchorOwner.IsValid
            && AnchorKind != ContentAnchorKind.Unknown
            && AnchorId.IsValid
            && RuntimeContentId.IsValid
            && Resource.IsValid;

        public string AnchorStableText => $"ContentAnchor:{AnchorScope}:{AnchorKind}:{AnchorOwner.StableText}:{AnchorId.StableText}";

        public bool Matches(ContentAnchorDeclaration declaration)
        {
            return declaration.IsValid
                && declaration.Scope == AnchorScope
                && declaration.Kind == AnchorKind
                && declaration.Owner == AnchorOwner
                && declaration.AnchorId == AnchorId;
        }

        public ContentAnchorBindingRequest ToBindingRequest()
        {
            return new ContentAnchorBindingRequest(
                RuntimeContext,
                AnchorScope,
                AnchorOwner,
                AnchorKind,
                AnchorId,
                RuntimeContentId,
                Resource,
                Source,
                Reason);
        }

        public bool Equals(PauseContentAnchorRequest other)
        {
            return RequestId.Equals(other.RequestId)
                && Purpose == other.Purpose
                && PauseState == other.PauseState
                && RuntimeContext.Equals(other.RuntimeContext)
                && AnchorScope == other.AnchorScope
                && AnchorOwner.Equals(other.AnchorOwner)
                && AnchorKind == other.AnchorKind
                && AnchorId.Equals(other.AnchorId)
                && Requiredness == other.Requiredness
                && RuntimeContentId.Equals(other.RuntimeContentId)
                && Resource.Equals(other.Resource)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseContentAnchorRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = RequestId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Purpose;
                hashCode = (hashCode * 397) ^ (int)PauseState;
                hashCode = (hashCode * 397) ^ RuntimeContext.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)AnchorScope;
                hashCode = (hashCode * 397) ^ AnchorOwner.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)AnchorKind;
                hashCode = (hashCode * 397) ^ AnchorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Requiredness;
                hashCode = (hashCode * 397) ^ RuntimeContentId.GetHashCode();
                hashCode = (hashCode * 397) ^ Resource.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Reason ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var sourceText = string.IsNullOrWhiteSpace(Source) ? "<none>" : Source;
            var reasonText = string.IsNullOrWhiteSpace(Reason) ? "<none>" : Reason;
            return $"request='{RequestId.StableText}' purpose='{Purpose}' pauseState='{PauseState}' anchor='{AnchorStableText}' requiredness='{Requiredness}' runtimeOwner='{RuntimeOwner.StableText}' runtimeScope='{RuntimeScope}' runtimeContent='{RuntimeContentId.StableText}' {Resource.ToDiagnosticString()} source='{sourceText}' reason='{reasonText}'";
        }

        public static PauseContentAnchorRequest ForAnchor(
            string requestId,
            PauseContentAnchorPurpose purpose,
            PauseState pauseState,
            RuntimeScopeContext runtimeContext,
            ContentAnchorScope anchorScope,
            string anchorOwnerId,
            ContentAnchorKind anchorKind,
            string anchorId,
            ContentAnchorRequiredness requiredness,
            string runtimeContentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            return new PauseContentAnchorRequest(
                PauseContentAnchorRequestId.From(requestId),
                purpose,
                pauseState,
                runtimeContext,
                anchorScope,
                ContentAnchorDeclaration.CreateOwnerKey(anchorScope, anchorOwnerId),
                anchorKind,
                ContentAnchorId.From(anchorId),
                requiredness,
                RuntimeContentId.From(runtimeContentId),
                resource,
                source,
                reason);
        }

        public static PauseContentAnchorRequest FromDeclaration(
            string requestId,
            PauseContentAnchorPurpose purpose,
            PauseState pauseState,
            RuntimeScopeContext runtimeContext,
            ContentAnchorDeclaration declaration,
            ContentAnchorRequiredness requiredness,
            string runtimeContentId,
            RuntimeMaterializationResource resource,
            string source,
            string reason)
        {
            if (!declaration.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor request requires a valid declaration.", nameof(declaration));
            }

            return new PauseContentAnchorRequest(
                PauseContentAnchorRequestId.From(requestId),
                purpose,
                pauseState,
                runtimeContext,
                declaration.Scope,
                declaration.Owner,
                declaration.Kind,
                declaration.AnchorId,
                requiredness,
                RuntimeContentId.From(runtimeContentId),
                resource,
                source,
                reason);
        }

        public static bool operator ==(PauseContentAnchorRequest left, PauseContentAnchorRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseContentAnchorRequest left, PauseContentAnchorRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

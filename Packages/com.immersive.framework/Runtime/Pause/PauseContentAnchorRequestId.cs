using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Stable identity for one Pause Content Anchor consumer request.
    /// This is not a UI object name, prefab name, input action name or Content Anchor id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23B Pause Content Anchor consumer request identity; passive contract only.")]
    public readonly struct PauseContentAnchorRequestId : IFrameworkIdentity, IEquatable<PauseContentAnchorRequestId>
    {
        private readonly FrameworkIdentityValue value;

        public PauseContentAnchorRequestId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public PauseContentAnchorRequestId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Pause Content Anchor request id must be valid.", nameof(value));
            }

            this.value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Pause;

        public FrameworkIdentityValue Value => value;

        public bool IsValid => value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, value);

        public string StableText => Key.StableText;

        public bool Equals(PauseContentAnchorRequestId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseContentAnchorRequestId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static PauseContentAnchorRequestId From(string value)
        {
            return new PauseContentAnchorRequestId(value);
        }

        public static bool operator ==(PauseContentAnchorRequestId left, PauseContentAnchorRequestId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseContentAnchorRequestId left, PauseContentAnchorRequestId right)
        {
            return !left.Equals(right);
        }
    }
}

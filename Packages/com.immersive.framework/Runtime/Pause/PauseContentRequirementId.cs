using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Stable identity for one logical Pause content requirement.
    /// This is not a Content Anchor binding handle, GameObject name, prefab id or UI object id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23E Pause content requirement identity; intent-only contract.")]
    public readonly struct PauseContentRequirementId : IFrameworkIdentity, IEquatable<PauseContentRequirementId>
    {
        private readonly FrameworkIdentityValue value;

        public PauseContentRequirementId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public PauseContentRequirementId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Pause content requirement id must be valid.", nameof(value));
            }

            this.value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Pause;

        public FrameworkIdentityValue Value => value;

        public bool IsValid => value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, value);

        public string StableText => Key.StableText;

        public bool Equals(PauseContentRequirementId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseContentRequirementId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static PauseContentRequirementId From(string value)
        {
            return new PauseContentRequirementId(value);
        }

        public static bool operator ==(PauseContentRequirementId left, PauseContentRequirementId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseContentRequirementId left, PauseContentRequirementId right)
        {
            return !left.Equals(right);
        }
    }
}

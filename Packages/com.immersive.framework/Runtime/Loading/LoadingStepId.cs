using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for one step inside a Loading operation.
    /// This is not a scene path, GameObject name, Transition step id or UI label.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22B Loading step identity primitive; no execution owner.")]
    public readonly struct LoadingStepId : IFrameworkIdentity, IEquatable<LoadingStepId>
    {
        private readonly FrameworkIdentityValue value;

        public LoadingStepId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public LoadingStepId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Loading step id must be valid.", nameof(value));
            }

            this.value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Loading;

        public FrameworkIdentityValue Value => value;

        public bool IsValid => value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, value);

        public string StableText => Key.StableText;

        public bool Equals(LoadingStepId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingStepId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static LoadingStepId From(string value)
        {
            return new LoadingStepId(value);
        }

        public static bool operator ==(LoadingStepId left, LoadingStepId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingStepId left, LoadingStepId right)
        {
            return !left.Equals(right);
        }
    }
}

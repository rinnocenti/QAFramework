using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Stable logical identity for one Loading readiness observation.
    /// This is not an Activity id, Transition id, scene path, UI label or gameplay object id.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F22G Loading readiness observation identity primitive; observation only.")]
    public readonly struct LoadingReadinessObservationId : IFrameworkIdentity, IEquatable<LoadingReadinessObservationId>
    {
        private readonly FrameworkIdentityValue value;

        public LoadingReadinessObservationId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public LoadingReadinessObservationId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Loading readiness observation id must be valid.", nameof(value));
            }

            this.value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Loading;

        public FrameworkIdentityValue Value => value;

        public bool IsValid => value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, value);

        public string StableText => Key.StableText;

        public bool Equals(LoadingReadinessObservationId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingReadinessObservationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static LoadingReadinessObservationId From(string value)
        {
            return new LoadingReadinessObservationId(value);
        }

        public static bool operator ==(LoadingReadinessObservationId left, LoadingReadinessObservationId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingReadinessObservationId left, LoadingReadinessObservationId right)
        {
            return !left.Equals(right);
        }
    }
}

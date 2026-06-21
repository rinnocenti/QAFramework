using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Experimental. Explicit id for local content inside its owning local identity scope.
    /// This value must be authored or provided explicitly; it must not be inferred from object names or paths.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Explicit local content id introduced by F5B.")]
    public readonly struct LocalContentId : IFrameworkIdentity, IEquatable<LocalContentId>
    {
        private readonly FrameworkIdentityValue value;

        public LocalContentId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public LocalContentId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Local content id must be explicit and valid.", nameof(value));
            }

            this.value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Local;

        public FrameworkIdentityValue Value => value;

        public bool IsValid => value.IsValid;

        public string StableText => value.Value;

        public bool Equals(LocalContentId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is LocalContentId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static LocalContentId From(string value)
        {
            return new LocalContentId(value);
        }

        public static bool operator ==(LocalContentId left, LocalContentId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LocalContentId left, LocalContentId right)
        {
            return !left.Equals(right);
        }
    }
}

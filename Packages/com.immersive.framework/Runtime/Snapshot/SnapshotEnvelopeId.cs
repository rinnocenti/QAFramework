using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Stable identity for one Snapshot envelope.
    /// This is not a save slot id, file name, PlayerPrefs key, JSON id or backend key.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21B Snapshot envelope identity primitive; backend-agnostic.")]
    public readonly struct SnapshotEnvelopeId : IFrameworkIdentity, IEquatable<SnapshotEnvelopeId>
    {
        private readonly FrameworkIdentityValue value;

        public SnapshotEnvelopeId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public SnapshotEnvelopeId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Snapshot envelope id must be valid.", nameof(value));
            }

            this.value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Snapshot;

        public FrameworkIdentityValue Value => value;

        public bool IsValid => value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, value);

        public string StableText => Key.StableText;

        public bool Equals(SnapshotEnvelopeId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotEnvelopeId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static SnapshotEnvelopeId From(string value)
        {
            return new SnapshotEnvelopeId(value);
        }

        public static bool operator ==(SnapshotEnvelopeId left, SnapshotEnvelopeId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotEnvelopeId left, SnapshotEnvelopeId right)
        {
            return !left.Equals(right);
        }
    }
}

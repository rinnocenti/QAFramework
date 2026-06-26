using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.Snapshot
{
    /// <summary>
    /// API status: Experimental. Stable identity for one Snapshot participant.
    /// This is not a scene object name, component type name, save slot id, backend key or PlayerPrefs key.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21C Snapshot participant identity primitive; backend-agnostic.")]
    public readonly struct SnapshotParticipantId : IFrameworkIdentity, IEquatable<SnapshotParticipantId>
    {
        private readonly FrameworkIdentityValue value;

        public SnapshotParticipantId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public SnapshotParticipantId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Snapshot participant id must be valid.", nameof(value));
            }

            this.value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.Snapshot;

        public FrameworkIdentityValue Value => value;

        public bool IsValid => value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, value);

        public string StableText => Key.StableText;

        public bool Equals(SnapshotParticipantId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is SnapshotParticipantId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static SnapshotParticipantId From(string value)
        {
            return new SnapshotParticipantId(value);
        }

        public static bool operator ==(SnapshotParticipantId left, SnapshotParticipantId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SnapshotParticipantId left, SnapshotParticipantId right)
        {
            return !left.Equals(right);
        }
    }
}

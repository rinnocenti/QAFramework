using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;

namespace Immersive.Framework.ProgressionSave
{
    /// <summary>
    /// API status: Experimental. Stable identity for one stored Progression Save record.
    /// This distinguishes the stored record from the logical slot that points to it.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F21E Progression Save record identity primitive; backend-agnostic.")]
    public readonly struct ProgressionSaveRecordId : IFrameworkIdentity, IEquatable<ProgressionSaveRecordId>
    {
        private readonly FrameworkIdentityValue value;

        public ProgressionSaveRecordId(string value)
            : this(new FrameworkIdentityValue(value))
        {
        }

        public ProgressionSaveRecordId(FrameworkIdentityValue value)
        {
            if (!value.IsValid)
            {
                throw new ArgumentException("Progression Save record id must be valid.", nameof(value));
            }

            this.value = value;
        }

        public FrameworkIdentityDomain Domain => FrameworkIdentityDomain.ProgressionSave;

        public FrameworkIdentityValue Value => value;

        public bool IsValid => value.IsValid;

        public FrameworkIdentityKey Key => new FrameworkIdentityKey(Domain, value);

        public string StableText => Key.StableText;

        public bool Equals(ProgressionSaveRecordId other)
        {
            return value.Equals(other.value);
        }

        public override bool Equals(object obj)
        {
            return obj is ProgressionSaveRecordId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override string ToString()
        {
            return StableText;
        }

        public static ProgressionSaveRecordId From(string value)
        {
            return new ProgressionSaveRecordId(value);
        }

        public static bool operator ==(ProgressionSaveRecordId left, ProgressionSaveRecordId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProgressionSaveRecordId left, ProgressionSaveRecordId right)
        {
            return !left.Equals(right);
        }
    }
}

using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive diagnostic issue produced while building an Activity Content Execution participant collection.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10E Activity Content Execution participant collection issue; no discovery runtime or Unity side effects.")]
    public readonly struct ActivityContentExecutionParticipantCollectionIssue : IEquatable<ActivityContentExecutionParticipantCollectionIssue>
    {
        public ActivityContentExecutionParticipantCollectionIssue(
            ActivityContentExecutionParticipantCollectionIssueKind kind,
            int index,
            string key,
            ActivityContentExecutionParticipantDescriptor descriptor,
            string message)
        {
            if (!Enum.IsDefined(typeof(ActivityContentExecutionParticipantCollectionIssueKind), kind)
                || kind == ActivityContentExecutionParticipantCollectionIssueKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Activity content execution participant collection issue kind must be explicit.");
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Activity content execution participant collection issue index cannot be negative.");
            }

            Kind = kind;
            Index = index;
            Key = Normalize(key);
            Descriptor = descriptor;
            Message = Normalize(message);
        }

        public ActivityContentExecutionParticipantCollectionIssueKind Kind { get; }

        public int Index { get; }

        public string Key { get; }

        public ActivityContentExecutionParticipantDescriptor Descriptor { get; }

        public string Message { get; }

        public bool HasDescriptor => Descriptor.IsValid;

        public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

        public bool Equals(ActivityContentExecutionParticipantCollectionIssue other)
        {
            return Kind == other.Kind
                && Index == other.Index
                && string.Equals(Key, other.Key, StringComparison.Ordinal)
                && Descriptor.Equals(other.Descriptor)
                && string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionParticipantCollectionIssue other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ Index;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Key ?? string.Empty);
                hashCode = (hashCode * 397) ^ Descriptor.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var keyText = !string.IsNullOrWhiteSpace(Key) ? Key : "<none>";
            var messageText = !string.IsNullOrWhiteSpace(Message) ? Message : "<none>";
            var descriptorText = Descriptor.IsValid ? Descriptor.ToDiagnosticString() : "<invalid>";
            return $"kind='{Kind}' index='{Index}' key='{keyText}' descriptor='{descriptorText}' message='{messageText}'";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

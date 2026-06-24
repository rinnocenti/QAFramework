using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// API status: Experimental. Passive request used by ActivityFlow to ask an explicit source for Activity Content Execution participants.
    /// It carries lifecycle context only; it does not discover scene objects, execute participants or mutate gameplay state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F10K Activity Content Execution participant source request; lifecycle context only.")]
    public readonly struct ActivityContentExecutionParticipantSourceRequest : IEquatable<ActivityContentExecutionParticipantSourceRequest>
    {
        public ActivityContentExecutionParticipantSourceRequest(
            RouteAsset route,
            ActivityAsset previousActivity,
            ActivityAsset nextActivity,
            string source,
            string reason)
        {
            Route = route;
            PreviousActivity = previousActivity;
            NextActivity = nextActivity;
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public RouteAsset Route { get; }

        public ActivityAsset PreviousActivity { get; }

        public ActivityAsset NextActivity { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool HasRoute => Route != null;

        public bool HasPreviousActivity => PreviousActivity != null;

        public bool HasNextActivity => NextActivity != null;

        public bool HasActivityTransition => HasPreviousActivity || HasNextActivity;

        public bool IsClear => HasPreviousActivity && !HasNextActivity;

        public bool IsEnter => !HasPreviousActivity && HasNextActivity;

        public bool IsSwitch => HasPreviousActivity && HasNextActivity && !ReferenceEquals(PreviousActivity, NextActivity);

        public bool IsSameActivity => HasPreviousActivity && HasNextActivity && ReferenceEquals(PreviousActivity, NextActivity);

        public bool IsValid => HasActivityTransition;

        public string RouteName => Route != null ? Route.RouteName : string.Empty;

        public string PreviousActivityName => PreviousActivity != null ? PreviousActivity.ActivityName : string.Empty;

        public string NextActivityName => NextActivity != null ? NextActivity.ActivityName : string.Empty;

        public bool Equals(ActivityContentExecutionParticipantSourceRequest other)
        {
            return ReferenceEquals(Route, other.Route)
                && ReferenceEquals(PreviousActivity, other.PreviousActivity)
                && ReferenceEquals(NextActivity, other.NextActivity)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ActivityContentExecutionParticipantSourceRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Route != null ? Route.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (PreviousActivity != null ? PreviousActivity.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (NextActivity != null ? NextActivity.GetHashCode() : 0);
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
            var routeText = !string.IsNullOrWhiteSpace(RouteName) ? RouteName : "<none>";
            var previousText = !string.IsNullOrWhiteSpace(PreviousActivityName) ? PreviousActivityName : "<none>";
            var nextText = !string.IsNullOrWhiteSpace(NextActivityName) ? NextActivityName : "<none>";
            var sourceText = !string.IsNullOrWhiteSpace(Source) ? Source : "<none>";
            var reasonText = !string.IsNullOrWhiteSpace(Reason) ? Reason : "<none>";
            return $"route='{routeText}' previousActivity='{previousText}' nextActivity='{nextText}' valid='{IsValid}' source='{sourceText}' reason='{reasonText}'";
        }

        public static bool operator ==(ActivityContentExecutionParticipantSourceRequest left, ActivityContentExecutionParticipantSourceRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActivityContentExecutionParticipantSourceRequest left, ActivityContentExecutionParticipantSourceRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

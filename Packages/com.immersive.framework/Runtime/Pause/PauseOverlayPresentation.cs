using System;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Visual-facing presentation data for Pause overlay adapters.
    /// It wraps a canonical PauseSnapshot and optional prepared Pause Content Anchor consumer result. It does not own UI,
    /// Canvas, prefab, input binding, Time.timeScale, Transition Effects or lifecycle execution.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F23C Pause Overlay presentation boundary; visual adapter data only.")]
    public readonly struct PauseOverlayPresentation : IEquatable<PauseOverlayPresentation>
    {
        public PauseOverlayPresentation(
            PauseSnapshot snapshot,
            bool shouldBeVisible,
            PauseContentAnchorConsumerResult contentAnchorResult,
            string title,
            string detail,
            string source)
        {
            if (!snapshot.IsValid)
            {
                throw new ArgumentException("Pause overlay presentation requires a valid Pause snapshot.", nameof(snapshot));
            }

            Snapshot = snapshot;
            ShouldBeVisible = shouldBeVisible;
            ContentAnchorResult = contentAnchorResult;
            Title = Normalize(title);
            Detail = Normalize(detail);
            Source = Normalize(source);
        }

        public PauseSnapshot Snapshot { get; }

        public PauseState State => Snapshot.State;

        public PauseRequestId LastRequestId => Snapshot.LastRequestId;

        public bool HasLastRequest => Snapshot.HasLastRequest;

        public bool ShouldBeVisible { get; }

        public PauseContentAnchorConsumerResult ContentAnchorResult { get; }

        public string Title { get; }

        public string Detail { get; }

        public string Source { get; }

        public bool IsValid => Snapshot.IsValid;

        public bool IsPaused => Snapshot.IsPaused;

        public bool IsRunning => Snapshot.IsRunning;

        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasContentAnchorResult => ContentAnchorResult.IsValid;

        public bool HasPreparedContentAnchor => HasContentAnchorResult && ContentAnchorResult.Prepared;

        public bool HasBlockingContentAnchorIssues => HasContentAnchorResult && ContentAnchorResult.BlocksPauseContent;

        public bool Equals(PauseOverlayPresentation other)
        {
            return Snapshot.Equals(other.Snapshot)
                && ShouldBeVisible == other.ShouldBeVisible
                && ContentAnchorResult.Equals(other.ContentAnchorResult)
                && string.Equals(Title, other.Title, StringComparison.Ordinal)
                && string.Equals(Detail, other.Detail, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is PauseOverlayPresentation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Snapshot.GetHashCode();
                hashCode = (hashCode * 397) ^ ShouldBeVisible.GetHashCode();
                hashCode = (hashCode * 397) ^ ContentAnchorResult.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Title ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Detail ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Source ?? string.Empty);
                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var requestText = HasLastRequest ? LastRequestId.StableText : "<none>";
            var titleText = HasTitle ? Title : "<none>";
            var detailText = HasDetail ? Detail : "<none>";
            var sourceText = HasSource ? Source : "<none>";
            var anchorStatus = HasContentAnchorResult ? ContentAnchorResult.Status.ToString() : "<none>";
            return $"state='{State}' visible='{ShouldBeVisible}' paused='{IsPaused}' running='{IsRunning}' lastRequest='{requestText}' title='{titleText}' detail='{detailText}' source='{sourceText}' hasContentAnchorResult='{HasContentAnchorResult}' contentAnchorPrepared='{HasPreparedContentAnchor}' contentAnchorBlocks='{HasBlockingContentAnchorIssues}' contentAnchorStatus='{anchorStatus}'";
        }

        public static PauseOverlayPresentation FromSnapshot(
            PauseSnapshot snapshot,
            bool shouldBeVisible,
            string title,
            string detail,
            string source)
        {
            return new PauseOverlayPresentation(snapshot, shouldBeVisible, default, title, detail, source);
        }

        public static PauseOverlayPresentation FromSnapshot(
            PauseSnapshot snapshot,
            bool shouldBeVisible,
            PauseContentAnchorConsumerResult contentAnchorResult,
            string title,
            string detail,
            string source)
        {
            return new PauseOverlayPresentation(snapshot, shouldBeVisible, contentAnchorResult, title, detail, source);
        }

        public static bool operator ==(PauseOverlayPresentation left, PauseOverlayPresentation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PauseOverlayPresentation left, PauseOverlayPresentation right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}

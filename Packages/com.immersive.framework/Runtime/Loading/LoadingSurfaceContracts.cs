using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.Loading
{
    /// <summary>
    /// API status: Experimental. Policy for the Unity loading surface wired through a Game Application.
    /// NoneConfigured keeps loading explicit NoOp. Optional uses the prefab when present and skips explicitly when absent.
    /// Required instantiates the prefab and fails explicitly if the surface is missing or invalid.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface policy for GameApplication authoring.")]
    public enum LoadingSurfacePolicy
    {
        NoneConfigured = 0,
        Optional = 1,
        Required = 2
    }

    /// <summary>
    /// API status: Experimental. Adapter-facing action for a loading surface request.
    /// This is a visual adapter action only; it does not execute SceneLifecycle, RouteLifecycle or ActivityFlow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface adapter action boundary; no lifecycle ownership.")]
    public enum LoadingSurfaceAction
    {
        Unknown = 0,
        Show = 10,
        Update = 20,
        Hide = 30
    }

    /// <summary>
    /// API status: Experimental. Result status for a loading surface adapter action.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface adapter status boundary; no lifecycle ownership.")]
    public enum LoadingSurfaceResultStatus
    {
        Unknown = 0,
        Succeeded = 10,
        SucceededWithWarnings = 20,
        Skipped = 30,
        Failed = 40,
        Rejected = 50
    }

    /// <summary>
    /// API status: Experimental. Visual-facing request data for a loading surface adapter.
    /// It carries display intent and diagnostics only; it does not own scene loading.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface request boundary; visual adapter data only.")]
    public readonly struct LoadingSurfaceRequest : IEquatable<LoadingSurfaceRequest>
    {
        public LoadingSurfaceRequest(
            LoadingSurfaceAction action,
            bool shouldBeVisible,
            LoadingProgress progress,
            bool progressSupported,
            string title,
            string detail,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(LoadingSurfaceAction), action) || action == LoadingSurfaceAction.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(action), action, "Loading surface request action must be explicit.");
            }

            Action = action;
            ShouldBeVisible = shouldBeVisible;
            Progress = progress;
            ProgressSupported = progressSupported;
            Title = Normalize(title);
            Detail = Normalize(detail);
            Source = Normalize(source);
            Reason = Normalize(reason);
        }

        public LoadingSurfaceAction Action { get; }

        public bool ShouldBeVisible { get; }

        public LoadingProgress Progress { get; }

        public bool ProgressSupported { get; }

        public string Title { get; }

        public string Detail { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool IsValid => Action != LoadingSurfaceAction.Unknown;

        public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

        public bool HasDetail => !string.IsNullOrWhiteSpace(Detail);

        public bool HasSource => !string.IsNullOrWhiteSpace(Source);

        public bool HasReason => !string.IsNullOrWhiteSpace(Reason);

        public bool Equals(LoadingSurfaceRequest other)
        {
            return Action == other.Action
                && ShouldBeVisible == other.ShouldBeVisible
                && Progress.Equals(other.Progress)
                && ProgressSupported == other.ProgressSupported
                && string.Equals(Title, other.Title, StringComparison.Ordinal)
                && string.Equals(Detail, other.Detail, StringComparison.Ordinal)
                && string.Equals(Source, other.Source, StringComparison.Ordinal)
                && string.Equals(Reason, other.Reason, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingSurfaceRequest other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Action;
                hashCode = (hashCode * 397) ^ ShouldBeVisible.GetHashCode();
                hashCode = (hashCode * 397) ^ Progress.GetHashCode();
                hashCode = (hashCode * 397) ^ ProgressSupported.GetHashCode();
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Title ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Detail ?? string.Empty);
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
            var titleText = HasTitle ? Title : "<none>";
            var detailText = HasDetail ? Detail : "<none>";
            var sourceText = HasSource ? Source : "<none>";
            var reasonText = HasReason ? Reason : "<none>";
            return $"action='{Action}' visible='{ShouldBeVisible}' progressSupported='{ProgressSupported}' progress='{Progress.NormalizedValue:0.###}' percent='{Progress.PercentRounded}' title='{titleText}' detail='{detailText}' source='{sourceText}' reason='{reasonText}'";
        }

        public static LoadingSurfaceRequest Show(
            string title,
            string detail,
            string source,
            string reason)
        {
            return new LoadingSurfaceRequest(
                LoadingSurfaceAction.Show,
                true,
                LoadingProgress.Zero,
                false,
                title,
                detail,
                source,
                reason);
        }

        public static LoadingSurfaceRequest Update(
            string title,
            string detail,
            string source,
            string reason)
        {
            return new LoadingSurfaceRequest(
                LoadingSurfaceAction.Update,
                true,
                LoadingProgress.Zero,
                false,
                title,
                detail,
                source,
                reason);
        }

        public static LoadingSurfaceRequest Hide(
            string title,
            string detail,
            string source,
            string reason)
        {
            return new LoadingSurfaceRequest(
                LoadingSurfaceAction.Hide,
                false,
                LoadingProgress.Zero,
                false,
                title,
                detail,
                source,
                reason);
        }

        public static bool operator ==(LoadingSurfaceRequest left, LoadingSurfaceRequest right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingSurfaceRequest left, LoadingSurfaceRequest right)
        {
            return !left.Equals(right);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// API status: Experimental. Explicit result for one loading surface adapter action.
    /// It reports visual adapter outcome only; it does not block or own SceneLifecycle, RouteLifecycle or ActivityFlow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface result boundary; no lifecycle ownership.")]
    public readonly struct LoadingSurfaceResult : IEquatable<LoadingSurfaceResult>
    {
        private readonly string[] _issues;

        public LoadingSurfaceResult(
            LoadingSurfaceRequest request,
            LoadingSurfaceResultStatus status,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            if (!request.IsValid)
            {
                throw new ArgumentException("Loading surface result requires a valid request.", nameof(request));
            }

            if (!Enum.IsDefined(typeof(LoadingSurfaceResultStatus), status) || status == LoadingSurfaceResultStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Loading surface result status must be explicit.");
            }

            Request = request;
            Status = status;
            AdapterName = Normalize(adapterName);
            Message = Normalize(message);
            _issues = CopyIssues(issues);
        }

        public LoadingSurfaceRequest Request { get; }

        public LoadingSurfaceAction Action => Request.Action;

        public bool ShouldBeVisible => Request.ShouldBeVisible;

        public LoadingProgress Progress => Request.Progress;

        public bool ProgressSupported => Request.ProgressSupported;

        public string Title => Request.Title;

        public string Detail => Request.Detail;

        public string Source => Request.Source;

        public string Reason => Request.Reason;

        public LoadingSurfaceResultStatus Status { get; }

        public string AdapterName { get; }

        public string Message { get; }

        public IReadOnlyList<string> Issues => _issues ?? Array.Empty<string>();

        public int IssueCount => Issues.Count;

        public int BlockingIssueCount => Status == LoadingSurfaceResultStatus.Failed || Status == LoadingSurfaceResultStatus.Rejected
            ? Math.Max(1, IssueCount)
            : 0;

        public bool HasIssues => IssueCount > 0;

        public bool Succeeded => Status == LoadingSurfaceResultStatus.Succeeded;

        public bool SucceededWithWarnings => Status == LoadingSurfaceResultStatus.SucceededWithWarnings;

        public bool Skipped => Status == LoadingSurfaceResultStatus.Skipped;

        public bool Failed => Status == LoadingSurfaceResultStatus.Failed;

        public bool Rejected => Status == LoadingSurfaceResultStatus.Rejected;

        public bool Completed => Succeeded || SucceededWithWarnings || Skipped;

        public bool IsValid => Request.IsValid && Status != LoadingSurfaceResultStatus.Unknown;

        public bool Equals(LoadingSurfaceResult other)
        {
            return Request.Equals(other.Request)
                && Status == other.Status
                && string.Equals(AdapterName, other.AdapterName, StringComparison.Ordinal)
                && string.Equals(Message, other.Message, StringComparison.Ordinal)
                && SequenceEquals(Issues, other.Issues);
        }

        public override bool Equals(object obj)
        {
            return obj is LoadingSurfaceResult other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Request.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Status;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(AdapterName ?? string.Empty);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Message ?? string.Empty);
                for (var i = 0; i < Issues.Count; i++)
                {
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Issues[i] ?? string.Empty);
                }

                return hashCode;
            }
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        public string ToDiagnosticString()
        {
            var adapterText = string.IsNullOrWhiteSpace(AdapterName) ? "<none>" : AdapterName;
            var messageText = string.IsNullOrWhiteSpace(Message) ? "<none>" : Message;
            var builder = new StringBuilder();
            builder.Append($"adapter='{adapterText}' action='{Action}' status='{Status}' visible='{ShouldBeVisible}' progressSupported='{ProgressSupported}' issues='{IssueCount}' blockingIssues='{BlockingIssueCount}' message='{messageText}' request=({Request.ToDiagnosticString()})");
            if (HasIssues)
            {
                builder.Append(" issues=[");
                for (var i = 0; i < Issues.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append("; ");
                    }

                    builder.Append(Issues[i]);
                }

                builder.Append(']');
            }

            return builder.ToString();
        }

        public static LoadingSurfaceResult SucceededResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Succeeded, adapterName, message, Array.Empty<string>());
        }

        public static LoadingSurfaceResult SucceededWithWarningsResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.SucceededWithWarnings, adapterName, message, issues);
        }

        public static LoadingSurfaceResult SkippedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Skipped, adapterName, message, Array.Empty<string>());
        }

        public static LoadingSurfaceResult FailedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Failed, adapterName, message, issues);
        }

        public static LoadingSurfaceResult RejectedResult(
            LoadingSurfaceRequest request,
            string adapterName,
            string message,
            IReadOnlyList<string> issues)
        {
            return new LoadingSurfaceResult(request, LoadingSurfaceResultStatus.Rejected, adapterName, message, issues);
        }

        public static bool operator ==(LoadingSurfaceResult left, LoadingSurfaceResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LoadingSurfaceResult left, LoadingSurfaceResult right)
        {
            return !left.Equals(right);
        }

        private static string[] CopyIssues(IReadOnlyList<string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                copy[i] = Normalize(source[i]);
            }

            return copy;
        }

        private static bool SequenceEquals(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    /// <summary>
    /// Adapter-facing contract for executing one loading surface request.
    /// Implementations may perform concrete Unity operations, but they must return explicit results and must not
    /// own SceneLifecycle, RouteLifecycle or ActivityFlow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F24D Loading surface adapter contract; no registry or lifecycle ownership.")]
    public interface ILoadingSurfaceAdapter
    {
        /// <summary>Human-readable adapter name for diagnostics.</summary>
        string AdapterName { get; }

        /// <summary>Returns true when this adapter can present the supplied request.</summary>
        bool Supports(LoadingSurfaceRequest request);

        /// <summary>Shows the loading surface for a canonical request.</summary>
        LoadingSurfaceResult Show(LoadingSurfaceRequest request);

        /// <summary>Updates the loading surface from canonical request data.</summary>
        LoadingSurfaceResult Update(LoadingSurfaceRequest request);

        /// <summary>Hides the loading surface for a canonical request.</summary>
        LoadingSurfaceResult Hide(LoadingSurfaceRequest request);
    }

    /// <summary>
    /// API status: Internal. Explicit no-op loading surface adapter used for NoneConfigured/optional skip cases.
    /// It produces explicit skipped results and does not touch scene state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24D no-op loading surface adapter; explicit skip only.")]
    internal sealed class NoOpLoadingSurfaceAdapter : ILoadingSurfaceAdapter
    {
        internal static readonly NoOpLoadingSurfaceAdapter Instance = new NoOpLoadingSurfaceAdapter();

        private NoOpLoadingSurfaceAdapter()
        {
        }

        public string AdapterName => "NoOp Loading Surface Adapter";

        public bool Supports(LoadingSurfaceRequest request)
        {
            return request.IsValid;
        }

        public LoadingSurfaceResult Show(LoadingSurfaceRequest request)
        {
            return BuildSkipped(request, "show");
        }

        public LoadingSurfaceResult Update(LoadingSurfaceRequest request)
        {
            return BuildSkipped(request, "update");
        }

        public LoadingSurfaceResult Hide(LoadingSurfaceRequest request)
        {
            return BuildSkipped(request, "hide");
        }

        private LoadingSurfaceResult BuildSkipped(LoadingSurfaceRequest request, string verb)
        {
            if (!request.IsValid)
            {
                return LoadingSurfaceResult.RejectedResult(
                    request,
                    AdapterName,
                    $"No-op loading surface adapter rejected invalid request for {verb}.",
                    new[] { "loading-surface-invalid-request" });
            }

            return LoadingSurfaceResult.SkippedResult(
                request,
                AdapterName,
                $"Loading surface {verb} skipped because no Unity surface is configured.");
        }
    }
}

using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. Structured validator issue or non-blocking diagnostic emitted by F5G.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Structured local contribution validation issue introduced by F5G.")]
    internal readonly struct LocalContributionValidationIssue
    {
        public LocalContributionValidationIssue(
            LocalContributionValidationIssueKind kind,
            string message,
            bool blocking,
            string identityText = null,
            string diagnosticLabel = null)
        {
            Kind = kind;
            Message = string.IsNullOrWhiteSpace(message) ? "Local contribution validation issue." : message.Trim();
            Blocking = blocking;
            IdentityText = string.IsNullOrWhiteSpace(identityText) ? string.Empty : identityText.Trim();
            DiagnosticLabel = string.IsNullOrWhiteSpace(diagnosticLabel) ? string.Empty : diagnosticLabel.Trim();
        }

        public LocalContributionValidationIssueKind Kind { get; }

        public string Message { get; }

        public bool Blocking { get; }

        public string IdentityText { get; }

        public string DiagnosticLabel { get; }

        public string Severity => Blocking ? "Error" : "Info";

        public string ToDiagnosticString()
        {
            var identity = string.IsNullOrWhiteSpace(IdentityText) ? string.Empty : $" identity='{FormatValue(IdentityText)}'";
            var label = string.IsNullOrWhiteSpace(DiagnosticLabel) ? string.Empty : $" label='{FormatValue(DiagnosticLabel)}'";
            return $"kind='{Kind}' severity='{Severity}'{identity}{label} message='{FormatValue(Message)}'";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\\'");
        }
    }
}

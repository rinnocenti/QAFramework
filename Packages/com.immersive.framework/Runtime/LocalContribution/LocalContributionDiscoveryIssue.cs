using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.LocalContribution
{
    /// <summary>
    /// API status: Internal. One structured local contribution discovery issue.
    /// F5D treats all emitted issues as blocking authoring errors; optional/required policy is deferred.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "Structured local contribution discovery issue introduced by F5D.")]
    internal readonly struct LocalContributionDiscoveryIssue
    {
        public LocalContributionDiscoveryIssue(
            LocalContributionDiscoveryIssueKind kind,
            string message,
            string identityText = null,
            string sceneName = null,
            string objectName = null)
        {
            Kind = kind;
            Message = string.IsNullOrWhiteSpace(message) ? "Local contribution discovery issue." : message.Trim();
            IdentityText = string.IsNullOrWhiteSpace(identityText) ? string.Empty : identityText.Trim();
            SceneName = string.IsNullOrWhiteSpace(sceneName) ? string.Empty : sceneName.Trim();
            ObjectName = string.IsNullOrWhiteSpace(objectName) ? string.Empty : objectName.Trim();
        }

        public LocalContributionDiscoveryIssueKind Kind { get; }

        public string Message { get; }

        public string IdentityText { get; }

        public string SceneName { get; }

        public string ObjectName { get; }

        public string ToDiagnosticString()
        {
            var identity = string.IsNullOrWhiteSpace(IdentityText) ? string.Empty : $" identity='{FormatValue(IdentityText)}'";
            var scene = string.IsNullOrWhiteSpace(SceneName) ? string.Empty : $" scene='{FormatValue(SceneName)}'";
            var obj = string.IsNullOrWhiteSpace(ObjectName) ? string.Empty : $" object='{FormatValue(ObjectName)}'";
            return $"kind='{Kind}'{identity}{scene}{obj} message='{FormatValue(Message)}'";
        }

        public override string ToString()
        {
            return ToDiagnosticString();
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<empty>"
                : value.Replace("'", "\'");
        }
    }
}

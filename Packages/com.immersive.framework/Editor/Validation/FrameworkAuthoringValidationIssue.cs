using UnityEngine;
namespace Immersive.Framework.Editor.Editor.Validation
{
    internal readonly struct FrameworkAuthoringValidationIssue
    {
        internal FrameworkAuthoringValidationIssue(
            FrameworkAuthoringValidationSeverity severity,
            string message,
            Object context)
        {
            Severity = severity;
            Message = message ?? string.Empty;
            Context = context;
        }

        internal FrameworkAuthoringValidationSeverity Severity { get; }

        internal string Message { get; }

        internal Object Context { get; }
    }
}

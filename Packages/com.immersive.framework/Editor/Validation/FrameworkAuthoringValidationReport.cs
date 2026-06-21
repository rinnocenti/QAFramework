using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Framework.Editor.Validation
{
    internal sealed class FrameworkAuthoringValidationReport
    {
        private readonly List<FrameworkAuthoringValidationIssue> _issues = new List<FrameworkAuthoringValidationIssue>();

        internal IReadOnlyList<FrameworkAuthoringValidationIssue> Issues => _issues;

        internal int ErrorCount { get; private set; }

        internal int WarningCount { get; private set; }

        internal int InfoCount { get; private set; }

        internal bool IsValid => ErrorCount == 0;

        internal bool HasIssues => _issues.Count > 0;

        internal void AddError(string message, Object context)
        {
            Add(FrameworkAuthoringValidationSeverity.Error, message, context);
        }

        internal void AddWarning(string message, Object context)
        {
            Add(FrameworkAuthoringValidationSeverity.Warning, message, context);
        }

        internal void AddInfo(string message, Object context)
        {
            Add(FrameworkAuthoringValidationSeverity.Info, message, context);
        }

        internal void AddRange(FrameworkAuthoringValidationReport other)
        {
            if (other == null)
            {
                return;
            }

            IReadOnlyList<FrameworkAuthoringValidationIssue> issues = other.Issues;
            for (int i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                Add(issue.Severity, issue.Message, issue.Context);
            }
        }

        private void Add(FrameworkAuthoringValidationSeverity severity, string message, Object context)
        {
            _issues.Add(new FrameworkAuthoringValidationIssue(severity, message, context));

            switch (severity)
            {
                case FrameworkAuthoringValidationSeverity.Error:
                    ErrorCount++;
                    break;
                case FrameworkAuthoringValidationSeverity.Warning:
                    WarningCount++;
                    break;
                default:
                    InfoCount++;
                    break;
            }
        }
    }
}

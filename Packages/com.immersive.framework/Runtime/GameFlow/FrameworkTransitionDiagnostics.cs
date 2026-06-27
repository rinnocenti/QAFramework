using Immersive.Framework.ApiStatus;
using Immersive.Framework.Transition;

namespace Immersive.Framework.GameFlow
{
    /// <summary>
    /// API status: Internal. Route/Activity request-level Transition diagnostics for logging and smoke validation.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F24B request-level Transition diagnostics.")]
    internal readonly struct FrameworkTransitionDiagnostics
    {
        public FrameworkTransitionDiagnostics(
            TransitionScope scope,
            TransitionResult beforeResult,
            TransitionResult afterResult)
        {
            Scope = scope;
            BeforeResult = beforeResult;
            AfterResult = afterResult;
        }

        public TransitionScope Scope { get; }

        public TransitionResult BeforeResult { get; }

        public TransitionResult AfterResult { get; }

        public bool HasBefore => BeforeResult.IsValid;

        public bool HasAfter => AfterResult.IsValid;

        public bool HasDiagnostics => Scope != TransitionScope.Unknown && (HasBefore || HasAfter);

        public string ScopeText => HasDiagnostics ? Scope.ToString() : "<none>";

        public string TransitionText
        {
            get
            {
                if (!HasDiagnostics)
                {
                    return "<none>";
                }

                if (HasBefore && HasAfter && BeforeText == AfterText)
                {
                    return BeforeText;
                }

                if (HasBefore && !HasAfter)
                {
                    return BeforeText;
                }

                if (!HasBefore && HasAfter)
                {
                    return AfterText;
                }

                return $"{BeforeText}/{AfterText}";
            }
        }

        public string BeforeText => FormatResult(BeforeResult);

        public string AfterText => FormatResult(AfterResult);

        public int BlockingIssueCount
        {
            get
            {
                var count = 0;
                if (HasBefore)
                {
                    count += BeforeResult.BlockingIssueCount;
                }

                if (HasAfter)
                {
                    count += AfterResult.BlockingIssueCount;
                }

                return count;
            }
        }

        public static FrameworkTransitionDiagnostics Completed(
            TransitionScope scope,
            TransitionResult beforeResult,
            TransitionResult afterResult)
        {
            return new FrameworkTransitionDiagnostics(scope, beforeResult, afterResult);
        }

        private static string FormatResult(TransitionResult result)
        {
            if (!result.IsValid)
            {
                return "<none>";
            }

            return string.IsNullOrWhiteSpace(result.Message)
                ? result.Status.ToString()
                : result.Message;
        }
    }
}

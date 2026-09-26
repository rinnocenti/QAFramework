using System;

namespace Immersive.QaFramework.Certification
{
    internal enum QaCertificationVerdict
    {
        Pass,
        Fail,
        Blocked
    }

    internal enum QaCleanupDisposition
    {
        BaselineRestored,
        FreshBootRequired
    }

    internal readonly struct QaCertificationResult
    {
        internal QaCertificationResult(
            string scenarioId,
            QaCertificationVerdict verdict,
            string firstCausalDivergence,
            QaCleanupDisposition cleanupDisposition,
            string cleanupIssue)
        {
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                throw new ArgumentException(
                    "Scenario ID is required.",
                    nameof(scenarioId));
            }

            ScenarioId = scenarioId.Trim();
            Verdict = verdict;
            FirstCausalDivergence = firstCausalDivergence ?? string.Empty;
            CleanupDisposition = cleanupDisposition;
            CleanupIssue = cleanupIssue ?? string.Empty;
        }

        internal string ScenarioId { get; }
        internal QaCertificationVerdict Verdict { get; }
        internal string FirstCausalDivergence { get; }
        internal QaCleanupDisposition CleanupDisposition { get; }
        internal string CleanupIssue { get; }
    }

    internal sealed class QaCertificationRecorder
    {
        private bool _hasVerdict;
        private QaCertificationVerdict _verdict;
        private string _firstCausalDivergence = string.Empty;

        internal void RecordPass()
        {
            TryRecord(
                QaCertificationVerdict.Pass,
                string.Empty);
        }

        internal void RecordFirstCausalDivergence(
            QaCertificationVerdict verdict,
            string issue)
        {
            if (verdict == QaCertificationVerdict.Pass)
            {
                throw new ArgumentException(
                    "PASS is a terminal result, not a causal divergence.",
                    nameof(verdict));
            }

            TryRecord(verdict, issue);
        }

        internal QaCertificationResult CreateResult(
            string scenarioId,
            QaCleanupDisposition cleanupDisposition,
            string cleanupIssue)
        {
            if (!_hasVerdict)
            {
                throw new InvalidOperationException(
                    "QA certification result requires a terminal verdict.");
            }

            return new QaCertificationResult(
                scenarioId,
                _verdict,
                _firstCausalDivergence,
                cleanupDisposition,
                cleanupIssue);
        }

        private void TryRecord(
            QaCertificationVerdict verdict,
            string firstCausalDivergence)
        {
            if (_hasVerdict)
            {
                return;
            }

            _hasVerdict = true;
            _verdict = verdict;
            _firstCausalDivergence = verdict == QaCertificationVerdict.Pass
                ? string.Empty
                : firstCausalDivergence ?? string.Empty;
        }
    }
}

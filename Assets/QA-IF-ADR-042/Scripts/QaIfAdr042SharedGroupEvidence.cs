using System.Collections.Generic;
using UnityEngine;

namespace Immersive.QaFramework.IfAdr042
{
    [CreateAssetMenu(
        fileName = "QaIfAdr042SharedGroupEvidence",
        menuName = "QA Framework/IF-ADR-042/Shared Group Evidence")]
    public sealed class QaIfAdr042SharedGroupEvidence : ScriptableObject
    {
        private readonly List<QaIfAdr042OutputProbe> _outputs =
            new List<QaIfAdr042OutputProbe>();
        private readonly List<QaIfAdr042OccurrenceProbe> _occurrences =
            new List<QaIfAdr042OccurrenceProbe>();
        private string _diagnostic = string.Empty;

        public IReadOnlyList<QaIfAdr042OutputProbe> Outputs => _outputs;
        public IReadOnlyList<QaIfAdr042OccurrenceProbe> Occurrences => _occurrences;
        public string Diagnostic => _diagnostic;

        private void OnEnable() => ResetRuntimeEvidence();

        public void ResetRuntimeEvidence()
        {
            _outputs.Clear();
            _occurrences.Clear();
            _diagnostic = string.Empty;
        }

        public bool Register(QaIfAdr042OutputProbe probe)
        {
            RemoveDestroyed();
            if (probe == null || !probe.IsReady)
                return Fail("Output registration requires one initialized public Camera Output.");
            if (_outputs.Contains(probe)) return true;
            if (_outputs.Count > 0)
                return Fail("More than one QA Output registered for the single SharedGroup Output.");
            _outputs.Add(probe);
            _diagnostic = string.Empty;
            return true;
        }

        public bool Register(QaIfAdr042OccurrenceProbe probe)
        {
            RemoveDestroyed();
            if (probe == null || probe.Composer == null ||
                probe.Composer.FrameworkOwnedGroupTargetGroup == null)
                return Fail("Occurrence registration requires a materialized public Group TargetGroup.");
            if (_occurrences.Contains(probe)) return true;
            if (_occurrences.Count > 0)
                return Fail("More than one runtime SharedGroup occurrence registered for the single Assignment/Output.");
            _occurrences.Add(probe);
            _diagnostic = string.Empty;
            return true;
        }

        public void Unregister(QaIfAdr042OutputProbe probe)
        {
            _outputs.Remove(probe);
            if (_outputs.Count == 0) _diagnostic = "The QA Output was destroyed.";
        }

        public void Unregister(QaIfAdr042OccurrenceProbe probe)
        {
            _occurrences.Remove(probe);
            if (_occurrences.Count == 0) _diagnostic = "The SharedGroup occurrence was destroyed.";
        }

        public void ReportFailure(string issue)
        {
            _diagnostic = string.IsNullOrWhiteSpace(issue)
                ? "IF-ADR-042 runtime evidence could not be established."
                : issue.Trim();
        }

        private bool Fail(string issue)
        {
            _diagnostic = issue;
            return false;
        }

        private void RemoveDestroyed()
        {
            _outputs.RemoveAll(output => output == null);
            _occurrences.RemoveAll(occurrence => occurrence == null);
        }
    }
}

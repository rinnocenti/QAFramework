using System.Collections.Generic;
using Immersive.Framework.Camera;
using UnityEngine;

namespace Immersive.QaFramework.IfAdr043
{
    [CreateAssetMenu(fileName = "QaIfAdr043CameraOutputEvidence", menuName = "QA Framework/IF-ADR-043/Camera Output Evidence")]
    public sealed class QaIfAdr043CameraOutputEvidence : ScriptableObject
    {
        private readonly List<QaIfAdr043CameraOutputProbe> _outputs = new List<QaIfAdr043CameraOutputProbe>();
        private string _diagnostic = string.Empty;

        public IReadOnlyList<QaIfAdr043CameraOutputProbe> Outputs => _outputs;
        public string Diagnostic => _diagnostic;

        private void OnEnable() => ResetEvidence();

        public void ResetEvidence()
        {
            _outputs.Clear();
            _diagnostic = string.Empty;
        }

        public bool Register(QaIfAdr043CameraOutputProbe probe)
        {
            _outputs.RemoveAll(output => output == null);
            if (probe == null || probe.Output == null || probe.MappedSlot == null)
            {
                _diagnostic = "Output probe registration requires a live Camera Output and explicit mapped Slot.";
                return false;
            }

            for (int index = 0; index < _outputs.Count; index++)
            {
                if (_outputs[index] == null)
                    continue;
                if (_outputs[index] == probe)
                    return true;
                if (_outputs[index].Output != null && _outputs[index].Output.OutputId == probe.Output.OutputId)
                {
                    _diagnostic = $"More than one runtime Output registered for identity '{probe.Output.OutputId}'.";
                    return false;
                }
            }

            _outputs.Add(probe);
            _diagnostic = string.Empty;
            return true;
        }

        public void Unregister(QaIfAdr043CameraOutputProbe probe)
        {
            _outputs.Remove(probe);
        }

        public void ReportFailure(string issue)
        {
            _diagnostic = string.IsNullOrWhiteSpace(issue) ? "Camera Output evidence failed." : issue.Trim();
        }
    }
}

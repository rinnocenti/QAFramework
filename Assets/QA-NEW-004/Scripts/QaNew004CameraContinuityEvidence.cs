using System;
using UnityEngine;

namespace Immersive.QaFramework.New004
{
    [CreateAssetMenu(
        fileName = "QaNew004CameraContinuityEvidence",
        menuName = "QA Framework/QA-NEW-004/Camera Continuity Evidence")]
    public sealed class QaNew004CameraContinuityEvidence : ScriptableObject
    {
        [NonSerialized] private QaNew004CameraOutputProbe _currentOutput;
        [NonSerialized] private string _diagnostic;

        public QaNew004CameraOutputProbe CurrentOutput => _currentOutput;
        public string Diagnostic => _diagnostic ?? string.Empty;

        private void OnEnable()
        {
            ResetRuntimeEvidence();
        }

        public void ResetRuntimeEvidence()
        {
            _currentOutput = null;
            _diagnostic = string.Empty;
        }

        public bool Register(QaNew004CameraOutputProbe output)
        {
            if (output == null)
            {
                _diagnostic = "Camera Output evidence registration requires a live probe.";
                return false;
            }

            if (_currentOutput != null && !ReferenceEquals(_currentOutput, output))
            {
                _diagnostic = "More than one QA-NEW-004 Camera Output registered for its single Output composition.";
                return false;
            }

            _currentOutput = output;
            _diagnostic = string.Empty;
            return true;
        }

        public void Unregister(QaNew004CameraOutputProbe output)
        {
            if (ReferenceEquals(_currentOutput, output))
            {
                _currentOutput = null;
                _diagnostic = "The Session Camera Output probe was destroyed.";
            }
        }

        public void ReportFailure(string issue)
        {
            _diagnostic = string.IsNullOrWhiteSpace(issue)
                ? "Camera Output evidence could not be established."
                : issue.Trim();
        }
    }
}

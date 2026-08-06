using System;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    internal static class QaPlayerHostEvidenceDiagnosticFormattingSmoke
    {
        [MenuItem("Immersive Framework/QA/Regressions/Game Flow/Run Player Host Evidence Diagnostic Formatting Smoke", priority = 262)]
        private static void Run()
        {
            var result = new PlayerHostEvidenceResult(
                PlayerHostEvidenceStatus.RejectedNoEvidence,
                "ReleaseHostEvidence",
                default,
                default,
                null,
                "qa.player.release",
                "partial-evidence",
                "No retained Host evidence is available.");
            string diagnostic = result.ToDiagnosticString();
            Require(diagnostic.Contains("slot='<invalid>'"), "Partial Host evidence must expose an explicit invalid Slot marker.");
            Require(diagnostic.Contains("source='qa.player.release'") && diagnostic.Contains("reason='partial-evidence'"), "Partial Host evidence diagnostics must retain source and reason.");
            Debug.Log("[QA_PLAYER_HOST_EVIDENCE_DIAGNOSTIC] status='Passed' case='partial-invalid-evidence'.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}

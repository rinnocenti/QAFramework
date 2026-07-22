using System;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using UnityEngine;

namespace ImmersiveFrameworkQA.PauseP1
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Pause/Official Player Preflight")]
    public sealed class PauseOfficialPlayerPreflightPanel : MonoBehaviour
    {
        private const string LogPrefix = "[PAUSE_PLAYER_PREFLIGHT]";
        private const string Source = nameof(PauseOfficialPlayerPreflightPanel);

        [SerializeField]
        private LocalPlayerProvisioningAuthoring provisioningAuthoring;

        [SerializeField]
        private PauseRequestTrigger pauseRequestTrigger;

        [SerializeField]
        private Rect panelRect = new Rect(500f, 16f, 500f, 300f);

        private string lastOperation = "Select Prepare Official Player for Pause QA.";

        public LocalPlayerProvisioningAuthoring ProvisioningAuthoring =>
            provisioningAuthoring;

        public PauseRequestTrigger PauseRequestTrigger =>
            pauseRequestTrigger;

        public void Configure(
            LocalPlayerProvisioningAuthoring nextProvisioningAuthoring,
            PauseRequestTrigger nextPauseRequestTrigger)
        {
            provisioningAuthoring = nextProvisioningAuthoring;
            pauseRequestTrigger = nextPauseRequestTrigger;
            panelRect = new Rect(500f, 16f, 500f, 300f);
        }

        public void PrepareOfficialPlayerForPauseQa()
        {
            bool ready = TryPrepare(
                out int joined,
                out string operation,
                out string diagnostic);

            lastOperation = diagnostic;
            if (ready)
            {
                Debug.Log(
                    $"{LogPrefix} status='Ready' joined='{joined}' " +
                    $"operation='{operation}'",
                    this);
                return;
            }

            Debug.LogError(
                $"{LogPrefix} status='Failed' joined='{joined}' " +
                $"reason='{Sanitize(diagnostic)}'",
                this);
        }

        private bool TryPrepare(
            out int joined,
            out string operation,
            out string diagnostic)
        {
            joined = 0;
            operation = string.Empty;
            diagnostic = string.Empty;

            if (provisioningAuthoring == null)
            {
                diagnostic =
                    "Official Local Player provisioning reference is missing.";
                return false;
            }

            if (!provisioningAuthoring.RuntimeReady)
            {
                diagnostic =
                    $"Official Local Player provisioning is not RuntimeReady. {provisioningAuthoring.RuntimeDiagnostic}";
                return false;
            }

            PlayerParticipationSnapshot snapshot =
                provisioningAuthoring.RuntimeSnapshot;
            joined = snapshot.JoinedCount;
            if (joined == 1)
            {
                operation = "AlreadyReady";
                diagnostic = "Official Local Player is already ready.";
                return true;
            }

            if (joined > 1)
            {
                diagnostic =
                    $"Pause QA supports exactly one Joined Slot; found '{joined}'.";
                return false;
            }

            if (snapshot.DynamicCapacity != 1)
            {
                PlayerParticipationOperationResult capacity =
                    provisioningAuthoring.SetDynamicCapacity(
                        1,
                        Source,
                        "pause-qa-preflight-capacity");
                if (capacity == null || !capacity.Completed)
                {
                    diagnostic =
                        "Could not set dynamic Local Player capacity to one. " +
                        (capacity != null
                            ? capacity.ToDiagnosticString()
                            : "No result was returned.");
                    return false;
                }
            }

            PlayerParticipationOperationResult opened =
                provisioningAuthoring.OpenJoining(
                    Source,
                    "pause-qa-preflight-open");
            if (opened == null || !opened.Completed)
            {
                diagnostic =
                    "Could not open Local Player joining. " +
                    (opened != null
                        ? opened.ToDiagnosticString()
                        : "No result was returned.");
                return false;
            }

            bool succeeded = false;
            string joinDiagnostic = string.Empty;
            string closeDiagnostic = string.Empty;
            try
            {
                LocalPlayerJoinResult join =
                    provisioningAuthoring.RequestJoin(
                        Source,
                        "pause-qa-preflight-join");
                if (join == null ||
                    join.Status != LocalPlayerJoinStatus.SucceededJoined)
                {
                    joinDiagnostic =
                        "Official Local Player join did not return SucceededJoined. " +
                        (join != null
                            ? join.ToDiagnosticString()
                            : "No result was returned.");
                }
                else
                {
                    PlayerParticipationSnapshot afterJoin =
                        provisioningAuthoring.RuntimeSnapshot;
                    joined = afterJoin.JoinedCount;
                    if (joined != 1)
                    {
                        joinDiagnostic =
                            $"Expected exactly one Joined Slot after join; found '{joined}'.";
                    }
                    else
                    {
                        succeeded = true;
                    }
                }
            }
            finally
            {
                PlayerParticipationOperationResult closed =
                    provisioningAuthoring.CloseJoining(
                        Source,
                        "pause-qa-preflight-close");
                if (closed == null || !closed.Completed)
                {
                    closeDiagnostic =
                        "Could not close Local Player joining. " +
                        (closed != null
                            ? closed.ToDiagnosticString()
                            : "No result was returned.");
                    succeeded = false;
                }
            }

            if (!succeeded)
            {
                diagnostic = string.IsNullOrWhiteSpace(closeDiagnostic)
                    ? joinDiagnostic
                    : string.IsNullOrWhiteSpace(joinDiagnostic)
                        ? closeDiagnostic
                        : joinDiagnostic + " " + closeDiagnostic;
                return false;
            }

            operation = "Joined";
            diagnostic = "Official Local Player joined and joining was closed.";
            return true;
        }

        private string ResolvePlayerStatus()
        {
            if (provisioningAuthoring == null ||
                !provisioningAuthoring.RuntimeReady)
            {
                return "Missing";
            }

            int joined =
                provisioningAuthoring.RuntimeSnapshot.JoinedCount;
            if (joined == 1)
            {
                return "Ready";
            }

            return joined > 1
                ? "Unsupported Multiple"
                : "Missing";
        }

        private void OnGUI()
        {
            panelRect = GUI.Window(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this),
                panelRect,
                DrawWindow,
                "Pause Product Binding QA");
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(8f);
            GUILayout.Label($"Official Player: {ResolvePlayerStatus()}");
            var wrapped = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
            GUILayout.Label(lastOperation, wrapped);
            GUILayout.Label(
                $"Product Request Binding: {ResolveProductRequestBinding()}");
            GUILayout.Label($"Logical Pause: {ResolveLogicalPause()}");
            GUILayout.Label(
                $"Last Authored Trigger Outcome: {ResolveLastTriggerOutcome()}");
            GUILayout.Label(
                $"Last Authored Trigger Status: {ResolveLastTriggerStatus()}");
            GUILayout.Label(
                $"Last Authored Trigger Diagnostic: {ResolveLastTriggerDiagnostic()}",
                wrapped);
            GUILayout.Space(8f);

            if (GUILayout.Button(
                    "Prepare Official Player for Pause QA",
                    GUILayout.Height(34f)))
            {
                PrepareOfficialPlayerForPauseQa();
            }

            GUI.DragWindow(
                new Rect(0f, 0f, 10000f, 24f));
        }

        private string ResolveProductRequestBinding() =>
            pauseRequestTrigger != null
                ? pauseRequestTrigger.ProductRequestBindingStatus
                : "Missing";

        private string ResolveLogicalPause()
        {
            if (pauseRequestTrigger == null ||
                !pauseRequestTrigger.TryGetPauseSnapshot(
                    out PauseSnapshot snapshot))
            {
                return "Unavailable";
            }

            return snapshot.State.ToString();
        }

        private string ResolveLastTriggerOutcome() =>
            pauseRequestTrigger != null
                ? pauseRequestTrigger.LastOutcome.ToString()
                : "Unavailable";

        private string ResolveLastTriggerStatus() =>
            pauseRequestTrigger != null
                ? pauseRequestTrigger.LastStatus.ToString()
                : "Unavailable";

        private string ResolveLastTriggerDiagnostic() =>
            pauseRequestTrigger != null &&
            !string.IsNullOrWhiteSpace(pauseRequestTrigger.LastMessage)
                ? pauseRequestTrigger.LastMessage
                : "Unavailable";

        private static string Sanitize(string value) =>
            string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "’")
                    .Replace(Environment.NewLine, " ");
    }
}

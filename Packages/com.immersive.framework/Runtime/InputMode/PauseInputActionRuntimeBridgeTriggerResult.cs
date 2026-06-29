using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.Pause;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result produced when an opt-in Unity InputAction trigger submits to the Pause PlayerInput runtime bridge.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F33B InputAction trigger result for the Pause runtime PlayerInput bridge.")]
    public sealed class PauseInputActionRuntimeBridgeTriggerResult
    {
        public PauseInputActionRuntimeBridgeTriggerResult(
            PauseInputActionRuntimeBridgeTriggerStatus status,
            PauseRequestKind requestKind,
            string actionMapName,
            string actionName,
            bool actionEvidenceRequired,
            bool actionResolved,
            bool bridgeSubmitted,
            PauseInputModeUnityPlayerInputRuntimeBridgeResult bridgeResult,
            string source,
            string reason,
            string message)
        {
            Status = status;
            RequestKind = requestKind;
            ActionMapName = actionMapName.NormalizeText();
            ActionName = actionName.NormalizeText();
            ActionEvidenceRequired = actionEvidenceRequired;
            ActionResolved = actionResolved;
            BridgeSubmitted = bridgeSubmitted;
            BridgeResult = bridgeResult;
            Source = source.NormalizeTextOrFallback(nameof(PauseInputActionRuntimeBridgeTriggerResult));
            Reason = reason.NormalizeText();
            Message = message.NormalizeText();
        }

        public PauseInputActionRuntimeBridgeTriggerStatus Status { get; }

        public PauseRequestKind RequestKind { get; }

        public string ActionMapName { get; }

        public string ActionName { get; }

        public bool ActionEvidenceRequired { get; }

        public bool ActionResolved { get; }

        public bool BridgeSubmitted { get; }

        public PauseInputModeUnityPlayerInputRuntimeBridgeResult BridgeResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public bool Succeeded => Status == PauseInputActionRuntimeBridgeTriggerStatus.Succeeded;

        public bool Ignored => Status == PauseInputActionRuntimeBridgeTriggerStatus.IgnoredBridgeResult;

        public bool Failed => !Succeeded && !Ignored;

        public InputModeKind RequestedMode => BridgeResult == null ? InputModeKind.Unknown : BridgeResult.RequestedMode;

        public InputModeUnityApplicationPlanOperation Operation => BridgeResult == null
            ? InputModeUnityApplicationPlanOperation.NoOperation
            : BridgeResult.Operation;

        public UnityInputActionMapName AppliedActionMapName => BridgeResult == null
            ? UnityInputActionMapName.From(string.Empty)
            : BridgeResult.AppliedActionMapName;

        public bool Applied => BridgeResult != null && BridgeResult.Applied;

        public bool ActivatedPlayerInput => BridgeResult != null && BridgeResult.ActivatedPlayerInput;

        public bool SelectedActionMap => BridgeResult != null && BridgeResult.SelectedActionMap;

        public bool DeactivatedPlayerInput => BridgeResult != null && BridgeResult.DeactivatedPlayerInput;

        public bool SwitchesActionMaps => BridgeResult != null && BridgeResult.SwitchesActionMaps;

        public bool AppliesInputBehavior => BridgeResult != null && BridgeResult.AppliesInputBehavior;

        public bool PauseRuntimeWiring => BridgeResult != null && BridgeResult.PauseRuntimeWiring;

        public bool CallsPlayerJoin => false;

        public bool SpawnsActor => false;

        public bool UsesCustomInputManager => false;

        public int IssueCount
        {
            get
            {
                if (BridgeResult != null)
                {
                    return BridgeResult.IssueCount;
                }

                return Failed ? 1 : 0;
            }
        }

        public int BlockingIssueCount
        {
            get
            {
                if (BridgeResult != null)
                {
                    return BridgeResult.BlockingIssueCount;
                }

                return Failed ? 1 : 0;
            }
        }

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("status='").Append(Status).Append("'");
            builder.Append(" requestKind='").Append(RequestKind).Append("'");
            builder.Append(" actionMap='").Append(ActionMapName.ToDiagnosticText()).Append("'");
            builder.Append(" action='").Append(ActionName.ToDiagnosticText()).Append("'");
            builder.Append(" actionEvidenceRequired='").Append(ActionEvidenceRequired).Append("'");
            builder.Append(" actionResolved='").Append(ActionResolved).Append("'");
            builder.Append(" bridgeSubmitted='").Append(BridgeSubmitted).Append("'");
            builder.Append(" requestedMode='").Append(RequestedMode).Append("'");
            builder.Append(" operation='").Append(Operation).Append("'");
            builder.Append(" appliedActionMap='").Append(AppliedActionMapName).Append("'");
            builder.Append(" applied='").Append(Applied).Append("'");
            builder.Append(" activatedPlayerInput='").Append(ActivatedPlayerInput).Append("'");
            builder.Append(" selectedActionMap='").Append(SelectedActionMap).Append("'");
            builder.Append(" deactivatedPlayerInput='").Append(DeactivatedPlayerInput).Append("'");
            builder.Append(" issues='").Append(IssueCount).Append("'");
            builder.Append(" blockingIssues='").Append(BlockingIssueCount).Append("'");
            builder.Append(" actionMapSwitching='").Append(SwitchesActionMaps).Append("'");
            builder.Append(" inputBehavior='").Append(AppliesInputBehavior).Append("'");
            builder.Append(" pauseRuntimeWiring='").Append(PauseRuntimeWiring).Append("'");
            builder.Append(" playerJoin='").Append(CallsPlayerJoin).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            builder.Append(" customInputManager='").Append(UsesCustomInputManager).Append("'");

            if (!string.IsNullOrWhiteSpace(Message))
            {
                builder.Append(" message='").Append(Message.ToDiagnosticText()).Append("'");
            }

            if (BridgeResult != null)
            {
                builder.Append(" bridge='").Append(BridgeResult.ToDiagnosticString().ToDiagnosticText()).Append("'");
            }

            return builder.ToString();
        }
    }
}

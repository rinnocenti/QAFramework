using System;
using System.Text;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Common;
using Immersive.Framework.UnityInput;

namespace Immersive.Framework.InputMode
{
    /// <summary>
    /// API status: Experimental. Result for the explicit end-to-end InputMode request application pipeline.
    /// This result exposes every stage and keeps PlayerInputManager join/spawn ownership out of the framework.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F32F InputMode request to explicit Unity PlayerInput application result.")]
    public sealed class InputModeUnityPlayerInputRequestApplicationResult
    {
        public InputModeUnityPlayerInputRequestApplicationResult(
            InputModeUnityPlayerInputRequestApplicationStatus status,
            InputModeKind requestedMode,
            InputModeRequestResult inputModeRequestResult,
            InputModeUnityApplicationPreviewResult applicationPreviewResult,
            InputModeUnityActionMapPreviewResult actionMapPreviewResult,
            InputModeUnityApplicationPlanResult applicationPlanResult,
            InputModeUnityPlayerInputApplicationResult playerInputApplicationResult,
            string source,
            string reason)
        {
            if (!Enum.IsDefined(typeof(InputModeUnityPlayerInputRequestApplicationStatus), status) || status == InputModeUnityPlayerInputRequestApplicationStatus.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "InputMode PlayerInput request application status must be explicit.");
            }

            Status = status;
            RequestedMode = requestedMode;
            InputModeRequestResult = inputModeRequestResult;
            ApplicationPreviewResult = applicationPreviewResult;
            ActionMapPreviewResult = actionMapPreviewResult;
            ApplicationPlanResult = applicationPlanResult;
            PlayerInputApplicationResult = playerInputApplicationResult;
            Source = source.NormalizeTextOrFallback(nameof(InputModeUnityPlayerInputRequestApplicationResult));
            Reason = reason.NormalizeText();
        }

        public InputModeUnityPlayerInputRequestApplicationStatus Status { get; }

        public InputModeKind RequestedMode { get; }

        public InputModeRequestResult InputModeRequestResult { get; }

        public InputModeUnityApplicationPreviewResult ApplicationPreviewResult { get; }

        public InputModeUnityActionMapPreviewResult ActionMapPreviewResult { get; }

        public InputModeUnityApplicationPlanResult ApplicationPlanResult { get; }

        public InputModeUnityPlayerInputApplicationResult PlayerInputApplicationResult { get; }

        public string Source { get; }

        public string Reason { get; }

        public bool Succeeded => Status == InputModeUnityPlayerInputRequestApplicationStatus.Succeeded;

        public bool Ignored => Status == InputModeUnityPlayerInputRequestApplicationStatus.IgnoredInputModeRequest;

        public bool Failed => !Succeeded && !Ignored;

        public bool Applied => PlayerInputApplicationResult != null && PlayerInputApplicationResult.Applied;

        public bool ActivatedPlayerInput => PlayerInputApplicationResult != null && PlayerInputApplicationResult.ActivatedPlayerInput;

        public bool SelectedActionMap => PlayerInputApplicationResult != null && PlayerInputApplicationResult.SelectedActionMap;

        public bool DeactivatedPlayerInput => PlayerInputApplicationResult != null && PlayerInputApplicationResult.DeactivatedPlayerInput;

        public UnityInputActionMapName AppliedActionMapName => PlayerInputApplicationResult == null
            ? UnityInputActionMapName.From(string.Empty)
            : PlayerInputApplicationResult.AppliedActionMapName;

        public InputModeUnityApplicationPlanOperation Operation => ApplicationPlanResult == null
            ? InputModeUnityApplicationPlanOperation.NoOperation
            : ApplicationPlanResult.Operation;

        public bool SwitchesActionMaps => PlayerInputApplicationResult != null && PlayerInputApplicationResult.SwitchesActionMaps;

        public bool AppliesInputBehavior => PlayerInputApplicationResult != null && PlayerInputApplicationResult.AppliesInputBehavior;

        public bool CallsPlayerJoin => false;

        public bool SpawnsActor => false;

        public bool UsesCustomInputManager => false;

        public int IssueCount => SumIssues(false);

        public int BlockingIssueCount => SumIssues(true);

        public string ToDiagnosticString()
        {
            var builder = new StringBuilder();
            builder.Append("status='").Append(Status).Append("'");
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
            builder.Append(" playerJoin='").Append(CallsPlayerJoin).Append("'");
            builder.Append(" actorSpawning='").Append(SpawnsActor).Append("'");
            builder.Append(" customInputManager='").Append(UsesCustomInputManager).Append("'");
            if (InputModeRequestResult != null)
            {
                builder.Append(" inputModeRequest='").Append(InputModeRequestResult.Status).Append("'");
            }

            if (ApplicationPreviewResult != null)
            {
                builder.Append(" applicationPreview='").Append(ApplicationPreviewResult.Status).Append("'");
            }

            if (ActionMapPreviewResult != null)
            {
                builder.Append(" actionMapPreview='").Append(ActionMapPreviewResult.Status).Append("'");
            }

            if (ApplicationPlanResult != null)
            {
                builder.Append(" applicationPlan='").Append(ApplicationPlanResult.Status).Append("'");
            }

            if (PlayerInputApplicationResult != null)
            {
                builder.Append(" playerInputApplication='").Append(PlayerInputApplicationResult.Status).Append("'");
            }

            return builder.ToString();
        }

        private int SumIssues(bool blockingOnly)
        {
            int count = 0;
            if (InputModeRequestResult != null)
            {
                count += blockingOnly ? InputModeRequestResult.BlockingIssueCount : InputModeRequestResult.IssueCount;
            }

            if (ApplicationPreviewResult != null)
            {
                count += blockingOnly ? ApplicationPreviewResult.BlockingIssueCount : ApplicationPreviewResult.IssueCount;
            }

            if (ActionMapPreviewResult != null)
            {
                count += blockingOnly ? ActionMapPreviewResult.BlockingIssueCount : ActionMapPreviewResult.IssueCount;
            }

            if (ApplicationPlanResult != null)
            {
                count += blockingOnly ? ApplicationPlanResult.BlockingIssueCount : ApplicationPlanResult.IssueCount;
            }

            if (PlayerInputApplicationResult != null)
            {
                count += blockingOnly ? PlayerInputApplicationResult.BlockingIssueCount : PlayerInputApplicationResult.IssueCount;
            }

            return count;
        }
    }
}

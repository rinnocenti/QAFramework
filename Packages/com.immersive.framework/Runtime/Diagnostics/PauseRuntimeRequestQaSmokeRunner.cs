#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Gate;
using Immersive.Framework.Pause;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Smoke for F20E minimal runtime Pause request path.
    /// It exercises FrameworkRuntimeHost -> PauseRuntime without input, overlay, UI, Time.timeScale,
    /// Route/Activity ownership or a global Gate registry.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F20E runtime Pause request path smoke; no input, overlay or Time.timeScale.")]
    internal static class PauseRuntimeRequestQaSmokeRunner
    {
        internal const string SmokeName = "Pause Runtime Request Smoke";

        internal static Task<bool> RunRuntimeRequestSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return Task.FromResult(false);
            }

            var normalizedSource = string.IsNullOrWhiteSpace(source) ? nameof(PauseRuntimeRequestQaSmokeRunner) : source.Trim();

            var ensureRunningPassed = EnsureRunning(runtimeHost, logger, normalizedSource);
            var pauseAppliedPassed = ValidatePauseRequestApplied(runtimeHost, logger, normalizedSource);
            var pausedGatePassed = ValidatePausedGateBlocksGameplay(runtimeHost, logger, normalizedSource);
            var toggleResumePassed = ValidateToggleRequestResumes(runtimeHost, logger, normalizedSource);
            var resumeNoChangePassed = ValidateResumeNoChange(runtimeHost, logger, normalizedSource);
            var runningSnapshotPassed = ValidateRunningSnapshot(runtimeHost, logger, normalizedSource);

            return Task.FromResult(ensureRunningPassed
                && pauseAppliedPassed
                && pausedGatePassed
                && toggleResumePassed
                && resumeNoChangePassed
                && runningSnapshotPassed);
        }

        private static bool EnsureRunning(FrameworkRuntimeHost runtimeHost, FrameworkLogger logger, string source)
        {
            var result = runtimeHost.RequestPause(PauseRequest.Resume(
                "qa.pause.runtime.ensure-running",
                source,
                "qa.pause.runtime.ensure-running"));

            var gateSnapshot = runtimeHost.PauseGateSnapshot;
            var passed = result.IsValid
                && result.Completed
                && result.IsRunning
                && runtimeHost.PauseState == PauseState.Running
                && gateSnapshot.IsEmpty;

            LogResultStep(logger, "ensure-running", result, runtimeHost, passed);
            return passed;
        }

        private static bool ValidatePauseRequestApplied(FrameworkRuntimeHost runtimeHost, FrameworkLogger logger, string source)
        {
            var result = runtimeHost.RequestPause(PauseRequest.Pause(
                "qa.pause.runtime.pause-applied",
                source,
                "qa.pause.runtime.pause"));

            var gateSnapshot = runtimeHost.PauseGateSnapshot;
            var passed = result.Applied
                && result.Completed
                && result.StateChanged
                && result.PreviousState == PauseState.Running
                && result.CurrentState == PauseState.Paused
                && runtimeHost.PauseState == PauseState.Paused
                && gateSnapshot.BlockerCount == 2
                && gateSnapshot.IsBlocked(GateScope.Gameplay, GateDomain.GameplayAction)
                && gateSnapshot.IsBlocked(GateScope.Interaction, GateDomain.InteractionAcceptance)
                && !gateSnapshot.IsBlocked(GateScope.Pause, GateDomain.PauseRequest);

            LogResultStep(logger, "pause-request-applied", result, runtimeHost, passed);
            return passed;
        }

        private static bool ValidatePausedGateBlocksGameplay(FrameworkRuntimeHost runtimeHost, FrameworkLogger logger, string source)
        {
            var evaluation = runtimeHost.EvaluatePauseGateAdmission(
                GateScope.Gameplay,
                GateDomain.GameplayAction,
                "GameplayAction",
                source,
                "qa.pause.runtime.evaluate-gameplay");

            var passed = runtimeHost.PauseState == PauseState.Paused
                && evaluation.IsBlocked
                && evaluation.Status == GateDecisionStatus.Blocked
                && evaluation.BlockingBlockerCount == 1
                && evaluation.Decision.PolicySource == PauseGateBlockerPolicy.PolicySource;

            LogGateStep(logger, "paused-gate-blocks-gameplay", runtimeHost, evaluation, passed);
            return passed;
        }

        private static bool ValidateToggleRequestResumes(FrameworkRuntimeHost runtimeHost, FrameworkLogger logger, string source)
        {
            var result = runtimeHost.RequestPause(PauseRequest.Toggle(
                "qa.pause.runtime.toggle-resume",
                source,
                "qa.pause.runtime.toggle"));

            var gateSnapshot = runtimeHost.PauseGateSnapshot;
            var passed = result.Applied
                && result.Completed
                && result.StateChanged
                && result.PreviousState == PauseState.Paused
                && result.CurrentState == PauseState.Running
                && runtimeHost.PauseState == PauseState.Running
                && gateSnapshot.IsEmpty;

            LogResultStep(logger, "toggle-request-resumes", result, runtimeHost, passed);
            return passed;
        }

        private static bool ValidateResumeNoChange(FrameworkRuntimeHost runtimeHost, FrameworkLogger logger, string source)
        {
            var result = runtimeHost.RequestPause(PauseRequest.Resume(
                "qa.pause.runtime.resume-no-change",
                source,
                "qa.pause.runtime.resume-no-change"));

            var gateSnapshot = runtimeHost.PauseGateSnapshot;
            var passed = result.IgnoredNoChange
                && result.Completed
                && !result.StateChanged
                && result.PreviousState == PauseState.Running
                && result.CurrentState == PauseState.Running
                && runtimeHost.PauseState == PauseState.Running
                && gateSnapshot.IsEmpty;

            LogResultStep(logger, "resume-no-change", result, runtimeHost, passed);
            return passed;
        }

        private static bool ValidateRunningSnapshot(FrameworkRuntimeHost runtimeHost, FrameworkLogger logger, string source)
        {
            var snapshotAvailable = runtimeHost.TryGetPauseSnapshot(out var snapshot);
            var gateSnapshot = runtimeHost.PauseGateSnapshot;
            var evaluation = runtimeHost.EvaluatePauseGateAdmission(
                GateScope.Gameplay,
                GateDomain.GameplayAction,
                "GameplayAction",
                source,
                "qa.pause.runtime.evaluate-running");

            var passed = snapshotAvailable
                && snapshot.IsRunning
                && !snapshot.IsPaused
                && snapshot.HasLastRequest
                && snapshot.LastRequestId.StableText == "Pause:qa.pause.runtime.resume-no-change"
                && gateSnapshot.IsEmpty
                && evaluation.IsAllowed
                && evaluation.BlockingBlockerCount == 0;

            LogSnapshotStep(logger, "snapshot-running", snapshotAvailable, snapshot, gateSnapshot, evaluation, passed);
            return passed;
        }

        private static void LogResultStep(
            FrameworkLogger logger,
            string step,
            PauseResult result,
            FrameworkRuntimeHost runtimeHost,
            bool passed)
        {
            var gateSnapshot = runtimeHost.PauseGateSnapshot;
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("request", result.RequestId.StableText),
                LogFields.Field("kind", result.Kind.ToString()),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("previousState", result.PreviousState.ToString()),
                LogFields.Field("currentState", result.CurrentState.ToString()),
                LogFields.Field("runtimeState", runtimeHost.PauseState.ToString()),
                LogFields.Field("completed", result.Completed),
                LogFields.Field("applied", result.Applied),
                LogFields.Field("ignoredNoChange", result.IgnoredNoChange),
                LogFields.Field("stateChanged", result.StateChanged),
                LogFields.Field("gateBlockers", gateSnapshot.BlockerCount),
                LogFields.Field("blocksGameplay", gateSnapshot.IsBlocked(GateScope.Gameplay, GateDomain.GameplayAction)),
                LogFields.Field("blocksInteraction", gateSnapshot.IsBlocked(GateScope.Interaction, GateDomain.InteractionAcceptance)),
                LogFields.Field("blocksPauseRequest", gateSnapshot.IsBlocked(GateScope.Pause, GateDomain.PauseRequest)),
                LogFields.Field("issues", result.IssueCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount));

            if (passed)
            {
                logger.Info("QA Pause Runtime Request Smoke step completed.", fields);
                logger.Debug("QA Pause Runtime Request Smoke result diagnostics.", LogFields.Field("details", result.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Pause Runtime Request Smoke step failed.", fields);
        }

        private static void LogGateStep(
            FrameworkLogger logger,
            string step,
            FrameworkRuntimeHost runtimeHost,
            GateEvaluationResult evaluation,
            bool passed)
        {
            var gateSnapshot = runtimeHost.PauseGateSnapshot;
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("runtimeState", runtimeHost.PauseState.ToString()),
                LogFields.Field("status", evaluation.Status.ToString()),
                LogFields.Field("scope", evaluation.Scope.ToString()),
                LogFields.Field("domain", evaluation.Domain.ToString()),
                LogFields.Field("policySource", evaluation.Decision.PolicySource),
                LogFields.Field("snapshotBlockers", gateSnapshot.BlockerCount),
                LogFields.Field("blockers", evaluation.BlockingBlockerCount),
                LogFields.Field("facts", evaluation.FactCount));

            if (passed)
            {
                logger.Info("QA Pause Runtime Request Smoke step completed.", fields);
                logger.Debug("QA Pause Runtime Request Smoke gate diagnostics.", LogFields.Field("details", evaluation.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Pause Runtime Request Smoke step failed.", fields);
        }

        private static void LogSnapshotStep(
            FrameworkLogger logger,
            string step,
            bool snapshotAvailable,
            PauseSnapshot snapshot,
            GateSnapshot gateSnapshot,
            GateEvaluationResult evaluation,
            bool passed)
        {
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("snapshotAvailable", snapshotAvailable),
                LogFields.Field("state", snapshotAvailable ? snapshot.State.ToString() : "<none>"),
                LogFields.Field("paused", snapshotAvailable && snapshot.IsPaused),
                LogFields.Field("running", snapshotAvailable && snapshot.IsRunning),
                LogFields.Field("lastRequest", snapshotAvailable && snapshot.HasLastRequest ? snapshot.LastRequestId.StableText : "<none>"),
                LogFields.Field("gateBlockers", gateSnapshot.BlockerCount),
                LogFields.Field("gameplayAdmission", evaluation.Status.ToString()),
                LogFields.Field("gameplayBlockers", evaluation.BlockingBlockerCount),
                LogFields.Field("facts", snapshotAvailable ? snapshot.FactCount : 0),
                LogFields.Field("issues", snapshotAvailable ? snapshot.IssueCount : 0),
                LogFields.Field("blockingIssues", snapshotAvailable ? snapshot.BlockingIssueCount : 0));

            if (passed)
            {
                logger.Info("QA Pause Runtime Request Smoke step completed.", fields);
                if (snapshotAvailable)
                {
                    logger.Debug("QA Pause Runtime Request Smoke snapshot diagnostics.", LogFields.Field("details", snapshot.ToDiagnosticString()));
                }

                return;
            }

            logger.Warning("QA Pause Runtime Request Smoke step failed.", fields);
        }
    }
}
#endif

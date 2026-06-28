#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Pause;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F20D Pause/Gate relationship.
    /// It validates that Pause can describe Gate blockers without registering runtime Gate state or mutating flow.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F20D Pause Gate blocker relationship smoke; synthetic passive policy only.")]
    internal static class PauseGateBlockerQaSmokeRunner
    {
        internal const string SmokeName = "Pause Gate Blocker Policy Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            var normalizedSource = string.IsNullOrWhiteSpace(source)
                ? nameof(PauseGateBlockerQaSmokeRunner)
                : source.Trim();

            var pausedSnapshot = PauseSnapshot.FromState(
                PauseState.Paused,
                normalizedSource,
                "qa.pause.gate.paused",
                new[] { "qa.pause.gate.paused.snapshot" });

            var pausedBlockersCreatedPassed = ValidatePausedBlockersCreated(logger, pausedSnapshot, normalizedSource);
            var inputBlockedPassed = ValidatePausedBlocksInputAcceptance(logger, pausedSnapshot, normalizedSource);
            var interactionBlockedPassed = ValidatePausedBlocksInteractionAcceptance(logger, pausedSnapshot, normalizedSource);
            var pauseRequestAllowedPassed = ValidatePauseRequestRemainsAllowed(logger, pausedSnapshot, normalizedSource);
            var runningReleasesPassed = ValidateRunningReleasesBlockers(logger, normalizedSource);
            var rejectedResumeKeepsBlockersPassed = ValidateRejectedResumeKeepsBlockers(logger, normalizedSource);

            return Task.FromResult(pausedBlockersCreatedPassed
                && inputBlockedPassed
                && interactionBlockedPassed
                && pauseRequestAllowedPassed
                && runningReleasesPassed
                && rejectedResumeKeepsBlockersPassed);
        }

        private static bool ValidatePausedBlockersCreated(
            FrameworkLogger logger,
            PauseSnapshot pausedSnapshot,
            string source)
        {
            var gateSnapshot = PauseGateBlockerPolicy.CreateSnapshotForPauseSnapshot(
                pausedSnapshot,
                source,
                "qa.pause.gate.paused-blockers");

            var passed = gateSnapshot.HasBlockers
                && gateSnapshot.BlockerCount == 2
                && gateSnapshot.CountByScope(GateScope.Input) == 1
                && gateSnapshot.CountByScope(GateScope.Interaction) == 1
                && gateSnapshot.CountByDomain(GateDomain.InputAcceptance) == 1
                && gateSnapshot.CountByDomain(GateDomain.InteractionAcceptance) == 1
                && gateSnapshot.IsBlocked(GateScope.Input, GateDomain.InputAcceptance)
                && gateSnapshot.IsBlocked(GateScope.Interaction, GateDomain.InteractionAcceptance)
                && !gateSnapshot.IsBlocked(GateScope.Pause, GateDomain.PauseRequest);

            LogSnapshotStep(logger, "paused-blockers-created", passed, pausedSnapshot, gateSnapshot);
            return passed;
        }

        private static bool ValidatePausedBlocksInputAcceptance(
            FrameworkLogger logger,
            PauseSnapshot pausedSnapshot,
            string source)
        {
            var gateSnapshot = PauseGateBlockerPolicy.CreateSnapshotForPauseSnapshot(
                pausedSnapshot,
                source,
                "qa.pause.gate.input-blocked");

            var evaluation = gateSnapshot.Evaluate(
                GateScope.Input,
                GateDomain.InputAcceptance,
                default,
                "InputAcceptance",
                source,
                "qa.pause.gate.evaluate-input",
                PauseGateBlockerPolicy.PolicySource);

            var passed = evaluation.IsBlocked
                && evaluation.Decision.Status == GateDecisionStatus.Blocked
                && evaluation.BlockingBlockerCount == 1
                && evaluation.BlockingBlockers[0].BlockerId.Value == PauseGateBlockerPolicy.InputAcceptanceBlockerId
                && evaluation.Decision.PolicySource == PauseGateBlockerPolicy.PolicySource;

            LogEvaluationStep(logger, "paused-blocks-input-acceptance", passed, pausedSnapshot, evaluation, gateSnapshot.BlockerCount);
            return passed;
        }

        private static bool ValidatePausedBlocksInteractionAcceptance(
            FrameworkLogger logger,
            PauseSnapshot pausedSnapshot,
            string source)
        {
            var gateSnapshot = PauseGateBlockerPolicy.CreateSnapshotForPauseSnapshot(
                pausedSnapshot,
                source,
                "qa.pause.gate.interaction-blocked");

            var evaluation = gateSnapshot.Evaluate(
                GateScope.Interaction,
                GateDomain.InteractionAcceptance,
                default,
                "InteractionAcceptance",
                source,
                "qa.pause.gate.evaluate-interaction",
                PauseGateBlockerPolicy.PolicySource);

            var passed = evaluation.IsBlocked
                && evaluation.Decision.Status == GateDecisionStatus.Blocked
                && evaluation.BlockingBlockerCount == 1
                && evaluation.BlockingBlockers[0].BlockerId.Value == PauseGateBlockerPolicy.InteractionAcceptanceBlockerId
                && evaluation.Decision.PolicySource == PauseGateBlockerPolicy.PolicySource;

            LogEvaluationStep(logger, "paused-blocks-interaction-acceptance", passed, pausedSnapshot, evaluation, gateSnapshot.BlockerCount);
            return passed;
        }

        private static bool ValidatePauseRequestRemainsAllowed(
            FrameworkLogger logger,
            PauseSnapshot pausedSnapshot,
            string source)
        {
            var gateSnapshot = PauseGateBlockerPolicy.CreateSnapshotForPauseSnapshot(
                pausedSnapshot,
                source,
                "qa.pause.gate.pause-request-allowed");

            var evaluation = gateSnapshot.Evaluate(
                GateScope.Pause,
                GateDomain.PauseRequest,
                default,
                "PauseRequest",
                source,
                "qa.pause.gate.evaluate-pause-request",
                PauseGateBlockerPolicy.PolicySource);

            var passed = evaluation.IsAllowed
                && evaluation.Decision.Status == GateDecisionStatus.Allowed
                && evaluation.BlockingBlockerCount == 0
                && evaluation.Decision.Scope == GateScope.Pause
                && evaluation.Decision.Domain == GateDomain.PauseRequest;

            LogEvaluationStep(logger, "pause-request-remains-allowed", passed, pausedSnapshot, evaluation, gateSnapshot.BlockerCount);
            return passed;
        }

        private static bool ValidateRunningReleasesBlockers(FrameworkLogger logger, string source)
        {
            var runningSnapshot = PauseSnapshot.FromState(
                PauseState.Running,
                source,
                "qa.pause.gate.running",
                new[] { "qa.pause.gate.running.snapshot" });

            var gateSnapshot = PauseGateBlockerPolicy.CreateSnapshotForPauseSnapshot(
                runningSnapshot,
                source,
                "qa.pause.gate.running-release");

            var evaluation = gateSnapshot.Evaluate(
                GateScope.Input,
                GateDomain.InputAcceptance,
                default,
                "InputAcceptance",
                source,
                "qa.pause.gate.evaluate-running-release",
                PauseGateBlockerPolicy.PolicySource);

            var passed = gateSnapshot.IsEmpty
                && gateSnapshot.BlockerCount == 0
                && evaluation.IsAllowed
                && evaluation.BlockingBlockerCount == 0;

            LogEvaluationStep(logger, "running-releases-blockers", passed, runningSnapshot, evaluation, gateSnapshot.BlockerCount);
            return passed;
        }

        private static bool ValidateRejectedResumeKeepsBlockers(FrameworkLogger logger, string source)
        {
            var request = PauseRequest.Resume(
                "qa.pause.gate.rejected-resume",
                source,
                "qa.pause.gate.rejected-resume");

            var result = PauseResult.RejectedResult(
                request,
                PauseState.Paused,
                "Resume rejected synthetically; Pause state remains paused.",
                new[]
                {
                    PauseIssue.Blocking(
                        "pause-gate-resume-rejected",
                        source,
                        "qa.pause.gate.rejected-resume",
                        "Synthetic resume rejection keeps Pause Gate blockers active.")
                });

            var pausedSnapshot = PauseSnapshot.FromResult(
                result,
                new[] { "qa.pause.gate.rejected-resume.snapshot" });

            var gateSnapshot = PauseGateBlockerPolicy.CreateSnapshotForPauseSnapshot(
                pausedSnapshot,
                source,
                "qa.pause.gate.rejected-resume-keeps-blockers");

            var evaluation = gateSnapshot.Evaluate(
                GateScope.Input,
                GateDomain.InputAcceptance,
                default,
                "InputAcceptance",
                source,
                "qa.pause.gate.evaluate-rejected-resume",
                PauseGateBlockerPolicy.PolicySource);

            var passed = result.Rejected
                && !result.Completed
                && result.IsPaused
                && result.BlockingIssueCount == 1
                && pausedSnapshot.IsPaused
                && gateSnapshot.BlockerCount == 2
                && evaluation.IsBlocked
                && evaluation.BlockingBlockerCount == 1;

            LogEvaluationStep(logger, "rejected-resume-keeps-blockers", passed, pausedSnapshot, evaluation, gateSnapshot.BlockerCount);
            return passed;
        }

        private static void LogSnapshotStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            PauseSnapshot pauseSnapshot,
            GateSnapshot gateSnapshot)
        {
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("pauseState", pauseSnapshot.State.ToString()),
                LogFields.Field("paused", pauseSnapshot.IsPaused),
                LogFields.Field("running", pauseSnapshot.IsRunning),
                LogFields.Field("policySource", PauseGateBlockerPolicy.PolicySource),
                LogFields.Field("blockers", gateSnapshot.BlockerCount),
                LogFields.Field("inputBlockers", gateSnapshot.CountByScope(GateScope.Input)),
                LogFields.Field("interactionBlockers", gateSnapshot.CountByScope(GateScope.Interaction)),
                LogFields.Field("blocksInputAcceptance", gateSnapshot.IsBlocked(GateScope.Input, GateDomain.InputAcceptance)),
                LogFields.Field("blocksInteractionAcceptance", gateSnapshot.IsBlocked(GateScope.Interaction, GateDomain.InteractionAcceptance)),
                LogFields.Field("blocksPauseRequest", gateSnapshot.IsBlocked(GateScope.Pause, GateDomain.PauseRequest)));

            if (passed)
            {
                logger.Info("QA Pause Gate Blocker Smoke step completed.", fields);
                logger.Debug("QA Pause Gate Blocker Smoke snapshot diagnostics.", LogFields.Field("pause", pauseSnapshot.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Pause Gate Blocker Smoke step failed.", fields);
        }

        private static void LogEvaluationStep(
            FrameworkLogger logger,
            string step,
            bool passed,
            PauseSnapshot pauseSnapshot,
            GateEvaluationResult evaluation,
            int snapshotBlockers)
        {
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("pauseState", pauseSnapshot.State.ToString()),
                LogFields.Field("paused", pauseSnapshot.IsPaused),
                LogFields.Field("running", pauseSnapshot.IsRunning),
                LogFields.Field("status", evaluation.Status.ToString()),
                LogFields.Field("scope", evaluation.Scope.ToString()),
                LogFields.Field("domain", evaluation.Domain.ToString()),
                LogFields.Field("policySource", evaluation.Decision.PolicySource),
                LogFields.Field("snapshotBlockers", snapshotBlockers),
                LogFields.Field("blockers", evaluation.BlockingBlockerCount),
                LogFields.Field("facts", evaluation.FactCount));

            if (passed)
            {
                logger.Info("QA Pause Gate Blocker Smoke step completed.", fields);
                logger.Debug("QA Pause Gate Blocker Smoke evaluation diagnostics.", LogFields.Field("details", evaluation.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Pause Gate Blocker Smoke step failed.", fields);
        }
    }
}
#endif

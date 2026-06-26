#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Identity;
using Immersive.Framework.Pause;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F20C Pause diagnostics.
    /// It validates passive Pause primitives without reading input, showing UI, touching Gate,
    /// changing Time.timeScale or executing a runtime Pause request path.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F20C Pause diagnostics smoke; synthetic passive primitives only.")]
    internal static class PauseQaSmokeRunner
    {
        internal const string SmokeName = "Pause Diagnostics Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            var normalizedSource = string.IsNullOrWhiteSpace(source) ? nameof(PauseQaSmokeRunner) : source.Trim();

            var requestPassed = ValidatePauseRequest(logger, normalizedSource);
            var pauseAppliedPassed = ValidatePauseAppliedResult(logger, normalizedSource);
            var resumeAppliedPassed = ValidateResumeAppliedResult(logger, normalizedSource);
            var toggleTargetPassed = ValidateToggleTargetState(logger, normalizedSource);
            var ignoredNoChangePassed = ValidateIgnoredNoChangeResult(logger, normalizedSource);
            var rejectedPassed = ValidateRejectedResult(logger, normalizedSource);
            var snapshotPassed = ValidateSnapshot(logger, normalizedSource);

            return Task.FromResult(requestPassed
                && pauseAppliedPassed
                && resumeAppliedPassed
                && toggleTargetPassed
                && ignoredNoChangePassed
                && rejectedPassed
                && snapshotPassed);
        }

        private static bool ValidatePauseRequest(FrameworkLogger logger, string source)
        {
            var request = PauseRequest.Pause("qa.pause.diagnostics.pause", source, "qa.pause.request");
            var targetState = PauseRequest.ResolveTargetState(request.Kind, PauseState.Running);
            var passed = request.IsValid
                && request.RequestId.Domain == FrameworkIdentityDomain.Pause
                && request.Kind == PauseRequestKind.Pause
                && request.RequestsPause
                && !request.RequestsResume
                && !request.RequestsToggle
                && targetState == PauseState.Paused;

            LogRequestStep(logger, "request", request, PauseState.Running, targetState, passed);
            return passed;
        }

        private static bool ValidatePauseAppliedResult(FrameworkLogger logger, string source)
        {
            var request = PauseRequest.Pause("qa.pause.diagnostics.pause-applied", source, "qa.pause.apply");
            var result = PauseResult.AppliedResult(request, PauseState.Running, PauseState.Paused, "Pause applied synthetically.");
            var passed = result.IsValid
                && result.Applied
                && result.Completed
                && result.StateChanged
                && result.IsPaused
                && !result.IsRunning
                && !result.HasIssues
                && result.BlockingIssueCount == 0
                && result.RequestId.Domain == FrameworkIdentityDomain.Pause;

            LogResultStep(logger, "pause-applied-result", result, passed);
            return passed;
        }

        private static bool ValidateResumeAppliedResult(FrameworkLogger logger, string source)
        {
            var request = PauseRequest.Resume("qa.pause.diagnostics.resume-applied", source, "qa.pause.resume");
            var result = PauseResult.AppliedResult(request, PauseState.Paused, PauseState.Running, "Resume applied synthetically.");
            var passed = result.IsValid
                && result.Applied
                && result.Completed
                && result.StateChanged
                && result.IsRunning
                && !result.IsPaused
                && !result.HasIssues
                && result.BlockingIssueCount == 0
                && result.RequestId.Domain == FrameworkIdentityDomain.Pause;

            LogResultStep(logger, "resume-applied-result", result, passed);
            return passed;
        }

        private static bool ValidateToggleTargetState(FrameworkLogger logger, string source)
        {
            var request = PauseRequest.Toggle("qa.pause.diagnostics.toggle", source, "qa.pause.toggle");
            var runningTarget = PauseRequest.ResolveTargetState(request.Kind, PauseState.Running);
            var pausedTarget = PauseRequest.ResolveTargetState(request.Kind, PauseState.Paused);
            var passed = request.IsValid
                && request.RequestsToggle
                && !request.RequestsPause
                && !request.RequestsResume
                && runningTarget == PauseState.Paused
                && pausedTarget == PauseState.Running;

            LogRequestStep(logger, "toggle-target-state", request, PauseState.Running, runningTarget, passed,
                LogFields.Field("pausedTarget", pausedTarget.ToString()));
            return passed;
        }

        private static bool ValidateIgnoredNoChangeResult(FrameworkLogger logger, string source)
        {
            var request = PauseRequest.Pause("qa.pause.diagnostics.ignored-no-change", source, "qa.pause.idempotent");
            var result = PauseResult.IgnoredNoChangeResult(request, PauseState.Paused, "Pause request ignored because the framework is already paused.");
            var passed = result.IsValid
                && result.IgnoredNoChange
                && result.Completed
                && !result.StateChanged
                && result.IsPaused
                && !result.HasIssues
                && result.BlockingIssueCount == 0;

            LogResultStep(logger, "ignored-no-change-result", result, passed);
            return passed;
        }

        private static bool ValidateRejectedResult(FrameworkLogger logger, string source)
        {
            var request = PauseRequest.Resume("qa.pause.diagnostics.rejected", source, "qa.pause.rejected");
            var issues = new[]
            {
                PauseIssue.Blocking(
                    "pause-policy-blocked",
                    source,
                    "qa.pause.rejected",
                    "Synthetic Pause policy blocker.")
            };

            var result = PauseResult.RejectedResult(request, PauseState.Paused, "Resume rejected synthetically.", issues);
            var passed = result.IsValid
                && result.Rejected
                && !result.Completed
                && !result.StateChanged
                && result.IsPaused
                && result.HasIssues
                && result.BlockingIssueCount == 1
                && result.Issues[0].BlocksRequest;

            LogResultStep(logger, "rejected-result", result, passed);
            return passed;
        }

        private static bool ValidateSnapshot(FrameworkLogger logger, string source)
        {
            var request = PauseRequest.Pause("qa.pause.diagnostics.snapshot", source, "qa.pause.snapshot");
            var result = PauseResult.AppliedResult(request, PauseState.Running, PauseState.Paused, "Pause snapshot source result.");
            var facts = new[]
            {
                "pause.diagnostics.snapshot.created"
            };

            var snapshot = PauseSnapshot.FromResult(result, facts);
            var passed = snapshot.IsValid
                && snapshot.IsPaused
                && !snapshot.IsRunning
                && snapshot.HasLastRequest
                && snapshot.LastRequestId == result.RequestId
                && snapshot.FactCount == 1
                && snapshot.IssueCount == 0
                && snapshot.BlockingIssueCount == 0;

            LogSnapshotStep(logger, "snapshot", snapshot, passed);
            return passed;
        }

        private static void LogRequestStep(
            FrameworkLogger logger,
            string step,
            PauseRequest request,
            PauseState currentState,
            PauseState targetState,
            bool passed,
            params LogField[] additionalFields)
        {
            var baseFields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("request", request.RequestId.StableText),
                LogFields.Field("kind", request.Kind.ToString()),
                LogFields.Field("source", FormatValue(request.Source)),
                LogFields.Field("reason", FormatValue(request.Reason)),
                LogFields.Field("currentState", currentState.ToString()),
                LogFields.Field("targetState", targetState.ToString()),
                LogFields.Field("requestsPause", request.RequestsPause),
                LogFields.Field("requestsResume", request.RequestsResume),
                LogFields.Field("requestsToggle", request.RequestsToggle));

            var fields = AppendFields(baseFields, additionalFields);
            if (passed)
            {
                logger.Info("QA Pause Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Pause Diagnostics Smoke request diagnostics.", LogFields.Field("details", request.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Pause Diagnostics Smoke step failed.", fields);
        }

        private static void LogResultStep(FrameworkLogger logger, string step, PauseResult result, bool passed)
        {
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("request", result.RequestId.StableText),
                LogFields.Field("kind", result.Kind.ToString()),
                LogFields.Field("previousState", result.PreviousState.ToString()),
                LogFields.Field("currentState", result.CurrentState.ToString()),
                LogFields.Field("status", result.Status.ToString()),
                LogFields.Field("completed", result.Completed),
                LogFields.Field("applied", result.Applied),
                LogFields.Field("rejected", result.Rejected),
                LogFields.Field("ignoredNoChange", result.IgnoredNoChange),
                LogFields.Field("failed", result.Failed),
                LogFields.Field("stateChanged", result.StateChanged),
                LogFields.Field("isPaused", result.IsPaused),
                LogFields.Field("isRunning", result.IsRunning),
                LogFields.Field("issues", result.IssueCount),
                LogFields.Field("blockingIssues", result.BlockingIssueCount));

            if (passed)
            {
                logger.Info("QA Pause Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Pause Diagnostics Smoke result diagnostics.", LogFields.Field("details", result.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Pause Diagnostics Smoke step failed.", fields);
        }

        private static void LogSnapshotStep(FrameworkLogger logger, string step, PauseSnapshot snapshot, bool passed)
        {
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("state", snapshot.State.ToString()),
                LogFields.Field("paused", snapshot.IsPaused),
                LogFields.Field("running", snapshot.IsRunning),
                LogFields.Field("lastRequest", snapshot.HasLastRequest ? snapshot.LastRequestId.StableText : "<none>"),
                LogFields.Field("source", FormatValue(snapshot.Source)),
                LogFields.Field("reason", FormatValue(snapshot.Reason)),
                LogFields.Field("issues", snapshot.IssueCount),
                LogFields.Field("blockingIssues", snapshot.BlockingIssueCount),
                LogFields.Field("facts", snapshot.FactCount));

            if (passed)
            {
                logger.Info("QA Pause Diagnostics Smoke step completed.", fields);
                logger.Debug("QA Pause Diagnostics Smoke snapshot diagnostics.", LogFields.Field("details", snapshot.ToDiagnosticString()));
                return;
            }

            logger.Warning("QA Pause Diagnostics Smoke step failed.", fields);
        }

        private static LogField[] AppendFields(LogField[] baseFields, LogField[] additionalFields)
        {
            if (additionalFields == null || additionalFields.Length == 0)
            {
                return baseFields;
            }

            var fields = new LogField[baseFields.Length + additionalFields.Length];
            Array.Copy(baseFields, fields, baseFields.Length);
            Array.Copy(additionalFields, 0, fields, baseFields.Length, additionalFields.Length);
            return fields;
        }

        private static string FormatValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "<none>" : value.Trim();
        }
    }
}
#endif

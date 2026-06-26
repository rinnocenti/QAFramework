#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F17D Gate request-admission diagnostics.
    /// It validates the same request-admission helper used by runtime in-flight guards without creating
    /// real concurrent Route/Activity/Reset operations.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F17D Gate admission diagnostics smoke; synthetic in-flight scenarios only.")]
    internal static class GateAdmissionQaSmokeRunner
    {
        internal const string SmokeName = "Gate Admission Diagnostics Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            var normalizedSource = string.IsNullOrWhiteSpace(source) ? nameof(GateAdmissionQaSmokeRunner) : source.Trim();

            var allowed = ValidateAllowedAdmission(logger, normalizedSource);
            var routeBlocked = ValidateBlockedAdmission(
                logger,
                normalizedSource,
                "route-in-flight",
                "ActivityRequest",
                routeRequestInFlight: true,
                activityRequestInFlight: false,
                cycleResetRequestInFlight: false,
                objectResetRequestInFlight: false,
                expectedBlockerId: "route-request-in-flight");

            var activityBlocked = ValidateBlockedAdmission(
                logger,
                normalizedSource,
                "activity-in-flight",
                "RouteRequest",
                routeRequestInFlight: false,
                activityRequestInFlight: true,
                cycleResetRequestInFlight: false,
                objectResetRequestInFlight: false,
                expectedBlockerId: "activity-request-in-flight");

            var cycleResetBlocked = ValidateBlockedAdmission(
                logger,
                normalizedSource,
                "cycle-reset-in-flight",
                "ObjectResetRequest",
                routeRequestInFlight: false,
                activityRequestInFlight: false,
                cycleResetRequestInFlight: true,
                objectResetRequestInFlight: false,
                expectedBlockerId: "cycle-reset-request-in-flight");

            var objectResetBlocked = ValidateBlockedAdmission(
                logger,
                normalizedSource,
                "object-reset-in-flight",
                "CycleResetRequest",
                routeRequestInFlight: false,
                activityRequestInFlight: false,
                cycleResetRequestInFlight: false,
                objectResetRequestInFlight: true,
                expectedBlockerId: "object-reset-request-in-flight");

            return Task.FromResult(allowed && routeBlocked && activityBlocked && cycleResetBlocked && objectResetBlocked);
        }

        private static bool ValidateAllowedAdmission(FrameworkLogger logger, string source)
        {
            var evaluation = GateRequestAdmission.EvaluateLifecycleRequest(
                "RouteRequest",
                source,
                "qa.gate-admission.allowed",
                routeRequestInFlight: false,
                activityRequestInFlight: false,
                cycleResetRequestInFlight: false,
                objectResetRequestInFlight: false);

            var passed = evaluation.IsAllowed
                && evaluation.Status == GateDecisionStatus.Allowed
                && evaluation.Scope == GateScope.GameFlow
                && evaluation.Domain == GateDomain.LifecycleRequest
                && evaluation.BlockingBlockerCount == 0
                && evaluation.Decision.PolicySource == GateRequestAdmission.PolicySource;

            LogStep(logger, "allowed", evaluation, expectedBlockerId: "<none>", blockerMatched: evaluation.BlockingBlockerCount == 0, passed: passed);
            return passed;
        }

        private static bool ValidateBlockedAdmission(
            FrameworkLogger logger,
            string source,
            string step,
            string subject,
            bool routeRequestInFlight,
            bool activityRequestInFlight,
            bool cycleResetRequestInFlight,
            bool objectResetRequestInFlight,
            string expectedBlockerId)
        {
            var evaluation = GateRequestAdmission.EvaluateLifecycleRequest(
                subject,
                source,
                "qa.gate-admission." + step,
                routeRequestInFlight,
                activityRequestInFlight,
                cycleResetRequestInFlight,
                objectResetRequestInFlight);

            var blockerMatched = ContainsBlocker(evaluation, expectedBlockerId);
            var passed = evaluation.IsBlocked
                && evaluation.Status == GateDecisionStatus.Blocked
                && evaluation.Scope == GateScope.GameFlow
                && evaluation.Domain == GateDomain.LifecycleRequest
                && evaluation.BlockingBlockerCount == 1
                && blockerMatched
                && evaluation.Decision.PolicySource == GateRequestAdmission.PolicySource;

            LogStep(logger, step, evaluation, expectedBlockerId, blockerMatched, passed);
            return passed;
        }

        private static bool ContainsBlocker(GateEvaluationResult evaluation, string expectedBlockerId)
        {
            var blockers = evaluation.BlockingBlockers;
            for (var i = 0; i < blockers.Count; i++)
            {
                if (blockers[i].BlockerId.Value == expectedBlockerId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogStep(
            FrameworkLogger logger,
            string step,
            GateEvaluationResult evaluation,
            string expectedBlockerId,
            bool blockerMatched,
            bool passed)
        {
            var fields = LogFields.Of(
                LogFields.Field("step", step),
                LogFields.Field("passed", passed),
                LogFields.Field("status", evaluation.Status.ToString()),
                LogFields.Field("scope", evaluation.Scope.ToString()),
                LogFields.Field("domain", evaluation.Domain.ToString()),
                LogFields.Field("subject", evaluation.Decision.Subject),
                LogFields.Field("policySource", evaluation.Decision.PolicySource),
                LogFields.Field("blockers", evaluation.BlockingBlockerCount),
                LogFields.Field("facts", evaluation.FactCount),
                LogFields.Field("expectedBlocker", expectedBlockerId),
                LogFields.Field("blockerMatched", blockerMatched));

            if (passed)
            {
                logger.Info("QA Gate Admission Diagnostics Smoke step completed.", fields);
                logger.Debug(
                    "QA Gate Admission Diagnostics Smoke diagnostics.",
                    LogFields.Field("details", evaluation.ToDiagnosticString()));
                return;
            }

            logger.Warning(
                "QA Gate Admission Diagnostics Smoke step failed.",
                fields);
            logger.Debug(
                "QA Gate Admission Diagnostics Smoke failure diagnostics.",
                LogFields.Field("details", evaluation.ToDiagnosticString()));
        }
    }
}
#endif

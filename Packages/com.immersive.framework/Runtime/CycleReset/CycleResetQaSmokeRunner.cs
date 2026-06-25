#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Diagnostics;
using Immersive.Logging.Records;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// Development-only runner for the F11 Cycle Reset runtime-host smoke.
    /// It installs synthetic participants temporarily and validates the canonical runtime request path.
    /// It does not reset Unity objects, reload scenes, release content, restore snapshots, return pooled objects or touch gameplay state.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "Shared QA runner for F11 Cycle Reset runtime-host smoke; no physical reset.")]
    internal static class CycleResetQaSmokeRunner
    {
        internal const string SmokeName = "Cycle Reset Runtime Host Smoke";

        internal static async Task<bool> RunRuntimeHostSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            bool runRouteCycleReset = true,
            bool runActivityCycleReset = true,
            bool logParticipantDetails = false,
            bool emitSmokeEnvelope = false)
        {
            if (logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(CycleResetQaSmokeRunner) : source;

            if (emitSmokeEnvelope)
            {
                logger.Info($"QA Smoke started. name='{SmokeName}'.");
            }

            if (runtimeHost == null)
            {
                logger.Warning($"QA Smoke aborted. name='{SmokeName}'. reason='Framework Runtime Host is missing'.");
                return false;
            }

            if (runtimeHost.State.CurrentRoute == null)
            {
                logger.Warning($"QA Smoke aborted. name='{SmokeName}'. reason='Active Route is missing'.");
                return false;
            }

            var participantSource = new SyntheticCycleResetParticipantSource(source);
            runtimeHost.SetCycleResetParticipantSource(participantSource);

            try
            {
                var completed = true;

                if (runRouteCycleReset)
                {
                    completed &= await RunRouteCycleResetStep(runtimeHost, logger, source, logParticipantDetails);
                }

                if (runActivityCycleReset)
                {
                    if (runtimeHost.State.CurrentActivity == null)
                    {
                        logger.Warning("QA Cycle Reset Smoke step failed. step='activity' reason='Activity Cycle Reset requires an active Activity'.");
                        completed = false;
                    }
                    else
                    {
                        completed &= await RunActivityCycleResetStep(runtimeHost, logger, source, logParticipantDetails);
                    }
                }

                if (emitSmokeEnvelope)
                {
                    if (completed)
                    {
                        logger.Info($"QA Smoke completed. name='{SmokeName}'.");
                    }
                    else
                    {
                        logger.Warning($"QA Smoke aborted. name='{SmokeName}'. reason='Step failed'.");
                    }
                }

                return completed;
            }
            finally
            {
                runtimeHost.SetCycleResetParticipantSource(null);
            }
        }

        private static async Task<bool> RunRouteCycleResetStep(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            bool logParticipantDetails)
        {
            var result = await runtimeHost.RequestRouteCycleResetAsync(source, "qa.cycle-reset.runtime-host.route");
            return ValidateRuntimeHostResult(logger, "route", result, ExpectedParticipantCountFor(result.Request), logParticipantDetails);
        }

        private static async Task<bool> RunActivityCycleResetStep(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source,
            bool logParticipantDetails)
        {
            var result = await runtimeHost.RequestActivityCycleResetAsync(source, "qa.cycle-reset.runtime-host.activity");
            return ValidateRuntimeHostResult(logger, "activity", result, ExpectedParticipantCountFor(result.Request), logParticipantDetails);
        }

        private static bool ValidateRuntimeHostResult(
            FrameworkLogger logger,
            string step,
            CycleResetResult result,
            int expectedParticipantCount,
            bool logParticipantDetails)
        {
            if (!result.Succeeded)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Runtime host request did not succeed'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.Status == CycleResetStatus.SucceededNoParticipants)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Runtime host request completed without participants, but synthetic participant source was installed'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.ParticipantCount != expectedParticipantCount)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Unexpected participant result count' expected='{expectedParticipantCount}' actual='{result.ParticipantCount}'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.SucceededCount != expectedParticipantCount)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Not all synthetic participants succeeded' expected='{expectedParticipantCount}' actual='{result.SucceededCount}'. {result.ToDiagnosticString()}");
                return false;
            }

            if (result.BlockingIssueCount > 0 || result.BlockingFailureCount > 0)
            {
                logger.Warning($"QA Cycle Reset Smoke step failed. step='{step}' reason='Blocking issue or failure was produced'. {result.ToDiagnosticString()}");
                return false;
            }

            logger.Info(
                "QA Cycle Reset Smoke step completed.",
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("scope", result.Request.Scope.ToString()),
                    LogFields.Field("route", result.Request.ActiveRouteName),
                    LogFields.Field("activity", string.IsNullOrWhiteSpace(result.Request.ActiveActivityName) ? "<none>" : result.Request.ActiveActivityName),
                    LogFields.Field("includeActiveActivity", result.Request.IncludesActiveActivity),
                    LogFields.Field("resultStatus", result.Status.ToString()),
                    LogFields.Field("participants", result.ParticipantCount),
                    LogFields.Field("participantSucceeded", result.SucceededCount),
                    LogFields.Field("participantSkipped", result.SkippedCount),
                    LogFields.Field("blockingIssues", result.BlockingIssueCount),
                    LogFields.Field("nonBlockingIssues", result.NonBlockingIssueCount)));

            if (logParticipantDetails)
            {
                logger.Debug(
                    "QA Cycle Reset Smoke diagnostics.",
                    LogFields.Of(
                        LogFields.Field("step", step),
                        LogFields.Field("result", result.ToDiagnosticString())));
            }

            return true;
        }

        private static int ExpectedParticipantCountFor(CycleResetRequest request)
        {
            if (!request.IsValid)
            {
                return 0;
            }

            if (request.IsActivityReset)
            {
                return 2;
            }

            return request.IncludesActiveActivity ? 3 : 2;
        }

        private static List<ICycleResetParticipant> CreateRouteSyntheticParticipants(CycleResetRequest request, string source)
        {
            var participants = new List<ICycleResetParticipant>
            {
                SyntheticCycleResetParticipant.Success(
                    CycleResetParticipantDescriptor.Required(
                        CycleResetParticipantId.From("qa.cycle-reset.route.required"),
                        CycleResetScope.Route,
                        order: 10,
                        displayName: "QA Route Required Cycle Reset Participant",
                        source: source,
                        reason: "qa.cycle-reset.route.required")),
                SyntheticCycleResetParticipant.Success(
                    CycleResetParticipantDescriptor.Optional(
                        CycleResetParticipantId.From("qa.cycle-reset.route.optional"),
                        CycleResetScope.Route,
                        order: 20,
                        displayName: "QA Route Optional Cycle Reset Participant",
                        source: source,
                        reason: "qa.cycle-reset.route.optional"))
            };

            if (request.IncludesActiveActivity)
            {
                participants.Add(
                    SyntheticCycleResetParticipant.Success(
                        CycleResetParticipantDescriptor.Required(
                            CycleResetParticipantId.From("qa.cycle-reset.route.active-activity.required"),
                            CycleResetScope.Activity,
                            order: 10,
                            displayName: "QA Route Included Activity Cycle Reset Participant",
                            source: source,
                            reason: "qa.cycle-reset.route.active-activity.required")));
            }

            return participants;
        }

        private static List<ICycleResetParticipant> CreateActivitySyntheticParticipants(string source)
        {
            return new List<ICycleResetParticipant>
            {
                SyntheticCycleResetParticipant.Success(
                    CycleResetParticipantDescriptor.Required(
                        CycleResetParticipantId.From("qa.cycle-reset.activity.required"),
                        CycleResetScope.Activity,
                        order: 10,
                        displayName: "QA Activity Required Cycle Reset Participant",
                        source: source,
                        reason: "qa.cycle-reset.activity.required")),
                SyntheticCycleResetParticipant.Success(
                    CycleResetParticipantDescriptor.Optional(
                        CycleResetParticipantId.From("qa.cycle-reset.activity.optional"),
                        CycleResetScope.Activity,
                        order: 20,
                        displayName: "QA Activity Optional Cycle Reset Participant",
                        source: source,
                        reason: "qa.cycle-reset.activity.optional"))
            };
        }

        private sealed class SyntheticCycleResetParticipantSource : ICycleResetParticipantSource
        {
            private readonly string source;

            public SyntheticCycleResetParticipantSource(string source)
            {
                this.source = source;
            }

            public IReadOnlyList<ICycleResetParticipant> ResolveCycleResetParticipants(CycleResetRequest request)
            {
                if (!request.IsValid)
                {
                    return Array.Empty<ICycleResetParticipant>();
                }

                if (request.IsRouteReset)
                {
                    return CreateRouteSyntheticParticipants(request, source);
                }

                if (request.IsActivityReset)
                {
                    return CreateActivitySyntheticParticipants(source);
                }

                return Array.Empty<ICycleResetParticipant>();
            }
        }

        private sealed class SyntheticCycleResetParticipant : ICycleResetParticipant
        {
            private readonly CycleResetParticipantDescriptor descriptor;
            private readonly SyntheticCycleResetParticipantMode mode;

            private SyntheticCycleResetParticipant(
                CycleResetParticipantDescriptor descriptor,
                SyntheticCycleResetParticipantMode mode)
            {
                this.descriptor = descriptor;
                this.mode = mode;
            }

            public CycleResetParticipantDescriptor GetCycleResetDescriptor()
            {
                return descriptor;
            }

            public CycleResetParticipantResult ResetCycle(CycleResetContext context)
            {
                switch (mode)
                {
                    case SyntheticCycleResetParticipantMode.Success:
                        return CycleResetParticipantResult.Success(
                            context,
                            descriptor.Source,
                            descriptor.Reason,
                            "Synthetic Cycle Reset participant succeeded.");

                    case SyntheticCycleResetParticipantMode.SkippedOptional:
                        return CycleResetParticipantResult.Skipped(
                            context,
                            CycleResetParticipantResultStatus.SkippedOptional,
                            descriptor.Source,
                            descriptor.Reason,
                            "Synthetic Cycle Reset participant skipped as optional.");

                    case SyntheticCycleResetParticipantMode.Failure:
                        return CycleResetParticipantResult.Failure(
                            context,
                            1,
                            descriptor.Source,
                            descriptor.Reason,
                            "Synthetic Cycle Reset participant failed.");

                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported synthetic Cycle Reset participant mode.");
                }
            }

            public static SyntheticCycleResetParticipant Success(CycleResetParticipantDescriptor descriptor)
            {
                return new SyntheticCycleResetParticipant(descriptor, SyntheticCycleResetParticipantMode.Success);
            }

            public static SyntheticCycleResetParticipant SkippedOptional(CycleResetParticipantDescriptor descriptor)
            {
                return new SyntheticCycleResetParticipant(descriptor, SyntheticCycleResetParticipantMode.SkippedOptional);
            }

            public static SyntheticCycleResetParticipant Failure(CycleResetParticipantDescriptor descriptor)
            {
                return new SyntheticCycleResetParticipant(descriptor, SyntheticCycleResetParticipantMode.Failure);
            }
        }

        private enum SyntheticCycleResetParticipantMode
        {
            Success = 0,
            SkippedOptional = 1,
            Failure = 2
        }
    }
}
#endif

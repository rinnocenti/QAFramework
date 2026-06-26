#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Logging.Records;

namespace Immersive.Framework.ObjectReset
{
    /// <summary>
    /// Development-only runner for F14 Object Reset foundation smokes.
    /// It uses synthetic ObjectEntry snapshots and performs no Unity object reset side effects.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F14 Object Reset QA smoke runner; no physical reset.")]
    internal static class ObjectResetQaSmokeRunner
    {
        internal const string TargetResolutionSmokeName = "Object Reset Target Resolution Smoke";

        internal const string ParticipantContractSmokeName = "Object Reset Participant Contract Smoke";

        internal static Task<bool> RunTargetResolutionSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            try
            {
                var activeRouteOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Route,
                    new FrameworkIdentityValue("qa.route.active"));
                var activeActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.active"));
                var staleActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.stale"));

                var routeEntry = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-reset.route.target"),
                    ObjectEntryScope.Route,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Route Target",
                    activeRouteOwner);
                var activityEntry = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-reset.activity.target"),
                    ObjectEntryScope.Activity,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Activity Target",
                    activeActivityOwner);

                var snapshot = CreateSnapshot(source, routeEntry, activityEntry);

                var validRequest = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(activityEntry),
                    source,
                    "qa.object-reset.target-resolution.valid");
                var validResult = ObjectResetTargetResolver.ResolveTarget(snapshot, validRequest);

                var missingRequest = ObjectResetRequest.ForTarget(
                    new ObjectResetTarget(
                        ObjectEntryId.From("qa.object-reset.missing"),
                        ObjectEntryScope.Activity,
                        activeActivityOwner),
                    source,
                    "qa.object-reset.target-resolution.missing");
                var missingResult = ObjectResetTargetResolver.ResolveTarget(snapshot, missingRequest);

                var staleRequest = ObjectResetRequest.ForTarget(
                    new ObjectResetTarget(
                        activityEntry.Id,
                        ObjectEntryScope.Activity,
                        staleActivityOwner),
                    source,
                    "qa.object-reset.target-resolution.foreign-or-stale");
                var staleResult = ObjectResetTargetResolver.ResolveTarget(snapshot, staleRequest);

                var invalidResult = ObjectResetTargetResolver.ResolveTarget(snapshot, default);

                var validResolved = validResult.Succeeded
                    && validResult.HasResolvedTarget
                    && validResult.ResolvedTarget.Id == activityEntry.Id
                    && validResult.BlockingIssueCount == 0;
                var missingRejected = missingResult.Status == ObjectResetResultStatus.RejectedTargetNotFound
                    && missingResult.BlockingIssueCount == 1
                    && !missingResult.HasResolvedTarget;
                var staleRejected = staleResult.Status == ObjectResetResultStatus.RejectedForeignTarget
                    && staleResult.BlockingIssueCount == 1
                    && !staleResult.HasResolvedTarget;
                var invalidRejected = invalidResult.Status == ObjectResetResultStatus.RejectedInvalidRequest
                    && invalidResult.BlockingIssueCount == 1
                    && !invalidResult.HasResolvedTarget;

                if (!validResolved || !missingRejected || !staleRejected || !invalidRejected)
                {
                    logger.Warning(
                        "QA Object Reset Target Resolution Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "target-resolution"),
                            LogFields.Field("validResolved", validResolved),
                            LogFields.Field("missingRejected", missingRejected),
                            LogFields.Field("foreignOrStaleRejected", staleRejected),
                            LogFields.Field("invalidRejected", invalidRejected),
                            LogFields.Field("validStatus", validResult.Status.ToString()),
                            LogFields.Field("missingStatus", missingResult.Status.ToString()),
                            LogFields.Field("foreignOrStaleStatus", staleResult.Status.ToString()),
                            LogFields.Field("invalidStatus", invalidResult.Status.ToString())));
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Reset Target Resolution Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "target-resolution"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot.IsAvailable),
                        LogFields.Field("objectEntries", snapshot.Count),
                        LogFields.Field("validResolved", validResolved),
                        LogFields.Field("missingRejected", missingRejected),
                        LogFields.Field("foreignOrStaleRejected", staleRejected),
                        LogFields.Field("invalidRejected", invalidRejected),
                        LogFields.Field("validStatus", validResult.Status.ToString()),
                        LogFields.Field("missingStatus", missingResult.Status.ToString()),
                        LogFields.Field("foreignOrStaleStatus", staleResult.Status.ToString()),
                        LogFields.Field("invalidStatus", invalidResult.Status.ToString()),
                        LogFields.Field("validBlockingIssues", validResult.BlockingIssueCount),
                        LogFields.Field("missingBlockingIssues", missingResult.BlockingIssueCount),
                        LogFields.Field("foreignOrStaleBlockingIssues", staleResult.BlockingIssueCount),
                        LogFields.Field("invalidBlockingIssues", invalidResult.BlockingIssueCount),
                        LogFields.Field("summary", validResult.ToDiagnosticString())));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Target Resolution Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "target-resolution"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
        }


        internal static Task<bool> RunParticipantContractSmokeAsync(
            FrameworkLogger logger,
            string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            try
            {
                var activeActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.active"));
                var staleActivityOwner = new FrameworkIdentityKey(
                    FrameworkIdentityDomain.Activity,
                    new FrameworkIdentityValue("qa.activity.stale"));

                var activityEntry = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-reset.participant.target"),
                    ObjectEntryScope.Activity,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Participant Target",
                    activeActivityOwner);

                var snapshot = CreateSnapshot(source, activityEntry);
                var request = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(activityEntry),
                    source,
                    "qa.object-reset.participant-contract.request");
                var resolution = ObjectResetTargetResolver.ResolveTarget(snapshot, request);

                if (!resolution.Succeeded || !resolution.HasResolvedTarget)
                {
                    logger.Warning(
                        "QA Object Reset Participant Contract Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "participant-contract"),
                            LogFields.Field("reason", "target-resolution-failed"),
                            LogFields.Field("targetStatus", resolution.Status.ToString()),
                            LogFields.Field("targetBlockingIssues", resolution.BlockingIssueCount)));
                    return Task.FromResult(false);
                }

                var requiredParticipant = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.participant.required"),
                        request.Target,
                        20,
                        "QA Required Object Reset Participant",
                        source,
                        "qa.object-reset.participant-contract.required"),
                    SyntheticObjectResetParticipantMode.Success);
                var optionalParticipant = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.participant.optional"),
                        request.Target,
                        10,
                        "QA Optional Object Reset Participant",
                        source,
                        "qa.object-reset.participant-contract.optional"),
                    SyntheticObjectResetParticipantMode.SkippedOptional);
                var failureParticipant = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.participant.required-failure"),
                        request.Target,
                        30,
                        "QA Required Object Reset Failure Participant",
                        source,
                        "qa.object-reset.participant-contract.failure"),
                    SyntheticObjectResetParticipantMode.BlockingFailure);
                var foreignParticipant = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.participant.foreign"),
                        new ObjectResetTarget(
                            activityEntry.Id,
                            ObjectEntryScope.Activity,
                            staleActivityOwner),
                        40,
                        "QA Foreign Object Reset Participant",
                        source,
                        "qa.object-reset.participant-contract.foreign"),
                    SyntheticObjectResetParticipantMode.Success);

                var participantSource = new SyntheticObjectResetParticipantSource(requiredParticipant, optionalParticipant);
                var resolvedParticipants = participantSource.ResolveObjectResetParticipants(request, resolution.ResolvedTarget);

                var requiredDescriptor = requiredParticipant.GetObjectResetDescriptor();
                var optionalDescriptor = optionalParticipant.GetObjectResetDescriptor();
                var failureDescriptor = failureParticipant.GetObjectResetDescriptor();
                var foreignDescriptor = foreignParticipant.GetObjectResetDescriptor();

                var requiredEntry = new ObjectResetParticipantEntry(requiredParticipant, requiredDescriptor, 0);
                var optionalEntry = new ObjectResetParticipantEntry(optionalParticipant, optionalDescriptor, 1);

                var requiredContext = new ObjectResetContext(request, resolution.ResolvedTarget, requiredDescriptor);
                var optionalContext = new ObjectResetContext(request, resolution.ResolvedTarget, optionalDescriptor);
                var failureContext = new ObjectResetContext(request, resolution.ResolvedTarget, failureDescriptor);

                var successResult = requiredParticipant.ResetObject(requiredContext);
                var skippedResult = optionalParticipant.ResetObject(optionalContext);
                var failureResult = failureParticipant.ResetObject(failureContext);

                var sourceResolved = resolvedParticipants.Count == 2
                    && ReferenceEquals(resolvedParticipants[0], requiredParticipant)
                    && ReferenceEquals(resolvedParticipants[1], optionalParticipant);
                var requiredSupportsTarget = requiredDescriptor.SupportsResolvedTarget(request, resolution.ResolvedTarget);
                var optionalSupportsTarget = optionalDescriptor.SupportsResolvedTarget(request, resolution.ResolvedTarget);
                var foreignRejected = !foreignDescriptor.SupportsResolvedTarget(request, resolution.ResolvedTarget);
                var entriesValid = requiredEntry.IsValid
                    && optionalEntry.IsValid
                    && requiredEntry.IsRequired
                    && optionalEntry.IsOptional;
                var contextsValid = requiredContext.IsValid
                    && optionalContext.IsValid
                    && failureContext.IsValid;
                var successValid = successResult.Succeeded
                    && !successResult.BlocksReset
                    && successResult.IssueCount == 0;
                var skippedValid = skippedResult.WasSkipped
                    && !skippedResult.BlocksReset
                    && skippedResult.IssueCount == 0;
                var failureValid = failureResult.Failed
                    && failureResult.BlocksReset
                    && failureResult.IssueCount == 1;

                if (!sourceResolved
                    || !requiredSupportsTarget
                    || !optionalSupportsTarget
                    || !foreignRejected
                    || !entriesValid
                    || !contextsValid
                    || !successValid
                    || !skippedValid
                    || !failureValid)
                {
                    logger.Warning(
                        "QA Object Reset Participant Contract Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "participant-contract"),
                            LogFields.Field("sourceResolved", sourceResolved),
                            LogFields.Field("requiredSupportsTarget", requiredSupportsTarget),
                            LogFields.Field("optionalSupportsTarget", optionalSupportsTarget),
                            LogFields.Field("foreignRejected", foreignRejected),
                            LogFields.Field("entriesValid", entriesValid),
                            LogFields.Field("contextsValid", contextsValid),
                            LogFields.Field("successValid", successValid),
                            LogFields.Field("skippedValid", skippedValid),
                            LogFields.Field("failureValid", failureValid)));
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Reset Participant Contract Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "participant-contract"),
                        LogFields.Field("source", source),
                        LogFields.Field("targetResolved", resolution.Succeeded),
                        LogFields.Field("sourceParticipants", resolvedParticipants.Count),
                        LogFields.Field("sourceResolved", sourceResolved),
                        LogFields.Field("requiredSupportsTarget", requiredSupportsTarget),
                        LogFields.Field("optionalSupportsTarget", optionalSupportsTarget),
                        LogFields.Field("foreignRejected", foreignRejected),
                        LogFields.Field("entriesValid", entriesValid),
                        LogFields.Field("contextsValid", contextsValid),
                        LogFields.Field("successStatus", successResult.Status.ToString()),
                        LogFields.Field("skippedStatus", skippedResult.Status.ToString()),
                        LogFields.Field("failureStatus", failureResult.Status.ToString()),
                        LogFields.Field("successBlocksReset", successResult.BlocksReset),
                        LogFields.Field("skippedBlocksReset", skippedResult.BlocksReset),
                        LogFields.Field("failureBlocksReset", failureResult.BlocksReset),
                        LogFields.Field("successIssues", successResult.IssueCount),
                        LogFields.Field("skippedIssues", skippedResult.IssueCount),
                        LogFields.Field("failureIssues", failureResult.IssueCount),
                        LogFields.Field("summary", successResult.ToDiagnosticString())));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Participant Contract Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "participant-contract"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
        }

        private static ObjectEntryRuntimeContextSnapshot CreateSnapshot(
            string source,
            params ObjectEntryDescriptor[] descriptors)
        {
            var set = new ObjectEntrySet(descriptors);
            var result = new ObjectEntryDeclarationSourceResult(
                set,
                ObjectEntryResultStatus.Accepted,
                declarationCount: set.Count,
                candidateDescriptorCount: set.Count,
                acceptedDeclarationCount: set.Count,
                rejectedDeclarationCount: 0,
                filteredDeclarationCount: 0);
            return result.ToRuntimeContextSnapshot($"{nameof(ObjectResetQaSmokeRunner)}:{source}");
        }


        private sealed class SyntheticObjectResetParticipant : IObjectResetParticipant
        {
            private readonly ObjectResetParticipantDescriptor descriptor;
            private readonly SyntheticObjectResetParticipantMode mode;

            public SyntheticObjectResetParticipant(
                ObjectResetParticipantDescriptor descriptor,
                SyntheticObjectResetParticipantMode mode)
            {
                this.descriptor = descriptor;
                this.mode = mode;
            }

            public ObjectResetParticipantDescriptor GetObjectResetDescriptor()
            {
                return descriptor;
            }

            public ObjectResetParticipantResult ResetObject(ObjectResetContext context)
            {
                switch (mode)
                {
                    case SyntheticObjectResetParticipantMode.Success:
                        return ObjectResetParticipantResult.Success(
                            context,
                            descriptor.Source,
                            descriptor.Reason,
                            "Synthetic Object Reset participant succeeded.");
                    case SyntheticObjectResetParticipantMode.SkippedOptional:
                        return ObjectResetParticipantResult.Skipped(
                            context,
                            ObjectResetParticipantResultStatus.SkippedOptional,
                            descriptor.Source,
                            descriptor.Reason,
                            "Synthetic Object Reset participant skipped as optional.");
                    case SyntheticObjectResetParticipantMode.BlockingFailure:
                        return ObjectResetParticipantResult.BlockingFailure(
                            context,
                            1,
                            descriptor.Source,
                            descriptor.Reason,
                            "Synthetic Object Reset participant failed as required.");
                    default:
                        return ObjectResetParticipantResult.Failure(
                            context,
                            1,
                            descriptor.Source,
                            descriptor.Reason,
                            "Synthetic Object Reset participant failed.");
                }
            }
        }

        private sealed class SyntheticObjectResetParticipantSource : IObjectResetParticipantSource
        {
            private readonly IReadOnlyList<IObjectResetParticipant> participants;

            public SyntheticObjectResetParticipantSource(params IObjectResetParticipant[] participants)
            {
                this.participants = participants ?? Array.Empty<IObjectResetParticipant>();
            }

            public IReadOnlyList<IObjectResetParticipant> ResolveObjectResetParticipants(
                ObjectResetRequest request,
                ObjectEntryDescriptor resolvedTarget)
            {
                return participants;
            }
        }

        private enum SyntheticObjectResetParticipantMode
        {
            Success = 0,
            SkippedOptional = 1,
            BlockingFailure = 2
        }
    }
}
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Diagnostics;
using Immersive.Framework.Identity;
using Immersive.Framework.GameFlow;
using Immersive.Framework.ObjectEntry;
using Immersive.Logging.Records;
using UnityEngine;

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

        internal const string RuntimeExecutorSmokeName = "Object Reset Runtime Executor Smoke";

        internal const string RuntimeHostIntegrationSmokeName = "Object Reset Runtime Host Integration Smoke";

        internal const string TriggerSmokeName = "Object Reset Trigger Smoke";

        internal const string BridgeSmokeName = "Object Reset Bridge Smoke";

        internal const string FoundationClosureSmokeName = "Object Reset Foundation Closure Smoke";

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



        internal static Task<bool> RunRuntimeExecutorSmokeAsync(
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
                var activityEntry = new ObjectEntryDescriptor(
                    ObjectEntryId.From("qa.object-reset.runtime.target"),
                    ObjectEntryScope.Activity,
                    ObjectEntrySourceKind.SceneAuthored,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Runtime Target",
                    activeActivityOwner);

                var snapshot = CreateSnapshot(source, activityEntry);
                var request = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(activityEntry),
                    source,
                    "qa.object-reset.runtime-executor.request");
                var runtime = new ObjectResetRuntime();

                var requiredSuccess = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.required"),
                        request.Target,
                        20,
                        "QA Runtime Required Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.required"),
                    SyntheticObjectResetParticipantMode.Success);
                var optionalSkipped = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.optional"),
                        request.Target,
                        10,
                        "QA Runtime Optional Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.optional"),
                    SyntheticObjectResetParticipantMode.SkippedOptional);
                var requiredFailure = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.required-failure"),
                        request.Target,
                        30,
                        "QA Runtime Required Failure Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.required-failure"),
                    SyntheticObjectResetParticipantMode.BlockingFailure);
                var optionalFailure = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.runtime.optional-failure"),
                        request.Target,
                        40,
                        "QA Runtime Optional Failure Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-executor.optional-failure"),
                    SyntheticObjectResetParticipantMode.NonBlockingFailure);

                var successSource = new SyntheticObjectResetParticipantSource(requiredSuccess, optionalSkipped);
                var plan = runtime.BuildPlan(snapshot, request, successSource);
                var successResult = runtime.Execute(snapshot, request, successSource);
                var noParticipantsResult = runtime.Execute(snapshot, request, new SyntheticObjectResetParticipantSource());
                var requiredFailureResult = runtime.Execute(snapshot, request, new SyntheticObjectResetParticipantSource(requiredFailure));
                var optionalFailureResult = runtime.Execute(snapshot, request, new SyntheticObjectResetParticipantSource(optionalFailure));

                var planOrdered = plan.ParticipantCount == 2
                    && plan.Participants[0].ParticipantId == optionalSkipped.GetObjectResetDescriptor().ParticipantId
                    && plan.Participants[1].ParticipantId == requiredSuccess.GetObjectResetDescriptor().ParticipantId;
                var successAggregated = successResult.Status == ObjectResetResultStatus.Succeeded
                    && successResult.ParticipantCount == 2
                    && successResult.ParticipantSucceededCount == 1
                    && successResult.ParticipantSkippedCount == 1
                    && successResult.ParticipantFailedCount == 0
                    && successResult.BlockingIssueCount == 0;
                var noParticipantsSucceeded = noParticipantsResult.Status == ObjectResetResultStatus.SucceededNoParticipants
                    && noParticipantsResult.ParticipantCount == 0
                    && noParticipantsResult.BlockingIssueCount == 0;
                var requiredFailureBlocked = requiredFailureResult.Status == ObjectResetResultStatus.Failed
                    && requiredFailureResult.ParticipantFailedCount == 1
                    && requiredFailureResult.ParticipantBlockingFailureCount == 1
                    && requiredFailureResult.BlockingIssueCount == 1;
                var optionalFailureWarned = optionalFailureResult.Status == ObjectResetResultStatus.CompletedWithWarnings
                    && optionalFailureResult.ParticipantFailedCount == 1
                    && optionalFailureResult.ParticipantBlockingFailureCount == 0
                    && optionalFailureResult.BlockingIssueCount == 0
                    && optionalFailureResult.NonBlockingIssueCount == 1;

                if (!planOrdered
                    || !successAggregated
                    || !noParticipantsSucceeded
                    || !requiredFailureBlocked
                    || !optionalFailureWarned)
                {
                    logger.Warning(
                        "QA Object Reset Runtime Executor Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "runtime-executor"),
                            LogFields.Field("planOrdered", planOrdered),
                            LogFields.Field("successAggregated", successAggregated),
                            LogFields.Field("noParticipantsSucceeded", noParticipantsSucceeded),
                            LogFields.Field("requiredFailureBlocked", requiredFailureBlocked),
                            LogFields.Field("optionalFailureWarned", optionalFailureWarned),
                            LogFields.Field("successStatus", successResult.Status.ToString()),
                            LogFields.Field("noParticipantsStatus", noParticipantsResult.Status.ToString()),
                            LogFields.Field("requiredFailureStatus", requiredFailureResult.Status.ToString()),
                            LogFields.Field("optionalFailureStatus", optionalFailureResult.Status.ToString())));
                    return Task.FromResult(false);
                }

                logger.Info(
                    "QA Object Reset Runtime Executor Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-executor"),
                        LogFields.Field("source", source),
                        LogFields.Field("targetResolved", successResult.HasResolvedTarget),
                        LogFields.Field("planParticipants", plan.ParticipantCount),
                        LogFields.Field("planRequired", plan.RequiredParticipantCount),
                        LogFields.Field("planOptional", plan.OptionalParticipantCount),
                        LogFields.Field("planOrdered", planOrdered),
                        LogFields.Field("successStatus", successResult.Status.ToString()),
                        LogFields.Field("successParticipants", successResult.ParticipantCount),
                        LogFields.Field("successParticipantSucceeded", successResult.ParticipantSucceededCount),
                        LogFields.Field("successParticipantSkipped", successResult.ParticipantSkippedCount),
                        LogFields.Field("successParticipantFailed", successResult.ParticipantFailedCount),
                        LogFields.Field("noParticipantsStatus", noParticipantsResult.Status.ToString()),
                        LogFields.Field("requiredFailureStatus", requiredFailureResult.Status.ToString()),
                        LogFields.Field("requiredFailureBlocks", requiredFailureResult.BlockingIssueCount),
                        LogFields.Field("optionalFailureStatus", optionalFailureResult.Status.ToString()),
                        LogFields.Field("optionalFailureWarnings", optionalFailureResult.NonBlockingIssueCount),
                        LogFields.Field("blockingIssues", successResult.BlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", successResult.NonBlockingIssueCount),
                        LogFields.Field("summary", successResult.ToDiagnosticString())));

                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Runtime Executor Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-executor"),
                        LogFields.Field("reason", exception.Message)));
                return Task.FromResult(false);
            }
        }

        internal static async Task<bool> RunRuntimeHostIntegrationSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Runtime Host Integration Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "runtime-host-integration"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                var objectEntryId = $"qa.object-reset.host.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetRuntimeHost_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Runtime Host Target",
                    qaActivityOwner: activeActivity);

                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-host-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                if (snapshot == null || !snapshot.TryGet(id, out var descriptor))
                {
                    logger.Warning(
                        "QA Object Reset Runtime Host Integration Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "runtime-host-integration"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot != null && snapshot.IsAvailable),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                var request = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    source,
                    "qa.object-reset.runtime-host.success");
                var requiredSuccess = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.host.required"),
                        request.Target,
                        20,
                        "QA Runtime Host Required Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-host.required"),
                    SyntheticObjectResetParticipantMode.Success);
                var optionalSkipped = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.host.optional"),
                        request.Target,
                        10,
                        "QA Runtime Host Optional Object Reset Participant",
                        source,
                        "qa.object-reset.runtime-host.optional"),
                    SyntheticObjectResetParticipantMode.SkippedOptional);

                runtimeHost.SetObjectResetParticipantSource(new SyntheticObjectResetParticipantSource(requiredSuccess, optionalSkipped));
                var successResult = await runtimeHost.RequestObjectResetAsync(request);

                runtimeHost.SetObjectResetParticipantSource(null);
                var noParticipantsResult = await runtimeHost.RequestObjectResetAsync(ObjectResetRequest.ForTarget(
                    request.Target,
                    source,
                    "qa.object-reset.runtime-host.no-participants"));

                var missingResult = await runtimeHost.RequestObjectResetAsync(ObjectResetRequest.ForTarget(
                    new ObjectResetTarget(
                        ObjectEntryId.From($"{objectEntryId}.missing"),
                        ObjectEntryScope.Activity,
                        descriptor.OwnerIdentity.Value),
                    source,
                    "qa.object-reset.runtime-host.missing"));

                var hostSnapshotAvailable = runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var hostSnapshot)
                    && hostSnapshot != null
                    && hostSnapshot.IsAvailable;
                var hostResolvedTarget = successResult.HasResolvedTarget
                    && successResult.ResolvedTarget.Id == id;
                var successValid = successResult.Status == ObjectResetResultStatus.Succeeded
                    && successResult.ParticipantCount == 2
                    && successResult.ParticipantSucceededCount == 1
                    && successResult.ParticipantSkippedCount == 1
                    && successResult.BlockingIssueCount == 0;
                var noParticipantsValid = noParticipantsResult.Status == ObjectResetResultStatus.SucceededNoParticipants
                    && noParticipantsResult.ParticipantCount == 0
                    && noParticipantsResult.BlockingIssueCount == 0;
                var missingRejected = missingResult.Status == ObjectResetResultStatus.RejectedTargetNotFound
                    && missingResult.BlockingIssueCount == 1;

                if (!hostSnapshotAvailable
                    || !hostResolvedTarget
                    || !successValid
                    || !noParticipantsValid
                    || !missingRejected)
                {
                    logger.Warning(
                        "QA Object Reset Runtime Host Integration Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "runtime-host-integration"),
                            LogFields.Field("hostSnapshotAvailable", hostSnapshotAvailable),
                            LogFields.Field("hostResolvedTarget", hostResolvedTarget),
                            LogFields.Field("successValid", successValid),
                            LogFields.Field("noParticipantsValid", noParticipantsValid),
                            LogFields.Field("missingRejected", missingRejected),
                            LogFields.Field("successStatus", successResult.Status.ToString()),
                            LogFields.Field("noParticipantsStatus", noParticipantsResult.Status.ToString()),
                            LogFields.Field("missingStatus", missingResult.Status.ToString())));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Runtime Host Integration Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-host-integration"),
                        LogFields.Field("source", source),
                        LogFields.Field("hostSnapshotAvailable", hostSnapshotAvailable),
                        LogFields.Field("snapshotSource", hostSnapshot != null ? hostSnapshot.Source : "<none>"),
                        LogFields.Field("snapshotRevision", runtimeHost.ObjectEntryRuntimeContextRevision),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("hostResolvedTarget", hostResolvedTarget),
                        LogFields.Field("successStatus", successResult.Status.ToString()),
                        LogFields.Field("successParticipants", successResult.ParticipantCount),
                        LogFields.Field("successParticipantSucceeded", successResult.ParticipantSucceededCount),
                        LogFields.Field("successParticipantSkipped", successResult.ParticipantSkippedCount),
                        LogFields.Field("successBlockingIssues", successResult.BlockingIssueCount),
                        LogFields.Field("noParticipantsStatus", noParticipantsResult.Status.ToString()),
                        LogFields.Field("missingStatus", missingResult.Status.ToString()),
                        LogFields.Field("missingBlockingIssues", missingResult.BlockingIssueCount),
                        LogFields.Field("summary", successResult.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Runtime Host Integration Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "runtime-host-integration"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-host-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunTriggerSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Trigger Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-trigger"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                var objectEntryId = $"qa.object-reset.trigger.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetTrigger_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Trigger Target",
                    qaActivityOwner: activeActivity);

                var trigger = qaObject.AddComponent<ObjectResetTrigger>();
                trigger.ConfigureForQa(
                    declaration,
                    string.Empty,
                    "qa.object-reset.trigger",
                    qaAllowNoParticipants: true);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-trigger-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                var targetCollected = snapshot != null && snapshot.TryGet(id, out var descriptor);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Trigger Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-trigger"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot != null && snapshot.IsAvailable),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                trigger.RequestObjectReset();
                var completed = await WaitForObjectResetTriggerAsync(trigger, 32);

                var requestCompleted = completed
                    && !trigger.IsRequestInFlight
                    && trigger.LastEventPhase == FlowRequestEventPhase.Completed;
                var resultSucceededNoParticipants = trigger.HasLastResult
                    && trigger.LastResultStatus == ObjectResetResultStatus.SucceededNoParticipants
                    && trigger.LastResultSucceededNoParticipants;
                var outcomeSucceeded = trigger.LastRequestSucceeded;
                var participantsValid = trigger.LastParticipantCount == 0
                    && trigger.LastSucceededParticipantCount == 0
                    && trigger.LastSkippedParticipantCount == 0
                    && trigger.LastFailedParticipantCount == 0;
                var issuesValid = trigger.LastBlockingIssueCount == 0
                    && trigger.LastNonBlockingIssueCount == 0;
                var idResolved = string.Equals(trigger.ResolvedAuthoringObjectEntryId, objectEntryId, StringComparison.Ordinal);

                if (!requestCompleted
                    || !targetCollected
                    || !resultSucceededNoParticipants
                    || !outcomeSucceeded
                    || !participantsValid
                    || !issuesValid
                    || !idResolved)
                {
                    logger.Warning(
                        "QA Object Reset Trigger Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-trigger"),
                            LogFields.Field("requestCompleted", requestCompleted),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("resultSucceededNoParticipants", resultSucceededNoParticipants),
                            LogFields.Field("outcomeSucceeded", outcomeSucceeded),
                            LogFields.Field("participantsValid", participantsValid),
                            LogFields.Field("issuesValid", issuesValid),
                            LogFields.Field("idResolved", idResolved),
                            LogFields.Field("lastStatus", trigger.LastResultStatus.ToString()),
                            LogFields.Field("lastOutcome", trigger.LastOutcome.ToString())));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Trigger Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "object-reset-trigger"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot != null && snapshot.IsAvailable),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("requestCompleted", requestCompleted),
                        LogFields.Field("lastOutcome", trigger.LastOutcome.ToString()),
                        LogFields.Field("resultStatus", trigger.LastResultStatus.ToString()),
                        LogFields.Field("succeededNoParticipants", trigger.LastResultSucceededNoParticipants),
                        LogFields.Field("participants", trigger.LastParticipantCount),
                        LogFields.Field("participantSucceeded", trigger.LastSucceededParticipantCount),
                        LogFields.Field("participantSkipped", trigger.LastSkippedParticipantCount),
                        LogFields.Field("participantFailed", trigger.LastFailedParticipantCount),
                        LogFields.Field("blockingIssues", trigger.LastBlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", trigger.LastNonBlockingIssueCount),
                        LogFields.Field("summary", trigger.LastResultSummary)));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Trigger Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "object-reset-trigger"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-trigger-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunBridgeSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Bridge Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-bridge"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                var objectEntryId = $"qa.object-reset.bridge.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetBridge_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Bridge Target",
                    qaActivityOwner: activeActivity);

                var trigger = qaObject.AddComponent<ObjectResetTrigger>();
                trigger.ConfigureForQa(
                    declaration,
                    string.Empty,
                    "qa.object-reset.bridge",
                    qaAllowNoParticipants: true);

                var bridge = qaObject.AddComponent<ObjectResetTriggerUnityEventBridge>();
                var counters = new BridgeEventCounters();
                AttachBridgeCounters(bridge, counters);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-bridge-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                var targetCollected = snapshot != null && snapshot.TryGet(id, out _);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Bridge Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-bridge"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot != null && snapshot.IsAvailable),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                trigger.RequestObjectReset();
                var completed = await WaitForObjectResetTriggerAsync(trigger, 32);

                var requestCompleted = completed
                    && !trigger.IsRequestInFlight
                    && trigger.LastEventPhase == FlowRequestEventPhase.Completed;
                var resultSucceededNoParticipants = trigger.HasLastResult
                    && trigger.LastResultStatus == ObjectResetResultStatus.SucceededNoParticipants
                    && trigger.LastResultSucceededNoParticipants;
                var outcomeSucceeded = trigger.LastRequestSucceeded;
                var countersValid = counters.Submitted == 1
                    && counters.Succeeded == 1
                    && counters.SucceededNoParticipants == 1
                    && counters.SucceededWithParticipants == 0
                    && counters.CompletedWithWarnings == 0
                    && counters.Ignored == 0
                    && counters.Failed == 0
                    && counters.Completed == 1;
                var issuesValid = trigger.LastBlockingIssueCount == 0
                    && trigger.LastNonBlockingIssueCount == 0;

                if (!requestCompleted
                    || !targetCollected
                    || !resultSucceededNoParticipants
                    || !outcomeSucceeded
                    || !countersValid
                    || !issuesValid)
                {
                    logger.Warning(
                        "QA Object Reset Bridge Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "object-reset-bridge"),
                            LogFields.Field("requestCompleted", requestCompleted),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("resultSucceededNoParticipants", resultSucceededNoParticipants),
                            LogFields.Field("outcomeSucceeded", outcomeSucceeded),
                            LogFields.Field("countersValid", countersValid),
                            LogFields.Field("issuesValid", issuesValid),
                            LogFields.Field("submitted", counters.Submitted),
                            LogFields.Field("succeeded", counters.Succeeded),
                            LogFields.Field("succeededNoParticipants", counters.SucceededNoParticipants),
                            LogFields.Field("completedWithWarnings", counters.CompletedWithWarnings),
                            LogFields.Field("ignored", counters.Ignored),
                            LogFields.Field("failed", counters.Failed),
                            LogFields.Field("completed", counters.Completed),
                            LogFields.Field("lastStatus", trigger.LastResultStatus.ToString()),
                            LogFields.Field("lastOutcome", trigger.LastOutcome.ToString())));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Bridge Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "object-reset-bridge"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", snapshot != null && snapshot.IsAvailable),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("requestCompleted", requestCompleted),
                        LogFields.Field("lastOutcome", trigger.LastOutcome.ToString()),
                        LogFields.Field("resultStatus", trigger.LastResultStatus.ToString()),
                        LogFields.Field("submitted", counters.Submitted),
                        LogFields.Field("succeeded", counters.Succeeded),
                        LogFields.Field("succeededWithParticipants", counters.SucceededWithParticipants),
                        LogFields.Field("succeededNoParticipants", counters.SucceededNoParticipants),
                        LogFields.Field("completedWithWarnings", counters.CompletedWithWarnings),
                        LogFields.Field("ignored", counters.Ignored),
                        LogFields.Field("failed", counters.Failed),
                        LogFields.Field("completed", counters.Completed),
                        LogFields.Field("blockingIssues", trigger.LastBlockingIssueCount),
                        LogFields.Field("nonBlockingIssues", trigger.LastNonBlockingIssueCount),
                        LogFields.Field("summary", trigger.LastResultSummary)));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Bridge Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "object-reset-bridge"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-bridge-smoke-cleanup");
                }
            }
        }


        internal static async Task<bool> RunFoundationClosureSmokeAsync(
            FrameworkRuntimeHost runtimeHost,
            FrameworkLogger logger,
            string source)
        {
            if (runtimeHost == null || logger == null)
            {
                return false;
            }

            source = string.IsNullOrWhiteSpace(source) ? nameof(ObjectResetQaSmokeRunner) : source;

            GameObject qaObject = null;
            try
            {
                var activeActivity = runtimeHost.State.CurrentActivity;
                if (activeActivity == null)
                {
                    logger.Warning(
                        "QA Object Reset Foundation Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "foundation-closure"),
                            LogFields.Field("reason", "active-activity-missing")));
                    return false;
                }

                var objectEntryId = $"qa.object-reset.foundation.target.{Guid.NewGuid():N}";
                qaObject = new GameObject("QA_ObjectResetFoundation_Target");
                var declaration = qaObject.AddComponent<ObjectEntryDeclaration>();
                declaration.ConfigureForQa(
                    objectEntryId,
                    ObjectEntryScope.Activity,
                    ObjectEntryRequiredness.Required,
                    "QA Object Reset Foundation Target",
                    qaActivityOwner: activeActivity);

                var trigger = qaObject.AddComponent<ObjectResetTrigger>();
                trigger.ConfigureForQa(
                    declaration,
                    string.Empty,
                    "qa.object-reset.foundation-closure.trigger",
                    qaAllowNoParticipants: true);

                var bridge = qaObject.AddComponent<ObjectResetTriggerUnityEventBridge>();
                var counters = new BridgeEventCounters();
                AttachBridgeCounters(bridge, counters);

                runtimeHost.SetObjectResetParticipantSource(null);
                var snapshot = runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-foundation-closure-smoke");
                var id = ObjectEntryId.From(objectEntryId);
                ObjectEntryDescriptor descriptor = default;
                var targetCollected = snapshot != null && snapshot.TryGet(id, out descriptor);
                if (!targetCollected)
                {
                    logger.Warning(
                        "QA Object Reset Foundation Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "foundation-closure"),
                            LogFields.Field("reason", "target-not-collected"),
                            LogFields.Field("snapshotAvailable", snapshot != null && snapshot.IsAvailable),
                            LogFields.Field("objectEntry", objectEntryId)));
                    return false;
                }

                var hostRequest = ObjectResetRequest.ForTarget(
                    ObjectResetTarget.FromDescriptor(descriptor),
                    source,
                    "qa.object-reset.foundation-closure.host");
                var targetResolution = ObjectResetTargetResolver.ResolveTarget(snapshot, hostRequest);

                var requiredSuccess = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Required(
                        ObjectResetParticipantId.From("qa.object-reset.foundation.required"),
                        hostRequest.Target,
                        20,
                        "QA Object Reset Foundation Required Participant",
                        source,
                        "qa.object-reset.foundation-closure.required"),
                    SyntheticObjectResetParticipantMode.Success);
                var optionalSkipped = new SyntheticObjectResetParticipant(
                    ObjectResetParticipantDescriptor.Optional(
                        ObjectResetParticipantId.From("qa.object-reset.foundation.optional"),
                        hostRequest.Target,
                        10,
                        "QA Object Reset Foundation Optional Participant",
                        source,
                        "qa.object-reset.foundation-closure.optional"),
                    SyntheticObjectResetParticipantMode.SkippedOptional);

                runtimeHost.SetObjectResetParticipantSource(new SyntheticObjectResetParticipantSource(requiredSuccess, optionalSkipped));
                var hostResult = await runtimeHost.RequestObjectResetAsync(hostRequest);

                runtimeHost.SetObjectResetParticipantSource(null);
                trigger.RequestObjectReset();
                var triggerCompleted = await WaitForObjectResetTriggerAsync(trigger, 32);

                var hostSnapshotAvailable = runtimeHost.TryGetObjectEntryRuntimeContextSnapshot(out var hostSnapshot)
                    && hostSnapshot != null
                    && hostSnapshot.IsAvailable;
                var targetResolved = targetResolution.Succeeded
                    && targetResolution.HasResolvedTarget
                    && targetResolution.ResolvedTarget.Id == id
                    && targetResolution.BlockingIssueCount == 0;
                var hostResultValid = hostResult.Status == ObjectResetResultStatus.Succeeded
                    && hostResult.ParticipantCount == 2
                    && hostResult.ParticipantSucceededCount == 1
                    && hostResult.ParticipantSkippedCount == 1
                    && hostResult.ParticipantFailedCount == 0
                    && hostResult.BlockingIssueCount == 0
                    && hostResult.NonBlockingIssueCount == 0;
                var triggerResultValid = triggerCompleted
                    && !trigger.IsRequestInFlight
                    && trigger.LastEventPhase == FlowRequestEventPhase.Completed
                    && trigger.LastRequestSucceeded
                    && trigger.LastResultStatus == ObjectResetResultStatus.SucceededNoParticipants
                    && trigger.LastParticipantCount == 0
                    && trigger.LastBlockingIssueCount == 0
                    && trigger.LastNonBlockingIssueCount == 0;
                var bridgeEventsValid = counters.Submitted == 1
                    && counters.Succeeded == 1
                    && counters.SucceededWithParticipants == 0
                    && counters.SucceededNoParticipants == 1
                    && counters.CompletedWithWarnings == 0
                    && counters.Ignored == 0
                    && counters.Failed == 0
                    && counters.Completed == 1;
                var blockingIssues = hostResult.BlockingIssueCount + trigger.LastBlockingIssueCount + targetResolution.BlockingIssueCount;
                var nonBlockingIssues = hostResult.NonBlockingIssueCount + trigger.LastNonBlockingIssueCount + targetResolution.NonBlockingIssueCount;

                if (!hostSnapshotAvailable
                    || !targetCollected
                    || !targetResolved
                    || !hostResultValid
                    || !triggerResultValid
                    || !bridgeEventsValid
                    || blockingIssues != 0
                    || nonBlockingIssues != 0)
                {
                    logger.Warning(
                        "QA Object Reset Foundation Closure Smoke step failed.",
                        LogFields.Of(
                            LogFields.Field("step", "foundation-closure"),
                            LogFields.Field("hostSnapshotAvailable", hostSnapshotAvailable),
                            LogFields.Field("targetCollected", targetCollected),
                            LogFields.Field("targetResolved", targetResolved),
                            LogFields.Field("hostResultValid", hostResultValid),
                            LogFields.Field("triggerResultValid", triggerResultValid),
                            LogFields.Field("bridgeEventsValid", bridgeEventsValid),
                            LogFields.Field("hostStatus", hostResult.Status.ToString()),
                            LogFields.Field("triggerStatus", trigger.LastResultStatus.ToString()),
                            LogFields.Field("blockingIssues", blockingIssues),
                            LogFields.Field("nonBlockingIssues", nonBlockingIssues)));
                    return false;
                }

                logger.Info(
                    "QA Object Reset Foundation Closure Smoke step completed.",
                    LogFields.Of(
                        LogFields.Field("step", "foundation-closure"),
                        LogFields.Field("source", source),
                        LogFields.Field("snapshotAvailable", hostSnapshotAvailable),
                        LogFields.Field("snapshotSource", hostSnapshot != null ? hostSnapshot.Source : "<none>"),
                        LogFields.Field("snapshotRevision", runtimeHost.ObjectEntryRuntimeContextRevision),
                        LogFields.Field("objectEntry", id.StableText),
                        LogFields.Field("targetCollected", targetCollected),
                        LogFields.Field("targetResolved", targetResolved),
                        LogFields.Field("hostStatus", hostResult.Status.ToString()),
                        LogFields.Field("hostParticipants", hostResult.ParticipantCount),
                        LogFields.Field("hostParticipantSucceeded", hostResult.ParticipantSucceededCount),
                        LogFields.Field("hostParticipantSkipped", hostResult.ParticipantSkippedCount),
                        LogFields.Field("hostParticipantFailed", hostResult.ParticipantFailedCount),
                        LogFields.Field("triggerCompleted", triggerCompleted),
                        LogFields.Field("triggerStatus", trigger.LastResultStatus.ToString()),
                        LogFields.Field("bridgeSubmitted", counters.Submitted),
                        LogFields.Field("bridgeSucceeded", counters.Succeeded),
                        LogFields.Field("bridgeSucceededNoParticipants", counters.SucceededNoParticipants),
                        LogFields.Field("bridgeCompleted", counters.Completed),
                        LogFields.Field("blockingIssues", blockingIssues),
                        LogFields.Field("nonBlockingIssues", nonBlockingIssues),
                        LogFields.Field("summary", hostResult.ToDiagnosticString())));

                return true;
            }
            catch (Exception exception)
            {
                logger.Warning(
                    "QA Object Reset Foundation Closure Smoke step failed.",
                    LogFields.Of(
                        LogFields.Field("step", "foundation-closure"),
                        LogFields.Field("reason", exception.Message)));
                return false;
            }
            finally
            {
                runtimeHost.SetObjectResetParticipantSource(null);
                if (qaObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(qaObject);
                    runtimeHost.RefreshObjectEntryRuntimeContextSnapshot($"{source}:object-reset-foundation-closure-smoke-cleanup");
                }
            }
        }


        private static async Task<bool> WaitForObjectResetTriggerAsync(
            ObjectResetTrigger trigger,
            int maxYields)
        {
            if (trigger == null)
            {
                return false;
            }

            var iterations = Math.Max(1, maxYields);
            for (int i = 0; i < iterations; i++)
            {
                await Task.Yield();
                if (!trigger.IsRequestInFlight && trigger.HasLastResult)
                {
                    return true;
                }
            }

            return !trigger.IsRequestInFlight && trigger.HasLastResult;
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


        private static void AttachBridgeCounters(ObjectResetTriggerUnityEventBridge bridge, BridgeEventCounters counters)
        {
            if (bridge == null || counters == null)
            {
                return;
            }

            bridge.RequestSubmitted.AddListener(() => counters.Submitted++);
            bridge.RequestSucceeded.AddListener(() => counters.Succeeded++);
            bridge.RequestSucceededWithParticipants.AddListener(() => counters.SucceededWithParticipants++);
            bridge.RequestSucceededNoParticipants.AddListener(() => counters.SucceededNoParticipants++);
            bridge.RequestCompletedWithWarnings.AddListener(() => counters.CompletedWithWarnings++);
            bridge.RequestIgnored.AddListener(() => counters.Ignored++);
            bridge.RequestFailed.AddListener(() => counters.Failed++);
            bridge.RequestCompleted.AddListener(() => counters.Completed++);
        }

        private sealed class BridgeEventCounters
        {
            public int Submitted;
            public int Succeeded;
            public int SucceededWithParticipants;
            public int SucceededNoParticipants;
            public int CompletedWithWarnings;
            public int Ignored;
            public int Failed;
            public int Completed;
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
                    case SyntheticObjectResetParticipantMode.NonBlockingFailure:
                        return ObjectResetParticipantResult.NonBlockingFailure(
                            context,
                            1,
                            descriptor.Source,
                            descriptor.Reason,
                            "Synthetic Object Reset participant failed without blocking.");
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
            BlockingFailure = 2,
            NonBlockingFailure = 3
        }
    }
}
#endif

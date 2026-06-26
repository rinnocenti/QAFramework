#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
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
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F14 Object Reset QA smoke runner; no physical reset, no participant execution.")]
    internal static class ObjectResetQaSmokeRunner
    {
        internal const string TargetResolutionSmokeName = "Object Reset Target Resolution Smoke";

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
    }
}
#endif

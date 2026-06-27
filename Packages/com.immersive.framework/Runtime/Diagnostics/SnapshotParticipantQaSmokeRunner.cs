#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Threading.Tasks;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;
using Immersive.Framework.Identity;
using Immersive.Framework.ObjectEntry;
using Immersive.Framework.Pause;
using Immersive.Framework.Snapshot;
using Immersive.Framework.Transition;
using Immersive.Framework.TransitionEffects;
using Immersive.Logging.Records;

namespace Immersive.Framework.Diagnostics
{
    /// <summary>
    /// API status: Development Tooling. Synthetic smoke for F21C Snapshot participant contracts.
    /// It validates descriptor/capture/restore result shapes without backend, PlayerPrefs, JSON,
    /// progression slots, UI, scene objects, ScriptableObjects or runtime orchestration.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.DevelopmentTooling, "F21C Snapshot participant diagnostics smoke; synthetic participant contracts only.")]
    internal static class SnapshotParticipantQaSmokeRunner
    {
        internal const string SmokeName = "Snapshot Participant Diagnostics Smoke";

        internal static Task<bool> RunDiagnosticsSmokeAsync(FrameworkLogger logger, string source)
        {
            if (logger == null)
            {
                return Task.FromResult(false);
            }

            var normalizedSource = string.IsNullOrWhiteSpace(source) ? nameof(SnapshotParticipantQaSmokeRunner) : source.Trim();

            var descriptorPassed = ValidateDescriptorAndContext(logger, normalizedSource, out var descriptor, out var context);

            var envelope = default(SnapshotEnvelope);
            var capturePassed = descriptorPassed && ValidateCapture(logger, descriptor, context, out envelope);
            var restorePassed = capturePassed && ValidateRestore(logger, descriptor, envelope);
            var rejectedRestorePassed = capturePassed && ValidateForeignEnvelopeRejected(logger, descriptor, context);
            var optionalSkipPassed = ValidateOptionalSkip(logger, normalizedSource);
            var canonicalBoundaryPassed = ValidateCanonicalBoundary(logger);

            return Task.FromResult(descriptorPassed
                && capturePassed
                && restorePassed
                && rejectedRestorePassed
                && optionalSkipPassed
                && canonicalBoundaryPassed);
        }

        private static bool ValidateDescriptorAndContext(
            FrameworkLogger logger,
            string source,
            out SnapshotParticipantDescriptor descriptor,
            out SnapshotCaptureContext context)
        {
            var owner = FrameworkIdentityKey.From(FrameworkIdentityDomain.Activity, "qa.snapshot.activity.owner");
            descriptor = SnapshotParticipantDescriptor.Required(
                SnapshotParticipantId.From("qa.snapshot.participant.required"),
                SnapshotScope.Activity,
                owner,
                SnapshotSchemaId.From("qa.snapshot.schema.activity"),
                SnapshotSchemaVersion.Initial(),
                10,
                "QA Required Snapshot Participant",
                source,
                "qa.snapshot.descriptor");

            context = new SnapshotCaptureContext(
                SnapshotScope.Activity,
                owner,
                new DateTime(2026, 6, 26, 12, 0, 0, DateTimeKind.Utc).Ticks,
                source,
                "qa.snapshot.capture");

            var passed = descriptor.IsValid
                && context.IsValid
                && descriptor.IsRequired
                && descriptor.SupportsCaptureContext(context)
                && context.Matches(descriptor)
                && descriptor.OwnerIdentity.Domain == FrameworkIdentityDomain.Activity;

            LogStep(
                logger,
                "descriptor-context",
                passed,
                LogFields.Of(
                    LogFields.Field("participant", descriptor.ParticipantId.StableText),
                    LogFields.Field("scope", descriptor.Scope.ToString()),
                    LogFields.Field("owner", descriptor.OwnerIdentity.StableText),
                    LogFields.Field("schema", descriptor.SchemaId.StableText),
                    LogFields.Field("version", descriptor.SchemaVersion.StableText),
                    LogFields.Field("requiredness", descriptor.Requiredness.ToString()),
                    LogFields.Field("contextMatches", context.Matches(descriptor))));

            if (passed)
            {
                logger.Debug("QA Snapshot Participant Diagnostics Smoke descriptor diagnostics.", LogFields.Field("details", descriptor.ToDiagnosticString()));
            }

            return passed;
        }

        private static bool ValidateCapture(
            FrameworkLogger logger,
            SnapshotParticipantDescriptor descriptor,
            SnapshotCaptureContext context,
            out SnapshotEnvelope envelope)
        {
            var participant = new SyntheticSnapshotParticipant(descriptor);
            var captureResult = participant.CaptureSnapshot(context);
            envelope = captureResult.Envelope;

            var passed = captureResult.IsValid
                && captureResult.Captured
                && captureResult.Completed
                && captureResult.HasEnvelope
                && !captureResult.BlocksSnapshot
                && envelope.IsValid
                && descriptor.SupportsEnvelope(envelope)
                && envelope.Payload.Format == SnapshotPayloadFormat.Text
                && envelope.Payload.HasBytes;

            LogStep(
                logger,
                "capture",
                passed,
                LogFields.Of(
                    LogFields.Field("status", captureResult.Status.ToString()),
                    LogFields.Field("completed", captureResult.Completed),
                    LogFields.Field("hasEnvelope", captureResult.HasEnvelope),
                    LogFields.Field("blocksSnapshot", captureResult.BlocksSnapshot),
                    LogFields.Field("payloadFormat", envelope.Payload.Format.ToString()),
                    LogFields.Field("payloadBytes", envelope.Payload.ByteCount)));

            if (passed)
            {
                logger.Debug("QA Snapshot Participant Diagnostics Smoke capture diagnostics.", LogFields.Field("details", captureResult.ToDiagnosticString()));
            }

            return passed;
        }

        private static bool ValidateRestore(
            FrameworkLogger logger,
            SnapshotParticipantDescriptor descriptor,
            SnapshotEnvelope envelope)
        {
            var participant = new SyntheticSnapshotParticipant(descriptor);
            var restoreContext = SnapshotRestoreContext.FromEnvelope(envelope, nameof(SnapshotParticipantQaSmokeRunner), "qa.snapshot.restore");
            var restoreResult = participant.RestoreSnapshot(restoreContext);

            var passed = restoreContext.IsValid
                && restoreContext.Matches(descriptor)
                && restoreResult.IsValid
                && restoreResult.Restored
                && restoreResult.Completed
                && restoreResult.HasEnvelope
                && !restoreResult.BlocksSnapshot
                && descriptor.SupportsEnvelope(restoreResult.Envelope);

            LogStep(
                logger,
                "restore",
                passed,
                LogFields.Of(
                    LogFields.Field("status", restoreResult.Status.ToString()),
                    LogFields.Field("completed", restoreResult.Completed),
                    LogFields.Field("hasEnvelope", restoreResult.HasEnvelope),
                    LogFields.Field("blocksSnapshot", restoreResult.BlocksSnapshot),
                    LogFields.Field("contextMatches", restoreContext.Matches(descriptor))));

            if (passed)
            {
                logger.Debug("QA Snapshot Participant Diagnostics Smoke restore diagnostics.", LogFields.Field("details", restoreResult.ToDiagnosticString()));
            }

            return passed;
        }

        private static bool ValidateForeignEnvelopeRejected(
            FrameworkLogger logger,
            SnapshotParticipantDescriptor descriptor,
            SnapshotCaptureContext context)
        {
            var foreignOwner = FrameworkIdentityKey.From(FrameworkIdentityDomain.Activity, "qa.snapshot.activity.foreign-owner");
            var foreignEnvelope = new SnapshotEnvelope(
                SnapshotEnvelopeId.From("qa.snapshot.envelope.foreign"),
                SnapshotScope.Activity,
                foreignOwner,
                descriptor.SchemaId,
                descriptor.SchemaVersion,
                SnapshotPayload.FromText("foreign payload", "text/plain"),
                context.CapturedUtcTicks,
                nameof(SnapshotParticipantQaSmokeRunner),
                "qa.snapshot.foreign");

            var participant = new SyntheticSnapshotParticipant(descriptor);
            var restoreResult = participant.RestoreSnapshot(SnapshotRestoreContext.FromEnvelope(
                foreignEnvelope,
                nameof(SnapshotParticipantQaSmokeRunner),
                "qa.snapshot.restore.foreign"));

            var passed = foreignEnvelope.IsValid
                && !descriptor.SupportsEnvelope(foreignEnvelope)
                && restoreResult.IsValid
                && restoreResult.Rejected
                && !restoreResult.Completed
                && restoreResult.BlocksSnapshot
                && !restoreResult.HasEnvelope;

            LogStep(
                logger,
                "foreign-envelope-rejected",
                passed,
                LogFields.Of(
                    LogFields.Field("status", restoreResult.Status.ToString()),
                    LogFields.Field("completed", restoreResult.Completed),
                    LogFields.Field("blocksSnapshot", restoreResult.BlocksSnapshot),
                    LogFields.Field("descriptorSupportsEnvelope", descriptor.SupportsEnvelope(foreignEnvelope))));

            return passed;
        }

        private static bool ValidateOptionalSkip(FrameworkLogger logger, string source)
        {
            var descriptor = SnapshotParticipantDescriptor.Optional(
                SnapshotParticipantId.From("qa.snapshot.participant.optional"),
                SnapshotScope.Route,
                FrameworkIdentityKey.From(FrameworkIdentityDomain.Route, "qa.snapshot.route.owner"),
                SnapshotSchemaId.From("qa.snapshot.schema.route.optional"),
                SnapshotSchemaVersion.Initial(),
                20,
                "QA Optional Snapshot Participant",
                source,
                "qa.snapshot.optional");

            var captureResult = SnapshotParticipantCaptureResult.SkippedResult(descriptor, "Optional participant skipped synthetically.");
            var restoreResult = SnapshotParticipantRestoreResult.SkippedResult(descriptor, "Optional participant restore skipped synthetically.");

            var passed = descriptor.IsValid
                && descriptor.IsOptional
                && captureResult.IsValid
                && captureResult.Skipped
                && captureResult.Completed
                && !captureResult.BlocksSnapshot
                && !captureResult.HasEnvelope
                && restoreResult.IsValid
                && restoreResult.Skipped
                && restoreResult.Completed
                && !restoreResult.BlocksSnapshot
                && !restoreResult.HasEnvelope;

            LogStep(
                logger,
                "optional-skip",
                passed,
                LogFields.Of(
                    LogFields.Field("captureStatus", captureResult.Status.ToString()),
                    LogFields.Field("restoreStatus", restoreResult.Status.ToString()),
                    LogFields.Field("captureBlocksSnapshot", captureResult.BlocksSnapshot),
                    LogFields.Field("restoreBlocksSnapshot", restoreResult.BlocksSnapshot)));

            return passed;
        }

        private static bool ValidateCanonicalBoundary(FrameworkLogger logger)
        {
            var canonicalNamespace = typeof(SnapshotEnvelope).Namespace;
            var diagnosticsSnapshotsOutsideCanonicalNamespace = typeof(PauseSnapshot).Namespace != canonicalNamespace
                && typeof(GateSnapshot).Namespace != canonicalNamespace
                && typeof(TransitionSnapshot).Namespace != canonicalNamespace
                && typeof(TransitionEffectSnapshot).Namespace != canonicalNamespace
                && typeof(ObjectEntryRuntimeContextSnapshot).Namespace != canonicalNamespace;

            var passed = string.Equals(canonicalNamespace, "Immersive.Framework.Snapshot", StringComparison.Ordinal)
                && diagnosticsSnapshotsOutsideCanonicalNamespace;

            LogStep(
                logger,
                "canonical-boundary",
                passed,
                LogFields.Of(
                    LogFields.Field("canonicalNamespace", canonicalNamespace ?? "<none>"),
                    LogFields.Field("knownDiagnosticSnapshotsOutsideCanonicalNamespace", diagnosticsSnapshotsOutsideCanonicalNamespace),
                    LogFields.Field("knownDiagnosticSnapshotTypes", 5),
                    LogFields.Field("backend", "none"),
                    LogFields.Field("playerPrefs", "none"),
                    LogFields.Field("progressionSlot", "none")));

            return passed;
        }

        private static void LogStep(FrameworkLogger logger, string step, bool passed, LogField[] fields)
        {
            var allFields = AppendFields(
                LogFields.Of(
                    LogFields.Field("step", step),
                    LogFields.Field("passed", passed)),
                fields);

            if (passed)
            {
                logger.Info("QA Snapshot Participant Diagnostics Smoke step completed.", allFields);
                return;
            }

            logger.Warning("QA Snapshot Participant Diagnostics Smoke step failed.", allFields);
        }

        private static LogField[] AppendFields(LogField[] baseFields, LogField[] additionalFields)
        {
            if (additionalFields == null || additionalFields.Length == 0)
            {
                return baseFields;
            }

            var combined = new LogField[baseFields.Length + additionalFields.Length];
            Array.Copy(baseFields, combined, baseFields.Length);
            Array.Copy(additionalFields, 0, combined, baseFields.Length, additionalFields.Length);
            return combined;
        }

        private sealed class SyntheticSnapshotParticipant : ISnapshotParticipant
        {
            private readonly SnapshotParticipantDescriptor descriptor;

            public SyntheticSnapshotParticipant(SnapshotParticipantDescriptor descriptor)
            {
                if (!descriptor.IsValid)
                {
                    throw new ArgumentException("Synthetic Snapshot participant requires a valid descriptor.", nameof(descriptor));
                }

                this.descriptor = descriptor;
            }

            public SnapshotParticipantDescriptor GetSnapshotDescriptor()
            {
                return descriptor;
            }

            public SnapshotParticipantCaptureResult CaptureSnapshot(SnapshotCaptureContext context)
            {
                if (!descriptor.SupportsCaptureContext(context))
                {
                    return SnapshotParticipantCaptureResult.RejectedResult(
                        descriptor,
                        "Synthetic participant rejected capture context because scope or owner did not match descriptor.");
                }

                var envelope = new SnapshotEnvelope(
                    SnapshotEnvelopeId.From(descriptor.ParticipantId.Value.Value + ".envelope"),
                    descriptor.Scope,
                    descriptor.OwnerIdentity,
                    descriptor.SchemaId,
                    descriptor.SchemaVersion,
                    SnapshotPayload.FromText("synthetic snapshot payload", "text/plain"),
                    context.CapturedUtcTicks,
                    context.Source,
                    context.Reason);

                return SnapshotParticipantCaptureResult.CapturedResult(
                    descriptor,
                    envelope,
                    "Synthetic participant captured a backend-agnostic envelope.");
            }

            public SnapshotParticipantRestoreResult RestoreSnapshot(SnapshotRestoreContext context)
            {
                if (!context.IsValid || !descriptor.SupportsEnvelope(context.Envelope))
                {
                    return SnapshotParticipantRestoreResult.RejectedResult(
                        descriptor,
                        "Synthetic participant rejected restore envelope because owner/schema/version did not match descriptor.");
                }

                return SnapshotParticipantRestoreResult.RestoredResult(
                    descriptor,
                    context.Envelope,
                    "Synthetic participant restored from a backend-agnostic envelope.");
            }
        }
    }
}
#endif

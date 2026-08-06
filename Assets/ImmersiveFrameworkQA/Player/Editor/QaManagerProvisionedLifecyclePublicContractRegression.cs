using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Edit Mode, public-only regression for the consumer-facing
    /// Manager-Provisioned Player lifecycle contracts and Authoring endpoint.
    /// It does not access package internals or simulate ActivityFlow.
    /// </summary>
    internal static class
        QaManagerProvisionedLifecyclePublicContractRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/" +
            "Run Manager-Provisioned Lifecycle Public Contract Regression";

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            var completed = new List<string>();
            GameObject authoringRoot = null;

            try
            {
                Require(
                    !EditorApplication.isPlayingOrWillChangePlaymode,
                    "Manager-Provisioned lifecycle public contract regression must run in Edit Mode.");
                completed.Add("edit-mode-required");

                ProveUnavailableFactory();
                completed.Add("unavailable-factory");

                ProveGateEvidenceNormalization();
                completed.Add("gate-evidence-normalization");

                ManagerProvisionedPlayerLifecycleSlotSnapshot
                    originalSlot = CreateReadySlot();

                ProvePendingPlayerContribution(originalSlot);
                completed.Add("player-readiness-pending");

                ProveSlotEvidenceCopy(originalSlot);
                completed.Add("slot-evidence-immutable-copy");

                ProveFailedTerminalContribution(originalSlot);
                completed.Add("failed-terminal-contribution");

                ProveReleasedTerminalContribution(originalSlot);
                completed.Add("released-terminal-contribution");

                ProveNullSlotRejected();
                completed.Add("null-slot-rejected");

                authoringRoot =
                    ProveUnboundAuthoringIsExplicitlyUnavailable();
                completed.Add("unbound-authoring-explicitly-unavailable");

                Require(
                    completed.Count == 9,
                    "Manager-Provisioned lifecycle public contract regression case count changed unexpectedly.");

                Debug.Log(
                    "[M07_MANAGER_PROVISIONED_LIFECYCLE_PUBLIC_CONTRACT_REGRESSION] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[M07_MANAGER_PROVISIONED_LIFECYCLE_PUBLIC_CONTRACT_REGRESSION] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (authoringRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(authoringRoot);
                }
            }
        }

        private static void ProveUnavailableFactory()
        {
            ManagerProvisionedPlayerLifecycleSnapshot snapshot =
                ManagerProvisionedPlayerLifecycleSnapshot.Unavailable(
                    "  public lifecycle unavailable  ");

            Require(!snapshot.IsAvailable,
                "Unavailable snapshot was marked available.");
            Require(
                snapshot.Status ==
                    ManagerProvisionedPlayerLifecycleStatus.Unavailable,
                "Unavailable snapshot did not preserve Unavailable status.");
            Require(
                snapshot.GateEvidenceScope ==
                    ManagerProvisionedPlayerGateEvidenceScope.None,
                "Unavailable snapshot exposed a gate evidence scope.");
            Require(!snapshot.HasGateEvidence,
                "Unavailable snapshot exposed gate evidence.");
            Require(!snapshot.GateHeld,
                "Unavailable snapshot silently represented a held gate.");
            Require(snapshot.Slots != null && snapshot.SlotCount == 0,
                "Unavailable snapshot did not expose an empty Slots collection.");
            Require(snapshot.Diagnostic == "public lifecycle unavailable",
                "Unavailable diagnostic was not normalized.");
            Require(
                !snapshot.IsReady &&
                !snapshot.IsFailure &&
                !snapshot.IsReleased,
                "Unavailable snapshot exposed a terminal convenience flag.");
        }

        private static void ProveGateEvidenceNormalization()
        {
            ManagerProvisionedPlayerLifecycleSnapshot missingEvidence =
                new ManagerProvisionedPlayerLifecycleSnapshot(
                    true,
                    ManagerProvisionedPlayerLifecycleStatus
                        .WaitingForActivity,
                    "  Activity A  ",
                    -3,
                    -7,
                    -5,
                    -1,
                    "  GameplayReady  ",
                    "  Idle  ",
                    "  No occurrence  ",
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityGateAggregate,
                    false,
                    true,
                    false,
                    -2,
                    null,
                    "  normalized  ");

            Require(
                missingEvidence.ActivityName == "Activity A" &&
                missingEvidence.EntryPolicy == "GameplayReady" &&
                missingEvidence.ReadinessStatus == "Idle" &&
                missingEvidence.ReadinessReason == "No occurrence" &&
                missingEvidence.Diagnostic == "normalized",
                "Lifecycle strings were not normalized.");
            Require(
                missingEvidence.ActivityOccurrence == 0 &&
                missingEvidence.SessionRevision == 0 &&
                missingEvidence.RequestedSessionRevision == 0 &&
                missingEvidence.AppliedSessionRevision == 0 &&
                missingEvidence.HostCount == 0,
                "Negative lifecycle counters were not clamped.");
            Require(
                missingEvidence.GateEvidenceScope ==
                    ManagerProvisionedPlayerGateEvidenceScope.None &&
                !missingEvidence.HasGateEvidence &&
                !missingEvidence.GateHeld,
                "Missing gate evidence was represented as aggregate gate evidence.");

            ManagerProvisionedPlayerLifecycleSnapshot missingScope =
                new ManagerProvisionedPlayerLifecycleSnapshot(
                    true,
                    ManagerProvisionedPlayerLifecycleStatus
                        .PreparingGameplayAdmission,
                    "Activity A",
                    1,
                    1,
                    1,
                    1,
                    "GameplayReady",
                    "Preparing",
                    "Player readiness pending",
                    ManagerProvisionedPlayerGateEvidenceScope.None,
                    true,
                    true,
                    false,
                    1,
                    null,
                    "scope intentionally missing");

            Require(
                missingScope.GateEvidenceScope ==
                    ManagerProvisionedPlayerGateEvidenceScope.None &&
                !missingScope.HasGateEvidence &&
                !missingScope.GateHeld,
                "Gate evidence without an explicit scope was accepted.");
        }

        private static void ProvePendingPlayerContribution(
            ManagerProvisionedPlayerLifecycleSlotSnapshot slot)
        {
            ManagerProvisionedPlayerLifecycleSnapshot snapshot =
                CreateLifecycleSnapshot(
                    ManagerProvisionedPlayerLifecycleStatus
                        .PreparingGameplayAdmission,
                    "Preparing",
                    "Player readiness pending",
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution,
                    true,
                    true,
                    new[] { slot });

            Require(snapshot.IsAvailable,
                "Pending lifecycle snapshot was unavailable.");
            Require(
                snapshot.GateEvidenceScope ==
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution,
                "Pending Player contribution lost its exact evidence scope.");
            Require(snapshot.HasGateEvidence && snapshot.GateHeld,
                "Preparing Player readiness contribution did not hold its projected gate.");
            Require(
                snapshot.Status ==
                    ManagerProvisionedPlayerLifecycleStatus
                        .PreparingGameplayAdmission,
                "Pending readiness did not preserve lifecycle status.");
            Require(
                snapshot.ReadinessStatus == "Preparing" &&
                snapshot.ReadinessReason ==
                    "Player readiness pending",
                "Pending readiness evidence was not preserved.");
            Require(!snapshot.IsReady && !snapshot.IsFailure,
                "Pending readiness exposed a terminal convenience flag.");
        }

        private static void ProveSlotEvidenceCopy(
            ManagerProvisionedPlayerLifecycleSlotSnapshot originalSlot)
        {
            var source =
                new[] { originalSlot };

            ManagerProvisionedPlayerLifecycleSnapshot snapshot =
                CreateLifecycleSnapshot(
                    ManagerProvisionedPlayerLifecycleStatus.Ready,
                    "Completed",
                    "Completed",
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution,
                    true,
                    false,
                    source);

            source[0] =
                new ManagerProvisionedPlayerLifecycleSlotSnapshot(
                    "replacement",
                    "Available",
                    false,
                    string.Empty,
                    false,
                    false,
                    false,
                    "replacement");

            Require(snapshot.SlotCount == 1,
                "Lifecycle snapshot changed Slot count after source mutation.");
            Require(
                ReferenceEquals(snapshot.Slots[0], originalSlot),
                "Lifecycle snapshot did not preserve its copied Slot collection.");
            Require(
                snapshot.Slots[0].PlayerSlotId == "player-1" &&
                snapshot.Slots[0].SlotState == "Joined" &&
                snapshot.Slots[0].HasTechnicalHost &&
                snapshot.Slots[0].HasSelectedActor &&
                snapshot.Slots[0].SelectedActorProfile ==
                    "actor.default" &&
                snapshot.Slots[0].LogicalActorPrepared &&
                snapshot.Slots[0].PhysicalActorMaterialized &&
                snapshot.Slots[0].GameplayAdmitted,
                "Copied Slot evidence diverged.");
            Require(snapshot.IsReady,
                "Ready lifecycle snapshot did not expose IsReady.");
        }

        private static void ProveFailedTerminalContribution(
            ManagerProvisionedPlayerLifecycleSlotSnapshot slot)
        {
            ManagerProvisionedPlayerLifecycleSnapshot snapshot =
                CreateLifecycleSnapshot(
                    ManagerProvisionedPlayerLifecycleStatus.Failed,
                    "Failed",
                    "Player readiness failed",
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution,
                    true,
                    false,
                    new[] { slot });

            Require(snapshot.IsFailure,
                "Failed lifecycle snapshot did not expose IsFailure.");
            Require(!snapshot.GateHeld,
                "Terminal failed Player contribution remained held.");
            Require(snapshot.HasGateEvidence,
                "Terminal failed Player contribution lost its evidence.");
            Require(
                snapshot.ReadinessStatus == "Failed" &&
                snapshot.ReadinessReason ==
                    "Player readiness failed",
                "Failed readiness evidence was not preserved.");
        }

        private static void ProveReleasedTerminalContribution(
            ManagerProvisionedPlayerLifecycleSlotSnapshot slot)
        {
            ManagerProvisionedPlayerLifecycleSnapshot snapshot =
                CreateLifecycleSnapshot(
                    ManagerProvisionedPlayerLifecycleStatus.Released,
                    "Released",
                    "ActivityExit",
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution,
                    true,
                    false,
                    new[] { slot });

            Require(snapshot.IsReleased,
                "Released lifecycle snapshot did not expose IsReleased.");
            Require(!snapshot.GateHeld,
                "Released Player contribution remained held.");
            Require(snapshot.HasGateEvidence,
                "Released Player contribution lost occurrence evidence.");
            Require(
                snapshot.GateEvidenceScope ==
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution,
                "Released contribution was presented as an aggregate Activity gate.");
        }

        private static void ProveNullSlotRejected()
        {
            bool rejected = false;

            try
            {
                CreateLifecycleSnapshot(
                    ManagerProvisionedPlayerLifecycleStatus
                        .WaitingForJoin,
                    "Completed",
                    "No joined Player",
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution,
                    true,
                    false,
                    new ManagerProvisionedPlayerLifecycleSlotSnapshot[]
                    {
                        null
                    });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            Require(rejected,
                "Lifecycle snapshot accepted a null Slot entry.");
        }

        private static GameObject
            ProveUnboundAuthoringIsExplicitlyUnavailable()
        {
            var root =
                new GameObject(
                    "M07 Public Lifecycle Authoring QA");

            LocalPlayerProvisioningAuthoring authoring =
                root.AddComponent<
                    LocalPlayerProvisioningAuthoring>();

            bool observed =
                authoring
                    .TryGetManagerProvisionedLifecycleSnapshot(
                        out ManagerProvisionedPlayerLifecycleSnapshot
                            direct);

            Require(!observed,
                "Unbound Authoring reported lifecycle evidence.");
            Require(
                direct != null &&
                !direct.IsAvailable &&
                direct.Status ==
                    ManagerProvisionedPlayerLifecycleStatus.Unavailable,
                "Unbound Authoring did not return an explicit unavailable snapshot.");
            Require(
                direct.Diagnostic.Contains(
                    "not bound",
                    StringComparison.OrdinalIgnoreCase),
                "Unbound Authoring returned no diagnostic evidence.");

            ManagerProvisionedPlayerLifecycleSnapshot property =
                authoring.ManagerProvisionedLifecycleSnapshot;

            Require(
                property != null &&
                !property.IsAvailable &&
                property.Status ==
                    ManagerProvisionedPlayerLifecycleStatus.Unavailable &&
                property.Diagnostic == direct.Diagnostic,
                "Authoring lifecycle property diverged from TryGet behavior.");
            Require(!authoring.RuntimeReady,
                "Unbound Authoring unexpectedly reported RuntimeReady.");

            return root;
        }

        private static
            ManagerProvisionedPlayerLifecycleSlotSnapshot
                CreateReadySlot()
        {
            return new
                ManagerProvisionedPlayerLifecycleSlotSnapshot(
                    "  player-1  ",
                    "  Joined  ",
                    true,
                    "  actor.default  ",
                    true,
                    true,
                    true,
                    "  ready evidence  ");
        }

        private static ManagerProvisionedPlayerLifecycleSnapshot
            CreateLifecycleSnapshot(
                ManagerProvisionedPlayerLifecycleStatus status,
                string readinessStatus,
                string readinessReason,
                ManagerProvisionedPlayerGateEvidenceScope
                    gateEvidenceScope,
                bool hasGateEvidence,
                bool gateHeld,
                IReadOnlyList<
                    ManagerProvisionedPlayerLifecycleSlotSnapshot>
                        slots)
        {
            return new
                ManagerProvisionedPlayerLifecycleSnapshot(
                    true,
                    status,
                    "M07 QA Activity",
                    7,
                    12,
                    11,
                    12,
                    "GameplayReady",
                    readinessStatus,
                    readinessReason,
                    gateEvidenceScope,
                    hasGateEvidence,
                    gateHeld,
                    false,
                    1,
                    slots,
                    "public contract QA");
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

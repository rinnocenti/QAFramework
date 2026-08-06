using System;
using System.Collections.Generic;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.PlayerAssignment.Internal.Editor
{
    /// <summary>
    /// Editor-only CPSA-1 contract smoke. It exercises the plain Session authority
    /// without scenes, Play Mode, PlayerInput, reflection or persistent fixtures.
    /// </summary>
    internal static class QaCpsa1CurrentPlayerSlotAssignmentSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/Run Current Player Slot Assignment Foundation";
        private const string LogPrefix =
            "[CPSA1_CURRENT_PLAYER_SLOT_ASSIGNMENT_SMOKE]";

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            var completed = new List<string>();
            var profiles = new List<PlayerSlotProfile>();

            try
            {
                PlayerParticipationRuntimeContext primary = CreateJoinedContext(
                    profiles,
                    "primary",
                    2,
                    out PlayerSlotId primarySlotOne,
                    out PlayerSlotId primarySlotTwo);
                RuntimeContentOwner primaryOwner =
                    primary.CreateSessionAssignmentOwner();
                RuntimeContentOwner sceneOwner = RuntimeContentOwner.Activity(
                    "qa.cpsa1.primary.activity",
                    "QA CPSA1 Primary Activity");
                PlayerHostBindingIdentity managerHost =
                    primary.CreateHostBindingIdentity();
                PlayerHostBindingIdentity sceneHost =
                    primary.CreateHostBindingIdentity();

                PlayerSlotAssignmentResult managerBegin = primary.BeginAssignment(
                    primarySlotOne,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    primaryOwner,
                    managerHost,
                    "QA.CPSA1.Manager",
                    "manager-assignment");
                AssertStatus(
                    managerBegin,
                    PlayerSlotAssignmentStatus.SucceededAssigned,
                    "Manager-Provisioned assignment begin failed.");
                AssertTrue(
                    managerBegin.CurrentAssignment.AssignmentOwner == primaryOwner,
                    "Manager-Provisioned assignment did not retain the Session owner.");
                completed.Add("manager-session-owner");

                PlayerSlotAssignmentResult sceneBegin = primary.BeginAssignment(
                    primarySlotTwo,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    sceneOwner,
                    sceneHost,
                    "QA.CPSA1.Scene",
                    "scene-assignment");
                AssertStatus(
                    sceneBegin,
                    PlayerSlotAssignmentStatus.SucceededAssigned,
                    "Scene-Provided assignment begin failed.");
                AssertTrue(
                    sceneBegin.CurrentAssignment.AssignmentOwner == sceneOwner,
                    "Scene-Provided assignment did not retain the Activity owner.");
                completed.Add("scene-activity-or-route-owner");

                AssertCommonContract(
                    managerBegin.CurrentAssignment,
                    sceneBegin.CurrentAssignment);
                completed.Add("common-origin-contract");

                AssertTrue(
                    primary.TryGetCurrentAssignment(
                        primarySlotOne,
                        out PlayerSlotAssignmentSnapshot currentManager) &&
                    currentManager.AssignmentToken ==
                        managerBegin.CurrentAssignment.AssignmentToken,
                    "Current assignment lookup did not return Manager evidence.");
                completed.Add("current-assignment-lookup");

                PlayerSlotAssignmentResult confirmation =
                    primary.TryConfirmCurrentAssignment(
                        primarySlotOne,
                        managerBegin.CurrentAssignment.AssignmentToken,
                        "QA.CPSA1",
                        "confirm-current");
                AssertStatus(
                    confirmation,
                    PlayerSlotAssignmentStatus.SucceededConfirmed,
                    "Current assignment confirmation failed.");
                completed.Add("current-token-confirmation");

                PlayerSlotAssignmentResult idempotent = primary.BeginAssignment(
                    primarySlotOne,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    primaryOwner,
                    managerHost,
                    "QA.CPSA1.Manager.Retry",
                    "different-diagnostic-retry");
                AssertStatus(
                    idempotent,
                    PlayerSlotAssignmentStatus.SucceededAlreadyAssigned,
                    "Same domain assignment evidence was not idempotent across diagnostics.");
                AssertEqual(
                    managerBegin.CurrentAssignment.AssignmentToken,
                    idempotent.CurrentAssignment.AssignmentToken,
                    "Idempotent Begin changed the current token.");
                AssertEqual(
                    managerBegin.CurrentAssignment.AssignmentSequence,
                    idempotent.CurrentAssignment.AssignmentSequence,
                    "Idempotent Begin changed the assignment sequence.");
                AssertEqual(
                    managerBegin.CurrentAssignment.AssignmentRevision,
                    idempotent.CurrentAssignment.AssignmentRevision,
                    "Idempotent Begin changed the assignment revision.");
                AssertEqual(
                    managerBegin.CurrentAssignment.Source,
                    idempotent.CurrentAssignment.Source,
                    "Idempotent Begin replaced committed Source diagnostics.");
                AssertEqual(
                    managerBegin.CurrentAssignment.Reason,
                    idempotent.CurrentAssignment.Reason,
                    "Idempotent Begin replaced committed Reason diagnostics.");
                completed.Add("same-domain-evidence-different-diagnostics-idempotent");
                completed.Add("idempotent-begin-preserves-token");

                PlayerSlotAssignmentResult duplicate = primary.BeginAssignment(
                    primarySlotOne,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    primaryOwner,
                    primary.CreateHostBindingIdentity(),
                    "QA.CPSA1.Manager",
                    "different-evidence");
                AssertStatus(
                    duplicate,
                    PlayerSlotAssignmentStatus.RejectedAssignmentConflict,
                    "Different evidence duplicated a current assignment.");
                completed.Add("duplicate-assignment-rejected");

                PlayerSlotAssignmentToken releasedToken =
                    managerBegin.CurrentAssignment.AssignmentToken;
                PlayerSlotAssignmentResult release = primary.ReleaseAssignment(
                    primarySlotOne,
                    releasedToken,
                    "QA.CPSA1",
                    "release-current");
                AssertStatus(
                    release,
                    PlayerSlotAssignmentStatus.SucceededReleased,
                    "Current assignment release failed.");
                AssertTrue(
                    !primary.TryGetCurrentAssignment(primarySlotOne, out _),
                    "Released assignment remained current.");
                completed.Add("assignment-release");

                AssertStatus(
                    primary.ReleaseAssignment(
                        primarySlotOne,
                        releasedToken,
                        "QA.CPSA1",
                        "reuse-released-token"),
                    PlayerSlotAssignmentStatus.RejectedStaleToken,
                    "Released assignment token was reused.");
                completed.Add("released-token-reuse-rejected");

                PlayerHostBindingIdentity replacementHost =
                    primary.CreateHostBindingIdentity();
                PlayerSlotAssignmentResult replacement = primary.BeginAssignment(
                    primarySlotOne,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    primaryOwner,
                    replacementHost,
                    "QA.CPSA1.Manager",
                    "replacement-assignment");
                AssertStatus(
                    replacement,
                    PlayerSlotAssignmentStatus.SucceededAssigned,
                    "Replacement assignment begin failed.");
                AssertTrue(
                    replacement.CurrentAssignment.AssignmentSequence >
                        managerBegin.CurrentAssignment.AssignmentSequence,
                    "Replacement assignment sequence did not increase.");
                completed.Add("new-sequence-after-release");

                AssertStatus(
                    primary.TryConfirmCurrentAssignment(
                        primarySlotOne,
                        releasedToken,
                        "QA.CPSA1",
                        "stale-token"),
                    PlayerSlotAssignmentStatus.RejectedStaleToken,
                    "Stale assignment token was confirmed.");
                completed.Add("stale-token-rejected");

                AssertStatus(
                    primary.TryConfirmCurrentAssignment(
                        primarySlotOne,
                        sceneBegin.CurrentAssignment.AssignmentToken,
                        "QA.CPSA1",
                        "other-slot-token"),
                    PlayerSlotAssignmentStatus.RejectedTokenSlotMismatch,
                    "Assignment token from another Slot was accepted.");
                completed.Add("other-slot-token-rejected");

                PlayerParticipationRuntimeContext foreign = CreateJoinedContext(
                    profiles,
                    "primary",
                    1,
                    out PlayerSlotId foreignSlot,
                    out _);
                PlayerSlotAssignmentResult foreignBegin = foreign.BeginAssignment(
                    foreignSlot,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    foreign.CreateSessionAssignmentOwner(),
                    foreign.CreateHostBindingIdentity(),
                    "QA.CPSA1.Foreign",
                    "foreign-assignment");
                AssertStatus(
                    foreignBegin,
                    PlayerSlotAssignmentStatus.SucceededAssigned,
                    "Foreign context setup failed.");
                AssertStatus(
                    primary.TryConfirmCurrentAssignment(
                        primarySlotOne,
                        foreignBegin.CurrentAssignment.AssignmentToken,
                        "QA.CPSA1",
                        "foreign-token"),
                    PlayerSlotAssignmentStatus.RejectedForeignToken,
                    "Foreign Session assignment token was accepted.");
                completed.Add("foreign-token-rejected");

                PlayerParticipationRuntimeContext conflict = CreateJoinedContext(
                    profiles,
                    "host-conflict",
                    2,
                    out PlayerSlotId conflictSlotOne,
                    out PlayerSlotId conflictSlotTwo);
                RuntimeContentOwner conflictOwner =
                    conflict.CreateSessionAssignmentOwner();
                PlayerHostBindingIdentity sharedBinding =
                    conflict.CreateHostBindingIdentity();
                AssertStatus(
                    conflict.BeginAssignment(
                        conflictSlotOne,
                        PlayerSlotAssignmentOrigin.ManagerProvisioned,
                        conflictOwner,
                        sharedBinding,
                        "QA.CPSA1",
                        "host-owner"),
                    PlayerSlotAssignmentStatus.SucceededAssigned,
                    "Host conflict setup failed.");
                AssertStatus(
                    conflict.BeginAssignment(
                        conflictSlotTwo,
                        PlayerSlotAssignmentOrigin.SceneProvided,
                        RuntimeContentOwner.Route(
                            "qa.cpsa1.host-conflict.route",
                            "QA CPSA1 Host Conflict Route"),
                        sharedBinding,
                        "QA.CPSA1",
                        "host-conflict"),
                    PlayerSlotAssignmentStatus.RejectedHostBindingConflict,
                    "One Host binding was assigned to two Slots.");
                completed.Add("host-conflict-rejected");

                PlayerParticipationRuntimeContext validation = CreateJoinedContext(
                    profiles,
                    "validation",
                    1,
                    out PlayerSlotId validationSlot,
                    out _);
                RuntimeContentOwner validationOwner =
                    validation.CreateSessionAssignmentOwner();
                PlayerHostBindingIdentity validationHost =
                    validation.CreateHostBindingIdentity();
                AssertStatus(
                    validation.BeginAssignment(
                        validationSlot,
                        PlayerSlotAssignmentOrigin.None,
                        validationOwner,
                        validationHost,
                        "QA.CPSA1",
                        "invalid-origin"),
                    PlayerSlotAssignmentStatus.RejectedInvalidOrigin,
                    "Invalid origin was accepted.");
                AssertStatus(
                    validation.BeginAssignment(
                        validationSlot,
                        PlayerSlotAssignmentOrigin.SessionPersistent,
                        validationOwner,
                        validationHost,
                        "QA.CPSA1",
                        "reserved-origin"),
                    PlayerSlotAssignmentStatus.RejectedUnsupportedOrigin,
                    "Reserved SessionPersistent flow was implemented accidentally.");
                completed.Add("invalid-origin-rejected");

                AssertStatus(
                    validation.BeginAssignment(
                        validationSlot,
                        PlayerSlotAssignmentOrigin.ManagerProvisioned,
                        default,
                        validationHost,
                        "QA.CPSA1",
                        "invalid-owner"),
                    PlayerSlotAssignmentStatus.RejectedInvalidOwner,
                    "Invalid assignment owner was accepted.");
                completed.Add("invalid-owner-rejected");

                AssertStatus(
                    validation.BeginAssignment(
                        validationSlot,
                        PlayerSlotAssignmentOrigin.SceneProvided,
                        validationOwner,
                        validationHost,
                        "QA.CPSA1",
                        "scene-invalid-owner"),
                    PlayerSlotAssignmentStatus.RejectedInvalidOwner,
                    "Scene-Provided assignment accepted a Session owner.");
                completed.Add("scene-invalid-owner-rejected");

                RunOriginRollback(
                    profiles,
                    PlayerSlotAssignmentOrigin.ManagerProvisioned,
                    "manager-rollback");
                completed.Add("manager-rollback");

                RunOriginRollback(
                    profiles,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    "scene-rollback");
                completed.Add("scene-rollback");

                Debug.Log(
                    $"{LogPrefix} status='PASS' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} status='FAIL' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            finally
            {
                for (int index = 0; index < profiles.Count; index++)
                {
                    if (profiles[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(profiles[index]);
                    }
                }
            }
        }

        private static PlayerParticipationRuntimeContext CreateJoinedContext(
            ICollection<PlayerSlotProfile> profiles,
            string identityPrefix,
            int slotCount,
            out PlayerSlotId firstSlot,
            out PlayerSlotId secondSlot)
        {
            var configured = new List<PlayerSlotProfile>();
            for (int index = 0; index < slotCount; index++)
            {
                configured.Add(CreateProfile(
                    profiles,
                    $"QA CPSA1 {identityPrefix} {index + 1}",
                    $"qa.cpsa1.{identityPrefix}.{index + 1}"));
            }

            PlayerParticipationOperationResult creation =
                PlayerParticipationRuntimeContext.TryCreate(
                    configured,
                    slotCount,
                    true,
                    "QA.CPSA1",
                    "create-context",
                    out PlayerParticipationRuntimeContext context);
            AssertParticipationSucceeded(creation, "Context creation failed.");

            firstSlot = default;
            secondSlot = default;
            for (int index = 0; index < slotCount; index++)
            {
                PlayerParticipationOperationResult reservation =
                    context.TryReserveNextAvailableSlot(
                        "QA.CPSA1",
                        "reserve-slot");
                AssertParticipationSucceeded(
                    reservation,
                    "Slot reservation failed.");
                PlayerParticipationOperationResult joined =
                    context.TryMarkJoined(
                        reservation.ReservationToken,
                        "QA.CPSA1",
                        "mark-joined");
                AssertParticipationSucceeded(joined, "Mark Joined failed.");
                if (index == 0)
                {
                    firstSlot = joined.Slot.PlayerSlotId;
                }
                else if (index == 1)
                {
                    secondSlot = joined.Slot.PlayerSlotId;
                }
            }

            return context;
        }

        private static PlayerSlotProfile CreateProfile(
            ICollection<PlayerSlotProfile> profiles,
            string displayName,
            string slotId)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = slotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            profiles.Add(profile);
            return profile;
        }

        private static void RunOriginRollback(
            ICollection<PlayerSlotProfile> profiles,
            PlayerSlotAssignmentOrigin origin,
            string identityPrefix)
        {
            PlayerParticipationRuntimeContext context = CreateJoinedContext(
                profiles,
                identityPrefix,
                1,
                out PlayerSlotId slot,
                out _);
            PlayerSlotAssignmentResult begin = context.BeginAssignment(
                slot,
                origin,
                origin == PlayerSlotAssignmentOrigin.ManagerProvisioned
                    ? context.CreateSessionAssignmentOwner()
                    : RuntimeContentOwner.Activity(
                        $"qa.cpsa1.{identityPrefix}.activity",
                        $"QA CPSA1 {identityPrefix} Activity"),
                context.CreateHostBindingIdentity(),
                "QA.CPSA1",
                identityPrefix);
            AssertStatus(
                begin,
                PlayerSlotAssignmentStatus.SucceededAssigned,
                $"{origin} rollback setup failed.");
            AssertTrue(
                context.TryGetSlotSnapshot(
                    slot,
                    out PlayerSlotRuntimeSnapshot joinedSlot) &&
                joinedSlot.IsJoined,
                $"{origin} rollback setup has no Joined Slot evidence.");
            AssertStatus(
                context.ReleaseAssignment(
                    slot,
                    begin.CurrentAssignment.AssignmentToken,
                    "QA.CPSA1",
                    identityPrefix),
                PlayerSlotAssignmentStatus.SucceededReleased,
                $"{origin} rollback release failed.");
            PlayerParticipationOperationResult slotRollback =
                origin == PlayerSlotAssignmentOrigin.SceneProvided
                    ? context.TryAbandonCommittedSceneAdmission(
                        new SceneLocalPlayerAdmissionToken(
                            context.CreateSnapshot().ContextId,
                            1,
                            slot,
                            joinedSlot.Revision,
                            begin.CurrentAssignment.AssignmentToken),
                        "QA.CPSA1",
                        identityPrefix)
                    : context.TryAbandonJoinedSlotAfterAssignmentFailure(
                        slot,
                        "QA.CPSA1",
                        identityPrefix);
            AssertParticipationSucceeded(
                slotRollback,
                $"{origin} Slot rollback failed.");
            AssertTrue(
                slotRollback.Slot.AllocationState ==
                    PlayerSlotAllocationState.Available,
                $"{origin} rollback did not restore Slot availability.");
            AssertTrue(
                !context.TryGetCurrentAssignment(slot, out _),
                $"{origin} rollback retained a current assignment.");
        }

        private static void AssertCommonContract(
            PlayerSlotAssignmentSnapshot manager,
            PlayerSlotAssignmentSnapshot scene)
        {
            AssertTrue(manager.IsAssigned && scene.IsAssigned,
                "Both origins must produce Assigned snapshots.");
            AssertTrue(
                manager.AssignmentOrigin ==
                    PlayerSlotAssignmentOrigin.ManagerProvisioned &&
                scene.AssignmentOrigin ==
                    PlayerSlotAssignmentOrigin.SceneProvided,
                "Origin evidence changed.");
            AssertTrue(
                manager.AssignmentOwner.IsValid &&
                scene.AssignmentOwner.IsValid &&
                manager.AssignmentOwner.Scope == RuntimeContentScope.Session &&
                scene.AssignmentOwner.Scope is RuntimeContentScope.Activity or
                    RuntimeContentScope.Route,
                "Origins must retain their explicit Session or Activity/Route assignment owners.");
            AssertTrue(
                manager.AssignmentSequence > 0 &&
                scene.AssignmentSequence > 0 &&
                manager.AssignmentRevision == 1 &&
                scene.AssignmentRevision == 1 &&
                manager.AssignmentToken.IsValid &&
                scene.AssignmentToken.IsValid &&
                manager.HostBindingIdentity.IsValid &&
                scene.HostBindingIdentity.IsValid,
                "Common sequence, revision, token or Host binding contract is invalid.");
        }

        private static void AssertParticipationSucceeded(
            PlayerParticipationOperationResult result,
            string message)
        {
            if (result == null || !result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"{message} diagnostics='{result?.ToDiagnosticString()}'.");
            }
        }

        private static void AssertStatus(
            PlayerSlotAssignmentResult result,
            PlayerSlotAssignmentStatus expected,
            string message)
        {
            if (result == null || result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{result?.Status}' " +
                    $"diagnostics='{result?.ToDiagnosticString()}'.");
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'");
        }
    }
}

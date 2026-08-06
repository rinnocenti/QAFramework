using System;
using System.Collections.Generic;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.PlayerAssignment.Internal.Editor
{
    internal static class QaP3M4B1SceneLocalPlayerAdmissionTransactionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3M4B1 Scene Local Player Admission Transaction Smoke";

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();
            try
            {
                PlayerSlotProfile slot1 = CreateSlotProfile(
                    "QA P3M4B1 Slot 1",
                    "qa.p3m4b1.slot.1",
                    created);
                PlayerSlotProfile slot2 = CreateSlotProfile(
                    "QA P3M4B1 Slot 2",
                    "qa.p3m4b1.slot.2",
                    created);
                var orderedSlots = new[] { slot1, slot2 };

                RuntimeContentOwner activityOwner = RuntimeContentOwner.Activity(
                    "qa.p3m4b1.activity",
                    "QA P3M4B1 Activity");
                PlayerParticipationRuntimeContext mismatchContext =
                    CreateParticipationContext(orderedSlots, created);
                SceneLocalPlayerAdmissionRuntime mismatchRuntime =
                    CreateAdmissionRuntime(mismatchContext);
                Fixture mismatchFixture = CreateFixture(
                    "Mismatch",
                    slot2,
                    created);
                SceneLocalPlayerAdmissionRuntimeResult mismatch = InvokeAdmit(
                    mismatchRuntime,
                    mismatchFixture.Authoring,
                    activityOwner,
                    "QaP3M4B1",
                    "ordered-slot-mismatch");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedSlotOrderMismatch,
                    mismatch.Status,
                    mismatch.ToDiagnosticString());
                PlayerParticipationSnapshot mismatchSnapshot = CreateSnapshot(mismatchContext);
                AssertEqual(2, mismatchSnapshot.AvailableCount, "Rejected exact Slot request stranded capacity.");
                AssertEqual(0, mismatchSnapshot.ReservedCount, "Rejected exact Slot request stranded a reservation.");
                AssertFalse(mismatchFixture.Host.IsJoined, "Rejected exact Slot request joined its Host.");
                completed.Add("exact-ordered-slot-enforced");

                SceneLocalPlayerAdmissionRuntimeResult invalidOwner = InvokeAdmit(
                    mismatchRuntime,
                    mismatchFixture.Authoring,
                    mismatchContext.CreateSessionAssignmentOwner(),
                    "QaP3M4B1",
                    "invalid-scene-owner");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedInvalidRequest,
                    invalidOwner.Status,
                    invalidOwner.ToDiagnosticString());
                AssertEqual(
                    2,
                    CreateSnapshot(mismatchContext).AvailableCount,
                    "Invalid Scene owner mutated Slot availability.");
                AssertFalse(
                    mismatchFixture.Host.IsJoined,
                    "Invalid Scene owner mutated Host admission.");
                completed.Add("scene-invalid-owner-rejected");

                PlayerParticipationRuntimeContext context =
                    CreateParticipationContext(orderedSlots, created);
                PlayerParticipationSnapshot initialSnapshot = CreateSnapshot(context);
                AssertFalse(initialSnapshot.JoiningOpen, "QA context unexpectedly initialized with public joining open.");
                SceneLocalPlayerAdmissionRuntime runtime =
                    CreateAdmissionRuntime(context);
                var hostEvidence = new PlayerHostEvidenceProjection(context);
                Fixture fixture = CreateFixture(
                    "Nominal",
                    slot1,
                    created);
                bool hostActiveBefore = fixture.Host.gameObject.activeSelf;
                bool actorActiveBefore = fixture.Actor.gameObject.activeSelf;

                SceneLocalPlayerAdmissionRuntimeResult admitted = InvokeAdmit(
                    runtime,
                    fixture.Authoring,
                    activityOwner,
                    "QaP3M4B1",
                    "scene-authorized-admission");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted,
                    admitted.Status,
                    admitted.ToDiagnosticString());
                AssertTrue(admitted.Token.IsValid, "Successful Scene admission returned no typed token.");
                AssertTrue(
                    context.TryGetCurrentAssignment(
                        slot1.PlayerSlotId,
                        out PlayerSlotAssignmentSnapshot currentAssignment),
                    "Scene admission did not create a canonical current assignment.");
                AssertEqual(
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    currentAssignment.AssignmentOrigin,
                    "Scene admission created the wrong assignment origin.");
                AssertEqual(
                    activityOwner,
                    currentAssignment.AssignmentOwner,
                    "Scene admission did not retain the explicit Activity owner.");
                AssertEqual(
                    admitted.Token.AssignmentToken,
                    currentAssignment.AssignmentToken,
                    "Scene admission token does not reference the canonical assignment token.");
                PlayerHostEvidenceResult sceneRegistration =
                    hostEvidence.RegisterHostEvidence(
                        slot1.PlayerSlotId,
                        PlayerSlotAssignmentOrigin.SceneProvided,
                        admitted.Token.AssignmentToken,
                        currentAssignment.HostBindingIdentity,
                        fixture.Host,
                        "QaP3M4B1",
                        "scene-register-host-evidence");
                AssertHostEvidenceStatus(
                    sceneRegistration,
                    PlayerHostEvidenceStatus.SucceededRegistered,
                    "Scene admission did not register correlated Host evidence.");
                completed.Add("scene-activity-or-route-owner");
                completed.Add("scene-real-integration");
                completed.Add("scene-registers-correlated-host-evidence");
                completed.Add("manager-and-scene-use-common-host-evidence");

                AssertTrue(fixture.Host.IsJoined, "Successful Scene admission did not commit Host evidence.");
                AssertEqual(
                    slot1.PlayerSlotId,
                    fixture.Host.JoinedPlayerSlotId,
                    "Host committed a different Player Slot identity.");
                PlayerParticipationSnapshot joinedSnapshot = CreateSnapshot(context);
                AssertEqual(1, joinedSnapshot.JoinedCount, "Session did not commit one Joined Slot.");
                AssertEqual(0, joinedSnapshot.ReservedCount, "Committed Scene admission left a Reserved Slot.");
                completed.Add("host-slot-admission-committed");

                AssertEqual(0, joinedSnapshot.SelectedActorCount, "P3M4B1 unexpectedly selected an Actor.");
                AssertTrue(fixture.Actor.GetComponent<PlayerActorDeclaration>() != null,
                    "P3M4B1 changed the authored Logical Actor declaration.");
                completed.Add("actor-preparation-remains-out-of-scope");

                AssertTrue(
                    hostEvidence.TryGetHostEvidence(
                        slot1.PlayerSlotId,
                        out LocalPlayerHostAuthoring adoptionHost,
                        out PlayerHostEvidenceResult adoptionEvidence) &&
                    ReferenceEquals(adoptionHost, fixture.Host),
                    "Scene Actor adoption could not resolve confirmed Host evidence. " +
                    adoptionEvidence.ToDiagnosticString());
                completed.Add("scene-adoption-uses-confirmed-evidence");

                SceneLocalPlayerAdmissionRuntimeResult idempotent = InvokeAdmit(
                    runtime,
                    fixture.Authoring,
                    activityOwner,
                    "QaP3M4B1",
                    "idempotent-readmission");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyAdmitted,
                    idempotent.Status,
                    idempotent.ToDiagnosticString());
                AssertEqual(
                    admitted.Token,
                    idempotent.Token,
                    "Idempotent admission changed the typed admission token.");
                completed.Add("idempotent-readmission");

                SceneLocalPlayerAdmissionRuntimeResult foreignRelease = InvokeRelease(
                    runtime,
                    fixture.Authoring,
                    default,
                    "QaP3M4B1",
                    "foreign-token");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.RejectedForeignOrStaleToken,
                    foreignRelease.Status,
                    foreignRelease.ToDiagnosticString());
                AssertTrue(fixture.Host.IsJoined, "Foreign token rejection changed Host admission.");
                AssertEqual(1, CreateSnapshot(context).JoinedCount,
                    "Foreign token rejection changed Session Slot admission.");
                completed.Add("foreign-token-rejected");

                AssertHostEvidenceStatus(
                    hostEvidence.ReleaseHostEvidence(
                        slot1.PlayerSlotId,
                        admitted.Token.AssignmentToken,
                        currentAssignment.HostBindingIdentity,
                        fixture.Host,
                        "QaP3M4B1",
                        "release-scene-host-evidence"),
                    PlayerHostEvidenceStatus.SucceededReleased,
                    "Scene release did not release physical Host evidence first.");
                SceneLocalPlayerAdmissionRuntimeResult released = InvokeRelease(
                    runtime,
                    fixture.Authoring,
                    admitted.Token,
                    "QaP3M4B1",
                    "nominal-release");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededReleased,
                    released.Status,
                    released.ToDiagnosticString());
                PlayerParticipationSnapshot releasedSnapshot = CreateSnapshot(context);
                AssertEqual(0, releasedSnapshot.JoinedCount, "Release retained a Joined Slot.");
                AssertEqual(0, releasedSnapshot.LeavingCount, "Release stranded a Leaving Slot.");
                AssertEqual(2, releasedSnapshot.AvailableCount, "Release did not restore Slot availability.");
                AssertFalse(fixture.Host.IsJoined, "Release retained Host admission evidence.");
                AssertFalse(
                    context.TryGetCurrentAssignment(slot1.PlayerSlotId, out _),
                    "Release retained the canonical current assignment.");
                completed.Add("scene-real-release");
                completed.Add("scene-release-clears-host-evidence");

                AssertNotNull(fixture.Host, "Externally owned Host was destroyed by release.");
                AssertNotNull(fixture.Host.PlayerInput, "Externally owned PlayerInput was destroyed by release.");
                AssertEqual(hostActiveBefore, fixture.Host.gameObject.activeSelf,
                    "Release changed externally owned Host active state.");
                completed.Add("external-host-preserved");

                AssertNotNull(fixture.Actor, "Externally owned Logical Actor was destroyed by release.");
                AssertEqual(actorActiveBefore, fixture.Actor.gameObject.activeSelf,
                    "Release changed externally owned Logical Actor active state.");
                completed.Add("external-actor-preserved");

                SceneLocalPlayerAdmissionRuntimeResult alreadyReleased = InvokeRelease(
                    runtime,
                    fixture.Authoring,
                    default,
                    "QaP3M4B1",
                    "idempotent-release");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededAlreadyReleased,
                    alreadyReleased.Status,
                    alreadyReleased.ToDiagnosticString());
                completed.Add("idempotent-release");

                PlayerParticipationRuntimeContext compensationContext =
                    CreateParticipationContext(new[] { slot1 }, created);
                Fixture compensationFixture = CreateFixture(
                    "Compensation",
                    slot1,
                    created);
                var failingReleasePort =
                    new FailingAssignmentReleasePort(compensationContext);
                var compensationRuntime = new SceneLocalPlayerAdmissionRuntime(
                    compensationContext,
                    failingReleasePort);
                var compensationEvidence =
                    new PlayerHostEvidenceProjection(compensationContext);
                SceneLocalPlayerAdmissionRuntimeResult compensationAdmission =
                    InvokeAdmit(
                        compensationRuntime,
                        compensationFixture.Authoring,
                        activityOwner,
                        "QaP3M4B1",
                        "compensation-setup");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted,
                    compensationAdmission.Status,
                    compensationAdmission.ToDiagnosticString());
                AssertHostEvidenceStatus(
                    compensationEvidence.RegisterHostEvidence(
                        slot1.PlayerSlotId,
                        PlayerSlotAssignmentOrigin.SceneProvided,
                        compensationAdmission.Token.AssignmentToken,
                        compensationAdmission.Token.AssignmentToken.HostBindingIdentity,
                        compensationFixture.Host,
                        "QaP3M4B1",
                        "compensation-register-host-evidence"),
                    PlayerHostEvidenceStatus.SucceededRegistered,
                    "Scene compensation Host evidence setup failed.");
                AssertHostEvidenceStatus(
                    compensationEvidence.ReleaseHostEvidence(
                        slot1.PlayerSlotId,
                        compensationAdmission.Token.AssignmentToken,
                        compensationAdmission.Token.AssignmentToken.HostBindingIdentity,
                        compensationFixture.Host,
                        "QaP3M4B1",
                        "compensation-release-host-evidence"),
                    PlayerHostEvidenceStatus.SucceededReleased,
                    "Scene compensation could not release Host evidence.");
                SceneLocalPlayerAdmissionRuntimeResult compensatedRelease =
                    InvokeRelease(
                        compensationRuntime,
                        compensationFixture.Authoring,
                        compensationAdmission.Token,
                        "QaP3M4B1",
                        "forced-assignment-release-failure");
                AssertEqual(
                    SceneLocalPlayerAdmissionRuntimeStatus.FailedReleaseCommit,
                    compensatedRelease.Status,
                    compensatedRelease.ToDiagnosticString());
                AssertTrue(
                    compensationFixture.Host.IsJoined,
                    "Release compensation did not restore Host evidence.");
                AssertEqual(
                    1,
                    CreateSnapshot(compensationContext).JoinedCount,
                    "Release compensation did not restore the Joined Slot.");
                AssertTrue(
                    compensationContext.TryGetCurrentAssignment(
                        slot1.PlayerSlotId,
                        out PlayerSlotAssignmentSnapshot compensatedAssignment) &&
                    compensatedAssignment.AssignmentToken ==
                        compensationAdmission.Token.AssignmentToken &&
                    compensatedAssignment.AssignmentOwner == activityOwner,
                    "Release compensation did not preserve the current assignment.");
                AssertHostEvidenceStatus(
                    compensationEvidence.RegisterHostEvidence(
                        slot1.PlayerSlotId,
                        PlayerSlotAssignmentOrigin.SceneProvided,
                        compensationAdmission.Token.AssignmentToken,
                        compensationAdmission.Token.AssignmentToken.HostBindingIdentity,
                        compensationFixture.Host,
                        "QaP3M4B1",
                        "compensation-restore-host-evidence"),
                    PlayerHostEvidenceStatus.SucceededRegistered,
                    "Scene compensation did not restore the same Host evidence.");
                AssertTrue(
                    compensationEvidence.TryGetHostEvidence(
                        slot1.PlayerSlotId,
                        out LocalPlayerHostAuthoring restoredHost,
                        out _) &&
                    ReferenceEquals(restoredHost, compensationFixture.Host),
                    "Scene compensation restored different physical Host evidence.");
                completed.Add("scene-real-compensation");
                completed.Add("scene-compensation-restores-same-host-evidence");

                RunStaleSceneHostEvidenceReleaseCase(
                    slot1,
                    activityOwner,
                    created,
                    completed);

                Debug.Log(
                    "[P3M4B1_SCENE_LOCAL_PLAYER_ADMISSION_TRANSACTION_SMOKE] " +
                    $"status='PASS' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3M4B1_SCENE_LOCAL_PLAYER_ADMISSION_TRANSACTION_SMOKE] " +
                    $"status='FAIL' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private static PlayerParticipationRuntimeContext CreateParticipationContext(
            IReadOnlyList<PlayerSlotProfile> orderedSlots,
            ICollection<UnityEngine.Object> created)
        {
            PlayerParticipationOperationResult result =
                PlayerParticipationRuntimeContext.TryCreateWithActorSelectionPolicy(
                orderedSlots,
                orderedSlots.Count,
                false,
                PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                "QaP3M4B1",
                "initialize-scene-admission-context",
                out PlayerParticipationRuntimeContext context);
            AssertNotNull(result, "Player participation context factory returned no result.");
            AssertTrue(result.Succeeded, result.ToDiagnosticString());
            AssertNotNull(context, "Player participation context factory returned no context.");
            return context;
        }

        private static void RunStaleSceneHostEvidenceReleaseCase(
            PlayerSlotProfile slotProfile,
            RuntimeContentOwner owner,
            ICollection<UnityEngine.Object> created,
            ICollection<string> completed)
        {
            PlayerParticipationRuntimeContext context =
                CreateParticipationContext(new[] { slotProfile }, created);
            Fixture fixture = CreateFixture(
                "StaleEvidence",
                slotProfile,
                created);
            var runtime = new SceneLocalPlayerAdmissionRuntime(context);
            var hostEvidence = new PlayerHostEvidenceProjection(context);
            SceneLocalPlayerAdmissionRuntimeResult admission = InvokeAdmit(
                runtime,
                fixture.Authoring,
                owner,
                "QaP3M4B1",
                "stale-host-evidence-setup");
            AssertEqual(
                SceneLocalPlayerAdmissionRuntimeStatus.SucceededAdmitted,
                admission.Status,
                admission.ToDiagnosticString());
            AssertHostEvidenceStatus(
                hostEvidence.RegisterHostEvidence(
                    slotProfile.PlayerSlotId,
                    PlayerSlotAssignmentOrigin.SceneProvided,
                    admission.Token.AssignmentToken,
                    admission.Token.AssignmentToken.HostBindingIdentity,
                    fixture.Host,
                    "QaP3M4B1",
                    "stale-host-evidence-register"),
                PlayerHostEvidenceStatus.SucceededRegistered,
                "Stale Scene Host evidence setup failed.");
            PlayerSlotAssignmentResult assignmentRelease =
                context.ReleaseAssignment(
                    slotProfile.PlayerSlotId,
                    admission.Token.AssignmentToken,
                    "QaP3M4B1",
                    "make-scene-host-evidence-stale");
            AssertTrue(
                assignmentRelease.Succeeded,
                "Stale Scene Host evidence assignment release failed.");
            AssertHostEvidenceStatus(
                hostEvidence.ReleaseHostEvidence(
                    slotProfile.PlayerSlotId,
                    admission.Token.AssignmentToken,
                    admission.Token.AssignmentToken.HostBindingIdentity,
                    fixture.Host,
                    "QaP3M4B1",
                    "release-stale-scene-host-evidence"),
                PlayerHostEvidenceStatus.RejectedStaleAssignmentToken,
                "Scene release accepted stale physical Host evidence.");
            AssertTrue(
                hostEvidence.TryGetRetainedEvidence(
                    slotProfile.PlayerSlotId,
                    out _),
                "Rejected stale Scene release deleted retained Host evidence.");
            completed.Add("scene-release-with-stale-evidence-is-rejected");
        }

        private static SceneLocalPlayerAdmissionRuntime CreateAdmissionRuntime(
            PlayerParticipationRuntimeContext participationContext)
        {
            return new SceneLocalPlayerAdmissionRuntime(participationContext);
        }

        private static SceneLocalPlayerAdmissionRuntimeResult InvokeAdmit(
            SceneLocalPlayerAdmissionRuntime runtime,
            SceneLocalPlayerAdmissionAuthoring authoring,
            RuntimeContentOwner assignmentOwner,
            string source,
            string reason)
        {
            return runtime.TryAdmit(
                authoring,
                assignmentOwner,
                source,
                reason);
        }

        private static SceneLocalPlayerAdmissionRuntimeResult InvokeRelease(
            SceneLocalPlayerAdmissionRuntime runtime,
            SceneLocalPlayerAdmissionAuthoring authoring,
            SceneLocalPlayerAdmissionToken token,
            string source,
            string reason)
        {
            return runtime.TryRelease(
                authoring,
                token,
                source,
                reason);
        }

        private static PlayerParticipationSnapshot CreateSnapshot(
            PlayerParticipationRuntimeContext context)
        {
            return context.CreateSnapshot();
        }

        private static Fixture CreateFixture(
            string suffix,
            PlayerSlotProfile slotProfile,
            ICollection<UnityEngine.Object> created)
        {
            GameObject hostRoot = NewObject($"QA_P3M4B1_{suffix}_Host", created);
            PlayerInput input = hostRoot.AddComponent<PlayerInput>();
            LocalPlayerHostAuthoring host = hostRoot.AddComponent<LocalPlayerHostAuthoring>();
            GameObject actorMount = NewObject("ActorMount", created);
            actorMount.transform.SetParent(hostRoot.transform, false);
            SetObject(host, "playerInput", input);
            SetObject(host, "actorMount", actorMount.transform);

            GameObject actorRoot = NewObject($"QA_P3M4B1_{suffix}_Actor", created);
            actorRoot.transform.SetParent(actorMount.transform, false);
            PlayerActorDeclaration actor = actorRoot.AddComponent<PlayerActorDeclaration>();

            var actorProfile = ScriptableObject.CreateInstance<ActorProfile>();
            actorProfile.name = $"QA P3M4B1 {suffix} Actor Profile";
            created.Add(actorProfile);
            SetString(actorProfile, "actorProfileId", $"qa.p3m4b1.{suffix.ToLowerInvariant()}.actor");
            SetObject(actorProfile, "logicalActorHostPrefab", actorRoot);

            SceneLogicalPlayerActorEvidence evidence =
                actorRoot.AddComponent<SceneLogicalPlayerActorEvidence>();
            evidence.EditorSetEvidence(actorProfile, actorRoot, "qa-p3m4b1-evidence");

            SceneLocalPlayerAdmissionAuthoring authoring =
                hostRoot.AddComponent<SceneLocalPlayerAdmissionAuthoring>();
            SetObject(authoring, "playerSlotProfile", slotProfile);
            SetObject(authoring, "actorProfile", actorProfile);
            SetObject(authoring, "sceneLogicalPlayerActor", actor);
            authoring.EditorSetProfileEvidence(
                actorProfile,
                actorRoot,
                "qa-p3m4b1-fixture-evidence");

            return new Fixture(host, actor, authoring);
        }

        private static PlayerSlotProfile CreateSlotProfile(
            string name,
            string id,
            ICollection<UnityEngine.Object> created)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = name;
            created.Add(profile);
            SetString(profile, "playerSlotId", id);
            return profile;
        }

        private static GameObject NewObject(
            string name,
            ICollection<UnityEngine.Object> created)
        {
            var value = new GameObject(name);
            created.Add(value);
            return value;
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            AssertNotNull(property, $"Missing object property '{propertyName}' on '{target.GetType().Name}'.");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(
            UnityEngine.Object target,
            string propertyName,
            string value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            AssertNotNull(property, $"Missing string property '{propertyName}' on '{target.GetType().Name}'.");
            property.stringValue = value ?? string.Empty;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertFalse(bool condition, string message)
        {
            AssertTrue(!condition, message);
        }

        private static void AssertNotNull(object value, string message)
        {
            AssertTrue(value != null, message);
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static void AssertHostEvidenceStatus(
            PlayerHostEvidenceResult result,
            PlayerHostEvidenceStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{result.Status}' " +
                    $"diagnostics='{result.ToDiagnosticString()}'.");
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private sealed class Fixture
        {
            internal Fixture(
                LocalPlayerHostAuthoring host,
                PlayerActorDeclaration actor,
                SceneLocalPlayerAdmissionAuthoring authoring)
            {
                Host = host;
                Actor = actor;
                Authoring = authoring;
            }

            internal LocalPlayerHostAuthoring Host { get; }
            internal PlayerActorDeclaration Actor { get; }
            internal SceneLocalPlayerAdmissionAuthoring Authoring { get; }
        }

        private sealed class FailingAssignmentReleasePort :
            ISceneLocalPlayerAssignmentReleaseRuntimePort
        {
            private readonly PlayerParticipationRuntimeContext context;

            internal FailingAssignmentReleasePort(
                PlayerParticipationRuntimeContext context)
            {
                this.context = context;
            }

            public PlayerSlotAssignmentResult ReleaseAssignment(
                PlayerSlotId playerSlotId,
                PlayerSlotAssignmentToken expectedToken,
                string source,
                string reason)
            {
                context.TryGetCurrentAssignment(
                    playerSlotId,
                    out PlayerSlotAssignmentSnapshot current);
                return new PlayerSlotAssignmentResult(
                    PlayerSlotAssignmentStatus.RejectedStaleToken,
                    "ReleaseAssignment",
                    current,
                    current,
                    expectedToken,
                    source,
                    reason,
                    "Synthetic assignment release failure.");
            }
        }
    }
}

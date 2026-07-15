using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3K6ActivityGameplayReadyAdmissionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.6 Run Activity GameplayReady Admission Gate Smoke";

        private const string EvaluatorTypeName =
            "Immersive.Framework.PlayerParticipation.ActivityPlayerAdmissionEvaluator";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                AssertTrue(Application.isPlaying,
                    "P3K.6 Activity admission smoke must run in Play Mode.");

                Type evaluatorType = typeof(ActivityPlayerAdmissionEvaluationStatus)
                    .Assembly.GetType(EvaluatorTypeName, throwOnError: false);
                AssertNotNull(evaluatorType,
                    "P3K.6 ActivityPlayerAdmissionEvaluator type is missing.");

                MethodInfo evaluate = evaluatorType.GetMethod(
                    "Evaluate",
                    StaticAny,
                    null,
                    new[]
                    {
                        typeof(ActivityAsset),
                        typeof(PlayerParticipationSnapshot),
                        typeof(PlayerActorPreparationSnapshot),
                        typeof(PlayerGameplayAdmissionSnapshot)
                    },
                    null);
                AssertNotNull(evaluate,
                    "P3K.6 evaluator signature changed.");
                ValidateContractSurface(evaluatorType);
                completed.Add("contract-surface-valid");

                const string sessionId = "qa.p3k6.session";
                PlayerSlotProfile slotOneProfile = CreateSlotProfile(
                    "player.1", "Player 1", created);
                PlayerSlotProfile slotTwoProfile = CreateSlotProfile(
                    "player.2", "Player 2", created);
                PlayerSlotProfile slotThreeProfile = CreateSlotProfile(
                    "player.3", "Player 3", created);
                ActorProfile actorOneProfile = CreateActorProfile(
                    "actor.profile.one", "Actor One", created);
                ActorProfile actorTwoProfile = CreateActorProfile(
                    "actor.profile.two", "Actor Two", created);

                PlayerSlotId slotOne = slotOneProfile.PlayerSlotId;
                PlayerSlotId slotTwo = slotTwoProfile.PlayerSlotId;
                ActorProfileId actorOne = actorOneProfile.ActorProfileId;
                ActorProfileId actorTwo = actorTwoProfile.ActorProfileId;
                RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                    "qa.p3k6.activity", "P3K.6 Activity");

                PlayerActorPreparationSummary preparedOne = CreatePreparation(
                    sessionId, slotOne, actorOne, ActorId.From("actor.runtime.one"), owner, 1, 1);
                PlayerActorPreparationSummary preparedTwo = CreatePreparation(
                    sessionId, slotTwo, actorTwo, ActorId.From("actor.runtime.two"), owner, 2, 1);
                PlayerActorPreparationSummary unpreparedOne = CreateUnprepared(
                    sessionId, slotOne, actorOne, 1);
                PlayerActorPreparationSummary unpreparedTwo = CreateUnprepared(
                    sessionId, slotTwo, actorTwo, 1);

                PlayerGameplayAdmissionSummary readyOne = CreateAdmission(
                    preparedOne, PlayerGameplayAdmissionState.Ready, 1);
                PlayerGameplayAdmissionSummary blockedOne = CreateAdmission(
                    preparedOne, PlayerGameplayAdmissionState.BlockedByInputGate, 1);
                PlayerGameplayAdmissionSummary releaseFailedOne = CreateAdmission(
                    preparedOne, PlayerGameplayAdmissionState.ReleaseFailed, 1);
                PlayerGameplayAdmissionSummary readyTwo = CreateAdmission(
                    preparedTwo, PlayerGameplayAdmissionState.Ready, 2);
                PlayerGameplayAdmissionSummary notAdmittedOne = CreateNotAdmitted(
                    sessionId, slotOne, 0);
                PlayerGameplayAdmissionSummary notAdmittedTwo = CreateNotAdmitted(
                    sessionId, slotTwo, 0);

                PlayerParticipationRequirementsProfile none = CreateRequirements(
                    PlayerParticipationRequirementLevel.None, "None", created);
                PlayerParticipationRequirementsProfile joined = CreateRequirements(
                    PlayerParticipationRequirementLevel.JoinedSlots, "Joined", created);
                PlayerParticipationRequirementsProfile selected = CreateRequirements(
                    PlayerParticipationRequirementLevel.SelectedActors, "Selected", created);
                PlayerParticipationRequirementsProfile prepared = CreateRequirements(
                    PlayerParticipationRequirementLevel.LogicalActorsPrepared, "Prepared", created);
                PlayerParticipationRequirementsProfile gameplayReady = CreateRequirements(
                    PlayerParticipationRequirementLevel.GameplayReady, "Gameplay Ready", created);

                ActivityParticipationProjectionProfile noSlots = CreateProjection(
                    ActivityParticipationProjectionMode.NoSlots,
                    ActivityParticipationZeroParticipantPolicy.Allowed,
                    Array.Empty<PlayerSlotProfile>(),
                    "No Slots",
                    created);
                ActivityParticipationProjectionProfile allZeroAllowed = CreateProjection(
                    ActivityParticipationProjectionMode.AllJoinedSlots,
                    ActivityParticipationZeroParticipantPolicy.Allowed,
                    Array.Empty<PlayerSlotProfile>(),
                    "All Joined Zero Allowed",
                    created);
                ActivityParticipationProjectionProfile allZeroRejected = CreateProjection(
                    ActivityParticipationProjectionMode.AllJoinedSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected,
                    Array.Empty<PlayerSlotProfile>(),
                    "All Joined Zero Rejected",
                    created);
                ActivityParticipationProjectionProfile explicitOne = CreateProjection(
                    ActivityParticipationProjectionMode.ExplicitSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected,
                    new[] { slotOneProfile },
                    "Explicit One",
                    created);
                ActivityParticipationProjectionProfile explicitTwoOne = CreateProjection(
                    ActivityParticipationProjectionMode.ExplicitSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected,
                    new[] { slotTwoProfile, slotOneProfile },
                    "Explicit Two One",
                    created);
                ActivityParticipationProjectionProfile explicitUnknown = CreateProjection(
                    ActivityParticipationProjectionMode.ExplicitSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected,
                    new[] { slotThreeProfile },
                    "Explicit Unknown",
                    created);

                PlayerParticipationSnapshot noJoined = CreateParticipationSnapshot(
                    sessionId,
                    Slot(slotOneProfile, PlayerSlotAllocationState.Available, null, 1, 0),
                    Slot(slotTwoProfile, PlayerSlotAllocationState.Available, null, 1, 0));
                PlayerParticipationSnapshot joinedOneNoSelection = CreateParticipationSnapshot(
                    sessionId,
                    Slot(slotOneProfile, PlayerSlotAllocationState.Joined, null, 2, 0),
                    Slot(slotTwoProfile, PlayerSlotAllocationState.Available, null, 1, 0));
                PlayerParticipationSnapshot joinedOneSelected = CreateParticipationSnapshot(
                    sessionId,
                    Slot(slotOneProfile, PlayerSlotAllocationState.Joined, actorOneProfile, 3, 1),
                    Slot(slotTwoProfile, PlayerSlotAllocationState.Available, null, 1, 0));
                PlayerParticipationSnapshot joinedBothSelected = CreateParticipationSnapshot(
                    sessionId,
                    Slot(slotOneProfile, PlayerSlotAllocationState.Joined, actorOneProfile, 3, 1),
                    Slot(slotTwoProfile, PlayerSlotAllocationState.Joined, actorTwoProfile, 3, 1));
                PlayerParticipationSnapshot unavailableOne = CreateParticipationSnapshot(
                    sessionId,
                    Slot(slotOneProfile, PlayerSlotAllocationState.Unavailable, null, 1, 0),
                    Slot(slotTwoProfile, PlayerSlotAllocationState.Available, null, 1, 0));

                PlayerActorPreparationSnapshot preparationOnePending = CreatePreparationSnapshot(
                    sessionId, unpreparedOne, unpreparedTwo);
                PlayerActorPreparationSnapshot preparationOneReady = CreatePreparationSnapshot(
                    sessionId, preparedOne, unpreparedTwo);
                PlayerActorPreparationSnapshot preparationBothReady = CreatePreparationSnapshot(
                    sessionId, preparedOne, preparedTwo);

                PlayerGameplayAdmissionSnapshot admissionNone = CreateAdmissionSnapshot(
                    sessionId, notAdmittedOne, notAdmittedTwo);
                PlayerGameplayAdmissionSnapshot admissionBlockedOne = CreateAdmissionSnapshot(
                    sessionId, blockedOne, notAdmittedTwo);
                PlayerGameplayAdmissionSnapshot admissionReadyOne = CreateAdmissionSnapshot(
                    sessionId, readyOne, notAdmittedTwo);
                PlayerGameplayAdmissionSnapshot admissionReleaseFailedOne = CreateAdmissionSnapshot(
                    sessionId, releaseFailedOne, notAdmittedTwo);
                PlayerGameplayAdmissionSnapshot admissionBothReady = CreateAdmissionSnapshot(
                    sessionId, readyOne, readyTwo);
                PlayerGameplayAdmissionSnapshot admissionMixed = CreateAdmissionSnapshot(
                    sessionId, readyOne, CreateAdmission(
                        preparedTwo,
                        PlayerGameplayAdmissionState.BlockedByInputGate,
                        2));

                ActivityPlayerAdmissionEvaluationResult result;

                result = Evaluate(evaluate, null, null, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.MissingActivity);
                completed.Add("missing-activity-failed");

                ActivityAsset missingProjection = CreateActivity(
                    null, none, "Missing Projection", created);
                result = Evaluate(evaluate, missingProjection, null, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.MissingProjectionProfile);
                completed.Add("missing-projection-failed");

                ActivityAsset missingRequirements = CreateActivity(
                    noSlots, null, "Missing Requirements", created);
                result = Evaluate(evaluate, missingRequirements, null, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.MissingRequirementsProfile);
                completed.Add("missing-requirements-failed");

                ActivityAsset noPlayers = CreateActivity(
                    noSlots, none, "No Players", created);
                result = Evaluate(evaluate, noPlayers, null, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                AssertEqual(0, result.ProjectedSlotCount,
                    "NoSlots projected unexpected Slots.");
                completed.Add("no-slots-none-satisfied");

                ActivityAsset contradictory = CreateActivity(
                    noSlots, gameplayReady, "Contradictory", created);
                result = Evaluate(evaluate, contradictory, null, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.ContradictoryNoSlotsRequirement);
                completed.Add("no-slots-gameplayready-failed");

                ActivityAsset allNoneAllowed = CreateActivity(
                    allZeroAllowed, none, "Zero Allowed", created);
                result = Evaluate(evaluate, allNoneAllowed, noJoined, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                completed.Add("all-joined-zero-allowed-satisfied");

                ActivityAsset allJoinedRequired = CreateActivity(
                    allZeroRejected, joined, "Zero Rejected", created);
                result = Evaluate(evaluate, allJoinedRequired, noJoined, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Blocked,
                    ActivityPlayerAdmissionEvaluationCode.ZeroParticipantsRejected);
                completed.Add("all-joined-zero-rejected-blocked");

                ActivityAsset unknownSlot = CreateActivity(
                    explicitUnknown, joined, "Unknown Slot", created);
                result = Evaluate(evaluate, unknownSlot, noJoined, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.SlotNotConfigured);
                completed.Add("explicit-unconfigured-slot-failed");

                ActivityAsset explicitOrder = CreateActivity(
                    explicitTwoOne, none, "Explicit Order", created);
                result = Evaluate(evaluate, explicitOrder, joinedBothSelected, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                AssertEqual(slotTwo, result.Slots[0].PlayerSlotId,
                    "Explicit projection did not preserve authored order[0].");
                AssertEqual(slotOne, result.Slots[1].PlayerSlotId,
                    "Explicit projection did not preserve authored order[1].");
                completed.Add("explicit-order-preserved");

                ActivityAsset explicitJoined = CreateActivity(
                    explicitOne, joined, "Joined Required", created);
                result = Evaluate(evaluate, explicitJoined, noJoined, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.PendingResolution,
                    ActivityPlayerAdmissionEvaluationCode.SlotNotJoined);
                completed.Add("joined-available-pending");

                result = Evaluate(evaluate, explicitJoined, unavailableOne, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Blocked,
                    ActivityPlayerAdmissionEvaluationCode.SlotUnavailable);
                completed.Add("joined-unavailable-blocked");

                result = Evaluate(evaluate, explicitJoined, joinedOneNoSelection, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                completed.Add("joined-satisfied");

                ActivityAsset explicitSelected = CreateActivity(
                    explicitOne, selected, "Selected Required", created);
                result = Evaluate(evaluate, explicitSelected, joinedOneNoSelection, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.PendingResolution,
                    ActivityPlayerAdmissionEvaluationCode.SelectedActorMissing);
                completed.Add("selected-missing-pending");

                result = Evaluate(evaluate, explicitSelected, joinedOneSelected, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                completed.Add("selected-satisfied");

                ActivityAsset explicitPrepared = CreateActivity(
                    explicitOne, prepared, "Prepared Required", created);
                result = Evaluate(evaluate, explicitPrepared, joinedOneSelected, null, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.MissingPreparationSnapshot);
                completed.Add("prepared-snapshot-missing-failed");

                result = Evaluate(evaluate, explicitPrepared, joinedOneSelected,
                    preparationOnePending, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.PendingResolution,
                    ActivityPlayerAdmissionEvaluationCode.PreparationPending);
                completed.Add("prepared-unprepared-pending");

                result = Evaluate(evaluate, explicitPrepared, joinedOneSelected,
                    preparationOneReady, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                completed.Add("prepared-satisfied");

                PlayerActorPreparationSummary staleSelection = CreatePreparation(
                    sessionId, slotOne, actorOne, ActorId.From("actor.runtime.stale"), owner, 3, 2);
                PlayerActorPreparationSnapshot stalePreparation = CreatePreparationSnapshot(
                    sessionId, staleSelection, unpreparedTwo);
                result = Evaluate(evaluate, explicitPrepared, joinedOneSelected,
                    stalePreparation, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.PreparationIdentityMismatch);
                completed.Add("preparation-selection-stale-failed");

                ActivityAsset explicitGameplayReady = CreateActivity(
                    explicitOne, gameplayReady, "Gameplay Ready", created);
                result = Evaluate(evaluate, explicitGameplayReady, joinedOneSelected,
                    preparationOneReady, null);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.MissingGameplayAdmissionSnapshot);
                completed.Add("gameplay-snapshot-missing-failed");

                result = Evaluate(evaluate, explicitGameplayReady, joinedOneSelected,
                    preparationOneReady, admissionNone);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.PendingResolution,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionPending);
                completed.Add("gameplay-not-admitted-pending");

                result = Evaluate(evaluate, explicitGameplayReady, joinedOneSelected,
                    preparationOneReady, admissionBlockedOne);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.PendingResolution,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionBlockedByInputGate);
                AssertTrue(result.Slots[0].LogicalActorPrepared && !result.Slots[0].GameplayReady,
                    "Gate-blocked result lost prepared evidence or claimed readiness.");
                completed.Add("gameplay-gate-blocked-pending");

                result = Evaluate(evaluate, explicitGameplayReady, joinedOneSelected,
                    preparationOneReady, admissionReadyOne);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                AssertTrue(result.CanActivate && result.Slots[0].GameplayReady,
                    "Ready result did not permit Activity activation.");
                completed.Add("gameplay-ready-satisfied");

                result = Evaluate(evaluate, explicitGameplayReady, joinedOneSelected,
                    preparationOneReady, admissionReleaseFailedOne);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionReleaseFailed);
                completed.Add("gameplay-release-failed-failed");

                PlayerActorPreparationSummary foreignPrepared = CreatePreparation(
                    sessionId, slotOne, actorTwo, ActorId.From("actor.runtime.foreign"), owner, 4, 1);
                PlayerGameplayAdmissionSummary foreignAdmission = CreateAdmission(
                    foreignPrepared, PlayerGameplayAdmissionState.Ready, 3);
                PlayerGameplayAdmissionSnapshot foreignAdmissionSnapshot = CreateAdmissionSnapshot(
                    sessionId, foreignAdmission, notAdmittedTwo);
                result = Evaluate(evaluate, explicitGameplayReady, joinedOneSelected,
                    preparationOneReady, foreignAdmissionSnapshot);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionIdentityMismatch);
                completed.Add("gameplay-admission-identity-mismatch-failed");

                PlayerGameplayAdmissionSnapshot foreignSessionAdmission = CreateAdmissionSnapshot(
                    "qa.p3k6.foreign",
                    CreateNotAdmitted("qa.p3k6.foreign", slotOne, 0),
                    CreateNotAdmitted("qa.p3k6.foreign", slotTwo, 0));
                result = Evaluate(evaluate, explicitGameplayReady, joinedOneSelected,
                    preparationOneReady, foreignSessionAdmission);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.SessionMismatch);
                completed.Add("session-mismatch-failed");

                PlayerGameplayAdmissionSnapshot rosterMismatchAdmission = CreateAdmissionSnapshot(
                    sessionId, readyOne);
                result = Evaluate(evaluate, explicitGameplayReady, joinedOneSelected,
                    preparationOneReady, rosterMismatchAdmission);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.SnapshotRosterMismatch);
                completed.Add("gameplay-roster-mismatch-failed");

                ActivityAsset allGameplayReady = CreateActivity(
                    allZeroRejected, gameplayReady, "All Gameplay Ready", created);
                result = Evaluate(evaluate, allGameplayReady, joinedBothSelected,
                    preparationBothReady, admissionMixed);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.PendingResolution,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionBlockedByInputGate);
                AssertEqual(2, result.ProjectedSlotCount,
                    "Multi-Slot evaluation lost projected Slots.");
                AssertEqual(1, result.PendingSlotCount,
                    "Multi-Slot evaluation did not aggregate one pending Slot.");
                completed.Add("multi-slot-aggregate-pending");

                result = Evaluate(evaluate, allGameplayReady, joinedBothSelected,
                    preparationBothReady, admissionBothReady);
                AssertStatus(result, ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                AssertEqual(2, result.SatisfiedSlotCount,
                    "Multi-Slot ready evaluation did not satisfy both Slots.");
                completed.Add("multi-slot-all-ready-satisfied");

                ValidateNoUnityReferences(typeof(ActivityPlayerAdmissionEvaluationResult));
                ValidateNoUnityReferences(typeof(ActivityPlayerAdmissionSlotResult));
                completed.Add("public-result-no-unity-references");

                AssertEqual(30, completed.Count,
                    "P3K.6 smoke case count changed.");

                Debug.Log(
                    $"[P3K6_ACTIVITY_GAMEPLAYREADY_ADMISSION_GATE_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception root = exception is TargetInvocationException invocation &&
                    invocation.InnerException != null
                        ? invocation.InnerException
                        : exception;
                Debug.LogError(
                    $"[P3K6_ACTIVITY_GAMEPLAYREADY_ADMISSION_GATE_SMOKE] " +
                    $"status='Failed' exception='{root.GetType().Name}' " +
                    $"message='{Escape(root.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                Debug.LogException(root);
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

        private readonly struct SlotSpec
        {
            internal SlotSpec(
                PlayerSlotProfile profile,
                PlayerSlotAllocationState state,
                ActorProfile selectedActor,
                int revision,
                int selectionRevision)
            {
                Profile = profile;
                State = state;
                SelectedActor = selectedActor;
                Revision = revision;
                SelectionRevision = selectionRevision;
            }

            internal PlayerSlotProfile Profile { get; }
            internal PlayerSlotAllocationState State { get; }
            internal ActorProfile SelectedActor { get; }
            internal int Revision { get; }
            internal int SelectionRevision { get; }
        }

        private static SlotSpec Slot(
            PlayerSlotProfile profile,
            PlayerSlotAllocationState state,
            ActorProfile selectedActor,
            int revision,
            int selectionRevision) =>
            new SlotSpec(profile, state, selectedActor, revision, selectionRevision);

        private static ActivityPlayerAdmissionEvaluationResult Evaluate(
            MethodInfo evaluate,
            ActivityAsset activity,
            PlayerParticipationSnapshot participation,
            PlayerActorPreparationSnapshot preparation,
            PlayerGameplayAdmissionSnapshot admission)
        {
            return (ActivityPlayerAdmissionEvaluationResult)evaluate.Invoke(
                null,
                new object[] { activity, participation, preparation, admission });
        }

        private static ActivityAsset CreateActivity(
            ActivityParticipationProjectionProfile projection,
            PlayerParticipationRequirementsProfile requirements,
            string activityName,
            List<UnityEngine.Object> created)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = activityName;
            created.Add(activity);
            SerializedObject serialized = new SerializedObject(activity);
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("playerParticipationProjectionProfile")
                .objectReferenceValue = projection;
            serialized.FindProperty("playerParticipationRequirementsProfile")
                .objectReferenceValue = requirements;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static ActivityParticipationProjectionProfile CreateProjection(
            ActivityParticipationProjectionMode mode,
            ActivityParticipationZeroParticipantPolicy zeroPolicy,
            PlayerSlotProfile[] explicitSlots,
            string name,
            List<UnityEngine.Object> created)
        {
            ActivityParticipationProjectionProfile profile =
                ScriptableObject.CreateInstance<ActivityParticipationProjectionProfile>();
            profile.name = name;
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("projectionMode").intValue = (int)mode;
            serialized.FindProperty("zeroParticipantPolicy").intValue = (int)zeroPolicy;
            SerializedProperty array = serialized.FindProperty("explicitSlotProfiles");
            array.arraySize = explicitSlots?.Length ?? 0;
            for (int index = 0; index < array.arraySize; index++)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = explicitSlots[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static PlayerParticipationRequirementsProfile CreateRequirements(
            PlayerParticipationRequirementLevel level,
            string name,
            List<UnityEngine.Object> created)
        {
            PlayerParticipationRequirementsProfile profile =
                ScriptableObject.CreateInstance<PlayerParticipationRequirementsProfile>();
            profile.name = name;
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("requirementLevel").intValue = (int)level;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static PlayerSlotProfile CreateSlotProfile(
            string id,
            string name,
            List<UnityEngine.Object> created)
        {
            PlayerSlotProfile profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = name;
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static ActorProfile CreateActorProfile(
            string id,
            string name,
            List<UnityEngine.Object> created)
        {
            ActorProfile profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = name;
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue = id;
            serialized.FindProperty("displayName").stringValue = name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static PlayerParticipationSnapshot CreateParticipationSnapshot(
            string sessionId,
            params SlotSpec[] specs)
        {
            var slots = new PlayerSlotRuntimeSnapshot[specs.Length];
            ConstructorInfo slotConstructor = typeof(PlayerSlotRuntimeSnapshot).GetConstructor(
                InstanceAny,
                null,
                new[]
                {
                    typeof(int),
                    typeof(PlayerSlotProfile),
                    typeof(PlayerSlotId),
                    typeof(PlayerSlotAllocationState),
                    typeof(PlayerSlotReservationToken),
                    typeof(int),
                    typeof(string),
                    typeof(string),
                    typeof(ActorProfile),
                    typeof(int),
                    typeof(string),
                    typeof(string)
                },
                null);
            AssertNotNull(slotConstructor,
                "PlayerSlotRuntimeSnapshot constructor changed.");

            for (int index = 0; index < specs.Length; index++)
            {
                SlotSpec spec = specs[index];
                slots[index] = (PlayerSlotRuntimeSnapshot)slotConstructor.Invoke(
                    new object[]
                    {
                        index,
                        spec.Profile,
                        spec.Profile.PlayerSlotId,
                        spec.State,
                        default(PlayerSlotReservationToken),
                        spec.Revision,
                        nameof(QaP3K6ActivityGameplayReadyAdmissionSmoke),
                        "synthetic-participation",
                        spec.SelectedActor,
                        spec.SelectionRevision,
                        nameof(QaP3K6ActivityGameplayReadyAdmissionSmoke),
                        "synthetic-selection"
                    });
            }

            ConstructorInfo snapshotConstructor = typeof(PlayerParticipationSnapshot)
                .GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(int),
                        typeof(bool),
                        typeof(int),
                        typeof(bool),
                        typeof(PlayerActorSelectionPolicyProfile),
                        typeof(PlayerSlotRuntimeSnapshot[]),
                        typeof(PlayerParticipationOperationStatus),
                        typeof(string)
                    },
                    null);
            AssertNotNull(snapshotConstructor,
                "PlayerParticipationSnapshot constructor changed.");
            return (PlayerParticipationSnapshot)snapshotConstructor.Invoke(
                new object[]
                {
                    sessionId,
                    1,
                    true,
                    specs.Length,
                    false,
                    null,
                    slots,
                    PlayerParticipationOperationStatus.Succeeded,
                    "Synthetic participation snapshot."
                });
        }

        private static PlayerActorPreparationSummary CreatePreparation(
            string sessionId,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            int revision,
            int selectionRevision)
        {
            PlayerActorMaterializationOperationId operationId = CreateOperationId(
                sessionId, owner, playerSlotId, revision);
            RuntimeContentIdentity identity = RuntimeContentIdentity.From(
                owner,
                $"qa.p3k6.content.{playerSlotId.Value.Value}.{revision}");
            PlayerActorMaterializationSnapshot materialization = CreateMaterializationSnapshot(
                operationId, identity, playerSlotId, actorProfileId, actorId, revision);
            return CreatePreparationSummary(
                sessionId,
                playerSlotId,
                PlayerActorPreparationState.Prepared,
                actorProfileId,
                selectionRevision,
                materialization);
        }

        private static PlayerActorPreparationSummary CreateUnprepared(
            string sessionId,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            int selectionRevision)
        {
            return CreatePreparationSummary(
                sessionId,
                playerSlotId,
                PlayerActorPreparationState.Unprepared,
                actorProfileId,
                selectionRevision,
                default);
        }

        private static PlayerActorPreparationSummary CreatePreparationSummary(
            string sessionId,
            PlayerSlotId playerSlotId,
            PlayerActorPreparationState state,
            ActorProfileId actorProfileId,
            int selectionRevision,
            PlayerActorMaterializationSnapshot materialization)
        {
            ConstructorInfo constructor = typeof(PlayerActorPreparationSummary).GetConstructor(
                InstanceAny,
                null,
                new[]
                {
                    typeof(string),
                    typeof(PlayerSlotId),
                    typeof(PlayerActorPreparationState),
                    typeof(ActorProfileId),
                    typeof(int),
                    typeof(PlayerActorMaterializationSnapshot),
                    typeof(string),
                    typeof(string),
                    typeof(string)
                },
                null);
            AssertNotNull(constructor,
                "PlayerActorPreparationSummary constructor changed.");
            return (PlayerActorPreparationSummary)constructor.Invoke(
                new object[]
                {
                    sessionId,
                    playerSlotId,
                    state,
                    actorProfileId,
                    selectionRevision,
                    materialization,
                    nameof(QaP3K6ActivityGameplayReadyAdmissionSmoke),
                    "synthetic-preparation",
                    "Synthetic preparation evidence."
                });
        }

        private static PlayerActorMaterializationOperationId CreateOperationId(
            string sessionId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            int revision)
        {
            MethodInfo method = typeof(PlayerActorMaterializationOperationId)
                .GetMethod("TryCreate", StaticAny);
            AssertNotNull(method,
                "PlayerActorMaterializationOperationId.TryCreate is missing.");
            object[] arguments =
            {
                sessionId,
                owner,
                playerSlotId,
                revision,
                default(PlayerActorMaterializationOperationId),
                null
            };
            bool succeeded = (bool)method.Invoke(null, arguments);
            AssertTrue(succeeded,
                $"Synthetic materialization operation identity failed. {arguments[5]}");
            return (PlayerActorMaterializationOperationId)arguments[4];
        }

        private static PlayerActorMaterializationSnapshot CreateMaterializationSnapshot(
            PlayerActorMaterializationOperationId operationId,
            RuntimeContentIdentity identity,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            int revision)
        {
            ConstructorInfo constructor = typeof(PlayerActorMaterializationSnapshot).GetConstructor(
                InstanceAny,
                null,
                new[]
                {
                    typeof(PlayerActorMaterializationOperationId),
                    typeof(RuntimeContentIdentity),
                    typeof(PlayerSlotId),
                    typeof(ActorProfileId),
                    typeof(ActorId),
                    typeof(int),
                    typeof(PlayerActorMaterializationState),
                    typeof(string),
                    typeof(string)
                },
                null);
            AssertNotNull(constructor,
                "PlayerActorMaterializationSnapshot constructor changed.");
            return (PlayerActorMaterializationSnapshot)constructor.Invoke(
                new object[]
                {
                    operationId,
                    identity,
                    playerSlotId,
                    actorProfileId,
                    actorId,
                    revision,
                    PlayerActorMaterializationState.Active,
                    nameof(QaP3K6ActivityGameplayReadyAdmissionSmoke),
                    "synthetic-materialization"
                });
        }

        private static PlayerActorPreparationSnapshot CreatePreparationSnapshot(
            string sessionId,
            params PlayerActorPreparationSummary[] preparations)
        {
            ConstructorInfo constructor = typeof(PlayerActorPreparationSnapshot).GetConstructor(
                InstanceAny,
                null,
                new[]
                {
                    typeof(string),
                    typeof(int),
                    typeof(PlayerActorPreparationSummary[]),
                    typeof(PlayerActorMaterializationSnapshot[]),
                    typeof(PlayerActorPreparationStatus),
                    typeof(string)
                },
                null);
            AssertNotNull(constructor,
                "PlayerActorPreparationSnapshot constructor changed.");
            return (PlayerActorPreparationSnapshot)constructor.Invoke(
                new object[]
                {
                    sessionId,
                    1,
                    preparations,
                    Array.Empty<PlayerActorMaterializationSnapshot>(),
                    PlayerActorPreparationStatus.SucceededPrepared,
                    "Synthetic preparation snapshot."
                });
        }

        private static PlayerGameplayAdmissionSummary CreateNotAdmitted(
            string sessionId,
            PlayerSlotId playerSlotId,
            int revision)
        {
            MethodInfo method = typeof(PlayerGameplayAdmissionSummary).GetMethod(
                "NotAdmitted", StaticAny);
            AssertNotNull(method,
                "PlayerGameplayAdmissionSummary.NotAdmitted is missing.");
            return (PlayerGameplayAdmissionSummary)method.Invoke(
                null,
                new object[]
                {
                    sessionId,
                    playerSlotId,
                    revision,
                    nameof(QaP3K6ActivityGameplayReadyAdmissionSmoke),
                    "synthetic-not-admitted",
                    "Synthetic NotAdmitted evidence."
                });
        }

        private static PlayerGameplayAdmissionSummary CreateAdmission(
            PlayerActorPreparationSummary preparation,
            PlayerGameplayAdmissionState state,
            int revision)
        {
            string sessionId = preparation.SessionContextId;
            PlayerSlotId slot = preparation.PlayerSlotId;
            ActorProfileId actorProfile = preparation.PreparedActorProfileId;
            ActorId actorId = preparation.Materialization.ActorId;
            RuntimeContentOwner owner = preparation.Materialization.Owner;
            RuntimeContentIdentity identity = preparation.Materialization.RuntimeContentIdentity;
            int materializationRevision = preparation.Materialization.MaterializationRevision;
            int occupancyRevision = revision;
            int inputRevision = revision;
            int cameraRevision = revision;

            PlayerGameplayOccupancyToken occupancyToken = Construct<PlayerGameplayOccupancyToken>(
                new[]
                {
                    typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                    typeof(ActorProfileId), typeof(ActorId), typeof(PlayerActorPreparationToken),
                    typeof(RuntimeContentIdentity), typeof(int), typeof(int)
                },
                sessionId, owner, slot, actorProfile, actorId, preparation.Token,
                identity, materializationRevision, occupancyRevision);

            PlayerGameplayInputBindingToken inputToken = Construct<PlayerGameplayInputBindingToken>(
                new[]
                {
                    typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                    typeof(ActorProfileId), typeof(ActorId), typeof(PlayerActorPreparationToken),
                    typeof(PlayerGameplayOccupancyToken), typeof(RuntimeContentIdentity),
                    typeof(int), typeof(int), typeof(int)
                },
                sessionId, owner, slot, actorProfile, actorId, preparation.Token,
                occupancyToken, identity, materializationRevision, occupancyRevision,
                inputRevision);

            PlayerGameplayCameraEligibilityToken cameraToken =
                Construct<PlayerGameplayCameraEligibilityToken>(
                    new[]
                    {
                        typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                        typeof(ActorProfileId), typeof(ActorId), typeof(PlayerActorPreparationToken),
                        typeof(PlayerGameplayOccupancyToken), typeof(PlayerGameplayInputBindingToken),
                        typeof(RuntimeContentIdentity), typeof(int), typeof(int), typeof(int), typeof(int)
                    },
                    sessionId, owner, slot, actorProfile, actorId, preparation.Token,
                    occupancyToken, inputToken, identity, materializationRevision,
                    occupancyRevision, inputRevision, cameraRevision);

            PlayerGameplayAdmissionToken admissionToken = Construct<PlayerGameplayAdmissionToken>(
                new[]
                {
                    typeof(string), typeof(RuntimeContentOwner), typeof(PlayerSlotId),
                    typeof(ActorProfileId), typeof(ActorId), typeof(RuntimeContentIdentity),
                    typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)
                },
                sessionId, owner, slot, actorProfile, actorId, identity,
                materializationRevision, occupancyRevision, inputRevision,
                cameraRevision, revision);

            ConstructorInfo constructor = typeof(PlayerGameplayAdmissionSummary).GetConstructor(
                InstanceAny,
                null,
                new[]
                {
                    typeof(string), typeof(PlayerSlotId), typeof(PlayerGameplayAdmissionState),
                    typeof(ActorProfileId), typeof(ActorId), typeof(RuntimeContentOwner),
                    typeof(RuntimeContentIdentity), typeof(PlayerActorPreparationToken),
                    typeof(PlayerGameplayOccupancyToken), typeof(PlayerGameplayInputBindingToken),
                    typeof(PlayerGameplayCameraEligibilityToken), typeof(PlayerGameplayAdmissionToken),
                    typeof(PlayerGameplayCameraEligibilityState),
                    typeof(PlayerGameplayCameraRequiredness), typeof(bool), typeof(string),
                    typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                    typeof(int), typeof(string), typeof(string), typeof(string)
                },
                null);
            AssertNotNull(constructor,
                "PlayerGameplayAdmissionSummary constructor changed.");
            PlayerGameplayAdmissionSummary result =
                (PlayerGameplayAdmissionSummary)constructor.Invoke(
                    new object[]
                    {
                        sessionId,
                        slot,
                        state,
                        actorProfile,
                        actorId,
                        owner,
                        identity,
                        preparation.Token,
                        occupancyToken,
                        inputToken,
                        cameraToken,
                        admissionToken,
                        PlayerGameplayCameraEligibilityState.SkippedOptional,
                        PlayerGameplayCameraRequiredness.Optional,
                        false,
                        string.Empty,
                        string.Empty,
                        true,
                        false,
                        false,
                        false,
                        revision,
                        nameof(QaP3K6ActivityGameplayReadyAdmissionSmoke),
                        "synthetic-admission",
                        "Synthetic gameplay admission evidence."
                    });
            AssertTrue(result.IsValid,
                "Synthetic gameplay admission summary is invalid. " + result.ToDiagnosticString());
            return result;
        }

        private static PlayerGameplayAdmissionSnapshot CreateAdmissionSnapshot(
            string sessionId,
            params PlayerGameplayAdmissionSummary[] admissions)
        {
            ConstructorInfo constructor = typeof(PlayerGameplayAdmissionSnapshot).GetConstructor(
                InstanceAny,
                null,
                new[]
                {
                    typeof(string),
                    typeof(int),
                    typeof(PlayerGameplayAdmissionSummary[]),
                    typeof(PlayerGameplayAdmissionStatus),
                    typeof(string)
                },
                null);
            AssertNotNull(constructor,
                "PlayerGameplayAdmissionSnapshot constructor changed.");
            return (PlayerGameplayAdmissionSnapshot)constructor.Invoke(
                new object[]
                {
                    sessionId,
                    1,
                    admissions,
                    PlayerGameplayAdmissionStatus.SucceededReady,
                    "Synthetic gameplay admission snapshot."
                });
        }

        private static T Construct<T>(Type[] signature, params object[] arguments)
        {
            ConstructorInfo constructor = typeof(T).GetConstructor(
                InstanceAny, null, signature, null);
            AssertNotNull(constructor,
                $"{typeof(T).Name} constructor changed.");
            return (T)constructor.Invoke(arguments);
        }

        private static void ValidateContractSurface(Type evaluatorType)
        {
            AssertTrue(evaluatorType.IsAbstract && evaluatorType.IsSealed,
                "P3K.6 evaluator must remain a static authority, not a MonoBehaviour/context owner.");
            AssertTrue(Enum.IsDefined(
                    typeof(ActivityPlayerAdmissionEvaluationStatus),
                    ActivityPlayerAdmissionEvaluationStatus.Satisfied),
                "P3K.6 Satisfied status is missing.");
            AssertTrue(Enum.IsDefined(
                    typeof(ActivityPlayerAdmissionEvaluationStatus),
                    ActivityPlayerAdmissionEvaluationStatus.PendingResolution),
                "P3K.6 PendingResolution status is missing.");
            AssertTrue(Enum.IsDefined(
                    typeof(ActivityPlayerAdmissionEvaluationStatus),
                    ActivityPlayerAdmissionEvaluationStatus.Blocked),
                "P3K.6 Blocked status is missing.");
            AssertTrue(Enum.IsDefined(
                    typeof(ActivityPlayerAdmissionEvaluationStatus),
                    ActivityPlayerAdmissionEvaluationStatus.Failed),
                "P3K.6 Failed status is missing.");
        }

        private static void ValidateNoUnityReferences(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public);
            for (int index = 0; index < properties.Length; index++)
            {
                Type propertyType = properties[index].PropertyType;
                AssertTrue(!typeof(UnityEngine.Object).IsAssignableFrom(propertyType),
                    $"Public P3K.6 result property retains Unity object reference: " +
                    $"{type.Name}.{properties[index].Name} ({propertyType.Name}).");
            }
        }

        private static void AssertStatus(
            ActivityPlayerAdmissionEvaluationResult result,
            ActivityPlayerAdmissionEvaluationStatus expectedStatus,
            ActivityPlayerAdmissionEvaluationCode expectedCode)
        {
            AssertNotNull(result, "P3K.6 evaluator returned null.");
            if (result.Status != expectedStatus || result.Code != expectedCode)
            {
                throw new InvalidOperationException(
                    $"P3K.6 evaluation mismatch. ExpectedStatus='{expectedStatus}' " +
                    $"ActualStatus='{result.Status}' ExpectedCode='{expectedCode}' " +
                    $"ActualCode='{result.Code}'. {result.ToDiagnosticString()}");
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} Expected='{expected}' Actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

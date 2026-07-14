using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Synthetic technical smoke for P3K.2 effective runtime occupancy.
    /// </summary>
    public static class QaP3K2EffectiveRuntimeOccupancySmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.2 Run Effective Runtime Occupancy Smoke";
        private const string ContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayOccupancyRuntimeContext";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();

            try
            {
                Type contextType = ResolveContextType();
                ValidateContractSurface(contextType);
                completed.Add("contract-surface-valid");

                const string sessionId = "qa.p3k2.session";
                PlayerSlotId slotOne = PlayerSlotId.From("player.1");
                PlayerSlotId slotTwo = PlayerSlotId.From("player.2");

                PlayerActorPreparationSummary preparedOne = CreatePreparation(
                    sessionId,
                    slotOne,
                    ActorProfileId.From("actor.profile.one"),
                    ActorId.From("actor.runtime.one"),
                    RuntimeContentOwner.Activity(
                        "qa.p3k2.activity",
                        "P3K.2 Activity"),
                    1,
                    PlayerActorPreparationState.Prepared,
                    PlayerActorMaterializationState.Active);
                PlayerActorPreparationSummary preparedTwo = CreatePreparation(
                    sessionId,
                    slotTwo,
                    ActorProfileId.From("actor.profile.two"),
                    ActorId.From("actor.runtime.two"),
                    RuntimeContentOwner.Activity(
                        "qa.p3k2.activity",
                        "P3K.2 Activity"),
                    2,
                    PlayerActorPreparationState.Prepared,
                    PlayerActorMaterializationState.Active);
                PlayerActorPreparationSnapshot preparationSnapshot =
                    CreatePreparationSnapshot(
                        sessionId,
                        preparedOne,
                        preparedTwo);

                object context = CreateContext(
                    contextType,
                    preparationSnapshot);
                PlayerGameplayOccupancySnapshot initial = Snapshot(contextType, context);
                AssertTrue(initial.IsInitialized,
                    "P3K.2 occupancy snapshot is not initialized.");
                AssertEqual(2, initial.ConfiguredSlotCount,
                    "P3K.2 occupancy context lost configured Slots.");
                AssertEqual(0, initial.OccupiedCount,
                    "P3K.2 occupancy context did not start vacant.");
                completed.Add("context-initialized-from-preparation-roster");

                PlayerGameplayOccupancyResult occupiedOne = Confirm(
                    contextType,
                    context,
                    preparedOne,
                    "occupy-slot-one");
                AssertStatus(
                    occupiedOne,
                    PlayerGameplayOccupancyStatus.SucceededOccupied,
                    "Slot one occupancy confirmation failed.");
                AssertTrue(occupiedOne.CurrentSummary.IsOccupied,
                    "Slot one did not become Occupied.");
                AssertTrue(occupiedOne.CurrentSummary.Token.IsValid,
                    "Slot one has no functional occupancy token.");
                AssertEqual(preparedOne.Token,
                    occupiedOne.CurrentSummary.PreparationToken,
                    "Occupancy did not preserve the exact preparation token.");
                completed.Add("prepared-actor-confirmed-as-effective-occupant");

                PlayerGameplayOccupancyToken firstOccupancyToken =
                    occupiedOne.CurrentSummary.Token;
                int idempotentRevision = occupiedOne.Snapshot.Revision;
                PlayerGameplayOccupancyResult occupiedAgain = Confirm(
                    contextType,
                    context,
                    preparedOne,
                    "occupy-slot-one-again");
                AssertStatus(
                    occupiedAgain,
                    PlayerGameplayOccupancyStatus.SucceededAlreadyOccupied,
                    "Repeated occupancy confirmation was not idempotent.");
                AssertEqual(firstOccupancyToken,
                    occupiedAgain.CurrentSummary.Token,
                    "Idempotent occupancy confirmation changed its token.");
                AssertEqual(idempotentRevision,
                    occupiedAgain.Snapshot.Revision,
                    "Idempotent occupancy confirmation changed context revision.");
                completed.Add("confirm-is-idempotent");

                PlayerActorPreparationSummary replacementPreparation =
                    CreatePreparation(
                        sessionId,
                        slotOne,
                        ActorProfileId.From("actor.profile.replacement"),
                        ActorId.From("actor.runtime.replacement"),
                        RuntimeContentOwner.Activity(
                            "qa.p3k2.activity",
                            "P3K.2 Activity"),
                        3,
                        PlayerActorPreparationState.Prepared,
                        PlayerActorMaterializationState.Active);
                PlayerGameplayOccupancyResult occupiedConflict = Confirm(
                    contextType,
                    context,
                    replacementPreparation,
                    "occupy-conflicting-preparation");
                AssertStatus(
                    occupiedConflict,
                    PlayerGameplayOccupancyStatus.RejectedSlotAlreadyOccupied,
                    "A second preparation occupied an already occupied Slot.");
                AssertEqual(firstOccupancyToken,
                    occupiedConflict.CurrentSummary.Token,
                    "Rejected conflicting occupancy mutated the current occupant.");
                completed.Add("one-slot-one-effective-occupant");

                PlayerActorPreparationSummary duplicateActorPreparation =
                    CreatePreparation(
                        sessionId,
                        slotTwo,
                        ActorProfileId.From("actor.profile.duplicate"),
                        preparedOne.Materialization.ActorId,
                        RuntimeContentOwner.Activity(
                            "qa.p3k2.activity",
                            "P3K.2 Activity"),
                        5,
                        PlayerActorPreparationState.Prepared,
                        PlayerActorMaterializationState.Active);
                PlayerGameplayOccupancyResult duplicateActor = Confirm(
                    contextType,
                    context,
                    duplicateActorPreparation,
                    "duplicate-actor-identity");
                AssertStatus(
                    duplicateActor,
                    PlayerGameplayOccupancyStatus.RejectedPreparationAlreadyOccupied,
                    "The same runtime Actor identity occupied a second Slot.");
                completed.Add("one-preparation-one-effective-occupancy");

                PlayerActorPreparationSummary foreignSessionPreparation =
                    CreatePreparation(
                        "qa.p3k2.foreign-session",
                        slotOne,
                        ActorProfileId.From("actor.profile.foreign"),
                        ActorId.From("actor.runtime.foreign"),
                        RuntimeContentOwner.Activity(
                            "qa.p3k2.foreign-activity",
                            "Foreign Activity"),
                        1,
                        PlayerActorPreparationState.Prepared,
                        PlayerActorMaterializationState.Active);
                PlayerGameplayOccupancyResult foreignSession = Confirm(
                    contextType,
                    context,
                    foreignSessionPreparation,
                    "foreign-session-preparation");
                AssertStatus(
                    foreignSession,
                    PlayerGameplayOccupancyStatus.RejectedSessionMismatch,
                    "Foreign Session preparation was accepted.");
                completed.Add("foreign-session-preparation-rejected");

                PlayerActorPreparationSummary inactivePreparation =
                    CreatePreparation(
                        sessionId,
                        slotTwo,
                        ActorProfileId.From("actor.profile.inactive"),
                        ActorId.From("actor.runtime.inactive"),
                        RuntimeContentOwner.Activity(
                            "qa.p3k2.activity",
                            "P3K.2 Activity"),
                        4,
                        PlayerActorPreparationState.Prepared,
                        PlayerActorMaterializationState.StagedInactive);
                PlayerGameplayOccupancyResult inactive = Confirm(
                    contextType,
                    context,
                    inactivePreparation,
                    "inactive-preparation");
                AssertStatus(
                    inactive,
                    PlayerGameplayOccupancyStatus.RejectedPreparationNotReady,
                    "Inactive staged preparation became an effective occupant.");
                completed.Add("inactive-preparation-rejected");

                PlayerGameplayOccupancyResult occupiedTwo = Confirm(
                    contextType,
                    context,
                    preparedTwo,
                    "occupy-slot-two");
                AssertStatus(
                    occupiedTwo,
                    PlayerGameplayOccupancyStatus.SucceededOccupied,
                    "Slot two occupancy confirmation failed.");
                AssertEqual(2, occupiedTwo.Snapshot.OccupiedCount,
                    "Two configured Slots did not occupy independently.");
                completed.Add("two-slots-occupy-independently");

                PlayerGameplayOccupancyResult foreignRelease = Release(
                    contextType,
                    context,
                    slotOne,
                    occupiedTwo.CurrentSummary.Token,
                    "foreign-token-release");
                AssertStatus(
                    foreignRelease,
                    PlayerGameplayOccupancyStatus.RejectedForeignOrStaleOccupancy,
                    "Foreign Slot occupancy token was accepted.");
                AssertEqual(firstOccupancyToken,
                    foreignRelease.CurrentSummary.Token,
                    "Foreign release mutated the current Slot occupant.");
                completed.Add("foreign-occupancy-token-rejected");

                PlayerGameplayOccupancyResult releasedOne = Release(
                    contextType,
                    context,
                    slotOne,
                    firstOccupancyToken,
                    "release-slot-one");
                AssertStatus(
                    releasedOne,
                    PlayerGameplayOccupancyStatus.SucceededReleased,
                    "Slot one occupancy release failed.");
                AssertTrue(releasedOne.CurrentSummary.IsVacant,
                    "Slot one did not return to Vacant.");
                AssertEqual(1, releasedOne.Snapshot.OccupiedCount,
                    "Releasing Slot one disturbed Slot two.");
                completed.Add("release-is-token-guarded");

                PlayerGameplayOccupancyResult releasedAgain = Release(
                    contextType,
                    context,
                    slotOne,
                    default,
                    "release-slot-one-again");
                AssertStatus(
                    releasedAgain,
                    PlayerGameplayOccupancyStatus.SucceededAlreadyReleased,
                    "Repeated release without a token was not idempotent.");
                completed.Add("release-is-idempotent");

                PlayerGameplayOccupancyResult staleAfterRelease = Release(
                    contextType,
                    context,
                    slotOne,
                    firstOccupancyToken,
                    "stale-token-after-release");
                AssertStatus(
                    staleAfterRelease,
                    PlayerGameplayOccupancyStatus.RejectedForeignOrStaleOccupancy,
                    "Stale occupancy token was accepted after release.");
                completed.Add("stale-token-after-release-rejected");

                PlayerGameplayOccupancyResult reoccupied = Confirm(
                    contextType,
                    context,
                    replacementPreparation,
                    "reoccupy-slot-one");
                AssertStatus(
                    reoccupied,
                    PlayerGameplayOccupancyStatus.SucceededOccupied,
                    "Released Slot could not accept the current replacement preparation.");
                AssertTrue(reoccupied.CurrentSummary.Token != firstOccupancyToken,
                    "Reoccupancy reused a stale occupancy token.");
                AssertEqual(replacementPreparation.Token,
                    reoccupied.CurrentSummary.PreparationToken,
                    "Reoccupancy did not bind the replacement preparation.");
                completed.Add("reoccupancy-generates-new-token");

                PlayerGameplayOccupancyResult releasedReplacement = Release(
                    contextType,
                    context,
                    slotOne,
                    reoccupied.CurrentSummary.Token,
                    "release-replacement");
                AssertStatus(
                    releasedReplacement,
                    PlayerGameplayOccupancyStatus.SucceededReleased,
                    "Replacement occupancy release failed.");
                PlayerGameplayOccupancyResult releasedTwo = Release(
                    contextType,
                    context,
                    slotTwo,
                    occupiedTwo.CurrentSummary.Token,
                    "release-slot-two");
                AssertStatus(
                    releasedTwo,
                    PlayerGameplayOccupancyStatus.SucceededReleased,
                    "Slot two occupancy release failed.");
                AssertEqual(0, releasedTwo.Snapshot.OccupiedCount,
                    "Final release left effective occupancy records.");
                AssertEqual(2, releasedTwo.Snapshot.VacantCount,
                    "Final release did not restore all configured Slots to Vacant.");
                completed.Add("all-effective-occupancies-released");

                Debug.Log(
                    "[P3K2_EFFECTIVE_RUNTIME_OCCUPANCY_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K2_EFFECTIVE_RUNTIME_OCCUPANCY_SMOKE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static Type ResolveContextType()
        {
            Type type = typeof(PlayerGameplayOccupancyStatus).Assembly.GetType(
                ContextTypeName,
                false);
            AssertNotNull(type,
                $"P3K.2 runtime context type '{ContextTypeName}' is missing.");
            return type;
        }

        private static void ValidateContractSurface(Type contextType)
        {
            AssertTrue(!contextType.IsPublic,
                "P3K.2 runtime context must remain an internal package authority.");
            AssertNotNull(contextType.GetMethod("TryCreate", StaticAny),
                "P3K.2 runtime context has no TryCreate operation.");
            AssertNotNull(contextType.GetMethod("TryConfirmOccupancy", InstanceAny),
                "P3K.2 runtime context has no TryConfirmOccupancy operation.");
            AssertNotNull(contextType.GetMethod("TryReleaseOccupancy", InstanceAny),
                "P3K.2 runtime context has no TryReleaseOccupancy operation.");
            AssertNotNull(contextType.GetMethod("CreateSnapshot", InstanceAny),
                "P3K.2 runtime context has no CreateSnapshot operation.");
            AssertTrue(typeof(PlayerGameplayOccupancyToken).IsValueType,
                "P3K.2 occupancy token must be an immutable value type.");
            AssertTrue(typeof(PlayerGameplayOccupancySummary).IsValueType,
                "P3K.2 occupancy summary must be an immutable value type.");
        }

        private static object CreateContext(
            Type contextType,
            PlayerActorPreparationSnapshot preparationSnapshot)
        {
            MethodInfo method = contextType.GetMethod("TryCreate", StaticAny);
            object[] arguments =
            {
                preparationSnapshot,
                null,
                null
            };
            bool succeeded = (bool)method.Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.2 occupancy context creation failed. {arguments[2]}");
            AssertNotNull(arguments[1],
                "P3K.2 occupancy context creation returned no context.");
            return arguments[1];
        }

        private static PlayerGameplayOccupancyResult Confirm(
            Type contextType,
            object context,
            PlayerActorPreparationSummary preparation,
            string reason)
        {
            MethodInfo method =
                contextType.GetMethod("TryConfirmOccupancy", InstanceAny);
            return (PlayerGameplayOccupancyResult)method.Invoke(
                context,
                new object[]
                {
                    preparation,
                    nameof(QaP3K2EffectiveRuntimeOccupancySmoke),
                    reason
                });
        }

        private static PlayerGameplayOccupancyResult Release(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayOccupancyToken token,
            string reason)
        {
            MethodInfo method =
                contextType.GetMethod("TryReleaseOccupancy", InstanceAny);
            return (PlayerGameplayOccupancyResult)method.Invoke(
                context,
                new object[]
                {
                    playerSlotId,
                    token,
                    nameof(QaP3K2EffectiveRuntimeOccupancySmoke),
                    reason
                });
        }

        private static PlayerGameplayOccupancySnapshot Snapshot(
            Type contextType,
            object context)
        {
            MethodInfo method =
                contextType.GetMethod("CreateSnapshot", InstanceAny);
            return (PlayerGameplayOccupancySnapshot)method.Invoke(
                context,
                Array.Empty<object>());
        }

        private static PlayerActorPreparationSummary CreatePreparation(
            string sessionId,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            int revision,
            PlayerActorPreparationState preparationState,
            PlayerActorMaterializationState materializationState)
        {
            PlayerActorMaterializationOperationId operationId =
                CreateOperationId(
                    sessionId,
                    owner,
                    playerSlotId,
                    revision);
            RuntimeContentIdentity identity = RuntimeContentIdentity.From(
                owner,
                $"qa.p3k2.content.{playerSlotId.Value.Value}.{revision}");
            PlayerActorMaterializationSnapshot materialization =
                CreateMaterializationSnapshot(
                    operationId,
                    identity,
                    playerSlotId,
                    actorProfileId,
                    actorId,
                    revision,
                    materializationState);

            ConstructorInfo constructor =
                typeof(PlayerActorPreparationSummary).GetConstructor(
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
                "P3J.4 PlayerActorPreparationSummary constructor shape changed.");
            return (PlayerActorPreparationSummary)constructor.Invoke(
                new object[]
                {
                    sessionId,
                    playerSlotId,
                    preparationState,
                    actorProfileId,
                    revision,
                    materialization,
                    nameof(QaP3K2EffectiveRuntimeOccupancySmoke),
                    "synthetic-preparation",
                    "Synthetic current Player Actor preparation."
                });
        }

        private static PlayerActorMaterializationOperationId CreateOperationId(
            string sessionId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            int revision)
        {
            MethodInfo method =
                typeof(PlayerActorMaterializationOperationId).GetMethod(
                    "TryCreate",
                    StaticAny);
            AssertNotNull(method,
                "P3J.3 PlayerActorMaterializationOperationId.TryCreate is missing.");
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

        private static PlayerActorMaterializationSnapshot
            CreateMaterializationSnapshot(
                PlayerActorMaterializationOperationId operationId,
                RuntimeContentIdentity identity,
                PlayerSlotId playerSlotId,
                ActorProfileId actorProfileId,
                ActorId actorId,
                int revision,
                PlayerActorMaterializationState state)
        {
            ConstructorInfo constructor =
                typeof(PlayerActorMaterializationSnapshot).GetConstructor(
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
                "P3J.3 PlayerActorMaterializationSnapshot constructor shape changed.");
            return (PlayerActorMaterializationSnapshot)constructor.Invoke(
                new object[]
                {
                    operationId,
                    identity,
                    playerSlotId,
                    actorProfileId,
                    actorId,
                    revision,
                    state,
                    nameof(QaP3K2EffectiveRuntimeOccupancySmoke),
                    "synthetic-materialization"
                });
        }

        private static PlayerActorPreparationSnapshot CreatePreparationSnapshot(
            string sessionId,
            params PlayerActorPreparationSummary[] preparations)
        {
            ConstructorInfo constructor =
                typeof(PlayerActorPreparationSnapshot).GetConstructor(
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
                "P3J.4 PlayerActorPreparationSnapshot constructor shape changed.");
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

        private static void AssertStatus(
            PlayerGameplayOccupancyResult result,
            PlayerGameplayOccupancyStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} Expected='{expected}' Actual='{result.Status}'. " +
                    result.ToDiagnosticString());
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

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
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
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

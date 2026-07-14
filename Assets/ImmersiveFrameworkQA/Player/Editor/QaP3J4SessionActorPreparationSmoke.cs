using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Synthetic technical smoke for P3J.4 Session Actor preparation authority,
    /// idempotent prepare/release, guarded selection and transactional replacement.
    /// </summary>
    public static class QaP3J4SessionActorPreparationSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3J.4 Run Session Actor Preparation Smoke";
        private const string ParticipationContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeContext";
        private const string RuntimeContentRuntimeTypeName =
            "Immersive.Framework.RuntimeContent.RuntimeContentRuntime";
        private const string MaterializationAdapterTypeName =
            "Immersive.Framework.PlayerParticipation.AttachedPlayerActorMaterializationAdapter";
        private const string PreparationContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeContext";

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
                ValidateContractSurface(completed);
                using Fixture fixture = CreateFixture(created);
                completed.Add("session-preparation-context-composed");

                PlayerActorSelectionResult selectedOne = fixture.Select(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.ActorA,
                    0,
                    "select-slot-one");
                AssertSelectionStatus(
                    selectedOne,
                    PlayerActorSelectionStatus.SucceededSelected,
                    "Slot one initial Actor selection failed.");
                completed.Add("selection-routed-through-preparation-authority");

                PlayerActorPreparationResult preparedOne = fixture.Prepare(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.HostOne,
                    "prepare-slot-one");
                AssertPreparationStatus(
                    preparedOne,
                    PlayerActorPreparationStatus.SucceededPrepared,
                    "Slot one prepare failed.");
                AssertTrue(preparedOne.CurrentSummary.IsPrepared,
                    "Prepare result did not expose Prepared Session evidence.");
                AssertTrue(preparedOne.CurrentSummary.Token.IsValid,
                    "Prepare result has no functional preparation token.");
                completed.Add("selected-actor-prepared");

                AssertNotNull(preparedOne.MaterializationResult.LogicalActorHost,
                    "Prepared Actor has no physical Logical Actor Host.");
                AssertSame(fixture.HostOne.ActorMount,
                    preparedOne.MaterializationResult.LogicalActorHost.transform.parent,
                    "Prepared Actor is not attached below the explicit Actor Mount.");
                AssertTrue(preparedOne.MaterializationResult.LogicalActorHost.activeSelf,
                    "Prepared Actor was not activated after Session commit.");
                AssertEqual(1, fixture.CountLogicalActors(fixture.HostOne),
                    "Prepare produced an unexpected number of Logical Actors.");
                completed.Add("prepared-actor-active-under-stable-host");

                PlayerActorPreparationSnapshot preparedSnapshot = fixture.Snapshot;
                AssertEqual(1, preparedSnapshot.PreparedCount,
                    "Session preparation snapshot did not count one prepared Actor.");
                AssertEqual(1, fixture.RuntimeHandleCount,
                    "Prepare did not retain exactly one RuntimeContent handle.");
                AssertEqual(fixture.ContextId, preparedOne.CurrentSummary.SessionContextId,
                    "Preparation summary lost Session context identity.");
                completed.Add("per-slot-session-summary-preserved");

                PlayerActorPreparationToken firstToken = preparedOne.CurrentSummary.Token;
                int idempotentRevision = fixture.Snapshot.Revision;
                PlayerActorDeclaration firstDeclaration =
                    fixture.HostOne.ActorMount.GetComponentInChildren<PlayerActorDeclaration>(true);
                PlayerActorPreparationResult prepareAgain = fixture.Prepare(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.HostOne,
                    "prepare-slot-one-again");
                AssertPreparationStatus(
                    prepareAgain,
                    PlayerActorPreparationStatus.SucceededAlreadyPrepared,
                    "Repeated prepare was not idempotent.");
                AssertEqual(firstToken, prepareAgain.CurrentSummary.Token,
                    "Idempotent prepare changed the preparation token.");
                AssertEqual(idempotentRevision, fixture.Snapshot.Revision,
                    "Idempotent prepare changed Session preparation revision.");
                AssertSame(firstDeclaration,
                    fixture.HostOne.ActorMount.GetComponentInChildren<PlayerActorDeclaration>(true),
                    "Idempotent prepare replaced the physical Actor.");
                completed.Add("prepare-is-idempotent");

                PlayerActorSelectionResult guardedReplace = fixture.GuardedReplaceSelection(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.ActorC,
                    selectedOne.SelectionRevision,
                    "guarded-direct-replace");
                AssertSelectionStatus(
                    guardedReplace,
                    PlayerActorSelectionStatus.RejectedLogicalActorAlreadyPrepared,
                    "Direct Actor selection replacement bypassed prepared-Actor guard.");
                AssertSame(fixture.ActorA,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectedActorProfile,
                    "Rejected direct selection replacement mutated Session selection.");
                completed.Add("selection-mutation-guarded-while-prepared");

                PlayerActorSelectionResult selectedTwo = fixture.Select(
                    fixture.SlotTwo.PlayerSlotId,
                    fixture.ActorB,
                    0,
                    "select-slot-two");
                AssertSelectionStatus(
                    selectedTwo,
                    PlayerActorSelectionStatus.SucceededSelected,
                    "Slot two initial Actor selection failed.");
                PlayerActorPreparationResult preparedTwo = fixture.Prepare(
                    fixture.SlotTwo.PlayerSlotId,
                    fixture.HostTwo,
                    "prepare-slot-two");
                AssertPreparationStatus(
                    preparedTwo,
                    PlayerActorPreparationStatus.SucceededPrepared,
                    "Slot two prepare failed.");
                AssertEqual(2, fixture.Snapshot.PreparedCount,
                    "Two Slots did not retain independent preparation summaries.");
                AssertEqual(2, fixture.RuntimeHandleCount,
                    "Two prepared Slots did not retain independent RuntimeContent handles.");
                completed.Add("two-slots-prepare-independently");

                PlayerActorPreparationResult foreignRelease = fixture.Release(
                    fixture.SlotOne.PlayerSlotId,
                    preparedTwo.CurrentSummary.Token,
                    "foreign-token-release");
                AssertPreparationStatus(
                    foreignRelease,
                    PlayerActorPreparationStatus.RejectedForeignOrStalePreparation,
                    "Foreign Slot preparation token was accepted.");
                AssertEqual(firstToken,
                    fixture.GetSummary(fixture.SlotOne.PlayerSlotId).Token,
                    "Foreign release mutated the current Slot preparation.");
                completed.Add("foreign-preparation-token-rejected");

                PlayerActorPreparationResult releasedOne = fixture.Release(
                    fixture.SlotOne.PlayerSlotId,
                    firstToken,
                    "release-slot-one");
                AssertPreparationStatus(
                    releasedOne,
                    PlayerActorPreparationStatus.SucceededReleased,
                    "Prepared Actor release failed.");
                AssertTrue(releasedOne.CurrentSummary.IsUnprepared,
                    "Release did not return Slot preparation to Unprepared.");
                AssertEqual(0, fixture.CountLogicalActors(fixture.HostOne),
                    "Release left the Logical Actor under the stable host.");
                AssertSame(fixture.ActorA,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectedActorProfile,
                    "Release incorrectly cleared Actor selection.");
                AssertTrue(fixture.HostOne.IsJoined,
                    "Release destroyed or invalidated the stable Local Player Host.");
                completed.Add("release-preserves-joined-host-and-selection");

                PlayerActorPreparationResult releaseAgain = fixture.Release(
                    fixture.SlotOne.PlayerSlotId,
                    default,
                    "release-slot-one-again");
                AssertPreparationStatus(
                    releaseAgain,
                    PlayerActorPreparationStatus.SucceededAlreadyReleased,
                    "Repeated release was not idempotent.");
                completed.Add("release-is-idempotent");

                PlayerActorPreparationResult rePreparedOne = fixture.Prepare(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.HostOne,
                    "reprepare-slot-one");
                AssertPreparationStatus(
                    rePreparedOne,
                    PlayerActorPreparationStatus.SucceededPrepared,
                    "Slot one re-prepare failed.");
                AssertTrue(rePreparedOne.CurrentSummary.Token != firstToken,
                    "Re-materialization reused a stale preparation token.");
                AssertTrue(rePreparedOne.CurrentSummary.Materialization.ActorId !=
                    preparedOne.CurrentSummary.Materialization.ActorId,
                    "Re-materialization reused the previous runtime ActorId.");
                completed.Add("rematerialization-generates-new-identity");

                PlayerActorPreparationToken beforeReplacementToken =
                    rePreparedOne.CurrentSummary.Token;
                LocalPlayerHostAuthoring stableHostReference = fixture.HostOne;
                PlayerInput stablePlayerInputReference = fixture.HostOne.PlayerInput;
                PlayerActorPreparationResult replaced = fixture.Replace(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.ActorC,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectionRevision,
                    beforeReplacementToken,
                    fixture.ScopeContext,
                    "replace-slot-one");
                AssertPreparationStatus(
                    replaced,
                    PlayerActorPreparationStatus.SucceededReplaced,
                    "Prepared Actor replacement failed.");
                AssertEqual(fixture.ActorC.ActorProfileId,
                    replaced.CurrentSummary.PreparedActorProfileId,
                    "Replacement summary did not expose the replacement ActorProfile.");
                AssertTrue(replaced.CurrentSummary.Token != beforeReplacementToken,
                    "Replacement reused the previous preparation token.");
                AssertSame(fixture.ActorC,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectedActorProfile,
                    "Replacement did not commit Session Actor selection.");
                AssertEqual(1, fixture.CountLogicalActors(fixture.HostOne),
                    "Replacement left more than one Logical Actor attached.");
                AssertTrue(fixture.HostOne.ActorMount
                    .GetComponentInChildren<PlayerActorDeclaration>(true)
                    .gameObject.activeInHierarchy,
                    "Replacement Actor is not active.");
                completed.Add("prepared-actor-replaced-transactionally");

                AssertSame(stableHostReference, fixture.HostOne,
                    "Replacement changed the stable Local Player Host.");
                AssertSame(stablePlayerInputReference, fixture.HostOne.PlayerInput,
                    "Replacement changed PlayerInput authority.");
                AssertTrue(fixture.HostOne.HasJoinedSlot,
                    "Replacement removed joined Slot evidence from the stable host.");
                completed.Add("replacement-preserves-stable-playerinput-host");

                PlayerActorPreparationToken replacementToken =
                    replaced.CurrentSummary.Token;
                PlayerActorPreparationResult staleReplacement = fixture.Replace(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.ActorA,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectionRevision,
                    beforeReplacementToken,
                    fixture.ScopeContext,
                    "stale-token-replacement");
                AssertPreparationStatus(
                    staleReplacement,
                    PlayerActorPreparationStatus.RejectedForeignOrStalePreparation,
                    "Stale preparation token was accepted for replacement.");
                AssertEqual(replacementToken,
                    fixture.GetSummary(fixture.SlotOne.PlayerSlotId).Token,
                    "Stale replacement mutated current preparation.");
                completed.Add("stale-preparation-token-rejected");

                int handlesBeforeDuplicate = fixture.RuntimeHandleCount;
                PlayerActorPreparationResult duplicateReplacement = fixture.Replace(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.ActorB,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectionRevision,
                    replacementToken,
                    fixture.ScopeContext,
                    "duplicate-replacement");
                AssertPreparationStatus(
                    duplicateReplacement,
                    PlayerActorPreparationStatus.FailedSelectionCommit,
                    "Duplicate replacement did not report selection commit failure.");
                AssertTrue(duplicateReplacement.HasSelectionResult &&
                    duplicateReplacement.SelectionResult.Status ==
                        PlayerActorSelectionStatus.RejectedDuplicateActorSelection,
                    "Duplicate replacement lost typed selection rejection evidence.");
                AssertTrue(duplicateReplacement.RollbackAttempted &&
                    duplicateReplacement.RollbackSucceeded,
                    "Duplicate replacement did not roll back staged materialization.");
                AssertSame(fixture.ActorC,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectedActorProfile,
                    "Failed replacement did not preserve previous selection.");
                AssertEqual(replacementToken,
                    fixture.GetSummary(fixture.SlotOne.PlayerSlotId).Token,
                    "Failed replacement did not preserve previous prepared Actor.");
                AssertEqual(handlesBeforeDuplicate, fixture.RuntimeHandleCount,
                    "Failed replacement leaked a RuntimeContent handle.");
                AssertEqual(1, fixture.CountLogicalActors(fixture.HostOne),
                    "Failed replacement leaked a staged Logical Actor.");
                completed.Add("failed-replacement-preserves-old-actor-and-selection");

                RuntimeScopeContext foreignScope = fixture.CreateAdditionalScope(
                    RuntimeContentOwner.Route(
                        "qa.p3j4.foreign-route",
                        "QA P3J4 Foreign Route"));
                PlayerActorPreparationResult scopeMismatch = fixture.Replace(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.ActorA,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectionRevision,
                    replacementToken,
                    foreignScope,
                    "foreign-scope-replacement");
                AssertPreparationStatus(
                    scopeMismatch,
                    PlayerActorPreparationStatus.RejectedScopeMismatch,
                    "Replacement accepted a different Runtime Content owner.");
                AssertEqual(replacementToken,
                    fixture.GetSummary(fixture.SlotOne.PlayerSlotId).Token,
                    "Foreign scope replacement mutated current preparation.");
                completed.Add("replacement-owner-scope-is-stable");

                PlayerActorPreparationResult releaseTwo = fixture.Release(
                    fixture.SlotTwo.PlayerSlotId,
                    preparedTwo.CurrentSummary.Token,
                    "release-slot-two");
                PlayerActorPreparationResult releaseReplacement = fixture.Release(
                    fixture.SlotOne.PlayerSlotId,
                    replacementToken,
                    "release-replacement");
                AssertPreparationStatus(
                    releaseTwo,
                    PlayerActorPreparationStatus.SucceededReleased,
                    "Slot two cleanup failed.");
                AssertPreparationStatus(
                    releaseReplacement,
                    PlayerActorPreparationStatus.SucceededReleased,
                    "Replacement cleanup failed.");
                AssertEqual(0, fixture.Snapshot.PreparedCount,
                    "Cleanup left prepared Session summaries.");
                completed.Add("all-prepared-actors-release-explicitly");

                PlayerActorSelectionResult cleared = fixture.ClearSelection(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.GetSlot(fixture.SlotOne.PlayerSlotId).SelectionRevision,
                    "clear-after-release");
                AssertSelectionStatus(
                    cleared,
                    PlayerActorSelectionStatus.SucceededCleared,
                    "Selection remained blocked after prepared Actor release.");
                completed.Add("selection-mutation-restored-after-release");

                PlayerActorPreparationResult missingSelection = fixture.Prepare(
                    fixture.SlotOne.PlayerSlotId,
                    fixture.HostOne,
                    "prepare-without-selection");
                AssertPreparationStatus(
                    missingSelection,
                    PlayerActorPreparationStatus.RejectedActorSelectionMissing,
                    "Prepare silently selected or materialized an Actor without Session selection.");
                completed.Add("prepare-requires-explicit-selection");

                AssertEqual(0, fixture.RuntimeHandleCount,
                    "P3J.4 smoke left RuntimeContent handles registered.");
                AssertEqual(0, fixture.Snapshot.RetainedReleaseFailureCount,
                    "P3J.4 smoke left retained release failures.");
                AssertEqual(0, fixture.CountLogicalActors(fixture.HostOne),
                    "P3J.4 smoke left a Logical Actor under host one.");
                AssertEqual(0, fixture.CountLogicalActors(fixture.HostTwo),
                    "P3J.4 smoke left a Logical Actor under host two.");
                completed.Add("transactions-leave-no-runtime-content-leaks");

                Debug.Log(
                    "[P3J4_SESSION_ACTOR_PREPARATION_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3J4_SESSION_ACTOR_PREPARATION_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
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

        private static void ValidateContractSurface(ICollection<string> completed)
        {
            Type preparationType = ResolveRuntimeType(PreparationContextTypeName);
            AssertTrue(preparationType.IsSealed,
                "PlayerActorPreparationRuntimeContext should be sealed.");
            AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(preparationType),
                "PlayerActorPreparationRuntimeContext must remain a plain C# Session authority.");
            AssertNotNull(preparationType.GetMethod("TryPrepareSelectedActor", InstanceAny),
                "TryPrepareSelectedActor was not found.");
            AssertNotNull(preparationType.GetMethod("TryReleasePreparedActor", InstanceAny),
                "TryReleasePreparedActor was not found.");
            AssertNotNull(preparationType.GetMethod("TryReplacePreparedActor", InstanceAny),
                "TryReplacePreparedActor was not found.");
            AssertNotNull(preparationType.GetMethod("CreateSnapshot", InstanceAny),
                "Player Actor preparation snapshot surface was not found.");
            AssertTrue(preparationType.GetMethod("Awake", InstanceAny) == null &&
                preparationType.GetMethod("OnEnable", InstanceAny) == null &&
                preparationType.GetMethod("Start", InstanceAny) == null &&
                preparationType.GetMethod("Update", InstanceAny) == null,
                "Session preparation authority introduced implicit Unity lifecycle execution.");

            Type adapterType = ResolveRuntimeType(MaterializationAdapterTypeName);
            AssertNotNull(adapterType.GetMethod("TryReleaseMaterialization", InstanceAny),
                "P3J.4 physical release adapter surface was not found.");
            AssertNotNull(adapterType.GetMethod("TryRollbackMaterialization", InstanceAny),
                "P3J.3 rollback compatibility surface was removed.");

            AssertTrue(typeof(PlayerActorPreparationToken).IsValueType,
                "Preparation token must be an immutable value type.");
            AssertTrue(typeof(PlayerActorPreparationSummary).IsValueType,
                "Preparation summary must be an immutable value type.");
            AssertTrue(typeof(PlayerActorPreparationSnapshot).IsSealed,
                "Preparation snapshot must be sealed.");
            completed.Add("typed-session-preparation-contracts-exist");
        }

        private static Fixture CreateFixture(ICollection<UnityEngine.Object> created)
        {
            ActorProfile actorA = CreateActorProfile(
                created,
                "QA P3J4 Actor A",
                "qa.p3j4.actor.a");
            ActorProfile actorB = CreateActorProfile(
                created,
                "QA P3J4 Actor B",
                "qa.p3j4.actor.b");
            ActorProfile actorC = CreateActorProfile(
                created,
                "QA P3J4 Actor C",
                "qa.p3j4.actor.c");

            PlayerSlotProfile slotProfileOne = CreateSlotProfile(
                created,
                "QA P3J4 Slot One",
                "qa.p3j4.player.1");
            PlayerSlotProfile slotProfileTwo = CreateSlotProfile(
                created,
                "QA P3J4 Slot Two",
                "qa.p3j4.player.2");
            PlayerActorSelectionPolicyProfile policy = CreatePolicy(created);

            Type participationType = ResolveRuntimeType(ParticipationContextTypeName);
            MethodInfo createParticipation = participationType.GetMethod(
                "TryCreateWithActorSelectionPolicy",
                StaticAny);
            AssertNotNull(createParticipation,
                "PlayerParticipationRuntimeContext.TryCreateWithActorSelectionPolicy was not found.");
            object[] participationArguments =
            {
                new[] { slotProfileOne, slotProfileTwo },
                2,
                true,
                policy,
                "QA.P3J4",
                "session-actor-preparation",
                null
            };
            var initialization = createParticipation.Invoke(null, participationArguments)
                as PlayerParticipationOperationResult;
            AssertNotNull(initialization,
                "Player participation initialization returned no result.");
            AssertTrue(initialization.Succeeded,
                "Player participation initialization failed. " + initialization.ToDiagnosticString());
            object participationContext = participationArguments[6];
            AssertNotNull(participationContext,
                "Player participation initialization returned no context.");

            LocalPlayerHostAuthoring hostOne = CreateHost(created, "QA P3J4 Host One");
            LocalPlayerHostAuthoring hostTwo = CreateHost(created, "QA P3J4 Host Two");
            PlayerSlotRuntimeSnapshot slotOne = JoinNext(
                participationContext,
                participationType,
                hostOne,
                "join-slot-one");
            PlayerSlotRuntimeSnapshot slotTwo = JoinNext(
                participationContext,
                participationType,
                hostTwo,
                "join-slot-two");

            PlayerParticipationSnapshot participationSnapshot = GetParticipationSnapshot(
                participationContext,
                participationType);
            string contextId = participationSnapshot.ContextId;

            Type runtimeContentType = ResolveRuntimeType(RuntimeContentRuntimeTypeName);
            object runtimeContent = Activator.CreateInstance(runtimeContentType, true);
            AssertNotNull(runtimeContent,
                "RuntimeContentRuntime could not be created.");
            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                "qa.p3j4.activity",
                "QA P3J4 Activity");
            RuntimeScopeContext scopeContext = CreateScope(
                runtimeContent,
                runtimeContentType,
                owner);

            Type adapterType = ResolveRuntimeType(MaterializationAdapterTypeName);
            ConstructorInfo adapterConstructor = adapterType.GetConstructor(
                InstanceAny,
                null,
                new[] { runtimeContentType, typeof(string) },
                null);
            AssertNotNull(adapterConstructor,
                "AttachedPlayerActorMaterializationAdapter constructor was not found.");
            object adapter = adapterConstructor.Invoke(
                new object[] { runtimeContent, contextId });
            AssertNotNull(adapter,
                "AttachedPlayerActorMaterializationAdapter could not be created.");

            Type preparationType = ResolveRuntimeType(PreparationContextTypeName);
            MethodInfo createPreparation = preparationType.GetMethod("TryCreate", StaticAny);
            AssertNotNull(createPreparation,
                "PlayerActorPreparationRuntimeContext.TryCreate was not found.");
            object[] preparationArguments =
            {
                participationContext,
                adapter,
                null,
                null
            };
            bool preparationCreated = (bool)createPreparation.Invoke(
                null,
                preparationArguments);
            AssertTrue(preparationCreated,
                "Player Actor preparation context creation failed. " +
                (preparationArguments[3] as string));
            object preparationContext = preparationArguments[2];
            AssertNotNull(preparationContext,
                "Player Actor preparation context was not returned.");

            return new Fixture(
                participationType,
                participationContext,
                runtimeContentType,
                runtimeContent,
                preparationType,
                preparationContext,
                contextId,
                scopeContext,
                slotOne,
                slotTwo,
                hostOne,
                hostTwo,
                actorA,
                actorB,
                actorC);
        }

        private static PlayerSlotRuntimeSnapshot JoinNext(
            object context,
            Type contextType,
            LocalPlayerHostAuthoring host,
            string reason)
        {
            MethodInfo reserveMethod = contextType.GetMethod(
                "TryReserveNextAvailableSlot",
                InstanceAny);
            var reserve = reserveMethod.Invoke(
                context,
                new object[] { "QA.P3J4", reason })
                as PlayerParticipationOperationResult;
            AssertNotNull(reserve, "Slot reservation returned no result.");
            AssertTrue(reserve.Succeeded,
                "Slot reservation failed. " + reserve.ToDiagnosticString());

            MethodInfo stageHost = typeof(LocalPlayerHostAuthoring).GetMethod(
                "TryStageAdmission",
                InstanceAny);
            object[] stageArguments =
            {
                reserve.Slot,
                "QA.P3J4",
                reason,
                null
            };
            bool staged = (bool)stageHost.Invoke(host, stageArguments);
            AssertTrue(staged,
                "Local Player Host admission staging failed. " +
                (stageArguments[3] as string));

            MethodInfo joinedMethod = contextType.GetMethod("TryMarkJoined", InstanceAny);
            var joined = joinedMethod.Invoke(
                context,
                new object[]
                {
                    reserve.ReservationToken,
                    "QA.P3J4",
                    reason
                }) as PlayerParticipationOperationResult;
            AssertNotNull(joined, "Slot join returned no result.");
            AssertTrue(joined.Succeeded,
                "Slot join failed. " + joined.ToDiagnosticString());

            MethodInfo commitHost = typeof(LocalPlayerHostAuthoring).GetMethod(
                "CommitStagedAdmission",
                InstanceAny);
            commitHost.Invoke(
                host,
                new object[]
                {
                    joined.Slot,
                    "QA.P3J4",
                    reason
                });
            return joined.Slot;
        }

        private static LocalPlayerHostAuthoring CreateHost(
            ICollection<UnityEngine.Object> created,
            string name)
        {
            var root = new GameObject(name);
            root.SetActive(true);
            PlayerInput playerInput = root.AddComponent<PlayerInput>();
            var actorMount = new GameObject("ActorMount");
            actorMount.transform.SetParent(root.transform, false);
            LocalPlayerHostAuthoring host = root.AddComponent<LocalPlayerHostAuthoring>();
            var serialized = new SerializedObject(host);
            serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
            serialized.FindProperty("actorMount").objectReferenceValue = actorMount.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(root);
            return host;
        }

        private static ActorProfile CreateActorProfile(
            ICollection<UnityEngine.Object> created,
            string displayName,
            string actorProfileId)
        {
            var logicalActor = new GameObject(displayName + " Logical Actor");
            logicalActor.SetActive(false);
            logicalActor.AddComponent<PlayerActorDeclaration>();
            created.Add(logicalActor);

            var profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue = actorProfileId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("actorKind").intValue = (int)ActorKind.Player;
            serialized.FindProperty("actorRole").intValue = (int)ActorRole.Protagonist;
            serialized.FindProperty("logicalActorHostPrefab").objectReferenceValue = logicalActor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static PlayerSlotProfile CreateSlotProfile(
            ICollection<UnityEngine.Object> created,
            string displayName,
            string playerSlotId)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = playerSlotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static PlayerActorSelectionPolicyProfile CreatePolicy(
            ICollection<UnityEngine.Object> created)
        {
            var policy = ScriptableObject.CreateInstance<PlayerActorSelectionPolicyProfile>();
            policy.name = "QA P3J4 Unique Actor Selection";
            var serialized = new SerializedObject(policy);
            serialized.FindProperty("duplicatePolicy").intValue =
                (int)PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(policy);
            return policy;
        }

        private static RuntimeScopeContext CreateScope(
            object runtimeContent,
            Type runtimeContentType,
            RuntimeContentOwner owner)
        {
            MethodInfo createRoot = runtimeContentType.GetMethod(
                "CreateScopeRoot",
                InstanceAny);
            createRoot.Invoke(
                runtimeContent,
                new object[] { owner, "QA.P3J4", "create-scope-root" });

            MethodInfo createContext = runtimeContentType.GetMethod(
                "TryCreateScopeContext",
                InstanceAny);
            object[] contextArguments =
            {
                owner,
                "QA.P3J4",
                "session-actor-preparation",
                null
            };
            bool created = (bool)createContext.Invoke(
                runtimeContent,
                contextArguments);
            AssertTrue(created,
                "Runtime Scope Context could not be created.");
            return (RuntimeScopeContext)contextArguments[3];
        }

        private static PlayerParticipationSnapshot GetParticipationSnapshot(
            object context,
            Type contextType)
        {
            MethodInfo method = contextType.GetMethod("CreateSnapshot", InstanceAny);
            return method.Invoke(context, Array.Empty<object>())
                as PlayerParticipationSnapshot;
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = typeof(PlayerActorPreparationResult).Assembly.GetType(
                fullName,
                false);
            AssertNotNull(type,
                $"Runtime type '{fullName}' was not found.");
            return type;
        }

        private static void AssertPreparationStatus(
            PlayerActorPreparationResult result,
            PlayerActorPreparationStatus expected,
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

        private static void AssertSelectionStatus(
            PlayerActorSelectionResult result,
            PlayerActorSelectionStatus expected,
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

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(T expected, T actual, string message)
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
                : value.Replace("\\", "\\\\").Replace("'", "\\'")
                    .Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class Fixture : IDisposable
        {
            private readonly Type participationType;
            private readonly object participationContext;
            private readonly Type runtimeContentType;
            private readonly object runtimeContent;
            private readonly Type preparationType;
            private readonly object preparationContext;
            private bool disposed;

            internal Fixture(
                Type participationType,
                object participationContext,
                Type runtimeContentType,
                object runtimeContent,
                Type preparationType,
                object preparationContext,
                string contextId,
                RuntimeScopeContext scopeContext,
                PlayerSlotRuntimeSnapshot slotOne,
                PlayerSlotRuntimeSnapshot slotTwo,
                LocalPlayerHostAuthoring hostOne,
                LocalPlayerHostAuthoring hostTwo,
                ActorProfile actorA,
                ActorProfile actorB,
                ActorProfile actorC)
            {
                this.participationType = participationType;
                this.participationContext = participationContext;
                this.runtimeContentType = runtimeContentType;
                this.runtimeContent = runtimeContent;
                this.preparationType = preparationType;
                this.preparationContext = preparationContext;
                ContextId = contextId;
                ScopeContext = scopeContext;
                SlotOne = slotOne;
                SlotTwo = slotTwo;
                HostOne = hostOne;
                HostTwo = hostTwo;
                ActorA = actorA;
                ActorB = actorB;
                ActorC = actorC;
            }

            internal string ContextId { get; }
            internal RuntimeScopeContext ScopeContext { get; }
            internal PlayerSlotRuntimeSnapshot SlotOne { get; }
            internal PlayerSlotRuntimeSnapshot SlotTwo { get; }
            internal LocalPlayerHostAuthoring HostOne { get; }
            internal LocalPlayerHostAuthoring HostTwo { get; }
            internal ActorProfile ActorA { get; }
            internal ActorProfile ActorB { get; }
            internal ActorProfile ActorC { get; }

            internal PlayerActorPreparationSnapshot Snapshot
            {
                get
                {
                    MethodInfo method = preparationType.GetMethod(
                        "CreateSnapshot",
                        InstanceAny);
                    return method.Invoke(
                        preparationContext,
                        Array.Empty<object>()) as PlayerActorPreparationSnapshot;
                }
            }

            internal int RuntimeHandleCount
            {
                get
                {
                    MethodInfo method = runtimeContentType.GetMethod(
                        "SnapshotHandles",
                        InstanceAny);
                    var handles = method.Invoke(
                        runtimeContent,
                        new object[] { ScopeContext }) as RuntimeContentHandle[];
                    return handles?.Length ?? 0;
                }
            }

            internal PlayerActorSelectionResult Select(
                PlayerSlotId slotId,
                ActorProfile actorProfile,
                int expectedRevision,
                string reason)
            {
                var request = new PlayerActorSelectionRequest(
                    slotId,
                    actorProfile,
                    "QA.P3J4",
                    reason,
                    expectedRevision);
                return InvokeSelection("TrySelectActorProfile", request);
            }

            internal PlayerActorSelectionResult GuardedReplaceSelection(
                PlayerSlotId slotId,
                ActorProfile actorProfile,
                int expectedRevision,
                string reason)
            {
                var request = new PlayerActorSelectionRequest(
                    slotId,
                    actorProfile,
                    "QA.P3J4",
                    reason,
                    expectedRevision);
                return InvokeSelection("TryReplaceActorSelection", request);
            }

            internal PlayerActorSelectionResult ClearSelection(
                PlayerSlotId slotId,
                int expectedRevision,
                string reason)
            {
                var request = new PlayerActorSelectionRequest(
                    slotId,
                    null,
                    "QA.P3J4",
                    reason,
                    expectedRevision);
                return InvokeSelection("TryClearActorSelection", request);
            }

            internal PlayerActorPreparationResult Prepare(
                PlayerSlotId slotId,
                LocalPlayerHostAuthoring host,
                string reason)
            {
                MethodInfo method = preparationType.GetMethod(
                    "TryPrepareSelectedActor",
                    InstanceAny);
                return method.Invoke(
                    preparationContext,
                    new object[]
                    {
                        ScopeContext,
                        slotId,
                        host,
                        "QA.P3J4",
                        reason
                    }) as PlayerActorPreparationResult;
            }

            internal PlayerActorPreparationResult Release(
                PlayerSlotId slotId,
                PlayerActorPreparationToken token,
                string reason)
            {
                MethodInfo method = preparationType.GetMethod(
                    "TryReleasePreparedActor",
                    InstanceAny);
                return method.Invoke(
                    preparationContext,
                    new object[]
                    {
                        slotId,
                        token,
                        "QA.P3J4",
                        reason
                    }) as PlayerActorPreparationResult;
            }

            internal PlayerActorPreparationResult Replace(
                PlayerSlotId slotId,
                ActorProfile actorProfile,
                int expectedSelectionRevision,
                PlayerActorPreparationToken token,
                RuntimeScopeContext scopeContext,
                string reason)
            {
                MethodInfo method = preparationType.GetMethod(
                    "TryReplacePreparedActor",
                    InstanceAny);
                var request = new PlayerActorSelectionRequest(
                    slotId,
                    actorProfile,
                    "QA.P3J4",
                    reason,
                    expectedSelectionRevision);
                return method.Invoke(
                    preparationContext,
                    new object[]
                    {
                        scopeContext,
                        request,
                        token,
                        "QA.P3J4",
                        reason
                    }) as PlayerActorPreparationResult;
            }

            internal PlayerActorPreparationSummary GetSummary(PlayerSlotId slotId)
            {
                MethodInfo method = preparationType.GetMethod(
                    "TryGetPreparationSummary",
                    InstanceAny);
                object[] arguments = { slotId, null };
                bool found = (bool)method.Invoke(preparationContext, arguments);
                AssertTrue(found,
                    "Preparation summary was not found for Slot " + slotId.StableText);
                return (PlayerActorPreparationSummary)arguments[1];
            }

            internal PlayerSlotRuntimeSnapshot GetSlot(PlayerSlotId slotId)
            {
                MethodInfo method = participationType.GetMethod(
                    "TryGetActorSelection",
                    InstanceAny);
                object[] arguments = { slotId, null };
                bool found = (bool)method.Invoke(participationContext, arguments);
                AssertTrue(found,
                    "Participation Slot was not found: " + slotId.StableText);
                return (PlayerSlotRuntimeSnapshot)arguments[1];
            }

            internal RuntimeScopeContext CreateAdditionalScope(
                RuntimeContentOwner owner)
            {
                return CreateScope(runtimeContent, runtimeContentType, owner);
            }

            internal int CountLogicalActors(LocalPlayerHostAuthoring host)
            {
                return host.ActorMount
                    .GetComponentsInChildren<PlayerActorDeclaration>(true)
                    .Length;
            }

            private PlayerActorSelectionResult InvokeSelection(
                string methodName,
                PlayerActorSelectionRequest request)
            {
                MethodInfo method = preparationType.GetMethod(
                    methodName,
                    InstanceAny);
                return method.Invoke(
                    preparationContext,
                    new object[] { request }) as PlayerActorSelectionResult;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
            }
        }
    }
}

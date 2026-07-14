using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.UnityInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Play Mode technical smoke for P3K.3 typed prepared-Actor gameplay input binding.
    /// </summary>
    public static class QaP3K3TypedControlInputBindingSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.3 Run Typed Control and Input Binding Smoke";
        private const string OccupancyContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayOccupancyRuntimeContext";
        private const string InputContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayInputBindingRuntimeContext";

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
                    "P3K.3 typed input smoke must run in Play Mode.");
                Type occupancyContextType = ResolveType(
                    typeof(PlayerGameplayOccupancyStatus),
                    OccupancyContextTypeName);
                Type inputContextType = ResolveType(
                    typeof(PlayerGameplayInputBindingStatus),
                    InputContextTypeName);
                ValidateContractSurface(inputContextType);
                completed.Add("contract-surface-valid");

                const string sessionId = "qa.p3k3.session";
                PlayerSlotId slotOne = PlayerSlotId.From("player.1");
                PlayerSlotId slotTwo = PlayerSlotId.From("player.2");
                RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                    "qa.p3k3.activity",
                    "P3K.3 Activity");

                PlayerActorPreparationSummary preparedOne = CreatePreparation(
                    sessionId,
                    slotOne,
                    ActorProfileId.From("actor.profile.one"),
                    ActorId.From("actor.runtime.one"),
                    owner,
                    1);
                PlayerActorPreparationSummary preparedTwo = CreatePreparation(
                    sessionId,
                    slotTwo,
                    ActorProfileId.From("actor.profile.two"),
                    ActorId.From("actor.runtime.two"),
                    owner,
                    2);
                PlayerActorPreparationSnapshot preparationSnapshot =
                    CreatePreparationSnapshot(sessionId, preparedOne, preparedTwo);

                object occupancyContext = CreateOccupancyContext(
                    occupancyContextType,
                    preparationSnapshot);
                PlayerGameplayOccupancyResult occupiedOne = ConfirmOccupancy(
                    occupancyContextType,
                    occupancyContext,
                    preparedOne,
                    "occupy-slot-one");
                AssertEqual(PlayerGameplayOccupancyStatus.SucceededOccupied,
                    occupiedOne.Status,
                    "P3K.3 fixture could not occupy Slot one.");
                PlayerGameplayOccupancySnapshot occupancySnapshot =
                    OccupancySnapshot(occupancyContextType, occupancyContext);

                object inputContext = CreateInputContext(
                    inputContextType,
                    occupancyContext);
                PlayerGameplayInputBindingSnapshot initial =
                    InputSnapshot(inputContextType, inputContext);
                AssertTrue(initial.IsInitialized,
                    "P3K.3 input binding snapshot is not initialized.");
                AssertEqual(2, initial.ConfiguredSlotCount,
                    "P3K.3 input context lost configured Slots.");
                AssertEqual(0, initial.BoundCount,
                    "P3K.3 input context did not start Unbound.");
                completed.Add("context-initialized-from-occupancy-roster");

                AssertTrue(occupancySnapshot.TryGetSummary(
                        slotTwo,
                        out PlayerGameplayOccupancySummary vacantTwo),
                    "P3K.3 fixture lost vacant Slot two occupancy summary.");
                PlayerGameplayInputBindingResult vacantBind = Bind(
                    inputContextType,
                    inputContext,
                    preparedTwo,
                    vacantTwo,
                    null,
                    null,
                    null,
                    "bind-vacant-slot");
                AssertStatus(
                    vacantBind,
                    PlayerGameplayInputBindingStatus.RejectedOccupancyNotReady,
                    "Vacant Slot was accepted for gameplay input binding.");
                completed.Add("vacant-occupancy-rejected");

                PlayerGameplayOccupancyResult occupancyReleased = ReleaseOccupancy(
                    occupancyContextType,
                    occupancyContext,
                    slotOne,
                    occupiedOne.CurrentSummary.Token,
                    "release-before-stale-bind");
                AssertEqual(PlayerGameplayOccupancyStatus.SucceededReleased,
                    occupancyReleased.Status,
                    "P3K.3 fixture could not release Slot one occupancy.");
                PlayerGameplayInputBindingResult staleOccupancyBind = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    occupiedOne.CurrentSummary,
                    null,
                    null,
                    null,
                    "bind-stale-occupancy");
                AssertStatus(
                    staleOccupancyBind,
                    PlayerGameplayInputBindingStatus.RejectedForeignOrStaleOccupancy,
                    "Released stale occupancy was accepted for gameplay input binding.");
                completed.Add("live-occupancy-authority-rejects-stale-evidence");

                PlayerGameplayOccupancyResult activeOccupiedOne = ConfirmOccupancy(
                    occupancyContextType,
                    occupancyContext,
                    preparedOne,
                    "reoccupy-slot-one");
                AssertEqual(PlayerGameplayOccupancyStatus.SucceededOccupied,
                    activeOccupiedOne.Status,
                    "P3K.3 fixture could not reoccupy Slot one.");

                using HostFixture host = HostFixture.Create(
                    slotOne,
                    preparedOne.Materialization.ActorId,
                    "UI",
                    "Player",
                    created);
                AssertEqual("UI", CurrentMapName(host.PlayerInput),
                    "P3K.3 fixture did not start on the UI action map.");

                PlayerGameplayInputBindingResult bound = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    activeOccupiedOne.CurrentSummary,
                    host.Host,
                    host.ActorDeclaration,
                    host.GateAdapter,
                    "bind-slot-one");
                AssertStatus(
                    bound,
                    PlayerGameplayInputBindingStatus.SucceededBound,
                    "Prepared occupied Actor did not bind gameplay input.");
                AssertTrue(bound.CurrentSummary.IsBound,
                    "P3K.3 bind result did not expose Bound evidence.");
                AssertTrue(bound.CurrentSummary.Token.IsValid,
                    "P3K.3 bind result has no functional token.");
                AssertEqual(preparedOne.Token,
                    bound.CurrentSummary.PreparationToken,
                    "P3K.3 binding lost the exact preparation token.");
                AssertEqual(activeOccupiedOne.CurrentSummary.Token,
                    bound.CurrentSummary.OccupancyToken,
                    "P3K.3 binding lost the exact occupancy token.");
                AssertEqual(preparedOne.Materialization.ActorId,
                    bound.CurrentSummary.ActorId,
                    "P3K.3 binding lost generated ActorId.");
                AssertEqual("Player", CurrentMapName(host.PlayerInput),
                    "P3K.3 binding did not activate gameplay action map.");
                completed.Add("prepared-occupied-actor-bound-to-stable-playerinput");

                PlayerGameplayInputBindingToken firstToken =
                    bound.CurrentSummary.Token;
                int idempotentRevision = bound.Snapshot.Revision;
                PlayerGameplayInputBindingResult boundAgain = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    activeOccupiedOne.CurrentSummary,
                    host.Host,
                    host.ActorDeclaration,
                    host.GateAdapter,
                    "bind-slot-one-again");
                AssertStatus(
                    boundAgain,
                    PlayerGameplayInputBindingStatus.SucceededAlreadyBound,
                    "Repeated gameplay input bind was not idempotent.");
                AssertEqual(firstToken,
                    boundAgain.CurrentSummary.Token,
                    "Idempotent bind changed binding token.");
                AssertEqual(idempotentRevision,
                    boundAgain.Snapshot.Revision,
                    "Idempotent bind changed context revision.");
                completed.Add("bind-is-idempotent");

                PlayerActorDeclaration wrongActor = CreateActorDeclaration(
                    host.ActorMount,
                    ActorId.From("actor.runtime.wrong"),
                    host.PlayerInput,
                    "Wrong Actor",
                    created);
                PlayerGameplayInputBindingResult actorMismatch = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    activeOccupiedOne.CurrentSummary,
                    host.Host,
                    wrongActor,
                    host.GateAdapter,
                    "actor-mismatch");
                AssertStatus(
                    actorMismatch,
                    PlayerGameplayInputBindingStatus.RejectedActorMismatch,
                    "Mismatched Actor declaration was accepted.");
                completed.Add("actor-identity-mismatch-rejected");

                GameObject foreignInputObject = new GameObject("P3K3 Foreign Input");
                foreignInputObject.SetActive(false);
                created.Add(foreignInputObject);
                PlayerInput foreignInput = foreignInputObject.AddComponent<PlayerInput>();
                ConfigurePlayerInput(
                    foreignInput,
                    CreateInputAsset("P3K3 Foreign Actions", created),
                    "UI");
                PlayerActorDeclaration wrongInputActor = CreateActorDeclaration(
                    host.ActorMount,
                    preparedOne.Materialization.ActorId,
                    foreignInput,
                    "Wrong Input Actor",
                    created);
                PlayerGameplayInputBindingResult playerInputMismatch = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    activeOccupiedOne.CurrentSummary,
                    host.Host,
                    wrongInputActor,
                    host.GateAdapter,
                    "playerinput-mismatch");
                AssertStatus(
                    playerInputMismatch,
                    PlayerGameplayInputBindingStatus.RejectedPlayerInputMismatch,
                    "Actor with foreign PlayerInput evidence was accepted.");
                completed.Add("playerinput-evidence-mismatch-rejected");

                SetField(host.Host, "joinedPlayerSlotId", slotTwo);
                PlayerGameplayInputBindingResult hostMismatch = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    activeOccupiedOne.CurrentSummary,
                    host.Host,
                    host.ActorDeclaration,
                    host.GateAdapter,
                    "host-slot-mismatch");
                AssertStatus(
                    hostMismatch,
                    PlayerGameplayInputBindingStatus.RejectedHostMismatch,
                    "Stable host with foreign joined Slot was accepted.");
                SetField(host.Host, "joinedPlayerSlotId", slotOne);
                completed.Add("stable-host-slot-mismatch-rejected");

                GameObject foreignGateObject = new GameObject("P3K3 Foreign Gate");
                foreignGateObject.SetActive(false);
                created.Add(foreignGateObject);
                PlayerInput foreignGateInput = foreignGateObject.AddComponent<PlayerInput>();
                ConfigurePlayerInput(
                    foreignGateInput,
                    CreateInputAsset("P3K3 Foreign Gate Actions", created),
                    "UI");
                UnityPlayerInputGateAdapter foreignGate =
                    foreignGateObject.AddComponent<UnityPlayerInputGateAdapter>();
                ConfigureGateAdapter(foreignGate, foreignGateInput, null, "Player");
                PlayerGameplayInputBindingResult gateMismatch = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    activeOccupiedOne.CurrentSummary,
                    host.Host,
                    host.ActorDeclaration,
                    foreignGate,
                    "gate-mismatch");
                AssertStatus(
                    gateMismatch,
                    PlayerGameplayInputBindingStatus.RejectedGateAdapterMismatch,
                    "Gate adapter targeting another PlayerInput was accepted.");
                completed.Add("gate-adapter-mismatch-rejected");

                ConfigureGateAdapter(
                    host.GateAdapter,
                    host.PlayerInput,
                    host.SlotDeclaration,
                    "MissingGameplayMap");
                PlayerGameplayInputBindingResult missingMap = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    activeOccupiedOne.CurrentSummary,
                    host.Host,
                    host.ActorDeclaration,
                    host.GateAdapter,
                    "missing-action-map");
                AssertStatus(
                    missingMap,
                    PlayerGameplayInputBindingStatus.RejectedMissingActionMap,
                    "Missing gameplay action map was accepted.");
                ConfigureGateAdapter(
                    host.GateAdapter,
                    host.PlayerInput,
                    host.SlotDeclaration,
                    "Player");
                completed.Add("missing-gameplay-action-map-rejected");

                PlayerGameplayOccupancyResult occupiedTwo = ConfirmOccupancy(
                    occupancyContextType,
                    occupancyContext,
                    preparedTwo,
                    "occupy-slot-two");
                AssertEqual(PlayerGameplayOccupancyStatus.SucceededOccupied,
                    occupiedTwo.Status,
                    "P3K.3 fixture could not occupy Slot two.");
                PlayerActorDeclaration actorTwo = CreateActorDeclaration(
                    host.ActorMount,
                    preparedTwo.Materialization.ActorId,
                    host.PlayerInput,
                    "Slot Two Actor",
                    created);
                ConfigureSlotDeclaration(
                    host.SlotDeclaration,
                    slotTwo,
                    host.PlayerInput,
                    "P3K3 Slot Two");
                SetField(host.Host, "joinedPlayerSlotId", slotTwo);
                PlayerGameplayInputBindingResult duplicatePlayerInput = Bind(
                    inputContextType,
                    inputContext,
                    preparedTwo,
                    occupiedTwo.CurrentSummary,
                    host.Host,
                    actorTwo,
                    host.GateAdapter,
                    "same-playerinput-second-slot");
                AssertStatus(
                    duplicatePlayerInput,
                    PlayerGameplayInputBindingStatus.RejectedPlayerInputAlreadyBound,
                    "One stable-host PlayerInput was accepted for two current Slot bindings.");
                ConfigureSlotDeclaration(
                    host.SlotDeclaration,
                    slotOne,
                    host.PlayerInput,
                    "P3K3 Slot One");
                SetField(host.Host, "joinedPlayerSlotId", slotOne);
                completed.Add("one-playerinput-one-current-binding");

                SetField(host.GateAdapter, "_isBlockedByAdapter", true);
                PlayerGameplayInputBindingResult blocked = Refresh(
                    inputContextType,
                    inputContext,
                    slotOne,
                    firstToken,
                    "simulate-gate-block");
                AssertStatus(
                    blocked,
                    PlayerGameplayInputBindingStatus.SucceededAvailabilityRefreshed,
                    "Gate-blocked availability refresh failed.");
                AssertTrue(blocked.CurrentSummary.IsBlockedByGate,
                    "Gate-blocked binding did not report BlockedByGate.");
                AssertEqual(firstToken,
                    blocked.CurrentSummary.Token,
                    "Gate availability refresh replaced binding identity.");
                completed.Add("gate-block-preserves-binding-identity");

                SetField(host.GateAdapter, "_isBlockedByAdapter", false);
                PlayerGameplayInputBindingResult allowed = Refresh(
                    inputContextType,
                    inputContext,
                    slotOne,
                    firstToken,
                    "simulate-gate-release");
                AssertTrue(allowed.CurrentSummary.IsAllowed,
                    "Gate release did not restore Allowed availability.");
                AssertEqual(firstToken,
                    allowed.CurrentSummary.Token,
                    "Gate release replaced binding identity.");
                completed.Add("gate-release-restores-availability");

                PlayerGameplayInputBindingResult invalidRelease = Release(
                    inputContextType,
                    inputContext,
                    slotOne,
                    default,
                    "invalid-token-release");
                AssertStatus(
                    invalidRelease,
                    PlayerGameplayInputBindingStatus.RejectedForeignOrStaleBinding,
                    "Bound gameplay input accepted an invalid release token.");
                AssertEqual(firstToken,
                    invalidRelease.CurrentSummary.Token,
                    "Rejected release mutated current input binding.");
                completed.Add("release-is-token-guarded");

                PlayerGameplayInputBindingResult released = Release(
                    inputContextType,
                    inputContext,
                    slotOne,
                    firstToken,
                    "release-slot-one");
                AssertStatus(
                    released,
                    PlayerGameplayInputBindingStatus.SucceededReleased,
                    "Gameplay input release failed.");
                AssertTrue(released.CurrentSummary.IsUnbound,
                    "Gameplay input release did not return Slot to Unbound.");
                AssertEqual("UI", CurrentMapName(host.PlayerInput),
                    "Gameplay input release did not restore previous action map.");
                completed.Add("release-restores-previous-action-map");

                PlayerGameplayInputBindingResult releasedAgain = Release(
                    inputContextType,
                    inputContext,
                    slotOne,
                    default,
                    "release-slot-one-again");
                AssertStatus(
                    releasedAgain,
                    PlayerGameplayInputBindingStatus.SucceededAlreadyReleased,
                    "Repeated gameplay input release was not idempotent.");
                completed.Add("release-is-idempotent");

                PlayerGameplayInputBindingResult rebound = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    activeOccupiedOne.CurrentSummary,
                    host.Host,
                    host.ActorDeclaration,
                    host.GateAdapter,
                    "rebind-slot-one");
                AssertStatus(
                    rebound,
                    PlayerGameplayInputBindingStatus.SucceededBound,
                    "Released gameplay input could not rebind.");
                AssertTrue(rebound.CurrentSummary.Token != firstToken,
                    "Rebind reused stale gameplay input token.");
                completed.Add("rebind-generates-new-token");

                PlayerGameplayInputBindingResult staleRelease = Release(
                    inputContextType,
                    inputContext,
                    slotOne,
                    firstToken,
                    "stale-token-after-rebind");
                AssertStatus(
                    staleRelease,
                    PlayerGameplayInputBindingStatus.RejectedForeignOrStaleBinding,
                    "Stale pre-rebind input token was accepted.");
                AssertEqual(rebound.CurrentSummary.Token,
                    staleRelease.CurrentSummary.Token,
                    "Stale release disturbed rebound input identity.");
                completed.Add("stale-token-after-rebind-rejected");

                bool releasedAll = ReleaseAll(
                    inputContextType,
                    inputContext,
                    out int releasedCount,
                    out int failedCount,
                    out string releaseAllIssue);
                AssertTrue(releasedAll,
                    $"Release-all failed. {releaseAllIssue}");
                AssertEqual(1, releasedCount,
                    "Release-all did not release the rebound Slot.");
                AssertEqual(0, failedCount,
                    "Release-all reported failed input bindings.");
                PlayerGameplayInputBindingSnapshot final =
                    InputSnapshot(inputContextType, inputContext);
                AssertEqual(0, final.BoundCount,
                    "Release-all left gameplay input bindings.");
                AssertEqual(2, final.UnboundCount,
                    "Release-all did not restore every Slot to Unbound.");
                completed.Add("all-gameplay-input-bindings-released");

                Debug.Log(
                    "[P3K3_TYPED_CONTROL_INPUT_BINDING_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"slot='{slotOne.StableText}' actor='{preparedOne.Materialization.ActorId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K3_TYPED_CONTROL_INPUT_BINDING_SMOKE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    UnityEngine.Object target = created[index];
                    if (target != null)
                    {
                        UnityEngine.Object.DestroyImmediate(target);
                    }
                }
            }
        }

        private static Type ResolveType(Type publicContract, string typeName)
        {
            Type type = publicContract.Assembly.GetType(typeName, false);
            AssertNotNull(type, $"Required runtime type '{typeName}' is missing.");
            return type;
        }

        private static void ValidateContractSurface(Type contextType)
        {
            AssertTrue(!contextType.IsPublic,
                "P3K.3 runtime context must remain an internal package authority.");
            AssertNotNull(contextType.GetMethod("TryCreate", StaticAny),
                "P3K.3 runtime context has no TryCreate operation.");
            AssertNotNull(contextType.GetMethod("TryBind", InstanceAny),
                "P3K.3 runtime context has no TryBind operation.");
            AssertNotNull(contextType.GetMethod("TryRefreshAvailability", InstanceAny),
                "P3K.3 runtime context has no availability refresh operation.");
            AssertNotNull(contextType.GetMethod("TryRelease", InstanceAny),
                "P3K.3 runtime context has no TryRelease operation.");
            AssertNotNull(contextType.GetMethod("TryReleaseAll", InstanceAny),
                "P3K.3 runtime context has no TryReleaseAll operation.");
            AssertNotNull(contextType.GetMethod("CreateSnapshot", InstanceAny),
                "P3K.3 runtime context has no CreateSnapshot operation.");
            AssertTrue(typeof(PlayerGameplayInputBindingToken).IsValueType,
                "P3K.3 input binding token must be an immutable value type.");
            AssertTrue(typeof(PlayerGameplayInputBindingSummary).IsValueType,
                "P3K.3 input binding summary must be an immutable value type.");
        }

        private static object CreateOccupancyContext(
            Type contextType,
            PlayerActorPreparationSnapshot snapshot)
        {
            MethodInfo method = contextType.GetMethod("TryCreate", StaticAny);
            object[] arguments = { snapshot, null, null };
            bool succeeded = (bool)method.Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.2 occupancy context creation failed. {arguments[2]}");
            return arguments[1];
        }

        private static PlayerGameplayOccupancyResult ConfirmOccupancy(
            Type contextType,
            object context,
            PlayerActorPreparationSummary preparation,
            string reason)
        {
            return (PlayerGameplayOccupancyResult)contextType
                .GetMethod("TryConfirmOccupancy", InstanceAny)
                .Invoke(context, new object[]
                {
                    preparation,
                    nameof(QaP3K3TypedControlInputBindingSmoke),
                    reason
                });
        }

        private static PlayerGameplayOccupancyResult ReleaseOccupancy(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayOccupancyToken token,
            string reason)
        {
            return (PlayerGameplayOccupancyResult)contextType
                .GetMethod("TryReleaseOccupancy", InstanceAny)
                .Invoke(context, new object[]
                {
                    playerSlotId,
                    token,
                    nameof(QaP3K3TypedControlInputBindingSmoke),
                    reason
                });
        }

        private static PlayerGameplayOccupancySnapshot OccupancySnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayOccupancySnapshot)contextType
                .GetMethod("CreateSnapshot", InstanceAny)
                .Invoke(context, Array.Empty<object>());
        }

        private static object CreateInputContext(
            Type contextType,
            object occupancyContext)
        {
            MethodInfo method = contextType.GetMethod("TryCreate", StaticAny);
            object[] arguments = { occupancyContext, null, null };
            bool succeeded = (bool)method.Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.3 input context creation failed. {arguments[2]}");
            AssertNotNull(arguments[1],
                "P3K.3 input context creation returned no context.");
            return arguments[1];
        }

        private static PlayerGameplayInputBindingResult Bind(
            Type contextType,
            object context,
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            LocalPlayerHostAuthoring host,
            PlayerActorDeclaration actorDeclaration,
            UnityPlayerInputGateAdapter gateAdapter,
            string reason)
        {
            return (PlayerGameplayInputBindingResult)contextType
                .GetMethod("TryBind", InstanceAny)
                .Invoke(context, new object[]
                {
                    preparation,
                    occupancy,
                    host,
                    actorDeclaration,
                    gateAdapter,
                    nameof(QaP3K3TypedControlInputBindingSmoke),
                    reason
                });
        }

        private static PlayerGameplayInputBindingResult Refresh(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken token,
            string reason)
        {
            return (PlayerGameplayInputBindingResult)contextType
                .GetMethod("TryRefreshAvailability", InstanceAny)
                .Invoke(context, new object[]
                {
                    playerSlotId,
                    token,
                    nameof(QaP3K3TypedControlInputBindingSmoke),
                    reason
                });
        }

        private static PlayerGameplayInputBindingResult Release(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken token,
            string reason)
        {
            return (PlayerGameplayInputBindingResult)contextType
                .GetMethod("TryRelease", InstanceAny)
                .Invoke(context, new object[]
                {
                    playerSlotId,
                    token,
                    nameof(QaP3K3TypedControlInputBindingSmoke),
                    reason
                });
        }

        private static bool ReleaseAll(
            Type contextType,
            object context,
            out int releasedCount,
            out int failedCount,
            out string issue)
        {
            object[] arguments =
            {
                nameof(QaP3K3TypedControlInputBindingSmoke),
                "release-all",
                0,
                0,
                null
            };
            bool succeeded = (bool)contextType
                .GetMethod("TryReleaseAll", InstanceAny)
                .Invoke(context, arguments);
            releasedCount = (int)arguments[2];
            failedCount = (int)arguments[3];
            issue = arguments[4] as string ?? string.Empty;
            return succeeded;
        }

        private static PlayerGameplayInputBindingSnapshot InputSnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayInputBindingSnapshot)contextType
                .GetMethod("CreateSnapshot", InstanceAny)
                .Invoke(context, Array.Empty<object>());
        }

        private static PlayerActorPreparationSummary CreatePreparation(
            string sessionId,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            int revision)
        {
            PlayerActorMaterializationOperationId operationId = CreateOperationId(
                sessionId,
                owner,
                playerSlotId,
                revision);
            RuntimeContentIdentity identity = RuntimeContentIdentity.From(
                owner,
                $"qa.p3k3.content.{playerSlotId.Value.Value}.{revision}");
            PlayerActorMaterializationSnapshot materialization =
                CreateMaterializationSnapshot(
                    operationId,
                    identity,
                    playerSlotId,
                    actorProfileId,
                    actorId,
                    revision);
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
                    PlayerActorPreparationState.Prepared,
                    actorProfileId,
                    revision,
                    materialization,
                    nameof(QaP3K3TypedControlInputBindingSmoke),
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
            MethodInfo method = typeof(PlayerActorMaterializationOperationId)
                .GetMethod("TryCreate", StaticAny);
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
                    PlayerActorMaterializationState.Active,
                    nameof(QaP3K3TypedControlInputBindingSmoke),
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

        private static InputActionAsset CreateInputAsset(
            string assetName,
            List<UnityEngine.Object> created)
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = assetName;
            created.Add(asset);
            InputActionMap ui = asset.AddActionMap("UI");
            ui.AddAction("Submit", InputActionType.Button);
            InputActionMap player = asset.AddActionMap("Player");
            player.AddAction("Move", InputActionType.Value);
            return asset;
        }

        private static void ConfigurePlayerInput(
            PlayerInput playerInput,
            InputActionAsset actions,
            string initialMap)
        {
            if (playerInput == null)
            {
                throw new ArgumentNullException(nameof(playerInput));
            }

            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            playerInput.actions = actions;
            playerInput.defaultActionMap = initialMap;
        }

        private static void SetCurrentActionMap(
            PlayerInput playerInput,
            string actionMapName,
            string context)
        {
            if (playerInput == null)
            {
                throw new ArgumentNullException(nameof(playerInput));
            }

            if (playerInput.actions == null)
            {
                throw new InvalidOperationException(
                    $"{context} PlayerInput has no runtime InputActionAsset.");
            }

            InputActionMap actionMap =
                playerInput.actions.FindActionMap(actionMapName, throwIfNotFound: false);
            if (actionMap == null)
            {
                throw new InvalidOperationException(
                    $"{context} PlayerInput has no action map '{actionMapName}'.");
            }

            // The P3K.3 synthetic fixture keeps PlayerInput outside Unity's global
            // player-registration lifecycle. The binding contract is exercised through
            // the public currentActionMap state on the exact runtime action asset.
            playerInput.currentActionMap = actionMap;
            if (!ReferenceEquals(playerInput.currentActionMap, actionMap) ||
                !actionMap.enabled)
            {
                throw new InvalidOperationException(
                    $"{context} did not select and enable action map '{actionMapName}'.");
            }
        }

        private static void ConfigureSlotDeclaration(
            PlayerSlotDeclaration declaration,
            PlayerSlotId slotId,
            PlayerInput playerInput,
            string label)
        {
            MethodInfo configure = typeof(PlayerSlotDeclaration).GetMethod(
                "ConfigureForDiagnostics",
                InstanceAny,
                null,
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(PlayerInput),
                    typeof(string)
                },
                null);
            AssertNotNull(configure,
                "PlayerSlotDeclaration ConfigureForDiagnostics is missing.");
            configure.Invoke(declaration, new object[]
            {
                slotId.Value.Value,
                label,
                playerInput,
                "qa.p3k3.joined-slot"
            });
        }

        private static void ConfigureGateAdapter(
            UnityPlayerInputGateAdapter adapter,
            PlayerInput playerInput,
            PlayerSlotDeclaration slotDeclaration,
            string actionMapName)
        {
            SerializedObject serialized = new SerializedObject(adapter);
            serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
            serialized.FindProperty("sourceSlot").objectReferenceValue = slotDeclaration;
            serialized.FindProperty("gameplayActionMapName").stringValue = actionMapName;
            serialized.FindProperty("logStateChanges").boolValue = false;
            serialized.FindProperty("logMissingRuntimeOnce").boolValue = false;
            serialized.FindProperty("logMissingTargetOnce").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerActorDeclaration CreateActorDeclaration(
            Transform parent,
            ActorId actorId,
            PlayerInput playerInput,
            string label,
            List<UnityEngine.Object> created)
        {
            GameObject actorObject = new GameObject(label);
            actorObject.transform.SetParent(parent, false);
            created.Add(actorObject);
            PlayerActorDeclaration declaration =
                actorObject.AddComponent<PlayerActorDeclaration>();
            MethodInfo configure = typeof(PlayerActorDeclaration).GetMethod(
                "ConfigureForDiagnostics",
                InstanceAny | BindingFlags.DeclaredOnly,
                null,
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(PlayerInput),
                    typeof(string)
                },
                null);
            configure.Invoke(declaration, new object[]
            {
                actorId.Value.Value,
                label,
                playerInput,
                "qa.p3k3.actor-declaration"
            });
            return declaration;
        }

        private static string CurrentMapName(PlayerInput playerInput)
        {
            return playerInput != null && playerInput.currentActionMap != null
                ? playerInput.currentActionMap.name
                : string.Empty;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, InstanceAny);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
                type = type.BaseType;
            }
            throw new MissingFieldException(target.GetType().FullName, fieldName);
        }

        private static void AssertStatus(
            PlayerGameplayInputBindingResult result,
            PlayerGameplayInputBindingStatus expected,
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
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void AssertNotNull(object value, string message)
        {
            if (value == null) throw new InvalidOperationException(message);
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
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class HostFixture : IDisposable
        {
            private readonly List<UnityEngine.Object> created;

            private HostFixture(List<UnityEngine.Object> created)
            {
                this.created = created;
            }

            internal GameObject Root { get; private set; }
            internal LocalPlayerHostAuthoring Host { get; private set; }
            internal PlayerInput PlayerInput { get; private set; }
            internal PlayerSlotDeclaration SlotDeclaration { get; private set; }
            internal UnityPlayerInputGateAdapter GateAdapter { get; private set; }
            internal Transform ActorMount { get; private set; }
            internal PlayerActorDeclaration ActorDeclaration { get; private set; }

            internal static HostFixture Create(
                PlayerSlotId slotId,
                ActorId actorId,
                string initialActionMap,
                string gameplayActionMap,
                List<UnityEngine.Object> created)
            {
                var fixture = new HostFixture(created);
                fixture.Root = new GameObject("P3K3 Stable Local Player Host");
                fixture.Root.SetActive(false);
                created.Add(fixture.Root);
                fixture.PlayerInput = fixture.Root.AddComponent<PlayerInput>();
                ConfigurePlayerInput(
                    fixture.PlayerInput,
                    CreateInputAsset("P3K3 Actions", created),
                    initialActionMap);

                fixture.Host = fixture.Root.AddComponent<LocalPlayerHostAuthoring>();
                GameObject mountObject = new GameObject("Actor Mount");
                mountObject.transform.SetParent(fixture.Root.transform, false);
                created.Add(mountObject);
                fixture.ActorMount = mountObject.transform;

                fixture.SlotDeclaration =
                    fixture.Root.AddComponent<PlayerSlotDeclaration>();
                ConfigureSlotDeclaration(
                    fixture.SlotDeclaration,
                    slotId,
                    fixture.PlayerInput,
                    "P3K3 Slot");

                SetField(fixture.Host, "playerInput", fixture.PlayerInput);
                SetField(fixture.Host, "actorMount", fixture.ActorMount);
                SetField(fixture.Host, "stagedSlotDeclaration", fixture.SlotDeclaration);
                SetField(fixture.Host, "joinedPlayerSlotId", slotId);
                SetField(fixture.Host, "joinedConfiguredIndex", 0);
                FieldInfo admission = typeof(LocalPlayerHostAuthoring).GetField(
                    "admissionState",
                    InstanceAny);
                admission.SetValue(
                    fixture.Host,
                    Enum.ToObject(admission.FieldType, 20));

                fixture.GateAdapter =
                    fixture.Root.AddComponent<UnityPlayerInputGateAdapter>();
                ConfigureGateAdapter(
                    fixture.GateAdapter,
                    fixture.PlayerInput,
                    fixture.SlotDeclaration,
                    gameplayActionMap);

                fixture.ActorDeclaration = CreateActorDeclaration(
                    fixture.ActorMount,
                    actorId,
                    fixture.PlayerInput,
                    "P3K3 Prepared Logical Actor",
                    created);

                SetCurrentActionMap(
                    fixture.PlayerInput,
                    initialActionMap,
                    "P3K.3 Stable Local Player Host");
                return fixture;
            }

            public void Dispose()
            {
                // Shared cleanup is performed by the smoke's created-object stack.
            }
        }
    }
}

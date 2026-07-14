using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Synthetic technical smoke for P3J.3 contracts, attached Unity materialization,
    /// generated Actor identity, RuntimeContent registration and explicit rollback.
    /// </summary>
    public static class QaP3J3LogicalActorMaterializationAdapterSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3J.3 Run Logical Actor Materialization Adapter Smoke";
        private const string ContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeContext";
        private const string RuntimeContentRuntimeTypeName =
            "Immersive.Framework.RuntimeContent.RuntimeContentRuntime";
        private const string AdapterTypeName =
            "Immersive.Framework.PlayerParticipation.AttachedPlayerActorMaterializationAdapter";

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

                JoinedHostFixture primary = CreateJoinedHostFixture(
                    created,
                    "QA P3J3 Primary",
                    "qa.p3j3.player.1");
                RuntimeFixture runtime = CreateRuntimeFixture(
                    primary.ContextId,
                    RuntimeContentOwner.Activity(
                        "qa.p3j3.activity",
                        "QA P3J3 Activity"));
                ActorProfile validProfile = CreateActorProfile(
                    created,
                    "QA P3J3 Valid Actor",
                    "qa.p3j3.actor.valid",
                    CreateLogicalActorPrefab(created, "QA P3J3 Logical Actor", LogicalActorShape.Valid));

                PlayerActorMaterializationResult success = runtime.Materialize(
                    primary.Slot,
                    validProfile,
                    primary.Host,
                    "successful-materialization");
                AssertStatus(
                    success,
                    PlayerActorMaterializationStatus.SucceededStaged,
                    "Valid attached Logical Actor materialization failed.");
                completed.Add("valid-materialization-succeeds");

                AssertTrue(success.Request.OperationId.IsValid,
                    "Materialization has no framework-generated operation identity.");
                AssertTrue(success.Request.ActorId.IsValid,
                    "Materialization has no framework-generated ActorId.");
                AssertTrue(success.Request.ActorId.Value.Value != success.Request.ActorProfileId.Value.Value,
                    "Runtime ActorId reused ActorProfileId.");
                AssertTrue(success.Request.MaterializationRevision == 1,
                    "First adapter materialization did not receive revision 1.");
                completed.Add("framework-generates-operation-and-actor-identities");

                AssertNotNull(success.LogicalActorHost,
                    "Successful materialization has no Logical Actor Host instance.");
                AssertSame(primary.Host.ActorMount, success.LogicalActorHost.transform.parent,
                    "Logical Actor Host was not attached below the explicit Actor Mount.");
                AssertTrue(!success.LogicalActorHost.activeSelf,
                    "Logical Actor Host did not remain staged inactive.");
                completed.Add("logical-actor-staged-inactive-under-explicit-mount");

                AssertNotNull(success.PlayerActorDeclaration,
                    "Successful materialization has no PlayerActorDeclaration evidence.");
                AssertSame(primary.Host.PlayerInput, success.PlayerActorDeclaration.PlayerInput,
                    "Logical Actor PlayerInput evidence was not bound from the stable host.");
                AssertEqual(success.Request.ActorId, success.PlayerActorDeclaration.ActorId,
                    "PlayerActorDeclaration did not receive the generated ActorId.");
                AssertTrue(primary.Host.HasLogicalActor,
                    "Stable host does not expose the attached Logical Actor evidence.");
                completed.Add("logical-actor-identity-and-playerinput-bound-explicitly");

                AssertTrue(success.HasRuntimeContentRequest && success.HasRuntimeContentResult,
                    "Successful materialization omitted RuntimeContent request/result evidence.");
                AssertTrue(success.RuntimeContentResult.Succeeded,
                    "RuntimeContent registration did not succeed.");
                AssertEqual(success.Request.RuntimeContentIdentity, success.RuntimeContentResult.Identity,
                    "Typed and generic RuntimeContent identities diverged.");
                AssertEqual(1, runtime.HandleCount,
                    "RuntimeContent root did not register exactly one materialized handle.");
                completed.Add("runtime-content-handle-registered");

                AssertTrue(success.Snapshot.IsValid && success.Snapshot.IsStaged,
                    "Typed materialization snapshot is invalid or not staged.");
                AssertEqual(primary.Slot.PlayerSlotId, success.Snapshot.PlayerSlotId,
                    "Snapshot lost Player Slot identity.");
                AssertEqual(validProfile.ActorProfileId, success.Snapshot.ActorProfileId,
                    "Snapshot lost Actor Profile identity.");
                AssertEqual(success.Request.ActorId, success.Snapshot.ActorId,
                    "Snapshot lost runtime Actor identity.");
                completed.Add("typed-snapshot-preserves-functional-identities");

                AssertTrue(runtime.Rollback(success, "successful-materialization-cleanup", out string rollbackIssue),
                    "Explicit physical rollback failed. " + rollbackIssue);
                AssertTrue(!primary.Host.HasLogicalActor,
                    "Rollback left the Logical Actor attached to the stable host.");
                AssertEqual(0, runtime.HandleCount,
                    "Rollback leaked a RuntimeContent handle.");
                completed.Add("physical-rollback-releases-instance-and-runtime-handle");

                LocalPlayerHostAuthoring unjoinedHost = CreateHost(created, "QA P3J3 Unjoined Host");
                PlayerActorMaterializationResult unjoined = runtime.Materialize(
                    primary.Slot,
                    validProfile,
                    unjoinedHost,
                    "unjoined-host");
                AssertStatus(
                    unjoined,
                    PlayerActorMaterializationStatus.RejectedHostNotJoined,
                    "Unjoined Local Player Host was accepted.");
                completed.Add("joined-host-required");

                JoinedHostFixture foreignSlotFixture = CreateJoinedHostFixture(
                    created,
                    "QA P3J3 Foreign Slot",
                    "qa.p3j3.player.2");
                PlayerActorMaterializationResult slotMismatch = runtime.Materialize(
                    foreignSlotFixture.Slot,
                    validProfile,
                    primary.Host,
                    "slot-mismatch");
                AssertStatus(
                    slotMismatch,
                    PlayerActorMaterializationStatus.RejectedSlotMismatch,
                    "Host/Slot identity mismatch was accepted.");
                completed.Add("host-slot-identity-match-required");

                ActorProfile missingPrefabProfile = CreateActorProfile(
                    created,
                    "QA P3J3 Missing Prefab",
                    "qa.p3j3.actor.missing-prefab",
                    null);
                PlayerActorMaterializationResult missingPrefab = runtime.Materialize(
                    primary.Slot,
                    missingPrefabProfile,
                    primary.Host,
                    "missing-prefab");
                AssertStatus(
                    missingPrefab,
                    PlayerActorMaterializationStatus.RejectedMissingLogicalActorPrefab,
                    "Actor Profile without Logical Actor Host prefab was accepted.");
                completed.Add("missing-logical-actor-prefab-rejected");

                AssertProfileShapeRejected(
                    runtime,
                    primary,
                    created,
                    LogicalActorShape.WithPlayerInput,
                    PlayerActorMaterializationStatus.FailedUnexpectedPlayerInput,
                    "logical-actor-playerinput-rejected",
                    completed);
                AssertProfileShapeRejected(
                    runtime,
                    primary,
                    created,
                    LogicalActorShape.MissingPlayerActorDeclaration,
                    PlayerActorMaterializationStatus.FailedMissingPlayerActorDeclaration,
                    "missing-player-actor-declaration-rejected",
                    completed);
                AssertProfileShapeRejected(
                    runtime,
                    primary,
                    created,
                    LogicalActorShape.MultiplePlayerActorDeclarations,
                    PlayerActorMaterializationStatus.FailedMultiplePlayerActorDeclarations,
                    "multiple-player-actor-declarations-rejected",
                    completed);
                AssertProfileShapeRejected(
                    runtime,
                    primary,
                    created,
                    LogicalActorShape.AdditionalActorDeclaration,
                    PlayerActorMaterializationStatus.FailedUnexpectedActorDeclaration,
                    "additional-actor-declaration-rejected",
                    completed);

                RuntimeFixture missingRootRuntime = CreateRuntimeFixtureWithoutRoot(
                    primary.ContextId,
                    RuntimeContentOwner.Activity(
                        "qa.p3j3.missing-root",
                        "QA P3J3 Missing Root"));
                PlayerActorMaterializationResult missingRoot = missingRootRuntime.Materialize(
                    primary.Slot,
                    validProfile,
                    primary.Host,
                    "missing-runtime-root");
                AssertStatus(
                    missingRoot,
                    PlayerActorMaterializationStatus.RejectedScopeTransition,
                    "Materialization without a RuntimeContent scope root was accepted.");
                AssertTrue(!primary.Host.HasLogicalActor,
                    "Missing-root rejection created a physical Logical Actor.");
                completed.Add("runtime-scope-root-required-before-materialization");

                AssertTrue(runtime.HandleCount == 0,
                    "Negative cases leaked RuntimeContent handles.");
                completed.Add("negative-cases-leave-no-runtime-content-leaks");

                Debug.Log(
                    "[P3J3_LOGICAL_ACTOR_MATERIALIZATION_ADAPTER_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3J3_LOGICAL_ACTOR_MATERIALIZATION_ADAPTER_SMOKE] status='Failed' " +
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
            AssertTrue(typeof(PlayerActorMaterializationOperationId).IsValueType,
                "PlayerActorMaterializationOperationId must be a value identity.");
            AssertTrue(typeof(PlayerActorMaterializationRequest).IsValueType,
                "PlayerActorMaterializationRequest must be immutable value evidence.");
            AssertTrue(typeof(PlayerActorMaterializationSnapshot).IsValueType,
                "PlayerActorMaterializationSnapshot must be immutable value evidence.");
            AssertTrue(typeof(PlayerActorMaterializationResult).IsSealed,
                "PlayerActorMaterializationResult must be a final result contract.");
            completed.Add("typed-materialization-contracts-exist");

            ConstructorInfo[] publicOperationConstructors =
                typeof(PlayerActorMaterializationOperationId).GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public);
            AssertEqual(0, publicOperationConstructors.Length,
                "Callers can construct Player Actor materialization operation identities.");
            MethodInfo operationFactory = typeof(PlayerActorMaterializationOperationId)
                .GetMethod("TryCreate", StaticAny);
            AssertNotNull(operationFactory,
                "Framework-generated Player Actor materialization operation factory is missing.");
            AssertTrue(!operationFactory.IsPublic,
                "Player Actor materialization operation factory is public caller authority.");
            completed.Add("operation-identity-is-framework-generated");

            Type adapterType = ResolveRuntimeType(AdapterTypeName);
            AssertTrue(adapterType.IsSealed,
                "Attached Player Actor materialization adapter must be final.");
            AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(adapterType),
                "Attached Player Actor materialization adapter became a scene lifecycle component.");
            AssertTrue(adapterType.GetMethod("Awake", InstanceAny) == null &&
                adapterType.GetMethod("OnEnable", InstanceAny) == null &&
                adapterType.GetMethod("Start", InstanceAny) == null,
                "Attached Player Actor materialization adapter introduced implicit gameplay lifecycle execution.");
            completed.Add("adapter-is-scoped-plain-csharp-runtime");
        }

        private static void AssertProfileShapeRejected(
            RuntimeFixture runtime,
            JoinedHostFixture fixture,
            ICollection<UnityEngine.Object> created,
            LogicalActorShape shape,
            PlayerActorMaterializationStatus expectedStatus,
            string completedCase,
            ICollection<string> completed)
        {
            string suffix = shape.ToString();
            GameObject prefab = CreateLogicalActorPrefab(
                created,
                "QA P3J3 " + suffix,
                shape);
            ActorProfile profile = CreateActorProfile(
                created,
                "QA P3J3 " + suffix + " Profile",
                "qa.p3j3.actor." + suffix.ToLowerInvariant(),
                prefab);
            PlayerActorMaterializationResult result = runtime.Materialize(
                fixture.Slot,
                profile,
                fixture.Host,
                completedCase);
            AssertStatus(result, expectedStatus,
                $"Invalid Logical Actor shape '{shape}' was not rejected correctly.");
            AssertTrue(!fixture.Host.HasLogicalActor,
                $"Invalid Logical Actor shape '{shape}' left physical Actor evidence.");
            completed.Add(completedCase);
        }

        private static JoinedHostFixture CreateJoinedHostFixture(
            ICollection<UnityEngine.Object> created,
            string name,
            string slotId)
        {
            PlayerSlotProfile profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = name + " Slot";
            var profileSerialized = new SerializedObject(profile);
            profileSerialized.FindProperty("playerSlotId").stringValue = slotId;
            profileSerialized.FindProperty("displayName").stringValue = name + " Slot";
            profileSerialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);

            Type contextType = ResolveRuntimeType(ContextTypeName);
            MethodInfo createContext = contextType.GetMethod("TryCreate", StaticAny);
            AssertNotNull(createContext, "PlayerParticipationRuntimeContext.TryCreate was not found.");
            object[] createArguments =
            {
                new[] { profile },
                1,
                true,
                "QA.P3J3",
                "joined-host-fixture",
                null
            };
            var createResult = createContext.Invoke(null, createArguments)
                as PlayerParticipationOperationResult;
            AssertNotNull(createResult, "Participation context creation returned no result.");
            AssertTrue(createResult.Succeeded,
                "Participation context creation failed. " + createResult.ToDiagnosticString());
            object context = createArguments[5];
            AssertNotNull(context, "Participation context creation returned no context.");

            MethodInfo reserveMethod = contextType.GetMethod(
                "TryReserveNextAvailableSlot",
                InstanceAny);
            var reserveResult = reserveMethod.Invoke(
                context,
                new object[] { "QA.P3J3", "reserve-joined-host" })
                as PlayerParticipationOperationResult;
            AssertNotNull(reserveResult, "Slot reservation returned no result.");
            AssertTrue(reserveResult.Succeeded,
                "Slot reservation failed. " + reserveResult.ToDiagnosticString());

            LocalPlayerHostAuthoring host = CreateHost(created, name + " Host");
            MethodInfo stageMethod = typeof(LocalPlayerHostAuthoring).GetMethod(
                "TryStageAdmission",
                InstanceAny);
            AssertNotNull(stageMethod, "LocalPlayerHostAuthoring.TryStageAdmission was not found.");
            object[] stageArguments =
            {
                reserveResult.Slot,
                "QA.P3J3",
                "stage-joined-host",
                null
            };
            bool staged = (bool)stageMethod.Invoke(host, stageArguments);
            AssertTrue(staged,
                "Local Player Host admission staging failed. " + (stageArguments[3] ?? string.Empty));

            MethodInfo markJoinedMethod = contextType.GetMethod("TryMarkJoined", InstanceAny);
            var joinedResult = markJoinedMethod.Invoke(
                context,
                new object[]
                {
                    reserveResult.ReservationToken,
                    "QA.P3J3",
                    "commit-joined-host"
                }) as PlayerParticipationOperationResult;
            AssertNotNull(joinedResult, "Slot join commit returned no result.");
            AssertTrue(joinedResult.Succeeded,
                "Slot join commit failed. " + joinedResult.ToDiagnosticString());

            MethodInfo commitHostMethod = typeof(LocalPlayerHostAuthoring).GetMethod(
                "CommitStagedAdmission",
                InstanceAny);
            AssertNotNull(commitHostMethod,
                "LocalPlayerHostAuthoring.CommitStagedAdmission was not found.");
            commitHostMethod.Invoke(
                host,
                new object[]
                {
                    joinedResult.Slot,
                    "QA.P3J3",
                    "commit-joined-host"
                });

            MethodInfo snapshotMethod = contextType.GetMethod("CreateSnapshot", InstanceAny);
            var snapshot = snapshotMethod.Invoke(context, Array.Empty<object>())
                as PlayerParticipationSnapshot;
            AssertNotNull(snapshot, "Participation snapshot was not created.");
            return new JoinedHostFixture(
                context,
                snapshot.ContextId,
                joinedResult.Slot,
                host);
        }

        private static LocalPlayerHostAuthoring CreateHost(
            ICollection<UnityEngine.Object> created,
            string name)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            PlayerInput playerInput = root.AddComponent<PlayerInput>();
            var mount = new GameObject("ActorMount");
            mount.transform.SetParent(root.transform, false);
            LocalPlayerHostAuthoring host = root.AddComponent<LocalPlayerHostAuthoring>();
            var serialized = new SerializedObject(host);
            serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
            serialized.FindProperty("actorMount").objectReferenceValue = mount.transform;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(root);
            return host;
        }

        private static ActorProfile CreateActorProfile(
            ICollection<UnityEngine.Object> created,
            string name,
            string profileId,
            GameObject logicalActorHostPrefab)
        {
            var profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = name;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue = profileId;
            serialized.FindProperty("displayName").stringValue = name;
            serialized.FindProperty("actorKind").intValue = (int)ActorKind.Player;
            serialized.FindProperty("actorRole").intValue = (int)ActorRole.Protagonist;
            serialized.FindProperty("logicalActorHostPrefab").objectReferenceValue = logicalActorHostPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static GameObject CreateLogicalActorPrefab(
            ICollection<UnityEngine.Object> created,
            string name,
            LogicalActorShape shape)
        {
            var root = new GameObject(name);
            root.SetActive(false);
            switch (shape)
            {
                case LogicalActorShape.Valid:
                    root.AddComponent<PlayerActorDeclaration>();
                    break;
                case LogicalActorShape.WithPlayerInput:
                    root.AddComponent<PlayerInput>();
                    root.AddComponent<PlayerActorDeclaration>();
                    break;
                case LogicalActorShape.MissingPlayerActorDeclaration:
                    break;
                case LogicalActorShape.MultiplePlayerActorDeclarations:
                    root.AddComponent<PlayerActorDeclaration>();
                    var secondPlayerActor = new GameObject("Second Player Actor");
                    secondPlayerActor.transform.SetParent(root.transform, false);
                    secondPlayerActor.AddComponent<PlayerActorDeclaration>();
                    break;
                case LogicalActorShape.AdditionalActorDeclaration:
                    root.AddComponent<PlayerActorDeclaration>();
                    var additionalActor = new GameObject("Additional Actor");
                    additionalActor.transform.SetParent(root.transform, false);
                    additionalActor.AddComponent<ActorDeclaration>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }

            created.Add(root);
            return root;
        }

        private static RuntimeFixture CreateRuntimeFixture(
            string sessionContextId,
            RuntimeContentOwner owner)
        {
            Type runtimeType = ResolveRuntimeType(RuntimeContentRuntimeTypeName);
            object runtime = Activator.CreateInstance(runtimeType, true);
            AssertNotNull(runtime, "RuntimeContentRuntime could not be created.");

            MethodInfo createRootMethod = runtimeType.GetMethod("CreateScopeRoot", InstanceAny);
            AssertNotNull(createRootMethod, "RuntimeContentRuntime.CreateScopeRoot was not found.");
            createRootMethod.Invoke(
                runtime,
                new object[] { owner, "QA.P3J3", "create-materialization-root" });

            MethodInfo createContextMethod = runtimeType.GetMethod("TryCreateScopeContext", InstanceAny);
            AssertNotNull(createContextMethod,
                "RuntimeContentRuntime.TryCreateScopeContext was not found.");
            object[] contextArguments =
            {
                owner,
                "QA.P3J3",
                "logical-actor-materialization",
                null
            };
            bool contextCreated = (bool)createContextMethod.Invoke(runtime, contextArguments);
            AssertTrue(contextCreated, "Runtime Scope Context could not be created.");
            var scopeContext = (RuntimeScopeContext)contextArguments[3];
            return new RuntimeFixture(runtimeType, runtime, sessionContextId, scopeContext);
        }

        private static RuntimeFixture CreateRuntimeFixtureWithoutRoot(
            string sessionContextId,
            RuntimeContentOwner owner)
        {
            Type runtimeType = ResolveRuntimeType(RuntimeContentRuntimeTypeName);
            object runtime = Activator.CreateInstance(runtimeType, true);
            AssertNotNull(runtime, "RuntimeContentRuntime could not be created.");
            var scopeContext = new RuntimeScopeContext(
                owner,
                "QA.P3J3",
                "missing-root-materialization");
            return new RuntimeFixture(runtimeType, runtime, sessionContextId, scopeContext);
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = typeof(PlayerActorMaterializationRequest).Assembly.GetType(fullName, false);
            AssertNotNull(type, $"Runtime type '{fullName}' was not found.");
            return type;
        }

        private static void AssertStatus(
            PlayerActorMaterializationResult result,
            PlayerActorMaterializationStatus expected,
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

        private enum LogicalActorShape
        {
            Valid,
            WithPlayerInput,
            MissingPlayerActorDeclaration,
            MultiplePlayerActorDeclarations,
            AdditionalActorDeclaration
        }

        private sealed class JoinedHostFixture
        {
            internal JoinedHostFixture(
                object context,
                string contextId,
                PlayerSlotRuntimeSnapshot slot,
                LocalPlayerHostAuthoring host)
            {
                Context = context;
                ContextId = contextId;
                Slot = slot;
                Host = host;
            }

            internal object Context { get; }
            internal string ContextId { get; }
            internal PlayerSlotRuntimeSnapshot Slot { get; }
            internal LocalPlayerHostAuthoring Host { get; }
        }

        private sealed class RuntimeFixture
        {
            private readonly Type runtimeType;
            private readonly object runtime;
            private readonly Type adapterType;
            private readonly object adapter;
            private readonly RuntimeScopeContext scopeContext;

            internal RuntimeFixture(
                Type runtimeType,
                object runtime,
                string sessionContextId,
                RuntimeScopeContext scopeContext)
            {
                this.runtimeType = runtimeType;
                this.runtime = runtime;
                this.scopeContext = scopeContext;
                adapterType = ResolveRuntimeType(AdapterTypeName);
                ConstructorInfo constructor = adapterType.GetConstructor(
                    InstanceAny,
                    null,
                    new[] { runtimeType, typeof(string) },
                    null);
                AssertNotNull(constructor,
                    "AttachedPlayerActorMaterializationAdapter constructor was not found.");
                adapter = constructor.Invoke(new object[] { runtime, sessionContextId });
            }

            internal int HandleCount
            {
                get
                {
                    MethodInfo snapshotMethod = runtimeType.GetMethod(
                        "SnapshotHandles",
                        InstanceAny,
                        null,
                        new[] { typeof(RuntimeScopeContext) },
                        null);
                    AssertNotNull(snapshotMethod,
                        "RuntimeContentRuntime.SnapshotHandles was not found.");
                    var handles = snapshotMethod.Invoke(
                        runtime,
                        new object[] { scopeContext }) as RuntimeContentHandle[];
                    AssertNotNull(handles, "RuntimeContent handle snapshot was null.");
                    return handles.Length;
                }
            }

            internal PlayerActorMaterializationResult Materialize(
                PlayerSlotRuntimeSnapshot slot,
                ActorProfile profile,
                LocalPlayerHostAuthoring host,
                string reason)
            {
                MethodInfo method = adapterType.GetMethod("TryMaterialize", InstanceAny);
                AssertNotNull(method,
                    "AttachedPlayerActorMaterializationAdapter.TryMaterialize was not found.");
                return method.Invoke(
                    adapter,
                    new object[]
                    {
                        scopeContext,
                        slot,
                        profile,
                        host,
                        "QA.P3J3",
                        reason
                    }) as PlayerActorMaterializationResult;
            }

            internal bool Rollback(
                PlayerActorMaterializationResult result,
                string reason,
                out string issue)
            {
                PropertyInfo handleProperty = typeof(PlayerActorMaterializationResult)
                    .GetProperty("Handle", InstanceAny);
                AssertNotNull(handleProperty,
                    "PlayerActorMaterializationResult internal typed handle was not found.");
                object handle = handleProperty.GetValue(result);
                AssertNotNull(handle, "Successful result has no internal typed handle.");

                MethodInfo rollbackMethod = adapterType.GetMethod(
                    "TryRollbackMaterialization",
                    InstanceAny);
                AssertNotNull(rollbackMethod,
                    "AttachedPlayerActorMaterializationAdapter.TryRollbackMaterialization was not found.");
                object[] arguments =
                {
                    handle,
                    "QA.P3J3",
                    reason,
                    null
                };
                bool rolledBack = (bool)rollbackMethod.Invoke(adapter, arguments);
                issue = arguments[3] as string ?? string.Empty;
                return rolledBack;
            }
        }
    }
}

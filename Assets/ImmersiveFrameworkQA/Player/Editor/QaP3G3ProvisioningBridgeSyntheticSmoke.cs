using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Synthetic regression for reservation, Local Player Host provisioning, correlation,
    /// staged Slot admission and rollback. No real PlayerInputManager timing is required.
    /// </summary>
    public static class QaP3G3ProvisioningBridgeSyntheticSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3G.3 Run Provisioning Bridge Synthetic Smoke";
        private const string ContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeContext";
        private const string BridgeTypeName =
            "Immersive.Framework.PlayerParticipation.LocalPlayerProvisioningBridge";
        private static readonly BindingFlags StaticInternal =
            BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly BindingFlags InstanceInternal =
            BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();
            var disposables = new List<IDisposable>();

            try
            {
                RunSuccessfulOrderedJoinCases(created, disposables, completed);
                RunLateCallbackCases(created, disposables, completed);
                RunRollbackCases(created, disposables, completed);
                RunPolicyRejectionCases(created, disposables, completed);
                RunReentrantCase(created, disposables, completed);

                Debug.Log(
                    "[P3G3_PROVISIONING_BRIDGE_SYNTHETIC_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3G3_PROVISIONING_BRIDGE_SYNTHETIC_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = disposables.Count - 1; index >= 0; index--)
                {
                    disposables[index]?.Dispose();
                }

                for (int index = created.Count - 1; index >= 0; index--)
                {
                    if (created[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(created[index]);
                    }
                }
            }
        }

        private static void RunSuccessfulOrderedJoinCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 2, true, 2);
            PlayerInput firstPlayer = CreatePlayerHost(created, "QA P3G3 Player 1", true);
            fixture.Backend.NextPlayerInput = firstPlayer;
            fixture.Backend.CallbackPlayerInput = firstPlayer;
            fixture.Backend.EmitCallbackBeforeReturn = true;

            LocalPlayerJoinResult first = fixture.Join("first-join");
            AssertStatus(first, LocalPlayerJoinStatus.SucceededJoined,
                "First synthetic join failed.");
            AssertEqual(
                LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput,
                first.CallbackConfirmation,
                "Callback-first join was not correlated.");
            AssertEqual(0, first.Slot.ConfiguredIndex,
                "First join did not receive configured Slot index 0.");
            completed.Add("callback-first-single-join-succeeds");

            AssertNotNull(first.LocalPlayerHost,
                "Successful result has no Local Player Host evidence.");
            AssertSame(firstPlayer, first.LocalPlayerHost.PlayerInput,
                "Local Player Host does not resolve direct PlayerInput.");
            AssertTrue(first.LocalPlayerHost.HasJoinedSlot,
                "Local Player Host did not commit Slot binding.");
            AssertEqual(first.Slot.PlayerSlotId, first.LocalPlayerHost.JoinedPlayerSlotId,
                "Host Slot identity differs from Session commit.");
            completed.Add("technical-host-slot-binding-committed");

            AssertTrue(!first.LocalPlayerHost.HasLogicalActor,
                "Join materialized a Logical Actor unexpectedly.");
            AssertTrue(first.LocalPlayerHost.ActorMount != null &&
                first.LocalPlayerHost.ActorMount.GetComponentInChildren<ActorDeclaration>(true) == null,
                "Actor Mount is not empty after join.");
            completed.Add("join-leaves-logical-actor-unprepared");

            PlayerInput secondPlayer = CreatePlayerHost(created, "QA P3G3 Player 2", true);
            fixture.Backend.NextPlayerInput = secondPlayer;
            fixture.Backend.CallbackPlayerInput = secondPlayer;
            LocalPlayerJoinResult second = fixture.Join("second-join");
            AssertStatus(second, LocalPlayerJoinStatus.SucceededJoined,
                "Second synthetic join failed.");
            AssertEqual(1, second.Slot.ConfiguredIndex,
                "Second join did not receive configured Slot index 1.");
            AssertTrue(first.Slot.PlayerSlotId != second.Slot.PlayerSlotId,
                "Two Players received one Slot identity.");
            completed.Add("second-join-next-slot");

            AssertEqual(secondPlayer.playerIndex, second.UnityPlayerIndex,
                "Unity playerIndex evidence was not copied.");
            AssertEqual("PlayerSlot:qa.p3g3.player.2",
                second.Slot.PlayerSlotId.StableText,
                "Slot identity was inferred from playerIndex.");
            completed.Add("player-index-is-diagnostic-only");

            AssertTrue(fixture.Backend.ReservationObservedBeforeProvisioning,
                "Slot was not Reserved before backend provisioning.");
            completed.Add("reservation-exists-before-provisioning");

            AssertTrue(first.HasReservationEvidence && first.HasCommitEvidence &&
                !first.HasRollbackEvidence,
                "Successful result did not preserve reservation/commit evidence.");
            completed.Add("result-preserves-reservation-and-commit-evidence");
        }

        private static void RunLateCallbackCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 1, true, 1);
            PlayerInput player = CreatePlayerHost(created, "QA P3G3 Late Callback", true);
            fixture.Backend.NextPlayerInput = player;
            fixture.Backend.EmitCallbackBeforeReturn = false;

            LocalPlayerJoinResult result = fixture.Join("late-callback");
            AssertStatus(result, LocalPlayerJoinStatus.SucceededJoined,
                "Direct result without callback was rejected.");
            AssertEqual(LocalPlayerJoinCallbackConfirmation.Pending,
                result.CallbackConfirmation,
                "Missing callback did not remain Pending.");
            completed.Add("no-callback-admits-pending-confirmation");

            fixture.Backend.EmitJoined(player);
            AssertTrue(fixture.TryGetConfirmation(result.OperationId,
                    out LocalPlayerJoinCallbackConfirmation confirmation),
                "Late callback confirmation was not stored.");
            AssertEqual(LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput,
                confirmation,
                "Late callback did not confirm direct PlayerInput.");
            completed.Add("late-callback-confirms");

            using Fixture unexpectedFixture = CreateFixture(created, disposables, 1, true, 1);
            PlayerInput unexpectedPlayer = CreatePlayerHost(created, "QA P3G3 Unexpected", true);
            unexpectedFixture.Backend.EmitJoined(unexpectedPlayer);
            LocalPlayerJoinResult unexpected = unexpectedFixture.LastUnexpectedResult;
            AssertNotNull(unexpected, "Unexpected joined callback produced no result.");
            AssertStatus(unexpected, LocalPlayerJoinStatus.RejectedUnexpectedJoin,
                "Unexpected joined callback was accepted.");
            AssertEqual(1, unexpectedFixture.Backend.RejectCallCount,
                "Unexpected host was not rejected.");
            completed.Add("unexpected-callback-rejected");
        }

        private static void RunRollbackCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                fixture.Backend.ReturnNull = true;
                LocalPlayerJoinResult result = fixture.Join("null-result");
                AssertStatus(result,
                    LocalPlayerJoinStatus.RejectedProvisioningReturnedNull,
                    "Null provisioning result was accepted.");
                AssertRollbackRestoredAvailable(fixture, result, "Null result");
                completed.Add("join-null-rolls-back");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                PlayerInput destroyed = CreatePlayerHost(created, "QA P3G3 Destroyed", true);
                fixture.Backend.NextPlayerInput = destroyed;
                fixture.Backend.DestroyBeforeReturn = true;
                LocalPlayerJoinResult result = fixture.Join("destroyed-player-input");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedMissingPlayerInput,
                    "Destroyed PlayerInput was admitted.");
                AssertRollbackRestoredAvailable(fixture, result, "Destroyed PlayerInput");
                completed.Add("missing-player-input-rolls-back");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                PlayerInput missingHost = CreatePlayerHost(created, "QA P3G3 Missing Host", false);
                fixture.Backend.NextPlayerInput = missingHost;
                LocalPlayerJoinResult result = fixture.Join("missing-host");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedMissingLocalPlayerHost,
                    "PlayerInput without LocalPlayerHostAuthoring was admitted.");
                AssertRollbackRestoredAvailable(fixture, result, "Missing Local Player Host");
                completed.Add("missing-local-player-host-rolls-back");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                PlayerInput direct = CreatePlayerHost(created, "QA P3G3 Direct", true);
                PlayerInput callback = CreatePlayerHost(created, "QA P3G3 Divergent", true);
                fixture.Backend.NextPlayerInput = direct;
                fixture.Backend.CallbackPlayerInput = callback;
                fixture.Backend.EmitCallbackBeforeReturn = true;
                LocalPlayerJoinResult result = fixture.Join("callback-mismatch");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedCorrelationMismatch,
                    "Divergent callback was accepted.");
                AssertRollbackRestoredAvailable(fixture, result, "Callback mismatch");
                AssertTrue(fixture.Backend.RejectCallCount >= 2,
                    "Divergent hosts were not rejected.");
                completed.Add("callback-mismatch-rolls-back");
            }
        }

        private static void RunPolicyRejectionCases(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using (Fixture fixture = CreateFixture(created, disposables, 1, false, 1))
            {
                fixture.Backend.NextPlayerInput = CreatePlayerHost(created, "QA Closed", true);
                LocalPlayerJoinResult result = fixture.Join("joining-closed");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedJoiningClosed,
                    "Closed joining reached provisioning.");
                AssertEqual(0, fixture.Backend.JoinCallCount,
                    "Backend was called while joining was closed.");
                completed.Add("joining-closed-blocks-provisioning");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 0))
            {
                fixture.Backend.NextPlayerInput = CreatePlayerHost(created, "QA Capacity", true);
                LocalPlayerJoinResult result = fixture.Join("capacity-reached");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedCapacityReached,
                    "Zero Session capacity reached provisioning.");
                completed.Add("capacity-blocks-provisioning");
            }

            using (Fixture fixture = CreateFixture(created, disposables, 1, true, 1))
            {
                fixture.Backend.UsesManualJoin = false;
                fixture.Backend.NextPlayerInput = CreatePlayerHost(created, "QA Automatic", true);
                LocalPlayerJoinResult result = fixture.Join("manual-manager-required");
                AssertStatus(result, LocalPlayerJoinStatus.RejectedManagerConfiguration,
                    "Non-manual backend was accepted.");
                completed.Add("manual-manager-required");
            }
        }

        private static void RunReentrantCase(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            ICollection<string> completed)
        {
            using Fixture fixture = CreateFixture(created, disposables, 2, true, 2);
            PlayerInput player = CreatePlayerHost(created, "QA Reentrant", true);
            fixture.Backend.NextPlayerInput = player;
            fixture.Backend.CallbackPlayerInput = player;
            fixture.Backend.EmitCallbackBeforeReturn = true;

            LocalPlayerJoinResult nested = null;
            fixture.Backend.BeforeReturn = () => nested = fixture.Join("nested-join");
            LocalPlayerJoinResult outer = fixture.Join("outer-join");

            AssertStatus(nested, LocalPlayerJoinStatus.RejectedOperationInFlight,
                "Reentrant operation was accepted.");
            AssertStatus(outer, LocalPlayerJoinStatus.SucceededJoined,
                "Outer join failed after reentrant rejection.");
            completed.Add("reentrant-operation-rejected");
        }

        private static Fixture CreateFixture(
            ICollection<UnityEngine.Object> created,
            ICollection<IDisposable> disposables,
            int slotCount,
            bool joiningOpen,
            int capacity)
        {
            var profiles = new PlayerSlotProfile[slotCount];
            for (int index = 0; index < slotCount; index++)
            {
                profiles[index] = CreateProfile(
                    created,
                    $"QA P3G3 Slot {index + 1}",
                    $"qa.p3g3.player.{index + 1}");
            }

            object context = CreateContext(profiles, capacity, joiningOpen);
            GameObject validPrefab = CreateHostObject(created,
                "QA P3G3 Backend Host Prefab", true);
            var backend = new SyntheticProvisioningBackend
            {
                IsAvailable = true,
                UsesManualJoin = true,
                PlayerPrefab = validPrefab,
                TechnicalMaxPlayerCount = Math.Max(1, slotCount)
            };

            object bridge = CreateBridge(context, backend);
            var fixture = new Fixture(context, bridge, backend);
            disposables.Add(fixture);
            return fixture;
        }

        private static object CreateContext(
            IReadOnlyList<PlayerSlotProfile> profiles,
            int capacity,
            bool joiningOpen)
        {
            Type contextType = ResolveRuntimeType(ContextTypeName);
            MethodInfo method = contextType.GetMethod("TryCreate", StaticInternal);
            AssertNotNull(method, "PlayerParticipationRuntimeContext.TryCreate was not found.");
            object[] arguments =
            {
                profiles,
                capacity,
                joiningOpen,
                "QA.P3G3",
                "synthetic-context",
                null
            };
            var result = method.Invoke(null, arguments) as PlayerParticipationOperationResult;
            AssertNotNull(result, "Context creation returned no result.");
            AssertTrue(result.Succeeded, "Context creation failed. " + result.ToDiagnosticString());
            AssertNotNull(arguments[5], "Context creation returned no context.");
            return arguments[5];
        }

        private static object CreateBridge(
            object context,
            ILocalPlayerProvisioningBackend backend)
        {
            Type bridgeType = ResolveRuntimeType(BridgeTypeName);
            ConstructorInfo constructor = bridgeType.GetConstructor(
                InstanceInternal,
                null,
                new[] { context.GetType(), typeof(ILocalPlayerProvisioningBackend) },
                null);
            AssertNotNull(constructor, "LocalPlayerProvisioningBridge constructor was not found.");
            return constructor.Invoke(new object[] { context, backend });
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = typeof(LocalPlayerJoinRequest).Assembly.GetType(fullName, false);
            AssertNotNull(type, $"Runtime type '{fullName}' was not found.");
            return type;
        }

        private static PlayerSlotProfile CreateProfile(
            ICollection<UnityEngine.Object> created,
            string displayName,
            string slotId)
        {
            var profile = ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = slotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            created.Add(profile);
            return profile;
        }

        private static GameObject CreateHostObject(
            ICollection<UnityEngine.Object> created,
            string name,
            bool includeHost)
        {
            var gameObject = new GameObject(name);
            gameObject.SetActive(false);
            PlayerInput playerInput = gameObject.AddComponent<PlayerInput>();
            if (includeHost)
            {
                var mount = new GameObject("ActorMount");
                mount.transform.SetParent(gameObject.transform, false);
                LocalPlayerHostAuthoring host =
                    gameObject.AddComponent<LocalPlayerHostAuthoring>();
                var serialized = new SerializedObject(host);
                serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
                serialized.FindProperty("actorMount").objectReferenceValue = mount.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            created.Add(gameObject);
            return gameObject;
        }

        private static PlayerInput CreatePlayerHost(
            ICollection<UnityEngine.Object> created,
            string name,
            bool includeHost)
        {
            return CreateHostObject(created, name, includeHost)
                .GetComponent<PlayerInput>();
        }

        private static PlayerParticipationSnapshot Snapshot(object context)
        {
            MethodInfo method = context.GetType().GetMethod("CreateSnapshot", InstanceInternal);
            AssertNotNull(method, "CreateSnapshot was not found.");
            return method.Invoke(context, Array.Empty<object>()) as PlayerParticipationSnapshot;
        }

        private static void AssertRollbackRestoredAvailable(
            Fixture fixture,
            LocalPlayerJoinResult result,
            string label)
        {
            AssertTrue(result.HasReservationEvidence, label + " has no reservation evidence.");
            AssertTrue(result.HasRollbackEvidence, label + " has no rollback evidence.");
            AssertTrue(result.RollbackResult.Succeeded, label + " rollback failed.");
            PlayerParticipationSnapshot snapshot = fixture.Snapshot;
            AssertEqual(0, snapshot.ReservedCount, label + " stranded a Reserved Slot.");
            AssertEqual(0, snapshot.JoinedCount, label + " admitted a Player unexpectedly.");
            AssertEqual(1, snapshot.AvailableCount, label + " did not restore Available Slot.");
            if (result.LocalPlayerHost != null)
            {
                AssertTrue(!result.LocalPlayerHost.HasJoinedSlot,
                    label + " left public joined-host evidence.");
            }
        }

        private static void AssertStatus(
            LocalPlayerJoinResult result,
            LocalPlayerJoinStatus expected,
            string message)
        {
            AssertNotNull(result, message + " Result is null.");
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{result.Status}' diagnostics='{result.ToDiagnosticString()}'.");
            }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
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

        private static void AssertSame(object expected, object actual, string message)
        {
            if (!ReferenceEquals(expected, actual))
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

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\").Replace("'", "\\'")
                    .Replace("\r", " ").Replace("\n", " ");
        }

        private sealed class Fixture : IDisposable
        {
            private readonly object context;
            private readonly object bridge;
            private bool disposed;

            internal Fixture(
                object context,
                object bridge,
                SyntheticProvisioningBackend backend)
            {
                this.context = context;
                this.bridge = bridge;
                Backend = backend;
                Backend.SnapshotProvider = () => Snapshot(context);
            }

            internal SyntheticProvisioningBackend Backend { get; }
            internal PlayerParticipationSnapshot Snapshot =>
                QaP3G3ProvisioningBridgeSyntheticSmoke.Snapshot(context);
            internal LocalPlayerJoinResult LastUnexpectedResult =>
                GetProperty<LocalPlayerJoinResult>(bridge, "LastUnexpectedJoinResult");

            internal LocalPlayerJoinResult Join(string reason)
            {
                MethodInfo method = bridge.GetType().GetMethod("TryJoin", InstanceInternal);
                AssertNotNull(method, "LocalPlayerProvisioningBridge.TryJoin was not found.");
                return method.Invoke(
                    bridge,
                    new object[] { new LocalPlayerJoinRequest("QA.P3G3", reason) })
                    as LocalPlayerJoinResult;
            }

            internal bool TryGetConfirmation(
                LocalPlayerJoinOperationId operationId,
                out LocalPlayerJoinCallbackConfirmation confirmation)
            {
                MethodInfo method = bridge.GetType().GetMethod(
                    "TryGetCallbackConfirmation",
                    InstanceInternal);
                AssertNotNull(method, "TryGetCallbackConfirmation was not found.");
                object[] arguments = { operationId, null };
                bool found = (bool)method.Invoke(bridge, arguments);
                confirmation = (LocalPlayerJoinCallbackConfirmation)arguments[1];
                return found;
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;
                (bridge as IDisposable)?.Dispose();
            }

            private static T GetProperty<T>(object target, string name)
                where T : class
            {
                PropertyInfo property = target.GetType().GetProperty(name, InstanceInternal);
                AssertNotNull(property, $"Property '{name}' was not found.");
                return property.GetValue(target) as T;
            }
        }

        private sealed class SyntheticProvisioningBackend : ILocalPlayerProvisioningBackend
        {
            internal bool IsAvailable { get; set; }
            bool ILocalPlayerProvisioningBackend.IsAvailable => IsAvailable;
            internal bool UsesManualJoin { get; set; }
            bool ILocalPlayerProvisioningBackend.UsesManualJoin => UsesManualJoin;
            internal GameObject PlayerPrefab { get; set; }
            GameObject ILocalPlayerProvisioningBackend.PlayerPrefab => PlayerPrefab;
            internal int CurrentPlayerCount { get; set; }
            int ILocalPlayerProvisioningBackend.CurrentPlayerCount => CurrentPlayerCount;
            internal int TechnicalMaxPlayerCount { get; set; }
            int ILocalPlayerProvisioningBackend.TechnicalMaxPlayerCount => TechnicalMaxPlayerCount;
            internal PlayerInput NextPlayerInput { get; set; }
            internal PlayerInput CallbackPlayerInput { get; set; }
            internal bool EmitCallbackBeforeReturn { get; set; }
            internal bool ReturnNull { get; set; }
            internal bool DestroyBeforeReturn { get; set; }
            internal Action BeforeReturn { get; set; }
            internal Func<PlayerParticipationSnapshot> SnapshotProvider { get; set; }
            internal int JoinCallCount { get; private set; }
            internal int RejectCallCount { get; private set; }
            internal bool ReservationObservedBeforeProvisioning { get; private set; }

            public event Action<PlayerInput> PlayerJoined;

            public PlayerInput JoinPlayer(LocalPlayerJoinRequest request)
            {
                JoinCallCount++;
                PlayerParticipationSnapshot snapshot = SnapshotProvider?.Invoke();
                ReservationObservedBeforeProvisioning |= snapshot != null &&
                    snapshot.ReservedCount == 1;
                BeforeReturn?.Invoke();

                if (ReturnNull)
                {
                    return null;
                }

                PlayerInput result = NextPlayerInput;
                if (EmitCallbackBeforeReturn)
                {
                    PlayerJoined?.Invoke(CallbackPlayerInput ?? result);
                }

                if (DestroyBeforeReturn && !ReferenceEquals(result, null))
                {
                    UnityEngine.Object.DestroyImmediate(result.gameObject);
                }

                return result;
            }

            public void RejectPlayer(PlayerInput playerInput, string source, string reason)
            {
                RejectCallCount++;
            }

            internal void EmitJoined(PlayerInput playerInput)
            {
                PlayerJoined?.Invoke(playerInput);
            }
        }
    }
}

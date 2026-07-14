using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Real Play Mode smoke for Activity-owned Logical Player Actor enter, restart and exit.
    /// Preparation is driven only by FrameworkRuntimeHost Activity operations.
    /// </summary>
    public static class QaP3J6ActivityPlayerActorLifecycleSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3J.6 Run Activity Player Actor Lifecycle Smoke";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "P3J.6 Activity lifecycle smoke requires Play Mode.");
                completed.Add("play-mode-required");

                ActivityAsset activity = AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                    QaP3J6ActivityPlayerActorLifecycleSetup.ActivityPath);
                AssertNotNull(activity,
                    "P3J.6 Activity asset is missing. Apply the fixture outside Play Mode.");
                AssertEqual(PlayerParticipationRequirementLevel.LogicalActorsPrepared,
                    activity.PlayerParticipationRequirementsProfile.RequirementLevel,
                    "P3J.6 Activity does not require Logical Actors Prepared.");
                ActivityAsset negativeActivity =
                    AssetDatabase.LoadAssetAtPath<ActivityAsset>(
                        QaP3J6ActivityPlayerActorLifecycleSetup.NegativeActivityPath);
                AssertNotNull(negativeActivity,
                    "P3J.6 negative readiness Activity is missing.");
                completed.Add("activity-fixtures-resolved");

                LocalPlayerProvisioningAuthoring authoring = ResolveAuthoring();
                AssertTrue(authoring.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " +
                    authoring.RuntimeDiagnostic);
                PlayerInputManager manager = authoring.PlayerInputManager;
                AssertNotNull(manager, "Provisioning authoring has no PlayerInputManager.");
                AssertEqual(0, manager.playerCount,
                    "P3J.6 smoke is one-shot. Re-enter Play Mode before running again.");
                completed.Add("real-provisioning-runtime-ready");

                object runtimeHost = ResolveCurrentRuntimeHost();
                object preparationModule = ResolvePreparationModule(runtimeHost);
                completed.Add("runtime-host-preparation-module-resolved");

                PlayerParticipationOperationResult open =
                    InvokeReference<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryOpenJoining",
                        nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                        "activity-lifecycle-real-join");
                AssertTrue(open.Completed && open.Snapshot.JoiningOpen,
                    "Opening joining failed. " + open.ToDiagnosticString());

                LocalPlayerJoinResult join = authoring.RequestJoin(
                    nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                    "activity-lifecycle-real-join");
                AssertNotNull(join, "Real join returned no result.");
                AssertTrue(join.Succeeded,
                    "Real join failed. " + join.ToDiagnosticString());
                AssertTrue(!join.Slot.HasSelectedActor,
                    "Join selected an Actor before Activity entry.");
                AssertTrue(!join.LocalPlayerHost.HasLogicalActor,
                    "Join prepared a Logical Actor before Activity entry.");
                completed.Add("joined-host-remains-unselected-and-unprepared");

                LocalPlayerHostAuthoring stableHost = join.LocalPlayerHost;
                PlayerInput stablePlayerInput = join.PlayerInput;
                PlayerSlotId slotId = join.Slot.PlayerSlotId;

                object enterResult = await InvokeTaskResultAsync(
                    runtimeHost,
                    "RequestActivityAsync",
                    activity,
                    nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                    "activity-player-actor-enter");
                AssertTrue(ReadBool(enterResult, "Succeeded"),
                    "Activity entry request failed. " + ReadString(enterResult, "Message"));
                completed.Add("real-activity-entry-succeeded");

                object enterFlow = ReadProperty(enterResult, "ActivityFlowResult");
                object enterReadiness = ReadProperty(enterFlow, "ActivityReadinessState");
                AssertTrue(ReadBool(enterReadiness, "IsReady"),
                    "Activity readiness was blocked after successful Actor preparation. " +
                    ReadString(enterReadiness, "DiagnosticReason"));
                object execution = ReadProperty(enterFlow, "ActivityContentExecutionResult");
                AssertTrue(ReadBool(execution, "Executed"),
                    "Activity content execution lifecycle did not execute.");
                AssertTrue(!ReadBool(execution, "BlocksReadiness"),
                    "Activity Player Actor participant blocked readiness after a successful entry.");
                completed.Add("required-participant-contributes-ready-state");

                PlayerSlotRuntimeSnapshot selectedSlot = FindSlot(
                    authoring.RuntimeSnapshot,
                    slotId);
                AssertTrue(selectedSlot.HasSelectedActor,
                    "Activity entry did not select the Slot default Actor.");
                completed.Add("activity-entry-selected-default-actor");

                PlayerActorDeclaration firstDeclaration =
                    stableHost.ActorMount.GetComponentInChildren<PlayerActorDeclaration>(true);
                AssertNotNull(firstDeclaration,
                    "Activity entry did not materialize a Logical Player Actor.");
                AssertTrue(firstDeclaration.gameObject.activeInHierarchy,
                    "Activity-owned Logical Player Actor is not active.");
                AssertSame(stablePlayerInput, firstDeclaration.PlayerInput,
                    "Activity-owned Logical Actor lost stable PlayerInput binding.");
                ActorId firstActorId = firstDeclaration.ActorId;
                completed.Add("activity-entry-prepared-active-logical-actor");

                ActivityPlayerActorLifecycleSnapshot firstLifecycle =
                    GetLifecycleSnapshot(preparationModule);
                AssertEqual(ActivityPlayerActorLifecycleStatus.SucceededEntered,
                    firstLifecycle.Status,
                    "Lifecycle snapshot did not record successful Activity entry.");
                AssertTrue(firstLifecycle.Owner.IsValid &&
                    firstLifecycle.Owner.Scope ==
                        Immersive.Framework.RuntimeContent.RuntimeContentScope.Activity,
                    "Lifecycle snapshot does not expose Activity ownership.");
                AssertEqual(1, firstLifecycle.ProjectedSlotCount,
                    "Activity projection did not select one joined Slot.");
                AssertEqual(1, firstLifecycle.PreparedCount,
                    "Activity lifecycle did not record one prepared Actor.");
                AssertEqual(1, firstLifecycle.Slots.Count,
                    "Activity lifecycle did not expose one prepared Slot token.");
                PlayerActorPreparationToken firstPreparationToken =
                    firstLifecycle.Slots[0].PreparationToken;
                AssertTrue(firstPreparationToken.IsValid,
                    "Activity entry did not expose a valid preparation token.");
                completed.Add("activity-owner-and-slot-evidence-preserved");

                object restartResult = await InvokeAwaitableResultAsync(
                    runtimeHost,
                    "RestartActivityAsync",
                    3,
                    activity,
                    nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                    "activity-player-actor-restart");
                AssertTrue(ReadBool(restartResult, "Succeeded"),
                    "Activity restart failed. " + ReadString(restartResult, "Message"));
                completed.Add("real-activity-restart-succeeded");

                await Awaitable.NextFrameAsync();
                AssertTrue(firstDeclaration == null,
                    "Restart did not destroy the previous Activity-owned Logical Actor after the frame boundary.");
                PlayerActorDeclaration restartedDeclaration =
                    stableHost.ActorMount.GetComponentInChildren<PlayerActorDeclaration>(true);
                AssertNotNull(restartedDeclaration,
                    "Restart did not prepare a replacement Logical Player Actor.");
                AssertTrue(restartedDeclaration.ActorId != firstActorId,
                    "Restart reused the previous runtime Actor identity.");
                AssertSame(stablePlayerInput, restartedDeclaration.PlayerInput,
                    "Restart replaced or lost stable PlayerInput authority.");
                AssertSame(stableHost, join.LocalPlayerHost,
                    "Restart replaced the stable Local Player Host.");
                ActorId restartedActorId = restartedDeclaration.ActorId;
                completed.Add("restart-releases-old-and-prepares-new-identity");

                ActivityPlayerActorLifecycleSnapshot restartLifecycle =
                    GetLifecycleSnapshot(preparationModule);
                AssertEqual(ActivityPlayerActorLifecycleStatus.SucceededEntered,
                    restartLifecycle.Status,
                    "Restart re-entry did not end in entered lifecycle state.");
                AssertTrue(restartLifecycle.Owner.IsValid,
                    "Restart lifecycle lost Activity owner evidence.");
                AssertEqual(1, restartLifecycle.PreparedCount,
                    "Restart did not retain exactly one prepared Actor.");
                AssertEqual(1, restartLifecycle.Slots.Count,
                    "Restart lifecycle did not expose one current preparation token.");
                PlayerActorPreparationToken restartedPreparationToken =
                    restartLifecycle.Slots[0].PreparationToken;
                AssertTrue(restartedPreparationToken.IsValid &&
                    restartedPreparationToken != firstPreparationToken,
                    "Restart reused the stale Player Actor preparation token.");
                AssertTrue(
                    restartedPreparationToken.RuntimeContentIdentity !=
                        firstPreparationToken.RuntimeContentIdentity,
                    "Restart reused the previous RuntimeContent identity.");

                PlayerActorPreparationResult staleRelease =
                    InvokeReference<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryReleasePreparedActor",
                        slotId,
                        firstPreparationToken,
                        nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                        "reject-stale-restart-preparation-token");
                AssertNotNull(staleRelease,
                    "Stale preparation-token release returned no result.");
                AssertEqual(
                    PlayerActorPreparationStatus.RejectedForeignOrStalePreparation,
                    staleRelease.Status,
                    "Restart did not reject the stale preparation token.");
                AssertSame(restartedDeclaration,
                    stableHost.ActorMount.GetComponentInChildren<PlayerActorDeclaration>(true),
                    "Stale preparation-token rejection disturbed the current Actor.");
                AssertEqual(1,
                    GetPreparationSnapshot(preparationModule).PreparedCount,
                    "Stale preparation-token rejection changed current preparation state.");
                completed.Add(
                    "restart-rejects-stale-preparation-token-and-retains-one-actor");

                object clearResult = await InvokeTaskResultAsync(
                    runtimeHost,
                    "ClearActivityAsync",
                    nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                    "activity-player-actor-clear");
                AssertTrue(ReadBool(clearResult, "Succeeded"),
                    "Activity clear failed. " + ReadString(clearResult, "Message"));
                await Awaitable.NextFrameAsync();
                AssertTrue(restartedDeclaration == null,
                    "Activity clear did not destroy the Activity-owned Logical Actor after the frame boundary.");
                AssertTrue(!stableHost.HasLogicalActor,
                    "Activity clear left a Logical Actor under the stable host.");
                completed.Add("activity-clear-releases-owned-logical-actor");

                ActivityPlayerActorLifecycleSnapshot exitLifecycle =
                    GetLifecycleSnapshot(preparationModule);
                AssertEqual(ActivityPlayerActorLifecycleStatus.SucceededExited,
                    exitLifecycle.Status,
                    "Lifecycle snapshot did not record successful Activity exit.");
                AssertEqual(1, exitLifecycle.ReleasedCount,
                    "Activity exit did not record one released Actor.");
                completed.Add("activity-exit-evidence-preserved");

                PlayerSlotRuntimeSnapshot finalSlot = FindSlot(
                    authoring.RuntimeSnapshot,
                    slotId);
                AssertTrue(finalSlot.IsJoined && finalSlot.HasSelectedActor,
                    "Activity exit did not preserve Joined Slot and Actor selection.");
                AssertTrue(stableHost.IsJoined && stableHost.HasJoinedSlot,
                    "Activity exit invalidated the stable Local Player Host.");
                AssertSame(stablePlayerInput, stableHost.PlayerInput,
                    "Activity exit replaced PlayerInput authority.");
                completed.Add("exit-preserves-session-host-input-slot-and-selection");

                PlayerActorPreparationRuntimeHostSnapshot finalPreparation =
                    GetPreparationSnapshot(preparationModule);
                AssertEqual(0, finalPreparation.Preparation.PreparedCount,
                    "Activity clear left a prepared Actor summary.");
                AssertEqual(0,
                    finalPreparation.Preparation.RetainedReleaseFailures.Count,
                    "Activity lifecycle retained a failed release handle.");
                completed.Add("activity-lifecycle-leaves-no-preparation-leaks");

                object negativeResult = await InvokeTaskResultAsync(
                    runtimeHost,
                    "RequestActivityAsync",
                    negativeActivity,
                    nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                    "activity-player-actor-required-unjoined-slot");
                AssertTrue(ReadBool(negativeResult, "Succeeded"),
                    "Negative Activity request did not complete its lifecycle evaluation. " +
                    ReadString(negativeResult, "Message"));
                object negativeFlow = ReadProperty(
                    negativeResult,
                    "ActivityFlowResult");
                object negativeReadiness = ReadProperty(
                    negativeFlow,
                    "ActivityReadinessState");
                object negativeExecution = ReadProperty(
                    negativeFlow,
                    "ActivityContentExecutionResult");
                AssertTrue(ReadBool(negativeReadiness, "IsNotReady"),
                    "Required unjoined Slot did not make Activity readiness NotReady.");
                AssertTrue(ReadBool(negativeExecution, "BlocksReadiness"),
                    "Required unjoined Slot did not produce blocking participant evidence.");
                AssertTrue(!stableHost.HasLogicalActor,
                    "Failed Activity entry leaked a Logical Actor.");
                AssertEqual(0,
                    GetPreparationSnapshot(preparationModule).PreparedCount,
                    "Failed Activity entry leaked a prepared Actor summary.");
                completed.Add("required-preparation-failure-blocks-readiness-without-leaks");

                object negativeClear = await InvokeTaskResultAsync(
                    runtimeHost,
                    "ClearActivityAsync",
                    nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                    "clear-negative-readiness-activity");
                AssertTrue(ReadBool(negativeClear, "Succeeded"),
                    "Negative Activity clear failed. " +
                    ReadString(negativeClear, "Message"));
                completed.Add("negative-activity-cleared-explicitly");

                PlayerParticipationOperationResult close =
                    InvokeReference<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryCloseJoining",
                        nameof(QaP3J6ActivityPlayerActorLifecycleSmoke),
                        "activity-lifecycle-smoke-complete");
                AssertTrue(close.Completed && !close.Snapshot.JoiningOpen,
                    "Closing joining failed. " + close.ToDiagnosticString());
                AssertTrue(!manager.joiningEnabled,
                    "PlayerInputManager joining gate remained open.");
                completed.Add("joining-closed-after-lifecycle-flow");

                Debug.Log(
                    "[P3J6_ACTIVITY_PLAYER_ACTOR_LIFECYCLE_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' slot='{slotId.StableText}' " +
                    $"firstActor='{firstActorId.StableText}' " +
                    $"restartedActor='{restartedActorId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                LogFailure(inner, completed);
                throw inner;
            }
            catch (Exception exception)
            {
                LogFailure(exception, completed);
                throw;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return EditorApplication.isPlaying;
        }

        private static async Task<object> InvokeTaskResultAsync(
            object target,
            string methodName,
            params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, arguments.Length);
            object operation = method.Invoke(target, arguments);
            AssertTrue(operation is Task,
                $"Method '{methodName}' did not return Task.");
            await (Task)operation;
            return ReadProperty(operation, "Result");
        }

        private static async Task<object> InvokeAwaitableResultAsync(
            object target,
            string methodName,
            int parameterCount,
            params object[] arguments)
        {
            MethodInfo method = FindMethod(target.GetType(), methodName, parameterCount);
            object awaitable = method.Invoke(target, arguments);
            AssertNotNull(awaitable,
                $"Method '{methodName}' returned no Awaitable.");
            object awaiter = ReadMethod(awaitable.GetType(), "GetAwaiter")
                .Invoke(awaitable, null);
            PropertyInfo isCompleted = awaiter.GetType().GetProperty(
                "IsCompleted",
                InstanceAny);
            AssertNotNull(isCompleted,
                "Awaitable awaiter has no IsCompleted property.");
            while (!(bool)isCompleted.GetValue(awaiter))
            {
                await Awaitable.NextFrameAsync();
            }

            return ReadMethod(awaiter.GetType(), "GetResult")
                .Invoke(awaiter, null);
        }

        private static object ResolveCurrentRuntimeHost()
        {
            Type type = ResolveRuntimeType(RuntimeHostTypeName);
            MethodInfo method = type.GetMethod("TryGetCurrent", StaticAny);
            AssertNotNull(method, "FrameworkRuntimeHost.TryGetCurrent was not found.");
            object[] arguments = { null };
            bool resolved = (bool)method.Invoke(null, arguments);
            AssertTrue(resolved && arguments[0] != null,
                "Current FrameworkRuntimeHost was not resolved.");
            return arguments[0];
        }

        private static object ResolvePreparationModule(object runtimeHost)
        {
            Component host = runtimeHost as Component;
            AssertNotNull(host, "FrameworkRuntimeHost is not a Unity Component.");
            Type type = ResolveRuntimeType(PreparationModuleTypeName);
            Component module = host.GetComponent(type);
            AssertNotNull(module,
                "FrameworkRuntimeHost has no PlayerActorPreparationRuntimeHostModule.");
            return module;
        }

        private static ActivityPlayerActorLifecycleSnapshot GetLifecycleSnapshot(
            object module)
        {
            object[] arguments = { null };
            bool available = (bool)ReadMethod(
                module.GetType(),
                "TryGetActivityPlayerActorLifecycleSnapshot")
                .Invoke(module, arguments);
            AssertTrue(available,
                "Activity Player Actor lifecycle snapshot is unavailable.");
            var snapshot = arguments[0] as ActivityPlayerActorLifecycleSnapshot;
            AssertNotNull(snapshot,
                "Activity Player Actor lifecycle snapshot is missing.");
            return snapshot;
        }

        private static PlayerActorPreparationRuntimeHostSnapshot
            GetPreparationSnapshot(object module)
        {
            object[] arguments = { null };
            ReadMethod(module.GetType(), "TryGetSnapshot").Invoke(module, arguments);
            var snapshot = arguments[0] as PlayerActorPreparationRuntimeHostSnapshot;
            AssertNotNull(snapshot,
                "Player Actor preparation runtime-host snapshot is missing.");
            return snapshot;
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId == playerSlotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"Player Slot '{playerSlotId.StableText}' was not found in Session snapshot.");
        }

        private static T InvokeReference<T>(
            object target,
            string methodName,
            params object[] arguments)
            where T : class
        {
            return ReadMethod(target.GetType(), methodName)
                .Invoke(target, arguments) as T;
        }

        private static MethodInfo FindMethod(
            Type type,
            string methodName,
            int parameterCount)
        {
            MethodInfo[] methods = type.GetMethods(InstanceAny);
            for (int index = 0; index < methods.Length; index++)
            {
                if (string.Equals(methods[index].Name, methodName,
                        StringComparison.Ordinal) &&
                    methods[index].GetParameters().Length == parameterCount)
                {
                    return methods[index];
                }
            }

            throw new MissingMethodException(type.FullName, methodName);
        }

        private static MethodInfo ReadMethod(Type type, string name)
        {
            MethodInfo method = type.GetMethod(name, InstanceAny);
            AssertNotNull(method,
                $"Method '{type.FullName}.{name}' was not found.");
            return method;
        }

        private static object ReadProperty(object target, string name)
        {
            AssertNotNull(target,
                $"Cannot read property '{name}' from a null target.");
            PropertyInfo property = target.GetType().GetProperty(name, InstanceAny);
            AssertNotNull(property,
                $"Property '{target.GetType().FullName}.{name}' was not found.");
            return property.GetValue(target);
        }

        private static bool ReadBool(object target, string name)
        {
            return (bool)ReadProperty(target, name);
        }

        private static string ReadString(object target, string name)
        {
            return ReadProperty(target, name) as string ?? string.Empty;
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

        private static LocalPlayerProvisioningAuthoring ResolveAuthoring()
        {
            LocalPlayerProvisioningAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<LocalPlayerProvisioningAuthoring>(
                    FindObjectsInactive.Include);
            LocalPlayerProvisioningAuthoring resolved = null;
            int loadedCount = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                LocalPlayerProvisioningAuthoring candidate = candidates[index];
                if (candidate == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded)
                {
                    continue;
                }

                loadedCount++;
                resolved = candidate;
            }

            AssertEqual(1, loadedCount,
                "Expected exactly one loaded LocalPlayerProvisioningAuthoring.");
            return resolved;
        }

        private static void LogFailure(
            Exception exception,
            ICollection<string> completed)
        {
            Debug.LogError(
                "[P3J6_ACTIVITY_PLAYER_ACTOR_LIFECYCLE_SMOKE] status='Failed' " +
                $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                $"completed='{string.Join(",", completed)}'.");
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
    }
}

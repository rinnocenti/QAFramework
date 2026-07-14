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
    /// Play Mode smoke proving the official real-join-to-preparation path on one FrameworkRuntimeHost.
    /// One-shot per Play Mode because local Player leave remains outside P3J.5.
    /// </summary>
    public static class QaP3J5RuntimeHostEndToEndPreparationSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3J.5 Run Runtime Host End-to-End Preparation Smoke";
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
                    "P3J.5 end-to-end preparation smoke must run in Play Mode.");
                completed.Add("play-mode-required");

                LocalPlayerProvisioningAuthoring authoring = ResolveAuthoring();
                AssertTrue(authoring.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " +
                    authoring.RuntimeDiagnostic);
                completed.Add("real-provisioning-runtime-ready");

                object runtimeHost = ResolveCurrentRuntimeHost();
                object preparationModule = ResolvePreparationModule(runtimeHost);
                PlayerActorPreparationRuntimeHostSnapshot initialHostSnapshot =
                    GetHostSnapshot(preparationModule);
                AssertTrue(initialHostSnapshot.IsInitialized,
                    "Player Actor preparation runtime-host module is not initialized. " +
                    initialHostSnapshot.Diagnostic);
                AssertEqual(authoring.RuntimeSnapshot.ContextId,
                    initialHostSnapshot.SessionContextId,
                    "Participation and preparation modules use different Session identities.");
                completed.Add("preparation-module-composed-on-runtime-host");

                PlayerInputManager manager = authoring.PlayerInputManager;
                AssertNotNull(manager, "Provisioning authoring has no PlayerInputManager.");
                AssertEqual(0, manager.playerCount,
                    "P3J.5 smoke is one-shot. Re-enter Play Mode before running again.");
                AssertEqual(0, authoring.RuntimeSnapshot.JoinedCount,
                    "Session already contains a Joined Player.");
                AssertEqual(0, initialHostSnapshot.RegisteredHostCount,
                    "Preparation runtime already contains a registered host.");
                completed.Add("initial-runtime-state-clean");

                PlayerParticipationOperationResult openResult =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryOpenJoining",
                        nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                        "runtime-host-end-to-end-preparation");
                AssertTrue(openResult.Completed && openResult.Snapshot.JoiningOpen,
                    "Opening joining through preparation module failed. " +
                    openResult.ToDiagnosticString());
                AssertTrue(manager.joiningEnabled,
                    "Real PlayerInputManager technical joining gate did not open.");
                completed.Add("joining-routed-through-runtime-host-preparation-module");

                var joinRequest = new LocalPlayerJoinRequest(
                    nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                    "runtime-host-end-to-end-preparation");
                LocalPlayerJoinResult joinResult = authoring.RequestJoin(joinRequest);
                AssertNotNull(joinResult, "Real local Player join returned no result.");
                AssertTrue(joinResult.Succeeded,
                    "Real local Player join failed. " + joinResult.ToDiagnosticString());
                AssertEqual(LocalPlayerJoinCallbackConfirmation.ConfirmedSamePlayerInput,
                    joinResult.CallbackConfirmation,
                    "PlayerInputManager joined callback did not confirm the returned PlayerInput.");
                completed.Add("public-authoring-real-join-completed");

                LocalPlayerHostAuthoring host = joinResult.LocalPlayerHost;
                AssertNotNull(host, "Join result has no stable Local Player Host.");
                AssertTrue(host.IsJoined && host.HasJoinedSlot,
                    "Stable Local Player Host did not preserve Joined Slot evidence.");
                AssertEqual(joinResult.Slot.PlayerSlotId, host.JoinedPlayerSlotId,
                    "Joined host and Session Slot identities differ.");
                AssertSame(joinResult.PlayerInput, host.PlayerInput,
                    "Stable host does not own the PlayerInput returned by provisioning.");
                AssertTrue(!host.HasLogicalActor,
                    "Local Player join prepared a Logical Actor implicitly.");
                completed.Add("stable-host-joined-without-logical-actor");

                PlayerActorPreparationRuntimeHostSnapshot joinedHostSnapshot =
                    GetHostSnapshot(preparationModule);
                AssertEqual(1, joinedHostSnapshot.RegisteredHostCount,
                    "Successful real join was not registered with preparation authority.");
                AssertEqual(LocalPlayerJoinStatus.SucceededJoined,
                    joinedHostSnapshot.LastJoinStatus,
                    "Runtime-host diagnostics lost the successful join status.");
                completed.Add("joined-host-registered-explicitly");

                PlayerSlotId slotId = joinResult.Slot.PlayerSlotId;
                PlayerActorSelectionResult selected = Invoke<PlayerActorSelectionResult>(
                    preparationModule,
                    "TrySelectDefaultActor",
                    slotId,
                    joinResult.Slot.SelectionRevision,
                    nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                    "select-default-before-preparation");
                AssertNotNull(selected, "Default Actor selection returned no result.");
                AssertEqual(PlayerActorSelectionStatus.SucceededSelected,
                    selected.Status,
                    "Default Actor selection failed. " + selected.ToDiagnosticString());
                AssertNotNull(selected.SelectedActorProfile,
                    "Default Actor selection has no ActorProfile evidence.");
                completed.Add("default-actor-selected-through-preparation-authority");

                RuntimeScopeContext scopeContext = CreateExplicitScopeContext(
                    runtimeHost,
                    out object runtimeContent,
                    out Type runtimeContentType);
                AssertTrue(scopeContext.IsValid,
                    "Explicit Runtime Scope Context is invalid.");
                completed.Add("explicit-runtime-scope-created");

                PlayerActorPreparationResult prepared = Invoke<PlayerActorPreparationResult>(
                    preparationModule,
                    "TryPrepareSelectedActor",
                    scopeContext,
                    slotId,
                    nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                    "prepare-selected-default-actor");
                AssertNotNull(prepared, "Prepare Selected Actor returned no result.");
                AssertEqual(PlayerActorPreparationStatus.SucceededPrepared,
                    prepared.Status,
                    "Prepare Selected Actor failed. " + prepared.ToDiagnosticString());
                AssertTrue(prepared.CurrentSummary.IsPrepared &&
                    prepared.CurrentSummary.Token.IsValid,
                    "Preparation result has no valid prepared summary/token.");
                completed.Add("selected-logical-actor-prepared");

                PlayerActorDeclaration declaration =
                    host.ActorMount.GetComponentInChildren<PlayerActorDeclaration>(true);
                AssertNotNull(declaration,
                    "Prepared Logical Actor has no PlayerActorDeclaration under ActorMount.");
                AssertTrue(declaration.gameObject.activeInHierarchy,
                    "Prepared Logical Actor is not active.");
                AssertSame(host.PlayerInput, declaration.PlayerInput,
                    "Prepared Logical Actor did not receive the stable host PlayerInput binding.");
                AssertTrue(declaration.ActorId.IsValid,
                    "Prepared Logical Actor has no framework-generated ActorId.");
                AssertTrue(!string.Equals(
                        declaration.ActorId.StableText,
                        selected.SelectedActorProfileId.StableText,
                        StringComparison.Ordinal),
                    "Runtime ActorId reused the immutable ActorProfileId.");
                completed.Add("logical-actor-active-bound-and-runtime-identified");

                PlayerActorPreparationRuntimeHostSnapshot preparedHostSnapshot =
                    GetHostSnapshot(preparationModule);
                AssertEqual(1, preparedHostSnapshot.PreparedCount,
                    "Runtime-host snapshot did not report one prepared Actor.");
                AssertEqual(0, preparedHostSnapshot.ReleaseFailedCount,
                    "Runtime-host snapshot reports an unexpected release failure.");
                completed.Add("runtime-host-preparation-diagnostics-updated");

                PlayerActorPreparationResult idempotentPrepare =
                    Invoke<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryPrepareSelectedActor",
                        scopeContext,
                        slotId,
                        nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                        "prepare-selected-default-actor-again");
                AssertEqual(PlayerActorPreparationStatus.SucceededAlreadyPrepared,
                    idempotentPrepare.Status,
                    "Repeated prepare was not idempotent. " +
                    idempotentPrepare.ToDiagnosticString());
                AssertEqual(prepared.CurrentSummary.Token,
                    idempotentPrepare.CurrentSummary.Token,
                    "Idempotent prepare replaced the current Actor identity.");
                completed.Add("runtime-host-prepare-idempotent");

                var clearWhilePreparedRequest = new PlayerActorSelectionRequest(
                    slotId,
                    null,
                    nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                    "selection-mutation-while-prepared",
                    selected.SelectionRevision);
                PlayerActorSelectionResult guardedClear =
                    Invoke<PlayerActorSelectionResult>(
                        preparationModule,
                        "TryClearActorSelection",
                        clearWhilePreparedRequest);
                AssertEqual(PlayerActorSelectionStatus.RejectedLogicalActorAlreadyPrepared,
                    guardedClear.Status,
                    "Prepared Slot allowed direct Actor selection mutation. " +
                    guardedClear.ToDiagnosticString());
                completed.Add("selection-mutation-guarded-while-prepared");

                PlayerInput stablePlayerInput = host.PlayerInput;
                GameObject stableHostObject = host.gameObject;
                PlayerActorPreparationResult released =
                    Invoke<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryReleasePreparedActor",
                        slotId,
                        prepared.CurrentSummary.Token,
                        nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                        "release-prepared-default-actor");
                AssertTrue(released.Succeeded,
                    "Prepared Actor release failed. " + released.ToDiagnosticString());

                // Unity Object.Destroy is deferred until the frame boundary in Play Mode.
                // The release contract is already complete here, but physical destruction
                // must be observed only after Unity processes the pending destroy queue.
                await Awaitable.NextFrameAsync();
                AssertTrue(declaration == null,
                    "Released Logical Actor instance was not destroyed after the frame boundary.");
                completed.Add("prepared-logical-actor-released-explicitly");

                AssertSame(stableHostObject, host.gameObject,
                    "Logical Actor release replaced the stable Local Player Host.");
                AssertSame(stablePlayerInput, host.PlayerInput,
                    "Logical Actor release replaced the stable PlayerInput.");
                AssertTrue(host.IsJoined && host.HasJoinedSlot,
                    "Logical Actor release removed Joined Slot evidence.");
                completed.Add("release-preserves-stable-host-playerinput-and-slot");

                var clearAfterReleaseRequest = new PlayerActorSelectionRequest(
                    slotId,
                    null,
                    nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                    "selection-mutation-after-release",
                    selected.SelectionRevision);
                PlayerActorSelectionResult cleared = Invoke<PlayerActorSelectionResult>(
                    preparationModule,
                    "TryClearActorSelection",
                    clearAfterReleaseRequest);
                AssertEqual(PlayerActorSelectionStatus.SucceededCleared,
                    cleared.Status,
                    "Actor selection mutation did not recover after release. " +
                    cleared.ToDiagnosticString());
                completed.Add("selection-mutation-restored-after-release");

                RuntimeContentHandle[] handles = SnapshotHandles(
                    runtimeContent,
                    runtimeContentType,
                    scopeContext);
                AssertEqual(0, handles.Length,
                    "Released Actor left RuntimeContent handles in the explicit scope.");
                PlayerActorPreparationRuntimeHostSnapshot finalHostSnapshot =
                    GetHostSnapshot(preparationModule);
                AssertEqual(0, finalHostSnapshot.PreparedCount,
                    "Runtime-host snapshot retained a prepared Actor after release.");
                AssertEqual(0, finalHostSnapshot.RetainedReleaseFailureCount,
                    "Runtime-host snapshot retained failed cleanup evidence.");
                completed.Add("release-leaves-no-runtime-content-leaks");

                object[] releaseAllArguments =
                {
                    nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                    "end-to-end-smoke-shutdown-check",
                    0,
                    0,
                    null
                };
                bool releaseAll = (bool)GetMethod(
                    preparationModule.GetType(),
                    "TryReleaseAllPreparedActors").Invoke(
                        preparationModule,
                        releaseAllArguments);
                AssertTrue(releaseAll,
                    "Idempotent runtime-host release-all failed. " +
                    (releaseAllArguments[4] as string));
                AssertEqual(0, (int)releaseAllArguments[2],
                    "Release-all found an unexpected prepared Actor.");
                completed.Add("runtime-host-release-all-idempotent");

                PlayerParticipationOperationResult closeResult =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryCloseJoining",
                        nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                        "runtime-host-end-to-end-preparation-complete");
                AssertTrue(closeResult.Completed && !closeResult.Snapshot.JoiningOpen,
                    "Closing joining through preparation module failed. " +
                    closeResult.ToDiagnosticString());
                AssertTrue(!manager.joiningEnabled,
                    "Real PlayerInputManager joining gate remained open.");
                completed.Add("joining-closed-after-end-to-end-flow");

                Debug.Log(
                    "[P3J5_RUNTIME_HOST_END_TO_END_PREPARATION_SMOKE] status='Passed' " +
                    $"cases='{completed.Count}' session='{finalHostSnapshot.SessionContextId}' " +
                    $"slot='{slotId.StableText}' actor='{prepared.CurrentSummary.Materialization.ActorId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                Debug.LogError(
                    "[P3J5_RUNTIME_HOST_END_TO_END_PREPARATION_SMOKE] status='Failed' " +
                    $"exception='{inner.GetType().Name}' message='{Escape(inner.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw inner;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3J5_RUNTIME_HOST_END_TO_END_PREPARATION_SMOKE] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return EditorApplication.isPlaying;
        }

        private static object ResolveCurrentRuntimeHost()
        {
            Type runtimeHostType = ResolveRuntimeType(RuntimeHostTypeName);
            MethodInfo tryGetCurrent = runtimeHostType.GetMethod(
                "TryGetCurrent",
                StaticAny);
            AssertNotNull(tryGetCurrent,
                "FrameworkRuntimeHost.TryGetCurrent was not found.");
            object[] arguments = { null };
            bool resolved = (bool)tryGetCurrent.Invoke(null, arguments);
            AssertTrue(resolved && arguments[0] != null,
                "Current FrameworkRuntimeHost was not resolved.");
            return arguments[0];
        }

        private static object ResolvePreparationModule(object runtimeHost)
        {
            Type moduleType = ResolveRuntimeType(PreparationModuleTypeName);
            Component hostComponent = runtimeHost as Component;
            AssertNotNull(hostComponent,
                "FrameworkRuntimeHost is not a Unity Component.");
            Component module = hostComponent.GetComponent(moduleType);
            AssertNotNull(module,
                "FrameworkRuntimeHost has no PlayerActorPreparationRuntimeHostModule.");
            return module;
        }

        private static PlayerActorPreparationRuntimeHostSnapshot GetHostSnapshot(
            object module)
        {
            object[] arguments = { null };
            bool available = (bool)GetMethod(module.GetType(), "TryGetSnapshot").Invoke(
                module,
                arguments);
            var snapshot = arguments[0] as PlayerActorPreparationRuntimeHostSnapshot;
            AssertNotNull(snapshot,
                "Player Actor preparation runtime-host snapshot is missing.");
            AssertTrue(available || !snapshot.IsInitialized,
                "Snapshot availability and initialization evidence disagree.");
            return snapshot;
        }

        private static RuntimeScopeContext CreateExplicitScopeContext(
            object runtimeHost,
            out object runtimeContent,
            out Type runtimeContentType)
        {
            PropertyInfo runtimeContentProperty = runtimeHost.GetType().GetProperty(
                "RuntimeContentRuntime",
                InstanceAny);
            AssertNotNull(runtimeContentProperty,
                "FrameworkRuntimeHost.RuntimeContentRuntime was not found.");
            runtimeContent = runtimeContentProperty.GetValue(runtimeHost);
            AssertNotNull(runtimeContent,
                "FrameworkRuntimeHost has no RuntimeContentRuntime.");
            runtimeContentType = runtimeContent.GetType();

            RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                "qa.p3j5.activity." + Guid.NewGuid().ToString("N"),
                "P3J.5 End-to-End Preparation");
            GetMethod(runtimeContentType, "CreateScopeRoot").Invoke(
                runtimeContent,
                new object[]
                {
                    owner,
                    nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                    "create-explicit-preparation-scope"
                });

            object[] contextArguments =
            {
                owner,
                nameof(QaP3J5RuntimeHostEndToEndPreparationSmoke),
                "runtime-host-end-to-end-preparation",
                null
            };
            bool created = (bool)GetMethod(
                runtimeContentType,
                "TryCreateScopeContext").Invoke(
                    runtimeContent,
                    contextArguments);
            AssertTrue(created,
                "RuntimeContentRuntime could not create the explicit preparation scope context.");
            return (RuntimeScopeContext)contextArguments[3];
        }

        private static RuntimeContentHandle[] SnapshotHandles(
            object runtimeContent,
            Type runtimeContentType,
            RuntimeScopeContext scopeContext)
        {
            return GetMethod(runtimeContentType, "SnapshotHandles").Invoke(
                    runtimeContent,
                    new object[] { scopeContext }) as RuntimeContentHandle[] ??
                Array.Empty<RuntimeContentHandle>();
        }

        private static T Invoke<T>(object target, string methodName, params object[] arguments)
            where T : class
        {
            return GetMethod(target.GetType(), methodName).Invoke(target, arguments) as T;
        }

        private static MethodInfo GetMethod(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName, InstanceAny);
            AssertNotNull(method,
                $"Method '{type.FullName}.{methodName}' was not found.");
            return method;
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

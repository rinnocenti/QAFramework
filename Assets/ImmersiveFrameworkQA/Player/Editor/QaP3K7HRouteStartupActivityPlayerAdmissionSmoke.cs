using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
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
    /// One-shot Play Mode proof for the official FrameworkRuntimeHost-scoped
    /// Route Startup Activity Player admission integration.
    /// </summary>
    public static class QaP3K7HRouteStartupActivityPlayerAdmissionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.7H Run Route Startup Activity Player Admission Smoke";
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";
        private const string GameplayModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayRuntimeHostModule";
        private const string EndpointSourceTypeName =
            "Immersive.Framework.PlayerParticipation.HostScopedPlayerGameplayChainEndpointSource";
        private const string TargetRoutePrimaryScenePath =
            "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleRouteB.unity";
        private const string TargetRoutePrimarySceneName =
            "QA_LifecycleRouteB";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags StaticAny =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static async void Run()
        {
            var completed = new List<string>();

            object runtimeContent = null;
            Type runtimeContentType = null;
            RuntimeScopeContext currentContext = default;

            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "P3K.7H Route Startup admission smoke must run in Play Mode.");
                completed.Add("play-mode-required");

                LocalPlayerProvisioningAuthoring authoring =
                    await AwaitProvisioningAuthoringAsync();
                AssertTrue(authoring.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " +
                    authoring.RuntimeDiagnostic);
                completed.Add("provisioning-runtime-ready");

                object runtimeHost = ResolveCurrentRuntimeHost();
                completed.Add("runtime-host-resolved");

                object preparationModule = ResolveHostComponent(
                    runtimeHost,
                    PreparationModuleTypeName,
                    "PlayerActorPreparationRuntimeHostModule");
                object gameplayModule = ResolveHostComponent(
                    runtimeHost,
                    GameplayModuleTypeName,
                    "PlayerGameplayRuntimeHostModule");
                completed.Add("official-player-authorities-resolved");

                PlayerActorPreparationRuntimeHostSnapshot initialPreparation =
                    GetPreparationSnapshot(preparationModule);
                PlayerGameplayRuntimeHostSnapshot initialGameplay =
                    GetGameplaySnapshot(gameplayModule);
                AssertTrue(initialPreparation.IsInitialized &&
                    initialGameplay.IsInitialized,
                    "P3J/P3K official runtime composition is not initialized.");
                AssertEqual(initialPreparation.SessionContextId,
                    initialGameplay.SessionContextId,
                    "P3J and P3K Session identities differ.");
                completed.Add("session-authorities-initialized");

                AssertTrue(initialGameplay.LifecycleAdmission != null,
                    "P3K.7H lifecycle admission snapshot is missing.");
                AssertEqual(ActivityPlayerLifecycleAdmissionState.None,
                    initialGameplay.LifecycleAdmission.State,
                    "P3K.7H lifecycle admission is not initially clean.");
                completed.Add("lifecycle-admission-initially-clean");

                PlayerInputManager manager = authoring.PlayerInputManager;
                AssertNotNull(manager,
                    "Provisioning authoring has no PlayerInputManager.");
                AssertEqual(0, manager.playerCount,
                    "P3K.7H smoke is one-shot. Re-enter Play Mode before running again.");

                PlayerParticipationOperationResult opened =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryOpenJoining",
                        nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                        "route-startup-lifecycle-admission");
                AssertTrue(opened.Completed && opened.Snapshot.JoiningOpen,
                    "Opening joining failed. " + opened.ToDiagnosticString());
                completed.Add("joining-opened");

                LocalPlayerJoinResult joined = authoring.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                        "route-startup-lifecycle-admission"));
                AssertNotNull(joined,
                    "Real local Player join returned no result.");
                AssertTrue(joined.Succeeded,
                    "Real local Player join failed. " +
                    joined.ToDiagnosticString());
                completed.Add("real-local-player-joined");

                LocalPlayerHostAuthoring stableHost = joined.LocalPlayerHost;
                PlayerInput stablePlayerInput = joined.PlayerInput;
                PlayerSlotId slotId = joined.Slot.PlayerSlotId;
                AssertNotNull(stableHost,
                    "Joined Player has no stable Local Player Host.");
                AssertNotNull(stablePlayerInput,
                    "Joined Player has no PlayerInput.");
                AssertSame(stablePlayerInput, stableHost.PlayerInput,
                    "Stable host does not own the joined PlayerInput.");
                completed.Add("stable-player-host-preserved");

                PlayerActorSelectionResult selected =
                    Invoke<PlayerActorSelectionResult>(
                        preparationModule,
                        "TrySelectDefaultActor",
                        slotId,
                        joined.Slot.SelectionRevision,
                        nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                        "select-current-actor");
                AssertNotNull(selected,
                    "Default Actor selection returned no result.");
                AssertTrue(selected.Succeeded &&
                    selected.SelectedActorProfile != null,
                    "Default Actor selection failed. " +
                    selected.ToDiagnosticString());
                completed.Add("default-actor-selected");

                RouteAsset currentRoute =
                    ResolveCurrentRoute(runtimeHost);
                AssertNotNull(currentRoute,
                    "FrameworkRuntimeHost has no current Route.");
                AssertTrue(currentRoute.HasPrimaryScene,
                    "Current Route has no Primary Scene for the Route Startup smoke.");

                ActivityAsset currentActivity =
                    ResolveCurrentActivity(runtimeHost);
                AssertNotNull(currentActivity,
                    "FrameworkRuntimeHost has no current Activity.");
                completed.Add("current-activity-resolved");

                currentContext = CreateActivityScopeContext(
                    runtimeHost,
                    currentActivity.ActivityName,
                    currentActivity.ActivityName,
                    out runtimeContent,
                    out runtimeContentType);
                RuntimeContentOwner currentOwner =
                    RuntimeContentOwner.Activity(
                        currentActivity.ActivityName,
                        currentActivity.ActivityName);
                AssertEqual(currentOwner, currentContext.Owner,
                    "Current Activity scope owner differs from lifecycle owner.");
                completed.Add("current-activity-scope-authoritative");

                RuntimeContentOwner currentRouteOwner =
                    RuntimeContentOwner.Route(
                        currentRoute.PrimaryScenePath,
                        currentRoute.RouteName);
                AssertEqual(1,
                    CountRuntimeRoots(
                        runtimeContent,
                        runtimeContentType,
                        currentRouteOwner),
                    "Current Route does not own exactly one RuntimeContent root.");
                completed.Add("current-route-resolved-and-scope-authoritative");

                PlayerActorPreparationResult prepared =
                    Invoke<PlayerActorPreparationResult>(
                        preparationModule,
                        "TryPrepareSelectedActor",
                        currentContext,
                        slotId,
                        nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                        "prepare-current-activity-actor");
                AssertNotNull(prepared,
                    "Current Actor preparation returned no result.");
                AssertTrue(prepared.Succeeded &&
                    prepared.CurrentSummary.IsPrepared,
                    "Current Actor preparation failed. " +
                    prepared.ToDiagnosticString());
                PlayerActorPreparationSummary previousPreparation =
                    prepared.CurrentSummary;
                completed.Add("current-actor-prepared");

                UnityPlayerInputGateAdapter gateAdapter =
                    ConfigureGateAdapter(stableHost, stablePlayerInput);
                AssertNotNull(gateAdapter,
                    "Stable host Gate adapter could not be configured.");
                completed.Add("current-input-gate-configured");

                PlayerGameplayRuntimeOperationResult ensured =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                        "ensure-current-gameplay");
                AssertNotNull(ensured,
                    "Current gameplay chain operation returned no result.");
                AssertTrue(ensured.Succeeded &&
                    ensured.CurrentAdmission.GameplayReady,
                    "Current gameplay chain is not GameplayReady. " +
                    ensured.ToDiagnosticString());
                AssertEqual(currentOwner,
                    ensured.CurrentAdmission.Owner,
                    "Current GameplayReady admission is not owned by the active Activity.");
                completed.Add("current-gameplayready-authoritative");

                PlayerActorDeclaration previousDeclaration =
                    ResolveDeclaration(
                        stableHost,
                        previousPreparation.Materialization.ActorId);
                AssertNotNull(previousDeclaration,
                    "Current Actor declaration was not found.");
                AssertTrue(previousDeclaration.gameObject.activeInHierarchy,
                    "Current Actor is not active before the switch.");
                completed.Add("previous-actor-active-before-request");

                ActivityAsset targetActivity =
                    CreateGameplayReadyActivity(
                        joined.Slot.Profile);
                RuntimeContentOwner targetOwner =
                    RuntimeContentOwner.Activity(
                        targetActivity.ActivityName,
                        targetActivity.ActivityName);
                completed.Add("gameplayready-target-authored");

                RouteAsset targetRoute = CreateRouteStartupTarget(
                    currentRoute,
                    targetActivity);
                RuntimeContentOwner targetRouteOwner =
                    RuntimeContentOwner.Route(
                        targetRoute.PrimaryScenePath,
                        targetRoute.RouteName);
                AssertTrue(!ReferenceEquals(currentRoute, targetRoute) &&
                    !string.Equals(
                        currentRoute.PrimaryScenePath,
                        targetRoute.PrimaryScenePath,
                        StringComparison.Ordinal) &&
                    currentRouteOwner != targetRouteOwner,
                    "Target Route does not have a distinct canonical Route owner.");
                completed.Add("route-startup-target-authored");

                object requestResult = await InvokeTaskResultAsync(
                    runtimeHost,
                    "RequestRouteAsync",
                    targetRoute,
                    nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                    "route-startup-gameplayready-switch");
                AssertTrue(GetBooleanProperty(requestResult, "Succeeded"),
                    "Route request with GameplayReady Startup Activity failed. " +
                    GetStringProperty(requestResult, "Message"));
                completed.Add("route-startup-request-succeeded");

                RouteAsset activeRoute = ResolveCurrentRoute(runtimeHost);
                AssertSame(targetRoute, activeRoute,
                    "Destination Route did not become current.");
                completed.Add("target-route-became-current");

                ActivityAsset activeTarget = ResolveCurrentActivity(runtimeHost);
                AssertSame(targetActivity, activeTarget,
                    "Target Activity did not become current.");
                completed.Add("target-activity-became-current");

                PlayerGameplayRuntimeHostSnapshot switchedGameplay =
                    GetGameplaySnapshot(gameplayModule);
                ActivityPlayerLifecycleAdmissionSnapshot lifecycle =
                    switchedGameplay.LifecycleAdmission;
                AssertNotNull(lifecycle,
                    "P3K.7H lifecycle admission evidence is missing after switch.");
                AssertTrue(lifecycle.IsCompleted,
                    "P3K.7H lifecycle admission did not complete. " +
                    lifecycle.ToDiagnosticString());
                completed.Add("lifecycle-admission-completed");

                AssertEqual(
                    ActivityPlayerLifecycleAdmissionFlowKind
                        .RouteStartupActivitySwitch,
                    lifecycle.FlowKind,
                    "Lifecycle admission did not retain the Route Startup flow kind.");
                AssertTrue(lifecycle.IsRouteStartupFlow,
                    "Lifecycle admission does not identify a Route Startup flow.");
                AssertEqual(lifecycle.FlowKind,
                    lifecycle.Token.FlowKind,
                    "Lifecycle snapshot and transaction token flow identities differ.");
                AssertEqual(lifecycle.PreviousRouteName,
                    lifecycle.Token.PreviousRouteName,
                    "Lifecycle snapshot and transaction token previous Route identities differ.");
                AssertEqual(lifecycle.TargetRouteName,
                    lifecycle.Token.TargetRouteName,
                    "Lifecycle snapshot and transaction token target Route identities differ.");
                completed.Add("route-startup-flow-identified");

                AssertTrue(lifecycle.TransitionAuthorized,
                    "Transition was not explicitly authorized after ReadyToCommit.");
                AssertTrue(lifecycle.PreviousExitAcknowledged,
                    "Previous Activity lifecycle exit was not acknowledged.");
                AssertEqual(
                    ActivityPlayerPreviousExitDisposition
                        .SupersededAwaitingCommit,
                    lifecycle.PreviousExitDisposition,
                    "Previous Activity exit was not transferred before Route Startup commit.");
                AssertTrue(lifecycle.TargetEnterAdopted,
                    "Target Activity lifecycle did not adopt committed Player evidence.");
                completed.Add("transition-and-lifecycle-order-proven");

                AssertEqual(currentRoute.RouteName,
                    lifecycle.PreviousRouteName,
                    "Lifecycle transaction lost the previous Route identity.");
                AssertEqual(targetRoute.RouteName,
                    lifecycle.TargetRouteName,
                    "Lifecycle transaction lost the destination Route identity.");
                completed.Add("exact-route-identities-retained");

                AssertEqual(currentOwner, lifecycle.PreviousOwner,
                    "Lifecycle transaction lost the previous Activity owner.");
                AssertEqual(targetOwner, lifecycle.TargetOwner,
                    "Lifecycle transaction lost the target Activity owner.");
                AssertEqual(0,
                    CountRuntimeRoots(runtimeContent, runtimeContentType, currentOwner),
                    "Previous Activity RuntimeContent root remained after the committed switch.");
                completed.Add("exact-activity-owners-retained");

                AssertEqual(0,
                    CountRuntimeRoots(
                        runtimeContent,
                        runtimeContentType,
                        currentRouteOwner),
                    "Previous Route RuntimeContent root remained after Route switch.");
                completed.Add("previous-route-runtime-scope-clean");

                AssertEqual(1,
                    CountRuntimeRoots(
                        runtimeContent,
                        runtimeContentType,
                        targetRouteOwner),
                    "Destination Route does not own exactly one RuntimeContent root.");
                completed.Add("target-route-runtime-scope-authoritative");

                AssertTrue(!lifecycle.CommitCleanupPending,
                    "Nominal lifecycle handoff retained unexpected commit cleanup.");
                AssertNotNull(lifecycle.HandoffGroup,
                    "Lifecycle transaction has no P3K.7E group evidence.");
                AssertTrue(lifecycle.HandoffGroup.IsCommitted,
                    "P3K.7E group was not committed before lifecycle adoption.");
                completed.Add("group-commit-complete-before-adoption");

                AssertEqual(1, lifecycle.SlotCount,
                    "Lifecycle admission retained an unexpected Slot count.");
                AssertEqual(slotId, lifecycle.Slots[0].PlayerSlotId,
                    "Lifecycle admission retained the wrong Slot.");
                AssertTrue(lifecycle.Slots[0].Committed &&
                    lifecycle.Slots[0].Adopted,
                    "Lifecycle Slot was not committed and adopted.");
                completed.Add("exact-slot-handoff-adopted");

                PlayerActorPreparationRuntimeHostSnapshot switchedPreparation =
                    GetPreparationSnapshot(preparationModule);
                PlayerActorPreparationSummary targetPreparation =
                    FindPreparation(switchedPreparation.Preparation, slotId);
                AssertTrue(targetPreparation.IsPrepared,
                    "Target P3J Actor is not prepared.");
                AssertEqual(targetOwner,
                    targetPreparation.Materialization.Owner,
                    "Target P3J Actor owner differs from target Activity owner.");
                completed.Add("target-p3j-authoritative");

                PlayerGameplayAdmissionSummary targetAdmission =
                    FindAdmission(switchedGameplay.Admission, slotId);
                AssertTrue(targetAdmission.GameplayReady,
                    "Target P3K.5 admission is not GameplayReady.");
                AssertEqual(targetPreparation.Token,
                    targetAdmission.PreparationToken,
                    "Target P3J and P3K preparation identities differ.");
                AssertEqual(targetOwner, targetAdmission.Owner,
                    "Target P3K admission owner differs from target Activity owner.");
                completed.Add("target-p3k-authoritative");

                AssertEqual(lifecycle.Slots[0].TargetPreparationToken,
                    targetPreparation.Token,
                    "Lifecycle evidence lost the exact promoted P3J token.");
                AssertEqual(lifecycle.Slots[0].TargetAdmissionToken,
                    targetAdmission.Token,
                    "Lifecycle evidence lost the exact promoted P3K.5 token.");
                completed.Add("exact-promoted-tokens-retained");

                AssertTrue(previousDeclaration == null ||
                    !previousDeclaration.gameObject.activeInHierarchy,
                    "Previous Actor remained active after the committed handoff.");

                // UnityEngine.Object.Destroy finalizes physical destruction at the
                // frame boundary. P3K.7D proves the same contract explicitly.
                await Awaitable.NextFrameAsync();
                AssertTrue(previousDeclaration == null,
                    "Previous physical Actor survived the committed handoff frame boundary.");
                completed.Add("previous-actor-release-finalized-after-commit-frame");

                AssertEqual(0, switchedGameplay.CandidateCount,
                    "Candidate staging retained a candidate after lifecycle completion.");
                AssertTrue(!switchedGameplay.HasActiveHandoffGroup,
                    "P3K.7E group remained active after lifecycle completion.");
                completed.Add("handoff-authorities-settled");

                object lifecycleSnapshotResult = InvokeRaw(
                    preparationModule,
                    "TryGetActivityPlayerActorLifecycleSnapshot",
                    null);
                ActivityPlayerActorLifecycleSnapshot p3jLifecycle =
                    (ActivityPlayerActorLifecycleSnapshot)
                    ((object[])lifecycleSnapshotResult)[0];
                AssertEqual(PlayerParticipationRequirementLevel.GameplayReady,
                    p3jLifecycle.RequirementLevel,
                    "P3J.6 did not retain GameplayReady adoption evidence.");
                AssertEqual(1, p3jLifecycle.PreparedCount,
                    "P3J.6 adopted an unexpected prepared Slot count.");
                completed.Add("p3j6-adoption-evidence-retained");

                object clearResult = await InvokeTaskResultAsync(
                    runtimeHost,
                    "ClearActivityAsync",
                    nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                    "clear-adopted-gameplayready-activity");
                AssertTrue(GetBooleanProperty(clearResult, "Succeeded"),
                    "Clearing adopted Activity failed. " +
                    GetStringProperty(clearResult, "Message"));
                completed.Add("adopted-activity-clear-succeeded");

                await Awaitable.NextFrameAsync();
                PlayerGameplayRuntimeHostSnapshot finalGameplay =
                    GetGameplaySnapshot(gameplayModule);
                AssertEqual(0, finalGameplay.GameplayReadyCount,
                    "Gameplay admission remained after Activity exit.");
                AssertEqual(0, finalGameplay.OccupiedCount,
                    "Gameplay occupancy remained after Activity exit.");
                AssertEqual(0, finalGameplay.BoundInputCount,
                    "Gameplay input binding remained after Activity exit.");
                completed.Add("gameplay-chain-released-before-actor");

                PlayerActorPreparationRuntimeHostSnapshot finalPreparation =
                    GetPreparationSnapshot(preparationModule);
                PlayerActorPreparationSummary finalSlot =
                    FindPreparation(finalPreparation.Preparation, slotId);
                AssertTrue(!finalSlot.IsPrepared,
                    "P3J Actor remained prepared after Activity exit.");
                completed.Add("target-actor-released-after-gameplay");

                PlayerActorDeclaration targetDeclaration =
                    ResolveDeclaration(
                        stableHost,
                        targetPreparation.Materialization.ActorId);
                AssertTrue(targetDeclaration == null,
                    "Target physical Actor remained after Activity exit.");
                completed.Add("target-physical-actor-destroyed");

                AssertTrue(ResolveCurrentActivity(runtimeHost) == null,
                    "FrameworkRuntimeHost still reports an active Activity after clear.");
                completed.Add("activity-state-cleared");

                AssertTrue(stableHost != null && stablePlayerInput != null,
                    "Stable Session Player host or PlayerInput was destroyed by Activity exit.");
                AssertSame(stablePlayerInput, stableHost.PlayerInput,
                    "Stable Session PlayerInput changed across Activity lifecycle.");
                completed.Add("stable-session-player-survives-activity-exit");

                AssertEqual(0, finalGameplay.CandidateCount,
                    "Candidate state remained after final cleanup.");
                AssertTrue(!finalGameplay.HasActiveHandoffGroup,
                    "Handoff group remained after final cleanup.");
                completed.Add("candidate-and-group-clean");

                AssertEqual(0,
                    CountRuntimeRoots(runtimeContent, runtimeContentType, targetOwner),
                    "Target Activity RuntimeContent root remained after clear.");
                completed.Add("target-runtime-scope-clean");

                AssertSame(targetRoute,
                    ResolveCurrentRoute(runtimeHost),
                    "Clearing the Startup Activity changed the current Route.");
                AssertEqual(1,
                    CountRuntimeRoots(
                        runtimeContent,
                        runtimeContentType,
                        targetRouteOwner),
                    "Destination Route root was removed by Activity clear.");
                completed.Add("route-scope-survives-startup-activity-clear");

                PlayerParticipationOperationResult closed =
                    Invoke<PlayerParticipationOperationResult>(
                        preparationModule,
                        "TryCloseJoining",
                        nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                        "route-startup-lifecycle-admission-complete");
                AssertTrue(closed.Completed && !closed.Snapshot.JoiningOpen,
                    "Closing joining failed. " + closed.ToDiagnosticString());
                completed.Add("joining-closed");

                AssertPublicContractsContainNoUnityReferences(
                    typeof(ActivityPlayerLifecycleAdmissionToken),
                    typeof(ActivityPlayerLifecycleAdmissionSlotSnapshot),
                    typeof(ActivityPlayerLifecycleAdmissionSnapshot),
                    typeof(ActivityPlayerLifecycleAdmissionResult),
                    typeof(ActivityPlayerLifecycleAdmissionFlowKind),
                    typeof(ActivityPlayerPreviousExitDisposition));
                completed.Add("public-lifecycle-contracts-no-unity-references");

                AssertEqual(48, completed.Count,
                    "P3K.7H smoke case count changed unexpectedly.");
                Debug.Log(
                    "[P3K7H_ROUTE_STARTUP_ACTIVITY_PLAYER_ADMISSION_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"session='{finalGameplay.SessionContextId}' " +
                    $"slot='{slotId.StableText}' " +
                    $"route='{targetRoute.RouteName}' target='{targetActivity.ActivityName}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                Debug.LogError(
                    "[P3K7H_ROUTE_STARTUP_ACTIVITY_PLAYER_ADMISSION_SMOKE] " +
                    $"status='Failed' exception='{inner.GetType().Name}' " +
                    $"message='{Escape(inner.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw inner;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K7H_ROUTE_STARTUP_ACTIVITY_PLAYER_ADMISSION_SMOKE] " +
                    $"status='Failed' exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static async System.Threading.Tasks.Task<LocalPlayerProvisioningAuthoring>
            AwaitProvisioningAuthoringAsync()
        {
            const int MaxFrames = 300;
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                LocalPlayerProvisioningAuthoring[] candidates =
                    UnityEngine.Object.FindObjectsByType<
                        LocalPlayerProvisioningAuthoring>(
                        FindObjectsInactive.Include);
                LocalPlayerProvisioningAuthoring resolved = null;
                int loadedCount = 0;
                for (int index = 0; index < candidates.Length; index++)
                {
                    LocalPlayerProvisioningAuthoring candidate =
                        candidates[index];
                    if (candidate == null ||
                        !candidate.gameObject.scene.IsValid() ||
                        !candidate.gameObject.scene.isLoaded)
                    {
                        continue;
                    }

                    loadedCount++;
                    resolved = candidate;
                }

                if (loadedCount > 1)
                {
                    throw new InvalidOperationException(
                        $"Expected one loaded LocalPlayerProvisioningAuthoring, found '{loadedCount}'.");
                }

                if (loadedCount == 1 && resolved.RuntimeReady)
                {
                    return resolved;
                }

                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "LocalPlayerProvisioningAuthoring did not become RuntimeReady before the smoke timeout.");
        }

        private static RouteAsset ResolveCurrentRoute(
            object runtimeHost)
        {
            PropertyInfo stateProperty = runtimeHost.GetType().GetProperty(
                "State",
                InstanceAny);
            AssertNotNull(stateProperty,
                "FrameworkRuntimeHost.State was not found.");
            object state = stateProperty.GetValue(runtimeHost);
            AssertNotNull(state,
                "FrameworkRuntimeHost.State returned no value.");
            PropertyInfo currentRoute = state.GetType().GetProperty(
                "CurrentRoute",
                InstanceAny);
            AssertNotNull(currentRoute,
                "FrameworkRuntimeState.CurrentRoute was not found.");
            return currentRoute.GetValue(state) as RouteAsset;
        }

        private static ActivityAsset ResolveCurrentActivity(
            object runtimeHost)
        {
            PropertyInfo stateProperty = runtimeHost.GetType().GetProperty(
                "State",
                InstanceAny);
            AssertNotNull(stateProperty,
                "FrameworkRuntimeHost.State was not found.");
            object state = stateProperty.GetValue(runtimeHost);
            AssertNotNull(state,
                "FrameworkRuntimeHost.State returned no value.");
            PropertyInfo currentActivity = state.GetType().GetProperty(
                "CurrentActivity",
                InstanceAny);
            AssertNotNull(currentActivity,
                "FrameworkRuntimeState.CurrentActivity was not found.");
            return currentActivity.GetValue(state) as ActivityAsset;
        }

        private static async System.Threading.Tasks.Task<object>
            InvokeTaskResultAsync(
                object target,
                string methodName,
                params object[] arguments)
        {
            object taskObject = GetMethod(
                target.GetType(),
                methodName).Invoke(target, arguments);
            AssertNotNull(taskObject,
                $"Async method '{methodName}' returned no Task.");
            System.Threading.Tasks.Task task =
                taskObject as System.Threading.Tasks.Task;
            AssertNotNull(task,
                $"Async method '{methodName}' did not return a Task.");
            await task;
            PropertyInfo resultProperty = taskObject.GetType().GetProperty(
                "Result",
                InstanceAny);
            AssertNotNull(resultProperty,
                $"Async method '{methodName}' Task has no Result property.");
            return resultProperty.GetValue(taskObject);
        }

        private static bool GetBooleanProperty(
            object target,
            string propertyName)
        {
            AssertNotNull(target,
                $"Cannot read '{propertyName}' from a null result.");
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                InstanceAny);
            AssertNotNull(property,
                $"Property '{target.GetType().FullName}.{propertyName}' was not found.");
            return (bool)property.GetValue(target);
        }

        private static string GetStringProperty(
            object target,
            string propertyName)
        {
            AssertNotNull(target,
                $"Cannot read '{propertyName}' from a null result.");
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                InstanceAny);
            AssertNotNull(property,
                $"Property '{target.GetType().FullName}.{propertyName}' was not found.");
            return property.GetValue(target) as string ?? string.Empty;
        }

        private static object InvokeRaw(
            object target,
            string methodName,
            params object[] supplied)
        {
            MethodInfo method = GetMethod(target.GetType(), methodName);
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];
            for (int index = 0; index < arguments.Length; index++)
            {
                arguments[index] = supplied != null &&
                    index < supplied.Length
                        ? supplied[index]
                        : null;
            }
            bool result = (bool)method.Invoke(target, arguments);
            AssertTrue(result,
                $"Method '{methodName}' returned false.");
            return arguments;
        }

        private static int CountRuntimeRoots(
            object runtimeContent,
            Type runtimeContentType,
            RuntimeContentOwner owner)
        {
            MethodInfo snapshotRoots = null;
            MethodInfo[] methods = runtimeContentType.GetMethods(InstanceAny);
            for (int index = 0; index < methods.Length; index++)
            {
                if (methods[index].Name == "SnapshotRoots" &&
                    methods[index].GetParameters().Length == 0)
                {
                    snapshotRoots = methods[index];
                    break;
                }
            }
            AssertNotNull(snapshotRoots,
                "RuntimeContentRuntime.SnapshotRoots() was not found.");
            object rootsObject = snapshotRoots.Invoke(
                runtimeContent,
                Array.Empty<object>());
            Array roots = rootsObject as Array;
            AssertNotNull(roots,
                "RuntimeContentRuntime.SnapshotRoots returned no array.");
            int count = 0;
            for (int index = 0; index < roots.Length; index++)
            {
                object root = roots.GetValue(index);
                PropertyInfo ownerProperty = root.GetType().GetProperty(
                    "Owner",
                    InstanceAny);
                AssertNotNull(ownerProperty,
                    "RuntimeScopeRoot.Owner was not found.");
                if ((RuntimeContentOwner)ownerProperty.GetValue(root) == owner)
                {
                    count++;
                }
            }
            return count;
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun() => EditorApplication.isPlaying;

        private static LocalPlayerProvisioningAuthoring ResolveAuthoring()
        {
            LocalPlayerProvisioningAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<
                    LocalPlayerProvisioningAuthoring>(
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

        private static object ResolveCurrentRuntimeHost()
        {
            Type runtimeHostType = ResolveRuntimeType(RuntimeHostTypeName);
            MethodInfo tryGetCurrent =
                runtimeHostType.GetMethod("TryGetCurrent", StaticAny);
            AssertNotNull(tryGetCurrent,
                "FrameworkRuntimeHost.TryGetCurrent was not found.");
            object[] arguments = { null };
            bool resolved = (bool)tryGetCurrent.Invoke(null, arguments);
            AssertTrue(resolved && arguments[0] != null,
                "Current FrameworkRuntimeHost was not resolved.");
            return arguments[0];
        }

        private static object ResolveHostComponent(
            object runtimeHost,
            string typeName,
            string label)
        {
            Type type = ResolveRuntimeType(typeName);
            Component host = runtimeHost as Component;
            AssertNotNull(host,
                "FrameworkRuntimeHost is not a Unity Component.");
            Component component = host.GetComponent(type);
            AssertNotNull(component,
                $"FrameworkRuntimeHost has no {label}.");
            return component;
        }

        private static PlayerActorPreparationRuntimeHostSnapshot
            GetPreparationSnapshot(object module)
        {
            object[] arguments = { null };
            bool available = (bool)GetMethod(
                module.GetType(),
                "TryGetSnapshot").Invoke(module, arguments);
            var snapshot =
                arguments[0] as PlayerActorPreparationRuntimeHostSnapshot;
            AssertNotNull(snapshot,
                "P3J preparation runtime-host snapshot is missing.");
            AssertTrue(available || !snapshot.IsInitialized,
                "P3J snapshot availability and initialization disagree.");
            return snapshot;
        }

        private static PlayerGameplayRuntimeHostSnapshot
            GetGameplaySnapshot(object module)
        {
            object[] arguments = { null };
            bool available = (bool)GetMethod(
                module.GetType(),
                "TryGetSnapshot").Invoke(module, arguments);
            var snapshot =
                arguments[0] as PlayerGameplayRuntimeHostSnapshot;
            AssertNotNull(snapshot,
                "Player gameplay runtime-host snapshot is missing.");
            AssertTrue(available || !snapshot.IsInitialized,
                "Gameplay snapshot availability and initialization disagree.");
            return snapshot;
        }

        private static void ValidateEndpointSourceShape(
            object runtimeHost)
        {
            Type sourceType = ResolveRuntimeType(EndpointSourceTypeName);
            AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(sourceType),
                "Endpoint source must remain a plain runtime adapter.");

            FieldInfo[] fields = sourceType.GetFields(InstanceAny);
            for (int index = 0; index < fields.Length; index++)
            {
                AssertTrue(
                    fields[index].FieldType !=
                        typeof(UnityPlayerInputGateAdapter),
                    "Multi-Slot endpoint source retained one fixed Gate adapter.");
            }

            MethodInfo outputMethod =
                runtimeHost.GetType().GetMethod(
                    "TryGetPlayerGameplayCameraOutputSession",
                    InstanceAny);
            AssertNotNull(outputMethod,
                "FrameworkRuntimeHost Player gameplay camera output surface is missing.");
            object[] outputArguments = { null, null };
            bool outputAvailable =
                (bool)outputMethod.Invoke(runtimeHost, outputArguments);
            AssertTrue(outputAvailable &&
                outputArguments[0] is CameraOutputSessionBinding,
                "FrameworkRuntimeHost did not retain the Session camera output. " +
                (outputArguments[1] as string));
        }

        private static PlayerActorPreparationSummary FindPreparation(
            PlayerActorPreparationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            AssertNotNull(snapshot,
                "P3J preparation snapshot is missing.");
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId == playerSlotId)
                {
                    return snapshot.Slots[index];
                }
            }

            throw new InvalidOperationException(
                $"P3J preparation snapshot has no Slot '{playerSlotId.StableText}'.");
        }

        private static PlayerGameplayAdmissionSummary FindAdmission(
            PlayerGameplayAdmissionSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            AssertNotNull(snapshot,
                "P3K.5 admission snapshot is missing.");
            AssertTrue(snapshot.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary summary),
                $"P3K.5 admission snapshot has no Slot '{playerSlotId.StableText}'.");
            return summary;
        }

        private static PlayerActorDeclaration ResolveDeclaration(
            LocalPlayerHostAuthoring host,
            ActorId actorId)
        {
            if (host == null ||
                host.ActorMount == null ||
                !actorId.IsValid)
            {
                return null;
            }

            PlayerActorDeclaration[] declarations =
                host.ActorMount.GetComponentsInChildren<
                    PlayerActorDeclaration>(true);
            for (int index = 0; index < declarations.Length; index++)
            {
                if (declarations[index] != null &&
                    declarations[index].ActorId == actorId)
                {
                    return declarations[index];
                }
            }

            return null;
        }

        private static UnityPlayerInputGateAdapter ConfigureGateAdapter(
            LocalPlayerHostAuthoring host,
            PlayerInput playerInput)
        {
            AssertNotNull(host,
                "Stable Local Player Host is missing.");
            AssertNotNull(playerInput,
                "Stable PlayerInput is missing.");
            AssertNotNull(playerInput.actions,
                "Stable PlayerInput has no InputActionAsset.");

            string actionMapName =
                ResolveGameplayActionMapName(playerInput);
            AssertTrue(!string.IsNullOrEmpty(actionMapName),
                "Stable PlayerInput has no usable action map.");

            UnityPlayerInputGateAdapter adapter =
                host.GetComponent<UnityPlayerInputGateAdapter>();
            if (adapter == null)
            {
                adapter =
                    host.gameObject.AddComponent<
                        UnityPlayerInputGateAdapter>();
            }

            SerializedObject serialized = new SerializedObject(adapter);
            SerializedProperty playerInputProperty =
                serialized.FindProperty("playerInput");
            SerializedProperty sourceSlotProperty =
                serialized.FindProperty("sourceSlot");
            SerializedProperty actionMapProperty =
                serialized.FindProperty("gameplayActionMapName");
            AssertNotNull(playerInputProperty,
                "Gate adapter playerInput property was not found.");
            AssertNotNull(sourceSlotProperty,
                "Gate adapter sourceSlot property was not found.");
            AssertNotNull(actionMapProperty,
                "Gate adapter gameplayActionMapName property was not found.");
            playerInputProperty.objectReferenceValue = playerInput;
            sourceSlotProperty.objectReferenceValue =
                host.PlayerSlotDeclaration;
            actionMapProperty.stringValue = actionMapName;

            SerializedProperty logState =
                serialized.FindProperty("logStateChanges");
            SerializedProperty logRuntime =
                serialized.FindProperty("logMissingRuntimeOnce");
            SerializedProperty logTarget =
                serialized.FindProperty("logMissingTargetOnce");
            if (logState != null) logState.boolValue = false;
            if (logRuntime != null) logRuntime.boolValue = false;
            if (logTarget != null) logTarget.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return adapter;
        }

        private static string ResolveGameplayActionMapName(
            PlayerInput playerInput)
        {
            if (playerInput.currentActionMap != null)
            {
                return playerInput.currentActionMap.name;
            }

            if (!string.IsNullOrWhiteSpace(
                    playerInput.defaultActionMap) &&
                playerInput.actions.FindActionMap(
                    playerInput.defaultActionMap,
                    false) != null)
            {
                return playerInput.defaultActionMap;
            }

            return playerInput.actions.actionMaps.Count > 0
                ? playerInput.actions.actionMaps[0].name
                : string.Empty;
        }

        private static ActivityAsset CreateGameplayReadyActivity(
            PlayerSlotProfile slotProfile)
        {
            ActivityParticipationProjectionProfile projection =
                ScriptableObject.CreateInstance<
                    ActivityParticipationProjectionProfile>();
            projection.name = "P3K.7H Explicit Player";
            SerializedObject projectionSerialized =
                new SerializedObject(projection);
            projectionSerialized.FindProperty("displayName").stringValue =
                "P3K.7H Explicit Player";
            projectionSerialized.FindProperty("projectionMode").intValue =
                (int)ActivityParticipationProjectionMode.ExplicitSlots;
            projectionSerialized.FindProperty(
                    "zeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Rejected;
            SerializedProperty slots =
                projectionSerialized.FindProperty(
                    "explicitSlotProfiles");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue =
                slotProfile;
            projectionSerialized.ApplyModifiedPropertiesWithoutUndo();

            PlayerParticipationRequirementsProfile requirements =
                ScriptableObject.CreateInstance<
                    PlayerParticipationRequirementsProfile>();
            requirements.name = "P3K.7H Gameplay Ready";
            SerializedObject requirementsSerialized =
                new SerializedObject(requirements);
            requirementsSerialized.FindProperty("displayName").stringValue =
                "P3K.7H Gameplay Ready";
            requirementsSerialized.FindProperty(
                    "requirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.GameplayReady;
            requirementsSerialized.ApplyModifiedPropertiesWithoutUndo();

            ActivityAsset activity =
                ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = "P3K.7H Target Activity";
            SerializedObject activitySerialized =
                new SerializedObject(activity);
            activitySerialized.FindProperty("activityName").stringValue =
                "P3K.7H Target Activity";
            activitySerialized.FindProperty(
                    "playerParticipationProjectionProfile")
                .objectReferenceValue = projection;
            activitySerialized.FindProperty(
                    "playerParticipationRequirementsProfile")
                .objectReferenceValue = requirements;
            activitySerialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static RouteAsset CreateRouteStartupTarget(
            RouteAsset currentRoute,
            ActivityAsset startupActivity)
        {
            AssertNotNull(currentRoute,
                "Current Route is required to author the QA destination Route.");
            AssertNotNull(startupActivity,
                "Startup Activity is required to author the QA destination Route.");
            AssertTrue(currentRoute.HasPrimaryScene,
                "Current Route must expose a valid Primary Scene.");

            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = "P3K.7H Target Route";
            SerializedObject serialized = new SerializedObject(route);
            serialized.FindProperty("routeName").stringValue =
                "P3K.7H Target Route";
            serialized.FindProperty("primaryScenePath").stringValue =
                TargetRoutePrimaryScenePath;
            serialized.FindProperty("primarySceneName").stringValue =
                TargetRoutePrimarySceneName;
            serialized.FindProperty("startupActivity").objectReferenceValue =
                startupActivity;
            serialized.FindProperty("transitionGateMode").intValue =
                (int)currentRoute.TransitionGateMode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return route;
        }

        private static RuntimeScopeContext CreateActivityScopeContext(
            object runtimeHost,
            string ownerId,
            string displayName,
            out object runtimeContent,
            out Type runtimeContentType)
        {
            PropertyInfo runtimeContentProperty =
                runtimeHost.GetType().GetProperty(
                    "RuntimeContentRuntime",
                    InstanceAny);
            AssertNotNull(runtimeContentProperty,
                "FrameworkRuntimeHost.RuntimeContentRuntime was not found.");
            runtimeContent =
                runtimeContentProperty.GetValue(runtimeHost);
            AssertNotNull(runtimeContent,
                "FrameworkRuntimeHost has no RuntimeContentRuntime.");
            runtimeContentType = runtimeContent.GetType();

            RuntimeContentOwner owner =
                RuntimeContentOwner.Activity(ownerId, displayName);
            GetMethod(runtimeContentType, "CreateScopeRoot").Invoke(
                runtimeContent,
                new object[]
                {
                    owner,
                    nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                    "create-session-gameplay-scope"
                });

            object[] contextArguments =
            {
                owner,
                nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                "session-gameplay-runtime-composition",
                null
            };
            bool created = (bool)GetMethod(
                runtimeContentType,
                "TryCreateScopeContext").Invoke(
                    runtimeContent,
                    contextArguments);
            AssertTrue(created,
                $"RuntimeContentRuntime could not create Activity scope '{owner.StableText}'.");
            return (RuntimeScopeContext)contextArguments[3];
        }

        private static RuntimeContentHandle[] SnapshotHandles(
            object runtimeContent,
            Type runtimeContentType,
            RuntimeScopeContext context)
        {
            return GetMethod(
                    runtimeContentType,
                    "SnapshotHandles").Invoke(
                        runtimeContent,
                        new object[] { context })
                    as RuntimeContentHandle[] ??
                Array.Empty<RuntimeContentHandle>();
        }

        private static void RemoveScopeRoot(
            object runtimeContent,
            Type runtimeContentType,
            RuntimeContentOwner owner)
        {
            object result = GetMethod(
                    runtimeContentType,
                    "RemoveScopeRoot").Invoke(
                        runtimeContent,
                        new object[]
                        {
                            owner,
                            nameof(QaP3K7HRouteStartupActivityPlayerAdmissionSmoke),
                            "session-gameplay-runtime-cleanup"
                        });
            AssertNotNull(result,
                $"RuntimeContent scope removal returned no result for '{owner.StableText}'.");
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type =
                typeof(PlayerGameplayRuntimeHostSnapshot)
                    .Assembly.GetType(fullName, false);
            AssertNotNull(type,
                $"Runtime type '{fullName}' was not found.");
            return type;
        }

        private static MethodInfo GetMethod(
            Type type,
            string methodName,
            BindingFlags flags = default)
        {
            BindingFlags resolvedFlags =
                flags == default ? InstanceAny : flags;
            MethodInfo method =
                type.GetMethod(methodName, resolvedFlags);
            AssertNotNull(method,
                $"Method '{type.FullName}.{methodName}' was not found.");
            return method;
        }

        private static T Invoke<T>(
            object target,
            string methodName,
            params object[] arguments)
            where T : class
        {
            return GetMethod(
                    target.GetType(),
                    methodName).Invoke(
                        target,
                        arguments) as T;
        }

        private static void AssertPublicContractsContainNoUnityReferences(
            params Type[] contractTypes)
        {
            for (int typeIndex = 0;
                 typeIndex < contractTypes.Length;
                 typeIndex++)
            {
                Type type = contractTypes[typeIndex];
                PropertyInfo[] properties =
                    type.GetProperties(
                        BindingFlags.Instance |
                        BindingFlags.Public);
                for (int index = 0;
                     index < properties.Length;
                     index++)
                {
                    AssertTrue(
                        !typeof(UnityEngine.Object).IsAssignableFrom(
                            properties[index].PropertyType),
                        $"Public contract '{type.FullName}' property " +
                        $"'{properties[index].Name}' retains a Unity object reference.");
                }

                FieldInfo[] fields =
                    type.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public);
                for (int index = 0;
                     index < fields.Length;
                     index++)
                {
                    AssertTrue(
                        !typeof(UnityEngine.Object).IsAssignableFrom(
                            fields[index].FieldType),
                        $"Public contract '{type.FullName}' field " +
                        $"'{fields[index].Name}' retains a Unity object reference.");
                }
            }
        }

        private static void AssertTrue(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertNotNull(
            object value,
            string message)
        {
            if (value == null)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertSame(
            object expected,
            object actual,
            string message)
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(
                    expected,
                    actual))
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.Pause;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.UnityInput;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using PauseState = Immersive.Framework.Pause.PauseState;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// One-shot Play Mode proof for the official FrameworkRuntimeHost-scoped
    /// Route Startup Activity Player admission integration.
    /// </summary>
    internal static class QaPlayerGameplayAdmissionRegression
    {
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string PreparationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule";
        private const string GameplayModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayRuntimeHostModule";
        private const string EndpointSourceTypeName =
            "Immersive.Framework.PlayerParticipation.HostScopedPlayerGameplayChainEndpointSource";
        private const string PauseActionId =
            "a2222222-3333-4444-8555-666666666666";
        private const string AlternateActionMapId =
            "b1111111-2222-4333-8444-555555555555";
        private const string UiActionMapId =
            "a5555555-6666-4777-8888-999999999999";
        private const int ExpectedCompletedCaseCount = 114;

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static IReadOnlyList<string> lastCompleted =
            Array.Empty<string>();
        [MenuItem("Immersive Framework/QA/Player/Actor/Run Lifecycle Integration")]
        private static async void RunRegression()
        {
            try
            {
                IReadOnlyList<string> completed = await RunRegressionAsync();
                Debug.Log(
                    "[PLAYER_GAMEPLAY_ADMISSION_REGRESSION] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[PLAYER_GAMEPLAY_ADMISSION_REGRESSION] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", lastCompleted)}'.");
                throw;
            }
        }

        [MenuItem("Immersive Framework/QA/Player/Manager Provisioned/Advanced/Run Prefab Divergence")]
        private static void RunPrefabDivergenceRegression()
        {
            var completed = new List<string>();
            try
            {
                AssertTrue(EditorApplication.isPlaying,
                    "Local Player Prefab Divergence regression must run in Play Mode.");

                LocalPlayerProvisioningAuthoring[] candidates =
                    UnityEngine.Object.FindObjectsByType<LocalPlayerProvisioningAuthoring>(
                        FindObjectsInactive.Include);
                LocalPlayerProvisioningAuthoring authoring = null;
                for (int index = 0; index < candidates.Length; index++)
                {
                    LocalPlayerProvisioningAuthoring candidate = candidates[index];
                    if (candidate != null && candidate.gameObject.scene.isLoaded)
                    {
                        authoring = authoring == null
                            ? candidate
                            : throw new InvalidOperationException(
                                "Expected exactly one loaded Local Player provisioning authoring for the divergence fixture.");
                    }
                }

                AssertNotNull(authoring,
                    "Divergence fixture has no loaded Local Player provisioning authoring.");
                AssertNotNull(authoring.PlayerInputManager,
                    "Divergence fixture has no PlayerInputManager.");
                AssertNotNull(authoring.LocalPlayerHostPrefab,
                    "Divergence fixture has no authored Local Player Host Prefab.");
                AssertTrue(authoring.HasManagerPrefabDivergence,
                    "Divergence fixture does not contain distinct manager and authored Local Player Host prefabs.");
                completed.Add("divergent-prefab-fixture-confirmed");

                AssertTrue(!authoring.RuntimeReady,
                    "Framework runtime became ready despite the divergent Local Player Host Prefab.");
                AssertTrue(authoring.RuntimeDiagnostic.IndexOf(
                        "diverg", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Runtime diagnostic does not identify the Local Player Host Prefab divergence. " +
                    authoring.RuntimeDiagnostic);
                completed.Add("runtime-readiness-blocked-with-diagnostic");

                LocalPlayerJoinResult join = authoring.RequestJoin(
                    nameof(QaPlayerGameplayAdmissionRegression),
                    "divergent-prefab-must-not-join");
                AssertTrue(join != null && !join.Succeeded,
                    "RequestJoin succeeded despite divergent Local Player Host Prefab.");
                AssertEqual(LocalPlayerJoinStatus.RejectedRuntimeUnavailable, join.Status,
                    "Divergent prefab RequestJoin did not report runtime unavailability.");
                AssertTrue(join.ToDiagnosticString().IndexOf(
                        "diverg", StringComparison.OrdinalIgnoreCase) >= 0,
                    "Rejected RequestJoin did not retain the Local Player Host Prefab divergence diagnostic. " +
                    join.ToDiagnosticString());
                AssertEqual(0, authoring.PlayerInputManager.playerCount,
                    "Divergent prefab scenario created a physical Player Host.");
                LocalPlayerHostAuthoring[] hosts =
                    UnityEngine.Object.FindObjectsByType<LocalPlayerHostAuthoring>(
                        FindObjectsInactive.Include);
                int loadedRuntimeHosts = 0;
                for (int index = 0; index < hosts.Length; index++)
                {
                    LocalPlayerHostAuthoring host = hosts[index];
                    if (host != null && host.gameObject.scene.isLoaded)
                    {
                        loadedRuntimeHosts++;
                    }
                }

                AssertEqual(0, loadedRuntimeHosts,
                    "Divergent prefab scenario materialized a Local Player Host.");
                completed.Add("requestjoin-blocked-without-physical-host-residue");

                Debug.Log(
                    "[LOCAL_PLAYER_PREFAB_DIVERGENCE_REGRESSION] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[LOCAL_PLAYER_PREFAB_DIVERGENCE_REGRESSION] status='Failed' " +
                    $"exception='{exception.GetType().Name}' message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        internal static async Task<IReadOnlyList<string>> RunRegressionAsync()
        {
            var completed = new List<string>();
            lastCompleted = completed;

            object runtimeContent = null;
            Type runtimeContentType = null;
            RuntimeScopeContext currentContext = default;
            bool createdCurrentActivityScopeRoot = false;
            object runtimeHost = null;
            object preparationModule = null;
            object gameplayModule = null;
            LocalPlayerProvisioningAuthoring authoring = null;
            QaPlayerGameplayAdmissionFixture fixture = null;
            LocalPlayerJoinResult cleanupJoinResult = null;
            LocalPlayerHostAuthoring stableHost = null;
            PlayerInput stablePlayerInput = null;
            PlayerSlotId cleanupSlotId = default;
            UnityPlayerInputGateAdapter gateAdapter = null;
            InputActionMap desiredMap = null;
            bool originalPlayerInputEnabled = false;
            bool originalDesiredMapEnabled = false;
            float originalTimeScale = Time.timeScale;
            PauseRequestTrigger pauseTrigger = null;
            PausePlayerInputBinding temporaryPauseBinding = null;
            InputActionReference temporaryPauseReference = null;
            bool temporaryPauseBindingCreatedBySmoke = false;
            RouteAsset entryRoute = null;
            ActivityAsset targetActivity = null;
            RouteAsset targetRoute = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            bool originalJoiningOpen = false;
            var compensations =
                new RegressionCompensationStack();

            try
            {
                try
                {
                AssertTrue(EditorApplication.isPlaying,
                    "Player Gameplay Admission regression must run in Play Mode.");
                completed.Add("play-mode-required");

                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                authoring = fixture.ProvisioningAuthoring;
                AssertTrue(authoring.RuntimeReady,
                    "Local Player provisioning runtime is not ready. " +
                    authoring.RuntimeDiagnostic);
                originalJoiningOpen = authoring.RuntimeSnapshot.JoiningOpen;
                completed.Add("provisioning-runtime-ready");

                runtimeHost = fixture.RuntimeHost;
                completed.Add("runtime-host-resolved");

                preparationModule = fixture.PreparationModule;
                gameplayModule = fixture.GameplayModule;
                completed.Add("official-player-authorities-resolved");

                PlayerActorPreparationRuntimeHostSnapshot initialPreparation =
                    fixture.PreparationSnapshot;
                PlayerGameplayRuntimeHostSnapshot initialGameplay =
                    fixture.GameplaySnapshot;
                AssertTrue(initialPreparation.IsInitialized &&
                    initialGameplay.IsInitialized,
                    "P3J/P3K official runtime composition is not initialized.");
                AssertEqual(initialPreparation.SessionContextId,
                    initialGameplay.SessionContextId,
                    "P3J and P3K Session identities differ.");
                completed.Add("session-authorities-initialized");

                AssertTrue(initialGameplay.LifecycleAdmission != null,
                    "Player Gameplay Admission lifecycle snapshot is missing.");
                AssertEqual(ActivityPlayerLifecycleAdmissionState.None,
                    initialGameplay.LifecycleAdmission.State,
                    "Player Gameplay Admission lifecycle is not initially clean.");
                completed.Add("lifecycle-admission-initially-clean");

                PlayerInputManager manager = authoring.PlayerInputManager;
                AssertNotNull(manager,
                    "Provisioning authoring has no PlayerInputManager.");
                AssertNotNull(authoring.LocalPlayerHostPrefab,
                    "Canonical QA authoring has no Local Player Host Prefab.");
                AssertSame(authoring.LocalPlayerHostPrefab, manager.playerPrefab,
                    "Official Framework boot did not materialize the authored Local Player Host Prefab on the canonical empty manager.");
                completed.Add("authored-local-player-host-prefab-materialized");
                AssertEqual(0, manager.playerCount,
                    "Player Gameplay Admission regression requires a fresh Play Mode session with zero existing players. Exit Play Mode, run 'Immersive Framework/QA/Setup/Prepare Player Gameplay Admission Regression', enter Play Mode again, and run this regression before any Pause or Player preflight.");
                completed.Add("no-automatic-player-join-before-request");

                PlayerParticipationOperationResult opened = fixture.OpenJoining(
                    "route-startup-lifecycle-admission");
                AssertTrue(opened.Completed && opened.Snapshot.JoiningOpen,
                    "Opening joining failed. " + opened.ToDiagnosticString());
                compensations.Register(
                    "joining-state");
                completed.Add("joining-opened");

                LocalPlayerJoinResult joined = fixture.JoinPlayer(
                    "route-startup-lifecycle-admission");
                AssertNotNull(joined,
                    "Real local Player join returned no result.");
                cleanupJoinResult = joined;
                AssertTrue(joined.Succeeded,
                    "Real local Player join failed. " +
                    joined.ToDiagnosticString());
                AssertTrue(
                    joined.Slot.IsJoined &&
                    !joined.Slot.HasSelectedActor,
                    "Real local Player join must leave the Slot Joined and Unselected. " +
                    joined.ToDiagnosticString());
                compensations.Register(
                    "manager-join");
                AssertNotNull(
                    joined.Slot.Profile?.DefaultActorProfile,
                    "Joined Player Slot has no default ActorProfile intent.");
                AssertSame(authoring.LocalPlayerHostPrefab, manager.playerPrefab,
                    "Official RequestJoin changed the materialized Local Player Host Prefab.");
                AssertEqual(1, manager.playerCount,
                    "Official RequestJoin did not materialize exactly one PlayerInput.");
                AssertNotNull(joined.PlayerInput,
                    "Official RequestJoin returned no PlayerInput.");
                AssertNotNull(joined.LocalPlayerHost,
                    "Official RequestJoin returned no LocalPlayerHostAuthoring.");
                AssertSame(joined.PlayerInput, joined.LocalPlayerHost.PlayerInput,
                    "Joined Local Player Host does not expose the joined PlayerInput.");
                AssertSame(joined.LocalPlayerHost,
                    joined.PlayerInput.GetComponent<LocalPlayerHostAuthoring>(),
                    "Joined PlayerInput GameObject does not own the returned Local Player Host.");
                AssertNotNull(joined.LocalPlayerHost.ActorMount,
                    "Joined Local Player Host does not expose the authored Actor Mount.");
                stableHost = fixture.JoinedHost;
                stablePlayerInput = fixture.JoinedPlayerInput;
                AssertSame(joined.LocalPlayerHost, stableHost,
                    "Fixture did not preserve the joined Local Player Host.");
                AssertSame(joined.PlayerInput, stablePlayerInput,
                    "Fixture did not preserve the joined PlayerInput.");
                completed.Add("materialized-host-prefab-idempotent-after-request");
                completed.Add("official-requestjoin-created-materialized-host");
                completed.Add("real-local-player-joined");

                originalPlayerInputEnabled = stablePlayerInput != null &&
                    stablePlayerInput.enabled;
                PlayerSlotId slotId = fixture.JoinedSlotId;
                cleanupSlotId = slotId;
                completed.Add("stable-player-host-preserved");

                AssertNotNull(stableHost.transform.parent,
                    "Joined technical host has no explicit Session lifetime parent.");
                AssertEqual(
                    stableHost.transform.parent.gameObject.scene,
                    stableHost.gameObject.scene,
                    "Joined technical host and Session lifetime parent belong to different Scenes.");
                completed.Add("technical-host-parented");

                LocalPlayerActorSelectionRequestAuthoring selectionEndpoint =
                    fixture.SelectionEndpoint;
                AssertNotNull(selectionEndpoint,
                    "Canonical UIGlobal fixture has no Local Player Actor selection request endpoint.");
                AssertTrue(ReferenceEquals(
                        selectionEndpoint.ProvisioningAuthoring,
                        authoring),
                    "Local Player Actor selection endpoint does not reference the canonical provisioning authoring.");
                AssertTrue(
                    selectionEndpoint.TryValidateConfiguration(out string endpointIssue),
                    "Public default Actor selection endpoint is invalid. " + endpointIssue);
                AssertTrue(
                    selectionEndpoint.HasPlayerActorSelectionRuntimeBinding,
                    "Public default Actor selection endpoint has no official Session runtime binding. " +
                    selectionEndpoint.PlayerActorSelectionRuntimeBindingDiagnostic);
                completed.Add("actor-selection-endpoint-officially-bound");
                PlayerActorSelectionResult selected =
                    fixture.SelectDefaultActor(
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "select-current-actor");
                AssertNotNull(selected,
                    "Default Actor selection returned no result.");
                AssertTrue(selected.Succeeded &&
                    ReferenceEquals(
                        selected.SelectedActorProfile,
                        joined.Slot.Profile.DefaultActorProfile),
                    "Default Actor selection failed. " +
                    selected.ToDiagnosticString());
                completed.Add("public-default-actor-selection");

                RouteAsset currentRoute = fixture.CurrentRoute;
                entryRoute = currentRoute;
                AssertNotNull(currentRoute,
                    "FrameworkRuntimeHost has no current Route.");
                AssertTrue(currentRoute.HasPrimaryScene,
                    "Current Route has no Primary Scene for the Player Gameplay Admission regression.");

                ActivityAsset currentActivity = fixture.CurrentActivity;
                AssertNotNull(currentActivity,
                    "FrameworkRuntimeHost has no current Activity.");
                completed.Add("current-activity-resolved");

                currentContext = fixture.CreateCurrentActivityScope(
                    nameof(QaPlayerGameplayAdmissionRegression),
                    "create-session-gameplay-scope");
                runtimeContent = fixture.RuntimeContentRuntime;
                runtimeContentType = fixture.RuntimeContentRuntimeType;
                createdCurrentActivityScopeRoot = fixture.CreatedCurrentActivityScopeRoot;
                RuntimeContentOwner currentOwner =
                    RuntimeContentOwner.Activity(
                        currentActivity.ActivityId.StableText,
                        currentActivity.ActivityName,
                        RuntimeDefinitionToken.FromUnityObject(currentActivity));
                AssertEqual(currentOwner, currentContext.Owner,
                    "Current Activity scope owner differs from lifecycle owner.");
                if (createdCurrentActivityScopeRoot)
                {
                    compensations.Register(
                        "current-activity-runtime-root");
                }
                completed.Add("current-activity-scope-authoritative");

                RuntimeContentOwner currentRouteOwner =
                    RuntimeContentOwner.Route(
                        currentRoute.RouteId.StableText,
                        currentRoute.RouteName,
                        RuntimeDefinitionToken.FromUnityObject(currentRoute));
                AssertEqual(1,
                    CountRuntimeRoots(
                        runtimeContent,
                        runtimeContentType,
                        currentRouteOwner),
                    "Current Route does not own exactly one RuntimeContent root.");
                completed.Add("current-route-resolved-and-scope-authoritative");

                PlayerActorPreparationResult prepared =
                    fixture.PrepareSelectedActor(
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "prepare-current-activity-actor");
                AssertNotNull(prepared,
                    "Current Actor preparation returned no result.");
                AssertTrue(prepared.Succeeded &&
                    prepared.CurrentSummary.IsPrepared,
                    "Current Actor preparation failed. " +
                    prepared.ToDiagnosticString());
                PlayerActorPreparationSummary previousPreparation =
                    prepared.CurrentSummary;
                compensations.Register(
                    "actor-preparation");
                completed.Add("current-actor-prepared");

                gateAdapter =
                    ConfigureGateAdapter(stableHost, stablePlayerInput);
                AssertNotNull(gateAdapter,
                    "Stable host Gate adapter could not be configured.");
                completed.Add("current-input-gate-configured");

                PlayerGameplayRuntimeOperationResult ensured =
                    fixture.EnsureGameplayReady(
                        nameof(QaPlayerGameplayAdmissionRegression),
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
                compensations.Register(
                    "gameplay-chain");
                completed.Add("current-gameplayready-authoritative");
                AssertTrue(
                    gateAdapter.HasInputGateRuntimeBinding,
                    "Canonical Gate adapter was not bound to the FrameworkRuntimeHost Gate authority. " +
                    gateAdapter.InputGateRuntimeBindingDiagnostic);
                completed.Add("current-input-gate-runtime-bound");

                PlayerGameplayRuntimeHostSnapshot currentGameplay =
                    GetGameplaySnapshot(gameplayModule);
                AssertTrue(
                    currentGameplay.InputBinding.TryGetSummary(
                        slotId,
                        out PlayerGameplayInputBindingSummary initialInput) &&
                    initialInput.IsBound &&
                    initialInput.Token.IsValid,
                    "Manager-Provisioned gameplay has no canonical current Input binding.");
                AssertEqual(
                    previousPreparation.ActorEvidence.AssignmentToken,
                    initialInput.AssignmentToken,
                    "Input binding does not retain the current assignment token.");
                AssertEqual(
                    previousPreparation.ActorEvidence.HostBindingIdentity,
                    initialInput.HostBindingIdentity,
                    "Input binding does not retain the current Host binding identity.");
                AssertEqual(
                    previousPreparation.Token,
                    initialInput.PreparationToken,
                    "Input binding does not retain the current Actor preparation token.");
                AssertEqual(
                    previousPreparation.PreparedActorProfileId,
                    initialInput.ActorProfileId,
                    "Input binding does not retain the current Actor Profile.");
                AssertEqual(
                    previousPreparation.Materialization.ActorId,
                    initialInput.ActorId,
                    "Input binding does not retain the current Actor identity.");
                PlayerGameplayCameraEligibilitySummary initialCamera =
                    FindCameraEligibility(
                        currentGameplay.CameraEligibility,
                        slotId);
                PlayerGameplayAdmissionSummary initialAdmission =
                    FindAdmission(
                        currentGameplay.Admission,
                        slotId);
                AssertTrue(
                    initialCamera.HasCurrentDecision &&
                    initialCamera.Token.IsValid,
                    "Manager-Provisioned gameplay has no canonical Camera eligibility decision.");
                AssertTrue(
                    initialAdmission.GameplayReady &&
                    initialAdmission.InputBindingToken ==
                        initialInput.Token &&
                    initialAdmission.CameraEligibilityToken ==
                        initialCamera.Token,
                    "Gameplay Admission is not correlated to the canonical Input and Camera evidence.");
                completed.Add("manager-input-correlated-to-current-actor");
                completed.Add("manager-camera-is-independent-from-input-binding");

                object[] currentInputLookup = (object[])InvokeRaw(
                    gameplayModule,
                    "TryGetCurrentInputBinding",
                    slotId,
                    null,
                    null);
                PlayerGameplayInputBindingSummary lookedUpInput =
                    (PlayerGameplayInputBindingSummary)currentInputLookup[1];
                var lookupConfirmation =
                    (PlayerGameplayInputBindingResult)currentInputLookup[2];
                AssertEqual(initialInput.Token, lookedUpInput.Token,
                    "Current Input lookup changed the binding token.");
                AssertTrue(
                    lookupConfirmation.Succeeded &&
                    lookupConfirmation.CurrentSummary.IsBound,
                    "Current Input lookup did not confirm the canonical binding. " +
                    lookupConfirmation.ToDiagnosticString());
                completed.Add("lookup-current-input-non-destructive");
                completed.Add("manager-input-lookup-confirms-current");

                object inputAuthority =
                    GetField(gameplayModule, "inputContext");
                AssertTrue(
                    currentGameplay.Occupancy.TryGetSummary(
                        slotId,
                        out PlayerGameplayOccupancySummary currentOccupancy),
                    "Manager-Provisioned gameplay has no current occupancy prerequisite.");
                PlayerGameplayInputBindingResult rawInput =
                    Invoke<PlayerGameplayInputBindingResult>(
                        inputAuthority,
                        "TryBind",
                        default(PlayerActorPreparationSummary),
                        currentOccupancy,
                        stableHost,
                        null,
                        gateAdapter,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "raw-player-input-without-current-actor");
                AssertTrue(
                    rawInput.Rejected &&
                    rawInput.Status ==
                        PlayerGameplayInputBindingStatus.RejectedInvalidRequest,
                    "Raw PlayerInput was accepted without current Actor evidence.");
                completed.Add("raw-player-input-without-current-actor-rejected");

                PlayerActorDeclaration currentDeclarationForInputTests =
                    ResolveDeclaration(
                        stableHost,
                        previousPreparation.Materialization.ActorId);
                AssertNotNull(currentDeclarationForInputTests,
                    "Current Actor declaration was not found for Input negative cases.");
                GameObject foreignHostRoot =
                    new GameObject("QA PIC-1 Foreign Host");
                foreignHostRoot.SetActive(false);
                try
                {
                    LocalPlayerHostAuthoring foreignHost =
                        foreignHostRoot.AddComponent<
                            LocalPlayerHostAuthoring>();
                    PlayerGameplayInputBindingResult hostMismatch =
                        Invoke<PlayerGameplayInputBindingResult>(
                            inputAuthority,
                            "TryBind",
                            previousPreparation,
                            currentOccupancy,
                            foreignHost,
                            currentDeclarationForInputTests,
                            gateAdapter,
                            nameof(QaPlayerGameplayAdmissionRegression),
                            "reject-host-mismatch");
                    AssertTrue(
                        hostMismatch.Rejected &&
                        hostMismatch.Status ==
                            PlayerGameplayInputBindingStatus
                                .RejectedPhysicalBindingDivergence,
                        "Input binding accepted a Host different from current CPSA-3 physical evidence.");
                }
                finally
                {
                    UnityEngine.Object.Destroy(foreignHostRoot);
                }
                completed.Add("host-mismatch-rejected");

                PlayerGameplayInputBindingResult foreignInput =
                    Invoke<PlayerGameplayInputBindingResult>(
                        gameplayModule,
                        "ConfirmCurrentInputBinding",
                        slotId,
                        default(PlayerGameplayInputBindingToken),
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "reject-invalid-input-token");
                AssertTrue(
                    foreignInput.Rejected &&
                    foreignInput.Status ==
                        PlayerGameplayInputBindingStatus
                            .RejectedForeignOrStaleBinding,
                    "Current Input confirmation accepted an invalid token.");
                completed.Add("invalid-input-token-rejected");

                PlayerGameplayInputBindingResult otherSlotInput =
                    Invoke<PlayerGameplayInputBindingResult>(
                        gameplayModule,
                        "ConfirmCurrentInputBinding",
                        PlayerSlotId.From("qa.pic1.other-slot"),
                        initialInput.Token,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "reject-other-slot-input-token");
                AssertTrue(otherSlotInput.Rejected,
                    "Current Input confirmation accepted a token for another Slot.");
                completed.Add("other-slot-input-token-rejected");

                desiredMap =
                    stablePlayerInput.actions.FindActionMap(
                        initialInput.DesiredActionMapName,
                        true);
                originalDesiredMapEnabled = desiredMap.enabled;

                PlayerGameplayRuntimeOperationResult idempotentEnsure =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        "QaPic1DifferentDiagnosticSource",
                        "same-domain-evidence-different-diagnostics");
                AssertTrue(idempotentEnsure.Succeeded,
                    "Idempotent gameplay ensure failed. " +
                    idempotentEnsure.ToDiagnosticString());
                PlayerGameplayInputBindingSummary idempotentInput =
                    FindInputBinding(
                        GetGameplaySnapshot(gameplayModule).InputBinding,
                        slotId);
                AssertEqual(initialInput.Token, idempotentInput.Token,
                    "Different operation diagnostics renewed the Input binding token.");
                AssertEqual(initialInput.BindingRevision,
                    idempotentInput.BindingRevision,
                    "Different operation diagnostics renewed the Input binding revision.");
                PlayerGameplayCameraEligibilitySummary idempotentCamera =
                    FindCameraEligibility(
                        GetGameplaySnapshot(gameplayModule).CameraEligibility,
                        slotId);
                AssertEqual(initialCamera.Token, idempotentCamera.Token,
                    "Idempotent ensure renewed Camera eligibility.");
                completed.Add("exact-binding-idempotent");
                completed.Add("different-diagnostics-preserve-binding-token");
                completed.Add("idempotent-begin-preserves-binding-revision");
                completed.Add("idempotent-begin-preserves-camera-token");

                InputActionMap alternateMap =
                    stablePlayerInput.actions.FindActionMap(
                        AlternateActionMapId,
                        true);
                InputActionMap uiMap =
                    stablePlayerInput.actions.FindActionMap(
                        UiActionMapId,
                        true);
                pauseTrigger =
                    ResolveBoundPauseRequestTrigger();
                AssertActionMapReconfigurationPrerequisites(
                    stablePlayerInput,
                    gateAdapter,
                    desiredMap,
                    alternateMap,
                    uiMap,
                    pauseTrigger);
                PreflightActionMapActivation(
                    stablePlayerInput,
                    desiredMap,
                    alternateMap);
                PlayerGameplayInputBindingSummary afterPreflightInput =
                    FindInputBinding(
                        GetGameplaySnapshot(gameplayModule).InputBinding,
                        slotId);
                AssertEqual(
                    initialInput.Token,
                    afterPreflightInput.Token,
                    "Action Map preflight changed the canonical Input binding token.");

                ConfigureGateActionMap(gateAdapter, alternateMap);
                PlayerGameplayRuntimeOperationResult alternateEnsure =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "explicit-alternate-action-map");
                AssertTrue(
                    alternateEnsure != null &&
                    alternateEnsure.Succeeded,
                    "Explicit Action Map reconfiguration failed. " +
                    alternateEnsure?.ToDiagnosticString());
                PlayerGameplayRuntimeHostSnapshot alternateSnapshot =
                    GetGameplaySnapshot(gameplayModule);
                PlayerGameplayInputBindingSummary alternateInput =
                    FindInputBinding(
                        alternateSnapshot.InputBinding,
                        slotId);
                PlayerGameplayCameraEligibilitySummary alternateCamera =
                    FindCameraEligibility(
                        alternateSnapshot.CameraEligibility,
                        slotId);
                AssertTrue(
                    alternateInput.Token != initialInput.Token &&
                    alternateInput.BindingRevision >
                        initialInput.BindingRevision,
                    "Explicit Action Map reconfiguration did not renew structural Input identity.");
                AssertEqual(alternateMap.name,
                    alternateInput.DesiredActionMapName,
                    "Explicit Action Map reconfiguration retained the old desired map.");
                AssertTrue(
                    ReferenceEquals(
                        stablePlayerInput.currentActionMap,
                        alternateMap) &&
                    alternateMap.enabled &&
                    !ReferenceEquals(
                        stablePlayerInput.currentActionMap,
                        desiredMap),
                    "Explicit Action Map reconfiguration did not make QA Gameplay Alternate current and enabled.");
                AssertEqual(1,
                    alternateSnapshot.BoundInputCount,
                    "Explicit Action Map reconfiguration created duplicate current bindings.");
                AssertEqual(
                    initialCamera.Token,
                    alternateCamera.Token,
                    "Action Map reconfiguration renewed the independent Camera capability.");
                PlayerGameplayAdmissionSummary alternateAdmission =
                    FindAdmission(
                        alternateSnapshot.Admission,
                        slotId);
                AssertTrue(
                    alternateAdmission.GameplayReady &&
                    alternateAdmission.InputBindingToken ==
                        alternateInput.Token &&
                    alternateAdmission.CameraEligibilityToken ==
                        alternateCamera.Token,
                    "Gameplay Admission did not follow the structurally reconfigured Input binding.");
                AssertUpstreamEvidenceUnchanged(
                    preparationModule,
                    slotId,
                    previousPreparation,
                    initialInput,
                    "Explicit Action Map reconfiguration");
                PlayerGameplayInputBindingResult replacedMapToken =
                    Invoke<PlayerGameplayInputBindingResult>(
                        gameplayModule,
                        "ConfirmCurrentInputBinding",
                        slotId,
                        initialInput.Token,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "confirm-reconfigured-action-map-old-token");
                AssertTrue(
                    replacedMapToken.Rejected,
                    "Explicit Action Map reconfiguration kept the old Input token current.");
                compensations.Register(
                    "action-map-reconfigured");
                completed.Add("explicit-action-map-change-renews-binding");
                completed.Add("explicit-action-map-change-keeps-single-current-binding");
                completed.Add("explicit-action-map-change-preserves-camera-capability");
                completed.Add("stale-input-token-rejected");

                ConfigureGateActionMap(gateAdapter, desiredMap);
                PlayerGameplayRuntimeOperationResult restoreMapEnsure =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "restore-original-action-map");
                AssertTrue(
                    restoreMapEnsure != null &&
                    restoreMapEnsure.Succeeded,
                    "Original Action Map reconfiguration failed. " +
                    restoreMapEnsure?.ToDiagnosticString());
                PlayerGameplayRuntimeHostSnapshot restoredMapSnapshot =
                    GetGameplaySnapshot(gameplayModule);
                PlayerGameplayInputBindingSummary restoredMapInput =
                    FindInputBinding(
                        restoredMapSnapshot.InputBinding,
                        slotId);
                PlayerGameplayCameraEligibilitySummary restoredMapCamera =
                    FindCameraEligibility(
                        restoredMapSnapshot.CameraEligibility,
                        slotId);
                AssertTrue(
                    restoredMapInput.Token != alternateInput.Token &&
                    restoredMapInput.BindingRevision >
                        alternateInput.BindingRevision,
                    "Restoring the original desired Action Map did not renew structural Input identity.");
                AssertEqual(desiredMap.name,
                    restoredMapInput.DesiredActionMapName,
                    "Original desired Action Map was not restored.");
                AssertTrue(
                    ReferenceEquals(
                        stablePlayerInput.currentActionMap,
                        desiredMap) &&
                    desiredMap.enabled,
                    "Original Gameplay Action Map did not become current and enabled.");
                AssertEqual(1,
                    restoredMapSnapshot.BoundInputCount,
                    "Original Action Map restoration created duplicate current bindings.");
                AssertUpstreamEvidenceUnchanged(
                    preparationModule,
                    slotId,
                    previousPreparation,
                    initialInput,
                    "Original Action Map restoration");
                PlayerGameplayAdmissionSummary restoredMapAdmission =
                    FindAdmission(
                        restoredMapSnapshot.Admission,
                        slotId);
                AssertTrue(
                    restoredMapAdmission.GameplayReady &&
                    restoredMapAdmission.InputBindingToken ==
                        restoredMapInput.Token &&
                    restoredMapAdmission.CameraEligibilityToken ==
                        restoredMapCamera.Token,
                    "Gameplay Admission did not follow the restored Input binding.");
                initialInput = restoredMapInput;
                initialCamera = restoredMapCamera;
                initialAdmission = restoredMapAdmission;
                completed.Add("explicit-action-map-restore-renews-binding");

                ConfigureGateActionMap(gateAdapter, uiMap);
                PlayerGameplayRuntimeOperationResult uiMapEnsure =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "reject-unactivatable-ui-action-map");
                AssertTrue(
                    uiMapEnsure != null &&
                    !uiMapEnsure.Succeeded &&
                    uiMapEnsure.RollbackAttempted &&
                    uiMapEnsure.RollbackSucceeded &&
                    uiMapEnsure.Message.Contains(
                        "FailedActionMapActivation",
                        StringComparison.Ordinal),
                    "UI Action Map failure did not report a successful physical rollback. " +
                    uiMapEnsure?.ToDiagnosticString());
                PlayerGameplayRuntimeHostSnapshot afterUiFailure =
                    GetGameplaySnapshot(gameplayModule);
                PlayerGameplayInputBindingSummary retainedAfterUiFailure =
                    FindInputBinding(
                        afterUiFailure.InputBinding,
                        slotId);
                PlayerGameplayCameraEligibilitySummary cameraAfterUiFailure =
                    FindCameraEligibility(
                        afterUiFailure.CameraEligibility,
                        slotId);
                PlayerGameplayAdmissionSummary admissionAfterUiFailure =
                    FindAdmission(
                        afterUiFailure.Admission,
                        slotId);
                AssertTrue(
                    retainedAfterUiFailure.IsBound &&
                    retainedAfterUiFailure.Token ==
                        initialInput.Token &&
                    retainedAfterUiFailure.BindingRevision ==
                        initialInput.BindingRevision &&
                    ReferenceEquals(
                        stablePlayerInput.currentActionMap,
                        desiredMap) &&
                    desiredMap.enabled &&
                    afterUiFailure.BoundInputCount == 1,
                    "UI Action Map rollback did not preserve the current Gameplay binding.");
                AssertEqual(
                    initialCamera.Token,
                    cameraAfterUiFailure.Token,
                    "UI Action Map rollback changed Camera eligibility.");
                AssertEqual(
                    initialAdmission.Token,
                    admissionAfterUiFailure.Token,
                    "UI Action Map rollback changed Gameplay Admission.");
                ConfigureGateActionMap(gateAdapter, desiredMap);
                completed.Add("unactivatable-action-map-rejected");
                completed.Add("unactivatable-action-map-preserves-current-binding");
                completed.Add("unactivatable-action-map-preserves-camera-eligibility");

                ConfigureInvalidGateActionMap(
                    gateAdapter,
                    stablePlayerInput.actions);
                PlayerGameplayRuntimeOperationResult invalidMapEnsure =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "reject-invalid-action-map");
                AssertTrue(
                    invalidMapEnsure != null &&
                    !invalidMapEnsure.Succeeded &&
                    !invalidMapEnsure.RollbackAttempted,
                    "Missing desired Action Map was unexpectedly accepted or initiated a physical rollback. " +
                    invalidMapEnsure?.ToDiagnosticString());
                PlayerGameplayRuntimeHostSnapshot afterMissingMap =
                    GetGameplaySnapshot(gameplayModule);
                PlayerGameplayInputBindingSummary retainedAfterInvalidMap =
                    FindInputBinding(
                        afterMissingMap.InputBinding,
                        slotId);
                AssertEqual(initialInput.Token,
                    retainedAfterInvalidMap.Token,
                    "Failed Action Map reconfiguration displaced the current Input binding.");
                AssertEqual(initialInput.BindingRevision,
                    retainedAfterInvalidMap.BindingRevision,
                    "Failed Action Map reconfiguration changed the BindingRevision.");
                AssertEqual(
                    initialCamera.Token,
                    FindCameraEligibility(
                        afterMissingMap.CameraEligibility,
                        slotId).Token,
                    "Missing Action Map rejection changed Camera eligibility.");
                AssertEqual(
                    initialAdmission.Token,
                    FindAdmission(
                        afterMissingMap.Admission,
                        slotId).Token,
                    "Missing Action Map rejection changed Gameplay Admission.");
                AssertTrue(
                    ReferenceEquals(
                        stablePlayerInput.currentActionMap,
                        desiredMap) &&
                    desiredMap.enabled,
                    "Missing Action Map rejection changed the physical current map.");
                ConfigureGateActionMap(gateAdapter, desiredMap);
                completed.Add("invalid-action-map-rejected");
                completed.Add("invalid-action-map-preserves-current-binding");

                try
                {
                    stablePlayerInput.enabled = false;
                    PlayerGameplayInputBindingResult disabledInput =
                        RefreshInputAvailability(
                            gameplayModule,
                            slotId,
                            initialInput.Token,
                            "temporarily-disable-player-input");
                    AssertAvailabilityUnchangedBinding(
                        disabledInput,
                        initialInput,
                        PlayerGameplayInputAvailability.PlayerInputDisabled,
                        "PlayerInput disable");
                    AssertCameraToken(
                        gameplayModule,
                        slotId,
                        initialCamera.Token,
                        "PlayerInput disable");
                }
                finally
                {
                    stablePlayerInput.enabled =
                        originalPlayerInputEnabled;
                }

                PlayerGameplayInputBindingResult reenabledInput =
                    RefreshInputAvailability(
                        gameplayModule,
                        slotId,
                        initialInput.Token,
                        "restore-player-input-enabled");
                AssertAvailabilityUnchangedBinding(
                    reenabledInput,
                    initialInput,
                    PlayerGameplayInputAvailability.Allowed,
                    "PlayerInput restore");
                AssertCameraToken(
                    gameplayModule,
                    slotId,
                    initialCamera.Token,
                    "PlayerInput restore");
                completed.Add("player-input-disable-preserves-binding");
                completed.Add("player-input-restore-restores-allowed");
                completed.Add("player-input-toggle-preserves-camera-token");

                try
                {
                    desiredMap.Disable();
                    PlayerGameplayInputBindingResult actionsUnavailable =
                        RefreshInputAvailability(
                            gameplayModule,
                            slotId,
                            initialInput.Token,
                            "temporarily-disable-desired-action-map");
                    AssertAvailabilityUnchangedBinding(
                        actionsUnavailable,
                        initialInput,
                        PlayerGameplayInputAvailability.ActionsUnavailable,
                        "Desired Action Map disable");
                    AssertCameraToken(
                        gameplayModule,
                        slotId,
                        initialCamera.Token,
                        "Desired Action Map disable");
                }
                finally
                {
                    if (originalDesiredMapEnabled)
                    {
                        desiredMap.Enable();
                    }
                    else
                    {
                        desiredMap.Disable();
                    }
                }

                PlayerGameplayInputBindingResult restoredActions =
                    RefreshInputAvailability(
                        gameplayModule,
                        slotId,
                        initialInput.Token,
                        "restore-desired-action-map");
                AssertAvailabilityUnchangedBinding(
                    restoredActions,
                    initialInput,
                    PlayerGameplayInputAvailability.Allowed,
                    "Desired Action Map restore");
                AssertCameraToken(
                    gameplayModule,
                    slotId,
                    initialCamera.Token,
                    "Desired Action Map restore");
                completed.Add("action-map-temporary-disable-preserves-binding");
                completed.Add("action-map-restore-restores-allowed");
                completed.Add("action-map-toggle-preserves-camera-token");

                pauseTrigger =
                    ResolveBoundPauseRequestTrigger();
                object pauseProduct =
                    GetField(runtimeHost, "_pauseProductBindingRuntime");
                AssertTrue(
                    !GetBooleanProperty(
                        pauseProduct,
                        "HasActivePlayerInputBinding"),
                    "Pause ApplicationOnly prerequisite is invalid: the Pause Product already has an active PlayerInput binding.");

                pauseTrigger.RequestPause();
                AssertPauseProductResult(
                    pauseTrigger,
                    "AppliedWithoutPlayerInput",
                    "ApplicationOnly",
                    PauseState.Paused,
                    "Pause without PlayerInput binding");
                AssertTrue(
                    Mathf.Approximately(Time.timeScale, 0f),
                    "Pause ApplicationOnly did not set Time.timeScale to zero.");

                PlayerGameplayInputBindingResult applicationOnlyInput =
                    RefreshInputAvailability(
                        gameplayModule,
                        slotId,
                        initialInput.Token,
                        "refresh-application-only-pause-gate");
                AssertAvailabilityUnchangedBinding(
                    applicationOnlyInput,
                    initialInput,
                    PlayerGameplayInputAvailability.BlockedByGate,
                    "Pause ApplicationOnly canonical Gate projection");
                AssertTrue(gateAdapter.IsBlockedByAdapter,
                    "Pause ApplicationOnly did not project the canonical Pause Gate to the bound adapter.");
                AssertCameraToken(
                    gameplayModule,
                    slotId,
                    initialCamera.Token,
                    "Pause ApplicationOnly");
                completed.Add("pause-without-player-binding-applies-application-only");
                completed.Add("pause-without-player-binding-preserves-input-binding");
                completed.Add("pause-application-only-projects-canonical-gate");

                pauseTrigger.RequestResume();
                AssertPauseProductResult(
                    pauseTrigger,
                    "AppliedWithoutPlayerInput",
                    "ApplicationOnly",
                    PauseState.Running,
                    "Resume without PlayerInput binding");
                PlayerGameplayInputBindingResult applicationOnlyResumed =
                    RefreshInputAvailability(
                        gameplayModule,
                        slotId,
                        initialInput.Token,
                        "refresh-after-application-only-resume");
                AssertAvailabilityUnchangedBinding(
                    applicationOnlyResumed,
                    initialInput,
                    PlayerGameplayInputAvailability.Allowed,
                    "ApplicationOnly resume");
                AssertTrue(!gateAdapter.IsBlockedByAdapter,
                    "ApplicationOnly Resume did not release the canonical Pause Gate.");
                completed.Add("pause-application-only-resume-restores-allowed");

                temporaryPauseBinding =
                    stableHost.GetComponent<
                        PausePlayerInputBinding>();
                if (temporaryPauseBinding == null)
                {
                    temporaryPauseBinding =
                        stableHost.gameObject.AddComponent<
                            PausePlayerInputBinding>();
                    temporaryPauseBindingCreatedBySmoke = true;
                }
                temporaryPauseReference =
                    ConfigurePausePlayerInputBinding(
                        temporaryPauseBinding,
                        stablePlayerInput,
                        desiredMap);
                AssertTrue(
                    temporaryPauseBinding.TryValidateAuthoring(
                        out string pauseBindingIssue),
                    "Canonical Pause PlayerInput fixture is invalid. " +
                    pauseBindingIssue);
                object[] pauseBindArguments =
                    (object[])InvokeRaw(
                        temporaryPauseBinding,
                        "TryInjectBindingPort",
                        pauseProduct,
                        null);
                AssertTrue(
                    temporaryPauseBinding.HasActiveBinding,
                    "Pause fixture did not register the canonical PlayerInput binding. " +
                    (pauseBindArguments[1] as string ?? string.Empty));
                completed.Add("pause-player-binding-active");
                compensations.Register(
                    "pause-player-binding");

                pauseTrigger.RequestPause();
                AssertPauseProductResult(
                    pauseTrigger,
                    "Applied",
                    "PlayerInputTransaction",
                    PauseState.Paused,
                    "Pause with PlayerInput binding");
                completed.Add("pause-player-input-transaction-applied");

                PlayerGameplayInputBindingResult pausedInput =
                    RefreshInputAvailability(
                        gameplayModule,
                        slotId,
                        initialInput.Token,
                        "refresh-input-while-paused");
                AssertAvailabilityUnchangedBinding(
                    pausedInput,
                    initialInput,
                    PlayerGameplayInputAvailability.BlockedByGate,
                    "Pause PlayerInput transaction");
                AssertTrue(gateAdapter.IsBlockedByAdapter,
                    "Pause did not block the canonical Input Gate.");
                AssertUpstreamEvidenceUnchanged(
                    preparationModule,
                    slotId,
                    previousPreparation,
                    initialInput,
                    "Pause PlayerInput transaction");
                AssertCameraToken(
                    gameplayModule,
                    slotId,
                    initialCamera.Token,
                    "Pause PlayerInput transaction");
                completed.Add("pause-block-preserves-binding");
                completed.Add("pause-block-preserves-binding-token");
                completed.Add("pause-block-preserves-binding-revision");
                completed.Add("pause-block-preserves-upstream-evidence");
                completed.Add("pause-block-reports-blocked-availability");
                completed.Add("pause-block-preserves-camera-token");

                pauseTrigger.RequestResume();
                AssertPauseProductResult(
                    pauseTrigger,
                    "Applied",
                    "PlayerInputTransaction",
                    PauseState.Running,
                    "Resume with PlayerInput binding");
                PlayerGameplayInputBindingResult resumedInput =
                    RefreshInputAvailability(
                        gameplayModule,
                        slotId,
                        initialInput.Token,
                        "refresh-input-after-resume");
                AssertAvailabilityUnchangedBinding(
                    resumedInput,
                    initialInput,
                    PlayerGameplayInputAvailability.Allowed,
                    "Pause PlayerInput Resume");
                AssertTrue(!gateAdapter.IsBlockedByAdapter,
                    "Resume did not release the canonical Input Gate.");
                AssertCameraToken(
                    gameplayModule,
                    slotId,
                    initialCamera.Token,
                    "Pause PlayerInput Resume");
                completed.Add("resume-restores-allowed");
                completed.Add("resume-preserves-binding-token");
                completed.Add("resume-preserves-binding-revision");
                completed.Add("resume-preserves-camera-token");

                InvokeRaw(
                    temporaryPauseBinding,
                    "ReleaseForSceneLifecycle",
                    "qa-pic1-pause-case-complete",
                    null);
                AssertTrue(
                    !temporaryPauseBinding.HasActiveBinding,
                    "Pause PlayerInput binding remained active after explicit case cleanup.");
                completed.Add("pause-player-binding-released");
                if (temporaryPauseBindingCreatedBySmoke)
                {
                    UnityEngine.Object.Destroy(
                        temporaryPauseBinding);
                }
                if (temporaryPauseReference != null)
                {
                    UnityEngine.Object.Destroy(
                        temporaryPauseReference);
                }
                await Awaitable.NextFrameAsync();
                temporaryPauseBinding = null;
                temporaryPauseReference = null;
                temporaryPauseBindingCreatedBySmoke = false;

                PlayerGameplayOccupancySummary occupancyBeforeInputRelease =
                    FindOccupancy(
                        GetGameplaySnapshot(gameplayModule).Occupancy,
                        slotId);
                PlayerGameplayInputBindingResult releasedInput =
                    Invoke<PlayerGameplayInputBindingResult>(
                        gameplayModule,
                        "ReleaseInputBinding",
                        slotId,
                        initialInput.Token,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "release-current-input-only");
                AssertTrue(
                    releasedInput != null &&
                    releasedInput.Succeeded &&
                    releasedInput.CurrentSummary.IsUnbound,
                    "Exact current Input release failed. " +
                    releasedInput?.ToDiagnosticString());
                PlayerGameplayRuntimeHostSnapshot inputReleasedSnapshot =
                    GetGameplaySnapshot(gameplayModule);
                AssertEqual(0,
                    inputReleasedSnapshot.BoundInputCount,
                    "Exact current Input release retained a bound Input record.");
                AssertUpstreamEvidenceUnchanged(
                    preparationModule,
                    slotId,
                    previousPreparation,
                    initialInput,
                    "Exact current Input release");
                PlayerGameplayOccupancySummary occupancyAfterInputRelease =
                    FindOccupancy(
                        inputReleasedSnapshot.Occupancy,
                        slotId);
                AssertEqual(
                    occupancyBeforeInputRelease.Token,
                    occupancyAfterInputRelease.Token,
                    "Exact current Input release changed Gameplay Occupancy.");
                PlayerGameplayCameraEligibilitySummary cameraAfterInputRelease =
                    FindCameraEligibility(
                        inputReleasedSnapshot.CameraEligibility,
                        slotId);
                AssertEqual(
                    initialCamera.Token,
                    cameraAfterInputRelease.Token,
                    "Input release changed the independent Camera capability.");
                AssertTrue(
                    FindAdmission(inputReleasedSnapshot.Admission, slotId)
                        .IsNotAdmitted,
                    "Input release did not remove the current Admission first.");
                completed.Add("input-release-preserves-camera-and-occupancy");
                AssertTrue(
                    stableHost.IsJoined &&
                    stableHost.HasJoinedSlot &&
                    stableHost.JoinedPlayerSlotId == slotId,
                    "Exact current Input release changed Assignment or Host admission.");
                PlayerGameplayInputBindingResult releasedTokenConfirmation =
                    Invoke<PlayerGameplayInputBindingResult>(
                        gameplayModule,
                        "ConfirmCurrentInputBinding",
                        slotId,
                        initialInput.Token,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "confirm-released-input-token");
                AssertTrue(
                    releasedTokenConfirmation.Rejected,
                    "Released Input token remained current.");
                completed.Add("current-input-release-clears-only-input");
                completed.Add("current-input-release-preserves-upstream-evidence");
                completed.Add("released-input-token-becomes-stale");

                PlayerGameplayRuntimeOperationResult reboundGameplay =
                    Invoke<PlayerGameplayRuntimeOperationResult>(
                        gameplayModule,
                        "TryEnsureCurrentGameplay",
                        slotId,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "rebind-after-current-input-release");
                AssertTrue(
                    reboundGameplay != null &&
                    reboundGameplay.Succeeded,
                    "Gameplay chain could not rebind after exact Input release. " +
                    reboundGameplay?.ToDiagnosticString());
                PlayerGameplayRuntimeHostSnapshot reboundSnapshot =
                    GetGameplaySnapshot(gameplayModule);
                PlayerGameplayInputBindingSummary reboundInput =
                    FindInputBinding(
                        reboundSnapshot.InputBinding,
                        slotId);
                PlayerGameplayCameraEligibilitySummary reboundCamera =
                    FindCameraEligibility(
                        reboundSnapshot.CameraEligibility,
                        slotId);
                AssertTrue(
                    reboundInput.Token.IsValid &&
                    reboundInput.Token != initialInput.Token &&
                    reboundInput.BindingRevision >
                        initialInput.BindingRevision,
                    "Rebind after exact Input release did not renew structural identity.");
                AssertEqual(initialInput.AssignmentToken,
                    reboundInput.AssignmentToken,
                    "Rebind after Input release changed AssignmentToken.");
                AssertEqual(initialInput.HostBindingIdentity,
                    reboundInput.HostBindingIdentity,
                    "Rebind after Input release changed HostBindingIdentity.");
                AssertEqual(initialInput.PreparationToken,
                    reboundInput.PreparationToken,
                    "Rebind after Input release changed PreparationToken.");
                initialInput = reboundInput;
                initialCamera = reboundCamera;
                completed.Add("input-rebind-after-release-renews-token");

                PlayerActorDeclaration previousDeclaration =
                    ResolveDeclaration(
                        stableHost,
                        previousPreparation.Materialization.ActorId);
                AssertNotNull(previousDeclaration,
                    "Current Actor declaration was not found.");
                AssertTrue(previousDeclaration.gameObject.activeInHierarchy,
                    "Current Actor is not active before the switch.");
                completed.Add("previous-actor-active-before-request");

                targetActivity = fixture.CreateGameplayReadyActivity(
                    joined.Slot.Profile,
                    "qa.p3k7h.target.activity",
                    "Player Gameplay Admission Target Activity");
                RuntimeContentOwner targetOwner =
                    RuntimeContentOwner.Activity(
                        targetActivity.ActivityId.StableText,
                        targetActivity.ActivityName,
                        RuntimeDefinitionToken.FromUnityObject(targetActivity));
                completed.Add("gameplayready-target-authored");

                targetRoute = fixture.CreateRouteStartupTarget(
                    currentRoute,
                    targetActivity,
                    "qa.p3k7h.target.route",
                    "Player Gameplay Admission Target Route");
                RuntimeContentOwner targetRouteOwner =
                    RuntimeContentOwner.Route(
                        targetRoute.RouteId.StableText,
                        targetRoute.RouteName,
                        RuntimeDefinitionToken.FromUnityObject(targetRoute));
                AssertTrue(!ReferenceEquals(currentRoute, targetRoute) &&
                    currentRoute.RouteId != targetRoute.RouteId &&
                    currentRouteOwner != targetRouteOwner,
                    "Target Route does not have a distinct canonical Route owner.");
                completed.Add("route-startup-target-authored");

                object requestResult = await fixture.RequestRouteAsync(
                    targetRoute,
                    nameof(QaPlayerGameplayAdmissionRegression),
                    "route-startup-gameplayready-switch");
                AssertTrue(GetBooleanProperty(requestResult, "Succeeded"),
                    "Route request with GameplayReady Startup Activity failed. " +
                    GetStringProperty(requestResult, "Message"));
                completed.Add("route-startup-request-succeeded");

                RouteAsset activeRoute = fixture.CurrentRoute;
                AssertSame(targetRoute, activeRoute,
                    "Destination Route did not become current.");
                compensations.Register(
                    "target-route");
                completed.Add("target-route-became-current");

                ActivityAsset activeTarget = fixture.CurrentActivity;
                AssertSame(targetActivity, activeTarget,
                    "Target Activity did not become current.");
                completed.Add("target-activity-became-current");

                PlayerGameplayRuntimeHostSnapshot switchedGameplay =
                    GetGameplaySnapshot(gameplayModule);
                ActivityPlayerLifecycleAdmissionSnapshot lifecycle =
                    switchedGameplay.LifecycleAdmission;
                AssertNotNull(lifecycle,
                    "Player Gameplay Admission lifecycle evidence is missing after switch.");
                AssertTrue(lifecycle.IsCompleted,
                    "Player Gameplay Admission lifecycle did not complete. " +
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
                AssertEqual(currentRoute.RouteId,
                    lifecycle.Token.PreviousRouteId,
                    "Lifecycle snapshot and transaction token previous Route identities differ.");
                AssertEqual(targetRoute.RouteId,
                    lifecycle.Token.TargetRouteId,
                    "Lifecycle snapshot and transaction token target Route identities differ.");
                completed.Add("functional-route-identities-retained");
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
                    "Lifecycle transaction lost the previous Route diagnostic name.");
                AssertEqual(targetRoute.RouteName,
                    lifecycle.TargetRouteName,
                    "Lifecycle transaction lost the destination Route diagnostic name.");
                completed.Add("route-diagnostic-names-retained");

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

                PlayerGameplayCameraEligibilitySummary targetCamera =
                    FindCameraEligibility(
                        switchedGameplay.CameraEligibility,
                        slotId);
                AssertTrue(
                    targetAdmission.OccupancyToken !=
                        initialAdmission.OccupancyToken &&
                    targetAdmission.InputBindingToken !=
                        initialAdmission.InputBindingToken &&
                    targetAdmission.CameraEligibilityToken !=
                        initialAdmission.CameraEligibilityToken &&
                    targetCamera.PreparationToken == targetPreparation.Token,
                    "Candidate chain was admitted without replacing the full current capability chain.");
                completed.Add("cutover-releases-current-capabilities-before-candidate-occupancy");

                PlayerGameplayInputBindingSummary targetInput =
                    FindInputBinding(switchedGameplay.InputBinding, slotId);
                AssertTrue(targetInput.IsBound && targetInput.Token.IsValid,
                    "Target Actor has no canonical Input binding.");
                AssertTrue(targetInput.Token != initialInput.Token,
                    "Actor replacement reused the previous Input binding token.");
                AssertTrue(
                    targetInput.PreparationToken !=
                        initialInput.PreparationToken,
                    "Actor replacement did not renew the Input preparation correlation.");
                AssertEqual(initialInput.AssignmentToken,
                    targetInput.AssignmentToken,
                    "Actor replacement changed the Manager assignment token.");
                AssertEqual(initialInput.HostBindingIdentity,
                    targetInput.HostBindingIdentity,
                    "Actor replacement changed the stable Host binding.");
                PlayerGameplayInputBindingResult staleInput =
                    Invoke<PlayerGameplayInputBindingResult>(
                        gameplayModule,
                        "ConfirmCurrentInputBinding",
                        slotId,
                        initialInput.Token,
                        nameof(QaPlayerGameplayAdmissionRegression),
                        "confirm-replaced-actor-input-token");
                AssertTrue(!staleInput.Succeeded,
                    "Actor replacement accepted the previous Input token.");
                completed.Add("actor-replacement-invalidates-old-input");
                completed.Add("actor-replacement-renews-input-token");
                completed.Add("actor-replacement-preserves-assignment");
                completed.Add("actor-replacement-preserves-host");

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

                object clearResult = await fixture.ClearActivityAsync(
                    nameof(QaPlayerGameplayAdmissionRegression),
                    "clear-adopted-gameplayready-activity");
                AssertTrue(GetBooleanProperty(clearResult, "Succeeded"),
                    "Clearing adopted Activity failed. " +
                    GetStringProperty(clearResult, "Message"));
                compensations.Register(
                    "target-activity-clear");
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
                completed.Add("activity-clear-releases-gameplay-chain");
                completed.Add("activity-clear-releases-input-binding");

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

                AssertTrue(fixture.CurrentActivity == null,
                    "FrameworkRuntimeHost still reports an active Activity after clear.");
                completed.Add("activity-state-cleared");

                AssertTrue(stableHost != null && stablePlayerInput != null,
                    "Stable Session Player host or PlayerInput was destroyed by Activity exit.");
                AssertSame(stablePlayerInput, stableHost.PlayerInput,
                    "Stable Session PlayerInput changed across Activity lifecycle.");
                completed.Add("stable-session-player-survives-activity-exit");
                AssertTrue(stableHost.IsJoined &&
                    stableHost.HasJoinedSlot &&
                    stableHost.JoinedPlayerSlotId == slotId,
                    "Activity clear changed the Manager-Provisioned Slot assignment or Host admission.");
                completed.Add("activity-clear-preserves-assignment");
                completed.Add("activity-clear-preserves-host");

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
                        nameof(QaPlayerGameplayAdmissionRegression),
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

                AssertEqual(
                    ExpectedCompletedCaseCount,
                    completed.Count,
                    "Player Gameplay Admission regression case count changed unexpectedly.");

                }
                catch (TargetInvocationException exception)
                {
                    executionFailure =
                        exception.InnerException ?? exception;
                }
                catch (Exception exception)
                {
                    executionFailure = exception;
                }
            }
            finally
            {
                Exception regressionCleanupFailure = null;
                try
                {
                    await CleanupRegressionAsync(
                        fixture,
                        runtimeHost,
                        preparationModule,
                        gameplayModule,
                        cleanupSlotId,
                        authoring,
                        cleanupJoinResult,
                        runtimeContent,
                        runtimeContentType,
                        currentContext,
                        createdCurrentActivityScopeRoot,
                        entryRoute,
                        stablePlayerInput,
                        gateAdapter,
                        desiredMap,
                        originalPlayerInputEnabled,
                        originalDesiredMapEnabled,
                        originalTimeScale,
                        pauseTrigger,
                        temporaryPauseBinding,
                        temporaryPauseReference,
                        temporaryPauseBindingCreatedBySmoke,
                        originalJoiningOpen,
                        compensations,
                        targetActivity,
                        targetRoute);
                }
                catch (Exception exception)
                {
                    regressionCleanupFailure = exception;
                }

                Exception fixtureCleanupFailure = null;
                if (fixture != null)
                {
                    try
                    {
                        await fixture.CleanupAsync();
                        if (fixture.CleanupFailure != null)
                            throw new InvalidOperationException(
                                "Player QA fixture cleanup failed.",
                                fixture.CleanupFailure);
                    }
                    catch (Exception exception)
                    {
                        fixtureCleanupFailure = exception;
                    }
                }

                if (regressionCleanupFailure != null && fixtureCleanupFailure != null)
                {
                    cleanupFailure = new AggregateException(
                        "Player Gameplay Admission regression and fixture cleanup both failed.",
                        regressionCleanupFailure,
                        fixtureCleanupFailure);
                }
                else
                {
                    cleanupFailure = regressionCleanupFailure ?? fixtureCleanupFailure;
                }
            }

            if (executionFailure != null &&
                cleanupFailure != null)
            {
                throw new AggregateException(
                    "Player Gameplay Admission regression execution and cleanup both failed.",
                    executionFailure,
                    cleanupFailure);
            }

            if (executionFailure != null)
            {
                throw executionFailure;
            }

            if (cleanupFailure != null)
            {
                throw cleanupFailure;
            }

            return completed;
        }

        private static async Task CleanupRegressionAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            object runtimeHost,
            object preparationModule,
            object gameplayModule,
            PlayerSlotId playerSlotId,
            LocalPlayerProvisioningAuthoring authoring,
            LocalPlayerJoinResult joined,
            object runtimeContent,
            Type runtimeContentType,
            RuntimeScopeContext currentContext,
            bool createdCurrentActivityScopeRoot,
            RouteAsset entryRoute,
            PlayerInput playerInput,
            UnityPlayerInputGateAdapter gateAdapter,
            InputActionMap desiredMap,
            bool originalPlayerInputEnabled,
            bool originalDesiredMapEnabled,
            float originalTimeScale,
            PauseRequestTrigger pauseTrigger,
            PausePlayerInputBinding pauseBinding,
            InputActionReference pauseReference,
            bool pauseBindingCreatedBySmoke,
            bool originalJoiningOpen,
            RegressionCompensationStack compensations,
            ActivityAsset targetActivity,
            RouteAsset targetRoute)
        {
            var failures = new List<Exception>();

            bool pauseRequiresResume =
                !Mathf.Approximately(
                    Time.timeScale,
                    originalTimeScale);
            if (pauseTrigger != null &&
                pauseTrigger.TryGetPauseSnapshot(
                    out PauseSnapshot cleanupPauseSnapshot) &&
                cleanupPauseSnapshot.State == PauseState.Paused)
            {
                pauseRequiresResume = true;
            }

            if (pauseTrigger != null &&
                pauseRequiresResume)
            {
                CaptureCleanupFailure(
                    failures,
                    pauseTrigger.RequestResume,
                    "Pause cleanup failure: application resume");
            }

            if (!Mathf.Approximately(
                    Time.timeScale,
                    originalTimeScale))
            {
                Time.timeScale = originalTimeScale;
                failures.Add(
                    new InvalidOperationException(
                        "Pause cleanup failure: the official product did not restore Time.timeScale; the original value was applied as final compensation."));
            }

            if (pauseBinding != null &&
                pauseBinding.HasActiveBinding)
            {
                CaptureCleanupFailure(
                    failures,
                    () => InvokeRaw(
                        pauseBinding,
                        "ReleaseForSceneLifecycle",
                        "qa-pic1-global-cleanup",
                        null),
                    "Pause cleanup failure: PlayerInput binding release");
            }

            if (gateAdapter != null)
            {
                CaptureCleanupFailure(
                    failures,
                    () =>
                    {
                        gateAdapter.Restore();
                        AssertTrue(
                            !gateAdapter.IsBlockedByAdapter,
                            "Gate adapter remained blocked after Restore.");
                    },
                    "Gate cleanup failure");
            }

            if (playerInput != null)
            {
                CaptureCleanupFailure(
                    failures,
                    () => playerInput.enabled =
                        originalPlayerInputEnabled,
                    "PlayerInput cleanup failure: enabled posture restore");
            }

            if (playerInput != null &&
                desiredMap != null)
            {
                CaptureCleanupFailure(
                    failures,
                    () =>
                    {
                        ConfigureGateActionMap(
                            gateAdapter,
                            desiredMap);
                        playerInput.SwitchCurrentActionMap(
                            desiredMap.name);
                        if (originalDesiredMapEnabled)
                        {
                            desiredMap.Enable();
                        }
                        else
                        {
                            desiredMap.Disable();
                        }
                        AssertTrue(
                            ReferenceEquals(
                                playerInput.currentActionMap,
                                desiredMap) &&
                            desiredMap.enabled ==
                                originalDesiredMapEnabled,
                            "Original Gameplay Action Map posture was not restored.");
                    },
                    "Action Map cleanup failure");
            }

            if (gateAdapter != null)
            {
                CaptureCleanupFailure(
                    failures,
                    () =>
                    {
                        gateAdapter.ApplyCurrentGate();
                        AssertTrue(
                            !gateAdapter.IsBlockedByAdapter,
                            "Gate adapter became blocked after applying the restored canonical Gate.");
                    },
                    "Gate cleanup failure: canonical Gate reapply");
            }

            if (fixture != null &&
                targetActivity != null &&
                ReferenceEquals(
                    fixture.CurrentActivity,
                    targetActivity))
            {
                try
                {
                    object clearResult =
                        await fixture.ClearActivityAsync(
                            nameof(QaPlayerGameplayAdmissionRegression),
                            "qa-pic1-cleanup-clear-smoke-activity");
                    AssertTrue(
                        GetBooleanProperty(
                            clearResult,
                            "Succeeded"),
                        "Smoke-owned Activity clear failed. " +
                        GetStringProperty(
                            clearResult,
                            "Message"));
                }
                catch (Exception exception)
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Activity cleanup failure: smoke-owned Activity clear.",
                            exception));
                }
            }


            if (fixture != null &&
                entryRoute != null)
            {
                try
                {
                    RouteAsset currentRoute =
                        fixture.CurrentRoute;
                    if (currentRoute != null &&
                        !ReferenceEquals(currentRoute, entryRoute))
                    {
                        object routeResult =
                            await fixture.RequestRouteAsync(
                                entryRoute,
                                nameof(QaPlayerGameplayAdmissionRegression),
                                "qa-pic1-global-cleanup-restore-entry-route");
                        AssertTrue(
                            GetBooleanProperty(routeResult, "Succeeded"),
                            "Cleanup could not restore the entry Route. " +
                            GetStringProperty(routeResult, "Message"));
                    }
                }
                catch (Exception exception)
                {
                    failures.Add(
                        new InvalidOperationException(
                            "Route cleanup failure: entry Route restoration.",
                            exception));
                }
            }


            if (pauseBindingCreatedBySmoke &&
                pauseBinding != null)
            {
                UnityEngine.Object.Destroy(pauseBinding);
            }

            if (pauseReference != null)
            {
                UnityEngine.Object.Destroy(pauseReference);
            }

            if (EditorApplication.isPlaying)
            {
                await Awaitable.NextFrameAsync();
            }

            if (failures.Count > 0)
            {
                throw new AggregateException(
                    "Player Gameplay Admission regression cleanup failed. " +
                    $"registeredCompensations='{compensations?.ToDiagnosticString() ?? string.Empty}'.",
                    failures);
            }
        }

        private static void AssertManagerRollbackPrerequisites(
            object gameplayModule,
            object preparationModule,
            PlayerSlotId playerSlotId)
        {
            if (gameplayModule != null)
            {
                PlayerGameplayRuntimeHostSnapshot gameplay =
                    GetGameplaySnapshot(gameplayModule);
                AssertTrue(
                    gameplay.GameplayReadyCount == 0 &&
                    gameplay.CameraDecisionCount == 0 &&
                    gameplay.BoundInputCount == 0 &&
                    gameplay.OccupiedCount == 0,
                    "Manager join rollback is blocked by retained Gameplay chain state. " +
                    DescribeGameplaySlot(
                        gameplay,
                        playerSlotId));
            }

            if (preparationModule != null)
            {
                PlayerActorPreparationSummary preparation =
                    FindPreparation(
                        GetPreparationSnapshot(preparationModule)
                            .Preparation,
                        playerSlotId);
                AssertTrue(
                    !preparation.IsPrepared,
                    "Manager join rollback is blocked by a prepared Actor. " +
                    preparation.ToDiagnosticString());
            }
        }

        private static string DescribeGameplaySlot(
            PlayerGameplayRuntimeHostSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            if (snapshot == null)
            {
                return "snapshot=<null>";
            }

            string occupancy = "<none>";
            if (snapshot.Occupancy != null &&
                snapshot.Occupancy.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayOccupancySummary occupancySummary))
            {
                occupancy =
                    $"{occupancySummary.State}:{occupancySummary.Token.StableText}";
            }

            string input = "<none>";
            if (snapshot.InputBinding != null &&
                snapshot.InputBinding.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary inputSummary))
            {
                input =
                    $"{inputSummary.State}:{inputSummary.Token.StableText}:" +
                    $"revision={inputSummary.BindingRevision}:" +
                    $"availability={inputSummary.Availability}";
            }

            string camera = "<none>";
            if (snapshot.CameraEligibility != null &&
                snapshot.CameraEligibility.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayCameraEligibilitySummary cameraSummary))
            {
                camera =
                    $"{cameraSummary.State}:{cameraSummary.Token.StableText}";
            }

            string admission = "<none>";
            if (snapshot.Admission != null &&
                snapshot.Admission.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayAdmissionSummary admissionSummary))
            {
                admission =
                    $"{admissionSummary.State}:{admissionSummary.Token.StableText}";
            }

            return
                $"slot='{playerSlotId.StableText}' " +
                $"occupancy='{occupancy}' input='{input}' " +
                $"camera='{camera}' admission='{admission}' " +
                $"snapshot='{snapshot.ToDiagnosticString()}'";
        }

        private static void CaptureCleanupFailure(
            ICollection<Exception> failures,
            Action operation,
            string operationName)
        {
            try
            {
                operation();
            }
            catch (Exception exception)
            {
                failures.Add(
                    new InvalidOperationException(
                        $"{operationName} cleanup failed.",
                        exception));
            }
        }

        private sealed class RegressionCompensationStack
        {
            private readonly List<string> registrations =
                new List<string>();

            internal void Register(
                string mutation)
            {
                if (string.IsNullOrWhiteSpace(mutation))
                {
                    throw new ArgumentException(
                        "Compensation registration requires a mutation name.",
                        nameof(mutation));
                }

                registrations.Add(
                    mutation.Trim());
            }

            internal string ToDiagnosticString()
            {
                var reverse = new List<string>(
                    registrations.Count);
                for (int index = registrations.Count - 1;
                     index >= 0;
                     index--)
                {
                    reverse.Add(
                        registrations[index]);
                }

                return string.Join(
                    " -> ",
                    reverse);
            }
        }

        private static async Task<LocalPlayerProvisioningAuthoring>
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
                "LocalPlayerProvisioningAuthoring did not become RuntimeReady before the regression timeout.");
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
            UnityEngine.Object[] materializedObjects =
                Resources.FindObjectsOfTypeAll(runtimeHostType);
            var candidates = new List<Component>();
            var seen = new HashSet<Component>();

            for (int index = 0; index < materializedObjects.Length; index++)
            {
                UnityEngine.Object materializedObject = materializedObjects[index];
                if (materializedObject == null ||
                    !runtimeHostType.IsInstanceOfType(materializedObject) ||
                    !(materializedObject is Component component) ||
                    component.gameObject == null ||
                    EditorUtility.IsPersistent(component))
                {
                    continue;
                }

                UnityEngine.SceneManagement.Scene scene = component.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded ||
                    UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(scene) ||
                    !seen.Add(component))
                {
                    continue;
                }

                candidates.Add(component);
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "FrameworkRuntimeHost runtime instance was not found. " +
                    "Expected exactly one materialized component in a loaded scene.");
            }

            if (candidates.Count != 1)
            {
                var diagnostics = new List<string>(candidates.Count);
                for (int index = 0; index < candidates.Count; index++)
                {
                    Component candidate = candidates[index];
                    UnityEngine.SceneManagement.Scene scene = candidate.gameObject.scene;
                    diagnostics.Add(
                        $"GameObject='{candidate.gameObject.name}', " +
                        $"Scene='{scene.name}', ScenePath='{scene.path}', " +
                        $"EntityId='{candidate.GetEntityId()}'");
                }

                throw new InvalidOperationException(
                    "Expected exactly one FrameworkRuntimeHost runtime instance, " +
                    $"but found '{candidates.Count}'. Candidates: " +
                    string.Join("; ", diagnostics));
            }

            return candidates[0];
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
                "FrameworkRuntimeHost did not retain the persistent camera output. " +
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

        private static PlayerGameplayOccupancySummary FindOccupancy(
            PlayerGameplayOccupancySnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            AssertNotNull(snapshot,
                "P3K.2 Occupancy snapshot is missing.");
            AssertTrue(snapshot.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayOccupancySummary summary),
                $"P3K.2 Occupancy snapshot has no Slot '{playerSlotId.StableText}'.");
            return summary;
        }

        private static PlayerGameplayInputBindingSummary FindInputBinding(
            PlayerGameplayInputBindingSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            AssertNotNull(snapshot,
                "P3K.3 Input binding snapshot is missing.");
            AssertTrue(snapshot.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary summary),
                $"P3K.3 Input binding snapshot has no Slot '{playerSlotId.StableText}'.");
            return summary;
        }

        private static PlayerGameplayCameraEligibilitySummary
            FindCameraEligibility(
                PlayerGameplayCameraEligibilitySnapshot snapshot,
                PlayerSlotId playerSlotId)
        {
            AssertNotNull(snapshot,
                "P3K.4 Camera eligibility snapshot is missing.");
            AssertTrue(snapshot.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayCameraEligibilitySummary summary),
                $"P3K.4 Camera eligibility snapshot has no Slot '{playerSlotId.StableText}'.");
            return summary;
        }

        private static PlayerGameplayInputBindingResult
            RefreshInputAvailability(
                object gameplayModule,
                PlayerSlotId playerSlotId,
                PlayerGameplayInputBindingToken expectedBinding,
                string reason)
        {
            PlayerGameplayInputBindingResult result =
                Invoke<PlayerGameplayInputBindingResult>(
                    gameplayModule,
                    "RefreshInputAvailability",
                    playerSlotId,
                    expectedBinding,
                    nameof(QaPlayerGameplayAdmissionRegression),
                    reason);
            AssertNotNull(result,
                $"Input availability refresh returned no result. reason='{reason}'.");
            return result;
        }

        private static void AssertAvailabilityUnchangedBinding(
            PlayerGameplayInputBindingResult result,
            PlayerGameplayInputBindingSummary baseline,
            PlayerGameplayInputAvailability expectedAvailability,
            string operation)
        {
            AssertTrue(
                result.Succeeded &&
                result.CurrentSummary.IsBound &&
                result.CurrentSummary.Availability == expectedAvailability,
                $"{operation} returned an unexpected Input state. " +
                result.ToDiagnosticString());
            AssertEqual(baseline.Token,
                result.CurrentSummary.Token,
                $"{operation} changed the Input BindingToken.");
            AssertEqual(baseline.BindingRevision,
                result.CurrentSummary.BindingRevision,
                $"{operation} changed the Input BindingRevision.");
            AssertEqual(baseline.AssignmentToken,
                result.CurrentSummary.AssignmentToken,
                $"{operation} changed the AssignmentToken.");
            AssertEqual(baseline.HostBindingIdentity,
                result.CurrentSummary.HostBindingIdentity,
                $"{operation} changed the HostBindingIdentity.");
            AssertEqual(baseline.PreparationToken,
                result.CurrentSummary.PreparationToken,
                $"{operation} changed the PreparationToken.");
            AssertEqual(baseline.ActorProfileId,
                result.CurrentSummary.ActorProfileId,
                $"{operation} changed the ActorProfileId.");
            AssertEqual(baseline.ActorId,
                result.CurrentSummary.ActorId,
                $"{operation} changed the ActorId.");
        }

        private static void AssertCameraToken(
            object gameplayModule,
            PlayerSlotId playerSlotId,
            PlayerGameplayCameraEligibilityToken expectedToken,
            string operation)
        {
            PlayerGameplayCameraEligibilitySummary camera =
                FindCameraEligibility(
                    GetGameplaySnapshot(gameplayModule).CameraEligibility,
                    playerSlotId);
            AssertEqual(expectedToken, camera.Token,
                $"{operation} changed the Camera EligibilityToken.");
        }

        private static void AssertUpstreamEvidenceUnchanged(
            object preparationModule,
            PlayerSlotId playerSlotId,
            PlayerActorPreparationSummary baselinePreparation,
            PlayerGameplayInputBindingSummary baselineInput,
            string operation)
        {
            PlayerActorPreparationSummary current =
                FindPreparation(
                    GetPreparationSnapshot(preparationModule).Preparation,
                    playerSlotId);
            AssertEqual(baselinePreparation.Token, current.Token,
                $"{operation} changed the current Actor preparation.");
            AssertEqual(
                baselineInput.AssignmentToken,
                current.ActorEvidence.AssignmentToken,
                $"{operation} changed the current AssignmentToken.");
            AssertEqual(
                baselineInput.HostBindingIdentity,
                current.ActorEvidence.HostBindingIdentity,
                $"{operation} changed the current HostBindingIdentity.");
        }

        private static void AssertPauseProductResult(
            PauseRequestTrigger trigger,
            string expectedProductStatus,
            string expectedExecutionMode,
            PauseState expectedState,
            string operation)
        {
            AssertNotNull(trigger,
                $"{operation} requires the official Pause Request Trigger.");
            AssertTrue(trigger.LastRequestSucceeded,
                $"{operation} failed. status='{trigger.LastProductStatus}' " +
                $"executionMode='{trigger.LastExecutionMode}' " +
                $"message='{trigger.LastMessage}'.");
            AssertEqual(expectedProductStatus,
                trigger.LastProductStatus,
                $"{operation} returned an unexpected Pause Product status.");
            AssertEqual(expectedExecutionMode,
                trigger.LastExecutionMode,
                $"{operation} returned an unexpected execution mode.");
            AssertEqual(expectedState,
                trigger.LastCurrentState,
                $"{operation} returned an unexpected Pause state.");
        }

        private static PauseRequestTrigger ResolveBoundPauseRequestTrigger()
        {
            PauseRequestTrigger[] candidates =
                UnityEngine.Object.FindObjectsByType<PauseRequestTrigger>(
                    FindObjectsInactive.Include);
            PauseRequestTrigger resolved = null;
            int boundCount = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                PauseRequestTrigger candidate = candidates[index];
                if (candidate == null ||
                    candidate.gameObject == null ||
                    !candidate.gameObject.scene.IsValid() ||
                    !candidate.gameObject.scene.isLoaded ||
                    !candidate.HasPauseProductRequestBinding)
                {
                    continue;
                }

                resolved = candidate;
                boundCount++;
            }

            AssertEqual(1, boundCount,
                "Expected exactly one loaded Pause Request Trigger bound to the official Pause product.");
            return resolved;
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
            SerializedProperty actionMapProperty =
                serialized.FindProperty("gameplayActionMapName");
            AssertNotNull(playerInputProperty,
                "Gate adapter playerInput property was not found.");
            AssertNotNull(actionMapProperty,
                "Gate adapter gameplayActionMapName property was not found.");
            playerInputProperty.objectReferenceValue = playerInput;
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

        private static void ConfigureGateActionMap(
            UnityPlayerInputGateAdapter adapter,
            InputActionMap actionMap)
        {
            AssertNotNull(adapter,
                "Gate adapter is missing.");
            AssertNotNull(actionMap,
                "Gate Action Map is missing.");
            AssertNotNull(actionMap.asset,
                "Gate Action Map has no InputActionAsset.");

            SerializedObject serialized =
                new SerializedObject(adapter);
            SerializedProperty reference =
                serialized.FindProperty("gameplayActionMap");
            AssertNotNull(reference,
                "Gate adapter typed Gameplay Action Map reference was not found.");
            reference.FindPropertyRelative("actionAsset")
                .objectReferenceValue = actionMap.asset;
            reference.FindPropertyRelative("actionMapId")
                .stringValue = actionMap.id.ToString("D");
            reference.FindPropertyRelative("cachedActionMapName")
                .stringValue = actionMap.name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssertActionMapReconfigurationPrerequisites(
            PlayerInput playerInput,
            UnityPlayerInputGateAdapter gateAdapter,
            InputActionMap gameplayMap,
            InputActionMap alternateMap,
            InputActionMap uiMap,
            PauseRequestTrigger pauseTrigger)
        {
            AssertNotNull(playerInput,
                "Action Map reconfiguration requires the canonical PlayerInput.");
            AssertNotNull(playerInput.actions,
                "Action Map reconfiguration requires the canonical InputActionAsset.");
            AssertNotNull(gateAdapter,
                "Action Map reconfiguration requires the canonical Gate adapter.");
            AssertNotNull(gameplayMap,
                "Canonical Gameplay Action Map is missing.");
            AssertNotNull(alternateMap,
                "QA Gameplay Alternate Action Map is missing.");
            AssertNotNull(uiMap,
                "UI Action Map is missing for the negative fixture.");
            AssertTrue(
                ReferenceEquals(gameplayMap.asset, playerInput.actions) &&
                ReferenceEquals(alternateMap.asset, playerInput.actions) &&
                ReferenceEquals(uiMap.asset, playerInput.actions),
                "Gameplay, QA Gameplay Alternate and UI must belong to the exact PlayerInput.actions asset.");
            AssertEqual(
                "QA Gameplay Alternate",
                alternateMap.name,
                "Alternate Action Map has an unexpected deterministic name.");
            AssertTrue(
                alternateMap.actions.Count > 0,
                "QA Gameplay Alternate must contain at least one activatable action.");
            AssertTrue(
                uiMap.actions.Count == 0,
                "UI negative fixture changed: the map is no longer empty and may now be activatable.");
            AssertTrue(
                playerInput.enabled &&
                playerInput.isActiveAndEnabled,
                "PlayerInput must be active and enabled before structural reconfiguration.");
            AssertTrue(
                gateAdapter.HasInputGateRuntimeBinding &&
                !gateAdapter.IsBlockedByAdapter,
                "Input Gate must be bound and open before structural reconfiguration. " +
                gateAdapter.InputGateRuntimeBindingDiagnostic);
            AssertTrue(
                ReferenceEquals(playerInput.currentActionMap, gameplayMap) &&
                gameplayMap.enabled,
                "Gameplay must be current and enabled before structural reconfiguration.");
            AssertNotNull(pauseTrigger,
                "Action Map reconfiguration requires the official Pause trigger.");
            AssertTrue(
                pauseTrigger.TryGetPauseSnapshot(
                    out PauseSnapshot pauseSnapshot) &&
                pauseSnapshot.State == PauseState.Running,
                "Application must be Running before structural reconfiguration.");

            var mapDiagnostics = new List<string>();
            for (int index = 0;
                 index < playerInput.actions.actionMaps.Count;
                 index++)
            {
                InputActionMap map =
                    playerInput.actions.actionMaps[index];
                mapDiagnostics.Add(
                    $"{map.name}:{map.id:D}:enabled={map.enabled}:actions={map.actions.Count}");
            }

            var deviceDiagnostics = new List<string>();
            for (int index = 0;
                 index < playerInput.devices.Count;
                 index++)
            {
                InputDevice device = playerInput.devices[index];
                deviceDiagnostics.Add(
                    device != null
                        ? $"{device.displayName}:{device.deviceId}"
                        : "<null>");
            }

            Debug.Log(
                "[PLAYER_GAMEPLAY_ADMISSION_REGRESSION][INPUT_FIXTURE] " +
                $"asset='{playerInput.actions.name}' " +
                $"maps='{string.Join("|", mapDiagnostics)}' " +
                $"current='{playerInput.currentActionMap?.name ?? string.Empty}' " +
                $"default='{playerInput.defaultActionMap}' " +
                $"controlScheme='{playerInput.currentControlScheme}' " +
                $"devices='{string.Join("|", deviceDiagnostics)}' " +
                $"playerInputActive='{playerInput.isActiveAndEnabled}' " +
                $"gateBound='{gateAdapter.HasInputGateRuntimeBinding}' " +
                $"gateBlocked='{gateAdapter.IsBlockedByAdapter}'.");
        }

        private static void PreflightActionMapActivation(
            PlayerInput playerInput,
            InputActionMap originalMap,
            InputActionMap alternateMap)
        {
            var enabledStates = new Dictionary<Guid, bool>();
            for (int index = 0;
                 index < playerInput.actions.actionMaps.Count;
                 index++)
            {
                InputActionMap map =
                    playerInput.actions.actionMaps[index];
                enabledStates[map.id] = map.enabled;
            }

            Exception preflightFailure = null;
            try
            {
                playerInput.SwitchCurrentActionMap(
                    alternateMap.name);
                AssertTrue(
                    ReferenceEquals(
                        playerInput.currentActionMap,
                        alternateMap) &&
                    alternateMap.enabled,
                    "QA Gameplay Alternate failed direct PlayerInput activation preflight.");
            }
            catch (Exception exception)
            {
                preflightFailure = exception;
            }
            finally
            {
                try
                {
                    playerInput.SwitchCurrentActionMap(
                        originalMap.name);
                    for (int index = 0;
                         index < playerInput.actions.actionMaps.Count;
                         index++)
                    {
                        InputActionMap map =
                            playerInput.actions.actionMaps[index];
                        if (enabledStates.TryGetValue(
                                map.id,
                                out bool wasEnabled) &&
                            wasEnabled)
                        {
                            map.Enable();
                        }
                        else
                        {
                            map.Disable();
                        }
                    }

                    AssertTrue(
                        ReferenceEquals(
                            playerInput.currentActionMap,
                            originalMap) &&
                        originalMap.enabled,
                        "Action Map preflight did not restore the original Gameplay map.");
                }
                catch (Exception restoreFailure)
                {
                    throw preflightFailure != null
                        ? new AggregateException(
                            "Action Map preflight and restoration both failed.",
                            preflightFailure,
                            restoreFailure)
                        : new InvalidOperationException(
                            "Action Map preflight restoration failed.",
                            restoreFailure);
                }
            }

            if (preflightFailure != null)
            {
                throw new InvalidOperationException(
                    "QA Gameplay Alternate is not directly activatable by the canonical PlayerInput.",
                    preflightFailure);
            }
        }

        private static void ConfigureInvalidGateActionMap(
            UnityPlayerInputGateAdapter adapter,
            InputActionAsset actionAsset)
        {
            AssertNotNull(adapter,
                "Gate adapter is missing.");
            AssertNotNull(actionAsset,
                "Invalid Gate Action Map fixture requires an InputActionAsset.");

            SerializedObject serialized =
                new SerializedObject(adapter);
            SerializedProperty reference =
                serialized.FindProperty("gameplayActionMap");
            AssertNotNull(reference,
                "Gate adapter typed Gameplay Action Map reference was not found.");
            reference.FindPropertyRelative("actionAsset")
                .objectReferenceValue = actionAsset;
            reference.FindPropertyRelative("actionMapId")
                .stringValue = "ffffffff-ffff-4fff-8fff-ffffffffffff";
            reference.FindPropertyRelative("cachedActionMapName")
                .stringValue = "QA Missing Gameplay Map";
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static InputActionReference ConfigurePausePlayerInputBinding(
            PausePlayerInputBinding binding,
            PlayerInput playerInput,
            InputActionMap gameplayMap)
        {
            AssertNotNull(binding,
                "Pause PlayerInput binding component is missing.");
            AssertNotNull(playerInput,
                "Pause PlayerInput binding requires the canonical PlayerInput.");
            AssertNotNull(playerInput.actions,
                "Pause PlayerInput binding requires PlayerInput actions.");
            AssertNotNull(gameplayMap,
                "Pause PlayerInput binding requires the canonical desired Action Map.");

            InputAction pauseAction =
                playerInput.actions.FindAction(
                    PauseActionId,
                    false);
            AssertNotNull(pauseAction,
                $"Canonical PlayerInput has no Pause action GUID '{PauseActionId}'.");
            AssertNotNull(pauseAction.actionMap,
                "Canonical Pause action has no Action Map.");
            AssertTrue(
                pauseAction.actionMap.id != gameplayMap.id,
                "Pause and Gameplay must use distinct Action Maps.");

            InputActionReference reference =
                binding.PauseAction;
            bool createdReference =
                reference == null ||
                reference.action == null ||
                reference.action.id != pauseAction.id;
            if (createdReference)
            {
                reference =
                    InputActionReference.Create(pauseAction);
                AssertNotNull(reference,
                    "Could not create the temporary canonical Pause action reference.");
            }

            SerializedObject serialized =
                new SerializedObject(binding);
            serialized.FindProperty("playerInput").objectReferenceValue =
                playerInput;
            serialized.FindProperty("pauseAction").objectReferenceValue =
                reference;

            SerializedProperty gameplayReference =
                serialized.FindProperty("gameplayActionMap");
            AssertNotNull(gameplayReference,
                "Pause binding gameplay Action Map reference was not found.");
            gameplayReference.FindPropertyRelative("actionAsset")
                .objectReferenceValue = playerInput.actions;
            gameplayReference.FindPropertyRelative("actionMapId")
                .stringValue = gameplayMap.id.ToString("D");
            gameplayReference.FindPropertyRelative("cachedActionMapName")
                .stringValue = gameplayMap.name;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return createdReference
                ? reference
                : null;
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

        private static RuntimeScopeContext CreateActivityScopeContext(
            object runtimeHost,
            string ownerId,
            string displayName,
            out object runtimeContent,
            out Type runtimeContentType,
            out bool createdScopeRoot)
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
                RuntimeContentOwner.Activity(
                    ownerId,
                    displayName,
                    RuntimeDefinitionToken.MintAnonymous());
            int rootsBefore =
                CountRuntimeRoots(
                    runtimeContent,
                    runtimeContentType,
                    owner);
            GetMethod(runtimeContentType, "CreateScopeRoot").Invoke(
                runtimeContent,
                new object[]
                {
                    owner,
                    nameof(QaPlayerGameplayAdmissionRegression),
                    "create-session-gameplay-scope"
                });
            createdScopeRoot = rootsBefore == 0;

            object[] contextArguments =
            {
                owner,
                nameof(QaPlayerGameplayAdmissionRegression),
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
                            nameof(QaPlayerGameplayAdmissionRegression),
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

        private static object GetField(
            object target,
            string fieldName)
        {
            AssertNotNull(target,
                $"Cannot read field '{fieldName}' from a null target.");
            FieldInfo field = target.GetType().GetField(
                fieldName,
                InstanceAny);
            AssertNotNull(field,
                $"Field '{target.GetType().FullName}.{fieldName}' was not found.");
            return field.GetValue(target);
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

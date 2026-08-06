using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Internal.Editor
{
    /// <summary>Shared Editor-only access to the official Host-scoped Player runtime.</summary>
    public sealed class QaPlayerGameplayAdmissionFixture : IAsyncDisposable
    {
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const int MaxFrames = 300;
        private const string TargetRoutePrimaryScenePath =
            "Assets/ImmersiveFrameworkQA/Lifecycle/Scenes/QA_LifecycleRouteB.unity";
        private const string TargetRoutePrimarySceneName = "QA_LifecycleRouteB";
        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public LocalPlayerProvisioningAuthoring Provisioning { get; private set; }
        public LocalPlayerProvisioningAuthoring ProvisioningAuthoring => Provisioning;
        public object RuntimeHost { get; private set; }
        public object PreparationModule { get; private set; }
        public object GameplayModule { get; private set; }
        public object RuntimeContentRuntime { get; private set; }
        public Type RuntimeContentRuntimeType => RuntimeContentRuntime?.GetType();
        public LocalPlayerJoinResult JoinResult { get; private set; }
        public PlayerSlotId JoinedSlotId => JoinResult != null
            ? JoinResult.Slot.PlayerSlotId
            : default;
        public LocalPlayerHostAuthoring JoinedHost => JoinResult?.LocalPlayerHost;
        public UnityEngine.InputSystem.PlayerInput JoinedPlayerInput => JoinResult?.PlayerInput;
        public LocalPlayerActorSelectionRequestAuthoring SelectionEndpoint =>
            selectionEndpoint ??= Provisioning.GetComponent<LocalPlayerActorSelectionRequestAuthoring>();
        public PlayerActorSelectionResult LastSelectionResult { get; private set; }
        public bool OriginalJoiningOpen { get; private set; }
        public RuntimeScopeContext ActivityScope { get; private set; }
        public PlayerActorPreparationResult LastPreparationResult { get; private set; }
        public PlayerActorPreparationSummary CurrentPreparation =>
            LastPreparationResult != null ? LastPreparationResult.CurrentSummary : default;
        public PlayerGameplayRuntimeOperationResult LastGameplayReadyResult { get; private set; }
        public PlayerGameplayAdmissionSummary CurrentGameplayAdmission =>
            LastGameplayReadyResult != null ? LastGameplayReadyResult.CurrentAdmission : default;
        public RuntimeScopeContext CurrentActivityContext => ActivityScope;
        public RuntimeContentOwner CurrentActivityOwner => ActivityScope.Owner;
        public bool CreatedCurrentActivityScopeRoot { get; private set; }
        public bool JoiningWasOpen { get; private set; }
        public Exception CleanupFailure { get; private set; }
        public ActivityAsset LastCreatedActivity { get; private set; }
        public RouteAsset LastCreatedRoute { get; private set; }
        public int PlayerCount => Provisioning?.PlayerInputManager != null
            ? Provisioning.PlayerInputManager.playerCount
            : 0;
        public IReadOnlyList<LocalPlayerJoinResult> JoinedPlayers =>
            ownedJoinResults.AsReadOnly();
        public int BaselinePlayerCount { get; private set; }
        public int BaselineJoinedSlotCount { get; private set; }
        public int BaselineRegisteredHostCount { get; private set; }
        public int BaselineRuntimeScopeRootCount { get; private set; }
        public int RegisteredHostCount => PreparationSnapshot.RegisteredHostCount;

        public QaPlayerJoinEvidence CaptureJoinEvidence(LocalPlayerJoinResult joinResult)
        {
            if (joinResult == null)
                throw new InvalidOperationException("Cannot capture QA Player join evidence from a null result.");

            return new QaPlayerJoinEvidence(
                joinResult,
                joinResult.Slot.PlayerSlotId,
                joinResult.LocalPlayerHost,
                joinResult.PlayerInput,
                joinResult.PlayerInput != null ? joinResult.PlayerInput.playerIndex : -1,
                DescribePlayerInput(joinResult.PlayerInput),
                DescribeHost(joinResult.LocalPlayerHost));
        }

        public bool IsPrimaryJoinEvidenceCurrent(QaPlayerJoinEvidence evidence)
        {
            if (evidence == null) return false;
            return ReferenceEquals(JoinResult, evidence.JoinResult) &&
                   JoinedSlotId == evidence.SlotId &&
                   ReferenceEquals(JoinedHost, evidence.Host) &&
                   ReferenceEquals(JoinedPlayerInput, evidence.PlayerInput) &&
                   JoinedPlayerInput != null && JoinedPlayerInput.playerIndex == evidence.PlayerIndex;
        }
        private LocalPlayerActorSelectionRequestAuthoring selectionEndpoint;
        private readonly List<ScriptableObject> createdRuntimeOnlyAssets = new();
        private readonly List<LocalPlayerJoinResult> ownedJoinResults = new();
        private readonly List<OwnedJoinTeardownEvidence> ownedJoinTeardown = new();
        private bool ownsJoiningRestore;
        private ActivityPlayerLifecycleAdmissionToken ownedLifecycleRollbackToken;

        public static async Task<QaPlayerGameplayAdmissionFixture> CreateAsync()
        {
            var fixture = new QaPlayerGameplayAdmissionFixture();
            fixture.Provisioning = await fixture.AwaitProvisioningAsync();
            fixture.OriginalJoiningOpen = fixture.Provisioning.RuntimeSnapshot.JoiningOpen;
            fixture.RuntimeHost = ResolveRuntimeHost();
            if (fixture.RuntimeHost == null)
                throw new InvalidOperationException("FrameworkRuntimeHost is unavailable.");
            PropertyInfo runtimeContentProperty = fixture.RuntimeHost.GetType()
                .GetProperty("RuntimeContentRuntime", InstanceAny);
            if (runtimeContentProperty == null)
                throw new InvalidOperationException("FrameworkRuntimeHost.RuntimeContentRuntime was not found.");
            fixture.RuntimeContentRuntime = runtimeContentProperty.GetValue(fixture.RuntimeHost);
            if (fixture.RuntimeContentRuntime == null)
                throw new InvalidOperationException("FrameworkRuntimeHost has no RuntimeContentRuntime.");
            fixture.PreparationModule = ResolveHostComponent(
                fixture.RuntimeHost,
                "Immersive.Framework.PlayerParticipation.PlayerActorPreparationRuntimeHostModule",
                "Player Actor Preparation module");
            if (fixture.PreparationModule == null)
                throw new InvalidOperationException("FrameworkRuntimeHost has no Player Actor Preparation module.");
            fixture.GameplayModule = ResolveHostComponent(
                fixture.RuntimeHost,
                "Immersive.Framework.PlayerParticipation.PlayerGameplayRuntimeHostModule",
                "Player Gameplay module");
            if (fixture.GameplayModule == null)
                throw new InvalidOperationException("FrameworkRuntimeHost has no Player Gameplay module.");
            fixture.BaselinePlayerCount = fixture.PlayerCount;
            fixture.BaselineJoinedSlotCount = fixture.JoinedSlotCount;
            fixture.BaselineRegisteredHostCount = fixture.RegisteredHostCount;
            fixture.BaselineRuntimeScopeRootCount = fixture.RuntimeScopeRootCount;
            fixture.ValidateRequiredRuntimeBindings();
            return fixture;
        }

        public RouteAsset CurrentRoute => GetStateProperty<RouteAsset>("CurrentRoute");
        public ActivityAsset CurrentActivity => GetStateProperty<ActivityAsset>("CurrentActivity");

        public async Task<object> RequestActivityAsync(
            ActivityAsset activity,
            string source,
            string reason) =>
            await InvokeTaskResultAsync(RuntimeHost, "RequestActivityAsync", activity, source, reason);

        public async Task<object> RequestRouteAsync(
            RouteAsset route,
            string source,
            string reason) =>
            await InvokeTaskResultAsync(RuntimeHost, "RequestRouteAsync", route, source, reason);

        public async Task<object> ClearActivityAsync(string source, string reason) =>
            await InvokeTaskResultAsync(RuntimeHost, "ClearActivityAsync", source, reason);

        public ActivityAsset CreateGameplayReadyActivity(
            PlayerSlotProfile slotProfile,
            string activityId,
            string activityName)
        {
            if (slotProfile == null) throw new ArgumentNullException(nameof(slotProfile));
            if (string.IsNullOrWhiteSpace(activityId)) throw new ArgumentException("Activity ID is required.", nameof(activityId));
            if (string.IsNullOrWhiteSpace(activityName)) throw new ArgumentException("Activity name is required.", nameof(activityName));

            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = activityName;
            SerializedObject serialized = new SerializedObject(activity);
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("activityId").stringValue = activityId;
            serialized.FindProperty("playerParticipationProjectionMode").intValue =
                (int)ActivityParticipationProjectionMode.ExplicitSlots;
            serialized.FindProperty("playerParticipationZeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Rejected;
            SerializedProperty slots = serialized.FindProperty("playerParticipationExplicitSlotProfiles");
            slots.arraySize = 1;
            slots.GetArrayElementAtIndex(0).objectReferenceValue = slotProfile;
            serialized.FindProperty("playerParticipationRequirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.GameplayReady;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            LastCreatedActivity = activity;
            createdRuntimeOnlyAssets.Add(activity);
            return activity;
        }

        public ActivityAsset CreateGameplayReadyAllJoinedSlotsActivity(
            string activityId,
            string activityName)
        {
            if (string.IsNullOrWhiteSpace(activityId)) throw new ArgumentException("Activity ID is required.", nameof(activityId));
            if (string.IsNullOrWhiteSpace(activityName)) throw new ArgumentException("Activity name is required.", nameof(activityName));

            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = activityName;
            SerializedObject serialized = new SerializedObject(activity);
            serialized.FindProperty("activityName").stringValue = activityName;
            serialized.FindProperty("activityId").stringValue = activityId;
            serialized.FindProperty("playerParticipationProjectionMode").intValue =
                (int)ActivityParticipationProjectionMode.AllJoinedSlots;
            serialized.FindProperty("playerParticipationZeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Allowed;
            serialized.FindProperty("playerParticipationExplicitSlotProfiles").arraySize = 0;
            serialized.FindProperty("playerParticipationRequirementLevel").intValue =
                (int)PlayerParticipationRequirementLevel.GameplayReady;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            LastCreatedActivity = activity;
            createdRuntimeOnlyAssets.Add(activity);
            return activity;
        }

        public RouteAsset CreateRouteStartupTarget(
            RouteAsset currentRoute,
            ActivityAsset startupActivity,
            string routeId,
            string routeName)
        {
            if (currentRoute == null) throw new ArgumentNullException(nameof(currentRoute));
            if (startupActivity == null) throw new ArgumentNullException(nameof(startupActivity));
            if (!currentRoute.HasPrimaryScene) throw new InvalidOperationException("Current Route must expose a valid Primary Scene.");
            if (string.IsNullOrWhiteSpace(routeId)) throw new ArgumentException("Route ID is required.", nameof(routeId));
            if (string.IsNullOrWhiteSpace(routeName)) throw new ArgumentException("Route name is required.", nameof(routeName));

            RouteAsset route = ScriptableObject.CreateInstance<RouteAsset>();
            route.name = routeName;
            SerializedObject serialized = new SerializedObject(route);
            serialized.FindProperty("routeId").stringValue = routeId;
            serialized.FindProperty("routeName").stringValue = routeName;
            serialized.FindProperty("primaryScenePath").stringValue = TargetRoutePrimaryScenePath;
            serialized.FindProperty("primarySceneName").stringValue = TargetRoutePrimarySceneName;
            serialized.FindProperty("startupActivity").objectReferenceValue = startupActivity;
            serialized.FindProperty("transitionGateMode").intValue = (int)currentRoute.TransitionGateMode;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            LastCreatedRoute = route;
            createdRuntimeOnlyAssets.Add(route);
            return route;
        }
        public PlayerActorPreparationRuntimeHostSnapshot PreparationSnapshot =>
            GetPreparationSnapshot(PreparationModule);

        public PlayerGameplayRuntimeHostSnapshot GameplaySnapshot =>
            GetGameplaySnapshot(GameplayModule);

        public PlayerParticipationSnapshot ParticipationSnapshot =>
            Provisioning.RuntimeSnapshot;

        public ActivityPlayerLifecycleAdmissionSnapshot LifecycleSnapshot =>
            GameplaySnapshot.LifecycleAdmission;

        public int JoinedSlotCount => ParticipationSnapshot?.JoinedCount ?? 0;

        public int RuntimeScopeRootCount =>
            (int)GetRequiredProperty(RuntimeContentRuntime, "RootCount");

        public ActivityPlayerLifecycleAdmissionResult PrepareSameRouteLifecycle(
            ActivityAsset previousActivity,
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            ActivityPlayerLifecycleAdmissionResult result = Invoke<ActivityPlayerLifecycleAdmissionResult>(
                ResolveLifecycleAuthority(),
                "TryPrepareSameRouteSwitch",
                previousActivity,
                targetActivity,
                source,
                reason);
            if (result?.CurrentSnapshot != null &&
                result.CurrentSnapshot.Token.IsValid &&
                result.CurrentSnapshot.IsRollbackAvailable)
            {
                ownedLifecycleRollbackToken = result.CurrentSnapshot.Token;
            }

            return result;
        }

        public ActivityPlayerLifecycleAdmissionResult RollbackSameRouteLifecycle(
            ActivityPlayerLifecycleAdmissionToken token,
            string source,
            string reason)
        {
            if (!token.IsValid)
                throw new ArgumentException("Lifecycle rollback requires an exact valid token.", nameof(token));
            if (!ownedLifecycleRollbackToken.IsValid || token != ownedLifecycleRollbackToken)
                throw new InvalidOperationException(
                    "Lifecycle rollback requires the exact reversible transaction owned by this fixture.");

            ActivityPlayerLifecycleAdmissionResult result = Invoke<ActivityPlayerLifecycleAdmissionResult>(
                ResolveLifecycleAuthority(),
                "TryRollback",
                token,
                source,
                reason);
            if (result?.Status == ActivityPlayerLifecycleAdmissionStatus.SucceededRolledBack &&
                token == ownedLifecycleRollbackToken)
            {
                ownedLifecycleRollbackToken = default;
            }

            return result;
        }

        public PlayerParticipationOperationResult OpenJoining(string reason)
        {
            if (!ownsJoiningRestore)
            {
                JoiningWasOpen = Provisioning.RuntimeSnapshot.JoiningOpen;
                ownsJoiningRestore = true;
            }
            return Invoke<PlayerParticipationOperationResult>(PreparationModule,
                "TryOpenJoining", nameof(QaPlayerGameplayAdmissionFixture), reason);
        }

        public LocalPlayerJoinResult JoinPlayer(string reason)
        {
            if (JoinResult != null)
                throw new InvalidOperationException("JoinPlayer cannot replace the primary Local Player join result.");

            LocalPlayerJoinResult result = RequestOfficialJoin(reason);
            JoinResult = result;
            return result;
        }

        public LocalPlayerJoinResult JoinAdditionalPlayer(string reason)
        {
            LocalPlayerJoinResult primaryJoin = JoinResult;
            if (primaryJoin == null)
                throw new InvalidOperationException("JoinAdditionalPlayer requires the primary Player join first.");

            RequireAdditionalJoinCapacity(nameof(JoinAdditionalPlayer));

            LocalPlayerJoinResult result = RequestOfficialJoin(reason);
            ValidateSecondaryJoin(
                result,
                primaryJoin,
                primaryJoin.Slot.PlayerSlotId,
                primaryJoin.LocalPlayerHost,
                primaryJoin.PlayerInput,
                nameof(JoinAdditionalPlayer));
            return result;
        }

        public LocalPlayerJoinResult JoinAdditionalPlayerSharingPrimaryDevice(string reason)
        {
            LocalPlayerJoinResult primaryJoin = JoinResult;
            PlayerInput primaryInput = primaryJoin?.PlayerInput;
            LocalPlayerHostAuthoring primaryHost = primaryJoin?.LocalPlayerHost;
            PlayerSlotId primarySlotId = primaryJoin != null
                ? primaryJoin.Slot.PlayerSlotId
                : default;
            if (primaryJoin == null)
                throw new InvalidOperationException("JoinAdditionalPlayerSharingPrimaryDevice requires the primary Player join first.");
            if (primaryInput == null)
                throw new InvalidOperationException(
                    "Multi-Player QA requires the primary PlayerInput to expose one explicit device that can be supplied to the secondary official join request.");
            if (primaryInput.devices.Count == 0)
                throw new InvalidOperationException(
                    "Multi-Player QA requires the primary PlayerInput to expose one explicit device that can be supplied to the secondary official join request.");

            InputDevice sharedDevice = primaryInput.devices[0];
            if (sharedDevice == null || !sharedDevice.added)
                throw new InvalidOperationException(
                    "Multi-Player QA requires the primary PlayerInput to expose one explicit device that can be supplied to the secondary official join request.");

            RequireAdditionalJoinCapacity(nameof(JoinAdditionalPlayerSharingPrimaryDevice));
            PlayerParticipationSnapshot before = ParticipationSnapshot;
            Debug.Log(
                "[QA_PLAYER_FIXTURE][MULTI_PLAYER_JOIN] " +
                "phase='before-secondary' " +
                $"managerPlayers='{PlayerCount}' technicalMax='{Provisioning.TechnicalMaxPlayerCount}' " +
                $"dynamicCapacity='{before?.DynamicCapacity}' joinedSlots='{before?.JoinedCount}' " +
                $"device='{DescribeDevice(sharedDevice)}' deviceId='{sharedDevice.deviceId}' " +
                $"primaryDeviceCount='{primaryInput.devices.Count}' requestDeviceHint='True'");

            var request = new LocalPlayerJoinRequest(
                nameof(QaPlayerGameplayAdmissionFixture),
                reason,
                pairWithDevice: sharedDevice);
            LocalPlayerJoinResult result = Provisioning.RequestJoin(request);
            if (result != null && result.Succeeded)
                RegisterOwnedJoin(result);
            LogSecondaryJoinResult(result, primaryJoin, primarySlotId, primaryHost, primaryInput, sharedDevice);
            if (result == null || !result.Succeeded)
                throw new InvalidOperationException(
                    "Official secondary Local Player join with the primary device hint failed. " +
                    BuildSecondaryJoinFailureDiagnostic(result, primaryInput, sharedDevice));

            ValidateSecondaryJoin(
                result,
                primaryJoin,
                primarySlotId,
                primaryHost,
                primaryInput,
                nameof(JoinAdditionalPlayerSharingPrimaryDevice));
            if (!ContainsDevice(result.PlayerInput, sharedDevice))
                throw new InvalidOperationException(
                    "Official secondary Local Player join did not retain the explicitly supplied primary device. " +
                    BuildSecondaryJoinFailureDiagnostic(result, primaryInput, sharedDevice));
            if (!ContainsDevice(primaryInput, sharedDevice) ||
                !IsPrimaryPlayerStillMaterialized(primaryJoin, primarySlotId, primaryHost, primaryInput))
                throw new InvalidOperationException(
                    "The primary Local Player was not preserved after the secondary device-sharing join. " +
                    BuildSecondaryJoinFailureDiagnostic(result, primaryInput, sharedDevice));

            Debug.Log(
                "[QA_PLAYER_FIXTURE][MULTI_PLAYER_JOIN] " +
                "phase='secondary-completed' " +
                $"status='{result.Status}' managerPlayers='{PlayerCount}' joinedSlots='{JoinedSlotCount}' " +
                $"primarySlot='{primarySlotId}' secondarySlot='{result.Slot.PlayerSlotId}' sharedDevice='True'");
            return result;
        }

        private LocalPlayerJoinResult RequestOfficialJoin(string reason)
        {
            LocalPlayerJoinResult result = Provisioning.RequestJoin(new LocalPlayerJoinRequest(
                nameof(QaPlayerGameplayAdmissionFixture), reason));
            if (result == null || !result.Succeeded)
                throw new InvalidOperationException(
                    "Official local Player join failed. " + result?.ToDiagnosticString());
            RegisterOwnedJoin(result);
            return result;
        }

        public void AssertCleanBaseline(string caseName)
        {
            PlayerGameplayRuntimeHostSnapshot gameplay = GameplaySnapshot;
            if (PlayerCount != BaselinePlayerCount ||
                JoinedSlotCount != BaselineJoinedSlotCount ||
                RegisteredHostCount != BaselineRegisteredHostCount ||
                PreparationSnapshot.PreparedCount != 0 ||
                gameplay.GameplayReadyCount != 0 || gameplay.OccupiedCount != 0 ||
                gameplay.BoundInputCount != 0 ||
                (gameplay.CameraEligibility?.EligibleCount ?? 0) != 0 ||
                gameplay.CandidateCount != 0 || gameplay.ActivePerSlotHandoffCount != 0 ||
                gameplay.HasActiveHandoffGroup)
            {
                throw new InvalidOperationException(
                    $"Player QA baseline is contaminated before '{caseName}'. " +
                    $"players='{PlayerCount}/{BaselinePlayerCount}' joined='{JoinedSlotCount}/{BaselineJoinedSlotCount}' " +
                    $"registeredHosts='{RegisteredHostCount}/{BaselineRegisteredHostCount}' " +
                    $"preparation='{PreparationSnapshot.PreparedCount}' gameplay='{gameplay.ToDiagnosticString()}'.");
            }
        }

        public TwoPlayerActorAuthoringEvidence AssertTwoPlayerActorAuthoringReady(string caseName)
        {
            if (string.IsNullOrWhiteSpace(caseName))
                throw new ArgumentException("A QA case name is required.", nameof(caseName));

            PlayerParticipationSnapshot participation = ParticipationSnapshot;
            if (participation == null || !participation.IsInitialized)
                throw new InvalidOperationException(
                    "Two-Player Actor authoring preflight requires an initialized official Player participation snapshot.");
            if (participation.ConfiguredSlotCount < 2)
                throw new InvalidOperationException(
                    "Two-Player Actor authoring preflight requires at least two configured Player Slots. " +
                    $"configured='{participation.ConfiguredSlotCount}'.");

            PlayerSlotRuntimeSnapshot first = GetParticipationSlot(PlayerSlotId.Player1);
            PlayerSlotRuntimeSnapshot second = GetParticipationSlot(PlayerSlotId.Player2);
            PlayerSlotProfile firstSlotProfile = first.Profile;
            PlayerSlotProfile secondSlotProfile = second.Profile;
            ActorProfile firstProfile = firstSlotProfile != null ? firstSlotProfile.DefaultActorProfile : null;
            ActorProfile secondProfile = secondSlotProfile != null ? secondSlotProfile.DefaultActorProfile : null;

            ValidateTwoPlayerActorAuthoringSlot("first", first, firstSlotProfile, firstProfile);
            ValidateTwoPlayerActorAuthoringSlot("second", second, secondSlotProfile, secondProfile);
            if (ReferenceEquals(firstProfile, secondProfile) ||
                firstProfile.ActorProfileId == secondProfile.ActorProfileId)
            {
                throw new InvalidOperationException(
                    "Two-Player Actor authoring preflight requires distinct Default Actor Profiles under " +
                    $"'{participation.ActorSelectionDuplicatePolicy}'. first='{firstProfile.ActorProfileId.StableText}' " +
                    $"second='{secondProfile.ActorProfileId.StableText}'.");
            }
            if (participation.ActorSelectionDuplicatePolicy !=
                PlayerActorSelectionDuplicatePolicy.UniqueAcrossJoinedSlots)
            {
                throw new InvalidOperationException(
                    "Two-Player Actor authoring preflight requires the canonical " +
                    "UniqueAcrossJoinedSlots selection policy. " +
                    $"actual='{participation.ActorSelectionDuplicatePolicy}'.");
            }

            var evidence = new TwoPlayerActorAuthoringEvidence(
                participation.ActorSelectionDuplicatePolicy,
                first.PlayerSlotId,
                firstSlotProfile,
                firstProfile,
                second.PlayerSlotId,
                secondSlotProfile,
                secondProfile);
            Debug.Log(
                "[QA_PLAYER_FIXTURE][ACTOR_AUTHORING_PREFLIGHT] " +
                $"case='{caseName}' status='Passed' policy='{evidence.Policy}' " +
                $"firstSlot='{evidence.FirstSlotId.StableText}' firstProfile='{evidence.FirstActorProfileId.StableText}' " +
                $"secondSlot='{evidence.SecondSlotId.StableText}' secondProfile='{evidence.SecondActorProfileId.StableText}' " +
                $"profilesDistinct='{evidence.ProfilesDistinct}' firstMaterializable='{evidence.FirstMaterializable}' " +
                $"secondMaterializable='{evidence.SecondMaterializable}'.");
            return evidence;
        }

        private static void ValidateTwoPlayerActorAuthoringSlot(
            string position,
            PlayerSlotRuntimeSnapshot slot,
            PlayerSlotProfile slotProfile,
            ActorProfile actorProfile)
        {
            PlayerSlotId profileSlotId = default;
            string slotIssue = slotProfile == null ? "PlayerSlotProfile is null." : string.Empty;
            bool hasValidSlotId = slotProfile != null &&
                slotProfile.TryGetPlayerSlotId(out profileSlotId, out slotIssue);
            if (!slot.IsValid || !hasValidSlotId || profileSlotId != slot.PlayerSlotId)
            {
                throw new InvalidOperationException(
                    "Two-Player Actor authoring preflight found an invalid " + position +
                    $" Slot. snapshotSlot='{slot.PlayerSlotId.StableText}' issue='{slotIssue}'.");
            }
            ActorProfileId actorProfileId = default;
            string actorIssue = actorProfile == null ? "Default Actor Profile is null." : string.Empty;
            bool hasValidActorProfileId = actorProfile != null &&
                actorProfile.TryGetActorProfileId(out actorProfileId, out actorIssue);
            if (!hasValidActorProfileId ||
                !actorProfile.HasLogicalActorHostPrefab || !actorProfile.HasDefinedActorKind ||
                !actorProfile.HasDefinedActorRole)
            {
                throw new InvalidOperationException(
                    "Two-Player Actor authoring preflight found an invalid Default Actor Profile for " +
                    $"{position} Slot '{slot.PlayerSlotId.StableText}'. profile='{actorProfile?.name ?? "<null>"}' " +
                    $"actorId='{actorProfileId.StableText}' issue='{actorIssue}' " +
                    $"prefab='{actorProfile?.LogicalActorHostPrefab}'.");
            }
        }

        private void RegisterOwnedJoin(LocalPlayerJoinResult result)
        {
            ownedJoinResults.Add(result);
            ownedJoinTeardown.Add(new OwnedJoinTeardownEvidence(result));
        }

        private void RequireAdditionalJoinCapacity(string operation)
        {
            PlayerParticipationSnapshot participation = ParticipationSnapshot;
            if (Provisioning.TechnicalMaxPlayerCount < 2 ||
                participation == null || participation.DynamicCapacity < 2 ||
                participation.ConfiguredSlotCount < 2 ||
                participation.JoinedCount + participation.AvailableCount < 2)
            {
                throw new InvalidOperationException(
                    $"{operation} requires TechnicalMaxPlayerCount, DynamicCapacity and usable Slot capacity of at least two.");
            }
        }

        private void ValidateSecondaryJoin(
            LocalPlayerJoinResult result,
            LocalPlayerJoinResult primaryJoin,
            PlayerSlotId primarySlotId,
            LocalPlayerHostAuthoring primaryHost,
            PlayerInput primaryInput,
            string operation)
        {
            if (result == null)
                throw new InvalidOperationException($"{operation} returned a null secondary Local Player join result.");
            if (!result.Succeeded)
                throw new InvalidOperationException($"{operation} rejected the secondary Local Player join. {result.ToDiagnosticString()}");
            if (!result.Slot.PlayerSlotId.IsValid)
                throw new InvalidOperationException($"{operation} returned an invalid secondary PlayerSlotId.");
            if (result.PlayerInput == null)
                throw new InvalidOperationException($"{operation} returned no secondary PlayerInput.");
            if (result.LocalPlayerHost == null)
                throw new InvalidOperationException($"{operation} returned no secondary LocalPlayerHostAuthoring.");
            if (result.Slot.PlayerSlotId == primarySlotId)
                throw new InvalidOperationException(
                    $"{operation} reused the primary Slot. primarySlot='{primarySlotId.StableText}' secondarySlot='{result.Slot.PlayerSlotId.StableText}'.");
            if (ReferenceEquals(result.PlayerInput, primaryInput))
                throw new InvalidOperationException(
                    $"{operation} reused the primary PlayerInput. primary='{DescribePlayerInput(primaryInput)}' secondary='{DescribePlayerInput(result.PlayerInput)}'.");
            if (ReferenceEquals(result.LocalPlayerHost, primaryHost))
                throw new InvalidOperationException(
                    $"{operation} reused the primary LocalPlayerHostAuthoring. primary='{DescribeHost(primaryHost)}' secondary='{DescribeHost(result.LocalPlayerHost)}'.");
            if (!ReferenceEquals(JoinResult, primaryJoin) || JoinedSlotId != primarySlotId ||
                !ReferenceEquals(JoinedPlayerInput, primaryInput) || !ReferenceEquals(JoinedHost, primaryHost))
                throw new InvalidOperationException(
                    $"{operation} overwrote primary join identity. primarySlot='{primarySlotId.StableText}' currentSlot='{JoinedSlotId.StableText}'.");
            if (PlayerCount != 2)
                throw new InvalidOperationException($"{operation} expected PlayerCount='2', actual='{PlayerCount}'.");
            if (JoinedSlotCount != 2)
                throw new InvalidOperationException($"{operation} expected JoinedSlotCount='2', actual='{JoinedSlotCount}'.");
            if (ownedJoinResults.Count != 2)
                throw new InvalidOperationException($"{operation} expected owned joins='2', actual='{ownedJoinResults.Count}'.");
        }

        private bool IsPrimaryPlayerStillMaterialized(
            LocalPlayerJoinResult primaryJoin,
            PlayerSlotId primarySlotId,
            LocalPlayerHostAuthoring primaryHost,
            PlayerInput primaryInput)
        {
            PlayerSlotRuntimeSnapshot primarySlot = GetParticipationSlot(primarySlotId);
            return ReferenceEquals(JoinResult, primaryJoin) && primaryJoin.Succeeded &&
                   primaryJoin.Slot.PlayerSlotId == primarySlotId && primarySlot.IsJoined &&
                   ReferenceEquals(JoinedHost, primaryHost) && ReferenceEquals(JoinedPlayerInput, primaryInput);
        }

        private static bool ContainsDevice(PlayerInput playerInput, InputDevice device)
        {
            if (playerInput == null || device == null) return false;
            for (int index = 0; index < playerInput.devices.Count; index++)
                if (playerInput.devices[index] == device) return true;
            return false;
        }

        private void LogSecondaryJoinResult(
            LocalPlayerJoinResult result,
            LocalPlayerJoinResult primaryJoin,
            PlayerSlotId primarySlotId,
            LocalPlayerHostAuthoring primaryHost,
            PlayerInput primaryInput,
            InputDevice sharedDevice)
        {
            PlayerSlotId secondarySlotId = result != null ? result.Slot.PlayerSlotId : default;
            PlayerInput secondaryInput = result?.PlayerInput;
            LocalPlayerHostAuthoring secondaryHost = result?.LocalPlayerHost;
            Debug.Log(
                "[QA_PLAYER_FIXTURE][MULTI_PLAYER_JOIN] " +
                "phase='secondary-result' " +
                $"status='{result?.Status}' succeeded='{result?.Succeeded}' managerPlayers='{PlayerCount}' " +
                $"joinedSlots='{JoinedSlotCount}' ownedJoins='{ownedJoinResults.Count}' " +
                $"primarySlot='{StablePlayerSlotOrNone(primarySlotId)}' secondarySlot='{StablePlayerSlotOrNone(secondarySlotId)}' " +
                $"slotsEqual='{secondarySlotId == primarySlotId}' " +
                $"primaryPlayerInput='{DescribePlayerInput(primaryInput)}' secondaryPlayerInput='{DescribePlayerInput(secondaryInput)}' " +
                $"playerInputsReferenceEqual='{ReferenceEquals(primaryInput, secondaryInput)}' " +
                $"primaryPlayerIndex='{primaryInput?.playerIndex}' secondaryPlayerIndex='{secondaryInput?.playerIndex}' " +
                $"primaryPlayerInputEntity='{DescribeEntity(primaryInput)}' secondaryPlayerInputEntity='{DescribeEntity(secondaryInput)}' " +
                $"primaryHost='{DescribeHost(primaryHost)}' secondaryHost='{DescribeHost(secondaryHost)}' " +
                $"hostsReferenceEqual='{ReferenceEquals(primaryHost, secondaryHost)}' " +
                $"primaryHostEntity='{DescribeEntity(primaryHost)}' secondaryHostEntity='{DescribeEntity(secondaryHost)}' " +
                $"device='{DescribeDevice(sharedDevice)}' primaryContainsDevice='{ContainsDevice(primaryInput, sharedDevice)}' " +
                $"secondaryContainsDevice='{ContainsDevice(secondaryInput, sharedDevice)}' " +
                $"result='{result?.ToDiagnosticString()}'");
        }

        private string BuildSecondaryJoinFailureDiagnostic(
            LocalPlayerJoinResult result,
            PlayerInput primaryInput,
            InputDevice primaryDevice)
        {
            return $"result='{result?.ToDiagnosticString()}' managerPlayers='{PlayerCount}' " +
                   $"technicalMax='{Provisioning.TechnicalMaxPlayerCount}' dynamicCapacity='{ParticipationSnapshot?.DynamicCapacity}' " +
                   $"joiningEnabled='{Provisioning.RuntimeSnapshot?.JoiningOpen}' primaryDevice='{DescribeDevice(primaryDevice)}' " +
                   $"primaryDeviceId='{primaryDevice?.deviceId}' primaryDevices='{DescribeDevices(primaryInput)}'.";
        }

        private static string DescribeDevice(InputDevice device)
        {
            return device == null ? "<null>" : $"{device.displayName}|{device.layout}|{device.deviceId}";
        }

        private static string DescribeDevices(PlayerInput playerInput)
        {
            if (playerInput == null || playerInput.devices.Count == 0) return "<none>";
            string[] devices = new string[playerInput.devices.Count];
            for (int index = 0; index < playerInput.devices.Count; index++)
                devices[index] = DescribeDevice(playerInput.devices[index]);
            return string.Join(",", devices);
        }

        private static string StablePlayerSlotOrNone(PlayerSlotId slotId)
        {
            return slotId.IsValid ? slotId.StableText : "<none>";
        }

        private static string DescribePlayerInput(PlayerInput playerInput)
        {
            return playerInput == null
                ? "<null>"
                : $"name={playerInput.name}|playerIndex={playerInput.playerIndex}|entity={DescribeEntity(playerInput)}|deviceCount={playerInput.devices.Count}";
        }

        private static string DescribeHost(LocalPlayerHostAuthoring host)
        {
            return host == null
                ? "<null>"
                : $"gameObject={host.gameObject.name}|entity={DescribeEntity(host)}|playerInput={DescribePlayerInput(host.PlayerInput)}";
        }

        private static string DescribeEntity(UnityEngine.Object instance)
        {
            return instance == null ? "<null>" : instance.GetEntityId().ToString();
        }

        public PlayerActorSelectionResult SelectDefaultActor(string source, string reason)
        {
            if (JoinResult == null) throw new InvalidOperationException("JoinPlayer must run first.");
            return SelectDefaultActor(JoinResult.Slot.PlayerSlotId, source, reason);
        }

        public PlayerActorSelectionResult SelectDefaultActor(
            PlayerSlotId slotId,
            string source,
            string reason)
        {
            if (SelectionEndpoint == null) throw new InvalidOperationException("Official Actor selection endpoint is unavailable.");
            PlayerSlotRuntimeSnapshot slot = GetParticipationSlot(slotId);
            PlayerActorSelectionResult result = SelectionEndpoint.RequestDefaultActorSelection(
                slotId, slot.SelectionRevision,
                source, reason);
            if (result == null || !result.Succeeded)
                throw new InvalidOperationException("Official default Actor selection failed. " + result?.ToDiagnosticString());
            LastSelectionResult = result;
            return LastSelectionResult;
        }

        public RuntimeScopeContext CreateCurrentActivityScope(string source, string reason)
        {
            ActivityAsset activity = CurrentActivity;
            if (activity == null || !activity.HasValidActivityId)
                throw new InvalidOperationException("Current Activity is unavailable or has an invalid ID.");
            RuntimeContentOwner owner = RuntimeContentOwner.Activity(activity.ActivityId.StableText, activity.ActivityName);
            object rootResult = Invoke(RuntimeContentRuntime, "CreateScopeRoot", owner, source, reason);
            CreatedCurrentActivityScopeRoot = (bool)rootResult.GetType().GetProperty("Applied", InstanceAny).GetValue(rootResult);
            object[] arguments = { owner, source, reason, null };
            MethodInfo createContext = ResolveRequiredMethod(
                RuntimeContentRuntime.GetType(),
                "TryCreateScopeContext",
                typeof(RuntimeContentOwner),
                typeof(string),
                typeof(string),
                typeof(RuntimeScopeContext).MakeByRefType());
            bool created = (bool)createContext.Invoke(RuntimeContentRuntime, arguments);
            if (!created) throw new InvalidOperationException("Could not create official Activity RuntimeScopeContext.");
            ActivityScope = (RuntimeScopeContext)arguments[3];
            return ActivityScope;
        }

        public PlayerActorPreparationResult PrepareSelectedActor(string source, string reason)
        {
            if (JoinResult == null || !ActivityScope.Owner.IsValid) throw new InvalidOperationException("Join and Activity scope are required.");
            return PrepareSelectedActor(JoinResult.Slot.PlayerSlotId, source, reason);
        }

        public PlayerActorPreparationResult PrepareSelectedActor(
            PlayerSlotId slotId,
            string source,
            string reason)
        {
            if (!ActivityScope.Owner.IsValid) throw new InvalidOperationException("Activity scope is required.");
            LastPreparationResult = Invoke<PlayerActorPreparationResult>(PreparationModule,
                "TryPrepareSelectedActor", ActivityScope, slotId,
                source, reason);
            return LastPreparationResult;
        }

        public PlayerGameplayRuntimeOperationResult EnsureGameplayReady(string source, string reason)
        {
            if (JoinResult == null) throw new InvalidOperationException("JoinPlayer must run first.");
            return EnsureGameplayReady(JoinResult.Slot.PlayerSlotId, source, reason);
        }

        public PlayerGameplayRuntimeOperationResult EnsureGameplayReady(
            PlayerSlotId slotId,
            string source,
            string reason)
        {
            LastGameplayReadyResult = Invoke<PlayerGameplayRuntimeOperationResult>(GameplayModule,
                "TryEnsureCurrentGameplay", slotId,
                source, reason);
            return LastGameplayReadyResult;
        }

        public PlayerSlotRuntimeSnapshot GetParticipationSlot(PlayerSlotId slotId)
        {
            PlayerParticipationSnapshot participation = ParticipationSnapshot;
            if (participation == null)
                throw new InvalidOperationException("Official Player participation snapshot is unavailable.");

            for (int index = 0; index < participation.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = participation.Slots[index];
                if (slot.PlayerSlotId == slotId) return slot;
            }

            throw new InvalidOperationException(
                $"Player Slot '{slotId.StableText}' is absent from the official participation snapshot.");
        }

        public bool TryGetPreparationSummary(
            PlayerSlotId slotId,
            out PlayerActorPreparationSummary preparation) =>
            TryGetPreparationSummary(PreparationSnapshot, slotId, out preparation);

        public bool IsSlotPrepared(
            PlayerSlotId slotId,
            out PlayerActorPreparationSummary preparation)
        {
            return TryGetPreparationSummary(slotId, out preparation) &&
                   preparation.IsPrepared;
        }

        public bool TryGetGameplayAdmissionSummary(
            PlayerSlotId slotId,
            out PlayerGameplayAdmissionSummary admission)
        {
            PlayerGameplayAdmissionSnapshot snapshot = GameplaySnapshot.Admission;
            if (snapshot != null && snapshot.TryGetSummary(slotId, out admission)) return true;
            admission = default;
            return false;
        }

        public bool IsGameplayReadyAdmitted(
            PlayerSlotId slotId,
            out PlayerGameplayAdmissionSummary admission)
        {
            return TryGetGameplayAdmissionSummary(slotId, out admission) &&
                   admission.GameplayReady;
        }

        public async Task CleanupAsync()
        {
            try
            {
                RollbackOwnedLifecycleIfRequired();
                DestroyCreatedRuntimeOnlyAssets();
                for (int index = ownedJoinResults.Count - 1; index >= 0; index--)
                {
                    ReleaseGameplayChain(ownedJoinResults[index].Slot.PlayerSlotId);
                }
                EnsureGameplayTerminalClean();
                for (int index = ownedJoinResults.Count - 1; index >= 0; index--)
                {
                    ReleasePreparedActor(ownedJoinResults[index].Slot.PlayerSlotId);
                }
                EnsurePreparationTerminalClean();
                object provisioningModule = Provisioning.GetType().GetField("runtimeModule", InstanceAny).GetValue(Provisioning);
                for (int index = ownedJoinResults.Count - 1; index >= 0; index--)
                {
                    LocalPlayerJoinResult rollback = Invoke<LocalPlayerJoinResult>(
                        provisioningModule,
                        "RollbackCommittedJoin",
                        ownedJoinResults[index],
                        "fixture-cleanup");
                    if (rollback == null || rollback.RollbackResult == null ||
                        !rollback.RollbackResult.Succeeded)
                        throw new InvalidOperationException("Official Local Player join rollback failed. " + rollback?.ToDiagnosticString());
                    Debug.Log(
                        "[QA_PLAYER_FIXTURE][JOIN_ROLLBACK] " +
                        $"slot='{ownedJoinResults[index].Slot.PlayerSlotId.StableText}' " +
                        $"operation='{ownedJoinResults[index].OperationId.StableText}' status='{rollback.Status}' " +
                        $"rollbackSucceeded='True' registeredHostsAfter='{RegisteredHostCount}' " +
                        $"managerPlayersAfter='{PlayerCount}' message='{rollback.Message}'.");
                }
                Debug.Log(
                    "[QA_PLAYER_FIXTURE][CLEANUP] phase='logical-released' " +
                    $"ownedJoins='{ownedJoinResults.Count}' players='{PlayerCount}' joined='{JoinedSlotCount}' " +
                    $"registeredHosts='{RegisteredHostCount}' prepared='{PreparationSnapshot.PreparedCount}' " +
                    $"gameplayReady='{GameplaySnapshot.GameplayReadyCount}' occupied='{GameplaySnapshot.OccupiedCount}' " +
                    $"inputBound='{GameplaySnapshot.BoundInputCount}' cameraEligible='{GameplaySnapshot.CameraEligibility?.EligibleCount ?? 0}' " +
                    $"candidates='{GameplaySnapshot.CandidateCount}' handoffs='{GameplaySnapshot.ActivePerSlotHandoffCount}' " +
                    $"group='{(GameplaySnapshot.HasActiveHandoffGroup ? "active" : "inactive")}'.");
                if (CreatedCurrentActivityScopeRoot && CurrentActivityOwner.IsValid)
                {
                    Invoke(RuntimeContentRuntime, "RemoveScopeRoot", CurrentActivityOwner,
                        nameof(QaPlayerGameplayAdmissionFixture), "fixture-cleanup");
                    CreatedCurrentActivityScopeRoot = false;
                }
                if (ownsJoiningRestore)
                {
                    PlayerParticipationOperationResult restored = JoiningWasOpen
                        ? Provisioning.OpenJoining(nameof(QaPlayerGameplayAdmissionFixture), "fixture-cleanup")
                        : Provisioning.CloseJoining(nameof(QaPlayerGameplayAdmissionFixture), "fixture-cleanup");
                    if (restored == null || !restored.Completed || restored.Snapshot.JoiningOpen != JoiningWasOpen)
                        throw new InvalidOperationException("Joining state cleanup failed.");
                    ownsJoiningRestore = false;
                }
                int framesWaited = await AwaitOwnedPhysicalHostsDestroyedAsync(ownedJoinTeardown);
                if (PlayerCount != BaselinePlayerCount ||
                    JoinedSlotCount != BaselineJoinedSlotCount ||
                    RegisteredHostCount != BaselineRegisteredHostCount)
                    throw new InvalidOperationException("Fixture cleanup retained joined Player state.");
                Debug.Log(
                    "[QA_PLAYER_FIXTURE][CLEANUP] phase='physical-destroyed' " +
                    $"framesWaited='{framesWaited}' playerInputsAlive='0' hostsAlive='0' gameObjectsAlive='0' " +
                    $"managerPlayers='{PlayerCount}' registeredHosts='{RegisteredHostCount}'.");
                ownedJoinResults.Clear();
                ownedJoinTeardown.Clear();
                JoinResult = null;
            }
            catch (Exception exception) { CleanupFailure ??= exception; }
            await Task.CompletedTask;
        }

        public async ValueTask DisposeAsync() => await CleanupAsync();

        private T GetStateProperty<T>(string name) where T : class
        {
            object state = RuntimeHost.GetType().GetProperty("State", InstanceAny).GetValue(RuntimeHost);
            return state.GetType().GetProperty(name, InstanceAny).GetValue(state) as T;
        }

        private void DestroyCreatedRuntimeOnlyAssets()
        {
            RouteAsset currentRoute = CurrentRoute;
            ActivityAsset currentActivity = CurrentActivity;
            for (int index = 0; index < createdRuntimeOnlyAssets.Count; index++)
            {
                ScriptableObject asset = createdRuntimeOnlyAssets[index];
                if (ReferenceEquals(asset, currentRoute) || ReferenceEquals(asset, currentActivity))
                    throw new InvalidOperationException("Fixture cannot destroy a runtime-only asset that remains published by FrameworkRuntimeHost.");
            }

            for (int index = createdRuntimeOnlyAssets.Count - 1; index >= 0; index--)
            {
                ScriptableObject asset = createdRuntimeOnlyAssets[index];
                if (asset != null) UnityEngine.Object.Destroy(asset);
            }

            createdRuntimeOnlyAssets.Clear();
        }

        private async Task<LocalPlayerProvisioningAuthoring> AwaitProvisioningAsync()
        {
            for (int frame = 0; frame < MaxFrames; frame++)
            {
                LocalPlayerProvisioningAuthoring[] candidates = UnityEngine.Object.FindObjectsByType<LocalPlayerProvisioningAuthoring>(FindObjectsInactive.Include);
                LocalPlayerProvisioningAuthoring result = null;
                for (int index = 0; index < candidates.Length; index++)
                    if (candidates[index] != null && candidates[index].gameObject.scene.isLoaded) result = result == null ? candidates[index] : throw new InvalidOperationException("Expected one loaded LocalPlayerProvisioningAuthoring.");
                if (result != null && result.RuntimeReady) return result;
                await Awaitable.NextFrameAsync();
            }
            throw new InvalidOperationException("LocalPlayerProvisioningAuthoring did not become RuntimeReady.");
        }

        private static object ResolveRuntimeHost()
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
                throw new InvalidOperationException(
                    "FrameworkRuntimeHost runtime instance was not found. " +
                    "Expected exactly one materialized component in a loaded scene.");

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
            Type moduleType = ResolveRuntimeType(typeName);
            Component hostComponent = runtimeHost as Component;
            if (hostComponent == null)
                throw new InvalidOperationException("FrameworkRuntimeHost is not a Unity Component.");
            Component module = hostComponent.GetComponent(moduleType);
            return module ?? throw new InvalidOperationException(
                $"FrameworkRuntimeHost has no {label}.");
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = typeof(PlayerGameplayRuntimeHostSnapshot).Assembly.GetType(fullName, false);
            return type ?? throw new InvalidOperationException(
                $"Runtime type '{fullName}' was not found.");
        }

        private void ValidateRequiredRuntimeBindings()
        {
            int bindings = 0;
            ResolveRequiredMethod(RuntimeHost.GetType(), "RequestActivityAsync", typeof(ActivityAsset), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(RuntimeHost.GetType(), "RequestRouteAsync", typeof(RouteAsset), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(RuntimeHost.GetType(), "ClearActivityAsync", typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(RuntimeContentRuntime.GetType(), "CreateScopeRoot", typeof(RuntimeContentOwner), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(RuntimeContentRuntime.GetType(), "TryCreateScopeContext", typeof(RuntimeContentOwner), typeof(string), typeof(string), typeof(RuntimeScopeContext).MakeByRefType()); bindings++;
            ResolveRequiredMethod(RuntimeContentRuntime.GetType(), "RemoveScopeRoot", typeof(RuntimeContentOwner), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(PreparationModule.GetType(), "TryOpenJoining", typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(PreparationModule.GetType(), "TryPrepareSelectedActor", typeof(RuntimeScopeContext), typeof(PlayerSlotId), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(PreparationModule.GetType(), "TryReleasePreparedActor", typeof(PlayerSlotId), typeof(PlayerActorPreparationToken), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(PreparationModule.GetType(), "TryGetSnapshot", typeof(PlayerActorPreparationRuntimeHostSnapshot).MakeByRefType()); bindings++;
            ResolveRequiredMethod(GameplayModule.GetType(), "TryEnsureCurrentGameplay", typeof(PlayerSlotId), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(GameplayModule.GetType(), "TryReleaseCurrentGameplay", typeof(PlayerSlotId), typeof(PlayerGameplayAdmissionToken), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(GameplayModule.GetType(), "TryGetSnapshot", typeof(PlayerGameplayRuntimeHostSnapshot).MakeByRefType()); bindings++;
            object lifecycle = ResolveLifecycleAuthority();
            ResolveRequiredMethod(lifecycle.GetType(), "TryPrepareSameRouteSwitch", typeof(ActivityAsset), typeof(ActivityAsset), typeof(string), typeof(string)); bindings++;
            ResolveRequiredMethod(lifecycle.GetType(), "TryRollback", typeof(ActivityPlayerLifecycleAdmissionToken), typeof(string), typeof(string)); bindings++;
            object provisioning = Provisioning.GetType().GetField("runtimeModule", InstanceAny)?.GetValue(Provisioning);
            MethodInfo rollback = ResolveRequiredMethod(provisioning?.GetType(), "RollbackCommittedJoin", typeof(LocalPlayerJoinResult), typeof(string)); bindings++;
            ValidateReflectionBinderSelfTest();
            Debug.Log("[QA_PLAYER_FIXTURE][REFLECTION_PREFLIGHT] status='Passed' " +
                $"bindings='{bindings}' rollbackSignature='{DescribeMethodSignature(rollback)}' ambiguous='0' missing='0'");
        }

        private static void ValidateReflectionBinderSelfTest()
        {
            var probe = new ReflectionBinderProbe();
            MethodInfo stringMethod = ResolveMethodForArguments(probe, "Operation", new object[] { "value" });
            MethodInfo pairMethod = ResolveMethodForArguments(probe, "Operation", new object[] { "value", true });
            MethodInfo intMethod = ResolveMethodForArguments(probe, "Operation", new object[] { 1 });
            if (stringMethod.GetParameters()[0].ParameterType != typeof(string) ||
                pairMethod.GetParameters().Length != 2 ||
                intMethod.GetParameters()[0].ParameterType != typeof(int))
                throw new InvalidOperationException("Reflection binder self-test selected an unexpected overload.");
            try { ResolveMethodForArguments(probe, "Operation", new object[] { null }); }
            catch (InvalidOperationException exception) when (exception.Message.Contains("Reflection binding remains ambiguous."))
            {
                return;
            }
            throw new InvalidOperationException("Reflection binder self-test expected null overload ambiguity.");
        }

        private sealed class ReflectionBinderProbe
        {
            private void Operation(string value) { }
            private void Operation(string value, bool flag) { }
            private void Operation(object value) { }
            private void Operation(int value) { }
        }
        private static object Invoke(object target, string method, params object[] arguments)
        {
            if (target == null)
                throw new InvalidOperationException(
                    $"Reflection operation '{method}' cannot run because its target module is unavailable.");

            object[] resolvedArguments = arguments ?? Array.Empty<object>();
            MethodInfo methodInfo = ResolveMethodForArguments(target, method, resolvedArguments);
            try
            {
                return methodInfo.Invoke(target, resolvedArguments);
            }
            catch (TargetInvocationException exception)
            {
                Exception inner = exception.InnerException ?? exception;
                throw new InvalidOperationException(
                    $"Reflection operation '{DescribeMethodSignature(methodInfo)}' failed. " +
                    $"innerType='{inner.GetType().FullName}' message='{inner.Message}'.",
                    inner);
            }
        }

        private static MethodInfo ResolveMethodForArguments(
            object target,
            string methodName,
            IReadOnlyList<object> arguments)
        {
            Type targetType = target?.GetType() ??
                throw new InvalidOperationException($"Reflection operation '{methodName}' has no target type.");
            var candidates = new List<string>();
            var compatible = new List<(MethodInfo Method, int Score)>();
            foreach (MethodInfo candidate in targetType.GetMethods(InstanceAny))
            {
                if (candidate.Name != methodName || candidate.ContainsGenericParameters) continue;
                candidates.Add(DescribeMethodSignature(candidate));
                ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length != arguments.Count) continue;
                int score = 0;
                bool matches = true;
                for (int index = 0; index < parameters.Length; index++)
                {
                    Type parameterType = parameters[index].ParameterType;
                    bool byRef = parameterType.IsByRef;
                    if (byRef) parameterType = parameterType.GetElementType();
                    object argument = arguments[index];
                    if (argument == null)
                    {
                        if (parameterType.IsValueType &&
                            Nullable.GetUnderlyingType(parameterType) == null)
                        {
                            matches = false;
                            break;
                        }
                        score += 1;
                        continue;
                    }

                    Type argumentType = argument.GetType();
                    if (parameterType == argumentType)
                    {
                        score += 4;
                    }
                    else if (parameterType.IsInstanceOfType(argument))
                    {
                        score += 2;
                    }
                    else
                    {
                        matches = false;
                        break;
                    }
                }
                if (matches) compatible.Add((candidate, score));
            }

            if (compatible.Count == 0)
                throw CreateReflectionResolutionException(
                    targetType, methodName, arguments, candidates, compatible,
                    "No compatible overload was found.");
            int highest = int.MinValue;
            for (int index = 0; index < compatible.Count; index++)
                highest = Math.Max(highest, compatible[index].Score);
            MethodInfo selected = null;
            int highestCount = 0;
            for (int index = 0; index < compatible.Count; index++)
            {
                if (compatible[index].Score != highest) continue;
                selected = compatible[index].Method;
                highestCount++;
            }
            if (highestCount != 1)
                throw CreateReflectionResolutionException(
                    targetType, methodName, arguments, candidates, compatible,
                    "Reflection binding remains ambiguous.");
            return selected;
        }

        private static MethodInfo ResolveRequiredMethod(
            Type targetType,
            string methodName,
            params Type[] parameterTypes)
        {
            MethodInfo method = targetType?.GetMethod(
                methodName,
                InstanceAny,
                binder: null,
                types: parameterTypes,
                modifiers: null);
            if (method == null)
            {
                string parameterDescription = string.Join(",",
                    Array.ConvertAll(parameterTypes,
                        parameterType => parameterType?.FullName ?? "<null>"));
                throw new InvalidOperationException(
                    $"Required reflection binding is missing. target='{targetType?.FullName}' " +
                    $"method='{methodName}' parameters='{parameterDescription}'.");
            }
            return method;
        }

        private static InvalidOperationException CreateReflectionResolutionException(
            Type targetType,
            string methodName,
            IReadOnlyList<object> arguments,
            IReadOnlyList<string> candidates,
            IReadOnlyList<(MethodInfo Method, int Score)> compatible,
            string issue)
        {
            string argumentTypes = string.Join(",",
                GetArgumentTypeNames(arguments));
            var compatibleDescriptions = new List<string>();
            for (int index = 0; index < compatible.Count; index++)
                compatibleDescriptions.Add(
                    DescribeMethodSignature(compatible[index].Method) +
                    " score='" + compatible[index].Score + "'");
            return new InvalidOperationException(
                "Reflection method resolution failed. " +
                $"target='{targetType.FullName}' method='{methodName}' arguments='{argumentTypes}' " +
                $"candidates='{string.Join(";", candidates)}' " +
                $"compatible='{string.Join(";", compatibleDescriptions)}' issue='{issue}'.");
        }

        private static IEnumerable<string> GetArgumentTypeNames(IReadOnlyList<object> arguments)
        {
            for (int index = 0; index < arguments.Count; index++)
                yield return arguments[index]?.GetType().FullName ?? "<null>";
        }

        private static string DescribeMethodSignature(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            var parameterNames = new string[parameters.Length];
            for (int index = 0; index < parameters.Length; index++)
            {
                Type type = parameters[index].ParameterType;
                if (type.IsByRef) type = type.GetElementType();
                parameterNames[index] = type.Name;
            }
            return method.DeclaringType.Name + "." + method.Name +
                "(" + string.Join(",", parameterNames) + ")";
        }

        private static object GetRequiredProperty(object target, string propertyName)
        {
            PropertyInfo property = target?.GetType().GetProperty(propertyName, InstanceAny);
            if (property == null)
                throw new InvalidOperationException(
                    $"Runtime evidence property '{propertyName}' is unavailable on '{target?.GetType().FullName}'.");
            return property.GetValue(target);
        }
        private static T Invoke<T>(object target, string method, params object[] arguments) => (T)Invoke(target, method, arguments);

        private static PlayerActorPreparationRuntimeHostSnapshot GetPreparationSnapshot(object module)
        {
            const string operation = "Player Actor Preparation snapshot";
            const string methodName = "TryGetSnapshot";
            if (module == null)
                throw new InvalidOperationException(
                    $"{operation} cannot run because its module is unavailable. Expected method '{methodName}'.");

            MethodInfo method = ResolveRequiredMethod(
                module.GetType(),
                methodName,
                typeof(PlayerActorPreparationRuntimeHostSnapshot).MakeByRefType());

            object[] arguments = { null };
            if (!(method.Invoke(module, arguments) is bool available))
                throw new InvalidOperationException(
                    $"{operation} expected boolean availability from '{methodName}' on module " +
                    $"'{module.GetType().FullName}'.");

            PlayerActorPreparationRuntimeHostSnapshot snapshot =
                arguments[0] as PlayerActorPreparationRuntimeHostSnapshot;
            if (snapshot == null)
                throw new InvalidOperationException(
                    $"{operation} returned an absent snapshot from '{methodName}' on module " +
                    $"'{module.GetType().FullName}'.");
            if (!available && snapshot.IsInitialized)
                throw new InvalidOperationException(
                    $"{operation} is incoherent: '{methodName}' reported unavailable, " +
                    $"but its snapshot from module '{module.GetType().FullName}' is initialized.");
            return snapshot;
        }

        private static PlayerGameplayRuntimeHostSnapshot GetGameplaySnapshot(object module)
        {
            const string operation = "Player Gameplay snapshot";
            const string methodName = "TryGetSnapshot";
            if (module == null)
                throw new InvalidOperationException(
                    $"{operation} cannot run because its module is unavailable. Expected method '{methodName}'.");

            MethodInfo method = ResolveRequiredMethod(
                module.GetType(),
                methodName,
                typeof(PlayerGameplayRuntimeHostSnapshot).MakeByRefType());

            object[] arguments = { null };
            if (!(method.Invoke(module, arguments) is bool available))
                throw new InvalidOperationException(
                    $"{operation} expected boolean availability from '{methodName}' on module " +
                    $"'{module.GetType().FullName}'.");

            PlayerGameplayRuntimeHostSnapshot snapshot =
                arguments[0] as PlayerGameplayRuntimeHostSnapshot;
            if (snapshot == null)
                throw new InvalidOperationException(
                    $"{operation} returned an absent snapshot from '{methodName}' on module " +
                    $"'{module.GetType().FullName}'.");
            if (!available && snapshot.IsInitialized)
                throw new InvalidOperationException(
                    $"{operation} is incoherent: '{methodName}' reported unavailable, " +
                    $"but its snapshot from module '{module.GetType().FullName}' is initialized.");
            return snapshot;
        }

        private object ResolveLifecycleAuthority()
        {
            FieldInfo contextField = GameplayModule?.GetType().GetField(
                "activityLifecycleAdmissionContext",
                InstanceAny);
            if (contextField == null)
                throw new InvalidOperationException(
                    "Player Gameplay module does not expose its Host-scoped Activity Player lifecycle admission authority.");

            object authority = contextField.GetValue(GameplayModule);
            return authority ?? throw new InvalidOperationException(
                "Host-scoped Activity Player lifecycle admission authority is unavailable.");
        }

        private void RollbackOwnedLifecycleIfRequired()
        {
            if (!ownedLifecycleRollbackToken.IsValid) return;

            ActivityPlayerLifecycleAdmissionSnapshot snapshot = LifecycleSnapshot;
            if (snapshot == null || snapshot.Token != ownedLifecycleRollbackToken ||
                !snapshot.IsRollbackAvailable)
            {
                throw new InvalidOperationException(
                    "Fixture-owned lifecycle transaction is no longer the exact reversible transaction required for cleanup.");
            }

            ActivityPlayerLifecycleAdmissionResult rollback = RollbackSameRouteLifecycle(
                ownedLifecycleRollbackToken,
                nameof(QaPlayerGameplayAdmissionFixture),
                "fixture-cleanup-rollback-lifecycle");
            if (rollback == null ||
                rollback.Status != ActivityPlayerLifecycleAdmissionStatus.SucceededRolledBack)
            {
                throw new InvalidOperationException(
                    "Fixture could not roll back its exact lifecycle transaction. " +
                    rollback?.ToDiagnosticString());
            }
        }

        private void ReleasePreparedActor(PlayerSlotId slotId)
        {
            PlayerActorPreparationRuntimeHostSnapshot snapshot = PreparationSnapshot;
            if (!TryGetPreparationSummary(snapshot, slotId, out PlayerActorPreparationSummary preparation) ||
                !preparation.IsPrepared) return;
            PlayerActorPreparationResult released = Invoke<PlayerActorPreparationResult>(
                PreparationModule, "TryReleasePreparedActor", slotId,
                preparation.Token, nameof(QaPlayerGameplayAdmissionFixture), "fixture-cleanup");
            if (released == null || !released.Succeeded)
                throw new InvalidOperationException("Fixture could not release the exact current preparation token.");
            if (TryGetPrepared(PreparationSnapshot, slotId))
                throw new InvalidOperationException("Fixture Actor cleanup retained prepared state.");
            LastPreparationResult = null;
        }

        private void ReleaseGameplayChain(PlayerSlotId slotId)
        {
            PlayerGameplayRuntimeHostSnapshot before = GameplaySnapshot;
            PlayerGameplayAdmissionSummary admission = default;
            bool hasAdmission = before.Admission != null &&
                before.Admission.TryGetSummary(slotId, out admission) &&
                admission.IsAdmitted;
            if (hasAdmission)
            {
                PlayerGameplayRuntimeOperationResult released = Invoke<PlayerGameplayRuntimeOperationResult>(
                    GameplayModule, "TryReleaseCurrentGameplay", slotId, admission.Token,
                    nameof(QaPlayerGameplayAdmissionFixture), "fixture-cleanup");
                if (released == null || !released.Succeeded)
                    throw new InvalidOperationException("Fixture could not release the exact current Gameplay admission token.");
            }
            PlayerGameplayRuntimeHostSnapshot after = GameplaySnapshot;
            if (after.Admission != null &&
                after.Admission.TryGetSummary(slotId, out PlayerGameplayAdmissionSummary afterAdmission) &&
                afterAdmission.IsAdmitted)
            {
                throw new InvalidOperationException(
                    "Fixture Gameplay cleanup retained the current Slot admission.");
            }
            LastGameplayReadyResult = null;
        }

        private void EnsureGameplayTerminalClean()
        {
            PlayerGameplayRuntimeHostSnapshot gameplay = GameplaySnapshot;
            if (gameplay.GameplayReadyCount != 0 ||
                (gameplay.CameraEligibility?.EligibleCount ?? 0) != 0 ||
                gameplay.BoundInputCount != 0 || gameplay.OccupiedCount != 0 ||
                gameplay.CandidateCount != 0 || gameplay.ActivePerSlotHandoffCount != 0 ||
                gameplay.HasActiveHandoffGroup)
            {
                throw new InvalidOperationException(
                    "Fixture Gameplay cleanup retained Admission, Camera, Input, Occupancy, candidate or handoff state.");
            }
        }

        private void EnsurePreparationTerminalClean()
        {
            if (PreparationSnapshot.PreparedCount != 0)
                throw new InvalidOperationException("Fixture Actor cleanup retained prepared state.");
        }

        private async Task<int> AwaitOwnedPhysicalHostsDestroyedAsync(
            IReadOnlyList<OwnedJoinTeardownEvidence> evidence)
        {
            const int maxFrames = 10;
            for (int frame = 0; frame <= maxFrames; frame++)
            {
                bool allDestroyed = true;
                for (int index = 0; index < evidence.Count; index++)
                {
                    OwnedJoinTeardownEvidence item = evidence[index];
                    if (item.PlayerInput != null || item.Host != null || item.GameObject != null)
                    {
                        allDestroyed = false;
                        break;
                    }
                }

                if (allDestroyed && PlayerCount == BaselinePlayerCount &&
                    RegisteredHostCount == BaselineRegisteredHostCount)
                    return frame;
                if (frame == maxFrames) break;
                await Awaitable.NextFrameAsync();
            }

            throw new InvalidOperationException(
                "Owned Local Player physical Hosts were not destroyed within '10' frames. " +
                $"managerPlayers='{PlayerCount}' registeredHosts='{RegisteredHostCount}' " +
                $"ownedJoins='{evidence.Count}'.");
        }

        private static bool TryGetPrepared(PlayerActorPreparationRuntimeHostSnapshot snapshot, PlayerSlotId slotId)
        {
            return TryGetPreparationSummary(snapshot, slotId, out PlayerActorPreparationSummary preparation) &&
                preparation.IsPrepared;
        }

        private static bool TryGetPreparationSummary(
            PlayerActorPreparationRuntimeHostSnapshot snapshot,
            PlayerSlotId slotId,
            out PlayerActorPreparationSummary preparation)
        {
            preparation = default;
            PlayerActorPreparationSnapshot preparationSnapshot = snapshot?.Preparation;
            if (preparationSnapshot == null) return false;

            for (int index = 0; index < preparationSnapshot.Slots.Count; index++)
            {
                PlayerActorPreparationSummary candidate = preparationSnapshot.Slots[index];
                if (candidate.PlayerSlotId != slotId) continue;

                preparation = candidate;
                return true;
            }

            return false;
        }
        private static async Task<object> InvokeTaskResultAsync(object target, string method, params object[] arguments)
        {
            object taskObject = Invoke(target, method, arguments);
            if (!(taskObject is Task task))
                throw new InvalidOperationException(
                    $"Reflection operation '{method}' on module '{target.GetType().FullName}' did not return a Task.");
            await task;
            PropertyInfo resultProperty = task.GetType().GetProperty("Result", InstanceAny);
            if (resultProperty == null)
                throw new InvalidOperationException(
                    $"Reflection operation '{method}' on module '{target.GetType().FullName}' returned a Task without Result.");
            return resultProperty.GetValue(task);
        }
    }

    internal sealed class OwnedJoinTeardownEvidence
    {
        internal OwnedJoinTeardownEvidence(LocalPlayerJoinResult join)
        {
            OperationId = join.OperationId;
            SlotId = join.Slot.PlayerSlotId;
            AssignmentToken = join.AssignmentToken;
            HostBindingIdentity = join.HostBindingIdentity;
            PlayerInput = join.PlayerInput;
            Host = join.LocalPlayerHost;
            GameObject = join.LocalPlayerHost != null
                ? join.LocalPlayerHost.gameObject
                : join.PlayerInput != null
                    ? join.PlayerInput.gameObject
                    : null;
        }

        internal LocalPlayerJoinOperationId OperationId { get; }
        internal PlayerSlotId SlotId { get; }
        internal PlayerSlotAssignmentToken AssignmentToken { get; }
        internal PlayerHostBindingIdentity HostBindingIdentity { get; }
        internal PlayerInput PlayerInput { get; }
        internal LocalPlayerHostAuthoring Host { get; }
        internal GameObject GameObject { get; }
    }

    public readonly struct TwoPlayerActorAuthoringEvidence
    {
        internal TwoPlayerActorAuthoringEvidence(
            PlayerActorSelectionDuplicatePolicy policy,
            PlayerSlotId firstSlotId,
            PlayerSlotProfile firstSlotProfile,
            ActorProfile firstActorProfile,
            PlayerSlotId secondSlotId,
            PlayerSlotProfile secondSlotProfile,
            ActorProfile secondActorProfile)
        {
            Policy = policy;
            FirstSlotId = firstSlotId;
            FirstSlotProfile = firstSlotProfile;
            FirstActorProfile = firstActorProfile;
            SecondSlotId = secondSlotId;
            SecondSlotProfile = secondSlotProfile;
            SecondActorProfile = secondActorProfile;
        }

        public PlayerActorSelectionDuplicatePolicy Policy { get; }
        public PlayerSlotId FirstSlotId { get; }
        public PlayerSlotProfile FirstSlotProfile { get; }
        public ActorProfile FirstActorProfile { get; }
        public ActorProfileId FirstActorProfileId => FirstActorProfile.ActorProfileId;
        public PlayerSlotId SecondSlotId { get; }
        public PlayerSlotProfile SecondSlotProfile { get; }
        public ActorProfile SecondActorProfile { get; }
        public ActorProfileId SecondActorProfileId => SecondActorProfile.ActorProfileId;
        public bool ProfilesDistinct =>
            !ReferenceEquals(FirstActorProfile, SecondActorProfile) &&
            FirstActorProfileId != SecondActorProfileId;
        public bool FirstMaterializable =>
            FirstActorProfile != null && FirstActorProfile.HasLogicalActorHostPrefab;
        public bool SecondMaterializable =>
            SecondActorProfile != null && SecondActorProfile.HasLogicalActorHostPrefab;
    }

    public sealed class QaPlayerJoinEvidence
    {
        internal QaPlayerJoinEvidence(
            LocalPlayerJoinResult joinResult,
            PlayerSlotId slotId,
            LocalPlayerHostAuthoring host,
            object playerInput,
            int playerIndex,
            string playerInputDiagnostic,
            string hostDiagnostic)
        {
            JoinResult = joinResult;
            SlotId = slotId;
            Host = host;
            PlayerInput = playerInput;
            PlayerIndex = playerIndex;
            PlayerInputDiagnostic = playerInputDiagnostic ?? string.Empty;
            HostDiagnostic = hostDiagnostic ?? string.Empty;
        }

        public LocalPlayerJoinResult JoinResult { get; }
        public PlayerSlotId SlotId { get; }
        public LocalPlayerHostAuthoring Host { get; }
        public object PlayerInput { get; }
        public int PlayerIndex { get; }
        public string PlayerInputDiagnostic { get; }
        public string HostDiagnostic { get; }
    }
}

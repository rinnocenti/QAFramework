using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Consolidated Play Mode conformance matrix for ADR 019 Session lifetime invariants.
    ///
    /// The readiness boundary proof keeps Session membership/selection independent from
    /// Activity Actor representation until LogicalActorsPrepared is required.
    /// B proves that Session Join remains authoritative while gameplay occupancy is absent.
    /// C proves that an already Joined Slot is not reused by a second official Join.
    /// E proves that Activity exit does not imply Player Leave and does not destroy the
    /// Session-owned Manager-Provisioned technical Host.
    /// D proves that Session termination removes the host-scoped participation authority
    /// and its Session-owned physical Player resources.
    ///
    /// D intentionally runs last because destroying FrameworkRuntimeHost is the actual
    /// Session lifetime boundary and therefore ends the current QA runtime session.
    /// </summary>
    internal static class QaAdr19SessionLifetimeMatrixRegression
    {
        private const string RuntimeHostTypeName =
            "Immersive.Framework.ApplicationLifecycle.FrameworkRuntimeHost";
        private const string ParticipationModuleTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerParticipationRuntimeHostModule";
        private const string ProvisioningModuleTypeName =
            "Immersive.Framework.PlayerParticipation.LocalPlayerProvisioningRuntimeHostModule";

        private const string MenuPath =
            "Immersive Framework/QA/Player/Session/ADR 19/Run Session Lifetime Matrix";

        [MenuItem(MenuPath)]
        private static async void RunRegression()
        {
            var completed = new List<string>();

            try
            {
                AssertTrue(
                    EditorApplication.isPlaying,
                    "ADR 19 Session Lifetime Matrix must run in Play Mode.");

                AssertReadinessRepresentationBoundary();
                completed.Add("readiness-representation-boundary");

                await RunJoinedWithoutGameplayOccupancyAsync();
                completed.Add("19.1B-joined-without-gameplay-occupancy");

                await RunJoinedSlotNotReusedAsync();
                completed.Add("19.1C-joined-slot-not-reused");

                // Activity exit mutates the current Activity, so it runs after the
                // non-destructive Session cases and immediately before termination.
                await RunActivityExitPreservesParticipationAsync();
                completed.Add("19.1E-activity-exit-preserves-participation-and-host");

                // Session termination is destructive by definition. It must be last.
                await RunSessionTerminationClearsParticipationAsync();
                completed.Add("19.1D-session-termination-clears-participation-and-host");

                Debug.Log(
                    "[ADR19_SESSION_LIFETIME_MATRIX] status='Passed' " +
                    $"cases='{completed.Count}' completed='{string.Join(",", completed)}' " +
                    "executionOrder='readiness,B,C,E,D' sessionTerminated='True' " +
                    "nextAction='Exit and re-enter Play Mode before running another runtime smoke'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[ADR19_SESSION_LIFETIME_MATRIX] status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
        }

        private static void AssertReadinessRepresentationBoundary()
        {
            ConstructorInfo constructor = ResolveLifecycleSnapshotConstructor();
            (PlayerParticipationRequirementLevel Level, bool RequiresRepresentation)[] cases =
            {
                (PlayerParticipationRequirementLevel.None, false),
                (PlayerParticipationRequirementLevel.JoinedSlots, false),
                (PlayerParticipationRequirementLevel.SelectedActors, false),
                (PlayerParticipationRequirementLevel.LogicalActorsPrepared, true),
                (PlayerParticipationRequirementLevel.GameplayReady, true)
            };

            for (int index = 0; index < cases.Length; index++)
            {
                PlayerParticipationRequirementLevel level = cases[index].Level;
                bool expected = cases[index].RequiresRepresentation;
                var snapshot = (ActivityPlayerActorLifecycleSnapshot)constructor.Invoke(
                    new object[]
                    {
                        ActivityPlayerActorLifecycleStatus.SucceededEnteredNoParticipants,
                        "ADR19 Readiness Boundary QA",
                        default(RuntimeContentOwner),
                        level,
                        0,
                        0,
                        0,
                        0,
                        0,
                        Array.Empty<ActivityPlayerActorSlotLifecycleSnapshot>(),
                        "readiness-boundary-probe"
                    });

                AssertEqual(
                    expected,
                    snapshot.RequiresActivityActorRepresentation,
                    $"ADR19 readiness boundary is incorrect for requirement '{level}'.");

                string expectedDiagnostic =
                    $"requiresActivityActorRepresentation='{expected}'";
                AssertTrue(
                    snapshot.ToDiagnosticString().Contains(
                        expectedDiagnostic,
                        StringComparison.Ordinal),
                    $"ADR19 lifecycle diagnostics do not expose the representation boundary for '{level}'. " +
                    snapshot.ToDiagnosticString());

                IReadOnlyList<PlayerParticipationReadinessEvidence> requiredEvidence =
                    PlayerParticipationReadinessRequirements.GetRequiredEvidence(level);
                bool requiresLogicalActorPreparation = false;
                for (int evidenceIndex = 0;
                     evidenceIndex < requiredEvidence.Count;
                     evidenceIndex++)
                {
                    if (requiredEvidence[evidenceIndex] ==
                        PlayerParticipationReadinessEvidence.LogicalActorPrepared)
                    {
                        requiresLogicalActorPreparation = true;
                        break;
                    }
                }

                AssertEqual(
                    expected,
                    requiresLogicalActorPreparation,
                    $"ADR19 representation boundary diverged from canonical readiness evidence for '{level}'.");
            }

            Debug.Log(
                "[ADR19_READINESS_REPRESENTATION_BOUNDARY] status='Passed' " +
                "none='SessionOnly' joinedSlots='SessionOnly' selectedActors='SessionOnly' " +
                "logicalActorsPrepared='ActivityRepresentationRequired' " +
                "gameplayReady='ActivityRepresentationRequired'.");
        }

        private static ConstructorInfo ResolveLifecycleSnapshotConstructor()
        {
            ConstructorInfo[] constructors =
                typeof(ActivityPlayerActorLifecycleSnapshot).GetConstructors(
                    BindingFlags.Instance | BindingFlags.NonPublic);
            for (int index = 0; index < constructors.Length; index++)
            {
                if (constructors[index].GetParameters().Length == 11)
                {
                    return constructors[index];
                }
            }

            throw new InvalidOperationException(
                "ADR19 QA could not resolve the internal ActivityPlayerActorLifecycleSnapshot constructor.");
        }

        private static async Task RunJoinedWithoutGameplayOccupancyAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;

            try
            {
                fixture = await CreateFreshFixtureAsync("19.1B");
                EnsureJoiningOpen(fixture, "adr19-19.1b-joined-without-gameplay-occupancy");

                LocalPlayerJoinResult joined = fixture.JoinPlayer(
                    "adr19-19.1b-joined-without-gameplay-occupancy");
                AssertSuccessfulJoin(joined, "19.1B");

                PlayerSlotId slotId = joined.Slot.PlayerSlotId;
                PlayerParticipationSnapshot participation = fixture.ParticipationSnapshot;
                PlayerSlotRuntimeSnapshot slot = fixture.GetParticipationSlot(slotId);
                PlayerGameplayRuntimeHostSnapshot gameplay = fixture.GameplaySnapshot;

                AssertTrue(slot.IsJoined,
                    "19.1B expected the official Session Slot to remain Joined.");
                AssertEqual(PlayerSlotAllocationState.Joined, slot.AllocationState,
                    "19.1B expected Joined allocation state.");
                AssertEqual(1, participation.JoinedCount,
                    "19.1B expected exactly one Joined Session Player.");

                AssertGameplayOccupancyAbsent(gameplay, "19.1B");

                PlayerParticipationSnapshot afterOccupancyObservation =
                    fixture.Provisioning.RuntimeSnapshot;
                PlayerSlotRuntimeSnapshot afterSlot =
                    FindSlot(afterOccupancyObservation, slotId, "19.1B");

                AssertTrue(afterSlot.IsJoined,
                    "19.1B observing absent gameplay occupancy changed Session Join truth.");
                AssertEqual(participation.JoinedCount, afterOccupancyObservation.JoinedCount,
                    "19.1B absent gameplay occupancy changed JoinedCount.");

                Debug.Log(
                    "[ADR19_1B_JOINED_WITHOUT_GAMEPLAY_OCCUPANCY] status='Passed' " +
                    $"slot='{slotId.StableText}' joined='{afterOccupancyObservation.JoinedCount}' " +
                    $"occupied='{gameplay.OccupiedCount}' gameplayReady='{gameplay.GameplayReadyCount}' " +
                    $"inputBound='{gameplay.BoundInputCount}' proof='Session Join is independent from gameplay occupancy'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                executionFailure = await CleanupFixtureAsync(
                    fixture,
                    "19.1B",
                    executionFailure);
            }

            RethrowCaseFailure("19.1B", executionFailure);
        }

        private static async Task RunJoinedSlotNotReusedAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;

            try
            {
                fixture = await CreateFreshFixtureAsync("19.1C");
                EnsureJoiningOpen(fixture, "adr19-19.1c-joined-slot-not-reused");

                LocalPlayerJoinResult primary = fixture.JoinPlayer(
                    "adr19-19.1c-primary-join");
                AssertSuccessfulJoin(primary, "19.1C primary");

                PlayerSlotId primarySlotId = primary.Slot.PlayerSlotId;
                AssertGameplayOccupancyAbsent(fixture.GameplaySnapshot, "19.1C before secondary Join");
                AssertTrue(fixture.GetParticipationSlot(primarySlotId).IsJoined,
                    "19.1C primary Slot was not Joined before secondary Join.");

                // The contract under proof is Slot retention, not availability of a second
                // unpaired physical input device. Reuse the primary device as an explicit
                // QA provisioning hint so the second official Join reaches Session allocation
                // while the fixture still proves that the primary Player/Host remains intact.
                LocalPlayerJoinResult secondary = fixture.JoinAdditionalPlayerSharingPrimaryDevice(
                    "adr19-19.1c-secondary-join");
                AssertSuccessfulJoin(secondary, "19.1C secondary");

                PlayerSlotId secondarySlotId = secondary.Slot.PlayerSlotId;
                PlayerParticipationSnapshot participation = fixture.ParticipationSnapshot;

                AssertTrue(primarySlotId != secondarySlotId,
                    "19.1C official Join reused the already Joined primary Slot.");
                AssertTrue(fixture.GetParticipationSlot(primarySlotId).IsJoined,
                    "19.1C primary Slot stopped being Joined after secondary Join.");
                AssertTrue(fixture.GetParticipationSlot(secondarySlotId).IsJoined,
                    "19.1C secondary Slot was not committed as Joined.");
                AssertEqual(2, participation.JoinedCount,
                    "19.1C expected two distinct Joined Slots after two official Joins.");
                AssertGameplayOccupancyAbsent(fixture.GameplaySnapshot, "19.1C after secondary Join");

                Debug.Log(
                    "[ADR19_1C_JOINED_SLOT_NOT_REUSED] status='Passed' " +
                    $"primarySlot='{primarySlotId.StableText}' secondarySlot='{secondarySlotId.StableText}' " +
                    $"joined='{participation.JoinedCount}' occupied='{fixture.GameplaySnapshot.OccupiedCount}' " +
                    "secondaryProvisioning='shared-primary-device-hint' " +
                    "proof='absence of gameplay occupancy does not make a Joined Session Slot reusable'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                executionFailure = await CleanupFixtureAsync(
                    fixture,
                    "19.1C",
                    executionFailure);
            }

            RethrowCaseFailure("19.1C", executionFailure);
        }

        private static async Task RunActivityExitPreservesParticipationAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;

            try
            {
                fixture = await CreateFreshFixtureAsync("19.1E");
                AssertNotNull(fixture.CurrentActivity,
                    "19.1E requires one current Activity so ClearActivityAsync represents a real Activity exit.");

                string activityName = fixture.CurrentActivity.name;
                EnsureJoiningOpen(fixture, "adr19-19.1e-activity-exit-preserves-participation");

                LocalPlayerJoinResult joined = fixture.JoinPlayer(
                    "adr19-19.1e-activity-exit-preserves-participation");
                AssertSuccessfulJoin(joined, "19.1E");

                PlayerSlotId slotId = joined.Slot.PlayerSlotId;
                PlayerParticipationSnapshot beforeExit = fixture.ParticipationSnapshot;
                PlayerSlotRuntimeSnapshot slotBeforeExit =
                    fixture.GetParticipationSlot(slotId);
                Component runtimeHost = fixture.RuntimeHost as Component;
                var sessionPlayerInput = joined.PlayerInput;
                LocalPlayerHostAuthoring sessionLocalPlayerHost = joined.LocalPlayerHost;

                AssertTrue(slotBeforeExit.IsJoined,
                    "19.1E expected the Player to be Joined before Activity exit.");
                AssertNotNull(runtimeHost,
                    "19.1E FrameworkRuntimeHost is unavailable before Activity exit.");
                AssertNotNull(sessionPlayerInput,
                    "19.1E successful Manager-Provisioned Join has no PlayerInput.");
                AssertNotNull(sessionLocalPlayerHost,
                    "19.1E successful Manager-Provisioned Join has no LocalPlayerHostAuthoring.");
                AssertTrue(
                    sessionPlayerInput.transform.IsChildOf(runtimeHost.transform),
                    "19.1E Manager-Provisioned PlayerInput did not enter FrameworkRuntimeHost Session lifetime.");

                await fixture.ClearActivityAsync(
                    nameof(QaAdr19SessionLifetimeMatrixRegression),
                    "adr19-19.1e-activity-exit");

                AssertTrue(fixture.CurrentActivity == null,
                    "19.1E ClearActivityAsync did not exit the current Activity.");

                PlayerParticipationSnapshot afterExit = fixture.Provisioning.RuntimeSnapshot;
                PlayerSlotRuntimeSnapshot slotAfterExit =
                    FindSlot(afterExit, slotId, "19.1E");

                AssertTrue(afterExit.IsInitialized,
                    "19.1E Activity exit disposed Session participation authority.");
                AssertEqual(beforeExit.ContextId, afterExit.ContextId,
                    "19.1E Activity exit replaced the Session participation context.");
                AssertEqual(beforeExit.JoinedCount, afterExit.JoinedCount,
                    "19.1E Activity exit changed Session JoinedCount.");
                AssertTrue(slotAfterExit.IsJoined,
                    "19.1E Activity exit implicitly performed Player Leave.");
                AssertEqual(PlayerSlotAllocationState.Joined, slotAfterExit.AllocationState,
                    "19.1E Activity exit changed the Joined Slot allocation state.");
                AssertTrue(sessionPlayerInput != null,
                    "19.1E Activity exit destroyed the Session-owned Manager-Provisioned PlayerInput.");
                AssertTrue(sessionLocalPlayerHost != null,
                    "19.1E Activity exit destroyed the Session-owned Manager-Provisioned Local Player Host.");
                AssertTrue(
                    sessionPlayerInput.transform.IsChildOf(runtimeHost.transform),
                    "19.1E Activity exit moved the Manager-Provisioned PlayerInput out of Session ownership.");
                AssertTrue(
                    ReferenceEquals(
                        sessionPlayerInput.GetComponent<LocalPlayerHostAuthoring>(),
                        sessionLocalPlayerHost),
                    "19.1E Activity exit replaced the Manager-Provisioned Local Player Host occurrence.");

                Debug.Log(
                    "[ADR19_1E_ACTIVITY_EXIT_PRESERVES_PARTICIPATION] status='Passed' " +
                    $"activity='{Escape(activityName)}' slot='{slotId.StableText}' " +
                    $"sessionContext='{afterExit.ContextId}' joined='{afterExit.JoinedCount}' " +
                    "playerInputAlive='True' hostAlive='True' currentActivity='<none>' " +
                    "proof='Activity exit is not Player Leave and does not release Session-owned Manager-Provisioned Host resources'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                // The Activity exit is the operation under proof and is intentionally not
                // reverted. Join ownership is still cleaned through the canonical fixture.
                executionFailure = await CleanupFixtureAsync(
                    fixture,
                    "19.1E",
                    executionFailure);
            }

            RethrowCaseFailure("19.1E", executionFailure);
        }

        private static async Task RunSessionTerminationClearsParticipationAsync()
        {
            QaPlayerGameplayAdmissionFixture fixture = null;

            try
            {
                fixture = await CreateFreshFixtureAsync("19.1D");
                EnsureJoiningOpen(fixture, "adr19-19.1d-session-termination");

                LocalPlayerJoinResult joined = fixture.JoinPlayer(
                    "adr19-19.1d-session-termination");
                AssertSuccessfulJoin(joined, "19.1D");

                PlayerParticipationSnapshot beforeTermination = fixture.ParticipationSnapshot;
                AssertEqual(1, beforeTermination.JoinedCount,
                    "19.1D requires one Joined Session Player before termination.");

                Component runtimeHost = fixture.RuntimeHost as Component;
                AssertNotNull(runtimeHost,
                    "19.1D FrameworkRuntimeHost is not a Unity Component.");
                AssertEqual(RuntimeHostTypeName, runtimeHost.GetType().FullName,
                    "19.1D resolved an unexpected Session host type.");

                Type participationModuleType = ResolveRuntimeType(ParticipationModuleTypeName);
                Type provisioningModuleType = ResolveRuntimeType(ProvisioningModuleTypeName);
                Component participationModule = runtimeHost.GetComponent(participationModuleType);
                Component provisioningModule = runtimeHost.GetComponent(provisioningModuleType);

                AssertNotNull(participationModule,
                    "19.1D FrameworkRuntimeHost has no host-scoped participation module before termination.");
                AssertNotNull(provisioningModule,
                    "19.1D FrameworkRuntimeHost has no host-scoped provisioning module before termination.");

                LocalPlayerProvisioningAuthoring authoring = fixture.Provisioning;
                GameObject sessionHostObject = runtimeHost.gameObject;
                var sessionPlayerInput = joined.PlayerInput;
                LocalPlayerHostAuthoring sessionLocalPlayerHost = joined.LocalPlayerHost;

                AssertNotNull(sessionPlayerInput,
                    "19.1D successful Manager-Provisioned Join has no PlayerInput before Session termination.");
                AssertNotNull(sessionLocalPlayerHost,
                    "19.1D successful Manager-Provisioned Join has no LocalPlayerHostAuthoring before Session termination.");
                AssertTrue(
                    sessionPlayerInput.transform.IsChildOf(runtimeHost.transform),
                    "19.1D Manager-Provisioned PlayerInput is not owned by the Session runtime before termination.");

                // ADR 019 defines Session lifetime at FrameworkRuntimeHost scope. Destroying
                // that host is therefore the real termination boundary; this intentionally
                // does not call RollbackCommittedJoin or treat Activity exit as Leave.
                UnityEngine.Object.Destroy(sessionHostObject);
                await Awaitable.NextFrameAsync();
                await Awaitable.NextFrameAsync();

                AssertTrue(runtimeHost == null,
                    "19.1D FrameworkRuntimeHost survived Session termination.");
                AssertTrue(participationModule == null,
                    "19.1D host-scoped Session participation module survived FrameworkRuntimeHost termination.");
                AssertTrue(provisioningModule == null,
                    "19.1D host-scoped Local Player provisioning module survived FrameworkRuntimeHost termination.");
                AssertTrue(sessionPlayerInput == null,
                    "19.1D Session-owned Manager-Provisioned PlayerInput survived Session termination.");
                AssertTrue(sessionLocalPlayerHost == null,
                    "19.1D Session-owned Manager-Provisioned Local Player Host survived Session termination.");
                AssertEqual(0, CountLoadedSceneComponents(participationModuleType),
                    "19.1D found a surviving loaded Session participation authority after FrameworkRuntimeHost termination.");

                string endpointEvidence;
                if (authoring != null)
                {
                    AssertTrue(!authoring.RuntimeReady,
                        "19.1D Local Player provisioning endpoint remained RuntimeReady after Session termination.");

                    PlayerParticipationSnapshot afterTermination = authoring.RuntimeSnapshot;
                    AssertTrue(!afterTermination.IsInitialized,
                        "19.1D public participation snapshot remained initialized after Session termination.");
                    AssertEqual(0, afterTermination.JoinedCount,
                        "19.1D public participation endpoint retained Joined Session state after termination.");
                    AssertEqual(0, afterTermination.ConfiguredSlotCount,
                        "19.1D public participation endpoint retained Session Slot state after termination.");
                    endpointEvidence =
                        "authoring-survived-runtime-unbound-empty-participation";
                }
                else
                {
                    endpointEvidence =
                        "authoring-destroyed-with-session-host";
                }

                Debug.Log(
                    "[ADR19_1D_SESSION_TERMINATION_CLEARS_PARTICIPATION] status='Passed' " +
                    $"joinedBefore='{beforeTermination.JoinedCount}' " +
                    "participationAuthoritiesAfter='0' playerInputAlive='False' hostAlive='False' " +
                    $"endpointEvidence='{endpointEvidence}' " +
                    "proof='Session termination removes Session participation authority and Session-owned Manager-Provisioned physical resources without using Player Leave rollback'.");

                // Do not call fixture cleanup. Session termination is the cleanup boundary under test,
                // and the fixture's rollback path would be both unavailable and semantically incorrect.
                fixture = null;
            }
            catch (Exception exception)
            {
                // If termination has not happened yet, preserve normal fixture hygiene. Once the
                // runtime host is gone, cleanup cannot and should not emulate a Session rollback.
                if (fixture != null && fixture.RuntimeHost is Component runtimeHost && runtimeHost != null)
                {
                    await fixture.CleanupAsync();
                    if (fixture.CleanupFailure != null)
                    {
                        throw new AggregateException(
                            "19.1D execution and fixture cleanup both failed.",
                            exception,
                            fixture.CleanupFailure);
                    }
                }

                throw;
            }
        }

        private static async Task<QaPlayerGameplayAdmissionFixture> CreateFreshFixtureAsync(
            string caseName)
        {
            QaPlayerGameplayAdmissionFixture fixture =
                await QaPlayerGameplayAdmissionFixture.CreateAsync();

            LocalPlayerProvisioningAuthoring provisioning = fixture.Provisioning;
            AssertNotNull(provisioning,
                $"{caseName} fixture has no LocalPlayerProvisioningAuthoring.");
            AssertTrue(provisioning.RuntimeReady,
                $"{caseName} Local Player provisioning runtime is not ready. " +
                provisioning.RuntimeDiagnostic);

            PlayerParticipationSnapshot participation = provisioning.RuntimeSnapshot;
            AssertTrue(participation.IsInitialized,
                $"{caseName} Session participation snapshot is not initialized.");
            AssertTrue(participation.ConfiguredSlotCount > 0,
                $"{caseName} Session has no configured Player Slots.");
            AssertEqual(0, fixture.BaselinePlayerCount,
                $"{caseName} requires a fresh QA runtime with zero physical Players. Exit and re-enter Play Mode, then run this matrix before other Player smokes.");
            AssertEqual(0, fixture.BaselineJoinedSlotCount,
                $"{caseName} requires a fresh QA runtime with zero Joined Session Players.");

            AssertGameplayOccupancyAbsent(fixture.GameplaySnapshot,
                $"{caseName} baseline");
            return fixture;
        }

        private static void EnsureJoiningOpen(
            QaPlayerGameplayAdmissionFixture fixture,
            string reason)
        {
            PlayerParticipationOperationResult result = fixture.OpenJoining(reason);
            AssertNotNull(result,
                "Opening Session joining returned no result.");
            AssertTrue(result.Completed && result.Snapshot != null &&
                       result.Snapshot.JoiningOpen,
                "Opening Session joining failed. " + result.ToDiagnosticString());
        }

        private static void AssertSuccessfulJoin(
            LocalPlayerJoinResult join,
            string caseName)
        {
            AssertNotNull(join,
                $"{caseName} official Local Player Join returned no result.");
            AssertTrue(join.Succeeded,
                $"{caseName} official Local Player Join failed. " +
                join.ToDiagnosticString());
            AssertTrue(join.Slot.PlayerSlotId.IsValid,
                $"{caseName} successful Join returned an invalid PlayerSlotId.");
        }

        private static void AssertGameplayOccupancyAbsent(
            PlayerGameplayRuntimeHostSnapshot gameplay,
            string caseName)
        {
            AssertNotNull(gameplay,
                $"{caseName} Player Gameplay snapshot is unavailable.");
            AssertTrue(gameplay.IsInitialized,
                $"{caseName} Player Gameplay authority is not initialized.");
            AssertEqual(0, gameplay.GameplayReadyCount,
                $"{caseName} unexpectedly has Gameplay Ready admission evidence.");
            AssertEqual(0, gameplay.OccupiedCount,
                $"{caseName} unexpectedly has gameplay occupancy evidence.");
            AssertEqual(0, gameplay.BoundInputCount,
                $"{caseName} unexpectedly has gameplay input binding evidence.");
        }

        private static PlayerSlotRuntimeSnapshot FindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId slotId,
            string caseName)
        {
            AssertNotNull(snapshot,
                $"{caseName} participation snapshot is unavailable.");
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                PlayerSlotRuntimeSnapshot slot = snapshot.Slots[index];
                if (slot.PlayerSlotId == slotId)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException(
                $"{caseName} Slot '{slotId.StableText}' is absent from the Session participation snapshot.");
        }

        private static async Task<Exception> CleanupFixtureAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            string caseName,
            Exception executionFailure)
        {
            if (fixture == null)
            {
                return executionFailure;
            }

            await fixture.CleanupAsync();
            Exception cleanupFailure = fixture.CleanupFailure;
            if (cleanupFailure == null)
            {
                return executionFailure;
            }

            if (executionFailure == null)
            {
                return new InvalidOperationException(
                    $"{caseName} fixture cleanup failed. {cleanupFailure.Message}",
                    cleanupFailure);
            }

            return new AggregateException(
                $"{caseName} execution and fixture cleanup both failed.",
                executionFailure,
                cleanupFailure);
        }

        private static void RethrowCaseFailure(
            string caseName,
            Exception failure)
        {
            if (failure != null)
            {
                throw new InvalidOperationException(
                    $"ADR 19 {caseName} failed. {failure.Message}",
                    failure);
            }
        }

        private static Type ResolveRuntimeType(string fullName)
        {
            Type type = typeof(PlayerParticipationSnapshot).Assembly.GetType(
                fullName,
                throwOnError: false);
            return type ?? throw new InvalidOperationException(
                $"Runtime type '{fullName}' was not found.");
        }

        private static int CountLoadedSceneComponents(Type componentType)
        {
            UnityEngine.Object[] candidates =
                Resources.FindObjectsOfTypeAll(componentType);
            int count = 0;

            for (int index = 0; index < candidates.Length; index++)
            {
                if (!(candidates[index] is Component component) ||
                    component == null ||
                    EditorUtility.IsPersistent(component))
                {
                    continue;
                }

                UnityEngine.SceneManagement.Scene scene = component.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded)
                {
                    count++;
                }
            }

            return count;
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
                : value.Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
        }
    }
}

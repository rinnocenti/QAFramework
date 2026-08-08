using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.Bootstrap;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.PlayerAssignment.Internal.Editor
{
    /// <summary>
    /// Edit Mode technical smoke for IF-SESSION-CONFIG-05 — Player Session
    /// Runtime Integration. Proves that authored PlayerSessionProfile intent is
    /// resolved once into immutable EffectivePlayerSessionConfiguration and
    /// applied exactly once into the existing Session Player authority.
    /// </summary>
    /// <remarks>
    /// PUBLIC surfaces are preferred. Where ADR-015 has not yet exposed a
    /// consumer-scoped Session authority entrypoint, preparation is marked as
    /// INTERNAL TECHNICAL SETUP and does not promote package internals into
    /// product API.
    /// <para>
    /// Not covered here: Route/Activity does not reapply Session structural
    /// configuration remains not directly certified by this smoke and must be
    /// proven by a later appropriate Play Mode / integration surface that can
    /// exercise real Route/Activity transitions without invalid Edit Mode host
    /// creation (DontDestroyOnLoad / FrameworkRuntimeHost.Create).
    /// Broader ADR-016 contract matrix closure lives in
    /// <see cref="QaIfSessionConfig07PlayerSessionContractClosureSmoke"/>
    /// (IF-SESSION-CONFIG-07) without re-running these six cases.
    /// </para>
    /// </remarks>
    internal static class QaIfSessionConfig05PlayerSessionRuntimeIntegrationSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/" +
            "Run IF-SESSION-CONFIG-05 Player Session Runtime Integration Smoke";

        private const string LogPrefix =
            "[IF_SESSION_CONFIG_05_PLAYER_SESSION_RUNTIME_INTEGRATION]";

        private const string Source =
            "QA.IF-SESSION-CONFIG-05";

        private static readonly string[] CaseIds =
        {
            "01-disabled-valid-absence",
            "02-enabled-missing-profile",
            "03-manager-only-ordered-allocation",
            "04-mixed-scene-then-manager",
            "05-profile-edit-after-init-frozen",
            "06-leave-unresolved-no-auto-actor"
        };

        [MenuItem(MenuPath)]
        internal static void Run()
        {
            var created = new List<UnityEngine.Object>();
            var results = new List<CaseResult>(CaseIds.Length);

            try
            {
                Require(
                    !EditorApplication.isPlayingOrWillChangePlaymode,
                    "IF-SESSION-CONFIG-05 smoke must run in Edit Mode.");

                results.Add(RunCase(
                    CaseIds[0],
                    () => Case01DisabledValidAbsence(created)));
                results.Add(RunCase(
                    CaseIds[1],
                    () => Case02EnabledMissingProfile(created)));
                results.Add(RunCase(
                    CaseIds[2],
                    () => Case03ManagerOnlyOrderedAllocation(created)));
                results.Add(RunCase(
                    CaseIds[3],
                    () => Case04MixedSceneThenManager(created)));
                results.Add(RunCase(
                    CaseIds[4],
                    () => Case05ProfileEditAfterInitFrozen(created)));
                results.Add(RunCase(
                    CaseIds[5],
                    () => Case06LeaveUnresolvedNoAutoActor(created)));

                Require(
                    results.Count == CaseIds.Length,
                    "IF-SESSION-CONFIG-05 case count changed unexpectedly.");

                bool allPassed = true;
                var summary = new StringBuilder();
                for (int index = 0; index < results.Count; index++)
                {
                    CaseResult result = results[index];
                    if (!result.Passed)
                    {
                        allPassed = false;
                    }

                    if (index > 0)
                    {
                        summary.Append(';');
                    }

                    summary.Append(result.CaseId)
                        .Append('=')
                        .Append(result.Passed ? "PASS" : "FAIL");
                    if (!result.Passed &&
                        !string.IsNullOrEmpty(result.Detail))
                    {
                        summary.Append('(')
                            .Append(Escape(result.Detail))
                            .Append(')');
                    }
                }

                if (allPassed)
                {
                    Debug.Log(
                        $"{LogPrefix} status='PASS' cases='{results.Count}' " +
                        $"results='{summary}'.");
                }
                else
                {
                    Debug.LogError(
                        $"{LogPrefix} status='FAIL' cases='{results.Count}' " +
                        $"results='{summary}'.");
                    throw new InvalidOperationException(
                        "IF-SESSION-CONFIG-05 smoke failed one or more cases. " +
                        summary);
                }
            }
            catch (Exception exception)
            {
                if (!(exception is InvalidOperationException) ||
                    results.Count == 0)
                {
                    Debug.LogError(
                        $"{LogPrefix} status='FAIL' " +
                        $"exception='{exception.GetType().Name}' " +
                        $"message='{Escape(exception.Message)}'.");
                }

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

        // ------------------------------------------------------------------
        // Cases
        // ------------------------------------------------------------------

        /// <summary>
        /// PUBLIC: disabled GameApplication is a valid absence of Player
        /// Session. Bootstrap composition is skipped.
        /// </summary>
        private static void Case01DisabledValidAbsence(
            ICollection<UnityEngine.Object> created)
        {
            GameApplicationAsset gameApplication = CreateGameApplication(
                created,
                playerSessionEnabled: false,
                defaultPlayerSessionProfile: null);

            Require(
                !gameApplication.PlayerSessionEnabled,
                "Disabled Game Application still reports Player Session enabled.");
            Require(
                gameApplication.DefaultPlayerSessionProfile == null,
                "Disabled Game Application unexpectedly exposes a Default Player Session Profile.");

            // INTERNAL TECHNICAL SETUP: bootstrap composition gate is internal.
            Require(
                !ImmersiveFrameworkBootstrap.ShouldComposePlayerParticipationRuntime(
                    gameApplication),
                "Disabled Player Session still requested participation composition.");
        }

        /// <summary>
        /// PUBLIC: enabled Session without Profile fails typed resolution with
        /// MissingRequiredConfiguration. Bootstrap must not compose on that path.
        /// </summary>
        private static void Case02EnabledMissingProfile(
            ICollection<UnityEngine.Object> created)
        {
            GameApplicationAsset gameApplication = CreateGameApplication(
                created,
                playerSessionEnabled: true,
                defaultPlayerSessionProfile: null);

            Require(
                gameApplication.PlayerSessionEnabled,
                "Enabled Game Application did not report Player Session enabled.");
            Require(
                gameApplication.DefaultPlayerSessionProfile == null,
                "Enabled-without-Profile fixture still has a Default Profile.");

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(
                    gameApplication.DefaultPlayerSessionProfile);

            Require(resolution != null, "Resolver returned null.");
            Require(resolution.Failed, "Missing Profile resolution succeeded.");
            Require(
                resolution.Failure ==
                    PlayerSessionInitializationFailure.MissingRequiredConfiguration,
                "Missing Profile did not report MissingRequiredConfiguration. " +
                $"actual='{resolution.Failure}' message='{resolution.Message}'.");
            Require(
                resolution.Configuration == null,
                "Failed resolution still produced an effective configuration.");

            // INTERNAL TECHNICAL SETUP: composition gate remains internal.
            Require(
                ImmersiveFrameworkBootstrap.ShouldComposePlayerParticipationRuntime(
                    gameApplication),
                "Enabled Player Session did not request composition.");
        }

        /// <summary>
        /// PUBLIC resolve + INTERNAL Session authority: homogeneous
        /// ManagerProvisioned allocates P1 then P2 in authored order.
        /// </summary>
        private static void Case03ManagerOnlyOrderedAllocation(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile playerOne = CreateSlotProfile(
                created, "qa.session05.p1", "P1", null);
            PlayerSlotProfile playerTwo = CreateSlotProfile(
                created, "qa.session05.p2", "P2", null);
            PlayerProvisioningProfile provisioning = CreateProvisioningProfile(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile sessionProfile = CreateSessionProfile(
                created,
                new[] { playerOne, playerTwo },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(sessionProfile);
            RequireSucceededResolution(resolution, "Manager-only resolution");
            EffectivePlayerSessionConfiguration configuration =
                resolution.Configuration;
            Require(
                configuration.SupportedSlotCount == 2 &&
                configuration.Slots[0].PlayerSlotId.Equals(playerOne.PlayerSlotId) &&
                configuration.Slots[1].PlayerSlotId.Equals(playerTwo.PlayerSlotId),
                "Manager-only effective Slot order changed.");
            Require(
                configuration.Slots[0].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned &&
                configuration.Slots[1].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned,
                "Manager-only effective Host provisioning changed.");

            // INTERNAL TECHNICAL SETUP: Session authority creation is not a
            // public consumer surface (ADR-015 dependency).
            PlayerParticipationRuntimeContext context =
                CreateContextFromEffective(configuration);

            PlayerParticipationOperationResult first =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "manager-first");
            RequireStatus(
                first,
                PlayerParticipationOperationStatus.Succeeded,
                "Manager-only first reservation failed.");
            Require(
                first.Slot.PlayerSlotId.Equals(playerOne.PlayerSlotId),
                "Manager-only first reservation did not allocate P1.");

            PlayerParticipationOperationResult second =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "manager-second");
            RequireStatus(
                second,
                PlayerParticipationOperationStatus.Succeeded,
                "Manager-only second reservation failed.");
            Require(
                second.Slot.PlayerSlotId.Equals(playerTwo.PlayerSlotId),
                "Manager-only second reservation did not allocate P2.");
        }

        /// <summary>
        /// PUBLIC resolve + INTERNAL Session authority: mixed SceneProvided P1
        /// and ManagerProvisioned P2. Manager cannot skip P1 while available;
        /// after SceneProvided admits P1, Manager receives P2.
        /// </summary>
        private static void Case04MixedSceneThenManager(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile playerOne = CreateSlotProfile(
                created, "qa.session05.mixed.p1", "Mixed P1", null);
            PlayerSlotProfile playerTwo = CreateSlotProfile(
                created, "qa.session05.mixed.p2", "Mixed P2", null);
            PlayerProvisioningProfile provisioning = CreateProvisioningProfile(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                new SlotOverrideSpec(
                    playerOne,
                    PlayerHostProvisioningMode.SceneProvided));
            PlayerSessionProfile sessionProfile = CreateSessionProfile(
                created,
                new[] { playerOne, playerTwo },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(sessionProfile);
            RequireSucceededResolution(resolution, "Mixed resolution");
            EffectivePlayerSessionConfiguration configuration =
                resolution.Configuration;
            Require(
                configuration.Slots[0].HostProvisioningMode ==
                    PlayerHostProvisioningMode.SceneProvided,
                "Mixed effective P1 Host provisioning is not SceneProvided.");
            Require(
                configuration.Slots[1].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned,
                "Mixed effective P2 Host provisioning is not ManagerProvisioned.");

            // INTERNAL TECHNICAL SETUP: Session authority creation/reservation.
            PlayerParticipationRuntimeContext context =
                CreateContextFromEffective(configuration);

            PlayerParticipationOperationResult managerSkip =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "manager-cannot-skip-p1");
            RequireStatus(
                managerSkip,
                PlayerParticipationOperationStatus.RejectedInvalidRequest,
                "Manager reserved while first Available Slot is SceneProvided.");
            Require(
                managerSkip.Message.IndexOf(
                    "No provisioning fallback",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "Manager skip rejection did not state that no fallback was applied. " +
                $"message='{managerSkip.Message}'.");

            PlayerParticipationOperationResult sceneP1 =
                context.TryReserveSceneLocalPlayerSlot(
                    playerOne.PlayerSlotId,
                    Source,
                    "scene-admit-p1",
                    out bool orderedSlotMismatch);
            Require(
                !orderedSlotMismatch,
                "SceneProvided P1 reservation reported ordered Slot mismatch.");
            RequireStatus(
                sceneP1,
                PlayerParticipationOperationStatus.Succeeded,
                "SceneProvided P1 reservation failed.");
            Require(
                sceneP1.Slot.PlayerSlotId.Equals(playerOne.PlayerSlotId),
                "SceneProvided reservation did not admit P1.");

            // Scene pipeline admission mark (synthetic, not full Host lifecycle).
            PlayerParticipationOperationResult joinedP1 =
                context.TryMarkJoined(
                    sceneP1.ReservationToken,
                    Source,
                    "scene-joined-p1");
            RequireStatus(
                joinedP1,
                PlayerParticipationOperationStatus.Succeeded,
                "SceneProvided P1 MarkJoined failed.");

            PlayerParticipationOperationResult managerP2 =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "manager-after-p1");
            RequireStatus(
                managerP2,
                PlayerParticipationOperationStatus.Succeeded,
                "Manager did not receive P2 after SceneProvided P1 admission.");
            Require(
                managerP2.Slot.PlayerSlotId.Equals(playerTwo.PlayerSlotId),
                "Manager post-P1 reservation did not allocate P2.");
        }

        /// <summary>
        /// PUBLIC resolve + INTERNAL Session authority: mutating Profile assets
        /// after Session initialization does not rewrite structural Session
        /// configuration or Host provisioning modes.
        /// </summary>
        private static void Case05ProfileEditAfterInitFrozen(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile playerOne = CreateSlotProfile(
                created, "qa.session05.freeze.p1", "Freeze P1", null);
            PlayerSlotProfile playerTwo = CreateSlotProfile(
                created, "qa.session05.freeze.p2", "Freeze P2", null);
            PlayerSlotProfile playerThree = CreateSlotProfile(
                created, "qa.session05.freeze.p3", "Freeze P3", null);
            PlayerProvisioningProfile provisioning = CreateProvisioningProfile(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile sessionProfile = CreateSessionProfile(
                created,
                new[] { playerOne, playerTwo },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(sessionProfile);
            RequireSucceededResolution(resolution, "Freeze resolution");
            EffectivePlayerSessionConfiguration frozen =
                resolution.Configuration;

            // INTERNAL TECHNICAL SETUP: Session authority creation.
            PlayerParticipationRuntimeContext context =
                CreateContextFromEffective(frozen);
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            Require(
                before.ConfiguredSlotCount == 2 &&
                before.DynamicCapacity == 2 &&
                before.JoiningOpen,
                "Freeze baseline snapshot is incorrect.");

            // Mutate authored Profile after Session exists.
            ApplySessionProfile(
                sessionProfile,
                new[] { playerThree, playerTwo, playerOne },
                initialCapacity: 1,
                initialJoiningOpen: false,
                provisioning);
            ApplyProvisioningProfile(
                provisioning,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.LeaveUnresolved);

            PlayerSessionInitializationResult reResolved =
                PlayerSessionConfigurationResolver.Resolve(sessionProfile);
            RequireSucceededResolution(
                reResolved,
                "Post-edit re-resolution (evidence only; must not reapply)");
            Require(
                reResolved.Configuration.SupportedSlotCount == 3 &&
                reResolved.Configuration.InitialCapacity == 1 &&
                !reResolved.Configuration.InitialJoiningOpen &&
                reResolved.Configuration.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.LeaveUnresolved &&
                reResolved.Configuration.Slots[0].HostProvisioningMode ==
                    PlayerHostProvisioningMode.SceneProvided,
                "Post-edit re-resolution did not observe mutated authored intent.");

            PlayerParticipationSnapshot after = context.CreateSnapshot();
            Require(
                after.ConfiguredSlotCount == before.ConfiguredSlotCount,
                "Profile edit changed live Supported Slot count.");
            Require(
                after.DynamicCapacity == before.DynamicCapacity,
                "Profile edit changed live Session capacity.");
            Require(
                after.JoiningOpen == before.JoiningOpen,
                "Profile edit changed live Joining state.");
            Require(
                after.Slots[0].PlayerSlotId.Equals(playerOne.PlayerSlotId) &&
                after.Slots[1].PlayerSlotId.Equals(playerTwo.PlayerSlotId),
                "Profile edit reordered live Session Slots.");

            // Frozen Host provisioning still Manager for P1.
            PlayerParticipationOperationResult reserve =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "frozen-manager-still-applies");
            RequireStatus(
                reserve,
                PlayerParticipationOperationStatus.Succeeded,
                "Frozen Manager provisioning was lost after Profile edit.");
            Require(
                reserve.Slot.PlayerSlotId.Equals(playerOne.PlayerSlotId),
                "Frozen allocation order was lost after Profile edit.");
        }

        /// <summary>
        /// PUBLIC resolve + INTERNAL Session authority: LeaveUnresolved with a
        /// configured Default Actor must reject automatic default selection.
        /// </summary>
        private static void Case06LeaveUnresolvedNoAutoActor(
            ICollection<UnityEngine.Object> created)
        {
            ActorProfile defaultActor = CreateActorProfile(
                created,
                "qa.session05.actor.default",
                "Session05 Default Actor");
            PlayerSlotProfile playerOne = CreateSlotProfile(
                created,
                "qa.session05.actor.p1",
                "Actor P1",
                defaultActor);
            Require(
                playerOne.HasDefaultActorProfile &&
                ReferenceEquals(playerOne.DefaultActorProfile, defaultActor),
                "LeaveUnresolved fixture lost configured Default Actor.");

            PlayerProvisioningProfile provisioning = CreateProvisioningProfile(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.LeaveUnresolved);
            PlayerSessionProfile sessionProfile = CreateSessionProfile(
                created,
                new[] { playerOne },
                initialCapacity: 1,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(sessionProfile);
            RequireSucceededResolution(resolution, "LeaveUnresolved resolution");
            Require(
                resolution.Configuration.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.LeaveUnresolved,
                "LeaveUnresolved was not captured in effective configuration.");
            Require(
                resolution.Configuration.Slots[0].HasDefaultActorProfile,
                "Effective Slot lost Default Actor evidence.");

            // INTERNAL TECHNICAL SETUP: Session authority + default selection.
            PlayerParticipationRuntimeContext context =
                CreateContextFromEffective(resolution.Configuration);

            PlayerParticipationOperationResult reserved =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "leave-unresolved-reserve");
            RequireStatus(
                reserved,
                PlayerParticipationOperationStatus.Succeeded,
                "LeaveUnresolved reservation failed.");
            PlayerParticipationOperationResult joined =
                context.TryMarkJoined(
                    reserved.ReservationToken,
                    Source,
                    "leave-unresolved-join");
            RequireStatus(
                joined,
                PlayerParticipationOperationStatus.Succeeded,
                "LeaveUnresolved MarkJoined failed.");
            Require(
                !joined.Slot.HasSelectedActor,
                "Join auto-selected an Actor under LeaveUnresolved.");

            PlayerActorSelectionResult selectDefault =
                context.TrySelectDefaultActor(
                    playerOne.PlayerSlotId,
                    joined.Slot.SelectionRevision,
                    Source,
                    "leave-unresolved-select-default");
            Require(
                selectDefault != null,
                "TrySelectDefaultActor returned null.");
            Require(
                selectDefault.Status ==
                    PlayerActorSelectionStatus.RejectedDefaultResolutionDisabled,
                "LeaveUnresolved did not reject default Actor selection. " +
                $"actual='{selectDefault.Status}' message='{selectDefault.Message}'.");
            Require(
                !selectDefault.Succeeded,
                "LeaveUnresolved default selection reported success.");
            Require(
                !selectDefault.Slot.HasSelectedActor,
                "LeaveUnresolved still committed a Default Actor.");
        }

        // ------------------------------------------------------------------
        // Case harness
        // ------------------------------------------------------------------

        private static CaseResult RunCase(string caseId, Action body)
        {
            try
            {
                body();
                Debug.Log(
                    $"{LogPrefix} case='{caseId}' status='PASS'.");
                return new CaseResult(caseId, true, string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} case='{caseId}' status='FAIL' " +
                    $"message='{Escape(exception.Message)}'.");
                return new CaseResult(caseId, false, exception.Message);
            }
        }

        private readonly struct CaseResult
        {
            internal CaseResult(string caseId, bool passed, string detail)
            {
                CaseId = caseId;
                Passed = passed;
                Detail = detail ?? string.Empty;
            }

            internal string CaseId { get; }

            internal bool Passed { get; }

            internal string Detail { get; }
        }

        // ------------------------------------------------------------------
        // Fixture factories (canonical QA authoring style)
        // ------------------------------------------------------------------

        private static GameApplicationAsset CreateGameApplication(
            ICollection<UnityEngine.Object> created,
            bool playerSessionEnabled,
            PlayerSessionProfile defaultPlayerSessionProfile)
        {
            var gameApplication =
                ScriptableObject.CreateInstance<GameApplicationAsset>();
            gameApplication.name = "QA_IF_SESSION_CONFIG_05_GameApplication";
            created.Add(gameApplication);

            var serialized = new SerializedObject(gameApplication);
            serialized.FindProperty("playerSessionEnabled").boolValue =
                playerSessionEnabled;
            serialized.FindProperty("defaultPlayerSessionProfile")
                .objectReferenceValue = defaultPlayerSessionProfile;
            serialized.FindProperty("playerActorSelectionDuplicatePolicy")
                .intValue =
                (int)PlayerActorSelectionDuplicatePolicy.AllowDuplicates;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gameApplication;
        }

        private static PlayerSessionProfile CreateSessionProfile(
            ICollection<UnityEngine.Object> created,
            PlayerSlotProfile[] supportedSlots,
            int initialCapacity,
            bool initialJoiningOpen,
            PlayerProvisioningProfile provisioningProfile)
        {
            var profile =
                ScriptableObject.CreateInstance<PlayerSessionProfile>();
            profile.name = "QA_IF_SESSION_CONFIG_05_SessionProfile";
            created.Add(profile);
            ApplySessionProfile(
                profile,
                supportedSlots,
                initialCapacity,
                initialJoiningOpen,
                provisioningProfile);
            return profile;
        }

        private static void ApplySessionProfile(
            PlayerSessionProfile profile,
            PlayerSlotProfile[] supportedSlots,
            int initialCapacity,
            bool initialJoiningOpen,
            PlayerProvisioningProfile provisioningProfile)
        {
            var serialized = new SerializedObject(profile);
            SerializedProperty slots =
                serialized.FindProperty("supportedSlots");
            slots.arraySize =
                supportedSlots != null ? supportedSlots.Length : 0;
            for (int index = 0; index < slots.arraySize; index++)
            {
                slots.GetArrayElementAtIndex(index).objectReferenceValue =
                    supportedSlots[index];
            }

            serialized.FindProperty("initialCapacity").intValue =
                initialCapacity;
            serialized.FindProperty("initialJoiningOpen").boolValue =
                initialJoiningOpen;
            serialized.FindProperty("playerProvisioningProfile")
                .objectReferenceValue = provisioningProfile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct SlotOverrideSpec
        {
            internal SlotOverrideSpec(
                PlayerSlotProfile playerSlotProfile,
                PlayerHostProvisioningMode hostProvisioningMode)
            {
                PlayerSlotProfile = playerSlotProfile;
                HostProvisioningMode = hostProvisioningMode;
            }

            internal PlayerSlotProfile PlayerSlotProfile { get; }

            internal PlayerHostProvisioningMode HostProvisioningMode { get; }
        }

        private static PlayerProvisioningProfile CreateProvisioningProfile(
            ICollection<UnityEngine.Object> created,
            PlayerHostProvisioningMode defaultHostProvisioning,
            PlayerActorResolutionPolicy actorResolutionPolicy,
            params SlotOverrideSpec[] slotOverrides)
        {
            var profile =
                ScriptableObject.CreateInstance<PlayerProvisioningProfile>();
            profile.name = "QA_IF_SESSION_CONFIG_05_ProvisioningProfile";
            created.Add(profile);
            ApplyProvisioningProfile(
                profile,
                defaultHostProvisioning,
                actorResolutionPolicy,
                slotOverrides);
            return profile;
        }

        private static void ApplyProvisioningProfile(
            PlayerProvisioningProfile profile,
            PlayerHostProvisioningMode defaultHostProvisioning,
            PlayerActorResolutionPolicy actorResolutionPolicy,
            params SlotOverrideSpec[] slotOverrides)
        {
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("defaultHostProvisioning").intValue =
                (int)defaultHostProvisioning;
            serialized.FindProperty("actorResolutionPolicy").intValue =
                (int)actorResolutionPolicy;

            SerializedProperty overrides =
                serialized.FindProperty("slotOverrides");
            overrides.arraySize =
                slotOverrides != null ? slotOverrides.Length : 0;
            for (int index = 0; index < overrides.arraySize; index++)
            {
                SerializedProperty element =
                    overrides.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("playerSlotProfile")
                    .objectReferenceValue =
                    slotOverrides[index].PlayerSlotProfile;
                element.FindPropertyRelative("hostProvisioningMode")
                    .intValue =
                    (int)slotOverrides[index].HostProvisioningMode;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerSlotProfile CreateSlotProfile(
            ICollection<UnityEngine.Object> created,
            string slotId,
            string displayName,
            ActorProfile defaultActorProfile)
        {
            var profile =
                ScriptableObject.CreateInstance<PlayerSlotProfile>();
            profile.name = displayName;
            created.Add(profile);

            var serialized = new SerializedObject(profile);
            serialized.FindProperty("playerSlotId").stringValue = slotId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("defaultActorProfile")
                .objectReferenceValue = defaultActorProfile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static ActorProfile CreateActorProfile(
            ICollection<UnityEngine.Object> created,
            string actorProfileId,
            string displayName)
        {
            var profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = displayName;
            created.Add(profile);

            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue =
                actorProfileId;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        // ------------------------------------------------------------------
        // INTERNAL TECHNICAL SETUP helpers
        // ------------------------------------------------------------------

        private static PlayerParticipationRuntimeContext CreateContextFromEffective(
            EffectivePlayerSessionConfiguration configuration)
        {
            PlayerParticipationOperationResult result =
                PlayerParticipationRuntimeContext.TryCreateWithEffectiveConfiguration(
                    configuration,
                    PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                    Source,
                    "create-from-effective",
                    out PlayerParticipationRuntimeContext context);

            Require(
                result != null && result.Succeeded,
                "INTERNAL TECHNICAL SETUP failed to create Session context. " +
                (result != null ? result.ToDiagnosticString() : "null"));
            Require(
                context != null,
                "INTERNAL TECHNICAL SETUP returned no Session context.");
            return context;
        }

        // ------------------------------------------------------------------
        // Assertions
        // ------------------------------------------------------------------

        private static void RequireSucceededResolution(
            PlayerSessionInitializationResult resolution,
            string label)
        {
            Require(resolution != null, $"{label}: resolution is null.");
            Require(
                resolution.Succeeded && resolution.Configuration != null,
                $"{label}: resolution failed. failure='{resolution.Failure}' " +
                $"message='{resolution.Message}'.");
        }

        private static void RequireStatus(
            PlayerParticipationOperationResult result,
            PlayerParticipationOperationStatus expected,
            string message)
        {
            Require(result != null, message + " Result is null.");
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} expected='{expected}' actual='{result.Status}' " +
                    $"diagnostics='{result.ToDiagnosticString()}'.");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value
                    .Replace("\\", "\\\\")
                    .Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

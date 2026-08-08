using System;
using System.Collections.Generic;
using System.Text;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.PlayerAssignment.Internal.Editor
{
    /// <summary>
    /// IF-SESSION-CONFIG-07 — QA Contract Closure for ADR-016 Player Session
    /// initial configuration contracts (cuts 01–06).
    /// </summary>
    /// <remarks>
    /// Companion to
    /// <see cref="QaIfSessionConfig05PlayerSessionRuntimeIntegrationSmoke"/>.
    /// Cases already certified by CONFIG-05 are deliberately not re-executed:
    /// disabled valid absence; enabled missing Profile; Manager-only ordered
    /// allocation; mixed Scene+Manager no Manager skip; Profile edit freeze
    /// after init; LeaveUnresolved blocks auto default Actor.
    /// <para>
    /// Classification tags on each case:
    /// PUBLIC-ONLY — arrangement, action and assertion use public contracts;
    /// PARTIAL PUBLIC EVIDENCE — public resolve/evidence plus privileged Session
    /// authority setup (ADR-015 consumer surface incomplete);
    /// INTERNAL TECHNICAL — Session authority operations that remain internal.
    /// </para>
    /// <para>
    /// NOT DIRECTLY CERTIFIED here: real Route/ActivityFlow transitions that
    /// prove Session structural configuration is not reapplied (requires Play
    /// Mode integration surface; Edit Mode host creation is invalid).
    /// </para>
    /// </remarks>
    internal static class QaIfSessionConfig07PlayerSessionContractClosureSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/" +
            "Run IF-SESSION-CONFIG-07 Player Session Contract Closure Smoke";

        private const string LogPrefix =
            "[IF_SESSION_CONFIG_07_PLAYER_SESSION_CONTRACT_CLOSURE]";

        private const string Source =
            "QA.IF-SESSION-CONFIG-07";

        /// <summary>
        /// Ordered case ids. Count is asserted by the runner.
        /// </summary>
        private static readonly string[] CaseIds =
        {
            // PUBLIC-ONLY
            "pub-default-resolution-complete",
            "pub-override-complete-source-no-merge",
            "pub-missing-provisioning-profile",
            "pub-duplicate-supported-slot-identity",
            "pub-invalid-capacity-above-supported",
            "pub-capacity-below-supported-slots-remain",
            "pub-unsupported-provisioning-override-slot",
            "pub-scene-only-effective-resolution",
            "pub-resolve-configured-default-policy",
            "pub-typed-failure-and-immutable-evidence",
            "pub-session-vs-activity-projection-separation",
            // PARTIAL PUBLIC EVIDENCE / INTERNAL TECHNICAL
            "int-scene-only-ordered-allocation",
            "int-no-fallback-scene-on-manager-slot",
            "int-first-available-skips-occupied",
            "int-late-join-frozen-provisioning",
            "int-runtime-capacity-not-init-evidence",
            "int-resolve-configured-default-selects-actor"
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
                    "IF-SESSION-CONFIG-07 smoke must run in Edit Mode.");

                // PUBLIC-ONLY
                results.Add(RunCase(
                    CaseIds[0],
                    "PUBLIC-ONLY",
                    () => PubDefaultResolutionComplete(created)));
                results.Add(RunCase(
                    CaseIds[1],
                    "PUBLIC-ONLY",
                    () => PubOverrideCompleteSourceNoMerge(created)));
                results.Add(RunCase(
                    CaseIds[2],
                    "PUBLIC-ONLY",
                    () => PubMissingProvisioningProfile(created)));
                results.Add(RunCase(
                    CaseIds[3],
                    "PUBLIC-ONLY",
                    () => PubDuplicateSupportedSlotIdentity(created)));
                results.Add(RunCase(
                    CaseIds[4],
                    "PUBLIC-ONLY",
                    () => PubInvalidCapacityAboveSupported(created)));
                results.Add(RunCase(
                    CaseIds[5],
                    "PUBLIC-ONLY",
                    () => PubCapacityBelowSupportedSlotsRemain(created)));
                results.Add(RunCase(
                    CaseIds[6],
                    "PUBLIC-ONLY",
                    () => PubUnsupportedProvisioningOverrideSlot(created)));
                results.Add(RunCase(
                    CaseIds[7],
                    "PUBLIC-ONLY",
                    () => PubSceneOnlyEffectiveResolution(created)));
                results.Add(RunCase(
                    CaseIds[8],
                    "PUBLIC-ONLY",
                    () => PubResolveConfiguredDefaultPolicy(created)));
                results.Add(RunCase(
                    CaseIds[9],
                    "PUBLIC-ONLY",
                    () => PubTypedFailureAndImmutableEvidence(created)));
                results.Add(RunCase(
                    CaseIds[10],
                    "PUBLIC-ONLY",
                    () => PubSessionVsActivityProjectionSeparation(created)));

                // PARTIAL PUBLIC EVIDENCE + INTERNAL TECHNICAL SETUP
                results.Add(RunCase(
                    CaseIds[11],
                    "PARTIAL PUBLIC EVIDENCE",
                    () => IntSceneOnlyOrderedAllocation(created)));
                results.Add(RunCase(
                    CaseIds[12],
                    "INTERNAL TECHNICAL",
                    () => IntNoFallbackSceneOnManagerSlot(created)));
                results.Add(RunCase(
                    CaseIds[13],
                    "INTERNAL TECHNICAL",
                    () => IntFirstAvailableSkipsOccupied(created)));
                results.Add(RunCase(
                    CaseIds[14],
                    "PARTIAL PUBLIC EVIDENCE",
                    () => IntLateJoinFrozenProvisioning(created)));
                results.Add(RunCase(
                    CaseIds[15],
                    "INTERNAL TECHNICAL",
                    () => IntRuntimeCapacityNotInitEvidence(created)));
                results.Add(RunCase(
                    CaseIds[16],
                    "INTERNAL TECHNICAL",
                    () => IntResolveConfiguredDefaultSelectsActor(created)));

                Require(
                    results.Count == CaseIds.Length,
                    "IF-SESSION-CONFIG-07 case count changed unexpectedly.");

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
                        .Append('[')
                        .Append(result.Classification)
                        .Append("]=")
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
                        $"results='{summary}' " +
                        "reusedConfig05='disabled-missing-manager-mixed-freeze-leaveUnresolved' " +
                        "gap='route-activity-reapply-not-directly-certified' " +
                        "adr015='session-authority-consumer-surface-missing'.");
                }
                else
                {
                    Debug.LogError(
                        $"{LogPrefix} status='FAIL' cases='{results.Count}' " +
                        $"results='{summary}'.");
                    throw new InvalidOperationException(
                        "IF-SESSION-CONFIG-07 contract closure failed one or more cases. " +
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
        // PUBLIC-ONLY
        // ------------------------------------------------------------------

        private static void PubDefaultResolutionComplete(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.default.p1", "Default P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.default.p2", "Default P2", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2 },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "default resolution");
            EffectivePlayerSessionConfiguration configuration =
                resolution.Configuration;

            Require(configuration.SupportedSlotCount == 2,
                "Default resolution Supported Slot count changed.");
            Require(configuration.InitialCapacity == 2,
                "Default resolution Initial Capacity changed.");
            Require(configuration.InitialJoiningOpen,
                "Default resolution Initial Joining changed.");
            Require(
                configuration.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                "Default resolution Actor policy changed.");
            Require(
                configuration.Slots[0].PlayerSlotId.Equals(p1.PlayerSlotId) &&
                configuration.Slots[1].PlayerSlotId.Equals(p2.PlayerSlotId),
                "Default resolution Slot identity/order changed.");
            Require(
                configuration.Slots[0].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned &&
                configuration.Slots[1].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned,
                "Default resolution Host provisioning changed.");
            Require(
                resolution.Failure == PlayerSessionInitializationFailure.None &&
                resolution.Succeeded &&
                !resolution.Failed,
                "Successful resolution flags/enums are inconsistent.");
        }

        private static void PubOverrideCompleteSourceNoMerge(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile defaultP1 = CreateSlot(
                created, "qa.s07.merge.default.p1", "Default P1", null);
            PlayerSlotProfile defaultP2 = CreateSlot(
                created, "qa.s07.merge.default.p2", "Default P2", null);
            PlayerProvisioningProfile defaultProvisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile defaultSession = CreateSession(
                created,
                new[] { defaultP1, defaultP2 },
                initialCapacity: 2,
                initialJoiningOpen: true,
                defaultProvisioning);

            PlayerSlotProfile overrideOnly = CreateSlot(
                created, "qa.s07.merge.override.p3", "Override P3", null);
            PlayerProvisioningProfile overrideProvisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.LeaveUnresolved);
            PlayerSessionProfile overrideSession = CreateSession(
                created,
                new[] { overrideOnly },
                initialCapacity: 1,
                initialJoiningOpen: false,
                overrideProvisioning);

            // GameApplication default is an authored source only. Public
            // resolution accepts one Profile source; an explicit override is a
            // complete replacement, not a field merge with the default.
            GameApplicationAsset gameApplication =
                ScriptableObject.CreateInstance<GameApplicationAsset>();
            created.Add(gameApplication);
            var gaSerialized = new SerializedObject(gameApplication);
            gaSerialized.FindProperty("playerSessionEnabled").boolValue = true;
            gaSerialized.FindProperty("defaultPlayerSessionProfile")
                .objectReferenceValue = defaultSession;
            gaSerialized.ApplyModifiedPropertiesWithoutUndo();

            PlayerSessionInitializationResult defaultResolution =
                PlayerSessionConfigurationResolver.Resolve(
                    gameApplication.DefaultPlayerSessionProfile);
            PlayerSessionInitializationResult overrideResolution =
                PlayerSessionConfigurationResolver.Resolve(overrideSession);

            RequireSucceeded(defaultResolution, "default source");
            RequireSucceeded(overrideResolution, "override source");

            EffectivePlayerSessionConfiguration fromDefault =
                defaultResolution.Configuration;
            EffectivePlayerSessionConfiguration fromOverride =
                overrideResolution.Configuration;

            Require(
                fromDefault.SupportedSlotCount == 2 &&
                fromDefault.Slots[0].PlayerSlotId.Equals(defaultP1.PlayerSlotId) &&
                fromDefault.Slots[1].PlayerSlotId.Equals(defaultP2.PlayerSlotId) &&
                fromDefault.Slots[0].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned,
                "Default source resolution is not complete/independent.");
            Require(
                fromOverride.SupportedSlotCount == 1 &&
                fromOverride.Slots[0].PlayerSlotId.Equals(overrideOnly.PlayerSlotId) &&
                fromOverride.InitialCapacity == 1 &&
                !fromOverride.InitialJoiningOpen &&
                fromOverride.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.LeaveUnresolved &&
                fromOverride.Slots[0].HostProvisioningMode ==
                    PlayerHostProvisioningMode.SceneProvided,
                "Override source was not a complete independent configuration.");
            Require(
                fromOverride.SupportedSlotCount != fromDefault.SupportedSlotCount &&
                !fromOverride.Slots[0].PlayerSlotId.Equals(
                    fromDefault.Slots[0].PlayerSlotId),
                "Override resolution silently merged Default Session fields.");
        }

        private static void PubMissingProvisioningProfile(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.missing.prov.p1", "Missing Prov P1", null);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1 },
                initialCapacity: 1,
                initialJoiningOpen: true,
                provisioningProfile: null);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireFailed(
                resolution,
                PlayerSessionInitializationFailure.MissingRequiredConfiguration,
                "Missing Provisioning Profile");
        }

        private static void PubDuplicateSupportedSlotIdentity(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.dup.slot", "Dup A", null);
            PlayerSlotProfile p1Dup = CreateSlot(
                created, "qa.s07.dup.slot", "Dup B", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p1Dup },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireFailed(
                resolution,
                PlayerSessionInitializationFailure.InvalidPlayerSessionProfile,
                "Duplicate Supported Slot identity");
        }

        private static void PubInvalidCapacityAboveSupported(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.cap.high.p1", "Cap High P1", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1 },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireFailed(
                resolution,
                PlayerSessionInitializationFailure.InvalidPlayerSessionProfile,
                "Capacity above Supported Slot count");
        }

        private static void PubCapacityBelowSupportedSlotsRemain(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.cap.low.p1", "Cap Low P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.cap.low.p2", "Cap Low P2", null);
            PlayerSlotProfile p3 = CreateSlot(
                created, "qa.s07.cap.low.p3", "Cap Low P3", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2, p3 },
                initialCapacity: 1,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "capacity below supported");
            EffectivePlayerSessionConfiguration configuration =
                resolution.Configuration;

            Require(configuration.InitialCapacity == 1,
                "Initial Capacity was not preserved.");
            Require(configuration.SupportedSlotCount == 3,
                "Slots outside Initial Capacity were dropped from Supported universe.");
            Require(
                configuration.Slots[0].PlayerSlotId.Equals(p1.PlayerSlotId) &&
                configuration.Slots[1].PlayerSlotId.Equals(p2.PlayerSlotId) &&
                configuration.Slots[2].PlayerSlotId.Equals(p3.PlayerSlotId),
                "Supported Slot order/identity lost when Capacity < Supported.");
        }

        private static void PubUnsupportedProvisioningOverrideSlot(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile supported = CreateSlot(
                created, "qa.s07.override.supported", "Supported", null);
            PlayerSlotProfile foreign = CreateSlot(
                created, "qa.s07.override.foreign", "Foreign", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                new SlotOverrideSpec(
                    foreign,
                    PlayerHostProvisioningMode.SceneProvided));
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { supported },
                initialCapacity: 1,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireFailed(
                resolution,
                PlayerSessionInitializationFailure
                    .UnsupportedProvisioningOverrideSlot,
                "Unsupported provisioning override Slot");
        }

        private static void PubSceneOnlyEffectiveResolution(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.scene.only.p1", "Scene P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.scene.only.p2", "Scene P2", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2 },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "scene-only resolution");
            Require(
                resolution.Configuration.Slots[0].HostProvisioningMode ==
                    PlayerHostProvisioningMode.SceneProvided &&
                resolution.Configuration.Slots[1].HostProvisioningMode ==
                    PlayerHostProvisioningMode.SceneProvided,
                "Scene-only default Host provisioning was not applied.");
        }

        private static void PubResolveConfiguredDefaultPolicy(
            ICollection<UnityEngine.Object> created)
        {
            ActorProfile actor = CreateActor(
                created, "qa.s07.actor.default", "Default Actor");
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.policy.p1", "Policy P1", actor);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1 },
                initialCapacity: 1,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "ResolveConfiguredDefault policy");
            Require(
                resolution.Configuration.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                "ResolveConfiguredDefault was not captured.");
            Require(
                resolution.Configuration.Slots[0].HasDefaultActorProfile &&
                ReferenceEquals(
                    resolution.Configuration.Slots[0].DefaultActorProfile,
                    actor),
                "Default Actor evidence was not captured on effective Slot.");
        }

        private static void PubTypedFailureAndImmutableEvidence(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSessionInitializationResult missing =
                PlayerSessionConfigurationResolver.Resolve(null);
            RequireFailed(
                missing,
                PlayerSessionInitializationFailure.MissingRequiredConfiguration,
                "null Profile typed failure");
            Require(
                missing.Configuration == null &&
                missing.Failed &&
                !missing.Succeeded &&
                !string.IsNullOrEmpty(missing.Message),
                "Typed failure did not preserve read-only failure evidence.");

            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.immut.p1", "Immut P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.immut.p2", "Immut P2", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2 },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult ok =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(ok, "immutable success evidence");
            EffectivePlayerSessionConfiguration configuration =
                ok.Configuration;
            int supportedBefore = configuration.SupportedSlotCount;
            PlayerSlotId firstBefore = configuration.Slots[0].PlayerSlotId;
            PlayerHostProvisioningMode modeBefore =
                configuration.Slots[0].HostProvisioningMode;

            ApplySession(
                session,
                new[] { p2 },
                initialCapacity: 1,
                initialJoiningOpen: false,
                provisioning);
            ApplyProvisioning(
                provisioning,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.LeaveUnresolved);

            Require(
                configuration.SupportedSlotCount == supportedBefore &&
                configuration.Slots[0].PlayerSlotId.Equals(firstBefore) &&
                configuration.Slots[0].HostProvisioningMode == modeBefore &&
                configuration.InitialCapacity == 2 &&
                configuration.InitialJoiningOpen &&
                configuration.ActorResolutionPolicy ==
                    PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                "Effective configuration was mutated after authored Profile edits.");
            Require(
                configuration.Slots is IReadOnlyList<EffectivePlayerSlotProvisioning>,
                "Effective Slots collection is not exposed as read-only.");
        }

        private static void PubSessionVsActivityProjectionSeparation(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.act.p1", "Act P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.act.p2", "Act P2", null);
            PlayerSlotProfile p3 = CreateSlot(
                created, "qa.s07.act.p3", "Act P3", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2, p3 },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "session vs activity separation");
            EffectivePlayerSessionConfiguration configuration =
                resolution.Configuration;

            ActivityAsset activity =
                ScriptableObject.CreateInstance<ActivityAsset>();
            created.Add(activity);
            var activitySerialized = new SerializedObject(activity);
            activitySerialized.FindProperty("activityName").stringValue =
                "QA S07 Explicit Subset";
            activitySerialized.FindProperty("playerParticipationProjectionMode")
                .intValue =
                (int)ActivityParticipationProjectionMode.ExplicitSlots;
            activitySerialized
                .FindProperty("playerParticipationRequirementLevel")
                .intValue =
                (int)PlayerParticipationRequirementLevel.JoinedSlots;
            activitySerialized
                .FindProperty("playerParticipationZeroParticipantPolicy")
                .intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Allowed;
            SerializedProperty explicitSlots = activitySerialized.FindProperty(
                "playerParticipationExplicitSlotProfiles");
            explicitSlots.arraySize = 1;
            explicitSlots.GetArrayElementAtIndex(0).objectReferenceValue = p1;
            activitySerialized.ApplyModifiedPropertiesWithoutUndo();

            Require(
                activity.TryGetPlayerParticipationProjectionDescriptor(
                    out ActivityParticipationProjectionDescriptor descriptor,
                    out string descriptorIssue),
                "Activity projection descriptor failed. " + descriptorIssue);
            Require(
                descriptor.ProjectsExplicitSlots &&
                descriptor.ExplicitSlotProfiles.Count == 1 &&
                ReferenceEquals(descriptor.ExplicitSlotProfiles[0], p1),
                "Activity projection does not own an explicit subset.");

            Require(
                configuration.SupportedSlotCount == 3 &&
                configuration.InitialCapacity == 2,
                "Activity projection redefined Session Supported Slots or Capacity.");
            Require(
                configuration.Slots[0].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned &&
                configuration.Slots[1].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned &&
                configuration.Slots[2].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned,
                "Activity projection rewrote Session Host provisioning intent.");
        }

        // ------------------------------------------------------------------
        // PARTIAL PUBLIC EVIDENCE / INTERNAL TECHNICAL
        // ------------------------------------------------------------------

        /// <summary>
        /// PARTIAL PUBLIC EVIDENCE: public resolve + INTERNAL Session authority
        /// Scene-only ordered allocation (CONFIG-05 covers Manager-only and mixed).
        /// </summary>
        private static void IntSceneOnlyOrderedAllocation(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.scene.alloc.p1", "Scene Alloc P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.scene.alloc.p2", "Scene Alloc P2", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2 },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "scene-only alloc resolve");

            // INTERNAL TECHNICAL SETUP: Session authority (ADR-015 gap).
            PlayerParticipationRuntimeContext context =
                CreateContext(resolution.Configuration);

            PlayerParticipationOperationResult first =
                context.TryReserveSceneLocalPlayerSlot(
                    p1.PlayerSlotId,
                    Source,
                    "scene-first",
                    out bool mismatchFirst);
            Require(!mismatchFirst, "Scene first ordered mismatch.");
            RequireStatus(
                first,
                PlayerParticipationOperationStatus.Succeeded,
                "Scene-only first reservation failed.");
            Require(
                first.Slot.PlayerSlotId.Equals(p1.PlayerSlotId),
                "Scene-only first reservation did not allocate P1.");
            RequireStatus(
                context.TryMarkJoined(first.ReservationToken, Source, "join-p1"),
                PlayerParticipationOperationStatus.Succeeded,
                "Scene-only P1 MarkJoined failed.");

            PlayerParticipationOperationResult second =
                context.TryReserveSceneLocalPlayerSlot(
                    p2.PlayerSlotId,
                    Source,
                    "scene-second",
                    out bool mismatchSecond);
            Require(!mismatchSecond, "Scene second ordered mismatch.");
            RequireStatus(
                second,
                PlayerParticipationOperationStatus.Succeeded,
                "Scene-only second reservation failed.");
            Require(
                second.Slot.PlayerSlotId.Equals(p2.PlayerSlotId),
                "Scene-only second reservation did not allocate P2.");
        }

        /// <summary>
        /// INTERNAL TECHNICAL: Scene pipeline cannot fall back onto a
        /// ManagerProvisioned Slot (inverse of CONFIG-05 Manager→Scene guard).
        /// </summary>
        private static void IntNoFallbackSceneOnManagerSlot(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.nofallback.p1", "NoFallback P1", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1 },
                initialCapacity: 1,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "no-fallback resolve");
            PlayerParticipationRuntimeContext context =
                CreateContext(resolution.Configuration);

            PlayerParticipationOperationResult sceneOnManager =
                context.TryReserveSceneLocalPlayerSlot(
                    p1.PlayerSlotId,
                    Source,
                    "scene-on-manager",
                    out _);
            RequireStatus(
                sceneOnManager,
                PlayerParticipationOperationStatus.RejectedInvalidRequest,
                "Scene reservation accepted a ManagerProvisioned Slot.");
            Require(
                sceneOnManager.Message.IndexOf(
                    "No provisioning fallback",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "Scene→Manager rejection did not state no fallback. " +
                $"message='{sceneOnManager.Message}'.");
        }

        /// <summary>
        /// INTERNAL TECHNICAL: first available Slot is the first Available in
        /// authored order after prior occupancy (allocation policy continuity).
        /// </summary>
        private static void IntFirstAvailableSkipsOccupied(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.first.p1", "First P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.first.p2", "First P2", null);
            PlayerSlotProfile p3 = CreateSlot(
                created, "qa.s07.first.p3", "First P3", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2, p3 },
                initialCapacity: 3,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "first-available resolve");
            PlayerParticipationRuntimeContext context =
                CreateContext(resolution.Configuration);

            PlayerParticipationOperationResult r1 =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "occupy-p1");
            RequireStatus(
                r1,
                PlayerParticipationOperationStatus.Succeeded,
                "First available P1 failed.");
            RequireStatus(
                context.TryMarkJoined(r1.ReservationToken, Source, "join-p1"),
                PlayerParticipationOperationStatus.Succeeded,
                "Join P1 failed.");

            PlayerParticipationOperationResult r2 =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "next-after-p1");
            RequireStatus(
                r2,
                PlayerParticipationOperationStatus.Succeeded,
                "First available after occupancy failed.");
            Require(
                r2.Slot.PlayerSlotId.Equals(p2.PlayerSlotId),
                "First available did not skip occupied P1 for P2.");
        }

        /// <summary>
        /// PARTIAL PUBLIC EVIDENCE: late Join uses frozen per-Slot Host
        /// provisioning after Profile mutation (extends CONFIG-05 freeze to a
        /// post-occupancy late Join).
        /// </summary>
        private static void IntLateJoinFrozenProvisioning(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.late.p1", "Late P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.late.p2", "Late P2", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault,
                new SlotOverrideSpec(
                    p1,
                    PlayerHostProvisioningMode.SceneProvided));
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2 },
                initialCapacity: 2,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "late-join resolve");
            EffectivePlayerSessionConfiguration frozen =
                resolution.Configuration;
            PlayerParticipationRuntimeContext context = CreateContext(frozen);

            PlayerParticipationOperationResult sceneP1 =
                context.TryReserveSceneLocalPlayerSlot(
                    p1.PlayerSlotId,
                    Source,
                    "late-scene-p1",
                    out bool mismatch);
            Require(!mismatch, "Late-join Scene P1 ordered mismatch.");
            RequireStatus(
                sceneP1,
                PlayerParticipationOperationStatus.Succeeded,
                "Late-join Scene P1 reservation failed.");
            RequireStatus(
                context.TryMarkJoined(
                    sceneP1.ReservationToken,
                    Source,
                    "late-join-p1"),
                PlayerParticipationOperationStatus.Succeeded,
                "Late-join Scene P1 MarkJoined failed.");

            // Mutate authored provisioning after P1 is already in Session.
            ApplyProvisioning(
                provisioning,
                PlayerHostProvisioningMode.SceneProvided,
                PlayerActorResolutionPolicy.LeaveUnresolved);

            PlayerParticipationOperationResult lateManager =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "late-manager-p2");
            RequireStatus(
                lateManager,
                PlayerParticipationOperationStatus.Succeeded,
                "Late Manager Join lost frozen Manager provisioning for P2.");
            Require(
                lateManager.Slot.PlayerSlotId.Equals(p2.PlayerSlotId),
                "Late Join did not allocate frozen P2.");
            Require(
                frozen.Slots[1].HostProvisioningMode ==
                    PlayerHostProvisioningMode.ManagerProvisioned,
                "Frozen effective evidence for P2 changed after late Join.");

            // Profile mutation forced SceneProvided default, but frozen P2 must
            // remain ManagerProvisioned for late Join.
            RequireStatus(
                context.TryReleaseReservation(
                    lateManager.ReservationToken,
                    Source,
                    "release-late-p2"),
                PlayerParticipationOperationStatus.Succeeded,
                "Could not release late P2 reservation for frozen re-check.");

            PlayerParticipationOperationResult sceneOnFrozenManager =
                context.TryReserveSceneLocalPlayerSlot(
                    p2.PlayerSlotId,
                    Source,
                    "late-scene-on-frozen-manager-p2",
                    out _);
            RequireStatus(
                sceneOnFrozenManager,
                PlayerParticipationOperationStatus.RejectedInvalidRequest,
                "Late Join accepted Scene provisioning for frozen Manager P2.");
            Require(
                sceneOnFrozenManager.Message.IndexOf(
                    "No provisioning fallback",
                    StringComparison.OrdinalIgnoreCase) >= 0,
                "Late Join Scene→Manager rejection did not state no fallback.");

            PlayerParticipationOperationResult managerAgain =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "late-manager-p2-again");
            RequireStatus(
                managerAgain,
                PlayerParticipationOperationStatus.Succeeded,
                "Frozen Manager provisioning for P2 was lost after Profile edit.");
            Require(
                managerAgain.Slot.PlayerSlotId.Equals(p2.PlayerSlotId),
                "Re-reservation after late Join freeze check did not reselect P2.");
        }

        /// <summary>
        /// INTERNAL TECHNICAL: runtime Capacity commands mutate live Session
        /// state, not the immutable initialization evidence.
        /// </summary>
        private static void IntRuntimeCapacityNotInitEvidence(
            ICollection<UnityEngine.Object> created)
        {
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.rtcap.p1", "RtCap P1", null);
            PlayerSlotProfile p2 = CreateSlot(
                created, "qa.s07.rtcap.p2", "RtCap P2", null);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1, p2 },
                initialCapacity: 1,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "runtime capacity resolve");
            EffectivePlayerSessionConfiguration initEvidence =
                resolution.Configuration;
            Require(initEvidence.InitialCapacity == 1,
                "Init evidence capacity fixture incorrect.");

            PlayerParticipationRuntimeContext context =
                CreateContext(initEvidence);
            PlayerParticipationSnapshot before = context.CreateSnapshot();
            Require(before.DynamicCapacity == 1,
                "Live capacity did not start from Initial Capacity.");
            Require(before.ConfiguredSlotCount == 2,
                "Supported Slots outside capacity were not configured.");

            PlayerParticipationOperationResult capacityReached =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "fill-capacity");
            RequireStatus(
                capacityReached,
                PlayerParticipationOperationStatus.Succeeded,
                "Initial capacity reservation failed.");
            RequireStatus(
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "over-capacity"),
                PlayerParticipationOperationStatus.RejectedCapacityReached,
                "Capacity bound did not reject over-capacity reservation.");

            PlayerParticipationOperationResult raise =
                context.TrySetDynamicCapacity(2, Source, "raise-capacity");
            RequireStatus(
                raise,
                PlayerParticipationOperationStatus.Succeeded,
                "Runtime capacity raise failed.");
            Require(
                context.CreateSnapshot().DynamicCapacity == 2,
                "Live capacity did not change via runtime command.");
            Require(
                initEvidence.InitialCapacity == 1,
                "Runtime capacity command mutated initialization evidence.");

            PlayerParticipationOperationResult overUniverse =
                context.TrySetDynamicCapacity(3, Source, "over-universe");
            RequireStatus(
                overUniverse,
                PlayerParticipationOperationStatus.RejectedInvalidRequest,
                "Capacity above Supported Slot universe was accepted.");
        }

        /// <summary>
        /// INTERNAL TECHNICAL: ResolveConfiguredDefault may select the
        /// configured Default Actor (complement of CONFIG-05 LeaveUnresolved).
        /// </summary>
        private static void IntResolveConfiguredDefaultSelectsActor(
            ICollection<UnityEngine.Object> created)
        {
            // Selection validates ActorProfile fully (id/kind/role + Logical
            // Actor Host prefab). Reuse the same synthetic fixture shape as
            // QaP3G3ProvisioningBridgeSyntheticSmoke.CreateActorProfile.
            ActorProfile actor = CreateSelectableActorProfile(
                created,
                "qa.s07.select.actor",
                "Selectable Default");
            Require(
                actor.HasLogicalActorHostPrefab &&
                actor.HasDefinedActorKind &&
                actor.HasDefinedActorRole,
                "Selectable Default ActorProfile fixture is not selection-valid.");
            PlayerSlotProfile p1 = CreateSlot(
                created, "qa.s07.select.p1", "Select P1", actor);
            PlayerProvisioningProfile provisioning = CreateProvisioning(
                created,
                PlayerHostProvisioningMode.ManagerProvisioned,
                PlayerActorResolutionPolicy.ResolveConfiguredDefault);
            PlayerSessionProfile session = CreateSession(
                created,
                new[] { p1 },
                initialCapacity: 1,
                initialJoiningOpen: true,
                provisioning);

            PlayerSessionInitializationResult resolution =
                PlayerSessionConfigurationResolver.Resolve(session);
            RequireSucceeded(resolution, "select-default resolve");
            PlayerParticipationRuntimeContext context =
                CreateContext(resolution.Configuration);

            PlayerParticipationOperationResult reserved =
                context.TryReserveNextAvailableSlot(
                    PlayerHostProvisioningMode.ManagerProvisioned,
                    Source,
                    "select-reserve");
            RequireStatus(
                reserved,
                PlayerParticipationOperationStatus.Succeeded,
                "Select-default reservation failed.");
            PlayerParticipationOperationResult joined =
                context.TryMarkJoined(
                    reserved.ReservationToken,
                    Source,
                    "select-join");
            RequireStatus(
                joined,
                PlayerParticipationOperationStatus.Succeeded,
                "Select-default MarkJoined failed.");
            Require(
                !joined.Slot.HasSelectedActor,
                "Join auto-selected Actor without SelectDefaultActor.");

            PlayerActorSelectionResult selectDefault =
                context.TrySelectDefaultActor(
                    p1.PlayerSlotId,
                    joined.Slot.SelectionRevision,
                    Source,
                    "select-default");
            Require(selectDefault != null, "SelectDefaultActor returned null.");
            Require(
                selectDefault.Succeeded &&
                selectDefault.Status ==
                    PlayerActorSelectionStatus.SucceededSelected,
                "ResolveConfiguredDefault failed to select Default Actor. " +
                $"status='{selectDefault.Status}' message='{selectDefault.Message}'.");
            Require(
                selectDefault.Slot.HasSelectedActor &&
                ReferenceEquals(
                    selectDefault.SelectedActorProfile,
                    actor),
                "Selected Default Actor evidence is incorrect.");
        }

        // ------------------------------------------------------------------
        // Case harness
        // ------------------------------------------------------------------

        private static CaseResult RunCase(
            string caseId,
            string classification,
            Action body)
        {
            try
            {
                body();
                Debug.Log(
                    $"{LogPrefix} case='{caseId}' classification='{classification}' status='PASS'.");
                return new CaseResult(
                    caseId,
                    classification,
                    true,
                    string.Empty);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{LogPrefix} case='{caseId}' classification='{classification}' status='FAIL' " +
                    $"message='{Escape(exception.Message)}'.");
                return new CaseResult(
                    caseId,
                    classification,
                    false,
                    exception.Message);
            }
        }

        private readonly struct CaseResult
        {
            internal CaseResult(
                string caseId,
                string classification,
                bool passed,
                string detail)
            {
                CaseId = caseId;
                Classification = classification;
                Passed = passed;
                Detail = detail ?? string.Empty;
            }

            internal string CaseId { get; }

            internal string Classification { get; }

            internal bool Passed { get; }

            internal string Detail { get; }
        }

        // ------------------------------------------------------------------
        // Fixtures
        // ------------------------------------------------------------------

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

        private static PlayerSessionProfile CreateSession(
            ICollection<UnityEngine.Object> created,
            PlayerSlotProfile[] supportedSlots,
            int initialCapacity,
            bool initialJoiningOpen,
            PlayerProvisioningProfile provisioningProfile)
        {
            var profile =
                ScriptableObject.CreateInstance<PlayerSessionProfile>();
            profile.name = "QA_IF_SESSION_CONFIG_07_SessionProfile";
            created.Add(profile);
            ApplySession(
                profile,
                supportedSlots,
                initialCapacity,
                initialJoiningOpen,
                provisioningProfile);
            return profile;
        }

        private static void ApplySession(
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

        private static PlayerProvisioningProfile CreateProvisioning(
            ICollection<UnityEngine.Object> created,
            PlayerHostProvisioningMode defaultHostProvisioning,
            PlayerActorResolutionPolicy actorResolutionPolicy,
            params SlotOverrideSpec[] slotOverrides)
        {
            var profile =
                ScriptableObject.CreateInstance<PlayerProvisioningProfile>();
            profile.name = "QA_IF_SESSION_CONFIG_07_ProvisioningProfile";
            created.Add(profile);
            ApplyProvisioning(
                profile,
                defaultHostProvisioning,
                actorResolutionPolicy,
                slotOverrides);
            return profile;
        }

        private static void ApplyProvisioning(
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

        private static PlayerSlotProfile CreateSlot(
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

        private static ActorProfile CreateActor(
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

        /// <summary>
        /// Minimal selection-valid ActorProfile: identity + default kind/role +
        /// Logical Actor Host root with PlayerActorDeclaration (canonical
        /// synthetic pattern from P3G3 Assignment InternalEditor).
        /// </summary>
        private static ActorProfile CreateSelectableActorProfile(
            ICollection<UnityEngine.Object> created,
            string actorProfileId,
            string displayName)
        {
            var logicalActorHost = new GameObject(
                displayName + " Logical Actor Host");
            logicalActorHost.SetActive(false);
            logicalActorHost.AddComponent<PlayerActorDeclaration>();
            created.Add(logicalActorHost);

            var profile = ScriptableObject.CreateInstance<ActorProfile>();
            profile.name = displayName;
            created.Add(profile);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("actorProfileId").stringValue =
                actorProfileId;
            serialized.FindProperty("displayName").stringValue = displayName;
            // Defaults already use ActorKind.Player / ActorRole.Protagonist.
            serialized.FindProperty("logicalActorHostPrefab")
                .objectReferenceValue = logicalActorHost;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Require(
                profile.HasLogicalActorHostPrefab &&
                ReferenceEquals(
                    profile.LogicalActorHostPrefab,
                    logicalActorHost),
                "Selectable ActorProfile did not bind Logical Actor Host prefab.");
            return profile;
        }

        private static PlayerParticipationRuntimeContext CreateContext(
            EffectivePlayerSessionConfiguration configuration)
        {
            // INTERNAL TECHNICAL SETUP: Session authority is not a public
            // consumer surface (ADR-015 dependency).
            PlayerParticipationOperationResult result =
                PlayerParticipationRuntimeContext.TryCreateWithEffectiveConfiguration(
                    configuration,
                    PlayerActorSelectionDuplicatePolicy.AllowDuplicates,
                    Source,
                    "create-from-effective",
                    out PlayerParticipationRuntimeContext context);

            Require(
                result != null && result.Succeeded && context != null,
                "INTERNAL TECHNICAL SETUP failed to create Session context. " +
                (result != null ? result.ToDiagnosticString() : "null"));
            return context;
        }

        // ------------------------------------------------------------------
        // Assertions
        // ------------------------------------------------------------------

        private static void RequireSucceeded(
            PlayerSessionInitializationResult resolution,
            string label)
        {
            Require(resolution != null, $"{label}: resolution is null.");
            Require(
                resolution.Succeeded && resolution.Configuration != null,
                $"{label}: resolution failed. failure='{resolution.Failure}' " +
                $"message='{resolution.Message}'.");
        }

        private static void RequireFailed(
            PlayerSessionInitializationResult resolution,
            PlayerSessionInitializationFailure expected,
            string label)
        {
            Require(resolution != null, $"{label}: resolution is null.");
            Require(resolution.Failed, $"{label}: expected failure.");
            Require(
                resolution.Failure == expected,
                $"{label}: expected failure='{expected}' actual='{resolution.Failure}' " +
                $"message='{resolution.Message}'.");
            Require(
                resolution.Configuration == null,
                $"{label}: failed resolution exposed configuration.");
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

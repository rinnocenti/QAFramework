using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Play Mode technical smoke for P3K.4 prepared Player camera eligibility.
    /// Camera request publication and GameplayReady aggregation are intentionally
    /// outside this cut.
    /// </summary>
    public static class QaP3K4PreparedPlayerCameraEligibilitySmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.4 Run Prepared Player Camera Eligibility Smoke";
        private const string OccupancyContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayOccupancyRuntimeContext";
        private const string InputContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayInputBindingRuntimeContext";
        private const string CameraContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayCameraEligibilityRuntimeContext";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        private static readonly BindingFlags StaticAny =
            BindingFlags.Static |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                AssertTrue(
                    Application.isPlaying,
                    "P3K.4 camera eligibility smoke must run in Play Mode.");

                Type occupancyContextType = ResolveType(
                    typeof(PlayerGameplayOccupancyStatus),
                    OccupancyContextTypeName);
                Type inputContextType = ResolveType(
                    typeof(PlayerGameplayInputBindingStatus),
                    InputContextTypeName);
                Type cameraContextType = ResolveType(
                    typeof(PlayerGameplayCameraEligibilityStatus),
                    CameraContextTypeName);

                ValidateContractSurface(cameraContextType);
                completed.Add("contract-surface-valid");

                const string sessionId = "qa.p3k4.session";
                PlayerSlotId slotOne = PlayerSlotId.From("player.1");
                PlayerSlotId slotTwo = PlayerSlotId.From("player.2");
                RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                    "qa.p3k4.activity",
                    "P3K.4 Activity");

                PlayerActorPreparationSummary preparedOne = CreatePreparation(
                    sessionId,
                    slotOne,
                    ActorProfileId.From("actor.profile.one"),
                    ActorId.From("actor.runtime.one"),
                    owner,
                    1);

                PlayerActorPreparationSummary preparedTwo = CreatePreparation(
                    sessionId,
                    slotTwo,
                    ActorProfileId.From("actor.profile.two"),
                    ActorId.From("actor.runtime.two"),
                    owner,
                    2);

                PlayerActorPreparationSnapshot preparationSnapshot =
                    CreatePreparationSnapshot(
                        sessionId,
                        preparedOne,
                        preparedTwo);

                object occupancyContext = CreateOccupancyContext(
                    occupancyContextType,
                    preparationSnapshot);

                PlayerGameplayOccupancyResult occupiedOne = ConfirmOccupancy(
                    occupancyContextType,
                    occupancyContext,
                    preparedOne,
                    "occupy-slot-one");

                AssertEqual(
                    PlayerGameplayOccupancyStatus.SucceededOccupied,
                    occupiedOne.Status,
                    "P3K.4 fixture could not occupy Slot one.");

                PlayerGameplayOccupancySnapshot occupancySnapshot =
                    OccupancySnapshot(
                        occupancyContextType,
                        occupancyContext);

                object inputContext = CreateInputContext(
                    inputContextType,
                    occupancyContext);

                object cameraContext = CreateCameraContext(
                    cameraContextType,
                    occupancyContext,
                    inputContext);

                PlayerGameplayCameraEligibilitySnapshot initial =
                    CameraSnapshot(
                        cameraContextType,
                        cameraContext);

                AssertTrue(
                    initial.IsInitialized,
                    "P3K.4 camera eligibility snapshot is not initialized.");
                AssertEqual(
                    2,
                    initial.ConfiguredSlotCount,
                    "P3K.4 camera context lost configured Slots.");
                AssertEqual(
                    2,
                    initial.NotEvaluatedCount,
                    "P3K.4 camera context did not start NotEvaluated.");
                completed.Add("context-initialized-from-live-rosters");

                AssertTrue(
                    occupancySnapshot.TryGetSummary(
                        slotTwo,
                        out PlayerGameplayOccupancySummary vacantTwo),
                    "P3K.4 fixture lost vacant Slot two occupancy.");

                PlayerGameplayInputBindingSnapshot unboundInputSnapshot =
                    InputSnapshot(
                        inputContextType,
                        inputContext);

                AssertTrue(
                    unboundInputSnapshot.TryGetSummary(
                        slotTwo,
                        out PlayerGameplayInputBindingSummary unboundTwo),
                    "P3K.4 fixture lost Slot two input summary.");

                PlayerGameplayCameraEligibilityResult vacantResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedTwo,
                        vacantTwo,
                        unboundTwo,
                        null,
                        null,
                        "vacant-occupancy");

                AssertStatus(
                    vacantResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedOccupancyNotReady,
                    "Vacant Slot was accepted for camera eligibility.");
                completed.Add("vacant-occupancy-rejected");

                AssertTrue(
                    unboundInputSnapshot.TryGetSummary(
                        slotOne,
                        out PlayerGameplayInputBindingSummary unboundOne),
                    "P3K.4 fixture lost Slot one input summary.");

                PlayerGameplayCameraEligibilityResult unboundInputResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        unboundOne,
                        null,
                        null,
                        "unbound-input");

                AssertStatus(
                    unboundInputResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedInputBindingNotReady,
                    "Unbound gameplay input was accepted for camera eligibility.");
                completed.Add("unbound-input-rejected");

                PlayerGameplayInputBindingSummary inputOne =
                    CreateBoundInputSummary(
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        1);

                InjectInputSummary(
                    inputContextType,
                    inputContext,
                    inputOne);

                PlayerGameplayInputBindingSummary inputTwo =
                    CreateBoundInputSummary(
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        2);

                InjectInputSummary(
                    inputContextType,
                    inputContext,
                    inputTwo);

                PlayerGameplayCameraEligibilityResult staleInputResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputOne,
                        null,
                        null,
                        "stale-input");

                AssertStatus(
                    staleInputResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleInputBinding,
                    "Stale gameplay input evidence was accepted.");
                completed.Add("live-input-authority-rejects-stale-evidence");

                PlayerGameplayCameraEligibilityResult requiredSkip =
                    SkipOptional(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        PlayerGameplayCameraRequiredness.Required,
                        "skip-required-camera");

                AssertStatus(
                    requiredSkip,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedOptionalSkipRequired,
                    "Required camera policy was skipped.");
                completed.Add("required-camera-cannot-skip");

                PlayerGameplayCameraEligibilityResult skipped =
                    SkipOptional(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        PlayerGameplayCameraRequiredness.Optional,
                        "skip-optional-camera");

                AssertStatus(
                    skipped,
                    PlayerGameplayCameraEligibilityStatus
                        .SucceededSkippedOptional,
                    "Optional camera skip failed.");
                AssertTrue(
                    skipped.CurrentSummary.IsSkippedOptional,
                    "Optional camera skip did not publish SkippedOptional evidence.");
                completed.Add("optional-camera-skip-succeeds");

                PlayerGameplayCameraEligibilityToken skipToken =
                    skipped.CurrentSummary.Token;

                PlayerGameplayCameraEligibilityResult skippedAgain =
                    SkipOptional(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        PlayerGameplayCameraRequiredness.Optional,
                        "skip-optional-camera-again");

                AssertStatus(
                    skippedAgain,
                    PlayerGameplayCameraEligibilityStatus
                        .SucceededAlreadySkipped,
                    "Repeated optional camera skip was not idempotent.");
                AssertEqual(
                    skipToken,
                    skippedAgain.CurrentSummary.Token,
                    "Idempotent skip changed eligibility token.");
                completed.Add("optional-camera-skip-idempotent");

                PlayerGameplayCameraEligibilityResult invalidSkipRelease =
                    Release(
                        cameraContextType,
                        cameraContext,
                        slotOne,
                        default,
                        "invalid-skip-release");

                AssertStatus(
                    invalidSkipRelease,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleEligibility,
                    "Skipped camera accepted an invalid release token.");
                completed.Add("optional-skip-release-token-guarded");

                PlayerGameplayCameraEligibilityResult releasedSkip =
                    Release(
                        cameraContextType,
                        cameraContext,
                        slotOne,
                        skipToken,
                        "release-skip");

                AssertStatus(
                    releasedSkip,
                    PlayerGameplayCameraEligibilityStatus.SucceededReleased,
                    "Optional skip release failed.");
                completed.Add("optional-skip-released");

                PlayerGameplayCameraEligibilityResult releasedSkipAgain =
                    Release(
                        cameraContextType,
                        cameraContext,
                        slotOne,
                        default,
                        "release-skip-again");

                AssertStatus(
                    releasedSkipAgain,
                    PlayerGameplayCameraEligibilityStatus
                        .SucceededAlreadyReleased,
                    "Repeated optional skip release was not idempotent.");
                completed.Add("optional-skip-release-idempotent");

                CameraFixture fixture = CameraFixture.Create(
                    preparedOne.Materialization.ActorId,
                    PlayerGameplayCameraRequiredness.Required,
                    created);

                PlayerGameplayCameraEligibilityResult eligible =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        fixture.ActorDeclaration,
                        fixture.Authoring,
                        "confirm-required-camera");

                AssertStatus(
                    eligible,
                    PlayerGameplayCameraEligibilityStatus.SucceededEligible,
                    "Coherent required camera authoring was not eligible.");
                AssertTrue(
                    eligible.CurrentSummary.IsEligible,
                    "Camera eligibility did not publish Eligible evidence.");
                AssertTrue(
                    eligible.CurrentSummary.IsRequired,
                    "Required camera authoring lost requiredness.");
                AssertEqual(
                    inputTwo.Token,
                    eligible.CurrentSummary.InputBindingToken,
                    "Camera eligibility lost exact input binding token.");
                AssertTrue(
                    !string.IsNullOrEmpty(
                        eligible.CurrentSummary.RequestId) &&
                    !string.IsNullOrEmpty(
                        eligible.CurrentSummary.LifetimeScopeId) &&
                    !string.IsNullOrEmpty(
                        eligible.CurrentSummary.TieBreakerId),
                    "Camera eligibility did not derive request identities.");
                completed.Add("prepared-required-camera-authoring-eligible");

                PlayerGameplayCameraEligibilityToken firstEligibilityToken =
                    eligible.CurrentSummary.Token;

                PlayerGameplayCameraEligibilityResult eligibleAgain =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        fixture.ActorDeclaration,
                        fixture.Authoring,
                        "confirm-required-camera-again");

                AssertStatus(
                    eligibleAgain,
                    PlayerGameplayCameraEligibilityStatus
                        .SucceededAlreadyEligible,
                    "Repeated camera eligibility was not idempotent.");
                AssertEqual(
                    firstEligibilityToken,
                    eligibleAgain.CurrentSummary.Token,
                    "Idempotent camera eligibility changed token.");
                completed.Add("camera-eligibility-idempotent");

                PlayerActorDeclaration wrongActor =
                    CreateActorDeclaration(
                        new GameObject(
                            "P3K.4 Wrong Actor").transform,
                        ActorId.From("actor.runtime.wrong"),
                        "P3K.4 Wrong Actor",
                        created);

                PlayerGameplayCameraEligibilityResult wrongActorResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        wrongActor,
                        fixture.Authoring,
                        "wrong-actor");

                AssertStatus(
                    wrongActorResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedActorMismatch,
                    "Foreign Actor identity was accepted.");
                completed.Add("actor-identity-mismatch-rejected");

                PlayerGameplayCameraAuthoring outsideAuthoring =
                    CreateOutsideAuthoring(
                        fixture,
                        created);

                PlayerGameplayCameraEligibilityResult outsideResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        fixture.ActorDeclaration,
                        outsideAuthoring,
                        "outside-authoring");

                AssertStatus(
                    outsideResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedAuthoringHierarchyMismatch,
                    "Authoring outside the prepared Actor was accepted.");
                completed.Add("authoring-hierarchy-mismatch-rejected");

                SetField(
                    fixture.Rig,
                    "targetSourceKind",
                    CameraTargetSourceKind.PlayerComposer);

                PlayerGameplayCameraEligibilityResult playerComposerRigResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        fixture.ActorDeclaration,
                        fixture.Authoring,
                        "player-composer-rig");

                AssertStatus(
                    playerComposerRigResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigUsesPlayerComposer,
                    "PlayerComposer-backed rig was accepted.");
                completed.Add("playercomposer-rig-source-rejected");

                SetField(
                    fixture.Rig,
                    "targetSourceKind",
                    CameraTargetSourceKind.ExplicitTransform);

                Transform alternateFollow =
                    CreateChild(
                        fixture.Root.transform,
                        "P3K.4 Alternate Follow",
                        created);

                SetField(
                    fixture.Authoring,
                    "followTarget",
                    alternateFollow);

                PlayerGameplayCameraEligibilityResult targetMismatchResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        fixture.ActorDeclaration,
                        fixture.Authoring,
                        "rig-target-mismatch");

                AssertStatus(
                    targetMismatchResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigTargetMismatch,
                    "Mismatched rig and authoring targets were accepted.");
                completed.Add("rig-target-mismatch-rejected");

                SetField(
                    fixture.Authoring,
                    "followTarget",
                    fixture.FollowTarget);

                SetField(
                    fixture.Authoring,
                    "cameraRig",
                    null);

                PlayerGameplayCameraEligibilityResult missingRigResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        fixture.ActorDeclaration,
                        fixture.Authoring,
                        "required-missing-rig");

                AssertStatus(
                    missingRigResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedRigMissing,
                    "Required camera authoring without rig was accepted.");
                completed.Add("required-missing-rig-rejected");

                SetField(
                    fixture.Authoring,
                    "cameraRig",
                    fixture.Rig);

                PlayerGameplayCameraEligibilityResult invalidRelease =
                    Release(
                        cameraContextType,
                        cameraContext,
                        slotOne,
                        default,
                        "invalid-eligibility-release");

                AssertStatus(
                    invalidRelease,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleEligibility,
                    "Eligible camera accepted an invalid release token.");
                AssertEqual(
                    firstEligibilityToken,
                    invalidRelease.CurrentSummary.Token,
                    "Rejected release changed camera eligibility.");
                completed.Add("eligibility-release-token-guarded");

                PlayerGameplayCameraEligibilityResult released =
                    Release(
                        cameraContextType,
                        cameraContext,
                        slotOne,
                        firstEligibilityToken,
                        "release-eligibility");

                AssertStatus(
                    released,
                    PlayerGameplayCameraEligibilityStatus.SucceededReleased,
                    "Camera eligibility release failed.");
                completed.Add("camera-eligibility-released");

                PlayerGameplayCameraEligibilityResult releasedAgain =
                    Release(
                        cameraContextType,
                        cameraContext,
                        slotOne,
                        default,
                        "release-eligibility-again");

                AssertStatus(
                    releasedAgain,
                    PlayerGameplayCameraEligibilityStatus
                        .SucceededAlreadyReleased,
                    "Repeated camera eligibility release was not idempotent.");
                completed.Add("camera-eligibility-release-idempotent");

                PlayerGameplayCameraEligibilityResult reeligible =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        fixture.ActorDeclaration,
                        fixture.Authoring,
                        "reconfirm-camera");

                AssertStatus(
                    reeligible,
                    PlayerGameplayCameraEligibilityStatus.SucceededEligible,
                    "Released camera eligibility could not be re-established.");
                AssertTrue(
                    reeligible.CurrentSummary.Token != firstEligibilityToken,
                    "Re-eligibility reused stale token.");
                completed.Add("reeligibility-generates-new-token");

                PlayerGameplayCameraEligibilityResult staleRelease =
                    Release(
                        cameraContextType,
                        cameraContext,
                        slotOne,
                        firstEligibilityToken,
                        "stale-token-after-reeligibility");

                AssertStatus(
                    staleRelease,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleEligibility,
                    "Stale camera eligibility token was accepted.");
                AssertEqual(
                    reeligible.CurrentSummary.Token,
                    staleRelease.CurrentSummary.Token,
                    "Stale release disturbed current eligibility.");
                completed.Add("stale-token-after-reeligibility-rejected");

                bool releasedAll = ReleaseAll(
                    cameraContextType,
                    cameraContext,
                    out int releasedCount,
                    out int failedCount,
                    out string releaseAllIssue);

                AssertTrue(
                    releasedAll,
                    $"Camera eligibility release-all failed. {releaseAllIssue}");
                AssertEqual(
                    1,
                    releasedCount,
                    "Release-all did not release current eligibility.");
                AssertEqual(
                    0,
                    failedCount,
                    "Release-all reported camera eligibility failures.");
                completed.Add("all-camera-eligibilities-released");

                PlayerGameplayOccupancyResult occupancyReleased =
                    ReleaseOccupancy(
                        occupancyContextType,
                        occupancyContext,
                        slotOne,
                        occupiedOne.CurrentSummary.Token,
                        "release-occupancy-before-stale-camera");

                AssertEqual(
                    PlayerGameplayOccupancyStatus.SucceededReleased,
                    occupancyReleased.Status,
                    "P3K.4 fixture could not release occupancy.");

                PlayerGameplayCameraEligibilityResult staleOccupancyResult =
                    ConfirmEligibility(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        inputTwo,
                        fixture.ActorDeclaration,
                        fixture.Authoring,
                        "stale-occupancy");

                AssertStatus(
                    staleOccupancyResult,
                    PlayerGameplayCameraEligibilityStatus
                        .RejectedForeignOrStaleOccupancy,
                    "Released occupancy evidence was accepted.");
                completed.Add("live-occupancy-authority-rejects-stale-evidence");

                PlayerGameplayCameraEligibilitySnapshot final =
                    CameraSnapshot(
                        cameraContextType,
                        cameraContext);

                AssertEqual(
                    0,
                    final.EligibleCount,
                    "P3K.4 final snapshot retained Eligible camera evidence.");
                AssertEqual(
                    0,
                    final.SkippedOptionalCount,
                    "P3K.4 final snapshot retained skipped camera evidence.");
                AssertEqual(
                    2,
                    final.NotEvaluatedCount,
                    "P3K.4 final snapshot did not restore all Slots.");

                Debug.Log(
                    "[P3K4_PREPARED_PLAYER_CAMERA_ELIGIBILITY_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"slot='{slotOne.StableText}' " +
                    $"actor='{preparedOne.Materialization.ActorId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K4_PREPARED_PLAYER_CAMERA_ELIGIBILITY_SMOKE] " +
                    $"status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                for (int index = created.Count - 1; index >= 0; index--)
                {
                    UnityEngine.Object target = created[index];
                    if (target != null)
                    {
                        UnityEngine.Object.DestroyImmediate(target);
                    }
                }
            }
        }

        private static Type ResolveType(
            Type publicContract,
            string typeName)
        {
            Type type = publicContract.Assembly.GetType(
                typeName,
                false);
            AssertNotNull(
                type,
                $"Required runtime type '{typeName}' is missing.");
            return type;
        }

        private static void ValidateContractSurface(
            Type contextType)
        {
            AssertTrue(
                !contextType.IsPublic,
                "P3K.4 runtime context must remain internal.");
            AssertNotNull(
                contextType.GetMethod(
                    "TryCreate",
                    StaticAny),
                "P3K.4 runtime context has no TryCreate.");
            AssertNotNull(
                contextType.GetMethod(
                    "TryConfirmEligibility",
                    InstanceAny),
                "P3K.4 runtime context has no eligibility operation.");
            AssertNotNull(
                contextType.GetMethod(
                    "TrySkipOptional",
                    InstanceAny),
                "P3K.4 runtime context has no optional skip.");
            AssertNotNull(
                contextType.GetMethod(
                    "TryRelease",
                    InstanceAny),
                "P3K.4 runtime context has no release.");
            AssertNotNull(
                contextType.GetMethod(
                    "TryReleaseAll",
                    InstanceAny),
                "P3K.4 runtime context has no release-all.");
            AssertNotNull(
                contextType.GetMethod(
                    "CreateSnapshot",
                    InstanceAny),
                "P3K.4 runtime context has no snapshot.");
            AssertNotNull(
                contextType.GetMethod(
                    "TryGetEligibilityEvidence",
                    InstanceAny),
                "P3K.4 runtime context has no physical evidence boundary.");
            AssertTrue(
                typeof(PlayerGameplayCameraEligibilityToken).IsValueType,
                "P3K.4 eligibility token must be a value type.");
            AssertTrue(
                typeof(PlayerGameplayCameraEligibilitySummary).IsValueType,
                "P3K.4 eligibility summary must be a value type.");
        }

        private static object CreateOccupancyContext(
            Type contextType,
            PlayerActorPreparationSnapshot snapshot)
        {
            MethodInfo method = contextType.GetMethod(
                "TryCreate",
                StaticAny);
            object[] arguments =
            {
                snapshot,
                null,
                null
            };
            bool succeeded = (bool)method.Invoke(
                null,
                arguments);
            AssertTrue(
                succeeded,
                $"P3K.2 occupancy context creation failed. {arguments[2]}");
            return arguments[1];
        }

        private static PlayerGameplayOccupancyResult ConfirmOccupancy(
            Type contextType,
            object context,
            PlayerActorPreparationSummary preparation,
            string reason)
        {
            return (PlayerGameplayOccupancyResult)contextType
                .GetMethod(
                    "TryConfirmOccupancy",
                    InstanceAny)
                .Invoke(
                    context,
                    new object[]
                    {
                        preparation,
                        nameof(
                            QaP3K4PreparedPlayerCameraEligibilitySmoke),
                        reason
                    });
        }

        private static PlayerGameplayOccupancyResult ReleaseOccupancy(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayOccupancyToken token,
            string reason)
        {
            return (PlayerGameplayOccupancyResult)contextType
                .GetMethod(
                    "TryReleaseOccupancy",
                    InstanceAny)
                .Invoke(
                    context,
                    new object[]
                    {
                        playerSlotId,
                        token,
                        nameof(
                            QaP3K4PreparedPlayerCameraEligibilitySmoke),
                        reason
                    });
        }

        private static PlayerGameplayOccupancySnapshot OccupancySnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayOccupancySnapshot)contextType
                .GetMethod(
                    "CreateSnapshot",
                    InstanceAny)
                .Invoke(
                    context,
                    Array.Empty<object>());
        }

        private static object CreateInputContext(
            Type contextType,
            object occupancyContext)
        {
            MethodInfo method = contextType.GetMethod(
                "TryCreate",
                StaticAny);
            object[] arguments =
            {
                occupancyContext,
                null,
                null
            };
            bool succeeded = (bool)method.Invoke(
                null,
                arguments);
            AssertTrue(
                succeeded,
                $"P3K.3 input context creation failed. {arguments[2]}");
            return arguments[1];
        }

        private static PlayerGameplayInputBindingSnapshot InputSnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayInputBindingSnapshot)contextType
                .GetMethod(
                    "CreateSnapshot",
                    InstanceAny)
                .Invoke(
                    context,
                    Array.Empty<object>());
        }

        private static object CreateCameraContext(
            Type contextType,
            object occupancyContext,
            object inputContext)
        {
            MethodInfo method = contextType.GetMethod(
                "TryCreate",
                StaticAny);
            object[] arguments =
            {
                occupancyContext,
                inputContext,
                null,
                null
            };
            bool succeeded = (bool)method.Invoke(
                null,
                arguments);
            AssertTrue(
                succeeded,
                $"P3K.4 camera context creation failed. {arguments[3]}");
            return arguments[2];
        }

        private static PlayerGameplayCameraEligibilityResult
            ConfirmEligibility(
                Type contextType,
                object context,
                PlayerActorPreparationSummary preparation,
                PlayerGameplayOccupancySummary occupancy,
                PlayerGameplayInputBindingSummary inputBinding,
                PlayerActorDeclaration actorDeclaration,
                PlayerGameplayCameraAuthoring authoring,
                string reason)
        {
            return (PlayerGameplayCameraEligibilityResult)contextType
                .GetMethod(
                    "TryConfirmEligibility",
                    InstanceAny)
                .Invoke(
                    context,
                    new object[]
                    {
                        preparation,
                        occupancy,
                        inputBinding,
                        actorDeclaration,
                        authoring,
                        nameof(
                            QaP3K4PreparedPlayerCameraEligibilitySmoke),
                        reason
                    });
        }

        private static PlayerGameplayCameraEligibilityResult SkipOptional(
            Type contextType,
            object context,
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary inputBinding,
            PlayerGameplayCameraRequiredness requiredness,
            string reason)
        {
            return (PlayerGameplayCameraEligibilityResult)contextType
                .GetMethod(
                    "TrySkipOptional",
                    InstanceAny)
                .Invoke(
                    context,
                    new object[]
                    {
                        preparation,
                        occupancy,
                        inputBinding,
                        requiredness,
                        nameof(
                            QaP3K4PreparedPlayerCameraEligibilitySmoke),
                        reason
                    });
        }

        private static PlayerGameplayCameraEligibilityResult Release(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayCameraEligibilityToken token,
            string reason)
        {
            return (PlayerGameplayCameraEligibilityResult)contextType
                .GetMethod(
                    "TryRelease",
                    InstanceAny)
                .Invoke(
                    context,
                    new object[]
                    {
                        playerSlotId,
                        token,
                        nameof(
                            QaP3K4PreparedPlayerCameraEligibilitySmoke),
                        reason
                    });
        }

        private static bool ReleaseAll(
            Type contextType,
            object context,
            out int releasedCount,
            out int failedCount,
            out string issue)
        {
            object[] arguments =
            {
                nameof(
                    QaP3K4PreparedPlayerCameraEligibilitySmoke),
                "release-all-camera-eligibility",
                0,
                0,
                null
            };

            bool succeeded = (bool)contextType
                .GetMethod(
                    "TryReleaseAll",
                    InstanceAny)
                .Invoke(
                    context,
                    arguments);

            releasedCount = (int)arguments[2];
            failedCount = (int)arguments[3];
            issue = arguments[4] as string ?? string.Empty;
            return succeeded;
        }

        private static PlayerGameplayCameraEligibilitySnapshot
            CameraSnapshot(
                Type contextType,
                object context)
        {
            return (PlayerGameplayCameraEligibilitySnapshot)contextType
                .GetMethod(
                    "CreateSnapshot",
                    InstanceAny)
                .Invoke(
                    context,
                    Array.Empty<object>());
        }

        private static PlayerGameplayInputBindingSummary
            CreateBoundInputSummary(
                PlayerActorPreparationSummary preparation,
                PlayerGameplayOccupancySummary occupancy,
                int bindingRevision)
        {
            ConstructorInfo tokenConstructor =
                typeof(PlayerGameplayInputBindingToken).GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(RuntimeContentOwner),
                        typeof(PlayerSlotId),
                        typeof(ActorProfileId),
                        typeof(ActorId),
                        typeof(PlayerActorPreparationToken),
                        typeof(PlayerGameplayOccupancyToken),
                        typeof(RuntimeContentIdentity),
                        typeof(int),
                        typeof(int),
                        typeof(int)
                    },
                    null);

            AssertNotNull(
                tokenConstructor,
                "P3K.3 input binding token constructor changed.");

            var token =
                (PlayerGameplayInputBindingToken)tokenConstructor.Invoke(
                    new object[]
                    {
                        preparation.SessionContextId,
                        occupancy.Owner,
                        occupancy.PlayerSlotId,
                        occupancy.ActorProfileId,
                        occupancy.ActorId,
                        occupancy.PreparationToken,
                        occupancy.Token,
                        occupancy.RuntimeContentIdentity,
                        preparation.Materialization
                            .MaterializationRevision,
                        occupancy.OccupancyRevision,
                        bindingRevision
                    });

            ConstructorInfo summaryConstructor =
                typeof(PlayerGameplayInputBindingSummary).GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(PlayerSlotId),
                        typeof(PlayerGameplayInputBindingState),
                        typeof(PlayerGameplayInputAvailability),
                        typeof(ActorProfileId),
                        typeof(ActorId),
                        typeof(RuntimeContentOwner),
                        typeof(RuntimeContentIdentity),
                        typeof(PlayerActorPreparationToken),
                        typeof(PlayerGameplayOccupancyToken),
                        typeof(PlayerGameplayInputBindingToken),
                        typeof(string),
                        typeof(string),
                        typeof(string),
                        typeof(int),
                        typeof(string),
                        typeof(string),
                        typeof(string)
                    },
                    null);

            AssertNotNull(
                summaryConstructor,
                "P3K.3 input binding summary constructor changed.");

            var summary =
                (PlayerGameplayInputBindingSummary)summaryConstructor.Invoke(
                    new object[]
                    {
                        preparation.SessionContextId,
                        occupancy.PlayerSlotId,
                        PlayerGameplayInputBindingState.Bound,
                        PlayerGameplayInputAvailability.Allowed,
                        occupancy.ActorProfileId,
                        occupancy.ActorId,
                        occupancy.Owner,
                        occupancy.RuntimeContentIdentity,
                        occupancy.PreparationToken,
                        occupancy.Token,
                        token,
                        "Player",
                        "UI",
                        "P3K.4 Synthetic PlayerInput",
                        bindingRevision,
                        nameof(
                            QaP3K4PreparedPlayerCameraEligibilitySmoke),
                        "synthetic-bound-input",
                        "Synthetic current gameplay input binding."
                    });

            AssertTrue(
                summary.IsValid,
                "Synthetic P3K.3 input binding summary is invalid.");
            return summary;
        }

        private static void InjectInputSummary(
            Type inputContextType,
            object inputContext,
            PlayerGameplayInputBindingSummary summary)
        {
            FieldInfo slotsField = inputContextType.GetField(
                "slots",
                InstanceAny);
            AssertNotNull(
                slotsField,
                "P3K.3 input context Slots field changed.");

            var summaries =
                slotsField.GetValue(inputContext) as
                    Dictionary<
                        PlayerSlotId,
                        PlayerGameplayInputBindingSummary>;

            AssertNotNull(
                summaries,
                "P3K.3 input context Slots dictionary is unavailable.");

            summaries[summary.PlayerSlotId] = summary;

            FieldInfo revisionField = inputContextType.GetField(
                "revision",
                InstanceAny);
            AssertNotNull(
                revisionField,
                "P3K.3 input context revision field changed.");
            revisionField.SetValue(
                inputContext,
                Math.Max(
                    (int)revisionField.GetValue(inputContext) + 1,
                    summary.BindingRevision + 1));
        }

        private static PlayerActorPreparationSummary CreatePreparation(
            string sessionId,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            int revision)
        {
            PlayerActorMaterializationOperationId operationId =
                CreateOperationId(
                    sessionId,
                    owner,
                    playerSlotId,
                    revision);

            RuntimeContentIdentity identity =
                RuntimeContentIdentity.From(
                    owner,
                    $"qa.p3k4.content.{playerSlotId.Value.Value}.{revision}");

            PlayerActorMaterializationSnapshot materialization =
                CreateMaterializationSnapshot(
                    operationId,
                    identity,
                    playerSlotId,
                    actorProfileId,
                    actorId,
                    revision);

            ConstructorInfo constructor =
                typeof(PlayerActorPreparationSummary).GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(PlayerSlotId),
                        typeof(PlayerActorPreparationState),
                        typeof(ActorProfileId),
                        typeof(int),
                        typeof(PlayerActorMaterializationSnapshot),
                        typeof(string),
                        typeof(string),
                        typeof(string)
                    },
                    null);

            AssertNotNull(
                constructor,
                "P3J.4 preparation summary constructor changed.");

            return (PlayerActorPreparationSummary)constructor.Invoke(
                new object[]
                {
                    sessionId,
                    playerSlotId,
                    PlayerActorPreparationState.Prepared,
                    actorProfileId,
                    revision,
                    materialization,
                    nameof(
                        QaP3K4PreparedPlayerCameraEligibilitySmoke),
                    "synthetic-preparation",
                    "Synthetic current Player Actor preparation."
                });
        }

        private static PlayerActorMaterializationOperationId
            CreateOperationId(
                string sessionId,
                RuntimeContentOwner owner,
                PlayerSlotId playerSlotId,
                int revision)
        {
            MethodInfo method =
                typeof(PlayerActorMaterializationOperationId)
                    .GetMethod(
                        "TryCreate",
                        StaticAny);

            object[] arguments =
            {
                sessionId,
                owner,
                playerSlotId,
                revision,
                default(PlayerActorMaterializationOperationId),
                null
            };

            bool succeeded = (bool)method.Invoke(
                null,
                arguments);

            AssertTrue(
                succeeded,
                $"Synthetic materialization identity failed. {arguments[5]}");

            return
                (PlayerActorMaterializationOperationId)arguments[4];
        }

        private static PlayerActorMaterializationSnapshot
            CreateMaterializationSnapshot(
                PlayerActorMaterializationOperationId operationId,
                RuntimeContentIdentity identity,
                PlayerSlotId playerSlotId,
                ActorProfileId actorProfileId,
                ActorId actorId,
                int revision)
        {
            ConstructorInfo constructor =
                typeof(PlayerActorMaterializationSnapshot).GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(PlayerActorMaterializationOperationId),
                        typeof(RuntimeContentIdentity),
                        typeof(PlayerSlotId),
                        typeof(ActorProfileId),
                        typeof(ActorId),
                        typeof(int),
                        typeof(PlayerActorMaterializationState),
                        typeof(string),
                        typeof(string)
                    },
                    null);

            AssertNotNull(
                constructor,
                "P3J.3 materialization snapshot constructor changed.");

            return
                (PlayerActorMaterializationSnapshot)constructor.Invoke(
                    new object[]
                    {
                        operationId,
                        identity,
                        playerSlotId,
                        actorProfileId,
                        actorId,
                        revision,
                        PlayerActorMaterializationState.Active,
                        nameof(
                            QaP3K4PreparedPlayerCameraEligibilitySmoke),
                        "synthetic-materialization"
                    });
        }

        private static PlayerActorPreparationSnapshot
            CreatePreparationSnapshot(
                string sessionId,
                params PlayerActorPreparationSummary[] preparations)
        {
            ConstructorInfo constructor =
                typeof(PlayerActorPreparationSnapshot).GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(int),
                        typeof(PlayerActorPreparationSummary[]),
                        typeof(PlayerActorMaterializationSnapshot[]),
                        typeof(PlayerActorPreparationStatus),
                        typeof(string)
                    },
                    null);

            AssertNotNull(
                constructor,
                "P3J.4 preparation snapshot constructor changed.");

            return
                (PlayerActorPreparationSnapshot)constructor.Invoke(
                    new object[]
                    {
                        sessionId,
                        1,
                        preparations,
                        Array.Empty<
                            PlayerActorMaterializationSnapshot>(),
                        PlayerActorPreparationStatus.SucceededPrepared,
                        "Synthetic preparation snapshot."
                    });
        }

        private static PlayerActorDeclaration CreateActorDeclaration(
            Transform parent,
            ActorId actorId,
            string label,
            List<UnityEngine.Object> created)
        {
            GameObject actorObject = parent.gameObject;
            if (!created.Contains(actorObject))
            {
                created.Add(actorObject);
            }

            PlayerActorDeclaration declaration =
                actorObject.GetComponent<PlayerActorDeclaration>();

            if (declaration == null)
            {
                declaration =
                    actorObject.AddComponent<PlayerActorDeclaration>();
            }

            MethodInfo configure =
                typeof(PlayerActorDeclaration).GetMethod(
                    "ConfigureForDiagnostics",
                    InstanceAny | BindingFlags.DeclaredOnly,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(string),
                        typeof(PlayerInput),
                        typeof(string)
                    },
                    null);

            AssertNotNull(
                configure,
                "PlayerActorDeclaration diagnostic configuration is missing.");

            configure.Invoke(
                declaration,
                new object[]
                {
                    actorId.Value.Value,
                    label,
                    null,
                    "qa.p3k4.actor-declaration"
                });

            return declaration;
        }

        private static PlayerGameplayCameraAuthoring CreateOutsideAuthoring(
            CameraFixture source,
            List<UnityEngine.Object> created)
        {
            GameObject outside = new GameObject(
                "P3K.4 Outside Camera Authoring");
            created.Add(outside);

            PlayerGameplayCameraAuthoring authoring =
                outside.AddComponent<PlayerGameplayCameraAuthoring>();

            SetField(
                authoring,
                "requiredness",
                PlayerGameplayCameraRequiredness.Required);
            SetField(
                authoring,
                "cameraRig",
                source.Rig);
            SetField(
                authoring,
                "followTarget",
                source.FollowTarget);
            SetField(
                authoring,
                "lookAtTarget",
                source.LookAtTarget);
            SetField(
                authoring,
                "precedence",
                50);

            return authoring;
        }

        private static Transform CreateChild(
            Transform parent,
            string name,
            List<UnityEngine.Object> created)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(
                parent,
                false);
            created.Add(child);
            return child.transform;
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();

            while (type != null)
            {
                FieldInfo field = type.GetField(
                    fieldName,
                    InstanceAny);

                if (field != null)
                {
                    field.SetValue(
                        target,
                        value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private static void AssertStatus(
            PlayerGameplayCameraEligibilityResult result,
            PlayerGameplayCameraEligibilityStatus expected,
            string message)
        {
            AssertNotNull(
                result,
                message + " Result is null.");

            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} Expected='{expected}' " +
                    $"Actual='{result.Status}'. " +
                    result.ToDiagnosticString());
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
                    $"{message} Expected='{expected}' Actual='{actual}'.");
            }
        }

        private static string Escape(
            string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value
                    .Replace(
                        "'",
                        "\\'")
                    .Replace(
                        "\r",
                        " ")
                    .Replace(
                        "\n",
                        " ");
        }

        private sealed class CameraFixture
        {
            internal GameObject Root { get; private set; }
            internal PlayerActorDeclaration ActorDeclaration
            {
                get;
                private set;
            }
            internal CameraRigComposer Rig { get; private set; }
            internal PlayerGameplayCameraAuthoring Authoring
            {
                get;
                private set;
            }
            internal Transform FollowTarget { get; private set; }
            internal Transform LookAtTarget { get; private set; }

            internal static CameraFixture Create(
                ActorId actorId,
                PlayerGameplayCameraRequiredness requiredness,
                List<UnityEngine.Object> created)
            {
                var fixture = new CameraFixture();

                fixture.Root = new GameObject(
                    "P3K.4 Prepared Logical Player Actor");
                created.Add(fixture.Root);

                fixture.ActorDeclaration =
                    CreateActorDeclaration(
                        fixture.Root.transform,
                        actorId,
                        fixture.Root.name,
                        created);

                fixture.FollowTarget = CreateChild(
                    fixture.Root.transform,
                    "P3K.4 Follow Target",
                    created);

                fixture.LookAtTarget = CreateChild(
                    fixture.Root.transform,
                    "P3K.4 LookAt Target",
                    created);

                Transform rigTransform = CreateChild(
                    fixture.Root.transform,
                    "P3K.4 Camera Rig",
                    created);

                fixture.Rig =
                    rigTransform.gameObject
                        .AddComponent<CameraRigComposer>();

                SetField(
                    fixture.Rig,
                    "presentationIntent",
                    CameraRigPresentationIntent.Follow);
                SetField(
                    fixture.Rig,
                    "targetSourceKind",
                    CameraTargetSourceKind.ExplicitTransform);
                SetField(
                    fixture.Rig,
                    "playerComposer",
                    null);
                SetField(
                    fixture.Rig,
                    "explicitFollowTarget",
                    fixture.FollowTarget);
                SetField(
                    fixture.Rig,
                    "explicitLookAtTarget",
                    fixture.LookAtTarget);
                SetField(
                    fixture.Rig,
                    "followRequirement",
                    CameraTargetRequirement.Required);
                SetField(
                    fixture.Rig,
                    "lookAtRequirement",
                    CameraTargetRequirement.Optional);

                Transform authoringTransform = CreateChild(
                    fixture.Root.transform,
                    "P3K.4 Camera Authoring",
                    created);

                fixture.Authoring =
                    authoringTransform.gameObject
                        .AddComponent<PlayerGameplayCameraAuthoring>();

                SetField(
                    fixture.Authoring,
                    "requiredness",
                    requiredness);
                SetField(
                    fixture.Authoring,
                    "cameraRig",
                    fixture.Rig);
                SetField(
                    fixture.Authoring,
                    "followTarget",
                    fixture.FollowTarget);
                SetField(
                    fixture.Authoring,
                    "lookAtTarget",
                    fixture.LookAtTarget);
                SetField(
                    fixture.Authoring,
                    "precedence",
                    50);

                return fixture;
            }
        }
    }
}

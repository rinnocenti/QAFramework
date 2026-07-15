using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.RuntimeContent;
using Immersive.Framework.UnityInput;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player.Editor
{
    /// <summary>
    /// Play Mode end-to-end technical smoke for P3K.5 gameplay admission,
    /// local Player camera publication, derived readiness and reverse release.
    /// </summary>
    public static class QaP3K5GameplayAdmissionSmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.5 Run Gameplay Admission and Camera Publication Smoke";
        private const string OccupancyContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayOccupancyRuntimeContext";
        private const string InputContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayInputBindingRuntimeContext";
        private const string CameraContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayCameraEligibilityRuntimeContext";
        private const string AdmissionContextTypeName =
            "Immersive.Framework.PlayerParticipation.PlayerGameplayAdmissionRuntimeContext";

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
                    "P3K.5 gameplay admission smoke must run in Play Mode.");

                Type occupancyContextType = ResolveType(
                    typeof(PlayerGameplayOccupancyStatus),
                    OccupancyContextTypeName);
                Type inputContextType = ResolveType(
                    typeof(PlayerGameplayInputBindingStatus),
                    InputContextTypeName);
                Type cameraContextType = ResolveType(
                    typeof(PlayerGameplayCameraEligibilityStatus),
                    CameraContextTypeName);
                Type admissionContextType = ResolveType(
                    typeof(PlayerGameplayAdmissionStatus),
                    AdmissionContextTypeName);

                ValidateContractSurface(admissionContextType);
                completed.Add("contract-surface-valid");

                const string sessionId = "qa.p3k5.session";
                PlayerSlotId slotOne = PlayerSlotId.From("player.1");
                PlayerSlotId slotTwo = PlayerSlotId.From("player.2");
                RuntimeContentOwner owner = RuntimeContentOwner.Activity(
                    "qa.p3k5.activity",
                    "P3K.5 Activity");

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
                object inputContext = CreateInputContext(
                    inputContextType,
                    occupancyContext);
                object cameraContext = CreateCameraContext(
                    cameraContextType,
                    occupancyContext,
                    inputContext);
                object admissionContext = CreateAdmissionContext(
                    admissionContextType,
                    occupancyContext,
                    inputContext,
                    cameraContext);

                PlayerGameplayAdmissionSnapshot initial = AdmissionSnapshot(
                    admissionContextType,
                    admissionContext);
                AssertTrue(initial.IsInitialized,
                    "P3K.5 admission snapshot is not initialized.");
                AssertEqual(2, initial.ConfiguredSlotCount,
                    "P3K.5 admission context lost configured Slots.");
                AssertEqual(2, initial.NotAdmittedCount,
                    "P3K.5 admission context did not start NotAdmitted.");
                completed.Add("context-initialized-from-live-rosters");

                PlayerGameplayOccupancySnapshot initialOccupancy =
                    OccupancySnapshot(occupancyContextType, occupancyContext);
                PlayerGameplayInputBindingSnapshot initialInput =
                    InputSnapshot(inputContextType, inputContext);
                PlayerGameplayCameraEligibilitySnapshot initialCamera =
                    CameraSnapshot(cameraContextType, cameraContext);

                AssertTrue(initialOccupancy.TryGetSummary(
                        slotTwo,
                        out PlayerGameplayOccupancySummary vacantTwo),
                    "P3K.5 fixture lost vacant Slot two occupancy.");
                AssertTrue(initialInput.TryGetSummary(
                        slotTwo,
                        out PlayerGameplayInputBindingSummary unboundTwo),
                    "P3K.5 fixture lost unbound Slot two input.");
                AssertTrue(initialCamera.TryGetSummary(
                        slotTwo,
                        out PlayerGameplayCameraEligibilitySummary notEvaluatedTwo),
                    "P3K.5 fixture lost Slot two camera decision.");

                PlayerGameplayAdmissionResult vacantAdmission = Admit(
                    admissionContextType,
                    admissionContext,
                    vacantTwo,
                    unboundTwo,
                    notEvaluatedTwo,
                    null,
                    "vacant-admission");
                AssertStatus(
                    vacantAdmission,
                    PlayerGameplayAdmissionStatus.RejectedOccupancyNotReady,
                    "Vacant Slot was gameplay admitted.");
                completed.Add("vacant-occupancy-rejected");

                PlayerGameplayOccupancyResult occupiedOne = ConfirmOccupancy(
                    occupancyContextType,
                    occupancyContext,
                    preparedOne,
                    "occupy-slot-one");
                AssertEqual(
                    PlayerGameplayOccupancyStatus.SucceededOccupied,
                    occupiedOne.Status,
                    "P3K.5 fixture could not occupy Slot one.");

                PlayerGameplayInputBindingSnapshot unboundSnapshot =
                    InputSnapshot(inputContextType, inputContext);
                AssertTrue(unboundSnapshot.TryGetSummary(
                        slotOne,
                        out PlayerGameplayInputBindingSummary unboundOne),
                    "P3K.5 fixture lost unbound Slot one input.");
                PlayerGameplayCameraEligibilitySnapshot cameraNotEvaluated =
                    CameraSnapshot(cameraContextType, cameraContext);
                AssertTrue(cameraNotEvaluated.TryGetSummary(
                        slotOne,
                        out PlayerGameplayCameraEligibilitySummary notEvaluatedOne),
                    "P3K.5 fixture lost Slot one camera decision.");

                PlayerGameplayAdmissionResult unboundAdmission = Admit(
                    admissionContextType,
                    admissionContext,
                    occupiedOne.CurrentSummary,
                    unboundOne,
                    notEvaluatedOne,
                    null,
                    "unbound-admission");
                AssertStatus(
                    unboundAdmission,
                    PlayerGameplayAdmissionStatus.RejectedInputBindingNotReady,
                    "Unbound input was gameplay admitted.");
                completed.Add("unbound-input-rejected");

                using HostFixture host = HostFixture.Create(
                    slotOne,
                    preparedOne.Materialization.ActorId,
                    "UI",
                    "Player",
                    created);

                PlayerGameplayInputBindingResult boundOne = Bind(
                    inputContextType,
                    inputContext,
                    preparedOne,
                    occupiedOne.CurrentSummary,
                    host.Host,
                    host.ActorDeclaration,
                    host.GateAdapter,
                    "bind-slot-one");
                AssertStatus(
                    boundOne,
                    PlayerGameplayInputBindingStatus.SucceededBound,
                    "P3K.5 fixture could not bind gameplay input.");

                PlayerGameplayAdmissionResult missingCameraDecision = Admit(
                    admissionContextType,
                    admissionContext,
                    occupiedOne.CurrentSummary,
                    boundOne.CurrentSummary,
                    notEvaluatedOne,
                    null,
                    "missing-camera-decision");
                AssertStatus(
                    missingCameraDecision,
                    PlayerGameplayAdmissionStatus.RejectedCameraDecisionNotReady,
                    "NotEvaluated camera evidence was gameplay admitted.");
                completed.Add("camera-decision-required");

                PlayerGameplayCameraEligibilityResult skippedOptional =
                    SkipOptional(
                        cameraContextType,
                        cameraContext,
                        preparedOne,
                        occupiedOne.CurrentSummary,
                        boundOne.CurrentSummary,
                        PlayerGameplayCameraRequiredness.Optional,
                        "skip-optional-camera");
                AssertEqual(
                    PlayerGameplayCameraEligibilityStatus
                        .SucceededSkippedOptional,
                    skippedOptional.Status,
                    "P3K.5 fixture could not skip optional camera.");

                PlayerGameplayAdmissionResult optionalReady = Admit(
                    admissionContextType,
                    admissionContext,
                    occupiedOne.CurrentSummary,
                    boundOne.CurrentSummary,
                    skippedOptional.CurrentSummary,
                    null,
                    "admit-skipped-optional");
                AssertStatus(
                    optionalReady,
                    PlayerGameplayAdmissionStatus.SucceededReady,
                    "Skipped optional camera did not produce Ready admission.");
                AssertTrue(optionalReady.CurrentSummary.GameplayReady,
                    "Skipped optional admission did not expose GameplayReady.");
                completed.Add("skipped-optional-admission-ready");

                AssertTrue(
                    !optionalReady.CurrentSummary.CameraRequestPublished,
                    "Skipped optional camera unexpectedly published a request.");
                AssertEqual(
                    string.Empty,
                    optionalReady.CurrentSummary.CameraOutputId,
                    "Skipped optional camera unexpectedly retained output identity.");
                completed.Add("skipped-optional-needs-no-output");

                PlayerGameplayAdmissionToken optionalToken =
                    optionalReady.CurrentSummary.Token;
                PlayerGameplayAdmissionResult optionalAgain = Admit(
                    admissionContextType,
                    admissionContext,
                    occupiedOne.CurrentSummary,
                    boundOne.CurrentSummary,
                    skippedOptional.CurrentSummary,
                    null,
                    "admit-skipped-optional-again");
                AssertStatus(
                    optionalAgain,
                    PlayerGameplayAdmissionStatus.SucceededAlreadyAdmitted,
                    "Repeated optional admission was not idempotent.");
                AssertEqual(
                    optionalToken,
                    optionalAgain.CurrentSummary.Token,
                    "Idempotent optional admission changed token.");
                completed.Add("admission-idempotent");

                SetField(host.GateAdapter, "_isBlockedByAdapter", true);
                PlayerGameplayInputBindingResult blockedInput = RefreshInput(
                    inputContextType,
                    inputContext,
                    slotOne,
                    boundOne.CurrentSummary.Token,
                    "block-input-gate");
                AssertTrue(blockedInput.CurrentSummary.IsBlockedByGate,
                    "P3K.5 fixture did not block gameplay input.");

                PlayerGameplayAdmissionResult blockedAdmission =
                    RefreshAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        optionalToken,
                        "refresh-blocked-readiness");
                AssertStatus(
                    blockedAdmission,
                    PlayerGameplayAdmissionStatus.SucceededReadinessRefreshed,
                    "Blocked Gate readiness refresh failed.");
                AssertTrue(blockedAdmission.CurrentSummary.IsBlockedByInputGate,
                    "Blocked Gate did not clear derived GameplayReady.");
                AssertEqual(optionalToken,
                    blockedAdmission.CurrentSummary.Token,
                    "Gate refresh changed admission token.");
                completed.Add("gate-block-clears-derived-readiness");

                SetField(host.GateAdapter, "_isBlockedByAdapter", false);
                PlayerGameplayInputBindingResult allowedInput = RefreshInput(
                    inputContextType,
                    inputContext,
                    slotOne,
                    boundOne.CurrentSummary.Token,
                    "allow-input-gate");
                AssertTrue(allowedInput.CurrentSummary.IsAllowed,
                    "P3K.5 fixture did not restore gameplay input availability.");

                PlayerGameplayAdmissionResult allowedAdmission =
                    RefreshAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        optionalToken,
                        "refresh-allowed-readiness");
                AssertTrue(allowedAdmission.CurrentSummary.GameplayReady,
                    "Allowed Gate did not restore derived GameplayReady.");
                AssertEqual(optionalToken,
                    allowedAdmission.CurrentSummary.Token,
                    "Allowed Gate refresh changed admission token.");
                completed.Add("gate-release-restores-derived-readiness");

                PlayerGameplayAdmissionResult invalidOptionalRelease =
                    ReleaseAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        default,
                        "invalid-optional-release");
                AssertStatus(
                    invalidOptionalRelease,
                    PlayerGameplayAdmissionStatus
                        .RejectedForeignOrStaleAdmission,
                    "Optional admission accepted invalid release token.");
                completed.Add("release-token-guarded");

                PlayerGameplayAdmissionResult optionalReleased =
                    ReleaseAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        optionalToken,
                        "release-optional-admission");
                AssertStatus(
                    optionalReleased,
                    PlayerGameplayAdmissionStatus.SucceededReleased,
                    "Optional gameplay admission release failed.");
                AssertDependenciesReleased(
                    occupancyContextType,
                    occupancyContext,
                    inputContextType,
                    inputContext,
                    cameraContextType,
                    cameraContext,
                    slotOne,
                    host.PlayerInput);
                completed.Add("optional-release-reverses-chain");

                PlayerGameplayAdmissionResult optionalReleasedAgain =
                    ReleaseAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        default,
                        "release-optional-admission-again");
                AssertStatus(
                    optionalReleasedAgain,
                    PlayerGameplayAdmissionStatus.SucceededAlreadyReleased,
                    "Repeated admission release was not idempotent.");
                completed.Add("release-idempotent");

                using CameraFixture camera = CameraFixture.Create(
                    host.ActorDeclaration.transform,
                    PlayerGameplayCameraRequiredness.Required,
                    created);

                ChainEvidence requiredChain = RebuildChain(
                    occupancyContextType,
                    occupancyContext,
                    inputContextType,
                    inputContext,
                    cameraContextType,
                    cameraContext,
                    preparedOne,
                    host,
                    camera,
                    "required-chain-before-output-failure");

                PlayerGameplayAdmissionResult missingOutput = Admit(
                    admissionContextType,
                    admissionContext,
                    requiredChain.Occupancy,
                    requiredChain.Input,
                    requiredChain.Camera,
                    null,
                    "required-camera-without-output");
                AssertStatus(
                    missingOutput,
                    PlayerGameplayAdmissionStatus.FailedCameraOutputResolution,
                    "Missing output did not fail required camera admission.");
                AssertTrue(missingOutput.RollbackAttempted,
                    "Missing output did not attempt admission rollback.");
                AssertTrue(missingOutput.RollbackSucceeded,
                    $"Missing output rollback failed. {missingOutput.RollbackIssue}");
                completed.Add("missing-output-fails-explicitly");

                AssertDependenciesReleased(
                    occupancyContextType,
                    occupancyContext,
                    inputContextType,
                    inputContext,
                    cameraContextType,
                    cameraContext,
                    slotOne,
                    host.PlayerInput);
                completed.Add("failed-publication-rolls-back-prerequisites");

                ChainEvidence publishedChain = RebuildChain(
                    occupancyContextType,
                    occupancyContext,
                    inputContextType,
                    inputContext,
                    cameraContextType,
                    cameraContext,
                    preparedOne,
                    host,
                    camera,
                    "required-chain-for-publication");

                using CameraOutputFixture output = CameraOutputFixture.Create(
                    "camera.output.p3k5",
                    created);

                PlayerGameplayAdmissionResult published = Admit(
                    admissionContextType,
                    admissionContext,
                    publishedChain.Occupancy,
                    publishedChain.Input,
                    publishedChain.Camera,
                    output.Binding,
                    "publish-required-camera");
                AssertStatus(
                    published,
                    PlayerGameplayAdmissionStatus.SucceededReady,
                    "Required camera admission did not become Ready.");
                AssertTrue(published.CurrentSummary.CameraRequestPublished,
                    "Required camera admission did not publish a request.");
                AssertEqual(output.Binding.OutputIdText,
                    published.CurrentSummary.CameraOutputId,
                    "Published admission retained the wrong output id.");
                completed.Add("eligible-camera-published");

                AssertTrue(output.Binding.Context.HasWinner,
                    "Camera output has no winner after Player publication.");
                AssertEqual(
                    published.CurrentSummary.CameraRequestId,
                    output.Binding.Context.Winner.RequestId.ToString(),
                    "Camera output winner does not match admission request.");
                AssertTrue(output.Binding.Applicator.HasAppliedRequest,
                    "Camera output applicator did not apply Player request.");
                AssertTrue(camera.CinemachineCamera.enabled,
                    "Published Player rig was not enabled by output applicator.");
                completed.Add("camera-output-applied-materialized-rig");

                PlayerGameplayAdmissionToken firstPublishedToken =
                    published.CurrentSummary.Token;
                PlayerGameplayAdmissionResult publishedAgain = Admit(
                    admissionContextType,
                    admissionContext,
                    publishedChain.Occupancy,
                    publishedChain.Input,
                    publishedChain.Camera,
                    output.Binding,
                    "publish-required-camera-again");
                AssertStatus(
                    publishedAgain,
                    PlayerGameplayAdmissionStatus.SucceededAlreadyAdmitted,
                    "Repeated published admission was not idempotent.");
                AssertEqual(firstPublishedToken,
                    publishedAgain.CurrentSummary.Token,
                    "Idempotent camera admission changed token.");
                completed.Add("camera-publication-idempotent");

                SetField(host.GateAdapter, "_isBlockedByAdapter", true);
                PlayerGameplayInputBindingResult publishedBlockedInput =
                    RefreshInput(
                        inputContextType,
                        inputContext,
                        slotOne,
                        publishedChain.Input.Token,
                        "block-published-input");
                PlayerGameplayAdmissionResult publishedBlocked =
                    RefreshAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        firstPublishedToken,
                        "refresh-published-blocked");
                AssertTrue(publishedBlocked.CurrentSummary.IsBlockedByInputGate,
                    "Published admission did not reflect Gate block.");
                AssertTrue(output.Binding.Context.HasWinner,
                    "Gate block incorrectly released Player camera request.");
                AssertTrue(camera.CinemachineCamera.enabled,
                    "Gate block incorrectly disabled published Player rig.");
                completed.Add("gate-block-preserves-camera-publication");

                SetField(host.GateAdapter, "_isBlockedByAdapter", false);
                PlayerGameplayInputBindingResult publishedAllowedInput =
                    RefreshInput(
                        inputContextType,
                        inputContext,
                        slotOne,
                        publishedChain.Input.Token,
                        "allow-published-input");
                AssertTrue(publishedAllowedInput.CurrentSummary.IsAllowed,
                    "Published input did not return to Allowed.");
                PlayerGameplayAdmissionResult publishedReadyAgain =
                    RefreshAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        firstPublishedToken,
                        "refresh-published-ready");
                AssertTrue(publishedReadyAgain.CurrentSummary.GameplayReady,
                    "Published admission did not return to GameplayReady.");
                completed.Add("published-readiness-restored");

                PlayerGameplayAdmissionResult invalidPublishedRelease =
                    ReleaseAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        optionalToken,
                        "stale-token-against-published-admission");
                AssertStatus(
                    invalidPublishedRelease,
                    PlayerGameplayAdmissionStatus
                        .RejectedForeignOrStaleAdmission,
                    "Published admission accepted stale optional token.");
                AssertTrue(output.Binding.Context.HasWinner,
                    "Rejected stale release disturbed camera output.");
                completed.Add("published-release-token-guarded");

                PlayerGameplayAdmissionResult publishedReleased =
                    ReleaseAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        firstPublishedToken,
                        "release-published-admission");
                AssertStatus(
                    publishedReleased,
                    PlayerGameplayAdmissionStatus.SucceededReleased,
                    "Published gameplay admission release failed.");
                AssertTrue(!output.Binding.Context.HasWinner,
                    "Camera request remained after gameplay release.");
                AssertTrue(!output.Binding.Applicator.HasAppliedRequest,
                    "Camera output remained applied after gameplay release.");
                AssertTrue(!camera.CinemachineCamera.enabled,
                    "Player Cinemachine rig remained enabled after release.");
                completed.Add("camera-request-released-first");

                AssertDependenciesReleased(
                    occupancyContextType,
                    occupancyContext,
                    inputContextType,
                    inputContext,
                    cameraContextType,
                    cameraContext,
                    slotOne,
                    host.PlayerInput);
                completed.Add("published-release-reverses-full-chain");

                ChainEvidence secondPublishedChain = RebuildChain(
                    occupancyContextType,
                    occupancyContext,
                    inputContextType,
                    inputContext,
                    cameraContextType,
                    cameraContext,
                    preparedOne,
                    host,
                    camera,
                    "second-published-chain");

                PlayerGameplayAdmissionResult secondPublished = Admit(
                    admissionContextType,
                    admissionContext,
                    secondPublishedChain.Occupancy,
                    secondPublishedChain.Input,
                    secondPublishedChain.Camera,
                    output.Binding,
                    "second-published-admission");
                AssertStatus(
                    secondPublished,
                    PlayerGameplayAdmissionStatus.SucceededReady,
                    "Second published admission failed.");
                AssertTrue(secondPublished.CurrentSummary.Token !=
                    firstPublishedToken,
                    "Re-admission reused stale admission token.");
                completed.Add("readmission-generates-new-token");

                PlayerGameplayAdmissionResult staleAdmissionRelease =
                    ReleaseAdmission(
                        admissionContextType,
                        admissionContext,
                        slotOne,
                        firstPublishedToken,
                        "stale-token-after-readmission");
                AssertStatus(
                    staleAdmissionRelease,
                    PlayerGameplayAdmissionStatus
                        .RejectedForeignOrStaleAdmission,
                    "Stale token released current re-admission.");
                AssertEqual(secondPublished.CurrentSummary.Token,
                    staleAdmissionRelease.CurrentSummary.Token,
                    "Stale release changed current admission token.");
                completed.Add("stale-token-after-readmission-rejected");

                bool releaseAllSucceeded = ReleaseAdmissionAll(
                    admissionContextType,
                    admissionContext,
                    out int releasedCount,
                    out int failedCount,
                    out string releaseAllIssue);
                AssertTrue(releaseAllSucceeded,
                    $"P3K.5 release-all failed. {releaseAllIssue}");
                AssertEqual(1, releasedCount,
                    "P3K.5 release-all did not release current admission.");
                AssertEqual(0, failedCount,
                    "P3K.5 release-all reported failures.");
                completed.Add("all-admissions-released");

                PlayerGameplayAdmissionResult staleEvidenceAdmission = Admit(
                    admissionContextType,
                    admissionContext,
                    secondPublishedChain.Occupancy,
                    secondPublishedChain.Input,
                    secondPublishedChain.Camera,
                    output.Binding,
                    "stale-evidence-after-release");
                AssertStatus(
                    staleEvidenceAdmission,
                    PlayerGameplayAdmissionStatus
                        .RejectedForeignOrStaleOccupancy,
                    "Released stale prerequisite evidence was admitted.");
                completed.Add("live-authorities-reject-stale-evidence");

                PlayerGameplayAdmissionSnapshot final = AdmissionSnapshot(
                    admissionContextType,
                    admissionContext);
                AssertEqual(0, final.ReadyCount,
                    "P3K.5 final snapshot retained Ready admissions.");
                AssertEqual(0, final.BlockedByInputGateCount,
                    "P3K.5 final snapshot retained blocked admissions.");
                AssertEqual(0, final.ReleaseFailedCount,
                    "P3K.5 final snapshot retained release failures.");
                AssertEqual(2, final.NotAdmittedCount,
                    "P3K.5 final snapshot did not restore all Slots.");
                AssertEqual(0, final.PublishedCameraCount,
                    "P3K.5 final snapshot retained camera publication.");
                completed.Add("final-snapshot-clean");

                Debug.Log(
                    "[P3K5_GAMEPLAY_ADMISSION_CAMERA_PUBLICATION_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"slot='{slotOne.StableText}' " +
                    $"actor='{preparedOne.Materialization.ActorId.StableText}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[P3K5_GAMEPLAY_ADMISSION_CAMERA_PUBLICATION_SMOKE] " +
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

        private static Type ResolveType(Type publicContract, string typeName)
        {
            Type type = publicContract.Assembly.GetType(typeName, false);
            AssertNotNull(type,
                $"Required runtime type '{typeName}' is missing.");
            return type;
        }

        private static void ValidateContractSurface(Type contextType)
        {
            AssertTrue(!contextType.IsPublic,
                "P3K.5 runtime context must remain internal.");
            AssertNotNull(contextType.GetMethod("TryCreate", StaticAny),
                "P3K.5 runtime context has no TryCreate.");
            AssertNotNull(contextType.GetMethod("TryAdmit", InstanceAny),
                "P3K.5 runtime context has no TryAdmit.");
            AssertNotNull(contextType.GetMethod(
                    "TryRefreshReadiness", InstanceAny),
                "P3K.5 runtime context has no readiness refresh.");
            AssertNotNull(contextType.GetMethod("TryRelease", InstanceAny),
                "P3K.5 runtime context has no TryRelease.");
            AssertNotNull(contextType.GetMethod("TryReleaseAll", InstanceAny),
                "P3K.5 runtime context has no TryReleaseAll.");
            AssertNotNull(contextType.GetMethod("CreateSnapshot", InstanceAny),
                "P3K.5 runtime context has no CreateSnapshot.");
            AssertTrue(typeof(PlayerGameplayAdmissionToken).IsValueType,
                "P3K.5 admission token must be a value type.");
            AssertTrue(typeof(PlayerGameplayAdmissionSummary).IsValueType,
                "P3K.5 admission summary must be a value type.");
        }

        private static object CreateOccupancyContext(
            Type contextType,
            PlayerActorPreparationSnapshot snapshot)
        {
            object[] arguments = { snapshot, null, null };
            bool succeeded = (bool)contextType
                .GetMethod("TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.2 occupancy context creation failed. {arguments[2]}");
            return arguments[1];
        }

        private static object CreateInputContext(
            Type contextType,
            object occupancyContext)
        {
            object[] arguments = { occupancyContext, null, null };
            bool succeeded = (bool)contextType
                .GetMethod("TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.3 input context creation failed. {arguments[2]}");
            return arguments[1];
        }

        private static object CreateCameraContext(
            Type contextType,
            object occupancyContext,
            object inputContext)
        {
            object[] arguments =
            {
                occupancyContext,
                inputContext,
                null,
                null
            };
            bool succeeded = (bool)contextType
                .GetMethod("TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.4 camera context creation failed. {arguments[3]}");
            return arguments[2];
        }

        private static object CreateAdmissionContext(
            Type contextType,
            object occupancyContext,
            object inputContext,
            object cameraContext)
        {
            object[] arguments =
            {
                occupancyContext,
                inputContext,
                cameraContext,
                null,
                null
            };
            bool succeeded = (bool)contextType
                .GetMethod("TryCreate", StaticAny)
                .Invoke(null, arguments);
            AssertTrue(succeeded,
                $"P3K.5 admission context creation failed. {arguments[4]}");
            return arguments[3];
        }

        private static PlayerGameplayOccupancyResult ConfirmOccupancy(
            Type contextType,
            object context,
            PlayerActorPreparationSummary preparation,
            string reason)
        {
            return (PlayerGameplayOccupancyResult)contextType
                .GetMethod("TryConfirmOccupancy", InstanceAny)
                .Invoke(context, new object[]
                {
                    preparation,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    reason
                });
        }

        private static PlayerGameplayOccupancySnapshot OccupancySnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayOccupancySnapshot)contextType
                .GetMethod("CreateSnapshot", InstanceAny)
                .Invoke(context, Array.Empty<object>());
        }

        private static PlayerGameplayInputBindingResult Bind(
            Type contextType,
            object context,
            PlayerActorPreparationSummary preparation,
            PlayerGameplayOccupancySummary occupancy,
            LocalPlayerHostAuthoring host,
            PlayerActorDeclaration actorDeclaration,
            UnityPlayerInputGateAdapter gateAdapter,
            string reason)
        {
            return (PlayerGameplayInputBindingResult)contextType
                .GetMethod("TryBind", InstanceAny)
                .Invoke(context, new object[]
                {
                    preparation,
                    occupancy,
                    host,
                    actorDeclaration,
                    gateAdapter,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    reason
                });
        }

        private static PlayerGameplayInputBindingResult RefreshInput(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayInputBindingToken token,
            string reason)
        {
            return (PlayerGameplayInputBindingResult)contextType
                .GetMethod("TryRefreshAvailability", InstanceAny)
                .Invoke(context, new object[]
                {
                    playerSlotId,
                    token,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    reason
                });
        }

        private static PlayerGameplayInputBindingSnapshot InputSnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayInputBindingSnapshot)contextType
                .GetMethod("CreateSnapshot", InstanceAny)
                .Invoke(context, Array.Empty<object>());
        }

        private static PlayerGameplayCameraEligibilityResult ConfirmEligibility(
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
                .GetMethod("TryConfirmEligibility", InstanceAny)
                .Invoke(context, new object[]
                {
                    preparation,
                    occupancy,
                    inputBinding,
                    actorDeclaration,
                    authoring,
                    nameof(QaP3K5GameplayAdmissionSmoke),
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
                .GetMethod("TrySkipOptional", InstanceAny)
                .Invoke(context, new object[]
                {
                    preparation,
                    occupancy,
                    inputBinding,
                    requiredness,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    reason
                });
        }

        private static PlayerGameplayCameraEligibilitySnapshot CameraSnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayCameraEligibilitySnapshot)contextType
                .GetMethod("CreateSnapshot", InstanceAny)
                .Invoke(context, Array.Empty<object>());
        }

        private static PlayerGameplayAdmissionResult Admit(
            Type contextType,
            object context,
            PlayerGameplayOccupancySummary occupancy,
            PlayerGameplayInputBindingSummary inputBinding,
            PlayerGameplayCameraEligibilitySummary cameraEligibility,
            CameraOutputSessionBinding outputSession,
            string reason)
        {
            return (PlayerGameplayAdmissionResult)contextType
                .GetMethod("TryAdmit", InstanceAny)
                .Invoke(context, new object[]
                {
                    occupancy,
                    inputBinding,
                    cameraEligibility,
                    outputSession,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    reason
                });
        }

        private static PlayerGameplayAdmissionResult RefreshAdmission(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken token,
            string reason)
        {
            return (PlayerGameplayAdmissionResult)contextType
                .GetMethod("TryRefreshReadiness", InstanceAny)
                .Invoke(context, new object[]
                {
                    playerSlotId,
                    token,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    reason
                });
        }

        private static PlayerGameplayAdmissionResult ReleaseAdmission(
            Type contextType,
            object context,
            PlayerSlotId playerSlotId,
            PlayerGameplayAdmissionToken token,
            string reason)
        {
            return (PlayerGameplayAdmissionResult)contextType
                .GetMethod("TryRelease", InstanceAny)
                .Invoke(context, new object[]
                {
                    playerSlotId,
                    token,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    reason
                });
        }

        private static bool ReleaseAdmissionAll(
            Type contextType,
            object context,
            out int releasedCount,
            out int failedCount,
            out string issue)
        {
            object[] arguments =
            {
                nameof(QaP3K5GameplayAdmissionSmoke),
                "release-all-admissions",
                0,
                0,
                null
            };
            bool succeeded = (bool)contextType
                .GetMethod("TryReleaseAll", InstanceAny)
                .Invoke(context, arguments);
            releasedCount = (int)arguments[2];
            failedCount = (int)arguments[3];
            issue = arguments[4] as string ?? string.Empty;
            return succeeded;
        }

        private static PlayerGameplayAdmissionSnapshot AdmissionSnapshot(
            Type contextType,
            object context)
        {
            return (PlayerGameplayAdmissionSnapshot)contextType
                .GetMethod("CreateSnapshot", InstanceAny)
                .Invoke(context, Array.Empty<object>());
        }

        private static ChainEvidence RebuildChain(
            Type occupancyContextType,
            object occupancyContext,
            Type inputContextType,
            object inputContext,
            Type cameraContextType,
            object cameraContext,
            PlayerActorPreparationSummary preparation,
            HostFixture host,
            CameraFixture camera,
            string reason)
        {
            PlayerGameplayOccupancyResult occupancy = ConfirmOccupancy(
                occupancyContextType,
                occupancyContext,
                preparation,
                $"{reason}-occupancy");
            AssertEqual(PlayerGameplayOccupancyStatus.SucceededOccupied,
                occupancy.Status,
                "P3K.5 chain rebuild could not occupy Slot.");

            PlayerGameplayInputBindingResult input = Bind(
                inputContextType,
                inputContext,
                preparation,
                occupancy.CurrentSummary,
                host.Host,
                host.ActorDeclaration,
                host.GateAdapter,
                $"{reason}-input");
            AssertEqual(PlayerGameplayInputBindingStatus.SucceededBound,
                input.Status,
                "P3K.5 chain rebuild could not bind input.");

            PlayerGameplayCameraEligibilityResult cameraResult =
                ConfirmEligibility(
                    cameraContextType,
                    cameraContext,
                    preparation,
                    occupancy.CurrentSummary,
                    input.CurrentSummary,
                    host.ActorDeclaration,
                    camera.Authoring,
                    $"{reason}-camera");
            AssertEqual(
                PlayerGameplayCameraEligibilityStatus.SucceededEligible,
                cameraResult.Status,
                "P3K.5 chain rebuild could not confirm camera eligibility.");

            return new ChainEvidence(
                occupancy.CurrentSummary,
                input.CurrentSummary,
                cameraResult.CurrentSummary);
        }

        private static void AssertDependenciesReleased(
            Type occupancyContextType,
            object occupancyContext,
            Type inputContextType,
            object inputContext,
            Type cameraContextType,
            object cameraContext,
            PlayerSlotId playerSlotId,
            PlayerInput playerInput)
        {
            PlayerGameplayOccupancySnapshot occupancy = OccupancySnapshot(
                occupancyContextType,
                occupancyContext);
            AssertTrue(occupancy.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayOccupancySummary occupancySummary) &&
                occupancySummary.IsVacant,
                "P3K.5 reverse release retained effective occupancy.");

            PlayerGameplayInputBindingSnapshot input = InputSnapshot(
                inputContextType,
                inputContext);
            AssertTrue(input.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayInputBindingSummary inputSummary) &&
                inputSummary.IsUnbound,
                "P3K.5 reverse release retained gameplay input binding.");

            PlayerGameplayCameraEligibilitySnapshot camera = CameraSnapshot(
                cameraContextType,
                cameraContext);
            AssertTrue(camera.TryGetSummary(
                    playerSlotId,
                    out PlayerGameplayCameraEligibilitySummary cameraSummary) &&
                cameraSummary.IsNotEvaluated,
                "P3K.5 reverse release retained camera eligibility.");

            AssertEqual("UI", CurrentMapName(playerInput),
                "P3K.5 reverse release did not restore the previous input map.");
        }

        private static PlayerActorPreparationSummary CreatePreparation(
            string sessionId,
            PlayerSlotId playerSlotId,
            ActorProfileId actorProfileId,
            ActorId actorId,
            RuntimeContentOwner owner,
            int revision)
        {
            PlayerActorMaterializationOperationId operationId = CreateOperationId(
                sessionId,
                owner,
                playerSlotId,
                revision);
            RuntimeContentIdentity identity = RuntimeContentIdentity.From(
                owner,
                $"qa.p3k5.content.{playerSlotId.Value.Value}.{revision}");
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
            AssertNotNull(constructor,
                "P3J.4 PlayerActorPreparationSummary constructor changed.");
            return (PlayerActorPreparationSummary)constructor.Invoke(
                new object[]
                {
                    sessionId,
                    playerSlotId,
                    PlayerActorPreparationState.Prepared,
                    actorProfileId,
                    revision,
                    materialization,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    "synthetic-preparation",
                    "Synthetic current Player Actor preparation."
                });
        }

        private static PlayerActorMaterializationOperationId CreateOperationId(
            string sessionId,
            RuntimeContentOwner owner,
            PlayerSlotId playerSlotId,
            int revision)
        {
            MethodInfo method = typeof(PlayerActorMaterializationOperationId)
                .GetMethod("TryCreate", StaticAny);
            object[] arguments =
            {
                sessionId,
                owner,
                playerSlotId,
                revision,
                default(PlayerActorMaterializationOperationId),
                null
            };
            bool succeeded = (bool)method.Invoke(null, arguments);
            AssertTrue(succeeded,
                $"Synthetic materialization operation identity failed. {arguments[5]}");
            return (PlayerActorMaterializationOperationId)arguments[4];
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
            AssertNotNull(constructor,
                "P3J.3 PlayerActorMaterializationSnapshot constructor changed.");
            return (PlayerActorMaterializationSnapshot)constructor.Invoke(
                new object[]
                {
                    operationId,
                    identity,
                    playerSlotId,
                    actorProfileId,
                    actorId,
                    revision,
                    PlayerActorMaterializationState.Active,
                    nameof(QaP3K5GameplayAdmissionSmoke),
                    "synthetic-materialization"
                });
        }

        private static PlayerActorPreparationSnapshot CreatePreparationSnapshot(
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
            AssertNotNull(constructor,
                "P3J.4 PlayerActorPreparationSnapshot constructor changed.");
            return (PlayerActorPreparationSnapshot)constructor.Invoke(
                new object[]
                {
                    sessionId,
                    1,
                    preparations,
                    Array.Empty<PlayerActorMaterializationSnapshot>(),
                    PlayerActorPreparationStatus.SucceededPrepared,
                    "Synthetic preparation snapshot."
                });
        }

        private static InputActionAsset CreateInputAsset(
            string assetName,
            List<UnityEngine.Object> created)
        {
            InputActionAsset asset =
                ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = assetName;
            created.Add(asset);
            InputActionMap ui = asset.AddActionMap("UI");
            ui.AddAction("Submit", InputActionType.Button);
            InputActionMap player = asset.AddActionMap("Player");
            player.AddAction("Move", InputActionType.Value);
            return asset;
        }

        private static void ConfigurePlayerInput(
            PlayerInput playerInput,
            InputActionAsset actions,
            string initialMap)
        {
            playerInput.actions = actions ??
                throw new ArgumentNullException(nameof(actions));
            playerInput.defaultActionMap = initialMap;
        }

        private static void SetCurrentActionMap(
            PlayerInput playerInput,
            string actionMapName,
            string context)
        {
            InputActionMap actionMap = playerInput.actions.FindActionMap(
                actionMapName,
                throwIfNotFound: false);
            AssertNotNull(actionMap,
                $"{context} has no action map '{actionMapName}'.");
            playerInput.currentActionMap = actionMap;
            AssertTrue(ReferenceEquals(playerInput.currentActionMap, actionMap) &&
                actionMap.enabled,
                $"{context} did not enable action map '{actionMapName}'.");
        }

        private static void ConfigureSlotDeclaration(
            PlayerSlotDeclaration declaration,
            PlayerSlotId slotId,
            PlayerInput playerInput,
            string label)
        {
            MethodInfo configure = typeof(PlayerSlotDeclaration).GetMethod(
                "ConfigureForDiagnostics",
                InstanceAny,
                null,
                new[]
                {
                    typeof(string),
                    typeof(string),
                    typeof(PlayerInput),
                    typeof(string)
                },
                null);
            AssertNotNull(configure,
                "PlayerSlotDeclaration ConfigureForDiagnostics is missing.");
            configure.Invoke(declaration, new object[]
            {
                slotId.Value.Value,
                label,
                playerInput,
                "qa.p3k5.joined-slot"
            });
        }

        private static void ConfigureGateAdapter(
            UnityPlayerInputGateAdapter adapter,
            PlayerInput playerInput,
            PlayerSlotDeclaration slotDeclaration,
            string actionMapName)
        {
            SerializedObject serialized = new SerializedObject(adapter);
            serialized.FindProperty("playerInput").objectReferenceValue = playerInput;
            serialized.FindProperty("sourceSlot").objectReferenceValue = slotDeclaration;
            serialized.FindProperty("gameplayActionMapName").stringValue = actionMapName;
            serialized.FindProperty("applyOnEnable").boolValue = false;
            serialized.FindProperty("logStateChanges").boolValue = false;
            serialized.FindProperty("logMissingRuntimeOnce").boolValue = false;
            serialized.FindProperty("logMissingTargetOnce").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static PlayerActorDeclaration CreateActorDeclaration(
            Transform parent,
            ActorId actorId,
            PlayerInput playerInput,
            string label,
            List<UnityEngine.Object> created)
        {
            GameObject actorObject = new GameObject(label);
            actorObject.transform.SetParent(parent, false);
            created.Add(actorObject);
            PlayerActorDeclaration declaration =
                actorObject.AddComponent<PlayerActorDeclaration>();
            MethodInfo configure = typeof(PlayerActorDeclaration).GetMethod(
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
            AssertNotNull(configure,
                "PlayerActorDeclaration ConfigureForDiagnostics is missing.");
            configure.Invoke(declaration, new object[]
            {
                actorId.Value.Value,
                label,
                playerInput,
                "qa.p3k5.actor-declaration"
            });
            return declaration;
        }

        private static Transform CreateChild(
            Transform parent,
            string name,
            List<UnityEngine.Object> created)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            created.Add(child);
            return child.transform;
        }

        private static string CurrentMapName(PlayerInput playerInput)
        {
            return playerInput != null && playerInput.currentActionMap != null
                ? playerInput.currentActionMap.name
                : string.Empty;
        }

        private static void SetField(
            object target,
            string fieldName,
            object value)
        {
            Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, InstanceAny);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            throw new MissingFieldException(
                target.GetType().FullName,
                fieldName);
        }

        private static void AssertStatus(
            PlayerGameplayAdmissionResult result,
            PlayerGameplayAdmissionStatus expected,
            string message)
        {
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} Expected='{expected}' Actual='{result.Status}'. " +
                    result.ToDiagnosticString());
            }
        }

        private static void AssertStatus(
            PlayerGameplayInputBindingResult result,
            PlayerGameplayInputBindingStatus expected,
            string message)
        {
            if (result.Status != expected)
            {
                throw new InvalidOperationException(
                    $"{message} Expected='{expected}' Actual='{result.Status}'. " +
                    result.ToDiagnosticString());
            }
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

        private static void AssertEqual<T>(
            T expected,
            T actual,
            string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException(
                    $"{message} Expected='{expected}' Actual='{actual}'.");
            }
        }

        private static string Escape(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }

        private readonly struct ChainEvidence
        {
            internal ChainEvidence(
                PlayerGameplayOccupancySummary occupancy,
                PlayerGameplayInputBindingSummary input,
                PlayerGameplayCameraEligibilitySummary camera)
            {
                Occupancy = occupancy;
                Input = input;
                Camera = camera;
            }

            internal PlayerGameplayOccupancySummary Occupancy { get; }
            internal PlayerGameplayInputBindingSummary Input { get; }
            internal PlayerGameplayCameraEligibilitySummary Camera { get; }
        }

        private sealed class HostFixture : IDisposable
        {
            internal GameObject Root { get; private set; }
            internal LocalPlayerHostAuthoring Host { get; private set; }
            internal PlayerInput PlayerInput { get; private set; }
            internal PlayerSlotDeclaration SlotDeclaration { get; private set; }
            internal UnityPlayerInputGateAdapter GateAdapter { get; private set; }
            internal Transform ActorMount { get; private set; }
            internal PlayerActorDeclaration ActorDeclaration { get; private set; }

            internal static HostFixture Create(
                PlayerSlotId slotId,
                ActorId actorId,
                string initialActionMap,
                string gameplayActionMap,
                List<UnityEngine.Object> created)
            {
                var fixture = new HostFixture();
                fixture.Root = new GameObject(
                    "P3K.5 Stable Local Player Host");
                fixture.Root.SetActive(false);
                created.Add(fixture.Root);

                fixture.PlayerInput =
                    fixture.Root.AddComponent<PlayerInput>();
                fixture.PlayerInput.enabled = false;
                ConfigurePlayerInput(
                    fixture.PlayerInput,
                    CreateInputAsset("P3K.5 Actions", created),
                    initialActionMap);

                fixture.Host =
                    fixture.Root.AddComponent<LocalPlayerHostAuthoring>();
                GameObject mountObject = new GameObject("Actor Mount");
                mountObject.transform.SetParent(fixture.Root.transform, false);
                created.Add(mountObject);
                fixture.ActorMount = mountObject.transform;

                fixture.SlotDeclaration =
                    fixture.Root.AddComponent<PlayerSlotDeclaration>();
                ConfigureSlotDeclaration(
                    fixture.SlotDeclaration,
                    slotId,
                    fixture.PlayerInput,
                    "P3K.5 Slot");

                SetField(fixture.Host, "playerInput", fixture.PlayerInput);
                SetField(fixture.Host, "actorMount", fixture.ActorMount);
                SetField(
                    fixture.Host,
                    "stagedSlotDeclaration",
                    fixture.SlotDeclaration);
                SetField(fixture.Host, "joinedPlayerSlotId", slotId);
                SetField(fixture.Host, "joinedConfiguredIndex", 0);
                FieldInfo admission = typeof(LocalPlayerHostAuthoring).GetField(
                    "admissionState",
                    InstanceAny);
                admission.SetValue(
                    fixture.Host,
                    Enum.ToObject(admission.FieldType, 20));

                fixture.GateAdapter =
                    fixture.Root.AddComponent<UnityPlayerInputGateAdapter>();
                ConfigureGateAdapter(
                    fixture.GateAdapter,
                    fixture.PlayerInput,
                    fixture.SlotDeclaration,
                    gameplayActionMap);
                fixture.GateAdapter.enabled = false;

                fixture.ActorDeclaration = CreateActorDeclaration(
                    fixture.ActorMount,
                    actorId,
                    fixture.PlayerInput,
                    "P3K.5 Prepared Logical Actor",
                    created);

                SetCurrentActionMap(
                    fixture.PlayerInput,
                    initialActionMap,
                    "P3K.5 Stable Local Player Host");
                fixture.Root.SetActive(true);
                AssertTrue(fixture.ActorDeclaration.gameObject.activeInHierarchy,
                    "P3K.5 Logical Actor fixture is not active in hierarchy.");
                return fixture;
            }

            public void Dispose()
            {
            }
        }

        private sealed class CameraFixture : IDisposable
        {
            internal CameraRigComposer Rig { get; private set; }
            internal CinemachineCamera CinemachineCamera { get; private set; }
            internal PlayerGameplayCameraAuthoring Authoring { get; private set; }
            internal Transform FollowTarget { get; private set; }
            internal Transform LookAtTarget { get; private set; }

            internal static CameraFixture Create(
                Transform actorRoot,
                PlayerGameplayCameraRequiredness requiredness,
                List<UnityEngine.Object> created)
            {
                var fixture = new CameraFixture();
                fixture.FollowTarget = CreateChild(
                    actorRoot,
                    "P3K.5 Follow Target",
                    created);
                fixture.LookAtTarget = CreateChild(
                    actorRoot,
                    "P3K.5 LookAt Target",
                    created);

                Transform rigTransform = CreateChild(
                    actorRoot,
                    "P3K.5 Camera Rig",
                    created);
                fixture.Rig =
                    rigTransform.gameObject.AddComponent<CameraRigComposer>();
                fixture.CinemachineCamera =
                    rigTransform.gameObject.AddComponent<CinemachineCamera>();
                fixture.CinemachineCamera.enabled = false;

                SetField(fixture.Rig,
                    "presentationIntent",
                    CameraRigPresentationIntent.Follow);
                SetField(fixture.Rig,
                    "targetSourceKind",
                    CameraTargetSourceKind.ExplicitTransform);
                SetField(fixture.Rig, "playerComposer", null);
                SetField(fixture.Rig,
                    "explicitFollowTarget",
                    fixture.FollowTarget);
                SetField(fixture.Rig,
                    "explicitLookAtTarget",
                    fixture.LookAtTarget);
                SetField(fixture.Rig,
                    "followRequirement",
                    CameraTargetRequirement.Required);
                SetField(fixture.Rig,
                    "lookAtRequirement",
                    CameraTargetRequirement.Optional);
                SetField(fixture.Rig,
                    "cinemachineCamera",
                    fixture.CinemachineCamera);

                Transform authoringTransform = CreateChild(
                    actorRoot,
                    "P3K.5 Camera Authoring",
                    created);
                fixture.Authoring = authoringTransform.gameObject
                    .AddComponent<PlayerGameplayCameraAuthoring>();
                SetField(fixture.Authoring, "requiredness", requiredness);
                SetField(fixture.Authoring, "cameraRig", fixture.Rig);
                SetField(fixture.Authoring,
                    "followTarget",
                    fixture.FollowTarget);
                SetField(fixture.Authoring,
                    "lookAtTarget",
                    fixture.LookAtTarget);
                SetField(fixture.Authoring, "precedence", 50);
                return fixture;
            }

            public void Dispose()
            {
            }
        }

        private sealed class CameraOutputFixture : IDisposable
        {
            internal GameObject Root { get; private set; }
            internal CameraOutputSessionBinding Binding { get; private set; }

            internal static CameraOutputFixture Create(
                string outputId,
                List<UnityEngine.Object> created)
            {
                var fixture = new CameraOutputFixture();
                fixture.Root = new GameObject("P3K.5 Camera Output");
                fixture.Root.SetActive(false);
                created.Add(fixture.Root);

                UnityEngine.Camera unityCamera =
                    fixture.Root.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain =
                    fixture.Root.AddComponent<CinemachineBrain>();
                fixture.Binding = fixture.Root
                    .AddComponent<CameraOutputSessionBinding>();

                SetField(fixture.Binding, "outputId", outputId);
                SetField(fixture.Binding, "unityCamera", unityCamera);
                SetField(fixture.Binding, "cinemachineBrain", brain);
                SetField(fixture.Binding, "initializeOnAwake", false);
                SetField(fixture.Binding, "logDiagnostics", false);

                fixture.Root.SetActive(true);
                AssertTrue(fixture.Binding.TryInitialize(out string issue),
                    $"P3K.5 camera output initialization failed. {issue}");
                return fixture;
            }

            public void Dispose()
            {
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Actors;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.Player.Editor
{
    internal static class QaP3K7APreActivationAdmissionBoundarySmoke
    {
        private const string MenuPath =
            "Immersive Framework/QA/Player/P3K.7A Run Pre-Activation Admission Boundary Smoke";

        private const string GateTypeName =
            "Immersive.Framework.PlayerParticipation.ActivityPlayerAdmissionFlowGate";

        private static readonly BindingFlags InstanceAny =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem(MenuPath)]
        public static void Run()
        {
            var completed = new List<string>();
            var created = new List<UnityEngine.Object>();

            try
            {
                AssertTrue(Application.isPlaying,
                    "P3K.7A smoke must run in Play Mode.");

                Type gateType = typeof(ActivityPlayerAdmissionFlowDecision)
                    .Assembly.GetType(GateTypeName, throwOnError: false);
                AssertNotNull(gateType,
                    "P3K.7A ActivityPlayerAdmissionFlowGate type is missing.");

                object gate = Activator.CreateInstance(gateType, nonPublic: true);
                AssertNotNull(gate,
                    "P3K.7A gate could not be created.");

                MethodInfo evaluate = gateType.GetMethod(
                    "Evaluate",
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(ActivityAsset),
                        typeof(PlayerParticipationSnapshot),
                        typeof(PlayerActorPreparationSnapshot),
                        typeof(PlayerGameplayAdmissionSnapshot),
                        typeof(string),
                        typeof(string)
                    },
                    null);
                MethodInfo createDecision = gateType.GetMethod(
                    "CreateDecision",
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(ActivityPlayerAdmissionEvaluationResult),
                        typeof(string),
                        typeof(string)
                    },
                    null);
                AssertNotNull(evaluate,
                    "P3K.7A Evaluate signature changed.");
                AssertNotNull(createDecision,
                    "P3K.7A CreateDecision signature changed.");
                ValidateContractSurface();
                completed.Add("contract-surface-valid");

                AssertTrue(!typeof(MonoBehaviour).IsAssignableFrom(gateType),
                    "P3K.7A gate must remain a plain scoped runtime object.");
                completed.Add("gate-not-monobehaviour");

                ActivityPlayerAdmissionFlowDecision decision = Evaluate(
                    evaluate,
                    gate,
                    null,
                    null,
                    null,
                    null,
                    "qa",
                    "missing-activity");
                AssertDisposition(
                    decision,
                    ActivityPlayerAdmissionFlowDisposition.RejectFailed,
                    ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.MissingActivity);
                completed.Add("missing-activity-reject-failed");

                PlayerParticipationRequirementsProfile none = CreateRequirements(
                    PlayerParticipationRequirementLevel.None,
                    "None",
                    created);
                PlayerParticipationRequirementsProfile gameplay = CreateRequirements(
                    PlayerParticipationRequirementLevel.GameplayReady,
                    "Gameplay Ready",
                    created);
                ActivityParticipationProjectionProfile noSlots = CreateNoSlotsProjection(created);
                ActivityAsset noPlayersActivity = CreateActivity(
                    noSlots,
                    none,
                    "No Players",
                    created);
                ActivityAsset contradictoryActivity = CreateActivity(
                    noSlots,
                    gameplay,
                    "Contradictory",
                    created);

                decision = Evaluate(
                    evaluate,
                    gate,
                    noPlayersActivity,
                    null,
                    null,
                    null,
                    "qa",
                    "no-slots-none");
                AssertDisposition(
                    decision,
                    ActivityPlayerAdmissionFlowDisposition.Proceed,
                    ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                AssertTrue(decision.CanProceed && !decision.CanRetry,
                    "Satisfied decision did not expose Proceed semantics.");
                completed.Add("no-slots-none-proceed");

                decision = Evaluate(
                    evaluate,
                    gate,
                    contradictoryActivity,
                    null,
                    null,
                    null,
                    "qa",
                    "no-slots-gameplay");
                AssertDisposition(
                    decision,
                    ActivityPlayerAdmissionFlowDisposition.RejectFailed,
                    ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.ContradictoryNoSlotsRequirement);
                completed.Add("no-slots-gameplay-reject-failed");

                ActivityPlayerAdmissionEvaluationResult satisfied = CreateEvaluation(
                    ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied,
                    Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                    "Satisfied evaluation.");
                ActivityPlayerAdmissionEvaluationResult pending = CreateEvaluation(
                    ActivityPlayerAdmissionEvaluationStatus.PendingResolution,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionPending,
                    new[] { CreateSlot(ActivityPlayerAdmissionSlotStatus.PendingResolution) },
                    "Pending evaluation.");
                ActivityPlayerAdmissionEvaluationResult blocked = CreateEvaluation(
                    ActivityPlayerAdmissionEvaluationStatus.Blocked,
                    ActivityPlayerAdmissionEvaluationCode.ZeroParticipantsRejected,
                    Array.Empty<ActivityPlayerAdmissionSlotResult>(),
                    "Blocked evaluation.");
                ActivityPlayerAdmissionEvaluationResult failed = CreateEvaluation(
                    ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionReleaseFailed,
                    new[] { CreateSlot(ActivityPlayerAdmissionSlotStatus.Failed) },
                    "Failed evaluation.");

                ActivityPlayerAdmissionFlowDecision mappedSatisfied = CreateDecision(
                    createDecision, gate, satisfied, "qa", "map-satisfied");
                AssertDisposition(
                    mappedSatisfied,
                    ActivityPlayerAdmissionFlowDisposition.Proceed,
                    ActivityPlayerAdmissionEvaluationStatus.Satisfied,
                    ActivityPlayerAdmissionEvaluationCode.Satisfied);
                completed.Add("satisfied-maps-proceed");

                ActivityPlayerAdmissionFlowDecision mappedPending = CreateDecision(
                    createDecision, gate, pending, "qa", "map-pending");
                AssertDisposition(
                    mappedPending,
                    ActivityPlayerAdmissionFlowDisposition.AwaitResolution,
                    ActivityPlayerAdmissionEvaluationStatus.PendingResolution,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionPending);
                completed.Add("pending-maps-await");

                ActivityPlayerAdmissionFlowDecision mappedBlocked = CreateDecision(
                    createDecision, gate, blocked, "qa", "map-blocked");
                AssertDisposition(
                    mappedBlocked,
                    ActivityPlayerAdmissionFlowDisposition.RejectBlocked,
                    ActivityPlayerAdmissionEvaluationStatus.Blocked,
                    ActivityPlayerAdmissionEvaluationCode.ZeroParticipantsRejected);
                completed.Add("blocked-maps-reject-blocked");

                ActivityPlayerAdmissionFlowDecision mappedFailed = CreateDecision(
                    createDecision, gate, failed, "qa", "map-failed");
                AssertDisposition(
                    mappedFailed,
                    ActivityPlayerAdmissionFlowDisposition.RejectFailed,
                    ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionReleaseFailed);
                completed.Add("failed-maps-reject-failed");

                AssertTrue(mappedPending.RequiresResolution && mappedPending.CanRetry,
                    "Pending decision must require explicit resolution and retry.");
                completed.Add("pending-is-retryable");

                AssertTrue(mappedBlocked.IsRejected && !mappedBlocked.CanRetry &&
                    mappedFailed.IsRejected && !mappedFailed.CanRetry,
                    "Blocked/failed decisions must reject without claiming immediate retry.");
                completed.Add("rejections-not-retryable");

                AssertTrue(
                    mappedSatisfied.AttemptSequence < mappedPending.AttemptSequence &&
                    mappedPending.AttemptSequence < mappedBlocked.AttemptSequence &&
                    mappedBlocked.AttemptSequence < mappedFailed.AttemptSequence,
                    "P3K.7A attempt sequence is not monotonic.");
                completed.Add("attempt-sequence-monotonic");

                ActivityPlayerAdmissionFlowDecision normalized = CreateDecision(
                    createDecision,
                    gate,
                    satisfied,
                    "   ",
                    "   ");
                AssertTrue(!string.IsNullOrWhiteSpace(normalized.Source) &&
                    !string.IsNullOrWhiteSpace(normalized.Reason),
                    "P3K.7A source/reason normalization failed.");
                completed.Add("source-reason-normalized");

                AssertEqual(1, mappedPending.ProjectedSlotCount,
                    "Pending Slot evidence was not preserved.");
                AssertEqual(1, mappedPending.PendingSlotCount,
                    "Pending Slot aggregate was not preserved.");
                AssertEqual(1, mappedFailed.FailedSlotCount,
                    "Failed Slot aggregate was not preserved.");
                completed.Add("aggregate-counts-preserved");

                AssertTrue(ReferenceEquals(mappedPending.Evaluation, pending),
                    "P3K.7A decision did not retain the exact P3K.6 evaluation.");
                completed.Add("exact-evaluation-retained");

                string diagnostic = mappedPending.ToDiagnosticString();
                AssertTrue(diagnostic.Contains("AwaitResolution") &&
                    diagnostic.Contains("GameplayAdmissionPending") &&
                    diagnostic.Contains("attempt="),
                    "P3K.7A diagnostic does not expose disposition, code and attempt.");
                completed.Add("decision-diagnostic-complete");

                ActivityPlayerAdmissionFlowDecision nullEvaluation = CreateDecision(
                    createDecision,
                    gate,
                    null,
                    "qa",
                    "null-evaluation");
                AssertDisposition(
                    nullEvaluation,
                    ActivityPlayerAdmissionFlowDisposition.RejectFailed,
                    ActivityPlayerAdmissionEvaluationStatus.Failed,
                    ActivityPlayerAdmissionEvaluationCode.MissingActivity);
                completed.Add("null-evaluation-explicit-failure");

                ValidateNoUnityReferences(typeof(ActivityPlayerAdmissionFlowDecision));
                completed.Add("public-decision-no-unity-references");

                AssertEqual(18, completed.Count,
                    "P3K.7A smoke case count changed.");

                Debug.Log(
                    $"[P3K7A_PRE_ACTIVATION_ADMISSION_BOUNDARY_SMOKE] " +
                    $"status='Passed' cases='{completed.Count}' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Exception root = exception is TargetInvocationException invocation &&
                    invocation.InnerException != null
                        ? invocation.InnerException
                        : exception;
                Debug.LogError(
                    $"[P3K7A_PRE_ACTIVATION_ADMISSION_BOUNDARY_SMOKE] " +
                    $"status='Failed' exception='{root.GetType().Name}' " +
                    $"message='{Escape(root.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                Debug.LogException(root);
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

        private static ActivityPlayerAdmissionFlowDecision Evaluate(
            MethodInfo method,
            object gate,
            ActivityAsset activity,
            PlayerParticipationSnapshot participation,
            PlayerActorPreparationSnapshot preparation,
            PlayerGameplayAdmissionSnapshot gameplay,
            string source,
            string reason)
        {
            return (ActivityPlayerAdmissionFlowDecision)method.Invoke(
                gate,
                new object[]
                {
                    activity,
                    participation,
                    preparation,
                    gameplay,
                    source,
                    reason
                });
        }

        private static ActivityPlayerAdmissionFlowDecision CreateDecision(
            MethodInfo method,
            object gate,
            ActivityPlayerAdmissionEvaluationResult evaluation,
            string source,
            string reason)
        {
            return (ActivityPlayerAdmissionFlowDecision)method.Invoke(
                gate,
                new object[] { evaluation, source, reason });
        }

        private static ActivityPlayerAdmissionEvaluationResult CreateEvaluation(
            ActivityPlayerAdmissionEvaluationStatus status,
            ActivityPlayerAdmissionEvaluationCode code,
            ActivityPlayerAdmissionSlotResult[] slots,
            string message)
        {
            ConstructorInfo constructor = typeof(ActivityPlayerAdmissionEvaluationResult)
                .GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(string),
                        typeof(ActivityParticipationProjectionMode),
                        typeof(ActivityParticipationZeroParticipantPolicy),
                        typeof(PlayerParticipationRequirementLevel),
                        typeof(ActivityPlayerAdmissionEvaluationStatus),
                        typeof(ActivityPlayerAdmissionEvaluationCode),
                        typeof(ActivityPlayerAdmissionSlotResult[]),
                        typeof(string)
                    },
                    null);
            AssertNotNull(constructor,
                "P3K.6 evaluation result constructor changed.");
            return (ActivityPlayerAdmissionEvaluationResult)constructor.Invoke(
                new object[]
                {
                    "QA P3K.7A Activity",
                    "qa.p3k7a.session",
                    ActivityParticipationProjectionMode.ExplicitSlots,
                    ActivityParticipationZeroParticipantPolicy.Rejected,
                    PlayerParticipationRequirementLevel.GameplayReady,
                    status,
                    code,
                    slots,
                    message
                });
        }

        private static ActivityPlayerAdmissionSlotResult CreateSlot(
            ActivityPlayerAdmissionSlotStatus status)
        {
            ConstructorInfo constructor = typeof(ActivityPlayerAdmissionSlotResult)
                .GetConstructor(
                    InstanceAny,
                    null,
                    new[]
                    {
                        typeof(int), typeof(int), typeof(PlayerSlotId),
                        typeof(PlayerParticipationRequirementLevel),
                        typeof(ActivityPlayerAdmissionSlotStatus),
                        typeof(ActivityPlayerAdmissionMissingRequirement),
                        typeof(ActivityPlayerAdmissionEvaluationCode),
                        typeof(ActorProfileId), typeof(ActorId),
                        typeof(bool), typeof(bool), typeof(bool), typeof(bool),
                        typeof(string)
                    },
                    null);
            AssertNotNull(constructor,
                "P3K.6 Slot result constructor changed.");

            ActivityPlayerAdmissionEvaluationCode code = status switch
            {
                ActivityPlayerAdmissionSlotStatus.PendingResolution =>
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionPending,
                ActivityPlayerAdmissionSlotStatus.Failed =>
                    ActivityPlayerAdmissionEvaluationCode.GameplayAdmissionReleaseFailed,
                ActivityPlayerAdmissionSlotStatus.Blocked =>
                    ActivityPlayerAdmissionEvaluationCode.SlotUnavailable,
                _ => ActivityPlayerAdmissionEvaluationCode.Satisfied
            };

            return (ActivityPlayerAdmissionSlotResult)constructor.Invoke(
                new object[]
                {
                    0,
                    0,
                    PlayerSlotId.From("player.1"),
                    PlayerParticipationRequirementLevel.GameplayReady,
                    status,
                    status == ActivityPlayerAdmissionSlotStatus.Satisfied
                        ? ActivityPlayerAdmissionMissingRequirement.None
                        : ActivityPlayerAdmissionMissingRequirement.GameplayReady,
                    code,
                    default(ActorProfileId),
                    default(ActorId),
                    true,
                    true,
                    true,
                    status == ActivityPlayerAdmissionSlotStatus.Satisfied,
                    "Synthetic P3K.7A Slot decision evidence."
                });
        }

        private static ActivityAsset CreateActivity(
            ActivityParticipationProjectionProfile projection,
            PlayerParticipationRequirementsProfile requirements,
            string name,
            List<UnityEngine.Object> created)
        {
            ActivityAsset activity = ScriptableObject.CreateInstance<ActivityAsset>();
            activity.name = name;
            created.Add(activity);
            SerializedObject serialized = new SerializedObject(activity);
            serialized.FindProperty("activityName").stringValue = name;
            serialized.FindProperty("playerParticipationProjectionProfile")
                .objectReferenceValue = projection;
            serialized.FindProperty("playerParticipationRequirementsProfile")
                .objectReferenceValue = requirements;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return activity;
        }

        private static ActivityParticipationProjectionProfile CreateNoSlotsProjection(
            List<UnityEngine.Object> created)
        {
            ActivityParticipationProjectionProfile profile =
                ScriptableObject.CreateInstance<ActivityParticipationProjectionProfile>();
            profile.name = "No Slots";
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("projectionMode").intValue =
                (int)ActivityParticipationProjectionMode.NoSlots;
            serialized.FindProperty("zeroParticipantPolicy").intValue =
                (int)ActivityParticipationZeroParticipantPolicy.Allowed;
            serialized.FindProperty("explicitSlotProfiles").arraySize = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static PlayerParticipationRequirementsProfile CreateRequirements(
            PlayerParticipationRequirementLevel level,
            string name,
            List<UnityEngine.Object> created)
        {
            PlayerParticipationRequirementsProfile profile =
                ScriptableObject.CreateInstance<PlayerParticipationRequirementsProfile>();
            profile.name = name;
            created.Add(profile);
            SerializedObject serialized = new SerializedObject(profile);
            serialized.FindProperty("requirementLevel").intValue = (int)level;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        private static void ValidateContractSurface()
        {
            AssertTrue(Enum.IsDefined(
                    typeof(ActivityPlayerAdmissionFlowDisposition),
                    ActivityPlayerAdmissionFlowDisposition.Proceed),
                "P3K.7A Proceed disposition is missing.");
            AssertTrue(Enum.IsDefined(
                    typeof(ActivityPlayerAdmissionFlowDisposition),
                    ActivityPlayerAdmissionFlowDisposition.AwaitResolution),
                "P3K.7A AwaitResolution disposition is missing.");
            AssertTrue(Enum.IsDefined(
                    typeof(ActivityPlayerAdmissionFlowDisposition),
                    ActivityPlayerAdmissionFlowDisposition.RejectBlocked),
                "P3K.7A RejectBlocked disposition is missing.");
            AssertTrue(Enum.IsDefined(
                    typeof(ActivityPlayerAdmissionFlowDisposition),
                    ActivityPlayerAdmissionFlowDisposition.RejectFailed),
                "P3K.7A RejectFailed disposition is missing.");
        }

        private static void ValidateNoUnityReferences(Type type)
        {
            PropertyInfo[] properties = type.GetProperties(
                BindingFlags.Instance | BindingFlags.Public);
            for (int index = 0; index < properties.Length; index++)
            {
                Type propertyType = properties[index].PropertyType;
                AssertTrue(!typeof(UnityEngine.Object).IsAssignableFrom(propertyType),
                    $"Public P3K.7A decision retains Unity object reference: " +
                    $"{type.Name}.{properties[index].Name} ({propertyType.Name}).");
            }
        }

        private static void AssertDisposition(
            ActivityPlayerAdmissionFlowDecision decision,
            ActivityPlayerAdmissionFlowDisposition disposition,
            ActivityPlayerAdmissionEvaluationStatus evaluationStatus,
            ActivityPlayerAdmissionEvaluationCode evaluationCode)
        {
            AssertNotNull(decision,
                "P3K.7A gate returned null.");
            if (decision.Disposition != disposition ||
                decision.EvaluationStatus != evaluationStatus ||
                decision.EvaluationCode != evaluationCode)
            {
                throw new InvalidOperationException(
                    $"P3K.7A decision mismatch. expectedDisposition='{disposition}' " +
                    $"actualDisposition='{decision.Disposition}' " +
                    $"expectedStatus='{evaluationStatus}' actualStatus='{decision.EvaluationStatus}' " +
                    $"expectedCode='{evaluationCode}' actualCode='{decision.EvaluationCode}'. " +
                    decision.ToDiagnosticString());
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

        private static void AssertEqual<T>(T expected, T actual, string message)
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
    }
}

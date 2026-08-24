using System;
using ImmersiveFrameworkQA.Player;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using System.Collections.Generic;
using System.Threading.Tasks;
using Immersive.Framework.ActivityFlow;
using Immersive.Framework.ApplicationLifecycle;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Transition;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Play Mode proof that the public Manager-Provisioned lifecycle
    /// projection follows the real Activity, Session and Player readiness
    /// authorities while no Player has joined.
    ///
    /// Internal fixture access is used only to arrange the existing M07
    /// environment. All lifecycle evidence asserted by this regression comes
    /// from LocalPlayerProvisioningAuthoring public APIs.
    /// </summary>
    public static class
        QaManagerProvisionedLifecycleWaitingProjectionRegression
    {
        private const string MenuPath =
            "Immersive Framework/QA/Regressions/Player/" +
            "Run Manager-Provisioned Lifecycle Waiting Projection Regression";

        private const string Prefix =
            "[M07_MANAGER_PROVISIONED_LIFECYCLE_WAITING_PROJECTION_REGRESSION]";

        private const int ExpectedCaseCount = 14;
        private const int StartupFrameBudget = 300;

        [MenuItem(MenuPath, true)]
        private static bool ValidateRun()
        {
            return EditorApplication.isPlaying;
        }

        [MenuItem(MenuPath)]
        private static async void Run()
        {
            await RunAsync();
        }

        /// <summary>
        /// Typed Play Mode entry point for the canonical Player QA orchestrator.
        /// </summary>
        public static Task RunForFullPlayerQaAsync() => RunAsync();

        private static async Task RunAsync()
        {
            var completed = new List<string>();
            QaActivityEntryReadinessFixture fixture = null;

            try
            {
                Require(
                    EditorApplication.isPlaying,
                    "Manager-Provisioned waiting projection regression requires Play Mode.");
                completed.Add("play-mode-required");

                QaM07InternalReconcileSetup
                    .RequirePreparedForCurrentPlayMode();
                completed.Add("m07-setup-confirmed");

                Require(
                    QaH2FrameworkReadiness.TryResolveUniqueHost(
                        out FrameworkRuntimeHost host,
                        out string hostDiagnostic) &&
                    host != null,
                    "Manager-Provisioned waiting projection requires one " +
                    "FrameworkRuntimeHost. " + hostDiagnostic);
                await QaH2FrameworkReadiness.RequireStartedRouteAsync(
                    host,
                    StartupFrameBudget);
                completed.Add("official-host-resolved");

                LocalPlayerProvisioningAuthoring authoring =
                    ResolveProvisioningAuthoring(host);
                Require(
                    authoring != null &&
                    authoring.RuntimeReady,
                    "Ready LocalPlayerProvisioningAuthoring was not resolved from the official host.");
                completed.Add("provisioning-authoring-resolved");

                PlayerParticipationSnapshot initialSession =
                    authoring.RuntimeSnapshot;
                Require(
                    initialSession != null &&
                    initialSession.IsInitialized &&
                    CountJoined(initialSession) == 0,
                    "Regression requires a fresh Play Mode with no joined Players.");
                completed.Add("fresh-session-confirmed");

                fixture =
                    await QaActivityEntryReadinessFixture
                        .CreateAsync();
                completed.Add("waiting-fixture-created");

                PlayerSlotProfile slotProfile =
                    ResolveFirstLocalPlayerSlot();

                ActivityAsset activity =
                    fixture.CreateActivity(
                        "qa.m07.public.waiting-projection",
                        "M07 Public Waiting Projection",
                        ActivityEntryReadinessPolicy.ObserveOnly,
                        ActivityVisualTransitionMode.Fade,
                        TransitionGateMode
                            .InputInteractionAndGameplay,
                        QaM07InternalReconcileSetup
                            .ContentScenePath);

                ConfigurePlayerParticipation(
                    activity,
                    PlayerParticipationRequirementLevel
                        .JoinedSlots,
                    slotProfile);
                completed.Add("waiting-activity-configured");

                FrameworkActivityRequestResult request =
                    await fixture.Activities
                        .RequestActivityAsync(
                            activity,
                            nameof(
                                QaManagerProvisionedLifecycleWaitingProjectionRegression),
                            "m07-public-waiting-projection");

                Require(
                    request.Succeeded,
                    request.Message);
                completed.Add("waiting-entry-succeeded");

                Require(
                    authoring
                        .TryGetManagerProvisionedLifecycleSnapshot(
                            out ManagerProvisionedPlayerLifecycleSnapshot
                                waiting),
                    "Public Authoring endpoint did not expose lifecycle evidence after Activity entry.");

                RequireWaitingForJoin(
                    waiting,
                    activity);
                completed.Add("public-waiting-for-join");

                RequirePendingPlayerContribution(waiting);
                completed.Add("public-player-contribution-pending");

                int waitingOccurrence =
                    waiting.ActivityOccurrence;
                int waitingSessionRevision =
                    waiting.SessionRevision;

                FrameworkActivityRequestResult clear =
                    await fixture.Activities
                        .ClearActivityAsync(
                            nameof(
                                QaManagerProvisionedLifecycleWaitingProjectionRegression),
                            "m07-public-waiting-projection-clear");

                Require(
                    clear.Succeeded,
                    clear.Message);
                completed.Add("waiting-exit-succeeded");

                Require(
                    authoring
                        .TryGetManagerProvisionedLifecycleSnapshot(
                            out ManagerProvisionedPlayerLifecycleSnapshot
                                released),
                    "Public Authoring endpoint did not preserve lifecycle evidence after Activity exit.");

                RequireReleased(
                    released,
                    activity,
                    waitingOccurrence);
                completed.Add("public-released");

                RequireReleasedPlayerContribution(released);
                completed.Add("public-player-contribution-released");

                PlayerParticipationSnapshot finalSession =
                    authoring.RuntimeSnapshot;

                Require(
                    finalSession != null &&
                    finalSession.IsInitialized &&
                    CountJoined(finalSession) == 0 &&
                    released.SessionRevision ==
                        waitingSessionRevision &&
                    released.HostCount == 0 &&
                    !HasMaterializedSlotEvidence(released),
                    "Activity waiting/exit sequence mutated Session participation or leaked Player runtime evidence.");
                completed.Add("session-preserved");

                Require(
                    completed.Count ==
                        ExpectedCaseCount,
                    "Manager-Provisioned waiting projection case count changed unexpectedly.");

                Debug.Log(
                    $"{Prefix} status='Passed' " +
                    $"cases='{completed.Count}' " +
                    $"occurrence='{waitingOccurrence}' " +
                    $"proof='WaitingForJoin,PlayerContributionPreparing,Released,SessionPreserved' " +
                    $"completed='{string.Join(",", completed)}'.");
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' " +
                    $"exception='{exception.GetType().Name}' " +
                    $"message='{Escape(exception.Message)}' " +
                    $"completed='{string.Join(",", completed)}'.");
                throw;
            }
            finally
            {
                if (fixture != null)
                {
                    await fixture.DisposeAsync();
                }
            }
        }

        private static void RequireWaitingForJoin(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            ActivityAsset activity)
        {
            Require(
                snapshot != null &&
                snapshot.IsAvailable,
                "Waiting projection is unavailable.");

            Require(
                snapshot.Status ==
                    ManagerProvisionedPlayerLifecycleStatus
                        .WaitingForJoin,
                "Public projection did not report WaitingForJoin. " +
                Describe(snapshot));

            Require(
                !snapshot.IsReady &&
                !snapshot.IsFailure &&
                !snapshot.IsReleased,
                "WaitingForJoin exposed a terminal convenience flag.");

            Require(
                snapshot.ActivityOccurrence > 0 &&
                string.Equals(
                    snapshot.ActivityName,
                    activity.ActivityName,
                    StringComparison.Ordinal),
                "WaitingForJoin did not preserve current Activity identity and occurrence. " +
                Describe(snapshot));

            Require(
                string.Equals(
                    snapshot.EntryPolicy,
                    PlayerParticipationRequirementLevel
                        .JoinedSlots
                        .ToString(),
                    StringComparison.Ordinal),
                "WaitingForJoin did not expose JoinedSlots participation requirement. " +
                Describe(snapshot));

            Require(
                snapshot.SlotCount > 0 &&
                !HasJoinedSlot(snapshot),
                "WaitingForJoin did not project configured, non-joined Session Slots. " +
                Describe(snapshot));

            Require(
                snapshot.HostCount == 0 &&
                !HasMaterializedSlotEvidence(snapshot),
                "WaitingForJoin exposed technical Host or Actor evidence before join. " +
                Describe(snapshot));
        }

        private static void RequirePendingPlayerContribution(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            Require(
                snapshot.HasGateEvidence &&
                snapshot.GateEvidenceScope ==
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution &&
                snapshot.GateHeld,
                "WaitingForJoin did not expose the held official Player readiness contribution. " +
                Describe(snapshot));

            Require(
                string.Equals(
                    snapshot.ReadinessStatus,
                    "Preparing",
                    StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(
                    snapshot.ReadinessReason),
                "WaitingForJoin did not expose Preparing Player readiness evidence. " +
                Describe(snapshot));

            Require(
                !string.IsNullOrWhiteSpace(
                    snapshot.Diagnostic),
                "Public pending lifecycle projection exposed no diagnostic evidence. " +
                Describe(snapshot));
        }

        private static void RequireReleased(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot,
            ActivityAsset activity,
            int expectedOccurrence)
        {
            Require(
                snapshot != null &&
                snapshot.IsAvailable,
                "Released projection is unavailable.");

            Require(
                snapshot.Status ==
                    ManagerProvisionedPlayerLifecycleStatus
                        .Released &&
                snapshot.IsReleased,
                "Public projection did not report Released after Activity exit. " +
                Describe(snapshot));

            Require(
                snapshot.ActivityOccurrence ==
                    expectedOccurrence &&
                string.Equals(
                    snapshot.ActivityName,
                    activity.ActivityName,
                    StringComparison.Ordinal),
                "Released projection lost occurrence-scoped Activity evidence. " +
                Describe(snapshot));

            Require(
                !snapshot.IsReady &&
                !snapshot.IsFailure,
                "Released projection exposed an incompatible terminal flag.");
        }

        private static void RequireReleasedPlayerContribution(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            Require(
                snapshot.HasGateEvidence &&
                snapshot.GateEvidenceScope ==
                    ManagerProvisionedPlayerGateEvidenceScope
                        .ActivityPlayerReadinessContribution &&
                !snapshot.GateHeld,
                "Released lifecycle did not preserve terminal Player contribution evidence. " +
                Describe(snapshot));

            Require(
                string.Equals(
                    snapshot.ReadinessStatus,
                    "Released",
                    StringComparison.Ordinal),
                "Released lifecycle did not expose Released readiness state. " +
                Describe(snapshot));
        }

        private static PlayerSlotProfile
            ResolveFirstLocalPlayerSlot()
        {
            ImmersiveFrameworkSettingsAsset settings =
                Resources.Load<
                    ImmersiveFrameworkSettingsAsset>(
                    ImmersiveFrameworkSettingsAsset
                        .ResourcesPath);

            GameApplicationAsset application =
                settings != null
                    ? settings.ActiveGameApplication
                    : null;

            PlayerSlotProfile slotProfile = null;

            Require(
                application != null &&
                QaPlayerSessionQaSupport.TryGetSupportedSlot(
                    application,
                    0,
                    out slotProfile) &&
                slotProfile != null,
                "M07 fixture requires the first configured Local Player Slot.");

            return slotProfile;
        }

        private static void ConfigurePlayerParticipation(
            ActivityAsset activity,
            PlayerParticipationRequirementLevel requirement,
            PlayerSlotProfile slotProfile)
        {
            Require(
                activity != null,
                "Activity is required for Player participation configuration.");
            Require(
                slotProfile != null,
                "Explicit Player Slot is required for Player participation configuration.");

            var serialized =
                new SerializedObject(activity);

            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationProjectionMode"),
                ActivityParticipationProjectionMode
                    .ExplicitSlots
                    .ToString());

            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationZeroParticipantPolicy"),
                ActivityParticipationZeroParticipantPolicy
                    .Rejected
                    .ToString());

            SetEnumName(
                RequireProperty(
                    serialized,
                    "playerParticipationRequirementLevel"),
                requirement.ToString());

            SerializedProperty explicitSlots =
                RequireProperty(
                    serialized,
                    "playerParticipationExplicitSlotProfiles");

            explicitSlots.arraySize = 1;
            explicitSlots
                .GetArrayElementAtIndex(0)
                .objectReferenceValue =
                    slotProfile;

            serialized
                .ApplyModifiedPropertiesWithoutUndo();
        }

        private static LocalPlayerProvisioningAuthoring
            ResolveProvisioningAuthoring(
                FrameworkRuntimeHost host)
        {
            bool resolved = QaPlayerRuntimeObservationBridge
                .TryGetLocalPlayerProvisioningAuthoring(
                    host,
                    out LocalPlayerProvisioningAuthoring authoring,
                    out string diagnostic);
            Require(resolved && authoring != null,
                "Official host did not expose Local Player provisioning Authoring. " +
                diagnostic);
            return authoring;
        }

        private static SerializedProperty RequireProperty(
            SerializedObject serialized,
            string propertyName)
        {
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);

            Require(
                property != null,
                $"Serialized property '{propertyName}' was not found.");

            return property;
        }

        private static void SetEnumName(
            SerializedProperty property,
            string enumName)
        {
            Require(
                property.propertyType ==
                    SerializedPropertyType.Enum,
                $"Property '{property.propertyPath}' is not an enum.");

            int index =
                Array.IndexOf(
                    property.enumNames,
                    enumName);

            Require(
                index >= 0,
                $"Enum value '{enumName}' was not found on '{property.propertyPath}'.");

            property.enumValueIndex =
                index;
        }

        private static int CountJoined(
            PlayerParticipationSnapshot snapshot)
        {
            int joined = 0;

            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                if (snapshot.Slots[index].IsJoined)
                {
                    joined++;
                }
            }

            return joined;
        }

        private static bool HasJoinedSlot(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                if (string.Equals(
                        snapshot.Slots[index].SlotState,
                        "Joined",
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMaterializedSlotEvidence(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            for (int index = 0;
                 index < snapshot.Slots.Count;
                 index++)
            {
                ManagerProvisionedPlayerLifecycleSlotSnapshot slot =
                    snapshot.Slots[index];

                if (slot.HasTechnicalHost ||
                    slot.HasSelectedActor ||
                    slot.LogicalActorPrepared ||
                    slot.PhysicalActorMaterialized ||
                    slot.GameplayAdmitted)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Describe(
            ManagerProvisionedPlayerLifecycleSnapshot snapshot)
        {
            return snapshot != null
                ? snapshot.ToDiagnosticString()
                : "snapshot='<null>'.";
        }

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }

        private static void Require(
            bool condition,
            string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(
                    message);
            }
        }
    }
}

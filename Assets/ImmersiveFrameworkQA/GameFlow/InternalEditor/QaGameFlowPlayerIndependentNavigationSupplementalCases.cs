using System;
using System.Reflection;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.RuntimeContent;
using ImmersiveFrameworkQA.Player.Internal.Editor;
using UnityEditor;
using UnityEngine;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    /// <summary>
    /// Runtime implementations used by the fixed 16-case Player-independent
    /// Navigation Regression after the six baseline cases.
    ///
    /// The seven diagnostic-fault cases delegate to the dedicated focused
    /// smoke so there is one source of truth for fault installation, one-shot
    /// consumption, cleanup and post-commit retry evidence.
    /// </summary>
    internal static class QaGameFlowPlayerIndependentNavigationSupplementalCases
    {
        private const string Source =
            nameof(QaGameFlowPlayerIndependentNavigationRegression);

        internal static Task RunPreparationTokenMismatchFailsBeforeCommitAsync() =>
            RunFaultAsync("PreparationTokenMismatch");

        internal static Task RunOwnerMismatchFailsBeforeCommitAsync() =>
            RunFaultAsync("OwnerMismatch");

        internal static Task RunCommittedNotReadyKeepsDestinationAsync() =>
            RunFaultAsync("CommittedTargetNotReady");

        internal static Task RunCommittedFinalizationFailedKeepsDestinationAsync() =>
            RunFaultAsync("CommittedFinalizationFailure");

        internal static Task RunFailedBeforeCommitKeepsOriginAsync() =>
            RunFaultAsync("PreCommitFailure");

        internal static Task RunRuntimeUnavailableTypedFailureAsync() =>
            RunFaultAsync("RuntimeUnavailable");

        internal static Task RunLoadingRejectedBeforePresentationAsync() =>
            RunFaultAsync("LoadingRejectedBeforePresentation");

        private static async Task RunFaultAsync(string scenario)
        {
            Require(
                EditorApplication.isPlaying,
                $"Player-independent Navigation fault case '{scenario}' requires Play Mode.");

            await QaGameFlowDiagnosticFaultLeaseSmoke
                .RunScenarioForRegressionAsync(scenario);
        }

        internal static async Task RunInvalidTokenFailsBeforeCommitAsync()
        {
            const string reason = "invalid-token-fails-before-commit";
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            ActivityPlayerLifecycleAdmissionToken reversibleToken = default;

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                ActivityAsset entryActivity = RequireCurrentActivity(fixture, reason);
                int rootCountBefore = fixture.RuntimeScopeRootCount;

                PrepareSinglePlayerGameplayReady(fixture, reason);

                ActivityAsset targetActivity =
                    fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                        "qa.player-independent.invalid-token.activity",
                        "QA Invalid Token Activity");

                ActivityPlayerLifecycleAdmissionResult preparation =
                    fixture.PrepareSameRouteLifecycle(
                        entryActivity,
                        targetActivity,
                        Source,
                        reason);

                ActivityPlayerLifecycleAdmissionSnapshot ready =
                    fixture.LifecycleSnapshot;
                Require(
                    preparation != null &&
                    preparation.ReadyForTransition &&
                    ready != null &&
                    ready.State ==
                        ActivityPlayerLifecycleAdmissionState.ReadyToCommit &&
                    ready.IsRollbackAvailable &&
                    ready.Token.IsValid,
                    "Invalid-token case could not establish the canonical reversible " +
                    "ReadyToCommit transaction. " +
                    preparation?.ToDiagnosticString());

                reversibleToken = ready.Token;
                object authority = ResolveLifecycleAuthority(fixture);
                ActivityPlayerLifecycleAdmissionResult rejected =
                    InvokeLifecycleOperation(
                        authority,
                        "TryCommit",
                        default(ActivityPlayerLifecycleAdmissionToken),
                        Source,
                        reason);

                string rejectedStatus =
                    rejected?.Status.ToString() ?? string.Empty;
                Require(
                    rejected != null &&
                    rejectedStatus.StartsWith(
                        "Rejected",
                        StringComparison.Ordinal),
                    "Invalid lifecycle token did not return a typed rejection. " +
                    rejected?.ToDiagnosticString());

                ActivityPlayerLifecycleAdmissionSnapshot afterReject =
                    fixture.LifecycleSnapshot;
                Require(
                    afterReject != null &&
                    afterReject.Token == reversibleToken &&
                    afterReject.State ==
                        ActivityPlayerLifecycleAdmissionState.ReadyToCommit &&
                    afterReject.IsRollbackAvailable &&
                    ReferenceEquals(fixture.CurrentActivity, entryActivity) &&
                    fixture.RuntimeScopeRootCount == rootCountBefore + 1,
                    "Invalid lifecycle token changed or committed the valid transaction. " +
                    afterReject?.ToDiagnosticString());

                ActivityPlayerLifecycleAdmissionResult rollback =
                    fixture.RollbackSameRouteLifecycle(
                        reversibleToken,
                        Source,
                        reason + "-rollback");
                reversibleToken = default;

                Require(
                    rollback != null &&
                    rollback.Status ==
                        ActivityPlayerLifecycleAdmissionStatus
                            .SucceededRolledBack &&
                    ReferenceEquals(fixture.CurrentActivity, entryActivity) &&
                    fixture.RuntimeScopeRootCount == rootCountBefore,
                    "Invalid-token case could not roll back the untouched valid transaction. " +
                    rollback?.ToDiagnosticString());

                Debug.Log(
                    "[QA_GAME_FLOW_PLAYER_INDEPENDENT_NAVIGATION][CASE_EVIDENCE] " +
                    "case='invalid-token-fails-before-commit' " +
                    $"rejection='{rejectedStatus}' originPreserved='True' " +
                    "validTransactionRollback='SucceededRolledBack'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                if (fixture != null &&
                    reversibleToken.IsValid)
                {
                    try
                    {
                        fixture.RollbackSameRouteLifecycle(
                            reversibleToken,
                            Source,
                            reason + "-finally-rollback");
                    }
                    catch (Exception exception)
                    {
                        cleanupFailure ??= exception;
                    }
                }

                cleanupFailure = await CleanupFixtureAsync(
                    fixture,
                    cleanupFailure);
            }

            ThrowCombined(executionFailure, cleanupFailure);
        }

        internal static async Task RunInvalidActivityIdTypedFailureAsync()
        {
            const string reason = "invalid-activity-id-typed-failure";
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                ActivityAsset entryActivity = RequireCurrentActivity(fixture, reason);
                ActivityPlayerLifecycleAdmissionSnapshot lifecycleBefore =
                    fixture.LifecycleSnapshot;
                int rootCountBefore = fixture.RuntimeScopeRootCount;

                ActivityAsset invalidActivity =
                    fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                        "qa.player-independent.invalid-id.temporary",
                        "QA Invalid Activity Id");
                var serialized = new SerializedObject(invalidActivity);
                SerializedProperty id =
                    serialized.FindProperty("activityId");
                Require(
                    id != null,
                    "ActivityAsset serialized Activity ID property is unavailable.");
                id.stringValue = string.Empty;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                object result = await fixture.RequestActivityAsync(
                    invalidActivity,
                    Source,
                    reason);

                string kind = GetText(result, "Kind");
                string message = GetText(result, "Message");
                Require(
                    !GetBoolean(result, "Succeeded"),
                    "Invalid Activity ID request unexpectedly succeeded.");
                Require(
                    ContainsInvalidActivityIdEvidence(kind, message),
                    "Invalid Activity ID request did not expose typed invalid-ID evidence. " +
                    $"kind='{kind}' message='{message}'.");
                Require(
                    ReferenceEquals(fixture.CurrentActivity, entryActivity) &&
                    fixture.RuntimeScopeRootCount == rootCountBefore &&
                    fixture.GameplaySnapshot.CandidateCount == 0 &&
                    fixture.GameplaySnapshot.ActivePerSlotHandoffCount == 0 &&
                    SameLifecycleIdentity(
                        lifecycleBefore,
                        fixture.LifecycleSnapshot),
                    "Invalid Activity ID request changed lifecycle or runtime materialization.");

                Debug.Log(
                    "[QA_GAME_FLOW_PLAYER_INDEPENDENT_NAVIGATION][CASE_EVIDENCE] " +
                    "case='invalid-activity-id-typed-failure' " +
                    $"kind='{Escape(kind)}' originPreserved='True' " +
                    "candidates='0' handoffs='0' rootsDelta='0'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                cleanupFailure = await CleanupFixtureAsync(
                    fixture,
                    cleanupFailure);
            }

            ThrowCombined(executionFailure, cleanupFailure);
        }

        internal static async Task RunHostLifecycleAuthorityCoherentAsync()
        {
            const string reason = "host-lifecycle-authority-coherent";
            QaPlayerGameplayAdmissionFixture fixture = null;
            Exception executionFailure = null;
            Exception cleanupFailure = null;
            ActivityAsset entryActivity = null;

            try
            {
                fixture = await QaPlayerGameplayAdmissionFixture.CreateAsync();
                entryActivity = RequireCurrentActivity(fixture, reason);

                PrepareSinglePlayerGameplayReady(fixture, reason);

                ActivityAsset targetActivity =
                    fixture.CreateGameplayReadyAllJoinedSlotsActivity(
                        "qa.player-independent.host-lifecycle-coherent.activity",
                        "QA Host Lifecycle Coherent Activity");

                object result = await fixture.RequestActivityAsync(
                    targetActivity,
                    Source,
                    reason);
                Require(
                    GetBoolean(result, "Succeeded"),
                    "Host/lifecycle coherence request failed. " +
                    GetText(result, "Message"));

                object hostState = GetRequiredProperty(
                    fixture.RuntimeHost,
                    "State");
                ActivityAsset hostActivity =
                    GetRequiredProperty(
                        hostState,
                        "CurrentActivity") as ActivityAsset;
                ActivityPlayerLifecycleAdmissionSnapshot lifecycle =
                    fixture.LifecycleSnapshot;
                RuntimeContentOwner expectedOwner =
                    RuntimeContentOwner.Activity(
                        targetActivity.ActivityId.StableText,
                        targetActivity.ActivityName,
                        RuntimeDefinitionToken.FromUnityObject(targetActivity));

                Require(
                    ReferenceEquals(fixture.CurrentActivity, targetActivity) &&
                    ReferenceEquals(hostActivity, targetActivity),
                    "Host and Game Flow do not project the same current Activity.");
                Require(
                    lifecycle != null &&
                    lifecycle.State ==
                        ActivityPlayerLifecycleAdmissionState.Completed &&
                    lifecycle.LastStatus ==
                        ActivityPlayerLifecycleAdmissionStatus
                            .SucceededLifecycleCompleted &&
                    lifecycle.TargetOwner == expectedOwner &&
                    lifecycle.TargetEnterAdopted &&
                    !lifecycle.IsRollbackAvailable &&
                    !lifecycle.CommitCleanupPending,
                    "Lifecycle authority is not terminal and coherent with the Host. " +
                    lifecycle?.ToDiagnosticString());

                ActivityPlayerLifecycleAdmissionSnapshot projected =
                    fixture.GameplaySnapshot.LifecycleAdmission;
                Require(
                    projected != null &&
                    projected.Token == lifecycle.Token &&
                    projected.State == lifecycle.State &&
                    projected.LastStatus == lifecycle.LastStatus &&
                    projected.TargetOwner == lifecycle.TargetOwner,
                    "Player Gameplay Host projects a lifecycle snapshot different from " +
                    "the canonical lifecycle authority.");

                Debug.Log(
                    "[QA_GAME_FLOW_PLAYER_INDEPENDENT_NAVIGATION][CASE_EVIDENCE] " +
                    "case='host-lifecycle-authority-coherent' " +
                    $"activity='{Escape(targetActivity.ActivityName)}' " +
                    $"state='{lifecycle.State}' status='{lifecycle.LastStatus}' " +
                    "hostProjection='coherent' gameplayProjection='coherent' " +
                    "rollbackAvailable='False'.");
            }
            catch (Exception exception)
            {
                executionFailure = exception;
            }
            finally
            {
                cleanupFailure = await TryRestoreActivityAsync(
                    fixture,
                    entryActivity,
                    reason,
                    cleanupFailure);
                cleanupFailure = await CleanupFixtureAsync(
                    fixture,
                    cleanupFailure);
            }

            ThrowCombined(executionFailure, cleanupFailure);
        }

        private static void PrepareSinglePlayerGameplayReady(
            QaPlayerGameplayAdmissionFixture fixture,
            string reason)
        {
            fixture.AssertCleanBaseline(reason);
            Require(
                fixture.OpenJoining(reason)?.Completed == true,
                $"Could not open joining for '{reason}'.");

            LocalPlayerJoinResult join = fixture.JoinPlayer(reason);
            Require(
                join != null && join.Succeeded,
                $"Could not join the QA Player for '{reason}'.");

            fixture.CreateCurrentActivityScope(Source, reason);

            PlayerActorSelectionResult selection =
                fixture.SelectDefaultActor(
                    join.Slot.PlayerSlotId,
                    Source,
                    reason);
            PlayerActorPreparationResult preparation =
                fixture.PrepareSelectedActor(
                    join.Slot.PlayerSlotId,
                    Source,
                    reason);
            PlayerGameplayRuntimeOperationResult gameplay =
                fixture.EnsureGameplayReady(
                    join.Slot.PlayerSlotId,
                    Source,
                    reason);

            Require(
                selection != null && selection.Succeeded,
                $"Default Actor selection failed for '{reason}'.");
            Require(
                preparation != null && preparation.Succeeded,
                $"Actor preparation failed for '{reason}'. " +
                preparation?.ToDiagnosticString());
            Require(
                gameplay != null &&
                gameplay.Succeeded &&
                gameplay.CurrentAdmission.GameplayReady,
                $"GameplayReady chain failed for '{reason}'. " +
                gameplay?.ToDiagnosticString());
        }

        private static object ResolveLifecycleAuthority(
            QaPlayerGameplayAdmissionFixture fixture)
        {
            MethodInfo resolver = fixture?.GetType().GetMethod(
                "ResolveLifecycleAuthority",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (resolver == null)
            {
                throw new InvalidOperationException(
                    "QA fixture lifecycle authority resolver is unavailable.");
            }

            object authority = resolver.Invoke(fixture, null);
            if (authority == null)
            {
                throw new InvalidOperationException(
                    "QA fixture resolved no lifecycle authority.");
            }

            return authority;
        }

        private static ActivityPlayerLifecycleAdmissionResult
            InvokeLifecycleOperation(
                object authority,
                string methodName,
                ActivityPlayerLifecycleAdmissionToken token,
                string source,
                string reason)
        {
            MethodInfo method = authority.GetType().GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(ActivityPlayerLifecycleAdmissionToken),
                    typeof(string),
                    typeof(string)
                },
                null);
            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Lifecycle authority operation '{methodName}' is unavailable.");
            }

            return method.Invoke(
                authority,
                new object[] { token, source, reason })
                as ActivityPlayerLifecycleAdmissionResult;
        }

        private static ActivityAsset RequireCurrentActivity(
            QaPlayerGameplayAdmissionFixture fixture,
            string caseName)
        {
            ActivityAsset activity = fixture?.CurrentActivity;
            Require(
                activity != null && activity.HasValidActivityId,
                $"Case '{caseName}' requires a valid current Activity.");
            return activity;
        }

        private static bool SameLifecycleIdentity(
            ActivityPlayerLifecycleAdmissionSnapshot expected,
            ActivityPlayerLifecycleAdmissionSnapshot actual)
        {
            if (ReferenceEquals(expected, actual))
            {
                return true;
            }

            if (expected == null || actual == null)
            {
                return expected == null && actual == null;
            }

            return expected.Token == actual.Token &&
                   expected.State == actual.State &&
                   expected.LastStatus == actual.LastStatus &&
                   expected.PreviousOwner == actual.PreviousOwner &&
                   expected.TargetOwner == actual.TargetOwner &&
                   expected.IsRollbackAvailable ==
                   actual.IsRollbackAvailable;
        }

        private static bool ContainsInvalidActivityIdEvidence(
            string kind,
            string message)
        {
            string evidence = (kind + " " + message).ToLowerInvariant();
            return evidence.Contains("invalid") &&
                   evidence.Contains("activ") &&
                   evidence.Contains("id");
        }

        private static async Task<Exception> TryRestoreActivityAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            ActivityAsset entryActivity,
            string reason,
            Exception cleanupFailure)
        {
            if (fixture == null ||
                entryActivity == null ||
                ReferenceEquals(fixture.CurrentActivity, entryActivity))
            {
                return cleanupFailure;
            }

            try
            {
                object restore = await fixture.RequestActivityAsync(
                    entryActivity,
                    Source,
                    reason + "-restore-entry");
                if (!GetBoolean(restore, "Succeeded") ||
                    !ReferenceEquals(fixture.CurrentActivity, entryActivity))
                {
                    throw new InvalidOperationException(
                        "Could not restore the entry Activity. " +
                        GetText(restore, "Message"));
                }
            }
            catch (Exception exception)
            {
                cleanupFailure ??= exception;
            }

            return cleanupFailure;
        }

        private static async Task<Exception> CleanupFixtureAsync(
            QaPlayerGameplayAdmissionFixture fixture,
            Exception cleanupFailure)
        {
            if (fixture == null)
            {
                return cleanupFailure;
            }

            try
            {
                await fixture.CleanupAsync();
                if (fixture.CleanupFailure != null)
                {
                    throw fixture.CleanupFailure;
                }
            }
            catch (Exception exception)
            {
                cleanupFailure ??= exception;
            }

            return cleanupFailure;
        }

        private static object GetRequiredProperty(
            object target,
            string propertyName)
        {
            PropertyInfo property = target?.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Runtime evidence property '{propertyName}' is unavailable " +
                    $"on '{target?.GetType().FullName}'.");
            }

            return property.GetValue(target);
        }

        private static bool GetBoolean(
            object target,
            string propertyName)
        {
            return GetRequiredProperty(target, propertyName) is bool value &&
                   value;
        }

        private static string GetText(
            object target,
            string propertyName)
        {
            object value = GetRequiredProperty(target, propertyName);
            return value?.ToString() ?? string.Empty;
        }

        private static void ThrowCombined(
            Exception executionFailure,
            Exception cleanupFailure)
        {
            if (executionFailure != null &&
                cleanupFailure != null)
            {
                throw new AggregateException(
                    "Game Flow case execution and cleanup both failed.",
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
        }

        private static void Require(
            bool condition,
            string message)
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
                : value.Replace("'", "\\'")
                    .Replace("\r", " ")
                    .Replace("\n", " ");
        }
    }
}

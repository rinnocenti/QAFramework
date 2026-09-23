using System;
using System.Collections.Generic;
using System.Reflection;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.RuntimeContent;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ImmersiveFrameworkQA.Camera.Editor
{
    /// <summary>
    /// Focused IF-ADR-032 certification for Subject occurrence, stale evidence
    /// and Camera Presentation transaction integrity.
    ///
    /// This fixture exercises the current non-MonoBehaviour CameraPresentationRuntime
    /// directly through a QA-local reflection adapter. No product-facing legacy
    /// composition adapter or parallel Camera runtime is introduced.
    /// </summary>
    internal static class QaCamera032SubjectTransactionCertification
    {
        private const string MenuPath =
            "Immersive Framework/QA/Camera/Run CAMERA-032 Subject + Transaction Certification";
        private const string Prefix =
            "[CAMERA-032-SUBJECT-TRANSACTION-CERTIFICATION]";
        private const string AggregateResultKey =
            "ImmersiveFrameworkQA.CAMERA_032_SubjectTransaction.AggregatePassed";

        private static readonly string[] SubjectCases =
        {
            "membership-zero-one-many-one-zero",
            "required-target-release-restores-default",
            "fixed-zero-subject-publishes",
            "fresh-subject-occurrence-after-rejoin",
            "stale-availability-cannot-reactivate",
            "stale-selection-cannot-overwrite-current",
            "stale-membership-token-rejected",
            "stale-presentation-rollback-rejected",
            "stale-membership-rollback-rejected"
        };

        private static readonly string[] TransactionCases =
        {
            "rig-apply-failure-preserves-coherent-state",
            "target-projection-failure-preserves-current-state",
            "request-admission-failure-rolls-back",
            "force-default-survives-failed-admission",
            "request-release-failure-restores-state",
            "release-rollback-retry-completes",
            "critical-rollback-failure-explicit",
            "owner-safe-release-preserves-other-presentation"
        };

        [MenuItem(MenuPath, priority = 237)]
        private static void Run()
        {
            SessionState.SetBool(
                AggregateResultKey,
                false);

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError(
                    $"{Prefix} status='Failed' verdict='CAMERA_032_SUBJECT_TRANSACTION_FAIL' " +
                    "diagnostic='Certification must run in Edit Mode.'.");
                return;
            }

            var completedSubject =
                new List<string>(SubjectCases.Length);
            var completedTransaction =
                new List<string>(TransactionCases.Length);
            var failures =
                new List<string>();

            RunSubjectCases(
                completedSubject,
                failures);
            RunTransactionCases(
                completedTransaction,
                failures);

            int totalCompleted =
                completedSubject.Count +
                completedTransaction.Count;
            int totalExpected =
                SubjectCases.Length +
                TransactionCases.Length;

            if (failures.Count == 0 &&
                completedSubject.Count == SubjectCases.Length &&
                completedTransaction.Count == TransactionCases.Length)
            {
                SessionState.SetBool(
                    AggregateResultKey,
                    true);
                Debug.Log(
                    $"{Prefix} status='Passed' " +
                    "verdict='CAMERA_032_SUBJECT_TRANSACTION_CERTIFIED' " +
                    $"subjectCases='{completedSubject.Count}/{SubjectCases.Length}' " +
                    $"transactionCases='{completedTransaction.Count}/{TransactionCases.Length}' " +
                    $"totalCases='{totalCompleted}/{totalExpected}' " +
                    "cleanup='TransientPreviewScenesClosed'.");
                return;
            }

            Debug.LogError(
                $"{Prefix} status='Failed' " +
                "verdict='CAMERA_032_SUBJECT_TRANSACTION_FAIL' " +
                $"subjectCases='{completedSubject.Count}/{SubjectCases.Length}' " +
                $"transactionCases='{completedTransaction.Count}/{TransactionCases.Length}' " +
                $"totalCases='{totalCompleted}/{totalExpected}' " +
                $"failed='{failures.Count}' " +
                $"diagnostic='{Escape(string.Join(" | ", failures))}' " +
                "cleanup='TransientPreviewScenesClosed'.");
        }

        internal static void RunForAggregate()
        {
            Run();
        }

        internal static bool AggregatePassed =>
            SessionState.GetBool(
                AggregateResultKey,
                false);

        private static void RunSubjectCases(
            ICollection<string> completed,
            ICollection<string> failures)
        {
            Execute(
                completed,
                failures,
                SubjectCases[0],
                CaseMembershipZeroOneManyOneZero);
            Execute(
                completed,
                failures,
                SubjectCases[1],
                CaseRequiredTargetReleaseRestoresDefault);
            Execute(
                completed,
                failures,
                SubjectCases[2],
                CaseFixedZeroSubjectPublishes);
            Execute(
                completed,
                failures,
                SubjectCases[3],
                CaseFreshSubjectOccurrenceAfterRejoin);
            Execute(
                completed,
                failures,
                SubjectCases[4],
                CaseStaleAvailabilityCannotReactivate);
            Execute(
                completed,
                failures,
                SubjectCases[5],
                CaseStaleSelectionCannotOverwriteCurrent);
            Execute(
                completed,
                failures,
                SubjectCases[6],
                CaseStaleMembershipTokenRejected);
            Execute(
                completed,
                failures,
                SubjectCases[7],
                CaseStalePresentationRollbackRejected);
            Execute(
                completed,
                failures,
                SubjectCases[8],
                CaseStaleMembershipRollbackRejected);
        }

        private static void RunTransactionCases(
            ICollection<string> completed,
            ICollection<string> failures)
        {
            Execute(
                completed,
                failures,
                TransactionCases[0],
                CaseRigApplyFailurePreservesCoherentState);
            Execute(
                completed,
                failures,
                TransactionCases[1],
                CaseTargetProjectionFailurePreservesCurrentState);
            Execute(
                completed,
                failures,
                TransactionCases[2],
                CaseRequestAdmissionFailureRollsBack);
            Execute(
                completed,
                failures,
                TransactionCases[3],
                CaseForceDefaultSurvivesFailedAdmission);
            Execute(
                completed,
                failures,
                TransactionCases[4],
                CaseRequestReleaseFailureRestoresState);
            Execute(
                completed,
                failures,
                TransactionCases[5],
                CaseReleaseRollbackRetryCompletes);
            Execute(
                completed,
                failures,
                TransactionCases[6],
                CaseCriticalRollbackFailureExplicit);
            Execute(
                completed,
                failures,
                TransactionCases[7],
                CaseOwnerSafeReleasePreservesOtherPresentation);
        }

        private static void CaseMembershipZeroOneManyOneZero()
        {
            using var f =
                new Fixture(
                    "membership-cycle",
                    CameraRigPresentationIntent.Group);

            RequireDefault(
                f,
                "zero");

            CameraSubjectAvailabilityToken b =
                f.AddSubject("subject-b");
            RequirePresentation(
                f,
                1,
                "one");

            CameraRequestId requestId =
                f.Composition.RequestId;

            CameraSubjectAvailabilityToken a =
                f.AddSubject("subject-a");
            RequirePresentation(
                f,
                2,
                "many");

            Require(
                f.Composition.RequestId == requestId,
                "Group membership transition replaced the Presentation request occurrence.");

            RequireGroupOrder(
                f.PresentationRig,
                f.RequireSubject("subject-a"),
                f.RequireSubject("subject-b"));

            f.RemoveSubject(b);
            RequirePresentation(
                f,
                1,
                "many-to-one");
            RequireGroupOrder(
                f.PresentationRig,
                f.RequireSubject("subject-a"));

            f.RemoveSubject(a);
            RequireDefault(
                f,
                "one-to-zero");

            Require(
                f.Composition.Snapshot.SubjectCount == 0,
                "Zero-membership terminal state retained Camera Subjects.");
        }

        private static void CaseRequiredTargetReleaseRestoresDefault()
        {
            using var f =
                new Fixture(
                    "required-target-release",
                    CameraRigPresentationIntent.Follow);

            RequireDefault(
                f,
                "baseline");

            CameraSubjectAvailabilityToken token =
                f.AddSubject("subject-a");
            RequirePresentation(
                f,
                1,
                "subject-present");

            f.RemoveSubject(token);

            Require(
                f.Composition.Snapshot.LastReconcileStatus ==
                    CameraSharedCompositionReconcileStatus
                        .SucceededAwaitingSubjects,
                "Required-target Presentation did not enter awaiting-Subjects state after losing its exact Subject.");
            RequireDefault(
                f,
                "required-subject-lost");
            Require(
                f.PresentationRig.CinemachineCamera.Follow == null,
                "Required-target Presentation retained a stale Follow target.");
        }

        private static void CaseFixedZeroSubjectPublishes()
        {
            using var f =
                new Fixture(
                    "fixed-zero-subject",
                    CameraRigPresentationIntent.Fixed);

            RequirePresentation(
                f,
                0,
                "fixed-zero-subject");

            Require(
                f.Composition.Snapshot.SubjectCount == 0,
                "Fixed Presentation synthesized Camera Subject membership.");
            Require(
                f.PresentationRig.CinemachineCamera.Follow == null &&
                f.PresentationRig.CinemachineCamera.LookAt == null,
                "Fixed Presentation created a fake target while zero Subjects were available.");
        }

        private static void CaseFreshSubjectOccurrenceAfterRejoin()
        {
            using var f =
                new Fixture(
                    "fresh-occurrence",
                    CameraRigPresentationIntent.Follow);

            CameraSubjectAvailabilityToken first =
                f.AddSubject("subject-a");
            CameraSubjectAvailabilityEntry firstEntry =
                f.RequireSubject("subject-a");
            CameraCompositionMembershipEntry firstMember =
                f.RequireMember("subject-a");

            Transform firstObservation =
                firstEntry.Subject.Observation;

            f.RemoveSubject(first);
            RequireDefault(
                f,
                "between-occurrences");

            CameraSubjectAvailabilityToken second =
                f.AddSubject("subject-a");
            CameraSubjectAvailabilityEntry secondEntry =
                f.RequireSubject("subject-a");
            CameraCompositionMembershipEntry secondMember =
                f.RequireMember("subject-a");

            Require(
                second != first,
                "Rejoin reused the previous Camera Subject availability token.");
            Require(
                secondEntry.Token != firstEntry.Token,
                "Rejoin retained stale Camera Subject occurrence evidence.");
            Require(
                secondMember.Token != firstMember.Token,
                "Rejoin retained the previous Camera membership occurrence token.");
            Require(
                !ReferenceEquals(
                    secondEntry.Subject.Observation,
                    firstObservation),
                "Rejoin reused the previous Subject observation Transform.");
            Require(
                ReferenceEquals(
                    f.PresentationRig.CinemachineCamera.Follow,
                    secondEntry.Subject.Observation),
                "Presentation did not consume the fresh Subject occurrence.");
        }

        private static void CaseStaleAvailabilityCannotReactivate()
        {
            using var f =
                new Fixture(
                    "stale-availability",
                    CameraRigPresentationIntent.Follow);

            CameraSubjectAvailabilityToken token =
                f.AddSubject("subject-a");
            CameraSubjectAvailabilitySnapshot stale =
                f.Availability.CreateSnapshot();

            f.RemoveSubject(token);
            RequireDefault(
                f,
                "after-release");

            CameraSharedCompositionSnapshot rejected =
                f.Composition.Reconcile(stale);

            Require(
                rejected.LastReconcileStatus ==
                    CameraSharedCompositionReconcileStatus
                        .RejectedStaleAvailabilitySnapshot,
                $"Stale availability was not rejected. status='{rejected.LastReconcileStatus}'.");
            RequireDefault(
                f,
                "after-stale-reconcile");
        }

        private static void CaseStaleSelectionCannotOverwriteCurrent()
        {
            using var f =
                new Fixture(
                    "stale-selection",
                    CameraRigPresentationIntent.Follow,
                    CameraSharedCompositionSubjectPolicyKind
                        .ExplicitSelection);

            f.AddSubject("subject-a");
            f.AddSubject("subject-b");

            CameraCompositionSubjectSelectionResult selectA =
                f.Selection.Replace(
                    new[]
                    {
                        new CameraSubjectId(
                            "subject-a")
                    });
            Require(
                selectA.Succeeded,
                selectA.Message);

            CameraCompositionSubjectSelectionSnapshot stale =
                selectA.Snapshot;

            CameraCompositionSubjectSelectionResult selectB =
                f.Selection.Replace(
                    new[]
                    {
                        new CameraSubjectId(
                            "subject-b")
                    });
            Require(
                selectB.Succeeded,
                selectB.Message);

            CameraSubjectAvailabilityEntry b =
                f.RequireSubject("subject-b");
            Require(
                ReferenceEquals(
                    f.PresentationRig.CinemachineCamera.Follow,
                    b.Subject.Observation),
                "Current explicit selection did not present Subject B.");

            CameraSharedCompositionSnapshot rejected =
                f.Composition.Reconcile(
                    f.Availability.CreateSnapshot(),
                    stale);

            Require(
                rejected.LastReconcileStatus ==
                    CameraSharedCompositionReconcileStatus
                        .RejectedStaleSubjectSelectionSnapshot,
                $"Stale selection was not rejected. status='{rejected.LastReconcileStatus}'.");
            Require(
                ReferenceEquals(
                    f.PresentationRig.CinemachineCamera.Follow,
                    b.Subject.Observation),
                "Stale explicit selection overwrote the current Subject occurrence.");
        }

        private static void CaseStaleMembershipTokenRejected()
        {
            using var f =
                new Fixture(
                    "stale-membership-token",
                    CameraRigPresentationIntent.Follow);

            CameraSubjectAvailabilityToken first =
                f.AddSubject("subject-a");
            CameraCompositionMembershipContext membership =
                f.RuntimeMembership;
            CameraCompositionMembershipToken oldToken =
                f.RequireMember("subject-a").Token;

            f.RemoveSubject(first);
            f.AddSubject("subject-a");

            CameraCompositionMembershipEntry current =
                f.RequireMember("subject-a");

            CameraCompositionMembershipResult rejected =
                membership.TryRelease(
                    oldToken,
                    f.Availability.CreateSnapshot());

            Require(
                rejected.Status ==
                    CameraCompositionMembershipStatus
                        .RejectedForeignOrStaleToken,
                $"Old membership occurrence token was not rejected. status='{rejected.Status}'.");
            Require(
                membership.Count == 1 &&
                membership.Snapshot.TryGet(
                    new CameraSubjectId(
                        "subject-a"),
                    out CameraCompositionMembershipEntry retained) &&
                retained.Token == current.Token,
                "Rejected stale membership token disturbed the current occurrence.");
        }

        private static void CaseStalePresentationRollbackRejected()
        {
            using var f =
                new Fixture(
                    "stale-presentation-rollback",
                    CameraRigPresentationIntent.Group);

            f.AddSubject("subject-a");
            object previous =
                InvokeNonPublic(
                    f.PresentationRig,
                    "CapturePresentationState");

            f.AddSubject("subject-b");
            object expectedCurrent =
                InvokeNonPublic(
                    f.PresentationRig,
                    "CapturePresentationState");

            f.AddSubject("subject-c");

            object restore =
                InvokeNonPublic(
                    f.PresentationRig,
                    "RestorePresentationState",
                    previous,
                    expectedCurrent);

            Require(
                restore != null,
                "Presentation rollback returned no result.");
            Require(
                !GetProperty<bool>(
                    restore,
                    "Succeeded"),
                "Stale Presentation rollback unexpectedly succeeded.");
            Require(
                string.Equals(
                    GetProperty<object>(
                        restore,
                        "Status")
                        ?.ToString(),
                    "RejectedStaleTransaction",
                    StringComparison.Ordinal),
                $"Presentation rollback returned unexpected status '{GetProperty<object>(restore, "Status")}'.");
            Require(
                f.PresentationRig
                        .FrameworkOwnedGroupTargetGroup !=
                    null &&
                f.PresentationRig
                        .FrameworkOwnedGroupTargetGroup
                        .Targets.Count ==
                    3,
                "Stale Presentation rollback overwrote newer Group evidence.");
        }

        private static void CaseStaleMembershipRollbackRejected()
        {
            using var f =
                new Fixture(
                    "stale-membership-rollback",
                    CameraRigPresentationIntent.Group);

            f.AddSubject("subject-a");
            CameraCompositionMembershipContext membership =
                f.RuntimeMembership;
            CameraCompositionMembershipSnapshot previous =
                membership.Snapshot;

            f.AddSubject("subject-b");
            CameraCompositionMembershipSnapshot expectedCurrent =
                membership.Snapshot;

            f.AddSubject("subject-c");

            object[] arguments =
            {
                previous,
                expectedCurrent,
                null
            };

            bool restored =
                (bool)InvokeNonPublicWithArguments(
                    membership,
                    "TryRestore",
                    arguments);

            string diagnostic =
                arguments[2] as string ??
                string.Empty;

            Require(
                !restored,
                "Stale membership rollback unexpectedly succeeded.");
            Require(
                diagnostic.IndexOf(
                    "newer",
                    StringComparison.OrdinalIgnoreCase) >=
                0,
                $"Stale membership rollback did not report newer evidence. diagnostic='{diagnostic}'.");
            Require(
                membership.Count == 3,
                "Stale membership rollback overwrote newer membership.");
        }

        private static void CaseRigApplyFailurePreservesCoherentState()
        {
            using var f =
                new Fixture(
                    "rig-apply-failure",
                    CameraRigPresentationIntent.Follow);

            SetVector3(
                f.PresentationBehavior,
                "followOffset",
                new Vector3(
                    float.NaN,
                    0f,
                    0f));

            f.AddSubject("subject-a");

            Require(
                f.Composition.Snapshot.LastReconcileStatus ==
                    CameraSharedCompositionReconcileStatus
                        .BlockedPresentationFailure,
                $"Rig apply failure returned unexpected status '{f.Composition.Snapshot.LastReconcileStatus}'.");
            Require(
                f.Composition.Snapshot.SubjectCount == 0,
                "Rig apply failure partially committed Subject membership.");
            RequireDefault(
                f,
                "rig-apply-failure");
            Require(
                f.PresentationRig.CinemachineCamera.Follow == null,
                "Rig apply failure left a partially applied Follow target.");
        }

        private static void CaseTargetProjectionFailurePreservesCurrentState()
        {
            using var f =
                new Fixture(
                    "target-projection-failure",
                    CameraRigPresentationIntent.Follow);

            f.AddSubject("subject-a");
            CameraSubjectAvailabilityEntry a =
                f.RequireSubject("subject-a");
            Transform previousFollow =
                f.PresentationRig.CinemachineCamera.Follow;
            CameraRequestId request =
                f.Composition.RequestId;

            f.AddSubject("subject-b");

            Require(
                f.Composition.Snapshot.LastReconcileStatus ==
                    CameraSharedCompositionReconcileStatus
                        .BlockedProjectionFailure,
                $"Target projection failure returned unexpected status '{f.Composition.Snapshot.LastReconcileStatus}'.");
            Require(
                f.Composition.Snapshot.SubjectCount == 1,
                "Target projection failure changed the committed membership.");
            Require(
                f.Composition.IsRequestPublished &&
                f.Output.Context.Contains(
                    request),
                "Target projection failure released the previously valid Camera request.");
            Require(
                ReferenceEquals(
                    f.PresentationRig.CinemachineCamera.Follow,
                    previousFollow) &&
                ReferenceEquals(
                    previousFollow,
                    a.Subject.Observation),
                "Target projection failure overwrote the previous coherent presentation.");
        }

        private static void CaseRequestAdmissionFailureRollsBack()
        {
            using var f =
                new Fixture(
                    "request-admission-failure",
                    CameraRigPresentationIntent.Follow);

            CameraRequest conflicting =
                f.CreateRequest(
                    "conflicting",
                    f.CreateRig(
                        CameraRigPresentationIntent.Fixed),
                    10,
                    f.Composition.MembershipContextIdText);

            CameraOutputSessionResult admitted =
                f.Output.Session.Admit(
                    conflicting);
            Require(
                admitted.Succeeded,
                admitted.DiagnosticSummary);

            f.AddSubject("subject-a");

            Require(
                f.Composition.Snapshot.LastReconcileStatus ==
                    CameraSharedCompositionReconcileStatus
                        .BlockedRequestFailure,
                $"Request admission failure returned unexpected status '{f.Composition.Snapshot.LastReconcileStatus}'.");
            Require(
                f.Output.Context.AdmittedRequestCount == 1 &&
                f.Output.Context.HasWinner &&
                f.Output.Context.Winner.RequestId ==
                    conflicting.RequestId,
                "Admission rollback disturbed the pre-existing winner.");
            Require(
                !f.Output.Context.Contains(
                    f.Composition.RequestId) &&
                !f.Composition.IsRequestPublished,
                "Failed Presentation request remained admitted.");
            Require(
                f.Composition.Snapshot.SubjectCount == 0 &&
                f.PresentationRig.CinemachineCamera.Follow == null,
                "Admission rollback did not restore Presentation/membership state.");
        }

        private static void CaseForceDefaultSurvivesFailedAdmission()
        {
            using var f =
                new Fixture(
                    "force-default-failed-admission",
                    CameraRigPresentationIntent.Follow);

            CameraRequest conflicting =
                f.CreateRequest(
                    "conflicting",
                    f.CreateRig(
                        CameraRigPresentationIntent.Fixed),
                    10,
                    f.Composition.MembershipContextIdText);
            Require(
                f.Output.Session.Admit(
                    conflicting).Succeeded,
                "Could not establish conflicting normal Camera request.");

            var owner =
                new CameraOutputForceDefaultOwnerId(
                    "qa.camera.032.transaction-force-default");

            Require(
                f.Output.Session.ForceDefault(
                    owner).Succeeded,
                "Could not establish force-default baseline.");

            try
            {
                f.AddSubject("subject-a");

                Require(
                    f.Composition.Snapshot.LastReconcileStatus ==
                        CameraSharedCompositionReconcileStatus
                            .BlockedRequestFailure,
                    "Failed normal request admission was not observed while force-default was active.");
                Require(
                    f.Output.Session.IsDefaultForced &&
                    f.Output.Applicator.HasAppliedDefault,
                    "Failed normal request operation disturbed force-default physical continuity.");
                Require(
                    !f.Output.Context.Contains(
                        f.Composition.RequestId),
                    "Failed Presentation request remained admitted under force-default.");
                Require(
                    f.Output.Context.HasWinner &&
                    f.Output.Context.Winner.RequestId ==
                        conflicting.RequestId,
                    "Failed Presentation admission changed the logical normal winner.");
            }
            finally
            {
                CameraOutputApplyResult released =
                    f.Output.Session.ReleaseForceDefault(
                        owner);
                Require(
                    released.Succeeded,
                    released.DiagnosticSummary);
            }
        }

        private static void CaseRequestReleaseFailureRestoresState()
        {
            using var f =
                new Fixture(
                    "request-release-failure",
                    CameraRigPresentationIntent.Follow);

            CameraSubjectAvailabilityToken token =
                f.AddSubject("subject-a");
            Transform previousFollow =
                f.PresentationRig.CinemachineCamera.Follow;
            CinemachineCamera defaultCamera =
                f.DefaultRig.CinemachineCamera;

            SetPrivateField(
                f.DefaultRig,
                "cinemachineCamera",
                null);

            try
            {
                f.RemoveSubject(token);

                Require(
                    f.Composition.Snapshot.LastReconcileStatus ==
                        CameraSharedCompositionReconcileStatus
                            .BlockedRequestFailure,
                    $"Request release rollback returned unexpected status '{f.Composition.Snapshot.LastReconcileStatus}'.");
                Require(
                    f.Output.Context.Contains(
                        f.Composition.RequestId) &&
                    f.Composition.IsRequestPublished,
                    "Release failure did not restore the Presentation request.");
                Require(
                    f.Composition.Snapshot.SubjectCount == 1,
                    "Release failure did not restore previous Subject membership.");
                Require(
                    ReferenceEquals(
                        f.PresentationRig.CinemachineCamera.Follow,
                        previousFollow),
                    "Release failure did not restore previous Follow evidence.");
                Require(
                    ReferenceEquals(
                        f.Output.Applicator.AppliedCamera,
                        f.PresentationRig.CinemachineCamera),
                    "Release rollback did not restore the physical previous winner.");
            }
            finally
            {
                SetPrivateField(
                    f.DefaultRig,
                    "cinemachineCamera",
                    defaultCamera);
            }
        }

        private static void CaseReleaseRollbackRetryCompletes()
        {
            using var f =
                new Fixture(
                    "release-rollback-retry",
                    CameraRigPresentationIntent.Follow);

            CameraSubjectAvailabilityToken token =
                f.AddSubject("subject-a");
            CinemachineCamera defaultCamera =
                f.DefaultRig.CinemachineCamera;

            SetPrivateField(
                f.DefaultRig,
                "cinemachineCamera",
                null);

            try
            {
                f.RemoveSubject(token);
                Require(
                    f.Composition.Snapshot.LastReconcileStatus ==
                        CameraSharedCompositionReconcileStatus
                            .BlockedRequestFailure,
                    "Precondition release rollback did not occur.");
            }
            finally
            {
                SetPrivateField(
                    f.DefaultRig,
                    "cinemachineCamera",
                    defaultCamera);
            }

            CameraSharedCompositionSnapshot retry =
                f.Composition.Reconcile(
                    f.Availability.CreateSnapshot());

            Require(
                retry.LastReconcileStatus ==
                    CameraSharedCompositionReconcileStatus
                        .SucceededAwaitingSubjects,
                $"Retry after successful rollback did not complete. status='{retry.LastReconcileStatus}' issue='{retry.LastBlockingIssue}'.");
            RequireDefault(
                f,
                "release-retry");
        }

        private static void CaseCriticalRollbackFailureExplicit()
        {
            using var f =
                new Fixture(
                    "critical-rollback",
                    CameraRigPresentationIntent.Group);

            f.AddSubject("subject-a");

            object runtime =
                f.Composition.Runtime;
            ICameraRequestPublisher originalPublisher =
                GetPrivateField<ICameraRequestPublisher>(
                    runtime,
                    "_requestPublisher");
            CinemachineTargetGroup group =
                f.PresentationRig
                    .FrameworkOwnedGroupTargetGroup;

            Require(
                originalPublisher != null &&
                originalPublisher.IsPublished &&
                group != null,
                "Critical rollback case could not establish its published Group baseline.");

            var rejecting =
                new MutatingRejectingPublisher(
                    originalPublisher.Request,
                    () => SetPrivateField(
                        f.PresentationRig,
                        "frameworkOwnedGroupTargetGroup",
                        null));

            SetPrivateField(
                runtime,
                "_requestPublisher",
                rejecting);

            try
            {
                int released =
                    f.Availability.ReleaseOwner(
                        f.SubjectOwner);
                Require(
                    released == 1,
                    $"Critical rollback case released '{released}' Subjects instead of one.");

                Require(
                    f.Composition.Snapshot.LastReconcileStatus ==
                        CameraSharedCompositionReconcileStatus
                            .CriticalRollbackFailure,
                    $"Rollback failure was not explicit terminal evidence. status='{f.Composition.Snapshot.LastReconcileStatus}' issue='{f.Composition.Snapshot.LastBlockingIssue}'.");
                Require(
                    f.Composition.Snapshot.LastBlockingIssue
                        .IndexOf(
                            "rollback",
                            StringComparison.OrdinalIgnoreCase) >=
                        0,
                    "Critical rollback failure omitted rollback diagnostics.");
                Require(
                    !f.Composition.Snapshot.IsReady,
                    "Critical rollback failure incorrectly reported a ready Presentation.");
            }
            finally
            {
                SetPrivateField(
                    f.PresentationRig,
                    "frameworkOwnedGroupTargetGroup",
                    group);
                SetPrivateField(
                    runtime,
                    "_requestPublisher",
                    originalPublisher);

                f.Composition.Reconcile(
                    f.Availability.CreateSnapshot());
            }

            RequireDefault(
                f,
                "critical-rollback-cleanup");
        }

        private static void CaseOwnerSafeReleasePreservesOtherPresentation()
        {
            using var f =
                new Fixture(
                    "owner-safe-release",
                    CameraRigPresentationIntent.Fixed,
                    CameraSharedCompositionSubjectPolicyKind
                        .AllAvailableSubjects,
                    10);

            AdditionalPresentation second =
                f.CreateAdditionalPresentation(
                    "second",
                    CameraRigPresentationIntent.Fixed,
                    20);

            Require(
                f.Output.Context.AdmittedRequestCount == 2 &&
                f.Output.Context.Winner.RequestId ==
                    second.Composition.RequestId,
                "Two Presentation occurrences did not establish deterministic arbitration.");

            ICameraRequestPublisher firstPublisher =
                GetRuntimePublisher(
                    f.Composition);
            ICameraRequestPublisher secondPublisher =
                GetRuntimePublisher(
                    second.Composition);

            Require(
                firstPublisher != null &&
                secondPublisher != null &&
                firstPublisher.Request.Owner.OwnerScopeId !=
                    secondPublisher.Request.Owner.OwnerScopeId,
                "Independent Presentation occurrences share one request owner scope.");

            f.Composition.DetachOutputSession(
                "CAMERA-032 owner-safe non-winner release");

            Require(
                f.Output.Context.AdmittedRequestCount == 1 &&
                f.Output.Context.Contains(
                    second.Composition.RequestId) &&
                f.Output.Context.Winner.RequestId ==
                    second.Composition.RequestId,
                "Releasing the non-winning Presentation disturbed another owner's request. " +
                DescribeOwnerSafeState(
                    f,
                    second));

            f.Composition.AttachOutputSession(
                f.Output);

            Require(
                f.Output.Context.AdmittedRequestCount == 2 &&
                f.Output.Context.Winner.RequestId ==
                    second.Composition.RequestId,
                "Reattached lower Presentation did not republish independently. " +
                DescribeOwnerSafeState(
                    f,
                    second));

            second.Composition.DetachOutputSession(
                "CAMERA-032 owner-safe winner release");

            Require(
                f.Output.Context.AdmittedRequestCount == 1 &&
                f.Output.Context.Contains(
                    f.Composition.RequestId) &&
                f.Output.Context.Winner.RequestId ==
                    f.Composition.RequestId,
                "Releasing the winning Presentation tore down the surviving owner's request. " +
                DescribeOwnerSafeState(
                    f,
                    second));
        }

        private static string DescribeOwnerSafeState(
            Fixture fixture,
            AdditionalPresentation second)
        {
            CameraOutputContextSnapshot snapshot =
                fixture.Output.Context.CaptureSnapshot();

            return
                $"admitted='{snapshot.AdmittedRequestCount}' " +
                $"hasWinner='{snapshot.HasWinner}' " +
                $"winner='{(snapshot.HasWinner ? snapshot.Winner.RequestId.Value : string.Empty)}' " +
                $"firstRequest='{fixture.Composition.RequestId.Value}' " +
                $"firstPublished='{fixture.Composition.IsRequestPublished}' " +
                $"firstContained='{fixture.Output.Context.Contains(fixture.Composition.RequestId)}' " +
                $"secondRequest='{second.Composition.RequestId.Value}' " +
                $"secondPublished='{second.Composition.IsRequestPublished}' " +
                $"secondContained='{fixture.Output.Context.Contains(second.Composition.RequestId)}'.";
        }

        private static ICameraRequestPublisher
            GetRuntimePublisher(
                QaCameraPresentationOccurrence composition)
        {
            return GetPrivateField<ICameraRequestPublisher>(
                composition.Runtime,
                "_requestPublisher");
        }

        private static void Execute(
            ICollection<string> completed,
            ICollection<string> failures,
            string id,
            Action body)
        {
            try
            {
                body();
                completed.Add(id);
                Debug.Log(
                    $"{Prefix} case='{id}' status='Passed'.");
            }
            catch (Exception exception)
            {
                string diagnostic =
                    exception.GetBaseException().Message;
                failures.Add(
                    $"{id}: {diagnostic}");
                Debug.LogError(
                    $"{Prefix} case='{id}' status='Failed' " +
                    $"diagnostic='{Escape(diagnostic)}'.");
            }
        }

        private static void RequirePresentation(
            Fixture fixture,
            int expectedSubjects,
            string phase)
        {
            Require(
                fixture.Composition.IsRequestPublished,
                $"Presentation request is not published at '{phase}'.");
            Require(
                fixture.Output.Context.Contains(
                    fixture.Composition.RequestId),
                $"Output does not contain the Presentation request at '{phase}'.");
            Require(
                fixture.Output.Applicator.HasAppliedRequest &&
                fixture.Output.Applicator.AppliedRequestId ==
                    fixture.Output.Context.Winner.RequestId,
                $"Physical Output is not synchronized to its normal winner at '{phase}'.");
            Require(
                fixture.Composition.Snapshot.SubjectCount ==
                    expectedSubjects,
                $"Presentation Subject count diverged at '{phase}'. actual='{fixture.Composition.Snapshot.SubjectCount}' expected='{expectedSubjects}'.");
        }

        private static void RequireDefault(
            Fixture fixture,
            string phase)
        {
            Require(
                !fixture.Composition.IsRequestPublished,
                $"Presentation request remained published at '{phase}'.");
            Require(
                !fixture.Output.Context.Contains(
                    fixture.Composition.RequestId),
                $"Output retained the Presentation request at '{phase}'.");
            Require(
                fixture.Output.Context.AdmittedRequestCount == 0 &&
                !fixture.Output.Context.HasWinner,
                $"Output retained normal Camera requests at '{phase}'.");
            Require(
                fixture.Output.Applicator.HasAppliedDefault &&
                ReferenceEquals(
                    fixture.Output.Applicator.AppliedCamera,
                    fixture.DefaultRig.CinemachineCamera),
                $"Output Default is not physically applied at '{phase}'.");
        }

        private static void RequireGroupOrder(
            CameraRigComposer rig,
            params CameraSubjectAvailabilityEntry[] expected)
        {
            CinemachineTargetGroup group =
                rig.FrameworkOwnedGroupTargetGroup;
            Require(
                group != null &&
                group.Targets != null &&
                group.Targets.Count ==
                    expected.Length,
                $"Group target count diverged. actual='{group?.Targets?.Count ?? -1}' expected='{expected.Length}'.");

            for (int index = 0;
                 index < expected.Length;
                 index++)
            {
                Require(
                    ReferenceEquals(
                        group.Targets[index].Object,
                        expected[index].Subject.Observation),
                    $"Group deterministic order diverged at index='{index}' expectedSubject='{expected[index].Subject.SubjectId.Value}'.");
            }
        }

        private static object InvokeNonPublic(
            object target,
            string methodName,
            params object[] arguments)
        {
            return InvokeNonPublicWithArguments(
                target,
                methodName,
                arguments);
        }

        private static object InvokeNonPublicWithArguments(
            object target,
            string methodName,
            object[] arguments)
        {
            Require(
                target != null,
                $"Reflection target for '{methodName}' is null.");

            MethodInfo method =
                target.GetType().GetMethod(
                    methodName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Require(
                method != null,
                $"Non-public method '{methodName}' is unavailable on '{target.GetType().Name}'.");

            try
            {
                return method.Invoke(
                    target,
                    arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ??
                    exception;
            }
        }

        private static T GetPrivateField<T>(
            object target,
            string fieldName)
        {
            Require(
                target != null,
                $"Reflection target for field '{fieldName}' is null.");

            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Require(
                field != null,
                $"Private field '{fieldName}' is unavailable on '{target.GetType().Name}'.");

            return (T)field.GetValue(
                target);
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            Require(
                target != null,
                $"Reflection target for field '{fieldName}' is null.");

            FieldInfo field =
                target.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Require(
                field != null,
                $"Private field '{fieldName}' is unavailable on '{target.GetType().Name}'.");

            field.SetValue(
                target,
                value);
        }

        private static T GetProperty<T>(
            object target,
            string propertyName)
        {
            Require(
                target != null,
                $"Reflection target for property '{propertyName}' is null.");

            PropertyInfo property =
                target.GetType().GetProperty(
                    propertyName,
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

            Require(
                property != null,
                $"Property '{propertyName}' is unavailable on '{target.GetType().Name}'.");

            return (T)property.GetValue(
                target);
        }

        private static void SetObject(
            UnityEngine.Object target,
            string propertyName,
            UnityEngine.Object value)
        {
            var serialized =
                new SerializedObject(target);
            serialized.Update();
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);
            Require(
                property != null,
                $"Serialized object property '{propertyName}' is unavailable on '{target.GetType().Name}'.");
            property.objectReferenceValue =
                value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(
            UnityEngine.Object target,
            string propertyName,
            bool value)
        {
            var serialized =
                new SerializedObject(target);
            serialized.Update();
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);
            Require(
                property != null,
                $"Serialized bool property '{propertyName}' is unavailable on '{target.GetType().Name}'.");
            property.boolValue =
                value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(
            UnityEngine.Object target,
            string propertyName,
            int value)
        {
            var serialized =
                new SerializedObject(target);
            serialized.Update();
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);
            Require(
                property != null,
                $"Serialized int property '{propertyName}' is unavailable on '{target.GetType().Name}'.");
            property.intValue =
                value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector3(
            UnityEngine.Object target,
            string propertyName,
            Vector3 value)
        {
            var serialized =
                new SerializedObject(target);
            serialized.Update();
            SerializedProperty property =
                serialized.FindProperty(
                    propertyName);
            Require(
                property != null,
                $"Serialized Vector3 property '{propertyName}' is unavailable on '{target.GetType().Name}'.");
            property.vector3Value =
                value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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

        private static string Escape(
            string value)
        {
            return (value ?? string.Empty)
                .Replace(
                    "\\",
                    "\\\\")
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

        private sealed class QaCameraPresentationOccurrence :
            IDisposable
        {
            private static readonly Type RuntimeType =
                typeof(CameraOutputAuthoring).Assembly.GetType(
                    "Immersive.Framework.Camera.CameraPresentationRuntime",
                    true);

            private readonly object runtime;

            internal QaCameraPresentationOccurrence(
                string diagnosticName,
                CameraOutputDefinition outputDefinition,
                CameraSharedCompositionSubjectPolicyKind subjectPolicy,
                CameraRigComposer rig,
                int requestPrecedence,
                string lifecycleOwnerId)
            {
                runtime =
                    Activator.CreateInstance(
                        RuntimeType,
                        BindingFlags.Instance |
                        BindingFlags.NonPublic,
                        null,
                        new object[]
                        {
                            diagnosticName
                        },
                        null);

                var context =
                    new RuntimeScopeContext(
                        RuntimeContentOwner.Session(
                            lifecycleOwnerId,
                            diagnosticName),
                        nameof(
                            QaCamera032SubjectTransactionCertification),
                        "focused-subject-transaction");

                Invoke(
                    "Configure",
                    new[]
                    {
                        typeof(CameraOutputDefinition),
                        typeof(CameraSharedCompositionSubjectPolicyKind),
                        typeof(CameraRigComposer),
                        typeof(int),
                        typeof(CameraPresentationTransitionMode),
                        typeof(RuntimeScopeContext)
                    },
                    outputDefinition,
                    subjectPolicy,
                    rig,
                    requestPrecedence,
                    CameraPresentationTransitionMode.Blend,
                    context);
            }

            internal object Runtime => runtime;

            internal CameraRequestId RequestId =>
                GetProperty<CameraRequestId>(
                    runtime,
                    "RequestId");

            internal bool IsRequestPublished =>
                GetProperty<bool>(
                    runtime,
                    "IsRequestPublished");

            internal string MembershipContextIdText =>
                GetProperty<string>(
                    runtime,
                    "MembershipContextIdText");

            internal string OutputIdText =>
                GetProperty<CameraOutputId>(
                    runtime,
                    "RequestedOutputId").Value ??
                string.Empty;

            internal CameraSharedCompositionSnapshot Snapshot =>
                GetProperty<CameraSharedCompositionSnapshot>(
                    runtime,
                    "Snapshot");

            internal void SetEnabled(
                bool enabled)
            {
                Invoke(
                    "SetEnabled",
                    new[]
                    {
                        typeof(bool)
                    },
                    enabled);
            }

            internal void AttachCameraSubjectAvailability(
                ICameraSubjectAvailabilitySource availability)
            {
                Invoke(
                    "AttachCameraSubjectAvailability",
                    new[]
                    {
                        typeof(ICameraSubjectAvailabilitySource)
                    },
                    availability);
            }

            internal void AttachSubjectSelectionSource(
                ICameraCompositionSubjectSelectionSource selection)
            {
                Invoke(
                    "AttachSubjectSelectionSource",
                    new[]
                    {
                        typeof(ICameraCompositionSubjectSelectionSource)
                    },
                    selection);
            }

            internal void AttachOutputSession(
                CameraOutputAuthoring output)
            {
                Invoke(
                    "AttachOutputSession",
                    new[]
                    {
                        typeof(CameraOutputAuthoring)
                    },
                    output);
            }

            internal void DetachOutputSession(
                string reason)
            {
                Invoke(
                    "DetachOutputSession",
                    new[]
                    {
                        typeof(string)
                    },
                    reason);
            }

            internal CameraSharedCompositionSnapshot Reconcile(
                CameraSubjectAvailabilitySnapshot availability)
            {
                return (CameraSharedCompositionSnapshot)Invoke(
                    "Reconcile",
                    new[]
                    {
                        typeof(CameraSubjectAvailabilitySnapshot)
                    },
                    availability);
            }

            internal CameraSharedCompositionSnapshot Reconcile(
                CameraSubjectAvailabilitySnapshot availability,
                CameraCompositionSubjectSelectionSnapshot selection)
            {
                return (CameraSharedCompositionSnapshot)Invoke(
                    "Reconcile",
                    new[]
                    {
                        typeof(CameraSubjectAvailabilitySnapshot),
                        typeof(CameraCompositionSubjectSelectionSnapshot)
                    },
                    availability,
                    selection);
            }

            public void Dispose()
            {
                ((IDisposable)runtime).Dispose();
            }

            private object Invoke(
                string methodName,
                Type[] parameterTypes,
                params object[] arguments)
            {
                MethodInfo method =
                    RuntimeType.GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.NonPublic,
                        null,
                        parameterTypes,
                        null);

                Require(
                    method != null,
                    $"CameraPresentationRuntime method '{methodName}' is unavailable.");

                try
                {
                    return method.Invoke(
                        runtime,
                        arguments);
                }
                catch (TargetInvocationException exception)
                {
                    throw exception.InnerException ??
                        exception;
                }
            }
        }

        private sealed class Fixture :
            IDisposable
        {
            private readonly string suffix;
            private readonly Scene scene;
            private readonly List<ScriptableObject> definitions =
                new List<ScriptableObject>();
            private readonly List<QaCameraPresentationOccurrence> presentations =
                new List<QaCameraPresentationOccurrence>();
            private bool disposed;

            internal Fixture(
                string suffix,
                CameraRigPresentationIntent intent,
                CameraSharedCompositionSubjectPolicyKind policy =
                    CameraSharedCompositionSubjectPolicyKind
                        .AllAvailableSubjects,
                int precedence = 10)
            {
                this.suffix =
                    suffix;

                scene =
                    EditorSceneManager.NewPreviewScene();
                Require(
                    scene.IsValid() &&
                    scene.isLoaded,
                    $"Could not create transient preview scene for '{suffix}'.");

                OutputDefinition =
                    QaCameraAuthoringFixtures
                        .CreateOutputDefinition();
                definitions.Add(
                    OutputDefinition);

                DefaultRig =
                    CreateRig(
                        CameraRigPresentationIntent.Fixed);

                GameObject outputRoot =
                    Root(
                        $"qa-camera-032-output-{suffix}");
                outputRoot.SetActive(
                    false);

                UnityEngine.Camera unityCamera =
                    outputRoot.AddComponent<UnityEngine.Camera>();
                CinemachineBrain brain =
                    outputRoot.AddComponent<CinemachineBrain>();

                Output =
                    outputRoot.AddComponent<CameraOutputAuthoring>();
                SetObject(
                    Output,
                    "outputDefinition",
                    OutputDefinition);
                SetObject(
                    Output,
                    "unityCamera",
                    unityCamera);
                SetObject(
                    Output,
                    "cinemachineBrain",
                    brain);
                SetObject(
                    Output,
                    "defaultCameraRig",
                    DefaultRig);
                SetBool(
                    Output,
                    "initializeOnAwake",
                    false);
                SetBool(
                    Output,
                    "logDiagnostics",
                    false);

                Require(
                    Output.TryInitialize(
                        out string outputIssue),
                    $"Transient Camera Output failed to initialize. {outputIssue}");

                Availability =
                    new CameraSubjectAvailabilityContext(
                        new SubjectAvailabilityContextId(
                            $"qa-camera-032-availability-{suffix}"));
                SubjectOwner =
                    new CameraSubjectAvailabilityOwnerId(
                        $"qa-camera-032-subject-owner-{suffix}");

                PresentationRig =
                    CreateRig(
                        intent);
                PresentationBehavior =
                    PresentationRig.BehaviorDefinition;

                Composition =
                    new QaCameraPresentationOccurrence(
                        $"qa-camera-032-presentation-{suffix}",
                        OutputDefinition,
                        policy,
                        PresentationRig,
                        precedence,
                        $"qa-camera-032-session-{suffix}");
                presentations.Add(
                    Composition);

                if (policy ==
                    CameraSharedCompositionSubjectPolicyKind
                        .ExplicitSelection)
                {
                    Selection =
                        new CameraCompositionSubjectSelectionContext(
                            new CameraCompositionSubjectSelectionContextId(
                                $"qa-camera-032-selection-{suffix}"));
                    Composition.AttachSubjectSelectionSource(
                        Selection);
                }

                Composition.AttachOutputSession(
                    Output);
                Composition.AttachCameraSubjectAvailability(
                    Availability);
                Composition.SetEnabled(
                    true);

                if (Selection != null)
                {
                    Composition.Reconcile(
                        Availability.CreateSnapshot(),
                        Selection.CurrentSnapshot);
                }
                else
                {
                    Composition.Reconcile(
                        Availability.CreateSnapshot());
                }
            }

            internal CameraOutputDefinition OutputDefinition { get; }

            internal CameraOutputAuthoring Output { get; }

            internal CameraRigComposer DefaultRig { get; }

            internal CameraRigComposer PresentationRig { get; }

            internal CameraRigBehaviorDefinition PresentationBehavior { get; }

            internal QaCameraPresentationOccurrence Composition { get; }

            internal CameraSubjectAvailabilityContext Availability { get; }

            internal CameraSubjectAvailabilityOwnerId SubjectOwner { get; }

            internal CameraCompositionSubjectSelectionContext Selection { get; }

            internal CameraCompositionMembershipContext RuntimeMembership
            {
                get
                {
                    object runtime =
                        Composition.Runtime;
                    CameraCompositionMembershipContext membership =
                        GetPrivateField<
                            CameraCompositionMembershipContext>(
                            runtime,
                            "_membership");
                    Require(
                        membership != null,
                        "Camera Presentation runtime has no active membership context.");
                    return membership;
                }
            }

            internal CameraSubjectAvailabilityToken AddSubject(
                string id)
            {
                GameObject subject =
                    Root(
                        $"{suffix}-{id}-{Guid.NewGuid():N}");

                CameraSubjectAvailabilityResult result =
                    Availability.TryMakeAvailable(
                        new CameraSubject(
                            new CameraSubjectId(id),
                            subject.transform,
                            id),
                        SubjectOwner);

                Require(
                    result.Succeeded,
                    result.Message);
                return result.Token;
            }

            internal void RemoveSubject(
                CameraSubjectAvailabilityToken token)
            {
                CameraSubjectAvailabilityResult result =
                    Availability.TryMakeUnavailable(
                        token);
                Require(
                    result.Succeeded,
                    result.Message);
            }

            internal CameraSubjectAvailabilityEntry RequireSubject(
                string id)
            {
                CameraSubjectAvailabilitySnapshot snapshot =
                    Availability.CreateSnapshot();
                Require(
                    snapshot.TryGet(
                        new CameraSubjectId(id),
                        out CameraSubjectAvailabilityEntry entry),
                    $"Camera Subject '{id}' is not available.");
                return entry;
            }

            internal CameraCompositionMembershipEntry RequireMember(
                string id)
            {
                CameraCompositionMembershipSnapshot snapshot =
                    RuntimeMembership.Snapshot;
                Require(
                    snapshot.TryGet(
                        new CameraSubjectId(id),
                        out CameraCompositionMembershipEntry entry),
                    $"Camera Presentation membership does not contain Subject '{id}'.");
                return entry;
            }

            internal CameraRigComposer CreateRig(
                CameraRigPresentationIntent intent)
            {
                GameObject root =
                    Root(
                        $"qa-camera-032-rig-{suffix}-{intent}-{Guid.NewGuid():N}");

                var composer =
                    root.AddComponent<CameraRigComposer>();
                CameraRigBehaviorDefinition behavior =
                    CreateBehavior(
                        intent);
                definitions.Add(
                    behavior);

                SetObject(
                    composer,
                    "behaviorDefinition",
                    behavior);

                GameObject cameraRoot =
                    Root(
                        $"qa-camera-032-cinemachine-{suffix}-{intent}-{Guid.NewGuid():N}");
                cameraRoot.transform.SetParent(
                    root.transform,
                    false);

                CinemachineCamera camera =
                    cameraRoot.AddComponent<CinemachineCamera>();
                composer.EditorSetGeneratedReference(
                    camera);

                if (intent ==
                    CameraRigPresentationIntent.Group)
                {
                    GameObject groupRoot =
                        Root(
                            $"qa-camera-032-group-{suffix}-{Guid.NewGuid():N}");
                    groupRoot.transform.SetParent(
                        root.transform,
                        false);

                    CinemachineTargetGroup group =
                        groupRoot.AddComponent<
                            CinemachineTargetGroup>();
                    CinemachineGroupFraming framing =
                        cameraRoot.AddComponent<
                            CinemachineGroupFraming>();

                    SetObject(
                        composer,
                        "frameworkOwnedGroupTargetGroup",
                        group);
                    SetObject(
                        composer,
                        "frameworkOwnedGroupFraming",
                        framing);
                }

                return composer;
            }

            internal CameraRequest CreateRequest(
                string id,
                CameraRigComposer rig,
                int precedence,
                string tieBreaker = null)
            {
                CameraRequestCreateResult result =
                    CameraRequestCreateResult.Create(
                        new CameraRequestId(
                            $"{suffix}-{id}"),
                        Output.Context.OutputId,
                        new CameraRequestOwner(
                            CameraRequestOwnerKind.Activity,
                            new CameraRequestOwnerScopeId(
                                $"{suffix}-{id}-owner")),
                        new CameraRequestLifetime(
                            CameraRequestLifetimeKind.Activity,
                            new CameraRequestLifetimeScopeId(
                                $"{suffix}-{id}-lifetime")),
                        CameraRigReference.FromComposer(
                            rig),
                        new CameraRequestPolicy(
                            precedence,
                            tieBreaker ??
                            $"{suffix}-{id}-tie"),
                        CameraRequestReleaseCondition
                            .ExplicitRelease,
                        nameof(
                            QaCamera032SubjectTransactionCertification),
                        id);

                Require(
                    result.IsSucceeded,
                    result.BlockingIssue);
                return result.Request;
            }

            internal AdditionalPresentation CreateAdditionalPresentation(
                string id,
                CameraRigPresentationIntent intent,
                int precedence)
            {
                CameraRigComposer rig =
                    CreateRig(
                        intent);
                var availability =
                    new CameraSubjectAvailabilityContext(
                        new SubjectAvailabilityContextId(
                            $"qa-camera-032-{suffix}-{id}-availability"));

                QaCameraPresentationOccurrence composition =
                    new QaCameraPresentationOccurrence(
                        $"qa-camera-032-{suffix}-{id}-presentation",
                        OutputDefinition,
                        CameraSharedCompositionSubjectPolicyKind
                            .AllAvailableSubjects,
                        rig,
                        precedence,
                        $"qa-camera-032-session-{suffix}-{id}");
                presentations.Add(
                    composition);
                composition.AttachOutputSession(
                    Output);
                composition.AttachCameraSubjectAvailability(
                    availability);
                composition.SetEnabled(
                    true);
                composition.Reconcile(
                    availability.CreateSnapshot());

                return new AdditionalPresentation(
                    composition,
                    rig,
                    availability);
            }

            private GameObject Root(
                string name)
            {
                var root =
                    new GameObject(name);
                SceneManager.MoveGameObjectToScene(
                    root,
                    scene);
                return root;
            }

            private static CameraRigBehaviorDefinition CreateBehavior(
                CameraRigPresentationIntent intent)
            {
                return intent switch
                {
                    CameraRigPresentationIntent.Fixed =>
                        ScriptableObject.CreateInstance<
                            FixedCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Follow =>
                        ScriptableObject.CreateInstance<
                            FollowCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Mounted =>
                        ScriptableObject.CreateInstance<
                            MountedCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.ThirdPerson =>
                        ScriptableObject.CreateInstance<
                            ThirdPersonCameraRigBehaviorDefinition>(),
                    CameraRigPresentationIntent.Group =>
                        ScriptableObject.CreateInstance<
                            GroupCameraRigBehaviorDefinition>(),
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(intent),
                        intent,
                        null)
                };
            }

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                disposed = true;

                // Release every QA-owned Presentation through the explicit
                // output-session boundary while the transient Output Session is
                // still valid. Edit Mode certification must not depend on
                // MonoBehaviour activation callbacks for transactional cleanup.
                for (int index =
                         presentations.Count - 1;
                     index >= 0;
                     index--)
                {
                    QaCameraPresentationOccurrence presentation =
                        presentations[index];
                    if (presentation == null)
                    {
                        continue;
                    }

                    presentation.DetachOutputSession(
                        "CAMERA-032 Subject Transaction QA cleanup");
                    presentation.SetEnabled(
                        false);
                    presentation.Dispose();
                }

                for (int index =
                         definitions.Count - 1;
                     index >= 0;
                     index--)
                {
                    if (definitions[index] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(
                            definitions[index]);
                    }
                }

                if (scene.IsValid())
                {
                    EditorSceneManager.ClosePreviewScene(
                        scene);
                }
            }
        }

        private readonly struct AdditionalPresentation
        {
            internal AdditionalPresentation(
                QaCameraPresentationOccurrence composition,
                CameraRigComposer rig,
                CameraSubjectAvailabilityContext availability)
            {
                Composition =
                    composition;
                Rig =
                    rig;
                Availability =
                    availability;
            }

            internal QaCameraPresentationOccurrence Composition { get; }

            internal CameraRigComposer Rig { get; }

            internal CameraSubjectAvailabilityContext Availability { get; }
        }

        private sealed class MutatingRejectingPublisher :
            ICameraRequestPublisher
        {
            private readonly Action beforeReject;

            internal MutatingRejectingPublisher(
                CameraRequest request,
                Action beforeReject)
            {
                Request =
                    request;
                this.beforeReject =
                    beforeReject;
            }

            public CameraRequest Request { get; }

            public bool IsPublished =>
                true;

            public CameraRequestPublisherResult Publish()
            {
                return default;
            }

            public CameraRequestPublisherResult Release()
            {
                beforeReject?.Invoke();
                return default;
            }
        }
    }
}

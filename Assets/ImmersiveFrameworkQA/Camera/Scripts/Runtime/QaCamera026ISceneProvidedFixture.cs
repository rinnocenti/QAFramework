using System;
using System.Collections;
using Immersive.Framework.Camera;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaCamera026ISceneProvidedFixture :
        MonoBehaviour,
        ICameraSubjectAvailabilityConsumer
    {
        private const int FrameBudget = 600;
        private const string Prefix = "[CAMERA-026-I]";

        [SerializeField] private SceneProvidedLocalPlayerAuthoring scenePlayer;
        [SerializeField] private PlayerSessionObserver playerSessionObserver;
        [SerializeField] private RouteRequestTrigger backToHubTrigger;

        private ICameraSubjectAvailabilitySource subjectAvailability;

        public static bool Executed { get; private set; }
        public static bool Passed { get; private set; }
        public static string Diagnostic { get; private set; } = string.Empty;

        public void AttachCameraSubjectAvailability(
            ICameraSubjectAvailabilitySource availabilitySource)
        {
            subjectAvailability = availabilitySource ??
                throw new ArgumentNullException(nameof(availabilitySource));
        }

        private void Awake()
        {
            Executed = false;
            Passed = false;
            Diagnostic = string.Empty;
        }

        private IEnumerator Start()
        {
            IEnumerator proof = RunProof();
            while (true)
            {
                object current = null;
                bool hasNext;
                try
                {
                    hasNext = proof.MoveNext();
                    if (hasNext)
                    {
                        current = proof.Current;
                    }
                }
                catch (Exception exception)
                {
                    Executed = true;
                    Passed = false;
                    Diagnostic = exception.Message;
                    Debug.LogError(
                        $"{Prefix} phase='scene-provided' status='Failed' " +
                        $"missing='{Escape(Diagnostic)}'.",
                        this);
                    yield break;
                }

                if (!hasNext)
                {
                    yield break;
                }

                yield return current;
            }
        }

        private IEnumerator RunProof()
        {
            IPlayerSessionScopedAccess access = null;
            yield return WaitFor(
                () =>
                    scenePlayer != null &&
                    scenePlayer.RuntimeReady &&
                    scenePlayer.HasActiveAdmission &&
                    scenePlayer.LastActorAdoptionResult != null &&
                    scenePlayer.LastActorAdoptionResult.Succeeded &&
                    playerSessionObserver != null &&
                    playerSessionObserver.TryGetAccess(out access, out _) &&
                    subjectAvailability != null,
                "scene-provided-adoption-and-injection");

            Require(scenePlayer.TryGetPlayerSlotId(
                    out PlayerSlotId playerSlotId,
                    out string slotIssue),
                "Scene-Provided fixture has no valid Player Slot. " + slotIssue);
            PlayerSessionScopedSlotObservation current =
                RequireCurrentActor(access, playerSlotId, "adopted");
            CameraSubjectAvailabilitySnapshot available =
                subjectAvailability.CreateSnapshot();
            Require(
                available != null && available.Count == 1,
                "Scene-Provided adoption must publish exactly one Camera Subject.");
            CameraSubjectAvailabilityEntry subject = RequireSubjectForHost(
                available,
                scenePlayer.LocalPlayerHost,
                "Scene-Provided");
            Require(
                scenePlayer.HasActiveAdmission &&
                current.Slot.IsJoined &&
                current.CurrentActor.Assignment.IsAssigned &&
                current.CurrentActor.Assignment.AssignmentOrigin ==
                    PlayerSlotAssignmentOrigin.SceneProvided &&
                scenePlayer.LastActorAdoptionResult.Status ==
                    ScenePlayerActorAdoptionStatus.SucceededAdopted &&
                scenePlayer.LastActorAdoptionResult.Token.IsValid &&
                current.CurrentActor.Preparation.IsValid &&
                current.CurrentActor.Preparation.Token ==
                    scenePlayer.LastActorAdoptionResult.Token.PreparationToken &&
                current.CurrentActor.ActorEvidence.PhysicalOwnership ==
                    PlayerActorPhysicalOwnership.FrameworkOwned,
                "Scene-Provided Subject was not observed with the exact canonical adopted Actor occurrence.");
            Transform adoptedObservation = subject.Subject.Observation;
            Require(
                subject.IsValid &&
                adoptedObservation != null &&
                adoptedObservation.IsChildOf(scenePlayer.LocalPlayerHost.ActorMount),
                "Scene-Provided Camera Subject does not expose the expected adopted Actor observation Transform.");

            // RequestRelease retires only Scene-provided contextual admission/correlation.
            // The Session join and the already-published physical observation remain public here.
            SceneLocalPlayerAdmissionRuntimeResult released =
                scenePlayer.RequestRelease(
                    nameof(QaCamera026ISceneProvidedFixture),
                    "camera-026-i-scene-provided-release");
            Require(
                released != null &&
                    released.Status == SceneLocalPlayerAdmissionRuntimeStatus.SucceededReleased,
                released != null
                    ? "scene-provided-admission-release: " + released.ToDiagnosticString()
                    : "scene-provided-admission-release returned no result.");

            yield return WaitFor(
                () => scenePlayer != null && !scenePlayer.HasActiveAdmission,
                "scene-provided-admission-release");

            PlayerSessionScopedSlotObservation postRelease = default;
            yield return WaitFor(
                () =>
                {
                    if (!TryFindSlot(
                            access,
                            playerSlotId,
                            out PlayerSessionScopedSlotObservation candidate) ||
                        !candidate.Slot.IsJoined ||
                        !candidate.Slot.HasSelectedActor ||
                        candidate.Slot.SelectedActorProfile != current.Slot.SelectedActorProfile)
                    {
                        return false;
                    }

                    CameraSubjectAvailabilitySnapshot snapshot =
                        subjectAvailability.CreateSnapshot();
                    if (snapshot == null || snapshot.Count != 1 ||
                        !snapshot.TryGet(
                            subject.Subject.SubjectId,
                            out CameraSubjectAvailabilityEntry preserved) ||
                        !preserved.IsValid ||
                        preserved.Token != subject.Token ||
                        preserved.OwnerId != subject.OwnerId ||
                        !ReferenceEquals(
                            preserved.Subject.Observation,
                            adoptedObservation))
                    {
                        return false;
                    }

                    postRelease = candidate;
                    return true;
                },
                "scene-provided-physical-preservation");

            SessionPlayerLeaveResult leave = access.RequestLeave(
                new SessionPlayerLeaveRequest(
                    playerSlotId,
                    postRelease.Slot.Revision,
                    nameof(QaCamera026ISceneProvidedFixture),
                    "camera-026-i-session-player-leave"));
            Require(
                leave != null &&
                    leave.Status == SessionPlayerLeaveStatus.SucceededLeft &&
                    leave.ProvisioningMode == PlayerHostProvisioningMode.SceneProvided &&
                    leave.ProvisioningReleased &&
                    leave.TerminalCommitted,
                leave != null
                    ? "scene-provided-session-leave: " + leave.ToDiagnosticString()
                    : "scene-provided-session-leave returned no result.");

            yield return WaitFor(
                () =>
                {
                    if (!TryFindSlot(
                            access,
                            playerSlotId,
                            out PlayerSessionScopedSlotObservation candidate) ||
                        candidate.Slot.AllocationState != PlayerSlotAllocationState.Available)
                    {
                        return false;
                    }

                    CameraSubjectAvailabilitySnapshot snapshot =
                        subjectAvailability.CreateSnapshot();
                    return snapshot != null &&
                        snapshot.Count == 0 &&
                        !snapshot.TryGet(subject.Subject.SubjectId, out _);
                },
                "scene-provided-subject-removal-after-leave");

            Executed = true;
            Passed = true;
            Diagnostic =
                $"slot='{playerSlotId.StableText}' adoption='PASS' " +
                "canonicalOccurrence='PASS' subjectPublication='PASS' " +
                "scene-provided-admission-release='PASS' " +
                "scene-provided-physical-preservation='PASS' " +
                "scene-provided-session-leave='PASS' " +
                "scene-provided-subject-removal-after-leave='PASS'.";
            Debug.Log(
                $"{Prefix} phase='scene-provided' status='Passed' {Diagnostic}",
                this);

            yield return WaitFor(
                () => backToHubTrigger != null &&
                    backToHubTrigger.HasRouteRuntimeBinding &&
                    !backToHubTrigger.IsRequestInFlight,
                "scene-provided-back-to-hub-binding");
            backToHubTrigger.RequestRoute();
        }

        private IEnumerator WaitFor(Func<bool> condition, string label)
        {
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                if (condition())
                {
                    yield break;
                }

                yield return null;
            }

            throw new TimeoutException(
                $"Timed out waiting for '{label}'. {DescribePublicObservationState()}");
        }

        private string DescribePublicObservationState()
        {
            string sceneAdmission = scenePlayer != null
                ? $"runtimeReady='{scenePlayer.RuntimeReady}' activeAdmission='{scenePlayer.HasActiveAdmission}' " +
                    $"runtimeResult='{Escape(scenePlayer.LastRuntimeResult?.ToDiagnosticString())}' " +
                    $"actorAdoption='{Escape(scenePlayer.LastActorAdoptionResult?.ToDiagnosticString())}'"
                : "scenePlayer='<missing>'";

            PlayerSessionScopedObservationSnapshot observation =
                playerSessionObserver != null
                    ? playerSessionObserver.CurrentObservation
                    : null;
            string player = observation != null
                ? $"available='{observation.IsAvailable}' sessionRevision='{observation.SessionRevision}' " +
                    $"activityOccurrence='{observation.ActivityOccurrence}' slots='{observation.Slots.Count}' " +
                    $"diagnostic='{Escape(observation.Diagnostic)}'"
                : "observation='<unavailable>'";

            CameraSubjectAvailabilitySnapshot camera =
                subjectAvailability?.CreateSnapshot();
            string subjects = camera != null
                ? $"context='{camera.ContextId}' revision='{camera.Revision}' count='{camera.Count}'"
                : "snapshot='<unavailable>'";

            return $"sceneAdmission=({sceneAdmission}) player=({player}) cameraSubjects=({subjects}).";
        }

        private static PlayerSessionScopedSlotObservation RequireCurrentActor(
            IPlayerSessionScopedAccess access,
            PlayerSlotId playerSlotId,
            string phase)
        {
            Require(access != null,
                $"Player session access is unavailable at '{phase}'.");
            bool observed = access.TryGetObservation(
                out PlayerSessionScopedObservationSnapshot observation);
            Require(
                observed && observation != null && observation.IsAvailable,
                $"Player observation is unavailable at '{phase}'.");
            for (int index = 0; index < observation.Slots.Count; index++)
            {
                PlayerSessionScopedSlotObservation slot = observation.Slots[index];
                if (slot.Slot.PlayerSlotId == playerSlotId)
                {
                    Require(
                        slot.HasCurrentActorEvidence &&
                        slot.CurrentActor.HasCurrentActor &&
                        slot.CurrentActor.Preparation.IsPrepared,
                        $"Scene-Provided Player has no canonical current Actor at '{phase}'.");
                    return slot;
                }
            }

            throw new InvalidOperationException(
                $"Scene-Provided Player Slot '{playerSlotId.StableText}' is absent at '{phase}'.");
        }

        private static bool TryFindSlot(
            IPlayerSessionScopedAccess access,
            PlayerSlotId playerSlotId,
            out PlayerSessionScopedSlotObservation slot)
        {
            slot = default;
            if (access == null ||
                !access.TryGetObservation(out PlayerSessionScopedObservationSnapshot observation) ||
                observation == null ||
                !observation.IsAvailable)
            {
                return false;
            }

            for (int index = 0; index < observation.Slots.Count; index++)
            {
                PlayerSessionScopedSlotObservation candidate = observation.Slots[index];
                if (candidate.Slot.PlayerSlotId == playerSlotId)
                {
                    slot = candidate;
                    return true;
                }
            }

            return false;
        }

        private static CameraSubjectAvailabilityEntry RequireSubjectForHost(
            CameraSubjectAvailabilitySnapshot snapshot,
            LocalPlayerHostAuthoring host,
            string label)
        {
            Require(snapshot != null && host != null && host.ActorMount != null,
                $"{label} Subject resolution requires exact Host evidence.");
            CameraSubjectAvailabilityEntry resolved = default;
            int matches = 0;
            for (int index = 0; index < snapshot.Entries.Count; index++)
            {
                CameraSubjectAvailabilityEntry candidate = snapshot.Entries[index];
                if (candidate.Subject.Observation != null &&
                    candidate.Subject.Observation.IsChildOf(host.ActorMount))
                {
                    resolved = candidate;
                    matches++;
                }
            }

            Require(matches == 1,
                $"Expected exactly one {label} Camera Subject, found '{matches}'.");
            return resolved;
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}

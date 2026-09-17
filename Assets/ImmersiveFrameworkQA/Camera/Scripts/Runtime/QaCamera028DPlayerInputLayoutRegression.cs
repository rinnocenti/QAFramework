using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Camera
{
    public enum QaCamera028DPlayerInputLayoutMode
    {
        CompleteCoverage = 10,
        IncompleteCoverage = 20
    }

    /// <summary>
    /// CAMERA-028-D public integration regression. Player Session commands use the
    /// explicit provisioning product endpoint; Camera evidence uses only authored
    /// fixture references and public snapshots from the exact Outputs.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QaCamera028DPlayerInputLayoutRegression : MonoBehaviour
    {
        private const string Prefix = "[CAMERA-028-D]";
        private const string GameFlowFailureMessage = "Game Flow failed.";
        private const string MissingPlayerCameraSplitScreenError =
            "Player has no camera associated with it. Cannot set up split-screen. Point PlayerInput.camera to camera for player.";
        private const int FrameBudget = 600;
        private const int StableFrameCount = 3;

        private static readonly string[] CompleteCases =
        {
            "fixture-valid",
            "initial-zero-players",
            "inverted-slot-output-policy",
            "semantic-baseline-captured",
            "joining-opened",
            "first-player-joined",
            "first-join-no-null-camera-error",
            "first-player-exact-camera-b",
            "one-player-rect-stable",
            "first-join-output-view-preserved",
            "first-join-subject-authority-preserved",
            "first-join-request-rig-preserved",
            "second-player-joined",
            "second-join-no-null-camera-error",
            "inverted-exact-camera-bindings",
            "two-player-layout-physically-coherent",
            "two-player-rect-stable",
            "second-join-output-view-preserved",
            "second-join-subject-authority-preserved",
            "second-join-request-rig-preserved",
            "second-player-left",
            "departed-player-camera-association-released",
            "remaining-player-exact-camera-b",
            "one-player-restored-rect-stable",
            "leave-output-view-preserved",
            "leave-subject-authority-preserved",
            "leave-request-rig-preserved",
            "players-cleaned",
            "joining-closed"
        };

        private static readonly string[] IncompleteCases =
        {
            "fixture-valid",
            "incomplete-policy-authored",
            "framework-rejected-missing-slot-binding",
            "zero-player-residue"
        };

        [SerializeField] private QaCamera028DPlayerInputLayoutFixture fixture;
        [SerializeField] private QaCamera028DPlayerInputLayoutMode mode;

        private readonly List<string> completed = new();
        private string expectedBootFailure = string.Empty;
        private bool expectedBootFailureObserved;
        private int missingPlayerCameraSplitScreenErrorCount;
        private string lastMissingPlayerCameraSplitScreenError = string.Empty;
        private bool started;

        public static bool Executed { get; private set; }
        public static bool Passed { get; private set; }
        public static string Diagnostic { get; private set; } = string.Empty;
        public static int CompletedCaseCount { get; private set; }
        public static int ExpectedCaseCount { get; private set; }
        public static QaCamera028DPlayerInputLayoutMode ExecutedMode { get; private set; }

        public void Configure(
            QaCamera028DPlayerInputLayoutFixture authoredFixture,
            QaCamera028DPlayerInputLayoutMode authoredMode)
        {
            fixture = authoredFixture;
            mode = authoredMode;
        }

        private void Awake()
        {
            Executed = false;
            Passed = false;
            Diagnostic = string.Empty;
            CompletedCaseCount = 0;
            ExpectedCaseCount = ExpectedCases.Length;
            ExecutedMode = mode;
            completed.Clear();
            expectedBootFailure = string.Empty;
            expectedBootFailureObserved = false;
            missingPlayerCameraSplitScreenErrorCount = 0;
            lastMissingPlayerCameraSplitScreenError = string.Empty;
            started = false;

            if (fixture != null && fixture.SlotP2 != null &&
                fixture.SlotP2.PlayerSlotId.IsValid)
            {
                expectedBootFailure =
                    "PlayerInputManager automatic split-screen requires an explicit Camera Output binding for configured Player Slot '" +
                    fixture.SlotP2.PlayerSlotId.StableText + "'.";
            }
        }

        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        private void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            StartCoroutine(RunGuarded());
        }

        private IEnumerator RunGuarded()
        {
            IEnumerator proof = mode ==
                    QaCamera028DPlayerInputLayoutMode.CompleteCoverage
                ? RunCompleteCoverage()
                : RunIncompleteCoverage();

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
                    Fail(exception.GetBaseException().Message);
                    yield break;
                }

                if (!hasNext)
                {
                    yield break;
                }

                yield return current;
            }
        }

        private IEnumerator RunCompleteCoverage()
        {
            string cleanupDiagnostic = "NotRequired";
            LocalPlayerJoinResult player1 = null;
            LocalPlayerJoinResult player2 = null;
            Keyboard player1Keyboard = null;
            Keyboard player2Keyboard = null;
            bool joiningOpenedByRegression = false;

            try
            {
                RequireFixture(completeCoverage: true);
                Complete("fixture-valid");

                IEnumerator wait = WaitFor(
                    () => fixture.Provisioning.RuntimeReady &&
                        fixture.OutputA.IsInitialized && fixture.OutputB.IsInitialized &&
                        SemanticSurfaceReady(),
                    "CAMERA-028-D runtime surfaces did not become ready.");
                while (wait.MoveNext()) yield return wait.Current;

                Require(fixture.PlayerInputManager.splitScreen,
                    "PlayerInputManager splitScreen was not enabled at runtime.");
                Require(fixture.PlayerInputManager.playerCount == 0 &&
                        PlayerInput.all.Count == 0 &&
                        fixture.Provisioning.RuntimeSnapshot.JoinedCount == 0,
                    "CAMERA-028-D requires a fresh 0 Player baseline.");
                Complete("initial-zero-players");

                RequireExactInvertedPolicy();
                Complete("inverted-slot-output-policy");

                CameraSemanticBaseline baseline = CaptureSemanticBaseline();
                Rect zeroA = fixture.OutputA.UnityCamera.rect;
                Rect zeroB = fixture.OutputB.UnityCamera.rect;
                IEnumerator zeroStable =
                    RequireRectsStable(zeroA, zeroB, "zero-player");
                while (zeroStable.MoveNext()) yield return zeroStable.Current;
                Complete("semantic-baseline-captured");

                player1Keyboard = InputSystem.AddDevice<Keyboard>();
                player2Keyboard = InputSystem.AddDevice<Keyboard>();
                Require(player1Keyboard != null && player2Keyboard != null &&
                        player1Keyboard.added && player2Keyboard.added &&
                        player1Keyboard.deviceId != player2Keyboard.deviceId,
                    "CAMERA-028-D could not create two distinct QA-owned input devices.");

                if (!fixture.Provisioning.RuntimeSnapshot.JoiningOpen)
                {
                    PlayerParticipationOperationResult opened =
                        fixture.Provisioning.OpenJoining(
                            nameof(QaCamera028DPlayerInputLayoutRegression),
                            "camera-028-d-open-joining");
                    Require(opened != null && opened.Succeeded &&
                            opened.Snapshot != null && opened.Snapshot.JoiningOpen,
                        opened != null ? opened.ToDiagnosticString() :
                            "OpenJoining returned no result.");
                    joiningOpenedByRegression = true;
                }
                Complete("joining-opened");

                player1 = fixture.Provisioning.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaCamera028DPlayerInputLayoutRegression),
                        "camera-028-d-player-1",
                        player1Keyboard));
                Require(player1 != null && player1.Succeeded &&
                        player1.Slot.PlayerSlotId == fixture.SlotP1.PlayerSlotId &&
                        player1.PlayerInput != null,
                    player1 != null ? player1.ToDiagnosticString() :
                        "First Join returned no result.");
                Complete("first-player-joined");
                RequireNoMissingPlayerCameraSplitScreenError("first-join");
                Complete("first-join-no-null-camera-error");

                wait = WaitFor(
                    () => fixture.PlayerInputManager.splitScreen &&
                        fixture.PlayerInputManager.playerCount == 1 &&
                        PlayerInput.all.Count == 1 &&
                        ReferenceEquals(
                            player1.PlayerInput.camera,
                            fixture.OutputB.UnityCamera),
                    "First Player did not stabilize on exact Output B Unity Camera.");
                while (wait.MoveNext()) yield return wait.Current;
                Complete("first-player-exact-camera-b");

                Rect oneA = fixture.OutputA.UnityCamera.rect;
                Rect oneB = fixture.OutputB.UnityCamera.rect;
                wait = RequireRectsStable(oneA, oneB, "one-player");
                while (wait.MoveNext()) yield return wait.Current;
                Complete("one-player-rect-stable");
                RequireOutputAndViewPreserved(baseline, "first-join");
                Complete("first-join-output-view-preserved");
                RequireSubjectAuthorityPreserved(baseline, "first-join");
                Complete("first-join-subject-authority-preserved");
                RequireRequestAndRigPreserved(baseline, "first-join");
                Complete("first-join-request-rig-preserved");

                player2 = fixture.Provisioning.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaCamera028DPlayerInputLayoutRegression),
                        "camera-028-d-player-2",
                        player2Keyboard));
                Require(player2 != null && player2.Succeeded &&
                        player2.Slot.PlayerSlotId == fixture.SlotP2.PlayerSlotId &&
                        player2.PlayerInput != null,
                    player2 != null ? player2.ToDiagnosticString() :
                        "Second Join returned no result.");
                Complete("second-player-joined");
                RequireNoMissingPlayerCameraSplitScreenError("second-join");
                Complete("second-join-no-null-camera-error");

                wait = WaitFor(
                    () => fixture.PlayerInputManager.splitScreen &&
                        fixture.PlayerInputManager.playerCount == 2 &&
                        PlayerInput.all.Count == 2 &&
                        ReferenceEquals(
                            player1.PlayerInput.camera,
                            fixture.OutputB.UnityCamera) &&
                        ReferenceEquals(
                            player2.PlayerInput.camera,
                            fixture.OutputA.UnityCamera),
                    "Two Players did not stabilize on the exact inverted Camera mapping.");
                while (wait.MoveNext()) yield return wait.Current;
                Complete("inverted-exact-camera-bindings");

                Rect twoA = fixture.OutputA.UnityCamera.rect;
                Rect twoB = fixture.OutputB.UnityCamera.rect;
                RequireCoherentTwoPlayerLayout(twoA, twoB);
                Complete("two-player-layout-physically-coherent");

                wait = RequireRectsStable(twoA, twoB, "two-player");
                while (wait.MoveNext()) yield return wait.Current;
                Complete("two-player-rect-stable");
                RequireOutputAndViewPreserved(baseline, "second-join");
                Complete("second-join-output-view-preserved");
                RequireSubjectAuthorityPreserved(baseline, "second-join");
                Complete("second-join-subject-authority-preserved");
                RequireRequestAndRigPreserved(baseline, "second-join");
                Complete("second-join-request-rig-preserved");

                PlayerSlotRuntimeSnapshot player2Occurrence =
                    RequireSlot(fixture.Provisioning.RuntimeSnapshot,
                        fixture.SlotP2.PlayerSlotId);
                PlayerInput departedPlayerInput = player2.PlayerInput;
                UnityEngine.Camera departedPlayerCamera =
                    departedPlayerInput != null
                        ? departedPlayerInput.camera
                        : null;
                SessionPlayerLeaveResult leave2 =
                    fixture.Provisioning.RequestLeave(
                        new SessionPlayerLeaveRequest(
                            player2Occurrence.PlayerSlotId,
                            player2Occurrence.Revision,
                            nameof(QaCamera028DPlayerInputLayoutRegression),
                            "camera-028-d-player-2-leave"));
                Require(leave2 != null && leave2.Succeeded,
                    leave2 != null ? leave2.ToDiagnosticString() :
                        "Second Player Leave returned no result.");
                player2 = null;
                Complete("second-player-left");

                wait = WaitFor(
                    () => departedPlayerInput == null ||
                        departedPlayerInput.camera == null,
                    "Departed Player retained its PlayerInput.camera association after Leave.");
                while (wait.MoveNext()) yield return wait.Current;
                Require(ReferenceEquals(
                            departedPlayerCamera,
                            fixture.OutputA.UnityCamera),
                    "Departed Player did not release the exact Output A Camera association established before Leave.");
                Complete("departed-player-camera-association-released");

                wait = WaitFor(
                    () => fixture.PlayerInputManager.splitScreen &&
                        fixture.PlayerInputManager.playerCount == 1 &&
                        PlayerInput.all.Count == 1 &&
                        ReferenceEquals(
                            player1.PlayerInput.camera,
                            fixture.OutputB.UnityCamera),
                    "Remaining Player did not preserve exact Output B Camera after Leave.");
                while (wait.MoveNext()) yield return wait.Current;
                Complete("remaining-player-exact-camera-b");

                Rect restoredOneA = fixture.OutputA.UnityCamera.rect;
                Rect restoredOneB = fixture.OutputB.UnityCamera.rect;
                wait = RequireRectsStable(
                    restoredOneA,
                    restoredOneB,
                    "one-player-after-leave");
                while (wait.MoveNext()) yield return wait.Current;
                Complete("one-player-restored-rect-stable");
                RequireOutputAndViewPreserved(baseline, "second-player-leave");
                Complete("leave-output-view-preserved");
                RequireSubjectAuthorityPreserved(baseline, "second-player-leave");
                Complete("leave-subject-authority-preserved");
                RequireRequestAndRigPreserved(baseline, "second-player-leave");
                Complete("leave-request-rig-preserved");

                LeaveIfJoined(ref player1, "camera-028-d-player-1-cleanup");
                wait = WaitFor(
                    () => fixture.PlayerInputManager.splitScreen &&
                        fixture.PlayerInputManager.playerCount == 0 &&
                        PlayerInput.all.Count == 0 &&
                        fixture.Provisioning.RuntimeSnapshot.JoinedCount == 0,
                    "CAMERA-028-D cleanup left residual Player state.");
                while (wait.MoveNext()) yield return wait.Current;
                Complete("players-cleaned");

                if (joiningOpenedByRegression)
                {
                    PlayerParticipationOperationResult closed =
                        fixture.Provisioning.CloseJoining(
                            nameof(QaCamera028DPlayerInputLayoutRegression),
                            "camera-028-d-close-joining");
                    Require(closed != null && closed.Succeeded &&
                            closed.Snapshot != null && !closed.Snapshot.JoiningOpen,
                        closed != null ? closed.ToDiagnosticString() :
                            "CloseJoining returned no result.");
                    joiningOpenedByRegression = false;
                }
                Complete("joining-closed");
                cleanupDiagnostic = "PlayersReleased,JoiningRestored,DevicesRemoved";
                CompletePassed(
                    $"slotP1='{fixture.SlotP1.PlayerSlotId.StableText}'->" +
                    $"outputB='{fixture.OutputB.OutputIdText}' cameraB='{Describe(fixture.OutputB.UnityCamera)}' " +
                    $"slotP2='{fixture.SlotP2.PlayerSlotId.StableText}'->" +
                    $"outputA='{fixture.OutputA.OutputIdText}' cameraA='{Describe(fixture.OutputA.UnityCamera)}' " +
                    $"rect0='A:{Describe(zeroA)}|B:{Describe(zeroB)}' " +
                    $"rect1='A:{Describe(oneA)}|B:{Describe(oneB)}' " +
                    $"rect2='A:{Describe(twoA)}|B:{Describe(twoB)}' " +
                    $"rectAfterLeave='A:{Describe(restoredOneA)}|B:{Describe(restoredOneB)}' " +
                    "semantics='OutputIdentity,ViewAssociation,SubjectAuthority,RequestWinner,RigSelection'",
                    cleanupDiagnostic);
            }
            finally
            {
                TryFinallyCleanup(
                    ref player2,
                    ref player1,
                    joiningOpenedByRegression,
                    player1Keyboard,
                    player2Keyboard);
            }
        }

        private void TryFinallyCleanup(
            ref LocalPlayerJoinResult player2,
            ref LocalPlayerJoinResult player1,
            bool joiningOpenedByRegression,
            Keyboard player1Keyboard,
            Keyboard player2Keyboard)
        {
            TryCleanupPlayer(ref player2, "camera-028-d-player-2-finally");
            TryCleanupPlayer(ref player1, "camera-028-d-player-1-finally");
            if (joiningOpenedByRegression &&
                fixture != null && fixture.Provisioning != null &&
                fixture.Provisioning.RuntimeReady)
            {
                try
                {
                    fixture.Provisioning.CloseJoining(
                        nameof(QaCamera028DPlayerInputLayoutRegression),
                        "camera-028-d-finally-close-joining");
                }
                catch (Exception cleanupException)
                {
                    LogCleanupFailure("Joining", cleanupException);
                }
            }

            TryRemoveDevice(player1Keyboard, "Player1Device");
            TryRemoveDevice(player2Keyboard, "Player2Device");
        }

        private void TryCleanupPlayer(
            ref LocalPlayerJoinResult join,
            string reason)
        {
            try
            {
                LeaveIfJoined(ref join, reason);
            }
            catch (Exception cleanupException)
            {
                LogCleanupFailure(reason, cleanupException);
            }
        }

        private void TryRemoveDevice(InputDevice device, string stage)
        {
            try
            {
                if (device != null && device.added)
                {
                    InputSystem.RemoveDevice(device);
                }
            }
            catch (Exception cleanupException)
            {
                LogCleanupFailure(stage, cleanupException);
            }
        }

        private void LogCleanupFailure(string stage, Exception exception) =>
            Debug.LogError(
                $"{Prefix} cleanup-stage='{Escape(stage)}' " +
                $"diagnostic='{Escape(exception.GetBaseException().Message)}'.",
                this);

        private IEnumerator RunIncompleteCoverage()
        {
            RequireFixture(completeCoverage: false);
            Complete("fixture-valid");
            RequireExactInvertedPolicy();
            Require(!fixture.CompleteSlotCoverage &&
                    fixture.OutputPolicy.Bindings.Count == 1,
                "CAMERA-028-D negative phase requires exact Slot P2 coverage omission.");
            Complete("incomplete-policy-authored");

            IEnumerator wait = WaitFor(
                () => expectedBootFailureObserved,
                "Framework did not emit the exact missing Player Slot Camera Output rejection.");
            while (wait.MoveNext()) yield return wait.Current;
            Complete("framework-rejected-missing-slot-binding");

            Require(fixture.PlayerInputManager.playerCount == 0 &&
                    PlayerInput.all.Count == 0,
                "Incomplete coverage rejection left PlayerInput residue.");
            Complete("zero-player-residue");
            CompletePassed(
                $"incompleteSlot='{fixture.SlotP2.PlayerSlotId.StableText}' " +
                $"expectedFailure='{expectedBootFailure}' zeroPlayerResidue='True'",
                "NoPlayerResidue");
        }

        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            _ = stackTrace;
            if (string.IsNullOrEmpty(condition))
            {
                return;
            }

            if (mode == QaCamera028DPlayerInputLayoutMode.CompleteCoverage &&
                IsMissingPlayerCameraSplitScreenError(condition, type))
            {
                missingPlayerCameraSplitScreenErrorCount++;
                lastMissingPlayerCameraSplitScreenError = condition;
                return;
            }

            if (mode != QaCamera028DPlayerInputLayoutMode.IncompleteCoverage ||
                string.IsNullOrEmpty(expectedBootFailure) ||
                expectedBootFailureObserved)
            {
                return;
            }

            if (IsExpectedIncompleteCoverageBootFailure(
                    condition,
                    type,
                    expectedBootFailure))
            {
                expectedBootFailureObserved = true;
            }
        }

        internal static bool IsMissingPlayerCameraSplitScreenError(
            string condition,
            LogType type) =>
            type == LogType.Error &&
            string.Equals(
                condition,
                MissingPlayerCameraSplitScreenError,
                StringComparison.Ordinal);

        internal static bool IsExpectedIncompleteCoverageBootFailure(
            string condition,
            LogType type,
            string expectedReason)
        {
            if (type != LogType.Error ||
                string.IsNullOrEmpty(condition) ||
                string.IsNullOrEmpty(expectedReason))
            {
                return false;
            }

            string expectedReasonField =
                $"reason='{EscapeStructuredLogFieldValue(expectedReason)}'";
            return condition.IndexOf(
                       GameFlowFailureMessage,
                       StringComparison.Ordinal) >= 0 &&
                   condition.IndexOf(
                       expectedReasonField,
                       StringComparison.Ordinal) >= 0;
        }

        private static string EscapeStructuredLogFieldValue(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");

        private IEnumerator WaitFor(Func<bool> predicate, string failure)
        {
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                if (predicate())
                {
                    yield break;
                }
                yield return null;
            }

            throw new TimeoutException(
                $"{failure} frameBudget='{FrameBudget}'.");
        }

        private IEnumerator RequireRectsStable(Rect expectedA, Rect expectedB, string stage)
        {
            for (int frame = 0; frame < StableFrameCount; frame++)
            {
                yield return null;
                Require(
                    RectMatches(fixture.OutputA.UnityCamera.rect, expectedA) &&
                    RectMatches(fixture.OutputB.UnityCamera.rect, expectedB),
                    $"Camera.rect changed without join/leave at '{stage}' frame='{frame + 1}'. " +
                    $"expectedA='{Describe(expectedA)}' actualA='{Describe(fixture.OutputA.UnityCamera.rect)}' " +
                    $"expectedB='{Describe(expectedB)}' actualB='{Describe(fixture.OutputB.UnityCamera.rect)}'.");
            }
        }

        private void RequireNoMissingPlayerCameraSplitScreenError(string stage)
        {
            Require(missingPlayerCameraSplitScreenErrorCount == 0,
                $"PlayerInputManager observed PlayerInput.camera null during '{stage}'. " +
                $"count='{missingPlayerCameraSplitScreenErrorCount}' " +
                $"error='{Escape(lastMissingPlayerCameraSplitScreenError)}'.");
        }

        private void RequireCoherentTwoPlayerLayout(Rect outputA, Rect outputB)
        {
            Rect available = fixture.PlayerInputManager.splitScreenArea;
            Require(IsPositiveRectInside(outputA, available) &&
                    IsPositiveRectInside(outputB, available),
                "Two-Player layout must contain two positive Camera.rect values inside " +
                $"PlayerInputManager.splitScreenArea. area='{Describe(available)}' " +
                $"A='{Describe(outputA)}' B='{Describe(outputB)}'.");

            const float tolerance = 0.000001f;
            float overlapWidth = Mathf.Min(outputA.xMax, outputB.xMax) -
                Mathf.Max(outputA.xMin, outputB.xMin);
            float overlapHeight = Mathf.Min(outputA.yMax, outputB.yMax) -
                Mathf.Max(outputA.yMin, outputB.yMin);
            Require(overlapWidth <= tolerance || overlapHeight <= tolerance,
                "Two active Player cameras overlap physically. A fullscreen Camera plus " +
                "a subdivided Camera is not a coherent PlayerInputManager layout. " +
                $"A='{Describe(outputA)}' B='{Describe(outputB)}' " +
                $"overlapWidth='{overlapWidth:F6}' overlapHeight='{overlapHeight:F6}'.");
        }

        private static bool IsPositiveRectInside(Rect candidate, Rect container)
        {
            const float tolerance = 0.000001f;
            return candidate.width > tolerance &&
                candidate.height > tolerance &&
                candidate.xMin >= container.xMin - tolerance &&
                candidate.yMin >= container.yMin - tolerance &&
                candidate.xMax <= container.xMax + tolerance &&
                candidate.yMax <= container.yMax + tolerance;
        }

        private void RequireFixture(bool completeCoverage)
        {
            Require(fixture != null,
                "CAMERA-028-D regression requires its authored fixture.");
            Require(fixture.TryValidateAuthoredSurface(out string issue), issue);
            Require(fixture.CompleteSlotCoverage == completeCoverage,
                "CAMERA-028-D regression mode does not match authored policy coverage.");
        }

        private void RequireExactInvertedPolicy()
        {
            Require(HasBinding(fixture.SlotP1, fixture.OutputB) &&
                    !HasBinding(fixture.SlotP1, fixture.OutputA),
                "Slot P1 must resolve only exact Output B.");
            Require(
                fixture.CompleteSlotCoverage
                    ? HasBinding(fixture.SlotP2, fixture.OutputA) &&
                      !HasBinding(fixture.SlotP2, fixture.OutputB)
                    : !HasBinding(fixture.SlotP2, fixture.OutputA) &&
                      !HasBinding(fixture.SlotP2, fixture.OutputB),
                fixture.CompleteSlotCoverage
                    ? "Slot P2 must resolve only exact Output A."
                    : "Incomplete coverage must omit Slot P2 without fallback.");
        }

        private bool HasBinding(
            PlayerSlotProfile slot,
            CameraOutputAuthoring output)
        {
            for (int index = 0; index < fixture.OutputPolicy.Bindings.Count; index++)
            {
                PlayerCameraOutputBindingAuthoring binding =
                    fixture.OutputPolicy.Bindings[index];
                if (binding != null &&
                    ReferenceEquals(binding.PlayerSlotProfile, slot) &&
                    ReferenceEquals(
                        binding.OutputDefinition,
                        output.OutputDefinition))
                {
                    return true;
                }
            }
            return false;
        }

        private CameraSemanticBaseline CaptureSemanticBaseline()
        {
            CameraSharedComposition shared =
                fixture.OutputA.GetComponent<CameraSharedComposition>();
            Require(shared != null &&
                    ReferenceEquals(shared.OutputDefinition,
                        fixture.OutputA.OutputDefinition),
                "CAMERA-028-D requires exact Output A shared View association.");
            CameraViewOutputPolicyAuthoring viewPolicy = fixture.ViewOutputPolicy;
            Require(viewPolicy.Bindings.Count == 1 &&
                    ReferenceEquals(
                        viewPolicy.Bindings[0].OutputDefinition,
                        fixture.OutputB.OutputDefinition),
                "CAMERA-028-D requires exact Output B View association.");

            return new CameraSemanticBaseline(
                CaptureOutput(fixture.OutputA),
                CaptureOutput(fixture.OutputB),
                shared.ViewIdText,
                shared.OutputIdText,
                shared.Snapshot.AvailabilityContextId,
                shared.Snapshot.AssignmentContextId,
                viewPolicy.Bindings[0].ViewIdText,
                viewPolicy.Bindings[0].OutputIdText);
        }

        private bool SemanticSurfaceReady()
        {
            CameraSharedComposition shared =
                fixture.OutputA.GetComponent<CameraSharedComposition>();
            if (shared == null ||
                !ReferenceEquals(shared.Output, fixture.OutputA) ||
                !shared.Snapshot.AvailabilityContextId.IsValid ||
                !shared.Snapshot.AssignmentContextId.IsValid)
            {
                return false;
            }

            return fixture.ViewOutputPolicy != null;
        }

        private void RequireOutputAndViewPreserved(
            CameraSemanticBaseline baseline,
            string stage)
        {
            OutputSemanticSnapshot currentA = CaptureOutput(fixture.OutputA);
            OutputSemanticSnapshot currentB = CaptureOutput(fixture.OutputB);
            Require(baseline.OutputA.IdentityEquals(currentA) &&
                    baseline.OutputB.IdentityEquals(currentB),
                $"PlayerInput layout changed Camera Output identity or exact Unity Camera at '{stage}'.");

            CameraSharedComposition shared =
                fixture.OutputA.GetComponent<CameraSharedComposition>();
            CameraViewOutputPolicyAuthoring viewPolicy = fixture.ViewOutputPolicy;
            Require(shared != null &&
                    shared.ViewIdText == baseline.ViewAId &&
                    shared.OutputIdText == baseline.ViewAOutputId &&
                    viewPolicy.Bindings.Count == 1 &&
                    viewPolicy.Bindings[0].ViewIdText == baseline.ViewBId &&
                    viewPolicy.Bindings[0].OutputIdText == baseline.ViewBOutputId,
                $"PlayerInput layout changed View/Output association at '{stage}'.");
        }

        private void RequireSubjectAuthorityPreserved(
            CameraSemanticBaseline baseline,
            string stage)
        {
            CameraSharedComposition shared =
                fixture.OutputA.GetComponent<CameraSharedComposition>();
            Require(shared != null &&
                    shared.Snapshot.AvailabilityContextId ==
                        baseline.SubjectAvailabilityContextId &&
                    shared.Snapshot.AssignmentContextId ==
                        baseline.SubjectAssignmentContextId,
                $"PlayerInput layout changed Subject availability or assignment authority at '{stage}'.");
        }

        private void RequireRequestAndRigPreserved(
            CameraSemanticBaseline baseline,
            string stage)
        {
            OutputSemanticSnapshot currentA = CaptureOutput(fixture.OutputA);
            OutputSemanticSnapshot currentB = CaptureOutput(fixture.OutputB);
            Require(baseline.OutputA.RequestAndRigEquals(currentA) &&
                    baseline.OutputB.RequestAndRigEquals(currentB),
                $"PlayerInput layout changed admitted Camera requests, request winner or Rig selection at '{stage}'.");
        }

        private static OutputSemanticSnapshot CaptureOutput(
            CameraOutputAuthoring output)
        {
            Require(output != null && output.Context != null &&
                    output.Applicator != null && output.UnityCamera != null,
                "Camera semantic snapshot requires initialized explicit Output evidence.");
            CameraOutputContextSnapshot context = output.Context.CaptureSnapshot();
            return new OutputSemanticSnapshot(
                output.OutputId,
                output.UnityCamera,
                context.AdmittedRequestCount,
                context.HasWinner,
                context.HasWinner ? context.Winner.RequestId : default,
                context.HasWinner ? context.Winner.Rig.Composer : null,
                output.Applicator.HasAppliedRequest,
                output.Applicator.HasAppliedDefault,
                output.Applicator.AppliedRequestId,
                output.Applicator.AppliedCamera);
        }

        private void LeaveIfJoined(ref LocalPlayerJoinResult join, string reason)
        {
            if (join == null || fixture == null || fixture.Provisioning == null ||
                !fixture.Provisioning.RuntimeReady)
            {
                join = null;
                return;
            }

            PlayerParticipationSnapshot snapshot =
                fixture.Provisioning.RuntimeSnapshot;
            if (TryFindSlot(snapshot, join.Slot.PlayerSlotId,
                    out PlayerSlotRuntimeSnapshot slot) && slot.IsJoined)
            {
                fixture.Provisioning.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        slot.PlayerSlotId,
                        slot.Revision,
                        nameof(QaCamera028DPlayerInputLayoutRegression),
                        reason));
            }
            join = null;
        }

        private static PlayerSlotRuntimeSnapshot RequireSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId slotId)
        {
            Require(TryFindSlot(snapshot, slotId, out PlayerSlotRuntimeSnapshot slot),
                $"Player Slot '{slotId.StableText}' is absent from runtime snapshot.");
            return slot;
        }

        private static bool TryFindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId slotId,
            out PlayerSlotRuntimeSnapshot slot)
        {
            slot = default;
            if (snapshot == null || !slotId.IsValid)
            {
                return false;
            }
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId == slotId)
                {
                    slot = snapshot.Slots[index];
                    return true;
                }
            }
            return false;
        }

        private void Complete(string caseName)
        {
            string[] expected = ExpectedCases;
            Require(completed.Count < expected.Length &&
                    string.Equals(expected[completed.Count], caseName,
                        StringComparison.Ordinal),
                $"CAMERA-028-D case order mismatch. expected='{NextExpected()}' actual='{caseName}'.");
            completed.Add(caseName);
            CompletedCaseCount = completed.Count;
        }

        private void CompletePassed(string verdict, string cleanup)
        {
            Require(completed.Count == ExpectedCases.Length,
                $"CAMERA-028-D incomplete. next='{NextExpected()}'.");
            Executed = true;
            Passed = true;
            Diagnostic = verdict;
            Debug.Log(
                $"{Prefix} status='Passed' phase='{mode}' verdict='{Escape(verdict)}' " +
                $"cases='{completed.Count}/{ExpectedCases.Length}' next='<none>' " +
                $"completed='{string.Join(",", completed)}' missing='' " +
                $"execution='' unwind='' cleanup='{cleanup}'.",
                this);
        }

        private void Fail(string reason)
        {
            Executed = true;
            Passed = false;
            Diagnostic = reason ?? string.Empty;
            Debug.LogError(
                $"{Prefix} status='Failed' phase='{mode}' verdict='CAMERA_028_D_FAIL' " +
                $"cases='{completed.Count}/{ExpectedCases.Length}' next='{NextExpected()}' " +
                $"completed='{string.Join(",", completed)}' " +
                $"missing='{Escape(reason)}' execution='{Escape(reason)}' " +
                "unwind='SeeFinallyCleanup' cleanup='Attempted'.",
                this);
        }

        private string[] ExpectedCases =>
            mode == QaCamera028DPlayerInputLayoutMode.CompleteCoverage
                ? CompleteCases
                : IncompleteCases;

        private string NextExpected() =>
            completed.Count < ExpectedCases.Length
                ? ExpectedCases[completed.Count]
                : "<none>";

        private static bool RectMatches(Rect left, Rect right)
        {
            const float tolerance = 0.000001f;
            return Mathf.Abs(left.x - right.x) <= tolerance &&
                Mathf.Abs(left.y - right.y) <= tolerance &&
                Mathf.Abs(left.width - right.width) <= tolerance &&
                Mathf.Abs(left.height - right.height) <= tolerance;
        }

        private static string Describe(Rect value) =>
            $"x={value.x:F6},y={value.y:F6},w={value.width:F6},h={value.height:F6}";

        private static string Describe(UnityEngine.Camera value) =>
            value != null
                ? $"{value.name}#{value.GetEntityId()}"
                : "<null>";

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

        private readonly struct OutputSemanticSnapshot
        {
            internal OutputSemanticSnapshot(
                CameraOutputId outputId,
                UnityEngine.Camera unityCamera,
                int admittedRequestCount,
                bool hasWinner,
                CameraRequestId winnerRequestId,
                CameraRigComposer winnerRig,
                bool hasAppliedRequest,
                bool hasAppliedDefault,
                CameraRequestId appliedRequestId,
                Unity.Cinemachine.CinemachineCamera appliedCamera)
            {
                OutputId = outputId;
                UnityCamera = unityCamera;
                AdmittedRequestCount = admittedRequestCount;
                HasWinner = hasWinner;
                WinnerRequestId = winnerRequestId;
                WinnerRig = winnerRig;
                HasAppliedRequest = hasAppliedRequest;
                HasAppliedDefault = hasAppliedDefault;
                AppliedRequestId = appliedRequestId;
                AppliedCamera = appliedCamera;
            }

            internal CameraOutputId OutputId { get; }
            internal UnityEngine.Camera UnityCamera { get; }
            internal int AdmittedRequestCount { get; }
            internal bool HasWinner { get; }
            internal CameraRequestId WinnerRequestId { get; }
            internal CameraRigComposer WinnerRig { get; }
            internal bool HasAppliedRequest { get; }
            internal bool HasAppliedDefault { get; }
            internal CameraRequestId AppliedRequestId { get; }
            internal Unity.Cinemachine.CinemachineCamera AppliedCamera { get; }

            internal bool IdentityEquals(OutputSemanticSnapshot other) =>
                OutputId == other.OutputId &&
                ReferenceEquals(UnityCamera, other.UnityCamera);

            internal bool RequestAndRigEquals(OutputSemanticSnapshot other) =>
                AdmittedRequestCount == other.AdmittedRequestCount &&
                HasWinner == other.HasWinner &&
                WinnerRequestId == other.WinnerRequestId &&
                ReferenceEquals(WinnerRig, other.WinnerRig) &&
                HasAppliedRequest == other.HasAppliedRequest &&
                HasAppliedDefault == other.HasAppliedDefault &&
                AppliedRequestId == other.AppliedRequestId &&
                ReferenceEquals(AppliedCamera, other.AppliedCamera);
        }

        private readonly struct CameraSemanticBaseline
        {
            internal CameraSemanticBaseline(
                OutputSemanticSnapshot outputA,
                OutputSemanticSnapshot outputB,
                string viewAId,
                string viewAOutputId,
                SubjectAvailabilityContextId subjectAvailabilityContextId,
                ViewAssignmentContextId subjectAssignmentContextId,
                string viewBId,
                string viewBOutputId)
            {
                OutputA = outputA;
                OutputB = outputB;
                ViewAId = viewAId;
                ViewAOutputId = viewAOutputId;
                SubjectAvailabilityContextId = subjectAvailabilityContextId;
                SubjectAssignmentContextId = subjectAssignmentContextId;
                ViewBId = viewBId;
                ViewBOutputId = viewBOutputId;
            }

            internal OutputSemanticSnapshot OutputA { get; }
            internal OutputSemanticSnapshot OutputB { get; }
            internal string ViewAId { get; }
            internal string ViewAOutputId { get; }
            internal SubjectAvailabilityContextId SubjectAvailabilityContextId { get; }
            internal ViewAssignmentContextId SubjectAssignmentContextId { get; }
            internal string ViewBId { get; }
            internal string ViewBOutputId { get; }
        }
    }
}

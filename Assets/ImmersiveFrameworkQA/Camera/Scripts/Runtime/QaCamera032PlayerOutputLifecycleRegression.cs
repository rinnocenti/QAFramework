using System;
using System.Collections;
using System.Collections.Generic;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Camera
{
    [DisallowMultipleComponent]
    public sealed class QaCamera032PlayerOutputLifecycleRegression : MonoBehaviour
    {
        private const string Prefix = "[CAMERA-032-PLAYER-OUTPUT-LIFECYCLE]";
        private const int FrameBudget = 900;

        private static readonly string[] ExpectedCases =
        {
            "runtime-ready",
            "zero-player-default-continuity",
            "exclusive-cinemachine-channels",
            "p1-joined-exact-output",
            "p1-third-person-full-screen",
            "p2-joined-exact-output",
            "two-player-split-coherent",
            "two-player-third-person-isolated",
            "p2-left-output-dormant",
            "p1-restored-full-screen",
            "leave-recomposition-no-inputsystem-camera-error",
            "p1-third-person-survives-leave",
            "p2-rejoined-fresh-host",
            "split-restored-after-rejoin",
            "terminal-default-continuity"
        };

        [SerializeField] private LocalPlayerProvisioningAuthoring provisioning;
        [SerializeField] private PlayerInputManager playerInputManager;
        [SerializeField] private PlayerSlotProfile slotP1;
        [SerializeField] private PlayerSlotProfile slotP2;
        [SerializeField] private CameraOutputDefinition outputDefinitionP1;
        [SerializeField] private CameraOutputDefinition outputDefinitionP2;

        private readonly List<string> completed = new();
        private bool started;
        private string inputSystemCameraError = string.Empty;

        public static bool Executed { get; private set; }
        public static bool Passed { get; private set; }
        public static int CompletedCaseCount { get; private set; }
        public static int ExpectedCaseCount => ExpectedCases.Length;
        public static string Diagnostic { get; private set; } = string.Empty;

        public void Configure(
            LocalPlayerProvisioningAuthoring authoredProvisioning,
            PlayerInputManager authoredPlayerInputManager,
            PlayerSlotProfile authoredSlotP1,
            PlayerSlotProfile authoredSlotP2,
            CameraOutputDefinition authoredOutputDefinitionP1,
            CameraOutputDefinition authoredOutputDefinitionP2)
        {
            provisioning = authoredProvisioning;
            playerInputManager = authoredPlayerInputManager;
            slotP1 = authoredSlotP1;
            slotP2 = authoredSlotP2;
            outputDefinitionP1 = authoredOutputDefinitionP1;
            outputDefinitionP2 = authoredOutputDefinitionP2;
        }

        private void Awake()
        {
            Executed = false;
            Passed = false;
            CompletedCaseCount = 0;
            Diagnostic = string.Empty;
            completed.Clear();
            started = false;
            inputSystemCameraError = string.Empty;
            Application.logMessageReceived += OnLogMessageReceived;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void Start()
        {
            if (started) return;
            started = true;
            StartCoroutine(RunGuarded());
        }

        private IEnumerator RunGuarded()
        {
            IEnumerator proof = Run();
            while (true)
            {
                object current = null;
                bool hasNext;
                try
                {
                    hasNext = proof.MoveNext();
                    if (hasNext) current = proof.Current;
                }
                catch (Exception exception)
                {
                    Fail(exception.GetBaseException().Message);
                    yield break;
                }

                if (!hasNext) yield break;
                yield return current;
            }
        }

        private IEnumerator Run()
        {
            Keyboard p1Device = null;
            Keyboard p2Device = null;
            LocalPlayerJoinResult p1 = null;
            LocalPlayerJoinResult p2 = null;

            try
            {
                RequireFixture();

                CameraOutputAuthoring outputP1 = null;
                CameraOutputAuthoring outputP2 = null;
                IEnumerator wait = WaitFor(
                    () =>
                    {
                        if (provisioning == null || !provisioning.RuntimeReady)
                            return false;

                        return TryResolveExactOutput(outputDefinitionP1, out outputP1) &&
                            TryResolveExactOutput(outputDefinitionP2, out outputP2) &&
                            outputP1.IsInitialized &&
                            outputP2.IsInitialized;
                    },
                    "ADR-032 runtime/output surfaces did not become ready.");
                while (wait.MoveNext()) yield return wait.Current;

                Complete("runtime-ready");

                Require(
                    playerInputManager.playerCount == 0 &&
                    PlayerInput.all.Count == 0 &&
                    provisioning.RuntimeSnapshot.JoinedCount == 0,
                    "ADR-032 proof requires a fresh zero-Player Session.");
                Require(
                    outputP1.UnityCamera.enabled &&
                    outputP2.UnityCamera.enabled,
                    "Zero-Player Session continuity requires configured Player-bound Outputs to remain physically available for Default/Route presentation.");
                Require(
                    outputP1.Applicator != null &&
                    outputP2.Applicator != null &&
                    outputP1.Applicator.HasAppliedDefault &&
                    outputP2.Applicator.HasAppliedDefault,
                    "Zero-Player Session continuity did not project the persistent Default Rig on every configured Output.");
                Require(
                    !playerInputManager.splitScreen,
                    "Automatic split-screen must remain physically inactive with zero associated Player Outputs.");
                Complete("zero-player-default-continuity");

                Require(
                    outputP1.CinemachineBrain.ChannelMask == OutputChannels.Default,
                    "First Session Output did not receive Cinemachine Default channel.");
                Require(
                    outputP2.CinemachineBrain.ChannelMask == OutputChannels.Channel01,
                    "Second Session Output did not receive Cinemachine Channel01.");
                Require(
                    outputP1.CinemachineBrain.ChannelMask !=
                        outputP2.CinemachineBrain.ChannelMask,
                    "Two physical Outputs share one Cinemachine channel.");
                Require(
                    outputP1.DefaultCameraRig.CinemachineCamera.OutputChannel ==
                        outputP1.CinemachineBrain.ChannelMask &&
                    outputP2.DefaultCameraRig.CinemachineCamera.OutputChannel ==
                        outputP2.CinemachineBrain.ChannelMask,
                    "Default Rig OutputChannel does not match its exact physical Brain.");
                Complete("exclusive-cinemachine-channels");

                p1Device = InputSystem.AddDevice<Keyboard>();
                p2Device = InputSystem.AddDevice<Keyboard>();
                Require(
                    p1Device != null &&
                    p2Device != null &&
                    p1Device.added &&
                    p2Device.added &&
                    p1Device.deviceId != p2Device.deviceId,
                    "ADR-032 proof could not create two distinct QA-owned input devices.");

                if (!provisioning.RuntimeSnapshot.JoiningOpen)
                {
                    PlayerParticipationOperationResult opened =
                        provisioning.OpenJoining(
                            nameof(QaCamera032PlayerOutputLifecycleRegression),
                            "camera-032-open-joining");
                    Require(
                        opened != null &&
                        opened.Succeeded &&
                        opened.Snapshot != null &&
                        opened.Snapshot.JoiningOpen,
                        opened != null
                            ? opened.ToDiagnosticString()
                            : "OpenJoining returned no result.");
                }

                p1 = provisioning.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaCamera032PlayerOutputLifecycleRegression),
                        "camera-032-p1-join",
                        p1Device));
                Require(
                    p1 != null &&
                    p1.Succeeded &&
                    p1.PlayerInput != null &&
                    p1.Slot.PlayerSlotId == slotP1.PlayerSlotId,
                    p1 != null
                        ? p1.ToDiagnosticString()
                        : "P1 Join returned no result.");

                wait = WaitFor(
                    () =>
                        outputP1.UnityCamera.enabled &&
                        !outputP2.UnityCamera.enabled &&
                        ReferenceEquals(p1.PlayerInput.camera, outputP1.UnityCamera) &&
                        playerInputManager.playerCount == 1 &&
                        PlayerInput.all.Count == 1 &&
                        !playerInputManager.splitScreen,
                    "P1 did not stabilize on its exact single physical Output.");
                while (wait.MoveNext()) yield return wait.Current;

                Complete("p1-joined-exact-output");

                wait = WaitFor(
                    () =>
                        HasThirdPersonWinner(outputP1, out _) &&
                        IsFullScreen(outputP1.UnityCamera.rect),
                    "P1 single-player ThirdPerson presentation did not become authoritative/full-screen.");
                while (wait.MoveNext()) yield return wait.Current;

                Require(
                    !outputP2.UnityCamera.enabled,
                    "Unassociated P2 Output covered the one-Player view.");
                Complete("p1-third-person-full-screen");

                p2 = provisioning.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaCamera032PlayerOutputLifecycleRegression),
                        "camera-032-p2-join",
                        p2Device));
                Require(
                    p2 != null &&
                    p2.Succeeded &&
                    p2.PlayerInput != null &&
                    p2.Slot.PlayerSlotId == slotP2.PlayerSlotId,
                    p2 != null
                        ? p2.ToDiagnosticString()
                        : "P2 Join returned no result.");

                wait = WaitFor(
                    () =>
                        outputP1.UnityCamera.enabled &&
                        outputP2.UnityCamera.enabled &&
                        ReferenceEquals(p1.PlayerInput.camera, outputP1.UnityCamera) &&
                        ReferenceEquals(p2.PlayerInput.camera, outputP2.UnityCamera) &&
                        playerInputManager.playerCount == 2 &&
                        PlayerInput.all.Count == 2 &&
                        playerInputManager.splitScreen,
                    "Two Players did not stabilize on their exact physical Outputs.");
                while (wait.MoveNext()) yield return wait.Current;

                Complete("p2-joined-exact-output");

                RequireCoherentTwoPlayerLayout(
                    outputP1.UnityCamera.rect,
                    outputP2.UnityCamera.rect);
                Complete("two-player-split-coherent");

                wait = WaitFor(
                    () =>
                        HasThirdPersonWinner(outputP1, out _) &&
                        HasThirdPersonWinner(outputP2, out _),
                    "Independent P1/P2 ThirdPerson winners did not become authoritative.");
                while (wait.MoveNext()) yield return wait.Current;

                CameraRigComposer p1ThirdPerson =
                    outputP1.Context.Winner.Rig.Composer;
                CameraRigComposer p2ThirdPerson =
                    outputP2.Context.Winner.Rig.Composer;
                Require(
                    p1ThirdPerson != null &&
                    p2ThirdPerson != null &&
                    !ReferenceEquals(p1ThirdPerson, p2ThirdPerson),
                    "P1 and P2 resolved the same materialized ThirdPerson occurrence.");
                Require(
                    p1ThirdPerson.CinemachineCamera.OutputChannel ==
                        outputP1.CinemachineBrain.ChannelMask &&
                    p2ThirdPerson.CinemachineCamera.OutputChannel ==
                        outputP2.CinemachineBrain.ChannelMask,
                    "ThirdPerson Presentation occurrence did not inherit its exact Output channel.");
                Require(
                    p1ThirdPerson.CinemachineCamera.Follow != null &&
                    p2ThirdPerson.CinemachineCamera.Follow != null &&
                    !ReferenceEquals(
                        p1ThirdPerson.CinemachineCamera.Follow,
                        p2ThirdPerson.CinemachineCamera.Follow),
                    "P1/P2 ExplicitSelection did not isolate the current Actor Camera Subjects.");
                Complete("two-player-third-person-isolated");

                PlayerInput firstP2Host = p2.PlayerInput;
                inputSystemCameraError = string.Empty;
                PlayerSlotRuntimeSnapshot p2Occurrence =
                    RequireSlot(
                        provisioning.RuntimeSnapshot,
                        slotP2.PlayerSlotId);
                SessionPlayerLeaveResult leaveP2 =
                    provisioning.RequestLeave(
                        new SessionPlayerLeaveRequest(
                            p2Occurrence.PlayerSlotId,
                            p2Occurrence.Revision,
                            nameof(QaCamera032PlayerOutputLifecycleRegression),
                            "camera-032-p2-leave"));
                Require(
                    leaveP2 != null && leaveP2.Succeeded,
                    leaveP2 != null
                        ? leaveP2.ToDiagnosticString()
                        : "P2 Leave returned no result.");
                p2 = null;

                wait = WaitFor(
                    () =>
                        !outputP2.UnityCamera.enabled &&
                        outputP1.UnityCamera.enabled &&
                        playerInputManager.playerCount == 1 &&
                        PlayerInput.all.Count == 1,
                    "P2 Leave did not retire its physical Output occurrence.");
                while (wait.MoveNext()) yield return wait.Current;

                Complete("p2-left-output-dormant");

                wait = WaitFor(
                    () =>
                        !playerInputManager.splitScreen &&
                        ReferenceEquals(p1.PlayerInput.camera, outputP1.UnityCamera) &&
                        IsFullScreen(outputP1.UnityCamera.rect),
                    "P1 did not return to a PlayerInputManager-owned full-screen layout after P2 Leave.");
                while (wait.MoveNext()) yield return wait.Current;

                Complete("p1-restored-full-screen");

                Require(
                    string.IsNullOrEmpty(inputSystemCameraError),
                    "PlayerInputManager emitted an invalid split-screen recomposition error during P2 Leave. " +
                    inputSystemCameraError);
                Complete("leave-recomposition-no-inputsystem-camera-error");

                Require(
                    HasThirdPersonWinner(outputP1, out CameraRigComposer survivingP1) &&
                    survivingP1.CinemachineCamera.Follow != null,
                    "P1 ThirdPerson presentation did not survive the independent P2 Leave.");
                Complete("p1-third-person-survives-leave");

                p2 = provisioning.RequestJoin(
                    new LocalPlayerJoinRequest(
                        nameof(QaCamera032PlayerOutputLifecycleRegression),
                        "camera-032-p2-rejoin",
                        p2Device));
                Require(
                    p2 != null &&
                    p2.Succeeded &&
                    p2.PlayerInput != null &&
                    p2.Slot.PlayerSlotId == slotP2.PlayerSlotId &&
                    !ReferenceEquals(p2.PlayerInput, firstP2Host),
                    p2 != null
                        ? p2.ToDiagnosticString()
                        : "P2 Rejoin returned no result.");
                Complete("p2-rejoined-fresh-host");

                wait = WaitFor(
                    () =>
                        playerInputManager.splitScreen &&
                        playerInputManager.playerCount == 2 &&
                        PlayerInput.all.Count == 2 &&
                        outputP1.UnityCamera.enabled &&
                        outputP2.UnityCamera.enabled &&
                        ReferenceEquals(p2.PlayerInput.camera, outputP2.UnityCamera) &&
                        HasThirdPersonWinner(outputP2, out _),
                    "P2 Rejoin did not restore exact split-screen Camera participation.");
                while (wait.MoveNext()) yield return wait.Current;

                RequireCoherentTwoPlayerLayout(
                    outputP1.UnityCamera.rect,
                    outputP2.UnityCamera.rect);
                Complete("split-restored-after-rejoin");

                inputSystemCameraError = string.Empty;
                LeaveIfJoined(
                    slotP2.PlayerSlotId,
                    "camera-032-p2-terminal-leave");
                p2 = null;
                LeaveIfJoined(
                    slotP1.PlayerSlotId,
                    "camera-032-p1-terminal-leave");
                p1 = null;

                wait = WaitFor(
                    () =>
                        provisioning.RuntimeSnapshot.JoinedCount == 0 &&
                        PlayerInput.all.Count == 0 &&
                        playerInputManager.playerCount == 0 &&
                        !playerInputManager.splitScreen &&
                        outputP1.UnityCamera.enabled &&
                        outputP2.UnityCamera.enabled &&
                        outputP1.Applicator != null &&
                        outputP2.Applicator != null &&
                        outputP1.Applicator.HasAppliedDefault &&
                        outputP2.Applicator.HasAppliedDefault,
                    "ADR-032 terminal zero-Player state did not restore Session Default continuity.");
                while (wait.MoveNext()) yield return wait.Current;

                Require(
                    string.IsNullOrEmpty(inputSystemCameraError),
                    "PlayerInputManager emitted an invalid split-screen recomposition error during terminal Leave. " +
                    inputSystemCameraError);
                Complete("terminal-default-continuity");

                Executed = true;
                Passed = completed.Count == ExpectedCases.Length;
                CompletedCaseCount = completed.Count;
                Diagnostic = Passed
                    ? "ADR-032 two-Output Player lifecycle and zero-Player Default continuity passed."
                    : $"Case count diverged. actual='{completed.Count}' expected='{ExpectedCases.Length}'.";
                Debug.Log(
                    $"{Prefix} status='{(Passed ? "Passed" : "Failed")}' " +
                    $"cases='{completed.Count}/{ExpectedCases.Length}' " +
                    $"completed='{string.Join(",", completed)}' " +
                    $"diagnostic='{Escape(Diagnostic)}'.",
                    this);
            }
            finally
            {
                TryLeaveFinally(
                    slotP2 != null ? slotP2.PlayerSlotId : default,
                    "camera-032-p2-finally");
                TryLeaveFinally(
                    slotP1 != null ? slotP1.PlayerSlotId : default,
                    "camera-032-p1-finally");
                if (p1Device != null && p1Device.added)
                    InputSystem.RemoveDevice(p1Device);
                if (p2Device != null && p2Device.added)
                    InputSystem.RemoveDevice(p2Device);
            }
        }

        private void RequireFixture()
        {
            Require(
                provisioning != null &&
                playerInputManager != null &&
                slotP1 != null &&
                slotP2 != null &&
                outputDefinitionP1 != null &&
                outputDefinitionP2 != null,
                "ADR-032 lifecycle fixture is incomplete.");
            Require(
                slotP1.PlayerSlotId.IsValid &&
                slotP2.PlayerSlotId.IsValid &&
                slotP1.PlayerSlotId != slotP2.PlayerSlotId,
                "ADR-032 lifecycle fixture requires two distinct valid Player Slots.");
            Require(
                outputDefinitionP1.HasValidId &&
                outputDefinitionP2.HasValidId &&
                outputDefinitionP1.OutputId != outputDefinitionP2.OutputId,
                "ADR-032 lifecycle fixture requires two distinct valid Camera Outputs.");
        }

        private static bool TryResolveExactOutput(
            CameraOutputDefinition definition,
            out CameraOutputAuthoring output)
        {
            output = null;
            if (definition == null) return false;

            CameraOutputAuthoring[] candidates =
                UnityEngine.Object.FindObjectsByType<CameraOutputAuthoring>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            int matches = 0;
            for (int index = 0; index < candidates.Length; index++)
            {
                CameraOutputAuthoring candidate = candidates[index];
                if (candidate == null ||
                    !ReferenceEquals(candidate.OutputDefinition, definition))
                    continue;

                matches++;
                output = candidate;
            }

            return matches == 1 && output != null;
        }

        private static bool HasThirdPersonWinner(
            CameraOutputAuthoring output,
            out CameraRigComposer composer)
        {
            composer = null;
            if (output == null ||
                output.Context == null ||
                !output.Context.HasWinner)
                return false;

            composer = output.Context.Winner.Rig.Composer;
            return composer != null &&
                composer.PresentationIntent == CameraRigPresentationIntent.ThirdPerson &&
                composer.CinemachineCamera != null &&
                composer.CinemachineCamera.enabled &&
                output.Applicator != null &&
                output.Applicator.HasAppliedRequest;
        }

        private static bool IsFullScreen(Rect rect) =>
            Approximately(rect.x, 0f) &&
            Approximately(rect.y, 0f) &&
            Approximately(rect.width, 1f) &&
            Approximately(rect.height, 1f);

        private static void RequireCoherentTwoPlayerLayout(Rect p1, Rect p2)
        {
            Require(
                p1.width > 0f && p1.height > 0f &&
                p2.width > 0f && p2.height > 0f,
                $"Split-screen contains a non-positive viewport. p1='{p1}' p2='{p2}'.");
            Require(
                !p1.Overlaps(p2),
                $"P1/P2 viewports overlap. p1='{p1}' p2='{p2}'.");
            Require(
                Approximately(p1.width + p2.width, 1f) ||
                Approximately(p1.height + p2.height, 1f),
                $"P1/P2 split does not consume one complete screen axis. p1='{p1}' p2='{p2}'.");
        }

        private void LeaveIfJoined(PlayerSlotId playerSlotId, string reason)
        {
            if (!playerSlotId.IsValid ||
                provisioning == null ||
                !provisioning.RuntimeReady)
                return;

            PlayerParticipationSnapshot snapshot = provisioning.RuntimeSnapshot;
            if (!TryFindSlot(snapshot, playerSlotId, out PlayerSlotRuntimeSnapshot slot) ||
                !slot.IsJoined)
                return;

            SessionPlayerLeaveResult result =
                provisioning.RequestLeave(
                    new SessionPlayerLeaveRequest(
                        slot.PlayerSlotId,
                        slot.Revision,
                        nameof(QaCamera032PlayerOutputLifecycleRegression),
                        reason));
            Require(
                result != null && result.Succeeded,
                result != null
                    ? result.ToDiagnosticString()
                    : $"Leave returned no result. slot='{playerSlotId.StableText}'.");
        }

        private void TryLeaveFinally(PlayerSlotId playerSlotId, string reason)
        {
            try
            {
                LeaveIfJoined(playerSlotId, reason);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"{Prefix} cleanup='Failed' slot='{playerSlotId.StableText}' " +
                    $"diagnostic='{Escape(exception.GetBaseException().Message)}'.",
                    this);
            }
        }

        private static PlayerSlotRuntimeSnapshot RequireSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId)
        {
            Require(
                TryFindSlot(snapshot, playerSlotId, out PlayerSlotRuntimeSnapshot slot),
                $"Player Slot '{playerSlotId.StableText}' is not present in current Session snapshot.");
            return slot;
        }

        private static bool TryFindSlot(
            PlayerParticipationSnapshot snapshot,
            PlayerSlotId playerSlotId,
            out PlayerSlotRuntimeSnapshot slot)
        {
            if (snapshot != null)
            {
                for (int index = 0; index < snapshot.Slots.Count; index++)
                {
                    PlayerSlotRuntimeSnapshot candidate = snapshot.Slots[index];
                    if (candidate.PlayerSlotId == playerSlotId)
                    {
                        slot = candidate;
                        return true;
                    }
                }
            }

            slot = default;
            return false;
        }

        private void OnLogMessageReceived(
            string condition,
            string stackTrace,
            LogType type)
        {
            if (type != LogType.Error &&
                type != LogType.Exception &&
                type != LogType.Assert)
            {
                return;
            }

            if (string.IsNullOrEmpty(condition) ||
                condition.IndexOf(
                    "Player has no camera associated with it. Cannot set up split-screen.",
                    StringComparison.Ordinal) < 0)
            {
                return;
            }

            inputSystemCameraError =
                string.IsNullOrEmpty(stackTrace)
                    ? condition
                    : $"{condition} {stackTrace}";
        }

        private IEnumerator WaitFor(Func<bool> condition, string failure)
        {
            for (int frame = 0; frame < FrameBudget; frame++)
            {
                if (condition()) yield break;
                yield return null;
            }

            throw new InvalidOperationException(failure);
        }

        private void Complete(string name)
        {
            int expectedIndex = completed.Count;
            Require(
                expectedIndex < ExpectedCases.Length &&
                string.Equals(ExpectedCases[expectedIndex], name, StringComparison.Ordinal),
                $"Unexpected ADR-032 case order. actual='{name}' index='{expectedIndex}'.");

            completed.Add(name);
            CompletedCaseCount = completed.Count;
            Debug.Log($"{Prefix} case='{name}' status='Passed'.", this);
        }

        private static bool Approximately(float left, float right) =>
            Mathf.Abs(left - right) <= 0.0001f;

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private void Fail(string reason)
        {
            Executed = true;
            Passed = false;
            CompletedCaseCount = completed.Count;
            Diagnostic = reason ?? string.Empty;
            Debug.LogError(
                $"{Prefix} status='Failed' " +
                $"cases='{completed.Count}/{ExpectedCases.Length}' " +
                $"completed='{string.Join(",", completed)}' " +
                $"diagnostic='{Escape(Diagnostic)}'.",
                this);
        }

        private static string Escape(string value) =>
            (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\r", " ")
                .Replace("\n", " ");
    }
}

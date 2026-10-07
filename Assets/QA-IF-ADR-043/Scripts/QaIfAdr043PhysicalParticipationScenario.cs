using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Immersive.Framework.Authoring;
using Immersive.Framework.Camera;
using Immersive.Framework.CameraAuthoring;
using Immersive.Framework.PlayerParticipation;
using Immersive.Framework.PlayerSlots;
using Immersive.QaFramework.Certification;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Immersive.QaFramework.IfAdr043
{
    [DisallowMultipleComponent]
    public sealed class QaIfAdr043PhysicalParticipationScenario : MonoBehaviour
    {
        private const string ScenarioId = "IF-ADR-043";
        private const string QaControlScheme = "QA.Keyboard";
        private static readonly string[] ExpectedCases =
        {
            "0 baseline",
            "0 -> P1",
            "P1 -> P1+P2",
            "P1+P2 -> P2",
            "P2 -> 0",
            "0 -> P1 rejoin",
            "P1 -> P1+P2 rejoin"
        };

        [SerializeField] private GameApplicationAsset gameApplication;
        [SerializeField] private LocalPlayerProvisioningAuthoring provisioning;
        [SerializeField] private LocalPlayerProvisioningEndpointRegistration provisioningRegistration;
        [SerializeField] private QaIfAdr043CameraOutputEvidence outputEvidence;
        [SerializeField] private PlayerSlotProfile player1;
        [SerializeField] private PlayerSlotProfile player2;
        [SerializeField, Min(1)] private int frameBudget = 180;

        private readonly QaCertificationRecorder _certification = new QaCertificationRecorder();
        private readonly Dictionary<PlayerSlotId, PlayerInput> _inputs = new Dictionary<PlayerSlotId, PlayerInput>();
        private readonly Dictionary<PlayerSlotId, CameraOutputId> _outputForSlot = new Dictionary<PlayerSlotId, CameraOutputId>();
        private Keyboard _player1QaDevice;
        private Keyboard _player2QaDevice;
        private SessionCameraAssignmentId _assignmentId;
        private string _assignmentText = string.Empty;
        private string _firstIssue = string.Empty;
        private string _cleanupIssue = string.Empty;
        private string _frameCoverageIssue = string.Empty;
        private int _completedCases;
        private int _framesObserved;
        private int _framesWithoutCamera;
        private int _firstFrameWithoutCamera = -1;
        private bool _started;
        private bool _terminal;
        private bool _coverageMonitoring;
        private bool _waitSucceeded;
        private string _waitIssue = string.Empty;
        private QaCleanupDisposition _cleanup = QaCleanupDisposition.FreshBootRequired;

        private void Start()
        {
            if (_started) return;
            _started = true;
            StartCoroutine(Run());
        }

        private void OnDisable()
        {
            if (!_started || _terminal) return;
            StopAllCoroutines();
            StopFrameCoverageMonitoring();
            RecordBlocked("Scenario was interrupted before its seven-case certification completed.");
            StartCoroutine(CleanupAndPublish());
        }

        private void OnDestroy() => StopFrameCoverageMonitoring();

        private IEnumerator Run()
        {
            if (!TryValidateAuthoredComposition(out string issue))
            {
                RecordBlocked("Invalid authored precondition: " + issue);
                PublishTerminal();
                yield break;
            }

            yield return WaitFor(() => provisioning.RuntimeReady &&
                outputEvidence.Outputs.Count == 2 && outputEvidence.Outputs.All(output => output != null && output.IsReady),
                "Framework did not expose one ready Local Player Provisioning endpoint and both ready Outputs.");
            if (!_waitSucceeded)
            {
                RecordBlocked(_waitIssue);
                PublishTerminal();
                yield break;
            }

            if (!TryCaptureBaseline(out issue))
            {
                RecordBlocked("Invalid runtime baseline: " + issue);
                PublishTerminal();
                yield break;
            }

            if (!TryCreateQaDevices(out issue))
            {
                RecordBlocked("Could not establish deterministic QA input infrastructure: " + issue);
                yield return CleanupAndPublish();
                yield break;
            }

            _completedCases++;
            StartFrameCoverageMonitoring();

            yield return JoinAndVerify(player1, "0 -> P1", new[] { player1.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return JoinAndVerify(player2, "P1 -> P1+P2", new[] { player1.PlayerSlotId, player2.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return LeaveAndVerify(player1, "P1+P2 -> P2", new[] { player2.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return LeaveAndVerify(player2, "P2 -> 0", Array.Empty<PlayerSlotId>());
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return JoinAndVerify(player1, "0 -> P1 rejoin", new[] { player1.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return JoinAndVerify(player2, "P1 -> P1+P2 rejoin", new[] { player1.PlayerSlotId, player2.PlayerSlotId });
            if (HasDivergence) { yield return CleanupAndPublish(); yield break; }

            yield return CleanupAndPublish();
        }

        private bool TryValidateAuthoredComposition(out string issue)
        {
            issue = string.Empty;
            if (gameApplication == null || provisioning == null || provisioningRegistration == null ||
                outputEvidence == null || player1 == null || player2 == null)
            {
                issue = "Game Application, explicit provisioning registration, Output evidence and two Slot Profiles are required.";
                return false;
            }

            if (!provisioningRegistration.TryResolveAuthoring(out LocalPlayerProvisioningAuthoring registeredProvisioning, out issue) ||
                registeredProvisioning != provisioning)
            {
                issue = "Persistent scene registration does not resolve the scenario's Local Player Provisioning endpoint. " + issue;
                return false;
            }

            if (!provisioning.HasPlayerInputManager || !provisioning.UsesManualJoin ||
                !provisioning.UsesCSharpJoinNotifications || !provisioning.IsManagerPrefabMaterialized ||
                provisioning.PlayerInputManager.maxPlayerCount != 2 || provisioning.PlayerInputManager.splitScreen ||
                provisioning.PlayerInputManager.gameObject != provisioning.gameObject)
            {
                issue = "The registered ManagerProvisioned endpoint must own one manual two-player, non-split-screen PlayerInputManager using the authored Host prefab.";
                return false;
            }

            GameObject hostPrefab = provisioning.LocalPlayerHostPrefab;
            PlayerInput hostInput = hostPrefab != null ? hostPrefab.GetComponent<PlayerInput>() : null;
            LocalPlayerHostAuthoring hostAuthoring = hostPrefab != null ? hostPrefab.GetComponent<LocalPlayerHostAuthoring>() : null;
            if (hostInput == null || hostAuthoring == null || hostAuthoring.PlayerInput != hostInput)
            {
                issue = "The authored ManagerProvisioned Host prefab must expose its root PlayerInput through LocalPlayerHostAuthoring.";
                return false;
            }
            if (hostInput.actions == null || hostInput.defaultActionMap != "Player" ||
                hostInput.defaultControlScheme != QaControlScheme ||
                hostInput.actions.FindActionMap("Player") == null || hostInput.actions.FindAction("Move") == null)
            {
                issue = "The authored Host lacks the Player input prerequisites needed to execute deterministic device-paired joins.";
                return false;
            }
            InputControlScheme? controlScheme = hostInput.actions.FindControlScheme(QaControlScheme);
            InputAction moveAction = hostInput.actions.FindAction("Move");
            if (!controlScheme.HasValue || moveAction == null ||
                controlScheme.Value.name != QaControlScheme ||
                !controlScheme.Value.deviceRequirements.Any(requirement => requirement.controlPath == "<Keyboard>" && !requirement.isOptional) ||
                !moveAction.bindings.Any(binding => (binding.groups ?? string.Empty).Split(';').Contains(QaControlScheme)))
            {
                issue = "The authored Host input prerequisites do not bind the QA.Keyboard scheme to the required Keyboard device.";
                return false;
            }

            if (!gameApplication.PlayerSessionEnabled || gameApplication.DefaultPlayerSessionProfile == null ||
                gameApplication.DefaultPlayerSessionProfile.SupportedSlotCount != 2 ||
                !gameApplication.DefaultPlayerSessionProfile.TryValidate(out issue))
            {
                if (string.IsNullOrEmpty(issue)) issue = "Game Application must enable a valid Player Session with exactly two Supported Slots.";
                return false;
            }

            if (!gameApplication.StartupRoute || !gameApplication.StartupRoute.HasPrimaryScene ||
                gameApplication.CameraSession == null || gameApplication.CameraSession.OutputPrefabs.Count != 2 ||
                gameApplication.StartupCameraAssignments.Count != 1)
            {
                issue = "Game Application requires a valid startup Route, two Camera Outputs and one startup Camera Assignment.";
                return false;
            }

            if (!player1.TryGetPlayerSlotId(out PlayerSlotId firstSlot, out issue) ||
                !player2.TryGetPlayerSlotId(out PlayerSlotId secondSlot, out issue) || firstSlot == secondSlot)
            {
                issue = "The two QA Slot Profiles must have distinct valid PlayerSlotId values. " + issue;
                return false;
            }

            PlayerSessionProfile session = gameApplication.DefaultPlayerSessionProfile;
            if (!session.InitialJoiningOpen || session.SupportedSlots.Count != 2 ||
                session.SupportedSlots[0] != player1 || session.SupportedSlots[1] != player2 ||
                session.HostProvisioning != PlayerHostProvisioningMode.ManagerProvisioned)
            {
                issue = "The ManagerProvisioned Player Session must open joining and list P1 then P2 as its exact two Supported Slots.";
                return false;
            }

            if (!gameApplication.StartupCameraAssignments[0].TryBuild(out SessionCameraAssignment assignment, out issue))
                return false;
            if (assignment.OccurrenceMode != CameraOccurrenceMode.IndividualPerPlayer ||
                assignment.MembershipPolicy != CameraMembershipPolicy.ExplicitPlayerSlots ||
                assignment.MemberSlots.Count != 2 || assignment.Outputs.Count != 2 || assignment.MemberOutputs.Count != 2)
            {
                issue = "The startup Assignment must map exactly two explicit Player Slots to two IndividualPerPlayer Outputs.";
                return false;
            }

            _outputForSlot.Clear();
            foreach (CameraPlayerOutputMapping mapping in assignment.MemberOutputs)
            {
                if (_outputForSlot.ContainsKey(mapping.PlayerSlotId))
                {
                    issue = "The startup Assignment contains duplicate Player Slot mappings.";
                    return false;
                }
                _outputForSlot.Add(mapping.PlayerSlotId, mapping.OutputId);
            }
            if (!_outputForSlot.TryGetValue(firstSlot, out _) || !_outputForSlot.TryGetValue(secondSlot, out _))
            {
                issue = "The startup Assignment must include the exact two QA Player Slot identities.";
                return false;
            }

            _assignmentId = assignment.Id;
            _assignmentText = _assignmentId.Value;
            return true;
        }

        private bool TryCaptureBaseline(out string issue)
        {
            issue = string.Empty;
            PlayerParticipationSnapshot participation = provisioning.RuntimeSnapshot;
            if (participation.Slots.Count != 2 || participation.Slots.Any(slot => slot.IsJoined))
            {
                issue = "A fresh zero-Player baseline with exactly two configured Slots is required.";
                return false;
            }

            QaIfAdr043CameraOutputProbe[] probes = outputEvidence.Outputs.ToArray();
            if (probes.Length != 2 || probes.Any(probe => probe == null || !probe.IsReady || probe.MappedSlot == null) ||
                probes.Select(probe => probe.Output.OutputId).Distinct().Count() != 2)
            {
                issue = $"Expected two unique ready runtime Camera Outputs. evidence='{outputEvidence.Diagnostic}' count='{probes.Length}'.";
                return false;
            }

            foreach (QaIfAdr043CameraOutputProbe probe in probes)
            {
                if (!probe.MappedSlot.TryGetPlayerSlotId(out PlayerSlotId slotId, out issue))
                {
                    issue = $"Runtime Output probe has an invalid mapped Slot. {issue}";
                    return false;
                }
                if (!_outputForSlot.TryGetValue(slotId, out CameraOutputId expectedOutput) || probe.Output.OutputId != expectedOutput)
                {
                    issue = $"Runtime Output '{probe.Output.OutputId}' does not match the authored Assignment identity for Slot '{slotId.StableText}'.";
                    return false;
                }

                CameraOutputState state = probe.Output.Session.OutputState;
                if (!state.HasActiveAssignment || state.ActiveAssignmentId != _assignmentId)
                {
                    issue = $"Output '{probe.Output.OutputId}' does not expose the expected active Assignment '{_assignmentText}'.";
                    return false;
                }
            }

            if (probes.Count(probe => IsPhysicalCameraEnabled(probe.Output.UnityCamera)) != 1)
            {
                issue = "The zero-Player baseline must have exactly one enabled physical QA Output Camera.";
                return false;
            }
            return true;
        }

        private bool TryCreateQaDevices(out string issue)
        {
            issue = string.Empty;
            try
            {
                _player1QaDevice = InputSystem.AddDevice<Keyboard>("QA IF-ADR-043 P1");
                _player2QaDevice = InputSystem.AddDevice<Keyboard>("QA IF-ADR-043 P2");
            }
            catch (Exception exception)
            {
                issue = exception.Message;
                return false;
            }

            if (_player1QaDevice == null || !_player1QaDevice.added ||
                _player2QaDevice == null || !_player2QaDevice.added || _player1QaDevice == _player2QaDevice)
            {
                issue = "Input System did not return two distinct added QA-owned Keyboard devices.";
                return false;
            }
            return true;
        }

        private IEnumerator JoinAndVerify(PlayerSlotProfile profile, string stage, IReadOnlyList<PlayerSlotId> expectedJoined)
        {
            Keyboard device = profile == player1 ? _player1QaDevice : profile == player2 ? _player2QaDevice : null;
            if (device == null || !device.added)
            {
                RecordBlocked($"{stage}: required QA-owned Keyboard device is unavailable; the Player contract was not exercised.");
                yield break;
            }

            var request = new LocalPlayerJoinRequest("QA-IF-ADR-043", stage, device, QaControlScheme);
            LocalPlayerJoinResult result = provisioning.RequestJoin(request);
            if (result == null || !result.Succeeded || result.PlayerInput == null || result.LocalPlayerHost == null)
            {
                RecordFail($"{stage}: public Join did not provision a Player with scheme='{QaControlScheme}' device='{device.displayName}'. result='{result?.ToDiagnosticString()}' provisioning='{provisioning.RuntimeDiagnostic}'.");
                yield break;
            }

            if (!profile.TryGetPlayerSlotId(out PlayerSlotId expectedSlot, out string issue))
            {
                RecordBlocked($"{stage}: authored Slot identity became unavailable after Join. {issue}");
                yield break;
            }
            if (result.Slot.PlayerSlotId != expectedSlot)
            {
                RecordFail($"{stage}: Join allocated Slot '{result.Slot.PlayerSlotId.StableText}', expected '{expectedSlot.StableText}'.");
                yield break;
            }

            _inputs[expectedSlot] = result.PlayerInput;
            yield return WaitForStage(stage, expectedJoined);
            if (_waitSucceeded) _completedCases++;
            else RecordFail(_waitIssue);
        }

        private IEnumerator LeaveAndVerify(PlayerSlotProfile profile, string stage, IReadOnlyList<PlayerSlotId> expectedJoined)
        {
            if (!profile.TryGetPlayerSlotId(out PlayerSlotId slotId, out string issue))
            {
                RecordBlocked($"{stage}: authored Slot identity became unavailable before Leave. {issue}");
                yield break;
            }

            PlayerParticipationSnapshot snapshot = provisioning.RuntimeSnapshot;
            PlayerSlotRuntimeSnapshot slot = default;
            bool found = false;
            for (int index = 0; index < snapshot.Slots.Count; index++)
            {
                if (snapshot.Slots[index].PlayerSlotId != slotId) continue;
                slot = snapshot.Slots[index];
                found = true;
                break;
            }
            if (!found || !slot.IsJoined)
            {
                RecordFail($"{stage}: public Session snapshot has no Joined Slot '{slotId.StableText}'. status='{snapshot.LastOperationStatus}' message='{snapshot.LastOperationMessage}'.");
                yield break;
            }

            SessionPlayerLeaveResult result = provisioning.RequestLeave(new SessionPlayerLeaveRequest(
                slotId, slot.Revision, "QA-IF-ADR-043", stage));
            if (result == null || !result.Succeeded)
            {
                RecordFail($"{stage}: public Leave failed. result='{result?.ToDiagnosticString()}'.");
                yield break;
            }
            _inputs.Remove(slotId);

            // Aguarda a destruição do PlayerInput e a liberação do device após o Leave público.
            yield return null;
            yield return WaitForStage(stage, expectedJoined);
            if (_waitSucceeded) _completedCases++;
            else RecordFail(_waitIssue);
        }

        private IEnumerator WaitForStage(string stage, IReadOnlyList<PlayerSlotId> expectedJoined, bool requireContinuousCameraFrames = true)
        {
            _waitSucceeded = false;
            _waitIssue = string.Empty;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (requireContinuousCameraFrames && !string.IsNullOrEmpty(_frameCoverageIssue))
                {
                    _waitIssue = _frameCoverageIssue;
                    yield break;
                }
                if (VerifyStage(stage, expectedJoined, out _waitIssue, requireContinuousCameraFrames))
                {
                    _waitSucceeded = true;
                    yield break;
                }
                yield return null;
            }
            _waitIssue = $"{stage}: public Player/Output state did not converge within {frameBudget} frames. last='{_waitIssue}'";
        }

        private bool VerifyStage(string stage, IReadOnlyList<PlayerSlotId> expectedJoined,
            out string issue, bool requireContinuousCameraFrames = true)
        {
            issue = string.Empty;
            if (requireContinuousCameraFrames && !string.IsNullOrEmpty(_frameCoverageIssue))
            {
                issue = _frameCoverageIssue;
                return false;
            }

            QaIfAdr043CameraOutputProbe[] probes = outputEvidence.Outputs.ToArray();
            if (probes.Length != 2 || probes.Any(probe => probe == null || !probe.IsReady || probe.MappedSlot == null))
            {
                issue = $"{stage}: two ready runtime Outputs are required. evidence='{outputEvidence.Diagnostic}'.";
                return false;
            }

            PlayerParticipationSnapshot snapshot = provisioning.RuntimeSnapshot;
            PlayerSlotId[] observedJoined = snapshot.Slots.Where(slot => slot.IsJoined)
                .Select(slot => slot.PlayerSlotId).ToArray();
            if (snapshot.Slots.Count != 2 || expectedJoined.Count != observedJoined.Length ||
                expectedJoined.Except(observedJoined).Any() || observedJoined.Except(expectedJoined).Any())
            {
                issue = $"{stage}: public Player Session membership='{DescribeSlots(observedJoined)}' expected='{DescribeSlots(expectedJoined)}'.";
                return false;
            }

            int enabledOutputCount = 0;
            foreach (QaIfAdr043CameraOutputProbe probe in probes)
            {
                CameraOutputAuthoring output = probe.Output;
                if (!_outputForSlot.TryGetValue(probe.MappedSlot.PlayerSlotId, out CameraOutputId expectedOutput) ||
                    output.OutputId != expectedOutput)
                {
                    issue = $"{stage}: runtime Output '{output.OutputId}' no longer matches its authored Slot/Output identity.";
                    return false;
                }

                CameraOutputState state = output.Session.OutputState;
                if (!state.HasActiveAssignment || state.ActiveAssignmentId != _assignmentId)
                {
                    issue = $"{stage}: Output '{output.OutputId}' lost Assignment identity '{_assignmentText}'.";
                    return false;
                }

                bool bound = expectedJoined.Contains(probe.MappedSlot.PlayerSlotId);
                bool physicalEnabled = IsPhysicalCameraEnabled(output.UnityCamera);
                if (physicalEnabled) enabledOutputCount++;
                if (physicalEnabled != bound && expectedJoined.Count > 0)
                {
                    issue = $"{stage}: physical Output '{output.OutputId}' enabled='{physicalEnabled}' bound='{bound}' for Players='{DescribeSlots(expectedJoined)}'.";
                    return false;
                }

                if (expectedJoined.Count > 0 && !bound && physicalEnabled)
                {
                    issue = $"{stage}: non-bound physical Output '{output.OutputId}' is enabled while Players are Joined.";
                    return false;
                }
            }

            int expectedEnabledCount = expectedJoined.Count == 0 ? 1 : expectedJoined.Count;
            if (enabledOutputCount != expectedEnabledCount)
            {
                issue = $"{stage}: enabled physical Output count='{enabledOutputCount}', expected='{expectedEnabledCount}'.";
                return false;
            }

            foreach (PlayerSlotId slotId in expectedJoined)
            {
                if (!_inputs.TryGetValue(slotId, out PlayerInput input) || input == null)
                {
                    issue = $"{stage}: no live public PlayerInput is retained for Slot '{slotId.StableText}'.";
                    return false;
                }

                QaIfAdr043CameraOutputProbe mappedOutput = probes.SingleOrDefault(probe =>
                    _outputForSlot.TryGetValue(slotId, out CameraOutputId outputId) && probe.Output.OutputId == outputId);
                if (mappedOutput == null || input.camera != mappedOutput.Output.UnityCamera)
                {
                    issue = $"{stage}: PlayerInput.camera for Slot '{slotId.StableText}' does not reference its assigned physical Output Camera.";
                    return false;
                }
            }
            return true;
        }

        private void StartFrameCoverageMonitoring()
        {
            _coverageMonitoring = true;
            StartCoroutine(ObserveOutputFrames());
        }

        private void StopFrameCoverageMonitoring()
        {
            _coverageMonitoring = false;
        }

        private IEnumerator ObserveOutputFrames()
        {
            while (_coverageMonitoring)
            {
                yield return new WaitForEndOfFrame();
                if (!_coverageMonitoring) yield break;

                _framesObserved++;
                int participatingOutputs = 0;
                if (outputEvidence != null)
                {
                    foreach (QaIfAdr043CameraOutputProbe probe in outputEvidence.Outputs)
                    {
                        if (probe != null && probe.IsReady &&
                            IsPhysicalCameraEnabled(probe.Output.UnityCamera))
                        {
                            participatingOutputs++;
                        }
                    }
                }

                if (participatingOutputs > 0) continue;
                _framesWithoutCamera++;
                if (_firstFrameWithoutCamera < 0)
                    _firstFrameWithoutCamera = Time.frameCount;
                if (string.IsNullOrEmpty(_frameCoverageIssue))
                    _frameCoverageIssue = $"Frame '{Time.frameCount}' had no physically participating registered QA Output Camera (both Outputs were null, disabled, or inactive).";
            }
        }

        private IEnumerator WaitFor(Func<bool> condition, string timeoutIssue)
        {
            _waitSucceeded = false;
            _waitIssue = string.Empty;
            for (int frame = 0; frame < Mathf.Max(1, frameBudget); frame++)
            {
                if (condition())
                {
                    _waitSucceeded = true;
                    yield break;
                }
                yield return null;
            }
            _waitIssue = timeoutIssue + " " + (provisioning != null ? provisioning.RuntimeDiagnostic : string.Empty) + " " +
                         (outputEvidence != null ? outputEvidence.Diagnostic : string.Empty);
        }

        private IEnumerator CleanupAndPublish()
        {
            if (provisioning == null || !provisioning.RuntimeReady)
            {
                SetCleanupFailure("Provisioning endpoint was unavailable; public Leaves could not be confirmed.");
                StopFrameCoverageMonitoring();
                PublishTerminal();
                yield break;
            }

            int leaveBudget = Mathf.Max(2, provisioning.RuntimeSnapshot.Slots.Count);
            bool leavesSucceeded = true;
            for (int pass = 0; pass < leaveBudget; pass++)
            {
                PlayerSlotRuntimeSnapshot joined = provisioning.RuntimeSnapshot.Slots.FirstOrDefault(slot => slot.IsJoined);
                if (!joined.IsValid) break;

                SessionPlayerLeaveResult leave = provisioning.RequestLeave(new SessionPlayerLeaveRequest(
                    joined.PlayerSlotId, joined.Revision, "QA-IF-ADR-043", "terminal-cleanup"));
                if (leave == null || !leave.Succeeded)
                {
                    leavesSucceeded = false;
                    SetCleanupFailure("Public cleanup Leave failed. " + leave?.ToDiagnosticString());
                    break;
                }
                _inputs.Remove(joined.PlayerSlotId);
            }

            PlayerSlotId[] remainingSlots = CurrentJoinedSlots();
            if (remainingSlots.Length != 0)
            {
                SetCleanupFailure("Public cleanup left Joined Slots: " + DescribeSlots(remainingSlots) + ".");
                StopFrameCoverageMonitoring();
                PublishTerminal();
                yield break;
            }

            if (!leavesSucceeded)
            {
                StopFrameCoverageMonitoring();
                PublishTerminal();
                yield break;
            }

            if (outputEvidence != null && outputEvidence.Outputs.Count == 2)
            {
                yield return WaitForStage("BaselineRestored", Array.Empty<PlayerSlotId>(), requireContinuousCameraFrames: false);
                if (!_waitSucceeded)
                    SetCleanupFailure("Zero-Player Camera baseline was not restored. " + _waitIssue);
            }
            else
            {
                SetCleanupFailure("Cleanup could not observe both QA Camera Outputs.");
            }

            // PlayerInputManager releases its PlayerInput/device pairing during the public Leave.
            yield return null;
            RemoveQaDevice(ref _player1QaDevice, "P1");
            RemoveQaDevice(ref _player2QaDevice, "P2");
            StopFrameCoverageMonitoring();

            if (string.IsNullOrEmpty(_cleanupIssue))
                _cleanup = QaCleanupDisposition.BaselineRestored;
            else
                _cleanup = QaCleanupDisposition.FreshBootRequired;

            PublishTerminal();
        }

        private void RemoveQaDevice(ref Keyboard device, string player)
        {
            if (device == null) return;
            try
            {
                if (device.added) InputSystem.RemoveDevice(device);
            }
            catch (Exception exception)
            {
                SetCleanupFailure($"QA-owned Keyboard for {player} could not be removed: {exception.Message}");
                return;
            }
            if (device.added)
            {
                SetCleanupFailure($"QA-owned Keyboard for {player} remained added after its public Leave.");
                return;
            }
            device = null;
        }

        private PlayerSlotId[] CurrentJoinedSlots() => provisioning.RuntimeSnapshot.Slots
            .Where(slot => slot.IsJoined).Select(slot => slot.PlayerSlotId).ToArray();

        private static bool IsPhysicalCameraEnabled(Camera camera) =>
            camera != null && camera.enabled && camera.gameObject.activeInHierarchy;

        private bool HasDivergence => !string.IsNullOrEmpty(_firstIssue);

        private void RecordFail(string issue)
        {
            if (HasDivergence) return;
            _firstIssue = string.IsNullOrWhiteSpace(issue) ? "Functional divergence without diagnostic." : issue;
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Fail, _firstIssue);
        }

        private void RecordBlocked(string issue)
        {
            if (HasDivergence) return;
            _firstIssue = string.IsNullOrWhiteSpace(issue) ? "Scenario precondition was not satisfied." : issue;
            _certification.RecordFirstCausalDivergence(QaCertificationVerdict.Blocked, _firstIssue);
        }

        private void SetCleanupFailure(string issue)
        {
            if (string.IsNullOrEmpty(_cleanupIssue))
                _cleanupIssue = issue;
            else
                _cleanupIssue += " " + issue;
        }

        private void PublishTerminal()
        {
            if (_terminal) return;
            _terminal = true;
            if (!HasDivergence)
            {
                if (_cleanup == QaCleanupDisposition.BaselineRestored)
                    _certification.RecordPass();
                else if (_completedCases == 0)
                    RecordBlocked("No functional case ran because the authored/runtime precondition was not established.");
                else
                    RecordFail("Cleanup did not restore the zero-Player baseline: " + _cleanupIssue);
            }

            QaCertificationResult result = _certification.CreateResult(ScenarioId, _cleanup, _cleanupIssue);
            string status = result.Verdict switch
            {
                QaCertificationVerdict.Pass => "Passed",
                QaCertificationVerdict.Blocked => "Blocked",
                _ => "Failed"
            };
            string next = _completedCases < ExpectedCases.Length ? ExpectedCases[_completedCases] : "<none>";
            string message = $"[{ScenarioId}] status='{status}' verdict='{result.Verdict.ToString().ToUpperInvariant()}' cases='{_completedCases}/{ExpectedCases.Length}' next='{Sanitize(next)}' completed='{_completedCases}' missing='{Sanitize(next)}' assignment='{_assignmentText}' framesObserved='{_framesObserved}' framesWithoutCamera='{_framesWithoutCamera}' firstFrameWithoutCamera='{_firstFrameWithoutCamera}' execution='{Sanitize(result.FirstCausalDivergence)}' unwind='public-leave-requests' cleanup='{result.CleanupDisposition}' cleanupIssue='{Sanitize(result.CleanupIssue)}'.";
            if (result.Verdict == QaCertificationVerdict.Pass) Debug.Log(message, this);
            else if (result.Verdict == QaCertificationVerdict.Blocked) Debug.LogWarning(message, this);
            else Debug.LogError(message, this);
        }

        private static string DescribeSlots(IEnumerable<PlayerSlotId> slots) =>
            string.Join(",", slots.Select(slot => slot.StableText));

        private static string Sanitize(string value) => (value ?? string.Empty)
            .Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", "\\r").Replace("\n", "\\n");
    }
}

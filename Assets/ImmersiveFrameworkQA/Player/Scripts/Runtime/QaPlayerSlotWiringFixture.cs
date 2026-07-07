using System;
using Immersive.Framework.Actors;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Reset.Unity;
using Immersive.Framework.UnityInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/F48C PlayerSlot Wiring Fixture")]
    public sealed class QaPlayerSlotWiringFixture : MonoBehaviour
    {
        private const string Source = nameof(QaPlayerSlotWiringFixture);
        private const string Reason = "qa.playerslot-wiring";
        private const string ExpectedSlotId = "player.1";
        private const string ExpectedActorId = "qa.player.actor";

        [Header("Positive Wiring")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerSlotDeclaration playerSlot;
        [SerializeField] private PlayerActorDeclaration playerActor;
        [SerializeField] private PlayerSlotOccupancy occupancy;
        [SerializeField] private UnityPlayerInputGateAdapter inputGateAdapter;
        [SerializeField] private UnityResetSubjectAdapter resetSubjectAdapter;

        [Header("Negative Wiring")]
        [SerializeField] private PlayerSlotOccupancy conflictingOccupancy;

        [Header("Execution")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool applyGateAdapterOnStart = true;
        [SerializeField] private bool registerResetSubjectOnStart = true;
        [SerializeField] private bool retryUntilPassed = true;
        [SerializeField] private int retryFrameInterval = 30;
        [SerializeField] private int retryAttemptLimit = 20;

        private bool _lastSmokePassed;
        private bool _waitingForRuntimeRegistration;
        private int _retryFrameCountdown;
        private int _retryAttempts;

        private void Start()
        {
            if (runOnStart)
            {
                RunSmoke();
            }
        }

        private void Update()
        {
            if (!runOnStart || !retryUntilPassed || _lastSmokePassed || !_waitingForRuntimeRegistration)
            {
                return;
            }

            if (_retryAttempts >= retryAttemptLimit)
            {
                return;
            }

            if (_retryFrameCountdown > 0)
            {
                _retryFrameCountdown--;
                return;
            }

            _retryAttempts++;
            _retryFrameCountdown = Math.Max(1, retryFrameInterval);
            RunSmoke();
        }

        [ContextMenu("Immersive Framework QA/Player/Run F48C PlayerSlot Wiring Smoke")]
        public void RunSmoke()
        {
            _waitingForRuntimeRegistration = false;
            bool playerInputPassed = playerInput != null;
            bool slotPassed = ValidateSlot();
            bool actorPassed = ValidateActor();
            bool occupancyPassed = ValidatePositiveOccupancy();
            bool gatePassed = ValidateInputGateAdapter();
            bool resetPassed = ValidateResetSubjectAdapter();
            bool noDuplicateActorPassed = ValidateNoDuplicateActorDeclaration();
            bool conflictPassed = ValidateConflictDiagnostic();

            bool passed = playerInputPassed
                && slotPassed
                && actorPassed
                && occupancyPassed
                && gatePassed
                && resetPassed
                && noDuplicateActorPassed
                && conflictPassed;

            if (applyGateAdapterOnStart && inputGateAdapter != null)
            {
                inputGateAdapter.ApplyCurrentGate();
            }

            _lastSmokePassed = passed;

            Debug.Log(
                $"[F48C_PLAYER_SLOT_WIRING_QA] status='{(passed ? "Succeeded" : "Failed")}' " +
                $"playerInput='{FormatObject(playerInput)}' " +
                $"slot='{slotPassed}' actor='{actorPassed}' occupancy='{occupancyPassed}' " +
                $"inputGate='{gatePassed}' resetSubject='{resetPassed}' noDuplicateActor='{noDuplicateActorPassed}' conflict='{conflictPassed}'.");
        }

        private bool ValidateSlot()
        {
            if (playerSlot == null)
            {
                Debug.LogError("[F48C_PLAYER_SLOT_WIRING_QA] PlayerSlotDeclaration is missing.");
                return false;
            }

            string slotId = playerSlot.PlayerSlotId.Value.Value;
            bool passed = string.Equals(slotId, ExpectedSlotId, StringComparison.Ordinal)
                && playerSlot.HasPlayerInputEvidence;

            Debug.Log(
                $"[F48C_PLAYER_SLOT_WIRING_QA] PlayerSlotDeclaration validation. " +
                $"passed='{passed}' playerSlotId='{slotId}' stableText='{playerSlot.PlayerSlotId.StableText}' " +
                $"playerInputEvidence='{FormatObject(playerSlot.PlayerInputEvidence)}'.");
            return passed;
        }

        private bool ValidateActor()
        {
            if (playerActor == null)
            {
                Debug.LogError("[F48C_PLAYER_SLOT_WIRING_QA] PlayerActorDeclaration is missing.");
                return false;
            }

            string actorId = playerActor.ActorId.Value.Value;
            bool passed = string.Equals(actorId, ExpectedActorId, StringComparison.Ordinal)
                && playerActor.ActorKind == ActorKind.Player
                && playerActor.ActorRole == ActorRole.Protagonist
                && playerActor.HasPlayerInputEvidence;

            Debug.Log(
                $"[F48C_PLAYER_SLOT_WIRING_QA] PlayerActorDeclaration validation. " +
                $"passed='{passed}' actorId='{actorId}' stableText='{playerActor.ActorId.StableText}' " +
                $"kind='{playerActor.ActorKind}' role='{playerActor.ActorRole}' playerInputEvidence='{FormatObject(playerActor.PlayerInput)}'.");
            return passed;
        }

        private bool ValidatePositiveOccupancy()
        {
            if (playerSlot == null || occupancy == null)
            {
                Debug.LogError("[F48C_PLAYER_SLOT_WIRING_QA] PlayerSlotOccupancy positive fixture is missing.");
                return false;
            }

            PlayerSlotSet set = PlayerSlotValidator.ValidateDeclarations(
                new[] { playerSlot },
                new[] { occupancy },
                Source,
                Reason + ".positive-occupancy");

            bool passed = set.Succeeded
                && set.Count == 1
                && set.OccupancyCount == 1
                && set.Occupancies[0].PlayerSlotId.Value.Value == ExpectedSlotId
                && set.Occupancies[0].OccupiedActorId.Value.Value == ExpectedActorId
                && set.Occupancies[0].HasActorDeclarationEvidence;

            Debug.Log(
                $"[F48C_PLAYER_SLOT_WIRING_QA] PlayerSlotOccupancy validation. " +
                $"passed='{passed}' playerSlotId='{(set.OccupancyCount > 0 ? set.Occupancies[0].PlayerSlotId.StableText : "<missing>")}' " +
                $"occupiedActorId='{(set.OccupancyCount > 0 ? set.Occupancies[0].OccupiedActorId.StableText : "<missing>")}' " +
                $"diagnostics='{set.ToDiagnosticString()}'.");
            return passed;
        }

        private bool ValidateInputGateAdapter()
        {
            if (inputGateAdapter == null)
            {
                Debug.LogError("[F48C_PLAYER_SLOT_WIRING_QA] UnityPlayerInputGateAdapter is missing.");
                return false;
            }

            bool passed = inputGateAdapter.PlayerInput == playerInput
                && inputGateAdapter.HasSourceSlot
                && inputGateAdapter.SourceSlot == playerSlot
                && string.Equals(inputGateAdapter.SourceSlotIdText, ExpectedSlotId, StringComparison.Ordinal);

            Debug.Log(
                $"[F48C_PLAYER_SLOT_WIRING_QA] UnityPlayerInputGateAdapter validation. " +
                $"passed='{passed}' playerSlotSource='{(inputGateAdapter.HasSourceSlot ? "PlayerSlotDeclaration" : "None")}' " +
                $"playerSlotId='{inputGateAdapter.SourceSlotIdText}' sourceSlot='{FormatObject(inputGateAdapter.SourceSlot)}'.");
            return passed;
        }

        private bool ValidateResetSubjectAdapter()
        {
            if (resetSubjectAdapter == null)
            {
                Debug.LogError("[F48C_PLAYER_SLOT_WIRING_QA] UnityResetSubjectAdapter is missing.");
                return false;
            }

            bool sourcePassed = resetSubjectAdapter.HasSourcePlayerActor
                && !resetSubjectAdapter.HasSourceActor
                && resetSubjectAdapter.SourcePlayerActor == playerActor;

            bool registrationPassed = true;
            string registrationStatus = "NotRequested";
            if (registerResetSubjectOnStart)
            {
                registrationPassed = resetSubjectAdapter.IsRegistered
                    || resetSubjectAdapter.RegisterWithCurrentHost(Reason + ".reset-register");
                registrationStatus = resetSubjectAdapter.IsRegistered ? "Registered" : "PendingOrRejected";
                _waitingForRuntimeRegistration = !registrationPassed;
            }

            Debug.Log(
                $"[F48C_PLAYER_SLOT_WIRING_QA] UnityResetSubjectAdapter validation. " +
                $"passed='{sourcePassed && registrationPassed}' sourceActor='{FormatObject(resetSubjectAdapter.SourceActor)}' " +
                $"sourcePlayerActor='{FormatObject(resetSubjectAdapter.SourcePlayerActor)}' " +
                $"registrationStatus='{registrationStatus}' subjectId='{(resetSubjectAdapter.IsRegistered ? resetSubjectAdapter.SubjectId.StableText : "<none>")}'.");
            return sourcePassed && registrationPassed;
        }

        private bool ValidateNoDuplicateActorDeclaration()
        {
            bool passed = playerActor != null
                && playerActor.GetComponent<ActorDeclaration>() == null;

            Debug.Log(
                $"[F48C_PLAYER_SLOT_WIRING_QA] Positive player duplicate ActorDeclaration validation. " +
                $"passed='{passed}' actorDeclaration='{FormatObject(playerActor != null ? playerActor.GetComponent<ActorDeclaration>() : null)}' " +
                $"playerActorDeclaration='{FormatObject(playerActor)}'.");
            return passed;
        }

        private bool ValidateConflictDiagnostic()
        {
            if (playerSlot == null || conflictingOccupancy == null)
            {
                Debug.LogWarning("[F48C_PLAYER_SLOT_WIRING_QA] Conflict fixture is missing; negative path skipped.");
                return true;
            }

            PlayerSlotSet conflictSet = PlayerSlotValidator.ValidateDeclarations(
                new[] { playerSlot },
                new[] { conflictingOccupancy },
                Source,
                Reason + ".conflict");

            bool hasConflictIssue = false;
            for (int i = 0; i < conflictSet.Issues.Count; i++)
            {
                if (conflictSet.Issues[i].Kind == PlayerSlotSetIssueKind.ConflictingOccupiedActorSources
                    && conflictSet.Issues[i].Blocking)
                {
                    hasConflictIssue = true;
                    break;
                }
            }

            bool passed = conflictSet.Failed && hasConflictIssue;
            Debug.Log(
                $"[F48C_PLAYER_SLOT_WIRING_QA] Conflicting ActorDeclaration/PlayerActorDeclaration validation. " +
                $"passed='{passed}' diagnostics='{conflictSet.ToDiagnosticString()}'.");
            return passed;
        }

        private static string FormatObject(UnityEngine.Object target)
        {
            return target != null ? target.name : "<none>";
        }
    }
}

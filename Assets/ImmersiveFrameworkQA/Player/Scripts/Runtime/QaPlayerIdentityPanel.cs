using System;
using System.Reflection;
using System.Text;
using Immersive.Framework.Actors;
using Immersive.Framework.GameFlow;
using Immersive.Framework.PlayerSlots;
using Immersive.Framework.Reset;
using Immersive.Framework.Reset.Unity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ImmersiveFrameworkQA.Player
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Player/Player Identity Panel")]
    public sealed class QaPlayerIdentityPanel : MonoBehaviour
    {
        [SerializeField] private string title = "Player Identity QA";
        [SerializeField] private Rect panelRect = new Rect(16f, 16f, 620f, 680f);
        [SerializeField] private Text summaryText;
        [SerializeField] private Text resultText;
        [SerializeField] private RouteRequestTrigger backToHubTrigger;

        private string _lastResult = "Idle. Run an Actor, PlayerSlot, or Reset Bridge QA probe.";
        private bool _lastPassed;
        private int _passCount;
        private int _failCount;
        private Vector2 _scrollPosition;

        [ContextMenu("Actor Identity QA/Run Valid Actor QA")]
        public void RunValidActorQa()
        {
            SetResult(RunValidActorCheck());
        }

        [ContextMenu("Actor Identity QA/Run Invalid ActorId QA")]
        public void RunInvalidActorIdQa()
        {
            SetResult(RunInvalidActorIdCheck());
        }

        [ContextMenu("Actor Identity QA/Run Duplicate ActorId QA")]
        public void RunDuplicateActorIdQa()
        {
            SetResult(RunDuplicateActorIdCheck());
        }

        [ContextMenu("Actor Identity QA/Run PlayerActor QA")]
        public void RunPlayerActorQa()
        {
            SetResult(RunPlayerActorCheck());
        }

        [ContextMenu("Actor Identity QA/Run All Actor Identity QA")]
        public void RunAllActorIdentityQa()
        {
            QaCheckResult[] results =
            {
                RunValidActorCheck(),
                RunInvalidActorIdCheck(),
                RunDuplicateActorIdCheck(),
                RunPlayerActorCheck(),
                RunUnknownRoleCheck()
            };

            bool passed = true;
            var builder = new StringBuilder();
            builder.AppendLine("Run All Actor Identity QA");
            for (int i = 0; i < results.Length; i++)
            {
                passed &= results[i].Passed;
                builder.Append(results[i].Passed ? "PASS" : "FAIL");
                builder.Append(" - ");
                builder.AppendLine(results[i].Message);
            }

            SetResult(new QaCheckResult(passed, builder.ToString().TrimEnd()));
        }

        [ContextMenu("PlayerSlot QA/Run Valid PlayerSlot QA")]
        public void RunValidPlayerSlotQa()
        {
            SetPlayerSlotResult(RunValidPlayerSlotCheck());
        }

        [ContextMenu("PlayerSlot QA/Run Invalid PlayerSlotId QA")]
        public void RunInvalidPlayerSlotIdQa()
        {
            SetPlayerSlotResult(RunInvalidPlayerSlotIdCheck());
        }

        [ContextMenu("PlayerSlot QA/Run Duplicate PlayerSlotId QA")]
        public void RunDuplicatePlayerSlotIdQa()
        {
            SetPlayerSlotResult(RunDuplicatePlayerSlotIdCheck());
        }

        [ContextMenu("PlayerSlot QA/Run Valid Occupancy QA")]
        public void RunValidOccupancyQa()
        {
            SetPlayerSlotResult(RunValidOccupancyCheck());
        }

        [ContextMenu("PlayerSlot QA/Run Missing Occupied Actor QA")]
        public void RunMissingOccupiedActorQa()
        {
            SetPlayerSlotResult(RunMissingOccupiedActorCheck());
        }

        [ContextMenu("PlayerSlot QA/Run Invalid Occupied ActorId QA")]
        public void RunInvalidOccupiedActorIdQa()
        {
            SetPlayerSlotResult(RunInvalidOccupiedActorIdCheck());
        }

        [ContextMenu("PlayerSlot QA/Run Duplicate Occupancy QA")]
        public void RunDuplicateOccupancyQa()
        {
            SetPlayerSlotResult(RunDuplicateOccupancyCheck());
        }

        [ContextMenu("PlayerSlot QA/Run All PlayerSlot QA")]
        public void RunAllPlayerSlotQa()
        {
            SetPlayerSlotResult(RunAllPlayerSlotChecks());
        }

        [ContextMenu("PlayerSlot QA/Run All Actor + PlayerSlot QA")]
        public void RunAllActorAndPlayerSlotQa()
        {
            QaCheckResult[] results =
            {
                RunValidActorCheck(),
                RunInvalidActorIdCheck(),
                RunDuplicateActorIdCheck(),
                RunPlayerActorCheck(),
                RunUnknownRoleCheck(),
                RunValidPlayerSlotCheck(),
                RunInvalidPlayerSlotIdCheck(),
                RunDuplicatePlayerSlotIdCheck(),
                RunValidOccupancyCheck(),
                RunMissingOccupiedActorCheck(),
                RunInvalidOccupiedActorIdCheck(),
                RunDuplicateOccupancyCheck()
            };

            SetPlayerSlotResult(FormatAggregateResult("Run All Actor + PlayerSlot QA", results));
        }
        [ContextMenu("Reset Bridge QA/Run AuthoredText Reset Subject QA")]
        public void RunAuthoredTextResetSubjectBridgeQa()
        {
            SetResetBridgeResult(RunAuthoredTextResetSubjectBridgeCheck());
        }

        [ContextMenu("Reset Bridge QA/Run Actor Source Reset Subject QA")]
        public void RunActorSourceResetSubjectBridgeQa()
        {
            SetResetBridgeResult(RunActorSourceResetSubjectBridgeCheck());
        }

        [ContextMenu("Reset Bridge QA/Run Runtime Prefix Reset Subject QA")]
        public void RunRuntimePrefixResetSubjectBridgeQa()
        {
            SetResetBridgeResult(RunRuntimePrefixResetSubjectBridgeCheck());
        }

        [ContextMenu("Reset Bridge QA/Run Invalid Actor Source Reset Subject QA")]
        public void RunInvalidActorSourceResetSubjectBridgeQa()
        {
            SetResetBridgeResult(RunInvalidActorSourceResetSubjectBridgeCheck());
        }

        [ContextMenu("Reset Bridge QA/Run All Reset Bridge QA")]
        public void RunAllResetBridgeQa()
        {
            SetResetBridgeResult(RunAllResetBridgeChecks());
        }

        [ContextMenu("Reset Bridge QA/Run All Actor + PlayerSlot + Reset Bridge QA")]
        public void RunAllActorPlayerSlotAndResetBridgeQa()
        {
            QaCheckResult[] results =
            {
                RunValidActorCheck(),
                RunInvalidActorIdCheck(),
                RunDuplicateActorIdCheck(),
                RunPlayerActorCheck(),
                RunUnknownRoleCheck(),
                RunValidPlayerSlotCheck(),
                RunInvalidPlayerSlotIdCheck(),
                RunDuplicatePlayerSlotIdCheck(),
                RunValidOccupancyCheck(),
                RunMissingOccupiedActorCheck(),
                RunInvalidOccupiedActorIdCheck(),
                RunDuplicateOccupancyCheck(),
                RunAuthoredTextResetSubjectBridgeCheck(),
                RunActorSourceResetSubjectBridgeCheck(),
                RunRuntimePrefixResetSubjectBridgeCheck(),
                RunInvalidActorSourceResetSubjectBridgeCheck()
            };

            SetResetBridgeResult(FormatAggregateResult("Run All Actor + PlayerSlot + Reset Bridge QA", results));
        }


        public void BackToQaHub()
        {
            if (backToHubTrigger == null)
            {
                SetResult(new QaCheckResult(false, "Back to QA Hub trigger is not assigned."));
                return;
            }

            backToHubTrigger.RequestRoute();
            SetResult(new QaCheckResult(true, "Requested Back to QA Hub route."));
        }

        public void Configure(Text nextSummaryText, Text nextResultText, RouteRequestTrigger nextBackToHubTrigger, Rect nextPanelRect)
        {
            summaryText = nextSummaryText;
            resultText = nextResultText;
            backToHubTrigger = nextBackToHubTrigger;
            panelRect = nextPanelRect;
            RefreshTexts();
        }

        private QaCheckResult RunValidActorCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ActorIdentity_ValidNonPlayer");
            try
            {
                ActorDeclaration actor = CreateActor(
                    root,
                    "Valid NonPlayer Actor",
                    "qa.actor.enemy.valid",
                    ActorKind.NonPlayer,
                    ActorRole.Enemy);

                ActorSet set = ActorValidator.ValidateDeclarations(
                    new[] { actor },
                    Array.Empty<PlayerActorDeclaration>(),
                    nameof(QaPlayerIdentityPanel),
                    "qa.actor.identity.valid-non-player");

                bool passed = set.Succeeded
                    && set.Count == 1
                    && set.NonPlayerActorCount == 1
                    && set.PlayerActorCount == 0;

                return new QaCheckResult(
                    passed,
                    passed
                        ? "Valid NonPlayer Actor PASS. ActorDeclaration validates without PlayerInput."
                        : "Valid NonPlayer Actor failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunInvalidActorIdCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ActorIdentity_InvalidEmptyActorId");
            try
            {
                ActorDeclaration actor = CreateActor(
                    root,
                    "Invalid Empty ActorId",
                    string.Empty,
                    ActorKind.NonPlayer,
                    ActorRole.Enemy);

                ActorSet set = ActorValidator.ValidateDeclarations(
                    new[] { actor },
                    Array.Empty<PlayerActorDeclaration>(),
                    nameof(QaPlayerIdentityPanel),
                    "qa.actor.identity.invalid-empty-actor-id");

                bool passed = set.Failed && ContainsIssue(set, ActorSetIssueKind.InvalidActorId, blocking: true);
                return new QaCheckResult(
                    passed,
                    passed
                        ? "Invalid Empty ActorId PASS. Validator emitted blocking InvalidActorId."
                        : "Invalid Empty ActorId failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunDuplicateActorIdCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ActorIdentity_DuplicateActorId");
            try
            {
                ActorDeclaration first = CreateActor(
                    root,
                    "Duplicate Actor A",
                    "qa.actor.duplicate",
                    ActorKind.NonPlayer,
                    ActorRole.Enemy);

                ActorDeclaration second = CreateActor(
                    root,
                    "Duplicate Actor B",
                    "qa.actor.duplicate",
                    ActorKind.NonPlayer,
                    ActorRole.Ally);

                ActorSet set = ActorValidator.ValidateDeclarations(
                    new[] { first, second },
                    Array.Empty<PlayerActorDeclaration>(),
                    nameof(QaPlayerIdentityPanel),
                    "qa.actor.identity.duplicate-actor-id");

                bool passed = set.Failed && ContainsIssue(set, ActorSetIssueKind.DuplicateActorId, blocking: true);
                return new QaCheckResult(
                    passed,
                    passed
                        ? "Invalid Duplicate ActorId PASS. Validator emitted blocking DuplicateActorId."
                        : "Invalid Duplicate ActorId failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunPlayerActorCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ActorIdentity_ValidPlayerActor");
            try
            {
                PlayerInput input = root.AddComponent<PlayerInput>();
                PlayerActorDeclaration playerActor = root.AddComponent<PlayerActorDeclaration>();
                SetPrivateField(playerActor, "actorId", "qa.actor.player.valid");
                SetPrivateField(playerActor, "displayName", "Valid Player Actor");
                SetPrivateField(playerActor, "playerInput", input);
                SetPrivateField(playerActor, "reason", "qa.actor.identity.player.valid");

                PlayerActorSet playerSet = PlayerActorValidator.ValidateDeclarations(
                    new[] { playerActor },
                    nameof(QaPlayerIdentityPanel),
                    "qa.actor.identity.valid-player");

                ActorSet actorSet = ActorValidator.ValidateDeclarations(
                    Array.Empty<ActorDeclaration>(),
                    new[] { playerActor },
                    nameof(QaPlayerIdentityPanel),
                    "qa.actor.identity.valid-player.aggregate");

                bool passed = playerSet.Succeeded
                    && actorSet.Succeeded
                    && playerSet.PlayerInputEvidenceCount == 1
                    && actorSet.PlayerActorCount == 1;

                return new QaCheckResult(
                    passed,
                    passed
                        ? "Valid Player Actor PASS. PlayerActorDeclaration validates with same-GameObject PlayerInput evidence."
                        : "Valid Player Actor failed. playerSet=" + playerSet.ToDiagnosticString() + " actorSet=" + actorSet.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunUnknownRoleCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ActorIdentity_UnknownRole");
            try
            {
                ActorDeclaration actor = CreateActor(
                    root,
                    "Unknown Role Actor",
                    "qa.actor.role.unknown",
                    ActorKind.NonPlayer,
                    ActorRole.Unknown);

                ActorSet set = ActorValidator.ValidateDeclarations(
                    new[] { actor },
                    Array.Empty<PlayerActorDeclaration>(),
                    nameof(QaPlayerIdentityPanel),
                    "qa.actor.identity.unknown-role");

                bool hasUnknownRoleDiagnostic = ContainsIssue(set, ActorSetIssueKind.UnknownActorRole, blocking: false);
                bool passed = set.Succeeded && hasUnknownRoleDiagnostic;
                return new QaCheckResult(
                    passed,
                    passed
                        ? "Unknown Role Diagnostic PASS. Current policy is non-blocking UnknownActorRole."
                        : "Unknown Role Diagnostic failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunValidPlayerSlotCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_PlayerSlot_ValidDeclaration");
            try
            {
                PlayerSlotDeclaration slot = CreatePlayerSlot(root, "Valid PlayerSlot", "player.1", null);
                PlayerSlotSet set = PlayerSlotValidator.ValidateDeclarations(
                    new[] { slot },
                    Array.Empty<PlayerSlotOccupancy>(),
                    nameof(QaPlayerIdentityPanel),
                    "qa.player-slot.valid-declaration");

                bool passed = set.Succeeded
                    && set.Count == 1
                    && set.PlayerInputEvidenceCount == 0
                    && !set.RequiresPlayerInput;

                return new QaCheckResult(
                    passed,
                    passed
                        ? "Valid PlayerSlot Declaration PASS. PlayerSlotDeclaration validates without PlayerInput."
                        : "Valid PlayerSlot Declaration failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunInvalidPlayerSlotIdCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_PlayerSlot_InvalidEmptySlotId");
            try
            {
                PlayerSlotDeclaration slot = CreatePlayerSlot(root, "Invalid Empty PlayerSlotId", string.Empty, null);
                PlayerSlotSet set = PlayerSlotValidator.ValidateDeclarations(
                    new[] { slot },
                    Array.Empty<PlayerSlotOccupancy>(),
                    nameof(QaPlayerIdentityPanel),
                    "qa.player-slot.invalid-empty-slot-id");

                bool passed = set.Failed && ContainsIssue(set, PlayerSlotSetIssueKind.InvalidPlayerSlotId, blocking: true);
                return new QaCheckResult(
                    passed,
                    passed
                        ? "Invalid Empty PlayerSlotId PASS. Validator emitted blocking InvalidPlayerSlotId."
                        : "Invalid Empty PlayerSlotId failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunDuplicatePlayerSlotIdCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_PlayerSlot_DuplicateSlotId");
            try
            {
                PlayerSlotDeclaration first = CreatePlayerSlot(root, "Duplicate PlayerSlot A", "player.1", null);
                PlayerSlotDeclaration second = CreatePlayerSlot(root, "Duplicate PlayerSlot B", "player.1", null);
                PlayerSlotSet set = PlayerSlotValidator.ValidateDeclarations(
                    new[] { first, second },
                    Array.Empty<PlayerSlotOccupancy>(),
                    nameof(QaPlayerIdentityPanel),
                    "qa.player-slot.duplicate-slot-id");

                bool passed = set.Failed && ContainsIssue(set, PlayerSlotSetIssueKind.DuplicatePlayerSlotId, blocking: true);
                return new QaCheckResult(
                    passed,
                    passed
                        ? "Duplicate PlayerSlotId PASS. Validator emitted blocking DuplicatePlayerSlotId."
                        : "Duplicate PlayerSlotId failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunValidOccupancyCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_PlayerSlot_ValidOccupancy");
            try
            {
                ActorDeclaration actor = CreateActor(
                    root,
                    "Valid Occupied Actor",
                    "qa.actor.player.slot.valid",
                    ActorKind.Player,
                    ActorRole.Protagonist);

                PlayerSlotDeclaration slot = CreatePlayerSlot(root, "Valid Occupancy PlayerSlot", "player.1", null);
                PlayerSlotOccupancy occupancy = CreateOccupancy(root, "Valid PlayerSlot Occupancy", "player.1", actor, string.Empty);

                PlayerSlotSet set = PlayerSlotValidator.ValidateDeclarations(
                    new[] { slot },
                    new[] { occupancy },
                    nameof(QaPlayerIdentityPanel),
                    "qa.player-slot.valid-occupancy");

                bool passed = set.Succeeded
                    && set.Count == 1
                    && set.OccupancyCount == 1;

                return new QaCheckResult(
                    passed,
                    passed
                        ? "Valid PlayerSlot Occupancy PASS. PlayerSlotId resolves to ActorDeclaration ActorId."
                        : "Valid PlayerSlot Occupancy failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunMissingOccupiedActorCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_PlayerSlot_MissingOccupiedActor");
            try
            {
                PlayerSlotOccupancy occupancy = CreateOccupancy(root, "Missing Occupied Actor", "player.1", null, string.Empty);
                PlayerSlotSet set = PlayerSlotValidator.ValidateDeclarations(
                    Array.Empty<PlayerSlotDeclaration>(),
                    new[] { occupancy },
                    nameof(QaPlayerIdentityPanel),
                    "qa.player-slot.missing-occupied-actor");

                bool passed = set.Failed && ContainsIssue(set, PlayerSlotSetIssueKind.MissingOccupiedActor, blocking: true);
                return new QaCheckResult(
                    passed,
                    passed
                        ? "Missing Occupied Actor PASS. Validator emitted blocking MissingOccupiedActor."
                        : "Missing Occupied Actor failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunInvalidOccupiedActorIdCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_PlayerSlot_InvalidOccupiedActorId");
            try
            {
                ActorDeclaration invalidActor = CreateActor(
                    root,
                    "Invalid Occupied ActorId",
                    string.Empty,
                    ActorKind.Player,
                    ActorRole.Protagonist);

                PlayerSlotOccupancy occupancy = CreateOccupancy(root, "Invalid Occupied ActorId Occupancy", "player.1", invalidActor, string.Empty);
                PlayerSlotSet set = PlayerSlotValidator.ValidateDeclarations(
                    Array.Empty<PlayerSlotDeclaration>(),
                    new[] { occupancy },
                    nameof(QaPlayerIdentityPanel),
                    "qa.player-slot.invalid-occupied-actor-id");

                bool hasInvalidActorId = ContainsIssue(set, PlayerSlotSetIssueKind.InvalidOccupiedActorId, blocking: true);
                bool hasMissingActor = ContainsIssue(set, PlayerSlotSetIssueKind.MissingOccupiedActor, blocking: true);
                bool passed = set.Failed && (hasInvalidActorId || hasMissingActor);
                return new QaCheckResult(
                    passed,
                    passed
                        ? "Invalid Occupied ActorId PASS. Validator emitted: " + GetIssueKinds(set)
                        : "Invalid Occupied ActorId failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunDuplicateOccupancyCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_PlayerSlot_DuplicateOccupancy");
            try
            {
                ActorDeclaration firstActor = CreateActor(
                    root,
                    "Duplicate Occupancy Actor A",
                    "qa.actor.player.slot.duplicate.a",
                    ActorKind.Player,
                    ActorRole.Protagonist);

                ActorDeclaration secondActor = CreateActor(
                    root,
                    "Duplicate Occupancy Actor B",
                    "qa.actor.player.slot.duplicate.b",
                    ActorKind.NonPlayer,
                    ActorRole.Ally);

                PlayerSlotOccupancy first = CreateOccupancy(root, "Duplicate Occupancy A", "player.1", firstActor, string.Empty);
                PlayerSlotOccupancy second = CreateOccupancy(root, "Duplicate Occupancy B", "player.1", secondActor, string.Empty);

                PlayerSlotSet set = PlayerSlotValidator.ValidateDeclarations(
                    Array.Empty<PlayerSlotDeclaration>(),
                    new[] { first, second },
                    nameof(QaPlayerIdentityPanel),
                    "qa.player-slot.duplicate-occupancy");

                bool passed = set.Failed && ContainsIssue(set, PlayerSlotSetIssueKind.DuplicatePlayerSlotOccupancy, blocking: true);
                return new QaCheckResult(
                    passed,
                    passed
                        ? "Duplicate PlayerSlot Occupancy PASS. Validator emitted blocking DuplicatePlayerSlotOccupancy."
                        : "Duplicate PlayerSlot Occupancy failed. " + set.ToDiagnosticString());
            }
            finally
            {
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunAllPlayerSlotChecks()
        {
            QaCheckResult[] results =
            {
                RunValidPlayerSlotCheck(),
                RunInvalidPlayerSlotIdCheck(),
                RunDuplicatePlayerSlotIdCheck(),
                RunValidOccupancyCheck(),
                RunMissingOccupiedActorCheck(),
                RunInvalidOccupiedActorIdCheck(),
                RunDuplicateOccupancyCheck()
            };

            return FormatAggregateResult("Run All PlayerSlot QA", results);
        }

        private QaCheckResult RunAuthoredTextResetSubjectBridgeCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ResetBridge_AuthoredText");
            try
            {
                const string expectedSubjectId = "qa.reset.bridge.authored-text";
                UnityResetSubjectAdapter adapter = CreateResetSubjectAdapter(
                    root,
                    "AuthoredText Reset Subject Adapter",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    expectedSubjectId,
                    string.Empty,
                    ResetSubjectScope.Activity,
                    null);

                bool registered = TryRegisterResetSubject(adapter, "qa.reset-bridge.authored-text", out string registrationIssue);
                bool passed = registered
                    && adapter.IsRegistered
                    && adapter.SubjectId.IsValid
                    && string.Equals(adapter.SubjectId.StableText, expectedSubjectId, StringComparison.Ordinal);

                return new QaCheckResult(
                    passed,
                    passed
                        ? "AuthoredText Reset Subject Bridge PASS. Legacy subjectId path still registers expected ResetSubjectId."
                        : "AuthoredText Reset Subject Bridge failed. registered='" + registered + "' subjectId='" + ResolveAdapterSubjectIdText(adapter) + "' expected='" + expectedSubjectId + "' issue='" + registrationIssue + "'.");
            }
            finally
            {
                ClearResetSubjectAdapters(root, "qa.reset-bridge.authored-text.cleanup");
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunActorSourceResetSubjectBridgeCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ResetBridge_ActorSource");
            try
            {
                ActorDeclaration actor = CreateActor(
                    root,
                    "Reset Bridge Source Actor",
                    "qa.actor.reset.bridge",
                    ActorKind.Player,
                    ActorRole.Protagonist);

                string expectedSubjectId = actor.ActorId.StableText;
                UnityResetSubjectAdapter adapter = CreateResetSubjectAdapter(
                    root,
                    "ActorSource Reset Subject Adapter",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.reset.bridge.should-not-win",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    actor);

                bool registered = TryRegisterResetSubject(adapter, "qa.reset-bridge.actor-source", out string registrationIssue);
                bool passed = registered
                    && adapter.IsRegistered
                    && adapter.SubjectId.IsValid
                    && string.Equals(adapter.SubjectId.StableText, expectedSubjectId, StringComparison.Ordinal);

                return new QaCheckResult(
                    passed,
                    passed
                        ? "Actor Source Reset Subject Bridge PASS. sourceActor ActorId won over authored text."
                        : "Actor Source Reset Subject Bridge failed. registered='" + registered + "' subjectId='" + ResolveAdapterSubjectIdText(adapter) + "' expectedActorId='" + expectedSubjectId + "' issue='" + registrationIssue + "'.");
            }
            catch (Exception exception)
            {
                return new QaCheckResult(false, "Actor Source Reset Subject Bridge threw exception: " + exception.GetType().Name + " " + exception.Message);
            }
            finally
            {
                ClearResetSubjectAdapters(root, "qa.reset-bridge.actor-source.cleanup");
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunRuntimePrefixResetSubjectBridgeCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ResetBridge_RuntimePrefix");
            try
            {
                ActorDeclaration actor = CreateActor(
                    root,
                    "Runtime Prefix Ignored Actor",
                    "qa.actor.reset.runtime-prefix-ignored",
                    ActorKind.Player,
                    ActorRole.Protagonist);

                UnityResetSubjectAdapter adapter = CreateResetSubjectAdapter(
                    root,
                    "RuntimePrefix Reset Subject Adapter",
                    UnityResetSubjectIdGenerationMode.RuntimeInstanceId,
                    "qa.reset.bridge.should-not-win-runtime",
                    "qa.reset.bridge.runtime",
                    ResetSubjectScope.Activity,
                    actor);

                bool registered = TryRegisterResetSubject(adapter, "qa.reset-bridge.runtime-prefix", out string registrationIssue);
                string resolvedSubjectId = ResolveAdapterSubjectIdText(adapter);
                bool passed = registered
                    && adapter.IsRegistered
                    && adapter.SubjectId.IsValid
                    && !string.Equals(resolvedSubjectId, actor.ActorId.StableText, StringComparison.Ordinal)
                    && resolvedSubjectId.IndexOf("qa.reset.bridge.runtime", StringComparison.Ordinal) >= 0;

                return new QaCheckResult(
                    passed,
                    passed
                        ? "RuntimePrefix Reset Subject Bridge PASS. RuntimeInstanceId ignored sourceActor and used runtime prefix."
                        : "RuntimePrefix Reset Subject Bridge failed. registered='" + registered + "' subjectId='" + resolvedSubjectId + "' actorId='" + actor.ActorId.StableText + "' issue='" + registrationIssue + "'.");
            }
            catch (Exception exception)
            {
                return new QaCheckResult(false, "RuntimePrefix Reset Subject Bridge threw exception: " + exception.GetType().Name + " " + exception.Message);
            }
            finally
            {
                ClearResetSubjectAdapters(root, "qa.reset-bridge.runtime-prefix.cleanup");
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunInvalidActorSourceResetSubjectBridgeCheck()
        {
            GameObject root = CreateSyntheticRoot("QA_ResetBridge_InvalidActorSource");
            try
            {
                ActorDeclaration invalidActor = CreateActor(
                    root,
                    "Invalid Reset Bridge Source Actor",
                    string.Empty,
                    ActorKind.Player,
                    ActorRole.Protagonist);

                UnityResetSubjectAdapter adapter = CreateResetSubjectAdapter(
                    root,
                    "InvalidActorSource Reset Subject Adapter",
                    UnityResetSubjectIdGenerationMode.AuthoredStableId,
                    "qa.reset.bridge.should-not-win-invalid-actor",
                    string.Empty,
                    ResetSubjectScope.Activity,
                    invalidActor);

                bool registered = TryRegisterResetSubject(adapter, "qa.reset-bridge.invalid-actor-source", out string registrationIssue);
                bool exceptionCaptured = registrationIssue.StartsWith("EXCEPTION ", StringComparison.Ordinal);
                bool passed = !registered && !adapter.IsRegistered && !exceptionCaptured;

                return new QaCheckResult(
                    passed,
                    passed
                        ? "Invalid Actor Source Reset Subject Bridge PASS. Invalid sourceActor was rejected without exception or registration."
                        : "Invalid Actor Source Reset Subject Bridge failed. registered='" + registered + "' isRegistered='" + adapter.IsRegistered + "' subjectId='" + ResolveAdapterSubjectIdText(adapter) + "' issue='" + registrationIssue + "'.");
            }
            catch (Exception exception)
            {
                return new QaCheckResult(false, "Invalid Actor Source Reset Subject Bridge threw exception instead of controlled rejection: " + exception.GetType().Name + " " + exception.Message);
            }
            finally
            {
                ClearResetSubjectAdapters(root, "qa.reset-bridge.invalid-actor-source.cleanup");
                DestroySyntheticRoot(root);
            }
        }

        private QaCheckResult RunAllResetBridgeChecks()
        {
            QaCheckResult[] results =
            {
                RunAuthoredTextResetSubjectBridgeCheck(),
                RunActorSourceResetSubjectBridgeCheck(),
                RunRuntimePrefixResetSubjectBridgeCheck(),
                RunInvalidActorSourceResetSubjectBridgeCheck()
            };

            return FormatAggregateResult("Run All Reset Bridge QA", results);
        }

        private static ActorDeclaration CreateActor(
            GameObject root,
            string displayName,
            string actorId,
            ActorKind kind,
            ActorRole role)
        {
            GameObject actorObject = new GameObject(displayName);
            actorObject.transform.SetParent(root.transform, false);
            ActorDeclaration declaration = actorObject.AddComponent<ActorDeclaration>();
            SetPrivateField(declaration, "actorId", actorId);
            SetPrivateField(declaration, "actorKind", kind);
            SetPrivateField(declaration, "actorRole", role);
            SetPrivateField(declaration, "displayName", displayName);
            SetPrivateField(declaration, "reason", "qa.actor.identity.synthetic");
            return declaration;
        }

        private static PlayerSlotDeclaration CreatePlayerSlot(
            GameObject root,
            string displayName,
            string slotId,
            PlayerInput inputEvidence)
        {
            GameObject slotObject = new GameObject(displayName);
            slotObject.transform.SetParent(root.transform, false);
            PlayerSlotDeclaration declaration = slotObject.AddComponent<PlayerSlotDeclaration>();
            SetPrivateField(declaration, "slotId", slotId);
            SetPrivateField(declaration, "displayName", displayName);
            SetPrivateField(declaration, "playerInput", inputEvidence);
            SetPrivateField(declaration, "reason", "qa.player-slot.synthetic");
            return declaration;
        }

        private static PlayerSlotOccupancy CreateOccupancy(
            GameObject root,
            string displayName,
            string slotId,
            ActorDeclaration actorDeclaration,
            string occupiedActorId)
        {
            GameObject occupancyObject = new GameObject(displayName);
            occupancyObject.transform.SetParent(root.transform, false);
            PlayerSlotOccupancy occupancy = occupancyObject.AddComponent<PlayerSlotOccupancy>();
            SetPrivateField(occupancy, "slotDeclaration", (PlayerSlotDeclaration)null);
            SetPrivateField(occupancy, "slotId", slotId);
            SetPrivateField(occupancy, "actorDeclaration", actorDeclaration);
            SetPrivateField(occupancy, "occupiedActorId", occupiedActorId);
            SetPrivateField(occupancy, "displayName", displayName);
            SetPrivateField(occupancy, "reason", "qa.player-slot.occupancy.synthetic");
            return occupancy;
        }


        private static UnityResetSubjectAdapter CreateResetSubjectAdapter(
            GameObject root,
            string displayName,
            UnityResetSubjectIdGenerationMode idGeneration,
            string subjectId,
            string runtimeSubjectIdPrefix,
            ResetSubjectScope scope,
            ActorDeclaration sourceActor)
        {
            GameObject adapterObject = new GameObject(displayName);
            adapterObject.transform.SetParent(root.transform, false);
            UnityResetSubjectAdapter adapter = adapterObject.AddComponent<UnityResetSubjectAdapter>();
            SetPrivateField(adapter, "registerOnEnable", false);
            SetPrivateField(adapter, "unregisterOnDisable", true);
            SetPrivateField(adapter, "retryUntilRuntimeAvailable", false);
            SetPrivateField(adapter, "idGeneration", idGeneration);
            SetPrivateField(adapter, "subjectId", subjectId);
            SetPrivateField(adapter, "runtimeSubjectIdPrefix", runtimeSubjectIdPrefix);
            SetPrivateField(adapter, "scope", scope);
            SetPrivateField(adapter, "displayName", displayName);
            SetPrivateField(adapter, "diagnosticTag", "qa.reset-bridge");
            SetPrivateField(adapter, "participantDiscovery", UnityResetParticipantDiscoveryMode.SameGameObject);
            SetPrivateField(adapter, "includeInactiveParticipants", true);
            SetPrivateField(adapter, "includeUnityResettableComponents", false);
            SetPrivateField(adapter, "sourceActor", sourceActor);
            return adapter;
        }

        private static bool TryRegisterResetSubject(UnityResetSubjectAdapter adapter, string reason, out string issue)
        {
            issue = string.Empty;
            if (adapter == null)
            {
                issue = "adapter is null";
                return false;
            }

            try
            {
                bool registered = adapter.RegisterWithCurrentHost(reason);
                if (!registered)
                {
                    issue = "RegisterWithCurrentHost returned false. Check FrameworkRuntimeHost/Reset owner and adapter warning logs.";
                }

                return registered;
            }
            catch (Exception exception)
            {
                issue = "EXCEPTION " + exception.GetType().Name + ": " + exception.Message;
                return false;
            }
        }

        private static void ClearResetSubjectAdapters(GameObject root, string reason)
        {
            if (root == null)
            {
                return;
            }

            UnityResetSubjectAdapter[] adapters = root.GetComponentsInChildren<UnityResetSubjectAdapter>(true);
            for (int i = 0; i < adapters.Length; i++)
            {
                UnityResetSubjectAdapter adapter = adapters[i];
                if (adapter == null)
                {
                    continue;
                }

                try
                {
                    adapter.ClearRegistration(reason);
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("[QA_RESET_BRIDGE] Cleanup failed for reset subject adapter. error='" + exception.Message + "'.", adapter);
                }
            }
        }

        private static string ResolveAdapterSubjectIdText(UnityResetSubjectAdapter adapter)
        {
            if (adapter == null)
            {
                return "<null-adapter>";
            }

            try
            {
                return adapter.SubjectId.IsValid ? adapter.SubjectId.StableText : "<invalid-subject-id>";
            }
            catch (Exception exception)
            {
                return "<subject-id-error:" + exception.GetType().Name + ">";
            }
        }

        private static bool ContainsIssue(ActorSet set, ActorSetIssueKind kind, bool blocking)
        {
            if (set == null)
            {
                return false;
            }

            for (int i = 0; i < set.Issues.Count; i++)
            {
                ActorSetIssue issue = set.Issues[i];
                if (issue.Kind == kind && issue.Blocking == blocking)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsIssue(PlayerSlotSet set, PlayerSlotSetIssueKind kind, bool blocking)
        {
            if (set == null)
            {
                return false;
            }

            for (int i = 0; i < set.Issues.Count; i++)
            {
                PlayerSlotSetIssue issue = set.Issues[i];
                if (issue.Kind == kind && issue.Blocking == blocking)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetIssueKinds(PlayerSlotSet set)
        {
            if (set == null || set.Issues.Count == 0)
            {
                return "<none>";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < set.Issues.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(set.Issues[i].Kind);
                builder.Append(set.Issues[i].Blocking ? " blocking" : " non-blocking");
            }

            return builder.ToString();
        }

        private static QaCheckResult FormatAggregateResult(string title, QaCheckResult[] results)
        {
            bool passed = true;
            var builder = new StringBuilder();
            builder.AppendLine(title);
            for (int i = 0; i < results.Length; i++)
            {
                passed &= results[i].Passed;
                builder.Append(results[i].Passed ? "PASS" : "FAIL");
                builder.Append(" - ");
                builder.AppendLine(results[i].Message);
            }

            return new QaCheckResult(passed, builder.ToString().TrimEnd());
        }

        private static GameObject CreateSyntheticRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.hideFlags = HideFlags.DontSave;
            root.SetActive(false);
            return root;
        }

        private static void DestroySyntheticRoot(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(root);
                return;
            }

            DestroyImmediate(root);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }

        private void SetResult(QaCheckResult result)
        {
            SetResult(result, "[QA_ACTOR_IDENTITY]");
        }

        private void SetPlayerSlotResult(QaCheckResult result)
        {
            SetResult(result, "[QA_PLAYER_SLOT]");
        }

        private void SetResetBridgeResult(QaCheckResult result)
        {
            SetResult(result, "[QA_RESET_BRIDGE]");
        }

        private void SetResult(QaCheckResult result, string logPrefix)
        {
            _lastPassed = result.Passed;
            _lastResult = result.Message;
            string prefix = string.IsNullOrWhiteSpace(logPrefix) ? "[QA_PLAYER_IDENTITY]" : logPrefix;

            if (result.Passed)
            {
                _passCount++;
                Debug.Log(prefix + " " + result.Message, this);
            }
            else
            {
                _failCount++;
                Debug.LogWarning(prefix + " " + result.Message, this);
            }

            RefreshTexts();
        }

        private void RefreshTexts()
        {
            string summary = $"Actor + PlayerSlot + Reset Bridge QA | passes={_passCount} failures={_failCount}";
            if (summaryText != null)
            {
                summaryText.text = summary;
            }

            if (resultText != null)
            {
                resultText.text = _lastResult;
                resultText.color = _lastPassed ? new Color(0.55f, 1f, 0.55f, 1f) : Color.white;
            }
        }

        private void OnGUI()
        {
            panelRect = ClampToScreen(GUI.Window(System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this), panelRect, DrawWindow, title));
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(8f);
            GUILayout.Label($"Passes: {_passCount} | Failures: {_failCount}");
            GUILayout.Label("Last Result:");
            GUILayout.TextArea(_lastResult, GUILayout.MinHeight(120f));
            GUILayout.Space(8f);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(Mathf.Max(260f, panelRect.height - 190f)));

            GUILayout.Label("Actor Identity");
            if (GUILayout.Button("Run Valid Actor QA", GUILayout.Height(32f)))
            {
                RunValidActorQa();
            }

            if (GUILayout.Button("Run Invalid ActorId QA", GUILayout.Height(32f)))
            {
                RunInvalidActorIdQa();
            }

            if (GUILayout.Button("Run Duplicate ActorId QA", GUILayout.Height(32f)))
            {
                RunDuplicateActorIdQa();
            }

            if (GUILayout.Button("Run PlayerActor QA", GUILayout.Height(32f)))
            {
                RunPlayerActorQa();
            }

            if (GUILayout.Button("Run All Actor Identity QA", GUILayout.Height(36f)))
            {
                RunAllActorIdentityQa();
            }

            GUILayout.Space(8f);
            GUILayout.Label("PlayerSlot Identity");
            if (GUILayout.Button("Run Valid PlayerSlot QA", GUILayout.Height(32f)))
            {
                RunValidPlayerSlotQa();
            }

            if (GUILayout.Button("Run Invalid PlayerSlotId QA", GUILayout.Height(32f)))
            {
                RunInvalidPlayerSlotIdQa();
            }

            if (GUILayout.Button("Run Duplicate PlayerSlotId QA", GUILayout.Height(32f)))
            {
                RunDuplicatePlayerSlotIdQa();
            }

            if (GUILayout.Button("Run Valid Occupancy QA", GUILayout.Height(32f)))
            {
                RunValidOccupancyQa();
            }

            if (GUILayout.Button("Run Missing Occupied Actor QA", GUILayout.Height(32f)))
            {
                RunMissingOccupiedActorQa();
            }

            if (GUILayout.Button("Run Invalid Occupied ActorId QA", GUILayout.Height(32f)))
            {
                RunInvalidOccupiedActorIdQa();
            }

            if (GUILayout.Button("Run Duplicate Occupancy QA", GUILayout.Height(32f)))
            {
                RunDuplicateOccupancyQa();
            }

            if (GUILayout.Button("Run All PlayerSlot QA", GUILayout.Height(36f)))
            {
                RunAllPlayerSlotQa();
            }

            if (GUILayout.Button("Run All Actor + PlayerSlot QA", GUILayout.Height(36f)))
            {
                RunAllActorAndPlayerSlotQa();
            }

            GUILayout.Space(8f);
            GUILayout.Label("Reset ActorId Bridge");
            if (GUILayout.Button("Run AuthoredText Reset Subject QA", GUILayout.Height(32f)))
            {
                RunAuthoredTextResetSubjectBridgeQa();
            }

            if (GUILayout.Button("Run Actor Source Reset Subject QA", GUILayout.Height(32f)))
            {
                RunActorSourceResetSubjectBridgeQa();
            }

            if (GUILayout.Button("Run Runtime Prefix Reset Subject QA", GUILayout.Height(32f)))
            {
                RunRuntimePrefixResetSubjectBridgeQa();
            }

            if (GUILayout.Button("Run Invalid Actor Source Reset Subject QA", GUILayout.Height(32f)))
            {
                RunInvalidActorSourceResetSubjectBridgeQa();
            }

            if (GUILayout.Button("Run All Reset Bridge QA", GUILayout.Height(36f)))
            {
                RunAllResetBridgeQa();
            }

            if (GUILayout.Button("Run All Actor + PlayerSlot + Reset Bridge QA", GUILayout.Height(36f)))
            {
                RunAllActorPlayerSlotAndResetBridgeQa();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            GUI.enabled = backToHubTrigger != null;
            if (GUILayout.Button("Back to QA Hub", GUILayout.Height(30f)))
            {
                BackToQaHub();
            }

            GUI.enabled = true;
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }

        private static Rect ClampToScreen(Rect rect)
        {
            float width = Mathf.Max(360f, rect.width);
            float height = Mathf.Max(360f, rect.height);
            float maxX = Mathf.Max(0f, Screen.width - width);
            float maxY = Mathf.Max(0f, Screen.height - height);
            return new Rect(Mathf.Clamp(rect.x, 0f, maxX), Mathf.Clamp(rect.y, 0f, maxY), width, height);
        }

        private readonly struct QaCheckResult
        {
            public QaCheckResult(bool passed, string message)
            {
                Passed = passed;
                Message = string.IsNullOrWhiteSpace(message) ? "<empty result>" : message;
            }

            public bool Passed { get; }
            public string Message { get; }
        }
    }
}

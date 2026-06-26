using System;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.Gate;

namespace Immersive.Framework.Pause
{
    /// <summary>
    /// API status: Experimental. Passive relationship between logical Pause state and Gate blockers.
    /// This policy describes which Gate blockers a paused framework state would expose. It does not register,
    /// apply, release or own Gate state, input, UI, overlay or Time.timeScale.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F20D passive Pause-to-Gate blocker policy; no runtime registry or request path.")]
    public static class PauseGateBlockerPolicy
    {
        public const string PolicySource = "F20D.PauseGateBlocker";
        public const string GameplayActionBlockerId = "pause-state-paused-gameplay-action";
        public const string InteractionAcceptanceBlockerId = "pause-state-paused-interaction-acceptance";

        public static GateBlocker CreateGameplayActionBlocker(PauseSnapshot snapshot, string source, string reason)
        {
            ValidatePausedSnapshot(snapshot, nameof(snapshot));

            return GateBlocker.ForAnyOwner(
                GameplayActionBlockerId,
                GateScope.Gameplay,
                GateDomain.GameplayAction,
                NormalizeSource(source),
                NormalizeReason(reason, snapshot, "Gameplay actions are blocked while Pause state is Paused."),
                PolicySource);
        }

        public static GateBlocker CreateInteractionAcceptanceBlocker(PauseSnapshot snapshot, string source, string reason)
        {
            ValidatePausedSnapshot(snapshot, nameof(snapshot));

            return GateBlocker.ForAnyOwner(
                InteractionAcceptanceBlockerId,
                GateScope.Interaction,
                GateDomain.InteractionAcceptance,
                NormalizeSource(source),
                NormalizeReason(reason, snapshot, "Interaction acceptance is blocked while Pause state is Paused."),
                PolicySource);
        }

        public static GateSnapshot CreateSnapshotForState(PauseState state, string source, string reason)
        {
            if (!Enum.IsDefined(typeof(PauseState), state) || state == PauseState.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Pause Gate blocker policy requires an explicit Pause state.");
            }

            var pauseSnapshot = PauseSnapshot.FromState(
                state,
                NormalizeSource(source),
                string.IsNullOrWhiteSpace(reason) ? "pause.gate.snapshot" : reason.Trim(),
                state == PauseState.Paused
                    ? new[] { "Pause Gate blocker snapshot created for paused state." }
                    : new[] { "Pause Gate blocker snapshot released for running state." });

            return CreateSnapshotForPauseSnapshot(pauseSnapshot, source, reason);
        }

        public static GateSnapshot CreateSnapshotForPauseSnapshot(PauseSnapshot snapshot, string source, string reason)
        {
            if (!snapshot.IsValid)
            {
                throw new ArgumentException("Pause Gate blocker policy requires a valid Pause snapshot.", nameof(snapshot));
            }

            if (snapshot.IsRunning)
            {
                return CreateReleasedSnapshot();
            }

            if (!snapshot.IsPaused)
            {
                throw new ArgumentOutOfRangeException(nameof(snapshot), snapshot.State, "Pause Gate blocker policy only supports Running or Paused states.");
            }

            return new GateSnapshot(new[]
            {
                CreateGameplayActionBlocker(snapshot, source, reason),
                CreateInteractionAcceptanceBlocker(snapshot, source, reason)
            });
        }

        public static GateSnapshot CreateReleasedSnapshot()
        {
            return GateSnapshot.Empty();
        }

        private static void ValidatePausedSnapshot(PauseSnapshot snapshot, string parameterName)
        {
            if (!snapshot.IsValid)
            {
                throw new ArgumentException("Pause Gate blocker requires a valid Pause snapshot.", parameterName);
            }

            if (!snapshot.IsPaused)
            {
                throw new ArgumentException("Pause Gate blocker can only be created from a paused Pause snapshot.", parameterName);
            }
        }

        private static string NormalizeSource(string source)
        {
            return string.IsNullOrWhiteSpace(source) ? nameof(PauseGateBlockerPolicy) : source.Trim();
        }

        private static string NormalizeReason(string reason, PauseSnapshot snapshot, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(reason))
            {
                return reason.Trim();
            }

            var requestText = snapshot.HasLastRequest ? snapshot.LastRequestId.StableText : "<none>";
            return $"{fallback} state='{snapshot.State}' lastRequest='{requestText}'.";
        }
    }
}

using Immersive.Foundation.Events;
using Immersive.Framework.ApiStatus;
using Immersive.Framework.GameFlow;
using UnityEngine;

namespace Immersive.Framework.CycleReset
{
    /// <summary>
    /// API status: Experimental. Foundation event emitted by scene-authored Cycle Reset triggers.
    /// This is an authored request boundary only; Cycle Reset remains Route/Activity lifecycle reset.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "F11F Cycle Reset authored trigger event surface.")]
    public sealed class CycleResetTriggerEvent : IEvent
    {
        public CycleResetTriggerEvent(
            MonoBehaviour trigger,
            CycleResetScope scope,
            FlowRequestEventPhase phase,
            FlowRequestOutcome outcome,
            string source,
            string reason,
            string message,
            CycleResetResult result,
            bool hasResult)
        {
            Trigger = trigger;
            Scope = scope;
            Phase = phase;
            Outcome = outcome;
            Source = source ?? string.Empty;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
            Result = result;
            HasResult = hasResult;
        }

        public MonoBehaviour Trigger { get; }

        public CycleResetScope Scope { get; }

        public FlowRequestEventPhase Phase { get; }

        public FlowRequestOutcome Outcome { get; }

        public string Source { get; }

        public string Reason { get; }

        public string Message { get; }

        public CycleResetResult Result { get; }

        public bool HasResult { get; }

        public bool IsSubmitted => Phase == FlowRequestEventPhase.Submitted;

        public bool IsCompleted => Phase == FlowRequestEventPhase.Completed;

        public bool Succeeded => Outcome == FlowRequestOutcome.Succeeded;

        public bool Ignored => Outcome == FlowRequestOutcome.Ignored;

        public bool Failed => Outcome == FlowRequestOutcome.Failed;
    }
}

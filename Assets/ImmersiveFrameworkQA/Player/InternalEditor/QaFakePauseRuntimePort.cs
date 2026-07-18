using System;
using Immersive.Framework.Pause;

namespace ImmersiveFrameworkQA.InputMode.Editor
{
    internal sealed class QaFakePauseRuntimePort : IPauseRuntimePort
    {
        internal QaFakePauseRuntimePort()
        {
            Snapshot = PauseSnapshot.FromState(
                PauseState.Running,
                nameof(QaFakePauseRuntimePort),
                "initial",
                Array.Empty<string>());
        }

        internal PauseSnapshot Snapshot { get; set; }
        internal PauseResult ConfiguredRequestResult { get; set; }
        internal Func<PauseRequest, PauseResult> ConfiguredRequestResultFactory { get; set; }
        internal bool HasSnapshot { get; set; } = true;
        internal int TryGetPauseSnapshotCount { get; private set; }
        internal int RequestPauseCount { get; private set; }
        internal int SnapshotCallCount => TryGetPauseSnapshotCount;
        internal int RequestCallCount => RequestPauseCount;
        internal PauseRequest LastPauseRequest { get; private set; }

        public bool TryGetPauseSnapshot(out PauseSnapshot snapshot)
        {
            TryGetPauseSnapshotCount++;
            snapshot = Snapshot;
            return HasSnapshot;
        }

        public PauseResult RequestPause(PauseRequest request)
        {
            RequestPauseCount++;
            LastPauseRequest = request;
            PauseResult result = ConfiguredRequestResultFactory != null
                ? ConfiguredRequestResultFactory(request)
                : ConfiguredRequestResult.IsValid
                    ? ConfiguredRequestResult
                    : PauseResult.AppliedResult(
                        request,
                        Snapshot.State,
                        PauseRequest.ResolveTargetState(request.Kind, Snapshot.State),
                        "QA fake Pause runtime port applied the request.");
            Snapshot = PauseSnapshot.FromResult(result, Array.Empty<string>());
            return result;
        }
    }
}

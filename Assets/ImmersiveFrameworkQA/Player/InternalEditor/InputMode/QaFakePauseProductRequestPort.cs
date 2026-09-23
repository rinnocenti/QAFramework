using System;
using Immersive.Framework.Pause;
namespace ImmersiveFrameworkQA.InputMode.Internal.Editor
{
    internal sealed class QaFakePauseProductRequestPort : IPauseProductRequestPort
    {
        internal QaFakePauseProductRequestPort()
        {
            Snapshot = PauseSnapshot.FromState(
                PauseState.Running,
                nameof(QaFakePauseProductRequestPort),
                "initial",
                Array.Empty<string>());
        }

        internal PauseSnapshot Snapshot { get; set; }
        internal PauseProductRequestResult ConfiguredRequestResult { get; set; }
        internal Func<PauseRequest, PauseProductRequestResult> ConfiguredRequestResultFactory { get; set; }
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

        public PauseProductRequestResult RequestPause(PauseRequest request)
        {
            RequestPauseCount++;
            LastPauseRequest = request;
            PauseProductRequestResult result = ConfiguredRequestResultFactory != null
                ? ConfiguredRequestResultFactory(request)
                : ConfiguredRequestResult.Status != PauseProductRequestStatus.Unknown
                    ? ConfiguredRequestResult
                    : CreateAppliedResult(request);
            if (result.PauseResult.IsValid)
            {
                Snapshot = PauseSnapshot.FromResult(result.PauseResult, Array.Empty<string>());
            }

            return result;
        }

        private PauseProductRequestResult CreateAppliedResult(PauseRequest request)
        {
            PauseResult pauseResult = PauseResult.AppliedResult(
                request,
                Snapshot.State,
                PauseRequest.ResolveTargetState(request.Kind, Snapshot.State),
                "QA fake Pause product request port applied the request.");
            return new PauseProductRequestResult(
                PauseProductRequestStatus.Applied,
                pauseResult,
                null,
                pauseResult.Message);
        }
    }
}

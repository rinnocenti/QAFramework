using System;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    internal sealed class QaFakeActivityRuntimePort : IActivityRuntimePort
    {
        internal Func<ActivityAsset, string, string, FrameworkActivityRequestResult>
            RequestResultFactory { get; set; }

        internal Func<string, string, FrameworkActivityRequestResult>
            ClearResultFactory { get; set; }

        internal Exception ExceptionToThrow { get; set; }

        internal int RequestActivityCallCount { get; private set; }

        internal int ClearActivityCallCount { get; private set; }

        internal ActivityAsset LastTargetActivity { get; private set; }

        internal string LastSource { get; private set; }

        internal string LastReason { get; private set; }

        public Task<FrameworkActivityRequestResult> RequestActivityAsync(
            ActivityAsset targetActivity,
            string source,
            string reason)
        {
            RequestActivityCallCount++;
            LastTargetActivity = targetActivity;
            LastSource = source;
            LastReason = reason;
            ThrowIfConfigured();

            FrameworkActivityRequestResult result = RequestResultFactory != null
                ? RequestResultFactory(targetActivity, source, reason)
                : FrameworkActivityRequestResult.SucceededWith(
                    targetActivity,
                    source,
                    reason,
                    default);
            return Task.FromResult(result);
        }

        public Task<FrameworkActivityRequestResult> ClearActivityAsync(
            string source,
            string reason)
        {
            ClearActivityCallCount++;
            LastTargetActivity = null;
            LastSource = source;
            LastReason = reason;
            ThrowIfConfigured();

            FrameworkActivityRequestResult result = ClearResultFactory != null
                ? ClearResultFactory(source, reason)
                : FrameworkActivityRequestResult.SucceededWith(
                    null,
                    source,
                    reason,
                    default);
            return Task.FromResult(result);
        }

        private void ThrowIfConfigured()
        {
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }
        }
    }
}

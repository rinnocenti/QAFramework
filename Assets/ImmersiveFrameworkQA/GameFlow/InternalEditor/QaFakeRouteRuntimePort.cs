using System;
using System.Threading.Tasks;
using Immersive.Framework.Authoring;
using Immersive.Framework.GameFlow;
namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor.ImmersiveFrameworkQA.GameFlow.InternalEditor
{
    internal sealed class QaFakeRouteRuntimePort : IRouteRuntimePort
    {
        internal Func<RouteAsset, string, string, FrameworkRouteRequestResult>
            ResultFactory { get; set; }
        internal Exception ExceptionToThrow { get; set; }
        internal int RequestCallCount { get; private set; }
        internal RouteAsset LastTargetRoute { get; private set; }
        internal string LastSource { get; private set; }
        internal string LastReason { get; private set; }

        public Task<FrameworkRouteRequestResult> RequestRouteAsync(
            RouteAsset targetRoute,
            string source,
            string reason)
        {
            RequestCallCount++;
            LastTargetRoute = targetRoute;
            LastSource = source;
            LastReason = reason;
            if (ExceptionToThrow != null)
            {
                throw ExceptionToThrow;
            }

            FrameworkRouteRequestResult result = ResultFactory != null
                ? ResultFactory(targetRoute, source, reason)
                : FrameworkRouteRequestResult.SucceededWith(
                    targetRoute,
                    source,
                    reason,
                    default);
            return Task.FromResult(result);
        }
    }
}

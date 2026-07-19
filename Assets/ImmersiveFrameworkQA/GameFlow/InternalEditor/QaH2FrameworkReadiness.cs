using Immersive.Framework.ApplicationLifecycle;

namespace ImmersiveFrameworkQA.GameFlow.Internal.Editor
{
    public static class QaH2FrameworkReadiness
    {
        public static bool TryGetReady(out string diagnostic)
        {
            if (!FrameworkRuntimeHost.TryGetCurrent(out FrameworkRuntimeHost host) || host == null)
            {
                diagnostic = "host='unavailable'.";
                return false;
            }

            FrameworkRuntimeState state = host.State;
            diagnostic = $"host='available' gameFlowStarted='{state.GameFlowStarted}' route='{state.CurrentRouteName}' activity='{state.CurrentActivityName}' activityReady='{state.IsActivityReady}'.";
            return state.GameFlowStarted &&
                state.CurrentRoute != null &&
                state.CurrentActivity != null &&
                state.IsActivityReady;
        }
    }
}

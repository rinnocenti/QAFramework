using Immersive.Framework.Authoring;
using Immersive.Framework.RouteLifecycle;
using UnityEngine;

namespace ImmersiveFrameworkQA.Lifecycle
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Immersive Framework QA/Lifecycle/Route Content Lifecycle Probe")]
    public sealed class QaRouteContentLifecycleProbe : MonoBehaviour, IRouteContentLifecycleReceiver
    {
        public int EnterCount { get; private set; }

        public int ExitCount { get; private set; }

        public RouteAsset LastRoute { get; private set; }

        public RouteAsset LastPreviousRoute { get; private set; }

        public RouteAsset LastNextRoute { get; private set; }

        public void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            EnterCount++;
            LastRoute = context.Route;
            LastPreviousRoute = context.PreviousRoute;
            LastNextRoute = context.NextRoute;
        }

        public void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            ExitCount++;
            LastRoute = context.Route;
            LastPreviousRoute = context.PreviousRoute;
            LastNextRoute = context.NextRoute;
        }
    }
}

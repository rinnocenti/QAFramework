using System;
using Immersive.Framework.RouteLifecycle;

namespace Immersive.QaFramework.New003
{
    public sealed class QaNew003RouteLifecycleProbe : RouteContentBehaviour
    {
        public event Action<QaNew003RouteLifecycleProbe, RouteContentLifecycleContext> Entered;
        public event Action<QaNew003RouteLifecycleProbe, RouteContentLifecycleContext> Exited;

        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }
        public RouteContentLifecycleContext LastEnteredContext { get; private set; }
        public RouteContentLifecycleContext LastExitedContext { get; private set; }

        protected override void OnRouteContentEntered(RouteContentLifecycleContext context)
        {
            EnterCount++;
            LastEnteredContext = context;
            Entered?.Invoke(this, context);
        }

        protected override void OnRouteContentExited(RouteContentLifecycleContext context)
        {
            ExitCount++;
            LastExitedContext = context;
            Exited?.Invoke(this, context);
        }
    }
}

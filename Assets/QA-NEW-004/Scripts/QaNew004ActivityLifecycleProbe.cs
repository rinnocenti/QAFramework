using System;
using Immersive.Framework.ActivityFlow;

namespace Immersive.QaFramework.New004
{
    public sealed class QaNew004ActivityLifecycleProbe : ActivityContentBehaviour
    {
        public event Action<QaNew004ActivityLifecycleProbe, ActivityContentLifecycleContext> Entered;
        public event Action<QaNew004ActivityLifecycleProbe, ActivityContentLifecycleContext> Exited;

        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }
        public ActivityContentLifecycleContext LastEnteredContext { get; private set; }
        public ActivityContentLifecycleContext LastExitedContext { get; private set; }

        protected override void OnActivityContentEntered(
            ActivityContentLifecycleContext context)
        {
            EnterCount++;
            LastEnteredContext = context;
            Entered?.Invoke(this, context);
        }

        protected override void OnActivityContentExited(
            ActivityContentLifecycleContext context)
        {
            ExitCount++;
            LastExitedContext = context;
            Exited?.Invoke(this, context);
        }
    }
}

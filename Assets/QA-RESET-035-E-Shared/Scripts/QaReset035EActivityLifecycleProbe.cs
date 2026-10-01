using System;
using Immersive.Framework.ActivityFlow;

namespace Immersive.QaFramework.Reset035E
{
    public sealed class QaReset035EActivityLifecycleProbe : ActivityContentBehaviour
    {
        public event Action<QaReset035EActivityLifecycleProbe, ActivityContentLifecycleContext> Entered;
        public event Action<QaReset035EActivityLifecycleProbe, ActivityContentLifecycleContext> Exited;

        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }

        protected override void OnActivityContentEntered(ActivityContentLifecycleContext context)
        {
            EnterCount++;
            Entered?.Invoke(this, context);
        }

        protected override void OnActivityContentExited(ActivityContentLifecycleContext context)
        {
            ExitCount++;
            Exited?.Invoke(this, context);
        }
    }
}

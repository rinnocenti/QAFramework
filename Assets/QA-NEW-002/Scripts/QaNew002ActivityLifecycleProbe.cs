using System;
using Immersive.Framework.ActivityFlow;

namespace Immersive.QaFramework.New002
{
    public sealed class QaNew002ActivityLifecycleProbe : ActivityContentBehaviour
    {
        public event Action<QaNew002ActivityLifecycleProbe, ActivityContentLifecycleContext> Entered;
        public event Action<QaNew002ActivityLifecycleProbe, ActivityContentLifecycleContext> Exited;

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

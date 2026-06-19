using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed class ActivityExitedEvent : IEvent
    {
        public ActivityExitedEvent(ActivityAsset activity, ActivityAsset nextActivity)
        {
            Activity = activity;
            NextActivity = nextActivity;
        }

        public ActivityAsset Activity { get; }

        public ActivityAsset NextActivity { get; }
    }
}

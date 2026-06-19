using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed class ActivityEnteredEvent : IEvent
    {
        public ActivityEnteredEvent(ActivityAsset activity, ActivityAsset previousActivity)
        {
            Activity = activity;
            PreviousActivity = previousActivity;
        }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }
    }
}

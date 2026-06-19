using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed class ActivityExitedEvent : IEvent
    {
        public ActivityExitedEvent(ActivityAsset activity, ActivityAsset nextActivity, string source, string reason)
        {
            Activity = activity;
            NextActivity = nextActivity;
            Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }

        public ActivityAsset Activity { get; }

        public ActivityAsset NextActivity { get; }

        public string Source { get; }

        public string Reason { get; }
    }
}

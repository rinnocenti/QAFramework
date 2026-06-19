using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;

namespace Immersive.Framework.ActivityFlow
{
    internal sealed class ActivityEnteredEvent : IEvent
    {
        public ActivityEnteredEvent(ActivityAsset activity, ActivityAsset previousActivity, string source, string reason)
        {
            Activity = activity;
            PreviousActivity = previousActivity;
            Source = string.IsNullOrWhiteSpace(source) ? "Unknown" : source.Trim();
            Reason = string.IsNullOrWhiteSpace(reason) ? "None" : reason.Trim();
        }

        public ActivityAsset Activity { get; }

        public ActivityAsset PreviousActivity { get; }

        public string Source { get; }

        public string Reason { get; }
    }
}

using Immersive.Foundation.Events;
using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
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

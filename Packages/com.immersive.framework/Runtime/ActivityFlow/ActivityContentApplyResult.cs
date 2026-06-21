using Immersive.Framework.Authoring;
using Immersive.Framework.ApiStatus;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Diagnostics-only result for applying scene-authored Activity content bindings.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Experimental, "Baseline surface kept for development use until the owning roadmap phase stabilizes it.")]
    internal readonly struct ActivityContentApplyResult
    {
        public ActivityContentApplyResult(
            ActivityAsset activeActivity,
            int bindingCount,
            int activatedCount,
            int deactivatedCount,
            int unchangedCount,
            int missingActivityCount,
            ActivityContentSet activityContentSet,
            string message,
            string detailMessage,
            string warningMessage)
        {
            ActiveActivity = activeActivity;
            BindingCount = bindingCount;
            ActivatedCount = activatedCount;
            DeactivatedCount = deactivatedCount;
            UnchangedCount = unchangedCount;
            MissingActivityCount = missingActivityCount;
            ActivityContentSet = activityContentSet;
            Message = message ?? string.Empty;
            DetailMessage = detailMessage ?? string.Empty;
            WarningMessage = warningMessage ?? string.Empty;
        }

        public ActivityAsset ActiveActivity { get; }

        public int BindingCount { get; }

        public int ActivatedCount { get; }

        public int DeactivatedCount { get; }

        public int UnchangedCount { get; }

        public int MissingActivityCount { get; }

        public ActivityContentSet ActivityContentSet { get; }

        public int ActivityContentCount => ActivityContentSet.Count;

        public bool HasActivityContent => ActivityContentSet.HasContent;

        public string Message { get; }

        public string DetailMessage { get; }

        public string WarningMessage { get; }

        public bool HasBindings => BindingCount > 0;

        public bool HasDetailMessage => !string.IsNullOrWhiteSpace(DetailMessage);

        public bool HasWarningMessage => !string.IsNullOrWhiteSpace(WarningMessage);

        public static ActivityContentApplyResult Empty(ActivityAsset activeActivity = null)
        {
            return new ActivityContentApplyResult(activeActivity, 0, 0, 0, 0, 0, ActivityContentSet.Empty(activeActivity), string.Empty, string.Empty, string.Empty);
        }

        public static ActivityContentApplyResult Applied(
            ActivityAsset activeActivity,
            int bindingCount,
            int activatedCount,
            int deactivatedCount,
            int unchangedCount,
            int missingActivityCount,
            ActivityContentSet activityContentSet,
            string detailMessage,
            string warningMessage)
        {
            if (bindingCount <= 0)
            {
                return Empty(activeActivity);
            }

            string target = activeActivity != null
                ? $"for Activity '{activeActivity.ActivityName}'"
                : "with no active Activity";

            string message = $"Activity Content applied {bindingCount} binding(s) {target}. activated='{activatedCount}' deactivated='{deactivatedCount}' unchanged='{unchangedCount}' activityContentHandles='{activityContentSet.Count}'.";
            if (missingActivityCount > 0)
            {
                message += $" missingActivity='{missingActivityCount}'.";
            }

            return new ActivityContentApplyResult(
                activeActivity,
                bindingCount,
                activatedCount,
                deactivatedCount,
                unchangedCount,
                missingActivityCount,
                activityContentSet,
                message,
                detailMessage,
                warningMessage);
        }
    }
}

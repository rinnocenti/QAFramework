using Immersive.Framework.ApiStatus;
using Immersive.Framework.Authoring;
using Immersive.Framework.ContentFlow;

namespace Immersive.Framework.ActivityFlow
{
    /// <summary>
    /// Immutable evidence record for one Activity scene composition plan entry.
    /// F25B does not load scenes; this only records whether the declaration is execution-ready for future cuts.
    /// </summary>
    [FrameworkApiStatus(FrameworkApiStatus.Internal, "F25B Activity scene composition result entry; planning evidence only.")]
    internal readonly struct ActivitySceneCompositionResultEntry
    {
        public ActivitySceneCompositionResultEntry(
            ActivitySceneCompositionPlanEntry planEntry,
            ActivitySceneCompositionEntryStatus status,
            bool blocksComposition,
            string message)
        {
            PlanEntry = planEntry;
            Status = status;
            BlocksComposition = blocksComposition;
            Message = Normalize(message);
        }

        public ActivitySceneCompositionPlanEntry PlanEntry { get; }

        public ActivitySceneCompositionEntryStatus Status { get; }

        public bool BlocksComposition { get; }

        public string Message { get; }

        public FrameworkContentIdentity ContentIdentity => PlanEntry.ContentIdentity;

        public string ContentId => PlanEntry.ContentId;

        public string SceneName => PlanEntry.SceneName;

        public string ScenePath => PlanEntry.ScenePath;

        public FrameworkContentRequiredness Requiredness => PlanEntry.Requiredness;

        public ActivityContentSceneLoadMode LoadMode => PlanEntry.LoadMode;

        public ActivityContentReleasePolicy ReleasePolicy => PlanEntry.ReleasePolicy;

        public int ExecutionOrder => PlanEntry.ExecutionOrder;

        public bool Planned => Status == ActivitySceneCompositionEntryStatus.Planned;

        public bool NotExecutionReady => Status == ActivitySceneCompositionEntryStatus.NotExecutionReady;

        public bool HasIssue => NotExecutionReady || BlocksComposition;

        public string ToDiagnosticString()
        {
            var scene = !string.IsNullOrWhiteSpace(SceneName) ? SceneName : ScenePath;
            if (string.IsNullOrWhiteSpace(scene))
            {
                scene = "<missing>";
            }

            var identity = ContentIdentity.IsValid ? ContentIdentity.StableText : "<missing>";
            var contentId = !string.IsNullOrWhiteSpace(ContentId) ? ContentId : "<missing>";
            var message = !string.IsNullOrWhiteSpace(Message) ? Message : "<none>";
            return $"identity='{identity}' id='{contentId}' scene='{scene}' requiredness='{Requiredness}' loadMode='{LoadMode}' releasePolicy='{ReleasePolicy}' order='{ExecutionOrder}' status='{Status}' blocksComposition='{BlocksComposition}' message='{message}'";
        }

        public static ActivitySceneCompositionResultEntry PlannedEntry(ActivitySceneCompositionPlanEntry planEntry)
        {
            return new ActivitySceneCompositionResultEntry(
                planEntry,
                ActivitySceneCompositionEntryStatus.Planned,
                false,
                "Activity scene declaration is execution-ready. F25B records planning evidence only; loading is deferred.");
        }

        public static ActivitySceneCompositionResultEntry NotExecutionReadyEntry(ActivitySceneCompositionPlanEntry planEntry)
        {
            var blocksComposition = planEntry.Requiredness == FrameworkContentRequiredness.Required;
            var message = blocksComposition
                ? "Required Activity scene declaration is not execution-ready. Scene, explicit content id and content identity are required."
                : "Optional Activity scene declaration is not execution-ready and will be skipped once execution exists.";

            return new ActivitySceneCompositionResultEntry(
                planEntry,
                ActivitySceneCompositionEntryStatus.NotExecutionReady,
                blocksComposition,
                message);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
